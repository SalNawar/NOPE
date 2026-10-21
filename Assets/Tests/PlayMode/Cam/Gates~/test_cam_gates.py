"""CAM's gate coverage (docs/pending/CAM.md, rounds 4 to 8), both ways, over every line a run of CAM's scenarios
writes. Forward: every line CAM measures sits under one of the gate checks in CamChecks, under the line's owner, so the
merge gate cannot go green while such a line is red or missing: the lines CamOps' ops write (round 3's verifier found
cam_views.play.d12 under no gate) and the CAM lines the rig's built-in ops write (gc under the scenario's owner,
cameraCheck and the run's own camera line under CAM); a scenario with a gc step also runs on the QA player (the gc
verdict is [BP]), its CAM lines' bp. twins under a CAM BuiltPlayer check. Whole runs (round 7): every check that reads
a run requires the run's last probe (QaRunner.Run stops at a step that throws and still writes the lines of the steps
that ran), and each run's console, presses, quality and last probe refuse its CAM run check, as RUN-SAFE's checks read
their runs'. Reverse (round 6): no CamChecks pattern reads a line of another owner (QA-RIG's console, presses, probes
and frame analyses, TEXT-LANG's LiberationSans probe, a plate owner's verdict) but the listed deliberate inclusions, so
a CAM check never refuses the merge gate on another track's known red (round 6's cam_desk.* read TEXT-LANG's
liberationSans.end). Also: every check's pattern names lines a run writes, the cap report runs with papers on the desk,
each framing leg names its view, and the wiring of the rig fixes that live in coroutines; and (round 8) cam_gc's gc
window holds its whole cycle, which proves it ran there.
Reads the C# sources and the scenario files; needs no Unity. CAM's own file (round 7: moved out of QA-RIG's tools/qa
into CAM's Assets/Tests/PlayMode/Cam/, in a folder whose name ends in '~' so Unity imports nothing from it). Run it
from the repository's root with:
    python -m unittest discover -s "Assets/Tests/PlayMode/Cam/Gates~" -p "test_*.py"
"""
import collections
import json
import math
import os
import re
import sys
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..', '..', '..'))
QA = os.path.join(ROOT, 'tools', 'qa')
sys.path.insert(0, QA)

import seqlib  # noqa: E402 (QA-RIG's twin of GateResults.Matches, read from tools/qa)

CAM_OPS = os.path.join(ROOT, 'Assets', 'Tests', 'PlayMode', 'Cam', 'Rig', 'CamOps.cs')
CAM_CHECKS = os.path.join(ROOT, 'Assets', 'Editor', 'QA', 'Checks', 'CamChecks.cs')
QA_RUNNER = os.path.join(ROOT, 'Assets', 'Tests', 'PlayMode', 'Rig', 'QaRunner.cs')
SEQ_RECORDER = os.path.join(ROOT, 'Assets', 'Tests', 'PlayMode', 'Rig', 'SeqRecorder.cs')
DESK_VIEW_POSE = os.path.join(ROOT, 'Assets', 'Scripts', 'Visuals', 'DeskViewPose.cs')
DESK_CONFIG = os.path.join(ROOT, 'Assets', 'Scripts', 'Office', 'DeskConfigSO.cs')
DESK_ASSET = os.path.join(ROOT, 'Assets', 'Data', 'Config', 'Desk_Default.asset')
DESK_PLAN_OPS = os.path.join(ROOT, 'Assets', 'Tests', 'PlayMode', 'DeskPlan', 'Rig', 'DeskPlanOps.cs')
SEQTOOLS = os.path.join(QA, 'seqtools.py')
SCENARIOS = os.path.join(QA, 'scenarios')

# CamOps' ops and the assertion families each writes ('<family><name>...'); ops with none write no line.
OP_FAMILIES = {
    'camres': [],
    'cammotion': [],
    'camcycle': ['cycle.'],
    'camframe': ['frame.'],
    'camfade': ['fade.'],
    'camhold': ['hold.'],
    'camparallax': ['city.parallax.', 'city.look.', 'city.nobars.', 'city.click.', 'city.q.', 'city.f1.'],
    'camshake': ['shake.'],
    'camchrome': ['chrome.', 'plate.'],
    'camtilts': ['tilts.'],
    'camplay': ['play.'],
    'camcap': ['capreport.'],
}

# The rig's built-in ops that write lines (QaRunner): op -> [(family, the name when the step has none, owner)], the
# owner 'scenario' being the scenario's owner (QaRunner's Scenario.owner); a probe's unnamed fallback is its step's index.
BUILT_IN = {
    'cameracheck': [('camera.', None, 'CAM')],
    'gc': [('gc.', 'steady', 'scenario')],
    'drop': [('drop.', 'drag', 'scenario')],
    'probe': [('missingScripts.', 'step', 'QA-RIG'), ('materials.', 'step', 'QA-RIG'), ('liberationSans.', 'step', 'TEXT-LANG')],
    'selftest': [('selftest.', 'path', 'QA-RIG')],
}

# The lines every run writes at its end (QaRunner.Finish; quality when the harness set the level): id -> owner.
RUN_END = {'console': 'QA-RIG', 'camera': 'CAM', 'presses': 'QA-RIG', 'quality': 'QA-RIG'}

# The run-end lines a QA player's run does not write: QaRunner.FromCommandLine names no quality level (run_player.py
# checks the build's level from run.json itself).
PLAYER_SKIPS = {'quality'}

# The built-in ops that record a sequence (QaRunner: <run>/seq/<name>); tools/qa's analyses write a result for it.
SEQ_OPS = {'recstart', 'record'}

