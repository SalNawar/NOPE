"""The audit's source set, defined once and shared by every audit tool.

Source set = git-tracked `*.cs` files under Assets/Scripts, Assets/Editor and
Assets/Tests, minus:
  * Assets/Editor/OfficeArt/**  (another team's art tooling)
  * local automation files named `_TimeDesk*` / `_ClaudeJob*` (normally untracked)
Third-party code (Assets/80s_Office, Packages, TextMesh Pro, art packs) lives
outside those folders, so it never enters the set.

Every file gets:
  * `assembly`: the `name` of the nearest ancestor folder's .asmdef, otherwise
    `Assembly-CSharp-Editor` when the path has an `/Editor/` segment, else
    `Assembly-CSharp` (Unity's own rule).
  * `owner`: "codex-light-touch" for the three files another agent maintains,
    "ours" for everything else.
  * `role`: "test" (Assets/Tests), "editor" (Editor assemblies) or "runtime".
  * `tier`: the magic-number/string tier - "rule" (TimeDesk.Domain plus the
    runtime folders that hold game rules), "presentation" (UI, Office, Visuals,
    DevTools runtime), "editor", "test".
  * `guid`: the script's .meta guid when a tracked .cs.meta exists.

All paths in outputs are repo-relative with forward slashes.
"""
from __future__ import annotations

import json
import re
import subprocess
from dataclasses import dataclass
from pathlib import Path

TOOL_VERSION = "audit-static 1.0"

INCLUDE_ROOTS = ("Assets/Scripts/", "Assets/Editor/", "Assets/Tests/")
EXCLUDE_PREFIXES = ("Assets/Editor/OfficeArt/",)
EXCLUDE_NAME_PREFIXES = ("_TimeDesk", "_ClaudeJob")
LIGHT_TOUCH = {
    "Assets/Scripts/Office/OfficeHallCrowdPalette.cs",
    "Assets/Scripts/Office/OfficeTrafficVehicle.cs",
    "Assets/Scripts/Office/OfficeWindowGlass.cs",
}
# Runtime folders (under Assets/Scripts) whose code holds game rules.
RULE_FOLDERS = {"", "Core", "Shift", "Timeline", "Home", "Endings",
                "Investigation", "Dialog", "Characters"}
PRESENTATION_FOLDERS = {"UI", "Office", "Visuals", "DevTools"}

ASSET_EXTS = (".unity", ".prefab", ".asset")


@dataclass
class SourceFile:
    path: str          # repo-relative, forward slashes
    abspath: Path
    assembly: str
    owner: str
    role: str          # runtime | editor | test
    tier: str          # rule | presentation | editor | test
    guid: str | None

    def read(self) -> str:
        return read_text(self.abspath)


def read_text(p: Path) -> str:
    """Reads a source file as text: strips a UTF-8 BOM, normalises CRLF/CR to LF."""
    data = p.read_bytes()
    if data.startswith(b"\xef\xbb\xbf"):
        data = data[3:]
    text = data.decode("utf-8", errors="replace")
    return text.replace("\r\n", "\n").replace("\r", "\n")


def git(repo: Path, *args: str) -> str:
    r = subprocess.run(["git", "-C", str(repo), *args], capture_output=True,
                       text=True, encoding="utf-8", check=True)
    return r.stdout


def git_head(repo: Path) -> str:
    return git(repo, "rev-parse", "HEAD").strip()


def tracked(repo: Path) -> list[str]:
    return sorted(git(repo, "ls-files", "-z").split("\0")[:-1])


def asmdefs(repo: Path, files: list[str]) -> dict[str, str]:
    """Folder (repo-relative, trailing slash) -> assembly name, from tracked .asmdef files."""
    out = {}
    for f in files:
        if f.startswith("Assets/") and f.endswith(".asmdef"):
            name = json.loads(read_text(repo / f))["name"]
            out[f.rsplit("/", 1)[0] + "/"] = name
    return out


def assembly_of(path: str, asm_folders: dict[str, str]) -> str:
    best = ""
    for folder in asm_folders:
        if path.startswith(folder) and len(folder) > len(best):
            best = folder
    if best:
        return asm_folders[best]
    return "Assembly-CSharp-Editor" if "/Editor/" in "/" + path else "Assembly-CSharp"


