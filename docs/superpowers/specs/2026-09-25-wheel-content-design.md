# Traveller wheel content, basic version: design (piece 8)

*2026-09-25 · decisions made by Claude under Saleh's instruction "dont stop until you finish everything", open to his review · branch `feat/wheel-content`, from `feat/characters` at `65de80e` (piece 4 on top of pieces 0-3, 5 and 7)*

Saleh on the wheel: "The player will be able to trigger some interactions with the traveller by clicking on the traveller which will open a wheel of options they can then select an option each option could be a question or a request etc. will be fleshed out later". This piece is the basic, extensible version: what kind each choice is (to order the ring and pick its icon), a few spoken requests authored as data, placeholder icons, and a paced speech bubble that also says the claim on arrival and changes a premade's face line by line. Scope: the piece-7 spec's "Out of scope → piece 8" (`2026-09-25-physical-desk-design.md` §4) and its contract §2.21; "Look >" already shipped with piece 4. Not here: wheel theming (piece 6), a paged ring beyond 8, any new mechanic behind a request.

Baseline: `compile_check.py` 0 errors, the offline runner `passed 576, failed 0`, a headless dump of days 1-3 for seeds 12345 and 999 (`p8_baseline_cases.txt`, 60 travellers).

**No scene work in this piece.** The builder-built 2D OfficeScene is outdated and the game is moving into the art office (an additive gameplay scene). Nothing here changes the builder or a scene: icons and the typed reveal are created at runtime on the existing wheel and bubble, and the new knobs are read at runtime. §6 lists the checks to run in the art office after the move.

## 1. Decisions

| Id | Decision | Rationale |
|---|---|---|
| W1 | **Kinds: `DialogChoiceKind` gains `Request`, `Question`, `Look`, `Dialog` (appended after `Normal`, `Back`).** A sub-menu entry takes the kind of what it opens: "Ask about home >" is a Question, "Look >" is a Look. `Normal` stays the default: a reply inside a narrative dialog. | The ">" in a label already says "opens a menu"; the icon then says what is inside. No Menu/Nav kind, so there is nothing to keep in sync. |
| W2 | **One order for every menu: Back, Request, Question, Look, Dialog, Normal (`DialogChoiceKinds.Arrange`, a stable sort).** The wheel renders choices in that order (ring item 0 at the top, then clockwise). | Groups each kind together and keeps "< Back" first where there is no centre slot. Every existing menu already comes out in this order, so nothing moves for existing content; new kinds slot into their group. |
| W3 | **An icon per kind, by file name `wheel_{kind}` (`wheel_request`, ...), loaded from `Resources/WheelIcons` (final art goes in `Assets/Art/UI/Resources/WheelIcons/`), else a generated 32×32 white placeholder glyph kept in memory only:** a sheet of paper (Request), a speech balloon (Question), an eye (Look), two balloons (Dialog), a left arrow (Back), a dot (Normal). Only the traveller wheel shows icons, in a square at each button's left edge with the label moved past it. | The characters' pattern (piece 4): art drops in by name with no code change. Glyphs are shapes, never letters (UI art rule 5). Runtime-created, so no builder, prefab or scene change; the intercom list of the preserved hybrid scene keeps plain labels. |
| W4 | **Spoken requests are data: `interview.requests[]` in `world_source.json` (`id`, `label`, `prompt`, `reply`), generated into `InterviewLines.requests`.** The hub offers each after the document requests: id `act:{id}`, kind Request, one-shot per traveller, the desk's prompt then the traveller's reply. Starter content: "Step closer" and "Speak up". | "No new mechanics": a request only makes the traveller answer, so it needs no `DialogAction` (the piece-7 contract §2.21 expected new action values; a later request with a mechanic adds its action value and a source field then). Like a question, the wheel stays open and the transcript opens. |
| W5 | **Capacity: the hub's worst case counts the spoken requests** (documents handed over on request + spoken requests + the ask and look entries + unbound dialogs + one premade dialog). Today: 1 + 2 + 2 + 2 + 1 = 8 = the wheel's capacity. | Generate World and the validator keep rejecting a hub the wheel cannot show. There is no headroom left: a new unbound dialog needs a request dropped or the paged ring. |
| W6 | **The bubble paces lines (`SpeechQueue`, pure).** The traveller's lines play one after another; each types out at `bubbleCharsPerSecond` (0 = at once); once fully shown it stays `bubbleSeconds` when nothing follows, or `bubbleMinSeconds` (never more than `bubbleSeconds`) before the next queued line replaces it. A new reply queues behind the current one and never cuts a line. Knobs in `DeskConfigSO` (40 cps, 1.5 s, 4 s). | A single line at 0 cps behaves exactly as before (up 4 s). Queueing keeps two-line replies and quick successive questions readable; the PC transcript stays the evidence. |
| W7 | **The claim on arrival: when a traveller is presented, the bubble says their claim** (the transcript already starts with it), if the wheel may open then (the settled booth view). | The traveller speaks first, as in the reference game. Outside the booth view a bubble would float over the monitor; the banner and the transcript hold the claim anyway. |
| W8 | **A premade's face changes when the line carrying the expression starts in the bubble, not when the choice is picked.** The wheel sets it on its `TravellerView`. When the bubble is cut (the view leaves the booth, or the traveller leaves) the last expression among the lines not yet shown applies at once. `InvestigationUIController.TravellerExpressionChanged` and `GameManager`'s forwarder are removed: one path. | The face matches the line being read; after a cut the face never lags behind what was said. |
| W9 | **`InterviewScript.SaidSince` (the traveller's lines since an index) replaces `SpokenSince` (a joined string) and `ExpressionSince`.** | The wheel now needs the lines themselves, one by one, each with its expression. |
| W10 | **Drive-by: the validator's blank-wording check now covers `lookLabel`** (piece 4 added the label to the generator's check only). | Same list this piece touches. |

