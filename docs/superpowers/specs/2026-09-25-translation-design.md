# Translation upgrades, basic version: design (piece 9)

*2026-09-25 · decisions made by Claude under Saleh's instruction "dont stop until you finish everything", open to his review · spec only (no code, no commits); to be built on `feat/translation` from `main` once piece 6 (`feat/ui-reacts`) and the office move (`feat/office-move`, which carries pieces 4, 5 and 8) have merged*

Saleh (item 8 of the physical-desk list): "there are different upgrades to translate different languages two types written documents and dialogue. when the translation happens letters will flip one by one we will flesh it out later."

This piece is the basic, extensible version. Every place has a tongue. From day 2 a traveller's papers and speech arrive in their claimed place's tongue, drawn in that tongue's script (hieroglyphs, cuneiform, Arabic, Greek, Chinese, kana, runes, or foreign Latin-letter words). The player can buy translators at Home: one pack per region, each sold as two upgrades, **Papers** (written documents) and **Speech** (dialogue). With a translator, the text translates at the moment it is revealed (the scan for papers, the speech bubble for speech), and its letters flip one by one from the foreign glyphs to English. Evidence does not change: every comparison still runs on the canonical English value, so an untranslated field can still be compared with the books and a liar can always be proven without any translator. A translator lets the player read. It does not decide what can be proven.

**Base and names.** This spec builds on the implemented names of the pieces it touches:
- piece 7's display seam `DisplayText.For(canonical, TextMedium)` and its two reveal points, `InvestigationUIController.OpenDocumentWindow` (scan finished) and the traveller's lines handed to `TravellerWheel.Say` (physical-desk spec R19, §2.21);
- piece 8's `SpeechQueue`, `TravellerWheel.Say(IReadOnlyList<DialogLine>)`, `InterviewScript.SaidSince` and `OverlayCallout.Reveal`;
- piece 6's `RuntimeFonts`, `FontCandidate`, `ArabicShaper`, `UiText`/`ui.strings`, `CultureThemeService`, `SettingsWindowController` and `UiLanguagePreference` (at `3379996` on `feat/ui-reacts`);
- piece 3's `GateSnapshot`, `TimelineService.BuildInterviewDay`/`Snapshot`, `Gates.UnlockNight`, the generator's unlock-trigger pattern (`MakeUnlockTrigger`, `DayGate`), `DiscrepancyLog.Prove`, `CompareEvidence.MatchValue` (piece 4);
- the office move's scene pair (`OfficeScenes`: the art `OfficeScene` plus the additive `OfficeGameplay` built by Build Office UI, at `e8b8290` on `feat/office-move`).

Line numbers refer to those commits. The office move still has uncommitted edits to `TravellerWheel`, `OverlayCallout`, `DeskController` and more, so the piece-9 plan re-reads every file before anchoring an edit, and the implemented code wins over this text. Nothing here depends on the builder-built 2D booth.

**Re-check before building (2026-09-25, `main` at `d998ab5`, where pieces 4, 5, 6, 8 and the office move have merged).** Every name above exists as written: `DisplayText.For(string, TextMedium)` with its three callers (`DocumentWindowController.cs:144`, `TranscriptWindowController.cs:46`, `TravellerWheel.cs:190`); `SpeechQueue.Say(text, expression)`; `TravellerWheel.Say(IReadOnlyList<DialogLine>)`, fed only traveller lines by `InterviewScript.SaidSince`; `OverlayCallout.Show`/`Reveal`; `RuntimeFonts.Resolve(ThemeSO, string)`; `CultureThemeService` (its `_fonts` made once in `Configure`); `SettingsWindowController` with the language pair; `UiLanguagePreference`; `Gates.UnlockNight`, `MakeUnlockTrigger`, `DayGate`; `OfficeScenes`. The line numbers below are updated to `d998ab5`. What the re-check changed:
- **The Home shop.** `HomeManager.HandleBuyUpgrade` refreshes the shop by calling `ShowShop` again, so the page resets only when the shop panel opens (it was hidden); the re-show after a purchase keeps the page. The shop's rows container does not control its children's heights (`HomeSceneBuilder`, `childControlHeight` off), so each runtime row keeps a new `RectTransform`'s 100 px height and its 44 px `LayoutElement` is ignored: four upgrades already overflow the 360 px area. `CreateRow` and `CreateLabelRow` now size their row to that height, so seven rows fit (§2.5).
- **Generated library entries.** The generator's `HandAuthored` keeps every library entry outside the Interview and History folders; the Translation folder joins that exclusion, so the translators and the notice are re-appended on every run, never kept as hand-authored (§2.7).
- **The Settings window** is 580 × 400 (`BuildOSWindow`'s default); the note with its extra sentence would not fit a 16%-high box, so the window grows to 580 × 520 and its rows are re-anchored (§2.6).
- **The office move.** The scan wakes the PC (`BoothCoordinator.HandleScanFinished` → `PcScreen.Wake`) but does not open the PC frame: the desktop, and with it the flip, shows on the office PC's cloned screen, and in the frame when that is open. The reveal point stays the scan (§7 updated).
- **R6 detail.** Setting a TMP text's font resets its material to the font's default, so `TextFlip` restores the text's own font *and* material.
- **Script fonts.** `RuntimeFonts` caches one result per key, so a script's font is resolved once with a sample of every cell of every tongue written in that script (a sample of the first tongue alone would leave the others unchecked, and their letters could reach TMP's global fallback).
- **The glyph spike** (§6 step 3, run first; §6.1). Hieroglyphs, cuneiform and runes draw from Segoe UI Historic through the OS-font path, supplementary-plane cells included; no tongue needs the fallback cipher on this machine. The spike also found a piece-6 bug: `ArabicShaper` gives a hamza that follows a dual-joining letter its final form, which does not exist (`'\0'`), and TMP stops reading the text at that character. The Arabic table maps `y` to hamza, so "by" hit it. `ArabicShaper` now joins a letter to the previous one only when the letter itself joins (tested: `ArabicShaperTests`).

## 0. Decisions

Claude made these under Saleh's instruction, working from the code named above; they bind this piece and are open to Saleh's review. The R rows below the table settle details the decisions leave open.

