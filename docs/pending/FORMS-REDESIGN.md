# FORMS-REDESIGN: the eight art forms read at the big mat (hand-off from DOC-LOOK)

**Status:** a new track, not started. Written by DOC-LOOK (#9) on `fix/a-doc-look` as the hand-off the orchestrator asked for on 2026-10-20.

**Why it exists.** The orchestrator's decision of 2026-10-20 on BUG-9 (Saleh's note 25, "tiny font"):
- DOC-LOOK keeps its interim: the fit floor stays at 0.5 of the art's size, and labels grow x1.3.
- A new track, FORMS-REDESIGN, redesigns the 8 art forms with larger places, so the data reaches the 10/7 px floor at the big mat's reading view.
- `doclook.forms.capModel` stays red at merge level until then. INTEGRATION adds the known-red line at merge, quoting this decision.
- **Legibility is a quality bar, not optional: the redesign is required before any demo.**

**What "done" means.** Every field of every art form reads at the floor at the big mat, with every cross-checked value printed whole (never cut, never broken mid-word):
- `doclook.forms.capModel` is green at merge level, and the known-red line is removed.
- The `doclook_cap` scenario passes at A1 and A2, on days 1, 5 and 12 (DOC-LOOK's recipe R5).
- The adversarial reviewer reads every field of the day-1 passport aloud from the A2 still at 100% (R6).
- `Validate Content Library` reports no `ArtLayout.CutWords` or `ArtLayout.Problems` line.

## 1. The floor in pixels

These numbers come from the gate's own model (`DocLookChecks.CapModel`, `CapHeight`), which is measured at the big mat's reading view (`ModelPixelsPerMetre` = 1041 x 0.67) at each form's desk size.

- **Data:** at least 10 px cap at A1 (1080 lines) and 7 px at A2 (720 lines). A2 is 2/3 of A1, so the A2 floor is the one that binds: **10.5 px cap at A1.**
  - In the value face (Geist Mono: cap 0.71 em, advance 0.6 em), that is a 14.7 px em.
  - Each character is **8.8 px** wide, and a line is about **18 px** tall (at 1.2 em).
- **Labels:** at least 6 px cap at A2, which is 9 px at A1. A label line is about **13 px** tall.
- **Whole values:**
  - The paper never breaks a word across lines, and it never cuts a value (`TextFit`, `TmpFormText.ArtFit`).
  - The validator proves this for the content's names and origins (`ArtLayout.CutWords`) and for every other value's probe (`ArtLayout.Problems`, word check).
  - A value may wrap onto more lines at word breaks, if its place has the room for those lines.

## 2. Growth still needed (today's art at the big mat)

The smallest data on each form, with DOC-LOOK's interim (labels x1.3, values x1.5). Each cell shows data at A1 / A2. The growth is the gate's own (`CapHeight.Needed`: the larger of 10 px over the A1 figure and 7 px over the A2 one).

| Form | Data now | Labels at A2 | Growth to the floor |
|---|---|---|---|
| TC-101 passport | 4.8 / 3.2 px | 1.6 px | x2.2 |
| TC-230 entry ticket | 5.8 / 3.9 | 3.4 | x1.8 |
| TC-240 transponder card | 5.6 / 3.7 | 2.8 | x1.9 |
| TC-310 stranding waiver | 2.6 / 1.7 | 1.6 | x4.1 |
| TC-520 work permit | 5.5 / 3.6 | 2.9 | x1.9 |
| TC-610 travel permit | 4.3 / 2.9 | 2.8 | x2.4 |
| TC-620 intake declaration | 5.3 / 3.5 | 2.5 | x2.0 |
| TC-630 return order | 5.2 / 3.5 | 2.7 | x2.0 |

**Field by field.** The columns below are:
- **Now:** data cap px at A1 / A2.
- **Growth:** how much larger that field's data must print.
- **Place now:** the value place's width on screen at A1.
- **Longest:** the longest value in characters, with its longest word in brackets. These come from `FieldLengths.Longest` and the content, measured 2026-10-20.
- **Needs at the floor:** that value's width on one line at 8.8 px a character.

| Form | Field | Now | Growth | Place now | Longest | Needs at the floor |
|---|---|---|---|---|---|---|
| TC-101 | Full Name | 4.9 / 3.3 | x2.1 | 75 px | 23 (15) | 203 px |
| | Citizen ID | 5.0 / 3.4 | x2.1 | 33 px | 7 | 62 px |
| | Date of Birth | 5.0 / 3.4 | x2.1 | 39 px | 15 ("28 Jan 9999 BCE") | 132 px |
| | Destination | 4.8 / 3.2 | x2.2 | 39 px | 43 (13) | 379 px |
| | Travel Class | 5.5 / 3.7 | x1.9 | 33 px | 8 | 71 px |
| | Valid Until | 5.5 / 3.7 | x1.9 | 39 px | 11 | 97 px |
| TC-230 | Citizen ID | 5.8 / 3.9 | x1.8 | 73 px | 7 | 62 px |
| | Transponder | 5.8 / 3.9 | x1.8 | 73 px | 26 (9) | 229 px |
| | Transponder Class | 5.8 / 3.9 | x1.8 | 73 px | 7 | 62 px |
| | Currency Carried | 5.8 / 3.9 | x1.8 | 73 px | 28 (13) | 247 px |
| | Declared Effects | 5.8 / 3.9 | x1.8 | 73 px | 27 (13) | 238 px |
| | Departure | 5.8 / 3.9 | x1.8 | 73 px | 11 | 97 px |
| TC-240 | Citizen ID | 5.6 / 3.7 | x1.9 | 91 px | 7 | 62 px |
| | Transponder | 5.7 / 3.8 | x1.8 | 51 px | 26 (9) | 229 px |
| | Transponder Class | 5.6 / 3.7 | x1.9 | 22 px | 7 | 62 px |
| TC-310 | Signatory | 3.9 / 2.6 | x2.7 | 97 px | 23 (15) | 203 px |
| | Citizen ID | 3.9 / 2.6 | x2.7 | 97 px | 7 | 62 px |
| | Transponder | 3.9 / 2.6 | x2.7 | 97 px | 26 (9) | 229 px |
| | Debt Passed to Kin | 4.0 / 2.7 | x2.6 | 97 px | 12 | 106 px |
| | **Waiver No.** | **2.6 / 1.7** | **x4.1** | **41 px** | 7 | 62 px |
| | Signature (hand) | 5.4 / 3.6 | x2.0 | 53 px | 23 (15) | 203 px |
| TC-520 | Worker | 6.2 / 4.2 | x1.7 | 104 px | 23 (15) | 203 px |
| | Citizen ID | 6.3 / 4.2 | x1.7 | 104 px | 7 | 62 px |
| | Employer | 6.3 / 4.2 | x1.7 | 104 px | 25 (9) | 220 px |
| | Worksite | 6.4 / 4.2 | x1.6 | 98 px | 43 (13) | 379 px |
| | Term | 5.5 / 3.6 | x1.9 | 104 px | 8 | 71 px |
| | Day Wage | 5.5 / 3.7 | x1.9 | 104 px | 12 | 106 px |
| TC-610 | Full Name | 4.3 / 2.9 | x2.4 | 64 px | 23 (15) | 203 px |
| | Displacement No. | 5.8 / 3.8 | x1.8 | 55 px | 7 | 62 px |
| | Date of Birth | 6.2 / 4.1 | x1.7 | 45 px | 15 | 132 px |
| | Origin | 5.8 / 3.8 | x1.8 | 53 px | 43 (13) | 379 px |
| | Incident | 5.8 / 3.8 | x1.8 | 61 px | 9 | 79 px |
| | Valid Until | 5.8 / 3.8 | x1.8 | 45 px | 11 | 97 px |
| TC-620 | Declarant | 5.3 / 3.5 | x2.0 | 90 px | 23 (15) | 203 px |
| | Displacement No. | 5.3 / 3.5 | x2.0 | 90 px | 7 | 62 px |
| | Coin of Home | 5.3 / 3.5 | x2.0 | 90 px | 28 (13) | 247 px |
| | Native Tongue | 5.3 / 3.5 | x2.0 | 90 px | 28 (14) | 247 px |
| | Effects Carried | 5.3 / 3.5 | x2.0 | 90 px | 27 (13) | 238 px |
| TC-630 | Returnee | 5.3 / 3.5 | x2.0 | 95 px | 23 (15) | 203 px |
| | Displacement No. | 5.4 / 3.6 | x2.0 | 95 px | 7 | 62 px |
| | Return To | 5.2 / 3.5 | x2.0 | 95 px | 43 (13) | 379 px |
| | Incident | 5.3 / 3.5 | x2.0 | 95 px | 9 | 79 px |
| | Departure | 5.5 / 3.6 | x1.9 | 95 px | 11 | 97 px |

The longest values, for reference:
- names: "Michelangelo Buonarroti" (23, a premade), "Giovanfrancesco" (15, the longest drawn word);
- origins: "Ottoman Iraq (Baghdad Vilayet) (Industrial)" (43);
- facts: "Iron ingots & cloth (barter)" and "Middle Chinese and Mongolian" (28);
- transponders: "Tick-Tock Basic · TT-99999" (26);
- employers: "Venetian Arsenal Holdings" (25);
- dates: "28 Sep 2150" (11), and a birth date BCE "28 Jan 9999 BCE" (15);
- numbers: "ZZZ-999" and "DP-9999" (7), "9,999,999 cr" (12), "720 days" (8), "R-0928-99" (9).

## 3. Why re-laying the boxes on today's art is not enough

Today's art spends most of each page on titles, emblems, legal text and seals:
- TC-310's title and emblem take about 45% of the page, and its data table about 20%.
- TC-230's title band takes about 30%.
- The passport repeats the Bureau's header on both pages.

The label columns also take 25% to 45% of each row's width.

Even with an ideal re-draw, a model of the pages says most forms do not fit at today's desk size:
- **The re-draw assumed:**
  - a header of 12% of the page;
  - 5% margins;
  - labels stacked over their values (a band, so each value gets the row's full width);
  - two short values sharing a row when both fit half the width;
  - room kept for the photo, the stamp box, the seal and the signature.
- **The figures:** the text each form needs at the floor, against the room it has, and the least desk size at which it fits (x today's `look.scale`).

| Form | Text at the floor | Room on the page | Fits at |
|---|---|---|---|
| TC-101 passport (both pages, photo, visa box kept) | 299 px | 126 px | about x1.4 to x1.7 |
| TC-230 | 246 px | 157 px | x1.40 |
| TC-240 | 88 px | 138 px | x1.00 (fits today) |
| TC-310 | 228 px | 168 px | x1.30 |
| TC-520 | 246 px | 160 px | x1.35 |
| TC-610 | 299 px | 173 px | x1.50 |
| TC-620 | 264 px | 173 px | x1.35 |
| TC-630 | 228 px | 172 px | x1.15 |

This is a planning model, not a measure (the scripts are in DOC-LOOK's scratchpad, `doclook3/`). FORMS-REDESIGN confirms it with `doclook.forms.capModel` on the real art.

**So the redesign is two levers, used together:**
1. **New art that gives the page to the data** (section 4).
2. **Larger papers on the mat** where the art alone cannot reach the floor (section 5).

If Saleh would rather not enlarge a paper, the remaining lever is fewer fields on it. That changes what the player can cross-check, so it is his call (section 6). It is not this track's.

## 4. The art: what every form changes

These are for the ChatGPT/Canva art job (the FIX_PLAN Risks foresaw it: "Art re-bakes of labels may need ChatGPT").

- **Data first.** The data area takes at least 60% of the page; today it is 20% to 45%.
- **Titles.**
  - The title and the Bureau's header together take no more than about 12% of the page height.
  - The big emblems and medallions shrink to a corner mark.
  - The legal paragraphs (TC-310) go, or shrink to one line of small print. They are not data, and they are not on the cap gate's list.
- **Labels stacked over values**, as bands at the top of each cell, never in a left column. Each value then has the cell's full width.
- **Value places sized from the table in section 2.**
  - A place is as wide as its longest value needs at 8.8 px a character, or as tall as the lines that value wraps to at the place's width.
  - A place for an origin (43 characters) holds 2 or 3 lines.
  - A name place holds 2 lines (a premade's two-word name).
  - Short values (IDs, classes, dates, terms, wages, incident numbers) may share a row, two to a row, when both fit half the width.
- **Clean faces only.** No label baked into the art: `printLabels` prints every label, in the art's ink and in the reading language. Every label's place is a clear band at least 13 px tall at A1.
- **Keep what the player uses:** the photo window, the visa or stamp box (where one exists), the seal circle, the signature line, the chip (cards), the rivets and spine (the folder and booklets), and the security marks (UV patches, microprint) where TOOLS placed them.
- **The paper's look stays:** each form keeps its colour, its frame and its family. Only the proportions change.

## 5. Per form: what to do

Each form needs: new art (clean face), new `ArtField` places in its `DocTemplate_*.asset`, and a new `look.scale` where section 3 says so.

- **TC-101 passport** (booklet; each page 202 x 154 px at A1). The hardest: six text fields, a photo and the visa box on a passport-sized booklet.
  - **Move:** identity on the data page, travel facts on the visa page.
    - The data page keeps the photo, Full Name, Citizen ID and Date of Birth.
    - The visa page takes Destination (Era), Travel Class and Valid Until (the visa's own facts), above a smaller entry-visa box.
    - The shut booklet shows the data page only, so the identity reads on the counter and the travel facts on the desk, where the player reads them.
  - **Header:** once, on the visa page, as a thin band. The data page loses its copy of the Bureau's header.
  - **Size:** about x1.4 with a visa box of 30% of its page, up to x1.7 with today's visa box. Agree the booklet's size on the mat with DESK-PLAN (section 7).
- **TC-230 entry ticket** (245 x 189 px).
  - **Move:** Departure into the stub's DATE box. The stub's GATE box is not a field today, so leave it empty or drop it.
  - **Rows:** Citizen ID and Transponder Class share a row. Transponder, Currency Carried and Declared Effects each get a full-width band of 2 lines.
  - **Title:** the band shrinks from about 30% to 12%.
  - **Size:** x1.4.
- **TC-240 transponder card** (290 x 166 px).
  - Fits today with bands across the right two thirds. Transponder (26) is one line at the full text width.
  - Transponder Class gets a real place (22 px today, the card's second-smallest).
  - The title shrinks.
  - **Size:** today's.
- **TC-310 stranding waiver** (178 x 237 px). The worst field anywhere: Waiver No. at 2.6 px, in the title band's small box.
  - **Move:** Waiver No. into the data table as a full field.
  - **Drop the art's decoration:** the legal paragraphs go to one line or go entirely (not data), and the emblem and the title shrink to a header.
  - **Rows:** Signatory (2 lines), Citizen ID and Waiver No. on one row, Transponder (2 lines), Debt Passed to Kin, and the Signature line, which is wide enough for a two-word name in the hand face.
  - **Size:** x1.3.
- **TC-520 work permit** (181 x 235 px).
  - **Drop:** the emblem and the long title shrink to a header.
  - **Rows:** Term and Day Wage share a row. Worker (2 lines), Employer (2 lines) and Worksite (3 lines) are full width.
  - **Size:** x1.35. The seal circle and "authorised by" stay, smaller.
- **TC-610 travel permit** (a two-page permit, 227 x 209 px).
  - **Move:** the text spreads over both pages under the photo.
    - The left page has the photo, Full Name and Date of Birth.
    - The right page has Displacement No. and Incident on one row, then Origin (2 lines) and Valid Until.
  - **Drop:** the big title shrinks; the right page's medallion goes.
  - **Size:** x1.5. Without the move, it needs more.
- **TC-620 intake declaration** (178 x 237 px).
  - **Rows:** five fields as full-width bands. Coin of Home, Native Tongue and Effects Carried are 2 lines each (Effects can need 3 at today's width).
  - **Size:** x1.35.
- **TC-630 return order** (181 x 235 px).
  - **Rows:** Incident and Departure share a row. Returnee (2 lines) and Return To (3 lines) are full width.
  - **Size:** x1.15.

## 6. Keeping every cross-checked value whole

- **Every text field above is cross-checked.** It is a `DocumentTemplateSO.fieldSpecs` category that an honest traveller carries the same on every form, answer and record row, and that a liar or a forger can change.
- **Cutting a value is never allowed.** A cut value reads as a false discrepancy against another paper.
- **What this plan does instead:**
  - moves fields (TC-101, TC-230, TC-310, TC-610);
  - pairs short values on one row;
  - wraps long values onto 2 or 3 lines at word breaks.
- **The plan merges no two values into one field.** A merged place would hide which value a discrepancy is on.
- **The plan drops no field.** Dropping one changes what the player can catch, so it needs Saleh. If he prefers fewer fields to larger papers, these are the candidates (each removes a cross-check, and the same value can still be caught elsewhere):
  - TC-230's Declared Effects (also on TC-620 for the displaced, and in the interview);
  - TC-101's Travel Class (also on the Citizen Account);
  - TC-610's Date of Birth (also on the passport and the record).

## 7. What the track needs from others, and what it owns

- **Owns (proposed):**
  - the art slots `Assets/Art/Forms/` for the 8 forms (`ArtSlots.Paper*`, the clean faces);
  - the 8 `DocTemplate_TC*.asset` (their `look.scale`, `ArtField` places, `ArtCaption`s and `ArtPrint`s);
  - `ArtLayout.cs`, for the one change below.
- **Ownership:** these are DOC-LOOK's files today. DOC-LOOK hands them over once it has merged, so the orchestrator moves them in LEASES.md.
- **Code needed: a multi-line value place.**
  - Today a value never grows past one line of its place (`ArtLayout.ValueFill` 0.74).
  - A place drawn for 2 or 3 lines needs an `ArtField.lines` (default 1). The value's size then comes from the place's height over its lines, and `ArtFit` wraps it there.
  - It needs tests in `FormLayoutTests` (one line by default; 2 lines fit a 43-character origin at the floor).
- **Then retire DOC-LOOK's interim:**
  - `valueGrowth` and `labelGrowth` go back to 1 (the art sets the sizes);
  - the fit floor rises from `TextFit.Least` 0.5 toward the plan's 0.85, now that each place holds its longest value whole;
  - `doclook.forms.regrown` is re-pointed or retired, since it measures growth over the old art.
- **DESK-PLAN (mat packing) and CAM (reading view):**
  - A larger paper changes how the mat packs (DESK-PLAN's homes and slide-ins) and what the reading view frames.
  - The new `look.scale` per form is agreed with both before the art is drawn, because the art's aspect and its places depend on it.
- **TOOLS:** the UV patches and microprint bands (`UvPaperLayer`, `MicroPaperLayer`) are placed by slot. Check they still land on their fields after the re-layout.
- **TEXT-LANG:** Greek and Arabic labels and values in the new places (the cap gate covers every culture, FIX_PLAN).
- **Verification:**
  - DOC-LOOK's recipes R4 (`doclook.forms.capModel` green), R5 (`doclook_cap`, A1 and A2, days 1, 5 and 12), R6 (human read) and R1 (validator: no cut words, no `Problems` line);
  - a frame sheet of each form at A2 for the reviewer.
