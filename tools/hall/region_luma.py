"""A region's luma over a QA-RIG sequence (HALL-FX's steadiness measures).

    python tools/hall/region_luma.py <seq folder> (--roi x0,y0,x1,y1 | --track NAME[,NAME...]) [--stat mean|p90]
                                     [--max-range R] [--from S] [--to S] [--fps 30]
    python tools/hall/region_luma.py <seq folder> (--roi ... | --track ...) --monotonic [--tolerance 0.05]
    python tools/hall/region_luma.py <seq folder> (--roi ... | --track ...) --dips N [--min-depth 10]
    python tools/hall/region_luma.py --compare <seqA> <seqB> (--roi ... | --track ...) [--stat ...] [--max-ratio Q]

The region is either
  --roi    fixed screen fractions, origin top-left (0,0 to 1,1), so one ROI means the same place at every aspect; or
  --track  the rects the scenario tracked (QA-RIG's "track" op: frames.tsv's tracked column, capture px, per frame),
           so the region follows the object at every aspect and as it moves; several names: the mean of each
           one's statistic (each car on its own, not the sky between them). A frame where none of them is on screen
           is skipped.
Frames are the sequence's f0000.png ... (SeqRecorder, 30 fps). Luma is Rec. 601 on 0-255. The statistic over the
region is its mean, or with --stat p90 the 90th percentile (a small lit object in a tracked rect: the car's body and
lights, not the sky behind it).

  steady (default): prints the statistic per second and its range (max - min) over the frames from --from to --to
                    seconds; exit 0 when the range is at most --max-range (the AVAILABLE sign's +-3: --max-range 6).
  --monotonic:      the hall slot crossfade (BUG-71): from its first frames' level to its last frames', the series
                    never leaves that span by more than --tolerance of it, and never turns back by more than that
                    (a dip toward the painting fails); prints the per-frame values.
  --dips N:         the AVAILABLE sign's invitation (BUG-67): exactly N dips, each at least --min-depth luma under
                    the levels either side of it, then steady; prints the per-frame values and each dip's time.
  --compare:        the statistic of A's region over B's (the traffic tint: H5 over H2, pass at most --max-ratio 0.5).

It writes nothing; the Unity pass copies its output into R7/FIX/HALL-FX/RESULT.md.
"""
import argparse
import csv
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


def tracked(seq):
    """frames.tsv's tracked rects per frame index: {i: {name: (x0, y0, x1, y1)}} in capture px."""
    path = os.path.join(seq, 'frames.tsv')
    if not os.path.exists(path):
        raise SystemExit(f'no frames.tsv in {seq}: --track needs a QA-RIG sequence')
    out = {}
    with open(path, encoding='utf-8') as f:
        for row in csv.DictReader(f, delimiter='\t'):
            rects = {}
            for part in (row.get('tracked') or '').split(';'):
                if ':' not in part:
                    continue
                name, coords = part.split(':', 1)
                try:
                    rects[name] = tuple(float(v) for v in coords.split(','))
                except ValueError:
                    continue
            out[int(row['i'])] = rects
    return out


def region_stat(path, box, stat, fractions):
    """The statistic of the luma inside box (x0, y0, x1, y1): fractions of the frame, else capture px."""
    img = np.asarray(Image.open(path).convert('RGB'), np.float32)
    h, w = img.shape[:2]
    x0, y0, x1, y1 = box
    if fractions:
        x0, x1, y0, y1 = x0 * w, x1 * w, y0 * h, y1 * h
    x0, y0 = max(0, int(x0)), max(0, int(y0))
    x1, y1 = min(w, max(x0 + 1, int(round(x1)))), min(h, max(y0 + 1, int(round(y1))))
    if x1 <= x0 or y1 <= y0:
        return None
    luma = img[y0:y1, x0:x1] @ np.array([0.299, 0.587, 0.114], np.float32)
    return float(np.percentile(luma, 90)) if stat == 'p90' else float(luma.mean())


