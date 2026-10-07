"""Process GPT's 1980s character sources into the game's 80s art set.

Reads the raw sources ChatGPT delivered on codex/textured-travellers-all
(ArtDeliverables/TimeDesk/Characters/RetroRegeneration/Raw: flat green
backgrounds, wardrobe on flat magenta fitting figures, 1024 x 1536) and
writes runtime keys (LookKeys grammar, the pose suffix "__{pose}" included)
into the 80s art set, Assets/Art/Characters/Resources/Characters/80s/{key}.png,
which CharacterArt draws a traveller from only when every layer of their look
is there (a traveller never mixes the 80s set with the classic one).

    python tools/characters/process_retro.py                 # stage everything (Temp/CharacterRetro)
    python tools/characters/process_retro.py --only premade_caesar_neutral ...
    python tools/characters/process_retro.py --qa <dir>      # contact sheets of the staged keys
    python tools/characters/process_retro.py --install       # copy the accepted keys (+ metas) into the 80s set

Decisions (README "1980s sources"):

* Registration. GPT draws every source of one gender on the same fitting
  figure, so one similarity per gender maps it to LookCanvas: the base's
  scalp to HeadTop, its soles to Feet, its skull's centre line to CenterX.
  Each wardrobe source is checked against that figure (its visible fitting
  figure's contour fitted onto the base's silhouette in raw space; arms are
  left out, since pose sources move them): a source more than MAX_SHIFT px or
  MAX_SCALE off is rejected, never stretched or nudged. Every premade image
  takes one similarity, from GPT's premade drawing convention (eyes at raw
  y 340, soles at 1473) to the fitting figure's eye line and Feet; an
  expression or pose frame whose soles or centre sit more than MAX_SHIFT px
  from its neutral's is rejected.
* Skin. Heads keep GPT's own drawing of each skin and face. Body skin 1 is
  the bare fitting figure; bodies 2-5 are recoloured from it (contract
  section 6, so every outfit fits every body) to GPT's own drawn tones
  (each skin's face-a forehead), so a body always matches its head (GPT's
  other skins' bases wear calibration clothes: only their heads are used).
  The classic set's CharacterSwatches are not imposed: they are only the
  pipeline's targets, and GPT's five tones are the 80s set's.
* Hair and beards are baked into the five LookKeys.HairColours (a wig keeps
  its colour); hair flagged "back" also gives its hairback key.
* Head items keep only what is connected to the head (the fitting figure's
  calibration clothes are dropped); the civil 2150 hair, drawn on a figure in
  a jacket, is cut just under the chin first.
* Mannequins are fitting guides only: never processed.
"""
import argparse
from concurrent.futures import ProcessPoolExecutor, as_completed
import hashlib
import json
from pathlib import Path
import re
import shutil
import time
import traceback

import numpy as np
from PIL import Image, ImageDraw

from charkit import contract, imgops as io, layers, landmarks as lm, measure, register as rg, unitymeta, warp
import process_library as pl

ROOT = Path(contract.ROOT)
RAW = ROOT / 'ArtDeliverables/TimeDesk/Characters/RetroRegeneration/Raw'
STAGE = ROOT / 'Temp/CharacterRetro'
ART_SET = '80s'
INSTALL = ROOT / contract.resources_folder() / ART_SET
C = contract.canvas()
SIZE = (C['Width'], C['Height'])
POSE = re.compile(r'^(?P<base>.+?)__(?P<pose>(explaining|thinking|objecting)_[abc])$')
EXPRESSIONS = ('neutral', 'happy', 'angry', 'worried', 'photo')

# The largest drift a source may show from its fitting figure (px, raw) and the largest scale change.
MAX_SHIFT = 6.0
MAX_SCALE = 0.015
# The contour residual (px, mean of the inliers) above which a source is rejected.
MAX_RESIDUAL = 2.5


class Skip(Exception):
    """A raw source that is not a runtime layer (a fitting guide, a name the game would not load)."""


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, default=lambda o: o.tolist() if hasattr(o, 'tolist') else str(o)), encoding='utf-8')


# --- What each source produces --------------------------------------------------------------

def wardrobe_item(g, nation, era, slot):
    """The world_source.json item of a place's gender in a slot ({} when the place or the item is missing)."""
    try:
        w = contract.wardrobe(nation, era)
    except KeyError:
        return None
    return w.get(g, {}).get(slot) or None


SLOT = {'outfit': 'outfit', 'hair': 'hair', 'facialhair': 'facialHair', 'headwear': 'headwear', 'accessory': 'accessory'}


