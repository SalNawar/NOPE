"""Independent inventory checks and alpha-composited previews of staged art."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

import numpy as np
from PIL import Image, ImageDraw
from charkit import contract, unitymeta
from process_library import COMPLETION, ROOT, STAGE, write_json

INSTALLED = ROOT / contract.resources_folder()
ORDER = ['hairback', 'body', 'outfit', 'head', 'facialhair', 'hair', 'headwear', 'accessory']


def sprite(key):
    staged = STAGE/'Sprites'/(key+'.png')
    path = staged if staged.exists() else INSTALLED/(key+'.png')
    return Image.open(path).convert('RGBA') if path.exists() else None


def composite(keys):
    im = Image.new('RGBA', (1024, 1536), (111, 112, 113, 255))
    for key in keys:
        layer = sprite(key)
        if layer:
            im.alpha_composite(layer)
    return im.convert('RGB')


def source_stack(task):
    name = task['name']
    if name.startswith('premade_'):
        return [name]
    if name.startswith(('base_', 'head_')):
        key = task['produces'][0]
        p = key.split('_')
        return [f'body_{p[1]}_{p[2]}', key]
    p = name.split('_')
    g, nation, era = p[1:4]
    keys = [f'body_{g}_skin1', f'outfit_{g}_{nation}_{era}', f'head_{g}_skin1_facea']
    if nation == 'neutral':
        keys[1] = f'outfit_{g}_britain_future'
    if p[0] == 'hair':
        keys += [k for k in task['produces'] if k.endswith('_brown') or not k.endswith(('_black','_blond','_red','_grey'))]
    else:
        # Display this piece with the hairstyle normally used in its place.
        hairnation = 'neutral' if era == 'future' else nation
        keys.append(f'hair_{g}_{hairnation}_{era}_brown')
        if p[0] == 'headwear' and nation != 'neutral':
            wardrobe = contract.wardrobe(nation,era)[g]
            if 'Hair' in ((wardrobe.get('headwear') or {}).get('covers') or []):
                keys = [k for k in keys if not k.startswith(('hair_', 'hairback_'))]
        keys += [k for k in task['produces'] if k.endswith('_brown') or p[0] not in ('facialhair',)]
    return sorted(set(keys), key=lambda x: ORDER.index(x.split('_')[0]))


def previews(q):
    out = COMPLETION/'ProcessedQA'
    out.mkdir(exist_ok=True)
    if len(q) == 455:
        expected = {f'stacks-{offset+1:03d}-{min(offset+20,len(q)):03d}.png' for offset in range(0,len(q),20)}
        for p in out.glob('stacks-*.png'):
            if p.name not in expected:
                p.unlink()  # obsolete previews created by this tool's sample pass
    for offset in range(0, len(q), 20):
        tasks = q[offset:offset+20]
        sheet = Image.new('RGB', (1500, 1400), (238, 235, 227))
        draw = ImageDraw.Draw(sheet)
        for j, t in enumerate(tasks):
            x, y = j%5*300, j//5*350
            im = composite(source_stack(t))
            sheet.paste(im.resize((200, 300), Image.Resampling.LANCZOS), (x, y))
            crop = im.crop((350, 200, 674, 524)).resize((95,95), Image.Resampling.LANCZOS)
            sheet.paste(crop, (x+202,y+2))
            draw.text((x+3,y+307), t['name'], fill='black')
        sheet.save(out/f'stacks-{offset+1:03d}-{offset+len(tasks):03d}.png')


def place_previews():
    """Complete native wardrobe combinations, with the game's covers flags."""
    world=json.loads((ROOT/contract.WORLD_SOURCE).read_text(encoding='utf-8-sig'))
    entries=[]
    for place in world['places']:
        nation,era=place['country'],place['era']
        for g in ('m','f'):
            outfit=f'outfit_{g}_{nation}_{era}'
            if sprite(outfit) is None:
                continue
            index=len(entries)
            skin=index%5+1
            colour=['black','brown','blond','red','grey'][index%5]
            keys=[f'body_{g}_skin{skin}',outfit,f'head_{g}_skin{skin}_facea']
            wardrobe=place['wardrobe'][g]
            hat=wardrobe.get('headwear') or {}
            covers=hat.get('covers') or []
            for layer,slot in [('hairback','hair'),('hair','hair'),('facialhair','facialHair'),('headwear','headwear'),('accessory','accessory')]:
                if layer in ('hair','hairback') and 'Hair' in covers:
                    continue
                item=wardrobe.get(slot) or {}
                art_nation=item.get('artNation') or ('neutral' if era=='future' and layer in ('hair','hairback','facialhair') else nation)
                base=f'{layer}_{g}_{art_nation}_{era}'
                key=base if sprite(base) is not None else base+'_'+colour
                if sprite(key) is not None:
                    keys.append(key)
            keys=sorted(keys,key=lambda k:ORDER.index(k.split('_')[0]))
            entries.append((f'{g}_{nation}_{era}',keys))
    out=COMPLETION/'ProcessedQA'
    for offset in range(0,len(entries),10):
        sheet=Image.new('RGB',(1500,1300),(238,235,227))
        draw=ImageDraw.Draw(sheet)
        for j,(name,keys) in enumerate(entries[offset:offset+10]):
            x,y=j%5*300,j//5*650
            im=composite(keys)
            sheet.paste(im.resize((256,384),Image.Resampling.LANCZOS),(x,y))
            # Fixed contract passport crop and desk-size upper-body crop.
            sheet.paste(im.crop((362,215,662,590)).resize((96,120),Image.Resampling.LANCZOS),(x,y+390))
            desk=im.crop((280,190,744,760)).resize((164,201),Image.Resampling.LANCZOS)
            sheet.paste(desk,(x+130,y+400))
            draw.text((x+3,y+630),name,fill='black')
        sheet.save(out/f'wardrobes-{offset+1:03d}-{min(offset+10,len(entries)):03d}.png')
    print(f'Wrote {len(entries)} full wardrobe/desk/passport previews.',flush=True)


