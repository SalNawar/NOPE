"""Pet coats (the Home pet spec PS11): keys the Canva pet art's magenta background and paints the white coat into five
coats per animal, then installs them as the game's art slots, each with its closed-eye blink picture.

The base art (src/pet_<kind>_<mood>.png, made in Canva for run 7) is a white-coated dog and cat in four moods each
(idle, happy, sad, sick), drawn in the same pose so the game can swap them in place. Every coat is painted over the same
pixels, so all coats and moods line up, and the line art, the eyes, the nose and the blanket stay as drawn.

v3 (Saleh 2026-10-17: "dog and cat coats are weird"): a coat is no longer a flat two-colour tint. The white fur's own
cel shading (its luminance: the lit fur, its shadow, the hatching's deep tone and the sheen) is mapped through a
four-tone ramp per coat (deep, shadow, base, highlight; shadows lean violet as the game's cel style does), and the
markings come from the rigs' marks (Assets/Art/UI/Resources/Home/pet_rigs.json): pale fur (a tan dog's cream muzzle,
chest and paws, a charcoal dog's white blaze, a ginger cat's cream chin and chest) with a fur-ragged edge, a dog's
inner ear, and tabby stripes for the ginger and cream cats (bands across the flank, the legs, the tail and the brow,
their wobble from a seeded noise). A black cat's highlights lean blue (its sheen).

The blink (the pets' animation, PetAnimator): for each mood whose eyes are open, pet_<kind>_<coat>_<mood>_blink.png
is the picture's eyes shut: each eye's ellipse (the rig's eyes) filled with the fur around it (a normalized
convolution of the fur outside) and a closed lid drawn across it in the line art's ink; transparent elsewhere, so the
game lays it over the coat's picture for a blink.

Output: Assets/Art/UI/Resources/Home/pet_<kind>_<coat>_<mood>.png (1024x1024, transparent; ArtSlots.PetSprite) and
the blinks (ArtSlots.PetBlink), each with a .meta whose GUID is fixed (derived from the file's name), so a regenerated
file keeps its GUID. Unity fills in the importer's settings on import (ArtSlotImporter). The coats' ids must match
world_source.json home.pet.kinds[].coats.

Needs numpy, Pillow and scipy.

Usage: python tools/art/pets/make_pets.py [--sheet <contact sheet .png>] [--old <folder of the previous coats>]
"""
import hashlib
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "src")
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(REPO, "Assets", "Art", "UI", "Resources", "Home")
RIGS = os.path.join(OUT, "pet_rigs.json")
MOODS = ["idle", "happy", "sad", "sick"]
SIZE = 1024
LUMA = np.array([0.299, 0.587, 0.114], dtype=np.float32)
MAX_HIGHLIGHT_PIXELS = 500  # at 1024x1024: eye highlights are under 300 px (a far leg's fur is ~2,600)
HIGHLIGHT_RING_LUM = 0.36  # an eye highlight's ring (iris, pupil) is darker than this; a fur speck's is lighter
STOPS = {"dog": [0.58, 0.78, 0.94, 0.985], "cat": [0.55, 0.73, 0.96, 0.995]}  # the white fur's tones: hatching, cel shadow, lit fur, sheen
INK = (58, 38, 46)  # the closed lid's line: the art's brown-violet ink

