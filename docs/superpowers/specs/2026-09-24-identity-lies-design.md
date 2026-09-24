# Identity & lies: design (piece 2)

*2026-09-24 · decisions made by Claude under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), open to his review · branch `feat/identity-lies` (stacked on `feat/world-model` @ `14bd22e`)*

Every traveller gets a true home as well as a claimed one. An honest traveller comes from the place they claim, and everything they carry agrees with it. A liar comes from another of today's places and travels under that claimed place's cover identity, but the disguise leaks at least one tell carrying the real home's value. Piece 1's random "forged field" is retired: every anachronism is now a liar's tell.

Line numbers refer to the branch base `14bd22e`.

## 0. Decisions

Claude made these under Saleh's instruction "go with all pieces, don't stop" (2026-09-24), working from the piece-2 code analysis (session scratchpad `piece2_map.json`). They bind this piece and are open to Saleh's review. D-numbers match the analysis's decisions. E rows are the follow-up decisions from its completeness check. R rows are the details this spec settles where a decision left them open.

| Id | Decision | Rationale |
|---|---|---|
| D1 | Today's place pick becomes the **claimed** home and drives the whole cover identity: given name, birth date, Citizen Record, claim line and papers. Only a liar gets a second pick, the **true** home. The existing `CaseInstance` fields keep meaning "the claim". True-home data is added, never renamed, and doc comments that say "true" are corrected. | The first seven draws of every traveller (legendary, era, blueprint, archetype, place, name, birth date; `CaseFactory.cs:147-178`) stay untouched, so every seed keeps its claims, names and dates. The claim line and papers need no change (`CaseFactory.cs:213-217`, `312-331`). Renaming the fields would touch about 15 call sites without changing behaviour. |
| D2 | "Forged papers, honest home" travellers no longer exist. Honest means fully consistent papers, and every anachronistic field is a liar's tell. `ShouldAccept = !isLiar && claimAllowed`. A liar must have at least one tell the scanner can register. If none is possible, the traveller stays honest and a warning is logged. | This is Saleh's lie model: "honest = claim == true home and everything consistent". It leaves one generator per visible symptom. A liar with no registrable tell could not be denied without an unproven-denial citation (`ShiftScoring.cs:102-108`). |
| D3 | A liar's Citizen Record shows the registered **cover** identity: the cover name, the cover birth date and the **claimed** origin label. Records never reveal the true home on their own. | Printing the true home as plain text would give away every liar at once and make every tell pointless. No code or UI changes: `BuildRegistry` already reads the claim fields (`CaseFactory.cs:427-435`). |
| D4 | Piece 2 has two kinds of tell. **Place-fact tells** use the book categories (Currency, Language, Technology) and carry the **true** home's value. A **BirthDate tell** keeps the cover date's day and month but prints a year from the true home's birth years, so it differs from the record. A BirthDate tell is possible only when both places have birth years and a different year exists (see R2). Piece 1's random forgery is removed with its tests: `FactTable.PickOtherValue`, `BirthDates.Forge` + `AddYears`, and `CaseBlueprintSO.forgedBirthYearShift`. | A place-fact tell is proven against the claim's book row (mismatch proof), and the true home's row names the home (origin proof). The BirthDate tell keeps the Records app's evidence job. |
| E1 | A tell is a **category**. It rewrites every field of that category (Currency appears on both the passport and the permit). Categories are chosen uniformly among the eligible ones, not per field. | The two documents never disagree with each other (a document-vs-document compare can never be logged, `DiscrepancyLog.cs:181-185`). This also matches capping the tell count by categories, since the log keeps one discrepancy per category (`DiscrepancyLog.cs:252-255`). |
| D5 | The true home can be any **other** of today's places, including places in other eras. Candidates come from `CaseFactory`'s existing list of today's places (`_todays`, `CaseFactory.cs:26,73`). | Only today's places have facts and book rows (`FactTable` is the one fact source, `ContentLibrarySO.cs:107-122`). A home outside today's world would need a second source. |
| D6 | The liar chance is the existing per-traveller formula: `blueprint.contradictionChance` + `WorldState.forgeryChanceModifier` + `ForgeryChanceBonus` effects, clamped to 0..1 (`CaseFactory.cs:268-271`). The tell count is a per-day `DayPlanSO` knob (default 1). It is authored in `world_source.json` `days[]`, written by the generator, and capped at the number of eligible categories. The legacy per-clue use of `contradictionChance` (`CaseFactory.cs:569-579`) stays and is documented as shared. | This reuses the slot outcome "Forgery warning" (+0.1) and the Democratic Collapse effect (+0.15), which already mean "more cheats". The generator is the authority for day plans (FEATURES.md:94). |
| D7 | A new salted per-traveller stream, `Seeds.ForLies(caseSeed)`, carries the liar roll, the true-home pick and the tell picks. The forge draws leave the case stream. | This follows the `ForClues` precedent (`Seeds.cs:45-49`). The number of liar draws varies, and later pieces will append draws to the case stream. The cost is that piece-1 seeds now produce different cheaters; nothing has been shipped and cases are never saved. |
| D8 | The verdict stays ACCEPT/DENY. Denying a liar needs at least one logged discrepancy of any proof kind (the existing evidence gate). The verdict records whether the traveller lied and where their true home is. | The existing gate and UI work unchanged. A redirect outcome needs a place picker and new scoring, which belong to piece 6. |
| D9 | A traveller whose **claim** breaks an active rule is always honest: the guaranteed violators, and anyone whose ordinary pick lands on a forbidden place. Rules check only the claim. | Each directive stays a clean test. The claim is the destination, and the rules forbid destinations (`TravelRuleSO.Allows`, `DayPlanSO.cs:117-127`). |
| E2 | Legendaries never lie in piece 2. | Premade characters are designed in piece 4, and no `LegendarySO` asset exists. |
| D10 | The traveller's gender is recorded. It is derived from which list of the drawn place (`maleNames` or `femaleNames`) the name came from, after removing any `NameRoster` numeral suffix, and costs no extra draw. Legendaries and "Subject #n" are unknown. | Pieces 3 (dialog) and 4 (per-gender outfits) need it. Deriving it changes no draw. |
| E3 | The `ShouldAccept`/`isLiar` decision and the evidence-gate decision move into Domain, which is the smallest change that makes them testable. Assembly-CSharp keeps thin callers. | The Definition of done requires rule logic to have decision-table tests (ENGINEERING_MANIFESTO.md:51-56), and Assembly-CSharp cannot be tested. |
| E4 | Player-facing text that piece 2 makes inaccurate is reworded to talk about lies and disguises: the citation text, the scanner idle hint, and "forgers" in the ledger docs. Flavour lines stay: the slot outcome "Forgery warning" and the trigger/effect names. | The copy should stay truthful without rewriting flavour. |
| E5 | The piece-1 spec is an approved record. This spec supersedes its random-forgery lines explicitly (§3.2) and does not edit it. | Same pattern as the piece-1 review amendments. |
| E6 | No dev cheat to force a liar. | At chance 0.5, seeds give plenty of liars for verification. |
| E7 | `WorldState` is unchanged, so the save version stays 2. | Cases, verdicts and the ledger are not saved (`SaveSystem.cs:11-35`, `GameManager.cs:130`). |

### Refinements settled by this spec

