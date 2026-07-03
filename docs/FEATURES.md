# NOPE — Feature Inventory

The contract of what the game does. **Any change that removes or alters a
listed behaviour must update this file in the same commit.** Tested = covered
by the EditMode suite in `Assets/Tests/EditMode`.

## Run & meta

- [ ] Title scene → new run / continue via `RunManager` (+ `SaveSystem` persistence)
- [ ] Day plans per day number from `ContentLibrary_Main` (fallback: inspector plan)
- [ ] Deterministic per-day seed for case generation
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
- [ ] Fallback text-mode investigation when the rich desk isn't built

## Scoring & consequences

- [ ] Correct decision: pay (base × pay-rate multiplier + timeline bonuses; legendary bonus)
- [ ] Wrong decision: citation (free warnings, then escalating penalties), stability loss
- [ ] Evidence-gated denial: denying a forger with **zero** documented discrepancies = citation + deduction even though the visitor lied (`requireEvidenceToDeny` toggle)
- [ ] Directive-violation denials never need scanned evidence
- [ ] Only provable forgeries are generated (reference book must contain the claim's truth; birth dates provable via citizen records; names never forged until the missing-record mechanic lands)
- [ ] Timeline impacts apply only on ACCEPT; sends tracked per era
- [ ] Shift ledger (tested: `ShiftLedgerTests`); citation slip pauses the day until acknowledged

## Content & tooling

- [ ] `Tools > TimeDesk > Generate Investigation Sample` — full playable world (eras, nations, profiles, archetypes, books, templates, rules, day plans)
- [ ] `Tools > TimeDesk > Build Office UI` — idempotent, authoritative scene builder
- [ ] Travel rules: era / nation / nation+era forbidden, shown in briefing + directives
- [ ] Day events system (before/after-case scheduled events; no event types authored yet)
- [ ] Legendary encounters (bonus pay, extra stability risk)
- [ ] Debug panel (dev tools)