def validate(q):
    required = set(contract.required_keys()) | {k for t in q for k in t['produces'] if k.startswith('premade_')}
    reports = []
    errors = []
    for t in q:
        p = STAGE/'Records'/(t['name']+'.json')
        r = json.loads(p.read_text()) if p.exists() else {'status': 'missing', 'name': t['name']}
        reports.append(r)
        if r['status'] != 'processed':
            errors.append(f"{t['name']}: {r.get('error',r['status'])}")
        elif set(r['keys']) != set(t['produces']):
            errors.append(t['name']+': record does not supply every planned output')
        elif t['name'].split('_')[0] in ('hair','facialhair','headwear','accessory') and not .75<=r['transform']['scale']<=1.3:
            errors.append(t['name']+': implausible head registration scale')
        for key, decision in r.get('keys',{}).items():
            output=STAGE/'Sprites'/(key+'.png')
            if not output.exists() or hashlib.sha256(output.read_bytes()).hexdigest()!=decision['sha256']:
                errors.append(key+': staged output differs from its processing record')
    present = {p.stem for p in INSTALLED.glob('*.png')} | {p.stem for p in (STAGE/'Sprites').glob('*.png')}
    if required != present:
        errors += ['Missing: '+repr(sorted(required-present)), 'Unexpected: '+repr(sorted(present-required))]
    files = {}
    for key in sorted(required):
        path = STAGE/'Sprites'/(key+'.png')
        if not path.exists():
            path = INSTALLED/(key+'.png')
        if not path.exists():
            continue
        with Image.open(path) as im:
            if im.mode != 'RGBA' or im.size != (1024,1536):
                errors.append(f'{key}: wrong canvas/mode {im.size}/{im.mode}')
            rgba = np.asarray(im.convert('RGBA'), dtype=np.int16)
        rgb, a = rgba[:,:,:3], rgba[:,:,3]
        if a.max() == 0 or a.min() != 0:
            errors.append(key+': blank or opaque background')
        if max(a[:2].max(), a[-2:].max(), a[:,:2].max(), a[:,-2:].max()) > 0:
            errors.append(key+': alpha reaches canvas edge')
        gd = rgb[:,:,1] - np.maximum(rgb[:,:,0], rgb[:,:,2])
        ms = np.minimum(rgb[:,:,0], rgb[:,:,2])-rgb[:,:,1]
        mag = (ms>100) & (np.minimum(rgb[:,:,0],rgb[:,:,2])>.6*np.maximum(rgb[:,:,0],rgb[:,:,2]))
        contamination = int((((gd>100)|mag)&(a>25)).sum())
        if contamination:
            errors.append(f'{key}: {contamination} key-colour pixels')
        files[key] = {'sha256':hashlib.sha256(path.read_bytes()).hexdigest(), 'keyColourPixels':contamination}
    report = {'expectedKeys':len(required),'presentKeys':len(present),'sourceCount':len(reports),
              'processedSourceCount':sum(r['status']=='processed' for r in reports), 'errors':errors,'files':files}
    write_json(COMPLETION/'processed-validation.json', report)
    write_json(COMPLETION/'processing-report.json', {'sources':reports})
    print(json.dumps({k:v for k,v in report.items() if k!='files'},indent=2),flush=True)
    return report


