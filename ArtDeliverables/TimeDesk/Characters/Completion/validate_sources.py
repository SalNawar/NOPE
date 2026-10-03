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
coverage = json.loads((HERE.parent / 'Production/coverage.json').read_text(encoding='utf-8-sig'))
pilot_keys = {key for task in coverage['rawDeliverables']
              if '/batch01-pilot/' in task['raw'] for key in task['produces']}
planned_keys = {key for task in queue for key in task['produces']} | pilot_keys
expected_keys = set(coverage['requiredFlat']) | {
    key for task in queue for key in task['produces'] if key.startswith('premade_')}
if planned_keys != expected_keys:
    errors.append('Source plan key mismatch: ' + repr(sorted(planned_keys ^ expected_keys)))
if len(planned_keys) != 944:
    errors.append('Expected 944 planned keys, got ' + str(len(planned_keys)))
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
          'plannedTotalLayerKeys': len(planned_keys),
          'gameReadyLayerProcessingComplete': manifest['gameReadyLayerProcessingComplete'],
          'errors': errors}
print(json.dumps(report, indent=2))
raise SystemExit(bool(errors))
