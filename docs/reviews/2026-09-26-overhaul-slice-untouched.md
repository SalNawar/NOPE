# Overhaul slice "untouched": gate record (2026-09-26)

An audit-overhaul slice (`docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md`,
Phase 4) that takes confirmed backlog items (`SCRATCH/audit/backlog.json`, verdicts in
`verify_A/B.json`) which the redesign plan (`docs/superpowers/plans/2026-09-26-redesign-plan.md` §3)
leaves to the overhaul or maps to phases that did not fix them. It edits only files that
no phase in flight tonight is changing. Branch `overhaul/slice-untouched`, from main `e71def0`,
with main merged in at the end (`fc96484`, then `e271878`, phase 17's desktop icons). One commit per item, each compiling with its tests green.

**Files kept out of the slice:** GameManager, InvestigationUIController, CaseFactory,
Lies*, VerdictRules, DiscrepancyLog, `world_source.json`, DesktopShell, the desk and papers,
and OfficeSceneUIBuilder. Every file phase 25 had dirty in `NOPE-p5` was also kept out
(HomeManager, WorldState, GameConfigSO, ContentLibrarySO, `Strings_en.asset`, the generator
and the validator). HomeManager was left alone on the orchestrator's instruction.

## The items and what pins them

| Item(s) | Commit | Change | Pinned by |
|---|---|---|---|
| R2-005, R1-003 (ThemeRoleId) | `73609e3` | `ThemeRoles.IsDiegetic` is an explicit list of the 9 diegetic roles, no longer `>= DiegeticPaper`. Its text is identical to phase 4's working copy. Merged to main early. | `ThemeRolesTests`: every role is in exactly one set; a value outside the enum is chrome (this failed before the change); every `ThemeRoleId` int is pinned, Desktop = 0 .. DiegeticDevice = 45, with the count |
| R6-010 (the Home/Title part), 8 × CS0618 | `9b35d7f` | The byte-identical Panel/Text/Button helpers and the Canvas/EventSystem bootstrap of `HomeSceneBuilder` and `TitleSceneBuilder` become one internal `SceneUiKit`, which uses `FindAnyObjectByType`. | Unity: each builder was run into a fresh empty scene before and after the change. The dumps are identical (Title 73 lines, Home 161), and a second run changes nothing. The golden static job's rebuild of the committed Home and Title scenes also equals them. |
| R2-006, R4-013 (the shown side) | `68b38d9` | Domain `ShopPrices.Discounted(cost, percent)`: the float maths and `Mathf.RoundToInt`'s half-to-even rounding, now in one place. `HomeUIController` shows it. | `ShopPricesTests` (10 cases). Unity, play mode at Home: with Archive Access's 10 % off, the row reads "Advanced Scanner — 180 cr (10% off)" and buying charges 180. |
| R2-021 (HomeEconomy), R2-009, R2-011 (HomeEconomy) | `636e432` | Domain `HomeRules`: DrainPoints, CanTreat, Treated, Worsened and Worsens (the nightly roll, the same System.Random hash). `HomeEconomy` keeps its public API and applies the rules. Its 12 trace logs and the unreachable cap fallback go. | `HomeRulesTests`: decision tables, plus the rolls drawn today pinned (seed 12345, members 0-2) |
| R2-007 (EndingService), R2-021 (EndingService), R2-011 (EndingService) | `e515fc4` | Domain `EndingRules.Met(type, threshold, attributeTotal, EndingCheck)` and `IsFired`. `EndingService` calls them. Its about 8 trace logs per verdict go; it keeps one warning and one line when a run ends. | `EndingRulesTests` (+9: each condition at its line) |
| R4-014 | `e7c404a` | `HasExpensesPanel`, `HasShopPanel` and `HasSlotPanel` also need their continue buttons, so a panel without one is skipped instead of stranding the evening. | Unity, edit mode, each continue button unassigned in turn: 6 FAIL before, 6 PASS after |
| R4-010 | `39a5907` | TitleUIController tests its optional panels with Unity's ==, not `?.`. | Unity, edit mode, each panel unassigned (confirmed fake null): before, `UnassignedReferenceException` (4 FAIL); after, 4 PASS |
| R2-003, R2-012, R3-030 | `58b147f` | The dev overlay is enabled only while open (no OnGUI and no Update when closed). The `~` key is an InputAction. It is attached only in the editor and development builds. Every cheat and the tab switch run after the GUI pass. | Unity, play mode: the closed overlay is disabled; it opens; a queued flag cheat runs after the pass and its new row draws with no error; it closes. See the profile below. |
| R2-027 | `f803765` | The Timeline Inspector tab and the state dump move to `DebugInspector`, with the score and effect lines written once. The controller drops from 443 lines to 328, back under the 400-line limit. | Read-only drawing; the overlay play check above |
| R3-014 (DayRunner), the quarantine | `10f454e` | `DayRunner.cs` deleted (no code or GUID reference). `quarantine/` deleted (never compiled). | `git grep` of the GUID; compile |
| R6-018 | `1a0d743` | 11 unused packages removed: 2d.animation, 2d.aseprite, 2d.psdimporter, 2d.spriteshape, 2d.tilemap, 2d.tilemap.extras, 2d.tooling, visualscripting, collab-proxy, multiplayer.center, ai.inference. With them go 2d.common and dt.app-ui, and the dangling `com.unity.dt.app-ui` entry in `EditorBuildSettings` `m_configObjects`. | Unity: the packages resolve (60 left). The project compiles. The EditMode suite and the smoke play-through pass. |

## Follow-ups left on purpose

- **HomeManager's charged price.** `HandleBuyUpgrade` still computes `Mathf.RoundToInt(upgrade.cost * (1f - discountPercent / 100f))` (identical today).
  - After phase 25, those lines become `int cost = ShopPrices.Discounted(upgrade.cost, discountPercent);`. That completes R2-006 and R4-013.
  - The feature check above (shown 180 = charged 180) stays the gate.
- **ShiftScoring's two `firedNow = stability <= firedAtStability` lines.** They should call `EndingRules.IsFired`. That completes R2-007; phase 7 (R3-019) and phase 13 touch that file.
- **HomeManager's trace logs.** 26 `Debug.Log`, 18 of them entry/exit: the rest of R2-011. They go with the HomeManager follow-up.

## Skipped, with reasons

- **R4-011 (Home/Title English literals to UiText).** Every new key needs `world_source.json` `ui.strings`, then Generate World (`Strings_en.asset`, the library). Phase 25 had both dirty tonight, and the plan gives R4-011 to phase 25 "where touched".
- **R4-012 (Home HUD through `tray.money`/`tray.stability`/`tray.day`).** Those keys are Flavour tier, so each culture table translates them. Home's texts are untagged and get no culture font, so the Home HUD would switch to Arabic or Japanese with no font to draw it. It belongs with R4-011, once Home is themed.
- **CS0618 in OfficeSceneUIBuilder (7).** The builder is in flight (phases 3, 25 and 17). The warning in `80s_Office/Scripts/ReplaceGameObjects.cs` belongs to a third-party art pack, which is outside the audit's scope.
- **The other parts of R2-021 and R6-010.** CharacterArt, TravellerView and the office builder's duplicates are not in this slice's files.
- **R2-008 (the drift roll on a Seeds stream).** It would change every night's drift, a golden-master change to take with a re-pack. `HomeRulesTests` pins today's rolls, so the change will show.
- **R6-018's optional part (the seven unused built-in modules).** Kept: low value, and the verdicts covered only the packages. `com.unity.ai.assistant` is kept as an editor workflow tool (Saleh's call), and `com.unity.timeline` for UnitySkills.

