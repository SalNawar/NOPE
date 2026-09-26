# Traveller types (the debt dystopia): design

*2026-09-26 · drafted by Claude from Saleh's answers of 2026-09-26, then finalized the same day with his answers to its nine Q-items, his translation change and his content-source note ("Saleh's answers" below; §13 records how each one was applied) · read-only map of `main` at `ff3a6e0` (`E:\unity\NOPE-feat-clock`) · the code wins over this text · the PC redesign spec (`2026-09-26-pc-redesign-design.md`) owns every screen; this spec owns the data, the rules and the generation; §16 is the seam between the two · built in the phases of `docs/superpowers/plans/2026-09-26-redesign-plan.md`*

Saleh, verbatim:
- "there are 3 types of travelers. the first are tourist who are travelling for fun, the second are laborer's who are travelling to pay debt and the third are people who got pulled from their time line and need to get back. each will have different type of documentation and processing rules but same mechanics. the idea is that these citizens are trapped with massive debt in this dystopian future while others are escaping for fun."
- Documents: "All 2150 agency forms".
- Order: "day 1 tourists day3 labourer day 5 displaced day 6 famous displaced".
- Papers: "2 for rich tourists. then 4 for poor tourists. 3 for labourer and 3 for displaced."
- Dress: "everyone must be dressed in the appropriate clothes of the era they are going to or else they will cause a panic."
- Transponders: "tourists and labourer must have a transponder labourerer and poor tourist have a cheap one that could fail and they can get stuck their they need to sign a weaver".
- Lies: "all kind of lies some times people trying to escape debt and run into the past, sometimes they have a fake contract with more money paid to them, sometimes they are not rich tourists, sometimes it is displaced people up to no good, etc". Ticked: debtors posing as tourists, forged labour papers, fake displaced people, smuggling.
- The art side's office concept: "A FRESH START — DEBT RELIEF DEPARTURES", yellow sunburst banners, "CLEAR YOUR DEBT", "ATTENDANCE REQUIRED", and a PC "Citizen Account" window (ID 773-2840-19, Status Eligible, Balance 125,430 cr; icons Records, Travel, Forms).

## Saleh's answers (2026-09-26)

| Q | His answer | Applied in |
|---|---|---|
| Q1 Day 1 | Rich and poor tourists from day 1, but poor tourists are always honest on day 1. | K3, K5, §2.3 |
| Q2 The transponder | On the papers now: model, serial and class on the manifest and on the account. A physical desk device later. | F3, §7.3, the plan's future phases |
| Q3 Proof | Papers prove, answers hint. Two papers that disagree prove a lie; answers only raise suspicion. | L4 |
| Q4 Wrong dress | Either another place's item or 2150 clothes (reusing the Future art), plus a new set of 2150 accessories for the art brief. | C2-C4, §7 |
| Q5 Cheap transponders | They really fail, and there is a fine. A seeded stranding chance for travellers let through on a cheap transponder, which can change history through the existing carries; the clerk is fined for letting one through without a signed waiver. | S1-S3 |
| Q6 The clerk's debt | Yes, and it drives the ending. The Citizen Account opens on the clerk's own account (773-2840-19), and the bankrupt ending becomes the clerk's own Debt Relief departure. | D1-D3 |
| Q7 The poor tourist's fourth paper | "any of the above": each poor tourist carries one of a Holiday Credit Agreement, a Proof of Funds or Travel Insurance (seeded per traveller), and each can be forged. | F2, §3.4-3.6, L10 |
| Q8 Dates | The date and an expiry. From day 4 the departure date must be today, and papers carry an expiry date that must not have passed. | F7, P3 (`PaperDates`) |
| Q9 Dress precision | The exact place is checked, and the Costume Guide is grouped by era. | C3, C5 |

Two instructions came with the answers:
- **Translation.** "all documents must be filled in english. the translator should be for speech of other languages so you can comunicate with other traverlers if you dont have it most of the dialogue will be not in english but key words will still be in english enough for the player to understand some stuff in the dialogue. this we will tune later." (F5, I3, I4, §8.1, §11.1)
- **Content source, later.** "we will create an excel file for all cases later with all possible dialogue options, all possible dialogue interactions, all real characters, all premade characters and stories etc" (CS1, §15)

## 0. The map: what exists and what this spec does with it

Today every traveller is effectively displaced and going home: the claim is a home place, the Travel Passport and Transit Permit print its facts, and a liar's disguise leaks their true home's values. Everything below was read in the code at `ff3a6e0`.

