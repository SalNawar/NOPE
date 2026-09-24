# NOPE — Feature Inventory

The contract of what the game does. **Any change that removes or alters a
listed behaviour must update this file in the same commit.** Tested = covered
by the EditMode suite in `Assets/Tests/EditMode`.

## Run & meta

- [ ] Title scene → new run / continue via `RunManager` (+ `SaveSystem` persistence; save version 2 — version-1 saves from the made-up world are ignored, Title offers New Run)
- [ ] Day plans per day number from `ContentLibrary_Main` (fallback: inspector plan). Ramp: day 1 Ancient × Egypt/Iraq/Greece/Italy (8); day 2 + Medieval, + China/Britain, no Ancient Egypt (10); day 3 + Early modern, all 8 countries, no Medieval China / Early modern Japan (12)
- [ ] Deterministic case generation: same run + same day = same travellers; one seeded stream per traveller (`Seeds.ForCase`), plus a day stream for rule violators, a separate stream for legacy clues and a per-traveller lie stream (`Seeds.ForLies`); day seed formula unchanged (seeding tested: `SeedsTests`, `SeededRandomTests`, `WeightedRandomTests`; whole-day determinism is checked in Unity, not by the EditMode suite)
- [ ] Endings evaluated after every verdict (`EndingService`); firing threshold on stability; bankruptcy threshold
- [ ] Home phase between days: expenses, family conditions & care, slot machine (pay-rate modifier), upgrades

## Office scene — two-state booth

- [ ] World-space booth (back wall, partitions, desk, traveller, CRT, READY sign) with Cinemachine office/monitor cameras
- [ ] Click CRT or READY sign → zoom into monitor (desktop UI); Escape or "< Office" button → pull back
- [ ] Diegetic readouts: wall calendar (day), stability monitor (percent + lamp tint), credits till (with ding)
- [ ] Timeline-reactive poster (sprite swaps driven by timeline cues)
- [ ] Morning briefing as "THE TEMPORAL TIMES" newsletter over the booth (Start Shift)
- [ ] End-of-day "SHIFT LEDGER" newsletter over the booth (Go Home), incl. undocumented-denials line
- [ ] Per-case READY gate: visitor is presented only after the player taps READY (tested: `ReadyGateTests`)
- [ ] Analog wall clock (placeholder face + hands) driven by the shift clock

## Fake-OS desktop

- [ ] XP-style wallpaper, taskbar with Start button and system tray (Day / Credits / Stability / Clock)
- [ ] Start menu: Settings (stub window) + Power (quit); fixed-height entries
- [ ] Desktop icon grid top-left; reference book icons + app icons (documents come via the intercom, not icons)
- [ ] Upgrade-gated icons (Lexicon/Dialect/Material) dim until the upgrade is owned
- [ ] Draggable windows; min/max/close chrome on ALL windows (app, document, reference)
- [ ] Placeholder apps: Internet, Lexicon, Dialect, Material, Clue Log, Notes
- [ ] Directives sticky-note window (closed by default, opened from icon; shows day's travel rules)
- [ ] Every new case closes all open windows (pin system planned to override)
- [ ] Citizen Records app: type a name → agency record (Name/Born rows are compare-clickable; origin + clerk note); a liar's record is their cover identity (claimed origin), so records never reveal a true home; the rich desk warns once at start when the evidence system is active but Citizen Records is not wired (birth-date tells need it)

## World

- [ ] Real countries as lineages: Egypt, Iraq, Greece, Italy, China, Japan, Britain, Germany × Ancient / Medieval / Early modern / Industrial / Modern = 40 places (plus a Future era with no travellers yet)
- [ ] Each place has a moment and year, five facts (currency, language, technology, capital, ruler) and 8 male + 8 female period names
- [ ] Travellers are born 18–70 years before their place's year (set by the generator); ancient dates print as BCE (dates tested: `BirthDatesTests`)
- [ ] Visitor roles: Artist, Diplomat, Merchant, Scientist, Soldier, Wanderer (their accepted sends move Democracy / Science / Art); names come only from the claimed place's period names (a liar's name is part of their cover)
- [ ] Every traveller asks to go home to the place they claim, and their name, birth date, papers and Citizen Record all come from that claim. An honest traveller really comes from there: every field agrees with the books and the record. A liar really comes from another of today's places (any country, any of today's eras) and travels under the claimed place's cover identity, but the papers leak tells carrying the true home's value: a Currency, Language or Technology tell rewrites every field of that category; a birth-date tell keeps the record's day and month with a year from the true home's birth years. Each liar leaks the day's tell count (`DayPlanSO` "tell count", 1 on days 1–3), capped by the categories that can carry a tell for them. The liar chance is the blueprint's contradiction chance plus the slot-machine modifier and forgery-risk effects; rule violators and legendaries never lie; a rolled liar with no possible tell stays honest and a warning is logged (tell selection, eligibility and the may-lie exemptions tested: `LiesTests`, `ForgeryTests`; the liar chance, cover records and whole-day behaviour are checked in Unity, not by the EditMode suite)
- [ ] Traveller gender is recorded from the claimed place's name lists ("Marcus II" counts as Marcus; legendaries and "Subject #n" are unknown); it is not shown yet (tested: `TravellerGendersTests`, `NameRosterTests`)