# The analyses tools/qa/seqtools.py writes a gate result for: <scenario>.seq.<sequence>.<analysis>, QA-RIG's by default.
ANALYSES = {'flicker', 'static_flicker', 'flash', 'latency', 'frozen', 'cuts', 'hitches'}

# Other tracks' ops a CAM scenario runs (DESK-PLAN's rig, DeskPlanOps): op -> (family, the name when the step has none
# ('target': the step's target), owner).
FOREIGN_OPS = {
    'deskvisible': ('visible.', 'target', 'DESK-PLAN'),
    'deskexpect': ('expect.', 'target', 'DESK-PLAN'),
    'deskclickboxes': ('clickboxes.', 'desk', 'DESK-PLAN'),
}

# A CAM run's last probe (each CAM scenario ends on the probe "end"): its line says the run reached its end. QaRunner.Run
# stops at the first step that throws (a drag whose target is off the screen, a resize or a wait timing out) and still
# calls Finish, so the lines of the steps that ran are all written: a check reading them alone goes green on the legs
# that ran (round 7, the class of round 4's cam_views.play.d12). QA-RIG's line, read on purpose.
END = 'missingScripts.end'

# The runs each check reads (the runs whose lines its patterns match; pinned against CamChecks below): each check
# requires the last probe of every one, so none goes green on a run that stopped early. A [BP] run is 'bp.<scenario>'.
READS = {
    'cam.play.tags': ('cam_views', 'cam_chrome', 'cam_desk', 'cam_gc'),
    'cam.play.tilts': ('cam_views',),
    'cam.play.day12': ('cam_views',),
    'cam.play.framing': ('cam_views',),
    'cam.play.fade': ('cam_views',),
    'cam.play.hold': ('cam_views',),
    'cam.play.city': ('cam_views',),
    'cam.play.shakes': ('cam_views',),
    'cam.play.chrome': ('cam_chrome',),
    'cam.play.guidePlate': ('cam_chrome',),
    'cam.play.stampHint': ('cam_chrome',),
    'cam.play.capreport': ('cam_views',),
    'cam.play.deskplan': ('cam_desk',),
    'cam.play.cycle': ('cam_gc',),
    'cam.bp.gc': ('bp.cam_gc',),
    'cam.bp.camera': ('bp.cam_gc',),
}

# One run check per CAM run (round 7): the run ran whole and clean, its own lines read as RUN-SAFE's checks read their
# runs' (RunSafeChecks.RunPatterns): its console (an error or exception logged during it, from CameraFeel, CityView,
# DeskView or the rig, refused no gate before), its presses, its quality level and its last probe's scripts and
# materials, all QA-RIG's. A player's run writes no quality line (PLAYER_SKIPS), as QA-RIG's qa.bp.smoke reads none.
RUN_CHECKS = {
    'cam.play.run.views': 'cam_views',
    'cam.play.run.chrome': 'cam_chrome',
    'cam.play.run.desk': 'cam_desk',
    'cam.play.run.gc': 'cam_gc',
    'cam.bp.run.gc': 'bp.cam_gc',
}
RUN_LINES = ('console', 'presses', 'quality', END, 'materials.end')


def _run_lines_of(run):
    """The run's own lines its run check reads (a player's run has no quality line)."""
    return [f'{run}.{line}' for line in RUN_LINES if not (run.startswith('bp.') and line in PLAYER_SKIPS)]


# The other owners' lines a CAM check reads on purpose: (check, pattern) -> the lines' owner. cam.play.deskplan judges
# DESK-PLAN's homes at A3 and A4 (FIX_PLAN v1 deferred them to CAM): each home seen and the intercom's wheel from the
# reading view (DESK-PLAN's lines) and every centre click delivered (QA-RIG's presses). Every check requires the last
# probe of each run it reads (READS), and each run check reads its run's own lines (RUN_CHECKS), QA-RIG's. Nothing else
# of another owner: a run's LiberationSans probe stays TEXT-LANG's, its sequences' analyses QA-RIG's.
DELIBERATE = {
    ('cam.play.deskplan', 'cam_desk.visible.*'): 'DESK-PLAN',
    ('cam.play.deskplan', 'cam_desk.expect.*'): 'DESK-PLAN',
    ('cam.play.deskplan', 'cam_desk.presses'): 'QA-RIG',
}
DELIBERATE.update({(cid, f'{run}.{END}'): 'QA-RIG' for cid, runs in READS.items() for run in runs})
DELIBERATE.update({(cid, line): 'QA-RIG' for cid, run in RUN_CHECKS.items() for line in _run_lines_of(run)})

Check = collections.namedtuple('Check', 'id owner patterns built_player kind demo')
Line = collections.namedtuple('Line', 'id owner how cam run')


def _read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def _safe(name):
    # QaRunner.Safe: anything but a letter, digit, '_' or '-' becomes '_'.
    return re.sub(r'[^A-Za-z0-9_-]', '_', name)


def _body(src, signature):
    """The body of the C# method declared by <signature> (indented as a class member)."""
    return re.search(re.escape(signature) + r'\s*\{(.*?)\n    \}\n', src, re.S).group(1)


def _scenarios():
    """CAM's scenario files: (file, parsed)."""
    files = sorted(f for f in os.listdir(SCENARIOS) if f.startswith('cam_') and f.endswith('.json'))
    return [(f, json.loads(_read(os.path.join(SCENARIOS, f)))) for f in files]


def _plates():
    """CamOps.Plates: (key, owner) of each overlay plate camchrome reports, its key a literal or a const."""
    src = _read(CAM_OPS)
    table = re.search(r'\bPlates\s*=\s*\{(.*?)\};', src, re.S).group(1)
    plates = []
    for key, owner in re.findall(r'\(\s*"[^"]+"\s*,\s*("[^"]+"|\w+)\s*,\s*"([^"]+)"\s*\)', table):
        if not key.startswith('"'):
            key = '"' + re.search(r'const\s+string\s+' + key + r'\s*=\s*"([^"]+)"', src).group(1) + '"'
        plates.append((key.strip('"'), owner))
    return plates


