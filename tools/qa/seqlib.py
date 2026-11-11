"""QA-RIG frame analysis (P0-B): the pure functions behind tools/qa/seqtools.py.

Everything works on numpy arrays, so it is unit-tested on synthetic frames (test_seqlib.py) without Unity:
- flicker: FIX_PLAN's A-B-A toggle (luma jumps > 24/255 and returns within 3 frames), outside the expected-motion
  mask (tracked objects that moved, the pointer), with the pass rule "zero pixels toggling more than twice in 10 s";
- flash: GAME_FEEL_BIBLE G3, flashes per second per screen cell (a flash = a pair of opposing luma changes), the
  full-field flashes and the saturated-red ones;
- latency: GAME_FEEL_BIBLE check 2, frames from a press (events.tsv) to the first visible change, and to its cue;
- sound: GAME_FEEL_BIBLE check 2's other half, frames from a press to the sound cue that answers it (events.tsv's cue rows);
- frozen and cuts: BUG-31's "no frame repeated more than 0.25 s" and BUG-32's single-frame jumps (a frame whose mean
  luma jumps against its steady neighbours, or a one-frame pop);
- blockiness: the 8x8 block-artefact ratio (VIS audit).
A sequence folder is what SeqRecorder writes: f0000.png ..., frames.tsv, events.tsv, meta.json (a timing sequence,
recorded after the op realtime n=1, has no frames: only frames.tsv's real frame times, for the hitch analysis). The
analyses write their own pictures into the folder (flicker_heat.png ...): only names like f0000.png are frames.
"""
import csv
import json
import os
import re
import datetime

import numpy as np
from PIL import Image

FPS = 30

# A frame's file name (SeqRecorder: f0000.png, f0001.png ...): 'f', four or more digits, '.png'. Nothing else in the folder
# is a frame (the analyses' own pictures, flicker_heat.png, sit beside them; GATE-15 of 2026-10-28).
FRAME_NAME = re.compile(r'^f\d{4,}\.png$')

# The events.tsv channels that mark a sound played: 'cue' (SeqRecorder: every cue the sound bank was asked for, and every
# sound the game plays outside it, QaSignals.Event("cue", ...)) and 'audio' (the older name).
CUE_CHANNELS = ('cue', 'audio')


# ----------------------------------------------------------------------------------------------------------------------
# Reading a sequence

def frame_files(seq):
    """The sequence's frames in order: the files named like f0000.png (FRAME_NAME), never an analysis's picture."""
    if not os.path.isdir(seq):
        return []
    return sorted(os.path.join(seq, n) for n in os.listdir(seq) if FRAME_NAME.match(n))


def read_meta(seq):
    """The sequence's meta.json as a dict (empty when the file is missing)."""
    path = os.path.join(seq, 'meta.json')
    if not os.path.exists(path):
        return {}
    with open(path, encoding='utf-8-sig') as f:
        return json.load(f)


def is_timing(seq):
    """True for a timing sequence (meta.json frames false, or fixedStep false from before the frames key): real frame
    times only, no captured frames."""
    meta = read_meta(seq)
    if not meta:
        return False
    return not meta.get('frames', meta.get('fixedStep', True))


def motion_still(seq):
    """True when the sequence was recorded with motion off (meta.json motionStill: Reduced Motion or a Motion intensity
    of 0, when the game's motions cut by design)."""
    return bool(read_meta(seq).get('motionStill', False))


def read_tsv(path):
    """A TSV with a header as a list of dicts (empty when the file is missing)."""
    if not os.path.exists(path):
        return []
    with open(path, encoding='utf-8', newline='') as f:
        return list(csv.DictReader(f, delimiter='\t'))


def luma(rgb):
    """Rec. 601 luma (0..255, float32) of an HxWx3 (or HxWx4) uint8 array."""
    a = np.asarray(rgb, dtype=np.float32)
    if a.ndim == 2:
        return a
    return 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]


