"""The hall portals' registration check (HALL-FX, BUG-26a; the pass of docs/pending/HALL-FX.md RESULT step 10).

    python tools/hall/portal_check.py <run folder> [--out DIR]

Reads what HallFxRig's hallPortalsShot op writes into a QA-RIG run folder: qa_report.txt's "== portals at <hour>
<w>x<h>" blocks (each PortalEffect's screen quad, and the hall drawing's own projection: its canvas corners on
screen) and shots/portals/h<hour>_<w>x<h>_on.png / _off.png (the same frame with the vortexes shown and hidden).
For each showing ring:

  1. where the painted hole really is on screen: the hall drawing (HallWarmStone.png) is a flat sprite, so its four
     projected canvas corners give the exact canvas-to-screen map (a homography) the GPU draws the painting with; the
     ring's aperture mask (HallPortalMasks.png) is laid by that map. It does not depend on PortalEffect's own
     placement (LocalQuad, MaskMap, the quad's transform, the shader's mask sampling), which is what is measured.
     The map is checked on the OFF frame: 16 px tiles of the painting, warped by it around each ring, are matched
     to the frame by normalised cross-correlation (+-TILE_SEARCH px, sub-pixel); their median offset is the
     "painting" column, and over PAINT_PX the ring fails (the painting is not where its sprite projects, so the
     mask would not be either). The ring's crop alone registered by cross-correlation (the first version's
     registration) is printed as a reference column: on the arches it matches at 0.69-0.85 and wanders 1-3 px
     (integration, 2026-10-21), too coarse for a 2 px / 3 % pass. A run without the projection (older runs)
     falls back on that registration;
  2. the vortex is the pixels the effect changes (|on - off| > DIFF, holes filled), less what stands in front;
  3. hole      the registered aperture (mask R >= 0.5) less what stands in front of it (G >= 0.5);
     centre    the vortex's centroid against the hole's (screen px);           pass: within CENTRE_PX
     axes      the vortex's radii (2 standard deviations) against the hole's;  pass: within AXES
     halo      changed pixels outside the aperture grown by HALO_GROW px;      pass: 0 (taken before LIGHT's bloom)
     cover     the share of the hole the vortex changes (reported; a dark swirl tone may match the painting)
     match     the crop registration's correlation (reported; under 0.6 the frame did not find the ring: look by eye).

The check fails closed (the orchestrator's ruling at the six tracks' landing, 2026-10-21, on the verifier's finding
that it failed open). A ring whose hole lies wholly off the frame FAILS, unless it is declared with --allow-off
RING@WxH (ring 5 at 1280x1024: its quad starts past the screen's right edge, so hall_fx_portals runs with
--allow-off 5@1280x1024) and a ring of the same frame was measured with its painting check passing, which proves
the projection that put it off screen. A block with no ring measured FAILS, a ring of HallPortals.asset the block
does not report FAILS, and a report with no portal block FAILS. A ring the frame's edge cuts is measured on the
part both sides show.

It also prints, for reference, the audit's ray rule (R7 AUDIT VIS scripts/portals.py) on the off frame: rays from the
effect's centre to the first run of 3 px under luma 105, an ellipse fitted to where they stop. That rule counts lit
metal (a ring's inner lip seen from above) and the bay's lower panel as hole, so it reads 5-20 % taller than the
aperture on rings 2-5 (offline, on the painting itself: ring 2 0.84, ring 3 0.91, ring 4 0.89, ring 5 0.91 of its
height); it is not the pass.

Writes <out>/portal_check.txt and overlays portal_check_h<hour>_<w>x<h>.png (hole green, vortex magenta, halo
yellow). Exit 0 when every measured ring passes. The Unity pass copies the report into R7/FIX/HALL-FX/RESULT.md.
"""
import argparse
import math
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as nd

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ATLAS = os.path.join(ROOT, 'Assets', 'Data', 'Config', 'HallPortalMasks.png')
PAINTING = os.path.join(ROOT, 'Assets', 'Art', 'Office', 'AnimeHallLayers', 'Completion', 'WarmStone', 'HallWarmStone.png')
ASSET = os.path.join(ROOT, 'Assets', 'Data', 'Config', 'HallPortals.asset')