## 2. Behaviour

- **Ring order and icons.** Hub: document requests, spoken requests, "Ask about home >", "Look >", dialogs. Each wheel button shows its kind's icon at its left. "< Back" (centre) shows the back arrow.
- **Spoken requests.** "Step closer" → DESK "Step closer to the glass, please." / traveller "Like this?"; "Speak up" → DESK "Speak up, please. I can barely hear you." / traveller "Sorry. Is this better?". Each is offered once per traveller; the reply shows in the bubble and in the transcript (never compare-clickable: not an answer).
- **Bubble.** Lines type out at 40 characters a second; the last one stays 4 s once complete; a line followed by another stays at least 1.5 s once complete. Replies queue. A choice without a reply ("Ask about home >", "< Back") adds nothing, so the current line plays on. Clicking the CRT (or anything that disallows the wheel) clears the bubble.
- **Arrival.** Pressing READY presents the traveller, and the bubble types "I request passage home to <place>."
- **Premade faces.** Senenmut's face turns happy when "My temple at Deir el-Bahari..." starts typing, worried when "Then my name will be chiselled..." does.

## 3. Design

**Domain (`TimeDesk.Domain`, pure, tested)**
- `Dialog.cs`: `DialogChoiceKind` + `Request`, `Question`, `Look`, `Dialog`. New `static class DialogChoiceKinds`: `int Rank(kind)` (Back 0, Request 1, Question 2, Look 3, Dialog 4, Normal 5), `IReadOnlyList<DialogChoice> Arrange(IReadOnlyList<DialogChoice>)` (stable, nulls dropped), `string IconName(kind)` = `"wheel_" + kind` in lower case.
- `InterviewContent.cs`: new `[Serializable] InterviewRequest { id, label, prompt (LineText), reply (LineText) }`; `InterviewLines.requests` (list).
- `InterviewScript.cs`: `Build` sets every choice's kind (W1) and adds the spoken requests (W4); `SaidSince(transcript, from)` (W9); `DialogChecks.MenuProblems(..., int spokenRequests, ...)` (W5).

