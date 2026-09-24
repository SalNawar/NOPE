# World model: design (piece 1)

*2026-09-24 · approved by Saleh in chat ("yes go ahead") · branch `feat/world-model` (stacked on `feat/cursor-hover-shift-clock`)*

Replaces the made-up world with the real one, and makes every world fact come from one lookup that piece 5 (history changes facts) will plug into.

## 1. Behaviour

- **The world is 8 countries × 5 eras = 40 places.** The countries are Egypt, Iraq/Mesopotamia, Greece, Italy, China, Japan, Britain and Germany; the eras are Ancient, Medieval, Early modern, Industrial and Modern. Each place is one researched moment (e.g. "Abbasid Baghdad", 830 CE). It has a display name, a currency, a language, a signature technology, a capital, a ruler, and 8 male and 8 female period names. The **Future** era exists as the office's own time, but nobody travels from it yet (piece 5).
- **Three-day ramp:**

| Day | Eras | Countries | Queue | Rules |
|---|---|---|---|---|
| 1 | Ancient | Egypt, Iraq, Greece, Italy | 8 | none |
| 2 | Ancient, Medieval | + China, Britain | 10 | no travel to Ancient Egypt |
| 3 | Ancient, Medieval, Early modern | all 8 | 12 | no travel to Medieval China; no travel to Early modern Japan |

- **"Today's world" is the day's eras × countries.** Travellers come only from today's places. The reference books list only today's places (day 1's currency book has 4 entries), in country-then-era order.
- **Forged fields take a random wrong value** from today's other places, drawn from the traveller's own seeded stream, instead of always the first row. The origin (match) proof is unchanged: the forged value belongs to another place in the books.
- **Visitors are born in their place's time.** Each place stores a birth-year range (the moment's year minus an age of 70 to 18). Years before 1 CE are written "1450 BCE". A forged birth date shifts the year by 2–24 and never produces year 0.
- **Same run + same day = same visitors.** Case generation uses a seeded random stream per traveller, derived from the day seed. The day seed formula is unchanged, so existing seeds keep their event schedules.
- **Claim line:** "I request passage home to Abbasid Baghdad (Medieval)." Citizen Records show the same origin label.
- **Morning paper:** the first nightly ranking of the 40 places is silent. After that, only tier changes are reported, which come from the player's sends.
- **Old saves are rejected** (save version 2), because place and era ids change. Title shows New Run only.

## 2. Design

