# Phase A TOOLS: a torch that shows what it reveals, a readable microprint, and a translation engine that touches only what you see

Branch `fix/a-tools`, cut from main `9b8ab5f` (P0: CARVE + CUTS + QA-RIG). Sources: FIX_PLAN.md §3 "TOOLS" with FIX_PLAN_v2.md §3 "TOOLS: delta" (v2 wins), BUGLIST.md (BUG-2, 4, 52, 55, 65, 73), DECISIONS_1019a.md, GAME_FEEL_BIBLE.md rows C1 (TOOLS' part: the microprint) and C3 (the UV torch).

**Saleh's calls this track builds (quoted, DECISIONS_1019a.md):**
- **Magnifier (D5):** he is "not sold". "The magnifier is folded into the scanner: the scanner's hidden-layer view reveals the microprint of the ReprintedPaper lie. The physical magnifiers are cut." (cut by P0-C; this track supplies the microprint content and the read rule).
- **Language (D7):** "maybe the pc will come again in the future but for now the in-game explanation is that bureau equipment is immune to low level time manipulation changes" and "you should see the glasses on the table ... from there you can see the one word being translated when the mouse hovers on it". The PC never shows foreign text, so the lens never reads the PC; the hover engine here is what GLASSES (Phase B) reuses.
- **UV torch (2026-10-18, standing):** "when you hold the UV the mouse should be on the base not the light" (kept: the pointer holds the grip).
- **Everything diegetic / D2:** "everything should be animated and satisfying responsive and gives good feedback".

Everything else that is a design choice is marked **proposal (ours)** below.

**Not done here (not TOOLS' files):** UI-PC draws the microprint in the scan view at #13 (`ScanLayerSwitch`); CONTENT re-enables ReprintedPaper at #17 (`LieKinds.IsDormant`); DOC-HANDLE wires `HoldRegistry.Cancel`/`OnFocusLost` into the control model (Esc, right-click, Alt+Tab); CAM blocks view changes while something is held (55c).

---

## 1. Changes per BUG id

### BUG-2b: microprint read in the scan view, credited only when it rendered legibly (FIX_PLAN_v2: TOOLS content and rule, UI-PC render at #13)
- **`Domain/ScanLayers.cs`: new `ScanMicroprint`.** What the scan view's hidden layer prints for a paper's microprint line, and at what size:
  - `Text(MicroLine, form)`: one repeat of the line the paper prints (`MicroLine.Printed`), "TCB · TC-101 · 4471-208", no trailing separator. The Citizen ID is what the player holds against the ID box.
  - `MinCapPx = 12` (FIX_PLAN: "≥12 px cap at A2"); `Headroom = 1.25` (**proposal**: the view prints at 15 px so rounding, a font's own cap line or a slightly smaller window never drop it under 12); `MaxFontSize = 96` (a guard for an unmeasured view).
  - `FontSize(capShare, pixelsPerUnit)` and `CapPx(...)`: the size at which capitals reach 12 × 1.25 px wherever the text is drawn (the PC window at 1280×720, a scaled window), from the font's cap share and the screen pixels per unit measured as drawn.
- **`Domain/MicroLens.cs`, rewritten.** `MicroLens.Readable(layerOpen, renderedCapPx)`: a read counts only when the player has the paper's hidden layer open **and** the line rendered with capitals ≥12 px, measured from the drawn glyphs. The geometric lens maths (`Shown`, `Held`, `ReadLength`, the old `Readable`) is gone with the magnifiers. 1019a credited a line drawn 0.024 px tall because a lens shape covered it.
- **`Office/Desk/MicroprintView.cs` (new, TOOLS).** The seam UI-PC's scan view calls at #13:
  - `Print(TMP_Text, MicroLine, form)` writes the line on one line at `ScanMicroprint.FontSize`, auto-size off, measuring where the text is drawn (its canvas camera).
  - `CapPx(TMP_Text)` measures the capitals as last drawn: the tallest capital or digit glyph of the laid-out text, carried to the screen through its transform and camera; 0 when the text, its canvas or its camera is off.
  - UI-PC's call at #13: `if (MicroLens.Readable(layer == ScanLayer.Uv, MicroprintView.CapPx(row))) readings.ReadMicro(document);` on the frame after `Print`.
- **`Office/Desk/MicroPaperLayer.cs`.** The desk line still prints as a hairline (too fine to read at the desk, by design). When the scan view credits the read (`DeskReadings.Changed`), the desk line answers: its tick pops (inspect_link) and the line becomes clickable in inspect mode, as before. `Track` is now an empty shim; see the cleanup list.

### BUG-2c: the torch lights the paper and its body never covers the patch; the pool only on the desk
- **`Domain/UvTorchHold.cs` (new, TOOLS), pure.**
  - `Unoccluded(eye, tail, lens, bodyRadius, centre, radius)`: the share of the lit patch the camera sees past the torch's body. The body is a capsule from tail cap to lens; the patch is 37 samples; a sample is hidden when the line from the eye to it passes through the body.
  - `Hold(eye, grip, deskY, height, away, preferredTilt, body…, coneRadius)`: of the aims (a tilt within `UvCone`'s 10-80°, a turn of at most ±60° off straight ahead), the nearest to the built tilt straight ahead (a degree of turn costs half a degree of tilt) whose patch the eye sees past the body (`TargetShare` 0.97). When none qualifies, it picks the one that shows the most.
  - The 1019a hold (40°, straight away) put the lens end over the patch from the reading view's high camera; the test proves it (`TheBuiltHold_CoversThePatch_FromTheReadingView`). Over eight eyes from both views, the hold keeps the patch ≥95 % visible and within 0.35 m of the hand.
  - `PoolShows`: the pool and the reveal show only when the cone's centre is on the desk area.
  - `Follow`: the grip eases after the pointer and never trails it by more than 8 mm (C3).
  - `Fluoresce`: the reveal rises over 120 ms (C3); at once under Reduced Motion.
  - **Proposals (ours):** `TargetShare` 0.97 (one sample over the 0.95 pass line), `MaxYaw` 60°, `SwingSeconds` 0.08 (the torch swings to a new aim), `ResolveDistance` 5 mm (the hold is re-solved only after the grip or the eye moves that far; an early-exit search keeps it cheap). These live as constants because `DeskConfigSO` is DOC-HANDLE's; INTEGRATION may move them into a TOOLS SO later.
- **`Office/Desk/UvTorch.cs`.**
  - The cone lands on the **desk plane** (`DeskSurface`). Before, it landed at the cradle's height (`transform.position.y`), which is the shelf, so the oval floated.
  - The hold plane is `uvHoldHeight` over the desk.
  - The aim comes from `UvTorchHold.Hold` for the current camera; the torch measures its own body from its meshes at rest, with BeamFX left out.
  - The pool is drawn, and the reveal lit (`_UvConeOn` > 0), only while the cone's centre is on the desk area (`DeskSurface.Clamp`). The pool lies at the lit spot.
  - `_UvConeOn` is now 0..1, rising over 120 ms (the marks fluoresce in).
  - The lit spot trails the aim by about two frames (the cone's soft edge lag, C3).

### BUG-4b: the torch's click box hugs the torch
- **`Editor/OfficeSceneUIBuilder.Tools.cs`.** `TorchBounds` takes the bounds of the torch's own meshes, leaving out the 32 cm BeamFX cone, with a 5 mm margin (**proposal**). 1019a's `LocalBounds` took in BeamFX: the box was 2.6× the torch and swallowed clicks on the chip reader. The builder also sets the switch travel (below).
- **Gate:** `tools.torch.clickBox` (static) measures the built OfficeGameplay. It is red until INTEGRATION rebuilds OfficeGameplay with this builder, then green.

### BUG-52: the hover-translation engine (the one GLASSES reuses)
- **`Domain/TranslationLens.cs`:**
  - `Candidate(onPc, shown, drawn, alpha)`: the lens touches only text the player can see. It is never the PC's (D7; FIX_PLAN_v2: "excludes the PC entirely"), never a text on a canvas no camera draws (1019a read the closed PC's desktop through its disabled frame camera), and never a faded one.
  - `RevertSeconds = 0.2` and `RevertSpeed(duration)`: a flip back always lands within 0.2 s (the plan's pass line; a constant here because `TranslationLensSettings` is TEXT-LANG's).
- **`UI/Translation/TranslationLensPresenter.cs`:**
  - **No full scan.** Only the texts of the object under the pointer are looked at: its window or top panel, else the desk object its physics hit belongs to. The 5 s rescan of every TMP text in the scenes is gone; the label order is refreshed when the strings or the scene change, and texts that are gone are pruned.
  - **Every candidate passes `Candidate`.** The PC's texts are excluded by `UiText.SurfaceOf` (the PCDesktop layer). A UI text counts only when its canvas is enabled and is an overlay or is drawn by an enabled camera, and its inherited alpha is above 0; a desk word only when its renderer and the hit's camera are on.
  - **Never stale.** While anything is translated, the hover is re-checked every frame, even with the pointer still, so a word whose object moves away flips back (CODE-163). With nothing translated and the pointer still, nothing runs and nothing is allocated.
  - **Within a frame.** The hover runs before the animation, so the hovered word changes on the same frame (C4: "within 1 frame").
  - **Reverts within 0.2 s.** A flip back plays at `RevertSpeed`, and a reverse that starts mid-flip keeps its place.
  - **Profiler marker** `TranslationLens.Hover`, for GLASSES' ≤0.5 ms per move budget.
  - **QA seam** `Translated(list, reverting)`: the texts turned towards English, or with `reverting` also those still flipping back.
- **`Visuals/LensText.cs`: bidi once per flip.**
  - A right-to-left label's visual order is worked out once, when the phrase is built.
  - `Compose` draws each replaced word in its own place (shaped and ordered on its own), so the rest of the line never reshuffles as letters flip. Before, `ArabicShaper.ToVisual` ran on the whole mixed line every step.
  - New `WholeFrame` (levels 2 and 3):
    - Left to right, the label flips as one text, as before.
    - Right to left, it flips word by word in place on the same clock, and reads as its English sentence only once landed: one reordering per flip.
  - The presenter draws whole flips through `WholeFrame`.
- **Auto-size once per flip.**
  - The lens already held a label's size while translated (`Activate`, `resizeSeconds`).
  - `UI/Translation/TextFlip.cs` (the speech bubble's flip) now fits the line's size once, as it is shown, and holds it with auto-size off through the flip and its settle. It used to re-fit at every letter, which stepped the line's size (CODE-78). The text's own sizing comes back with the next line or the release.
  - **Not here:** in a right-to-left speech flip the per-step visual order is `DisplayText`'s (TEXT-LANG's file). Its reorder at each letter is left for TEXT-LANG's per-line RTL work (the seam request is in §5).

### BUG-55b: the torch is a hold
- `UvTorch` is an `IHoldable`:
  - `HoldRegistry.Desk.Begin` on the press;
  - `End` on the release, the case's end or a disable;
  - `CancelHold` sends it back as a release does.
- The held grip follows the pointer **every frame** (`Pointer.current` through the press camera), not only on pointer events, so it stays on the cursor while the camera blends.
- DOC-HANDLE's control model (Esc, right-click, focus loss → `HoldRegistry.Cancel`) and CAM (no view change while holding) consume it.

### BUG-65c: MicroLens allocations
- `MicroLens.Shown`, which allocated two arrays per call, is deleted with the lens geometry. `MicroLens` now allocates nothing. The `DeskMagnifier` part was cut by P0-C.

### BUG-73f: UvRevealText.shader comments
- The header now says what the shader does:
  - its colour is `_FaceColor` alone, and the vertex colour only fades it (alpha; its RGB is unused);
  - `_UvConeOn` is 0..1, fluorescing in;
  - it draws the ghost of a washed value. The "seal's microprint" was wrong: the seal's ring is a `UvReveal` quad.

### GAME_FEEL_BIBLE C3 (UV torch) as built

| Cell | Built |
|---|---|
| IR | The lift and the switch-on happen on the press frame; the grip is under the cursor |
| SS | The thumb switch slides toward the lens and clicks 1 mm down. The builder sets `switchTravel` (0, -1 mm, -10 mm); the default matches |
| WP | Short lag ≤8 mm behind the cursor (`UvTorchHold.Follow`) |
| AN | Tips 10° up out of its cradle on hover (hover feel spring; never while held) |
| SF | The revealed marks fluoresce in over 120 ms |
| TR | The lit spot trails about 2 frames |
| AU | uv_on, uv_off, uv_hum, as before |
| AC | The beam lights the paper and the body never covers the patch |
| PE | **Not built.** Violet motes (6-10, 600 ms) need the particle system the bible's §6 gap 1 gives to FEEL-KIT (Phase B), or to FEEDBACK's emitter. Filed there |

C1 (scanner) is TOOLS' only for the microprint (≥12 px cap: content and rule here, render at #13). The lid, scan bar and READY lamp are not TOOLS' files.

---

## 2. Files (all TOOLS')

- **Changed:**
  - `Assets/Scripts/Domain/MicroLens.cs`, `ScanLayers.cs`, `TranslationLens.cs`;
  - `Assets/Scripts/Visuals/LensText.cs`;
  - `Assets/Scripts/Office/Desk/UvTorch.cs`, `MicroPaperLayer.cs`;
  - `Assets/Scripts/UI/Translation/TranslationLensPresenter.cs`, `TextFlip.cs`;
  - `Assets/Editor/OfficeSceneUIBuilder.Tools.cs`;
  - `Assets/Art/Office/Gameplay/Shaders/UvRevealText.shader` (comments only);
  - tests `MicroLensTests.cs`, `TranslationLensTests.cs`, `LensTextTests.cs`.
- **New:**
  - `Assets/Scripts/Domain/UvTorchHold.cs`;
  - `Assets/Scripts/Office/Desk/MicroprintView.cs`;
  - `Assets/Tests/EditMode/UvTorchHoldTests.cs`;
  - `Assets/Editor/QA/Checks/ToolsChecks.cs`;
  - `Assets/Tests/PlayMode/Tools/Rig/ToolsOps.cs` (the QA ops `toolsTorch`, `toolsClicks`, `toolsLens`, `toolsRevert`, `toolsMicro`);
  - `tools/qa/scenarios/tools_torch.json`, `tools_clicks.json`, `tools_lens.json`, `tools_microprint.json`;
  - all `.meta` files.
- **Untouched owned files:**
  - `TranslationLensCheats.cs` (its cheats still grant the levels);
  - `UvPaperLayer.cs`;
  - `UvPool.mat` and `UvPool.png`.
- **Not edited (other owners):**
  - `OfficeGameplay.unity`: INTEGRATION rebuilds it.
  - `DeskHardware.cs` and `DeskConfigSO.cs`.
  - `UvReveal.shader`: no track lists it; its header still says `_UvConeOn` is "1 while switched on", which is now the fully-lit value of a 0..1 ramp. INTEGRATION may reword it.

## 3. Tests

| Check | Before (main 9b8ab5f) | After |
|---|---|---|
| `compile_check_any.py` | 0 errors | 0 errors (Assembly-CSharp with the QA rig, Assembly-CSharp-Editor, TimeDeskEditMode) |
| Offline runner (TimeDeskEditMode) | passed 3710, failed 0 | passed 3722, failed 0 |
| `tools/qa/editor_runner` (QA_PROJECT set) | - | passed 69, failed 0, 16 NeedsEditor skipped |
| `python -m unittest discover -s tools/qa` | - | 35 OK; the scenario test reads the four TOOLS scenarios and their registered ops |

**New and changed EditMode tests:**
- **`UvTorchHoldTests` (7, new):**
  - a body over the patch hides it, one aside does not;
  - the 1019a built hold covers the patch from the reading view;
  - `Hold` shows ≥95 % from eight eyes in both views, with the patch ≤0.35 m from the hand, and the share it reports equals the one measured;
  - it keeps the built aim when the eye already sees the patch;
  - `PoolShows` on a surface hit vs a miss;
  - `Follow` ≤8 mm;
  - `Fluoresce` 120 ms, and at once under Reduced Motion.
- **`MicroLensTests` (3, rewritten):**
  - credited only with the layer open and a measured cap ≥12 px;
  - `FontSize` reaches the cap with headroom wherever the view draws it;
  - `Text` is one repeat of the line.
- **`TranslationLensTests` (+2):**
  - `Candidate` excludes the PC, undrawn canvases, hidden texts and faded texts;
  - `RevertSpeed` keeps every flip back ≤0.2 s.
- **`LensTextTests` (+3):**
  - right to left, the other words never move while a word flips;
  - `WholeFrame` right to left flips word by word in place and reads as the English sentence only once landed;
  - `WholeFrame` left to right flips as one text.

TDD note: the Domain tests and the APIs were written together in this session. `MicroLensTests` was written before `MicroLens` was rewritten, against APIs that did not exist yet. `Hold_ShowsThePatchPastTheBody…` ran red on the first `Hold` (a 240-try cap; eye (0, 0.35, -0.8) got 94.6 %) and went green once the early-exit search covered every aim.

## 4. For INTEGRATION

**Cleanup list (dead after this track, in files TOOLS does not own):**
- `DeskHardware.TrackLens` (DESK-PLAN's file) has no caller since P0-C. Deleting it frees `MicroPaperLayer.Track` (now an empty shim) and the `LensShape` enum in `MicroLens.cs`, which TOOLS then deletes in the same merge.
- `UvReveal.shader` header (see §2).

**Known-red entries to add to `tools/qa/gate_known_red.txt`:**
- `tools.micro tools_microprint.*  # BUG-2b: UI-PC's scan view draws the microprint at #13 (FIX_PLAN_v2 deferred criterion)`.
- `tools.torch.clickBox`, only until INTEGRATION rebuilds OfficeGameplay with this builder.

**FEATURES.md (rewrite these lines; line numbers as of 9b8ab5f):**
- **51 (the desk machines), the UV torch sentence.** Append: "the hold is chosen for the camera so the torch's body never covers the lit patch (`UvTorchHold.Hold`: the built tilt straight ahead unless the camera would see the body over it, then the nearest tilt and turn that shows ≥97 % of it; tested: `UvTorchHoldTests`); the cone lands on the desk plane (`DeskSurface`), and the pool and the reveal show only while it is on the desk area; the grip trails the pointer by at most 8 mm and follows it every frame; the resting torch tips 10° up on hover; its switch clicks 1 mm down; the revealed marks fluoresce in over 120 ms; while held it is the desk's hold (`HoldRegistry`); its click box is its own meshes' size (`tools.torch.clickBox`)".
- **51, the microprint sentence (written by CUTS: "printed but cannot be read until #17").** Becomes: "the scanner's hidden-layer view prints the line at a size whose capitals reach 12 px where it is drawn (`ScanMicroprint`, `MicroprintView.Print`), and a read is credited only when the layer is open and the line rendered at ≥12 px, measured from the drawn glyphs (`MicroLens.Readable`, `MicroprintView.CapPx`; tested: `MicroLensTests`); the desk line's tick pops when the scan view credits it; the view that draws it is UI-PC's (#13)".
- **156 (the Translation Lens).**
  - Replace "nothing runs while the pointer rests" with: "only the texts of the object under the pointer are read, and only those the player can see: never the PC's (D7) and never a text on a canvas no camera draws or faded out (`TranslationLens.Candidate`; tested: `TranslationLensTests`); while anything is translated the hover is checked every frame, so a word whose object moves away flips back; every flip back lands within 0.2 s (`TranslationLens.RevertSeconds`); a right-to-left label keeps its order while its words flip, and a whole right-to-left label reads as its English sentence once landed (`LensPhrase.Compose`, `WholeFrame`; tested: `LensTextTests`); nothing runs while the pointer rests and nothing is translated".
  - Also drop "the PC's chrome and windows" from the list of places it reads.
- **The speech flip (the TextFlip line, under Translation):** add "its size is fitted once per line and held through the flip (auto-size off until the next line)".

**CHEATS.md:** no change.

**SOUND_LIST:** no new cue.

**Art requests:** none.

## 5. Seams consumed and provided

| Seam | Direction | Partner | Note |
|---|---|---|---|
| `HoldRegistry`/`IHoldable` | consumes | DOC-HANDLE (#8) | the torch registers; Cancel and focus loss are DOC-HANDLE's to call |
| `DeskSurface.Clamp`, its plane | consumes | DOC-HANDLE / DESK-PLAN | read only |
| `MicroprintView.Print/CapPx`, `ScanMicroprint`, `MicroLens.Readable` | provides | UI-PC (#13) | the scan view's microprint row and its credit |
| `TranslationLens.Candidate`, `RevertSeconds`, `LensPhrase.WholeFrame`, `TranslationLensPresenter.Translated` and the `TranslationLens.Hover` marker | provides | GLASSES (Phase B lease) | the engine GLASSES reuses |
| **Request** to TEXT-LANG (#12) | asks | TEXT-LANG | a `DisplayText` per-flip layout (the visual slot of each cell worked out once per line), so a right-to-left speech flip never reorders per letter. TextFlip would then write per step without re-running bidi |

---

## RESULT: deferred to Unity (code-only run; no Unity here)

Everything below runs in the NOPE-p5 editor lane by job, after INTEGRATION's rebuild of OfficeGameplay (Build Office UI) on the merged branch.

**Common set-up for every job:**
1. Write `Library/QA/job_scenario.txt` with the scenario's name.
2. Run the job `QaJobs.Scenario`.
3. Wait for `harness.txt`.
4. Read `run.json` and `analysis.txt` in `Library/QA/runs/<scenario>_<time>/`.
5. Put the GIFs (`seqtools gif`) and the stills in `R7\FIX\TOOLS\`, with `RESULT.md` giving each pass line's measured number.

1. **Build and gate.**
   - Build Office UI, then `QaJobs.Gate`.
   - **Pass:**
     - `tools.torch.clickBox` green: box ≤1.25× the torch's own meshes on every axis (1019a's was 2.6× the torch); record the measured sizes;
     - the EditMode suite green (UnitySkills' known failure aside);
     - the semantic dump differs from main only in UvTorch's BoxCollider size and centre and in `switchTravel`.
2. **Torch (BUG-2c).** Scenario `tools_torch`:
   - day 7 (the torch's day) at A2, office view then reading view, P-FAST/P-SLOW/P-WOBBLE over the handed-over papers;
   - then A1 in both views.
   - If the reading view does not tilt from the step's mat click, retarget that `click` to a paper.
   - **Pass:**
     - every `tools_torch.tools.torch.*.unoccluded` passes: the worst held frame ≥95 % of the lit patch seen past the torch's own meshes, projected (an independent measure from the meshes, not `UvTorchHold`'s capsule);
     - every `*.poolOnDesk` passes: 0 frames of the pool off the desk;
     - `torch_office`/`torch_reading` GIFs at full size: the reveal is never under the body, the pool never floats, there is no pop when the aim changes, and the marks fluoresce in;
     - the camera assertion is clean.
3. **Clicks (BUG-4b).** Scenario `tools_clicks`: day 12 at A1, then A2.
   - **Pass:** `tools.clicks.chipReader` 10/10 and `chipReader_a2` 10/10 on the chip reader; `tools.clicks.uvTorch` 10/10 on the torch.
4. **Translation lens (BUG-52).** Scenario `tools_lens`: day 8 at A2, the PC closed, sweeps over the left two thirds and over the desk.
   - Retarget `toolsRevert` to a foreign word found in the run (the rulebook folder's cover label is the default).
   - For the Arabic answer: open the wheel through the intercom on an Arabic speaker's case (`cheat ForceAppearance` with an Arabic tongue), ask a question, and add a `toolsLens` step over the bubble.
   - **Pass:**
     - `tools.lens.*.phantoms` 0: every translated text drawn and under the pointer;
     - `tools.lens.*.pc` 0;
     - `tools.lens.rulebook.revert` ≤0.2 s;
     - `seqtools flicker seq/lens_sweep` passes on the speech and paper text;
     - the `TranslationLens.Hover` marker ≤0.5 ms per move in the profiler (`QaJobs.GcSites` window), with 0 B per idle frame.
5. **Torch hold (BUG-55b).** In `tools_torch`, add `key` Escape, Q, A, Tab and an Alt+Tab focus loss during a held sweep once DOC-HANDLE (#8) and CAM (#6) are merged.
   - **Pass:** the torch returns to its cradle and nothing else happens: no view change, no paper moves.
   - Before #8 this is not measurable (nobody calls `HoldRegistry.Cancel`).
6. **Scanner microprint (BUG-2b) [deferred to #13].** Scenario `tools_microprint` once UI-PC's scan view prints the row with `MicroprintView.Print`.
   - Retarget `MicroprintRow` to UI-PC's object, at A2, on a microprint paper (a planted ReprintedPaper after #17).
   - **Pass:**
     - `tools.micro.a2.cap` ≥12 px;
     - `tools.micro.a2.credited` true only after the UV layer was opened and drawn (run the step before opening the layer as well: it must be false there);
     - the microprint is read correctly by eye from the still.
7. **Feel cards C3 (and C1's microprint line).** `R7\FIX\FEEL\C3.md` from the `torch_*` sequences, at 1× and 0.25×, plus one Reduced Motion run (Settings' Reduced Motion on, `tools_torch` again).
   - **Expected:**
     - checks 1, 2, 4, 7, 8, 9, 10, 11 and 12 yes (press-frame lift, ≤8 mm lag, 10° hover tip, 120 ms fluoresce, 2-frame spot lag, cues);
     - check 5 (particles) **no** until FEEL-KIT/FEEDBACK provides the emitter (the gap is recorded above);
     - Reduced Motion: no tip, lag, fluoresce or swing.
