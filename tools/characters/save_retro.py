import argparse,json,hashlib,struct,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtDeliverables/TimeDesk/Characters/RetroRegeneration'
p=argparse.ArgumentParser();p.add_argument('job');a=p.parse_args();j=json.loads(Path(a.job).read_text(encoding='utf-8-sig'))
src=Path(j['source']);data=src.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n';size=list(struct.unpack('>II',data[16:24]));assert size==[1024,1536],size
rel=j.get('folder','Raw')+'/'+j['name']+'.png';dest=OUT/rel;dest.parent.mkdir(parents=True,exist_ok=True)
assert not dest.exists(),f'Refusing overwrite: {dest}'
shutil.copy2(src,dest);dest.with_suffix('.prompt.md').write_text(j['prompt'],encoding='utf-8')
mp=OUT/'manifest.json';m=json.loads(mp.read_text(encoding='utf-8')) if mp.exists() else {'scope':'All regular traveller source layers and current 33 uniques, textured regeneration','complete':False,'items':[]}
m['items'].append({**j,'path':rel,'size':size,'sha256':hashlib.sha256(data).hexdigest(),'tool':'built-in image_gen','review':'visually reviewed; registration pending','integration':'raw source only'})
mp.write_text(json.dumps(m,indent=2,ensure_ascii=False),encoding='utf-8')
rows=['# Generated assets','',f"Saved: {len(m['items'])}. Full regeneration is NOT complete.",'','| Asset | Status |','|---|---|']
rows += [f"| {i['name']} | {i['review']} |" for i in m['items']]
(OUT/'CHECKLIST.md').write_text('\n'.join(rows)+'\n',encoding='utf-8')
print(f"Saved {j['name']}; total {len(m['items'])}")