- **Place = `NationEraProfileSO`**, the existing "country at a time" type, with the id `{country}_{era}`. It gains `moment` (text), `year`, `birthYearMin`/`birthYearMax`, `facts` (a list of `{ClueCategory, value}`: Currency, Language, Technology, Geography = capital, Politics = ruler), `maleNames`, `femaleNames`, and an `OriginLabel` (for example "Abbasid Baghdad (Medieval)"). The ClueCategory enum is unchanged; it already has Geography and Politics.
- **`EraSO`** gains `order` (chronological). **`DayPlanSO`** gains `allowedNations` (empty means all).
- **`FactTable` (TimeDesk.Domain, pure, string-keyed).**
  - `Add(nationId, eraId, originLabel, category, value)`
  - `Get(nationId, eraId, category)`
  - `Rows(category)` in insertion order
  - `OriginLabel(nationId, eraId)`
  - `PickOtherValue(category, exclude, randomIndex)`: a distinct value that differs from `exclude` under the same trimmed, case-insensitive comparison `DiscrepancyLog` uses (made `internal` so it's shared, not copied)
  - `FactRow.ToEvidence()` via the existing `CompareEvidence.ForReferenceEntry`

  `DiscrepancyLog` and its tests are otherwise untouched.
- **`ContentLibrarySO.BuildFactTable(DayPlanSO)`** is the only code that reads profile facts: today's places, ordered country then era. This is the seam piece 5 extends with history. `GameManager` builds the table once per day, a snapshot, and hands it to `CaseFactory` and the investigation UI.
- **`ReferenceBookSO` becomes a cover** (title plus category). `entries`, `GetValue`, `GetAnyOtherValue`, `ReferenceEntry` and `description` are removed in the same change, so there are never two sources of truth. `ReferenceBookWindowController` and the fallback text render `facts.Rows(category)`.
- **Randomness (TimeDesk.Domain, tested):**
  - `IRandomSource`: `Range(min, maxExclusive)` with Unity's tolerance for max ≤ min, and `Value()` in [0,1).
  - `SeededRandom`: SplitMix64, so it's stable across runtimes.
  - `Seeds`: `Day` is byte-identical to the old `RunManager.GetDaySeed`, plus `Mix` (avalanche) and `ForCase(daySeed, index)`, salted so case streams never replay the schedule stream.
  - `WeightedRandom` moves to Domain and takes an `IRandomSource`. Zero-weight items can no longer be picked.
  - `BirthDates.Generate`, `Forge`, `Format` and `Parse`, with BCE handling.
  - `UnityRandomSource` (Assembly-CSharp) keeps the Home slot machine as it was.
- **`CaseFactory(lib, facts)`, `GenerateDayCases(plan, state, daySeed)`:** a fresh `SeededRandom(Seeds.ForCase(...))` per case. It picks the place from today's profiles (era weights × allowed nations), takes names from the place pools via `NameRoster`, draws birth dates from the place range, and gets provable and forged values and field values from `FactTable`, logging a warning on a content gap. It stores `CaseInstance.originLabel` for Citizen Records.
- **`TimelineService.RecomputeDominance`** stays silent on the first ranking (no previous tiers).
- **`SaveSystem`:** `SaveVersion = 2` and `MinCompatibleVersion = 2`. `HasSave()` and `Load()` ignore older files with a warning.
- **Content: one authoritative generator.** `Tools > TimeDesk > Generate World` reads the committed `Assets/Data/World/world_source.json` (the researched facts, moments, years and names, country themes, and the day ramp). It creates or updates the eras, the 8 nations, the 40 profiles (with Democracy/Science/Art baselines from each country's theme, and no tier effects), the rules and the 3 day plans (reusing the `DayPlan_Inv_Day1..3` assets, since OfficeScene falls back to Day 1), then sets every `ContentLibrary_Main` array explicitly. It deletes the retired assets listed in section 3. `InvestigationContentGenerator` and `Phase7ContentGenerator` are deleted.
- **Validator** additions: every profile has all 5 facts, names, and a valid birth range; every day plan's eras × allowed nations resolve to profiles.

## 3. Retired

- Made-up nations, profiles, attributes, archetypes and rules.
- The country-named eras (`Era_China` and the rest) and `Era_Rome`.
- The 5 Phase 7 profiles and their 30 tier effects.
- The 21 Phase 7 legendaries (already unreachable; premade characters are redesigned in piece 4).
- The legacy clue assets, `DocTemplate_Currency`/`Lang`, `CaseBlueprint_Basic` and `DayPlan_1..3`. `Test_DayLoop.unity` is repointed to `DayPlan_Inv_Day1`/`CaseBlueprint_Investigation`.

The legacy clue code path stays (with an empty library) for the text-mode fallback.

## 4. Out of scope

- Days 4+ (they keep replaying the inspector fallback plan).
- Clothing and appearance (piece 4, from the committed costume research).
- History-dependent facts (piece 5).
- Question books for capital and ruler (piece 3; the facts are stored now).
- Saving the day's generation inputs, so Continue after the end-of-shift save regenerates the day.

## 5. Tests (EditMode)

- `FactTableTests`: get hit and miss, row order, origin label, `PickOtherValue` (exclusion is case- and whitespace-insensitive, distinct values only, none available gives null), and rows round-tripping through `DiscrepancyLog.TryRegister` (mismatch and origin proofs still register).
- `SeededRandomTests`: same seed gives the same sequence, bounds, `Range(n,n)`, reversed bounds.
- `SeedsTests`: a golden value for `Day`, distinct days and cases, and a case stream that differs from the raw day-seed stream.
- `WeightedRandomTests`: zero weights never picked, all-zero gives default, a single item, rough proportions.
- `BirthDatesTests`: range, BCE formatting and parsing, forge offsets, never year 0, round trip.

Existing suites stay green. The build is verified in the branch's own Unity instance: generator, builder, the Test Runner, and a scripted play-through of day 1.
