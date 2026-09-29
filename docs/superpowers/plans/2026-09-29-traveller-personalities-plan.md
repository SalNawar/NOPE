# Traveller personalities: the plan

> **For agentic workers:** one phase is one agent session on its own branch. Start a phase by writing its detailed task list (superpowers:writing-plans) from its entry below and the spec sections it names, then execute it (superpowers:executing-plans or superpowers:subagent-driven-development).
> - Spec: `docs/superpowers/specs/2026-09-29-traveller-personalities-design.md` (**S** below: S W3 is its decision W3, S §4.3 its section 4.3). Its §12 questions wait for Saleh; an answer that differs from the recorded option changes only the decisions it names, and the phase that builds them re-reads §12 before it starts.
> - Also read: `docs/superpowers/specs/2026-09-26-traveller-types-design.md` (**T**), the redesign plan `docs/superpowers/plans/2026-09-26-redesign-plan.md` (its §0 "How every phase runs" applies to every phase here, unchanged), `docs/FEATURES.md`, `docs/ENGINEERING_MANIFESTO.md`, `docs/CONTENT_SHEETS.md`, `docs/reviews/MERGE_CRITERIA.md`.
> - House rules: `SCRATCH/HOUSE_RULES.md` and `SCRATCH/wave1/COMMON_BRIEF.md` ("Saleh's design rules (2026-09-29)" at its end override any spec text that disagrees). `SCRATCH` = `C:\Users\Saleh\AppData\Local\Temp\claude\E--unity-NOPE\06be6de7-86f0-489b-bc3c-afd3817f5196\scratchpad`.
> - The code wins over this text: each phase re-reads the files it names before it edits them. This plan was written against `main` `d8d46a4`.

**Goal:** every generated traveller is a person with a personality that speaks through every reply (the claim, the hand-over, a missing form, a spoken request, an answer, small talk and the verdict reaction), while every traveller of a day is asked through one identical wheel; all lines are rows Saleh authors and balances in the content spreadsheet; no personality is a tell and no existing draw moves.

