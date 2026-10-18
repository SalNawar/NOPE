"""Pet coats (the Home pet spec PS11): keys the Canva pet art's magenta background and makes five coats per animal,
then installs them as the game's art slots (their animation frames: make_pet_frames.py).

The base art (src/pet_<kind>_<mood>.png, made in Canva for run 7, exported at its native 1264 px) is a white-coated dog
and cat in four moods each (idle, happy, sad, sick), drawn in the same pose so the game can swap them in place.

Dogs: every coat is painted over the white picture's own pixels, so all coats and moods line up, and the line art, the
eyes, the nose and the blanket stay as drawn. The white fur's own cel shading (its luminance: the lit fur, its shadow,
the hatching's deep tone and the sheen) is mapped through a four-tone ramp per coat (deep, shadow, base, highlight;
shadows lean violet as the game's cel style does), and the markings come from the rigs' marks
(tools/art/pets/pet_rigs.json): pale fur (a tan dog's cream muzzle, chest and paws, a charcoal dog's white blaze) with a
fur-ragged edge, and a dog's inner ear.

Cats (Saleh 2026-10-18 on demo 1018a: "cat coat looks weird"): each coat but the white is its own Canva drawing, made
from the white picture of its mood with the pose kept (src/coats/pet_cat_<coat>_<mood>.png: the ginger and the cream
tabbies' stripes follow the body's form, the slate and the black are solid with their own cel shading), so the fur
reads as drawn, not recoloured. The drawings line up with the white pictures (silhouettes within about a pixel), so
every coat's animation frames are still laid over its picture (make_pet_frames.py). The white cat is the base picture.

Crispness (Saleh 2026-10-18: "pet resolution pictures are bad"): the work is done at the sources' 1264 px and the
result is resampled once to 1024 px (Lanczos on premultiplied colour, so no dark or pink fringe), the size the game
imports (ArtSlotImporter).

Output: Assets/Art/UI/Resources/Home/pet_<kind>_<coat>_<mood>.png (1024x1024, transparent; ArtSlots.PetSprite), each
with a .meta whose GUID is fixed (derived from the file's name), so a regenerated file keeps its GUID. Unity fills in the
importer's settings on import (ArtSlotImporter). The coats' ids must match world_source.json home.pet.kinds[].coats.

Needs numpy, Pillow and scipy.

Usage: python tools/art/pets/make_pets.py [--only cat_ginger] [--sheet <contact sheet .png> [--old <folder of the previous coats>]]
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
DRAWN = os.path.join(SRC, "coats")
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(REPO, "Assets", "Art", "UI", "Resources", "Home")
RIGS = os.path.join(HERE, "pet_rigs.json")
MOODS = ["idle", "happy", "sad", "sick"]
SIZE = 1264  # the Canva sources' native size: everything is worked at it
OUT_SIZE = 1024  # the installed pictures' size
SCALE = SIZE / 1024  # the pixel sizes below were tuned at 1024 px
LUMA = np.array([0.299, 0.587, 0.114], dtype=np.float32)
MAX_HIGHLIGHT_PIXELS = int(500 * SCALE * SCALE)  # eye highlights are under 300 px at 1024 (a far leg's fur is ~2,600)
HIGHLIGHT_RING_LUM = 0.36  # an eye highlight's ring (iris, pupil) is darker than this; a fur speck's is lighter
STOPS = {"dog": [0.58, 0.78, 0.94, 0.985]}  # the white fur's tones: hatching, cel shadow, lit fur, sheen

# A dog coat: ramps of four colours (deep, shadow, base, highlight) at STOPS for the fur and the pale marks, and the
# inner ear's colour. A cat coat: "drawn" (its own Canva drawing per mood, src/coats) or None (the white picture).
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
    "cat": {"white": None, "ginger": "drawn", "slate": "drawn", "black": "drawn", "cream": "drawn"},
}


def load(path: str) -> np.ndarray:
    """A source picture as float RGB at SIZE (every Canva source is exported at it)."""
    img = Image.open(path).convert("RGB")
    if img.size != (SIZE, SIZE):
        raise SystemExit(f"{path}: {img.size[0]}x{img.size[1]}, expected the Canva export's {SIZE}x{SIZE}")
    return np.asarray(img).astype(np.float32)


def magenta(rgb: np.ndarray) -> np.ndarray:
    """How magenta each pixel is: red and blue over green (about 400 to 500 on the key, under 130 on any drawn colour)."""
    return (rgb[..., 0] - rgb[..., 1]) + (rgb[..., 2] - rgb[..., 1])


def background_level(rgb: np.ndarray) -> np.ndarray:
    """The magenta background's own level under every pixel. A generated drawing's background is no flat (255, 0, 255):
    it drifts from one drawing to the next (about 270 to 450) and shades across a drawing, so it is read from the
    drawing's clearly-background pixels (very magenta for their brightness) and spread smoothly under the figure."""
    m = magenta(rgb)
    clear = (m > 200) & (m > 1.2 * rgb.max(-1))
    weight = ndimage.gaussian_filter(clear.astype(np.float32), 40)
    level = ndimage.gaussian_filter(np.where(clear, m, 0).astype(np.float32), 40) / np.maximum(weight, 1e-4)
    return np.where(weight > 1e-3, level, np.median(m[clear]) if clear.any() else 440)