# A coat: ramps of four colours (deep, shadow, base, highlight) at its kind's STOPS for the fur, the pale marks and the stripes
# (with the stripes' strength), and the inner ear's colour. None: the art's own white, untouched (a white cat).
COATS = {
    "dog": {
        "cream": dict(fur=[(150, 120, 112), (214, 186, 162), (246, 230, 198), (255, 248, 228)],
                      pale=[(178, 160, 160), (228, 214, 208), (255, 250, 240), (255, 255, 250)], inner=(222, 168, 150)),
        "tan": dict(fur=[(112, 62, 56), (168, 102, 74), (218, 158, 94), (240, 194, 132)],
                    pale=[(170, 140, 134), (222, 196, 176), (250, 236, 210), (255, 248, 232)], inner=(196, 124, 108)),
        "chocolate": dict(fur=[(46, 24, 30), (78, 44, 42), (118, 72, 48), (152, 102, 68)],
                          pale=[(84, 52, 46), (122, 82, 64), (162, 114, 80), (188, 140, 100)], inner=(150, 92, 84)),
        "charcoal": dict(fur=[(24, 22, 34), (42, 40, 56), (70, 68, 82), (106, 106, 124)],
                         pale=[(150, 146, 164), (200, 198, 212), (242, 240, 236), (255, 255, 252)], inner=(146, 106, 114)),
        "grey": dict(fur=[(70, 70, 92), (110, 114, 138), (158, 164, 178), (194, 200, 212)],
                     pale=[(140, 142, 160), (192, 194, 208), (228, 230, 234), (246, 248, 250)], inner=(194, 148, 150)),
    },
    "cat": {
        "white": None,
        "ginger": dict(fur=[(118, 50, 46), (180, 86, 60), (232, 140, 66), (250, 180, 106)],
                       pale=[(176, 136, 126), (226, 188, 164), (250, 228, 194), (255, 244, 224)],
                       stripes=[(96, 38, 36), (148, 62, 42), (194, 94, 44), (214, 124, 66)], strength=0.8),
        "slate": dict(fur=[(52, 54, 78), (88, 94, 120), (128, 138, 158), (166, 176, 196)],
                      pale=[(96, 102, 126), (140, 148, 170), (180, 188, 204), (208, 214, 226)]),
        "black": dict(fur=[(12, 10, 18), (30, 28, 42), (46, 44, 58), (80, 90, 122)]),
        "cream": dict(fur=[(140, 104, 98), (196, 158, 136), (238, 210, 168), (252, 234, 204)],
                      pale=[(180, 160, 158), (228, 212, 202), (252, 242, 226), (255, 252, 242)],
                      stripes=[(150, 104, 84), (190, 142, 108), (218, 176, 132), (234, 200, 160)], strength=0.45),
    },
}


def key(rgb: np.ndarray) -> np.ndarray:
    """Alpha from the distance to the flat magenta background, pulled in by a pixel so no pink fringe survives."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    magenta = (r - g) + (b - g)  # high for magenta
    alpha = np.clip((380 - magenta) / 140, 0, 1)
    eroded = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))
    return np.asarray(eroded).astype(np.float32) / 255


def despill(rgb: np.ndarray, alpha: np.ndarray) -> np.ndarray:
    out = rgb.copy()
    edge = alpha > 0
    spill = np.minimum(out[..., 0], out[..., 2]) - out[..., 1]
    fix = edge & (spill > 0)
    for c in (0, 2):
        out[..., c] = np.where(fix, out[..., c] - spill * 1.0, out[..., c])
    return np.clip(out, 0, 255)


def coat_mask(rgb: np.ndarray):
    """The coat: pale, unsaturated pixels (the white fur and its cool shading), soft-edged; and the luminance."""
    f = rgb / 255.0
    mx, mn = f.max(axis=2), f.min(axis=2)
    lum = f @ LUMA
    sat = (mx - mn) / (mx + 1e-6)
    pale = np.clip((lum - 0.42) / 0.18, 0, 1)
    grey = np.clip((0.30 - sat) / 0.12, 0, 1)
    # The sick pose's blanket is a pale blue (blue over red by 0.18 and more): keep it out of the coat. The fur's
    # lavender shading sits near 0 to 0.06, so it stays coat.
    not_blue = np.clip((0.12 - (f[..., 2] - f[..., 0])) / 0.05, 0, 1)
    return pale * grey * not_blue, lum


def fur_only(mask: np.ndarray, lum: np.ndarray) -> np.ndarray:
    """Keeps the eye highlights as drawn (Saleh 2026-10-08: recolouring changed "even the eyes"). A highlight is a
    small pale island ringed by dark iris or pupil; a small island ringed by fur and hatching is a fur speck."""
    lab, n = ndimage.label(mask > 0.5)
    if n == 0:
        return mask
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    keep = np.ones(mask.shape, bool)
    for i in np.flatnonzero(sizes < MAX_HIGHLIGHT_PIXELS):
        island = lab == i + 1
        ring = ndimage.binary_dilation(island, iterations=3) & ~island & (mask < 0.5)
        if ring.any() and lum[ring].mean() < HIGHLIGHT_RING_LUM:
            keep &= ~ndimage.binary_dilation(island, iterations=2)
    return mask * keep


def ramp(lum: np.ndarray, colours, stops) -> np.ndarray:
    """The four-tone ramp: each luminance between the stops takes the blend of its two neighbours' colours."""
    c = np.array(colours, dtype=np.float32)
    return np.stack([np.interp(lum, stops, c[:, ch]) for ch in range(3)], axis=-1)


