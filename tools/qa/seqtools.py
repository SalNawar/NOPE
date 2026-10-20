"""QA-RIG sequence tools (P0-B): the offline analysis of what SeqRecorder captured (f0000.png ..., frames.tsv,
events.tsv), promoted from the DOC audit's seqtools.py and static_flicker.py, the PLAY audit's seqtool.py and the VIS
audit's blockiness.py. Every analysis prints a report, writes its pictures into the sequence folder, and with
--result-dir writes a gate result DemoGate reads (Library/QA/results/<id>.json).

usage:
  python seqtools.py flicker <seq> [--static] [--mask mask.png] [--th 24] [--back 8] [--within 3] [--max-toggles 2]
  python seqtools.py flash <seq> [--max-per-s 3] [--th 20]
  python seqtools.py latency <seq> [--roi x0,y0,x1,y1] [--track name] [--need-cue] [--max-frames 1]
  python seqtools.py frozen <seq> [--max-seconds 0.25]
  python seqtools.py cuts <seq>
  python seqtools.py hitches <seq> [--budget-ms 33.4]   (a timing sequence: recorded after the op realtime n=1)
  python seqtools.py all <seq>                 (flicker, flash, and latency when the sequence has presses; on a timing
                                                sequence, which has no frames, hitches)
  python seqtools.py gif <seq> <out.gif> [--scale 0.67] [--start N] [--end M] [--step K]
  python seqtools.py sheet <seq> <out.png> [--start N] [--end M] [--step 3] [--cols 4]
  python seqtools.py side <seq> <reference frames folder> <out.gif> [--frames N] [--step K] [--height 360]
  python seqtools.py blockiness <png> [<png> ...]
  python seqtools.py import-run <run.json> --result-dir D [--bp] [--commit C [--dirty] | --build-info qa_build.json]
result options (every analysis): --result-dir D --id ID --owner TRACK [--commit C [--dirty]] [--bp]
  (the id defaults to <run>.seq.<seq>.<analysis>, the owner to QA-RIG, the commit to git HEAD of --repo or the cwd;
  --dirty marks a result measured on uncommitted changes, which DemoGate counts as stale; without --commit it is read
  from git: changes to Assets, Packages or ProjectSettings less Unity's churn, as GitDirty)
"""
import argparse
import json
import os
import subprocess
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import seqlib  # noqa: E402


def git_head(repo):
    try:
        return subprocess.run(['git', '-C', repo, 'rev-parse', 'HEAD'], capture_output=True, text=True, check=True).stdout.strip()
    except (OSError, subprocess.CalledProcessError):
        return ''


# Unity's own churn (GitDirty.Churn): never a change to what is measured. An editor rewrites the build scene list and the
# current quality level by itself; the generated project files (*.csproj, *.sln) are outside the paths and dropped anyway.
CHURN = {p.lower() for p in ('Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset',
                             'ProjectSettings/EditorBuildSettings.asset',
                             'ProjectSettings/QualitySettings.asset')}
CHURN_EXTENSIONS = ('.csproj', '.sln')


def dirty_paths(name_only_output):
    """GitDirty.Filter: git diff --name-only's output less the churn paths and the generated project files."""
    paths = (line.strip() for line in (name_only_output or '').splitlines())
    return [p for p in paths if p and p.lower() not in CHURN and not p.lower().endswith(CHURN_EXTENSIONS)]


def git_dirty(repo):
    """GitDirty.Changed: tracked files under Assets, Packages and ProjectSettings whose content differs from HEAD, less
    the churn (False when git cannot run)."""
    try:
        out = subprocess.run(['git', '-C', repo, '-c', 'core.quotepath=off', 'diff', '--name-only', 'HEAD', '--',
                              'Assets', 'Packages', 'ProjectSettings'], capture_output=True, text=True, check=True).stdout
    except (OSError, subprocess.CalledProcessError):
        return False
    return bool(dirty_paths(out))


def load(seq):
    files = seqlib.frame_files(seq)
    rows = seqlib.read_tsv(os.path.join(seq, 'frames.tsv'))
    events = seqlib.read_tsv(os.path.join(seq, 'events.tsv'))
    return files, rows, events


