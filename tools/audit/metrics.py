"""Static baseline metrics -> <out>/metrics_baseline.json + metrics_baseline.md.

Inputs: the source set (sources.py), the lexer/parser (cslex.py) and the
analyzer results (<out>/analyzer.json from analyzer_build.py, or --analyzer).

Metrics (each section of the .md states its rule and limits):
  size          lines per type (span of all parts, nested types included) and
                per method-like unit (header line .. closing line); code lines
                = non-blank, non-comment lines. Thresholds: type > 400 lines,
                method > 60 lines, complexity > 15.
  complexity    Roslyn CA1502 value per unit when the analyzer ran, plus two
                token approximations (codescan.complexity) cross-checked
                against it.
  warnings      compiler (CS) warnings in the source set, from analyzer.json.
  unused        analyzer IDE0051/IDE0052/CA1823 + a text fallback; serialized
                fields never read in code are listed separately.
  duplication   clones.py token-window clones, exact and normalized, W=40/60/100,
                production and test code separately.
  literals      magic numbers and string literals by tier (rule/presentation/
                editor/test), codescan.LiteralScanner buckets.
  per-frame     Find/GetComponent and allocation sites reachable from
                Update/LateUpdate/FixedUpdate/OnGUI within the same type.
  statics       static mutable state.
  singletons    Type.Instance-style access sites and FindObject* searches.
Output is deterministic: sorted, repo-relative paths, no timestamps.
"""
from __future__ import annotations

import argparse
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import clones  # noqa: E402
import codescan  # noqa: E402
import metrics_md  # noqa: E402
import sources  # noqa: E402
from model import ENTRY_ATTRIBUTES, UNITY_MESSAGES, Model, roslyn_symbol  # noqa: E402

TYPE_LINES_LIMIT = 400
METHOD_LINES_LIMIT = 60
COMPLEXITY_LIMIT = 15
WINDOWS = (40, 60, 100)
DETAIL_WINDOW = 60


class Ctx:
    def __init__(self, model: Model, analyzer: dict | None, assets: dict):
        self.model = model
        self.analyzer = analyzer
        self.assets = assets
        self._lines = {}

    def src_line(self, path: str, line: int) -> str:
        if path not in self._lines:
            self._lines[path] = self.model.by_path[path].read().split("\n")
        ls = self._lines[path]
        return ls[line - 1].strip() if 0 < line <= len(ls) else ""


# ------------------------------------------------------------------ size

def size_and_complexity(ctx: Ctx):
    model = ctx.model
    arms = {p: codescan.switch_arms(fp.tokens, fp.match) for p, fp in model.files.items()}
    roslyn = defaultdict(list)
    if ctx.analyzer:
        for e in ctx.analyzer["complexity"]:
            roslyn[(e["file"], e["symbol"])].append(e)
    used = set()
    units = []
    for u in model.units:
        fp = model.files[u.file]
        spec, cal = codescan.complexity(fp.tokens, u.body[0], u.body[1], arms[u.file])
        cand = {u.name_line, u.line}
        if u.body[1] > u.body[0]:
            cand.add(fp.tokens[u.body[0]].line)
        rc = None
        for e in roslyn.get((u.file, roslyn_symbol(u)), []):
            if e["line"] in cand and id(e) not in used:
                used.add(id(e))
                rc = e["complexity"]
                break
        body_start = fp.tokens[u.body[0] - 1].line   # the '{' or '=>' opening the body
        units.append({
            "name": u.name, "file": u.file, "line": u.line, "end_line": u.end_line,
            "lines": u.lines, "body_lines": u.end_line - body_start + 1,
            "code_lines": model.code_lines_in(u.file, u.line, u.end_line),
            "kind": u.accessor or u.member.kind, "role": model.by_path[u.file].role,
            "cc_roslyn": rc, "cc_spec": spec, "cc_calibrated": cal,
        })
    unmatched = []
    if ctx.analyzer:
        for e in ctx.analyzer["complexity"]:
            if id(e) not in used:
                unmatched.append(e)
    types = []
    for td in model.types:
        parts = sorted(td.parts, key=lambda p: (p.file, p.line))
        types.append({
            "name": td.display_name, "kind": td.kind, "assembly": model.assembly(td),
            "role": model.role(td), "owner": model.owner(td),
            "files": [f"{p.file}:{p.line}-{p.end_line}" for p in parts],
            "lines": sum(p.end_line - p.line + 1 for p in parts),
            "code_lines": sum(model.code_lines_in(p.file, p.line, p.end_line) for p in parts),
            "methods": sum(1 for m in td.members if m.kind in ("method", "ctor", "dtor", "operator", "conversion")),
        })
    types.sort(key=lambda t: (-t["lines"], t["name"]))
    units.sort(key=lambda u: (-u["lines"], u["file"], u["line"], u["name"]))
    return types, units, unmatched


