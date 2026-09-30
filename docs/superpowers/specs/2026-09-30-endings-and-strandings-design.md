# Endings and strandings: the world you made (design)

*2026-09-30 · drafted by Claude from Saleh's direction of 2026-09-29 and his design rules of the same day · read-only map of `main` at `1679980` (`E:\unity\NOPE-p5`) · the code wins over this text · the traveller-types spec (`2026-09-26-traveller-types-design.md`, **T**) owns the kinds, forms and strandings' roll (S1-S3); the days 7-15 spec (`2026-09-29-days-7-15-design.md`, **D**) owns the beats and the morning papers of the second week; the personalities spec (`2026-09-29-traveller-personalities-design.md`, **P**) owns the voices. This spec owns what the run ends on, and what happens to a stranded traveller after the roll. A neutral patch (`fix/endings-neutral`, in parallel) drops Retirement and the attribute epilogues and makes day 15 a neutral "world you made" summary followed by END OF DEMO; this spec is the real system that summary grows into (§12). Every open choice is a multiple-choice question in §14; the build takes option A of each until Saleh answers.*

Saleh, verbatim (2026-09-29), on the endings:

> "this is linear and against my design. there are more than those 3 factors and even within those the game is not guiding to a state. we dont make judgements: democracy is not preferable to fascism or communism, nuclear future is not better than cybernetic or space age or naturalism or anarchy etc. you dont retire at the end of the game. the story takes place over a few weeks and the outcome depends on your choices."

On the strandings (a cheap Economy transponder fails and leaves an accepted traveller stuck in the past):

> "they dont always bring modern tech but you read in the internet for example about some of them, some will go forgotten, some will damage stability and you will be fined if they didnt sign a waiver which they promise not to reveal info or alter history, or time police will come kill them."

His design rules of the same day, which override any text here that disagrees:
1. One penalty for any wrong decision, approval or rejection alike, after one tunable free warning a day (`GameConfigSO.freeWarningsPerDay`, 1). A premade's extra stability loss stays: "this is a different consequence".
2. Personalities, not traveller types, drive lines and reactions; a believable but funny, absurd world.
3. Interactions are the same for every traveller type; only the context and the reply change.
4. Balance knobs are editable by Saleh in the engine, and authored cases (premades, forced slots, story beats) stay outside the random draws.

And from the same evening: never show the player the numbers behind a hidden mechanic (chances, percentages).

## 0. The map: what exists and what this spec does with it

| What (where) | Today (`main` `1679980`) | This spec |
|---|---|---|
| The run's end (`EndingRules`, `EndingService`, `Assets/Data/Endings`) | Fired and Bankrupt at any check; Retirement ("Quarter's End") at day 15's night; an attribute epilogue (Art, Science, Democracy totals against fixed bars, `AttrTotalAtLeast`) replaces Retirement when its bar is reached | Fired and Bankrupt unchanged. The run's end becomes **the World Report**: no bar, no winner, no Retirement; a description of 2150 as the player's choices left it, across several **world factors**, each answered by one of several equal **outcomes** (§3-§5). The epilogue assets retire; the enum values stay (serialized) |
| The world's state (`WorldState.timeline`, `HistoryState`) | three global attribute totals (Democracy, Science, Art), per-place attribute deltas, nation scores, the influence ranking and the leader (the present culture), latched fact edits, pending carries | kept as they are (they drive the leader and history); **new**: the pulls each factor's outcomes have gathered (`WorldState.pulls`), a permanent stranding log, three run counters (§2) |
| Strandings (`Strandings.Roll`, `ShiftStrandings`, `HistoryService.RecordStranding`) | 8 % per accepted Economy traveller at the shift's end; every stranding carries the present's Technology into the destination and prints `news.stranded` the next morning; no money | the roll is **unchanged** (same stream, same draws); each stranded traveller then gets **one fate** from a weighted table (forgotten, in the news, brings 2150 technology, shakes the timeline, the Time Police), weighted by whether a valid signed waiver is on file (§6) |
| The Stranding Waiver (TC-310, T §3.3) | carried by poor tourists and labourers, handed over signed or "UNSIGNED" or not at all; missing or unsigned is a directive fault (deny without evidence) | the printed declaration gains Saleh's promise ("I will not reveal the future or alter history"); the clerk may have a missing or unsigned waiver **signed at the desk**; a stranded traveller without one leaves the breach with the desk (§7) |
| The stranding fine (T S3) | retired by phase 23 (rule 1) | the tension with rule 1 is set out in §7.4; the build takes Q10's option A |
| FEATURES lines | 12 (Endings), 145, 152, 160 (strandings and carries), 176 ("endings come from attribute totals") | rewritten by the phases of §11 |

## 1. Goals

