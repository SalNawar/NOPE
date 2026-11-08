"""The hall portals as gates: the frame-strip checks of tools/qa/scenarios/hall_portals_gate.json (the night of
2026-11-07, DECISIONS D6; PortalGate, PortalEffect, TimeDesk/PortalGlow).

    python tools/hall/gate_check.py <run folder> [--ring 1] [--closed 2 ...] [--out DIR]
    python tools/hall/gate_check.py <hall_fx_portals run folder> --fx

With --fx it reads a hall_fx_portals run instead (BUG-26a's portal strobe): seqlib.flicker inside every ring's quad
(qa_report.txt's "== portals at <hour> <w>x<h>" blocks) on each portals_* sequence; pass when no pixel there toggles
more than twice in 10 s (the city in the hall's windows is the known red qa.play.flicker's, outside the quads).

Reads what the scenario writes into a QA-RIG run folder: the sequences seq/gate_dial_h12 (the lock-in, the chevrons,
the open, the kawoosh), seq/gate_pool_h12 and seq/gate_pool_h22 (10 s of the open event horizon by day and night) and
seq/gate_close_h22 (the close), each frame's gate columns (HallGateRig's hallGateTrack: gate, lit, chev, pool, iris,
burst, env of the ring) and qa_report.txt's "== gate ring N at WxH" blocks (the ring's screen quad, screen px, y up;
the quad is PortalVortex.Margin, 1.5, opening radii each way). On the frames (capture px, origin top-left) the ring's
hole is the ellipse inside 0.95 opening radii, its chevrons' band the ring 1.05 to 1.5.

  steps     the ring's discrete changes (a chevron lit, the event horizon opening, a kawoosh starting) from the
            columns, with the frames where the chevrons' band changes on the pictures: pass when the chevrons step
            at the calibration's rate (20 / 7 s, +-0.1 s), each change in one frame, and no two changes closer than
            2 s. Each change is timed on the clock that drives it: the board calibrates on unscaled time
            (SwitchboardView: a hit-stop never stops a calibration), so a chevron and the open are timed on the
            frames' unscaled column; the ring's own motion (the dwell before a kawoosh, the kawoosh, the close) runs on
            game time, one frame a 30th of a second in a fixed-step capture. In play both clocks run together; in
            a capture the unscaled clock runs at the speed of the real frames, so the chevrons step in fewer frames.
  pool      the share of the hole's pixels whose luma moves more than 8 from one frame to the next, over each pool
            sequence: pass when it stays under 8 %.
  kawoosh   the 36 frames from the kawoosh's first: the envelope column and the hole's mean luma over the frame
            before it, each normalised by its peak: pass when both peak at frame 10 to 14 and no frame steps more
            than 0.15.
  close     the close's horizon and chevron columns, and the hole's and the band's mean luma from open to dark,
            normalised: pass when no frame steps more than 0.1.
  flicker   tools/qa/seqlib.flicker (a pixel toggling more than twice in 10 s) inside the ring's quad only, on every
            sequence: pass when no pixel there toggles more than twice.
  closed    each --closed ring (default 2: under maintenance on day 1, while the scenario dials its module) on the
            1920x1080 stills: the mean G-R of its hole on gate_lockedin_h12, gate_open_h12 and gate_kawoosh_h12 against
            gate_offline_h12 (within 3), and on gate_open_h22 and gate_closed_h22 against it (within 8: the night's
            light shifts a dark hole by 2.4, an open one by 41): pass when each holds (a closed ring stays dark
            whatever its module does; Saleh 2026-09-29: "hide a closed portal's halo").

Luma is Rec. 601 on the frames as captured (as seqtools reads them). Writes <out>/gate_check.txt (out: the run
folder) and exits 0 when every check passes.
"""
import argparse
import os
import re
import sys

import numpy as np

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'qa'))
import seqlib  # noqa: E402

FPS = seqlib.FPS
STEP_SECONDS = 20.0 / 7.0          # one chevron per calibration / 7 (unscaled seconds)
DWELL_SECONDS = 2.0                 # no two changes closer than 2 s
POOL_SHARE = 0.08
KAWOOSH_FRAMES = 36
PEAK = (10, 14)
KAWOOSH_STEP = 0.15
CLOSE_STEP = 0.1
CHANGE = 8.0
CLOSED_GR = 3.0                     # a closed ring's hole: mean G-R within 3 of its dark look by day
CLOSED_NIGHT_GR = 8.0               # and within 8 of it at night (the night's light shifts a dark hole by 2.4)


