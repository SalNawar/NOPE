# Traveller personalities: design

*2026-09-29 · drafted by Claude from Saleh's brief and his design rules of the same day · read-only map of `main` at `d8d46a4` (`E:\unity\NOPE-docs`) · the code wins over this text · the traveller-types spec (`2026-09-26-traveller-types-design.md`) owns the kinds, papers, lies and verdicts; this spec owns who the travellers are as people, what they say, and the one wheel they are all asked through; §14 is the seam · built in the phases of `docs/superpowers/plans/2026-09-29-traveller-personalities-plan.md` · every decision below is recorded with its reason; the multiple-choice questions of §12 wait for Saleh's answers*

Saleh, verbatim:
- "not every poor and rich. people have personalities we should focus on delivering a believable but also funny absurd world"
- "people will have different reactions. as a rule your interactions will stay consistant across types it is the context that changes"

His design rules of 2026-09-29 that this spec applies (they override any spec text that disagrees):
- **Rule 2.** Personalities, not traveller types, drive claim lines, reactions and small talk; aim for a believable but funny, absurd world.
- **Rule 3.** Interactions are the same for every traveller type (the same wheel entries and verbs); only the reply changes with personality and context.
- **Rule 4.** Balance knobs must be editable by Saleh in the Unity editor (ScriptableObjects that Generate World does not overwrite, or data he edits through the content spreadsheet), and authored cases (premades, forced slots, story beats) stay outside the random draws.
- Rule 1 (one penalty for any wrong decision) is untouched: a personality never changes a score (R5).

## 0. The map: what exists and what this spec does with it

Read in the code at `d8d46a4` (phase 9, labourers, is not on main yet: no Labourer blueprint, no paper-set directive, `InterviewCase.missingVariant` always `Honest`).

| What (where) | Today | This spec |
|---|---|---|
| The claim (`interview.claims`, one row per kind; `Interview.Claim`; `CaseInstance.claimLine`) | the kind's sentence, said in the bubble and printed on the banner and the office claim tag | said in the personality's words, the kind as context (V1); the banner keeps the kind's sentence (V6) |
| The hand-over reply (`interview.requestReply`) | "Here you are." from everyone | per personality (V1) |
| Missing-form replies (`interview.missingFormReplies`: kind, request, variant) | six rows; a rich tourist asked for a waiver: "It's a Premium unit, I don't need one." | per personality, kind and variant as context (V1); new default rows for the pairs the one menu adds (W5) |
| Spoken requests (`interview.requests`: Step closer, Speak up) | one reply each | per personality (V1) |
| Questions (`questions[]` with `kinds`; `overrides` by era with prompt and answer) | the displaced are asked about home (five questions), the 2150 citizens about the trip (two), through two rows per category for Currency and Device and two ask entries ("Ask about home >", "Ask about the trip >") | one question per category, asked of everyone in the same words (W2, W3); the answer's sentence by kind and era (overrides, answer only) and by personality (V1) |
| The papers menu (`DocumentTemplateSO.askableBy`, `FormRequests.For`, `TimelineService.AgencyForms` over every day plan) | per kind: citizens Manifest, Waiver, Proof of means; the displaced Intake Declaration, Return Order | one menu for everyone: every on-request form of the days so far (W4) |
| Small talk (`places[].smallTalk`, `eras[].smallTalk`; one draw on the dialog stream, `Interview.PickSmallTalk`) | the claimed place's lines, else its era's, for every kind: a 2150 tourist bound for Athens recites Athens' small talk | the personality's lines and the traveller's home lines in one pool; a citizen's home is 2150 (V5) |
| The verdict | the traveller vanishes as the stamp lands (`GameManager.HandleDecision` calls `SetTravellerAtDesk(false)`); nothing is said | a spoken reaction by personality, verdict and intent; the figure lingers for a knob's seconds (R1-R5) |
| Narrative dialogs (`dialogs[]`) | authored lines, spoken by any traveller the dialog is offered to | unchanged (V10) |
| Premades (`premades[]`: intro, record note, dialog) | authored intro and dialog; everyone's generic lines elsewhere | unchanged, plus an optional authored personality, never drawn (PS3) |
| Streams (`Seeds`) | case, violators, lies, dialog (small talk), looks, legendary, forms (a value), account, faults, strandings, slot, debt news | + `Seeds.ForPersonality` ("PRSN", one draw); the dialog seed becomes the seed of every line pick, as values, never draws (PS2, V4) |
| Content sheets (`ContentSheetMap`, 82 sheets) | claims, missing-form replies, questions, overrides, premades | + 9 sheets (`personalities`, `interviewReactions`, seven `voice*` sheets); questions lose `kinds`; overrides lose `prompt` and gain `kinds`; premades gain `personality`; the interview loses `tripAskLabel` (§9) |

## 1. Decisions