def _tier(path: str, role: str, assembly: str) -> str:
    if role in ("test", "editor"):
        return role
    if assembly == "TimeDesk.Domain":
        return "rule"
    rest = path[len("Assets/Scripts/"):]
    folder = rest.split("/", 1)[0] if "/" in rest else ""
    if folder in RULE_FOLDERS:
        return "rule"
    return "presentation"


def meta_guid(repo: Path, path: str, tracked_set: set[str]) -> str | None:
    meta = path + ".meta"
    if meta not in tracked_set:
        return None
    m = re.search(r"^guid:\s*([0-9a-f]{32})", read_text(repo / meta), re.M)
    return m.group(1) if m else None


def in_source_set(path: str) -> bool:
    if not path.endswith(".cs") or not path.startswith(INCLUDE_ROOTS):
        return False
    if path.startswith(EXCLUDE_PREFIXES):
        return False
    return not path.rsplit("/", 1)[-1].startswith(EXCLUDE_NAME_PREFIXES)


def load_sources(repo: Path) -> list[SourceFile]:
    repo = Path(repo)
    files = tracked(repo)
    tracked_set = set(files)
    asm = asmdefs(repo, files)
    out = []
    for f in files:
        if not in_source_set(f):
            continue
        assembly = assembly_of(f, asm)
        if f.startswith("Assets/Tests/"):
            role = "test"
        elif assembly == "Assembly-CSharp-Editor" or "/Editor/" in "/" + f:
            role = "editor"
        else:
            role = "runtime"
        out.append(SourceFile(
            path=f, abspath=repo / f, assembly=assembly,
            owner="codex-light-touch" if f in LIGHT_TOUCH else "ours",
            role=role, tier=_tier(f, role, assembly),
            guid=meta_guid(repo, f, tracked_set)))
    return out


def asset_files(repo: Path) -> list[str]:
    """Tracked scene/prefab/asset files under Assets/ (text or binary)."""
    return [f for f in tracked(Path(repo)) if f.startswith("Assets/") and f.endswith(ASSET_EXTS)]


_SCRIPT_RE = re.compile(rb"m_Script: \{fileID: -?\d+, guid: ([0-9a-f]{32})")
_METHOD_RE = re.compile(rb"m_MethodName: ([A-Za-z_][A-Za-z0-9_]*)")


def scan_assets(repo: Path) -> dict:
    """Scans tracked text-YAML assets once.

    Returns {"script_refs": {guid: {asset_path: count}},
             "method_names": {name: [asset_path, ...]},
             "scanned": n_text, "skipped_binary": n_binary}.
    Binary assets (no `%YAML` header, e.g. LightingData) cannot hold m_Script
    YAML lines and are skipped without being read in full.
    """
    repo = Path(repo)
    refs: dict[str, dict[str, int]] = {}
    methods: dict[str, set[str]] = {}
    scanned = skipped = 0
    for f in asset_files(repo):
        p = repo / f
        with open(p, "rb") as fh:
            head = fh.read(5)
            if head != b"%YAML":
                skipped += 1
                continue
            data = head + fh.read()
        scanned += 1
        for m in _SCRIPT_RE.finditer(data):
            g = m.group(1).decode()
            refs.setdefault(g, {}).setdefault(f, 0)
            refs[g][f] += 1
        for m in _METHOD_RE.finditer(data):
            methods.setdefault(m.group(1).decode(), set()).add(f)
    return {"script_refs": refs,
            "method_names": {k: sorted(v) for k, v in sorted(methods.items())},
            "scanned": scanned, "skipped_binary": skipped}


def write_json(path: Path, data) -> None:
    """Deterministic JSON: sorted keys, LF, UTF-8, trailing newline."""
    text = json.dumps(data, indent=1, sort_keys=True, ensure_ascii=False)
    Path(path).write_bytes((text + "\n").encode("utf-8"))


def write_text(path: Path, text: str) -> None:
    Path(path).write_bytes(text.replace("\r\n", "\n").encode("utf-8"))


def default_out(repo: Path) -> Path:
    return Path(repo) / "tools" / "audit" / "out"
