# PC case screen "Lens": build spec

- **Status:** waits for Saleh's yes. No game code changed.
- **Date:** 2026-11-12. **Branch:** `feat/pc-lens`, from `integrate/night-build` (d076619).
- **Decision:** vault note `Decisions/2026-11-12 The PC case screen is Lens with drag and drop`.
- **Reference for layout and behaviour:** the prototype, route "1 Lens" (`E:/unity/NOPE-tools/design/pc_ui_1111/r5`). The neon colours of the prototype are not the reference (Saleh, 2026-11-12: "Nothing in the office should have neon colors").
- **Engineering level:** production grade for behaviour and tests, because it lands on main. Reuse first. No new art.

## 1. The screen

The screen fills the PC glass (1440 x 1080 units). It replaces the title bar, the menu bar, the status line, the two pane headers and the findings column of the Investigation app.

```
+--------------------------------------------------------------------+
| < Home   (face) Akira                 [2 findings] [Inspect Space]  13:14 |
|          Soldier, Displaced person                                  |
+--------------------------------+-----------------------------------+
| The next paper opens here      | Select this side                  |
|                                |                                   |
|   TRAVEL PERMIT (document)     |   REGISTRY (document)             |
|                                |                                   |
+--------------------------------+-----------------------------------+
|                    [ Discrepancy ]  (shows after two picks)         |
|  Permit  Ticket  Card  Transcript | Registry  Rules  Calendar  Books |
+--------------------------------------------------------------------+
```

- **Header:** Home key, the traveller (name and role), the findings count, the Inspect key, the time.
- **Two sides:** each side holds one document. A document keeps its own document look (the present form pages).
- **Answer plate:** one word group and one short line. It shows only after two picks.
- **Dock:** one row. Left group: the papers of the traveller and the transcript. Right group: Registry, Rules, Calendar and each reference book. A document shows from the day the game introduces it (as today).

## 2. Controls

| Action | Result |
|---|---|
| Click a side tag | That side is selected ("The next paper opens here"). F6 does the same. |
| Click a document in the dock | The document opens on the selected side. Then the other side is selected. |
| Drag a document from the dock, drop on a side | The document opens on that side. |
| Drop on a full side | The new document takes the place of the old one. The old one goes back to the dock. |
| Drag from side to side | The two documents change places. |
| Drop outside the sides | The document goes back to the dock. |
| Space, or the Inspect key | Inspect on or off. |
| Click a field or a rule line, inspect off | Nothing is picked. A notice says "Press Space to inspect." |
| Click a field or a rule line, inspect on | First pick: outline, the plate says "Pick a second thing." Second pick: the PC answers. |
| Drag a document on a side, inspect on | The document does not move. The dock still works. |
| Right-click or Esc | Backs out one step: the pick, then inspect, then (Esc only) the PC settings. |
| Enter (keyboard ring) | Picks the focused value. Space had this job. Space is now inspect. |

The answer is "Matching data", "Discrepancy" or "No relation", with a dashed line between the two picks. The present `MatchBoard` logic gives the answer. A discrepancy goes to the findings and raises its question on the wheel (as today).

## 3. What is reused, new and removed

| Reused as it is | New (small) | Removed from this screen |
|---|---|---|
| `AppPane` (one for each side), all document views | `LensSides`: the pure rule for select, place, change places, put away | Title bar, menu bar, status line, pane headers, 95 unit gutter |
| `MatchBoard`, `CompareController`, `FindingLog` | The dock (the rows of `MenuBarView` drawn as one row) with drag and drop | The split key (Ctrl+\\): the screen always has two sides |
| `MatchLines` (the dashed line) | Inspect mode on the PC (Space, the key, the gate on picks) | The line from the first pick to the pointer, and the label on the line |
| `FindingsView` (opens from the count in the header) | The answer plate (three answers) | The parts in section 4 that Saleh rules out |
| The scene builder `OfficeSceneUIBuilder` | The Lens layout in the builder | |

## 4. Rulings that I need from Saleh

### 4.1 Parts of the present app that do the detective work

"Hide" means: not on this screen. The code stays until a later clean-up.