| Id | Decision | Why |
|---|---|---|
| PS1 | **The cast is seven personalities**, content rows in `world_source.json` `personalities` (id, name, weight, note): **Chatty, Curt, Anxious, Sunny, Grand, Stickler, Glum** (§2). The set is data: Saleh adds, removes and re-weights them in the spreadsheet. | Seven cover the two axes a desk conversation plays on (says a lot or a little; trusts the system or not), the three stances of the debt dystopia (believes it: Sunny; endures it: Glum; games it: Grand), the bureaucracy's own voice (Stickler) and the border game's nerves (Anxious). Fewer loses a stance; more dilutes each below about ten appearances a run (69 travellers over days 1-6). |
| PS2 | **A generated traveller's personality is one weighted draw over the cast on a new salted stream**, `Seeds.ForPersonality(caseSeed)` ("PRSN", `0x5052534E`), through `WeightedRandom.Pick`, with **one weight per personality for every kind**. Nothing else draws on the stream, and the draw reads nothing else. `SeedsTests`' distinctness table gains the salt. First cut: every weight 1. | The house rule for a new random concern, and the brief's "no existing draw shifts". One weight for every kind makes the personality independent of the kind ("not every poor and rich") and of every fault (T1). |
| PS3 | **Premades are never drawn.** `premades[].personality` (optional) names a premade's authored personality; blank is the default voice (today's lines). Their intros, record notes and dialogs stay authored. The first cut leaves all ten blank (§12 Q8 offers fits). | Rule 4 (authored cases stay outside the random draws) and the brief ("premades keep their authored voices"). |
| PS4 | **The personality is never printed**: no paper, record, row, banner or tag shows it; only its lines do. The debug panel shows it and can force it (`DevToolsState.ForcedPersonality`, for testing, never saved; the draw still runs, so no stream moves). | A printed label would let the player read the tag instead of the person; "believable" is people showing who they are. §12 Q10 offers an agency "temperament" row. |
| PS5 | **A traveller's voice is fixed at generation**: `CaseInstance.personality` (the id, blank for none) and `CaseInstance.dialogSeed` (`Seeds.ForDialog(caseSeed)`). Every line is resolved from them by one pure Domain resolver, `Voices` (§4.3): the small talk at generation (it needs the home's lines), every other line when the interview is built, the reaction at the stamp. | One place decides every line; Assembly-CSharp only passes the case in; the rule runs headless. |
| W1 | **The rule (Saleh's rule 3): every traveller of a day is offered the same wheel**: the same entries (labels, kinds, order, icons), the same verbs and the same desk prompts. Only the traveller's reply changes, by their personality and the context (§3). | His words. It also removes a meta-tell: today the wheel itself says which kind stands at the desk. |
| W2 | **One ask entry**: `interview.askLabel` = "Ask about the trip >" for everyone; `interview.tripAskLabel` and `Interview.AskLabel(lines, kind)` go. | Every traveller travels to their claim (traveller types K2: the claim is the destination for every kind), a displaced person to their home; "the trip" is true of all. |
| W3 | **One question per category, asked of everyone in the same words, about the place they are going to** (`{place}`). `questions[].kinds` goes, with `InterviewQuestion.AsksOf`, `InterviewDay.QuestionsFor` / `AskableCategoriesFor` / `AnswerTellCategoriesFor` and `InterviewQuestions.MostForOneKind`: the day's lists serve everyone. `overrides[]` lose `prompt` (the desk's words are the question's) and gain `kinds`: an override rewrites the traveller's default answer for some kinds, an era, or both, the most specific winning (V3). The merged rows keep today's order (currency, language, device, capital, ruler, date of birth); Currency and Device from day 4, Language, Capital and Ruler from day 5, Date of birth by the upgrade (§3.2). | Rule 3. The trip's prompts (today's citizen prompts) also read right for the displaced going home. Keeping the order and gating every fact question by day alone keeps every tell draw (T8). |
| W4 | **The papers menu is the same for everyone**: every on-request form of the day plans up to today, in first-appearance order, a request group once. `DocumentTemplateSO.askableBy`, `AskableForm.AskableBy` / `IsAskableBy` and `FormRequests.For(kind, …)` go. A traveller hands over the form they carry; asked for one they never carry, they answer with their missing-form line, as a rich tourist asked for a waiver does today. | Rule 3. "Up to today" grows the menu as new kinds arrive (the displaced's two forms from day 5), never offers a form before its kind has appeared, and never takes an entry away on a later day with a smaller mix. The capacity holds: "< Back" and five requests is six of the wheel's eight. |
| W5 | **Every (kind, request) pair the menu offers a kind that never carries the form needs a default `Honest` missing-form line**: `FormRequests.ReplyProblems`, the same rule, fed each day's menu. First cut: the displaced asked for a Manifest, a Waiver or a Proof of means, and each 2150 kind asked for an Intake Declaration or a Return Order (§3.3). | The existing rule and data path; these lines are where rule 3's jokes live ("A return order? I have a return booking. Is that the same thing?"). |
| W6 | **What may still differ between travellers**: a premade's own dialog entry (an authored story beat, rule 4), which garments "Look >" lists (the traveller's own clothes), and the PC's steps checklist (the desk's procedure per kind, traveller types §5.1: "different ... processing rules but same mechanics"). | Rule 3 governs types; a story beat and a traveller's own body are not a type's interaction, and the checklist is the clerk's paperwork, not the wheel. |
| V1 | **Seven voice slots, each the reply to one verb**: the claim, the hand-over, a missing form, a spoken request, an answer, small talk, the verdict reaction (§4.1). The desk's lines (the opener, the prompts, the requests' words) have no personality. | Every line a traveller says on the wheel's verbs, and nothing the desk says (W1). |
| V2 | **Defaults stay where they are** (`interview.claims`, `requestReply`, `missingFormReplies`, `requests[].reply`, the questions' answers and overrides, the places' and eras' small talk). **Personality lines are extra rows under `interview.voices`**, one list per slot, each naming its personality. The reaction's defaults are new rows, `interview.reactions`. A personality with no matching row says the default. | No migration and no second home for today's lines; Saleh authors personalities a row at a time; with no voice rows the game says exactly today's lines (apart from the one wheel of W and the small talk of V5). |
| V3 | **One resolution rule for every slot** (`Voices`, §4.3): among the slot's rows for the traveller's personality whose kinds (blank: any) and era (blank: any) match, the most specific tier wins (a named reason 4, named kinds 2, a named era 1, summed); several rows in the tier make a pool (V4); no matching row: the defaults, chosen by the same rule. The questions' overrides use the same matcher (`ContextMatch`, a small Domain helper scoring a row's kinds and era). | One rule an author can predict ("say more about who, and the line is theirs"), shared by the overrides (reuse). |
| V4 | **A line's pick is a value, never a draw**: `pool[(uint)Seeds.Mix(dialogSeed, Seeds.OfKey(slotKey)) % count]`, the slot key naming the verb ("claim", "answer:q_currency", "missing:TC-310:Honest", ...); `Seeds.OfKey` folds `Seeds.Mix` over the key's characters (stable in every runtime, unlike `string.GetHashCode`). The dialog stream's one draw (small talk) becomes such a value; `Interview.PickSmallTalk` and `CaseFactory`'s dialog stream go, and `Seeds.ForDialog` stays as the seed. | Adding a line to one slot never moves another slot's pick, and a replay says the same thing (`FormSerials`' precedent: a value, never a draw). The dialog seed was made for "dialog variant picks"; this is that job, whole. |
| V5 | **Small talk is the one slot whose default is content about a place**, so its pool is the personality's best tier and the home's lines together. Home is where the traveller comes from: a displaced person's claimed place (its lines, else its era's); a 2150 citizen's present (the present place's lines, else the Future era's: "The maglev was late again. Some things never change."). The desk still asks everyone "How is life back home?". | Both are true of the traveller at once (a Chatty woman from Babylon). Today a tourist bound for Athens recites Athens' small talk; the one "home" rule fixes it for everyone. |
| V6 | **The banner and the office claim tag keep the kind's plain claim** (`CaseInstance.claimLine`, from `interview.claims`); the bubble and the transcript say the personality's. | The banner is the desk's summary, read at a glance at 720p; a Chatty claim would crowd it. Speech is where people show. §12 Q5. |
| V7 | **Tokens per slot**: a claim must hold `{place}`; an answer must hold `{value}` (the canonical value, as today) and may hold `{place}`; a hand-over or missing-form line may hold `{document}` (the request's label) and `{place}`; every other line may hold `{place}`; nothing else. | The fills the resolver has; the answer stays compare-clickable; the key-word slots (place, name, document) keep the fills English untranslated. |
| V8 | **Every voice line passes today's line checks** (not blank; worst case within `interview.maxLineChars`, 100; known references) **and a fact guard**: Generate World and the validator warn when a line contains a checkable value (any place's or the present's fact value, a transponder model, an employer). | A personality line is never evidence (T5), and a fact written into a line goes stale when history edits the facts. |
| V9 | **Translation and key words are unchanged**: a displaced person's lines, personality lines included, show in their tongue with the key words in English until a Speech translator flips them; a 2150 citizen speaks English. | Piece 9 and traveller types I3-I4. Untranslated, a Curt displaced man still says "{place}. Home. ▯▯▯." |
| V10 | **Narrative dialogs keep their authored lines** for every traveller. | A dialog is a written scene with branches and effects; variants per personality multiply a branching script by seven. §12 Q7. |
| T1 | **The personality is independent of every fault by construction**: its own stream, weights that name no kind and no fault, and no rule that reads it (the lie roll, the fault order, the account, the look and the verdict never do). | No personality is more likely among liars: the brief's "a personality must never become a tell by itself". |
| T2 | **Before the stamp, a line depends only on what the desk can see**: the personality, the kind as presented (a poor citizen posing as rich presents as rich), the claimed era, the verb (the request, the question) and whether the traveller holds the requested form. `VoiceContext` holds the kind and the claimed era only, pinned by a test. | A line chosen by the hidden truth (lying or not, the lie kind, the true home, the real account) would be a tell. |
| T3 | **A liar's tell lives only in values**: a printed field, an answer's `{value}`, a worn garment. An answer's sentence is the personality's for that question, the same whether the value is a tell or not (tested). | The tell stays where the evidence rules prove it (papers prove, answers hint: traveller types L4); the sentence around it is flavour. |
| T4 | **No personality is a tell, and none is meant to be.** Nervous, evasive, grand or chatty travellers are honest and lying in the same proportions, which is the game's lesson that papers prove and people only hint. | Saleh's Q3 of 2026-09-26 (papers prove, answers hint). A personality tell could not deny anyone (the evidence gate needs a logged deviation) and would only bias accepts, punishing honest Anxious travellers. §12 Q2 offers a soft tell. |
| T5 | **Voice lines never confess and never state a checkable fact**: no line says or implies "my papers are forged", "I'm not really Premium" or "I'm from somewhere else", and none names a fact value (V8) or the traveller's own numbers (debt, wage, class, transponder, dates). A personality may sound suspicious in general (Anxious: "Is that allowed?"), because every traveller of it does. | A confession-shaped line would be a tell spoken by the innocent too, and it would teach players to deny on words, which the evidence gate punishes. |
| T6 | **The one fault-shaped context before the stamp is a missing form's variant**: a traveller who should carry a form and left it out answers with the `Missing` variant ("I... didn't get round to that one."). | It reads the absence, which the desk already sees (the form is not handed over) and which is itself the directive fault, read without evidence. The line adds no knowledge. |
| T7 | **After the stamp, the reaction may show intent** (§6). | The verdict slip shows CORRECT or WRONG at the same moment, so the reaction tells nothing the slip does not; a caught liar's "Worth a try." is the payoff of the catch. |
| T8 | **The one wheel keeps every tell draw**: the displaced's answer-tell categories keep today's order on days 5-6, and a citizen's smuggling planner keeps only Currency and Technology before any draw (`Lies.Plan`'s `Keep`), so the citizens' three new questions change nothing it draws. | The golden cases' tells, papers and names must stay byte-identical (§8). |
| R1 | **A traveller says one line as the stamp lands: their reaction**, in the bubble and the transcript. | Saleh: "people will have different reactions"; the verdict is where a character shows most. |
| R2 | **The reaction's context is the verdict** (Accepted, Denied), **the intent** (Lying: a place lie, smuggling included, or a record lie; Honest otherwise, directive faults and costume errors included, since those travellers do not know), optionally **the fault reason** (`Faults`: forged, disguised, smuggled, closed, wrongDate, expired, panic), the kind and the era. | Four base reactions per personality cover every verdict; a reason lets a line fit its catch ("Closed? How exciting! Like a secret!"). |
| R3 | **Defaults for every verdict × intent in `interview.reactions`** (the validator requires the four); personality rows in `interview.voices.reactions`. | Every traveller reacts, a premade without a personality included. |
| R4 | **The rules move nothing**: the decision, the score, the booth phase (no traveller at the desk), the slip and the day flow are today's. Only the figure and the bubble linger, for `DeskConfigSO.reactionSeconds` (first cut 2.5 s; 0 leaves at once, today's behaviour); calling the next traveller ends the linger. | Presentation only, so no rule, save or clock changes; a knob Saleh edits in the Inspector on `Desk_Default.asset`, which Generate World never writes (rule 4). |
| R5 | **A reaction never changes a score, a penalty or history.** | Rule 1. |
| C1 | **The content is rows** (§9): 9 new sheets, 4 changed; `ContentSheetMap` entries for every new field, the template rewritten. | The spreadsheet is where Saleh authors and balances lines (traveller types CS1). |
| C2 | **Authoring minimum: none** (V2). The first cut authors, per personality, the core a player hears on every traveller: 2 claims (a citizen line and a displaced line), the hand-over, the rich tourist's waiver and proof refusals, 2 small-talk lines (citizen, displaced), the 4 base reactions: 12 lines, 84 for the cast. The answers, spoken requests, remaining missing forms and reason reactions follow in the tone pass (plan phase V4). | The lines heard on every traveller first; the rest where they pay. |
| C3 | **One validation rule, `VoiceChecks.Problems` (Domain)**, shared by Generate World and the validator, as `Interview.ClaimProblems` is (§9.2). | One rule, two callers: the house pattern. |
| C4 | **The validator reports each personality's coverage once** (the slots that fall back to the default), as an info line, never a warning. | Saleh sees what is left to author without the log filling with warnings. |
| M1 | **Save version stays 2**: nothing persisted changes; the personality is regenerated with the day from its seed. | Additive. |

