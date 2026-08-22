# Day One — "The Wall Remembers" — Design

Date: 2026-08-22 · Status: approved by Marwan (director) · Companion plan: `../plans/2026-08-22-day-one-implementation.md`

## Goal

Design the first full day of NOPE — briefing to sleep — as the game's first
impression: a cold-open, six-visitor shift plus a full evening, built on a
**scripted spine** (three authored anchor visitors) wrapped in **generated
fill** (three procedural slots), all inside the existing `DayPlanSO` /
`CaseFactory` / `DayOrchestrator` systems. No new mechanics, no tutorials, no
boss character. The institution itself is the voice. By bedtime the player
should feel: *I was watched, I judged six people, one of them got under my
skin — and the office wall knows what I did.*

**Director's locked decisions** (from the 2026-08-22 design session):

| Decision | Choice |
|---|---|
| Day | Day 1 (first impression) |
| Structure | Scripted spine: 3 anchors + 3 generated slots |
| Tone | Tense bureaucracy — faceless institution, Papers, Please dread |
| Teaching | Cold open; the first visitor is the tutorial |
| Length | 6 visitors, ~15–20 min shift |
| Nations | Real-world set (Greece, Germany, Japan, Egypt, China) |
| Legendary | None — teased in the briefing only |
| Stakes | Real but cushioned: citations + stability loss hurt; no ending fires on Day 1 |
| Closer | "The Sibling" — a genuine passport with one altered field |
| Mercy | Accepting the sibling leaves a visible trace on the office wall (Day 2) |
| Scope | Full day including the evening (Home phase) |

## Tone guide (applies to every line of copy)

1. **The bureau speaks; nobody else does.** No supervisor, no mentor, no
   dialogue popups from authority. THE TEMPORAL TIMES, directives, citation
   slips, and stamped notices are the only institutional voice — passive,
   clipped, indifferent. Never "I"; always "this office," "the undersigned,"
   "per directive."
2. **Real nations, real respect.** Visitors from Greece, Germany, Japan,
   Egypt, and China are written as individuals, never caricatures. Forgery is
   a personal circumstance, not a national trait — no nation's visitors lie
   more than any other's. Cultures appear through craft, goods, and
   documents, not accents or jokes.
3. **The system is the antagonist.** Every visitor is sympathetic or
   self-interested *within* the rules; the pressure comes from the rules
   themselves. Copy never mocks the traveler.
4. **Era texture over era trivia.** A detail is good if a 1978 clerk could
   squint and believe it (an amphora shipping manifest, a Meiji-era travel
   seal), not if it reads like a history quiz.

## The day at a glance

```text
intensity
   4 |                    /\            /\
   3 |          /\      /    \        /  \___ ledger
   2 |    /\   /  \    /      \      /       \___ evening (denouement)
   1 |   /  \_/    \  /        \    /
   0 |  V1   V2   V3          V4  V5    V6        sleep → wall has changed
   +--SAFE--PRACTICE--CHALLENGE--TWIST--EXAM---> shift
      anchor  gen#1   anchor      gen#2  gen#3  anchor
```

| Slot | Visitor | Type | Curve beat | Job |
|---|---|---|---|---|
| V1 | Dimitra Kanellos | **Scripted anchor** | SAFE | Honest, unfailable. Teaches request → scan → compare → accept. |
| V2 | generated | Generated (pool A) | PRACTICE | Easy honest case; safe compare play. |
| V3 | "Klaus Reinhardt" | **Scripted anchor** | CHALLENGE | First forger. One provable lie. First deny + evidence gate. |
| V4 | generated | Generated (pool B) | TWIST | Red herrings — scanner stays empty, eyes say otherwise. |
| V5 | generated | Generated (pool C) | TWIST→ | Honest papers, forbidden destination. Directive deny, no evidence. |
| V6 | Yuki Asakura | **Scripted anchor** | EXAM | The Sibling. Deny is correct; accept is mercy the wall remembers. |

Cold open: no tooltips, no highlighted arrows. The briefing and the
directives window are the entire tutorial. THE TEMPORAL TIMES headline for
Day 1 does one line of teaching by being a document:

> **FIRST SHIFT PROTOCOL REMINDER —** Present documents upon request.
> Deviations must be logged before denial. This office thanks you for your
> compliance.

---

## Part 1 — The scripted spine

### V1 — The Honest One (opening anchor)

