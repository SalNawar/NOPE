# Traveller personalities: design

*2026-09-29 · drafted by Claude from Saleh's brief and his design rules of the same day, then revised the same day with his answers to all ten of its questions ("Saleh's answers" below; §12 records how each was applied) · read-only map of `main` at `d8d46a4` (`E:\unity\NOPE-docs`) · the code wins over this text · the traveller-types spec (`2026-09-26-traveller-types-design.md`) owns the kinds, papers, lies and verdicts; this spec owns who the travellers are as people, what they say, the one wheel they are all asked through, and the removal of every printed claim; §14 is the seam · built in the phases of `docs/superpowers/plans/2026-09-29-traveller-personalities-plan.md` · every decision below is recorded with its reason; one new question (Q11) waits for Saleh*

Saleh, verbatim:
- "not every poor and rich. people have personalities we should focus on delivering a believable but also funny absurd world"
- "people will have different reactions. as a rule your interactions will stay consistant across types it is the context that changes"

His design rules of 2026-09-29 that this spec applies (they override any spec text that disagrees):
- **Rule 2.** Personalities, not traveller types, drive claim lines, reactions and small talk; aim for a believable but funny, absurd world.
- **Rule 3.** Interactions are the same for every traveller type (the same wheel entries and verbs); only the reply changes with personality and context.
- **Rule 4.** Balance knobs must be editable by Saleh in the Unity editor (ScriptableObjects that Generate World does not overwrite, or data he edits through the content spreadsheet), and authored cases (premades, forced slots, story beats) stay outside the random draws.
- Rule 1 (one penalty for any wrong decision) is untouched: a personality never changes a score (R5).

## Saleh's answers (2026-09-29)

| Q | His answer | Applied in |
|---|---|---|
| Q1 The cast | The seven: Chatty, Curt, Anxious, Sunny, Grand, Stickler, Glum. | PS1, §2 |
| Q2 A tell? | "A rare 'slip' line": a liar may rarely say a slip line honest travellers never say; a hint that prompts a closer look, never a proof. | T4, T9-T11, §5, §7.3 |
| Q3 The wheel | Identical for everyone: "you can't know the type before checking the papers". The same questions in the same words, one papers menu of every form met so far, refusals in character. | W1-W6, B5, §3 |
| Q4 Small talk | "everything: personality, home, type": one pool drawing on the personality's lines, the home's lines and the kind's lines. | V5 |
| Q5 The banner | "there should be no banner; the claimant will ask in their personality, then you need to rely on papers to figure it out". | B1-B5, §3.5; the plan's first phase, V0 |
| Q6 The reaction | By verdict and intent, and "there should be personality in it, they will say one or two lines". | R1-R3, §6 |
| Q7 Narrative dialogs | One authored version for everyone. | V10 |
| Q8 Premades | "own lines for every slot": every famous traveller gets an authored line for every voice slot. | PS3, §4.5 |
| Q9 Weights | One weight per personality for every kind. | PS2 |
| Q10 Does the agency know? | The temperament is never printed. | PS4 |

In this spec **a slip** is the liar's slip line (Q2); the paper that shows CORRECT or WRONG is always called **the verdict slip**.

## 0. The map: what exists and what this spec does with it

Read in the code at `d8d46a4` (phase 9, labourers, is not on main yet: no Labourer blueprint, no paper-set directive, `InterviewCase.missingVariant` always `Honest`).

| What (where) | Today | This spec |
|---|---|---|
| The claim, printed (`claim.banner` = "{0}\n\"{1}\"": the name and role, then the claim sentence; `InvestigationUIController.ShowRich` formats it from `CaseInstance.claimLine`) | on the Investigation app's case header (`InvestigationApp.claimText`, the builder's `ClaimText`) and the office claim tag (`OfficeCaseHud`'s `ClaimStrip`), always readable | **removed**: nothing prints the claim (B1-B3) |
| The claim, recorded (`CaseVerdict.claimSummary`) | written by `ShiftScoring`, read by nothing but the audit play's verdict dump | removed (B3) |
| The claim, spoken (`InterviewScript.Opening`: the transcript's `case.claim` line, the bubble on arrival; search indexes it as a transcript line) | the kind's sentence (`interview.claims`) | the only claim: said in the voice's words, the kind as context (V1) |
| The Reference's claimed row ("(claimed)", "Claimed place only", smart links to the claimed-place row, the steps' `CostumeClaimed` link) | the case's destination, first and marked | kept: it follows the place the traveller asked for aloud and prints no claim sentence or kind (B4; §12 Q11 asks) |
| The steps checklist (`StepsPanel.BeginCase` → `CaseSteps.Resolve(sets, kind, day)`) | the kind's set from the moment the traveller arrives: a poor tourist's "Ask for a proof of means" names their kind before a paper is read | the `default` set until the paper handed over on arrival is read, then the kind's (B5) |
| The hand-over reply (`interview.requestReply`) | "Here you are." from everyone | per voice (V1) |
| Missing-form replies (`interview.missingFormReplies`: kind, request, variant) | six rows; a rich tourist asked for a waiver: "It's a Premium unit, I don't need one." | per voice, in character, kind and variant as context (V1); new default rows for the pairs the one menu adds (W5) |
| Spoken requests (`interview.requests`: Step closer, Speak up) | one reply each | per voice (V1) |
| Questions (`questions[]` with `kinds`; `overrides` by era with prompt and answer) | the displaced are asked about home (five questions), the 2150 citizens about the trip (two), through two rows per category for Currency and Device and two ask entries ("Ask about home >", "Ask about the trip >") | one question per category, asked of everyone in the same words (W2, W3); the answer's sentence by kind and era (overrides, answer only) and by voice (V1) |
| The papers menu (`DocumentTemplateSO.askableBy`, `FormRequests.For`, `TimelineService.AgencyForms` over every day plan) | per kind: citizens Manifest, Waiver, Proof of means; the displaced Intake Declaration, Return Order | one menu for everyone: every on-request form of the days so far (W4) |
| Small talk (`places[].smallTalk`, `eras[].smallTalk`; one draw on the dialog stream, `Interview.PickSmallTalk`) | the claimed place's lines, else its era's, for every kind: a 2150 tourist bound for Athens recites Athens' small talk | one pool on three sources: the personality's lines, the home's lines (a citizen's home is 2150) and the kind's lines, weighted (V5) |
| The verdict | the traveller vanishes as the stamp lands (`GameManager.HandleDecision` calls `SetTravellerAtDesk(false)`); nothing is said | one or two lines in the voice, by verdict and intent; the figure lingers for a knob's seconds (R1-R5) |
| A liar's words | the same as an honest traveller's | the same, except a rare slip line after small talk (T9-T11) |
| Narrative dialogs (`dialogs[]`) | authored lines, spoken by any traveller the dialog is offered to | unchanged (V10) |
| Premades (`premades[]`: intro, record note, dialog) | authored intro and dialog; everyone's generic lines elsewhere | unchanged, plus an authored line for every voice slot, never drawn (PS3) |
| Streams (`Seeds`) | case, violators, lies, dialog (small talk), looks, legendary, forms (a value), account, faults, strandings, slot, debt news | + `Seeds.ForPersonality` ("PRSN", one draw), + `Seeds.ForSlip` ("SLIP", one draw, liars only); the dialog seed becomes the seed of every line pick, as values, never draws (PS2, T9, V4) |
| Content sheets (`ContentSheetMap`, 82 sheets) | claims, missing-form replies, questions, overrides, premades | + 12 sheets; questions lose `kinds`; overrides lose `prompt` and gain `kinds`; days gain `slipChance`; the interview gains the small-talk weights and loses `tripAskLabel`; the `claim.banner` string goes (§9) |

## 1. Decisions