## 2. The personalities

### 2.1 The cast

Each is a way of being a person at a desk in 2150, not a job, a wealth or a kind. The ids are the sheet's.

| Id | Name | In one line | How they talk | What they talk about | Never |
|---|---|---|---|---|---|
| `chatty` | Chatty | Tells you everything, then a bit more. | Long, warm, run-on; commas, "well", "long story"; always gets to the answer. | relatives, exes, neighbours, their therapist, the trip's plans | a number, a secret, a pause |
| `curt` | Curt | Time is money, and in 2150 the queue bills by the minute. | One to four words; full stops; no pleasantries. | nothing, efficiently | rudeness beyond brevity; an unanswered question |
| `anxious` | Anxious | Worried about paradoxes, transponders, butterflies and you. | Questions back; repeats itself; "is that allowed?" | what could go wrong in the past, the fine print, sneezing near history | a confession; anything only a liar would say (T4, T5) |
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

## 3. One wheel for every traveller

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

New default `Honest` lines (`interview.missingFormReplies`, one row per kind, as today):

| Kind | Request | Line |
|---|---|---|
| Displaced | TC-230 | "A manifest? I was pulled out of my own time. I didn't pack." |
| Displaced | TC-310 | "A waiver? Nobody asked me anything before the sky opened." |
| Displaced | proof | "Means? I have what was in my pockets when the sky opened." |
| RichTourist, PoorTourist, Labourer (a row each) | TC-620 | "An intake declaration? I'm leaving, not arriving." |
| RichTourist, PoorTourist, Labourer (a row each) | TC-630 | "A return order? I have a return booking. Is that the same thing?" |

