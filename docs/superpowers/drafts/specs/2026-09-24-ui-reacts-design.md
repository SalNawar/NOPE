# The PC reacts to history: design (piece 6)

*2026-09-24 · decisions made by Claude under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), open to his review · branch `feat/ui-reacts` (stacked on pieces 2–5: `feat/identity-lies` → `feat/dialog-questions` → `feat/characters` → `feat/history-facts`)*

Saleh: "everything the bg of your pc the font color and style even approve and reject changes color and language everything".

The office PC now belongs to the present that history produced. Piece 5 decides, each night, which country leads the Future. From the next morning the desk takes on that country's culture: the wallpaper, the window and taskbar colours, the fonts, Accept and Deny (colour, not position), the newspaper mastheads and a set of about 30 short labels in that country's language, with a small English gloss on every control that decides a case. The wallet is paid in the Future currency. Evidence never changes: papers, books, records and answers keep their canonical Latin values, so every comparison works exactly as before. Before any country leads, the desk keeps today's neutral XP look, which becomes one theme among nine.

Line numbers refer to `efe385d` on `feat/identity-lies` (piece 2 implemented). Pieces 3, 4 and 5 land in between and move lines; the piece-6 plan re-reads every file before anchoring an edit, and the implemented code wins over this text. Names from piece 3 are its spec's (`scratchpad/specs/2026-09-24-dialog-questions-design.md`, committed at `fbf4905`). Pieces 4 and 5 were drafted in parallel with this spec: names from them are their draft specs' (`scratchpad/specs/2026-09-24-characters-design.md`, `scratchpad/specs/2026-09-24-history-facts-design.md`), within their decision files (`piece4_decisions.md`, `piece5_decisions.md`). §2.2 states the piece-5 contract exactly as piece 5's §2.16 offers it, and the plan's first task fills in the implemented names (§2.2 binding tables).

## 0. Decisions

Claude made these under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), working from the piece-6 code analysis (session scratchpad `piece6_map.json`) and the binding decision file `piece6_decisions.md`. They bind this piece and are open to Saleh's review. U rows are the analysis's decisions D1–D15; Z rows are its completeness check's extra decisions; the last rows are the items earlier pieces moved here. The R rows under the table are the details this spec settles where a decision left them open. The two O rows after them are the places where this spec **departs from a binding decision**; they are flagged for Saleh's review.