**Role in the design:** the safe case. The player literally cannot fail this
verdict (any reasonable read of the documents yields ACCEPT). Its job is to
teach the desk verb-chain by *being clean*: request the passport, see the
claim match, check the name against Citizen Records, accept.

- **Name:** Dimitra Kanellos, 41, ceramicist.
- **Claim:** returning home — Greece, Classical era (verify exact era id
  against `ContentLibrary_Main`; see implementation plan Task 0).
- **Documents:** passport only (one document, two pages max). Identity
  fields match `CitizenRegistry` exactly. Every field matches the
  reference-book entry for Greece + claimed era. Zero clue injections.
- **Archetype:** Artisan (honest default impacts — a modest Culture-leaning
  timeline nudge on accept, so the wall has *something* honest to show if
  the player denies the sibling).
- **Flavor:** a shipping manifest page listing amphorae — the era texture
  that makes the case feel read rather than quizzed.
- **Teaching without text:** she is the first READY tap, the first passport
  request, the first MATCH bar, the first accept stamp. Because everything
  matches, the correct action is discoverable by trying things.

### V3 — The First Liar (midpoint anchor)

**Role in the design:** the first deny, and the first bite of the evidence
gate. Exactly **one** provable forgery, nothing else — a clean, confident
lie. Players who deny on gut feeling alone (no scan) eat their first
unproven-denial citation *even though the visitor was lying* — the day's
hardest lesson, delivered by the system, not by a tutorial.

- **Name:** "Klaus Reinhardt," 38, self-described importer.
- **Claim:** Germany, (imperial/Prussian era — verify era id). Confident,
  papers eager, everything plausibly in order except one field.
- **The lie:** the passport's **currency line** carries a value belonging to
  a different nation's era — a **mismatch proof** (forged field ≠ claimed
  era's reference entry, `isAnachronism` flagged). The reference book for
  Germany + claimed era contains the truth, so the forgery is provable by
  design (`CaseFactory` only forges provable fields).
- **Intercom behavior:** replies slightly too quickly; if further scripted
  intercom lines exist by then, one beat of bland overconfidence. Never
  cartoonish.
