"""Applies reviewer fixes (from the review workflow output) to costumes.json."""
import json, shutil, sys
from pathlib import Path

SCRATCH = Path(__file__).parent
src = SCRATCH / "costumes.json"
shutil.copy(src, SCRATCH / "costumes.before-review.json")
data = json.loads(src.read_text(encoding="utf-8"))
E = {(e["country"], e["era"]): e for e in data["entries"]}
review = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))["result"]

applied, skipped = 0, []
for f in review["fixes"]:
    e = E.get((f["country"].lower(), f["era"].lower()))
    if e is None:
        skipped.append(f); continue
    field = f["field"]
    if field == "avoid":
        e["avoid"] = [s.strip() for s in f["newValue"].split(" ; ") if s.strip()]
    elif field == "signature":
        e["signature"] = f["newValue"]
    elif "." in field:
        g, k = field.split(".", 1)
        if g not in ("male", "female") or k not in e[g]:
            skipped.append(f); continue
        e[g][k] = f["newValue"]
    else:
        skipped.append(f); continue
    applied += 1
    print(f"- {f['country']}/{f['era']} {field}: {f['reason'][:160]}")

src.write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")
print(f"applied {applied}, skipped {len(skipped)}")
for s in skipped: print("SKIPPED", s)