def plan(name):
    """(kind, gender, keys, pose) for a raw source name; raises Skip for a name the game would not load."""
    m = POSE.match(name)
    base, pose = (m.group('base'), m.group('pose')) if m else (name, None)
    suffix = '__' + pose if pose else ''
    parts = base.split('_')
    kind = parts[0]
    colours = [c for c, _ in contract.hair_colours()]
    if kind == 'mannequin':
        raise Skip('a fitting guide, not a runtime layer')
    if kind == 'premade':
        if pose:
            return kind, None, [name], pose
        if len(parts) != 3 or parts[2] not in EXPRESSIONS:
            raise Skip('not premade_{id}_{expression}')
        return kind, None, [name], None
    g = parts[1]
    if kind == 'base':
        skin = int(parts[2][4:])
        keys = [contract.head_key(g, skin, 'a')]
        if skin == 1:
            keys += [contract.body_key(g, n) for n in range(1, 6)]
        return kind, g, keys, None
    if kind == 'head':
        return kind, g, [base], None
    if kind not in SLOT:
        raise Skip(f'unknown layer {kind}')
    nation, era = parts[2], parts[3]
    rest = parts[4:]
    if nation == 'civil':
        # Saleh 2026-10-07: the ID photo's 2150 civilian dress (outfit_{g}_civil_2150_v1..v3, hair_{g}_civil_2150_{colour}).
        if kind == 'outfit':
            if len(rest) != 1 or not re.fullmatch(r'v[1-3]', rest[0]):
                raise Skip('civil outfit without v1..v3')
            return kind, g, [base + suffix], pose
        if kind == 'hair':
            stem = f'hair_{g}_civil_2150'
            return kind, g, [f'{stem}_{c}' for c in colours], pose
        raise Skip('civil dress has only outfits and hair')
    item = wardrobe_item(g, nation, era, SLOT[kind])
    if not item or not item.get('label'):
        raise Skip(f'{nation}_{era} has no {SLOT[kind]} for {g}')
    if rest:
        raise Skip('unexpected tokens ' + '_'.join(rest))
    stem = f'{kind}_{g}_{nation}_{era}'
    if kind in ('hair', 'facialhair'):
        wig = kind == 'hair' and item.get('wig')
        back = kind == 'hair' and item.get('back')
        tails = [''] if wig else ['_' + c for c in colours]
        keys = []
        for t in tails:
            if back:
                keys.append(f'hairback_{g}_{nation}_{era}{t}')
            keys.append(stem + t)
        return kind, g, [k + suffix for k in keys], pose
    return kind, g, [stem + suffix], pose


# --- Registration -----------------------------------------------------------------------------

def figure_transform(g):
    """The similarity of a gender's fitting figure (base skin 1): scalp to HeadTop, soles to Feet, centre line to CenterX."""
    cache = STAGE / 'Records' / f'_figure_{g}.json'
    if cache.exists():
        return json.loads(cache.read_text())
    rgb = io.load_rgb(str(RAW / f'base_{g}_skin1_facea.png'))
    k = pl.key_source(rgb, mannequin=False)
    fig = k['alpha'] > .5
    top, soles = lm.top(fig), lm.bottom(fig)
    cx = lm.centre_x(fig, top + 60, top + 100)
    f = pl.face(rgb, k, False)
    s = (C['Feet'] - C['HeadTop']) / (soles - top)
    T = rg.Similarity(s, C['CenterX'] - s * cx, C['HeadTop'] - s * top)
    contour = lm.contour(fig)
    value = {'transform': T.as_dict(), 'top': top, 'soles': soles, 'centre_x': cx, 'face': f,
             'chinOnCanvas': float(T.apply([f['chin']])[0][1])}
    write_json(cache, value)
    np.save(STAGE / 'Records' / f'_figure_{g}_contour.npy', contour)
    np.save(STAGE / 'Records' / f'_figure_{g}_mask.npy', fig)
    return value


def similarity(d):
    return rg.Similarity(d['scale'], d['tx'], d['ty'])


def check_on_figure(g, keyed, chin_y, pose, kind):
    """Fits the source's visible fitting figure onto the gender's base silhouette (raw space, identity start); the stats, or raises when it is off."""
    base_mask = np.load(STAGE / 'Records' / f'_figure_{g}_mask.npy')
    base_edge = base_mask & ~io.erode(base_mask, 1)
    if kind in ('base', 'head'):
        # Head sources are other faces: their shoulders and arms (calibration clothes over a body) carry the check.
        fig = keyed['alpha'] > .5
        pts = lm.contour(fig)
    else:
        pts = measure.mannequin_contour(keyed)
    x, y = pts[:, 0], pts[:, 1]
    stable = np.ones(len(pts), bool)
    if pose:
        # Pose sources move the arms: only the head, neck and legs count.
        stable = (y < chin_y + 60) | (y > 1000)
    if kind == 'head':
        stable &= y > chin_y + 30
    pts = pts[stable]
    if len(pts) < 200:
        return {'skipped': f'only {len(pts)} visible fitting-figure contour points'}
    base_pts = lm.contour(base_mask)
    T, stats = rg.chamfer_fit(pts, base_edge, base_pts, rg.Similarity(), search_px=10, search_scale=0.03)
    stats.update({'scale': round(T.s, 4), 'tx': round(T.tx + (T.s - 1) * 512, 2), 'ty': round(T.ty + (T.s - 1) * 800, 2)})
    off = abs(T.s - 1) > MAX_SCALE or abs(stats['tx']) > MAX_SHIFT or abs(stats['ty']) > MAX_SHIFT
    if off or stats['contour_mean_px'] > MAX_RESIDUAL:
        raise RuntimeError(f'not registered on its fitting figure: {stats}')
    return stats


