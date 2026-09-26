"""Keying: removes the flat green background and the magenta mannequin, with despill.

Every raw source is opaque: the item on a magenta mannequin (or, for a base
figure, the figure itself) on flat #00FF00. The keyer classifies pixels,
then computes a soft alpha only in a thin band where the item meets a key
colour, and decontaminates that band. Decisions (README "Keying"):

* Green is keyed by its colour difference gd = G - max(R, B), normalised by
  the measured background's. Only gd >= 0.3 of the background's is "green"
  (low enough to catch small background holes darkened by their outline).
  Bottle green (24, 64, 40: gd ~ 24), olive and grey-green jade (64, 88, 72:
  gd ~ 16) are an order of magnitude below that and are never keyed; their
  own gd is also the spill allowance next to them, so despill leaves them alone.
* Magenta is keyed by ms = min(R, B) - G with R and B balanced (the magenta
  hue family: fill, dark outline, face marks, shading painted on the mannequin).
  Wine reds (R >> B) are not in the family.
* Near-black pixels are ambiguous (the mannequin's dark outline and an item's
  outline have the same colours). They are decided by what they touch and by
  their warmth: a dark pixel is mask when magenta is within 3 px, no item fill
  (and no thick dark area, such as a black wig) is near, and it is not warm
  (item outlines and cords are dark brown) or sits on the mannequin's silhouette.
* Soft alpha: alpha = (k_key - k_pixel) / (k_key - k_item), along the key's own
  difference axis, where k_item is the most key-free item colour within 3 px.
* Despill: gd is clamped to the local legitimate gd (of deep item pixels within
  6 px, 0 for non-green items) and the magenta excess is removed; band pixels
  take the nearest solid item colour.
"""

import numpy as np

from . import imgops as io

GREEN_KEY = 0.3  # gd / gd_bg at or above which a pixel is background green (bottle green is ~0.1, jade ~0.08)
MAG_MIN = 25.0  # min(R, B) - G at or above which a balanced pixel is magenta-family
DARK_MAX = 110.0  # max channel below which a pixel is "dark" (outline class)


def _stats(rgb):
    border = np.concatenate([rgb[:20].reshape(-1, 3), rgb[-20:].reshape(-1, 3),
                             rgb[:, :20].reshape(-1, 3), rgb[:, -20:].reshape(-1, 3)])
    bg = np.median(border, axis=0)
    return bg