| System (where) | Today | This spec |
|---|---|---|
| Claim fields (`CaseInstance.claimedNation/claimedEra/nation/originLabel`) | the claimed home = the destination | **kept**: they mean "the place the traveller is sent to" for every kind (K2) |
| Case shape (`CaseBlueprintSO`: document templates, archetype pool, liar chance; picked by `WeightedRandom` on the case stream, `CaseFactory.cs:218-222`) | one blueprint, `CaseBlueprint_Investigation` | **one blueprint per kind** (K1) |
| Papers (`DocumentTemplateSO`, `DocTemplate_Passport` 4 rows, `DocTemplate_Permit` 2 rows; `PaperFace.Capacity` = 6 rows per desk paper; 4 `paperSpawnSlots`) | 2 papers | **ten agency forms**, 2 to 4 per traveller, at most 6 fields each, laid out by the PC spec's form engine (F1, F2) |
| Records (`CitizenRegistry`, `CitizenRecord`: name, born, origin, note; `CaseFactory.BuildRegistry`) | the registered identity (a liar's cover) | **rows**: the Citizen Account (2150 citizens, the truth), the Displacement Registry (the displaced, the cover as today) and the clerk's own account (R1, D1) |
| Lies (`Lies.Plan`, `Forgery.IsProvableTell`, `LiePlan`; channels Papers / Answer / Appearance; `Seeds.ForLies`) | place tells: the true home's facts and birth year | **kept** for place tells, with the present as a new source (fake displaced, smuggling); **record tells** added beside it (L1-L3) |
| Proofs (`DiscrepancyLog.Prove`: ClaimMismatch, ForeignOrigin, RecordMismatch; one per category) | statement vs book or record | **kept**; new categories; one new proof: two papers that disagree (L4) |
| Verdict (`VerdictRules.ShouldAccept(isLiar, claimAllowed)`, `IsUnprovenDenial`) | liars need a logged deviation; rule violators need none | **same two functions**, inputs generalised to "deviation fault" and "directive fault" (P2) |
| Rules (`TravelRuleSO`: era / nation / nation+era forbidden; `ViolatorSlots`; `DayPlanSO.guaranteeRuleViolators`) | closed destinations, one guaranteed violator each | **directives**: closures plus standing procedures introduced over days, same slot picker (P3, P4) |
| Looks (`Looks.Compose(claim, leakFrom, ...)`, `Looks.CanLeak`, Costume Guide = Culture fact) | dressed from the claimed home; a liar leaks one true-home item | **dressed from the destination** (the same claim, so the same code); costume errors for 2150 citizens: another place's item, 2150 clothes or a 2150 accessory (C1-C5) |
| Premades (`LegendarySO`, `Premades`, forced slots day 1 slot 3 and day 2 slot 6, pools days 2-6) | famous people going home | **the famous displaced**, from day 6 (K3) |
| History (`HistoryService`, `Carries`, the leader's Future place, `TodaysWorld`) | the Future is a destination from day 6 while a nation leads | the Future becomes **the present** (the home of 2150 citizens), never a destination; smugglers and stranded travellers carry 2150 into the past (H1-H3, S2) |
| Translation (`Translation.InTongue(category)`, `translation.fromDay` 2, Papers and Speech translators) | every place-fact value on papers and every traveller line in the claimed place's tongue | **every document in English**; the displaced speak their tongue from day 5 with the key words in English; translators for speech only (F5, I3, I4) |
| Interview (`questions[]`, `overrides` by era, hub capacity 8, `interview.claim`) | "Ask about home >", six questions from day 1-3 | wording and question lists **per kind**; a "Request papers >" sub-menu (I1, I2) |
| The clerk (`WorldState.money`; the bankrupt ending at `bankruptcyMoneyThreshold`) | a wallet and a generic bankrupt screen | **the clerk's own Citizen Account**, 125,430 cr in debt; each shift's pay chips at it; bankruptcy is the clerk's own Debt Relief departure (D1-D3) |
| Transponders | none | on the papers and the account; a cheap one can strand its traveller (F3, S1-S3) |

## 1. Decisions

| Id | Decision | Why |
|---|---|---|
| K1 | **Four kinds, one blueprint each**: `RichTourist`, `PoorTourist`, `Labourer`, `Displaced` (a new Domain enum `TravellerKind`; `CaseBlueprintSO` gains `kind`). The famous displaced are `Displaced` premades. A day plan weights the blueprints per day (`days[].kinds` in `world_source.json`, written by Generate World); a kind entry may be marked `honest` (K5). | A blueprint already is "the shape of a case" (its documents, roles and liar chance), and the blueprint pick is already one weighted draw on the case stream, so the kind costs no new draw and every seed keeps its draw count. |
| K2 | **The claim is the destination for every kind.** Tourists and labourers claim a past place of today's world and come from **the present** (2150, H1); the displaced claim their origin, which is also where they are sent. | Every consumer of the claim (books, closures, impacts, looks, answers) already keys on the destination; the displaced are exactly today's traveller. |
| K3 | **Introduction:** rich and poor tourists day 1 (the poor always honest on day 1), labourers day 3, the displaced day 5, the famous displaced day 6 (§2.3). The era ramp, the countries and the queue sizes (8/10/12/12/13/14) are unchanged; the Future era leaves the destinations. | Saleh's order and his Q1 answer. Day 1 shows both tourist paper sets; its only liars present rich papers (L1, L2), so the four-paper set is learnt on honest travellers. |
| K4 | **Who a 2150 citizen is:** a name from the eight Future places' name lists together (64 men's and 64 women's names, no name in both lists, checked), born 2080-2132 (the Future places' birth years). The displaced keep today's identity rules (the claimed origin's names and birth years). | Tourists and labourers are people of 2150; the lists exist, and gender still follows the list the name came from (`TravellerGenders.FromNameLists`). |
| K5 | **One fault source per traveller**, decided in this order, the first that applies winning and the later rolls skipped (no draw): the premade's authoring; a planned slot (P4); the lie roll; the violation roll; the costume roll. A traveller drawn from an `honest` kind entry skips all three rolls (no draw). | One reason per denial keeps every row of the verdict table testable, as today's "violators never lie"; a skip that draws nothing keeps every stream's count stable. |
| F1 | **Every paper is a Temporal Customs agency form** (`TC-nnn`, one page, at most 6 fields). The kind's primary form carries the photo and is handed over on arrival; the others on request. Each form is laid out by the PC spec's form engine and must pass `FormLayout.Check`: it fits the desk face at the 720p floors (the PC spec's FO10, which replaces the `PaperFace.Capacity` row limit). `DocTemplate_Passport` and `DocTemplate_Permit` are retired. | "All 2150 agency forms". Six fields keep every form inside the budget the PC spec measured (0.92 of the page, two short boxes to a row, §6.3 there). |
| F2 | **Ten forms** (§3): TC-101 Leisure Departure Visa, TC-230 Departure Manifest, TC-310 Stranding Waiver, TC-415 Holiday Credit Agreement, TC-416 Proof of Funds, TC-417 Travel Insurance Certificate, TC-520 Debt Relief Labour Contract, TC-610 Displacement Certificate, TC-620 Intake Declaration, TC-630 Return Order. Rich tourist 2 (101, 230); poor tourist 4 (101, 230, 310 and one **proof of means**: 415, 416 or 417, drawn per traveller on the account stream by content weights); labourer 3 (520, 230, 310); displaced 3 (610, 620, 630). | Saleh's counts exactly, and his Q7 answer ("any of the above"): the proof of means is one of three, each forgeable (L10). |
| F3 | **The transponder is on the papers**: model, serial and class on the Departure Manifest and on the Citizen Account. **The waiver is a paper form with a signature row.** The displaced carry no transponder: the agency returns them one way. A physical transponder the traveller sets on the desk is a **future phase** and an art-brief item (§7.3), not scheduled in the plan. | Saleh's Q2: papers now, the device later. Paper keeps every check inside today's mechanics (read, compare, log). |
| F4 | **New `ClueCategory` values, appended** (the enum is serialized; a test pins its int values, audit R1-003): `CitizenId`, `Destination`, `AccountStatus`, `TransponderId`, `TransponderClass`, `Debt`, `Credit`, `Funds`, `PolicyNo`, `Employer`, `Term`, `Wage`, `WaiverNo`, `Incident`, `DepartureDate`, `Expiry`, `Signature`. **Invariant: one value per compared category per traveller**, so every honest statement of a category agrees wherever it is printed or said. `DepartureDate`, `Expiry` and `Signature` are **directive-only**: read against the calendar and the Directives, never compared, never a proof, and outside the invariant. | A category decides what a row may be compared with; the invariant is what makes two disagreeing papers a proof (L4). Dates differ from form to form by nature, so they are rules, not evidence. |
| F5 | **All forms are filled in English.** Saleh: "all documents must be filled in english." No form value is ever drawn in a tongue, the Intake Declaration included: the agency takes the declarant's statement down in English. | Saleh's translation change. It retires piece 9's papers translation (I4, §11.1). |
| F6 | **Amounts are in "cr" and dates are in the 2150 calendar.** Day 1 is a content knob (`agency.firstDate`, 14 Mar 2150), today is `firstDate + day − 1`, formatted as `BirthDates.Format` writes dates. **The art's desk calendar shows today's date** with the day number ("14 MAR 2150 · DAY 1"). | The art's account says "125,430 cr"; one date format already exists; the date checks (F7) need today's date on the desk. |
| F7 | **Dates on the papers** (Saleh's Q8). The Departure Manifest (TC-230) and the Return Order (TC-630) print the departure date. The Visa (TC-101), the three proofs of means (TC-415/416/417) and the Displacement Certificate (TC-610) print "Valid Until". Honest papers depart today, and a Valid Until is 3 to 365 days ahead (account stream, §4.3). From day 4 a directive (`PaperDates`) requires both: a departure dated today and nothing expired. | "the date AND an expiry". The manifest is the departure's own paper and every 2150 citizen carries one, so one row covers rich, poor and labourer; the date moves off the visa so every form keeps six fields. |
| R1 | **One records source, three record types.** The Citizen Account (2150 citizens) is the truth about them. The Displacement Registry entry (the displaced) is the registered identity, a liar's cover, exactly as today (piece-2 D3). The clerk's own account has the account's shape (D1). A record is rows (category, label, value) in three groups matching the art's icons: Records, Forms on file, Travel (§4). | The account is where debt, contract, transponder, waiver and proof of means can be checked; rows let the records view and the compare path stay generic. |
| R2 | **Accounts are generated on a new salted stream, `Seeds.ForAccount`** ("ACCT"), from ranges in `world_source.json` `agency` (§4.3), the poor tourist's proof of means and every honest Valid Until included. | A new concern gets its own stream (house rules), so tuning an amount never shifts who travels. |
| R3 | **Records are found by name or by agency number** (`CitizenRegistry.Find` tries an exact number first). The PC spec's search later serves the lookup through its index with the same rule (its SE6). | The forms print the number on every page. |
| R4 | **Ancestry:** a 2150 citizen's account names a lineage (a past place; flavour for the Ancestry site); a famous displaced person's real biography (their record note) is the History and Ancestry sites' entry. Generated displaced people get no ancestry entry. | For a generated displaced liar an entry would either be missing (revealing the lie without proof) or repeat the cover registry (a second source of the same truth). |
| P1 | **Two fault kinds.** A **directive fault** is what the Directives forbid in the presented papers and the claim, or a Frozen standing on the account: a closed destination, a missing or unsigned form, the wrong paper set, a frozen account, a departure not dated today, an expired paper. A **deviation fault** is anything that needs a statement compared with a truth: every lie, smuggling, wrong dress. The rest of the account (class, debt, contract, forms on file) is compared, never read against a rule. Accept only a traveller with no fault. Denying a deviation-only traveller needs a logged deviation (the evidence gate, unchanged). | It is today's split (a liar needs evidence, a violator does not) stated so that every new rule falls on one side. A debtor posing as a tourist shows a forged class, so reading "Eligible" on the account leads to the compare that proves it; it never also counts as a directive fault. |
| P2 | **`VerdictRules` keeps both functions; their inputs become `hasDeviationFault` and `hasDirectiveFault`** (`ShouldAccept = !deviation && !directive`; `IsUnprovenDenial = requireEvidence && evidence == 0 && !accepted && deviation && !directive`). `CaseInstance.claimAllowedByRules` becomes the directive fault it stands for. | The same decision table with the two inputs named for what they now mean; no second verdict path. |
| P3 | **Directives are `TravelRuleSO` with new types** (`PaperSet`, `DebtStanding`, `DressForDestination`, `NoPresentGoods`, `PaperDates`, `ReturnHome`) beside the three closure types. Each type has a pure Domain predicate, `Directives.Breaks(type, CaseFacts)`, over `CaseFacts` (the kind, the destination, the classes printed, the forms carried and whether the waiver is signed, the papers' dates against today, the account's standing), and a directive line. Each kind also has a procedure line (content), listed from the kind's first day. Standing procedures stay active from their first day on; closures change daily as today (§5.3). | One rule asset type, one Directives list, one briefing path; the predicates are decision tables in Domain. |
| P4 | **Guaranteed faulty travellers:** each closure active today and each procedure on its first day gets one faulty traveller in the first half of the queue (`ViolatorSlots.Pick`, unchanged, on the violator stream). After its first day, a directive is broken at the day's `violationChance`, the dress rule at `costumeErrorChance` (knobs in `days[]`), and the lie rules (smuggling, the displaced's return home) through the lie roll. | Today's guarantee, extended to the rules that do not arise by chance; the slot picker and its forced-premade exclusion are reused. |
| P5 | **"Cause a panic" is the dress rule's denial reason**: its deviation line, the citation on a wrong accept ("Approved a traveller who would cause a panic in {place}") and a next-morning news line. | Saleh's words become the player-facing reason. |
| L1 | **Place tells stay `Lies.Plan`.** New callers: the **fake displaced** (candidates = the present only) and **smuggling** (candidates = the present; categories Currency and Technology only; the traveller's own claim is honest). The roll moves out of `Plan` into a `Lies.Roll` step that also picks the lie kind. | The premade liar already calls `Plan` with a one-place candidate list; no second place-lie path. |
| L2 | **Record tells: new Domain `RecordLies`.** Each record lie kind (L1-L5, L10) names the forged fields (form, category), and each record category has a false-value maker (the birth date reuses `BirthDates.PickOtherYear`). Every forged field is a tell and differs from the account. The day's `tellCount` keeps capping place tells only. | The birth-date tell is today's only record tell; this is its generalisation, not a copy. A forgery has to be consistent to fool anyone, so all of its forged fields are provable. |
| L3 | **`Forgery.IsProvableCategory`: every compared record category is provable when the records are reachable** (the birth-date rule, extended). A directive-only category is never a tell. A rich desk without the records view generates no record lie and warns once (piece-2 R15's pattern). | Only provable tells are generated. |
| L4 | **Papers prove, answers hint** (Saleh's Q3). `DiscrepancyLog` gains `DiscrepancyProof.CrossMismatch` (appended): two document fields of one compared category that disagree prove a forgery (one of them is a tell; the report says the papers contradict each other and names neither as the forgery; report key `deviation.crossMismatch.papers`). An answer against a paper stays a hint: MISMATCH in the bar and nothing logged (piece 3's R2, unchanged). A spoken tell against a book or a record still proves, as today. RecordMismatch covers every record category unchanged. The contradicting pairs come from one Domain rule, `PaperChecks.Contradictions` (the same compared category, two documents, values that differ by `DiscrepancyLog.ValuesMatch`), which the PC spec's Analysis Scanner also reads (its SC3). | The agency's forms are the agency's word, so two that disagree prove one is forged; speech is the traveller's word. One rule serves the proof and the scanner, so the scanner can never mark a pair the player cannot log. |
| L5 | **Streams:** the case stream keeps its draw count; the lie stream gains the kind pick after the roll; three new salted streams: `Seeds.ForAccount` ("ACCT"), `Seeds.ForFaults` ("FALT") and `Seeds.ForStrandings` ("STRD") (§6.4). | House rules: a new concern, a new stream. |
| C1 | **Looks are drawn from the destination's wardrobe** (the claim, so `Looks.Compose` is unchanged). A 2150 citizen's skin and hair weights come from the country whose Future list gave their name; their age is 2150 minus their birth year. | "Dressed for the era they are going to"; for the displaced the destination is the origin, so today's rule survives there. |
| C2 | **Costume errors** (2150 citizens only; Saleh's Q4), one of three variants drawn on the fault stream by content weights (`costumeErrors` in `world_source.json`): **another place's item** (one signature item of another of today's places, through `Looks.CanLeak`, unchanged); **2150 clothes** (the present's whole look); **a 2150 accessory** (one item of the new 2150 accessory kit, worn with an otherwise right costume). | The leak rules already guarantee an item that is readable, visible and provable against the Costume Guide; 2150 clothes make the plainest lesson; an accessory is the subtle slip. |
| C3 | **The Costume Guide always has the present's row** (its clothes and its accessory kit), so every costume error has an origin proof that names 2150. On the page it is **grouped by era** (Saleh's Q9): the PC spec's Reference register lists the claimed row first, then the places under era headings. | The origin proof of a 2150 garment names 2150; era groups make a long guide faster to scan. |
| C4 | **Art:** the Future batch becomes the present's wardrobe (2150 clothes), and a **new 2150 accessory kit** joins the character brief (§7.3). | Saleh's Q4. |
| C5 | **Judged per place** (Saleh's Q9): the destination's own look. A foreign item of the same era also causes a panic. | "the exact place is checked"; the Costume Guide, the Culture facts and the proofs are per place already. |
| I1 | **Questions per kind:** tourists and labourers are asked about the trip (currency and device, from day 4, when spoken tells start); the displaced about home (all six, from day 5). `questions[]` gains `kinds`; wording overrides gain `kind`. | Only categories that can carry a tell for that kind are worth a wheel slot; a question that can never catch anyone is noise. |
| I2 | **"Request papers >"** (a sub-menu) when a traveller can be asked for 2 or more forms. For 2150 citizens: the Manifest, the Waiver and **one "Proof of means" entry** (the three proofs share `DocumentTemplateSO.askGroup` "proof"; the traveller hands over the one they hold). For the displaced: the Intake Declaration and the Return Order. The forms the desk may ask each kind for are data (`askableBy`). A form the traveller does not carry is answered with a spoken line ("I don't have one"), one-shot. | The hub is exactly full today (8 of 8, piece 8 W5). One entry for the three proofs keeps the sub-menu at three, and asking for a paper a traveller should have is how a missing one shows. |
| I3 | **Who speaks what:** 2150 citizens speak English; the displaced speak their claimed origin's tongue (the tongue never shows or hides a lie, unchanged). `translation.fromDay` becomes 5, and its notice moves to day 4's morning paper ("from tomorrow"), so a Speech translator bought that night is in force when the displaced arrive. | The agency's own citizens need no translator; foreign speech arrives with the displaced, and the player hears of it a night ahead. |
| I4 | **Translators are for speech only.** Saleh: "the translator should be for speech of other languages so you can comunicate with other traverlers if you dont have it most of the dialogue will be not in english but key words will still be in english". The four Speech translators stay; the four Papers translators and every paper flip retire (§11.1). Untranslated speech keeps its **key words** in English by a data rule, `translation.keyWords` (§8.1): the template slots that stay English, a word list, and digits. Answers still compare on their canonical values, so every liar stays provable without a translator. | Saleh's instruction. A data rule is what "this we will tune later" needs: tuning edits two lists, never code. |
| H1 | **The present** is the leader's Future place, or with no leader a **neutral present** authored in `world_source.json` (`present`: facts, birth years, wardrobe and accessory kit; nation token `neutral`). It is never a destination; its row is in every book from day 1; `TodaysWorld.Present` exposes it. | 2150 citizens, smuggled goods and fake displaced people need 2150 values from day 1, and a leader may never exist. The wallet already reads "Credits" with no leader and the leader's Future currency otherwise: the same rule. |
| H2 | **The Future era is no longer a destination** (day 6 loses its `future` weight); its display name becomes "2150". | Nobody travels to the present; "(Future)" beside the office's own time reads wrong once travellers depart from it. |
| H3 | **Impacts on accept land on the destination (unchanged code).** An accepted traveller with a place-tell source carries that source's Technology into the destination: a displaced liar (their true home, as today), a fake displaced person or a smuggler (the present); a stranded traveller carries the present's too (S2). `CaseInstance.trueHome` (a profile) becomes `tellSource` (a Domain `PlaceRef`: ids and label), because the neutral present has no profile asset. | Saleh's "inventions change era and country" gets its most direct carrier: 2150 goods landing in the past. |
| S1 | **Cheap transponders fail** (Saleh's Q5). At the shift's end, each accepted traveller whose real transponder (the account's, whatever the manifest claims) is Economy is rolled on `Seeds.ForStrandings` against `agency.strandChance` (first cut 0.08), in queue order, one draw each. A stranded traveller does not come back. | "they can get stuck their". A roll on its own stream never shifts who travels; the real class decides, so a debtor who forged a Premium manifest still rides a cheap unit. |
| S2 | **A stranding can change history.** The stranded traveller's 2150 Technology is carried into the destination through the existing carry path (`HistoryService.RecordCarry`, source = the present, H3), and the next morning's paper reports the stranding (`news.stranded`). | Saleh: it can change history "through the existing carries"; there is no second history mechanism. |
| S3 | **The fine.** A stranded traveller whom the clerk let through without a valid signed waiver (none handed over, "UNSIGNED", or a waiver number the account never registered) costs `agency.strandFine` cr (first cut 150): a shift-ledger line and a Fines entry on the clerk's statement. | Saleh: fined "for letting one through without a signed waiver". A forged waiver is no waiver, because the account never registered it. |
| D1 | **The clerk's own Citizen Account** (Saleh's Q6), authored in `world_source.json` `agency.clerk`: Citizen ID 773-2840-19, status Eligible, starting debt 125,430 cr; the name, birth date and lineage are Saleh's to author. Its rows use the account's groups (R1, §4.4). The PC's Citizen Account app opens on it (the PC spec's AC1). | The art's window is the clerk's. It is authored content, not a draw, because the clerk is one fixed character. |
| D2 | **Each shift's pay chips at the debt.** `agency.clerk.garnishShare` (first cut 0.25) of the shift's pay goes to the debt at the shift's end: a ledger line ("Debt Relief instalment: 55 cr · still owed 125,375 cr"), recorded as `WorldState.clerkDebtPaid` (additive). A paid-off debt stops the instalments, and the status then reads Standard. | "trapped with massive debt": the number barely moves, which is the point. Storing what was paid (not what is owed) makes an old save load as "nothing paid yet". |
| D3 | **The bankrupt ending becomes the clerk's own Debt Relief departure**: the same condition (money at or below `GameConfigSO.bankruptcyMoneyThreshold`), new copy ("A FRESH START. Your account is frozen and your Debt Relief departure is booked."), and the ending screen shows the clerk's own Labour Contract (TC-520, drawn by the form engine) beside the account, now Frozen. | Saleh's Q6: the debt drives the ending. The condition stays, so no ending rule changes; the instalments bring it nearer. |
| T1 | **The debt theme is a light touch in the news and the ledger** (§10), with the clerk's debt (D1-D3) and strandings (S1-S3). | The PC spec owns the news site; this spec gives it lines and numbers. |
| CS1 | **Content is row-shaped, for a spreadsheet later.** Saleh: "we will create an excel file for all cases later with all possible dialogue options, all possible dialogue interactions, all real characters, all premade characters and stories etc". Every content table this spec adds or touches is a list of flat records (§15): one record per row, an id, scalar columns, references by id. The import path (CSV or XLSX, then `world_source.json` or its successor, then Generate World) is a later phase of the plan; nothing is built for it now. | Designing the rows now costs nothing, and it makes the later import a converter, not a redesign. |
| M1 | **Save version stays 2.** Every new field is additive (`clerkDebtPaid`); strandings live in the history block's carries and the counters. | Nothing persisted changes shape. |

## 2. The kinds

### 2.1 Taxonomy

| Kind | Who | Purpose | Claim (destination) | Comes from | Account status | Transponder | Papers (on arrival first) | From day |
|---|---|---|---|---|---|---|---|---|
| Rich tourist | a 2150 citizen with money | fun | a past place of today's world | the present | Premium | Premium class | TC-101 Visa, TC-230 Manifest (2) | 1 |
| Poor tourist | a 2150 citizen travelling on credit, savings or insurance | fun, paid for at a stretch | a past place | the present | Standard | Economy class (can fail) | TC-101 Visa, TC-230 Manifest, TC-310 Waiver, one proof of means: TC-415 Credit, TC-416 Funds or TC-417 Insurance (4) | 1 (always honest on day 1) |
| Labourer | a 2150 citizen in debt | to work the debt off | the worksite, a past place | the present | Eligible (for Debt Relief) | Economy class (can fail) | TC-520 Contract, TC-230 Manifest, TC-310 Waiver (3) | 3 |
| Displaced | a person pulled out of their own time | to get home | their origin (a past place) | the claimed origin (a liar: elsewhere) | Registry entry, "Awaiting return" | none (returned one way) | TC-610 Certificate, TC-620 Intake, TC-630 Return Order (3) | 5 |
| Famous displaced | a premade (the ten existing) | to get home | their authored place | as authored (Socrates lies) | Registry entry with their note | none | as displaced | 6 |

### 2.2 What each kind is judged against

- **Tourists and labourers** are judged against their **Citizen Account** (identity, class, debt, the forms on file), the **Directives** (including the papers' dates against the desk calendar), the **destination's book rows** (the currency and device they carry), **each other's papers** (two that disagree prove a forgery, L4) and the **Costume Guide** (their costume).
- **The displaced** are judged as today: their **declaration, answers and dress** against their origin's book rows, their birth date against the **registry**, plus the Directives.

### 2.3 The day plan, days 1-6

Eras, countries and queues are today's. `days[]` gains `kinds` (blueprint weights, each entry optionally `honest`), `lies` (the lie kinds enabled), `violationChance`, `costumeErrorChance` and a `guarantee` flag per rule entry.

| Day | Queue | Destinations (eras × countries) | Kinds (weights) | Closures (today's rules) | New procedures (a guaranteed faulty traveller that day) | New lies | Place-tell channels | Premades |
|---|---|---|---|---|---|---|---|---|
| 1 | 8 | Ancient × Egypt, Iraq, Greece, Italy | rich 1, poor 1 (`honest`) | none | none (the tourists' procedure line: the two paper sets, and every paper must match the Citizen Account) | L1 poor posing as rich, L2 doctored visa | Papers (no place lie exists yet) | none (Senenmut moves) |
| 2 | 10 | + Medieval; + China, Britain | rich 3, poor 2 | New Kingdom Egypt | Tourist paper sets; dress for the destination | L3 fake waiver, L10 forged proof of means; costume errors | Papers | none (Socrates moves) |
| 3 | 12 | + Early modern; all 8 | rich 2, poor 2, labourer 2 | Medieval China, Early modern Japan | Labour paper set; debt standing | L4 debtor posing as tourist, L5 forged contract | Papers | none |
| 4 | 12 | + Industrial | as day 3 | Victorian Britain | No 2150 goods; paper dates (a departure dated today, nothing expired) | L6 smuggling | Papers, Answer | none |
| 5 | 13 | + Modern | rich 2, poor 2, labourer 2, displaced 2 | Weimar Berlin, New Kingdom Egypt | The displaced return home | L7 false origin, L8 fake displaced; L6 for the displaced | Papers, Answer, Appearance | none |
| 6 | 14 | as day 5 (no Future) | as day 5 | Showa Tokyo | none | L9 famous impostor | all three | forced Senenmut slot 8, "Socrates" slot 11; the other eight pooled at 5% |
| 7+ | as day 6 (`DayPlans.Pick`, unchanged) |

- **Day 1 (Q1).** Its liars present rich papers only: L1 is a Standard citizen carrying a Premium visa, drawn from the rich entry. Every traveller who presents the poor paper set on day 1 is honest, and no directive can be broken yet.
- The forced premades move to the second half of day 6 (the validator warns about a forced premade in the first half of a day with rules).
- Guaranteed faulty travellers in the first half (P4): day 2, 3 of 5 slots (a closure, the tourist paper set, a costume error); day 3, 4 of 6; day 4, 3 of 6 (a closure, a smuggler, a paper-dates fault); day 5, 3 of 7. Content can drop a `guarantee` if a day feels loaded.
- The liar chance stays the blueprint's (`contradictionChance`, now per kind; 0.5 today) plus the slot and effect modifiers. First cuts: `violationChance` 0.1 and `costumeErrorChance` 0.1 (both 0 before their rule's first day), `agency.strandChance` 0.08, `agency.clerk.garnishShare` 0.25. These are knobs for playtest (the plan's balance phase).

## 3. The forms

Every form is a `DocumentTemplateSO` (an authored asset, listed on its kind's blueprint), with its layout in `form` (the PC spec's FO3). New template fields: `formNumber` (printed in the title: "TC-101 LEISURE DEPARTURE VISA"), `askableBy` (the kinds the desk may ask for it, I2) and `askGroup` (one request for several forms, I2). "Tells" names the lies of §6 that can rewrite the row; "directive" marks a directive-only row (F4). Every value is English (F5).

### 3.1 TC-101 Leisure Departure Visa (tourists; on arrival; photo)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Full Name | Name | the account's name | the records search | never |
| 2 | Citizen ID | CitizenId | the account's ID | account; every other form | L2 |
| 3 | Date of Birth | BirthDate | the account's date | account | L2 |
| 4 | Destination | Destination | the claim ("Periclean Athens (Ancient)") | closures; the proof of means; the account's booked departure | never |
| 5 | Visa Class | AccountStatus | Premium or Standard, = the account's status | account; the proof of means; the paper-set directive | L1, L4 |
| 6 | Valid Until | Expiry | 3-365 days after today | the calendar (F7) | directive |

The photo is the traveller in costume (a crop of the same look, as today): the departure photo is taken at the costume fitting.

### 3.2 TC-230 Departure Manifest (every 2150 citizen; on request)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Citizen ID | CitizenId | the account's ID | account; the primary form | L1 (borrowed) |
| 2 | Transponder | TransponderId | model · serial ("Hopper Mk II · HP-40718") | account (forms on file); TC-310 | L1 (borrowed) |
| 3 | Transponder Class | TransponderClass | Premium (rich) or Economy | account; the paper-set directive | L1, L4 |
| 4 | Currency Carried | Currency | the destination's currency (exchanged at the desk's bureau) | Currency Ledger: the destination's row; the present's row names 2150 | L6 |
| 5 | Declared Effects | Technology | the destination's device (the approved period kit) | Index of Devices | L6 |
| 6 | Departure | DepartureDate | today | the calendar (F7) | directive |

### 3.3 TC-310 Stranding Waiver (poor tourists and labourers; on request)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Signatory | Name | the account's name | | never |
| 2 | Citizen ID | CitizenId | the account's ID | account; other forms | never |
| 3 | Transponder | TransponderId | the manifest's transponder | TC-230; account | L3 |
| 4 | Debt Passed to Kin | Debt | the account's debt (it passes to the next of kin if the traveller is stranded) | account | L4 |
| 5 | Waiver No. | WaiverNo | the waiver registered on the account ("SW-204817") | account | L3 |
| 6 | Signature | Signature | the signatory's hand, or "UNSIGNED" | the paper-set directive | directive |

### 3.4 TC-415 Holiday Credit Agreement (a poor tourist's proof of means; on request)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Borrower | Name | the account's name | | never |
| 2 | Citizen ID | CitizenId | the account's ID | account | never |
| 3 | Destination | Destination | the claim | TC-101 | never |
| 4 | Account Class | AccountStatus | Standard | account; TC-101 | L4 |
| 5 | Credit Line | Credit | the credit line on file ("9,400 cr") | account | L4, L10 |
| 6 | Valid Until | Expiry | 3-365 days after today | the calendar (F7) | directive |

### 3.5 TC-416 Proof of Funds (a poor tourist's proof of means; on request)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Account Holder | Name | the account's name | | never |
| 2 | Citizen ID | CitizenId | the account's ID | account | never |
| 3 | Account Class | AccountStatus | Standard | account; TC-101 | L4 |
| 4 | Funds Held | Funds | the savings on file ("6,200 cr") | account | L4, L10 |
| 5 | Valid Until | Expiry | 3-365 days after today | the calendar (F7) | directive |

### 3.6 TC-417 Travel Insurance Certificate (a poor tourist's proof of means; on request)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Insured | Name | the account's name | | never |
| 2 | Citizen ID | CitizenId | the account's ID | account | never |
| 3 | Destination | Destination | the claim | TC-101 | never |
| 4 | Policy No. | PolicyNo | the policy on file ("TI-551902") | account | L4, L10 |
| 5 | Valid Until | Expiry | 3-365 days after today | the calendar (F7) | directive |

### 3.7 TC-520 Debt Relief Labour Contract (labourers; on arrival; photo)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Worker | Name | the account's name | | never |
| 2 | Citizen ID | CitizenId | the account's ID | account; other forms | never |
| 3 | Employer | Employer | the registered contract's employer ("Tyburn Mills Consortium") | account | L5 |
| 4 | Worksite | Destination | the claim = the registered worksite | account; closures | L5 (swapped) |
| 5 | Term | Term | "180 days" | account | L5 |
| 6 | Day Wage | Wage | "420 cr" | account | L5 |

The labourer's debt is printed on their waiver (row 4 of TC-310), and their departure date on the manifest: the contract has no seventh row, and a contract that starts today does not expire.

### 3.8 TC-610 Displacement Certificate (displaced; on arrival; photo)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Full Name | Name | the registered name (a liar's cover) | the records search | never |
| 2 | Displacement No. | CitizenId | "DP-4471-02" | registry; TC-620; TC-630 | never |
| 3 | Date of Birth | BirthDate | the registered date | registry | L7, L8 (birth-year tell, as today) |
| 4 | Origin | Destination | the claim | registry; TC-630; closures | never |
| 5 | Incident | Incident | the rift that took them ("R-0311-07") | registry; TC-630 | never |
| 6 | Valid Until | Expiry | 3-365 days after today | the calendar (F7) | directive |

### 3.9 TC-620 Intake Declaration (displaced; on request; English)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Declarant | Name | the registered name | | never |
| 2 | Displacement No. | CitizenId | the certificate's number | TC-610 | never |
| 3 | Coin of Home | Currency | the origin's currency | Currency Ledger | L6, L7, L8 |
| 4 | Native Tongue | Language | the origin's language | Tongues & Scripts | L7, L8 |
| 5 | Effects Carried | Technology | the origin's device | Index of Devices | L6, L7, L8 |

These are today's tell rows (the passport's Coin of Issue and Native Tongue, the permit's Declared Device); the permit's second Currency row ("Bond Currency") goes. The declarant's statement is taken down in English (F5): the Native Tongue row names a language, it is not written in it.

### 3.10 TC-630 Return Order (displaced; on request)

| # | Label | Category | Honest value | Checked against | Tells |
|---|---|---|---|---|---|
| 1 | Returnee | Name | the registered name | | never |
| 2 | Displacement No. | CitizenId | the certificate's number | TC-610 | never |
| 3 | Return To | Destination | the claim | TC-610; registry | never |
| 4 | Incident | Incident | the certificate's incident | TC-610; registry | never |
| 5 | Departure | DepartureDate | today | the calendar (F7) | directive |

### 3.11 Cross-checks at a glance

| Category | Printed on | Truth |
|---|---|---|
| CitizenId | every form of a traveller | the account or registry; each other (L4) |
| Destination | TC-101, TC-415, TC-417, TC-520, TC-610, TC-630 | the Directives (closures); the account's booked departure; the registry's origin; each other (L4) |
| AccountStatus | TC-101, TC-415, TC-416 | the account's status; each other (L4) |
| TransponderId / TransponderClass | TC-230 (both), TC-310 (id) | the account's forms on file; each other (L4) |
| Debt, Credit, Funds, PolicyNo, WaiverNo | TC-310, TC-415, TC-416, TC-417 | the account |
| Employer, Term, Wage | TC-520 | the account's registered contract |
| BirthDate | TC-101, TC-610 | the account or registry |
| Currency, Technology | TC-230, TC-620 | the books (the destination's row; any other row, the present's included, names where the value belongs) |
| Language | TC-620 | Tongues & Scripts |
| Culture (worn) | the look (wheel, "Look >") | the Costume Guide |
| Incident | TC-610, TC-630 | the registry; each other (L4) |
| DepartureDate, Expiry, Signature (directive-only) | TC-230, TC-630 / TC-101, TC-415-417, TC-610 / TC-310 | the calendar and the Directives only (directive faults) |

### 3.12 Where the papers go on the desk

At most four papers per traveller: the desk's four spawn slots and the landing rule already hold them (`DeskConfigSO.paperSpawnSlots`, `PaperLanding`). Build Office UI keeps guarding the limits: spawn slots against `ContentLibraryValidator.MaxDocuments`, and each template's form against the desk face through `FormLayout.Check` (the PC spec's FO10; until the form engine lands, `PaperFace.Capacity`).

## 4. Records

### 4.1 The Citizen Account (2150 citizens)

Rows are (category, label, value); a row with a compared category is compare-clickable as `RecordField` evidence.

| Group (art icon) | Row | Category | Rich | Poor | Labourer |
|---|---|---|---|---|---|
| Records | Name | Name | yes | yes | yes |
| Records | Citizen ID ("418-0937-52") | CitizenId | yes | yes | yes |
| Records | Born | BirthDate | 2080-2132 | same | same |
| Records | Status | AccountStatus | Premium | Standard | Eligible |
| Records | Standing | none | Good (a violator: Frozen, with a reason) | same | same |
| Records | Debt | Debt | 0 cr | 0-18,000 cr | 40,000-320,000 cr |
| Records | Lineage | none | a past place (flavour) | same | same |
| Forms on file | Transponder | TransponderId | a Premium model · serial | an Economy model · serial | an Economy model · serial |
| Forms on file | Transponder Class | TransponderClass | Premium | Economy | Economy |
| Forms on file | Waiver | WaiverNo | none on file | SW-nnnnnn | SW-nnnnnn |
| Forms on file | Proof of means (one row: the proof the traveller holds) | Credit, Funds or PolicyNo | none on file | a credit line (4,000-12,000 cr), savings (3,000-15,000 cr) or a policy (TI-nnnnnn) | none on file |
| Forms on file | Contract | Employer, Term, Wage (three rows) | none | none | employer, 90-720 days, 180-520 cr a day |
| Travel | Booked departure | Destination (the date beside it has no category: the Directives read the papers' dates) | the claim, today | same | the worksite, today |
| Travel | History | none | 0-3 past trips ("12 Aug 2149, Periclean Athens, returned") | same, some "returned late" | past contracts ("2146-47, Victorian mills, 540 days"), the debt still owed |
| | Note | none | "No remarks on file." | same | same |

The art's "Status Eligible, Balance 125,430 cr" is the clerk's own account (D1, §4.4). Traveller samples use other numbers.

### 4.2 The Displacement Registry entry (the displaced)

| Row | Category | Value |
|---|---|---|
| Name | Name | the registered name (a liar's cover) |
| Displacement No. | CitizenId | "DP-nnnn-nn" |
| Born | BirthDate | the registered date |
| Origin | Destination | the claimed origin's label |
| Incident | Incident | "R-mmdd-nn" |
| Found | none | a date in the last 30 days |
| Status | none | Awaiting return |
| Note | none | "No remarks on file.", or a premade's record note |

### 4.3 Generation (`Seeds.ForAccount`, fixed order)

ID; status-specific amounts (debt); for a poor tourist, the proof of means (one weighted draw over `agency.proofWeights`) and its value (credit line, savings or policy number); transponder model (weighted list in `agency.transponders`, by class) and serial; waiver number; contract (employer from `agency.employers` for the destination's era, term, wage); lineage; history entries; then the honest Valid Until of each expiring form the traveller carries, in form order. For the displaced: number, incident, found date, the certificate's Valid Until. Every number is unique within the day (like names), so a forged number never belongs to another traveller.

### 4.4 The clerk's own account (D1-D3)

| Group | Row | Value |
|---|---|---|
| Records | Name, Born, Lineage | authored (`agency.clerk`) |
| Records | Citizen ID | 773-2840-19 |
| Records | Status | Eligible (Standard once the debt is paid off) |
| Records | Standing | Good; Frozen on the bankrupt ending |
| Records | Debt | `agency.clerk.startDebt` (125,430 cr) − `WorldState.clerkDebtPaid` |
| Forms on file | Employment | "Temporal Customs · Desk 3"; the instalment rate ("25% of pay to Debt Relief") |
| Travel | Booked departure | none, until the bankrupt ending books the clerk's Debt Relief departure |
| | Note | authored |

No row of the clerk's account is evidence: it is nobody's case, so the PC spec draws it without pick keys. Its daily statement (wages, fines, the instalment, household costs, purchases) is the PC spec's (AC1, TC-960).

## 5. Processing rules

### 5.1 The checklists (the PC spec's optional steps checklist reads these)

**Rich tourist**
1. The destination is open today (Directives). *Directive.*
2. Name, Citizen ID and birth date on the visa, and the ID on the manifest, match the Citizen Account. *Deviation (record; the two IDs also paper vs paper).*
3. The visa class is the account's status (Premium). *Deviation (record).*
4. A Premium visa travels on a Premium transponder (the manifest's class). *Directive (from day 2).*
5. The manifest's transponder and class are the account's. *Deviation (record).*
6. The account's standing is not Frozen. *Directive (from day 3).*
7. The currency and effects are the destination's; nothing from 2150. *Deviation (book, from day 4).*
8. Dressed for the destination. *Deviation (Costume Guide, from day 2).*
9. The manifest's departure is today and the visa has not expired. *Directive (from day 4).*

**Poor tourist**: 1-3 and 5-9 as above (status Standard), and:
- A Standard visa needs an Economy transponder, a **signed** Stranding Waiver and one proof of means (asked for, handed over). *Directive (from day 2).*
- The waiver's transponder is the manifest's; its waiver number and debt are the account's. *Deviation (paper vs paper for the transponder, record for the rest).*
- The proof of means matches the account (the credit line, savings or policy on file), and its class and destination match the visa. *Deviation (record; paper vs paper).*
- The proof of means has not expired. *Directive (from day 4).*

**Labourer**
1. The worksite is open today. *Directive.*
2. Name and ID match the account; the account's status is Eligible and its standing not Frozen. *Directive for the standing; deviation for the rest.*
3. Employer, worksite, term and wage match the account's registered contract. *Deviation (record).*
4. An Economy transponder and a signed waiver whose transponder, number and debt match. *Directive for missing or unsigned; deviation for the rest.*
5. Currency, effects, costume and the manifest's departure date as for tourists.

**Displaced** (today's traveller)
1. The origin is open today. *Directive.*
2. The certificate's name, number and birth date match the registry. *Deviation (record: a liar's birth-year tell).*
3. The declaration's coin, tongue and effects, their answers and their dress are the origin's: another place's value is a false origin, a 2150 value a fake displaced person or smuggling. *Deviation (book, Costume Guide).*
4. The return order sends them to the certificate's origin, under the same incident. *Never broken in v1 (a consistency check).*
5. The return order's departure is today and the certificate has not expired. *Directive (from day 4; the displaced arrive on day 5).*

**Famous displaced**: as displaced; the registry note names them.

### 5.2 The verdict table (P1, P2)

| Traveller | Accept | Deny, ≥1 deviation logged | Deny, none logged |
|---|---|---|---|
| No fault | correct | impossible (nothing loggable) | wrong: "Denied a legitimate traveller." |
| Directive fault | wrong: the directive's reason ("Approved a closed destination." / "Approved incomplete paperwork." / "Approved a frozen account." / "Approved a departure on the wrong date." / "Approved an expired paper.") | impossible | correct (no evidence needed) |
| Deviation fault | wrong: the fault's reason ("Approved forged papers." / "Approved a disguised traveller." / "Let 2150 goods leave 2150." / "Approved a traveller who would cause a panic in {place}.") | correct | unproven denial (wrong), as today |

One fault source per traveller (K5), so no traveller has both kinds. The citation keys become `citation.acceptedWrong.{reason}` (UI strings).

### 5.3 The Directives over days

| Type | Line (Directives window, briefing) | Kinds | First day | Faulty traveller it makes |
|---|---|---|---|---|
| Procedure line (per kind, content; no predicate) | tourists: "Leisure departures: a Leisure Visa and a Departure Manifest; a Standard visa also needs a signed Stranding Waiver and a proof of means. Every paper must match the Citizen Account."; labourers and the displaced get theirs on their first day | the kind | the kind's first day | none (the liars break it: L1, L2) |
| EraForbidden / NationForbidden / NationEraForbidden | today's closure lines ("Embargo: no travel to New Kingdom Egypt today.") | all | 2 (as today) | a traveller bound for the closed place |
| PaperSet (tourists) | "A Premium visa travels on a Premium transponder. A Standard visa needs an Economy transponder, a signed Stranding Waiver and a proof of means: a Holiday Credit Agreement, a Proof of Funds or Travel Insurance." | tourists | 2 | a Premium visa with an Economy transponder; a waiver missing or unsigned; no proof of means |
| DressForDestination | "Dress for the destination. An item from another time or place causes a panic: check the Costume Guide." | 2150 citizens | 2 | a costume error (a deviation fault) |
| PaperSet (labour) | "Debt Relief departures: a registered Labour Contract, an Economy transponder and a signed Stranding Waiver." | labourers | 3 | a waiver missing or unsigned |
| DebtStanding | "Citizens in debt (Eligible) depart only on Debt Relief. Frozen accounts may not depart." | 2150 citizens | 3 | a Frozen account (the papers honest) |
| NoPresentGoods | "No 2150 currency or technology leaves 2150." | all | 4 | a smuggler (a deviation fault) |
| PaperDates | "Depart only on the date on your manifest or return order, and never on an expired paper." | all | 4 | a departure dated another day, or an expired paper |
| ReturnHome | "The displaced return only to their own time: their declaration must match their origin." | displaced | 5 | a false-origin liar (a deviation fault) |

### 5.4 How a rule's faulty traveller is made

| Rule | Maker (on the violator stream when planned, the fault stream when rolled) |
|---|---|
| Closure | today's: a place the rule forbids (`PlanViolators`, unchanged) |
| PaperSet | a compatible kind; one variant: an Economy manifest on a Premium visa; the waiver left out; the waiver's signature "UNSIGNED"; the proof of means left out |
| DebtStanding | a 2150 citizen whose standing is Frozen ("Frozen: default, 2 Mar 2150") |
| PaperDates | one variant: the manifest's or return order's departure 1-3 days off; or one Valid Until (the visa, the proof of means or the certificate) 1-30 days before today |
| DressForDestination, NoPresentGoods, ReturnHome | the matching fault forced on that slot (a costume error, L6, L7) |

A traveller who breaks a directive is otherwise honest and never lies (K5).

## 6. The lie catalogue

### 6.1 The lies

| Id | Lie (Saleh's word) | Kinds | Day | What is false | Truth | Proven by | Machinery |
|---|---|---|---|---|---|---|---|
| L1 | "not rich tourists": poor posing as rich | a Standard citizen with rich papers (drawn from the rich entry) | 1 | forged: visa class Premium, manifest class Premium; or borrowed: visa class Premium and a rich citizen's manifest (their ID, transponder and class) | account | paper vs record; the borrowed manifest's ID also contradicts the visa's (paper vs paper) | `RecordLies` |
| L2 | doctored identity | tourists (never a day-1 poor traveller) | 1 | the visa's Citizen ID or birth year | account | paper vs record; a doctored ID also contradicts the manifest's (paper vs paper) | `RecordLies` (birth year: `BirthDates.PickOtherYear`) |
| L3 | fake waiver | poor tourists, labourers | 2 | an unregistered waiver number, or a waiver made out for another transponder | account; the manifest | paper vs record; the other transponder contradicts the manifest (paper vs paper) | `RecordLies` |
| L4 | debtors posing as tourists ("escape debt and run into the past") | an Eligible citizen with tourist papers | 3 | as rich: visa and manifest classes; as poor: visa and proof class Standard, waiver debt a sliver of the real one, a proof of means the account does not hold | account | paper vs record | `RecordLies` |
| L5 | "a fake contract with more money paid to them" (forged labour papers) | labourers | 3 | one of: wage × 1.5-3; term × 0.25-0.6; employer; the worksite swapped for another open place | account's contract | paper vs record | `RecordLies` |
| L6 | smuggling (2150 currency or technology); for the displaced, "up to no good" | every kind | 4 (the displaced 5) | the currency carried or the effects: the present's value; or the answer to the currency or device question | books | paper or answer vs book (the destination's row: mismatch; the present's row: names 2150) | `Lies.Plan`, candidates = the present, categories Currency and Technology |
| L7 | false origin (today's liar) | displaced | 5 | declaration, answers, dress or birth year leak another past place | books, Costume Guide, registry | as today | `Lies.Plan` (unchanged) |
| L8 | fake displaced (a 2150 citizen) | displaced | 5 | as L7, but the values are the present's | books, registry | as today; the origin proof names 2150 | `Lies.Plan`, candidates = the present |
| L9 | the famous impostor | "Socrates" (Republican Rome) | 6 | as authored | books | as today | the premade liar path (unchanged) |
| L10 | forged proof of means | poor tourists | 2 | the proof's value is not on file: a credit line or a policy the account does not hold, or the funds × 3-10 | account (forms on file) | paper vs record | `RecordLies` |
| F | wrong dress (a fault, not a lie) | 2150 citizens | 2 | another place's signature item, 2150 clothes, or a 2150 accessory (C2) | Costume Guide | garment vs book ("DRESS INCORRECT ... would cause a panic") | `Looks.CanLeak` and `Looks.Compose(claim, leakFrom)`; a whole-look mode that flags every garment; the accessory kit as the present's leakable accessory items |
| V | missing or unsigned waiver, missing proof of means, wrong paper set, frozen account, departure not dated today, expired paper | as §5.3 | 2-4 | nothing is false: the traveller breaks a directive | the Directives | reading (no evidence) | `Directives.Breaks` |

A traveller who is asked for a form they lack answers with their kind's line (I2): an honest rich tourist asked for a waiver says "It's a Premium unit, I don't need one"; an L1 liar says the same; a V traveller says "I... didn't get round to that one." Asked for a proof of means, an honest rich tourist says "I pay my own way."

### 6.2 How the machinery extends

| Piece | Change |
|---|---|
| `Lies` | `Roll(liarChance, lieKinds, rng)`: the roll, then the kind (one `Range` draw when 2+ kinds are enabled for this traveller today). `Plan` keeps its options, home pick, tell picks and birth year, without its own roll; new inputs: an optional category filter (smuggling). `HomeCandidate` gains nothing: the present is one more candidate built from `TodaysWorld.Present`. |
| `RecordLies` (new, Domain) | `Plan(lieKind, forms, account, todaysNumbers, rng)`: the variant (one draw when the kind has variants), then each forged field's false value (one draw per value); the result applies to named fields only (a borrowed manifest rewrites that form, not every CitizenId). False-value makers, one per record category (a decision table, §6.3). |
| `LiePlan` | holds both kinds of tell (place and record) with their channel or form; `ApplyTo` rewrites the named fields; `TellValue`/`ChannelOf` unchanged for place tells. |
| `Forgery` | `IsProvableCategory`: compared record categories as BirthDate (provable when the records are reachable); `IsProvableTell` unchanged for place facts; directive-only categories never. |
| `PaperChecks` (new, Domain) | `Contradictions(documents)`: every pair of fields on two documents with the same compared category and values that differ (`DiscrepancyLog.ValuesMatch`), in document order then field order. Read by `DiscrepancyLog.Prove` and the Analysis Scanner (the PC spec's SC3). A generation test holds every honest traveller to none. |
| `DiscrepancyLog` | `Prove` unchanged for statement vs truth; `CrossMismatch` for a pair `PaperChecks` lists when one side is a tell (L4); report key `deviation.crossMismatch.papers`; `ClueLabels` words for the new categories (CITIZEN ID, VISA CLASS, TRANSPONDER, TRANSPONDER CLASS, DEBT, CREDIT, FUNDS, POLICY NO., EMPLOYER, TERM, WAGE, WAIVER NO., DESTINATION, INCIDENT). |
| `ViolatorSlots` | unchanged; `PlanViolators` counts the planned rules of §5.3 and asks each rule's maker for its slot. |
| `VerdictRules` | P2. |
| `Strandings` (new, Domain) | `Roll(accepted, chance, rng)`: one draw per accepted traveller whose account's transponder is Economy, in queue order; `Fined(waiver)`: true when no valid signed waiver was presented (S3). |
| `ClerkDebt` (new, Domain) | `Instalment(pay, share, owed)`: the garnished amount, never more than what is owed (D2). |
| `CaseInstance` | `kind`, `account` (the record), `directiveFault` (the broken rule, or none), `recordTells`, `costumeFault`; `trueHome` becomes `tellSource` (H3); `IsLiar` stays "has a place-tell source" (carries read it); `HasDeviationFault` = place tells, record tells or a costume fault. `CaseVerdict.wasLiar` becomes `faultReason`. |
| `Premades` | unchanged rules; content moves the forced slots and pools to day 6. |

### 6.3 False-value makers (`RecordLies`, each tested)

| Category | False value |
|---|---|
| AccountStatus | one class up (Standard to Premium; Eligible to Standard or Premium) |
| TransponderClass | Economy to Premium |
| TransponderId | another model of the class needed, a fresh serial |
| Debt | 0-10% of the real debt, rounded to 100 cr, never equal |
| Credit | a line in 4,000-12,000 cr where the account has none |
| Funds | the savings on file × 3-10, rounded to 100 cr; or 4,000-12,000 cr where the account has none |
| PolicyNo | a fresh policy number nobody holds today |
| Wage | × 1.5-3, rounded to 10 cr |
| Term | × 0.25-0.6, rounded to 30 days, at least 30 |
| Employer | another employer of the destination's era |
| Destination | another place open today (never a closed one, which would add a directive fault) |
| WaiverNo, CitizenId | a fresh number nobody holds today |
| BirthDate | `BirthDates.PickOtherYear` (existing) |
| DepartureDate, Expiry, Signature | never forged: directive-only (their wrong values are the `PaperDates` and `PaperSet` makers) |

### 6.4 Streams

| Stream | Seed | Draws | Change |
|---|---|---|---|
| Case | `Seeds.ForCase` | era, blueprint, role, place, name, birth date | same count: the blueprint draw now picks the kind; a 2150 citizen's name and birth date come from the present's lists and years |
| Violators | `Seeds.ForViolators` | slots, then each planned rule's maker | days with only closures draw as today |
| Lies | `Seeds.ForLies` | roll; lie kind; the planner (place: home, tells, birth year; record: variant, values) | the kind pick is new; `LiesTests`' golden orders are rewritten |
| Account (new) | `Seeds.ForAccount` ("ACCT") | §4.3 | new |
| Faults (new) | `Seeds.ForFaults` ("FALT") | violation roll, rule and variant; costume roll, variant and source | new |
| Strandings (new) | `Seeds.ForStrandings` ("STRD"), per run and day | one roll per accepted Economy traveller, in queue order | new |
| Dialog, Looks, Legendary, Clues | unchanged | unchanged | none (a costume error's source goes into `Compose` as today's leak, costing no look draw) |

## 7. Dress for the destination

### 7.1 What changes in the characters system

- **The look is the destination's** (C1). Today's code already dresses the traveller from the claim, and the claim is now the destination, so `Looks.Compose` and `LookKeys` do not change. For the displaced the destination is the origin: today's rule survives there, and a displaced liar still leaks their true home's signature item (Appearance tell, from day 5).
- **The leak item becomes a wrong item** for a 2150 citizen: a **costume error** (F, C2) in one of three variants, drawn on the fault stream:
  - one signature item of another of today's places for their gender, among the places where `Looks.CanLeak(destination, place, gender)` holds;
  - the present's whole look (2150 clothes: every garment flagged);
  - one item of the 2150 accessory kit, over an otherwise right costume (the kit is the present's wardrobe's accessory items, leakable like any signature item).
  The garment is flagged a tell, so comparing it with the destination's Costume Guide row logs "DRESS INCORRECT", and with its own row names where it belongs (2150 for the last two).
- **Per place, not per era** (C5): the Costume Guide, the Culture facts and the proofs are per place, and a same-era foreign item is still a foreigner's dress in local eyes.
- **A premade** keeps their whole picture ("Period dress").
- **Skin, hair colour and age** of a 2150 citizen come from their name's country and 2150 (C1); they are never tells, as today.

### 7.2 The Costume Guide

Today's places, as today, plus the present's row ("Temporal Customs Zone (2150): panelled coat-dress · wrist comm", or the leader's Future outfit and the kit). On the page the guide is grouped by era (C3): the claimed row first, then an era heading before each era's places, the claimed place's era first. The rows, the compare and the proofs are piece 4's.

### 7.3 The character art brief (`docs/CHARACTER_ART_BRIEF.md`)

| Brief | Change |
|---|---|
| §1 "Honest travellers wear only their own. A liar's disguise leaks one item from their real home" | "Every traveller wears the look of the time they are going to. A costume can carry one wrong item: a tourist's costume error (another place's item, 2150 clothes or a 2150 accessory), or a displaced liar's real home." |
| §1 skin and hair "where the traveller claims to be from" | "where they are from: a 2150 citizen's family country; a displaced person's claimed home" |
| Batches 3-7 (40 places × 2 genders) and each place's "must read at a glance" item | unchanged coverage; the signature items are now also the costume errors, seen more often, so their readability matters more |
| Batch 8 (the Future, 20 images) | needed: the present's wardrobe, worn by a tourist who forgot their costume (2150 clothes) and by the present's Costume Guide row |
| New: the 2150 accessory kit | one small set per gender (for example a wrist comm, smart lenses, a transit badge, a sleeve display), each item readable at a glance and leakable over any period costume; filed under the art nation `neutral` |
| New, future phase: the transponder unit | a handheld device the traveller sets on the desk, with a small readout of model, serial and class; Premium and Economy variants (F3; not scheduled) |
| Batch 9 (premades) | unchanged; they arrive on day 6 |

## 8. The interview

- **Claim lines per kind** (`interview.claim` becomes a per-kind table): tourist "One leisure departure to {place}, please."; labourer "Debt Relief departure to {place}. It's all in the contract."; displaced "Please. Send me home to {place}." The claim tag and the banner read the same line.
- **The ask menu per kind**: "Ask about the trip >" (tourists, labourers: Currency "What will you pay with in {place}?" / "I've changed my money into {value}."; Device "What are you taking with you?" / "Just the approved kit: {value}.") and "Ask about home >" (the displaced: today's six questions and wording). `questions[]` gains `kinds`; `overrides` gain `kind`. The trip questions arrive on day 4, when a spoken tell (smuggling) first exists; the home questions on day 5; the birth-date question stays upgrade-gated and hint-only.
- **Small talk per kind** (content): tourists excited, labourers grim, the displaced as today (the place's and era's lines).
- **Requests**: the primary form on arrival; "Request papers >" lists the forms of `askableBy`, one entry per `askGroup` (2150 citizens: Manifest, Waiver, Proof of means; the displaced: Intake Declaration, Return Order), "< Back" in the centre. The hub's worst case stays 8: papers 1, spoken requests 2, ask 1, look 1, dialogs 2, a premade's dialog 1. `DialogChecks.MenuProblems` counts the sub-menu as one hub entry and checks it (1 + at most 3).
- **Translation** (I3, I4): no form is ever in a tongue (F5). A 2150 citizen speaks English, whoever leads. A displaced traveller speaks their claimed origin's tongue from day 5 (`translation.fromDay` 5, announced in day 4's paper): in the bubble and the transcript an untranslated line keeps its key words in English (§8.1), and with the region's Speech translator its line flips into English as today. The shop sells the four Speech translators; buying one before day 4's night buys nothing until day 5, which the notice says.

### 8.1 The key-word rule (I4)

`world_source.json` `translation.keyWords` (Generate World writes it into the translation settings; the validator checks it):

```json
"keyWords": {
  "slots": ["place", "name", "document"],
  "words": ["home", "please", "papers", "yes", "no", "Temporal Customs"],
  "digits": true
}
```

- A traveller's line is formatted from a template and its slots (`{place}`, `{value}`, `{name}`, `{document}`). A pure Domain rule, `KeyWords.Spans(template, fills, rule)`, returns the character spans that stay English: the fill of each listed slot, each whole-word match of a listed word (case- and accent-insensitive), and digits (which the pseudoscript already passes through).
- Everything else in an untranslated line is drawn in the tongue's glyphs (`Pseudoscript`), in the bubble and in the transcript. The Speech translator flips the glyph spans only (`FlipSequence` skips the English spans).
- `{value}` is not a key slot by default: an answer's value stays in the tongue, so the Speech translator still earns its price, and the answer still compares on its canonical value.
- Right-to-left tongues keep English spans in their own order through `ArabicShaper` (a test covers Latin inside Arabic: audit R2-022).
- The validator reports an unknown slot name, a blank word and a duplicate word.
- Example (a Greek displaced traveller, no Speech translator): "Please. Σενδ με home το Periclean Athens." (glyphs illustrative).
- Tuning is an edit to the lists ("this we will tune later").

## 9. History and the present

- **The present** (H1): with a leader, the leader's Future place (its facts and its outfit; names stay the eight lists together, K4); with none, the neutral present from `world_source.json` `present` (proposed: "Temporal Customs Zone", Currency "Credits", Language, Technology, capital and ruler to author, all unique today; its outfit is the art brief's `future_neutral` and its accessories the kit). `ContentLibrarySO` builds it once per day into `TodaysWorld.Present`, and `FillFacts` adds its row, so every book lists it from day 1.
- **The Future era** (H2) keeps its places, ids and history role; `TodaysProfiles` stops offering it as a destination; the validator's Future-day checks ("a Future day allows every country", "day plans checked once per possible leader", era rules on the Future place) go with it. The era's display name becomes "2150".
- **Impacts** land on the destination on accept (unchanged code): a tourist's role touches the place they visit, a labourer's the worksite, a displaced person's their home. Roles are unchanged for every kind.
- **Carries** (H3, S2): an accepted traveller with a place-tell source carries that source's Technology into the destination (`HistoryService.RecordCarry` reads `tellSource` ids and today's facts, which hold the present's row); a stranded traveller carries the present's Technology the same way. A record liar or a costume error carries nothing. Accepting a costume error adds the panic news line (P5).
- **Influence, the leader and dominance news** are unchanged (past places only).

## 10. The debt theme (a light touch)

- **Morning paper**: one debt-economy line a day from a pool (`news.debt`, content: "Debt Relief Departures reach a record high.", "CLEAR YOUR DEBT: 180 days in the Victorian mills.", "12,000 accounts frozen in default.", "Premium families book Periclean Athens for the summer.", "A stranded tourist's family inherits 212,000 cr.") and counts from yesterday ("43 citizens left on Debt Relief yesterday."). Each stranding adds a line (`news.stranded`: "Stranded: {name}, lost in {place} when an Economy transponder failed."). The PC spec's news site shows them.
- **Shift ledger**: "Leisure departures: n", "Debt Relief departures: n (x cr of debt put to work)", the clerk's instalment (D2) and any stranding fine (S3).
- **The clerk**: the Citizen Account app opens on the clerk's own account (D1); each shift's pay chips at the debt (D2); the bankrupt ending is the clerk's own Debt Relief departure (D3).
- **The office art** carries the rest (the sign, the banners, "CLEAR YOUR DEBT", "ATTENDANCE REQUIRED"); a briefing line may echo "Attendance at your Debt Relief departure is required. Missed departures are frozen."

## 11. Kept, changed, removed

| Item | Fate | Migration risk |
|---|---|---|
| Domain: `FactTable`, `DiscrepancyLog` proofs, `Lies.Plan`, `Forgery`, `BirthDates`, `NameRoster`, `ViolatorSlots`, `Premades`, `Looks`/`LookKeys`/`CanLeak`, `InterviewScript`/`DialogRunner`, history (influence, leader, carries), the speech half of translation | kept | none |
| The desk, the PC, the wheel, the compare bar, the Deviation Report, the stamps, the evidence gate | kept (same mechanics) | none |
| 40 places, facts, wardrobes, names; the six books; closures; roles; premades | kept | premades change days (below) |
| `CaseFactory`, `CaseInstance`, `CaseVerdict`, `ShiftScoring`, `VerdictRules` inputs, `CitizenRegistry` (rows), `DocumentTemplateSO` (+`formNumber`, `askableBy`, `askGroup`, `form`), `CaseBlueprintSO` (+kind), `DayPlanSO` (kind weights with `honest`, lie kinds, chances, guarantees), `TravelRuleSO` (+types), `InterviewContent` (per-kind claim, overrides, missing-form replies), `HistoryService.RecordCarry`, `TodaysWorld` (+Present), `ContentLibrarySO` (+present, agency content), `EndingService`'s bankrupt copy, `WorldState` (+`clerkDebtPaid`), the office calendar readout (the date) | changed | the case-generation dump and the play transcript (re-baselined per phase, the plan says which) |
| `DocTemplate_Passport`, `DocTemplate_Permit`, "Bond Currency", `CaseBlueprint_Investigation` | removed (replaced by the ten forms and four blueprints) | any scene or test naming them (the builder's paper face is generic; `DocumentRowsTests` and fixtures use their own fields) |
| The Future as a destination (day 6 weight, Future-day validator rules, era/nation rules on the Future place) | removed | a save made on day 6+ of an old run continues without Future travellers (intended) |
| Senenmut day 1 slot 3, "Socrates" day 2 slot 6, pools on days 2-5 | moved to day 6 | an old run whose premades were already met will not meet them again (met flags are kept) |
| `CaseInstance.trueHome`, `claimAllowedByRules`; `CaseVerdict.wasLiar` | replaced (`tellSource`, `directiveFault`, `faultReason`) | six files read `trueHome` (CaseFactory, CaseInstance, ShiftLedger, GameManager, ShiftScoring, HistoryService) |
| Question days (currency, language, device day 1; capital day 2; ruler day 3) | moved: the trip questions day 4, the home questions day 5, with their announcements | the day-2 and day-3 unlock lines move with them |
| The text-mode fallback investigation | removed (the PC spec's Q3: "delete it") | FEATURES.md "Fallback text-mode investigation" goes; audit R4-002 |

### 11.1 What of piece 9 retires (the translation change)

Documents always show English (F5), so everything that drew a document value in a tongue goes; the speech machinery stays.

| Retires | Why |
|---|---|
| The four Papers translators (`Upgrade_Tr_{Pack}_Papers`, ids `tr_{pack}_written`), `TranslatorKind.Written`, the generator's and validator's written branches | no document is in a tongue. Old saves that own one keep a harmless id (nothing reads it). |
| `Translation.InTongue(ClueCategory)`; `CaseTranslation.Field` and `PapersTranslated`; the field path of `CaseTranslation.Shown` and `EvidencePicks`' field placeholder | a field is always plain. |
| `DocumentReveal`, `RevealClock` (and `RevealClockTests`), `DocumentRows.TongueRow` and `HasTongue` (and their tests), `DocumentRowView`'s foreign branch (`FillRest`), the desk paper's and the scanned window's flips, piece 10's sightings (X25: `InvestigationUIController.Sighted`), `FlipTiming.rowStagger` (`translation.flip.rowStagger`) | the written reveal had no other job. |
| The Intake Declaration's `declarantTongue` (this spec's first draft) | F5. |

| Kept for speech | Note |
|---|---|
| `Translation.InTongue(DialogSpeaker)`, `TranslationDay`, the tongues, scripts, packs and glyph tables, `Pseudoscript`, `FlipSequence`, `DisplayText`, `TextFlip` (its font fix, audit R4-024), `CaseTranslation.Line`/`Bubble`/`Shown` for answers, `TranslationPresenter`, the script fonts, `compare.untranslated`, the four Speech upgrades, `MotionPreference`, the notice trigger | plus the key-word spans (§8.1) |

- **Saves (M1):** version 2 stays; nothing persisted changes shape (the history block, flags and counters keep their grammar; the present's id is a new string in carry records; `clerkDebtPaid` loads as 0 from an old save). A mid-run save continues under the new days.
- **`docs/FEATURES.md`** is rewritten section by section in the phase that changes it; the plan lists each phase's lines. No "(tested: X)" until X exists.
- **Golden masters:** the offline suite's golden values that change are `LiesTests` (draw order), `SeedsTests` (three new salts, one distinctness table), `DiscrepancyLogTests` (new categories; cross proofs), `InterviewScriptTests` (the sub-menu, capacity) and `DocumentRowsTests` (the retired tongue rows). The Phase 0 golden masters (`docs/reviews/audit-baseline`: the case-generation dump, the play transcript and saves, the scene dumps) are re-baselined in the phases the plan names, each diff explained in its commit.
- **The character brief's leak items:** the same items, a second job (§7.3); Batch 8 and the accessory kit are needed.
- **Balance:** the epilogue thresholds (Art 41, Science 26, Democracy 9) were set from simulated perfect play; new kinds, strandings and the clerk's instalments change how money and history move, so the 50-run simulation is re-run in the plan's balance phase.

## 12. Build order

The combined plan (`docs/superpowers/plans/2026-09-26-redesign-plan.md`) orders this spec's work together with the PC redesign's. This spec's parts land in these phases:

| This spec's part | Plan phase |
|---|---|
| Speech-only translation (F5, I3's speakers, I4, §8.1, §11.1) | 1 |
| Records as rows, find by number, the agency block and the calendar date (R1, R3, F6); the fallback deleted | 2 |
| Kinds and the displaced's forms (K1, K2, F1-F4 for TC-610/620/630, the Displaced blueprint on every day) | 3 |
| The present, the Citizen Account and rich tourists (H1, K4, R1-R4, TC-101/230) | 6 |
| Record lies and paper proofs (L1, L2, L4, `Lies.Roll`, P1, P2) | 7 |
| Poor tourists (TC-310, TC-415/416/417, the proof choice, the papers sub-menu I2, day 1's `honest` entry) | 8 |
| Labourers and the directives (TC-520, P3, P4, `Seeds.ForFaults`, L3-L5, L10) | 9 |
| Dress for the destination (C1-C5, P5, the costume roll) | 10 |
| Smuggling and dates (L6, F7, `PaperDates`, I1, H3) | 11 |
| The displaced from day 5, the famous on day 6 (K3, L7-L9, H2, I3's day 5) | 12 |
| Strandings and the clerk's debt (S1-S3, D1-D3, T1) | 13 |
| Balance (the epilogue re-simulation, the knobs) | 23 |
| The spreadsheet import (CS1, §15) | 26 |
| Art (Batch 8, the accessory kit) | 27 |
| The transponder desk device (F3) | future, not scheduled |

## 13. Q-items (answered 2026-09-26)

| Q | Question | Saleh's answer | Resolved as |
|---|---|---|---|
| Q1 | Day 1's travellers | Rich and poor tourists from day 1, but poor tourists are always honest on day 1 (option C). | K3, K5 (`honest` kind entries skip every fault roll), §2.3 (day 1: rich 1, poor 1 honest; its liars are L1 and L2, both rich-presenting) |
| Q2 | Where the transponder lives | On the papers now; a physical desk device later (option C). | F3; §7.3 (the transponder unit, a future art item); the plan's future phases |
| Q3 | Do two papers that disagree prove a forgery? | Papers prove, answers hint (option B). | L4 (`CrossMismatch`, `PaperChecks.Contradictions`, shared with the PC spec's Analysis Scanner) |
| Q4 | What a wrong-era costume can be | Another place's item or 2150 clothes, plus a new 2150 accessory set (option C). | C2-C4; §7.1, §7.3 |
| Q5 | Do cheap transponders really fail? | Yes, with a fine: a seeded stranding chance that can change history through the carries; a fine for letting one through without a signed waiver (option C). | S1-S3; `Seeds.ForStrandings`; §9, §10 |
| Q6 | Is the clerk in debt too? | Yes, and it drives the ending: the account is the clerk's (773-2840-19); the bankrupt ending is the clerk's own Debt Relief departure (option C). | D1-D3; §4.4; the PC spec's AC1 |
| Q7 | The poor tourist's fourth paper | "any of the above": one of the three per traveller, seeded; each can be forged. | F2; TC-415, TC-416, TC-417 (§3.4-3.6); I2's one "Proof of means" request; L10 |
| Q8 | Date checks | The date and an expiry: the departure date must be today from day 4, and papers carry an expiry date that must not have passed. | F4 (directive-only dates), F7, P3 (`PaperDates`), §5.3, §5.4 |
| Q9 | Per era or per place? | The exact place is checked, and the Costume Guide is grouped by era (option C). | C3, C5; the PC spec's Reference register |

## 14. Intent audit

**Against Saleh's words.**
- *Three types, different documents and rules, same mechanics:* four kinds (the tourist split by paper count), each with its forms and checklist; the desk, the wheel, the compare, the report, the evidence gate and the stamps are unchanged.
- *"trapped with massive debt ... while others are escaping for fun":* the account's debt drives the labourer, the poor tourist's proof of means, the debtor's lie, the theme lines and now the clerk's own account and ending (D1-D3); the rich tourist's two papers show who escapes easily.
- *"All 2150 agency forms":* ten TC forms, all filled in English; the era shows only in values (destinations, currencies, devices, a displaced person's declaration).
- *The order:* §2.3, with his Q1 answer: both tourist kinds on day 1, the poor always honest that day.
- *The paper counts:* 2 / 4 / 3 / 3 (F2); the poor tourist's fourth paper is any of three (Q7).
- *Dress for the destination, or panic:* C1-C5, P5; the exact place is checked (Q9).
- *Transponders, cheap ones for labourers and poor tourists, the waiver:* F3, the manifest's class, the waiver form, the paper-set directives; cheap ones really fail and cost a fine (S1-S3).
- *All kinds of lies:* L1-L10 cover debtors fleeing (L4, L8), inflated contracts (L5), poor posing as rich (L1), forged proofs of means (L10), fake and shady displaced people (L8, L6), smuggling (L6), forged waivers (L3), plus the old origin lie (L7).

**Against his answers and instructions.** Each Q-item is applied where §13 says, and each instruction where the answers table says. "all documents must be filled in english": F5 and §11.1 remove every path that drew a document value in a tongue. "key words will still be in english ... this we will tune later": §8.1 makes the key words data. "an excel file for all cases later": CS1 and §15 make the content row-shaped and schedule the import, with nothing built now.

**Redo or override checks.**
- Overrides piece 2's "every traveller goes home" (the claim is now the destination; for the displaced it is still home), piece 5's Future destinations (H2) and the premade schedule (piece 4). Each is required by Saleh's model.
- Overrides piece 9 twice, by Saleh's instruction: `fromDay` 2 becomes 5 (I3), and papers translation retires (F5, §11.1).
- Overrides piece 3's R2 for papers only: two papers that disagree now prove (L4); an answer against a paper is still a hint.
- Overrides piece 10's shared written reveal and sightings (§11.1).
- Reuses, never duplicates: the claim fields, the blueprint pick, `Lies.Plan`, `BirthDates.PickOtherYear`, `ViolatorSlots`, `DiscrepancyLog.Prove` and `ValuesMatch`, `Looks.CanLeak`, `Carries` (for smugglers and strandings alike), the premade liar path, the directive list, the bankrupt ending's condition.

**No-code-path checks.** The kinds are blueprints (data); the day mix, `honest` entries, lie kinds, chances, directives, key words, proof weights and the clerk are `world_source.json` content through Generate World; the forms are template assets. New code is limited to the Domain rules that did not exist (record lies, directive predicates, the fault order, paper contradictions, strandings, the instalment, key-word spans) and the glue that calls them.

## 15. Content as rows (CS1; the spreadsheet path, a later phase)

Saleh will author "all possible dialogue options, all possible dialogue interactions, all real characters, all premade characters and stories" in a spreadsheet. The data is shaped for that now; the import is built in the plan's content-import phase.

**Row rules** (every table this spec adds or touches follows them, and the PC spec's `pc` tables too):
- One record per row, one table per sheet. Each table has an `id` column, unique within it; child tables name their parent's id.
- Columns are scalars: text, integer, number, true/false, an enum name, an id, or a list of ids or enum names in one cell separated by `|`.
- Nothing nests deeper than a child table: a dialog's nodes, lines and choices are three child tables, a place's names and wardrobe items two, a mail's body lines one (with an `order` column).
- A blank cell means the column's documented default; the importer never guesses, and an unknown column is an error (audit R6-014's strict keys).
- Text keeps its slots (`{place}`, `{value}`, `{name}`) and is English; flavour translations stay in the UI tables.

**The tables, as rows** (the ones this spec adds in bold): `places`, `names` (place, gender, name), `wardrobe` (place, gender, slot, item, signature), **`present`**, **`accessoryKit`** (gender, item), `rules` (+ the new types), `days`, **`dayKinds`** (day, kind, weight, honest), **`dayLies`** (day, lie kind), `questions` (+ `kinds`), `wording` (question, kind, era, place, prompt, answer), **`claims`** (kind, text), `requests`, **`missingFormReplies`** (kind, askGroup, variant, text), `smallTalk` (kind, place or era, text), `dialogs`, `dialogNodes`, `dialogLines` (dialog, node, order, speaker, text, expression), `dialogChoices` (dialog, node, choice, label, next, effect), `premades`, **`agencyTransponders`** (id, class, model, weight), **`agencyEmployers`** (id, era, name), **`agencyProofs`** (form, weight), **`agencyClerk`** (one row), **`newsDebt`** and **`newsStranded`** (id, text, fromDay), **`keyWords`** (kind: slot or word, value), and the PC spec's `steps`, `sites`, `pages`, `ancestryPeople`, `ancestryRelations`, `mail`, `mailLines`.

**The pipeline** (built later): the workbook (one sheet per table) → CSV per sheet (UTF-8), or the XLSX read directly if that needs no third-party package → `Tools > TimeDesk > Import Content Sheets` writes `world_source.json`, or its successor (one JSON file per table), in the same formatting → **Generate World**, unchanged in its role → the validator. An exporter writes today's content out to the same sheets first, so the workbook starts from what exists. The row parsing and its checks live in a tested assembly, not in editor-only code (audit R6-007).

## 16. The seam with the PC redesign spec

The PC redesign spec's §12 is the contract; a change to one of its rows updates both specs in the same commit. What this revision changed on that seam:

| Here | There |
|---|---|
| F1: forms at most 6 fields, fitting the face (`FormLayout.Check`) | FO10: "fits the face at the floors"; the §6.2 layouts (TC-101 with Valid Until as field 5) |
| F2: ten forms (TC-416, TC-417 added) | FO3, FO9 (TC-1xx to TC-6xx documents, TC-9xx PC pages); a document-kind glyph per form (§14 there) |
| F5, I4, §8.1: documents always English; speech with key words | TR1-TR3, SE5, CP2: untranslated text is speech only (the transcript and the bubble) |
| L4: `PaperChecks.Contradictions` | SC3: the Analysis Scanner marks the first pair; CM1 unchanged |
| D1-D3: the clerk's account, 773-2840-19 | AC1, §2.13: the Citizen Account app is the clerk's; its statement has the instalment and the debt |
| C3: the Costume Guide grouped by era | §2.6: the Reference register's era groups for the Costume Guide |
| F4: `Funds`, `PolicyNo`, `Expiry` | §4.3: SmartLinks map `Funds` and `PolicyNo` to the record, `Expiry` to Rules |
| §5.1: checklists | ST3: the step sets per kind |
| §10: news lines, strandings, the instalment | IN3 (news lines), AC1 (the statement), ML1 (mail) |