def default_id(seq, analysis):
    seq = os.path.abspath(seq)
    name = os.path.basename(seq.rstrip('\\/'))
    parent = os.path.dirname(seq)
    run = os.path.basename(os.path.dirname(parent)) if os.path.basename(parent) == 'seq' else ''
    # A run folder is <scenario>_<yyyyMMdd_HHmmss>: the scenario is the id's prefix.
    scenario = run.rsplit('_', 2)[0] if run.count('_') >= 2 else run
    return f'{scenario}.seq.{name}.{analysis}' if scenario else f'seq.{name}.{analysis}'


def emit(args, seq, analysis, ok, measured, detail):
    print(f"{'PASS' if ok else 'FAIL'} {analysis} {seq}: {measured}")
    if detail:
        print('  ' + detail)
    if not getattr(args, 'result_dir', None):
        return
    rid = args.id or default_id(seq, analysis)
    if args.bp and not rid.startswith('bp.'):
        rid = 'bp.' + rid
    if args.commit is not None:
        commit, dirty = args.commit, args.dirty
    else:
        commit, dirty = git_head(args.repo or os.getcwd()), git_dirty(args.repo or os.getcwd())
    path = seqlib.write_result(args.result_dir, seqlib.gate_result(rid, args.owner, ok, measured, detail, commit, os.path.abspath(seq), args.bp,
                                                                   dirty))
    print('  result', path)


