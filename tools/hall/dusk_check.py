"""The dusk sequences ran on a running clock (HALL-FX, BUG-13; docs/pending/HALL-FX.md RESULT step 5).

    python tools/hall/dusk_check.py <run folder> [--min-rise 0.05]

For every sequence seq/dusk_* of a hall_fx_dusk run, reads frames.tsv's extra columns that HallFxRig's hallClock op
adds (hallHour, the hour the hall shows; cityLit, the city's lit share _HallCityLights.x) and checks that the clock
ran and the windows were lighting while it recorded: the hour never runs back, and the lit share never falls and
rises by at least --min-rise over the sequence (so windows faded in during the capture, and the flicker gate on the
same sequence measured them). Prints each sequence's hours and lit share; exit 0 when every one passes. It does not
measure flicker itself: seqtools flicker does, on the same folders.
"""
import argparse
import csv
import glob
import os
import sys


def columns(seq):
    with open(os.path.join(seq, 'frames.tsv'), encoding='utf-8') as f:
        for row in csv.DictReader(f, delimiter='\t'):
            extra = {}
            for part in (row.get('extra') or '').split(';'):
                if '=' in part:
                    k, v = part.split('=', 1)
                    extra[k] = v
            yield extra


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('run')
    ap.add_argument('--min-rise', type=float, default=0.05)
    args = ap.parse_args()
    seqs = sorted(glob.glob(os.path.join(args.run, 'seq', 'dusk_*')))
    if not seqs:
        raise SystemExit(f'no seq/dusk_* in {args.run}: run tools/qa/scenarios/hall_fx_dusk.json (with HallFxRig)')
    ok = True
    for seq in seqs:
        rows = [r for r in columns(seq) if 'cityLit' in r and 'hallHour' in r and r['hallHour'] != '-']
        if len(rows) < 2:
            print(f'{os.path.basename(seq)}: no hallHour / cityLit columns (hallClock was not run before it): FAIL')
            ok = False
            continue
        hours = [float(r['hallHour']) for r in rows]
        lit = [float(r['cityLit']) for r in rows]
        ran = all(b >= a - 1e-4 for a, b in zip(hours, hours[1:])) and hours[-1] > hours[0]
        rising = all(b >= a - 1e-4 for a, b in zip(lit, lit[1:])) and lit[-1] - lit[0] >= args.min_rise
        passed = ran and rising
        ok &= passed
        print(f'{os.path.basename(seq)}: {len(rows)} frames, hour {hours[0]:.3f} -> {hours[-1]:.3f}, lit share {lit[0]:.3f} -> {lit[-1]:.3f} '
              f'(clock ran: {ran}, lit rising by >= {args.min_rise}: {rising}): {"PASS" if passed else "FAIL"}')
    print('PASS' if ok else 'FAIL')
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