def columns(row):
    """frames.tsv's extra column ("name=value;...") as a dict."""
    out = {}
    for part in (row.get('extra') or '').split(';'):
        if '=' in part:
            k, v = part.split('=', 1)
            out[k] = v
    return out


def quads(run, ring):
    """{(w, h): (cx, cy, x0, y0, x1, y1)} of the ring from qa_report.txt (the last block per size)."""
    path = os.path.join(run, 'qa_report.txt')
    text = open(path, encoding='utf-8').read() if os.path.exists(path) else ''
    found = {}
    pat = r'== gate ring %d at (\d+)x(\d+)\n\s+centre \(([-\d.]+),([-\d.]+)\) quad \(([-\d.]+),([-\d.]+)\)-\(([-\d.]+),([-\d.]+)\)' % ring
    for m in re.finditer(pat, text):
        w, h = int(m.group(1)), int(m.group(2))
        found[(w, h)] = tuple(float(m.group(i)) for i in range(3, 9))
    return found


def regions(seq, rows, quad):
    """The ring's hole, its chevrons' band and its quad on the frames (bool masks)."""
    first = seqlib.load_luma(seqlib.frame_files(seq)[0])
    h, w = first.shape
    sw, sh = float(rows[0]['screenW']), float(rows[0]['screenH'])
    k = w / sw
    cx, cy, x0, y0, x1, y1 = quad
    rx, ry = (x1 - x0) / 2 / 1.5 * k, (y1 - y0) / 2 / 1.5 * k
    ccx, ccy = cx * k, (sh - cy) * k
    yy, xx = np.mgrid[0:h, 0:w]
    r = np.hypot((xx + 0.5 - ccx) / max(rx, 1e-3), (yy + 0.5 - ccy) / max(ry, 1e-3))
    hole = r <= 0.95
    band = (r >= 1.05) & (r <= 1.5)
    box = (np.abs(xx + 0.5 - ccx) <= 1.5 * rx + 2) & (np.abs(yy + 0.5 - ccy) <= 1.5 * ry + 2)
    return hole, band, box


def load(seq):
    files = seqlib.frame_files(seq)
    return [seqlib.load_luma(f) for f in files]


def seq_quad(run, rows, ring):
    q = quads(run, ring)
    key = (int(float(rows[0]['screenW'])), int(float(rows[0]['screenH'])))
    return q.get(key)


def check_steps(seq, rows, lumas, band, ring, lines):
    cols = [columns(r) for r in rows]
    lit = [int(c.get('lit%d' % ring, '0') or 0) for c in cols]
    gate = [c.get('gate%d' % ring, '') for c in cols]
    events = []
    for i in range(1, len(rows)):
        if lit[i] != lit[i - 1]:
            events.append((i, 'chevron %d' % lit[i]))
        if gate[i] in ('Open', 'Sending') and gate[i - 1] == 'LockedIn':
            events.append((i, 'open'))
        if gate[i] == 'Sending' and gate[i - 1] != 'Sending':
            events.append((i, 'kawoosh'))
    events.sort()
    unscaled = [float(r['unscaled']) for r in rows]
    chev = [i for i, e in events if e.startswith('chevron')]
    spacing = [round(unscaled[b] - unscaled[a], 3) for a, b in zip(chev, chev[1:])]
    # a board-driven change (a chevron, the open) on the unscaled clock; the kawoosh on game time (frames / 30)
    gaps = [round((b[0] - a[0]) / FPS if b[1] == 'kawoosh' else unscaled[b[0]] - unscaled[a[0]], 3) for a, b in zip(events, events[1:])]
    # the band's changed pixels per frame: a chevron lights in one frame, then holds
    moved = [int(((np.abs(lumas[i] - lumas[i - 1]) > 24) & band).sum()) for i in range(1, len(lumas))]
    jumps = [i + 1 for i, n in enumerate(moved) if n >= 3]
    lines.append('steps: events ' + ', '.join('%s@%d' % (e, i) for i, e in events))
    lines.append('steps: chevron spacing (unscaled s) %s, expected %.3f; in frames %s' % (spacing, STEP_SECONDS, [b - a for a, b in zip(chev, chev[1:])]))
    lines.append('steps: gaps between changes (s, each on its own clock) %s; smallest %s (least %.1f)' % (gaps, min(gaps) if gaps else '-', DWELL_SECONDS))
    lines.append('steps: frames where the chevrons\' band jumped (>= 3 px over 24 luma): %s' % jumps)
    # the unscaled column steps a real frame at a time (about 0.07 s in a capture), so a step lands within one frame of its time
    frame_real = max(unscaled[i] - unscaled[i - 1] for i in range(1, len(unscaled))) if len(unscaled) > 1 else 0.0
    ok_rate = len(chev) >= 6 and all(abs(x - STEP_SECONDS) <= frame_real + 0.01 for x in spacing)
    ok_gap = bool(gaps) and min(gaps) >= DWELL_SECONDS - frame_real
    # each chevron in one frame: before the open (whose light and bloom also reach the band), every frame the band
    # jumps is a chevron's step frame, and every step jumps in its own frame
    opened = next((i for i, e in events if e == 'open'), len(rows))
    early = [j for j in jumps if j < opened]
    ok_one = set(early) == set(chev)
    lines.append('steps: the band jumped before the open at frames %s; the chevrons stepped at %s' % (early, chev))
    return ok_rate and ok_gap and ok_one, events