| Id | Decision | Why |
|---|---|---|
| PS1 | **The cast is seven personalities** (Saleh's Q1), content rows in `world_source.json` `personalities` (id, name, weight, note): **Chatty, Curt, Anxious, Sunny, Grand, Stickler, Glum** (§2). The set is data: Saleh adds, removes and re-weights them in the spreadsheet. | His answer. Seven cover the two axes a desk conversation plays on (says a lot or a little; trusts the system or not), the three stances of the debt dystopia (believes it: Sunny; endures it: Glum; games it: Grand), the bureaucracy's own voice (Stickler) and the border game's nerves (Anxious). |
| PS2 | **A generated traveller's personality is one weighted draw over the cast on a new salted stream**, `Seeds.ForPersonality(caseSeed)` ("PRSN", `0x5052534E`), through `WeightedRandom.Pick`, with **one weight per personality for every kind** (Saleh's Q9). Nothing else draws on the stream, and the draw reads nothing else. `SeedsTests`' distinctness table gains the salt. First cut: every weight 1. | His answer; the house rule for a new random concern, and the brief's "no existing draw shifts". One weight for every kind makes the personality independent of the kind ("not every poor and rich") and of every fault (T1). |
| PS3 | **Premades are never drawn and speak their own lines in every voice slot** (Saleh's Q8). A voice row may name a premade instead of a personality; each premade has an authored line for every slot it can reach (§4.5: the claim, the hand-over, the missing forms of the day-6 menu, the two spoken requests, the six answers, small talk, the reactions of its intent, and a slip for a liar premade); the validator refuses a premade with a slot uncovered once the content lands (plan phase V5). Their intros, record notes and dialogs stay authored. | His answer, and rule 4 (authored cases stay outside the random draws). A premade is one written person; a drawn personality would flatten Socrates into a type. |
| PS4 | **The personality is never printed** (Saleh's Q10): no paper, record, row or tag shows it; only its lines do. The debug panel shows it and can force it (`DevToolsState.ForcedPersonality`, for testing, never saved; the draw still runs, so no stream moves). | His answer. A printed label would let the player read the tag instead of the person; "believable" is people showing who they are. |
| PS5 | **A traveller's voice is fixed at generation**: `CaseInstance.personality` (the id, blank for a premade), `CaseInstance.dialogSeed` (`Seeds.ForDialog(caseSeed)`) and, for a liar who slips, `CaseInstance.slip` (the line). Every line is resolved from them by one pure Domain resolver, `Voices` (§4.3): the small talk and the slip at generation (they need the home's lines and the lie), every other line when the interview is built, the reaction at the stamp. | One place decides every line; Assembly-CSharp only passes the case in; the rule runs headless. |
| B1 | **Nothing prints the claim** (Saleh's Q5): the Investigation app's case header loses its claim text (`InvestigationApp.claimText`, the builder's `ClaimText`), the office case HUD loses its claim tag (`OfficeCaseHud.SetClaim`, `claimRoot`, `claimText`, the builder's `ClaimStrip`), and the `claim.banner` string goes. | His words: "there should be no banner; the claimant will ask in their personality, then you need to rely on papers to figure it out". |
| B2 | **What takes their place**: the case header keeps its counters ("Papers 1 of 3 received · 1 scanned · Deviations 0") and the Accept and Deny buttons; the counters take the header's full height and read "Waiting for the next traveller" (`idle.waiting`) between travellers. The window title keeps "Investigation · <traveller>" (the desk's register of who stands there, not what they ask for). The office HUD keeps only its compare strip, moved up into the claim tag's place. The theme role `ClaimStrip` stays on the case header's band. | The header still says how far the papers are, and the PC's decision buttons stay where the hand expects them; the office view loses a strip and gains nothing, as Saleh asked. |
| B3 | **The claim exists only as the traveller's spoken line**: the transcript's `case.claim` line and the bubble on arrival, in their voice's words (V1). `CaseInstance.claimLine` and `CaseVerdict.claimSummary` go; the audit jobs print the spoken claim instead. The claim stays findable by search as the transcript line it is. | One source for the claim, and it is the person's. `claimSummary` had no reader in the game. |
| B4 | **The Reference keeps its claimed row** ("(claimed)", "Claimed place only" on at each case, the smart links to the claimed-place row, the steps' Costume Guide link). | It follows the place the traveller asked for aloud, and it prints a book row, never the claim sentence or the kind; the destination is also printed on every paper handed over on arrival. §12 Q11 asks Saleh whether he wants it hidden until a paper is read. |
| B5 | **The steps checklist names no kind before the papers**: it lists the `default` set when a traveller arrives and switches to their kind's set once the paper handed over on arrival has been read (lifted into the hand at the desk, or its scanned copy opened), progress kept by step id. | Saleh's Q3: "you can't know the type before checking the papers". Today the set appears at arrival, and a poor tourist's "Ask for a proof of means" names their kind before the visa's class is read. The arrival paper is the papers' first word on the kind. |
| W1 | **The rule (Saleh's rule 3 and Q3): every traveller of a day is offered the same wheel**: the same entries (labels, kinds, order, icons), the same verbs and the same desk prompts. Only the traveller's reply changes, by their voice and the context (§3). | His words; the wheel must not tell the type before the papers do. |
| W2 | **One ask entry**: `interview.askLabel` = "Ask about the trip >" for everyone; `interview.tripAskLabel` and `Interview.AskLabel(lines, kind)` go. | Every traveller travels to their claim (traveller types K2: the claim is the destination for every kind), a displaced person to their home; "the trip" is true of all. |
| W3 | **One question per category, asked of everyone in the same words, about the place they are going to** (`{place}`). `questions[].kinds` goes, with `InterviewQuestion.AsksOf`, `InterviewDay.QuestionsFor` / `AskableCategoriesFor` / `AnswerTellCategoriesFor` and `InterviewQuestions.MostForOneKind`: the day's lists serve everyone. `overrides[]` lose `prompt` (the desk's words are the question's) and gain `kinds`: an override rewrites the traveller's default answer for some kinds, an era, or both, the most specific winning (V3). The merged rows keep today's order (currency, language, device, capital, ruler, date of birth); Currency and Device from day 4, Language, Capital and Ruler from day 5, Date of birth by the upgrade (§3.2). | Rule 3 and Q3. The trip's prompts (today's citizen prompts) also read right for the displaced going home. Keeping the order and gating every fact question by day alone keeps every tell draw (T8). |
| W4 | **The papers menu is the same for everyone**: every on-request form of the day plans up to today, in first-appearance order, a request group once. `DocumentTemplateSO.askableBy`, `AskableForm.AskableBy` / `IsAskableBy` and `FormRequests.For(kind, …)` go. A traveller hands over the form they carry; asked for one they never carry, they refuse in character (V1). | Saleh's Q3 ("one papers menu of every form met so far"). "Up to today" grows the menu as new kinds arrive (the displaced's two forms from day 5), never offers a form before its kind has appeared, and never takes an entry away on a later day with a smaller mix. "< Back" and five requests is six of the wheel's eight. |
| W5 | **Every (kind, request) pair the menu offers a kind that never carries the form needs a default `Honest` missing-form line**: `FormRequests.ReplyProblems`, the same rule, fed each day's menu. First cut: the displaced asked for a Manifest, a Waiver or a Proof of means, and each 2150 kind asked for an Intake Declaration or a Return Order (§3.3). | The existing rule and data path; the voices' refusals override them in character. |
| W6 | **What may still differ between travellers**: a premade's own dialog entry (an authored story beat, rule 4), which garments "Look >" lists (the traveller's own clothes), and the steps checklist's set once the arrival paper has been read (B5; the desk's procedure per kind, traveller types §5.1: "different ... processing rules but same mechanics"). | Rule 3 governs types; a story beat and a traveller's own body are not a type's interaction, and the checklist is the clerk's paperwork, not the wheel. |
| V1 | **Eight voice slots, each the reply to one verb or moment**: the claim, the hand-over, a missing form, a spoken request, an answer, small talk, the slip, the verdict reaction (§4.1). The desk's lines (the opener, the prompts, the requests' words) have no personality. | Every line a traveller says, and nothing the desk says (W1). |
| V2 | **Defaults stay where they are** (`interview.claims`, `requestReply`, `missingFormReplies`, `requests[].reply`, the questions' answers and overrides, the places' and eras' small talk). **Personality and premade lines are extra rows under `interview.voices`**, one list per slot, each naming a personality or a premade. New defaults: `interview.reactions`, `interview.slips`, `interview.kindSmallTalk`. A voice with no matching row says the default. | No migration and no second home for today's lines; Saleh authors a row at a time. |
| V3 | **One resolution rule for every slot** (`Voices`, §4.3): a premade's own rows first; else, among the slot's rows for the traveller's personality whose kinds (blank: any) and era (blank: any) match, the most specific tier wins (a named reason or lie 4, named kinds 2, a named era 1, summed); several rows in the tier make a pool (V4); no matching row: the defaults, chosen by the same rule. The questions' overrides use the same matcher (`ContextMatch`, a small Domain helper scoring a row's kinds and era). | One rule an author can predict ("say more about who, and the line is theirs"), shared by the overrides (reuse). |
| V4 | **A line's pick is a value, never a draw**: `pool[(uint)Seeds.Mix(dialogSeed, Seeds.OfKey(slotKey)) % count]`, the slot key naming the verb ("claim", "answer:q_currency", "missing:TC-310:Honest", ...); `Seeds.OfKey` folds `Seeds.Mix` over the key's characters (stable in every runtime, unlike `string.GetHashCode`). The dialog stream's one draw (small talk) becomes such a value; `Interview.PickSmallTalk` and `CaseFactory`'s dialog stream go, and `Seeds.ForDialog` stays as the seed. | Adding a line to one slot never moves another slot's pick, and a replay says the same thing (`FormSerials`' precedent: a value, never a draw). |
| V5 | **Small talk draws on three sources** (Saleh's Q4): the personality's lines (their best tier), the home's lines (a displaced person's claimed place, else its era; a 2150 citizen's present place, else the Future era: "The maglev was late again. Some things never change.") and the kind's lines (`interview.kindSmallTalk`, by kinds and era). A source is picked first by the weights `interview.smallTalkWeights` (personality, home, kind; first cut 1, 1, 1; a source with no line is skipped), then a line within it, both as values (slot keys `smalltalk:source`, `smalltalk`). A premade says their own small talk only (PS3). The desk still asks everyone "How is life back home?". | His answer: "everything: personality, home, type". Picking the source first keeps the mix a knob Saleh sets in the sheet, whatever the line counts. Today a tourist bound for Athens recites Athens' small talk; the one "home" rule fixes it. |
| V6 | *(Retired by Saleh's Q5: there is no banner; B1-B3.)* | |
| V7 | **Tokens per slot**: a claim must hold `{place}`; an answer must hold `{value}` (the canonical value, as today) and may hold `{place}`; a hand-over or missing-form line may hold `{document}` (the request's label) and `{place}`; every other line (small talk, the slip, a reaction's lines, a spoken request's reply) may hold `{place}`; nothing else. | The fills the resolver has; the answer stays compare-clickable; the key-word slots (place, name, document) keep the fills English untranslated. |
| V8 | **Every voice line passes today's line checks** (not blank; worst case within `interview.maxLineChars`, 100; known references) **and a fact guard**: Generate World and the validator warn when a line contains a checkable value (any place's or the present's fact value, a transponder model, an employer), and refuse it in a slip line (T11). | A voice line is never evidence (T5), and a fact written into a line goes stale when history edits the facts. |
| V9 | **Translation and key words are unchanged**: a displaced person's lines, personality, premade and slip lines included, show in their tongue with the key words in English until a Speech translator flips them; a 2150 citizen speaks English. | Piece 9 and traveller types I3-I4. Untranslated, a Curt displaced man still says "{place}. Home. ▯▯▯." |
| V10 | **Narrative dialogs keep one authored version for everyone** (Saleh's Q7). | His answer. A dialog is a written scene with branches and effects; variants per personality would multiply a branching script by seven. |
| T1 | **The personality is independent of every fault by construction**: its own stream, weights that name no kind and no fault, and no rule that reads it (the lie roll, the fault order, the account, the look, the slip roll and the verdict never do). | No personality is more likely among liars: the brief's "a personality must never become a tell by itself". |
| T2 | **Before the stamp, a line depends only on what the desk can see** (the voice, the kind as presented, the claimed era, the verb, whether the traveller holds the requested form), **with one designed exception: the slip** (T9). `VoiceContext` holds the kind and the claimed era only, pinned by a test; the slip is a separate line on the case, never a context. | A line chosen by the hidden truth would be a tell; the slip is the one tell Saleh asked for, kept apart so it is the only one. |
| T3 | **A liar's tell lives in values**: a printed field, an answer's `{value}`, a worn garment. An answer's sentence is the voice's for that question, the same whether the value is a tell or not (tested). The slip hints; it never carries a value (T11). | The tell stays where the evidence rules prove it (papers prove, answers hint: traveller types L4); the sentence around it is flavour. |
| T4 | **No personality is a tell** (Q1, Q2, Q9): every personality is as common among liars as among the honest, so the nervous, the grand and the chatty are exactly as likely to be honest. **The slip is the one soft tell** (Saleh's Q2): a line only a liar says, rarely, spread evenly over the personalities. | His answer to Q2 keeps the lesson that papers prove and people only hint, and gives the attentive listener a reason to look closer. |
| T5 | **Voice lines never confess and never state a checkable fact**: no line says "my papers are forged", "I'm not really Premium" or "I'm from somewhere else"; none names a fact value (V8) or the traveller's own numbers (debt, wage, class, transponder, dates). A personality may sound suspicious in general (Anxious: "Is that allowed?"), because every traveller of it does. | A confession-shaped line would be a tell spoken by the innocent too, and it would teach players to deny on words, which the evidence gate punishes. |
| T6 | **The one fault-shaped context before the stamp, besides the slip, is a missing form's variant**: a traveller who should carry a form and left it out answers with the `Missing` variant ("I... didn't get round to that one."). | It reads the absence, which the desk already sees (the form is not handed over) and which is itself the directive fault, read without evidence. |
| T7 | **After the stamp, the reaction may show intent** (§6). | The verdict slip shows CORRECT or WRONG at the same moment, so the reaction tells nothing the verdict slip does not. |
| T8 | **The one wheel keeps every tell draw**: the displaced's answer-tell categories keep today's order on days 5-6, and a citizen's smuggling planner keeps only Currency and Technology before any draw (`Lies.Plan`'s `Keep`), so the citizens' three new questions change nothing it draws. | The golden cases' tells, papers and names must stay byte-identical (§8). |
| T9 | **Who slips** (Saleh's Q2): a generated traveller with Lying intent (a place lie, smuggling included, or a record lie: `IsLiar` or `IsForger`) rolls once, after their lie is planned, on a new salted stream `Seeds.ForSlip(caseSeed)` ("SLIP", `0x534C4950`) against the day's **`slipChance`** (`days[].slipChance`, sheet data Saleh tunes per day; first cut 0.15 on every day). Honest travellers never roll and never slip. A liar premade slips when their slip line is authored (no roll, rule 4); an honest premade has none. The roll reads nothing but the chance, and nothing reads the roll but the slip. | "Rare", and a knob in the sheet, per day like the other chances (`costumeErrorChance`), so a day can teach it. Its own stream keeps every other draw where it is. At 0.15 and about half the citizens lying, roughly one traveller in thirteen slips. |
| T10 | **Where a slip is said**: once, as a second line right after the traveller's small-talk reply, in the bubble and the transcript. It is a plain traveller line: never an answer (not compare-clickable), never logged, never evidence, never a citation's reason. The line: the voice's slip rows, optionally narrowed by the lie kind (`lie`, score 4), else the default rows (`interview.slips`, by lie kind). | Small talk is the wheel's one free conversation; a slip there rewards talking to people and stays a hint, where a slip in the claim would be an unmissable flag. "Never a proof": the evidence gate still needs a logged deviation. |
| T11 | **What a slip may say**: it points at the area of the lie (the luggage, the class, the name, home) with a slip of the tongue, an over-denial or a too-specific protest, and never names a checkable value, never confesses outright, never names the true home. The fact guard refuses a slip that holds a fact value (V8). | A hint that prompts a closer look, never a proof (Q2): the player still has to find the tell on the papers, in an answer or in the dress. |
| R1 | **A traveller reacts to the stamp in one or two lines, in their voice** (Saleh's Q6), in the bubble and the transcript. A reaction row holds `text` and an optional `then` (the second line). | His answer: "there should be personality in it, they will say one or two lines". |
| R2 | **The reaction's context is the verdict** (Accepted, Denied) **and the intent** (Lying: a place lie, smuggling included, or a record lie; Honest otherwise, directive faults and costume errors included, since those travellers do not know), optionally **the fault reason** (`Faults`: forged, disguised, smuggled, closed, wrongDate, expired, panic), the kind and the era. | Q6: by verdict and intent. Four base reactions per personality cover every verdict; a reason lets a line fit its catch. |
| R3 | **Every personality of the cast (weight above 0) must author its four base reactions** (Accepted and Denied × Honest and Lying; the validator refuses a gap), and every premade the pair of its intent. The defaults in `interview.reactions` stay as the safety net (the validator requires the four). | Q6: the reaction is in the personality's voice, so a personality without one is unfinished. |
| R4 | **The rules move nothing**: the decision, the score, the booth phase (no traveller at the desk), the verdict slip and the day flow are today's. Only the figure and the bubble linger: the reaction's lines play, then the figure stays `DeskConfigSO.reactionSeconds` after the last is fully shown (first cut 2.5 s; 0 leaves at once, today's behaviour); calling the next traveller ends the linger. | Presentation only, so no rule, save or clock changes; a knob Saleh edits in the Inspector on `Desk_Default.asset`, which Generate World never writes (rule 4). |
| R5 | **A reaction never changes a score, a penalty or history.** | Rule 1. |
| C1 | **The content is rows** (§9): 12 new sheets, 5 changed; `ContentSheetMap` entries for every new field; the template rewritten. | The spreadsheet is where Saleh authors and balances lines (traveller types CS1). |
| C2 | **Authoring**: the defaults make every voice work with no rows (V2). The first cut authors, per personality, the core a player hears on every traveller: 2 claims (a citizen line and a displaced line), the hand-over, the rich tourist's waiver and proof refusals, 2 small-talk lines (citizen, displaced), 2 slips, the 4 base reactions (R3): 14 lines, 98 for the cast. Then the answers, spoken requests, remaining refusals and reason reactions, and every premade's lines (§4.5), in the plan's content phase. | The lines heard on every traveller first; the rest where they pay. |
| C3 | **One validation rule, `VoiceChecks.Problems` (Domain)**, shared by Generate World and the validator, as `Interview.ClaimProblems` is (§9.2). | One rule, two callers: the house pattern. |
| C4 | **The validator reports each personality's coverage once** (the slots that fall back to the default), as an info line, never a warning. | Saleh sees what is left to author without the log filling with warnings. |
| M1 | **Save version stays 2**: nothing persisted changes; the personality and the slip are regenerated with the day from their seeds. `CaseVerdict` is not saved. | Additive. |

## 2. The personalities

### 2.1 The cast

Each is a way of being a person at a desk in 2150, not a job, a wealth or a kind. The ids are the sheet's.

| Id | Name | In one line | How they talk | What they talk about | Never |
|---|---|---|---|---|---|
| `chatty` | Chatty | Tells you everything, then a bit more. | Long, warm, run-on; commas, "well", "long story"; always gets to the answer. | relatives, exes, neighbours, their therapist, the trip's plans | a number, a secret, a pause |
| `curt` | Curt | Time is money, and in 2150 the queue bills by the minute. | One to four words; full stops; no pleasantries. | nothing, efficiently | rudeness beyond brevity; an unanswered question |
| `anxious` | Anxious | Worried about paradoxes, transponders, butterflies and you. | Questions back; repeats itself; "is that allowed?" | what could go wrong in the past, the fine print, sneezing near history | a confession; a line only a liar would say, outside a slip (T4, T5) |
| `sunny` | Sunny | Believes every word the agency prints. | Exclamation marks; quotes the slogans ("A FRESH START!"); sees the bright side of foreclosure. | the brochure, Debt Relief, "attendance required", the lovely forms | irony (Sunny means it) |
| `grand` | Grand | Treats the desk as staff, whatever their account says. | Haughty, declarative; "my people", "my lawyer"; demands, never asks. | status, views, the better class of everything | asking nicely |
| `stickler` | Stickler | Knows the regulations better than the clerk and says so. | Precise, formal; cites notices and form numbers; corrects wording. | the procedure, the typo on your sign, the correct way to hold a form | a wrong statement of a real Directive (it cites real rules correctly or invents absurd sub-clauses, never contradicts one) |
| `glum` | Glum | The debt won years ago; this is just the paperwork. | Flat, dry, short; deadpan comparisons. | debt, lateness, the inevitable | hope, except as a joke |

### 2.2 Across the kinds

The same person in each kind (rule 2: the personality drives, the kind is context). Every personality appears in every kind at the same weight (PS2).

| | Rich tourist | Poor tourist | Labourer (from plan phase 9) | Displaced |
|---|---|---|---|---|
| Chatty | the anniversary trip, the ex | the trip saved up for, the cousin who went Economy | the family the debt came from | the goat, the neighbours, four days in the intake hall |
| Curt | bored by leisure | counting the minutes it bills | the contract is the contract | wants home, now |
| Anxious | Premium never fails, does it? | the cheap transponder | the term, the waiver, the kin | will anyone remember them |
| Sunny | the brochure's sunset | "Holiday credit is freedom!" | "CLEAR YOUR DEBT! What a slogan!" | loves 2150's leaflets, can't wait to tell home |
| Grand | the view, the peasants | Grand on credit | a foreman in spirit | was somebody there |
| Stickler | the correct visa class | every form in order | read the contract, found the typo | has read all the agency's forms while waiting |
| Glum | even the past is overbooked | the waiver is the only sure thing | "So am I" (in the contract) | pulled from their time, then queued |

## 3. One wheel for every traveller, and no claim in print

### 3.1 The wheel, before and after

| Menu | Today (per kind) | After (the same for every traveller of a day) |
|---|---|---|
| Hub | the papers entry; the spoken requests; "Ask about home >" (displaced) or "Ask about the trip >" (citizens); "Look >"; today's dialogs | the papers entry; the spoken requests; "Ask about the trip >"; "Look >"; today's dialogs (a premade's own dialog while that premade stands, W6) |
| Papers menu | citizens: Departure Manifest, Stranding Waiver, Proof of means; displaced: Intake Declaration, Return Order | every on-request form of the days so far: days 1-4 Departure Manifest, Stranding Waiver, Proof of means; from day 5 also Intake Declaration, Return Order |
| Ask menu | displaced: Currency, Language, Device (day 1), Capital, Ruler (day 5); citizens: Currency, Device (day 4); Date of birth with the upgrade; small talk | everyone: Currency, Device (day 4), Language, Capital, Ruler (day 5), Date of birth with the upgrade, small talk |
| Look menu | the traveller's garments | unchanged (the traveller's own garments) |

Capacity (`DialogChecks.MenuProblems`, unchanged): the hub's worst case stays 8; the papers menu is 1 + 5 = 6; the ask menu 1 + 6 + 1 = 8.

### 3.2 The questions

The desk's prompt is the question's own, the same for everyone. The default answer is a 2150 citizen's; the displaced's override (`kinds: [Displaced]`) is today's home answer, so every sentence the displaced say today stays. Only the displaced's prompts change.

| Id | Label | The desk asks (everyone) | Default answer (citizens) | Override answers | From day |
|---|---|---|---|---|---|
| `q_currency` | Currency | "What will you pay with in {place}?" | "I've changed my money into {value}." (today's) | Displaced: "We pay in {value}."; Displaced in the ancient era: "We trade with {value}." | 4 |
| `q_language` | Language | "What language will you speak in {place}?" | "I've been practising my {value}." | Displaced: "At home we speak {value}." | 5 |
| `q_device` | Device | "What are you taking with you?" | "Just the approved kit: {value}." (today's) | Displaced: "Every day I use {value}." | 4 |
| `q_capital` | Capital | "Which city in {place} are you headed for?" | "{value}, like the brochure says." | Displaced: "Our capital is {value}." | 5 |
| `q_ruler` | Ruler | "Who rules {place} these days?" | "{value}, I'm told." | Displaced: "We are ruled by {value}." | 5 |
| `q_born` | Date of birth | "When were you born?" | "I was born on {value}." | none | the upgrade |

`q_trip_currency` and `q_trip_device` merge into `q_currency` and `q_device`. Every answer's value is the claim's fact as today (`CaseFactory.ResolveFieldValue`), so a citizen's Language, Capital and Ruler are the destination's: honest flavour, never a tell (a citizen's only spoken tell is smuggling, Currency and Technology, T8). The day-4 and day-5 announcements say "every traveller" instead of "departing citizens"; Language gets a day-5 announcement.

### 3.3 The papers menu and the new default replies

| Day | The menu (everyone) | Who carries what |
|---|---|---|
| 1-4 | Departure Manifest, Stranding Waiver, Proof of means | rich: Manifest; poor: all three; labourer (phase 9): Manifest, Waiver |
| 5-6 | + Intake Declaration, Return Order | displaced: Intake Declaration, Return Order |

New default `Honest` lines (`interview.missingFormReplies`, one row per kind, as today); each voice refuses in character on top of them (§4.4):

| Kind | Request | Line |
|---|---|---|
| Displaced | TC-230 | "A manifest? I was pulled out of my own time. I didn't pack." |
| Displaced | TC-310 | "A waiver? Nobody asked me anything before the sky opened." |
| Displaced | proof | "Means? I have what was in my pockets when the sky opened." |
| RichTourist, PoorTourist, Labourer (a row each) | TC-620 | "An intake declaration? I'm leaving, not arriving." |
| RichTourist, PoorTourist, Labourer (a row each) | TC-630 | "A return order? I have a return booking. Is that the same thing?" |

### 3.4 What stays different

A premade's own dialog entry, the garments a "Look >" lists, and the steps checklist's set once the arrival paper has been read (W6, B5). Everything else on the wheel is identical for every traveller of the day.

### 3.5 Where the claim is shown today, and what replaces it (B1-B5)

Found by searching the code for `claim.banner`, `claimLine`, `claimText`, `SetClaim`, `claimSummary`, the steps and search:

| Place (code) | Shows today | After |
|---|---|---|
| The Investigation app's case header (`InvestigationApp.claimText`, filled by `BeginCase(claim, …)` from `InvestigationUIController.ShowRich`: `UiText.Format("claim.banner", visitorDisplayName, claimLine)`; built as `ClaimText` by `OfficeSceneUIBuilder.BuildAppHeader`) | the name and role, then the claim in quotes; "Waiting for the next traveller" between cases | removed; the counters row fills the header and shows the idle line between cases; Accept and Deny unchanged (B1, B2) |
| The office case HUD's claim tag (`OfficeCaseHud.SetClaim`, `claimRoot`, `claimText`; built as `ClaimStrip` by `OfficeSceneUIBuilder.BuildOfficeCaseHud`) | the same text, top centre of the office view while the PC is closed | removed; the compare strip moves up into its place (B1, B2) |
| The string `claim.banner` ("{0}\n\"{1}\"") | the format of both | removed from `ui.strings` (B1) |
| `CaseInstance.claimLine` (`CaseFactory`: `Interview.Claim(kind, place)`) | the banner's sentence | removed; the spoken claim is resolved by the voice (B3) |
| `CaseVerdict.claimSummary` (`ShiftScoring.ResolveDecision`) | the claim in the verdict record, read only by the audit play's dump | removed (B3) |
| The window title (`app.titleCase`: "Investigation · {0}", the traveller's name and role) | who stands at the desk | kept: it names the person, not their request (B2) |
| The transcript's `case.claim` line and the bubble on arrival (`InterviewScript.Opening`) | the kind's claim sentence | the only claim, in the voice's words (B3, V1) |
| Search (phase 19's case layer: each transcript line as it is spoken) | the claim as a transcript line | unchanged: the spoken claim is found as a transcript line (B3) |
| The Reference (`DayReference.SetClaim`, `ReferenceView.SetClaim`, `book.claimedLabel` "(claimed)", `app.ref.claimedOnly`, `SmartLinks`' `CaseClaim`, `app.link.reference`, the steps' `CostumeClaimed`) | the claimed place's row first and marked; the filter on at each case; links to that row | kept (B4; §12 Q11) |
| The steps checklist (`StepsPanel.BeginCase` → `CaseSteps.Resolve(sets, kind.ToString(), day)`) | the kind's set from arrival | the `default` set until the arrival paper is read, then the kind's (B5) |

## 4. Voice lines

### 4.1 The slots

Every sheet below takes a `personality` or a `premade` (exactly one) and optional `kinds` and `era`.

| Slot | Said when | Key (the verb) | Tokens | Default (where it lives) | Sheet |
|---|---|---|---|---|---|
| Claim | stepping up (the transcript's second line); the only claim (B3) | none | `{place}` required | `interview.claims` (the kind's) | `voiceClaims` |
| Hand-over | handing a form over | the request (optional: a form number or a group id) | `{document}`, `{place}` | `interview.requestReply` | `voiceHandOver` |
| Missing form | asked for a form they do not carry: a refusal in character | the request (required); the variant (Honest, Missing) | `{document}`, `{place}` | `interview.missingFormReplies` (kind, request, variant) | `voiceMissingForms` |
| Spoken request | "Step closer", "Speak up" | the request id (required) | `{place}` | `interview.requests[].reply` | `voiceSpoken` |
| Answer | asked a question | the question id (required) | `{value}` required, `{place}` | the question's override for the kind and era, else its answer (§3.2) | `voiceAnswers` |
| Small talk | "Small talk" | none | `{place}` | the home's lines and `interview.kindSmallTalk`, the three sources weighted (V5) | `voiceSmallTalk` |
| Slip | after the small-talk reply, for a liar who slipped (T9, T10) | the lie kind (optional) | `{place}` | `interview.slips` (by lie kind) | `voiceSlips` |
| Reaction | the stamp lands: one or two lines | the verdict and the intent (required); the fault reason (optional) | `{place}` in both lines | `interview.reactions` (verdict, intent, reason, kinds, era) | `voiceReactions` |

### 4.2 The data

```json
"days": [ { "day": 1, "...": "today's fields, then:", "slipChance": 0.15 } ],
"personalities": [
  { "id": "curt", "name": "Curt", "weight": 1, "note": "Time is money, and in 2150 the queue bills by the minute." }
],
"interview": {
  "...": "today's fields, unchanged but tripAskLabel, then:",
  "smallTalkWeights": { "personality": 1, "home": 1, "kind": 1 },
  "kindSmallTalk": [
    { "kinds": ["RichTourist"], "text": "We do three eras a year. The children prefer the ones with castles." },
    { "kinds": ["Displaced"], "text": "Everything in this century hums. Even the walls." }
  ],
  "slips": [
    { "lie": "Smuggling", "text": "Nothing from home in my luggage. Home meaning here. Obviously." },
    { "lie": "PoorPosingAsRich", "text": "Premium, yes. We Premiums always... what is it we always do?" }
  ],
  "reactions": [
    { "verdict": "Accepted", "intent": "Honest", "text": "Thank you!" },
    { "verdict": "Accepted", "intent": "Honest", "kinds": ["Displaced"], "text": "Thank you. Home, at last." },
    { "verdict": "Denied", "intent": "Honest", "text": "But... I did everything right." },
    { "verdict": "Denied", "intent": "Honest", "kinds": ["Displaced"], "text": "Then how do I get home?" },
    { "verdict": "Accepted", "intent": "Lying", "text": "Thank you. Thank you very much." },
    { "verdict": "Denied", "intent": "Lying", "text": "Worth a try." }
  ],
  "voices": {
    "claims":       [ { "personality": "curt", "kinds": ["Displaced"], "text": "{place}. Home. Now." },
                      { "premade": "socrates", "text": "Send me home to {place}. But first: what is a home?" } ],
    "handOver":     [ { "personality": "curt", "text": "Here." } ],
    "missingForms": [ { "personality": "grand", "kinds": ["RichTourist"], "request": "TC-310", "variant": "Honest", "text": "A waiver? My transponder has people for that." } ],
    "spoken":       [ { "personality": "grand", "request": "step_closer", "text": "I don't step. I proceed." } ],
    "answers":      [ { "personality": "glum", "question": "q_currency", "text": "{value}. Not that it'll be enough." } ],
    "smallTalk":    [ { "personality": "glum", "kinds": ["RichTourist", "PoorTourist", "Labourer"], "text": "The maglev was late. So was my pay. So, apparently, was my birth." } ],
    "slips":        [ { "personality": "glum", "text": "If this goes wrong, tell my creditors I tried." } ],
    "reactions":    [ { "personality": "glum", "verdict": "Denied", "intent": "Lying", "text": "Figures.", "then": "Same time tomorrow, then." } ]
  }
}
```

`kinds`, `era`, a hand-over's `request`, `lie`, `reason`, `then` and the unused one of `personality` / `premade` are left out of the JSON when blank. Generate World writes the cast into `ContentLibrarySO` (`personalities`), the rows into `InterviewLines` (`kindSmallTalk`, `slips`, `reactions`, and `voices`: a `VoiceBook`, one list per slot of one flat serializable row, `VoiceLine`: personality, premade, kinds, era, key, variant, lie, verdict, intent, line, then) and the weights into `InterviewLines.smallTalkWeights`; `DayPlanSO` gains `slipChance`. `ReactionVerdict` and `ReactionIntent` are new serialized enums, append-only and pinned. Line ids follow the generator's grammar: `interview.reactions.{n}`, `interview.slips.{n}`, `interview.kindSmallTalk.{n}`, `interview.voices.{list}.{personality or premade}.{n}` (a reaction's second line `.then`).

### 4.3 The resolver

Pure Domain (`Voices`), tested headless. For one slot, one verb and one traveller (the personality id or the premade id, the `VoiceContext` of kind and claimed era, the dialog seed):

1. **Match.** A row matches when it names the traveller's premade (for a premade) or personality (for anyone else), its kinds are blank or hold the kind, its era is blank or is the claimed era, and its keys equal the verb's (the request, the question, the variant; for a slip a lie kind that is blank or the traveller's; for a reaction the verdict and the intent, and a reason that is blank or the traveller's fault reason).
2. **Tier.** Score each match: a named reason or lie kind 4, named kinds 2, a named era 1. The pool is the matches with the highest score.
3. **Fall back.** An empty pool takes the default rows through the same two steps (the defaults of §4.1). For an answer, the default is one line: the question's best-scoring override (a tie keeps the first listed), else its answer.
4. **Small talk** (V5): a source is picked first among those with a line (the personality's tier, the home's lines, the kind's best tier) by `interview.smallTalkWeights`, as a value (`smalltalk:source`); then a line of that source. A premade's own small talk is their only source.
5. **Pick.** One line of the pool, as a value: `pool[(uint)Seeds.Mix(dialogSeed, Seeds.OfKey(slotKey)) % pool.Count]`. Slot keys: `claim`, `handover:{request}`, `missing:{request}:{variant}`, `spoken:{id}`, `answer:{question id}`, `smalltalk:source`, `smalltalk`, `slip`, `reaction:{verdict}:{intent}`.
6. **Fill.** `Interview.Fill` the tokens (`{place}` the claimed place's label, `{value}` the canonical value, `{document}` the request's label) and take the key-word spans over the template and its fills (`KeyWords.Spans`), as every traveller line does today.

With no voice rows at all, every line is today's, except small talk (V4, V5) and the new slots (slip, reaction), which fall to their defaults.

### 4.4 Worked example: one verb, seven replies

A Premium tourist bound for Periclean Athens is asked for a Stranding Waiver on day 1 (the rich tourist's `Honest` missing form). The desk says the same words to all of them: "Your Stranding Waiver, please."

| Personality | Reply |
|---|---|
| (default) | "It's a Premium unit, I don't need one." |
| Chatty | "No waiver, it's the Premium unit! My cousin went Economy once. We don't talk about him." |
| Curt | "Premium. No." |
| Anxious | "A waiver? Do I need one? It's Premium. Premium never fails. Does it?" |
| Sunny | "A waiver? Oh, it's Premium! Like the advert: travel without a care!" |
| Grand | "A waiver? My transponder has people for that." |
| Stickler | "Premium units are exempt from the {document}. It's on the back of the form." |
| Glum | "Premium unit. They never fail, I'm told. I'm told a lot of things." |

A poor citizen posing as rich (L1) presents as rich, so they say their personality's same line (T2). A displaced Grand asked for the same waiver on day 5 says their own Displaced line, or the default "A waiver? Nobody asked me anything before the sky opened."

### 4.5 The premades' own lines (Saleh's Q8)

Every premade is displaced and stands on day 6, so the slots each can reach are fixed. The plan's content phase authors, for each of the ten:

| Slot | Lines | Notes |
|---|---|---|
| Claim | 1 | holds `{place}` |
| Hand-over | 1 | for the Intake Declaration and the Return Order they carry |
| Missing forms (Honest) | 3 | TC-230, TC-310 and the proof of means: the day-6 menu's forms a displaced person never carries |
| Spoken requests | 2 | Step closer, Speak up |
| Answers | 6 | `q_currency`, `q_language`, `q_device`, `q_capital`, `q_ruler`, `q_born`; each holds `{value}` |
| Small talk | 1 | their only small-talk source (V5) |
| Reactions | 2 | the honest pair (Accepted · Honest, Denied · Honest); "Socrates" the Lying pair; each may add a `then` line |
| Slip | 1, "Socrates" only | said when he stands (T9: authored, no roll) |

Sixteen lines for an honest premade, seventeen for "Socrates". Their intros, record notes and Senenmut's dialog stay as authored. Examples: Senenmut's claim "Home to {place}. The Pharaoh's temple will not finish itself."; his `q_ruler` answer "{value}. May she live forever, and read my plans."; "Socrates"' claim "Send me home to {place}. But first: what is a home?", his slip "I know that I know nothing. Especially about where I was born.", his Denied · Lying reaction "You ask good questions." then "I taught you that."; Gutenberg's small talk "My business in Strasbourg is private. It involves letters. Movable ones."

## 5. Personalities, slips and tells

The contract, and what holds each part of it:

| Part | Rule | Held by |
|---|---|---|
| Who gets which personality | one weighted draw on its own stream, the same weights for every kind; premades authored (PS2, PS3, T1) | `PersonalitiesTests` (one draw; the weights); `SeedsTests` (the salt, distinct); the plan's independence probe in Unity: days 1-6 × 20 seeds generated twice, the second with every liar and fault chance at 0 in memory, give every slot the same personality, and the report tabulates the cast among liars and among the honest |
| What a line may depend on before the stamp | the voice, the presented kind, the claimed era, the verb, whether the form is held (T2) | `VoicesTests.ContextHoldsOnlyWhatTheDeskSees` pins `VoiceContext`'s fields to the kind and the claimed era: adding a field fails the test, so a reviewer must read T2 |
| Where a liar's tell lives | in values; an answer's sentence is the same for a tell and for the truth (T3) | `InterviewScriptTests`: two cases alike but for one answer's tell give the same sentence around the value, and the same claim, replies and small-talk line |
| The slip, the one designed exception | a liar only, at the day's `slipChance`, on its own stream; after small talk; never a value, never evidence (T9-T11) | `SlipsTests` (only Lying intent rolls; one draw; the chance; a premade liar slips when authored); `SeedsTests` ("SLIP"); `InterviewScriptTests` (the slip follows the small-talk reply, is not an answer line); the fact guard as an error; the independence probe: the personalities are identical with slips on or off |
| What a line may say | never a confession, never a checkable fact, never the traveller's own numbers (T5, T11) | the fact guard (V8); the author's checklist (§7.4); review |
| The one fault-shaped reply | the `Missing` variant, which reads a visible absence (T6) | `FormRequestsTests`, `InterviewScriptTests` (the variant comes from the case, set by the paper-set fault in plan phase 9) |
| After the stamp | the reaction may show intent (T7) | `VoicesTests` (the reaction's context); R4's order: the verdict slip and the lines land together |

**Is any personality a tell? No** (T4): every personality is spread evenly over the honest and the lying, so an Anxious traveller is exactly as likely to be honest as a Curt one. The player who distrusts the nervous and waves through the charming is corrected by the evidence gate and the citations: papers prove, people only hint.

**The slip is the one tell Saleh asked for, and it only points.** A Chatty smuggler who slips says, after their small talk, "Nothing in the bag but socks! Lots of socks. Don't open the socks." Nothing in it can be logged: the player must now ask about the device, request the Manifest and compare its Declared Effects with the Index of Devices to find the tell. A slip never names the value or the true home, and an honest traveller never says one, so hearing one is a reason to look closer and never a reason to deny.

**A liar's personality and their tells.** The personality shapes the sentence around a tell, never the tell. A Glum smuggler asked about the currency says "Credits. Not that it'll be enough." while an honest Glum tourist says "Silver drachma (owl). Not that it'll be enough.": the same template, and only the value, compared against the Currency Ledger, catches the smuggler. Apart from a rare slip, a liar never breaks character and never says anything different until the stamp.

## 6. The verdict reaction

| | Honest (no lie; a directive fault or a costume error counts as honest intent) | Lying (a place lie, smuggling or a record lie) |
|---|---|---|
| **Accepted** | relief or joy, in character: Stickler "Stamped at a slight angle." then "I'll allow it. This once." | got away with it, in character: "Thank you, thank you!" then "Worth every credit I don't have!" |
| **Denied** | indignation or despair, in character: Grand "You'll hear from my lawyer." then "Well, my lawyer's lawyer. We share one." | caught, in character: Glum "Figures." then "Same time tomorrow, then." |

- **One or two lines** (R1): `text`, and `then` when the author wants a beat. Both lines follow the slot's checks (V7, V8).
- A reason row narrows a line to its catch: an honest traveller denied for a closed destination (`closed`: Sunny "Closed? How exciting!" then "Like a secret!"), a costume error (`panic`: Anxious "What's wrong with my outfit?" then "Is it the hat? It's the hat, isn't it."), a smuggler caught (`smuggled`: Grand "Fine. Keep it." then "I have three more at home.").
- **Sequence** (R4): the stamp lands; `GameManager.HandleDecision` scores and records exactly as today (the booth phase goes to "no traveller" at once, so the wheel cannot open); the reaction's lines (resolved from the case: the intent from `IsLiar` or `IsForger`, the reason from `FaultReason`) are appended to the transcript and said in the bubble in turn; the verdict slip shows as today (a citation still pauses the clock); the figure and the bubble stay until `DeskConfigSO.reactionSeconds` after the last line is fully shown, then the traveller leaves; calling the next traveller first ends it at once.
- Decided on the PC (the office view hidden): the lines reach the transcript (the Transcript tab shows them and search indexes them) and the bubble if the office view shows.
- A displaced person's reaction is in their tongue with its key words, as every line of theirs (V9).

## 7. Tone guide

### 7.1 Principles

1. **Believable first.** Every line is something a person could say at a customs counter. The joke rides on who they are and where they live, never on nonsense.
2. **The world is absurd; the people are sincere.** The dystopia is played straight (the queue bills by the minute, debt passes to your kin, the agency has slogans); each person reacts to it in character. Sunny means it; Glum has stopped arguing.
3. **Short.** One breath. The worst case (the longest place label is 43 characters, the longest fact value 28) stays within 100 characters; a claim should stay near 70 so the bubble reads at a glance. A reaction that needs more takes its `then` line.
4. **Never evidence** (T5): no fact values (currencies, devices, languages, rulers, capitals, transponder models, employers), no own numbers (debt, wage, class, dates), no confession, no line that contradicts a Directive or the papers.
5. **The kind is context, not character.** Tourists are going on holiday, labourers to work off a debt, the displaced home; each personality meets that in its own way (a Sunny labourer is thrilled; a Glum tourist is unimpressed). The claim is now the only statement of it (B3), so a claim says where and why in the person's words.
6. **Same energy, honest or lying** (T3, T4). A personality never "breaks" to hint at a lie, except in a slip, which is written as one.
7. **Punch up.** The joke is on the agency, the debt economy, the adverts and the absurdity; never on poverty, and never cruel about the displaced's plight.
8. **In the world.** No real-world brands, no present-day pop culture, no profanity, no winks at the player, no mention of the game.
9. **Survive translation.** A displaced line keeps its key words (home, please, papers, yes, no, Temporal Customs, and the place) in English when untranslated; put the gist on one of them where you can ("{place}. Home. Now.").

### 7.2 The debt dystopia palette

- **Use:** Debt Relief departures ("A FRESH START", "CLEAR YOUR DEBT", "ATTENDANCE REQUIRED"); queues and forms billed by the minute; debt passing to your kin (the waiver's own row); frozen accounts and default; Premium families and Economy transponders; holiday credit; the payment plan on everything (shoes, the sofa, a sofa's cushions); the past as a resort brochure; the intake hall where the displaced wait; the maglev being late; the agency's leaflets and slogans.
- **Avoid:** real places the world does not hold (say "the past", an era, or `{place}`); anything a player could check against a paper or a book (T5); jokes about suicide, violence or illness; slurs of any era.

### 7.3 The cast in their own words (examples)

Each line is labelled with its slot and context, and each fits V7 and V8 with the longest fills. These are the first rows the plan authors (phase V2, and the reactions and slips in V3 and V4), the start of C2's core.

**Chatty**
- Claim, tourist: "{place}! Our anniversary. Well, mine. He left. Long story!"
- Claim, displaced: "Home to {place}. My goat's been alone for days. Or centuries?"
- Missing form (TC-310, Honest), rich tourist: "No waiver, it's the Premium unit! My cousin went Economy once. We don't talk about him."
- Answer (`q_device`), citizen: "Just the approved kit: {value}. My therapist says I overpack. Emotionally."
- Slip (Smuggling): "Nothing in the bag but socks! Lots of socks. Don't open the socks."
- Reaction, Accepted · Honest: "Thank you! I'll bring you something back." then "Not a plague. Something nice."

**Curt**
- Claim, citizen: "{place}. Leisure. Go."
- Claim, displaced: "{place}. Home. Now."
- Hand-over: "Here."
- Spoken (Speak up): "Said it once. That was billed."
- Slip (any lie): "Everything's in order. Don't check."
- Reaction, Denied · Honest: "Unbelievable." then "And I paid for the queue."

**Anxious**
- Claim, tourist: "{place}, please. Is it safe? They said it's safe. It's safe?"
- Missing form (TC-310, Honest), rich tourist: "A waiver? Do I need one? It's Premium. Premium never fails. Does it?"
- Small talk, citizen: "I read one sneeze in the past can delete a whole family tree. Is that true?"
- Answer (any fact question): "{value}. Is that right? I checked the list twice. Three times."
- Slip (any lie): "Is it normal to sweat this much when everything is completely legal?"
- Reaction, Accepted · Honest: "Oh, thank goodness." then "Now I can worry about the actual trip."

**Sunny**
- Claim, labourer: "Debt Relief to {place}! A fresh start! Like the brochure!"
- Small talk, citizen: "My whole block got frozen in default! Everyone's home all day now! So cosy!"
- Missing form (proof, Honest), displaced: "Proof of means? I have a smile and a leaflet! Attendance required!"
- Slip (PoorPosingAsRich): "Premium, like the advert! Well, like the advert for the credit card."
- Reaction, Denied · Honest: "Denied! That's fine!" then "Every no is a yes I haven't paid for yet!"

**Grand**
- Claim, poor tourist: "{place}. Somewhere with a view, and a better class of peasant."
- Claim, displaced: "Return me to {place} at once. I was somebody there."
- Missing form (TC-310, Honest), rich tourist: "A waiver? My transponder has people for that."
- Spoken (Step closer): "I don't step. I proceed."
- Slip (PoorPosingAsRich): "Premium, naturally. I simply enjoy queueing with ordinary people."
- Reaction, Denied · Honest: "You'll hear from my lawyer." then "Well, my lawyer's lawyer. We share one."

**Stickler**
- Claim, tourist: "One leisure departure to {place}, as the notice words it."
- Hand-over: "Face up, top edge first, as the sign above you requires."
- Missing form (TC-310, Honest), rich tourist: "Premium units are exempt from the {document}. It's on the back of the form."
- Small talk, displaced: "I've read all your forms. Four days in the intake hall. Two have typos."
- Slip (DoctoredIdentity): "Every detail on that visa is correct. I corrected them myself."
- Reaction, Accepted · Honest: "Stamped at a slight angle." then "I'll allow it. This once."

**Glum**
- Claim, labourer: "Debt Relief to {place}. It's all in the contract. So am I."
- Small talk, citizen: "The maglev was late. So was my pay. So, apparently, was my birth."
- Answer (`q_currency`): "{value}. Not that it'll be enough."
- Slip (any lie): "If this goes wrong, tell my creditors I tried."
- Reaction, Accepted · Honest: "Approved. Great." then "Now the hard part: everything else."
- Reaction, Denied · Lying: "Figures." then "Same time tomorrow, then."

**The kinds' small talk** (defaults, `interview.kindSmallTalk`): rich tourist "We do three eras a year. The children prefer the ones with castles."; poor tourist "Two years of saving, and the credit company paid the rest. So kind of them."; labourer "They say the past has fresh air. And fourteen-hour shifts."; displaced "Everything in this century hums. Even the walls."

**The default slips** (`interview.slips`, by lie kind): Smuggling "Nothing from home in my luggage. Home meaning here. Obviously."; PoorPosingAsRich "Premium, yes. We Premiums always... what is it we always do?"; DoctoredIdentity "My details? It's all on the visa. That visa. The one you're holding."; FalseOrigin "Home. Yes. {place}. I say it every morning so I don't forget."; FakeDisplaced "The sky opened and took me from the past. The very old past. Ancient-ish."

### 7.4 Before adding a row (the author's checklist)

1. Could a real person say it at this counter, in this mood? (principle 1)
2. Does it fit every kind and era its row allows? A blank-kinds line must suit a tourist and a displaced person alike.
3. Does it name a fact, a number of the traveller's, or imply a specific lie? Then rewrite it (T5; the fact guard warns about facts). A slip may point at the area of its lie kind, never at a value (T11).
4. Would an honest traveller of this personality say exactly this? It must be yes for every line before the stamp, except a slip, where it must be no.
5. Does it hold its required token (`{place}` in a claim, `{value}` in an answer) and fit 100 characters with the longest fill? (Generate World checks.)
6. Is the joke on the system rather than on the poor or the displaced? (principle 7)

## 8. Golden masters: what changes

The offline suite's golden values:

| Test | Change |
|---|---|
| `SeedsTests` | `PersonalitySalt` ("PRSN") and `SlipSalt` ("SLIP") pinned, in the one distinctness table; `Seeds.OfKey` pinned values |
| `CaseStepsTests` | the set before and after the arrival paper is read (B5) |
| `InterviewTests` | `AskLabel` and `PickSmallTalk` gone; the answer by kind and era; one prompt per question |
| `InterviewScriptTests` | the same hub, papers and ask menus for every kind; the voice lines; the slip after small talk; the reaction's one or two lines |
| `InterviewDayTests` | the day's questions for every kind; the forms of the days so far |
| `FormRequestsTests` | `askableBy` gone; the menu of the days so far; the replies over it |
| `SerializedEnumsTests` | `ReactionVerdict`, `ReactionIntent` pinned; the `askableBy` pin goes |
| `UiStringsTests` | `claim.banner` gone with its callers |
| `ContentSheetMapTests` | the round trip covers the new sheets (no test change; it fails until they are mapped) |

The Phase 0 golden masters (`docs/reviews/audit-baseline/golden`), re-packed once at the end of the plan (its phase V5), each phase's diff explained in its commit:

| File | Changes | Must stay byte-identical |
|---|---|---|
| `scene_OfficeGameplay.txt` | the case header's `ClaimText` and the HUD's `ClaimStrip` removed; the counters' and the compare strip's rects (V0) | every other object |
| `cases.txt` | `claimLine=` prints the spoken claim (identical until the cast's claim rows land); the WORLD `askable` / `answerTells` lines (one list per day, no per-kind repeats); every CASE line gains `personality=` after `gender=` and `slip=` (the slip line's id, or `-`); `smalltalk=` changes for every traveller (the value pick, V4; the three sources, V5; the cast's rows as they land); a citizen's `answers=` on days 5-6 gain Language, Geography and Politics (the destination's values) | the name, born, role, allowed, violator, liar, home, **tells**, papers, record, look, garments, premade, intro, tongue: every draw of the case, lie, look, account, fault and violator streams |
| `play_transcript.txt` | the verdict dumps lose `claimSummary`; the displaced's prompts (days 5-6); the day-4 and day-5 announcements; the papers menus of days 5-6 and their missing-form lines; every spoken line of a traveller whose voice has a row; the slips after small talk; one or two reaction lines after each verdict | every verdict's outcome, score, citation, ledger line, evidence count and the Home phase |
| `play_saves/*.json` | nothing (nothing persisted changes, M1) | all |
| `world_generate.txt`, `data_hashes.txt`, `validator.txt` | the string table (`claim.banner` gone), the library (the cast, the voices, the defaults, the weights), the day plans (`slipChance`), the merged question assets, the nine form templates (`askableBy` stripped), `Desk_Default.asset` (`reactionSeconds`); the validator's new checks and its coverage lines | every other asset |
| other scene dumps | nothing | all |
| profiles | nothing per frame (the resolver runs when a traveller steps up, at a wheel choice and at the stamp, never per frame) | the allocation sites, the per-frame GC |

`ContentSheets/TimeDesk_Content_Template.xlsx` is rewritten in each phase that changes the map.

## 9. Content sheets and validation

### 9.1 The sheets

| Sheet | JSON path | Columns | Notes |
|---|---|---|---|
| `personalities` (new, keyed by id) | `personalities` | `id` (required), `name`, `weight` (number), `note` | the cast; a weight of 0 benches a personality; `note` is for authors (the tone in a line), as `confusable.why` is for reviewers |
| `kindSmallTalk` (new) | `interview.kindSmallTalk` | `kinds` (required list), `era` (ref `eras`), `text` | the kind's small talk (V5) |
| `interviewSlips` (new) | `interview.slips` | `lie` (blank or a lie kind), `kinds`, `era`, `text` | the default slips; the validator demands one with a blank lie |
| `interviewReactions` (new) | `interview.reactions` | `verdict` (Accepted, Denied), `intent` (Honest, Lying), `reason` (blank or a fault reason), `kinds`, `era`, `text`, `then` | the defaults; the validator demands the four verdict × intent rows with blank reason, kinds and era |
| `voiceClaims` (new) | `interview.voices.claims` | `personality` or `premade` (exactly one; refs), `kinds`, `era`, `text` | |
| `voiceHandOver` (new) | `interview.voices.handOver` | the voice, `request` (blank: any), `kinds`, `era`, `text` | |
| `voiceMissingForms` (new) | `interview.voices.missingForms` | the voice, `request` (required), `variant` (Honest, Missing), `kinds`, `era`, `text` | refusals in character |
| `voiceSpoken` (new) | `interview.voices.spoken` | the voice, `request` (ref `interviewRequests`), `kinds`, `era`, `text` | |
| `voiceAnswers` (new) | `interview.voices.answers` | the voice, `question` (ref `questions`), `kinds`, `era`, `text` | |
| `voiceSmallTalk` (new) | `interview.voices.smallTalk` | the voice, `kinds`, `era`, `text` | |
| `voiceSlips` (new) | `interview.voices.slips` | the voice, `lie`, `kinds`, `era`, `text` | |
| `voiceReactions` (new) | `interview.voices.reactions` | the voice, `verdict`, `intent`, `reason`, `kinds`, `era`, `text`, `then` | |
| `days` (changed) | `days` | + `slipChance` (number) | T9's knob |
| `questions` (changed) | `questions` | − `kinds` | one question per category |
| `questionOverrides` (changed) | `questions[].overrides` | `era` (blank: any), + `kinds`, − `prompt`, `answer` | an answer by kind and era |
| `interview` (changed) | `interview` | − `tripAskLabel`; + `smallTalkWeights.personality`, `.home`, `.kind` | `askLabel` is everyone's; V5's knob |
| `uiStrings` (changed) | `ui.strings` | − the row `claim.banner` | B1 |
| `premades` (unchanged) | `premades` | no personality column: a premade's voice is its own rows (PS3) | |

82 sheets become 94. `docs/CONTENT_SHEETS.md`'s "Dialogue and interactions" line gains the new sheets.

### 9.2 The checks (`VoiceChecks.Problems` and `Personalities.Problems`, called by Generate World and the validator)

- The cast: ids unique and not blank; weights at least 0; at least one positive when the list is not empty; names not blank.
- Every voice row: exactly one of a listed personality or a listed premade; known kinds; a known era; its keys valid for its slot (a request that is a form number or a group id of some day's forms; a spoken request of `interview.requests`; a question of `questions`; a lie kind; a reason of `Faults`); text (and `then`) not blank where present; only its slot's tokens, the required one present (V7); its worst case within `interview.maxLineChars`.
- The defaults: the four reactions, one slip with a blank lie, one kind small-talk line per kind in play.
- The cast's reactions: every personality with a weight above 0 has its four base reactions (R3).
- The premades (from plan phase V5, when their lines land): every premade has a line for every slot of §4.5; a slip only for a premade that lies.
- `days[].slipChance` within 0 and 1; `interview.smallTalkWeights` each at least 0, at least one positive.
- Refused: a slip holding a fact value, a transponder model or an employer (T11). Warned: any other line holding one (V8); a row identical to another in every column.
- Info: each personality's coverage (the slots that fall back to a default), one line per personality (C4).
- The existing rules, fed the new shapes: `InterviewQuestions.Problems` (one question per category), `FormRequests.ReplyProblems` over each day's menu and the kinds in play that day, `DialogChecks.MenuProblems` with the most requests any day offers.

## 10. `docs/FEATURES.md` lines that change

| Line (today) | Change | Phase |
|---|---|---|
| 11 Deterministic case generation | + the personality and slip streams (one draw each; the slip for liars only); the dialog seed feeds the line picks as values | V2, V4 |
| 35 Traveller wheel | the same entries for every traveller of a day | V1 |
| 36 Speech bubble | + the slip after small talk; + the reaction's one or two lines after the stamp; the figure lingers `reactionSeconds` | V3, V4 |
| 38 The office case HUD | no claim tag: only the compare strip | V0 |
| 46 Investigation app | the case header holds the counters and Accept and Deny, no claim | V0 |
| 48 Steps | the default set until the arrival paper is read, then the kind's | V0 |
| 71, 73 Kinds, premades | + every generated traveller has a personality, independent of the kind; a premade speaks its own lines | V2 |
| 77 Claim banner | removed: the claim is spoken only, in the traveller's words | V0, V2 |
| 78 Traveller wheel = the interview | one papers menu (every on-request form of the days so far) and one ask entry for everyone; refusals by kind, variant and voice | V1, V2 |
| 79 Questions | one question per category for every traveller, in the same words; the day ramp of §3.2 | V1 |
| 80 Honest answers, small talk | the answer's sentence by voice, kind and era, the value as today; small talk from the personality, the home (a citizen's is 2150) and the kind, by weights | V1, V2 |
| 176 Content sheets | 94 sheets; the new ones named | V0-V4 |

## 11. Kept, changed, removed

| Item | Fate | Migration risk |
|---|---|---|
| `Lies`, `RecordLies`, `FaultOrder`, `VerdictRules`, the evidence gate, `DiscrepancyLog`, translation and key words, the premades' intros and dialogs, narrative dialogs, `SpeechQueue`, the transcript, the window title, the Reference's claimed row | kept | none |
| new Domain `ContextMatch`, `Personalities`, `Voices` (with `VoiceLine`, `VoiceBook`, `VoiceContext`, `VoiceKeys`, `ReactionIntents`), `Slips`, `VoiceChecks`, `ReactionVerdict`, `ReactionIntent` | added | none (new) |
| `InterviewScript` (`Opening`, `Build`, + `Reaction`, + the slip after small talk), `InterviewCase` (+ the voice), `InterviewDay` (the day's lists), `InterviewQuestion` (`AnswerFor(era, kind)`), `WordingOverride` (+ `kinds`), `FormRequests` (the menu of the days so far), `TimelineService.AgencyForms` (up to today), `CaseSteps` / `StepsPanel` (the set after the arrival paper, B5), `CaseInstance` (+ `personality`, `dialogSeed`, `slip`), `CaseFactory` (the draws; the three small-talk sources), `ContentLibrarySO` (+ the cast), `InterviewLines` (+ `kindSmallTalk`, `slips`, `reactions`, `voices`, `smallTalkWeights`), `DayPlanSO` (+ `slipChance`), `Seeds` (+ `ForPersonality`, `ForSlip`, `OfKey`), `DeskConfigSO` (+ `reactionSeconds`), `TravellerView` (the linger), `GameManager.HandleDecision` (the reaction), `InterviewPresenter` (the reaction and slip lines), `InvestigationApp` and `OfficeCaseHud` (no claim), the builder (`BuildAppHeader`, `BuildOfficeCaseHud`), `DevToolsState` and the debug panel (force a personality), generator and validator, `ContentSheetMap`, the audit jobs | changed | the golden masters of §8; `OfficeGameplay` rebuilt in V0 |
| `claim.banner`, `InvestigationApp.claimText`, `OfficeCaseHud.SetClaim` / `claimRoot` / `claimText`, the builder's `ClaimText` and `ClaimStrip`, `CaseInstance.claimLine`, `CaseVerdict.claimSummary`, `InterviewQuestion.kinds` / `AsksOf`, `InterviewDay.QuestionsFor` / `AskableCategoriesFor` / `AnswerTellCategoriesFor`, `InterviewQuestions.MostForOneKind`, `WordingOverride.prompt`, `InterviewQuestion.PromptFor`, `interview.tripAskLabel`, `Interview.AskLabel`, `DocumentTemplateSO.askableBy`, `AskableForm.AskableBy` / `IsAskableBy`, `FormRequests.For(kind, …)`, the questions `q_trip_currency` and `q_trip_device`, `Interview.PickSmallTalk`, `CaseFactory`'s dialog stream | removed | every caller is listed in the plan's phases V0-V2; the nine form templates lose their `askableBy` lines |

## 12. The questions and Saleh's answers

| Q | Question | Answer | Resolved as |
|---|---|---|---|
| Q1 | The cast | The seven. | PS1 |
| Q2 | May a personality be a tell? | B: "A rare 'slip' line", a hint, never a proof. | T4, T9-T11 (`slipChance` 0.15, after small talk, lines by lie kind, never a fact) |
| Q3 | The same wheel for everyone | A: identical for everyone, "you can't know the type before checking the papers". | W1-W6, B5 |
| Q4 | Small talk | "everything: personality, home, type". | V5 (three sources, weighted) |
| Q5 | The banner | "there should be no banner; the claimant will ask in their personality". | B1-B5 (§3.5); plan phase V0 |
| Q6 | The reaction | By verdict and intent, with personality, "one or two lines". | R1-R3 |
| Q7 | Narrative dialogs | A: one authored version for everyone. | V10 |
| Q8 | The premades | C: own lines for every slot. | PS3, §4.5 |
| Q9 | Personality weights | A: one weight per personality for every kind. | PS2 |
| Q10 | Does the agency know? | A: the temperament is never printed. | PS4 |

New, raised by Q5 (for the coordinator to ask):

- **Q11. The Reference's claimed row.** With no banner, the Reference still opens each case on the claimed place's row, marked "(claimed)", with "Claimed place only" on. **A) keep it: it follows the place the traveller asked for aloud and prints no claim or kind (B4)**; B) show it only once a paper naming the destination has been read (the arrival paper, B5's moment); C) remove the claimed row and the filter: the player turns the books to the place themselves.

## 13. Intent audit

**Against Saleh's words.**
- *"not every poor and rich. people have personalities":* seven personalities that cut across every kind at one weight (PS1, PS2, Q9); §2.2 plays each in every kind.
- *"a believable but also funny absurd world":* the tone guide (§7): believable first, the absurd is the world's and the people are sincere; the debt dystopia palette; the cast's and the premades' own lines.
- *"people will have different reactions":* every verb's reply varies by voice (V1), and the stamp gets one or two lines in the voice (R1-R3, Q6).
- *"your interactions will stay consistent across types it is the context that changes"* and Q3's *"you can't know the type before checking the papers":* one wheel for everyone (W1-W6), no printed claim (B1-B3) and no kind-specific checklist before the arrival paper is read (B5).
- *Q2's slip:* a liar-only line at a sheet knob, after small talk, pointing without proving (T9-T11). *Q4:* three small-talk sources, weighted (V5). *Q5:* no banner and no claim tag anywhere (§3.5). *Q7:* one authored version of each dialog (V10). *Q8:* every premade's own line in every slot (PS3, §4.5). *Q10:* the temperament is never printed (PS4).
- *Rule 2:* the claim (spoken, V1, B3), the reactions (R) and small talk (V5) are the voice's. *Rule 3:* W1-W6. *Rule 4:* the weights, `slipChance` and the small-talk weights are sheet data; `reactionSeconds` is on `Desk_Default.asset`; premades are authored and never drawn, and a premade liar's slip is authored, never rolled (PS3, T9). *Rule 1:* R5.

**Redo or override checks.**
- Overrides the PC spec's **AP1** case header (the claim) and piece 10's **X12** office claim tag, and removes `CaseVerdict.claimSummary`, by Saleh's Q5.
- Overrides phase 21's **steps checklist** timing (the kind's set from arrival), by Q3's reasoning; keeps the per-kind sets and their steps.
- Overrides traveller-types **I1** (questions per kind) and phase 11's "no `overrides[].kind`" and `tripAskLabel`, by rule 3: per-kind entries are what the rule forbids. It is also less code.
- Overrides **I2**'s `askableBy` (a papers menu per kind), by rule 3; keeps I2's groups, missing-form replies and hand-over.
- Overrides traveller-types **§8**'s claim table as the banner's and its "small talk per kind": the claim table becomes the spoken default, and the kinds' small talk becomes one of three sources (Q4).
- Overrides piece 3's **era override of the prompt** (the desk's words are one per question); keeps its answer.
- Changes the dialog stream's one draw into a value of the same seed (V4); the stream's purpose (dialog variant picks) is unchanged.
- Keeps the Reference's claimed row (B4, pending Q11), the lie planner, the record lies, the fault order, the verdict rules, the evidence gate and translation.

**Reuse, never duplicate.** `WeightedRandom.Pick` draws the personality; `Seeds.Mix` makes the line picks (`OfKey` folds it; no second hash); one matcher serves the voice rows and the questions' overrides; `Interview.Fill`, `HoldsToken`, `WorstCaseLength` and `KeyWords.Spans` fill and check every line; `FormRequests.ReplyProblems` and `DialogChecks.MenuProblems` guard the one menu; `CaseSteps.Resolve` serves the checklist before and after the arrival paper; the reaction and the slip use the existing transcript, bubble and `SpeechQueue`; the debug force follows `ForcedCostumeError`'s pattern.

**No-code-path checks.** Could personalities be content alone? No: nothing picks a line by a trait of the traveller today; the minimum is one resolver, two streams, three fields on the case and the reaction's call. Could the reaction be a narrative dialog? No: a dialog is a wheel choice, and the reaction is said at the stamp. Could the banner's removal be content alone? No: the header and the tag are built by the builder and filled by code; removing them removes code. Could the one wheel be content alone? In part (the same labels on per-kind rows), but the menus would still hold different entries; removing `kinds` and `askableBy` is less code, not more.

## 14. The seam with the other specs and the plan

| There | Here |
|---|---|
| The PC spec's AP1/AP2 (the case header: the claim, the counters, Accept and Deny) and §12's `ClaimStrip` role | B1, B2: no claim; the counters fill the header; the role stays on the header's band |
| Piece 10's X12 (the office case HUD: the claim tag and the compare strip) | B1, B2: the compare strip alone |
| Phase 21's steps (ST1-ST4: a set per kind from arrival) | B5 |
| Traveller types I1 (questions per kind), §8's ask menus, phase 11's trip questions | W2, W3 (§3.2) |
| Traveller types I2 (`askableBy`, the papers menu per kind; groups; missing-form replies) | W4, W5; the groups, replies and hand-over are kept |
| Traveller types §8's claim table (per kind) | kept as the spoken default (V2, B3) |
| Traveller types §8's "small talk per kind" | one of V5's three sources |
| Traveller types §6.1's missing-form lines ("It's a Premium unit, I don't need one.") | kept as the defaults; the voices refuse in character (§4.4) |
| Plan phase 9 (labourers, not on main at `d8d46a4`): TC-520 (on arrival, never requested), the `Missing` lines, `InterviewCase.missingVariant`, the labourer's claim and step set | whichever lands second merges: if phase 9 lands first, phase V1 strips TC-520's `askableBy` with the others; if V1 lands first, phase 9 adds no `askableBy`, and its `Missing` variant feeds T6 as designed |
| The PC spec's Transcript tab and search (phase 19: each line joins search as it is spoken) | the claim, the slip and the reaction are traveller lines: shown, searchable, never evidence |

Built in `docs/superpowers/plans/2026-09-29-traveller-personalities-plan.md`: V0 no printed claim; V1 one wheel for every traveller; V2 personalities and the voice resolver; V3 the verdict reaction; V4 the slip; V5 the cast's and the premades' lines, the tone pass and the golden re-pack.