| Id | Decision | Rationale |
|---|---|---|
| T1 | **Every place has a tongue.** It is set by `places[].tongue` (required) in `world_source.json`. `translation.tongues[]` lists each tongue's display name, script, translator pack and glyph table, and `translation.scripts[]` lists each script's direction and font chain. **A traveller writes and speaks the tongue of their claimed place** (the disguise includes the language), never their true home's. A tongue with a blank pack is native: the clerk reads it (English). **The Future is not special**: each Future place names its tongue, and Britain's is English. | One id per place is the smallest piece of data that answers "which language". The Language fact ("Middle Egyptian, hieroglyphs") is free text that the books show; it must never be parsed. Taking the tongue from the claim keeps translation out of the lie model: it can never add, hide or move a tell, so the tell rules and draws of pieces 2–4 stay untouched. |
| T2 | **What is foreign.** On papers: the values of place-fact fields, meaning every category except Name and BirthDate (today Coin of Issue, Native Tongue, Declared Device and Bond Currency). In speech: every traveller line (claim, answers, small talk, spoken-request replies, premade and narrative lines), in the transcript and in the bubble. **Always readable:** field labels, document titles, the holder's name and date of birth, digits, the claim banner, desk lines, wheel labels, the reference books, Citizen Records, the Deviation Report, the compare bar and all PC chrome. The physical paper shows only its title and holder, so it does not change. | Labels and identity fields belong to the agency's bilingual form and its transliteration. Without them the player could not tell which book a field goes with, could not search Records (by typed name) and could not read dates (`BirthDates` month tokens), so the birth-date proof needs no translator. |
| T3 | **Untranslated text is drawn in a pseudo-script.** Each tongue has an authored substitution table of 26 cells. Letters a–z are mapped case-insensitively; Latin-1 letters are first folded to their base letter; a script with upper and lower case (Greek, Latin letters) keeps each letter's case. Digits, spaces, punctuation and symbols pass through unchanged. One table per tongue serves both papers and speech. The starter tables scramble the letters (vowels among vowels, consonants among consonants) for scripts that players can read. Hieroglyphs and cuneiform use sound signs instead. | The result is the same across runs and machines without a new hash or random stream, so `Seeds` does not change. Designers control the look. A value looks the same on the passport and in the answer, so piece 3's hint ("the answer disagrees with the papers") still works on untranslated text. Digits stay western (piece 6 Z6). The scramble stops a reader of Greek, Arabic, Chinese or Japanese from reading the English back out of the glyphs. |
| T4 | **Scripts and fonts reuse piece 6.** Arabic is shaped by `ArabicShaper.ToVisual`. Each script's font chain (OS fonts, never copied into the project) is resolved by piece 6's `RuntimeFonts` through a new keyed overload; `CultureThemeService` owns that one session-wide instance. Latin and Greek use the runtime LiberationSans. Han and Kana use piece 6's Chinese and Japanese chains. Hieroglyphs, cuneiform and runes use Segoe UI Historic (`seguihis.ttf`). When no font can draw a tongue's script, that tongue falls back to an ASCII fallback cipher in the text's own font, and one warning is logged. | Piece 6 (U8, U9, R5) already solved OS fonts, Arabic shaping and the tracked-atlas trap; a second font loader would duplicate that work. Checked on this machine with a cmap probe (scratchpad `p9_cmap.py`, `p9_tables.py`): `seguihis.ttf` covers 1071 hieroglyphs, 922 cuneiform signs and 89 runes, and each starter table is fully covered by its chain. |
| T5 | **Evidence is untouched (the canonical rule).** Every comparison still carries the canonical value in `CompareEvidence.value`. Untranslated statement rows stay compare-clickable. MATCH/MISMATCH and `DiscrepancyLog.Prove` decide on the canonical strings exactly as they do today. **The compare bar never prints pseudo-script**: an untranslated side reads "(untranslated Akkadian; Near East Translator)". When a deviation is logged, its line quotes the canonical values, so proving a tell also reveals what it said. | Without any translator, a liar stays provable on every channel: a paper or spoken tell against the claim's book row (ClaimMismatch), a birth-date tell against Records (readable, T2), and a dress tell (which is not text). No case can become unwinnable, and the deny gate keeps its meaning. The bar keeps its Latin UI font, so no foreign glyph can reach a tracked TMP asset. This departs from the physical-desk spec's expectation (§4 line 1057) of "untranslated rows not comparable" plus "a manual path"; see §3.2. |
| T6 | **Translators: one pack per region, each sold as two upgrades.** The packs are Near East (Egyptian, Akkadian, Arabic), Mediterranean (Greek, Latin, Italian), East Asia (Chinese, Japanese) and Northern Europe (Brittonic, Old English, Proto-Germanic, German). Each pack is sold as **Papers** (Written, 100 credits) and **Speech** (Spoken, 80 credits). That makes 8 generated `UpgradeSO`s with ids `tr_{pack}_written` and `tr_{pack}_spoken` (`Translation.UpgradeId`). They are bought in the Home shop and saved in `WorldState.unlockedUpgradeIds`: no new save field, and `SaveVersion` stays 2. A purchase takes effect from the next office day (the day-start snapshot, as for questions). | This is Saleh's "different upgrades ... two types". One pack per tongue would mean 24 upgrades. Regions pair up the eight countries, so the player knows which pack a claim needs ("Egypt → Near East"). Papers costs more because papers carry most of the tells every day. |
| T7 | **Translation starts on `translation.fromDay` (2).** Before that day, every tongue reads as English (the agency's interpreter pool). That day's morning paper announces the change through a generated one-shot trigger, using piece 3's unlock-notice pattern (`DayAtLeast Gates.UnlockNight(fromDay)`). | Day 1 teaches the loop on readable papers. The player then has a Home evening and a notice before they meet foreign text. It takes one knob and one notice, and no new mechanism. |
| T8 | **The flip.** Each letter of the foreign form turns into its English letter in reading order. On the way it passes briefly through `scrambleSteps` glyphs of its own tongue (split-flap style). Non-letters never flip, and each row of a document starts `rowStagger` after the one above. **Reveal points** (physical-desk R19): with the Papers translator, the scan (`OpenDocumentWindow`, the first time each document opens this case); with the Speech translator, a line starting in the bubble, whose hold then begins once the flip has finished. **Transcript rows show the settled text.** **Skip:** a click on a document row completes that whole document; opening the wheel completes the bubble's line. **Reduced motion** (a player preference in Settings) shows the settled text at the reveal. The knobs are `translation.flip` in the content library. The timing rule is pure (`FlipSequence`, Visuals). | Piece 7 chose these reveal points and the single display seam. The transcript is the record, not a reveal point. One pure timing rule drives both media and is tested headless. |
| T9 | **The Home shop pages.** With 12 upgrades the shop outgrows its 7 rows (a 360 px rows area at 50 px a row). At runtime, `HomeUIController` shows 6 upgrades per page plus a pager row ("Next >"). The paging maths moves to a pure `Paging` class (Visuals) that `PagedRowsWindow` shares. HomeScene is not rebuilt. | A ScrollRect would need a change to `HomeSceneBuilder` (which is not authoritative) and a HomeScene rebuild while Codex is editing that scene. The shared maths avoids a second copy of `PagedRowsWindow.PageCount`. |
| T10 | **Where rules live.** Domain holds the content types, the upgrade-id grammar, which categories and speakers use the tongue, and `TranslationDay` (is a tongue foreign today? is it translated? both read from the day-start `GateSnapshot`). Visuals holds `Pseudoscript`, `FlipSequence`, `DisplayText` (still the one place where displayed text may differ from the canonical value) and `Paging`. The glue only renders. | House rule: decisions live in tested assemblies. Domain cannot see Visuals, and Visuals cannot see Domain, so the presentation rules go in Visuals and the game rules in Domain. |
| T11 | **No new mechanics beyond reading.** There is no manual decoding, no time cost, no accent tell and no partial translation. | "Keep it basic; we will flesh it out later." Each of these is listed in §4 with the seam it would use. |
| T12 | **Verification happens in the art office.** Offline tests come first. Then: Generate World and the validator, a font probe, the EditMode suite, Build Office UI (which changes only the Settings window), and a scripted play-through in the art office (OfficeScene plus the additive OfficeGameplay). | The 2D booth is outdated; the office move's scene pair is the live office. |

### Refinements settled by this spec

| Id | Refinement | Rationale |
|---|---|---|
| R1 | **Table rules** (`Pseudoscript.ParseTable`, one home, called by the generator, the validator and the runtime): exactly 26 cells, where a cell is one character or one surrogate pair (a supplementary code point); all cells distinct; no whitespace. In a right-to-left script, every cell passes `ArabicShaper.CanShape`. The fallback table is 26 distinct lower-case ASCII letters (`Pseudoscript.IsAsciiLetters`). | A duplicate cell would make two different words look identical. A missing cell would leave an English letter visible. |
| R2 | **Folding:** àáâãäå→a, ç→c, èéêë→e, ìíîï→i, ñ→n, òóôõöø→o, ùúûü→u, ýÿ→y, ß→s, and the same for the upper-case letters. Everything else passes through unchanged (£, ¥, digits, punctuation). The starter values that contain such letters are "Frappé" and "Reichspräsident". | These letters would otherwise show as Latin letters inside Arabic or Han text. |
| R3 | **Case:** when the source letter is upper-case and its cell is a single BMP character, the cell's `char.ToUpperInvariant` is shown (Greek β→Β). This has no effect on scripts without case or on surrogate pairs. | Capitalised proper names still look capitalised in Greek and in the Latin-letter tongues. |
| R4 | **One cell per canonical character.** Canonical text is BMP only (authored text is ASCII, and fact values are Latin-1), so cell i always stands for canonical character i, and flipping cell i shows canonical character i. | This is what makes "letters flip one by one" well defined for every script. |
| R5 | **Right to left:** the composed logical string (foreign cells plus any English letters already flipped) goes through `ArabicShaper.ToVisual`, which keeps Latin runs in order and reverses and joins Arabic runs. A string that is entirely English comes back unchanged. The flip therefore starts at the right edge (where Arabic reading starts), and the finished line snaps to left-to-right English. | This reuses piece 6's shaper. The snap is accepted in the basic version (§7). |
| R6 | **Font per text:** while a text shows any foreign or scramble cell, its font is the script's runtime asset, whose fallback is the runtime LiberationSans, so the English letters also draw. Once the text settles, its own font is restored. Tracked TMP assets never receive a foreign glyph. Rows that paging re-creates start again from the template's font. | Piece 6 R5: the tracked static LiberationSans and its dynamic fallback must never grow. |
| R7 | **Reveal time per document:** `DocumentWindowController.Reveal()` records `Time.unscaledTime` on its first call for the case. Rows re-created by a page change work out their state from that time. Reopening the window never replays the flip. A click on a row moves the reveal to "finished". | The flip keeps going across paging and ends in the same state however the pages are browsed. |
| R8 | **Allocation:** a flipping text rebuilds its string only when `DisplayText.Progress` changes (at each change point: a cell starting, a scramble step, a cell landing). Strings are therefore built per visible change, not per frame. The `Update`/`LateUpdate` work runs only while a flip is running and does no `GetComponent` or `Find`. | MERGE_CRITERIA "Optimization": no per-frame allocations in hot paths. |
| R9 | **The bubble:** `SpeechQueue.Say(text, expression, revealSeconds)` queues the canonical line together with its flip duration. The line counts as fully shown at the later of typing and reveal. `LineSeconds` is the flip's clock, and `EndReveal()` implements the skip. Typing (`OverlayCallout.Reveal`, TMP `maxVisibleCharacters`) is unchanged. | The hold (4 s) must follow the flip, or a long line would be replaced before it finished translating. |
| R10 | **The compare bar's text:** document rows and answer rows pass `CompareController.Select` the canonical value when the row is readable, and the placeholder (R11) when it is untranslated. The evidence stays canonical, and `CompareEvidence.MatchValue` makes MATCH use it, exactly as piece 4 already does for garments (whose bar text is an item name). `DisplayText` output never reaches `CompareController` or `CompareEvidence` (physical-desk R19 still holds). | There are no foreign glyphs in the bar and no second match path. |
| R11 | **Placeholder:** the UI string key `compare.untranslated`, "(untranslated {0}; {1} Translator)", where {0} is the tongue's name and {1} the pack's. It goes in piece 6's English table only (Full tier, never flavour). | It tells the player what they are looking at and which pack reads it. |
| R12 | **Generated assets** live in `Assets/Data/World/Translation/` (owned and pruned): `Upgrade_Tr_{Pack}_{Papers,Speech}.asset` and `Trigger_TranslationNotice.asset`. `WireLibrary` writes the upgrades as the hand-authored ones (anything outside the owned folder), in their order, followed by the generated ones in pack order (Papers, then Speech). It appends the notice trigger after the unlock triggers. | This is the precedent the triggers already follow: hand-authored assets are kept and generated ones appended. |
| R13 | **Upgrade texts:** the display names are "{Pack} Translator: Papers" and "{Pack} Translator: Speech". The descriptions are "Translates {tongues} on scanned papers." and "Translates {tongues} in speech.", with the tongue names joined by commas and "and". The shop does not show descriptions today (`HomeUIController.BuildShopRows`). | The shop row names the region and the type; the description is ready for when the shop shows descriptions. |
| R14 | **Missing data:** if the library has no translation data, every tongue reads as English and one warning names Generate World. A case whose tongue is blank or unknown reads as native (the validator reports both as errors). The text fallback (no rich desk) always prints canonical text. | Fail loudly in the log, never in the frame. The fallback already switches off the evidence gate. |
| R15 | **`fromDay` is required and must be at least 1.** Source defaults never decide behaviour (piece 3 R27). With `fromDay` 1 there is no notice trigger, because there is no night before day 1. | This keeps piece 3's rule for gated questions. |
| R16 | **Premades** use the tongue of the place they claim, set by the same `CaseFactory` line. | One rule for every traveller. |
| R17 | **Motion preference:** `MotionPreference.Reduced` is stored in PlayerPrefs under `TimeDesk.ReducedMotion` ("full" by default, or "reduced") and is read each time a traveller is presented. The Settings window gets a "Motion: Full / Reduced" pair from the builder, next to piece 6's language pair. | It is a per-player accessibility choice, kept outside the run save like piece 6's `UiLanguagePreference`. |
| R18 | **Readability:** `TranslationDay.Foreign(tongue)` is true when the tongue is known, is not native, and the snapshot's day is at least `fromDay`. `Translated(tongue, kind)` is true when `Foreign` holds and the snapshot owns `UpgradeId(pack, kind)`. | Before `fromDay` nothing flips, because there is nothing to translate. The flip only ever shows a translator at work. |

## 1. Behaviour

### 1.1 Who speaks what

| Country | Ancient | Medieval | Early modern → Future |
|---|---|---|---|
| Egypt | Egyptian (hieroglyphs) | Arabic | Arabic |
| Iraq | Akkadian (cuneiform) | Arabic | Arabic |
| Greece | Greek | Greek | Greek |
| Italy | Latin | Italian | Italian |
| China | Chinese | Chinese | Chinese |
| Japan | Japanese (kana) | Japanese | Japanese |
| Britain | Brittonic | Old English | English (native: always readable) |
| Germany | Proto-Germanic (runes) | German | German |

- A traveller's papers and speech use the tongue of the place they **claim**. A liar from Babylonia claiming New Kingdom Egypt writes and speaks Egyptian, so the tongue never gives the lie away and never hides it.
- Italian, German, Latin, Brittonic and Old English use Latin letters, arranged into foreign-looking words.

### 1.2 When foreign text appears

- **Day 1:** everything is readable (the interpreter pool).
- **The day-2 morning paper** carries the notice: "Budget cuts close the interpreter pool. From today, foreign papers and speech reach the desk untranslated; translators are sold at Home. The scanner still compares untranslated text with the books."
- **From day 2**, a traveller whose tongue is not English arrives untranslated, unless the player owns that region's translator for the medium (Papers for documents, Speech for dialogue).

### 1.3 What stays readable

- Every document's title and field labels ("Coin of Issue", "Native Tongue", ...), the Full Name and the Date of Birth.
- Digits everywhere, including inside foreign values ("1897", "1000").
- The claim banner, desk lines, wheel labels and requests.
- Reference books, Citizen Records, the Deviation Report, the compare bar and all PC chrome. (Piece 6's culture labels are a separate feature: the PC's own language follows history, with English glosses.)

### 1.4 How untranslated text looks

- Each value and each traveller sentence is drawn in the tongue's script, one glyph per letter. "Silver shekel (by weight)" looks like this:
  - Egyptian: 𓋴𓇌𓃭𓎛𓇋𓂋 𓋴𓉔𓇋𓎡𓇋𓃭 (𓃀𓏭 𓏲𓇋𓇌𓎼𓉔𓏏)
  - Greek: Βηφδαέ βραταφ (λω ζαηπργ)
  - Chinese: 小一火下衣大 小天衣木衣火 (文由 口衣一月天上)
  - Latin: Wupzov wlonop (fe bouklx)
- Arabic reads right to left with joined letters. Numbers inside it read left to right.
- The same value always looks the same, in every run, on the papers and in speech. A player who notices that the passport's currency glyphs differ from the glyphs the traveller says has spotted a hint, just as with readable text. Only a comparison with a book proves it.
- If the player's computer has no font for a script, that tongue shows as scrambled Latin letters instead, and a warning names the script and the fonts that were tried.

### 1.5 Comparing untranslated text

- Untranslated document values and transcript answers stay clickable. In the compare bar an untranslated side reads, for example, `MISMATCH    Travel Passport · Coin of Issue:  (untranslated Egyptian; Near East Translator)    vs    Currency Ledger · New Kingdom Egypt (Ancient):  Deben (copper, by weight)`.
- MATCH and MISMATCH are decided on the original text, exactly as for readable text.
- A tell is logged as usual, and the log line quotes it in English: `●  DEVIATION LOGGED — CURRENCY INCORRECT — papers: "Silver shekel (by weight)"  /  expected: "Deben (copper, by weight)"`.
- Every liar can be proven with no translator at all:

| Tell | Proof without a translator |
|---|---|
| Paper tell (Currency, Language, Device) | the field against the claim's book row (ClaimMismatch) |
| Spoken tell (Currency, Language, Device, Capital, Ruler) | the answer row against the claim's book row |
| Birth-date tell | Date of Birth (readable) against the Records "Born" row |
| Dress tell | the garment against the Costume Guide (not text) |

- What a translator adds: the player can read values and sentences, clear an honest traveller at a glance instead of comparing every field, see where a foreign value really belongs (an origin proof), and follow premade and narrative conversations.

### 1.6 Translators and the Home shop

| Pack | Tongues | Papers | Speech |
|---|---|---|---|
| Near East | Egyptian, Akkadian, Arabic | 100 | 80 |
| Mediterranean | Greek, Latin, Italian | 100 | 80 |
| East Asia | Chinese, Japanese | 100 | 80 |
| Northern Europe | Brittonic, Old English, Proto-Germanic, German | 100 | 80 |

- The shop lists the four existing upgrades, then the eight translators ("Near East Translator: Papers — 100 cr"). With more than 6 upgrades it pages: 6 per page plus a "Page 1/2" row whose button, "Next >", turns the page (and wraps round). Buying an upgrade keeps the page.
- A translator bought at Home works from the next office day. Owned translators are saved with the run and survive Save + Continue. Shop discount effects apply to them like any upgrade.

### 1.7 The flip

- **Papers:** with the region's Papers translator, a scanned document's window opens showing the foreign values, then their letters flip into English one by one, each passing briefly through two glyphs of its script. Foreign rows start 0.15 s apart. A 20-letter value such as "Silver shekel (by weight)" finishes 1.18 s after the scan (0.3 s delay + 19 × 0.04 s + 0.12 s). A passport's two foreign rows (Coin of Issue, then Native Tongue "Middle Egyptian, hieroglyphs") finish in about 1.5 s.
  - A click on any row finishes the whole document at once (and selects the row for comparison, as usual).
  - Reopening the window, or scanning the paper again, shows English. Page 2 shows whatever state the flip has reached.
- **Speech:** with the region's Speech translator, each traveller line types into the bubble in the foreign script (at 40 characters per second, piece 8), and its letters flip into English behind the typing. The line's hold (4 s, or 1.5 s when another line waits) starts once the flip has finished. The arrival claim, "I request passage home to New Kingdom Egypt (Ancient).", has 43 letters and finishes after 2.10 s.
  - Clicking the traveller (opening the wheel) finishes the current line at once.
- **The transcript** shows English lines when the Speech translator is owned, and foreign lines when it is not. It never animates.
- **Settings → Motion: Reduced** shows translations at once: the English appears at the reveal, with no flipping. It is a per-player choice, kept like the UI language, and it applies from the next traveller.

### 1.8 Determinism and saves

- **Nothing random is added.** For the same run and day, the travellers, lies, papers and answers are identical, byte for byte, to what they were before this piece. The glyphs are a fixed function of the tongue and the text.
- **Saves:** translators are ordinary owned upgrades, and `SaveVersion` stays 2. The motion choice lives in PlayerPrefs, outside the run.
- **Old saves:** a save made on day 2 or later before this piece meets foreign text on Continue, and gets the notice in its next morning's paper (piece 3 has the same late-notice behaviour).

### 1.9 When something is missing

- **No translation data in the library:** everything is readable, and one warning names Generate World.
- **No font for a script:** that tongue uses the ASCII fallback cipher, with one warning per script (§1.4).
- **The text fallback** (no rich desk) prints canonical text.

## 2. Design

### 2.1 Where things live

- **TimeDesk.Domain** (`Assets/Scripts/Domain`, pure, EditMode-tested): new `TranslationContent.cs` and `Translation.cs`.
- **TimeDesk.Visuals** (`Assets/Scripts/Visuals`, engine-free, EditMode-tested): new `Pseudoscript.cs`, `FlipSequence.cs` and `Paging.cs`; changed `DisplayText.cs` and `SpeechQueue.cs`.
- **Assembly-CSharp:**
  - new folder `Assets/Scripts/UI/Translation/` with `TranslationSettings.cs`, `TranslationPresenter.cs`, `CaseTranslation.cs`, `TextFlip.cs` and `MotionPreference.cs`;
  - changed `ContentLibrarySO`, `Timeline/NationEraProfileSO`, `CaseInstance`, `CaseFactory`, `Timeline/TimelineService`, `GameManager`, `UI/InvestigationUIController`, `UI/DocumentWindowController`, `UI/TranscriptWindowController`, `UI/TravellerWheel`, `UI/OverlayCallout`, `UI/CompareController` (doc only), `UI/HomeUIController`, `UI/PagedRowsWindow`, and piece 6's `UI/Theme/RuntimeFonts`, `UI/Theme/CultureThemeService` and `UI/Theme/SettingsWindowController`.
- **Assembly-CSharp-Editor:** `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `OfficeSceneUIBuilder.cs` (Settings window only).
- **Content:**
  - `world_source.json` (a `translation` section, `places[].tongue`, and keys in piece 6's `ui.strings`);
  - the generated `Assets/Data/World/Translation/` and places;
  - `ContentLibrary_Main.asset`;
  - the rebuilt `OfficeGameplay.unity`.
- **Tests:** `Assets/Tests/EditMode`. The test assembly references Domain and Visuals only.

Line endings (the repository's, from `git ls-files --eol` at `e8b8290`):
- **CRLF:** `CaseFactory.cs`, `CaseInstance.cs`, `ContentLibrarySO.cs`, `GameManager.cs`.
- **LF:** every other file above, and new files.

The plan re-checks each file before editing, and uses `SCRATCH/subs.py`.

### 2.2 Domain

**`TranslationContent.cs`** (new) holds the serializable content types, like `InterviewContent.cs`:

```csharp
/// A place's tongue (world_source.json translation.tongues[]).
[Serializable] public sealed class Tongue
{
    public string id;          // "arabic"
    public string displayName; // "Arabic"
    public string script;      // a translation.scripts[] id
    public string pack;        // a translation.packs[] id; blank = native (the clerk reads it)
    public string glyphs;      // the 26-cell table for a..z (Pseudoscript.ParseTable); blank for a native tongue
    public bool Native => string.IsNullOrWhiteSpace(pack);
}

/// A translator pack: one region, sold as a Papers and a Speech upgrade.
[Serializable] public sealed class TranslatorPack { public string id; public string displayName; }

/// The translation rules the day reads (the content library's copy of world_source.json translation).
[Serializable] public sealed class TranslationRules
{
    public int fromDay;                         // first office day with foreign text (>= 1)
    public List<Tongue> tongues = new();
    public List<TranslatorPack> packs = new();
}
```

**`Translation.cs`** (new):

```csharp
/// Which translator a text needs: papers (documents) or speech (dialogue).
public enum TranslatorKind { Written, Spoken }

public static class Translation
{
    /// The upgrade id of a pack's translator: "tr_{pack}_written" / "tr_{pack}_spoken" (one grammar: the generator writes it, TranslationDay reads it).
    public static string UpgradeId(string packId, TranslatorKind kind);

    /// True for a document field printed in the place's tongue: every place fact; never Name or BirthDate (the agency's transliteration, T2).
    public static bool InTongue(ClueCategory category);

    /// True for a line spoken in the place's tongue: the traveller's (the desk speaks English).
    public static bool InTongue(DialogSpeaker speaker);

    /// Content problems (the generator and the validator both call it): null rules; fromDay < 1;
    /// a blank or duplicate tongue id; a blank display name; a script not in scriptIds;
    /// a pack that is neither blank nor a pack id; a non-native tongue without glyphs;
    /// a blank or duplicate pack id or name; a pack no tongue uses (its upgrades would buy nothing);
    /// a place whose tongue is blank or unknown. One message per problem, naming the ids.
    public static List<string> Problems(TranslationRules rules, IEnumerable<string> scriptIds,
                                        IEnumerable<KeyValuePair<string, string>> placeTongues);
}

/// Today's translation (R18): which tongues are foreign and which are translated, fixed at the start of the day.
public sealed class TranslationDay
{
    public TranslationDay(TranslationRules rules, GateSnapshot dayStart); // null rules = nothing foreign
    public Tongue TongueOf(string tongueId);                              // null for blank/unknown
    public TranslatorPack PackOf(Tongue tongue);                          // null for native/unknown
    public bool Foreign(string tongueId);                                 // known, not native, day >= fromDay
    public bool Translated(string tongueId, TranslatorKind kind);         // Foreign and the snapshot owns UpgradeId(pack, kind)
}
```

`TranslationDay` copies what it reads, so a later change to the rules object changes nothing (the `GateSnapshot` pattern).

### 2.3 Visuals

**`Pseudoscript.cs`** (new):
- `public const int TableSize = 26;`
- `public static IReadOnlyList<string> ParseTable(string glyphs, out string problem)`: splits the string into cells, where a surrogate pair is one cell. Returns null with a problem ("24 cells, need 26"; "cell 'κ' appears twice"; "whitespace in the table") when R1 fails.
- `public static int LetterIndex(char c)`: 0–25 for a–z and A–Z and for the R2 letters after folding; −1 for anything else.
- `public static string Cell(char c, IReadOnlyList<string> table)`: `table[LetterIndex(c)]`, upper-cased per R3; otherwise `c` itself.
- `public static bool IsAsciiLetters(IReadOnlyList<string> table)`: true when all 26 cells are distinct lower-case a–z (the fallback rule).

**`FlipSequence.cs`** (new):

```csharp
/// The flip's knobs (ContentLibrarySO.Translation.flip, authored in world_source.json translation.flip).
[Serializable] public sealed class FlipTiming
{
    public float startDelay = 0.3f;      // seconds from the reveal to the first letter
    public float letterInterval = 0.04f; // seconds between two letters starting
    public float letterSeconds = 0.12f;  // seconds a letter takes to land
    public int scrambleSteps = 2;        // glyphs a letter passes through (0 = straight to English)
    public float rowStagger = 0.15f;     // extra delay per document row
}

public enum CellState { Foreign, Flipping, English }

public static class FlipSequence
{
    /// When a letter starts: startDelay + row * rowStagger + rank * letterInterval (negative knobs count as 0).
    public static float StartOf(int letterRank, int row, FlipTiming t);
    /// A letter's state at elapsed seconds; step = its scramble step while Flipping (0..scrambleSteps-1).
    public static CellState StateAt(int letterRank, int row, FlipTiming t, float elapsed, out int step);
    /// The table index a flipping letter shows: (letter + 7 * (step + 1)) % 26 (7 is coprime with 26, so steps differ).
    public static int ScrambleIndex(int letter, int step);
    /// When the last letter lands (0 for no letters).
    public static float Duration(int letterCount, int row, FlipTiming t);
    /// How many change points (a letter starting, each scramble step, a letter landing) have passed; the output changes exactly when this does.
    public static int Progress(int letterCount, int row, FlipTiming t, float elapsed);
}
```

`letterRank` counts only the letters before a cell (`LetterIndex >= 0`). Cells that are not letters are the same in both forms and never take time.

**`DisplayText.cs`** (changed; still "the one place displayed text may differ from its canonical value"):

```csharp
/// One tongue's look: its parsed table and direction.
public sealed class ForeignText
{
    public ForeignText(IReadOnlyList<string> table, bool rightToLeft);
    public IReadOnlyList<string> Table { get; }
    public bool RightToLeft { get; }
}

public enum RevealKind { Plain, Untranslated, Flipping }

/// How a text shows: plain (canonical), untranslated (the foreign form), or flipping from the foreign form to canonical.
public readonly struct Reveal
{
    public static Reveal Plain { get; }
    public static Reveal Untranslated(ForeignText foreign);
    public static Reveal Flipping(ForeignText foreign, float elapsed, int row);
    public RevealKind Kind { get; }
    public ForeignText Foreign { get; }
    public float Elapsed { get; }
    public int Row { get; }
}

public static class DisplayText
{
    /// The text to show ("" for null). Plain, or a null Foreign: the canonical text.
    /// Untranslated: every letter's cell. Flipping: per FlipSequence (reduced motion: canonical once elapsed >= 0,
    /// the foreign form before). Right-to-left: the composed logical string through ArabicShaper.ToVisual.
    public static string For(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion);
    /// Seconds until the text settles: 0 when settled, +infinity when untranslated.
    public static float Remaining(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion);
    /// FlipSequence.Progress for this text (0 for Plain and Untranslated): recompose only when it changes (R8).
    public static int Progress(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion);
    /// True while any foreign or scramble cell shows (the font rule, R6).
    public static bool ShowsForeign(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion);
}
```

`TextMedium` and `For(string, TextMedium)` go: the medium now picks a translator, which is a Domain rule (`TranslatorKind`), and the callers pass a `Reveal`.

**`SpeechQueue.cs`** (changed, R9):
- `Say(string text, string expression, float revealSeconds = 0f)`: the line counts as fully shown `max(typing seconds, revealSeconds)` after it starts. Negative or NaN counts as 0.
- `public float LineSeconds`: seconds since the shown line started (0 when nothing shows).
- `public void EndReveal()`: the shown line's reveal counts as done now (its hold starts at the later of now and the end of its typing).
- `EndSeconds` becomes `max(TypeSeconds, reveal) + (waiting ? min : hold)`. Every existing call passes no reveal, so the existing pacing is unchanged.

**`Paging.cs`** (new):
- `PageCount(int count, int perPage)`: at least 1; a `perPage` below 1 counts as 1.
- `Clamp(int page, int count, int perPage)`.
- `First(int page, int count, int perPage)` and `End(int page, int count, int perPage)`: the first index on the clamped page and one past its last index.
- `PagedRowsWindow.PageCount` and `Rebuild` call these (`PagedRowsWindow.cs` 90-94, 109-111), as does the Home shop.

### 2.4 Assembly-CSharp: content and runtime

**`TranslationSettings.cs`** (new):

```csharp
/// A script: its direction and its OS font chain (piece 6's FontCandidate; empty = the runtime LiberationSans).
[Serializable] public sealed class TranslationScript { public string id; public bool rightToLeft; public List<FontCandidate> fonts = new(); }

/// Everything translation needs at runtime (generated by Generate World from world_source.json translation).
[Serializable] public sealed class TranslationSettings
{
    public TranslationRules rules = new();
    public List<TranslationScript> scripts = new();
    public FlipTiming flip = new();
    public string fallbackGlyphs;
    public TranslationScript GetScript(string id); // null for unknown
    public bool HasData => rules != null && rules.tongues.Count > 0;
}
```

**`ContentLibrarySO`** (CRLF): under `[Header("Translation (piece 9)")]`, `[SerializeField] private TranslationSettings translation = new();` and `public TranslationSettings Translation => translation;`. Generate World writes both.

**`NationEraProfileSO`**: `public string tongue;` ("The tongue this place writes and speaks: a translation.tongues[] id, from world_source.json places[].tongue").

**`CaseInstance`** (CRLF): `public string tongueId;` ("The claimed place's tongue (piece 9): the traveller's papers and speech use it; never the true home's").

**`CaseFactory`** (CRLF): `tongueId = place != null ? place.tongue : string.Empty`, next to `originLabel` (228, 246). It is the same `place` the claim label comes from, for premades too. No draw is added.

**`TimelineService`**: `public static TranslationDay BuildTranslationDay(ContentLibrarySO lib, WorldState world) => new TranslationDay(lib != null ? lib.Translation.rules : null, Snapshot(world, null));`. It is the day-start snapshot of `BuildInterviewDay`, reused.

**`GameManager`** (CRLF): where it injects the interview day (162-190), it also calls `investigationUI.SetTranslation(TimelineService.BuildTranslationDay(contentLibrary, _worldState), contentLibrary.Translation)`. It logs one warning when `!Translation.HasData`: "[GameManager] The content library has no translation data: every tongue reads as English. Run Tools > TimeDesk > Generate World."

**`RuntimeFonts`** (piece 6): a new `public Result Resolve(string key, IReadOnlyList<FontCandidate> candidates, string sample)` holds today's candidate loop, cache and coverage check, keyed by `key`. `Resolve(ThemeSO theme, string sample)` becomes: no runtime font → default; else `Resolve(theme.cultureId, theme.fonts, sample)`. Assets are named `Runtime {key} ({name})`.

**`CultureThemeService`** (piece 6): `public RuntimeFonts Fonts => _fonts;` ("The session's runtime fonts, also used by translation for script fonts; null before Configure").

**`TranslationPresenter`** (new, plain class owned by `InvestigationUIController`, one per scene):
- `TranslationPresenter(TranslationDay day, TranslationSettings settings)`.
- `CaseTranslation ForCase(CaseInstance inst)`: returns `CaseTranslation.None` unless `day.Foreign(inst.tongueId)`.
- Otherwise it builds, once per tongue per day:
  - the `ForeignText` (the parsed table and the script's direction);
  - its font, once per script: `CultureThemeService.Instance.Fonts.Resolve("script:" + script.id, script.fonts, sample)`, where the sample is the cells of every tongue written in that script plus a–z and A–Z.
- When the table does not parse, the service or its fonts are missing, or `Covers` is false: it uses the fallback table (left to right) and no font, and logs one warning per script: "[Translation] No font draws the {script} script (tried: {tried}; missing {missing}): {tongue} shows in the fallback cipher. Install one of the fonts or add a candidate in world_source.json translation.scripts."
- It reads `MotionPreference.Reduced` and formats the placeholder `UiText.Format("compare.untranslated", tongue.displayName, pack.displayName)`.

**`CaseTranslation`** (new, plain class):

```csharp
public sealed class CaseTranslation
{
    public static CaseTranslation None { get; }        // everything plain
    public bool Foreign { get; }                        // the claim's tongue is foreign today
    public ForeignText Text { get; }                    // null when not foreign
    public TMP_FontAsset Font { get; }                  // the script's runtime asset; null = the text's own font
    public bool PapersTranslated { get; }
    public bool SpeechTranslated { get; }
    public FlipTiming Timing { get; }
    public bool ReducedMotion { get; }
    public string Placeholder { get; }                  // the compare bar's text for an untranslated side
    /// A document field: Plain (not foreign, or not in the tongue), Untranslated (no Papers translator, or not yet revealed: NaN), else Flipping.
    public Reveal Field(ClueCategory category, float revealElapsed, int row);
    /// A transcript line, settled: Plain (not foreign, the desk, or Speech translated), else Untranslated.
    public Reveal Line(DialogSpeaker speaker);
    /// The bubble's current traveller line: Plain, Untranslated, or Flipping at the line's clock.
    public Reveal Bubble(float lineSeconds);
    /// The compare bar's text for a statement: the canonical value when readable, else the placeholder.
    public string Shown(bool inTongue, bool translated, string canonical);
}
```

The Domain decisions (`Translation.InTongue`, `TranslationDay`) are applied by the presenter. The Visuals decisions are applied by `DisplayText`. `CaseTranslation` only pairs them up for one case.

**`TextFlip`** (new, plain class) drives one `TMP_Text`:
- `Show(TMP_Text text, string canonical, Reveal reveal, CaseTranslation tr)`: remembers the text's own font and material the first time, writes `DisplayText.For`, and sets the font per R6.
- `bool Tick(float elapsed)`: rebuilds the `Reveal` at `elapsed` and recomposes only when `DisplayText.Progress` changes (R8). When the text settles it restores the text's own font and returns false.
- `Complete()`: shows the settled text in the text's own font.
- `static void Write(TMP_Text text, string canonical, Reveal reveal, CaseTranslation tr)`: the same writing and font rule for a text that never animates (transcript rows).

**`MotionPreference`** (new, static): `public static bool Reduced { get; set; }`, stored in PlayerPrefs under `TimeDesk.ReducedMotion` ("full" by default, or "reduced"), saved on set. It follows the `UiLanguagePreference` pattern.

### 2.5 UI

**`InvestigationUIController`:**
- **New:** `SetTranslation(TranslationDay day, TranslationSettings settings)` creates the presenter. Its doc says "the day-start translation, injected by GameManager".
- **`ShowRich`:**
  - `_caseTranslation = _presenter != null ? _presenter.ForCase(inst) : CaseTranslation.None`;
  - `clone.SetDocument(doc, compareController, inst.look, _art, _caseTranslation)` (378);
  - before `StartInterview`, `wheel.SetTranslation(_caseTranslation)` when a wheel is wired.
- **`StartInterview`:** `transcriptWindow.Bind(..., compareController, _caseTranslation)`.
- **`OpenDocumentWindow`** (564) also calls the document controller's `Reveal()`: the written reveal point. The per-case document controllers are kept in a list beside `_docWindows`.
- **The text fallback** is unchanged (canonical, R14).

**`DocumentWindowController`:**
- **`SetDocument(..., CaseTranslation tr)`** stores `tr`, sets `_revealedAt = NaN` and clears the flips.
- **`Reveal()`:** does nothing unless `tr.Foreign && tr.PapersTranslated`, the document has a field `InTongue`, and `_revealedAt` is still NaN. Then it sets `_revealedAt = Time.unscaledTime` and rebuilds the page.
- **`Rebuild` (131-156), per field:**
  - `row` is the field's index among this page's fields that are in the tongue, so rows that never flip take no time;
  - `inTongue = Translation.InTongue(f.category)`;
  - the value text goes through `TextFlip.Show(texts[1], f.value, tr.Field(f.category, now - _revealedAt, row), tr)`, and running flips are kept for `Update`;
  - the compare call passes `tr.Shown(inTongue, tr.PapersTranslated, f.value)` as the bar text and keeps `CompareEvidence.FromDocumentField(field)`;
  - the row's click first finishes the document (it moves `_revealedAt` back by the longest remaining flip on the page) and then selects the row.
- **`Update`** runs only while flips are running, ticking them with `Time.unscaledTime - _revealedAt`.
- **The class doc** says that values in the claim's tongue show untranslated or flip at the scan.

**`TranscriptWindowController`:**
- `Bind(..., CaseTranslation tr)`.
- `FillRow` writes the sentence through `TextFlip.Write(texts[1], line.Text, tr.Line(line.Speaker), tr)`.
- An answer row passes `tr.Shown(true, tr.SpeechTranslated, line.Value)` as the bar text, and keeps `CompareEvidence.ForAnswer(...)`.
- The class doc: traveller lines are in the claim's tongue and show English only with the Speech translator.

**`TravellerWheel`:**
- **New:** `SetTranslation(CaseTranslation tr)`.
- **`Say`:** each line is queued as `_speech.Say(line.Text, line.Expression, reveal)`, where `reveal = DisplayText.Remaining(line.Text, tr.Bubble(0f), tr.Timing, tr.ReducedMotion)` when `tr.Foreign && tr.SpeechTranslated`, and 0 otherwise.
- **`ShowSpeech`:**
  - on a new line, `bubble.Show(DisplayText.For(_speech.Text, tr.Bubble(0f), ...), ...)` and `_flip.Show(bubble.Label, _speech.Text, tr.Bubble(_speech.LineSeconds), tr)`;
  - each `LateUpdate` while showing, `_flip.Tick(_speech.LineSeconds)`;
  - `bubble.Reveal(_speech.VisibleCharacters)` as today.
- **`Open()`** calls `_flip.Complete()` and `_speech.EndReveal()` (the skip).
- **`SetCanOpen(false)`**, which clears the speech, also calls `_flip.Complete()`, so the bubble's own font is restored.
- **The class doc** mentions the flip.

**`OverlayCallout`:** `public TMP_Text Label => label;` ("The callout's text, for a caller that animates it: the wheel's translation flip").

**`CompareController`** (doc only): `Select`'s `value` becomes "the text the bar shows for this side (the canonical value, or the untranslated placeholder); with typed evidence, MATCH is decided on CompareEvidence.MatchValue, the canonical value; never DisplayText output".

**`SettingsWindowController`** (piece 6): new `[SerializeField] private Button fullMotionButton, reducedMotionButton;`. `Awake` wires them to `MotionPreference.Reduced = false / true`, and `ShowSelection` paints both pairs with the existing `Paint`. The class doc gains the motion choice.

**`HomeUIController`:**
- New `[SerializeField, Min(1)] private int shopRowsPerPage = 6;` ("Upgrades per shop page; the rows area fits 7 rows with the pager") and `private int _shopPage;`.
- `BuildShopRows` shows `Paging.First..End` of the upgrade list. When `PageCount > 1` it adds a pager row through the existing `CreateRow`: label "Page {n}/{m}", button "Next >", which advances `_shopPage` (wrapping round) and rebuilds.
- `ShowShop` resets the page to 0 when the shop panel opens (it was hidden). The re-show after a purchase (`HomeManager.HandleBuyUpgrade` calls `ShowShop` again) keeps the page, clamped.
- `CreateRow` and `CreateLabelRow` set their row's height to its `LayoutElement` height (the rows container does not control child heights, so rows were 100 px tall).
- Home keeps English literals (piece 6 R9/O1(b)).

**`PagedRowsWindow`:** calls `Paging` (T9).

### 2.6 Builder (`OfficeSceneUIBuilder.cs`, LF)

`BuildSettingsWindow` (piece 6, 1485-1504) builds the window at 580 × 520 (was the 580 × 400 default) and adds, re-applied on every run like the rest:
- the language rows move up: `Body` (0.05, 0.78)–(0.95, 0.87), the language buttons (0.05, 0.64)–(0.48, 0.76) and (0.52, 0.64)–(0.95, 0.76);
- `MotionLabel`: key `settings.motion`, anchors (0.05, 0.50)–(0.95, 0.59), 20 pt like the language label, `WindowBody`;
- `FullMotionButton` (`settings.motionFull`): anchors (0.05, 0.36)–(0.48, 0.48);
- `ReducedMotionButton` (`settings.motionReduced`): anchors (0.52, 0.36)–(0.95, 0.48);
- `NoteText` moves to (0.05, 0.04)–(0.95, 0.32).

It wires `fullMotionButton` and `reducedMotionButton`. Theme tags are stamped as for the language pair. Build Office UI rebuilds `OfficeGameplay.unity`. Nothing else in the builder changes: the flip works on the existing texts at runtime.

### 2.7 Content pipeline

**`world_source.json`** (LF, 2-space indent, raw UTF-8: the file already stores £, ¥, ä and é raw):
- `places[].tongue` (required);
- a new root section:

```json
"translation": {
  "fromDay": 2,
  "announce": "Budget cuts close the interpreter pool. From today, foreign papers and speech reach the desk untranslated; translators are sold at Home. The scanner still compares untranslated text with the books.",
  "flip": { "startDelay": 0.3, "letterInterval": 0.04, "letterSeconds": 0.12, "scrambleSteps": 2, "rowStagger": 0.15 },
  "fallbackGlyphs": "olmnupqrystvwxazbcdfeghjik",
  "scripts": [ { "id": "latin", "rightToLeft": false, "fonts": [] }, "..." ],
  "packs": [ { "id": "near_east", "displayName": "Near East", "writtenCost": 100, "spokenCost": 80 }, "..." ],
  "tongues": [ { "id": "arabic", "displayName": "Arabic", "script": "arabic", "pack": "near_east", "glyphs": "..." }, "..." ]
}
```

- piece 6's `ui.strings` gains (Full tier, gloss None):
  - `compare.untranslated` "(untranslated {0}; {1} Translator)";
  - `settings.motion` "Motion";
  - `settings.motionFull` "Full";
  - `settings.motionReduced` "Reduced";
  - `settings.note` gets the sentence "Reduced motion shows translations at once."

**`WorldContentGenerator`** (LF):
- **Data classes:** `TranslationData`, `ScriptData` (with piece 6's font data shape), `PackData` and `TongueData`, plus `PlaceData.tongue`.
- **`CheckTranslation`** runs before anything is written, and every error aborts the run. It checks:
  - `Translation.Problems(rules, scripts, places)`;
  - `Pseudoscript.ParseTable` for every non-native tongue;
  - `ArabicShaper.CanShape` for every table whose script is right to left;
  - `Pseudoscript.IsAsciiLetters` for the fallback;
  - that script ids are unique and each has a font list (empty means LiberationSans);
  - that costs are at least 0;
  - that no generated id (`Translation.UpgradeId`) equals a hand-authored upgrade id in the library;
  - that `announce` is non-blank ASCII when `fromDay > 1`;
  - that the flip knobs are at least 0;
  - that the three new UI keys exist.
- **Writes:**
  - `MakePlace` sets `tongue`;
  - `MakeTranslator(pack, kind)` writes `Translation/Upgrade_Tr_{PackPascal}_{Papers|Speech}.asset` (id, display name and description per R13, cost, no `unlockEffect`);
  - `MakeTranslationNotice` writes `Translation/Trigger_TranslationNotice.asset` when `fromDay > 1`: id `translation_notice`, one-shot, `newsLineOnFire = announce`, conditions `DayGate(fromDay, Gates.UnlockNight(fromDay))`;
  - the library's `translation` is written from the data.
- **`WireLibrary`** per R12. `OwnedFolders` gains `Translation`, and `HandAuthored` skips it like the Interview and History folders. The summary log counts translators.

**`ContentLibraryValidator`** (LF) gains `CheckTranslation(lib)`:
- the same `Translation.Problems` over `lib.Translation` and every place's `tongue`;
- the table and fallback rules;
- every pack has both its upgrades in `lib.Upgrades` (by `Translation.UpgradeId`);
- upgrade ids are unique;
- `fromDay > 1` needs the notice trigger in the library's triggers.

### 2.8 Content

**Scripts:**

| Script | Direction | Font candidates (file, family), then the runtime LiberationSans |
|---|---|---|
| latin | LTR | none |
| greek | LTR | none (LiberationSans draws Greek, piece 6 R5) |
| arabic | RTL | tahoma.ttf / Tahoma; arial.ttf / Arial; Geeza Pro; Noto Sans Arabic (piece 6's Arabic chain) |
| han | LTR | msyh.ttc 0 / Microsoft YaHei; simsun.ttc 0 / SimSun; PingFang SC; Noto Sans CJK SC |
| kana | LTR | YuGothR.ttc 0 / Yu Gothic; msgothic.ttc 0 / MS Gothic; Hiragino Sans; Noto Sans CJK JP |
| hieroglyphs | LTR | seguihis.ttf / Segoe UI Historic; Noto Sans Egyptian Hieroglyphs |
| cuneiform | LTR | seguihis.ttf / Segoe UI Historic; Noto Sans Cuneiform |
| runic | LTR | seguihis.ttf / Segoe UI Historic; Noto Sans Runic |

**Tongues and tables** (cells for a, b, c, … z). All are checked with `p9_tables.py`: 26 distinct cells, each covered by its chain on this machine, and the Arabic cells inside `ArabicShaper`'s range.

| Tongue | Script | Pack | Table |
|---|---|---|---|
| english | latin | (native) | – |
| egyptian | hieroglyphs | near_east | 𓄿𓃀𓍿𓂧𓇋𓆑𓎼𓉔𓇌𓆓𓎡𓃭𓅓𓈖𓂝𓊪𓈎𓂋𓋴𓏏𓅱𓎛𓏲𓐍𓏭𓊃 (U+1313F U+130C0 U+1337F U+130A7 U+131CB U+13191 U+133BC U+13254 U+131CC U+13193 U+133A1 U+130ED U+13153 U+13216 U+1309D U+132AA U+1320E U+1308B U+132F4 U+133CF U+13171 U+1339B U+133F2 U+1340D U+133ED U+13283: uniliteral sound signs) |
| akkadian | cuneiform | near_east | 𒀀𒁀𒆠𒁕𒂊𒉿𒂵𒄩𒄿𒅖𒅗𒆷𒈠𒈾𒌑𒉺𒋡𒊏𒊓𒋫𒌋𒉌𒊒𒆪𒅀𒍝 (U+12000 U+12040 U+121A0 U+12055 U+1208A U+1227F U+120B5 U+12129 U+1213F U+12156 U+12157 U+121B7 U+12220 U+1223E U+12311 U+1227A U+122E1 U+1228F U+12293 U+122EB U+1230B U+1224C U+12292 U+121AA U+12140 U+1235D: syllable signs by sound) |
| arabic | arabic | near_east | عكسريقجحاشتنملوبطدفخهزغضءذ |
| greek | greek | mediterranean | ολμναξπρηστφχψυςάέβγιδζθωκ |
| latin | latin | mediterranean | ifghojklumnpqrystvwxazbced |
| italian | latin | mediterranean | ohjkulmnèpqrstavwxzbecdfig |
| chinese | han | east_asia | 安文山水衣日月天一地木火金石欧田中大小上乌下口心由王 |
| japanese | kana | east_asia | あかきくえけこさいしすせそたおちつてとなうにのはやま |
| brittonic | latin | north_europe | wpqrastveyxzbcidfghjoklmun |
| oldenglish | latin | north_europe | umnpyqrsæþvƿxzebcðfgihjkol |
| germanic | runic | north_europe | ᛟᚷᚹᚺᚨᚾᛃᛈᛖᛉᛊᛏᛒᛗᚢᛚᛜᛞᚴᛦᛁᚠᚦᚱᛇᚲ |
| german | latin | north_europe | eklminpqörßtvwuxzbcdüfghäj |

Display names: English, Egyptian, Akkadian, Arabic, Greek, Latin, Italian, Chinese, Japanese, Brittonic, Old English, Proto-Germanic, German.

**Place tongues:** as in §1.1, for all 48 places, the Future included. The Han and Kana cells were chosen for neutral meanings (sky, water, mountain, …), and native readers review every non-Latin table before release (§7). The Proto-Germanic place's fact reads "pre-runic". Runes are the closest script and are used for flavour; the fact stays as it is.

**Packs:** near_east "Near East", mediterranean "Mediterranean", east_asia "East Asia", north_europe "Northern Europe". Every pack costs 100 for Papers and 80 for Speech.

### 2.9 Knobs

| Knob | Where at runtime | Value | Authored in |
|---|---|---|---|
| First day with foreign text | `TranslationRules.fromDay` | 2 | `translation.fromDay` |
| The notice | `Trigger_TranslationNotice.newsLineOnFire` | §1.2 | `translation.announce` |
| Tongues, packs, tables | `ContentLibrarySO.Translation.rules` | §2.8 | `translation.tongues`, `translation.packs` |
| Script direction and fonts | `ContentLibrarySO.Translation.scripts` | §2.8 | `translation.scripts` |
| A place's tongue | `NationEraProfileSO.tongue` | §1.1 | `places[].tongue` |
| Translator prices | generated `UpgradeSO.cost` | 100 / 80 | `translation.packs[].writtenCost` / `spokenCost` |
| Flip timing | `ContentLibrarySO.Translation.flip` (`FlipTiming`) | 0.3 s / 0.04 s / 0.12 s / 2 steps / 0.15 s | `translation.flip` |
| Fallback cipher | `ContentLibrarySO.Translation.fallbackGlyphs` | §2.7 | `translation.fallbackGlyphs` |
| Shop upgrades per page | `HomeUIController.shopRowsPerPage` | 6 | serialized layout value (the scene keeps the default) |
| Reduced motion | PlayerPrefs `TimeDesk.ReducedMotion` | full | Settings window |
| Placeholder and Settings copy | `ui.strings` | §2.7 | `ui.strings` |

No rule reads a code constant. The only fixed numbers are `TableSize` (the alphabet), `ScrambleIndex`'s stride of 7 (any number coprime with 26), and the builder's anchors.

### 2.10 Copy

| Where | Text |
|---|---|
| day-2 morning paper | `translation.announce` (§1.2) |
| compare bar, untranslated side | `compare.untranslated` "(untranslated {0}; {1} Translator)" |
| Settings window | "Motion", "Full", "Reduced"; the note's extra sentence |
| Home shop pager | "Page {n}/{m}", "Next >" (Home literals, piece 6 O1(b)) |
| upgrade names and descriptions | R13 |
| warnings | `GameManager` (no data), `TranslationPresenter` (fonts), generator and validator messages |

### 2.11 Every file that changes

| File | Assembly | Change |
|---|---|---|
| `Assets/Scripts/Domain/TranslationContent.cs`, `Translation.cs` (+metas) | Domain | new §2.2 |
| `Assets/Scripts/Visuals/Pseudoscript.cs`, `FlipSequence.cs`, `Paging.cs` (+metas) | Visuals | new §2.3 |
| `Assets/Scripts/Visuals/DisplayText.cs` | Visuals | `ForeignText`, `Reveal`, the new `For` family; `TextMedium` removed |
| `Assets/Scripts/Visuals/ArabicShaper.cs`, `Assets/Tests/EditMode/ArabicShaperTests.cs` | Visuals | the hamza fix (re-check) |
| `Assets/Scripts/Visuals/SpeechQueue.cs` | Visuals | the reveal time, `LineSeconds`, `EndReveal` |
| `Assets/Scripts/UI/Translation.meta` and `UI/Translation/{TranslationSettings,TranslationPresenter,CaseTranslation,TextFlip,MotionPreference}.cs` (+metas) | Assembly-CSharp | new §2.4 |
| `ContentLibrarySO.cs`, `Timeline/NationEraProfileSO.cs`, `CaseInstance.cs`, `CaseFactory.cs`, `Timeline/TimelineService.cs`, `GameManager.cs` | Assembly-CSharp | §2.4 |
| `UI/Theme/RuntimeFonts.cs`, `UI/Theme/CultureThemeService.cs` | Assembly-CSharp | the keyed `Resolve`; `Fonts` |
| `UI/InvestigationUIController.cs`, `UI/DocumentWindowController.cs`, `UI/TranscriptWindowController.cs`, `UI/TravellerWheel.cs`, `UI/OverlayCallout.cs`, `UI/CompareController.cs` (doc), `UI/Theme/SettingsWindowController.cs`, `UI/HomeUIController.cs`, `UI/PagedRowsWindow.cs` | Assembly-CSharp | §2.5 |
| `Assets/Editor/WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `OfficeSceneUIBuilder.cs` | Editor | §2.6, §2.7 |
| `Assets/Data/World/world_source.json` | content | §2.7, §2.8 |
| `Assets/Data/World/Translation/` (folder, 8 upgrades, 1 trigger, metas) | generated | new |
| `Assets/Data/World/Places/Place_*.asset` (48) | generated | `tongue` |
| `Assets/Data/Content Library/ContentLibrary_Main.asset` | generated | `translation`; upgrades and triggers appended |
| `Assets/Scenes/OfficeGameplay.unity` | scene | rebuilt by the builder (Settings window) |
| `Assets/Tests/EditMode/{Translation,Pseudoscript,FlipSequence,Paging}Tests.cs` (+metas) | tests | new §5 |
| `Assets/Tests/EditMode/{DisplayText,SpeechQueue}Tests.cs` | tests | changed §5 |
| `docs/FEATURES.md` | docs | §3.3 |
| `docs/superpowers/specs/2026-09-25-translation-design.md` | docs | this spec, committed first |

Checked and unchanged:
- `DeskDocument` (the paper shows its title and holder, both readable);
- `DiscrepancyLog`, `CompareEvidence`, `Forgery`, `Lies`, `Seeds`, `Interview`, `InterviewScript`, `InterviewDay` and `CaseFactory`'s draws;
- `WorldState` and `SaveSystem`;
- the books, Citizen Records, `HomeManager` (purchase unchanged), `DebugPanelController` (it already lists every library upgrade, so testers can grant translators), `TitleScene` and the preserved hybrid scene.

### 2.12 Why some logic stays outside Domain and Visuals

The glue is:
- `TranslationPresenter` (it resolves fonts, formats the placeholder and reads PlayerPrefs);
- `CaseTranslation` (it pairs the Domain answers with the Visuals reveal for one case: each member is one call into tested code);
- `TextFlip` (it writes TMP text and fonts);
- the window controllers, the wheel, the shop, `GameManager`/`TimelineService` (the snapshot copy), `CaseFactory` (one field copied from the claim's place), and the generator, the validator and the builder.

Every rule they apply is tested code:
- which categories and speakers use the tongue, foreign or translated today, and the upgrade ids (`Translation`, `TranslationDay`);
- the table rules and glyph mapping (`Pseudoscript`);
- the timing, change points and skip (`FlipSequence`, `DisplayText`);
- the bubble's hold (`SpeechQueue`);
- the shop and window paging (`Paging`).

The Unity checks in §6 prove the whole-screen behaviour.

### 2.13 Intent audit: the existing mechanism tried for each new one

| New | Existing mechanism reused or tried | Why something new was still needed |
|---|---|---|
| Foreign display | `DisplayText` (piece 7's seam, its only callers already at the reveal points) | the seam's identity becomes the real rule; no second display path |
| Glyph substitution | `Seeds`/`SeededRandom` (tried: a seeded permutation per tongue) | a random permutation needs a stable string hash that does not exist, and a Visuals copy of `Seeds.Mix`; authored tables need neither and give designers control |
| Fonts | piece 6's `RuntimeFonts` and its chains | a keyed overload only; the loop, cache and coverage check are reused |
| Arabic | piece 6's `ArabicShaper` | none |
| Upgrades | `UpgradeSO`, `unlockedUpgradeIds`, the Home shop, `GateSnapshot.HasUpgrade`, the debug panel | generated instances only |
| Day gate and notice | piece 3's day-start snapshot, `Gates.UnlockNight`, `DayGate`, the one-shot trigger with `newsLineOnFire` | one generated trigger |
| Evidence | `CompareEvidence.MatchValue` (piece 4's shown vs match value), `DiscrepancyLog.Prove` | none: no evidence code changes |
| Timing | `SpeechQueue` (the bubble's clock) | the flip needs per-letter timing; `SpeechQueue` gains only a reveal time |
| Shop paging | `PagedRowsWindow` (window paging; the shop is not a window) | its maths is extracted as `Paging` and shared |
| Motion preference | `UiLanguagePreference` (the pattern), the Settings window | one more preference and button pair |

## 3. Retired or superseded

### 3.1 Code removed or replaced

- `TextMedium` and `DisplayText.For(string, TextMedium)` (`DisplayText.cs`) are replaced by the `Reveal` form (§2.3). Their three callers change: `DocumentWindowController.cs:144`, `TranscriptWindowController.cs:46` and `TravellerWheel.cs:187` at `ab94784` (line 190 in the office move's working tree).
- `PagedRowsWindow.PageCount` (90-94) and the index maths in `Rebuild` (109-111) become `Paging` calls.
- `RuntimeFonts.Resolve(ThemeSO, string)`'s body moves into the keyed overload.
- `DisplayTextTests` is rewritten (§5).

### 3.2 Earlier spec lines superseded (kept as approved records, not edited)

**`2026-09-25-physical-desk-design.md` (piece 7):**
- **K19 (line 37)**, "No language data": language data now exists (T1).
- **R19 (line 72)** and **§2.6 (354-357):** `DisplayText.For(canonical, medium)` "returns the canonical string today" becomes `For(canonical, Reveal, FlipTiming, reducedMotion)`, and `TextMedium` goes. The medium chooses a translator (`TranslatorKind`, Domain). What still holds: `DisplayText` is the one place display differs from canonical; its result never reaches `CompareController` or `CompareEvidence` (R10); no event without a subscriber is added.
- **§2.10 (lines 556, 566, 587, 591)** and **§2.11 step 5 (625):** "callers apply `DisplayText`". They still do, through `CaseTranslation`/`TextFlip`.
- **§2.21 (886-889):** delivered as written. The paper "stays in its source script" trivially: it shows only its title and holder, both of which are readable (T2).
- **§4 (1053-1057):** "tongue ids", "Written/Spoken upgrades" and "the letter-flip model behind DisplayText" are delivered. **"Untranslated rows not comparable"** and **"a manual path so tells stay provable"** are replaced by T5: untranslated rows stay comparable on canonical values, so no manual path is needed. A manual decode stays possible later (§4).
- **§5 (1122),** `DisplayTextTests` "returns the canonical string for both media": replaced (§5).

**`2026-09-25-wheel-content-design.md` (piece 8):**
- **W6 (line 20):** "once fully shown it stays `bubbleSeconds`": a line is fully shown at the later of its typing and its translation flip (R9). With no Speech translator, or reduced motion, the pacing is exactly W6's.

**`2026-09-24-dialog-questions-design.md` (piece 3):**
- **R18 (line 71),** ASCII-only authored text: the tongue glyph tables are non-ASCII by design. They are drawn only with runtime font assets (R6), so the tracked atlas never grows. Every other translation text (names, the notice, keys) is ASCII.
- **§1.1 lines 93-94** ("each row shows ... the sentence"; "clicking one puts the canonical fact value ... into the compare bar"): a traveller's sentence shows in the claim's tongue unless the Speech translator is owned. An untranslated answer puts the placeholder into the bar, not the value. The canonical sentence still contains the value verbatim, and the evidence is unchanged.

**`2026-09-24-ui-reacts-design.md` (piece 6, as promoted at `45f50b9`):**
- **U3 (line 82) and §1.3 (176-177),** "every document value ... and spoken answer stays exactly as generated, in Latin script": the values stay canonical Latin (unchanged). Their *display* follows T2/T3 from `fromDay`.
- **Z4 (line 98) and §1.3 line 177,** diegetic content keeps the project font: a diegetic text takes its script's runtime font while it shows foreign cells (R6). Its colours stay neutral.
- **R2 (line 113),** interview and dialog lines "stay English in v1": they are still authored in English (canonical). They are displayed in the tongue until translated.

**`2026-07-03-scanner-evidence-design.md`:** the compare bar still shows "values side by side". An untranslated side shows the placeholder (R11), and the verdict is unchanged.

### 3.3 `docs/FEATURES.md` (same commits as the behaviour)

Line numbers are from `e8b8290`; the plan re-finds each line.
- **:13** (Home phase): "... upgrades (the shop pages 6 at a time; translators, see Translation)".
- **:34** (speech bubble): append "; with the claim's Speech translator a foreign line types in its script and its letters flip into English one by one, and its hold starts after the flip; opening the wheel finishes the flip; reduced motion shows it at once (timing tested: `FlipSequenceTests`, `DisplayTextTests`; the hold tested: `SpeechQueueTests`)".
- **:45** (Clue Log): append "; traveller lines are in the claim's tongue (§Translation) and show English with the Speech translator; an untranslated answer is still compare-clickable and shows in the bar as '(untranslated <tongue>; <pack> Translator)'".
- **New section "Translation"** after "Investigation loop":
  - "Every place has a tongue; a traveller's papers and speech use the claimed place's tongue (never the true home's). English is always readable. From day 2 (`translation.fromDay`, announced in that morning's paper) a foreign tongue shows in its script: place-fact values on papers (never labels, titles, names or dates) and every traveller line; digits stay readable; the same value always looks the same (rules tested: `TranslationTests`; glyphs tested: `PseudoscriptTests`)".
  - "Untranslated text still compares on its original value: MATCH/MISMATCH and deviations are unchanged, so every liar is provable without a translator; the compare bar never prints foreign glyphs (canonical-evidence rule; the evidence code is unchanged and its tests stay green: `DiscrepancyLogTests`)".
  - "Translators: four regional packs (Near East, Mediterranean, East Asia, Northern Europe), each sold at Home as Papers (100) and Speech (80); in force from the next office day; saved as owned upgrades (ids tested: `TranslationTests`)".
  - "With the Papers translator, a scanned document's foreign values flip into English letter by letter (rows staggered); a click finishes the document; reopening never replays (timing tested: `FlipSequenceTests`)".
  - "Fonts: each script's OS font chain (Segoe UI Historic for hieroglyphs, cuneiform and runes), resolved at runtime, never copied; a script with no font falls back to a Latin cipher with a warning; Arabic is shaped right to left. Settings: Motion Full/Reduced (per player)".
  - Each bullet's scene behaviour is checked in the art office (this spec §6).
- **:130** (Generate World): add "the translation section (tongues with their glyph tables, scripts, packs, the flip timing, the fallback cipher, the notice) and every place's tongue; writes the 8 translator upgrades and the notice trigger into the owned `Assets/Data/World/Translation` (library upgrades: hand-authored kept in order, translators appended); checks the translation content (`Translation.Problems`, 26 distinct cells, Arabic cells shapeable, an ASCII fallback, no upgrade id clash)".
- **:131** (validator): add "the translation settings, every place's tongue, both upgrades of every pack in the library, and the notice".
- **Piece 6's Settings bullet:** add "and Motion: Full / Reduced (translation flips at once when reduced)".

## 4. Out of scope

- **A manual decoding path:** a slow flip bought with shift time on an untranslated row. It is the natural lever if playtests find the translators too weak (O-cost: a click mode on rows and a clock cost; the flip rule already exists).
- **Accent and dialect tells:** a liar whose speech slips into the true home's tongue would be a new tell channel. The Dialect placeholder app stays a placeholder.
- **Other translator shapes:** per-tongue packs, tiered or partial translation (only some fields, or garbled words), translation quality by era, and a Lexicon app listing owned translators.
- **Richer rendering:**
  - a per-character 3D flip (TMP vertex animation) and a flip sound;
  - typing right-to-left lines from the right (§7);
  - translated field labels or document titles;
  - a physical paper printed in its script;
  - books in foreign scripts.
- **A player notebook** of learned words.
- **Reduced motion for piece 8's typing and piece 7's camera push.**
- **Translating the PC chrome.** That is piece 6's culture feature, which is separate.
- **A culture naming a script** instead of repeating its font chain (the Arabic and CJK chains are authored twice today, see §7).
- **Home scene theming and a ScrollRect shop** (piece 6 F1).

## 5. Tests (EditMode, `Assets/Tests/EditMode`)

- **`TranslationTests`** (new, Domain):
  - `UpgradeId`: "tr_near_east_written" and "tr_near_east_spoken";
  - `InTongue(category)`: one `[TestCase]` per `ClueCategory`, false only for Name and BirthDate. `InTongue(speaker)`: Traveller true, Desk false;
  - `TranslationDay`, as a decision table on crafted snapshots:
    - native tongue: never foreign;
    - unknown or blank id: not foreign;
    - day below `fromDay`: not foreign, even while owning both upgrades;
    - day at `fromDay` with no upgrade: foreign, and translated for neither kind;
    - owning only Papers: translated Written, not Spoken, and the reverse;
    - owning another pack's upgrade: not translated;
    - the flag "upgrade:tr_near_east_written" alone: not translated (it reads upgrades, not flags);
    - null rules: nothing foreign;
    - editing the rules object after construction changes nothing;
  - `Problems`: one case per problem in §2.2's list (each message names its id), and a clean rules set with no problems.
- **`PseudoscriptTests`** (new, Visuals):
  - `ParseTable`: 26 BMP cells parse; 25 and 27 fail; a duplicate fails and is named; whitespace fails; 26 surrogate-pair cells parse as 26; null or blank fails;
  - `LetterIndex`: a/A → 0, z → 25, é → 4, ä → 0, ß → 18, digits, spaces, "£" and "¥" → −1;
  - `Cell`: case-insensitive; an upper-case letter upper-cases a Greek cell (β→Β) and leaves a Han cell alone;
  - non-letters pass through;
  - every starter table (§2.8, as literals) parses, and the fallback passes `IsAsciiLetters`;
  - the Egyptian rendering of "Silver shekel (by weight)" equals the §1.4 string;
  - a different table gives a different string.
- **`FlipSequenceTests`** (new, Visuals):
  - before `startDelay` every letter is Foreign;
  - letters start in rank order `letterInterval` apart; a row adds `rowStagger`;
  - a letter passes through `scrambleSteps` steps and lands at `start + letterSeconds`; `scrambleSteps` 0 goes straight to English;
  - a letter never goes back;
  - `Duration` equals the last landing, and 0 for no letters;
  - `Progress` is monotonic and changes exactly at the change points (checked at ±1 ms around each);
  - negative knobs count as 0;
  - `ScrambleIndex` differs from the letter for steps 0 and 1 and stays in range.
- **`DisplayTextTests`** (rewritten, Visuals):
  - Plain gives the canonical text; null gives "";
  - Untranslated gives every letter's cell and keeps digits and punctuation;
  - Flipping at 0 gives the foreign form, at `Duration` the canonical text, and midway English for the first letters and foreign for the rest;
  - reduced motion gives canonical at elapsed ≥ 0;
  - `Remaining` is `Duration − elapsed`, 0 when settled, +infinity when untranslated;
  - `ShowsForeign` is false once settled;
  - right to left: the Arabic untranslated form equals `ArabicShaper.ToVisual` of the logical cells, a half-flipped string keeps its English run in order, and the settled string equals the canonical text;
  - a null `Foreign` behaves as Plain.
- **`SpeechQueueTests`** (changed): a reveal longer than the typing delays the hold, and a shorter one changes nothing; `LineSeconds` counts from the line start and resets on the next line; `EndReveal` starts the hold now (not before typing ends); negative and NaN reveals count as 0; every existing test is unchanged (they pass no reveal).
- **`PagingTests`** (new, Visuals): `PageCount` for 0, 1, 6, 7, 12 and 13 items at 6 per page; `perPage` 0 counts as 1; `Clamp` below 0 and past the end; `First`/`End` on the last partial page. The existing paged-window behaviour is covered through the same maths.
- **Unchanged and green:** `DiscrepancyLogTests` (no evidence change), `LiesTests`, `SeedsTests` (no new stream), `InterviewScriptTests`, `InterviewDayTests` and the rest.

## 6. Verification plan (in the art office)

**Offline, after every change:** `compile_check.py` reports 0 errors in every project, and the reflection runner reports every Domain and Visuals test passing. The plan states the expected count after each task, from the baseline measured at its base commit.

**In the branch's Unity 6000.4.11f1**, through temporary `_TimeDesk*` scripts (the piece-8 pattern, `SCRATCH/p8__TimeDesk*.cs.txt`). They are never committed, and their reports go to the scratchpad. The office is the office move's pair: the art `OfficeScene` with `OfficeGameplay` loaded additively (`OfficeScenes`). The preserved hybrid scene is never opened.

0. **Baseline, before any piece-9 code:** dump seeds 12345 and 999 × days 1–6, one line per traveller (claim, name, date, role, liar, home, tells with channels, paper values, answers, small talk).
1. **Content:**
   - Generate World twice; the second run changes no file;
   - `Assets/Data/World/Translation` holds 8 upgrades and `Trigger_TranslationNotice`;
   - the library has 12 upgrades, the 4 hand-authored ones first;
   - the notice trigger comes after the unlock triggers and its only condition is `DayAtLeast 1`;
   - all 48 places carry the §1.1 tongue;
   - Validate Content Library reports no issues;
   - negative checks, each on an in-memory copy handed to the generator's `CheckTranslation` by reflection (nothing written): a 25-cell table, a duplicate cell, an Arabic table holding U+0679, a place without a tongue, a pack no tongue uses, `fromDay` 0, and a hand-authored upgrade id `tr_near_east_written`. Each gives exactly the expected error.
2. **Determinism:** the step-0 dump is byte-identical after the piece. A new dump column `tongue` equals the claimed place's tongue for every traveller, premades included.
3. **Font spike** (editor, no scene):
   - a fresh `RuntimeFonts` resolves every script's chain on this machine;
   - `Covers` is true for every tongue's table (supplementary-plane cells included);
   - a sample line per tongue ("Silver shekel (by weight)", with Arabic shaped) is rendered to `SCRATCH/p9_tongues.png` for a by-eye check that hieroglyphs, cuneiform and runes draw and are not tofu;
   - the tracked `LiberationSans SDF` and its fallback are unchanged afterwards.
   If supplementary-plane glyphs do not render, those three tongues use the fallback cipher until fixed; that is content only, by emptying their script's fonts.
4. **EditMode suite** through `TestRunnerApi`: every TimeDesk test passes. Report the known third-party UnitySkills failures and ignore them.
5. **Builder:** run Build Office UI.
   - The Settings window has `MotionLabel`, `FullMotionButton` and `ReducedMotionButton` wired to `SettingsWindowController`, and the note fits.
   - Semantic idempotence: two builds give equal dumps (the piece-3 §6 step 5 method).
   - Nothing else in `OfficeGameplay` changes.
6. **Scripted play-through in the art office** (play mode, `[InitializeOnLoad]` + SessionState steps). The script prepares a run at day 2 with a seed whose queue holds four travellers: an Ancient Egypt liar with a Papers Currency tell, an honest Ancient Greece traveller, a Medieval Iraq liar with a Capital Answer tell, and a Chinese traveller. It picks the seed with the same `CaseFactory` call. Then:
   1. **No translators:**
      - the passport's Coin of Issue and Native Tongue show glyphs in the Segoe UI Historic runtime asset;
      - Full Name, Date of Birth, the labels and the claim banner are English;
      - the bubble types the claim in hieroglyphs, and the transcript row shows the same glyphs;
      - Coin of Issue against the Currency Ledger claim row: the bar shows the placeholder, then DEVIATION LOGGED quoting "Silver shekel (by weight)"-style canonical values; Deny is correct with no citation;
      - the Greek traveller's fields against their rows give MATCH; Accept is correct;
      - the Iraqi's Capital answer row (Arabic, shaped right to left) against the Capitals Gazetteer claim row logs the deviation.
   2. **With translators** (`tr_near_east_written`, `tr_near_east_spoken` and `tr_east_asia_written` granted, then the day restarted through the automation's day start):
      - scanning the Egyptian passport opens its window foreign; sampling the value texts every 0.1 s shows letters landing in reading order, rows staggered, all English by the longest row's `Duration` (about 1.5 s), and the font restored;
      - a second scan shows English at once;
      - a click during a flip completes every row and selects the clicked row;
      - the permit's page 2 is English when viewed after the flip;
      - the bubble types foreign and flips behind the typing; the next queued line starts only after the flip plus 1.5 s; opening the wheel mid-flip completes it;
      - transcript rows show English.
   3. **Reduced motion** (set through the Settings button): at the scan, values are English at once, and bubble lines are English as they type.
   4. **Fit:** every document value, transcript row and bubble line shown reports `isTextOverflowing == false` after `ForceMeshUpdate`, in both forms. This includes the Chinese forms of the longest fact values (28 characters, the book-row limit) and the longest traveller line.
   5. **Home:** the shop shows page 1 (6 upgrades and the pager); "Next >" shows page 2; buying a translator there keeps page 2 and marks it owned; the money goes down by the price; Save + Continue keeps it owned.
   6. **Day 1** of a new run: nothing foreign anywhere. The day-2 paper carries the notice exactly once.
   7. No warning or error is logged, and `git status` shows no change to the tracked TMP font assets.
7. **Hygiene:**
   - revert the Unity-touched files that are not part of the work;
   - commit the regenerated content and the rebuilt `OfficeGameplay.unity`;
   - delete the `_TimeDesk*` files;
   - append the verification record to this spec.

### 6.1 Glyph spike (run before any code, 2026-09-25)

A temporary editor script resolved each script's chain through piece 6's `RuntimeFonts` (a `ThemeSO` per script in memory), checked every cell of every table, and rendered "Silver shekel (by weight) 1897" and the full table per tongue with TMP into `SCRATCH/p9_tongues.png`:
- **Hieroglyphs, cuneiform and runes:** `file seguihis.ttf` loads for all three; every cell (the supplementary-plane ones included) is in the runtime asset with a non-empty glyph; TMP counts one character per surrogate pair (30 characters for the 30-character line, R4). The rendered glyphs are real signs, not tofu. Hieroglyphs are thin line drawings in Segoe UI Historic and read faint at small sizes (§7).
- **Arabic** (`tahoma.ttf`), **Han** (`msyh.ttc`) and **Kana** (`YuGothR.ttc`) load and cover their tables; **Greek** and the Latin-letter tongues draw with the runtime LiberationSans (è, æ, þ, ƿ, ð, ä, ö, ü, ß included, from `LiberationSans.ttf`, whose cmap has them).
- **Decision:** no tongue uses the fallback cipher on this machine; the cipher stays for machines without a font (§1.9).
- **Found:** the Arabic sample line stopped after 13 characters: `ArabicShaper` wrote `'\0'` for a hamza after `ك` ("by"), fixed as recorded in the re-check above. A first run also resolved each tongue with its own sample under a shared script key, so later tongues of a script were never checked: the reason for the per-script sample.
- The tracked `LiberationSans SDF` and its fallback files were unchanged.

## 7. Risks

- **The translators may feel weak.** Blind comparison proves every tell, so a translator buys reading speed and comprehension, not proof. That is intentional for the basic version (T5, T11). If playtests agree it is too weak, the lever is a manual-decode time cost (§4) or cheaper prices. Either way the game never becomes harder than today for a player who buys nothing, except that on day 2 they must compare values they cannot read.
- **Day 2 carries a lot.** Answer tells, premades and foreign text all arrive together. `translation.fromDay` is the knob; raising it to 3 or 4 is content only.
- **Width.** Han and kana cells are full-width, so a foreign value can be about twice as wide as its English form. Document values and transcript rows already auto-size (piece 3 R10); §6 step 6.4 checks overflow. If a row overflows, a later table can use half-width forms, or the rows' minimum size can drop.
- **Right-to-left motion.** TMP reveals characters in string (visual) order, so an Arabic line types from its left end, and a finished flip snaps from right-aligned runs to left-to-right English (R5). This is cosmetic, and belongs to the "flesh it out later" pass (type a shaped prefix instead of `maxVisibleCharacters`).
- **A flip can play unseen.** The scan wakes the PC but does not open the PC frame (the office move), so unless the frame is open the flip plays on the office PC's cloned screen. The flip is short and reopening shows English, so nothing is lost. If playtests want every flip seen up close, the lever is to start the papers' flip when the frame first shows the window (a view event into `InvestigationUIController`).
- **Supplementary-plane glyphs.** Reading the code, TMP handles surrogate pairs (`TMP_FontAssetUtilities.GetCodePoint` in `HasCharacters`, and TMP's text parsing), but the glyphs have not been rendered yet. §6 step 3 is a spike with a content-only fallback.
- **Fonts elsewhere.** The macOS and Linux names in the chains are unverified; the fallback cipher and its warning cover a missing font. Segoe UI Historic ships with Windows 10 and 11.
- **The same chains authored twice.** The Arabic and CJK chains appear in piece 6's cultures and in piece 9's scripts. They are separate concerns (UI style against the script's glyphs) but can drift apart; §4 lists the fix.
- **Cultural care.** The tables are gibberish by design, but a scramble could by chance spell a real word in Arabic, Chinese, Japanese, Greek, German or Italian. Native readers review the tables and the generated sample line per tongue before release, as piece 6 already requires for its translations. Hieroglyph and cuneiform cells are sound signs; Han and kana cells were chosen for neutral meanings.
- **Blind narrative choices.** Without the Speech translator, a premade's lines are unreadable, while the desk's replies (wheel labels) stay English. Players may feel their story choices are blind; that is the Speech translator's draw.
- **Dependencies and churn.** This piece needs piece 6 (`RuntimeFonts`, `FontCandidate`, `ArabicShaper`, `UiText`, the Settings window) and the office move (`OfficeGameplay`). The move is rewriting `TravellerWheel`, `OverlayCallout` and the desk right now. The plan's first task records the implemented names after both merges and re-reads every file; a rename there changes this spec's glue, not its rules.
- **`world_source.json` merge.** Every place gains a `tongue` key (48 lines) and a new section is added. Concurrent content pieces must merge the JSON by hand (never regenerate it from a script, per the house rules) and re-run Generate World.
- **Old saves** meet foreign text on Continue before their late notice arrives (§1.8). This is documented, and Continue works as today.

## 8. Verification record (2026-09-25, `E:\unity\NOPE-art`, branch `feat/translation`, Unity 6000.4.11f1)

Temporary `_TimeDeskP9*` scripts (`-executeMethod`, GUI editor; never committed, deleted; reports and screenshots in the scratchpad as `p9_*`). The office is the art `OfficeScene` with the additive `OfficeGameplay` (`OfficeScenes`), loaded by Title → New Run and Home → Sleep as the game does; no 2D scene was opened.

**How the build went against the plan.** The work landed as: the spec re-check (§0 re-check, §6.1), the `ArabicShaper` hamza fix, then Domain (`Translation`, `TranslationDay`, `TranslationContent`), Visuals (`Pseudoscript`, `FlipSequence`, `DisplayText`, the `SpeechQueue` reveal, `Paging`), the content pipeline and its generated content, the runtime, the Home shop, the Settings motion pair with the rebuilt `OfficeGameplay.unity`, and two fixes the verification found. Where the build differs from §2:
- `InvestigationUIController._docWindows` now holds the `DocumentWindowController`s themselves instead of a second list beside the `GameObject`s (one list, no parallel copy).
- **Wide glyphs in document rows** (found by the play-through, commit `fix(translation): wide glyphs shrink into their document row`): the row's horizontal layout gave a value its glyphs' preferred width, so cuneiform and hieroglyph values squeezed their label and ran past the photo. A value in the tongue now takes the row's width after its label (a `LayoutElement`, `DocumentWindowController.FillRest`) and may wrap there, and a text showing foreign cells shrinks to fit through piece 6's `UiText.FitLabel` (which gained `keepWrapping`) and its `labelMinScale` knob; its English then shows at its own size. `isTextOverflowing` did not see the problem (the rect was as wide as the text), so the play-through's fit check compares the drawn bounds with the rect and checks the label keeps its width.
- Content null guards (review): Generate World and the validator no longer assume `translation.packs`/`tongues` are present.

**Offline:** `compile_check_art.py` 0 errors in every project; the reflection runner `passed 946, failed 0` (844 at `d998ab5`; +3 `ArabicShaperTests`, +35 `TranslationTests`, +33 `PseudoscriptTests`, +9 `FlipSequenceTests`, `DisplayTextTests` rewritten (+8), +5 `SpeechQueueTests`, +9 `PagingTests`).

**EditMode suite** (TestRunnerApi, final code): 1074 passed, 2 failed, both third-party `UnitySkills` (`PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly`, known; `SkillsModeManagerTests.Migration_RepeatLoad_IsIdempotent_NoDuplicateAuditEvent`, which reads the machine's EditorPrefs, as in the office move).

**Content** (§6 step 1):
- Generate World twice: the second run changed no file under `Assets/Data` and logged no warning; the summary counts 13 tongues, 8 translator upgrades, 1 translation notice.
- `Assets/Data/World/Translation` holds `Upgrade_Tr_{NearEast,Mediterranean,EastAsia,NorthEurope}_{Papers,Speech}` and `Trigger_TranslationNotice`; the library has 12 upgrades, the 4 hand-authored ones first, then `tr_near_east_written`, `tr_near_east_spoken`, … in pack order ("Near East Translator: Papers", 100, "Translates Egyptian, Akkadian and Arabic on scanned papers.").
- The notice trigger comes after the unlock triggers and before the history triggers, is one-shot, and its only condition is `DayAtLeast 1`.
- All 48 places carry the §1.1 tongue; the library's translation reads fromDay 2, 13 tongues, 4 packs, 8 scripts, flip 0.3/0.04/0.12/2/0.15.
- Validate Content Library: no issues, 0 warnings.
- Negative checks through the generator's `CheckTranslation` on in-memory copies (nothing written), each exactly one error: a 25-cell table ("tongue 'egyptian': 25 cells, need 26"), a duplicate cell ("cell 'κ' appears twice"), an Arabic table holding U+0679 ("cannot shape (U+0679)"), a place without a tongue ("Place 'egypt_ancient' has no tongue"), a pack no tongue uses ('oceania'), `fromDay` 0, and a hand-authored upgrade with the id `tr_near_east_written`.

**Determinism** (§6 step 2): the step-0 dump (seeds 12345 and 999 × days 1–6, day 6 also under the Japan and Egypt leaders: 194 travellers, with claim, name, date, role, violator, liar, home, tells and channels, papers, answers, premade, opener, claim line, small talk, look and garments) is identical in every existing column after the piece, and case generation twice gives the same travellers. The new `tongue` column equals the claimed place's tongue for every traveller, premades included (Senenmut → egyptian, Socrates → greek).

**Font probe** (§6 step 3, on the generated library): every script's chain resolves through `RuntimeFonts` with the presenter's per-script sample (`TranslationPresenter.ScriptSample`, private since the review: every cell of every tongue in the script, upper case included, plus a–z and A–Z): hieroglyphs, cuneiform and runes `file seguihis.ttf`, Arabic `file tahoma.ttf`, Han `file msyh.ttc`, kana `file YuGothR.ttc`, Greek and the Latin-letter tongues the runtime LiberationSans; all cover their samples. Every table parses from the saved library (the YAML `\U0001xxxx` escapes round-trip). The tracked `LiberationSans SDF` and its fallback are unchanged on disk after every run.

**Builder** (§6 step 5): two builds of `OfficeGameplay.unity` hold the same objects and values (3414 dump lines, 0 differ; the second build logs no warning); against the office move's final dump only the Settings window differs (580 × 520, its rows re-anchored, `MotionLabel`, `FullMotionButton`, `ReducedMotionButton` wired to `SettingsWindowController`, the note's new text); the art office's scene file is byte-unchanged.

**Play-through in the art office** (§6 step 6; Title → New Run, seed 12345; 240 checks, 0 failures in the final run):
- **Day 1:** Lysimache (Periclean Athens, tongue greek) is not foreign: the bubble types the claim in English in its own font, the scanned passport and the transcript read in English; the morning paper has no notice.
- **Day 2:** the morning paper carries the notice exactly once. All ten travellers: each foreign tongue untranslated for both media (no translator owned). The claim types in the bubble in the tongue's script and the script's runtime font; the scanned passport shows Coin of Issue and Native Tongue in the script (cuneiform for Shat-Aya, hieroglyphs for Tuya, Arabic shaped right to left for Hunayn, Greek for Socrates, Brittonic in Latin letters for Riovassus) in the script's runtime font, the Full Name and Date of Birth in English in the window's own font; the transcript shows the desk in English and the traveller in the tongue; every value, transcript row and bubble line is drawn inside its rect with the label at its full width. Five liars proven with no translator: Awil-Ninurta's Coin of Issue (a paper tell) and the spoken tells of Socrates, Buran, Amat-Mamu and Riovassus (answer rows) go into the compare bar as "(untranslated Akkadian; Near East Translator)"-style placeholders with no foreign glyph, then against the claim's book row log "DEVIATION LOGGED — CURRENCY INCORRECT — papers: "Copper cash (Xining Yuanbao)" / expected: "Silver shekel (by weight)"" (and the Device, Capital and Language equivalents) in English; every decision correct with no citation.
- **Home:** the shop's page 1 holds the 4 hand-authored upgrades, Near East Papers and Speech and the "Page 1/2 · Next >" row, each row 44 px and inside the rows area; "Next >" shows page 2 (the other six translators); buying Near East Papers (100) and Speech (80) on page 1 and Mediterranean Papers and Speech on page 2 marks each owned, takes its price, and keeps the page. The save written at Sleep holds all four; day 3 loads them (Continue's path).
- **Day 3** (no notice in the paper; Iraq now leads, so the desk wears piece 6's Iraqi theme):
  - Arib al-Ma'muniyya (Arabic; both translators): the claim starts in shaped Arabic, its English grows in reading order at the right edge (R5), is whole at 2.08 s (the flip's duration 2.10 s) in the bubble's own font, stays up to 6.08 s (flip + hold 4 s); two answers queued at once start 2.92 s apart (the first's flip 1.42 s, longer than its 0.80 s of typing, + the 1.5 s minimum); opening the wheel 0.45 s into the second finishes it at once; the transcript reads in English. Her passport opens in Arabic in the Tahoma runtime font, rows start 0.30 s and 0.45 s after the scan, every value is English at 1.33 s (the longest row's duration) back in the window's own font; a second scan shows English at once and keeps the reveal time; the requested permit, clicked 0.4 s into its flip, finishes the whole document and selects the row ("Paper (Baghdad mills)" in the bar); its page 2 reads English.
  - Tuquztimur (Arabic): the papers' right-to-left flip mid-way and done, and his bubble mid-flip.
  - Ambrogio (Italian; both translators): the claim flips left to right from its first letter; his passport's values flip letter by letter in reading order on both rows (rows 0.30/0.45 s, English at 1.22 s, own font back).
  - Untranslated scripts: Peter's German (Latin letters; Northern Europe not owned), Chaoyun's Chinese (Han, wrapping onto two lines in its row) and Kichibei's Japanese (kana), papers and bubbles.
  - Settings ▸ Reduced (clicked in the frame; stored as `TimeDesk.ReducedMotion` = "reduced"; the note fits in three lines), then Quintus (Latin; both translators): the claim types in English from its first character and the scanned values are English at once.
- **Log:** only the expected warnings: the binder's anchor warning once per office load (the art side still lacks four anchors) and day 1's `DayOrchestrator` note that Senenmut's forced slot 3 was never reached (the test closes day 1 after one traveller). The player's preferences and save were restored afterwards.

**Screenshots** (scratchpad): `p9_tongues` (the spike); `p9_day1_readable`; `p9_day2_papers_{cuneiform,hieroglyphs,arabic,greek,latin_brittonic}`, `p9_day2_bubble_hieroglyphs`, `p9_day2_placeholder_{paper,answer}`, `p9_day2_proof_{paper,answer}`; `p9_home_shop_{page1,page2,bought}`; `p9_day3_bubble_flip_mid` (Arabic), `p9_day3_bubble_translated`, `p9_day3_transcript_translated`, `p9_day3_flip_mid`/`_done` (Arabic, Arib), `p9_day3_flip_mid_arabic`/`_done_arabic` (Tuquztimur), `p9_day3_skip_click`, `p9_day3_bubble_flip_arabic`, `p9_day3_bubble_flip_mid_italian`, `p9_day3_flip_mid_italian`/`_done_italian`, `p9_day3_papers_{latin,han,kana}`, `p9_day3_bubble_{latin,han,kana}`, `p9_day3_settings_motion`, `p9_day3_reduced`. Read by eye: no tofu in any script; Arabic joined and right to left, digits and Latin runs in order; Han and kana full-width and wrapped inside their rows; cuneiform wraps onto three lines inside its row.

**Open items found:**
- Hieroglyphs draw correctly but faintly: Segoe UI Historic's signs are hairline drawings, and a long value or line shrinks them further. Levers for the "flesh it out" pass: a per-script weight (an SDF face dilate on the script's runtime material), a larger minimum size, or Noto Sans Egyptian Hieroglyphs first in the chain.
- The scan wakes the PC but does not open the frame, so a papers flip often plays on the office PC's cloned screen (§7).
- A right-to-left line types from the left of its visual string, and untranslated Arabic never wraps (the shaper's no-break spaces), so long Arabic lines shrink instead (§7).
- Greek `σ` and `ς` both upper-case to `Σ`, so capital J and P look alike in Greek (cosmetic; a table edit).
- The Home shop's runtime rows are white text on the Home panel's current cream background (it predates this piece; the rows now fit).

**Self-review** (`git diff main..HEAD`, read as a reviewer would, with the intent audit):
- *Redone or overridden decisions:* piece 7's `DisplayText` identity becomes the real rule and `TextMedium` goes, as piece 7 planned; piece 8's pacing is unchanged unless a line carries a reveal; piece 6's `RuntimeFonts` gains a key rather than a second loader, and `UiText.FitLabel` a `keepWrapping` flag rather than a second fit rule; `ArabicShaper` gets a bug fix, not a new path. `PagedRowsWindow` now calls the shared `Paging`, so the page maths has one home.
- *Coded where data or existing systems sufficed?* No: translators are ordinary `UpgradeSO`s in the existing shop and save, the notice is an ordinary one-shot trigger, the day gate is the day-start `GateSnapshot`, and evidence code is untouched (untranslated rows compare on `CompareEvidence`'s canonical value).
- *Found and fixed:* the wide-glyph row layout (above); null guards on a translation section without packs or tongues (generator and validator); `TranslationPresenter.ScriptSample` made private (its only caller is the presenter). Each fix re-ran the offline gates (946/946); the final EditMode suite and play-through ran on the fixed code.
- *Checked and left:* `DocumentWindowController.Update` runs per clone but returns at once without flips; a document's flip clock is unscaled time (R7) while the bubble's follows the game's delta time (piece 8), which only differ if the game is ever time-scaled; the presenter warns once per script per office day.
