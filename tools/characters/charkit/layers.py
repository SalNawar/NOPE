"""Layer surgery on registered canvas images: head/body split, skin and hair
colour baking, the hair-back split and mannequin-gap sealing.

All functions take and return canvas-space arrays: rgb (H, W, 3) 0..255 and
alpha (H, W) 0..1, straight (not premultiplied).
"""

import numpy as np

from . import imgops as io

# --- colour helpers ------------------------------------------------------------------------


def to_linear(c):
    c = np.asarray(c, np.float32) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(lin):
    lin = np.clip(lin, 0.0, 1.0)
    return 255.0 * np.where(lin <= 0.0031308, lin * 12.92, 1.055 * lin ** (1 / 2.4) - 0.055)


def hsv(rgb):
    """Hue (degrees), saturation, value (0..1) of 0..255 RGB."""
    c = rgb / 255.0
    mx = c.max(-1)
    mn = c.min(-1)
    d = mx - mn
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    with np.errstate(invalid="ignore", divide="ignore"):
        h = np.where(mx == r, (g - b) / d % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60.0
        s = np.where(mx > 0, d / mx, 0.0)
    return np.nan_to_num(h), s, mx


def dominant(rgb, sel, q=6):
    """The most common colour among selected pixels (quantised to q levels, then averaged within the bin)."""
    px = rgb[sel]
    keys = (px // q).astype(np.int32)
    k = keys[:, 0] * 65536 + keys[:, 1] * 256 + keys[:, 2]
    vals, inv, counts = np.unique(k, return_inverse=True, return_counts=True)
    best = np.argmax(counts)
    return px[inv == best].mean(axis=0)


def hue_dist(h, h0):
    d = np.abs(h - h0) % 360.0
    return np.minimum(d, 360.0 - d)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


# --- base figure ---------------------------------------------------------------------------


def jaw_line(rgb, alpha, chin_y, cx):
    """Neck columns and the jaw line over them.

    Returns (nl, nr, jaw) with jaw[x - nl] the last row of the jaw outline
    in that column: the lowest short dark run between 45 px above the chin
    and 6 px below. A column whose lowest dark run is long is the neck's own
    side outline, not the jaw: it takes the nearest jaw column's value.
    """
    lefts, rights = [], []
    for y in range(int(chin_y + 2), int(chin_y + 15)):
        xs = np.nonzero(alpha[y] > 0.5)[0]
        runs = np.split(xs, np.nonzero(np.diff(xs) > 1)[0] + 1)
        neck = next(r for r in runs if r[0] <= cx <= r[-1])
        lefts.append(neck[0])
        rights.append(neck[-1])
    # the neck's widest extent just under the chin; its side outlines stay with the neck
    nl, nr = int(min(lefts)) - 3, int(max(rights)) + 3
    dark = (io.lum(rgb) < 110) & (alpha > 0.5)
    ya, yb = int(chin_y - 45), int(chin_y + 6)
    jaw = np.full(nr - nl + 1, -1)
    for i, x in enumerate(range(nl, nr + 1)):
        ys = np.nonzero(dark[ya:yb, x])[0]
        if not len(ys):
            continue
        breaks = np.nonzero(np.diff(ys) > 1)[0]
        start = ys[breaks[-1] + 1] if len(breaks) else ys[0]
        if ys[-1] - start <= 8:
            jaw[i] = ya + ys[-1]
    valid = np.nonzero(jaw >= 0)[0]
    if not len(valid):
        return nl, nr, np.full(nr - nl + 1, int(chin_y))
    for i in np.nonzero(jaw < 0)[0]:
        jaw[i] = jaw[valid[np.argmin(np.abs(valid - i))]]
    # a broken piece of the neck's side outline can still pass as a short run: the jaw is
    # smooth, so a column far below its neighbours' median is pulled up to it
    pad = np.pad(jaw, 3, mode="edge")
    med = np.array([np.median(pad[i:i + 7]) for i in range(len(jaw))])
    jaw = np.minimum(jaw, (med + 2).astype(int))
    return nl, nr, jaw


def split_head_body(rgb, alpha, chin_y, cx):
    """Splits a registered base figure into (head_alpha, body_rgb, body_alpha).

    Head: everything above the neck line outside the neck columns (skull,
    ears, jaw) and, over the neck, everything down to the jaw outline (no
    lighter row below it, which would show on a darker body). Body: the
    rest, and the neck continued upwards behind the face to 40 px above the
    chin (hidden by the head) by repeating the neck row just under the jaw,
    so a head of another face or skin tone never shows a gap at the chin.
    """
    h, w = alpha.shape
    nl, nr, jaw = jaw_line(rgb, alpha, chin_y, cx)
    yy, xx = np.mgrid[0:h, 0:w]
    neck_top = int(chin_y + 10)
    head = (yy < neck_top) & ((xx < nl) | (xx > nr))
    in_neck = (xx >= nl) & (xx <= nr)
    jaw_full = np.full(w, -1)
    jaw_full[nl:nr + 1] = jaw
    head |= in_neck & (yy <= jaw_full[None, :])
    head_alpha = alpha * head

    body_alpha = alpha * ~head
    body_rgb = rgb.copy()
    y_ref = int(jaw.max() + 4)
    y_ext = int(chin_y - 40)
    for x in range(nl, nr + 1):
        if y_ref >= h or alpha[y_ref, x] <= 0:
            continue
        body_rgb[y_ext:jaw_full[x] + 1, x] = rgb[y_ref, x]
        body_alpha[y_ext:jaw_full[x] + 1, x] = alpha[y_ref, x]
    return head_alpha, body_rgb, body_alpha


def skin_weight(rgb, alpha, skin):
    """How much each pixel is skin (0..1): near the skin's hue, not grey, not an outline."""
    h, s, v = hsv(rgb)
    hs, ss, vs = hsv(np.asarray(skin, np.float32)[None, None, :])
    hs, ss, vs = float(hs[0, 0]), float(ss[0, 0]), float(vs[0, 0])
    w_h = 1.0 - smoothstep(18.0, 30.0, hue_dist(h, hs))
    w_s = smoothstep(0.35 * ss, 0.6 * ss, s)
    w_v = smoothstep(0.35 * vs, 0.55 * vs, v)
    return w_h * w_s * w_v * (alpha > 0)


def recolour(rgb, weight, src, dst):
    """Moves src colour to dst (per channel, in linear light, so shades keep their ratio), by weight."""
    ratio = to_linear(dst) / np.maximum(to_linear(src), 1e-4)
    moved = to_srgb(to_linear(rgb) * ratio[None, None, :])
    return rgb + (moved - rgb) * weight[..., None]


def skin_colour(rgb, alpha):
    """The base's skin fill colour: the dominant warm, fairly light colour of the figure."""
    h, s, v = hsv(rgb)
    sel = (alpha > 0.99) & (v > 0.6) & (s > 0.12) & (s < 0.5) & (h > 5) & (h < 45)
    return dominant(rgb, sel)


# --- hair ----------------------------------------------------------------------------------


def hair_colour(rgb, alpha):
    """The drawing's hair fill colour: the dominant saturated mid-tone of the layer."""
    h, s, v = hsv(rgb)
    sel = (alpha > 0.99) & (s > 0.2) & (v > 0.3) & (v < 0.85)
    return dominant(rgb, sel)


def luminance(lin):
    return lin[..., 0] * 0.2126 + lin[..., 1] * 0.7152 + lin[..., 2] * 0.0722


def bake_hair(rgb, alpha, src, target, keep_outline):
    """One baked hair colour.

    A hair pixel (hue within about 30 degrees of the drawing's brown, and
    saturated) becomes the target colour scaled by the pixel's luminance
    relative to the drawing's fill colour, in linear light: the shade and the
    highlight keep their brightness ratios and take the target's hue exactly
    (scaling each channel separately would amplify the brown's weak blue into
    purple fringes on grey and blond). Anything else (ties, pins, nets in
    white, silver, blue or gold) keeps its colour: ornaments are masked by
    hue. With keep_outline the dark outline (under about half the fill's
    brightness) keeps the brief's dark brown; for black hair the outline is
    moved too, so it stays darker than the fill.
    """
    h, s, v = hsv(rgb)
    hs, ss, vs = [float(x[0, 0]) for x in hsv(np.asarray(src, np.float32)[None, None, :])]
    w = (1.0 - smoothstep(22.0, 34.0, hue_dist(h, hs))) * smoothstep(0.08, 0.16, s)
    if keep_outline:
        w = w * smoothstep(0.42 * vs, 0.60 * vs, v)
    else:
        # near-neutral dark pixels (outline anti-aliasing) follow when the outline moves
        w = np.maximum(w, 1.0 - smoothstep(0.40 * vs, 0.55 * vs, v))
    lin = to_linear(rgb)
    ratio = luminance(lin) / max(float(luminance(to_linear(src)[None, :])[0]), 1e-4)
    moved = to_srgb(to_linear(target)[None, None, :] * ratio[..., None])
    w = w * (alpha > 0)
    return rgb + (moved - rgb) * w[..., None]


def split_hairback(rgb, alpha, chin_y, cx, face_half_width, step_px=20):
    """Splits a long hairstyle into (front_alpha, back_alpha).

    The front view shows the rear hair only where it hangs outside and
    shorter than the front locks (the lappets of a wig, the locks over the
    shoulders): from each outer edge inwards, the columns whose hair ends at
    least step_px above the front locks' lowest point are rear hair; their
    pixels below the chin move to the hair-back layer (drawn behind the body).
    """
    h, w = alpha.shape
    solid = alpha > 0.5
    ys = np.arange(h)[:, None]
    bottom = np.where(solid.any(0), (solid * ys).max(0), -1)
    front_bottom = bottom.max()
    rear_cols = np.zeros(w, bool)
    cols = np.nonzero(solid.any(0))[0]
    for side_cols in (cols, cols[::-1]):
        for x in side_cols:
            if abs(x - cx) <= face_half_width:
                break
            if bottom[x] >= front_bottom - step_px:
                break
            rear_cols[x] = True
    back = rear_cols[None, :] & (ys > chin_y)
    return alpha * ~back, alpha * back


# --- sealing mannequin gaps ------------------------------------------------------------------


def seal_gaps(rgb, alpha, magenta_w, green_w, base_alpha):
    """Fills holes left by a mannequin that does not match the base figure.

    A hole is where the source showed magenta (the mannequin, which the base
    figure replaces) but the registered base figure has nothing, in a region
    enclosed by the item (not touching the source's green background): it
    would show the room through the item. Holes are filled with the item's
    nearest colour, fully opaque. Returns (rgb, alpha, filled_px).
    """
    hole = (magenta_w > 0.5) & (base_alpha < 0.5) & (alpha < 0.5)
    labels, n = io.label(hole)
    if not n:
        return rgb, alpha, 0
    touching = np.unique(labels[io.dilate(green_w > 0.5, 2) & hole])
    enclosed = np.ones(n + 1, bool)
    enclosed[0] = False
    enclosed[touching] = False
    fill = enclosed[labels]
    if not fill.any():
        return rgb, alpha, 0
    opaque = alpha > 0.9
    grown, _ = io.bleed(np.where(opaque[..., None], rgb, 0), opaque, 40)
    rgb = np.where(fill[..., None], grown, rgb)
    alpha = np.where(fill, 1.0, alpha)
    return rgb, alpha, int(fill.sum())