def premade_gender(pid):
    world = json.loads((ROOT / contract.WORLD_SOURCE).read_text(encoding='utf-8'))
    p = next((p for p in world['premades'] if p['id'] == pid), None)
    if p is None:
        raise Skip(f'premade {pid} is not in world_source.json')
    return 'f' if p.get('gender') == 'Female' else 'm'


# GPT draws every premade image to one convention (measured on the 33 neutral
# images: the eye line at raw y 340 within a few px, the soles at 1473 (median;
# robes and shoes 1447-1477), the face centred on x 512). Automatic eye
# finding is unreliable on beards, glasses, hats and veils, so the whole cast
# takes that convention's similarity: every head keeps GPT's placement.
PREMADE_EYES = 340.0
PREMADE_SOLES = 1473.0
PREMADE_CENTRE = 512.0


def premade_transform(pid, image='neutral'):
    """
    A premade's similarity (every image of the cast, the ID photo included):
    the cast's eye line onto the eye line of its gender's registered fitting
    figure (where the bubble and the photo crop expect a generated
    traveller's face), the cast's soles onto Feet, its centre onto CenterX.
    """
    fig = figure_transform(premade_gender(pid))
    Tf = similarity(fig['transform'])
    target_eyes = float(np.mean(Tf.apply([fig['face']['eye_l'], fig['face']['eye_r']])[:, 1]))
    s = (C['Feet'] - target_eyes) / (PREMADE_SOLES - PREMADE_EYES)
    T = rg.Similarity(s, C['CenterX'] - s * PREMADE_CENTRE, target_eyes - s * PREMADE_EYES)
    rgb = io.load_rgb(str(RAW / f'premade_{pid}_neutral.png'))
    k = pl.key_source(rgb, mannequin=False)
    return {'transform': T.as_dict(), 'soles': lm.bottom(k['alpha'] > .5)}


# --- Isolation --------------------------------------------------------------------------------

def remove_face_marks(a, f, kind):
    """Drops the fitting figure's isolated face marks (keyed as item) strictly inside the face."""
    labels, n = io.label(a > .15)
    if not n:
        return a, 0
    counts = np.bincount(labels.ravel())
    yy, xx = np.mgrid[:a.shape[0], :a.shape[1]]
    mark_bottom = f['nose'][1] + 5 if kind == 'facialhair' else f['chin'][1] - 5
    interior = (abs(xx - f['centre_x']) < 60) & (yy > min(f['eye_l'][1], f['eye_r'][1]) - 45) & (yy < mark_bottom)
    ids = np.unique(labels[interior])
    inside = np.bincount(labels[interior], minlength=n + 1)
    drop = np.zeros(n + 1, bool)
    drop[ids] = (counts[ids] < 1800) & (inside[ids] == counts[ids])
    drop[0] = False
    removed = int((drop[labels] & (a > .15)).sum())
    return a * ~drop[labels], removed


def keep_head_connected(a, chin_y, kind):
    """Keeps only the parts connected to the head (seeded above the chin; a beard from just under it)."""
    labels, n = io.label(a > .15)
    keep = np.zeros(n + 1, bool)
    seed_bottom = int(chin_y + (105 if kind == 'facialhair' else 45))
    keep[np.unique(labels[:seed_bottom])] = True
    keep[0] = False
    return a * keep[labels]


# --- One source -------------------------------------------------------------------------------

def save(key, rgb, alpha, rec):
    if not (alpha > .5).any():
        raise RuntimeError('Empty output: ' + key)
    visible = alpha > 0
    bled, _ = io.bleed(np.where(visible[..., None], rgb, 0), visible, 6)
    rgb = np.where(visible[..., None], rgb, bled)
    out = STAGE / 'Sprites' / (key + '.png')
    io.save_rgba(str(out), rgb, alpha)
    ys, xs = np.nonzero(alpha > .5)
    key_px = ((rgb[..., 1] - np.maximum(rgb[..., 0], rgb[..., 2]) > 100) |
              ((np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1] > 100) &
               (np.minimum(rgb[..., 0], rgb[..., 2]) > .6 * np.maximum(rgb[..., 0], rgb[..., 2])))) & (alpha > .1)
    rec['keys'][key] = {'bbox': [int(xs.min()), int(ys.min()), int(xs.max() + 1), int(ys.max() + 1)],
                        'keyColourPixels': int(key_px.sum()),
                        'sha256': hashlib.sha256(out.read_bytes()).hexdigest()}
    if key_px.sum():
        raise RuntimeError(f'{key}: {int(key_px.sum())} key-colour pixels survive')