def _on_player(scenario):
    """True when the scenario also runs on the QA player: a gc step's verdict is [BP] (run_player.py)."""
    return any(s['op'].lower() == 'gc' for s in scenario['steps'])


def _checks():
    """CamChecks' play and built-player checks, each with every result pattern it requires (GateResults.Require, or
    the local All over several patterns, with the check's owner and whether it reads built-player results)."""
    src = _read(CAM_CHECKS)
    cam = re.search(r'const\s+string\s+Owner\s*=\s*"([^"]+)"', src).group(1)
    owner = lambda o: cam if o == 'Owner' else o.strip('"')
    kinds = {cid: (kind or 'Static', bool(demo)) for cid, kind, demo in re.findall(
        r'\[GateCheck\(\s*"([^"]+)"\s*,\s*(?:Owner|"[^"]+")\s*(?:,\s*GateKind\.(\w+))?\s*(,\s*DemoOnly\s*=\s*true)?\s*\)\]', src)}
    checks = collections.OrderedDict()
    for cid, o, pattern, bp in re.findall(
            r'GateResults\.Require\(\s*"([^"]+)"\s*,\s*(Owner|"[^"]+")\s*,\s*"([^"]+)"\s*(?:,\s*(true)\s*)?\)', src):
        kind, demo = kinds[cid]
        checks.setdefault(cid, Check(cid, owner(o), [], bp == 'true', kind, demo)).patterns.append(pattern)
    for cid, o, bp, args in re.findall(
            r'\bAll\(\s*"([^"]+)"\s*,\s*(Owner|"[^"]+")\s*,\s*(true|false)\s*,((?:\s*"[^"]+"\s*,?)+)\)', src):
        kind, demo = kinds[cid]
        checks.setdefault(cid, Check(cid, owner(o), [], bp == 'true', kind, demo)).patterns.extend(re.findall(r'"([^"]+)"', args))
    return list(checks.values())


def _knob_seconds(name, source, block=None):
    """A seconds knob's live value: the asset's (Desk_Default.asset, under its block when the knob sits in one), else
    the code's default in its source."""
    asset = _read(DESK_ASSET)
    if block:
        found = re.search(r'^  ' + block + r':\n(?:    .*\n)*?    ' + name + r': ([0-9.]+)', asset, re.M)
    else:
        found = re.search(r'^  ' + name + r': ([0-9.]+)', asset, re.M)
    if found:
        return float(found.group(1))
    return float(re.search(r'\b' + name + r' = ([0-9.]+)f;', _read(source)).group(1))


def run_lines(scenario, plates):
    """Every line a run of the scenario writes, with its owner, how it is judged ('play', 'bp' or 'editor-gc') and
    whether CAM measures it (CamOps' lines, and the built-in lines under CAM): the run's own lines at its end, each
    step's (CamOps' ops, the built-in ops, DESK-PLAN's ops), and the frame analyses of each recorded sequence; with a gc
    step the run is also played on the QA player, every line again with a bp. prefix but the quality line the player's
    run does not write. Each line names its run ('<scenario>', or 'bp.<scenario>' on the player)."""
    name, owner = scenario['name'], scenario.get('owner', 'QA-RIG')
    lines = [(rid, line_owner, line_owner == 'CAM') for rid, line_owner in RUN_END.items()]
    for i, step in enumerate(scenario['steps']):
        op, step_name = step['op'].lower(), step.get('name')
        if op == 'camchrome':
            if step.get('n', 0) > 0:
                continue  # a leg reports at its end (n = 0)
            leg = step_name or 'chrome'
            lines.append((f'chrome.{leg}', 'CAM', True))
            for key, plate_owner in plates:
                lines.append((f'chrome.{leg}.{key}', 'CAM', True))  # a plate the leg showed but never drew: not measured
                lines.append((f'plate.{leg}.{key}', plate_owner, True))  # a plate drawn in the leg
            continue
        for family in OP_FAMILIES.get(op, []):
            lines.append((f'{family}{step_name or "x"}', 'CAM', True))
        for family, fallback, line_owner in BUILT_IN.get(op, []):
            line_owner = owner if line_owner == 'scenario' else line_owner
            lines.append((f'{family}{_safe(step_name or (f"step{i}" if fallback == "step" else fallback))}', line_owner,
                          line_owner == 'CAM'))
        if op in FOREIGN_OPS:
            family, fallback, line_owner = FOREIGN_OPS[op]
            target = (step.get('target') or '').split('/')[-1]
            lines.append((f'{family}{step_name or (target if fallback == "target" else fallback)}', line_owner, False))
        if op in SEQ_OPS:
            lines.extend((f'seq.{_safe(step_name or "")}.{analysis}', 'QA-RIG', False) for analysis in sorted(ANALYSES))
    on_player = _on_player(scenario)
    judged = []
    for rid, line_owner, cam in lines:
        # The editor's gc number includes the editor's own allocations (QaRunner.Gc's detail): its verdict is the [BP] twin's.
        judged.append(Line(f'{name}.{rid}', line_owner, 'editor-gc' if rid.startswith('gc.') else 'play', cam, name))
        if on_player and rid not in PLAYER_SKIPS:
            judged.append(Line(f'bp.{name}.{rid}', line_owner, 'bp', cam, f'bp.{name}'))
    return judged


def cam_lines(scenario, plates):
    """The lines of run_lines that CAM measures."""
    return [line for line in run_lines(scenario, plates) if line.cam]