def key(rgb: np.ndarray) -> np.ndarray:
    """Alpha from how close each pixel is to the background's own magenta (opaque at 60% of its level and under,
    clear from 10 under it), pulled in by a pixel so no pink fringe survives."""
    level = background_level(rgb)
    alpha = np.clip((level - 10 - magenta(rgb)) / (0.4 * level - 10), 0, 1)
    eroded = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))
    alpha = np.asarray(eroded).astype(np.float32) / 255
    # Faint specks off the figure (the generator's blotchy background and soft shadows) are islands that never
    # become half opaque: clear them. The figure and its whiskers are opaque somewhere.
    lab, n = ndimage.label(alpha > 0)
    if n:
        peak = ndimage.maximum(alpha, lab, range(1, n + 1))
        alpha = np.where(np.isin(lab, 1 + np.flatnonzero(peak < 0.5)), 0, alpha)
    return alpha


def despill(rgb: np.ndarray, alpha: np.ndarray) -> np.ndarray:
    """Takes the background's magenta out of the figure's rim (within 3 px of the clear background): red and blue
    down to green where both are over it. The figure's inside keeps its colours (its pinks and lavenders)."""
    out = rgb.copy()
    rim = (alpha > 0) & ndimage.binary_dilation(alpha < 0.5, iterations=3)
    spill = np.minimum(out[..., 0], out[..., 2]) - out[..., 1]
    fix = rim & (spill > 0)
    for c in (0, 2):
        out[..., c] = np.where(fix, out[..., c] - spill * 1.0, out[..., c])
    return np.clip(out, 0, 255)


def keyed(rgb: np.ndarray):
    """A source picture keyed off its magenta: (despilled RGB, alpha)."""
    alpha = key(rgb)
    return despill(rgb, alpha), alpha


def coat_mask(rgb: np.ndarray):
    """The white coat: pale, unsaturated pixels (the white fur and its cool shading), soft-edged; and the luminance."""
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


def fur(rgb: np.ndarray):
    """The white fur's coverage (eye highlights left out) and the luminance."""
    mask, lum = coat_mask(rgb)
    return fur_only(mask, lum), lum


def ramp(lum: np.ndarray, colours, stops) -> np.ndarray:
    """The four-tone ramp: each luminance between the stops takes the blend of its two neighbours' colours."""
    c = np.array(colours, dtype=np.float32)
    return np.stack([np.interp(lum, stops, c[:, ch]) for ch in range(3)], axis=-1)


