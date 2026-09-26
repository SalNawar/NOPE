"""Builds the code with the .NET SDK's own Roslyn analyzers and records what they say.

What it collects (written to <out>/analyzer.json, sorted and path-relative):
  * compiler warnings (CSxxxx) in our source set, by code and file;
  * exact cyclomatic complexity per method from CA1502 (threshold forced low
    through CodeMetricsConfig.txt so every method is reported);
  * unused private members: IDE0051 (never used), IDE0052 (assigned, never
    read), CA1823 (unused field); IDE0060 unused parameters as info;
  * class coupling from CA1506 (types and methods).

How: the Unity-style csproj files produced by the compile-check script
(old-style, LangVersion 9, NoWarn 0169,0649, Unity DLLs by absolute path,
ProjectReferences between the five projects) are copied into <out>/ab/, given
their own output/intermediate folders, and extended with
  * <Analyzer> items for the SDK's NetAnalyzers + CodeStyle DLLs,
  * a global analyzer config (is_global) that switches every analyzer
    diagnostic off, then enables CA1502, CA1506, CA1823, IDE0051, IDE0052,
    IDE0060 as warnings (compiler CS warnings keep their normal level),
  * CodeMetricsConfig.txt as AdditionalFiles.
Then `dotnet build -t:Rebuild` runs on Assembly-CSharp-Editor.csproj and
TimeDeskEditMode.csproj (which pull in the other three) with a warnings-only
file logger. Diagnostics are de-duplicated (the same file is compiled by one
project, but shared projects are rebuilt by both builds), filtered to the
source set (others counted as `dropped`) and written without absolute paths.

Limits: the csproj NoWarn (0169 field never used, 0649 field never assigned)
is kept because Unity's compiler uses it; that suppression is recorded in the
output. Complexity is Roslyn's own metric (CodeAnalysisMetricData).
"""
from __future__ import annotations

import argparse
import re
import subprocess
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import sources  # noqa: E402

PROJECTS = ["Assembly-CSharp", "Assembly-CSharp-Editor", "TimeDesk.Domain",
            "TimeDesk.Visuals", "TimeDeskEditMode"]
BUILD_ROOTS = ["Assembly-CSharp-Editor", "TimeDeskEditMode"]
ENABLED = {"CA1502": "warning", "CA1506": "warning", "CA1823": "warning",
           "IDE0051": "warning", "IDE0052": "warning", "IDE0060": "warning"}
CSPROJ_NOWARN = ["CS0169", "CS0649"]

GLOBALCONFIG = """is_global = true
global_level = 100
dotnet_analyzer_diagnostic.severity = none
{rules}
# IDE0060: report unused parameters on all methods, not only private ones
dotnet_code_quality_unused_parameters = all
"""
# `CA1502: 0` reports every method whose complexity is > 0, i.e. all of them;
# CA1506 likewise reports every type and member with its coupling.
METRICS_CONFIG = "CA1502: 0\nCA1506: 0\n"

DIAG_RE = re.compile(
    r"^(?P<file>[A-Za-z]:[^()\r\n]*?|[^()\r\n]*?)\((?P<line>\d+),(?P<col>\d+)(?:,\d+,\d+)?\): "
    r"(?P<sev>warning|error|info) (?P<code>[A-Z]+\d+): (?P<msg>.*?)(?: \[(?P<proj>[^\]]+)\])?\s*$")
CC_RE = re.compile(r"'(?P<sym>.+)' has a cyclomatic complexity of '?(?P<cc>\d+)'?")
CPL_RE = re.compile(r"'(?P<sym>.+)' is coupled with '?(?P<n>\d+)'? different types from '?(?P<ns>\d+)'? different namespaces")


def sdk_dir() -> Path:
    out = subprocess.run(["dotnet", "--list-sdks"], capture_output=True, text=True, check=True).stdout
    best = None
    for line in out.splitlines():
        m = re.match(r"(\S+) \[(.+)\]", line.strip())
        if m:
            ver, root = m.group(1), m.group(2)
            key = tuple(int(x) if x.isdigit() else 0 for x in re.split(r"[.-]", ver))
            if best is None or key > best[0]:
                best = (key, Path(root) / ver, ver)
    if best is None:
        raise SystemExit("no .NET SDK found")
    return best[1]


def analyzer_dlls(sdk: Path) -> list[Path]:
    base = sdk / "Sdks" / "Microsoft.NET.Sdk"
    dlls = [base / "analyzers" / "Microsoft.CodeAnalysis.NetAnalyzers.dll",
            base / "analyzers" / "Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll",
            base / "codestyle" / "cs" / "Microsoft.CodeAnalysis.CodeStyle.dll",
            base / "codestyle" / "cs" / "Microsoft.CodeAnalysis.CSharp.CodeStyle.dll"]
    for d in dlls:
        if not d.exists():
            raise SystemExit(f"missing analyzer {d}")
    return dlls


