# The redesign plan: traveller types and the PC redesign, one order

> **For agentic workers:** one phase is one agent session on its own branch. Start a phase by writing its task list (superpowers:writing-plans) from its entry below and the spec sections it names, then execute it (superpowers:executing-plans or superpowers:subagent-driven-development).
> - Specs: `docs/superpowers/specs/2026-09-26-traveller-types-design.md` (**T**) and `docs/superpowers/specs/2026-09-26-pc-redesign-design.md` (**P**), both finalized with Saleh's answers on 2026-09-26.
> - House rules: `SCRATCH/HOUSE_RULES.md`; read it before a phase starts. `SCRATCH` = `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`.
> - The code wins over this text: each phase re-reads the files it names before it edits them.

**Goal:** the three traveller types of the debt dystopia (T) and the redesigned PC (P) in the live game, in small phases, each merged to `main` only after it is verified.

**Order:** the forms engine and the traveller domain come first; the PC app follows; the Internet, Mail, Notes, the content import and the art come last. The audit hotfix slice is already in flight on `overhaul/audit` (phase 0).

---

## 0. How every phase runs

- **Branch and worktree.** Branch `redesign/NN-<name>` from the current `main`, in the worktree the one persistent Unity editor has open (today `E:\unity\NOPE-art`; work in another worktree needs the orchestrator to switch the editor). Never edit the main checkout `E:\unity\NOPE`, never edit the art scene `Assets/Scenes/OfficeScene.unity`, never push, rebase or amend. The orchestrator fast-forwards `main` after the phase is verified.
- **Order inside a phase.**
  1. Re-read the named code. The code wins over the specs and this plan.
  2. Tests first: write the failing EditMode tests (Domain and Visuals), see them fail (a compile error counts), implement, see them pass.
  3. Offline gates after every code change: `python SCRATCH/compile_check.py <worktree>` (0 errors) and the offline runner (`passed N, failed 0`, N at least the phase's starting count plus its new tests).
  4. Content goes into `Assets/Data/World/world_source.json` and through Generate World; generated assets are never hand-edited.
  5. A builder change means Build Office UI in the editor, then OfficeGameplay committed (compared by semantic dump, not bytes).
  6. `docs/FEATURES.md` changes in the commit of the behaviour; "(tested: X)" only when test X exists and covers it.
- **Unity verification** (the one persistent editor, never launched or closed by a phase):
  - Put the phase's static parameterless methods in untracked `Assets/Editor/_TimeDesk<NN>*.cs` files (never committed). Queue each as `Library\ClaudeJobs\<unique>.job`, one at a time. Wait for `<unique>.returned` or `.failed`. Async work (TestRunnerApi, play mode, screenshots) writes its own report to `SCRATCH` and leaves the editor in edit mode.
  - Play only in the art office: `OfficeScene` with the additive `OfficeGameplay`, as the game loads them. Never the old 2D booth.
  - Screenshots at 1920 × 1080 and 1280 × 720. The readability audit (the readability pass's job) runs over the phase's new states in the neutral theme and the eight cultures.
  - Run the EditMode suite through TestRunnerApi. The known third-party failure (`UnitySkills...SceneSummarize_CountsObjectsCorrectly`) is reported and ignored.
  - Afterwards revert what Unity touched outside the phase (`*.csproj`, `ProjectSettings/*`, the LiberationSans fallback asset), and delete the `_TimeDesk*` files and their metas.
- **Golden masters** (the Phase 0 baseline, `docs/reviews/audit-baseline`, and its tools `tools/audit/*`, on `main` once phase 0 lands):
  - Static metrics: `run_static_baseline.py --check`. New types and removed dead code are expected; each accepted regression is named with `--allow` and written in the commit message.
  - Unity jobs `_TimeDeskAudit{Static,Play,Profile}` (copied from `tools/audit/unity/*.cs.txt`), then `golden.py diff`.
  - Each phase below names its **golden effect**: the golden files it changes on purpose. Everything else must stay byte-identical, the profile may not gain a per-frame allocation or an allocation site, and no scene load may be more than 25 % slower.
  - A phase with an intended effect re-baselines once (`golden.py pack … --out docs/reviews/audit-baseline/golden`) and commits it with the reason and a short account of the diff.
  - The scripted play-through (`tools/audit/unity/_TimeDeskAuditPlay.cs.txt`) is updated in the phase whose flow it drives changes: which papers it requests, which proofs it runs, what it buys at Home, the PC path.
- **Review.** Each phase passes `docs/reviews/MERGE_CRITERIA.md` and Saleh's review preferences (the gates, and an intent audit with redo/override and no-code-path checks) before the orchestrator merges it.
- **Sizes:** **S** is a short session, **M** most of a session, **L** a full session. An L phase names a split seam in case it runs over.

## 1. The phases at a glance

| # | Phase | Scope | Size | Depends on |
|---|---|---|---|---|
| 0 | Audit hotfix slice (in flight) | The backlog's high and medium bugs and Unity pitfalls, on `overhaul/audit`; the golden-master tools and baseline | (not this plan) | – |
| 1 | Speech-only translation | Documents always English; the Papers translators, paper flips and sightings retire; key words stay English in untranslated speech | M | 0 |
| 2 | Records as rows, the agency calendar | Records as grouped rows, lookup by name or number, keyed record picks; the `agency` block and the desk calendar's date; the text fallback deleted | M | 0 |
| 3 | Traveller kinds and the displaced's forms | `TravellerKind`, blueprints per kind, TC-610/620/630 replace the passport and permit, `Seeds.ForAccount`, the "Request papers >" sub-menu | L | 1, 2 |
| 4 | Forms engine and the desk paper | `FormLayout`, `FormStyleSO`, serials and barcodes; the desk paper draws its form; `PaperFace` retires | L | 3 |
| 5 | Scanned copies as forms | `FormView` on the PC's scanned-document window | M | 4 |
| 6 | The present, accounts, rich tourists | The present (2150) in every book; Citizen Accounts; TC-101/230; honest rich tourists on day 1 | L | 3, 5 |
| 7 | Record lies and paper proofs | `Lies.Roll`, `RecordLies` (L1, L2), `PaperChecks` and the cross proof, the two fault kinds in the verdict | L | 6 |
| 8 | Poor tourists and proofs of means | TC-310/415/416/417, the proof choice, request groups and missing-form replies; day 1's honest poor tourists | M | 7 |
| 9 | Labourers and the directives | TC-520, directive types and makers, `Seeds.ForFaults`, L3, L4, L5, L10 | L | 8 |
| 10 | Dress for the destination | Costume errors (another place's item, 2150 clothes, a 2150 accessory), the panic reason | M | 9 |
| 11 | Smuggling and dates | L6 with the present as a source, `NoPresentGoods`, `PaperDates`, trip questions, `tellSource` and carries from the present | L | 10 |
| 12 | The displaced on day 5, the famous on day 6 | The day mix, L8, `ReturnHome`, translation from day 5, the Future out of the destinations, premades to day 6 | M | 11 |
| 13 | Strandings and the clerk's debt | Stranding rolls, carries and fines; the clerk's account, instalments and the Debt Relief ending; the debt news | M | 12 |
| 14 | Windows and the compare dock | `WindowStack`, focus, taskbar buttons, exact maximise, the dock | M | 13 |
| 15 | The Investigation façade | `InvestigationUIController` split into presenters, behaviour byte-identical | M | 14 |
| 16 | The Investigation app | One window, six tabs, one pane, every view a form; the Costume Guide by era; nothing steals the view | L | 15 |
| 17 | Desktop icons | Six real icons (drag, arrange, save, double-click, keys, badges), context menu, new theme roles, 28 flavour labels | M | 16 |
| 18 | Two panes and smart links | Split, reorder, `SmartLinks`, back and forward, keyed highlights, dock links | M | 17 |
| 19 | Search | `TextMatch`, `CaseIndex`, the Records lookup through the index | M | 18 |
| 20 | Keys, clipboard, pins | `ShortcutMap`, the Escape chain, focus, copy and paste, pins and recents, zoom | L | 19 |
| 21 | Steps | The optional checklist, step sets per traveller kind | M | 20 |
| 22 | Scanner upgrades | Auto-Feed and Analysis replace Archive Access | M | 21 |
| 23 | Balance pass | The epilogue re-simulation and the new knobs | S | 22 |
| 24 | Internet | The browser, News with its archive, Chronopedia, the Lineage Archive | L | 23 |
| 25 | Mail, Citizen Account, Notes, Settings | The clerk's account and statement, Mail, Notes, the Settings sections | L | 24 |
| 26 | Content spreadsheet import (later) | CSV/XLSX sheets ↔ `world_source.json`, then Generate World | M | 25 |
| 27 | Art hooks | The delivered art wired in: glyphs, seal, faces, covers, 2150 clothes, the accessory kit | M | 17, art |
| – | Future (not scheduled) | The transponder desk device; Ancestry cards as evidence | – | – |

Phases 14-15 do not depend on the traveller phases; if an agent is free they can move earlier, as long as merges stay one at a time (the builder files would conflict).

## 2. The phases

### Phase 0 — Audit hotfix slice (in flight, not built by this plan)

- **Where:** branch `overhaul/audit`, from the Phase 0 baseline at `ff3a6e0` (`docs/reviews/audit-baseline`, tag `audit-baseline`).
- **Scope, as this plan assumes it:** the backlog's two high items and its medium items of category bug or Unity pitfall:
  - R3-001: build index 0 boots `Test_DayLoop`;
  - R5-001: a held paper dragged out with the frame open is stranded;
  - R5-002: `DeskDraggable` has no `OnDisable`;
  - R5-003: `PaperExaminer` poses before the camera moves;
  - R2-001: untranslated right-to-left speech types from its end;
  - R6-001: Generate World does not check day ids, numbers and queues;
  - R6-014: `JsonUtility` silently ignores unknown keys.
- It also lands the golden-master tools (`tools/audit`) and the baseline every later phase diffs against, re-baselined for its own documented fixes.
- **For this plan:** phase 1 starts from `main` after the slice has merged. If the slice fixes more, a phase skips what `main` already has; if it fixes less, the phase that touches the file takes the item (phase 1 takes R2-001, phase 26 takes R6-014).

### Phase 1 — Speech-only translation (M)

- **Specs:** T F5, I4, §8.1, §11.1; P TR1-TR3, SE5 (its data side). `translation.fromDay` stays 2 here: every traveller is still today's displaced kind, and day 5 comes in phase 12.
- **Scope:**
  - Retire the papers translation (T §11.1):
    - the four Papers upgrades (the generator stops making them, and its owned folder prunes them);
    - `TranslatorKind.Written` (the id grammar `tr_{pack}_spoken` stays);
    - `Translation.InTongue(ClueCategory)`;
    - `CaseTranslation.Field` and `PapersTranslated`, and the field path of `EvidencePicks`;
    - `DocumentReveal`, `RevealClock`, `DocumentRows.TongueRow` and `HasTongue`, and `DocumentRowView`'s foreign branch;
    - the desk paper's and the scanned window's flips, the sightings (`InvestigationUIController.Sighted` and its callers);
    - `FlipTiming.rowStagger`.
  - The key-word rule: `translation.keyWords` in the JSON; Domain `KeyWords.Spans(template, fills, rule)`; `DialogLine` carries its English spans from the formatter; `Pseudoscript`, `DisplayText` and `FlipSequence` keep those spans English and never flip them; the bubble and the transcript both use it.
  - `TextFlip.Write` takes the text's own font (R4-024, TR3).
  - The notice text drops "papers". The Home shop now has 8 upgrades (2 pages; `Paging` stays).
- **Files:**
  - Domain: `Translation.cs`, `TranslationContent.cs`, `Dialog.cs`, `InterviewScript.cs`, `DocumentRows.cs`, new `KeyWords.cs`.
  - Visuals: `Pseudoscript.cs`, `DisplayText.cs`, `FlipSequence.cs`, `SpeechQueue.cs`; `RevealClock.cs` deleted.
  - UI: `DocumentRowView.cs`, `EvidencePicks.cs`, `InvestigationUIController.cs`, `DocumentWindowController.cs`, `TranscriptWindowController.cs`, `TravellerWheel.cs`, and in `UI/Translation/`: `CaseTranslation.cs`, `TranslationPresenter.cs`, `TextFlip.cs`, `TranslationSettings.cs`.
  - Desk: `Office/Desk/DeskController.cs`, `DeskDocument.cs`, `PaperExaminer.cs`; `Office/BoothCoordinator.cs` (the sighting hooks).
  - Editor: `WorldContentGenerator.cs`, `WorldContentGenerator.Translation.cs`, `ContentLibraryValidator.cs`.
  - Content: `world_source.json`, the regenerated library and `Assets/Data/World/Translation/`.
  - `docs/FEATURES.md`; `tools/audit/unity/_TimeDeskAuditPlay.cs.txt` (Home buys the Near East Speech translator).
- **Tests first:**
  - new `KeyWordsTests`: slot fills, whole words (case- and accent-insensitive), digits, merged overlaps, the empty rule;
  - `PseudoscriptTests`, `DisplayTextTests`, `FlipSequenceTests`: English spans never change and never flip; `rowStagger` gone;
  - `ArabicShaperTests`: Latin inside Arabic (R2-022);
  - `TranslationTests`: the written ids gone, the key-word problems;
  - `DocumentRowsTests`: the tongue rows removed;
  - `RevealClockTests` deleted.
- **Unity:**
  - Generate World: four upgrades pruned, library rewired, validator clean.
  - Play day 2 in the art office:
    - a foreign traveller's papers read English held at the desk and on the PC;
    - without the Speech translator the bubble keeps "home", the place and the name in English;
    - with it the line flips; reduced motion shows it at once;
    - the transcript row matches the bubble;
    - an untranslated answer compares, and the bar shows the placeholder;
    - the Home shop's two pages.
  - Screenshots at both sizes.
- **Golden effect:** `world_generate`, `data_hashes` and `validator` (translation assets); the play transcript and saves (the Home purchase; no paper flip). `cases.txt` and the scene dumps are unchanged.
- **FEATURES:** Translation 86-91 rewritten (the papers never in a tongue; speech only; key words; the flip on speech only; the scene-check line); 13 (the shop's pages); 42 (reduced motion now covers speech only).
- **Audit absorbed:** R4-024, R4-023 (the TextFlip font test), R2-022, R2-019 (the shaper's allocations, touched), R1-013 (`DocumentRows`' duplicated loop, touched), R4-021 in part (`CaseTranslation`'s decision table moves to a tested assembly).
- **Depends on:** 0 (R2-001's fix, the golden tools).

### Phase 2 — Records as rows, the agency calendar (M)

- **Specs:** T R1, R3, F6; P Q3 (delete the fallback), §2.5 (the row shape).
- **Scope:**
  - `CitizenRecord` becomes rows (category, label, value) in groups; today's registry entry fills them (Name, Born, Origin, Note).
  - `CitizenRegistry.Find` tries an exact number first.
  - `PickKeys.Record(category, recordId)` fixes R4-009.
  - The old Citizen Records window lists a record's rows generically.
  - The `agency` block (`name`, `programme`, `firstDate`) and Domain `AgencyCalendar.Today(firstDate, day)`, through `BirthDates.Format`; the desk calendar readout shows "14 MAR 2150 · DAY 1", formatted on change, not per frame.
  - The text fallback deleted with its `fallback.*` strings.
- **Files:**
  - Domain: `CitizenRegistry.cs`, `ComparePair.cs`, `DiscrepancyLog.cs` (`ForRecordField`), new `AgencyCalendar.cs`.
  - Scripts: `CaseFactory.cs`; `UI/CitizenRecordsWindowController.cs`, `UI/EvidencePicks.cs`, `UI/InvestigationUIController.cs` (the fallback); `Office/OfficeReadouts.cs`; `ContentLibrarySO.cs`.
  - Editor: `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `OfficeSceneUIBuilder.cs` (the row template).
  - Content: `world_source.json`; OfficeGameplay rebuilt.
  - `docs/FEATURES.md`; the audit play job (record picks by key).
- **Tests first:**
  - `CitizenRegistryTests`: rows and groups; find by number first; an exact match beats an earlier substring (R1-022);
  - `ComparePairTests`: two records' Born rows are two picks (R4-009);
  - new `AgencyCalendarTests`: day 1 = `firstDate`, month and year rollover, format;
  - `DiscrepancyLogTests`: the record proof through rows.
- **Unity:** Generate World; Build Office UI. Play days 1-2: a lookup by name and by number, the birth-date proof, the calendar's date. Screenshot at 720p. The profile shows `OfficeReadouts` no longer allocating each frame.
- **Golden effect:** `cases.txt` (the record's format); the play transcript (record pick keys); `scene_OfficeGameplay` (the Records rows); the profile (an allocation site removed: an improvement, re-baselined).
- **FEATURES:** 21 (the calendar's date), 50 (records as rows, by name or number), 76, 82 (removed), 152-153 (the agency block).
- **Audit absorbed:** R4-002, R4-009, R1-007, R1-009, R1-022, R5-004, R5-023.
- **Depends on:** 0. It is independent of phase 1 but merges after it.

### Phase 3 — Traveller kinds and the displaced's forms (L)

- **Specs:** T K1, K2, F1-F4 (the categories this phase uses), R1-R2 (registry entries), §3.8-3.10, §4.2, §8's claim table, I2's sub-menu; P §6.2 (the pattern the three forms will follow).
- **Scope:**
  - `TravellerKind` (Domain enum) and `CaseBlueprintSO.kind`; one Displaced blueprint on every day, replacing `CaseBlueprint_Investigation`.
  - `ClueCategory` gains CitizenId, Destination, Incident, DepartureDate and Expiry, appended; a pin test fixes every int value (R1-003), and later phases extend it.
  - `DocumentTemplateSO` gains `formNumber` and `askableBy`. TC-610, TC-620 and TC-630 replace `DocTemplate_Passport` and `DocTemplate_Permit`, whose assets are deleted.
  - `Seeds.ForAccount` ("ACCT"): the Displacement No., the incident, the found date and the certificate's Valid Until (honest dates only; no date is checked yet).
  - The registry entry rows (§4.2). The per-kind claim table (the displaced line).
  - The "Request papers >" sub-menu: the displaced have two forms on request; `DialogChecks.MenuProblems` counts it as one hub entry.
  - `CaseInstance`'s duplicate claim fields merged (R3-020). The dead legacy clue path (`ClueSO`, the clue stream) removed (R3-012).
  - The desk paper still uses `PaperFace`. If TC-610 (six fields and a photo) does not fit at 720p, its Valid Until sits on page 2 until phase 4.
- **Files:**
  - Domain: `ClueCategory.cs`, `ClueLabels.cs`, `Seeds.cs`, `InterviewScript.cs`, `InterviewContent.cs`, new `TravellerKind.cs`, new `AgencyNumbers.cs` (the number, incident and date makers).
  - Scripts: `CaseBlueprintSO.cs`, `DocumentTemplateSO.cs`, `CaseFactory.cs`, `CaseInstance.cs`, `ClueSO.cs` (deleted), `GameManager.cs` (the claim).
  - Content: `Assets/Data/Investigation/` (three templates, `CaseBlueprint_Displaced`), `world_source.json` (claims, the sub-menu labels, UI strings).
  - Editor: `WorldContentGenerator.cs`, `ContentLibraryValidator.cs` (the face-fit check runs here too, R6-021).
  - `docs/FEATURES.md`; the audit play job (the sub-menu; proofs against the new rows).
- **Tests first:**
  - `SeedsTests`: `ForAccount` distinct, with the distinctness test made one table (R3-035) and the new salt's numbers pinned (R1-002);
  - the `ClueCategory` pin test;
  - `InterviewScriptTests`: the sub-menu (1 + at most 3, Back, one-shot requests);
  - `AgencyNumbersTests`;
  - `DocumentRowsTests` over the three forms.
- **Unity:** Generate World. Play days 1-3 in the art office:
  - TC-610 on arrival; "Request papers >" with the Intake Declaration and the Return Order;
  - the declaration's coin proven against the ledger, the birth year against the registry;
  - a registry lookup by DP number;
  - the three forms held at 1080p and 720p (the face fit).
- **Golden effect:**
  - `cases.txt`: the documents, the registry rows and the numbers. The case stream's draws are unchanged, so every claim, name and birth date must match the baseline; only the papers differ.
  - The play transcript and saves; `data_hashes`.
- **FEATURES:** 11 (the account stream; the clue stream gone), 30 (the certificate on arrival, the rest on request), 58 (kinds; every traveller displaced for now), 64 (the claim per kind), 65 (the sub-menu), 68-69 (the certificate's identity fields), 76, 153-154.
- **Audit absorbed:** R1-003, R1-002 (in part), R3-020, R3-012, R3-035, R3-008 (the English literals in `CaseFactory` it touches), R6-021.
- **Split seam:** the sub-menu (with its tests) can land as its own commit series after the forms.
- **Depends on:** 1, 2.

### Phase 4 — Forms engine and the desk paper (L)

- **Specs:** P FO1-FO8, FO10, §6.1-§6.3, §6.5 (the desk renderer), TH2's explicit `IsDiegetic`; T F1.
- **Scope:**
  - Visuals: `FormLayout` with `FormSpec`, `FormBlock`, `FormCell`, `FormData`, `PlacedForm`, `FormMetrics` and `ITextMeasure`; `Barcode`.
  - `FormStyleSO` (`Assets/Data/Forms/FormStyle_Agency.asset`), with its contrast pairs checked by Build Office UI.
  - `DocumentTemplateSO.form`. `DocumentFieldSpec.page` goes (FO4), and with it the Domain's only UnityEngine use, so `TimeDesk.Domain` becomes engine-free (R1-001, R6-020).
  - `CaseFactory` sets `DocumentField.page` from `FormSpec.PageOf` and `DocumentInstance.serial` from `Seeds.ForForms` (a salted value, not a stream draw).
  - `DeskDocument` draws the form: TMP text per item, one mesh per paper for boxes, rules and the barcode, hits through `FormLayout.SlotAt`.
  - `PaperFace` retires into `FormLayout` (`FaceRect` stays; `DeskConfigSO.face` moves into `FormStyleSO`).
  - The forms of TC-610, TC-620 and TC-630; a code-drawn seal placeholder.
  - The `DiegeticForm` role appended, with `ThemeRoles.IsDiegetic` made an explicit list (R2-005).
- **Files:**
  - Visuals: new `FormLayout.cs`, new `Barcode.cs`, `PaperFace.cs` (reduced to `FaceRect`), `ThemeRoles.cs`.
  - Scripts: new `Forms/FormStyleSO.cs`; `DocumentTemplateSO.cs`; `CaseFactory.cs`, `CaseInstance.cs`; `Office/Desk/DeskDocument.cs`, `DeskController.cs`; `Office/DeskConfigSO.cs`.
  - Domain: `DocumentField.cs`, `DocumentRows.cs`, `Seeds.cs`, `TimeDesk.Domain.asmdef`.
  - Editor: `OfficeSceneUIBuilder.Desk.cs` (the paper's text templates, line material, seal quad), `OfficeSceneUIBuilder.cs` (form checks, form contrast), `ContentLibraryValidator.cs`.
  - The three templates; OfficeGameplay rebuilt; `docs/FEATURES.md`.
- **Tests first:**
  - `FormLayoutTests`: no overlaps; every item inside the page; reading order; spans and the two-row photo; page breaks and `PageOf`; flow height with a fake measure; `SlotAt`; `Check`'s reports;
  - `BarcodeTests`;
  - `SeedsTests` (`ForForms`);
  - `DocumentRowsTests` (the page from the form);
  - `ThemeRolesTests` (every enum value);
  - `PaperFaceTests` folded into `FormLayoutTests`.
- **Unity:**
  - Build Office UI: `FormLayout.Check` and the form contrast are clean in the log.
  - Play: each form held at 1080p and 720p (floors met, nothing clipped); examine; hover; a box pick; a proof from a held paper.
  - The readability audit on the held paper under the art's tonemapping.
  - The profile: no per-frame allocation from the paper.
- **Golden effect:** `scene_OfficeGameplay` (the paper parts); `cases.txt` (the serials); the static metrics (`PaperFace` gone, new types allowed). The play transcript must not change.
- **FEATURES:** 30 (the paper shows its form), 145 (the 720p floors for forms), 154 (the builder's form checks).
- **Audit absorbed:** R1-001, R6-020, R2-026, R2-005, R6-005 (form colours have one source, `FormStyleSO`), R2-020 (in part), R6-015 (new enum writes use the value, not the index).
- **Split seam:** the Visuals engine and its tests (offline only) as one commit series; the desk renderer and the builder as the second.
- **Depends on:** 3.

### Phase 5 — Scanned copies as forms (M)

- **Specs:** P FO1 (`FormView`), FO9 (documents), §2.4, §6.5, AP7 (a document's pages scroll).
- **Scope:** `FormView`, the uGUI renderer (pooled parts, a pick button per slot, the backing and its SCANNED strip). The scanned-document window shows the form: pages stacked in a scroll, `DocumentWindowController`'s paging copy gone, one display-name source (R4-006). It is still a window; the app comes in phase 16.
- **Files:** new `UI/Forms/FormView.cs`; `UI/DocumentWindowController.cs` (a thin window over `FormView`); `UI/InvestigationUIController.cs`; `Editor/OfficeSceneUIBuilder.cs` (the document window's `FormView` templates); OfficeGameplay; `docs/FEATURES.md`.
- **Tests first:** `FormLayoutTests` at the PC width (542 u; H = 708 u; the §6.3 sizes).
- **Unity:** Build Office UI. Play: scan each form; pick on the PC; a PC row against a book row; 1080p and 720p; the readability audit in nine themes (forms unthemed, chrome themed).
- **Golden effect:** `scene_OfficeGameplay`. The play transcript is unchanged.
- **FEATURES:** 68 (the scanned copy is the same form on the backing).
- **Audit absorbed:** R4-006, R4-007 (the document window's paging copy).
- **Depends on:** 4.

### Phase 6 — The present, accounts and rich tourists (L)

- **Specs:** T H1, K4, C1, R1-R4, §3.1-3.2, §4.1, §4.3; K1 (`RichTourist`), K2.
- **Scope:**
  - The present: the neutral present in the JSON (`present`); the choice between the leader's Future place and it as a Domain rule (`Present.Choose`); `TodaysWorld.Present`; `FillFacts` adds its row, so every book lists it from day 1.
  - 2150 names and birth years (K4); gender from the name lists.
  - Citizen Accounts on `Seeds.ForAccount` (Domain `AccountMaker`: the §4.3 order, `agency.transponders`), in the registry as rows (Records, Forms on file, Travel); lookup by Citizen ID.
  - TC-101 and TC-230, with their forms. `ClueCategory` gains AccountStatus, TransponderId, TransponderClass and Debt.
  - The `RichTourist` blueprint: day 1 is rich tourists (honest in this phase) beside the displaced, until phases 8 and 12 settle the mix. Their claim line; "Request Departure Manifest" as a direct entry.
  - The wallet word from `TodaysWorld.Present` (R4-018).
  - The account generation leaves `CaseFactory` with explicit inputs (R3-025).
- **Files:**
  - Domain: new `AccountMaker.cs`, new `Present.cs`, `CitizenRegistry.cs`, `ClueCategory.cs`, `ClueLabels.cs`, `NameRoster.cs`, `TravellerGender.cs`, `FactTable.cs`.
  - Scripts: `TodaysWorld.cs`, `ContentLibrarySO.cs`, `CaseFactory.cs`, `CaseInstance.cs`, `UI/Theme/CultureThemeService.cs`.
  - Content: `Assets/Data/Investigation/` (TC101, TC230, `CaseBlueprint_RichTourist`); `world_source.json` (`present`, `agency.transponders`, `days[].kinds`, claims).
  - Editor: generator and validator. `docs/FEATURES.md`; the audit play job.
- **Tests first:**
  - `AccountMakerTests`: ranges per status, unique numbers within the day, the fixed draw order, determinism;
  - `PresentTests`: a leader, no leader;
  - `NameRosterTests` and `TravellerGendersTests` (the present's lists);
  - `FactTableTests`: the present's row; a null origin label rejected (R1-017);
  - `SeedsTests` (pins).
- **Unity:** Generate World. Play day 1: rich tourists present the visa, the manifest on request, a lookup by Citizen ID, the present's row in each book and in the Costume Guide, the wallet word; with a forced leader (debug panel), the present is that leader's Future place.
- **Golden effect:** `cases.txt` (day 1's mix, 2150 names); the play transcript and saves.
- **FEATURES:** 54 and 131 (the present), 58 (tourists), 59, 50 (accounts), 64, 65, 71 (the present's row from day 1), 147 (the wallet from the present), 152-153.
- **Audit absorbed:** R3-025 (the account part), R4-018, R1-017, R1-002 (pins), R3-008.
- **Split seam:** the present and the books (H1) first; then accounts and the rich tourist.
- **Depends on:** 3, 5.
- **As built (2026-09-26, branch `redesign/p06-present-accounts`, main `83a43a6` merged in; phase 5 was not built, so TC-101/230 print through phase 4's desk forms and the scanned copy follows when phase 5 lands):**
  - The present: `world_source.json` `present` (name, year, facts, wardrobe and phase 10's `kit`; its Culture derived as a place's) → `PresentContent` on the library (its `look` is phase 10's `PresentLook`: the clothes and the kit; one present model, `ContentLibrarySO.PresentLook` reads it); `ContentLibrarySO.BuildPresent(history)` (`Present.Choose`: the leader's Future place, else the neutral present; builds one place) → `TodaysWorld.Present` (a `PresentPlace` with its `Wardrobe`: the leader's Future outfit or the neutral clothes, which phase 10's `CaseFactory.PresentSource` wears as the 2150 clothes; the kit stays the neutral kit); `BuildToday` adds its row with `Present.AddRow` (never twice). `Present.NeutralNationId` = "neutral" is the one token. The wallet reads the present's currency (R4-018).
  - The day's kinds: `DayPlanSO.Kinds` (`KindWeight`: blueprint, weight) from `days[].kinds`, and `content.blueprints` (kind → asset) replace the single wired blueprint and `CaseBlueprintSO.difficulty`. `TravellerKinds.PickWeight` keeps a premade's slot displaced. **Phase 8** adds `honest` to `KindWeight` and `days[].kinds`, and `CaseBlueprint_PoorTourist` to `content.blueprints`.
  - A citizen (`TravellerKinds.IsCitizen`): a name from `ContentLibrarySO.CitizenNames()` (the Future lists together), born in the present's years, `tongueId` empty, the destination's dress over the family country's looks (`CaseFactory.FamilyOf`), `CaseInstance.account` (`CitizenAccount`) from `AccountMaker.Make(AccountRequest, agency.accounts, agency.transponders, today, takenToday, account stream)`. The draw order is ID, debt, transponder model and serial, lineage, trips, one Valid Until per expiring form; **phase 8** inserts the proof of means after the debt and the waiver after the transponder (their own statuses only, so a rich tourist's draws never move). `agency.accounts` already holds all three statuses' ranges and `agency.transponders` both classes. `CaseFactory.ResolveFieldValue` reads the account per category and gives each expiring form its own Valid Until (the form's index among the traveller's expiring forms).
  - **Phase 7:** the rich blueprint's `contradictionChance` is 0 and `TravellerKinds.MayLieAboutPlace` keeps citizens off the lie stream; `Lies.Roll` and `RecordLies` replace that gate. Record rows already carry their categories (`AccountRecords.Record`: Name, CitizenId, BirthDate, AccountStatus, Debt, TransponderId, TransponderClass, Destination as evidence), so `RecordMismatch` over them needs no view change. **Phase 8** turns the Waiver / Proof of means rows ("None on file") into evidence rows of `WaiverNo`, `Credit`, `Funds`, `PolicyNo`.
  - The clerk: phase 25's `IClerkAccountSource` stays the one source; `AccountRecords.Clerk(profile, Account.ExtractRows(...))` puts the clerk's own account in Citizen Records (`GameManager.BuildRegistry`), no row evidence; the clerk's Citizen ID is reserved in the day's numbers. Phase 13a's debt shows there with no change.
  - Deviations kept for later phases: rich tourists answer the day's "Ask about home >" questions with the destination's facts and use the place's small talk (I1, phase 11); the present's label reads "(Future)" until phase 12 renames the era "2150" (H2).

### Phase 7 — Record lies, fault kinds and paper proofs (L)

- **Specs:** T L1, L2, L4, K5, P1, P2, §5.2, §6.2-6.4 (the parts for L1, L2 and the cross proof); P CM1.
- **Scope:**
  - `Lies.Roll(liarChance, lieKinds, rng)`: the roll moves out of `Plan`, and the kind pick draws only when 2 or more kinds are enabled, so today's displaced keep their draws.
  - Domain `RecordLies` (L1 forged and borrowed, L2) with the makers for AccountStatus, TransponderClass, TransponderId, CitizenId and BirthDate. `LiePlan` holds both kinds of tell; `Forgery.IsProvableCategory`.
  - Domain `PaperChecks.Contradictions`, and `DiscrepancyLog`'s `CrossMismatch` (key `deviation.crossMismatch.papers`). `ValuesMatch` moves to a small `Values` helper that both read (R1-004).
  - `VerdictRules` inputs become `hasDeviationFault` and `hasDirectiveFault`. `CaseInstance.directiveFault` and `recordTells`; `CaseVerdict.faultReason`; the citations `citation.acceptedWrong.{reason}`.
  - One decision handler in `GameManager` (R3-017) and one wrong-decision path in `ShiftScoring` (R3-019). The unreachable legacy era-pick loop goes (R3-011), since `Test_DayLoop` left the build in phase 0.
  - The day-1 procedure line; the rich blueprint's liar chance on; `days[].lies` for day 1 (L1, L2).
- **Files:**
  - Domain: `Lies.cs`, new `RecordLies.cs`, new `PaperChecks.cs`, new `Values.cs`, `Forgery.cs`, `DiscrepancyLog.cs`, `ClueLabels.cs`, `VerdictRules.cs`, `ShiftLedger.cs`.
  - Scripts: `CaseFactory.cs`, `CaseInstance.cs`, `Shift/ShiftScoring.cs`, `GameManager.cs`, `DayPlanSO.cs`, `OfficeUIController.cs` (the legacy loop's remains).
  - Content: `world_source.json` (lies per day, report and citation strings, the procedure line); generator and validator.
  - `docs/FEATURES.md`; the audit play job (record tells and a cross proof; the planned mistakes kept).
- **Tests first:**
  - `LiesTests`: `Roll`'s draw order, with the golden orders rewritten; `Plan` without its roll; a one-kind day draws as before;
  - new `RecordLiesTests`: each maker; the variants; only named fields rewritten; every forged field differs and is provable;
  - new `PaperChecksTests`: the same category on two documents; directive-only categories skipped; none for an honest set;
  - `DiscrepancyLogTests`: the cross proof only with a tell; an answer against a paper stays a hint; one proof per category;
  - `VerdictRulesTests`: the §5.2 table; `ForgeryTests`; `ShiftLedgerTests`.
- **Unity:** play day 1:
  - an L1 forgery (visa class vs account status);
  - an L1 borrowed manifest (the visa's ID vs the manifest's: the cross proof);
  - an L2 (birth year vs account);
  - a wrong accept's citation reason, and an unproven denial;
  - screenshots of the report.
- **Golden effect:** `cases.txt` (tourists lie from day 1; each displaced traveller's lie draws must equal the baseline); the play transcript and saves (reasons, citations).
- **FEATURES:** 73-80 (the cross proof; record proofs for every record category), 116-119 (the reasons; provable record tells), 124, 58.
- **Audit absorbed:** R3-017, R3-019, R3-011, R1-010, R1-004, R1-014, R3-024 (the verdict part of `GameManager`).
- **Split seam:** `Lies.Roll` with `RecordLies` first; then the cross proof and the verdict inputs.
- **Depends on:** 6.

### Phase 8 — Poor tourists and proofs of means (M)

- **Specs:** T K3, K5 (`honest`), F2, §3.3-3.6, §4.1 (the proof rows), §4.3 (the proof draw), I2 (request groups, missing-form replies); P §2.4 (the group chip).
- **Scope:**
  - TC-310, TC-415, TC-416 and TC-417 with their forms (TC-310's `Signature` block).
  - The `PoorTourist` blueprint with a form choice group: `askGroup` "proof", drawn on the account stream by `agency.proofWeights`.
  - Account rows: waiver number, the proof of means (a credit line, savings or a policy), debt. `ClueCategory` gains WaiverNo, Credit, Funds, PolicyNo and Signature.
  - Day 1's poor entry `honest` (the K5 skip, with no draw).
  - The sub-menu for citizens: Manifest, Waiver, Proof of means. Missing-form replies per kind (`missingFormReplies`).
- **Files:**
  - Domain: `InterviewScript.cs` (groups, replies, `DialogChecks`), `InterviewContent.cs`, `ClueCategory.cs`, `ClueLabels.cs`, `AccountMaker.cs`, new `FaultOrder.cs`.
  - Scripts: `DocumentTemplateSO.cs`, `CaseBlueprintSO.cs`, `CaseFactory.cs`, `DayPlanSO.cs`.
  - Content: four templates and `CaseBlueprint_PoorTourist`; `world_source.json` (`agency.proofWeights`, `missingFormReplies`, `honest`); generator and validator.
  - `docs/FEATURES.md`; the audit play job (requests through the groups).
- **Tests first:**
  - `InterviewScriptTests`: a group is one entry; a missing form's one-shot reply; the hub's worst case of 8;
  - `AccountMakerTests`: the proof choice and its values, in the fixed order;
  - new `FaultOrderTests`: K5's order, and an `honest` entry skipping every roll with no draw;
  - `DialogChecks` allocation clean-up (R1-019).
- **Unity:** Generate World. Play day 1:
  - a poor tourist's four papers on the desk (the four spawn slots, 720p);
  - the proof of means handed over from the group entry;
  - a rich tourist asked for a waiver gives the missing-form line;
  - every poor tourist that day is honest.
- **Golden effect:** `cases.txt` (day 1's mix); the play transcript.
- **FEATURES:** 30 (four papers), 33 (the sub-menu counted as one entry), 65 (groups, missing forms), 58 (poor tourists, honest on day 1), 50 (Forms on file).
- **Audit absorbed:** R1-019.
- **Depends on:** 7.

### Phase 9 — Labourers and the directives (L)

- **Specs:** T K1 (`Labourer`), P3, P4, §3.7, §5.1, §5.3-5.4 (`PaperSet`, `DebtStanding`), §6.1 (L3, L4, L5, L10), §6.3, L5's `Seeds.ForFaults`.
- **Scope:**
  - `TravelRuleSO` gains `PaperSet` and `DebtStanding` (the other new types come with their phases).
  - Domain `Directives`: `CaseFacts`, `Breaks(type, facts)`, the makers, the directive lines.
  - `PlanViolators` counts the planned rules (`ViolatorSlots` unchanged). `Seeds.ForFaults` ("FALT"): the violation roll, the rule and the variant.
  - The `Labourer` blueprint (TC-520, TC-230, TC-310), the contract on the account (`agency.employers`); `ClueCategory` gains Employer, Term and Wage.
  - `RecordLies` gains L3, L4, L5 and L10, with the makers for Debt, Credit, Funds, PolicyNo, Wage, Term, Employer, Destination and WaiverNo.
  - `days[]` gains `lies`, `violationChance` and `guarantee`. `DayPlanSO.ResolveSchedule` moves to a salted stream (R3-010).
- **Files:**
  - Domain: new `Directives.cs`, `RecordLies.cs`, `Seeds.cs`, `ClueCategory.cs`, `ClueLabels.cs`, `AccountMaker.cs`.
  - Scripts: `Investigation/TravelRuleSO.cs` (types, summaries), `DayPlanSO.cs`, `CaseFactory.cs`, `CaseInstance.cs`, `GameManager.cs` (the briefing lines).
  - Content: TC520 and `CaseBlueprint_Labourer`; `world_source.json` (rules, days, employers).
  - Editor: generator and validator (rule types through `ParseEnum`, R6-002; the directive rules written once, in Domain, and read by both, R6-006 in part).
  - `docs/FEATURES.md`; the audit play job (day 4's planned mistake is still "the first rule violator is accepted").
- **Tests first:**
  - new `DirectivesTests` (each type's decision table, each maker);
  - `ViolatorSlotsTests` (the §2.3 guarantee counts);
  - `RecordLiesTests` (the new makers);
  - `SeedsTests` (`ForFaults`);
  - `DayPlansTests` (the new fields);
  - `FaultOrderTests` (the violation roll in its place).
- **Unity:** Generate World. Play days 2-3:
  - a paper-set violator (unsigned waiver: denied without evidence, or accepted with "Approved incomplete paperwork.");
  - a frozen account;
  - an L5 contract proven against the account; an L3 cross proof; an L10;
  - the briefing and Directives lines.
- **Golden effect:** `cases.txt` (days 2-6); the play transcript and saves.
- **FEATURES:** 58 (labourers), 118, 120 (a guaranteed violator per directive), 155 (the directive types), 23 and 48 (the lines), 116.
- **Audit absorbed:** R3-010, R6-002, R6-006 (in part).
- **Split seam:** the directives with their makers first; then labourers and the four lies.
- **Depends on:** 8.

### Phase 10 — Dress for the destination (M)

- **Specs:** T C1-C5, P5, §7.1-7.2, the costume roll (§6.4); `DressForDestination` (§5.3).
- **Scope:**
  - Costume errors on the fault stream, in three variants by `costumeErrors` weights: another place's item (`Looks.CanLeak`), the present's whole look (a whole-look mode that flags every garment), one item of the 2150 accessory kit.
  - The kit in the present's wardrobe (accessory slot, leakable, art nation `neutral`), drawn by the runtime placeholders until the art lands.
  - The `DressForDestination` line and its day-2 guarantee. The panic reason: the deviation line, the citation and the next morning's news line.
  - The Costume Guide's present row lists the kit. Its era grouping is the app's (phase 16).
- **Files:** Domain `Looks.cs`, `LookData.cs`, `Directives.cs`, `FaultOrder.cs`; `CaseFactory.cs`; `world_source.json` (the present's wardrobe and kit, `costumeErrors`, strings); generator and validator (the kit's items and the present's Culture fact); `docs/FEATURES.md`.
- **Tests first:**
  - `LooksTests`: the whole-look mode; an accessory leak; `CanLeak` unchanged; the error costs no look draw;
  - `FaultOrderTests`: the costume roll;
  - `DiscrepancyLogTests`: a garment against the present's row names 2150; the panic key.
- **Unity:** play day 2 with each variant (forced by seed or the debug panel):
  - Look ▸ a garment against the Costume Guide gives DRESS INCORRECT;
  - a wrong accept gives the panic citation, and the next morning the panic line;
  - screenshots of the placeholder 2150 clothes and accessory.
- **Golden effect:** `cases.txt` (days 2+); the play transcript.
- **FEATURES:** 95 (dressed for the destination; costume errors), 77-80 (garment proofs; the panic wording), 116.
- **Audit absorbed:** R1-006, R1-011, R1-012.
- **Depends on:** 9.

### Phase 11 — Smuggling and dates (L)

- **Specs:** T L1 (the present as a candidate, the category filter), L6, F7, H3, I1, §5.3-5.4 (`NoPresentGoods`, `PaperDates`), §9's carries.
- **Scope:**
  - `Lies.Plan`'s category filter; the present as a `HomeCandidate` from `TodaysWorld.Present`; L6 on the manifest or in an answer.
  - `NoPresentGoods` (day-4 guarantee).
  - `PaperDates`: `Breaks` reads the manifest's or return order's departure and every Valid Until against `AgencyCalendar.Today`; the makers (a departure 1-3 days off, a Valid Until 1-30 days past).
  - `questions[].kinds` and `overrides[].kind`; the trip questions from day 4 with their announcements.
  - `CaseInstance.trueHome` becomes `tellSource` (a `PlaceRef`) across its six readers; `HistoryService.RecordCarry` from the present.
- **Files:**
  - Domain: `Lies.cs`, `Directives.cs`, `InterviewContent.cs`, `InterviewDay.cs`, `Carries.cs`, `History.cs`.
  - Scripts: `CaseInstance.cs`, `CaseFactory.cs`, `ShiftLedger.cs` (Domain), `GameManager.cs`, `Shift/ShiftScoring.cs`, `Timeline/HistoryService.cs`, `TodaysWorld.cs`.
  - Content: `world_source.json` (questions, rules, days); generator and validator.
  - `docs/FEATURES.md`; the audit play job (the trip questions; a spoken smuggling proof).
- **Tests first:**
  - `LiesTests`: the present as a candidate; the filter;
  - `DirectivesTests`: `PaperDates` (today, past, future), `NoPresentGoods`;
  - `InterviewDayTests`: questions per kind; day 4's unlocks;
  - `CarriesTests` and `HistoryTests`: the present as a carry source.
- **Unity:** play day 4 and its night:
  - a smuggler proven on the manifest and in an answer;
  - a wrong departure date and an expired visa, both denied without evidence;
  - the calendar read;
  - an accepted smuggler, then the next morning's carry line and the destination's revised Technology.
- **Golden effect:** `cases.txt` (day 4+); the play transcript and saves (carries).
- **FEATURES:** 66 (questions per kind), 67, 119, 121 and 132 (carries from the present), 21.
- **Audit absorbed:** R1-020, R3-015 (the unread `sent:*` counters, in the files it touches).
- **Split seam:** smuggling and the carries first; then the dates and the trip questions.
- **Depends on:** 10.

### Phase 12 — The displaced on day 5, the famous on day 6 (M)

- **Specs:** T K3, L7, L8, H2, I3, `ReturnHome` (§5.3), §2.3's final mix, §11 (the premades).
- **Scope:**
  - The Displaced blueprint leaves days 1-4. L8 (the fake displaced, with the present as a candidate).
  - `ReturnHome` (day-5 guarantee).
  - `translation.fromDay` 5, the notice in day 4's paper ("from tomorrow").
  - H2: `TodaysProfiles` offers no Future place, the Future-day validator checks go, the era is shown as "2150".
  - The premades: Senenmut forced into day 6 slot 8, "Socrates" into slot 11, the pools on day 6; the validator warns about a forced premade in the first half of a day with rules.
  - The home questions on day 5.
- **Files:**
  - Content: `world_source.json` (days, premades, translation).
  - Editor: `WorldContentGenerator.cs` and `.Translation.cs` (the notice's night), `ContentLibraryValidator.cs`.
  - Scripts: `ContentLibrarySO.cs`, `TodaysWorld.cs`, `DayOrchestrator.cs` (R3-037).
  - Domain: `Directives.cs`, `Lies.cs`.
  - `docs/FEATURES.md`; the audit play job (the displaced from day 5; the Speech translator bought on night 4).
- **Tests first:** `PremadesTests` (day-6 slots); `HistoryTests` (no Future destination); `TranslationTests` (from day 5, the notice's night); `DirectivesTests` (`ReturnHome`); `LiesTests` (L8).
- **Unity:** Generate World. Play days 4-6:
  - the day-4 notice;
  - a Speech translator bought on night 4 flips day 5's lines; without it, the key words;
  - a fake displaced person's origin proof names 2150;
  - Senenmut in day 6 slot 8, and the "Socrates" impostor.
- **Golden effect:** `cases.txt` (days 1-6 and the eight leader variants); the play transcript and saves; `world_generate`.
- **FEATURES:** 54 and 131 (the Future is the present), 58, 60 (premades on day 6), 66, 86 (from day 5), 155.
- **Audit absorbed:** R3-037, R6-022.
- **Depends on:** 11.

### Phase 13 — Strandings and the clerk's debt (M)

- **Specs:** T S1-S3, D1-D3, T1, §4.4, §10.
- **Scope:**
  - `Seeds.ForStrandings` ("STRD"); Domain `Strandings` (the roll at the shift's end in queue order, on the account's class; the fine rule).
  - Carries from the present into the destination through the existing path.
  - `news.stranded`, the `news.debt` pool and yesterday's counts.
  - The ledger lines: leisure and Debt Relief departures, the instalment, the stranding fine.
  - `agency.clerk`; `WorldState.clerkDebtPaid` (additive, save version 2); Domain `ClerkDebt.Instalment`.
  - The bankrupt ending: the same condition, the new copy, the clerk's own TC-520 drawn by the form engine on the ending panel.
- **Files:**
  - Domain: new `Strandings.cs`, new `ClerkDebt.cs`, `Seeds.cs`, `ShiftLedger.cs`.
  - Scripts: `WorldState.cs`, `GameManager.cs` (the shift's end), `Timeline/HistoryService.cs`, `Timeline/TimelineService.cs` (news lines), `Endings/EndingService.cs`, `UI/TitleUIController.cs`.
  - `Editor/TitleSceneBuilder.cs` (the ending panel's form); `Assets/Data/Endings/Ending_Bankrupt.asset`.
  - Content: `world_source.json` (`agency.clerk`, `strandChance`, `strandFine`, the news pools); generator and validator.
  - `docs/FEATURES.md`; the audit play job (ledger lines; `clerkDebtPaid` in the saves).
- **Tests first:** new `StrandingsTests` (one draw per accepted Economy traveller; the real class decides; the fine's cases); new `ClerkDebtTests` (the share; never past what is owed; paid off stops); `SeedsTests`; `ShiftLedgerTests`; `EndingRulesTests` (the condition unchanged).
- **Unity:** play days 2-4 with a forced stranding:
  - the ledger's instalment, and a fine for a traveller let through unsigned;
  - the next morning's stranding line and the carried Technology;
  - Continue keeps `clerkDebtPaid`;
  - a forced bankruptcy (debug panel) shows the Debt Relief ending with the clerk's contract;
  - Title rebuilt if its builder changed.
- **Golden effect:** the play transcript and saves (ledger, money, carries, the new field); `scene_TitleScene`. `cases.txt` is unchanged.
- **FEATURES:** 12 (the bankrupt ending is the clerk's Debt Relief departure), 124 (ledger lines), 23 (news lines), 132 (strandings carry), 9 (the additive save field).
- **Audit absorbed:** R2-007, R2-011 and R3-016 (the trace logs in the files it touches), R3-018, R3-023, R2-010 and R3-021 (the stability range and rule numbers as named knobs where touched), R3-005, R3-031, R6-016 (the Title builder's scene check).
- **Depends on:** 12.

### Phase 14 — Windows and the compare dock (M)

- **Specs:** P WN1-WN4, WN6-WN7, DK8 (taskbar buttons), DK9, CM2, KB3 (the menus and Start-menu part), §3.2, §4.1.
- **Scope:**
  - Domain `WindowStack`, `ClickTiming` and `DesktopEscapeRule` (menus, Start, drags).
  - `DesktopWindowManager`: focus on a press inside a window, exact maximise above the dock, restore. `DesktopWindow` (was `OSWindowChrome`, with `FormerlySerializedAs`) and `WindowDrag` (was `DraggableWindow`, without its dead members).
  - Taskbar buttons.
  - The dock: `CompareController` rewired, the bar texts cached, the old CompareBar gone.
  - `OfficeViewController` defers Escape to the desktop's stamp.
  - `DesktopConfigSO` (`Desktop_Default.asset`).
- **Files:**
  - Domain: new `WindowStack.cs`, new `ClickTiming.cs`, new `DesktopEscapeRule.cs`.
  - UI: new `DesktopWindowManager.cs`, `DesktopWindow.cs` and `WindowDrag.cs` (renamed files), new `CompareDock.cs`, `DesktopShell.cs`, `CompareController.cs`.
  - `Office/OfficeViewController.cs`; new `DesktopConfigSO`; `Editor/OfficeSceneUIBuilder.cs`; OfficeGameplay; `docs/FEATURES.md`.
- **Tests first:** `WindowStackTests`, `ClickTimingTests`, `DesktopEscapeRuleTests`.
- **Unity:** Build Office UI. Play:
  - focus by clicking inside a window; minimise and restore from the taskbar; maximise stops above the dock;
  - Escape closes the Start menu before the frame;
  - the dock shows MATCH, MISMATCH and DEVIATION LOGGED under covering windows;
  - typing into the Records field through the frame (P §15's risk);
  - the clone;
  - the readability audit (the dock, the taskbar buttons).
- **Golden effect:** `scene_OfficeGameplay`; the profile (the dock adds no per-frame allocation). The play transcript is unchanged.
- **FEATURES:** 41, 45, 72, 79.
- **Audit absorbed:** R4-015 (the maximise insets), R4-016, R4-017, R4-026 (the bar texts), R5-005 (the desktop's share).
- **Depends on:** 13 in merge order only; nothing in 1-13 is needed.

### Phase 15 — The Investigation façade (M)

- **Specs:** P RF1.
- **Scope:**
  - A behaviour-preserving split of `InvestigationUIController` into a façade (the same public API for `GameManager`) and `CaseDocumentsPresenter`, `InterviewPresenter`, `EvidencePresenter` and `DayReference`.
  - The reachability predicates keep their meaning, pinned by a Domain decision table with tests and by a scene assertion in the job (R4-022).
  - The desk unsubscribe goes through a remembered flag (R4-003). Accept and Deny are wired once (R4-004). The hide-then-callback pattern becomes one helper in the files touched (R4-025).
- **Files:** `UI/InvestigationUIController.cs` and new `UI/Investigation/*Presenter.cs`, `DayReference.cs`; new Domain `InvestigationWiring.cs`.
- **Tests first:** new `InvestigationWiringTests` (reachability).
- **Unity:** the gate is sameness: the golden play transcript, the saves, `cases.txt` and the scene dump must be byte-identical.
- **Golden effect:** none. The static metrics improve (the god class gone) and are re-baselined with that reason.
- **FEATURES:** none.
- **Audit absorbed:** R4-001, R4-003, R4-004, R4-022, R4-025, R3-036 (the null contracts in the touched calls).
- **Depends on:** 14.

### Phase 16 — The Investigation app (L)

- **Specs:** P AP1-AP2, AP5-AP8, AP3's one-pane case, WN5, CM5, DK7 (the case tiles), FO3, FO9 (the page kinds), §2.2-2.10, §2.6's era groups; T C3.
- **Scope:**
  - One window, "Investigation", with a case header (claim, counters, Accept and Deny), a toolbar (Back and Forward inactive until phase 18), and six tabs in one pane: Documents, Records, Reference, Transcript, Report, Rules. Every view is a form: TC-901, TC-911 to 916, TC-920, TC-930, TC-940.
  - Lists scroll: `PagedRowsWindow`, `ReferenceBookWindowController`, `TranscriptWindowController`, `CitizenRecordsWindowController` and `DocumentWindowController` are replaced.
  - Nothing opens or switches by itself (WN5, CM5).
  - `ReferenceBookSO.groupByEra` for the Costume Guide (Saleh's Q9).
  - The case tiles retire. An interim "Investigation" tile opens the app until phase 17.
- **Files:**
  - UI: new `InvestigationApp.cs`, `AppPane.cs`, `DocumentsView.cs`, `RecordsView.cs`, `ReferenceView.cs`, `TranscriptView.cs`, `ReportView.cs`, `RulesView.cs`; the five old window controllers deleted.
  - `Investigation/ReferenceBookSO.cs`; new `Forms/FormSpecSO.cs` and `Assets/Data/Forms/Form_*.asset`.
  - Editor: new `OfficeSceneUIBuilder.App.cs`.
  - Content: `world_source.json` (`app.*` strings, the book flag); OfficeGameplay; `docs/FEATURES.md`.
- **Tests first:** `FormLayoutTests` (the page kinds: flow tables, `RecordGroups`, era-group rows); new `ReferenceRowsTests` in Domain (the claimed row first, the era groups, the present's row last); new `TabOrderTests` (the default order).
- **Unity:** Build Office UI. The day-1 and day-2 play-through through the app:
  - scan, toast, Documents;
  - a record proof and a book proof; deny with evidence; the decision clears the case;
  - the Costume Guide by era;
  - no window opens by itself; the no-case state on the clone;
  - the readability audit in nine themes at 720p.
- **Golden effect:** `scene_OfficeGameplay`; the play transcript (the job's PC path drives the app).
- **FEATURES:** 43, 47, 48, 49, 50, 64, 68, 70, 71, 73, 78, 81.
- **Audit absorbed:** R4-005, R4-007 (with `PagedRowsWindow`), R6-004 (a checked `SetRef` in the new builder partial), R6-008 (one convergence policy for the new builder code), R6-012 (the app's builder in its own partial), R6-009 (the dead migrations in the builder files it touches), R4-021 (in part).
- **Split seam:** the shell with Documents, Records and Reference first; then Transcript, Report, Rules and the no-steal events.
- **Depends on:** 15 (also 5, 10 and 12, all merged by then).

### Phase 17 — Desktop icons (M)

- **Specs:** P DK1-DK6, DK8 (the Start menu), DK10, TH2 (the new chrome roles), TH3, the placeholder apps' retirement.
- **Scope:**
  - Domain `DesktopLayout`. `DesktopIconView` replaces `DesktopIcon`: select, drag, drop, arrange, save in PlayerPrefs, double-click or single-click, arrow-key selection, badges.
  - The desktop context menu ("Arrange icons") and the Start menu's apps. `DesktopPreferences`.
  - The chrome roles appended. The flavour labels go from 34 to 28 (4 added, 10 retired).
  - The Lexicon, Dialect and Material placeholders retire. Internet and Notes keep their current windows until phases 24 and 25; Mail and Citizen Account get their icons in phase 25.
- **Files:**
  - Domain: new `DesktopLayout.cs`.
  - UI: new `DesktopIconView.cs` (`DesktopIcon.cs` deleted), new `ContextMenu.cs`, new `DesktopPreferences.cs`, `DesktopShell.cs`.
  - Visuals: `ThemeRoles.cs`.
  - Content: `world_source.json` (flavour tables, palette rules). The builder; OfficeGameplay; `docs/FEATURES.md`.
- **Tests first:** new `DesktopLayoutTests`; `ThemeRolesTests`; `UiStringsTests` (the table sizes); the validator's strings.
- **Unity:** Generate World; Build Office UI. Drag, arrange, reload the scene and find the positions kept; both open settings; the keys; the badges; the icon plates over the eight wallpapers in the readability audit.
- **Golden effect:** `scene_OfficeGameplay`; the play transcript (the job opens the app from its icon).
- **FEATURES:** 41-44, 46, 142 (28 labels).
- **Audit absorbed:** R4-020, R4-015 (the locked alpha), R6-023 (an automated check of the builder's label keys against `ui.strings`), R4-027.
- **Depends on:** 16.

### Phase 18 — Two panes and smart links (M)

- **Specs:** P AP3-AP4, AP9, LK1-LK2, CM2 (the dock's side links), CM3, §4.3.
- **Scope:**
  - The split (active pane, shared order, narrow tabs) and reordering (drag, keys; saved).
  - `NavHistory` for Back and Forward.
  - Domain `SmartLinks`, with the record categories, `Funds`, `PolicyNo` and the directive-only categories. ↗ on forms opens the other pane.
  - Keyed highlights: `CompareController.IsPicked` and `PicksChanged`. The dock's sides link back.
- **Files:** Domain new `SmartLinks.cs`, `NavHistory.cs`, `EntryKeys.cs`, `TabOrder.cs`; UI `AppPane.cs`, `FormView.cs`, `CompareController.cs`, `CompareDock.cs`; the builder; `docs/FEATURES.md`.
- **Tests first:** new `SmartLinksTests` (every §4.3 row), new `NavHistoryTests`, `TabOrderTests` (move, save).
- **Unity:**
  - the visa on the left, ↗, the Citizen Account on the right with the row focused, pick both, DEVIATION LOGGED;
  - one pick lit in both panes;
  - a restored window drops to one pane;
  - the order survives a reload.
- **Golden effect:** `scene_OfficeGameplay`.
- **FEATURES:** the app lines (panes, links, the dock's links).
- **Audit absorbed:** R4-026 (keyed highlights instead of re-created rows).
- **Depends on:** 17.

### Phase 19 — Search (M)

- **Specs:** P SE1-SE6 (SE5 for speech only), §4.2.
- **Scope:** Domain `TextMatch`, `SearchQuery` and `CaseIndex` (the day and case layers; untranslated lines indexed by their key words). The search field and the results panel; jump and flash. The Records lookup through the index; `CitizenRegistry.Find` retires.
- **Files:** Domain new `TextMatch.cs`, `CaseIndex.cs`, `CitizenRegistry.cs`; UI new `SearchBox.cs`, `SearchResultsView.cs`, `RecordsView.cs`; the builder; `docs/FEATURES.md`.
- **Tests first:** new `TextMatchTests`; new `CaseIndexTests` (layers, grouping, ranking; an untranslated line matches only its key words and speaker; the chip matching the same tongue and canonical); `CitizenRegistryTests` (the `Find` tests move to the index's rule).
- **Unity:** search across the sources, jump and flash; a day-5 displaced traveller without the Speech translator (typed words find only the key words); 720p.
- **Golden effect:** `scene_OfficeGameplay`.
- **FEATURES:** the search line; 50 (the lookup).
- **Audit absorbed:** R1-007 (`CitizenRegistry`'s last dead members).
- **Depends on:** 18.

### Phase 20 — Keys, clipboard, pins (L)

- **Specs:** P KB1-KB5, CP1-CP3, PR1-PR2, §3.4-3.5, §4.5.
- **Scope:**
  - `DesktopKeyboard` and `ShortcutMap`; `AppFocus` and the focus ring; the full `DesktopEscapeRule`; the F1 card.
  - `AppClipboard`, with copy and paste (a foreign clip becomes a chip in search).
  - `PinBoard` and `RecentList` in the sidebar. Zoom at 100, 125 and 150 %.
- **Files:** Domain new `ShortcutMap.cs`, `AppFocus.cs`, `AppClipboard.cs`, `PinBoard.cs`, `RecentList.cs`, `DesktopEscapeRule.cs`; UI new `DesktopKeyboard.cs`, `PinsPanel.cs`, `RecentPanel.cs`; `Office/OfficeViewController.cs`; the builder; `docs/FEATURES.md`.
- **Tests first:** new `ShortcutMapTests`, `DesktopEscapeRuleTests` (the full order), new `AppFocusTests`, `AppClipboardTests`, `PinBoardTests`, `RecentListTests`.
- **Unity:**
  - The editor is unfocused, so the job calls the commands directly. A keyboard-only case: search, jump, Space, link, Space, deny.
  - Zoom at 720p; a transcript line copied and pasted as a chip.
  - The report lists one manual pass of real keys for Saleh.
- **Golden effect:** `scene_OfficeGameplay`; the profile (the poller adds no per-frame allocation).
- **FEATURES:** the keys, clipboard and pins lines; 37 (the Escape order).
- **Audit absorbed:** R5-005 (the desktop's share of the one Escape arbiter).
- **Split seam:** the keys and the Escape chain first; then the clipboard, pins and zoom.
- **Depends on:** 19.

### Phase 21 — Steps (M)

- **Specs:** P ST1-ST4, §4.4, §4.8 (`pc.steps`); T §5.1.
- **Scope:** Domain `CaseSteps`, `CaseProgress` and `StepSpec`. The `pc.steps` sets (`RichTourist`, `PoorTourist` inheriting it, `Labourer`, `Displaced`, `default`), generated into `ContentLibrarySO.Pc` and validated. The panel, jumps and hints, the toggle and its preference.
- **Files:** Domain new `CaseSteps.cs`; UI new `StepsPanel.cs`; `ContentLibrarySO.cs`; `world_source.json`; generator and validator; the builder; `docs/FEATURES.md`.
- **Tests first:** new `CaseStepsTests` (each `StepWhen`; progress; a MISMATCH ticks like a MATCH; a manual override holds; inherit and override; an unknown type falls back to default); the validator's `pc.steps` rules.
- **Unity:** each kind's steps tick on their events; the toggle survives a reload; a step jump.
- **Golden effect:** `world_generate` and `data_hashes`; `scene_OfficeGameplay`.
- **FEATURES:** the steps line.
- **Depends on:** 20 (and 12 for every kind's set).

### Phase 22 — Scanner upgrades (M)

- **Specs:** P SC1-SC6, §4.10; T L4 (`PaperChecks`).
- **Scope:**
  - The Auto-Feed Scanner (`scanner_autofeed`, new) and the Analysis Scanner (`adv_scanner`, renamed, 300 cr). Archive Access and its discount effect, and the scanner-boost flag effect, retire.
  - `ScannerDay` read from the day-start snapshot.
  - The Auto-Feed queue in `DeskPapers`: hand-over order, one at a time, a held paper waits its turn.
  - The analysis pass on a scan by hand (`analysisScanSeconds`), Domain `PaperAnalysis` marking the first undocumented pair, the dashed marks in the Documents view, the scan strip's line.
  - The placeholder tray and lamp.
  - The shop's discounted price computed once (the discount mechanic stays).
- **Files:**
  - Domain: `DeskPapers.cs`, new `PaperAnalysis.cs`, new `ScannerDay.cs`.
  - Scripts: `Office/Desk/DeskController.cs`, `DeskScanner.cs`; `Office/DeskConfigSO.cs`; `UI/DocumentsView.cs`, `UI/Forms/FormView.cs`; `FormStyleSO` (the `analysis` colour); `Home/HomeManager.cs`, `UI/HomeUIController.cs`.
  - Content: `Assets/Data/Upgrades/` and `Assets/Data/Effects/` (the upgrades and effects).
  - `Editor/OfficeSceneUIBuilder.Desk.cs`; OfficeGameplay; `docs/FEATURES.md`.
- **Tests first:** new `PaperAnalysisTests` (the first undocumented pair; none for every honest traveller of the generation dump; directive-only categories never; one pair only); `DeskPapersTests` (the queue); new `ScannerDayTests`.
- **Unity:** buy both (debug money), then the next day:
  - handed-over papers scan in order, and a held paper waits;
  - an analysis scan by hand on an L3 case marks the pair on both copies; an honest case reads "no contradiction";
  - the mark's contrast in the audit; the Home shop's rows.
- **Golden effect:** `data_hashes` (upgrades, effects); `scene_OfficeGameplay`. The play transcript is unchanged: the job buys neither.
- **FEATURES:** 13 (the shop), 31 (the scanner's two upgrades).
- **Audit absorbed:** R2-006, R4-013, R5-010, R5-015, R2-013 (in `HomeManager`, touched).
- **Depends on:** 21 (also 16 and 7).

### Phase 23 — Balance pass (S)

- **Specs:** T §11 (balance), §2.3's knobs, S1-S3, D2; P SC (the Analysis Scanner's help).
- **Scope:** re-run the 50-run simulation (perfect and imperfect play) and set the epilogue thresholds (today Art 41, Science 26, Democracy 9). Tune the knobs: `violationChance`, `costumeErrorChance`, `strandChance`, `strandFine`, `garnishShare`, `proofWeights`, `contradictionChance` per kind, and the bankruptcy pressure. Tune the key-word lists only if Saleh asks.
- **Files:** `world_source.json`; the `GameConfigSO` asset; an untracked `_TimeDesk23Sim.cs` job; `docs/FEATURES.md` (12, the thresholds).
- **Tests first:** `EndingRulesTests` (the thresholds come from data).
- **Unity:** the simulation job; a days 1-6 play-through with screenshots.
- **Golden effect:** `cases.txt` (the chances); the play transcript and saves.
- **FEATURES:** 12.
- **Audit absorbed:** R2-010 and R3-021 (the rest of the rule literals), R3-032 (if the simulation needs `ShiftClockDriver`'s defaults in edit mode).
- **Depends on:** 22.

### Phase 24 — Internet (L)

- **Specs:** P IN1-IN6, §2.11, §4.6 (sites, news, history, ancestry); T R4, §10.
- **Scope:**
  - The browser (address, back and forward, the portal), `pc.sites` with no upgrade gate.
  - News: the front page from the morning paper's lines (debt and stranding lines included), `WorldState.newsArchive` with 30 issues.
  - Chronopedia: an article per place and the present; revisions and carries.
  - The Lineage Archive: premades, authored people, 2150 lineage cards.
  - Cross-app links: Reference "Revised" ↗ opens History. The Internet icon opens the browser.
- **Files:** Domain new `Sites.cs`, `NewsArchive.cs`, `NewsPages.cs`, `HistoryPages.cs`, `AncestryPages.cs`; UI new `BrowserWindow.cs`, `SiteRenderer.cs`; `WorldState.cs`; `GameManager.cs` (the issue recorded at the briefing); `Timeline/TimelineService.cs` (R3-007); `world_source.json` (`pc.sites`, `pc.pages`, `pc.ancestry`); generator and validator; the builder; `docs/FEATURES.md`.
- **Tests first:** new `SitesTests`, `NewsArchiveTests`, `NewsPagesTests`, `HistoryPagesTests`, `AncestryPagesTests`; the validator's `pc.sites` rules.
- **Unity:**
  - day 1, Home, day 2: the archive has day 1;
  - a forced leader (debug panel): a revision on its article and in Revisions; a Reference "Revised" ↗ opens it;
  - the pages at 720p and their contrast.
- **Golden effect:** the saves (`newsArchive`); `scene_OfficeGameplay`; `world_generate`.
- **FEATURES:** 46 (Internet is real), 23, 137, 9.
- **Audit absorbed:** R3-007.
- **Split seam:** the browser and News first; then Chronopedia and the Lineage Archive.
- **Depends on:** 23 (also 17, 18 and 13).

### Phase 25 — Mail, Citizen Account, Notes, Settings (L)

- **Specs:** P ML1-ML2, AC1, NT1, SG1, §2.12-2.15, §4.6; T D1-D3, §4.4.
- **Scope:**
  - Mail from its sources (the directive memo, the Times, citation notices, authored mail) and its read flags.
  - The Citizen Account app: the clerk's Record Extract without pick keys, and the statement TC-960 with DEBT RELIEF and OWED, written at the shift's end and at Home into `WorldState.accountDays`.
  - Notes with clippings and text.
  - Settings in sections (Desktop, Investigation, Keyboard).
  - The Mail and Citizen Account icons.
- **Files:** Domain new `Mailbox.cs`, `Account.cs`, `Notes.cs`; UI new `MailWindow.cs`, `AccountWindow.cs`, `NotesWindow.cs`, `UI/Theme/SettingsWindowController.cs`, `DesktopPreferences.cs`; `WorldState.cs` (`mailRead`, `accountDays`, `notes`); `GameManager.cs`; `Home/HomeEconomy.cs`; `world_source.json` (`pc.mail`); the builder; `docs/FEATURES.md`.
- **Tests first:** new `MailboxTests`, `AccountTests` (record and cap; OWED follows the instalments), `NotesTests`.
- **Unity:**
  - a citation, acknowledged, then its notice in Mail with a badge;
  - the statement after the shift and after Home;
  - a clipping survives Continue (a save and load in the job);
  - each Settings choice survives a reload.
- **Golden effect:** the saves (the new fields); `scene_OfficeGameplay`.
- **FEATURES:** 42 (Settings), 46 (Notes), new Mail and Account lines, 9.
- **Audit absorbed:** R2-011 (the Home trace logs, if phase 13 left any), R2-025 and R4-011 (Home strings through `UiText`, where touched).
- **Split seam:** Mail and the Account first; then Notes and Settings.
- **Depends on:** 24.
- **As built (2026-09-26, ahead of 4, 13, 17, 20 and 24; branch `redesign/p25-apps`):**
  - The apps open from the Start menu (Mail, Citizen Account, Notes above Settings) through `DesktopApps.OpenApp(id)` on the desktop canvas (ids `mail`, `citizen_account`, `notes`, `settings`, `internet`). **Phase 17** gives the icons the same call, and reads `MailFeed.Unread` for Mail's badge. The Notes tile already opens the Notes app.
  - The Citizen Account reads the clerk through Domain `IClerkAccountSource` (`Profile`, `Balance`, `Debt`, `ShiftInstalment`), implemented by `ClerkAccountSource`, which also writes the statement (`RecordShift` in `GameManager` at the shift's end, `RecordHome` in `HomeManager`). **Phase 13** returns `ClerkDebtState.Of(agency.clerk.startDebt, WorldState.clerkDebtPaid, the share in percent, frozen on the bankrupt ending)` from `Debt` and the shift's `ClerkDebt.Instalment` from `ShiftInstalment`, and adds its stranding fine to the ledger's penalties (or passes it): the rows, the statement's DEBT RELIEF and OWED columns, the status (Standard once paid off), the standing (Frozen) and the booked departure follow with no view change. Until then they read "–". The clerk's authored rows are `agency.clerk` (Citizen ID 773-2840-19; the name "Theo Marlow", born 9 Feb 2121, lineage "Victorian Britain" are placeholders for Saleh to author); phase 13 adds `startDebt` and `garnishShare` to that block.
  - Notes take clippings through `NotesWindow.Clip(Clipping)` (text, label, source key, traveller, foreign); the "Paste clipping" button pastes the system clipboard's text. **Phase 20**'s clipboard calls `Clip` with the clip's source, and Ctrl+V with the window focused and no field focused.
  - Mail's Times issue reads the day's paper from phase 24's archive (`WorldState.newsArchive`; today's from the morning paper), and its link opens that issue in the browser (`BrowserWindow.Go`, `NewsPages.IssueAddress`); "internet" is the browser's window.
  - The memo, the Record Extract and the Statement are drawn with today's widgets in `MailWindow.ShowMemo` and `AccountWindow.Draw`; **phases 4-5**' forms engine replaces those two methods with `Form_Memo`, `Form_RecordExtract` and `Form_Statement`.
  - Settings has Language, Motion and Keyboard (Show shortcuts opens a card of today's keys). The Desktop section (icon open mode, reset positions) and the Investigation section (steps, text size, tab order) come with phases 17, 18, 20 and 21, with `DesktopPreferences` holding their PlayerPrefs.
  - The authored mail is `world_source.json` `pc.mail` (the content sheets' `pcMail` and `pcMailBody`); the clerk's rows are `agency.clerk` (the `agency` sheet's `clerk.*` columns).
  - Citation notices come from the shift's ledger, so they are read on their day; a later day lists the day's memo, the Times issue and the authored mail.

### Phase 26 — Content spreadsheet import (M, later)

- **Specs:** T CS1, §15; P CT1.
- **Scope:**
  - A row-table layer in a tested assembly: CSV with UTF-8, quoting, `|` lists, blank cells as documented defaults, an unknown column as an error, duplicate ids as errors.
  - `Tools > TimeDesk > Export Content Sheets` (`world_source.json` to one sheet per table) and `Import Content Sheets` (sheets to `world_source.json`, or its per-table successor, in the same formatting).
  - XLSX read directly only if `System.IO.Compression` suffices (no third-party package).
  - Generate World unchanged in its role; the validator runs after. A schema note per table, written in this phase.
- **Files:** new `Assets/Scripts/Content/` (or Domain) row tables; new `Assets/Editor/ContentSheets.cs`; the schema doc; `docs/FEATURES.md`.
- **Tests first:** new `RowTableTests`; an export-then-import round-trip test.
- **Unity:** the round trip leaves `world_source.json` identical (EOL-normalised); Generate World is idempotent; the validator is clean.
- **Golden effect:** none (the round trip must be identical).
- **FEATURES:** 152 (the content source), a new "Content sheets" line.
- **Audit absorbed:** R6-014 (if phase 0 did not take it), R6-003, R6-006 (the rest), R6-007, R6-013.
- **Depends on:** 25 (every table defined).

### Phase 27 — Art hooks (M)

- **Specs:** P P10, §6.6, §14; T C4, §7.3.
- **Scope:** wire in the art as it is delivered:
  - the six icon glyphs, six tab glyphs, ten document-kind glyphs and three site glyphs; the UI kit (tab, badge, toast);
  - the agency seal, a face per form kind and the agency page face; the reference covers on the chips; the scanner's tray and lamp;
  - Batch 8 (2150 clothes, the present's wardrobe) and the 2150 accessory kit.
  The asset list, the character art brief, the UI art rules and the UI art contract are updated as the specs' §14 and §7.3 list. Those doc edits can go out earlier as a docs-only commit if the art team needs them.
- **Files:** `Assets/Art/UI/Desktop/`, `Assets/Art/UI/Forms/`, `Assets/Art/Characters/`; `Editor/CharacterArtImporter.cs` (R6-017); the builder's wiring; `docs/ART_ASSET_LIST.md`, `docs/CHARACTER_ART_BRIEF.md`, `docs/UI_ART_CONTRACT.md`, `docs/superpowers/drafts/art/UI_ART_RULES.md`.
- **Tests first:** `LookKeysTests` (the kit's keys), `PaletteTests` (the tints).
- **Unity:** tinting per theme; the readability audit; the characters on the desk at both sizes.
- **Golden effect:** `scene_OfficeGameplay`; `data_hashes`.
- **FEATURES:** 97 (character art), 145.
- **Audit absorbed:** R6-017.
- **Depends on:** 17, 5 and 10, and the art deliveries.

### Future phases (not scheduled)

- **The transponder desk device** (T F3; Saleh's Q2: "a physical desk device later"). A handheld unit the traveller sets on the desk, with a readout of model, serial and class that mirrors the manifest. It needs its art first (T §7.3).
- **Ancestry cards as evidence** (P §12): one `EvidencePicks` method, if the game asks for it.

## 3. The audit backlog against the phases

The backlog is `SCRATCH/audit/backlog.json` (164 items, confirmed and triaged by the Phase 1 audit). Each phase above takes what it naturally touches. Summary:

| Phase | Items |
|---|---|
| 0 (in flight) | R3-001, R5-001, R5-002, R5-003, R2-001, R6-001, R6-014 |
| 1 | R4-024, R4-023, R2-022, R2-019, R1-013, R4-021 (in part) |
| 2 | R4-002, R4-009, R1-007, R1-009, R1-022, R5-004, R5-023 |
| 3 | R1-003, R1-002 (in part), R3-020, R3-012, R3-035, R3-008, R6-021 |
| 4 | R1-001, R6-020, R2-026, R2-005, R6-005, R2-020 (in part), R6-015 |
| 5 | R4-006, R4-007 (the document window) |
| 6 | R3-025, R4-018, R1-017, R1-002 (pins) |
| 7 | R3-017, R3-019, R3-011, R1-010, R1-004, R1-014, R3-024 (the verdict part) |
| 8 | R1-019 |
| 9 | R3-010, R6-002, R6-006 (in part) |
| 10 | R1-006, R1-011, R1-012 |
| 11 | R1-020, R3-015 |
| 12 | R3-037, R6-022 |
| 13 | R2-007, R2-011, R3-016, R3-018, R3-023, R2-010, R3-021, R3-005, R3-031, R6-016 |
| 14 | R4-015, R4-016, R4-017, R4-026, R5-005 (in part) |
| 15 | **R4-001** (the `InvestigationUIController` god class), R4-003, R4-004, R4-022, R4-025, R3-036 |
| 16 | R4-005, **R4-007** (the duplicated window paging code), R6-004, R6-008, R6-012, R6-009, R4-021 (in part) |
| 17 | R4-020, R4-015, R6-023, R4-027 |
| 18 | R4-026 |
| 19 | R1-007 |
| 20 | R5-005 |
| 22 | R2-006, R4-013, R5-010, R5-015, R2-013 |
| 23 | R2-010, R3-021, R3-032 |
| 24 | R3-007 |
| 25 | R2-025, R4-011 |
| 26 | R6-003, R6-006, R6-007, R6-013 (R6-014 if not in phase 0) |
| 27 | R6-017 |

**Left to the overhaul's own slices** (nothing in this plan touches them):
- the asmdef split and the untestable Assembly-CSharp test gaps: R2-021, R3-033, R5-007, R6-007 (its general part);
- R4-019 (`UiText` as a service locator), R3-024's remaining responsibilities, R3-026 and R3-027 (singleton reach-ins, effects by asset name), R3-009 and R3-039 (the content library and RunConfig loaded twice);
- the office binder and camera items: R5-006, R5-008, R5-009, R5-011 to R5-014, R5-016 to R5-022;
- the remaining low items: R1-005, R1-008, R1-015, R1-016, R1-018, R1-021, R1-023, R2-002 to R2-004, R2-008, R2-009, R2-012, R2-014 to R2-018, R2-023, R2-024, R2-027, R2-028, R3-002 to R3-004, R3-006, R3-013, R3-014, R3-022, R3-028 to R3-030, R3-038, R3-040, R4-008, R4-010, R4-012, R4-014, R6-010, R6-011, R6-018, R6-019, R6-024.

A phase that meets one of these in a file it edits may take it, with its regression test, and says so in its commit.

## 4. Why this order

- **Translation first (phase 1).** "all documents must be filled in english" removes the paper flip, so the forms engine never has to draw one.
- **Records, kinds and the displaced's forms before the engine (2-3).** The engine then lays out real TC forms, never throwaway passport and permit forms, and each later phase authors the form of the template it adds.
- **The traveller domain in content order (6-13).** It follows T's day ramp, so every phase ships a playable game, and the golden masters change one explained step at a time.
- **The PC after the domain (14-22).** The Records view, the steps and the analysis marks then show real accounts, checklists and paper contradictions, not interim data. The façade (15) is a pure refactor whose gate is byte-identity.
- **Balance before the late surfaces (23).** Once every rule that moves money and history exists, the late phases (24-27) add surfaces, not rules.
