# The desk-first ramp and the introduction registry: design

*2026-10-05 · the desk-first redesign, track A (`feat/streamline-ramp`) · Saleh's items 2, 9 and 10 ("day 1 = 1 paper + 1 destination accepted, then every day one new rule or paper"; "app features hidden until their rule is unlocked"; "far too much to check: streamline like Papers, Please") and his answer "deny freely, but a denial with no logged evidence earns a citation" · decided and built by Claude for Saleh's review · the code wins over this text*

## 1. The ramp

One new paper or rule a day. The bulletin (the briefing's first line) names it.

| Day | New | Papers | Rules (beside `Rule_Open_Dnn` and `Rule_PaperDates`) | New lies | Introduced (`days[].introduces`) |
|---|---|---|---|---|---|
| 1 | the Passport, one open destination (Periclean Athens), its Valid Until, its photo | TC-101 | – | someone else's photo | calendar, rulebook, apps Investigation, Mail, Notes, Settings; fields Name, Destination, Expiry, Photo |
| 2 | the Entry Ticket: its Citizen ID against the Passport's, its date today | + TC-230 | – | doctored identity (Citizen ID) | fields Citizen ID, Departure date; app Internet |
| 3 | a second destination, on the Departure Board | – | – | – | the board; apps Portals, Orders |
| 4 | issuing seals and the Seal Register | – | – | forged seal | field Seal, book Seal |
| 5 | the scanner and the records; with them the displaced and their Travel Permit | + TC-610 | `Rule_LeisureDepartures` (every traveller's record: Citizen Account or Displacement Registry) | (the doctored birth year shows) | scanner, Records, fields Birth date and Incident, app Citizen Account |
| 6 | the Transponder Card and travel class | + TC-240 | `Rule_TravelClass` | poor posing as rich | fields class, transponder, transponder class |
| 7 | the Stranding Waiver | + TC-310 | `Rule_TouristWaiverSet` (replaces the class rule) | fake waiver | fields Signature, Waiver No. |
| 8 | Debt Relief labourers, the Work Permit | + TC-520 | `Rule_LabourPaperSet` | forged contract | fields Employer, Term, Wage |
| 9 | dress for the destination | – | `Rule_DressForDestination` | (costume errors) | the Look menu's garments, the Costume Guide |
| 10 | debt standing | – | `Rule_DebtStanding` | debtor posing as a tourist | the record's Standing and Debt, field Debt |
| 11 | the Driftbox 3 recall | – | `Rule_DriftboxRecall` | – | – |
| 12 | bans: a board route CLOSED (Weimar Berlin) | – | `Rule_NoModernGermany` (that day only) | – | – |
| 13 | the Economy range limit | – | `Rule_NoEconomyAncient` | – | – |
| 14 | the return home, the Intake Declaration | + TC-620 | `Rule_ReturnHome` | false origin, fake displaced | TC-620's Currency, Language, Technology; books Tongues, Capitals, Rulers; the Language, Capital and Ruler questions |
| 15 | the Return Order | + TC-630 | – | – | – |

Day 1 is the baseline (two rules at once: the open destination and the dates); `DayPacing` checks only its bulletin. The open destinations count with the closures, so a new day's open places are no new check.

The story beats keep working (checked by the balance simulation, 50 of 50 perfect runs): Pell Quimby days 7 (unsigned waiver, the waiver's day), 11 (the recall, moved from day 10) and 15; Ines day 8; Gutenberg day 9; Rook day 11; Hollis day 13; the auditor, "Ada Lovelace" and "Socrates" day 14 (the return home's day; Ada and Socrates moved from day 12). Track E's famous travellers (displaced) keep their forced slots and pools from day 6.

## 2. Decisions made on Saleh's behalf

1. **The passport is TC-101 renamed** ("Citizen Travel Passport", the old visa's data and fields; the class label reads "Travel Class"). The Entry Ticket is TC-230 renamed, the Work Permit TC-520 renamed. A shorter title "Passport" overflowed the form by 0.003 of a page, so the title is the longer one. Track D draws the passport booklet over TC-101.
2. **The ticket matches the passport by Citizen ID**, not by name: TC-230 prints no name. The date part is the paper-dates rule, now from day 1 (an expired passport on day 1, a ticket dated another day from day 2).
3. **The Transponder Card (Track D's TC-240) arrives on day 6 with the travel class** (the class day, see 4), handed over on arrival beside the Passport (no request line or missing-paper voices needed), carried by every 2150 citizen (the three citizen blueprints, after the ticket), issued by the Portal Hall Dispatch with the ticket. The ticket keeps its own transponder fields; a forged class on the passport differs from the card's true one.
4. **Waiver and class swapped (the class and the card day 6, the waiver day 7; Track D's note asked for the card on day 7, the brief's class day)**, and the waiver is the Economy unit's, not "Ancient and Medieval". In the fiction and the code every Economy account registers a waiver (`AccountMaker.HoldsWaiver`); an era waiver would be a second waiver system. The swap also lands Pell's unsigned-waiver beat on the waiver's own day.
5. **The second destination stays the only other one**: two open places a day from day 3 (the board's 01 and 02; 02 enters service on day 3 by itself). A repaired 04 or 05 shows its route CLOSED.
6. **Cut from the 15-day ramp**: the proofs of means (never issued; their voice lines removed), smuggling and the no-2150-goods rule, the Currency and Device questions (fromDay 16), the plain daily closures (the open destinations are the bans). The luggage fields (TC-230's Currency Carried and Declared Effects) never show.
7. **Before the scanner (days 1-4)** a handed-over paper's copy reaches the PC at once, quietly, so the workbench (rules, calendar, click-and-match) can prove the day's faults until Track B's desk inspection lands. Track B removes this (`CaseDocumentsPresenter.SetDay`'s `copiesOnHandOver`). The copy's strip still reads "SCANNED" (Track B).
8. **Steps and the Deviation Report are hidden, not deleted.** The steps run headless (the decision view, Ctrl+1…5, the audit play), until Track C's menu bar replaces them; the Report tab is unreachable from the findings column and gets no badge. Its search entries still exist (Track C).
9. **The displaced arrive on day 5 with the records** (merged after Track E landed: its famous travellers are displaced and appear from day 6). Their check is the same record check (`Rule_LeisureDepartures` now reads "every paper must match the traveller's record"; `Rule_DisplacedReturns` is no longer listed), so day 5 brings one paper (TC-610) and one rule. The ban (a plain closure) takes the displaced's old day 12. Foreign speech starts on day 8 (its notice in Orders' reach, day 6); Interview Protocols' question (date of birth) from day 5.
10. **Day 1's mix**: rich 1 : poor 2 (honest), violation chance 0, so day 1 is about 3 deniable travellers in 8 (the closed destination, the expired passport, a swapped photo).

## 3. The registry: the API for tracks B and C

`Introductions` (Domain, `Assets/Scripts/Domain/Introductions.cs`; tested: `IntroductionsTests`), one instance per library: `ContentLibrarySO.Introductions` (built once from the day plans and the questions).

```csharp
Introductions intro = library.Introductions;      // RunManager.Instance.Library, or the GameManager's contentLibrary
intro.Has(day, Feature.Scanner);                  // introduced on or before day
intro.FirstDay(Feature.Paper("TC-230"));          // 2; 0 = never
intro.IsNew(day, key); intro.NewOn(day);          // today's new things
intro.ShowsField(day, "TC-101", ClueCategory.Expiry);
```

Keys (`Feature`):

| Key | Source | Example |
|---|---|---|
| `Feature.Paper(form)` | `days[].papers` | `paper:TC-310` |
| `Feature.Rule(asset)` | `days[].rules` | `rule:Rule_DebtStanding` |
| `Feature.Lie(LieKind)` | `days[].lies` | `lie:ForgedSeal` |
| `Feature.Question(id)` | `questions[].fromDay` | `question:q_capital` |
| `Feature.Scanner`, `Board`, `Calendar`, `Rulebook` | `days[].introduces` | `tool:scanner` |
| `Feature.Clothes` | `days[].introduces` | `wheel:clothes` |
| `Feature.Records`, `Standing` | `days[].introduces` | `pc:records` |
| `Feature.App(DesktopAppIds id)` | `days[].introduces` | `app:orders` |
| `Feature.Book(ClueCategory)` | `days[].introduces` | `book:Seal` |
| `Feature.Field(category)`, `Feature.Field(form, category)` | `days[].introduces` | `field:Expiry`, `field:TC-620/Currency` |

A new named key (a desk tool, a PC menu, a wheel entry) is added to `Feature` and to `Introductions.Named`; Generate World and the validator then accept it in `days[].introduces` (`Introductions.Problems`) and it goes into the day's list in `world_source.json`. Nothing is hidden by date in code: a feature is introduced on the first day a plan lists it.

What reads it today: `TimelineService.BuildScannerDay` (`ScannerDay.Hidden`), `GameManager.BoardIntroduced` (the board), `InvestigationApp.SetIntroductions` (the shelf's agency chips, the desktop icons), `ReferenceView.OnShelf` (books), `CaseDocumentsPresenter.SetDay` and `DocumentForm.For` (`FormData.FieldHidden`), `CaseFactory.ShownForms` (lies forge only fields shown), `TimelineService.BuildInterviewDay` (`InterviewDay.Clothes`), `GameManager.BuildRegistry` (Standing).

For **Track B** (the desk): inspection on the desk should hide the fields the registry hides (`FormData.FieldHidden` already reaches the desk paper: hidden slots are `FormSlot.Hidden` and draw nothing), and the scanner arrives on day 5 (`ScannerDay.Hidden`). For **Track C** (the wheel and the PC): the menus and the apps read `Has(day, Feature.App(...))`, the Records menu `Feature.Records`, the Books menu `Feature.Book(...)`, the calendar `Feature.Calendar` (from day 1, on the PC too), a missing-paper flag `Feature.Paper(form)`, a question `Feature.Question(id)`.

## 4. Deny

Saleh: "the player may deny freely, but a denial with no logged evidence earns a citation". `VerdictRules.IsUnprovenDenial` now covers a directive fault too; the evidence is a deviation's proof or a directive finding (`FindingRules.IsDirectiveEvidence`: a rule broken, a departure not today, an expired paper), counted at the decision (`InvestigationUIController.EvidenceCount`). It is still the one penalty (the free warning first).

## 5. Balance (50 runs, `SCRATCH/deskfirst/A/balance_after`)

Perfect play: world report ×50, pay 2246 a run (2322 before), 0 wrong calls, the lowest wallet 50. Imperfect: world report ×50, stability 68.6 at the end. Careless: fired 47, bankrupt 3 (whole queue); bankrupt 49, fired 1 (10 a shift). Faulty travellers 30-50 % a day (day 1 33 %: the closed destination, the expired passport, a swapped photo). The audit play (days 1-3, seed 12345) passes 680 checks; its two failures are the full run's ("ends at night 15" and the planned mistakes of days 3-4). Every day 7-15 beat stands in 50 of 50 perfect runs with the right call. Seed-12345 determinism passes; no errors.
