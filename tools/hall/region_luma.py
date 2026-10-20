"""A region's mean luma over a QA-RIG sequence (HALL-FX's steadiness measures).

    python tools/hall/region_luma.py <seq folder> --roi x0,y0,x1,y1 [--max-range R] [--from S] [--to S] [--fps 30]
    python tools/hall/region_luma.py --compare <seqA> <seqB> --roi ... [--max-ratio Q]

The ROI is in screen fractions, origin top-left (0,0 to 1,1), so one ROI means the same place at every aspect.
Frames are the sequence's f0000.png ... (SeqRecorder, 30 fps). Luma is Rec. 601 on 0-255.

  steady (default): prints the region's mean luma per second and its range (max - min) over the frames from --from to
                    --to seconds; exit 0 when the range is at most --max-range (the AVAILABLE sign's +-3: --max-range 6;
                    the hall slots' mid-fade +-5 %: use --max-range on the crossfade's frames).
  --compare:        the mean of A's region over the mean of B's (the traffic tint: H5 over H2, pass at most --max-ratio 0.5).

It writes nothing; the Unity pass copies its output into R7/FIX/HALL-FX/RESULT.md.
"""
import argparse
import glob
import os
import sys

import numpy as np
from PIL import Image


def frames(seq):
    files = sorted(glob.glob(os.path.join(seq, 'f*.png')))
    if not files:
        raise SystemExit(f'no frames in {seq}')
    return files


def region_mean(path, roi):
    img = np.asarray(Image.open(path).convert('RGB'), np.float32)
    h, w = img.shape[:2]
    x0, y0, x1, y1 = roi
    crop = img[int(y0 * h):max(int(y0 * h) + 1, int(y1 * h)), int(x0 * w):max(int(x0 * w) + 1, int(x1 * w))]
    return float((crop @ np.array([0.299, 0.587, 0.114], np.float32)).mean())


def series(seq, roi, fps, start, end):
    files = frames(seq)
    a = int(round(start * fps))
    b = len(files) if end is None else min(len(files), int(round(end * fps)))
    return [region_mean(f, roi) for f in files[a:b]]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('seq', nargs='?')
    ap.add_argument('--compare', nargs=2)
    ap.add_argument('--roi', required=True)
    ap.add_argument('--max-range', type=float, default=6.0)
    ap.add_argument('--max-ratio', type=float, default=0.5)
    ap.add_argument('--from', dest='start', type=float, default=0.0)
    ap.add_argument('--to', dest='end', type=float, default=None)
    ap.add_argument('--fps', type=float, default=30.0)
    args = ap.parse_args()
    roi = tuple(float(v) for v in args.roi.split(','))
    if args.compare:
        a = np.mean(series(args.compare[0], roi, args.fps, args.start, args.end))
        b = np.mean(series(args.compare[1], roi, args.fps, args.start, args.end))
        ratio = a / max(b, 1e-6)
        ok = ratio <= args.max_ratio
        print(f'region luma {a:.1f} / {b:.1f} = {ratio:.3f} (pass <= {args.max_ratio}): {"PASS" if ok else "FAIL"}')
        return 0 if ok else 1
    if not args.seq:
        ap.error('a sequence folder, or --compare A B')
    values = series(args.seq, roi, args.fps, args.start, args.end)
    per_second = [np.mean(values[i:i + int(args.fps)]) for i in range(0, len(values), int(args.fps))]
    spread = max(values) - min(values)
    ok = spread <= args.max_range
    print('per second: ' + ' '.join(f'{v:.1f}' for v in per_second))
    print(f'{len(values)} frames, mean {np.mean(values):.1f}, range {spread:.2f} (pass <= {args.max_range}): {"PASS" if ok else "FAIL"}')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