### 3.4 What stays different

A premade's own dialog entry, the garments a "Look >" lists, and the steps checklist per kind (W6). Everything else on the wheel is identical for every traveller of the day.

## 4. Voice lines

### 4.1 The slots

| Slot | Said when | Key (the verb) | Context columns | Tokens | Default (where it lives) | Sheet |
|---|---|---|---|---|---|---|
| Claim | stepping up (the transcript's second line) | none | kinds, era | `{place}` required | `interview.claims` (the kind's) | `voiceClaims` |
| Hand-over | handing a form over | the request (optional: a form number or a group id) | kinds, era | `{document}`, `{place}` | `interview.requestReply` | `voiceHandOver` |
| Missing form | asked for a form they do not carry | the request (required); the variant (Honest, Missing) | kinds, era | `{document}`, `{place}` | `interview.missingFormReplies` (kind, request, variant) | `voiceMissingForms` |
| Spoken request | "Step closer", "Speak up" | the request id (required) | kinds, era | `{place}` | `interview.requests[].reply` | `voiceSpoken` |
| Answer | asked a question | the question id (required) | kinds, era | `{value}` required, `{place}` | the question's override for the kind and era, else its answer (§3.2) | `voiceAnswers` |
| Small talk | "Small talk" | none | kinds, era | `{place}` | the home's lines (V5), always in the pool | `voiceSmallTalk` |
| Reaction | the stamp lands | the verdict and the intent (required); the fault reason (optional) | kinds, era | `{place}` | `interview.reactions` (verdict, intent, reason, kinds, era) | `voiceReactions` |

### 4.2 The data

```json
"personalities": [
  { "id": "curt", "name": "Curt", "weight": 1, "note": "Time is money, and in 2150 the queue bills by the minute." }
],
"interview": {
  "...": "today's fields, unchanged but tripAskLabel, then:",
  "reactions": [
    { "verdict": "Accepted", "intent": "Honest", "text": "Thank you!" },
    { "verdict": "Accepted", "intent": "Honest", "kinds": ["Displaced"], "text": "Thank you. Home, at last." },
    { "verdict": "Denied", "intent": "Honest", "text": "But... I did everything right." },
    { "verdict": "Denied", "intent": "Honest", "kinds": ["Displaced"], "text": "Then how do I get home?" },
    { "verdict": "Accepted", "intent": "Lying", "text": "Thank you. Thank you very much." },
    { "verdict": "Denied", "intent": "Lying", "text": "Worth a try." }
  ],
  "voices": {
    "claims":       [ { "personality": "curt", "kinds": ["Displaced"], "text": "{place}. Home. Now." } ],
    "handOver":     [ { "personality": "curt", "text": "Here." } ],
    "missingForms": [ { "personality": "grand", "kinds": ["RichTourist"], "request": "TC-310", "variant": "Honest", "text": "A waiver? My transponder has people for that." } ],
    "spoken":       [ { "personality": "grand", "request": "step_closer", "text": "I don't step. I proceed." } ],
    "answers":      [ { "personality": "glum", "question": "q_currency", "text": "{value}. Not that it'll be enough." } ],
    "smallTalk":    [ { "personality": "glum", "kinds": ["RichTourist", "PoorTourist", "Labourer"], "text": "The maglev was late. So was my pay. So, apparently, was my birth." } ],
    "reactions":    [ { "personality": "glum", "verdict": "Denied", "intent": "Lying", "text": "Figures." } ]
  }
},
"premades": [ { "id": "socrates", "...": "today's fields, then (an example: the first cut leaves it out, PS3):", "personality": "stickler" } ]
```

