# The PC, redesigned for use: one navigator, full words, a calm shell: design

*2026-09-30 · written and built overnight by Claude (Saleh asleep: no questions, every decision taken and recorded here) on `feat/pc-ux-redesign` from `main` `c089333` · springboard: Linear and Notion (one sidebar that navigates, a search-first command palette), macOS System Settings and Finder (a source list with the chosen item's children under it), Things (a quiet checklist), Raycast (pinned and recent items in the palette before you type), VS Code (split editor: one button to open beside, one to close) · the fiction stays: a 2150 Temporal Customs desk terminal in a beige bezel, re-themed by the leading culture · the code wins over this text*

Saleh, verbatim (2026-09-30):

> "take another pass improving the ux of the pc tool. it looks so outdated and horrible to use so many tabs so unintuitive. as an expert ux designer redesign it from scratch to be minimalistic readable clear, there shouldn't be abbreviated letters on the tabs, think of real modern designs for software and use them as a spring board."

His standing rules that bound this pass: the game's rules and data do not change; every task the clerk could do stays possible; the UI strings live in `world_source.json`; the PC re-themes by the leading culture (fonts and scripts, Arabic and CJK included, must still fit); readability floors hold at 720p. The earlier PC spec (`2026-09-26-pc-redesign-design.md`) stays the contract for behaviour (the index, search matching, smart links, compare, pins, the Escape chain, the steps' rules); this spec replaces its **layout, navigation and look** (its §1.1 DK2, DK8, §1.2 WN3, §1.3 AP2-AP4, §1.4 ST1, §1.6 PR1's sidebar lists, §2's wire-frames) and says so row by row.

## 0. The audit (what the PC was, `main` `c089333`)

Screenshots of every screen at 1080p and 720p under three cultures (neutral, Iraq in Arabic, Japan in Japanese) were taken before any change: `SCRATCH/pcux/before/shots` (the tour job `SCRATCH/pcux/_TimeDeskPcTour.cs.txt`).

### 0.1 What the clerk does on the PC during a shift

| # | Task | Where it was | Clicks from the desk (before) |
|---|---|---|---|
| T1 | Read a scanned paper | Investigation ▸ a pane's "Documents" tab ▸ the paper's chip in a sideways-scrolling chip row | 2-3 |
| T2 | Look up the traveller's citizen record (name or number) | the "REC" tab ▸ the lookup field ▸ SEARCH | 3 + typing |
| T3 | Compare a paper's value against a record, a book row or another paper | click a value in one pane, click the other in the second pane; the dock under the windows says MATCH or MISMATCH | 2 per pick |
| T4 | Check a place fact in today's reference books | the "REF" tab ▸ the book's chip ▸ its claimed-place row ("Claimed place only") | 2-3 |
| T5 | Check today's rules and directives | the "RUL" tab (or Mail's directive memo ▸ its link) | 1-2 |
| T6 | Re-read what the traveller said | the "TRN" tab (the Interview Record; "Answers only") | 1 |
| T7 | See what is proven so far | the "RPT" tab (the Deviation Report); the dock's DEVIATION LOGGED | 1 |
| T8 | Search the case and the day | the toolbar's field; results grouped by source with "DOC", "REC", "BOOK", "TRN", "DEV", "RULE" badges | typing |
| T9 | Follow a smart link (↗) from a value to where it is checked | the ↗ on a form box; the other pane | 1 |
| T10 | Pin or revisit an item | the pane header's Pin; the sidebar's PINNED and RECENT lists | 1 |
| T11 | Tick the checklist, jump to a step | the sidebar's STEPS | 1 |
| T12 | Decide | ACCEPT / DENY in the case header (or the stamp tray at the desk) | 1 |
| T13 | Read mail and the day's directive memo | Mail (inbox list + memo form) | 2 |
| T14 | Read the news, look up history and ancestry | Internet: The Temporal Times, Chronopedia, Lineage Archive | 2-4 |
| T15 | Check own debt, balance, statement | Citizen Account (Record Extract + Statement forms) | 1 |
| T16 | Order upgrades, portal repairs; cancel or install | Orders (the upgrade tree, zoom, the detail pane) | 2-3 |
| T17 | Check today's portal routes | Portals (the Portal Schedule form) | 1 |
| T18 | Keep notes and clippings | Notes | 1-2 |
| T19 | Change language, motion, icons, text size, steps; see the shortcuts | Settings; F1 | 1-2 |
| T20 | Arrange the desktop, open apps, get back to the desk, turn the screen off | icons, Start menu, "< Desk", the tray | 1 |

