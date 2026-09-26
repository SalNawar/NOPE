# The PC redesign: real desktop icons, one Investigation app, the Internet, and forms everywhere: design

*2026-09-26 · spec only (no code) · decisions by Claude under Saleh's answers below, finalized the same day with his answers to the three Q-items (§13), his translation change and his content-source note · read at `main` = `ff3a6e0` in `E:\unity\NOPE-feat-clock` (pieces 0–10, the readability pass) · built in the phases of `docs/superpowers/plans/2026-09-26-redesign-plan.md` (§11 maps them), each on its own branch from `main` · the code wins over this text*

Saleh, verbatim:
- "I want to improve the icons on the computer there are too many and it is confusing and too static. they should be real icons that I can move around. we need to group everything related to investigation in one icon once you open it it should be like an app with tabs and search bar and accibility feature for ease of access so the user can easily find what they need."
- "all the permits and all the documents and all the pages on the pc need to start to look like forms and actual documents not just rectangles there should be proper texts and formating on them."
- On the Internet: "player will be able to acess some website 1. news: here will be some daily news which will reflect the state of the world 2. history website will reflect history in the past. 3. anncestory will have info about some citizens from the past. 4. other websites might be added in the future."
- On the app: "reorgnize tabs, smart links can take you to refrences, you can copy paste, Smart links, Pinned + recent, Keyboard shortcuts".

His answers to the brief:

| Question | Answer |
|---|---|
| What the app holds | Everything for a case: the scanned documents, the reference books, citizen records, the transcript, the Deviation Report and the day's rules |
| Organisation | Tabs by source, a search bar, and steps |
| Steps | An optional checklist the player can turn on or off |
| Search | Global; results grouped by source; a click jumps there and highlights it |
| App size | Fills the PC screen: maximised in the close-up, with room for tabs, the sidebar and two panes side by side; can be restored to a window |
| Desktop icons | Free + auto-arrange: drop anywhere, positions saved, a right-click "Arrange icons" |
| On the desktop | The Investigation app, Internet, Mail, Citizen Account, Notes, Settings |
| Documents | All 2150 agency forms: form numbers, boxed fields, barcodes, fine print, a stamp area |

His answers to this spec's Q-items and the instructions that came with them (2026-09-26; §13 records where each is applied):
- **Q1, the Citizen Account icon:** the clerk's own account (it follows his answer to the traveller-types spec's Q6). A traveller's account lives in Investigation ▸ Records.
- **Q2, Archive Access:** removed. "instead have two versions of a scanner one auto scans and one manual scan but auto highlights a contradiction." (§1.13)
- **Q3, the text fallback:** delete it.
- **Translation:** "all documents must be filled in english. the translator should be for speech of other languages so you can comunicate with other traverlers if you dont have it most of the dialogue will be not in english but key words will still be in english enough for the player to understand some stuff in the dialogue. this we will tune later." (TR1-TR3, SE5, CP2)
- **Content source, later:** "we will create an excel file for all cases later with all possible dialogue options, all possible dialogue interactions, all real characters, all premade characters and stories etc" (CT1)

**The art side's concept.** Branch `art`, `ArtDeliverables/TimeDesk/PaletteExploration/Palette20/MOOD_AND_LAYOUT.md` (2026-09-26): the office is a debt-relief building where travel to the past is "presented as an opportunity to pay their debts". Its PC shows a "Citizen Account" window (ID 773-2840-19, Status Eligible, Balance 125,430 cr) and the icons Records, Travel and Forms. The same file calls its account numbers and emblem "proposed visual storytelling, not approved game lore". This spec takes the layout and the tone. Saleh made the account the clerk's own (Q1).

**The parallel spec.** `2026-09-26-traveller-types-design.md` covers tourists (rich with 2 papers, poor with 4), labourers (3) and the displaced (3), transponders and waivers, dress for the destination, debt lies, strandings and the clerk's own debt. Both specs were finalized together with Saleh's answers, and §12 aligns them: its ten forms, records, checklists and news lines are what this spec's forms, Records view, steps and News site show. Throughout, a case's document set is data: the app shows whatever documents a case has (1 to 6 kinds, any fields).

**Names.** "u" is a desktop canvas unit. The desktop is 1440 × 1080 u, drawn into the PC frame's 1120 × 840 glass, so 1 u = 0.78 px at 1920 × 1080 and 0.52 px at 1280 × 720. "H" is a form page's height, the unit form text is sized in (§6). "The frame" is the PC close-up (`PcFrame`); "the clone" is the desktop drawn onto the office PC's glass (`PcScreenClone`).

## 0. What `main` does today (verified at `ff3a6e0`)