def skin_tone(g, n):
    """GPT's drawn skin tone n of a gender: the median forehead colour of its face-a base (between the brows and the scalp)."""
    rgb = io.load_rgb(str(RAW / f'base_{g}_skin{n}_facea.png'))
    k = pl.key_source(rgb, mannequin=False)
    f = pl.face(rgb, k, False)
    ey = int((f['eye_l'][1] + f['eye_r'][1]) / 2)
    cx = int(f['centre_x'])
    return np.median(rgb[ey - 50:ey - 28, cx - 16:cx + 16].reshape(-1, 3), axis=0)


def recolour_skin(rgb, alpha, src, dst):
    """
    The bare body in another tone: every pixel but the grey undergarment takes
    the target tone's hue at its own brightness relative to the source tone
    (linear light), so the shading, the pigment texture and the outlines keep
    their ratios. A per-channel or hue-window recolour leaves the textured
    80s art's light speckles in the old tone.
    """
    h, s, v = layers.hsv(rgb)
    grey = (s < .14) & (v > .3) & (alpha > .5)
    grey = io.dilate(io.erode(grey, 3), 4) & (s < .2)
    lin = layers.to_linear(rgb)
    ratio = layers.luminance(lin) / max(float(layers.luminance(layers.to_linear(src)[None, :])[0]), 1e-4)
    moved = layers.to_srgb(layers.to_linear(dst)[None, None, :] * ratio[..., None])
    w = ((alpha > 0) & ~grey).astype(np.float32)
    return rgb + (moved - rgb) * w[..., None]


def split_head(rgb, alpha, f):
    """
    Splits a figure into (head_alpha, body_rgb, body_alpha, stats) at the
    jaw. The 80s figures have a short neck that widens into the shoulders just
    under the chin, so the neck's columns are its run on the chin row less
    6 px a side. Over them the head ends at the jaw outline's last dark row;
    beside them at the jaw's level at the neck's edges (ears and skull above,
    neck sides below). The body's neck is continued upwards behind the face
    to 40 px above the chin with the row under the jaw (hidden by any head).
    """
    h, w = alpha.shape
    chin, cx = f['chin'][1], f['centre_x']
    xs = np.nonzero(alpha[int(round(chin))] > .5)[0]
    runs = np.split(xs, np.nonzero(np.diff(xs) > 1)[0] + 1)
    run = next(r for r in runs if r[0] <= cx <= r[-1])
    nl, nr = int(run[0]) + 6, int(run[-1]) - 6
    ey = int((f['eye_l'][1] + f['eye_r'][1]) / 2)
    skin_lum = float(np.median(io.lum(rgb[ey - 50:ey - 28, int(cx) - 16:int(cx) + 16])))
    dark = (io.lum(rgb) < min(110.0, .62 * skin_lum)) & (alpha > .5)
    ya, yb = int(chin - 70), int(chin + 6)
    jaw = np.full(nr - nl + 1, -1)
    for i, x in enumerate(range(nl, nr + 1)):
        ys = np.nonzero(dark[ya:yb, x])[0]
        if not len(ys):
            continue
        breaks = np.nonzero(np.diff(ys) > 1)[0]
        start = ys[breaks[-1] + 1] if len(breaks) else ys[0]
        if ys[-1] - start <= 8:
            jaw[i] = ya + ys[-1]
    valid = np.nonzero(jaw >= 0)[0]
    if not len(valid):
        raise RuntimeError('no jaw outline over the neck')
    for i in np.nonzero(jaw < 0)[0]:
        jaw[i] = jaw[valid[np.argmin(np.abs(valid - i))]]
    yy, xx = np.mgrid[0:h, 0:w]
    cut = np.full(w, int(max(jaw[0], jaw[-1])))
    cut[nl:nr + 1] = jaw
    head = yy <= cut[None, :]
    # A head source's calibration shirt can reach the cut beside the neck: its grey never belongs to a head.
    _, sat, val = layers.hsv(rgb)
    head &= ~((sat < .15) & (val > .35) & (yy > chin - 30))
    head_alpha = alpha * head
    body_alpha = alpha * ~head
    body_rgb = rgb.copy()
    y_ref = int(jaw.max() + 4)
    y_ext = int(chin - 40)
    for x in range(nl, nr + 1):
        if alpha[y_ref, x] <= 0:
            continue
        body_rgb[y_ext:cut[x] + 1, x] = rgb[y_ref, x]
        body_alpha[y_ext:cut[x] + 1, x] = alpha[y_ref, x]
    return head_alpha, body_rgb, body_alpha, {'neck': [nl, nr], 'jaw': [int(jaw.min()), int(jaw.max())]}


