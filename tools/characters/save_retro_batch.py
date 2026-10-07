import json,subprocess,sys
from pathlib import Path
p=Path(sys.argv[1]);items=json.loads(p.read_text(encoding='utf-8-sig'))
for job in items:
 single=p.with_name('pending-job.json');single.write_text(json.dumps(job),encoding='utf-8')
 subprocess.run([sys.executable,'tools/characters/save_retro.py',str(single)],check=True)