1. **The run ends on a description, never a verdict.** The last screen says what 2150 became (who runs it, what it runs on, how it pays its way, whose culture leads, what the past now holds, where the people you met ended up, and where the clerk stands), and never says whether that is good.
2. **More than three factors, each with many equal answers.** Governance is not a Democracy bar going up or down: it is Directorate, Democracy, Monarchy, Fascism, Communism or Theocracy, all written with equal care, each with its upsides and its absurdities.
3. **The outcome is the sum of choices over the weeks.** Every traveller the clerk lets through, every famous person sent home or kept, every dialog choice, bribe and stranding leaves a trace; the ending reads them all.
4. **The world changes where the player can read it**, in words (the morning paper, the Internet's History site), never as a meter.
5. **Strandings have varied, readable fates**: most are never heard of again, some make the news, a few change history or shake the timeline, and the Time Police clean up some; the waiver decides who answers for it.
6. **Everything is data** Saleh edits: the factors, outcomes, leanings, pulls, fate weights and lines are spreadsheet rows; the few tuning scalars are Inspector fields.

Non-goals: no ending ranks runs; no "good" or "bad" ending exists beyond the two failures (Fired, Bankrupt), which are about the clerk, not the world; no new traveller kind, verb or wheel entry; no change to the stranding roll or who travels.

## 2. The world factors an ending reads

### 2.1 What the run holds today (inventory)

| Factor | Where it lives today | Saved | Kind of value | Enough for the ending? |
|---|---|---|---|---|
| Global attribute totals (Democracy, Science, Art) | `timeline` scores `attrTotal:{attr}`, written on every accept (`TimelineService.ApplyVerdictImpacts`) | yes | three signed numbers; a Soldier *lowers* Democracy | **No**: a scale with a direction is exactly the judgement Saleh rejected ("democracy is not preferable to fascism") |
| Per-place attribute deltas | `attr:{profile}:{attr}` / `attr:{nation}@{era}:{attr}` | yes | signed numbers per place | feeds influence; not an outcome |
| Influence ranking and the leader (the present culture) | `HistoryState.ranking`, `leaderId`, `leaderSinceDay` | yes | one of eight lineages, or none | **Yes**: already categorical and unranked ("whose culture leads 2150") |
| The present (the leader's Future place: currency, language, technology, capital, ruler) | `ContentLibrarySO.BuildPresent` | derived | names | Yes, as flavour (the report quotes the present's currency and capital) |
| The rewritten past | `HistoryState.factEdits` (cause Rule or Carry, source, since day) | yes | a list of edits | **Yes**: "The past you rewrote" section |
| Beats and famous travellers | flags `premade:{id}:met / accepted / denied`, story rules' fired flags, dialog flags (`bribe:rook:taken`) | yes | flags | **Yes**: "Where are they now" section |
| Strandings | counter `stranded`; `HistoryState.pendingStrandings` cleared after the morning paper | counter only | a number | **No**: who, where and what became of them is lost after one night |
| Panics (costume errors accepted) | `HistoryState.pendingPanics`, cleared after the paper | no run total | – | **No** (a counter is enough) |
| Debt Relief departures | `debtReliefYesterday` (the last shift only) | yes | a number | **No** (needs a run total) |
| The displaced who stayed in 2150 (denied, so never sent home) | nothing, except a premade's `denied` flag | – | – | **No** (needs a counter; the famous are covered by flags) |
| Stability | `stabilityHundredths` | yes | 0-100 | as words only ("the timeline creaks") |
| The clerk (wallet, debt paid, citations, statement, house, family) | `money`, `clerkDebtPaid`, `totalCitations`, `accountDays`, `unlockedUpgradeIds`, `family` | yes | numbers and lists | **Yes**: "Your desk" section, in words |
| **Who runs 2150** (governance type) | nothing: the present's `Politics` fact is a ruler's *name* ("The Customs Directorate") | – | – | **Missing**: new factor (§3) |
| **What 2150 runs on** (nuclear, cybernetic, space age...) | nothing: `Technology` facts are device names ("Wrist comm") | – | – | **Missing**: new factor (§3) |
| **How 2150 pays its way** (the debt society) | nothing beyond the clerk's own debt | – | – | **Missing**: new factor (§3) |

### 2.2 What this spec adds to the saved run

| Addition | Shape | Why |
|---|---|---|
| `WorldState.pulls` | list of `{factor, outcome, amount}` | the weight each outcome has gathered (§4); the one new source of truth for the three new factors |
| `WorldState.leads` | list of `{factor, outcome, sinceDay}` | which outcome leads each factor since when, so the paper reports a change once (§5.1) |
| `HistoryState.strandingLog` | list of `StrandingRecord`, never cleared, with new fields `fate`, `waivered`, `fined`, `citizenId` | the report's strandings and the Mail memos (§6) |
| Counters (`WorldState.counters`, no schema change) | `panics`, `debtReliefTotal`, `displacedStayed` | the report's lines; readable by history rules too |
| `WorldState.worldReport` | the composed report (title, picture keys, sections of lines) written once when the run ends | the Title re-shows the same text even if content changes later |

## 3. Factors and outcomes

A **factor** is a question the ending answers about 2150. An **outcome** is one answer. Every factor has an **"as you found it"** outcome (2150 as the game starts), so a clerk who changes little still gets a real answer, not a failure. Outcomes are rows of content; none carries a sign, a score or a colour of approval.

| Factor (question) | Outcomes (first cut; every row is a spreadsheet row) | Where it comes from |
|---|---|---|
| **Who runs 2150?** (`government`) | The Directorate *(as you found it)*, Democracy, Monarchy, Fascism, Communism, Theocracy | new: pulls (§4) |
| **What does 2150 run on?** (`future`) | The Credit Age *(as you found it: wrist comms, queues, the debt grid)*, Nuclear, Cybernetic, Space Age, Naturalism, Anarchy | new: pulls; Saleh's own list of futures |
| **How does 2150 pay its way?** (`money`) | The Debt *(as you found it)*, Jubilee (every debt forgiven), Company Towns (employers run life), The Commons (shared and bartered), Banking Houses (credit from the merchant dynasties) | new: pulls |
| **Whose culture leads?** (`culture`) | none *(as you found it: the neutral Temporal Customs Zone)*, or one of the eight lineages | existing: the leader (`HistoryState.leaderId`); no new state |

Beside the four factors, the report carries four **sections** that are lists, not answers: the past you rewrote (fact edits), 2150 in the past (strandings and their fates, smuggled goods, panics), where they are now (beats, the famous, the displaced who stayed), and your desk (the clerk). §5.2 shows them.

Anarchy stands on the future line because Saleh listed it among the futures; moving it to "Who runs 2150?" is Q2.

**Each outcome row** holds: `factor`, `id`, `name` ("Space Age"), `adjective` ("Space-Age", for the composed title), `noun` ("Theocracy", government outcomes), `clause` ("where every debt was forgiven", money outcomes), `headline` (the paper's line the morning it takes the lead), `report` (two or three sentences for the ending, written with a joke and an upside for every outcome, never a verdict), `picture` (an art key, §5.3).

## 4. How choices over the weeks become an outcome, without ranking

### 4.1 Pulls

Every choice that shapes the world adds a **pull** to one outcome of one factor. No pull is ever negative: nothing pushes *against* an outcome, so no outcome is "the bad direction".

| Choice | Pull | Example |
|---|---|---|
| **The clerk accepts a traveller** (right or wrong) | the traveller's **role** decides the factor, the **destination** decides the outcome: the role's pull (content, `world.roles`) on the factor, toward the destination's **leaning** on that factor (content, `places[].leanings`), times the kind's scale (`GameConfigSO`) | Day 4: Hiroshi, a scientist, goes to Meiji Nagoya. Scientists pull the future line; Meiji Nagoya leans Cybernetic (the Toyoda automatic loom). Cybernetic gains 2 × 1 (a labourer's scale) = 2. |
| | a role that pulls nothing, or a destination with no leaning on that factor, adds nothing | A wanderer sent to Ottoman Ioannina changes no factor. |
| **The clerk denies a traveller** | a small pull toward each factor's "as you found it" outcome (`denialPull`, 0.5): the past stays untouched | Turning away a poor tourist bound for Babylonia adds 0.5 to The Directorate, The Credit Age and The Debt. |
| **A famous traveller** (a premade) | authored pulls on the premade row (`premades[].pulls`), used instead of the role's | Gutenberg accepted: The Commons +3 ("print for everyone"), Anarchy +2 (pamphlets). Senenmut accepted: Monarchy +3. |
| **A story rule fires** (a beat's consequence, a Directorate headline) | authored pulls on the rule (`history.rules[].pulls`), applied the night it fires | `audit_rook` (the bribe found): Company Towns +2. `drive_last_day`: The Debt +4. |
| **A dialog choice** | an effect op `PullOutcome` in the dialog's effect | Taking Rook's 50 cr: Company Towns +1. |
| **A displaced person is denied** (they stay in 2150) | counter `displacedStayed` +1 (report only), plus the denial pull | "Twelve people from other centuries now live in 2150." |
| **A stranding's fate** | none (a stranding already changes the past, §6) | – |

Roles are the archetypes every traveller already has (Artist, Diplomat, Merchant, Scientist, Soldier, Wanderer; `CaseBlueprintSO.archetypePool`). First cut of `world.roles`:

| Role | Factor it pulls | Pull |
|---|---|---|
| Scientist | What does 2150 run on? | 2 |
| Diplomat | Who runs 2150? | 2 |
| Soldier | Who runs 2150? | 2 |
| Merchant | How does 2150 pay its way? | 2 |
| Artist | none (artists already feed the leader through Art influence: the culture factor) | – |
| Wanderer | none | – |

Kind scale (Inspector, first cut): rich tourist 0.5, poor tourist 0.5 (a holiday leaves a lighter mark), labourer 1, displaced 1 (they go home for good). A famous premade's authored pulls are not scaled.

### 4.2 Leanings (the first cut, one spreadsheet cell each)

A place's **leaning** is what a traveller sent there brings back into the drift of history: where an idea has roots, not a verdict on the place. Blank = the place pulls nothing on that factor. Every cell is a first cut for Saleh to edit in the `placeLeanings` sheet before the content lands (phase E1).

| Place | Who runs 2150? | What does 2150 run on? | How does 2150 pay? |
|---|---|---|---|
| New Kingdom Egypt | Monarchy | Naturalism | The Commons (the grain stores) |
| Mamluk Egypt | Theocracy | Space Age (the astrolabe makers) | Banking Houses |
| Ottoman Egypt | Monarchy | – | Banking Houses |
| Khedivate of Egypt | Monarchy | – | Company Towns (the canal company) |
| Nasser's Egypt | Communism | Nuclear | Jubilee (land reform) |
| Babylonia | Theocracy | Space Age (the star tables) | Jubilee (the kings' debt cancellations) |
| Abbasid Baghdad | Theocracy | Cybernetic (the Banu Musa automata) | Banking Houses (the cheque, *sakk*) |
| Ottoman Baghdad | Theocracy | – | – |
| Ottoman Iraq (Baghdad Vilayet) | – | Anarchy | – |
| Kingdom of Iraq | Monarchy | – | Company Towns (the oil company) |
| Periclean Athens | Democracy | Space Age | Jubilee (Solon's shaking-off of debts) |
| Byzantine Mystras | Theocracy | Anarchy | – |
| Ottoman Ioannina | Theocracy | – | – |
| Athens, Kingdom of Greece | Monarchy | – | – |
| Metapolitefsi Athens | Democracy | Anarchy | The Commons |
| Republican Rome | Democracy | – | Jubilee (the calls for new tables) |
| Florentine Republic | Democracy | Space Age | Banking Houses (the Medici bank) |
| Sforza Milan | Fascism | Cybernetic (Leonardo's mechanical knight) | Banking Houses |
| Bologna, Kingdom of Italy | Communism | Cybernetic (Marconi's wireless) | The Commons (the co-operatives) |
| Dolce Vita Rome | Democracy | Nuclear (the Via Panisperna boys) | – |
| Eastern Han Luoyang | Communism (the salt and iron monopolies) | Space Age (Zhang Heng's instruments) | – |
| Northern Song Kaifeng | Communism (the New Policies) | Space Age (Su Song's clock tower) | Banking Houses (the first paper money) |
| Late Ming Suzhou | – | Naturalism (the gardens) | Banking Houses |
| Qing Shanghai | – | Anarchy (the treaty port patchwork) | Company Towns |
| Beijing, People's Republic | Communism | Nuclear | The Commons |
| Kofun Yamato | Monarchy | Naturalism | – |
| Muromachi Kyoto | Theocracy (the temples) | Naturalism | – |
| Tokugawa Edo | Fascism (the military government) | Naturalism (the circular city) | – |
| Meiji Nagoya | Monarchy | Cybernetic (the automatic loom) | Company Towns (the zaibatsu) |
| Showa Tokyo | – | Cybernetic | Company Towns |
| Iron Age Britain | Communism (common land) | Naturalism | The Commons |
| Anglo-Saxon England | Democracy (the witan) | Naturalism | The Commons |
| Elizabethan England | Monarchy | Space Age (the navigators) | Company Towns (the chartered companies) |
| Victorian Britain | Monarchy | Cybernetic (Babbage's engine) | Company Towns (the mills) |
| Post-war Britain | Democracy | Nuclear | Jubilee |
| Germania | Communism | Naturalism | The Commons |
| Holy Roman Empire (Rhineland) | Theocracy | Anarchy (the patchwork) | – |
| Renaissance Nuremberg | – | Cybernetic (the clockwork makers) | Banking Houses |
| Wilhelmine Germany | Monarchy | Nuclear (Planck) | Company Towns |
| Weimar Berlin | Democracy | Nuclear (the physicists) | – |

The Fascism column is deliberately thin (two places, where the word and the military state have roots, not where atrocities happened); Saleh may widen it. The Future places are never destinations (T H2) and lean nowhere.

### 4.3 Which outcome answers a factor

At any moment, a factor's answer is read from its pulls by one pure rule (`WorldFactors.Lead`):

1. Every factor starts with its "as you found it" outcome at `statusQuoWeight` (Inspector, first cut 6): the world's inertia.
2. The outcome with the most pull leads if it is ahead of every other by more than `leadMargin` (first cut 2).
3. Otherwise the factor is **split** between its top two outcomes, itself an authored, equal answer ("2150 is split: launch rings over the Aegean, rewilded valleys along the Rhine"), never a failure.
4. A lead, once taken, is kept until another outcome passes it by the margin (the leader rule's hysteresis, `NationLeader`), so the answer does not flicker night to night.

Why this is not a ranking: no outcome has a bar to reach, no outcome is the default "good" one, and the answer is only ever "which of the equals has the most of your choices behind it". The same run can lean Cybernetic Theocracy, Space-Age Democracy or a split Naturalism; the game never says which is better.

**A worked example (three weeks).** Days 1-5 send many tourists to Periclean Athens and Victorian Britain; days 7-15's labourers go mostly to Industrial worksites (D A4). By day 15: government pulls Monarchy 21, Democracy 17, The Directorate 6 + 9 (denials) → Monarchy leads by 4 (> 2). Future: Cybernetic 26, Space Age 12 → Cybernetic. Money: Company Towns 18, Jubilee 17 → split (a 1-point gap). Culture: Britain leads. The report's title: **"The Cybernetic Monarchy"**, subtitle "under British culture, where the companies and the forgivers are still arguing". A player who had denied Rook, accepted Gutenberg and sent Pell to Athens would read another world, from the same rules.

### 4.4 What the player controls

Following the rules exactly still shapes the world: who the queue sends is seeded, but which travellers the clerk reaches before closing, and every beat's verdict, are the player's (goal 3). A player who wants another 2150 can bend the rules on purpose (let a scientist with a bad paper through to Meiji Nagoya) and pay the one penalty for it: the uniform fine is the price of a choice, as in the genre. Q4 offers the stricter reading (only deliberate choices move the world).

## 5. How the ending is presented

### 5.1 During the run (words, never meters)

- **The morning paper** prints a factor's `headline` the morning after an outcome takes its lead (or the split's line), capped with the history news (`GameConfigSO` "History" cap, the leader line first): "2150: the first launch ring opens over the Aegean. The Space League is hiring." Nothing is printed while nothing changes.
- **The Internet's History site** (Chronopedia) gains a **"2150 today"** page: each factor's current answer in one sentence (the outcome's `report` first line, or "undecided" for a split), whose culture leads, and the newest rewritten facts. No numbers, no bars, no "rising" arrows (goal 4; the mood rule).
- The office already themes itself by the leading culture (`CultureThemeService`); no new office change in this spec (a later art pass could hang each future's banner in the hall).

### 5.2 At the end (the World Report)

The run's end moment is unchanged: the night of the run's last day (`ContentLibrarySO.LastDay`, 15 in the demo; "a few weeks" in a full game), at sleep, after Home. Fired and Bankrupt keep their immediate checks. Then:

1. **The final paper.** A full-screen **Temporal Times, Day 16** front page in the leading culture's theme:
   - **Headline**: the composed title, "The {future adjective} {government noun}" ("The Cybernetic Monarchy", "The Space-Age Theocracy", "The Credit-Age Directorate" for a world as you found it), and a subtitle from the money outcome's clause and the culture ("under Japanese culture, where every debt was forgiven"). Every one of the 36 government × future pairs names itself from its parts: no pair is authored as special.
   - **The picture** (§5.3).
   - **Four short columns**, one per factor: the outcome's `report` text (a split prints both halves).
   - **The past you rewrote**: each latched fact edit in words, newest first, at most eight ("Florence prints with Chinese movable type. Victorian London has a glowing wrist."), from `HistoryState.factEdits`.
   - **2150 in the past**: the strandings by fate ("Three travellers were never heard from again. Pell Quimby is a legend of Periclean Athens. The Time Police closed two files."), smuggled goods and panics counted.
   - **Where are they now**: one line per beat and per famous traveller met, chosen by their verdict flags (authored lines on the premade rows, `premades[].fates`: accepted / denied / stranded), plus the displaced who stayed ("Twelve people from other centuries now live in 2150; one of them runs a print shop").
   - **Your desk**: the clerk in words, from the wallet, the debt paid, the citations, the house and the family ("Your account still reads Eligible. The kids have a radio now.") and the last line, always: **"Desk 3 opens at 9:00."** The clerk does not retire: the story stops, the job does not.
2. **The END OF DEMO card** (`EndingSO.closingCard`), as the neutral patch shows it.
3. **The Title** offers New Run and **"The world you made"**, which re-shows the saved report.

After **Fired** or **Bankrupt**, the failure card shows first (as today: the Labour Contract for Bankrupt), then the same paper under "The world you leave behind" (Q9).

The report never shows a score, a percentage or a rank. Counts of people and events are allowed ("three travellers"), because they are what happened, not how well.

### 5.3 Pictures

Layered, so the art budget does not grow with the combinations (Q8): one **background per future** (six: the Credit Age's queues, the nuclear skyline, the cybernetic city, the launch rings, the rewilded valleys, the patchwork) and one **banner per government** (six) hung in front of it, over the culture theme's colours. Twelve pictures cover all 36 pairs. Art keys `ending_future_{id}` and `ending_government_{id}` at the ending art path (`Assets/Art/Title/`); a missing picture leaves the title background, as `EndingSO.picture` does today. Listed in `docs/ART_ASSET_LIST.md` when phase E4 lands.

### 5.4 The endings' code, after this spec

- `EndingConditionType` keeps every value (serialized, append-only); `AttrTotalAtLeast` and `EndingKind.Epilogue` stay in the enum and in `EndingRules` (tested) but no asset uses them: the three epilogue assets retire (the neutral patch may already remove them).
- The run's end is one hand-authored `EndingSO` (`id` `world_report`, `DayAtLeast`, threshold = the last day, the neutral patch's asset), whose body the ending screen replaces with `WorldState.worldReport`, composed by a pure `WorldReport.Compose(ReportInputs)` when the run ends.
- FEATURES line 176 becomes: "the present culture is one of the World Report's factors".

## 6. Strandings: what becomes of a stranded traveller

### 6.1 The roll stays; a fate follows

At the shift's end, `Strandings.Roll` runs exactly as today (same `Seeds.ForStrandings` stream, one draw per accepted Economy traveller, `agency.strandChance` 8 %), so who is stranded never moves and the golden masters' strandings stay. Then each stranded traveller, in queue order, draws **one fate** on a new stream, `Seeds.ForStrandingFates(daySeed)` (salt "FATE"), from the fate table's column for their waiver: **waivered** (a valid, signed, registered waiver was on file when they left: carried, signed, its number on their account) or **not**.

### 6.2 The fate table (first cut; `agency.strandingFates[]`, one row per fate)

| Fate | What happens | Where the player reads it | Waivered weight | Not waivered weight |
|---|---|---|---|---|
| **Forgotten** | nothing: they blend in, or vanish | only the agency's failure memo in Mail (§6.3, Q12) | 40 | 20 |
| **In the news** (a legend) | they talk; the past remembers a stranger with impossible knowledge. No fact changes | the morning paper, from a line pool by era: "Byzantine chroniclers record a woman who spoke to her wrist and was answered."; Chronopedia adds the story to the place's article under "Curiosities" | 25 | 20 |
| **Brings 2150** (an invention) | today's behaviour: they carry the present's Technology into the destination (the carry path, `HistoryService.RecordStranding`); from the next day the books, papers and tells read it | the paper's carry line and `news.stranded`; the rewritten fact in the books ("Revised") | 20 | 20 |
| **Shakes the timeline** (a tremor) | stability loses `stability` percent of where it stands (first cut 1 %, the compounding rule, `StabilityRules.ApplyPercent`), applied at the shift's end | the paper: "TIMELINE TREMOR near Babylonia. The Directorate says it is nothing."; the stability readout | 10 | 15 |
| **The Time Police** | they are found and killed; nothing of 2150 stays behind | the paper, in the Directorate's dry voice: "TIME POLICE: an unregistered traveller was removed from Periclean Athens. The file is closed." (Q13) | 5 | 25 |

- An unsigned traveller is likelier to meet the Time Police: with no waiver on file, nobody has registered them in the past, so the agency cleans up.
- **Brings 2150** falls back to **In the news** when the carry cannot be made (the place already reads the present's Technology), so every fate row's line stays true.
- The fates, their weights, stabilities and line pools are spreadsheet rows; `news.stranded` stays as the line of a fate row with no pool.
- **Personalities tilt the fate** (rule 2): a personality row may name one fate it doubles (`personalities[].strandingFate`: Chatty → In the news, Grand → Brings 2150, Anxious and Glum → Forgotten, Stickler → none). A tilt multiplies that fate's weight before the draw; the draw count never changes.
- With only a fifth of strandings carrying technology, D X5's "2150 contamination" worry (several carries a run blurring the "no 2150 goods" check) shrinks without touching `strandChance`.

### 6.3 The agency's failure memo (Mail)

Every stranding, whatever its fate, sends the clerk one Mail message the next morning (a Mail source like the delivery notices, built from `strandingLog`, so nothing new is saved for it):

> **TRANSPONDER FAILURE REPORT** · Unit HP-40718 (Driftbox 3) failed in Periclean Athens. Traveller: Pell Quimby, 773-5512-08. Waiver SW-204817 on file: the traveller's debt of 212,000 cr passes to kin. Status: not recovered.

Without a waiver the last lines read "No waiver on file. Liability referred to Desk 3." and, when Q10's fine applies, "A fine of 25 cr has been charged." For a Forgotten fate this memo is the only trace (Q12).

## 7. The waiver as an agency form

### 7.1 What the waiver promises

TC-310 keeps its six rows (T §3.3: Signatory, Citizen ID, Transponder, Debt Passed to Kin, Waiver No., Signature). Its **printed declaration**, above the Signature row, becomes content (`agency.waiver.declaration`):

> "I travel on an Economy unit at my own risk. If I am stranded, I will not reveal the future or alter history, and my debt passes to my next of kin."

No new row and no new check: the promise is what the fates of §6 break or keep (Q16 offers a tick-box row instead).

### 7.2 Who answers for a stranding

| Waiver when they left | They are stranded and... | Who answers |
|---|---|---|
| valid and signed | Forgotten | nobody |
| valid and signed | In the news, Brings 2150, Shakes the timeline (the promise broken) | the traveller: their debt passes to kin (the memo); nothing for the clerk |
| none, unsigned or fake | Forgotten or the Time Police | nobody pays; the memo notes "no waiver on file" |
| none, unsigned or fake | In the news, Brings 2150, Shakes the timeline | **the desk** (§7.4) |

Note what "none, unsigned or fake" means in play: a missing or unsigned waiver is a directive fault (T PaperSet) and a fake one a lie (L3), and a poor tourist posing as rich (L1, L4) rides an Economy unit without one. **So every unwaivered stranding follows a wrong approval**, which the clerk already paid for at the stamp (the one penalty, or the day's free warning).

### 7.3 Getting it signed at the desk

Today a traveller whose waiver is missing or unsigned can only be denied. This spec lets the clerk fix it (Q14):

- **The pad.** A pad of blank TC-310s sits on the desk (a desk object beside the stamp; art key `desk_waiver_pad`). The clerk drags a blank onto the traveller, or picks the papers menu's existing **Waiver** request again after an unsigned or missing reply: the same entry for every traveller (rule 3).
- **The reply is the traveller's** (rule 2): most sign ("Where do I... there? Lovely."); some refuse in character, by a per-traveller draw on a new salted stream (`Seeds.ForWaiverSign`, "SIGN", seeded like the traveller's other streams) against their personality's `waiverRefusal` (content, first cut: Grand 0.5, Curt 0.3, Stickler 0.1 "Only after I read clause 9", the rest 0.05). A rich tourist on a Premium unit answers as today ("It's a Premium unit, I don't need one").
- **Signed at the desk** means valid: the desk issues the number and registers it on the account at once (the account's Forms on file row gains it), so the paper-set directive is met and approving is correct. Denying the traveller instead stays correct too: the rule says refuse unsigned papers, and the clerk is not obliged to chase anyone.
- **It cures only** the two directive faults `WaiverMissing` and `WaiverUnsigned`. A fake waiver (L3) is a lie: a fresh signature does not undo the forged number, which the clerk must still catch.
- **What it costs** (Q15): time. The shift clock runs on for as long as a spoken request takes (`GameConfigSO.waiverSignMinutes`, first cut the same as a question), so fewer travellers reach the desk before closing. No money changes hands.
- The beats keep their meaning: Pell's day-7 unsigned waiver (D §4.1) can now be signed at the desk, and she leaves on her honeymoon, the `pell_departed` branch D already writes.

### 7.4 The tension with "one penalty for any wrong decision"

Saleh's rule 1 (2026-09-29): "clerk is fined for any mistake on application the same either approval or rejection. we dont penalize based on the type of mistake." Phase 23 retired T S3's 150 cr stranding fine under it. His stranding note of the same evening: "you will be fined if they didnt sign a waiver".

The two meet in one case only (§7.2): a traveller the clerk wrongly approved without a waiver, who is stranded and then breaks the promise. The clerk has already paid the one penalty for that approval. The options (Q10):

- **A (built first).** The **same one fine, charged a second time by the failure memo**, only in that case: the amount is `wrongDecisionPenalty` (one knob, one number for every fine in the game, never a new amount), no free warning applies, and it shows on the statement's FINES cell and the shift ledger. It keeps "one fine" in the sense of one amount and honours "you will be fined"; it does charge one mistake twice when its consequence lands.
- **B.** No second charge: the memo names the stamp's fine ("cited on day 7") and nothing more. Rule 1 in its strictest reading; the waiver then bites only through the Time Police weight.
- **C.** No money: the breach costs stability instead (`agency.waiver.breachStability`, first cut 2 %), like the famous travellers' extra loss that Saleh kept as "a different consequence".
- **D.** A separate stranding fine of its own amount (T S3's 150 cr). Listed for completeness; it contradicts rule 1.

A switch in `GameConfigSO` (`waiverBreachConsequence`: `SecondFine`, `None`, `Stability`) holds the answer, so Saleh can change his mind in the Inspector without a rebuild.

## 8. Knobs

### 8.1 Content (`world_source.json`, Generate World, the spreadsheet through `ContentSheetMap`)

| JSON | Sheet (`ContentSheetMap`) | Columns | Notes |
|---|---|---|---|
| `world.factors[]` | `worldFactors` (new, key `id`) | `id`, `question`, `statusQuo` (Ref `worldOutcomes`), `order`, `splitLine` ({a} and {b}) | four rows; `culture` is marked `fromLeader` (its answer is the leader; no outcomes of its own beyond the "as you found it" row) |
| `world.outcomes[]` | `worldOutcomes` (new, key `factor`+`id`) | `factor` (Ref), `id`, `name`, `adjective`, `noun` (Omit), `clause` (Omit), `headline`, `report`, `picture` (Omit) | the six + six + five outcomes of §3 |
| `world.roles[]` | `worldRoles` (new) | `archetype` (an archetype id), `factor` (Ref), `pull` (Num) | §4.1's table |
| `places[].leanings[]` | `placeLeanings` (new child of `places`) | `factor` (Ref), `outcome` (Ref) | §4.2's table; a blank row is no leaning |
| `premades[].pulls[]` | `premadePulls` (new child of `premades`) | `factor`, `outcome`, `amount` | Gutenberg, Senenmut, ... |
| `premades[].fates` | `premades` columns | `fateAccepted`, `fateDenied`, `fateStranded` (Omit) | the "Where are they now" lines |
| `history.rules[].pulls[]` | `historyPulls` (new child of `historyRules`) | `factor`, `outcome`, `amount` | generated as the rule effect's `PullOutcome` ops |
| `agency.strandingFates[]` | `agencyStrandingFates` (new, key `id`) | `id`, `fate` (OneOf the `StrandingFate` names), `weightWaivered`, `weightUnwaivered`, `stability` (Omit) | §6.2's table |
| `agency.strandingFates[].lines[]` | `strandingFateLines` (new child) | `era` (Omit, Ref `eras`), `text` ({name}, {place}) | the paper's lines per fate, by era when given |
| `agency.waiver` | `agency` columns | `declaration`, `memoWaivered`, `memoUnwaivered`, `memoFine` | §6.3, §7.1 |
| `personalities[]` | `personalities` columns | `waiverRefusal` (Num, Omit), `strandingFate` (Omit, OneOf) | §6.2, §7.3 |
| `pc` | the History site's rows | the "2150 today" page's title and labels | §5.1 |

Every new field is omitted when blank, so days 1-15 and every existing row stay byte-identical (D N1). Then Tools > TimeDesk > Write Content Spreadsheet Template, and `docs/CONTENT_SHEETS.md` lists the new sheets.

### 8.2 Inspector (`GameConfigSO`, Generate World never writes it)

| Header | Field | First cut | What it does |
|---|---|---|---|
| World (the ending) | `statusQuoWeight` | 6 | the "as you found it" outcome's starting weight on every factor |
| | `leadMargin` | 2 | how far ahead an outcome must be to lead (else split) |
| | `denialPull` | 0.5 | a denial's pull toward each "as you found it" outcome |
| | `kindScaleRich`, `kindScalePoor`, `kindScaleLabourer`, `kindScaleDisplaced` | 0.5, 0.5, 1, 1 | a kind's scale on a role's pull |
| | `reportEditsShown` | 8 | how many rewritten facts the report lists |
| Strandings | `waiverBreachConsequence` | `SecondFine` | Q10's answer (§7.4) |
| | `waiverBreachStability` | 2 | the stability percent when the answer is `Stability` |
| | `waiverSignMinutes` | as a spoken question | the clock cost of a desk signature |

The balance simulation (`BalanceSimSettingsSO`) gains no knob; it reads these.

## 9. Save compatibility

- **Save version stays 3.** Every addition is additive (the pattern of `newsArchive`, `mailRead`, `clerkDebtPaid`): `pulls`, `leads`, `worldReport`, `HistoryState.strandingLog` and the new `StrandingRecord` fields load empty or at their defaults from an older save.
- **An older save continued mid-run** has no pulls. On load, when `pulls` is empty and the day is past 1, a one-time **seed from the saved per-place attribute deltas** (`WorldPulls.FromScores`) rebuilds an approximation: a place's Science delta (its magnitude) pulls the future line toward the place's leaning, its Democracy delta the government line; Art pulls nothing (the culture factor reads the leader). The money line starts at "as you found it" (merchants and artists share the Art attribute, so they cannot be told apart). The seed runs once; a later load finds pulls and skips it.
- **Strandings before the log**: the report counts them from the `stranded` counter minus the log's length, as "n earlier strandings the agency did not file".
- `StrandingFate` (Forgotten, News, Carry, Tremor, Police) and `WaiverBreachConsequence` are new serialized enums, append-only, pinned in `SerializedEnumsTests`; `EffectOpType.PullOutcome` is appended and pinned.
- A run that ended before this spec keeps its `endingId`; the Title shows its old ending card (no report is composed for it).

## 10. Test plan

EditMode, Domain first (the plan names each test):

| Test | What it pins |
|---|---|
| `WorldFactorsTests` | a pull lands on the destination's leaning for the role's factor, times the kind's scale; no leaning or no factor: nothing; a denial pulls each "as you found it" by `denialPull`; the lead needs more than the margin; a tie or a gap within the margin is a split of the top two; a held lead survives a gap within the margin (hysteresis); every factor starts at `statusQuoWeight`; no pull is ever negative |
| `WorldReportTests` | the title from the future adjective and the government noun for every pair of the first-cut content; the subtitle from the money clause and the culture; a split prints both halves; the sections' lines from fact edits, the stranding log, flags and counters; the report text holds **no digit next to %, no score and no rank word** from a forbidden list (best, worst, good ending, bad ending, win, lose, retire); "Desk 3 opens at 9:00." is last |
| `WorldPullsTests` | `FromScores` seeds only when pulls are empty and the day is past 1; magnitudes, not signs |
| `StrandingFatesTests` | the fate column by waiver status; a personality's tilt doubles one weight and never changes the draw count; `Carry` falls back to `News` when the carry cannot be made; the fate stream is apart from the roll's (the roll's draws unchanged, checked against the existing `StrandingsTests` vectors); the lines' tokens |
| `WaiverTests` | valid = carried, signed and registered; desk signing cures `WaiverMissing` and `WaiverUnsigned` only; the refusal draw per personality on its own stream; a Premium rich tourist is never offered one; the breach table of §7.2 for each `WaiverBreachConsequence` |
| `StrandingsTests` (existing) | the roll: unchanged vectors |
| `EndingRulesTests` (existing, extended) | the world report ending at the last day; failures first at any moment; no epilogue asset needed |
| `SeedsTests` | "FATE" and "SIGN" distinct from every salt |
| `SerializedEnumsTests` | `StrandingFate`, `WaiverBreachConsequence`, `EffectOpType.PullOutcome` |
| `ContentSheetMapTests` | the new sheets and columns round-trip; blank fields omitted |
| `HistoryChecksTests` / a new `WorldChecksTests` | the validator's rules: every factor has its "as you found it" outcome; every outcome's `headline` and `report` are filled; every leaning names an outcome of its factor; every role names an archetype; fate weights are not all zero in either column; every fate line holds its tokens |
| Save tests (`SaveFilesTests` pattern) | an older save loads with empty pulls, then seeds once |

The balance simulation (phase 23's `BalanceSimulation`) reports, per style over 50 runs: the World Report titles and how often each outcome leads each factor, the splits, the fates by kind and the fines they caused. **Watch lines** (variety, not a target state): every outcome leads its factor in at least one run of the 50; no title comes up in more than a quarter of perfect runs; the "as you found it" outcome leads in fewer than half. A watch line crossed is tuned in the leanings and the Inspector, never by making one outcome "harder".

Unity (one batched session, never launched without Saleh's OK): Generate World and the template; the golden play to day 15's ending (the report's paper, the pictures' fallbacks, END OF DEMO, the Title's "The world you made"); a forced-strandings day (the debug panel) showing each fate's paper line and the Mail memo; a desk signing with a Grand and a Sunny traveller; 1080p and 720p.

## 11. Implementation plan (phases, one commit per task, TDD in Domain)

| Phase | Tasks | Depends on |
|---|---|---|
| **E0** (built on `fix/endings-neutral`) | the neutral patch: Retirement and the attribute bars dropped; day 15 = "The World You Made" (`Ending_WorldReport`, id `world_report`): the four outcomes listed plainly + END OF DEMO, and after a failure the same page as "The world you leave behind" (Q9 A). **The seam E1 fills:** `world_source.json` `world.factors` (sheet `worldFactors`: `id`, `question`, `answer`, `foundAs`) → `ContentLibrarySO.World` (`WorldContent`, checked by `WorldContent.Problems` in Generate World and the validator) → `ContentLibrarySO.WorldOutcomes(history)` → `WorldFactors.Lines` → `OutcomeLine`s on the Title (`TitleUIController.ShowWorld`) and in the balance summary. Today `FactorAnswer.AsFound` reads `foundAs` (government, future, money) and `FactorAnswer.Leader` the leading nation (culture); E1 appends a `FactorAnswer` value that reads the factor's outcomes and pulls (a split being its own answer), adds the outcome rows beside the factor rows, and E4 replaces the page with the composed report, keeping the asset, its closing card and the timing | – |
| **S1 Stranding fates** | `StrandingFate`, `StrandingFates.Pick` and `Seeds.ForStrandingFates` (tests first); `StrandingRecord` fields and `strandingLog`; `ShiftStrandings` draws the fate and applies it (carry, tremor, lines); `agency.strandingFates` content, sheet, generator, validator; the Mail memo source; FEATURES 145, 152, 160 | – |
| **S2 The waiver** | the declaration text; `Waiver.IsValid`, the desk-signing rule and `Seeds.ForWaiverSign` (tests first); the pad and the repeated Waiver request in the wheel (the builder); personality refusal lines (content, P's voice rows); registration on the account; `waiverBreachConsequence` and the fine path (statement, ledger); FEATURES 139, 152 | S1 |
| **E1 Factors as content** | `world` block, leanings, roles, premade and history pulls in `world_source.json`; `ContentSheetMap`; Generate World writes a `WorldFactorsSO` into the content library; the validator's `WorldChecks` | – |
| **E2 Pulls** | `WorldFactors.Pull`, `Lead` (tests first); `WorldState.pulls` and `leads`; pulls recorded in `DayCycle.Decide` (one path for the game and the simulation, D B8), the `PullOutcome` op for dialogs and history rules; `WorldPulls.FromScores` on load; counters `panics`, `debtReliefTotal`, `displacedStayed` | E1 |
| **E3 In-run words** | the lead-change headlines in the morning paper (the history cap); Chronopedia's "2150 today" page (`HistoryPages`) | E2 |
| **E4 The World Report** | `WorldReport.Compose` (tests first); `worldReport` saved at the run's end; the ending screen's paper layout (the Title scene's ending panel, scrolling sections, the layered picture, the closing card); the failure endings' second page; the Title's "The world you made"; the epilogue assets retired; FEATURES 12, 176; `ART_ASSET_LIST.md` | E2, E0 |
| **E5 Balance** | the simulation's report of titles, outcomes, fates and fines; a first tuning pass of the leanings and knobs against the watch lines | E4, S2 |
| **E6 Unity** | the one batched session of §10 | all |

S1-S2 and E1-E2 can run as two parallel tracks; they meet at E4 (the report reads the stranding log).

## 12. Seams

| With | What this spec assumes or asks |
|---|---|
| The neutral patch (`fix/endings-neutral`) | its day-15 "world you made" summary and END OF DEMO are the frame; E4 replaces the summary's content with the composed report and keeps its asset, closing card and timing |
| D (days 7-15) | D E1 and E2 (Retirement's rules, re-derived epilogue thresholds) are superseded; D §7's Retirement copy retires with the milestone framing. D's story rules gain pulls (§4.1); the `news.debt` line "A stranded tourist's family inherits 212,000 cr." now echoes the memo; D X5's contamination count reads only the Carry fate |
| T (traveller types) | S1's roll unchanged; S2 (every stranding carries) becomes one fate of five; S3 stays retired, with §7.4's switch in its place; F3's waiver form keeps its rows; the PaperSet directive's missing and unsigned waiver become curable at the desk (§7.3) |
| P (personalities) | two columns on the personality rows (`waiverRefusal`, `strandingFate`) and the desk-signing replies in the voice rows (sign, refuse, a Stickler's clause) |
| The PC redesign | the Mail memo is a Mail source; the "2150 today" page is Chronopedia's; the Temporal Times renders the report's paper styles |
| Phase 23 (balance, stability) | `StabilityRules.ApplyPercent` for tremors; `wrongDecisionPenalty` is the one amount any second fine uses |
| Portals and the House | none: the report reads the house upgrades and family as they are |

## 13. Intent audit

**Against Saleh's words.**
- *"this is linear":* no bar, threshold or total is compared with a target; outcomes are answers chosen by which has the most of the player's choices behind it (§4.3).
- *"more than those 3 factors":* four factors (government, future, money, culture) and four report sections (the rewritten past, 2150 in the past, the people, the desk), all data, so more factors are rows.
- *"not guiding to a state ... we dont make judgements":* no outcome is a default or a goal; no pull is negative; the report forbids rank words and scores (tested); every outcome's text is written with an upside and a joke; the paper and Chronopedia use words, never meters.
- *"democracy is not preferable to fascism or communism, nuclear future is not better than cybernetic or space age or naturalism or anarchy":* all named outcomes exist side by side (§3).
- *"you dont retire":* the report ends "Desk 3 opens at 9:00."; Retirement's framing retires.
- *"a few weeks ... the outcome depends on your choices":* the end is read at the run's last day, whatever its length; every accept, denial, beat, dialog and stranding counts (§4.1).
- *Strandings:* "they dont always bring modern tech" (one fate in five), "you read in the internet" (the paper and Chronopedia), "some will go forgotten" (Forgotten), "some will damage stability" (Tremor), "fined if they didnt sign a waiver which they promise not to reveal info or alter history" (the declaration, §7.2, §7.4), "time police will come kill them" (Police).
- *Rule 1:* the only fine is the one penalty's amount; the one case where it could be charged again is exposed as Q10 with a switch.
- *Rule 2:* refusals and fates tilt by personality, never by kind.
- *Rule 3:* the desk signature reuses the papers menu's Waiver entry for every traveller.
- *Rule 4:* every table is a spreadsheet sheet; every scalar an Inspector field; beats' pulls are authored on their rows, outside the random draws.
- *Never show the numbers:* no pull, weight, chance or percentage reaches the player.

**Redo or override checks.**
- Overrides D E1-E3 and FEATURES 12's epilogues and thresholds (Saleh's rejection); the neutral patch does the removal, this spec the replacement.
- Overrides T S2 (every stranding carries) with a fate table; the roll (S1) and its stream are untouched, so no traveller or stranding moves.
- Relaxes the PaperSet directive: a missing or unsigned waiver may be cured at the desk (Q14); denying stays correct, so no verdict that is right today becomes wrong.
- Reintroduces a stranding-linked charge only behind a switch whose first option re-uses the one amount (Q10), after phase 23 retired T S3.
- Does not reuse the Democracy, Science and Art totals for the ending: they stay the influence engine (leader and history rules), unchanged.

**No-code-path checks.** Factors, outcomes, leanings, roles, pulls, fates, lines, memos and refusal chances are content through Generate World; the knobs are `GameConfigSO` fields. New code is limited to what data cannot express: the pull and lead rules, the report's composition and screen, the fate draw and its effects, the desk signature and the waiver's validity, the breach switch, the save fields and the one-time seed. Rejected as duplicates: a second news mechanism (lead lines use the paper's history lines and cap), a second stability rule (tremors use `ApplyPercent`), a second history path (the Carry fate is today's carry), a new fine amount.

## 14. Open questions for Saleh

Each question lists the recommended option first (A). **Status (2026-09-30): all sixteen are answered**; each question's line carries the answer and §16 records Saleh's words. Where an answer differs from the text of §1-§13, §16 wins.

1. **What should the ending describe about 2150?** *(answered: A, and for now no ending story (§16.1))* Example of a finished run: "Japanese culture, a Cybernetic future, a Theocracy, every debt forgiven."
   - **A (recommended)** Four things: who runs 2150, what 2150 runs on, how 2150 pays its way (the debt), and whose culture leads (already in the game).
   - **B** Three things: who runs it, what it runs on, and whose culture leads (drop the money question).
   - **C** Five things: A plus "what 2150 believes in" (faith, art, fun), a new line to write.

2. **Which governments can 2150 end up with?** *(answered: 8 or 12 options, the list lives in config (§16.1))*
   - **A (recommended)** The Directorate (how 2150 is at the start), Democracy, Monarchy, Fascism, Communism, Theocracy.
   - **B** A, plus Anarchy moved here from the futures list (so "no government" is a government answer, not a future).
   - **C** A shorter list: The Directorate, Democracy, Fascism, Communism.

3. **Which futures can 2150 end up with?** *(answered: many combinations, the outcome lists live in config and the combos are calculated (§16.1))*
   - **A (recommended)** The Credit Age (how 2150 is at the start: wrist comms and queues), Nuclear, Cybernetic, Space Age, Naturalism, Anarchy.
   - **B** A, plus a Clockwork future (brass and steam, from Victorian London and Renaissance Nuremberg).
   - **C** Only your five (Nuclear, Cybernetic, Space Age, Naturalism, Anarchy): 2150 always ends up changed, never as it started.

4. **How does a traveller change the world?** *(answered: A, plus famous travellers have big impacts and story beats offer opportunities and surprises (§16.1))* Example: on day 4 you approve a scientist going to Meiji Nagoya, home of the automatic loom.
   - **A (recommended)** Every traveller you let through nudges 2150 toward what their destination is known for, through their job: that scientist nudges the future toward Cybernetic. Even a by-the-book clerk shapes the world through who comes to the desk; breaking a rule on purpose (and paying the fine) steers it further.
   - **B** Only your deliberate choices count: letting a rule-breaker through, turning an honest person away, and dialog choices. A perfect by-the-book run leaves 2150 as it started.
   - **C** Only the story characters (Pell, Gutenberg, Rook...) and dialog choices change the world.

5. **Should the player see the world changing during the run?** *(answered: A, widened: the change shows in everything that can change, never the variables (§16.1))*
   - **A (recommended)** Yes, in words only: the morning paper reports when something takes over ("The first launch ring opens over the Aegean"), and the Internet's History site has a "2150 today" page. No bars, no numbers.
   - **B** No: the world is a surprise on the last day.
   - **C** Yes, with bars for each outcome (not recommended: bars invite chasing a score, the linear feel you rejected).

6. **What if two outcomes are neck and neck at the end?** *(answered: A (§16.1))* Example: Space Age and Naturalism end one pull apart.
   - **A (recommended)** The ending says the world is split, as its own answer: "Launch rings over the Aegean, rewilded valleys along the Rhine."
   - **B** 2150 stays as it started on that question until one side clearly leads.
   - **C** Whichever of the two took the lead most recently wins.

7. **What does the last screen look like?** *(answered: for now no ending story; day 15 ends the demo showing the four outcomes (§16.1))*
   - **A (recommended)** The last morning paper (the Temporal Times, day 16): a headline naming your world ("The Cybernetic Monarchy"), a picture, one short column per question, then "The past you rewrote", "Where are they now" (Pell, Ines, Gutenberg...), "Your desk" (the clerk, still working: "Desk 3 opens at 9:00."), then END OF DEMO.
   - **B** One illustrated card: the world's name and one paragraph.
   - **C** A short slideshow, one slide per question, then the people, then END OF DEMO.

8. **Pictures for the ending.** *(answered: later, layered (§16.1))*
   - **A (recommended)** Twelve pictures layered: one background per future (6) and one banner per government (6) in front of it. Every combination is covered.
   - **B** One picture per combination (36 or more), the richest and costliest.
   - **C** No pictures in the demo: text on the paper only.

9. **When the clerk is fired or goes bankrupt, what comes after the failure screen?** *(answered: A (§16.1))*
   - **A (recommended)** The same last paper, titled "The world you leave behind": the world you shaped still exists.
   - **B** Only the failure screen, as today.

10. **The waiver fine and your "one penalty for any mistake" rule.** *(answered: D, 100 cr, a knowing exception to the one-fine rule (§16.2))* Example: Pell arrives with an unsigned waiver and you approve her anyway. That approval is a wrong decision, so you pay the one penalty (25 cr) at the stamp. Two days later her cheap unit fails in Athens and she tells everyone about wrist computers, breaking the waiver's promise she never signed. Should that cost you again?
    - **A (recommended)** Yes, the same one fine (25 cr, the same setting as every other fine), charged again by the agency's failure report, only in this case. No new fine amount exists anywhere.
    - **B** No: the fine at the stamp was the whole price; the report only reminds you of it.
    - **C** No money: it costs timeline stability instead, like the extra loss for getting a famous traveller wrong.
    - **D** A separate, bigger stranding fine (like the old 150 cr). This breaks the one-penalty rule.

11. **How often does each fate happen?** *(answered: A (§16.2))* (For you only; the player never sees these numbers.)
    - **A (recommended)** With a signed waiver: forgotten 40 %, in the news 25 %, brings 2150 technology 20 %, shakes the timeline 10 %, Time Police 5 %. Without one: forgotten 20 %, news 20 %, technology 20 %, shakes 15 %, Time Police 25 % (nobody registered them, so the police clean up).
    - **B** All five equally likely, with or without a waiver.
    - **C** A's first column for everyone: the waiver only decides who pays, not what happens.

12. **What is left of a "forgotten" traveller?** *(answered: A (§16.2))*
    - **A (recommended)** Only the agency's failure report in the clerk's Mail ("Unit HP-40718 failed in Periclean Athens. Traveller: Pell Quimby. Status: not recovered."), never mentioned again.
    - **B** Nothing at all: no mail, and the shift report stops counting them.
    - **C** A line in the paper: "No trace was found of a traveller lost in Periclean Athens."

13. **How does the paper report the Time Police?** *(answered: A (§16.2))*
    - **A (recommended)** In the Directorate's dry voice, so the killing is clear but deadpan: "TIME POLICE: an unregistered traveller was removed from Periclean Athens. The file is closed."
    - **B** Openly: "TIME POLICE execute a stranded traveller in Periclean Athens."
    - **C** They arrest instead of kill: "...was brought back to 2150 in cuffs."

14. **Can the clerk get a waiver signed at the desk?** *(answered: A (§16.2))* Example: Pell hands over her waiver unsigned.
    - **A (recommended)** Yes: you hand her a blank waiver from a pad on the desk. Most travellers sign; some refuse in character (a Grand traveller: "I do not sign things. People sign things for me."). Once signed you can approve her correctly; turning her away stays correct too.
    - **B** No: travellers must arrive with it signed; unsigned means deny (today's rule).
    - **C** Yes, and the clerk may even sign it for them: a forgery that saves time but counts as a wrong approval if an auditor checks.

15. **What does getting a waiver signed cost the clerk?** *(answered: A (§16.2))*
    - **A (recommended)** Only time: the shift clock runs on about as long as asking a question, so fewer travellers reach the desk before closing.
    - **B** Blank waivers are a desk supply you buy from the Orders app (for example a pad of ten for 20 cr).
    - **C** The traveller pays a waiver fee; it costs the clerk nothing, but some travellers refuse to pay it.

16. **Where is the waiver's promise ("I will not reveal the future or alter history") written?** *(answered: A, plus a death clause in the fine print (§16.2))*
    - **A (recommended)** In the waiver's printed text above the signature, next to "my debt passes to my next of kin". Nothing new to check: the signature covers it.
    - **B** As a separate tick box on the waiver that can be left empty, a new thing for the clerk to check.

## 15. Build record: the world's outcomes (phases E1-E3, branch `feat/world-outcomes`, 2026-09-30)

Built overnight on Saleh's answers of 2026-09-30 (four factors; 8 governments with room for 12; "many combinations, the game is config based, we will calculate the combos"; every traveller nudges, the famous nudge hard, beats bring opportunities and surprises; the world shows in words, never variables; a split is its own answer; the fired and bankrupt runs show "the world you leave behind"), on top of the neutral patch E0 (main `5f782f4`). Where this section and §1-§14 differ, this section records what was built; every choice made without Saleh is listed in §15.3 for his review.

### 15.1 What was built

| Piece | Where |
|---|---|
| The rules: pulls, the lead with hysteresis and the split, the night's latch, the one-time seed of an older save, the answer in words | `Assets/Scripts/Domain/WorldPulls.cs` (tests: `WorldPullsTests`) |
| The content types and checks: `FactorAnswer.Pulls` (appended, pinned), the factor's `statusQuo`, `splitLine`, `splitHeadline`; `WorldContent.outcomes` and `roles`; `Problems` and `RefProblems`; `JudgingWords` (whole words) and the percentage check; the outcome lines with their reports | `Assets/Scripts/Domain/WorldFactors.cs` (tests: `WorldFactorsTests`) |
| Content | `world_source.json` `world.factors` (three `Pulls` rows), `world.outcomes` (19 rows), `world.roles` (4 rows: scientist 4, diplomat 3, soldier 3, merchant 5, since §15.6), `places[].leanings` (40 places), `premades[].pulls` (21 famous travellers), `history.rules[].pulls` (28 rules), the UI string `site.history.today`; sheets `worldOutcomes`, `worldRoles`, `placeLeanings`, `premadePulls`, `historyPulls` (`ContentSheetMap`, `docs/CONTENT_SHEETS.md`) |
| Generation and validation | `WorldContentGenerator.World.cs` (the world block, `CheckWorldRefs`), the places' `leanings`, the premades' `pulls`, the history rules' `PullOutcome` ops; `ContentLibraryValidator.CheckWorldRefs` (leanings, premade pulls, every `PullOutcome` op in any effect, the roles' archetypes) |
| Knobs | `GameConfigSO` "World": `worldStatusQuoWeight` 6, `worldLeadMargin` 2, `worldDenialPull` 0.05 (the first cut 0.5 froze every simulated run, §15.5), `worldKindScaleRich` 0.5, `worldKindScalePoor` 0.5, `worldKindScaleLabourer` 1, `worldKindScaleDisplaced` 1 |
| Play | `WorldOutcomeService`: `RecordDecision` in `DayCycle.Decide` (the game and the balance simulation), `Latch` in `TimelineService.NightlyResolve` (after the story rules, before the carries, under the history news cap), `SeedOlderSave` on Continue; `EffectOpType.PullOutcome` (appended, pinned) applied as an instant op; Rook's bribe effect pulls Company Towns 1 |
| The END OF DEMO seam (E0) | `ContentLibrarySO.WorldOutcomes(world, config)` reads the pull factors now (`WorldContent.AnswersNow`), so the last shift counts; `DayCycle.EndRun` gives the run's last day its own night resolve before the page (E0's known gap: day 15's choices now shape the page); the fired and bankrupt runs' "the world you leave behind" reads the same answers (their last night's latch plus that day's decisions) |
| 2150 today | Chronopedia's present ends with a box per factor (question, answer, the answer's report), as the paper last reported it (`ContentLibrarySO.WorldLatched`, `SiteWorld.WorldToday`, `HistoryPages.Present`; tested: `HistoryPagesTests`) |
| Balance | the simulation's world section keeps E0's distribution per factor and per style and adds §10's watch lines (as found, splits, outcomes no run reached) and the mean pull a run gathers per factor |
| Saves | version stays 3; `WorldState.pulls` and `leads` are additive; an older save with neither, past day 1, is seeded once |

### 15.2 The outcome lists (content; a row each, any number)

- **Who runs 2150?** The Directorate *(as found)*, Democracy, Monarchy, Fascism, Communism, Theocracy, **Technocracy**, **The Corporate Board** (8; the config holds 12 or more: nothing in code counts them).
- **What does 2150 run on?** The Credit Age *(as found)*, The Nuclear Age, The Cybernetic Age, The Space Age, Naturalism, Anarchy.
- **How does 2150 pay its way?** The Debt *(as found)*, Jubilee, Company Towns, The Commons, The Banking Houses.
- **Whose culture leads?** the leader (unchanged from E0).

### 15.3 Decisions taken without Saleh (review these)

1. **Q1-Q6, Q9: option A** of each, with Saleh's Q2 answer (8 governments: A's six plus Technocracy and The Corporate Board, worded neutrally like the rest).
2. **No separate `WorldFactorsSO`** (§11 E1): E0 had already put the world block into `ContentLibrary_Main` (`ContentLibrarySO.World`), so the outcomes and roles join it there: one source, Generate World writes it, Saleh edits the sheets.
3. **Outcome rows carry `name`, `headline`, `report` only.** The composed title's `adjective`, `noun`, `clause` and the `picture` key (§3, §5.2-§5.3) belong to E4's World Report and are not added before a reader exists (no dead content).
4. **Leanings: §4.2's table as written, plus the blank government cells filled for the two new governments** (Saleh must review every cell):
   Late Ming Suzhou and Showa Tokyo lean **Technocracy** (the examination scholar-gentry; the ministry planners); Qing Shanghai and Renaissance Nuremberg lean **The Corporate Board** (the treaty port's municipal council of trading houses; the patrician merchant council). Ottoman Iraq's government stays blank. The Future places lean nowhere.
5. **Famous travellers' pulls (unscaled, 4 to 6; a role's is 2 times the kind's 0.5 or 1):** Senenmut Monarchy 6; Socrates Democracy 6; Aspasia Democracy 5; Ban Zhao Technocracy 5; Arib The Banking Houses 5; al-Khwarizmi Cybernetic 6; Gutenberg The Commons 6 and Anarchy 3; Leonardo Cybernetic 5 and Space Age 3; Lanyer Democracy 4; Gallerani Monarchy 4; Ada Lovelace Cybernetic 6; al-Tahtawi Technocracy 5; Umm Kulthum The Commons 4; al-Mala'ika Anarchy 4; al-Sayyab Naturalism 4; Elytis Space Age 4; Fellini Jubilee 4; He Zehui Nuclear 6; Toyoda Cybernetic 4 and Company Towns 4; Turing Cybernetic 6; Meitner Nuclear 6. The 2150 story characters (Pell, Ines, Rook, Hollis, the auditor) have no premade pulls: their beats pull through their story rules (5), and when accepted they pull like anyone through their role.
6. **Story and history rules' pulls (2 to 4, the night they fire):** movable_type_florence Commons 2; yen_reichsmark Banking Houses 2; roman_aljabr Technocracy 2; sterling_cairo Banking Houses 2; greek_thebes Democracy 2; mark_britain Company Towns 2; florin_mystras Banking Houses 2; hieroglyphs_babylon Theocracy 2; babbage_engine Cybernetic 3; athenian_steam Nuclear 2; elected_pharaoh Democracy 3; drive_begins The Debt 1; pell_departed Democracy 2; ines_mills Company Towns 4; ines_rebooked The Debt 2; rift_storm Anarchy 1 (a surprise); driftbox_recall The Directorate 1; rook_departed Company Towns 3; rook_complaint Democracy 2; ada_departed Cybernetic 2; audit_week The Directorate 1; range_limit Space Age 1; hollis_departed Jubilee 3; hollis_collections The Debt 3; audit_passed The Directorate 3; audit_failed Technocracy 2; audit_rook Company Towns 2 (§4.1); drive_last_day The Debt 2 (§4.1's 4, halved). The rules that fire in every run by the day (drive_begins, rift_storm, driftbox_recall, audit_week, range_limit, drive_last_day) pull 1 or 2 only: they fire whatever the player does, so a bigger pull acts as inertia; the rules that follow a choice (a verdict, a bribe) pull 2 to 4. Dialog: taking Rook's 200 cr pulls Company Towns 1 (§4.1).
7. **A denial pulls every pull factor's "as you found it" outcome by 0.05** (§4.1's 0.5 is a tenth too strong: a shift denies about half its travellers in the second week, and at 0.5 all 300 simulated runs ended as the run found 2150, §15.5), a displaced person's included; the `displacedStayed`, `panics` and `debtReliefTotal` counters (§2.2) are E4's report lines and are not added yet.
8. **The lead (§4.3)**, exactly: the "as you found it" outcome starts 6 ahead; a held single answer stays until another passes it by more than 2; otherwise the heaviest leads when ahead of the next by more than 2, else the factor is split between the top two (ties in content order). A split is re-read each night (no hysteresis of its own); a held single answer never turns into a split with its own challenger (the challenger must pass it first), so splits appear when a lead changes hands into a close race.
9. **The split's words** are the factor's own authored lines: "{a} and {b}, sharing the building" (government), "{a} and {b}, one on each bank of the river" (future), "{a} and {b}, side by side" (money), and a split headline each for the paper.
10. **Where the answers are read.** The END OF DEMO page and "the world you leave behind" read the answers *now* (the held answers' hysteresis applied to the latest pulls), so a decision of the last shift counts even when the night did not latch it; the last day also gets its own night resolve first (`DayCycle.EndRun`), so its story rules, leader and pulls land. A failure does not run a night (the world is left as it stood). Chronopedia's "2150 today" reads the latched answers (what the morning paper reported), so the site never announces a change the paper has not.
11. **The morning paper**: a changed answer's headline goes after the leader line and before the carries, under the one history news cap (3); nothing prints while nothing changes, and the first night's "as you found it" is saved silently.
12. **The older save's seed (§9)** runs only when the save has no pull *and* no latched answer (a run of this build latches every night, so it can never be re-seeded); which attribute seeds which factor is derived, not listed: an attribute seeds the factor of the one role whose archetype moves it (Science: the scientists, the future; Democracy: the diplomats and soldiers, the government); Art, moved by artists, merchants and wanderers alike, seeds nothing, so the money line starts as found (§9).
13. **Names.** E0 named its line builder `WorldFactors`, so the rules of §4 (`WorldFactors.Pull`, `Lead`, `WorldPulls.FromScores` in §11) live together in `WorldPulls`; the play glue is `WorldOutcomeService`.
14. **Roles: scientist 3, merchant 3, diplomat 2, soldier 2** (§4.1 has 2 each; raised again in §15.6 to scientist 4, diplomat 3, soldier 3, merchant 5): the government has two roles pulling it and the future and the money one each, so the single roles pull 3 and every factor gathers about as much over a run (§15.5).
15. **No ranking words**: `WorldFactors.JudgingWords` (best, worst, better, worse, good, bad, win, lose, score, rank, retire, triumph, success, fail, victory, defeat, ideal, utopia, dystopia and their listed forms) are matched as whole words so "goods" and "window" stay free; Generate World, the validator and `WorldFactorsTests` refuse any world text using one or printing a percentage.

### 15.4 Follow-ups (not built)

- **The world reflected everywhere** (Saleh: "the changing world shows in EVERYTHING that can change"): the PC's look and font, the languages, the city out of the window, the hall, the office's banners per government and future: a later epic, with the layered pictures of §5.3 (Q8).
- **E4, the World Report**: the composed title ("The Cybernetic Monarchy"), the four columns, the rewritten past, 2150 in the past, where they are now, your desk; `WorldState.worldReport`; the Title's "The world you made"; the counters of §2.2.
- **S1-S2, the strandings' fates and the waiver at the desk** (§6-§7): not in this track.
- **Tuning**: the balance simulation's watch lines (the report of this track) against the leanings and knobs; Saleh reviews the leanings (15.3 item 4) first.

### 15.5 Balance (the 50-run simulation, 2026-09-30, main `cfe6846` merged)

At the spec's first cut (a denial pulls 0.5, every role 2, the day-fired story rules 2 to 4) **all 300 simulated runs ended as the run found 2150** on all three factors: a shift denies about half its travellers in the second week, and every denial pulled all three "as found" outcomes. The build ships a first tuning pass (knob and content, all editable): a denial pulls 0.05, scientists and merchants 3 (so the future and the money gather about as much as the government's two roles), the day-fired story rules 1 or 2. The day-15 answers per style at the shift clock's pace (10 a shift, 50 runs each; a distribution, never a target):

| Style | Who runs 2150? | What does 2150 run on? | How does 2150 pay its way? |
|---|---|---|---|
| Perfect | Monarchy 27, Democracy 12, The Directorate 6, splits 3, Communism 1, Theocracy 1 | Cybernetic 21, Credit Age 11, Nuclear 6, Anarchy 5, Space Age 2, Naturalism 1, splits 4 | The Debt 29, Company Towns 10, Commons 6, Banking Houses 2, Jubilee 1, splits 2 |
| Imperfect | Monarchy 30, Democracy 11, Directorate 3, Theocracy 3, Communism 1, splits 2 | Cybernetic 21, Credit Age 9, Anarchy 5, Nuclear 5, Space Age 5, Naturalism 2, splits 3 | The Debt 20, Company Towns 17, Commons 7, Banking Houses 3, Jubilee 1, splits 2 |
| Careless (bankrupt or fired early) | Monarchy 30, Directorate 9, Democracy 5, Theocracy 2, splits 4 | Credit Age 37, Cybernetic 8, Anarchy 2, others 1 each | The Debt 35, Company Towns 7, Commons 4, Banking Houses 2, splits 2 |

Watch lines (§10) against this: every future and every way to pay leads in some perfect run at the pace; **Fascism, Technocracy and The Corporate Board lead in none** (two places lean each, 15.3 item 4) and **Monarchy leads more than a quarter of perfect runs** (twelve places lean Monarchy, five of the eight Industrial places the second week's labourers go to). Both are the leanings' to fix, Saleh's review first (15.3 item 4), not a knob's: no outcome is made "harder". The "as found" answers lead in fewer than half of the perfect runs on the government and the future, and in 29 of 50 on the money (the Drive's story rules and a slow merchant inflow). The whole-queue runs, with more travellers, drift further from "as found".

### 15.6 The leanings rebalance (branch `fix/world-leanings`, 2026-09-30): provisional, needs Saleh's review

After §15.5 the demo's world barely varied: Monarchy led 27 of 50 perfect runs at the pace, Fascism, Technocracy and The Corporate Board never led, and the money stayed The Debt in 29 of 50. A content-only rebalance (no code, no knob): the Industrial worksites' leanings spread over several governments and ways to pay, a few blank cells filled, and the roles' pulls raised so every factor's answers can outweigh "as found" by the second week. The rows were chosen with an offline model of the simulation's own runs (every place's traffic per run, a search over plausible options only: places where an idea has roots, never where atrocities happened), then checked in the simulation itself.

**Every changed row below is PROVISIONAL and needs Saleh's review.**

| Place | Factor | Was | Now | Why it has roots there |
|---|---|---|---|---|
| New Kingdom Egypt | government | Monarchy | Theocracy | the pharaoh as a living god; the temple estates |
| Khedivate of Egypt | government | Monarchy | The Corporate Board | the Suez Canal Company |
| Abbasid Baghdad | government | Theocracy | Technocracy | the House of Wisdom's scholars and translators |
| Abbasid Baghdad | future | Cybernetic | Space Age | al-Ma'mun's observatory and his measure of the Earth |
| Ottoman Iraq (Baghdad Vilayet) | government | none | The Corporate Board | the river steamship and railway companies |
| Ottoman Iraq (Baghdad Vilayet) | money | none | The Banking Houses | Baghdad's merchant banking families |
| Kingdom of Iraq | government | Monarchy | The Corporate Board | the Iraq Petroleum Company |
| Byzantine Mystras | money | none | The Commons | Plethon's proposals for land held in common |
| Florentine Republic | government | Democracy | The Corporate Board | the guilds (the Arti) that ran the republic |
| Qing Shanghai | money | Company Towns | The Banking Houses | the Shanxi banks and the treaty port's banks |
| Meiji Nagoya | government | Monarchy | The Corporate Board | the zaibatsu boards |
| Showa Tokyo | future | Cybernetic | none | spread: the Cybernetic Age already has six places and four famous travellers |
| Elizabethan England | money | Company Towns | The Banking Houses | the goldsmith bankers and Gresham's Royal Exchange |
| Post-war Britain | government | Democracy | Technocracy | the post-war planners and the new health service |
| Wilhelmine Germany | government | Monarchy | Technocracy | the Prussian civil service and the engineering houses |
| Weimar Berlin | future | Nuclear | Anarchy | Dada Berlin's patchwork |
| Weimar Berlin | money | none | Jubilee | the 1923 inflation erased every mortgage |

Roles (`world.roles`, provisional too): scientist 3 to 4, diplomat 2 to 3, soldier 2 to 3, merchant 3 to 5.

Not changed, on purpose: the Fascism list stays the spec's two places (Sforza Milan, Tokugawa Edo). The search tried Republican Rome (the fasces of its lictors) and it was taken out again: a byword for the republic should not lean that way to move a statistic. The result: **Fascism leads in no simulated run**. It stays reachable by the player's own choices (soldiers and diplomats sent to its two places), which the by-the-book simulation never makes. Widening it is Saleh's call (§4.2).

**Before and after** (the simulation's day-15 answers, 50 runs a style at the shift clock's pace, 10 a shift; each outcome's runs out of 50, splits apart):

| Style | Factor | Before (§15.5, `eab4718`) | After |
|---|---|---|---|
| Perfect | government | Monarchy 27, Democracy 12, The Directorate 6, Communism 1, Theocracy 1, splits 3 | Democracy 13, Monarchy 13, Theocracy 10, Technocracy 5, The Directorate 3, The Corporate Board 2, Communism 1, splits 3 |
| Perfect | future | Cybernetic 21, Credit Age 11, Nuclear 6, Anarchy 5, Space Age 2, Naturalism 1, splits 4 | Cybernetic 15, Anarchy 9, Space Age 8, Naturalism 6, Nuclear 5, Credit Age 3, splits 4 |
| Perfect | money | The Debt 29, Company Towns 10, Commons 6, Banking Houses 2, Jubilee 1, splits 2 | Company Towns 15, Commons 15, Banking Houses 13, The Debt 5, Jubilee 1, splits 1 |
| Imperfect | government | Monarchy 30, Democracy 11, Directorate 3, Theocracy 3, Communism 1, splits 2 | Theocracy 14, Democracy 12, Monarchy 11, Technocracy 5, The Corporate Board 2, Communism 1, The Directorate 1, splits 4 |
| Imperfect | future | Cybernetic 21, Credit Age 9, Anarchy 5, Nuclear 5, Space Age 5, Naturalism 2, splits 3 | Anarchy 12, Cybernetic 12, Space Age 12, Naturalism 5, Nuclear 4, Credit Age 1, splits 4 |
| Imperfect | money | The Debt 20, Company Towns 17, Commons 7, Banking Houses 3, Jubilee 1, splits 2 | Company Towns 17, Commons 16, Banking Houses 13, The Debt 1, Jubilee 1, splits 2 |
| Careless | government | Monarchy 30, The Directorate 9, Democracy 5, Theocracy 2, splits 4 | Theocracy 20, Monarchy 10, The Directorate 6, Democracy 5, Communism 2, splits 7 |
| Careless | future | Credit Age 37, Cybernetic 8, Anarchy 2, Nuclear 1, Space Age 1, splits 1 | Credit Age 25, Naturalism 6, Cybernetic 5, Space Age 5, Anarchy 3, Nuclear 1, splits 5 |
| Careless | money | The Debt 35, Company Towns 7, Commons 4, Banking Houses 2, splits 2 | The Debt 14, Commons 13, Banking Houses 11, Company Towns 6, Jubilee 6 |

Against the brief's targets:
- **Perfect and imperfect play**: the most frequent answer leads 24 to 34 % of runs (26 % for the government in perfect play), against about 70 % before.
- **Coverage**: every future and every way to pay leads in some run. Every government but Fascism does; The Corporate Board leads 2 runs at the pace (8 to 9 on the whole queue).
- **"As found"**: still possible (The Directorate in 1 to 6 runs, The Credit Age in 1 to 3, The Debt in 1 to 5 at the pace) but no longer dominant. The money moves off The Debt in 45 to 49 of 50 careful runs.
- **Careless play**: most of these runs end bankrupt or fired early. They weigh the Ancient and Medieval days (Theocracy 20/50) and leave the future as found in 25/50: few accepts, a short run.
- **The whole queue**: more travellers, so the world moves further. "As found" leads no run; the largest share is Cybernetic, 21/50.

## 16. Saleh's answers (2026-09-30)

Recorded from Saleh's answers to §14. They override the text of §1-§13 wherever the two disagree; the build of S1-S2 (branch `feat/strandings`) follows them.

### 16.1 Endings (Q1-Q9)

- **No ending story for now (Q1, Q7).** Day 15 just ends the demo showing the **four outcomes**: who runs 2150, what it runs on, how it pays its way, and whose culture leads. No World Report paper, no "Where are they now" story yet; the four answers, then END OF DEMO.
- **Governments (Q2): 8 or 12 options.** The list is config, not code.
- **Futures (Q3): many combinations.** The game is config-based: the outcome lists live in config (spreadsheet rows), and the combinations get calculated, never authored one by one.
- **Influence (Q4): every traveller nudges** (option A); **famous travellers have big impacts**; **story beats offer opportunities and surprises**.
- **Visibility (Q5): the change shows in everything that can change**: the paper, the websites, the PC, its font, languages, the city out of the window, the hall. **Never the variables** (no meters, numbers or bars).
- **Ties (Q6): a split world is its own answer** (option A).
- **Fired or bankrupt (Q9): "the world you leave behind"** follows the failure screen too (option A).
- **Pictures (Q8): later, layered** (a background per future and a banner per government in front of it).

### 16.2 Strandings (Q10-Q16)

- **Q10 = D: a separate stranding fine of 100 cr** (a knob, `GameConfigSO.strandingFine`), charged by the agency's failure report only when the stranded traveller had **no valid signed waiver** on file. **Saleh chose this knowingly as an exception to his rule "one penalty for any wrong decision"** (2026-09-29): the rule still governs every decision at the stamp (one amount, approval or rejection alike, after the free warning); the stranding fine is not a decision penalty but the agency billing the desk for an unregistered traveller lost in the past, a consequence that lands days later. It is the only fine in the game with its own amount. The switch of §7.4 (`waiverBreachConsequence`) is not built: the answer is fixed, the amount is the knob.
  - Scope, as built: the fine is charged for **every** fate of a stranding without a valid signed waiver (Forgotten and the Time Police included), because Saleh's answer ties it to the missing waiver, not to the promise being broken; §7.2's "nobody pays" rows for unwaivered strandings are superseded. No free warning applies to it.
- **Q11 = A: fate odds by waiver**, knobs (content rows): signed 40 / 25 / 20 / 10 / 5, unsigned 20 / 20 / 20 / 15 / 25 (Forgotten / In the news / Brings 2150 technology / Tremor / Time Police).
- **Q12 = A: a Forgotten traveller leaves only the agency's failure report in Mail.**
- **Q13 = A: the Time Police are reported dry and deadpan**, in the Directorate's voice.
- **Q14 = A: the clerk can get a waiver signed at the desk** from a pad; some travellers refuse in character; denying stays correct.
- **Q15 = A: it costs only shift-clock time**, about one question's worth.
- **Q16 = A, plus a death clause.** The promise not to reveal the future or alter history is in the printed text above the signature, **and the fine print carries a death clause** (authored wording, deadpan, in content): the signatory accepts that the Time Police may remove them from the past.