def process(name):
    rec = {'source': str((RAW / (name + '.png')).relative_to(ROOT)).replace('\\', '/'), 'name': name, 'keys': {}}
    try:
        kind, g, keys, pose = plan(name)
        rec.update({'kind': kind, 'pose': pose, 'produces': keys})
        rgb = io.load_rgb(str(RAW / (name + '.png')))
        mannequin = kind not in ('base', 'head', 'premade')
        k = pl.key_source(rgb, mannequin=mannequin)
        a = k['alpha'].copy()
        if (a > .5).sum() < 200:
            raise RuntimeError("nothing left after keying: the item is drawn in the fitting figure's magenta-tinted ink, so it cannot be told from the figure's face marks")

        if kind == 'premade':
            pid = name.split('_')[1]
            ref = premade_transform(pid)
            T = similarity(ref['transform'])
            soles = lm.bottom(a > .5)
            fig = a > .5
            top = lm.top(fig)
            cx = lm.centre_x(fig, top + 40, top + 100)
            neutral = io.load_rgb(str(RAW / f'premade_{pid}_neutral.png'))
            kn = pl.key_source(neutral, mannequin=False)
            fn = kn['alpha'] > .5
            tn = lm.top(fn)
            if name.endswith('_photo'):
                # The ID photo is registered on its own eyes and soles (other clothes and shoes).
                drift = {}
            else:
                drift = {'soles': soles - ref['soles'], 'centre_x': cx - lm.centre_x(fn, tn + 40, tn + 100), 'top': top - tn}
            rec['driftFromNeutral'] = drift
            if any(abs(v) > MAX_SHIFT for k_, v in drift.items() if k_ != 'top'):
                raise RuntimeError(f'not registered on its neutral image: {drift}')
            rec['transform'] = ref['transform']
            crgb, ca = warp.warp(k['rgb'], a, T, SIZE)
            ys, xs = np.nonzero(ca > .5)
            if xs.min() < 0 or xs.max() >= C['Width'] or ys.min() < 0:
                raise RuntimeError('figure outside the canvas')
            for key in keys:
                save(key, crgb, ca, rec)
            rec['status'] = 'processed'
            return rec

        fig_ref = figure_transform(g)
        T = similarity(fig_ref['transform'])
        f_base = fig_ref['face']
        rec['transform'] = fig_ref['transform']
        rec['figureCheck'] = check_on_figure(g, k, f_base['chin'][1], pose, kind)

        if kind in ('base', 'head'):
            f = pl.face(rgb, k, False)
            rec['measuredFace'] = f
            head_a, body_rgb, body_a, rec['split'] = split_head(k['rgb'], a, f)
            hrgb, ha = warp.warp(k['rgb'], head_a, T, SIZE)
            if kind == 'head':
                # GPT drew faces b-d of a skin a little off its face-a tone (an older face often pinker):
                # moved onto it per channel in linear light, so a head always matches its body.
                ey = int((f['eye_l'][1] + f['eye_r'][1]) / 2)
                cx = int(f['centre_x'])
                own = np.median(rgb[ey - 50:ey - 28, cx - 16:cx + 16].reshape(-1, 3), axis=0)
                tone = skin_tone(g, int(name.split('_')[2][4:]))
                hrgb = layers.recolour(hrgb, layers.skin_weight(hrgb, ha, own), own, tone)
                rec['toneMoved'] = [own, tone]
            save(keys[0], hrgb, ha, rec)
            if len(keys) > 1:
                brgb, ba = warp.warp(body_rgb, body_a, T, SIZE)
                tones = [skin_tone(g, n) for n in range(1, 6)]
                rec['skinTones'] = tones
                for n, key in enumerate(keys[1:], start=1):
                    out = brgb if n == 1 else recolour_skin(brgb, ba, tones[0], tones[n - 1])
                    save(key, out, ba, rec)
            rec['status'] = 'processed'
            return rec

        # Wardrobe on the fitting figure.
        try:
            f = pl.face(rgb, k, True)
        except (RuntimeError, ValueError, IndexError) as e:
            f = None
            rec['faceMarks'] = 'not found: ' + str(e)
        if f is not None:
            a, removed = remove_face_marks(a, f, kind)
            rec['removedFaceMarkPixels'] = removed
        chin = f['chin'][1] if f is not None else f_base['chin'][1]
        if kind == 'hair' and name.startswith('hair_') and '_civil_' in name:
            # The jacket guide: nothing under the chin's level, and under the eyes only the hair's own colour
            # (measured on the crown) or its dark outline stays.
            a[int(chin + 12):] = 0
            ey = int((f['eye_l'][1] + f['eye_r'][1]) / 2) if f is not None else int(chin - 85)
            hue, sat, val = layers.hsv(k['rgb'])
            crown = (a > .99) & (sat > .1) & (val > .07)
            crown[ey - 40:] = False
            src = layers.dominant(k['rgb'], crown)
            hs = float(layers.hsv(src[None, None, :])[0][0, 0])
            other = ((layers.hue_dist(hue, hs) > 30) & (val > .3) & (sat > .12)) | ((val > .7) & (sat < .18))
            other[:ey] = False
            a = a * ~io.dilate(other, 1)
            labels, n = io.label(a > .15)
            keep = np.zeros(n + 1, bool)
            keep[np.unique(labels[:ey])] = True
            keep[0] = False
            a = a * keep[labels]
            rec['isolation'] = 'cut 12 px under the chin, and under the eyes every colour but the hair colour (the jacket guide)'
        if kind in ('hair', 'facialhair', 'headwear'):
            a = keep_head_connected(a, chin, kind)
        crgb, ca = warp.warp(k['rgb'], a, T, SIZE)

        if kind in ('hair', 'facialhair'):
            front, back = ca, None
            if any(key.startswith('hairback_') for key in keys):
                fc = {n: T.apply([f_base[n]])[0] for n in ('eye_l', 'eye_r', 'chin')}
                front, back = layers.split_hairback(crgb, ca, fc['chin'][1], C['CenterX'],
                                                    face_half_width=abs(fc['eye_r'][0] - fc['eye_l'][0]))
                if not (back > .5).any():
                    yy, xx = np.mgrid[:C['Height'], :C['Width']]
                    rear = (yy > fc['chin'][1] + 35) & (abs(xx - C['CenterX']) > 90)
                    back, front = ca * rear, ca * ~rear
                    rec['rearSplit'] = 'outer-hanging-hair'
            colours = dict(contract.hair_colours())
            coloured = any(key.split('__')[0].split('_')[-1] in colours for key in keys)
            if coloured:
                hue, sat, val = layers.hsv(crgb)
                sel = (ca > .99) & (sat > .1) & (val > .07) & (val < .9)
                if sel.sum() < 50:
                    # A thin, near-black moustache: its solid pixels whatever their saturation.
                    sel = (ca > .9) & (val < .9)
                src = layers.dominant(crgb, sel)
                rec['hairMeasured'] = src
            for key in keys:
                colour = key.split('__')[0].split('_')[-1]
                baked = layers.bake_hair(crgb, ca, src, colours[colour], keep_outline=colour != 'black') if colour in colours else crgb
                save(key, baked, back if key.startswith('hairback_') else front, rec)
        else:
            for key in keys:
                save(key, crgb, ca, rec)
        rec['status'] = 'processed'
    except Skip as e:
        rec['status'] = 'skipped'
        rec['error'] = str(e)
    except Exception as e:
        rec['status'] = 'rejected'
        rec['error'] = str(e)
        rec['traceback'] = traceback.format_exc()
    return rec


