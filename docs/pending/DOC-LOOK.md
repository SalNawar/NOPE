# DOC-LOOK (Phase A #9): readable papers that look and open like paper

Branch `fix/a-doc-look`, cut from `origin/main` 9b8ab5f (P0: CARVE + CUTS + QA-RIG). Code only: nothing here was run in Unity. Everything that needs Unity is a recipe under **RESULT** below.

Sources: FIX_PLAN §3 DOC-LOOK, FIX_PLAN_v2 §3 "DOC-LOOK: delta" (D3 and D15 decided; v2 wins), BUGLIST, DECISIONS_1019a (D3, D15, "keep a big mat size", DESK APPROVED), GAME_FEEL_BIBLE rows A1-A6 and A9, QA-RIG §4 and §8.

`docs/FEATURES.md` is **not** edited here. INTEGRATION folds this file in. The lines to rewrite are listed under "FEATURES (for INTEGRATION)".

## Ownership

Every file is DOC-LOOK's by FIX_PLAN §3 "Owns", CARVE.md (`.DeskPaper.cs`, `DeskDocument.Render.cs`, `DeskRulebook.cs`) or QA-RIG §2 (`Checks/DocLookChecks.cs`, `Tests/PlayMode/DocLook/**`, `tools/qa/scenarios/doclook_*.json`). Three notes for the verifier:

| File | Why it is here |
|---|---|
| `Assets/Editor/OfficeArtImporter.cs` (new) | Named in FIX_PLAN §3 DOC-LOOK "Owns" ("a new `Assets/Editor/OfficeArtImporter.cs`") and in §0 rule 2 as the only writer of `Assets/Art/Office/**` import settings. |
| `Assets/Scripts/Visuals/FormArt.cs` | It is not on any track's list. It is the serialized art-form type that the DocTemplate assets (DOC-LOOK's) hold, read only by `ArtLayout` (DOC-LOOK's). Three fields are added (`printLabels`, `labelGrowth`, `valueGrowth`), and `Worded` changes. |
| `tools/qa/so_reader_allowlist.txt` (one line) | That list is kept by INTEGRATION. Cards are now rigid by rule (`PaperBend.FlexOf`, D3), so `MotionKnobs.cardFlex` has no reader left and `qa.so.readers` would refuse the merge. The line names BUG-11b / D3 and its owner, DOC-HANDLE, who deletes the knob with its 11a work; the line goes with it. **INTEGRATION: please confirm or move the line.** |

Not committed: `NOPE-feat-clock.sln` (IDE churn).

## Changes by BUG id

### BUG-9: text too small; the fit shrank the important values (note 25)
- **Bigger text on every art document.** Each of the 8 art templates (`DocTemplate_TC101/230/240/310/520/610/620/630.asset`) now has:
  - `printLabels: 1`: the paper always shows its clean face and prints every label and caption, in the art's ink, in English or the culture's language. Before, the small baked labels showed whenever English was read.
  - `labelGrowth: 1.3` and `valueGrowth: 1.5`, sized for the big mat, where papers show at about 67% and text must grow about 1.5x (DECISIONS "keep a big mat size").
- **Cells re-laid** (`ArtLayout.Place`, `Cell`, `Stacked`, `Grown`, `LabelLine`, `ValueFill`):
  - A label that stands above its value becomes a band at the top of the cell, and the value takes the rest below it. The field is picked where its value prints.
  - A label beside its value grows inside its own place.
  - A value never grows taller than one line of its place (`ValueFill` 0.74).
- **Wrap or cut, never break a word** (`TextFit`, `TmpFormText.Unbreak`, called from `DeskDocument.Print` and from the rulebook's rows and guide body):
  - A text whose widest word is too wide shrinks just enough for it (never under `WordLeast` 0.6).
  - A word still too wide is cut and ends with a full stop ("KONSTANTI."), so no word ever breaks across two lines.
  - A word that holds a rich-text tag is never cut (the guide's sheets).
  - The rulebook's rows are only cut, never shrunk, because `LayRows` sets their size again afterwards.
- **The 0.85 floor.**
  - `TextFit.Floor` = 0.85 is the legibility floor. A text fitted below it counts as *shrunk* (`TextFit.Shrunk`), and the cap-height gate reports it.
  - **Deviation:** the absolute least size stays the old one (`TextFit.Least` 0.5 of the *art's own* size; `ArtLayout.FitFloorOf` gives 0.5 / growth of a grown size). The longest generated values (the passport's 43-character origins) need that room to fit, and the validator measures them at it. A grown text therefore never prints smaller than before, and the gate flags any text that is under the floor.
- **The rulebook paginates before it shrinks** (`DeskRulebook.RowTextFloor` 0.6 → 1). Only a rule alone on its sheet shrinks: it stops at `TextFit.Floor` first and goes down to `RowTextLeast` only as a last resort.
- **The validator measures in the art's value face.** `ContentLibraryValidator.Forms` measures an art form in `FormStyleSO.artValueFont`, not LiberationSans, so a value that passes the check also fits on the paper.
- **The cap-height gate.**
  - `CapHeight` (pure): pixels of cap height from the size, the page, the face's cap per em and the view scale. The floors are 10/7 px for data and 6 px for labels at A2.
  - `DocLookChecks.doclook.forms.capModel` (demo): modelled at the big mat's reading view.
  - `doclook.play.capHeight` (demo): measured on screen by the rig's `capHeight` op (`DocLookOps`, scenario `doclook_cap.json`).
  - `doclook.forms.regrown` (merge): every clean-faced art form prints its labels, and its smallest data grew ≥1.15x.

### BUG-10a: the hover hull shattered (D15 decided: papers lift, props get an outline; notes 3, 8, 9)
- **Papers, the folder and its tabs lift.** They implement `IHoverLift` (`DeskDocument`, `DeskRulebook`). `HoverHighlighter` finds the `IHoverLift` on or above the hovered Clickable and calls `HoverLift(InteractionFeedbackSO.paperHoverLift` 0.3). `DeskPaperDepth` eases the contact shadow 30% of the way to "held" in 90 ms (the Balanced spring), so the paper rises Papers, Please-style with the hand cursor and gets no outline.
- **Props get a fixed-pixel rim, on the office camera only.** `HoverHull.shader` is rewritten:
  - It draws a stencil mask of the prop's meshes, then the same meshes pushed out in screen space by `outlinePixels` (2) from the mesh centre, where the mask is not set.
  - Split normals cannot tear it apart.
  - It is the same width at every distance.
  - `Graphics.RenderMesh` draws it with `RenderParams.camera` = the camera of the pointer's raycast.
- **The v1 hull fallback is dropped (v2).** `worldOutlineWidth` became `outlinePixels`, and `InteractionFeedback_Default.asset` is updated.
- **No hover while anything is held** (`HoldRegistry.Desk.IsHolding`): no lift or outline flashes under a carried paper.

### BUG-10b: the folder's left-edge hole-colour strip
`DocumentCel.shader`: where the model's edge overhangs the art's clear margin, the shader takes the art's nearest opaque colour `_HoleReach` (0.012 uv) toward the centre. `_HoleColor` is used only when no art lies near.

### BUG-10c: rulebook text through the ruled lines; the floating NEW tag
- `RuledLines.Spacing`: a wrapped rule's lines step down whole rules (TMP `lineSpacing` from the rule pitch, the font's line height and the size). `RuledLines.Snap`: a row grows by whole rules.
- The GUIDE tab's NEW badge is a sticker on the tab's right end, on the board (`.DeskPaper.cs`).

### BUG-11b: look, open and close (D3 decided)
- **The passport arrives shut and opens.**
  - `PaperCover` (new) is a hinge on the booklet's spine. It carries the cover front (the nation's colour with its emblem in gold) and, back to back, the visa page's half of the face art.
  - `BookletFlip` (pure, in `PassportCovers.cs`) drives it: GAME_FEEL A1, a 15° lift in 40 ms, then a 160 ms OutBack swing to 180°, the same way back; under Reduced Motion it jumps.
  - It opens when the passport is picked up or lies on the desk, and shuts on the counter (the hand-back).
  - While it is shut: the visa page is cut from the model (`DocumentCel._ClipAbove`, `DeskPaperDepth.ClipAbove`), the prints above the spine are hidden, and the contact shadow lies under the data page only (`DeskDocument.ShowShut`).
  - The builder adds `Sheet/Cover/Hinge/{Front, Emblem, Back}` to the paper template (`BuildPassportCover`).
