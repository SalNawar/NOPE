"""Packs and diffs the audit's behaviour golden masters.

The Unity jobs in tools/audit/unity write one folder per run (see
docs/reviews/audit-baseline/README.md):
  static  (_TimeDeskAuditStatic.RunA/RunB): cases.txt, world_generate.txt,
          validator.txt, data_hashes.txt, scene_*_committed.txt,
          scene_*_rebuilt.txt, scenes_summary.txt, contract.txt
  play    (_TimeDeskAuditPlay.RunA/RunB):   play_transcript.txt,
          play_warnings.txt, saves/*.json
  profile (_TimeDeskAuditProfile.RunA/RunB): profile_summary.txt

  python tools/audit/golden.py pack --static S --play P --profile F [--profile F2] --out DIR
      writes the compact baseline: every deterministic file with LF line
      endings (gzip, mtime 0, when over 200 kB), MANIFEST.sha256 of those
      uncompressed contents, and the profile summaries (measurements, not
      byte-exact).
  python tools/audit/golden.py diff --baseline DIR [--static S] [--play P] [--profile F]
      compares new runs with the baseline: every deterministic file must be
      identical, line endings aside (a unified diff of the first differences
      is printed);
      each scene's rebuild must equal the baseline's committed dump (order
      ignored); the profile may not allocate more per frame (median PlayerLoop
      GC per window), may not show a new allocation site in our assemblies,
      and no scene load may be slower than the baseline by more than 25%.
      Exit 1 on any difference; --allow NAME[,NAME] accepts named files
      (an intended, documented change).
"""
from __future__ import annotations

import argparse
import difflib
import gzip
import hashlib
import io
import re
import sys
from pathlib import Path

GZIP_OVER = 200_000

# baseline file -> (run kind, file in that run's folder)
DETERMINISTIC = {
    "cases.txt": ("static", "cases.txt"),
    "world_generate.txt": ("static", "world_generate.txt"),
    "validator.txt": ("static", "validator.txt"),
    "data_hashes.txt": ("static", "data_hashes.txt"),
    "scene_OfficeGameplay.txt": ("static", "scene_OfficeGameplay_committed.txt"),
    "scene_HomeScene.txt": ("static", "scene_HomeScene_committed.txt"),
    "scene_TitleScene.txt": ("static", "scene_TitleScene_committed.txt"),
    "scenes_summary.txt": ("static", "scenes_summary.txt"),
    "contract.txt": ("static", "contract.txt"),
    "play_transcript.txt": ("play", "play_transcript.txt"),
    "play_warnings.txt": ("play", "play_warnings.txt"),
}
REBUILDS = {
    "scene_OfficeGameplay.txt": "scene_OfficeGameplay_rebuilt.txt",
    "scene_HomeScene.txt": "scene_HomeScene_rebuilt.txt",
    "scene_TitleScene.txt": "scene_TitleScene_rebuilt.txt",
}
OUR_ASSEMBLIES = ("Assembly-CSharp.dll", "TimeDesk.Domain.dll", "TimeDesk.Visuals.dll")


def norm(data: bytes) -> bytes:
    """Line endings as LF: Unity writes CRLF on Windows, and git's autocrlf may convert either way."""
    return data.replace(b"\r\n", b"\n")


def read_bytes(path: Path) -> bytes:
    """A golden file's content (gunzipped), line endings normalised."""
    data = gzip.decompress(path.read_bytes()) if path.suffix == ".gz" else path.read_bytes()
    return norm(data)


def find(base: Path, name: str) -> Path | None:
    for p in (base / name, base / (name + ".gz")):
        if p.exists():
            return p
    return None


def write_member(out: Path, name: str, data: bytes) -> str:
    """Writes one file (gzip without a timestamp when large); returns the stored name."""
    if len(data) > GZIP_OVER:
        buf = io.BytesIO()
        with gzip.GzipFile(filename="", mode="wb", fileobj=buf, mtime=0, compresslevel=9) as gz:
            gz.write(data)
        (out / (name + ".gz")).write_bytes(buf.getvalue())
        return name + ".gz"
    (out / name).write_bytes(data)
    return name


def run_files(kind_dirs: dict) -> dict:
    """baseline name -> source path, for every deterministic file present in the given runs."""
    files = {}
    for name, (kind, src) in DETERMINISTIC.items():
        d = kind_dirs.get(kind)
        if d is not None and (d / src).exists():
            files[name] = d / src
    play = kind_dirs.get("play")
    if play is not None and (play / "saves").is_dir():
        for p in sorted((play / "saves").glob("*.json")):
            files[f"play_saves/{p.name}"] = p
    return files


def cmd_pack(a) -> int:
    out = Path(a.out)
    out.mkdir(parents=True, exist_ok=True)
    (out / "play_saves").mkdir(exist_ok=True)
    files = run_files({"static": a.static, "play": a.play})
    missing = [n for n in DETERMINISTIC if n not in files]
    if missing:
        print("missing:", ", ".join(missing), file=sys.stderr)
        return 1
    manifest = []
    for name in sorted(files):
        data = read_bytes(files[name])
        stored = write_member(out, name, data)
        manifest.append(f"{hashlib.sha256(data).hexdigest()}  {name}{'  (stored gzipped)' if stored != name else ''}")
    for i, prof in enumerate(a.profile or []):
        (out / f"profile_{chr(ord('A') + i)}.txt").write_bytes((prof / "profile_summary.txt").read_bytes())
    (out / "MANIFEST.sha256").write_text("\n".join(manifest) + "\n", encoding="utf-8", newline="\n")
    print(f"packed {len(files)} deterministic files + {len(a.profile or [])} profile summaries into {out}")
    return 0