## Investigation loop

- [ ] Claim banner (visitor name + "I request passage home to <place> (<era>)")
- [ ] Intercom interaction panel: per-case traveller actions — "Request Travel Passport", "Request Transit Permit" (more actions planned: interrogation, photo capture)
- [ ] Documents render as SCANNED pages (white page + photo placeholder on dark scanner backing), multi-page, structured fields
- [ ] Passport carries identity fields: Full Name + Date of Birth (checked against Citizen Records)
- [ ] Reference book windows list TODAY's places only (country then era, paged), read from the day's `FactTable` snapshot — the same values printed on papers (table tested: `FactTableTests`; the day's place filter is covered by the content validator and the Unity world check)
- [ ] Reference books: Currency Ledger, Tongues & Scripts, Index of Devices, Capitals Gazetteer, Rulers & Regents (all on the desktop from day 1; five book windows open in two staggered rows)
- [ ] Click-to-compare any two values; MATCH/MISMATCH bar (visual, no auto-verdict); auto-sized text
- [ ] Scanner = Deviation Report: true contradictions auto-register (tested: `DiscrepancyLogTests`)
  - [ ] Mismatch proof: a liar's tell ≠ claimed-era reference entry
  - [ ] Match proof: a liar's tell = a *different* era/nation's entry (origin proof); it names the traveller's true home
  - [ ] Record proof: a birth-date tell ≠ agency citizen record (tested)
  - [ ] Junk comparisons never register (wrong category, foreign-era mismatch, honest fields)
  - [ ] One discrepancy per category; a second proof of a documented category shows "ALREADY DOCUMENTED" in the compare bar and adds nothing; cleared per case; window auto-opens on first find
  - [ ] Compare bar flips to a red "DEVIATION LOGGED — …" verdict when evidence registers (never a green MATCH)
  - [ ] Reports label Geography CAPITAL, Politics RULER, Technology DEVICE and birth dates BIRTH DATE (tested: `DiscrepancyLogTests`)
- [ ] Accept / Deny decision buttons
- [ ] Fallback text-mode investigation when the rich desk isn't built (papers, the traveller's agency record and today's books)

## Shift clock & queue

- [ ] Papers, Please-style clock: 09:00–17:00 over 8 real minutes (GameConfigSO "Shift clock"); starts at Start Shift (tested: `ShiftClockTests`)
- [ ] The day plan's visitor count is the queue size (8 / 10 / 12 on days 1–3); the day ends at closing time or when the queue empties
- [ ] Closing: a traveller at the desk may be finished; one still behind READY is never called (tested: `DaySlotSequencerTests`, `ShiftFlowTests`, `ReadyGateTests`)
- [ ] Scheduled events / forced cases in slots never reached before closing are reported as a warning
- [ ] The clock pauses only while a citation slip is shown
- [ ] Visitor names are unique within a day ("Marcus II" once a pool runs out; a legendary never repeats a name used that day) (tested: `NameRosterTests`)

## Interaction feedback

- [ ] Game cursor in every scene: arrow, or a hand over anything clickable (`InteractionFeedbackSO` via `RunConfig`; click points derived from the cursor art) (tested: `CursorHotspotTests`)
- [ ] Hover highlight: white outline on booth clickables, XP-amber outline on desktop UI buttons and icons (tested: `OutlineMaskTests`)
- [ ] Hover outlines need Rectangle (not Tight/rotated) atlas packing on clickable sprites; otherwise a warning is logged

## Scoring & consequences

- [ ] Correct decision: pay (base × pay-rate multiplier + timeline bonuses; legendary bonus)
- [ ] Wrong decision: citation (free warnings, then escalating penalties), stability loss
- [ ] Evidence-gated denial: denying a liar with **zero** documented discrepancies counts as a wrong decision (citation and stability loss; a deduction once the free warnings are used) even though the visitor lied (`requireEvidenceToDeny` toggle) (the unproven-denial rule tested: `VerdictRulesTests`; the citation and deduction are Assembly-CSharp (`ShiftScoring`) and checked in Unity, not by the EditMode suite)
- [ ] Directive-violation denials never need scanned evidence (the gate exemption tested: `VerdictRulesTests`)
- [ ] Only provable tells are generated (tested: `ForgeryTests`, `BirthDatesTests`): a place fact is a tell only when a reference book covers it and the true home's value differs from the claim's and belongs to no other of today's places (so the books prove it and the origin proof names the home); a birth-date tell keeps day and month and takes a year from the true home's birth years, never the record's (provable via citizen records); names, capitals and rulers are never tells
- [ ] Every active travel rule gets at least one violator in the first half of the queue (`DayPlanSO` "guarantee rule violators", on by default) (slots tested: `ViolatorSlotsTests`)
- [ ] Timeline impacts apply only on ACCEPT and land on the claimed place (where the traveller is sent), liar or not; sends tracked per era
- [ ] A new run ranks every place's attributes from the baselines silently; each night's news then reports only real tier changes ("Science is now DOMINANT in …", "… is rising in …")
- [ ] Dominance tiers carry no gameplay effects for now: the Phase 7 tier effects were retired with the made-up world (history reacting to choices is a later piece)
- [ ] Shift ledger (tested: `ShiftLedgerTests`); citation slip pauses the day (and the shift clock) until acknowledged

## Content & tooling

- [ ] `Tools > TimeDesk > Generate World` — builds the real world from the hand-maintained `Assets/Data/World/world_source.json` (eras, countries, 40 places, rules, day plans with each day's tell count and tell channels, small talk, the interview's wording and its two layout limits (menu capacity, longest line), questions with an unlock-announcement trigger for every gated question, and dialogs) and wires `ContentLibrary_Main`, the case blueprint and the day plans named in its `content` section; checks every reference before writing anything, and that every line id is unique, authored interview text is ASCII, no line is longer than the transcript holds, no menu is fuller than the intercom shows and no dialog effect carries a timed modifier; idempotent; owns only `Assets/Data/World/{Eras,Nations,Places,Rules,Interview}` (unlisted assets there go to the trash) and never removes hand-authored legendaries, effects or triggers (the library's triggers are rewired: hand-authored ones kept in order, generated unlock triggers appended); the scratchpad script that once wrote the source is retired
- [ ] Content validator checks every place (five facts, names, birth years set) and every day plan (today has places; every weighted era has one; every rule can be broken; listed legendaries come from today's places; at least one tell channel); questions (a book or the Citizen Record proves every question's category; one question per category; answer templates hold `{value}`), dialogs (structure, reachable endings, effects and their op types, one-shot), the interview's wording and menu capacity, every upgrade id, and small talk for the eras day plans use
- [ ] `Tools > TimeDesk > Build Office UI` — idempotent, authoritative scene builder
- [ ] Travel rules: era / nation / nation+era forbidden, shown in briefing + directives
- [ ] Day events system (before/after-case scheduled events; no event types authored yet; events placed past closing time never run)
- [ ] Legendary encounters (bonus pay, extra stability risk) — none authored in the real world yet (premade characters come later)
- [ ] Debug panel (dev tools)