- **Scoring paths:**
  - Scan the currency → Deviation Report logs it → DENY = correct, full pay.
  - DENY without scanning = unproven denial: free warning #1 (see numbers)
    + stability loss, distinct slip: *"Denial without documented deviation."*
  - ACCEPT = wrong verdict: warning/citation ladder + his archetype impacts
    land on the timeline (a cold little smudge on tomorrow's wall).
- **Why a mismatch proof and not a record proof:** V1 just taught Citizen
  Records; V3 must teach the *reference book* compare. V6 then returns to
  Citizen Records for the record proof. Each anchor teaches one proof style.

### V6 — The Sibling (closing anchor — the centerpiece)

**Role in the design:** the aftertaste. A mechanically airtight case whose
correct answer (DENY) is emotionally expensive. Everything the player learned
today is needed to *see* the lie; nothing they learned makes *stamping* it
easy.

- **The person at the window:** Yuki Asakura, 19, presenting the passport of
  her younger sister, **Emi Asakura, age 10**.
- **The documents:** Emi's passport is **genuine** — the name exists in
  `CitizenRegistry` with a real record. Exactly one field is altered: the
  **date of birth**, moved +9 years so the passport reads as the presenter's
  age. The claim: Japan, (era to verify — e.g., Shōwa), destination their
  aunt's prefecture.
- **The proof:** **record proof** — compare the passport DOB against the
  Citizen Records entry for the name on the passport; the true birth date
  disagrees. The Deviation Report logs it. (Fully supported by existing
  `DiscrepancyLog` record-proof rules; requires Emi's citizen record to
  exist — authoring requirement, not a code change.)
- **The story the documents tell** (no cutscene — environmental delivery):
  a second document page, a guardianship transfer form half-filled-out in a
  child's handwriting, the guardian's line blank. The player assembles the
  why from paper. If intercom lines are available: one exchange —
  *"Her name is on the passport. That is her name."* — and nothing more.
- **Verdicts:**
  - **DENY (correct):** full pay. The ledger is clean. The slip the player
    never sees reads: *accepted documents returned to bearer; bearer
    declined.* The day ends on mastery and a sour taste — by design.
  - **ACCEPT (mercy):** wrong verdict → warning/citation ladder + stability
    loss (see numbers), **and** the accept applies the Guardian archetype's
    timeline impact — sized to be the single largest attribute score of Day 1
    (3× a normal honest accept), so on Day 2 the **mid-decor ranked layer
    changes to that attribute no matter what else the player accepted**.
    The mercy is *on the wall*. Nothing announces it. The player who denied
    walks into an unchanged, neutral wall — the absence is the other half of
    the beat.
- **Why the DOB forgery works mechanically:** names are never forged until
  the missing-record mechanic lands, so the sibling must lie on a field that
  CAN be forged and proven — DOB against an existing citizen record is
  exactly that, and it's emotionally the right field: she aged her sister
  into herself.

---

## Part 2 — The generated slots (rulebooks, not scripts)

All three slots run through `CaseFactory` with day-plan knobs. Weights are
design targets; they live in the `DayPlanSO`, tunable without code.

### V2 — Pool A: "A quiet morning" (PRACTICE)

| Knob | Value | Reasoning |
|---|---|---|
| Honest / forged | 100% / 0% | The player has solved exactly one case. Zero threat. |
| Documents | 1 | Passport only — same shape as V1. |
| Red-herring clue injections | 0–1 (weight 70/30) | A harmless odd-looking field with no anachronism flag; safe compare practice that never registers. |
| Candidate eras | 3, even weights | Variety without reading load. |
| Archetypes | Artisan / Scholar / Farmer | Neutral, warm-but-plain honest impacts. |

### V4 — Pool B: "Eyes vs. machine" (TWIST)

| Knob | Value | Reasoning |
|---|---|---|
| Honest-with-odd-papers / simple forgery | 70% / 30% | The twist slot trains *restraint*: trust the Deviation Report, not your gut. |
| "Odd but honest" build | one field that MISMATCHes against the *wrong-era reference page* (or reads strangely) but registers nothing — honest fields never log | Directly exercises the junk-comparison rule the audit hardened. |
| Simple forgery build | one provable anachronism, same as V3's shape | Keeps attention honest — the pool that cried wolf must sometimes have a wolf. |
| Candidate eras | 4 | Slightly wider; by now the player can read a reference book. |

### V5 — Pool C: "The directive" (TWIST→, moral pre-echo)

| Knob | Value | Reasoning |
|---|---|---|
| Honest / forged | 100% / 0% | The pressure is the rule, not the lie. |
| Directive violation | 100% — destination hits the day's forbidden nation+era pair | Deny is correct **without scanned evidence** (directive denials never need it) — teaches the one deny-path that bypasses the gate, and denies an *honest* person lawfully: the mechanical pre-echo of V6's moral, with none of the cost. |
| Documents | 1–2 | Clean papers; the directives sticky-note is the whole case. |
| Mitigation for the directives-window UX gap | the forbidden pair is also stated in the Day 1 briefing headline (below) | The known "directives easily missed" debt is load-bearing in this slot; the briefing carries it until the UX fix lands. |

**Slot order is fixed** (A → B → C) via forced per-slot generation configs in
the day plan — the difficulty curve is the product; generation fills the
shape, it does not decide it.

---

## Part 3 — Legendary tease (no encounter)

- THE TEMPORAL TIMES sidebar, day 1, bottom-left, two column-inches:

> **"CHRONONAUT" REPORTS DISMISSED**
> Rumors of a traveler bearing papers from no recorded era were dismissed
> by this office as clerical error. There is no such file. There is no
> such traveler.

- The office poster sprite gains a small wanted-notice placeholder variant
  (existing timeline-cue poster system; no new mechanism).
- First legendary encounter is scheduled **Day 3+** (existing `LegendarySO`
  day-range) — the tease pays off after the loop is learned.

---

## Part 4 — The evening (Home phase, designed start to end)

The evening's design job: teach that the day's money is already spent, hand
the player exactly one meaningful purchase, and make the slot machine a
cheap, optional itch — then let sleep show the consequence wall.

1. **Expenses (the gut punch, cushioned).** Itemized `ExpenseReport`:
   lodging, heat, two family members. Tuned so a *typical* Day 1 (5–6
   correct) leaves 12–18 credits; a *worst* Day 1 still covers bills (family
   drift worsens, no bankruptcy possible — endings gate is Day 2+, and the
   numbers below keep worst-case income ≥ expenses by construction).
2. **Shop (one real choice).** Day 1 stock: exactly one affordable upgrade —
   **Lexicon** (evidence-adjacent, makes Day 2 gentler) at 12 credits —
   beside one visible-but-unaffordable upgrade (Material, 30) as the
   wanting-object. Buy Lexicon = no savings; skip = Day 2 with a cushion.
3. **Slot machine (the greed tease).** First spin costs 3. Day-1-weighted
   outcomes: mostly neutral (55%), small loss (20%), small win (20%), rare
   tomorrow-modifier (5%: pay-rate ×1.25). The machine is *allowed* to feel
   tempting and slightly shameful; it is never required.
4. **Sleep → nightly resolve.** `TimelineService.NightlyResolve` runs;
   tomorrow package is built. Fade to black.
5. **Day 2 cold open — the wall.** The player's first sight of Day 2 is the
   booth, not the briefing: if they showed mercy, the mid-decor has changed;
   if not, it hasn't. One beat of silence before THE TEMPORAL TIMES appears.
   (No new system — `TimelineRankedSprite` already renders this; the design
   only sizes the impact so mercy wins the Day-1 board.)

---

## Part 5 — Numbers

> **Reconciled 2026-08-22 (implementation Task 0/7) — shipped values.**
> Base pay is **10**/correct (max day 60, typical 40–60), free warnings
> **2**, citation ladder **[10, 15, 20]**, stability loss **−5** flat per
> wrong verdict (the −4/−7 split collapsed to the single existing knob),
> expenses **20 base + 5 × 2 family = 30**, starting money **0** (the day's
> pay is the wallet), spin cost **3**. Typical day leaves ~20 (Lexicon yes,
> Material no); mercy day leaves ~10 (Lexicon no — mercy still costs it);
> worst case carries as debt (bankruptcy −100, gated Day 2+). Era anchors:
> V1 Classical Greece (Era_Greece), V3 Imperial Germany (Era_NGermany,
> display adapted from "Nazi Germany"), V6 Imperial Japan (Era_Japan).

> All values are **proposals mapped to existing config knobs where their
> names are known** (`freeWarningsPerDay`, `citationPenalties`,
> `requireEvidenceToDeny`, `rankedLayerMinScore`). Field names marked ⚠ must
> be reconciled against `GameConfig_Default` during implementation Task 0 —
> values, not names, are the design decision.

### Shift pay & penalties (per verdict)

| Outcome | Pay | Penalty | Stability | Reasoning |
|---|---|---|---|---|
| Correct | +8 ⚠ base | — | — | 6 × 8 = 48 max day pay; typical 32–48. |
| Correct deny of V3/V6 with evidence | +8 | — | — | Proof work is the skill; same pay, mastery is the reward. |
| Wrong (free warnings 1–2) | 0 | none | −4 | Cushioned Day 1: two soft landings. |
| Wrong (3rd+) | 0 | −10 ⚠ citation | −7 | Real teeth, arrives only after two lessons. |
| Unproven denial (V3 gut-deny path) | 0 | warning ladder | −4 | The lesson costs a warning, not a fortune. |
| Mercy ACCEPT (V6) | 0 | ladder position | −4 | System-consistent: mercy is *a* wrong verdict; the wall is what makes it different. |

`freeWarningsPerDay = 2`. Worst-case Day 1: 6 wrong verdicts = 2 free + 4
citations (−40) + stability −38 ⚠ → **no ending can fire** because
`endingsMinDay = 2` (new knob, see implementation plan) — cushioning is a
rule, not a tuning accident.

### Mercy impact sizing (the wall trace)

| Impact | Value | Reasoning |
|---|---|---|
| Honest accept attribute impact (V1/V2 archetypes) | +2 each | Day-1 board leader among honest accepts ≈ +4–6. |
| V3 accept (the liar gets through) | −3, cold attribute | A smudge, not a scar. |
| **Guardian accept (mercy)** | **+8** on the Guardian's attribute | 3–4× any honest-accept total ⇒ guaranteed top attribute score for Day 1 ⇒ mid-decor layer **must** change on Day 2. Clears `rankedLayerMinScore = 0` trivially. |
| Guardian deny (correct) | +0 (default archetype deny applies nothing) | The wall's honest answer to a clean ledger is neutrality — the visible *absence* is designed. |

### Timing targets (player-facing minutes)

| Slot | Target | Cap | Reasoning |
|---|---|---|---|
| V1 | 2:00 | 3:30 | First-ever case; everything is read for the first time. |
| V2 | 2:00 | 3:00 | Same shape as V1, now fluent. |
| V3 | 4:00 | 6:00 | Reference-book search + first deny + citation slip pause. |
| V4 | 3:00 | 5:00 | Deliberation between eyes and machine. |
| V5 | 2:30 | 4:00 | Directives re-read; short case. |
| V6 | 4:30 | 8:00 | The deliberation IS the content; the cap is generous by design. |
| **Shift total** | **≈ 18 min** | ≈ 29 | Sanities: speedrun ≈ 10–12; deliberate ≈ 28. Both fine for Day 1. |

### Travel rule of the day

One forbidden nation+era pair: **Egypt + Ancient Egypt** (adapted 2026-08-22
during Task 0 — the original proposal "Japan + V5's era" is impossible:
Imperial Japan is the only Japan era and V6's destination must stay legal).
Exactly one rule: a directive the player can hold in their head all day.
V5's pool targets it; V6 does NOT violate it (her destination is legal —
the *identity* is the crime; this keeps the two moral cases mechanically
distinct).

### Evening economy (worked end-to-end)

| Line | Typical (5 correct) | Merciful (4 correct + mercy) | Worst (2 correct, 4 warnings/citations mixed) |
|---|---|---|---|
| Shift income | 40 | 32 − ladder penalty 0–10 | 16 − 20 |
| Expenses: lodging 16 + heat 4 + family 2×5 | −30 | −30 | −30 ⚠ (drift worsens; bills still covered by design floor: base day pay ⚠ guarantees ≥ 30 on any day with ≥ 3 correct — worst case dips negative but Day-1 endings are gated; verify floor with real `HomeEconomy` constants in Task 0) |
| Remaining | 10 | −2…+2 | −34 (carried as debt ⚠ if the system supports it; else clamp — Task 0) |
| Lexicon @ 12 | buy = 0 left, or skip = 10 saved | out of reach — Day 1 mercy has a price | out of reach |
| Slot @ 3/spin | optional 1–2 spins | unwise | unavailable |

The table's intended read: **mercy costs the Lexicon.** That is the whole
evening, expressed in one purchase the player can't make.

---

## Risks & open questions

- **Era/attribute vocabulary** — this design references eras and timeline
  attributes by role ("Classical Greece," "Guardian attribute"); exact ids
  must be verified against `ContentLibrary_Main` (implementation Task 0)
  before authoring. If a needed era pairing doesn't exist, prefer adapting
  the anchor to what exists over authoring new nation-era profiles on Day 1.
- **Reference-book variety** — books have one entry per nation+era; V4's
  "odd but honest" mismatches rely on comparing against *wrong* pages, which
  works, but the pool will feel same-shaped quickly. Accepted for Day 1;
  flagged for enrichment later.
- **Directives-window UX debt** — V5 leans on the briefing headline as a
  crutch. If the directives window stays easy to miss, V5 teaches players to
  deny by luck. The known UX follow-up should land before Day 3.
- **Respect review** — real-nation forgery stories need a read-through by
  the director against the tone guide before shipping; V3's German forger
  and V6's Japanese family are the two highest-attention passes.
- **The sibling's record** — if Emi Asakura's citizen record is missing,
  `CaseFactory` refuses to forge (provable-only rule) and V6 silently
  degrades to an honest case. Task 4 makes the record a hard authoring
  requirement with a validation check.
- **What could break the day** — a player who accepts V3 *and* denies V6
  unproven has a miserable ledger and a smudged wall; that's a legitimate
  Day 1 story (the game noticed), but Day 2's difficulty must not assume a
  competent Day 1. DayPlan_Inv_Day2 tuning is out of scope here, flagged.

## Out of scope (follow-ups)

- **Chrono Converter (ADDED 2026-08-22, director request — "another layer").**
  Desktop app translating era-native dates ↔ modern reckoning with a per-era
  plausible-span check. Informational only (no formal proof path); pure rules
  in `TimeDesk.Domain.CalendarConverter` + calendar data on `EraSO`, which is
  now the single source driving BOTH birth-date generation and conversion.
  Future hook if wanted: span violations could become a formal proof style.

- **Verdict signifier — SHIPPED 2026-08-22 (polish pass).** Every verdict now
  shows a pausing receipt (green cleared/denied slip with pay, red for
  citations) and the verdict line persists on the booth HUD through the next
  READY beat. The original finding is preserved below for the record:
  correct verdicts used to set-and-clear the result text within one frame.
- Any new mechanic, window, or code system beyond the `endingsMinDay` knob.
- Day 2+ day plans (only their existence as consequence surfaces).
- The missing-record identity mechanic (would obsolete V6's specific lie).
- Pinning windows across cases, interrogation, photo capture.