def run(rec_name):
    rec = process(rec_name)
    write_json(STAGE / 'Records' / (rec_name + '.json'), rec)
    return rec_name, rec['status'], rec.get('error', '')


# --- QA ---------------------------------------------------------------------------------------

ROOM = (92, 84, 96)


def load(key):
    p = STAGE / 'Sprites' / (key + '.png')
    if not p.exists():
        return None
    return np.asarray(Image.open(p).convert('RGBA'), np.float32) / 255.0


def compose(keys, bg=ROOM):
    out = np.zeros((C['Height'], C['Width'], 3), np.float32) + np.array(bg, np.float32) / 255.0
    for key in keys:
        im = load(key)
        if im is None:
            continue
        a = im[..., 3:]
        out = out * (1 - a) + im[..., :3] * a
    return Image.fromarray((out * 255).round().astype(np.uint8))


def guides(img, scale):
    d = ImageDraw.Draw(img)
    for y, col in ((C['HeadTop'], (255, 220, 0)), (C['Chin'], (0, 200, 255)), (C['Shoulders'], (255, 120, 0)), (C['Feet'], (255, 0, 0))):
        d.line([(0, y * scale), (img.width, y * scale)], fill=col)
    d.rectangle([C['PhotoLeft'] * scale, C['PhotoTop'] * scale, C['PhotoRight'] * scale, C['PhotoBottom'] * scale], outline=(255, 255, 255))
    return img


