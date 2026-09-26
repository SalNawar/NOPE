"""QA without Unity: composites and measurements of the processed pilot.

Writes into the QA folder:
  stack_*.png           full-canvas stacks (half size) in LookLayer order
  stacks_sheet.png      every stack side by side, headroom to waist
  desk_1080.png         the stacks at the office's size at 1920 x 1080 (head ~58 px), headroom to waist
  desk_720.png          the same at 1280 x 720 (head ~39 px); *_x3.png: the same pixels enlarged 3x
  passport.png          the passport crop (LookCanvas photo rect) at its in-game sizes and full size
  keying_sheet.png      before/after keying of every raw source (raw, keyed on checker, on dark, alpha, pixel classes)
  edges_worst.png       for every source, its worst edge windows at 4x over black and white
  hair_colours_and_skins.png  the five baked hair colours and the five body skin tones
and returns the numbers (qa.json): the alpha-edge fringe check per key.
"""

import os

import numpy as np
from PIL import Image, ImageDraw

from . import imgops as io

LAYER_ORDER = ("hairback", "body", "outfit", "head", "facialhair", "hair", "headwear", "accessory")

STACKS = [
    ("athens_m", "Athens man (Periclean, full look)",
     ["body_m_skin1", "outfit_m_greece_ancient", "head_m_skin1_facea", "facialhair_m_greece_ancient_brown",
      "hair_m_greece_ancient_brown", "headwear_m_greece_ancient", "accessory_m_greece_ancient"]),
    ("athens_m_nohat", "Athens man without the petasos",
     ["body_m_skin1", "outfit_m_greece_ancient", "head_m_skin1_facea", "facialhair_m_greece_ancient_brown",
      "hair_m_greece_ancient_brown", "accessory_m_greece_ancient"]),
    ("athens_f", "Athens woman (full look)",
     ["body_f_skin1", "outfit_f_greece_ancient", "head_f_skin1_facea", "hair_f_greece_ancient_brown",
      "headwear_f_greece_ancient"]),
    ("athens_f_nohat", "Athens woman without the sakkos",
     ["body_f_skin1", "outfit_f_greece_ancient", "head_f_skin1_facea", "hair_f_greece_ancient_brown"]),
    ("mamluk_f", "Mamluk woman: izar hood (covers the hair, so no hair layer)",
     ["body_f_skin1", "outfit_f_egypt_medieval", "head_f_skin1_facea", "headwear_f_egypt_medieval"]),
    ("mamluk_f_hair", "Mamluk woman's pinned braids (hood off)",
     ["body_f_skin1", "outfit_f_egypt_medieval", "head_f_skin1_facea", "hair_f_egypt_medieval_brown"]),
    ("mamluk_f_sakkos", "Leak: sakkos over the Mamluk braids",
     ["body_f_skin1", "outfit_f_egypt_medieval", "head_f_skin1_facea", "hair_f_egypt_medieval_brown",
      "headwear_f_greece_ancient"]),
    ("athens_f_hood", "Leak: izar hood on the Athens woman",
     ["body_f_skin1", "outfit_f_greece_ancient", "head_f_skin1_facea", "headwear_f_egypt_medieval"]),
    ("thebes_f_wig", "Tripartite wig + hair back on the base",
     ["hairback_f_egypt_ancient", "body_f_skin1", "head_f_skin1_facea", "hair_f_egypt_ancient"]),
    ("thebes_f_wig_chiton", "Wig over a garment (the lappets in front)",
     ["hairback_f_egypt_ancient", "body_f_skin1", "outfit_f_greece_ancient", "head_f_skin1_facea", "hair_f_egypt_ancient"]),
    ("kofun_m", "Magatama necklace on the man",
     ["body_m_skin1", "head_m_skin1_facea", "accessory_m_japan_ancient"]),
    ("kofun_m_chiton", "Magatama necklace over the Athens outfit",
     ["body_m_skin1", "outfit_m_greece_ancient", "head_m_skin1_facea", "facialhair_m_greece_ancient_brown",
      "hair_m_greece_ancient_brown", "accessory_m_japan_ancient"]),
]

ROOM = (118, 110, 100)  # a neutral warm grey, about the office wall behind the desk

# keying classes (keying.key): green, magenta, dark mask, dark item, item fill, dropped speck
CLASS_COLOURS = np.array([[0, 190, 0], [255, 0, 255], [0, 0, 255], [0, 0, 0], [255, 255, 255], [255, 160, 0]], np.float32)


def _layer_rank(key):
    return LAYER_ORDER.index(key.split("_")[0])


def composite(p, keys, bg=ROOM):
    keys = sorted(keys, key=_layer_rank)
    out = np.zeros((p.size[1], p.size[0], 3), np.float32) + np.array(bg, np.float32)
    for k in keys:
        rgb, a = p.canvas_layers[k]
        out = rgb * a[..., None] + out * (1 - a[..., None])
    return out


def to_img(a):
    return Image.fromarray(np.uint8(np.clip(np.round(a), 0, 255)))


