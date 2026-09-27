# Redesign: status report and resume plan

*2026-09-26 around 14:30. Work stopped at Saleh's request (usage limit). `main` = `500dc63`: every phase below marked "In main" passed compile, offline tests, the Unity EditMode suite, a smoke test and its feature tests in the art office. Unfinished phases are saved as WIP commits on their own pushed branches. They are unverified and must not be merged as they are.*

Plan of record: [2026-09-26-redesign-plan.md](2026-09-26-redesign-plan.md). Specs: [traveller types](../specs/2026-09-26-traveller-types-design.md) and [PC redesign](../specs/2026-09-26-pc-redesign-design.md).

## 1. New in `main` (what the game does now)

### Travellers and papers
- **Documents are always in English** (phase 1). Translators are for speech only, one per region. Untranslated speech keeps key words ("home", the place, names, digits) in English, and the word list is data.
- **Records** (phase 2) are grouped entries (RECORDS / FORMS ON FILE / TRAVEL), found by number or by name. The agency calendar shows "14 MAR 2150 · DAY 1".
- **Traveller types** (phase 3): RichTourist, PoorTourist, Labourer and Displaced. The displaced carry the TC-610 Displacement Certificate, TC-620 Intake Declaration and TC-630 Return Order. Papers are requested through "Request papers >".
- **The present, 2150** (phase 6) appears in every reference book: the neutral "Temporal Customs Zone", or the leading nation's Future city. Citizen Accounts are found by Citizen ID or name, including the clerk's own 773-2840-19. **Rich tourists arrive on day 1** with the TC-101 Leisure Visa and the TC-230 Departure Manifest; they are honest for now, and their lies are phase 7.
- **Dress for the destination** (phase 10). A 2150 traveller must wear the destination's clothes. The costume errors are another place's item, 2150 clothes, or an item from the new 2150 accessory kit. The proof is the Look menu against the Costume Guide, which is grouped by era. The citation reads "would cause a panic", and the next paper reports the panic.
- **The clerk's debt** (phase 13a). The clerk starts 125,430 cr in debt, and 25% of each shift's pay goes to Debt Relief. The debt shows in the shift report, the Citizen Account and the statement, and a debt story runs in each morning paper. Bankruptcy ends the game as **"A Fresh Start"**: the account is frozen and the clerk's own Debt Relief labour contract (TC-520) is shown.

### Forms
- **Desk papers print as real agency forms** (phase 4): the Temporal Customs header, form number, numbered sections, boxed fields, the photo, a barcode and serial, the desk-stamp area and fine print. They are readable at 720p with contrast above 13:1.
- **The PC draws the same forms** (phase 5). Scanned copies in the app match the desk paper box for box, and a pick compares across desk, copy and records. Page kinds: the record extract, register, memo, and the statement (printed landscape). Pages in the app size to their pane.

### The PC
- **Windows** (phase 14): focus on any press, taskbar buttons, exact maximise, and a compare dock no window covers.
- **The Investigation app** (phases 15 and 16): one window that fills the PC screen, with a case header (the claim, counters, Accept/Deny), six tabs (Documents, Records, Reference, Transcript, Report, Rules), and a toolbar and sidebar that are built but not live yet. **Nothing opens by itself**: badges and a "scanned" toast instead.
- **Desktop icons** (phase 17): six real icons (Investigation, Internet, Mail, Citizen Account, Notes, Settings) that can be dragged freely and arranged, with positions saved. Double-click opens, with a single-click option. There is a right-click menu, and Mail's badge shows the unread count.
- **Internet** (phase 24): a browser with News and its back issues, Chronopedia (history with revisions) and the Lineage Archive (ancestry). Sites are data.
- **Mail, the Citizen Account, Notes and Settings** (phase 25).

