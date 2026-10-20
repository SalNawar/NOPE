"""QA-RIG built-player capture ([BP]; P0-B BuiltPlayerCapture): plays a scenario in the QA player and turns what it
recorded into gate results DemoGate reads.

usage: python run_player.py <scenario.json> [--project E:\\unity\\NOPE-t6] [--exe <QA player exe>] [--w 1920 --h 1080]
                            [--timeout 1800] [--no-analyze]

1. The QA player is built by the job QaJobs.BuildQaPlayer (default exe: E:\\unity\\NOPE-builds\\QA_<project folder>\\
   TimeSorter.exe, with qa_build.json beside it naming the commit it was built from, whether the tree was dirty, its
   product name and its quality level).
2. It refuses a player whose qa_build.json is missing or whose product name does not end in " QA": the QA player has
   its own product name, so its saves, PlayerPrefs and window settings never touch the real game's (Saleh's saves).
3. It starts the player windowed at --w x --h with -qaScenario and -qaOut <project>/Library/QA/runs/<scenario>_<time>;
   the player plays the scenario with its own virtual mouse (never the real one) and quits at the end.
4. It imports run.json's assertions as "bp."-prefixed results stamped with the built commit (dirty when the build was),
   and runs seqtools all on every recorded sequence: flicker, flash and latency on a captured one, hitches on a timing
   one (bp.<scenario>.seq.<name>.<analysis>).
5. The verdict (exit code 0 or 1) follows the known-red list (tools/qa/gate_known_red.txt's run patterns, as
   harness.txt in the editor): red results an open BUG part a later track owns are listed as known red and do not fail
   it; run.json's own ok stays the strict one.
It opens a window on the desktop: run it only when Saleh is away from the PC (house rule).
"""
import argparse
import datetime
import json
import os
import subprocess
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import seqlib  # noqa: E402
import seqtools  # noqa: E402

QA_PRODUCT_SUFFIX = ' QA'  # QaRunner.QaProductSuffix


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('scenario')
    ap.add_argument('--project', default=os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
    ap.add_argument('--exe')
    ap.add_argument('--w', type=int, default=1920)
    ap.add_argument('--h', type=int, default=1080)
    ap.add_argument('--timeout', type=float, default=1800)
    ap.add_argument('--no-analyze', action='store_true')
    a = ap.parse_args(argv)

    project = os.path.abspath(a.project)
    exe = a.exe or os.path.join(r'E:\unity\NOPE-builds', 'QA_' + os.path.basename(project.rstrip('\\/')), 'TimeSorter.exe')
    if not os.path.exists(exe):
        print(f'No QA player at {exe}: build it first (job QaJobs.BuildQaPlayer).')
        return 2
    info_path = os.path.join(os.path.dirname(exe), 'qa_build.json')
    if not os.path.exists(info_path):
        print(f'REFUSED: no {info_path}, so nothing says this player is a QA player with its own saves. Rebuild it (job QaJobs.BuildQaPlayer).')
        return 2
    with open(info_path, encoding='utf-8-sig') as f:
        info = json.load(f)
    product = info.get('product', '')
    if not product.endswith(QA_PRODUCT_SUFFIX):
        print(f"REFUSED: the player's product name is '{product}', not a QA one ('...{QA_PRODUCT_SUFFIX}'): it would play over the "
              'game\'s own save and window settings. Rebuild it (job QaJobs.BuildQaPlayer).')
        return 2
    commit, dirty = info.get('commit', ''), bool(info.get('dirty', False))
    with open(a.scenario, encoding='utf-8-sig') as f:
        name = json.load(f).get('name', 'scenario')
    out = os.path.join(project, 'Library', 'QA', 'runs', f'{name}_{datetime.datetime.now():%Y%m%d_%H%M%S}')
    os.makedirs(out, exist_ok=True)
    args = [exe, '-qaScenario', os.path.abspath(a.scenario), '-qaOut', out, '-screen-fullscreen', '0',
            '-screen-width', str(a.w), '-screen-height', str(a.h), '-logFile', os.path.join(out, 'player.log')]
    print('starting', ' '.join(args))
    proc = subprocess.Popen(args)
    end = time.time() + a.timeout
    while proc.poll() is None and time.time() < end:
        time.sleep(2)
    if proc.poll() is None:
        proc.kill()
        print(f'TIMEOUT after {a.timeout:.0f} s: the player was stopped')
    run_json = os.path.join(out, 'run.json')
    if not os.path.exists(run_json):
        print(f'FAIL: the player wrote no run.json ({out}; see player.log)')
        return 1
    results = os.path.join(project, 'Library', 'QA', 'results')
    with open(run_json, encoding='utf-8-sig') as f:
        run = json.load(f)
    imported = seqlib.results_from_run(run, commit, out, True, dirty)
    for r in imported:
        seqlib.write_result(results, r)
    print(f'imported {len(imported)} result(s) (commit {commit or "unknown"}{", DIRTY: they count as stale" if dirty else ""}); '
          f'run.json {"ok" if run.get("ok") else "not ok"}')

    with open(os.path.join(project, 'tools', 'qa', 'gate_known_red.txt'), encoding='utf-8-sig') as f:
        patterns = seqlib.known_red_run_patterns(f.read())
    unexpected, known = seqlib.judge_run(run, patterns)
    want = info.get('qualityName', '')
    if want and run.get('quality') != want:
        unexpected.append(f"played at quality {run.get('quality')}, the player's level is {want}")
    if not a.no_analyze:
        seq_root = os.path.join(out, 'seq')
        seqs = [os.path.abspath(os.path.join(seq_root, s)) for s in sorted(os.listdir(seq_root))] if os.path.isdir(seq_root) else []
        for path in seqs:
            seqtools.main(['all', path, '--result-dir', results, '--bp', '--commit', commit] + (['--dirty'] if dirty else []))
        # The analyses' verdicts are read back from the results they wrote for these sequences.
        written = []
        for file in os.listdir(results) if os.path.isdir(results) else []:
            with open(os.path.join(results, file), encoding='utf-8-sig') as f:
                r = json.load(f)
            if r.get('source') in seqs and not r.get('pass'):
                written.append((r['id'], f"{r['id']} [{r.get('owner', '')}] {r.get('measured', '')}"))
        more, more_known = seqlib.judge(written, patterns)
        unexpected += more
        known += more_known
    for line in known:
        print('KNOWN RED', line)
    for line in unexpected:
        print('FAIL', line)
    ok = not unexpected
    print(('PASS' if ok else 'FAIL') + (f' ({len(known)} known red)' if known else ''), out)
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
