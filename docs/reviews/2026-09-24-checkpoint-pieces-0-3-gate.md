# Gate record: checkpoint of pieces 0–3 and Codex's art checkpoint onto `main`

- **Branch:** `integrate/checkpoint-p3`. Verified at `3d702cf`; this record is the commit on top of it.
- **Base:** `main` = `origin/main` = `b7f8671` (Codex's art checkpoint). `git ls-remote` shows it has not moved since the merge.
- **Merge method:** fast-forward `main` from `b7f8671` to this branch's tip. No force push. Decision D11 in the art-sync list below.
- **Requested by:** Saleh, 2026-09-24: "push all your work merge everything on main". Earlier he said "don't worry about chatgpt work, focus on your work".
- **Recorded:** 2026-09-24 by Claude, against `docs/reviews/MERGE_CRITERIA.md`. Claude wrote pieces 0–3, so this is the author's gate record, not a third-party review. Each piece had independent review passes (listed below), and Saleh has not reviewed this record yet.
- **Verdict: ACCEPT WITH CONDITIONS.**
  - All six gates pass, and no quality bar scores 2 or lower.
  - The conditions are follow-ups that do not block the merge. The main one: the office *view* on `main` goes back to the builder's 2D booth, drawn with Codex's repainted booth sprites that no longer fit its layout. The art sync is what makes the builder respect Codex's hybrid office. It is condition 1.

## What is merged

`main` gains 87 commits plus this record: 554 files, +92,710 / −27,136 lines against `b7f8671`.

| Piece | Branch @ tip | Commits | Spec (approval) | Plan | Scope |
|---|---|---|---|---|---|
| 0 | `feat/cursor-hover-shift-clock` @ `308b836` | 18 | `docs/superpowers/specs/2026-09-24-cursor-hover-shift-clock-design.md` (approved by Saleh in chat) | `docs/superpowers/plans/2026-09-24-cursor-hover-shift-clock.md` | Game cursor and hover outline; real-time shift clock (tray and wall clock) that closes the day; queues of 8/10/12; unique names within a day |
| 1 | `feat/world-model` @ `14bd22e` | 11 | `…/2026-09-24-world-model-design.md` (approved by Saleh in chat) | `…/plans/2026-09-24-world-model.md` | 8 real countries × 5 eras = 40 places; `FactTable` as the one source of facts; seeded determinism (`Seeds`); provable forgeries; BCE dates; save v2; Generate World from `world_source.json` |
| 2 | `feat/identity-lies` @ `dc676cd` | 21 | `…/2026-09-24-identity-lies-design.md` (decided by Claude under "go with all pieces, don't stop"; open to Saleh's review) | `…/plans/2026-09-24-identity-lies.md` | True home and claimed home; liars travel under a cover identity and leak provable tells; verdict rules in Domain |
| 3 | `feat/dialog-questions` @ `60c9c6e` | 31 | `…/2026-09-24-dialog-questions-design.md` (same status as piece 2) | `…/plans/2026-09-24-dialog-questions.md` | The intercom becomes an interview: questions unlock by day or upgrade, spoken tells, a paged transcript, authored dialogs whose effects apply at the end of the shift |
| Merge | `integrate/checkpoint-p3` @ `3d702cf` | 6 | this record | — | Merge `6295488` of `origin/main` (`b7f8671`), then 5 fix commits from the merge review (below) |

- **Codex's checkpoint:** `b7f8671` is already on `main`. It is 2,461 files: 1,370 under `Assets/Art`, 925 under `ArtDeliverables/`, 143 under `.agents/skills`, the `Assets/Settings` renderer, volume and quality assets, one script (`OfficeTrafficVehicle.cs`), and changes to `HomeScene`, `TitleScene` and `OfficeScene`.
- **Merge base:** `413230c`. The two sides changed 548 and 2,461 files, and only `Assets/Scenes/OfficeScene.unity` changed on both, so it was the only conflict.

## Gates

| Gate | Result | Evidence |
|---|---|---|
| G1 Builds & boots | **PASS** | Offline compile at `3d702cf`, re-run for this record: `Assembly-CSharp-Editor` (which also builds Assembly-CSharp, `TimeDesk.Domain` and `TimeDesk.Visuals`) and `TimeDeskEditMode` both 0 errors. In Unity 6000.4.11f1 the first import took 20.6 s with no import errors, and the scripts compiled in 6.0 s. The play smoke passed on TitleScene, HomeScene and OfficeScene (days 1–3) with 0 failures and no warnings or errors in any phase. `OfficeScene_HybridArt.unity` has 0 missing scripts, prefabs and references (9 roots, 853 GameObjects, 2,487 components) and logs nothing when opened. See the note on build index 0 under follow-up 8. |
| G2 Tests | **PASS** | EditMode suite in Unity: 466 passed, 1 failed. The failure is the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`, which depends on the open scene. Offline Domain runner, re-run for this record at `3d702cf`: `passed 337, failed 0`. Decision-table suites added per piece are listed under "Evidence per piece". |
| G3 Feature complete vs spec | **PASS** | Each spec ends with a verification record that re-ran its §6 plan: piece 0 §7, piece 1 §7, piece 2 §8, piece 3 §8. Departures are recorded in the specs, not left silent: piece 1 §6, piece 2 "Review notes", piece 3 "Implemented differently" and "Code review". The merge fixes apply art-sync decision D7 early (hover) and guard the builder; neither is a half-done feature. Caveat: the specs for pieces 2 and 3 were decided under a standing instruction and still await Saleh's review (condition 5). |
| G4 FEATURES.md | **PASS** | `docs/FEATURES.md` changes by +62/−27 against `main`, in the same commits as the behaviour. The two behavioural merge fixes each edit it: 508e81d changes the hover line (hidden hit zones get the hand cursor only) and 6229ac3 changes the builder line (it builds only `OfficeScene.unity`). `3d702cf` changes data only (cursor art and click points), which FEATURES line 81 already covers ("click points derived from the cursor art"). Codex's one script is used only by `OfficeScene_HybridArt.unity` and `ArtDeliverables/` copies, none of which the game loads. |
| G5 Mergeable & current | **PASS** | `HEAD` contains `origin/main`, so `main` fast-forwards. The single conflict was resolved as the next section describes. `ProjectSettings/EditorBuildSettings.asset` is identical to `main`'s, and the hybrid scene is not in it. Codex's `art` branch (`0db426f`, 6 commits after `b7f8671`) is not part of this merge (follow-up 2). |
| G6 Asset hygiene | **PASS** | Of the 2,063 tracked files under `Assets`, 0 lack a `.meta` and 0 folders lack a `.meta`. Binaries are all on LFS (PNG 715/715, FBX 130/130, blend 7/7). The new scene meta's GUID `5b38d25320c04a9387c8383324caf600` appears only in its own `.meta`. Churn: the TMP fallback atlas is back to its empty committed state (`c3de892`); `NOPE-feat-clock.sln` and `TimeDesk.Visuals.csproj` stay untracked. Declared and left alone: 3 orphan folder metas (merge-review finding 5, below) and Codex's committed `Assembly-CSharp*.csproj`, `.agents/` and `Assets/Screenshots.meta` (D9, follow-up 7). |

## The OfficeScene decision

**Decision:** keep the builder-built `Assets/Scenes/OfficeScene.unity`, and preserve Codex's version unmodified as `Assets/Scenes/OfficeScene_HybridArt.unity`.

- **Mine is kept.** Its blob at `HEAD` is `1913e1682b`, the same as at `60c9c6e` (the verified piece-3 tip). Build Office UI run on it in Unity reproduced it exactly: the semantic dumps have 25,531 lines on each side and no differences. The scene was not re-saved.
- **Codex's is preserved.** `OfficeScene_HybridArt.unity` is blob `3c91bbf377`, byte-identical to `b7f8671:Assets/Scenes/OfficeScene.unity`. Its hand-written `.meta` has a new GUID and the standard `DefaultImporter` block, so its references cannot collide with `OfficeScene.unity`'s GUID `a3e1026f…`. It is not in the build settings.
- **Why not take Codex's side.** Codex's scene has none of the piece 0–3 wiring: the shift-clock driver and readouts, the interview transcript and intercom capacity, the Expand canvas scalers, and the rest of the builder's wiring. The builder cannot add that wiring without destroying the scene. Codex's booth objects share the builder's names (`OfficeRoot`, `CRTMonitor`, `ReadySign`, `Main Camera`), so a build there would switch its perspective camera to orthographic, move its hit zones and add the 2D booth. Taking Codex's side would have shipped a `main` where pieces 0–3 do not run. Making the builder respect the hybrid office is the art sync (condition 1).
- **The builder is guarded** (`6229ac3`). Build Office UI now refuses any scene but `OfficeScene.unity`. Run on the hybrid scene in Unity, it logged exactly one error, left the scene clean, and the scene's 49,104-line dump was unchanged.
- **Codex's documentation is marked** (`9e459f7`). A note at the top of `ArtDeliverables/TimeDesk/HybridScene/UNITY_INTEGRATION.md` says where the hybrid office now lives and that `OfficeScene.unity` should be taken from `main` on future merges. Codex's own text is unchanged.
- **Known cost.** Codex repainted the builder's booth placeholder sprites in `Assets/Art/Office/Placeholder/` (backwall, partition, desk, crt, sign, till, calendar, poster, stabilitymonitor). The builder's 2D layout does not fit the new art: in `checkpoint_office.png` the props float on black, the back wall is a small box, and the "60", "100%" and "01" readouts spill out of their art. This is merge-review finding 3, handled as follow-up 1. The monitor view and the gameplay are unaffected; the play smoke passed.

## Merge review (7 findings) and Unity verification

**Fixed (5):**
- `508e81d` (hover): the outline shows only on visible booth sprites; a hidden hit zone gets the hand cursor only. In play mode, the visible CRT gained and then lost its outline, and with its sprite hidden it got none. FEATURES.md and the class doc are updated.
- `6229ac3` (builder guard): described above.
- `c3de892` (TMP fallback atlas): back to its empty committed state, as in `d3a9a49`.
- `9e459f7` (docs note): described above.
- `3d702cf` (cursor): only the builder's existing cursor step ran, and the scene was not saved. The game cursor now uses Codex's `cursor_arrow`/`cursor_hand` art, imported as readable Cursor textures without mips, with click points arrow (3, 2) and hand (13, 3) derived from the art. Only `InteractionFeedback_Default.asset` references those GUIDs, so the import-type change (Sprite to Cursor) breaks no reference.

**Not fixed (2):**
- **Finding 5 (stray metas): rejected, with reasons.**
  - The 3 orphan folder metas are `Assets/Art/Office/Hybrid/BlenderOffice/Textures.meta` and `Assets/Screenshots.meta` (both from `b7f8671`), and `Assets/Scripts/New Folder.meta` (from `b9c504a`, the first commit).
  - Unity recreates the empty folders with an info message and the tree stays clean.
  - Codex's `art` branch already has files in both of his folders, so a `git rm` on `main` would delete his folder metas on his next merge.
  - The 2-line `OfficeTrafficVehicle.cs.meta` is left as Codex's editor wrote it; Unity leaves it alone.
- **Finding 3 (booth layout against the new art):** follow-up 1.

**Unity results (worktree editor, 6000.4.11f1, run through temporary `-executeMethod` scripts that were deleted afterwards):**
- Run 1 started at 23:26 on `9e459f7`. Its cursor import became `3d702cf`.
  - Generate World ran twice and changed 0 files each time: 6 eras, 8 nations, 40 places, 3 day plans, 6 questions, 2 dialogs and 3 unlock triggers.
  - Validate Content Library reported no issues.
  - The builder guard and the reproduction of `OfficeScene` behaved as described above.
  - EditMode: 466 passed, 1 known failure.
- The play smoke started at 23:32 on `3d702cf`. 0 failures, no warnings or errors.
  - **Day 1, seed 2:**
    - The claim banner read correctly, and all 5 books listed today's 4 places in order.
    - The honest traveller's Currency answer matched and the traveller was accepted.
    - The liar's Declared Device field ("Cylinder seal") logged DEVICE INCORRECT, the Deviation Report opened on its own, and the denial was correct with no citation.
    - Forced closing showed the shift report (processed 2, correct 2).
  - **Day 2, seed 77:**
    - The hub and ask menu were as expected.
    - Transcript paging worked: 12 lines made 2 pages of 8, with the right rows on each page.
    - The spoken capital logged CAPITAL INCORRECT, and a second proof showed ALREADY DOCUMENTED.
    - The rumour dialog completed and was saved, and all 3 decisions were correct.
  - **Day 3:** the paper carried the rumour tip and the ruler and birth-date notices. The ask menu showed all 8 choices, and asking the ruler added its prompt and answer to the transcript.
  - **TitleScene and HomeScene:** both played, and each has the cursor bootstrap with the final cursor art.
- **Clean-up:** Unity-touched files were reverted: csproj, ProjectSettings, the TMP atlas, and the RunConfig seed/day pins. The player's save was restored. The tree is clean apart from the two untracked IDE files.

**What was re-checked for this record rather than taken from the report:** the offline compile and Domain tests; the merge parents and conflict set; both scene blobs; the build settings; meta completeness and orphans; LFS coverage; GUID uniqueness; and `origin/main` via `ls-remote`. The Unity-side figures come from the automation's own PASS/FAIL reports (`checkpoint_run1_report.txt` and `checkpoint_play_report.txt`), which were read in full.

## Evidence per piece

| Piece | Independent reviews (findings → outcome) | Verification record | EditMode (Unity) | Domain (offline) | Decision-table suites added |
|---|---|---|---|---|---|
| 0 | Four-lens review (logic, Unity runtime, intent audit, regressions), with an adversarial verifier for each lens: 12 issues, all fixed (spec §6) | spec §7: builder wiring, scripted play smoke (clock, closing paths, outline geometry) | every TimeDesk test passes + the known failure | 86/86 | `ShiftClockTests`, `ShiftFlowTests`, `DaySlotSequencerTests`, `NameRosterTests`, `OutlineMaskTests`, `CursorHotspotTests`; `ReadyGateTests` extended |
| 1 | Correctness, content pipeline and intent audit; amendments in spec §6 supersede the matching lines | spec §7: 102 retired assets; Generate World idempotent; world check on 2 seeds × 3 days; day-1 play-through | 285 + the known failure | 156/156 | `SeedsTests`, `SeededRandomTests`, `WeightedRandomTests`, `BirthDatesTests`, `FactTableTests`, `ForgeryTests`, `ViolatorSlotsTests`, `OriginLabelsTests` |
| 2 | Spec review: 18 findings, all applied. Plan review: 5 corrections. Implementation review: 6 findings, 4 fixed and 2 closed by Task 13 | spec §8: world check on 200 seeds × 3 days (0 problems, liar rate 0.500), identities equal to the baseline, day-1 play-through | 345 + the known failure | 216/216 | `LiesTests`, `VerdictRulesTests`, `TravellerGendersTests`, `ScriptedRandomTests`; `DiscrepancyLogTests` moved to tell vocabulary |
| 3 | Critique table. Independent review: 26 findings, 25 applied in full and 1 with a corrected premise. Plan review ("Implemented differently"). Code review: 9 findings (C1–C8), all applied | spec §8: content negative checks, gates and triggers equal to the baseline, world check on 200 seeds × 3 days × with/without the upgrade (148,672 interview lines within 100 characters), builder dumps equal, day 2–3 play-through | 466 + the known failure | 337/337 | `GatesTests`, `EffectOpsTests`, `DialogRunnerTests`, `InterviewTests`, `InterviewDayTests`, `InterviewScriptTests` |
| Merge | Merge review: 7 findings, 5 fixed and 2 not fixed, with reasons (above) | this record | 466 + the known failure | 337/337 (re-run here) | none (no new rule logic) |

## Quality bars

- **Configurability: 4/5.**
  - The knobs live in ScriptableObjects and the source JSON: clock length and closing in `GameConfigSO`; queue sizes and `tellCount` in `DayPlanSO`; the birth-date shift in `CaseBlueprintSO`; cursor art and click points in `InteractionFeedbackSO`; interview `menuCapacity` and `maxLineChars` in `world_source.json`.
  - Docked: the 2D booth layout (positions and sizes) is builder code. That is why the repainted sprites no longer fit, and moving layout ownership to the scene is D3.
- **Modularity / decoupling: 4/5.**
  - The rules are in `TimeDesk.Domain` with tests: `Seeds`, `FactTable`, `Forgery`, `Lies`, `Gates`, `Interview`, `DialogRunner`, `ShiftClock` and `DaySlotSequencer`. `TimeDesk.Visuals` is a pure assembly.
  - Missing wiring warns and skips instead of throwing.
  - Docked: `CaseFactory`, `TimelineService` and the save gate stay in Assembly-CSharp as glue. Each is named with its reason (world-model §6 "Why some rules stay outside Domain", dialog-questions §2.18).
- **Scalability: 5/5.**
  - Places, facts, names, questions, dialogs and unlock triggers are all data in `world_source.json`. The generator (idempotent) writes them and the validator checks them.
  - A new question or dialog is a JSON entry, and the capacity and line-length limits are checked there.
- **Optimization: 4/5.**
  - `HoverHighlighter` reuses the input module's raycast and caches its hierarchy walk.
  - The builder converges: dumps are equal across builds, and here it reproduced the committed scene exactly. A second Generate World run changes 0 files.
  - Docked: every builder change rewrites large parts of `OfficeScene.unity` (piece 0 alone: +19,198/−13,432 lines, mostly the scene). The builder-owned scene makes this inevitable, but it is heavy to review.
- **Code quality: 4/5.**
  - The style matches, and public members carry doc comments.
  - The builder guard fails loudly and names the fix.
  - Review findings are recorded with their verdicts, and the commits describe their changes honestly.
  - Docked: the existing CS0618 obsolete-API warnings remain in the compile log, and the merge commit's message is one line for a 2,461-file merge. This record carries its reasoning instead.

## Intent audit

**1. Does it redo or override existing code or decisions?**

- **`OfficeScene.unity` on `main`.** Codex's hybrid office is replaced by the builder-built scene. The reason is argued above: the pieces cannot run in Codex's scene, and the builder cannot yet respect it. The override loses nothing: Codex's scene is preserved byte-identical and opens cleanly. The cost is the office view (follow-up 1).
- **TMP fallback atlas.** Codex's populated glyph cache goes back to the empty committed state. It is runtime-generated data that Unity rewrites on every run. The house rule and G6 treat it as churn, so D9 ("don't rewrite Codex's churn") is superseded for this one file.
- **Cursor import settings.** Codex's `cursor_arrow`/`cursor_hand` imports change from Sprite to Cursor and become readable. The builder's existing cursor step made this change, and nothing references those textures as sprites. Codex's `art` branch may touch these metas again; take `main`'s on merge unless the art changed.
- **Piece-0 hover behaviour.** A hidden sprite no longer gets an outline. This is D7, applied early, and FEATURES is updated.
- **Builder contract.** The builder used to build whatever scene was open; it now builds only `OfficeScene.unity`, with FEATURES updated. This was necessary because the two office scenes share object names.
- **Codex's doc.** The merge only adds a note at the top of `UNITY_INTEGRATION.md`; his text is unchanged.

**2. Does it re-code something the existing architecture already supports without code?**

- **Scene preservation** is a plain asset with a new GUID; no code.
- **Cursor art** goes through the existing builder step and the existing `InteractionFeedbackSO` fields; no code.
- **Hover:** the no-code alternative, taking the `SpriteRenderer` off the hit zones, would edit Codex's scene and remove the bounds the `Clickable` is found by. The fix is two lines inside the owning class.
- **Builder guard:** the no-code alternative, renaming the hybrid scene's objects, would edit Codex's scene. The guard is six lines inside the owner. The path literal exists once (`OfficeScenePath`), and no other helper checks the active scene.
- **No parallel mechanism was added.** Each piece's own intent audit is in its spec: piece 0 §6, piece 1 §6, piece 2 "Review notes", piece 3 "Review notes".

## Conditions and open follow-ups

1. **Art sync (condition, before piece 4).** Make the builder respect Codex's hybrid office. Then `OfficeScene.unity` becomes the hybrid scene with the piece 0–3 wiring, and `OfficeScene_HybridArt.unity` is retired. This also resolves merge-review finding 3.
   - A local draft exists: `feat/art-sync` @ `51be2fd`, not pushed. It merged `origin/art` @ `004eb02` into the stack with `OfficeScene` taken from art, based on `60c9c6e`. Redo it on top of this `main`.
   - The decisions are parked in the session scratchpad's `artsync_decisions.md`, with the analysis in `integration_map.json`. They are copied here so the record survives the scratchpad:
     - D1: the hybrid scene is the base. The scene owns the world layout, camera and render settings; the builder owns UI and gameplay wiring.
     - D2: sync now, and re-sync at the end.
     - D3: the builder becomes "existing-wins". It sets position, sprite and camera only when it creates an object, wires readouts through existing references or named anchors, and updates skinned windows in place instead of destroying and recreating them.
     - D4: keep Codex's text-free UI skins. Sprites with baked words get builder labels over text-free backgrounds.
     - D5: wire Codex's 3D digital clock through `ShiftClockReadouts` and retire the analog wall-clock build.
     - D6 (piece 4): the traveller is a billboard at `HybridOffice/Booth/TravellerAnchor`.
     - D7: no outline on hidden sprites (done in `508e81d`). A true 3D outline is a later follow-up.
     - D8: tint the stability readout by band, with the thresholds serialized.
     - D9: leave Codex's pushed churn, except the TMP atlas (above).
     - D10: Codex commits his own WIP, and the sync merges what is pushed.
     - D11: fast-forward `main` after this gate record.
     - D12: leave Codex's `art` checkout alone and tell him `main` moved.
     - D13: `ReactivePoster` stays inactive; the poster anchor is a follow-up.
     - D14: `readySign`/`ReadyGate` keep their names in code, and the displayed caption is a game-owned "NEXT".
     - D15: keep the remote `Art` branch and raise it with Saleh.
   - Its verification: a Codex-preservation dump before and after Build Office UI, two builds with equal results, and a hybrid-scene play-through with screenshots.
2. **Codex's newer art.** `origin/art` is at `0db426f`, 6 commits after `b7f8671` ("pack-based hybrid office rebuild"): 4,617 files changed, including `Assets/Scenes/OfficeScene.unity` again. None of it is on `main` or in this merge. The art sync takes it in. Coordinate with Codex first: on `main`, his hybrid office now lives in `OfficeScene_HybridArt.unity`, so his `OfficeScene.unity` edits must land there (or in the builder), not over the builder-built scene.
3. **Desktop icon labels wrap and overlap** in `checkpoint_monitor.png` ("Currenc y Ledger", "Rulers & Regent s"). The merge changed no UI code and not `OfficeScene`, so this is probably older than the merge, but it was not compared against a pre-merge screenshot.
4. **Orphan folder metas** (finding 5): leave them. Drop them only once Codex's folders exist on `main`, or with his agreement.
5. **Saleh's review of the specs for pieces 2 and 3.** They were decided under "go with all pieces, don't stop". Also still open from piece 0 §7 is a by-eye check of the outline and cursor under a real mouse. The cursor bootstrap in Title and Home is now checked by script.
6. **Carried from piece specs:** the `DayPlanSO` event schedule and `HomeEconomy` still use `System.Random` (world-model §6).
7. **Optional clean-up for Saleh:** Codex committed tracked `Assembly-CSharp*.csproj` changes, `.agents/skills` and `Assets/Screenshots.meta` (D9). The remote `Art` branch (`d3a9a49`) is stale (D15).
8. **Build index 0 is `Test_DayLoop.unity`.** A player build would start there, not at Title. This predates the merge (unchanged since `e886dc5`) and `EditorBuildSettings.asset` is identical to `main`'s. Editor play from TitleScene is unaffected.
9. **Stale PR:** if a pull request for `feat/world-model` → `main` (pieces 0 and 1) is still open, this fast-forward makes it redundant; close it.
10. **Known test failure:** the third-party `UnitySkills…SceneSummarize_CountsObjectsCorrectly` still fails when a scene is open. It is unrelated to this merge.

## Evidence files (session scratchpad, not committed)

- `checkpoint_run1_report.txt`, `checkpoint_play_report.txt` (the automation's PASS/FAIL lines)
- `unity_cp_run1.log`, `unity_cp_play.log`
- `checkpoint_office.png`, `checkpoint_monitor.png`, `checkpoint_monitor_interview.png`
- `cp_run1.cs.txt`, `cp_play.cs.txt` (copies of the deleted temporary scripts)
- `artsync_decisions.md`, `integration_map.json`