def prepare(cc_dir: Path, ab: Path, repo: Path, dlls: list[Path]) -> None:
    ab.mkdir(parents=True, exist_ok=True)
    rules = "\n".join(f"dotnet_diagnostic.{k}.severity = {v}" for k, v in sorted(ENABLED.items()))
    (ab / "audit.globalconfig").write_text(GLOBALCONFIG.format(rules=rules), encoding="utf-8")
    (ab / "CodeMetricsConfig.txt").write_text(METRICS_CONFIG, encoding="utf-8")
    tracked = set(sources.tracked(repo))
    root = str(repo).replace("\\", "/").rstrip("/").lower() + "/"
    skipped = []

    def keep(m):
        """Drops untracked files inside the repo (local automation, WIP) so the
        build depends only on the commit plus third-party code."""
        path = m.group(1).replace("\\", "/")
        if not path.lower().startswith(root):
            return m.group(0)
        rel = path[len(root):]
        if rel in tracked:
            return m.group(0)
        skipped.append(rel)
        return ""

    for name in PROJECTS:
        src = cc_dir / f"{name}.csproj"
        text = src.read_text(encoding="utf-8-sig")
        if str(repo).lower() not in text.lower():
            raise SystemExit(f"{src} does not compile {repo}; run the compile-check script first")
        text = re.sub(r'\s*<Compile Include="([^"]*)" />', keep, text)
        outdir = f"<OutputPath>{ab / 'bin' / name}\\</OutputPath>"
        text = re.sub(r"<OutputPath>[^<]*</OutputPath>", lambda _m: outdir, text)
        extra = (
            "  <PropertyGroup>\n"
            f"    <BaseIntermediateOutputPath>{ab / 'obj' / name}\\</BaseIntermediateOutputPath>\n"
            f"    <IntermediateOutputPath>{ab / 'obj' / name}\\Debug\\</IntermediateOutputPath>\n"
            "    <RunAnalyzers>true</RunAnalyzers>\n"
            "    <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>\n"
            "    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>\n"
            "    <GenerateMSBuildEditorConfigFile>true</GenerateMSBuildEditorConfigFile>\n"
            "  </PropertyGroup>\n"
            "  <ItemGroup>\n"
            + "".join(f'    <Analyzer Include="{d}" />\n' for d in dlls)
            + f'    <EditorConfigFiles Include="{ab / "audit.globalconfig"}" />\n'
            + f'    <AdditionalFiles Include="{ab / "CodeMetricsConfig.txt"}" />\n'
            "  </ItemGroup>\n")
        anchor = '  <Import Project="$(MSBuildToolsPath)\\Microsoft.CSharp.targets" />'
        if anchor not in text:
            raise SystemExit(f"{src}: Microsoft.CSharp.targets import not found")
        text = text.replace(anchor, extra + anchor, 1)
        (ab / f"{name}.csproj").write_text(text, encoding="utf-8")
    if skipped:
        print(f"not compiled (untracked in the repo): {', '.join(sorted(skipped))}")


def build(ab: Path) -> list[str]:
    lines = []
    for name in BUILD_ROOTS:
        log = ab / f"build_{name}.log"
        cmd = ["dotnet", "build", str(ab / f"{name}.csproj"), "-t:Rebuild", "-nologo",
               "-v:q", "-clp:ErrorsOnly;NoSummary", "-nodeReuse:false",
               f"-flp:v=q;WarningsOnly;logfile={log}"]
        r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
        if r.returncode != 0:
            sys.stderr.write(r.stdout[-4000:] + r.stderr[-2000:])
            raise SystemExit(f"build of {name} failed (exit {r.returncode})")
        lines += log.read_text(encoding="utf-8-sig", errors="replace").splitlines()
    return lines


def parse_diagnostics(lines, repo: Path, source_paths: set[str]):
    seen = set()
    ours, dropped = [], {}
    root = str(repo).replace("\\", "/").rstrip("/").lower() + "/"
    for line in lines:
        m = DIAG_RE.match(line.strip())
        if not m:
            continue
        f = m.group("file").replace("\\", "/")
        rel = f[len(root):] if f.lower().startswith(root) else f
        proj = Path(m.group("proj") or "").stem
        msg = re.sub(r"\s*\(https?://[^)]*\)", "", m.group("msg")).strip()   # drop help links
        d = (rel, int(m.group("line")), int(m.group("col")), m.group("code"), msg)
        if d in seen:
            continue
        seen.add(d)
        if rel not in source_paths:
            if rel == f:   # outside the repo (e.g. MSBuild targets): keep only the file name
                key = "(outside repo) " + rel.rsplit("/", 1)[-1]
            else:
                key = rel.rsplit("/", 1)[0] if "/" in rel else rel
            dropped.setdefault(m.group("code"), {}).setdefault(key, 0)
            dropped[m.group("code")][key] += 1
            continue
        ours.append({"file": rel, "line": d[1], "col": d[2], "code": d[3], "severity": m.group("sev"),
                     "message": d[4], "project": proj})
    ours.sort(key=lambda x: (x["file"], x["line"], x["col"], x["code"], x["message"]))
    return ours, dropped


