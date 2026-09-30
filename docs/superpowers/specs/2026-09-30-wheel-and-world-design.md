# Wheel and world: Papers, Please lessons 3, 9 and 10 (wave 5, track D)

Date: 2026-09-30. Branch `feat/wheel-and-world`. Brief: `SCRATCH/wave5/PP_BRIEF.md`, track D.
Saleh's rules applied: one penalty per wrong decision (nothing here fines anyone), personalities not types (every answer is the personality's), the same interactions for everyone (one wheel entry, one verb), knobs in ScriptableObjects or content, authored cases outside the draws.

## Lesson 3: a logged difference becomes a question on the wheel

| # | Decision | Why |
|---|---|---|
| W1 | The trigger is the existing deviation model: `DiscrepancyLog.Prove`/`Add` in `EvidencePresenter`. A new event `EvidencePresenter.Documented(Discrepancy)` fires after the report and the app hear of it; `InvestigationUIController` routes it to `InterviewPresenter.Confront`. The model's API is unchanged; `Discrepancy` only gains `statementDocument` and `otherDocument` (the papers a proof names, filled by `Prove`, read by nothing but the question). | Track A rebuilds the compare UI on the same events; one additive event and two additive fields keep both tracks mergeable. |
| W2 | The question lives in a hub sub-menu, "Ask about a difference >", one one-shot entry per logged difference ("VISA CLASS: Premium?"). The entry joins the hub with the first logged difference (`InterviewScript.AddConfront`) and shows only while a question is left (`DialogChoice.HideWhenSpent`, a runner rule: a sub-menu whose menu offers only Back is hidden). | The ask menu is full (Back, six questions, small talk = 8). A hub entry is what Papers, Please does (the interrogate button appears after a find). |
| W3 | `interview.menuCapacity` 8 -> 9 and the ring 300 x 200 -> 365 x 225 (`Desk_Default.wheelRadii`), because the hub's conservative worst case (papers, two spoken requests, ask, look, differences, two rumour dialogs, a premade's dialog) is nine. `RadialLayout.MaxFit` = 9 exactly with 240 x 44 buttons. | Keeping the buttons' size keeps their readability; the ring is 130 px wider and 50 px taller, clamped on screen as before. |
| W4 | The desk's question is content per proof and statement kind (`interview.confront.prompts`), optionally per category (the account and contract rows), in one or two desk lines so each fits a transcript row (`maxLineChars` 100 with the longest fills): "Your {document} says {value}." / "Your account says {other}." | Saleh's example ("Your visa says Premium, your account says Economy.") needs two long fills; two lines keep the transcript's layout. |
| W5 | The answer: honest travellers (`ReactionIntent.Honest`: in practice a costume error, the only honest traveller a difference can be logged on) Explain; a liar Cracks when a value of their dialog seed and the category is below their personality's `confess` (content, `personalities[].confess`: chatty 0.6, curt 0.3, anxious 0.8, sunny 0.7, grand 0.15, stickler 0.5, glum 0.4), else Doubles down; once cracked in a case, always; a lying premade always doubles down (days 7-15 B7: a beat never confesses). A value, never a draw, so the order of questions changes nothing and no stream moves. | "Liars crack or double down" by personality, deterministic and seeded without a new stream. |
| W6 | An explanation clears nothing: no rule excuses a logged difference, so the verdict (and the one penalty) is the same. The confession is a transcript line, never evidence (not an Answer). | "which does not clear the fault unless the rules say so": no rule says so today. |
| W7 | Voices: `interview.voices.confront` (per personality or premade, by outcome, optionally a fault reason and a lie kind, scoring 4 each like the reactions and slips) over `interview.confront.replies` (defaults). `VoiceChecks` requires, for every personality in the draw, a base Explain, Crack and DoubleDown; a base default for each; a DoubleDown for every lying premade (rook, auditor, socrates, ada). Authored: 7 x 3 base lines, a panic Explain per personality, a fault-specific Crack for each, 11 defaults. | "extend the voice checks so every personality covers the new slots". |
| W8 | Time: asking costs the time the exchange takes, as every question does (the shift clock runs; no question has a flat cost). | "Answering costs shift time like other questions." |

## Lesson 9: recurring faces

| # | Decision | Why |
|---|---|---|
| R1 | Only generated travellers return; premades and story characters recur where the day plans put them (Pell's forced alternatives, days 7-15 B12). A traveller comes back once at most. | Authored cases stay outside the draws (rule 4). |
| R2 | At a denial, one draw on the traveller's own new stream `Seeds.ForReturn` ("BACK", from their case seed): `returnChance`, then the story (`returnCorrectedChance`); the window `returnAfterDaysMin`..`returnAfterDaysMax` days. Knobs in `GameConfigSO` "Recurring faces" (0.4, 1, 3, 0.5). | Seeded and deterministic; Saleh edits them in the Inspector. |
| R3 | Who they are is saved in `WorldState.returns` (additive): name, display name, kind, role, claim, gender, birth date, personality, Citizen ID and their case seed. On return their look and account streams are reseeded from it, so the same face (skin, face, hair) and the same account number come back. | "recognisable (same name/face)". |
| R4 | Each day takes at most `days[].returns` of them (content: 1 from day 2), the earliest denied first, on the first day of their window whose plan draws their kind and whose world holds their place open to them; they take a slot no forced appearance or planned faulty traveller holds, drawn on the day's own stream `Seeds.ForReturnSlots` ("RSLT") after the violators, so a day with none draws exactly as before. | Days with no returning traveller keep their golden masters. |
| R5 | Corrected papers = the same claim, honest (no lie, no fault, no costume error rolled: the "right call" is to accept). A new story = the same claim, every roll made again (they may lie again, or differently). They keep their claim so their face (age band, wardrobe) is the same. | A returning traveller is recognisable and the player learns something from the second look. |
| R6 | Their history: the desk greets them ("Back again, madam? You stood at this window on day 3.", `interview.openerReturning`), and the paper's desk section tells their second visit the next morning (`news.returnedAccepted`, `news.returnedDenied`). | "A returning traveller carries their history (the paper can mention them)." |

## Lesson 10: the paper traces each change to a face

| # | Decision | Why |
|---|---|---|
| T1 | Carries name the traveller whose stamp carried the value and the day (`CarryRecord.traveller`, `FactEdit.traveller`/`travellerDay`, the latest of the pair's records), `history.lines.carryBy`. | "each change traces back to a face". |
| T2 | Panics name the traveller (`PanicRecord.traveller`), `history.lines.panicBy`. | Same. |
| T3 | World-outcome headlines end with the last traveller whose verdict pulled that outcome and the day (`WorldState.pullTraces`, `Traces.Record` at every decision, accept or deny, since a denial pulls "as found"; a split names the later of its two), `history.lines.traced`. An answer only a story rule pulled names no one. | "world-outcome headlines too". |
| T4 | The name is the display name (given name and role, "Omar (Merchant)"), as strandings print it. An older save's pending records have no name and keep the old lines. | Recognisable, and old saves stay readable. |

## Open questions for Saleh

1. The honest explanation never excuses a difference (W6). Should a rule ever let an honest slip through (e.g. a clerical slip on a non-evidence field)?
2. The ring is wider for a ninth choice (W3). Would you rather keep the 8-slot ring and move the two spoken requests into a sub-menu?
3. Returning travellers keep their claim (R5). Should a new story also change destination (their face would still match; their dress would not)?
4. Returns per day are 1 (content) and the chance 0.4 (Inspector). Tune after a play.
