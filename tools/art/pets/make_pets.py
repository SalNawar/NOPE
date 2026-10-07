"""Pet coats (the Home pet spec PS11): keys the Canva pet art's magenta background and recolours the white coat into
five coats per animal, then installs them as the game's art slots.

The base art (src/pet_<kind>_<mood>.png, made in Canva for run 7) is a white-coated dog and cat in four moods each
(idle, happy, sad, sick), drawn in the same pose so the game can swap them in place. Every coat is a recolour of the
same pixels, so all coats and moods line up.

Output: Assets/Art/UI/Resources/Home/pet_<kind>_<coat>_<mood>.png (1024x1024, transparent; ArtSlots.PetSprite), each
with a .meta whose GUID is fixed (derived from the file's name), so a regenerated file keeps its GUID. Unity fills in the
importer's settings on import (ArtSlotImporter); the committed metas are the ones Unity wrote. The coats' ids must match
world_source.json home.pet.kinds[].coats.

The recolour keeps the eyes' highlights as drawn and the line art dark on dark coats (v2, Saleh 2026-10-08: the first
recolour changed "even the eyes"). Needs numpy, Pillow and scipy.

Usage: python tools/art/pets/make_pets.py [--sheet <contact sheet .jpg>]
"""
import hashlib
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "src")
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
OUT = os.path.join(REPO, "Assets", "Art", "UI", "Resources", "Home")
MOODS = ["idle", "happy", "sad", "sick"]

MAX_HIGHLIGHT_PIXELS = 500  # at 1024x1024: eye highlights are under 300 px (a far leg's fur is ~2,600)
HIGHLIGHT_RING_LUM = 0.36  # an eye highlight's ring (iris, pupil) is darker than this; a fur speck's is lighter

# Coat: (light, shadow) in sRGB. The first coat of each kind is the art's own white.
COATS = {
    "dog": {"cream": None, "tan": ((222, 176, 118), (168, 116, 68)), "chocolate": ((128, 82, 54), (82, 50, 34)),
            "charcoal": ((112, 108, 118), (70, 66, 78)), "grey": ((168, 176, 188), (112, 120, 136))},
    "cat": {"white": None, "ginger": ((236, 156, 82), (182, 98, 44)), "slate": ((140, 146, 160), (90, 94, 110)),
            "black": ((88, 84, 96), (54, 50, 62)), "cream": ((236, 214, 178), (190, 160, 122))},
}


