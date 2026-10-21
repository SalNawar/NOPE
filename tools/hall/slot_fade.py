"""Does the painting show through a hall slot's crossfade? (HALL-FX BUG-71; docs/pending/HALL-FX.md RESULT step 9.)

    python tools/hall/slot_fade.py <run folder> <seq name> --slot 13-flag-left-cloth [--max-share 0.05]

The promise (SlotCrossfade): wherever both of a slot's faces cover, one of them is opaque at every moment, so the
painting under the slot never shows through; before the fix a Fade / 1 - Fade blend showed a quarter of it at
mid-fade. A luma curve cannot see that where the painting and the two faces have about the same luma (the left flag
and the red wall behind it), so this measures it in colour, on the pixels where both faces are opaque
(tools/hall/slot_mask.py's mask):

  O, N   each pixel's colour at the sequence's first and last frames (the outgoing and the incoming face);
  P      the painting's colour there: HallWarmStone.png laid by the drawing's projection (the hallDrawing step) as it
         is, since hall_fx_crossfade forces 12:00, where the hall's four-state light is white (the integration's probe
         still with both faces hidden matched the PNG at the flag within 1 of 255);
  share  per frame and pixel, how far the colour has come toward the painting past the nearer of O and N, as a share
         of that face's distance from it: max(0, min(|O - P|, |N - P|) - |C - P|) / min(|O - P|, |N - P|); 0 while
         the colour stays between the faces or away from the painting, 0.25 for a quarter of the painting showing.

Frames are read with QA-RIG's extra sRGB encode undone (region_luma.decode), and colours are compared in linear
light, where the blend happens. Prints the worst frame's mean share and the share of mask pixels over --max-share
in it; exit 0 when the mean share stays at most --max-share (FIX_PLAN: the crossfade "steady (+-5%)").
"""
import argparse
import csv
import glob
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from portal_check import PAINTING, homography, warp  # noqa: E402
from region_luma import decode  # noqa: E402
from slot_mask import drawing_at  # noqa: E402


def linear(img):
    """0-255 sRGB (once encoded) to linear 0-1."""
    v = img / 255.0
    return np.where(v <= 0.04045, v / 12.92, ((v + 0.055) / 1.055) ** 2.4)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('run')
    ap.add_argument('seq')
    ap.add_argument('--slot', required=True)
    ap.add_argument('--max-share', type=float, default=0.05)
    args = ap.parse_args()
    seq = os.path.join(args.run, 'seq', args.seq)
    mask_path = os.path.join(seq, f'mask_{args.slot}.png')
    if not os.path.exists(mask_path):
        raise SystemExit(f'no {mask_path}: run tools/hall/slot_mask.py {args.run} {args.seq} --slot {args.slot} first')
    mask = np.asarray(Image.open(mask_path).convert('L')) >= 128
    with open(os.path.join(seq, 'frames.tsv'), encoding='utf-8') as f:
        row = next(csv.DictReader(f, delimiter='\t'))
    sw, sh, cw, ch = int(row['screenW']), int(row['screenH']), int(row['capW']), int(row['capH'])
    files = sorted(glob.glob(os.path.join(seq, 'f*.png')))
    frames = np.stack([linear(decode(np.asarray(Image.open(p).convert('RGB'), np.float32))) for p in files])

    # The painting under each capture pixel, by the drawing's projection.
    cwid, chei, corners = drawing_at(os.path.join(args.run, 'qa_report.txt'), sw, sh)
    inv = np.linalg.inv(homography([(0.0, 0.0), (cwid, 0.0), (0.0, chei), (cwid, chei)], [(x, sh - y) for x, y in corners]))
    py, px = np.mgrid[0:ch, 0:cw]
    x, y = warp(inv, (px + 0.5) * sw / cw, (py + 0.5) * sh / ch)
    painting = linear(np.asarray(Image.open(PAINTING).convert('RGB'), np.float32))
    xi = np.clip(np.floor(x).astype(int), 0, painting.shape[1] - 1)
    yi = np.clip(np.floor(y).astype(int), 0, painting.shape[0] - 1)
    paint = painting[yi, xi]

    P = paint

    O, N = frames[0][mask], frames[-1][mask]
    Pm = P[mask]
    near = np.minimum(np.linalg.norm(O - Pm, axis=1), np.linalg.norm(N - Pm, axis=1))
    usable = near > 0.02
    worst, worst_i, worst_over = 0.0, 0, 0.0
    for i, frame in enumerate(frames):
        C = frame[mask]
        share = np.maximum(0.0, near - np.linalg.norm(C - Pm, axis=1)) / np.maximum(near, 1e-9)
        s = float(share[usable].mean())
        if s > worst:
            worst, worst_i, worst_over = s, i, float((share[usable] > args.max_share).mean())
    ok = worst <= args.max_share
    print(f'{args.seq} {args.slot}: {int(usable.sum())} px (of {int(mask.sum())}) where a face differs from the painting; the painting '
          f'as painted (12:00); worst frame {worst_i}: mean share toward the '
          f'painting {worst:.4f}, {worst_over:.1%} of the pixels over {args.max_share} (pass <= {args.max_share}): {"PASS" if ok else "FAIL"}')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
