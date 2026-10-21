"""Tests for portal_check.py's fail-closed rules (no Unity): python -m unittest discover -s tools/hall

The orchestrator's ruling at the six tracks' landing (2026-10-21), on the HALL-FX verifier's finding 2: the portal
pass failed open. A ring wholly off the frame was printed as "not measured" and passed, so a projection moved off
screen passed every block. Now a ring off the frame FAILS unless declared with --allow-off for that size and proven
by a measured ring of the same frame, and a frame with no ring measured FAILS.
"""
import os
import shutil
import sys
import tempfile
import unittest

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import portal_check  # noqa: E402

EFFECTS = """   PortalEffect_05 showing True night 0.00 centre (1255.3,621.9) quad (1216.7,574.0)-(1294.5,670.4)
   PortalEffect_02 showing True night 0.00 centre (558.4,518.3) quad (535.4,499.1)-(581.6,537.5)
   PortalEffect_04 showing True night 0.00 centre (1094.5,619.4) quad (1069.9,579.7)-(1119.4,659.5)
   PortalEffect_01 showing True night 0.00 centre (641.4,437.0) quad (589.1,389.8)-(694.4,484.9)
   PortalEffect_03 showing True night 0.00 centre (718.7,517.4) quad (695.2,496.2)-(742.4,538.7)
"""
# The integration run's 1280x720 projection, moved 5000 px: every ring projects off the frame.
OFF_FRAME = '   drawing canvas 2172x724 corners (4889.174,5773.751) (6382.575,5773.751) (4936.191,5282.990) (6336.074,5282.990)\n'


def painting_is_here():
    try:
        return open(portal_check.PAINTING, 'rb').read(8)[1:4] == b'PNG'
    except OSError:
        return False


class BlockVerdictTests(unittest.TestCase):
    def test_a_ring_off_the_frame_fails_unless_declared(self):
        ok, lines = portal_check.block_verdict(1280, 1024, [(2, True)], [(5, 'past the right edge')], set())
        self.assertFalse(ok)
        self.assertIn('not declared with --allow-off: FAIL', lines[0])

    def test_a_declared_ring_off_the_frame_passes_when_a_measured_ring_proves_the_projection(self):
        ok, lines = portal_check.block_verdict(1280, 1024, [(2, True), (1, True)], [(5, 'past the right edge')], {(5, 1280, 1024)})
        self.assertTrue(ok, lines)
        self.assertIn('checked on portal 2', lines[0])

    def test_a_declaration_is_for_its_size_only(self):
        ok, _ = portal_check.block_verdict(1280, 720, [(2, True)], [(5, 'past the right edge')], {(5, 1280, 1024)})
        self.assertFalse(ok)

    def test_a_declared_ring_off_the_frame_fails_when_no_measured_ring_passed_its_painting_check(self):
        ok, lines = portal_check.block_verdict(1280, 1024, [(2, False)], [(5, 'past the right edge')], {(5, 1280, 1024)})
        self.assertFalse(ok)
        self.assertIn('no ring of this frame passed its painting check', lines[0])

    def test_a_frame_with_no_ring_measured_fails(self):
        ok, lines = portal_check.block_verdict(1280, 720, [], [(n, 'off') for n in (1, 2)], {(1, 1280, 720), (2, 1280, 720)})
        self.assertFalse(ok)
        self.assertTrue(any('no ring measured' in line for line in lines))

    def test_a_ring_the_frame_does_not_report_fails(self):
        ok, lines = portal_check.block_verdict(1920, 1080, [(1, True), (2, True)], [], set(), expected=[1, 2, 3], reported=[1, 2])
        self.assertFalse(ok)
        self.assertIn('portal 3: not reported', lines[0])

    def test_every_ring_measured_passes(self):
        ok, lines = portal_check.block_verdict(1920, 1080, [(1, True), (2, True)], [], set(), expected=[1, 2], reported=[1, 2])
        self.assertTrue(ok)
        self.assertEqual([], lines)

    def test_allow_off_reads_ring_at_size(self):
        self.assertEqual({(5, 1280, 1024), (4, 640, 480)}, portal_check.allowed_off(['5@1280x1024', ' 4@640x480 ']))
        with self.assertRaises(SystemExit):
            portal_check.allowed_off(['5 at 1280x1024'])


@unittest.skipUnless(painting_is_here(), 'HallWarmStone.png is a Git LFS pointer here')
class RunFolderTests(unittest.TestCase):
    """main() on a synthetic run folder: the verifier's demonstration (the projection moved 5000 px) and an empty report."""

    def setUp(self):
        self.run = tempfile.mkdtemp(prefix='portal_check_')
        os.makedirs(os.path.join(self.run, 'shots', 'portals'))
        blank = Image.fromarray(np.zeros((720, 1280, 3), np.uint8))
        for side in ('on', 'off'):
            blank.save(os.path.join(self.run, 'shots', 'portals', f'h12_1280x720_{side}.png'))

    def tearDown(self):
        shutil.rmtree(self.run, ignore_errors=True)

    def check(self, report, *extra):
        with open(os.path.join(self.run, 'qa_report.txt'), 'w', encoding='utf-8') as f:
            f.write(report)
        code = portal_check.main([self.run, '--out', os.path.join(self.run, 'out'), *extra])
        return code, open(os.path.join(self.run, 'out', 'portal_check.txt'), encoding='utf-8').read()

    def test_a_projection_moved_off_the_frame_fails(self):
        code, text = self.check('== portals at 12 1280x720\n' + EFFECTS + OFF_FRAME)
        self.assertEqual(1, code)
        self.assertEqual(5, text.count('not declared with --allow-off: FAIL'))
        self.assertIn('no ring measured at 1280x720: FAIL', text)
        self.assertTrue(text.rstrip().endswith('FAIL'))

    def test_declaring_every_ring_off_still_fails_with_nothing_measured(self):
        code, text = self.check('== portals at 12 1280x720\n' + EFFECTS + OFF_FRAME, *[f'--allow-off={n}@1280x720' for n in range(1, 6)])
        self.assertEqual(1, code)
        self.assertEqual(5, text.count('no ring of this frame passed its painting check: FAIL'))

    def test_a_report_with_no_portal_block_fails(self):
        code, text = self.check('== smoke\n   nothing here\n')
        self.assertEqual(1, code)
        self.assertIn('no "== portals at" block', text)


if __name__ == '__main__':
    unittest.main()