**Visuals (`TimeDesk.Visuals`, pure, tested)**
- `SpeechQueue(charsPerSecond, minSeconds, holdSeconds)`: `Say(text, expression)` (a blank text is ignored; starts at once when idle), `Tick(seconds)` (non-positive ignored; may pass several lines in one tick), `Showing`, `Text`, `VisibleCharacters`, `LineNumber` (counts lines started), `Expression` (the latest started line's expression that is not blank; null after `Clear`), `string Clear()` (drops everything; returns the last expression among the lines not yet started). Negative knobs count as 0; `minSeconds` above `holdSeconds` counts as `holdSeconds`.
- `WheelIconPlaceholder`: `Size` = 32; `byte[] Render(string iconName)`: RGBA32, row 0 = bottom, white glyph on transparent; null for an unknown name.

**Runtime (Assembly-CSharp; checked in the art office, §6)**
- `DeskConfigSO`: `bubbleCharsPerSecond` (40), `bubbleMinSeconds` (1.5); `bubbleSeconds` (4) now means "once fully shown, when nothing follows".
- `TravellerWheel`: owns the `SpeechQueue` and the icon sprites. `Say(IReadOnlyList<DialogLine>)` queues the lines (through `DisplayText`, Spoken) while the wheel may open. `LateUpdate` ticks the queue, shows each new line on the bubble, reveals its characters and sets the traveller's expression. `SetCanOpen(false)` clears the queue (applying W8's pending expression). `IconFor(kind)`: the art sprite or the placeholder (cached; placeholders destroyed with the wheel).
- `OverlayCallout`: `Reveal(int characters)` (TMP `maxVisibleCharacters`); `Show` reveals everything again; an infinite time never runs out (the wheel times lines itself).
- `InteractionAction.icon` (optional); `InteractionPanelController` draws it in a square (`iconSize`, 28 reference px, a serialized layout value like the ring's radii) at the button's left and insets the label.
- `InvestigationUIController`: renders `DialogChoiceKinds.Arrange(choices)` with the wheel's icons; `Choose` hands `SaidSince(before)` to the wheel; `StartInterview` hands it the opening's traveller line (the claim, W7).
- `GameManager`: the expression forwarder is removed (W8).

**Content pipeline**
- `world_source.json` `interview.requests`; `WorldContentGenerator` builds `InterviewLines.requests` with line ids `interview.requests.{id}.prompt` / `.reply` and checks each request: id present and unique, label, prompt and reply not blank, ASCII, line ids unique, prompt and reply within `maxLineChars`, and the hub count (W5).
- `ContentLibraryValidator`: the same blank and duplicate checks on the library, the hub count, and `lookLabel` (W10).

## 4. Tests (EditMode, offline runner then Unity's Test Runner)

- `DialogChoiceKindsTests` (new): rank order; `Arrange` groups by kind, keeps order within a kind, drops nulls, puts Back first, and leaves every built menu of `InterviewScript.Build` in its built order; icon names distinct and prefixed; every kind's icon name draws a placeholder.
- `SpeechQueueTests` (new): typing speed and the instant case; hold and minimum; queued lines in order, a new reply never cutting a line; several lines passed in one tick; blank lines ignored; the expression of the latest started line (blank keeps it); `Clear` returns the pending expression and resets; knob clamping; non-positive ticks ignored.
- `WheelIconPlaceholderTests` (new): size, a white glyph on transparent, distinct glyphs, unknown names.
- `InterviewScriptTests` (changed): every built choice's kind; spoken requests (after document requests, before "ask", one-shot, lines and ids, none when there are none, null entries skipped); `SaidSince` (replacing the `SpokenSince` and `ExpressionSince` tests); `MenuProblems` counting spoken requests.

## 5. FEATURES.md

Updated in the same commits: the traveller wheel (order, icons), the speech bubble (pacing, arrival claim, faces), the interview's hub (spoken requests), Characters (when a premade's face changes), Generate World and the validator (request checks).

## 6. Scene checks to run in the art office after the move

Nothing here was run in a scene (by instruction). The move must check, at the 1920×1080 reference and one wider screen:
1. The wheel: each button shows its kind's icon at the left, readable, with the label clear of it; "< Back" in the centre shows the arrow and its label still fits; the ring still fits 8 items.
2. Hub order on a day-1 traveller: "Request Transit Permit", "Step closer", "Speak up", "Ask about home >", "Look >", then any dialog.
3. "Step closer": the transcript gets the desk's prompt and "Like this?", the bubble types the reply, the entry leaves the hub, the wheel stays open.
4. READY: the traveller appears and the bubble types the claim.
5. Pacing: a reply types at about 40 characters a second and stays about 4 s; picking a second question during the first reply queues it (the first finishes, stays about 1.5 s, then the second types).
6. Senenmut (day 1, slot 3, seed 12345): happy on the temple line, worried on the chiselled line, each as the line starts.
7. Clicking the CRT mid-reply clears the bubble; Senenmut's face shows the last expression already said.
8. A document request closes the wheel while its reply keeps typing.
9. `Desk_Default`: 0 characters a second shows whole lines at once; the new knobs appear in the inspector.
10. No warning or error is logged; the preserved hybrid scene is not opened or changed.

## 7. Verification record

*(filled in when the piece is verified)*
