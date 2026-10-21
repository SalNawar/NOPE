# DOC-LOOK (Phase A #9): readable papers that look and open like paper

Branch `fix/a-doc-look`, cut from `origin/main` 9b8ab5f (P0: CARVE + CUTS + QA-RIG) and rebased onto 4290bd6 (RUN-SAFE landed; no shared files). Code only: nothing here was run in Unity. Everything that needs Unity is a recipe under **RESULT** below.

The commits:
- The first submission (8c2239d before the rebase).
- The resubmission, which answers the review of 8c2239d. Its nine points are listed under "Review of 8c2239d" with what changed for each.
- A note on the rebase.
- The wave-2 fixes: they answer the re-verify (`reverify:DOC-LOOK:1`, WAVE2_OPEN.md) and record the orchestrator's three decisions of 2026-10-20. See "Wave 2" below.
- The fixes for the re-verify of 460c564 (the wave-2 close). See "Re-verify of 460c564" below.
- The fixes for the re-verify of ce69276. See "Re-verify of ce69276" below.
- The fixes for the re-verify of 5f80371 (WAVE2_OPEN2.md, DOC-LOOK). See "Re-verify of 5f80371" below.

Sources: FIX_PLAN §3 DOC-LOOK, FIX_PLAN_v2 §3 "DOC-LOOK: delta" (D3 and D15 decided; v2 wins), BUGLIST, DECISIONS_1019a (D3, D15, "keep a big mat size", DESK APPROVED), GAME_FEEL_BIBLE rows A1-A6 and A9, QA-RIG §4 and §8.

`docs/FEATURES.md` is **not** edited here. INTEGRATION folds this file in. The lines to rewrite are listed under "FEATURES (for INTEGRATION)".

## Decided by the orchestrator (2026-10-20)

These calls answer the re-verify's two blocking points (WAVE2_OPEN.md DOC-LOOK 3 and 4) and its new S3 bug (5). They are quoted here as given to DOC-LOOK. The decisions log (DECISIONS_1019a.md, "Orchestrator calls, wave 2 close") records 1 and 2 in short: the ownership, the interim, the FORMS-REDESIGN track and the known red. Decision 2's hand-off bullet (DOC-LOOK writes `docs/pending/FORMS-REDESIGN.md`) came from the orchestrator's instructions to this track, and the log records it since 2026-10-21 ("Orchestrator rulings (mine), 2026-10-21", item 5: DOC-LOOK writes the file as the hand-off, and the FORMS-REDESIGN track takes it over).

1. **Ownership approved.** DOC-LOOK may edit `FormArt.cs` (`printLabels`, `labelGrowth`, `valueGrowth`, `Worded`) and add the one `cardFlex` line to `tools/qa/so_reader_allowlist.txt`.
2. **BUG-9 interim approved.**
   - Keep the 0.5 fit floor and labels x1.3 until a new track, **FORMS-REDESIGN**, redesigns the 8 art forms with larger places, so the data reaches the 10/7 px floor at the big mat's reading view.
   - `doclook.forms.capModel` stays red at merge level. INTEGRATION adds the known-red line at merge, with this decision.
   - DOC-LOOK writes `docs/pending/FORMS-REDESIGN.md` as the hand-off: per form, the growth still needed, which fields to merge, drop or move to keep every cross-checked value whole, and the art changes needed.
   - "Legibility is a quality bar, not optional: the redesign is required before any demo."
3. **Fix the new S3 bug:** a shut passport and a shut folder must not keep full-size colliders (the hidden half must not catch clicks or drags). Test it. Done (see "Wave 2", point 5).

**The known-red line for INTEGRATION** (`tools/qa/gate_known_red.txt`, INTEGRATION's file; this branch does not add it):
`doclook.forms.capModel  # BUG-9 note 25, DOC-LOOK (#9): data under the 10/7 px floor at the big mat until FORMS-REDESIGN redesigns the 8 art forms; orchestrator's decision 2026-10-20 (fit floor 0.5 and labels x1.3 approved as the interim; the redesign is required before any demo)`

**The hand-off:** `docs/pending/FORMS-REDESIGN.md`. It covers:
- the floor in pixels;
- each field's growth, its longest value and the width that value needs;
- why today's art cannot reach the floor, plus a model of the least desk size per form (x1.0 to x1.7);
- the art changes;
- per form, what to move and pair;
- the fields that could be dropped only by Saleh's call;
- the multi-line place the code needs;
- the tracks it must agree with.

### Background: why DOC-LOOK could not reach the floor (kept for the record)

**The numbers.** These come from the gate's own model, mirrored in a script: the smallest data and label on each art form at its desk size, with today's re-layout (labels x1.3, values x1.5). The growth is the gate's (`CapHeight.Needed`: the larger of 10 px over the A1 figure and 7 px over the A2 one). The first submission's table used the A1 floor alone.

| Form | At the big mat's view (0.67, the decision) | At Q5 A (about 0.9) | At today's view (1.0) | Growth still needed at the big mat |
|---|---|---|---|---|
| TC-101 passport | data 4.8 / 3.2 px, labels 1.6 px at A2 | 6.4 / 4.3 | 7.1 / 4.8 | x2.2 |
| TC-230 | 5.8 / 3.9, labels 3.4 | 7.8 / 5.2 | 8.7 / 5.8 | x1.8 |
| TC-240 | 5.6 / 3.7, labels 2.8 | 7.6 / 5.0 | 8.4 / 5.6 | x1.9 |
| TC-310 | 2.6 / 1.7, labels 1.6 | 3.5 / 2.3 | 3.9 / 2.6 | x4.1 |
| TC-520 | 5.5 / 3.6, labels 2.9 | 7.3 / 4.9 | 8.2 / 5.4 | x1.9 |
| TC-610 | 4.3 / 2.9, labels 2.8 | 5.8 / 3.9 | 6.5 / 4.3 | x2.4 |
| TC-620 | 5.3 / 3.5, labels 2.5 | 7.1 / 4.7 | 7.9 / 5.2 | x2.0 |
| TC-630 | 5.2 / 3.5, labels 2.7 | 7.0 / 4.6 | 7.7 / 5.2 | x2.0 |

Floors: data 10 px cap at A1 and 7 px at A2, labels 6 px at A2. Each cell shows data at A1 / A2. R4 prints the gate's exact figures.

**Why re-laying out the art boxes cannot close the gap.** At the big mat the passport is about 308 px tall and 202 px wide at 1080p, so its data page is about 200 x 154 px.
- Data at 10 px cap in the value face (Geist Mono, cap 0.71 em, advance about 0.6 em) is a 14 px em, and each character is about 8.4 px wide.
- At that size the whole data page holds about 24 characters across.
- A date needs about 92 px. A 43-character origin needs three full-width lines.
- The passport has seven text fields, plus the photo and the chip.
- So no arrangement of boxes on the TC-101 art reaches the floor. The same arithmetic holds for the other seven forms, and the TC-310's smallest field is the worst case.
- Saleh's "about 1.5x" only gives back today's size (5.1 px) on the 67% mat. Today's size was the size note 25 was filed for.

**The calls, for Saleh (via the orchestrator).**
1. **Bigger papers on screen.** Bring the reading pose closer for papers (CAM). This undoes part of Q5 B. Alone it is not enough: at today's view the data reads 3.9 to 8.7 px.
2. **Redesigned forms.** New Canva art for the eight forms, with fewer and larger fields and type about 2x (a ChatGPT and Canva job; the FIX_PLAN Risks foresaw it). Alone, at the big mat, it models at 5.2 to 11.6 px (passport 9.6).
   - Together with call 1, every form except TC-310 models at 10 px or more (TC-310 about 7.8, so its smallest field needs a larger place).
3. **Lower floors.** For example, data 6 px and labels 3 px at A1. Note 25 ("tiny font") then stays open.
4. **A reading aid**, such as a hold-to-read pose. This conflicts with D1 ("no need to enlarge the documents when you click them").

DOC-LOOK recommended **1 + 2**. The orchestrator chose call 2, as the FORMS-REDESIGN track (decision 2 above). FORMS-REDESIGN.md's model adds larger papers on the mat where the art alone cannot reach the floor, agreed with DESK-PLAN and CAM.

**The two deviations, approved as the interim by decision 2:**
- **The fit floor stays at 0.5 of the art's size; the plan asks for 0.85.**
  - FIX_PLAN says "the fit floor rises from 0.5 to 0.85 ... wrap or abbreviate instead of shrinking".
  - Each value place on the art holds one line. At 0.85 of the grown size, the passport's DESTINATION box holds about 10 characters, so 43-character origins would be cut ("abbreviated"). That is data loss on a field the player cross-checks.
  - With a 0.85 floor, the validator (`ArtLayout.Problems`) would refuse the content. `TextFit.Floor` = 0.85 only labels a text fitted below it as "shrunk" for the gate.
  - FORMS-REDESIGN raises it once each place holds its longest value whole.
- **Labels grow x1.3; the plan asks for 2-3x.** A re-laid label band takes its height from the value under it, so at x2 the values would shrink. FORMS-REDESIGN's art draws the label bands at their floor.

## Review of 8c2239d: what changed (resubmission)

| # | Point | Change |
|---|---|---|
| 1 | BUG-9 cannot reach its floor, and the gate hid it | `doclook.forms.capModel` is now merge level. It also checks labels at 6 px (A2), and each line says how much larger the data must print (`CapHeight.Needed`). The decision is written up above, with numbers, and R4 says the gate is expected red. The fit floor and label growth are flagged for approval, not self-approved. |
| 2 | Fills and Lines quads bent on 4 vertices; chords sank under the face | `DeskDocument.Subdivide` cuts every print quad into cells no larger than `PaperBend.PrintCellShare` (1/24) of the page along each side, before the rest vertices are taken. Test: the bent chord never lies more than 0.3 mm (`BentPrintLift`) behind the face, at the knobs' greatest bow, lag, twist and ripple, on three paper sizes (`PrintCells_TheBentChordNeverSinksUnderTheFace_*`). At 1/16 the worst case was 0.45 mm, so 1/24 (0.20 mm) was chosen. |
| 3 | `BookletFlip`: snap on reversal; overshoot through the paper and desk | `BookletFlip` was rewritten. A turn back starts from the current angle with no lift, timed by the way left (as fast as a swing from rest). The OutBack overshoot is folded back off the surface the cover lands on (a rebound toward the viewer), so the angle stays within 0 to 180. Tests: angle kept on reversal at 5 points both ways, no step faster than a swing from rest, 0 ≤ angle ≤ 180 across whole swings, the rebound, and reversal timing. |
| 4 | GAME_FEEL A1: "open on the press frame", page bow, sounds | `DeskDocument` (Render partial) handles `IPointerDownHandler`. A left press calls `PaperCover.Press()`, which opens a shut cover on the press frame. A cover opened on the counter stays open: it shuts only as the booklet comes back to the counter from a hold or the desk (edge-triggered; it used to shut every frame it lay there). The cover's sheets are an 8x8 grid (`PaperCover.Sheet`), so DocumentCel bends them. The free edge bows up to 3 mm behind the swing (`BookletFlip.Flex`, `PaperCover.Bow`). `passport_open` and `passport_close` play when a swing starts. |
| 5 | D3: the folder does not open or close | Done (`FolderCover`). See BUG-11b. |
| 6 | The folder flick detached its prints | `DeskPaperDepth` raises `Bent` with every bend it shows. `DeskRulebook.RidePrints` moves each printed part of the open page, and the cover's words, onto the bent face, as a paper's prints ride theirs (depth under the centre, slope, `PaperBend.Bulge` lift). Every step restores first. A part that a layout moved in the meantime keeps its new place. |
| 7 | "Cut with a full stop" could change data | `ArtLayout.CutWords` is a new validator rule (`ContentLibraryValidator.Forms`). Every art form's Name and Destination fields are measured against the content's 12 longest given-name and origin words, in capitals, in the art value face, at the least size. A word that would break across lines is a problem. `TmpFormText.ArtFit` now also requires the widest word to fit the place at the size it picks, so `Unbreak` has nothing left to cut once the rule passes. Test: `FormLayoutTests.CutWords_*`. |
| 8 | `Stamp` did not flatten the model's shader bend | `Stamp` calls `ShowBend(PaperBend.Flat)` (the model, the prints, the fills and lines), then `RestorePrints()`. |
| 9 | Ownership of `FormArt.cs` and `so_reader_allowlist.txt` | Not changed here. They still need the orchestrator's (or INTEGRATION's) confirmation. See Ownership. |