def heat_png(seq, files, heat, name):
    base = np.asarray(Image.open(files[len(files) // 2]).convert('RGB')).copy()
    hm = np.clip(heat * 60, 0, 255).astype(np.uint8)
    base[..., 0] = np.maximum(base[..., 0], hm)
    base[..., 1] = np.where(hm > 0, base[..., 1] // 3, base[..., 1])
    base[..., 2] = np.where(hm > 0, base[..., 2] // 3, base[..., 2])
    out = os.path.join(seq, name)
    Image.fromarray(base).save(out)
    return out


def cmd_flicker(args, seq=None):
    seq = seq or args.seq
    files, rows, _ = load(seq)
    lumas = [seqlib.load_luma(f) for f in files]
    states = [seqlib.frame_state(r) for r in rows] if rows else None
    if states is not None and len(states) < len(lumas):
        states += [states[-1]] * (len(lumas) - len(states))
    mask = None
    if getattr(args, 'mask', None):
        mask = np.asarray(Image.open(args.mask).convert('L')) > 127
    r = seqlib.flicker(lumas, states, mask, args.th, args.back, args.within, max_toggles=args.max_toggles, static_only=args.static)
    analysis = 'static_flicker' if args.static else 'flicker'
    lines = [f'frames {r["n"]}; toggles {r["toggles"]}; pixels over {args.max_toggles} toggles in 10 s: {r["bad_px"]}; worst pixel {r["worst"]}']
    if r['heat'] is not None and r['toggles']:
        lines.append('heat map ' + heat_png(seq, files, r['heat'], analysis + '_heat.png'))
        lines.append('top blocks (toggles, x, y): ' + ', '.join(f'{s}@{x},{y}' for s, x, y in seqlib.hot_blocks(r['heat'], top=10)))
        lines.append('frames: ' + ', '.join(f'{i}:{n}px {bb}' for i, n, bb in r['frames'][:12]))
    with open(os.path.join(seq, analysis + '.txt'), 'w', encoding='utf-8') as f:
        f.write('\n'.join(lines) + '\n')
    emit(args, seq, analysis, r['pass'], f'{r["bad_px"]} px toggling > {args.max_toggles}x in 10 s (worst {r["worst"]})', '; '.join(lines[1:3]))
    return r['pass']


def rgb_frames(files):
    return [np.asarray(Image.open(f).convert('RGB')) for f in files]


def cmd_flash(args, seq=None):
    seq = seq or args.seq
    files, _, _ = load(seq)
    r = seqlib.flash(rgb_frames(files), th=args.flash_th, max_per_s=args.max_per_s)
    detail = f'full-field changes at frames {r["full_field"][:10]}; red {r["red"][:10]}; busiest cells {sorted(r["cells"].items(), key=lambda kv: -kv[1])[:5]}'
    emit(args, seq, 'flash', r['pass'], f'{r["worst_per_s"]} flashes/s at worst (max {args.max_per_s}); {len(r["red"])} red full-field', detail)
    return r['pass']


def cmd_latency(args, seq=None):
    seq = seq or args.seq
    files, rows, events = load(seq)
    if not any(e.get('channel') in ('mouse', 'key') for e in events):
        print(f'SKIP latency {seq}: no press in events.tsv')
        return True
    lumas = [seqlib.load_luma(f) for f in files]
    roi = None
    if getattr(args, 'roi', None):
        roi = tuple(float(v) for v in args.roi.split(','))
    r = seqlib.latency(lumas, rows, events, roi=roi, max_frames=args.max_frames, need_cue=args.need_cue)
    if getattr(args, 'track', None) and rows:
        # The ROI is the tracked object's rect at each press.
        r = {'presses': [], 'pass': True}
        index_of = {int(row['frame']): int(row['i']) for row in rows}
        for e in events:
            if e.get('channel') not in ('mouse', 'key'):
                continue
            i = index_of.get(int(e['frame']))
            if i is None or i >= len(rows):
                continue
            rect = seqlib.parse_rects(rows[i].get('tracked', '')).get(args.track)
            sub = seqlib.latency(lumas, rows, [x for x in events if x is e or x.get('channel') not in ('mouse', 'key')], roi=rect,
                                 max_frames=args.max_frames, need_cue=args.need_cue)
            r['presses'] += sub['presses']
        r['pass'] = bool(r['presses']) and all(p['pass'] for p in r['presses'])
    worst = max((p['visual_frames'] for p in r['presses'] if p['visual_frames'] is not None), default=None)
    missing = sum(1 for p in r['presses'] if p['visual_frames'] is None)
    detail = '; '.join(f'press f{p["frame"]} {p["name"]}: visual {p["visual_frames"]}, cue {p["cue_frames"]}' for p in r['presses'][:12])
    emit(args, seq, 'latency', r['pass'], f'{len(r["presses"])} presses; worst {worst} frames to the first change (max {args.max_frames}); {missing} with no change', detail)
    return r['pass']


def cmd_frozen(args, seq=None):
    seq = seq or args.seq
    files, _, _ = load(seq)
    r = seqlib.frozen([seqlib.load_luma(f) for f in files])
    ok = r['seconds'] <= args.max_seconds
    emit(args, seq, 'frozen', ok, f'longest still run {r["seconds"]:.2f} s ({r["frames"]} frames from f{r["start"]}); max {args.max_seconds} s', '')
    return ok


def cmd_cuts(args, seq=None):
    seq = seq or args.seq
    files, _, _ = load(seq)
    c = seqlib.cuts([seqlib.load_luma(f) for f in files])
    emit(args, seq, 'cuts', not c, f'{len(c)} single-frame full-screen changes', ', '.join(f'f{i} {a}' for i, a in c[:12]))
    return not c


def cmd_hitches(args, seq=None):
    seq = seq or args.seq
    _, rows, _ = load(seq)
    r = seqlib.hitches(rows, args.budget_ms)
    ok = r['frames'] > 0 and not r['over']
    emit(args, seq, 'hitches', ok, f'{len(r["over"])} of {r["frames"]} frames over {args.budget_ms:.1f} ms; worst {r["worst"]:.1f} ms, median {r["median"]:.1f} ms',
         ', '.join(f'f{i} {t:.0f} ms' for i, t in r['over'][:12]) + ('' if r['frames'] else 'no frame times: record with the realtime op'))
    return ok


def cmd_all(args):
    # Each analysis writes its own result under its default id.
    args.id = None
    if seqlib.is_timing(args.seq):
        # A timing sequence has frame times and no frames: the hitch measurement is all it is for.
        return cmd_hitches(args)
    ok = cmd_flicker(args)
    ok = cmd_flash(args) and ok
    ok = cmd_latency(args) and ok
    return ok


def cmd_gif(args):
    files, rows, _ = load(args.seq)
    frames = []
    for i, f in enumerate(files):
        if i < args.start or (args.end is not None and i > args.end) or (i - args.start) % args.step:
            continue
        im = Image.open(f).convert('RGB')
        if i < len(rows):
            st = seqlib.frame_state(rows[i])
            if st['mouse']:
                x, y = st['mouse']
                down = rows[i].get('down') == '1'
                d = ImageDraw.Draw(im)
                d.ellipse([x - 7, y - 7, x + 7, y + 7], outline=(255, 0, 0) if down else (255, 255, 0), width=3)
        ImageDraw.Draw(im).text((6, 6), f'{os.path.basename(args.seq)} f{i:03d} t={i / seqlib.FPS:.2f}s', fill=(255, 255, 255))
        if args.scale != 1:
            im = im.resize((int(im.width * args.scale), int(im.height * args.scale)), Image.LANCZOS)
        frames.append(im.quantize(colors=128, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))
    if not frames:
        print('no frames')
        return False
    frames[0].save(args.out, save_all=True, append_images=frames[1:], duration=int(1000 / seqlib.FPS * args.step), loop=0, optimize=True)
    print('gif', args.out, len(frames), 'frames')
    return True


def cmd_sheet(args):
    files, _, _ = load(args.seq)
    sel = [i for i in range(len(files)) if i >= args.start and (args.end is None or i <= args.end) and (i - args.start) % args.step == 0]
    if not sel:
        print('no frames')
        return False
    w = 480
    ims = []
    for i in sel:
        im = Image.open(files[i]).convert('RGB')
        im = im.resize((w, int(w * im.height / im.width)))
        ImageDraw.Draw(im).text((4, 4), f'f{i}', fill=(255, 255, 0))
        ims.append(im)
    rows = (len(ims) + args.cols - 1) // args.cols
    sheet = Image.new('RGB', (args.cols * w, rows * ims[0].height))
    for k, im in enumerate(ims):
        sheet.paste(im, ((k % args.cols) * w, (k // args.cols) * ims[0].height))
    sheet.save(args.out)
    print('sheet', args.out, len(ims), 'frames')
    return True


def cmd_side(args):
    """Our sequence next to a reference clip's frames (Papers, Please or ReStory, FIX_PLAN_v2 rule 8), as one GIF."""
    ours = seqlib.frame_files(args.seq)
    ref = sorted(f for f in os.listdir(args.ref) if f.lower().endswith('.png'))
    n = min(len(ours), len(ref)) if args.frames <= 0 else min(args.frames, len(ours), len(ref))
    if n == 0:
        print('no frames')
        return False
    frames = []
    for k in range(0, n, args.step):
        a = Image.open(ours[k]).convert('RGB')
        b = Image.open(os.path.join(args.ref, ref[k])).convert('RGB')
        h = args.height
        a = a.resize((int(a.width * h / a.height), h))
        b = b.resize((int(b.width * h / b.height), h))
        both = Image.new('RGB', (a.width + b.width + 8, h), (0, 0, 0))
        both.paste(a, (0, 0))
        both.paste(b, (a.width + 8, 0))
        ImageDraw.Draw(both).text((6, 6), f'ours f{k}', fill=(255, 255, 0))
        ImageDraw.Draw(both).text((a.width + 14, 6), f'reference f{k}', fill=(255, 255, 0))
        frames.append(both.quantize(colors=160, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE))
    frames[0].save(args.out, save_all=True, append_images=frames[1:], duration=int(1000 / seqlib.FPS * args.step), loop=0, optimize=True)
    print('side by side', args.out, len(frames), 'frames')
    return True


def cmd_blockiness(args):
    for p in args.pngs:
        print(f'{p}\t{seqlib.blockiness(np.asarray(Image.open(p).convert("RGBA"))):.2f}')
    return True


def cmd_import_run(args):
    with open(args.run, encoding='utf-8') as f:
        run = json.load(f)
    commit, dirty = args.commit, args.dirty
    if commit is None and args.build_info:
        with open(args.build_info, encoding='utf-8-sig') as f:
            info = json.load(f)
        commit, dirty = info.get('commit', ''), bool(info.get('dirty', False))
    if commit is None:
        commit, dirty = git_head(args.repo or os.getcwd()), git_dirty(args.repo or os.getcwd())
    results = seqlib.results_from_run(run, commit, os.path.dirname(os.path.abspath(args.run)), args.bp, dirty)
    for r in results:
        seqlib.write_result(args.result_dir, r)
    print(f'imported {len(results)} result(s) from {args.run} into {args.result_dir} (commit {commit or "unknown"})')
    return all(r['pass'] for r in results)


def parser():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)

    def result_opts(p):
        p.add_argument('--result-dir')
        p.add_argument('--id')
        p.add_argument('--owner', default='QA-RIG')
        p.add_argument('--commit')
        p.add_argument('--dirty', action='store_true')
        p.add_argument('--repo')
        p.add_argument('--bp', action='store_true')

    def flicker_opts(p):
        p.add_argument('--static', action='store_true')
        p.add_argument('--mask')
        p.add_argument('--th', type=float, default=24.0)
        p.add_argument('--back', type=float, default=8.0)
        p.add_argument('--within', type=int, default=3)
        p.add_argument('--max-toggles', type=int, default=2)

    def flash_opts(p):
        p.add_argument('--max-per-s', type=int, default=3)
        p.add_argument('--flash-th', type=float, default=20.0)

    def latency_opts(p):
        p.add_argument('--roi')
        p.add_argument('--track')
        p.add_argument('--need-cue', action='store_true')
        p.add_argument('--max-frames', type=int, default=1)

    p = sub.add_parser('flicker'); p.add_argument('seq'); flicker_opts(p); result_opts(p); p.set_defaults(fn=cmd_flicker)
    p = sub.add_parser('flash'); p.add_argument('seq'); flash_opts(p); result_opts(p); p.set_defaults(fn=cmd_flash)
    p = sub.add_parser('latency'); p.add_argument('seq'); latency_opts(p); result_opts(p); p.set_defaults(fn=cmd_latency)
    p = sub.add_parser('frozen'); p.add_argument('seq'); p.add_argument('--max-seconds', type=float, default=0.25); result_opts(p); p.set_defaults(fn=cmd_frozen)
    p = sub.add_parser('cuts'); p.add_argument('seq'); result_opts(p); p.set_defaults(fn=cmd_cuts)
    p = sub.add_parser('hitches'); p.add_argument('seq'); p.add_argument('--budget-ms', type=float, default=1000.0 / 30 + 0.1); result_opts(p); p.set_defaults(fn=cmd_hitches)
    p = sub.add_parser('all'); p.add_argument('seq'); flicker_opts(p); flash_opts(p); latency_opts(p); result_opts(p)
    p.add_argument('--budget-ms', type=float, default=1000.0 / 30 + 0.1); p.set_defaults(fn=cmd_all)
    p = sub.add_parser('gif'); p.add_argument('seq'); p.add_argument('out'); p.add_argument('--scale', type=float, default=0.67)
    p.add_argument('--start', type=int, default=0); p.add_argument('--end', type=int); p.add_argument('--step', type=int, default=1); p.set_defaults(fn=cmd_gif)
    p = sub.add_parser('sheet'); p.add_argument('seq'); p.add_argument('out'); p.add_argument('--start', type=int, default=0)
    p.add_argument('--end', type=int); p.add_argument('--step', type=int, default=3); p.add_argument('--cols', type=int, default=4); p.set_defaults(fn=cmd_sheet)
    p = sub.add_parser('side'); p.add_argument('seq'); p.add_argument('ref'); p.add_argument('out'); p.add_argument('--frames', type=int, default=0)
    p.add_argument('--step', type=int, default=1); p.add_argument('--height', type=int, default=360); p.set_defaults(fn=cmd_side)
    p = sub.add_parser('blockiness'); p.add_argument('pngs', nargs='+'); p.set_defaults(fn=cmd_blockiness)
    p = sub.add_parser('import-run'); p.add_argument('run'); p.add_argument('--result-dir', required=True); p.add_argument('--bp', action='store_true')
    p.add_argument('--commit'); p.add_argument('--dirty', action='store_true'); p.add_argument('--build-info'); p.add_argument('--repo')
    p.set_defaults(fn=cmd_import_run)
    return ap


def main(argv=None):
    args = parser().parse_args(argv)
    return 0 if args.fn(args) else 1


if __name__ == '__main__':
    sys.exit(main())
