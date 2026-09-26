"""Renders metrics_baseline.json as Markdown (headline, then one section per
metric with its rule, limits and top offenders). Pure function of the data."""
from __future__ import annotations


def _t(rows, header):
    out = ["| " + " | ".join(header) + " |", "|" + "|".join("---" for _ in header) + "|"]
    for r in rows:
        out.append("| " + " | ".join(str(x).replace("|", "\\|").replace("\n", " ") for x in r) + " |")
    return out


def _short(path: str) -> str:
    return path.replace("Assets/", "", 1)


def _cc(u):
    return u["cc_roslyn"] if u["cc_roslyn"] is not None else u["cc_calibrated"]


def render(d: dict) -> str:
    h = d["headline"]
    th = d["thresholds"]
    L = [f"# Static baseline metrics ({d['commit'][:10]})", "",
         f"Tool: {d['tool']}; commit {d['commit']}. Source set: tracked C# under Assets/Scripts, "
         f"Assets/Editor, Assets/Tests (Assets/Editor/OfficeArt excluded). Analyzer results "
         f"{'included' if d['analyzer_available'] else 'NOT available'}.", "",
         "## Headline", ""]
    dup = d["duplication"]["summary"]
    rows = [
        ("Files", h["files"]), ("Types (partials merged)", h["types"]),
        ("Lines (all) / code lines", f"{h['lines']} / {h['code_lines']}"),
        ("Method-like units (methods, ctors, accessors with bodies) / methods", f"{h['method_units']} / {h['methods_excluding_accessors']}"),
        ("Compiler warnings in our code", h["compiler_warnings"]),
        (f"Types > {th['type_lines']} lines (all / production)", f"{h['types_over_400_lines']} / {h['types_over_400_lines_production']}"),
        (f"Methods > {th['method_lines']} lines (all / production)", f"{h['methods_over_60_lines']} / {h['methods_over_60_lines_production']}"),
        (f"Complexity > {th['complexity']} (all / production)", f"{h['complexity_over_15']} / {h['complexity_over_15_production']}"),
        ("Duplicate groups / duplicated lines, W=60 exact, production", f"{h['dup_groups_exact_W60_production']} / {h['dup_lines_exact_W60_production']}"),
        ("Duplicate groups / duplicated lines, W=60 normalized, production", f"{h['dup_groups_normalized_W60_production']} / {h['dup_lines_normalized_W60_production']}"),
        ("Duplicate groups / duplicated lines, W=60 exact, tests", f"{h['dup_groups_exact_W60_test']} / {h['dup_lines_exact_W60_test']}"),
        ("Duplicate groups / duplicated lines, W=60 normalized, tests", f"{h['dup_groups_normalized_W60_test']} / {h['dup_lines_normalized_W60_test']}"),
        ("Magic numbers in rule code", h["magic_numbers_rule_code"]),
        ("String literals in rule code", h["string_literals_rule_code"]),
        ("Per-frame Find/GetComponent sites", h["per_frame_find_getcomponent_sites"]),
        ("Per-frame allocation sites", h["per_frame_allocation_sites"]),
        ("Static mutable state", h["static_mutable_state"]),
        ("Singleton access sites", h["singleton_access_sites"]),
        ("Unused private members: analyzer / text fallback", f"{h['unused_private_members_analyzer']} / {h['unused_private_members_text']}"),
        ("Serialized fields never read in code", h["serialized_never_read"]),
    ]
    L += _t(rows, ["Metric", "Value"]) + [""]

    # ---- size
    L += ["## Size", "",
          "Type lines = span of every part (partials summed; nested types included in their outer type "
          "and also listed on their own). Unit lines = header line to closing line (attributes and doc "
          "comments excluded; the > 60 threshold uses this); body = opening brace (or `=>`) to closing "
          "line. Code lines = lines holding a token or a preprocessor directive.", "",
          "### 25 biggest types", ""]
    L += _t([(f"`{t['name']}`", t["kind"], t["lines"], t["code_lines"], t["methods"], t["role"],
              ", ".join(_short(f) for f in t["files"])) for t in d["types"][:25]],
            ["Type", "Kind", "Lines", "Code", "Methods", "Role", "Parts"]) + [""]
    units = sorted(d["units"], key=lambda u: (-u["lines"], u["file"], u["line"]))
    L += ["### 40 biggest methods", ""]
    L += _t([(f"`{u['name']}`", f"{_short(u['file'])}:{u['line']}", u["lines"], u["body_lines"], u["code_lines"],
              u["cc_roslyn"] if u["cc_roslyn"] is not None else "-", u["cc_spec"]) for u in units[:40]],
            ["Method", "Where", "Lines", "Body", "Code", "CC (Roslyn)", "CC (spec approx)"]) + [""]
    ol = d["over_limits"]
    L += [f"### Over the structure thresholds", "",
          f"- Types > {th['type_lines']} lines: {len(ol['types'])}: " + ", ".join(f"`{x}`" for x in ol["types"]),
          f"- Methods > {th['method_lines']} lines: {len(ol['methods_lines'])} (all in the table above when <= 40).",
          f"- Units with complexity > {th['complexity']}: {len(ol['methods_complexity'])} (Roslyn CA1502 value).", ""]
    top_cc = sorted(d["units"], key=lambda u: (-_cc(u), u["file"], u["line"]))
    L += ["### 30 most complex units", ""]
    L += _t([(f"`{u['name']}`", f"{_short(u['file'])}:{u['line']}", _cc(u), u["cc_spec"], u["lines"])
             for u in top_cc[:30]], ["Unit", "Where", "CC (Roslyn)", "CC (spec approx)", "Lines"]) + [""]

    cp = d.get("class_coupling_types") or []
    if cp:
        L += ["### 15 most coupled types (CA1506 class coupling, per type part)", ""]
        L += _t([(f"`{c['type']}`", f"{_short(c['file'])}:{c['line']}", c["types"], c["namespaces"]) for c in cp[:15]],
                ["Type", "Where", "Coupled types", "Namespaces"]) + [""]

    # ---- complexity
    cx = d["complexity_cross_check"]
    L += ["## Cyclomatic complexity: Roslyn vs approximation", "",
          "Roslyn value = CA1502 from the SDK's NetAnalyzers (CodeMetricsConfig `CA1502: 0` so every "
          "method is reported), matched to units by file + line + Roslyn symbol name "
          f"({cx['units_with_roslyn']} matched, {cx['units_without_roslyn']} unmatched; "
          f"{d['complexity_unmatched_analyzer_entries']['count']} analyzer entries had no unit: "
          f"{d['complexity_unmatched_analyzer_entries']['by_symbol_prefix']}, max complexity "
          f"{d['complexity_unmatched_analyzer_entries']['max_complexity']} - auto-property accessors and "
          "bodiless members).", "",
          "- **Spec approximation**: 1 + `if` + `case` (not `default`) + `for` + `foreach` + `while` + "
          "`catch` + `&&` + `||` + `??` + ternary `?` + switch-expression arms (the `_` arm excluded) + "
          "`when` clauses.",
          "- **Roslyn-calibrated approximation**: 1 + `if` + `case` + `default:` + `for` + `foreach` + "
          "`while` + `&&` + `||` + `??` + ternary `?` + `?.` + `?[` (probing Roslyn showed it counts every "
          "case label including default and conditional access, but not `catch`, `when`, switch-expression "
          "arms, `??=` or pattern `and`/`or`). Lambdas and local functions count toward their method in both.",
          "- Ternary vs nullable `?`: a `?` is a nullable marker when followed by `>` `,` `)` `]` `;` `=` `{` "
          "`=>`, by `[]`, or by an identifier that is followed by `=` `;` `,` `)` `=>` `{` `in`.", ""]
    rows = []
    for rule, label in (("cc_spec", "spec approximation"), ("cc_calibrated", "Roslyn-calibrated")):
        c = cx[rule]
        rows.append((label, c["exact_agreement"], c["within_1"], c["mean_abs_diff"], c["over_limit_approx"],
                     c["over_limit_roslyn"]))
    L += _t(rows, ["Rule", "Exact agreement", "Within 1", "Mean abs diff", f"> {th['complexity']} (approx)",
                   f"> {th['complexity']} (Roslyn)"]) + [""]
    dis = cx["cc_spec"]["biggest_disagreements"]
    if dis:
        L += ["Biggest disagreements, spec approximation vs Roslyn:", ""]
        L += _t([(f"`{x['name']}`", f"{_short(x['file'])}:{x['line']}", x["roslyn"], x["approx"]) for x in dis],
                ["Unit", "Where", "Roslyn", "Spec approx"]) + [""]

    # ---- warnings
    w = d["compiler_warnings"]
    L += ["## Compiler warnings in our code", ""]
    if w is None:
        L += ["Analyzer build not available.", ""]
    else:
        L += [f"{w['total']} warnings. csproj NoWarn (kept, as Unity's compiler uses it): "
              f"{', '.join(d['csproj_nowarn_suppressed'] or [])} - those are not reported.", ""]
        L += _t(sorted(w["by_code"].items()), ["Code", "Count"]) + [""]
        L += _t([(_short(f), ", ".join(f"{k}x{v}" for k, v in c.items())) for f, c in w["by_file"].items()],
                ["File", "Warnings"]) + [""]
        L += _t([(f"{_short(x['file'])}:{x['line']}", x["code"], x["message"]) for x in w["list"]],
                ["Where", "Code", "Message"]) + [""]

    # ---- unused
    u = d["unused"]
    L += ["## Unused private members", "",
          "Analyzer = IDE0051 (never used), IDE0052 (assigned, never read), CA1823 (unused field); "
          f"{u['analyzer_raw_members']} members flagged raw, minus Unity/reflection entry points "
          f"{u['analyzer_excluded']}. Text fallback = private member whose identifier occurs in its "
          "type's files only as often as it is declared (same exclusions: Unity messages, UnityEvent "
          "m_MethodName in assets, names in string literals, reflection-entry attributes, overrides, "
          "explicit interface implementations). IDE0052 needs data flow the text check lacks.", "",
          f"Analyzer: {len(u['analyzer_unused'])}; text: {len(u['text_unused'])}; both: {u['both']}; "
          f"analyzer only: {u['analyzer_only']}; text only: {u['text_only']}.", ""]
    L += _t([(f"{_short(x['file'])}:{x['line']}", f"`{x['type']}.{x['member']}`", x["kind"], ", ".join(x["codes"]))
             for x in u["analyzer_unused"] + [y for y in u["text_unused"]
                                              if (y["file"], y["line"], y["member"]) not in
                                              {(z["file"], z["line"], z["member"]) for z in u["analyzer_unused"]}]],
            ["Where", "Member", "Kind", "Source"]) + [""]
    up = u.get("unused_parameters_ide0060") or []
    L += [f"Unused parameters (IDE0060, info, all methods incl. public): {len(up)}" +
          ("; " + "; ".join(f"{_short(x['file'])}:{x['line']} {x['message'].split(' (')[0]}" for x in up) if up else ""), ""]
    L += ["Serialized, never read in code ([SerializeField] fields Unity writes but no code reads; "
          "`in strings` = the name also appears as a string literal, e.g. written by an editor builder "
          "through SerializedObject.FindProperty):", ""]
    L += _t([(f"{_short(x['file'])}:{x['line']}", f"`{x['type']}.{x['member']}`", ", ".join(x["codes"]),
              "in strings" if x["named_in_strings"] else "")
             for x in u["serialized_never_read"]], ["Where", "Field", "Source", "Note"]) + [""]

    # ---- duplication
    dd = d["duplication"]
    L += ["## Duplicate code", "",
          "Token-window hashing over lexed tokens (comments/whitespace, `using` directives and declaration "
          "attributes ignored). Exact = tokens verbatim; normalized = identifiers/numbers/strings replaced by "
          "placeholders (keywords and punctuation kept). Equal windows are extended to maximal clone pairs; "
          "self-overlapping pairs dropped; pairs with the same token sequence merged into groups. Duplicated "
          "lines = code lines covered by any clone location, once per file. Production (runtime + editor) and "
          "test code are scanned separately.", ""]
    rows = []
    for scope in ("production", "test"):
        for mode in ("exact", "normalized"):
            rows.append([scope, mode] + [f"{dup[f'{scope}/{mode}/W{W}']['groups']} / "
                                        f"{dup[f'{scope}/{mode}/W{W}']['duplicated_lines']}" for W in dd["windows"]])
    L += _t(rows, ["Scope", "Mode"] + [f"W={W} groups / lines" for W in dd["windows"]]) + [""]
    for key, n in (("production/exact", 15), ("production/normalized", 15), ("test/exact", 5), ("test/normalized", 10)):
        gl = dd["groups"][key]
        L += [f"### Largest groups, {key}, W={dd['detail_window']} ({len(gl)} groups)", ""]
        L += _t([(g["tokens"], g["lines"], "<br>".join(_short(x) for x in g["locations"]), f"`{g['snippet'][:90]}`")
                 for g in gl[:n]], ["Tokens", "Lines", "Locations", "Starts with"]) + [""]
        pr = (dd.get("periodic_regions") or {}).get(key) or []
        if pr:
            L += [f"Periodic regions (same-shaped rows repeating; not counted as clones): " +
                  ", ".join(_short(x) for x in pr[:12]) + (f" ... +{len(pr) - 12}" if len(pr) > 12 else ""), ""]
        pf = list(dd["per_file"][key].items())[:12]
        if pf:
            L += ["Duplicated lines per file (top 12): " + ", ".join(f"{_short(f)} {c}" for f, c in pf), ""]

    # ---- literals
    lit = d["literals"]
    L += ["## Magic numbers and string literals", "",
          "Tiers: **rule** = TimeDesk.Domain + Assets/Scripts root, Core, Shift, Timeline, Home, Endings, "
          "Investigation, Dialog, Characters; **presentation** = UI, Office, Visuals, DevTools; **editor**; "
          "**test**.", "",
          f"Numbers: allowed values {lit['allowed_numbers']} (by value, sign included). Not magic: const "
          "fields and local consts, enum values, attribute arguments; initializers of serialized fields "
          "(`serialized_default`, tunable knobs) and of other fields/properties (`field_initializer`, a named "
          "value) are counted separately; `new T[n]` sizes stay magic (flagged `array_size`). Numbers inside "
          "interpolation holes count.", "",
          "Strings: not counted as literals: const, attribute arguments (incl. [MenuItem] paths), empty "
          "strings, and log/exception text (arguments of Debug.Log*/Debug.Assert*/Assert.*/`new "
          "*Exception(...)`, counted as `log`). Field initializers bucketed as for numbers. An interpolated "
          "string is one literal.", ""]
    rows = []
    for tier in ("rule", "presentation", "editor", "test"):
        t = lit["tiers"].get(tier)
        if not t:
            continue
        n, s = t["numbers"], t["strings"]
        rows.append((tier, n.get("magic", 0), n.get("allowed", 0), n.get("const", 0), n.get("field_initializer", 0),
                     n.get("serialized_default", 0), s.get("literal", 0), s.get("log", 0), s.get("const", 0),
                     s.get("field_initializer", 0) + s.get("serialized_default", 0)))
    L += _t(rows, ["Tier", "Magic numbers", "Allowed", "Const", "Field init", "Serialized default",
                   "String literals", "Log/exception text", "Const strings", "String field init"]) + [""]
    for tier in ("rule", "presentation", "editor"):
        t = lit["tiers"].get(tier)
        if t and t["top_files"]:
            L += [f"Top files, {tier} tier (magic numbers / string literals): " +
                  ", ".join(f"{_short(x['file'])} {x['magic']}/{x['strings']}" for x in t["top_files"][:10]), ""]
    L += [f"### Rule code: magic numbers ({len(lit['rule_magic_numbers'])})", ""]
    L += _t([(f"{_short(x['file'])}:{x['line']}", f"`{x['literal']}`", f"`{x['member']}`",
              "array size" if x["array_size"] else "") for x in lit["rule_magic_numbers"][:100]],
            ["Where", "Literal", "Member", "Note"]) + [""]
    rs = lit["rule_strings"]
    L += [f"### Rule code: string literals ({len(rs)}; first 80, full list in JSON)", ""]
    L += _t([(f"{_short(x['file'])}:{x['line']}", f"`{x['literal'][:70]}`", f"`{x['member']}`") for x in rs[:80]],
            ["Where", "Literal", "Member"]) + [""]
    L += [f"Rule code also has {len(lit['rule_named_field_initializers'])} literals in named field initializers "
          f"and {len(lit['rule_serialized_defaults'])} serialized defaults (JSON).", ""]

    # ---- per-frame
    pf = d["per_frame"]
    L += ["## Per-frame paths", "",
          "Roots: Update/LateUpdate/FixedUpdate/OnGUI of runtime types (Assets/Scripts), plus method groups "
          "registered with `+=` on EditorApplication.update, Canvas.willRenderCanvases, "
          "RenderPipelineManager.*, Application.onBeforeRender, Camera.onPre*/onPostRender. Reachability "
          "follows calls resolved by simple name to methods of the same (partial) type, and bare uses of the "
          "type's properties to their accessors; calls into other types, base classes, events, coroutines "
          "and callbacks are NOT followed. Sites are reported once, with the first root that reaches them "
          "(`roots` in JSON lists all).", "",
          f"{len(pf['roots'])} roots: " + ", ".join(f"`{r['name']}`" for r in pf["roots"]), ""]
    if pf["registration_notes"]:
        L += ["Registration notes: " + "; ".join(pf["registration_notes"]), ""]
    L += [f"### Find / GetComponent ({len(pf['find_sites'])}): {pf['find_by_kind']}", ""]
    L += _t([(f"{_short(x['file'])}:{x['line']}", x["kind"], " <- ".join(reversed(x["chain"])), f"`{x['code'][:80]}`")
             for x in pf["find_sites"]], ["Where", "Kind", "Chain (callee <- root)", "Code"]) + [""]
    L += [f"### Allocations ({len(pf['alloc_sites'])}): {pf['alloc_by_kind']}", "",
          "Kinds: `new` of reference types (Unity/BCL value types and structs declared in our code excluded), "
          "arrays, string interpolation/concatenation/Format/Join/Concat, ToString(), lambdas and anonymous "
          "methods (delegate/closure), LINQ calls (files with `using System.Linq`; Contains/Reverse skipped as "
          "ambiguous), ToList/ToArray, foreach over LINQ. Boxing and params arrays are not detected.", ""]
    L += _t([(f"{_short(x['file'])}:{x['line']}", x["kind"], " <- ".join(reversed(x["chain"])), f"`{x['code'][:80]}`")
             for x in pf["alloc_sites"][:150]], ["Where", "Kind", "Chain (callee <- root)", "Code"]) + [""]

    # ---- statics
    st = d["static_mutable_state"]
    L += [f"## Static mutable state ({len(st)}): {d['static_mutable_by_kind']}", "",
          "Static non-const non-readonly fields; static readonly fields of a mutable collection/array/"
          "StringBuilder type; static auto-properties with a set/init accessor; static events.", ""]
    L += _t([(f"{_short(x['file'])}:{x['line']}", f"`{x['type']}.{x['member']}`", f"`{x['declared_type']}`",
              x["kind"], x["access"], x["assembly"], x["role"]) for x in st],
            ["Where", "Member", "Type", "Kind", "Access", "Assembly", "Role"]) + [""]

    # ---- singletons
    sg = d["singletons"]
    L += [f"## Singleton access ({h['singleton_access_sites']} sites)", "",
          "`X.Instance` / `X.HasInstance` for any identifier X, and `X.Current` where X declares a static "
          "Current. Types declaring such static accessors: " +
          ", ".join(f"`{k}` ({', '.join(sorted({x['member'] for x in v}))})" for k, v in sg["definers"].items()), ""]
    L += _t([(f"`{k}`", v["sites"], len(v["files"]), v["by_role"], v["accessors"]) for k, v in sg["by_type"].items()],
            ["Type", "Sites", "Files", "By role", "Accessors"]) + [""]
    L += [f"FindObject*-style searches in runtime code ({len(sg['find_object_searches_runtime'])}), by type: "
          f"{sg['find_object_by_type']}", ""]
    L += _t([(f"{_short(x['file'])}:{x['line']}", x["call"], f"`{x['type']}`") for x in sg["find_object_searches_runtime"]],
            ["Where", "Call", "Type"]) + [""]
    return "\n".join(L)