def check_pool(seq, lumas, hole, lines, name):
    shares = [float(((np.abs(lumas[i] - lumas[i - 1]) > CHANGE) & hole).sum()) / max(1, hole.sum()) for i in range(1, len(lumas))]
    worst = max(shares) if shares else 0.0
    lines.append('pool %s: changed share per frame max %.4f mean %.4f over %d hole px (pass under %.2f)' % (
        name, worst, float(np.mean(shares)) if shares else 0.0, int(hole.sum()), POOL_SHARE))
    return worst < POOL_SHARE


def peak_and_step(series):
    s = np.asarray(series, dtype=np.float64)
    top = s.max() if s.size else 0.0
    if top <= 1e-6:
        return -1, 1.0, s
    n = s / top
    return int(np.argmax(n)), float(np.abs(np.diff(n)).max()), n


def check_kawoosh(rows, lumas, hole, events, ring, lines):
    starts = [i for i, e in events if e == 'kawoosh']
    if not starts:
        lines.append('kawoosh: none in the sequence')
        return False
    s0 = starts[0]
    cols = [columns(r) for r in rows]
    env = [float(cols[i].get('env%d' % ring, '0') or 0) for i in range(s0, min(len(rows), s0 + KAWOOSH_FRAMES + 1))]
    base = float(lumas[s0 - 1][hole].mean())
    lift = [float(lumas[i][hole].mean()) - base for i in range(s0, min(len(lumas), s0 + KAWOOSH_FRAMES + 1))]
    pe, se, _ = peak_and_step(env)
    pp, sp, n = peak_and_step(lift)
    sending = sum(1 for c in cols if c.get('gate%d' % ring) == 'Sending')
    lines.append('kawoosh: from frame %d, %d frames Sending; envelope peak at frame %d, steepest step %.3f' % (s0, sending, pe, se))
    lines.append('kawoosh: the hole\'s mean luma lift peak at frame %d (+%.1f luma), steepest step %.3f; per frame %s' % (
        pp, max(lift), sp, ' '.join('%.2f' % v for v in n)))
    ok = PEAK[0] <= pe <= PEAK[1] and PEAK[0] <= pp <= PEAK[1] and se <= KAWOOSH_STEP and sp <= KAWOOSH_STEP
    ok = ok and abs(sending - KAWOOSH_FRAMES) <= 1
    return ok


def check_close(rows, lumas, hole, band, ring, lines):
    cols = [columns(r) for r in rows]
    pool = [float(c.get('pool%d' % ring, '0') or 0) for c in cols]
    chev = [float(c.get('chev%d' % ring, '0') or 0) for c in cols]
    drop = max([max(pool[i - 1] - pool[i], chev[i - 1] - chev[i]) for i in range(1, len(cols))] or [0.0])
    fading = [i for i in range(1, len(cols)) if pool[i] < pool[i - 1] or chev[i] < chev[i - 1]]
    lines.append('close: %d frames fading (frames %s to %s), steepest column step %.3f' % (
        len(fading), fading[0] if fading else '-', fading[-1] if fading else '-', drop))
    ok = drop <= CLOSE_STEP and 20 <= len(fading) <= 28
    for name, region in (('hole', hole), ('band', band)):
        series = [float(l[region].mean()) for l in lumas]
        a, b = series[0], series[-1]
        if abs(a - b) < 1.0:
            lines.append('close: the %s\'s mean luma barely moves (%.1f to %.1f)' % (name, a, b))
            continue
        n = [(v - b) / (a - b) for v in series]
        step = max(abs(n[i] - n[i - 1]) for i in range(1, len(n)))
        lines.append('close: the %s\'s mean luma %.1f to %.1f, steepest normalised step %.3f' % (name, a, b, step))
        ok = ok and step <= CLOSE_STEP
    return ok


