# Day One — Implementation Plan

Date: 2026-08-22 · Design: `../specs/2026-08-22-day-one-design.md` (read it
first — this plan implements that document, it does not restate it)

**Goal:** Ship the designed Day 1: real-world nation set live, six-visitor
scripted-spine day plan, three anchor cases, three generated-slot pools,
briefing/directives/ledger copy, evening economy numbers, and the
`endingsMinDay` cushioning rule.

**Ground rules** (from the Engineering Manifesto — non-negotiable here):
every number lands in a ScriptableObject, never a constant; rule changes
ship with decision-table tests; `docs/FEATURES.md` updates in the same
change; `Tools → TimeDesk` builders stay authoritative; content gaps fail
loudly in the log.

**Expected footprint:** one small code change (`endingsMinDay`), everything
else is data authoring + copy + tests. No new systems, no new windows.

---

## Task 0: Vocabulary inventory (read-only, do first)

**Files:** none changed; produces a gap list in the task notes / PR description.

- [x] Inventory `ContentLibrary_Main`: exact ids of eras, the five real-world
      nations and their `NationEraProfileSO` pairings, existing attributes,
      archetypes, citizen records, document templates, clue categories.
- [x] Resolve every ⚠ in the design's numbers tables against
      `GameConfig_Default` / `HomeEconomy` constants: per-verdict base pay,
      citation penalty ladder, stability losses, expense constants
      (lodging/heat/per-family), debt-vs-clamp behavior on negative credits.
- [x] Confirm whether an attribute suitable for the Guardian's mercy impact
      (+8) already exists (warmth/kinship/humanity-adjacent) or whether the
      closest existing attribute gets the impact — prefer reuse; author a new
      `AttributeSO` only if nothing fits (note why in the PR).
- [x] Confirm era ids for: V1 Greece claim, V3 Germany claim, V6 Japan claim,
      and the V5 forbidden pair (design proposes Japan + V5's era; verify the
      pairing exists so the travel rule is expressible as `TravelRuleSO`).
- [x] Gap list → adjust design doc anchors in place (era names only) if
      reality differs; anything bigger, back to the director.

**Done when:** every id referenced by Tasks 3–7 exists on paper, and the
numbers tables have real knob names.

### Task 0 findings (2026-08-22, executed)

**Vocabulary that exists:** eras `Greece`(display "Ancient Greece"),
`NGerrmany` (typo, display "Nazi Germany"), `Japan` ("Imperial Japan"),
`Egypt` ("Ancient Egypt"), `China` ("Imperial China") + 3 fictional
(rome/medieval/future). Nations greece/germany/japan/egypt/china (namePools
all EMPTY). Profiles exist for all 5 real nation-era pairs (Art/Science/
Democracy baselines). Attributes: art/democracy/science +
industry/militarism/mysticism/philosophy. Archetypes: Artist/Diplomat/
Merchant/Scientist/Soldier/Wanderer + 4 investigation ones. Reference books
(Currency/Language/Technology) cover ONLY the fictional nations. Passport
template has fields Name/BirthDate/Coin-of-Issue/Native-Tongue; Permit has
Declared-Device/Bond-Currency. `GameConfig_Default`: basePay 10,
freeWarnings 1, citations [5,10,20], requireEvidenceToDeny=true,
stabilityLossPerWrong 5, baseDailyExpense 10, expensePerFamilyMember 5,
slotSpinCost 10. RunConfig: startingMoney 50, no startingFamilyMembers.

**Gaps → resolutions (applied during implementation):**

1. **Blueprint pinning (small code addition — flagged to director).**
   Forced cases pin only the blueprint; nation/era/name/birthdate come from
   day-global eraWeights + random pools, and `PopulateDocumentFields` picks
   the forged field randomly. The scripted spine is impossible without
   optional pin fields on `CaseBlueprintSO` (nation/era/givenName/birthDate/
   forced forgery category+value). Implemented as data-driven optional
   fields honored only when set — no new systems or windows.
2. **V5 forbidden pair → Egypt + Ancient Egypt** (design proposed Japan +
   V5's era; impossible: Imperial Japan is the only Japan era and V6 must
   keep it legal). Era_Rome/Medieval/Future travel rules exist as patterns.
3. **Era display names adapted to design intent:** Era_Greece → "Classical
   Greece"; Era_NGermany → "Imperial Germany" (design wanted
   imperial/Prussian; id typo `NGerrmany` fixed to `NGermany`). Era ids
   referenced by saves/counters are otherwise untouched.
4. **Attr_Kinship authored** (id `kinship`) — no warmth-adjacent attribute
   exists among the 7; nothing to reuse. +8 mercy impact wins
   `ScoreRanking` (raw deltas, not baselines, feed ranked layers).
   Archetype_Guardian authored with empty defaultImpacts; the +8 lives as
   the blueprint's authoredImpact {deltaOnWrong: +8} — impacts only apply
   on ACCEPT (GameManager.HandleDecision), so correct deny lands nothing,
   matching the design table exactly.