def _reads(check, line):
    """True when the check reads the line: a built-player check reads built-player results, a play check the editor's."""
    return (line.how == 'bp') == check.built_player and any(seqlib.matches(p, line.id) for p in check.patterns)


class CamGateCoverageTests(unittest.TestCase):

    def test_the_table_names_every_op_and_family_camops_has(self):
        src = _read(CAM_OPS)
        ops = {op.lower() for op in re.findall(r'RegisterOp\(\s*"([A-Za-z]+)"', src)}
        self.assertEqual(set(OP_FAMILIES), ops, 'a CamOps op added or gone: place it in OP_FAMILIES')
        families = set(re.findall(r'(?:runner\.Assert|new ChromeLine)\(\s*\$?"([a-z]+(?:\.[a-z0-9]+)*\.)', src))
        listed = {f for fs in OP_FAMILIES.values() for f in fs}
        self.assertEqual(listed, families, 'an assertion family added or gone in CamOps: place it in OP_FAMILIES')

    def test_the_built_in_table_matches_the_rigs_lines(self):
        # QaRunner's lines in a run: cameraCheck's and the run's camera (always CAM), gc's and drop's (the scenario's
        # owner), a probe's three (QA-RIG's scripts and materials, TEXT-LANG's LiberationSans), the self-test's and the
        # run's console, presses and quality (QA-RIG's). Every other op of QaRunner writes none, the sequences' analyses
        # aside (tools/qa).
        src = _read(QA_RUNNER)
        self.assertRegex(src, r'case "cameracheck":\s*AssertCamera\("camera\." \+ Safe\(s\.name\)\)')
        self.assertRegex(src, r'AssertCamera\("camera"\);')
        self.assertRegex(src, r'Assert\(id, "CAM", r\.Pass')
        self.assertRegex(src, r'"gc\." \+ Safe\(string\.IsNullOrEmpty\(s\.name\) \? "steady" : s\.name\)')
        self.assertRegex(src, r'Assert\(\$"drop\.\{name\}", Scenario\.owner')
        self.assertRegex(src, r'case "probe":\s*Probe\(string\.IsNullOrEmpty\(s\.name\) \? \$"step\{_steps\.Count\}" : s\.name\);')
        for family, _, owner in BUILT_IN['probe']:
            self.assertRegex(src, r'Assert\(\$"' + re.escape(family) + r'\{Safe\(name\)\}", "' + owner + '"')
        finish = _body(src, 'private void Finish()')
        self.assertEqual(set(RUN_END), set(re.findall(r'Assert(?:Camera)?\("([a-z]+)"', finish)) | {'presses'})
        self.assertIn('AssertPresses();', finish)
        # PLAYER_SKIPS: the quality line only with a level to expect, which the QA player's run is never given.
        self.assertRegex(finish, r'if \(!string\.IsNullOrEmpty\(_expectedQuality\)\)\s*\{\s*string end = QualityName\(\);\s*Assert\("quality"')
        self.assertRegex(_body(src, 'private static void FromCommandLine()'), r'\bBegin\(scenario, outDir, true\);')
        # Run: a step that throws ends the run, and Finish still writes the run's lines (the reason for END).
        run = _body(src, 'private IEnumerator Run()')
        self.assertRegex(run, r'if \(!result\.ok \|\| quit\)\s*break;\s*\}\s*Finish\(\);')
        owners = set(re.findall(r'\bAssert\([^;]*?,\s*("[A-Z-]+"|Scenario\.owner)\s*,', src))
        self.assertEqual({'"QA-RIG"', '"TEXT-LANG"', '"CAM"', 'Scenario.owner'}, owners, 'a built-in line with a new owner: place it in BUILT_IN')
        ops = set(re.findall(r'"([a-z]+)"', re.search(r'public static readonly string\[\] Ops =\s*\{(.*?)\};', src, re.S).group(1)))
        self.assertTrue(set(BUILT_IN) | SEQ_OPS <= ops, 'a built-in op in the tables is gone from QaRunner.Ops')

    def test_the_other_writers_tables_match_their_sources(self):
        # DESK-PLAN's ops a CAM scenario runs, and the analyses tools/qa writes a result for.
        src = _read(DESK_PLAN_OPS)
        self.assertRegex(src, r'const string Owner = "DESK-PLAN";')
        self.assertEqual(set(FOREIGN_OPS), set(re.findall(r'RegisterOp\("(\w+)"', src)))
        for family, _, _ in FOREIGN_OPS.values():
            self.assertIn(f'"{family}', src)
        seq = _read(SEQTOOLS)
        written = set(re.findall(r"emit\(args, seq, '(\w+)'", seq)) | set(re.findall(r"'(\w*flicker)'", seq))
        self.assertEqual(ANALYSES, written, 'an analysis that writes a gate result added or gone: place it in ANALYSES')

    def test_every_cam_scenario_op_is_known(self):
        # Each op of a CAM scenario is placed: CamOps', a built-in one with or without lines, a sequence, DESK-PLAN's.
        known_ops = set(re.findall(r'"([a-z]+)"', re.search(r'public static readonly string\[\] Ops =\s*\{(.*?)\};',
                                                                _read(QA_RUNNER), re.S).group(1)))
        for file, s in _scenarios():
            for step in s['steps']:
                op = step['op'].lower()
                self.assertTrue(op in OP_FAMILIES or op in FOREIGN_OPS or op in known_ops, f'{file}: op {op} is placed nowhere')

    def test_every_cam_line_of_cams_scenarios_is_under_a_cam_gate_of_its_owner(self):
        checks = _checks()
        self.assertIn('cam_views.play.*', [p for c in checks for p in c.patterns], 'the scripted day 12 (camplay) needs its own gate (cam.play.day12)')
        plates = _plates()
        self.assertEqual({'guide': 'CONTENT', 'stamphint': 'STAMP'}, dict(plates))
        scenarios = _scenarios()
        self.assertTrue(scenarios)
        seen = {'play': 0, 'bp': 0, 'editor-gc': 0}
        for file, s in scenarios:
            self.assertEqual('CAM', s.get('owner'), f'{file}: a CAM scenario names CAM its owner (gc lines go to it)')
            for line in cam_lines(s, plates):
                seen[line.how] += 1
                if line.how == 'editor-gc':
                    continue  # its bp. twin is checked below
                bp = line.how == 'bp'
                under = [c for c in checks if _reads(c, line) and c.kind == ('BuiltPlayer' if bp else 'Play')]
                self.assertTrue(under, f'{file}: {line.id} is under no CamChecks {"BuiltPlayer" if bp else "Play"} gate')
                self.assertIn(line.owner, {c.owner for c in under}, f'{file}: {line.id} ({line.owner}) is only under another owner\'s gate: {under}')
        self.assertGreater(seen['play'], 0)
        self.assertGreater(seen['bp'], 0, 'cam_gc runs on the QA player: its gc and camera lines need [BP] gates')
        self.assertGreater(seen['editor-gc'], 0)

    def test_no_cam_gate_reads_another_owners_line_but_the_listed_ones(self):
        # The reverse of the test above (round 6): a check reading another owner's line refuses the merge gate on that
        # owner's red (cam_desk.* read TEXT-LANG's liberationSans.end, known red until #12, and QA-RIG's lines;
        # widening cam.play.chrome to cam_chrome.* would read STAMP's and CONTENT's plates and the probes again).
        checks = _checks()
        plates = _plates()
        for c in checks:
            for p in c.patterns:
                # The patterns name CAM's runs only, so CAM's scenarios are the whole space to search.
                self.assertTrue(p.startswith('cam_') or p.startswith('bp.cam_'), f'{c.id}: {p} names no CAM run')
        for (cid, pattern), _ in DELIBERATE.items():
            self.assertIn(pattern, next(c.patterns for c in checks if c.id == cid), f'a stale deliberate inclusion: {cid} {pattern}')
        reads = 0
        for file, s in _scenarios():
            for line in run_lines(s, plates):
                for c in checks:
                    for p in c.patterns:
                        if (line.how == 'bp') != c.built_player or not seqlib.matches(p, line.id):
                            continue
                        reads += 1
                        if line.owner != c.owner:
                            self.assertEqual(DELIBERATE.get((c.id, p)), line.owner,
                                             f'{file}: {c.id} ({c.owner}) reads {line.id} ({line.owner}) through {p}: '
                                             f'narrow the pattern to CAM\'s lines, or list it in DELIBERATE with the reason')
        self.assertGreater(reads, 0)

    def test_every_gate_pattern_reads_lines_a_run_writes(self):
        # A pattern no run writes is a check red forever ('not measured'), or one that stopped reading what it names.
        checks = _checks()
        plates = _plates()
        lines = [line for _, s in _scenarios() for line in run_lines(s, plates)]
        for c in checks:
            for p in c.patterns:
                self.assertTrue(any((line.how == 'bp') == c.built_player and seqlib.matches(p, line.id) for line in lines),
                                f'{c.id}: no run of a CAM scenario writes a line {p} reads')

    def test_the_built_player_checks_run_with_the_demo_gate(self):
        # QA-RIG's convention (qa.bp.*; FIX_PLAN: "[BP] gates run on the nightly scratch build"), and they read
        # built-player results only.
        bp = [c for c in _checks() if c.kind == 'BuiltPlayer']
        self.assertEqual({'cam.bp.gc', 'cam.bp.camera', 'cam.bp.run.gc'}, {c.id for c in bp})
        for c in bp:
            self.assertTrue(c.built_player and c.demo and all(p.startswith('bp.') for p in c.patterns), c.id)

    def test_every_play_and_built_player_check_is_read_by_these_tests(self):
        # A check whose patterns the parser above cannot see would escape every test here.
        ids = set(re.findall(r'\[GateCheck\(\s*"([^"]+)"\s*,\s*(?:Owner|"[^"]+")\s*,\s*GateKind\.(?:Play|BuiltPlayer)\b',
                             _read(CAM_CHECKS)))
        self.assertEqual(ids, {c.id for c in _checks()})
        self.assertEqual(ids, set(READS) | set(RUN_CHECKS), 'a check added or gone: place it in READS or RUN_CHECKS')

    def test_every_check_requires_the_end_of_every_run_it_reads(self):
        # Round 7: QaRunner.Run stops at the first step that throws (cam_chrome's drag of a decoration that is not on the
        # screen, a camres or a QaGame.Until timing out) and still calls Finish, so every line of the steps that ran is
        # written: cam.play.chrome and cam.play.stampHint went green on the legs that ran with chrome.a4 and
        # plate.a4.stamphint never written, and cam.play.framing on a cam_views run that stopped before the day-12 16:10,
        # A3 and A4 legs. Each CAM run ends on its probe "end"; a check reading any line of a run requires that probe's
        # line, so a run that stopped early fails every check that reads it.
        checks = _checks()
        plates = _plates()
        scenarios = _scenarios()
        runs_written = set()
        for file, s in scenarios:
            last = s['steps'][-1]
            self.assertEqual(('probe', 'end'), (last['op'].lower(), last.get('name')), f'{file}: a CAM run ends on its probe "end" ({END})')
            self.assertEqual(1, sum(1 for st in s['steps'] if st['op'].lower() == 'probe' and st.get('name') == 'end'), file)
            runs_written.add(s['name'])
            if _on_player(s):
                runs_written.add('bp.' + s['name'])
        lines = [line for _, s in scenarios for line in run_lines(s, plates)]
        required = set()
        for c in checks:
            runs = {line.run for line in lines if _reads(c, line)}
            listed = set(READS.get(c.id, ())) | ({RUN_CHECKS[c.id]} if c.id in RUN_CHECKS else set())
            self.assertEqual(listed, runs, f'{c.id} reads the runs {sorted(runs)}: name them in READS (or RUN_CHECKS)')
            for run in runs:
                self.assertIn(f'{run}.{END}', c.patterns, f'{c.id} reads {run} but would go green on a run that stopped early: require {run}.{END}')
                required.add(run)
        self.assertEqual(runs_written, required, 'every CAM run, in the editor and on the QA player, needs its completion required')

    def test_every_cam_runs_console_presses_quality_and_end_refuse_its_run_check(self):
        # Round 7: an error or exception logged during a CAM run (CameraFeel, CityView, DeskView, the rig) refused no gate,
        # only harness.txt said "failed"; round 6 had narrowed cam.play.deskplan off cam_desk.console and .quality. Each
        # run's own lines refuse its CAM run check, as RUN-SAFE's checks read theirs (RunSafeChecks.RunPatterns): console,
        # presses, quality (in the editor) and the last probe's scripts and materials.
        checks = {c.id: c for c in _checks()}
        plates = _plates()
        seen = 0
        for file, s in _scenarios():
            runs = [s['name']] + (['bp.' + s['name']] if _on_player(s) else [])
            owned = {RUN_CHECKS[cid]: checks[cid] for cid in RUN_CHECKS}
            for run in runs:
                self.assertIn(run, owned, f'{file}: the run {run} has no run check')
                check = owned[run]
                self.assertEqual(('CAM', run.startswith('bp.')), (check.owner, check.built_player), check.id)
                self.assertEqual('BuiltPlayer' if run.startswith('bp.') else 'Play', check.kind, check.id)
                own = _run_lines_of(run)
                self.assertEqual(set(own), set(check.patterns), f'{check.id} reads exactly its run\'s own lines')
                written = {line.id for line in run_lines(s, plates) if line.run == run}
                for rid in own:
                    self.assertIn(rid, written, f'{file}: {rid} is a line the run writes')
                    seen += 1
        self.assertGreater(seen, 0)

    def test_every_cap_leg_runs_with_the_days_papers_on_the_desk(self):
        # camcap measures the printed text of the papers on the desk, red 'not measured' with none, and a handed paper
        # waits on the counter (DeskController.HandOver) until a click or a drag takes it: round 5's day-1 legs were
        # red by construction. Each leg runs after camplay laid the day's papers on the desk, on the same day.
        legs = 0
        for file, s in _scenarios():
            played = False
            for step in s['steps']:
                op = step['op'].lower()
                if op in ('newrun', 'jumpday'):
                    played = False
                elif op == 'camplay':
                    played = True
                elif op == 'camcap':
                    legs += 1
                    self.assertTrue(played, f'{file}: camcap {step.get("name")} runs before any camplay of its day laid papers on the desk')
        self.assertGreater(legs, 0, 'the cap report (cam.play.capreport) needs its legs')

    def test_the_chrome_op_reports_what_its_steps_show_through_the_pure_report(self):
        # The wiring CamRigTests cannot reach (the op itself needs the editor): each camchrome step's expectations come
        # from ShownBy, and the leg's end asserts every line LegLines returns, as is.
        src = _read(CAM_OPS)
        body = _body(src, 'private static IEnumerator Chrome(QaRunner runner, QaStep step)')
        self.assertRegex(body, r'List<string> shows = ShownBy\(step\.n, step\.target\);')
        self.assertRegex(body, r'_chrome\.Shows\.UnionWith\(shows\);')
        self.assertRegex(body, r'UnplacedFrames = done\.UnplacedFrames,')
        self.assertRegex(body, r'Missing = string\.Join\(", ", ChromeNames\.Where\(c => done\.Controls\.All\(w => w\.Path != c\)\)\),')
        self.assertRegex(body, r'foreach \(ChromeLine line in LegLines\(name, done\.Shows, tally, ')
        self.assertRegex(body, r'runner\.Assert\(line\.Id, line\.Owner, line\.Pass, line\.Measured, line\.Detail\);')

    def test_the_chrome_watch_places_a_held_thing_that_is_no_component(self):
        # Round 6: a dragged decoration registers DeskDecor's plain Item, no Component, so a watch that placed only
        # Components counted 0 holding frames on every day-12 leg (CamRigTests pins HeldMember on the real type).
        src = _read(CAM_OPS)
        watch = _body(src, 'private static IEnumerator Watch(ChromeWatch watch)')
        self.assertRegex(watch, r'HoldRegistry\.Desk\.Current is IHoldable holding')
        self.assertRegex(watch, r'HeldObject\(holding\) is GameObject held && QaGame\.ScreenRect\(held\) is Rect heldRect')
        self.assertRegex(watch, r'watch\.UnplacedFrames\+\+;')
        self.assertNotRegex(watch, r'Current is Component')
        held = _body(src, 'private static GameObject HeldObject(IHoldable held)')
        self.assertRegex(held, r'HeldMember\(type\)')

    def test_a_chip_read_counts_once_its_paper_is_out(self):
        # Round 5's fix, in camplay's coroutine: the attempt's end comes from the pure ChipReadOf (CamRigTests), a stuck
        # read ends the reads before any count, and reads grows in one place, after both.
        play = _body(_read(CAM_OPS), 'private static IEnumerator Play(QaRunner runner, QaStep step)')
        self.assertTrue(re.search(r'ChipRead read = ChipReadOf\(began, back\);', play), 'camplay reads each attempt through ChipReadOf')
        self.assertTrue(re.search(r'if \(read == ChipRead\.Stuck\)\s*\{\s*notes\.Add\([^;]*\);\s*break;\s*\}\s*reads\+\+;', play),
                        'a stuck read is noted and ends the reads, and only then does a read count')
        self.assertEqual(1, play.count('reads++'), 'reads grows in one place')

    def test_every_chrome_leg_names_what_it_shows(self):
        # A plate a leg shows must report a line even when it is never drawn (red 'not measured', never silence), and
        # a half of the leg it shows must be seen: the day-1 leg names the guide's plate and the reading view; each
        # day-12 leg its held decoration and the reading view, and shows the stamp hint (camchrome n = 2).
        s = json.loads(_read(os.path.join(SCENARIOS, 'cam_chrome.json')))
        starts = {st.get('name'): [k for k in re.split(r'[,\s]+', st.get('target', '')) if k]
                  for st in s['steps'] if st['op'] == 'camchrome' and st.get('n') == 1}
        self.assertEqual(['guide', 'reading'], starts.pop('a1_d1'))
        self.assertEqual({'a1', 'a2', 'a3', 'a4'}, set(starts))
        for leg, shows in starts.items():
            self.assertEqual(['held', 'reading'], shows, leg)
        hints = [st for st in s['steps'] if st['op'] == 'camchrome' and st.get('n') == 2]
        self.assertEqual(4, len(hints), 'each day-12 leg (A1-A4) shows the stamp hint')

    def test_the_gc_window_holds_the_whole_cycle_and_the_cycle_proves_it_ran_there(self):
        # Round 8 (WAVE2_OPEN2 CAM #2): QaRunner.Gc counts frames (f x 30) while camcycle waited real seconds, so the
        # window held a frame-rate-dependent slice of the cycle (5.5 s of its 11.2 at 60 Hz, no tilt in measured), its
        # first tilt ran before the window opened, and a refused turn measured an idle window at 0 B. Now the cycle waits
        # for the window, turns a frame after it opens, runs on frames at the 30 fps game step, allocates nothing once it
        # runs, and reports a line the [BP] gc check requires with the gc number; the window holds the longest cycle the
        # knobs allow.
        s = json.loads(_read(os.path.join(SCENARIOS, 'cam_gc.json')))
        ops = [(st['op'].lower(), st) for st in s['steps']]
        starts = [i for i, (op, st) in enumerate(ops) if op == 'camcycle' and st.get('n', 0) > 0]
        self.assertEqual(1, len(starts), 'cam_gc starts one cycle (camcycle n = 1)')
        start = ops[starts[0]][1]
        gc = ops[starts[0] + 1]
        self.assertEqual('gc', gc[0], 'the gc step follows camcycle n = 1 at once: the cycle waits only briefly for its window')
        reports = [i for i, (op, st) in enumerate(ops) if op == 'camcycle' and st.get('n', 0) == 0]
        self.assertEqual(1, len(reports), 'cam_gc reports its cycle once (camcycle n = 0)')
        self.assertGreater(reports[0], starts[0] + 1, 'the cycle is reported after the gc window closed (its text allocates)')
        self.assertTrue(all(op == 'mark' for op, _ in ops[starts[0] + 2:reports[0]]), 'nothing but the sites mark runs between')
        self.assertEqual(start.get('name'), ops[reports[0]][1].get('name'))
        self.assertEqual(start.get('name'), gc[1].get('name'))
        self.assertEqual(0, gc[1].get('n', 0), 'the [BP] budget is 0 B a frame')
        bp = next(c for c in _checks() if c.id == 'cam.bp.gc')
        self.assertIn('bp.cam_gc.gc.*', bp.patterns)
        self.assertIn('bp.cam_gc.cycle.*', bp.patterns, 'the 0 B number counts only with the proof that the cycle ran inside it')

        # The window (QaRunner.Gc: at least 10 frames, f x 30) holds the longest cycle these knobs make (CityLookTimeline's
        # total is at most the turn plus the fade), its first turn at most two frames in (the window's uncounted opening
        # frame, or the next, is the frame the cycle sees it open on) and its last check one frame after its end.
        src = _read(CAM_OPS)
        const = lambda name: int(re.search(r'const int ' + name + r' = (\d+);', src).group(1))
        fps = int(re.search(r'public const int Fps = (\d+);', _read(SEQ_RECORDER)).group(1))
        runner = _read(QA_RUNNER)
        self.assertRegex(_body(runner, 'private IEnumerator Gc(QaStep s)'),
                         r'int frames = Mathf\.Max\(10, Mathf\.CeilToInt\(\(s\.f > 0f \? s\.f : 3f\) \* SeqRecorder\.Fps\)\);')
        window = max(10, math.ceil(gc[1].get('f', 3) * fps))
        desk = _knob_seconds('seconds', DESK_VIEW_POSE, 'deskView')
        city = _knob_seconds('citySeconds', DESK_CONFIG) + _knob_seconds('cityFadeSeconds', DESK_CONFIG)
        move = lambda seconds: math.ceil(seconds * fps) + const('SettleFrames')
        cycles = round(start.get('f', 0)) or 2
        longest = cycles * 2 * (move(desk) + move(city)) + 2
        self.assertLessEqual(longest, window, f'the gc window ({window} frames) cannot hold the cycle ({longest} frames at most): lengthen it')

        # The window is open exactly while the camera log is quiet outside a timing sequence (QaRunner), and it opens
        # before Gc's uncounted first frame.
        self.assertIn('Cameras.QuietWhen = () => _measuringGc || Recorder != null && Recorder.Timing;', runner)
        self.assertRegex(_body(runner, 'private IEnumerator Gc(QaStep s)'),
                         r'(?s)_measuringGc = true;.*?yield return null;\s*for \(int i = 0; i < frames; i\+\+\)\s*\{\s*yield return null;\s*if \(gc\.Valid\)\s*samples\.Add\(gc\.LastValue\);\s*\}.*?_measuringGc = false;')
        self.assertRegex(src, r'private static bool GcWindowOpen\(QaRunner runner\) =>\s*runner != null && runner\.Cameras != null && runner\.Cameras\.QuietWhen != null && runner\.Cameras\.QuietWhen\(\) &&\s*\(runner\.Recorder == null \|\| !runner\.Recorder\.Timing\);')

        # The cycle: waits for the window, sets the 30 fps step, turns one frame later, checks the window after every
        # frame, and makes nothing while it runs (no wait objects, no text, no collections).
        cycling = _body(src, 'private static IEnumerator Cycling(QaRunner runner, CycleTally tally, DeskView desk, CityView city, int deskFrames, int cityFrames)')
        self.assertRegex(cycling, r'^\s*for \(int wait = 0; !GcWindowOpen\(runner\); wait\+\+\)')
        self.assertRegex(cycling, r'tally\.Opened = true;\s*tally\.RestoreRate = Time\.captureFramerate;\s*Time\.captureFramerate = SeqRecorder\.Fps;\s*yield return null;\s*for \(int turn = 0;')
        self.assertRegex(cycling, r'yield return null;\s*tally\.Ran\+\+;\s*if \(tally\.ShutAt == 0 && !GcWindowOpen\(runner\)\)\s*tally\.ShutAt = tally\.Ran;')
        self.assertRegex(cycling, r'Stop\(tally\);\s*$')
        for name, body in (('Cycling', cycling),
                           ('Turn', _body(src, 'private static bool Turn(DeskView desk, CityView city, int phase)')),
                           ('Stop', _body(src, 'private static void Stop(CycleTally tally)'))):
            for made in (r'\bnew\b', r'WaitFor', r'\$"', r'"', r'=>', r'\.Where\(|\.Select\(|\.ToList\(', r'string\.'):
                self.assertNotRegex(body, made, f'{name} runs inside the gc window and must allocate nothing ({made})')
        turn = _body(src, 'private static bool Turn(DeskView desk, CityView city, int phase)')
        self.assertRegex(turn, r'desk\.TiltIn\(CameraMoveCause\.ViewKey\);')
        self.assertIn('return desk.IsOn != was;', turn)
        self.assertIn('return city.IsOn != looked;', turn)
        cycle = _body(src, 'private static IEnumerator Cycle(QaRunner runner, QaStep step)')
        self.assertRegex(cycle, r'runner\.StartCoroutine\(Cycling\(runner, tally, desk, city, deskFrames, cityFrames\)\);\s*yield break;')
        self.assertRegex(cycle, r'Stop\(done\);\s*bool pass = CycleVerdict\(done, out string measured, out string detail\);\s*runner\.Assert\("cycle\." \+ name, Owner, pass, measured, detail\);')

    def test_every_framing_leg_is_judged_on_the_view_it_names(self):
        # Round 7: camframe judged whichever view was on and camcap measured from any view, but DeskView.Toggle does
        # nothing unless its toggle is live, so a *_reading leg could be judged on the office view's criteria with no
        # .bottom line, and a cap report taken at the office pose. Each camframe step names its view in its target
        # (CamOps.ReadingLeg, CamRigTests), its name agrees, and the op refuses a leg whose view is not on (both of a
        # reading leg's lines red); camcap refuses unless the reading view is on.
        legs = {'office': 0, 'reading': 0}
        for file, s in _scenarios():
            for step in s['steps']:
                if step['op'].lower() == 'camframe':
                    view = step.get('target')
                    self.assertIn(view, legs, f'{file}: camframe {step.get("name")} names no view ("office" or "reading")')
                    self.assertTrue(step.get('name', '').endswith('_' + view), f'{file}: camframe {step.get("name")} names the {view} view')
                    legs[view] += 1
        self.assertTrue(legs['office'] > 0 and legs['reading'] > 0, legs)
        src = _read(CAM_OPS)
        frame = _body(src, 'private static IEnumerator Frame(QaRunner runner, QaStep step)')
        self.assertRegex(frame, r'^\s*bool reading = ReadingLeg\(step\.target\);', 'the leg\'s view is read before anything is measured')
        self.assertRegex(frame, r'if \(view\.IsOn != reading\)\s*\{\s*NotMeasured\(')
        self.assertEqual(1, frame.count('view.IsOn'), 'once the views agree, the leg is judged on its own view (reading), never on IsOn')
        self.assertRegex(frame, r'if \(!reading\)\s*yield break;\s*Ray bottom')
        not_measured = _body(src, 'private static void NotMeasured(QaRunner runner, string name, bool reading, string why)')
        self.assertRegex(not_measured, r'runner\.Assert\("frame\." \+ name, Owner, false, "not measured", why\);')
        self.assertRegex(not_measured, r'if \(reading\)\s*runner\.Assert\("frame\." \+ name \+ "\.bottom", Owner, false, "not measured", why\);')
        cap = _body(src, 'private static IEnumerator Cap(QaRunner runner, QaStep step)')
        self.assertRegex(cap, r'if \(cam == null \|\| view == null \|\| !view\.IsOn\)\s*\{\s*runner\.Assert\("capreport\." \+ name, Owner, false, "not measured"')
        self.assertLess(cap.index('!view.IsOn'), cap.index('FindObjectsByType<DeskDocument>'), 'camcap measures nothing outside the reading view')


if __name__ == '__main__':
    unittest.main()
