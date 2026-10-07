"""Fits an isolated slot design onto the hall painting's exact object, for the hall slots (HALL_SLOTS_ART_REQUEST.md).

GPT's image tool cannot paint a 2172x724 canvas registered pixel for pixel, so it delivers a flat, front-view design of
the object instead (one banner cloth, one header plate) on a plain magenta or transparent background. This script
places it exactly and writes a transparent 2172x724 PNG with only the object's pixels set, ready for
Assets/Art/UI/Resources/Hall/Slots/<slot>/<variant>.png.

Every mode first isolates the design's straight printed rectangle (printed_rect):
- The background goes: an RGB source is keyed on the median colour of its corners (the magenta margin, which varies
  by a few values); a transparent source keeps only pixels of alpha >= 200, so its faint low-alpha fringe never counts.
- The rectangle is the longest run of rows (then columns) the design covers for more than half the most covered one:
  a stray fringe pixel or a noisy margin cannot widen it, which the raw alpha>0 bounding box would.
- It is inset by 2 px, so no anti-aliased magenta or fringe edge comes along, and made opaque (any pixel the key took
  inside it gets the rectangle's median colour).

Modes, by slot:

cloth (13-flag-left-cloth, 14-flag-right-cloth): the design replaces a painted banner.
1. It finds the painted object's pixels inside the slot's box on HallWarmStone.png: the red cloth (its dark-red
   edge shading too), plus a one-pixel fringe so no old red shows at the anti-aliased edge. The dark ink outline is left transparent, so the painting's
   own outline frames the new cloth.
2. It stretches the printed rectangle onto the cloth's bounding box.
3. It keeps the painting's folds and edge shading: each pixel of the design is multiplied by the old cloth's
   brightness relative to its median.

plate (15-departure-board-frame): the design is a new header plate hung between the departure board's two rods.
1. It measures the plate's place on HallWarmStone.png inside the slot's box: the two rods are the tall dark or
   saturated patches hanging from the box's top at its ends (their inner edges bound the plate, clear of them), and
   the board's top is its ink line, the first dark row under the rods' feet (the plate stands on it).
2. It fits the printed rectangle into that place keeping its aspect (contain, never stretch), as wide as the rods
   allow and standing on the board; what is left above it stays the painting.
3. It carries the painting's shading: the ceiling's broad light across the plate (its brightness with the bright
   diffusers left out, heavily blurred, relative to its median, within 0.9..1.08), a one-pixel lit top edge and a
   two-pixel shaded lower lip (the hall's two-tone cel), and a one-pixel outline in the painting's own ink.
   The rods and the board are never drawn over, so they read beside and under the plate.

Usage:
    python fit_slot_art.py <slot> <design.png> <out.png> [--painting HallWarmStone.png] [--preview preview.png]
Slots: 13-flag-left-cloth, 14-flag-right-cloth (cloth), 15-departure-board-frame (plate).
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
# A transparent source's pixels count as the design only from this alpha (its fringe is fainter).
OPAQUE = 200
# Pixels taken off each side of the printed rectangle (its anti-aliased edge against the background).
INSET = 2

# Slot: the paintable box (x, y, w, h) from HALL_SLOTS_ART_REQUEST.md, and how the design is fitted.
SLOTS = {
    "13-flag-left-cloth": {"box": (786, 0, 70, 204), "mode": "cloth"},
    "14-flag-right-cloth": {"box": (1360, 0, 70, 200), "mode": "cloth"},
    "15-departure-board-frame": {"box": (950, 36, 226, 40), "mode": "plate"},
}

LUMA = np.array([0.299, 0.587, 0.114], dtype=np.float32)


def object_mask(rgb: np.ndarray) -> np.ndarray:
    """The red cloth: clearly red and not dark. Returns floats in 0..1."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    red = (r > 90) & (r > g * 1.45) & (r > b * 1.45)
    return red.astype(np.float32)


