"""CAM's gate coverage (docs/pending/CAM.md, rounds 4 to 6), both ways, over every line a run of CAM's scenarios
writes. Forward: every line CAM measures sits under one of the gate checks in CamChecks, under the line's owner, so the
merge gate cannot go green while such a line is red or missing: the lines CamOps' ops write (round 3's verifier found
cam_views.play.d12 under no gate) and the CAM lines the rig's built-in ops write (gc under the scenario's owner,
cameraCheck and the run's own camera line under CAM); a scenario with a gc step also runs on the QA player (the gc
verdict is [BP]), its CAM lines' bp. twins under a CAM BuiltPlayer check. Reverse (round 6): no CamChecks pattern reads
a line of another owner (QA-RIG's console, presses, probes and frame analyses, TEXT-LANG's LiberationSans probe, a
plate owner's verdict) but the listed deliberate inclusions, so a CAM check never refuses the merge gate on another
track's known red (round 6's cam_desk.* read TEXT-LANG's liberationSans.end). Also: every check's pattern names lines a
run writes, the cap report runs with papers on the desk, and the wiring of the two rig fixes that live in coroutines.
Reads the C# sources and the scenario files; needs no Unity. CAM's file in QA-RIG's tools/qa (discover finds it here;
the orchestrator's approval is pending, CAM.md)."""
import collections
import json
import os
import re
import unittest

import seqlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, '..', '..')
CAM_OPS = os.path.join(ROOT, 'Assets', 'Tests', 'PlayMode', 'Cam', 'Rig', 'CamOps.cs')
CAM_CHECKS = os.path.join(ROOT, 'Assets', 'Editor', 'QA', 'Checks', 'CamChecks.cs')
QA_RUNNER = os.path.join(ROOT, 'Assets', 'Tests', 'PlayMode', 'Rig', 'QaRunner.cs')
DESK_PLAN_OPS = os.path.join(ROOT, 'Assets', 'Tests', 'PlayMode', 'DeskPlan', 'Rig', 'DeskPlanOps.cs')
SEQTOOLS = os.path.join(HERE, 'seqtools.py')
SCENARIOS = os.path.join(HERE, 'scenarios')

# CamOps' ops and the assertion families each writes ('<family><name>...'); ops with none write no line.
OP_FAMILIES = {
    'camres': [],
    'cammotion': [],
    'camcycle': [],
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

# The other owners' lines a CAM check reads on purpose: (check, pattern) -> the lines' owner. cam.play.deskplan judges
# DESK-PLAN's homes at A3 and A4 (FIX_PLAN v1 deferred them to CAM): each home seen and the intercom's wheel from the
# reading view (DESK-PLAN's lines), every click delivered and the run's last probe reached (QA-RIG's), as qa.play.smoke
# and RUN-SAFE's checks read a run's own lines. Nothing else of another owner.
DELIBERATE = {
    ('cam.play.deskplan', 'cam_desk.visible.*'): 'DESK-PLAN',
    ('cam.play.deskplan', 'cam_desk.expect.*'): 'DESK-PLAN',
    ('cam.play.deskplan', 'cam_desk.presses'): 'QA-RIG',
    ('cam.play.deskplan', 'cam_desk.missingScripts.end'): 'QA-RIG',
    ('cam.play.deskplan', 'cam_desk.materials.end'): 'QA-RIG',
}

Check = collections.namedtuple('Check', 'id owner patterns built_player kind demo')
Line = collections.namedtuple('Line', 'id owner how cam')


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


def _checks():
    """CamChecks' play and built-player checks, each with every result pattern it requires (GateResults.Require, or
    the local All over several patterns, which reads them under CAM)."""
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
    for cid, args in re.findall(r'\bAll\(\s*"([^"]+)"\s*,((?:\s*"[^"]+"\s*,?)+)\)', src):
        kind, demo = kinds[cid]
        checks.setdefault(cid, Check(cid, cam, [], False, kind, demo)).patterns.extend(re.findall(r'"([^"]+)"', args))
    return list(checks.values())


def run_lines(scenario, plates):
    """Every line a run of the scenario writes, with its owner, how it is judged ('play', 'bp' or 'editor-gc') and
    whether CAM measures it (CamOps' lines, and the built-in lines under CAM): the run's own lines at its end, each
    step's (CamOps' ops, the built-in ops, DESK-PLAN's ops), and the frame analyses of each recorded sequence; with a gc
    step the run is also played on the QA player, every line again with a bp. prefix."""
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
    on_player = any(s['op'].lower() == 'gc' for s in scenario['steps'])
    judged = []
    for rid, line_owner, cam in lines:
        # The editor's gc number includes the editor's own allocations (QaRunner.Gc's detail): its verdict is the [BP] twin's.
        judged.append(Line(f'{name}.{rid}', line_owner, 'editor-gc' if rid.startswith('gc.') else 'play', cam))
        if on_player:
            judged.append(Line(f'bp.{name}.{rid}', line_owner, 'bp', cam))
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
        self.assertEqual({'cam.bp.gc', 'cam.bp.camera'}, {c.id for c in bp})
        for c in bp:
            self.assertTrue(c.built_player and c.demo and all(p.startswith('bp.') for p in c.patterns), c.id)

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


if __name__ == '__main__':
    unittest.main()
