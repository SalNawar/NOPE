# The desk: inspect, stamps, scanner, city view (desk-first track B, 2026-10-05)

> **Superseded in part (2026-10-06):** Papers, Please's controls (`2026-10-06-controls-design.md`) replaced DB3's and DB6's physical stamps, ink pad and hand-back buttons with the stamp bar, the papers in the hand (DB6's reading lift) with the counter and the desk zones, and made inspection a mode (the red button, SPACE). DB1, DB2's evidence model, DB4 and DB5 stand.

Saleh's items (2026-10-05): (4) "scanner: sometimes things get stuck behind it"; (5) "the tilt on the desk zooms more and the tilt is 80 degrees; players can match there after they scan"; (6) "player can look left for a view at the city"; (11) "the player should be able to check documents at the desk, highlight clear mistakes and approve; scanning allows using additional features"; (12) "approve or reject are actual physical seals: the player picks stamps, inks them, then stamps on the document". Saleh was asleep: every decision below is the default, his to overturn. The behaviour contract is `docs/FEATURES.md` (Desk: Physical stamps, Inspection at the desk, The city view, Desk view, Desk scanner).

## DB1 The desk view (item 5)

`DeskViewTuning` is an absolute pose: pitched 80° below the horizon, 0.62 m back from an aim point 0.06 m beyond the mat's centre, a 50° lens, a 0.5 s eased blend (`DeskViewPose`). 0.62 m was chosen from the screenshots: a passport lying on the desk reads at 1280×720; the papers' spawn slots, the rulebook and the stamp tray stay inside the frame. All knobs are on `DeskConfigSO.deskView`.

## DB2 Inspection at the desk (item 11)

- One evidence model: a lying paper's box clicked in the desk view goes into the same compare and the same `MatchBoard` as the PC, so its findings are the PC's findings and count for the decision.
- Held against: another paper's box, the desk calendar (`Feature.Calendar`), the traveller's face (a click on the traveller while a value is held; otherwise the wheel opens as before), a row of the rulebook card on the desk (`Feature.Rulebook`). The clock holds nothing: A cut the departure times, so the date is the calendar's.
- The line is the PC's `MatchLines` drawn on the office overlay between two ends placed each frame over the values' places on the screen; an end off the screen waits at the screen's edge toward it.
- "Highlight clear mistakes": a logged difference (a value against another, a rule broken, a date that fails) marks its papers' boxes in a translucent red for the case (`FindingMarks`). Matches and notes mark nothing. Nothing is marked by itself.
- "Scanning allows additional features": the desk inspection needs no scan. A paper's copy reaches the PC only when scanned (from the scanner's day 5), with its search entries and links (records, account). A's stopgap (the copy at the hand-over before day 5) is removed, as A asked.

## DB3 Physical stamps (item 12)

- The desk stamp slides a tray out (APPROVED, DENIED, the ink pad; code-drawn stand-ins). A stamp picked up tilts the view down and follows the pointer; the pad inks it for `stampPressesPerInking` presses (1); a dry press leaves a faint ghost and counts for nothing.
- The verdict is the passport's (the traveller's first paper handed over: the passport, or a displaced person's travel permit): its last inked press. Pressing the other stamp later replaces the verdict (a correction; both marks stay). Marks on other papers count for nothing.
- "Hand the papers back" (top right, with the verdict's tick or cross) or the stamped passport dropped on the traveller's side of the desk decides the case through the one decision path, so A's rule holds: deny freely, a denial with no logged evidence earns the one citation. The leaving papers keep the player's marks.
- The old Accept/Deny overlay tray is deleted. The PC's decision step (Accept/Deny on the workbench) is left as it is (track C's PC).
- A stamp in the hand is a tool, not a modal: the papers on the desk take the press; the PC, the props, the traveller and the mat wait (`BoothRules.StampHeld`, `StampsLive`).

## DB4 The scanner (item 4)

