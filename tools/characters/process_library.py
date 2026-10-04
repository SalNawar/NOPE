"""Process the completed source queue, staging before installation.

Uses the approved charkit keying, premultiplied registration, colour baking
and Unity importer contract. Never invokes process_pilot or changes raw art.
Outputs and per-source decisions live in Temp/CharacterLibrary until reviewed.
"""
import argparse
from concurrent.futures import ProcessPoolExecutor, as_completed
import hashlib
import json
from pathlib import Path
import time
import traceback
from functools import lru_cache

import numpy as np
from PIL import Image
from charkit import contract, imgops as io, keying, layers, landmarks as lm, measure, register as rg, unitymeta, warp
import processing_stencils

processing_stencils.install()

ROOT = Path(contract.ROOT)
COMPLETION = ROOT / 'ArtDeliverables/TimeDesk/Characters/Completion'
STAGE = ROOT / 'Temp/CharacterLibrary'
C = contract.canvas()
SIZE = (C['Width'], C['Height'])


def key_source(rgb,mannequin):
    """Run the identical keyer in a padded foreground box, then restore canvas.

    Keep the original full-canvas background measurement. Forty clear pixels
    exceed every neighbourhood/bleed reach in the approved keyer. Pixels
    outside this box are necessarily green and have zero alpha and RGB.
    """
    bg=keying._stats(rgb)
    gd=rgb[:,:,1]-np.maximum(rgb[:,:,0],rgb[:,:,2])
    green=gd>=keying.GREEN_KEY*(bg[1]-max(bg[0],bg[2]))
    ys,xs=np.nonzero(~green)
    if not len(xs):
        raise RuntimeError('Blank source')
    y0,y1=max(0,int(ys.min())-40),min(rgb.shape[0],int(ys.max())+41)
    x0,x1=max(0,int(xs.min())-40),min(rgb.shape[1],int(xs.max())+41)
    original_stats=keying._stats
    keying._stats=lambda _:bg
    try:
        cropped=keying.key(rgb[y0:y1,x0:x1],mannequin)
    finally:
        keying._stats=original_stats
    result={}
    for name,value in cropped.items():
        shape=rgb.shape if name=='rgb' else rgb.shape[:2]
        canvas=np.full(shape,name=='green',dtype=value.dtype)
        canvas[y0:y1,x0:x1]=value
        result[name]=canvas
    return result


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, default=lambda o: o.tolist() if hasattr(o, 'tolist') else str(o)), encoding='utf-8')