| Id | Refinement | Rationale |
|---|---|---|
| R1 | True-home candidates are today's other places that give at least one eligible tell, and the pick is uniform among them. The "stays honest" fallback fires only when no other place qualifies. | This cuts needless honest fallbacks while keeping D5. On days 1–3 every ordered pair of today's places differs in all three book categories (12/12, 132/132 and 552/552 pairs, computed from `world_source.json`), so on those days the filter changes nothing. |
| R2 | The BirthDate tell's year is drawn uniformly from the true home's birth years, excluding year 0 and the cover year. The tell is eligible when the cover date is readable and such a year exists. | "Drawn year ≠ cover year" then holds by construction with one draw and no re-draw. Ranges do overlap: 4 of 132 day-2 pairs and 30 of 552 day-3 pairs, and Early-modern Egypt and Greece both span 1630..1682. |
| R3 | A category can be a tell only if the traveller's papers print it (at least one field). Eligible categories keep the order in which they first appear on the papers. `Lies.Plan` derives the distinct categories itself from the papers' `DocumentField` list, so a category printed twice (Currency) is one candidate. | A tell in an unprinted category could never be seen or logged. A fixed order keeps the pick deterministic. Deriving the list in Domain puts the de-duplication, which the tell count and "without replacement" depend on, under test instead of in `CaseFactory` glue. |
| R4 | `CaseInstance.isForged` is replaced by `IsLiar`, derived from the new `trueHome`. `CaseVerdict.wasForged` is replaced by `wasLiar` plus `trueHomeLabel`. | Kept alongside, the old flags would always equal the new ones, and two flags with one meaning break zero re-implementation. D1's "add, don't rename" applies to the claim fields. The decisions' Save rule (ledger fields stay additive) does not apply: `CaseVerdict` and the ledger are never persisted (E7; `SaveFile` holds only `version` and `WorldState`, `SaveSystem.cs:30-35`). |
| R5 | `Forgery.IsProvable` becomes `Forgery.IsProvableTell` (same class and file). `FactTable.HasOtherValue`, `PickOtherValue` and the private `OtherValues` are removed with their tests. | They lose their only callers (`Forgery.cs:32`, `CaseFactory.cs:297`). |
| R6 | One exemption rule, `Lies.MayLie(isLegendary, claimAllowed, papers)`, covers D9, E2 and cases with no papers (a null or empty field list). Exempt travellers make no lie draw. | The draws are on the traveller's own stream, so skipping them shifts nothing else. |
| R7 | A tell count below 1 counts as 1 in Domain. The generator rejects `tells < 1`, and `DayPlanSO.tellCount` is `[Min(1)]`. | A liar always has at least one tell. A missing JSON key then fails loudly instead of meaning 0. |
| R8 | Gender first tries an exact list match, then the name with its numeral suffix removed (`NameRoster.BaseName`). Both passes compare with `DiscrepancyLog.ValuesMatch` (trimmed, case-insensitive); `TravellerGenders` writes no comparison of its own. A null list counts as empty. A name found in both lists or in neither is Unknown. In piece 2 gender is data only: the case logs (`CaseFactory` and the `GameManager` case start) print it. | Eight names appear in more than one place (for example Fatima and Zaynab), but never in both lists of one place, so the derivation must use the drawn place's own lists. `NationEraProfileSO.maleNames`/`femaleNames` are plain serialized arrays that can be null on a hand-authored place (`AllNames` guards it, `NationEraProfileSO.cs:56-57`). |
| R9 | One `CaseFactory` helper computes the liar chance, for both the lie roll and the legacy clue count. | Today the formula is written twice (`CaseFactory.cs:268-271`, `571-574`). The legacy behaviour is unchanged: same formula, same clue stream. |
| R10 | The verdict records the true home as a label, `trueHomeLabel`, which is the claim's label for an honest traveller. `trueEraId` keeps its current meaning, the claimed era (the destination). | Piece 6 report lines print a label. The legacy era path gives `trueEraId` its meaning (`ShiftScoring.cs:28`). |
| R11 | Draw order on the lie stream: the roll, then the home, then one pick per tell category (without replacement), then the BirthDate year if BirthDate was picked. | This fixes the stream contract. `LiesTests` pins it with golden-order scripts (`ScriptedRandom`, §5): a draw of the wrong kind, a swapped draw or an extra draw fails the test. `ScriptedRandomTests` pins that failing behaviour itself, so the guarantee cannot erode silently. |
| R12 | New copy. Wrong accept: "Approved a disguised traveller or a forbidden destination." Scanner idle hint: "Compare a document field against the claimed place's reference entry, the entry it really belongs to, or the Citizen Record to log evidence." | The E4 wording, spelled out. |
| R13 | A place-fact tell also needs the home's value to be unique today: no row of `facts.Rows(category)` other than the home's own row matches it under `DiscrepancyLog.ValuesMatch`. The check reads the existing rows, so it adds no index. | D4 promises that the origin proof names the true home. If a third place shared the home's value, clicking that place's row would name the wrong place and disagree with `trueHomeLabel`. With this rule the promise holds by construction. Days 1–3 share no book value (0 collisions, computed from `world_source.json`), so nothing changes there. The data already holds one shared value, Language "Baghdadi Arabic; Turkish" (Iraq Early modern and Industrial), and piece 5 history edits can add more. A shared value only removes that category for that home, and the home drops out as a candidate if nothing else is eligible (R1). |
| R14 | The true home's label comes from today's `FactTable`, exactly like the claim's: one `CaseFactory.PlaceLabel` helper serves both, and the label is stored on the case. | The piece-1 amendment makes `FactTable.OriginLabel` the source of a case's label (world-model spec §6). Reading `NationEraProfileSO.OriginLabel` for the home would give the claim and the home two label sources, which could disagree with the scanner's `FactRow.OriginLabel` once piece 5 feeds history into the table. |
| R15 | A birth-date tell stays provable wherever the desk runs. The text-mode fallback prints the traveller's agency record (name, born, origin) next to the papers and books. The rich desk logs a warning once when the evidence system is active but Citizen Records is not wired. | About a quarter of day-1 liars carry only a BirthDate tell (tell count 1, up to four eligible categories), and only the record proves it. Without the record the fallback shows such a liar as honest, and a rich scene without Records cannot win the case. The manifesto asks for a warning that names the fix for a wiring gap. |

## 1. Behaviour

### 1.1 Who is who

- **Every traveller claims a home.** The claim is one of today's places, picked exactly as in piece 1: an era by the day's weights, then a country among today's places in that era, or a planned violator's place. Everything the player sees comes from the claim:
  - the banner "I request passage home to <claimed place>";
  - the name, from the claimed place's names;
  - the birth date, from its birth years;
  - the papers: its currency, language and device;
  - the Citizen Record.
- **Honest traveller:** really from the claimed place. Every field on every paper equals the claimed place's book entries and the record. Nothing can be logged against them.
- **Liar:** really from another of today's places, the **true home**. It can be any country and any of today's eras (on day 1 every liar is a same-era, cross-country liar). The liar wears the claimed place's identity as a cover, and the disguise leaks **tells**: by default one per liar, set per day.
  - **Currency, Language or Technology tell:** every field of that category prints the true home's value. For Currency that is both "Coin of Issue" (passport) and "Bond Currency" (permit, page 2).
  - **Birth-date tell:** "Date of Birth" keeps the record's day and month but shows a year from the true home's birth years, never the record's year. That year may still fall inside the claimed place's range when the two ranges overlap.
  - Names, capitals (Geography) and rulers (Politics) are never tells.
- **Who may lie:** any non-legendary traveller who has papers and whose claimed destination is allowed today. Always honest:
  - guaranteed rule violators;
  - anyone else whose claim a rule forbids;
  - legendaries (none are authored).
- **A liar's true home may itself be forbidden today.** For example, someone from Ancient Egypt posing as Ancient Greece on day 2. Rules check only the claim, so this traveller is denied as a liar, which needs evidence, not as a violator.
- **How often:** each eligible traveller lies with the liar chance, capped at 1:
  - the blueprint's contradiction chance (0.5 on `CaseBlueprint_Investigation`);
  - plus tomorrow's slot modifier (+0.1 after "Forgery warning");
  - plus active forgery-risk effects (+0.15 while Democratic Collapse lasts).

  Day 1 (8 travellers, no rules) averages 4 liars.
- **Tell count:** the day's tell count, capped at the number of categories that can carry a tell for this liar. Days 1–3 use 1.
- **No possible tell:** if the roll makes a traveller a liar but no other place today could give them a tell they can be caught by, they stay honest and a warning is logged. This cannot happen on days 1–3.

### 1.2 Catching a liar

- Tells are logged exactly as forged fields were:
  - A place-fact tell against the **claimed** place's book row gives "CURRENCY INCORRECT — papers: "X" / expected: "Y"" (mismatch proof).
  - The same tell against the **true home's** row gives "… papers show "X", which belongs to <true home>" (origin proof). This is how the player learns where a liar is from. No row of today's book other than the home's shows the tell's value (R13), so the origin proof can only name the true home.
  - A birth-date tell against the Citizen Record's Born row gives "BIRTH DATE INCORRECT — papers: … / agency records: …" (record proof). It proves the lie but does not name the home, because no book maps years to places.
- Comparing a tell against a third place's row, comparing two papers, or comparing honest fields logs nothing (unchanged).
- Typing a liar's name in Citizen Records finds their **cover** record: cover name, cover birth date and the claimed origin. It never shows the true home.
- The log still keeps one discrepancy per category, so with one tell a liar yields at most one deviation.

### 1.3 Verdicts

| Traveller | Accept | Deny with ≥1 logged deviation | Deny with 0 logged deviations |
|---|---|---|---|
| Honest, claim allowed | correct | impossible (an honest traveller has nothing loggable) | wrong: "Denied a legitimate, permitted traveler." |
| Honest, claim forbidden (violator) | wrong: "Approved a disguised traveller or a forbidden destination." | impossible (a violator is honest, so nothing is loggable) | correct (directive denials need no evidence) |
| Liar (claim always allowed) | wrong: "Approved a disguised traveller or a forbidden destination." | correct | unproven denial: wrong, "Deviation denied without documented evidence. Scan the papers next time." In the text-mode fallback, where the evidence system is inactive, this denial is correct. |

- The text-mode fallback lists the papers, the traveller's agency record (name, born, origin) and today's books, so every kind of tell can be spotted there too, including a birth-date-only liar (R15).
- Pay, citations, warnings and stability amounts are unchanged.
- The timeline moves only on ACCEPT. Impacts land on the **claimed** place, where the traveller is sent, whether or not they lied. This is unchanged code, now intended: `TimelineService.cs:67-91` reads `inst.nation` and `inst.claimedEra`, and both are the claim. Sends are counted per claimed era.
- The shift report is unchanged ("Undocumented denials: N").
- The scanner idle hint uses the R12 text.

### 1.4 Determinism

- Same run + same day = same travellers, same liars, same true homes, same tells.
- For any seed, claims, names, registered birth dates, roles and violator placement are identical to piece 1. The lie draws live on a new stream, and piece 1's forge draws were the last draws on each traveller's case stream (`CaseFactory.cs:273,291-297`).
- Which travellers lie differs from which travellers were piece-1 forgers.

### 1.5 Gender

- Every generated traveller is male or female, depending on which of the claimed place's name lists their name came from ("Marcus II" counts as Marcus). Legendaries and "Subject #n" are unknown. Nothing on screen shows gender yet; the case logs print it.