| Id | Decision | Rationale |
|---|---|---|
| U1 | **The present culture is piece 5's Future leader country id** (the saved ordered ranking is kept for later blends). With no leader (below piece 5's floor, or before any leader) the desk keeps the neutral look: today's XP colours and English. | One wallpaper, one font and one language need one winner. The same id also selects piece 5's Future place, so the wallet currency and the desk's language never disagree. A neutral start makes the change visible and earned. |
| U2 | **The saved history block is the source of truth; the culture reaches the scene through the existing cue pipeline**: a `culture:<countryId>` cue on `EffectChannel.UI`, read by `TimelineCueReceiver` subclasses that do the visual work. | PR #3 was rejected for forking the cue pipeline (`docs/reviews/2026-08-29-pr3-timeline-ranked-office-layers.md:5-17`). `EffectChannel.UI` exists and nothing uses it (`EffectSO.cs:16`); the receiver's doc names "UI skins" (`TimelineCueReceiver.cs:6`). |
| U3 | **Evidence stays canonical Latin**: document values, names, birth dates, `OriginLabels`, `FactTable` strings and spoken answers. Only chrome, labels and prose templates change. | Four string equalities decide the game: `DiscrepancyLog.ValuesMatch` (`DiscrepancyLog.cs:262-263`), the compare bar (`CompareController.cs:122`), `CitizenRegistry.Find` on typed input, and `BirthDates.TryParse` on English month tokens. |
| U4 | **Flavour tier.** A curated set of 34 keys (Accept/Deny, start, window titles, mastheads, tray labels, INTERCOM, SEARCH, START SHIFT/GO HOME, MATCH/MISMATCH …) is translated into the culture's language, with a small English gloss on the controls that decide a case. Every UI string goes through keys, so a later full translation is data only. | Saleh asked for language change "even approve and reject". Full translation into six languages would make the game unplayable for most players and needs a translator pass. Glosses keep the decision buttons unambiguous. |
| U5 | **Culture content is authored in `world_source.json`** (a culture block per country: language, fonts, palette, wallpaper and poster paths). `Tools > TimeDesk > Generate World` writes the theme and string-table assets; art is found by path, with a warning when it is missing. | The project's content rule: one hand-maintained source and one authoritative generator (FEATURES.md:95; piece 3 X10). JSON text is diffable and reviewable for translations. |
| U6 | **The builder stamps role tags and becomes authoritative for text and buttons**; one applier walks the tags. The theme is applied only at scene load, never mid-comparison. | `Text()` and `MakeButton()` return existing objects untouched (`OfficeSceneUIBuilder.cs:572-576`, `598-606`), against the manifesto's "builders re-apply the layout they own" (`ENGINEERING_MANIFESTO.md:40-42`). `CompareController` restores the colour it saved at selection (`CompareController.cs:99-110`, `146-153`). |
| U7 | **A small custom string lookup** (culture table → reading language → key, with `{n}` and `{n:format}` arguments and a warning on a miss) in `TimeDesk.Visuals`, engine-free and tested. Domain returns data, not sentences. | About 150 keys, switched by history rather than OS locale. No package approval needed (`com.unity.localization` is not installed), and it stays testable under the current test asmdef. |
| U8 | **Non-Latin fonts come from installed OS fonts at runtime** (`TMP_FontAsset.CreateFontAsset`), from ordered per-script candidate lists with separate Chinese and Japanese chains, prewarmed and cached for the session. Greek uses LiberationSans. Fonts are never downloaded or copied into the project. A culture whose labels no installed font can draw falls back to English labels with a warning. | Microsoft fonts report fsType 8, so importing them would redistribute them. `CreateFontAsset(familyName, styleName)` exists in this TMP (`TMP_FontAsset.cs:491-499`). Han unification needs two CJK chains. |
| U9 | **Arabic ships as real right-to-left text through a pure presentation-forms shaper with RTL run handling in `TimeDesk.Visuals`, tested**, subject to an editor spike. No layout mirroring in v1. If the spike fails, Egypt and Iraq get palette, fonts and wallpaper with Latin transliterated labels (§6 step 1). | TMP has no shaper (`TMP_Text.cs:1441-1452` commented out) and no bidi. Two of the eight countries are Arabic-speaking. Mirroring would touch every builder anchor. |
| U10 | **Per-culture Accept/Deny and MATCH/MISMATCH colours, but positions never swap, each decision button keeps a shape glyph (✓ / ✗), and a tested contrast validator runs in the generator and fails loudly** (text ≥ 4.5:1, large text and UI glyphs ≥ 3:1). The hover outline colours belong to the theme and are validated (R7: two rings). | Saleh wants approve and reject to change colour. Fixed positions and glyphs keep muscle memory, so a colour flip (red is approval in China) cannot cause wrong verdicts. The current amber outline is 1.44:1 on the XP window face (computed from the builder's values). |
| U11 | **Surfaces:** the monitor's desktop canvas, the newsletter overlay (briefing and ledger), and the booth's reactive poster through the existing `TimelineReactiveSprite` (with the blank-poster bug fixed). Home only if the role helpers make it cheap; the Title scene is out. | Saleh's words centre on "your pc". The booth poster reuses the existing reaction mechanism. Home and Title have their own non-authoritative builders (`HomeSceneBuilder.cs:193-196`, `TitleSceneBuilder.cs:136-139`). |
| U12 | **A Settings toggle "UI language: follow history / always English"**, a per-player preference kept outside the run save, hosted in the existing empty Settings window. Colours, fonts and wallpaper still follow history. | Accessibility for players who cannot read CJK or Arabic, without losing the spectacle. The Settings stub exists (`OfficeSceneUIBuilder.cs:1290`, `DesktopShell.OpenSettings`). |
| U13 | **Text-free placeholder wallpapers (and posters) per culture are generated from the palette**, like today's `EnsureWallpaper`, and are replaced by real art later. Text-free art is a hard rule. | Unblocks the code now. Baked words in art would defeat the language switch. |
| U14 | **The theme changes only at the day boundary.** Piece 5 already announces a new leader in the morning paper; piece 6 adds no news line. | History already changes only across the nightly reload (`RunManager.cs:176-190`). |
| U15 | **Verification:** Domain and Visuals decision-table tests offline first; then a GUI-mode play smoke per forced culture that reads back colours, fonts, labels and glyph coverage, and saves screenshots to the scratchpad for a by-eye check. | Rendering (CJK glyphs, Arabic joining, overflow) cannot be proven by reading values alone. |
| Z1 | **Pieces 5 and 6 own the ranking and the cue emission** (piece 5 computes and emits; piece 6 consumes). Note for Saleh to tell Marwan: PR #3's resubmission builds on piece 5's ranking and must not add a second one. | One notion of "winning" (PR #3 review, item 1). |
| Z2 | **The theme is applied on scene load** (`SceneManager.sceneLoaded`, which runs after `Awake`/`OnEnable` and before `Start`) by a persistent host created like `InteractionFeedbackBootstrap`, so the briefing never flashes the neutral look. | The `sceneLoaded` host avoids the unordered `Start` race of a per-scene receiver (`TimelineCueReceiver.cs:24-27`, no custom execution order). |
| Z3 | **Themes tint and restyle UI chrome and swap the wallpaper; booth art is never re-tinted** (Codex's delivered booth art stays as it is). The builder stops re-applying hard-coded tints: themed elements take their colours from theme data. | Keeps the art cost bounded and Codex's work intact. |
| Z4 | **Traveller documents, reference books and Citizen Records content are diegetic**: a diegetic role that PC theming skips. | Their values are evidence, and a later art task can skin documents by the traveller's own place (main's untracked per-country document kits) without fighting the PC theme. |
| Z5 | **Old saves get the culture step on Continue**, idempotently. | A save whose history names a leader but lacks the day's culture cue (for example, made between pieces 5 and 6, or with a renamed effect) themes correctly on load. |
| Z6 | **Western digits and the 24-hour clock stay everywhere.** | Digits sit next to canonical values (`BirthDates`, the clock `ShiftClock.Format`, `ShiftClockTests.cs:130-137`); mixing digit systems invites misreads. |
| Z7 | **Endings stay based on attribute totals** (`EndingService`), and the present culture may differ from the ending shown on the Title (Italy's colours with a "Scientific Age" ending). Written down, not reconciled. | Endings are piece 5's area; a culture condition is a later extension. |
| Z8 | **Culture art is found by one path convention**, recorded for the art pipeline: the office convention (a file at a fixed path, referenced by scene objects and generated assets, replaced in place). Piece 4's character art (loaded by key at runtime) is the project's other convention, and one art contract documents both (R11). | Codex's planned Tier-2 "ArtLibrary" loader (main's untracked `docs/ART_ASSET_LIST.md:10,217`) must read the same paths, or two mechanisms fork. |
| Z9 | **Arabic is shaped inside the string lookup**, never through TMP's `ITextPreprocessor`. | The preprocessor field is an interface-typed `[SerializeField]` (`TMP_Text.cs:160`), which Unity does not serialize, so it would be lost on every `Instantiate` clone (inference from Unity serialization). |
| W | **The wallet is relabelled with the present culture's Future currency name**: the Currency fact of the leader's Future place as piece 5 resolves it, `lib.BuildWorldFacts(history).Get(leaderId, lib.FutureEra.id, Currency)` (piece 5 H9, its §2.16 C6). | The evidence currency changes in piece 5; the wallet follows the same resolved value. |
| M | **Book rows changed by history get a "revised" marker** (piece 5 H14), because it is cheap (R14, §2.3, §2.7). | The player sees which reference entries moved without any change to evidence values. |
| P | **Per-traveller report lines in the shift ledger** (piece 2 parked) are **deferred** (§4 F8). | The ledger paper is fixed height and already holds nine lines; twelve more would need paging. Not cheap. |

### Refinements settled by this spec

| Id | Refinement | Rationale |
|---|---|---|
| R1 | **The host is one persistent `CultureThemeService` that is itself a `TimelineCueReceiver`** listening on `EffectChannel.UI`. `CultureThemeBootstrap` creates it (`RuntimeInitializeOnLoadMethod(AfterSceneLoad)`, `DontDestroyOnLoad`, configured before it first enables, exactly like `InteractionFeedbackBootstrap.cs:15-35`). It refreshes on `sceneLoaded`, once right after the bootstrap, and when `GameManager.Start` calls `CultureThemeService.RefreshActive()` right after `RunManager.GetOrCreate()` (`GameManager.cs:86`). Every refresh also refreshes the scene's other `TimelineCueReceiver`s. | Covers both U2 (a receiver subclass does the work) and Z2 (`sceneLoaded`). When the Office scene is played directly, no `RunManager` exists at `sceneLoaded`; the explicit call from `GameManager.Start` re-themes before the briefing shows (`GameManager.cs:171`), in the same frame. Refreshing the other receivers fixes the same race for the booth poster. |
| R2 | **Which strings go through keys.** Every string owned by UI code or by the builder goes through the string layer (about 150 keys, §2.8); English lives only in the English table. **Display words and report sentences that Domain builds today become keys as well, and Domain returns data** (U7): a deviation is reported as a key chosen in Domain (`Discrepancy.ReportKey`, nine templates `deviation.{proof}.{statement}`), with the category word (`category.*`, key from `ClueLabels.Key`) and the canonical values as arguments; piece 4's slot words become `slot.*` (`Looks.SlotKey`). `Discrepancy.Summary`, `ClueLabels.Report` and `Looks.SlotLabel` lose their callers and are removed (§2.3). **Content** strings (asset display names, field labels, archetype roles, rule descriptions, piece-3 interview and dialog lines, effect, trigger and history news lines, endings, upgrades) stay content: English in v1, and a later translation adds per-culture fields to their authoring source, keyed by the ids they already have (asset names, piece-3 stable line ids, piece-5 `history.*` ids). The code-owned strings that stay literal English are listed in O1. **In v1 a culture table may translate only the 34 flavour keys**; the generator and the validator reject anything else. | U4 and U7 as written. Pieces 3 and 4 handed these words to this piece (piece 3 §4: "moving `ClueLabels` into tables"; piece 4 §2.20: the slot labels and "DRESS" "are display text"). The key choice stays in Domain and is tested; the English sentence has one home, the table. The flavour-only rule keeps diegetic surfaces (which keep the project font, Z4) from ever receiving a script they cannot draw. |
| R3 | **Labels live per language, not per country.** `ui.languages[]` holds one table per language (`ar` serves Egypt and Iraq), and a country's culture block names its language. **Palettes are seeds plus one shared role map**: each culture authors 23 named colours; `ui.paletteMap` maps each of the 36 themed roles to a fill seed (with alpha), an ink seed and a contrast class. The neutral theme lists exact overrides, so it reproduces the committed scene's colours (within 1/510 per channel, hex rounding) apart from the intended changes listed in §2.12. | Avoids duplicating the Arabic table, and keeps eight palettes to 23 values each instead of 72. |
| R4 | **One `ThemeTag` component per graphic** (role, fill or ink part, optional label key, the builder's base font style and a text kind) replaces the separate ThemeRole/ThemeLabel of U6. Diegetic content is nine explicit diegetic roles that the applier skips (§2.5): the scanned page, the photo box, the scanner backing with its page footer, document and record rows and their text, book rows, record labels, the record note, piece 4's figure layers (white) and garment highlight overlays (transparent). The builder writes their neutral colour, which is exactly what their owners set today. The window frames around them are themed (O2). | Every builder graphic gets a role, so an untagged graphic is a build error, not a silent gap. A figure layer or a highlight overlay needs a role whose neutral colour is white or transparent, or the builder's re-applied colour would tint the figure or show the overlays (piece 4 §2.9: highlight overlays are (1, 1, 1, 0), tinted only by `CompareController.Fill`). |
| R5 | **Font chains end in a runtime LiberationSans asset** built in memory from the project's own `LiberationSans.ttf` (`TMP_FontAsset.CreateFontAsset(Font)`, `TMP_FontAsset.cs:539-541`); the tracked `LiberationSans SDF` and its tracked dynamic fallback are never chained or written. **Greece uses that runtime LiberationSans (U8)**, which draws Greek; Italy, Britain and Germany may name an OS display face first (Palatino Linotype, Georgia, Segoe UI). A candidate may name an OS font file and face index (for `.ttc` collections), tried through `Font.GetPathsToOSFonts()` and the public path overload (`TMP_FontAsset.cs:512`) before its family name. | The static LiberationSans atlas has 250 characters, so Greek would otherwise go through the tracked dynamic fallback and dirty it in editor play mode (the churn commit `d3a9a49` cleaned up). "Font style" is part of Saleh's ask. The path route removes the family-name matching doubt ("Yu Gothic Medium" versus "Yu Gothic"/"Medium"). |
| R6 | **Shaping happens once, at runtime, in the string lookup's output** for every culture entry of a right-to-left table, static labels and templates alike. **Spaces inside a right-to-left run come out as no-break spaces (U+00A0)**, so TMP never breaks a line inside an Arabic phrase (TMP skips U+00A0 as a break opportunity, `TMP_Text.cs:4741`); a line can still break at the Latin spaces around it. The generator and the validator run the same shaper only to reject text it cannot shape. Culture (flavour) templates take only numbers as arguments, so an already shaped string is never shaped twice. If the spike fails, Egypt and Iraq switch to an `ar-Latn` table (Latin transliterations, left to right); Eastern Arabic numerals are not used either way (Z6 overrides the numerals of the analysis's fallback option). | One shaping point instead of two. The tables stay in logical order, which is what a native reader can review. The shaper emits visual order, so a wrap inside a right-to-left run would put its lines in the wrong order; flavour keys also land in texts that wrap (the newsletter titles and body, the citation slip, the compare bar). |
| R7 | **The hover outline is two rings**: a dark inner ring (`ringDark`, at the settings' outline distance) and a light outer ring (`ringLight`, at twice that distance) drawn by the one `HoverUIOutline` effect. The validator requires the two ring colours to contrast at least `outline²` : 1 (9:1 for the 3:1 minimum) with each other; then one of them reaches 3:1 against **any** colour behind the clickable (the proof is in §2.4, `Contrast.Problems`). Each role has a contrast class: text 4.5, large text 3.0 (TMP size ≥ 24, or ≥ 18 bold), glyph 3.0 (window-control marks such as "X"), hint 3.0 (input placeholder). The neutral rings are `#092E70` and `#FFD76E`. | The surface behind a clickable is often not one colour: the wallpaper art (painted sky, hills and clouds, or final art never seen by the generator), the desk dim over it, piece 4's figure art behind garment buttons. With one outline colour the check would need every pixel behind every clickable; with two rings the guarantee holds everywhere. Today's amber `#FAB833` reaches only 1.44:1 on the XP window face and 2.74:1 on the XP wallpaper blue. |
| R8 | **The ✓ / ✗ glyphs are two rotated bars each**: plain `Image`s with no sprite (a solid quad), taking the button's ink. No texture and no PNG. | LiberationSans has neither U+2713 nor U+2717 in its static atlas, and the tracked fallback must not grow. Bars need no painter, no import settings and no LFS file. |
| R9 | **Home keeps its English UI**; only the wallet noun changes there, at seven sites including the shop prices (§2.8), and the Home money text and price labels shrink instead of wrapping (R21). Home theming is deferred (§4 F1, O1). | Home would need its own role stamping in `HomeSceneBuilder` (non-authoritative, CRLF) and a HomeScene rebuild, while Codex holds uncommitted HomeScene edits on main. Not cheap. The wallet must not say "Credits" or "cr" at Home and "Drachma" in the office. |
| R10 | **The language preference takes effect at the next scene load**, and the Settings window says so. | The theme is applied only at scene load (U6, U14); a mid-shift relabel would leave runtime-written texts in the old language until each controller rewrote them. |
| R11 | **Culture art lives at `Assets/Art/Culture/{countryId}/wallpaper.png` and `poster.png`.** Generate World creates a text-free placeholder only when the file is missing and never overwrites one; final art replaces the file in place, keeping its `.meta`. The neutral wallpaper stays `Assets/Art/Generated/xp_bliss.png`, which the generator now also ensures (the builder stops painting it). The painters are pure code in `TimeDesk.Visuals` (`CulturePlaceholders`, tested); the PNG write and import helpers move out of the builder into a new editor helper, `PlaceholderPng`, which the builder and the generator both call. `docs/UI_ART_CONTRACT.md` states this office convention and points to piece 4's character-art contract for the other one. | This is the convention the builder already uses and Codex already follows ("Swap the PNG later for final art (same path/name)", `OfficeSceneUIBuilder.cs:824`; main's modified `xp_bliss.png`); replacing in place keeps sprite references valid without a scene rebuild. Piece 4 draws character placeholders at runtime instead (its R9) because it has hundreds of layer keys and loads art by key; piece 6 has 16 files, and the booth poster must stay a `TimelineReactiveSprite` cue mapping (R12), which holds Sprite assets: a runtime placeholder would need a second sprite source in that class. |
| R12 | **The booth poster is the existing `TimelineReactiveSprite` listening on `EffectChannel.UI`**, with one builder-written mapping per culture (`culture:<id>` → that culture's poster). The blank-poster fix lives in the class: an unset default keeps the renderer's authored sprite. | The PR #3 review's rule: extend that one class instead of cloning it. One effect has exactly one channel (`EffectSO.cs:67`, `TimelineEffects.cs:129`), so the poster listens on the channel the culture cue uses. |
| R13 | **The wallet follows the culture id of the active cue** (`CultureChoice.Wallet`, §2.4), in three forms: the label ("Credits: 0" in the tray), the noun inside sentences ("+5 credits") and the short form on Home's shop prices ("120 cr"). A culture shows its Future currency value as written in all three; neutral, or a leader whose Future place has no Currency fact, shows the matching key (`wallet.credits`, `wallet.creditsInline`, `wallet.creditsShort`). | The same id drives the theme, so the two never disagree, and the neutral desk keeps today's capitalisation. The currency value is a proper name (evidence text), so it is never re-cased. |
| R14 | **The "revised" marker.** A book row is marked when its resolved value differs from the place's authored value (`History.IsRevised`, Domain, tested; `DiscrepancyLog.ValuesMatch` decides "differs"), for as long as history keeps it changed. Piece 5's `ContentLibrarySO.FillFacts` calls it and marks the row with `FactTable.MarkChanged`; the book window appends the marker to the row's label column, never to the value. This is the meaning piece 5's §2.16 C7 states. | The value is evidence (U3). `FillFacts` is the only code that sees both values (piece 5 H18, R4). A permanent marker tells the player which reference entries differ from the world they started in; a one-day marker would vanish before a player who skipped the paper sees it. |
| R15 | **Compare equality is piece 4's** (its R2: `DiscrepancyLog.ValuesMatch` public, `CompareEvidence.MatchValue`, the private copy deleted). Piece 6 only deletes `CompareController`'s evidence-less `Select(label, value, highlight)` overload, which has no caller at `efe385d` and none in pieces 3–4's specs (the plan re-greps before deleting). | No dead code (MERGE_CRITERIA); the overload would let a later caller bypass typed evidence. |
| R16 | **Builder authority, completely.** `Panel`, `Text`, `MakeButton` and `BuildRowTemplate` re-apply everything they own (geometry, colour from the neutral theme, text from the English table, style, tag) on existing objects. The ten XP constants (including the unused `PanelNavy`, `OfficeSceneUIBuilder.cs:32`) and every inline UI colour literal move to the neutral theme. Where the committed scene has drifted from the builder's literals, §2.12 decides per object which value the neutral theme keeps. The builder logs an error for any graphic it leaves untagged. | Manifesto rule; Law 3; and translated or glossed labels are longer than English. Once the builder is authoritative, a drifted object would change on the next build unless the neutral value is chosen deliberately. |
| R17 | **Numbers are formatted by the string layer, in the invariant culture.** Templates carry .NET format specifiers in their placeholders (`{0:+0.#;-0.#}`, `{0:0}%`), `UiStrings.Format` formats number arguments with them and `CultureInfo.InvariantCulture`, and string arguments pass verbatim. Translations must keep the reading entry's placeholders, specifiers included. | Western digits (Z6) and a decimal point regardless of the player's OS locale (today's interpolations use the thread culture), and today's signs and rounding ("+5 today", "87%") survive the move into tables. |
| R18 | **The verdict line gets a backdrop strip** (`VerdictStrip`, the claim strip's colours), shown only while the line has text. | The line is white text over the wallpaper, across a painted cloud (1.2:1 on the neutral Bliss, 1.3–1.6:1 in the eight cultures, computed); U10 allows no by-eye exemption. |
| R19 | **The desk dim becomes a builder-owned role.** The full-screen translucent `Image` on `InvestigationRoot` (`#0F121A8C` in the committed scene, which today's `Panel(…, null)` leaves untouched) takes the `DeskDim` role. | It is a graphic every investigation control sits on; an untagged graphic is a build error (R4). |
| R20 | **No "apply culture now" cheat.** Piece 5's force-leader cheat already emits today's cue (`HistoryService.ForceLeader` → `RebuildLeaderEffect(world, lib, world.day)`) and keeps the leader every night (its R19). A tester forces a leader, then Skip Day; the automation (§6) forces the leader before it loads the Office. | A mid-shift scene reload restarts the day through `GameManager.Start` (a new ledger and queue while the day's pay and impacts stay in `WorldState`) and would bring the forced leader's Future place in at once. |
| R21 | **Texts that can receive a long currency or the marker shrink instead of wrapping**: the builder gives the tray's money text, the verdict text and the row template's label text no wrapping and auto-size from `labelMinScale × size` to `size`; at Home, `HomeUIController` does the same for its money text and its price labels. | Future currency values reach 26 characters ("Mediterranean lira (eLira)"); the tray money slot is about 190 px wide at 18 pt in a 27 px tray, and a book row is 34 px high. |
| R22 | **The presentation rules the glue applies are pure, tested code in `TimeDesk.Visuals`**: `ThemeRoleId` with `ThemeRoles.IsDiegetic` (the rule that keeps theming off evidence, Z4), `CultureChoice.Language` (culture labels or English, and why), `CultureChoice.Wallet`, `CultureChoice.ComposeStyle`, and `Palette.Resolve`, which rejects a culture's override of a diegetic role. | The house rule: decisions live where the EditMode suite can test them. Only walking scenes and setting engine properties stays in glue (§2.15). |

### Departures from binding decisions (for Saleh's review)

| Id | Departure | Rationale and later cost |
|---|---|---|
| O1 | **U4 and U7 are met for everything except these code-owned strings, which stay literal English in v1:** (a) the lines saved as finished text in `WorldState.tomorrow` (the dominance lines `TimelineService` writes, and the trigger and history lines, which are content); (b) the Home and Title scenes apart from the wallet noun (about 20 runtime literals in `HomeUIController`/`HomeManager`, and the labels `HomeSceneBuilder`/`TitleSceneBuilder` bake); (c) the generated display name "Name (Role)" and its "Traveler" fallback, which `CaseFactory` composes as case data; (d) log messages. | (a) Keying them needs an additive save field (a key and arguments per line) and a new package builder; piece 5 rejected that field for v1 because nothing would read it (its R21). Cost later: that field, and the briefing formats each line through `UiText`. (b) Needs role stamping in two non-authoritative builders and rebuilding scenes Codex is editing on main (F1). Cost later: keying the runtime literals is cheap; the builders are F1. (c) Case generation must not depend on UI tables (a missing table would put a key into the save). Cost later: `CaseInstance` carries given name and role separately and every reader (claim banner, ledger, fallback) composes through a key. (d) Never shown to the player. |
| O2 | **Z4 is met for document, book and record content; the window frames around them are PC chrome and themed** (title bar, window controls, Prev/Next). The scanned page, the scanner backing, its page footer and every row stay diegetic. | The frames are the PC's window manager; an XP-blue title bar on a document inside a Chinese desktop would look broken. Z4's purposes hold: every evidence pixel keeps its neutral look, and a later document kit by the traveller's place (F9) skins the diegetic roles without touching the theme. |

## 1. Behaviour

### 1.1 Whose present the desk shows

- **The present culture is the country that leads the Future** (piece 5). It is fixed each night and saved, so the desk looks the same all day, and the same again after Save + Continue.
- **Before any country leads, the desk is neutral**: today's XP look ("Luna" blue taskbar, green start button, Bliss wallpaper) and English, with the few intended changes of §1.7. On day 1 the desk is always neutral.
- **When a new country takes the lead**, piece 5's morning paper announces it and the desk wears the new culture from that morning. The look never changes during a shift.
- **The present culture and the run's ending are separate.** Endings still come from attribute totals, so a run can end in "Scientific Age" on an Italian-looking desk (Z7).

### 1.2 What changes with the culture

| Surface | What changes |
|---|---|
| Wallpaper | The culture's wallpaper (a text-free placeholder until final art exists). |
| Taskbar, Start button, system tray, Start menu | Colours; the "start" label; the tray's "Day" and "Stability" words. |
| Every desktop window frame (title bar, window controls, body colour, page footer; the document window's dark scanner backing and its footer are diegetic and keep their look) | Colours and fonts; titles of the Directives, Scanner, Records and Settings windows. |
| Desktop icons | Colours; the app icons' labels (Directives, Scanner, Records, Internet, Lexicon, Dialect, Material, Notes, Clue Log). Book icons keep the book names (content). |
| Desk dim | The translucent dim behind the investigation desk: colour. |
| Intercom panel | Colours; the "INTERCOM" title. Request and question buttons keep their English wording (piece-3 content). |
| Accept / Deny | Colours, the word in the culture's language with the English word underneath, and a ✓ or ✗ glyph that never changes. Accept is always on the left. |
| Compare bar | Colours, including the MATCH, MISMATCH and DEVIATION LOGGED colours (China's mismatch is slate, not red); MATCH / MISMATCH in the culture's language with the English word in brackets. |
| Citation slip | Colours; the "TIMELINE DEVIATION NOTICE" title; the Acknowledge button (with gloss). |
| Morning paper and shift ledger | Paper, ink and button colours; the mastheads; the dateline titles; START SHIFT / GO HOME (with gloss); the "TIMELINE NEWS" header. |
| Records app | Frame colours and title; the SEARCH button (with gloss). The record itself is diegetic and never changes. |
| Settings window | New: the UI-language choice (§1.4). |
| Claim strip, verdict line | Colours. The verdict line now sits on a strip in the claim strip's colours while it shows (R18). |
| Hover outline | Two rings in the theme's colours, dark inside and light outside, so one of them always stands out (R7). |
| Fonts | Every themed text uses the culture's font (§1.4). |
| Booth poster | The culture's poster. |
| Wallet | The Future currency's name everywhere money is named: tray, verdict line, citation penalty, shift ledger, and at Home (money, expenses, shop prices, slot machine). Long names shrink to fit (R21). |
| Reference books | A row whose value history changed shows "[revised]" after its place label. |

### 1.3 What never changes

- **Evidence**: every document value, name, birth date, place label (`OriginLabels`), reference-book value, Citizen Record field and spoken answer stays exactly as generated, in Latin script. Comparisons, deviations and the deny gate behave exactly as before.
- **Diegetic content** keeps today's neutral look and font: the scanned document page, its photo box and the dark scanner backing with its page footer, every document and book row, the record's rows, origin and clerk note, the interview transcript rows, and piece 4's Visitor figure and passport photo. Only the window frames around them are themed (O2).
- **Digits and the clock**: western digits and the 24-hour clock everywhere (Z6).
- **Layout**: no element moves or mirrors, including for Arabic. Accept stays left of Deny.
- **The booth**: calendar, stability monitor and lamp, credits till, wall clock and all booth art keep their look. Only the poster reacts.
- **The Title scene**, and the Home scene apart from the currency name.
- **Content text**: document and field names, book names, travel-rule descriptions, interview and dialog lines, news lines and endings stay English in v1 (R2). The deviation report lines and the category words (CAPITAL, DEVICE, DRESS …) move into the English table and stay English in every culture in v1.

### 1.4 Language and fonts

- **34 flavour labels** (§2.12) change to the culture's language. Egypt and Iraq use Arabic, Greece Greek, Italy Italian, China Simplified Chinese, Japan Japanese, Germany German; Britain keeps English (its palette and font still change).
- **Glosses**: ACCEPT, DENY, START SHIFT, GO HOME, SEARCH and Acknowledge show the English word on a second, smaller line; MATCH and MISMATCH show it in brackets; "< Office" shows it underneath. No gloss when the culture's language is English.
- **Everything else stays in English**, including prose templates (tray money, briefing text, citation body, ledger lines, scanner report).
- **Settings → UI language**: "Follow history" (default) or "Always English". The choice is remembered per player (not in the run save), survives New Run, and applies the next time the office opens. Colours, fonts, wallpaper and poster follow history either way.
- **Fonts**: each culture tries installed fonts in order (for example Microsoft YaHei, then SimSun, then the macOS and Linux equivalents for Chinese), then the game's own LiberationSans. Greece uses the game's LiberationSans directly (U8). Fonts are read from the player's system at runtime and never copied into the game. If no installed font can draw the culture's labels, the desk shows English labels in the culture's colours, and a warning names the culture, the missing characters and the fonts tried.
- **Arabic** reads right to left, with joined letters; numbers inside it read left to right. A line never breaks inside an Arabic phrase (R6); keyed labels shrink to fit instead of wrapping.

### 1.5 Readability rules

- Every culture passes the contrast check before it can be generated: text at least 4.5:1 against its background, large text (the Start label, claim banner, citation text, mastheads) and window-control marks at least 3:1, the input placeholder at least 3:1.
- The hover outline is two rings whose colours contrast at least 9:1 with each other, so one ring reaches at least 3:1 against whatever is behind the clickable: a window, the wallpaper art (placeholder or final), the desk dim or a figure. On the neutral desk the rings are dark blue inside and light amber outside (today's single amber is 1.44:1 on the XP window face).
- The verdict line sits on its own strip (R18), so no text is read directly off the wallpaper.
- All nine themes (neutral + 8) pass with the palettes of §2.12 (computed; the generator re-checks them).

### 1.6 Saves, Continue and determinism

- Piece 6 adds nothing to `WorldState`; the save version stays 2. The culture comes from piece 5's saved history block and its saved cue effect.
- **Continue** re-emits the day's culture cue from the saved history (idempotent), so a save that lacks the cue still themes correctly (Z5).
- **No randomness**: the theme is a pure function of the active cue, the content and the player's language preference.
- Case generation is unchanged: the same run and day give the same travellers, papers and answers in every culture.

### 1.7 Intended changes on the neutral desk

The builder becomes authoritative (R16), so each object whose committed look differs from the builder's literals gets a deliberate value (§2.12). What a player can see change on the neutral desk:
- the hover outline becomes the two rings (R7);
- the verdict line gets its strip (R18);
- Accept and Deny get their ✓ / ✗ glyphs (R8), and their labels move right to make room;
- the reference-book window's Prev/Next buttons and the book-shelf button template change from `#E6E6EB` to the XP face `#ECE9D8`, and the citation slip's Acknowledge button from `#F2F2F2` to `#ECE9D8` with a 22 pt label (was 26), like every default button;
- the reference-book window's title is 15 pt like every other window title (was 20);
- keyed labels, the tray money, the verdict and row labels shrink instead of wrapping when their text is long (R21);
- the Settings window gets its language choice (§2.9), and the Scanner window's baked "No deviations documented." sample is replaced by the idle text the controller writes anyway.
Everything else keeps its committed colour, size and text, including Accept `#296B38`, Deny `#752929`, the book rows' faint `#FFFFFF0A`, the desk dim and the citation text at 26 pt.

## 2. Design

### 2.1 Where things live

- **TimeDesk.Domain** (`Assets/Scripts/Domain`, EditMode-tested): no new file. Members added to earlier pieces' files: `CultureCue.Pick` (piece 5's `CultureCue.cs`), `History.IsRevised` (piece 5's `History.cs`), `ClueLabels.Key` replacing `Report` (piece 3's `ClueLabels.cs`), `Looks.SlotKey` replacing `SlotLabel` (piece 4's `Looks.cs`). Changed: `FactTable.cs` (revised marks) and `DiscrepancyLog.cs` (the deviation report as a key and arguments; `Summary` removed).
- **TimeDesk.Visuals** (`Assets/Scripts/Visuals`, `noEngineReferences: true`, `autoReferenced: true`, EditMode-tested): new `Rgba.cs`, `Contrast.cs`, `Palette.cs`, `UiStrings.cs`, `ArabicShaper.cs`, `ThemeRoles.cs`, `CultureChoice.cs`, `CulturePlaceholders.cs`; piece 4's `PixelShapes` is reused. Presentation maths, text and presentation rules, not game rules, so Visuals (the cursor-spec precedent, `2026-09-24-cursor-hover-shift-clock-design.md:22`).
- **Assembly-CSharp**: new folder `Assets/Scripts/UI/Theme/` with `ThemeTag.cs`, `ThemeSO.cs`, `UiStringTableSO.cs`, `CultureUiSettings.cs`, `CultureThemeService.cs`, `CultureThemeBootstrap.cs`, `RuntimeFonts.cs`, `UiText.cs`, `UiLanguagePreference.cs`, `SettingsWindowController.cs`; changes listed in §2.7 and §2.8.
- **Assembly-CSharp-Editor** (`Assets/Editor`, no asmdef): new `PlaceholderPng.cs` (`internal static`, LF: the PNG write and import helpers moved out of the builder, R11); changed `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `OfficeSceneUIBuilder.cs`; piece 4's `SerializedArrays` is reused.
- **Content**: `world_source.json` (`ui` section, `countries[].culture`); generated `Assets/Data/World/Culture/` (9 themes, 7 string tables); placeholder art `Assets/Art/Culture/{id}/` (16 PNGs, created only when missing); `ContentLibrary_Main.asset`; `OfficeScene.unity` (rebuilt).
- **Tests**: `Assets/Tests/EditMode` (references Domain and Visuals only). The offline runner supports `[Test]`, `[TestCase]` and `[SetUp]` only.

Line endings in the working tree at `efe385d`, to preserve:
- **LF**: `OfficeSceneUIBuilder.cs`, `WorldContentGenerator.cs`, `world_source.json`, `HoverHighlighter.cs`, `HoverUIOutline.cs`, `InteractionFeedbackBootstrap.cs`, `InteractionFeedbackSO.cs`, `ReferenceBookWindowController.cs`, `ShiftClockReadouts.cs`, `FactTable.cs`, `FactTableTests.cs`, every Visuals file; new files.
- **CRLF**: `DiscrepancyLog.cs`, `DiscrepancyLogTests.cs`, `TimelineCueReceiver.cs`, `TimelineReactiveSprite.cs`, `ContentLibrarySO.cs`, `RunManager.cs`, `GameManager.cs`, `OfficeUIController.cs`, `DayFlowUIController.cs`, `InvestigationUIController.cs`, `CompareController.cs`, `DocumentWindowController.cs`, `CitizenRecordsWindowController.cs`, `DesktopShell.cs`, `ShiftScoring.cs`, `TravelRuleSO.cs`, `HomeUIController.cs`, `HomeManager.cs`, `DebugPanelController.cs`, `ContentLibraryValidator.cs`, `docs/FEATURES.md`.
- Files pieces 3–5 add keep the endings they were created with (piece 3's new files, piece 4's `Looks.cs` and piece 5's `CultureCue.cs`, `History.cs` and `HistoryService.cs` are new files, so LF unless their plans say otherwise; the plan checks).

### 2.2 The contracts consumed from pieces 5, 4 and 3

Piece 5's decision file says: "expose the resolved present culture = the Future leader country id (plus the ordered ranking) in the saved history block, emitted to the UI via the existing cue pipeline (TimelineCueReceiver, EffectChannel.UI) — piece 6 consumes it. The debug panel gets a 'force leader' cheat for testing." Piece 5's draft spec offers it in its §2.16 ("Piece 6 (UI reacts), matching its §2.2 C1–C7"), and piece 6 relies on exactly this:

| # | Piece 5 provides (its §2.16 unless noted) | Piece 6 uses it for |
|---|---|---|
| C1 | `WorldState.history.leaderId` ("" = neutral: below the floor, or before any leader) and `history.ranking` (saved, highest first). Save version stays 2. | Nothing at runtime: the theme follows the cue (C2), never live scores. Piece 5's inspector already prints both (its §2.11). |
| C2 | While a nation leads, `TimelineEffects.GetCues(world, lib, EffectChannel.UI)` contains `CultureCue.Format(id)`, from the nation's generated leader effect (`NationSO.leaderEffect`: channel UI, permanent, source prefix `history:leader`), re-activated every night and by the force-leader cheat (its R9). The grammar is Domain `CultureCue` (`Prefix`, `Format`, `TryParse`, its R20), written by the generator's `MakeLeaderEffect` (`Format`) and checked by the validator's `CheckFuture` (`TryParse`), so it has one owner. | The theme (`CultureThemeService`) and the booth poster (`TimelineReactiveSprite`). |
| C3 | `HistoryService.RebuildLeaderEffect(WorldState world, ContentLibrarySO lib, int startDay)` in `Assets/Scripts/Timeline/HistoryService.cs`: the idempotent emission step (removes its own entries through `TimelineService.RemoveEffectsFrom`, then activates). Private in piece 5, "made public by piece 6 for its Continue call". | `RunManager.ContinueRun` for today (Z5, §2.7). |
| C4 | `HistoryService.ForceLeader(world, lib, nationId)`: sets the leader, emits today's cue at once through `RebuildLeaderEffect(world, lib, world.day)`, and keeps the leader every night of the session (`DevToolsState.ForcedLeaderId`, its R19). The debug panel's History buttons call it. | The play smoke (§6) and manual testing: force, then Skip Day (R20). |
| C5 | The morning-paper line announcing a leader change (H4, H14), saved as finished English text in `WorldState.tomorrow.newsLines` (its R21). | Nothing is added (U14). Keyed news is O1(a) and F2. |
| C6 | `lib.BuildWorldFacts(world.history).Get(leaderId, lib.FutureEra.id, ClueCategory.Currency)` when a leader exists. | The wallet label (W, R13). |
| C7 | `ContentLibrarySO.FillFacts(FactTable table, IEnumerable<NationEraProfileSO> places, HistoryState history)`: the one private method behind `BuildToday` and `BuildWorldFacts` that turns place facts into rows through `History.Resolve`, and the only code that sees a fact's authored and resolved values together (`BuildFactTable` is gone, its R4). Piece 5 reserves it for piece 6's `MarkChanged` call and adds no marker API. | The "revised" marker (M, R14). |

**Binding table (the plan's first task).** Before any piece-6 edit, the plan reads piece 5's implemented code and records, for C1–C7, the actual type, member and asset names. Piece 6 calls them and re-implements none of them. **One adaptation** inside piece 5's code is allowed, the one piece 5 anticipates: `HistoryService.RebuildLeaderEffect` becomes `public`, and its doc adds "Also called by RunManager.ContinueRun for today, so a save without the day's culture cue themes correctly (piece 6 Z5)." Piece 6 also adds members to piece 5's Domain files (`CultureCue.Pick`, which piece 5's §2.4 leaves to piece 6, and `History.IsRevised`, §2.3) and one call inside `FillFacts` (§2.7).

**Names consumed from pieces 3 and 4** (the plan records their implemented names the same way; the implemented code wins): piece 3's `ClueLabels.Report` (replaced by `Key`, R2), `Discrepancy.source`, `EvidenceKind.Answer`, `CompareController.ShowAlreadyDocumented`, `PagedRowsWindow` and the book's `FillRow`, `TranscriptWindowController`, the `interview.claim` content line (spoken as `case.claim`, its R26) and the Dialect window copy (its R9); piece 4's public `DiscrepancyLog.ValuesMatch` and `CompareEvidence.MatchValue` (its R2), `EvidenceKind.Appearance`, `Looks.SlotLabel` (replaced by `SlotKey`, R2), `TravellerPortraitView`, the builder's `BuildPortraitStack` (layer Images and their `Highlight` overlays), the Visitor window, the PhotoBox without its "PHOTO" label (its §2.12), `PixelShapes.InEllipse` and `SerializedArrays.Set` (its R28).

### 2.3 Domain

**`CultureCue`** (piece 5's `CultureCue.cs`; `Prefix`, `Format` and `TryParse` are piece 5's) gains:

```csharp
/// The culture a list of active UI cues selects: the first parsable culture cue in list order, or null.
/// matches = how many cues parsed (the caller warns when more than one did).
public static string Pick(IReadOnlyList<string> cues, out int matches);
```

The service warns once when `matches > 1` ("two culture cues are active; the first wins — check the effects that emit `culture:` cues").

**`History`** (piece 5's `History.cs`) gains the revised rule (R14):

```csharp
/// True when history gives this place's category a value that differs from its authored value
/// (DiscrepancyLog.ValuesMatch decides "differs"); the reference books mark such rows "revised".
public static bool IsRevised(HistoryState h, string nationId, string eraId, ClueCategory category, string baseValue)
    => !DiscrepancyLog.ValuesMatch(Resolve(h, nationId, eraId, category, baseValue), baseValue);
```

Piece 5's `Resolve` has no day filter (every edit is latched at night for the next day, and facts are only built for that day or later), so `IsRevised` has none either: a row stays marked while the newest edit for it differs from the authored value.

**`FactTable`** (`FactTable.cs`, LF) gains the revised marks (R14):
- `private readonly HashSet<(string nation, string era, ClueCategory category)> _changed`;
- `public bool MarkChanged(string nationId, string eraId, ClueCategory category)`: true when the cell is in the table and is now marked; false (and nothing marked) for a cell that is not in the table or for null ids;
- `public bool IsChanged(string nationId, string eraId, ClueCategory category)`: false for null ids and unmarked cells;
- the class doc (40-47) gains "which values history changed (for the books' revised marker)".

**`DiscrepancyLog.cs`** (CRLF): `Discrepancy` reports itself as data (R2, U7):
- `public static string ReportKeyFor(DiscrepancyProof proof, EvidenceKind statement)`: `"deviation."` + `claimMismatch` / `foreignOrigin` / `recordMismatch` + `"."` + `said` (Answer) / `wears` (Appearance) / `papers` (any other kind). Doc: "The UI string key of a deviation line; the nine English templates live in world_source.json ui.strings." Callers: `ReportKey` and the generator's key check (§2.11).
- `public string ReportKey => ReportKeyFor(provedBy, source);`
- `public string ReportOther => provedBy == DiscrepancyProof.ForeignOrigin ? actualOrigin : expectedValue;` ("The value the statement is held against: the expected or recorded value, or the place the stated value belongs to").
- `Summary` is removed. Its production callers (`InvestigationUIController.cs:127` and `164` at `efe385d`) format through `UiText.Deviation` (§2.6); its nine sentences, in piece 3's and piece 4's final wording, move verbatim into `ui.strings` with `{0}` = the category word, `{1}` = `documentValue` and `{2}` = `ReportOther`.
- `ValuesMatch` stays as piece 4 leaves it (public, with piece 4's and piece 5's doc).

**`ClueLabels`** (piece 3's `ClueLabels.cs`): `Report(ClueCategory)` is replaced by `public static string Key(ClueCategory category) => "category." + category;` ("The UI string key of a category's report word; the words live in world_source.json ui.strings."). The words move verbatim: `category.Geography` "CAPITAL", `category.Politics` "RULER", `category.Technology` "DEVICE", `category.BirthDate` "BIRTH DATE", `category.Culture` "DRESS" (piece 4), every other category its upper-case name. Any caller the plan finds outside the UI (a log or a validator message) uses the enum name.

**`Looks`** (piece 4's `Looks.cs`): `SlotLabel(LookSlot)` is replaced by `public static string SlotKey(LookSlot slot) => "slot." + slot;`, with piece 4's words in `ui.strings` (`slot.Outfit` "Outfit", "Hair", "Facial hair", "Headwear", "Accessory").

### 2.4 Visuals

All static classes in the global namespace, engine-free, argument guards throwing `ArgumentException` like `CursorHotspot`.

**`Rgba`** (`Rgba.cs`): `public readonly struct Rgba { public readonly float R, G, B, A; public Rgba(float r, float g, float b, float a = 1f); public Rgba WithAlpha(float a); public static bool TryParseHex(string hex, out Rgba color); }`. `TryParseHex` accepts `#RRGGBB` and `#RRGGBBAA` (either case); anything else, including null, returns false.

**`Contrast`** (`Contrast.cs`):
- `public static double RelativeLuminance(Rgba c)`: WCAG 2.x (sRGB linearisation, 0.2126/0.7152/0.0722), alpha ignored;
- `public static double Ratio(Rgba a, Rgba b)`: (L1 + 0.05) / (L2 + 0.05), symmetric;
- `public static Rgba Over(Rgba fg, Rgba bg)`: alpha compositing, opaque result;
- `public enum ContrastClass { None, Text, LargeText, Glyph, Hint }`;
- `[Serializable] public sealed class ContrastRules { public float text = 4.5f, largeText = 3f, glyph = 3f, hint = 3f, outline = 3f, minTextFillAlpha = 0.7f; public float Min(ContrastClass c); }` (`None` → 0);
- `public readonly struct ContrastPair { public readonly string Name; public readonly Rgba Ink, Fill; public readonly ContrastClass Class; }`;
- `public static List<string> Problems(IReadOnlyList<ContrastPair> pairs, Rgba ringDark, Rgba ringLight, ContrastRules rules)`: one message per pair below `rules.Min(Class)` ("Role 'DenyButton': ink on fill is 3.9:1, needs 4.5:1"), one per pair whose fill alpha is below `minTextFillAlpha`, and one when `Ratio(ringDark, ringLight) < rules.outline²` ("Hover rings are 7.6:1 apart; they need 9:1 so that one of them reaches 3:1 on any background"). Fills are checked as opaque (the alpha floor keeps that honest); a translucent ink (the input placeholder) is composited over its fill first. The ring rule's proof, in the doc: with ring luminances a < b (each + 0.05) and any background s, if a ≤ s ≤ b then (s/a)·(b/s) = b/a ≥ o², so one factor is ≥ o; if s < a or s > b, the farther ring alone is ≥ b/a ≥ o² ≥ o.

**`Palette`** (`Palette.cs`) resolves a culture's seeds through the shared role map (R3):
- `[Serializable] public sealed class PaletteRule { public string role; public string fill; public float alpha = 1f; public string ink; public ContrastClass textClass; }` (the map entry; `fill`/`ink` are seed names, either may be empty);
- `[Serializable] public sealed class PaletteOverride { public string role; public string fill; public string ink; }` (hex, either may be empty);
- `public sealed class ResolvedRole { public ThemeRoleId Role; public Rgba? Fill; public Rgba? Ink; public ContrastClass TextClass; }`;
- `public static List<ResolvedRole> Resolve(IReadOnlyList<PaletteRule> map, IReadOnlyDictionary<string, Rgba> baseSeeds, IReadOnlyDictionary<string, Rgba> seeds, IReadOnlyList<PaletteOverride> overrides, bool allowDiegetic, List<string> problems)`: role names parse as `ThemeRoleId` (an unknown one adds a problem); a map rule for a diegetic role adds a problem (diegetic roles have no seeds); an override of a diegetic role adds a problem unless `allowDiegetic` (true only for the neutral theme, Z4); a seed missing from `seeds` falls back to `baseSeeds` (the neutral theme's); an unknown seed name or unparsable hex adds a problem; the rule's alpha multiplies the fill; an override replaces fill and/or ink (its hex alpha is used as written);
- `public static List<ContrastPair> Pairs(IReadOnlyList<ResolvedRole> roles)`: one pair per role with both colours and a class other than `None`.

**`UiStrings`** (`UiStrings.cs`) is the lookup of U7:
- `public enum GlossStyle { None, Below, Inline }`, `public enum StringTier { Full, Flavour }`;
- `[Serializable] public sealed class UiStringEntry { public string key; public string text; public GlossStyle gloss; public StringTier tier; }` (`gloss` and `tier` are read from the reading table only);
- constructor `UiStrings(IReadOnlyList<UiStringEntry> reading, IReadOnlyList<UiStringEntry> culture, bool cultureRightToLeft, int glossPercent)`; a null `culture` means "reading language only";
- `public string Get(string key)`: the culture entry, else the reading entry, else the key itself (recorded in `MissingKeys`). A culture entry of a right-to-left table goes through `ArabicShaper.ToVisual`. When a culture entry is used and the reading entry's gloss is `Below`, the result is `native + "\n<size=" + glossPercent + "%><noparse>" + reading + "</noparse></size>"` (the `noparse` keeps a reading text such as "< Office" from being read as a rich-text tag); `Inline` gives `native + " (" + reading + ")"`;
- `public string Format(string key, params object[] args)`: fills each `{n}` or `{n:format}` of the chosen template first (an `IFormattable` argument through `ToString(format, CultureInfo.InvariantCulture)`, `format` null for `{n}`; a string verbatim, its format ignored; null as empty), then shapes (culture, right to left), then glosses. Strings are inserted verbatim: no trimming, casing or shaping of a Latin argument beyond the run reordering of `ArabicShaper` (which keeps a Latin run's characters in order);
- `public IReadOnlyCollection<string> MissingKeys { get; }` (distinct, in first-miss order);
- `public static IReadOnlyList<string> Placeholders(string template)`: the distinct placeholder tokens in first-appearance order ("0", "1:+0.#;-0.#");
- `public static List<string> TableProblems(IReadOnlyList<UiStringEntry> reading, IReadOnlyList<UiStringEntry> culture, bool rightToLeft)`: duplicate or blank keys, an unmatched brace, culture keys missing from the reading table, culture keys that are not `Flavour` (R2), placeholder token sets (format specifiers included) that differ from the reading entry, and (right to left) text `ArabicShaper.CanShape` rejects. The generator and the validator both call it, so the table rules have one home.

**`ArabicShaper`** (`ArabicShaper.cs`), the reference model used to write §5's golden cases is `scratchpad/p6/shaper.py`:
- `public static string ToVisual(string logical)`: (1) contextual shaping of U+0621–U+063A and U+0641–U+064A into Presentation Forms-B (isolated/final/initial/medial by the dual- or right-joining class of each letter; a letter joins the previous one when that one is dual-joining), with the four lam-alef ligatures U+FEF5–U+FEFC; (2) direction classes: Arabic letters and presentation forms are R, ASCII letters and digits are L, the rest neutral; `%` after a digit, `+`/`-` before a digit, and `. , :` between two digits join the digit run; a neutral between two L runs is L, otherwise R (an RTL paragraph); (3) the runs are emitted in reverse order, R runs reversed character by character with `() [] {} <> «»` mirrored and every U+0020 turned into U+00A0 (R6), L runs kept in order. The result is rendered left to right by TMP, without `isRightToLeftText`. Text with no Arabic letter is returned unchanged.
- `public static bool CanShape(string text, out string unsupported)`: false when the text holds a character of the Arabic block (U+0600–U+06FF) that the tables do not cover (for example U+0679); `unsupported` lists them.

**`ThemeRoles`** (`ThemeRoles.cs`): `public enum ThemeRoleId { … }` (the §2.5 table, serialized by `ThemeTag`; append only once shipped) and `public static class ThemeRoles { public static bool IsDiegetic(ThemeRoleId role); }` (true for roles 36–44). The rule that keeps theming off evidence (Z4) is thereby tested.

**`CultureChoice`** (`CultureChoice.cs`), the choices the service makes (R22):
- `public enum LabelLanguage { Culture, SameAsReading, NoTable, EnglishBySetting, EnglishNoFont }`;
- `public static LabelLanguage Language(string themeLanguage, string readingLanguage, bool hasCultureTable, bool alwaysEnglish, bool fontCovers)`: `SameAsReading` when the two languages are equal (ordinal), else `NoTable`, else `EnglishBySetting`, else `EnglishNoFont`, else `Culture`, checked in that order; only `Culture` gives the service a culture table;
- `public static string Wallet(string cultureId, string futureCurrency, string fallback)`: `futureCurrency` when `cultureId` and `futureCurrency` are both non-blank, else `fallback` (R13);
- `public static int ComposeStyle(int baseStyle, int italicFlag, bool stripItalic, int add)`: `(stripItalic ? baseStyle & ~italicFlag : baseStyle) | add` (TMP's `FontStyles` as int flags; the caller passes `FontStyles.Italic`).

**`CulturePlaceholders`** (`CulturePlaceholders.cs`), the text-free placeholder painters (R11, U13), RGBA32 bytes with row 0 at the bottom:
- `public static byte[] Wallpaper(int width, int height, Rgba skyTop, Rgba skyBottom, Rgba groundLow, Rgba groundHigh, Rgba cloud)`: the Bliss painter moved from `OfficeSceneUIBuilder.EnsureWallpaper`/`Clouds`/`Blob` (757-818) with `System.Math` in place of `Mathf`: the same hill curve, gradients and three cloud blobs, so the neutral colours reproduce `xp_bliss.png`;
- `public static byte[] Poster(int width, int height, Rgba field, Rgba disc, Rgba border, int borderPx)`: a field, a centred disc through piece 4's `PixelShapes.InEllipse`, and a border.

### 2.5 Assembly-CSharp: theme data

**`ThemeRoleId`** (Visuals `ThemeRoles.cs`, §2.4), a serialized enum. Every graphic the builder makes carries one; the neutral values are the committed scene's colours (hex of the builder's floats where the two agree; §2.12 decides where they differ):

| # | Role | Used by (builder line at `efe385d`) | Neutral fill / ink |
|---|---|---|---|
| 0 | Desktop | `Desktop` wallpaper image (629) | `#3B73BD` (shown only without a wallpaper sprite) |
| 1 | VerdictStrip | new `VerdictStrip` image behind `VerdictText`, and `VerdictText` (80) (R18) | `#0F2E6BCC` / `#FFFFFF` |
| 2 | Taskbar | `Taskbar` (706) | `#2157DB` |
| 3 | TaskbarGloss | `TaskbarGloss`, `StartGloss` (707, 710) | `#FFFFFF2E` |
| 4 | StartButton | `StartButton` + label (709, 711) | `#3D993B` / `#FFFFFF` |
| 5 | Tray | `Tray` + its four texts (716-720) | `#1A52C7` / `#FFFFFF` |
| 6 | WindowBody | window bodies and body texts (1330, 1343, 363), `PageText` of book windows (412), Records status (175), piece 4's Visitor window body and hint | `#ECE9D8` / `#1A1714` |
| 7 | TitleBar | window headers and titles (391, 397, 1332, 1340) | `#2157DB` / `#FFFFFF` |
| 8 | TitleGloss | header gloss (392) | `#FFFFFF24` |
| 9 | Button | default `MakeButton` face: min/max (741-742), prev/next (411, 413), Acknowledge (85), book-shelf template (128), the Settings window's two buttons | `#ECE9D8` / `#000000` |
| 10 | CloseButton | `CloseBtn` (743) | `#DB402E` / `#FFFFFF` |
| 11 | AcceptButton | `AcceptButton` + label + glyph bars (213) | `#296B38` / `#FFFFFF` |
| 12 | DenyButton | `DenyButton` + label + glyph bars (214) | `#752929` / `#FFFFFF` |
| 13 | ActionButton | intercom `ActionButtonTemplate` (158) | `#29476B` / `#FFFFFF` |
| 14 | SearchButton | Records `SearchButton` (180) | `#264D80` / `#FFFFFF` |
| 15 | DesktopIcon | app icons (1391) | `#334D73D9` / `#FFFFFF` |
| 16 | BackButton | `BackToOfficeButton` (1251) | `#334D80F2` / `#FFFFFF` |
| 17 | Panel | `IntercomPanel` (153); the runtime fallback panel | `#121A29EB` / `#FFFFFF` |
| 18 | PanelTitle | intercom title (154) | ink `#B3D9FF` |
| 19 | ClaimStrip | `ClaimStrip`, `ClaimBanner` (120-121) | `#0F2E6BCC` / `#FFFFFF` |
| 20 | Alert | `CitationPanel`, `CitationText` (83-84) | `#D93326F5` / `#FFFFFF` |
| 21 | StickyNote | Directives window body and text (135-139) | `#FFF599F7` / `#1A1714` |
| 22 | CompareBar | `CompareBar`, `CompareText` (205-206) | `#FFFFE0` / `#1A1714` |
| 23 | CompareMatch | `CompareController.matchColor` (283) | ink `#0D731F` |
| 24 | CompareMismatch | `CompareController.mismatchColor` (284), also the DEVIATION LOGGED line (`ShowDeviation`) | ink `#B81A14` |
| 25 | CompareNeutral | `CompareController.neutralColor` (285) | ink `#2E260D` |
| 26 | SelectionHighlight | `CompareController.highlightColor` (`CompareController.cs:21`, never written by the builder today) | `#FFEB59B3` |
| 27 | StartMenu | `StartMenu` (1295) | `#1A1F2EF7` |
| 28 | MenuEntry | `SettingsEntry` (1297) | `#334059` / `#FFFFFF` |
| 29 | PowerEntry | `PowerEntry` (1299) | `#803333` / `#FFFFFF` |
| 30 | NewsletterBorder | newsletter panel and rule (689, 694) | `#1A1714` |
| 31 | Newsletter | newsletter paper, masthead, title, body (690-697) | `#ECE9D8` / `#1A1714` |
| 32 | NewsletterButton | START SHIFT / GO HOME (698) | `#292621` / `#FFFFFF` |
| 33 | DeskDim | `InvestigationRoot` image (116: `Panel(…, null)` today, so only the committed scene holds it, R19) | `#0F121A8C` |
| 34 | InputField | Records `SearchInput` box and text (1359, 1365) | `#FFFFFF` / `#1A1714` |
| 35 | InputPlaceholder | input placeholder (1363) | ink `#737373CC` |
| 36 | DiegeticPaper | `ScanPage` (340) | `#F7F5EB` (never themed) |
| 37 | DiegeticPhoto | the `PhotoBox` frame (342; piece 4 removes its "PHOTO" label, 343) | `#8C8F94` (never themed) |
| 38 | DiegeticRow | the document window's `RowTemplate` + texts (427, 434-435), record rows and values (1379, 1384), `OriginText` (183) | `#FFFFFFB3` / `#1A1714` (never themed) |
| 39 | DiegeticLabel | record row labels (1383) | ink `#595240` (never themed) |
| 40 | DiegeticNote | record `NoteText` (184) | ink `#594D33` (never themed) |
| 41 | DiegeticBacking | the document window's body (the dark scanner backing) and its `PageText` footer (337, 346) | `#21242B` / `#D9DBE0` (never themed) |
| 42 | DiegeticBookRow | the book window's `RowTemplate` + texts (427, 434-435, built through `BuildBookWindow`) | `#FFFFFF0A` / `#1A1714` (never themed) |
| 43 | DiegeticArt | piece 4's portrait layer Images (Visitor window and passport photo) | `#FFFFFF` (never themed) |
| 44 | DiegeticOverlay | piece 4's garment `Highlight` overlays (tinted only by `CompareController.Fill`) | `#FFFFFF00` (never themed) |

Roles 36–44 are diegetic: `ThemeRoles.IsDiegetic(role)` (Visuals, tested) is true for them; the applier skips them, cultures may not override them (`Palette.Resolve`), and their only source is the neutral theme, which the builder reads. Graphics that pieces 3 and 4 add get roles by the same rule: window frames `WindowBody`/`TitleBar`/`Button` (O2), their content (transcript rows, the figure layers and highlight overlays, the photo) diegetic.

**`ThemeTag`** (`ThemeTag.cs`), one per graphic:

```csharp
public enum ThemePart { Fill, Ink }                 // Image: which colour it takes (a glyph bar takes Ink); TMP: always ink
public enum ThemeTextKind { Body, Heading, Button } // which of the theme's style additions apply

[DisallowMultipleComponent]
public sealed class ThemeTag : MonoBehaviour
{
    [SerializeField] private ThemeRoleId role;
    [SerializeField] private ThemePart part;
    [SerializeField] private string labelKey;          // TMP only; empty = text written at runtime by a controller
    [SerializeField] private FontStyles baseStyle;     // TMP only; the builder's style
    [SerializeField] private ThemeTextKind textKind;   // TMP only
    public ThemeRoleId Role { get; } public ThemePart Part { get; } public string LabelKey { get; }
    public FontStyles BaseStyle { get; } public ThemeTextKind TextKind { get; }
    /// Sets the tag. Callers: the builder, and UI built at runtime (the investigation fallback, §2.8), which then calls CultureThemeService.ApplyTo.
    public void Configure(ThemeRoleId role, ThemePart part, string labelKey, FontStyles baseStyle, ThemeTextKind kind);
}
```

The tag carries no surface and no outline: the hover rings are the theme's (R7), read by `HoverHighlighter` (§2.7).

**`ThemeSO`** (`ThemeSO.cs`), generated only, never mutated at runtime:

```csharp
[CreateAssetMenu(fileName = "Theme_", menuName = "TimeDesk/UI/Theme", order = 20)]
public sealed class ThemeSO : ScriptableObject
{
    public string cultureId;                 // "neutral" or a NationSO.id
    public string displayName;
    public string language;                  // a UiStringTableSO.language
    public bool runtimeFont;                 // false = keep the project's default TMP font (neutral only)
    public List<FontCandidate> fonts = new(); // tried in order; the runtime LiberationSans asset is always the last resort
    public FontStyles headingAdd, buttonAdd;  // added to the builder's style for those text kinds
    public bool stripItalic;                 // CJK and Arabic: drop synthetic italics
    public List<PaletteEntry> palette = new(); // resolved by the generator, one per ThemeRoleId in use
    public Color ringDark, ringLight;        // the hover rings (R7)
    public Sprite wallpaper;
    public Sprite boothPoster;               // null for neutral (the poster's own sprite is the default)
    public PaletteEntry Get(ThemeRoleId role); // cached lookup; null when absent
}

[Serializable] public sealed class FontCandidate { public string file; public int face; public string family; public string style = "Regular"; }
[Serializable] public sealed class PaletteEntry { public ThemeRoleId role; public Color fill; public Color ink; public ContrastClass textClass; } // the class lets the validator re-check a stored theme
```

**`UiStringTableSO`** (`UiStringTableSO.cs`), generated only: `public string language; public bool rightToLeft; public List<UiStringEntry> entries = new();`.

**`CultureUiSettings`** (`CultureUiSettings.cs`), `[Serializable]`, held by the content library: `public string readingLanguage = "en"; [Range(30, 100)] public int glossPercent = 60; [Range(0.3f, 1f)] public float labelMinScale = 0.55f; public ContrastRules contrast = new(); public Font latinFallbackFont;` (the project's `Assets/TextMesh Pro/Fonts/LiberationSans.ttf`, whose import has "Include Font Data").

**`ContentLibrarySO`** (CRLF) gains, under `[Header("Culture (piece 6)")]`, all written by Generate World:
- `[SerializeField] private CultureUiSettings cultureUi = new();`, `[SerializeField] private ThemeSO neutralTheme;`, `[SerializeField] private ThemeSO[] themes;` (library nation order), `[SerializeField] private UiStringTableSO[] stringTables;`;
- `public CultureUiSettings CultureUi`, `public ThemeSO NeutralTheme`, `public IReadOnlyList<ThemeSO> Themes`, `public IReadOnlyList<UiStringTableSO> StringTables` (empty when null);
- `public ThemeSO GetThemeByCultureId(string id)` and `public UiStringTableSO GetStringTable(string language)`: cached in `EnsureLookups` beside `GetEraById` (`ContentLibrarySO.cs:223-229`, `292`), cleared in `OnEnable`; null for unknown or blank ids.

### 2.6 Runtime

**`CultureThemeBootstrap`** (`CultureThemeBootstrap.cs`), the `InteractionFeedbackBootstrap` pattern:
- `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void CreateService()`: returns when a `CultureThemeService` exists; loads `RunConfig` from Resources; warns and returns when it has no content library or no neutral theme ("… run Tools > TimeDesk > Generate World"); otherwise creates an inactive `CultureTheme` host, adds the service, calls `Configure(library)`, marks it `DontDestroyOnLoad`, activates it and calls `Refresh()` once (the first scene's `sceneLoaded` has already fired).

**`CultureThemeService : TimelineCueReceiver`** (`CultureThemeService.cs`):

```csharp
public sealed class CultureThemeService : TimelineCueReceiver
{
    public static CultureThemeService Instance { get; private set; }
    public ContentLibrarySO Library { get; }   // the configured library (UiText reads its CultureUi)
    public ThemeSO ActiveTheme { get; }        // never null once configured (neutral fallback)
    public string ActiveCultureId { get; }     // the cue's culture id, or null (neutral)
    public UiStrings Strings { get; }          // reading-only unless the culture's labels apply
    public LabelLanguage Language { get; }     // CultureChoice.Language's answer (inspector)
    public string FutureCurrency { get; }      // C6 value for the active culture, or null
    public string FontName { get; }            // inspector: the resolved font, or "default"
    public void Configure(ContentLibrarySO library);        // before the host first enables
    public static void RefreshActive();                      // Instance?.Refresh(); null-safe
    public void ApplyTo(GameObject root);                    // runtime-built UI (the investigation fallback)
    protected override EffectChannel ListenChannel => EffectChannel.UI;
    protected override void Start() { }                      // the bootstrap and sceneLoaded refresh instead
    protected override void OnCuesChanged(IReadOnlyList<string> cues);
}
```

- `OnEnable` subscribes `SceneManager.sceneLoaded`: the handler remembers the loaded scene as the one to theme and calls `Refresh()` (the other callers theme the active scene); `OnDisable` unsubscribes; `OnDestroy` disposes `RuntimeFonts` and clears `Instance`.
- **`OnCuesChanged(cues)`**, the whole reaction:
  1. `id = CultureCue.Pick(cues, out int n)` (warn once when `n > 1`).
  2. `theme = id != null ? library.GetThemeByCultureId(id) : null`; an id without a theme warns ("No theme for culture '{id}'; the desk stays neutral. Run Tools > TimeDesk > Generate World.") and uses the neutral theme (and `ActiveCultureId` becomes null).
  3. Fonts: `RuntimeFonts.Resolve(theme, sample)` where `sample` is every entry of the culture table (shaped when right to left), the reading texts of glossed keys, and `0123456789:%+-.() ` plus U+00A0.
  4. Strings: `Language = CultureChoice.Language(theme.language, readingLanguage, cultureTable != null, UiLanguagePreference.AlwaysEnglish, covers)`; `Strings = new UiStrings(reading, Language == Culture ? cultureTable.entries : null, rtl, glossPercent)`. `EnglishNoFont` logs one warning naming the culture, the missing code points and the candidates tried, and the text font falls back to the project default.
  5. Currency: `FutureCurrency` = C6's value for `ActiveCultureId`, from `RunManager.Instance.World.history` (null when neutral or when no run exists yet; a missing Currency fact warns once). The wallet word itself is `UiText.Currency` (below), which asks `CultureChoice.Wallet`.
  6. Apply to the target scene (below), then call `Refresh()` on every other `TimelineCueReceiver` in that scene (`FindObjectsByType<TimelineCueReceiver>(FindObjectsInactive.Include, FindObjectsSortMode.None)`, skipping itself).
- **Apply** walks the target scene's `GetRootGameObjects()` → `GetComponentsInChildren<ThemeTag>(true)` (inactive templates and the inactive desktop canvas included):
  - diegetic roles (`ThemeRoles.IsDiegetic`): skipped;
  - `Image`: `color = part == Fill ? entry.fill : entry.ink`; the `Desktop` role takes `theme.wallpaper` as sprite with white colour when set, otherwise no sprite and the fill;
  - `TMP_Text`: `color = entry.ink`; `font` = the resolved text font (neutral: `TMP_Settings.defaultFontAsset`); `fontStyle = (FontStyles)CultureChoice.ComposeStyle((int)baseStyle, (int)FontStyles.Italic, theme.stripItalic, (int)(kind == Heading ? headingAdd : kind == Button ? buttonAdd : 0))`; when `labelKey` is set, `text = Strings.Get(labelKey)`;
  - every `CompareController` in the scene gets `ApplyTheme(match, mismatch, neutral, highlight)` from roles 23–26;
  - no outline work: `HoverHighlighter` reads the theme's rings when a hover starts (§2.7);
  - no per-frame work; no ScriptableObject is written.
- **`ApplyTo(root)`** runs the same walk under one root with the current theme (the fallback panel is built after scene load).
- The service never refreshes on its own during a shift. Its callers are the bootstrap, `sceneLoaded` and `GameManager.Start`.

**`RuntimeFonts`** (`RuntimeFonts.cs`, plain class owned by the service, `IDisposable`):
- `public RuntimeFonts(Font latinSource)`;
- `public (TMP_FontAsset asset, string name, bool covers, string missing) Resolve(ThemeSO theme, string sample)`, cached per theme id:
  1. `!theme.runtimeFont` → `(TMP_Settings.defaultFontAsset, "default", true, "")`;
  2. the Latin fallback, created once: `TMP_FontAsset.CreateFontAsset(latinSource)` (in memory, Dynamic; a null result warns);
  3. each candidate in order: with `file`, the first path of `Font.GetPathsToOSFonts()` (read once) whose file name matches case-insensitively, through `TMP_FontAsset.CreateFontAsset(path, face, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024)`; else with `family`, `TMP_FontAsset.CreateFontAsset(family, style)`; the first non-null asset wins;
  4. none (Greece has no candidates, R5) → the Latin fallback itself;
  5. an OS asset gets `fallbackFontAssetTable = { latin }`;
  6. `covers = asset.HasCharacters(sample, out uint[] missing, searchFallbacks: true, tryAddCharacter: true)`, which also prewarms the atlas.
- Created assets are never saved, never added to `TMP_Settings`, and never chained to the tracked LiberationSans assets. `Dispose()` destroys them with their atlas textures and materials.

**`UiText`** (`UiText.cs`), the facade controllers call:
- `public static string Get(string key)` and `public static string Format(string key, params object[] args)` (`UiStrings.Format`: numbers through the template's format specifiers in the invariant culture, strings verbatim, R17);
- `public enum WalletForm { Label, Inline, Short }` and `public static string Currency(WalletForm form)`: `CultureChoice.Wallet(service?.ActiveCultureId, service?.FutureCurrency, Get(form == Label ? "wallet.credits" : form == Inline ? "wallet.creditsInline" : "wallet.creditsShort"))` (R13);
- `public static string Category(ClueCategory category)` = `Get(ClueLabels.Key(category))`, and `public static string Deviation(Discrepancy d)` = `Format(d.ReportKey, Category(d.category), d.documentValue, d.ReportOther)` (R2);
- `public static void FitLabel(TMP_Text text)`: no wrapping, auto-size from `labelMinScale × fontSize` to `fontSize`, the runtime twin of the builder's label policy for text built outside the builder (Home, R21); with no library it leaves the text as it is;
- they use `CultureThemeService.Instance.Strings` when the service exists; otherwise a reading-only `UiStrings` built once from `RunConfig` → content library → reading table (so edit-mode automation gets English), and, with no table at all, the key;
- each missing key is logged once: "[UiText] No UI string '{key}' in the '{language}' table; add it to world_source.json ui.strings and run Generate World."

**`UiLanguagePreference`** (`UiLanguagePreference.cs`): `public static bool AlwaysEnglish { get; set; }` over `PlayerPrefs` key `"TimeDesk.UiLanguage"` (`"history"` default, `"english"`). PlayerPrefs is new in this project; it is the per-player store U12 asks for, and nothing else fits (the run save is per run and deleted by New Run, `RunManager.cs:87`).

### 2.7 Existing components extended

- **`TimelineCueReceiver`** (CRLF): `protected virtual EffectChannel ListenChannel => channel;`; `Refresh` (42) and the `Channel` property (19) use it. Doc: a subclass created at runtime (the culture service) overrides the channel.
- **`TimelineReactiveSprite`** (CRLF): a new `Awake` resolves `target` (as `Reset` does, 35-38) and, when `defaultSprite` is null, keeps the renderer's authored sprite as the default (fixes the blank poster, `OfficeScene.unity:21189-21190`). Docs: the mapping cue is "broadcast on this receiver's channel" (19), and the class doc says the booth poster listens on `UI` for `culture:<id>` cues.
- **`HoverUIOutline`** (LF): gains `public bool twoRings; public Color outerColor;` and overrides `ModifyMesh`: the base `Outline` pass draws the inner ring (`effectColor` at `effectDistance`); when `twoRings`, a second pass with the same `Shadow` helpers draws the outer ring (`outerColor` at twice the distance) beneath it. Class doc: "… two rings on themed UI (one of them always contrasts, piece 6 R7)".
- **`HoverHighlighter.SetHighlighted`** (LF, 205): when the host carries a `ThemeTag` and `CultureThemeService.Instance` exists, `effectColor = theme.ringDark`, `outerColor = theme.ringLight`, `twoRings = true`; otherwise today's `settings.uiOutlineColor` and one ring (only when the highlight changes, not per frame). `InteractionFeedbackSO.uiOutlineColor` (35) keeps its value; its doc becomes "Outline colour for desktop UI that carries no theme tag (Title and Home scenes); themed UI uses its theme's two rings."
- **`CompareController`** (CRLF):
  - `public void ApplyTheme(Color match, Color mismatch, Color neutral, Color highlight)` sets the four serialized colours (21-30);
  - the evidence-less `Select(label, value, highlight)` overload (57-59) is removed (R15); equality stays piece 4's `DiscrepancyLog.ValuesMatch` over `CompareEvidence.MatchValue`;
  - strings through keys (§2.8).
- **`GameManager.Start`** (CRLF): right after `RunManager run = RunManager.GetOrCreate();` (86), `CultureThemeService.RefreshActive();`, so the culture (and every receiver) is applied before anything reads `UiText` and before the briefing (171).
- **`RunManager.ContinueRun`** (CRLF): after `World = loaded; DevToolsState.ResetAll();` (131-132), `HistoryService.RebuildLeaderEffect(World, Library, World.day);` (C3, made public; Z5). It runs whether the save resumes at Home or in the Office (piece 5's `ResumeRun` opens the scene afterwards).
- **`ContentLibrarySO.FillFacts`** (piece 5, C7): after each `table.Add(…, History.Resolve(history, nation.id, era.id, f.category, f.value))`, `if (History.IsRevised(history, nation.id, era.id, f.category, f.value)) table.MarkChanged(nation.id, era.id, f.category);` (R14). It runs for `BuildToday` and for `BuildWorldFacts`; nothing reads the marks of the world table, so they are harmless there. The method doc adds "and marks the rows history revised (piece 6)".
- **`ReferenceBookWindowController`** (LF; after piece 3 its row filling is `FillRow`): the label column shows `UiText.Format("book.revisedLabel", fact.OriginLabel)` when `_facts.IsChanged(fact.NationId, fact.EraId, fact.Category)`; the compare label and the evidence are unchanged.
- **`OfficeUIController`** (CRLF): `[SerializeField] private GameObject resultBackdrop;` ("The verdict line's strip; shown only while the line has text (piece 6 R18)"), and a private `SetResult(string text)` that sets `resultText` and activates the backdrop when the text is non-empty; the three writes (99-100, 203-204, 239-243) call it. Strings and currency (§2.8).
- **`InvestigationUIController`** (CRLF): `compareController.ShowDeviation(UiText.Deviation(found))` (127) and the scanner list line `UiText.Format("list.bullet", UiText.Deviation(d))` (164); piece 3's `ShowAlreadyDocumented` passes `UiText.Category(category)`. Strings and fallback (§2.8).
- **`HomeUIController`** (CRLF): the currency sites of §2.8, and `UiText.FitLabel` on `moneyText` (in `Awake`) and on each price label it creates (307-308) (R21).
- **`DebugPanelController`** (CRLF): Timeline Inspector, after piece 5's history lines (its §2.11): "Present culture: {ActiveCultureId or neutral} (cue)", "Theme: {displayName} · labels: {Language} · font: {FontName} · wallet: {UiText.Currency(Label)}", and "Missing UI strings: {MissingKeys.Count}". Leader and ranking stay in piece 5's lines. No new cheat (R20).
- **`DesktopShell`** (CRLF): doc lines 6 and 23 say the Settings window holds the UI-language choice; no code change.

### 2.8 Every UI string through keys

**Rule (R2).** A string that UI code or the builder writes goes through `UiText` (runtime) or the English table (builder). Its English text moves verbatim into `ui.strings` in `world_source.json`, under a key `{area}.{name}`. Domain's report words and sentences are keys too, chosen in Domain (`deviation.*`, `category.*`, `slot.*`, §2.3). The code-owned exceptions are listed in O1 (the saved `tomorrow` lines, Home and Title apart from the wallet, the generated display name, logs). Content strings are not keys (R2).

Flavour keys (34, translated, §2.12) are marked **F**; every other key is Full tier (English in every culture in v1). "Sample" means the builder bakes the key's English text (with sample arguments) into a text that a controller rewrites at runtime; the tag's `labelKey` stays empty so the applier never overwrites it. Numbers are passed as numbers and formatted by the placeholder's specifier (R17); every template below shows its specifiers. Where a row names a text that piece 3 or 4 changes first (the Dialect window, the Clue Log window, the scanner idle text, the PhotoBox), the plan takes the implemented text.

**Builder labels** (`OfficeSceneUIBuilder.cs`, `efe385d` lines):

| Key | English | Line |
|---|---|---|
| `citation.title` F | TIMELINE DEVIATION NOTICE | 84 (sample for `CitationText`) |
| `citation.acknowledge` F | Acknowledge | 85 |
| `briefing.masthead` F / `briefing.start` F | THE TEMPORAL TIMES / START SHIFT | 97-98 |
| `results.masthead` F / `results.goHome` F | SHIFT LEDGER — EVENING EDITION / GO HOME | 99-100 |
| `newsletter.bodySample` | ... | 697 (sample) |
| `claim.placeholder` | Visitor | 121 (sample) |
| `shelf.template` | Book | 128 (sample) |
| `window.directives` F / `directives.none` | Directives / Directives: all destinations cleared today. | 135-136 (body is a sample) |
| `icon.directives` F, `icon.scanner` F, `icon.records` F | Directives, Scanner, Records | 141, 149, 201 |
| `window.scanner` F | Scanner — Deviation Report | 146 (the body sample becomes `scanner.idle`; the short literal "No deviations documented." goes) |
| `intercom.title` F / `intercom.actionSample` | INTERCOM / Request | 154, 158 (sample) |
| `records.title` F / `records.idle` | Citizen Records / Type a citizen's name and press SEARCH. | 172-173 (body is a sample) |
| `records.placeholder` / `records.search` F | Type a full name… / SEARCH | 179-180 |
| `records.row.name` / `records.row.born` | Name / Born | 181-182 (diegetic) |
| `accept` F / `deny` F | ACCEPT / DENY | 213-214 |
| `window.document` | Document | 338 (sample; the PhotoBox has no label after piece 4) |
| `window.reference` | Reference | 364 (sample) |
| `window.prev` / `window.page` / `window.next` | < / Page {0}/{1} / > | 411-413 (page is a sample: 1, 1) |
| `row.sampleLabel` / `row.sampleValue` | Label / Value | 434-435 (diegetic samples) |
| `taskbar.start` F | start | 711 |
| `tray.day` F, `tray.money`, `wallet.credits`, `tray.stability` F | Day {0}; {0}: {1}; Credits; Stability: {0:0}% | 717-719 (samples: 1; Credits, 0; 100). The clock sample is `ShiftClock.Format(540)` (720), not a key. |
| `window.minimize` / `window.maximize` / `window.close` | _ / [] / X | 741-743 |
| `desktop.back` F | < Office | 1251 |
| `icon.internet` F … `icon.notes` F, `icon.clueLog` F | Internet, Lexicon, Dialect, Material, Notes, Clue Log | 1276-1281 |
| `window.internet`, `window.lexicon`, `window.dialect`, `window.material`, `window.clueLog`, `window.notes` | Internet - News, Lexicon, Dialect (piece 3, was "Dialect Filter"), Material Scanner, Case Notes (piece 3: "Case Notes: Interview"), Sticky Notes | 1276-1281 |
| `body.internet`, `body.lexicon`, `body.dialect`, `body.material`, `body.notes` | Today's news feed. (placeholder); Wikipedia-style era glossary. (placeholder); Notes on accents and phrasing. (placeholder) (piece 3); Flags tech/materials beyond the claimed era. (upgrade); Your notes. (placeholder) | 1276-1281 (piece 3 removes the Clue Log body) |
| `window.settings` F | Settings | 1290 (the body "Settings (empty for now)." is replaced, §2.9) |
| `startmenu.settings` F / `startmenu.power` F | Settings / Power | 1297, 1299 |
| `settings.language`, `settings.followHistory`, `settings.alwaysEnglish`, `settings.note` | UI language; Follow history; Always English; Colours, fonts and the wallpaper always follow history. A language change applies the next time the office opens. | new (§2.9) |
| `window.visitor`, `visitor.hint`, `icon.visitor` | Visitor; Click a garment, then a Costume Guide entry, to compare.; Visitor | piece 4's Visitor window and icon |

**Runtime strings** (`efe385d` lines; every file CRLF except `ReferenceBookWindowController.cs`):

| File | Line | Key(s) and English |
|---|---|---|
| `OfficeUIController.cs` | 217, 220, 223 | `tray.money` "{0}: {1}" with `UiText.Currency(Label)` and the balance; `tray.stability` "Stability: {0:0}%"; `tray.day` "Day {0}" |
| | 241-243 | `verdict.correct` "CORRECT  (+{0} {1})"; `verdict.wrong` "WRONG  ({0:+0.#;-0.#} stability)"; `verdict.wrongPenalty` "WRONG  ({0:+0.#;-0.#} stability, -{1} {2})" (the currency is `Currency(Inline)`) |
| `GameManager.cs` | 248 | `day.complete` "Day {0} complete." |
| | 418, 494 | `verdict.simpleCorrect` "Correct" / `verdict.simpleWrong` "Wrong" (the ✅/❌ emoji no project font or sprite asset can draw are dropped) |
| `DayFlowUIController.cs` | 80 | `briefing.title` F "Day {0} — Morning Briefing" |
| | 88 | `briefing.empty` "No directives. Process subjects accurately." |
| | 93, 101 | `list.bullet` "• {0}" |
| | 98 | `briefing.newsHeader` F "— TIMELINE NEWS —" |
| | 126 | `results.title` F "Day {0} — Shift Report" |
| | 131-147 | `results.processed` "Subjects processed: {0}"; `results.correctWrong` "Correct: {0}   Wrong: {1}"; `results.pay` "Pay earned: +{0}"; `results.penalties` "Citation penalties: -{0}"; `results.net` "Net: {0:+0;-0;0} {1}   (Balance: {2})" ({1} = `Currency(Inline)`); `results.stability` "Timeline stability: {0:0}% ({1:+0.#;-0.#;0} today)"; `results.citations` "Citations today: {0}"; `results.unproven` "Undocumented denials: {0} (scan the evidence before denying)" |
| `InvestigationUIController.cs` | 55 | the `_directives` initializer becomes `string.Empty` (`SetDirectives` always runs first) |
| | 127 | `compare.deviationLogged` through `ShowDeviation(UiText.Deviation(found))` |
| | 155-158 | `scanner.idle` (piece 3's text, §1.7 of its spec; piece 4 adds dress) |
| | 164, 187 | `list.bullet` around `UiText.Deviation(d)` and `r.Summary()` |
| | 167 | `scanner.summary` "{0} deviation(s) documented. Denial is justified." |
| | 182, 184 | `directives.none`; `directives.header` "Directives (deny violators):" |
| | 221 | `claim.banner` "{0}\n\"{1}\"" (the display name and piece 3's claim line, both data) |
| | 256 | `window.document` (the request label itself becomes piece-3 content) |
| | 397-449 | `fallback.claim` "{0}\n\"{1}\"\n\n{2}"; `fallback.documents` "— DOCUMENTS PRESENTED —"; `fallback.document` "[{0}]"; `fallback.field` "    {0}: {1}"; `fallback.record` "— AGENCY RECORD —"; `fallback.noRecord` "    No record on file."; `fallback.recordName`/`Born`/`Origin` "    Name: {0}" / "    Born: {0}" / "    Origin: {0}"; piece 3's interview and "REFERENCE (claimed place)" headers by the same rule |
| | 469-484 | `fallback.accept` "ACCEPT (approve travel)"; `fallback.deny` "DENY (refuse travel)"; the literal colours (472, 480, 482, 513) go: `NewUI`/`NewText`/`NewButton` add `ThemeTag`s through `ThemeTag.Configure` (Panel, Panel ink, AcceptButton, DenyButton) and `EnsureFallback` ends with `CultureThemeService.Instance?.ApplyTo(_fallbackPanel)` |
| `CompareController.cs` | 96 | `compare.deviationLogged` "●  DEVIATION LOGGED — {0}" ({0} = `UiText.Deviation`) |
| | 124-125 | `compare.match` F, `compare.mismatch` F; `compare.pair` "{0}    {1}:  {2}    vs    {3}:  {4}" |
| | 130 | `compare.pickAnother` "{0}:  {1}    vs    (pick another value to compare)" |
| | piece 3 | `compare.alreadyDocumented` "●  ALREADY DOCUMENTED — {0} is in the Deviation Report" ({0} = `UiText.Category`) |
| piece 3's `TranscriptWindowController` | answer click | `compare.intercomLabel` "Intercom · {0}" ({0} = `UiText.Category(line.Category)`) |
| piece 4's `TravellerPortraitView` | garment click | `compare.visitorLabel` "Visitor · {0}" ({0} = `UiText.Get(Looks.SlotKey(g.Slot))`) |
| `DocumentWindowController.cs` | 46, 101 | `window.document` |
| | 61 | `window.page` |
| | 102 | `document.compareLabel` "{0} · {1}" |
| `ReferenceBookWindowController.cs` (piece 3: `PagedRowsWindow`) | 50, 98 | `window.reference` |
| | 72 | `window.page` |
| | 117 | `book.compareLabel` "{0}: {1}" |
| | new | `book.revisedLabel` "{0}  [revised]" (§2.7) |
| `CitizenRecordsWindowController.cs` | 42-43 | `records.compare.name` "Records · Name"; `records.compare.born` "Records · Born" |
| | 67, 90 | `records.idle` |
| | 68 | `records.noRecord` "NO RECORD matching \"{0}\"." (the typed query is inserted verbatim) |
| | 75 | `records.onFile` "RECORD ON FILE:" |
| | 81 | `records.origin` "Origin:  {0}" (diegetic text) |
| `Shift/ShiftScoring.cs` | 141-163 | `citation.unproven` (piece 3's text); `citation.acceptedWrong` "Approved a disguised traveller or a forbidden destination."; `citation.deniedWrong` "Denied a legitimate, permitted traveler."; `citation.warning` "Warning {0}/{1} — no pay deduction."; `citation.penalty` "Penalty: -{0} {1}." ({1} = `Currency(Inline)`); `citation.stability` "Stability {0:+0.#;-0.#}"; `citation.layout` "{0}\n{1}\n{2}\n{3}" (title, mistake, warning or penalty, stability) |
| | 207-225 | `citation.misrouted` "Subject misrouted: sent to '{0}', belonged to '{1}'." with the same layout (legacy era path) |
| `Investigation/TravelRuleSO.cs` | 62-74 | `rule.unknownPlace` "?"; `rule.eraForbidden` "No travel permitted to {0}."; `rule.nationForbidden` "No travel permitted to {0}."; `rule.nationEraForbidden` "No travel permitted to {0} in {1}."; `rule.generic` "Travel restriction in effect." (an authored `description` still wins, 59-60) |
| `UI/HomeUIController.cs` | 146 | "Credits" becomes `UiText.Currency(Label)` (the rest of the sentence stays literal, R9) |
| | 189, 269, 358 | "credits" becomes `UiText.Currency(Inline)` |
| | 307-308 | "cr" becomes `UiText.Currency(Short)` ("— {cost} {short}"; "(… % off)" stays literal) |
| `Home/HomeManager.cs` | 181, 226 | "credits" becomes `UiText.Currency(Inline)` |

**Domain report words** (§2.3; English moved verbatim from the Domain code pieces 3 and 4 leave):

| Keys | English |
|---|---|
| `deviation.claimMismatch.papers` / `.said` / `.wears` | {0} INCORRECT — papers: "{1}"  /  expected: "{2}"; the same with `traveller said: "{1}"` / `traveller wears: "{1}"` |
| `deviation.foreignOrigin.papers` / `.said` / `.wears` | {0} INCORRECT — papers show "{1}", which belongs to {2}; the same with `traveller said "{1}"` / `traveller wears "{1}"` |
| `deviation.recordMismatch.papers` / `.said` / `.wears` | {0} INCORRECT — papers: "{1}"  /  agency records: "{2}"; the same with `traveller said: "{1}"` / `traveller wears: "{1}"` |
| `category.Language`, `.Material`, `.Politics`, `.Technology`, `.Currency`, `.Geography`, `.Culture`, `.Name`, `.BirthDate` | LANGUAGE, MATERIAL, RULER, DEVICE, CURRENCY, CAPITAL, DRESS, NAME, BIRTH DATE |
| `slot.Outfit`, `.Hair`, `.FacialHair`, `.Headwear`, `.Accessory` | Outfit, Hair, Facial hair, Headwear, Accessory (the `LookSlot` names are piece 4's) |

Canonical values (names, places, fact values, dates, typed queries, garment labels) are always arguments, never part of a template. Flavour templates (`tray.day`, `tray.stability`, `briefing.title`, `results.title`) take only numbers (R6).

Strings that pieces 3, 4 and 5 add in UI code or the builder follow the same rule; the plan lists them when it re-reads those files.

### 2.9 Settings window

- **Builder** (`BuildDesktopShell`, 1290): `SettingsWindow` becomes a 460×300 `WindowBody` window titled `window.settings`, with `Body` = `settings.language`, two buttons `FollowHistoryButton` (`settings.followHistory`) and `AlwaysEnglishButton` (`settings.alwaysEnglish`), both `Button` role, side by side, and `NoteText` (`settings.note`, wraps). A `SettingsWindowController` on the window gets both buttons.
- **`SettingsWindowController`** (new): `[SerializeField] private Button followHistoryButton, alwaysEnglishButton;` `Awake` wires each to set `UiLanguagePreference.AlwaysEnglish` and call `ShowSelection()`; `OnEnable` calls `ShowSelection()`, which colours the selected button with the active theme's `ActionButton` fill and ink and the other with its `Button` colours (theme from `CultureThemeService.Instance`, else the library's neutral theme). No text changes until the next scene load (R10).
- `DesktopShell` is unchanged: `OpenSettings` already opens this window.

### 2.10 Builder (`OfficeSceneUIBuilder.cs`, LF)

- **Inputs first.** `Build()` loads `ContentLibrary_Main` at its start (today at 223) and takes `NeutralTheme`, the reading `UiStringTableSO` and `CultureUi`. Missing ones log an error naming Generate World, and the build stops before changing the scene.
- **Colours from data.** The XP constants (25-36, including the unused `PanelNavy`) and every inline UI colour literal (80-214, 283-285, 337-346, 392, 427, 629, 689-720, 1252, 1295-1299, 1359-1384, 1391) go. Each graphic's colour comes from `neutral.Get(role)`. `SetColor` for the compare colours (283-285) writes all four fields, `highlightColor` included, from roles 23–26. The booth colours (881-994) stay: the booth is not themed (Z3).
- **Helpers take roles.** `Panel(…, ThemeRoleId? role)` (null = no graphic), `Text(…, ThemeRoleId role, UiLabel label, FontStyles style, ThemeTextKind kind)`, `MakeButton(…, ThemeRoleId role, string labelKey)`, `BuildWindowShell(win, UiLabel title, ThemeRoleId frameRole, ThemeRoleId rowRole)` (the page footer takes `frameRole`'s ink, replacing 346; the document window passes `DiegeticBacking` and `DiegeticRow`, the book window `WindowBody` and `DiegeticBookRow`), `BuildRowTemplate(parent, ThemeRoleId rowRole)`, `BuildOSWindow(layer, name, string titleKey, UiLabel body, Vector2? size, ThemeRoleId bodyRole)` (the Directives window passes `StickyNote`, replacing 137-139), `BuildRecordRow`, `BuildInputField`, `BuildDesktopIcon(…, string labelKey, …)`. `UiLabel` is a private struct: `Static(key)` (tag keeps the key), `Sample(key, args)` (baked text, empty tag key) or `None`. Every call site passes its role from the §2.5 table; the compiler enforces completeness.
- **Authoritative.** `Text` (570-594) re-applies content, size, alignment, anchors, colour, style, tag and the label policy on an existing text; `MakeButton` (596-623) re-applies the image colour, the tag and its label through `Text`; `BuildRowTemplate` (418-439) re-applies its image, layout element, tag and texts. The post-hoc style lines move into the calls (155, 185, 398, 693, 712, 1341, 1364). Sizes follow the builder's literals, except `CitationText`, whose literal becomes 26 (the committed scene's size, §2.12).
- **Label policy.** A keyed label (`Static`) is baked with `textWrappingMode = NoWrap` and auto-size from `CultureUi.labelMinScale × size` to `size` (so glosses and longer words fit). The same policy applies to three runtime-written texts that can receive a long currency name or the revised marker (R21): the tray's `MoneyText`, `VerdictText` and the row template's `Label`. Bodies keep wrapping at fixed size.
- **Tags.** Every graphic the builder creates gets a `ThemeTag` via `ThemeTag.Configure` (role, part, key, base style, kind), including the `InvestigationRoot` dim (`Panel(…, ThemeRoleId.DeskDim)`, R19) and, after piece 4, the portrait layers (`DiegeticArt`) and their `Highlight` overlays (`DiegeticOverlay`) made by `BuildPortraitStack`.
- **Verdict strip** (R18): `Panel(root, "VerdictStrip", (0.25, 0.855), (0.75, 0.925), …, ThemeRoleId.VerdictStrip)`, placed just before `VerdictText` so it draws behind it, inactive after the build; `soOffice` (259-267) wires `resultBackdrop` to it.
- **Wallpaper.** `BuildDesktop` (627-640) uses `neutral.wallpaper` and never paints. `EnsureWallpaper`, `Clouds` and `Blob` (757-818) are removed: the painter moves to Visuals `CulturePlaceholders.Wallpaper`, and Generate World ensures `xp_bliss.png` (R11).
- **PNG helpers.** `WritePlaceholderPng` (836-853) and `EnsureFolderTree` (1497-1506) move, unchanged, into the new editor helper `PlaceholderPng` (`Write`, `EnsureFolderTree`); the builder's remaining callers (`EnsureOfficeShape` 1069, `EnsureCursorTexture` 1208, the config folder 1136) call it. `CursorPixel` stays where piece 4 leaves it (calling `PixelShapes.InPolygon`).
- **Decision glyphs** (R8): `AcceptButton/Glyph` and `DenyButton/Glyph` are 32×32 containers at the label's left (the label's anchors start after them), each with two child `Image`s, `Stroke1` and `Stroke2`: no sprite, `raycastTarget` off, the `Ink` part of their button's role. The tick is a 6×14 bar rotated 45° and a 6×26 bar rotated −45° that meet at the bottom; the cross is two 6×30 bars rotated ±45° through the centre.
- **Booth poster** (`BuildReactiveProp`, 985-994): also writes `channel = UI` and, authoritatively, one mapping per culture theme with a poster: `cueId = CultureCue.Format(theme.cultureId)`, `sprite = theme.boothPoster`.
- **Settings window**: §2.9.
- **Completeness check.** At the end, every `Graphic` under the desktop canvas and `OfficeOverlayCanvas` without a `ThemeTag` is logged as an error with its path ("… has no ThemeTag; give it a role in OfficeSceneUIBuilder"). Excluded: TMP sub-mesh objects, and the legacy objects no builder step owns, which the committed scene still holds: the era-pick texts under the `OfficeUIController` host (`Canvas/Office UI Controller/{VisitorText, Doc1Text, Doc2Text, ResultText, EraButtonsRoot}`, wired to `OfficeUIController`'s legacy fields) and the inactive `Canvas/HUD` the builder hides (47-52). They stay untagged and unthemed until the legacy era path is retired (§4 F10). `InvestigationRoot`'s image is not excluded: the builder now owns it (R19).
- **Idempotence.** A second build changes nothing (checked in §6).
- The final log line mentions the theme tags. The scene is rebuilt in the worktree's Unity and `OfficeScene.unity` is committed.

### 2.11 Content pipeline

**`world_source.json`** (LF) gains a top-level `ui` object and a `culture` object on each country. JsonUtility has no dictionaries and parses enums as integers, so every list is an array of objects and every enum is a string the generator parses (the existing `FactData.category` pattern):

```json
"ui": {
  "readingLanguage": "en", "glossPercent": 60, "labelMinScale": 0.55,
  "contrast": { "text": 4.5, "largeText": 3.0, "glyph": 3.0, "hint": 3.0, "outline": 3.0, "minTextFillAlpha": 0.7 },
  "latinFallbackFont": "Assets/TextMesh Pro/Fonts/LiberationSans.ttf",
  "paletteMap": [ { "role": "Taskbar", "fill": "chrome", "alpha": 1, "ink": "", "textClass": "None" }, "…" ],
  "neutral": { "id": "neutral", "displayName": "Temporal Customs standard issue", "language": "en", "runtimeFont": false,
               "seeds": [ { "name": "chrome", "hex": "#2157DB" }, "…" ], "overrides": [ { "role": "Panel", "fill": "#121A29EB", "ink": "#FFFFFF" }, "…" ],
               "art": [ { "name": "skyTop", "hex": "#336EC2" }, "…" ], "wallpaper": "Assets/Art/Generated/xp_bliss.png", "poster": "" },
  "strings": [ { "key": "accept", "text": "ACCEPT", "gloss": "Below", "tier": "Flavour" }, "…" ],
  "languages": [ { "language": "ar", "rtl": true, "entries": [ { "key": "accept", "text": "قبول" }, "…" ] }, "…" ]
},
"countries": [ { "id": "china", "displayName": "China", "baselines": [ "…" ],
  "culture": { "displayName": "China", "language": "zh-Hans", "runtimeFont": true,
    "fonts": [ { "file": "msyh.ttc", "face": 0, "family": "", "style": "" }, { "file": "", "face": 0, "family": "Microsoft YaHei", "style": "Regular" }, "…" ],
    "headingAdd": "", "buttonAdd": "", "stripItalic": true,
    "seeds": [ { "name": "chrome", "hex": "#A31F1F" }, "…" ], "overrides": [], "art": [],
    "wallpaper": "Assets/Art/Culture/china/wallpaper.png", "poster": "Assets/Art/Culture/china/poster.png" } } ]
```

**`WorldContentGenerator`** (LF):
- `OwnedFolders` (32) gains `"Culture"` (`Assets/Data/World/Culture`, pruned like the others). Art folders are never owned or pruned.
- **Source classes**: `WorldSource.ui` (`UiData`), `CountryData.culture` (`CultureData`), plus `ContrastData`, `RoleRuleData`, `CultureData`, `FontData`, `NamedHexData`, `OverrideData`, `StringData`, `LanguageData`, `EntryData`, all flat with string fields.
- **Checks** (new `CheckCulture(src, errors)`, called before anything is written):
  - `ui` exists; `readingLanguage` has a table built from `strings`; `latinFallbackFont` loads as a `Font`;
  - `paletteMap`: every seed name exists in the neutral seeds; `textClass` parses; the alpha of a fill with a text class is at least `minTextFillAlpha` (role names and the diegetic rule are `Palette.Resolve`'s, below);
  - every country has a culture; every culture's `language` is the reading language or a `languages[]` entry; `headingAdd`/`buttonAdd` parse as `FontStyles` names (empty = none); a culture whose table holds any character above U+03FF other than punctuation (Arabic, CJK) names at least one font, since the Latin fallback cannot draw it (Greek, U+0370–U+03FF, needs none: the runtime LiberationSans draws it); the wallpaper and poster paths lie under `Assets/Art/`;
  - `UiStrings.TableProblems` for every language (unique, non-blank keys; only flavour keys; the same placeholder tokens, specifiers included; shapeable right-to-left text);
  - the keys the code derives from Domain enums are all in `ui.strings`: `ClueLabels.Key` for every `ClueCategory`, `Looks.SlotKey` for every `LookSlot`, and `Discrepancy.ReportKeyFor` for every `DiscrepancyProof` with `DocumentField`, `Answer` and `Appearance`. Other keys are known only to the code, so a key missing from `ui.strings` is caught by `UiText`'s one-time warning and by the Unity smoke, which fails on any missing key (§6);
  - `Palette.Resolve` problems (with `allowDiegetic` only for `neutral`), then `Contrast.Problems` for every culture and the neutral one, with the theme's rings: the pairs from `Palette.Pairs`, plus the diegetic text pairs, each translucent fill composited over what it sits on (diegetic roles have no map rule, so `Pairs` does not list them): "SelectionHighlight on diegetic text" (the `DiegeticRow` ink on the culture's highlight over the `DiegeticRow` fill), document rows (`DiegeticRow` over `DiegeticPaper`), record rows (`DiegeticRow` over the culture's `WindowBody`), record labels and note (the `DiegeticLabel` and `DiegeticNote` inks on the culture's `WindowBody`), book rows (`DiegeticBookRow` over the culture's `WindowBody`) and the scanner footer (`DiegeticBacking` ink on its fill), all at the `Text` minimum. **Any problem aborts the generation**, as reference errors do today (U10).
- **Builders**:
  - `MakeStringTable(language)` writes `Culture/Strings_{language}.asset` (`entries` as authored, `rightToLeft` from `rtl`; the reading table carries gloss and tier);
  - `MakeTheme(culture, id)` writes `Culture/Theme_{id}.asset`: the resolved palette (one `PaletteEntry` per role, diegetic roles only for neutral), `ringDark`/`ringLight` from the seeds of those names, fonts, styles, and the art below;
  - `EnsureCultureArt(path, pixels)`: when no file exists at `path`, writes the painted bytes with `PlaceholderPng.Write` and imports them as a Sprite (wallpaper 960×540 like `xp_bliss.png`, no mipmaps; poster 80×110, 100 pixels per unit, centred pivot, like the office placeholders), logging "[WorldContentGenerator] No art at {path}; generated a text-free placeholder. Replace the PNG in place (keep its .meta) with final text-free art." An existing file is never touched. Wallpapers (every culture, and `xp_bliss.png` for neutral) come from `CulturePlaceholders.Wallpaper` with `skyTop`, `skyBottom`, `groundLow`, `groundHigh` and `cloud` from the theme's `art` list, defaulting to `chrome`, `wallpaper`, `chromeDeep`, `accent` and `paper`; posters from `CulturePlaceholders.Poster` with a `chrome` field, an `accent` disc and a `dark` border. No text is ever painted.
- **`WireLibrary`** (293-310) also sets `cultureUi` (through `SerializedProperty.boxedValue`), `neutralTheme`, and `themes` (country order) and `stringTables` through piece 4's `SerializedArrays.Set` (authoritative).
- The class doc (9-22) lists the culture themes and string tables and says it owns `Assets/Data/World/Culture` and may create (never replace) culture art and the neutral wallpaper.

**`ContentLibraryValidator`** (CRLF) gains `private static int CheckCulture(ContentLibrarySO lib)`, called from `ValidateLibrary` (50) like the existing checks, its issue count added to `issues`:
- the neutral theme exists; every library nation has a theme (a missing one is a warning: that culture stays neutral); no duplicate culture ids;
- every theme's language has a string table, and the reading table exists;
- `UiStrings.TableProblems` for every culture table against the reading table;
- `Contrast.Problems` on every theme's stored palette and rings (the same pairs as the generator);
- a theme whose table holds characters above U+03FF other than punctuation has at least one font candidate; `CultureUi.latinFallbackFont` is set;
- missing wallpaper (culture or neutral) or culture poster: a warning.

### 2.12 Content

**Neutral theme** (`ui.neutral`): language `en`, `runtimeFont: false`, no poster, wallpaper `Assets/Art/Generated/xp_bliss.png` (existing; Generate World paints it with the Bliss colours below only if it is missing). Seeds: wallpaper `#3B73BD`, chrome `#2157DB`, chromeInk `#FFFFFF`, chromeDeep `#1A52C7`, accent `#3D993B`, accentInk `#FFFFFF`, paper `#ECE9D8`, ink `#1A1714`, face `#ECE9D8`, faceInk `#000000`, note `#FFFFE0`, approve `#296B38`, approveInk `#FFFFFF`, reject `#DB402E`, rejectInk `#FFFFFF`, dark `#292621`, darkInk `#FFFFFF`, match `#0D731F`, mismatch `#B81A14`, highlight `#FFEB59`, panelTitle `#B3D9FF`, muted `#737373CC`, white `#FFFFFF`, ringDark `#092E70`, ringLight `#FFD76E`. Overrides reproduce the remaining committed values exactly: Panel `#121A29EB`, PanelTitle fill `#121A29EB`, StartMenu `#1A1F2EF7`, ClaimStrip and VerdictStrip `#0F2E6BCC` / `#FFFFFF`, ActionButton `#29476B`, SearchButton `#264D80`, DesktopIcon `#334D73D9`, BackButton `#334D80F2`, MenuEntry `#334059`, DenyButton `#752929`, Alert `#D93326F5`, PowerEntry `#803333`, NewsletterBorder `#1A1714`, DeskDim `#0F121A8C`, StickyNote `#FFF599F7`, CompareNeutral ink `#2E260D`, and the diegetic roles 36–44 (§2.5). Bliss art colours: skyTop `#336EC2`, skyBottom `#C7E6FF`, groundLow `#457326`, groundHigh `#7DB042`, cloud `#FFFFFF`. Hex rounding moves each channel by at most 0.002 from the builder's floats.

**Where the committed scene and the builder's literals differ** (read from `OfficeScene.unity` at `efe385d`; the builder never re-applied these, because `Text`/`MakeButton`/`BuildRowTemplate` return existing objects, `Panel(…, null)` leaves an image alone and `BookWindowTemplate` is never rebuilt). R16 makes the builder authoritative, so each gets a deliberate neutral value:

| Object | Scene | Builder literal | Neutral keeps | Why |
|---|---|---|---|---|
| `InvestigationRoot/AcceptButton` | `#296B38` | `#33803D` | the scene's (seed `approve`) | the colours players know; 6.5:1 with white |
| `InvestigationRoot/DenyButton` | `#752929` | `#B8332E` | the scene's (override) | the same; 10.0:1 with white |
| `BookWindowTemplate/Rows/RowTemplate` | `#FFFFFF0A` | `#FFFFFFB3` | the scene's (`DiegeticBookRow`) | a book reads as a list on the window paper, a document as fields on a scan |
| `CitationPanel/CitationText` | 26 pt | 24 pt | the scene's (the literal becomes 26) | the slip's readability |
| `InvestigationRoot` image | `#0F121A8C` | none (`Panel(…, null)`) | the scene's (`DeskDim`, R19) | every investigation control sits on it |
| `BookWindowTemplate/{PrevButton, NextButton}`, `BookShelf/BookShelfButtonTemplate` | `#E6E6EB` | `#ECE9D8` | the builder's (`Button`) | one face for every default button; barely visible |
| `CitationPanel/ContinueButton` and its label | `#F2F2F2`, 26 pt | `#ECE9D8`, 22 pt | the builder's (`Button`) | the same; the glossed label shrinks to fit |
| `BookWindowTemplate/Header/TitleText` | 20 pt | 15 pt | the builder's | every other window title is 15 pt |
| `CompareBar/CompareText` | `#FFF2B2` | ink `#1A1714` | the builder's | never seen: `CompareController` sets the colour before the bar shows |

The builder's rows are intended visible changes (§1.7), listed as exceptions in §6 step 4.

**Palette map** (`ui.paletteMap`; fill seed with alpha / ink seed / class). No role is marked as a surface any more: the rings contrast with any background (R7).

| Role | Fill | Ink | Class |
|---|---|---|---|
| Desktop | wallpaper | – | – |
| VerdictStrip | chromeDeep ×0.8 | chromeInk | LargeText |
| Taskbar | chrome | – | – |
| TaskbarGloss / TitleGloss | white ×0.18 / white ×0.14 | – | – |
| StartButton | accent | accentInk | LargeText |
| Tray | chromeDeep | chromeInk | Text |
| WindowBody | paper | ink | Text |
| TitleBar | chrome | chromeInk | Text |
| Button | face | faceInk | Text |
| CloseButton | reject | rejectInk | Glyph |
| AcceptButton | approve | approveInk | Text |
| DenyButton, PowerEntry | reject | rejectInk | Text |
| ActionButton, SearchButton, MenuEntry | accent | accentInk | Text |
| DesktopIcon / BackButton | accent ×0.85 / accent ×0.95 | accentInk | Text |
| Panel | chromeDeep ×0.92 | chromeInk | Text |
| PanelTitle | chromeDeep ×0.92 (backdrop) | panelTitle | LargeText |
| ClaimStrip | chromeDeep ×0.8 | chromeInk | LargeText |
| Alert | reject ×0.96 | rejectInk | LargeText |
| StickyNote | note ×0.97 | ink | Text |
| CompareBar, CompareNeutral | note | ink | Text |
| CompareMatch / CompareMismatch | note (backdrop) | match / mismatch | Text |
| SelectionHighlight | highlight ×0.7 | – | – (checked against diegetic ink, §2.11) |
| StartMenu | chromeDeep ×0.97 | – | – |
| NewsletterBorder | dark | – | – |
| Newsletter | paper | ink | Text |
| NewsletterButton | dark | darkInk | Text |
| DeskDim | dark ×0.55 | – | – |
| InputField | white | ink | Text |
| InputPlaceholder | white (backdrop) | muted | Hint |

**Culture seeds** (`chromeInk`, `approveInk` and `rejectInk` are `#FFFFFF` for every culture; `white` and `muted` come from neutral). All nine themes pass §1.5 (computed with `scratchpad/p6/contrast.py`, revised for this review: lowest text ratio per culture 5.0–8.1, large text 3.6–8.7, rings 9.2–12.5:1 apart, diegetic text on every culture's window paper at least 6.2:1):

| Culture | wallpaper | chrome / chromeDeep | accent / accentInk | paper / ink | face / faceInk | note | approve / reject | dark / darkInk | match / mismatch | highlight | panelTitle | ring dark / light |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| egypt | #C9A45C | #1C4E80 / #123456 | #C8962E / #1A1208 | #F2E6C9 / #2B1D0E | #E8D9B5 / #2B1D0E | #FFF3C4 | #2E7D32 / #A8322A | #2B1D0E / #F2E6C9 | #1B5E20 / #9E1B12 | #E9C46A | #F2D27A | #0F2A4D / #F2D27A |
| iraq | #2E6F95 | #1B4F72 / #0E2F44 | #D4A017 / #1B1405 | #EFE6D2 / #231A10 | #E3D6B8 / #231A10 | #FDF1C7 | #2F7D4A / #A63A2A | #231A10 / #EFE6D2 | #1E6B3A / #9A2A1C | #F0C75E | #F0C75E | #0D2A3D / #F0C75E |
| greece | #6FA8DC | #0D5EAF / #0A3D73 | #1F6F8B / #FFFFFF | #FAFAF7 / #1B2433 | #E6EBF0 / #1B2433 | #FFF6D8 | #2E7D5B / #B23A3A | #1B2433 / #FAFAF7 | #1E6B45 / #A12D2D | #FFE08A | #BFE0FF | #093768 / #FFE08A |
| italy | #C98B5A | #7A2E1F / #4A1C13 | #2E6B3F / #FFFFFF | #F5EBDD / #2A1A12 | #EADBC6 / #2A1A12 | #FFF1D0 | #3C7A3A / #A4262C | #2A1A12 / #F5EBDD | #2F6B2D / #A4262C | #F4D58D | #F4D58D | #4A1C13 / #F4D58D |
| china | #8E1B1B | #A31F1F / #5E0F0F | #D4AF37 / #2A1B00 | #FBF3E4 / #1E1410 | #F1E3C8 / #1E1410 | #FFF4CC | **#B8262A / #37474F** | #1E1410 / #FBF3E4 | #9B1B1B / #37474F | #F5D76E | #F5D76E | #5E0F0F / #F5D76E |
| japan | #E9DFC8 | #1F2F4A / #121C2E | #B23A1A / #FFFFFF | #F7F3EA / #1A1A1A | #ECE4D4 / #1A1A1A | #FFF7E0 | #2E6B4F / #9C2F14 | #1A1A1A / #F7F3EA | #2E6B4F / #9C2F14 | #F3D9A0 | #F3B38A | #121C2E / #F3B38A |
| britain | #5B7F95 | #1F2A44 / #121A2B | #00563F / #FFFFFF | #F4EFE3 / #1C1C1C | #E7E0D0 / #1C1C1C | #FFF8DC | #1E5631 / #7B1E1E | #1C1C1C / #F4EFE3 | #1E5631 / #8B1E1E | #EED9A0 | #D4C28A | #121A2B / #EED9A0 |
| germany | #9EA3A8 | #2B2B2B / #161616 | #E0B000 / #111111 | #F2F2F0 / #111111 | #E2E2DE / #111111 | #FFF4B8 | #2F6F3E / #C0272D | #111111 / #F2F2F0 | #2F6F3E / #B0232A | #FFE066 | #FFCC00 | #161616 / #FFCC00 |

China's approval is red and its rejection slate (red is auspicious); its MATCH is red and MISMATCH slate in the same sense. The ✓/✗ glyphs, fixed positions and glosses carry the meaning.

**Fonts and styles** (`runtimeFont: true` for every culture; the runtime LiberationSans asset always ends the chain):

| Culture | Language | Candidates in order | Styles |
|---|---|---|---|
| egypt, iraq | ar | Segoe UI; Tahoma; Arial; Geeza Pro (macOS); Noto Sans Arabic (Linux) | stripItalic |
| greece | el | none: the runtime LiberationSans, which draws Greek (U8, R5) | – |
| italy | it | Palatino Linotype; Book Antiqua; Palatino (macOS) | headingAdd SmallCaps |
| china | zh-Hans | file `msyh.ttc` face 0; Microsoft YaHei; SimSun; PingFang SC (macOS); Noto Sans CJK SC (Linux) | stripItalic |
| japan | ja | file `YuGothR.ttc` face 0; Yu Gothic; MS Gothic; Hiragino Sans (macOS); Noto Sans CJK JP (Linux) | stripItalic |
| britain | en | Georgia | headingAdd SmallCaps |
| germany | de | Segoe UI; Arial; Helvetica Neue (macOS) | buttonAdd Bold |

Every family style is "Regular". The Windows faces were found on the development machine (the analysis's font probe); the macOS and Linux names are unverified and harmless when absent.

**Flavour translations** (`ui.strings` English + `ui.languages[]`; drafted by Claude, to be reviewed by native readers before release, §7):

| Key | en | ar | el | it | de | zh-Hans | ja |
|---|---|---|---|---|---|---|---|
| accept (Below) | ACCEPT | قبول | ΕΓΚΡΙΣΗ | ACCETTA | ANNEHMEN | 批准 | 承認 |
| deny (Below) | DENY | رفض | ΑΠΟΡΡΙΨΗ | RIFIUTA | ABLEHNEN | 拒绝 | 却下 |
| taskbar.start | start | ابدأ | έναρξη | avvio | Start | 开始 | スタート |
| startmenu.settings | Settings | الإعدادات | Ρυθμίσεις | Impostazioni | Einstellungen | 设置 | 設定 |
| startmenu.power | Power | إيقاف التشغيل | Τερματισμός | Spegni | Beenden | 关机 | 電源 |
| briefing.masthead | THE TEMPORAL TIMES | صحيفة الزمن | Η ΧΡΟΝΙΚΗ ΕΦΗΜΕΡΙΔΑ | IL CORRIERE DEL TEMPO | DER ZEITBOTE | 时间时报 | 時空新報 |
| briefing.start (Below) | START SHIFT | ابدأ المناوبة | ΕΝΑΡΞΗ ΒΑΡΔΙΑΣ | INIZIA IL TURNO | SCHICHT BEGINNEN | 开始值班 | 勤務開始 |
| results.masthead | SHIFT LEDGER — EVENING EDITION | سجل المناوبة — الطبعة المسائية | ΗΜΕΡΟΛΟΓΙΟ ΒΑΡΔΙΑΣ — ΑΠΟΓΕΥΜΑΤΙΝΗ ΕΚΔΟΣΗ | REGISTRO DEL TURNO — EDIZIONE DELLA SERA | SCHICHTBUCH — ABENDAUSGABE | 值班日志 — 晚间版 | 勤務台帳 — 夕刊 |
| results.goHome (Below) | GO HOME | إلى البيت | ΣΠΙΤΙ | A CASA | NACH HAUSE | 回家 | 帰宅 |
| intercom.title | INTERCOM | الاتصال الداخلي | ΕΝΔΟΕΠΙΚΟΙΝΩΝΙΑ | CITOFONO | SPRECHANLAGE | 对讲机 | インターホン |
| records.title | Citizen Records | سجلات المواطنين | Μητρώο Πολιτών | Anagrafe | Einwohnerregister | 公民档案 | 住民記録 |
| records.search (Below) | SEARCH | بحث | ΑΝΑΖΗΤΗΣΗ | CERCA | SUCHEN | 搜索 | 検索 |
| compare.match (Inline) | MATCH | تطابق | ΤΑΙΡΙΑΖΕΙ | CORRISPONDE | STIMMT | 相符 | 一致 |
| compare.mismatch (Inline) | MISMATCH | عدم تطابق | ΔΕΝ ΤΑΙΡΙΑΖΕΙ | NON CORRISPONDE | ABWEICHUNG | 不符 | 不一致 |
| citation.title | TIMELINE DEVIATION NOTICE | إشعار انحراف زمني | ΕΙΔΟΠΟΙΗΣΗ ΧΡΟΝΙΚΗΣ ΑΠΟΚΛΙΣΗΣ | AVVISO DI DEVIAZIONE TEMPORALE | MELDUNG EINER ZEITABWEICHUNG | 时间线偏差通知 | 時間線逸脱通知 |
| citation.acknowledge (Below) | Acknowledge | تم الاطلاع | Ελήφθη | Preso atto | Zur Kenntnis | 知悉 | 了解 |
| window.directives | Directives | التوجيهات | Οδηγίες | Direttive | Anweisungen | 指令 | 指示 |
| window.scanner | Scanner — Deviation Report | الماسح — تقرير الانحرافات | Σαρωτής — Αναφορά αποκλίσεων | Scanner — Rapporto deviazioni | Scanner — Abweichungsbericht | 扫描仪 — 偏差报告 | スキャナー — 逸脱報告 |
| window.settings | Settings | الإعدادات | Ρυθμίσεις | Impostazioni | Einstellungen | 设置 | 設定 |
| icon.directives | Directives | التوجيهات | Οδηγίες | Direttive | Anweisungen | 指令 | 指示 |
| icon.scanner | Scanner | الماسح | Σαρωτής | Scanner | Scanner | 扫描仪 | スキャナー |
| icon.records | Records | السجلات | Μητρώο | Anagrafe | Register | 档案 | 記録 |
| icon.internet | Internet | الإنترنت | Διαδίκτυο | Internet | Internet | 互联网 | インターネット |
| icon.lexicon | Lexicon | المعجم | Λεξικό | Lessico | Lexikon | 词典 | 用語集 |
| icon.dialect | Dialect | اللهجات | Διάλεκτος | Dialetto | Mundart | 方言 | 方言 |
| icon.material | Material | المواد | Υλικά | Materiali | Material | 材料 | 素材 |
| icon.notes | Notes | ملاحظات | Σημειώσεις | Appunti | Notizen | 便笺 | メモ |
| icon.clueLog | Clue Log | سجل الأدلة | Ενδείξεις | Indizi | Hinweise | 线索 | 手がかり |
| tray.day | Day {0} | اليوم {0} | Ημέρα {0} | Giorno {0} | Tag {0} | 第{0}天 | {0}日目 |
| tray.stability | Stability: {0:0}% | الاستقرار: {0:0}% | Σταθερότητα: {0:0}% | Stabilità: {0:0}% | Stabilität: {0:0}% | 稳定度：{0:0}% | 安定度：{0:0}% |
| desktop.back (Below) | < Office | العودة إلى المكتب | < Γραφείο | < Ufficio | < Büro | < 办公室 | < オフィス |
| briefing.title | Day {0} — Morning Briefing | اليوم {0} — الإحاطة الصباحية | Ημέρα {0} — Πρωινή ενημέρωση | Giorno {0} — Briefing del mattino | Tag {0} — Morgenbesprechung | 第{0}天 — 晨间简报 | {0}日目 — 朝のブリーフィング |
| results.title | Day {0} — Shift Report | اليوم {0} — تقرير المناوبة | Ημέρα {0} — Αναφορά βάρδιας | Giorno {0} — Rapporto del turno | Tag {0} — Schichtbericht | 第{0}天 — 值班报告 | {0}日目 — 勤務報告 |
| briefing.newsHeader | — TIMELINE NEWS — | — أخبار الخط الزمني — | — ΕΙΔΗΣΕΙΣ ΧΡΟΝΟΓΡΑΜΜΗΣ — | — NOTIZIE DELLA LINEA TEMPORALE — | — ZEITLINIEN-NACHRICHTEN — | — 时间线新闻 — | — 時間線ニュース — |

All Arabic entries shape with §2.4's tables, and every translation keeps its English placeholders, format specifiers included (checked with `scratchpad/p6/shaper.py`). "< Office" in Arabic drops the arrow ("back to the office"), because a mirrored arrow would point the wrong way in an unmirrored layout.

### 2.13 Knobs

| Knob | Where at runtime | Value | Authored in |
|---|---|---|---|
| Per-culture palette (seeds, overrides) | `ThemeSO.palette`, `ringDark`/`ringLight` (resolved) | §2.12 | `countries[].culture.seeds/overrides`, `ui.neutral` |
| Role → seed map, contrast classes | used by the generator and the validator | §2.12 | `ui.paletteMap` |
| Contrast minimums, text-fill alpha floor | `ContentLibrarySO.CultureUi.contrast` | 4.5 / 3 / 3 / 3 / outline 3 (rings 9:1 apart) / alpha 0.7 | `ui.contrast` |
| Culture language | `ThemeSO.language` | §2.12 | `countries[].culture.language` |
| Label tables, number formats | `UiStringTableSO` (templates carry their format specifiers) | §2.8, §2.12 | `ui.strings`, `ui.languages[]` |
| Reading language | `CultureUi.readingLanguage` | `en` | `ui.readingLanguage` |
| Gloss size | `CultureUi.glossPercent` | 60 | `ui.glossPercent` |
| Label auto-size floor | `CultureUi.labelMinScale` (read by the builder and `UiText.FitLabel`) | 0.55 | `ui.labelMinScale` |
| Font candidates, style additions | `ThemeSO.fonts`, `headingAdd`, `buttonAdd`, `stripItalic` | §2.12 | `countries[].culture` |
| Latin fallback font | `CultureUi.latinFallbackFont` | `LiberationSans.ttf` | `ui.latinFallbackFont` |
| Wallpaper, poster | `ThemeSO.wallpaper`, `boothPoster` | art paths (R11) | `countries[].culture.wallpaper/poster`, `ui.neutral.wallpaper` |
| Placeholder painter colours | generator only | defaults from seeds | `countries[].culture.art`, `ui.neutral.art` |
| UI language preference | PlayerPrefs `TimeDesk.UiLanguage` | history | Settings window |

No knob is a code constant. The fixed numbers in code are TMP's atlas parameters for OS fonts (90 pt sampling, 9 px padding, 1024², the values `CreateFontAsset(familyName, …)` itself uses, `TMP_FontAsset.cs:494`), passed unchanged to the path overload, and geometry: the builder's layout numbers (the glyph bars included) and the placeholder painters' shapes, moved unchanged from the builder.

### 2.14 Every file that changes

| File | Assembly | Change |
|---|---|---|
| `Assets/Scripts/Domain/CultureCue.cs` (piece 5) | Domain | `Pick` |
| `Assets/Scripts/Domain/History.cs` (piece 5) | Domain | `IsRevised` |
| `Assets/Scripts/Domain/FactTable.cs` | Domain | `MarkChanged`, `IsChanged`, doc |
| `Assets/Scripts/Domain/DiscrepancyLog.cs` | Domain | `ReportKeyFor`, `ReportKey`, `ReportOther`; − `Summary` |
| `Assets/Scripts/Domain/ClueLabels.cs` (piece 3) | Domain | `Key` replaces `Report` |
| `Assets/Scripts/Domain/Looks.cs` (piece 4) | Domain | `SlotKey` replaces `SlotLabel` |
| `Assets/Scripts/Visuals/{Rgba,Contrast,Palette,UiStrings,ArabicShaper,ThemeRoles,CultureChoice,CulturePlaceholders}.cs` (+metas) | Visuals | new |
| `Assets/Scripts/UI/Theme.meta` and `Assets/Scripts/UI/Theme/{ThemeTag,ThemeSO,UiStringTableSO,CultureUiSettings,CultureThemeService,CultureThemeBootstrap,RuntimeFonts,UiText,UiLanguagePreference,SettingsWindowController}.cs` (+metas) | Assembly-CSharp | new |
| `Assets/Scripts/Timeline/TimelineCueReceiver.cs` | Assembly-CSharp | `ListenChannel` |
| `Assets/Scripts/Timeline/TimelineReactiveSprite.cs` | Assembly-CSharp | default-sprite capture, docs |
| `Assets/Scripts/Timeline/HistoryService.cs` (piece 5) | Assembly-CSharp | `RebuildLeaderEffect` public, doc (the one §2.2 adaptation) |
| `Assets/Scripts/ContentLibrarySO.cs` | Assembly-CSharp | culture fields, lookups; the `MarkChanged` call in piece 5's `FillFacts` |
| `Assets/Scripts/Core/RunManager.cs` | Assembly-CSharp | Continue culture step |
| `Assets/Scripts/GameManager.cs` | Assembly-CSharp | `RefreshActive` call; strings (248, 418, 494) |
| `Assets/Scripts/OfficeUIController.cs` | Assembly-CSharp | strings, currency, `resultBackdrop` |
| `Assets/Scripts/UI/DayFlowUIController.cs` | Assembly-CSharp | strings, currency |
| `Assets/Scripts/UI/InvestigationUIController.cs` | Assembly-CSharp | strings, deviation lines through `UiText.Deviation`; fallback tags and theme |
| `Assets/Scripts/UI/CompareController.cs` | Assembly-CSharp | `ApplyTheme`, evidence-less overload removed, strings |
| `Assets/Scripts/UI/DocumentWindowController.cs` | Assembly-CSharp | strings |
| `Assets/Scripts/UI/ReferenceBookWindowController.cs` (and piece 3's `PagedRowsWindow.cs`) | Assembly-CSharp | strings, revised marker |
| `Assets/Scripts/UI/CitizenRecordsWindowController.cs` | Assembly-CSharp | strings |
| `Assets/Scripts/UI/HoverHighlighter.cs`, `Assets/Scripts/UI/HoverUIOutline.cs` | Assembly-CSharp | the theme's two rings |
| `Assets/Scripts/UI/InteractionFeedbackSO.cs` | Assembly-CSharp | doc (35) |
| `Assets/Scripts/UI/DesktopShell.cs` | Assembly-CSharp | docs (6, 23) |
| `Assets/Scripts/Shift/ShiftScoring.cs` | Assembly-CSharp | citation strings, currency |
| `Assets/Scripts/Investigation/TravelRuleSO.cs` | Assembly-CSharp | fallback summaries |
| `Assets/Scripts/UI/HomeUIController.cs`, `Assets/Scripts/Home/HomeManager.cs` | Assembly-CSharp | currency forms; `FitLabel` (Home controller) |
| `Assets/Scripts/DevTools/DebugPanelController.cs` | Assembly-CSharp | inspector lines |
| pieces 3–4 UI files that write strings or report words (`TranscriptWindowController`, `TravellerPortraitView`, any Visitor-window text) | Assembly-CSharp | strings by §2.8's rule |
| `Assets/Editor/PlaceholderPng.cs` (+meta) | Editor | new: `Write`, `EnsureFolderTree` (moved from the builder) |
| `Assets/Editor/WorldContentGenerator.cs` | Editor | §2.11 |
| `Assets/Editor/ContentLibraryValidator.cs` | Editor | `CheckCulture` from `ValidateLibrary` |
| `Assets/Editor/OfficeSceneUIBuilder.cs` | Editor | §2.10 |
| `Assets/Data/World/world_source.json` | content | `ui`, `countries[].culture` |
| `Assets/Data/World/Culture.meta`, `Theme_{neutral,egypt,iraq,greece,italy,china,japan,britain,germany}.asset`, `Strings_{en,ar,el,it,de,zh-Hans,ja}.asset` (+metas) | generated | new |
| `Assets/Art/Culture.meta`, `Assets/Art/Culture/{8 ids}.meta`, `…/{id}/wallpaper.png`, `poster.png` (+metas) | generated placeholder art (LFS, 16 files) | new |
| `Assets/Data/Content Library/ContentLibrary_Main.asset` | generated | culture fields |
| `Assets/Scenes/OfficeScene.unity` | scene | rebuilt |
| `Assets/Tests/EditMode/{Contrast,Rgba,Palette,UiStrings,ArabicShaper,ThemeRoles,CultureChoice,CulturePlaceholders}Tests.cs` (+metas) | tests | new, §5 |
| `Assets/Tests/EditMode/{CultureCue,History,FactTable,DiscrepancyLog}Tests.cs` and the tests of `ClueLabels` and `Looks` (pieces 3–5) | tests | added or moved rows, §5 |
| `docs/FEATURES.md` | docs | §3.3 |
| `docs/UI_ART_CONTRACT.md` | docs | new: text-free art; neutral greyscale chrome sprites (tinted by the theme); the office art convention (culture art paths and sizes, placeholders replaced in place) next to piece 4's character-art convention (`docs/CHARACTER_ART_CONTRACT.md`, loaded by key at runtime); diegetic surfaces untouched |
| `docs/superpowers/specs/2026-09-24-ui-reacts-design.md` | docs | this spec, committed first |

Checked and not changed: `SaveSystem.cs` (no version bump), `WorldState.cs`, `EffectSO.cs`, `TimelineEffects.cs`, `TimelineService.cs` (piece 5 already gives `RemoveEffectsFrom` and the leader step), `CaseFactory.cs` (the display name and claim stay generation data, O1(c), piece 3 R26), `OfficeReadouts.cs`, `ShiftClockReadouts.cs`, `ShiftClock.cs`, `OfficeViewController.cs`, `OSWindowChrome.cs`, `DesktopIcon.cs`, `InteractionPanelController.cs`, `InteractionFeedbackBootstrap.cs`, `TitleUIController.cs`, `TitleSceneController.cs`, `HomeSceneBuilder.cs`, `TitleSceneBuilder.cs`, `BirthDates.cs`, `OriginLabels.cs`, `CitizenRegistry.cs`, `EndingService.cs`; `Assets/TextMesh Pro/**` (piece 6 never writes them; §6 checks that no culture code point reaches the tracked fallback asset); `HomeScene.unity`, `TitleScene.unity`; no glyph PNGs (R8).

### 2.15 Why some logic stays outside Domain and Visuals

The glue is `CultureThemeService` (walks scenes, sets `Graphic` colours, fonts and sprites), `RuntimeFonts` (TMP and OS font APIs), `UiText` (Resources, logging), `UiLanguagePreference` (PlayerPrefs), `SettingsWindowController`, `HoverUIOutline` (a uGUI mesh effect), the builder, the generator and the validator. Each reads engine objects or writes scenes and assets. Every decision they make is a call into tested code: `CultureCue.Pick`, `History.IsRevised`, `Discrepancy.ReportKeyFor`/`ReportOther`, `ClueLabels.Key`, `Looks.SlotKey`, `FactTable.MarkChanged`/`IsChanged` (Domain); `ThemeRoles.IsDiegetic`, `CultureChoice.Language`/`Wallet`/`ComposeStyle`, `Palette.Resolve`/`Pairs`, `Contrast.Problems`, `UiStrings` (lookup, fallback, gloss, number formats, table rules), `ArabicShaper`, `CulturePlaceholders` (Visuals). Two small things stay in glue, each with a reason: `UiText.Deviation` puts three arguments into the template Domain chose (a one-line call; the Unity smoke reads a scanner line and a compare-bar line), and `HoverUIOutline`'s second ring is a mesh pass on the engine's `Shadow` helpers (the Unity smoke reads the effect's colours, and the screenshots show it). The cue grammar and the report keys are game data, so they live in Domain; colour maths, text and presentation rules live in Visuals.

### 2.16 Intent audit: the existing mechanism tried for each new one

| New | Existing mechanism reused or tried | Why something new was still needed |
|---|---|---|
| Theme host | `TimelineCueReceiver` (subclassed, one virtual added); `InteractionFeedbackBootstrap` (pattern copied: persistent host, configured before enable) | Nothing applies UI looks; a per-scene receiver races `GameManager.Start` |
| Theme and string assets | `WorldContentGenerator` + `world_source.json`; `ContentLibrarySO` lookups; piece 4's `SerializedArrays` | none (new data types only) |
| Role tags | the builder's `Panel`/`Text`/`MakeButton` chokepoints | a runtime applier needs to find its targets; name paths would be hidden coupling |
| Booth poster | `TimelineReactiveSprite` (extended, not cloned) | none |
| Hover outline | `HoverHighlighter` and its `HoverUIOutline` effect (a second ring added to the same effect) | the settings asset must not be mutated at runtime, and one colour cannot contrast with every background |
| Compare colours | `CompareController` serialized colours; equality is piece 4's `ValuesMatch`/`MatchValue` | a public entry point for colours |
| Deviation words | Domain `Discrepancy.Summary`, `ClueLabels.Report`, `Looks.SlotLabel` (replaced by keys chosen in Domain, not copied) | sentences in Domain could not be translated as data (U4, U7) |
| String layer | none exists; `com.unity.localization` rejected (U7) | new |
| Contrast maths | none | new |
| Arabic shaping | none (TMP's shaper is commented out); RTLTMPro rejected (a download) | new |
| Fonts | `TMP_FontAsset.CreateFontAsset` overloads | a cache with an ordered chain |
| Placeholder art | the builder's Bliss painter (moved to Visuals) and PNG helpers (moved to `PlaceholderPng`, not copied); piece 4's `PixelShapes` (reused); piece 4's runtime placeholders (tried, R11) | none |
| Decision glyphs | TMP text (no ✓/✗ in the static atlas); the builder's polygon painter (not needed) | two plain bars per glyph |
| Settings UI | the existing Settings stub window and `DesktopShell.OpenSettings` | a controller for two buttons |
| Preference storage | the run save (rejected: per run, deleted by New Run) | PlayerPrefs |
| Wallet | piece 5's resolved Future facts (C6) | none |
| Force culture | piece 5's force-leader cheat and emission step (C3, C4) | none (R20) |
| Revised marker | `FactTable` (extended); piece 5's `History.Resolve` (`IsRevised` sits beside it) | none |
| Verdict strip | the claim strip's colours and `Panel` | a backdrop for a text that sat on the wallpaper |

### 2.17 The analysis's completeness list, item by item

| # | Item (`piece6_map.json` critique.missing) | Where handled |
|---|---|---|
| 1 | PR #3 already wrote a country argmax (`ScoreRanking.TryGetTop`) and `RankedArtNaming.HsvFor` | Piece 5 owns the ranking (its H8 ports `ScoreRanking`; Z1). Piece 6 ranks nothing. Palettes are authored, so `HsvFor` is not needed. |
| 2 | Main's uncommitted OfficeScene assigns Codex's UI sprites; `Panel()` re-tints them | Z3: chrome sprites are tinted by their role, so chrome art must be neutral greyscale (`docs/UI_ART_CONTRACT.md`); the builder never assigns or clears chrome sprites except the wallpaper. Codex's coloured sprites will look double-tinted until re-exported (§7). Theme-driven chrome sprite slots are follow-up F5. |
| 3 | Two theming axes (per-traveller document kits vs the PC) | Z4 diegetic roles keep document content out of PC theming (O2 for the frames); skinning documents by the traveller's place is follow-up F9. |
| 4 | Player-visible English missed by the string list | §2.8 covers each: ShiftScoring citations, the verdict line, `TravelRuleSO.Summary`; `Page n/m`; Records strings; the deviation lines and category words (R2). The claim line and the intro are piece-3 content (its R26, R8); the "Request" label became piece-3 content; `InteractionPanelController.cs:55` writes an action's label, not a literal; the display name "Name (Role)" with its "Traveler" fallback, the "Subject #n" fallback name and the registry note are generated identity and record data and stay (O1(c)); Home keeps English apart from the wallet (R9, O1(b)). |
| 5 | Content-asset strings (field labels, book and document names, roles, news, endings, upgrades) | R2: content, English in v1; a later translation adds per-culture fields keyed by existing ids (F2). |
| 6 | `ShiftClock.Format`, numeric readouts, numerals | Z6: unchanged; not culture surfaces; `BirthDates` excluded. |
| 7 | `OfficeReadouts` rewrites the lamp colour every frame | The booth is not themed (Z3), so nothing fights it. |
| 8 | `HoverHighlighter` reads the tracked settings asset live | The rings come from the active theme (§2.7); no ScriptableObject is written at runtime. |
| 9 | `InteractionFeedbackBootstrap` precedent | R1: `CultureThemeBootstrap` follows it. |
| 10 | The static LiberationSans atlas holds 250 characters; Greek would dirty the tracked fallback | R5: runtime LiberationSans from the TTF (Greece's font); the tracked assets are never chained; §6 checks no new code point reaches the tracked fallback. |
| 11 | OS font path route (`Font.GetPathsToOSFonts`, path overload) | R5: `FontCandidate.file`/`face`, tried first. |
| 12 | UI Toolkit's Advanced text generator for Arabic | Rejected for v1: it applies only to UI Toolkit panels, and the desk is uGUI/TMP (§4 F11). |
| 13 | `ITextPreprocessor` is lost on clones | Z9/R6: shaping in the string lookup. |
| 14 | Save and determinism details: (a) `IsActiveOnDay` has no lower bound; (b) old saves; (c) `DevToolsState` resets on Continue; (d) PlayerPrefs is new | (a) harmless: cues are read only at scene load and the emission step replaces its own entries (§7); (b) Z5; (c) no piece-6 state lives in `DevToolsState` (the leader is in the saved history, and piece 5's force override is cleared on Continue by design); (d) U12, §2.6, and §6 restores the key after the smoke. |
| 15 | Generator specifics: JsonUtility arrays; generated effects vs hand-authored ones | Arrays everywhere (§2.11). Piece 6 generates no effects (the cue effects are piece 5's, C2); its owned folder is `Assets/Data/World/Culture`, and FEATURES.md:95 says so (§3.3). |
| 16 | Tests that encode English | The `DiscrepancyLogTests` rows that assert the English report sentence move to `ReportKey`/`ReportOther` assertions (the sentence becomes table data, checked by the generator's key check and the Unity smoke); `BirthDatesTests`, `OriginLabelsTests`, `ShiftClockTests` unchanged; guard tests: `UiStrings` passes canonical arguments verbatim and the shaper keeps Latin runs in order (§5). |
| 17 | FEATURES.md lines not in the update list | §3.3 covers :9 (unchanged, save stays 2), :18, :20, :21-22, :28-29, :33, :36, :54, :61-63, :76-77, :91, :95, :97, :101. |
| 18 | Endings vs the present culture | Z7, written down in §1.1 and in a FEATURES bullet (§3.3). |
| 19 | Baked text in planned art; Codex's ArtLibrary loader | U13/Z8: `docs/UI_ART_CONTRACT.md` states text-free art and the path conventions; a note for Saleh to pass to Codex (§7). The mastheads, "< Office" and READY stay text in code (READY has no string today; the sign's art must stay text-free). |

## 3. Retired or superseded

### 3.1 Code removed

- `OfficeSceneUIBuilder.cs`: the XP constants (25-36, including the unused `PanelNavy`), the inline UI colour literals and label literals (§2.10), `EnsureWallpaper`/`Clouds`/`Blob` (757-818; the painter moves to Visuals `CulturePlaceholders`), `WritePlaceholderPng` (836-853) and `EnsureFolderTree` (1497-1506) (moved to `PlaceholderPng`), the Directives colour override (137-139), the document page-text colour override (346), the post-hoc style lines (155, 185, 398, 693, 712, 1341, 1364), and the "Settings (empty for now)." body (1290). `CursorPixel` stays (piece 4 moves only `InPolygon`).
- `CompareController.cs`: the evidence-less `Select` overload (57-59). (Piece 4 removes the private `ValuesMatch`.)
- Domain: `Discrepancy.Summary` (`DiscrepancyLog.cs`), `ClueLabels.Report` (piece 3's `ClueLabels.cs`) and `Looks.SlotLabel` (piece 4's `Looks.cs`), each replaced by a key chosen in Domain (R2).
- `InvestigationUIController.cs`: the fallback's literal colours (472, 480, 482, 513).
- `GameManager.cs`: the ✅/❌ emoji (418, 494).
- Every English literal listed in §2.8 (moved into `world_source.json`).

### 3.2 Earlier spec lines superseded (kept as approved records, not edited)

- `2026-09-24-cursor-hover-shift-clock-design.md` §1 line 13 ("UI buttons and icons get an XP-style amber outline while hovered") and line 23 (the settings asset holds "the … UI outline colours"): themed UI takes its theme's two rings (R7); the settings asset's UI outline colour remains only for untagged UI (Title, Home).
- `2026-06-21-office-scene-two-states-design.md` §8 line 165 (`TimelineReactiveSprite` "channel = `Visuals`"): the booth poster listens on `UI` for culture cues.
- `2026-09-24-identity-lies-design.md` §4 lines 509-514 (the piece-6 bucket): "UI language and colour changes" are this piece; per-traveller report lines, the redirect outcome and place picker, the true home on the citation slip and the fate of the legacy era-pick path are follow-ups F8 and F10 (§4), and so is the "Name (Role)" banner gotcha of line 625.
- Piece 3's spec (`2026-09-24-dialog-questions-design.md`): O1 there ("UI language (piece 6)") is this piece. Its copy (§2.16 there) moves into `ui.strings` under §2.8's keys with the same English. Its R18 (ASCII-only authored interview text) stands: translations live in `ui.languages[]` and render only through runtime fonts. Its **R13** ("One label source": `ClueLabels.Report`) keeps one source, which becomes the `category.*` keys chosen by `ClueLabels.Key`; its §4 item "moving `ClueLabels` into tables" is done here. Its `DiscrepancyLogTests` rows that assert the report sentence move to key assertions (§5).
- Piece 4's draft spec (`2026-09-24-characters-design.md`): `Looks.SlotLabel` (its §2.4) becomes `SlotKey`; its C6 wording ("traveller wears") survives as the `deviation.*.wears` templates; its §2.20 items for piece 6 (the Visitor window is chrome, the photo and Costume Guide diegetic, the slot labels and "DRESS" are keys, `ValuesMatch` reused) are done here; its report-sentence test rows move to key assertions.
- Piece 5's draft spec (`2026-09-24-history-facts-design.md`): `HistoryService.RebuildLeaderEffect` becomes public (its §2.8 and §2.16 C3 anticipate this). Its §2.16 C7 marker meaning is R14's (no conflict remains). Its `ForceLeader` doc and §2.16 C4 mention piece 6's "apply culture now" scene reload; there is no such cheat (R20): the forced cue shows at the next scene load, normally the next day. Its R21 and §2.16 C5 (news saved as English text, keyed news deferred to piece 6's F2) stand as O1(a). Its decision file's H14 book-row marker is §2.7 here.
- `piece6_decisions.md`: U4/U7 (O1) and Z4 (O2) are departed from as flagged in §0; U8's "Greek uses LiberationSans" is kept (R5).

### 3.3 `docs/FEATURES.md` (same commits as the behaviour)

Line numbers at `efe385d`; pieces 3–5 edit this file first.
- :18 "< Office" button: add "(its label follows the present culture's language, with the English word underneath)".
- :20 "Timeline-reactive poster": "the booth poster shows the present culture's poster (cue `culture:<id>` on the UI channel); an unset default keeps its own art".
- :21-22 newsletters: "mastheads, titles and buttons follow the present culture's language (with English glosses on the buttons) and colours".
- :28 "XP-style wallpaper, taskbar …": "Desktop themed by the present culture (wallpaper, colours, fonts, flavour labels); the XP look is the neutral theme shown before any country leads the Future".
- :29 Start menu: "Settings: UI language (follow history / always English; remembered per player; applies at the next office load)".
- :33 placeholder apps: unchanged apart from icon labels following the culture.
- :36 Citizen Records: add "window frame and SEARCH follow the present culture; the record itself never changes".
- :54 compare bar: "MATCH/MISMATCH in the culture's language with the English word in brackets; colours per culture" (no test claim: the equality is piece 4's line).
- :55 Scanner: add "its report lines and category words come from the UI tables (the key choice tested: `DiscrepancyLogTests`)".
- :61 "Compare bar flips to a red …": "flips to a "DEVIATION LOGGED — …" verdict in the theme's mismatch colour (red on the neutral desk, slate under China), never a MATCH".
- :62 Accept/Deny: "colours and language per culture, English gloss, a fixed ✓/✗ glyph; Accept always left".
- :63 fallback text mode: add "(its labels and colours follow the present culture)".
- :76-77 hover: "themed UI: two rings from the theme, dark inside and light outside, at least 9:1 apart, so one of them stands out on any background (ring rule tested: `ContrastTests`); untagged UI (Title, Home): the settings' amber".
- :91 citation slip: add "its title and Acknowledge follow the present culture's language and its colours the theme; the penalty names the wallet currency".
- :95 generator: add "the culture themes and UI string tables (`Assets/Data/World/Culture`, owned), with a contrast check that aborts on failure; text-free placeholder culture art (and the neutral wallpaper) created only when missing".
- :97 builder: "idempotent, authoritative scene builder (text, buttons and colours included; every graphic carries a theme role)".
- :101 debug panel: add "the Timeline Inspector shows the present culture, theme, label language, font, wallet word and missing UI strings".
- New bullets:
  - "Present culture: from the morning after a country leads the Future (piece 5), the PC wears that country's theme; neutral before (cue choice tested: `CultureCueTests`)";
  - "The present culture and the run's ending are separate: endings come from attribute totals, so an ending can differ from the desk's look";
  - "UI strings: every UI string comes from `world_source.json` tables; 34 flavour labels translated per culture, the rest English; numbers formatted in the invariant culture; right-to-left Arabic shaped in the string layer, never broken inside a phrase (tested: `UiStringsTests`, `ArabicShaperTests`)";
  - "Fonts: installed OS fonts at runtime per culture, never bundled; a culture whose labels no font can draw falls back to English labels with a warning (the choice tested: `CultureChoiceTests`; fonts checked in Unity, not by the EditMode suite)";
  - "Contrast: every theme passes text 4.5:1, large text and glyphs 3:1 and the ring rule before it can be generated (tested: `ContrastTests`, `PaletteTests`)";
  - "Wallet: named after the present culture's Future currency (Credits when neutral), in the office and at Home (the choice tested: `CultureChoiceTests`; the sites checked in Unity)";
  - "Reference books mark rows whose value history changed with [revised] (rule tested: `HistoryTests`; marks tested: `FactTableTests`; the history hook in `FillFacts` checked in Unity)".

## 4. Out of scope (follow-ups)

- **F1 Home and Title theming** and their string keys (O1(b)): keying the Home runtime literals, role stamping in `HomeSceneBuilder`/`TitleSceneBuilder` (non-authoritative today) and rebuilding scenes Codex is editing on main.
- **F2 Full translation**: prose templates, content per-culture fields (asset names, archetype roles, rule descriptions, piece-3 interview and dialog lines and piece-5 history lines by their ids), keyed news in the tomorrow package (an additive save field of key + arguments, O1(a)), the display name as given name and role (O1(c)), and a font decision for diegetic surfaces.
- **F3 RTL layout mirroring** (taskbar, windows, alignment).
- **F4 Eastern Arabic numerals and 12-hour clocks** (Z6 says no).
- **F5 Culture chrome sprites** (taskbar, window frame, button art) as theme slots the builder assigns, once neutral greyscale art exists; this also stops rebuilds dropping hand-assigned sprites on window buttons, which `BuildWinControls` destroys and recreates (737-739).
- **F6 Culture blends** from piece 5's ranking (U1).
- **F7 Booth theming**: readouts, lamp colours, a keyed READY label, booth art (Z3).
- **F8 Per-traveller report lines** in the shift ledger (P).
- **F9 Document kits by the traveller's place** (main's untracked per-country document art), on the diegetic surfaces.
- **F10 Piece 6b** (identity-lies §4): redirect to the true home with a place picker, the true home on the citation slip, the legacy era-pick path (`GameManager.cs:379, 398-466`, `OfficeUIController.ShowCase`, whose two literals stay unkeyed until then, and its scene leftovers `Canvas/Office UI Controller/{VisitorText, Doc1Text, Doc2Text, ResultText, EraButtonsRoot}` and the inactive `Canvas/HUD`), and the "Name (Role)" banner versus bare-name records.
- **F11 Bundled OFL fonts** (for example Noto) for platforms without the listed fonts, which needs Saleh's approval to download; and UI Toolkit's Advanced text as an Arabic alternative.
- **F12 Culture-aware endings** (Z7).

## 5. Tests (EditMode, `Assets/Tests/EditMode`)

Offline runner limits apply (`[Test]`, `[TestCase]`, `[SetUp]`). Fixtures use arbitrary role, key and culture names, never live content ids (the PR #3 lesson), except the golden Arabic words, which are text, not ids.

- **`RgbaTests`**: `#ECE9D8` parses to 236/233/216 over 255 with alpha 1; `#FFFFFFB3` gives alpha 179/255; lower case parses; missing `#`, wrong length, a non-hex digit and null return false.
- **`ContrastTests`**:
  - luminance: black 0, white 1, `#777777` ≈ 0.1845 (±0.0005);
  - ratio: white/black = 21 (±0.01), symmetric, a colour against itself = 1; white on `#2157DB` ≈ 6.085; white on `#3D993B` ≈ 3.609 (±0.005);
  - `Over`: alpha 1 gives the foreground, alpha 0 the background, alpha 0.5 the midpoint;
  - `Problems`: a text pair at 4.4:1 is reported with its name and both numbers; the same pair as `LargeText` passes at 3.0; `Glyph` and `Hint` use 3.0; `None` is never reported; a text fill with alpha 0.6 is reported; a translucent ink is composited over its fill before the check;
  - the ring rule: rings `#0B3A8C`/`#FFD76E` (7.6:1 apart) are reported with both numbers; `#092E70`/`#FFD76E` (9.3:1) pass; and, as a property row, for two rings 9:1 apart the better of the two reaches at least 3:1 against black, white, `#777777`, `#3B73BD`, `#F2E6C9`, `#123456` and the background at their geometric-mean luminance (the worst case).
- **`PaletteTests`**: a role takes its fill and ink seeds; the rule's alpha multiplies the fill; a seed missing from the culture falls back to the base seeds; an override replaces the fill only, the ink only, or both; an unknown seed name, an unknown role name, an override of an unknown role and bad hex each add one problem; a map rule for a diegetic role adds one problem; an override of a diegetic role adds one problem unless `allowDiegetic`; `Pairs` skips roles without a class or without both colours.
- **`UiStringsTests`**:
  - culture hit; culture miss falls back to reading; a miss in both returns the key, and `MissingKeys` lists it once after two lookups;
  - `Format` fills `{0}` and `{1}`; canonical arguments pass verbatim: "3 Jun 1450 BCE", "Babylon (Babili)", "Reichspräsident", "¥10,000"; a string argument ignores a specifier; null gives "";
  - number formats: `{0:+0;-0;0}` gives "+5", "-3" and "0"; `{0:0}%` with 87.4321 gives "87%"; `{0:+0.#;-0.#}` with -2.26 gives "-2.3"; with the thread culture set to de-DE (restored in `finally`), `{0:0.#}` with 2.5 still gives "2.5";
  - gloss `Below` gives "native\n<size=60%><noparse>ENGLISH</noparse></size>" with `glossPercent` 60 (a reading text "< Office" stays inside the `noparse`); `Inline` gives "native (ENGLISH)"; `None` gives the native text; a reading hit never glosses; a null culture table never glosses;
  - right to left: a culture entry is shaped (`"اليوم {0}"` with 3 gives U+0033 U+00A0 then the shaped word in visual order); a reading fallback is not shaped;
  - `Placeholders("Day {0} of {1:0.#}")` is ["0", "1:0.#"];
  - `TableProblems`: a duplicate key, a blank key, an unmatched brace, a culture key missing from the reading table, a culture key whose reading entry is `Full`, a placeholder mismatch (including `{0}` against `{0:0}`) and unshapeable right-to-left text are each reported; a clean table gives none.
- **`ArabicShaperTests`** (golden cases computed with `scratchpad/p6/shaper.py`, revised for R6):
  - "قبول" → U+FEDD U+FEEE U+FE92 U+FED7;
  - "رفض" → U+FEBE U+FED3 U+FEAD;
  - "لا" → U+FEFB;
  - "الأدلة" → U+FE94 U+FEDF U+FEA9 U+FEF7 U+FE8D (a lam-alef with hamza after a right-joining alef);
  - "إلى" → U+FEF0 U+FEDF U+FE87;
  - "الاستقرار: 87%" → "87%" U+00A0 ":" then U+FEAD U+FE8D U+FEAE U+FED8 U+FE98 U+FEB3 U+FEFB U+FE8D (the percent stays with its digits; the space inside the Arabic run becomes a no-break space);
  - "عدم تطابق" → U+FED6 U+FE91 U+FE8E U+FEC4 U+FE97 U+00A0 U+FEE1 U+FEAA U+FECB;
  - "سجل الأدلة" → U+FE94 U+FEDF U+FEA9 U+FEF7 U+FE8D U+00A0 U+FEDE U+FEA0 U+FEB3;
  - "(اليوم)" mirrors the brackets; "ACCEPT 1" (its space stays U+0020), "批准" and "" are returned unchanged; null gives "";
  - `CanShape` is false for "ٹ" (U+0679) and names it; true for every golden word.
- **`ThemeRolesTests`** (new): `IsDiegetic` is false for every role 0–35 and true for 36–44 (`[TestCase]` rows at both edges and one per diegetic role).
- **`CultureChoiceTests`** (new):
  - `Language`: ("el", "en", table, not always English, covered) → `Culture`; ("en", "en", …) → `SameAsReading`; ("EN", "en", …) → not `SameAsReading` (ordinal); no table → `NoTable`; always English → `EnglishBySetting`, also when not covered; not covered → `EnglishNoFont`;
  - `Wallet`: ("c", "Digital yuan (e-CNY)", "Credits") → the currency, unchanged; a null or blank culture id → the fallback; a blank currency → the fallback;
  - `ComposeStyle`: base 1|2 with italic flag 2 and strip → 1 | add; without strip the italic stays; add 32 is OR-ed in; base 0 with add 0 → 0.
- **`CulturePlaceholdersTests`** (new): each output is width × height × 4 bytes, fully opaque and identical on a second call; the wallpaper's bottom-left pixel equals `groundLow`, the pixel under the second cloud's centre (0.6, 0.9) is lighter than the sky pixel at (0.95, 0.9), and a top-row pixel at x 0.4 is within 2/255 of `skyTop`; the poster's centre pixel is the disc colour, a corner the border colour and a pixel between border and disc the field colour; a non-positive size throws `ArgumentException`.
- **`CultureCueTests`** (piece 5's file, rows added): `Pick`: empty or null → null with 0 matches; ["a", "culture:x"] → "x", 1; ["culture:x", "culture:y"] → "x", 2; a blank culture cue ("culture:") is skipped.
- **`HistoryTests`** (piece 5's file, rows added for `IsRevised`): null state → false; no edits → false; an edit for the place with another value → true; an edit equal to the base → false; a case-only or surrounding-space difference → false (`ValuesMatch`); an older edit that differs under a newest one equal to the base → false; the reverse → true; an edit for another place or another category → false; a blank edit value → false (`Resolve` ignores it). There is no day row: piece 5's `Resolve` has no day filter.
- **`FactTableTests`**: `MarkChanged` on an added cell is true and `IsChanged` then true; on a cell not in the table it is false and nothing is marked; other categories and places stay unmarked; null ids give false.
- **`DiscrepancyLogTests`** (changed, CRLF): `ReportKeyFor` rows: (ClaimMismatch, DocumentField) → "deviation.claimMismatch.papers"; (ForeignOrigin, Answer) → "deviation.foreignOrigin.said"; (RecordMismatch, Appearance) → "deviation.recordMismatch.wears"; (ClaimMismatch, None) → "deviation.claimMismatch.papers"; `ReportOther` is `actualOrigin` for ForeignOrigin and `expectedValue` otherwise. The rows of pieces 2–4 that assert the report sentence (for example piece 4's "Summary contains `traveller wears: \"`") assert `ReportKey` and `ReportOther` on the same proofs instead.
- **Piece 3's `ClueLabels` rows and piece 4's `Looks` rows** (wherever the plan finds them): one `[TestCase]` per category for `ClueLabels.Key` ("category.Geography", …) and per slot for `Looks.SlotKey`, replacing the word rows (the words are table data now).
- **Unchanged and green**: every other existing test, including `BirthDatesTests`, `OriginLabelsTests`, `ShiftClockTests`, `CursorHotspotTests` and piece 4's `PixelShapesTests` (reused, not changed).

## 6. Verification plan

**Offline, after every change**: `compile_check.py` reports 0 errors on every project; the reflection runner passes every test.

**In the branch's own Unity 6000.4.11f1**, through temporary `_TimeDesk*` scripts (not committed; reports and screenshots to the scratchpad):

1. **Spike, before the runtime code** (decides U9 and R5 in practice). A play-mode script builds a temporary canvas and, for each culture, resolves its font chain with the §2.6 algorithm, logs which candidate won (file or family), creates the runtime LiberationSans asset, renders all 34 flavour strings (Arabic through `ArabicShaper`), records `HasCharacters` and captures a screenshot. Pass: every chain resolves on this machine; Arabic letters join and read right to left in the screenshot (checked by eye, and by a native reader when Saleh can arrange one); Greek, CJK and German render with no missing glyph; a wrapped test text with an Arabic phrase breaks only outside the phrase. If Arabic fails, the `ar-Latn` fallback of R6 is authored instead, and the spec's review notes record it.
2. **Baseline dump** at the pre-builder commit: every `Graphic` under the desktop canvas and `OfficeOverlayCanvas` (path, type, colour, sprite name; for TMP: text, size, style, alignment, font) to a file.
3. **Content**: Generate World twice; the second run changes no file and creates no art. Validate Content Library reports no issue. The 9 themes, 7 tables and 16 placeholder images exist, `xp_bliss.png` is untouched, and `ContentLibrary_Main` lists them.
4. **Builder**: Build Office UI; no untagged-graphic error (`InvestigationRoot` included); the dump after the build equals the baseline within 0.0025 per colour channel, except the intended changes (§1.7 and §2.12's builder rows): the glyph bars and the label anchors next to them, the new `VerdictStrip`, no-wrap and auto-size on keyed labels and on `MoneyText`, `VerdictText` and the row labels, the book window's Prev/Next and the shelf template face (`#E6E6EB` → `#ECE9D8`), the Acknowledge face and label size (`#F2F2F2`/26 → `#ECE9D8`/22), the book window title size (20 → 15), `CompareText`'s baked colour, the Settings window content, and the removed "No deviations documented." sample. Accept `#296B38`, Deny `#752929`, the book rows `#FFFFFF0A`, the desk dim `#0F121A8C` and `CitationText` at 26 pt are unchanged. Build again: no scene change. Commit the scene.
5. **EditMode suite** through `TestRunnerApi`: all pass except the known third-party `UnitySkills…SceneSummarize_CountsObjectsCorrectly` (reported).
6. **Play smoke per culture** (neutral + 8): the temporary automation starts a New Run from Title with a fixed seed, calls `HistoryService.ForceLeader(world, lib, id)` (skipped for neutral; it emits today's cue itself, C4) and loads the Office.
   - At the briefing (booth view, first rendered frame): masthead text, colour and font name read back and compared with the theme; screenshot.
   - Start the shift, READY, focus the monitor: read back the Taskbar, TitleBar, WindowBody, Accept and Deny colours; Accept's label (native + gloss, or English for Britain and neutral); the glyph bars' colours; the wallpaper sprite; the poster sprite; `CompareController`'s four colours; a hovered Accept's `HoverUIOutline` (two rings, `ringDark` inside and `ringLight` outside); the tray money text starts with the expected currency; the verdict strip is active while a verdict shows; a scanner line and the DEVIATION LOGGED line read as the English template filled with the category word; `HasCharacters` of every culture string on the resolved font; `UiText`'s missing-key list is empty after visiting the briefing, a case, the compare bar (a MATCH and a MISMATCH), the scanner, Records (idle and a miss), Settings, a citation and the ledger. Screenshot of the desktop.
   - Egypt also: screenshots of the Arabic briefing and of a citation slip (the Arabic title inside the multi-line slip).
   - Italy (the longest Future currency, "Mediterranean lira (eLira)", 26 characters) also: screenshots of the tray, a verdict line, the ledger and, after Go Home, the Home money text and shop prices; nothing wraps or clips. A forced history edit (step 11's dev path) puts "[revised]" on a long place label in the same run.
   - Evidence unchanged: for the first two travellers, every document value, book row value and record value equals the neutral run's (same seed).
   - The tracked `LiberationSans SDF - Fallback.asset` gains no code point from any culture table (its character table before and after the run is compared; pre-existing churn such as U+2190 and U+25CF is ignored).
   - No errors in the log; the only warnings are the expected ones (placeholder art already exists, so none).
7. **Always English**: the automation first reads the `TimeDesk.UiLanguage` PlayerPrefs value (or notes that the key is absent), sets "english" for China and Egypt, checks that labels are English and colours and wallpaper are the culture's, and in a `finally` block restores the old value or deletes the key.
8. **Continue**: a save whose history names a leader and whose culture cue entry was removed; Title → Continue → the cue entry for today exists once (twice Continue still once), and the Office (or Home, then the next Office) is themed.
9. **Direct play**: with that save, OfficeScene played directly shows the themed briefing on its first frame (the `RefreshActive` path).
10. **Missing font**: with a culture's candidates temporarily renamed to non-existent families (in the temp script, on a runtime copy of the theme), the desk shows English labels in that culture's colours and logs the warning naming the culture and the missing characters.
11. **Revised marker**: a piece-5 history edit on a place in today's plan (its dev path) shows "[revised]" on that book row's label, and the row's value and compare evidence are unchanged.
12. **Hygiene**: revert Unity-touched unrelated files (`*.csproj`, `ProjectSettings/*`, the TMP fallback asset); keep the rebuilt `OfficeScene.unity`; delete the `_TimeDesk*` files and metas; check that `TimeDesk.UiLanguage` holds its value from before step 7 (or is absent); commit the generated assets and art. Send the screenshots to Saleh for the by-eye check (and the Arabic ones for a native reader).

## 7. Risks

- **Pieces 3–5's final names.** This spec was drafted beside pieces 4 and 5 and before piece 3 was implemented. The binding tables (§2.2) are the plan's first task. The one allowed adaptation inside piece 5 is making `RebuildLeaderEffect` public; the additions to pieces 3–5's Domain files (`Pick`, `IsRevised`, `Key`, `SlotKey`, `ReportKey`) are small and listed.
- **Report wording.** The nine deviation templates must be copied from the implemented `Summary` (pieces 3 and 4 reword it) before `Summary` is deleted. The generator's key check fails loudly on a missing template, and the smoke reads a scanner line.
- **Fonts on other machines.** Only the Windows fonts were probed. A player without them sees English labels in the culture's colours, with a warning. macOS and Linux names in the chains are unverified. Bundling an OFL font would fix it and needs Saleh's approval (F11).
- **Font licensing.** OS fonts are read at runtime and never saved; runtime font assets are never written to disk (they are not assets, and the editor never saves them). Legal confirmation is still pending, as for any use of OS fonts.
- **Arabic correctness.** The shaper is tested against a reference model written by the same author; a native reader should check the spike screenshots. Numbers and Latin inside Arabic follow simplified bidi rules; flavour templates take only numbers, which keeps the cases simple.
- **Translation quality.** The 34 × 6 labels are Claude's drafts and need native review before release. Wrong words are data fixes.
- **Layout overflow.** Longer words and glosses rely on auto-size down to 55%; Future currency names (up to 26 characters) and the revised marker rely on the same policy (R21). Home's layout is not builder-owned, so `UiText.FitLabel` covers its money text and prices. The Italy and Egypt screenshots are the check. Bodies are English and unchanged.
- **Scene merge with Codex.** Rebuilding OfficeScene rewrites many objects (tags, colours) while Codex holds uncommitted OfficeScene edits on main; the merge will need a rebuild after Codex's work lands. Codex's coloured chrome sprites will be tinted by the theme until re-exported as neutral greyscale (`docs/UI_ART_CONTRACT.md`), and the builder still recreates window buttons, dropping hand-assigned sprites on them (F5). Saleh should tell Codex about the art contract and the two path conventions before the ArtLibrary loader is built.
- **The neutral look moves a little, on purpose.** §1.7 lists every visible change: the two hover rings, the verdict strip, the glyphs, three button faces and two text sizes. Hex rounding shifts other colours by at most 0.002 per channel (invisible).
- **The rings are a new look.** The outer ring reaches 4 px beyond a hovered control (twice the 2 px outline distance) and can overlap a neighbour by that much while hovered.
- **Placeholder art in LFS.** 16 generated PNGs are committed (R11), unlike piece 4's runtime placeholders; they are replaced in place by final art.
- **`IsActiveOnDay` has no lower bound** (`WorldState.cs:257-258`): an effect added for tomorrow is active at once. Harmless here, because cues are read only at scene load and piece 5's step replaces its own entries; a future mid-day reader would see tomorrow's culture early.
- **`AfterSceneLoad` ordering.** The first scene is themed by the bootstrap's own refresh, relying on `AfterSceneLoad` running before the first `Start`; `GameManager.Start`'s explicit refresh covers the Office either way, and §6 step 9 checks it.
- **Runtime font memory.** Up to one OS font asset per culture visited plus the Latin fallback live for the session; `Dispose` frees them. A CJK atlas can grow with glyphs; the flavour set is small.
- **PlayerPrefs** is a new persistence mechanism (the Windows registry in the editor); it holds one string, and the smoke restores it (§6 steps 7 and 12).
- **Stacked branches.** Piece 6 sits on four unmerged pieces; FEATURES, the builder and `world_source.json` will conflict on rebase and must be re-anchored, never resolved by dropping another piece's lines.

## Review notes

First draft (2026-09-24). While drafting:
- every code claim was checked at `efe385d` (`git show` of the files, not the working tree, which another agent was editing); the analysis's corrections were applied, including that one effect has exactly one channel (so the poster listens on `UI`), that `Discrepancy`'s parts are already public, that `CompareController.highlightColor` is never written by the builder, that the Title builder's early returns are at `TitleSceneBuilder.cs:136-139` (not 247-250), and that `PanelNavy` is unused;
- the nine palettes and the neutral one were checked against §1.5's thresholds with `scratchpad/p6/contrast.py`, and the Arabic labels and golden cases with `scratchpad/p6/shaper.py`.

### Independent review (2026-09-24), revision 1

Every finding was checked before it was applied: against the code and `OfficeScene.unity` at `efe385d` (read from the worktree's git, which is at `fbf4905`, code equal to `efe385d`), against piece 3's committed spec, and against the current drafts of pieces 4 and 5 (`scratchpad/specs/2026-09-24-characters-design.md`, `2026-09-24-history-facts-design.md`). The helper scripts `scratchpad/p6/contrast.py` and `shaper.py` were revised and re-run (backups in `scratchpad/rev6/`). RF1–RF32 follow the review's order; several overlap (RF1/RF4/RF5/RF24, RF2/RF22, RF7/RF19, RF8/RF18, RF10/RF20, RF6/RF17/RF28).

| # | Finding (short) | Verdict | Where |
|---|---|---|---|
| RF1 | The emission step is piece 5's private `HistoryService.RebuildLeaderEffect`, not in `TimelineService`; `ForceLeader` already emits today's cue | applied | §2.2 C3/C4 and the one adaptation (make it public), §2.7 `RunManager`, §2.14 (`HistoryService.cs`; `TimelineService.cs` unchanged), R20 (no reload cheat) |
| RF2 | `BuildFactTable` is gone (piece 5 R4); the hook belongs in `FillFacts`; marker meaning differs from piece 5 | applied; the meaning part was already resolved | Hook in `FillFacts`, which also serves `BuildWorldFacts` (harmless there): §2.2 C7, §2.7. Piece 5's current §2.16 C7 already states R14's meaning (its own review's F5 removed the `sinceDay` wording and the "translates the templates" item), so one meaning is recorded in both specs; §3.2 notes it, and piece 5's R21/C5 keyed news is O1(a)/F2 |
| RF3 | The wallet lookup is `BuildWorldFacts(...).Get(leader, FutureEra.id, Currency)`; the extraction adaptation is unnecessary | applied | W, §2.2 C6 (adaptation deleted), §2.6 step 5 |
| RF4 | The `culture:` grammar has two editor writers | already satisfied by piece 5 | Piece 5's draft now owns `CultureCue` (`Format` in `MakeLeaderEffect`, `TryParse` in `CheckFuture`, its R20). Piece 6 drops its adaptation and adds only `Pick` (§2.2 C2, §2.3, §2.14) |
| RF5 | The inspector repeats piece 5's leader and ranking lines | applied | §2.7 `DebugPanelController` |
| RF6 | Piece 4 already makes `ValuesMatch` public and rewrites `Refresh`; the "(tested: DiscrepancyLogTests)" claim on :54 has no backing | applied | R15 now records piece 4's work; only the caller-less evidence-less `Select` overload is removed here; `DiscrepancyLog.cs` changes only for the report keys; `ValuesMatch`'s doc is left as pieces 4/5 write it; the :54 claim is dropped (§3.3). No `ValuesMatch` table is added: piece 6 claims nothing about it, and `HistoryTests` pins the rows piece 6 relies on (case and surrounding spaces) |
| RF7 | There is no shared editor texture helper (piece 4 R9); `CursorPixel` stays; `InPolygon` moves to `PixelShapes` | applied | New `Assets/Editor/PlaceholderPng.cs` (the PNG helpers moved from the builder); painters in Visuals (RF19); `CursorPixel` left; §2.1, §2.10, §2.14, §2.16, §3.1 |
| RF8 | The committed `InvestigationRoot` dim (`#0F121A8C`) is untagged, and the outline under it fails 3:1 | applied | R19 (`DeskDim` role, builder-owned), §2.5, §2.10; the outline problem is removed by the two rings (RF18) |
| RF9 | The committed scene has drifted from the builder's literals, so "exactly today's look" and the 0.0025 baseline were false | applied | Decided per object in the §2.12 table (the scene wins for Accept, Deny, book rows, `CitationText`, the dim; the builder wins for three button faces, two sizes and `CompareText`); §1.1, new §1.7, §6 step 4 and §7 list the intended changes |
| RF10 | `case.claimLine` duplicates piece 3's `interview.claim` content | applied | Row deleted (§2.8); `CaseFactory.cs` is not changed (§2.14) |
| RF11 | `document.photo` and the Dialect copy are stale after pieces 3–4 | applied | §2.8 (key dropped, piece 3's Dialect copy), §2.5 row 37, and a general "the plan takes the implemented text" note |
| RF12 | Piece 4's figure layers and highlight overlays would be tinted by a role | applied | `DiegeticArt` (`#FFFFFF`) and `DiegeticOverlay` (`#FFFFFF00`), §2.5, §2.10, R4 |
| RF13 | `CheckCulture` belongs in `ValidateLibrary`, returning an issue count | applied | §2.11 |
| RF14 | Home prices still say "cr" (`HomeUIController.cs:307-308`) | applied | R9, R13 (`wallet.creditsShort`), §2.8 |
| RF15 | One "Credits" key breaks the lower-case sentences | applied | Three wallet forms (R13, `UiText.Currency(WalletForm)`), §2.8 |
| RF16 | `ThemeTag.Configure` is called at runtime by the fallback | applied | §2.5 doc, §2.8 fallback row |
| RF17 | FEATURES edits missing for :101, :18, :63, :91 and the Z7 bullet; the marker's test claim overstated | applied | §3.3 (:18, :63, :91, :101 edits, a Z7 bullet, the marker bullet scoped to `HistoryTests` + `FactTableTests` with the hook checked in Unity) |
| RF18 | Wallpaper-backed outlines fail 3:1 against the painted wallpaper (verified: 1.1–1.7:1 for six cultures); final art and figure art are never checked | applied in a different form | Two rings at least 9:1 apart (R7, §2.4 `Contrast.Problems` with the proof): one ring reaches 3:1 on any background, so surfaces, `PickOutline`, per-tag outlines and the surface resolution are removed, and final art, the dim and figure art are covered. Ring seeds changed for neutral, Egypt, Iraq and Greece (§2.12). Rejected parts: checking every wallpaper colour (neither single outline passes on both a dark sky and a light cloud) and sampling final-art PNGs in the validator, both unnecessary with rings; the "Selectable on a non-surface role" error, since no surface roles remain |
| RF19 | Piece 6 must own the painter extraction; painters should be pure and tested; use `SerializedArrays`; add a piece-4 binding step | applied | Visuals `CulturePlaceholders` (tested, `PixelShapes.InEllipse` for the disc), editor `PlaceholderPng`, `SerializedArrays.Set` in `WireLibrary`, the pieces 3/4 binding list in §2.2. The glyph painter is gone: the glyphs are two plain bars (R8), so no glyph PNGs and no `InPolygon` reuse are needed |
| RF20 | `case.claimLine` duplicates piece 3's content; composing "Name (Role)" through UI strings puts presentation into generation | applied | Rows deleted (§2.8); `CaseFactory` keeps composing the display name as data and reads no UI table (O1(c), §2.14) |
| RF21 | R2 kept Domain sentences (`Summary`, `ClueLabels.Report`) against U4/U7 without saying so | applied, both options | The report and the category and slot words become keys chosen in Domain (`ReportKeyFor`, `ClueLabels.Key`, `Looks.SlotKey`; R2, §2.3, §2.8, §5); `Summary`, `Report` and `SlotLabel` are removed. The remaining exceptions (saved news lines, Home and Title, the generated display name, logs) are an explicit departure, O1, each with its later cost |
| RF22 | The revised rule sat in untested glue; its meaning must match piece 5's | applied | `History.IsRevised` (Domain, piece 5's file) with a decision table (§5), called by `FillFacts`; one meaning in both specs (see RF2). The "sinceDay in the future" row does not apply: piece 5's `Resolve` has no day filter (noted in §5) |
| RF23 | `VerdictText` was exempt from the contrast rule (white on a cloud 1.3–1.6:1, verified; 1.2:1 on the neutral desk) | applied | `VerdictStrip` role and graphic, shown only with text (R18, §2.5, §2.7 `OfficeUIController`, §2.10, §2.12); the exemption is gone |
| RF24 | The rebuild must be exposed; the reload cheat restarts the day and opens the Future early; inspector duplicates; `CultureCue` is piece 5's | applied | As RF1, RF4 and RF5; the cheat is dropped (R20); `Pick` and its rows are additions to piece 5's `CultureCue`/`CultureCueTests` (§2.3, §5) |
| RF25 | Format specifiers (`+0;-0;0`, `0`, `+0.#;-0.#`) were lost | applied | `{n:format}` in `UiStrings.Format` (R17, §2.4, tested in §5); every template in §2.8 shows its specifier; translations must keep them (`TableProblems`) |
| RF26 | Arabic flavour text lands in texts that wrap | applied | U+00A0 inside right-to-left runs (R6, §2.4, golden cases in §5; TMP's break rule verified at `TMP_Text.cs:4741`); Arabic briefing and citation screenshots (§6) |
| RF27 | Long Future currencies and the revised marker overflow | applied | R21 (no-wrap + auto-size on `MoneyText`, `VerdictText` and row labels; `UiText.FitLabel` at Home); Italy screenshots (§6). Rejected part: a short wallet name would be a second source for the currency (W binds the resolved fact) |
| RF28 | FEATURES :61 becomes false under China; :54's test claim | applied | §3.3 :61 ("the theme's mismatch colour") and :54. No `ValuesMatch` table is added: piece 6 claims nothing about it now (RF6), and `HistoryTests` pins the rows piece 6 relies on |
| RF29 | Several presentation rules lived only in untestable glue | applied | Visuals `ThemeRoles` (with `IsDiegetic`) and `CultureChoice` (`Language`, `Wallet`, `ComposeStyle`); `Palette.Resolve` rejects diegetic overrides (R22, §2.4, §5). Surface resolution no longer exists (RF18) |
| RF30 | Two art mechanisms (piece 4 runtime by key, piece 6 generate-time committed) | reason recorded | R11 and Z8: culture art follows the office convention (fixed path, asset references, replaced in place), because the poster must stay a `TimelineReactiveSprite` mapping of Sprite assets and there are 16 files; `docs/UI_ART_CONTRACT.md` documents both conventions (§2.14); the LFS cost is a risk (§7) |
| RF31 | Frame theming and Greece's Palatino departed from Z4 and U8 silently | applied | O2 records the frame theming as an explicit departure; the scanner backing becomes diegetic (`DiegeticBacking`); Greece uses the runtime LiberationSans (U8 kept, R5, §2.12) |
| RF32 | The smoke leaves `TimeDesk.UiLanguage` set in the registry | applied | §6 steps 7 and 12 |

Also corrected while verifying: the header now names pieces 4's and 5's draft specs; `HoverUIOutline.cs` (LF) and `DiscrepancyLogTests.cs` (CRLF) join the line-ending lists; `CaseFactory.cs` and `TimelineService.cs` move to "checked and not changed".

The plan's review should append its findings here.
