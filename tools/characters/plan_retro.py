import json,re,subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[2];O=R/'ArtDeliverables/TimeDesk/Characters/RetroRegeneration';P=R/'ArtDeliverables/TimeDesk/Characters/Production'
world=json.loads((R/'Assets/Data/World/world_source.json').read_text(encoding='utf-8'))
coverage=json.loads((P/'coverage.json').read_text(encoding='utf-8'))
old=json.loads((R/'ArtDeliverables/TimeDesk/Characters/Completion/prompt-queue.json').read_text(encoding='utf-8'))
old={i['name']:i for i in old}
poses={'neutral':'arms relaxed slightly away from body, empty hands down','explaining_a':'RIGHT elbow open, right palm up at lower chest, left arm relaxed','explaining_b':'BOTH elbows bent, both palms open low with asymmetrical heights, modest outward explanation','explaining_c':'LEFT hand opens close to upper chest with compact measured gesture, right arm relaxed','thinking_a':'RIGHT fingertips under chin, elbow bent, left hand down; hand must render over lower beard','thinking_b':'LEFT fingertips lightly at cheek, right forearm supports left elbow; face stays fully visible','thinking_c':'hands loosely linked at waist, fingers relaxed and empty, reflective reserved stance','objecting_a':'RIGHT open palm forward near shoulder, fingers up, calm please-wait gesture','objecting_b':'BOTH palms open at lower chest facing outward, questioning refusal, elbows close to torso','objecting_c':'RIGHT hand rests on upper chest, LEFT palm extends low to side, restrained personal objection'}
queue=[]
def add(name,family,**kw):
 if not any(i['name']==name for i in queue):queue.append(dict(name=name,family=family,status='pending',**kw))
for g in 'mf':
 for skin in range(1,6):
  for face in 'abcd':add(('base' if face=='a' else 'head')+f'_{g}_skin{skin}_face{face}','base',gender=g,skin=skin,face=face,pose='neutral')
 for pose,desc in poses.items():
  if pose!='neutral':add(f'base_{g}_skin1_facea__{pose}','body_pose',gender=g,skin=1,face='a',pose=pose,gesture=desc)
  add(f'mannequin_{g}_{pose}','mannequin',gender=g,pose=pose,gesture=desc)
# Regenerate all wardrobe entries from current world, not obsolete premade coverage.
for pi,place in enumerate(world['places']):
 for gi,g in enumerate('mf'):
  wardrobe=place.get('wardrobe',{}).get(g,{})
  for layer,item in wardrobe.items():
   if not isinstance(item,dict) or not item.get('label'):continue
   token='facialhair' if layer=='facialHair' else layer.lower()
   if token not in ['outfit','hair','facialhair','headwear','accessory']:continue
   nation=item.get('artNation',place['country']);era=place['era'];name=f'{token}_{g}_{nation}_{era}'
   details=old.get(name,{}).get('prompt','')
   if 'Subject:' in details:details=details[details.index('Subject:'):]
   familyposes=['neutral']
   # Each costume gets one distinct gesture in each category. The library has three variants per category across the cast.
   if token=='outfit':
    v='abc'[(pi+gi)%3];familyposes += [f'explaining_{v}',f'thinking_{"abc"[(pi+gi+1)%3]}',f'objecting_{"abc"[(pi+gi+2)%3]}']
   for pose in familyposes:
    add(name if pose=='neutral' else name+'__'+pose,'wardrobe',gender=g,layer=token,place=place['displayName'],moment=place['moment'],country=nation,era=era,label=item['label'],details=details,flags=item,pose=pose,gesture=poses[pose],base_key=name)
# Current 33 unique roster from the finished previous branch, never its old-style PNGs.
roster=json.loads(subprocess.check_output(['git','show','codex/famous-travellers-33:ArtDeliverables/TimeDesk/Characters/Famous33/roster.json'],cwd=R,text=True,encoding='utf-8'))
(O/'unique-roster.json').write_text(json.dumps(roster,indent=2,ensure_ascii=False),encoding='utf-8')
# Keep roster structure for generation driver.
for i,item in enumerate(roster):
 ident=item['id'];v='abc'[i%3]
 for exp in ['neutral','happy','angry','worried']:add(f'premade_{ident}_{exp}','unique',id=ident,expression=exp,pose='neutral',roster=item)
 for cat in ['explaining','thinking','objecting']:
  pose=cat+'_'+v;add(f'premade_{ident}__{pose}','unique_pose',id=ident,expression='neutral',pose=pose,gesture=poses[pose],roster=item)
(O/'pose-families.json').write_text(json.dumps(poses,indent=2),encoding='utf-8')
(O/'queue.json').write_text(json.dumps(queue,indent=2,ensure_ascii=False),encoding='utf-8')
from collections import Counter
print('Planned raw deliverables:',len(queue));print(dict(Counter(i['family'] for i in queue)))
