# Traveller personalities: the plan

> **For agentic workers:** one phase is one agent session on its own branch. Start a phase by writing its detailed task list (superpowers:writing-plans) from its entry below and the spec sections it names, then execute it (superpowers:executing-plans or superpowers:subagent-driven-development).
> - Spec: `docs/superpowers/specs/2026-09-29-traveller-personalities-design.md` (**S** below: S W3 is its decision W3, S §4.3 its section 4.3). Saleh answered all ten of its questions on 2026-09-29 (S "Saleh's answers", S §12); one new question, S §12 Q11 (the Reference's claimed row), is open, and phase V0 builds its recorded option A until he answers.
> - Also read: `docs/superpowers/specs/2026-09-26-traveller-types-design.md` (**T**), the PC redesign spec `docs/superpowers/specs/2026-09-26-pc-redesign-design.md` (**P**: AP1-AP2, the case header), the redesign plan `docs/superpowers/plans/2026-09-26-redesign-plan.md` (its §0 "How every phase runs" applies to every phase here, unchanged), `docs/FEATURES.md`, `docs/ENGINEERING_MANIFESTO.md`, `docs/CONTENT_SHEETS.md`, `docs/reviews/MERGE_CRITERIA.md`.
> - House rules: `SCRATCH/HOUSE_RULES.md` and `SCRATCH/wave1/COMMON_BRIEF.md` ("Saleh's design rules (2026-09-29)" at its end override any spec text that disagrees). `SCRATCH` = `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`.
> - The code wins over this text: each phase re-reads the files it names before it edits them. This plan was written against `main` `d8d46a4`.

**Goal:** nothing prints the claim or names the traveller's kind before the papers do; every traveller of a day is asked through one identical wheel; every generated traveller is a person with a personality that speaks through every reply (the claim, the hand-over, a refusal, a spoken request, an answer, small talk, a rare liar's slip and the verdict reaction), every premade in their own authored lines; all lines are rows Saleh authors and balances in the content spreadsheet; no personality is a tell and no existing draw moves.