`kinds`, `era`, a hand-over's `request`, `reason` and a premade's `personality` are left out of the JSON when blank. Generate World writes the cast into `ContentLibrarySO` (`personalities`), the rows into `InterviewLines.reactions` and `InterviewLines.voices` (a `VoiceBook`: one list per slot of one flat serializable row, `VoiceLine`: personality, kinds, era, key, variant, verdict, intent, line), and a premade's personality into `LegendarySO.personality`. `ReactionVerdict` and `ReactionIntent` are new serialized enums, append-only and pinned. Line ids follow the generator's grammar: `interview.reactions.{n}`, `interview.voices.{list}.{personality}.{n}` (n counts that personality's rows in that list).

### 4.3 The resolver

Pure Domain (`Voices`), tested headless. For one slot, one verb and one traveller (the personality id, the `VoiceContext` of kind and claimed era, the dialog seed):

1. **Match.** A row matches when its personality is the traveller's, its kinds are blank or hold the kind, its era is blank or is the claimed era, and its keys equal the verb's (the request, the question, the variant; for a reaction the verdict and the intent, and a reason that is blank or the traveller's fault reason).
2. **Tier.** Score each match: a named reason 4, named kinds 2, a named era 1. The pool is the matches with the highest score.
3. **Fall back.** An empty pool takes the default rows through the same two steps (the defaults of §4.1). For an answer, the default is one line: the question's best-scoring override (a tie keeps the first listed), else its answer. For small talk, the pool is the personality's tier and the home's lines together (V5).
4. **Pick.** One line of the pool, as a value: `pool[(uint)Seeds.Mix(dialogSeed, Seeds.OfKey(slotKey)) % pool.Count]`. Slot keys: `claim`, `handover:{request}`, `missing:{request}:{variant}`, `spoken:{id}`, `answer:{question id}`, `smalltalk`, `reaction:{verdict}:{intent}`.
5. **Fill.** `Interview.Fill` the tokens (`{place}` the claimed place's label, `{value}` the canonical value, `{document}` the request's label) and take the key-word spans over the template and its fills (`KeyWords.Spans`), as every traveller line does today.

A traveller with no personality (a premade left blank, or an empty cast) skips step 1 and says the defaults. With no voice rows at all, every line is today's, except small talk (V4, V5).

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

## 5. Personalities and tells

The contract, and what holds each part of it:

| Part | Rule | Held by |
|---|---|---|
| Who gets which personality | one weighted draw on its own stream, the same weights for every kind; premades authored (PS2, PS3, T1) | `PersonalitiesTests` (one draw; the weights); `SeedsTests` (the salt, distinct); the plan's independence probe in Unity: days 1-6 × 20 seeds generated twice, the second with every liar and fault chance at 0 in memory, give every slot the same personality, and the report tabulates the cast among liars and among the honest |
| What a line may depend on before the stamp | the personality, the presented kind, the claimed era, the verb, whether the form is held (T2) | `VoicesTests.ContextHoldsOnlyWhatTheDeskSees` pins `VoiceContext`'s fields to the kind and the claimed era: adding a field fails the test, so a reviewer must read T2 |
| Where a liar's tell lives | in values only; an answer's sentence is the same for a tell and for the truth (T3) | `InterviewScriptTests`: two cases alike but for one answer's tell give the same sentence around the value, and the same claim, replies and small talk |
| What a line may say | never a confession, never a checkable fact, never the traveller's own numbers (T5) | the fact guard (V8, a warning); the author's checklist (§7.4); review |
| The one fault-shaped context | the `Missing` variant, which reads a visible absence (T6) | `FormRequestsTests`, `InterviewScriptTests` (the variant comes from the case, set by the paper-set fault in plan phase 9) |
| After the stamp | the reaction may show intent (T7) | `VoicesTests` (the reaction's context); R4's order: the slip and the line land together |

**Is any personality a tell? No** (T4), and that is deliberate: every personality is spread evenly over the honest and the lying, so an Anxious traveller is exactly as likely to be honest as a Curt one. The player who learns to distrust the nervous and wave through the charming is corrected by the evidence gate and the citations, which is the game's point: papers prove, people only hint. §12 Q2 offers Saleh a soft tell instead.

**A liar's personality and their tells.** The personality shapes the sentence around a tell, never the tell. A Glum smuggler asked about the currency says "Credits. Not that it'll be enough." while an honest Glum tourist says "Silver drachma (owl). Not that it'll be enough.": the same template, and only the value, compared against the Currency Ledger, catches the smuggler. A Chatty forger hands over a forged visa with the same words as an honest Chatty tourist. A liar never breaks character, never gets a nervous line an honest traveller of their personality would not say, and never says anything different until the stamp.

## 6. The verdict reaction

| | Honest (no lie; a directive fault or a costume error counts as honest intent) | Lying (a place lie, smuggling or a record lie) |
|---|---|---|
| **Accepted** | relief or joy, in character ("Stamped at a slight angle. I'll allow it. This once.") | got away with it, in character ("Thank you, thank you! Worth every credit I don't have!") |
| **Denied** | indignation or despair, in character ("You'll hear from my lawyer. Well, my lawyer's lawyer. We share one.") | caught, in character ("Figures.") |

- A reason row narrows a line to its catch: an honest traveller denied for a closed destination (`closed`), a costume error (`panic`: "What's wrong with my outfit? Is it the hat? It's the hat, isn't it."), a smuggler caught (`smuggled`: "Fine. Keep it. I have three more at home.").
- **Sequence** (R4): the stamp lands; `GameManager.HandleDecision` scores and records exactly as today (the booth phase goes to "no traveller" at once, so the wheel cannot open); the reaction line (resolved from the case: the intent from `IsLiar || IsForger`, the reason from `FaultReason`) is appended to the transcript and said in the bubble; the verdict slip shows as today (a citation still pauses the clock); the figure and the bubble stay for `DeskConfigSO.reactionSeconds`, then the traveller leaves; calling the next traveller first ends it at once.
- Decided on the PC (the office view hidden): the line reaches the transcript (the Transcript tab shows it and search indexes it) and the bubble if the office view shows.
- A displaced person's reaction is in their tongue with its key words, as every line of theirs (V9).

## 7. Tone guide

### 7.1 Principles

1. **Believable first.** Every line is something a person could say at a customs counter. The joke rides on who they are and where they live, never on nonsense.
2. **The world is absurd; the people are sincere.** The dystopia is played straight (the queue bills by the minute, debt passes to your kin, the agency has slogans); each person reacts to it in character. Sunny means it; Glum has stopped arguing.
3. **Short.** One breath. The worst case (the longest place label is 43 characters, the longest fact value 28) stays within 100 characters; a claim should stay near 70 so the bubble reads at a glance.
4. **Never evidence** (T5): no fact values (currencies, devices, languages, rulers, capitals, transponder models, employers), no own numbers (debt, wage, class, dates), no confession, no line that contradicts a Directive or the papers.
5. **The kind is context, not character.** Tourists are going on holiday, labourers to work off a debt, the displaced home; each personality meets that in its own way (a Sunny labourer is thrilled; a Glum tourist is unimpressed).
6. **Same energy, honest or lying** (T3, T4). A personality never "breaks" to hint at a lie.
7. **Punch up.** The joke is on the agency, the debt economy, the adverts and the absurdity; never on poverty, and never cruel about the displaced's plight.
8. **In the world.** No real-world brands, no present-day pop culture, no profanity, no winks at the player, no mention of the game.
9. **Survive translation.** A displaced line keeps its key words (home, please, papers, yes, no, Temporal Customs, and the place) in English when untranslated; put the gist on one of them where you can ("{place}. Home. Now.").

### 7.2 The debt dystopia palette

- **Use:** Debt Relief departures ("A FRESH START", "CLEAR YOUR DEBT", "ATTENDANCE REQUIRED"); queues and forms billed by the minute; debt passing to your kin (the waiver's own row); frozen accounts and default; Premium families and Economy transponders; holiday credit; the payment plan on everything (shoes, the sofa, a sofa's cushions); the past as a resort brochure; the intake hall where the displaced wait; the maglev being late; the agency's leaflets and slogans.
- **Avoid:** real places the world does not hold (say "the past", an era, or `{place}`); anything a player could check against a paper or a book (T5); jokes about suicide, violence or illness; slurs of any era.

### 7.3 The cast in their own words (examples)

Each line is labelled with its slot and context, and each fits V7 and V8 with the longest fills. These are the first rows the plan authors (phase V2), the start of C2's core.

**Chatty**
- Claim, tourist: "{place}! Our anniversary. Well, mine. He left. Long story!"
- Claim, displaced: "Home to {place}. My goat's been alone for days. Or centuries?"
- Missing form (TC-310, Honest), rich tourist: "No waiver, it's the Premium unit! My cousin went Economy once. We don't talk about him."
- Answer (`q_device`), citizen: "Just the approved kit: {value}. My therapist says I overpack. Emotionally."
- Reaction, Accepted · Honest: "Thank you! I'll bring you something back. Not a plague. Something nice."

**Curt**
- Claim, citizen: "{place}. Leisure. Go."
- Claim, displaced: "{place}. Home. Now."
- Hand-over: "Here."
- Spoken (Speak up): "Said it once. That was billed."
- Reaction, Denied · Honest: "Unbelievable. And I paid for the queue."

**Anxious**
- Claim, tourist: "{place}, please. Is it safe? They said it's safe. It's safe?"
- Missing form (TC-310, Honest), rich tourist: "A waiver? Do I need one? It's Premium. Premium never fails. Does it?"
- Small talk, citizen: "I read one sneeze in the past can delete a whole family tree. Is that true?"
- Answer (any fact question): "{value}. Is that right? I checked the list twice. Three times."
- Reaction, Accepted · Honest: "Oh, thank goodness. Now I can worry about the actual trip."

**Sunny**
- Claim, labourer: "Debt Relief to {place}! A fresh start! Like the brochure!"
- Small talk, citizen: "My whole block got frozen in default! Everyone's home all day now! So cosy!"
- Missing form (proof, Honest), displaced: "Proof of means? I have a smile and a leaflet! Attendance required!"
- Hand-over: "Here you go! Isn't paperwork wonderful? It's like a hug, but stamped!"
- Reaction, Denied · Honest: "Denied! That's fine! Every no is a yes I haven't paid for yet!"

**Grand**
- Claim, poor tourist: "{place}. Somewhere with a view, and a better class of peasant."
- Claim, displaced: "Return me to {place} at once. I was somebody there."
- Missing form (TC-310, Honest), rich tourist: "A waiver? My transponder has people for that."
- Spoken (Step closer): "I don't step. I proceed."
- Reaction, Denied · Honest: "You'll hear from my lawyer. Well, my lawyer's lawyer. We share one."

**Stickler**
- Claim, tourist: "One leisure departure to {place}, as the notice words it."
- Hand-over: "Face up, top edge first, as the sign above you requires."
- Missing form (TC-310, Honest), rich tourist: "Premium units are exempt from the {document}. It's on the back of the form."
- Small talk, displaced: "I've read all your forms. Four days in the intake hall. Two have typos."
- Reaction, Accepted · Honest: "Stamped at a slight angle. I'll allow it. This once."

**Glum**
- Claim, labourer: "Debt Relief to {place}. It's all in the contract. So am I."
- Small talk, citizen: "The maglev was late. So was my pay. So, apparently, was my birth."
- Answer (`q_currency`): "{value}. Not that it'll be enough."
- Reaction, Accepted · Honest: "Approved. Great. Now the hard part: everything else."
- Reaction, Denied · Lying: "Figures."

Reason reactions, for flavour: Sunny, Denied · Honest · `closed`: "Closed? How exciting! Like a secret!"; Anxious, Denied · Honest · `panic`: "What's wrong with my outfit? Is it the hat? It's the hat, isn't it."; Grand, Denied · Lying · `smuggled`: "Fine. Keep it. I have three more at home."

### 7.4 Before adding a row (the author's checklist)

1. Could a real person say it at this counter, in this mood? (principle 1)
2. Does it fit every kind and era its row allows? A blank-kinds line must suit a tourist and a displaced person alike.
3. Does it name a fact, a number of the traveller's, or imply a lie? Then rewrite it (T5; the fact guard warns about facts).
4. Would an honest traveller of this personality say exactly this? It must be yes for every line before the stamp.
5. Does it hold its required token (`{place}` in a claim, `{value}` in an answer) and fit 100 characters with the longest fill? (Generate World checks.)
6. Is the joke on the system rather than on the poor or the displaced? (principle 7)

## 8. Golden masters: what changes

The offline suite's golden values:

| Test | Change |
|---|---|
| `SeedsTests` | `PersonalitySalt` pinned ("PRSN"), in the one distinctness table; `Seeds.OfKey` pinned values |
| `InterviewTests` | `AskLabel` and `PickSmallTalk` gone; the answer by kind and era; one prompt per question |
| `InterviewScriptTests` | the same hub, papers and ask menus for every kind; the voice lines; the reaction line |
| `InterviewDayTests` | the day's questions for every kind; the forms of the days so far |
| `FormRequestsTests` | `askableBy` gone; the menu of the days so far; the replies over it |
| `SerializedEnumsTests` | `ReactionVerdict`, `ReactionIntent` pinned; the `askableBy` pin goes |
| `ContentSheetMapTests` | the round trip covers the new sheets (no test change; it fails until they are mapped) |

The Phase 0 golden masters (`docs/reviews/audit-baseline/golden`), re-packed once at the end of the plan (its phase V4), each phase's diff explained in its commit:

| File | Changes | Must stay byte-identical |
|---|---|---|
| `cases.txt` | the WORLD `askable` / `answerTells` lines (one list per day, no per-kind repeats); every CASE line gains `personality=` after `gender=`; `smalltalk=` changes for every traveller (the value pick, V4; a citizen's pool is 2150's, V5; personality lines as they land); a citizen's `answers=` on days 5-6 gain Language, Geography and Politics (the destination's values) | the claim, name, born, role, allowed, violator, liar, home, **tells**, papers, record, look, garments, premade, intro, `claimLine`, tongue: every draw of the case, lie, look, account, fault and violator streams |
| `play_transcript.txt` | the displaced's prompts (days 5-6); the day-4 and day-5 announcements; the papers menus of days 5-6 and their missing-form lines; every spoken line of a traveller whose personality has a row; one reaction line after each verdict | every verdict, score, citation, ledger line, evidence count and the Home phase |
| `play_saves/*.json` | nothing (nothing persisted changes, M1) | all |
| `world_generate.txt`, `data_hashes.txt`, `validator.txt` | the library (the cast, reactions, voices), the merged question assets, the nine form templates (`askableBy` stripped), the premades' assets, `Desk_Default.asset` (`reactionSeconds`); the validator's new checks and its coverage lines | every other asset |
| scene dumps | nothing: no builder change is planned (the reaction uses the existing bubble, transcript and figure); if one proves necessary, the rebuilt `OfficeGameplay` is committed with its dump | all |
| profiles | nothing per frame (the resolver runs when a traveller steps up, at a wheel choice and at the stamp, never per frame) | the allocation sites, the per-frame GC |

`ContentSheets/TimeDesk_Content_Template.xlsx` is rewritten in each phase that changes the map.

## 9. Content sheets and validation

### 9.1 The sheets

| Sheet | JSON path | Columns | Notes |
|---|---|---|---|
| `personalities` (new, keyed by id) | `personalities` | `id` (required), `name`, `weight` (number), `note` | the cast; a weight of 0 benches a personality; `note` is for authors (the tone in a line), as `confusable.why` is for reviewers |
| `interviewReactions` (new) | `interview.reactions` | `verdict` (Accepted, Denied), `intent` (Honest, Lying), `reason` (blank or a fault reason), `kinds` (list), `era` (ref `eras`), `text` | the defaults; the validator demands the four verdict × intent rows with blank reason, kinds and era |
| `voiceClaims` (new) | `interview.voices.claims` | `personality` (required, ref), `kinds`, `era`, `text` | |
| `voiceHandOver` (new) | `interview.voices.handOver` | `personality`, `request` (blank: any), `kinds`, `era`, `text` | |
| `voiceMissingForms` (new) | `interview.voices.missingForms` | `personality`, `request` (required), `variant` (Honest, Missing), `kinds`, `era`, `text` | |
| `voiceSpoken` (new) | `interview.voices.spoken` | `personality`, `request` (ref `interviewRequests`), `kinds`, `era`, `text` | |
| `voiceAnswers` (new) | `interview.voices.answers` | `personality`, `question` (ref `questions`), `kinds`, `era`, `text` | |
| `voiceSmallTalk` (new) | `interview.voices.smallTalk` | `personality`, `kinds`, `era`, `text` | |
| `voiceReactions` (new) | `interview.voices.reactions` | `personality`, `verdict`, `intent`, `reason`, `kinds`, `era`, `text` | |
| `premades` (changed) | `premades` | + `personality` (ref, blank: the default voice) | |
| `questions` (changed) | `questions` | − `kinds` | one question per category |
| `questionOverrides` (changed) | `questions[].overrides` | `era` (blank: any), + `kinds`, − `prompt`, `answer` | an answer by kind and era |
| `interview` (changed) | `interview` | − `tripAskLabel` | `askLabel` is everyone's |

82 sheets become 91. `docs/CONTENT_SHEETS.md`'s "Dialogue and interactions" line gains the new sheets.

### 9.2 The checks (`VoiceChecks.Problems` and `Personalities.Problems`, called by Generate World and the validator)

- The cast: ids unique and not blank; weights at least 0; at least one positive when the list is not empty; names not blank.
- Every voice row: a listed personality; known kinds; a known era; its keys valid for its slot (a request that is a form number or a group id of some day's forms; a spoken request of `interview.requests`; a question of `questions`; a reason of `Faults`); text not blank; only its slot's tokens, the required one present (V7); its worst case within `interview.maxLineChars`.
- The reactions: the four defaults present.
- A premade's personality is listed or blank.
- Warnings: a line holding a fact value, a transponder model or an employer (V8); a row identical to another in every column.
- Info: each personality's coverage (the slots that fall back to a default), one line per personality (C4).
- The existing rules, fed the new shapes: `InterviewQuestions.Problems` (one question per category), `FormRequests.ReplyProblems` over each day's menu and the kinds in play that day, `DialogChecks.MenuProblems` with the most requests any day offers.

## 10. `docs/FEATURES.md` lines that change

| Line (today) | Change | Phase |
|---|---|---|
| 11 Deterministic case generation | + the personality stream (one draw); the dialog seed feeds the line picks as values | V2 |
| 35 Traveller wheel | the same entries for every traveller of a day | V1 |
| 36 Speech bubble | + the reaction after the stamp; the figure lingers `reactionSeconds` | V3 |
| 71, 73 Kinds, premades | + every generated traveller has a personality, independent of the kind; a premade's is authored | V2 |
| 77 Claim banner | the banner keeps the kind's words; the bubble says the personality's | V2 |
| 78 Traveller wheel = the interview | one papers menu (every on-request form of the days so far) and one ask entry for everyone; missing-form lines by kind, variant and personality | V1, V2 |
| 79 Questions | one question per category for every traveller, in the same words; the day ramp of §3.2 | V1 |
| 80 Honest answers, small talk | the answer's sentence by personality, kind and era, the value as today; small talk from the personality and the home (a citizen's is 2150) | V1, V2 |
| 176 Content sheets | 91 sheets; the new ones named | V1-V3 |

## 11. Kept, changed, removed

| Item | Fate | Migration risk |
|---|---|---|
| `Lies`, `RecordLies`, `FaultOrder`, `VerdictRules`, the evidence gate, `DiscrepancyLog`, translation and key words, the steps checklist, the premades' intros and dialogs, narrative dialogs, `SpeechQueue`, the transcript | kept | none |
| new Domain `ContextMatch`, `Personalities`, `Voices` (with `VoiceLine`, `VoiceBook`, `VoiceContext`, `VoiceKeys`, `ReactionIntents`), `VoiceChecks`, `ReactionVerdict`, `ReactionIntent` | added | none (new) |
| `InterviewScript` (`Opening`, `Build`, + `Reaction`), `InterviewCase` (+ the voice), `InterviewDay` (the day's lists), `InterviewQuestion` (`AnswerFor(era, kind)`), `WordingOverride` (+ `kinds`), `FormRequests` (the menu of the days so far), `TimelineService.AgencyForms` (up to today), `CaseInstance` (+ `personality`, `dialogSeed`), `CaseFactory` (the draw; the home's lines), `LegendarySO` (+ `personality`), `ContentLibrarySO` (+ the cast), `InterviewLines` (+ `reactions`, `voices`), `Seeds` (+ `ForPersonality`, `OfKey`), `DeskConfigSO` (+ `reactionSeconds`), `TravellerView` (the linger), `GameManager.HandleDecision` (the reaction), `InterviewPresenter` (the reaction line), `DevToolsState` and the debug panel (force a personality), generator and validator, `ContentSheetMap`, the audit jobs | changed | the golden masters of §8 |
| `InterviewQuestion.kinds` / `AsksOf`, `InterviewDay.QuestionsFor` / `AskableCategoriesFor` / `AnswerTellCategoriesFor`, `InterviewQuestions.MostForOneKind`, `WordingOverride.prompt`, `InterviewQuestion.PromptFor`, `interview.tripAskLabel`, `Interview.AskLabel`, `DocumentTemplateSO.askableBy`, `AskableForm.AskableBy` / `IsAskableBy`, `FormRequests.For(kind, …)`, the questions `q_trip_currency` and `q_trip_device`, `Interview.PickSmallTalk`, `CaseFactory`'s dialog stream | removed | every caller is listed in the plan's phases V1 and V2; the nine form templates lose their `askableBy` lines |

## 12. Open questions for Saleh

Each lists the option this spec recorded first, in bold; an answer that differs changes only the named decisions.

- **Q1. The cast.** **A) the seven of §2 (PS1)**; B) six: drop Curt, Glum keeps the short lines; C) five: Chatty, Anxious, Sunny, Grand, Glum; D) the seven plus one of yours (name it and give it a line).
- **Q2. May a personality be a tell?** **A) never: every personality is as common among liars as among the honest (T4)**; B) a slip: a liar sometimes adds one personality "slip" line, a hint like an answer, never a proof, at a knob's chance (the honest never say it); C) a lean: some personalities weighted up among liars, a statistical tell only.
- **Q3. The same wheel for everyone.** **A) the same questions and the same papers menu for every traveller; the replies differ (W1-W5)**; B) the same hub only ("Ask about the trip >" for all), but each kind keeps its own questions and papers inside; C) keep today's per-kind wheel; personalities change the replies only.
- **Q4. Small talk.** **A) one pool: the personality's lines and the home's lines (V5)**; B) the personality's lines only, the home's as the fallback; C) the displaced keep their home's lines only, 2150 citizens get the personality's.
- **Q5. The banner and the claim tag.** **A) the kind's plain claim; the personality speaks in the bubble (V6)**; B) the personality's spoken claim everywhere; C) the destination alone ("To: Periclean Athens (Ancient)").
- **Q6. The verdict reaction.** **A) by verdict and intent: a caught liar reacts as caught, once the slip shows (R2, T7)**; B) by verdict only: reactions never reveal who lied; C) no spoken reaction; a face or a gesture later (art).
- **Q7. Narrative dialogs (the calculator rumour).** **A) authored lines for everyone (V10)**; B) per-personality variants of a dialog's lines, in a later phase.
- **Q8. The premades.** **A) an optional authored personality, blank for all ten in the first cut (PS3)**; B) these fits: Senenmut Grand, Socrates Stickler, Aspasia Chatty, Ban Zhao Curt, Arib Sunny, al-Khwarizmi Stickler, Gutenberg Anxious, Leonardo Chatty, Aemilia Lanyer Glum, Cecilia Gallerani Grand (Socrates, the impostor, shares his personality with an honest premade, so it is no meta-tell); C) premade-only lines for every slot (their own voice everywhere, about twenty lines each).
- **Q9. Personality weights.** **A) one weight per personality for every kind (PS2)**; B) weights per kind (for example more Grand among rich tourists): believable, but the personality then hints at the kind; C) weights per day (a day of Glum labourers).
- **Q10. Does the agency know?** **A) no: the personality is never printed (PS4)**; B) a "Temperament" row on the Citizen Account and the Registry entry ("Temperament: Compliant", "Temperament: Excitable, flagged"), flavour, never evidence; C) only in the clerk's end-of-shift ledger ("3 Anxious, 2 Grand processed").

