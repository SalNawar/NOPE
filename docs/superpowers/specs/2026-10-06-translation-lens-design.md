# The Translation Lens and the language lock: design

*2026-10-06 · `feat/translate-lens` · decided and built by Claude for Saleh's review (no questions asked: every choice below is the default, his to overturn) · the code wins over this text; the behaviour contract is `docs/FEATURES.md` (Translation: "The Translation Lens"; Fake-OS desktop: Settings' Language)*

Saleh, verbatim: "let's lock changing language after week 1 and only after the player unlocks a new ability which translates any word the player hovers on. When a word is being translated the letters flip back to English. The player can upgrade to translate sentences and then entire objects, but always with this effect."

## 1. What it means in the game

| Days | Settings' Language | What the office's labels show | The lens |
|---|---|---|---|
| 1-7 | a free choice: Follow history / Always English | the leading culture's labels (Follow history) or English | none (unless granted by a cheat) |
| 8 on | both buttons disabled, Follow history shown chosen, a line under them: "Locked from day 8: the office reads the world's language now. Hover a word to translate it (the Translation Lens)." | always the leading culture's labels | level 1 issued by the Bureau; levels 2 and 3 from Orders |

The lens's three levels, each with the same letter flip:

1. **Word** (issued on day 8): hovering a word of a culture label flips that word's letters, one by one, into its English; leaving flips them back.
2. **Sentence** (Orders, 60 cr): hovering anywhere on a label flips the whole label into its English sentence (the label's own English, so the word order is English: "اليوم 8 — الإحاطة الصباحية" lands as "Day 8 — Morning Briefing", not word for word).
3. **Object** (Orders, 120 cr, after Sentence): hovering anywhere on an object (a PC window, or a top panel such as the morning paper or the evening ledger) flips every label on it, one label after another (`cascadeSeconds`).

The flip: each letter (cell) starts `letterInterval` after the one before, passes through `scrambleSteps` letters of the word it leaves and lands `letterSeconds` later (`translation.lens.flip`, the speech flip's `FlipTiming` shape and `FlipSequence` clock). Two texts of different lengths flip over the longer one's cells (a cell past a text's end shows nothing), so a two-character Japanese word grows into "Settings" as its cells land. Reduced motion (Settings' Motion) swaps at once.

## 2. Decisions made on Saleh's behalf

1. **The lock day is the lens's introduction day, not a number.** The ramp introduces `tool:lens` on day 8 (`days[].introduces`, `Feature.Lens`); `TranslationLens.LanguageLocked(day, Introductions)` is "the lens is introduced by today", and the Settings line prints `TranslationLens.LockDay`. Moving the lens in the ramp moves the lock with it (design rule 4: balance in content).
2. **Week 1 is days 1-7, so the lock starts on day 8**, the day foreign speech also starts (`translation.fromDay` 8) and the Work Permit arrives. The day 8 bulletin names the lens ("the Bureau issues you a Translation Lens"), and the rulebook's GUIDE adds the page "THE TRANSLATION LENS" (`guide.pages` `lens`, point `pc`), so the bulletin-names-its-page rule (`GuideContent.BulletinProblems`) holds; day 8 has two pages now.
3. **The first level is issued, not sold.** Locking the language without a way to read it would trap the player, so the Bureau issues the Word lens on the lock day (`TranslationLens.Reach`: level 1 from the lens's day, or when its id `lens_word` is owned). Levels 2 and 3 are ordinary Orders upgrades (`translation.lens.levels`, written by Generate World as `Upgrade_Lens_*` in `Assets/Data/World/Translation`, Interview band, delivered the next morning). A level counts only on top of the one before it; Sentence may be bought before day 8 and starts working on day 8.
4. **No parallel system, but speech keeps its translators.** The lens is built on the same parts as speech translation: the Orders tree and its delivery, `FlipTiming`/`FlipSequence`, `MotionPreference`, `ArabicShaper`, the culture's runtime fonts, `UiText`. The lens reads the office's written language (the culture labels); the traveller's spoken tongue is a different axis (the tongue of the place they claim, not the world's leading culture) and stays with the four regional Speech translators, which flip the bubble's line on their own. Folding the Speech translators into the lens (hover an untranslated answer word to read it) is an open option for Saleh, not done: it would remove the speech puzzle's cost.
5. **What the lens reads: every culture label, wherever it is shown.** `UiStrings` remembers each culture label it returns (`UiStrings.Phrases`: the label as drawn, its English, its words); the presenter finds those labels inside any UI text of the loaded scenes (at word boundaries, longest first), so a label set by a theme tag, by code (`briefing.title`, `tray.day`) or inside a longer text (the paper's news header in its body, the citation's title) is found. Papers at the desk, books and the rulebook's pages are English by Saleh's standing rule ("all documents must be filled in English") and have nothing to translate; the lens would read them if they ever carried a culture label.
6. **Word English comes from a per-language glossary in content** (`ui.languages[].words`: the word as the labels write it and its English). Generate World and the validator refuse a culture label with a word the glossary lacks (`LensWords.TableProblems`), so level 1 never meets a word it cannot read. A label of a single word and nothing else takes the label's English (no glossary row needed: "الإعدادات" → "Settings"). Matching ignores case and accents (Greek capitals, a final sigma). A word of an all-capitals label shows in capitals.
7. **Chinese and Japanese words are the glossary's**: a run of Han or kana (no spaces) is cut at the glossary's words, longest first ("朝のブリーフィング" → "朝の" + "ブリーフィング"); letters no glossary word starts with stay together as one word.
8. **Right to left**: a label is composed in logical order with the flipped word in place and shaped by `ArabicShaper.ToVisual`, so an English word or sentence reads left to right inside the right-to-left line and a whole label lands as plain English. The shaper's source map (`ToVisual(logical, sources)`) maps each drawn character back to its word for the hover.
9. **Layout stays still**: while a label is translated its auto-size is held at the size it shows and eases (`resizeSeconds`) to the size at which its English fits its box (never larger, never below the auto-size floor); leaving restores its own size and auto-size. Wrapping texts keep their size and wrap.
10. **Occlusion**: a label under another window does not translate (the topmost UI element under the pointer decides the object; a window open over a panel is an object of its own).
11. **Zero work at rest**: the presenter does nothing while the pointer rests and no flip runs (a settled translation is not rewritten), and finds the scenes' texts again only when a scene loads, the culture's labels change, or every 5 s while the pointer moves.
12. **The cheat hook**: `TranslationLens.Grant(LensReach level, ICollection<string> owned, IReadOnlyList<string> levelIds)` adds the level ids up to `level` to the owned upgrades (`WorldState.unlockedUpgradeIds`; `library.Translation.lens.rules.levelIds`). Granting Word before day 8 lets the lens read in week 1 (the language stays free). The cheat menu's "More (other tracks)" calls it (`TranslationLensCheats`: Lens: Word / Sentence / Object / remove; Language: lock now / unlock / follow the ramp, through the session-only `CultureThemeService.LockOverride`); the lens reads the owned ids on the next pointer move, no refresh needed.

## 3. The parts

- **Domain** (`Assets/Scripts/Domain/TranslationLens.cs`, tested: `TranslationLensTests`): `LensReach`, `LensWordRef`, `LensRules` (the level ids), `TranslationLens` (`LanguageLocked`, `LockDay`, `Reach`, `Grant`, `Covered`: what a hover covers, `Problems`); `Feature.Lens` (`Introductions`).
- **Visuals** (`Assets/Scripts/Visuals/LensText.cs`, tested: `LensTextTests`): `LensWord` (a glossary row), `LensSegment`, `LensPhrase` (a label's words, `Compose` with words replaced, `ComposeWhole`), `LensWords` (`Split`, `Missing`, `TableProblems`, `Fold`, `Glossary`), `LensFlip` (`Frame`, `Duration`, `Cells`); `UiStrings` takes the glossary and keeps `Phrases`.
- **Runtime**: `TranslationLensPresenter` (UI/Translation; added beside `CultureThemeService` by `CultureThemeBootstrap`: hover, cascade, resize, reduced motion; `PointerOverride` for the probe); `CultureThemeService.LanguageLocked` (Always English ignored from the lock day); `SettingsWindowController` (buttons disabled, the lock line); `TranslationLensSettings` in `TranslationSettings.lens` (levels, flip, cascade, resize); `UiStringTableSO.words`.
- **Builder**: the Settings window's `LanguageLockText` (a caption under the language pair, hidden until the lock); OfficeGameplay rebuilt.
- **Content**: `translation.lens` (levels, flip, cascadeSeconds, resizeSeconds), `ui.languages[].words` (six glossaries), `ui.strings` `settings.languageLocked`, day 8's `introduces` and bulletin, the guide page `lens`; content sheets `translationLens`, `lensLevels`, `uiLanguageWords` (`ContentSheetMap`), the template rewritten.

## 4. Open points for Saleh

- Fold the regional Speech translators into the lens (hovering untranslated speech), or keep speech separate (built: separate).
- Only the 30 flavour labels are translated per culture today, so the lens has those to read (PC chrome, the paper's masthead, title and news header, the ledger, the citation, MATCH/MISMATCH). Making more of the office foreign (window bodies, menus) is content: add the keys' culture text and the glossary words.
- The decision controls keep their English gloss under the culture label (Accept, Deny, START SHIFT...): with the lens they could drop it from day 8.
- Prices (60 and 120 cr) are first guesses; no balance simulation was run (the lens changes no money flow unless bought).

## 5. Verification

Reports in `SCRATCH/translate`: compile 0 errors; the offline runner and EditMode (TimeDeskEditMode) 0 failures; Generate World twice (the second run changes nothing) and the validator; Build Office UI (the builder changed: OfficeGameplay rebuilt and committed); the audit play days 1-3; the lens probe (`probe/probe_report.txt`, screenshots at 1920x1080 and 1280x720: day 7 the language changes, day 8 it is locked; Arabic and Japanese at each level on the morning paper and the PC, frame sequences of the flip).