def sheet(rows, labels, cell, out, title=None):
    """rows: list of lists of PIL images (already cell-sized)."""
    w, h = cell
    cols = max(len(r) for r in rows)
    pad = 16
    img = Image.new('RGB', (cols * w, len(rows) * (h + pad) + (20 if title else 0)), (40, 34, 44))
    d = ImageDraw.Draw(img)
    y0 = 0
    if title:
        d.text((4, 4), title, fill=(255, 255, 255))
        y0 = 20
    for r, row in enumerate(rows):
        for c, im in enumerate(row):
            img.paste(im, (c * w, y0 + r * (h + pad) + pad))
            if labels[r][c]:
                d.text((c * w + 3, y0 + r * (h + pad) + 2), labels[r][c][:40], fill=(255, 255, 255))
    img.save(out)


def qa(out_dir):
    out = Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)
    keys = {p.stem for p in (STAGE / 'Sprites').glob('*.png')}
    scale = .25
    cw, ch = int(C['Width'] * scale), int(C['Height'] * scale)
    # Premades: every expression, pose and the photo (crop) per premade.
    pids = sorted({k.split('_')[1] for k in keys if k.startswith('premade_')})
    order = ['neutral', 'happy', 'angry', 'worried']
    rows, labels = [], []
    for pid in pids:
        row, lab = [], []
        names = [f'premade_{pid}_{e}' for e in order] + sorted(k for k in keys if k.startswith(f'premade_{pid}__'))
        for n in names:
            im = compose([n]).resize((cw, ch), Image.LANCZOS) if n in keys else Image.new('RGB', (cw, ch), (90, 0, 0))
            row.append(guides(im, scale))
            lab.append(n.replace(f'premade_{pid}', pid))
        photo = f'premade_{pid}_photo'
        if photo in keys:
            crop = compose([photo]).crop((C['PhotoLeft'], C['PhotoTop'], C['PhotoRight'], C['PhotoBottom']))
            pw = cw
            row.append(crop.resize((pw, int(pw * crop.height / crop.width)), Image.LANCZOS))
            lab.append(pid + ' photo')
        rows.append(row)
        labels.append(lab)
    for i in range(0, len(rows), 6):
        sheet(rows[i:i + 6], labels[i:i + 6], (cw, ch), out / f'premades_{i // 6 + 1}.png', 'premade: 4 expressions, 3 pose frames, ID photo crop (guides: head top, chin, shoulders, soles; photo rect)')
    # Bases: every head on its body.
    rows, labels = [], []
    for g in 'mf':
        for skin in range(1, 6):
            row, lab = [], []
            for face in 'abcd':
                ks = [f'body_{g}_skin{skin}', f'head_{g}_skin{skin}_face{face}']
                im = compose(ks).crop((262, 160, 762, 1000)).resize((250, 420), Image.LANCZOS)
                row.append(im)
                lab.append(f'{g} skin{skin} face{face}')
            rows.append(row)
            labels.append(lab)
    sheet(rows, labels, (250, 420), out / 'bases.png', 'bodies and heads (80s set)')
    # Egypt looks: each place and gender, neutral and its outfit's pose frames (on the neutral body).
    world = json.loads((ROOT / contract.WORLD_SOURCE).read_text(encoding='utf-8'))
    rows, labels = [], []
    order_layers = ['hairback', 'body', 'outfit', 'head', 'facialhair', 'hair', 'headwear', 'accessory']
    for place in world['places']:
        if place['country'] != 'egypt':
            continue
        era = place['era']
        for g in 'mf':
            look = place['wardrobe'][g]
            stack = {'body': f'body_{g}_skin3', 'head': f'head_{g}_skin3_facec'}
            missing = []
            for layer, slot in (('outfit', 'outfit'), ('hair', 'hair'), ('facialhair', 'facialHair'), ('headwear', 'headwear'), ('accessory', 'accessory')):
                item = look.get(slot) or {}
                if not item.get('label') or item.get('artNation'):
                    if item.get('label'):
                        missing.append(slot)
                    continue
                key = f'{layer}_{g}_egypt_{era}'
                if layer in ('hair', 'facialhair') and not item.get('wig'):
                    key += '_brown'
                if layer == 'hair' and item.get('back'):
                    stack['hairback'] = key.replace('hair_', 'hairback_', 1)
                stack[layer] = key
                if key not in keys:
                    missing.append(slot)
            covered = {c for it in look.values() if isinstance(it, dict) for c in it.get('covers', [])}
            if 'Hair' in covered:
                stack.pop('hair', None)
                stack.pop('hairback', None)
            row, lab = [], []
            ordered = [stack[l] for l in order_layers if l in stack]
            im = compose(ordered).resize((cw, ch), Image.LANCZOS)
            row.append(guides(im, scale))
            lab.append(f'{g} egypt {era}' + (' MISSING ' + ','.join(missing) if missing else ''))
            for p in sorted(k for k in keys if k.startswith(stack.get('outfit', '~') + '__')):
                posed = [p if s == stack['outfit'] else s for s in ordered]
                row.append(compose(posed).resize((cw, ch), Image.LANCZOS))
                lab.append(p.split('__')[1] + ' (outfit only)')
            rows.append(row)
            labels.append(lab)
    for i in range(0, len(rows), 6):
        sheet(rows[i:i + 6], labels[i:i + 6], (cw, ch), out / f'egypt_{i // 6 + 1}.png', 'Egypt looks, skin 3 face c, brown hair: neutral, then the outfit pose frames on the neutral body (body pose sources are missing)')
    # Civil 2150 photos: each variant on two skins and faces, as the passport crop.
    rows, labels = [], []
    for g in 'mf':
        for variant in ('v1', 'v2', 'v3'):
            row, lab = [], []
            for skin, face, colour in ((1, 'a', 'brown'), (3, 'b', 'black'), (5, 'c', 'grey'), (2, 'd', 'red'), (4, 'a', 'blond')):
                ks = [f'body_{g}_skin{skin}', f'outfit_{g}_civil_2150_{variant}', f'head_{g}_skin{skin}_face{face}', f'hair_{g}_civil_2150_{colour}']
                crop = compose(ks, (226, 222, 214)).crop((C['PhotoLeft'], C['PhotoTop'], C['PhotoRight'], C['PhotoBottom']))
                row.append(crop.resize((180, 225), Image.LANCZOS))
                lab.append(f'{g} {variant} s{skin}{face} {colour}')
            rows.append(row)
            labels.append(lab)
    sheet(rows, labels, (180, 225), out / 'civil_photos.png', 'ID photos in 2150 civilian dress (the photo crop)')


