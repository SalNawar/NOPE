"""Read-only comparison: the crowd installation must only add its own scene root."""
import json
import re
import subprocess
from pathlib import Path

SCENE = 'Assets/Scenes/OfficeScene.unity'
BASELINE = 'd99d585'

def documents(text):
    parts = re.split(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n', text, flags=re.M)
    return {parts[i+1]: (int(parts[i]), parts[i+2]) for i in range(1, len(parts), 3)}

before = documents(subprocess.check_output(['git', 'show', BASELINE+':'+SCENE], text=True))
after = documents(Path(SCENE).read_text())

def path(identifier, seen=None):
    seen = set() if seen is None else set(seen)
    if identifier == '0':
        return ''
    if identifier not in after or identifier in seen:
        return '?'+identifier
    seen.add(identifier)
    kind, body = after[identifier]
    def reference(key):
        match = re.search(r'\b'+key+r': \{fileID: (-?\d+)', body)
        return match[1] if match else '0'
    if kind == 1:
        label = re.search(r'^  m_Name: (.*)', body, re.M)[1]
        transform = next(v for v in re.findall(r'component: \{fileID: (-?\d+)', body) if v in after and after[v][0] == 4)
        father = re.search(r'm_Father: \{fileID: (-?\d+)', after[transform][1])[1]
        parent = path(father, seen)
        return (parent+'/' if parent else '')+label
    if reference('m_GameObject') != '0':
        return path(reference('m_GameObject'), seen)
    return 'SceneRoots' if kind == 1660057539 else 'Scene settings '+str(kind)

added = [{'id': i, 'kind': after[i][0], 'path': path(i)} for i in after.keys()-before.keys()]
removed = sorted(before.keys()-after.keys())
modified = [i for i in before.keys() & after.keys() if before[i] != after[i]]
unexpected = [a for a in added if not a['path'].startswith('OfficeHallCrowds')]
root = next(i for i, (kind, body) in after.items() if kind == 4 and path(i) == 'OfficeHallCrowds')
for identifier in modified:
    kind, new = after[identifier]
    old = before[identifier][1]
    if kind != 1660057539 or new.replace('  - {fileID: '+root+'}\n', '') != old:
        unexpected.append({'id': identifier, 'change': 'unexpected existing document modification'})
report = {'baseline': BASELINE, 'scene': SCENE, 'addedDocuments': len(added),
          'modifiedExistingDocuments': modified, 'removedDocuments': removed,
          'unexpected': unexpected, 'newHierarchy': 'OfficeHallCrowds',
          'success': not removed and not unexpected}
Path(__file__).with_name('scene_scope_audit.json').write_text(json.dumps(report, indent=2))
print(json.dumps(report, indent=2))
assert report['success'], 'Scene changes outside the crowd hierarchy.'
