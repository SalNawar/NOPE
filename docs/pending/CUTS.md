# P0-C CUTS

Branch `fix/p0-cuts`, cut from `fix/p0-carve` (f347060). Source: FIX_PLAN_v2.md "P0-C CUTS" and Saleh's calls in DECISIONS_1019a.md (later entries override earlier ones; the portal ticket tube was dropped, so nothing is added for portals).

**What it is:** removing what Saleh cut, in one change, so that no Phase A track fixes a surface that is going away and nothing cut can be ordered, taught or clicked.

**What it is not:** it moves nothing and adds nothing (the two small exceptions needed to remove things cleanly are listed under "Decisions"). It leaves the click-to-enlarge alone, because DOC-HANDLE owns it.

`docs/FEATURES.md` and `docs/CHEATS.md` are **not** edited here. INTEGRATION folds this file in; the lines to rewrite are listed under "FEATURES and CHEATS".

## The cuts, and why

| # | Cut | Saleh's words (DECISIONS_1019a.md) |
|---|---|---|
| 1 | **The magnifiers:** the lens stand (`LensKit`), the loupe, the bar, the Fresnel arm (`DeskMagnifier`), and the red SPACE inspect button with its code-drawn magnifier (`MagnifierGlyph`) | "not sold"; the scanner's hidden-layer view will show the microprint instead (D5). FIX_PLAN_v2 §1b: 59 "SPACE magnifier button: CUT", 60b "lens levels and stickers: CUT", 65c "DeskMagnifier part: CUT" |
| 2 | **The old stamp:** its click box (it was a hidden third TAB that called `DeskStampTray.ToggleRack`), its decor entry with its drag handle, and its art parts | "Old stamp: cut." (D5) |
| 3 | **Machine close-ups:** `MachineCloseUp`, `CloseUpTimeline`, their triggers, and the Settings "Machine close-ups" row | D6: "when they slide in, we subtly zoom in a bit not too hard just a small focus to frame the shot", so there are no full machine close-ups. FIX_PLAN_v2 64b, 65e and 73e: CUT |
| 4 | **Talk routes other than the intercom:** clicking the traveller and clicking the art's phone no longer open the wheel. The desk intercom (`DeskFurniture`, built by the office binder) is the one way to talk | "intercom to bring out dialogue options" (D5, Talking) |
| 5 | **Dead dialogue options:** "Step closer" and "Speak up" (`interview.requests`), with the 90 voice rows that answered them (`interview.voices.spoken`) | "Step closer": "there are dialogue that does nothing like step closer" (D5). "Speak up": **our proposal** (FIX_PLAN_v2 P0-C item 5), not Saleh's call; it is R7, which DECISIONS_1019a.md lists as not asked yet (see Decisions, 7) |
| 6 | **ReprintedPaper, for now:** never drawn, although days 9 onward still list it | Our proposal (FIX_PLAN_v2 P0-C item 6): with the magnifiers gone nothing can prove it, so it waits until the scanner shows the microprint (re-enabled at #17) and no lie is unprovable in between |

## What was removed

### Code (deleted files)
- `Scripts/Office/Desk/DeskMagnifier.cs`, `Scripts/Office/Desk/LensKit.cs`, `Scripts/UI/MagnifierGlyph.cs`
- `Scripts/Office/Desk/MachineCloseUp.cs`, `Scripts/Domain/CloseUpTimeline.cs`
- All with their `.meta` files.

`Visuals/LensText.cs` stays. It is the Translation Lens's hover-translation engine (`LensWord`, `LensWords`, `LensFlip`), not the magnifiers'; GLASSES reuses it (FIX_PLAN_v2 #52).

### Code (members removed)
- **DeskUpgrades:**
  - `lensKit`
  - `TryLensFootprint`
  - `Shown` (only MachineCloseUp read it)
  - `BindHardware`
  - `HandleWord` and its `WordTurned` subscription
  - the lens block in `BeginDay`
  - the lens release in `EndCase`
  - `LayOne(lensKit, …)`
- **DeskHardware:** `Upgrades` (only MachineCloseUp read it), `TryLensFootprint`, and the `upgrades.BindHardware(this)` call.
- **DeskController:** the lens stand's paper clearance in `ClearOfBlockers`, and `Scanning` (only MachineCloseUp read it).
- **Rolodex:** `Clicked` (raised for the close-up only).
- **TranslationLensPresenter:** `WordTurned` (only the loupe's tick heard it).
- **DeskLayoutSettings:** the `lensKit` bay; the five `closeUp*` knobs; the decor item `old_stamp`.
- **DrawerSequence:** `PropArt.LensKit`.
- **SoundCues:** `LoupeTick` and `ArmFold`, with their placeholder cases in `CueSounds`.
- **MotionPreference:** `MachineCloseUps` (its PlayerPrefs key `TimeDesk.MachineCloseUps` is no longer read).
- **SettingsWindowController:** `closeUpsOnButton`, `closeUpsOffButton`, `ChooseCloseUps`.
- **DeskInspect:**
  - `inspectButton`, `onState`, `wheel`;
  - `SetLive(bool live, bool shown)` becomes `SetLive(bool live)`, and BoothCoordinator's call follows;
  - `TravellerClicked` now only picks the face in inspect mode.
- **GuideDirector:** `inspectButton` and its `"inspect"` arrow target. `GuideTargets.Named` loses `"inspect"`.
- **OfficeSceneBinder:** the `MachineCloseUp` it added at bind.

### Builder (Build Office UI)
- **`.Tools.cs`:** `BuildLensKit` and `Magnifier` removed.
- **`.Upgrades.cs`:** `LensKitModel`, the seven `Lens_*` prop slots and the `lensKit` wiring removed.
- **`.Chrome.cs`:** `BuildInspectButton`, its three sizes, and its entry in the city view's `deskControls` removed.
- **`.Kit.cs`:** `KitInspect`, `InspectKeycap` and the kit pass's call removed.
- **`.Guide.cs`:** the `inspectButton` wiring and its "unwired" check removed.
- **`.Apps.cs`:** the Settings window's `MachineCloseUpsRow` removed.
- **`.Desk.cs`:**
  - `Prop("Stamp", …)` and its `ToggleRack` call removed, plus the inspect `wheel` reference;
  - the art phone's `Intercom` prop keeps its squash, but its `TravellerWheel.Open` call is cleared (`ClearPersistentCalls`);
  - stale objects of an older build are destroyed: `Props/Stamp` and the overlay's `InspectButton`.

### Art, data and tools
- **Art deleted:**
  - `Art/Office/Gameplay/Machines/LensKit.fbx`
  - `LensKit_Fresnel.png`
  - the seven `Materials/PropArt_Lens_*.mat`
  - `Data/Config/DeskReactions/Reaction_Stamp.asset`
  - all with their `.meta` files.
- **`tools/props/machines`:** `upgrade_props_model.py` loses `build_lens`, the LensKit contract, `GLASS` and the `lens` prop key; `machine_textures.py` loses `fresnel_rings`.
- **The scene contract** (`OfficeSceneContractSO` and `Data/Config/OfficeSceneContract.asset`):
  - the `Stamp` anchor spec is gone;
  - the old stamp's three art parts (`Clean_Stamp__DeskClean_Wood`, `…_Brass` and `…_Dark`) join `legacyRoots`, so they are switched off when the art office loads;
  - `OfficeAnchorId.Stamp` stays in the enum, because it is append-only.
  - `docs/SCENE_CONTRACT_GAMEPLAY.md` follows.
- **`world_source.json`** (2-space indent; regenerate with Generate World):
  - `interview.requests` is now `[]`, and `interview.voices.spoken` is now `[]` (90 rows, all for the two cut requests).
  - The UI strings `settings.machineCloseUps`, `…On`, `…Off` and `controls.inspectHint` are removed from the reading table and from all 6 culture tables: 28 entries.
  - `guide.basics[1]` drops "or the red magnifier".
  - The FTUE step `inspect` now reads "Press {inspect} to inspect.", and its arrow target is `paper:TC-101` (the Passport) instead of the cut button.
  - The guide page `lens` drops the loupe sentence.
  - Day 9's bulletin drops "WARNING: reprinted tickets are about…under the loupe…".
  - The fault rows `reprintedTicket` and `reprintedPermit` drop "the lens stand's loupe ·" from `against`.
  - The `machines` contact's tip drops "and under the loupe".
- **ReprintedPaper:** `LieKinds.IsDormant(lie)` (true for ReprintedPaper only), and `LieKinds.For` skips a dormant lie. This feeds both CaseFactory draws (`Lies.Roll` and the place-lie list). The days' `lies` lists keep it, so #17 re-enables it by deleting one line.
  - Seeded draws on days that list it change: the pool is one lie smaller. Expected.

## Tests

- **Deleted (they only covered cut code):** `Assets/Tests/EditMode/CloseUpTimelineTests.cs`, all 5 of its tests:
  - `AMachineUsed_IsShownClose_ForItsHold`
  - `TheSettingOff_OrADrag_ShowsNothing`
  - `AnyInput_GivesTheViewBack_AtOnce`
  - `AMachineStillWorking_KeepsItsCloseUp_AnotherDoesNot`
  - `TheNextMachine_TakesItOver`
- **Changed:**
  - `UpgradeObjectsTests.PropContracts_NameEachObjectsMovingParts` drops `PropArt.LensKit` from its list.
  - `LieKindsTests`: one assertion message changed.
- **Added:** `LieKindsTests.For_NeverDrawsTheDormantReprintedPaper_UntilTheScannerShowsTheMicroprint`. It was red first (a compile error), then green.
- **Kept on purpose:**
  - `MicroLensTests` (MicroLens stays with TOOLS: FIX_PLAN_v2 65c).
  - The tests that use "Step closer"/"Speak up" as fixture rows: InterviewScriptTests, VoiceChecksTests, VoicesTests, DialogChoiceKindsTests, SeedsTests, and a BubbleLayoutTests comment. They test the spoken-request mechanism, which still exists, with their own data, not the cut content.
- **QA sweep** (`tools/audit/unity/_TimeDeskQASweep.cs.txt`):
  - the wheel step now checks that a click on the traveller opens nothing, then opens the wheel through the desk intercom's click;
  - the city-view checks no longer name the inspect button;
  - `TimeDesk.MachineCloseUps` is out of the preferences it saves and restores.

| Check | fix/p0-carve | fix/p0-cuts |
|---|---|---|
| `compile_check_t5.py` | 0 errors, 161 warnings | 0 errors, 161 warnings |
| Offline runner | passed 3714, failed 0 | passed 3710, failed 0 (−5 CloseUpTimeline, +1 dormant lie) |

## Deferred to Unity (code-only run)

These need the editor. Until they run, **the branch's OfficeGameplay.unity and the generated content are stale.** The scene still holds the LensKit with its DeskMagnifiers, the InspectButton with its MagnifierGlyph, `Props/Stamp`, the phone's wheel call and the close-ups row (missing scripts for the deleted classes). The content library still has the two requests and the old strings.

1. **Generate World**, then the content validator: green. Then a grep over `world_source.json` and the generated assets (`Assets/Data/**`) finds 0 references to the cut items: magnifier, loupe, Fresnel, lens stand, step_closer, speak_up, machineCloseUps, inspectHint. Commit the regenerated assets.
2. **Build Office UI**, then compare OfficeGameplay's semantic dump with fix/p0-carve's. The diff must hold only these removals:
   - `DeskUpgrades/LensKit` and its parts;
   - `OfficeOverlayCanvas/InspectButton`;
   - `Office/Props/Stamp`;
   - the `Intercom` prop's persistent `TravellerWheel.Open` call;
   - DeskInspect's `wheel`, `inspectButton` and `onState`;
   - GuideDirector's `inspectButton`;
   - SettingsWindowController's two close-up buttons and the `MachineCloseUpsRow`;
   - the city view's `deskControls` entry for the button;
   - the lens prop materials.

   Commit the scene.
3. **The EditMode suite in Unity.** It must be green, except the known third-party UnitySkills failure. It includes the carve's 12 `NeedsEditor` seam tests.
4. **Smoke play:** day 1, then a cheat jump to day 9.
   - 0 console lines that fix/p0-carve doesn't log. The binder's "found by fallback paths" line loses its `Stamp = …` entry; that is a listed removal.
   - A virtual click on the traveller and on the art's phone opens no wheel.
   - A click on the desk intercom opens it, with 0 camera moves from the office view.
   - **Expected exception:** from the reading view, opening the wheel still returns the view. `BoothRules.DeskViewAllowed` is false while the wheel is open; this is BUGLIST's "the intercom kicks the player out of the reading view". That is CAM/FEEDBACK's to change, not a cut.
   - The old stamp's art is not visible.
   - Day 9 draws no ReprintedPaper traveller.

## What each track must know

- **TOOLS:**
  - You no longer own DeskMagnifier, LensKit, MagnifierGlyph or the lens stand; they are deleted. `.Tools.cs` holds the UV torch only.
  - **Kept for you, without a caller:**
    - `MicroLens` (with `LensShape` and its tests);
    - `MicroPaperLayer.Track`;
    - `DeskHardware.TrackLens`;
    - `DeskReadings.ReadMicro` (the QA play probe still calls it).
  - The microprint line is still printed on every microprint form, but it is never read. #17 (the scanner shows the microprint) either feeds `ReadMicro` from the scanner and deletes the lens geometry, or reuses it.
  - Re-enable ReprintedPaper at #17 by deleting `LieKinds.IsDormant`'s ReprintedPaper case (and the method, if it is then empty), and by restoring the guide and bulletin teaching in the scanner's words.
- **CAM:**
  - `MachineCloseUp` and its blend hook are gone, so 73e's "static Cinemachine blend-hook leak" now concerns only DeskView's hook.
  - D6's small framing zoom when a machine slides in is new work, and it is not here.
  - The red SPACE button is gone. Inspect mode is SPACE only, and it has no on-screen "on" state apart from the highlights (the button's lit ring and its "click two things to compare" hint went with it).
  - BUG-59's "inspect becomes a desk object" is still open.
- **FEEDBACK / DIALOGUE:**
  - The wheel opens only from the desk intercom (`OfficeSceneBinder.OpenWheel`). `DeskInspect.TravellerClicked` only picks the face in inspect mode.
  - `interview.requests` and `voices.spoken` are empty. The spoken-request mechanism (InterviewRequest, `Voices` spoken, `VoiceChecks.SpokenRequests`, `VoiceKeys.Spoken`) stays for DIALOGUE to reuse (for example for "inform the traveller") or delete.
- **CONTENT:**
  - The FTUE `inspect` step points at the Passport.
  - The `lens` guide page now only teaches the hover translation.
  - Day 9's bulletin no longer warns about reprinted tickets.
  - The `machines` contact's lie list still names ReprintedPaper. No traveller carries it, so it never fires.
  - `ContentSheetMap`'s `microForms` note still says "read under the lens stand's magnifiers". It was left unchanged so that the spreadsheet template needs no regeneration: fix it with the template at #17.
- **DESK-PLAN:**
  - The lens stand's bay (`lensKit`) and the old stamp's decor entry are gone, and the remaining decor settles as before (the old stamp was last in zone 0).
  - The art's phone (the `Intercom` anchor) is now a squash-only prop. CODE-20 says to cut it unless it gets a function; that is your call to raise.
  - The RING SERVICE stickers are **not** cut. 60b's "stickers wait for the crate" is still yours under 60a.
- **STAMP:** the old stamp no longer toggles the rack. The rack's TAB and STAMPS tab are unchanged.
- **UI-PC:** Settings loses its Machine close-ups row. `settingsWindowSize` keeps its 1010 u height; reclaim the row's height if you want it.
- **QA-RIG:** the QA sweep's wheel step and city checks have changed (see Tests). The play probe's `ReadMicro` path stays, but no Micro tell is planned while ReprintedPaper is dormant.
- **Art side (Saleh / ChatGPT):** the hall art may delete `Clean_Stamp__*` (the game switches them off). The kit's inspect-button pieces (`inspect`, `inspect_hover` in `kit_manifest.json`) are unused now. `ArtDeliverables/TimeDesk/Audio/SOUND_LIST.md` rows 103 `loupe_tick` and 104 `arm_fold` have no player any more.

## Stale references left for the tracks

These still name cut items. They are not edited here; each owner fixes its own when it next touches the file.
- **`docs/pending/CARVE.md`** (the file to owner map), for INTEGRATION: line 47 lists `BuildInspectButton` under `.Chrome.cs` (CAM), and line 56 lists `BuildLensKit` and `Magnifier` under `.Tools.cs` (TOOLS). Both are deleted (see "Builder" above). Line 158 (carve decision 2) records where they went in the carve; it is history and stays true of f347060.
- **`Assets/Scripts/Domain/ContentSheets/ContentSheetMap.cs:749`**, for CONTENT: the guide `point` note still lists `inspect` as a valid target, but `GuideTargets.Named` no longer accepts it (the guide check, `GuideTargets.IsKnown`, refuses a page that points there). Drop `inspect` from the note with the template at #17, alongside the `microForms` note at :124 (see CONTENT above). The `{inspect}` key placeholders at :740, :755 and :762 are the SPACE key's print, not the button, and stay.
- **`docs/ART_ASSET_LIST.md:177`**, for the art side / INTEGRATION: the "Inspect button" row (`inspect_button.png`, `inspect_button_on.png`, `MagnifierGlyph`) describes the cut button. Drop the row.

## FEATURES and CHEATS (for INTEGRATION)

Rewrite these `docs/FEATURES.md` lines (line numbers as of f347060):
- **51 (the desk machines):** the microprint line is printed but cannot be read until #17, and `LieKind.ReprintedPaper` is dormant (`LieKinds.IsDormant`, tested: `LieKindsTests.For_NeverDrawsTheDormantReprintedPaper_UntilTheScannerShowsTheMicroprint`).
- **52 (the upgrades' desk objects):** drop the Translation Lens's stand, LensKit, its magnifiers, `TryLensFootprint`, loupe_tick and `WordTurned`.
- **53 (the desk's plan):** drop the lens stand's bay, the old stamp from the decor list, and the machine close-ups (with "tested: `CloseUpTimelineTests`").
- **24 (closing the PC):** a click on the room beside the PC goes on to what it lands on, but the traveller no longer opens the wheel; only the intercom does.
- **55 and 62 (the traveller wheel, the reading view):** only the desk intercom opens the wheel. Clicking the traveller or the art's phone does not.
- **59, 63, 64 and 71 (inspection, the guide, the controls, the desk's pieces):**
  - no red inspect button: SPACE toggles inspect mode;
  - the FTUE's inspect arrow points at the Passport;
  - no inspect button kit piece or keycap.
- **58 (the city view):** only the PC and STAMPS pull tabs fade out; there is no red inspect button.
- **121 (the wheel entries):** no spoken requests today.
- **189 (motion settings):** no Machine close-ups row.

`docs/CHEATS.md`: no cheat was removed. "Lens: Word / Sentence / Object / remove" still grant the Translation Lens's levels (the hover translation), but no stand appears on the desk any more. Say so in the "Lens" row.

## Decisions taken in the cuts (orchestrator's, not Saleh's)

1. **What "the magnifiers" covers.** The physical lens stand and its three magnifiers, and the red SPACE chrome button (FIX_PLAN_v2 59).
   - **Not covered:** the Translation Lens itself (the hover translation, its three Orders levels and its cheats). D7's GLASSES replaces it later. Until then, cutting it would leave the language days unplayable.
   - **Not covered:** the Investigation app's magnifier desktop icon (`tile_magnifier`).
2. **Hiding the old stamp's art.** The art scene is never edited, so its three parts join the contract's existing `legacyRoots` switch-off. The binder's anchor warning skips an anchor the contract lists no spec for, so the cut `Stamp` anchor raises no new console line. With no contract assigned, every default or missing anchor still warns, as before. That condition is the one line of new logic.
3. **The cut inspect button's FTUE step** keeps its action (Inspect) but needs a valid arrow target. It points at the Passport, the paper being inspected, rather than at nothing.
4. **The builder destroys the stale objects** (`Props/Stamp`, `InspectButton`) and clears the phone's persistent wheel call, so a rebuild over an older OfficeGameplay leaves nothing behind. Without this, Build Office UI is not idempotent for removals.
5. **MicroLens and the microprint read path stay without a caller.** FIX_PLAN_v2 65c leaves "MicroLens arrays" with TOOLS, and #17 brings the microprint back through the scanner. This is a listed exception to "no dead code", and it is temporary.
6. **ReprintedPaper is switched off in code** (`LieKinds.IsDormant`), not by deleting it from 7 days' `lies` lists. The data stays authored and the switch is one line to undo.
7. **"Speak up" is cut with "Step closer".** Saleh named only "step closer" as a dialogue that does nothing (D5); cutting "Speak up" is R7, which DECISIONS_1019a.md lists as not asked yet. We cut it because it is the same kind of dead option (FIX_PLAN_v2 P0-C item 5). If Saleh says keep it, restore its `interview.requests` row and its `voices.spoken` rows from f347060's `world_source.json`; the mechanism is untouched.