MARGIN = 1.5      # PortalVortex.Margin: the quad is this many opening radii each way
DIFF = 14         # an effect pixel: a channel changes by more than this between on and off
CENTRE_PX = 2.0   # FIX_PLAN: centre error <= 2 px
AXES = 0.03       # FIX_PLAN: axes within +-3 %
HALO_GROW = 2     # px the aperture is grown by before a changed pixel counts as halo (antialiasing, the registration's half pixel)
SEARCH_PX = 10    # the registration's search around the quad's placement, screen px each way
SEARCH_SCALE = 0.06  # and its scale search, each way
TILE = 16         # the painting check's tiles, screen px
TILE_SEARCH = 3   # and their search, screen px each way
PAINT_PX = 0.5    # the painting's median offset from its projection, at most, screen px per axis


def rings_of(asset):
    """HallPortals.asset's rings: {portal: (cx, cy, rx, ry, crop x, y, w, h, atlas u, v, uw, vh)}."""
    text = open(asset, encoding='utf-8').read().replace('\r\n', '\n')
    num = r'(-?[\d.eE+-]+)'
    rect = lambda name: name + r':\n\s+serializedVersion: 2\n\s+x: ' + num + r'\n\s+y: ' + num + r'\n\s+width: ' + num + r'\n\s+height: ' + num
    block = re.compile(r'  - portal: (\d+)\n(?:    .*\n)+?    opening: \{x: ' + num + ', y: ' + num + ', z: ' + num + ', w: ' + num
                       + r'\}\n    reach: ' + num + r'\n    ' + rect('maskCanvas') + r'\n    ' + rect('maskRect'))
    out = {}
    for m in block.finditer(text):
        v = [float(g) for g in m.groups()[1:]]
        out[int(m.group(1))] = (v[0], v[1], v[2], v[3]) + tuple(v[5:9]) + tuple(v[9:13])
    return out


def blocks_of(report):
    """qa_report.txt's portal blocks: {(hour, w, h): [effect dicts]} (the format HallFxRig writes)."""
    blocks, cur = {}, None
    for line in open(report, encoding='utf-8').read().splitlines():
        m = re.match(r'== portals at (\d+) (\d+)x(\d+)', line)
        if m:
            cur = (int(m.group(1)), int(m.group(2)), int(m.group(3)))
            blocks[cur] = []
            continue
        m = re.match(r'\s+PortalEffect_(\d+) showing (\w+) night ([\d.]+) centre \(([\d.\-]+),([\d.\-]+)\) '
                     r'quad \(([\d.\-]+),([\d.\-]+)\)-\(([\d.\-]+),([\d.\-]+)\)', line)
        if m and cur:
            blocks[cur].append(dict(n=int(m.group(1)), showing=m.group(2) == 'True', x0=float(m.group(6)), y0=float(m.group(7)),
                                    x1=float(m.group(8)), y1=float(m.group(9))))
        elif line.startswith('=='):
            cur = None
    return blocks


def drawings_of(report):
    """
    qa_report.txt's hall drawing projections: {(hour, w, h): (canvas w, canvas h, [TL, TR, BL, BR])} with the corners
    in screen px, y up (HallFxRig's "drawing canvas ... corners" line inside a portal block).
    """
    out, cur = {}, None
    num = r'\(([\d.\-]+),([\d.\-]+)\)'
    for line in open(report, encoding='utf-8').read().splitlines():
        m = re.match(r'== portals at (\d+) (\d+)x(\d+)', line)
        if m:
            cur = (int(m.group(1)), int(m.group(2)), int(m.group(3)))
            continue
        m = re.match(r'\s+drawing canvas (\d+)x(\d+) corners ' + ' '.join([num] * 4), line)
        if m and cur:
            v = [float(g) for g in m.groups()]
            out[cur] = (v[0], v[1], [(v[2], v[3]), (v[4], v[5]), (v[6], v[7]), (v[8], v[9])])
        elif line.startswith('=='):
            cur = None
    return out


