"""CAM's gate coverage (docs/pending/CAM.md, rounds 4 and 5): every line a run of CAM's scenarios writes that CAM
measures sits under one of the gate checks in CamChecks, under the line's owner, so the merge gate cannot go green
while such a line is red or missing. The lines: those CamOps' ops write (round 3's verifier found cam_views.play.d12
under no gate) and the CAM lines the rig's built-in ops write (round 4's: gc under the scenario's owner, cameraCheck
and the run's own camera line under CAM). A scenario with a gc step is also run on the QA player (the gc verdict is
[BP]): its CAM lines' bp. twins sit under a CAM BuiltPlayer check that reads built-player results. Reads the C#
sources and the scenario files; needs no Unity. CAM's file in QA-RIG's tools/qa (discover finds it here)."""
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

# The rig's built-in ops whose lines are CAM's in a CAM scenario (QaRunner): the family, the name when the step has
# none, and the owner ('scenario': the scenario's owner, QaRunner's Scenario.owner).
BUILT_IN = {
    'cameracheck': ('camera.', None, 'CAM'),
    'gc': ('gc.', 'steady', 'scenario'),
    'drop': ('drop.', 'drag', 'scenario'),
}


def _read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


def _safe(name):
    # QaRunner.Safe: anything but a letter, digit, '_' or '-' becomes '_'.
    return re.sub(r'[^A-Za-z0-9_-]', '_', name)


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
    """CamChecks' play and built-player checks: (check id, owner, result pattern, built player, kind, demo only)."""
    src = _read(CAM_CHECKS)
    cam = re.search(r'const\s+string\s+Owner\s*=\s*"([^"]+)"', src).group(1)
    owner = lambda o: cam if o == 'Owner' else o.strip('"')
    kinds = {cid: (kind or 'Static', bool(demo)) for cid, kind, demo in re.findall(
        r'\[GateCheck\(\s*"([^"]+)"\s*,\s*(?:Owner|"[^"]+")\s*(?:,\s*GateKind\.(\w+))?\s*(,\s*DemoOnly\s*=\s*true)?\s*\)\]', src)}
    checks = []
    for cid, o, pattern, bp in re.findall(
            r'GateResults\.Require\(\s*"([^"]+)"\s*,\s*(Owner|"[^"]+")\s*,\s*"([^"]+)"\s*(?:,\s*(true)\s*)?\)', src):
        kind, demo = kinds[cid]
        checks.append((cid, owner(o), pattern, bp == 'true', kind, demo))
    return checks


def cam_lines(scenario, plates):
    """(line id, owner, how it is judged: 'play', 'bp' or 'editor-gc') of every line a run of the scenario writes that
    CAM measures: CamOps' ops and the built-in ops' CAM lines, in the editor and, with a gc step, on the QA player."""
    name, owner = scenario['name'], scenario.get('owner', 'QA-RIG')
    lines = [('camera', 'CAM')]  # QaRunner.Finish: the run's own camera line
    for step in scenario['steps']:
        op, step_name = step['op'].lower(), step.get('name')
        if op == 'camchrome':
            if step.get('n', 0) > 0:
                continue  # a leg reports at its end (n = 0)
            leg = step_name or 'chrome'
            lines.append((f'chrome.{leg}', 'CAM'))
            for key, plate_owner in plates:
                lines.append((f'chrome.{leg}.{key}', 'CAM'))  # a plate the leg showed but never drew: not measured
                lines.append((f'plate.{leg}.{key}', plate_owner))  # a plate drawn in the leg
            continue
        for family in OP_FAMILIES.get(op, []):
            lines.append((f'{family}{step_name or "x"}', 'CAM'))
        if op in BUILT_IN:
            family, fallback, line_owner = BUILT_IN[op]
            line_owner = owner if line_owner == 'scenario' else line_owner
            if line_owner == 'CAM':
                lines.append((f'{family}{_safe(step_name or fallback)}', line_owner))
    on_player = any(s['op'].lower() == 'gc' for s in scenario['steps'])
    judged = []
    for rid, line_owner in lines:
        # The editor's gc number includes the editor's own allocations (QaRunner.Gc's detail): its verdict is the [BP] twin's.
        judged.append((f'{name}.{rid}', line_owner, 'editor-gc' if rid.startswith('gc.') else 'play'))
        if on_player:
            judged.append((f'bp.{name}.{rid}', line_owner, 'bp'))
    return judged