def key(rgb, mannequin):
    """Keys one raw image.

    Returns dict: alpha (0..1), rgb (decontaminated, straight), the key
    masks green / magenta (for gap detection) and classes (per pixel: 0 green,
    1 magenta, 2 dark mask, 3 dark item, 4 item fill, 5 dropped speck; shown
    on the QA keying sheet).
    """
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    bg = _stats(rgb)
    gd = g - np.maximum(r, b)
    gd_bg = float(bg[1] - max(bg[0], bg[2]))
    green = gd >= GREEN_KEY * gd_bg

    mx = np.maximum(r, b)
    mn = np.minimum(r, b)
    ms = mn - g
    if mannequin:
        magenta = (ms >= MAG_MIN) & (mn >= 0.5 * mx) & (mx >= 40) & ~green
        strong = magenta & (ms >= 150)
        mg = np.median(rgb[strong], axis=0) if strong.any() else np.array([254.0, 3.0, 253.0])
        # Dark pixels with a magenta tint belong to the mannequin's outline / face marks.
        tinted = (r - g >= 8) & (b - g >= 8) & (mn >= 0.45 * mx) & ~green & ~magenta
        magenta |= tinted & (np.maximum(np.maximum(r, g), b) < DARK_MAX)
        # "Dark" also covers a dark colour blended with the green (the outline's
        # anti-aliased rim): unmix the green share first, then test darkness.
        lam = np.clip(gd / gd_bg, 0.0, 0.9)[..., None]
        unmixed = (rgb - lam * bg[None, None, :]) / (1.0 - lam)
        dark = (unmixed.max(axis=2) < DARK_MAX) & ~green & ~magenta
        fill = ~green & ~magenta & ~dark
        # Thick dark areas are item (a black wig, a dark bottle-green panel), never outline.
        strict = (np.maximum(np.maximum(r, g), b) < DARK_MAX) & (gd < 45) & dark
        thick = io.dilate(io.erode(strict, 3), 3) & dark
        near_item = io.dilate(fill, 2) | io.dilate(thick, 3)
        near_mag = io.dilate(magenta, 3)
        # Item outlines and thin dark items (cords, straps) are warm dark brown
        # (blue lowest); the mannequin's marks are magenta-tinted (green lowest).
        # A warm dark pixel is item unless it sits on the mannequin's silhouette
        # (touching both the green and the magenta) with no item fill beside it.
        du = unmixed
        warm = (du[..., 0] - du[..., 2] >= 12) & (du[..., 1] - du[..., 2] >= 3)
        silhouette = io.dilate(green, 2) & near_mag
        dark_item = dark & (near_item | (warm & ~silhouette) | ~near_mag)
        dark_mask = dark & ~dark_item
        # Where magenta meets green with no outline between them, the one-pixel blend
        # of the two is a greyish colour: mask, when it lies on such a boundary.
        span = bg - mg
        beta = np.clip(((rgb - mg) @ span) / float(span @ span), 0.0, 1.0)
        resid = np.linalg.norm(rgb - (mg + beta[..., None] * span), axis=2)
        blend = (resid < 32) & (beta > 0.1) & (beta < 0.9) & io.dilate(green, 2) & io.dilate(magenta, 2) & fill
        dark_mask |= blend
        mask = green | magenta | dark_mask
        classes = np.select([green, magenta, dark_mask, dark], [0, 1, 2, 3], 4).astype(np.uint8)
    else:
        magenta = np.zeros_like(green)
        mg = np.array([254.0, 3.0, 253.0])
        mask = green.copy()
        classes = np.where(green, 0, 4).astype(np.uint8)

    item = ~mask
    # Item specks are noise from the mask's own edges: tiny ones anywhere, and
    # small ones (under 40 px) that lie away (over 8 px) from any real part.
    labels, n = io.label(item)
    if n:
        sizes = np.bincount(labels.ravel())
        big = sizes >= 40
        big[0] = False
        near_big = io.dilate(big[labels], 8)
        touched = np.zeros(n + 1, bool)
        touched[np.unique(labels[near_big])] = True
        small = (sizes < 12) | (~big & ~touched)
        small[0] = False
        classes[small[labels]] = 5
        item &= ~small[labels]
        mask = ~item

    # --- soft alpha in the band where item meets a key colour ---
    inner = io.erode(item, 1)
    gd_item = io.local_min(gd, inner, 3)
    gd_item = np.where(np.isfinite(gd_item), gd_item, io.local_min(gd, item, 4))
    ms_item = io.local_min(ms, inner, 3)
    ms_item = np.where(np.isfinite(ms_item), ms_item, io.local_min(ms, item, 4))

    near_green = io.dilate(green, 2)
    near_magenta = io.dilate(magenta, 2) & ~near_green if mannequin else np.zeros_like(green)
    touches_item = io.dilate(item, 1)
    band = touches_item & (io.dilate(green | magenta, 2))

    alpha = item.astype(np.float32)
    ms_bg = float(min(mg[0], mg[2]) - mg[1])
    with np.errstate(invalid="ignore", divide="ignore"):
        a_g = (gd_bg - gd) / np.maximum(gd_bg - gd_item, 1.0)
        a_m = (ms_bg - ms) / np.maximum(ms_bg - ms_item, 1.0)
    a_g = np.clip(np.nan_to_num(a_g, nan=0.0, posinf=0.0, neginf=0.0), 0, 1)
    a_m = np.clip(np.nan_to_num(a_m, nan=0.0, posinf=0.0, neginf=0.0), 0, 1)
    soft = np.where(near_green, a_g, np.where(near_magenta, a_m, 1.0))
    has_item = np.isfinite(io.local_min(gd, item, 2))
    band &= has_item
    # Item pixels in the band keep at least a little coverage; key pixels only the mixed part.
    alpha = np.where(band & item, np.maximum(soft, 0.15), alpha)
    alpha = np.where(band & (green | magenta) & touches_item, soft, alpha)
    alpha = np.where(alpha < 0.04, 0.0, alpha)

    # --- decontaminate ---
    # 1. Despill every visible pixel near a key: green down to the local legitimate
    #    green (of deep item pixels within 6 px: a bottle-green gown keeps its green),
    #    magenta removed outright (no item is legitimately magenta).
    deep = io.erode(item, 4)
    allow_g = io.local_max(np.maximum(gd, 0), deep, 6)
    allow_g = np.where(np.isfinite(allow_g), allow_g, 0.0)
    orr, og, ob = rgb[..., 0].copy(), rgb[..., 1].copy(), rgb[..., 2].copy()
    rim = io.dilate(green, 6) & (alpha > 0)
    g_cap = np.maximum(orr, ob) + allow_g
    og = np.where(rim & (og > g_cap), g_cap, og)
    if mannequin:
        mrim = io.dilate(magenta, 6) & (alpha > 0)
        excess = np.minimum(orr, ob) - og
        cut = np.where(mrim & (excess > 0), excess, 0.0)
        orr = orr - cut
        ob = ob - cut
    clean = np.dstack([orr, og, ob])
    # 2. The soft band takes the colour of the nearest solid item pixel (unmixing the
    #    key out algebraically amplifies noise into tinted outlines); mostly-covered
    #    band pixels keep more of their own despilled colour.
    solid = item & (alpha >= 0.999)
    near, _ = io.bleed(np.where(solid[..., None], clean, 0), solid, 4)
    partial = (alpha > 0) & (alpha < 0.999)
    own = np.clip((alpha - 0.5) / 0.3, 0.0, 1.0)[..., None]
    out = np.where(partial[..., None], own * clean + (1 - own) * near, clean)
    out = np.clip(out, 0, 255)

    # Transparent pixels carry the nearest item colour (no dark or green halo under filtering).
    visible = alpha > 0
    filled, _ = io.bleed(np.where(visible[..., None], out, 0), visible, 8)
    out = np.where(visible[..., None], out, filled)
    return {"alpha": alpha, "rgb": out, "green": green, "magenta": magenta, "classes": classes}
