"""Type inventory of the audit source set -> <out>/inventory.json + inventory.md.

For every type (partial parts merged): full name, kind, modifiers, bases,
assembly, owner, file spans, total lines (sum of the parts' spans, nested
types included) and code lines (non-blank, non-comment lines in those spans),
member counts, the public surface (public/protected members with
signatures) and, for Unity objects, the serialized fields.

Callers are a token scan, not semantic resolution: the identifier tokens
(outside comments/strings, inside interpolation holes) of every OTHER source
file are searched for the type's simple name. Same-named types collide (the
count is shared; `name_shared_with` lists them). `same_file_refs` counts
mentions in the type's own files outside its own spans (e.g. a helper class
used by its neighbour).

Asset references: Unity serializes a MonoBehaviour/ScriptableObject by the
script file's .meta guid (`m_Script: {fileID: 11500000, guid: ...}`), and only
the class named like the file can be attached. So `asset_refs` = number of
tracked .unity/.prefab/.asset files whose m_Script lines carry that guid, for
the file's main type; other types in the file get 0.

Dead-type candidates = no code callers in other files, no same-file mentions
and no asset refs. Entry points that Unity/NUnit call by reflection ([Test],
[MenuItem], [InitializeOnLoad], ...) are annotated so they can be discounted.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import cslex  # noqa: E402
import sources  # noqa: E402
from model import ENTRY_ATTRIBUTES, Model, base_name  # noqa: E402

PUBLIC = ("public", "protected", "protected internal")
# Base types whose subclasses Unity instantiates and calls by itself.
ENTRY_BASES = {"AssetPostprocessor", "Editor", "EditorWindow", "ScriptableWizard", "PropertyDrawer",
               "DecoratorDrawer", "IPreprocessBuildWithReport", "IPostprocessBuildWithReport",
               "IProcessSceneWithReport", "AssetModificationProcessor"}


def type_record(model: Model, td, assets) -> dict:
    parts = sorted(td.parts, key=lambda p: (p.file, p.line))
    files = sorted({p.file for p in parts})
    total = sum(p.end_line - p.line + 1 for p in parts)
    code = sum(model.code_lines_in(p.file, p.line, p.end_line) for p in parts)
    kinds = {}
    for m in td.members:
        kinds[m.kind] = kinds.get(m.kind, 0) + 1
    in_interface = td.kind == "interface"
    surface = []
    for m in td.members:
        acc = m.effective_access()
        if acc in PUBLIC or in_interface:
            surface.append({"kind": m.kind, "name": m.name, "access": acc, "line": m.name_line,
                            "file": m.file, "signature": m.signature})
    family = model.unity_family(td)
    serialized = []
    if family:
        for m in model.serialized_fields(td):
            serialized.append({"name": m.name, "type": m.type_text, "line": m.name_line, "file": m.file,
                               "attributes": sorted({a.name for a in m.attributes})})
    # callers
    name = td.name
    callers = {}
    for path, counts in model.ident_counts.items():
        if path in files:
            continue
        n = counts.get(name, 0)
        if n:
            callers[path] = n
    same_file = 0
    for f in files:
        spans = [(p.lo, p.hi) for p in parts if p.file == f]
        for i, t in enumerate(model.files[f].tokens):
            if any(lo <= i < hi for lo, hi in spans):
                continue
            if t.kind == "id" and t.text == name:
                same_file += 1
            elif t.holes:
                same_file += sum(1 for h in t.holes for x in cslex.flatten(h) if x.kind == "id" and x.text == name)
    prod_callers = sorted(p for p in callers if model.by_path[p].role != "test")
    test_callers = sorted(p for p in callers if model.by_path[p].role == "test")
    # asset refs (main type of the file only)
    asset_refs = []
    main_file = None
    if td.outer is None:
        for f in files:
            if f.rsplit("/", 1)[-1][:-3] == td.name:
                main_file = f
        if main_file and model.by_path[main_file].guid:
            asset_refs = sorted(assets["script_refs"].get(model.by_path[main_file].guid, {}))
    entry = sorted({a.name for a in td.attributes if a.name in ENTRY_ATTRIBUTES} |
                   {a.name for m in td.members for a in m.attributes if a.name in ENTRY_ATTRIBUTES} |
                   {"base " + base_name(b) for b in td.bases if base_name(b) in ENTRY_BASES})
    shared = sorted(o.display_name for o in model.types_by_simple.get(name, []) if o is not td)
    return {
        "name": td.display_name,
        "key": td.key,
        "simple_name": name,
        "kind": td.kind,
        "access": _type_access(td),
        "modifiers": td.modifiers,
        "bases": td.bases,
        "unity_family": family,
        "assembly": model.assembly(td),
        "owner": model.owner(td),
        "role": model.role(td),
        "outer": td.outer.display_name if td.outer else None,
        "parts": [{"file": p.file, "line": p.line, "end_line": p.end_line} for p in parts],
        "lines": total,
        "code_lines": code,
        "member_counts": dict(sorted(kinds.items())),
        "public_surface": surface,
        "serialized_fields": serialized,
        "callers": dict(sorted(callers.items())),
        "caller_files": len(callers),
        "prod_caller_files": len(prod_callers),
        "test_caller_files": len(test_callers),
        "same_file_refs": same_file,
        "asset_refs": len(asset_refs),
        "asset_ref_files": asset_refs,
        "script_file": main_file,
        "entry_attributes": entry,
        "name_shared_with": shared,
    }


def _type_access(td) -> str:
    for a in ("public", "internal", "protected", "private"):
        if a in td.modifiers:
            return a
    return "private" if td.outer is not None else "internal"


def build(model: Model, assets) -> dict:
    types = [type_record(model, td, assets) for td in model.types]
    types.sort(key=lambda r: (r["assembly"], r["parts"][0]["file"], r["parts"][0]["line"], r["name"]))
    dead = [t for t in types if t["caller_files"] == 0 and t["same_file_refs"] == 0 and t["asset_refs"] == 0]
    return {
        "tool": sources.TOOL_VERSION,
        "commit": model.head,
        "files": len(model.sources),
        "types_total": len(types),
        "asset_scan": {"text_assets_scanned": assets["scanned"], "binary_assets_skipped": assets["skipped_binary"]},
        "types": types,
        "dead_candidates": [t["name"] for t in dead],
    }


def render_md(inv: dict) -> str:
    out = [f"# Type inventory ({inv['commit'][:10]})", "",
           f"Tool: {inv['tool']}. {inv['files']} source files, {inv['types_total']} types "
           f"(partial parts merged). Callers = other source files whose identifier tokens mention the "
           f"type's simple name; asset refs = tracked scenes/prefabs/assets whose `m_Script` carries the "
           f"script's meta guid (text assets scanned: {inv['asset_scan']['text_assets_scanned']}, binary "
           f"skipped: {inv['asset_scan']['binary_assets_skipped']}).", ""]
    by_asm = {}
    for t in inv["types"]:
        by_asm.setdefault(t["assembly"], []).append(t)
    for asm in sorted(by_asm):
        rows = by_asm[asm]
        out += [f"## {asm} ({len(rows)} types)", "",
                "| Type | Kind | File | Lines | Code | Public members | Code callers (prod/test) | Asset refs |",
                "|---|---|---|---:|---:|---:|---|---:|"]
        for t in rows:
            files = ", ".join(f"{p['file'].replace('Assets/', '')}:{p['line']}" for p in t["parts"])
            owner = " (codex-light-touch)" if t["owner"] != "ours" else ""
            out.append(f"| `{t['name']}`{owner} | {t['kind']} | {files} | {t['lines']} | {t['code_lines']} | "
                       f"{len(t['public_surface'])} | {t['caller_files']} ({t['prod_caller_files']}/"
                       f"{t['test_caller_files']}) | {t['asset_refs']} |")
        out.append("")
    dead = [t for t in inv["types"] if t["name"] in set(inv["dead_candidates"])]
    out += ["## Candidate dead types", "",
            "No code callers in other files, no mentions elsewhere in their own file, no asset refs. "
            "Entry points called by reflection are annotated; discount them.", "",
            "| Type | Kind | File | Lines | Annotation |", "|---|---|---|---:|---|"]
    for t in dead:
        note = ", ".join(a if a.startswith("base ") else f"[{a}]" for a in t["entry_attributes"])
        if not note and t["role"] == "test":
            note = "test code"
        if t["unity_family"] and not t["asset_refs"]:
            note = (note + "; " if note else "") + f"{t['unity_family']} with no asset refs"
        out.append(f"| `{t['name']}` | {t['kind']} | {t['parts'][0]['file']}:{t['parts'][0]['line']} | "
                   f"{t['lines']} | {note} |")
    out.append("")
    return "\n".join(out)


def main(argv=None):
    here = Path(__file__).resolve().parent
    ap = argparse.ArgumentParser(description="Type inventory of the audit source set")
    ap.add_argument("--repo", type=Path, default=here.parent.parent)
    ap.add_argument("--out", type=Path, default=None)
    a = ap.parse_args(argv)
    repo = a.repo.resolve()
    out = (a.out or sources.default_out(repo)).resolve()
    out.mkdir(parents=True, exist_ok=True)
    model = Model(repo)
    assets = sources.scan_assets(repo)
    inv = build(model, assets)
    sources.write_json(out / "inventory.json", inv)
    sources.write_text(out / "inventory.md", render_md(inv))
    print(f"inventory: {inv['types_total']} types, {len(inv['dead_candidates'])} dead candidates")
    return 0


if __name__ == "__main__":
    sys.exit(main())