### Content, art and quality
- **The Excel spreadsheet import** (phase 26): Tools > TimeDesk > Import/Export Content Spreadsheet. The template is `ContentSheets/TimeDesk_Content_Template.xlsx`, and bad rows are refused with the sheet, row and column.
- **Art slots** (phase 27, hooks): every 2D asset on the asset list has a by-name slot under `Assets/Art/UI/Resources/<area>/`. A delivered PNG appears on the next run with no code change. Paths are in `docs/ART_ASSET_LIST.md`.
- **The character pilot**: 42 layers are processed from ChatGPT's 15 images, so real Athens and Mamluk travellers stand at the desk.
- **Audit:** 8 confirmed bugs fixed (the build scene, a stuck paper, right-to-left typing, world checks, crash-safe saves, record proof, readout allocations, slot seeding); a clean-up slice (11 fixes, 11 unused packages removed); readability (608 contrast failures fixed to 0); per-project editor saves; the ChatGPT code clean-up (25 files discarded, tools re-pointed, comments).

## 2. In the pipeline (WIP branches, pushed and unverified)

| Phase | Branch | State at stop | Resume from |
|---|---|---|---|
| 7: first lies (Lies.Roll, record lies L1/L2, PaperChecks, two fault kinds) | `redesign/p07-first-lies` | just started (1 WIP commit) | a clean start from main is fine: read the WIP diff first |
| 18: two panes, tab reorder, smart links, back/forward, dock columns | `redesign/p18-panes-links` | code mostly done (7 commits); the Unity feature play was being written | run the Unity checks, fix, merge |
| 19: search (TextMatch, CaseIndex, Records via index) | `redesign/p19-search` | code written (2 commits); the Unity setup job was being written | Unity checks, fix, merge |
| 20: shortcuts, Escape chain, clipboard, pins/recents, zoom | `redesign/p20-keys-clipboard` | code mostly done (5 commits); the builder partial was being written | finish the builder, Unity checks |
| 21: optional steps checklist | `redesign/p21-steps` | code mostly done (5 commits); updating the RichTourist set | finish, Unity checks |
| 22: Auto-Feed and Analysis scanners | `redesign/p22-scanners` | just started (1 WIP commit) | a clean start is fine |

WIP commits may carry generated-asset churn (a rebuilt OfficeGameplay, Assets/Data line endings) from the moment they stopped. Review each diff before building on it.

## 3. Not started

- **8:** poor tourists: 4 papers (the 4th is one of Holiday Credit, Proof of Funds or Travel Insurance), request groups, missing-form replies; always honest on day 1.
- **9:** labourers (day 3): the TC-520 labour contract, debt checks, the new directive types, lies L3/L4/L5/L10.
- **11:** smuggling (2150 tech), departure and expiry dates (from day 4), trip questions, carries from the present.
- **12:** the displaced move to day 5 and the famous (premades) to day 6; ReturnHome; translation from day 5; the Future stops being a destination.
- **13b:** strandings from cheap transponders (seeded 8%, a history carry, a 150 cr fine without a signed waiver). The seams are in phase 13a's report: `Seeds.ForStrandings`, the ShiftLedger fines, the `news.stranded` map change.
- **16 part 2:** draw the remaining app views with FormView (TC-901 extract, the TC-911–916 registers, the transcript, report and memo); lists scroll; delete the old window controllers.
- **23:** balance: the 50-run re-simulation and knob tuning.
- **27 (art):** wire the delivered art as it arrives (the character batches, the 2150 clothes and kit, icons, form faces).
- **The end-of-epic passes:** a full play-through of days 1–6 with screenshots at 1080p and 720p, the golden-master re-pack, and the profile compared with the baseline.

## 4. Work plan for when we resume

**Setup (about 10 min).** Re-read `scratchpad/HOUSE_RULES.md`, the memory note `project-night-run-2026-09-26.md`, and this file.
- The six worktrees and their editors: `E:\unity\NOPE-art`, `NOPE-feat-clock`, `NOPE-p5`, `NOPE-gpt`, `NOPE-t5`, `NOPE-t6`. Each has the untracked `_ClaudeJobRunner.cs`. If an editor is closed, relaunch it once with `Unity.exe -projectPath <worktree>`.
- Resolve `main` with `git ls-remote origin refs/heads/main`, never `origin/art`.
- If ChatGPT pushed art in between, merge `art` into main first, with a contract check and a smoke test.

