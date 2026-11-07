"""Unit tests for the QA-RIG frame analysis on synthetic frames (no Unity): python -m unittest discover tools/qa"""
import json
import os
import re
import shutil
import sys
import tempfile
import unittest

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import seqlib  # noqa: E402
import seqtools  # noqa: E402

H, W = 54, 96


def flat(v, n):
    return [np.full((H, W), float(v), dtype=np.float32) for _ in range(n)]


def state(rects=None, mouse=None):
    return {'rects': rects or {}, 'mouse': mouse}


class FlickerTests(unittest.TestCase):
    def test_still_sequence_passes(self):
        r = seqlib.flicker(flat(100, 40))
        self.assertTrue(r['pass'])
        self.assertEqual(0, r['toggles'])

    def test_a_blinking_block_fails_and_is_located(self):
        frames = flat(100, 40)
        for i in range(5, 40, 4):  # a one-frame blink every 4 frames
            frames[i][10:20, 30:40] = 200
        r = seqlib.flicker(frames)
        self.assertFalse(r['pass'])
        self.assertEqual(100, r['bad_px'])
        blocks = seqlib.hot_blocks(r['heat'])
        self.assertTrue(any(16 <= x <= 32 and y <= 16 for _, x, y in blocks))

    def test_a_three_frame_excursion_counts_a_four_frame_one_does_not(self):
        three = flat(100, 30)
        four = flat(100, 30)
        for i in (5, 6, 7):
            three[i][:, :] = 160
        for i in (5, 6, 7, 8):
            four[i][:, :] = 160
        self.assertEqual(H * W, seqlib.flicker(three, max_toggles=0)['bad_px'])
        self.assertEqual(0, seqlib.flicker(four, max_toggles=0)['bad_px'])

    def test_a_permanent_change_is_not_a_toggle(self):
        frames = flat(100, 10) + flat(200, 10)
        self.assertEqual(0, seqlib.flicker(frames)['toggles'])

    def test_twice_in_ten_seconds_passes_three_times_fails(self):
        def blinks(k):
            frames = flat(100, 60)
            for i in range(k):
                frames[10 + i * 10][0:4, 0:4] = 220
            return frames
        self.assertTrue(seqlib.flicker(blinks(2))['pass'])
        self.assertFalse(seqlib.flicker(blinks(3))['pass'])

    def test_blinks_spread_over_more_than_ten_seconds_pass(self):
        frames = flat(100, 700)
        for i in (10, 320, 640):
            frames[i][0:4, 0:4] = 220
        self.assertTrue(seqlib.flicker(frames)['pass'])

    def test_a_moving_tracked_object_is_masked(self):
        frames = flat(100, 30)
        states = []
        for i in range(30):
            x = 10 + (i % 2) * 2  # an object jittering two pixels: its area changes every frame
            frames[i][20:30, x:x + 10] = 220
            states.append(state({'paper': (x, 20, x + 10, 30)}))
        self.assertFalse(seqlib.flicker(frames)['pass'])
        self.assertTrue(seqlib.flicker(frames, states)['pass'])

    def test_a_still_tracked_object_is_not_masked(self):
        frames = flat(100, 30)
        for i in range(3, 30, 3):
            frames[i][20:30, 10:20] = 220
        states = [state({'paper': (10, 20, 20, 30)}) for _ in range(30)]
        self.assertFalse(seqlib.flicker(frames, states)['pass'])

    def test_static_mask_and_static_only(self):
        frames = flat(100, 30)
        for i in range(3, 30, 3):
            frames[i][0:5, 0:5] = 220
        mask = np.zeros((H, W), dtype=bool)
        mask[0:5, 0:5] = True
        self.assertTrue(seqlib.flicker(frames, static_mask=mask)['pass'])
        moving = [state({'p': (0, 0, 1 + i % 2, 1)}) for i in range(30)]
        self.assertTrue(seqlib.flicker(frames, moving, static_only=True)['pass'])