def hole_gr(run, still, quad):
    """The mean G-R of the ring's hole (0.95 opening radii) on stills/<still>_1920x1080.png, or None when it is missing."""
    from PIL import Image
    path = os.path.join(run, 'stills', still + '_1920x1080.png')
    if not os.path.exists(path):
        return None
    a = np.asarray(Image.open(path).convert('RGB')).astype(float)
    h, w, _ = a.shape
    cx, cy, x0, y0, x1, y1 = quad
    rx, ry = (x1 - x0) / 2 / 1.5 * 0.95, (y1 - y0) / 2 / 1.5 * 0.95
    yy, xx = np.mgrid[0:h, 0:w]
    mask = ((xx + 0.5 - cx) / max(rx, 1e-3)) ** 2 + ((yy + 0.5 - (h - cy)) / max(ry, 1e-3)) ** 2 <= 1
    return float((a[..., 1] - a[..., 0])[mask].mean()) if mask.any() else None


def check_closed(run, ring, lines):
    """A closed ring stays dark while the scenario dials its module: its hole's mean G-R holds on the stills."""
    quad = quads(run, ring).get((1920, 1080))
    if quad is None:
        lines.append('closed ring %d: no "gate ring %d" quad for 1920x1080 in qa_report.txt' % (ring, ring))
        return False
    ok = True
    for base, others, tol in (('gate_offline_h12', ('gate_lockedin_h12', 'gate_open_h12', 'gate_kawoosh_h12'), CLOSED_GR),
                              ('gate_offline_h12', ('gate_open_h22', 'gate_closed_h22'), CLOSED_NIGHT_GR)):
        ref = hole_gr(run, base, quad)
        if ref is None:
            lines.append('closed ring %d: %s missing' % (ring, base))
            ok = False
            continue
        for still in others:
            v = hole_gr(run, still, quad)
            good = v is not None and abs(v - ref) <= tol
            ok &= good
            lines.append('closed ring %d: %s mean G-R %s against %s %.1f (within %.0f: %s)'
                         % (ring, still, 'missing' if v is None else '%.1f' % v, base, ref, tol, 'yes' if good else 'NO'))
    return ok


def check_flicker(lumas, rows, box, lines, name):
    states = [seqlib.frame_state(r) for r in rows] if rows else None
    r = seqlib.flicker(lumas, states, static_mask=~box)
    lines.append('flicker %s (inside the ring\'s quad): %d px toggling > 2x in 10 s, worst %d' % (name, r['bad_px'], r['worst']))
    return r['pass']


def fx_quads(run):
    """{(w, h, hour): [(ring, cx, cy, x0, y0, x1, y1)]} from a hall_fx_portals run's "== portals at" blocks."""
    text = open(os.path.join(run, 'qa_report.txt'), encoding='utf-8').read()
    out = {}
    for m in re.finditer(r'== portals at (\d+) (\d+)x(\d+)\n((?:   PortalEffect.*\n)+)', text):
        key = (int(m.group(2)), int(m.group(3)), int(m.group(1)))
        out[key] = [(int(q.group(1)),) + tuple(float(v) for v in q.groups()[1:]) for q in re.finditer(
            r'PortalEffect_(\d+) showing \w+ night [\d.]+ centre \(([-\d.]+),([-\d.]+)\) quad \(([-\d.]+),([-\d.]+)\)-\(([-\d.]+),([-\d.]+)\)', m.group(4))]
    return out