def noise(seed: int, sigma: float) -> np.ndarray:
    """A smooth seeded noise field over the picture, about -1..1."""
    rng = np.random.default_rng(seed)
    n = ndimage.gaussian_filter(rng.standard_normal((SIZE, SIZE)).astype(np.float32), sigma)
    return n / (n.std() + 1e-6) / 2.5


GRID = np.mgrid[0:SIZE, 0:SIZE].astype(np.float32) / SIZE  # (y, x), 0..1


def ellipse_distance(mark) -> np.ndarray:
    """Each pixel's distance from the mark's centre in its own (rotated) radii: 1 on its edge."""
    y, x = GRID
    cx, cy = mark["center"]
    rx, ry = mark["radius"]
    a = math.radians(mark.get("angle", 0))
    dx, dy = x - cx, y - cy
    u = dx * math.cos(a) + dy * math.sin(a)
    v = -dx * math.sin(a) + dy * math.cos(a)
    return np.sqrt((u / rx) ** 2 + (v / ry) ** 2)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def marks_mask(rig, kind: str, seed: int) -> np.ndarray:
    """The pale fur's (or the inner ear's) coverage: each mark an ellipse with a ragged, furry edge, crisp as cel art."""
    out = np.zeros((SIZE, SIZE), np.float32)
    rag = noise(seed, 3.0) * 0.16 + noise(seed + 1, 12.0) * 0.12
    for m in rig["marks"]:
        if m["type"] != kind:
            continue
        d = ellipse_distance(m) + rag
        out = np.maximum(out, 1 - smoothstep(0.86, 0.98, d))
    return out


def stripes_mask(rig, seed: int) -> np.ndarray:
    """Tabby stripes: bands repeating along each zone's axis (its period), wobbling with a seeded noise, tapering into
    points towards the zone's edge as a tabby's do."""
    out = np.zeros((SIZE, SIZE), np.float32)
    y, x = GRID
    wobble = noise(seed + 2, 18.0)
    for i, m in enumerate(z for z in rig["marks"] if z["type"] == "stripes"):
        a = math.radians(m.get("angle", 0))
        u = (x - m["center"][0]) * math.cos(a) + (y - m["center"][1]) * math.sin(a)
        phase = 2 * math.pi * u / m["period"] + wobble * 2.2 + i * 1.7
        band = np.cos(phase)
        d = ellipse_distance(m)
        width = 0.62 + 0.3 * d  # the band narrows (its threshold rises) towards the edge
        out = np.maximum(out, smoothstep(width, width + 0.08, band) * (1 - smoothstep(0.7, 1.0, d)))
    return out


