"""Audit Famous33 source delivery without changing image pixels."""
import json, hashlib, struct
from pathlib import Path
root = Path("ArtDeliverables/TimeDesk/Characters/Famous33")
data = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
roster = json.loads((root / "roster.json").read_text(encoding="utf-8"))
variants = ["neutral", "happy", "angry", "worried", "pose_explaining", "pose_thinking", "pose_objecting"]
expected = {(p["id"], v) for p in roster for v in variants}
actual = {(d["id"], d["expression"]) for d in data}
assert len(roster) == 33 and len(expected) == 231
assert len(actual) == len(data), "Duplicate manifest entries"
assert actual == expected, f"Missing: {sorted(expected-actual)}; unexpected: {sorted(actual-expected)}"
hashes = set()
paths = set()
for record in data:
    path = Path(record["path"])
    raw = path.read_bytes()
    assert raw[:8] == b"\x89PNG\r\n\x1a\n", path
    assert struct.unpack(">II", raw[16:24]) == (1024, 1536), path
    digest = hashlib.sha256(raw).hexdigest()
    assert digest == record["sha256"], path
    assert digest not in hashes, f"Duplicate image: {path}"
    hashes.add(digest)
    paths.add(path.resolve())
    assert path.with_suffix(".prompt.md").read_text(encoding="utf-8") == record["prompt"], path
    assert record["visual_review"].startswith("reviewed"), path
assert paths == {p.resolve() for p in (root/"Raw").rglob("*.png")}, "Untracked PNGs outside manifest"
checklist = (root/"CHECKLIST.md").read_text(encoding="utf-8")
for person in roster:
    labels = [next(d["visual_review"] for d in data if d["id"] == person["id"] and d["expression"] == v) for v in variants]
    assert "| " + " | ".join([person["name"]] + labels) + " |" in checklist, person["id"]
print("PASS: 33 characters x 7 variants = 231 unique PNGs; dimensions, hashes, prompts, visual-review records, raw-file coverage and checklist verified.")

