"""Where a hall slot's two faces both cover, on a QA-RIG sequence's frames (HALL-FX BUG-71: the crossfade's measure).

    python tools/hall/slot_mask.py <run folder> <seq name> --slot 13-flag-left-cloth [--files a.png b.png] [--out PNG]

The hall slots' art (Assets/Art/UI/Resources/Hall/Slots/<slot>/<file>.png) is drawn on the hall drawing's canvas
(HallSlotsLink.Show: a full-canvas picture on the painting's bounds), so a pixel of the art is a pixel of the canvas.
The crossfade's promise (SlotCrossfade: one face is opaque at every moment wherever both cover, so the painting never
shows through) holds only where both faces are opaque; at an art's antialiased edge, or where one face is
transparent, the blend may legitimately move one way and then the other. This tool writes that region as a mask
on the sequence's frames: the pixels whose centre falls where every art named (--files, default every art of the
slot) is opaque (alpha >= 250 of 255), shrunk by one capture pixel so no measured pixel straddles an edge.

The canvas is laid on the frames by the hall drawing's own projection (HallFxRig's "hallDrawing" op: its four canvas
corners on screen, in qa_report.txt's "== drawing at <w>x<h>" block for the sequence's screen size), scaled from
the screen to the capture (frames.tsv's screenW/capW). Writes <seq>/mask_<slot>.png (white: measured) unless --out
names another path, and prints the pixel count; region_luma.py --mask reads it.
"""
import argparse
import csv
import glob
import os
import re
import sys

import numpy as np
from PIL import Image
from scipy import ndimage as nd

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from portal_check import homography, warp  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
SLOTS = os.path.join(ROOT, 'Assets', 'Art', 'UI', 'Resources', 'Hall', 'Slots')


def drawing_at(report, w, h):
    """The last '== drawing at <w>x<h>' projection in qa_report.txt: (canvas w, canvas h, [TL, TR, BL, BR]) y up."""
    found, cur = None, False
    num = r'\(([\d.\-]+),([\d.\-]+)\)'
    for line in open(report, encoding='utf-8').read().splitlines():
        m = re.match(r'== drawing at (\d+)x(\d+)', line)
        if m:
            cur = (int(m.group(1)), int(m.group(2))) == (w, h)
            continue
        m = re.match(r'\s+drawing canvas (\d+)x(\d+) corners ' + ' '.join([num] * 4), line)
        if m and cur:
            v = [float(g) for g in m.groups()]
            found = (v[0], v[1], [(v[2], v[3]), (v[4], v[5]), (v[6], v[7]), (v[8], v[9])])
        elif line.startswith('=='):
            cur = False
    return found


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('run')
    ap.add_argument('seq')
    ap.add_argument('--slot', required=True)
    ap.add_argument('--files', nargs='*', default=None)
    ap.add_argument('--out', default=None)
    args = ap.parse_args()
    seq = os.path.join(args.run, 'seq', args.seq)
    with open(os.path.join(seq, 'frames.tsv'), encoding='utf-8') as f:
        row = next(csv.DictReader(f, delimiter='\t'))
    sw, sh, cw, ch = int(row['screenW']), int(row['screenH']), int(row['capW']), int(row['capH'])
    drawing = drawing_at(os.path.join(args.run, 'qa_report.txt'), sw, sh)
    if drawing is None:
        raise SystemExit(f'no "== drawing at {sw}x{sh}" projection in {args.run}/qa_report.txt (the scenario needs a hallDrawing step)')
    folder = os.path.join(SLOTS, args.slot)
    files = [os.path.join(folder, f) for f in args.files] if args.files else sorted(glob.glob(os.path.join(folder, '*.png')))
    if not files:
        raise SystemExit(f'no art in {folder}')
    opaque = None
    for path in files:
        alpha = np.asarray(Image.open(path).convert('RGBA'))[..., 3] >= 250
        opaque = alpha if opaque is None else opaque & alpha
    cwid, chei, corners = drawing
    if opaque.shape != (int(chei), int(cwid)):
        raise SystemExit(f'{files[0]} is {opaque.shape[1]}x{opaque.shape[0]}, the canvas {cwid:.0f}x{chei:.0f}')
    canvas = [(0.0, 0.0), (cwid, 0.0), (0.0, chei), (cwid, chei)]
    screen = [(x, sh - y) for x, y in corners]
    inv = np.linalg.inv(homography(canvas, screen))
    kx, ky = sw / cw, sh / ch
    py, px = np.mgrid[0:ch, 0:cw]
    x, y = warp(inv, (px + 0.5) * kx, (py + 0.5) * ky)
    xi, yi = np.floor(x).astype(int), np.floor(y).astype(int)
    on = (xi >= 0) & (yi >= 0) & (xi < opaque.shape[1]) & (yi < opaque.shape[0])
    mask = np.zeros((ch, cw), bool)
    mask[on] = opaque[yi[on], xi[on]]
    mask = nd.binary_erosion(mask, iterations=1)
    out = args.out or os.path.join(seq, f'mask_{args.slot}.png')
    Image.fromarray((mask * 255).astype(np.uint8)).save(out)
    print(f'{out}: {int(mask.sum())} px where all {len(files)} faces of {args.slot} are opaque')
    return 0 if mask.any() else 1


if __name__ == '__main__':
    sys.exit(main())
