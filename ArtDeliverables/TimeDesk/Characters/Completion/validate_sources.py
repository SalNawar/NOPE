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
    replacements_path = HERE / 'interim-head-replacements.json'
    replacements = json.loads(replacements_path.read_text()) if replacements_path.exists() else {}
    allowed = {f'Assets/Art/Characters/Resources/Characters/head_{g}_skin{s}_facea.png'
               for g in ('m', 'f') for s in range(2, 6)}
    if set(replacements) - allowed:
        errors.append('Unexpected pilot replacement: ' + repr(sorted(set(replacements) - allowed)))
    for relative, expected in json.loads(baseline_path.read_text(encoding='utf-8')).items():
        path = ROOT / relative
        replacement = replacements.get(relative)
        if replacement and relative in allowed:
            backup = ROOT / replacement['backup']
            if (replacement['originalSha256'] != expected or not backup.exists()
                    or hashlib.sha256(backup.read_bytes()).hexdigest() != expected):
                errors.append('Interim pilot original not preserved: ' + relative)
            expected = replacement['replacementSha256']
        if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
            errors.append('Preserved pilot changed: ' + relative)
installed_count = 0
if manifest['gameReadyLayerProcessingComplete']:
    processed = json.loads((HERE / 'processed-validation.json').read_text())
    native = json.loads((HERE / 'unity-import-validation.json').read_text())
    if processed['errors'] or set(processed['files']) != planned_keys:
        errors.append('Processed output inventory does not match the completed source plan')
    if native['errors'] or any(native.get(k) != 944 for k in ('expectedKeys', 'importedKeys', 'resourceKeys')):
        errors.append('Native Unity validation is incomplete or failed')
    folder = ROOT / 'Assets/Art/Characters/Resources/Characters'
    installed_count = len(list(folder.glob('*.png')))
    if {p.stem for p in folder.glob('*.png')} != planned_keys:
        errors.append('Installed sprite inventory differs from required keys')
    for key, output in processed['files'].items():
        path = folder / (key + '.png')
        if not path.exists() or hashlib.sha256(path.read_bytes()).hexdigest() != output['sha256']:
            errors.append('Installed output changed: ' + key)
report = {'requiredNewSources': len(queue), 'presentNewSources': len(records),
          'installedSpriteKeys': installed_count,
          'plannedNewLayerKeys': len({key for task in queue for key in task['produces']}),
          'plannedTotalLayerKeys': len(planned_keys),
          'gameReadyLayerProcessingComplete': manifest['gameReadyLayerProcessingComplete'],
          'errors': errors}
print(json.dumps(report, indent=2))
raise SystemExit(bool(errors))
