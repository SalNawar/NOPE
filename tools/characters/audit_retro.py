import json,hashlib,struct
from pathlib import Path
O=Path(__file__).resolve().parents[2]/'ArtDeliverables/TimeDesk/Characters/RetroRegeneration'
m=json.loads((O/'manifest.json').read_text(encoding='utf-8'))
seen=set()
for i in m['items']:
 assert i['name'] not in seen,i['name']
 seen.add(i['name']);p=O/i['path'];b=p.read_bytes()
 assert b[:8]==b'\x89PNG\r\n\x1a\n',p
 assert struct.unpack('>II',b[16:24])==(1024,1536),p
 assert hashlib.sha256(b).hexdigest()==i['sha256'],p
 assert p.with_suffix('.prompt.md').is_file(),p
print(f"Verified {len(seen)} unique PNG sources: dimensions, hashes, prompts. Alignment and Unity integration NOT verified.")
