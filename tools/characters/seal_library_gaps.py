"""Seal enclosed mannequin gaps against the preserved pilot silhouette.

Uses the pilot's established gap-sealing rule, after registration, without
re-keying or changing any raw source. Only enclosed magenta outside the
actual base is filled; openings touching green remain open.
"""
from concurrent.futures import ProcessPoolExecutor, as_completed
import argparse
import json
import numpy as np
from PIL import Image
from charkit import imgops, layers, register, warp
from process_library import C, COMPLETION, ROOT, SIZE, STAGE, save, write_json


def seal(task):
    record_path=STAGE/'Records'/(task['name']+'.json')
    rec=json.loads(record_path.read_text())
    if rec['status']!='processed':
        return task['name'],'not processed'
    if 'sealedGapPixels' in rec:
        return task['name'],rec['sealedGapPixels']
    raw=np.asarray(Image.open(ROOT/task['raw']).convert('RGB'),dtype=np.float32)
    r,g,b=raw.transpose(2,0,1)
    green=(g-np.maximum(r,b))>=76.5
    mag=(np.minimum(r,b)-g>=25)&(np.minimum(r,b)>=.5*np.maximum(r,b))&~green
    t=rec['transform']
    T=register.Similarity(t['scale'],t['tx'],t['ty'])
    mw=warp.warp_mask(mag,T,SIZE)
    gw=warp.warp_mask(green,T,SIZE)
    gender=task['name'].split('_')[1]
    base=np.zeros((1536,1024),np.float32)
    for part in (f'body_{gender}_skin1',f'head_{gender}_skin1_facea'):
        a=np.asarray(Image.open(ROOT/f'Assets/Art/Characters/Resources/Characters/{part}.png'))[:,:,3]/255.
        base=np.maximum(base,a)
    key=task['produces'][0]
    rgba=np.asarray(Image.open(STAGE/'Sprites'/(key+'.png')),dtype=np.float32)
    rgb,alpha,filled=layers.seal_gaps(rgba[:,:,:3],rgba[:,:,3]/255.,mw,gw,base)
    if filled:
        save(key,rgb,alpha,rec)
    rec['sealedGapPixels']=filled
    write_json(record_path,rec)
    return task['name'],filled


def isolate_support(task):
    record_path=STAGE/'Records'/(task['name']+'.json')
    rec=json.loads(record_path.read_text())
    if rec['status']!='processed':
        return task['name'],'not processed'
    kind=task['name'].split('_')[0]
    if not rec.get('faceMarkCleanupVersion') and not rec.get('eyewearIsolation'):
        # Registration brows can sit above the conservative keyer's original
        # read zone. Remove only small components wholly inside the face.
        f,t=rec['measuredFace'],rec['transform']
        s,tx,ty=t['scale'],t['tx'],t['ty']
        first_key=next(k for k in task['produces'] if not k.startswith('hairback_'))
        first=np.asarray(Image.open(STAGE/'Sprites'/(first_key+'.png')))
        labels,n=imgops.label(first[:,:,3]>38)
        counts=np.bincount(labels.ravel(),minlength=n+1)
        yy,xx=np.mgrid[:1536,:1024]
        bottom=(f['nose'][1]+5 if kind=='facialhair' else f['chin'][1]-5)*s+ty
        interior=(abs(xx-(f['centre_x']*s+tx))<60*s)&(yy>(min(f['eye_l'][1],f['eye_r'][1])-45)*s+ty)&(yy<bottom)
        inner_counts=np.bincount(labels[interior],minlength=n+1)
        drop=(counts<1800*s*s)&(inner_counts==counts)
        drop[0]=False
        removal=imgops.dilate(drop[labels],2)
        rec['postRegistrationFaceMarkPixels']=int((drop[labels]&(first[:,:,3]>38)).sum())
        if removal.any():
            for key in task['produces']:
                rgba=np.asarray(Image.open(STAGE/'Sprites'/(key+'.png')),dtype=np.float32)
                a=rgba[:,:,3]/255.;a[removal]=0
                save(key,rgba[:,:,:3],a,rec)
        rec['faceMarkCleanupVersion']=1
        write_json(record_path,rec)
    if kind=='accessory':
        return task['name'],rec.get('postRegistrationFaceMarkPixels',0)
    if 'supportComponentsRemoved' in rec:
        return task['name'],rec['supportComponentsRemoved']
    chin=rec['measuredFace']['chin'][1]*rec['transform']['scale']+rec['transform']['ty']
    seed_bottom=int(chin+(25 if kind=='facialhair' else -10))
    front_keys=[k for k in task['produces'] if not k.startswith('hairback_')]
    first=np.asarray(Image.open(STAGE/'Sprites'/(front_keys[0]+'.png')))
    labels,n=imgops.label(first[:,:,3]>38)
    keep=np.zeros(n+1,bool)
    keep[np.unique(labels[:seed_bottom])]=True
    keep[0]=False
    remove=(labels>0)&~keep[labels]
    rec['supportComponentsRemoved']=int(remove.sum())
    if remove.any():
        # Reuse this geometry mask for every colour; rear hair is deliberate
        # disconnected geometry and is not touched by this front-item filter.
        removal=imgops.dilate(remove,2)
        for key in front_keys:
            rgba=np.asarray(Image.open(STAGE/'Sprites'/(key+'.png')),dtype=np.float32)
            a=rgba[:,:,3]/255.
            a[removal]=0
            save(key,rgba[:,:,:3],a,rec)
    write_json(record_path,rec)
    return task['name'],rec['supportComponentsRemoved']


if __name__=='__main__':
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--limit',type=int)
    args=ap.parse_args()
    q=json.loads((COMPLETION/'prompt-queue.json').read_text())
    if args.limit:
        q=q[:args.limit]
    q=[t for t in q if t['produces'] and t['name'].split('_')[0] in ('outfit','hair','facialhair','headwear','accessory')]
    with ProcessPoolExecutor(max_workers=4) as pool:
        jobs=[pool.submit(seal if t['name'].startswith('outfit_') else isolate_support,t) for t in q]
        for job in as_completed(jobs):
            print(job.result(),flush=True)