def key(rgb: np.ndarray) -> np.ndarray:
    """Alpha from the distance to the flat magenta background, plus a despill of the pink fringe."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    magenta = (r - g) + (b - g)  # high for magenta
    alpha = np.clip((380 - magenta) / 140, 0, 1)
    # Pull the edge in by a pixel so no magenta-tinted fringe survives on dark coats.
    eroded = np.asarray(Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))).astype(np.float32) / 255
    return eroded


def despill(rgb: np.ndarray, alpha: np.ndarray) -> np.ndarray:
    out = rgb.copy()
    edge = alpha > 0
    spill = np.minimum(out[..., 0], out[..., 2]) - out[..., 1]
    fix = edge & (spill > 0)
    for c in (0, 2):
        out[..., c] = np.where(fix, out[..., c] - spill * 1.0, out[..., c])
    return np.clip(out, 0, 255)


def coat_mask(rgb: np.ndarray) -> np.ndarray:
    """The coat: pale, unsaturated pixels (the white fur and its cool shading), soft-edged."""
    f = rgb / 255.0
    mx, mn = f.max(axis=2), f.min(axis=2)
    lum = f @ np.array([0.299, 0.587, 0.114])
    sat = (mx - mn) / (mx + 1e-6)
    pale = np.clip((lum - 0.42) / 0.18, 0, 1)
    grey = np.clip((0.30 - sat) / 0.12, 0, 1)
    # The sick pose's blanket is a pale blue (blue over red by 0.18 and more): keep it out of the coat. The fur's
    # lavender shading sits near 0 to 0.06, so it stays coat (a tighter gate left it in blotches on dark coats).
    not_blue = np.clip((0.12 - (f[..., 2] - f[..., 0])) / 0.05, 0, 1)
    return pale * grey * not_blue, lum


def fur_only(mask: np.ndarray, lum: np.ndarray) -> np.ndarray:
    """Keeps the eye highlights as drawn (Saleh 2026-10-08: recolouring changed "even the eyes"). A highlight is a
    small pale island ringed by dark iris or pupil; a small island ringed by fur and hatching is a fur speck and is
    recoloured with the coat (else it would sparkle white on a dark coat)."""
    from scipy import ndimage
    lab, n = ndimage.label(mask > 0.5)
    if n == 0:
        return mask
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    keep = np.ones(mask.shape, bool)
    for i in np.flatnonzero(sizes < MAX_HIGHLIGHT_PIXELS):
        island = lab == i + 1
        ring = ndimage.binary_dilation(island, iterations=3) & ~island & (mask < 0.5)
        if ring.any() and lum[ring].mean() < HIGHLIGHT_RING_LUM:
            keep &= ~ndimage.binary_dilation(island, iterations=2)  # the island and its soft rim
    return mask * keep


def recolour(rgb: np.ndarray, light, shadow) -> np.ndarray:
    mask, lum = coat_mask(rgb)
    mask = fur_only(mask, lum)
    t = np.clip((lum - 0.55) / 0.40, 0, 1)[..., None]
    light = np.array(light, dtype=np.float32)
    shadow = np.array(shadow, dtype=np.float32)
    coat = shadow * (1 - t) + light * t
    out = rgb * (1 - mask[..., None]) + coat * mask[..., None]
    # On a dark coat the ink lines would sink into the fur: darken the line art (dark, unsaturated, off the fur) so it
    # stays darker than the coat's shadow. Eyes, nose and mouth keep their colours.
    shadow_lum = float(shadow @ np.array([0.299, 0.587, 0.114])) / 255
    k = float(np.clip(shadow_lum / 0.45, 0.4, 1.0))
    if k < 1.0:
        f = rgb / 255.0
        sat = (f.max(axis=2) - f.min(axis=2)) / (f.max(axis=2) + 1e-6)
        line = np.clip((0.40 - lum) / 0.15, 0, 1) * np.clip((0.35 - sat) / 0.15, 0, 1) * (1 - mask)
        out = out * (1 - line[..., None] * (1 - k))
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


def main():
    os.makedirs(OUT, exist_ok=True)
    folder_meta = OUT + ".meta"
    if not os.path.exists(folder_meta):
        with open(folder_meta, "w", encoding="utf-8", newline="\n") as f:
            f.write(f"fileFormatVersion: 2\nguid: {fixed_guid('')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n"
                    "  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    sheet_rows = []
    for kind, coats in COATS.items():
        for coat, colours in coats.items():
            row = []
            for mood in MOODS:
                src = np.asarray(Image.open(os.path.join(SRC, f"pet_{kind}_{mood}.png")).convert("RGB")).astype(np.float32)
                alpha = key(src)
                rgb = despill(src, alpha)
                if colours:
                    rgb = recolour(rgb, *colours)
                img = Image.fromarray(np.dstack([np.clip(rgb, 0, 255), alpha * 255]).astype(np.uint8), "RGBA")
                path = os.path.join(OUT, f"pet_{kind}_{coat}_{mood}.png")
                img.save(path)
                write_meta(path)
                row.append(img)
            sheet_rows.append(row)
    if "--sheet" in sys.argv:
        cell = 220
        sheet = Image.new("RGBA", (cell * 4, cell * len(sheet_rows)), (58, 46, 52, 255))
        for y, row in enumerate(sheet_rows):
            for x, img in enumerate(row):
                sheet.alpha_composite(img.resize((cell, cell), Image.LANCZOS), (x * cell, y * cell))
        sheet.convert("RGB").save(sys.argv[sys.argv.index("--sheet") + 1], quality=88)
    print("ok", len(sheet_rows), "coats into", OUT)


if __name__ == "__main__":
    main()