### 0.2 What was wrong (heuristics, worst first)

1. **Twelve tabs on one screen, six of them abbreviated.** Each of the two panes had its own strip of six tabs; a narrow strip collapsed the inactive ones to "DOC REC REF TRN RPT RUL". Search results carried the same abbreviations. The clerk had to decode letters to navigate (recognition over recall, broken).
2. **Four stacked navigation rows before any content.** Title bar, case header, toolbar, tab strip and chip row: 212 of 988 units before the first line of a paper, and the item chooser scrolled sideways with "<" ">" arrows, hiding papers and books off its edges.
3. **Everything the same weight.** Saturated blue title bars, blue tab strips, green chosen chips, yellow compare dock, tan sidebar: every surface shouted, so nothing led the eye; the Windows XP "Luna" look (glossy blue bars, a green italic "start") read as dated.
4. **Text too small at 720p.** The sidebar, steps, pins and hints were 13-18 units (6.7-9.3 px at 720p), under the project's own floor (no label under 12 px at 720p, `2026-09-26-pc-redesign-design.md` §6.3); desktop icon labels shrank to 10 units.
5. **Broken glyphs and cut labels.** The maximise button drew "▯" (no glyph in the font); Orders cut names ("East Asia Translator: Spe…"), chips cut long paper names, taskbar buttons cut titles.
6. **Controls far from their effect.** "Split" in the toolbar split the panes below; "Steps" in the toolbar folded the sidebar's list; "F1 Keys" said a key, not a thing.

## 1. Principles