- **Cards are rigid** (`PaperBend.FlexOf`: a card gets 0 whatever the knobs say). They keep their contact shadow.
- **"The folder never bends" is retired.** A page turn flicks the folder's loose sheets (`DeskPaperDepth.Flick`: a 3 mm bow and 1.5 mm twist that ease flat over 0.35 s, none under Reduced Motion; GAME_FEEL A3).
- **Not done here: the folder opening to its tabbed sheet.** The folder still lies open. Its fold and pocket belong to DESK-HOMES (B3), which leases `DeskRulebook.cs` after this merge (LEASES §2). The open and close belong with that pocket, and the item is listed as open below.

### BUG-12a: print detached from the bending face
- **Print and face are one surface.**
  - The Fills and Lines meshes bend vertex by vertex with the face (`BendPrintMeshes`, using rest and bent vertex buffers kept between frames: no allocation per bend).
  - Every other printed part rides the bent face and is lifted by how far the curl rises past its flat piece (`PaperBend.Bulge`), so no rule line vanishes under the landing curl.
- **The corner relief** (`PaperDepth.SheetRelief`/`ReliefOver`, matching `docs_models.py` sheet(): `lift_max` 0.7 mm in the bottom-right 28%, and a 0.2 mm edge curl): every print part and every fill or line vertex rests above it (`RestFlatPrints`).
- **One material in every zone.** The paper template's face now wears the cel material on the counter too (the URP Lit `Paper` material is gone), so the zone swap is a no-op.
- **Stack reorder is instant:** the rank shadow (BUG-40c) needs no spring. The 1.5 mm `SetLift` spring is DOC-HANDLE's.

### BUG-12b: a stamp right after a drop jumped to the page centre
`DeskDocument.Stamp` puts every part back at rest and flattens the bend before it places the mark, so the next bend takes fresh rest poses and the stale `_printRest` from before the drop no longer wins. The mark also clears the corner relief.

### BUG-12c: the folder's tabs floated 3 mm
The tab plates sit on the folder's back board (`RulebookTabPlateLift` = `RulebookBoardTop` −3.2 mm + the 0.6 mm slab, from `docs_models.py` folder(): `board_top`). The tab words go down with them. The click boxes stay at the folder's plane.

### BUG-14c: the photo vs the traveller tint (the include only)
`DocumentCel.shader` includes `Assets/Shaders/Include/DeskLight.hlsl` and multiplies by `DeskLight()`, as the props, the traveller and the photo do. The match itself is measured at LIGHT's merge (P0 stub = 1).

### BUG-29: the importer half (dog fur speckle)
`ArtSlots.Mipmapped`/`Trilinear`: the pet's pictures and frames get mipmaps and trilinear filtering (still uncompressed). `ArtSlotImporter.GetVersion` goes to 2, so they reimport once.

### BUG-37c: per-line RTL
`TmpFormText.Unbreak` breaks a logical Arabic text into lines on its logical text (`TextFit.WrapWords`), puts each line in visual order with `ArabicShaper.ToVisualLines` (the P0 per-line API), and sets the text from the right. Desk papers and rulebook rows and guide sheets all go through it.

### BUG-40c: contact shadow by stack rank
`DeskPaperDepth` subscribes to `DeskEvents.StackChanged`. The shadow's darkness × `PaperDepth.RankStrength(rank)` (1 at the bottom, +0.25 per place up, at most 1.75). **Inert until DOC-HANDLE (#8) raises the event** (P0 declares it, and nothing raises it yet).

### BUG-63: office textures with no mipmaps; paper art bilinear DXT5
- `OfficeArtImporter` (new AssetPostprocessor, version 1) gives every texture under `Assets/Art/Office/` mipmaps, trilinear filtering and CompressedHQ (BC7 on PC).
  - It returns early for `Assets/Art/Office/AnimeHallLayers/**` (`ArtSlots.OfficeImports`).
  - It leaves normal maps, lightmaps, shadowmasks and cursors alone.
  - **Deviation:** it keeps each texture's type (a Sprite stays a Sprite) instead of forcing Default, because switching the type would orphan every sprite reference.
  - Other tracks ask for rule rows. None have been asked for yet.
- The papers' slot art (`Forms/`) gets trilinear and CompressedHQ (`ArtSlotImporter`).
- Gate: `doclook.import.office`.