def paint(rgb: np.ndarray, rig, coat, seed: int, stops):
    """The coat painted over the white fur; returns the picture and the fur mask."""
    mask, lum = coat_mask(rgb)
    mask = fur_only(mask, lum)
    if coat is None:
        return rgb, mask
    out = ramp(lum, coat["fur"], stops)
    if "stripes" in coat:
        s = stripes_mask(rig, seed) * coat["strength"]
        out = out * (1 - s[..., None]) + ramp(lum, coat["stripes"], stops) * s[..., None]
    if "pale" in coat:
        p = marks_mask(rig, "pale", seed + 3)
        out = out * (1 - p[..., None]) + ramp(lum, coat["pale"], stops) * p[..., None]
    if "inner" in coat:
        q = marks_mask(rig, "inner", seed + 4)[..., None]
        inner = np.array(coat["inner"], np.float32) * (0.75 + 0.25 * np.clip((lum[..., None] - 0.6) / 0.35, 0, 1))
        out = out * (1 - q) + inner * q
    out = rgb * (1 - mask[..., None]) + out * mask[..., None]
    # On a dark coat the ink lines would sink into the fur, and the grey hatching at the fur's edges would speckle it:
    # darken the line art and the hatching (darker than the lit fur, unsaturated, off the fur) so they stay darker than
    # the coat's shadow. Eyes, nose and mouth keep their colours.
    k = line_darkening(coat)
    if k < 1.0:
        f = rgb / 255.0
        sat = (f.max(axis=2) - f.min(axis=2)) / (f.max(axis=2) + 1e-6)
        line = np.clip((0.70 - lum) / 0.25, 0, 1) * np.clip((0.35 - sat) / 0.15, 0, 1) * (1 - mask)
        out = out * (1 - line[..., None] * (1 - k))
    return out, mask


def line_darkening(coat) -> float:
    """How much a coat darkens the line art (1: not at all): a dark coat's shadow would swallow the ink."""
    if coat is None:
        return 1.0
    shadow_lum = float(np.array(coat["fur"][1], np.float32) @ LUMA) / 255
    return float(np.clip(shadow_lum / 0.45, 0.4, 1.0))


