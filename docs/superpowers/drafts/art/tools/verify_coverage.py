"""Checks a folder of delivered character art against coverage.json.

usage: python verify_coverage.py <folder with game-ready PNGs> [--raw <raw root>]
  <folder>  usually Assets/Art/Characters/Resources/Characters (game keys, <key>.png)
  --raw     optional: the repo root, to also check the raw ChatGPT files listed per batch
Prints present/missing per group and size problems (every file must be 1024x1536). Exit code 1 if anything is missing.
"""
import json, struct, sys
from pathlib import Path

COV = json.loads((Path(__file__).resolve().parent.parent / "coverage.json").read_text(encoding="utf-8"))

def png_size(p):
    with open(p, "rb") as f:
        h = f.read(24)
    if h[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", h[16:24])

def main():
    if len(sys.argv) < 2:
        print(__doc__); return 2
    folder = Path(sys.argv[1])
    req = COV["required"]
    groups = {"bases": req["bases"], "premades": [k for v in req["premades"].values() for k in v],
              "future": [k for v in req["future"].values() for g in v for k in v[g]] + req["futureShared"],
              "garments": [k for v in req["garments"].values() for g in v for k in v[g]]}
    missing_total, bad = 0, []
    for name, keys in groups.items():
        miss = [k for k in keys if not (folder / f"{k}.png").exists()]
        missing_total += len(miss)
        print(f"{name}: {len(keys) - len(miss)}/{len(keys)} present" + (f"; first missing: {', '.join(miss[:8])}" if miss else ""))
        for k in keys:
            p = folder / f"{k}.png"
            if p.exists():
                s = png_size(p)
                if s != (COV["canvas"]["width"], COV["canvas"]["height"]):
                    bad.append(f"{k}.png is {s}")
    extra = sorted(p.stem for p in folder.glob("*.png") if p.stem not in set(COV["requiredFlat"]))
    if extra:
        print(f"not in coverage ({len(extra)}): {', '.join(extra[:10])}")
    for b in bad:
        print("SIZE", b)
    if "--raw" in sys.argv:
        root = Path(sys.argv[sys.argv.index("--raw") + 1])
        by_batch = {}
        for r in COV["rawDeliverables"]:
            by_batch.setdefault(r["batch"], []).append((root / r["raw"]).exists())
        for b, flags in sorted(by_batch.items()):
            print(f"raw batch {b}: {sum(flags)}/{len(flags)}")
    print(f"total: {len(COV['requiredFlat']) - missing_total}/{len(COV['requiredFlat'])}")
    return 1 if missing_total or bad else 0

if __name__ == "__main__":
    sys.exit(main())
