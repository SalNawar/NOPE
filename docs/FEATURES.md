# NOPE — Feature Inventory

The contract of what the game does. **Any change that removes or alters a
listed behaviour must update this file in the same commit.** Tested = covered
by the EditMode suite in `Assets/Tests/EditMode`.

## Run & meta

- [ ] Title scene → new run / continue via `RunManager` (+ `SaveSystem` persistence)
- [ ] Day plans per day number from `ContentLibrary_Main` (fallback: inspector plan); day numbers are unique across the library — the fictional `DayPlan_4/5/6` set was renumbered off days 1–3 so the real-world investigation plans own days 1–3
- [ ] Deterministic per-day seed for case generation
- [ ] Endings evaluated after every verdict (`EndingService`); firing threshold on stability; bankruptcy threshold — **gated by `GameConfigSO.endingsMinDay` (default 2 — Day 1 cannot end the run)** (tested: `EndingGateTests`)
- [ ] Home phase between days: expenses, family conditions & care, slot machine (pay-rate modifier), upgrades

## Office scene — two-state booth

- [ ] World-space booth (back wall, partitions, desk, traveller, CRT, READY sign) with Cinemachine office/monitor cameras
- [ ] Click CRT or READY sign → zoom into monitor (desktop UI); Escape or "< Office" button → pull back
- [ ] Diegetic readouts: wall calendar (day), stability monitor (percent + lamp tint), credits till (with ding)
- [ ] Timeline-reactive poster (sprite swaps driven by timeline cues)
- [ ] Ranked office layers: back wall follows the top-scoring nation, mid decor the top attribute, desk decor the top profile+attribute pairing — sprite swaps driven by raw timeline scores, with a per-layer debug label showing the current winner (tested: `ScoreRankingTests`, `ScoreKeyTests`)
- [ ] Day One mercy wall-trace: accepting the sibling (V6) applies a +8 Kinship impact — the single largest Day-1 attribute score, so the mid-decor layer shows the kinship variant on Day 2 after mercy and stays neutral after a clean deny (content fact, not a mechanic; verified by `Tools > TimeDesk > Validate Day One Spine`)
- [ ] Ranked-layer art is generated per content id into `Assets/Art/Office/Ranked/` — Art Bible colours for authored nations/attributes, deterministic hash colours otherwise; pair sprites are two-tone (nation body + attribute band). Replacing a generated PNG with real art needs no code change (tested: `RankedArtNamingTests`)
- [ ] A ranked layer only reacts to a score above `GameConfigSO.rankedLayerMinScore` (default 0, strict) — an all-negative or zero category shows the neutral sprite and reports "no clear leader" rather than presenting a least-bad score as dominant (tested: `ScoreRankingTests`)
- [ ] Ranked-layer debug labels are gated to editor / development builds, so they can never render in a player build regardless of the per-layer `showDebugLabel` toggle
- [ ] Morning briefing as "THE TEMPORAL TIMES" newsletter over the booth (Start Shift) — headline copy is authored per day on `DayPlanSO.briefingHeadlines`, today's travel directives are listed beneath it
- [ ] End-of-day "SHIFT LEDGER" newsletter over the booth (Go Home), incl. undocumented-denials line
- [ ] Per-case READY gate: visitor is presented only after the player taps READY (tested: `ReadyGateTests`)

## Fake-OS desktop