def face(rgb, k, mannequin=False):
    """Fit visible facial features, excluding hair/hat from the scalp search.

    The pilot used silhouette top; a dressed figure's hat is not its scalp.
    Skin fill identifies the actual face. Magenta outlines may be brown, so
    their darkness includes all non-green pixels rather than purple alone.
    """
    fig = ~k['green']
    if mannequin:
        head = ((np.minimum(rgb[:520, :, 0], rgb[:520, :, 2])-rgb[:520, :, 1]) > 130) & fig[:520]
        head[:, :390] = False
        head[:, 634:] = False
        top = lm.top(head)
        xm = lm.centre_x(head, 310, 375)
        d = (255 - np.maximum(rgb[:520, :, 0], rgb[:520, :, 2])) * fig[:520] * io.dilate(head,6)
        y0, y1 = max(285, top + 25), min(390, top + 125)
    else:
        roi = rgb[190:390, 455:569]
        r, g, b = roi.transpose(2, 0, 1)
        skin_sel = (r > g + 8) & (g > b + 3) & (io.lum(roi) > 65)
        skin = layers.dominant(roi, skin_sel)
        luma = io.lum(rgb[:520])
        skin_h, _, _ = layers.hsv(skin[None, None])
        h, sat, _ = layers.hsv(rgb[:520])
        head = (layers.hue_dist(h, skin_h.item()) < 12) & (sat > .08) & (luma > io.lum(skin) * .9) & fig[:520]
        head[:, :420] = False
        head[:, 604:] = False
        head[:170] = False
        head[450:] = False
        # The forehead's broad skin band, avoiding isolated jewellery pixels.
        ys = np.nonzero(head.sum(1) > 40)[0]
        top = float(ys[0])
        xm = lm.centre_x(head, top + 45, min(top + 105, 430))
        d = (255 - luma) * fig[:520]
        # High contrast relative to that face's fill, including dark skin.
        d = np.where(luma < min(105, float(io.lum(skin)) * .65), d, 0)
        y0, y1 = top + 48, min(top + 115, 420)
    expected_eye = 340 if mannequin else top+90
    radii = (0, 1, 2, 3) if mannequin else (3, 2, 1, 0)
    for radius in radii:
        try:
            ya, yb = int(y0), int(y1)
            xa, xb = int(xm-50), int(xm+50)
            win = d[ya:yb, xa:xb]
            marks = win >= max(20, .45*np.percentile(win,99))
            opened = io.dilate(io.erode(marks,radius),radius) & marks
            labels,n = io.label(opened)
            candidates = []
            for j in range(1,n+1):
                ys,xs = np.nonzero(labels==j)
                w,h = xs.max()-xs.min()+1, ys.max()-ys.min()+1
                if len(xs)<8 or w>38 or h>28 or w>3*h:
                    continue
                candidates.append((float(xs.mean()+xa+.5),float(ys.mean()+ya+.5)))
            pairs = [(abs(l[1]-r[1])*4 + abs((l[1]+r[1])/2-expected_eye) +
                      .3*abs((r[0]-l[0])-55), l,r)
                     for l in candidates for r in candidates
                     if l[0]<xm-8 and r[0]>xm+8 and abs(l[1]-r[1])<7]
            if not pairs:
                raise RuntimeError('No matched eye pair')
            _,el,er = min(pairs)
            break
        except RuntimeError:
            if radius == radii[-1]:
                raise RuntimeError(f'Eyes not found: top={top}, centre={xm}, band={y0,y1}')
    ey = (el[1] + er[1]) / 2
    # Jaw and mouth use the original, unthresholded contrast.
    dark = (255 - np.maximum(rgb[..., 0], rgb[..., 2])) if mannequin else (255 - io.lum(rgb))
    dark *= fig
    win = dark[int(ey)+10:int(ey)+110, int(xm)-3:int(xm)+4]
    floor = float(np.percentile(win, 25))
    runs = lm.face_column(dark - floor, xm, ey + 12, min(ey + 115, 480), 25)
    nose = runs[0] if runs and runs[0][0] < ey+48 else (ey+27, ey+29, 0)
    mouth = next((r for r in runs[1:] if r[0] >= nose[1]+8), None)
    after = [r for r in runs if mouth and r[0] >= mouth[1]+10 and r[0] <= ey+105]
    chin = after[0] if after else None
    if chin is None and mannequin:
        # Calibration outlines may omit the jaw's centre stroke. This point
        # is used only for the isolation bounds, not fitting the head item.
        chin = (ey+82, ey+84, 0)
    if chin is None:
        # On dark skin the faint nose is absent, and the jaw shadow can
        # merge with its outline. The strongest centre-column jaw contrast
        # below the mouth gives the outline row, not the shadow's midpoint.
        ya, yb = int(ey+65), min(480, int(ey+110))
        col = dark[ya:yb, int(xm)-3:int(xm)+4].mean(1)
        jaw = ya + int(np.argmax(col))
        chin = (jaw, jaw+1, 0)
    if mouth is None:
        mouth = (ey+55, ey+57, 0)
    return {'eye_l': el, 'eye_r': er, 'nose': (xm, sum(nose[:2])/2),
            'mouth': (xm, sum(mouth[:2])/2), 'chin': (xm, sum(chin[:2])/2),
            'centre_x': xm, 'scalp': top}


def pilot_targets():
    result = {}
    for g in ('m', 'f'):
        path = ROOT / f'ArtDeliverables/TimeDesk/Characters/Raw/batch01-pilot/base_{g}_skin1_facea.png'
        rgb = io.load_rgb(str(path))
        k = key_source(rgb, mannequin=False)
        m = measure.base(rgb, k)
        s = (C['Feet']-C['HeadTop'])/(m['soles']-m['top'])
        T = rg.Similarity(s, C['CenterX']-s*m['centre_x'], C['HeadTop']-s*m['top'])
        fc = {n: T.apply([m['face'][n]])[0] for n in ('eye_l', 'eye_r', 'nose', 'mouth', 'chin')}
        result[g] = {'face': fc, 'transform': T.as_dict()}
    return result


