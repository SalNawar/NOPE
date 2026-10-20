"""The hall portals' painted-aperture masks and opening fits (HALL-FX, BUG-26a / BUG-73d).

    python tools/hall/portal_masks.py [--painting PNG] [--debug DIR]

Reads the hall drawing the rings are painted in (HallWarmStone.png, the art layer
"62 Approved deeper architecture"; read only, never edited) and, for each ring, finds
the hole the ring's metal encloses:

  1. "open" pixels are the wall and glass seen through the hole: bright (luma > OPEN_LUMA),
     or the warm, unsaturated wall in an arch's shade; never brass (a rail) or the cool
     grey of the ring's metal;
  2. the hole is the convex hull of the open components that meet the ring's seed
     ellipse (the hole of a ring or an arch is convex: the hull fills the bars, rails
     and clutter that cross it). An arch (rings 4, 5) has no lower half: its hole ends
     at the bay fence's top rail;
  3. what stands in front of the hole (the occluders) is the straight structure across
     it: rows or columns of the hole that are mostly not open (the fence rails, the
     mullion bar; an arch has rails only). Dark things that do not span the hole (a
     pipe on the wall behind) are behind it, so the vortex covers them;
  4. below the fence rail the hole is seen through the bay's glass.

Writes Assets/Data/Config/HallPortalMasks.png, an atlas of one crop per ring at SCALE
times the canvas, RGBA:
  R  the hole's coverage (antialiased)
  G  an occluder in front of it (the vortex keeps the painting there)
  B  seen through the glass (below the fence rail)
  A  the distance inside the hole's edge, EDGE_PX canvas px = 1 (the shader's rim)
and updates each ring's numbers in Assets/Data/Config/HallPortals.asset: the opening
(centre and radii: the R channel's moment fit, an ellipse's centre and its radii
2*sqrt(variance)), its crop on the canvas (maskCanvas) and in the atlas (maskRect),
and the reach (the farthest hole pixel in opening radii, so the iris covers it).
HallPortalsTests re-fits the PNG and checks the asset against it.
"""
import argparse
import math
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd
from scipy.spatial import ConvexHull

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
PAINTING = os.path.join(ROOT, 'Assets', 'Art', 'Office', 'AnimeHallLayers', 'Completion', 'WarmStone', 'HallWarmStone.png')
ATLAS = os.path.join(ROOT, 'Assets', 'Data', 'Config', 'HallPortalMasks.png')
ASSET = os.path.join(ROOT, 'Assets', 'Data', 'Config', 'HallPortals.asset')

OPEN_LUMA = 0.62     # the wall and glass seen through a hole are brighter than the ring's metal
SPAN = 0.7           # a row of the hole this much not open is a rail in front
BAR_SPAN = 0.45      # a column this much not open is a bar in front (a mullion may stop at the fence rail)
EDGE_PX = 6.0        # the rim's depth (canvas px) the A channel spans
SCALE = 2            # atlas px per canvas px
PAD = 4              # canvas px around each crop
MARGIN = 1.5         # PortalVortex.Margin: the effect's quad is this many opening radii each way

# portal, seed centre and radii (canvas px, read by eye on the painting: they only find the hole), fence rail row, arch
RINGS = [
    (1, 1096, 488, 49, 56, 478, False),
    (2, 968, 364, 25, 23, 364, False),
    (3, 1210, 363, 25, 23, 364, False),
    (4, 1763, 190, 16, 13, 203, True),
    (5, 2004, 188, 25, 20, 206, True),
]


# A Rect field as Unity writes it (four lines under serializedVersion).
RECT = (r'%s:\n      serializedVersion: 2\n      x: [-\d.e]+\n      y: [-\d.e]+\n'
        r'      width: [-\d.e]+\n      height: [-\d.e]+')


def rect_yaml(name, x, y, w, h):
    return (f'{name}:\n      serializedVersion: 2\n      x: {x}\n      y: {y}\n'
            f'      width: {w}\n      height: {h}')


def luma(rgb):
    return rgb @ np.array([0.299, 0.587, 0.114], np.float32)


def open_of(rgb):
    """The wall and glass seen through a hole: bright, or the warm unsaturated wall in shade; never brass."""
    L = luma(rgb)
    hi, lo = rgb.max(-1), rgb.min(-1)
    sat = (hi - lo) / np.maximum(hi, 1e-3)
    warm = rgb[..., 0] - rgb[..., 2]
    brass = (warm > 0.12) & (sat >= 0.35)
    wall = (warm > 0.04) & (sat < 0.35) & (L > 0.42)
    return ((L > OPEN_LUMA) | wall) & ~brass


