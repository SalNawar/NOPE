# Examine the papers at the desk (piece 10) Implementation Plan

> **For agentic workers:** execute task by task (REQUIRED SUB-SKILL: superpowers:executing-plans or superpowers:subagent-driven-development). Steps use checkbox (`- [ ]`) syntax.
> - Spec: `docs/superpowers/specs/2026-09-25-desk-examination-design.md`, committed in Task 0 from `SCRATCH/specs/2026-09-25-desk-examination-design.md`.
> - House rules: `SCRATCH/HOUSE_RULES.md`. Read it before Task 0. Where it names `E:\unity\NOPE-feat-clock`, this plan's worktree is `E:\unity\NOPE-art`.
> - `SCRATCH` = `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`.

**Goal:** the case is worked at the desk. The claim, with the role, stays on an office tag. Papers show their whole face and lift into the hand, two side by side. Field rows, the bubble's answer and garments feed the one compare that the PC uses, with an office strip. Held papers sit beside the open PC frame. The stamp gives the verdict. The PC keeps every function it has.

**Architecture:** every rule is Domain or Visuals and tested first:
- Domain: the row source (`DocumentRows`); pairing and keys (`ComparePair`, `PickKeys`); holds, slots and click routing (`DeskPapers`, `PaperClicks`); the input table (`BoothRules`).
- Visuals: the pose and layouts (`ExamineLayout`); the paper face (`PaperFace`); the reveal (`RevealClock`); the bubble's tag and hold (`SpeechQueue`).

Assembly-CSharp is glue:
- `EvidencePicks`, `ICompareHighlight` and a refactored `CompareController`;
- `DocumentRowView` and `DocumentReveal`;
- the desk (`DeskDocument`, `DeskController`, `PaperExaminer`);
- the bubble (`TravellerWheel`, `SpeechBubbleInput`);
- `StampTray`, `OfficeCaseHud`, `BoothCoordinator`, `PcFrame`, `InvestigationUIController`, `GameManager` (one subscription).

The builder builds the new scene parts. `world_source.json` changes one UI string.

**Tech stack:** Unity 6000.4.11f1, C# 9, NUnit EditMode (`TimeDeskEditMode` sees only Domain and Visuals), uGUI + TextMeshPro, URP, the Input System. Scratch helpers: `subs.py`, `make_meta.py`, `compile_check_art.py` (output `cc_art`), the reflection runner.

---

## Conventions used by every task

- **Worktree `E:\unity\NOPE-art`**, branch `feat/desk-exam`, created in Task 0 from `origin/main` at `d5844d0`.
  - Never touch `E:\unity\NOPE` (Codex, `art`) or `E:\unity\NOPE-feat-clock`.
  - Never edit `Assets/Scenes/OfficeScene.unity`: it must stay byte-unchanged.
  - Never push, rebase or amend.