| Surface | Today | Where |
|---|---|---|
| The desktop | A World Space canvas of 1440 × 1080 u on its own layer. The frame camera draws it into the frame's glass, and the clone camera renders it into a 1024 × 768 texture on the office PC. | `OfficeSceneUIBuilder.Desk.cs:44, 58, 269-309`; `PcFrame`; `PcScreenClone` |
| Icons | **17 text-only tiles** (82 × 60 u) in a fixed two-column `GridLayoutGroup` at the left. **9 apps:** Directives, Deviation Report, Records, Clue Log, Internet, Lexicon, Dialect, Material, Notes. **6 reference books.** **Each scanned document** of the case, added at the top and cleared per case. A builder icon opens its window; a runtime icon toggles it (R4-017). Lexicon and Material stay dimmed until Archive Access / Advanced Scanner is owned. | `OfficeSceneUIBuilder.cs:242-246, 1425-1444`; `InvestigationUIController.cs:719-778`; `DesktopIcon` |
| Windows | Each has a draggable 30 u title bar and min/max/close buttons. Minimise is the same as close. Maximise goes to hard-coded insets (R4-015). Only a press on the title bar brings a window to the front. There are no taskbar buttons and no focus. | `DraggableWindow` (dead members: R4-016), `OSWindowChrome` |
| Case UI on the desktop | A dim over the wallpaper, the claim strip at the top, and Accept/Deny at the bottom. The compare bar sits at 27–34 % of the height, over every window. Windows cascade from fixed origins. | `OfficeSceneUIBuilder.cs:232-341` |
| Windows opening themselves | Every new case closes all windows (`:409`). A scan opens its document window (`:630-642`). Every wheel choice except a request or a look opens the transcript (`:596`). The first proof opens the Deviation Report (`:289`). | `InvestigationUIController` |
| Documents | Plain label and value rows on a cream page over a dark backing, paged with Prev/Next. The desk paper uses `PaperFace`: a title band, label-over-value rows and the photo. | `DocumentWindowController`, `PaperFace`, `DeskDocument` |
| Books and transcript | Paged lists: 6 book rows a page, 8 transcript lines a page. | `PagedRowsWindow` (its paging is copied in `DocumentWindowController`: R4-007) |
| Records | Type a name and press SEARCH to get one record. Name and Born are pickable, but the pick key ignores which record is shown (R4-009). | `CitizenRecordsWindowController` |
| Placeholder apps | Internet ("Today's news feed. (placeholder)"), Lexicon, Dialect, Material and Notes: static text windows. | `world_source.json` `body.*` |
| Start and taskbar | Start (Settings, Turn off screen, Quit game), "< Desk", and the tray (Day, Credits, Stability, Clock). | `DesktopShell`, `BuildTaskbar` |
| Theme | Every chrome graphic is tagged by role. The diegetic roles (`role >= DiegeticPaper`) are never themed, so a role appended after `DiegeticDevice` would count as diegetic. The desktop icon labels are 7 of the 34 flavour labels (in the culture's language). | `ThemeRoles.IsDiegetic`, `world_source.json` `ui.languages` |
| Escape | Five components poll Escape. "One press does one thing" only holds because of BoothRules, and nothing tests it. | R5-005 |
| Compare | One `ComparePair` and one `CompareController` drive the PC bar and the office strip. Picks come from `EvidencePicks`, keys from `PickKeys`, proofs from `DiscrepancyLog.Prove`. | piece 10 E4, X19, X20 |
| God class | `InvestigationUIController`: 992 lines, 8 jobs, and a text fallback no scene can reach. | R4-001, R4-002 |

## 1. Decisions

Claude made every row, working from the code above; Saleh's answers to the Q-items are applied in them (§13).

### 1.1 The desktop (DK)

| Id | Decision | Why |
|---|---|---|
| DK1 | **Six icons, nothing else, ever:** Investigation (`investigation`), Internet (`internet`), Mail (`mail`), Citizen Account (`citizen_account`), Notes (`notes`), Settings (`settings`), in that default order. No icon is added at run time: a scan, a book or a case adds nothing to the desktop. | "too many and it is confusing". Everything for a case moves into one app (AP1). A desktop that never changes is one the player can arrange once. |
| DK2 | **Icon anatomy.** A cell of 120 × 132 u holds the glyph (72 × 72 u at the top: 56 px at 1080p, 37 px at 720p) over its label (20 u, at most two lines, centred) on a translucent plate in the `DesktopIcon` role, so it reads on any wallpaper. The glyph is the asset list's `Assets/Art/UI/Desktop/icon_<id>.png` (128 × 128, greyscale, tinted by the theme), with a code-drawn placeholder until the art lands. A badge (a 28 u circle in the new `Badge` role, with a count) sits at the glyph's top right: unread mail on Mail, a dot on Investigation when something arrived while the app was closed or minimised. The selected icon gets a plate in the new `IconSelection` role, and the hover outline stays `HoverHighlighter`'s. | An icon is a picture with a name, not a text tile. The plate keeps the label at AA contrast over the eight culture wallpapers (the readability rule). |
| DK3 | **Free placement.** Press on an icon and move past the EventSystem's drag threshold to drag it: it follows the pointer, keeping the grab offset, drawn above the others. On release, `DesktopLayout.Drop` clamps it inside the icon area (the desktop minus the taskbar and the compare dock, 1440 × 988 u). If it covers more than 25 % of another icon's cell, it moves to the nearest free arrange spot. Otherwise it stays exactly where it was dropped. | "drop anywhere". The nudge only stops icons stacking, which would hide one. |
| DK4 | **Positions are saved per player** in PlayerPrefs `TimeDesk.DesktopIcons` (`id:x,y;…`, invariant numbers, desktop units), the `UiLanguagePreference` pattern, so New Run keeps them. On load, unknown ids are dropped, a new id takes the first free arrange spot, and an off-screen position is clamped. | "positions saved". A desktop layout is the player's preference, not run state. |
| DK5 | **Arrange icons.** A right-click on the empty desktop opens a context menu with "Arrange icons". The same entry is in the Start menu, and Settings has "Reset icon positions". All three lay the icons out column-first from the top left (origin 20, 20 u; a 132 u column step and a 140 u row step) in the default order, and save. | "a right-click 'Arrange icons'". Three ways in, one rule (`DesktopLayout.Arrange`). |
| DK6 | **Double-click opens.** A single click selects, a double-click opens, Enter opens the selected icon, and the arrow keys move the selection to the nearest icon in that direction. Settings has "Open desktop icons with: Double click / Single click" (default Double). The double-click test is `ClickTiming.IsDouble` (0.4 s and 6 u, knobs in `DesktopConfigSO`), not the input module's `clickCount`. | "real icons": a real desktop opens on a double-click, and a single click must be free to select before a drag. The single-click option is the accessibility escape hatch. A pure rule makes the timing tested and module-independent. |
| DK7 | **Today's 17 tiles go** (table below). Their content moves into the app's tabs. The Lexicon, Dialect and Material placeholders retire. Internet and Notes become real apps. `DesktopIcon.requiredUpgradeId` goes with the icon gate: no desktop icon is ever dimmed, and no site is gated (IN2). | Every tile has a new home. Nothing is lost. |
| DK8 | **Start menu:** the six apps (a second, keyboard-friendly way to open them), then "Arrange icons", "Turn off screen" and "Quit game". The Settings entry is replaced by the Settings app in the list. **Taskbar:** Start, "< Desk", one button per open window (glyph and title, at most 200 u each, shrinking to fit; the focused window's button shows pressed; badges as DK2), then the tray (Day, Credits, Stability, Clock) as today. | The Start menu stays the launcher. Taskbar buttons are how a real OS shows open and minimised windows (WN1). |
| DK9 | **The compare dock:** the PC's compare bar moves out of the case UI into a strip docked above the taskbar (1440 × 56 u), outside every window and never covered by one. It shows while a traveller is at the desk: empty, it reads "Click two values to compare them" in dim ink; otherwise side A, the verdict and side B (CM2). A maximised window stops above it, so nothing jumps when a pick appears. | Today the bar floats at 27–34 % over the windows. The dock is always visible, including when the app is restored, minimised or closed, and while papers held beside the frame are picked (piece 10 X10). |
| DK10 | **The idle line** ("Waiting for the next traveller", 80 u, for the clone) shows on the wallpaper only when no traveller is at the desk and no window is open. Inside the app, the no-case state shows the same line (AP8). | With icons free and the app maximised, a strip across the middle would cover icons or hide under the app. |

| Today's tile | Its new home |
|---|---|
| Directives | Investigation ▸ **Rules** tab (a directive memo form), and the day's memo in Mail |
| Deviation Report | Investigation ▸ **Report** tab (Form TC-930) |
| Records | Investigation ▸ **Records** tab (the record extract form) |
| Clue Log (the transcript) | Investigation ▸ **Transcript** tab (an interview record form) |
| The 6 reference books | Investigation ▸ **Reference** tab, one book at a time (a register form), with its cover on the book's chip |
| Each scanned document | Investigation ▸ **Documents** tab (the document's own form) |
| Internet | The Internet app (IN1) |
| Notes | The Notes app (NT1) |
| Lexicon ("Wikipedia-style era glossary", placeholder) | Retired; the idea is the History site (IN4) |
| Dialect, Material (placeholders) | Retired; the Advanced Scanner upgrade gets a real job as the Analysis Scanner (§1.13) |

### 1.2 Windows, focus and the frame (WN)

| Id | Decision | Why |
|---|---|---|
| WN1 | **One window stack** (Domain `WindowStack`) owns every desktop window's z-order, focus, minimised state and taskbar order. `Open` shows and focuses a window (a minimised one restores). `Focus` raises it. `Minimise` hides it and keeps its taskbar button, then focuses the next visible window. `Close` removes it. `TaskbarClick` minimises the focused window, else restores and focuses. The view applies `ZOrder` as sibling order on the window layer. | Today there are four show/hide paths (R4-017), minimise is close, and there is no focus. One tested model replaces them. |
| WN2 | **A press anywhere in a window focuses it**, not only its title bar. At each press, the window manager raycasts the desktop canvas (the frame camera's `GraphicRaycaster`) and focuses the top window under the pointer. Keyboard shortcuts go to the focused window (KB2). | A button inside a window takes the pointer-down itself, so a parent handler never sees it. The manager's own raycast is how real toolkits focus. |
| WN3 | **Maximise** fills the icon area exactly (1440 × 988 u, above the dock), with no insets. A double-click on a title bar toggles it. A maximised window does not drag. Title bars are 36 u, with titles at 20 u. | R4-015. The larger bar reads at 720p (10.4 px). |
| WN4 | **Sizes.** Investigation and Internet open maximised; restored, they are 1120 × 820 u. Mail is 920 × 720, Citizen Account 720 × 780, Notes 780 × 720 and Settings 640 × 720 (knobs in `DesktopConfigSO`). A window remembers its restored rect and maximised state for the office session (not saved). Windows are not resizable. | "maximised in the close-up … can be restored to a window". Fixed sizes keep every layout checkable by the builder (the readability check needs known sizes). |
| WN5 | **Nothing steals the view.** A new case closes no window. The app's case content changes, and pins and panes behave as AP8 and PR2 say. A scan badges the Documents tab and shows a toast ("Leisure Departure Visa scanned ▸ Open"). A new transcript line badges the Transcript tab. A proof shows DEVIATION LOGGED in the dock with "Report ▸" and badges the Report tab. The only thing that opens by itself: a scan opens the app window if it is closed, and shows that document in the left pane only if the pane shows no document yet. The frame still never opens by itself. | Today four events open or close windows under the player (§0), and in a tabbed app that would switch what they are reading. Badges and toasts say where to look without moving anything. A scan still ends up on the PC, as piece 10's flow expects. |
| WN6 | **Inside the frame.** Nothing changes about the frame: its 4:3 glass, the click-outside catcher, the examine hole beside it (piece 10 X10) and the power button. Icons and windows are clamped to the desktop, and a drag that leaves the glass keeps mapping the pointer and is clamped. The mouse wheel scrolls the pane under the pointer (the desk view's wheel is not live while the frame is open: `DeskViewBackLive` needs `!Focused`). A right-click opens context menus on the desktop only. Desktop keyboard shortcuts work only while the frame is open (`BoothRules.DesktopInteractive`). | The frame already routes pointer input through the frame camera, so the desktop needs nothing new from the office. |
| WN7 | **On the clone.** The office PC shows the desktop exactly as it is: the maximised app, the dock and the toasts. The app's no-case state is drawn large (80 u) so the office PC reads it from the chair. Pausing the clone camera while the frame covers the office PC (R5-014) stays a separate item. | One desktop, two cameras, as today. |

### 1.3 The Investigation app (AP)

| Id | Decision | Why |
|---|---|---|
| AP1 | **One app, one window: "Investigation".** It holds every case source as a tab: **Documents, Records, Reference, Transcript, Report, Rules.** Its title bar reads "Investigation · {traveller's display name}", and the case header shows the claim (the one `UiText.Format("claim.banner", …)` text that the office claim tag also shows), the counters "Papers 2 of 3 received · 1 scanned · Deviations 1", and the PC's Accept and Deny buttons (their roles, fixed glyphs and gloss unchanged). | "group everything related to investigation in one icon". The claim banner, Accept/Deny and the counters leave the desktop with the rest of the case UI. |
| AP2 | **Layout, maximised (1440 × 988 u):** title bar 36 · case header 72 · toolbar 52 (Back, Forward, the search field, the Steps and Split toggles, "F1 Keys") · body 828. The body holds the **sidebar** (272 u: Steps, Pinned, Recent) and **two panes** (about 580 u each, with a 6 u divider). Each pane has its own tab strip (44 u), pane header (40 u: the item chooser) and content (744 u). The wireframes are in §2. | "room for tabs, the sidebar and two panes side by side". A 580 u pane fits a whole document page at a readable size (§6.3). |
| AP3 | **Each pane has its own tab strip; the tab order is shared.** A click or Ctrl+1…6 switches the *active* pane (the last one clicked or focused, drawn with a 3 u accent border). When a strip is too narrow for every label, inactive tabs collapse to glyph and badge (48 u), the active tab keeps its label, and tooltips name them all. The split turns off automatically when the window is narrower than sidebar + 2 × 520 u (so a restored window has one pane; the Split toggle shows why). | Two panes side by side are the point, and each must be able to switch sources on its own. A shared order keeps one arrangement. A pane never gets narrower than a readable form (§6.3). |
| AP4 | **Reordering tabs:** drag a tab along its strip past a neighbour's middle, or press Ctrl+Shift+PgUp/PgDn. A tab's context menu has "Move left", "Move right" and "Reset tab order". The order is saved per player (`TimeDesk.AppTabs`) through Domain `TabOrder`, and Ctrl+1…6 follow positions, not names. | "reorganize tabs". Positions are the browser convention. |
| AP5 | **The six views** (§2.4–§2.9), all forms (FO9): **Documents:** a chip per document of the case; a scanned one shows its form, and one on the desk or not yet handed over shows its outline with a hint. **Records:** a lookup by name or number, and the record's extract. **Reference:** a chip per book (its cover) and the book's register of today's places, with the claimed place's row first and outlined, and a "Claimed place only" toggle (on at case start). **Transcript:** the interview record, one scrolling table, newest at the bottom, with an "Answers only" toggle. **Report:** the Deviation Report form. **Rules:** the day's directive memo. | "Everything for a case", each in the shape its paper would have. |
| AP6 | **A document reaches the app when it is scanned**, as it reaches the PC today. Its chip exists from the start (the wheel's requests already name every document), labelled "On the desk: scan it to read it here" or "Not handed over: ask for it" until then. | Piece 10 keeps the scanner as the way papers reach the PC. The desk stays the place to read an unscanned paper. |
| AP7 | **Lists scroll, pages do not flip.** The transcript, a book's register, the report and the rules scroll in their pane. A multi-page document shows its pages one under the other, with "Page 1 of 2" rules between them. `PagedRowsWindow`, and `Paging` if nothing else calls it, retire. | Search and smart links jump to a row, and a pager would hide it on another page. Scrolling also removes the duplicated paging (R4-007). |
| AP8 | **Case start and end.** At case start the case layer resets (the index, case pins and recents, the steps). The left pane shows Documents. The right pane keeps its source, and a Reference pane turns "Claimed place only" on for the new claim. At the decision, the case sources show the no-case state: "Waiting for the next traveller", 80 u across the pane. Day sources stay as they are. | A player who arranged Reference beside Documents keeps that for the whole shift. |
| AP9 | **Pane history.** Each pane keeps a back/forward stack (Domain `NavHistory`, 30 entries): every tab switch, item switch, search jump and link jump pushes to it. The toolbar's ◀ ▶ and Alt+←/→ act on the active pane. | Smart links "take you to references", and Back takes you home again. |

### 1.4 Steps (ST)

| Id | Decision | Why |
|---|---|---|
| ST1 | **The sidebar** has three sections: **Steps**, **Pinned** and **Recent**. Ctrl+B hides the whole sidebar (the panes widen). The Steps section has its own on/off switch (the toolbar's "Steps" toggle, Ctrl+Shift+S, and Settings), saved per player (`TimeDesk.StepsShown`, default on). | "a sidebar that can be toggled" and "an optional checklist". |
| ST2 | **A step ticks when the player has made the check, never on what the check found.** "Check the currency against the reference" ticks when a pair of (a Currency statement, a Reference row) is compared, whether it showed MATCH or MISMATCH. A step with parts shows progress ("Facts 1 of 3"). The player can tick or untick any step by hand, and a manual state holds for the rest of the case. | A checklist that turned green only on the right answer would give the answer away. |
| ST3 | **Steps are data per traveller type.** `world_source.json` `pc.steps.sets[]` has `{type, inherit, steps[]}`, and each step is `{id, when, jump}`. The `when` vocabulary is a small tested Domain set (§4.4): `PapersReceived`, `PaperRead`, `Requested` (each with optional form kinds), `RulesViewed`, `RecordViewed`, `Compared(categories, statement, truth)`, `Asked(categories)` and `LookedAt`. Its labels are `ui.strings` keys `steps.<id>` (Full tier). The set used is the case's `TravellerKind`. The traveller-types spec's checklists (its §5.1) are the sets: `RichTourist`, `PoorTourist` (inheriting it), `Labourer` and `Displaced` (the famous displaced too). `default`, below, serves until that spec lands. | "per traveller type" (the brief). New checks are new rows, not code, unless a new kind of event is needed. |
| ST4 | **A click on a step jumps to where it is done**, in the active pane, the way a search result does. For example, "identity" looks up the primary form's name in Records, and "facts" opens the first unchecked field's smart-link target. A step whose work happens at the desk (asking for papers or questions) shows a toast hint instead ("Use the traveller wheel"). | "click to jump". |

The default set:

| Id | Label (`steps.<id>`) | Ticks when | Jump |
|---|---|---|---|
| `rules` | Check today's rules against the destination | `RulesViewed`: the Rules tab shown in a pane while the traveller is at the desk | Rules |
| `papers` | Receive every paper | `PapersReceived: all` (progress n of m) | Documents |
| `read` | Read every paper | `PaperRead: all`: each examined at the desk or shown in a pane (n of m) | Documents |
| `identity` | Check the name and date of birth in Records | `Compared {Name, BirthDate} × Record` | Records, the primary form's Name looked up |
| `facts` | Check each place fact against the reference | `Compared {each place-fact category on the papers} × Reference` (n of m) | the first unchecked field's link target |
| `questions` | Ask today's questions | `Asked {each category of InterviewDay.Questions}` (n of m) | toast: "Use the traveller wheel" |
| `answers` | Check the answers against the reference | `Compared {each answered category} × Reference`, the statement being an answer | Transcript |
| `dress` | Check the dress against the Costume Guide | `LookedAt` and `Compared {Culture} × Reference` | Reference ▸ Costume Guide, the claimed row |

### 1.5 Search (SE)

| Id | Decision | Why |
|---|---|---|
| SE1 | **One search field** in the toolbar (Ctrl+F). Results update as the player types (from 2 characters, or 1 digit; 150 ms debounce) in a drop-down panel over the panes, **grouped by source in the tab order**. A group shows its first 5 hits and "Show all n in Reference"; source chips in the panel's header filter to one source. Each hit shows its title ("Departure Manifest · Currency Carried"), a snippet with the matched text marked, and its source glyph. | "Global; results grouped by source". |
| SE2 | **The index has two layers** (Domain `CaseIndex`). **Day layer** (rebuilt at day start): every Reference row of today's places, every record in today's registry, and every rule. **Case layer** (reset per case, updated on events): each scanned document and each of its fields, each transcript line as it is spoken, and each deviation as it is logged. Not indexed: the Internet, Mail and Notes (the browser has its own site search), and other travellers' cases. The full table is §4.2. | Search covers what the app shows. A document joins when it reaches the PC (AP6). |
| SE3 | **Matching** (Domain `TextMatch`). Case- and accent-insensitive (lower-case, Unicode decomposition with the marks dropped, punctuation read as spaces). Each query word must be the start of a word in the entry ("drach" finds "Drachma"), and a quoted phrase must appear as written. **Ranking:** the whole value equal to the query, then words in the title, then in the value, then in the label; ties go by source order, then item order. The marks are mapped back to the unfolded text. No fuzzy matching. | Predictable, testable, and good enough for short values. Accent folding matters for names like "Ḥatnefer". |
| SE4 | **Jump and highlight.** Enter or a click opens the hit in the active pane; Ctrl+Enter or Ctrl+click opens it in the other pane. The view selects the item (turning a filter off if it hides the row), scrolls the row to the middle, gives it keyboard focus, and flashes it: two pulses of the form style's `found` colour over 1.2 s, then a steady 2 u outline until the next navigation or click. Reduced motion shows the outline only. The found outline is not the compare highlight: a different colour, and a pick is still the player's own click or Space. | "clicking one jumps there and highlights it". Focus on the row means Space picks it next. |
| SE5 | **Untranslated speech (piece 9, now speech only).** Documents are always English (Saleh: "all documents must be filled in english"), so only transcript lines can show untranslated: a displaced traveller's line, without the region's Speech translator. Such a line is indexed by what it shows: its English key words (the traveller-types spec's §8.1) as text, and its glyphs for the snippet, never by its hidden English. **A typed query matches only the key words and the speaker**, and the hit shows the glyphs. **A pasted foreign clip** (CP2) matches only untranslated lines of the same tongue whose canonical text equals it. A line is settled when spoken (translated when the Speech translator was owned at the start of the day), so nothing is re-indexed later. The hit's snippet is written through `TextFlip.Write`, so glyphs draw in the script's font. | Otherwise search would translate for free: a clip matched against English rows would give the English of a line the player cannot read, and the Speech translators would lose their point. "These two untranslated lines are the same" was already visible on the page, so matching glyph to glyph leaks nothing. |
| SE6 | **The Records lookup is the index, scoped to Records.** The pane's name field runs `CaseIndex.Search` restricted to Records and opens the best hit. `CitizenRegistry.Find` retires when the lookup moves to the index (its other caller, the text fallback, is deleted earlier: Q3). The traveller-types spec's R3 (find by agency number) is the same rule: a query equal to a record's number or name ranks first (the whole-value rank). | One text matcher, so "search finds her but Records says NO RECORD" cannot happen. |

### 1.6 Smart links, copy and paste, pins, keys (LK, CP, PR, KB)

| Id | Decision | Why |
|---|---|---|
| LK1 | **Smart links** (Domain `SmartLinks`, table in §4.3) point from a statement to where it is checked. A place-fact field goes to its book's claimed-place row. Name, Date of Birth and every record category (the traveller-types spec's Citizen ID, class, transponder, debt, credit, funds, policy, contract, waiver and incident rows) go to the traveller's own record in Records (looked up by the document's Citizen ID, else its Name), at that category's row. Destination goes to the record's booked departure (or the displaced's registered origin), and a departure date, a Valid Until or a signature goes to Rules. An answer goes to the same targets as a field of its category, a garment to the Costume Guide's claimed row, a rule to the Reference filtered to its place, and a deviation to both its sides. A revised Reference row links to its History article (IN4). A category with no target shows no link. | "smart links can take you to references". |
| LK2 | **How a link looks and works on a form.** The value keeps its pick action: a click on it (or Space) picks it for compare. After the value is a small ↗ glyph (a drawn sprite, 28 u hit box) with a dotted underline in the form's `link` ink and a hover hint ("Open Currency Ledger ▸ Classical Athens"). Clicking ↗ or pressing Enter follows the link into **the other pane**; Ctrl+click and Ctrl+Enter stay in the same pane (with the split off, the same pane and Back). A link never picks anything. | The link's natural result is the two sides side by side, ready to pick. Picking both sides stays the player's act, the core of the game. |
| CP1 | **Copy.** Ctrl+C on the focused row, or "Copy value" in the row's context menu, copies the value as shown. Ctrl+Shift+C, or "Copy row", copies "Label: value". Every row with an entry key can be copied, in any tab and on the Internet's pages. | "you can copy paste". |
| CP2 | **One clipboard** (Domain `AppClipboard`) holds one clip: the shown text, its source key and label, and for an untranslated transcript line (or the bubble's untranslated answer) its tongue and canonical text. The canonical text is used only by SE5's matching and is never displayed or pasted. The clip's text also goes to `GUIUtility.systemCopyBuffer`, so TMP fields paste it the normal way. | A copy keeps where it came from. The system buffer means no custom paste code in text fields. |
| CP3 | **Paste targets:** the search field (a foreign clip becomes a chip, "Transcript line 7 · untranslated Greek", and matches as SE5; otherwise plain text); the Records lookup field; the browser's site search fields; and Notes (a clipping card with its source link, NT1). **Not into the compare.** A pick must come from its source row, so its evidence stays canonical and traceable. A pasted clip in Notes links back to the row, where it can be picked. | Pasting into the compare would be a second way to pick, and one that could not show where a value came from. |
| PR1 | **Pin** any item with an entry key (a document, a field, a book, a Reference row, a record, a rule, a line, a deviation): Ctrl+P, the row's context menu, or the pane header's pin button (it pins the pane's current item). Pinned items are listed in the sidebar; a click jumps (SE4), and a right-click unpins. At most 20 pins. | "Pinned". |
| PR2 | **Scopes.** Case items (documents, fields, lines, deviations) are unpinned when the case ends. Day items (books, Reference rows, records, rules) stay until the day ends. Pins are kept for the office session and not saved. **Recent** lists the last 10 items opened or jumped to, newest first, with the same scoping. | A case item points at a traveller who has left. Today's code already planned "a pin system" to keep chosen windows across cases (`InvestigationUIController.cs:407`); day pins are that. |
| KB1 | **Keyboard shortcuts** (the full table is §3.4). Ctrl+F search · Ctrl+1…6 tabs · Ctrl+Tab next tab · Ctrl+\ split · Ctrl+B sidebar · Ctrl+Shift+S steps · Alt+←/→ back and forward · F6 other pane · Tab/Shift+Tab regions · arrows, Home, End, PgUp/PgDn inside a list · Enter follows · Space picks · Ctrl+C/Ctrl+Shift+C copy · Ctrl+V paste · Ctrl+P pin · Ctrl+= / Ctrl+- / Ctrl+0 zoom · Esc the chain (KB3) · F1 the shortcut card. | "Keyboard shortcuts". |
| KB2 | **One keyboard poller** (`DesktopKeyboard`) reads the Input System's `Keyboard.current`, only while the frame is open, and resolves chords through Domain `ShortcutMap.Resolve(chord, context)`. The context is: frame open, app focused, a text field focused, a menu open, the search panel open. While a text field has focus, only chords that cannot be typing pass (Ctrl+F, Ctrl+1…6, F6, Ctrl+\, Ctrl+B, Esc, F1); the field keeps its own Ctrl+C/V/X/A. | One tested map instead of shortcuts scattered over components (the R5-005 lesson). |
| KB3 | **Escape runs one chain.** In order: a context menu, the shortcut card, the search panel, a search field's text (cleared), a focused field (left), the Start menu, an icon drag (cancelled). Only when the desktop has none of these does the frame close (`OfficeViewController`), and then the office's own order continues as today. `DesktopEscapeRule.Resolve` is pure and tested. The desktop poller takes the press when the rule returns something, and the frame's Escape needs the desktop not to have taken it this frame (the stamp pattern of piece 10 X8). | A press that clears the search must not also close the frame. This is the desktop's part of R5-005's single Escape arbiter. |
| KB4 | **Focus is visible and reachable.** Tab moves through the regions: search → the active pane's tab strip → its pane header → its content → the other pane → the sidebar → the dock → Accept/Deny. Inside a list or form, the arrows move a 3 u focus ring (new `FocusRing` role, the 3:1 outline class) over the rows in reading order (`FormLayout` gives that order). Every mouse action has a key. | "accessibility feature for ease of access". |
| KB5 | **Zoom.** Ctrl+= / Ctrl+- / Ctrl+0 scale the panes' content to 100, 125 or 150 %, scrolling both ways. The default is Settings' "Text size" (`TimeDesk.AppZoom`). | At 720p the whole PC is 747 × 560 px, and a player who needs larger text needs more than the 720p floors (§6.3). |

### 1.7 Compare (CM)

| Id | Decision | Why |
|---|---|---|
| CM1 | **One compare pipeline, unchanged in its rules.** `ComparePair`, `CompareController`, `EvidencePicks`, `PickKeys` and `DiscrepancyLog.Prove` stay as they are, and the two panes are only two views. A row picked in either pane, on a held paper, in the bubble or through the Look menu is one pick; the same key again clears it; a pair proves through the one `PairCompared`. The traveller-types spec's paper-against-paper proof (its L4; Saleh's Q3 answer: papers prove, answers hint) lives inside `Prove`, so the app needs nothing for it. | "one compare pipeline, no duplicates" (brief). The panes are what makes it side by side. |
| CM2 | **The dock is the PC's bar** (DK9). `CompareController.compareBar` and `compareText` are rewired to it; the desktop's old CompareBar object goes. The dock draws side A, the verdict (MATCH, MISMATCH, DEVIATION LOGGED, ALREADY DOCUMENTED, as today) and side B. Each side's label is a link to its source (its pick key → `SmartLinks.ForKey`), and ✕ clears. A pick from a held paper that is not scanned links nowhere. The office strip is unchanged and still shows while the frame is closed. | The same text and colours on both bars, as today (`WriteBars`). A side you can click back to is the answer to "where did I pick that?". |
| CM3 | **Highlights follow keys, not objects.** `CompareController` gains `IsPicked(key)` and a `PicksChanged` event. A form row that is drawn, or redrawn after scrolling, a pane switch or zoom, shows the pick fill when its entry key is picked, in every pane that shows it. Desk rows and the bubble keep their `ICompareHighlight` objects. | Today `ImageHighlight` holds the row's `Image`. After a page flip the new row has no highlight, and a value shown in both panes would light only once. |
| CM4 | **The desk is unchanged.** Held papers, the office strip, the frame region beside the open frame (X10) and its hole all behave as piece 10 and the readability pass left them. Held rows pair with app rows as PC rows do today. (R5-001, the drag-out beside the open frame, is fixed by the audit hotfix slice on `overhaul/audit`.) | The desk is not part of this redesign. |
| CM5 | **The Deviation Report no longer opens itself** (WN5). The report's items are form rows linking to both their sides (LK1), and each deviation is indexed (SE2). | The dock already says DEVIATION LOGGED, and the Report tab is one click away. |

### 1.8 Internet (IN)

| Id | Decision | Why |
|---|---|---|
| IN1 | **A browser app**, opening maximised. Its chrome (themed): ◀ ▶ ⌂, an address field showing the page's address (`chronet://times.tc/day-3`; typing a site's name or picking it from its drop-down navigates), and the site's own search field when the site has one. Home is a portal of the available sites as tiles (glyph, name, one line). Links in pages navigate, with a back/forward stack (`NavHistory`, as AP9). | "player will be able to acess some website". A fake address bar says "web" at a glance without a real URL parser. |
| IN2 | **Sites are data.** `world_source.json` `pc.sites[]` has `{id, kind, name, domain, glyph, blurb, fromDay}`. `kind` picks a page builder: `news`, `history`, `ancestry` or `static` (authored pages of blocks, `pc.pages[]`). Generate World validates ids, kinds and days; the builder's check of icon upgrade ids goes with the icon gate. A site before its `fromDay` is not listed; every listed site is open. **Adding a site** means a JSON entry with `static` pages, or a new builder kind (one class and its tests) for generated content. | "other websites might be added in the future". Archive Access, the only planned gate, is removed (Q2), so no site carries an upgrade gate; one field brings it back if a site ever needs one. |
| IN3 | **News: "The Temporal Times".** Today's front page shows the same lines the morning newsletter showed (`world.tomorrow.briefingLines` and `newsLines`, the dominance, history and trigger news). The first news line is the lead. HISTORY lines go under "World". The notices (briefing lines) sit in a boxed column, with a "Timeline standings" box from `HistoryState.ranking`. The day's debt-economy line, yesterday's counts and the strandings (the traveller-types spec's `news.debt` and `news.stranded` pools, its §10) arrive as ordinary news lines, so the site has no filler of its own. **Back issues:** at each briefing the day's lines are appended to `WorldState.newsArchive` (at most 30 issues), and the "Archive" page lists them. | "daily news which will reflect the state of the world", reusing the morning paper's data. The lines are not recomputable once the night rebuilds them, so the archive records them. |
| IN4 | **History: "Chronopedia".** An index of the 8 countries × 6 eras, and an article per place, generated from existing data only: the display name, year and `moment`, and an infobox (form-style boxes: Capital, Ruler, Currency, Language, Technology, Dress). Values are as they stand today (`History.Resolve`), and each revised one is marked "Revised on day N (was X)". A **Revisions** page lists every latched edit (`HistoryState.factEdits`: the day, place, category, before and after, and the cause's name) and every carry, newest first. It covers every place in the world, not only today's, and **the present** (2150: `TodaysWorld.Present`, the leader's Future place or the neutral present, per the traveller-types spec's H1 and H2). Values are not compare-pickable. An infobox value whose place is in today's table links to Investigation ▸ Reference (LK1). | "history website will reflect history in the past", as it stands after history changed. The books stay the one evidence surface; the site is the context and the "why". |
| IN5 | **Ancestry: "Lineage Archive".** A name search (with country and era filters) and person cards (a record-card form: name, born, died, place, occupation or note, relations as links between cards). Its sources: the premades who are who they claim to be (an empty `truePlace`: name, birth date, place, record note), authored people (`pc.ancestry.people[]`), and, for a 2150 citizen searched by name or Citizen ID, a lineage card naming the past place on their account (flavour: the traveller-types spec's R4). Generated displaced people get no card (R4 there: a card would either reveal a liar without proof or repeat the registry). A card's place links to its History article. The cards are not compare-pickable in this spec (§12). | "info about some citizens from the past", data-driven. An impostor premade gets no card of the real person unless one is authored, so no card contradicts the game. |
| IN6 | **Site pages are content, not chrome.** They have a fixed per-site style (newsprint, encyclopedia, archive card) drawn with the form engine's blocks (FO2) in the new `SiteContent` role, which the theme skips. The world state shows in what they say, not in their colours. Their colours are checked once for contrast (FO7). | One set of contrast pairs per site instead of nine. The browser chrome around them is themed. |

### 1.9 Mail, Citizen Account, Notes, Settings (ML, AC, NT, SG)

| Id | Decision | Why |
|---|---|---|
| ML1 | **Mail** is an inbox: a list (unread in bold, day, sender, subject) and the open message, drawn as a memo form (TO, FROM, DATE and REF boxes, the body, a signature and a stamp). The day's messages come from existing sources: **the directive memo** (the day's rules, through the same `Directives` builder the Rules tab uses, with a link to Rules); **"The Temporal Times, day N"** (a link to the News site); **a citation notice for each citation** (the ledger's `CaseVerdict.citationText`, delivered once the slip is acknowledged); and **authored messages** (`pc.mail[]` with `fromDay`, `untilDay`, `flag`). Opening a message marks it read. Unread counts badge the icon and the taskbar button. | "Mail (daily rules, news, warnings)". Each message is a view of data the game already has, and only the read state is new. |
| ML2 | **Mail persistence:** `WorldState.mailRead` (message ids). Messages are regenerated from their sources by id, so the text is never saved twice. Older days' messages stay readable (their sources: the archived news, the ledger kept for the day, the day's plan). | No second copy of any text. |
| AC1 | **Citizen Account is the clerk's own account** (Q1; the traveller-types spec's D1-D3). It is drawn with the same Record Extract form (TC-901) as a traveller's, in the Records, Forms on file and Travel groups (that spec's §4.4): Citizen ID 773-2840-19 and the other authored rows from `agency.clerk`, the status (Eligible, Standard once paid off), the standing and the debt still owed (the starting debt less `WorldState.clerkDebtPaid`). Its rows carry no pick keys: the clerk is nobody's case. Below it is a **statement** (TC-960): a table of days (wages, fines, the Debt Relief instalment, household costs, purchases, the balance after, the debt still owed) from `WorldState.accountDays`, written when the shift ends (from the `ShiftLedger`, its instalment and any stranding fine) and at Home (from `HomeEconomy`). A **traveller's** account is their record in the Investigation app's Records tab. | The concept's window (ID, status, balance) is the clerk's, as Saleh answered. One form for every account means the clerk's reads exactly like the ones they check. Case evidence stays in the app (AP1). |
| NT1 | **Notes:** one page per day (a list of days on the left). A page has **Clippings** (pasted clips as cards: text, source label, a link while its source still exists that day, ✕ to remove) grouped under each traveller's name, and **Notes** (one multi-line field, at most 2,000 characters). Ctrl+V with the window focused and no field focused adds a clipping; inside the field it pastes text. Saved in `WorldState.notes`. | "Notes (paste)". TMP fields cannot hold interactive chips, so clippings are cards beside the text. |
| SG1 | **Settings** becomes a themed panel with sections (the current window, grown): **Language** and **Motion** (as today), **Desktop** (open icons with Double/Single click, Reset icon positions), **Investigation** (Steps shown or hidden, Text size 100/125/150 %, Reset tab order), and **Keyboard** (Show shortcuts: the F1 card). Every new choice is a per-player PlayerPrefs value in one `DesktopPreferences` class (the `UiLanguagePreference` pattern). | The accessibility choices live where the player looks for them. |

### 1.10 Forms everywhere (FO)

| Id | Decision | Why |
|---|---|---|
| FO1 | **One layout engine, two thin renderers.** `FormLayout` (Visuals, pure, tested) turns a `FormSpec` plus content counts and a page width into placed items (rectangles, each with a role, text size and slot) and the slots in reading order, with `SlotAt` for hit tests. **`FormView`** draws it with uGUI on the PC: the Documents tab (the scanned copy), every other tab's page, Mail, Account and the site pages. **`DeskDocument`** draws it with world-space TextMeshPro and quads on the desk paper. Both use one `FormStyleSO` (colours and sizes). `PaperFace`'s layout retires into `FormLayout` (its `FaceRect` stays). | "all the permits and all the documents and all the pages on the pc". One layout means the desk paper and its scanned copy are the same form. Two renderers keep the held paper's SDF text sharp at any resolution and keep piece 10's hit tests. Rendering the uGUI form into a texture per paper would cost a camera render and about 5 MB for each paper. |
| FO2 | **Blocks:** `Header` (the agency seal, the agency line, the programme line, the form number, the title, the serial), `Section` (a numbered heading bar and its blocks), `FieldRow` (boxed fields on a 12-column grid: each box has its label in small capitals at its top left and its value below; spans; a box may be several rows tall; a cell may hold the 4:5 photo), `Checkboxes` (options, one ticked by a field's value), `Table` (a header row and rows; each row a slot), `Paragraph`, `RecordGroups` (a record's groups of rows, each as a section of boxes), `Signature` (a rule and a caption; on a document it is bound to the form's Signature-category field: the signatory's hand, or the line left blank with UNSIGNED printed small, pickable like any field; on a page kind it is the desk officer's sign-off line), `Issued` (the issuing office's printed facsimile: "Issued by Temporal Customs · Desk 3"), `Barcode`, `FinePrint`, `StampArea` (a dashed box captioned "FOR OFFICIAL USE · DESK STAMP"), `Footer` (the standard foot: the issuing facsimile, the barcode and serial, the stamp area and fine print), `PageBreak`, and, for sites, `Masthead` and `Headline`. | "form numbers, boxed fields, barcodes, fine print, a stamp area" and "proper texts and formating". |
| FO3 | **Where specs live.** A document kind's form is a `FormSpec` on its `DocumentTemplateSO` (`form`). A PC page kind's form is a `FormSpecSO` asset in `Assets/Data/Forms/`: the record extract, the register (books), the interview record, the deviation report, the directive memo, the mail memo, the statement, the ancestry card and the history infobox. These are authored assets, like the templates (knobs live in ScriptableObjects). Their printed words (titles, section heads, captions, fine print) are English literals on the asset, like `DocumentFieldSpec.label`: forms are diegetic and never follow the UI language. | "data-driven per document kind". A new kind is a template with its form, and no code (the traveller-types spec). |
| FO4 | **A field is placed by its index, and the form decides its page.** A `FieldRow` cell names the template field it shows (`field: 2`). Every template field must be placed exactly once (the validator). The page a field sits on comes from where the form places it (`FormSpec.PageOf(field)`), which `CaseFactory` copies to `DocumentField.page`. `DocumentFieldSpec.page` goes. | One source for a field's page. `DocumentRows` and pick keys are unchanged. |
| FO5 | **No invented personal data.** A form's furniture is the agency line, the form number, the serial, the barcode, fine print, section heads, the issuing facsimile and the stamp area. A signature line appears only where the form has a Signature field (it draws that field). An empty line on every form would read as "unsigned", which is a real fault on the waiver (traveller-types F3). Dates are fields, never furniture: a Valid Until or a departure date is printed only where the template has one (traveller-types F7). Nothing on a form is a second copy of a traveller fact that could disagree with the first. The serial is `{form number}/{6 digits}` from `Seeds.ForForms(caseSeed)` (a new salt, a value not a stream draw, with SeedsTests' distinctness test) and the document index, stored as `DocumentInstance.serial` by `CaseFactory`. The barcode is drawn by code from the serial (`Barcode.Bars`: guard bars and 60 modules; decorative, not decodable). | Decoration that varied at random would read as tells. The serial is deterministic, and existing draws are untouched. |
| FO6 | **Sizes are in page heights (H), with floors for 720p.** Value 0.049 H, shrinking to at most 0.042 H. Label 0.038 H. Section head 0.036 H bold. Title 0.052 H. Form number and serial 0.026 H. Fine print 0.020 H. A box reserves the lines its field's longest value needs at the floor (measured: at most two; a box that would need three is reported). Worked sizes are in §6.3. A held paper at 720p draws values at 15.5 px (today's desk rows, which the traveller-types spec counts on) and labels at 12 px; the PC page at 720p draws values at 18 px. | "readable at 720p". Sizing in H makes the desk paper and the PC page the same form at two sizes. |
| FO7 | **Colours are the form style's, checked once.** `FormStyleSO` holds paper, ink, label ink, rule, box fill, link, found, fine-print ink, the stamp-area dash, and each site style's pairs. Build Office UI checks every pair at its text class through `Contrast` (ink on paper and on box fill, label on paper, link on paper, ink on the found and pick fills), as the palette check does, and logs an error below the minimum. Forms and site pages are never themed. | "contrast-checked". Forms are diegetic (UI art contract), so one check covers every culture. |
| FO8 | **The art is text-free; the code draws the lines.** One **agency seal** (`Assets/Art/UI/Forms/agency_seal.png`, 512 × 512, greyscale, text-free, Temporal Customs' own mark, not a nation's), printed at 10 % tint behind the header. **A paper face per document kind** (`paper_<kind>.png`, 1024 × 1339: paper tone, printed border, a guilloche band) and one plain agency face for PC pages (`paper_agency.png`). Boxes, rules, checkboxes, the barcode, the stamp area and the photo frame are code-drawn, so no face carries a box. The verdict ink marks (`stamp_accept`/`stamp_deny`, already in the list) land in the stamp area. | The asset list's rule that the game prints the text is kept. The seal reverses the retired "agency_logo, the nation seals" row for one fictional mark (§14). |
| FO9 | **The PC's pages as forms:** Documents (each kind's form, on the scanner's dark backing), Records (Record Extract TC-901: the record's rows in the groups Records, Forms on file and Travel), Reference (Register TC-911 to TC-916: Place, Era, Value, Note; the Costume Guide's register grouped by era, Saleh's answer to the traveller-types spec's Q9), Transcript (Interview Record TC-920: No., Speaker, Statement; answers marked ◆), Report (Deviation Report TC-930: case, numbered deviations, officer's signature, stamp), Rules (Directive Memo TC-940), Mail (Memorandum TC-950), Account (the Record Extract and Statement TC-960), Ancestry (Record Card LA-1), History infobox. Page kinds other than documents flow: their width is the pane's, and their height grows with their rows. | "all the pages on the pc". |
| FO10 | **Piece 10's face checks move to `FormLayout.Check`:** a template whose form does not fit the desk face at the floors, or places a field twice or never, is reported by Build Office UI (as `PaperFace.Capacity` is today) and by the content validator. `DeskConfigSO.face` (`PaperFaceTuning`) goes, and `FormStyleSO` holds the one set of sizes. The traveller-types spec's forms keep at most 6 fields (its F1) and must fit the face at the floors. Boxes two to a row let a 6-field form keep today's row size under the header and footer (§6.2, §6.3). | One set of knobs for the face, on both surfaces. |

### 1.11 Theme and translation (TH, TR)

| Id | Decision | Why |
|---|---|---|
| TH1 | **The culture theme restyles the OS, never the content.** Themed: the wallpaper, the icons' glyph tint, plates and labels, the taskbar, the Start and context menus, window chrome, the app's chrome (case header, toolbar, tab strips, sidebar, search panel, toasts, badges, focus ring), the dock, the browser chrome, and the Mail, Account, Notes and Settings chrome. Not themed: forms, site pages, the photo, the bubble. The compare pick fill stays the theme's `SelectionHighlight`, as today. | UI rules: "documents stay unthemed". |
| TH2 | **New roles are appended** (the enum is serialized): `TabStrip`, `Tab`, `TabActive`, `Sidebar`, `SearchResults`, `Badge`, `Toast`, `FocusRing`, `IconSelection` (chrome), and `DiegeticForm`, `SiteContent` (diegetic). **`ThemeRoles.IsDiegetic` becomes an explicit list**, tested for every enum value, because `role >= DiegeticPaper` would make every appended chrome role diegetic. The palette map gets rules for the chrome roles. `DeskDim` and `StickyNote` lose their graphics and leave the palette map; their enum members stay, documented as retired. | The rule as written cannot survive an append. |
| TH3 | **The flavour labels change from 34 to 28.** Removed: `icon.directives`, `icon.scanner`, `icon.records`, `icon.lexicon`, `icon.dialect`, `icon.material`, `icon.clueLog`, `window.directives`, `window.scanner`, `records.title`. Added: `icon.investigation`, `icon.mail`, `icon.account`, `icon.settings`, translated in each culture language table. Tab names, the search placeholder, the steps and the app's other chrome are Full tier (the reading language). | Icon labels were flavour and stay flavour, with the glyph now carrying the meaning. Navigating inside the investigation must always read. |
| TH4 | **Chrome built at run time** (search hits, step rows, pin rows, toasts, taskbar buttons, context menus) is cloned from builder-made, tagged templates. The builder's contrast check covers the templates, and the play-through audit covers the clones. | Clones inherit their template's tags and the theme already applied at scene load. |
| TR1 | **Forms are always English.** Saleh: "all documents must be filled in english." Every form value, on the desk and on the PC, is written plain: no document ever flips, so piece 10's shared written reveal (`RevealClock`, `DocumentReveal`) and its sightings (X25) retire in the plan's translation phase, before any form is drawn. Transcript lines keep piece 9's speech rules through `TextFlip.Write`: a displaced traveller's line without the region's Speech translator shows untranslated with its key words in English (the traveller-types spec's §8.1); with the translator it shows in English. | Saleh's translation change: "the translator should be for speech of other languages". |
| TR2 | **The dock never shows glyphs**: an untranslated answer reads "(untranslated Greek; Mediterranean Translator)", as today. Search follows SE5, and a copied untranslated line follows CP2/CP3. Form furniture, form values, site text, mail and steps are English (the agency's language and the reading language). | Piece 9 T5, now for speech only. |
| TR3 | **R4-024 is taken** (in the translation phase): `TextFlip.Write` takes the text's own font and always sets script or own, because `FormView` pools transcript rows across items, so a row may have held a foreign line before. | A pooled row would otherwise keep a script font after a flip. |

### 1.12 Code (RF)

| Id | Decision | Why |
|---|---|---|
| RF1 | **`InvestigationUIController` becomes a thin façade** with the same public API for `GameManager` (`SetDirectives`, `SetCitizenRegistry`, `SetFacts`, `SetInterviewDay`, `SetCharacterArt`, `SetTranslation`, `ShowCase`, `Hide`, `SetFrameOpen`, `EvidenceCount`, `EvidenceSystemActive`, `InterviewReachable`, `AppearanceReachable`). The work moves to `CaseDocumentsPresenter` (documents, reveal clocks, hand-over and scan), `InterviewPresenter` (runner, wheel, transcript), `EvidencePresenter` (the discrepancy log, the report, the dock's DEVIATION line) and `DayReference` (facts, registry, rules), with the app's views on top. The reachability predicates keep their meaning: "the app's Transcript view and the wheel are wired", "the wheel and the compare are wired". | R4-001's own proposal, done where the redesign touches every responsibility anyway. R4-022: the predicates gate tell generation. |
| RF2 | The complete keep / refactor / remove list is §10. | |

### 1.13 The scanner upgrades (SC)

Saleh (Q2): Archive Access is removed; "instead have two versions of a scanner one auto scans and one manual scan but auto highlights a contradiction."

| Id | Decision | Why |
|---|---|---|
| SC1 | **Two scanner upgrades in the Home shop.** The **Auto-Feed Scanner** (new: id `scanner_autofeed`, 200 cr) takes Archive Access's place in the shop. The **Analysis Scanner** is the existing Advanced Scanner upgrade (id `adv_scanner` kept, so a save that owns it keeps it; renamed "Analysis Scanner", 300 cr). `Upgrade_ArchiveAccess` and its shop-discount effect retire; the Advanced Scanner's unread `upgrade:adv_scanner` flag effect (`Effect_Upgrade_ScannerBoost`) retires with the dead promise "reveals an additional clue category". Both are read from the day-start snapshot, like every upgrade (in force from the next office day). | Two versions, as Saleh asked. The Advanced Scanner had no live effect (its only visible use was the retired Material placeholder), so it gets the job its name promises instead of a third scanner row. |
| SC2 | **They are neither tiers nor exclusive: each changes a different scan path, and they combine.** The Auto-Feed changes how a paper reaches the scanner; the Analysis changes what a scan by hand does. Owning both: handed-over papers scan themselves (plain), and a paper the player drags onto the scanner (a rescan included) gets the analysis pass. Owning the Analysis alone: every scan is by hand, and every one analyses. | Saleh's two versions are "one auto scans" and "one manual scan but auto highlights": the manual scan is the analysis's trigger, so the two never conflict. No exclusive purchase or trade-in rule is needed, and every upgrade stays a permanent purchase, as today. |
| SC3 | **Auto-Feed:** a paper handed over lands where it lands today, then enters the scanner by itself, in hand-over order, one at a time (a Domain `DeskPapers` scan queue; a paper held or dragged at its turn is skipped until it is back on the desk). The scan keeps its 1.5 s, the clock keeps running, and the scanned copy reaches the PC as any scan does (WN5). | "one auto scans": no drag; the same scan, so nothing downstream changes. |
| SC4 | **Analysis:** a scan by hand takes `DeskConfigSO.analysisScanSeconds` (first cut 3 s) and then reads the traveller's **scanned papers** through the traveller-types spec's `PaperChecks.Contradictions` (the same rule as its paper-against-paper proof, L4). It marks **the first contradicting pair whose category the Deviation Report does not hold yet** (document order, then field order) on the scanned copies: both fields get a dashed outline in the form style's `analysis` colour, and the scan strip reads "ANALYSED 10:44 · 1 CONTRADICTION MARKED", or "ANALYSED 10:44 · NO CONTRADICTION BETWEEN THE SCANNED PAPERS". | "auto highlights a contradiction", on the scanned papers. One rule serves the proof and the mark, so a marked pair can always be logged. |
| SC5 | **The analysis never solves the case.** It reads papers against papers only: never the books, the records, the answers, the dress, the Directives or the dates (a missing or unsigned form, a closed destination, a wrong date and every record or book lie stay the player's work). It marks one pair, never names which side is forged or what the fault is, and never picks: the player still clicks both fields to compare, the dock shows the result, and the evidence gate is unchanged. A mark lasts for the case; a later analysis marks the next undocumented pair, if any. Marks are on the PC only (the desk paper is a physical object). | Saleh's answer keeps the upgrade a help, not an answer: the core act (pick two, log, decide) stays the player's. |
| SC6 | **Data and art:** the two upgrades are hand-authored `UpgradeSO` assets as today; `analysisScanSeconds` is a `DeskConfigSO` knob; the `analysis` colour is a `FormStyleSO` pair checked for contrast (FO7). The placeholder flatbed shows a code-drawn feeder tray with the Auto-Feed and a lamp with the Analysis until the art adds them (§14). | Knobs in ScriptableObjects; nothing new in the shop's rules. |

### 1.14 Content (CT)

| Id | Decision | Why |
|---|---|---|
| CT1 | **The `pc` content is row-shaped, for a spreadsheet later.** Saleh: "we will create an excel file for all cases later with all possible dialogue options, all possible dialogue interactions, all real characters, all premade characters and stories etc". Every `pc` table (§4.8: steps, sites, pages, ancestry people and relations, mail and its lines) follows the traveller-types spec's row rules (its §15: one record per row, an id, scalar columns, references by id, one level of child tables). The import path (CSV or XLSX, then `world_source.json` or its successor, then Generate World) is a later phase of the plan; nothing is built for it now. | One content shape for both specs, so the later import is one converter. |

## 2. The screens (wire-frames)

Sizes are desktop units (u). Themed chrome is drawn with `═` and `─` rules. Forms (unthemed) are drawn inside `┌┐` boxes. `•` is a badge; `[x]` is a ticked box; `↗` is a smart link; `⌖` is a pin.

### 2.1 The desktop (no window open, a traveller at the desk)

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  .------.                                                                        │
│  | INV  |•      wallpaper: neutral, or the leading culture's                      │
│  '------'                                                                        │
│ Investigation                                       .------.                     │
│                                                     | MAIL |3   <- dropped here   │
│  .------.                                           '------'                     │
│  | WEB  |                                             Mail                       │
│  '------'                                                                        │
│  Internet                                                                        │
│  .------.                                                                        │
│  |  ID  |                                                                        │
│  '------'                                                                        │
│  Citizen                                                                         │
│  Account          .-----------------.                                            │
│  .------.         | Arrange icons   |   <- right-click on the empty desktop       │
│  | NOTE |         '-----------------'                                            │
│  '------'                                                                        │
│   Notes                                                                          │
│  .------.                                                                        │
│  | GEAR |  <- selected (IconSelection plate); Enter or a double-click opens      │
│  '------'                                                                        │
│  Settings                                                                        │
├──────────────────────────────────────────────────────────────────────────────────┤
│ COMPARE   Click two values to compare them.                                 56 u │
├──────────────────────────────────────────────────────────────────────────────────┤
│[start][< Desk][INV Investigation][WEB Internet]           │Day 3│1,240 cr│82%│10:42│
└──────────────────────────────────────────────────────────────────────────────────┘
```

- The icon area is 1440 × 988 u, above the dock and the taskbar. Cells are 120 × 132 u. The default arrangement is one column from (20, 20); here Mail was dragged out of it.
- The dock shows only while a traveller is at the desk, but its 56 u are always reserved, so a maximised window never changes size.
- Between travellers, with no window open, the idle line ("Waiting for the next traveller", 80 u, on a `ScreenStrip`) is drawn across the middle of the wallpaper, under the icons.
- Taskbar buttons are in open order. The focused window's button is pressed; a minimised window's is flat.

### 2.2 The Investigation app, maximised (1440 × 988 u), with a case on

```
╔═ INV Investigation · Aster Vale ══════════════════════════════════════[_][□][X]╗ 36
║ ASTER VALE (Artist): "One leisure departure to Classical Athens, please."      ║
║ Papers 2 of 2 received · 2 scanned · Deviations 1           [✓ ACCEPT][✗ DENY] ║ 72
╟────────────────────────────────────────────────────────────────────────────────╢
║ [◀][▶]  [ Search this case and today's records ...  Ctrl+F ] [Steps][Split][F1]║ 52
╟───────────────┬───────────────────────────────╥────────────────────────────────╢
║ STEPS    [on] │[DOC Documents•][R][B][T][D][L]║ [D][REC Records][B][T][D][L]   ║ 44
║ [x] Rules     │ (Visa ✓)(Manifest ✓)         ⌖║ NAME OR NUMBER [418-0937-52] ⌖ ║ 40
║ [x] Papers 2/2│ ┌───────────────────────────┐ ║ ┌────────────────────────────┐ ║
║ [x] Read   2/2│ │(seal) TEMPORAL CUSTOMS    │ ║ │(seal) TEMPORAL CUSTOMS     │ ║
║ [x] Identity  │ │ DEBT RELIEF DEPARTURES    │ ║ │ CITIZEN ACCOUNT · TC-901   │ ║
║ [x] Class     │ │ LEISURE DEPARTURE VISA    │ ║ │ 1 RECORDS                  │ ║
║ [ ] Transpndr │ │ Form TC-101   101/204817  │ ║ │ ┌NAME───────┐┌CITIZEN ID─┐ │ ║
║ [ ] Dress     │ │ 1 TRAVELLER               │ ║ │ │Aster Vale ││418-0937-52│ │ ║
║───────────────│ │ ┌FULL NAME──────┐┌──────┐ │ ║ │ └───────────┘└───────────┘ │ ║
║ PINNED        │ │ │Aster Vale    ↗││photo │ │ ║ │ ┌STATUS─────┐┌DEBT───────┐ │ ║
║ ⌖ Rule 2      │ │ └───────────────┘│      │ │ ║ │ │Standard   ││3,400 cr   │ │ ║
║ ⌖ Costume Gd. │ │ ┌CITIZEN ID─────┐│      │ │ ║ │ └───────────┘└───────────┘ │ ║
║───────────────│ │ │418-0937-52   ↗│└──────┘ │ ║ │ 2 FORMS ON FILE            │ ║
║ RECENT        │ │ 2 DEPARTURE               │ ║ │ 3 TRAVEL                   │ ║
║ Visa          │ │ ┌VISA CLASS─┐┌VALID UNTIL┐│ ║ └────────────────────────────┘ ║
║ Records: Ast..│ │ │Premium   ↗││9 Jun 2150 ││ ║                                ║
║               │ │ └───────────┘└───────────┘│ ║  (the active pane has the      ║
║               │ │ Issued: Temporal Customs  │ ║   accent border)               ║
║               │ │ |||| ||| || ||||  [STAMP] │ ║                                ║
║               │ │ fine print ...            │ ║                                ║
║               │ └───────────────────────────┘ ║                                ║
║               │   [Departure Manifest scanned ▸ Open]   <- toast, 4 s          ║
╚═══════════════╧═══════════════════════════════╩════════════════════════════════╝
 COMPARE  Leisure Departure Visa · Visa Class: Premium ↗  │ DEVIATION LOGGED │
          Citizen Account 418-0937-52 · Status: Standard ↗       Report ▸  [✕]
[start][< Desk][INV Investigation]                          │Day 1│1,240 cr│82%│10:42│
```

- The body is 828 u tall. The sidebar is 272 u wide, and each pane about 580 u, with a 6 u divider. Each pane has a 44 u tab strip, a 40 u pane header and 744 u of content.
- The left pane is narrow, so its inactive tabs show glyphs only. The active tab keeps its label, and tooltips name them all.
- The case is one of day 1's L1 liars (the traveller-types spec): a Standard citizen travelling on a Premium visa. The Visa Class's ↗ opened the traveller's Citizen Account in the right pane (LK2); picking the visa's class and the account's status logged the deviation.
- Every form value is English (TR1). A glyph (`▓▓▓`) can only appear in the Transcript tab, for a displaced traveller's untranslated line.
- The steps are the rich tourist's set (ST3), abbreviated.
- The Steps section can be switched off (it collapses), and Ctrl+B hides the whole sidebar (the panes widen to 717 u).

### 2.3 Search: the results panel

```
║ [◀][▶]  [ drach|                                     Ctrl+F ]  [Steps][Split]      ║
║          ┌────────────────────────────────────────────────────────────┐            ║
║          │ All · Documents · Records · Reference(3) · Transcript(1)   │            ║
║          │ REFERENCE                                                  │            ║
║          │  BOOK Currency Ledger · Classical Athens (Ancient)         │ <- focused ║
║          │       [Drach]ma                                            │            ║
║          │  BOOK Currency Ledger · Hellenistic Alexandria (Ancient)   │            ║
║          │       Silver tetra[drach]m                                 │            ║
║          │  BOOK ...                                                  │            ║
║          │ TRANSCRIPT                                                 │            ║
║          │  TRN  Lysimache · line 7                                   │            ║
║          │       "We paid in [drach]mas, of course."                  │            ║
║          │ Enter open · Ctrl+Enter other pane · Esc close             │            ║
║          └────────────────────────────────────────────────────────────┘            ║
```

- Groups follow the saved tab order. Each group shows 5 hits, then "Show all n". A chip in the header filters to one source.
- A pasted foreign clip (copied from an untranslated transcript line) shows in the field as a chip, `[Transcript line 7 · untranslated Greek ✕]`. It finds only untranslated lines of the same tongue that are equal to it (SE5). The sample's line 7 is a displaced traveller's answer heard with the Speech translator, so it is English and typed words find it.
- With no hit, the panel reads "Nothing in this case or today's records matches 'drachq'."
- (The place names in these samples are illustrative. The real rows are today's `FactTable`.)

### 2.4 Documents

The pane header shows one chip per document of the case, in paper order (a poor tourist's here):
- `(Leisure Departure Visa ✓)` when it is scanned;
- `(Departure Manifest: on the desk)` when it is handed over but not scanned;
- `(Proof of means: not handed over)` when it is still to be requested (a request group is named for the group until the traveller hands over the form they hold, the traveller-types spec's I2).

A chip that is not scanned shows the form's outline, greyed, with "Scan it on the desk to read it here" (or "Ask the traveller for it"). A scanned form sits on the dark scanner backing (`DiegeticBacking`). A strip along its top edge reads "SCANNED 10:42 · DESK SCANNER 1", with the time from the shift clock; after an analysis pass (SC4) it reads "ANALYSED 10:44 · 1 CONTRADICTION MARKED" or "ANALYSED 10:44 · NO CONTRADICTION BETWEEN THE SCANNED PAPERS". The traveller-types spec's forms are one page each; a multi-page form would stack its pages, with a "Page 2 of 2" rule between them. Every value is English (TR1).

```
 ┌─────────────────────────────────────────────┐
 │ ANALYSED 10:44 · 1 CONTRADICTION MARKED     │  (on the backing; with the Analysis Scanner)
 │ ┌─────────────────────────────────────────┐ │
 │ │ STRANDING WAIVER      Form TC-310       │ │
 │ │ Debt Relief Departures    310/583021    │ │
 │ │ 1 SIGNATORY                             │ │
 │ │ ┌SIGNATORY─────────┐┌CITIZEN ID───────┐ │ │
 │ │ │ Oren Hale       ↗││ 552-1804-33    ↗│ │ │
 │ │ └──────────────────┘└─────────────────┘ │ │
 │ │ 2 TERMS                                 │ │
 │ │ ┌TRANSPONDER┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┐ │ │  <- the analysis mark (dashed)
 │ │ ┆ Hopper Mk I · HP-11952             ↗┆ │ │
 │ │ └┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┘ │ │
 │ │ ┌DEBT PASSED TO KIN┐┌WAIVER NO.───────┐ │ │
 │ │ │ 9,800 cr        ↗││ SW-204817      ↗│ │ │
 │ │ └──────────────────┘└─────────────────┘ │ │
 │ │ ─────────────────── O. Hale (signature) │ │
 │ │ Issued: T. Customs   |||| || |||  [   ] │ │
 │ └─────────────────────────────────────────┘ │
 └─────────────────────────────────────────────┘
```

- The waiver's Transponder disagrees with the manifest's (a traveller-types L3 fake waiver), so the manifest's Transponder row carries the same dashed mark in the other pane. The mark names neither side as the forgery and picks nothing: picking both logs the deviation (SC4, SC5).

### 2.5 Records

A record is rows (category, label, value) in groups, as the traveller-types spec's R1 defines them. A 2150 citizen's Citizen Account has the groups **Records**, **Forms on file** and **Travel** (the art concept's three icons). A displaced person's Displacement Registry entry has one group. The form draws each group as a section of boxes, two to a row, a long value across the row.

```
 pane header:  NAME OR NUMBER [ 552-1804-33          ] [LOOK UP]   recent: Aster Vale · Oren Hale
 ┌─────────────────────────────────────────────────────────┐
 │ (seal) TEMPORAL CUSTOMS                                 │
 │ CITIZEN ACCOUNT · RECORD EXTRACT          Form TC-901   │
 │ Query "552-1804-33" · 1 record on file · 16 Mar 2150    │
 │ 1 RECORDS                                               │
 │ ┌NAME──────────────────┐┌CITIZEN ID──────────────────┐  │
 │ │ Oren Hale            ││ 552-1804-33                │  │  <- rows with a category are pickable
 │ └──────────────────────┘└────────────────────────────┘  │     (key: record:{number}:{category})
 │ ┌BORN──────────────────┐┌STATUS──────────────────────┐  │
 │ │ 2 Feb 2117           ││ Eligible                   │  │
 │ └──────────────────────┘└────────────────────────────┘  │
 │ ┌STANDING──────────────┐┌DEBT────────────────────────┐  │
 │ │ Good                 ││ 96,200 cr                  │  │
 │ └──────────────────────┘└────────────────────────────┘  │
 │ 2 FORMS ON FILE   (transponder, class, waiver, contract)│
 │ 3 TRAVEL          (booked departure, history)           │
 │ NOTE  No remarks on file.                               │
 │ fine print ...                                [ STAMP ] │
 └─────────────────────────────────────────────────────────┘
```

- The lookup takes a name or a number and runs the index scoped to Records (SE6). With no match, the extract reads "NO RECORD ON FILE for 'Aster Vail'", under its query line.
- A smart link from any field of a record category fills the field with the document's Citizen ID (else its Name), runs the lookup, and focuses that category's row.
- The LOOK UP button keeps `records.search` (a flavour label with its English gloss, as today's SEARCH button).
- Records become rows in the plan's phase 2 (the registry entry's rows, one group) and accounts in its phase 6; the form is the same for both.
- The clerk's own account (773-2840-19) is in no registry and is never looked up here: it is the Citizen Account app (§2.13).
- (The names and numbers here are samples, not content.)

### 2.6 Reference

- The pane header has six book chips, each with its cover (the asset list's `refbook_cover_<id>`; a code-drawn spine until the art lands) and the book's name when the chip is wide enough. After them is "[x] Claimed place only".
- The page is a register: a header (the form number, the book's name, "edition of day N"), a table with PLACE, ERA, VALUE and NOTE columns, then fine print.
- The claimed place's row comes first, outlined and tagged "(claimed)". The other rows follow in today's `FactTable` order.
- **The Costume Guide is grouped by era** (Saleh's answer to the traveller-types spec's Q9): after the claimed row, an era heading row ("MEDIEVAL") comes before each era's places, the claimed place's era first; the present's row (2150: its clothes and its accessory kit) closes the page. The grouping is a flag on the book (`ReferenceBookSO.groupByEra`), drawn by the register form; the rows and their picks are unchanged.
- A row whose value history changed shows "Revised" in NOTE, with a ↗ to its History article.
- Each row is pickable, and it is the same pick as today's book row (`PickKeys.BookRow`).

### 2.7 Transcript

An Interview Record form: a header (the traveller, "Desk officer", the day), then a ruled table with the columns No., SPEAKER and STATEMENT.
- Answer rows carry a ◆ in the No. column and can be picked (the same pick as the bubble's answer).
- A displaced traveller's line without the region's Speech translator shows untranslated, its key words in English (TR1); with the translator it shows in English. 2150 citizens always speak English.
- The table follows the newest line while the view is at the bottom. If the player has scrolled up, a "New line ▾" pill appears instead.
- "Answers only" hides the other rows.

### 2.8 Report

```
 ┌──────────────────────────────────────────────────────────────────────────┐
 │ (seal) DEVIATION REPORT                                       Form TC-930│
 │ Case 4 · Lysimache (Merchant) · to Classical Athens                     │
 │ ┌NO┬CATEGORY─┬STATEMENT──────────────┬CONTRADICTED BY──────────┬PROOF───┐│
 │ │ 1│Currency │Intake·Coin of Home   ↗│Currency Ledger·Athens ↗ │mismatch││
 │ └──┴─────────┴───────────────────────┴─────────────────────────┴────────┘│
 │ No further deviations documented.                                        │
 │ ───────────── desk officer, day 5                               [ STAMP ]│
 └──────────────────────────────────────────────────────────────────────────┘
```

Each row's two ↗ jump to its two sides. With nothing logged, the table reads the text of `scanner.idle`.

### 2.9 Rules

A Directive Memo (TC-940) with the boxes TO "All desk officers", FROM "Customs Directorate", DATE "16 Mar 2150" (the traveller-types spec's calendar, F6) and REF, then the numbered rules.
- Each rule is the text of `TravelRuleSO.Summary()`. Its place words are ↗ links to the Reference, filtered to that place.
- With no rule, the memo reads the text of `directives.none`.
- Nothing on the memo says whether a rule applies to the current traveller: the player has to check.

### 2.10 No case, the restored window, the clone

- **No case.** Each pane that shows a case source draws "Waiting for the next traveller" at 80 u. Reference, Records and Rules still work: the player can read between travellers.
- **Restored** (1120 × 820 u). There is one pane (AP3), and the sidebar stays. The Split toggle is greyed, with the hint "Maximise the window for two panes".
- **On the clone.** The office PC's glass shows all of this at about 0.28 of the frame's size. Only the 80 u idle line and the toasts' shapes are meant to read from the chair.

### 2.11 Internet

```
╔═ WEB Internet ══════════════════════════════════════════════════════[_][□][X]╗
║ [◀][▶][⌂]  [ chronet://times.tc/day-3                    ▾]  [ Search site ]║
╟─────────────────────────────────────────────────────────────────────────────╢
║   ┌─────────────────────────────────────────────────────────────────────┐   ║
║   │            THE TEMPORAL TIMES · online edition · Day 3               │   ║  Masthead
║   │ ─────────────────────────────────────────────────────────────────── │   ║
║   │ HISTORY: China now dominates the timeline. The Future belongs to... │   ║  Headline (lead)
║   │ WORLD                              │ NOTICES                         │   ║
║   │ HISTORY: Florentine printers set   │ ┌─────────────────────────────┐ │   ║
║   │ type the Chinese way ...    ↗      │ │ Desk officers may now ask...│ │   ║
║   │ ...                                │ └─────────────────────────────┘ │   ║
║   │ A FRESH START: DEBT RELIEF         │ TIMELINE STANDINGS              │   ║
║   │ DEPARTURES UP AGAIN (debt pool)    │ 1 China · 2 Egypt · 3 Italy ... │   ║
║   │                                    │ ARCHIVE: Day 2 ↗ · Day 1 ↗      │   ║
║   └─────────────────────────────────────────────────────────────────────┘   ║
╚═════════════════════════════════════════════════════════════════════════════╝
```

- **Home (⌂)** is the portal: a tile per listed site (glyph, name, blurb). No site is gated (IN2).
- **Chronopedia** (`chronet://chronopedia/italy/medieval`):
  - The page has its title ("Florentine Republic"), year and moment paragraph.
  - The infobox is a form `FieldRow` grid: CAPITAL, RULER, CURRENCY, LANGUAGE, TECHNOLOGY and DRESS (men's and women's). A revised box has a second line: "Revised day 5 (was Printing press)".
  - Below it is "Open in Investigation ▸ Reference ↗" when the place is in today's table.
  - The index is an 8 × 6 grid of countries by eras. The Revisions page is a table: DAY, PLACE, FACT, BEFORE, AFTER, WHY.
- **Lineage Archive** (`chronet://lineage/search?name=senen`):
  - The browser's "Search site" field searches names, with country and era chips. Hits are listed as rows.
  - A card is a Record Card LA-1 form: NAME, BORN, DIED, PLACE ↗, OCCUPATION/NOTE, and RELATIONS (links to other cards).

### 2.12 Mail

```
╔═ MAIL Mail (2 unread) ═════════════════════════════════════[_][□][X]╗
║ ┌ INBOX ────────────────┐ ┌─────────────────────────────────────────┐║
║ │● Day 3 Directive memo │ │ (seal) INTERNAL MEMORANDUM   TC-950     │║
║ │● Day 3 Temporal Times │ │ TO   All desk officers  DATE 16 Mar 2150│║
║ │  Day 2 Citation notice│ │ FROM Customs Directorate  REF D-3       │║
║ │  Day 2 Directive memo │ │ SUBJECT Today's directives              │║
║ │  Day 1 Welcome to ... │ │ 1. No travel to the Ancient era ↗       │║
║ │                       │ │ Open in Investigation ▸ Rules ↗         │║
║ └───────────────────────┘ │ ─────────── Customs Directorate [STAMP] │║
║                           └─────────────────────────────────────────┘║
╚══════════════════════════════════════════════════════════════════════╝
```

### 2.13 Citizen Account (the clerk's own: Q1)

- **The account:** the Record Extract of §2.5 (TC-901), opened on the clerk (the traveller-types spec's §4.4): CITIZEN ID 773-2840-19, STATUS Eligible, STANDING Good, DEBT 125,430 cr less what the instalments have paid, and the authored name, birth date and lineage. No row is pickable: the clerk is nobody's case.
- **Below it, the Statement** (TC-960): a table with the columns DAY, WAGES, FINES, DEBT RELIEF, HOUSEHOLD, PURCHASES, BALANCE and OWED (amounts in cr, the wallet's word). DEBT RELIEF is the shift's instalment (that spec's D2); FINES include stranding fines (its S3).
- Fine print ("Departures are voluntary.") and an empty stamp area.
- On the bankrupt ending the standing reads Frozen and the Travel group shows the clerk's booked Debt Relief departure (its D3).
- The window has no lookup field. Looking up a traveller is the Investigation app's job.

### 2.14 Notes

On the left, a list of days. On the right, the day's page:
- **CLIPPINGS**, grouped under the traveller's name. A card reads, for example, "Transcript · line 7: ▓▓▓ home ▓▓ Periclean Athens (untranslated Greek) ↗ ✕" or "Visa · Visa Class: Premium ↗ ✕". Its ↗ works while its source is still on the PC that day.
- **NOTES**: one multi-line field with a counter ("312 / 2000").
- When the page is empty, a hint: "Copy a value (Ctrl+C) and paste it here (Ctrl+V)."

### 2.15 Settings

A themed panel (640 × 720 u), with a titled section per group. Two-choice buttons work as today (the chosen one in the accent colours).
- **Language**: Follow history / Always English.
- **Motion**: Full / Reduced.
- **Desktop**: Open icons with Double click / Single click; [Reset icon positions].
- **Investigation**: Steps Shown / Hidden; Text size 100 / 125 / 150 %; [Reset tab order].
- **Keyboard**: [Show shortcuts].
- The existing note stays at the bottom.

### 2.16 The desk paper, held (piece 10's pose, the same form)

```
   ┌──────────────────────────┐   0.44 screen heights tall (475 px at 1080p, 317 px at 720p)
   │(seal) TEMPORAL CUSTOMS   │   title 0.052 H
   │ DISPLACEMENT CRT. TC-610 │   form no. 0.026 H
   │ 1 DISPLACED PERSON       │   section head 0.036 H
   │ ┌FULL NAME─────┐┌─────┐  │   label 0.038 H (12 px at 720p); value 0.049 H (15.5 px at 720p)
   │ │Lysimache     ││photo│  │
   │ └──────────────┘│     │  │
   │ ┌DATE OF BIRTH─┐│     │  │
   │ │3 Apr 1476 BCE│└─────┘  │
   │ 2 INCIDENT               │
   │ ┌INCIDENT┐┌VALID───┐     │   a box reserves the lines its longest value needs
   │ │R-0311- ││9 Jun   │     │
   │ │07      ││2150    │     │
   │ └────────┘└────────┘     │
   │ Issued: Temporal Customs │
   │ ||| || |||| 610/583021   │
   │ fine print     [STAMP]   │   the verdict ink mark lands here
   └──────────────────────────┘
```

A row's hit area is its box (`FormLayout.SlotAt` replaces `PaperFace.RowAt`), and the hover tint and the pick highlight fill the box. There are no ↗ links on the desk: the paper is a physical object, and links belong to the PC.

## 3. Behaviour

### 3.1 Events and what each one does

| Event | Comes from (today's code) | The app | Steps (`CaseProgress`) | Index | Elsewhere |
|---|---|---|---|---|---|
| The day starts (after the briefing) | `GameManager` → `SetDirectives`, `SetCitizenRegistry`, `SetFacts` | Reference, Records and Rules redraw | – | the day layer is rebuilt | Mail adds the day's messages. The News archive gets the day's issue (at `ShowBriefing`). |
| A traveller is presented | `ShowCase` | The case header and the Documents chips are set. The left pane goes to Documents. A Reference pane turns "Claimed place only" on. The badges reset. | reset; the step set comes from the traveller type | the case layer resets | Toasts clear. Case pins and recents are dropped (PR2). |
| A paper is handed over | `desk.BeginCase` (on arrival), `desk.HandOver` | The chip reads "On the desk"; the counters update | `PapersReceived` | – | With the Auto-Feed Scanner, the paper joins the scan queue (SC3). |
| A paper is examined at the desk | `desk.PaperExamined` | – | `PaperRead` | – | – |
| A scan finishes | `desk.ScanFinished` | The chip gets ✓, the Documents tab a badge, and a toast shows. If the app is closed it opens; the left pane shows the document if it shows no document yet (WN5). | – | the document and its fields are added | – |
| An analysis pass finishes (the Analysis Scanner, a scan by hand) | `desk.ScanFinished` with its analysis result | The first undocumented contradicting pair is marked on both scanned copies, and the scan strip says so (SC4). Nothing opens or switches. | – | – | – |
| A document is shown in a pane while the frame is open and the app is not minimised | new (the Documents view) | – | `PaperRead` | – | – |
| A line is spoken | `InterviewPresenter` (today's `Choose`) | The Transcript tab gets a badge; the rows append | `Asked(category)` for an answer | the line is added | – |
| A garment is looked at | `LookAt` | the dock shows the pick | `LookedAt` | – | – |
| A pair is compared | `CompareController.PairCompared` | the dock | `Compared(statement category and kind, truth kind)` | – | – |
| A deviation is logged | `HandlePairCompared` | The report gains a row and its tab a badge. The dock shows DEVIATION LOGGED and "Report ▸". Nothing opens. | – | the deviation is added | – |
| The Rules tab is shown while a traveller is at the desk | new | – | `RulesViewed` | – | – |
| The decision | `Decide` | The case sources show the no-case state | cleared | the case layer clears | Case pins and recents are dropped. A citation's Mail notice arrives once its slip is acknowledged. |
| The frame opens | `SetFrameOpen(true)` | – | – | – | The keyboard poller goes live. |
| The frame closes | `SetFrameOpen(false)` | Open menus and the search panel close | – | – | The keyboard poller stops. |
| The shift ends | `GameManager` → the shift report | – | – | – | The account day is written from the `ShiftLedger`, with the Debt Relief instalment and any stranding fine (AC1). |
| Home | `HomeEconomy` | – | – | – | The account day gets its expenses and purchases. |

No event opens the frame, switches a pane that is showing something, or closes a window.

### 3.2 Windows, focus and the pointer

- **The window layer** holds every window, and its sibling order is `WindowStack.ZOrder`. Two layers sit above it: the context-menu/Start-menu layer, then the toast/shortcut-card layer. The dock and the taskbar sit above all of these.
- **A press** anywhere on the desktop canvas:
  1. closes an open context menu or the Start menu when the press lands outside it;
  2. focuses the window under the pointer (WN2);
  3. on the empty desktop, deselects the icon and clears the focused window.
- **Double-click timing** is `ClickTiming.IsDouble(last, now, maxSeconds, maxDistance)`, used by icons and title bars alike.
- **A drag** (icon or title bar) begins past the EventSystem's threshold, follows the pointer in the parent's space through the press camera (as `DraggableWindow` does today), and is clamped with `RectClamp`. A maximised window does not drag.
- **The mouse wheel** scrolls the `ScrollRect` under the pointer, with the step as a knob. Over the tab strip it switches tabs; over the zoomed pane with Ctrl held it zooms.
- **Right-click** opens a context menu:
  - on the empty desktop: Arrange icons;
  - on an icon: Open;
  - on a taskbar button: Restore or Minimise, Close;
  - on a tab: Move left, Move right, Reset tab order;
  - on a row: Copy value, Copy row, Pin, Open link in the other pane, Pick for compare.
- **Hover hints** use today's `HoverHint` pattern on the desktop canvas (the tooltip role): 0.5 s on a tab, a ↗, a pin or a toolbar button.

### 3.3 Inside the frame and on the clone

- **The frame is unchanged.** Clicking the PC opens it; clicking outside, the close X, "< Desk" and Escape (last in the chain) close it.
- **The desktop takes input** only while `BoothRules.DesktopInteractive` is true: the frame open, the screen on, no newsletter. `MonitorScreen` already switches the desktop's raycaster, so icons, windows and shortcuts all follow it.
- **Held papers** beside the open frame keep their examine hole, and their rows pair with app rows (CM4). The dock shows the result, as the PC bar does today.
- **The clone** draws the same desktop (WN7). When the screen is off, the glass shows the art's own material, as today.

### 3.4 Keyboard shortcuts (live only while the frame is open)

| Keys | Context | Does |
|---|---|---|
| Ctrl+F | anywhere on the desktop | Opens or restores the Investigation app, focuses its search field and selects its text |
| Esc | anywhere | The Escape chain (§3.5) |
| F1 | anywhere | Shows or hides the shortcut card (this table, as a panel) |
| Enter | an icon selected | Opens it |
| arrows | an icon selected | Selects the nearest icon in that direction |
| Ctrl+1 … Ctrl+6 | the app | The active pane shows the tab at that position |
| Ctrl+Tab, Ctrl+Shift+Tab | the app | Next or previous tab in the active pane |
| Ctrl+Shift+PgUp, Ctrl+Shift+PgDn | the app | Moves the active tab left or right (reordering, saved) |
| F6, Shift+F6 | the app | The other pane becomes active |
| Ctrl+\ | the app | Split on or off |
| Ctrl+B | the app | Sidebar on or off |
| Ctrl+Shift+S | the app | Steps on or off |
| Alt+←, Alt+→ | the app | Back or forward in the active pane |
| Tab, Shift+Tab | the app | Next or previous region (KB4) |
| ↑ ↓ Home End PgUp PgDn | a list or form focused | Moves the focus ring over the rows in reading order |
| ← → | the tab strip focused | Previous or next tab |
| Enter | a row focused | Follows its smart link; in a list of items (chips, results, pins), opens the item |
| Ctrl+Enter | a row or search hit | The same, in the other pane |
| Enter | the search field | Opens the first hit |
| ↓ | the search field | Moves into the hits |
| Space | a row focused | Picks it for compare (the same as a click) |
| Ctrl+C, Ctrl+Shift+C | a row focused | Copies the value, or "Label: value" |
| Ctrl+V | a text field focused | Pastes (a foreign clip becomes a chip in the search field) |
| Ctrl+V | Notes focused, no field focused | Adds the clip as a clipping |
| Ctrl+P | a row, or a pane's item | Pins or unpins it |
| Ctrl+=, Ctrl+-, Ctrl+0 | the app | Zoom in, zoom out, zoom back to the Settings default |

While a text field has focus, only Ctrl+F, Ctrl+1…6, F6, Ctrl+\, Ctrl+B, Esc and F1 pass. Everything else is typing, and the field keeps its own Ctrl+C/V/X/A. In the office view (frame closed), no desktop shortcut is live.

### 3.5 The Escape chain

One press does one thing. The desktop rule (`DesktopEscapeRule.Resolve`) returns the first of:

1. `CloseMenu`: a context menu is open;
2. `CloseCard`: the shortcut card is open;
3. `CloseResults`: the search panel is open;
4. `ClearSearch`: the search field is focused and has text;
5. `LeaveField`: a text field is focused (focus returns to its pane);
6. `CloseStartMenu`: the Start menu is open;
7. `CancelDrag`: an icon or window drag is under way (it goes back where it started);
8. `None`.

On `None`, the frame closes (`OfficeViewController`), and the office's order continues as today: the wheel, the stamp tray, held papers, then the desk view (piece 10 X8, readability B2). The desktop poller runs before `OfficeViewController` (execution order), stamps the frame in which it took a press, and `OfficeViewController` ignores an Escape stamped in the same frame. This is the desktop's share of the single arbiter R5-005 proposes. `BoothCoordinator` can take the whole chain over later without changing the rule.

## 4. Data models

Domain types are pure C# in `TimeDesk.Domain`, with EditMode tests. Visuals types are in `TimeDesk.Visuals`, also tested. Engine types are in Assembly-CSharp. Every name below is a proposal; the build's first task re-reads the code.

### 4.1 Desktop and windows (Domain)

```csharp
/// An icon's top-left in desktop units, y down from the icon area's top.
public readonly struct IconPlace { public string Id; public float X, Y; }

/// The icon area and the arrange grid (from DesktopConfigSO).
public sealed class IconGrid { public float AreaWidth, AreaHeight, CellWidth, CellHeight, OriginX, OriginY, ColumnStep, RowStep; }

public static class DesktopLayout
{
    /// Column-first from the origin, in the given order.
    IReadOnlyList<IconPlace> Arrange(IReadOnlyList<string> order, IconGrid g);

    /// Clamped into the area; if the icon covers more than overlapShare of another cell,
    /// moved to the nearest free arrange spot. Otherwise exactly where it was dropped.
    IconPlace Drop(string id, float x, float y, IReadOnlyList<IconPlace> others, IconGrid g, float overlapShare);

    /// Parses "id:x,y;..." (invariant). Unknown ids are dropped, missing ones take the
    /// first free spots, and every place is clamped.
    IReadOnlyList<IconPlace> Restore(string saved, IReadOnlyList<string> order, IconGrid g);

    string Save(IReadOnlyList<IconPlace> places);

    /// Arrow-key selection: the nearest icon whose centre lies in that direction.
    string Nearest(string fromId, int dx, int dy, IReadOnlyList<IconPlace> places);
}

public static class ClickTiming
{
    bool IsDouble(float lastTime, float lastX, float lastY, float now, float x, float y, float maxSeconds, float maxDistance);
}

public sealed class WindowStack
{
    IReadOnlyList<string> ZOrder { get; }        // bottom to top, visible windows only
    IReadOnlyList<string> TaskbarOrder { get; }  // open order, minimised windows included
    string Focused { get; }                      // null when none
    bool IsOpen(string id); bool IsMinimised(string id);
    void Open(string id); void Focus(string id); void Minimise(string id); void Close(string id);
    void TaskbarClick(string id);                // focused and visible → minimise; else restore and focus
    event Action Changed;
}
```

### 4.2 Entries, the index and search (Domain)

```csharp
public enum AppSource { Documents, Records, Reference, Transcript, Report, Rules }   // append only (saved tab order)
public enum EntryScope { Case, Day }

/// A navigable item. The key is PickKeys' key where the item is pickable.
public readonly struct EntryRef { public string Key; public AppSource Source; public EntryScope Scope; }

public static class EntryKeys
{
    string Document(int doc);                         // "doc:0"
    // field:{d}:{f}, line:{i}, garment:{i} and book:{cat}:{nation}:{era} stay PickKeys'
    string RecordCard(string recordId);               // "rec:{id}"
    // PickKeys.Record(category, recordId) → "record:{id}:{category}"; id = the record's number, else its name   (R4-009)
    string Rule(int index);                           // "rule:1"
    string Deviation(ClueCategory c);                 // "dev:Currency"
    string Book(ClueCategory c);                      // "bookof:Currency" (the book as an item, for pins)
}

public sealed class IndexEntry
{
    public EntryRef Ref;
    public string Title;        // "Departure Manifest · Currency Carried"
    public string Label;        // English label (always searchable)
    public string Text;         // the value or line as shown when plain; for a Foreign line, its English key words only
    public bool Foreign;        // a transcript line shown untranslated (speech only: forms are always English, TR1)
    public string ForeignShown; // the glyphs as shown (the snippet)
    public string TongueId;     // for clip matching
    public string Canonical;    // for clip matching only; never displayed or matched against typed text
    public int Order;           // the item's order within its source
}

public sealed class Clip { public string Text, SourceKey, SourceLabel, TongueId, Canonical; public bool Foreign; }

public sealed class SearchQuery
{
    static SearchQuery Parse(string typed, Clip chip);  // words, "quoted phrases", an optional clip chip
    IReadOnlyList<string> Words { get; } IReadOnlyList<string> Phrases { get; } Clip Chip { get; } bool IsEmpty { get; }
}

public readonly struct Mark { public int Start, Length; }   // in the unfolded snippet
public readonly struct SearchHit { public EntryRef Ref; public string Title, Snippet; public IReadOnlyList<Mark> Marks; public int Score; public bool Foreign; }
public sealed class ResultGroup { public AppSource Source; public IReadOnlyList<SearchHit> Hits; public int More; }

public static class TextMatch
{
    string Fold(string s, List<int> map);             // lower-case, marks dropped, punctuation → space; map[i] = source index
    bool Matches(SearchQuery q, IndexEntry e, out int score, List<Mark> marks);
}

public sealed class CaseIndex
{
    void SetDay(IEnumerable<IndexEntry> entries);
    void BeginCase(); void Add(IndexEntry e); void Replace(IndexEntry e); void EndCase();
    IReadOnlyList<ResultGroup> Search(SearchQuery q, IReadOnlyList<AppSource> order, int perGroup, AppSource? only);
}
```

**What is indexed** (SE2):

| Source | Scope | Entry | Title | Searched text | Added |
|---|---|---|---|---|---|
| Documents | case | each scanned document | its name | its name | on scan |
| Documents | case | each field of a scanned document | "{document} · {label}" | label, plus the value (always plain, TR1) | on scan |
| Records | day | each record in today's registry (accounts and registry entries) | full name | every row's label and value (name, number, born, status, …) and the note | at day start |
| Reference | day | each `FactTable` row of today's places | "{book} · {place label}" | book, place, era, country, value, "revised" | at day start |
| Transcript | case | each line | "{speaker} · line {n}" | speaker, plus the line (plain), or its key words and glyphs (untranslated, SE5) | as spoken |
| Report | case | each deviation | "Deviation · {category}" | `UiText.Deviation(d)` | as logged |
| Rules | day | each rule | "Rule {n}" | `Summary()` | at day start |

**Matching** (SE3), with the untranslated rules of SE5:
- **Typed words** match only `Label` and `Text`. Each word must be the start of a folded word of the entry; each phrase must be a substring of the folded text.
- **A chip** matches only entries where `Foreign` is true, with the same `TongueId` and an equal folded `Canonical`.
- **Score:** 100 when the folded `Text` equals the folded query; +40 per word matched in the title, +20 in the text, +10 in the label; minus `Order / 1000`.
- A query that is empty, or shorter than 2 characters with no digit, gives no groups.

### 4.3 Smart links (Domain)

```csharp
public readonly struct LinkTarget { public AppSource Source; public string Key; public string Query; public bool ClaimedFilter; public bool IsNone; }
public readonly struct CaseClaim { public string NationId, EraId; }

public static class SmartLinks
{
    LinkTarget ForField(ClueCategory c, CaseClaim claim, string nameOnThisDocument);
    LinkTarget ForAnswer(ClueCategory c, CaseClaim claim, string primaryName);
    LinkTarget ForGarment(CaseClaim claim);                           // the Costume Guide's claimed row
    LinkTarget ForRule(string nationId, string eraId);                // the Reference, filtered to the place
    (LinkTarget statement, LinkTarget truth) ForDeviation(Discrepancy d, string statementKey, string truthKey);
    LinkTarget ForKey(string pickKey);                                // the dock's sides and pins
}
```

| From | Target |
|---|---|
| A field or answer of Currency, Language, Technology, Geography, Politics or Material | Reference: that category's book, the claimed place's row (`book:{c}:{claim}`) |
| A field or answer of Culture, or a garment | Reference: the Costume Guide, the claimed row |
| A field of Name | Records: the record found by this document's Citizen ID field, else by this Name |
| A field of Date of Birth, or of a record category (the traveller-types spec's `CitizenId`, `AccountStatus`, `TransponderId`, `TransponderClass`, `Debt`, `Credit`, `Funds`, `PolicyNo`, `Employer`, `Term`, `Wage`, `WaiverNo`, `Incident`) | Records: the same record, that category's row |
| A field of `Destination` | Records: the record's booked departure (a 2150 citizen) or registered origin (the displaced) |
| A field of `DepartureDate`, `Expiry` or `Signature` (the directive-only categories) | Rules (the directive that reads it) |
| An answer of Name or Date of Birth | Records: the record of the primary form's Citizen ID, else its Name |
| A rule | Reference with a place filter (the rule's nation, era or both); the first book with a row there |
| A deviation | its statement's key, and its truth's key (book row or record row) |
| A Reference row marked revised | Internet ▸ Chronopedia, the place's article at its revisions |
| A record's registered origin | Reference filtered to that origin, when a place of today's table has that label |
| A dock side, a pin, a recent | `ForKey` of its key |
| A category with no mapping (a later `ClueCategory`) | none: no ↗ is drawn |

### 4.4 Steps (Domain)

```csharp
public enum StepWhen { PapersReceived, PaperRead, Requested, RulesViewed, RecordViewed, Compared, Asked, LookedAt }
public enum TruthKind { Any, Reference, Record, Paper }   // Paper: statement against statement (the traveller-types spec's L4: papers prove)
public enum StatementKind { Any, Field, Answer, Garment }

public sealed class StepSpec
{
    public string Id; public StepWhen When;
    public ClueCategory[] Categories;   // empty = derive: Compared uses the categories on the papers; Asked uses today's questions
    public StatementKind Statement; public TruthKind Truth;
    public string[] DocumentKinds;      // template ids; empty = every document (PapersReceived, PaperRead, Requested)
    public LinkTarget Jump;             // IsNone → a hint toast (HintKey)
    public string HintKey;
}

/// What the player has done this case (fed by §3.1's events). Pure.
public sealed class CaseProgress
{
    void Received(int doc, string kind); void Read(int doc); void Requested(string kind); void RulesViewed(); void RecordViewed();
    void Compared(StatementKind s, ClueCategory c, TruthKind t); void Asked(ClueCategory c); void LookedAt();
    // plus the case's shape: the document kinds, the categories on the papers, today's question categories
}

public readonly struct StepState { public string Id; public bool Done; public int Have, Need; public bool Manual; }

public static class CaseSteps
{
    IReadOnlyList<StepSpec> Resolve(StepSetData sets, string travellerType);  // inherit; overrides by id; type unknown → default
    IReadOnlyList<StepState> Evaluate(IReadOnlyList<StepSpec> steps, CaseProgress p, IReadOnlyDictionary<string, bool> manual);
}
```

### 4.5 Clipboard, pins, recents, history, tabs, keys (Domain)

```csharp
public sealed class AppClipboard { Clip Current { get; } void Copy(Clip c); bool IsCurrent(string pastedText); }

public sealed class PinBoard
{
    IReadOnlyList<(EntryRef Ref, string Label)> Pins { get; }
    bool Toggle(EntryRef r, string label);   // false when full (20); the UI then shows "Unpin something first"
    void EndCase(); void EndDay();           // drop case-scoped / all pins
}

public sealed class RecentList { void Touch(EntryRef r, string label); void EndCase(); void EndDay(); IReadOnlyList<(EntryRef, string)> Items { get; } } // 10, newest first, no duplicates

public sealed class NavHistory<T> { void Go(T at); bool Back(out T at); bool Forward(out T at); bool CanBack { get; } bool CanForward { get; } }   // cap 30; Go clears forward

public sealed class TabOrder
{
    static TabOrder Parse(string saved);     // a missing or unknown source → the default order; duplicates dropped
    AppSource At(int position); int PositionOf(AppSource s); void Move(int from, int to); string Save();
}

public enum AppCommand { FocusSearch, Tab1, Tab2, Tab3, Tab4, Tab5, Tab6, NextTab, PrevTab, MoveTabLeft, MoveTabRight, OtherPane, ToggleSplit,
                         ToggleSidebar, ToggleSteps, Back, Forward, NextRegion, PrevRegion, Follow, FollowOther, Pick, Copy, CopyRow, Paste,
                         Pin, ZoomIn, ZoomOut, ZoomReset, Help, Escape, OpenIcon, IconLeft, IconRight, IconUp, IconDown, RowUp, RowDown,
                         RowFirst, RowLast, PageUp, PageDown }
public enum ShortcutKey { F, Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Tab, PageUp, PageDown, F6, Backslash, B, S, Left, Right, Up, Down,
                          Home, End, Enter, Space, C, V, P, Equals, Minus, Digit0, F1, Escape }
public readonly struct KeyChord { public ShortcutKey Key; public bool Ctrl, Shift, Alt; }
public readonly struct ShortcutContext { public bool FrameOpen, AppFocused, TextFieldFocused, MenuOpen, SearchOpen, IconSelected, RowFocused, TabStripFocused, NotesFocused; }
public static class ShortcutMap { bool Resolve(KeyChord k, ShortcutContext c, out AppCommand cmd); IReadOnlyList<(string keys, string whatKey)> Card(); }

public enum DesktopEscape { None, CloseMenu, CloseCard, CloseResults, ClearSearch, LeaveField, CloseStartMenu, CancelDrag }
public readonly struct DesktopEscapeState { public bool MenuOpen, CardOpen, ResultsOpen, SearchFocused, SearchHasText, FieldFocused, StartMenuOpen, Dragging; }
public static class DesktopEscapeRule { DesktopEscape Resolve(DesktopEscapeState s); }

public static class AppFocus { AppRegion Next(AppRegion from, bool split, bool sidebar, bool caseOn, int direction); }
public enum AppRegion { Search, TabStrip, PaneHeader, PaneContent, OtherPane, Sidebar, Dock, Decision }
```

### 4.6 Internet, Mail, Account, Notes (Domain)

```csharp
public enum SiteKind { News, History, Ancestry, Static }
public sealed class SiteSpec { public string Id, Name, Domain, Glyph, Blurb; public SiteKind Kind; public int FromDay; }
public static class Sites { bool Listed(SiteSpec s, int day); }   // no upgrade gate (IN2)

/// A page as blocks for FormLayout (§6.1): what the page builders return.
public sealed class SitePage { public string Address, Title; public List<PageBlock> Blocks; }

[Serializable] public sealed class NewsIssue { public int day; public List<string> briefing = new(); public List<string> news = new(); }
public static class NewsArchive { void Record(List<NewsIssue> archive, int day, IReadOnlyList<string> briefing, IReadOnlyList<string> news, int cap); } // replaces a same-day issue; drops the oldest over cap
public static class NewsPages { SitePage Front(NewsIssue today, IReadOnlyList<RankedScore> ranking, IReadOnlyList<NewsIssue> archive); SitePage Issue(NewsIssue n); }

public readonly struct PlaceInfo { public string Id, NationId, EraId, DisplayName, Moment; public int Year; }
public static class HistoryPages
{
    SitePage Index(IReadOnlyList<PlaceInfo> places, string futureNationId);
    SitePage Article(PlaceInfo p, IReadOnlyList<FactRow> today, Func<ClueCategory, string> baseValue, IReadOnlyList<FactEdit> edits, IReadOnlyList<CarryRecord> carries);
    SitePage Revisions(IReadOnlyList<FactEdit> edits, IReadOnlyList<CarryRecord> carries, Func<string, string, string> placeLabel);
}

public sealed class PersonCard { public string Id, Name, Born, Died, PlaceId, Note; public List<(string kind, string personId)> Relations; }
public static class AncestryPages { IReadOnlyList<PersonCard> Search(IReadOnlyList<PersonCard> all, SearchQuery q, string nationId, string eraId); SitePage Card(PersonCard p, Func<string, string> placeLabel); }

public enum MailKind { DirectiveMemo, TimesIssue, CitationNotice, Authored }
public sealed class MailItem { public string Id; public int Day; public MailKind Kind; public string From, Subject; public List<PageBlock> Body; public LinkTarget Link; }
public static class Mailbox { IReadOnlyList<MailItem> ForDays(/* rules per day, archive, ledger citations, authored, day, flags */); int Unread(IReadOnlyList<MailItem> items, ICollection<string> read); }

[Serializable] public sealed class AccountDay { public int day, wages, fines, debtRelief, household, purchases, balance, owed; }
public static class Account { void Record(List<AccountDay> days, AccountDay d, int cap); }
// The clerk's rows come from the traveller-types spec's agency.clerk and its ClerkDebt rule (D1-D2); no ID is generated.

[Serializable] public sealed class Clipping { public string text, label, sourceKey, traveller; public bool foreign; }
[Serializable] public sealed class NotePage { public int day; public string text = ""; public List<Clipping> clippings = new(); }
public static class Notes { NotePage Today(List<NotePage> pages, int day); bool Clip(NotePage p, Clipping c, int maxClippings); string Trim(string text, int maxChars); }
```

### 4.7 Saves and preferences

**`WorldState` gains** four additive fields: `newsArchive` (`List<NewsIssue>`, at most 30), `mailRead` (`List<string>`), `accountDays` (`List<AccountDay>`, at most 60), `notes` (`List<NotePage>`, at most 30 days, each at most 2,000 characters and 40 clippings). JsonUtility loads an older save with them empty, as it did with `history`. (The traveller-types spec adds `clerkDebtPaid`, also additive.) So `SaveSystem.SaveVersion` stays 2, and `MinCompatibleVersion` is unchanged (house rule: bump only on an incompatible change). Notes and mail flags are saved when the game saves (the end-of-shift save). A quit mid-shift loses that shift's notes, as it loses the shift.

**PlayerPrefs**, through one `DesktopPreferences` static class (the `UiLanguagePreference` pattern; saved at once):

| Key | Values | Default |
|---|---|---|
| `TimeDesk.DesktopIcons` | `id:x,y;…` | absent: arrange |
| `TimeDesk.IconOpen` | `double`, `single` | `double` |
| `TimeDesk.AppTabs` | `Documents,Records,…` | the default order |
| `TimeDesk.StepsShown` | `shown`, `hidden` | `shown` |
| `TimeDesk.SidebarShown` | `shown`, `hidden` | `shown` |
| `TimeDesk.AppSplit` | `on`, `off` | `on` |
| `TimeDesk.AppZoom` | `100`, `125`, `150` | `100` |

### 4.8 Content (`world_source.json`, through Generate World)

A new top-level `pc` block, generated into the content library (`ContentLibrarySO.Pc`), with validator rules for unique ids, known kinds, `when` values, categories and place ids, days of 1 or more, and every `ui.strings` key a step or site asks for:

```json
"pc": {
  "steps": {
    "sets": [
      { "type": "default", "steps": [
        { "id": "rules",     "when": "RulesViewed",                          "jump": { "source": "Rules" } },
        { "id": "papers",    "when": "PapersReceived",                       "jump": { "source": "Documents" } },
        { "id": "read",      "when": "PaperRead",                            "jump": { "source": "Documents" } },
        { "id": "identity",  "when": "Compared", "categories": ["Name", "BirthDate"], "truth": "Record", "jump": { "link": "primaryName" } },
        { "id": "facts",     "when": "Compared", "statement": "Field", "truth": "Reference", "jump": { "link": "firstUncheckedField" } },
        { "id": "questions", "when": "Asked",                                "hint": "steps.hint.wheel" },
        { "id": "answers",   "when": "Compared", "statement": "Answer", "truth": "Reference", "jump": { "source": "Transcript" } },
        { "id": "dress",     "when": "Compared", "categories": ["Culture"], "statement": "Garment", "truth": "Reference", "jump": { "link": "costumeClaimed" } }
      ] }
    ]
  },
  "sites": [
    { "id": "news",     "kind": "News",     "name": "The Temporal Times", "domain": "times.tc",  "glyph": "site_news",     "blurb": "Today's edition and back issues.", "fromDay": 1 },
    { "id": "history",  "kind": "History",  "name": "Chronopedia",        "domain": "chronopedia", "glyph": "site_history",  "blurb": "Every place and era, as history stands today.", "fromDay": 1 },
    { "id": "ancestry", "kind": "Ancestry", "name": "Lineage Archive",    "domain": "lineage",     "glyph": "site_ancestry", "blurb": "Citizens of the past.", "fromDay": 1 }
  ],
  "pages": [],
  "ancestry": { "includePremades": true, "people": [] },
  "mail": [ { "id": "welcome", "fromDay": 1, "untilDay": 1, "from": "Customs Directorate", "subject": "Welcome to the desk", "body": ["…"] } ]
}
```

The sample texts are placeholders for Saleh's words. The agency's printed name and programme line are two keys of the traveller-types spec's `agency` block (`agency.name` "TEMPORAL CUSTOMS", `agency.programme` "Debt Relief Departures"), beside its `firstDate` and its `clerk` (the clerk's own account), so there is one agency block. Every `pc` table is row-shaped (CT1). **New `ui.strings` keys** are in the Full tier, except the 4 new icon labels (Flavour, TH3). They fall under these prefixes:
- `app.*`: tab names, the toolbar, the case header's counters, the no-case line, the chip states;
- `search.*`: the placeholder, the group heads, no-hit;
- `steps.*`: each step's label and the hints;
- `pins.*`, `recent.*`, `toast.*`, `menu.*`, `desktop.*`, `browser.*`, `mail.*`, `account.*`, `notes.*`, `settings.*` (the new rows), `keys.*` (the shortcut card).

**Retired `ui.strings` keys** (each goes in the commit that removes its last caller):
- `icon.directives`, `icon.scanner`, `icon.records`, `icon.lexicon`, `icon.dialect`, `icon.material`, `icon.clueLog`;
- `window.directives`, `window.scanner`, `window.lexicon`, `window.dialect`, `window.material`, `window.transcript`;
- `records.title`, `body.*`, `startmenu.settings` (the Start menu lists Settings by `icon.settings`), `window.prev`, `window.next`.

`window.page` stays for the "Page 2 of 2" rule. `window.internet` and `window.notes` stay, as the new apps' titles, with new texts ("Internet", "Notes").

### 4.9 Knobs (ScriptableObjects)

**`DesktopConfigSO`** (`Assets/Data/Office/Desktop_Default.asset`, ensured by the builder like `DeskConfigSO`):

| Group | Knobs |
|---|---|
| Icons | the icon area, the cell size, the arrange origin and steps, the default order (ids), the drop overlap share (0.25), the double-click time (0.4 s) and distance (6 u) |
| Windows | each app's restored size, which apps open maximised, the title bar height |
| The app | the sidebar width, the minimum pane width (520 u), the heights of the header, toolbar, tab strip and pane header, the collapsed tab width |
| Search | the minimum characters, the debounce, hits per group, the most hits shown |
| Behaviour | the found flash (pulses, seconds), the toast seconds and the most at once, the most pins and recents, the history cap, the zoom levels |
| News, mail, notes | the archive and history caps, the notes limits |

**`FormStyleSO`** (`Assets/Data/Forms/FormStyle_Agency.asset`) holds the colours, sizes, stroke widths and per-site styles of §6.4. `DeskConfigSO.face` (`PaperFaceTuning`) goes (FO10).

### 4.10 The scanner upgrades (Domain, knobs)

```csharp
/// The day's scanner upgrades, read from the day-start snapshot like every upgrade (SC1).
public readonly struct ScannerDay { public bool AutoFeed, Analysis; public static ScannerDay From(GateSnapshot dayStart); }

// DeskPapers (grown, SC3): the Auto-Feed queue.
//   void Enqueue(int doc);                    // on hand-over, when AutoFeed
//   int NextToScan(Func<int, bool> onDesk);   // the first queued paper lying on the desk, or -1; the scanner takes one at a time

/// One marked pair (SC4): two fields on two scanned documents.
public readonly struct AnalysisMark { public int DocA, FieldA, DocB, FieldB; }

public static class PaperAnalysis
{
    /// The first pair of the traveller-types spec's PaperChecks.Contradictions (document order, then field order)
    /// among the scanned documents whose category is not documented yet; null when there is none.
    AnalysisMark? First(IReadOnlyList<CaseDocument> scanned, ICollection<ClueCategory> documented);
}
```

Knobs: `DeskConfigSO.analysisScanSeconds` (3 s; the plain scan keeps `scanSeconds`, 1.5 s); `FormStyleSO`'s `analysis` colour (the dashed mark, checked at the outline class, FO7); the two upgrades' costs on their `UpgradeSO` assets.

## 5. Components and the builder

### 5.1 Assembly-CSharp (engine side; each is thin over the Domain/Visuals rules)

| Component | Job |
|---|---|
| `DesktopWindowManager` (on the desktop canvas) | Owns the `WindowStack`, applies the z-order, focuses on a press (WN2), maximise and restore (WN3), the taskbar buttons, the idle line (DK10) |
| `DesktopWindow` (was `OSWindowChrome`, `[FormerlySerializedAs]` for its fields) | Title-bar buttons through the manager; double-click on the title to maximise; the restored rect |
| `WindowDrag` (was `DraggableWindow`, without its dead members: R4-016) | Title-bar drag inside the parent (`RectClamp`) |
| `DesktopIconView` (replaces `DesktopIcon`) | Select, drag, drop, open (`ClickTiming`, `DesktopLayout`), badge; no upgrade gate and no reach into `RunManager` (R4-020) |
| `DesktopShell` (grown) | The Start menu's app entries and "Arrange icons"; the desktop's context menu |
| `ContextMenu` | One menu panel, filled per target (§3.2) |
| `DesktopKeyboard` | The one poller (KB2), the Escape chain's desktop part (§3.5), the shortcut card |
| `CompareDock` | The dock's texts and side links (the texts are still written by `CompareController`) |
| `InvestigationUIController` (the façade: RF1) and `CaseDocumentsPresenter`, `InterviewPresenter`, `EvidencePresenter`, `DayReference` | The case and day model the app shows |
| `InvestigationApp` | The window shell: header, toolbar, sidebar, panes, split, zoom, badges, toasts, the case index and the navigator |
| `AppPane` | One pane: its tab strip, pane header, content host and `NavHistory` |
| `DocumentsView`, `RecordsView`, `ReferenceView`, `TranscriptView`, `ReportView`, `RulesView` | Each fills a `FormView` from its presenter, reveals an entry key (select, scroll, focus, flash) and reports its events (§3.1); the Documents view draws the analysis marks (SC4), the Reference view the Costume Guide's era groups (§2.6) |
| `DeskController`, `DeskScanner` (changed) | The Auto-Feed queue (SC3), the analysis pass and its longer scan (SC4), the placeholder tray and lamp (SC6) |
| `FormView` | Renders a placed form with pooled uGUI parts (§6.5), keyed pick highlights (CM3), the found flash, the focus ring, link and pick hit boxes |
| `SearchBox`, `SearchResultsView` | SE1 and SE4 |
| `StepsPanel`, `PinsPanel`, `RecentPanel` | The sidebar |
| `BrowserWindow`, `SiteRenderer` | IN1: the page builders' `SitePage` drawn by `FormView` |
| `MailWindow`, `AccountWindow`, `NotesWindow` | ML1, AC1, NT1 |
| `SettingsWindowController` (grown) | SG1 |
| `DesktopPreferences` (static) | The PlayerPrefs of §4.7 |
| `DeskDocument` (changed) | Draws the desk paper's form from `FormLayout` (§6.5) |

### 5.2 The builder (`OfficeSceneUIBuilder*.cs`, authoritative)

Build Office UI builds, wires and theme-tags:
- the six icons (in the default arrangement; runtime restores the saved one);
- the window manager, the taskbar buttons' template, the Start menu's entries, the context menu, the dock;
- the Investigation app: its shell, its two pane hosts, the templates for tabs, chips, search hits, step, pin and recent rows and toasts, and every `FormView` part template;
- the Internet, Mail, Account and Notes windows, and the grown Settings window;
- `DesktopConfigSO` and `FormStyleSO` (ensured once);
- the scanner's placeholder feeder tray and lamp, shown by the owned upgrades (SC6), in `.Desk.cs`.

It removes the retired objects with `DestroyChildIfPresent`: `BookShelf`, the nine app windows and their icons, `DirectivesWindow`, `IconScannerWindow`, `RecordsWindow`, `TranscriptWindow`, the document and book window templates, `ClaimStrip`/`ClaimBanner`, `CompareBar`, `AcceptButton`/`DenyButton` on the desktop, and the `InvestigationRoot` dim.

It checks:
- every form spec (`FormLayout.Check`: every field placed once, the desk face fits at the floors, the longest value fits);
- the form style's contrast pairs in every theme (FO7);
- the theme tags and `UiContrastCheck` on the new chrome;
- the site upgrade ids (moved from the icon check).

The desk paper's parts (the text templates, the box mesh material, the seal quad) are built in `.Desk.cs` beside today's paper parts. The art scene is never opened.

## 6. Forms

### 6.1 The model (Visuals)

```csharp
public enum FormBlockKind { Header, Section, FieldRow, RecordGroups, Checkboxes, Table, Paragraph, Signature, Issued, Barcode, FinePrint, StampArea, Footer, PageBreak, Masthead, Headline }

[Serializable] public sealed class FormCell
{
    public int field = -1;     // the template field shown here (documents), or -1
    public string slot;        // a named content slot ("photo", "caseLine", "rows", ...) for page kinds
    public string caption;     // the box's label when it is not a field's own label
    public int span = 12;      // of 12 columns
    public int rows = 1;       // rows tall (the photo spans 2)
}

[Serializable] public sealed class FormBlock
{
    public FormBlockKind kind;
    public string text;                 // section title, paragraph, fine print, caption (diegetic English)
    public FormCell[] cells;            // FieldRow
    public string[] columns; public float[] shares;   // Table
    public string[] options; public int field = -1;   // Checkboxes: the option equal to the field's value is ticked
    public string slot;                 // Table rows / Paragraph text from content
}

[Serializable] public sealed class FormSpec
{
    public string formNumber;           // page kinds only ("TC-930"); a document form leaves it blank and prints its template's
                                        // formNumber (traveller-types spec §3) and displayName: the validator rejects one that sets them
    public string title;                // page kinds only ("DEVIATION REPORT")
    public bool fixedPage = true;       // documents: the paper's aspect; page kinds: flow
    public FormBlock[] blocks;
    int PageOf(int field);              // pages split at PageBreak
}

public sealed class FormData            // what one form shows (built by the caller)
{
    public string Serial; public bool HasPhoto;
    public IReadOnlyList<string> FieldShown;           // shown strings per field index (for measuring)
    public IReadOnlyDictionary<string, string> Text;   // slot → text
    public IReadOnlyDictionary<string, IReadOnlyList<string[]>> Rows; // slot → table rows (shown)
    public IReadOnlyList<(string Title, IReadOnlyList<(string Label, string Value)> Rows)> Groups; // a record's groups (RecordGroups)
}

public interface ITextMeasure { float Height(string text, float size, float width); }   // TMP's preferred height; tests fake it

public enum FormItemKind { Seal, Text, Box, Rule, Photo, Checkbox, Bar, StampArea, RowBand }
// RecordGroups lays out a record's groups (FormData.Groups: a title and (category, label, value) rows each) as a Section and
// boxes two to a row, a long value across the row: a record's shape is data (the traveller-types spec's R1).
public enum FormTextRole { Agency, Programme, FormNumber, Title, Serial, Section, Label, Value, Cell, Paragraph, Caption, FinePrint, Masthead, Headline }
public readonly struct FormItem { public FormItemKind Kind; public FormTextRole Role; public FaceRect Rect; public int Slot; public string Text; public float Size; }
public readonly struct FormSlot { public int Index; public string Key; public FaceRect Hit; public int Page; }  // reading order; Key = the entry key suffix (field or row)
public sealed class PlacedForm { public float Width, Height; public IReadOnlyList<FormItem> Items; public IReadOnlyList<FormSlot> Slots; public IReadOnlyList<float> PageTops; }

public static class FormLayout
{
    PlacedForm Layout(FormSpec spec, FormData data, float width, FormMetrics m, ITextMeasure measure);   // width in the caller's units; fixed pages take height = width / aspect per page
    int SlotAt(PlacedForm f, float x, float y);                                               // −1 off every slot
    IReadOnlyList<string> Check(FormSpec spec, int fieldCount, bool deskFace, FormMetrics m, ITextMeasure measure, int longestValue);
}

public static class Barcode { IReadOnlyList<(int start, int width)> Bars(string serial, int modules); }   // guard bars; deterministic; never wider than modules
```

`FormMetrics` (on `FormStyleSO`, shared by the desk and the PC):
- the text sizes of FO6 in H;
- the value floor;
- the box padding, line and rule widths, section gap;
- the header and footer heights;
- the paper aspect (0.765: the desk paper's 0.26 × 0.34 m);
- the seal size and tint.

### 6.2 The forms (the specs the phases author)

**Documents.** Every template that exists when the forms phase runs gets a form. In the plan's order those are the displaced's three (TC-610, TC-620, TC-630): the passport and the permit are retired one phase earlier, so they never get one. Each later phase that adds a template authors its form in the same phase. Every document form follows one pattern:
- the primary form's name box beside the 2-row photo;
- short fields two to a row;
- a long field (a destination, an employer, a transponder) across the row;
- a `Signature` block only on a form with a Signature field;
- a `Footer` with the issuing facsimile, the barcode and serial, the stamp area and fine print.

TC-610 Displacement Certificate (the traveller-types spec's §3.8; its fields: 0 Full Name, 1 Displacement No., 2 Date of Birth, 3 Origin, 4 Incident, 5 Valid Until; one page):

```
Header
Section  "1  DISPLACED PERSON"
FieldRow [ field 0 span 8 ][ slot photo span 4 rows 2 ]
FieldRow [ field 1 span 8 ]
FieldRow [ field 2 span 6 ][ field 5 span 6 ]
Section  "2  INCIDENT"
FieldRow [ field 3 span 12 ]
FieldRow [ field 4 span 6 ]
Footer   issued · barcode + serial · stamp area
         · fine print "Issued under the Temporal Customs Act of 2139. Any alteration voids this document. Property of Temporal Customs; surrender on request."
```

TC-620 Intake Declaration (its §3.9; its fields: 0 Declarant, 1 Displacement No., 2 Coin of Home, 3 Native Tongue, 4 Effects Carried; no photo):

```
Header
Section  "1  DECLARANT"
FieldRow [ field 0 span 6 ][ field 1 span 6 ]
Section  "2  DECLARATION"
FieldRow [ field 2 span 6 ][ field 3 span 6 ]
FieldRow [ field 4 span 12 ]
Paragraph "The declarant states the above to be true. Taken down in English by the desk officer."
Footer
```

TC-101 Leisure Departure Visa (the traveller-types spec's §3.1; its fields: 0 Full Name, 1 Citizen ID, 2 Date of Birth, 3 Destination, 4 Visa Class, 5 Valid Until; one page):

```
Header
Section  "1  TRAVELLER"
FieldRow [ field 0 span 8 ][ slot photo span 4 rows 2 ]
FieldRow [ field 1 span 8 ]
FieldRow [ field 2 span 6 ][ field 5 span 6 ]
Section  "2  DEPARTURE"
FieldRow [ field 3 span 12 ]
FieldRow [ field 4 span 6 ]
Footer
```

TC-310 Stranding Waiver (its §3.3) ends with `Signature` bound to its field 5, above the Footer. At the §6.3 sizes, a 6-field form laid out this way uses about 0.9 H on the desk face. `FormLayout.Check` holds every form to it.

**Page kinds** (`FormSpecSO` assets, `fixedPage = false`):

| Asset | Number | Blocks (in order) | Slots |
|---|---|---|---|
| `Form_RecordExtract` | TC-901 | Header · Paragraph (the query line) · `RecordGroups` (each group as a Section and its rows as boxes, two to a row, a long value across) · Paragraph (the note) · Footer (stamp and fine print only) | `query`, `groups`, `note` |
| `Form_Register` | TC-911 … TC-916 | Header · Paragraph (the edition line) · Table [PLACE .34, ERA .16, VALUE .36, NOTE .14] · FinePrint | `edition`, `rows` |
| `Form_InterviewRecord` | TC-920 | Header · Paragraph (traveller, officer, day) · Table [NO .08, SPEAKER .22, STATEMENT .70] · FinePrint | `head`, `rows` |
| `Form_DeviationReport` | TC-930 | Header · Paragraph (the case line) · Table [NO .06, CATEGORY .14, STATEMENT .30, CONTRADICTED BY .30, PROOF .20] · Paragraph (none/further) · Signature "Desk officer, day {n}" · StampArea · FinePrint | `caseLine`, `rows`, `tail` |
| `Form_DirectiveMemo` | TC-940 | Header · FieldRow [TO 6][DATE 6] · FieldRow [FROM 6][REF 6] · Table [NO .08, DIRECTIVE .92] · Issued "Customs Directorate" · StampArea | `to`, `date`, `from`, `ref`, `rows` |
| `Form_Memo` (mail) | TC-950 | as the directive memo, with SUBJECT and a Paragraph body | `to`, `date`, `from`, `ref`, `subject`, `body` |
| `Form_Statement` | TC-960 | Header · Table [DAY, WAGES, FINES, HOUSEHOLD, PURCHASES, BALANCE] · FinePrint · StampArea (under the clerk's Record Extract) | `rows` |
| `Form_RecordCard` (Ancestry) | LA-1 | Header · FieldRow [NAME 8][BORN 4] · FieldRow [PLACE 8][DIED 4] · Paragraph (note) · Table [RELATION, NAME] | `name`, `born`, `place`, `died`, `note`, `rows` |
| `Form_Infobox` (History) | CP-1 | FieldRow ×3 (two boxes each, six facts) | `capital`, `ruler`, `currency`, `language`, `technology`, `dress` |

**Site page styles** (not forms; the same blocks): News uses Masthead · Headline · Paragraph columns · a boxed Table for the notices and the standings. History uses Headline · Paragraph (year and moment) · the infobox · a Table of revisions. Ancestry uses the record card.

### 6.3 Sizes and readability (FO6)

A PC page in a maximised pane is 542 u wide (the 580 u pane less its padding and scrollbar), so a document page is H = 708 u. A held desk paper is 0.44 screen heights (piece 10 X1).

| Text | Size (H) | PC page (708 u): 1080p / 720p | Held paper: 1080p (475 px) / 720p (317 px) |
|---|---|---|---|
| Value | 0.049 (floor 0.042) | 34.7 u: **27.0 / 18.0 px** (floor 23.1 / 15.4) | **23.3 / 15.5 px** (floor 20.0 / 13.3) |
| Label | 0.038 | 26.9 u: 20.9 / 13.9 px | 18.1 / 12.0 px |
| Section head (bold capitals) | 0.036 | 25.5 u: 19.8 / 13.2 px | 17.1 / 11.4 px |
| Title | 0.052 | 36.8 u: 28.6 / 19.1 px | 24.7 / 16.5 px |
| Form number, serial | 0.026 | 18.4 u: 14.3 / 9.5 px | 12.4 / 8.2 px |
| Fine print | 0.020 | 14.2 u: 11.0 / 7.3 px | 9.5 / 6.3 px |
| Chrome (tabs, sidebar, header, dock) | 20–28 u | 15.6–21.8 / 10.4–14.5 px | – |

- **The 720p floor.** No evidence value is drawn under 13 px, and no label under 12 px. A value keeps today's desk row size (15.5 px at 720p) until it has to shrink. Form numbers, serials and fine print carry nothing needed to play. They must still pass contrast, but they may be smaller; zoom (KB5) makes them legible.
- **Box heights.** A one-line box is 0.114 H (label, value, padding); a two-line box is 0.157 H. `FormLayout.Check` measures, with the injected measure, the longest value each field's category can hold (`FactTable.MaxValueLength` for place facts; the traveller-types spec's generators for the record categories) and reserves one or two lines.
- **Untranslated glyphs** may shrink below the floor. They are not readable text; their evidence is the placeholder in the dock.
- **A 6-field form's budget** (fixed page, in H): header 0.12, section heads 0.07, five box rows 0.57, gaps 0.04, footer 0.12. That is 0.92, inside 1.0. Today's passport uses 0.79.

### 6.4 Style and contrast (FO7)

`FormStyleSO` holds these colours (starting values; the check decides):
- paper `#F4F0E4`, ink `#1A1714`, label ink `#3B342B`, rule `#5B5347`, box fill `#FBF8F0`;
- link `#1F4E8C`, found `#FFD54A` at 45 %, the analysis mark `#B0452A` (dashed), fine-print ink `#4A443A`, stamp dash `#6B6358`, seal at 10 %;
- the scanner backing `#212329` with its strip text `#D9DBE0`;
- per site: News `#F2EFE6`/`#141414`, History `#FFFFFF`/`#202122` (link `#2A5DB0`), Ancestry `#EFE6D2`/`#2B2118`.

Build Office UI checks each pair at its class through `Contrast.Ratio`:
- ink and label on the paper and on the box fill (4.5);
- the link on both (4.5);
- ink on the found fill composited over the box fill (4.5);
- ink on **each theme's `SelectionHighlight`** composited over the box fill (4.5, in all nine themes: the pick fill is themed);
- fine print on the paper (4.5);
- rule, stamp dash and the analysis mark on the paper and the box fill (3.0, the outline class);
- each site's pairs.

The rendered audit (the readability pass's job) stays the final word. It gains the states of §11's checks, and it measures the held paper under the art's tonemapping as today (readability C3).

### 6.5 The two renderers

**`FormView` (uGUI):**
- Pools one object kind per `FormItemKind`. Texts are cloned from one TMP template per `FormTextRole` (tagged `DiegeticForm`, so the theme skips them, with colours and fonts from `FormStyleSO`).
- Boxes are an `Image` with a code-drawn 9-slice rule sprite (the builder makes it, like the other placeholders), and barcode bars are `Image`s. The seal is an `Image` (the art, or a code-drawn ring).
- The photo is `TravellerPortraitView`, as today.
- Each slot gets a pick `Button` over its box and, when it links, a ↗ `Button`.
- Field values are written plain (TR1: forms are always English); transcript lines go through `TextFlip.Write`.
- It rebuilds only when its item or data changes. A flip ticks in `Update` only while one runs, as today.

**`DeskDocument` (world):**
- Keeps its sheet, collider, drag, click routing (`PaperClicks`), examine material and photo.
- Replaces its rows with the form's items: TextMeshPro per text item (from per-role templates), and **one mesh per paper** holding every box outline, rule and barcode bar, built when the paper binds, in a line material in the style's rule colour. There is a quad for the seal and one highlight quad per slot.
- `RowAt` becomes `FormLayout.SlotAt` on the sheet's local point. The hover tint and the pick highlight work as piece 10 made them; the paper never flips (TR1).

### 6.6 Art (text-free, per the asset list's rules)

| Item | File | Size | Tier | Notes |
|---|---|---|---|---|
| Agency seal | `Assets/Art/UI/Forms/agency_seal.png` | 512 × 512 | 2 | Greyscale, Temporal Customs' own abstract mark (for example an hourglass in a ring). Not a flag, crest or nation's emblem, and no letters. Printed at 10 % behind the header. Placeholder: a code-drawn ring. |
| A face per document kind (the traveller-types spec's ten TC forms) | `Assets/Art/Office/Placeholder/paper.png` becomes `Assets/Art/UI/Forms/paper_<form number>.png` | 1024 × 1339 | 2 | Paper tone, printed border, a guilloche band behind the header, perhaps a kind's tint (visa, contract, displacement papers). **No boxes or lines**: the code draws them. The 2026-09-25 decision "one face per document kind" stands. |
| Agency page face | `Assets/Art/UI/Forms/paper_agency.png` | 1024 × 1339 | 2 | Every PC page kind: plain agency paper. Tiled vertically on flow pages. |
| Photo frame | code-drawn box | – | – | As the asset list already says. |
| Verdict ink marks | `stamp_accept.png`, `stamp_deny.png` (already listed) | 400 × 200 | 2 | They land in the stamp area. |

## 7. Theme roles

| New role (appended) | Diegetic | Used by | Palette rule (sketch) |
|---|---|---|---|
| `TabStrip` | no | the pane tab strips | the chrome fill |
| `Tab` | no | an inactive tab (fill and ink) | chrome, lighter; Text class |
| `TabActive` | no | the active tab | window body; the accent ink |
| `Sidebar` | no | the sidebar and its rows | window body, darker; Text class |
| `SearchResults` | no | the results panel and its hits | the tooltip's fill; Text class |
| `Badge` | no | the counts and dots | the accent; ink white; Text class |
| `Toast` | no | toasts | chrome deep; Text class |
| `FocusRing` | no | the keyboard focus outline | the accent; outline class (3:1) |
| `IconSelection` | no | the selected icon's plate | the accent at 45 % |
| `DiegeticForm` | **yes** | every form part (texts, boxes, seal, barcode, stamp area, links) | none (the form style) |
| `SiteContent` | **yes** | the Internet's page content | none (the site styles) |

Reused roles:
- `ClaimStrip`: the app's claim band, the office claim tag.
- `CompareBar`: the dock, the office strip.
- `AcceptButton`, `DenyButton`: the app header.
- `TitleBar`, `WindowBody`, `Button`, `CloseButton`, `InputField`, `InputPlaceholder`: every new window.
- `StartMenu`, `MenuEntry`: the context menu.
- `DesktopIcon`: the icon plate and label.
- `Tooltip`: hover hints.
- `DiegeticBacking`: the scanner backing.

`DeskDim` and `StickyNote` leave the palette map (TH2).

## 8. Tests (EditMode; offline first, then in the Unity job)

| Test class | Covers |
|---|---|
| `DesktopLayoutTests` | Arrange order and wrapping; a drop is clamped; an overlap over the share moves to the nearest free spot, under it stays; Restore drops unknown ids, places new ones, clamps; Save/Restore round trip with invariant numbers; `Nearest` in the four directions |
| `ClickTimingTests` | Inside and outside the time; inside and outside the distance; the first click is never double |
| `WindowStackTests` | Open, focus, minimise (focus passes), restore; `TaskbarClick` both ways; close; z-order and taskbar order; `Changed` fires once per change |
| `TabOrderTests` | The default; parse with unknown, missing and duplicate entries; move; save round trip; positions |
| `TextMatchTests` | Fold (case, accents, punctuation) with the index map; word starts; phrases; marks mapped back through accents |
| `CaseIndexTests` | The two layers; `EndCase` keeps the day layer; grouping in the given order; per-group cap and More; source filter; ranking; **untranslated lines: typed text matches only their key words and speaker, never the hidden English; a chip matches the same tongue and canonical only; document fields are always plain**; the Records lookup scope |
| `SmartLinksTests` | Every row of §4.3; the claimed row key; Date of Birth uses the document's Name; unmapped categories give none; `ForKey` of each key kind |
| `CaseStepsTests` | Each `StepWhen`; progress n of m; a MISMATCH pair ticks like a MATCH (never judges); a manual override holds; set resolution with inherit and override; an unknown type falls back to default |
| `PinBoardTests`, `RecentListTests`, `NavHistoryTests` | Caps, scopes (`EndCase`, `EndDay`), no duplicates, Go clears forward |
| `AppClipboardTests` | Copy and replace; `IsCurrent`; a foreign clip keeps its tongue |
| `ShortcutMapTests` | Every §3.4 row; the text-field filter; nothing resolves with the frame closed; the card lists every command once |
| `DesktopEscapeRuleTests` | The order of §3.5, one row per state; `None` when nothing is open |
| `AppFocusTests` | The region order with split, sidebar and case on or off |
| `FormLayoutTests` | No two items overlap; every item inside the page; reading order; spans and multi-row cells; the photo narrows nothing it does not cover; the page breaks and `PageOf`; flow height grows with rows (fake measure); `SlotAt` in and between boxes; `Check` reports an unplaced or twice-placed field, a desk face that does not fit, a value that does not fit at the floor |
| `BarcodeTests` | Deterministic; guard bars at both ends; total width = modules; different serials, different bars |
| `SeedsTests` (grown) | `ForForms` is distinct from every other salt |
| `ThemeRolesTests` (grown) | `IsDiegetic` for every enum value, the two new diegetic roles included |
| `NewsArchiveTests`, `NewsPagesTests` | Record, replace a day, cap; the lead is the first news line; the standings order |
| `HistoryPagesTests` | An article's values are `History.Resolve`'s; a revised value shows its day and the base value; the present's article follows `TodaysWorld.Present`; revisions are newest first, carries included |
| `AncestryPagesTests` | Premades with an empty `truePlace` only; search through `TextMatch`; country and era filters |
| `MailboxTests` | Each kind from its source; days; `untilDay` and flags; unread count |
| `AccountTests` | Record and cap; the OWED column follows the instalments (the traveller-types spec's `ClerkDebt`) |
| `NotesTests` | Today's page; the text cap; the clipping cap |
| `SitesTests` | Listed from `fromDay`, not before |
| `PaperAnalysisTests` | The first undocumented pair in document then field order; none for an honest set (every honest traveller of the generation dump); a documented category is skipped; directive-only categories never; one pair only |
| `DeskPapersTests` (grown) | The Auto-Feed queue: hand-over order, one at a time, a held paper skipped until it is back on the desk, a busy scanner waits |
| `ComparePairTests` (grown) | R4-009: two records' Born rows are two picks |
| Content validator tests (grown) | `pc.*` rules: unknown `when`, a bad category, an unknown upgrade, a duplicate site id, a missing string key |

The UI classes stay untestable in EditMode (R4-021). Every rule they apply is in the table above, and the Unity job covers the wiring (§11).

## 9. Intent audit (what already existed, tried first)

| New thing | Existing mechanism tried | Verdict |
|---|---|---|
| `WindowStack` | `DraggableWindow` + `OSWindowChrome` (front on header press; minimise = close) | They have no focus, no taskbar and no minimised state. They are replaced, not paralleled (the old classes change into the new ones). |
| `DesktopLayout` | `GridLayoutGroup` (today's grid) | A layout group cannot place freely. `RectClamp` is reused for the clamp. |
| `CaseIndex`, `TextMatch` | `CitizenRegistry.Find` (records only), `FactTable.Get` (exact) | Nothing searches across sources. `Find` retires, so records have one matcher (SE6). |
| `SmartLinks` | `PickKeys`, `EvidencePicks` | The keys are reused as link targets. Only the table (category → book) is new, and it reads `ReferenceBookSO.category`. |
| Steps | none (no checklist exists) | Its events are the existing ones (§3.1), and its ticks never judge (ST2). No new game rule. |
| `AppClipboard` | TMP fields' system-buffer copy/paste | Wrapped, not replaced: the clip keeps its source for Notes, and the canonical for SE5. |
| Pins, recents | the "pin system" planned in `InvestigationUIController.cs:407` | Built as that plan said. |
| `NavHistory` | none | One class serves the panes and the browser. |
| `ShortcutMap`, `DesktopEscapeRule` | Escape polled by five components (R5-005) | The desktop's keys go through one map. The frame's Escape defers to the desktop through the existing stamp pattern (X8). |
| The compare dock | the PC `CompareBar` | Moved, with `CompareController` unchanged. |
| Keyed highlights | `ICompareHighlight` | Kept for the desk and the bubble. `IsPicked` reads `ComparePair`'s own keys, so there is no second compare state. |
| `FormLayout` | `PaperFace` | It grows into `FormLayout`. `FaceRect` is kept, and `PaperFaceTuning` moves into `FormStyleSO`: one engine and one set of knobs for the desk and the PC. |
| `FormView` | `DocumentWindowController`, `PagedRowsWindow` | Replaced (R4-007 goes with them). `TextFlip` is reused for transcript lines; `DocumentRowView` and `RevealClock` retire (TR1). |
| News | `TomorrowPackage` lines | Reused. The archive is new because nothing keeps a past day's lines. |
| History | `FactTable`, `HistoryState`, `places[]` (`moment`, `year`), `history.rules[].name` | All reused; only the page builders are new. |
| Ancestry | premades (`name`, `birthDate`, `place`, `recordNote`) | Reused, plus authored people. |
| Mail | the day plan's rules, the news archive, `CaseVerdict.citationText` | Every text is reused. Only the read ids are new. |
| Account | `WorldState.money`, `ShiftLedger`, `HomeEconomy` | Reused. A per-day record is new, because the ledger is dropped at night. |
| Notes | the placeholder window | New content, saved with the run. |
| `DesktopConfigSO`, `FormStyleSO` | `DeskConfigSO` | `DeskConfigSO` is the physical desk and the PC's power and clone. The desktop's UI layout gets its own SO. The face knobs leave `DeskConfigSO` for `FormStyleSO`, so there is still one place per knob. |
| Serials | `Seeds` salts | A new salt with its distinctness test (house rule). No stream is drawn. |
| Scanner upgrades | the Advanced Scanner (an unread flag), Archive Access (a shop discount), the scan (`DeskPapers`, `DeskScanner`) | The Advanced Scanner becomes the Analysis Scanner; the Auto-Feed reuses the scan unchanged; the marks read the traveller-types spec's `PaperChecks.Contradictions`, the rule its proof uses. No new shop rule. |
| Untranslated search and clips | piece 9's canonical rule | Kept, for speech only: documents no longer have untranslated values (TR1). |

**What this spec overrides:**
- **Piece 10 X16** (the face layout) becomes FO1 and FO10. The face now also has furniture.
- **FEATURES.md:47 and :49** ("every wheel choice except… opens it"; "Every new case closes all open windows") and **`:78`** ("window auto-opens on first find") become WN5 and CM5. The app keeps the view steady.
- **The asset list's retired "agency_logo, the nation seals" row** and **UI_ART_RULES' "no … emblems or seals" on paper faces** become FO8. One Temporal Customs seal, printed by the form, not on the face.
- **The 17-icon decision** ("the 17 tiles get pictures") becomes DK1: six icons. The existing interim glyphs are reused as tab glyphs (§14).
- **Piece 9's papers translation and piece 10's shared written reveal and sightings (X25)** retire: Saleh's translation change (TR1).
- **Archive Access** retires, and the Advanced Scanner changes job: Saleh's Q2 answer (SC1).
- The audit items are taken as §10 says.

Each override is Saleh's own request, "too many… confusing… too static" and "look like forms", or follows from it.

## 10. Keep, refactor, remove

| Code | Fate | Notes and audit items |
|---|---|---|
| `ComparePair`, `PickKeys`, `EvidencePicks`, `DiscrepancyLog`, `CompareEvidence` | **Keep** | `PickKeys.Record(category, recordId)` and `EvidencePicks.ForRecord(…, recordId)` fix R4-009 (the record's number, else its name) |
| `CompareController` | **Keep, extended** | Rewired to the dock. Gains `IsPicked` and `PicksChanged`. Caches the two bar texts (R4-026). |
| `FactTable`, `CitizenRegistry` (minus `Find`), `History*`, `TodaysWorld`, `DocumentRows` (minus `TongueRow` and `HasTongue`), `DisplayText`, `CaseTranslation` (its speech half), `TranslationPresenter` | **Keep** | `TextFlip.Write` takes the own font (R4-024, TR3). |
| `DocumentReveal`, `RevealClock`, `DocumentRowView` (its foreign branch; its plain write folds into `FormView` and `DeskDocument`), the sightings (`Sighted`), `CaseTranslation.Field` and `PapersTranslated`, the four Papers translators | **Remove** (the plan's translation phase) | TR1; the traveller-types spec's §11.1 |
| `PcFrame`, `PcScreenClone`, `MonitorScreen`, `OfficeViewController`, `BoothRules`, `BoothCoordinator` | **Keep** | `OfficeViewController`'s Escape defers to the desktop (§3.5). R5-014 (the clone while the frame is open) stays separate. |
| Desk: `DeskController`, `PaperExaminer`, `DeskPapers`, `PaperClicks`, `PaperLanding` | **Keep** (`DeskController` and `DeskPapers` grow the scanner upgrades: SC3, SC4) | R5-001 and R5-002 are fixed by the audit hotfix slice. |
| `DeskDocument` | **Refactor** | Draws the form (§6.5). |
| `PaperFace` | **Refactor** into `FormLayout` | `FaceRect` stays. `PaperFaceTuning` moves to `FormStyleSO`. |
| `InvestigationUIController` | **Refactor** into the façade and four presenters (RF1, R4-001) | Keeps its public API and the meaning of its reachability predicates (R4-022). The R4-003 unsubscribe goes through a remembered flag. Accept/Deny are wired once (R4-004). |
| The text fallback (`ShowFallback` … `NewButton`, `InvestigationUIController.cs:809-991`) | **Remove** (Q3: "delete it"; the plan's phase 2) | R4-002, FEATURES.md:82 |
| `DocumentWindowController`, `PagedRowsWindow`, `ReferenceBookWindowController`, `TranscriptWindowController`, `CitizenRecordsWindowController` | **Replaced** by `FormView` and the six views | R4-007 is resolved. `Paging` and `PagingTests` go if nothing else calls them. |
| `DraggableWindow`, `OSWindowChrome` | **Refactor** into `WindowDrag`, `DesktopWindow` | Dead members removed (R4-016). Exact maximise (R4-015). One show/hide path (R4-017). |
| `DesktopIcon` | **Replaced** by `DesktopIconView` | The upgrade gate goes (no site is gated, IN2). No `RunManager` reach-in (R4-020). The locked alpha constant goes with it (R4-015). |
| `DesktopShell` | **Refactor** | The app list, Arrange icons, the context menu. |
| `SettingsWindowController` | **Refactor** | SG1. |
| `DayFlowUIController` | **Keep** | `GameManager` records the news archive when the briefing shows (one call). |
| Builder: `BuildDesktopShell`'s app list, `BuildDesktopIcon`, `FitIconLabel`, `BuildBookWindow`, `BuildDocumentWindow`, `BuildWindowShell`, `ApplyTranscriptRowLayout`, `BuildRecordRow`, the case UI in `Build()` (`:226-341`), `documentWindowOrigin/Step` and the `bookWindow*` knobs (R4-005 goes with them) | **Remove or replace** | §5.2 |
| `ThemeRoles.IsDiegetic` | **Refactor** | An explicit list (TH2) |
| `world_source.json` | **Change** | `pc` block; strings (§4.8); flavour tables (TH3); palette map rules (§7) |
| `DocumentTemplateSO` | **Change** | Gains `form`. `DocumentFieldSpec.page` goes (FO4); `CaseFactory` sets `DocumentField.page` and `DocumentInstance.serial`. |
| `Upgrade_ArchiveAccess`, `Effect_Upgrade_ShopDiscount`, `Effect_Upgrade_ScannerBoost` | **Remove** | SC1 |
| `Upgrade_AdvancedScanner` | **Change** | Renamed Analysis Scanner, id kept, 300 cr (SC1); a new `Upgrade_AutoFeedScanner` joins it |

## 11. Build order

The combined plan (`docs/superpowers/plans/2026-09-26-redesign-plan.md`) merges this spec's phases with the traveller-types spec's into one order. Every phase follows the house rules there: a branch from `main`, tests first, the offline gates, a Unity job in the one persistent editor through `Library\ClaudeJobs`, play and screenshots in the art office only (never the old booth), the golden-master diff, `docs/FEATURES.md` in the commit of the behaviour, and OfficeGameplay rebuilt and committed when the builder changed. This spec's phases land as:

| This spec's phase | Plan phase |
|---|---|
| The papers translation retired; `TextFlip`'s font fix (TR1, TR3) | 1 |
| The text fallback deleted (Q3) | 2 |
| P1 Forms: the engine, the style and the desk paper | 4 |
| P1 Forms: the scanned copies | 5 |
| P2 Windows and the dock | 14 |
| The Investigation façade (RF1), behaviour-preserving | 15 |
| P3 The Investigation app (one pane, six views) | 16 |
| P3 The desktop (six icons, TH2, TH3) | 17 |
| P4 Two panes and links | 18 |
| P5 Search | 19 |
| P6 Keys, clipboard, pins | 20 |
| P7 Steps | 21 |
| The scanner upgrades (SC) | 22 |
| P8 Internet | 24 |
| P9 Mail, Account, Notes, Settings | 25 |
| The content import (CT1) | 26 |
| P10 Art hooks | 27 |

The plan carries each phase's Unity checks, changed by the answers where they apply: no flip on any form (TR1), no site gate (IN2), the clerk's account (AC1), search over untranslated speech only (SE5).

## 12. Alignment with the traveller-types spec

Both specs were finalized together with Saleh's answers (2026-09-26); the traveller-types spec's §16 lists the same seam from its side. That spec owns the data, the rules and the generation; this one owns the screens. Where they meet (a change to a row updates both specs in the same commit):

| That spec | This spec |
|---|---|
| **F1, F2: ten Temporal Customs forms (TC-101 … TC-630, TC-416 and TC-417 included)**, 2 to 4 a traveller, one page, at most 6 fields; the passport and permit retired | FO3: each template gets its `form`. The agency printed on every form is **Temporal Customs**, and PC page kinds take TC-9xx numbers (FO9), so there is one numbering. Each form must fit the face (FO10, §6.3), with the row size unchanged. The Documents chips hold up to 6 kinds; a request group shows one chip until its form is handed over (§2.4). |
| **`DocumentTemplateSO.formNumber`** (§3 there) | The one form number. A document `FormSpec` has none of its own (§6.1). |
| **F4: new `ClueCategory` values** (CitizenId, Destination, AccountStatus, TransponderId, TransponderClass, Debt, Credit, Funds, PolicyNo, Employer, Term, Wage, WaiverNo, Incident, DepartureDate, Expiry, Signature) | Mapped in `SmartLinks` (§4.3): the record categories to the traveller's record, Destination to the booked departure or the origin, the directive-only DepartureDate, Expiry and Signature to Rules. The `Signature` field is drawn by the `Signature` block (FO2); only the waiver has one. |
| **F5, I3, I4: every form filled in English; the displaced speak their tongue from day 5 with the key words in English (its §8.1); translators for speech only** | TR1-TR3: forms are plain; SE5 and CP2 apply to transcript lines only. Nothing in the app depends on `fromDay`. |
| **F6: amounts in cr; dates in the 2150 calendar; the desk calendar shows the date** | Memos, the statement and the scanner strip print dates in that format. |
| **F7: dates on the papers (a departure date, a Valid Until)** | Plain fields with links to Rules (§4.3); the Analysis never reads them (SC5). |
| **R1: records as rows (category, label, value) in the groups Records, Forms on file and Travel; the Displacement Registry entry** | The Record Extract (TC-901, §2.5) draws any record's groups. Rows with a category are pickable, keyed `record:{number}:{category}` (R4-009's fix with the record's number). |
| **R3: find a record by name or agency number** | SE6: one matcher, an exact number or name first. |
| **R4: a 2150 citizen's lineage is flavour for the Ancestry site; the famous displaced's biography is the History and Ancestry entry; no card for generated displaced people** | IN5, as written there. |
| **§5.1: the checklists per kind** | ST3: the step sets per `TravellerKind`. Its directive checks tick on `PaperRead` of the named forms, `Requested` or `RecordViewed`; its deviation checks on `Compared` (with `TruthKind.Paper` for paper-against-paper, its L4). Nothing ticks on a result (ST2). |
| **L4 (Saleh's Q3: papers prove, answers hint): paper-against-paper proofs through `PaperChecks.Contradictions`** | CM1: the app needs nothing for the proof; the dock shows what `Prove` returns. The Analysis Scanner reads the same rule (SC4). |
| **H1, H2: the present (2150) in every book from day 1; the Future no longer a destination** | The Reference shows the present's row like any place's (it is in `FactTable`). Chronopedia has an article for the present (IN4). |
| **§10: debt lines in the morning paper (`news.debt`), counts, ledger lines** | IN3: the News site shows them as ordinary lines. There is no second filler list. |
| **D1-D3 (Saleh's Q6): the clerk's own account (773-2840-19), the instalments, the Debt Relief ending** | AC1, §2.13: the Citizen Account app is the clerk's, the same Record Extract as a traveller's without pick keys; its statement has DEBT RELIEF and OWED. |
| **C3 (Saleh's Q9): the Costume Guide grouped by era** | §2.6: the register's era groups (`ReferenceBookSO.groupByEra`). |
| **S1-S3: strandings and their fines** | IN3 (their news lines), AC1 (the fines on the statement). |
| **CS1: row-shaped content for a later spreadsheet import** | CT1: the `pc` tables follow the same row rules. |
| **Their build order** | The combined plan orders both specs (§11). Records become rows in the old Records window first (plan phase 2), and the app's Records view replaces it (plan phase 16). |

Not built for it here, until it asks: Ancestry cards as evidence (a pick kind would be one `EvidencePicks` method), and a transponder as a desk device (their F3: a future phase, by Saleh's Q2 answer).

## 13. Q-items (answered 2026-09-26)

| Q | Question | Saleh's answer | Applied in |
|---|---|---|---|
| Q1 | What does the desktop's Citizen Account open? | The clerk's own account (option a; it follows his answer to the traveller-types spec's Q6). A traveller's account lives in Investigation ▸ Records. | AC1, §2.13; the traveller-types spec's D1-D3 |
| Q2 | What should Archive Access gate? | Removed: "instead have two versions of a scanner one auto scans and one manual scan but auto highlights a contradiction." | §1.13 (SC1-SC6), IN2 (no site gate), §4.10, §10 |
| Q3 | What happens to the text fallback? | Delete it (option a). | §10; the plan's phase 2 |

The instructions that came with the answers are applied as well: the translation change in TR1-TR3, SE5, CP2 and CP3; the content source in CT1.

## 14. Doc changes (in the commits of the plan's phases)

**`docs/FEATURES.md`** ("Fake-OS desktop" is rewritten; line numbers are at `ff3a6e0`):
- `:41` taskbar, `:42` Start menu, `:43` icon grid, `:44` gated icons, `:45` draggable windows, `:46` placeholder apps, `:47` Clue Log, `:48` Directives, `:49` "every new case closes all open windows", `:50` Citizen Records. These become: the six icons (free placement, arrange, saved, double-click); the window manager; the Investigation app (its tabs, panes, steps, search, links, clipboard, pins, keys); the dock; Internet, Mail, Account, Notes, Settings.
- `:70-78`: the reference books and the Deviation Report as tabs; no auto-open.
- `:82`: the fallback line is removed (Q3), in the plan's phase 2.
- `:84-91` (Translation): speech only (TR1), in the plan's phase 1 (the traveller-types spec's §11.1).
- `:13` (Home): the shop's two scanner upgrades (SC1) and four Speech translators.
- `:90`: the papers' flip and the sightings are removed (TR1), in the plan's phase 1.
- `:142`: the flavour labels, 28.
- `:145`: the 720p floors for forms.
- `:154`: what the builder builds.
- Each line is marked "(tested: X)" only for the tests of §8.

**`docs/ART_ASSET_LIST.md`:**
- **§3, desktop icons.** "Desktop icons × 17" becomes **× 6**: `investigation`, `internet`, `mail`, `citizen_account`, `notes`, `settings`. The retired `icon_settings` comes back.
- **App glyphs × 6** (new, 64 × 64): the Investigation app's tab glyphs, reusing today's interim icons where they fit. `directives` → Rules, `scanner` → Report, `citizen_records` → Records, `cluelog` → Transcript, and new `documents` and `reference`.
- **Document-kind glyphs**, one per form kind (ten: `tc101`, `tc230`, `tc310`, `tc415`, `tc416`, `tc417`, `tc520`, `tc610`, `tc620`, `tc630`), for the Documents chips.
- **Scanner upgrades** (SC6): a feeder tray (Auto-Feed) and an analysis lamp (Analysis) for the desk scanner; Tier 2, text-free; code-drawn placeholders until then.
- **Site glyphs × 3** (`site_news`, `site_history`, `site_ancestry`, 64 × 64).
- **UI kit additions:** `tab.png` (128 × 44, 9-slice), `badge.png` (48 × 48), `toast.png` (128 × 64).
- **§4.** The agency seal, the per-kind faces and `paper_agency.png` (§6.6). Reference covers now show on the Reference chips.
- **"Retired".** Update the "agency_logo, the nation seals" and "icon_settings" rows. Add `icon_lexicon`, `icon_dialect` and `icon_material`.
- **Decisions.** Decision 5 ("the 17 tiles get pictures") is superseded by DK1.

**`docs/superpowers/drafts/art/UI_ART_RULES.md`.** The paper-faces paragraph gains: "the Temporal Customs seal is printed by the game from its own file; faces still carry no seal". Rule 2 names the one allowed fictional agency mark.

**`docs/UI_ART_CONTRACT.md`.** Diegetic now includes forms and site pages (`DiegeticForm`, `SiteContent`).

## 15. Risks

| Risk | Mitigation |
|---|---|
| The phases are large. The app alone replaces the case UI. | The plan splits it: the behaviour-preserving façade first (plan phase 15), then the app (16), then the icons (17). The app's play-through is the readability pass's existing job, re-pointed. The façade keeps `GameManager` untouched. |
| Text input on a World Space canvas through an offset camera (search, Records, Notes, site search) | Today's Records field works there. The windows phase's job (plan phase 14) types into a field through the frame before the app builds more. |
| Keyboard in an unfocused editor | Jobs call commands directly. The chord map is offline-tested. One manual pass by Saleh covers real keys (listed in the keys phase's report, plan phase 20). |
| Readability at 720p | The floors are checked in the layout and measured in the audit. Zoom is the player's lever. The held paper keeps piece 10's size (§6.3). |
| Performance of many TMP texts | Forms rebuild only on change. Parts are pooled. There is one mesh per desk paper. The clone renders as today (R5-014 is available if needed). |
| The flavour tables need 4 new labels in each culture language | A content task in the desktop phase (plan phase 17). Until a translation exists the label shows the reading language (`UiStrings` fallback), and the validator lists what is missing. |
| Serialized-reference churn from renamed components | `FormerlySerializedAs` and the builder rewire. OfficeGameplay is rebuilt, and compared by semantic dump. |
| The two specs drift apart after their answers | §12 is the contract between them. A change to one of its rows updates both specs in the same commit. Nothing is built for a seam before the other spec asks for it. |
| The Analysis Scanner makes paper lies too easy | It reads papers against papers only, marks one pair, never names the forgery and never picks (SC5). The balance phase measures it. |
| The key words read too little or too much of untranslated speech | They are data (`translation.keyWords`), tuned later, as Saleh said. |