def summarise(diags, dropped, sdk_ver: str, head: str):
    compiler = [d for d in diags if d["code"].startswith("CS")]
    by_code, by_file = {}, {}
    for d in compiler:
        by_code[d["code"]] = by_code.get(d["code"], 0) + 1
        by_file.setdefault(d["file"], {}).setdefault(d["code"], 0)
        by_file[d["file"]][d["code"]] += 1
    complexity = []
    for d in diags:
        if d["code"] == "CA1502":
            m = CC_RE.search(d["message"])
            if m:
                complexity.append({"file": d["file"], "line": d["line"], "symbol": m.group("sym"),
                                   "complexity": int(m.group("cc"))})
    coupling = []
    for d in diags:
        if d["code"] == "CA1506":
            m = CPL_RE.search(d["message"])
            if m:
                coupling.append({"file": d["file"], "line": d["line"], "symbol": m.group("sym"),
                                 "types": int(m.group("n")), "namespaces": int(m.group("ns"))})
    unused = [{"file": d["file"], "line": d["line"], "code": d["code"], "message": d["message"]}
              for d in diags if d["code"] in ("IDE0051", "IDE0052", "CA1823")]
    unused_params = [{"file": d["file"], "line": d["line"], "message": d["message"]}
                     for d in diags if d["code"] == "IDE0060"]
    other = [d for d in diags if not d["code"].startswith("CS") and d["code"] not in ENABLED]
    counts = {}
    for d in diags:
        counts[d["code"]] = counts.get(d["code"], 0) + 1
    return {
        "tool": sources.TOOL_VERSION,
        "commit": head,
        "sdk": sdk_ver,
        "enabled_analyzers": sorted(ENABLED),
        "csproj_nowarn_suppressed": CSPROJ_NOWARN,
        "diagnostic_counts": dict(sorted(counts.items())),
        "compiler_warnings": {"total": len(compiler), "by_code": dict(sorted(by_code.items())),
                              "by_file": {k: dict(sorted(v.items())) for k, v in sorted(by_file.items())},
                              "list": compiler},
        "complexity": sorted(complexity, key=lambda x: (x["file"], x["line"], x["symbol"])),
        "coupling": sorted(coupling, key=lambda x: (x["file"], x["line"], x["symbol"])),
        "unused_members": unused,
        "unused_parameters": unused_params,
        "other_analyzer_diagnostics": other,
        "dropped_outside_source_set": {k: dict(sorted(v.items())) for k, v in sorted(dropped.items())},
    }


def main(argv=None):
    here = Path(__file__).resolve().parent
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--repo", type=Path, default=here.parent.parent)
    ap.add_argument("--out", type=Path, default=None)
    ap.add_argument("--cc-dir", type=Path, required=False,
                    help="folder holding the compile-check csproj files (…/cc_art)")
    ap.add_argument("--reuse-logs", action="store_true",
                    help="parse existing <out>/ab/build_*.log instead of rebuilding")
    a = ap.parse_args(argv)
    repo = a.repo.resolve()
    out = (a.out or sources.default_out(repo)).resolve()
    out.mkdir(parents=True, exist_ok=True)
    ab = out / "ab"
    sdk = sdk_dir()
    if a.reuse_logs:
        lines = []
        for name in BUILD_ROOTS:
            lines += (ab / f"build_{name}.log").read_text(encoding="utf-8-sig", errors="replace").splitlines()
    else:
        if a.cc_dir is None:
            raise SystemExit("--cc-dir is required (folder with the compile-check csproj files)")
        prepare(a.cc_dir.resolve(), ab, repo, analyzer_dlls(sdk))
        lines = build(ab)
    srcs = sources.load_sources(repo)
    diags, dropped = parse_diagnostics(lines, repo, {s.path for s in srcs})
    data = summarise(diags, dropped, sdk.name, sources.git_head(repo))
    sources.write_json(out / "analyzer.json", data)
    print(f"analyzer.json: {len(diags)} diagnostics in source set; "
          f"{data['compiler_warnings']['total']} compiler warnings; "
          f"{len(data['complexity'])} CA1502 methods; {len(data['unused_members'])} unused-member hits")
    return 0


if __name__ == "__main__":
    sys.exit(main())