## 2. Design

### 2.1 Where things live

- **TimeDesk.Domain** (`Assets/Scripts/Domain`, pure, EditMode-tested):
  - new: `Lies.cs`, `VerdictRules.cs`, `TravellerGender.cs`;
  - changed: `Forgery.cs`, `BirthDates.cs`, `FactTable.cs`, `Seeds.cs`, `NameRoster.cs`, `ShiftLedger.cs` (`CaseVerdict`);
  - doc comments only: `DocumentField.cs`, `DiscrepancyLog.cs`, `CitizenRegistry.cs`.
- **Assembly-CSharp:** `CaseFactory.cs`, `CaseInstance.cs`, `DayPlanSO.cs`, `CaseBlueprintSO.cs`, `Shift/ShiftScoring.cs`, `GameManager.cs`, `UI/InvestigationUIController.cs`, plus doc-only edits (§2.8).
- **Assembly-CSharp-Editor** (no asmdef in `Assets/Editor`): `WorldContentGenerator.cs` and `ContentLibraryValidator.cs` (one message).
- **Content:**
  - `Assets/Data/World/world_source.json` gets `days[].tells`;
  - the generated `DayPlan_Inv_Day1..3.asset` get `tellCount: 1`;
  - `CaseBlueprint_Investigation.asset` is re-saved by the generator (`WorldContentGenerator.cs:71-74`), which drops its stale `forgedBirthYearShift` line (`:23`).
- **Tests:** `Assets/Tests/EditMode` (TimeDeskEditMode, which references `TimeDesk.Domain` and `TimeDesk.Visuals` only, not Assembly-CSharp). Every new test targets Domain. One shared test helper, `ScriptedRandom.cs`, is added with its own `ScriptedRandomTests` (§5).

### 2.2 `CaseInstance` (`Assets/Scripts/CaseInstance.cs`, Assembly-CSharp)

These fields keep their names and mean **the claim**. Only their doc comments change:

| Field | Line | New doc meaning |
|---|---|---|
| `trueEra` | 12-13 | The era the traveller claims as home and is sent to. It is the correct era on the legacy era-pick path. A liar's real era is `trueHome.era`. |
| `nation` | 24-25 | The nation the traveller claims as home (the destination; timeline impacts land here). |
| `originLabel` | 27-28 | Label of the claimed place (claim line and Citizen Records). |
| `visitorGivenName` | 36-37 | The registered given name, from the claimed place's names (a liar's cover name; records lookup key). |
| `trueBirthDate` | 39-40 | The registered date of birth, from the claimed place's birth years (what the agency has on file). A birth-date tell prints a different year on the papers. |

Added:

- `public NationEraProfileSO trueHome;`: where the traveller really comes from. It is another of today's places for a liar and null for an honest traveller, whose home is the claim.
- `public string trueHomeLabel = string.Empty;`: the true home's label, taken from today's `FactTable` like `originLabel` (R14). `Disguise` sets it together with `trueHome`; it stays empty for an honest traveller.
- `public bool IsLiar => trueHome != null;`
- `public string HomeLabel => IsLiar ? trueHomeLabel : originLabel;`: where the traveller really comes from, as a label, used by the verdict and the logs.
- `public TravellerGender gender;`

Removed: `isForged` (55-56).

Changed: `ShouldAccept => VerdictRules.ShouldAccept(IsLiar, claimAllowedByRules)` (64-68), with its doc now saying "deny a liar or a rule-breaking destination".

### 2.3 Domain rules (TimeDesk.Domain)

**`Seeds`** (`Seeds.cs`, LF):
- add `public const int LieSalt = 0x4C494553;` ("LIES") and `public static int ForLies(int caseSeed) => Mix(caseSeed, LieSalt);`.
- The doc says it is the stream for the liar roll, true-home pick and tell picks, apart from the case stream so lie tuning never changes who travellers are.
- `ForClues`' doc (45-49) changes "who is a forger" to "who lies".

**`BirthDates`** (`BirthDates.cs`, LF):
- remove `Forge` (63-95) and `AddYears` (97-106);
- add:
  - `public static bool HasOtherYear(string coverDate, int yearMin, int yearMax)`: true when `coverDate` parses and [yearMin, yearMax] (bounds may be reversed) holds a year that is neither 0 nor the cover year. It draws nothing.
  - `public static string PickOtherYear(string coverDate, int yearMin, int yearMax, IRandomSource rng)`: the cover's day and month with a year drawn uniformly from those years (exactly one `Range` draw, an index into the eligible years in ascending order). Null when `HasOtherYear` is false (no draw).
- Class doc (4-8): "generation and birth-date tells".

**`Forgery`** (`Forgery.cs`, LF). Replace `IsProvable` with:

```csharp
public static bool IsProvableTell(ClueCategory category, string claimNationId, string claimEraId,
                                  string coverBirthDate, HomeCandidate home, FactTable facts,
                                  ICollection<ClueCategory> bookCategories)
```

The decision table:
- `Name` → never.
- `BirthDate` → `BirthDates.HasOtherYear(coverBirthDate, home.BirthYearMin, home.BirthYearMax)`.
- Any other category → all of the following:
  - `facts` and `bookCategories` are present;
  - a book covers the category;
  - `facts.Get(claim)` and `facts.Get(home)` are both non-empty;
  - `!DiscrepancyLog.ValuesMatch(claimValue, homeValue)` (the shared internal comparison);
  - no row of `facts.Rows(category)` other than the home's own row (same nation and era id) matches `homeValue` under `DiscrepancyLog.ValuesMatch` (R13).

The class doc becomes: "Which fields can carry a liar's tell: only fields the player can disprove; a place-fact tell's origin proof can only name the true home."

**`FactTable`** (`FactTable.cs`, LF):
- remove `HasOtherValue` (99-103), `PickOtherValue` (105-114) and `OtherValues` (116-133);
- the class doc (40-47) changes "field values, forgeries, the claim's origin label" to "field values, tells, the claim's origin label" and "Citizen Records carry the origin label the case took from here" to "... the claimed place's label".

**`Lies`** (new `Lies.cs`, LF):

```csharp
/// One of today's places as the lie rules see it.
public readonly struct HomeCandidate
{
    public readonly string NationId, EraId;
    public readonly int BirthYearMin, BirthYearMax;
    public HomeCandidate(string nationId, string eraId, int birthYearMin, int birthYearMax);
}

public enum LieOutcome { Honest, Liar, NoPossibleLie }

/// The outcome of one traveller's lie roll.
public sealed class LiePlan
{
    public LieOutcome Outcome { get; }
    public int HomeIndex { get; }                      // index into `todays` (the unfiltered list Plan was given); -1 unless Liar
    public IReadOnlyList<ClueCategory> Tells { get; }  // pick order; empty unless Liar
    public void ApplyTo(IEnumerable<DocumentField> fields); // rewrites + flags every field of each tell category
}

public static class Lies
{
    public static bool MayLie(bool isLegendary, bool claimAllowed, IReadOnlyList<DocumentField> papers);
        // !isLegendary && claimAllowed && papers != null && papers.Count > 0
    public static LiePlan Plan(float liarChance, int tellCount,
                               string claimNationId, string claimEraId, string coverBirthDate,
                               IReadOnlyList<HomeCandidate> todays, IReadOnlyList<DocumentField> papers,
                               FactTable facts, ICollection<ClueCategory> bookCategories, IRandomSource rng);
}
```

`Plan` makes its draws in this order (R11):

1. **Roll:** `rng.Value() < liarChance`, one draw. On a miss the plan is `Honest`.
2. **Candidates:** the printed categories are the distinct categories of `papers` in first-appearance order (R3; a category printed on two fields counts once). For each `todays[i]` that is not the claim (compared by nation and era id), take the eligible categories: the printed categories, in that order, for which `Forgery.IsProvableTell` holds. A place with none is not a candidate (R1). The candidates keep their `todays` order. With no candidates the plan is `NoPossibleLie` (no further draw).
3. **Home:** `j = rng.Range(0, candidates.Count)` picks among the filtered candidates. `HomeIndex` is that candidate's original index in `todays`, never `j` itself, so `todays[HomeIndex]` is the home whose values the tells carry, even when R1 dropped an earlier place.
4. **Tells:** `n = clamp(tellCount, 1, the home's eligible count)` (R7). Pick n categories uniformly without replacement, one `Range` draw each.
5. **Birth year:** if `BirthDate` is among them, `BirthDates.PickOtherYear(coverBirthDate, home range, rng)`, one draw.

Values are stored per tell:
- a place fact stores `facts.Get(home.NationId, home.EraId, category)`;
- `BirthDate` stores the picked date.

`ApplyTo` sets `value` and `isAnachronism = true` on every field whose category is a tell. It changes nothing for an `Honest` or `NoPossibleLie` plan.

**`VerdictRules`** (new `VerdictRules.cs`, LF):
- `ShouldAccept(bool isLiar, bool claimAllowed) => !isLiar && claimAllowed`
- `IsUnprovenDenial(bool requireEvidence, int evidenceCount, bool accepted, bool isLiar, bool claimAllowed) => requireEvidence && evidenceCount == 0 && !accepted && isLiar && claimAllowed`

This is today's gate (`ShiftScoring.cs:102-103`) with `isForged` replaced by `isLiar`. `evidenceCount` is −1 when the evidence system is inactive.