def series(seq, args, start, end):
    files = frames(seq)
    a = int(round(start * args.fps))
    b = len(files) if end is None else min(len(files), int(round(end * args.fps)))
    rects = tracked(seq) if args.track else None
    names = args.track.split(',') if args.track else []
    values = []
    for i in range(a, b):
        if rects is not None:
            boxes = [r for n, r in rects.get(i, {}).items() if n in names]
        else:
            boxes = [args.roi]
        stats = [v for v in (region_stat(files[i], box, args.stat, rects is None) for box in boxes) if v is not None]
        if stats:
            values.append(float(np.mean(stats)))
    if not values:
        raise SystemExit(f'{seq}: the region is on no frame')
    return values


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('seq', nargs='?')
    ap.add_argument('--compare', nargs=2)
    ap.add_argument('--roi', type=lambda s: tuple(float(v) for v in s.split(',')))
    ap.add_argument('--track')
    ap.add_argument('--stat', choices=['mean', 'p90'], default='mean')
    ap.add_argument('--max-range', type=float, default=6.0)
    ap.add_argument('--max-ratio', type=float, default=0.5)
    ap.add_argument('--monotonic', action='store_true')
    ap.add_argument('--tolerance', type=float, default=0.05)
    ap.add_argument('--dips', type=int, default=None)
    ap.add_argument('--min-depth', type=float, default=10.0)
    ap.add_argument('--from', dest='start', type=float, default=0.0)
    ap.add_argument('--to', dest='end', type=float, default=None)
    ap.add_argument('--fps', type=float, default=30.0)
    args = ap.parse_args()
    if (args.roi is None) == (args.track is None):
        ap.error('one of --roi or --track')
    if args.compare:
        a = np.mean(series(args.compare[0], args, args.start, args.end))
        b = np.mean(series(args.compare[1], args, args.start, args.end))
        ratio = a / max(b, 1e-6)
        ok = ratio <= args.max_ratio
        print(f'region luma ({args.stat}) {a:.1f} / {b:.1f} = {ratio:.3f} (pass <= {args.max_ratio}): {"PASS" if ok else "FAIL"}')
        return 0 if ok else 1
    if not args.seq:
        ap.error('a sequence folder, or --compare A B')
    values = series(args.seq, args, args.start, args.end)
    if args.dips is not None:
        # A dip: a local minimum whose rise to the highest level on either side, before a lower one, is min-depth.
        found = []
        for i in range(1, len(values) - 1):
            if not (values[i] < values[i - 1] and values[i] <= values[i + 1]):
                continue
            left = max(values[:i])
            right = max(values[i + 1:])
            if min(left, right) - values[i] >= args.min_depth:
                if not found or i - found[-1] > 3:
                    found.append(i)
        ok = len(found) == args.dips
        print('per frame: ' + ' '.join(f'{v:.1f}' for v in values))
        print(f'{len(found)} dips at ' + ', '.join(f'{(i + int(round(args.start * args.fps))) / args.fps:.2f} s' for i in found)
              + f' (pass: exactly {args.dips}): {"PASS" if ok else "FAIL"}')
        return 0 if ok else 1
    if args.monotonic:
        k = max(1, min(5, len(values) // 6))
        first, last = float(np.mean(values[:k])), float(np.mean(values[-k:]))
        span = abs(last - first)
        slack = args.tolerance * max(span, 1.0)
        lo, hi = min(first, last) - slack, max(first, last) + slack
        rising = last >= first
        worst_out = max(max(0.0, lo - v, v - hi) for v in values)
        best, worst_back = values[0], 0.0
        for v in values:
            best = max(best, v) if rising else min(best, v)
            worst_back = max(worst_back, (best - v) if rising else (v - best))
        ok = worst_out <= 0.0 and worst_back <= slack
        print('per frame: ' + ' '.join(f'{v:.1f}' for v in values))
        print(f'{len(values)} frames, {first:.1f} -> {last:.1f} (span {span:.1f}), outside the span by {worst_out:.2f}, turned back by '
              f'{worst_back:.2f} (pass: 0 and <= {slack:.2f}): {"PASS" if ok else "FAIL"}')
        return 0 if ok else 1
    per_second = [np.mean(values[i:i + int(args.fps)]) for i in range(0, len(values), int(args.fps))]
    spread = max(values) - min(values)
    ok = spread <= args.max_range
    print('per second: ' + ' '.join(f'{v:.1f}' for v in per_second))
    print(f'{len(values)} frames, mean {np.mean(values):.1f}, range {spread:.2f} (pass <= {args.max_range}): {"PASS" if ok else "FAIL"}')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
