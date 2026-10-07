import json,argparse
from pathlib import Path
R=Path(__file__).resolve().parents[2];O=R/'ArtDeliverables/TimeDesk/Characters/RetroRegeneration'
p=argparse.ArgumentParser();p.add_argument('--family');p.add_argument('--limit',type=int,default=2);a=p.parse_args()
q=json.loads((O/'queue.json').read_text(encoding='utf-8'));m=json.loads((O/'manifest.json').read_text(encoding='utf-8'));done={i['name'] for i in m['items']}
q=[i for i in q if i['name'] not in done and (not a.family or i['family']==a.family)]
print(json.dumps(q[:a.limit],ensure_ascii=True))