- [ ] XP-style wallpaper, taskbar with Start button and system tray (Day / Credits / Stability)
- [ ] Start menu: Settings (stub window) + Power (quit); fixed-height entries
- [ ] Desktop icon grid top-left; reference book icons + app icons (documents come via the intercom, not icons)
- [ ] Upgrade-gated icons (Lexicon/Dialect/Material) dim until the upgrade is owned
- [ ] Draggable windows; min/max/close chrome on ALL windows (app, document, reference)
- [ ] Placeholder apps: Internet, Lexicon, Dialect, Material, Clue Log, Notes
- [ ] Directives sticky-note window (closed by default, opened from icon; shows day's travel rules)
- [ ] Every new case closes all open windows (pin system planned to override)
- [ ] Citizen Records app: type a name → agency record (Name/Born rows are compare-clickable; origin + clerk note)

## Investigation loop

- [ ] Claim banner (visitor name + stated destination/era)
- [ ] Intercom interaction panel: per-case traveller actions — "Request Travel Passport", "Request Transit Permit" (more actions planned: interrogation, photo capture)
- [ ] Documents render as SCANNED pages (white page + photo placeholder on dark scanner backing), multi-page, structured fields
- [ ] Passport carries identity fields: Full Name + Date of Birth (checked against Citizen Records)
- [ ] Reference book windows (category ground truth per nation+era, paged)
- [ ] Click-to-compare any two values; MATCH/MISMATCH bar (visual, no auto-verdict); auto-sized text
- [ ] Scanner = Deviation Report: true contradictions auto-register (tested: `DiscrepancyLogTests`)
  - [ ] Mismatch proof: forged field ≠ claimed-era reference entry
  - [ ] Match proof: forged field = a *different* era/nation's entry (origin proof)
  - [ ] Record proof: forged identity field ≠ agency citizen record (tested)
  - [ ] Junk comparisons never register (wrong category, foreign-era mismatch, honest fields)
  - [ ] One discrepancy per category; cleared per case; window auto-opens on first find
  - [ ] Compare bar flips to a red "DEVIATION LOGGED — …" verdict when evidence registers (never a green MATCH)
- [ ] Accept / Deny decision buttons
- [ ] Every verdict shows a pausing receipt (green TRAVEL AUTHORIZED / TRAVEL DENIED slip with pay; red slip for citations) — the day holds until acknowledged, and the verdict line stays on the booth HUD through the next READY beat
- [ ] Fallback text-mode investigation when the rich desk isn't built (logs a loud warning naming the missing wiring — degraded mode must never masquerade as the real desk)

## Scoring & consequences

- [ ] Correct decision: pay (base × pay-rate multiplier + timeline bonuses; legendary bonus)
- [ ] Wrong decision: citation (free warnings, then escalating penalties), stability loss
- [ ] Evidence-gated denial: denying a forger with **zero** documented discrepancies = citation + deduction even though the visitor lied (`requireEvidenceToDeny` toggle)
- [ ] Directive-violation denials never need scanned evidence
- [ ] Only provable forgeries are generated (reference book must contain the claim's truth; birth dates provable via citizen records; names never forged until the missing-record mechanic lands)
- [ ] Blueprint pinning: a `CaseBlueprintSO` may optionally pin era pool, nation, given name, birth date, intro line, and one exact forced forgery (category + value) — honored when the blueprint is forced into a slot, enabling scripted anchor visitors
- [ ] Timeline impacts apply only on ACCEPT; sends tracked per era
- [ ] Shift ledger (tested: `ShiftLedgerTests`); citation slip pauses the day until acknowledged

## Day One content set (2026-08-22)

- [ ] `DayPlan_Inv_Day1`: six visitors — forced anchors at slots 1/3/6 (V1 honest Greek / V3 first forger / V6 the sibling), generated pools at 2/4/5 (A: 100% honest practice; B: 30% single-forgery twist; C: honest but forbidden-destination Egypt directive), legendary chance 0
- [ ] Reference books cover the five real-world nation-era pairs (Currency/Language/Technology entries for Greece, Imperial Germany, Japan, Egypt, China)
- [ ] Birth dates are generated in the visitor's era calendar: Attic months for Classical Greece ("14 Elaphebolion 431"), Egyptian civil months for Ancient Egypt, and era-bounded years for the imperial nations — never a 20th-century default on an ancient traveler (calendar data lives on `EraSO`, single source for generation AND conversion)
- [ ] Chrono Converter desktop app ("Calendar" icon): translates era-native dates to modern reckoning and back, with an era span check ("WITHIN Classical Greece (450–350 BCE)" / "OUTSIDE — this date does not belong to the era"). Click any date field (passport DOB, Records "Born" row) to drop it straight into the converter — the claimed era pre-selects; then press CONVERT. Typing still works (bare year = modern-year mode). Informational layer — formal proof still runs through the compare bar. Pure rules in `CalendarConverter` (tested: `CalendarConverterTests`; smoke checks in Validate Day One Spine)
- [ ] Real-nation name pools (era-flavored, surname-style — registry keys)
- [ ] Travel rule of the day: Egypt + Ancient Egypt forbidden (V5 denies lawfully without evidence; V6's Japan destination stays legal)
- [ ] Evening economy: expenses 20 + 5×2 family = 30/day, starting money 0, Lexicon at 12 (lights the desktop Lexicon icon), Wall Material at 35 (wanting-object), slot table 55/20/20/5 with spin cost 3 (overtime = a 1-day PayRateBonus effect, not a permanent multiplier — expires via the nightly effect system)
- [ ] `Tools > TimeDesk > Validate Day One Spine` — 25-seed content validation of the whole shape (anchors, pools, records, mercy sizing)
- [ ] `Tools > TimeDesk > Simulate Full Day One` — headless full-day integration test through the real runtime systems (evidence gate, verdict scoring, evening economy, nightly resolve, Day-2 wall + ending gates, slot statistics) across four player archetypes: perfect / mercy / mercy-with-mistake / disaster

## Content & tooling

- [ ] `Tools > TimeDesk > Generate Investigation Sample` — full playable world (eras, nations, profiles, archetypes, books, templates, rules, day plans)
- [ ] `Tools > TimeDesk > Build Office UI` — idempotent, authoritative scene builder
- [ ] Travel rules: era / nation / nation+era forbidden, shown in briefing + directives
- [ ] Day events system (before/after-case scheduled events; no event types authored yet)
- [ ] Legendary encounters (bonus pay, extra stability risk)
- [ ] Debug panel (dev tools)
- [ ] Timeline Inspector prints scores grouped by nation with authored display names instead of raw keys, marking `[dominant]`/`[supporting]` (tested: `ScoreKeyTests`)