- **Compile check** (Bash): `python "$SCRATCH/compile_check_art.py" 'E:\unity\NOPE-art'`. Pass means `exit 0` for `Assembly-CSharp-Editor.csproj` and for `TimeDeskEditMode.csproj`.
- **Test run:** `"$SCRATCH/runner/bin/Debug/net10.0/runner.exe" '<SCRATCH>\cc_art\Temp\Bin\Debug'` (Windows path). Each task states the expected change, and Task 0 records the base (piece 9's record: `passed 946`).
- **Edits:**
  - Existing files are edited only through Python scripts in `SCRATCH/p10/` that call `subs.apply(path, [(old, new), …])`. The helper is exact and EOL-preserving, and each old text must match once.
  - New files are written whole with the Write tool (LF).
  - Every new `.cs` or folder under `Assets` gets a `.meta` in the same commit: `python "$SCRATCH/make_meta.py" <paths>`.
  - If a `subs.apply` old text does not match, the file moved since this plan was written: re-read it and adapt that one edit (the code is the truth).
- **TDD** for Domain and Visuals: write the failing tests, run them and see them fail (a compile error counts), implement, then see them pass. Assembly-CSharp is not unit-testable: its rules are the Domain and Visuals calls tested earlier, and its behaviour is checked in Unity (Tasks 14–15).
- **FEATURES.md** changes in the commit of the behaviour. Behaviour the builder makes changes in the scene commit (Task 14), as in pieces 7 and 9. Spec §8.1 lists every line.
- **The scene is rebuilt once, in Task 14.** Until then the committed `OfficeGameplay.unity` is `d5844d0`'s, and every commit keeps it playable: every new serialized reference is optional and null-safe, and the PC path behaves exactly as before until Task 14.
- **Commits:** `git add <exact paths>`, then `git commit -F -` with a heredoc. Every message ends with a blank line and `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Never stage `_TimeDesk*` files, `*.sln`, generated csproj files, or files the task did not change.
- **Unity runs** (Tasks 0, 14, 15):
  - First check that no Unity process has `NOPE-art` on its command line.
  - Run `Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' -ArgumentList @('-projectPath','E:\unity\NOPE-art','-executeMethod','<Class.Method>','-logFile','<SCRATCH>\p10_<x>.log') -PassThru -Wait` (GUI mode, no batchmode).
  - Temporary scripts are `Assets/Editor/_TimeDeskP10*.cs`, never committed. Reports go to `SCRATCH/p10_*`.
  - Afterwards revert the Unity-touched files that are not part of the task (`*.csproj`, `ProjectSettings/*`, `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`).
- **Reference automation to copy from:**
  - `SCRATCH/move/final_automation/_TimeDeskMovePlay.cs`: `Resolution(w, h)`, `Shot(name)`, the virtual mouse and keyboard, `Check`, the art office load.
  - `SCRATCH/move/final_automation/_TimeDeskMoveSetup.cs`.
  - `SCRATCH/p9/fonts_probe.cs.txt` (the runtime script fonts).
  - `SCRATCH/p8__TimeDeskP8Baseline.cs.txt` (the generation dump).

## File map

| File | Responsibility | Task |
|---|---|---|
| `docs/superpowers/specs/2026-09-25-desk-examination-design.md`, `docs/superpowers/plans/2026-09-25-desk-examination.md` | the spec and this plan | 0 |
| `Assets/Scripts/Domain/DocumentRows.cs` (+meta), `Assets/Tests/EditMode/DocumentRowsTests.cs` (+meta) | the row source | 1 |
| `Assets/Scripts/Domain/DeskPapers.cs` | `CaseDocument.fields` (Task 1); holds, `PaperClicks`, the `DeskHints` rename (Task 3); `holder` removed (Task 9) | 1, 3, 9 |
| `Assets/Scripts/CaseInstance.cs` | `PageCount` → `DocumentRows.PageCount` | 1 |
| `Assets/Scripts/Domain/ComparePair.cs` (+meta), `Assets/Tests/EditMode/ComparePairTests.cs` (+meta) | pairing and keys | 2 |
| `Assets/Tests/EditMode/DeskPapersTests.cs` | holds, clicks, the note | 3, 9 |
| `Assets/Scripts/Domain/BoothRules.cs`, `Assets/Tests/EditMode/BoothRulesTests.cs` | X15 | 4 |
| `Assets/Scripts/Visuals/ExamineLayout.cs` (+meta), `Assets/Tests/EditMode/ExamineLayoutTests.cs` (+meta) | pose and layouts | 5 |
| `Assets/Scripts/Visuals/PaperFace.cs` (+meta), `Assets/Tests/EditMode/PaperFaceTests.cs` (+meta) | the face | 6 |
| `Assets/Scripts/Visuals/RevealClock.cs` (+meta), `Assets/Tests/EditMode/RevealClockTests.cs` (+meta), `Assets/Scripts/Visuals/SpeechQueue.cs`, `Assets/Tests/EditMode/SpeechQueueTests.cs` | the reveal; the tag and the hold | 7 |
| `Assets/Scripts/UI/EvidencePicks.cs`, `UI/CompareHighlights.cs`, `UI/DocumentRowView.cs` (+metas); `UI/CompareController.cs`, `UI/DocumentWindowController.cs`, `UI/TranscriptWindowController.cs`, `UI/ReferenceBookWindowController.cs`, `UI/CitizenRecordsWindowController.cs`, `UI/InvestigationUIController.cs` | the behaviour-preserving refactor | 8 |
| `Assets/Scripts/Office/DeskConfigSO.cs`, `Office/Desk/DeskDocument.cs`, `Office/Desk/DeskController.cs`, `UI/InvestigationUIController.cs`, `Assets/Tests/EditMode/InterviewScriptTests.cs` | the paper face | 9 |
| `Assets/Scripts/Office/Desk/PaperExaminer.cs` (+meta); `Office/Desk/DeskController.cs`, `DeskDocument.cs`, `DeskDraggable.cs`, `Office/PcFrame.cs`, `Office/BoothCoordinator.cs`, `Office/OfficeSceneBinder.cs`, `UI/InvestigationUIController.cs`, `GameManager.cs`, `docs/FEATURES.md` | examine, the frame region, the reveal on sight | 10 |
| `Assets/Scripts/UI/SpeechBubbleInput.cs` (+meta); `UI/TravellerWheel.cs`, `UI/InvestigationUIController.cs` | bubble picks and hold | 11 |
| `Assets/Scripts/UI/StampTray.cs`, `UI/OfficeCaseHud.cs` (+metas); `Office/BoothCoordinator.cs`, `Office/OfficeSceneBinder.cs`, `UI/InvestigationUIController.cs` | the verdict at the desk; the claim tag | 12 |
| `Assets/Editor/OfficeSceneUIBuilder.cs`, `OfficeSceneUIBuilder.Desk.cs`, `Assets/Data/World/world_source.json` | the builder (X24); the note's text | 13 |
| `Assets/Scenes/OfficeGameplay.unity`, `Assets/Data/Content Library/ContentLibrary_Main.asset`, `Assets/Data/Config/Desk_Default.asset`, `Assets/Art/Office/Gameplay/Materials/{Paper_Examine,PaperRow_Highlight}.mat` (+metas), `docs/FEATURES.md` | the rebuild and the regenerated content | 14 |
| the spec (§11 verification record) | the record | 15, 16 |

---

### Task 0: Setup, names, baseline and the font spike

- [ ] **Step 1: Branch.** In `E:\unity\NOPE-art`: `git status --short` must be clean (untracked `*.sln`/csproj aside) and `git rev-parse HEAD origin/main` both `d5844d0…`. Then `git checkout -b feat/desk-exam origin/main`.
- [ ] **Step 2: Commit the spec, then this plan.**
  - Copy the spec from `SCRATCH/specs/` to `docs/superpowers/specs/2026-09-25-desk-examination-design.md` and commit it alone: `docs(spec): examine the papers at the desk (piece 10) design`.
  - Copy this plan to `docs/superpowers/plans/2026-09-25-desk-examination.md` and commit it: `docs(plan): examine the papers at the desk (piece 10) implementation plan`.
- [ ] **Step 3: Record the names.** Re-read at `HEAD`:
  - `DeskPapers.cs`, `BoothRules.cs`, `DiscrepancyLog.cs`, `SpeechQueue.cs`;
  - `CompareController.cs`, `DocumentWindowController.cs` (piece 9's `Reveal`, `_revealedAt`, `FillRest`, `FinishFlips`), `TranscriptWindowController.cs`, `ReferenceBookWindowController.cs`, `CitizenRecordsWindowController.cs`, `InvestigationUIController.cs`, `TravellerWheel.cs`, `OverlayCallout.cs`;
  - `DeskController.cs`, `DeskDocument.cs`, `DeskDraggable.cs`, `BoothCoordinator.cs`, `PcFrame.cs`, `DeskConfigSO.cs`, `OfficeSceneBinder.cs`, `GameManager.cs` (the booth and view wiring), `UI/Translation/{CaseTranslation,TextFlip}.cs`;
  - the builder's `BuildPaperTemplate`, `BuildPcFrame`, `BuildOverlayCallout`, `BuildTravellerWheel`, `BuildOffice` and the verdict/citation block in `Build()`.

  Write `SCRATCH/p10/names.md` with every name the spec §3 uses and where it is. Note any that differ, and use the code's names from then on. Record `git ls-files --eol` for every file in the file map.
- [ ] **Step 4: Offline baseline.** Compile check, then test run. Record `passed N, failed 0` (expected 946).
- [ ] **Step 5: Unity baseline** (`_TimeDeskP10Baseline.cs`, from the p8/p9 baseline dumper): dump seeds 12345 and 999 × days 1–6 (day 6 also under the Japan and Egypt leaders), with piece 9's columns, to `SCRATCH/p10_baseline_cases.txt`.
- [ ] **Step 6: Font spike on a world text** (`_TimeDeskP10FontSpike.cs`, from `SCRATCH/p9/fonts_probe.cs.txt`):
  - In a new unsaved scene, create a `TextMeshPro` (3D) with the default font, 0.3 m wide. For the hieroglyph, Arabic (shaped) and Han scripts, resolve the runtime font through `CultureThemeService`'s `RuntimeFonts` (or a fresh `RuntimeFonts` as the probe did), then `TextFlip.Write` "Silver shekel (by weight)" as Untranslated through a `CaseTranslation` built as `TranslationPresenter` builds it.
  - Render each to `SCRATCH/p10_world_tongues.png` with a camera.
  - Pass: the glyphs draw (not tofu, not blank), and `git status` shows the tracked TMP assets unchanged.
  - **If a script fails, stop and report** (spec §4). Delete the temporary scripts.

### Task 1: Domain — the row source (`DocumentRows`) and `CaseDocument.fields`

- [ ] **Step 1: Failing tests** `Assets/Tests/EditMode/DocumentRowsTests.cs` (spec §5):
  - `Ordered_IsPageMajorThenAuthored`;
  - `Ordered_SkipsNulls_IndexIsFieldIndex`;
  - `Ordered_TongueRowCountsAcrossDocument`;
  - `OnPage_TongueRowCountsWithinPage`, with the permit: Declared Device page 0, Bond Currency page 1, both in the tongue, so each has TongueRow 0 on its page and 0 and 1 in `Ordered`;
  - `OnPage_OtherPagesExcluded`;
  - `PageCount_EmptyIsOne`, `PageCount_HighestPagePlusOne`;
  - `HasTongue_Passport_True`, `HasTongue_NameAndBirthDateOnly_False`, `HasTongue_Empty_False`.

  Use `new DocumentField { category, label, value, page }`. Compile: it fails (no `DocumentRows`).
- [ ] **Step 2: Implement** `Assets/Scripts/Domain/DocumentRows.cs` (spec §3.2): `DocumentRow` (a readonly struct with `Index`, `Field`, `TongueRow`) and the static `DocumentRows` (`Ordered`, `OnPage`, `PageCount`, `HasTongue`). The ordering is a stable sort by page. `TongueRow` uses `Translation.InTongue(field.category)`. Everything has `/// <summary>` docs. Then `make_meta.py`.
- [ ] **Step 3: Glue.**
  - `DeskPapers.cs`: `CaseDocument` gains `public IReadOnlyList<DocumentField> fields;` ("The document's fields: the rows its paper shows (DocumentRows)").
  - `CaseInstance.cs`: `DocumentInstance.PageCount => DocumentRows.PageCount(fields)`.
  - `InvestigationUIController.ShowRich`: set `fields = doc != null ? doc.fields : null` in the `CaseDocument` initialiser.
- [ ] **Step 4:** compile check; test run (base + 11).
- [ ] **Step 5: Commit** `feat(domain): one row source for a document's scanned page and its paper`.

### Task 2: Domain — `ComparePair` and `PickKeys`

- [ ] **Step 1: Failing tests** `ComparePairTests.cs`:
  - `FirstPick_Pending`, `SecondPick_Paired`;
  - `SameKeyAsA_Clears`, `SameKeyAsB_Clears`;
  - `ThirdPick_StartsNewWithItAsA`;
  - `BlankKeys_NeverMatchEachOther`;
  - `Matches_UsesMatchValue_Garment` (shown "Ionic chiton", evidence value "Chiton and himation" against a book row of that value: Matches);
  - `Matches_CaseInsensitiveTrimmed`, `Mismatch_Differs`;
  - `Clear_EmptiesBoth`;
  - `PickKeys_Formats` (`field:0:2`, `line:5`, `garment:1`, `book:Currency:greece:ancient`, `record:BirthDate`);
  - `PickKeys_SameFieldSameKey`, `PickKeys_KindsNeverCollide`.
- [ ] **Step 2: Implement** `Assets/Scripts/Domain/ComparePair.cs` (spec §3.2). `Matches` calls `DiscrepancyLog.ValuesMatch(A.Evidence.MatchValue(A.Shown), B.Evidence.MatchValue(B.Shown))`. Add the meta.
- [ ] **Step 3:** compile check; test run (+13).
- [ ] **Step 4: Commit** `feat(domain): the compare pair and pick keys, one rule for every surface`.

### Task 3: Domain — holds, slots, click routing, the note's count

- [ ] **Step 1: Failing tests** added to `DeskPapersTests.cs`:
  - `Hold_OnlyFromOnDesk` (with the traveller, scanning, returned and out of range are refused);
  - `Hold_PrefersSideWhenFree`, `Hold_OtherSideWhenPreferredTaken`;
  - `Hold_BothFull_EvictsHeldLongest` (the evicted paper is `CanDrag` and not `IsHeld`, and the new one takes its slot);
  - `PutBack_HeldToDesk`, `PutBackAll_InSlotOrder`;
  - `Held_CanDrag_ButDropRefused`;
  - `Scan_IndependentOfHolds`;
  - `ReturnAll_ClearsHolds`;
  - `OnDeskCount_CountsHeld`;
  - `PaperClicks_DecisionTable` (8 `[TestCase]`s over held × secondary × onRow: not held with a primary click gives Examine; not held with a secondary click gives None; held with a primary click on a row gives Pick; held with a primary click off the rows gives PutBack; held with a secondary click gives PutBack either way);
  - rename the existing `ScanHintVisible` tests' argument to `usesToday` (the cases are unchanged).
- [ ] **Step 2: Implement** in `DeskPapers.cs` (spec §3.2):
  - the private `Held` state;
  - `ExamineSlot`, `HoldResult`;
  - `Hold`, `PutBack`, `PutBackAll`, `IsHeld`, `SlotOf`, `HeldCount` (hold order kept in a `List<int>`);
  - `CanDrag` also true for `Held`; `Drop` still needs `OnDesk`; `OnDeskCount` also counts `Held`; `ReturnAll` clears the order;
  - `PaperClickAction`, `PaperClicks.Decide`;
  - the `DeskHints` parameter rename and doc.

  `DeskController.RefreshHint` passes `_scansToday` for now (Task 10 adds reads).
- [ ] **Step 3:** compile check; test run (+ about 22; record the number).
- [ ] **Step 4: Commit** `feat(domain): papers held in the hand, two slots, and how a click on a paper routes`.

### Task 4: Domain — the booth's input table grows (X15)

- [ ] **Step 1: Failing tests** in `BoothRulesTests.cs`:
  - update every existing `new BoothContext(…)` for the two new parameters (`stampOpen: false, papersHeld: false`) and keep their expectations;
  - add `StampOpen_MakesCrtPowerPropsPapersTravellerInert`;
  - `HeldPapersLive_InBothViews_NotWithWheelStampOrNewsletter`;
  - `DeskCatcherAndEscape_NeedHeldPapersInOfficeView`;
  - `StampTrayAllowed_OfficeViewTravellerAtDesk`;
  - `CaseHudVisible_NotWhileFocused`.
- [ ] **Step 2: Implement** (spec §3.2): the context gains `StampOpen` and `PapersHeld`; the outputs gain `HeldPapersLive`, `DeskCatcherLive`, `ExamineEscapeLive`, `StampTrayAllowed` and `CaseHudVisible`; the others gain `!StampOpen`. Then update `BoothCoordinator.Context()` to pass `false, false`: a behaviour-identical call.
- [ ] **Step 3:** compile check; test run (+5).
- [ ] **Step 4: Commit** `feat(domain): the booth's input table knows the stamp tray and held papers`.

### Task 5: Visuals — `ExamineLayout`

- [ ] **Step 1: Failing tests** `ExamineLayoutTests.cs`:
  - `Pose_WorkedExample` (fov 55, d 0.6: (−0.12494, −0.16242, 0.6), height 0.27486; tolerance 1e-4);
  - `OfficeSlots_Symmetric_BottomAtKnob`;
  - `OfficeSlots_NoOverlap_16x9_21x9`;
  - `OfficeSlot_DipLowersByWheelDip`;
  - `OfficeSlot_ClampedAtNarrowAspect_5x4`;
  - `InRegion_OneCentred`;
  - `InRegion_TwoSideBySide_WideRegion` (region width 1.2);
  - `InRegion_TwoStacked_NarrowRegion` (width 0.611);
  - `InRegion_TooSmall_DoesNotFit`;
  - `InRegion_BoxesStayInsideRegion`;
  - `SafeDistance_WorkedExample_NoCap` (1.09 m, normal · (right, up, forward) = (0, 0.98481, −0.17365): the lower-corner depth is 1.637, so 0.6 is kept);
  - `SafeDistance_CloseDesk_Caps`;
  - `SafeDistance_NearClipFloor`;
  - `SafeDistance_RaysUp_NoCap`;
  - `Ease_EndpointsAndMonotonic`.
- [ ] **Step 2: Implement** `Assets/Scripts/Visuals/ExamineLayout.cs`: `ExamineTuning` (`[Serializable]` with the §3.3 defaults), `ScreenBox`, the static `ExamineLayout`. Engine-free (`System.Math`/`MathF`). Add the meta.
- [ ] **Step 3:** compile check; test run (+15).
- [ ] **Step 4: Commit** `feat(visuals): the examine pose and slot layouts in screen-height units`.

### Task 6: Visuals — `PaperFace`

- [ ] **Step 1: Failing tests** `PaperFaceTests.cs`:
  - `Rows_DoNotOverlapTitleOrPhoto`;
  - `RowsBesidePhoto_EndLeftOfIt_LaterRowsFullWidth`;
  - `RowAt_InsideEachRow_ReturnsIndex`;
  - `RowAt_TitlePhotoMarginsGaps_MinusOne`;
  - `Capacity_Six_WithAndWithoutPhoto`;
  - `MoreRowsThanCapacity_LaysOutCapacity`;
  - `NoPhoto_FullWidthRows`.

  Use the defaults, paper 0.26 × 0.34.
- [ ] **Step 2: Implement** `Assets/Scripts/Visuals/PaperFace.cs` (spec §3.3): `PaperFaceTuning`, `FaceRect`, `FaceRow`, `FaceLayout`, `PaperFace`. Use `LookCanvas.PhotoAspect` for the photo's width. Add the meta.
- [ ] **Step 3:** compile check; test run (+7).
- [ ] **Step 4: Commit** `feat(visuals): the paper face layout and its row hit test`.

### Task 7: Visuals — `RevealClock`; the bubble's tag and hold

- [ ] **Step 1: Failing tests.**
  - `RevealClockTests.cs`: `NaNBeforeStart`, `FirstStartWins`, `ElapsedCounts`, `FinishIsInfinity`, `FinishBeforeStart_Nothing`.
  - `SpeechQueueTests.cs` additions:
    - `Tag_FollowsShownLine`, `Tag_NoneWhenEmpty`, `Clear_ResetsTag`;
    - `Hold_FullyShownLineNeverEnds`, `Hold_TypingContinues`;
    - `Release_EndsAfterHold`, `Release_EndsAfterMinWhenLineWaits`;
    - `Hold_QueuedLinesWait`.

    Every existing test stays unchanged.
- [ ] **Step 2: Implement** `Assets/Scripts/Visuals/RevealClock.cs` (with its meta), and in `SpeechQueue.cs`:
  - the queue tuple gains `int tag`;
  - `Say(…, int tag = -1)` and `Tag`;
  - `Hold(bool)`: while held, `Tick` advances `_elapsed` only up to `max(TypeSeconds, _reveal)`, and the end check is skipped. Update the class doc.
- [ ] **Step 3:** compile check; test run (+13).
- [ ] **Step 4: Commit** `feat(visuals): a shared reveal clock; speech lines carry a tag and hold while hovered`.

### Task 8: Glue — one evidence builder and one compare controller (the PC behaves as before)

**Behaviour-preserving: no FEATURES change.**

- [ ] **Step 1:** Create the new files:
  - `Assets/Scripts/UI/CompareHighlights.cs`: `ICompareHighlight`, `ImageHighlight`.
  - `Assets/Scripts/UI/EvidencePicks.cs` (spec §3.4; the labels are today's keys).
  - `Assets/Scripts/UI/DocumentRowView.cs`: `DocumentRowView.Write`, which moves piece 9's `FillRest` in (used only when the value's parent has a `HorizontalLayoutGroup`), and `DocumentReveal.Begin`.

  Add the metas.
- [ ] **Step 2: `CompareController`:**
  - replace `Slot` with a `ComparePair` plus two `ICompareHighlight` fields;
  - `Select(ComparePick, ICompareHighlight)` (spec §3.4);
  - add `[SerializeField] GameObject officeBar; TMP_Text officeText;` (optional, null-safe), with each view's text and colour written by one private `Draw`;
  - remove `Select(string, string, Image, CompareEvidence)`;
  - update the class doc.
- [ ] **Step 3: Migrate the callers**, each to `EvidencePicks` and a `new ImageHighlight(bg)`:
  - `DocumentWindowController`:
    - `SetDocument` gains `int index` and `RevealClock clock`;
    - `Rebuild` iterates `DocumentRows.OnPage` and writes through `DocumentRowView.Write`;
    - `_revealedAt` → the clock;
    - `Reveal()` → `DocumentReveal.Begin(_clock, …, Time.unscaledTime)` plus a rebuild, **still called at the scan in this task**;
    - `FinishFlips` → `_clock.Finish()` and complete the running flips;
    - add `Refresh()`;
  - `TranscriptWindowController` (the line index is its row index);
  - `ReferenceBookWindowController`, `CitizenRecordsWindowController`;
  - `InvestigationUIController.LookAt` (`EvidencePicks.ForGarment`, highlight null);
  - `InvestigationUIController.ShowRich`: a `List<RevealClock>` per case, one per document, passed to `SetDocument`.
- [ ] **Step 4:** compile check; test run (unchanged).
- [ ] **Step 5: Self-check.** `grep -rn "CompareEvidence\.\(For\|From\)" Assets/Scripts` lists only `EvidencePicks.cs` and Domain. `grep -rn "_compare.Select(\|compareController.Select(" Assets/Scripts` shows only `ComparePick` calls.
- [ ] **Step 6: Commit** `refactor(compare): one evidence builder, one pair rule and highlight kinds for every surface`.

### Task 9: Glue — the paper's whole face

- [ ] **Step 1: `DeskConfigSO`:** a `[Header("Examine (piece 10)")]` section with `ExamineTuning examine`, `PaperFaceTuning face`, `Color examineTint = Color.white`, `Color rowHoverTint = new Color(0f, 0f, 0f, 0.06f)` and `Vector2 stampTrayOffset = new Vector2(0f, 140f)`, each with a `/// <summary>` doc. Update the class doc.
- [ ] **Step 2: `DeskDocument`:**
  - `Bind(int index, CaseDocument doc, CaseTranslation tr, RevealClock clock, DeskConfigSO config)` (spec §3.4): clone the optional `rowTemplate` (children `Label`, `Value`, `Highlight`) per `DocumentRows.Ordered` row; place the rows, the title and the photo by `PaperFace.Layout(rows, doc.showsPhoto, paperSize.x / paperSize.y, config.face)`; write the rows through `DocumentRowView.Write`;
  - `Refresh()`; an `Update` that runs only while flips run;
  - remove `holder` (its field, its write and its doc lines);
  - a null `rowTemplate` (the old scene) shows the title and photo only.
- [ ] **Step 3: `DeskPapers.cs`:** remove `CaseDocument.holder`. Update the `holder =` uses in `InvestigationUIController.ShowRich` and in the test helpers (`DeskPapersTests.cs:15`, `InterviewScriptTests.cs:56`).
- [ ] **Step 4: `DeskController.BeginCase(docs, look, art, CaseTranslation tr, IReadOnlyList<RevealClock> clocks)`** binds each paper. `InvestigationUIController.ShowRich` passes `_caseTranslation` and the clocks.
- [ ] **Step 5:** compile check; test run (unchanged count).
- [ ] **Step 6: Commit** `feat(desk): papers carry their document's whole face (rows, photo, the tongue)`. FEATURES waits for Task 14: the face shows only with the rebuilt template.

### Task 10: Glue — examine, the frame region, and the reveal on sight

- [ ] **Step 1: `DeskDraggable`:** `public bool GrabAtCentre { get; set; }`. In `OnBeginDrag`, the grab offset is zero while it is true.
- [ ] **Step 2: `PcFrame`:** `public float RightEdgePixels` (the frame's right edge from `GetWorldCorners`) and `public void SetExamineHole(float xMin, float yMin, float xMax, float yMax)`, which sizes an optional serialized `RectTransform examineHole` (converting screen px to its canvas; an empty rect when there is no hole).
- [ ] **Step 3: `PaperExaminer`** (new, `Assets/Scripts/Office/Desk/PaperExaminer.cs`, with its meta), per spec §3.4:
  - serialized `DeskConfigSO config`, `DeskSurface surface`, `PcFrame frame`; `SetCamera(Camera)`;
  - `Hold`, `Release`, `SetMode(bool frameOpen, bool dipped)`, `Clear`;
  - `LateUpdate` only while animating or re-posing (it caches the screen size, the camera pose and the fov);
  - the pose from `ExamineLayout.OfficeSlot`/`InRegion`/`Pose`/`SafeDistance`, where the desk values are the camera's height above `surface` and `surface.transform.up`'s dots with the camera's axes;
  - the frame hole from the union of the held papers' boxes while the frame is open and they fit.
- [ ] **Step 4: `DeskDocument`:**
  - `IPointerClickHandler` raises `Clicked(DeskDocument, bool secondary, int row)` only while its `Clickable` is interactable, with the row from `PaperFace.RowAt` at the hit point in `sheet` space;
  - `IPointerMoveHandler`/`IPointerExitHandler` tint the hovered row while examined;
  - `SetExamined(bool)`, which swaps to an optional `examineMaterial` and tints the photo to `config.examineTint`;
  - `Sheet`; `SetLift` is ignored while examined; `RowHighlight(int)` (a nested `PaperRowHighlight` with a `MaterialPropertyBlock`).
- [ ] **Step 5: `DeskController`:**
  - route `Clicked` through `PaperClicks.Decide`:
    - Examine: `DeskPapers.Hold(i, preferRight)`, with `preferRight` taken from the paper's viewport x through the examiner's camera; on eviction, put the evicted paper back; then the examiner's `Hold`, `GrabAtCentre = true`, and raise `PaperExamined(i)` and `HoldsChanged`;
    - Pick: raise `FieldPicked(i, row, paper.RowHighlight(row))`;
    - PutBack: `DeskPapers.PutBack`, the examiner's `Release`, then `_stack.BringToFront` and `ApplyStack` on landing;
  - drop the `onClick` → `BringToFront` listener;
  - `HandleDragBegan` on a held paper puts it back instantly first;
  - `SetHeldLive(bool)`; an optional serialized `ClickCatcher deskCatcher` whose `onClick` puts every held paper back, active on `SetDeskCatcherLive(bool)`;
  - Escape in `Update` only while `ExamineEscapeLive` has held since an earlier frame (store `Time.frameCount` when it turns on);
  - `EndCase` puts back instantly, then slides;
  - `RefreshHint` passes scans + reads;
  - `HeldCount`.
- [ ] **Step 6: `BoothCoordinator`:**
  - the context passes `stampOpen: false` (Task 12 wires it) and `papersHeld: desk != null && desk.HeldCount > 0`;
  - subscribe to `desk.HoldsChanged`;
  - apply `desk.SetHeldLive(HeldPapersLive)` and `desk.SetDeskCatcherLive(DeskCatcherLive)`;
  - an optional serialized `PaperExaminer examiner` gets `SetMode(focused, wheelOpen)`.
- [ ] **Step 7: The reveal on sight (X25).**
  - `InvestigationUIController`:
    - a private `Sighted(int i)` (`DocumentReveal.Begin`, then `_docWindows[i].Refresh()` and `desk.RefreshPaper(i)`);
    - `public void SetFrameOpen(bool open)`: on open, `Sighted` every active document window;
    - `OpenDocumentWindow` calls `Sighted` only while the frame is open (and no longer calls `Reveal()`);
    - handle `desk.PaperExamined` → `Sighted`;
    - handle `desk.FieldPicked` → `_clocks[i].Finish()`, then `compareController.Select(EvidencePicks.ForField(i, row, _caseDocuments[i].name, _caseTranslation), h)`.
  - `DocumentWindowController.Reveal()` goes.
  - `GameManager`: where it already holds `officeView` and `investigationUI`, subscribe `officeView.ViewChanged += v => investigationUI.SetFrameOpen(v == OfficeView.MonitorFocus)` (and unsubscribe in `OnDestroy`). This works in the not-yet-rebuilt scene, so the PC flip still happens when the frame shows the window.
- [ ] **Step 8: `OfficeSceneBinder`:**
  - an optional serialized `PaperExaminer examiner`; `examiner.SetCamera(office)`;
  - an optional serialized `BoxCollider deskCatcher`, which `BindDesk` sizes to the clamp area, 1 mm thick and 1 mm under the plane, right after `surface.Configure` (its existing `PlaceBox`).
- [ ] **Step 9: FEATURES** (code-driven, spec §4 and §8.1):
  - `:88`, the reveal on first sight;
  - `:32`, the note until the first read or scan (the text changes in Task 14).
- [ ] **Step 10:** compile check; test run (unchanged).
- [ ] **Step 11: Commit** `feat(desk): papers lift into the hand, sit beside the open PC and reveal their translation on first sight`.

### Task 11: Glue — the bubble's answers are evidence

- [ ] **Step 1: `TravellerWheel`:**
  - keep `List<DialogLine> _said` for the case, cleared in `SetCanOpen(false)`, and pass `_said.Count` as the tag in `Say`;
  - `CurrentLine` (the line for `_speech.Tag`, or null);
  - `event Action<DialogLine> LineClicked`; `SetBubbleHovered(bool)` → `_speech.Hold`;
  - an optional serialized `Button bubbleButton`, made interactable in `ShowSpeech` only while `CurrentLine != null && CurrentLine.IsAnswer`;
  - `BubbleHighlight` (an `ICompareHighlight` on the bubble panel's `Image`, cleared when the line changes);
  - the same-frame Escape guard (X8);
  - update the class doc.
- [ ] **Step 2: `SpeechBubbleInput`** (new, with its meta): `IPointerEnterHandler`/`IPointerExitHandler` → `wheel.SetBubbleHovered`; its button's `onClick` → the wheel's `ClickLine()`, which raises `LineClicked(CurrentLine)` when it is an answer.
- [ ] **Step 3: `InvestigationUIController`:** subscribe to `wheel.LineClicked`. Its handler selects `EvidencePicks.ForAnswer(index, line, _caseTranslation)` with `wheel.BubbleHighlight`, where `index` is the line's index in `_runner.Transcript` (the key `line:{index}` matches the transcript row's).
- [ ] **Step 4:** compile check; test run.
- [ ] **Step 5: Commit** `feat(wheel): the bubble's answer can be picked for comparison, and hovering holds the line`.

### Task 12: Glue — the stamp tray, the office case HUD

- [ ] **Step 1: `StampTray`** (new, with its meta; the wheel's host pattern):
  - `catcher` (a child), `panel`, `acceptButton`, `denyButton`;
  - `SetCamera`, `SetFollow(Transform)`, `Open()`, `Close()`, `SetCanOpen(bool)`, `IsOpen`, `OpenChanged`, `Decided`;
  - placed by `OverlayProjection.TryPlace` with `config.stampTrayOffset`; Escape with the same-frame guard; a click on the catcher closes; a choice raises `Decided` and closes.
- [ ] **Step 2: `OfficeCaseHud`** (new, with its meta): `claimRoot`, `claimText`; `SetClaim(string)`, `SetVisible(bool)`.
- [ ] **Step 3: `BoothCoordinator`:**
  - optional `StampTray stampTray`, `OfficeCaseHud hud`;
  - the context's `stampOpen`; subscribe to `stampTray.OpenChanged`;
  - apply `stampTray.SetCanOpen(StampTrayAllowed)` (tray first, like the wheel) and `hud.SetVisible(CaseHudVisible)`.
- [ ] **Step 4: `InvestigationUIController`:**
  - optional `OfficeCaseHud hud`, `StampTray stampTray`;
  - `ShowRich` formats `claim.banner` once, into `claimText` and `hud.SetClaim`;
  - `Hide` clears it;
  - subscribe `stampTray.Decided += Decide`.
- [ ] **Step 5: `OfficeSceneBinder`:** optional `StampTray stampTray`; `SetCamera(office)`; `SetFollow` to the stamp prop's click box (the `PropBinding` for `OfficeAnchorId.Stamp`).
- [ ] **Step 6:** compile check; test run.
- [ ] **Step 7: Commit** `feat(office): the stamp gives the verdict at the desk; the claim stays on an office tag`.

### Task 13: The builder (X24) and the note's text

Edit `OfficeSceneUIBuilder.Desk.cs` and `OfficeSceneUIBuilder.cs` (spec §3.5). Every new graphic is tagged (the builder's untagged-graphic check must stay silent), and every new object is rebuilt each run (`DestroyChildIfPresent`).
- [ ] **Step 1: `BuildPaperTemplate`:**
  - remove `Holder`;
  - add `Rows/RowTemplate` (a `Label` TMP 0.05–0.02 m auto-size, a `Value` TMP, and a `Highlight` quad with `PaperRow_Highlight`: URP Unlit, transparent, `_BaseColor` alpha 0, via `EnsureMaterial`);
  - add `Paper_Examine` (URP Unlit, the paper texture);
  - wire `rowTemplate` and `examineMaterial`.
- [ ] **Step 2: `BuildDesk`:**
  - `Desk/Catcher`: a `BoxCollider` on the Interactable layer with a `ClickCatcher`, inactive (the binder sizes it at load, Task 10 Step 8);
  - `Desk/Examiner` (`PaperExaminer`: config, surface, frame);
  - wire `DeskController.deskCatcher` and `OfficeSceneBinder.deskCatcher`.
- [ ] **Step 3: `BuildPcFrame`:** `Root/ExamineHole` (no graphic) and a second `RectHoleRaycastFilter` on `ExitCatcher` with `hole` = it; wire `examineHole`.
- [ ] **Step 4: `Build()` overlay order**, each rebuilt:
  - after `BuildFallbackHud`: `BuildOfficeCaseHud` (the claim strip and text: `ClaimStrip`, 1100 × 64 at 16 px from the top, 26 pt, fit; the office compare strip: `CompareBar`, 1200 × 56 at 88 px, 22 pt, fit, inactive);
  - then the PC frame, the wheel, and the speech bubble built *after* the wheel (move the call). Its panel `Image.raycastTarget = true`, with a `Button` (transition None, not interactable) and `SpeechBubbleInput`;
  - then the tooltip, and `BuildStampTray` (a host, a `Catcher` with a `ClickCatcher` pattern like the wheel's, a `Panel` 420 × 96, Accept and Deny through `MakeButton` with the `accept`/`deny` keys and the decision roles, and `BuildDecisionGlyph`);
  - then the verdict strip and text and the citation panel, built on `officeCanvas` instead of `root` (the desktop). Remove any desktop copies with `DestroyChildIfPresent(root, …)`.
- [ ] **Step 5: Wiring:**
  - `compare.officeBar`/`officeText`; `invest.hud`/`stampTray`;
  - `coordinator.stampTray`/`hud`/`examiner`;
  - `binder.examiner`/`stampTray`;
  - `wheel.bubbleButton`;
  - the stamp prop's `onClick` → `StampTray.Open` (`WirePersistentVoid`, next to its reaction).
- [ ] **Step 6: Checks** in `BuildOffice`, next to the spawn-slot check: for each template in `ContentLibraryValidator.TravellerBlueprints(library)`'s documents, if its field count exceeds `PaperFace.Capacity(template.showsPhoto, config.face)`, log `Debug.LogError($"[TimeDesk] {template.displayName} has {n} fields but a paper face holds {cap}; raise Desk_Default.face or shorten the template.")`.
- [ ] **Step 7: `world_source.json`** (subs.py on its exact line, LF, UTF-8): `desk.scanHint` → "Click a paper to read it; drag it onto the scanner to open it on the PC."
- [ ] **Step 8:** compile check; test run.
- [ ] **Step 9: Commit** `build(office): the paper face, the examiner, the stamp tray, the office case HUD and the overlay verdict`.

### Task 14: Unity — regenerate, rebuild, suite (the scene commit)

- [ ] **Step 1:** `_TimeDeskP10Build.cs`:
  - Generate World twice; the second run changes nothing;
  - Validate Content Library: clean;
  - Build Office UI twice, dumping each build semantically (the office move's dumper) to `SCRATCH/p10_build_{1,2}.txt`; they must be equal;
  - the log holds no error or warning from the builder;
  - `git diff --quiet -- Assets/Scenes/OfficeScene.unity` (the art office is unchanged).
- [ ] **Step 2: EditMode suite** (`TestRunnerApi`): every TimeDesk test passes; report the two known UnitySkills failures.
- [ ] **Step 3: Hygiene:** revert unrelated Unity-touched files; delete `_TimeDeskP10*`.
- [ ] **Step 4: FEATURES** (builder-driven lines, spec §8.1): `:18`, `:30`, `:31`, `:32` (the text), `:33`, `:34`, `:35`, the new HUD bullet, `:45`, `:62`, `:70`, `:79`, `:151`.
- [ ] **Step 5: Commit** `feat(scene): examine papers at the desk; bubble picks, the stamp and the office case HUD` with the scene, the library, `Desk_Default.asset` (only if changed), the two materials and their metas, the regenerated UI table, and `docs/FEATURES.md`.

### Task 15: Unity — the play-through in the art office

- [ ] **Step 1:** `_TimeDeskP10Play.cs`, from `_TimeDeskMovePlay.cs`: the art office loaded by Title → New Run (seed 12345), the virtual mouse and keyboard, `Resolution`, `Shot` and `Check`. Implement spec §6.5.1–15. Every resolution-dependent check and screenshot runs at **1920×1080, 2560×1080 and 1280×720**. For day 3, grant `tr_near_east_{written,spoken}` and `tr_mediterranean_{written,spoken}` as piece 9's run did.
- [ ] **Step 2:** Run it and read `SCRATCH/p10_play_report.txt`. **0 failures** is required. Look at every screenshot by eye: the slots clear of the head and the bubble, text readable at 720p, glyphs drawn, the strip and tag clear of the bubble.
- [ ] **Step 3:** Fix forward. Each fix is a `fix(…)` commit, with FEATURES in the same commit when behaviour or a documented default changes. Knob changes in `Desk_Default.asset` are commits too. Re-run until clean.
- [ ] **Step 4: Determinism.** Re-dump generation. It must be byte-identical to `SCRATCH/p10_baseline_cases.txt`.
- [ ] **Step 5: Hygiene** as in Task 14.

### Task 16: The record

- [ ] **Step 1:** Append a "§11 Verification record" to the spec: the offline totals, the suite, content, builder dumps, the play-through checks by number, the screenshots list, the knob values used, and any departures from §3 (the code wins; name each).
- [ ] **Step 2:** Update the plan's file map if a file changed name.
- [ ] **Step 3: Commit** `docs(spec): examine the papers at the desk, verification record`.
- [ ] **Step 4:** `git status --short` is clean (untracked `*.sln`/csproj aside) and `git log --oneline origin/main..` lists this piece's commits. **Stop here.** The orchestrator reviews, fast-forwards `main` and pushes.

### Task 17 (scope addition): the desk view (spec §11, T1–T6)

- [ ] **Step 1 (Domain, tests first):** `BoothRulesTests`: the context gains `deskView`; three columns (`M` the mat toggles, `X` Escape and the right-click return, `V` the desk view allowed) on every row, new rows for the desk view (office, no traveller, papers held, wheel, stamp tray, frame open, newsletter); "the desk view changes no other output"; the frame test covers the new outputs. See them fail to compile, then add `BoothContext.DeskView` and the outputs to `BoothRules`. Commit `feat(domain): the booth's input table knows the desk view`.
- [ ] **Step 2 (Visuals, tests first):** `DeskViewPoseTests`: the pitch aims at the mat's centre from the moved pose (forward shortens the depth, rise adds height), plus the pitch knob; it stays between 0 and 89°; the seconds are the knob's, 0 under Reduced Motion or a negative knob. Then `DeskViewTuning` and `DeskViewPose`. Commit `feat(visuals): the desk view's pose`.
- [ ] **Step 3 (glue):** `OverlayProjection.TryPlace(..., keepOnScreen)`, `OverlayCallout.keepOnScreen`, the wheel's ring; `DeskConfigSO.deskView`; `DeskView`; `BoothCoordinator` (the context, the outputs, the return at NEXT); `OfficeSceneBinder` (the view catcher, `DeskView.Bind`). `docs/FEATURES.md` in the same commit. Compile; offline tests. Commit `feat(office): a click on the mat tilts the camera over the desk`.
- [ ] **Step 4 (builder):** `Office/DeskView` with its inactive `Camera` child (`CinemachineCamera`, priority 0), `Office/Desk/ViewCatcher` (inactive), the references, the bubble's `keepOnScreen`. Commit `build(office): the desk view's camera and the mat's catcher`.
- [ ] **Step 5 (Unity, job files):** the pass job (Generate World idempotent, validator, builds ×3 equal by semantic dump, wiring checks for the new objects, the art office byte-unchanged, player scripts, the EditMode suite). Commit the rebuilt `OfficeGameplay.unity` (`feat(scene): the desk view`).
- [ ] **Step 6 (Unity, job files):** the play-through gains the desk view's checks (§11 T6) and its screenshots `p10_desk_view_{1080,2560,720}`; judge them. Record in §13.

## Expected commits (in order)

1. `docs(spec): examine the papers at the desk (piece 10) design`
2. `docs(plan): examine the papers at the desk (piece 10) implementation plan`
3. `feat(domain): one row source for a document's scanned page and its paper`
4. `feat(domain): the compare pair and pick keys, one rule for every surface`
5. `feat(domain): papers held in the hand, two slots, and how a click on a paper routes`
6. `feat(domain): the booth's input table knows the stamp tray and held papers`
7. `feat(visuals): the examine pose and slot layouts in screen-height units`
8. `feat(visuals): the paper face layout and its row hit test`
9. `feat(visuals): a shared reveal clock; speech lines carry a tag and hold while hovered`
10. `refactor(compare): one evidence builder, one pair rule and highlight kinds for every surface`
11. `feat(desk): papers carry their document's whole face (rows, photo, the tongue)`
12. `feat(desk): papers lift into the hand, sit beside the open PC and reveal their translation on first sight`
13. `feat(wheel): the bubble's answer can be picked for comparison, and hovering holds the line`
14. `feat(office): the stamp gives the verdict at the desk; the claim stays on an office tag`
15. `build(office): the paper face, the examiner, the stamp tray, the office case HUD and the overlay verdict`
16. `feat(scene): examine papers at the desk; bubble picks, the stamp and the office case HUD`
17. `fix(…)` commits from the play-through, if any
18. `docs(spec,plan): the desk view (scope addition)`
19. `feat(domain): the booth's input table knows the desk view`
20. `feat(visuals): the desk view's pose`
21. `feat(office): a click on the mat tilts the camera over the desk`
22. `build(office): the desk view's camera and the mat's catcher`
23. `feat(scene): the desk view`
24. `docs(spec): examine the papers at the desk, verification record`

## Risks for the implementer

- **The compare refactor (Task 8)** touches every PC comparison. Keep it behaviour-identical, and prove it with Task 15's PC checks (§6.5.7, §6.5.8, and the Deviation Report lines) before judging the desk.
- **Overlay above world:** any overlay graphic over a held paper eats its clicks. After Task 13, hover the slots at each resolution with the HUD visible, and confirm the raycast reaches the paper.
- **World TMP with runtime fonts:** the Task 0 spike decides it. If it fails, stop and report; do not work around it silently.
- **Escape order:** three components read Escape. The same-frame guard must be in each. §6.5.11 proves it.
- **The art's layout:** the slot knobs were measured on `23aa6e1`. If `E:\unity\NOPE-art`'s art office differs, tune `Desk_Default.examine` from the screenshots and record the values (Task 16).