**Architecture:** the rules are pure `TimeDesk.Domain` and tested first: `ContextMatch` (a row's kinds and era scored against a traveller), `Personalities` (the cast and its one draw), `Voices` (the resolver: match, tier, fall back, the small-talk sources, pick by value, fill), `Slips` (who slips), `ReactionIntents`, `VoiceChecks` (the content rules Generate World and the validator share), the one-wheel rules in `InterviewDay`, `InterviewQuestion` and `FormRequests`, and the steps' set choice in `CaseSteps`. `Seeds` gains the personality and slip streams and `OfKey`. Assembly-CSharp is glue: the builder drops the printed claim; `CaseFactory` draws the personality and the slip and resolves the small talk; `InterviewPresenter` passes the voice into `InterviewScript`; `GameManager` hands the reaction to the presenter; `TravellerView` lingers for `DeskConfigSO.reactionSeconds`. Content lives in `world_source.json` (`personalities`, `days[].slipChance`, `interview.smallTalkWeights`, `interview.kindSmallTalk`, `interview.slips`, `interview.reactions`, `interview.voices.*`) through Generate World, with `ContentSheetMap` entries and the template rewritten.

**Order:** the printed claim goes first (Saleh's Q5 asks for it before anything speaks), then the one wheel, then the personalities and their resolver, the reaction, the slip, and last the cast's and the premades' full lines, the tone pass and the golden re-pack.

---

## 0. How every phase runs

Everything in the redesign plan's §0 applies. In short, and what this plan adds:

- **Branch and worktree:** `design/personalities-vN-<name>` (or the name the orchestrator gives) from the current `main`, in the worktree and editor the orchestrator names. Never edit `E:\unity\NOPE`, never edit `Assets/Scenes/OfficeScene.unity`, never push, rebase or amend, never commit `_TimeDesk*` files or the `.sln`.
- **Tests first:** each task below names its failing tests. Write them, see them fail (a compile error counts), implement, see them pass. Assembly-CSharp is not unit-testable (the test assembly sees Domain and Visuals only): a glue task has no failing-test step, its rules are the Domain calls tested before it, and Unity checks it.
- **Offline gates after every change:** `python SCRATCH/compile_check.py '<worktree>'` (0 errors in every project; a track may use its own checker as the brief says), then `SCRATCH/runner/bin/Debug/net10.0/runner.exe '<the checker's cc folder>\Temp\Bin\Debug'` (`passed N, failed 0`, N at least the phase's starting count plus its new tests).
- **Content:** `Assets/Data/World/world_source.json` (2-space indent, LF; edit it by script, keeping its formatting) through Generate World; generated assets are never hand-edited. Every JSON field added or removed gets its `ContentSheetMap` entry in the same commit (`ContentSheetMapTests` fails otherwise); then Tools > TimeDesk > Write Content Spreadsheet Template, and the template is committed.
- **Serialized enums are append-only** and pinned in `SerializedEnumsTests`. This plan adds two new enums (`ReactionVerdict`, `ReactionIntent`) and appends to none.
- **Unity** (only through the job runner of the named editor; never launch, quit or click a dialog): after each phase, the player scripts compile, the EditMode suite passes except the known third-party `UnitySkills…SceneSummarize_CountsObjectsCorrectly`, Build Office UI rebuilds `OfficeGameplay.unity` identically by semantic dump when the builder did not change (phase V0 changes it: commit the rebuilt scene and explain the dump's diff), Generate World runs whenever the JSON changed (then commit `Assets/Data`), the validator is clean, and a **smoke play** in the art office (the anime hall, `OfficeScene` with `OfficeGameplay` as the game loads them): Title → New Run, seed 12345 → day 1, one traveller decided, no errors in the log. Then the phase's **feature probes** below, with screenshots at 1920 × 1080 and 1280 × 720 of every new UI state, into `SCRATCH/<tag>/`. The full six-day play-through and the golden re-pack are phase V5's (the end of this epic). Afterwards revert what Unity touched outside the phase (`*.csproj`, `ProjectSettings/*`, the LiberationSans fallback asset) and delete the `_TimeDesk*` files and their metas; queue `__EMPTY_SCENE__` before any git operation that changes a scene file the editor has open.
- **Golden masters:** each phase names its golden effect; everything else in `docs/reviews/audit-baseline/golden` must stay byte-identical when diffed (`tools/audit/golden.py diff`). Phases V0-V4 diff and explain; V5 re-packs once.
- **Docs in the commit of the behaviour:** `docs/FEATURES.md` (S §10 lists the lines), `docs/CONTENT_SHEETS.md` when sheets change; "(tested: X)" only when test X exists and covers it.
- **Commits:** small, conventional, each compiling; every message ends with a blank line and the attribution line the orchestrator names (today `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`).
- **Before reporting:** merge the latest `main`, re-run compile, offline and the smoke play, then report in the brief's shape (`PHASE Vn READY <hash>`, the numbers, the files carrying decisions made on Saleh's behalf, the open questions, the screenshots' folder).

## 1. The phases at a glance

| # | Phase | Scope | Size | Depends on |
|---|---|---|---|---|
| V0 | No printed claim | The case header's claim and the office claim tag removed (`claim.banner`, `CaseInstance.claimLine`, `CaseVerdict.claimSummary` with them); the counters fill the header, the compare strip moves up; the steps checklist on the `default` set until the arrival paper is read | M | main `d8d46a4` |
| V1 | One wheel for every traveller | One ask entry; one question per category asked of everyone in the same words, answers by kind and era; one papers menu of the forms met so far; the new default refusals | M | V0 |
| V2 | Personalities and the voice resolver | The cast and its stream; `Voices`, `VoiceChecks`; the claim, hand-over, refusals, spoken requests, answers and small talk (three weighted sources) in each voice; premade rows; a citizen's home is 2150; the debug force; the spec's first lines | L | V1 |
| V3 | The verdict reaction | `ReactionVerdict`, `ReactionIntent`; one or two lines by verdict and intent; the defaults and the cast's four base reactions (required); the figure lingers `reactionSeconds` | M | V2 |
| V4 | The slip | `Seeds.ForSlip`, `Slips`, `days[].slipChance`; the slip after small talk; the default and the cast's slip lines; the fact guard refuses a slip that names a fact | M | V3 |
| V5 | The cast's and the premades' lines, the tone pass, the golden re-pack | The cast's core completed, then answers, spoken requests, refusals, reason reactions; every premade's sixteen or seventeen lines; the tone checklist; the six-day play-through; the golden masters re-packed | M | V4 |

Phase 9 of the redesign plan (labourers) is not on `main` at `d8d46a4`. It touches the same seams (`askableBy` on TC-520, the `Missing` replies, `InterviewCase.missingVariant`, the labourer's claim and step set): whichever of phase 9 and V1 lands second merges the other (S §14).

## 2. The phases

### Phase V0 — No printed claim (M)

- **Specs:** S B1-B5, §3.5 (every place the claim is shown today, and what replaces it), §8 (V0's rows); P AP1-AP2 (the case header), piece 10's X12 (the office case HUD).
- **Scope:**
  - The Investigation app's case header: `InvestigationApp.claimText` and the builder's `ClaimText` removed; `InvestigationApp.BeginCase` takes the traveller's name only (the window title); `countersText` fills the header's height and shows `idle.waiting` between travellers (`EndCase`); Accept and Deny unchanged.
  - The office case HUD: `OfficeCaseHud.SetClaim`, `claimRoot`, `claimText` and the builder's `ClaimStrip` removed; the compare strip moves into the claim strip's place (`ClaimStripTop`); the desk view's "▲ Back" control keeps clear of it (checked on the screenshots); `BoothRules.CaseHudVisible` unchanged.
  - `InvestigationUIController.ShowRich` no longer formats a claim; the `claim.banner` string goes from `ui.strings`.
  - `CaseInstance.claimLine` removed (its only readers were the header, the tag and `ShiftScoring`); `CaseVerdict.claimSummary` removed. `CaseFactory`'s claim-line warning stays (the spoken default). The transcript's `case.claim` line is unchanged: until V2 it is the kind's sentence, as today.
  - The steps checklist (B5): `CaseSteps` names the set, the `default` set until the paper handed over on arrival is read (`CaseProgress.Read` of a paper not asked for: lifted at the desk or its scanned copy seen), then the kind's, progress kept by step id; `StepsPanel` re-resolves when that happens.
  - The Reference's claimed row, "Claimed place only", the smart links and the window title stay (B4, S §12 Q11 option A).
- **Files:**
  - Domain: `CaseSteps.cs`, `CaseProgress.cs` (whether an arrival paper was read), `ShiftLedger.cs` (`claimSummary` removed).
  - Scripts: `UI/Investigation/App/InvestigationApp.cs`, `UI/OfficeCaseHud.cs`, `UI/InvestigationUIController.cs`, `UI/Investigation/App/StepsPanel.cs`, `CaseInstance.cs`, `CaseFactory.cs`, `Shift/ShiftScoring.cs`.
  - Editor: `OfficeSceneUIBuilder.App.cs` (`BuildAppHeader`), `OfficeSceneUIBuilder.Desk.cs` (`BuildOfficeCaseHud`, the strips' constants, the Back control's place); `Assets/Scenes/OfficeGameplay.unity` rebuilt by Build Office UI.
  - Content: `world_source.json` (`claim.banner` removed from `ui.strings`); the generated string tables (Generate World).
  - Tests: `CaseStepsTests.cs`, `UiStringsTests.cs` (if it lists the key).
  - Docs: `docs/FEATURES.md` (38, 46, 48, 77, 176), a pointer in P AP1 and T §8's claim table to S B1-B3; the audit jobs: `_TimeDeskAuditStatic.cs.txt` prints the spoken claim (`Interview.Claim(kind, place)`, then in V2 the voice's) under `claimLine=`, `_TimeDeskAuditPlay.cs.txt` no longer reads the claim tag or `claimSummary`.
- **Tasks:**
  1. **The steps name no kind before the papers.** Tests first, `CaseStepsTests`: `SetName_IsTheDefaultUntilAnArrivalPaperIsRead`, `SetName_IsTheKindsOnceItIsRead`, `SetName_AReadRequestedPaperDoesNotSwitch`, `Resolve_ProgressCarriesOverBySetId` (a step done under `default`, e.g. "rules", stays done in the kind's set). Then `CaseSteps.SetName(kind, arrivalRead)`, `CaseProgress.ArrivalRead`. Gates; commit.
  2. **Glue for the steps.** `StepsPanel.BeginCase` starts on the default set and re-resolves on the first arrival read; `InvestigationUIController` passes which papers were handed over on arrival (it already builds `StepPaper`s). Offline gates; commit.
  3. **The claim goes.** `InvestigationApp` (no `claimText`; `BeginCase(travellerName)`; the idle line in the counters), `OfficeCaseHud` (no claim), `InvestigationUIController.ShowRich`, `CaseInstance.claimLine`, `CaseVerdict.claimSummary`, `ShiftScoring`; the `claim.banner` row removed from the JSON (`ContentSheetMapTests` green: `uiStrings` is a list of rows, no map change). Offline gates; commit.
  4. **The builder.** `BuildAppHeader` without `ClaimText` (the counters take the band); `BuildOfficeCaseHud` without `ClaimStrip` (the compare strip at the top); the theme role `ClaimStrip` stays on the header's band. Offline gates; commit; Build Office UI in Unity, then commit `OfficeGameplay.unity` with the dump's diff explained.
  5. **Docs.** FEATURES 38, 46, 48, 77, 176; the P and T pointers; the audit jobs. Commit.
- **Unity:** Generate World (the string tables; the validator clean); Build Office UI and the rebuilt scene's semantic dump: exactly the removed `ClaimText` and `ClaimStrip` and the moved counters and compare strip; the smoke play; feature probes:
  - day 1, a traveller at the desk: the office view with no claim tag (screenshot at both sizes), the compare strip at the top after a pick, the "▲ Back" control clear of it in the desk view;
  - the PC: the case header with the counters and Accept and Deny, no claim (screenshot at both sizes, maximised and restored); between travellers, "Waiting for the next traveller" in the header;
  - the claim said in the bubble on arrival and on the Transcript tab; a search for the destination finds the transcript line;
  - the steps checklist: the `default` steps on arrival, the rich tourist's set once the visa is lifted (screenshot before and after), and the same for a poor tourist (the proof of means steps appear only after the visa is read); a scanned copy seen on the PC switches the set too;
  - decide a traveller: no error from the removed fields.
- **Golden effect:** `scene_OfficeGameplay` (the removed objects, the moved rects); `play_transcript` (the verdict dumps lose `claimSummary`); `data_hashes` and `world_generate` (the string tables); `cases.txt` unchanged (the key prints the same sentence).
- **FEATURES:** 38, 46, 48, 77, 176.
- **Split seam:** tasks 1-2 (the steps) and 3-4 (the claim) are independent.
- **Depends on:** `main` `d8d46a4`.

### Phase V1 — One wheel for every traveller (M)

- **Specs:** S W1-W6, T8, §3.1-3.4, §8 (V1's rows), §9 (the questions' and interview's sheet changes), §11 (the removals).
- **Scope:**
  - One ask entry: `interview.askLabel` = "Ask about the trip >"; `interview.tripAskLabel` and `Interview.AskLabel(lines, kind)` removed.
  - One question per category for every kind: `InterviewQuestion.kinds` / `AsksOf` removed; `WordingOverride` loses `prompt`, gains `kinds`; `InterviewQuestion.AnswerFor(eraId, kind)` by the most specific override (kinds 2, era 1, summed; ties keep the first listed), scored by a small Domain matcher, `ContextMatch` (a row's kinds and era against a kind and an era), which phase V2's `Voices` reuses (S V3: one matcher); the prompt is the question's own. `InterviewDay`'s per-kind lists removed; its day lists serve everyone. `InterviewQuestions.Problems` becomes one question per category, `MostForOneKind` becomes the day's count.
  - One papers menu: `DocumentTemplateSO.askableBy`, `AskableForm.AskableBy` / `IsAskableBy`, `FormRequests.For(kind, …)` removed; a Domain rule lists the on-request forms of the day plans up to today in first-appearance order, a group once (`FormRequests.MetSoFar`); `TimelineService.AgencyForms` feeds it the plans in day order and today's day; `FormRequests.ReplyProblems` runs over each day's menu and the kinds in play that day.
  - Content: `q_trip_currency` and `q_trip_device` merged into `q_currency` and `q_device` (S §3.2: prompts, default answers, displaced overrides, days 4 and 5, announcements); the new default refusals (S §3.3).
- **Files:**
  - Domain: new `ContextMatch.cs`, `InterviewContent.cs`, `Interview.cs`, `InterviewDay.cs`, `InterviewScript.cs` (the ask entry, `PromptLine`, `DialogChecks.MenuProblems`' callers), `FormRequests.cs`, `TravellerKind.cs` (its doc names `askableBy`).
  - Scripts: `DocumentTemplateSO.cs`, `Dialog/QuestionSO.cs` (doc), `CaseFactory.cs` (the day's lists), `Timeline/TimelineService.cs`, `UI/Investigation/InterviewPresenter.cs`.
  - Editor: `WorldContentGenerator.cs`, `WorldContentGenerator.Kinds.cs`, `ContentLibraryValidator.cs`, `ContentLibraryValidator.Kinds.cs`.
  - Content: `world_source.json` (questions, `interview.askLabel`, `tripAskLabel` gone, the new refusals); `Assets/Data/Investigation/DocTemplate_TC101/230/310/415/416/417/610/620/630.asset` (their `askableBy` lines stripped by an EOL-preserving script); `Domain/ContentSheets/ContentSheetMap.cs`; `ContentSheets/TimeDesk_Content_Template.xlsx`.
  - Tests: new `ContextMatchTests.cs`, `InterviewTests.cs`, `InterviewDayTests.cs`, `InterviewScriptTests.cs`, `FormRequestsTests.cs`, `SerializedEnumsTests.cs` (the `askableBy` pin goes).
  - Docs: `docs/FEATURES.md` (35, 78, 79, 80, 176), `docs/CONTENT_SHEETS.md`, a pointer under T I1 and I2 to S W3 and W4; the audit jobs `_TimeDeskAuditStatic.cs.txt` (the WORLD askable line reads the day's lists) and `_TimeDeskAuditPlay.cs.txt` (check its papers loop handles the one menu; update it if it assumes per-kind requests).
- **Tasks:**
  1. **The answer by kind and era.** Tests first: new `ContextMatchTests` (`Score_BlankMatchesAnything`, `Score_NamedKindsTwoNamedEraOne`, `Score_AnotherKindOrEraNeverMatches`); `InterviewTests`: `AnswerFor_KindsOverrideBeatsEraOverride`, `AnswerFor_KindsAndEraBeatKindsAlone`, `AnswerFor_ATieKeepsTheFirstListed`, `AnswerFor_AnOverrideOfAnotherKindNeverApplies`, `AnswerFor_NoOverrideGivesTheDefault`, `Prompt_IsTheQuestionsOwnForEveryKindAndEra`; `AskLabel_*` tests removed. Then `ContextMatch` (new, Domain), `WordingOverride.kinds`, `prompt` removed, `AnswerFor(eraId, kind)`, `PromptFor` removed (callers read `prompt`). Gates; commit.
  2. **The day's questions for everyone.** Tests first, `InterviewDayTests`: `Questions_AreTheDaysForEveryKind`, `AskableCategories_OnePerCategoryInLibraryOrder`, `AnswerTellCategories_AreTheDayGatedOnes`, `Problems_OneQuestionPerCategory` ("Question 'x' asks about Currency, as 'y' does"), `Count_IsTheDaysQuestions`. Then `InterviewQuestion.kinds` / `AsksOf`, `QuestionsFor`, `AskableCategoriesFor`, `AnswerTellCategoriesFor`, `MostForOneKind` removed; `CaseFactory` and `InterviewPresenter` read `Questions`, `AskableCategories`, `AnswerTellCategories`. Gates; commit.
  3. **The papers menu of the days so far.** Tests first, `FormRequestsTests`: `MetSoFar_ListsEveryOnRequestFormOfTheDaysUpToToday`, `MetSoFar_NeverListsALaterDaysForm`, `MetSoFar_KeepsAnEarlierDaysFormOnADayWithASmallerMix`, `MetSoFar_OneEntryPerGroup`, `MetSoFar_SkipsFormsHandedOverOnArrival`, `ReplyProblems_EveryKindInPlayNeedsAnHonestLineForEachRequestItNeverCarries`. Then `FormRequests.MetSoFar`, `askableBy` and its members removed, `ReplyProblems` over (the day's menu, the day's kinds, each kind's carried forms), `TimelineService.AgencyForms(lib, day)`, `InterviewDay.AskableForms` (no kind). Gates; commit.
  4. **The same wheel.** Tests first, `InterviewScriptTests`: `Build_EveryKindGetsTheSameHubPapersAndAskMenus` (one day, four kinds with their own papers: identical choice ids, labels and kinds in the hub, the papers menu and the ask menu), `Build_TheAskEntryIsTheAskLabelForEveryKind`, `Build_ADisplacedTravellerAskedForAManifestSaysTheirHonestLine`, `Build_ACitizenAskedForAReturnOrderSaysTheirHonestLine`, `MenuProblems_FiveRequestsFitThePapersMenu`. Then the ask entry reads `askLabel`. Gates; commit.
  5. **Content.** The questions of S §3.2 (order kept: currency, language, device, capital, ruler, date of birth; `q_trip_*` removed; the days' announcements); `askLabel`; `tripAskLabel` removed; S §3.3's refusals; the nine templates stripped; `ContentSheetMap` (`questions.kinds` removed; `questionOverrides`: `era` omitted when blank, `kinds`, `answer`; `interview.tripAskLabel` removed); the generator and the validator call the Domain rules (one question per category; the replies per day; the menu's capacity with the most requests any day offers). `ContentSheetMapTests` green. Commit the source and map; Generate World and the template in Unity (below), then commit `Assets/Data` and the template.
  6. **Docs.** FEATURES 35, 78, 79, 80, 176; CONTENT_SHEETS.md; the T pointers; the audit jobs. Commit.
- **Unity:** Generate World (0 errors; the validator clean); Write Content Spreadsheet Template; the smoke play; feature probes:
  - day 1: a rich and a poor tourist in turn: read the wheel's hub, papers menu and ask menu from the ring's choices (identical ids, labels and order; screenshot the papers menu at both sizes); ask the rich tourist for the Stranding Waiver (the refusal in the bubble);
  - Skip Day to day 4: the trip questions for every traveller, the announcement in the morning paper;
  - Skip Day to day 5: a displaced traveller's papers menu (five entries; screenshot), asked for a Departure Manifest (their Honest line), the ask menu (the same six entries as a tourist's, the trip prompts); a tourist asked for a Return Order (their line); the menus' capacity (no overflow at 720p).
- **Golden effect:** `cases.txt`: the WORLD `askable` / `answerTells` lines (the day's lists), a citizen's `answers=` on days 5-6 (+ Language, Geography, Politics); every tell, paper, name, record and look byte-identical (T8). The play transcript: the day-4 and day-5 announcements, the displaced's prompts, the menus of days 5-6 and their refusals. `world_generate`, `data_hashes` (question assets, the nine templates), `validator`.
- **FEATURES:** 35, 78, 79, 80, 176.
- **Split seam:** tasks 1-2 (the questions) can land before 3-4 (the papers menu).
- **Depends on:** V0; the phase 9 seam (§1).

### Phase V2 — Personalities and the voice resolver (L)

- **Specs:** S PS1-PS5, V1-V5, V7-V9, T1-T6, §2, §4.1-4.4, §5, §7.3 (the first rows), §9.
- **Scope:**
  - `Seeds.PersonalitySalt` ("PRSN", `0x5052534E`), `Seeds.ForPersonality(caseSeed)`, `Seeds.OfKey(string)` (a fold of `Seeds.Mix` over the characters).
  - Domain `Personality` (id, name, weight, note), `Personalities.Pick(cast, rng)` (one `WeightedRandom.Pick`; none, with no draw, for an empty cast) and `Personalities.Problems`.
  - Domain `VoiceLine` (personality, premade, kinds, era, key, variant, lie, line; V3 adds the verdict, intent and `then`), `VoiceBook` (one list per slot: claims, handOver, missingForms, spoken, answers, smallTalk; V3 adds reactions, V4 slips), `VoiceContext` (kind, claimed era: nothing else), `VoiceKeys` (the slot keys), `Voices` (a premade's rows first, then match, tier, fall back, the small-talk sources, pick by value, per slot; scored by V1's `ContextMatch`), `VoiceChecks.Problems` (the voice column, tokens per slot, the worst case, references, the fact guard, duplicates, coverage).
  - `CaseInstance.personality` and `dialogSeed`; `CaseFactory`: the personality drawn on its own stream for a generated traveller (a premade draws nothing and speaks its own rows; `DevToolsState.ForcedPersonality` overrides after the draw), the small talk through `Voices` from the three sources (the personality's tier; the home's lines: the displaced's claimed place, else its era, a citizen's present place, else the Future era; the kind's lines) by `interview.smallTalkWeights` (a premade's own small talk only); `_dialogRng` and `Interview.PickSmallTalk` removed.
  - `InterviewCase.voice`; `InterviewScript.Opening` says the voice's claim, `Build` resolves the hand-over, refusals, spoken requests and answers through `Voices`.
  - `ContentLibrarySO` holds the cast; `InterviewLines.voices`, `kindSmallTalk`, `smallTalkWeights`; the debug panel shows the traveller's personality and sets `ForcedPersonality`.
  - Content: `personalities` (the seven, weight 1, their notes), `interview.smallTalkWeights` (1, 1, 1), `interview.kindSmallTalk` (S §7.3's four), `interview.voices` with S §7.3's lines for these slots; `ContentSheetMap` (`personalities`, `kindSmallTalk`, the six voice sheets with `personality` and `premade`, the weights' columns).
- **Files:**
  - Domain: `Seeds.cs`, new `Personalities.cs`, new `Voices.cs`, new `VoiceChecks.cs`, `Interview.cs`, `InterviewContent.cs`, `InterviewScript.cs`, `ContentSheets/ContentSheetMap.cs`.
  - Scripts: `CaseInstance.cs`, `CaseFactory.cs`, `ContentLibrarySO.cs`, `UI/Investigation/InterviewPresenter.cs`, `DevTools/DevToolsState.cs`, `DevTools/DebugPanelController.cs`.
  - Editor: `WorldContentGenerator.cs` (the cast, the voices, the kinds' small talk, the weights, line ids `interview.voices.{list}.{personality or premade}.{n}`, `interview.kindSmallTalk.{n}`; `VoiceChecks` and `Personalities.Problems` before writing), `ContentLibraryValidator.cs` (the same rules on the assets; the coverage info line).
  - Content: `world_source.json`; the template.
  - Tests: `SeedsTests.cs`, new `PersonalitiesTests.cs`, new `VoicesTests.cs`, new `VoiceChecksTests.cs`, `InterviewTests.cs`, `InterviewScriptTests.cs`.
  - Docs: FEATURES 11, 71, 73, 77, 78, 80, 176; CONTENT_SHEETS.md; the audit static job prints `personality=` after `gender=` and the voice's claim under `claimLine=`.
- **Tasks:**
  1. **The stream and the key.** Tests first, `SeedsTests`: `PersonalitySalt_IsPinned`, the salt in the one distinctness table, `ForPersonality_IsPinnedForSampleSeeds`, `OfKey_IsPinned` (for "claim", "smalltalk", "answer:q_currency"), `OfKey_DiffersPerKey`, `OfKey_OfEmptyIsItsStart`. Then `Seeds`. Gates; commit.
  2. **The cast.** Tests first, `PersonalitiesTests` (with `ScriptedRandom`): `Pick_IsOneDraw`, `Pick_FollowsTheWeights`, `Pick_NeverAZeroWeight`, `Pick_OfAnEmptyCastIsNoneWithNoDraw`, `Problems_BlankOrDuplicateId`, `Problems_NegativeWeight`, `Problems_NoPositiveWeight`, `Problems_BlankName`. Then `Personality`, `Personalities`. Gates; commit.
  3. **The resolver.** Tests first, `VoicesTests`: `Line_APremadesOwnRowComesFirst`, `Line_APersonalityRowBeatsTheDefault`, `Line_NamedKindsBeatBlankKinds`, `Line_ANamedEraBeatsABlankEra`, `Line_KindsOutrankEra`, `Line_ARowOfAnotherKindOrEraNeverMatches`, `Line_ARowOfAnotherPersonalityOrPremadeNeverMatches`, `Line_NoVoiceSaysTheDefault`, `Pick_IsAValueOfTheSeedAndTheSlotKey`, `Pick_RowsUnderAnotherKeyNeverMoveIt`, `Pool_IsTheBestTierOnly`, `SmallTalk_PicksASourceByTheWeightsThenALine`, `SmallTalk_SkipsASourceWithNoLine`, `SmallTalk_AZeroWeightSourceIsNeverPicked`, `SmallTalk_APremadeSaysOnlyTheirOwn`, `SmallTalk_ACitizensHomeIsThePresent`, `Answer_FallsBackToTheQuestionsOverrideThenItsAnswer`, `ContextHoldsOnlyWhatTheDeskSees` (reflection: `VoiceContext`'s public fields are exactly the kind and the claimed era), `SlotKeys_ArePinned`. Then `VoiceLine`, `VoiceBook`, `VoiceContext`, `VoiceKeys`, `Voices`. Gates; commit.
  4. **The content rules.** Tests first, `VoiceChecksTests`: `ARowNamesExactlyOneVoice`, `AClaimWithoutPlace`, `AnAnswerWithoutValue`, `AnUnknownToken`, `ATokenOutsideItsSlot` (`{value}` in a claim, `{document}` in small talk), `TooLongWithTheLongestFill`, `AnUnknownPersonalityOrPremade`, `AnUnknownEra`, `AnUnknownQuestion`, `AnUnknownSpokenRequest`, `ARefusalsRequestNoDayOffers`, `BlankText`, `AFactValueWarns`, `ATransponderModelWarns`, `AnEmployerWarns`, `ADuplicateRowWarns`, `TheSmallTalkWeights` (none negative, one positive), `AKindInPlayWithoutKindSmallTalk`, `CoverageListsEachPersonalitysFallbacks`. Then `VoiceChecks`. Gates; commit.
  5. **The interview speaks.** Tests first, `InterviewScriptTests`: `Opening_SaysTheVoicesClaim`, `Opening_ClaimKeyWordsAreTakenOverItsTemplate`, `Opening_NoVoiceSaysTheKindsClaim`, `Build_TheHandOverInTheVoicesWords`, `Build_ARefusalByKindVariantAndVoice`, `Build_ASpokenRequestsReply`, `Build_AnAnswerKeepsItsCanonicalValueAndFact`, `Build_ATellsSentenceIsTheHonestSentence` (two cases alike but for one answer's tell: the same sentence around the value, the same claim, replies and small talk), `Build_NoVoiceSaysTodaysLines` (a fixture's whole transcript equals the one before this phase, small talk aside). Then `InterviewCase.voice`, `Opening`, `Build`. Gates; commit.
  6. **Glue.** `CaseInstance`, `CaseFactory` (the draw; the force; the three sources; the small talk through `Voices`; the log line gains the personality), `ContentLibrarySO`, `InterviewLines`, `InterviewPresenter` (the voice into the case), `DevToolsState` and the debug panel (`ResetAll` clears the force). Offline gates; commit.
  7. **Content and tooling.** The cast (weight 1 each, S §2.1's one-liners as notes), the weights, the kinds' small talk, S §7.3's lines for the claim, hand-over, refusals, spoken requests, answers and small talk; `ContentSheetMap`; the generator and the validator (`VoiceChecks`, `Personalities.Problems`; the coverage info line). `ContentSheetMapTests` green. Commit; Generate World and the template in Unity, then commit `Assets/Data` and the template.
  8. **Docs.** FEATURES 11, 71, 73, 77, 78, 80, 176; CONTENT_SHEETS.md; the audit static job. Commit.
- **Unity:** Generate World (0 errors; the validator clean; the coverage lines listed in the report); the template; the smoke play; feature probes:
  - **each personality speaking** (a job loop: set `DevToolsState.ForcedPersonality` to each of the seven, New Run seed 12345, present day 1's slot 1): the claim in the bubble (screenshot at both sizes, one per personality) and on the Transcript tab, a request for the Stranding Waiver (the refusal), small talk (a 2150, personality or kind line, never the destination's);
  - **the independence probe** (a static job): days 1-6 × 20 run seeds generated twice, the second with every blueprint's liar chance and every day's fault chances at 0 in memory (never saved): every slot's personality identical (report `identical n / n`); the report tabulates the cast among liars and among the honest of the first run (for review, not a gate);
  - day 5: a displaced traveller with a personality line, untranslated (the key words in English) and, with a Speech translator bought on night 4, flipping into English;
  - day 6: Senenmut says the defaults where he has no row yet (his own lines land in V5).
- **Golden effect:** `cases.txt`: + `personality=` on every CASE line; `claimLine=` the voice's claim; `smalltalk=` for every traveller (the value pick; the three sources; the cast's rows); nothing else. The play transcript: the claims, replies, answers and small talk of travellers whose voice has a row.
- **FEATURES:** 11, 71, 73, 77, 78, 80, 176.
- **Split seam:** tasks 1-4 (Domain) as one commit series; then 5-8.
- **Depends on:** V1.

### Phase V3 — The verdict reaction (M)

- **Specs:** S R1-R5, T7, §6, §7.3 (the reactions), §9 (`interviewReactions`, `voiceReactions`).
- **Scope:**
  - Domain enums `ReactionVerdict` (Accepted, Denied) and `ReactionIntent` (Honest, Lying), serialized, pinned; `ReactionIntents.Of(isLiar, isForger)` (a place lie, smuggling included, or a record lie is Lying; anything else Honest).
  - `VoiceLine` gains the verdict, intent and `then`; `VoiceBook.reactions`; `InterviewLines.reactions` (the defaults); `Voices.Reaction` (a premade's rows, then the reason tier, kinds, era; the defaults); `VoiceChecks` requires the four defaults, every personality with a weight above 0 to have its four base reactions (R3), a known reason (`Faults`' reasons and the costume error's `panic`), and checks both lines.
  - `InterviewScript.Reaction(lines, case, verdict, intent, reason)` → one or two traveller `DialogLine`s with their key-word spans.
  - Glue: `GameManager.HandleDecision` resolves the reaction from the case (`IsLiar`, `IsForger`, `FaultReason`) after scoring, exactly where it is today, and hands it to the investigation UI; `InterviewPresenter` appends the lines to the transcript and says them in the bubble in turn; `TravellerView` keeps the figure until `DeskConfigSO.reactionSeconds` after the last line is fully shown (2.5 on `Desk_Default.asset`; 0 clears at once), and showing the next traveller ends the linger; the booth phase, the verdict slip and the clock are untouched.
  - Content: S §4.2's six default reactions, the cast's four base reactions each (S §7.3's, completed, with their `then` lines), S §6's reason rows; `ContentSheetMap` (`interviewReactions`, `voiceReactions`).
- **Files:**
  - Domain: `Voices.cs`, `VoiceChecks.cs`, `InterviewContent.cs`, `InterviewScript.cs`, `Faults.cs` (the list of reasons `VoiceChecks` reads, if it has none), `ContentSheets/ContentSheetMap.cs`.
  - Scripts: `GameManager.cs`, `UI/InvestigationUIController.cs` (the façade's call), `UI/Investigation/InterviewPresenter.cs`, `Characters/TravellerView.cs`, `Office/DeskConfigSO.cs`; `Assets/Data/Config/Desk_Default.asset` (the knob, authored: Generate World never writes it).
  - Editor: `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `ContentLibraryValidator.Desk.cs` (the knob's range).
  - Tests: `SerializedEnumsTests.cs`, `VoicesTests.cs`, `VoiceChecksTests.cs`, `InterviewScriptTests.cs`.
  - Docs: FEATURES 36, 176; CONTENT_SHEETS.md; the audit play job logs the reaction's lines after each verdict.
- **Tasks:**
  1. **The enums and the intent.** Tests first: `SerializedEnumsTests` pins `ReactionVerdict` and `ReactionIntent`; `VoicesTests.Intent_OfAPlaceLieSmugglingOrARecordLieIsLying`, `Intent_OfADirectiveFaultACostumeErrorOrNoFaultIsHonest`. Then the enums and `ReactionIntents`. Gates; commit.
  2. **The reaction's resolution.** Tests first, `VoicesTests`: `Reaction_APremadesOwnPairComesFirst`, `Reaction_ANamedReasonBeatsNamedKinds`, `Reaction_ARowOfAnotherReasonNeverMatches`, `Reaction_FallsBackToTheDefaults`, `Reaction_TheFourDefaultsCoverEveryVerdictAndIntent`, `Reaction_KeepsItsThenLine`, `Reaction_PickIsAValueOfItsKey`. Then `Voices.Reaction`. Gates; commit.
  3. **Its content rules.** Tests first, `VoiceChecksTests`: `Reactions_TheFourDefaultsAreRequired`, `Reactions_EveryWeightedPersonalityHasItsFour`, `Reaction_AnUnknownReason`, `Reaction_TokensArePlaceOnlyInBothLines`, `Reaction_ThenTooLong`. Then `VoiceChecks`. Gates; commit.
  4. **The lines.** Tests first, `InterviewScriptTests`: `Reaction_IsOneOrTwoTravellerLinesWithKeyWordSpans`, `Reaction_NoVoiceSaysTheDefault`. Then `InterviewScript.Reaction`. Gates; commit.
  5. **Glue.** `GameManager`, the façade, `InterviewPresenter`, `TravellerView`, `DeskConfigSO`, `Desk_Default.asset`, the validator's knob check. Offline gates; commit.
  6. **Content, docs.** The defaults and the cast's reactions; the map; the template; FEATURES 36 and 176; CONTENT_SHEETS.md; the audit play job. Commit; Generate World and the template in Unity, then commit `Assets/Data` and the template.
- **Unity:** Generate World; the template; the smoke play; feature probes on day 1 (seed 12345, `ForcedPersonality` set per probe):
  - accept an honest tourist: the reaction's two lines in the bubble in turn and on the transcript, the figure staying about 2.5 s after the second, then leaving (screenshot mid-linger at both sizes);
  - deny an honest tourist: the WRONG verdict slip and the indignant lines together;
  - deny a liar with a logged deviation: CORRECT and the caught lines; accept a liar: the citation and the got-away lines;
  - call the next traveller during a linger: it ends at once, no error, the next traveller's claim follows;
  - decide on the PC: the lines on the Transcript tab, found by search;
  - `reactionSeconds` 0 on a copy of the config in memory: the traveller leaves at once, as before;
  - a decision at closing time ends the day exactly as before (the clock untouched).
- **Golden effect:** the play transcript: one or two reaction lines after each verdict; `data_hashes` (`Desk_Default.asset`, the library); `cases.txt` and the saves unchanged.
- **FEATURES:** 36, 176.
- **Depends on:** V2.

### Phase V4 — The slip (M)

- **Specs:** S T2, T4, T9-T11, §5, §7.3 (the slips), §9 (`interviewSlips`, `voiceSlips`, `days.slipChance`).
- **Scope:**
  - `Seeds.SlipSalt` ("SLIP", `0x534C4950`), `Seeds.ForSlip(caseSeed)`.
  - Domain `Slips.Rolls(intent, premade)` (only a generated traveller of Lying intent rolls) and `Slips.Roll(chance, rng)` (one draw); a premade liar slips when their slip row exists (no roll).
  - `DayPlanSO.slipChance` from `days[].slipChance` (first cut 0.15 every day); `VoiceBook.slips`, `InterviewLines.slips` (the defaults by lie kind); `Voices.Slip` (a premade's row, then the personality's tier with the lie kind scoring 4, then the defaults by lie kind).
  - `CaseInstance.slip` (the line, or none), resolved in `CaseFactory` after the lie is planned; `InterviewScript.Build` adds it as a second traveller line to the small-talk choice (never an answer line).
  - `VoiceChecks`: a slip holding a fact value, a transponder model or an employer is refused (T11); a default slip with a blank lie is required; `slipChance` within 0 and 1.
  - Content: the five default slips (S §7.3), two slips per personality (S §7.3's, completed), "Socrates"' slip (S §4.5); `ContentSheetMap` (`interviewSlips`, `voiceSlips`, `days.slipChance`).
- **Files:**
  - Domain: `Seeds.cs`, new `Slips.cs`, `Voices.cs`, `VoiceChecks.cs`, `InterviewContent.cs`, `InterviewScript.cs`, `ContentSheets/ContentSheetMap.cs`.
  - Scripts: `DayPlanSO.cs`, `CaseInstance.cs`, `CaseFactory.cs`, `UI/Investigation/InterviewPresenter.cs` (the slip into the case).
  - Editor: `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`.
  - Tests: `SeedsTests.cs`, new `SlipsTests.cs`, `VoicesTests.cs`, `VoiceChecksTests.cs`, `InterviewScriptTests.cs`.
  - Docs: FEATURES 11, 36, 176; CONTENT_SHEETS.md; the audit static job prints `slip=` (the slip line's id, or `-`).
- **Tasks:**
  1. **The stream.** Tests first, `SeedsTests`: `SlipSalt_IsPinned`, the salt in the distinctness table. Then `Seeds`. Gates; commit.
  2. **Who slips.** Tests first, `SlipsTests`: `Rolls_OnlyLyingIntent`, `Rolls_NeverAPremade`, `Roll_IsOneDraw`, `Roll_FollowsTheChance`, `Roll_AtZeroNeverSlips`, `Premade_SlipsWhenItsLineIsAuthored`. Then `Slips`. Gates; commit.
  3. **Which line.** Tests first, `VoicesTests`: `Slip_ALieKindRowBeatsABlankOne`, `Slip_APremadesOwnRow`, `Slip_FallsBackToTheDefaultsByLieKind`, `Slip_PickIsAValueOfItsKey`. Then `Voices.Slip`. Gates; commit.
  4. **Its content rules.** Tests first, `VoiceChecksTests`: `Slip_AFactValueIsRefused`, `Slip_ATransponderModelOrEmployerIsRefused`, `Slips_ADefaultWithABlankLieIsRequired`, `Slip_AnUnknownLieKind`, `SlipChance_WithinZeroAndOne`. Then `VoiceChecks`. Gates; commit.
  5. **Said after small talk.** Tests first, `InterviewScriptTests`: `Build_ASlipFollowsTheSmallTalkReply`, `Build_ASlipIsNotAnAnswerLine`, `Build_NoSlipLeavesSmallTalkAsBefore`. Then `InterviewCase.slip`, `Build`. Gates; commit.
  6. **Glue.** `DayPlanSO`, `CaseInstance`, `CaseFactory` (the roll after the lie; the slip line; the log line gains it), `InterviewPresenter`. Offline gates; commit.
  7. **Content, docs.** The defaults, the cast's slips, "Socrates"' slip, `slipChance` on every day; the map; the template; FEATURES 11, 36, 176; CONTENT_SHEETS.md; the audit static job. Commit; Generate World and the template in Unity, then commit `Assets/Data` and the template.
- **Unity:** Generate World; the template; the smoke play; feature probes:
  - with `slipChance` 1 on a copy of day 1's plan in memory: a liar's small talk followed by their slip (screenshot at both sizes); the slip on the Transcript tab, not compare-clickable, not loggable; an honest traveller's small talk with no slip;
  - with `slipChance` 0: no slips, and every other line as before;
  - day 6: "Socrates" slips after his small talk;
  - the independence probe of V2 re-run: the personalities identical with slips on or off.
- **Golden effect:** `cases.txt`: + `slip=` on every CASE line; nothing else. The play transcript: the slips after small talk.
- **FEATURES:** 11, 36, 176.
- **Depends on:** V3.

### Phase V5 — The cast's and the premades' lines, the tone pass, the golden re-pack (M)

- **Specs:** S C2, C4, PS3, §4.5, §7 (the whole tone guide and the author's checklist), §8, §12 (Q11, if Saleh has answered it).
- **Scope:**
  - Re-read S §12 Q11: an answer other than A (the recorded default) changes the Reference's claimed row (B4) first, or is reported as a blocker when it needs code beyond this plan.
  - The cast: per personality, the core (S C2: 2 claims, the hand-over, the rich tourist's two refusals, 2 small-talk lines, 2 slips, 4 reactions) completed, then an answer per question (six), the two spoken requests, the refusals of every default pair (S §3.3), two or three reason reactions: about 25-30 lines each.
  - The premades (Saleh's Q8): every one of the ten gets its own line for every slot it can reach, sixteen for an honest premade and seventeen for "Socrates" (the table below); `VoiceChecks` then refuses a premade with a slot uncovered.
  - Every line through the author's checklist (S §7.4); the fact guard and the length check clean; the coverage report shows no fallback in the core.
  - Where Saleh authors in the spreadsheet instead: Export Content Spreadsheet, he edits, Import Content Spreadsheet (which runs Generate World).
  - The full six-day play-through in the art office; the golden masters re-packed with the account of the diff (S §8).
- **The premades' lines** (each row a `premade` row in the named voice sheet; the day-6 papers menu is Manifest, Waiver, Proof of means, Intake Declaration, Return Order, and every premade carries the Displacement Certificate, the Intake Declaration and the Return Order):

  | Premade | Intent | `voiceClaims` | `voiceHandOver` | `voiceMissingForms` (Honest) | `voiceSpoken` | `voiceAnswers` | `voiceSmallTalk` | `voiceReactions` | `voiceSlips` | Lines |
  |---|---|---|---|---|---|---|---|---|---|---|
  | `senenmut` Senenmut | honest | 1 | 1 | TC-230, TC-310, proof (3) | step_closer, speak_up (2) | q_currency, q_language, q_device, q_capital, q_ruler, q_born (6) | 1 | Accepted · Honest, Denied · Honest (2) | none | 16 |
  | `socrates` "Socrates" | lying (Republican Rome) | 1 | 1 | the same (3) | the same (2) | the same (6) | 1 | Accepted · Lying, Denied · Lying (2) | 1 | 17 |
  | `aspasia` Aspasia | honest | 1 | 1 | 3 | 2 | 6 | 1 | the honest pair (2) | none | 16 |
  | `banzhao` Ban Zhao | honest | 1 | 1 | 3 | 2 | 6 | 1 | 2 | none | 16 |
  | `arib` Arib al-Ma'muniyya | honest | 1 | 1 | 3 | 2 | 6 | 1 | 2 | none | 16 |
  | `khwarizmi` Muhammad al-Khwarizmi | honest | 1 | 1 | 3 | 2 | 6 | 1 | 2 | none | 16 |
  | `gutenberg` Johannes Gutenberg | honest | 1 | 1 | 3 | 2 | 6 | 1 | 2 | none | 16 |
  | `leonardo` Leonardo da Vinci | honest | 1 | 1 | 3 | 2 | 6 | 1 | 2 | none | 16 |
  | `lanyer` Aemilia Lanyer | honest | 1 | 1 | 3 | 2 | 6 | 1 | 2 | none | 16 |
  | `gallerani` Cecilia Gallerani | honest | 1 | 1 | 3 | 2 | 6 | 1 | 2 | none | 16 |

  161 lines in all. Each answer holds `{value}` (the canonical value: Senenmut's `q_ruler` is "{value}. May she live forever, and read my plans.", never the ruler's name written out); each claim holds `{place}`; a reaction may add its `then` line; "Socrates"' lines never name Rome or any value (T11). Their intros, record notes and Senenmut's dialog stay as authored.
- **Files:** `world_source.json`, `Assets/Data` (Generate World), the template; `Domain/VoiceChecks.cs` and `VoiceChecksTests.cs` (the premade coverage becomes an error: `Premades_EverySlotCovered`, `Premades_ASlipOnlyForALiar`); `docs/reviews/audit-baseline/golden` (the re-pack); `docs/FEATURES.md` (73) and any line whose listed example changed.
- **Tasks:**
  1. The cast's lines, by personality (seven commits, `content(voices): <personality>`), each followed by the offline gates (`ContentSheetMapTests`) and Generate World in Unity.
  2. The premades' lines, by premade (ten commits, `content(voices): <premade>`), the same gates.
  3. The premade coverage rule. Tests first, `VoiceChecksTests`: `Premades_EverySlotCovered`, `Premades_ASlipOnlyForALiar`. Then `VoiceChecks` (an error from now on). Gates; commit.
  4. The tone pass: read every line aloud in context (the gallery below); fix what fails the checklist. Commit.
  5. The golden re-pack: the audit jobs (static, play, profile, twice each for determinism), `golden.py diff` against the baseline explained in the commit (S §8's rows: every intended change, nothing else), then `golden.py pack` to `docs/reviews/audit-baseline/golden`. Commit.
- **Unity:** Generate World (0 errors, the validator clean, the coverage lines); the smoke play; the **gallery**: for each personality (the force) and each kind in play (days 1 and 5), the claim, a refusal, an answer, small talk, a slip (`slipChance` 1 in memory) and a reaction, screenshotted at both sizes into `SCRATCH/<tag>/gallery/`; day 6 with each premade forced in turn (the debug panel's force legendary): the claim, a refusal, an answer, small talk and the reaction, "Socrates"' slip; the **full six-day play-through** (seed 12345) with screenshots of each new state; the independence probe of V2 re-run on the full content (`identical n / n`).
- **Golden effect:** the re-pack itself: `cases.txt` (small talk and claims with the full cast), `play_transcript.txt` (every voice's lines), `world_generate`, `data_hashes`, `validator`; saves and profiles unchanged; `scene_OfficeGameplay` as V0 left it.
- **FEATURES:** 73; any line whose listed example changed.
- **Depends on:** V4.

## 3. The golden masters per phase

| File | V0 | V1 | V2 | V3 | V4 | V5 |
|---|---|---|---|---|---|---|
| `scene_OfficeGameplay.txt` | `ClaimText` and `ClaimStrip` removed; the counters and the compare strip moved | none | none | none | none | as V0 left it; re-packed |
| `cases.txt` | none (`claimLine=` prints the same sentence) | WORLD askable/answerTells; citizens' answers days 5-6 | + `personality=`; `claimLine=` the voice's; `smalltalk=` | none | + `slip=` | the full cast and premades; re-packed |
| `play_transcript.txt` | the verdict dumps lose `claimSummary` | announcements, displaced prompts, menus of days 5-6 | the voices' lines where rows exist | + one or two reaction lines per verdict | + slips after small talk | every line; re-packed |
| `play_saves/*.json` | none | none | none | none | none | none |
| `world_generate.txt`, `data_hashes.txt`, `validator.txt` | the string tables | questions, nine templates, validator | the library, validator (+ coverage) | the library, `Desk_Default.asset` | the library, day plans | the library; re-packed |
| profiles | none | none | none | none | none | none |
| must stay byte-identical throughout | every tell, paper, name, record, look, verdict outcome, score, ledger line and save | same | same | same | same | same |

Offline golden values change in the tests each task names (S §8's first table).

## 4. Risks and seams

- **Phase 9 (labourers)** lands before or after V1 (§1, S §14). Its labourer claim default, `Missing` replies and `InterviewCase.missingVariant` need no change here; its step set stays per kind, shown after the arrival paper is read (B5).
- **The builder change (V0)** rebuilds `OfficeGameplay`: its semantic dump must differ by exactly the removed claim objects and the moved strips; anything else moving is a builder bug. The readability audit (contrast at 720p) covers the header and the HUD in the neutral theme and the eight cultures.
- **A tell draw moving** would be a bug: V1's one wheel must leave every `tells=` byte-identical in `cases.txt` (S T8). If it moves, the question order or a gate differs from S §3.2; fix the content, never the planner.
- **The dialog stream's small-talk draw** disappears in V2 (S V4); only `smalltalk=` and the claim may change for it. Any other field moving means a stream was touched. The slip (V4) draws on its own stream, for liars only; any other field moving in V4 is a bug.
- **The linger and closing time** (V3): `_travellerAtDesk` goes false at the stamp as today, so the closing-time rule never waits on a reaction; the probe checks it.
- **Line length at 720p:** a Chatty line at 100 characters is two bubble lines, and a reaction may be two lines; the V2 and V3 screenshots confirm the bubble holds them (`maxLineChars` is a content knob if it does not).
- **The word "slip":** the liar's line is "the slip"; the paper that shows CORRECT or WRONG is always "the verdict slip" in code comments, strings and commits.
- **Parallel tracks appending enums:** V3 adds two new enums and appends to none, so no renumbering can collide.

## 5. Why this order

- V0 first: Saleh asked for the banner's removal outright, it stands alone (no personality needed), and it changes the scene, which is best done before the phases that only touch data and Domain.
- V1 next: it deletes the per-kind question and papers code, so V2's resolver threads one context through one wheel instead of four; rule 3 holds even before a single personality line exists.
- V2 before V3 and V4: the reaction and the slip are voice slots, so they need the cast, the resolver and the checks; the slip needs the lie planned and the small talk resolved, which V2 moves into `Voices`.
- V5 last: the content fills machinery that is already proven, the premades' coverage becomes an error only once their lines exist, and the golden re-pack happens once, at the end of the epic, as the check levels ask.