def text_diff(old: bytes, new: bytes, name: str, limit: int) -> list[str]:
    ol = old.decode("utf-8", "replace").splitlines()
    nl = new.decode("utf-8", "replace").splitlines()
    lines = list(difflib.unified_diff(ol, nl, f"baseline/{name}", f"new/{name}", n=1, lineterm=""))
    return lines[:limit] + ([f"... ({len(lines) - limit} more diff lines)"] if len(lines) > limit else [])


def parse_profile(text: str) -> dict:
    """window -> {gc_median, sites{path: bytes/frame}}; plus loads {name: ms}."""
    windows, loads, cur = {}, {}, None
    for line in text.splitlines():
        m = re.match(r"WINDOW (.+?): \d+ profiled frames", line)
        if m:
            cur = windows.setdefault(m.group(1), {"gc_median": None, "sites": {}})
            continue
        m = re.match(r"LOAD (.+?): (\d+) ms", line)
        if m:
            loads[m.group(1)] = int(m.group(2))
            continue
        if cur is None:
            continue
        m = re.match(r"\s+PlayerLoop GC alloc per frame: median (\d+) B", line)
        if m:
            cur["gc_median"] = int(m.group(1))
            continue
        m = re.match(r"\s+site (\d+) B/frame avg, in (\d+)/(\d+) frames.*?: (PlayerLoop.*)$", line)
        if m:
            cur["sites"][m.group(4)] = int(m.group(1))
    return {"windows": windows, "loads": loads}


def cmd_diff(a) -> int:
    base = Path(a.baseline)
    allow = set(filter(None, (a.allow or "").split(",")))
    problems = []
    files = run_files({"static": a.static, "play": a.play})
    for name, src in sorted(files.items()):
        bp = find(base, name)
        if bp is None:
            problems.append((name, [f"{name}: not in the baseline"]))
            continue
        old, new = read_bytes(bp), read_bytes(src)
        if old != new:
            problems.append((name, [f"{name}: differs from the baseline"] + text_diff(old, new, name, a.lines)))
    if a.play is not None:
        base_saves = {p.name.removesuffix(".gz") for p in (base / "play_saves").glob("*")}
        new_saves = {p.name for p in (a.play / "saves").glob("*.json")}
        for gone in sorted(base_saves - new_saves):
            problems.append((f"play_saves/{gone}", [f"play_saves/{gone}: missing from the new run"]))
    if a.static is not None:
        for name, rebuilt in REBUILDS.items():
            src = a.static / rebuilt
            bp = find(base, name)
            if src.exists() and bp is not None:
                old = sorted(read_bytes(bp).decode("utf-8").splitlines())
                new = sorted(src.read_text(encoding="utf-8").splitlines())
                if old != new:
                    only_old = sorted(set(old) - set(new))[: a.lines]
                    only_new = sorted(set(new) - set(old))[: a.lines]
                    problems.append((name + " (rebuild)", [f"{name}: the builder's rebuild differs from the baseline's committed dump"]
                                     + ["  - " + l for l in only_old] + ["  + " + l for l in only_new]))
    if a.profile is not None:
        for bp in sorted(base.glob("profile_*.txt"))[:1]:
            old = parse_profile(bp.read_text(encoding="utf-8"))
            new = parse_profile((a.profile / "profile_summary.txt").read_text(encoding="utf-8"))
            out = []
            for w, ow in old["windows"].items():
                nw = new["windows"].get(w)
                if nw is None:
                    out.append(f"window '{w}' missing from the new profile")
                    continue
                if nw["gc_median"] is not None and ow["gc_median"] is not None and nw["gc_median"] > ow["gc_median"]:
                    out.append(f"window '{w}': PlayerLoop GC median {ow['gc_median']} B -> {nw['gc_median']} B per frame")
                for site, bpf in nw["sites"].items():
                    if any(asm in site for asm in OUR_ASSEMBLIES) and site not in ow["sites"] and bpf > 0:
                        out.append(f"window '{w}': new allocation site in our code ({bpf} B/frame): {site}")
            for load, ms in old["loads"].items():
                if load in new["loads"] and new["loads"][load] > ms * 1.25:
                    out.append(f"load '{load}': {ms} ms -> {new['loads'][load]} ms")
            if out:
                problems.append(("profile", out))
    failed = False
    for name, lines in problems:
        tag = "ALLOWED" if name in allow or name.split(" ")[0] in allow else "FAIL"
        failed |= tag == "FAIL"
        print(f"[{tag}] " + lines[0])
        for l in lines[1:]:
            print("    " + l)
    print(f"{len(files)} deterministic files compared; {sum(1 for n, _ in problems)} difference(s); "
          f"{'FAIL' if failed else 'OK'}")
    return 1 if failed else 0


def main(argv=None) -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("pack")
    p.add_argument("--static", type=Path, required=True)
    p.add_argument("--play", type=Path, required=True)
    p.add_argument("--profile", type=Path, action="append")
    p.add_argument("--out", type=Path, required=True)
    d = sub.add_parser("diff")
    d.add_argument("--baseline", type=Path, required=True)
    d.add_argument("--static", type=Path)
    d.add_argument("--play", type=Path)
    d.add_argument("--profile", type=Path)
    d.add_argument("--allow", default="")
    d.add_argument("--lines", type=int, default=40)
    a = ap.parse_args(argv)
    return cmd_pack(a) if a.cmd == "pack" else cmd_diff(a)


if __name__ == "__main__":
    sys.exit(main())
