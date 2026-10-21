"""The traffic's tint by the hour (HALL-FX, CODE-69; docs/pending/HALL-FX.md RESULT step 6): a car's own body at H5 against H2.

    python tools/hall/traffic_tint.py <seq H5> <seq H2> --track NAME[,NAME...] [--max-ratio 0.5]

The cars are tracked objects (QA-RIG's "track": frames.tsv's per-frame screen rects, capture px). A car's rect also
holds what is behind it (the sky, the night city's lit windows), so a statistic over the rect measures the
background as much as the car (the first recipe's p90 read the lit windows behind a night car). This tool measures
the car's own pixels:

  1. each sequence's background is its per-pixel temporal median (the hour is frozen and a car crosses a pixel
     in a few frames, so the median is the scene without it);
  2. a frame's car pixels are those differing from the background by more than --diff luma inside the car's rect;
     resampled to a GRID x GRID cell grid in rect coordinates (the rect is the sprite's bounds, so a cell is the
     same part of the car in every frame), the cells found in at least half of the frames, over both sequences,
     are the car's body: one shape for both hours, so a dark night car on a dark sky is measured where a noon car
     was found, not only where it still stands out;
  3. a car's luma at an hour is the mean over its frames of the mean luma of its body cells.

The frames are read with QA-RIG's extra sRGB encode undone (region_luma.decode: the sequence frames are encoded
twice, so a ratio read on them is about its square root; a QA-RIG finding, integration 2026-10-21), so the ratio is
the screen's.

Prints each car's H5 / H2 ratio and the mean over the cars seen in both; exit 0 when that mean is at most
--max-ratio (the plan: mean traffic luma at H5 at most 50 % of H2). Writes nothing.
"""
import argparse
import csv
import glob
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from region_luma import decode  # noqa: E402

GRID = 12


def luma(path):
    """A frame's luma (Rec. 601, 0-255) with the recorder's extra sRGB encode undone (region_luma.decode): the screen's own."""
    return decode(np.asarray(Image.open(path).convert('RGB'), np.float32)) @ np.array([0.299, 0.587, 0.114], np.float32)


def load(seq, names, step):
    files = sorted(glob.glob(os.path.join(seq, 'f*.png')))
    with open(os.path.join(seq, 'frames.tsv'), encoding='utf-8') as f:
        rows = list(csv.DictReader(f, delimiter='\t'))
    frames = []
    for i in range(0, min(len(files), len(rows)), step):
        rects = {}
        for part in (rows[i].get('tracked') or '').split(';'):
            if ':' in part:
                n, c = part.split(':', 1)
                if n in names:
                    rects[n] = tuple(float(v) for v in c.split(','))
        frames.append((luma(files[i]), rects))
    background = np.median(np.stack([f for f, _ in frames]), axis=0)
    return frames, background


def cells(img, rect):
    """The rect's luma resampled to GRID x GRID cells (None when the rect is not wholly on the frame)."""
    h, w = img.shape
    x0, y0, x1, y1 = rect
    if x0 < 0 or y0 < 0 or x1 > w or y1 > h or x1 - x0 < 3 or y1 - y0 < 2:
        return None
    xs = np.clip(((np.arange(GRID) + 0.5) / GRID * (x1 - x0) + x0).astype(int), 0, w - 1)
    ys = np.clip(((np.arange(GRID) + 0.5) / GRID * (y1 - y0) + y0).astype(int), 0, h - 1)
    return img[np.ix_(ys, xs)]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('night')
    ap.add_argument('day')
    ap.add_argument('--track', required=True)
    ap.add_argument('--max-ratio', type=float, default=0.5)
    ap.add_argument('--diff', type=float, default=24.0)
    ap.add_argument('--step', type=int, default=3)
    args = ap.parse_args()
    names = args.track.split(',')
    seqs = [load(args.night, names, args.step), load(args.day, names, args.step)]
    ratios = []
    for name in names:
        found, samples = [], [[], []]
        for k, (frames, background) in enumerate(seqs):
            for img, rects in frames:
                if name not in rects:
                    continue
                c, b = cells(img, rects[name]), cells(background, rects[name])
                if c is None:
                    continue
                found.append(np.abs(c - b) > args.diff)
                samples[k].append(c)
        if not samples[0] or not samples[1]:
            print(f'{name}: not on screen in both sequences')
            continue
        body = np.mean(found, axis=0) >= 0.5
        if body.sum() < 4:
            print(f'{name}: no body found ({int(body.sum())} cells)')
            continue
        night = float(np.mean([s[body].mean() for s in samples[0]]))
        day = float(np.mean([s[body].mean() for s in samples[1]]))
        ratios.append(night / max(day, 1e-6))
        print(f'{name}: body {int(body.sum())} cells, H5 {night:.1f} over {len(samples[0])} frames / H2 {day:.1f} over {len(samples[1])} frames = {ratios[-1]:.3f}')
    if not ratios:
        print('no car measured: FAIL')
        return 1
    mean = float(np.mean(ratios))
    ok = mean <= args.max_ratio
    print(f'mean over {len(ratios)} cars: {mean:.3f} (pass <= {args.max_ratio}): {"PASS" if ok else "FAIL"}')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
