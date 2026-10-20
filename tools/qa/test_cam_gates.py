"""CAM's gate coverage (docs/pending/CAM.md, round 4): every assertion a CAM rig op writes in CAM's scenarios sits
under one of CAM's gate checks (CamChecks' GateResults.Require patterns), so the merge gate cannot go green while a
run's own line is red or missing. Round 3's verifier found cam_views.play.d12 (the scripted day 12) under no gate.
Reads the C# sources and the scenario files; needs no Unity."""
import json
import os
import re
import unittest

import seqlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, '..', '..')
CAM_OPS = os.path.join(ROOT, 'Assets', 'Tests', 'PlayMode', 'Cam', 'Rig', 'CamOps.cs')
CAM_CHECKS = os.path.join(ROOT, 'Assets', 'Editor', 'QA', 'Checks', 'CamChecks.cs')
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
    'camchrome': ['chrome.'],
    'camtilts': ['tilts.'],
    'camplay': ['play.'],
    'camcap': ['capreport.'],
}

# A report, never a verdict of CAM's (always passes; DOC-LOOK's gate reads the cap heights at #9).
REPORTS = {'capreport.'}


def _read(path):
    with open(path, encoding='utf-8') as f:
        return f.read()


class CamGateCoverageTests(unittest.TestCase):

    def patterns(self):
        return re.findall(r'GateResults\.Require\(\s*"[^"]+"\s*,\s*Owner\s*,\s*"([^"]+)"', _read(CAM_CHECKS))

    def test_the_table_names_every_op_and_family_camops_has(self):
        src = _read(CAM_OPS)
        ops = {op.lower() for op in re.findall(r'RegisterOp\(\s*"([A-Za-z]+)"', src)}
        self.assertEqual(set(OP_FAMILIES), ops, 'a CamOps op added or gone: place it in OP_FAMILIES')
        families = set(re.findall(r'runner\.Assert\(\s*\$?"([a-z]+(?:\.[a-z0-9]+)*\.)', src))
        listed = {f for fs in OP_FAMILIES.values() for f in fs}
        self.assertEqual(listed, families, 'an assertion family added or gone in CamOps: place it in OP_FAMILIES')

    def test_every_cam_line_of_cams_scenarios_is_under_a_cam_gate(self):
        pats = self.patterns()
        self.assertIn('cam_views.play.*', pats, 'the scripted day 12 (camplay) needs its own gate (cam.play.day12)')
        files = sorted(f for f in os.listdir(SCENARIOS) if f.startswith('cam_') and f.endswith('.json'))
        self.assertTrue(files)
        seen = 0
        for name in files:
            s = json.loads(_read(os.path.join(SCENARIOS, name)))
            for i, step in enumerate(s['steps']):
                for family in OP_FAMILIES.get(step['op'].lower(), []):
                    if family in REPORTS:
                        continue
                    rid = f"{s['name']}.{family}{step.get('name') or 'x'}"
                    seen += 1
                    self.assertTrue(any(seqlib.matches(p, rid) for p in pats), f'{name} step {i}: {rid} is under no CamChecks gate')
        self.assertGreater(seen, 0)

    def test_the_day1_chrome_leg_names_the_guide_plate_it_shows(self):
        # A plate a leg shows must report a line even when it is never drawn (red 'not measured', never silence):
        # the day-1 leg names the guide's plate (the stamp hint is named by camchrome n = 2 itself).
        s = json.loads(_read(os.path.join(SCENARIOS, 'cam_chrome.json')))
        starts = [st for st in s['steps'] if st['op'] == 'camchrome' and st.get('n') == 1 and st.get('name') == 'a1_d1']
        self.assertEqual(1, len(starts))
        self.assertIn('guide', [k.strip() for k in starts[0].get('target', '').split(',')])
        hints = [st for st in s['steps'] if st['op'] == 'camchrome' and st.get('n') == 2]
        self.assertEqual(4, len(hints), 'each day-12 leg (A1-A4) shows the stamp hint')


if __name__ == '__main__':
    unittest.main()