def hole_of(O, n, cx, cy, rx, ry, fence, arch):
    """The hole's hull (canvas polygon), from the open components that meet the seed ellipse."""
    m = int(max(rx, ry) * 2.2)
    x0, y0 = max(0, cx - m), max(0, cy - m)
    x1, y1 = min(O.shape[1], cx + m), min(O.shape[0], cy + m)
    crop = O[y0:y1, x0:x1].copy()
    if arch:
        crop[max(0, fence - y0):, :] = False
    labels, count = nd.label(crop)
    yy, xx = np.mgrid[y0:y1, x0:x1]
    seed = ((xx - cx) / (rx * 0.75)) ** 2 + ((yy - cy) / (ry * 0.75)) ** 2 <= 1.0
    if arch:
        seed &= yy < fence
    keep = np.zeros_like(crop)
    for k in np.unique(labels[seed & crop]):
        if k == 0:
            continue
        comp = labels == k
        # a component that leaks far past the seed is the wall around the ring, not the hole
        if (comp & ~(((xx - cx) / (rx * 1.35)) ** 2 + ((yy - cy) / (ry * 1.35)) ** 2 <= 1.0)).sum() > 0.15 * comp.sum():
            continue
        keep |= comp
    py, px = np.nonzero(keep)
    pts = np.column_stack([px + x0 + 0.5, py + y0 + 0.5])
    if len(pts) < 12:
        raise SystemExit(f'portal {n}: no hole found')
    hull = ConvexHull(pts)
    return pts[hull.vertices], keep, (x0, y0)


