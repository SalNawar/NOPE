"""QA-RIG built-player capture ([BP]; P0-B BuiltPlayerCapture): plays a scenario in the QA player and turns what it
recorded into gate results DemoGate reads.

usage: python run_player.py <scenario.json> [--project E:\\unity\\NOPE-t6] [--exe <QA player exe>] [--w 1920 --h 1080]
                            [--timeout 1800] [--no-analyze]

1. The QA player is built by the job QaJobs.BuildQaPlayer (default exe: E:\\unity\\NOPE-builds\\QA_<project folder>\\
   TimeSorter.exe, with qa_build.json beside it naming the commit it was built from).
2. This starts it windowed at --w x --h with -qaScenario and -qaOut <project>/Library/QA/runs/<scenario>_<time>; the
   player plays the scenario with its own virtual mouse (never the real one) and quits at the end.
3. It imports run.json's assertions as "bp."-prefixed results stamped with the built commit, and runs flicker, flash
   and latency on every recorded sequence, plus hitches on those recorded in real time (meta.json fixedStep false)
   (bp.<scenario>.seq.<name>.<analysis>).
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
    commit = ''
    if os.path.exists(info_path):
        with open(info_path, encoding='utf-8-sig') as f:
            commit = json.load(f).get('commit', '')
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
    imported = seqlib.results_from_run(run, commit, out, True)
    for r in imported:
        seqlib.write_result(results, r)
    print(f'imported {len(imported)} result(s) (commit {commit or "unknown"}); run {"ok" if run.get("ok") else "FAILED"}')
    ok = bool(run.get('ok'))
    if not a.no_analyze:
        seq_root = os.path.join(out, 'seq')
        for seq in sorted(os.listdir(seq_root)) if os.path.isdir(seq_root) else []:
            path = os.path.join(seq_root, seq)
            ok = seqtools.main(['all', path, '--result-dir', results, '--bp', '--commit', commit]) == 0 and ok
            meta = os.path.join(path, 'meta.json')
            if os.path.exists(meta):
                with open(meta, encoding='utf-8-sig') as f:
                    if not json.load(f).get('fixedStep', True):
                        ok = seqtools.main(['hitches', path, '--result-dir', results, '--bp', '--commit', commit]) == 0 and ok
    print('PASS' if ok else 'FAIL', out)
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