### BUG-65g: form text and depth setup cost
`DeskPaperDepth.ShowShadow` no longer allocates two lists through `ConvertAll` on every shadow update during a lift or hover. It reuses its vertex and colour lists. The renderer lookup is cached once (`Flexed()`). **Not reduced: `TmpFormText`'s per-paper setup.** `Unbreak` adds a few `GetPreferredValues` calls per printed text. Pooling and pre-building are DOC-HANDLE's; measure on its call-next recipe.

## Tests (offline: compile 0 errors; runner `passed 3735, failed 0`)

| Test | Covers | Fails without the fix because |
|---|---|---|
| `DocLookTests.WordShare_*`, `Unbroken_*` (now also: a tagged word is never cut), `WrapWords_*`, `IsLogicalArabic_*`, `Shrunk_*` | BUG-9, 37c: no mid-word break, 0.85 floor | `TextFit` is new (compile error); the tag assert fails on the cut-everything version |
| `DocLookTests.RuledLines_*` | BUG-10c | new |
| `DocLookTests.CapHeight_Pixels_*` | BUG-9 gate maths | new |
| `DocLookTests.BookletFlip_*` (4) | D3: shut → open → shut state machine, lift/overshoot, Reduced Motion, turning back mid-swing | new |
| `DocLookTests.FlexOf_ACardNeverBends_*` | D3 card rigidity (v2's added test) | before, a card took `cardFlex` 0.12 |
| `DocLookTests.Bulge_*` | BUG-12a: a lifted part is over the face everywhere under it | new |
| `DocLookTests.SheetRelief_*`, `RankStrength_*` | BUG-12a corner, BUG-40c | new |
| `FormLayoutTests.PrintLabels_*`, `Growth_*` (4), `FitFloorOf_*`, `CapHeight_Smallest_*` | BUG-9 re-layout | before, labels printed only when translated and sizes did not grow |
| `ArtSlotsTests.Mipmapped_AndTrilinear_*`, `OfficeImports_*` | BUG-29, 63; the AnimeHallLayers early return | new |

Filtered runs: DocLook 16/16, FormLayoutTests 75/75, ArtSlotsTests 40/40. `python -m unittest discover -s tools/qa -p "test_*.py"`: 35 OK.

## Open (not in this commit)
1. **The folder opens and closes** (D3). It goes with DESK-HOMES' pocket (B3), which leases `DeskRulebook.cs` next.
2. **The hover IoU op** (QA-RIG §8 "hover IoU ≥0.9, its op"). No ID-mask op is written. The hover is judged from the `doclook_hover` sequences by eye (recipe R7). Papers no longer get an outline (D15), so the IoU applies to the props' rim only.
3. **The line-mask diff** for "0 frames where a rule line disappears". The rig has no line-mask tool, so recipe R6 uses `flicker --static` plus a frame sheet.
4. **Labels re-baked into the face art.** Not needed: the clean faces plus `printLabels` print every label in TMP in the art's ink (the plan's fallback, made the default).
5. **`docs_models.py` cover meshes.** The cover is quads on a hinge. No model re-export.
6. **TmpFormText setup cost** (BUG-65g, see above).