## Gates (after merging main `fc96484`; re-run after `e271878`)

- **Compile:** 0 errors.
  - Compiler warnings in our code: 7, all OfficeSceneUIBuilder's CS0618 (out of scope, see above). The baseline had 16; this slice removed HomeSceneBuilder's 4 and TitleSceneBuilder's 4.
  - One more CS0618 is in a third-party art pack.
- **Offline EditMode tests:**
  - At the start: 1286 passed.
  - With this slice: 1343 passed (57 new).
  - After merging `fc96484`: 1427 passed, 0 failed.
  - After merging `e271878`: 1457 passed, 0 failed.
- **EditMode suite in the editor:** 1586 passed, 1 failed after `e271878` (1556 and the same failure after `fc96484`). The failure is the known third-party `UnitySkills...PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`. Before the merge, with the packages removed: 1472 passed and the same single failure.
- **Builders:** OfficeGameplay (6272 lines after `fc96484`, 6689 after `e271878`), Home 165 and Title 81 each rebuild equal to the committed scene, and a second rebuild changes nothing. 0 errors, 0 warnings. The art office is byte-unchanged.
- **Generate World:** 0 files changed on either run. The validator reports 0 errors and 0 warnings.
- **Smoke:** the scripted play-through ran days 1-6 in the art office with Home between them, after each merge: `fails=0` (82 checks), and every traveller was decided. Its warnings are the baseline's 7 anchor warnings.

## Golden masters

`golden.py diff` against the committed baseline found no difference that comes from this slice. It ran after `fc96484`; after `e271878` the transcript was checked again with the same result.

- **Identical:** `cases.txt`, `world_generate.txt`, `validator.txt`, the Home and Title scene dumps, `contract.txt` and `play_warnings.txt`.
- **`play_transcript.txt`: every gameplay line is identical, Home's included** (expenses, family conditions, care, purchases and money each night, endings). Only the 12 `save ... sha1` lines differ.
- **Every existing field of the 12 saves is identical.** The saves gained main's new fields, `newsArchive`, `mailRead`, `accountDays` and `notes` (phases 24 and 25).
- **`data_hashes.txt`, `scene_OfficeGameplay.txt` and `scenes_summary.txt` differ** by main's content and office UI (phases 24, 25 and 26), which reached main without a re-pack. This slice changes no data file and no office scene.
- **Not re-packed** (end of epic).

**Profile (intended, R2-012):** the closed dev overlay's OnGUI was the steady per-frame allocation site (`GUI.Repaint > GUIUtility.BeginGUI`, 368 B per camera).

| Window | Before (baseline) | After |
|---|---|---|
| Title | 368 B/frame | 0 B |
| Home | 368 B/frame | 0 B |
| Office | 736 B/frame | 368 B |

- The office's remaining 368 B is the IMGUI pass of CinemachineBrain's editor-only debug `OnGUI` (a third-party package, `#if UNITY_EDITOR`), so a player build allocates nothing there either.
- No new allocation site; every load within 25 % (`golden.py diff --profile`: OK).

## Unity note for the other editors

Removing the packages changes the asset postprocessors, so Unity reimports the models once. While the old import queue drained, the t5 editor showed "Opening file failed" dialogs for files of the removed packages (for example `com.unity.2d.psdimporter/.../pivot.fbx.meta`). Cancel is the right answer to each. Every other worktree's editor will go through this once when this slice reaches it.