def cc_value(u):
    return u["cc_roslyn"] if u["cc_roslyn"] is not None else u["cc_calibrated"]


def cross_check(units):
    rows = [u for u in units if u["cc_roslyn"] is not None]
    out = {"units_with_roslyn": len(rows), "units_without_roslyn": len(units) - len(rows)}
    for rule in ("cc_spec", "cc_calibrated"):
        agree = sum(1 for u in rows if u[rule] == u["cc_roslyn"])
        within1 = sum(1 for u in rows if abs(u[rule] - u["cc_roslyn"]) <= 1)
        mad = sum(abs(u[rule] - u["cc_roslyn"]) for u in rows) / max(1, len(rows))
        over15_r = sum(1 for u in rows if u["cc_roslyn"] > COMPLEXITY_LIMIT)
        over15_a = sum(1 for u in rows if u[rule] > COMPLEXITY_LIMIT)
        worst = sorted(rows, key=lambda u: (-abs(u[rule] - u["cc_roslyn"]), u["file"], u["line"]))[:15]
        out[rule] = {"exact_agreement": agree, "within_1": within1, "mean_abs_diff": round(mad, 3),
                     "over_limit_roslyn": over15_r, "over_limit_approx": over15_a,
                     "biggest_disagreements": [{"name": u["name"], "file": u["file"], "line": u["line"],
                                                "roslyn": u["cc_roslyn"], "approx": u[rule]} for u in worst
                                               if u[rule] != u["cc_roslyn"]]}
    return out


# ---------------------------------------------------------------- unused

