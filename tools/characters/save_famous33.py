"""Save generated originals and audit their delivery; never edit image pixels."""
import sys,json,shutil,hashlib,struct
from pathlib import Path
root=Path('ArtDeliverables/TimeDesk/Characters/Famous33')
records=json.load(sys.stdin)
manifest=root/'manifest.json'
data=json.loads(manifest.read_text(encoding='utf-8')) if manifest.exists() else []
for record in records:
 src=Path(record['source']); dest=root/'Raw'/record['id']/('premade_'+record['id']+'_'+record['expression']+'.png')
 dest.parent.mkdir(parents=True,exist_ok=True)
 raw=src.read_bytes()
 assert raw[:8]==b'\x89PNG\r\n\x1a\n',str(src)
 width,height=struct.unpack('>II',raw[16:24])
 assert (width,height)==(1024,1536),(str(src),width,height)
 shutil.copy2(src,dest)
 dest.with_suffix('.prompt.md').write_text(record['prompt'],encoding='utf-8')
 record.update(path=dest.as_posix(),width=width,height=height,sha256=hashlib.sha256(raw).hexdigest(),tool='built-in image_gen',visual_review=record.get('visual_review','pending'))
 data=[d for d in data if (d['id'],d['expression'])!=(record['id'],record['expression'])]+[record]
manifest.write_text(json.dumps(data,indent=2,ensure_ascii=False),encoding='utf-8')
roster=json.loads((root/'roster.json').read_text(encoding='utf-8'))
lines=['# Famous travellers — 33 characters, 231 images (expressions + poses)','','Fresh main-based branch codex/famous-travellers-33. Sources: Production/FAMOUS_PREMADES_REQUEST.md with its three amendments. Original generated green-background PNGs are preserved; game-ready keying/registration is a separate integration step.','','| Character | Neutral | Happy | Angry | Worried | Explaining | Thinking | Objecting |','|---|---|---|---|---|---|---|---|']
for person in roster:
 row=[person['name']]
 for expression in ['neutral','happy','angry','worried','pose_explaining','pose_thinking','pose_objecting']:
  found=next((x for x in data if x['id']==person['id'] and x['expression']==expression),None)
  row.append(found['visual_review'] if found else 'pending')
 lines.append('| '+' | '.join(row)+' |')
(root/'CHECKLIST.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(f'Saved {len(data)}/231 originals; all 1024x1536. Complete sets: '+str(sum(all(any(x['id']==p['id'] and x['expression']==e for x in data) for e in ['neutral','happy','angry','worried','pose_explaining','pose_thinking','pose_objecting']) for p in roster)))