**Architecture:** the rules are pure `TimeDesk.Domain` and tested first: `Personalities` (the cast and its one draw), `Voices` (the resolver: match, tier, fall back, pick by value, fill), `VoiceChecks` (the content rules Generate World and the validator share), `ReactionIntents` (the reaction's intent), and the one-wheel rules in `InterviewDay`, `InterviewQuestion` and `FormRequests`. `Seeds` gains the personality stream and `OfKey`. Assembly-CSharp is glue: `CaseFactory` draws the personality and resolves the small talk; `InterviewPresenter` passes the voice into `InterviewScript`; `GameManager` hands the reaction to the presenter; `TravellerView` lingers for `DeskConfigSO.reactionSeconds`. Content lives in `world_source.json` (`personalities`, `interview.reactions`, `interview.voices.*`) through Generate World, with `ContentSheetMap` entries and the template rewritten.

**Order:** the one wheel first (it removes per-kind code the voice layer would otherwise have to thread), then the personalities and their resolver, then the reaction, then the cast's full lines, the tone pass and the golden re-pack.

---

## 0. How every phase runs

Everything in the redesign plan's §0 applies. In short, and what this plan adds:

- **Branch and worktree:** `design/personalities-vN-<name>` (or the name the orchestrator gives) from the current `main`, in the worktree and editor the orchestrator names. Never edit `E:\unity\NOPE`, never edit `Assets/Scenes/OfficeScene.unity`, never push, rebase or amend, never commit `_TimeDesk*` files or the `.sln`.
- **Tests first:** each task below names its failing tests. Write them, see them fail (a compile error counts), implement, see them pass. Assembly-CSharp is not unit-testable (the test assembly sees Domain and Visuals only): a glue task has no failing-test step, its rules are the Domain calls tested before it, and Unity checks it.
- **Offline gates after every change:** `python SCRATCH/compile_check.py '<worktree>'` (0 errors in every project; a track may use its own checker as the brief says), then `SCRATCH/runner/bin/Debug/net10.0/runner.exe '<the checker's cc folder>\Temp\Bin\Debug'` (`passed N, failed 0`, N at least the phase's starting count plus its new tests).
- **Content:** `Assets/Data/World/world_source.json` (2-space indent, LF; edit it by script, keeping its formatting) through Generate World; generated assets are never hand-edited. Every JSON field added or removed gets its `ContentSheetMap` entry in the same commit (`ContentSheetMapTests` fails otherwise); then Tools > TimeDesk > Write Content Spreadsheet Template, and the template is committed.
- **Serialized enums are append-only** and pinned in `SerializedEnumsTests`. This plan adds two new enums (`ReactionVerdict`, `ReactionIntent`) and appends to none.
- **Unity** (only through the job runner of the named editor; never launch, quit or click a dialog): after each phase, the player scripts compile, the EditMode suite passes except the known third-party `UnitySkills…SceneSummarize_CountsObjectsCorrectly`, Build Office UI rebuilds `OfficeGameplay.unity` identically by semantic dump (no phase here changes the builder; if one must, commit the rebuilt scene), Generate World runs whenever the JSON changed (then commit `Assets/Data`), the validator is clean, and a **smoke play** in the art office (the anime hall, `OfficeScene` with `OfficeGameplay` as the game loads them): Title → New Run, seed 12345 → day 1, one traveller decided, no errors in the log. Then the phase's **feature probes** below, with screenshots at 1920 × 1080 and 1280 × 720 of every new UI state, into `SCRATCH/<tag>/`. The full six-day play-through and the golden re-pack are phase V4's (the end of this epic). Afterwards revert what Unity touched outside the phase (`*.csproj`, `ProjectSettings/*`, the LiberationSans fallback asset) and delete the `_TimeDesk*` files and their metas.
- **Golden masters:** each phase names its golden effect; everything else in `docs/reviews/audit-baseline/golden` must stay byte-identical when diffed (`tools/audit/golden.py diff`). Phases V1-V3 diff and explain; V4 re-packs once.
- **Docs in the commit of the behaviour:** `docs/FEATURES.md` (S §10 lists the lines), `docs/CONTENT_SHEETS.md` when sheets change; "(tested: X)" only when test X exists and covers it.
- **Commits:** small, conventional, each compiling; every message ends with a blank line and the attribution line the orchestrator names (today `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`).
- **Before reporting:** merge the latest `main`, re-run compile, offline and the smoke play, then report in the brief's shape (`PHASE Vn READY <hash>`, the numbers, the files carrying decisions made on Saleh's behalf, the open questions, the screenshots' folder).

## 1. The phases at a glance

| # | Phase | Scope | Size | Depends on |
|---|---|---|---|---|
| V1 | One wheel for every traveller | One ask entry; one question per category asked of everyone in the same words, answers by kind and era; one papers menu of the forms met so far; the new default missing-form lines | M | main `d8d46a4` |
| V2 | Personalities and the voice resolver | The cast and its stream; `Voices`, `VoiceChecks`; the claim, hand-over, missing forms, spoken requests, answers and small talk in each personality's words; a citizen's home is 2150; the premades' optional personality; the debug force; the spec's example lines | L | V1 |
| V3 | The verdict reaction | `ReactionVerdict`, `ReactionIntent`, the defaults and the cast's four base reactions; the line at the stamp; the figure lingers `reactionSeconds` | M | V2 |
| V4 | The cast's lines, the tone pass, the golden re-pack | The core twelve per personality completed, then the answers, spoken requests, missing forms and reason reactions; the tone checklist; the six-day play-through; the golden masters re-packed | M | V3, Saleh's answers to S §12 |

Phase 9 of the redesign plan (labourers) is not on `main` at `d8d46a4`. It touches the same seams (`askableBy` on TC-520, the `Missing` replies, `InterviewCase.missingVariant`, the labourer's claim): whichever of phase 9 and V1 lands second merges the other (S §14).

## 2. The phases

### Phase V1 — One wheel for every traveller (M)

- **Specs:** S W1-W6, T8, §3, §8 (V1's rows), §9 (the questions' and interview's sheet changes), §11 (the removals).
- **Scope:**
  - One ask entry: `interview.askLabel` = "Ask about the trip >"; `interview.tripAskLabel` and `Interview.AskLabel(lines, kind)` removed.
  - One question per category for every kind: `InterviewQuestion.kinds` / `AsksOf` removed; `WordingOverride` loses `prompt`, gains `kinds`; `InterviewQuestion.AnswerFor(eraId, kind)` by the most specific override (kinds 2, era 1, summed; ties keep the first listed), scored by a small Domain matcher, `ContextMatch` (a row's kinds and era against a kind and an era), which phase V2's `Voices` reuses (S V3: one matcher); the prompt is the question's own. `InterviewDay`'s per-kind lists removed; its day lists serve everyone. `InterviewQuestions.Problems` becomes one question per category, `MostForOneKind` becomes the day's count.
  - One papers menu: `DocumentTemplateSO.askableBy`, `AskableForm.AskableBy` / `IsAskableBy`, `FormRequests.For(kind, …)` removed; a Domain rule lists the on-request forms of the day plans up to today in first-appearance order, a group once (`FormRequests.MetSoFar`); `TimelineService.AgencyForms` feeds it the plans in day order and today's day; `FormRequests.ReplyProblems` runs over each day's menu and the kinds in play that day.
  - Content: `q_trip_currency` and `q_trip_device` merged into `q_currency` and `q_device` (S §3.2: prompts, default answers, displaced overrides, days 4 and 5, announcements); the new default missing-form lines (S §3.3).
- **Files:**
  - Domain: new `ContextMatch.cs`, `InterviewContent.cs`, `Interview.cs`, `InterviewDay.cs`, `InterviewScript.cs` (the ask entry, `PromptLine`, `DialogChecks.MenuProblems`' callers), `FormRequests.cs`, `TravellerKind.cs` (its doc names `askableBy`).
  - Scripts: `DocumentTemplateSO.cs`, `Dialog/QuestionSO.cs` (doc), `CaseFactory.cs` (the day's lists), `Timeline/TimelineService.cs`, `UI/Investigation/InterviewPresenter.cs`.
  - Editor: `WorldContentGenerator.cs`, `WorldContentGenerator.Kinds.cs`, `ContentLibraryValidator.cs`, `ContentLibraryValidator.Kinds.cs`.
  - Content: `world_source.json` (questions, `interview.askLabel`, `tripAskLabel` gone, the new replies); `Assets/Data/Investigation/DocTemplate_TC101/230/310/415/416/417/610/620/630.asset` (their `askableBy` lines stripped by an EOL-preserving script); `Domain/ContentSheets/ContentSheetMap.cs`; `ContentSheets/TimeDesk_Content_Template.xlsx`.
  - Tests: new `ContextMatchTests.cs`, `InterviewTests.cs`, `InterviewDayTests.cs`, `InterviewScriptTests.cs`, `FormRequestsTests.cs`, `SerializedEnumsTests.cs` (the `askableBy` pin goes).
  - Docs: `docs/FEATURES.md` (35, 78, 79, 80, 176), `docs/CONTENT_SHEETS.md`, a pointer under T I1 and I2 to S W3 and W4; the audit jobs `tools/audit/unity/_TimeDeskAuditStatic.cs.txt` (the WORLD askable line reads the day's lists) and `_TimeDeskAuditPlay.cs.txt` (check its papers loop handles the one menu; update it if it assumes per-kind requests).
- **Tasks:**
  1. **The answer by kind and era.** Tests first: new `ContextMatchTests` (`Score_BlankMatchesAnything`, `Score_NamedKindsTwoNamedEraOne`, `Score_AnotherKindOrEraNeverMatches`); `InterviewTests`: `AnswerFor_KindsOverrideBeatsEraOverride`, `AnswerFor_KindsAndEraBeatKindsAlone`, `AnswerFor_ATieKeepsTheFirstListed`, `AnswerFor_AnOverrideOfAnotherKindNeverApplies`, `AnswerFor_NoOverrideGivesTheDefault`, `Prompt_IsTheQuestionsOwnForEveryKindAndEra`; `AskLabel_*` tests removed. Then `ContextMatch` (new, Domain), `WordingOverride.kinds`, `prompt` removed, `AnswerFor(eraId, kind)`, `PromptFor` removed (callers read `prompt`). Gates; commit.
  2. **The day's questions for everyone.** Tests first, `InterviewDayTests`: `Questions_AreTheDaysForEveryKind`, `AskableCategories_OnePerCategoryInLibraryOrder`, `AnswerTellCategories_AreTheDayGatedOnes`, `Problems_OneQuestionPerCategory` ("Question 'x' asks about Currency, as 'y' does"), `Count_IsTheDaysQuestions`. Then `InterviewQuestion.kinds` / `AsksOf`, `QuestionsFor`, `AskableCategoriesFor`, `AnswerTellCategoriesFor`, `MostForOneKind` removed; `CaseFactory` and `InterviewPresenter` read `Questions`, `AskableCategories`, `AnswerTellCategories`. Gates; commit.
  3. **The papers menu of the days so far.** Tests first, `FormRequestsTests`: `MetSoFar_ListsEveryOnRequestFormOfTheDaysUpToToday`, `MetSoFar_NeverListsALaterDaysForm`, `MetSoFar_KeepsAnEarlierDaysFormOnADayWithASmallerMix`, `MetSoFar_OneEntryPerGroup`, `MetSoFar_SkipsFormsHandedOverOnArrival`, `ReplyProblems_EveryKindInPlayNeedsAnHonestLineForEachRequestItNeverCarries`. Then `FormRequests.MetSoFar`, `askableBy` and its members removed, `ReplyProblems` over (the day's menu, the day's kinds, each kind's carried forms), `TimelineService.AgencyForms(lib, day)`, `InterviewDay.AskableForms` (no kind). Gates; commit.
  4. **The same wheel.** Tests first, `InterviewScriptTests`: `Build_EveryKindGetsTheSameHubPapersAndAskMenus` (one day, four kinds with their own papers: identical choice ids, labels and kinds in the hub, the papers menu and the ask menu), `Build_TheAskEntryIsTheAskLabelForEveryKind`, `Build_ADisplacedTravellerAskedForAManifestSaysTheirHonestLine`, `Build_ACitizenAskedForAReturnOrderSaysTheirHonestLine`, `MenuProblems_FiveRequestsFitThePapersMenu`. Then the ask entry reads `askLabel`. Gates; commit.
  5. **Content.** The questions of S §3.2 (order kept: currency, language, device, capital, ruler, date of birth; `q_trip_*` removed; the days' announcements); `askLabel`; `tripAskLabel` removed; S §3.3's replies; the nine templates stripped; `ContentSheetMap` (`questions.kinds` removed; `questionOverrides`: `era` omitted when blank, `kinds`, `answer`; `interview.tripAskLabel` removed); the generator and the validator call the Domain rules (one question per category; the replies per day; the menu's capacity with the most requests any day offers). `ContentSheetMapTests` green. Commit the source and map; Generate World and the template in Unity (below), then commit `Assets/Data` and the template.
  6. **Docs.** FEATURES 35, 78, 79, 80, 176; CONTENT_SHEETS.md; the T pointers; the audit jobs. Commit.
- **Unity:** Generate World (0 errors; the validator clean); Write Content Spreadsheet Template; the smoke play; feature probes:
  - day 1: a rich and a poor tourist in turn: read the wheel's hub, papers menu and ask menu from the ring's choices (identical ids, labels and order; screenshot the papers menu at both sizes); ask the rich tourist for the Stranding Waiver (the refusal line in the bubble);
  - Skip Day to day 4: the trip questions for every traveller, the announcement in the morning paper;
  - Skip Day to day 5: a displaced traveller's papers menu (five entries; screenshot), asked for a Departure Manifest (their Honest line), the ask menu (the same six entries as a tourist's, the trip prompts); a tourist asked for a Return Order (their line); the menus' capacity (no overflow at 720p).
- **Golden effect:** `cases.txt`: the WORLD `askable` / `answerTells` lines (the day's lists), a citizen's `answers=` on days 5-6 (+ Language, Geography, Politics); every tell, paper, name, record and look byte-identical (T8). The play transcript: the day-4 and day-5 announcements, the displaced's prompts, the menus of days 5-6 and their missing-form lines. `world_generate`, `data_hashes` (question assets, the nine templates), `validator`.
- **FEATURES:** 35, 78, 79, 80, 176.
- **Split seam:** tasks 1-2 (the questions) can land before 3-4 (the papers menu).
- **Depends on:** `main` `d8d46a4`; the phase 9 seam (§1).

### Phase V2 — Personalities and the voice resolver (L)

- **Specs:** S PS1-PS5, V1-V9, T1-T6, §2, §4, §5, §7.3 (the first rows), §9.
- **Scope:**
  - `Seeds.PersonalitySalt` ("PRSN", `0x5052534E`), `Seeds.ForPersonality(caseSeed)`, `Seeds.OfKey(string)` (a fold of `Seeds.Mix` over the characters).
  - Domain `Personality` (id, name, weight, note) and `Personalities.Pick(cast, rng)` (one `WeightedRandom.Pick`; none, with no draw, for an empty cast) and `Personalities.Problems`.
  - Domain `VoiceLine` (personality, kinds, era, key, variant, verdict, intent, line; V3 adds the reaction fields' enums, so V2 leaves them out), `VoiceBook` (one list per slot: claims, handOver, missingForms, spoken, answers, smallTalk), `VoiceContext` (kind, claimed era: nothing else), `VoiceKeys` (the slot keys), `Voices` (match, tier, fall back, pick by value, per slot; the questions' overrides use its matcher), `VoiceChecks.Problems` (tokens per slot, the worst case, references, the fact guard, duplicates, coverage).
  - `CaseInstance.personality` and `dialogSeed`; `CaseFactory`: the personality drawn on its own stream for a generated traveller (a premade takes `LegendarySO.personality`, no draw; `DevToolsState.ForcedPersonality` overrides after the draw), the small talk through `Voices` with the home's lines (the displaced's claimed place, else its era; a citizen's present place, else the Future era), `_dialogRng` and `Interview.PickSmallTalk` removed.
  - `InterviewCase.voice`; `InterviewScript.Opening` says the personality's claim (the banner's `claimLine` stays the kind's), `Build` resolves the hand-over, missing forms, spoken requests and answers through `Voices`.
  - `LegendarySO.personality`; `ContentLibrarySO` holds the cast; `InterviewLines.voices`; the debug panel shows the traveller's personality and sets `ForcedPersonality`.
  - Content: `personalities` (the seven, weight 1, their notes), `interview.voices` with S §7.3's lines, `premades[].personality` blank; `ContentSheetMap` (`personalities`, the six voice sheets, `premades.personality`).
- **Files:**
  - Domain: `Seeds.cs`, new `Personalities.cs`, new `Voices.cs`, new `VoiceChecks.cs`, `Interview.cs`, `InterviewContent.cs`, `InterviewScript.cs`, `ContentSheets/ContentSheetMap.cs`.
  - Scripts: `CaseInstance.cs`, `CaseFactory.cs`, `LegendarySO.cs`, `ContentLibrarySO.cs`, `UI/Investigation/InterviewPresenter.cs`, `DevTools/DevToolsState.cs`, `DevTools/DebugPanelController.cs`.
  - Editor: `WorldContentGenerator.cs` (the cast, the voices, the premades' personality, line ids `interview.voices.{list}.{personality}.{n}`; `VoiceChecks` and `Personalities.Problems` before writing), `ContentLibraryValidator.cs` (the same rules on the assets; the coverage info line).
  - Content: `world_source.json`; the template.
  - Tests: `SeedsTests.cs`, new `PersonalitiesTests.cs`, new `VoicesTests.cs`, new `VoiceChecksTests.cs`, `InterviewTests.cs`, `InterviewScriptTests.cs`.
  - Docs: FEATURES 11, 71, 73, 77, 78, 80, 176; CONTENT_SHEETS.md; the audit static job prints `personality=` after `gender=`.
- **Tasks:**
  1. **The streams.** Tests first, `SeedsTests`: `PersonalitySalt_IsPinned`, the salt in the one distinctness table, `ForPersonality_IsPinnedForSampleSeeds`, `OfKey_IsPinned` (for "claim", "smalltalk", "answer:q_currency"), `OfKey_DiffersPerKey`, `OfKey_OfEmptyIsItsStart`. Then `Seeds`. Gates; commit.
  2. **The cast.** Tests first, `PersonalitiesTests` (with `ScriptedRandom`): `Pick_IsOneDraw`, `Pick_FollowsTheWeights`, `Pick_NeverAZeroWeight`, `Pick_OfAnEmptyCastIsNoneWithNoDraw`, `Problems_BlankOrDuplicateId`, `Problems_NegativeWeight`, `Problems_NoPositiveWeight`, `Problems_BlankName`. Then `Personality`, `Personalities`. Gates; commit.
  3. **The resolver.** Tests first, `VoicesTests`: `Line_APersonalityRowBeatsTheDefault`, `Line_NamedKindsBeatBlankKinds`, `Line_ANamedEraBeatsABlankEra`, `Line_KindsOutrankEra`, `Line_ARowOfAnotherKindOrEraNeverMatches`, `Line_ARowOfAnotherPersonalityNeverMatches`, `Line_NoPersonalitySaysTheDefault`, `Pick_IsAValueOfTheSeedAndTheSlotKey`, `Pick_RowsUnderAnotherKeyNeverMoveIt`, `Pool_IsTheBestTierOnly`, `SmallTalk_PoolsThePersonalitysTierAndTheHome`, `SmallTalk_WithNoPersonalityIsTheHomes`, `Answer_FallsBackToTheQuestionsOverrideThenItsAnswer`, `ContextHoldsOnlyWhatTheDeskSees` (reflection: `VoiceContext`'s public fields are exactly the kind and the claimed era), `SlotKeys_ArePinned`. Then `VoiceLine`, `VoiceBook`, `VoiceContext`, `VoiceKeys`, `Voices`, scoring rows with V1's `ContextMatch` (one matcher for the voice rows and the questions' overrides, S V3). Gates; commit.
  4. **The content rules.** Tests first, `VoiceChecksTests`: `AClaimWithoutPlace`, `AnAnswerWithoutValue`, `AnUnknownToken`, `ATokenOutsideItsSlot` (`{value}` in a claim, `{document}` in small talk), `TooLongWithTheLongestFill`, `AnUnknownPersonality`, `AnUnknownEra`, `AnUnknownQuestion`, `AnUnknownSpokenRequest`, `AMissingFormsRequestNoDayOffers`, `BlankText`, `AFactValueWarns`, `ATransponderModelWarns`, `AnEmployerWarns`, `ADuplicateRowWarns`, `CoverageListsEachPersonalitysFallbacks`. Then `VoiceChecks`. Gates; commit.
  5. **The interview speaks.** Tests first, `InterviewScriptTests`: `Opening_SaysThePersonalitysClaim`, `Opening_ClaimKeyWordsAreTakenOverItsTemplate`, `Opening_NoPersonalitySaysTheKindsClaim`, `Build_TheHandOverInThePersonalitysWords`, `Build_AMissingFormByKindVariantAndPersonality`, `Build_ASpokenRequestsReply`, `Build_AnAnswerKeepsItsCanonicalValueAndFact`, `Build_ATellsSentenceIsTheHonestSentence` (two cases alike but for one answer's tell: the same sentence around the value, the same claim, replies and small talk), `Build_NoPersonalitySaysTodaysLines` (a fixture's whole transcript equals the one before this phase). Then `InterviewCase.voice`, `Opening`, `Build`. Gates; commit.
  6. **Glue.** `CaseInstance`, `CaseFactory` (the draw; the force; the home's lines; the small talk through `Voices`; the log line gains the personality), `LegendarySO`, `ContentLibrarySO`, `InterviewLines.voices`, `InterviewPresenter` (the voice into the case), `DevToolsState` and the debug panel (`ResetAll` clears the force). Offline gates; commit.
  7. **Content and tooling.** The cast (weight 1 each, S §2.1's one-liners as notes), S §7.3's lines as the first voice rows, `premades[].personality` blank; `ContentSheetMap`; the generator and the validator (`VoiceChecks`, `Personalities.Problems`, a premade's personality listed or blank; the coverage info line). `ContentSheetMapTests` green. Commit; Generate World and the template in Unity, then commit `Assets/Data` and the template.
  8. **Docs.** FEATURES 11, 71, 73, 77, 78, 80, 176; CONTENT_SHEETS.md; the audit static job. Commit.
- **Unity:** Generate World (0 errors; the validator clean; the coverage lines listed in the report); the template; the smoke play; feature probes:
  - **each personality speaking** (a job loop: set `DevToolsState.ForcedPersonality` to each of the seven, New Run seed 12345, present day 1's slot 1): the claim in the bubble (screenshot at both sizes, one per personality), the banner and the claim tag still the kind's sentence, a request for the Stranding Waiver (the refusal), small talk (a 2150 line, never the destination's);
  - **the independence probe** (a static job): days 1-6 × 20 run seeds generated twice, the second with every blueprint's liar chance and every day's fault chances at 0 in memory (never saved): every slot's personality identical (report `identical n / n`); the report tabulates the cast among liars and among the honest of the first run (for review, not a gate);
  - day 5: a displaced traveller with a personality line, untranslated (the key words in English) and, with a Speech translator bought on night 4, flipping into English;
  - a premade (day 6, Senenmut) says today's lines (their personality blank).
- **Golden effect:** `cases.txt`: + `personality=` on every CASE line; `smalltalk=` for every traveller (the value pick; a citizen's pool is 2150's; the cast's small-talk rows); nothing else. The play transcript: the claims, replies, answers and small talk of travellers whose personality has a row.
- **FEATURES:** 11, 71, 73, 77, 78, 80, 176.
- **Split seam:** tasks 1-4 (Domain) as one commit series; then 5-8.
- **Depends on:** V1.

### Phase V3 — The verdict reaction (M)

- **Specs:** S R1-R5, T7, §6, §7.3 (the reactions), §9 (`interviewReactions`, `voiceReactions`).
- **Scope:**
  - Domain enums `ReactionVerdict` (Accepted, Denied) and `ReactionIntent` (Honest, Lying), serialized, pinned; `ReactionIntents.Of(isLiar, isForger)` (a place lie, smuggling included, or a record lie is Lying; anything else Honest).
  - `VoiceLine` gains the verdict and intent; `VoiceBook.reactions`; `InterviewLines.reactions` (the defaults); `Voices.Reaction` (the reason tier, then kinds, then era; the defaults); `VoiceChecks` requires the four defaults and a known reason (`Faults`' reasons and the costume error's `panic`).
  - `InterviewScript.Reaction(lines, case, verdict, intent, reason)` → a traveller `DialogLine` with its key-word spans.
  - Glue: `GameManager.HandleDecision` resolves the reaction from the case (`IsLiar`, `IsForger`, `FaultReason`) after scoring, exactly where it is today, and hands it to the investigation UI; `InterviewPresenter` appends it to the transcript and says it in the bubble; `TravellerView` keeps the figure for `DeskConfigSO.reactionSeconds` (2.5 on `Desk_Default.asset`; 0 clears at once), and showing the next traveller ends the linger; the booth phase, the slip and the clock are untouched.
  - Content: S §4.2's six default reactions, the cast's four base reactions each (S §7.3's, completed), S §7.3's reason rows; `ContentSheetMap` (`interviewReactions`, `voiceReactions`).
- **Files:**
  - Domain: `Voices.cs`, `VoiceChecks.cs`, `InterviewContent.cs`, `InterviewScript.cs`, `Faults.cs` (the list of reasons `VoiceChecks` reads, if it has none), `ContentSheets/ContentSheetMap.cs`.
  - Scripts: `GameManager.cs`, `UI/InvestigationUIController.cs` (the façade's call), `UI/Investigation/InterviewPresenter.cs`, `Characters/TravellerView.cs`, `Office/DeskConfigSO.cs`; `Assets/Data/Config/Desk_Default.asset` (the knob, authored: Generate World never writes it).
  - Editor: `WorldContentGenerator.cs`, `ContentLibraryValidator.cs`, `ContentLibraryValidator.Desk.cs` (the knob's range).
  - Tests: `SerializedEnumsTests.cs`, `VoicesTests.cs`, `VoiceChecksTests.cs`, `InterviewScriptTests.cs`.
  - Docs: FEATURES 36, 176; CONTENT_SHEETS.md; the audit play job logs the reaction line after each verdict.
- **Tasks:**
  1. **The enums and the intent.** Tests first: `SerializedEnumsTests` pins `ReactionVerdict` and `ReactionIntent`; `VoicesTests.Intent_OfAPlaceLieSmugglingOrARecordLieIsLying`, `Intent_OfADirectiveFaultACostumeErrorOrNoFaultIsHonest`. Then the enums and `ReactionIntents`. Gates; commit.
  2. **The reaction's resolution.** Tests first, `VoicesTests`: `Reaction_ANamedReasonBeatsNamedKinds`, `Reaction_ARowOfAnotherReasonNeverMatches`, `Reaction_FallsBackToTheDefaults`, `Reaction_TheFourDefaultsCoverEveryVerdictAndIntent`, `Reaction_PickIsAValueOfItsKey`. Then `Voices.Reaction`. Gates; commit.
  3. **Its content rules.** Tests first, `VoiceChecksTests`: `Reactions_TheFourDefaultsAreRequired`, `Reaction_AnUnknownReason`, `Reaction_TokensArePlaceOnly`. Then `VoiceChecks`. Gates; commit.
  4. **The line.** Tests first, `InterviewScriptTests`: `Reaction_IsATravellerLineWithItsKeyWordSpans`, `Reaction_NoPersonalitySaysTheDefault`. Then `InterviewScript.Reaction`. Gates; commit.
  5. **Glue.** `GameManager`, the façade, `InterviewPresenter`, `TravellerView`, `DeskConfigSO`, `Desk_Default.asset`, the validator's knob check. Offline gates; commit.
  6. **Content, docs.** The defaults and the cast's reactions; the map; the template; FEATURES 36 and 176; CONTENT_SHEETS.md; the audit play job. Commit; Generate World and the template in Unity, then commit `Assets/Data` and the template.
- **Unity:** Generate World; the template; the smoke play; feature probes on day 1 (seed 12345, `ForcedPersonality` set per probe):
  - accept an honest tourist: the reaction in the bubble and the transcript, the figure staying about 2.5 s then leaving (screenshot mid-linger at both sizes);
  - deny an honest tourist: the WRONG slip and the indignant line together;
  - deny a liar with a logged deviation: CORRECT and the caught line; accept a liar: the citation and the got-away line;
  - call the next traveller during a linger: it ends at once, no error, the next traveller's claim follows;
  - decide on the PC: the line in the Transcript tab, found by search;
  - `reactionSeconds` 0 on a copy of the config in memory: the traveller leaves at once, as before;
  - a decision at closing time ends the day exactly as before (the clock untouched).
- **Golden effect:** the play transcript: one reaction line after each verdict; `data_hashes` (`Desk_Default.asset`, the library); `cases.txt` and the saves unchanged.
- **FEATURES:** 36, 176.
- **Depends on:** V2.

### Phase V4 — The cast's lines, the tone pass, the golden re-pack (M)

- **Specs:** S C2, C4, §7 (the whole tone guide and the author's checklist), §8, §12 (Saleh's answers).
- **Scope:**
  - Re-read S §12 with Saleh's answers; apply any changed decision first (each names its decisions), or report it as a blocker when it needs code beyond this plan.
  - Author, per personality: the core twelve (S C2) completed, then an answer per question (six), the two spoken requests, the missing forms of every default pair (S §3.3) and the rich tourist's two refusals, two or three reason reactions: about 25-30 lines each. Where Saleh authors in the spreadsheet instead: Export Content Spreadsheet, he edits, Import Content Spreadsheet (which runs Generate World).
  - Every line through the author's checklist (S §7.4); the fact guard and the length check clean; the coverage report shows no fallback in the core twelve.
  - The full six-day play-through in the art office, the golden masters re-packed with the account of the diff (S §8).
- **Files:** `world_source.json`, `Assets/Data` (Generate World), the template; `docs/reviews/audit-baseline/golden` (the re-pack); `docs/FEATURES.md` if a line's wording changed a listed example.
- **Tasks:**
  1. The lines, by personality (seven commits, `content(voices): <personality>`), each followed by the offline gates (`ContentSheetMapTests`) and Generate World in Unity.
  2. The tone pass: read every line aloud in context (the probes' gallery below); fix what fails the checklist. Commit.
  3. The golden re-pack: the audit jobs (static, play, profile, twice each for determinism), `golden.py diff` against the baseline explained in the commit (S §8's rows: every intended change, nothing else), then `golden.py pack` to `docs/reviews/audit-baseline/golden`. Commit.
- **Unity:** Generate World (0 errors, the validator clean, the coverage lines); the smoke play; the **gallery**: for each personality (the force) and each kind in play (days 1 and 5), the claim, a missing-form refusal, an answer, small talk and a reaction, screenshotted at both sizes into `SCRATCH/<tag>/gallery/`; the **full six-day play-through** (seed 12345) with screenshots of each new state; the independence probe of V2 re-run on the full content (`identical n / n`).
- **Golden effect:** the re-pack itself: `cases.txt` (small talk with the full cast), `play_transcript.txt` (every personality line), `world_generate`, `data_hashes`, `validator`; saves, scenes and profiles unchanged.
- **FEATURES:** only if a listed example line changed.
- **Depends on:** V3; Saleh's answers to S §12.

## 3. The golden masters per phase

| File | V1 | V2 | V3 | V4 |
|---|---|---|---|---|
| `cases.txt` | WORLD askable/answerTells; citizens' answers days 5-6 | + `personality=`; `smalltalk=` | none | small talk with the full cast; re-packed |
| `play_transcript.txt` | announcements, displaced prompts, menus of days 5-6 | the cast's lines where rows exist | + one reaction per verdict | every line; re-packed |
| `play_saves/*.json` | none | none | none | none |
| `world_generate.txt`, `data_hashes.txt`, `validator.txt` | questions, nine templates, validator | the library, premades, validator (+ coverage) | the library, `Desk_Default.asset` | the library; re-packed |
| scene dumps, profiles | none | none | none | none |
| must stay byte-identical throughout | every tell, paper, name, record, look, verdict, score, ledger line and save | same | same | same |

Offline golden values change in the tests each task names (S §8's first table).

## 4. Risks and seams

- **Phase 9 (labourers)** lands before or after V1 (§1, S §14). Its labourer claim default, `Missing` replies and `InterviewCase.missingVariant` need no change here; its step set stays per kind (S W6).
- **A tell draw moving** would be a bug: V1's one wheel must leave every `tells=` byte-identical in `cases.txt` (S T8). If it moves, the question order or a gate differs from S §3.2; fix the content, never the planner.
- **The dialog stream's small-talk draw** disappears in V2 (S V4); only `smalltalk=` may change for it. Any other field moving means a stream was touched.
- **The linger and closing time** (V3): `_travellerAtDesk` goes false at the stamp as today, so the closing-time rule never waits on a reaction; the probe checks it.
- **Line length at 720p:** a Chatty line at 100 characters is two bubble lines; the V2 gallery screenshots confirm the bubble holds it (`maxLineChars` is a content knob if it does not).
- **Parallel tracks appending enums:** V3 adds two new enums and appends to none, so no renumbering can collide.

## 5. Why this order

- V1 first: it deletes the per-kind question and papers code, so V2's resolver threads one context through one wheel instead of four; it also stands on its own (rule 3 holds even before a single personality line exists).
- V2 before V3: the reaction is a voice slot, so it needs the cast, the stream, the resolver and the checks.
- V4 last: the content fills machinery that is already proven; Saleh's answers to S §12 arrive before the bulk of the authoring, so no line is written twice; the golden re-pack happens once, at the end of the epic, as the check levels ask.