@lru_cache(maxsize=26)
def neutral_registration(name):
    record=STAGE/'Records'/(name+'.json')
    if record.exists():
        r=json.loads(record.read_text())
        if r.get('status')=='processed':
            return r['measuredFace'],r['transform']
    tasks=json.loads((COMPLETION/'prompt-queue.json').read_text())
    task=next(t for t in tasks if t['name']==name)
    rgb=io.load_rgb(str(ROOT/task['raw']))
    k=key_source(rgb,False)
    f=face(rgb,k,False)
    soles=lm.bottom(k['alpha']>.5)
    s=(C['Feet']-C['Chin'])/(soles-f['chin'][1])
    T=rg.Similarity(s,C['CenterX']-s*f['centre_x'],C['Chin']-s*f['chin'][1])
    return f,T.as_dict()


def save(key, rgb, alpha, rec):
    if not (alpha > .5).any():
        raise RuntimeError('Empty output: ' + key)
    visible = alpha > 0
    bled, _ = io.bleed(np.where(visible[..., None], rgb, 0), visible, 6)
    rgb = np.where(visible[..., None], rgb, bled)
    out = STAGE / 'Sprites' / (key + '.png')
    io.save_rgba(str(out), rgb, alpha)
    ys, xs = np.nonzero(alpha > .5)
    key_px = ((rgb[..., 1]-np.maximum(rgb[..., 0], rgb[..., 2]) > 100) |
              ((np.minimum(rgb[..., 0], rgb[..., 2])-rgb[..., 1] > 100) &
               (np.minimum(rgb[..., 0], rgb[..., 2]) > .6*np.maximum(rgb[..., 0], rgb[..., 2])))) & (alpha > .1)
    rec['keys'][key] = {'bbox': [int(xs.min()), int(ys.min()), int(xs.max()+1), int(ys.max()+1)],
                        'keyColourPixels': int(key_px.sum()),
                        'sha256': hashlib.sha256(out.read_bytes()).hexdigest()}


