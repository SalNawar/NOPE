# The documents: fault canon and field audit

Every paper a traveller hands over, what each field is checked against, and the
small published canon of faults a document can carry (the document design spec,
`docs/superpowers/specs/2026-09-30-document-design.md`, lessons D3, D4 and D9).

**The canon is content.** It is `world_source.json` `agency.faults` (the content
spreadsheet's `agencyFaults` sheet; the narrative workbook shows it read-only on
its `Faults` sheet). The case factory draws only from it: a record lie's
variants are its rows (`RecordLies`), a forged seal's forgeries and papers and
the photo's forms are its rows (`VisualLies`), a place lie prints its tells only
on the fields its rows name, and a directive's falsified dates only on the
fields its rows name (`CaseFactory.CanonFields`). Generate World and Validate
Content Library refuse a canon that names a field its form does not print, a lie
or directive that does not exist, or that leaves out a field a maker could
write (`DocumentContentChecks`). The tables below are generated from the
templates and the canon; edit the content, not this page.

**How to read a fault.** One wrong decision is one fine, whatever the fault
(Saleh's rule 1). Every fault is proven the same way: pick the value on the
paper, pick what it is checked against, and the connecting line says *Data
differs* (a logged deviation, the evidence a denial needs). A directive fault
(a date, a missing or unsigned paper, a recalled unit) is read against today's
rules and needs no evidence. Faults not on a paper (a closed destination, a
frozen account, a costume error, an answer's tell) are outside this canon.

**Deliberate decoys.** A field marked *deliberate decoy* is checkable and always
consistent: a Citizen ID or Displacement No. on a secondary paper ties that
paper to its holder (the borrowed manifest shows what a wrong one means), a
destination repeats the spoken claim, and an incident number is the registry's
reference. They stay on the papers so that reading a paper is never a checklist
of only the fields that can lie; none can carry a fault, so checking one is
never required for a right decision. Names are the record lookup key and never a
tell (`Forgery`).

## D3: every field of every document

| Form | Field | Category | Checked against | Faults the canon can print there | Status |
|---|---|---|---|---|---|
| TC-101 Leisure Departure Visa | Full Name | Name | the record lookup (Citizen Records by ID, else name) | none | lookup key (never a tell: Forgery) |
| TC-101 Leisure Departure Visa | Citizen ID | CitizenId | Citizen Account / Displacement Registry; the other papers | DoctoredIdentity/doctoredId | checked |
| TC-101 Leisure Departure Visa | Date of Birth | BirthDate | Citizen Account / Registry · Born | DoctoredIdentity/doctoredYear | checked |
| TC-101 Leisure Departure Visa | Destination | Destination | the spoken claim; Citizen Account · Forms on file (a contract's worksite); Registry · origin | none | deliberate decoy (checkable, never faulted) |
| TC-101 Leisure Departure Visa | Visa Class | AccountStatus | Citizen Account · Status | DebtorPosingAsTourist/debtorAsPoor, DebtorPosingAsTourist/debtorAsRich, PoorPosingAsRich/richBorrowed, PoorPosingAsRich/richForged | checked |
| TC-101 Leisure Departure Visa | Valid Until | Expiry | the desk calendar (today's rules: paper dates) | ExpiredPaper | checked |
| TC-101 Leisure Departure Visa | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |
| TC-101 Leisure Departure Visa | Photo | Photo | the traveller at the desk (Look > Their face) | SwappedPhoto/photo | checked |
| TC-230 Departure Manifest | Citizen ID | CitizenId | Citizen Account / Displacement Registry; the other papers | PoorPosingAsRich/richBorrowed | checked |
| TC-230 Departure Manifest | Transponder | TransponderId | Citizen Account · Transponder; the other papers; today's recalls | PoorPosingAsRich/richBorrowed, RecalledTransponder | checked |
| TC-230 Departure Manifest | Transponder Class | TransponderClass | Citizen Account · Transponder class; today's rules (paper set) | DebtorPosingAsTourist/debtorAsRich, IncompletePapers, PoorPosingAsRich/richBorrowed, PoorPosingAsRich/richForged | checked |
| TC-230 Departure Manifest | Currency Carried | Currency | Currency Ledger (the claimed place's row) | Smuggling/tell | checked |
| TC-230 Departure Manifest | Declared Effects | Technology | Index of Devices (the claimed place's row) | Smuggling/tell | checked |
| TC-230 Departure Manifest | Departure | DepartureDate | the desk calendar (today's rules: paper dates) | WrongDepartureDate | checked |
| TC-230 Departure Manifest | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |
| TC-310 Stranding Waiver | Signatory | Name | the record lookup (Citizen Records by ID, else name) | none | lookup key (never a tell: Forgery) |
| TC-310 Stranding Waiver | Citizen ID | CitizenId | Citizen Account / Displacement Registry; the other papers | none | deliberate decoy (checkable, never faulted) |
| TC-310 Stranding Waiver | Transponder | TransponderId | Citizen Account · Transponder; the other papers; today's recalls | FakeWaiver/transponder | checked |
| TC-310 Stranding Waiver | Debt Passed to Kin | Debt | Citizen Account · Debt | DebtorPosingAsTourist/debtorAsPoor | checked |
| TC-310 Stranding Waiver | Waiver No. | WaiverNo | Citizen Account · Waiver | FakeWaiver/number | checked |
| TC-310 Stranding Waiver | Signature | Signature | today's rules (paper set: a signed waiver) | IncompletePapers | checked |
| TC-310 Stranding Waiver | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |
| TC-415 Holiday Credit Agreement | Borrower | Name | the record lookup (Citizen Records by ID, else name) | none | lookup key (never a tell: Forgery) |
| TC-415 Holiday Credit Agreement | Citizen ID | CitizenId | Citizen Account / Displacement Registry; the other papers | none | deliberate decoy (checkable, never faulted) |
| TC-415 Holiday Credit Agreement | Destination | Destination | the spoken claim; Citizen Account · Forms on file (a contract's worksite); Registry · origin | none | deliberate decoy (checkable, never faulted) |
| TC-415 Holiday Credit Agreement | Account Class | AccountStatus | Citizen Account · Status | DebtorPosingAsTourist/debtorAsPoor | checked |
| TC-415 Holiday Credit Agreement | Credit Line | Credit | Citizen Account · Forms on file | ForgedProof/credit | checked |
| TC-415 Holiday Credit Agreement | Valid Until | Expiry | the desk calendar (today's rules: paper dates) | ExpiredPaper | checked |
| TC-415 Holiday Credit Agreement | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |
| TC-416 Proof of Funds | Account Holder | Name | the record lookup (Citizen Records by ID, else name) | none | lookup key (never a tell: Forgery) |
| TC-416 Proof of Funds | Citizen ID | CitizenId | Citizen Account / Displacement Registry; the other papers | none | deliberate decoy (checkable, never faulted) |
| TC-416 Proof of Funds | Account Class | AccountStatus | Citizen Account · Status | DebtorPosingAsTourist/debtorAsPoor | checked |
| TC-416 Proof of Funds | Funds Held | Funds | Citizen Account · Forms on file | ForgedProof/funds | checked |
| TC-416 Proof of Funds | Valid Until | Expiry | the desk calendar (today's rules: paper dates) | ExpiredPaper | checked |
| TC-416 Proof of Funds | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |
| TC-417 Travel Insurance Certificate | Insured | Name | the record lookup (Citizen Records by ID, else name) | none | lookup key (never a tell: Forgery) |
| TC-417 Travel Insurance Certificate | Citizen ID | CitizenId | Citizen Account / Displacement Registry; the other papers | none | deliberate decoy (checkable, never faulted) |
| TC-417 Travel Insurance Certificate | Destination | Destination | the spoken claim; Citizen Account · Forms on file (a contract's worksite); Registry · origin | none | deliberate decoy (checkable, never faulted) |
| TC-417 Travel Insurance Certificate | Policy No. | PolicyNo | Citizen Account · Forms on file | ForgedProof/policy | checked |
| TC-417 Travel Insurance Certificate | Valid Until | Expiry | the desk calendar (today's rules: paper dates) | ExpiredPaper | checked |
| TC-417 Travel Insurance Certificate | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |
| TC-520 Labour Contract | Worker | Name | the record lookup (Citizen Records by ID, else name) | none | lookup key (never a tell: Forgery) |
| TC-520 Labour Contract | Citizen ID | CitizenId | Citizen Account / Displacement Registry; the other papers | none | deliberate decoy (checkable, never faulted) |
| TC-520 Labour Contract | Employer | Employer | Citizen Account · Forms on file (the contract) | ForgedContract/employer | checked |
| TC-520 Labour Contract | Worksite | Destination | the spoken claim; Citizen Account · Forms on file (a contract's worksite); Registry · origin | ForgedContract/worksite | checked |
| TC-520 Labour Contract | Term | Term | Citizen Account · Forms on file (the contract) | ForgedContract/term | checked |
| TC-520 Labour Contract | Day Wage | Wage | Citizen Account · Forms on file (the contract) | ForgedContract/wage | checked |
| TC-520 Labour Contract | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |
| TC-520 Labour Contract | Photo | Photo | the traveller at the desk (Look > Their face) | SwappedPhoto/photo | checked |
| TC-610 Displacement Certificate | Full Name | Name | the record lookup (Citizen Records by ID, else name) | none | lookup key (never a tell: Forgery) |
| TC-610 Displacement Certificate | Displacement No. | CitizenId | Citizen Account / Displacement Registry; the other papers | none | deliberate decoy (checkable, never faulted) |
| TC-610 Displacement Certificate | Date of Birth | BirthDate | Citizen Account / Registry · Born | FakeDisplaced/tell, FalseOrigin/tell | checked |
| TC-610 Displacement Certificate | Origin | Destination | the spoken claim; Citizen Account · Forms on file (a contract's worksite); Registry · origin | none | deliberate decoy (checkable, never faulted) |
| TC-610 Displacement Certificate | Incident | Incident | Displacement Registry · Incident | none | deliberate decoy (checkable, never faulted) |
| TC-610 Displacement Certificate | Valid Until | Expiry | the desk calendar (today's rules: paper dates) | ExpiredPaper | checked |
| TC-610 Displacement Certificate | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |
| TC-610 Displacement Certificate | Photo | Photo | the traveller at the desk (Look > Their face) | SwappedPhoto/photo | checked |
| TC-620 Intake Declaration | Declarant | Name | the record lookup (Citizen Records by ID, else name) | none | lookup key (never a tell: Forgery) |
| TC-620 Intake Declaration | Displacement No. | CitizenId | Citizen Account / Displacement Registry; the other papers | none | deliberate decoy (checkable, never faulted) |
| TC-620 Intake Declaration | Coin of Home | Currency | Currency Ledger (the claimed place's row) | FakeDisplaced/tell, FalseOrigin/tell, Smuggling/tell | checked |
| TC-620 Intake Declaration | Native Tongue | Language | Tongues & Scripts (the claimed place's row) | FakeDisplaced/tell, FalseOrigin/tell | checked |
| TC-620 Intake Declaration | Effects Carried | Technology | Index of Devices (the claimed place's row) | FakeDisplaced/tell, FalseOrigin/tell, Smuggling/tell | checked |
| TC-620 Intake Declaration | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |
| TC-630 Return Order | Returnee | Name | the record lookup (Citizen Records by ID, else name) | none | lookup key (never a tell: Forgery) |
| TC-630 Return Order | Displacement No. | CitizenId | Citizen Account / Displacement Registry; the other papers | none | deliberate decoy (checkable, never faulted) |
| TC-630 Return Order | Return To | Destination | the spoken claim; Citizen Account · Forms on file (a contract's worksite); Registry · origin | none | deliberate decoy (checkable, never faulted) |
| TC-630 Return Order | Incident | Incident | Displacement Registry · Incident | none | deliberate decoy (checkable, never faulted) |
| TC-630 Return Order | Departure | DepartureDate | the desk calendar (today's rules: paper dates) | WrongDepartureDate | checked |
| TC-630 Return Order | Issuing Seal | Seal | Seal Register (the issuing office's seal) | ForgedSeal/ink, ForgedSeal/legend, ForgedSeal/shape | checked |

## D9: the published canon (agency.faults)

| id | Document | Field | Lie / directive | Variant | Proved against | What the forger did |
|---|---|---|---|---|---|---|
| richVisaClass | TC-101 | AccountStatus | PoorPosingAsRich | richForged | Citizen Account · Status | The visa's class reads above the account's status. |
| richManifestClass | TC-230 | TransponderClass | PoorPosingAsRich | richForged | Citizen Account · Transponder class | The manifest's class reads Premium on an Economy account. |
| borrowedVisaClass | TC-101 | AccountStatus | PoorPosingAsRich | richBorrowed | Citizen Account · Status | The visa's class reads above the account's status. |
| borrowedManifestId | TC-230 | CitizenId | PoorPosingAsRich | richBorrowed | Citizen Account · Citizen ID; the visa | A rich citizen's manifest: their Citizen ID. |
| borrowedManifestUnit | TC-230 | TransponderId | PoorPosingAsRich | richBorrowed | Citizen Account · Transponder | A rich citizen's manifest: their transponder. |
| borrowedManifestClass | TC-230 | TransponderClass | PoorPosingAsRich | richBorrowed | Citizen Account · Transponder class | A rich citizen's manifest: their Premium class. |
| doctoredVisaId | TC-101 | CitizenId | DoctoredIdentity | doctoredId | Citizen Account · Citizen ID; the manifest | The visa carries another Citizen ID. |
| doctoredVisaYear | TC-101 | BirthDate | DoctoredIdentity | doctoredYear | Citizen Account · Born | The visa's birth date has another year. |
| debtorRichVisaClass | TC-101 | AccountStatus | DebtorPosingAsTourist | debtorAsRich | Citizen Account · Status | A debtor's visa reads a tourist's class. |
| debtorRichManifestClass | TC-230 | TransponderClass | DebtorPosingAsTourist | debtorAsRich | Citizen Account · Transponder class | A debtor's manifest reads a tourist's class. |
| debtorPoorVisaClass | TC-101 | AccountStatus | DebtorPosingAsTourist | debtorAsPoor | Citizen Account · Status | A debtor's visa reads Standard. |
| debtorWaiverDebt | TC-310 | Debt | DebtorPosingAsTourist | debtorAsPoor | Citizen Account · Debt | The waiver passes a sliver of the real debt to kin. |
| debtorCreditClass | TC-415 | AccountStatus | DebtorPosingAsTourist | debtorAsPoor (optional) | Citizen Account · Status | The credit agreement reads the posed class. |
| debtorFundsClass | TC-416 | AccountStatus | DebtorPosingAsTourist | debtorAsPoor (optional) | Citizen Account · Status | The proof of funds reads the posed class. |
| contractWage | TC-520 | Wage | ForgedContract | wage | Citizen Account · Forms on file (the contract) | The day wage is 1.5 to 3 times the registered one. |
| contractTerm | TC-520 | Term | ForgedContract | term | Citizen Account · Forms on file (the contract) | The term is a quarter to six tenths of the registered one. |
| contractEmployer | TC-520 | Employer | ForgedContract | employer | Citizen Account · Forms on file (the contract) | Another employer of the era. |
| contractWorksite | TC-520 | Destination | ForgedContract | worksite | Citizen Account · Forms on file (the contract) | Another place open today. |
| waiverNumber | TC-310 | WaiverNo | FakeWaiver | number | Citizen Account · Waiver | A waiver number the account never registered. |
| waiverUnit | TC-310 | TransponderId | FakeWaiver | transponder | Citizen Account · Transponder; the manifest | Made out for another unit of the account's class. |
| proofCredit | TC-415 | Credit | ForgedProof | credit | Citizen Account · Forms on file | A credit line 3 to 10 times the one on file. |
| proofFunds | TC-416 | Funds | ForgedProof | funds | Citizen Account · Forms on file | Savings 3 to 10 times those on file. |
| proofPolicy | TC-417 | PolicyNo | ForgedProof | policy | Citizen Account · Forms on file | A policy number the account does not hold. |
| sealShape | any paper | Seal | ForgedSeal | shape | the Seal Register | One paper's seal has another outline than its office's. |
| sealInk | any paper | Seal | ForgedSeal | ink | the Seal Register | One paper's seal is in another ink than its office's. |
| sealLegend | any paper | Seal | ForgedSeal | legend | the Seal Register | One paper's seal carries a wrong legend. |
| photoVisa | TC-101 | Photo | SwappedPhoto | photo (optional) | the traveller at the desk | The visa's photo shows someone else. |
| photoContract | TC-520 | Photo | SwappedPhoto | photo (optional) | the traveller at the desk | The contract's photo shows someone else. |
| photoCertificate | TC-610 | Photo | SwappedPhoto | photo (optional) | the traveller at the desk | The certificate's photo shows someone else. |
| originBirth | TC-610 | BirthDate | FalseOrigin | tell | Displacement Registry · Born | A birth year of another of today's places. |
| originCoin | TC-620 | Currency | FalseOrigin | tell | Currency Ledger | The coin of another of today's places. |
| originTongue | TC-620 | Language | FalseOrigin | tell | Tongues & Scripts | The tongue of another of today's places. |
| originEffects | TC-620 | Technology | FalseOrigin | tell | Index of Devices | The devices of another of today's places. |
| fakeDisplacedBirth | TC-610 | BirthDate | FakeDisplaced | tell | Displacement Registry · Born | A birth year of 2150. |
| fakeDisplacedCoin | TC-620 | Currency | FakeDisplaced | tell | Currency Ledger | The coin of 2150. |
| fakeDisplacedTongue | TC-620 | Language | FakeDisplaced | tell | Tongues & Scripts | The tongue of 2150. |
| fakeDisplacedEffects | TC-620 | Technology | FakeDisplaced | tell | Index of Devices | The devices of 2150. |
| smuggleCurrency | TC-230 | Currency | Smuggling | tell | Currency Ledger | 2150 currency carried out. |
| smuggleDevices | TC-230 | Technology | Smuggling | tell | Index of Devices | 2150 devices carried out. |
| smuggleCoin | TC-620 | Currency | Smuggling | tell | Currency Ledger | 2150 currency carried out. |
| smuggleEffects | TC-620 | Technology | Smuggling | tell | Index of Devices | 2150 devices carried out. |
| expiredVisa | TC-101 | Expiry | ExpiredPaper | expired | the desk calendar | Valid Until has passed. |
| expiredCredit | TC-415 | Expiry | ExpiredPaper | expired | the desk calendar | Valid Until has passed. |
| expiredFunds | TC-416 | Expiry | ExpiredPaper | expired | the desk calendar | Valid Until has passed. |
| expiredPolicy | TC-417 | Expiry | ExpiredPaper | expired | the desk calendar | Valid Until has passed. |
| expiredCertificate | TC-610 | Expiry | ExpiredPaper | expired | the desk calendar | Valid Until has passed. |
| wrongDateManifest | TC-230 | DepartureDate | WrongDepartureDate | date | the desk calendar | The departure is dated another day. |
| wrongDateReturn | TC-630 | DepartureDate | WrongDepartureDate | date | the desk calendar | The departure is dated another day. |
| waiverUnsigned | TC-310 | Signature | IncompletePapers | unsigned | Today's rules · paper set | The waiver is not signed. |
| waiverMissing | TC-310 | (the paper) | IncompletePapers | missing | Today's rules · paper set | The waiver is not carried. |
| proofMissingCredit | TC-415 | (the paper) | IncompletePapers | missing | Today's rules · paper set | The proof of means is not carried. |
| proofMissingFunds | TC-416 | (the paper) | IncompletePapers | missing | Today's rules · paper set | The proof of means is not carried. |
| proofMissingPolicy | TC-417 | (the paper) | IncompletePapers | missing | Today's rules · paper set | The proof of means is not carried. |
| economyManifest | TC-230 | TransponderClass | IncompletePapers | economy | Today's rules · paper set | A Premium citizen's manifest books an Economy unit. |
| recalledUnit | TC-230 | TransponderId | RecalledTransponder | recall | Today's rules · recalls | The manifest books a recalled model. |

## D4: the offices and their seals (agency.offices)

| Office | Seal | Wrong legend | Forms |
|---|---|---|---|
| Visa Office | Blue hexagon · VO | VD | TC-101 |
| Portal Hall Dispatch | Green circle · PH | PR | TC-230 |
| Debt Relief Directorate | Red shield · DR | DK | TC-310 |
| Leisure Credit Bureau | Violet diamond · LC | LE | TC-415, TC-416 |
| Transit Insurance Desk | Brown octagon · TI | TJ | TC-417 |
| Labour Placement Bureau | Black square · LB | LP | TC-520 |
| Displacement Registry | Violet circle · DP | DB | TC-610, TC-620 |
| Returns Office | Green shield · RO | RD | TC-630 |
