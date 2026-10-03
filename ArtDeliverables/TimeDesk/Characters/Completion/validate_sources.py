"""Read-only verification. Run with Python 3; no external packages needed."""
import hashlib
import json
import struct
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
manifest = json.loads((HERE / 'generation-manifest.json').read_text(encoding='utf-8'))
queue = json.loads((HERE / 'prompt-queue.json').read_text(encoding='utf-8'))
records = {record['name']: record for record in manifest['generated']}
errors = []
for task in queue:
    record = records.get(task['name'])
    if record is None:
        errors.append('Missing generated source: ' + task['name'])
        continue
    path = ROOT / record['raw']
    if not path.exists():
        errors.append('Missing file: ' + record['raw'])
        continue
    data = path.read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n' or struct.unpack('>II', data[16:24]) != (1024, 1536):
        errors.append('Wrong production canvas: ' + record['raw'])
    if hashlib.sha256(data).hexdigest() != record['sha256']:
        errors.append('Hash mismatch: ' + record['raw'])
    if not path.with_suffix('.prompt.md').exists():
        errors.append('Missing prompt: ' + record['raw'])
baseline_path = HERE / 'preserved-pilot.json'
if not baseline_path.exists():
    errors.append('Missing preserved pilot baseline')
else:
    for relative, expected in json.loads(baseline_path.read_text(encoding='utf-8')).items():
        path = ROOT / relative
        if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
            errors.append('Preserved pilot changed: ' + relative)
report = {'requiredNewSources': len(queue), 'presentNewSources': len(records),
          'plannedNewLayerKeys': len({key for task in queue for key in task['produces']}),
          'gameReadyLayerProcessingComplete': manifest['gameReadyLayerProcessingComplete'],
          'errors': errors}
print(json.dumps(report, indent=2))
raise SystemExit(bool(errors))
