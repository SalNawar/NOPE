"""Compares two metrics_baseline.json files and prints regressions per metric.

Usage: python compare.py BASELINE.json CURRENT.json [--allow key,key]

A regression is: a bigger headline count (every headline metric except the
informational size counters), a new compiler warning, a new clone group,
a new per-frame Find/GetComponent or allocation site, a new unit over a
threshold, a new rule-code magic number or string literal, new static
mutable state, a new singleton access site or a new unused member. Items
are keyed without line numbers (file + kind + code text / member name), so
code that merely moved is not reported. Exit code 1 when there are
regressions not covered by --allow (headline keys or section names).
"""
from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from pathlib import Path

INFORMATIONAL = {"files", "types", "lines", "code_lines", "method_units", "methods_excluding_accessors"}


def _load(p):
    p = Path(p)
    if p.suffix == ".gz":
        import gzip
        return json.loads(gzip.decompress(p.read_bytes()).decode("utf-8"))
    return json.loads(p.read_text(encoding="utf-8"))


def _new(old_items, new_items):
    """Multiset difference: items in new beyond their count in old."""
    c = Counter(old_items)
    out = []
    for x in new_items:
        if c[x] > 0:
            c[x] -= 1
        else:
            out.append(x)
    return out


def sections(d):
    """Keyed item lists per section (line numbers left out on purpose)."""
    s = {}
    w = d.get("compiler_warnings") or {"list": []}
    s["compiler_warnings"] = [(x["file"], x["code"], x["message"]) for x in w["list"]]
    dup = d["duplication"]["groups"]
    s["clone_groups"] = [(k, g["tokens"], tuple(sorted(loc.rsplit(":", 1)[0] for loc in g["locations"])))
                         for k, gl in sorted(dup.items()) for g in gl]
    pf = d["per_frame"]
    s["per_frame_find"] = [(x["file"], x["kind"], x["code"]) for x in pf["find_sites"]]
    s["per_frame_alloc"] = [(x["file"], x["kind"], x["code"]) for x in pf["alloc_sites"]]
    th = d["thresholds"]
    s["types_over_limit"] = [t["name"] for t in d["types"] if t["lines"] > th["type_lines"]]
    s["methods_over_line_limit"] = [(u["file"], u["name"]) for u in d["units"] if u["lines"] > th["method_lines"]]
    cc = lambda u: u["cc_roslyn"] if u["cc_roslyn"] is not None else u["cc_calibrated"]  # noqa: E731
    s["methods_over_complexity_limit"] = [(u["file"], u["name"]) for u in d["units"] if cc(u) > th["complexity"]]
    lit = d["literals"]
    s["rule_magic_numbers"] = [(x["file"], x["member"], x["literal"]) for x in lit["rule_magic_numbers"]]
    s["rule_strings"] = [(x["file"], x["member"], x["literal"]) for x in lit["rule_strings"]]
    s["static_mutable_state"] = [(x["file"], x["type"], x["member"], x["kind"]) for x in d["static_mutable_state"]]
    s["singleton_sites"] = [(x["file"], x["type"], x["accessor"], x["in_member"]) for x in d["singletons"]["sites"]]
    u = d["unused"]
    s["unused_members"] = [(x["file"], x["type"], x["member"]) for x in u["analyzer_unused"] + u["text_unused"]]
    return s


def compare(old, new):
    report = {"headline": [], "sections": {}, "improved": []}
    for k in sorted(set(old["headline"]) | set(new["headline"])):
        a, b = old["headline"].get(k), new["headline"].get(k)
        if a is None or b is None or a == b:
            continue
        if k in INFORMATIONAL:
            report["improved"].append(f"{k}: {a} -> {b} (informational)")
        elif b > a:
            report["headline"].append(f"{k}: {a} -> {b} (+{b - a})")
        else:
            report["improved"].append(f"{k}: {a} -> {b} ({b - a})")
    so, sn = sections(old), sections(new)
    for name in sorted(sn):
        added = _new(so.get(name, []), sn[name])
        if added:
            report["sections"][name] = added
    return report


def main(argv=None):
    ap = argparse.ArgumentParser(description="Diff two metrics_baseline.json files")
    ap.add_argument("baseline", type=Path)
    ap.add_argument("current", type=Path)
    ap.add_argument("--allow", default="", help="comma-separated headline keys / section names to accept")
    a = ap.parse_args(argv)
    old, new = _load(a.baseline), _load(a.current)
    allow = {x.strip() for x in a.allow.split(",") if x.strip()}
    r = compare(old, new)
    print(f"baseline {old['commit'][:10]} -> current {new['commit'][:10]}")
    failing = 0
    for line in r["headline"]:
        key = line.split(":", 1)[0]
        tag = "allowed" if key in allow else "REGRESSION"
        failing += tag == "REGRESSION"
        print(f"  [{tag}] {line}")
    for name, items in r["sections"].items():
        tag = "allowed" if name in allow else "REGRESSION"
        failing += tag == "REGRESSION"
        print(f"  [{tag}] {name}: {len(items)} new")
        for x in items[:25]:
            print(f"      + {x}")
        if len(items) > 25:
            print(f"      ... {len(items) - 25} more")
    for line in r["improved"]:
        print(f"  [ok] {line}")
    if not failing:
        print("no regressions")
    return 1 if failing else 0


if __name__ == "__main__":
    sys.exit(main())
