# SWITCHBOARD: the playable portal switchboard (LOOP_v5 on the v3 board)

Branch `feat/switchboard`, cut from `feat/desk-v3` (`adcdf6a`, the painted desk). Never pushed. Written overnight on 2026-10-21 while Saleh slept ("I expect everything fixed by the time I wake up, with a switchboard demo"): every call below that is not Saleh's is marked **ORCHESTRATOR** (decided, can be changed) or **PROPOSAL** (needs his yes).

Sources: `E:\unity\NOPE-tools\design\switchboard_v4\LOOP_v5.md` (the loop), the v3 board he locked (`design\desk\pipeline\paint\switch_full_v3_reg.png`), the board kit (`design\switchboard_v4\board\`: plate, `board_layout.json`, sprites), the portals spec (`run7\PHASEB\PORTALS\SPEC.md` rev 6 and `design\portals\PORTALS_SPEC.md` section 1), `docs/pending/CARVE.md`.

## What the player does

1. **A call comes in.** Approving a traveller at the desk (the stamped passport handed back, as today) rings their destination on the board: its call lamp blinks amber with a soft buzz heard from the desk, and in the office view the painted board's top lamp glows. The longer they wait, the faster it blinks.
2. **Turn to the board.** Click the painted board on the right wall, or press D (the right arrow) or S. The board comes up from the wall and fills the screen. A, S, a right-click or Esc turns back.
3. **Answer it.** Drag a free module's PLACE plug into the destination's jack and its PERIOD plug into the right period. Make sure one of the two power cords is in that module (move it from the power row or from another module). Pull MASTER down past 80 % and let go: clunk.
4. **Wait 20 s.** The module's display counts down (only while powered), then the group leaves: chime, flare, "+N SENT", and a small group bonus.
5. **Clear it or keep it.** The finished connection stays plugged (DONE lamp). The next call on that route leaves at once. Pull its plugs to free the module for another route.

A wrong period (another destination's period jack, or a period nobody at that destination is calling for) pops the module's fuse: sparks, FUSE for 2 s, then fix and pull again. Time only.

## Saleh's words this rests on (SALEH)

- "one cable for power one cable for destination have to be connected to the portal one cable for the period"; "it should be one breaker to the side"; "make it scalable in case I want to add more eras" (the v3 board, LOCKED 2026-10-20).
- "the power you will never move it you just connect it and that's it what's the point of it" (so power became the scarce runner: 2 power cords for 5 modules).
- "20s to calibrate", calibration in real seconds; the fuse pop costs time only; "if many people go at once maybe you get a bonus payout".
- "a minigame that should take seconds to figure out" (2026-10-21).
- The view: Esc opens Settings everywhere (R3/R4); here Esc backs out exactly as it does from the city view, which is what the office does today.

## Decisions

### From the art pass (the board kit's report)

1. **ORCHESTRATOR/PROPOSAL:** every destination has 3 period jacks, so the grid is a uniform 4 x 3 (v3 had Rome with 3, the others with 2). Unused jacks stay blank and take no plug.
2. **ORCHESTRATOR/PROPOSAL:** MASTER is a red handle on two copper rods, at rest at the top; pulling brings it down toward the rods' pivots (v3 had a knife switch with copper blades). The game draws the lever over the lever-free plate.
3. **ORCHESTRATOR:** the LOCKED placard on 04 and OUT OF SERVICE on 05 are painted into the plate; code covers nothing yet. If those modules come into service, the plate needs a placard-free variant or a cover.
4. **ORCHESTRATOR:** place plugs come in fixed colours per module (01 blue, 02 green, 03 pink, 04 brass, 05 the white plug for tinting). The period plugs are brass; the power plugs red. Both of a module's cords wear its colour, so you can follow a module's cords across the board.
5. **ORCHESTRATOR:** plugs are full size on destination and power-row jacks, 0.85 in module sockets, 1.12 in the hand; lamps at the art's sizes (call 1, module 0.875, MASTER 1.42).

### The loop (rules: `Switchboard`, pure, tested)

6. **SALEH:** calibration is 20 real seconds; the fuse pop costs time only; the group bonus exists and is small.
7. **ORCHESTRATOR:** the bonus is 5 credits per extra traveller in one group (the portals spec's 5(k-1)). It goes to the wallet and to the shift's other money (`ShiftLedger.otherMoney`), so the report's net counts it.
8. **ORCHESTRATOR:** MASTER locks in every patched module at once (one breaker, Saleh's). A module patched and waiting blinks the MASTER lamp and shows PULL.
9. **ORCHESTRATOR:** what counts as a "wrong period": the period jack belongs to another destination, or the destination has calls waiting and none is on this period. A route to a destination nobody calls yet locks in (opening a route ahead is allowed, LOOP_v5's "leave the route open").
10. **ORCHESTRATOR:** calibration counts only while a power cord is in the module, and pauses (keeping its progress) when the cord is pulled. An open route sends only while powered. So moving the 2 power cords is the run of the loop (LOOP_v5's revised power).
11. **ORCHESTRATOR:** a jack takes one plug. Two periods of one destination are answered one after the other (or by clearing and re-patching), never by two modules at once.
12. **ORCHESTRATOR:** pulling any cord of a locked-in module unlocks its route (calibration starts over). Pulling the power cord only pauses it.
13. **ORCHESTRATOR:** a plug in the hand does not change the rules: they change only when it lands. A right-click or Esc while holding puts it back with nothing changed.
14. **ORCHESTRATOR:** calls blink from 1.2 to 4 blinks a second over 90 s of waiting; the soft buzz repeats every 25 s while a call waits unanswered. There is no cost for waiting yet (the portals spec's waiting cost and human-rights fine are later stages).
15. **ORCHESTRATOR:** calls left waiting at closing time are simply logged; nothing carries to tomorrow, and every morning starts with a fresh board (no cables kept overnight).
16. **ORCHESTRATOR:** nothing on the board changes a verdict, the stability or the history. The hall's ring pulse still fires at approval, as today (`GameManager.Departed`, FLOW's); moving it to the send is a follow-up for FLOW and PORTALS.

### The board's words (`SwitchboardDirectory`, pure, tested)

17. **ORCHESTRATOR:** the 4 name plates show, in order: the day's open destinations (the OpenDestinations rule), then the destinations of the day's portal routes, then today's other places (book order). Each destination's periods are the eras of today's places there (up to 3), oldest first. Two periods with the same name (Egypt's "Ancient" and "Ancient" again) print their years instead.
18. **ORCHESTRATOR:** a call for a place that is not printed takes its nation's next free period jack, or the next free name plate, without moving anything already printed. With no free jack the traveller leaves at once (logged).
19. **PROPOSAL:** the board never shows which period a call wants: the player knows it from the traveller just approved (Saleh: picking a period should take thought). If that proves too hard, a small tally beside the call lamp is the cheap fix.

### The ramp

20. **ORCHESTRATOR:** module 01 from day 1, already powered by power cord 1 every morning (LOOP_v5 day 1: "one free module already powered: plug and pull"); 02 from day 3; 03 from day 5 (LOOP_v5 day 5: "power is short": 3 modules, 2 cords). 04 and 05 stay placarded. A module not yet open wears a dark veil and CLOSED. The days are a knob (`SwitchboardConfigSO.moduleFromDay`). The PORTALS spec's own ramp (02 from day 3, the Return Gate by repair) and its teaching table (PP-STUDY) should replace this when they land.
21. **SALEH (the task):** day 1 hangs an in-world hint card on the board: "A lamp blinks: plug a module to it, give it power, pull MASTER". **ORCHESTRATOR:** it hangs on the wood left of the board, day 1 only (`hintUntilDay`).

### The view

22. **ORCHESTRATOR:** D (and the right arrow) turns to the board as A turns to the city: the right wall and the left wall. S toggles it both ways. A or S, a right-click and Esc turn back (Esc backs out as it does from the city view). In ControlRules' back-out order the board comes after inspect mode and before the city. SPACE, TAB, Q and F1 close the board first, as they close the city.
23. **ORCHESTRATOR:** the "camera ease" is the board's plate growing out of the painted board (squeezed as the wall is seen at an angle) to fill the screen over 0.45 s, the matte darkening behind. The office camera itself does not move (it is CAM's). At 16:9 the plate fills the screen exactly; on other shapes it fits whole, with the dark matte beside it.
24. **ORCHESTRATOR:** the painted board's click area is a rectangle measured on the desk v3 painting (`render/board_mask_office.png`, viewport 0.867 to 1.0 by 0.324 to 0.620 at 16:9, moved for other aspects assuming the office camera keeps its height of view). It takes clicks only in the plain office view (not in the reading view, the city, inspect mode, on the PC or under a newsletter). **To check in Unity:** that it lies on the painted board at 1920x1080 and 1280x720; tune `wallRect` and `wallLamp` if not.
25. **ORCHESTRATOR:** the board does not close when the next traveller steps up (the city view does). The player finishes a patch first; the desk waits. It does close for the shift report and the PC.
26. **ORCHESTRATOR:** cords hang between the plugs' end caps and leave each plug along its axis (down-left, as the plug sprites point), so they dip out of a plug and sweep across the board. They cross the module faces and each other (Saleh: cables cross). Cord look: 40 px of slack, a run of 30 % of the gap up to 120 px along the plug axis, 8 px rubber with an ink outline and a highlight.
27. **ORCHESTRATOR:** the switchboard installs itself into the office's gameplay scene at load and builds its parts at run time (art slots + the plate's JSON), like the hover highlighter. So no OfficeGameplay rebuild is needed for the demo. Moving it into Build Office UI is possible later (one host, references wired), but nothing requires it.
28. **ORCHESTRATOR:** sounds: the sound bank's file for each cue (`switch_buzz`, `switch_plug_in`, `switch_plug_out`, `switch_ratchet`, `switch_master`, `switch_fuse`, `switch_open`, `switch_send`, `switch_tick`) when SOUND_LIST.md gets them, else the board's own generated placeholders, through the desk's mixer group.

## Files

| File | What |
|---|---|
| `Scripts/Domain/Switchboard.cs` | the rules: routes, calls, modules, power cords, MASTER, calibration, departures, fuse, lamps, the ramp (`SwitchboardRules`) |
| `Scripts/Domain/SwitchboardDirectory.cs` | today's places on the plates |
| `Scripts/Domain/SwitchboardLayout.cs` | the plate's measured layout, read from `board_layout.json` |
| `Scripts/Domain/SwitchCords.cs` | the cords' curve and the snapping |
| `Scripts/Domain/SwitchboardSounds.cs` | the generated placeholder sounds |
| `Scripts/Domain/SwitchboardWords.cs` | the UI string keys |
| `Scripts/Office/Switchboard/SwitchboardView*.cs`, `CordGraphic.cs`, `SwitchboardConfigSO.cs` | the view (install, day, events; parts and drawing; the hand), the cord mesh, the knobs |
| `Scripts/DevTools/SwitchboardCheats.cs` | the review cheat |
| `Resources/SwitchboardConfig.asset` | the knobs |
| `Art/UI/Resources/Switchboard/` | the plate (lever-free), the layout JSON, the plug, lamp and lever sprites (1x), imported from the board kit |
| Tests: `SwitchboardTests`, `SwitchboardDirectoryTests`, `SwitchboardLayoutTests` (layout, cords, snapping, sounds, words), `ControlRulesTests` (keys, back-out, the wall click) | |

Other owners' files touched (small, for the PORTALS lease chain to confirm): `Domain/ControlRules.cs` and `Office/OfficeControls.cs` (DOC-HANDLE → CITY-LIFE → PORTALS in LEASES: the S key, D at the desk, the back-out step, `BindSwitchboard`), `Domain/ArtSlots.cs` (the `Switchboard/` slots), `Editor/ArtSlotImporter.cs` (the plate's high-quality compression, like the hall's), `Domain/SoundSynth.cs` (`Normalise` made internal to reuse it), `Office/OfficeSceneBinder.cs` (DESK-HOMES: the desk's slide-ins pause while the board is open, as they do for the PC frame, so no machine slides in, unseen and heard, behind the board), `Data/World/world_source.json` (9 `switchboard.*` UI strings: the 8 worded ones tier Flavour with their translations in the 6 culture tables, the bonus template tier Full), `docs/FEATURES.md`, `docs/CHEATS.md`. GameManager, BoothCoordinator and the scenes are untouched.

## Checks (outside Unity)

- `compile_check_any.py E:\unity\NOPE-p5`: 0 errors; no new warnings in the new or edited files.
- Offline runner: passed 3873, failed 0 (the baseline on `adcdf6a` was 3843: 30 new tests).
- QA python tests (`tools/qa/test_seqlib.py`, `tools/characters/test_processing_stencils.py`): 38 passed.
- An offline render of the board's live parts with the same geometry rules (`design\switchboard_v4\board\preview_live_parts_v5.png`) shows the cords, plugs, lamps, labels, displays, lever and hint card landing on the plate.

## For the Unity phase (not run: no Unity before it)

1. Import: the `Switchboard/` art imports as sprites (ArtSlotImporter), the plate with high-quality compression; `SwitchboardConfig.asset` loads.
2. **Run Tools > TimeDesk > Generate World** (the 9 new `switchboard.*` UI strings and their culture translations, ours: a native reader should check the Arabic, Greek, Chinese and Japanese). Until then the hint card and the module displays' words stay blank (never the keys); the countdown digits still show.
3. Play day 1: approve a traveller; the wall lamp blinks and buzzes; D opens the board; plug 01's PLACE into the destination, PERIOD into its period (01 is powered); pull MASTER; 20 s later the group leaves; Esc returns. Then the cheat **Switchboard: 3 test calls** for the full loop with modules 01 to 03 and both power cords.
4. Captures at 1920x1080 and 1280x720 (the board, the turn, a held plug with its targets, MASTER pulled, a fuse, a departure flare); the wall click area on the painted board; no console errors.
5. Feel: the plug snap and MASTER's weight (the director's 7/10 gate).

## Open calls for Saleh

1. The ramp's days (decision 20) and whether day 1-2 should show only one module.
2. Should the board show which period a call wants (decision 19)?
3. Waiting costs, the waiting room and the human-rights fine (the portals spec's P1 rest): not in this demo.
4. The hall's ring pulse at approval vs at the board's send (decision 16).
5. 04 and 05: painted placards for now (decision 3).