def homography(src, dst):
    """The 3x3 map taking each src point (x, y) to its dst point (four or more pairs; least squares beyond four)."""
    rows = []
    for (x, y), (u, v) in zip(src, dst):
        rows.append([x, y, 1, 0, 0, 0, -u * x, -u * y, -u])
        rows.append([0, 0, 0, x, y, 1, -v * x, -v * y, -v])
    m = np.linalg.svd(np.array(rows, float))[2][-1].reshape(3, 3)
    return m / m[2, 2]


def projection(drawing, H):
    """The canvas-to-screen map (screen rows top-down) of a drawing projection, and its inverse."""
    cw, ch, corners = drawing
    canvas = [(0.0, 0.0), (cw, 0.0), (0.0, ch), (cw, ch)]
    screen = [(x, H - y) for x, y in corners]
    m = homography(canvas, screen)
    return m, np.linalg.inv(m)


def warp(m, x, y):
    """Points (arrays x, y) through the 3x3 map m."""
    d = m[2, 0] * x + m[2, 1] * y + m[2, 2]
    return (m[0, 0] * x + m[0, 1] * y + m[0, 2]) / d, (m[1, 0] * x + m[1, 1] * y + m[1, 2]) / d


def painting_offset(paint, frame_luma, inv, box):
    """
    How far the painting really is from its projection over box (screen px x0, y0, x1, y1): TILE px tiles of the
    painting's luma warped by the projection, each matched to the frame by normalised cross-correlation within
    +-TILE_SEARCH px (sub-pixel by a parabola either side); the median (dx, dy) of the tiles that match at 0.9 or
    better, and their count. (0, 0, 0) when no tile matches.
    """
    H, W = frame_luma.shape
    x0, y0 = max(TILE_SEARCH, int(box[0])), max(TILE_SEARCH, int(box[1]))
    x1, y1 = min(W - TILE_SEARCH, int(box[2])), min(H - TILE_SEARCH, int(box[3]))
    if x1 - x0 < TILE or y1 - y0 < TILE:
        return 0.0, 0.0, 0
    py, px = np.mgrid[y0:y1, x0:x1]
    cx, cy = warp(inv, px + 0.5, py + 0.5)
    warped = nd.map_coordinates(paint, [cy - 0.5, cx - 0.5], order=1, mode='nearest')
    shifts = []
    for ty in range(0, y1 - y0 - TILE + 1, TILE // 2):
        for tx in range(0, x1 - x0 - TILE + 1, TILE // 2):
            t = warped[ty:ty + TILE, tx:tx + TILE]
            if t.std() < 8:
                continue

            def score(dx, dy):
                fy, fx = y0 + ty + dy, x0 + tx + dx
                return ncc(t, frame_luma[fy:fy + TILE, fx:fx + TILE])

            best = max((score(dx, dy), dx, dy) for dy in range(-TILE_SEARCH, TILE_SEARCH + 1) for dx in range(-TILE_SEARCH, TILE_SEARCH + 1))
            c, dx, dy = best
            if c < 0.9 or abs(dx) == TILE_SEARCH or abs(dy) == TILE_SEARCH:
                continue

            def vertex(cm, c0, cp):
                d = cm - 2 * c0 + cp
                return 0.0 if d >= 0 else max(-0.5, min(0.5, 0.5 * (cm - cp) / d))

            shifts.append((dx + vertex(score(dx - 1, dy), c, score(dx + 1, dy)), dy + vertex(score(dx, dy - 1), c, score(dx, dy + 1))))
    if not shifts:
        return 0.0, 0.0, 0
    s = np.array(shifts)
    return float(np.median(s[:, 0])), float(np.median(s[:, 1])), len(s)


def luma(a):
    return a[..., :3].astype(np.float32) @ np.array([0.299, 0.587, 0.114], np.float32)


def moments(mask):
    """Centroid and radii (2 standard deviations) of a boolean mask, in its own pixel coordinates."""
    yy, xx = np.nonzero(mask)
    if len(xx) < 8:
        return None
    x, y = xx + 0.5, yy + 0.5
    return x.mean(), y.mean(), 2.0 * x.std(), 2.0 * y.std()


def ncc(a, b):
    a = a - a.mean()
    b = b - b.mean()
    d = math.sqrt((a * a).sum() * (b * b).sum())
    return float((a * b).sum() / d) if d > 0 else 0.0


def register(paint_luma, frame_luma, x0, y0, sx, sy):
    """
    The painted crop's best place on the frame: its top-left (screen px, top-down) and scale, searched around
    (x0, y0) at (sx, sy) screen px per canvas px, over the part of it the frame shows; the offset refined to a
    tenth of a pixel by a parabola through the correlations either side. Returns (x, y, sx, sy, correlation).
    """
    H, W = frame_luma.shape
    img = Image.fromarray(paint_luma.astype(np.float32), 'F')

    def score(t, ix, iy):
        th, tw = t.shape
        a0, b0 = max(0, -ix), max(0, -iy)
        a1, b1 = min(tw, W - ix), min(th, H - iy)
        if a1 - a0 < tw * 0.6 or b1 - b0 < th * 0.6:
            return -1.0
        return ncc(t[b0:b1, a0:a1], frame_luma[iy + b0:iy + b1, ix + a0:ix + a1])

    best = (0, 0, 1.0, None, -1.0)
    for f in np.arange(1 - SEARCH_SCALE, 1 + SEARCH_SCALE + 1e-6, 0.01):
        tw, th = max(4, int(round(paint_luma.shape[1] * sx * f))), max(4, int(round(paint_luma.shape[0] * sy * f)))
        t = np.asarray(img.resize((tw, th), Image.BILINEAR), np.float32)
        for dy in range(-SEARCH_PX, SEARCH_PX + 1):
            for dx in range(-SEARCH_PX, SEARCH_PX + 1):
                ix, iy = int(round(x0)) + dx, int(round(y0)) + dy
                c = score(t, ix, iy)
                if c > best[4]:
                    best = (ix, iy, f, t, c)
    ix, iy, f, t, c = best
    if t is None:
        return x0, y0, sx, sy, -1.0

    def vertex(cm, c0, cp):
        d = cm - 2 * c0 + cp
        return 0.0 if d >= 0 else max(-0.5, min(0.5, 0.5 * (cm - cp) / d))

    fx = vertex(score(t, ix - 1, iy), c, score(t, ix + 1, iy))
    fy = vertex(score(t, ix, iy - 1), c, score(t, ix, iy + 1))
    return ix + fx, iy + fy, t.shape[1] / paint_luma.shape[1], t.shape[0] / paint_luma.shape[0], c

def ray_hole(L, cx, cy, rx, ry):
    """The audit's ray rule (portals.py) on the off frame's luma: the reference column, not the pass."""
    h, w = L.shape
    pts = []
    for k in range(144):
        a = 2 * math.pi * k / 144
        ca, sa = math.cos(a), math.sin(a)
        run, found, r = 0, None, 0.55
        while r < 1.6:
            xi, yi = int(round(cx + ca * r * rx)), int(round(cy - sa * r * ry))
            if not (0 <= xi < w and 0 <= yi < h):
                break
            if L[yi, xi] < 105:
                run += 1
                if run >= 3:
                    found = r - 2 * (1 / max(rx, ry))
                    break
            else:
                run = 0
            r += 1.0 / max(rx, ry)
        if found is not None:
            pts.append((a, found))
    if len(pts) < 20:
        return None
    med = np.median([p[1] for p in pts])
    keep = [(a, r) for a, r in pts if r > 0.85 * med]
    X = np.array([cx + math.cos(a) * r * rx for a, r in keep])
    Y = np.array([cy - math.sin(a) * r * ry for a, r in keep])
    A, C, D, E = np.linalg.lstsq(np.column_stack([X * X, Y * Y, X, Y]), np.ones(len(X)), rcond=None)[0]
    G = 1 + D * D / (4 * A) + E * E / (4 * C)
    return -D / (2 * A), -E / (2 * C), math.sqrt(abs(G / A)), math.sqrt(abs(G / C))


def allowed_off(specs):
    """--allow-off's RING@WxH values as {(ring, w, h)}."""
    out = set()
    for s in specs or []:
        m = re.fullmatch(r'(\d+)@(\d+)x(\d+)', s.strip())
        if not m:
            raise SystemExit(f'--allow-off {s!r}: expected RING@WxH, e.g. 5@1280x1024')
        out.add((int(m.group(1)), int(m.group(2)), int(m.group(3))))
    return out


def block_verdict(w, h, measured, off, allowed, expected=(), reported=()):
    """
    One frame's fail-closed rules. measured: [(ring, painting check passed)] for each ring measured (its own
    pass is judged where it is measured); off: [(ring, why)] for each ring wholly off the frame; allowed: {(ring, w,
    h)} from --allow-off; expected and reported: the asset's rings and the block's. A ring off the frame FAILS unless
    it is declared for this size and a measured ring of this frame passed its painting check (the projection that
    put it off screen is proven on this frame); no ring measured FAILS; a ring of the asset the block does not report
    FAILS. Returns (ok, lines).
    """
    ok, lines = True, []
    proof = [n for n, painted in measured if painted]
    for n, why in off:
        if (n, w, h) in allowed and proof:
            lines.append(f'   portal {n}: off screen at {w}x{h} ({why}), declared with --allow-off; the projection is checked on portal {proof[0]}')
        else:
            ok = False
            reason = 'and not declared with --allow-off' if (n, w, h) not in allowed else 'declared, but no ring of this frame passed its painting check'
            lines.append(f'   portal {n}: off screen at {w}x{h} ({why}) {reason}: FAIL')
    if not measured:
        ok = False
        lines.append(f'   no ring measured at {w}x{h}: FAIL')
    for n in sorted(set(expected) - set(reported)):
        ok = False
        lines.append(f'   portal {n}: not reported at {w}x{h} (HallPortals.asset has it): FAIL')
    return ok, lines


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument('run')
    ap.add_argument('--out', default=None)
    ap.add_argument('--allow-off', action='append', default=[], metavar='RING@WxH',
                    help='a ring expected wholly off the frame at that size (repeatable); still FAILS unless a ring of the same frame passed its painting check')
    args = ap.parse_args(argv)
    allowed = allowed_off(args.allow_off)
    out = args.out or os.path.join(args.run, 'portal_check')
    os.makedirs(out, exist_ok=True)

    rings = rings_of(ASSET)
    head = open(PAINTING, 'rb').read(8)
    if head[1:4] != b'PNG':
        raise SystemExit(f'{PAINTING} is not a PNG here (a Git LFS pointer?): git lfs pull fetches it')
    paint = luma(np.asarray(Image.open(PAINTING).convert('RGB')))
    atlas = np.asarray(Image.open(ATLAS).convert('RGBA'))
    AH, AW = atlas.shape[:2]
    lines, ok = [], True
    drawings = drawings_of(os.path.join(args.run, 'qa_report.txt'))
    blocks = blocks_of(os.path.join(args.run, 'qa_report.txt'))
    if not blocks:
        lines.append('no "== portals at" block in qa_report.txt: FAIL')
        ok = False
    for (hour, w, h), effects in sorted(blocks.items()):
        stem = os.path.join(args.run, 'shots', 'portals', f'h{hour:02d}_{w}x{h}')
        on = np.asarray(Image.open(stem + '_on.png').convert('RGB')).astype(np.int16)
        off = np.asarray(Image.open(stem + '_off.png').convert('RGB')).astype(np.int16)
        H, W = on.shape[:2]
        changed = np.abs(on - off).max(2) > DIFF
        proj = projection(drawings[(hour, w, h)], H) if (hour, w, h) in drawings else None
        L = luma(off)
        vis = Image.fromarray(on.astype(np.uint8))
        dr = ImageDraw.Draw(vis)
        lines.append(f'== {hour:02d}:00 {w}x{h}')
        measured, offs = [], []
        for e in effects:
            n = e['n']
            if not e['showing']:
                lines.append(f'   portal {n}: shut (no vortex)')
                continue
            cx, cy, rx, ry, kx, ky, kw, kh, u, v, uw, vh = rings[n]
            # The quad's screen box (top-down rows), and where it puts the ring's painted crop.
            qx0, qx1 = e['x0'], e['x1']
            qy0, qy1 = H - e['y1'], H - e['y0']
            qsx, qsy = (qx1 - qx0) / (2 * MARGIN * rx), (qy1 - qy0) / (2 * MARGIN * ry)
            px0 = qx0 + (kx - (cx - MARGIN * rx)) * qsx
            py0 = qy0 + (ky - (cy - MARGIN * ry)) * qsy
            ik, jk = int(kx), int(ky)
            if proj is not None:
                # The crop's corners by the painting's own projection: the box the ring is measured in.
                ex, ey = warp(proj[0], np.array([kx, kx + kw, kx, kx + kw]), np.array([ky, ky, ky + kh, ky + kh]))
                if ex.max() <= 0 or ey.max() <= 0 or ex.min() >= W or ey.min() >= H:
                    offs.append((n, f'its painted crop projects to x {ex.min():.0f}-{ex.max():.0f}, y {ey.min():.0f}-{ey.max():.0f}'))
                    continue
                bx0, by0 = max(0, int(math.floor(ex.min()))), max(0, int(math.floor(ey.min())))
                bx1, by1 = min(W, int(math.ceil(ex.max()))), min(H, int(math.ceil(ey.max())))
                py, px = np.mgrid[by0:by1, bx0:bx1]
                X, Y = warp(proj[1], px + 0.5, py + 0.5)
                # Where the painting really is around the ring: within a crop's size of it, on the off frame.
                pdx, pdy, tiles = painting_offset(paint, L, proj[1], (ex.min() - kw, ey.min() - kh, ex.max() + kw, ey.max() + kh))
                painted = tiles >= 8 and abs(pdx) <= PAINT_PX and abs(pdy) <= PAINT_PX
                cx0, cy0 = warp(proj[0], np.array([kx]), np.array([ky]))
                rx0, ry0, rsx, rsy, match = register(paint[jk:jk + int(kh), ik:ik + int(kw)], L, px0, py0, qsx, qsy)
                place = (f'painting {pdx:+.2f},{pdy:+.2f} px from its projection over {tiles} tiles (pass <= {PAINT_PX}) | crop ncc (reference) '
                         f'{rx0 - cx0[0]:+.1f},{ry0 - cy0[0]:+.1f} px, match {match:.2f}')
            else:
                rx0, ry0, rsx, rsy, match = register(paint[jk:jk + int(kh), ik:ik + int(kw)], L, px0, py0, qsx, qsy)
                painted = True
                place = f'match {match:.2f} (quad off by {rx0 - px0:+.1f},{ry0 - py0:+.1f} px, scale {rsx / qsx:.3f})'
                # The aperture where the painting is: per screen pixel centre, the canvas point and the atlas texel under it.
                bx0, by0 = max(0, int(math.floor(min(qx0, rx0)))), max(0, int(math.floor(min(qy0, ry0))))
                bx1 = min(W, int(math.ceil(max(qx1, rx0 + kw * rsx))))
                by1 = min(H, int(math.ceil(max(qy1, ry0 + kh * rsy))))
                if bx1 <= bx0 or by1 <= by0:
                    # The registration put the ring's crop outside the frame (a ring at the screen's edge): nothing to measure.
                    lines.append(f'   portal {n}: its painted crop registered outside the frame (match {match:.2f}, crop at {rx0:.1f},{ry0:.1f}): FAIL, look by eye')
                    ok = False
                    continue
                py, px = np.mgrid[by0:by1, bx0:bx1]
                X = kx + (px + 0.5 - rx0) / rsx
                Y = ky + (py + 0.5 - ry0) / rsy
            ax = np.floor(u * AW + (X - kx) / kw * uw * AW).astype(int)
            ay = np.floor(AH - (v + vh) * AH + (Y - ky) / kh * vh * AH).astype(int)
            inside = (X >= kx) & (X < kx + kw) & (Y >= ky) & (Y < ky + kh)
            ax, ay = np.clip(ax, 0, AW - 1), np.clip(ay, 0, AH - 1)
            aperture = inside & (atlas[ay, ax, 0] >= 128)
            front = inside & (atlas[ay, ax, 1] >= 128)
            hole = aperture & ~front
            if not hole.any():
                offs.append((n, 'no pixel of its hole is on the frame'))
                continue
            clipped = bx0 == 0 or by0 == 0 or bx1 == W or by1 == H
            sub = nd.binary_fill_holes(changed[by0:by1, bx0:bx1])
            vortex = sub & ~front
            halo = changed[by0:by1, bx0:bx1] & ~nd.binary_dilation(aperture, iterations=HALO_GROW)
            mh, mv = moments(hole), moments(vortex)
            if mh is None or mv is None:
                lines.append(f'   portal {n}: not measurable (hole {hole.sum()} px, vortex {vortex.sum()} px)')
                ok = False
                continue
            dx, dy = mv[0] - mh[0], mv[1] - mh[1]
            ratio_x, ratio_y = mv[2] / mh[2], mv[3] / mh[3]
            cover = (vortex & hole).sum() / max(1, hole.sum())
            passed = painted and math.hypot(dx, dy) <= CENTRE_PX and abs(ratio_x - 1) <= AXES and abs(ratio_y - 1) <= AXES and halo.sum() == 0
            ok &= passed
            measured.append((n, proj is not None and painted))
            ref = ray_hole(L, (qx0 + qx1) / 2, (qy0 + qy1) / 2, (qx1 - qx0) / 2 / MARGIN, (qy1 - qy0) / 2 / MARGIN)
            ref_text = (f'ray rule (reference) centre ({ref[0]:.1f},{ref[1]:.1f}) radii {ref[2]:.1f}x{ref[3]:.1f}, hole/ray {mh[2] / ref[2]:.2f} {mh[3] / ref[3]:.2f}'
                        if ref else 'ray rule (reference) not measurable')
            lines.append(f'   portal {n}: hole centre ({bx0 + mh[0]:.1f},{by0 + mh[1]:.1f}) radii {mh[2]:.1f}x{mh[3]:.1f} | vortex centre '
                         f'({bx0 + mv[0]:.1f},{by0 + mv[1]:.1f}) radii {mv[2]:.1f}x{mv[3]:.1f} | offset {dx:+.1f},{dy:+.1f} px | axes {ratio_x:.3f} {ratio_y:.3f} '
                         f'| halo {halo.sum()} px | cover {cover:.2f}{" | cut by the frame edge" if clipped else ""} | {place} '
                         f'| {"PASS" if passed else "FAIL"} | {ref_text}')
            for (yy, xx), colour in ((np.nonzero(hole & ~vortex), (0, 255, 0)), (np.nonzero(vortex & ~hole), (255, 0, 255)), (np.nonzero(halo), (255, 255, 0))):
                for y, x in zip(yy, xx):
                    dr.point((bx0 + x, by0 + y), fill=colour)
        block_ok, block_lines = block_verdict(w, h, measured, offs, allowed, rings.keys(), [e['n'] for e in effects])
        lines += block_lines
        ok &= block_ok
        vis.save(os.path.join(out, f'portal_check_h{hour:02d}_{w}x{h}.png'))
    lines.append('PASS' if ok else 'FAIL')
    open(os.path.join(out, 'portal_check.txt'), 'w', encoding='utf-8').write('\n'.join(lines) + '\n')
    print('\n'.join(lines))
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