`ScannerClearance`: the scanner blocks its footprint and the shadow its body casts away from the office camera (height over the tangent of the camera's elevation, at most 0.35 m). A paper dropped, landing, refused or back from a scan into it moves the shortest way out (left, right or front, never further back), kept on the desk; else the eject spot in front of the scanner (else beside it).

## DB5 The city view (item 6)

A or the left arrow, or "< City (A)" on the left edge, turns the view 75° left and 6° up to a window over the city; D, the right arrow, Escape or "Desk (D) >" turn back. The city is the art's painting (`Assets/Art/UI/Resources/City/city_view.png`, by name) or a code-drawn stand-in (sky, three layers of seeded towers, lit windows, a window frame), tinted day to evening along the hall's evening curve, shown only while looked at. The art request is in `docs/ART_ASSET_LIST.md`, "The desk". Not gated by the ramp (it is a view, not a check).

## Gating

Stamps and desk inspection from day 1 (the verdict and the passport checks are day 1's). The calendar and the rulebook read `Introductions.Has(day, Feature.Calendar / Feature.Rulebook)` (both day 1 today). The scanner is A's (`ScannerDay.Hidden` until day 5).


## DB6 The desk polish (2026-10-05, Saleh asleep: every choice the default, his to overturn)

From the screenshots of DB1-DB5: the stamps read as blocks with spheres, the passport was small in the tilted view while the rulebook card took the left of it, the match banner repeated the line's label, and handing back was a button.

- **Stamps.** Built from the art's own desk stamp and ink pad (DeskClean `Clean_Stamp` at 0.5, `Clean_Inkpad` at 0.42, in the DeskClean NOPE/Desk Anime materials), so they match the desk exactly: a wooden handle, brass ferrule, wooden block and dark rubber die; a green or red cap on the knob tells them apart; the word on the block's front (the office view) and in reverse under the rubber (it prints the right way); an ink rim round the die and the inked word while inked. The pad keeps the art's open lid and gets a two-colour felt (green, red: one pad, two inks). The rack is DeskClean green-dark with wooden lips. Without the art's models the builder falls back to primitives in the same materials. Dedicated models are requested in `docs/ART_ASSET_LIST.md`, "The desk". Chosen over a code-built lathe mesh: the art's model is the desk's look already and costs nothing.
- **Reading at the desk.** A tilt into the desk view by the player (the mat, the mouse wheel, the PC's "< Desk"; not a stamp picked up) with no paper in the hand lifts the paper read last this case, else the passport (`DeskController.ReadFocus`), and in the desk view a paper in the hand stands at 0.74 of the screen's height, centred a little low, two side by side (`ExamineLayout.DeskSlot`, knobs `ExamineTuning.deskHeight`, `deskCentreY`): 533 px tall at 720p. Lift rather than zoom: the camera keeps DB1's pose, so the stamps, the rulebook and the lying papers stay where the player left them, and every box of the lifted paper is picked where it is (held papers already took picks). A stamp picked up puts the held papers back down to be stamped.
- **Rulebook.** Tucked at half size toward the mat's outer near corner until clicked; a click on the open card off its rows tucks it again; each day starts tucked. `rulebookAt` moved 5 cm forward so the open card stays in the desk view. Under the rules, the card lists the papers the traveller has not handed over (Track C's `MissingPapers`, the PC's Papers menu's list): a click flags one missing (`InvestigationUIController.FlagMissing`, so the wheel offers "Hand me your ..."); a flagged or not-carried paper says so.
- **The match banner.** The office compare strip is 760 px wide at 18 pt and prints the verdict and the two values ("MATCH · 2 Mar 2150 vs 14 MAR 2150"); while the desk's line labels the pair it hides (the line's label plate and the findings say it); a DEVIATION LOGGED note still shows. An end of the line off the screen (the calendar, the traveller's face from the desk view) waits at the screen's edge in the value's direction as the camera sees it, with a small arrow plate pointing the way.
- **Hand back.** Once the passport carries a verdict an ivory strip marked "▲ HAND BACK ▲" shows on the traveller's side of the desk (`handBackDepth` deep, ending where the desk view shows the desk at 90 % of the screen's height, so it is always reachable in the desk view); sliding the stamped passport onto it hands the papers back (the existing drop rule). The hint says so; "Hand the papers back" stays, smaller, as the secondary way.
- **The tray keeps the desk clear.** Out, the tray moves papers off its footprint as the scanner does (`ScannerClearance` reused with no shadow), left or right only (nearer would leave the desk view).