**`TravellerGender`** (new `TravellerGender.cs`, LF):
- `public enum TravellerGender { Unknown, Male, Female }`
- `public static class TravellerGenders { public static TravellerGender FromNameLists(string givenName, IReadOnlyList<string> maleNames, IReadOnlyList<string> femaleNames); }`

The R8 table applies: exact match first, then `NameRoster.BaseName(givenName)`, both compared with `DiscrepancyLog.ValuesMatch`; a null list counts as empty; a null or blank name, or a name in both lists or in neither, is Unknown.

**`NameRoster`** (`NameRoster.cs`, LF): add `public static string BaseName(string name)`. It returns the trimmed name without the " {numeral}" suffix that `Take` adds (`NameRoster.cs:64-69`). The last word counts as a suffix only when it is exactly `Roman(n)` for some n ≥ 2, so "Anna Maria", "Mary Ann" and "Sitt al-Wuzara" keep their full names. The check looks the word up in the set of every suffix `Take` can add, which `Roman` itself writes (n = 2..3999, built once on first use); there is no second numeral parser. The numeral format therefore has one owner, `Roman`.

**`CaseVerdict`** (`ShiftLedger.cs`, CRLF):
- `wasForged` (130-131) becomes `public bool wasLiar;` ("True if the traveller lied about their home").
- Add `public string trueHomeLabel = string.Empty;` ("Where the traveller really comes from: the claimed place for an honest traveller").
- Docs: `shouldAccept` (127) becomes "honest + allowed"; `unprovenDenial` (142) and `UnprovenDenialCount` (52) say "liar" instead of "forger".

**`DocumentField`** (`DocumentField.cs`, CRLF): docs at 22-27 and 42 describe `isAnachronism` as "this value is a liar's tell (anachronistic for the claim)".

**`DiscrepancyLog`** (`DiscrepancyLog.cs`, CRLF), doc comments only, so its docs agree with `DocumentField`'s:
- 50 (`CompareEvidence.isAnachronism`): "Document side: true if the value is a liar's tell (anachronistic for the claim)."
- 98 (`Discrepancy.documentValue`): "The tell's value printed on the visitor's papers."
- 143-147 (class doc): "a liar's tell differs from …" and "a liar's tell equals …" in place of "a forged document field".
- 191-192: "Only a liar's tell is a contradiction; a coincidental mismatch/match on an honest field proves nothing."
- 261 (`ValuesMatch`): "(mirrors the compare bar; shared with Forgery.IsProvableTell and TravellerGenders.FromNameLists)". `FactTable` no longer calls it once `OtherValues` is removed (R5).

**`CitizenRegistry`** (`CitizenRegistry.cs`, CRLF), doc only: `CitizenRecord.origin` (16) becomes "The registered origin label (a liar's is their claimed cover origin; records never show a true home)." Under D3 the old "Where/when this citizen belongs" is false for every liar.

`OriginLabels`, `ViolatorSlots` and `WeightedRandom` are unchanged.

### 2.4 Case generation (`CaseFactory.cs`, Assembly-CSharp, CRLF)

**Fields:**
- add `private IRandomSource _lieRng` ("the current traveller's lie stream; apart from `_rng` so lie tuning never changes who travellers are"). No stored candidate list is added: `Disguise` projects `_todays` at its one call (D5, no new index).
- Doc fixes: `_clueRng` (31) "never change who lies"; `_bookCategories` (34) "only these can carry a place-fact tell".

**`GenerateDayCases`** (85-87): after `_clueRng`, add `_lieRng = new SeededRandom(Seeds.ForLies(caseSeed));`.

**New `private string PlaceLabel(NationEraProfileSO p) => _facts.OriginLabel(p.nation.id, p.era.id) ?? p.OriginLabel;`** (R14): the one way a place's label is read. The claim (172-174 becomes `place != null ? PlaceLabel(place) : FallbackOriginLabel(nation, trueEra)`, same result) and the true home both use it.

**`GenerateSingleCase`** (141-222):
- **Summary** (139): "- Build documents, fill their fields from the claim, then maybe disguise a liar".
- **Gender:** after the name (175), set `inst.gender = legendary != null || place == null ? TravellerGender.Unknown : TravellerGenders.FromNameLists(givenName, place.maleNames, place.femaleNames)`. The field is set in the initialiser at 181-194. Null name arrays are fine (R8).
- **Step 7** (212-217):
  - the comment (212) becomes "7) Investigation layer: stated claim, structured fields, the lie (if any), daily rules.";
  - keep lines 213-216 as they are;
  - replace 217 with `List<DocumentField> fields = PopulateDocumentFields(inst);` followed by `LiePlan lie = Disguise(inst, fields, plan, blueprint, state, caseIndex1Based);`.
- **Log line (219):** replace `forged=` with `liar={inst.IsLiar}, home='{inst.HomeLabel}', tells=[{(lie != null ? string.Join(", ", lie.Tells) : string.Empty)}], gender={inst.gender}`. This log is the real caller of `LiePlan.Tells`. Also swap the `archetype?.displayName` null-propagation on a ScriptableObject for an explicit check (a manifesto defect on a line being edited).

**`PopulateDocumentFields`** (228-305): the signature becomes `List<DocumentField> PopulateDocumentFields(CaseInstance inst)`. It fills every field from the claim through `ResolveFieldValue` (unchanged) and returns the fields in paper order. It never returns null: the early return at 236-237 (`inst == null || _lib == null`) returns an empty list. Everything from line 264 on, the forge roll, provable list, target and wrong value, is deleted. Its doc no longer mentions forging.

**New `private LiePlan Disguise(CaseInstance inst, List<DocumentField> fields, DayPlanSO plan, CaseBlueprintSO blueprint, WorldState state, int caseIndex1Based)`.** It returns the plan, or null when the traveller is exempt:

1. If `!Lies.MayLie(inst.isLegendary, inst.claimAllowedByRules, fields)`, return null. No draw is made.
2. Project `_todays` to `HomeCandidate`s in the same order (`new HomeCandidate(p.nation.id, p.era.id, p.birthYearMin, p.birthYearMax)`, at most 24 small structs). The projection is a local, so it can never fall out of step with `_todays`.
3. Call `Lies.Plan` with:
   - `LiarChance(blueprint, state)`;
   - `plan.TellCount`;
   - the claim ids from `inst.claimedNation` and `inst.claimedEra`;
   - `inst.trueBirthDate`;
   - the projection;
   - `fields` as they are (Domain derives the distinct categories, R3);
   - `_facts`, `_bookCategories`, `_lieRng`.
4. `NoPossibleLie` logs this warning (the text spells out the fix): "[CaseFactory] Case {caseIndex1Based}: rolled a liar, but no other place today can carry a provable tell against '{originLabel}' (no printed, book-covered fact that differs from the claim's and belongs to that place alone, and no birth year other than the record's), so the traveller stays honest. Widen the day's eras or countries, add a reference book for a printed category, or give places that share a fact value distinct values." It names both causes: a place can differ from the claim and still give no tell when a third place shares its value (R13). The text keeps the prefix "rolled a liar, but no other place", which the §6 world check counts.
5. `Liar` sets `inst.trueHome = _todays[p.HomeIndex]` and `inst.trueHomeLabel = PlaceLabel(inst.trueHome)`, then calls `p.ApplyTo(fields)`.
6. It returns the plan.

**New `private float LiarChance(CaseBlueprintSO blueprint, WorldState state)`** (R9): `Mathf.Clamp01(blueprint.ContradictionChance + (state != null ? state.forgeryChanceModifier : 0f) + TimelineEffects.SumFloat(state, _lib, EffectOpType.ForgeryChanceBonus))`. `BuildDocumentsAndClues` (571-574) calls it too, with an unchanged result.

**Unchanged:** the code of `ResolveFieldValue`, `ResolveGivenName`, `GenerateBirthDate`, `PickPlace`, `PickEraFromPlan`, `TryRollLegendary`, `PlanViolators` and `BuildRegistry`.

Doc fixes:
- `BuildRegistry` (410-414): "Records carry the registered identity: an honest traveller's, or a liar's cover (claimed origin). They never reveal a true home."
- `ResolveFieldValue` (326-327): the placeholder keeps the field "internally consistent (never a tell)". This holds: `IsProvableTell` needs the claim's fact, so a category without one can never carry a tell.
- The class summary (5-13) describes the claim, cover identity and lie stream.
- The "true era" comments (137, 154, 358, 442) say "claimed era".

**Streams after piece 2:**

| Stream | Seed | Draws | Change |
|---|---|---|---|
| Case (`_rng`) | `Seeds.ForCase(daySeed, slot)` | legendary roll/pick, era, blueprint, archetype, place, name, birth date (3) | the forge roll, target and wrong-value draws are removed from the end, so no earlier draw moves |
| Clue (`_clueRng`) | `Seeds.ForClues(caseSeed)` | legacy clue count/picks | none |
| Violators | `Seeds.ForViolators(daySeed)` | slots and places | none |
| Lies (`_lieRng`) | `Seeds.ForLies(caseSeed)` | roll; if liar: home, one per tell, BirthDate year | new; no draws for exempt travellers |

### 2.5 Evidence and verdict path