def design_mask(rgba: np.ndarray) -> np.ndarray:
    """Which pixels are the design: alpha >= OPAQUE for a transparent source, else away from the corners' key colour."""
    if (rgba[..., 3] < 250).mean() > 0.02:
        return rgba[..., 3] >= OPAQUE
    h, w = rgba.shape[:2]
    k = max(2, min(h, w) // 40)
    corners = np.concatenate([rgba[:k, :k, :3].reshape(-1, 3), rgba[:k, -k:, :3].reshape(-1, 3),
                              rgba[-k:, :k, :3].reshape(-1, 3), rgba[-k:, -k:, :3].reshape(-1, 3)])
    key = np.median(corners, axis=0)
    return np.linalg.norm(rgba[..., :3] - key, axis=2) > 60


def printed_rect(design: Image.Image) -> np.ndarray:
    """The design's straight printed rectangle, background and edge fringe removed, opaque (float RGBA)."""
    rgba = np.asarray(design.convert("RGBA")).astype(np.float32)
    fg = design_mask(rgba)

    def span(cover: np.ndarray) -> tuple[int, int]:
        on = np.nonzero(cover > 0.5 * cover.max())[0]
        if on.size == 0:
            sys.exit("the design has no printed rectangle")
        runs = np.split(on, np.nonzero(np.diff(on) > 1)[0] + 1)
        run = max(runs, key=len)
        return int(run[0]), int(run[-1]) + 1

    y0, y1 = span(fg.mean(axis=1))
    x0, x1 = span(fg[y0:y1].mean(axis=0))
    rect = rgba[y0 + INSET:y1 - INSET, x0 + INSET:x1 - INSET].copy()
    inside = fg[y0 + INSET:y1 - INSET, x0 + INSET:x1 - INSET]
    if (~inside).any():
        rect[~inside, :3] = np.median(rect[inside][:, :3], axis=0)
    rect[..., 3] = 255
    return rect


def resize(rect: np.ndarray, w: int, h: int) -> np.ndarray:
    """A float RGBA array resized (Lanczos) to w x h."""
    return np.asarray(Image.fromarray(np.clip(rect, 0, 255).astype(np.uint8), "RGBA").resize((w, h), Image.LANCZOS)).astype(np.float32)


def fit_cloth(region: np.ndarray, rect: np.ndarray, slot: str) -> np.ndarray:
    """The cloth's pixels of the region with the design stretched onto them and the old cloth's shading (float RGBA)."""
    h, w = region.shape[:2]
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
    lum = region @ LUMA
    # The fringe is only the reddish blend between cloth and ink: never past a one-pixel outline into the wall behind.
    reddish = (region[..., 0] > region[..., 1] * 1.15) & (region[..., 0] > region[..., 2] * 1.15)
    mask = np.maximum(core, np.where((lum > 45) & reddish, grown, 0))

    ys, xs = np.nonzero(mask)
    bx0, bx1, by0, by1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    placed = resize(rect, bx1 - bx0, by1 - by0)

    # The painting's folds and edges: the old cloth's brightness relative to its median, applied to the new cloth.
    median = np.median(lum[core > 0])
    shade = np.clip(lum / max(median, 1), 0.55, 1.2)

    out = np.zeros((h, w, 4), dtype=np.float32)
    sub = (slice(by0, by1), slice(bx0, bx1))
    out[sub + (slice(0, 3),)] = placed[..., :3] * shade[sub][..., None]
    out[..., 3] = mask * 255
    print(f"{slot}: cloth {bx1 - bx0}x{by1 - by0}, {int(mask.sum())} px set")
    return out


def plate_place(region: np.ndarray, slot: str, top: int) -> tuple[int, int, int]:
    """The plate's place in the region: its left and right columns (right exclusive) between the rods, and the board's top row."""
    h, w = region.shape[:2]
    lum = region @ LUMA
    sat = region.max(axis=2) - region.min(axis=2)
    labels, count = ndimage.label((sat > 40) | (lum < 75))
    left, right = [], []
    for i in range(1, count + 1):
        ys, xs = np.nonzero(labels == i)
        # A rod: a tall patch hanging from the box's top at one of its ends.
        if ys.min() > top + 1 or ys.max() - ys.min() < h // 3:
            continue
        if xs.min() < w // 4:
            left.append((xs.min(), xs.max(), ys.max()))
        elif xs.max() > 3 * w // 4:
            right.append((xs.min(), xs.max(), ys.max()))
    if not left or not right:
        sys.exit(f"{slot}: the two rods were not found in the box")
    x0 = max(r[1] for r in left) + 2
    x1 = min(r[0] for r in right) - 1
    feet = max(r[2] for r in left + right)
    # The board's top: its ink line, the first dark row between the rods under their feet.
    middle = lum[:, x0:x1].mean(axis=1)
    dark = [y for y in range(feet + 1, h) if middle[y] < 60]
    if not dark:
        sys.exit(f"{slot}: the board's top was not found under the rods")
    return x0, x1, dark[0]


def fit_plate(region: np.ndarray, rect: np.ndarray, slot: str, top: int) -> np.ndarray:
    """The header plate, contained between the rods and standing on the board, with the painting's shading (float RGBA)."""
    h, w = region.shape[:2]
    x0, x1, board = plate_place(region, slot, top)
    aspect = rect.shape[1] / rect.shape[0]
    pw = x1 - x0
    ph = int(round(pw / aspect))
    if ph > board - top:
        ph = board - top
        pw = int(round(ph * aspect))
    px0 = x0 + (x1 - x0 - pw) // 2
    py0 = board - ph
    placed = resize(rect, pw, ph)

    lum = region @ LUMA
    sub = (slice(py0, py0 + ph), slice(px0, px0 + pw))
    # The ceiling's broad light across the plate, not its details: the diffusers' glare left out, then blurred.
    broad = lum.copy()
    broad[lum > 200] = np.median(lum[sub])
    broad = ndimage.gaussian_filter(broad, 8)
    shade = np.clip(broad / max(np.median(broad[sub]), 1), 0.9, 1.08)[sub]
    shade[0, :] *= 1.12
    shade[-2:, :] *= 0.8
    rgb = placed[..., :3] * shade[..., None]
    # The painting's ink (its darkest outline pixels around the plate) as a one-pixel outline.
    ink = np.median(region[lum <= np.percentile(lum, 3)], axis=0)
    rgb[0, :] = rgb[-1, :] = ink
    rgb[:, 0] = rgb[:, -1] = ink

    out = np.zeros((h, w, 4), dtype=np.float32)
    out[sub + (slice(0, 3),)] = rgb
    out[sub + (slice(3, 4),)] = 255
    print(f"{slot}: plate {pw}x{ph} between the rods, standing on the board's top (region row {board})")
    return out


def fit(slot: str, design_path: Path, out_path: Path, painting_path: Path, preview_path: Path | None) -> None:
    spec = SLOTS[slot]
    bx, by, bw, bh = spec["box"]
    x, y = max(0, bx - MARGIN), max(0, by - MARGIN)
    w, h = min(CANVAS[0], bx + bw + MARGIN) - x, min(CANVAS[1], by + bh + MARGIN) - y
    painting = Image.open(painting_path).convert("RGB")
    if painting.size != CANVAS:
        sys.exit(f"painting is {painting.size}, expected {CANVAS}")
    region = np.asarray(painting.crop((x, y, x + w, y + h))).astype(np.float32)
    rect = printed_rect(Image.open(design_path))

    out = fit_cloth(region, rect, slot) if spec["mode"] == "cloth" else fit_plate(region, rect, slot, by - y)
    out[..., :3] = np.clip(out[..., :3], 0, 255)

    canvas = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    canvas.paste(Image.fromarray(out.astype(np.uint8), "RGBA"), (x, y))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(out_path)
    print(f"  -> {out_path}")

    if preview_path:
        comp = painting.convert("RGBA")
        comp.alpha_composite(canvas)
        pad = 40
        crop = comp.crop((max(0, x - pad), max(0, y - pad), min(CANVAS[0], x + w + pad), min(CANVAS[1], y + h + pad)))
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
