import json,re
from pathlib import Path
R=Path(__file__).resolve().parents[2]
O=R/'ArtDeliverables/TimeDesk/Characters/RetroRegeneration'
brief=(R/'ArtDeliverables/TimeDesk/Characters/Production/CHARACTER_ART_BRIEF_v2.md').read_text(encoding='utf-8')
q=json.loads((O/'queue.json').read_text(encoding='utf-8'))
labels={'outfit':'Outfit','hair':'Hair','facialhair':'Facial hair','headwear':'Headwear','accessory':'Accessory'}
fixed=[];missing=[]
for j in q:
 if j['family']!='wardrobe' or j.get('details'): continue
 key=j['country']+'_'+j['era']
 match=re.search(r'PAIR: '+re.escape(key)+r'[^\n]*\n(.*?)\x60\x60\x60',brief,re.S)
 if not match: missing.append(j['name']);continue
 block=match.group(1); gender='MAN' if j['gender']=='m' else 'WOMAN'
 section=re.search(r'^'+gender+r'\s*\n(.*?)(?=^MAN\s*$|^WOMAN\s*$|^MUST READ|^LEAK ITEMS|^DO NOT DRAW|\Z)',block,re.S|re.M)
 item=re.search(r'^- '+re.escape(labels[j['layer']])+r':\s*(.+)',section.group(1),re.M) if section else None
 if not item:missing.append(j['name']);continue
 avoid=re.search(r'^DO NOT DRAW:\s*(.+)',block,re.M)
 j['details']='Draw: '+item.group(1)+ ('\nAvoid: '+avoid.group(1) if avoid else '')
 fixed.append(j['name'])
(O/'queue.json').write_text(json.dumps(q,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps({'filled':len(fixed),'still_missing':missing}))
