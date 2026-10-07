"""Pet coats (the Home pet spec PS11): keys the Canva pet art's magenta background and recolours the white coat into
five coats per animal, then installs them as the game's art slots.

The base art (src/pet_<kind>_<mood>.png, made in Canva for run 7) is a white-coated dog and cat in four moods each
(idle, happy, sad, sick), drawn in the same pose so the game can swap them in place. Every coat is a recolour of the
same pixels, so all coats and moods line up.

Output: Assets/Art/UI/Resources/Home/pet_<kind>_<coat>_<mood>.png (1024x1024, transparent; ArtSlots.PetSprite), each
with a .meta whose GUID is fixed (derived from the file's name), so a regenerated file keeps its GUID. Unity fills in the
importer's settings on import (ArtSlotImporter); the committed metas are the ones Unity wrote. The coats' ids must match
world_source.json home.pet.kinds[].coats.

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

# Coat: (light, shadow) in sRGB. The first coat of each kind is the art's own white.
COATS = {
    "dog": {"cream": None, "tan": ((222, 176, 118), (168, 116, 68)), "chocolate": ((128, 82, 54), (82, 50, 34)),
            "charcoal": ((92, 88, 96), (52, 48, 58)), "grey": ((168, 176, 188), (112, 120, 136))},
    "cat": {"white": None, "ginger": ((236, 156, 82), (182, 98, 44)), "slate": ((140, 146, 160), (90, 94, 110)),
            "black": ((70, 66, 76), (40, 36, 46)), "cream": ((236, 214, 178), (190, 160, 122))},
}


def key(rgb: np.ndarray) -> np.ndarray:
    """Alpha from the distance to the flat magenta background, the edge pulled in by a pixel (no pink fringe)."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    magenta = (r - g) + (b - g)  # high for magenta
    alpha = np.clip((380 - magenta) / 140, 0, 1)
    eroded = np.asarray(Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3))).astype(np.float32) / 255
    return eroded


def despill(rgb: np.ndarray, alpha: np.ndarray) -> np.ndarray:
    """Takes the magenta spill out of the edge pixels."""
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
    lum = f @ np.array([0.299, 0.587, 0.114])
    sat = (mx - mn) / (mx + 1e-6)
    pale = np.clip((lum - 0.42) / 0.18, 0, 1)
    grey = np.clip((0.30 - sat) / 0.12, 0, 1)
    # The sick pose's blanket is a pale blue: keep anything bluish out of the coat.
    not_blue = np.clip((0.035 - (f[..., 2] - f[..., 0])) / 0.03, 0, 1)
    return pale * grey * not_blue, lum


def recolour(rgb: np.ndarray, light, shadow) -> np.ndarray:
    """The coat's pixels mapped from shadow to light by their luminance; everything else kept."""
    mask, lum = coat_mask(rgb)
    t = np.clip((lum - 0.55) / 0.40, 0, 1)[..., None]
    light = np.array(light, dtype=np.float32)
    shadow = np.array(shadow, dtype=np.float32)
    coat = shadow * (1 - t) + light * t
    return rgb * (1 - mask[..., None]) + coat * mask[..., None]


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
