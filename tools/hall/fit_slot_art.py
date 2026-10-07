"""Fits an isolated slot design onto the hall painting's exact object, for the hall slots (HALL_SLOTS_ART_REQUEST.md).

GPT's image tool cannot paint a 2172x724 canvas registered pixel for pixel, so it delivers a flat, front-view design of
the object instead: one banner cloth with its emblem, on a plain background. This script places it exactly:

1. It finds the painted object's pixels inside the slot's box on HallWarmStone.png. For a banner, these are the red
   cloth, plus a one-pixel fringe so no old red shows at the anti-aliased edge. The dark ink outline is left
   transparent, so the painting's own outline frames the new cloth.
2. It keys the design's flat background, using the colour of its corners, and stretches the design onto the
   cloth's bounding box.
3. It keeps the painting's folds and edge shading: each pixel of the design is multiplied by the old cloth's
   brightness relative to its median.
4. It writes a transparent 2172x724 PNG with only those pixels set, ready for Assets/Art/UI/Resources/Hall/Slots/<slot>/<variant>.png.

Usage:
    python fit_slot_art.py <slot> <design.png> <out.png> [--painting HallWarmStone.png] [--preview preview.png]
Slots: 13-flag-left-cloth, 14-flag-right-cloth.
"""
import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

CANVAS = (2172, 724)
DEFAULT_PAINTING = Path(__file__).resolve().parents[2] / "Assets/Art/Office/AnimeHallLayers/Completion/WarmStone/HallWarmStone.png"

# How far past the request's box the painted object may reach (the warm-stone repaint is a few pixels off the boxes).
MARGIN = 8

# Slot: the paintable box (x, y, w, h) from HALL_SLOTS_ART_REQUEST.md, and the test that picks the object's pixels.
SLOTS = {
    "13-flag-left-cloth": {"box": (786, 0, 70, 204), "object": "red"},
    "14-flag-right-cloth": {"box": (1360, 0, 70, 200), "object": "red"},
}


def object_mask(rgb: np.ndarray) -> np.ndarray:
    """The red cloth: clearly red and not dark. Returns floats in 0..1."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    red = (r > 90) & (r > g * 1.45) & (r > b * 1.45)
    return red.astype(np.float32)


def key_background(design: Image.Image) -> Image.Image:
    """The design's flat background turned transparent, keyed on the median colour of its four corners (alpha kept if it already has one)."""
    rgba = np.asarray(design.convert("RGBA")).astype(np.float32)
    if (rgba[..., 3] < 250).mean() > 0.02:
        return design.convert("RGBA")
    h, w = rgba.shape[:2]
    k = max(2, min(h, w) // 40)
    corners = np.concatenate([rgba[:k, :k, :3].reshape(-1, 3), rgba[:k, -k:, :3].reshape(-1, 3),
                              rgba[-k:, :k, :3].reshape(-1, 3), rgba[-k:, -k:, :3].reshape(-1, 3)])
    bg = np.median(corners, axis=0)
    dist = np.linalg.norm(rgba[..., :3] - bg, axis=2)
    rgba[..., 3] = np.clip((dist - 30) / 40, 0, 1) * 255
    return Image.fromarray(rgba.astype(np.uint8), "RGBA")


def fit(slot: str, design_path: Path, out_path: Path, painting_path: Path, preview_path: Path | None) -> None:
    spec = SLOTS[slot]
    bx, by, bw, bh = spec["box"]
    x, y = max(0, bx - MARGIN), max(0, by - MARGIN)
    w, h = min(CANVAS[0], bx + bw + MARGIN) - x, min(CANVAS[1], by + bh + MARGIN) - y
    painting = Image.open(painting_path).convert("RGB")
    if painting.size != CANVAS:
        sys.exit(f"painting is {painting.size}, expected {CANVAS}")
    region = np.asarray(painting.crop((x, y, x + w, y + h))).astype(np.float32)

    core = object_mask(region)
    # Only the object itself: the largest connected patch, so other red things near the box are left alone.
    labels, count = ndimage.label(core > 0)
    if count:
        sizes = ndimage.sum(core > 0, labels, range(1, count + 1))
        core = (labels == 1 + int(np.argmax(sizes))).astype(np.float32)
    if core.sum() < 50:
        sys.exit(f"no object pixels found in {slot}'s box")
    # One pixel of fringe (the anti-aliased red-into-ink edge) so no old red peeks out; never into true ink.
    grown = np.asarray(Image.fromarray((core * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))).astype(np.float32) / 255
    lum = region @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    # The fringe is only the reddish blend between cloth and ink: never past a one-pixel outline into the wall behind.
    reddish = (region[..., 0] > region[..., 1] * 1.15) & (region[..., 0] > region[..., 2] * 1.15)
    mask = np.where((lum > 45) & reddish, grown, 0)

    ys, xs = np.nonzero(mask)
    bx0, bx1, by0, by1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1

    design = key_background(Image.open(design_path))
    # Trim the design to its own content, then stretch it onto the cloth's bounding box.
    bbox = design.getbbox()
    if bbox:
        design = design.crop(bbox)
    placed = np.asarray(design.resize((bx1 - bx0, by1 - by0), Image.LANCZOS)).astype(np.float32)

    # The painting's folds and edges: the old cloth's brightness relative to its median, applied to the new cloth.
    median = np.median(lum[core > 0])
    shade = np.clip(lum / max(median, 1), 0.55, 1.2)

    out = np.zeros((h, w, 4), dtype=np.float32)
    sub = (slice(by0, by1), slice(bx0, bx1))
    out[sub + (slice(0, 3),)] = placed[..., :3] * shade[sub][..., None]
    # Where the design is transparent (a shaped edge), the old cloth would show; fill it with the design's edge colour instead.
    alpha_design = placed[..., 3] / 255
    if (alpha_design < 0.99).any():
        fill = np.median(placed[alpha_design > 0.5][:, :3], axis=0) if (alpha_design > 0.5).any() else np.zeros(3)
        rgb = out[sub + (slice(0, 3),)]
        out[sub + (slice(0, 3),)] = rgb * alpha_design[..., None] + (fill * shade[sub][..., None]) * (1 - alpha_design[..., None])
    out[..., 3] = mask * 255
    out[..., :3] = np.clip(out[..., :3], 0, 255)

    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    canvas.paste(Image.fromarray(out.astype(np.uint8), "RGBA"), (x, y))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(out_path)
    print(f"{slot}: {int(mask.sum())} px set in box {spec['box']}, cloth {bx1 - bx0}x{by1 - by0} -> {out_path}")

    if preview_path:
        comp = painting.convert("RGBA")
        comp.alpha_composite(canvas)
        pad = 40
        crop = comp.crop((max(0, x - pad), 0, min(CANVAS[0], x + w + pad), min(CANVAS[1], y + h + pad)))
        crop = crop.resize((crop.width * 3, crop.height * 3), Image.NEAREST)
        crop.save(preview_path)


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("slot", choices=sorted(SLOTS))
    ap.add_argument("design", type=Path)
    ap.add_argument("out", type=Path)
    ap.add_argument("--painting", type=Path, default=DEFAULT_PAINTING)
    ap.add_argument("--preview", type=Path)
    a = ap.parse_args()
    fit(a.slot, a.design, a.out, a.painting, a.preview)


if __name__ == "__main__":
    main()