5. **Reference books extended** to cover the 5 real nations (Currency +
   Language + Technology entries) — without them no real-nation claim can
   be verified or forged (CaseFactory refuses unprovable forgeries).
6. **Real-nation namePools authored** (empty today → "Subject #N" names).
7. **Numbers reconciled (shipped values):** pay stays 10/correct (design
   table updated to match), freeWarningsPerDay 2, citations [10,15,20],
   stabilityLossPerWrong stays 5 (design's −4/−7 split collapses to one
   knob), baseDailyExpense 20 + expensePerFamilyMember 5 + 2 starting
   family members = 30/day (RunConfig gains the 2 names),
   startingMoney 0 (day's pay IS the wallet — otherwise Lexicon/Material
   choice is meaningless), slotSpinCost 3. Negative money carries as debt
   (bankruptcy at ≤ −100, gated Day 2+ by endingsMinDay).
8. **New authored assets:** Upgrade_Lexicon (12cr) + Upgrade_Material
   (30cr wanting-object), slot outcomes re-weighted 55/20/20/5 with
   design-scaled deltas, TravelRule Rule_NoEgyptDay1, TIMES/directives copy
   in DayPlan briefing fields (per DayFlowUIController wiring).

## Task 1: Fix the duplicate day-number shadowing (blocker)

**Files:** `DayPlan_1.asset`, `DayPlan_2.asset`, `DayPlan_3.asset` (fictional
set), `ContentLibrary_Main.asset`.

- [x] Renumber the fictional `DayPlan_1/2/3` to day numbers 4/5/6 (rename
      assets to match convention, e.g. `DayPlan_4/5/6`). Content is preserved
      for later days; nothing is deleted.
- [x] Run `ContentLibraryValidator` (it already has
      `CheckDuplicateDayNumbers`) — must pass clean.
- [x] Verify in play mode (or a `DayRunner` test) that day 1 resolves to
      `DayPlan_Inv_Day1` via `ContentLibrarySO.GetDayPlan`.
- [x] Update `docs/FEATURES.md` if the day-plan inventory line changes
      (it shouldn't — behavior contract is unchanged; the bug fix is that the
      contract now actually holds).

**Done when:** the real-world set is reachable in play; validator green.

## Task 2: `endingsMinDay` cushioning rule (the one code change)

**Files:** `Assets/Scripts/Core/GameConfigSO.cs` (new field),
`Assets/Scripts/Endings/EndingService.cs` (gate check), new
`Assets/Tests/EditMode/EndingServiceTests.cs`.

- [x] `GameConfigSO`: add `public int endingsMinDay = 2;`
- [x] `EndingService`: skip all ending evaluation while
      `world.Day < config.endingsMinDay`. Single gate at the entry point —
      not per-ending sprinkles. Both existing check sites (after verdict,
      before sleep) flow through it.
- [x] Decision-table test: day 1 + bankruptcy-level debt → no ending; day 1 +
      zero stability → no ending; day 2 + same conditions → endings fire;
      `endingsMinDay = 1` restores old behavior (config actually controls it).
- [x] Note: if `EndingService` still lives in `Assembly-CSharp` (known debt),
      place the test where the existing suites can reach it and note the
      asmdef situation in the PR — do not silently skip the test.
- [x] `docs/FEATURES.md`: endings line gains "gated by `endingsMinDay`
      (default 2 — Day 1 cannot end the run)".

**Done when:** tests green, knob visible in Inspector, FEATURES.md updated.

## Task 3: `DayPlan_Inv_Day1` — the six-slot shape

**Files:** `DayPlan_Inv_Day1.asset` (and new referenced assets from Tasks 4–5).