**Evidence:** no code change. `DiscrepancyLog.TryRegister` (`DiscrepancyLog.cs:167-259`) already accepts exactly what a tell is: a document field with `isAnachronism` against a book row or record field of the same category. `InvestigationUIController.HandlePairCompared` (106-124) passes the claim ids; only its doc (102-105) changes, to "when the player compares a liar's tell against the reference entry or record that disproves it".
- A place-fact tell's value equals `facts.Get(home)`, never matches the claim's value, and matches no other row of today's table (`IsProvableTell`, R13). So:
  - the claim's row gives `ClaimMismatch`;
  - the home's row gives `ForeignOrigin`, with `actualOrigin` = the home's `FactRow.OriginLabel` (`DiscrepancyLog.cs:236-245`), which is the same `FactTable` label as `trueHomeLabel` (R14);
  - any third place's row proves nothing.
- A BirthDate tell never equals the record's `birthDate` (R2), so the record's Born row gives `RecordMismatch` (198-211).

**Verdict:**
- `CaseInstance.ShouldAccept` calls `VerdictRules.ShouldAccept`.
- `ShiftScoring.ResolveDecision` (`ShiftScoring.cs`, CRLF):
  - 57-62: doc says "deny a liar or a rule-breaking destination";
  - 74: log `liar=` in place of `forged=`;
  - 85: `wasLiar = inst != null && inst.IsLiar`, plus `trueHomeLabel = inst != null ? inst.HomeLabel : string.Empty`;
  - 98-108: gate condition `inst != null && VerdictRules.IsUnprovenDenial(config.requireEvidenceToDeny, evidenceCount, accepted, inst.IsLiar, inst.claimAllowedByRules)`, and the comment at 98 says "denying a liar";
  - 144: R12 text.
  - `Resolve` (the legacy era path, 13-55) is unchanged.
- `GameManager.cs` (CRLF):
  - 295: the case-start log adds claim label, `liar`, `home`, `gender`, with explicit null checks instead of `?.` on SOs;
  - 516: the `[Result]` log reads `verdict.wasLiar` and `verdict.trueHomeLabel` instead of `inst.isForged`.
  - Lines 489, 502-511 and 414 are unchanged.
- `GameConfigSO.requireEvidenceToDeny` doc (32-36): "denying a liar".

**Timeline:** no code change. `TimelineService.ApplyVerdictImpacts`' doc (45-50) now says impacts land on "(claimed nation, chosen era): where the traveller is sent, liar or not".

### 2.6 Knobs

| Knob | Where | Value today | Authored in |
|---|---|---|---|
| Liar chance | `CaseBlueprintSO.contradictionChance` | 0.5 on `CaseBlueprint_Investigation.asset:21` (code default 0.25) | Blueprint inspector (authored asset, not generated) |
| … tomorrow modifier | `WorldState.forgeryChanceModifier` | +0.1 from `SlotOutcome_Forgery_warning.asset:22`, reset nightly (`RunManager.cs:197-202`) | Slot outcome assets |
| … effects | `EffectOpType.ForgeryChanceBonus` | +0.15 from `Effect_Trigger_ForgeryRisk.asset:21` (Democratic Collapse, 3 days) | Effect/trigger assets |
| Tell count | new `DayPlanSO.tellCount` (`[SerializeField, Min(1)] int tellCount = 1`, property `TellCount`) | 1 on days 1–3 | `world_source.json` `days[].tells`, written by `Tools > TimeDesk > Generate World` |
| Tell-capable categories | `ContentLibrary_Main` reference books | Currency, Language, Technology | Library (unchanged) |
| Printed categories | `DocumentTemplateSO.fieldSpecs` | Passport: Name, BirthDate, Currency, Language. Permit: Technology, Currency | Templates (unchanged) |
| Evidence gate | `GameConfigSO.requireEvidenceToDeny` | true | Config (unchanged) |

Doc changes for these knobs:
- `CaseBlueprintSO.contradictionChance` (28-30): "Chance per traveller to be a liar (plus the WorldState and effect modifiers). The legacy clue path also reads it as the chance per clue line to be a contradiction."
- `WorldState.forgeryChanceModifier` (56): "Additive modifier to the liar chance (and the legacy per-clue contradiction chance)."
- The comment on `EffectSO.ForgeryChanceBonus` (44): "+liar chance (0..1)".
- `ContentLibrarySO.ReferenceBookCategories` (124): "only these can carry a place-fact tell".
- `DayPlanSO` "TRUE era" docs (39, 250-252): "claimed (home) era".
- `DocumentTemplateSO.fieldSpecs` (21-25): "a liar's tells rewrite every field of a tell category".

Generator (`WorldContentGenerator.cs`):
- `DayData` (461-469) gains `public int tells;`.
- `CheckReferences` (182-193) adds `if (d.tells < 1) errors.Add($"Day '{d.asset}' needs \"tells\" of at least 1.")`.
- `MakeDay` (262-287) writes `so.FindProperty("tellCount").intValue = d.tells;`, and its doc lists the tell count.

`world_source.json` gets `"tells": 1` on each day. The scratchpad build script `build_world_source.py` (lines 71-79) gets the same key so a rebuild keeps it.

Removed knob: `CaseBlueprintSO.forgedBirthYearShift`, its property and its `OnValidate` clamp (35-39, 72-73, 83-84).

### 2.7 Copy and messages

- `ShiftScoring.cs:144`: R12 text.
- `InvestigationUIController.cs` (CRLF):
  - 147-150: R12 idle hint. The "N deviation(s) documented. Denial is justified." line stays.
  - Text fallback (R15): `SetCitizenRegistry` (127-131) also keeps the registry in a private field. `BuildFallbackBody` (397-428) gets that registry and adds a "— AGENCY RECORD —" block between the papers and the books, printing the current traveller's record from `registry.Find(inst.visitorGivenName)`: Name, Born and Origin. It prints "No record on file." when there is none.
  - Wiring warning (R15): `Awake` (85-94) logs once, when `EvidenceSystemActive && recordsWindow == null`: "[InvestigationUIController] Citizen Records not wired: birth-date tells cannot be proven. Run Tools > TimeDesk > Build Office UI."
- `CitizenRecordsWindowController.cs:8` (doc only): "so a record can disprove a liar's birth-date tell (RecordMismatch evidence)".
- `ContentLibraryValidator.cs:133`: "... their birth dates can never carry a birth-date tell." replaces "are never forged".
- Kept as flavour or as names: `SlotOutcome_Forgery_warning` resultLine, `Trigger_DemocracyCollapse` description, `Effect_Trigger_ForgeryRisk` displayName, the `SlotOutcomeSO` class doc ("legendary/forgery/pay"), the `forgeryChanceModifier` and `ForgeryChanceBonus` names and the logs that print them (`RunManager.cs:199`, `DebugPanelController.cs:280`), and the `Forgery` class name.

### 2.8 Every file that changes

Found with `grep` over `Assets` for `isForged`, `wasForged`, `IsProvable`, `HasOtherValue`, `PickOtherValue`, `Forge(`, `ForgedBirthYearShift`, `ContradictionChance`, `ShouldAccept`, `trueBirthDate`, `originLabel`, `AllNames`, "forg" (case-insensitive) and "true era". Every "forg" hit, tests included, is either listed below or named as kept in §2.7 (the English word "forget" aside):