def _label(img, text, fill=(0, 0, 0)):
    d = ImageDraw.Draw(img)
    d.text((4, 2), text, fill=fill)
    return img


def checker(h, w, n=16):
    yy, xx = np.mgrid[0:h, 0:w]
    c = ((yy // n + xx // n) % 2).astype(np.float32)
    return np.dstack([200 + 40 * c] * 3)


def resize(img, scale):
    return img.resize((max(1, round(img.width * scale)), max(1, round(img.height * scale))), Image.LANCZOS)


def edge_check(rgb, alpha):
    """Fringe numbers of one layer (see README 'QA')."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    gd = g - np.maximum(r, b)
    ms = np.minimum(r, b) - g
    clear = alpha < 0.02
    # edge pixels a viewer can see: at least a quarter covered, next to transparency
    edge = ((alpha >= 0.25) & (alpha < 0.98)) | ((alpha >= 0.98) & io.dilate(clear, 1))
    solid = io.erode(alpha >= 0.98, 1)
    legit = io.local_max(np.maximum(gd, 0), solid, 12)
    legit = np.where(np.isfinite(legit), legit, 0.0)
    green = edge & (gd > legit + 12)
    # magenta-hued: R and B both above G and balanced (a madder red or a wine red is not magenta)
    mag = edge & (ms > 12) & (np.minimum(r, b) >= 0.6 * np.maximum(r, b))
    visible = alpha >= 0.1
    key_green = visible & (gd > 100)
    key_mag = visible & (ms > 100)
    n = int(edge.sum())
    return {
        "edge_px": n,
        "green_fringe_px": int(green.sum()), "green_fringe_share": round(float(green.sum()) / max(n, 1), 5),
        "green_excess_max": round(float((gd - legit)[edge].max()) if n else 0.0, 1),
        "magenta_fringe_px": int(mag.sum()), "magenta_fringe_share": round(float(mag.sum()) / max(n, 1), 5),
        "magenta_max": round(float(ms[edge].max()) if n else 0.0, 1),
        "key_colour_px": int(key_green.sum() + key_mag.sum()),
    }, np.maximum(np.where(edge, gd - legit, -999), np.where(edge, ms, -999))


def run(p, qa_dir):
    results = {"edges": {}, "stacks": {}}
    have = set(p.canvas_layers)

    # --- full stacks -------------------------------------------------------------------------
    stacks = []
    for name, title, keys in STACKS:
        missing = [k for k in keys if k not in have]
        if missing:
            results["stacks"][name] = {"missing": missing}
            continue
        img = composite(p, keys)
        to_img(img).save(os.path.join(qa_dir, f"stack_{name}_full.png"))
        stacks.append((name, title, img))
        results["stacks"][name] = {"layers": sorted(keys, key=_layer_rank)}

    # sheet: headroom to waist at half size
    tiles = [_label(resize(to_img(img[120:800, 212:812]), 0.5), t[:44], (255, 255, 255)) for _, t, img in stacks]
    cols = 6
    tw, th = tiles[0].size
    sheet = Image.new("RGB", (cols * tw, ((len(tiles) + cols - 1) // cols) * th), ROOM)
    for i, t in enumerate(tiles):
        sheet.paste(t, ((i % cols) * tw, (i // cols) * th))
    sheet.save(os.path.join(qa_dir, "stacks_sheet.png"))

    # --- desk size ------------------------------------------------------------------------------
    head_px = p.C["Chin"] - p.C["HeadTop"]
    for label, head in (("1080", 58), ("720", 39)):
        s = head / head_px
        # headroom to the waist (the NEXT sign hides the rest), the figure's middle 480 px
        crops = [resize(to_img(img[120:p.C["Waist"], 272:752]), s) for _, _, img in stacks]
        cols = 6
        cw, ch = crops[0].size
        frame = Image.new("RGB", (cols * cw, 14 + ((len(crops) + cols - 1) // cols) * ch), ROOM)
        for i, c in enumerate(crops):
            frame.paste(c, ((i % cols) * cw, 14 + (i // cols) * ch))
        _label(frame, f"{label}p: head {head} px", (255, 255, 255))
        frame.save(os.path.join(qa_dir, f"desk_{label}.png"))
        # the same pixels enlarged 3x (nearest), to read what the player gets
        frame.resize((frame.width * 3, frame.height * 3), Image.NEAREST).save(os.path.join(qa_dir, f"desk_{label}_x3.png"))
    results["desk_scale_1080"] = round(58 / head_px, 4)

    # --- passport crop --------------------------------------------------------------------------
    L, T, R, B = p.C["PhotoLeft"], p.C["PhotoTop"], p.C["PhotoRight"], p.C["PhotoBottom"]
    rows = []
    for name, title, img in stacks:
        crop = to_img(composite(p, [k for k in STACKS[[s[0] for s in STACKS].index(name)][2]], bg=(206, 200, 186))[T:B, L:R])
        sizes = [(48, 60), (60, 75), (95, 119), (134, 167)]
        parts = [crop.resize(sz, Image.LANCZOS) for sz in sizes] + [crop]
        rows.append(parts)
    rw = sum(pt.width for pt in rows[0]) + 10 * len(rows[0])
    rh = rows[0][-1].height + 6
    half = (len(rows) + 1) // 2
    pas = Image.new("RGB", (2 * rw, rh * half), (60, 60, 60))
    for i, parts in enumerate(rows):
        x = (i // half) * rw
        for pt in parts:
            pas.paste(pt, (x, (i % half) * rh + (rh - 6 - pt.height)))
            x += pt.width + 10
    pas.save(os.path.join(qa_dir, "passport.png"))
    results["passport_rect"] = [L, T, R, B]

    # --- keying contact sheet -------------------------------------------------------------------
    tiles = []
    for raw, (rgb, k) in p.keyed.items():
        ys, xs = np.nonzero(k["alpha"] > 0)  # the kept item, not the whole mannequin
        y0, y1, x0, x1 = max(ys.min() - 30, 0), min(ys.max() + 30, rgb.shape[0]), max(xs.min() - 30, 0), min(xs.max() + 30, rgb.shape[1])
        a = k["alpha"][y0:y1, x0:x1]
        kc = k["rgb"][y0:y1, x0:x1]
        chk = checker(*a.shape)
        over = kc * a[..., None] + chk * (1 - a[..., None])
        dark = kc * a[..., None] + np.array([25, 25, 32], np.float32) * (1 - a[..., None])
        cls = CLASS_COLOURS[k["classes"][y0:y1, x0:x1]]
        panel = np.concatenate([rgb[y0:y1, x0:x1], over, dark, np.dstack([a * 255] * 3), cls], axis=1)
        im = to_img(panel)
        im = im.resize((round(im.width * 300 / im.height), 300), Image.LANCZOS)
        tiles.append(_label(im, raw + ": raw | keyed | on dark | alpha | classes"))
    cols = 3
    tw = max(t.width for t in tiles)
    sheet = Image.new("RGB", (cols * tw, ((len(tiles) + cols - 1) // cols) * 300), (255, 255, 255))
    for i, t in enumerate(tiles):
        sheet.paste(t, ((i % cols) * tw, (i // cols) * 300))
    sheet.save(os.path.join(qa_dir, "keying_sheet.png"))

    # --- alpha-edge check -----------------------------------------------------------------------
    worst_tiles = []
    seen_sources = set()
    for key, (rgb, a) in p.canvas_layers.items():
        stats, badness = edge_check(rgb, a)
        results["edges"][key] = stats
        src = p.report["keys"][key]["source"]
        if src in seen_sources or key.startswith("body_") and not key.endswith("skin1"):
            continue
        seen_sources.add(src)
        # three worst 48 px windows of this layer, 4x, over black and white
        sm = np.maximum(badness, 0)
        wins = []
        for _ in range(3):
            iy, ix = np.unravel_index(np.argmax(sm), sm.shape)
            y0 = int(np.clip(iy - 24, 0, a.shape[0] - 48))
            x0 = int(np.clip(ix - 24, 0, a.shape[1] - 48))
            wins.append((y0, x0))
            sm[max(iy - 60, 0):iy + 60, max(ix - 60, 0):ix + 60] = 0
        row = []
        for y0, x0 in wins:
            c = rgb[y0:y0 + 48, x0:x0 + 48]
            al = a[y0:y0 + 48, x0:x0 + 48, None]
            for bgc in ((0, 0, 0), (255, 255, 255)):
                t = to_img(c * al + np.array(bgc, np.float32) * (1 - al)).resize((192, 192), Image.NEAREST)
                row.append(t)
        strip = Image.new("RGB", (len(row) * 196, 206), (128, 128, 128))
        for i, t in enumerate(row):
            strip.paste(t, (i * 196, 14))
        _label(strip, f"{key}: worst edge windows (black / white)")
        worst_tiles.append(strip)
    ew = Image.new("RGB", (worst_tiles[0].width, 206 * len(worst_tiles)), (128, 128, 128))
    for i, t in enumerate(worst_tiles):
        ew.paste(t, (0, i * 206))
    ew.save(os.path.join(qa_dir, "edges_worst.png"))

    # --- baked variants -------------------------------------------------------------------------
    var = []
    for base in ("hair_m_greece_ancient", "facialhair_m_greece_ancient", "hair_f_greece_ancient", "hair_f_egypt_medieval"):
        g = base.split("_")[1]
        for colour, _ in p.hair:
            keys = [f"body_{g}_skin1", f"head_{g}_skin1_facea", f"{base}_{colour}"]
            img = composite(p, keys)[200:520, 352:672]
            var.append(to_img(img))
    for g in ("m", "f"):
        for tone in range(1, 6):
            img = composite(p, [f"body_{g}_skin{tone}", f"head_{g}_skin1_facea"])[380:700, 352:672]
            var.append(to_img(img))
    vs = Image.new("RGB", (5 * 320, (len(var) // 5) * 320), ROOM)
    for i, t in enumerate(var):
        vs.paste(t, ((i % 5) * 320, (i // 5) * 320))
    vs.save(os.path.join(qa_dir, "hair_colours_and_skins.png"))
    return results
