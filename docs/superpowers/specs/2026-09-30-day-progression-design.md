# Day progression, the shift report and the citation slip: design

*2026-09-30 · wave 5, track C (`feat/day-progression`) · Papers Please lessons 4, 5, 6, 7 and D7 from Saleh's list of 2026-09-30 ("apply all 20 to the game with the new prototype") · decided and built by Claude for Saleh's review · the code wins over this text*

Lessons applied here:

- **4.** One new rule or check per day, announced in the morning briefing.
- **D7.** Documents arrive one day at a time.
- **5.** The shift report makes the speed-vs-accuracy money trade-off obvious.
- **6.** The citation slip names the exact rule and the exact values; the first mistake of a day stays free.
- **7.** Moral choices use the same verbs (the wheel, the stamp); no special UI.

## 1. What each day introduced before (the D7 map)

Read from `world_source.json` `days[]` at main `fe96fe7`:

| Day | New papers | New checks / rules | Other new things |
|---|---|---|---|
| 1 | Leisure Visa, Departure Manifest, Stranding Waiver, proof of means (the poor tourist's four at once) | the tourists' procedure line (every paper against the Citizen Account) | rich and poor tourists (poor honest), lies L1, L2 |
| 2 | – | closures, the tourist paper set, dress for the destination (Costume Guide, costume errors) | lies L3, L10; answer channel; Medieval era |
| 3 | Labour Contract | labour paper set, debt standing | labourers, lies L4, L5; dress channel; Early modern |
| 4 | – | no 2150 goods (currency and device questions), paper dates | smuggling; Industrial |
| 5 | Displacement Certificate, Intake Declaration, Return Order | the displaced's procedure line, the return home (language, capital, ruler questions) | the displaced, lies L7, L8, foreign speech; Modern |
| 6 | – | – | the famous displaced (Senenmut, "Socrates") |
| 7-15 | – | recall (day 10), Economy range limit (day 13) | one authored story beat a day |

Days 1-5 brought 4, 3, 3, 2 and 5 new things at once. Validate Content Library on the old assets, with the new pacing check, reports all 13 problems (`SCRATCH/wave5/C/pacing_validator_before.txt`).

## 2. The re-paced ramp (after)

Each day brings at most one new paper and one new check. A paper and the procedure that asks for it arrive together, and the bulletin names them in one line. The eras, countries, queue sizes and every day 7-15 beat stay as they were.

| Day | Bulletin (the one new thing) | Papers issued | Kinds | New lies |
|---|---|---|---|---|
| 1 | the **Leisure Visa** against the Citizen Account | TC-101 | rich 1, poor 1 (honest) | L1, L2 |
| 2 | the **Departure Manifest** | + TC-230 | rich 3, poor 2 (honest) | – |
| 3 | **closed destinations** (the first closure: New Kingdom Egypt) | – | as day 2 | – |
| 4 | the **Stranding Waiver** (`Rule_TouristWaiverSet`: a Premium visa on a Premium unit, a Standard visa on an Economy unit with a signed waiver) | + TC-310 | rich 2, poor 2 | L3 fake waiver |
| 5 | the **proof of means** (`Rule_TouristPaperSet`, the full set) | + TC-415/416/417 | as day 4 | L10 forged proof |
| 6 | **the displaced**: the Displacement Certificate against the Registry | + TC-610 | + displaced 2 | – (Senenmut, story) |
| 7 | **dress for the destination** (Costume Guide) | – | as day 6 | costume errors (Pell, story) |
| 8 | **Debt Relief labourers**: Labour Contract, Economy unit, signed waiver | + TC-520 | + labourer 3 | L5 forged contract (Ines, story) |
| 9 | **debt standing** | – | – | L4 debtor as tourist (Gutenberg, story) |
| 10 | **transponder recall** (authored) | – | – | (Pell, story) |
| 11 | **no 2150 goods** (currency and device questions) | – | – | L6 smuggling (Rook's bribe, story) |
| 12 | the **Intake Declaration** and the return home (language, capital, ruler questions) | + TC-620 | – | L7, L8 (Ada, "Socrates", story) |
| 13 | **range limit** (authored) | – | – | (Hollis, story) |
| 14 | **paper dates** | – | – | (the auditor, story) |
| 15 | the **Return Order** | + TC-630 | – | (Pell, story) |

The tell channels follow the checks: papers only on days 1-6, dress from day 7, answers from day 11 (the first questions). Other gated content moves with its check:

- The currency and device questions start on day 11, the language, capital and ruler questions on day 12.
- Foreign speech starts on day 6, with the displaced.
- The checklist steps start on the day their check arrives.
- Violations are rolled from day 4 (0 before); costume errors from day 7.

## 3. Mechanisms

- **Papers in circulation**: `days[].papers` becomes `DayPlanSO.Papers`, an empty list meaning every form.
  - A traveller carries only the issued forms of their kind's blueprint (`DayPlanSO.TemplatesOf` in `CaseFactory.BuildDocuments`).
  - The papers menu offers only those forms (`TimelineService.DayForms`), and the desk's waiver pad needs the waiver issued.
  - A paper set never asks for a form not issued yet (`CaseFacts.Issued`, `Directives`).
  - The account draw is unchanged. The Citizen Account keeps its forms on file, so no random stream moves beyond the papers.
  - A later day never withdraws a paper (`DayPapers.Problems`).
- **Pacing check** (`DayPacing`): Generate World and the validator refuse any day that brings two new papers, two new directives, or something new without a bulletin.
  - A paper counts by its request id, so the three proofs of means count as one paper.
  - All the closures that apply to every kind count as one check. A closure for some kinds (the range limit) is a check of its own.
  - I enforced this on every day, not only days 1-5. The re-paced content passes it everywhere, so a story day needs no exception.
- **Bulletin**: `days[].bulletin` becomes `DayPlanSO.Bulletin`. It is the first line of the briefing, under "BULLETIN · NEW TODAY", in bold. It is also first in the News site's back issue.
- **Stranding fine gated by the waiver** (`Strandings.Fine`, `CaseInstance.waiverIssued`): before day 4 no waiver exists, so nobody could check one and no approval was wrong. Without this gate, the first balance run bankrupted 17 of 50 perfect-play runs on days 1-3 (see §6). The fate table still reads "no waiver on file".
- **"Socrates" moves** from day 6 to day 12, slot 11, and is pooled from day 13. His lie is a place lie and needs the place-lie channels that arrive on day 12. Senenmut (honest) keeps day 6, slot 8.
- **Procedure lines** no longer name papers: "Leisure departures: every paper a tourist hands over must match their Citizen Account." (and the same for the displaced). The paper-dates line reads "the date printed on your travel papers". So each line stays true while papers arrive.

## 4. Lesson 5: the shift report

`ShiftReport` (Domain; tested) reads the ledger, the queue size, the wallet and tonight's bill. The presenter prints amounts in one column (`<pos>`), with the net and the wallet after bills in bold:

```
Travellers processed: 10 of 12 (2 still waiting at closing, unpaid)

Right calls: 7 x 10 cr                         +70 cr
Wrong calls: 1, the free warning                 0 cr
Wrong calls: 2 x 25 cr fine                    -50 cr
Stranded in transit: 1, fined (no signed waiver) -100 cr
Debt Relief instalment (125,000 cr still owed)  -17 cr
Other money                                     +200 cr   (only when a dialog moved money: Rook's bribe)
Shift net                                       +103 cr
Bills due tonight                               -62 cr
Wallet after bills                          91 cr (now 153)
```

- **Speed**: travellers still waiting at closing are shown and unpaid.
- **Accuracy**: right calls times pay against wrong calls times fine, with the free warning on its own row.
- **Bills**: the same bill Home charges (`HomeEconomy.DailyExpenses`: rent, family, care, upkeep). A break-in is never foretold.
- **Nothing hidden**: the bribe used to change the wallet unseen. It is now `ShiftLedger.otherMoney`, and the net counts it.

## 5. Lesson 6: the citation slip

`CaseFactory` builds each traveller's `CitationFacts` at generation, with no draw. `ShiftScoring` prints them on a wrong call:

```
TIMELINE DEVIATION NOTICE
Approved incomplete paperwork.
Directive 3: This departure needs a signed Stranding Waiver.
Signature on the Stranding Waiver: UNSIGNED.
Warning 1/1 · no pay deduction.  Stability -0.50
```

- **The rule**: its row on today's Directive Memo, numbered as the memo numbers it (`Citations.MemoNumber`), then its line:
  - A closure's or a recall's own summary.
  - For the paper set, the broken condition (`Citations.PaperSetBreach`: Premium visa on an Economy unit, Standard visa on a Premium unit, missing contract, missing or unsigned waiver, missing proof).
  - The frozen standing, the departure date, the expired paper, the record check (under the kind's procedure line), the dress rule, the return home, or no 2150 goods.
- **The values**, each with where it was read:
  - The destination.
  - The box on the paper and its value, and what the record or the claimed place's book holds.
  - An answer, or a garment.
  - Today's date.
  - The account standing.
- **Wrong denial**: "No directive and no record refuses this traveller. Destination: X, open today."
- **Unproven denial**: "A denial needs a logged deviation. Deviations logged: 0."
- The first mistake of a day is still free (`freeWarningsPerDay`). The slip grew from 560×320 to 720×420 (Build Office UI) to hold the two new lines at 24 pt.

## 6. Balance (50-run simulation, before → after)

Knobs are unchanged. Full summaries are in `SCRATCH/wave5/C/balance_before` and `balance_after`.

| Style, pace | Endings | Pay/run | Wrong calls/run | Stability at end (mean) | Lowest wallet (mean/min) |
|---|---|---|---|---|---|
| Perfect, whole queue | world ×50 → world ×50 | 2331 → 2320 | 0 → 0 | 99.72 → 99.62 | 50/50 → 50/50 |
| Imperfect, whole queue | world ×50 → world ×50 | 2177 → 2175 | 14.7 → 14.1 | 66.8 → 68.3 | 41/-57 → 46/29 |
| Careless, whole queue | fired 46, bankrupt 4 → fired 50 | 1165 → 1351 | 19.5 → 20.5 | 60.8 → 58.6 | -31/-110 → -5/-80 |
| Perfect, 10 a shift | world ×50 → world ×50 | 1595 → 1586 | 0 → 0 | 99.82 → 99.76 | 50/50 → 50/50 |
| Imperfect, 10 a shift | world ×50 → world ×50 | 1442 → 1445 | 14.5 → 13.6 | 67.3 → 69.2 | 41/-57 → 42/-23 |
| Careless, 10 a shift | bankrupt 44, fired 6 → bankrupt 32, fired 18 | 625 → 823 | 13.9 → 15.9 | 70.2 → 66.2 | -104/-138 → -94/-150 |

- **Careless play still ends every run.** Its early days are gentler (fewer checks means fewer faults), so the careless clerk now lasts to days 5-14 instead of 3-12.
- **Faulty travellers per run** (perfect play): 105 → 93. Days 1-3 are lighter (8 %, 20 %, 31 % faulty); days 12-15 are as before.
- **Strandings**: 4.24 → 4.38 a run for the perfect clerk. Fines are 0 for the perfect clerk and 12 cr a run for the imperfect one.
- **Every story beat stands**: day 7-15 beats stand in 50 of 50 perfect runs, with the same right calls.
- **The top-tier House target still holds**: at 10 a shift, no top tier without bribes, and about one with them.
- **World outcomes** stay a spread: no outcome dominates, and the "as found" watch lines are unchanged in kind.
- The seed-12345 determinism check passes, and the runs log no errors.

## 7. Lesson 7 audit: moral choices use the wheel and the stamp

I audited every choice with a moral or story consequence (report in `SCRATCH/wave5/C`):

| Choice | Verb |
|---|---|
| Rook's bribe ("Take the 200 cr." / "Put that away.", `dlg_rook`) | wheel: a dialog's choices, applied at the shift's end |
| The desk's waiver pad (`pad:waiver`) | wheel: in the papers menu |
| Pell (days 7, 10, 15), Ines, Gutenberg, Rook, Ada, Hollis, the auditor | stamp: accept or deny at the tray or the PC header, the same `Decide` |
| Strandings and their fates | no choice: they follow from the accepted verdicts and the waiver on file |
| Mail, Notes, Orders, Home (care, house upgrades, slot machine), endings | no desk moral choice (economy and pacing only) |

- **Findings**: there is no special UI to convert. No bribe panel, no confirm modal, no desk pad object, and no Mail or Home button settles a story outcome.
- **Content fix**: the auditor said "Fifty credits went missing" for a 200 cr bribe. The line now says "Two hundred credits".
- **Report fix**: the bribe's money is now shown on the shift report (§4) instead of changing the wallet silently.

## 8. Decisions made on Saleh's behalf, and open questions

1. The ramp order in §2. The displaced come before the labourers so the famous displaced keep day 6, and Ines (day 8) meets her own kind's first day.
2. A paper arrives with the procedure that asks for it, and counts as one new thing. Questions arrive with their check and are not counted separately.
3. There is no stranding fine before the waiver exists (§3).
4. "Socrates" moves to day 12.
5. The Citizen Account keeps showing forms on file (waiver number, proof) before those papers are issued. That keeps every random stream stable. **Open**: hide those record rows until the paper is issued?
6. The Return Order arrives last (day 15). It carries no fault of its own until the paper dates apply to it. **Open**: move it earlier, beside the paper dates on day 14?
7. The pacing check applies to every day, not only days 1-5.