| File | Assembly | Change |
|---|---|---|
| `Assets/Scripts/Domain/Seeds.cs` | Domain | `LieSalt`, `ForLies`; `ForClues` doc |
| `Assets/Scripts/Domain/BirthDates.cs` | Domain | − `Forge`, `AddYears`; + `HasOtherYear`, `PickOtherYear`; class doc |
| `Assets/Scripts/Domain/Forgery.cs` | Domain | `IsProvable` → `IsProvableTell` |
| `Assets/Scripts/Domain/FactTable.cs` | Domain | − `HasOtherValue`, `PickOtherValue`, `OtherValues`; class doc |
| `Assets/Scripts/Domain/Lies.cs` (+ .meta) | Domain | new |
| `Assets/Scripts/Domain/VerdictRules.cs` (+ .meta) | Domain | new |
| `Assets/Scripts/Domain/TravellerGender.cs` (+ .meta) | Domain | new |
| `Assets/Scripts/Domain/NameRoster.cs` | Domain | + `BaseName` |
| `Assets/Scripts/Domain/ShiftLedger.cs` | Domain | `wasForged` → `wasLiar`, + `trueHomeLabel`, docs |
| `Assets/Scripts/Domain/DocumentField.cs` | Domain | docs 22-27, 42 |
| `Assets/Scripts/Domain/DiscrepancyLog.cs` | Domain | docs 50, 98, 143-147, 191-192, 261 (§2.3) |
| `Assets/Scripts/Domain/CitizenRegistry.cs` | Domain | doc 16 (`CitizenRecord.origin`) |
| `Assets/Scripts/CaseInstance.cs` | Assembly-CSharp | §2.2 |
| `Assets/Scripts/CaseFactory.cs` | Assembly-CSharp | §2.4 |
| `Assets/Scripts/DayPlanSO.cs` | Assembly-CSharp | + `tellCount`/`TellCount`; docs 39, 250-252 |
| `Assets/Scripts/CaseBlueprintSO.cs` | Assembly-CSharp | − `forgedBirthYearShift`; `contradictionChance` doc |
| `Assets/Scripts/Shift/ShiftScoring.cs` | Assembly-CSharp | §2.5 |
| `Assets/Scripts/GameManager.cs` | Assembly-CSharp | logs 295, 516 |
| `Assets/Scripts/UI/InvestigationUIController.cs` | Assembly-CSharp | doc 102-105; idle hint 147-150; registry field, fallback record block, Records wiring warning (§2.7) |
| `Assets/Scripts/UI/CitizenRecordsWindowController.cs` | Assembly-CSharp | doc 8 |
| `Assets/Scripts/Core/GameConfigSO.cs` | Assembly-CSharp | doc 32-36 |
| `Assets/Scripts/WorldState.cs` | Assembly-CSharp | doc 56 |
| `Assets/Scripts/EffectSO.cs` | Assembly-CSharp | comment 44 |
| `Assets/Scripts/ContentLibrarySO.cs` | Assembly-CSharp | doc 124 |
| `Assets/Scripts/DocumentTemplateSO.cs` | Assembly-CSharp | doc 21-25 |
| `Assets/Scripts/Timeline/TimelineService.cs` | Assembly-CSharp | doc 45-50 |
| `Assets/Editor/WorldContentGenerator.cs` | Editor | `DayData.tells`, check, `MakeDay` |
| `Assets/Editor/ContentLibraryValidator.cs` | Editor | message 133 |
| `Assets/Data/World/world_source.json` | content | `days[].tells` |
| `Assets/Data/Investigation/DayPlan_Inv_Day1..3.asset` | generated | `tellCount: 1` (Generate World) |
| `Assets/Data/Investigation/CaseBlueprint_Investigation.asset` | authored | stale `forgedBirthYearShift` dropped on re-save |
| `Assets/Tests/EditMode/{Forgery,BirthDates,FactTable,Seeds,NameRoster,WeightedRandom}Tests.cs` | tests | §5 |
| `Assets/Tests/EditMode/DiscrepancyLogTests.cs` (CRLF) | tests | tell vocabulary only (§5): `ForgedDocField`/`ForgedIdentityField` become `TellDocField`/`TellIdentityField`, `ForgedBirthDate_VsRecord_Registers_AsRecordMismatch` becomes `BirthDateTell_VsRecord_Registers_AsRecordMismatch`, the class doc says "A liar's papers print…", and one comment says "The tell equals…" |
| `Assets/Tests/EditMode/{Lies,VerdictRules,TravellerGenders,ScriptedRandom}Tests.cs` (+ .meta) | tests | new, §5 |
| `Assets/Tests/EditMode/ScriptedRandom.cs` (+ .meta) | tests | new shared helper, §5 |
| `docs/FEATURES.md` | docs | §3.3 |

Not changed, and checked:
- `CompareController.cs`, `DocumentWindowController.cs`, `ReferenceBookWindowController.cs`;
- `TimelineService.cs` code, `OfficeUIController.cs`, `DayFlowUIController.cs`, `LegendarySO.cs`, `NationEraProfileSO.cs`, `SaveSystem.cs`;
- `OfficeSceneUIBuilder.cs`, so the scene is not rebuilt.

### 2.9 Why some logic stays outside Domain

`CaseFactory.Disguise` and `LiarChance` are glue. They read ScriptableObject knobs (`CaseBlueprintSO`, `DayPlanSO`), sum effects through `TimelineEffects` (Assembly-CSharp), project `_todays` into `HomeCandidate`s at the call and read `_todays[HomeIndex]` back, and log warnings. Every decision they make is a call into tested Domain code: `Lies.MayLie`, `Lies.Plan`, `LiePlan.ApplyTo`, `Forgery.IsProvableTell`, `BirthDates.*`, `TravellerGenders.FromNameLists` and `VerdictRules.*`. The whole-day behaviour of this glue is proven by the Unity world check (§6).

The fallback's record block and the Records wiring warning (R15) are UI glue in `InvestigationUIController` over `CitizenRegistry.Find`, which `CitizenRegistryTests` covers. OfficeScene runs the rich desk, so the §6 play-through checks only that the warning stays silent there; the fallback block is not exercised by any automated check.

## 3. Retired or superseded

### 3.1 Code removed

- `CaseFactory.cs:264-304`: the forge chance, roll, provable list, target and wrong value.
- `CaseInstance.isForged` (55-56) and `CaseVerdict.wasForged` (`ShiftLedger.cs:130-131`), replaced by `IsLiar` and `wasLiar` (R4).
- `Forgery.IsProvable` (`Forgery.cs:9-33`), replaced by `IsProvableTell` (R5).
- `FactTable.HasOtherValue`, `PickOtherValue`, `OtherValues` (`FactTable.cs:99-133`).
- `BirthDates.Forge`, `AddYears` (`BirthDates.cs:63-106`).
- `CaseBlueprintSO.forgedBirthYearShift`, `ForgedBirthYearShift`, and the `OnValidate` clamp (`CaseBlueprintSO.cs:35-39, 72-73, 83-84`).
- Tests:
  - `FactTableTests.cs:20-29` (`FixedIndex`/`First`, used only below) and 68-103 (three `PickOtherValue` tests and `HasOtherValue_AnswersWithoutDrawing`);
  - `BirthDatesTests.cs:65-127` (six `Forge_*` tests);
  - `WeightedRandomTests.cs:5-13` (the private `FixedRolls` source), replaced by the shared `ScriptedRandom` (§5), so the suite keeps one scripted source;
  - all of `ForgeryTests.cs`, rewritten in §5.

### 3.2 Earlier spec lines superseded

`docs/superpowers/specs/2026-09-24-world-model-design.md` (piece 1, kept as the approved record):