| Id | Principle | What it rules out |
|---|---|---|
| P1 | **One way to navigate.** A single left navigator lists every source of the case in full words; the source's items (papers, books) list under it. Nothing else navigates. | Per-pane tab strips, abbreviated glyph tabs, sideways chip rows. |
| P2 | **Words, never letters.** Every label is a full word or phrase from `world_source.json` (`ui.strings`); no abbreviation anywhere in the chrome. Units stay units ("125 cr" is the game's currency sign, like "$"; the words around it are spelled out). | "DOC", "REC", "F1 Keys", "< Desk", "start". |
| P3 | **Content first.** Chrome takes the edges: one title bar and one toolbar row (108 units) before content, then the paper. | Four header rows. |
| P4 | **Quiet shell, loud decisions.** The shell (title bars, taskbar) is the culture's deep colour; the work surfaces are paper; colour means state (the selected source, a badge, the active pane, Accept and Deny), never decoration. | Glossy bars, saturated chrome everywhere, gloss overlays. |
| P5 | **Controls live beside what they change.** Open beside and Close sit in the pane header; the checklist folds from its own heading. | Split and Steps in a toolbar. |
| P6 | **Readable at 720p in every script.** Chrome text is at least 24 units (12.5 px at 720p), labels never shrink below it, and a label that could be long wraps instead of being cut. | Auto-sized 10-16 unit labels, ellipses in navigation. |
| P7 | **Keyboard parity.** Every action keeps its key; the new places are in the same Tab order. | Mouse-only navigation. |

## 2. Information architecture

**Before:** desktop ▸ 8 icons ▸ Investigation ▸ 2 panes × 6 tabs ▸ per-tab chip row ▸ view; plus a toolbar (Back, Forward, search, Steps, Split, F1 Keys) and a sidebar (STEPS, PINNED, RECENT).

**After:**

```
Desktop (8 icons, unchanged ids and order)
└─ Investigation
   ├─ Toolbar: Back · Forward · Search (the command palette) · Accept · Deny
   ├─ Navigator (left)
   │   ├─ the case: the traveller's counters (papers received and scanned, deviations)
   │   ├─ Papers ▸ each paper by its full name
   │   ├─ Citizen records
   │   ├─ Reference books ▸ each book by its full name
   │   ├─ Transcript
   │   ├─ Deviation report
   │   ├─ Today's rules
   │   └─ Checklist (folds from its heading)
   ├─ Pane: header (source › item, Pin, Open beside) and the page
   └─ Second pane, when open beside: header (source › item, Pin, Close) and the page
   Search palette (under the search field): before typing, Pinned and Recent; typing, results grouped by source in full words
```

| Id | Decision | Replaces | Why |
|---|---|---|---|
| IA1 | **The navigator** (`AppNav`): six source entries in the saved order, each a full-word label with its badge dot; the entry the **active pane** shows is selected (accent plate); the entry the other pane shows, while side by side, wears an accent outline ("beside"). A click shows the source in the active pane; Ctrl+click in the other pane (opening it beside). | AP3's two tab strips and their glyphs | P1, P2. A vertical list has room for full words in every script; a single list removes the question "which strip do I click?". |
| IA2 | **Items under their source.** The selected source's items (a paper per chip, a book per chip, exactly the views' existing `Chips`) list under its entry, indented, the shown one on a paper plate in bold; an item not readable yet is dimmed and says why when clicked, as before. | AP5's chip row, its arrows and the "Chosen" chip | Finder's and System Settings' pattern: the children of what you chose, all visible, no sideways scroll. |
| IA3 | **Source names in full:** Papers, Citizen records, Reference books, Transcript, Deviation report, Today's rules (`app.tab.*`). The search groups and the pane headers use the same words. | "Documents", "Records", "Reference", "Report", "Rules" and their letters | Say what it is: "Papers" is what the clerk holds; "Deviation report" names the form. |
| IA4 | **The case summary** tops the navigator: the counters on two lines ("Papers: 2 of 2 received, 2 scanned" / "Deviations logged: 0"), "Waiting for the next traveller" between travellers. The traveller's name stays in the title bar. | The case header band (AP1) | P3: the band's row goes; the counters are glanced at, not read. |
| IA5 | **One toolbar row:** Back, Forward, the search field (flexible), then Accept and Deny at its right end. | The case header and the toolbar (AP2) | P3; the decision stays at the top right, where modern tools put the primary action. |
| IA6 | **Side by side** is an icon button in the pane header (two panes drawn side by side; its hover hint reads "Open beside"); the second pane's header has a Close icon (✕, "Close"). Pin is an icon button beside it (a drawn pin, "Pin"). Too narrow a window (a restored one) disables Open beside and its hint says why. Ctrl+\ still toggles. | The toolbar's Split (AP3) | P5, VS Code's split editor. |
| IA7 | **The active pane** wears a 3-unit accent underline under its header and its title in full ink; the other pane's title is muted. | The 3-unit frame round the whole pane | Quieter, and it points at the header the navigator is talking to. |
| IA8 | **The checklist** is the navigator's last section: a click on its heading (a chevron, "Checklist", the count of steps done, "2 of 6") folds and unfolds it (the same StepsPanel.Toggle, Settings and Ctrl+Shift+S). | The toolbar's Steps and the STEPS block (ST1) | P5, Things' quiet checklist. |
| IA9 | **Pinned and Recent move into the search palette** (Raycast, Linear's Ctrl+K): focusing the empty search field opens the palette with Pinned, then Recent; typing turns it into results; a pick jumps (SmartLinks, as before); a press outside, Escape or a jump closes it. Pin stays in each pane header and on Ctrl+P and the row menu. | The sidebar's PINNED and RECENT (PR1) | They were two always-empty boxes most of the shift; in the palette they are one keystroke away where the clerk already goes to find things. |
| IA10 | **Search results** drop the source letters: each group is headed by its source's full name and each hit shows its title and marked snippet; the filter chips read "Papers (3)". | "DOC", "REC", "BOOK", "TRN", "DEV", "RULE" | P2. |
| IA11 | **Reordering** keeps its rule (TabOrder, saved per player): drag an entry up or down the navigator past a neighbour's middle, or its right-click menu's **Move up**, **Move down**, **Reset order**; Ctrl+1…6 follow the order. | Drag along a strip; "Move left/right" | The same preference, on a vertical list. |
| IA12 | **The desktop keeps its eight icons, ids, order, free placement, Arrange** (DK1-DK6 unchanged); only their look changes (§5.6). The Start button becomes **Menu**; "< Desk" becomes **Back to desk**; the taskbar's window buttons become the apps' glyphs (a tooltip names each), so no title is ever cut. | DK8's look | P2, P6; Windows 11 and macOS show open apps as icons. |

## 3. Layout grid

The desktop canvas is 1440 × 1080 units (u) drawn into the frame's glass: 1 u = 0.778 px at 1080p, 0.519 px at 720p. The grid is 8 u with 4 u steps inside controls.

| Part | Size | Notes |
|---|---|---|
| Taskbar | 60 u high | Menu (148 u), Back to desk (272 u: room for a culture's words over their English gloss), the window glyphs (56 u each), the tray (Day, Credits, Stability, the clock) |
| Compare dock | 64 u, right above the taskbar | unchanged role and behaviour; its texts at 24-26 u, two lines when a side is long |
| Window title bar | 44 u | title 26 u at 20 u from the left; three 52 × 44 u controls drawn as glyphs |
| Investigation toolbar | 68 u | 12 u padding; Back and Forward 44 × 44 u; the search field flexible; Accept 184 × 58 u, Deny 168 × 58 u (a culture's word over its English gloss) |
| Navigator | 308 u wide | 8 u side padding; case summary 96 u; a source entry 48 u; an item at least 44 u (indented 24 u, growing when its name wraps); the checklist follows; the whole column scrolls when it outgrows the window |
| Pane header | 64 u | "Papers  ›  Leisure Departure Visa" (the source regular, the item bold, at Caption size, wrapping to a second line rather than cut); Pin and Open beside or Close as 44 u icon buttons at its right |
| Two panes | (1440 − 308 − 8) / 2 = 562 u each, maximised (less half the 6 u gap) | a pane is never under `paneMinWidth` (520 u); a restored window (1120 u) has one pane; a scanned copy is drawn 520 u wide (it was 542 u) so a split pane holds its 558 u page whole |

## 4. Type scale and tokens

One family (the culture's font, LiberationSans in the neutral theme). Hierarchy by size and weight; the scale is tight (product register) and its floor is the 720p floor.

| Token | Units | 1080p / 720p | Weight | Used for |
|---|---|---|---|---|
| `Caption` | 24 | 18.7 / 12.5 px | regular | counters, meta lines, hints, placeholders, tray, badges' counts |
| `Body` | 26 | 20.2 / 13.5 px | regular | navigator entries and items, list rows, buttons, menu entries, checklist |
| `Title` | 28 | 21.8 / 14.5 px | bold | pane titles, section headings, window titles use 26 bold |
| `Heading` | 34 | 26.5 / 17.6 px | bold | a window's page heading (Mail's subject) |
| `Display` | 64 | 49.8 / 33.2 px | bold | the idle line on the office PC's clone |

The tokens are the builder's `PcType` and `PcSize` (`OfficeSceneUIBuilder.Tokens.cs`), with the knobs the runtime reads in `DesktopConfigSO` (the taskbar, title bar, dock and icon sizes). **Labels never shrink below `Caption`** (a keyed label's fit stops there); a label that can be long (a paper's name, a step, an order's name) wraps to a second line instead.

**Colour** keeps the theme system: every graphic is tagged by role and the culture's seeds colour it (no new role; no serialized enum changes). The palette map moves roles, not colours:

| Role | Before | After | Surface |
|---|---|---|---|
| `TitleBar` | chrome / chromeInk | **chromeDeep** / chromeInk | window title bars and their controls |
| `Taskbar` | chrome | **chromeDeep** | the taskbar (the tray already was) |
| `StartButton` | accent / accentInk | **chrome** / chromeInk | the Menu button |
| `Tab` | chrome / chromeInk | chrome / chromeInk (unchanged) | now the taskbar's window glyph plates and the window controls' hover plates |
| `Button` | face / faceInk | **white** / faceInk | every button: a plate that stands out on the paper and on the sidebar's warm grey alike |
| `Sidebar` | face / faceInk | unchanged | the navigator and the toolbar row |
| `Badge` | accent / accentInk | unchanged | the selected source, badges, chosen chips |
| `TabActive` | paper / ink | unchanged | the shown item under its source |
| `FocusRing` | accent | unchanged | the active pane's underline, "beside", the focus ring |

The neutral theme's seeds move off Windows XP: chrome `#2157DB` → `#33507A` (steel), chromeDeep `#1A52C7` → `#1C2636` (deep slate), accent `#2E7D32` → `#2F6BD8` (clear blue), face `#ECE9D8` → `#E2E0D6` (a warm grey under the paper), faceInk `#000000` → `#1C1B19`; its screen and claim strips follow the slate (`#1C2636CC`). The eight cultures keep their seeds (their chromeDeep already carried their tray text at AA). Glosses are no longer drawn (the `TaskbarGloss` and `TitleGloss` roles stay in the map, taken by no graphic).

## 5. Components

| Id | Component | Anatomy and states |
|---|---|---|
| C1 | **Window chrome** (`BuildOSWindow`, `BuildWinControls`) | chromeDeep title bar, the title at 26 u bold, left; minimise (a 14 × 2 u bar), maximise (a 14 × 14 u outline) and close (two crossed 18 × 2 u bars) drawn as rects in the title ink, on ghost buttons (hover and pressed tints). No text glyphs. |
| C2 | **Navigator entry** (`AppNav`) | 48 u row, the label at `Body`, the badge dot at its right; states: rest (face), hover (tint), selected (accent plate, accentInk label), beside (2 u accent outline), focused (the focus ring). |
| C3 | **Navigator item** | 44 u row indented 24 u, label at `Body` wrapping to two lines (the row grows to 64 u); shown: paper plate and bold; unavailable: 55 % alpha. |
| C4 | **Pane header** | 64 u, paper, a 2 u rule under it; "Papers  ›  Leisure Departure Visa" (the source regular, the item bold, at `Caption`; a long item wraps to a second line rather than being cut; an item named after its source, a record's "Citizen records · Mio", drops the repeat); Pin and Open beside (left pane) or Close (right pane) as 44 u icon buttons with hover hints; the active pane's 3 u accent underline. |
| C5 | **Search palette** | the field (white, `Body`, placeholder "Search papers, records, rules and the transcript"); under it a panel the field's width over the panes: before typing, "Pinned" and "Recent" lists (rows at `Body`, empty hints at `Caption`); typing, filter chips ("All", "Papers (3)"), group headings in full words, hits (title bold, snippet marked), "Show all 7 in Reference books". |
| C6 | **Checklist** | its heading row (48 u) is the fold's button: a chevron (down open, right folded), "Checklist" (bold, `Body`) and the count of steps done ("2 of 6", `Caption`, at its right end); rows: a 28 u tick box and the label at `Body`, wrapping. |
| C7 | **Taskbar** | chromeDeep, 60 u; Menu (chrome plate, `Body`); Back to desk (the accent plate, `Body`, its gloss under a culture's words); window glyphs (the app's desktop glyph, 32 u, on a Tab plate; focused: a 3 u accent bar under it; minimised: 50 % alpha; a hover hint names the window); the tray (`Caption`). |
| C8 | **Menus** (Menu, the context menu) | a chromeDeep-tinted panel, entries 48 u at `Body`, left-aligned with 20 u padding. |
| C9 | **Desktop icon** | 188 × 156 u cell (a seven-kana label fits one line): the glyph (72 u) over its label (`Caption`, up to two lines, never under it), both on translucent plates as before; the badge 34 u with its count at 22 u. |
| C10 | **Toast** | chromeDeep strip, `Body` text, an Open button. |
| C11 | **Empty states teach** | a pane between travellers: "Waiting for the next traveller" (Display in a single pane, shrinking to 40 u in a split one, as before); Papers with none scanned: "The papers the traveller hands over show here once they are scanned."; the palette with no pins: "Ctrl+P pins the row you are on, or use Pin in a pane's header." |

## 6. Interaction

- **Mouse.** Click a source: the active pane shows it. Ctrl+click: the other pane shows it (opening side by side when there is room). Click an item: the active pane shows that paper or book. Click in a pane: it becomes active (as before, the desktop's press). Right-click a source: Move up, Move down, Reset order. Drag a source up or down to reorder.
- **Keyboard** (the shortcut table is unchanged in its chords; its words change): Ctrl+F the palette; Ctrl+1…6 the source at that position; Ctrl+Tab and Ctrl+Shift+Tab the next and previous source; **↑ ↓ (and ← →) on the navigator's entries** step the sources; Tab walks the regions in the same order: search, its hits, the navigator's sources, the source's items and Pin, the pane's page, the other pane's page, the checklist and the palette's lists, the dock, Accept and Deny; F6 the other pane; Ctrl+\ side by side; Ctrl+B the navigator; Ctrl+Shift+S the checklist; Alt+← → back and forward; Ctrl+P pin; Ctrl+C copy; Esc the chain (the palette closes before the field clears).
- **What stays exactly as it was:** the index and matching, smart links, the compare pipeline and the dock, pins' and recents' scopes, the pane histories, zoom, copy and paste, the scan toast and badges, the Escape chain's order, the keyboard poller, and every Domain rule.

## 7. Before and after, screen by screen

| Screen | Before | After |
|---|---|---|
| Desktop | Blue XP taskbar, green italic "start", "< Desk", truncated title buttons; icons with 10-20 u labels | Slate taskbar: Menu, Back to desk, app glyphs, the tray at 24 u; icons with 24 u labels on a wider cell |
| Menu (Start) | 40 u entries | 48 u entries at 26 u |
| Investigation | title bar, case header, toolbar, two strips of six (abbreviated) tabs, two chip rows, sidebar of STEPS/PINNED/RECENT | title bar, one toolbar (Back, Forward, search, Accept, Deny), the navigator (case, six sources in words with their items, the checklist), one or two panes with a header each |
| Search | results with DOC/REC badges | a palette: Pinned and Recent before typing; results grouped under full source names |
| Mail | 16 u inbox label, 18 u rows | the same list and memo; the list and its headings at the scale |
| Internet | 20 u toolbar | toolbar at `Body`; the site pages (diegetic) unchanged |
| Citizen Account | forms | the same forms under the new chrome |
| Orders | 260 × 98 u nodes cutting names | 300 × 150 u nodes: names wrap (no ellipsis), prices and needs at `Caption` or more |
| Portals | a form | the same form under the new chrome |
| Notes | 14-16 u labels | the scale |
| Settings | 20 u rows, cramped pairs | the scale; each choice a full-width row of options |
| F1 card | 18 u rows | `Body` rows |

The after tour is `SCRATCH/pcux/after/shots` (the same job, the same states and cultures).

## 8. Decisions taken for Saleh (overnight; each reversible)

1. **Pinned and Recent live in the search palette**, not the sidebar (IA9). Reverse: move the two `SidebarEntryList`s back under the navigator.
2. **Taskbar window buttons are glyphs** with a hover name (IA12). Reverse: the template's label (kept in code) shows the title.
3. **Source names:** Papers, Citizen records, Reference books, Transcript, Deviation report, Today's rules (IA3); `ui.strings` `app.tab.*`.
4. **Neutral theme colours** moved from XP blue and green to slate, steel and a clear blue (§4); the eight cultures' seeds are untouched.
5. **"Menu"** replaces "start" and **"Back to desk"** replaces "< Desk" in every language table (the translations of "start" were replaced by each language's word for "menu"; "< Desk"'s by each language's "back to the desk"); "UI language" became "Language".
6. **Currency amounts keep "cr"** (a unit sign); every other word is spelled out ("not enough cr" became "you cannot afford it yet").
7. **The pane header's Pin, Open beside and Close are icons** with hover hints (not words), so the header's title fits one line in a split pane; the navigator's Ctrl+click and Ctrl+\ open the second pane too. Reverse: `HeaderIcon` back to a labelled button.
8. **Buttons are white plates** (the palette map's `Button` fill), so a button reads on the sidebar's warm grey and on the paper alike.
9. **The scanned copy is 520 u wide** (was 542 u; its form scales with it, a value still 17 px at 720p), so a split pane shows the whole page.
10. **The Internet's pages** take the PC's floor too: body 26 u, cells, notes and labels 24 u (they were 16-21 u).

## 9. Verification

compile_check 0 errors · the offline runner 0 failures · EditMode (TimeDeskEditMode) · Generate World twice (the second run changes nothing) and the validator · Build Office UI twice (the second rebuild equal by semantic dump) with its readability check (every text at AA in the nine themes) · the audit play (`tools/audit/unity/_TimeDeskAuditPlay.cs.txt`, driving the new navigation) days 1-3 · the after tour at 1080p and 720p in the neutral, Iraqi (Arabic) and Japanese themes. The golden masters that change are listed in the report (the audit's shots and its static UI dump; the play transcript is unchanged: no rule changed).