def check_fx(run, lines):
    """Flicker inside every ring's quad on each portals_* sequence of a hall_fx_portals run."""
    quads, results = fx_quads(run), {}
    for name in sorted(os.listdir(os.path.join(run, 'seq'))):
        path = os.path.join(run, 'seq', name)
        rows = seqlib.read_tsv(os.path.join(path, 'frames.tsv'))
        files = seqlib.frame_files(path)
        n = min(len(files), len(rows))
        if not name.startswith('portals_') or n < 3:
            continue
        lumas = [seqlib.load_luma(f) for f in files[:n]]
        sw, sh = int(float(rows[0]['screenW'])), int(float(rows[0]['screenH']))
        hour = int(name.rsplit('_h', 1)[1]) if '_h' in name else 12
        h, w = lumas[0].shape
        k = w / sw
        box = np.zeros((h, w), dtype=bool)
        for ring, cx, cy, x0, y0, x1, y1 in quads.get((sw, sh, hour), []):
            a0, a1 = int(max(0, x0 * k)), int(min(w, x1 * k + 1))
            b0, b1 = int(max(0, (sh - y1) * k)), int(min(h, (sh - y0) * k + 1))
            if a0 < a1 and b0 < b1:
                box[b0:b1, a0:a1] = True
        r = seqlib.flicker(lumas, [seqlib.frame_state(x) for x in rows[:n]], static_mask=~box)
        lines.append('flicker %s (inside the rings\' quads, %d px): %d px toggling > 2x in 10 s, worst %d' % (name, int(box.sum()), r['bad_px'], r['worst']))
        results['flicker ' + name] = r['pass'] and box.any()
    return results


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('run')
    ap.add_argument('--ring', type=int, default=1)
    ap.add_argument('--fx', action='store_true')
    ap.add_argument('--closed', type=int, nargs='*', default=[2])
    ap.add_argument('--out')
    a = ap.parse_args()
    run, ring = a.run, a.ring
    lines, results = [], {}
    if a.fx:
        results = check_fx(run, lines)
        lines.append('')
        for k, v in results.items():
            lines.append('%s %s' % ('PASS' if v else 'FAIL', k))
        text = '\n'.join(lines) + '\n'
        out = a.out or run
        os.makedirs(out, exist_ok=True)
        with open(os.path.join(out, 'gate_check_fx.txt'), 'w', encoding='utf-8') as f:
            f.write(text)
        print(text)
        sys.exit(0 if results and all(results.values()) else 1)

    def seq(name):
        path = os.path.join(run, 'seq', name)
        rows = seqlib.read_tsv(os.path.join(path, 'frames.tsv'))
        if not rows or not seqlib.frame_files(path):
            lines.append('%s: missing' % name)
            return None
        quad = seq_quad(run, rows, ring)
        if quad is None:
            lines.append('%s: no "gate ring %d" quad for %sx%s in qa_report.txt' % (name, ring, rows[0]['screenW'], rows[0]['screenH']))
            return None
        lumas = load(path)
        n = min(len(lumas), len(rows))
        return path, rows[:n], lumas[:n], regions(path, rows, quad)

    dial = seq('gate_dial_h12')
    events = []
    if dial:
        path, rows, lumas, (hole, band, box) = dial
        results['steps'], events = check_steps(path, rows, lumas, band, ring, lines)
        results['kawoosh'] = check_kawoosh(rows, lumas, hole, events, ring, lines)
        results['flicker dial'] = check_flicker(lumas, rows, box, lines, 'gate_dial_h12')
    else:
        results['steps'] = results['kawoosh'] = False
    for name in ('gate_pool_h12', 'gate_pool_h22'):
        got = seq(name)
        if got:
            path, rows, lumas, (hole, band, box) = got
            results['pool ' + name] = check_pool(path, lumas, hole, lines, name)
            results['flicker ' + name] = check_flicker(lumas, rows, box, lines, name)
        else:
            results['pool ' + name] = False
    close = seq('gate_close_h22')
    if close:
        path, rows, lumas, (hole, band, box) = close
        results['close'] = check_close(rows, lumas, hole, band, ring, lines)
        results['flicker close'] = check_flicker(lumas, rows, box, lines, 'gate_close_h22')
    else:
        results['close'] = False
    for c in a.closed:
        results['closed ring %d' % c] = check_closed(run, c, lines)

    lines.append('')
    for k, v in results.items():
        lines.append('%s %s' % ('PASS' if v else 'FAIL', k))
    text = '\n'.join(lines) + '\n'
    out = a.out or run
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, 'gate_check.txt'), 'w', encoding='utf-8') as f:
        f.write(text)
    print(text)
    sys.exit(0 if results and all(results.values()) else 1)


if __name__ == '__main__':
    main()