## Wave 2: the re-verify of e0e3e8e, and what changed

| # (WAVE2_OPEN.md) | Finding | Change |
|---|---|---|
| 3 | BUG-9 blocked on a decision | Decided (decision 2). The decision and the known-red line are above; the hand-off is `docs/pending/FORMS-REDESIGN.md`. |
| 4 | Ownership of `FormArt.cs` and the allowlist line | Approved (decision 1). See Ownership. |
| 5 | **S3: a shut passport and a shut folder kept full-size boxes**, so their hidden halves took hover, click, press-to-open and drag over bare counter or desk | **Fixed.** See "The shut boxes" below. |
| 6 | The cover's bow flipped sign in one frame on a turn back (a 6 mm pop), and it did not trail the rebound | **Fixed.** `BookletFlip.Flex` now eases (see "The bow" below). |
| 7 | `ArtLayout.CutWords` missed the premades' names; single-token values relied on a height check | **Fixed**, within the limit stated there (the re-verify of 460c564, point 2). See "The words" below. |
| 8 | The cover's emblem and the folder's seal are flat quads. The shutting board's bow rose under them by more than their 0.05 and 0.1 mm gaps (the model at the builder's sizes: up to 0.25 mm under the seal and 0.7 mm under the emblem; the reviewer estimated 0.25 and 0.37) | **Fixed.** See "The marks" below. |

**The shut boxes (S3).**
- **The passport.** `DeskDocument.ShowShut` cuts the sheet's `BoxCollider` (the click's, the hover's and DeskDraggable's proxy) to the data page under the spine while the booklet shows shut or swings (`ShutBox.Keep`, pure). It puts the box back to the whole sheet once the cover lies open.
- **Print parts on the visa page.** Every printed part's own collider over the spine (a seen UV tick, for example) is turned off while shut. Only the boxes it turned off come back on.
- **Each print is judged where it lies.** Each renderer and box is judged by its own place, not its layer root's. A layer's marks on the data page (UvPaperLayer, MicroPaperLayer, whose roots sit at the sheet's centre) no longer hide with the visa page.
- **The folder.** `DeskRulebook.ShowShut` cuts the folder's own card box (its click, its hover and the drag's proxy) at the spine to the board's side while shut (`ShowCardBox`). The spine and the face's right edge are measured into the box's space through the folder's face, so the cut follows the art. It puts the box back as built once the folder opens.
- **The rest of the folder** was already right: the left tabs hide while shut, the cover page's click box is off while shut, and the pages lie under the face.
- **Tests:**
  - `DocLookTests.ShutBox_KeepsOnlyTheSideOfTheSpineThatShows` (pure).
  - The rig op `coverHits` (`DocLookOps`) uses the event system's own raycast at the middle of each half of every booklet and of the folder. A shut booklet's visa page and a shut folder's left board must meet nothing of theirs, and the shown half must meet them. Open, both halves must meet them.
  - Each step says the state it checks in its text ("booklet=shut folder=shut", `DocLookOps.Expects`). It fails when a booklet or the folder is in the other state, when no booklet or no folder is shown to probe, or when the text does not say both states (the re-verify of ce69276, point 1).
  - `doclook_print` runs `coverHits` three times: `shut` at the start (both shut), `folder_reshut` after the folder is shut again (both shut), and `open` once both lie open (both open).
  - The merge gate reads it: `doclook.play.coverHits`, a Play check at merge level (it was demo only until the re-verify of 460c564). Every merge from this one on needs `doclook_print` results taken at HEAD on a clean tree (recipe R7b).
- **A side effect on the rig:** the rig's `click` op aims at a target's collider centre, so clicks on a shut passport now land on its cover.

**The bow (6).**
- **What changed.** `BookletFlip.Flex` is now a state that `Tick` moves toward a target:
  - the target is the sine of the cover's angle times its angular speed over `FullBowSpeed` (a whole swing's mean, at most all of it);
  - its sign follows the way the cover turns, so the free edge trails the swing and the rebound off the desk alike;
  - it changes by at most `FlexRate` (30 a second). A full turn from one way's bow to the other takes at least 67 ms, and at 60 fps the passport's edge moves at most 1.5 mm a frame.
- **At rest.** It settles flat after the cover rests (`Tick` keeps returning true while it settles, so FolderCover redraws the bow to flat).
- **Reset.** `Shut()` and a Reduced Motion jump reset it to flat.
- **The numbers.** The modelled peak is about 0.89 of the most (2.7 mm on the passport, 1.8 mm on the folder). The opening's rebound bows the other way by about 0.12.
- **Tests:**
  - `DocLookTests.BookletFlip_Flex_NeverJumps_WhenTheCoverTurnsBack`: at 60 and 120 fps, five turn-back points, both ways; the turn back alone keeps the bow; no step over `FlexRate` x dt; flat at rest.
  - `DocLookTests.BookletFlip_Flex_TrailsTheSwingAndItsRebound_AndNoneUnderReducedMotion`.
  - The existing `BookletFlip_Flex_TheFreeEdgeTrailsTheSwing_FlatAtRest` still passes.

**The words (7).**
- **(a) Name sources.** Every premade's own name (`ContentLibrarySO.Legendaries`' `displayName`) is now in the Name fields' words, beside every place's lists. The choice is `FieldWords.Of` (Domain, pure, since the re-verify of ce69276): the validator's `ContentWords` hands it the library's lists, and `ArtLayout.LongestWords` takes each field's 12 longest words in capitals. Test: `FormLayoutTests.FieldWords_*`.
  - The 2150 citizens' names are already in those lists.
  - A returning traveller keeps a drawn name.
  - The premades' longest word today is "Michelangelo" (12), shorter than the drawn "Giovanfrancesco" (15), so nothing new fails today. The gap is closed for future content.