def install(q, report):
    if report['errors']:
        raise RuntimeError('Cannot install library with failed validation')
    archive = COMPLETION/'InterimPilotHeads'
    replacement_file=COMPLETION/'interim-head-replacements.json'
    replacements=json.loads(replacement_file.read_text()) if replacement_file.exists() else {}
    for t in q:
        for key in t['produces']:
            source = STAGE/'Sprites'/(key+'.png')
            dest = INSTALLED/(key+'.png')
            unchanged = dest.exists() and hashlib.sha256(dest.read_bytes()).hexdigest() == hashlib.sha256(source.read_bytes()).hexdigest()
            if dest.exists() and not unchanged:
                if not key.startswith(('head_m_skin','head_f_skin')) or not key.endswith('_facea') or '_skin1_' in key:
                    raise RuntimeError('Refusing to overwrite approved pilot: '+key)
                archive.mkdir(exist_ok=True)
                backup = archive/dest.name
                if not backup.exists():
                    shutil.copy2(dest, backup)
                replacements[str(dest.relative_to(ROOT)).replace('\\','/')] = {
                    'originalSha256':hashlib.sha256(backup.read_bytes()).hexdigest(),
                    'replacementSha256':hashlib.sha256(source.read_bytes()).hexdigest(),
                    'backup':str(backup.relative_to(ROOT)).replace('\\','/'),
                    'reason':'Replace the pilot interim recolour with its newly generated skin-specific face a; retain GUID.'}
            shutil.copy2(source,dest)
            unitymeta.texture_meta(str(dest),1536,1490)
    write_json(COMPLETION/'interim-head-replacements.json', replacements)
    errors=[]
    for key,expected in report['files'].items():
        if hashlib.sha256((INSTALLED/(key+'.png')).read_bytes()).hexdigest()!=expected['sha256']:
            errors.append(key+': installed output hash differs from checked staging')
    write_json(COMPLETION/'installed-validation.json', {
        'expectedKeys':report['expectedKeys'], 'installedKeys':len(list(INSTALLED.glob('*.png'))),
        'matchedOutputHashes':len(report['files'])-len(errors), 'errors':errors})
    if errors:
        raise RuntimeError('Installed hashes did not match checked staging')
    print(f'Installed {len(report["files"])} required sprites, preserving pilot bodies/wardrobe and original GUIDs.',flush=True)


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--previews',action='store_true')
    ap.add_argument('--install',action='store_true')
    args=ap.parse_args()
    q=json.loads((COMPLETION/'prompt-queue.json').read_text())
    report=validate(q)
    if args.previews:
        previews(q)
        place_previews()
    if args.install:
        install(q,report)
    raise SystemExit(bool(report['errors']))


if __name__=='__main__':
    main()