def unused_members(ctx: Ctx):
    """Analyzer (IDE0051 unused, IDE0052 written-never-read, CA1823 unused
    field) and a text fallback: a private field/const/method/property/event
    whose identifier token occurs in the type's files only as often as it is
    declared there. Excluded from the fallback: Unity messages (by name),
    methods named by a UnityEvent m_MethodName in assets, names that appear
    as a string literal anywhere (Invoke/SendMessage/StartCoroutine), members
    with reflection-entry attributes, overrides, explicit interface
    implementations, constructors/operators. [SerializeField] fields are
    reported separately as `serialized, never read in code` (Unity writes
    them; code never reads them)."""
    model = ctx.model
    strings = set()
    for fp in model.files.values():
        for t in fp.tokens:
            if t.kind == "string" and t.text.startswith('"'):
                strings.add(t.text[1:-1])
    method_names = set(ctx.assets["method_names"])
    by_decl = {}
    for fp in model.files.values():
        for m in fp.members:
            by_decl[(m.file, m.name_line, m.name)] = m

    def serialized(m):
        return m.kind in ("field", "property") and any(a.name == "SerializeField" for a in m.attributes)

    def excluded(m):
        """Why a private member is not a real unused candidate, or None."""
        if m.kind == "method" and m.name in UNITY_MESSAGES:
            return "unity message"
        if m.kind == "method" and m.name in method_names:
            return "UnityEvent m_MethodName"
        if any(a.name in ENTRY_ATTRIBUTES for a in m.attributes):
            return "reflection-entry attribute"
        if m.kind == "method" and m.name in strings:
            return "named in a string literal"   # Invoke/SendMessage/StartCoroutine("X")
        return None

    text_unused, text_serialized = [], []
    for td in model.types:
        files = sorted({p.file for p in td.parts})
        decl_counts = Counter(m.name for m in td.members)
        for m in td.members:
            if m.effective_access() != "private" or m.kind in ("ctor", "dtor", "operator", "conversion",
                                                                "enum_member", "indexer"):
                continue
            if "override" in m.modifiers or "." in m.name:
                continue
            if excluded(m):
                continue
            uses = sum(model.ident_counts[f].get(m.name, 0) for f in files)
            if uses > decl_counts[m.name]:
                continue
            rec = {"file": m.file, "line": m.name_line, "type": td.display_name, "member": m.name, "kind": m.kind,
                   "named_in_strings": m.name in strings}
            (text_serialized if serialized(m) else text_unused).append(rec)
    an_unused, an_serialized, excluded_keys = [], [], {}
    raw = 0
    if ctx.analyzer:
        seen = set()
        for d in ctx.analyzer["unused_members"]:
            name = d["message"].split("'")[1].rsplit(".", 1)[-1] if "'" in d["message"] else "?"
            if (d["file"], d["line"], name) not in seen:
                seen.add((d["file"], d["line"], name))
                raw += 1
            m = by_decl.get((d["file"], d["line"], name))
            why = excluded(m) if m is not None else None
            if why:
                excluded_keys[(d["file"], d["line"], name)] = why
                continue
            rec = {"file": d["file"], "line": d["line"], "member": name, "code": d["code"],
                   "type": m.parent.display_name if m else "?", "kind": m.kind if m else "?",
                   "named_in_strings": name in strings}
            (an_serialized if (m is not None and serialized(m)) else an_unused).append(rec)

    def merge(recs):
        out = {}
        for r in recs:
            key = (r["file"], r["line"], r["member"])
            cur = out.setdefault(key, {"file": r["file"], "line": r["line"], "member": r["member"],
                                       "type": r["type"], "kind": r["kind"], "codes": [],
                                       "named_in_strings": r["named_in_strings"]})
            if r.get("code") and r["code"] not in cur["codes"]:
                cur["codes"].append(r["code"])
            if cur["type"] == "?":
                cur["type"], cur["kind"] = r["type"], r["kind"]
        for v in out.values():
            v["codes"].sort()
        return [out[k] for k in sorted(out)]

    an_keys = {(r["file"], r["line"], r["member"]) for r in an_unused}
    tx_keys = {(r["file"], r["line"], r["member"]) for r in text_unused}
    for r in text_unused:
        r["code"] = "text"
    for r in text_serialized:
        r["code"] = "text"
    return {
        "analyzer_ran": ctx.analyzer is not None,
        "analyzer_raw_members": raw,
        "analyzer_excluded": dict(sorted(Counter(excluded_keys.values()).items())),
        "analyzer_unused": merge(an_unused),
        "text_unused": merge(text_unused),
        "both": len(an_keys & tx_keys),
        "analyzer_only": len(an_keys - tx_keys),
        "text_only": len(tx_keys - an_keys),
        "serialized_never_read": merge(an_serialized + text_serialized),
        "unused_parameters_ide0060": ctx.analyzer["unused_parameters"] if ctx.analyzer else [],
    }


# ------------------------------------------------------------ duplication

def duplication(ctx: Ctx):
    model = ctx.model
    scopes = {"production": sorted(p for p, s in model.by_path.items() if s.role != "test"),
              "test": sorted(p for p, s in model.by_path.items() if s.role == "test")}
    out = {"windows": list(WINDOWS), "detail_window": DETAIL_WINDOW, "summary": {}, "groups": {}, "per_file": {}}
    for scope, paths in scopes.items():
        for mode in ("exact", "normalized"):
            for W in WINDOWS:
                groups, periodic, streams = clones.run(model.files, paths, mode, W)
                cover = clones.duplicated_lines(groups, streams)
                key = f"{scope}/{mode}/W{W}"
                out["summary"][key] = {"groups": len(groups),
                                       "locations": sum(len(g["locations"]) for g in groups),
                                       "duplicated_lines": sum(len(v) for v in cover.values()),
                                       "files": len(cover),
                                       "periodic_regions": len(periodic),
                                       "periodic_region_lines": sum(b - a + 1 for _, a, b, _ in periodic)}
                if W != DETAIL_WINDOW:
                    continue
                out.setdefault("periodic_regions", {})[f"{scope}/{mode}"] = [
                    f"{f}:{a}-{b} ({n} tokens)" for f, a, b, n in periodic]
                gl = []
                for g in groups:
                    f, lo, hi, a, b = g["locations"][0]
                    snippet = [ctx.src_line(f, ln) for ln in range(a, min(b, a + 2) + 1)]
                    gl.append({"tokens": g["tokens"], "lines": b - a + 1,
                               "locations": [f"{x[0]}:{x[3]}-{x[4]}" for x in g["locations"]],
                               "snippet": " | ".join(s for s in snippet if s)})
                out["groups"][f"{scope}/{mode}"] = gl
                out["per_file"][f"{scope}/{mode}"] = dict(sorted(
                    ((f, len(v)) for f, v in cover.items()), key=lambda kv: (-kv[1], kv[0])))
    return out