def process(task, targets):
    rec = {'source': task['raw'], 'name': task['name'], 'algorithmVersion': 4, 'keys': {}}
    try:
        name = task['name']
        kind = name.split('_')[0]
        rgb = io.load_rgb(str(ROOT / task['raw']))
        mannequin = kind not in ('base', 'head', 'premade')
        k = key_source(rgb, mannequin=mannequin)
        override_path=COMPLETION/'processing-overrides.json'
        overrides=json.loads(override_path.read_text()) if override_path.exists() else {}
        override=overrides.get(name)
        expression_neutral=name.rsplit('_',1)[0]+'_neutral' if kind=='premade' and not name.endswith('_neutral') else None
        if expression_neutral:
            f,neutral_t=neutral_registration(expression_neutral)
            rec['expressionRegistrationReference']=expression_neutral
        else:
            f = override['face'] if override and 'face' in override else face(rgb, k, mannequin)
        if override:
            rec['landmarkReview']=override['reason']
        rec['measuredFace'] = f
        a = k['alpha'].copy()
        draw = task['prompt'].split('Draw:')[-1].split('Avoid:')[0].lower()
        eyewear = kind == 'accessory' and any(word in draw for word in ('glasses','spectacle','pince'))
        if eyewear:
            # A frame is legitimate face geometry, not a registration mark.
            # Some sources draw it in the same dark family as the fitting
            # figure. Recover the connected frame/lenses, excluding pupils,
            # brows and the calibration silhouette. Keep its actual colour,
            # removing only a purple calibration tint from the dark ink.
            yy,xx=np.mgrid[:C['Height'],:C['Width']]
            region=(abs(xx-f['centre_x'])<90)&(yy>290)&(yy<700)
            candidate=region & io.erode(~k['green'],6) & ((rgb.max(2)<140)|(a>.15))
            labels,n=io.label(candidate)
            keep=np.zeros(n+1,bool)
            for j in range(1,n+1):
                ys,xs=np.nonzero(labels==j)
                if len(xs) and xs.max()-xs.min()>55 and ys.min()<370 and ys.max()-ys.min()>7:
                    keep[j]=True
            a=keep[labels].astype(np.float32)
            if not a.any():
                raise RuntimeError('Eyewear frame could not be isolated')
            k['rgb']=rgb.copy()
            tinted=(np.minimum(rgb[:,:,0],rgb[:,:,2])-rgb[:,:,1]>8)&(a>0)
            ink=rgb.max(2)
            for channel,factor in enumerate((.60,.55,.50)):
                k['rgb'][:,:,channel]=np.where(tinted,ink*factor,rgb[:,:,channel])
            rec['eyewearIsolation']='connected frame/lenses; calibration silhouette and isolated face marks excluded'
        if mannequin and not eyewear:
            # Warm brown registration marks can survive the conservative
            # pilot keyer. Remove isolated marks strictly INSIDE the face;
            # retain earring components outside it and connected hair/hat.
            labels, n = io.label(a > .15)
            counts = np.bincount(labels.ravel())
            yy, xx = np.mgrid[:C['Height'], :C['Width']]
            mark_bottom = f['nose'][1]+5 if kind == 'facialhair' else f['chin'][1]-5
            interior = (abs(xx-f['centre_x']) < 60) & (yy > min(f['eye_l'][1], f['eye_r'][1])-45) & (yy < mark_bottom)
            ids = np.unique(labels[interior])
            interior_counts = np.bincount(labels[interior], minlength=n+1)
            drop = np.zeros(n+1, bool)
            drop[ids] = (counts[ids] < 1800) & (interior_counts[ids] == counts[ids])
            drop[0] = False
            rec['removedFaceMarkPixels'] = int((drop[labels] & (a>.15)).sum())
            a *= ~drop[labels]
        if kind == 'premade':
            soles = lm.bottom(a > .5)
            if expression_neutral:
                s=neutral_t['scale']
                T=rg.Similarity(s,neutral_t['tx'],C['Feet']-s*soles)
            else:
                s = (C['Feet']-C['Chin'])/(soles-f['chin'][1])
                T = rg.Similarity(s, C['CenterX']-s*f['centre_x'], C['Chin']-s*f['chin'][1])
        else:
            g = name.split('_')[1]
            target = targets[g]['face']
            if kind in ('base', 'head'):
                s = (target['chin'][1]-C['HeadTop'])/(f['chin'][1]-f['scalp'])
                T = rg.Similarity(s, C['CenterX']-s*f['centre_x'], C['HeadTop']-s*f['scalp'])
                # Split BEFORE the transform; retain only the requested head.
                a, _, _ = layers.split_head_body(k['rgb'], a, f['chin'][1], f['centre_x'])
            else:
                # Head/chest supports have calibration clothes, so their body
                # contours are unsuitable for registration. Eyes+nose locate
                # the skull independent of long hats, veils and support clothes.
                marks = ('eye_l', 'eye_r', 'nose')
                T = rg.fit_points([f[n] for n in marks], [target[n] for n in marks])
                if kind != 'outfit' and not eyewear and not .75 <= T.s <= 1.3:
                    raise RuntimeError(f'Head registration scale outside the full-canvas source range: {T.s:.3f}')
                if kind == 'outfit' or eyewear:
                    # The naked scalp and soles are measurable in outfits.
                    fig = ~k['green']
                    top = lm.top(fig)
                    soles = lm.bottom(fig)
                    s = (C['Feet']-C['HeadTop'])/(soles-top)
                    T = rg.Similarity(s, C['CenterX']-s*f['centre_x'], C['HeadTop']-s*top)
                else:
                    cutoff = C['Height'] if kind == 'accessory' else f['chin'][1] + {'hair': 360, 'facialhair': 150, 'headwear': 400}[kind]
                    if override and 'itemBottom' in override:
                        cutoff = override['itemBottom']
                        rec['sourceIsolationBottom'] = cutoff
                    # Drop unrequested calibration clothing below the read zone.
                    a[int(cutoff):] = 0
                    if kind in ('hair', 'facialhair', 'headwear'):
                        labels, n = io.label(a > .15)
                        keep = np.zeros(n+1, bool)
                        seed_bottom = int(f['chin'][1]+(105 if kind == 'facialhair' else 45))
                        ids = np.unique(labels[:seed_bottom])
                        keep[ids] = True
                        keep[0] = False
                        a *= keep[labels]
        rec['transform'] = T.as_dict()
        crgb, ca = warp.warp(k['rgb'], a, T, SIZE)
        if override and 'outputRegions' in override:
            region=np.zeros(ca.shape,bool)
            for x0,y0,x1,y1 in override['outputRegions']:
                region[y0:y1,x0:x1]=True
            ca *= region
            rec['reviewedOutputRegions']=override['outputRegions']
        if kind in ('base', 'head'):
            hue, sat, val = layers.hsv(crgb)
            skin = layers.dominant(crgb, (ca>.99) & (hue>5) & (hue<45) & (sat>.12) & (sat<.8) & (val>.2))
            tone = int(name.split('_')[2][4:])
            crgb = layers.recolour(crgb, layers.skin_weight(crgb, ca, skin), skin, contract.skin_swatches()[tone-1])
            rec['skinMeasured'] = skin
        if kind == 'premade':
            # Fit an unusually wide gown inside the declared safe zone without
            # shrinking the head vertically or moving the feet.
            ys, xs = np.nonzero(ca > .5)
            half = max(C['CenterX']-xs.min(), xs.max()+1-C['CenterX'])
            sx = min(1., 390/half)
            if sx < 1:
                channels = []
                for c in range(4):
                    channel = ca if c == 3 else crgb[..., c]*ca
                    im = Image.fromarray(channel.astype(np.float32), 'F')
                    channels.append(np.asarray(im.transform(SIZE, Image.Transform.AFFINE,
                        (1/sx, 0, C['CenterX']*(1-1/sx), 0, 1, 0), Image.Resampling.BICUBIC)))
                ca = np.clip(channels[3], 0, 1)
                crgb = np.stack(channels[:3], -1)/np.maximum(ca[..., None], 1e-4)
                crgb = np.clip(crgb, 0, 255)
                rec['safeWidthScale'] = sx
        keys = task['produces']
        if kind in ('hair', 'facialhair'):
            back_keys = [x for x in keys if x.startswith('hairback_')]
            front, back = ca, None
            if back_keys:
                fc = targets[name.split('_')[1]]['face']
                front, back = layers.split_hairback(crgb, ca, fc['chin'][1], C['CenterX'],
                    face_half_width=abs(fc['eye_r'][0]-fc['eye_l'][0]))
                # Flowing rear hair can end level with the front locks. When
                # that heuristic yields nothing, assign the outer hanging
                # portion below the jaw to rear, keeping a disjoint front.
                if not (back > .5).any():
                    yy, xx = np.mgrid[:C['Height'], :C['Width']]
                    rear = (yy > fc['chin'][1]+35) & (abs(xx-C['CenterX']) > 90)
                    back, front = ca*rear, ca*~rear
                    rec['rearSplit'] = 'outer-hanging-hair'
            needs_colours = any(key.split('_')[-1] in dict(contract.hair_colours()) for key in keys)
            if needs_colours:
                hue, sat, val = layers.hsv(crgb)
                source_colour = layers.dominant(crgb, (ca>.99) & (sat>.1) & (val>.07) & (val<.9))
            else:
                source_colour = np.array([60, 45, 35])
            rec['hairMeasured'] = source_colour
            colours = dict(contract.hair_colours())
            for key in keys:
                colour = key.split('_')[-1]
                baked = layers.bake_hair(crgb, ca, source_colour, colours[colour], keep_outline=colour!='black') if colour in colours else crgb
                save(key, baked, back if key.startswith('hairback_') else front, rec)
        else:
            for key in keys:
                save(key, crgb, ca, rec)
        rec['status'] = 'processed'
    except Exception as e:
        rec['status'] = 'failed'
        rec['error'] = str(e)
        rec['traceback'] = traceback.format_exc()
    write_json(STAGE / 'Records' / (task['name']+'.json'), rec)
    return rec['name'], rec['status'], rec.get('error', '')


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--only', nargs='*')
    ap.add_argument('--workers', type=int, default=3)
    ap.add_argument('--resume', action='store_true')
    ap.add_argument('--redo-kinds', nargs='*', default=[])
    args = ap.parse_args()
    (STAGE / 'Sprites').mkdir(parents=True, exist_ok=True)
    q = json.loads((COMPLETION / 'prompt-queue.json').read_text())
    if args.only:
        q = [t for t in q if t['name'] in args.only]
    if args.resume:
        remaining=[]
        for task in q:
            p=STAGE/'Records'/(task['name']+'.json')
            record=json.loads(p.read_text()) if p.exists() else {}
            kind=task['name'].split('_')[0]
            old_head_item=kind in ('hair','facialhair','headwear','accessory') and record.get('algorithmVersion')!=4
            if kind in args.redo_kinds or record.get('status')!='processed' or old_head_item:
                remaining.append(task)
        q=remaining
    targets = pilot_targets()
    write_json(STAGE / 'pilot-targets.json', targets)
    start = time.time()
    failed = []
    with ProcessPoolExecutor(max_workers=args.workers) as pool:
        jobs = [pool.submit(process, task, targets) for task in q]
        for i, job in enumerate(as_completed(jobs), 1):
            result = job.result()
            print(f'{i}/{len(q)} {time.time()-start:.1f}s {result}', flush=True)
            if result[1] != 'processed':
                failed.append(result)
    raise SystemExit(bool(failed))


if __name__ == '__main__':
    main()