def blink(rgb: np.ndarray, fur: np.ndarray, rig, ink) -> np.ndarray:
    """The eyes shut: each eye's ellipse filled with the fur around it and a closed lid drawn across in <ink>; RGBA,
    transparent elsewhere."""
    out = np.zeros((SIZE, SIZE, 4), np.float32)
    lids = Image.new("L", (SIZE * 4, SIZE * 4), 0)
    draw = ImageDraw.Draw(lids)
    for e in rig["eyes"]:
        grown = dict(center=e["center"], radius=[e["radius"][0] * 1.12, e["radius"][1] * 1.18])
        d = ellipse_distance(grown)
        # The fur just round the eye, less any other tone (a pale muzzle below, a shadow above): the median's.
        ring = (fur > 0.85) & (d > 1.05) & (d < 1.6)
        lum = rgb @ LUMA / 255
        mid = np.median(lum[ring]) if ring.any() else 0.5
        known = ring & (np.abs(lum - mid) < 0.06)
        sigma = e["radius"][1] * SIZE * 0.8
        wk = ndimage.gaussian_filter(known.astype(np.float32), sigma)
        fill = np.stack([ndimage.gaussian_filter(rgb[..., c] * known, sigma) for c in range(3)], -1) / (wk[..., None] + 1e-6)
        a = 1 - smoothstep(0.92, 1.0, d)
        sel = a > 0
        out[sel, :3] = fill[sel]
        out[sel, 3] = np.maximum(out[sel, 3], a[sel])
        # The lid: a gentle downward curve across the eye, a little below its middle, with a lash at the outer corner.
        cx, cy = e["center"][0] * SIZE * 4, e["center"][1] * SIZE * 4
        rx, ry = e["radius"][0] * SIZE * 4, e["radius"][1] * SIZE * 4
        width = max(12, int(ry * 0.2))
        pts = [(cx + rx * 0.95 * t, cy + ry * 0.12 + ry * 0.32 * (1 - t * t)) for t in np.linspace(-1, 1, 24)]
        draw.line(pts, fill=255, width=width, joint="curve")
        outer = 1 if cx > SIZE * 4 * rig["head"]["center"][0] else -1
        end = pts[-1] if outer > 0 else pts[0]
        draw.line([end, (end[0] + outer * rx * 0.28, end[1] - ry * 0.22)], fill=255, width=max(8, width * 2 // 3))
    lid = np.asarray(lids.resize((SIZE, SIZE), Image.LANCZOS)).astype(np.float32) / 255
    out[..., :3] = out[..., :3] * (1 - lid[..., None]) + ink * lid[..., None]
    out[..., 3] = np.maximum(out[..., 3], lid)
    return out


def fixed_guid(name: str) -> str:
    """A GUID derived from the file's name: the same file always gets the same one."""
    return hashlib.md5(("TimeSorter/Art/UI/Resources/Home/" + name).encode("utf-8")).hexdigest()


def write_meta(png_path: str) -> None:
    """Writes the .meta (its fixed GUID) unless one exists: Unity keeps the GUID and fills in the importer."""
    meta = png_path + ".meta"
    if os.path.exists(meta):
        return
    with open(meta, "w", encoding="utf-8", newline="\n") as f:
        f.write(f"fileFormatVersion: 2\nguid: {fixed_guid(os.path.basename(png_path))}\n")


def save(img: Image.Image, name: str) -> None:
    path = os.path.join(OUT, name)
    img.save(path, optimize=True)
    write_meta(path)


def contact_sheet(path: str, rows, old_folder) -> None:
    """Old against new: one row per coat, its four moods before (when --old is given) and after, on the game's ink."""
    cell = 200
    gap = 20 if old_folder else 0
    cols = 8 if old_folder else 4
    sheet = Image.new("RGBA", (cell * cols + gap, cell * len(rows)), (58, 46, 52, 255))
    for y, (kind, coat, imgs) in enumerate(rows):
        for x, (mood, img) in enumerate(imgs):
            if old_folder:
                old = Image.open(os.path.join(old_folder, f"pet_{kind}_{coat}_{mood}.png")).convert("RGBA")
                sheet.alpha_composite(old.resize((cell, cell), Image.LANCZOS), (x * cell, y * cell))
            left = (x + (4 if old_folder else 0)) * cell + gap
            sheet.alpha_composite(img.resize((cell, cell), Image.LANCZOS), (left, y * cell))
        ImageDraw.Draw(sheet).text((4, y * cell + 4), f"{kind} {coat}", fill=(255, 255, 255, 255))
    sheet.convert("RGB").save(path)


def main():
    rigs = {(r["kind"].lower(), r["mood"].lower()): r for r in json.load(open(RIGS, encoding="utf-8"))["rigs"]}
    only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
    rows = []
    for kind, coats in COATS.items():
        for coat, colours in coats.items():
            if only and only != f"{kind}_{coat}":
                continue
            imgs = []
            for mi, mood in enumerate(MOODS):
                rig = rigs[(kind, mood)]
                src = np.asarray(Image.open(os.path.join(SRC, f"pet_{kind}_{mood}.png")).convert("RGB")).astype(np.float32)
                alpha = key(src)
                rgb = despill(src, alpha)
                seed = 1000 * (kind == "cat") + 10 * mi  # the same stripes and ragged edges for every coat of a mood
                painted, fur = paint(rgb, rig, colours, seed, STOPS[kind])
                img = Image.fromarray(np.dstack([np.clip(painted, 0, 255), alpha * 255]).astype(np.uint8), "RGBA")
                save(img, f"pet_{kind}_{coat}_{mood}.png")
                if rig["eyes"]:
                    b = blink(painted, fur, rig, np.array(INK, np.float32) * line_darkening(colours))
                    b[..., 3] *= alpha
                    b[..., 3] *= 255
                    save(Image.fromarray(np.clip(b, 0, 255).astype(np.uint8), "RGBA"), f"pet_{kind}_{coat}_{mood}_blink.png")
                imgs.append((mood, img))
            rows.append((kind, coat, imgs))
            print("painted", kind, coat, flush=True)
    if "--sheet" in sys.argv:
        old = sys.argv[sys.argv.index("--old") + 1] if "--old" in sys.argv else None
        contact_sheet(sys.argv[sys.argv.index("--sheet") + 1], rows, old)
    print("ok", len(rows), "coats into", OUT)


if __name__ == "__main__":
    main()