def noise(seed: int, sigma: float) -> np.ndarray:
    """A smooth seeded noise field over the picture, about -1..1 (sigma in 1024 px units)."""
    rng = np.random.default_rng(seed)
    n = ndimage.gaussian_filter(rng.standard_normal((SIZE, SIZE)).astype(np.float32), sigma * SCALE)
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


def paint(rgb: np.ndarray, rig, coat, seed: int) -> np.ndarray:
    """A dog coat painted over the white fur."""
    stops = STOPS["dog"]
    mask, lum = fur(rgb)
    out = ramp(lum, coat["fur"], stops)
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
    return out


def line_darkening(coat) -> float:
    """How much a dog coat darkens the line art (1: not at all): a dark coat's shadow would swallow the ink."""
    shadow_lum = float(np.array(coat["fur"][1], np.float32) @ LUMA) / 255
    return float(np.clip(shadow_lum / 0.45, 0.4, 1.0))


def seed(kind: str, mood: str) -> int:
    """The same ragged edges for every coat of a mood."""
    return 1000 * (kind == "cat") + 10 * MOODS.index(mood)


def white_path(kind: str, mood: str) -> str:
    return os.path.join(SRC, f"pet_{kind}_{mood}.png")


def drawn_path(coat: str, mood: str, frame: str = None) -> str:
    """A cat coat's Canva drawing: its picture for a mood, or (with a frame) its drawing of that frame."""
    if frame:
        return os.path.join(DRAWN, "frames", f"pet_cat_{coat}_{mood}_{frame}.png")
    return os.path.join(DRAWN, f"pet_cat_{coat}_{mood}.png")


def is_drawn(kind: str, coat: str) -> bool:
    """True for a coat made of its own drawings (the cats' but the white)."""
    return COATS[kind][coat] == "drawn"


def picture(kind: str, coat: str, mood: str, rigs):
    """A coat's picture for a mood at SIZE: (RGB, alpha)."""
    if kind == "cat":
        return keyed(load(drawn_path(coat, mood) if is_drawn(kind, coat) else white_path(kind, mood)))
    rgb, alpha = keyed(load(white_path(kind, mood)))
    return paint(rgb, rigs[(kind, mood)], COATS[kind][coat], seed(kind, mood)), alpha


def finish(rgb: np.ndarray, alpha: np.ndarray) -> Image.Image:
    """The picture as installed: resampled once from SIZE to OUT_SIZE with Lanczos on premultiplied colour."""
    rgba = np.dstack([np.clip(rgb, 0, 255), np.clip(alpha, 0, 1) * 255]).astype(np.uint8)
    img = Image.fromarray(rgba, "RGBA").convert("RGBa").resize((OUT_SIZE, OUT_SIZE), Image.LANCZOS)
    return img.convert("RGBA")


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


def load_rigs():
    """The dogs' rigs by (kind, mood)."""
    return {(r["kind"].lower(), r["mood"].lower()): r for r in json.load(open(RIGS, encoding="utf-8"))["rigs"]}


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
    rigs = load_rigs()
    only = sys.argv[sys.argv.index("--only") + 1] if "--only" in sys.argv else None
    rows = []
    for kind, coats in COATS.items():
        for coat in coats:
            if only and only != f"{kind}_{coat}":
                continue
            imgs = []
            for mood in MOODS:
                img = finish(*picture(kind, coat, mood, rigs))
                save(img, f"pet_{kind}_{coat}_{mood}.png")
                imgs.append((mood, img))
            rows.append((kind, coat, imgs))
            print("made", kind, coat, flush=True)
    if "--sheet" in sys.argv:
        old = sys.argv[sys.argv.index("--old") + 1] if "--old" in sys.argv else None
        contact_sheet(sys.argv[sys.argv.index("--sheet") + 1], rows, old)
    print("ok", len(rows), "coats into", OUT)


if __name__ == "__main__":
    main()