| No. | Part today | What it does for the player | My recommendation |
|---|---|---|---|
| 1 | Case board: cross-check table | Lists the shared facts of all papers in one table. Cells that differ glow. | Hide. |
| 2 | Case board: rules check | One row for each rule with VALID, EXPIRED, CLOSED, MISSING or WRONG DATE. | Hide. |
| 3 | Case board: record plate | Finds the record with no action of the clerk. Logs "No record on file" by itself. | Hide. See choice 1 in 4.2. |
| 4 | Case board: overlay | Lays two scans over each other and finds the differences. | Hide. |
| 5 | A scan opens the case board | The scan puts the board on the glass. | The scan opens the Lens screen. The paper goes to the dock, and to the selected side if that side is empty. |
| 6 | Guided steps (Ctrl+1 to 4) | Puts the right pair of documents up. Finds the record. Opens the book of the first field not checked. | Hide. The keys go. |
| 7 | Smart links (the arrow on a value) | One click opens the related document and runs the record search. | Hide. |
| 8 | Search drawer (Ctrl+K), pins, recent items | Finds a word in all documents. Shows the count for each source. | Hide. The Registry keeps its own search field. |
| 9 | Reference books | Puts the row of the claimed place first with the note "Claimed". Starts each case with the filter "Claimed place only". | Remove the note, the order and the filter. A book is in its natural order. |
| 10 | Status line counters | "Papers: 1 of 2 received, 0 scanned". | Hide. The header shows the name and the role only. |
| 11 | Dots on documents not opened | Marks each paper that the clerk did not open in this case. | Hide. Keep the dot for a new transcript line. |
| 12 | Scan layers: CHIP | A chip field that differs from the print glows. | Your ruling. The chip reader is paid equipment. I recommend: no glow, the clerk picks the two values. |
| 13 | Scan layers: UV | Fault marks glow under UV. | Keep. The lamp shows the mark; the clerk must still judge it. |
| 14 | Analysis Scanner marks | Dashed outlines on the fields that the scanner marked. | Your ruling. It is a paid order. I recommend: keep it as the reward of that order. |

Two parts stay because they record, and do not find: the findings list (only what the clerk compared) and the flag for a paper that the traveller did not hand over. That paper shows in the dock as a dashed item. A click flags it (as the Papers menu does today).

One gap comes from item 3. A traveller with no record needs a route to a finding. I propose: the line "No entry for DP-0000" of the Registry is a thing the clerk can pick. That line and the ID on a paper give "Discrepancy".

### 4.2 Open choices

| No. | Choice | My recommendation | Reason |
|---|---|---|---|
| 1 | The clerk types the record number, or the record comes with the scan | The clerk types it | It is the prototype. `RecordsView` does it today. The search is play, and it costs clock time. |
| 2 | A tool shows its time cost, or it does not | It does not | The shift clock is real time. Each action costs time already. A shown cost is a new rule that needs balance. |

Decided on 2026-11-12: the look. Saleh: "Nothing in the office should have neon colors". The Lens screen keeps the colours that the PC has today (his pick: "Colours of today"). No theme change is in this task.

## 5. Look and text size

- No neon colours and no glow. The screen uses the tokens of the present PC theme with their present values (`PcTheme_Cyberpunk.asset`): a warm cream glass, dark teal ink, an amber accent that follows the leading culture.
- The two sides use the deeper glass tone, so that a paper stands out from the ground. The selected side has a teal frame. The answer plate uses the theme tones for match, difference and note.
- Few words, large type. Field value 31 units, field label 17 units, document title 30 units, dock label 19 units (the prototype sizes on the 1440 unit glass).
- Target on the monitor: field values 18 px or more and labels 13 px or more, at 1920 x 1080 and at 1280 x 1024. The stills measure it.
- Risk: the glass is 1120 x 840 px at 1920 x 1080 today. If a still fails the target at 1280 x 1024, the PC view camera must go nearer. That is a separate change and I ask first.

## 6. Tests (four levels)

| Level | What |
|---|---|
| Unit | `LensSides` (place, full side, change places, put away, selected side). The key map (Space is inspect, Enter picks). |
| Integration | EditMode: the scene contract of the Lens screen, plus the full present suite. |
| Function | New PlayMode scenarios `lens_place`, `lens_drag`, `lens_inspect`, each at 1920 x 1080 and 1280 x 1024 with stills. The six present PC scenarios updated. `smoke_day1`. |
| End to end | One `e2e_run` in the built player. |

## 7. Task split

Worker agents are sonnet at medium effort. One opus agent verifies the code of B and C. I do E and F.

| Task | Content | Needs Unity |
|---|---|---|
| A | `LensSides` and the key map, with unit tests | No |
| B | Screen code: the app wiring, the dock with drag and drop, inspect mode, the answer plate, the removals of 4.1 | No |
| C | The Lens layout in the scene builder and the sizes | One batch run to build the scene |
| D | The scenarios and the stills (starts with C) | Yes |
| E | `docs/FEATURES.md`, the report with stills, the vault | No |
| F | The four test levels, one player build, the end to end run | Yes |

A, B and the code of C need no Unity. I ask for your go before the first Unity run. Nothing goes to main without your go.

## 8. Out of scope

Home, Orders, Mail and the other apps. A change of the PC theme. The Desk and Lamp concepts. New art. The removal of the hidden code. A change of the PC view camera.
