"""One entry point for the static half of the audit baseline.

  python tools/audit/run_static_baseline.py --repo <repo> --out <dir>
         [--compile-check <compile_check_art.py>] [--cc-dir <folder>]
         [--skip-analyzer] [--check <baseline metrics_baseline.json>]

Steps:
  1. (optional) runs the compile-check script, which regenerates the Unity-style
     csproj files for <repo> in <its folder>/cc_art;
  2. analyzer_build.py: copies those csproj files into <out>/ab, builds with
     the SDK analyzers, writes <out>/analyzer.json (skipped with
     --skip-analyzer, which reuses an existing <out>/analyzer.json);
  3. inventory.py  -> <out>/inventory.json, inventory.md;
  4. metrics.py    -> <out>/metrics_baseline.json, metrics_baseline.md;
  5. (optional) compare.py against --check; exit 1 on regressions.
"""
from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import analyzer_build  # noqa: E402
import compare  # noqa: E402
import inventory  # noqa: E402
import metrics  # noqa: E402
import sources  # noqa: E402


def main(argv=None):
    ap = argparse.ArgumentParser(description="Static audit baseline: analyzer build, inventory, metrics")
    ap.add_argument("--repo", type=Path, default=HERE.parent.parent)
    ap.add_argument("--out", type=Path, default=None)
    ap.add_argument("--compile-check", type=Path, default=None,
                    help="path to compile_check_art.py; run first to regenerate the csproj files")
    ap.add_argument("--cc-dir", type=Path, default=None,
                    help="folder with the generated csproj files (default: <compile-check dir>/cc_art)")
    ap.add_argument("--skip-analyzer", action="store_true", help="reuse <out>/analyzer.json")
    ap.add_argument("--check", type=Path, default=None, help="baseline metrics_baseline.json to gate against")
    a = ap.parse_args(argv)
    repo = a.repo.resolve()
    out = (a.out or sources.default_out(repo)).resolve()
    out.mkdir(parents=True, exist_ok=True)
    t0 = time.time()
    cc_dir = a.cc_dir
    if a.compile_check:
        r = subprocess.run([sys.executable, str(a.compile_check), str(repo)])
        if r.returncode != 0:
            print("compile check failed", file=sys.stderr)
            return r.returncode
        cc_dir = cc_dir or a.compile_check.resolve().parent / "cc_art"
    common = ["--repo", str(repo), "--out", str(out)]
    if not a.skip_analyzer:
        if cc_dir is None:
            ap.error("--cc-dir or --compile-check is required unless --skip-analyzer")
        analyzer_build.main(common + ["--cc-dir", str(cc_dir)])
        print(f"[{time.time() - t0:.0f}s] analyzer build done")
    inventory.main(common)
    print(f"[{time.time() - t0:.0f}s] inventory done")
    metrics.main(common)
    print(f"[{time.time() - t0:.0f}s] metrics done -> {out}")
    if a.check:
        return compare.main([str(a.check), str(out / "metrics_baseline.json")])
    return 0


if __name__ == "__main__":
    sys.exit(main())