def load_luma(path):
    """One frame's luma."""
    return luma(np.asarray(Image.open(path).convert('RGB')))


def parse_rects(cell):
    """frames.tsv's tracked column ("name:x0,y0,x1,y1;...") as {name: (x0, y0, x1, y1)} in capture pixels."""
    rects = {}
    for part in (cell or '').split(';'):
        if ':' not in part:
            continue
        name, coords = part.split(':', 1)
        try:
            x0, y0, x1, y1 = (float(v) for v in coords.split(','))
        except ValueError:
            continue
        rects[name] = (x0, y0, x1, y1)
    return rects


def frame_state(row):
    """What the expected-motion mask needs from one frames.tsv row: the tracked rects and the pointer."""
    if row is None:
        return {'rects': {}, 'mouse': None}
    try:
        mouse = (float(row.get('mx', '')), float(row.get('my', '')))
    except ValueError:
        mouse = None
    return {'rects': parse_rects(row.get('tracked', '')), 'mouse': mouse}


# ----------------------------------------------------------------------------------------------------------------------
# Expected-motion mask

def motion_mask(shape, states, i, within=3, pad=4, cursor=24):
    """True where motion is expected around frame i: every tracked rect that moved between frames i-1 and i+within
    (both positions, padded) and the pointer's neighbourhood while it moved. states: per-frame frame_state dicts."""
    h, w = shape
    mask = np.zeros((h, w), dtype=bool)
    lo, hi = max(0, i - 1), min(len(states) - 1, i + within)
    window = states[lo:hi + 1]
    names = set()
    for s in window:
        names.update(s['rects'].keys())
    for name in names:
        seen = [s['rects'].get(name) for s in window]
        if len(set(seen)) <= 1:
            continue
        for r in seen:
            if r is not None:
                _fill(mask, r, pad)
    for p in set(s['mouse'] for s in window if s['mouse'] is not None):
        _fill(mask, (p[0] - cursor, p[1] - cursor, p[0] + cursor, p[1] + cursor), 0)
    return mask


def _fill(mask, rect, pad):
    h, w = mask.shape
    ax, ay, bx, by = rect
    x0 = int(max(0, np.floor(min(ax, bx) - pad)))
    y0 = int(max(0, np.floor(min(ay, by) - pad)))
    x1 = int(min(w, np.ceil(max(ax, bx) + pad)))
    y1 = int(min(h, np.ceil(max(ay, by) + pad)))
    if x1 > x0 and y1 > y0:
        mask[y0:y1, x0:x1] = True


# ----------------------------------------------------------------------------------------------------------------------
# Flicker (FIX_PLAN: A-B-A toggles; pass = no pixel toggling more than twice in 10 s outside the mask)

def toggles(lumas, i, th=24.0, back=8.0, within=3):
    """The pixels that toggle at frame i: luma moves more than th from frame i-1 and comes back within `back` of it
    in one of the next `within` frames (i+0 .. i+within-1 count as the excursion; i+1 .. i+within as the return)."""
    base = lumas[i - 1]
    out = np.abs(lumas[i] - base) > th
    returned = np.zeros_like(out)
    for k in range(1, within + 1):
        if i + k < len(lumas):
            returned |= np.abs(lumas[i + k] - base) < back
    return out & returned