- §1 line 19: "Forged fields take a random wrong value from today's other places … the forged value belongs to another place in the books." Only liars carry anachronisms. Each tell carries the true home's value, and the origin proof names the true home.
- §1 line 20: "A forged birth date shifts the year by 2–24 and never produces year 0." Replaced by the birth-date tell (a year from the true home's birth years that differs from the record, never year 0).
- §1 line 22: "Citizen Records show the same origin label." Still true, and now a rule (D3): records show the **claimed** label, including for liars.
- §2 line 35: `PickOtherValue(...)` is removed.
- §2 line 46: `BirthDates.… Forge …` is removed; `HasOtherYear`/`PickOtherYear` are added.
- §2 line 48: "gets provable and forged values … from FactTable". Tells come from `Lies.Plan` on the lie stream.
- §5 line 74: the `PickOtherValue` tests are removed. Line 78: "forge offsets" is replaced by the `PickOtherYear` cases.
- §6 line 87 (the forgery rule in Domain): replaced by `Forgery.IsProvableTell`. `HasOtherValue` and `PickOtherValue` are removed.
- §6 line 88 (forged birth dates inside the place's years; the 2–24 knob): removed.
- §6 line 90: "clue settings never change who is a forger". Now "who lies", and the lie stream is separate too.
- §6 line 98: `ForgeryTests` is rewritten, and the new `BirthDatesTests` forge cases are removed.
- §7 line 107 is the piece-1 verification record. Its "every forged fact …" checks are replaced for piece 2 by §6 of this spec.

`docs/superpowers/specs/2026-07-03-scanner-evidence-design.md`: "forger" (lines 9, 25-26) now means "liar", and the "genuinely anachronistic" field of rule 1 (14-19) is a liar's tell. The rules themselves are unchanged.

### 3.3 `docs/FEATURES.md` (same commit as the behaviour)

- :11: add "a per-traveller lie stream (`Seeds.ForLies`)" (tested: `SeedsTests`).
- :36: add "a liar's record is their cover identity (claimed origin); records never reveal a true home". With the R15 warning, also add "the rich desk warns once at start when the evidence system is active but Citizen Records is not wired (birth-date tells need it)".
- :43: "names come only from the claimed place's period names (a liar's name is part of their cover)".
- :44: rewrite to cover honest travellers vs liars, cover identity, tells carrying the true home's value, the per-day tell count, the liar chance knobs, and "violators and legendaries never lie". The test claim is scoped like :11: "(tell selection, eligibility and the may-lie exemptions tested: `LiesTests`, `ForgeryTests`; the liar chance, cover records and whole-day behaviour are checked in Unity, not by the EditMode suite)". `LiarChance`, `BuildRegistry`, the `tellCount` wiring and the call-site exemptions are Assembly-CSharp glue that no EditMode test reaches.
- :55-57: "forged field" becomes "tell". :56 adds "names the traveller's true home".
- :62: "Fallback text-mode investigation when the rich desk isn't built (papers, the traveller's agency record and today's books)".
- :83: "denying a liar with zero …" counts as a wrong decision (citation and stability loss; a deduction once the free warnings are used). The test claim is scoped like :44: "(the unproven-denial rule tested: `VerdictRulesTests`; the citation and deduction are Assembly-CSharp (`ShiftScoring`) and checked in Unity, not by the EditMode suite)". `VerdictRulesTests` covers only the `IsUnprovenDenial` decision; `ShiftScoring.ResolveDecision` and `ApplyWrongDecision` issue the citation, and no EditMode test reaches them.
- :84: add "(the gate exemption tested: `VerdictRulesTests`)": its forbidden-claim row is the directive exemption.
- :85: rewrite as "only provable tells are generated" (place facts need a book and a true-home value that differs from the claim's and belongs to no other of today's places; a birth-date tell keeps day and month and takes a year from the true home's birth years; names, capitals and rulers are never tells) (tested: `ForgeryTests`, `BirthDatesTests`).
- :87: "impacts land on the claimed place (where the traveller is sent), liar or not".
- :94: the generator also writes each day's tell count.
- New bullet: traveller gender is recorded from the claimed place's name lists and is not shown yet (tested: `TravellerGendersTests`, `NameRosterTests`).

## 4. Out of scope

- **Piece 3 (dialog + questions):** question and interview tells, capital and ruler question books, the dialog system and an answer surface in the intercom, and a way to log a failed answer as evidence.
- **Piece 4 (characters):**
  - clothing-layer tells (skin and hair follow the claim and are never tells);
  - the traveller sprite and passport photo;
  - premade and legendary characters lying (`LegendarySO` claimed vs true place);
  - the existing silent loss of a violator when a legendary lands on its slot (`CaseFactory.cs:149-152`).
- **Piece 5 (history changes facts):** history-dependent facts (tells re-read the changed `FactTable`), Future travellers, consequences of accepted liars for their true home, and "embargo evader" motives.
- **Piece 6 (PC/UI reacts):**
  - per-traveller report lines (`claimSummary` and `trueHomeLabel` shown);
  - a redirect-to-true-home outcome and place picker;
  - naming the true home on the citation slip;
  - UI language and colour changes;
  - deciding the fate of the legacy era-pick path (`GameManager.cs:369-377,395-463`, Test_DayLoop only). That path ignores lies, as it ignored forgeries.
- **Not planned:** a per-category tell weight knob, a same-era-only liar knob, and a validator check for days where no liar is possible (the runtime warning covers it).

## 5. Tests (EditMode, `Assets/Tests/EditMode`)

- **`ScriptedRandom`** (new shared helper, `ScriptedRandom.cs`, not a test class). It is built from an ordered script of steps. Each step is either a `Value` answer (a float) or a `Range` answer (an offset from `minInclusive`, clamped into the range). Each draw takes the next step. A draw of the other kind than the next step, or a draw past the end of the script, fails the test. `Draws` counts the draws made, and `Done` says whether the whole script was used. `LiesTests`, the `BirthDatesTests` draw tests and `WeightedRandomTests` use it. It replaces `WeightedRandomTests.FixedRolls` and the removed `FactTableTests.FixedIndex`, so the suite has one scripted source.
- **`ScriptedRandomTests`** (new). The R11 guarantee rests on the helper failing, so it is tested itself:
  - a `Range` draw on a `Value` step, and a `Value` draw on a `Range` step, throw `AssertionException`;
  - a draw past the end of the script (including an empty script) throws `AssertionException`;
  - `Range(10, 13)` answers 11 for offset 1, 12 for offset 5 (clamped) and 10 for offset −3 (clamped);
  - `Draws` and `Done` track the script step by step.
- **`LiesTests`** (new). The fixture's `todays` list is, in this order: the claim Egypt Ancient; Greece Ancient, a twin whose values equal Egypt's under `ValuesMatch` and whose birth years are 0..0; Iraq Ancient, which differs from Egypt in every book category and has a birth range holding other years than the cover year; and Italy Ancient, which differs from Egypt only in Currency and whose birth years are exactly the cover year, so BirthDate is not eligible for it. Iraq's values and Italy's Currency appear nowhere else in the fixture (no R13 collision). The papers follow the real templates: the passport prints Name, BirthDate, Currency and Language, and the permit prints Technology and Currency, so Currency is printed twice. Books cover Currency, Language and Technology. Iraq's eligible categories are therefore BirthDate, Currency, Language and Technology, in that order. Cases:
  - `MayLie` decision table: legendary, forbidden claim, null papers or empty papers → false; otherwise true;
  - a roll at or above the chance is `Honest` and the script `[Value ≥ chance]` is `Done` (exactly one draw); chance 0 is never a liar and chance 1 always is (over many seeds);
  - **golden order, place fact:** script `[Value 0 (hit), Range 0 (home), Range 1 (tell)]` gives `Liar` with `HomeIndex == 2`, so `todays[2]` is Iraq, although Iraq is the first *filtered* candidate (index 0) because R1 dropped the twin before it. `Tells == [Currency]`, every Currency field's value equals `facts.Get(todays[HomeIndex], Currency)`, and the script is `Done` (3 draws);
  - **golden order, birth date:** script `[Value 0, Range 0, Range 0, Range y]` (y ≥ 1, so the tell and year answers differ) gives `Tells == [BirthDate]` and a printed date with the cover's day and month and exactly the y-th eligible year of Iraq's range (ascending), and the script is `Done` (4 draws). Swapping any two draws, or adding one, fails these two tests (R11);
  - first-appearance order: with tell count 1, the tell indexes 1, 2 and 3 give Currency, Language and Technology. Currency counts once although the permit prints it again;
  - a tell count of 9 is capped at Iraq's 4 eligible categories, which are distinct (at most one Currency tell);
  - a tell count of 0 still gives one tell;
  - an unprinted category is never a tell (papers without Technology never get a Technology tell);
  - the true home is never the claim and never the twin (over many seeds);
  - with only the claim and the twin today, the result is `NoPossibleLie` after exactly one draw;
  - with only the claim and Italy today, the tells are exactly {Currency} (per-home eligibility);
  - **collision (R13):** a second fixture holds the claim plus two places that share one Currency value different from the claim's, and only Currency is printed. The result is `NoPossibleLie`. Adding a fourth place with its own Currency makes it the only candidate: `todays[HomeIndex]` is always that place;
  - the same seed gives the same plan;
  - `ApplyTo` rewrites **both** Currency fields with the home's value and flags them, leaving the other fields unchanged and unflagged;
  - `ApplyTo` for a BirthDate tell keeps day and month, takes a year inside the home range, and never uses the record year;
  - `ApplyTo` on an `Honest` or `NoPossibleLie` plan changes nothing;
  - round trip: every tell field registers in `DiscrepancyLog`. The claim's row gives `ClaimMismatch`. The home's row gives `ForeignOrigin`, with `actualOrigin` equal to the home label. Italy's Currency row, a third place, registers nothing. The record's cover date gives `RecordMismatch`.
- **`ForgeryTests`** (rewritten for `IsProvableTell`):
  - a place fact with a book and different values is a tell;
  - no book → not a tell (Geography);
  - values equal under the scanner comparison → not a tell;
  - the home or the claim lacks the fact today → not a tell;
  - the home's value is shared with a third place today (under `ValuesMatch`) → not a tell (R13); the home's own row never counts as a collision;
  - BirthDate is a tell when the home range has another year, and not when the cover date is unreadable ("Unknown"), the home is 0..0, or the home range is only the cover year;
  - Name is never a tell;
  - a missing table or missing books → not a tell.
- **`BirthDatesTests`** (the `Forge` cases are removed; new cases):
  - `PickOtherYear` keeps day and month and stays in range;
  - it never takes the cover year when the ranges overlap (1630..1682), and still yields many different years;
  - it never gives year 0 across the BCE/CE boundary;
  - a single remaining candidate is always chosen;
  - reversed bounds are tolerated;
  - it uses exactly one draw (a one-step `ScriptedRandom` is `Done`), and that draw picks among the eligible years in ascending order;
  - it returns null when `HasOtherYear` is false, with no draw (`Draws == 0`);
  - `HasOtherYear` decision table (`[TestCase]`s): unreadable → false; 0..0 → false; {cover} → false; {0, cover} → false; a wider range → true; BCE → true.
- **`FactTableTests`**: the `PickOtherValue` and `HasOtherValue` tests and the `FixedIndex` helper are removed. The two `DiscrepancyLog` round-trip tests stay; the origin-proof one also asserts `actualOrigin == "Babylonia (Ancient)"`.
- **`SeedsTests`**:
  - new `LieStream_IsDistinctFromCaseClueAndViolatorStreams`: for cases 1..20, `ForLies(ForCase(d, c))` is not a case seed and differs from `ForClues` of the same case and from `ForViolators(d)`;
  - it is deterministic and differs per case.
- **`VerdictRulesTests`** (new):
  - `ShouldAccept`: all 4 rows;
  - `IsUnprovenDenial`: the one true row, plus each single flip giving false (gate off, evidence −1, evidence 1, accepted, honest, forbidden claim).
- **`TravellerGendersTests`** (new):
  - male → Male; female → Female; "Marcus II" → Male; "Anna Maria" in the female list → Female;
  - different case → matched;
  - a name in both lists → Unknown; "Subject #3" → Unknown; null or blank → Unknown;
  - a null list counts as empty: `FromNameLists("Marcus", null, femaleList)` → Unknown, and a name in `femaleList` with a null male list → Female.
- **`NameRosterTests`**:
  - `BaseName` `[TestCase]`s: "Marcus II" → "Marcus"; "Marcus XIV" → "Marcus"; "Marcus" unchanged; "Anna Maria" unchanged; "Mary Ann" unchanged; "Marcus I" unchanged (`Take` never adds I); "Marcus iv" unchanged; "Marcus IIII" and "Marcus MMMM" unchanged (`Roman` never writes them);
  - every suffix `Roman` writes for 2..3999 is stripped;
  - every name `Take` hands out after the pool is exhausted maps back to a pool name.
- **`WeightedRandomTests`**: `FixedRolls` is replaced by `ScriptedRandom` with the same rolls; the assertions are unchanged.
- **`DiscrepancyLogTests`**: vocabulary only (§2.8). The helpers become `TellDocField` and `TellIdentityField`, and `ForgedBirthDate_VsRecord_Registers_AsRecordMismatch` becomes `BirthDateTell_VsRecord_Registers_AsRecordMismatch`. Every assertion and the test count stay the same.
- **Unchanged and green:** `CitizenRegistryTests`, `ShiftLedgerTests`, `ViolatorSlotsTests`, `OriginLabelsTests`, `SeededRandomTests`, and the rest.

## 6. Verification plan

**Offline, after every change:**
- `compile_check.py` reports 0 errors on every project;
- the reflection runner reports all Domain tests passing.

**In the branch's own Unity 6000.4.11f1**, through temporary `_TimeDesk*` `-executeMethod` scripts (not committed; reports go to the scratchpad):

1. **Baseline, before any code change** (at `14bd22e`): dump seeds 12345 and 999, days 1–3, one line per slot: claim label, given name, registered birth date, archetype, `claimAllowed`, and whether the slot is a planned violator.
2. **Content:**
   - Generate World twice. The second run changes no file. The day plans gain `tellCount: 1`, and the blueprint loses `forgedBirthYearShift`.
   - Validate Content Library reports no issues.
3. **World check** on seeds 12345 and 999 × days 1–3, plus a sweep of 200 seeds for coverage. Cases come from `new CaseFactory(lib, lib.BuildFactTable(plan)).GenerateDayCases(plan, state, Seeds.Day(seed, day))`. The script must prove:
   1. **Determinism:** generating twice with the same seed gives identical dumps, including liar flags, homes, tells and every paper value; a different seed differs.
   2. **Stream contract:** claims, names, registered dates, archetypes and violator slots equal the baseline dump line for line.
   3. **Liars:** the true home is in today's places and is not the claim. At least one field has `isAnachronism`. The number of tell categories is `min(tellCount, eligible)`, which is 1 on days 1–3. Every field of a tell category is rewritten.
   4. **Tell values come from the true home:** a place-fact tell equals `facts.Get(trueHome, category)`. A BirthDate tell has the day and month of the record and a year inside the true home's range that is not the record's year.
   5. **Registrable:** every liar has at least one tell that `DiscrepancyLog.TryRegister` accepts, using the day's real `FactRow.ToEvidence()` rows and the registry's record. A place-fact tell gives `ClaimMismatch` against the claim's row and `ForeignOrigin` naming the true home against the home's row (`actualOrigin == trueHomeLabel`), and every other row of that category registers nothing (R13). A BirthDate tell gives `RecordMismatch`.
   6. **Honest travellers are fully consistent:** every field equals the claim's `FactTable` value, the registered name or the registered date; no field is flagged; `ShouldAccept == claimAllowedByRules`.
   7. **Violators are honest:** planned violators and every traveller with `claimAllowedByRules == false` are not liars. Legendaries are honest too (none on days 1–3).
   8. **Records show the cover:** `BuildRegistry(cases).Find(name)` gives name = `visitorGivenName`, birth date = the registered date, and origin = the claim label, never the true home's label for a liar.
   9. **Gender:** every generated traveller is Male or Female and matches the claimed place's list holding the (suffix-stripped) name. Unknown occurs 0 times on days 1–3.
   10. **Coverage** (sweep): the liar rate is reported and close to 0.5 among eligible travellers. Each tell kind (Currency, Language, Technology, BirthDate) occurs. Cross-era liars occur on days 2–3. No `NoPossibleLie` warning occurs.
4. **EditMode suite** through `TestRunnerApi`: everything passes except the known third-party `UnitySkills…SceneSummarize_CountsObjectsCorrectly` (reported, ignored).
5. **Scripted day-1 play-through** on the committed OfficeScene (play mode, `[InitializeOnLoad]` + SessionState steps, as in the piece-1 smoke). The script first picks a run seed whose day-1 queue holds an honest traveller, a liar with a place-fact tell and a liar with a BirthDate tell among the first slots, using the same `CaseFactory` call. It then drives the real UI objects:
   - an honest traveller → Accept → correct, no citation;
   - a place-fact liar → request the document, compare the tell field with the **true home's** book row → the scanner logs "… which belongs to <true home>" → Deny → correct, no citation;
   - a BirthDate liar → open the Citizen Records app (the builder creates it closed, and opening it is what wires its rows) → search for the cover name → the record shows the claimed origin and the cover date → compare Born with the passport's Date of Birth → "BIRTH DATE INCORRECT" is logged → Deny → correct;
   - one liar denied with no evidence → the unproven-denial citation;
   - forcing closing time → the results panel shows "Undocumented denials: 1";
   - no warnings or errors in the log.
6. **Hygiene:** the builder is unchanged, so the scene is not rebuilt. Revert Unity-touched unrelated files (`*.csproj`, `ProjectSettings/*`, the TMP fallback atlas, OfficeScene). Delete the `_TimeDesk*` files and their metas. Commit the regenerated day plans and the re-saved blueprint.

## 7. Risks

- **Changed cheaters for old seeds.** Moving the forge draws off the case stream means piece-1 seeds get different liars than they had forgers. This is harmless: nothing is shipped, cases are not saved, and step 2 of the §6 world check proves every other draw is unchanged.
- **BirthDate tells don't name the home.** No book maps a birth year to a place, so these tells prove a lie (record proof) but never say where the liar is from. Place-fact tells do. This is accepted per D4. A birth-year or era calendar surface belongs to a later piece.
- **Records-only tells depend on the Records app.** `EvidenceSystemActive` (`InvestigationUIController.cs:73`) does not check that the Records window is wired. In a scene without it, a liar whose only tell is BirthDate can only be denied through an unproven denial. R15 makes this loud (a warning naming the builder) rather than fixing it, and the text fallback prints the record. OfficeScene wires Records, and play-through step 5 exercises it.
- **Shared values shrink the tell pool.** R13 keeps a shared home value from ever being a tell, so collisions cannot mislead the origin proof, but each one removes a category for that home. Heavy history edits in piece 5 could push a day towards the `NoPossibleLie` warning. The ruler "Sultan Mustafa II" (Egypt and Greece Early modern) is shared too, but rulers have no book, so they are never tells in piece 2.
- **Cross-piece gate trap.** When pieces 3 and 4 add question and clothing tells, a liar whose only tell is not on paper could not be logged (`DiscrepancyLog.cs:181-194`). Those pieces must either add registration paths first or keep at least one paper tell per liar.
- **One knob, two meanings.** `contradictionChance` is both the liar chance and the legacy per-clue contradiction chance (D6). Its docs say so. Retiring the legacy clue path, which has an empty library today, would remove the second meaning.
- **Misleading field names.** Per D1, `trueEra`, `trueBirthDate` and `nation` now mean the claim or cover. Their docs are fixed but the names stay. A later rename touches about 15 call sites.
- **Gender is data only.** Its only readers in piece 2 are the logs and the verification script, and a reviewer may call it speculative. D10 records why it lands now: pieces 3 and 4 need it, and deriving it later would need the same code.
- **Stacked branches.** Piece 2 sits on the unmerged pieces 1 and 0, and FEATURES and spec edits can conflict on rebase. Never commit in `E:\unity\NOPE`.
- **Existing UX gotcha.** The banner shows "Name (Role)" while records are keyed by the bare name, so typing the banner text verbatim gives NO RECORD (`CitizenRegistry.cs:53-69`). The BirthDate tell makes Records matter more. This is not fixed here and is noted for piece 6.

## Review notes

An independent review of this spec (2026-09-24) raised 18 findings. Each was checked against the code at `14bd22e` and `world_source.json`, and all were applied; none was rejected. Where a finding offered a choice, this spec takes the following:

- `Disguise` returns the `LiePlan` (null when exempt) and takes `caseIndex1Based`, so `LiePlan.Tells` stays public with the case log as its caller.
- R8 and §1.5 now name only the case logs. Gender is not added to the `[Result]` log, because the case-start log already prints it.
- Italy's fixture birth years are exactly the cover year, which also exercises the {cover} row of `HasOtherYear` inside `Plan`.
- The text fallback prints the agency record (R15) instead of documenting birth-date-only liars as undetectable there. The Records wiring warning is added as well.
- `ScriptedRandom` also replaces `WeightedRandomTests.FixedRolls`, the optional part of that finding, so the suite keeps one scripted source.
- `MayLie` takes the field list too (R6), like `Plan` (R3).
- The collision rule, the label source and the fallback/warning are new rows R13–R15, so the existing R numbers stay stable.

The review of the implementation plan (2026-09-24) also corrected this spec; the plan's own "Review notes" list every finding:

- `DiscrepancyLog.cs`, `CitizenRegistry.cs` and `InvestigationUIController.cs` are CRLF at `14bd22e`, not LF (§2.3, §2.7).
- §3.3 now records the Records wiring warning (R15) in the Citizen Records line of `FEATURES.md`.
- `ScriptedRandomTests` pins the helper that R11's guarantee rests on (§5, §2.8).
- The §2.8 "forg" claim was false for `DiscrepancyLogTests`, which the grep had found but the table missed. Its helpers, one test name, the class doc and one comment move to tell vocabulary (§2.8, §5), so the claim now holds, tests included.
- The §6 play-through opens the Citizen Records app before searching, because its rows are wired only when the app first opens.