def coverage(poly, box, scale):
    """The polygon's antialiased coverage over the canvas box (x0, y0, w, h) at scale px per canvas px (4x supersampled)."""
    x0, y0, w, h = box
    ss = scale * 4
    img = Image.new('L', (w * ss, h * ss), 0)
    ImageDraw.Draw(img).polygon([((x - x0) * ss, (y - y0) * ss) for x, y in poly], fill=255)
    return np.asarray(img.resize((w * scale, h * scale), Image.BOX), np.float32) / 255.0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--painting', default=PAINTING)
    ap.add_argument('--debug', default=None)
    args = ap.parse_args()

    rgb = np.asarray(Image.open(args.painting).convert('RGB'), np.float32) / 255.0
    O = open_of(rgb)
    H, W = O.shape
    rings = []
    for n, cx, cy, rx, ry, fence, arch in RINGS:
        poly, _, _ = hole_of(O, n, cx, cy, rx, ry, fence, arch)
        # The crop: the hull's box padded, wide enough for the effect's quad around the fit (filled below).
        bx0, by0 = int(math.floor(poly[:, 0].min())) - PAD, int(math.floor(poly[:, 1].min())) - PAD
        bx1, by1 = int(math.ceil(poly[:, 0].max())) + PAD, int(math.ceil(poly[:, 1].max())) + PAD
        box = (bx0, by0, bx1 - bx0, by1 - by0)
        R = coverage(poly, box, SCALE)
        # The fit: the coverage's moments in canvas px (an ellipse's radii are twice its standard deviations).
        gy, gx = np.mgrid[0:R.shape[0], 0:R.shape[1]]
        cxs, cys = bx0 + (gx + 0.5) / SCALE, by0 + (gy + 0.5) / SCALE
        total = R.sum()
        ex, ey = (R * cxs).sum() / total, (R * cys).sum() / total
        erx = 2.0 * math.sqrt((R * (cxs - ex) ** 2).sum() / total)
        ery = 2.0 * math.sqrt((R * (cys - ey) ** 2).sum() / total)
        inside = R > 0.5
        reach = float(np.sqrt(((cxs[inside] - ex) / erx) ** 2 + ((cys[inside] - ey) / ery) ** 2).max())
        # The painting under the crop, sampled at the atlas's scale (nearest canvas px).
        sx = np.clip(np.floor(cxs).astype(int), 0, W - 1)
        sy = np.clip(np.floor(cys).astype(int), 0, H - 1)
        dark = ~O[sy, sx]
        hole = R > 0.5
        rows = np.array([(dark[i] & hole[i]).sum() / max(1, hole[i].sum()) for i in range(R.shape[0])])
        cols = np.array([(dark[:, j] & hole[:, j]).sum() / max(1, hole[:, j].sum()) for j in range(R.shape[1])])
        across = (rows[:, None] >= SPAN) | ((cols[None, :] >= BAR_SPAN) & (not arch))
        G = (dark & across & hole).astype(np.float32)
        G = nd.binary_dilation(G > 0, iterations=1).astype(np.float32) * (R > 0)  # keep the bars' antialiased edges painted
        B = (cys > fence).astype(np.float32) * (R > 0)
        A = np.clip(nd.distance_transform_edt(R > 0.5) / (EDGE_PX * SCALE), 0, 1).astype(np.float32)
        rings.append(dict(n=n, box=box, R=R, G=G, B=B, A=A, fit=(ex, ey, erx, ery), reach=reach, arch=arch, fence=fence))

    # The atlas: the crops side by side on one row, power-of-two sized.
    width = sum(r['R'].shape[1] + 2 for r in rings)
    height = max(r['R'].shape[0] for r in rings) + 2
    aw, ah = 1 << (width - 1).bit_length(), 1 << (height - 1).bit_length()
    atlas = np.zeros((ah, aw, 4), np.float32)
    x = 1
    for r in rings:
        h, w = r['R'].shape
        y = 1
        atlas[y:y + h, x:x + w] = np.stack([r['R'], r['G'], r['B'], r['A']], -1)
        # maskRect in texture coordinates (y up), the crop's lower-left corner and size
        r['rect'] = (x / aw, (ah - (y + h)) / ah, w / aw, h / ah)
        x += w + 2
    Image.fromarray(np.round(atlas * 255).astype(np.uint8), 'RGBA').save(ATLAS, optimize=True)

    text = open(ASSET, encoding='utf-8').read()
    for r in rings:
        ex, ey, erx, ery = r['fit']
        bx, by, bw, bh = r['box']
        ux, uy, uw, uh = r['rect']
        block = re.search(r'(  - portal: %d\n(?:    .*\n)+?)(?=  - portal:|  [a-z])' % r['n'], text)
        if block is None:
            raise SystemExit(f'portal {r["n"]}: no block in {ASSET}')
        b = block.group(1)
        b = re.sub(r'opening: \{[^}]*\}', 'opening: {x: %.2f, y: %.2f, z: %.2f, w: %.2f}' % (ex, ey, erx, ery), b)
        b = re.sub(RECT % 'maskCanvas', lambda m: rect_yaml('maskCanvas', '%d' % bx, '%d' % by, '%d' % bw, '%d' % bh), b)
        b = re.sub(RECT % 'maskRect', lambda m: rect_yaml('maskRect', '%.10g' % ux, '%.10g' % uy, '%.10g' % uw, '%.10g' % uh), b)
        b = re.sub(r'reach: [-\d.e]+', 'reach: %.4f' % r['reach'], b)
        text = text.replace(block.group(1), b)
        print(f'portal {r["n"]}: centre ({ex:.1f},{ey:.1f}) radii {erx:.1f}x{ery:.1f} reach {r["reach"]:.2f} crop {r["box"]}'
              f' (seed {[x for x in RINGS if x[0] == r["n"]][0][1:5]}); the quad holds the hole: {r["reach"] <= MARGIN}')
    with open(ASSET, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)

    if args.debug:
        os.makedirs(args.debug, exist_ok=True)
        Image.fromarray(np.round(atlas * 255).astype(np.uint8), 'RGBA').save(os.path.join(args.debug, 'atlas.png'))
        for r in rings:
            bx, by, bw, bh = r['box']
            crop = (rgb[by:by + bh, bx:bx + bw] * 255).astype(np.uint8)
            img = Image.fromarray(crop).resize((bw * 6, bh * 6), Image.NEAREST).convert('RGB')
            over = np.asarray(img, np.float32)
            R = np.kron(r['R'], np.ones((3, 3)))
            G = np.kron(r['G'], np.ones((3, 3)))
            B = np.kron(r['B'], np.ones((3, 3)))
            tint = np.zeros_like(over)
            tint[..., 0] = 255 * G
            tint[..., 1] = 255 * R * (1 - G) * (1 - B)
            tint[..., 2] = 255 * R * B * (1 - G)
            over = over * (1 - 0.45 * R[..., None]) + tint * 0.45 * R[..., None]
            img = Image.fromarray(np.clip(over, 0, 255).astype(np.uint8))
            d = ImageDraw.Draw(img)
            ex, ey, erx, ery = r['fit']
            d.ellipse([(ex - erx - bx) * 6, (ey - ery - by) * 6, (ex + erx - bx) * 6, (ey + ery - by) * 6], outline=(255, 0, 255))
            img.save(os.path.join(args.debug, f'portal{r["n"]}.png'))


if __name__ == '__main__':
    sys.exit(main())