## 13. Intent audit

**Against Saleh's words.**
- *"not every poor and rich. people have personalities":* seven personalities that cut across every kind at one weight (PS1, PS2); §2.2 plays each in every kind.
- *"a believable but also funny absurd world":* the tone guide (§7): believable first, the absurd is the world's and the people are sincere; the debt dystopia palette; the cast's own lines.
- *"people will have different reactions":* every verb's reply varies by personality (V1), and the stamp gets a spoken reaction (R1-R5).
- *"your interactions will stay consistent across types it is the context that changes":* one wheel for everyone (W1-W6); the context columns of every line (V3).
- *Rule 2:* the claim (spoken, V1, V6), the reactions (R) and small talk (V5) are the personality's. *Rule 3:* W1-W6. *Rule 4:* the weights are sheet data, `reactionSeconds` is on `Desk_Default.asset`, premades are authored and never drawn (PS3). *Rule 1:* R5.

**Redo or override checks.**
- Overrides traveller-types **I1** (questions per kind) and phase 11's "no `overrides[].kind`" and `tripAskLabel`, by rule 3: per-kind entries are what the rule forbids. It is also less code (the per-kind lists and `kinds` go).
- Overrides **I2**'s `askableBy` (a papers menu per kind), by rule 3; keeps I2's groups, missing-form replies and hand-over.
- Overrides traveller-types **§8**'s "small talk per kind (tourists excited, labourers grim)", by rule 2, and fixes phase 6's noted deviation (a citizen reciting the destination's small talk).
- Overrides piece 3's **era override of the prompt** (the desk's words are one per question); keeps its answer.
- Changes the dialog stream's one draw into a value of the same seed (V4); the stream's purpose (dialog variant picks) is unchanged.
- Keeps the banner's claim (V6), the lie planner, the record lies, the fault order, the verdict rules, the evidence gate and translation.