class FlashTests(unittest.TestCase):
    @staticmethod
    def strobe(period_frames, n=60, colour=(255, 255, 255), dark=(0, 0, 0)):
        frames = []
        for i in range(n):
            on = (i // (period_frames // 2)) % 2 == 0
            frames.append(np.full((H, W, 3), colour if on else dark, dtype=np.uint8))
        return frames

    def test_no_change_no_flash(self):
        r = seqlib.flash([np.full((H, W, 3), 90, dtype=np.uint8)] * 30)
        self.assertTrue(r['pass'])
        self.assertEqual(0, r['worst_per_s'])

    def test_a_slow_strobe_passes_a_fast_one_fails(self):
        slow = seqlib.flash(self.strobe(12))  # 2.5 flashes/s
        fast = seqlib.flash(self.strobe(6))   # 5 flashes/s
        self.assertTrue(slow['pass'], slow)
        self.assertLessEqual(slow['worst_per_s'], 3)
        self.assertFalse(fast['pass'])
        self.assertGreaterEqual(fast['worst_per_s'], 4)
        self.assertTrue(fast['full_field'])

    def test_a_local_strobe_is_caught_in_its_cell(self):
        frames = []
        for i in range(60):
            f = np.full((H, W, 3), 60, dtype=np.uint8)
            if (i // 3) % 2 == 0:
                f[0:9, 0:12] = 250
            frames.append(f)
        r = seqlib.flash(frames)
        self.assertFalse(r['pass'])
        self.assertIn('0,0', r['cells'])
        self.assertFalse(r['full_field'])

    def test_a_fade_is_not_a_flash(self):
        frames = [np.full((H, W, 3), int(v), dtype=np.uint8) for v in np.linspace(0, 255, 60)] * 2
        self.assertEqual(0, seqlib.flash(frames[:60])['worst_per_s'])

    def test_a_red_full_field_flash_fails_even_once(self):
        frames = [np.full((H, W, 3), 30, dtype=np.uint8) for _ in range(30)]
        for i in (10, 11):
            frames[i] = np.full((H, W, 3), (255, 0, 0), dtype=np.uint8)
        r = seqlib.flash(frames)
        self.assertEqual(1, r['worst_per_s'])
        self.assertTrue(r['red'])
        self.assertFalse(r['pass'])


class LatencyTests(unittest.TestCase):
    @staticmethod
    def seq(change_at, press_at=10, n=20, mouse=(80.0, 40.0)):
        lumas = flat(50, n)
        for i in range(change_at, n):
            lumas[i][5:15, 5:15] = 200
        rows = [{'i': str(i), 'frame': str(1000 + i), 'mx': str(mouse[0]), 'my': str(mouse[1]), 'tracked': ''} for i in range(n)]
        events = [{'frame': str(1000 + press_at), 'channel': 'mouse', 'name': 'left'}]
        return lumas, rows, events

    def test_change_on_the_press_frame_is_zero(self):
        r = seqlib.latency(*self.seq(10))
        self.assertTrue(r['pass'])
        self.assertEqual(0, r['presses'][0]['visual_frames'])

    def test_change_two_frames_late_fails(self):
        r = seqlib.latency(*self.seq(12))
        self.assertFalse(r['pass'])
        self.assertEqual(2, r['presses'][0]['visual_frames'])

    def test_a_change_under_the_pointer_only_is_not_a_response(self):
        lumas, rows, events = self.seq(30, n=20)
        for i in range(10, 20):
            lumas[i][35:45, 75:85] = 220  # the hover highlight under the pointer
        r = seqlib.latency(lumas, rows, events)
        self.assertIsNone(r['presses'][0]['visual_frames'])
        self.assertFalse(r['pass'])

    def test_the_cue_must_be_on_the_press_frame_plus_or_minus_one_when_asked(self):
        lumas, rows, events = self.seq(10)
        late = events + [{'frame': '1013', 'channel': 'audio', 'name': 'stamp'}]
        on_time = events + [{'frame': '1011', 'channel': 'audio', 'name': 'stamp'}]
        self.assertFalse(seqlib.latency(lumas, rows, late, need_cue=True)['pass'])
        self.assertTrue(seqlib.latency(lumas, rows, on_time, need_cue=True)['pass'])

    def test_the_recorders_cue_rows_are_cues(self):
        lumas, rows, events = self.seq(10)
        r = seqlib.latency(lumas, rows, events + [{'frame': '1010', 'channel': 'cue', 'name': 'stamp_approve'}], need_cue=True)
        self.assertTrue(r['pass'])
        self.assertEqual(0, r['presses'][0]['cue_frames'])


class SoundTests(unittest.TestCase):
    """The press-to-sound check (GAME_FEEL_BIBLE check 2: the cue fires on the press frame +-1; GATE-13)."""

    PRESS = [{'frame': '500', 'channel': 'mouse', 'name': 'left'}]

    def test_a_cue_on_the_press_frame_or_the_next_is_on_time(self):
        for at in ('499', '500', '501'):
            r = seqlib.sound(self.PRESS + [{'frame': at, 'channel': 'cue', 'name': 'stamp_approve'}])
            self.assertTrue(r['pass'], at)
            self.assertEqual('on time', r['presses'][0]['state'])
            self.assertEqual('stamp_approve', r['presses'][0]['cue'])

    def test_a_cue_later_in_the_window_is_late_and_fails(self):
        r = seqlib.sound(self.PRESS + [{'frame': '504', 'channel': 'cue', 'name': 'stamp_approve'}])
        self.assertFalse(r['pass'])
        self.assertEqual(('late', 4), (r['presses'][0]['state'], r['presses'][0]['cue_frames']))

    def test_no_cue_is_silent_and_fails_only_when_a_cue_is_needed(self):
        far = self.PRESS + [{'frame': '520', 'channel': 'cue', 'name': 'footsteps_arrive'}, {'frame': '497', 'channel': 'cue', 'name': 'x'}]
        r = seqlib.sound(far)
        self.assertTrue(r['pass'])
        self.assertEqual('silent', r['presses'][0]['state'])
        self.assertEqual(2, r['cues'])
        self.assertFalse(seqlib.sound(far, need_cue=True)['pass'])

    def test_the_older_audio_channel_counts_and_other_channels_do_not(self):
        self.assertEqual('on time', seqlib.sound(self.PRESS + [{'frame': '500', 'channel': 'audio', 'name': 'a'}])['presses'][0]['state'])
        self.assertEqual('silent', seqlib.sound(self.PRESS + [{'frame': '500', 'channel': 'qa', 'name': 'a'}])['presses'][0]['state'])

    def test_no_press_passes_with_nothing_to_judge(self):
        r = seqlib.sound([{'frame': '500', 'channel': 'cue', 'name': 'a'}])
        self.assertTrue(r['pass'])
        self.assertEqual([], r['presses'])


class FrozenCutsBlockTests(unittest.TestCase):
    def test_frozen_finds_the_longest_still_run(self):
        frames = flat(10, 5) + [np.full((H, W), 60.0, dtype=np.float32)] * 12 + flat(100, 3)
        r = seqlib.frozen([f + (i % 2) * 5 if i < 5 else f for i, f in enumerate(frames)])
        self.assertEqual(12, r['frames'])
        self.assertAlmostEqual(0.4, r['seconds'], places=3)

    def test_cuts_finds_a_full_screen_jump(self):
        frames = flat(10, 5) + flat(200, 5)
        self.assertEqual([5], [i for i, _ in seqlib.cuts(frames)])

    def test_a_one_frame_pop_is_one_cut(self):
        frames = flat(60, 10)
        frames[4] = frames[4].copy()
        frames[4][:, :W // 2] = 200  # half the screen pops for one frame: the mean jumps by 70 and comes back
        self.assertEqual([4], [i for i, _ in seqlib.cuts(frames)])

    def test_an_eased_fade_or_a_steady_pan_is_no_cut(self):
        fade = [np.full((H, W), float(v), dtype=np.float32) for v in (10, 10, 30, 70, 130, 180, 200, 200)]
        self.assertEqual([], seqlib.cuts(fade))
        x = np.arange(W, dtype=np.float32)
        pan = [np.tile((x * 3 + i * 40) % 256, (H, 1)) for i in range(12)]  # a texture sliding 13 px a frame
        self.assertEqual([], seqlib.cuts(pan))

    def test_the_threshold_is_a_knob(self):
        frames = flat(100, 5) + flat(110, 5)  # a 10-level step
        self.assertEqual([], seqlib.cuts(frames))
        self.assertEqual([5], [i for i, _ in seqlib.cuts(frames, th=8)])

    def test_blockiness_is_about_one_for_smooth_art_and_high_for_blocks(self):
        x = np.linspace(0, 255, 64)
        smooth = np.dstack([np.tile(x, (64, 1))] * 3 + [np.full((64, 64), 255.0)]).astype(np.uint8)
        blocks = smooth.astype(np.int32)
        steps = np.repeat(np.repeat((np.arange(64).reshape(8, 8) % 2) * 24, 8, 0), 8, 1)  # a 24-level step at every block edge
        blocks[:, :, :3] = np.clip(blocks[:, :, :3] // 2 + steps[..., None], 0, 255)
        blocks = blocks.astype(np.uint8)
        self.assertLess(seqlib.blockiness(smooth), 1.5)
        self.assertGreater(seqlib.blockiness(blocks), 3)


class HitchTests(unittest.TestCase):
    def test_frames_over_one_frame_are_hitches(self):
        rows = [{'i': str(i), 'dtMs': '16.7'} for i in range(20)]
        rows[7]['dtMs'] = '48'
        r = seqlib.hitches(rows)
        self.assertEqual([(7, 48.0)], r['over'])
        self.assertAlmostEqual(16.7, r['median'], places=3)


class ResultTests(unittest.TestCase):
    def test_run_import_prefixes_built_player_ids(self):
        run = {'assertions': [{'id': 'bp_smoke.console', 'owner': 'QA-RIG', 'pass': True, 'measured': 'errors 0', 'detail': ''}]}
        r = seqlib.results_from_run(run, 'abc', 'dir', True)[0]
        self.assertEqual('bp.bp_smoke.console', r['id'])
        self.assertTrue(r['builtPlayer'])
        self.assertEqual('abc', r['commit'])
        self.assertFalse(r['dirty'])
        self.assertEqual(set(r), {'id', 'owner', 'pass', 'measured', 'detail', 'commit', 'dirty', 'written', 'source', 'builtPlayer', 'na'})
        self.assertFalse(r['na'])
        self.assertTrue(seqlib.results_from_run(run, 'abc', 'dir', True, True)[0]['dirty'])

    def test_a_not_applicable_assertion_is_a_neutral_result_never_a_red_one(self):
        run = {'assertions': [{'id': 'desk_plan_homes.visible.ring', 'owner': 'DESK-PLAN', 'pass': True, 'na': True,
                               'measured': 'n/a: not applicable on the painted desk', 'detail': 'the panel shows nothing'}]}
        r = seqlib.results_from_run(run, 'abc', 'dir', False)[0]
        self.assertTrue(r['na'])
        self.assertTrue(r['pass'], 'written as pass, so a reader that knows no n/a never counts it red')
        self.assertTrue(seqlib.gate_result('x', 'o', False, 'm', 'd', na=True)['pass'])

    def test_file_names_match_the_csharp_side(self):
        self.assertEqual('bp.a_b.c.json', seqlib.result_file_name('bp.a b.c'))


class KnownRedTests(unittest.TestCase):
    """The known-red run patterns (DemoGate.KnownRedRunPatterns / JudgeRun's Python twin, for run_player.py)."""

    TEXT = '# h\nqa.play.camera   *.camera *.camera.*  # BUG-17, CAM\nqa.bp.x  # BUG-2\nqa.y bp.*.seq.*.flicker *.camera\n'

    def test_patterns_are_the_words_after_the_check(self):
        self.assertEqual(['*.camera', '*.camera.*', 'bp.*.seq.*.flicker'], seqlib.known_red_run_patterns(self.TEXT))

    def test_matches_is_the_csharp_wildcard(self):
        self.assertTrue(seqlib.matches('*.camera', 'bp_smoke.camera'))
        self.assertFalse(seqlib.matches('*.camera', 'bp_smoke.cameraX'))
        self.assertTrue(seqlib.matches('bp.*.seq.*.flicker', 'BP.bp_smoke.seq.h2.flicker'))

    def test_only_known_red_passes_a_regression_or_a_failed_step_does_not(self):
        pats = seqlib.known_red_run_patterns(self.TEXT)
        run = {'ok': False, 'steps': [{'i': 0, 'op': 'title', 'ok': True}],
               'assertions': [{'id': 'bp_smoke.camera', 'pass': False}, {'id': 'bp_smoke.console', 'pass': True}]}
        unexpected, known = seqlib.judge_run(run, pats)
        self.assertEqual([], unexpected)
        self.assertEqual(1, len(known))
        run['assertions'].append({'id': 'bp_smoke.presses', 'pass': False})
        self.assertEqual(1, len(seqlib.judge_run(run, pats)[0]))
        run = {'ok': False, 'steps': [{'i': 4, 'op': 'sign', 'ok': False, 'note': 'timed out'}], 'assertions': []}
        self.assertIn('step 4 sign', seqlib.judge_run(run, pats)[0][0])
        self.assertEqual(['the run stopped before its last step'], seqlib.judge_run({'ok': False, 'steps': [], 'assertions': []}, pats)[0])

    def test_the_projects_list_spares_the_open_parts_only(self):
        with open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'gate_known_red.txt'), encoding='utf-8') as f:
            pats = seqlib.known_red_run_patterns(f.read())
        for rid in ('bp_smoke.camera', 'bp.bp_smoke.seq.handover.flicker'):
            self.assertTrue(any(seqlib.matches(p, rid) for p in pats), rid)
        # qa.play.liberationSans left the list (gate-hygiene, 2026-11-07: the smoke day's three lines pass), so the
        # built player's smoke day reads its fallback lines strictly too.
        for rid in ('bp_smoke.console', 'bp_smoke.presses', 'bp_smoke.liberationSans.desk', 'bp.bp_smoke.gc.steady', 'bp.bp_smoke.seq.callnext.hitches',
                    'bp.bp_smoke.seq.x.flash'):
            self.assertFalse(any(seqlib.matches(p, rid) for p in pats), rid)


class FrameFileTests(unittest.TestCase):
    def test_only_numbered_frames_are_frames(self):
        root = tempfile.mkdtemp(prefix='qarig_frames_')
        try:
            for name in ('f0000.png', 'f0001.png', 'f12345.png', 'flicker_heat.png', 'static_flicker_heat.png', 'f.png', 'f01.png', 'f0002.jpg'):
                Image.fromarray(np.zeros((4, 4, 3), dtype=np.uint8)).save(os.path.join(root, name))
            self.assertEqual(['f0000.png', 'f0001.png', 'f12345.png'], [os.path.basename(f) for f in seqlib.frame_files(root)])
            self.assertEqual([], seqlib.frame_files(os.path.join(root, 'missing')))
        finally:
            shutil.rmtree(root, ignore_errors=True)


class CliTests(unittest.TestCase):
    """seqtools.py end to end on a sequence folder written like SeqRecorder writes it."""

    def setUp(self):
        self.root = tempfile.mkdtemp(prefix='qarig_')
        self.seq = os.path.join(self.root, 'smoke_day1_20261020_120000', 'seq', 'idle')
        os.makedirs(self.seq)
        rows = ['i\tframe\ttime\tunscaled\ttimeScale\tscreenW\tscreenH\tcapW\tcapH\tmx\tmy\tdown\tgcBytes\tmainMs\tdtMs\ttracked\textra']
        for i in range(30):
            a = np.full((H, W, 3), 80, dtype=np.uint8)
            if i in (8, 9, 20):
                a[0:6, 0:6] = 240  # blinks: frames 8-9 is one excursion, 20 another
            Image.fromarray(a).save(os.path.join(self.seq, f'f{i:04d}.png'))
            rows.append(f'{i}\t{500 + i}\t0\t0\t1\t1920\t1080\t{W}\t{H}\t90\t50\t0\t0\t1\t33\t\t')
        with open(os.path.join(self.seq, 'frames.tsv'), 'w', encoding='utf-8') as f:
            f.write('\n'.join(rows) + '\n')
        with open(os.path.join(self.seq, 'events.tsv'), 'w', encoding='utf-8') as f:
            f.write('frame\ti\tunscaled\tchannel\tname\n')

    def tearDown(self):
        shutil.rmtree(self.root, ignore_errors=True)

    def test_flicker_writes_a_result_with_the_default_id(self):
        results = os.path.join(self.root, 'results')
        code = seqtools.main(['flicker', self.seq, '--max-toggles', '1', '--result-dir', results, '--commit', 'abc', '--bp'])
        self.assertEqual(1, code)
        with open(os.path.join(results, 'bp.smoke_day1.seq.idle.flicker.json'), encoding='utf-8') as f:
            r = json.load(f)
        self.assertFalse(r['pass'])
        self.assertTrue(r['builtPlayer'])
        self.assertTrue(os.path.exists(os.path.join(self.seq, 'flicker_heat.png')))

    def test_all_passes_at_the_default_limits_and_skips_latency_without_presses(self):
        self.assertEqual(0, seqtools.main(['all', self.seq]))

    def test_all_runs_cuts_and_sound_and_never_reads_its_heat_map_as_a_frame(self):
        results = os.path.join(self.root, 'results')
        seqtools.main(['flicker', self.seq])  # writes flicker_heat.png into the sequence
        self.assertTrue(os.path.exists(os.path.join(self.seq, 'flicker_heat.png')))
        Image.fromarray(np.full((H, W, 3), 255, dtype=np.uint8)).save(os.path.join(self.seq, 'flicker_heat.png'))
        with open(os.path.join(self.seq, 'events.tsv'), 'a', encoding='utf-8') as f:
            f.write('510\t10\t1\tmouse\tleft\n510\t10\t1\tcue\tstamp_approve\n')
        self.assertEqual(0, seqtools.main(['all', self.seq, '--result-dir', results, '--commit', 'abc']))
        names = sorted(os.listdir(results))
        self.assertIn('smoke_day1.seq.idle.cuts.json', names)
        self.assertIn('smoke_day1.seq.idle.sound.json', names)
        with open(os.path.join(results, 'smoke_day1.seq.idle.cuts.json'), encoding='utf-8') as f:
            cut = json.load(f)
        self.assertTrue(cut['pass'], 'a white heat map read as a last frame would be a cut')
        self.assertIn('in 30 frames', cut['measured'])

    def test_all_skips_cuts_on_a_sequence_recorded_with_motion_off(self):
        with open(os.path.join(self.seq, 'meta.json'), 'w', encoding='utf-8') as f:
            f.write('{"fixedStep": true, "frames": true, "fps": 30, "motionStill": true}\n')
        self.assertTrue(seqlib.motion_still(self.seq))
        Image.fromarray(np.full((H, W, 3), 250, dtype=np.uint8)).save(os.path.join(self.seq, 'f0015.png'))
        results = os.path.join(self.root, 'results')
        self.assertEqual(1, seqtools.main(['cuts', self.seq]))
        seqtools.main(['all', self.seq, '--result-dir', results, '--commit', 'abc'])
        self.assertNotIn('smoke_day1.seq.idle.cuts.json', os.listdir(results))

    def test_colour_holds_a_frame_to_the_still_of_the_same_picture(self):
        # The still is the screen at twice the frame's size; frame 12 was captured with it (the recorder's 'still' row).
        stills = os.path.join(self.root, 'smoke_day1_20261020_120000', 'stills')
        os.makedirs(stills)
        still = np.zeros((H * 2, W * 2, 3), dtype=np.uint8)
        still[:, :, 0], still[:, :, 1], still[:, :, 2] = 55, 64, 36  # the mat's colour in a still (GATE-05)
        still[: H, : W] = (180, 120, 40)
        Image.fromarray(still).save(os.path.join(stills, 'rest_1920x1080.png'))
        with open(os.path.join(self.seq, 'events.tsv'), 'a', encoding='utf-8') as f:
            f.write('512\t12\t1\tstill\trest_1920x1080.png\n')
        once = np.asarray(Image.fromarray(still).resize((W, H), Image.BOX))
        Image.fromarray(once).save(os.path.join(self.seq, 'f0012.png'))
        results = os.path.join(self.root, 'results')
        self.assertEqual(0, seqtools.main(['colour', self.seq, '--result-dir', results, '--commit', 'abc', '--colour-roi', '0.6,0.6,0.9,0.9']))
        with open(os.path.join(results, 'smoke_day1.seq.idle.colour.json'), encoding='utf-8') as f:
            self.assertIn('worst mean RGB difference 0 ', json.load(f)['measured'])
        # The old recorder's second sRGB encode: 55, 64, 36 read 127, 137, 105.
        twice = np.round(255 * np.where(once / 255 <= 0.0031308, once / 255 * 12.92, 1.055 * (once / 255) ** (1 / 2.4) - 0.055)).astype(np.uint8)
        Image.fromarray(twice).save(os.path.join(self.seq, 'f0012.png'))
        self.assertEqual(1, seqtools.main(['colour', self.seq]))
        c = seqlib.colour_match(twice, still, (0.6, 0.6, 0.9, 0.9))
        self.assertEqual((55.0, 64.0, 36.0), c['roi_still'])
        self.assertGreater(c['roi_diff'], 60)

    def test_colour_skips_a_sequence_with_no_still(self):
        self.assertEqual(0, seqtools.main(['colour', self.seq]))
        self.assertEqual([], seqtools.still_rows(self.seq))

    def test_dirty_marks_the_result(self):
        results = os.path.join(self.root, 'results')
        seqtools.main(['flicker', self.seq, '--result-dir', results, '--commit', 'abc', '--dirty'])
        with open(os.path.join(results, 'smoke_day1.seq.idle.flicker.json'), encoding='utf-8') as f:
            self.assertTrue(json.load(f)['dirty'])

    def test_all_on_a_timing_sequence_measures_hitches_only(self):
        timing = os.path.join(self.root, 'smoke_day1_20261020_120000', 'seq', 'callnext')
        os.makedirs(timing)
        rows = ['i\tframe\ttime\tunscaled\ttimeScale\tscreenW\tscreenH\tcapW\tcapH\tmx\tmy\tdown\tgcBytes\tmainMs\tdtMs\ttracked\textra']
        rows += [f'{i}\t{600 + i}\t0\t0\t1\t1920\t1080\t0\t0\t90\t50\t0\t0\t1\t{48 if i == 5 else 16.7}\t\t' for i in range(20)]
        with open(os.path.join(timing, 'frames.tsv'), 'w', encoding='utf-8') as f:
            f.write('\n'.join(rows) + '\n')
        with open(os.path.join(timing, 'events.tsv'), 'w', encoding='utf-8') as f:
            f.write('frame\ti\tunscaled\tchannel\tname\n')
        with open(os.path.join(timing, 'meta.json'), 'w', encoding='utf-8') as f:
            f.write('{"fixedStep": false, "frames": false, "fps": 30}\n')
        self.assertTrue(seqlib.is_timing(timing))
        self.assertFalse(seqlib.is_timing(self.seq))
        results = os.path.join(self.root, 'results')
        self.assertEqual(1, seqtools.main(['all', timing, '--result-dir', results, '--commit', 'abc']))
        self.assertEqual(['smoke_day1.seq.callnext.hitches.json'], os.listdir(results))

    def test_gif_sheet_and_side_by_side(self):
        self.assertEqual(0, seqtools.main(['gif', self.seq, os.path.join(self.root, 'a.gif'), '--scale', '1']))
        self.assertEqual(0, seqtools.main(['sheet', self.seq, os.path.join(self.root, 'a.png')]))
        self.assertEqual(0, seqtools.main(['side', self.seq, self.seq, os.path.join(self.root, 'side.gif'), '--height', '54']))


class ScenarioTests(unittest.TestCase):
    """Every scenario file is valid JSON, named after its file, and uses only the runner's ops (QaRunner.Ops)."""

    HERE = os.path.dirname(os.path.abspath(__file__))

    def ops(self):
        with open(os.path.join(self.HERE, '..', '..', 'Assets', 'Tests', 'PlayMode', 'Rig', 'QaRunner.cs'), encoding='utf-8') as f:
            src = f.read()
        block = re.search(r'public static readonly string\[\] Ops =\s*\{(.*?)\};', src, re.S).group(1)
        ops = set(re.findall(r'"([a-z]+)"', block))
        # Tracks' own ops: QaRunner.RegisterOp("name", ...) in their rig files.
        rig = os.path.join(self.HERE, '..', '..', 'Assets', 'Tests', 'PlayMode')
        for root, _, files in os.walk(rig):
            for name in files:
                if name.endswith('.cs'):
                    with open(os.path.join(root, name), encoding='utf-8') as f:
                        ops.update(op.lower() for op in re.findall(r'RegisterOp\(\s*"([A-Za-z]+)"', f.read()))
        return ops

    def test_scenarios_use_known_ops(self):
        ops = self.ops()
        self.assertIn('drag', ops)
        folder = os.path.join(self.HERE, 'scenarios')
        files = [f for f in os.listdir(folder) if f.endswith('.json')]
        self.assertTrue(files)
        for name in files:
            with open(os.path.join(folder, name), encoding='utf-8') as f:
                s = json.load(f)
            self.assertEqual(os.path.splitext(name)[0], s['name'], name)
            for i, step in enumerate(s['steps']):
                self.assertIn(step['op'].lower(), ops, f'{name} step {i}')
                if step['op'] == 'drag' and step.get('path'):
                    self.assertRegex(step['path'], r'(?i)^(P-)?(plain|fast|slow|wobble|diag|flick|edge|off|onto)$', f'{name} step {i}')


class DirtyTests(unittest.TestCase):
    def test_unitys_churn_is_not_a_dirty_tree(self):
        out = ('Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset\n'
               'ProjectSettings/EditorBuildSettings.asset\r\nprojectsettings/qualitysettings.asset\n'
               'Assembly-CSharp.csproj\nNOPE-t6.sln\n'
               'ProjectSettings/ProjectSettings.asset\nPackages/manifest.json\nAssets/Scripts/GameManager.cs\n\n')
        self.assertEqual(['ProjectSettings/ProjectSettings.asset', 'Packages/manifest.json', 'Assets/Scripts/GameManager.cs'],
                         seqtools.dirty_paths(out))
        self.assertEqual([], seqtools.dirty_paths('ProjectSettings/QualitySettings.asset\n'))


if __name__ == '__main__':
    unittest.main()
