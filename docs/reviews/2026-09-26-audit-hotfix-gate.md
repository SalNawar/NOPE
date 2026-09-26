# Audit hotfix slice: gate record (2026-09-26)

Phase 4 gate of the code audit (`docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md`)
for one slice: the confirmed high and medium audit bugs. Branch `overhaul/audit`, on the
Phase 0 baseline `a2b6e6b` (tag `audit-baseline` = `ff3a6e0`). One commit per fix, each
compiling with its tests green.

## The fixes and what pins them

| # | Finding | Commit | Pinned by |
|---|---|---|---|
| 1 | R3-001 / R6-025 (high): a player build booted `Test_DayLoop` | `ca178dc` | `BuildScenesTests` (6 tests: the baseline list comes out Title, Office, Gameplay, Home, prototype off; order stable). In the editor: the build list is Title, OfficeScene, OfficeGameplay, HomeScene, then `Test_DayLoop` (off); Build Office UI wrote exactly the committed list. |
| 2 | R5-001 (high) + R5-002 (medium): a held paper stuck after a drag beside the open frame | `5f3de3d` | `BoothRulesTests`: new column `HeldDragOutLive` (row "frame open, papers held" = 0), a per-context test and the frame-open test; they failed with the old rule. In the office: the hotfix play-through's desk probe (below). |
| 3 | R2-001 (medium): untranslated right-to-left speech typed from its end | `f3e7bab` | `DisplayTextTests` (5 `Typed` tests), `ArabicShaperTests.ToVisual_ReportsEachVisualCharactersLogicalSource`. In the office: day 2 slot 3's Arabic line sampled mid-typing, hidden part at the left. |
| 4 | R6-001 (medium): Generate World did not check day identity or scalars | `881678e` | `DayPlansTests` (4 `Problems` tests, the audit's two failing inputs), `BirthDatesTests.AgeRangeProblem` (5 cases). One rule (`DayPlans.Problems`) for the generator and the validator. |
| 5 | R3-003 (confirmed, low after verification): a save crash could lose the run | `42c5a59` | `SaveFilesTests` (the 8 cases of `SaveFiles.Pick`). In the editor: the probe below recovers from the temp file and from the backup. |
| 6 | Phase 0: `DiscrepancyLog.Prove` took any record row as proof | `d93747d` | `DiscrepancyLogTests.RecordProof_AnotherTravellersRecord_ProvesNothing`, `..._AnEmptyRecordValue_ProvesNothing`, `..._NeedsTheRecordToNameTheTraveller` (red with the new signature and the old rule). In the office: a liar's birth-date tell against another traveller's record proves nothing. |
| 7 | R5-004 / Phase 0 GC: `OfficeReadouts` re-formatted every frame | `bed6e16` | The profiled play-through: every office window drops from 838 B to 736 B per frame and loses the `OfficeReadouts.Update` site (736 B is URP's `ExecuteRenderGraph`). |
| 8 | R2-004: the slot spin was unseeded | `276eaef` | `SeedsTests.SlotStream_IsDeterministic_AndApartFromEveryOtherStream_NightByNight`. In the office: Continue replays night 1's three spins exactly. |

## Gates

- **Compile:** 0 errors. Compiler warnings in our code: 16, all the baseline's CS0618.
- **Offline EditMode tests:** 1101 passed, 0 failed. The baseline had 1067; this slice adds 34.
- **EditMode suite in the editor:** 1230 passed, 1 failed. The failure is the known third-party `UnitySkills...PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`.
  - On the first run, a second UnitySkills test also failed: `SkillsModeManagerTests.Migration_RepeatLoad_IsIdempotent_NoDuplicateAuditEvent` (prefs state).
  - It passed when the UnitySkills suites ran alone, and again on a second full run. It is order-dependent and third-party.
- **Player scripts compile:** `PlayerBuildInterface.CompilePlayerScripts` for StandaloneWindows64 built 44 assemblies, `Assembly-CSharp`, `TimeDesk.Domain` and `TimeDesk.Visuals` among them.
- **Builders:** every rebuild equals the committed semantic dump, and a second rebuild equals the first: OfficeGameplay 4424 lines, Home 165, Title 81.
  - The builders logged 0 errors and 0 warnings.
  - The art office stayed byte-unchanged.
- **Static baseline** (`run_static_baseline.py --check`). The regressions are all intended or artefacts:
  - `string_literals_rule_code` 201 → 206 and `rule_strings` (7 "new"):
    - The six diagnostic messages of the new shared checks (`DayPlans.Problems`, `BirthDates.AgeRangeProblem`). The task asked for one rule with the validator's messages, the same kind as `HistoryChecks`.
    - The existing `"a different era"`, which moved from `Prove` to `ReferenceProof`.
  - `clone_groups` (11 "new", all in tests): existing normalised clone groups were re-hashed because every `Prove` call and record row gained the traveller argument.
    - The group count is unchanged (108).
    - The duplicated test lines fell from 849 to 847.
  - Improvements:
    - Complexity over 15: 74 → 72. `ArabicShaper.ToVisual` and `DiscrepancyLog.Prove` were split and are now under the limit.
    - Methods over 60 lines: 40 → 39.
    - Magic numbers: 52 → 51.
  - `per_frame_allocation_sites` stays 69. The text scan still sees the format calls in `OfficeReadouts.Apply`, which the profile shows no longer run each frame.

## Golden masters

`golden.py diff` against `docs/reviews/audit-baseline/golden` covered 23 deterministic files and found 2 differences, both explained below.

- **Identical:** `cases.txt`, the Generate World output, the validator and the data hashes (Generate World changed 0 files); the three committed scene dumps; `contract.txt`; `play_transcript.txt`; `play_warnings.txt`; all 12 play saves.
- **`scenes_summary.txt`, intended (fix 1).** Only the build-settings line changed:
  - Before: `Test_DayLoop, OfficeScene, OfficeGameplay, HomeScene, TitleScene`.
  - After: `TitleScene, OfficeScene, OfficeGameplay, HomeScene, Test_DayLoop(off)`.
- **The transcript did not change,** even with the `Prove` change (fix 6): the play job's record proofs now pass the traveller's own record (`tools/audit/unity`, fix 6), and every proof still registers. Seeding (fix 8) did not change it either: the golden play never spins.
- **Profile, GC (intended, fix 7):**
  - Office windows: 838 → 736 B per frame.
  - Title and Home: 368 B, unchanged.
  - No new allocation site in our assemblies.
- **Profile, times: not a code regression.** The time checks flagged one load, title → office 937 → 1201 ms (a second run: 1198 ms).
  - Every time rose in step. The other loads rose 17-21%.
  - The Title idle frame, a scene no fix touches, rose from 0.62-0.73 ms to 0.91-0.96 ms. The office's PlayerLoop roughly doubled, 2.1-2.5 ms to 4.2-5.3 ms, with less of our own per-frame work.
  - The editor process was restarted at 02:33, after the baseline was taken. A second editor (the main checkout's) was running alongside it.
  - The baseline code could not be profiled again in the same session to prove it outright.

## The hotfix play-through (art office, days 1-2 and Home with spins)

A temporary job (the audit play job with `LastDay = 2`, three spins each night, and probes) reported 0 fails. It adds no new warnings: the only warnings beyond the baseline's anchor warning are the two recovery warnings the save probe causes on purpose.

- **Desk (fixes 2):**
  - In the office view, a held paper can be dragged out.
  - Beside the open frame it takes clicks but its drag is off. A begin-drag starts nothing, and the paper stays held. Back in the office view the drag works again.
  - Opening the frame mid-drag on a desk paper ends the drag (`OnDisable`): `_dragging` is false and `_dragged` is -1.
  - The paper slides back and takes input and raycasts again. A stray release after the re-enable is ignored.
- **Record (fix 6):** a day-1 liar's birth-date tell against another traveller's record added no evidence.
- **Spins (fix 8):** night 1 drew three spins; `ContinueRun` then `ResumeRun` reloaded Home from the pre-Home save, and it drew the same three.
- **Bubble (fix 3):** day 2 slot 3's untranslated Arabic line was sampled mid-typing. The hidden run was at the left, and the typed run at the right end.
- **Save (fix 5):**
  - After the run the slot held the save and its backup, with no temp file.
  - With the save moved to the temp file, `Load` recovered it and restored the save.
  - With only the backup left, `Load` recovered the run before.

## Left open

- `PickKeys.Record(category)` does not include the record's owner. Picking one person's Born row and then another's clears the pair. This is harmless now that another person's record proves nothing, and belongs to a later slice.
- A drag held across a scene change was not exercised. On unload, `DeskDraggable.OnDisable` would send a paper still on the desk back as the scene goes. A paper the decision already returned is left to the teardown, and that case is exercised every shift.