## FEATURES (for INTEGRATION)
Line numbers as of 9b8ab5f:
- **16 (pet pictures):** "imported uncompressed and bilinear without mipmaps" → "imported uncompressed, with mipmaps and trilinear (`ArtSlots.Mipmapped`/`Trilinear`, BUG-29; tested: `ArtSlotsTests`)".
- **42 (paper bend):** "a card `cardFlex` 0.12" → "a card never bends (`PaperBend.FlexOf`, Saleh's D3; tested: `DocLookTests.FlexOf_ACardNeverBends_WhateverTheKnobs`)". "and never bends" (the folder) → "and a page turn flicks its loose sheets (`DeskPaperDepth.Flick`)". Add: "the printed fills and lines bend with the face vertex by vertex, every other printed part rides it lifted clear of the curl and the sheet's lifted corner (BUG-12a; tested: `DocLookTests.Bulge_*`, `SheetRelief_*`); a passport arrives shut and its cover swings open when picked up or laid on the desk, shutting on the counter (`PaperCover`, `BookletFlip`; tested: `DocLookTests.BookletFlip_*`); the contact shadow darkens with stack rank (`PaperDepth.RankStrength`, once DOC-HANDLE raises `DeskEvents.StackChanged`)".
- **63 (NEW badge):** "A red NEW badge over the rulebook's GUIDE tab" → "A red NEW sticker on the rulebook's GUIDE tab".
- **194 (hover):** rewrite: "Hover: a paper, the rulebook folder and its tabs lift (Papers, Please-style; `IHoverLift`, `InteractionFeedbackSO.paperHoverLift`) with the hand cursor; a prop gets a 2 px rim round its silhouette on the office camera only (`HoverHull` stencil mask + screen-space push, `outlinePixels`); nothing hovers while something is held (Saleh's D15, BUG-10a); the traveller's hit zone gets the hand cursor only; desktop UI as before".
- **New line (document text):** "Documents drawn on their art print every label over the clean face in the art's ink and grow their text for the big mat (`FormArt.printLabels`, `labelGrowth` 1.3, `valueGrowth` 1.5; a label over its value is re-laid as a band at the cell's top); no word ever breaks across lines (a too-wide word is cut with a full stop) and an Arabic text is laid line by line (`TmpFormText.Unbreak`, `TextFit`); the rulebook paginates before it shrinks and its lines sit on the ruled lines (`RuledLines`); the cap-height gate (`DocLookChecks`, `doclook_cap`) holds data at ≥10 px cap at 1080p and ≥7 px at 720p (tested: `DocLookTests`, `FormLayoutTests.Growth_*`)".
- **New line (import):** "Every texture under `Assets/Art/Office/` (never `AnimeHallLayers/`) imports with mipmaps, trilinear and BC7 (`OfficeArtImporter`; tested: `ArtSlotsTests.OfficeImports_*`; gate `doclook.import.office`)".

## RESULT

**Offline:** compile_check 0 errors (Assembly-CSharp-Editor, TimeDeskEditMode), runner `passed 3735, failed 0`, QA python tests OK.

**deferred: Unity.** Run these in the NOPE-feat-clock editor through its job runner, in the order given. R1-R4 are this merge's gate. The pixel pass lines (R5 and on) are measured at the final paper size and framing, so they count after DESK-PLAN (#5), CAM (#6) and DOC-HANDLE (#8) have merged (FIX_PLAN merge order: DOC-LOOK is #9).

- **R1 Import and EditMode.**
  - Steps: `__EMPTY_SCENE__`, then refresh. Wait until the heartbeat says idle (the importer version bumps reimport `Assets/Art/Office/**` and the Forms and pet slots once).
  - Run the EditMode suite.
  - **Pass:** 0 compile errors. Every test passes except the known `UnitySkills ... SceneSummarize_CountsObjectsCorrectly`. `QaEditorGateTests.EveryScenario_Reads_WithKnownOps` passes with `doclook_cap`, `doclook_print` and `doclook_hover` (the `capHeight` op is registered).
- **R2 Rebuild OfficeGameplay.**
  - Steps: `Tools > TimeDesk > Build Office UI`. Compare the semantic dump with main's.
  - **Pass:** the only differences are these:
    - `PaperTemplate/Sheet/Cover/Hinge/{Front, Emblem, Back}` is new and inactive;
    - `PaperTemplate/Sheet/Paper` wears `Paper_ExamineCel`;
    - the Rulebook's `TabPlate1-4` and `Tab1-4/Text` sit lower (plate y −0.0026, on the board);
    - `GuideBadge` moves onto the GUIDE tab.
  - The art scene `OfficeScene.unity` is byte-unchanged. Commit `OfficeGameplay.unity`.
- **R3 Importer scope** (FIX_PLAN "Importer scope").
  - Steps: after R1, run `git status --porcelain -- Assets/Art/Office/AnimeHallLayers`, then `git status -- Assets/Art/Office`.
  - **Pass:** the first command shows 0 lines. `ArtSlotsTests.OfficeImports_TheOfficesArt_NeverTheHallLayers` passes.
  - Commit the `.meta` changes under `Assets/Art/Office/**` (outside AnimeHallLayers) that the reimport wrote.
