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

## 6. Review amendments (2026-09-24)

An independent review (correctness, content pipeline, intent audit) led to these changes; they supersede the matching lines above.

- **Places** keep `birthYearMin`/`birthYearMax`, facts and names only. `moment` and `year` stay in `world_source.json` as research context (the generator derives birth years from `year`) and are not copied to the asset, since nothing reads them. `NationEraProfileSO.GetFact` is removed: `BuildFactTable` is the only reader of place facts (the validator reads the raw list). The origin label has one format, `OriginLabels.Format` (Domain, tested), and a case takes its label from `FactTable.OriginLabel`.
- **Forgery rule in Domain** (`Forgery.IsProvable`, tested): a place fact may be forged only when a reference book covers its category (capital and ruler have no books yet), today's table holds the claim's truth and another value exists. `FactTable.PickOtherValue` takes an `IRandomSource`; `HasOtherValue` answers without drawing.
- **Forged birth dates** stay inside the place's birth years (never born after one's own moment). The shift (2–24 years) is a `CaseBlueprintSO` knob. A place without birth years (0..0) gives "Unknown" dates, which are never forged, and the validator reports it.
- **Guaranteed rule violators.** A single forbidden place is rare in the queue (about 40% of day 2 and 3 runs would never test the directive), so each active rule gets one violator in the first half of the queue, drawn from a separate day stream (`Seeds.ForViolators`, `ViolatorSlots`, tested). `DayPlanSO.guaranteeRuleViolators` (default on) turns it off.
- **Legacy clue draws** use their own stream (`Seeds.ForClues`), so clue settings never change who is a forger.
- **Names** come only from the place's names (then "Subject #n"). `NationSO.namePool` and `ArchetypeSO.namePool` are removed (modern-sounding pools, no longer reachable).
- **Timeline:** instead of silencing whichever ranking comes first (which also hid tier changes caused on day 1), a new run ranks the baselines silently (`TimelineService.SeedDominance` from `RunManager.NewRun`) and every night reports changes against that. Tier effects stay retired, so dominance only makes news for now.
- **Saves:** `HasSave` itself warns when it ignores an older version (the check in `Load` was unreachable).
- **Generator:** reads the authored asset paths (library, blueprint, day-plan folder, attributes, archetypes, books) from a `content` section of the source and checks every reference before writing anything. Era and nation assets are named by id. It owns `Assets/Data/World/{Eras,Nations,Places,Rules}` (unlisted assets there go to the OS trash) and only drops missing references elsewhere, so it never wipes hand-authored legendaries, effects or triggers, and it leaves legendary chances to their authors. Upgrades, slot outcomes and endings are not world arrays and are untouched. Retiring the made-up world was a one-time migration run for this change and committed as deletions; the generator has no delete list. The authored assets it requires (attributes, archetypes, books, document templates, blueprint) are source-controlled content edited in the inspector, not generated.
- **Visitor roles** are the six Phase 7 archetypes (Artist, Diplomat, Merchant, Scientist, Soldier, Wanderer), whose accepted sends move Democracy / Science / Art; the made-up Investigation archetypes are retired.
- **Validator** also reports rules that no place of the day can break and listed legendaries whose place is outside the day's world. `CaseFactory` warns about the same legendary case at runtime.
- **Why some rules stay outside Domain:** today's place filter (`ContentLibrarySO.TodaysProfiles`: plan eras × allowed nations, ordered country then era) and the violator placement work on ScriptableObject references, so they are glue around tested Domain pieces (`ViolatorSlots`, `FactTable`, `Forgery`); the content validator and the Unity world check cover them. The timeline ranking and the save gate belong to pre-existing non-Domain services; moving them is out of scope here (piece 5 reworks the timeline).
- **Tests added:** `ForgeryTests`, `ViolatorSlotsTests`, `OriginLabelsTests`, and new cases in `BirthDatesTests` (range clamp, across year 0), `FactTableTests` (`HasOtherValue`) and `SeedsTests` (violator and clue streams).
- **Known follow-ups (not in this change):** the day event schedule (`DayPlanSO`) and Home family conditions (`HomeEconomy`) still use `System.Random`; on day 3 early-modern Egypt and Greece share the ruler "Sultan Mustafa II" (historically right; matters once ruler questions arrive in piece 3).

## 7. Verification (2026-09-24)

Run in the branch's own Unity 6000.4.11f1 editor through temporary `-executeMethod` scripts (not committed):

- One-time retirement deleted 102 assets. Generate World made 6 eras, 8 nations, 40 places, 3 rules and 3 day plans; a second run changed no file.
- Validate Content Library: no issues.
- World check (days 1–3, seeds 12345 and 999): same seed gives the same travellers and a different seed differs. Every forged fact has a book and a value from another of today's places. Forged birth years stay inside the place's range. Each active rule has a violator in the first half of the queue. Names are unique and origin labels match. Timeline: 40 places ranked silently, a quiet night makes no tier news, and +2 Science for New Kingdom Egypt reports "Science is now DOMINANT in New Kingdom Egypt."
- EditMode suite: 285 passed. The one failure is the known third-party `UnitySkills.Tests.Core.PerceptionSkillsTests.SceneSummarize_CountsObjectsCorrectly` (scene-dependent, unrelated). Offline Domain run: 156/156.
- Scripted day-1 play-through on the committed OfficeScene (the builder is unchanged, so the scene was not rebuilt):
  - The claim line names a real place, and all three books list exactly today's 4 places.
  - Comparing the forged passport language with the claimed place's row logs a "LANGUAGE INCORRECT" deviation, so the Deny is justified and gives no citation.
  - Forcing closing time closes the booth and shows the results panel.
  - No warnings or errors. Continuing the saved run regenerates the same traveller.