**Wave 1** (parallel, about 2–3 h). Each item merges main before reporting, and I fast-forward main one at a time.
1. T1: phase 7, the critical path. Everything in wave 2 waits on it.
2. T3: finish phase 18, then phase 16 part 2.
3. T5: finish phase 19.
4. T6: finish phase 20.
5. T2: finish phase 21.
6. T4: phase 22. Its Analysis Scanner switches to phase 7's PaperChecks when 7 lands.

**Wave 2** (after 7 lands, parallel, about 2–3 h). Phases 8, 9, 11 and 12 on four tracks. They all extend Lies, CaseFactory and world_source, so each merges main often; merge in the order 8 → 9 → 11 → 12. Then 13b (strandings), which needs the transponders and waivers from 8 and 9.

**Wave 3** (about 1–2 h):
- phase 23 balance;
- the end-of-epic passes (the full play-through with screenshots, golden re-pack, profile);
- the remaining audit items the plan left to the overhaul (see `scratchpad/audit/backlog.json` and the plan's mapping);
- the phase 27 art wiring as ChatGPT delivers.

**Estimate:** about 5–7 h of wall-clock with six tracks, if the session stays authenticated. An expired token stopped the night run for about 3.5 h.

## 5. Waiting on Saleh

- **Content placeholders to author:**
  - the clerk's name and history ("Theo Marlow", born 9 Feb 2121);
  - the Debt Relief employer ("Tyburn Mills Consortium", 420 cr a day);
  - the starting debt (125,430 cr);
  - the present's facts ("Temporal Customs Zone": Credits, Agency Standard English, Wrist comm, Directorate Tower);
  - 15 people from the past for the Lineage Archive.
- **ChatGPT:**
  - run `git merge main` into `art` before editing again;
  - follow the "Rules for the art side" in `ArtDeliverables/TimeDesk/ImportedOffice/CURRENT_STATE.md`;
  - character Batch 2 (skins and faces) is next: the woman's base head is accepted as is;
  - the brief is v2.3, with the 2150 clothes and accessory kit.
- **Art-side scene anchors** (whenever scene edits are allowed): `Anchor_Scanner` (a real scanner model), `Anchor_PCPower`, `Anchor_Traveller` (closer to the desk so characters read bigger), and `Anchor_HandOver`.
- **Open design notes:**
  - outline aliasing at 720p (mipmaps were rejected);
  - skins 2–5 show a faceless placeholder head until Batch 2;
  - the day-1 scan hint overlaps the calculator;
  - the calendar box on the art is small for "14 MAR 2150 · DAY 1".

## 6. Continued on 2026-09-26 (a cloud session, branch `claude/trusting-fermi-uhtkuk`)

Saleh's request in that session: make the Investigation app readable (the tabs were "super messy") and make Ctrl+C / Ctrl+V work. Done there, without Unity (the container has no editor; a .NET 8 SDK compiled Domain, Visuals and the EditMode tests: `passed 1774, failed 0`):

- **Phase 20 landed on the branch** (merge `3e6654a` of `redesign/p20-keys-clipboard`, whose tip already held `main`'s code): copy and paste, the keys, the F1 card, pins, recents and zoom. Its last WIP commit (the merge with main's phase 6) is still unverified in Unity.
- **The app's chrome restyled** (`OfficeSceneUIBuilder.App`, `AppPane`, `DesktopConfigSO`, the `Tab` palette rule): the tab strip and the chip row as `docs/FEATURES.md` now describes them. The old strip drew the active and the inactive tabs in the same colour and its chips at 11-17 units; the new sizes are knobs.
- **Phase 18 was not merged:** a trial merge of 18 and 20 together conflicts in 20 files (the app, the pane, the views, the builder, the scene). When 18 lands, its `OfficeSceneUIBuilder.Panes` takes `BuildTab` and `BuildChipTemplate` from `OfficeSceneUIBuilder.App` and its `AppPane` keeps `DrawChips`' chosen look; its glyph collapse (AP3) replaces the labels' shrinking on a narrow strip.

**Unity steps before this merges to `main`** (in the art office, `E:\unity\NOPE-art`):
1. Tools > TimeDesk > Generate World (the `Tab` rule changed: chrome and its ink), then Build Office UI (the strip, the chips, the keys' partial on the fresh app); commit `OfficeGameplay` and the theme assets.
2. The EditMode suite; the smoke test; the phase 20 feature probes (Ctrl+C on a row, Ctrl+V into Notes and the search field, F1, Ctrl+P, Ctrl+=).
3. Screenshots of the app at 1920 × 1080 and 1280 × 720: the tab strip active and inactive, a badge, the Documents chips (one chosen, one on the desk), the Reference chips; the readability audit over them.
4. The golden masters: `data_hashes.txt` (world_source.json, the themes), the scene dump and the profile change on purpose; re-pack once verified.

**Verified locally on 2026-09-26, 19:20-20:30, in `E:\unity\NOPE-art`'s persistent editor** (the branch checked out there; `redesign/p22-scanners` keeps its WIP commit on its own branch):

- Compile: 0 errors in every project; the offline runner `passed 1774, failed 0`; the player scripts compile; the EditMode suite through TestRunnerApi `passed 1903, failed 1` (the known third-party `SceneSummarize_CountsObjectsCorrectly`).
- Generate World changed the nine `Theme_*.asset` (the `Tab` rule) and nothing on a second run; Build Office UI logged no error and no warning (no missing ThemeTag, no UiContrastCheck finding, nothing the keys' builder did not find), rebuilt identically twice, and left the art office byte-unchanged. Committed with the themes.
- The play in the art office, day 1 (seed 12345), 177 checks, 0 failures (`_TimeDeskPr6Play`, untracked; report and screenshots in the scratchpad's `pr6/play`): the tabs are 26-unit labels on plates that neither overlap nor move when a tab switches or a badge arrives (the dot sits after the label in its reserved slot); the active label lies exactly over the inactive one; the Documents chips (the scanned one chosen on the accent plate in bold, the one on the desk dimmed); the six Reference chips fit the row whole at 17-20 units (14.9 px at 1080p, 10 px at 720p); the restored 1120 × 820 window squeezes the strip to 20-23-unit labels, none cut or overlapping; the focus ring walks the search field, the strip (← → switch tabs), the chips and the rows; Ctrl+C / Ctrl+Shift+C on a form row and on a citizen record row, Ctrl+V into Notes (a clipping with its source, under the traveller) and into the search field (TMP's own paste); Escape clears then leaves the search, closes the row menu and the F1 card before the frame; the row menu's Copy value, Pin/Unpin and Pick for compare; Ctrl+P; the F1 card lists all 22 rows of `ShortcutMap.Card`; Ctrl+= / Ctrl+- / Ctrl+0 zoom 100 → 125 → 150 → 125 → 100 %.
- **Found and fixed: the tab plate's gloss.** Measured as drawn (the text hidden and shown, the text's colour against every pixel under the glyphs, in the nine themes), the inactive tab labels on the 14 % white gloss read 3.35:1 in the neutral theme and 3.5-4.9:1 in the cultures, although the build-time check declares 4.62:1. The project renders in linear colour space and `Contrast.WorstRatio` composites translucent layers in gamma space, so a white gloss lightens the chrome more than the check computes (neutral: luminance 0.247 drawn, 0.177 composited). The `TitleGloss` rule's alpha is 0.05 now: every tab label reads 4.83:1 (neutral) to 8.45:1, the active labels 13:1 or better, the chips 5.1:1 or better, in all nine themes, maximised and restored. **Follow-up (not in this PR): make the check composite in linear space** and re-check every text on a translucent layer (TaskbarGloss, StartGloss, the 0.8-alpha panels).
- The golden masters: cases, the validator, the contract, the play transcript and its warnings are identical to the baseline; `data_hashes.txt` (the themes, `Strings_en`, and phase 5's `Form_Memo/RecordExtract/Register/Statement`, which were already missing from main's baseline), the OfficeGameplay dump and summary (the strip, the chips, phase 20's parts) and the Generate World file count change as intended; the profile passes (no window allocates more per frame, no new site, no slower load); runs A and B identical. Re-packed on the branch.