- **(b) Single-token values.** `ArtLayout.Problems` now also fails a value whose probe has a word wider than its place at the least size, even when its broken lines would fit the place's height.
  - It measures the **probe's** words, not the field's own. `DocumentForm.Problems` always passes `FormLayout.Probe` values, cut from "Middle Egyptian, hieroglyphs and a reed brush" to each field's longest length. So the check covers a field's real words only where they are no longer than its probe's. The limit is stated in full below.
  - `CutWords` and `Problems` share one test (`ArtLayout.Breaks`).
  - Test: `FormLayoutTests.Problems_AValueWordWiderThanItsPlaceAtTheLeastSize_IsAProblem_EvenWhenItsLinesFit`. It hands `FormLayout.Check` a raw 30-character token, which production never does, so it proves the check itself, not the probe the check is fed.
- **The limit, stated in full** (re-verify of 460c564, point 2). What the validator measures whole on the 8 art forms, and what it does not:

| Values | Measured by | Whole? |
|---|---|---|
| Names (Name fields, and Signature fields since the re-verify) and origins (Destination fields) | `CutWords`: the content's 12 longest words per field, in the field's place | Yes |
| The same names reprinted as Signature prints (TC-101's and TC-620's signatures; a Copy print of a name or an origin alike, none today) | `CutWords`, since the re-verify of 5f80371: the field's words, in the print's own place and style (a hand) | Yes. TC-101's signature is the tightest place a name prints: 0.232 of the page wide (its Name field's place is 0.370). "GIOVANFRANCESCO" takes about 79% of it at the least size, and about 20% of TC-620's |
| TC-230's stub, a Copy print of its Departure date ("28 Sep 2150": words of 4) | nothing: `Problems` measures only the fields' own places, `CutWords` only names and origins | **No** (its words take about 24% of the stub's width at the least size) |
| 7-character IDs (a Citizen ID or a Displacement No.: "ZZZ-999", "DP-9999") and transponder classes (7) | the probe "Middlee": one word of 7 | Yes (the value face is monospaced) |
| Transponders (26 characters, words up to 9), employers (25, words up to 9), waiver numbers (7, probed at a book row's 28) | probe words of 9, 9 and 11 | Yes |
| Statuses ("STANDARD", "ELIGIBLE": one word of 8; TC-101's Travel Class) | the probe "Middle E": words of 6 | **No** |
| Incident numbers ("R-0928-99": one word of 9; TC-610, TC-630) | "Middle Eg": words of 6 | **No** |
| Credits ("9,999,999 cr": 12 characters, its number a word of 9; TC-310's Debt, TC-520's Day Wage) | "Middle Egypt": words of 6 | **No** |
| Facts (28 characters, words of up to 13 and 14: TC-230's Currency Carried and Declared Effects; TC-620's Coin of Home, Native Tongue and Effects Carried) | "Middle Egyptian, hieroglyphs": words of up to 11 | **No** |
| Dates ("28 Sep 2150"), birth dates ("28 Jan 9999 BCE") and terms ("720 days"): words of 4 | probe words of 6, 8 and 6 | Over-measured: a word line on them can be a false alarm from the probe alone |

- **Nothing is cut today.** From FORMS-REDESIGN's place widths, every word in a **No** row fits its place at the least size: Travel Class is 33 px wide at A1 against about 12 px for its 8 characters; Incident is 61 and 95 px against 44 px even at today's full size; the credits places are about 100 px against under 20 px; the facts' places are 73 to 90 px against about 23 px for 14 characters. No check proves it, offline or in the validator: only these figures do.
- **Who closes it.** FORMS-REDESIGN re-lays every place, so its hand-off (section 7) asks it to measure each field's real widest word. Doing it here needs `FieldLengths`' widest values (Domain, not DOC-LOOK's file) and the facts' words.
- **Signature prints** (closed in code by the re-verify of 5f80371): TC-101 and TC-620 reprint the name field as a Signature print (`ArtPrintKind.Signature`), which `Place` adds with no slot. `CutWords` now measures the field's words again in each Signature or Copy print that reprints it, in the print's own place and style (`ArtLayout.Print`, shared with `Place`). Test: `FormLayoutTests.CutWords_ANameReprintedAsASignatureOrACopy_IsMeasuredInThePrintsOwnPlace`, on TC-101's name field and signature as its asset places them. It fails without the change, and with Copy prints skipped.
- **Signature fields** (closed in code by the re-verify of 460c564): `FieldWords.Of` gives them the names, since a holder signs with their given name (`CaseFactory`; "Giovanfrancesco" is 15). So `CutWords` measures them whole. `FormLayoutTests.FieldWords_*` tests the choice offline (it fails without the premades, without the Signature fields or without the origins). Only the validator's handing over of the library's lists is untested offline; R1 runs it.

**The marks (8).**
- The passport's emblem and the folder's seal ride their sheet's bow, as a paper's prints ride its bend:
  - on the face under the mark's centre;
  - turned to the face's slope;
  - lifted by `PaperBend.Bulge` (how far the curl rises past the mark).
- Shutting, the board never comes up through them. Flat, they lie at rest.
- **The arithmetic is pure** (since the re-verify of 460c564, point 1). `CoverBow.Ride` (`PassportCovers.cs`, TimeDesk.Visuals) takes the mark's rest place and half size, the sheet's place and scale, and the bend. It returns the mark's move along z and its two Euler angles. `PaperCover.Ride` (shared by the folder) only puts those on the mark's transform.
- `CoverBow.Of` gives one `PaperBend` for both the shader's `_Flex` (`PaperCover.Bow`) and the ride. It was `PaperCover.BowOf`.
- The folder's seal is now a serialized reference (`FolderCover.seal`), which the builder sets. **It needs the R2 rebuild.** Until then the field is empty and the seal stays flat, as before.
- **Tests** (`DocLookTests.CoverBow_*`, on the folder's and the passport's geometry as built, since the re-verify of ce69276):
  - The sizes are the builder's and the data's, copied into the test with their sources named, because Assembly-CSharp is out of the test assembly's reach. The folder's board is 0.224 by 0.237 m (`RulebookFolderSize` 0.46 by 0.25875, the board from the spine at 0.011 to the body's left edge, `DeskRulebook.BodyHeight` 0.917 tall). The passport's front is its data page, 0.142 by 0.109 m (`Desk_Default` `paperSize.y` 0.34 times TC-101's scale 0.64, aspect 0.65435, the spine at the middle).
  - `CoverBow_Ride_UnderTheBow_EveryMarkClearsItsBoardByItsRestGap`. Unridden, the shutting board rises past the mark's gap (by 0.25 mm under the seal and 0.7 mm under the emblem). Ridden, the mark is turned as Unity's `Euler(pitch, yaw, 0)` turns it, at flex -1, -0.5, 0.5 and 1, centred, a quarter of the way toward the free edge, toward the hinge, and askew. It stays at least its rest gap (0.05 or 0.1 mm) over the board at every point of a 9 by 9 grid.
  - `CoverBow_Ride_Flat_TheMarkLiesAtRest_AndTheBowCurlsOnlyTheFreeEdge`: flat, an off-centre mark gets 0, 0 and 0; the bow keeps the hinge end flat and curls the free edge twice the bow.
  - `CoverBow_Ride_AMarkOffCentre_TakesTheSlopeAndDepthOfTheFaceUnderIt`: an off-centre seal turns to the slope under its own centre, one way or the other by its side; an off-centre emblem tips about its x.
  - **They fail without the fix.** Eight mutations of `CoverBow` were run on the offline runner (again at the builder's sizes for the re-verify of ce69276), and each failed two of the three tests: no ride, no bulge, half the bulge, the yaw's sign flipped, the pitch's sign flipped, no angles, the sheet's place ignored, and the bow's travel turned toward the free edge.
- **What no offline test checks:** that `PaperCover.Ride` puts `CoverBow.Ride`'s numbers on the mark's `localPosition.z` and `localRotation`, that the builder lays the hinges, sheets and marks as the tests model them, and that the sizes copied into the tests still match the builder's and the data's after a later change. Only R7 checks that wiring, by eye.

## Re-verify of 460c564: what changed

| # | Finding | Change |
|---|---|---|
| 1 | `CoverMarks_RideTheShuttingBow_ClearOfTheBoard` tested only `PaperBend`'s maths, never `PaperCover.Ride`, so it passed with the ride deleted | **Fixed (path a).** The ride's arithmetic moved into a pure Visuals helper, `CoverBow.Ride`, with `CoverBow.Of` (was `PaperCover.BowOf`). Three tests replace the old one, and each of seven mutations of `CoverBow` fails them. See "The marks (8)" above. |
| 2 | The new word check measures the probe's words, so the docs claimed more than the code | **Path a, and one gap closed in code.** The full limit is the table under "The words (7)" above. The lines that over-claimed are corrected: BUG-9 below, R1, the FEATURES line, `ArtLayout.Problems`' and `ContentWords`' comments, and FORMS-REDESIGN.md (section 1; its section 7 now asks for each field's real widest word). Signature fields now get the names in `CutWords`. |
| 3 | Stale lines: "Needs a decision", "awaiting approval", and R4's x2.1 and x3.8 | Fixed (BUG-9 below, R4). |
| 4 | `doclook.play.coverHits` was demo only, so the merge gate never checked the S3 fix | **Fixed.** `DemoOnly` is dropped: the check is merge level, so the merge gate needs R7b's results taken at HEAD. |
| (state) | The decisions log names FORMS-REDESIGN but not that DOC-LOOK writes its hand-off; only this file's quote of decision 2 said so | The quote now says which bullet is not in the log ("Decided by the orchestrator" above). |

**Re-checked** (the second run of this re-verify, 2026-10-21, on 46911b9 over 4290bd6; origin/main has not moved): compile_check 0 errors; runner `passed 3777, failed 0`; tools/qa 35 OK. The seven `CoverBow` mutations were run again on the offline runner, each alone, and the file restored: each fails two of the three `CoverBow_*` tests (the bow's travel turned toward the free edge fails `Flat_*` and `AMarkOffCentre_*`; the other six fail `UnderTheBow_*` and `AMarkOffCentre_*`).

## Re-verify of ce69276: what changed

| # | Finding | Change |
|---|---|---|
| 1 (S4) | `coverHits` passed with no booklet probed, and never checked the state its step is named for, so a missed open or shut re-checked the state before it | **Fixed.** Each step says the state it checks in its text (`"booklet=shut folder=shut"`; `DocLookOps.Expects`). The op fails when a booklet or the folder is in the other state, when no booklet or no folder is shown to probe, or when the text does not say both states. The probes judge each half by the stated state. `doclook_print`'s three steps say theirs (`shut` and `folder_reshut`: both shut; `open`: both open). Tests (QA editor runner, offline): `DocLookOpsTests.Expects_*` and `DocLookOpsTests.DocLookPrint_*`, which reads the scenario and fails if a step loses its state or `open` stops checking the open state. The op's own run is R7b's (Unity). |
| 2 (S4) | `ContentWords`' premades and Signature additions had no test, and the FEATURES line attributed them to `CutWords_*` | **Fixed (path a).** The choice moved into a pure Domain helper, `FieldWords.Of`, with `ArtLayout.LongestWords` (Visuals) for the words. The validator's `ContentWords` only hands it the library's lists. New test `FormLayoutTests.FieldWords_NameAndSignatureFieldsMeasureThePlacesAndThePremadesNames_ADestinationTheOrigins`, also run through `CutWords`. The FEATURES line now names it. |
| 3 (S4) | The `CoverBow` tests used a 0.12 x 0.25 m board and a 0.088 x 0.0625 m front, not the builder's, and the "0.8 mm (emblem)" came from the test's geometry | **Fixed (the first path).** All three `CoverBow_*` tests now use the builder's and TC-101's sizes, copied with their sources named (see "The marks (8)"). The unridden rise is 0.25 mm (seal) and 0.7 mm (emblem) at those sizes, and the doc says so. Two asserts were loosened to fit the real sizes: the seal turned more than 0.1 degrees (it turns about 0.35 and 0.16 degrees), the emblem tipped more than 0.5 degrees (about 1.1). |
| 4 (nit) | `DocLookChecks`' comment said capModel is red until Saleh's call | Fixed: it points at the orchestrator's decision 2 and FORMS-REDESIGN. |
| 5 (process) | `docs/pending/FORMS-REDESIGN.md` is a file named for another track; the decisions log does not record that DOC-LOOK writes it | **For the orchestrator** (no code change): confirm the hand-off bullet of decision 2 and that FORMS-REDESIGN takes the file over, or have INTEGRATION record it in the log. **Ruled** on 2026-10-21 (see "Re-verify of 5f80371", point 5). |

**Re-checked** (2026-10-21, on this commit over 4290bd6; origin/main has not moved): compile_check 0 errors; runner `passed 3778, failed 0`; QA editor runner `passed 83, failed 0` (36 NeedsEditor skipped); tools/qa 35 OK.

**Mutations** (a scratch copy of the generated projects; the worktree was only read), each caught:
- `FieldWords.Of`: no premades, no Signature, Destination mapped off; `LongestWords`: no capitals, whole texts. Each fails `FieldWords_*`.
- The eight `CoverBow` mutations at the as-built sizes: the bow's travel turned toward the free edge fails `Flat_*` and `AMarkOffCentre_*`; no ride, no bulge, half the bulge, the yaw's sign, the pitch's sign, no angles and the sheet's place ignored each fail `UnderTheBow_*` and `AMarkOffCentre_*`.
- `DocLookOps.Expects` with shut and open swapped fails both `DocLookOpsTests`; matching any word that starts with the name fails `Expects_*`. `doclook_print` with the `open` step's text dropped, or with `folder_reshut` saying the folder is open, fails `DocLookPrint_*`.

## Re-verify of 5f80371: what changed

| # | Finding (WAVE2_OPEN2.md, DOC-LOOK) | Change |
|---|---|---|
| 3 (S4) | TC-101 and TC-620 reprint the name as a Signature print, which `Place` adds with slot -1; `CutWords` and `Problems` skip slot -1, so no check measured a name there, and four doc lines claimed none is ever cut | **Fixed (path a).** `CutWords` also measures each Signature or Copy print's field words in that print's own place and style: a new private `ArtLayout.Print` builds the print's item for both `Place` and `CutWords`, so the size is computed once. Its problem line names the print ("field 0's Signature print word ..."). New test `FormLayoutTests.CutWords_ANameReprintedAsASignatureOrACopy_IsMeasuredInThePrintsOwnPlace`, on TC-101's name field and signature as `DocTemplate_TC101.asset` places them: "GIOVANFRANCESCO" fits; a 30-letter word fits the name's place but not the signature's, and is one problem on the print; the same as a Copy print; a field with no content words is not measured. `CutWords_AContentWordWiderThanItsPlace*` now expects two problems, the name's place and its signature. The lines are corrected: the limit table (a row for the prints and one for TC-230's date stub, which no check measures), BUG-9's `CutWords` bullet, the FEATURES line, R1, `ArtLayout`'s and the validator's comments, and FORMS-REDESIGN.md (section 1; section 7 now names the prints). |
| 4 (nit) | `CoverBow`'s summary still said 0.8 mm under the emblem | Fixed: 0.7 mm, the rise at the builder's sizes (`PassportCovers.cs`). |
| 5 (process) | The decisions log did not record decision 2's hand-off bullet | **Ruled** by the orchestrator on 2026-10-21 (DECISIONS_1019a.md, "Orchestrator rulings (mine), 2026-10-21", item 5): DOC-LOOK writes `docs/pending/FORMS-REDESIGN.md` as the hand-off, and the FORMS-REDESIGN track takes the file over. Recorded in "Decided by the orchestrator", the ownership table and FORMS-REDESIGN.md's status line. |

**Mutations** (offline runner, each alone, the file restored after): without the print measure, `CutWords_ANameReprinted*` and `CutWords_AContentWord*` fail; with Copy prints skipped, `CutWords_ANameReprinted*` fails.

## Ownership

Every file is DOC-LOOK's by FIX_PLAN §3 "Owns", FIX_PLAN_v2 §3 delta (D3: the passport **and the folder** open and close), CARVE.md (`.DeskPaper.cs`, `DeskDocument.Render.cs`, `DeskRulebook.cs`) or QA-RIG §2 (`Checks/DocLookChecks.cs`, `Tests/PlayMode/DocLook/**`, `tools/qa/scenarios/doclook_*.json`). Notes for the verifier:

| File | Why it is here |
|---|---|
| `Assets/Editor/OfficeArtImporter.cs` (new) | Named in FIX_PLAN §3 DOC-LOOK "Owns" ("a new `Assets/Editor/OfficeArtImporter.cs`") and in §0 rule 2 as the only writer of `Assets/Art/Office/**` import settings. |
| `Assets/Scripts/Office/Desk/FolderCover.cs` (new) | D3's folder open and close, assigned to DOC-LOOK in Phase A (v2 decision table). It is built by `.DeskPaper.cs` and driven by `DeskRulebook.cs`, both DOC-LOOK's. |
| `Assets/Scripts/Visuals/FormArt.cs` | **Approved** (orchestrator, 2026-10-20, decision 1). It is the serialized art-form type that the DocTemplate assets (DOC-LOOK's) hold, read only by `ArtLayout` (DOC-LOOK's) and the two `Worded` callers. Three fields are added (`printLabels`, `labelGrowth`, `valueGrowth`), and `Worded` changes. |
| `tools/qa/so_reader_allowlist.txt` (one line) | **Approved** (orchestrator, 2026-10-20, decision 1). Cards are now rigid by rule (`PaperBend.FlexOf`, D3), so `MotionKnobs.cardFlex` has no reader left and `qa.so.readers` would refuse the merge. The line names BUG-11b / D3 and its owner, DOC-HANDLE, who deletes the knob with its 11a work; the line goes with it. |
| `docs/pending/FORMS-REDESIGN.md` (new) | The hand-off the orchestrator asked for (decision 2). Named for another track: ruled on 2026-10-21 (DECISIONS_1019a.md, "Orchestrator rulings (mine), 2026-10-21", item 5), DOC-LOOK writes it and the FORMS-REDESIGN track takes the file over. |
| `Assets/Scripts/Domain/FieldWords.cs` (new) | The validator's word sources for `ArtLayout.CutWords`, moved out of `ContentLibraryValidator.Forms` (DOC-LOOK's change) into Domain so they are tested (the re-verify of ce69276, point 2). It has no other reader. |
| `Assets/Tests/PlayMode/DocLook/Editor/DocLookOpsTests.cs` (new) | QA-RIG §2: `Tests/PlayMode/DocLook/**` is DOC-LOOK's. It compiles into Assembly-CSharp-Editor (an `Editor` folder, as `QaRig/Editor`), so the QA editor runner runs it offline. |
| `DeskDocument.Render.cs` declares `IPointerDownHandler` | The partial is DOC-LOOK's. If DOC-HANDLE (#8) adds an `OnPointerDown` to the logic partial for the press-frame pick-up, the two are a compile clash at the rebase: merge the bodies (`cover.Press()` plus DOC-HANDLE's). |

Not committed: `NOPE-feat-clock.sln` (IDE churn).

## Changes by BUG id

### BUG-9: text too small; the fit shrank the important values (note 25)
**The floor is not met; see "Decided by the orchestrator" above (decision 2) and its "Background".** What this branch does:
- **Bigger text on every art document.** Each of the 8 art templates (`DocTemplate_TC101/230/240/310/520/610/620/630.asset`) now has:
  - `printLabels: 1`: the paper always shows its clean face and prints every label and caption, in the art's ink, in English or the culture's language. Before, the small baked labels showed whenever English was read.
  - `labelGrowth: 1.3` and `valueGrowth: 1.5`: as large as the art's places allow (see the deviations above).
- **Cells re-laid** (`ArtLayout.Place`, `Cell`, `Stacked`, `Grown`, `LabelLine`, `ValueFill`):
  - A label that stands above its value becomes a band at the top of the cell, and the value takes the rest below it. The field is picked where its value prints.
  - A label beside its value grows inside its own place.
  - A value never grows taller than one line of its place (`ValueFill` 0.74).
- **Wrap or cut, never break a word** (`TextFit`, `TmpFormText.Unbreak`, called from `DeskDocument.Print` and from the rulebook's rows and guide body):
  - A text whose widest word is too wide shrinks just enough for it (never under `WordLeast` 0.6).
  - A word still too wide is cut and ends with a full stop ("KONSTANTI."), so no word ever breaks across two lines.
  - A word that holds a rich-text tag is never cut (the guide's sheets).
  - The rulebook's rows are only cut, never shrunk, because `LayRows` sets their size again afterwards.
- **No value is cut by the fit, and the validator proves it for names and origins** (resubmission; the rest is the limit below):
  - `TmpFormText.ArtFit` picks the largest size at which the text fits its place **and** its widest word fits the place's width.
  - `ArtLayout.CutWords` (in the validator) fails any of the content's longest name words (every place's lists and every premade's name, on Name and Signature fields) and origin words that does not fit at the least size, in its field's place and, since the re-verify of 5f80371, in each Signature or Copy print that reprints the field (TC-101's and TC-620's signatures). So when the validator reports no `CutWords` line (R1), `Unbreak` cuts no name or origin anywhere an art document prints one.
  - Other fields keep the probe check (`ArtLayout.Problems`). Since wave 2 it also fails a probe word wider than its place at the least size. That covers a 7-character ID whole, but not a status, an incident number, a credits figure or a fact's word, whose probe words are shorter than their own. The full limit is the table under "Wave 2", point 7. A Copy print of any other field (TC-230's date stub) is measured by neither check. Nothing is cut today, but for those fields and that stub no check proves it.
- **The 0.85 floor (a deviation, approved as the interim by decision 2).** `TextFit.Floor` = 0.85 marks a text fitted below it as shrunk (`TextFit.Shrunk`). The least size stays `TextFit.Least` 0.5 of the art's own size (`ArtLayout.FitFloorOf` = 0.5 / growth).
- **The rulebook paginates before it shrinks** (`DeskRulebook.RowTextFloor` 0.6 → 1). Only a rule alone on its sheet shrinks: it stops at `TextFit.Floor` first and goes down to `RowTextLeast` only as a last resort.
- **The validator measures in the art's value face.** `ContentLibraryValidator.Forms` measures an art form in `FormStyleSO.artValueFont`, not LiberationSans.
- **The cap-height gate.**
  - `CapHeight` (pure): pixels of cap height from the size, the page, the face's cap per em and the view scale; `Needed` gives the growth still missing. The floors are 10/7 px for data and 6 px for labels at A2.
  - `doclook.forms.capModel` (**merge**): modelled at the big mat's reading view, data and labels. It is red today.
  - `doclook.play.capHeight` (demo): measured on screen by the rig's `capHeight` op (`DocLookOps`, scenario `doclook_cap.json`).
  - `doclook.forms.regrown` (merge): every clean-faced art form prints its labels, and its smallest data grew ≥1.15x.

### BUG-10a: the hover hull shattered (D15 decided: papers lift, props get an outline; notes 3, 8, 9)
- **Papers, the folder and its tabs lift.**
  - They implement `IHoverLift` (`DeskDocument`, `DeskRulebook`).
  - `HoverHighlighter` finds the `IHoverLift` on or above the hovered Clickable and calls `HoverLift(InteractionFeedbackSO.paperHoverLift` 0.3).
  - `DeskPaperDepth` eases the contact shadow 30% of the way to "held" in 90 ms (the Balanced spring), so the paper rises Papers, Please-style with the hand cursor and gets no outline.
  - The folder's new cover-page click box hovers the same way.
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
- The page-turn flick no longer lifts the text off its lines (resubmission; see BUG-11b, the folder).

### BUG-11b: look, open and close (D3 decided)
- **The passport arrives shut and opens.**
  - `PaperCover` is a hinge on the booklet's spine. It carries the cover front (the nation's colour with its emblem in gold) and, back to back, the visa page's half of the face art.
  - **It opens on the press frame** of a left click on the shut cover. `DeskDocument.OnPointerDown` calls `PaperCover.Press` (GAME_FEEL A1).
  - It also opens when it is picked up or lies on the desk.
  - It shuts as it comes back to lie on the counter (the hand-back). This is triggered on arrival only, so a booklet opened by a click on the counter stays open.
  - `BookletFlip` (pure, `PassportCovers.cs`), GAME_FEEL A1: a 15° lift in 40 ms, then a 160 ms OutBack swing whose overshoot is a rebound off the desk (0 ≤ angle ≤ 180 always). A turn back mid-swing starts from where the cover is. Under Reduced Motion it jumps.
  - **Page bow:** the cover's sheets are 8x8 grids (`PaperCover.Sheet`). DocumentCel bows the free edge up to 3 mm behind the swing (`BookletFlip.Flex`, `PaperCover.Bow`, `FlipBow` 1.5 mm, lag 1).
  - **Sound:** `passport_open` and `passport_close` play when a swing starts. The cues and clips are FEEDBACK's (the bank has them, without clips today). This branch only makes the call at the action site, as FIX_PLAN FEEDBACK asks ("cues are requested by other tracks: ... passport open"). *Assumption: if FEEDBACK also wires these calls, keep one.*
  - While it is shut:
    - the visa page is cut from the model (`DocumentCel._Clip`, now a line `a x + b y > c`; `DeskPaperDepth.ClipAbove`);
    - the prints above the spine are hidden;
    - the contact shadow lies under the data page only (`DeskDocument.ShowShut`);
    - **the visa page takes no pointer** (wave 2, S3): the sheet's box covers the data page only, and printed parts' boxes over the spine are off (`ShutBox`).
  - The emblem rides the cover's bow (`PaperCover.Ride`), and the bow eases on a turn back (`BookletFlip.FlexRate`).
  - The builder adds `Sheet/Cover/Hinge/{Front, Emblem, Back}` to the paper template (`BuildPassportCover`).
- **The folder opens and closes** (resubmission; D3, GAME_FEEL A3 "T2 open"). `FolderCover` (new) is the folder's front board, hinged on its spine (51.1% across the art, between the rivets). It carries the board's outside (manila with the Bureau's seal) and, back to back, its inside: the left half of the folder's art, its cover page.
  - **It arrives shut each day** (`DeskRulebook.Show`, the day's rules). Shut:
    - the board lies over the right sheet;
    - the left board is cut from the model (`DeskPaperDepth.ClipLeftOf`);
    - the contact shadow lies under the right sheet only;
    - the pages lie 4 mm under the face (out of sight and out of the raycast), so a click on the shut folder meets its own box;
    - **its own box covers the board's side only** (wave 2, S3: `DeskRulebook.ShowCardBox`), so the cut left board takes no hover, click or drag;
    - the cover's words and the tabs over the left board (RULES, PAPERS) hide. GUIDE, with its NEW badge, and SEALS stay.
  - **It opens** on a click on the shut folder, on a tab (opening at that tab), on any page turn and when the guide opens a sheet.
  - **It shuts** on a click on its open cover page (`FolderCoverClick`, a click box over the left board just above the folder's own).
  - The guide's arrow points at the shut board.
  - Inspect's row and seal bounds report "closed" while it is shut.
  - The swing is `BookletFlip`, with a 2 mm edge bow (a heavy board, A3 ≤3 mm) and the `rulebook_page` cue.
  - Pocket fold and put-away stay Phase B (DESK-HOMES, B3).
  - The builder adds `Booklet/Cover/Hinge/{Front, Seal, Back}`, `Booklet/FolderCoverClick` and the materials `Rulebook_CoverOutside`, `Rulebook_CoverInside` and `Rulebook_CoverSeal`. It also sets `FolderCover.seal` (wave 2: the seal rides the board's bow).
- **Cards are rigid** (`PaperBend.FlexOf`: a card gets 0 whatever the knobs say). They keep their contact shadow.
- **"The folder never bends" is retired.** A page turn flicks the folder's loose sheets: a 3 mm bow and a 1.5 mm twist that ease flat over 0.35 s, none under Reduced Motion (`DeskPaperDepth.Flick`; GAME_FEEL A3). **The printed rows ride the flick** (resubmission: `DeskPaperDepth.Bent`, `DeskRulebook.RidePrints`), so no row sinks under a rising corner or leaves its ruled line.

### BUG-12a: print detached from the bending face
- **Print and face are one surface.**
  - The Fills and Lines meshes bend vertex by vertex with the face (`BendPrintMeshes`, using rest and bent vertex buffers kept between frames: no allocation per bend).
  - **Their quads are cut into cells no larger than 1/24 of the page per side** (`DeskDocument.Subdivide`, `PaperBend.PrintCells`; resubmission), so the straight chords stay within 0.2 mm of the face at the greatest bend, under the 0.3 mm the print rides over it.
  - Every other printed part rides the bent face and is lifted by how far the curl rises past its flat piece (`PaperBend.Bulge`).
- **The corner relief** (`PaperDepth.SheetRelief`/`ReliefOver`, matching `docs_models.py` sheet(): `lift_max` 0.7 mm in the bottom-right 28%, and a 0.2 mm edge curl): every print part and every fill or line vertex rests above it (`RestFlatPrints`).
- **One material in every zone.** The paper template's face now wears the cel material on the counter too (the URP Lit `Paper` material is gone), so the zone swap is a no-op.
- **Stack reorder is instant:** the rank shadow (BUG-40c) needs no spring. The 1.5 mm `SetLift` spring is DOC-HANDLE's.

### BUG-12b: a stamp right after a drop jumped to the page centre
`DeskDocument.Stamp` flattens the bend through `ShowBend(PaperBend.Flat)`: the model in its shader, every print part, the fills and the lines. It then puts every part back at rest and places the mark. The next bend takes fresh rest poses, so the stale `_printRest` from before the drop no longer wins. A physics bend that comes back exactly flat on the next frame finds face and print already flat together. The mark also clears the corner relief.

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
  - **Risks to check in R3 (review):**
    - 34 of the 123 office textures are not multiples of 4 (the 1254 px and 1672 x 941 sheets, `Booth/partition_calendar`, `HallFlags/HallFlag_Ochre`, the three `Placeholder/` pictures), and BC7 needs that.
    - About 50 sprites that import Uncompressed today (`Placeholder/Posters` 27, `Booth` 11, `Placeholder` 9, plus `HallFlags` and `DeskClean`) become compressed and mipmapped.
    - No office texture is point-filtered today.
  - Other tracks ask for rule rows. None have been asked for yet.
- The papers' slot art (`Forms/`) gets trilinear and CompressedHQ (`ArtSlotImporter`).
- Gate: `doclook.import.office`.

### BUG-65g: form text and depth setup cost
- `DeskPaperDepth.ShowShadow` no longer allocates two lists through `ConvertAll` on every shadow update during a lift or hover. It reuses its vertex and colour lists.
- The renderer lookup is cached once (`Flexed()`).
- The flick rider reuses its renderer list.
- **Not reduced: `TmpFormText`'s per-paper setup.**
  - `Unbreak` and `ArtFit`'s word check add a few `GetPreferredValues` calls per printed text.
  - `Subdivide` runs once per print. It makes at most 24 x 24 cells for a page-sized fill, usually 24 x 1 for a rule.
  - Pooling and pre-building are DOC-HANDLE's; measure on its call-next recipe.

## Tests (offline: compile 0 errors; runner `passed 3779, failed 0` and QA editor runner `passed 83, failed 0` on 4290bd6, after the re-verify of 5f80371)

| Test | Covers | Fails without the fix because |
|---|---|---|
| `DocLookTests.WordShare_*`, `Unbroken_*`, `WrapWords_*`, `IsLogicalArabic_*`, `Shrunk_*` | BUG-9, 37c: no mid-word break | `TextFit` is new (compile error); the tag assert fails on the cut-everything version |
| `DocLookTests.RuledLines_*` | BUG-10c | new |
| `DocLookTests.CapHeight_Pixels_*`, `CapHeight_Needed_*` | BUG-9 gate maths and its shortfall | new |
| `DocLookTests.BookletFlip_ShutOpenShut_*`, `_ReducedMotion_*` | D3: shut → open → shut state machine, the lift both ways, Reduced Motion | new |
| `DocLookTests.BookletFlip_NeverPassesThroughTheSurface_*` | review 3: 0 ≤ angle ≤ 180 over whole swings, and the rebound | 8c2239d peaked at 193.8° and −13.8° |
| `DocLookTests.BookletFlip_TurningBackMidSwing_KeepsTheAngle_*`, `_ATurnBack_IsTimedByTheWayLeft` | review 3: angle continuity at 5 points, both directions; no step faster than a swing from rest | 8c2239d jumped (180.8 → −0.8 at p = 0.5) |
| `DocLookTests.BookletFlip_Flex_*` | review 4: the page bow trails the swing and is flat at rest | new |
| `DocLookTests.FlexOf_ACardNeverBends_*` | D3 card rigidity (v2's added test) | before, a card took `cardFlex` 0.12 |
| `DocLookTests.Bulge_*` | BUG-12a: a lifted part is over the face everywhere under it | new |
| `DocLookTests.PrintCells_*` (2) | review 2: the cell count, and the chord never more than 0.3 mm behind the face at the greatest bend | 4-vertex quads lay up to 14 mm behind |
| `DocLookTests.SheetRelief_*`, `RankStrength_*` | BUG-12a corner, BUG-40c | new |
| `FormLayoutTests.PrintLabels_*`, `Growth_*` (4), `FitFloorOf_*`, `CapHeight_Smallest_*` | BUG-9 re-layout | before, labels printed only when translated and sizes did not grow |
| `FormLayoutTests.CutWords_*` | review 7: a content word wider than its place at the least size is a problem; a short one is not; no art, no problem | new |
| `FormLayoutTests.CutWords_ANameReprintedAsASignatureOrACopy_IsMeasuredInThePrintsOwnPlace` | the re-verify of 5f80371, point 3: a name reprinted as TC-101's signature is measured in the print's own place, as a Signature and as a Copy print | `CutWords` skipped every print (slot -1): the 30-letter word gave no problem |
| `FormLayoutTests.FieldWords_NameAndSignatureFieldsMeasureThePlacesAndThePremadesNames_ADestinationTheOrigins` | the re-verify of ce69276, point 2: a Name and a Signature field measure the places' and the premades' names, a Destination field the origins, other fields none; through `CutWords`, a premade's too-wide word is a cut on its field | `FieldWords` is new; each of five mutations (no premades, no Signature, no Destination, no capitals, whole texts) fails it |
| `ArtSlotsTests.Mipmapped_AndTrilinear_*`, `OfficeImports_*` | BUG-29, 63; the AnimeHallLayers early return | new |
| `DocLookTests.ShutBox_KeepsOnlyTheSideOfTheSpineThatShows` | wave 2, S3: a shut booklet's and folder's box keeps the shown side | `ShutBox` is new; the boxes were whole |
| `DocLookTests.BookletFlip_Flex_NeverJumps_WhenTheCoverTurnsBack` | wave 2, point 6: no bow step over `FlexRate` x dt at 60 and 120 fps, five turn-back points, both ways | the old bow flipped 2 (6 mm) in one frame at 90 degrees |
| `DocLookTests.BookletFlip_Flex_TrailsTheSwingAndItsRebound_AndNoneUnderReducedMotion` | wave 2, point 6: the bow trails the rebound; flat under Reduced Motion and on arrival | the old bow kept the opening's sign through the rebound |
| `DocLookTests.CoverBow_Ride_UnderTheBow_EveryMarkClearsItsBoardByItsRestGap`, `CoverBow_Ride_Flat_*`, `CoverBow_Ride_AMarkOffCentre_*` | wave 2, point 8; the re-verify of 460c564, point 1 (they replace `CoverMarks_RideTheShuttingBow_ClearOfTheBoard`, which tested only `PaperBend`) | unridden, the board rose up to 0.25 mm (seal) and 0.7 mm (emblem) under marks 0.05 and 0.1 mm over it, at the builder's sizes; each of eight mutations of `CoverBow` fails two of the three |
| `FormLayoutTests.Problems_AValueWordWiderThanItsPlaceAtTheLeastSize_IsAProblem_EvenWhenItsLinesFit` | wave 2, point 7b | the height check passed a broken single-token value |
| `DocLookOpsTests.Expects_*`, `DocLookOpsTests.DocLookPrint_*` (QA editor runner) | the re-verify of ce69276, point 1: a `coverHits` step reads the state it checks; every `doclook_print` step says both, `open` the open one | `Expects` is new; a swapped or loose reading, a step without its state or with the wrong one, each fails |
| rig `coverHits` in `doclook_print` (gate `doclook.play.coverHits`, merge level) | wave 2, S3, in the game; it fails a step whose booklet or folder is in the other state, or with no booklet or folder to probe | needs Unity (R7b) |

`python -m unittest discover -s tools/qa -p "test_*.py"`: 35 OK (the edited `doclook_print.json` still parses, with its three `coverHits` steps).
`QA_PROJECT=<worktree> dotnet run --project tools/qa/editor_runner -c Release -- <cc>\Temp\Bin\Debug`: `passed 83, failed 0` (36 NeedsEditor skipped), `DocLookOpsTests` among them.

## Open (not in this commit)
1. **BUG-9's floor:** decided (decision 2). FORMS-REDESIGN does the redesign (`docs/pending/FORMS-REDESIGN.md`). Until it lands, `doclook.forms.capModel` stays red on INTEGRATION's known-red line, and **no demo** ships.
2. **The hover IoU op** (QA-RIG §8 "hover IoU ≥0.9, its op"). No ID-mask op is written. The hover is judged from the `doclook_hover` sequences by eye (recipe R8). Papers no longer get an outline (D15), so the IoU applies to the props' rim only.
3. **The line-mask diff** for "0 frames where a rule line disappears". The rig has no line-mask tool, so recipe R7 uses `flicker --static` plus a frame sheet.
4. **Labels re-baked into the face art.** Not needed for the current art: the clean faces plus `printLabels` print every label in TMP in the art's ink (the plan's fallback, made the default). A redesign (call 2) would re-bake.
5. **`docs_models.py` cover meshes.** The covers are grid quads on hinges. No model re-export.
6. **TmpFormText setup cost** (BUG-65g, see above).
7. **GAME_FEEL A1's laminate glint on hover and a worn cover per nation.** Not done (art).
8. **The folder's pocket fold** (A4) is DESK-HOMES' (Phase B).

## FEATURES (for INTEGRATION)
Line numbers as of 4290bd6 (the same as at 9b8ab5f):
- **16 (pet pictures):** "imported uncompressed and bilinear without mipmaps" → "imported uncompressed, with mipmaps and trilinear (`ArtSlots.Mipmapped`/`Trilinear`, BUG-29; tested: `ArtSlotsTests`)".
- **42 (paper bend):**
  - "a card `cardFlex` 0.12" → "a card never bends (`PaperBend.FlexOf`, Saleh's D3; tested: `DocLookTests.FlexOf_ACardNeverBends_WhateverTheKnobs`)".
  - "and never bends" (the folder) → "and a page turn flicks its loose sheets, its printed rows riding the bend (`DeskPaperDepth.Flick`, `DeskRulebook.RidePrints`)".
  - Add: "the printed fills and lines bend with the face vertex by vertex, cut into cells of at most 1/24 of the page so they never sink under it, and every other printed part rides it lifted clear of the curl and the sheet's lifted corner (BUG-12a; tested: `DocLookTests.PrintCells_*`, `Bulge_*`, `SheetRelief_*`)".
  - Add: "a passport arrives shut; its cover swings open on the press of a click, when picked up or laid on the desk, and shuts as it comes back to the counter, with a 15° lift, an OutBack swing that rebounds off the desk and a page bow of up to 3 mm that trails the swing and its rebound and eases through flat on a turn back (`PaperCover`, `BookletFlip`; tested: `DocLookTests.BookletFlip_*`); its emblem rides the bow; shut, only its data page takes the pointer (`ShutBox`; tested: `DocLookTests.ShutBox_*`, rig `coverHits`)".
  - Add: "the rulebook folder arrives shut each day; a click on it or a tab opens it and a click on its cover page shuts it; shut, only the board's side takes the pointer (`FolderCover`, `DeskRulebook.ShowCardBox`, Saleh's D3)".
  - Add: "the contact shadow darkens with stack rank (`PaperDepth.RankStrength`, once DOC-HANDLE raises `DeskEvents.StackChanged`)".
- **63 (NEW badge):** "A red NEW badge over the rulebook's GUIDE tab" → "A red NEW sticker on the rulebook's GUIDE tab".
- **194 (hover):** rewrite: "Hover: a paper, the rulebook folder and its tabs lift (Papers, Please-style; `IHoverLift`, `InteractionFeedbackSO.paperHoverLift`) with the hand cursor; a prop gets a 2 px rim round its silhouette on the office camera only (`HoverHull` stencil mask + screen-space push, `outlinePixels`); nothing hovers while something is held (Saleh's D15, BUG-10a); the traveller's hit zone gets the hand cursor only; desktop UI as before".
- **New line (document text):** "Documents drawn on their art print every label over the clean face in the art's ink and grow their text as far as the art's places allow (`FormArt.printLabels`, `labelGrowth` 1.3, `valueGrowth` 1.5; a label over its value is re-laid as a band at the cell's top); no word ever breaks across lines (the fit keeps every word inside its place; a word that still does not fit is cut with a full stop); the validator fails any name word (premades' included, on name and signature fields and the signature or copy prints that reprint them, each in its own place) or origin word wider than its place at the least size (`ArtLayout.CutWords`) and checks every other field's probe words (`ArtLayout.Problems`; a probe's words can be shorter than a status's, an incident number's, a credits figure's or a fact's own, and a copy print of another field, the entry ticket's date stub, is measured by neither); an Arabic text is laid line by line (`TmpFormText.Unbreak`, `TextFit`); the rulebook paginates before it shrinks and its lines sit on the ruled lines (`RuledLines`); the cap-height gate (`doclook.forms.capModel`, merge level; `doclook_cap`) holds data to ≥10 px cap at 1080p and ≥7 px at 720p, labels ≥6 px at 720p, and is red until FORMS-REDESIGN redesigns the 8 art forms (the orchestrator's decision of 2026-10-20) (tested: `DocLookTests`, `FormLayoutTests.Growth_*`, `CutWords_*`, `FieldWords_*`, `Problems_*`)".
- **New line (import):** "Every texture under `Assets/Art/Office/` (never `AnimeHallLayers/`) imports with mipmaps, trilinear and BC7 (`OfficeArtImporter`; tested: `ArtSlotsTests.OfficeImports_*`; gate `doclook.import.office`)".

## RESULT

**Offline:**
- compile_check: 0 errors (Assembly-CSharp-Editor, TimeDeskEditMode).
- runner: `passed 3779, failed 0` after the re-verify of 5f80371 (3778 after the re-verify of ce69276; 3777 after the re-verify of 460c564; 3775 after wave 2; 3770 after the rebase onto 4290bd6; 3741 before it).
- QA editor runner (`QA_PROJECT` set): `passed 83, failed 0`, 36 NeedsEditor skipped.
- QA python tests: 35 OK.

**deferred: Unity.** Run these in the NOPE-feat-clock editor through its job runner, in the order given.
- R1-R4 and R7b are this merge's gate (R7b through `doclook.play.coverHits`, at merge level). R4 is **expected red** on `doclook.forms.capModel`, on INTEGRATION's known-red line (decision 2).
- The pixel pass lines (R5 and on) are measured at the final paper size and framing, so they count after DESK-PLAN (#5), CAM (#6) and DOC-HANDLE (#8) have merged (FIX_PLAN merge order: DOC-LOOK is #9).

- **R1 Import, validator and EditMode.**
  - Steps:
    - Run `__EMPTY_SCENE__`, then refresh. Wait until the heartbeat says idle (the importer version bumps reimport `Assets/Art/Office/**` and the Forms and pet slots once).
    - Run `Tools > TimeDesk > Validate Content Library`.
    - Run the EditMode suite.
  - **Pass:**
    - 0 compile errors.
    - The validator reports no `would cut it` problem (`ArtLayout.CutWords`) and no new form-fit problem. `CutWords` now also measures Signature fields' names: TC-310's Signature place is about 53 px wide at A1, and "GIOVANFRANCESCO" needs about 23 px at the least size, so no line is expected. Since the re-verify of 5f80371 it also measures the names in TC-101's and TC-620's Signature prints; TC-101's is the tightest, and "GIOVANFRANCESCO" takes about 79% of its width at the least size, so no line is expected there either.
    - Wave 2's word check (`ArtLayout.Problems`: "has a word ... wider than its place") is new, and it measures the probe's words. Before acting on a line from it, measure the field's real widest word (the table under "Wave 2", point 7): a line on a date or a term can be a false alarm from the probe alone. A real cut is re-laid (FORMS-REDESIGN) or listed with the decision.
    - Every test passes except the known `UnitySkills ... SceneSummarize_CountsObjectsCorrectly`.
    - `QaEditorGateTests.EveryScenario_Reads_WithKnownOps` passes with `doclook_cap`, `doclook_print` and `doclook_hover` (the `capHeight` op is registered).
  - The console shows no "multiple of 4" compression warning for a texture that matters. If one does, the importer gets a rule row for it: keep it uncompressed.
- **R2 Rebuild OfficeGameplay.**
  - Steps: `Tools > TimeDesk > Build Office UI`. Compare the semantic dump with main's.
  - **Pass:** the only differences are these:
    - `PaperTemplate/Sheet/Cover/Hinge/{Front, Emblem, Back}` is new and inactive;
    - `PaperTemplate/Sheet/Paper` wears `Paper_ExamineCel`;
    - the Rulebook's `TabPlate1-4` and `Tab1-4/Text` sit lower (plate y −0.0026, on the board);
    - `GuideBadge` moves onto the GUIDE tab;
    - `Rulebook/Booklet/Cover/Hinge/{Front, Seal, Back}` (with `FolderCover`, its `seal` set to the Seal quad) and `Rulebook/Booklet/FolderCoverClick` are new, and `DeskRulebook.folderCover` is set;
    - the materials `Rulebook_CoverOutside`, `Rulebook_CoverInside` and `Rulebook_CoverSeal` are new.
  - The art scene `OfficeScene.unity` is byte-unchanged. Commit `OfficeGameplay.unity` and the three new materials.
- **R3 Importer scope** (FIX_PLAN "Importer scope", plus the review's risks).
  - Steps: after R1, run `git status --porcelain -- Assets/Art/Office/AnimeHallLayers`, then `git status -- Assets/Art/Office`.
  - **Pass:** the first command shows 0 lines. `ArtSlotsTests.OfficeImports_TheOfficesArt_NeverTheHallLayers` passes.
  - **Do not commit the reimported metas blindly.**
    - Take before and after office-view stills at A1 (QaJobs.Scenario `smoke_day1`, still at the booth).
    - Compare the booth sprites (`Booth/`, `Placeholder/`, `Placeholder/Posters/`) and the 34 textures that are not multiples of 4.
    - Commit the `.meta` changes only when the stills show no softening of the pixel-exact sprites and no colour change. Otherwise add a rule row (keep those uncompressed, no mips) and reimport.
- **R4 Gate.**
  - Steps: `QaJobs.Gate`.
  - **Pass:**
    - `doclook.forms.regrown` is ok (each form is ≥x1.15; the report line lists them all).
    - `doclook.import.office` is ok ("N checked, 0 off, M under the hall layers left alone").
    - `qa.so.readers` is ok with the `cardFlex` allowlist line.
    - There is no other new red.
  - **Expected red: `doclook.forms.capModel`.**
    - Its line lists each form's data at A1/A2, its labels at A2 and the growth still needed. The table at the top predicts them: passport about 4.8/3.2 px, x2.2; TC-310 about 2.6/1.7 px, x4.1 (`CapHeight.Needed` takes the larger of the A1 and A2 ratios: 7/3.2 and 7/1.7).
    - INTEGRATION's known-red line (decision 2, quoted above) lets the merge through on it.
    - Copy the line into NOTES_RECHECK for note 25.
- **R7b Shut boxes** (wave 2, S3; run it right after R2, it does not depend on the final framing).
  - Steps: `QaJobs.Scenario` with `doclook_print`.
  - **Pass:** `doclook_print.coverHits.shut`, `.folder_reshut` and `.open` are all ok. Each line reads "booklet shut, folder shut: 1 booklet(s), 4 probes, 0 wrong" (`open`: "booklet open, folder open: ...").
  - **If one fails**, its detail says why:
    - "no booklet is shown": day 1 at seed 12345 has no passport on the counter at that step. Change the scenario (a later step, or `jumpDay`), not the check.
    - "is shut, the step checks it open" (or the other way): the click before it missed. `click` targets the first active `PaperTemplate(Clone)`, which can be another paper when two lie on the counter. Aim the click at the passport, then run again.
  - Also by hand on the shut passport and the shut folder: hover, click and drag on the empty hidden half do nothing (no lift, no hand cursor, no pick-up), and the shown half works.
  - The merge gate's `doclook.play.coverHits` reads these results. They must be taken at HEAD on a clean tree, so run R7b again after any later commit.
- **R5 Cap height** (BUG-9, note 25).
  - Steps: write `doclook_cap` to `Library/QA/job_scenario.txt` and run `QaJobs.Scenario`.
  - **Pass (after the decision):**
    - `doclook_cap.capHeight.a1` ok with data ≥10 px;
    - `doclook_cap.capHeight.a2` ok with data ≥7 px and labels ≥6 px;
    - `console` 0 errors, `presses` ok.
  - **Expected today:** both capHeight assertions fail, with measured px close to R4's model. Record them.
  - Stills: `stills/doclook_a1_reading_1920x1080.png` and `doclook_a2_reading_1280x720.png`.
  - Repeat on days 5 and 12 (add `{ "op": "jumpDay", "n": 5 }` after `waitOffice`), and in `el` and an Arabic culture once TEXT-LANG has merged.
- **R6 Human read.** Hand `doclook_a2_reading_1280x720.png` to the adversarial reviewer at 100% with no zoom. **Pass:** every field of the day-1 passport is read aloud and quoted back correctly. Expected to fail until the decision.
- **R7 Print region, folder glitches, open and close** (BUG-10b, 11b, 12a, 12c; D3).
  - Steps: `QaJobs.Scenario` with `doclook_print`. It now starts with:
    - `folder_open` (click the shut folder);
    - `folder_close` (click `FolderCoverClick`);
    - `folder_reopen`;
    - `passport_press_open` (click the shut passport on the counter).
  - Then, for each of `seq/print_a1` and `seq/print_a2`, run:
    - `python tools/qa/seqtools.py flicker <seq> --static`
    - `python tools/qa/seqtools.py sheet <seq> <out>.png`
    - `python tools/qa/seqtools.py gif <seq> <out>.gif`
  - **Pass:**
    - flicker PASS (0 px toggling more than 2x in 10 s while nothing tracked moves);
    - on the frame sheet: 0 frames where a rule line, box or band vanishes under the curl, including mid-span during `passport_fast` and `folder_wobble`;
    - 0 frames with a strip on the folder's left edge;
    - the tabs and their words lie on the board;
    - during the page-turn flick, every rule row stays on its ruled line and above the sheet;
    - the folder arrives shut: the right half shows the manila board with the seal, with only the GUIDE and SEALS tabs, and no strip or floating word where the left half was;
    - `folder_open` swings the board over to the left with a small rebound and no frame through the desk, and the rows appear when it lies open;
    - `folder_close` swings it back over the rows;
    - `passport_press_open`: the cover lifts on the press frame (±1 frame of the press in `drags.tsv`/the sequence), swings open with a visible page bow and rebounds without passing under the desk, and stays open on the counter until the drag;
    - the cover's emblem and the folder's seal stay on their boards through every swing: no edge cut by the bowing board (wave 2, point 8);
    - the page bow eases through flat on a turn back (put the passport back on the counter within 200 ms of a pick-up): no one-frame pop (wave 2, point 6);
    - the reviewer, comparing against a Papers, Please clip with `seqtools side`, answers "does it read as paper?" yes.
- **R8 Hover** (BUG-10a, D15).
  - Steps: `QaJobs.Scenario` with `doclook_hover`. Make GIFs and sheets of `hover_a1`, `hover_a2` and `hover_held`.
  - **Pass:**
    - papers and the folder (open or shut, its cover page too) lift with the hand cursor and show no outline;
    - every prop's rim is an unbroken band of about 2 px that hugs its silhouette at A1 and A2;
    - no rim shows in a close-up or on any camera other than the office's;
    - 0 rim or lift frames in `hover_held` while the paper is carried.
- **R9 Stamp right after a drop** (BUG-12b, STAMP's aiming recipe at #7).
  - Steps: in STAMP's dater scenario, put the passport down with a `drag`, then within 15 frames (0.5 s) `drag` a dater onto an aimed point on it. Do this 10 times.
  - **Pass:** 10/10 impressions within 4 px of the aimed point with their tilt kept, 0 z-fighting frames in the recorded sequence, and 0 frames where the face is bent while its print lies flat.
- **R10 Mipmap crawl [BP]** (BUG-63).
  - Steps: `QaJobs.BuildQaPlayer`, then `run_player.py` on `doclook_print.json`, then `seqtools flicker bp...print_a1 --static`.
  - **Pass:** no crawl on the paper and folder edges (≤ the A-B-A threshold). Run only when Saleh is away from the PC.
- **R11 Contact shadow** (BUG-40c), after DOC-HANDLE raises `StackChanged`.
  - Steps: DOC-HANDLE's stack-order stills (3 papers in 10 random orders at A2).
  - **Pass:** the reviewer names the top paper correctly 10/10.
- **R12 Photo include** (BUG-14c). In the frame debugger, DocumentCel binds `_DeskLightTint` and `_DeskLightLevel`. The match itself is measured at LIGHT's merge.
- **R13 Notes re-check.** Saleh's notes 25 (tiny font), 7, 8, 9 and 3 go in NOTES_RECHECK with R5-R8 evidence. Note 25 stays open until FORMS-REDESIGN lands (decision 2).