# --- Install ----------------------------------------------------------------------------------

def install():
    INSTALL.mkdir(parents=True, exist_ok=True)
    unitymeta.folder_meta(str(INSTALL))
    accepted = {}
    for p in sorted((STAGE / 'Records').glob('*.json')):
        if p.name.startswith('_'):
            continue
        rec = json.loads(p.read_text())
        if rec.get('status') == 'processed':
            accepted.update(rec['keys'])
    written = 0
    for key, info in sorted(accepted.items()):
        src = STAGE / 'Sprites' / (key + '.png')
        if hashlib.sha256(src.read_bytes()).hexdigest() != info['sha256']:
            raise SystemExit(f'{key}: staged file changed since its record')
        dst = INSTALL / (key + '.png')
        if not dst.exists() or dst.read_bytes() != src.read_bytes():
            shutil.copyfile(src, dst)
            written += 1
        unitymeta.texture_meta(str(dst), C['Height'], C['Height'] - C['Feet'])
    stale = sorted(p.stem for p in INSTALL.glob('*.png') if p.stem not in accepted)
    for key in stale:
        (INSTALL / (key + '.png')).unlink()
        meta = INSTALL / (key + '.png.meta')
        if meta.exists():
            meta.unlink()
    print(f'installed {len(accepted)} keys ({written} written, {len(stale)} stale removed) into {INSTALL.relative_to(ROOT)}')


def report():
    recs = [json.loads(p.read_text()) for p in sorted((STAGE / 'Records').glob('*.json')) if not p.name.startswith('_')]
    by = {}
    for r in recs:
        by.setdefault(r['status'], []).append(r)
    summary = {s: len(v) for s, v in by.items()}
    summary['keys'] = sum(len(r['keys']) for r in by.get('processed', []))
    summary['rejected_sources'] = {r['name']: r['error'] for r in by.get('rejected', [])}
    summary['skipped_sources'] = {r['name']: r['error'] for r in by.get('skipped', [])}
    write_json(STAGE / 'processing-report.json', summary)
    return summary


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--only', nargs='*')
    ap.add_argument('--workers', type=int, default=8)
    ap.add_argument('--qa')
    ap.add_argument('--install', action='store_true')
    args = ap.parse_args()
    if args.install:
        install()
        return
    if args.qa and not args.only:
        qa(args.qa)
        return
    (STAGE / 'Sprites').mkdir(parents=True, exist_ok=True)
    (STAGE / 'Records').mkdir(parents=True, exist_ok=True)
    names = sorted(p.stem for p in RAW.glob('*.png'))
    if args.only:
        names = [n for n in names if n in args.only]
    for g in 'mf':
        figure_transform(g)
    start = time.time()
    with ProcessPoolExecutor(max_workers=args.workers) as pool:
        jobs = [pool.submit(run, n) for n in names]
        for i, job in enumerate(as_completed(jobs), 1):
            name, status, error = job.result()
            print(f'{i}/{len(names)} {time.time() - start:.0f}s {name} {status} {error[:160]}', flush=True)
    print(json.dumps(report(), indent=1)[:4000])
    if args.qa:
        qa(args.qa)


if __name__ == '__main__':
    main()