# --------------------------------------------------------------- literals

def literals(ctx: Ctx):
    model = ctx.model
    tiers = defaultdict(lambda: {"numbers": Counter(), "strings": Counter(), "files": {}})
    rule_numbers, rule_strings = [], []
    named = []
    serialized = []
    for path in sorted(model.files):
        sf = model.by_path[path]
        sc = codescan.LiteralScanner(model, path)
        nums, strs = sc.numbers(), sc.strings()
        tier = tiers[sf.tier]
        nb = Counter(n["bucket"] for n in nums)
        sb = Counter(s["bucket"] for s in strs)
        tier["numbers"].update(nb)
        tier["strings"].update(sb)
        tier["files"][path] = {"magic": nb.get("magic", 0), "strings": sb.get("literal", 0),
                               "log": sb.get("log", 0), "field_initializer": nb.get("field_initializer", 0)
                               + sb.get("field_initializer", 0),
                               "serialized_default": nb.get("serialized_default", 0) + sb.get("serialized_default", 0)}
        if sf.tier == "rule":
            for n in nums:
                rec = {"file": path, "line": n["line"], "literal": n["text"], "member": n["member"]}
                if n["bucket"] == "magic":
                    rule_numbers.append(dict(rec, array_size=n["array_size"]))
                elif n["bucket"] == "field_initializer":
                    named.append(dict(rec, kind="number"))
                elif n["bucket"] == "serialized_default":
                    serialized.append(dict(rec, kind="number"))
            for s in strs:
                rec = {"file": path, "line": s["line"], "literal": s["text"], "member": s["member"]}
                if s["bucket"] == "literal":
                    rule_strings.append(rec)
                elif s["bucket"] == "field_initializer":
                    named.append(dict(rec, kind="string"))
                elif s["bucket"] == "serialized_default":
                    serialized.append(dict(rec, kind="string"))
    summary = {}
    for name, t in sorted(tiers.items()):
        top = sorted(t["files"].items(), key=lambda kv: (-(kv[1]["magic"] + kv[1]["strings"]), kv[0]))
        summary[name] = {"numbers": dict(sorted(t["numbers"].items())),
                         "strings": dict(sorted(t["strings"].items())),
                         "per_file": {f: v for f, v in sorted(t["files"].items())},
                         "top_files": [{"file": f, **v} for f, v in top[:15] if v["magic"] + v["strings"]]}
    return {"allowed_numbers": [0, 1, -1, 2, 0.5, 100], "tiers": summary,
            "rule_magic_numbers": rule_numbers, "rule_strings": rule_strings,
            "rule_named_field_initializers": named, "rule_serialized_defaults": serialized}


# -------------------------------------------------------------- per-frame

def per_frame(ctx: Ctx):
    model = ctx.model
    roots, notes = codescan.per_frame_roots(model)
    reached = codescan.reachable(model, roots)
    arms = {p: codescan.switch_arms(fp.tokens, fp.match) for p, fp in model.files.items()}
    finds, allocs = [], []
    reached_list = sorted(reached.values(), key=lambda r: (r[0].file, r[0].line, r[0].name))
    for u, root, chain, all_roots in reached_list:
        base = {"file": u.file, "root": root, "chain": chain, "roots": sorted(all_roots)}
        for line, kind, call in codescan.find_sites(model, u):
            finds.append(dict(base, line=line, kind=kind, call=call, code=ctx.src_line(u.file, line)))
        for line, kind in codescan.alloc_sites(model, u, arms):
            allocs.append(dict(base, line=line, kind=kind, code=ctx.src_line(u.file, line)))
    key = lambda r: (r["file"], r["line"], r["kind"], r["root"])  # noqa: E731
    finds.sort(key=key)
    allocs.sort(key=key)
    return {
        "roots": [{"name": u.name, "file": u.file, "line": u.line, "via": why} for u, why in roots],
        "registration_notes": notes,
        "reached_units": [{"name": r[0].name, "file": r[0].file, "line": r[0].line, "root": r[1],
                           "depth": len(r[2]) - 1} for r in reached_list],
        "find_sites": finds,
        "find_by_kind": dict(sorted(Counter(f["kind"] for f in finds).items())),
        "alloc_sites": allocs,
        "alloc_by_kind": dict(sorted(Counter(a["kind"] for a in allocs).items())),
    }