def flicker(lumas, states=None, static_mask=None, th=24.0, back=8.0, within=3, window=10 * FPS, max_toggles=2,
            static_only=False):
    """Runs the detector over a sequence. lumas: list of HxW float arrays. states: per-frame frame_state dicts (the
    expected-motion mask; None: no mask). static_mask: HxW bool, True = ignored. static_only: count only frames where
    nothing tracked moved (the DOC audit's static_flicker). Returns a dict: per-pixel toggle counts ('heat'), the worst
    count of any pixel in any `window` frames ('worst'), the offending pixels ('bad_px'), frames with toggles
    ('frames': [(i, px, bbox)]), and 'pass' (no pixel over max_toggles in any window)."""
    n = len(lumas)
    if n < 3:
        return {'pass': True, 'bad_px': 0, 'worst': 0, 'frames': [], 'heat': None, 'n': n, 'toggles': 0}
    shape = lumas[0].shape
    heat = np.zeros(shape, dtype=np.int32)
    running = np.zeros(shape, dtype=np.int32)
    worst = np.zeros(shape, dtype=np.int32)
    history = []
    frames = []
    for i in range(1, n):
        if static_only and states is not None and _moved(states, i, within):
            tog = np.zeros(shape, dtype=bool)
        else:
            tog = toggles(lumas, i, th, back, within)
            if states is not None:
                tog &= ~motion_mask(shape, states, i, within)
            if static_mask is not None:
                tog &= ~static_mask
        idx = np.flatnonzero(tog)
        history.append(idx)
        running.flat[idx] += 1
        if len(history) > window:
            old = history[len(history) - window - 1]
            running.flat[old] -= 1
        np.maximum(worst, running, out=worst)
        heat.flat[idx] += 1
        if idx.size:
            ys, xs = np.unravel_index(idx, shape)
            frames.append((i, int(idx.size), (int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max()))))
    bad = int((worst > max_toggles).sum())
    return {'pass': bad == 0, 'bad_px': bad, 'worst': int(worst.max()), 'frames': frames, 'heat': heat, 'n': n,
            'toggles': int(heat.sum())}


def _moved(states, i, within):
    lo, hi = max(0, i - 1), min(len(states) - 1, i + within)
    window = states[lo:hi + 1]
    return any(s != window[0] for s in window[1:])


def hot_blocks(heat, block=16, top=25):
    """The heat map's busiest blocks: [(toggles, x, y)] heaviest first."""
    h, w = heat.shape
    out = []
    for y in range(0, h, block):
        for x in range(0, w, block):
            s = int(heat[y:y + block, x:x + block].sum())
            if s > 0:
                out.append((s, x, y))
    out.sort(reverse=True)
    return out[:top]


# ----------------------------------------------------------------------------------------------------------------------
# Flash (GAME_FEEL_BIBLE G3: at most 3 flashes per second anywhere; no saturated red full-field flash)

def cell_means(lum, cols=8, rows=6):
    """The mean luma of each cell of a cols x rows grid (rows x cols array)."""
    h, w = lum.shape
    ys = np.linspace(0, h, rows + 1).astype(int)
    xs = np.linspace(0, w, cols + 1).astype(int)
    return np.array([[lum[ys[r]:ys[r + 1], xs[c]:xs[c + 1]].mean() for c in range(cols)] for r in range(rows)],
                    dtype=np.float32)


def pivots(series, th=20.0):
    """The zigzag turning points of a series: indices where it reverses after moving at least th (hysteresis), with
    the first and last extremes included."""
    s = [float(v) for v in series]
    n = len(s)
    if n == 0:
        return []
    piv = [0]
    trend = 0
    lo = hi = ext = 0
    for i in range(1, n):
        if trend == 0:
            if s[i] < s[lo]:
                lo = i
            if s[i] > s[hi]:
                hi = i
            if s[i] - s[lo] >= th:
                piv[0], trend, ext = lo, 1, i
            elif s[hi] - s[i] >= th:
                piv[0], trend, ext = hi, -1, i
        elif trend == 1:
            if s[i] >= s[ext]:
                ext = i
            elif s[ext] - s[i] >= th:
                piv.append(ext)
                trend, ext = -1, i
        else:
            if s[i] <= s[ext]:
                ext = i
            elif s[i] - s[ext] >= th:
                piv.append(ext)
                trend, ext = 1, i
    if trend != 0:
        piv.append(ext)
    return piv if len(piv) > 1 else []