- **R4 Gate.**
  - Steps: `QaJobs.Gate`.
  - **Pass:** `doclook.forms.regrown` is ok (each form is ≥x1.15; the report line lists them all), and `doclook.import.office` is ok ("N checked, 0 off, M under the hall layers left alone"). There is no new red, and `qa.so.readers` is ok with the `cardFlex` allowlist line.
  - Then run `QaJobs.GateDemo` and read the numbers in `doclook.forms.capModel` (modelled px per form at A1/A2).
- **R5 Cap height** (BUG-9, note 25).
  - Steps: write `doclook_cap` to `Library/QA/job_scenario.txt` and run `QaJobs.Scenario`.
  - **Pass:**
    - `doclook_cap.capHeight.a1` ok with data ≥10 px;
    - `doclook_cap.capHeight.a2` ok with data ≥7 px and labels ≥6 px;
    - `console` 0 errors, `presses` ok.
  - Stills: `stills/doclook_a1_reading_1920x1080.png` and `doclook_a2_reading_1280x720.png`.
  - Repeat on days 5 and 12 (add `{ "op": "jumpDay", "n": 5 }` after `waitOffice`), and in `el` and an Arabic culture once TEXT-LANG has merged.
  - The FIX_PLAN gate is 100% of data ≥10/7 px, labels ≥6 px at A2, 0 mid-word breaks and 0 overflow over forms × roles × cultures × days 1-15. capModel covers every form statically, and this op covers the papers on screen.
- **R6 Human read.** Hand `doclook_a2_reading_1280x720.png` to the adversarial reviewer at 100% with no zoom. **Pass:** every field of the day-1 passport is read aloud and quoted back correctly.
- **R7 Print region, folder glitches, open and close** (BUG-10b, 11b, 12a, 12c).
  - Steps: `QaJobs.Scenario` with `doclook_print`. Then for each of `seq/print_a1` and `seq/print_a2` run:
    - `python tools/qa/seqtools.py flicker <seq> --static`
    - `python tools/qa/seqtools.py sheet <seq> <out>.png`
    - `python tools/qa/seqtools.py gif <seq> <out>.gif`
  - **Pass:**
    - flicker PASS (0 px toggling more than 2x in 10 s while nothing tracked moves);
    - on the frame sheet: 0 frames where a rule line or box vanishes under the curl, 0 frames with a strip on the folder's left edge, and the tabs and their words lie on the board;
    - the passport arrives shut on the counter, and its cover lifts and swings open with a small overshoot as it reaches the mat;
    - the reviewer, comparing against a Papers, Please clip with `seqtools side`, answers "does it read as paper?" yes.
- **R8 Hover** (BUG-10a, D15).
  - Steps: `QaJobs.Scenario` with `doclook_hover`. Make GIFs and sheets of `hover_a1`, `hover_a2` and `hover_held`.
  - **Pass:**
    - papers and the folder lift with the hand cursor and show no outline;
    - every prop's rim is an unbroken band of about 2 px that hugs its silhouette at A1 and A2;
    - no rim shows in a close-up or on any camera other than the office's;
    - 0 rim or lift frames in `hover_held` while the paper is carried.
- **R9 Stamp right after a drop** (BUG-12b, STAMP's aiming recipe at #7).
  - Steps: in STAMP's dater scenario, put the passport down with a `drag`, then within 15 frames (0.5 s) `drag` a dater onto an aimed point on it. Do this 10 times.
  - **Pass:** 10/10 impressions within 4 px of the aimed point with their tilt kept, and 0 z-fighting frames in the recorded sequence.
- **R10 Mipmap crawl [BP]** (BUG-63).
  - Steps: `QaJobs.BuildQaPlayer`, then `run_player.py` on `doclook_print.json`, then `seqtools flicker bp...print_a1 --static`.
  - **Pass:** no crawl on the paper and folder edges (≤ the A-B-A threshold). Run only when Saleh is away from the PC.
- **R11 Contact shadow** (BUG-40c), after DOC-HANDLE raises `StackChanged`.
  - Steps: DOC-HANDLE's stack-order stills (3 papers in 10 random orders at A2).
  - **Pass:** the reviewer names the top paper correctly 10/10.
- **R12 Photo include** (BUG-14c). In the frame debugger, DocumentCel binds `_DeskLightTint` and `_DeskLightLevel`. The match itself is measured at LIGHT's merge.
- **R13 Notes re-check.** Saleh's notes 25 (tiny font), 7, 8, 9 and 3 go in NOTES_RECHECK with R5-R8 evidence.