# ------------------------------------------------------------------ main

def build(ctx: Ctx) -> dict:
    model = ctx.model
    types, units, unmatched = size_and_complexity(ctx)
    an = ctx.analyzer
    warnings = an["compiler_warnings"] if an else None
    statics = codescan.static_state(model)
    definers, sites, searches = codescan.singletons(model)
    single_by_type = {}
    for s in sites:
        g = single_by_type.setdefault(s["type"], {"sites": 0, "files": set(), "by_role": Counter(),
                                                    "accessors": Counter()})
        g["sites"] += 1
        g["files"].add(s["file"])
        g["by_role"][s["role"]] += 1
        g["accessors"][s["accessor"]] += 1
    singles = {k: {"sites": v["sites"], "files": sorted(v["files"]), "by_role": dict(sorted(v["by_role"].items())),
                   "accessors": dict(sorted(v["accessors"].items()))}
               for k, v in sorted(single_by_type.items(), key=lambda kv: (-kv[1]["sites"], kv[0]))}
    search_by_type = dict(sorted(Counter(s["type"] for s in searches).items(), key=lambda kv: (-kv[1], kv[0])))
    dup = duplication(ctx)
    lit = literals(ctx)
    pf = per_frame(ctx)
    unused = unused_members(ctx)
    over_type = [t for t in types if t["lines"] > TYPE_LINES_LIMIT]
    over_method = [u for u in units if u["lines"] > METHOD_LINES_LIMIT]
    over_cc = sorted([u for u in units if cc_value(u) > COMPLEXITY_LIMIT],
                     key=lambda u: (-cc_value(u), u["file"], u["line"]))
    rule_tier = lit["tiers"].get("rule", {"numbers": {}, "strings": {}})
    headline = {
        "files": len(model.sources),
        "types": len(model.types),
        "lines": sum(fp.lexinfo.n_lines for fp in model.files.values()),
        "code_lines": sum(len(fp.lexinfo.code_lines) for fp in model.files.values()),
        "method_units": len(units),
        "methods_excluding_accessors": sum(1 for u in units if u["kind"] in ("method", "ctor", "dtor", "operator", "conversion")),
        "compiler_warnings": warnings["total"] if warnings else None,
        "types_over_400_lines": len(over_type),
        "types_over_400_lines_production": sum(1 for t in over_type if t["role"] != "test"),
        "methods_over_60_lines": len(over_method),
        "methods_over_60_lines_production": sum(1 for u in over_method if u["role"] != "test"),
        "complexity_over_15": len(over_cc),
        "complexity_over_15_production": sum(1 for u in over_cc if u["role"] != "test"),
        "dup_groups_exact_W60_production": dup["summary"]["production/exact/W60"]["groups"],
        "dup_lines_exact_W60_production": dup["summary"]["production/exact/W60"]["duplicated_lines"],
        "dup_groups_normalized_W60_production": dup["summary"]["production/normalized/W60"]["groups"],
        "dup_lines_normalized_W60_production": dup["summary"]["production/normalized/W60"]["duplicated_lines"],
        "dup_groups_exact_W60_test": dup["summary"]["test/exact/W60"]["groups"],
        "dup_lines_exact_W60_test": dup["summary"]["test/exact/W60"]["duplicated_lines"],
        "dup_groups_normalized_W60_test": dup["summary"]["test/normalized/W60"]["groups"],
        "dup_lines_normalized_W60_test": dup["summary"]["test/normalized/W60"]["duplicated_lines"],
        "magic_numbers_rule_code": rule_tier["numbers"].get("magic", 0),
        "string_literals_rule_code": rule_tier["strings"].get("literal", 0),
        "per_frame_find_getcomponent_sites": len(pf["find_sites"]),
        "per_frame_allocation_sites": len(pf["alloc_sites"]),
        "static_mutable_state": len(statics),
        "singleton_access_sites": len(sites),
        "unused_private_members_analyzer": len(unused["analyzer_unused"]),
        "unused_private_members_text": len(unused["text_unused"]),
        "serialized_never_read": len(unused["serialized_never_read"]),
    }
    coupling = []
    if an:
        names = {}
        for td in model.types:
            for p in td.parts:
                names.setdefault((p.file, td.name), td)
        for e in an["coupling"]:
            td = names.get((e["file"], e["symbol"]))
            if td is not None:
                coupling.append({"type": td.display_name, "file": e["file"], "line": e["line"],
                                 "types": e["types"], "namespaces": e["namespaces"]})
        coupling.sort(key=lambda c: (-c["types"], c["type"], c["file"], c["line"]))
    return {
        "tool": sources.TOOL_VERSION,
        "commit": model.head,
        "class_coupling_types": coupling,
        "thresholds": {"type_lines": TYPE_LINES_LIMIT, "method_lines": METHOD_LINES_LIMIT,
                       "complexity": COMPLEXITY_LIMIT},
        "analyzer_available": an is not None,
        "headline": headline,
        "types": types,
        "units": sorted(units, key=lambda u: (u["file"], u["line"], u["name"])),
        "over_limits": {"types": [t["name"] for t in over_type],
                        "methods_lines": [f"{u['name']} ({u['file']}:{u['line']})" for u in over_method],
                        "methods_complexity": [f"{u['name']} ({u['file']}:{u['line']}) = {cc_value(u)}" for u in over_cc]},
        "complexity_cross_check": cross_check(units),
        "complexity_unmatched_analyzer_entries": {
            "count": len(unmatched),
            "by_symbol_prefix": dict(sorted(Counter(
                e["symbol"].split("_", 1)[0] + "_" if e["symbol"].startswith(("get_", "set_", "add_", "remove_", "init_"))
                else "method/other" for e in unmatched).items())),
            "max_complexity": max((e["complexity"] for e in unmatched), default=0)},
        "compiler_warnings": warnings,
        "csproj_nowarn_suppressed": an["csproj_nowarn_suppressed"] if an else None,
        "unused": unused,
        "duplication": dup,
        "literals": lit,
        "per_frame": pf,
        "static_mutable_state": statics,
        "static_mutable_by_kind": dict(sorted(Counter(s["kind"] for s in statics).items())),
        "singletons": {"definers": {k: v for k, v in sorted(definers.items())}, "by_type": singles,
                       "sites": sites, "find_object_searches_runtime": searches,
                       "find_object_by_type": search_by_type},
    }