def transitions(series, th=20.0, max_frames=6):
    """The fast luma changes of a series: each swing of at least th between turning points whose steep part (from
    leaving the old level to reaching the new one, within th/4) takes at most max_frames frames, as
    [(start_frame, end_frame, sign)]. A slow fade is not a transition."""
    s = [float(v) for v in series]
    piv = pivots(s, th)
    out = []
    for a, b in zip(piv, piv[1:]):
        if abs(s[b] - s[a]) < th:
            continue
        t0 = a
        for t in range(a, b + 1):
            if abs(s[t] - s[a]) <= th / 4:
                t0 = t
        t1 = b
        for t in range(t0, b + 1):
            if abs(s[t] - s[b]) <= th / 4:
                t1 = t
                break
        if t1 - t0 <= max_frames:
            out.append((t0, t1, 1 if s[b] > s[a] else -1))
    return out


def max_flashes_per_second(trans, fps=FPS):
    """The most flashes in any 1 s window: a flash is a pair of opposing changes, so half the changes in the window."""
    starts = sorted(t[0] for t in trans)
    best = 0
    lo = 0
    for hi in range(len(starts)):
        while starts[hi] - starts[lo] >= fps:
            lo += 1
        best = max(best, (hi - lo + 1) // 2)
    return best


def flash(rgb_frames, cols=8, rows=6, th=20.0, max_per_s=3, full_field=0.5, red_area=0.25):
    """Flash meter over a sequence of HxWx3 uint8 frames (a grid of cols x rows cells, each cell's mean luma).
    Returns the worst flashes per second of any cell ('worst_per_s'), the full-field changes (frames where at least
    `full_field` of the cells change together), the red ones among them (at least `red_area` of the screen saturated
    red during the change) and 'pass' (worst <= max_per_s and no red full-field flash)."""
    if len(rgb_frames) < 3:
        return {'pass': True, 'worst_per_s': 0, 'full_field': [], 'red': [], 'cells': {}}
    grid = np.stack([cell_means(luma(f), cols, rows) for f in rgb_frames])  # n, rows, cols
    per_cell = {}
    at = {}
    for r in range(rows):
        for c in range(cols):
            tr = transitions(grid[:, r, c], th)
            if tr:
                per_cell[(c, r)] = max_flashes_per_second(tr)
                for t0, t1, _ in tr:
                    at.setdefault(t0, set()).add((c, r))
    worst = max(per_cell.values()) if per_cell else 0
    full = []
    for f in sorted(at):
        cells = set()
        for g in range(f - 1, f + 2):
            cells |= at.get(g, set())
        if len(cells) >= full_field * cols * rows and (not full or f - full[-1] > 2):
            full.append(f)
    red = []
    for f in full:
        for k in range(max(0, f - 1), min(len(rgb_frames), f + 8)):
            a = np.asarray(rgb_frames[k]).astype(np.int16)
            if ((a[..., 0] > 200) & (a[..., 1] < 70) & (a[..., 2] < 70)).mean() >= red_area:
                red.append(f)
                break
    ok = worst <= max_per_s and not red
    return {'pass': ok, 'worst_per_s': worst, 'full_field': full, 'red': red,
            'cells': {f'{c},{r}': v for (c, r), v in per_cell.items() if v > 0}}


# ----------------------------------------------------------------------------------------------------------------------
# Input latency (GAME_FEEL_BIBLE check 2: the first visible change on the press frame, <= 1 frame at 30 fps)

def first_change(lumas, i, roi=None, exclude=None, th=12.0, min_px=12):
    """The first frame j >= i whose luma differs from frame i-1 by more than th on at least min_px pixels inside roi
    (x0, y0, x1, y1; None: the whole frame), outside `exclude` (a bool mask), or None."""
    base = lumas[i - 1]
    h, w = base.shape
    sel = np.zeros((h, w), dtype=bool)
    if roi is None:
        sel[:] = True
    else:
        _fill(sel, roi, 0)
    if exclude is not None:
        sel &= ~exclude
    for j in range(i, len(lumas)):
        if int((np.abs(lumas[j] - base) > th)[sel].sum()) >= min_px:
            return j
    return None


def latency(lumas, frames_rows, events_rows, roi=None, cue_channel=CUE_CHANNELS, max_frames=1, need_cue=False):
    """For every press in events.tsv (channel mouse or key), the frames from the press to the first visible change
    (pointer area excluded) and to the nearest cue. Returns {'presses': [...], 'pass': ...}."""
    index_of = {}
    for row in frames_rows:
        try:
            index_of[int(row['frame'])] = int(row['i'])
        except (KeyError, ValueError):
            pass
    states = [frame_state(r) for r in frames_rows]
    channels = (cue_channel,) if isinstance(cue_channel, str) else tuple(cue_channel)
    cues = [int(e['frame']) for e in events_rows if e.get('channel') in channels]
    presses = []
    for e in events_rows:
        if e.get('channel') not in ('mouse', 'key'):
            continue
        frame = int(e['frame'])
        i = index_of.get(frame)
        if i is None or i < 1:
            continue
        exclude = None
        if states and i < len(states) and states[i]['mouse'] is not None:
            exclude = np.zeros(lumas[0].shape, dtype=bool)
            mx, my = states[i]['mouse']
            _fill(exclude, (mx - 24, my - 24, mx + 24, my + 24), 0)
        j = first_change(lumas, i, roi, exclude)
        visual = None if j is None else j - i
        cue = min((c - frame for c in cues), key=abs, default=None)
        ok = visual is not None and visual <= max_frames and (not need_cue or (cue is not None and abs(cue) <= 1))
        presses.append({'frame': frame, 'i': i, 'name': e.get('name', ''), 'visual_frames': visual, 'cue_frames': cue,
                        'pass': ok})
    return {'presses': presses, 'pass': bool(presses) and all(p['pass'] for p in presses)}


def sound(events_rows, max_frames=1, window=6, need_cue=False, cue_channels=CUE_CHANNELS):
    """The press-to-sound check (GAME_FEEL_BIBLE check 2: "the cue fires on the press frame +-1"): for every press in
    events.tsv (channel mouse or key), the first sound cue (CUE_CHANNELS) from one frame before the press to `window`
    frames after it. A press is 'on time' when that cue is at most max_frames after it, 'late' when it comes later in
    the window, and 'silent' with no cue in the window. Late presses fail; silent ones fail only with need_cue (not
    every press answers with a sound: a view key moves the camera). Returns {'presses': [...], 'cues': n, 'pass': ...}
    with each press's frame, name, the cue's offset in frames ('cue_frames', None when silent), its name and state."""
    cues = []
    for e in events_rows:
        if e.get('channel') in cue_channels:
            try:
                cues.append((int(e['frame']), e.get('name', '')))
            except (KeyError, ValueError):
                pass
    cues.sort()
    presses = []
    for e in events_rows:
        if e.get('channel') not in ('mouse', 'key'):
            continue
        try:
            frame = int(e['frame'])
        except (KeyError, ValueError):
            continue
        answer = next(((f, n) for f, n in cues if frame - 1 <= f <= frame + window), None)
        if answer is None:
            state, offset, name = 'silent', None, ''
        else:
            offset, name = answer[0] - frame, answer[1]
            state = 'on time' if offset <= max_frames else 'late'
        ok = state == 'on time' or (state == 'silent' and not need_cue)
        presses.append({'frame': frame, 'name': e.get('name', ''), 'cue_frames': offset, 'cue': name, 'state': state,
                        'pass': ok})
    return {'presses': presses, 'cues': len(cues), 'pass': all(p['pass'] for p in presses)}


# ----------------------------------------------------------------------------------------------------------------------
# Frozen frames (BUG-31) and single-frame cuts (BUG-32)

def frozen(lumas, still=0.5, fps=FPS):
    """The longest run of frames that do not change (mean |diff| < still), in frames and seconds, and where."""
    best, run, start, best_start = 0, 0, 0, 0
    for i in range(1, len(lumas)):
        if float(np.abs(lumas[i] - lumas[i - 1]).mean()) < still:
            if run == 0:
                start = i - 1
            run += 1
            if run > best:
                best, best_start = run, start
        else:
            run = 0
    return {'frames': best + 1 if best else 0, 'seconds': (best + 1) / fps if best else 0.0, 'start': best_start}


def cuts(lumas, th=15.0):
    """Single-frame jumps (BUG-32: nothing pops): the frames whose mean luma changes by more than th (0..255; the knob,
    seqtools --cut-th) from the frame before, in one frame, against steady neighbours. Frame i is a cut when
    |m[i] - m[i-1]| > th, the change into frame i-1 is at most th/2, and either the change out of frame i is at most th/2
    (a step) or the next frame comes back (|m[i+1] - m[i-1]| <= th/2: a one-frame pop). An eased move or fade spreads
    its change over frames, so its neighbours never hold and it is no cut. Returns [(i, the jump in luma)]."""
    m = [float(np.asarray(lum, dtype=np.float32).mean()) for lum in lumas]
    n = len(m)
    out = []
    for i in range(1, n):
        jump = abs(m[i] - m[i - 1])
        if jump <= th:
            continue
        before = abs(m[i - 1] - m[i - 2]) if i >= 2 else 0.0
        after = abs(m[i + 1] - m[i]) if i + 1 < n else 0.0
        back = i + 1 < n and abs(m[i + 1] - m[i - 1]) <= th / 2
        if before <= th / 2 and (after <= th / 2 or back):
            out.append((i, round(jump, 1)))
    return out


# ----------------------------------------------------------------------------------------------------------------------
# Frame colour (GATE-05: a captured frame must show the game's colour, as a still of the same frame does)

def colour_match(frame_rgb, still_rgb, roi=None):
    """A captured frame against the full-size still of the same picture (SeqRecorder's 'still' row): the still is box
    filtered to the frame's size, then the mean R, G, B of each are compared, over the whole picture and over roi
    (x0, y0, x1, y1 fractions, origin top-left) when given. Returns {'frame': (r, g, b), 'still': (r, g, b), 'diff': the
    largest channel difference, 'roi_frame', 'roi_still', 'roi_diff' (None without roi)}."""
    frame = np.asarray(frame_rgb, dtype=np.float32)[..., :3]
    h, w = frame.shape[:2]
    still = np.asarray(Image.fromarray(np.asarray(still_rgb, dtype=np.uint8)[..., :3]).resize((w, h), Image.BOX), dtype=np.float32)

    def means(a, box):
        if box is not None:
            x0, y0, x1, y1 = box
            a = a[int(y0 * h):max(int(y0 * h) + 1, int(y1 * h)), int(x0 * w):max(int(x0 * w) + 1, int(x1 * w))]
        return tuple(round(float(v), 1) for v in a.reshape(-1, 3).mean(axis=0))

    out = {'frame': means(frame, None), 'still': means(still, None), 'roi_frame': None, 'roi_still': None, 'roi_diff': None}
    out['diff'] = round(max(abs(a - b) for a, b in zip(out['frame'], out['still'])), 1)
    if roi is not None:
        out['roi_frame'], out['roi_still'] = means(frame, roi), means(still, roi)
        out['roi_diff'] = round(max(abs(a - b) for a, b in zip(out['roi_frame'], out['roi_still'])), 1)
    return out


def hitches(frames_rows, budget_ms=1000.0 / 30 + 0.1):
    """Frame times from frames.tsv's dtMs (recorded with real time, the realtime op): frames over budget_ms
    (BUG-65: no hitch over one frame at call-next or on a drop), the worst one, and the median."""
    times = []
    for row in frames_rows:
        try:
            times.append((int(row['i']), float(row['dtMs'])))
        except (KeyError, ValueError):
            pass
    over = [(i, t) for i, t in times if t > budget_ms]
    ms = sorted(t for _, t in times)
    return {'frames': len(times), 'over': over, 'worst': max(ms) if ms else 0.0, 'median': ms[len(ms) // 2] if ms else 0.0}


# ----------------------------------------------------------------------------------------------------------------------
# Block artefacts (VIS audit's blockiness.py)

def rain_motion(rgb_frames, fps=FPS):
    """The rain's motion and slant from consecutive frames (CITY-WEATHER direction): the median of the frames is the static
    city, so each frame less the median holds the streaks alone. The shift of the streaks between frames i and i+1 is the
    peak of their circular cross-correlation (the tile wraps). Returns dx, dy in px per second (screen, top-left origin,
    dy > 0 is down), the streak axis pointing down the screen, and the angle in degrees between the motion and that axis
    (0: the streaks move along their slant with the lower end leading; 90 and over: across it or the wrong way)."""
    gray = [np.asarray(f, dtype=np.float32)[..., :3].mean(axis=2) for f in rgb_frames]
    bg = np.median(np.stack(gray), axis=0)
    hp = [g - bg for g in gray]
    shifts = []
    for a, b in zip(hp, hp[1:]):
        corr = np.fft.ifft2(np.conj(np.fft.fft2(a)) * np.fft.fft2(b)).real
        dy, dx = np.unravel_index(np.argmax(corr), corr.shape)
        shifts.append((dx - corr.shape[1] if dx > corr.shape[1] // 2 else dx, dy - corr.shape[0] if dy > corr.shape[0] // 2 else dy))
    mdx, mdy = float(np.median([s[0] for s in shifts])) * fps, float(np.median([s[1] for s in shifts])) * fps
    gy, gx = np.gradient(hp[len(hp) // 2])
    g = 0.5 * np.arctan2(2 * float((gx * gy).sum()), float((gx * gx).sum() - (gy * gy).sum()))   # the dominant gradient's angle
    ax, ay = -np.sin(g), np.cos(g)                                                                 # the streak axis, perpendicular to it
    if ay < 0:
        ax, ay = -ax, -ay                                                                          # pointing down the screen
    norm = float(np.hypot(mdx, mdy)) or 1.0
    angle = float(np.degrees(np.arccos(np.clip((mdx * ax + mdy * ay) / norm, -1.0, 1.0))))
    return {'dx': mdx, 'dy': mdy, 'axis_dx': float(ax), 'axis_dy': float(ay), 'angle': angle}


def blockiness(rgba):
    """The 8x8 block-edge ratio of an RGBA image (opaque pixels only): ~1 for clean art, well above 1 for JPEG-like
    blocks."""
    a = np.asarray(rgba).astype(np.float64)
    if a.shape[-1] == 3:
        a = np.concatenate([a, np.full(a.shape[:2] + (1,), 255.0)], axis=-1)
    m = a[:, :, 3] > 250
    g = a[:, :, :3].mean(2)
    dx = np.abs(np.diff(g, axis=1))
    mm = m[:, 1:] & m[:, :-1]
    cols = np.arange(dx.shape[1])
    b = (cols % 8 == 7)
    dy = np.abs(np.diff(g, axis=0))
    my = m[1:, :] & m[:-1, :]
    rows = np.arange(dy.shape[0])
    br = (rows % 8 == 7)
    def ratio(on, off):
        if not on.size or not off.size:
            return 1.0
        if off.mean() == 0:
            return float('inf') if on.mean() > 0 else 1.0
        return float(on.mean() / off.mean())
    return (ratio(dx[:, b][mm[:, b]], dx[:, ~b][mm[:, ~b]]) + ratio(dy[br, :][my[br, :]], dy[~br, :][my[~br, :]])) / 2


# ----------------------------------------------------------------------------------------------------------------------
# Gate results (Library/QA/results/<id>.json, read by DemoGate; the C# QaGateResult's fields)

def gate_result(rid, owner, ok, measured, detail, commit='', source='', built_player=False, dirty=False, na=False):
    """A gate result dict in QaGateResult's shape (dirty: measured on uncommitted changes, so not a measurement of
    commit; DemoGate counts it as stale. na: the check did not apply, a neutral result the gate lists and counts in
    neither pass nor fail; it is written with pass true, so a reader that knows no n/a never sees it as red)."""
    return {'id': rid, 'owner': owner, 'pass': bool(ok) or bool(na), 'measured': measured, 'detail': detail,
            'commit': commit or '', 'dirty': bool(dirty), 'written': datetime.datetime.now().strftime('%Y-%m-%dT%H:%M:%S'),
            'source': source, 'builtPlayer': bool(built_player), 'na': bool(na)}


def result_file_name(rid):
    """GateResults.FileName: the id with anything but letters, digits, '.', '_' and '-' made '_'."""
    return ''.join(ch if ch.isalnum() or ch in '._-' else '_' for ch in (rid or 'unnamed')) + '.json'


def write_result(folder, result):
    """Writes one gate result file into folder (created)."""
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, result_file_name(result['id']))
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(result, f, indent=4)
    return path


def results_from_run(run, commit, source, built_player, dirty=False):
    """A run.json's assertions as gate results (GateResults.FromRun: built-player ids get the 'bp.' prefix)."""
    out = []
    for a in run.get('assertions', []):
        rid = ('bp.' if built_player else '') + a.get('id', '')
        out.append(gate_result(rid, a.get('owner', ''), a.get('pass', False), a.get('measured', ''), a.get('detail', ''),
                               commit, source, built_player, dirty, a.get('na', False)))
    return out


# ----------------------------------------------------------------------------------------------------------------------
# The known-red list (tools/qa/gate_known_red.txt; DemoGate.ParseKnownRed / KnownRedRunPatterns / JudgeRun)

def matches(pattern, rid):
    """GateResults.Matches: '*' matches any run of characters, on the whole id, case-insensitive."""
    if pattern is None or rid is None:
        return False
    rx = '^' + '.*'.join(re.escape(p) for p in pattern.split('*')) + '$'
    return re.match(rx, rid, re.IGNORECASE) is not None


def known_red_run_patterns(text):
    """The run patterns of the known-red list: every word after a line's check id (before its '#')."""
    out = []
    for raw in (text or '').split('\n'):
        line = raw.strip()
        if not line or line.startswith('#'):
            continue
        words = line.split('#', 1)[0].split()
        for w in words[1:]:
            if w not in out:
                out.append(w)
    return out


def judge(failures, patterns):
    """Splits failures [(id, line)] into (unexpected lines, known-red lines) by the known-red run patterns."""
    unexpected, known = [], []
    for rid, line in failures:
        (known if any(matches(p, rid) for p in patterns) else unexpected).append(line)
    return unexpected, known


def judge_run(run, patterns):
    """DemoGate.JudgeRun: a run.json's failed steps and red assertions as (unexpected, known red) lines."""
    unexpected = [f"step {s.get('i')} {s.get('op')}: {s.get('note', '')}" for s in run.get('steps', []) if not s.get('ok')]
    red = [(a.get('id', ''), f"{a.get('id', '')} [{a.get('owner', '')}] {a.get('measured', '')}: {a.get('detail', '')}")
           for a in run.get('assertions', []) if not a.get('pass')]
    more, known = judge(red, patterns)
    unexpected += more
    if not run.get('ok') and not unexpected and not known:
        unexpected.append('the run stopped before its last step')
    return unexpected, known