class CamGateCoverageTests(unittest.TestCase):

    def test_the_table_names_every_op_and_family_camops_has(self):
        src = _read(CAM_OPS)
        ops = {op.lower() for op in re.findall(r'RegisterOp\(\s*"([A-Za-z]+)"', src)}
        self.assertEqual(set(OP_FAMILIES), ops, 'a CamOps op added or gone: place it in OP_FAMILIES')
        families = set(re.findall(r'(?:runner\.Assert|new ChromeLine)\(\s*\$?"([a-z]+(?:\.[a-z0-9]+)*\.)', src))
        listed = {f for fs in OP_FAMILIES.values() for f in fs}
        self.assertEqual(listed, families, 'an assertion family added or gone in CamOps: place it in OP_FAMILIES')

    def test_the_built_in_table_matches_the_rigs_cam_lines(self):
        # QaRunner's lines a CAM scenario owns: cameraCheck's and the run's camera (always CAM), gc's and drop's
        # (the scenario's owner). Anything else the runner writes names QA-RIG or TEXT-LANG.
        src = _read(QA_RUNNER)
        self.assertRegex(src, r'case "cameracheck":\s*AssertCamera\("camera\." \+ Safe\(s\.name\)\)')
        self.assertRegex(src, r'AssertCamera\("camera"\);')
        self.assertRegex(src, r'Assert\(id, "CAM", r\.Pass')
        self.assertRegex(src, r'"gc\." \+ Safe\(string\.IsNullOrEmpty\(s\.name\) \? "steady" : s\.name\)')
        self.assertRegex(src, r'Assert\(\$"drop\.\{name\}", Scenario\.owner')
        owners = set(re.findall(r'\bAssert\([^;]*?,\s*("[A-Z-]+"|Scenario\.owner)\s*,', src))
        self.assertEqual({'"QA-RIG"', '"TEXT-LANG"', '"CAM"', 'Scenario.owner'}, owners, 'a built-in line with a new owner: place it in BUILT_IN')

    def test_every_cam_line_of_cams_scenarios_is_under_a_cam_gate_of_its_owner(self):
        checks = _checks()
        patterns = [c[2] for c in checks]
        self.assertIn('cam_views.play.*', patterns, 'the scripted day 12 (camplay) needs its own gate (cam.play.day12)')
        plates = _plates()
        self.assertEqual({'guide': 'CONTENT', 'stamphint': 'STAMP'}, dict(plates))
        files = sorted(f for f in os.listdir(SCENARIOS) if f.startswith('cam_') and f.endswith('.json'))
        self.assertTrue(files)
        seen = {'play': 0, 'bp': 0, 'editor-gc': 0}
        for file in files:
            s = json.loads(_read(os.path.join(SCENARIOS, file)))
            self.assertEqual('CAM', s.get('owner'), f'{file}: a CAM scenario names CAM its owner (gc lines go to it)')
            for rid, owner, how in cam_lines(s, plates):
                seen[how] += 1
                if how == 'editor-gc':
                    continue  # its bp. twin is checked below
                bp = how == 'bp'
                under = [c for c in checks if seqlib.matches(c[2], rid) and c[3] == bp
                         and c[4] == ('BuiltPlayer' if bp else 'Play')]
                self.assertTrue(under, f'{file}: {rid} is under no CamChecks {"BuiltPlayer" if bp else "Play"} gate')
                self.assertIn(owner, {c[1] for c in under}, f'{file}: {rid} ({owner}) is only under another owner\'s gate: {under}')
        self.assertGreater(seen['play'], 0)
        self.assertGreater(seen['bp'], 0, 'cam_gc runs on the QA player: its gc and camera lines need [BP] gates')
        self.assertGreater(seen['editor-gc'], 0)

    def test_the_built_player_checks_run_with_the_demo_gate(self):
        # QA-RIG's convention (qa.bp.*; FIX_PLAN: "[BP] gates run on the nightly scratch build"), and they read
        # built-player results only.
        bp = [c for c in _checks() if c[4] == 'BuiltPlayer']
        self.assertEqual({'cam.bp.gc', 'cam.bp.camera'}, {c[0] for c in bp})
        for cid, _, pattern, built_player, _, demo in bp:
            self.assertTrue(built_player and demo and pattern.startswith('bp.'), cid)

    def test_the_chrome_op_reports_what_its_steps_show_through_the_pure_report(self):
        # The wiring CamRigTests cannot reach (the op itself needs the editor): each camchrome step's expectations come
        # from ShownBy, and the leg's end asserts every line LegLines returns, as is.
        src = _read(CAM_OPS)
        body = re.search(r'private static IEnumerator Chrome\(QaRunner runner, QaStep step\)\s*\{(.*?)\n    \}\n', src, re.S).group(1)
        self.assertRegex(body, r'List<string> shows = ShownBy\(step\.n, step\.target\);')
        self.assertRegex(body, r'_chrome\.Shows\.UnionWith\(shows\);')
        self.assertRegex(body, r'foreach \(ChromeLine line in LegLines\(name, done\.Shows, tally, ')
        self.assertRegex(body, r'runner\.Assert\(line\.Id, line\.Owner, line\.Pass, line\.Measured, line\.Detail\);')

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
