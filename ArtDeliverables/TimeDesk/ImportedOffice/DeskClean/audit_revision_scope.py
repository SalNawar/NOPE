"""Read-only scene comparison with the immediately preceding art checkpoint."""
import re,json,subprocess
from pathlib import Path
SCENE='Assets/Scenes/OfficeScene.unity'
def docs(text):
    parts=re.split(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n',text,flags=re.M)
    return {parts[i+1]:(int(parts[i]),parts[i+2]) for i in range(1,len(parts),3)}
before=docs(subprocess.check_output(['git','show','a1ec7d2:'+SCENE],text=True))
after=docs(Path(SCENE).read_text())
def name(id,all,seen=None):
    if id=='0':return ''
    if id not in all:return '?'+id
    seen=set() if seen is None else set(seen)
    if id in seen:return '?cycle'
    seen.add(id);kind,body=all[id]
    def ref(key):
        m=re.search(r'\b'+key+r': \{fileID: (-?\d+)',body);return m[1] if m else '0'
    if kind==1001:
        labels=re.findall(r'propertyPath: m_Name\n\s+value: (.*)',body)
        parent=name(ref('m_TransformParent'),all,seen)
        return parent+'/'+(labels[-1] if labels else 'Prefab')
    if kind==1:
        m=re.search(r'^  m_Name: (.*)',body,re.M);label=m[1] if m else 'GameObject'
        transform=next((v for v in re.findall(r'component: \{fileID: (-?\d+)',body) if v in all and all[v][0] in (4,224)),None)
        if not transform:return label
        father=re.search(r'm_Father: \{fileID: (-?\d+)',all[transform][1]);parent=name(father[1],all,seen) if father else ''
        return (parent+'/' if parent else '')+label
    go=ref('m_GameObject')
    if go!='0':return name(go,all,seen)
    prefab=ref('m_PrefabInstance')
    if prefab!='0':return name(prefab,all,seen)
    return 'Scene settings '+str(kind)
changes=[]
for id in before.keys()|after.keys():
    if before.get(id)==after.get(id):continue
    source=after if id in after else before
    changes.append(dict(id=id,kind=source[id][0],change='added' if id not in before else 'removed' if id not in after else 'modified',path=name(id,source)))
allowed=['ImportedOfficeDress/Desk/Clerk hotline','ImportedOfficeDress/Desk/Spare forms','HybridOffice/Booth/Finish_Mouse','HybridOffice/Booth/Finish_ComputerMedia']
# Preserve the live scene's additional 4 mm keyboard art offset; its root placement
# and all non-desk objects are unchanged. This revision does not author that offset.
keyboard=[c for c in changes if c['path']=='ImportedOfficeDress/Desk/Retro keyboard/Clean Art']
for c in keyboard:
    old=before[c['id']][1];new=after[c['id']][1]
    expected=old.replace('propertyPath: m_LocalPosition.z\n      value: 0\n','propertyPath: m_LocalPosition.z\n      value: -0.004\n')
    assert new==expected, 'Unexpected additional keyboard change'
    c['note']='Retained live 4 mm keyboard art offset; not authored by the revision installer.'
    allowed.append(c['path'])
unexpected=[c for c in changes if not any(c['path'].startswith(p) for p in allowed) and not(c['path']=='HybridOffice/Booth' and c['kind']==4 and c['change']=='modified')]
report={'baseline':'a1ec7d2','scene':SCENE,'changes':sorted(changes,key=lambda c:(c['path'],c['id'])),'unexpected':unexpected,'note':'Booth transform changes only remove the floppy child reference. Material assets are audited separately.'}
Path(__file__).with_name('anime_scene_scope.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'documentsChanged':len(changes),'unexpected':unexpected},indent=2))
assert not unexpected