**Reuse, never duplicate.** `WeightedRandom.Pick` draws the personality; `Seeds.Mix` makes the line picks (`OfKey` folds it; no second hash); one matcher serves the voice rows and the questions' overrides; `Interview.Fill`, `HoldsToken`, `WorstCaseLength` and `KeyWords.Spans` fill and check every line; `FormRequests.ReplyProblems` and `DialogChecks.MenuProblems` guard the one menu; the reaction uses the existing transcript, bubble and `SpeechQueue`; the debug force follows `ForcedCostumeError`'s pattern.

**No-code-path checks.** Could personalities be content alone? No: nothing picks a line by a trait of the traveller today; the minimum is one resolver, one stream, two fields on the case and the reaction's call. Could the reaction be a narrative dialog? No: a dialog is a wheel choice, and the reaction is said at the stamp, so it reuses the transcript and the bubble instead. Could the one wheel be content alone? In part (the same labels on per-kind rows), but the menus would still hold different entries; removing `kinds` and `askableBy` is less code, not more.

## 14. The seam with the traveller-types spec and the plan

| There | Here |
|---|---|
| I1 (questions per kind), §8's ask menus, phase 11's trip questions | W2, W3 (§3.2) |
| I2 (`askableBy`, the papers menu per kind; groups; missing-form replies) | W4, W5; the groups, replies and hand-over are kept |
| §8's claim table (per kind) | kept as the defaults and the banner (V2, V6) |
| §8's "small talk per kind" | V5 |
| §6.1's missing-form lines ("It's a Premium unit, I don't need one.") | kept as the defaults; personalities add their own (§4.4) |
| Plan phase 9 (labourers, not on main at `d8d46a4`): TC-520 (on arrival, never requested), the `Missing` lines, `InterviewCase.missingVariant`, the labourer's claim | whichever lands second merges: if phase 9 lands first, phase V1 strips TC-520's `askableBy` with the others; if V1 lands first, phase 9 adds no `askableBy`, and its `Missing` variant feeds T6 as designed |
| The PC spec's Transcript tab and search (phase 19: each line joins search as it is spoken) | the reaction is one more traveller line: shown, searchable, never evidence |

Built in `docs/superpowers/plans/2026-09-29-traveller-personalities-plan.md`: V1 one wheel for every traveller; V2 personalities and the voice resolver; V3 the verdict reaction; V4 the cast's lines, the tone pass and the golden re-pack.