- [x] Visitor count = 6.
- [x] Forced cases: slot 1 = V1 anchor, slot 3 = V3 anchor, slot 6 = V6
      anchor (via the plan's forced-case mechanism: "the Nth case is X").
- [x] Per-slot generation configs for slots 2/4/5 → Pools A/B/C (Task 5).
- [x] Legendary chance = 0 for day 1 (day range for the teased legendary
      starts day 3 — matches the design's pay-off timing).
- [x] Day seed determinism untouched (existing system).
- [x] Travel rule of the day: one `TravelRuleSO` (forbidden nation+era from
      Task 0); confirm it surfaces in briefing + directives.

**Done when:** a `DayRunner` headless pass resolves 6 cases in slot order
with the right anchors at 1/3/6.

## Task 4: Anchor content (V1 honest, V3 forger, V6 the sibling)

**Files:** new `CaseBlueprintSO`s (3), clue/`DocumentTemplateSO` wiring,
`CitizenRegistry` entries (Dimitra Kanellos; "Klaus Reinhardt" only if his
claim needs a record — otherwise skip; **Emi Asakura — hard requirement**),
Guardian `ArchetypeSO` (or reuse nearest existing), impacts per design table.

- [x] **V1:** single-document honest blueprint; all fields match reference
      book; citizen record exact; Artisan archetype (+2 warm attribute on
      accept); zero clue injections; amphora manifest flavor page.
- [x] **V3:** one provable forged currency field (mismatch-proof shape,
      `isAnachronism`); Germany reference book must contain the truth
      (provable-only rule); archetype with −3 cold impact on accept.
- [x] **V6:** genuine-passport blueprint; forged DOB (+9 years) targeted at
      Emi Asakura's existing citizen record (record-proof shape);
      guardianship-form second page; Guardian archetype with **+8** mercy
      impact on accept, nothing on deny; intercom line only if the scripted-
      line mechanism exists by then — otherwise paper-only delivery.
- [x] Validation: extend/run a content check that V6's target name resolves
      to a citizen record (fail loudly in log if not — the known silent-
      degradation risk from the design doc).
- [x] Impact sanity: after a mercy-only Day 1 (all correct + accept V6), the
      Guardian attribute must top every category `ScoreRanking.TGetTop` reads
      for the mid-decor layer; verify via `Timeline Inspector` (dominant
      marking) in a play session.

**Done when:** each anchor plays its designed proof style (match-everything /
mismatch-proof / record-proof) and the mercy wall-trace is observable on Day
2 in a manual run.

## Task 5: Generated-slot pools (A/B/C)

**Files:** three `CaseBlueprintSO`s (or blueprint variants) + weight knobs on
`DayPlan_Inv_Day1`; no new code.

- [x] Pool A (slot 2): 100% honest, 1 document, 0–1 harmless odd field
      (70/30), 3 candidate eras even weights, honest archetypes.
- [x] Pool B (slot 4): 70% odd-but-honest (mismatch-display, zero-register
      — wrong-era page compares), 30% single-forgery; 4 candidate eras.
- [x] Pool C (slot 5): 100% honest, destination forced onto the day's
      forbidden pair, 1–2 documents.
- [x] Play a seeded sample of each pool ×5 (dev force-seed or repeated runs)
      and confirm: A never forges; B's honest arm never logs a deviation;
      C always violates the directive but is otherwise clean.

**Done when:** sample matrix behaves; weights are Inspector-tunable values.

## Task 6: Copy — briefing, directives, ledger, poster

**Files:** briefing/ledger panel content assets (as wired by
`DayFlowUIController` today), directives sticky content, poster variant.

- [x] THE TEMPORAL TIMES Day 1: protocol-reminder headline (teaches by being
      a document), forbidden-pair directive stated in the headline (the V5
      crutch), "CHRONONAUT" dismissive sidebar (legendary tease).
- [x] Directives window: the one travel rule, bureau voice per tone guide.
- [x] Shift ledger: existing format + undocumented-denials line; verify copy
      for the mercy path reads as a wrong verdict (it must NOT moralize).
- [x] Poster wanted-notice placeholder variant via existing cue system.
- [ ] Director pass: tone-guide review of all copy, V3/V6 highest attention
      (flagged in design risks).

**Done when:** copy is in-asset (not code strings) and director-approved.

## Task 7: Evening economy numbers

**Files:** `GameConfig_Default` expense constants ⚠ (per Task 0 findings),
Day-1 shop stock (`UpgradeSO` availability), slot outcome weights ⚠.

- [x] Set/verify: expenses total 30 on the designed family size; Lexicon at
      12, Material at 30 in Day-1 stock; first-spin cost 3; Day-1 outcome
      weights 55/20/20/5.
- [x] Worked-check the three playthrough columns from the design's economy
      table against actual `HomeEconomy` math; fix discrepancies in the
      design doc (numbers, not shape) or the assets — whichever is wrong,
      with the doc updated to match what shipped.
- [x] Confirm negative-credits behavior (debt vs clamp) matches what the
      design doc now claims after Task 0.

**Done when:** the three columns of the economy table reproduce in play.

## Task 8: Close-out

- [ ] Full Day 1 → sleep → Day 2 manual pass: timing targets vs the design
      table, wall trace present after mercy / absent after clean deny,
      evidence-gate behaviors at V3/V6, ledger accuracy.
- [x] `DayRunner` headless full-day test added to the EditMode suite (slot
      order, anchor resolution, verdict scoring on scripted answers).
- [x] `docs/FEATURES.md` updated for: day-plan fix, `endingsMinDay`, day-1
      content set, mercy wall-trace sizing (it's a content fact, not a
      mechanic — one line under Ranked Office Layers).
- [x] Workspace memory updated with links to both docs.

**Done when:** the manifesto's definition of done holds — knobs in SOs,
tests cover the decision table, FEATURES.md is current, the builder can
rebuild the affected scenes, and the director has played the day.
