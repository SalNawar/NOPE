# Gate record: PR #6 — the Investigation app's readable tabs and chips; phase 20 (keys, clipboard, pins, zoom)

*2026-09-26. Branch `claude/trusting-fermi-uhtkuk` (base `main` 1f218d4). Verified in the art worktree's persistent editor (`E:\unity\NOPE-art`); the reports, screenshots and contrast table are in the session's scratchpad (`pr6/`). Verdict: **ACCEPT** (one finding fixed on the branch, one follow-up filed).*

## Gates

| # | Gate | Result |
|---|------|--------|
| G1 | Builds & boots | PASS. 0 errors in every project (the editor assembly included); the player scripts compile (31 assemblies); Title → New Run → day 1 in the art office, a traveller decided at the stamp; the office build logs no error and no warning; the art office byte-unchanged. |
| G2 | Tests pass & gate the merge | PASS. Offline runner 1,774 passed, 0 failed; EditMode through TestRunnerApi 1,903 passed, 1 failed: the known third-party `UnitySkills…SceneSummarize_CountsObjectsCorrectly`. Phase 20's rules ship with `AppClipboardTests`, `AppFocusTests`, `AppZoomTests`, `DesktopEscapeRuleTests`, `EntryKeysTests`, `PagingTests`, `PinBoardTests`, `RecentListTests`, `ShortcutMapTests`. |
| G3 | Feature complete vs. its spec | PASS. The PC redesign spec's AP2 (the tab strip), AP3 (the badge in a reserved slot), AP5 (the chips) and section 2.2 / 6.3, and phase 20 (KB1–KB5, CP1–CP3, PR1–PR2): every probe in the 177-check play passes (the focus ring over the strip, the chips and the rows; Ctrl+C / Ctrl+Shift+C on a form row and a record row; Ctrl+V into Notes with the clip's source and into the search field; the Escape chain before the frame; the row menu; Ctrl+P; the F1 card with all 22 rows; zoom 100/125/150). Phase 18's split, links and glyph collapse are explicitly later. |
| G4 | No silent contract changes | PASS. `docs/FEATURES.md`: the app line (the strip, the chips, the badge slot, the knobs, the 5 % gloss and why) and the keys line. |
| G5 | Mergeable & current | PASS. `main` is still 1f218d4, the branch's base; a fast-forward. |
| G6 | Asset hygiene | PASS after 89551ee: phase 20's WIP commit had carried Unity's csproj regenerations, TMP's dynamic fallback atlas and a float re-serialisation in four placeholder materials; all ten files are back at main's versions. The rebuilt `OfficeGameplay.unity` and the nine regenerated themes are the intended asset changes (their second build is identical by semantic dump). Every new file has its `.meta`. |

## Quality bars

- **Configurability 5.** The strip and chip sizes are `DesktopConfigSO` knobs (`tabStripHeight`, `tabLabelSize`, `chipRowHeight`, `chipLabelSize`); the gloss is the `TitleGloss` palette rule in `world_source.json`; the zoom levels, pins and recents limits are knobs; the shortcuts are one table (`ShortcutMap`).
- **Modularity 5.** The rules are in Domain with tests (`AppClipboard`, `AppFocus`, `AppZoom`, `DesktopEscapeRule`, `EntryKeys`, `PinBoard`, `RecentList`, `ShortcutMap`); one keyboard poller (`DesktopKeyboard`) runs commands on the app; the views expose rows through `AppRow`; missing wiring skips (`Init` null checks).
- **Scalability 4.** New tabs, chips and rows follow the same `IAppView` / `AppRow` path; the ring and the keys read the pane, not the views. Phase 18's two panes are a known extension point named in the code.
- **Optimization 5.** The profile golden passes: the desktop window with the poller allocates what every office window does (the render loop's 368 B), no new allocation site in our assemblies, no slower load. The builders are idempotent (identical rebuilds).
- **Code quality 4.** Doc comments on every public member; the static metrics against `main` (below) attribute no new warning, clone group, per-frame site, singleton or static state to this PR beyond what phase 20's rule code declares; commit messages describe the changes and their verification.

## Intent audit

1. **Redo or override?** The restyle replaces the previous tab and chip builders inside the same builder (`OfficeSceneUIBuilder.App`: `BuildTab`, `BuildChipTemplate`, `ChipLabel`) rather than beside them; `AppPane.DrawChips` toggles the template's own "Chosen" child instead of tinting; the `Tab` palette rule changes from the pane's paper to the chrome. Nothing parallel: the theme roles, the palette map, the layout groups and the badge mechanism are the existing ones. Phase 20 adds one table, one poller and one clipboard, all named in its plan; the Escape chain is one rule shared with the PC frame (`EscapeTakenFrame`).
2. **Re-coding what data or config already supports?** The sizes and the gloss are data; the strings are UI string keys in `world_source.json`; no constant in rule code. The rendered contrast audit that found the gloss finding stayed a temporary job (never committed), like the readability audit before it.

## Finding fixed on the branch

The inactive tab labels on the 14 % white gloss read 3.35:1 in the neutral theme as drawn (declared 4.62:1): the project renders in linear colour space and `Contrast.WorstRatio` composites translucent layers in gamma space. The `TitleGloss` alpha is 0.05 (`e0c2ddb`): 4.83:1 (neutral) to 8.45:1 as drawn in all nine themes. **Follow-up outside this PR:** make the check composite in linear space and re-check every text on a translucent layer.

## Static metrics against `main`

`tools/audit/run_static_baseline.py` on `main` (1f218d4) and on the branch, compared with `compare.py` (the audit's frozen baseline is far behind `main` on every count, so `main` is the fair reference). What this PR adds, all in phase 20's code: 5 methods over complexity 15 (`ShortcutMap.Lookup` and `List`, the one table's decision tree, tested by `ShortcutMapTests`; `InvestigationApp.Run`, a flat switch of commands; `DesktopKeyboard.Escape`, the chain's switch; `CitizenRecordsWindowController.FillRow`); 3 methods over 60 lines (`BuildAppPane`, `BuildSettingsWindow`, `InvestigationApp.Run`); 1 per-frame component lookup (`DesktopKeyboard.FocusedField`: a non-allocating `TryGetComponent` on the selected object, only while the desktop takes input; the total of per-frame lookup sites still falls from 10 to 9); 1 literal in rule code (`EntryKeys.TryBookRow`'s part count, 3) and 3 rule strings (`KeyChord.ToString`'s "Ctrl+", "Shift+", "Alt+", the card's key text); 2 static readonly collections (the poller's key table, a test fixture); `InvestigationApp` over 400 lines with its keys partial; 87 normalised clone groups (the table's repeated lines and builder calls). No new compiler warning, allocation site, singleton site or unused member (`AddGridLayout` is unused on `main` already). Accepted: none crosses a bar's reject line; the table and the switches are the design; left to the wave 3 audit pass.
