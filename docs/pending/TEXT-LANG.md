# TEXT-LANG: the player can always read the game

Branch `fix/a-text-lang`, cut from `fix/p0-cuts` (5aecb60). Phase A, merge #12. Sources: FIX_PLAN.md §3 "TEXT-LANG" and §0, FIX_PLAN_v2.md §3 "TEXT-LANG: delta (D7 scope decided)" (v2 wins), BUGLIST.md (BUG-3, 37, 38, 43, 62), DECISIONS_1019a.md, GAME_FEEL_BIBLE.md (rows B6, D4, F3; G1, G3), PHASEB/LEASES.md, docs/pending/CARVE.md and CUTS.md.

**What it is:** BUG-3a (surfaces, records, settings, bulletin), BUG-37a (the Arabic shaper and its per-line API), BUG-38a (fonts, atlases, capitals, the CJK bake), BUG-43b (one spelling) and BUG-62 (the PC basics in Settings).

**What it is not:** no Unity run (code-only: Saleh may be on the PC). `docs/FEATURES.md` is not edited (INTEGRATION folds this file in; the lines to rewrite are listed below). OfficeGameplay is not rebuilt or committed (INTEGRATION's). No WorldState field, so no save-version bump.

## Saleh's calls this track builds (quoted from DECISIONS_1019a.md)

- **D7:** "papers will change language and the pc background ... its a pc you set the language but you should see effects on it like maybe color changes". The glasses work "on news paper and documents".
- **R2:** culture effects "accumulate through day 7 and become visible from day 8" (today's trigger, the lens's day 8, stays: v2 "the culture day: today's lens day stays the trigger").
- **D20 / R3-R4** (for the seam FLOW calls): "if you press escape you should auto open the pc setting menu"; "Esc opens the same Settings window everywhere".
- Standing: everything diegetic; D2 "everything should be animated and satisfying responsive and gives good feedback".

Everything else below that is a design choice is marked **proposal (ours)**.

## What changed, per BUG id

### BUG-3a: surfaces, records, settings, bulletin (S1)

- **Surfaces (v2: decided).** `TextSurface` gains `Paper` and `Sign` (appended; serialized nowhere). `CultureChoice.Surface`:
  - `Pc` → `EnglishOnPc` (unchanged);
  - `Desk` (the chrome: report, Home, citation, verdict, wheel, guide, memo, briefing, the desk's controls and words) → the new `LabelLanguage.PlayerLanguage`, whatever the papers read;
  - `Paper` and `Sign` → the culture rule (`CultureChoice.FollowsWorld`).
  - **Signs follow Paper:** proposal (ours), as v2. No sign asks for the Sign surface yet (the hall's signs are HALL-FX's and DESK-PLAN's files); until one does, signs read the player's language.
- **One lookup per surface.** `UiStrings.ForSurface(surface, key, args)`: a paper or a sign by the culture rule (`Format`), every other surface in the reading language (`English`). `UiText.Get`/`Format` (the chrome's) and `UiText.Pc` now both read the player's language; `UiText.Get(key, TextSurface)` routes by surface; only `UiText.FormWords`, `DocumentWord` and `InReadingLanguage` (a paper's printed words) follow the culture. Every existing `UiText` caller in the chrome therefore reads the player's language without touching its file.
- **Theme tags.** `CultureThemeService.ApplyText` keeps a themed text's kit face (else the font it was built with) and its label in the player's language; the culture font is the papers' only (`CultureFont`, the global fallback).
- **The setting never locks and applies at once** ("you set the language").
  - `CultureChoice.EnglishBySetting(stored, cultureDay)`: "Always English" keeps the papers English on any day; "Follow history" gives them the world's language on any day; no choice reads English until the culture day and the world's language from then on (LANG's default of 2026-10-08, kept).
  - `UiLanguagePreference.Choose(bool)`; `SettingsWindowController.Choose` stores it and calls `CultureThemeService.RefreshActive()` at once; both buttons always work.
  - `CultureThemeService.LanguageLocked` keeps its name (TranslationLensCheats, TOOLS' file, reads it) but now only means "the culture day reached": the default's switch, never a lock.
  - Papers already on the desk keep their print (they are printed at bind by DeskDocument, DOC-HANDLE/DOC-LOOK's). The line under the pair says so (`settings.languageDesk`, rewritten). A culture font that stops being the papers' stays in TMP's fallback list until the next scene loads, so a paper printed before the change never shows boxes (CODE-80).
- **Records (CODE-45, CODE-79).** The citation slip, Mail's copy (`CaseVerdict.citationText`), the visit record (`citationReason`) and the untranslated placeholder are built from `UiText.Get`/`Format`, which now always read the player's language, so no record holds world-language text.
  - **Decision (ours):** they stay rendered text, not keys. The player's language is the reading language for the whole run (the setting only moves the papers), so a rendered record is lossless. A key-based record would need fields in `ShiftLedger.cs` / `Visits` / `WorldState.cs`, which this track does not own (ShiftLedger is unowned, WorldState has no Phase A owner); it belongs with INTEGRATION's one coordinated save-version bump if a player-language selector is ever added.
- **Bulletin.** `GameManager.TodaysBulletin`'s `briefing.newHours` reads the player's language (the chrome), so it no longer turns Greek inside English content.
- **CODE-159.** `EvidencePresenter`: the desk's compare strip uses the chrome's words (`UiText.Category`, `UiText.Deviation`).
- **Day reset (seam for RUN-SAFE, BUG-27a).** `CultureThemeService.ResetForNewDay()` also clears the cheat's culture-day override (`LockOverride`), not only the forced leader. The player's Settings choice is not day-scoped and stays.
- **Not this track's (3b and others):** Home's hard-coded strings (`HomeUIController`, `HomeManager`) are HOME's (3b). `Domain/HouseEffects.cs` (in this track's Owns) is left as it is: its words are English, the player's language, and moving them to keys changes the signature `HomeUIController` (HOME) calls; HOME-3D replaces the House panel. The hand-off is in "What each track must know".

### BUG-37a: the shaper and its per-line API

`Visuals/ArabicShaper.cs`:
- **Tags are tokens (CODE-43).** A known TMP tag (`<b>`, `<size=60%>`, `<color=#…>`, `<noparse>`…) has no direction: it never reorders or mirrors, and each styled run is re-emitted with balanced tags in visual order. A standalone tag (`<br>`, `<sprite>`…) stays where it lies; text inside `<noparse>` is text; a `<` that opens no known tag is text.
- **Wrap on logical text, then reorder per line (CODE-42).**
  - `ToVisualLines(lines)` (the P0 seam, same signature): each logical line into visual order, **plain spaces kept**, a tag open across a line break closed and reopened, so every line is balanced.
  - `Wrap(logical, width, measure)` (new): breaks the logical text by a measure (a text box's preferred width of the visual line), words whole, a word wider than the line broken first-letters-first, then `ToVisualLines`.
  - `ToVisual` (one string) keeps no-break spaces in a right-to-left run, so a box that still wraps a whole visual string never breaks inside a phrase.
- **Shape exactly once (CODE-44).** `IsVisual(text)`: a text holding a shaped letter and no letter left to shape is returned unchanged by `ToVisual` and `ToVisualLines` (`ToVisual(ToVisual(x)) == ToVisual(x)`). `ToLogical(visual)` undoes `ToVisual` (ligatures back to lam + alef, no-break spaces to spaces). `UiStrings.Format` turns a shaped argument back into logical text before a right-to-left template is filled, so the label is shaped once, at the outermost format.
- **RTL bodies right-aligned.** `IsRightToLeft(text)`: the first letter, tags and digits skipped.
- `DisplayText`: a re-emitted tag character has no source letter (-1), so `Typed` stays correct.
- **Mixed direction:** numbers, `%`, signs, decimal points and bracketed Latin keep their order inside a wrapped Arabic line (tested).
- **GUIDE arrows (CODE-164):** the rulebook and the guide are chrome now, so their `<`/`>` arrows are never shaped or mirrored.

### BUG-38a: fonts, atlases, capitals, the CJK bake

- **Zero LiberationSans, built.** `OfficeSceneUIBuilder.Text()` and SceneUiKit's text makers (`FindOrCreateText`, `FindOrCreateButton`, `FindOrCreateInputField`) set a new text in the kit's body face (Outfit Regular; bold: Outfit Bold). The kit pass ends with `KitRefont`: every text on every root of the office's scene still in LiberationSans (or no font) is set in the kit's body face, and the count is logged. `SceneUiKit.Refont(root)` is public for HOME's Home and Title builders.
- **Zero LiberationSans, at run time.** `CultureThemeService` makes the loaded UI kit's body face TMP's default for the session (a text made at run time with no font gets it), and sets every loaded scene's text still in LiberationSans in the kit's face as the scene is themed (the art scene's 42 and AnimeHall's 5 included, in memory only: no scene file changes). The project's default comes back when the service goes; the TMP Settings asset is never saved.
- **Atlases.** `Tools > TimeDesk > Text > Extend Kit Font Atlases` (`Editor/TextLang/KitFontAtlases.cs`) adds Latin-1, Latin Extended-A and Greek to the five kit faces' static atlases in place (same assets and GUIDs; more atlas pages when one is full). **Finding:** the kit's TTFs have Latin-1 and most of Extended-A but **no Greek** (fontTools: BigShoulders 1/144 Greek codes, GeistMono 4, Outfit 0, UnifrakturMaguntia 0). Greek therefore stays the fallback's: Greek is printed only on papers now (the culture's runtime font draws it). **Run deferred: Unity.**
- **Capitals.** The chrome no longer prints a culture's words, so TMP's UpperCase never meets a Greek word there (UI-19); a paper's words in capitals go through `ArtLayout.Capitals` (`UiText.FormWords`, unchanged). The dead "culture label in capitals" branch of `ApplyText` is gone.
- **CJK bake (CODE-81).** `RuntimeFonts`:
  - one runtime asset per OS font file and face (or family and style), shared by every key that resolves to it (a culture and a translation script);
  - sampled at 48 pt (was 90; padding 5);
  - coverage read from the font files through FontEngine (`TryGetGlyphIndex`), so resolving bakes nothing;
  - `Prewarm(asset, characters, perFrame)`: `CultureThemeService` bakes a culture font's glyphs a few per frame once it becomes the papers' (`CultureUiSettings.prewarmGlyphsPerFrame`, default 12; not in world_source.json, so the default holds until a designer sets it).
  - **Proposal (ours):** the prewarm starts when the culture font first becomes the papers', not "during the night"; the next day's leader is not known at night.

### BUG-43b: one spelling

`world_source.json` `ui.strings`: `case.roleUnknown` "Traveler" → "Traveller", `citation.deniedWrong` "traveler" → "traveller". Every citation string now says "traveller". (The culture tables translate the word; their text is unchanged.)

### BUG-62: the PC basics in Settings (S3)

- **Rules (pure):** `Visuals/SettingsRules.cs`: `WindowMode`, `SoundChannel`, `ScreenSize`; `Decibels` (20·log10, floor -80 dB), `Resolutions` (from 720p up, refresh rates merged, the current size always offered), `IndexOf`, `Step`, `Exposure` (±1 EV around the middle), `Percent`.
- **Preferences, applied at once:** `UI/Theme/SoundSettings.cs` (Master, Music, Effects; the mixer's exposed volumes through `Sounds.Bank.mixer`, else the listener's volume), `UI/Theme/ScreenSettings.cs` (window mode and resolution through `Screen.SetResolution` once the player chose one; vsync; brightness as a global Volume's post exposure, made on the first move away from the middle; `ReduceFlashing`, the static preference HALL-FX and LIGHT read), `UI/Theme/SettingsBootstrap.cs` (applies the stored choices at startup).
- `TimeDesk.mixer`: `MasterVolume`, `MusicVolume`, `UiVolume`, `DeskVolume`, `AmbienceVolume` exposed (the groups' own volume parameters).
- **Controller:** `SettingsWindowController` gains the basics' fields and handlers (null-safe), and `Open()` (static: brings the Settings window up, for FLOW's Esc).
- **Rows:** `Editor/OfficeSceneUIBuilder.Settings.cs` `BuildSettingsBasics(column)`: a Sound group (three volume sliders with values) and a Display group (Windowed / Borderless / Fullscreen, the resolution stepped with ◀ ▶, VSync Off / On, Brightness, Reduce flashing Off / On), in the Settings window's own row, slider and choice styles, wired to the controller.
  - **Not called yet.** The Settings window is UI-PC's (`.Apps.cs` `BuildSettingsWindow`, its 800 × 1010 size in `DesktopConfigSO`, already full). UI-PC (#13) inserts `BuildSettingsBasics(column);` before the note and gives the window room (a scroll or a second column). Until then the basics apply from stored preferences and code, but have no rows (deferred to #13; see RESULT).
- **Strings:** the new `settings.*` words in `world_source.json` (tier Full: the chrome reads the player's language).
- **Brightness needs post-processing on the cameras** (LIGHT's BUG-51, merge #11, before this one). Proposal (ours): a separate global volume at priority 1000 that overrides only post exposure, and is off in the middle.

## Feel bible rows (acceptance)

| Row | Requirement | How it is met |
|---|---|---|
| F3 Settings via Esc | "Settings has volume, Camera sway, Reduced Motion, Motion intensity, Reduce flashing (BUG-62)"; `ui_toggle` on each switch | Volume and Reduce flashing added (controller + rows); Camera sway, Reduced Motion and Motion intensity stay. Esc is FLOW's (`SettingsWindowController.Open()` provided). The `ui_toggle` cue is FEEDBACK's (the sound bank, BUG-33). |
| G1 Response | The first visible change on the press frame | Each Settings choice applies and repaints in its click handler (same frame). |
| G3 Flash safety | "Reduce flashing … turns every flash into a static tint held 200 ms" | `ScreenSettings.ReduceFlashing`, the static preference; the flashing effects that honour it are HALL-FX's, LIGHT's and UI-PC's (`FlashPolicy`, gap 2). |
| D4 AVAILABLE | Blocking bugs 3 and 38 | Its caption reads the player's language in a kit face (chrome, `UiText`; run-time sweep). |
| B6 Citation slip | Blocking bug 43 (text) | One spelling; the slip's words are the chrome's. |

## Tests

| Check | fix/p0-cuts | fix/a-text-lang |
|---|---|---|
| `compile_check_any.py` | 0 errors | 0 errors (Assembly-CSharp-Editor and TimeDeskEditMode exit 0) |
| Offline runner (TimeDeskEditMode) | passed 3710, failed 0 | passed 3782, failed 0 |
| Editor-side TEXT-LANG tests outside Unity (scratch runner over Assembly-CSharp-Editor, `NeedsEditor` skipped) | - | passed 4, failed 0 |

**EditMode (offline):**
- `ArabicShaperTests`: tags are tokens and balanced (3 cases), nested and valued tags, noparse text, a lone `<`, shape-once guard (5), `ToLogical` undoes `ToVisual` (12), wrap then reorder, a long word, mixed direction in a wrap, a tag carried across lines, no width, `IsRightToLeft` (5), `ToVisualLines` with plain spaces (replaces the P0 stub's test).
- `CultureChoiceTests`: `EnglishBySetting_NeverLocks` (8 cases), Always English holds on the culture day, the PC (5) and the chrome (5) never read the culture, a paper and a sign follow it (5), only the papers on the culture day. `ChoseEnglish`'s test is gone with the method.
- `UiStringsTests`: surface routing for every UI key of today's content in Greek and in Arabic (no chrome or PC key resolves to the culture; papers do, >900 keys), a shaped argument is shaped once.
- `SettingsRulesTests` (new): decibels, resolutions, nearest index, step, exposure, percent.
- `TextAuditTests` (new): accented Greek capitals, culture labels on player surfaces.
- `UiStringsTests.EveryLiteralKeyTheScriptsLookUp_IsInTheReadingTable` covers the new `settings.*` keys.

**Editor-side (Assembly-CSharp-Editor, Unity's EditMode suite):**
- `TextLangSeamTests`: ResetForNewDay clears the forced leader and the culture-day override; UiKeys; `SettingsWindowController.Open()` is false with no window.
- `TextLangEditorTests`: the kit charset (offline), and in `NeedsEditor`: the re-font sweep, the kit faces draw today's Latin strings themselves, OfficeGameplay sets no text in LiberationSans, the chrome reads the reading table (and "Traveller"), the Settings basics' rows build and wire.

**QA-RIG gate registration.** `Assets/Editor/QA/Checks/TextLangChecks.cs` needs QA-RIG's `GateCheck`, which is not on this branch's base (fix/p0-cuts), so it is written at INTEGRATION once QA-RIG (#2) is in:

```csharp
/// <summary>TEXT-LANG's gate checks (BUG-3a, BUG-38a): the shipped scenes' fonts, the kit atlases, and a play's takeover grep.</summary>
public static class TextLangChecks
{
    /// <summary>OfficeGameplay sets no text in LiberationSans (BUG-38a).</summary>
    [GateCheck("textlang.officeGameplay.liberationSans", "TEXT-LANG")]
    public static GateResult OfficeGameplayFonts()
    {
        var files = TextLangAudit.LiberationSansInFiles(GateResults.ProjectRoot).Where(f => f.StartsWith("Assets/Scenes/OfficeGameplay.unity")).ToList();
        return GateResult.Of("textlang.officeGameplay.liberationSans", "TEXT-LANG", files.Count == 0, $"{files.Count} file(s)", string.Join(", ", files));
    }

    /// <summary>Every kit face draws today's English, German and Italian strings itself (BUG-38a, CODE-83).</summary>
    [GateCheck("textlang.kitAtlases", "TEXT-LANG")]
    public static GateResult KitAtlases()
    {
        var gaps = TextLangAudit.KitAtlasGaps(Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath).contentLibrary);
        return GateResult.Of("textlang.kitAtlases", "TEXT-LANG", gaps.Count == 0, $"{gaps.Count} face(s) with gaps", string.Join(" | ", gaps));
    }

    /// <summary>The takeover grep: 0 culture text on the chrome or the PC over days 7-9 (BUG-3a).</summary>
    [GateCheck("textlang.play.takeover", "TEXT-LANG", GateKind.Play)]
    public static GateResult Takeover() => GateResults.Require("textlang.play.takeover", "TEXT-LANG", "textlang.takeover.*");
}
```

QA-RIG's own `qa.scenes.liberationSans` and `qa.play.liberationSans` (owner TEXT-LANG, known red) stay; the run-time sweep is what turns `qa.play.liberationSans` green.

## Seams: provided and consumed

| Seam | Provided to | State |
|---|---|---|
| `ArabicShaper.ToVisualLines(lines)` (P0 signature), `Wrap`, `IsVisual`, `ToLogical`, `IsRightToLeft` | DOC-LOOK (#9: DeskRulebook, TmpFormText, 37c), CONTENT (#17: CaseFactory, 37b), TOOLS (TextFlip, see below) | Real at this merge; DOC-LOOK's calls written against the stub now get the per-line behaviour |
| `CultureThemeService.ResetForNewDay()` | RUN-SAFE (#3) | Refined; RUN-SAFE's day-scoped test re-runs here |
| `UiKeys` | HOME, UI-PC, FEEDBACK | Unchanged (every key now reads the player's language anyway) |
| `SettingsWindowController.Open()` | FLOW (#15, Esc → Settings) | New |
| `OfficeSceneUIBuilder.BuildSettingsBasics(column)` | UI-PC (#13) | New; UI-PC calls it |
| `ScreenSettings.ReduceFlashing` | HALL-FX, LIGHT, UI-PC (`FlashPolicy`) | New static preference |
| `SceneUiKit.Refont(root)`, `SceneUiKit.SetBodyFace` | HOME (Home and Title builders) | New |
| Consumed: `Sounds.Bank.mixer` (FEEDBACK's bank, read only), `TranslationLens.LanguageLocked` (TOOLS, read only) | - | - |

## What each track must know

- **UI-PC (#13):** insert `BuildSettingsBasics(column);` in `BuildSettingsWindow` before the note, and make the Settings column scroll or split (the window is at its 1010-unit maximum). The window keeps `languageLockText` (the line under the language pair, now always shown). Until this lands, BUG-62's rows are missing (deferred criterion: the Settings [BP] recipe runs at #13).
- **DOC-LOOK (#9):** `ToVisualLines` now keeps plain spaces and balances tags per line; for a box you lay out yourself, `ArabicShaper.Wrap(logical, width, s => tmp.GetPreferredValues(s).x)` gives the visual lines, and `IsRightToLeft` says when to right-align. Pass logical (unshaped) text: a shaped line is treated as already visual. A paper already printed keeps its print after a language change; to follow at once, DeskDocument could re-bind on `CultureThemeService.LabelsChanged` (yours to decide).
- **TOOLS (#10):** `TextFlip` (speech bubbles, transcript) still hands TMP one visual string from `DisplayText.For`. Wrapped Arabic speech needs the same per-line layout (`ArabicShaper.Wrap` with the bubble's width). This was not in any track's list (CODE-42 names DisplayText, which is pure). Gap, filed to INTEGRATION. `TranslationLens.LockDay` has no caller now (it fed the removed lock line): cleanup list.
- **HOME (#14):** call `SceneUiKit.Refont(canvas)` at the end of the Home and Title builders (TitleScene has 2 LiberationSans texts). The words in `Domain/HouseEffects.cs` are English, the player's language; moving them to keys (3b) needs `HouseEffects.Line` to take a lookup, a change in TEXT-LANG's file: ask INTEGRATION to reassign `Domain/HouseEffects.cs` to HOME for it (HOME-3D replaces the House panel in Phase B).
- **CONTENT (#17):** CaseFactory's citation lines are chrome (`UiText.Get`/`Format`: the player's language); `UiText.DocumentWord` inside a citation still gives the paper's word. `UiStrings.Format` already de-shapes a shaped argument, so 37b needs no shaping change.
- **FLOW (#15):** `SettingsWindowController.Open()` brings Settings up once the PC is open. The day-shift `briefing.*` words are the chrome's.
- **LIGHT (#11):** brightness uses a global Volume (priority 1000) overriding only post exposure; it shows only where the camera renders post-processing.
- **HALL-FX / LIGHT / UI-PC:** read `ScreenSettings.ReduceFlashing` where you flash (G3).
- **RUN-SAFE (#3):** re-run the day-scoped test (BUG-27a) at this merge; `ResetForNewDay` now also clears `LockOverride`.
- **LANGUAGE (Phase B; leases CultureChoice.cs, CultureThemeService.cs, CaseTranslation.cs, TranslationPresenter.cs):** the culture day is `CultureThemeService.LanguageLocked` (the lens's day today); R2 moves it to one config value. Speech stays as it was.
- **Art side (GPT mailbox, via INTEGRATION):** `OfficeScene.unity` has 42 and `AnimeHall.unity` 5 texts set in LiberationSans. The run-time sweep sets them in the kit's face in memory; the files themselves fail a static scan until the art side changes them (request: set them in Outfit Regular SDF, `Assets/Fonts/Kit`).

## Decisions taken in this track (ours, not Saleh's)

1. The desk's controls, the rulebook (pages, tabs, GUIDE) and the AVAILABLE/DETAIN captions are **chrome** (the player's language): BUG-3's symptom names them, and the rulebook is the Bureau's own reference. Only a paper's printed words and the newspaper follow the world.
2. **Signs follow Paper** (v2's proposal), but no sign opts in yet, so signs read the player's language until their owner asks for `TextSurface.Sign`.
3. **The newspaper:** its story text is content (English, the world's events); its keyed words (`briefing.title`, `briefing.empty`) are the briefing's chrome. Translating the newspaper is GLASSES' (Phase B).
4. **Records stay rendered** in the player's language (see BUG-3a); no WorldState field, no save bump.
5. **The language line** says the change reaches papers from the next one printed, instead of reprinting the desk's papers (their print is DOC-HANDLE/DOC-LOOK's).
6. **Greek is not baked into the kit atlases:** the kit's fonts have no Greek glyphs. Greek is a paper-only script now and draws from the culture's runtime font.
7. **The prewarm starts when a culture font becomes the papers'**, a few glyphs a frame, rather than at night.
8. **Brightness** is a separate global volume; **window mode and resolution** are applied only once the player chose them.
9. `CultureThemeService.LanguageLocked` keeps its name for the cheat menu's file (TOOLS'); its meaning is now "the culture day".

## FEATURES and CHEATS (for INTEGRATION)

Rewrite these `docs/FEATURES.md` lines (line numbers as of 5aecb60):
- **79 (Menu: Settings):** Language is "Follow history / Always English" **for the papers and the newspaper only**, never locked, applied at once (papers already printed keep their print); no choice reads English until the culture day (the lens's day), the world's language from then on (`CultureChoice.EnglishBySetting`; tested: `CultureChoiceTests.EnglishBySetting_NeverLocks`). The chrome and the PC always read the player's language (`CultureChoice.Surface`, `UiStrings.ForSurface`; tested: `CultureChoiceTests`, `UiStringsTests.ForSurface_EveryKey_OnlyAPaperOrASignReadsTheCulture`). The line under the pair is the new `settings.languageDesk`; drop the "Locked from day 8" text and `settings.languageLocked`, and `CultureChoice.ChoseEnglish`. Add the PC basics: Sound (Master, Music, Effects), Display (window mode, resolution, VSync, Brightness, Reduce flashing), remembered per player outside the run save, applied at once (tested: `SettingsRulesTests`), once UI-PC's window shows the rows.
- **156 (the Translation Lens):** "from that day the desk's language follows the world's leading culture whatever Settings holds" becomes "from that day the papers read the world's language unless the player chose Always English"; hovering reads the papers' printed words (the chrome no longer shows culture labels). The cheat's lock is cleared at a new day.
- **236 (UI strings):** "Arabic shaped and ordered for display and never broken inside a phrase" becomes: Arabic shaped once, tags kept balanced, wrapped on its logical text then ordered per line (`ArabicShaper.Wrap`, `ToVisualLines`; tested: `ArabicShaperTests`); culture labels are remembered for the lens only on the papers.
- **237 (Fonts):** add: one runtime asset per OS font file, coverage read from the font file, 48 pt, prewarmed a few glyphs a frame (`CultureUiSettings.prewarmGlyphsPerFrame`); every built and run-time text is in a kit face, never LiberationSans (`SceneUiKit.Refont`, the kit pass; tested in Unity: `TextLangEditorTests`).
- **The citation lines:** "traveller" everywhere.

CHEATS: the "More" lens menu's language lock lasts until the next day (`ResetForNewDay`).

## RESULT

Every check outside Unity is green (table above). Everything below needs Unity and is **deferred: Unity** (code-only run; Saleh may be on the PC). Run in this order, in the worktree's own editor, by job; revert churn after (`*.csproj`, `ProjectSettings/*`, the LiberationSans SDF Fallback asset).

1. **deferred: Unity. Generate World.** `Tools > TimeDesk > Generate World`, then the content validator.
   - Pass: validator green; `Assets/Data/World/Culture/Strings_en.asset` holds `settings.sound` … `settings.reduceFlashing` and the new `settings.languageDesk`; no `Strings_*.asset` holds `settings.languageLocked`; `case.roleUnknown` reads "Traveller".
   - Commit the regenerated string tables (TEXT-LANG's assets) and whatever else the generator rewrites for these strings.
2. **deferred: Unity. Kit atlases.** `Tools > TimeDesk > Text > Extend Kit Font Atlases`.
   - Pass: each face logs more characters than before (each had 112-120); the "no glyph" list holds the Greek codes (all five faces) and, for UnifrakturMaguntia, most of Latin Extended-A; no error.
   - Commit the five `Assets/Fonts/Kit/*SDF.asset` (with their new atlas sub-assets).
   - Then `Tools > TimeDesk > Text > Audit Fonts`: "Kit atlas gaps for today's strings: none" (else list them in this file).
3. **deferred: Unity. Build Office UI**, then the OfficeGameplay semantic dump against fix/p0-cuts's.
   - Pass: the only differences are fonts: texts formerly in LiberationSans now in Outfit Regular/Bold SDF (the log line "[TimeDesk] The kit pass set N text(s) …" gives N; about 52 on 5aecb60). No object added or removed (the Settings rows wait for UI-PC).
   - Look at the PC's windows, the overlay and the desk's 3D words at A1 and A2 for overflow caused by the face change; list any.
   - Do not commit the scene (INTEGRATION writes OfficeGameplay).
4. **deferred: Unity. EditMode suite** (TestRunnerApi, all EditMode).
   - Pass: green except the known UnitySkills failure; includes `TextLangEditorTests` (6) and `TextLangSeamTests` (3).
5. **deferred: Unity. Font audit.** `Tools > TimeDesk > Text > Audit Fonts` after step 3.
   - Pass: `Assets/Scenes/OfficeGameplay.unity` absent from the list. Expected remaining: `OfficeScene.unity` 42 and `AnimeHall.unity` 5 (art side, request filed), `TitleScene.unity` 2 (HOME), `Test_DayLoop.unity` 5 (test scene).
6. **deferred: Unity. Language takeover** (BUG-3a; FIX_PLAN v2: "0 culture-language strings outside Paper surfaces and signs, and 0 on any PC surface").
   - Golden seed 12345, days 7, 8 and 9 **played in sequence** (not jumped), twice: once with no stored choice (delete `TimeDesk.UiLanguage` from the editor's prefs first, through `PlayerPrefKeys.For`), once with Follow history.
   - At each capture point (the shift report, Home, a wrong call: the citation slip and Mail's copy, the wheel open, the briefing, the PC's Mail and Investigation windows) run a temporary `_TimeDeskTextLangProbe` job method that writes `TextLangAudit.CultureOnPlayerSurfaces()`, `LiberationSansOnScreen()` and `AccentedGreekCapitalsOnScreen()` to SCRATCH.
   - Pass: 0 lines from `CultureOnPlayerSurfaces` at every point, on both runs; the papers show the culture's words from day 8 (no choice) and from day 7 (Follow history, when a culture leads).
   - Then store Always English and play day 9: the papers stay English.
   - Captures at A1 into `R7\FIX\TEXT-LANG\takeover\`.
7. **deferred: Unity. Mid-day switch** (BUG-3a: "visible change within 1 frame of Apply, and never locked").
   - Day 8 mid-shift, a paper on the desk in the culture's words: open Settings, click Always English, then Follow history.
   - Pass: both buttons are always interactable; the pair's selection and the line repaint on the click's frame (SEQ capture, 30 fps); the next traveller's papers print in the chosen language; the paper already on the desk keeps its print with no box glyphs; 0 new console lines.
8. **deferred: Unity. Fonts at run time** (BUG-38a).
   - Days 1, 8 and 12 (played, and by the cheat path): `TextLangAudit.LiberationSansOnScreen()` at the office, Home and the Title.
   - Pass: 0 texts; the console has no TMP "character … was not found" warning; the passport's "VISAS D'ENTRÉE" draws its É in the same face (zoom capture).
9. **deferred: Unity. Arabic** (BUG-37a).
   - Cheat an Arabic-led era (Egypt or Iraq) with Follow history; capture a paper with a long value, the rulebook GUIDE, the report and a citation.
   - Pass: the paper's words read right to left with no stray tag characters and numbers/brackets in order (by eye, or an Arabic reader); the rulebook, report and citation are in English with their arrows pointing outward.
   - DOC-LOOK's per-line call sites (37c) are measured here: a wrapped Arabic paper value's lines in reading order. An Arabic-tongue speech bubble that wraps is the TOOLS gap above (record its state).
10. **deferred: Unity. Greek capitals and toggles** (UI-19). Cheat a Greek-led era (Follow history).
    - Pass: `AccentedGreekCapitalsOnScreen()` 0 on the PC, Home and Settings; no toggle's word cut (TMP overflow scan of the Settings and Home buttons).
11. **deferred: Unity [BP]. CJK bake** (CODE-81). A development build; the first office load of a session under China, then under Japan (cheat leader), with Follow history.
    - Pass: no main-thread frame over 33 ms attributable to SDF baking (profiler: `TMP_FontAsset.TryAddCharacters` / FontEngine markers); the glyphs arrive 12 a frame after the load.
12. **deferred: Unity [BP], at merge #13 (UI-PC wires the rows). Settings basics** (BUG-62).
    - Each setting changed in the built player. Pass: the volume meters move (Master, Music, Effects); Windowed / Borderless / Fullscreen switch; the resolution steps; VSync on shows frame pacing at the display's rate; Brightness visibly brightens and darkens (with LIGHT's post-processing on); Reduce flashing persists across a restart.
    - Before #13: `SettingsBootstrap` applies stored choices at startup (set them through PlayerPrefs and restart the player).
13. **deferred: Unity. RUN-SAFE's day-scoped test** (BUG-27a re-run at #12). Cheat "lock now" on day 7, then skip to day 8.
    - Pass: `CultureThemeService.LockOverride` is null on day 8 and the papers follow the stored choice.