def main(argv=None):
    here = Path(__file__).resolve().parent
    ap = argparse.ArgumentParser(description="Static baseline metrics")
    ap.add_argument("--repo", type=Path, default=here.parent.parent)
    ap.add_argument("--out", type=Path, default=None)
    ap.add_argument("--analyzer", type=Path, default=None, help="analyzer.json (default <out>/analyzer.json)")
    a = ap.parse_args(argv)
    repo = a.repo.resolve()
    out = (a.out or sources.default_out(repo)).resolve()
    out.mkdir(parents=True, exist_ok=True)
    apath = a.analyzer or (out / "analyzer.json")
    analyzer = json.loads(Path(apath).read_text(encoding="utf-8")) if Path(apath).exists() else None
    if analyzer is None:
        print(f"warning: {apath} not found; analyzer sections will be empty", file=sys.stderr)
    model = Model(repo)
    if analyzer is not None and analyzer.get("commit") != model.head:
        print(f"warning: analyzer.json is for {analyzer.get('commit')}, repo is at {model.head}", file=sys.stderr)
    ctx = Ctx(model, analyzer, sources.scan_assets(repo))
    data = build(ctx)
    sources.write_json(out / "metrics_baseline.json", data)
    sources.write_text(out / "metrics_baseline.md", metrics_md.render(data))
    h = data["headline"]
    print("metrics: " + ", ".join(f"{k}={v}" for k, v in h.items()))
    return 0


if __name__ == "__main__":
    sys.exit(main())
