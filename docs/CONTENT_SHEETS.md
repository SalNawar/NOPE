# Content sheets: the spreadsheet path into world_source.json

Saleh's plan: "we will create an excel file for all cases later with all possible dialogue options, all possible dialogue interactions, all real characters, all premade characters and stories etc". This is that path (plan phase 26; traveller-types spec CS1 and §15, PC spec CT1).

## The pipeline

```
ContentSheets/TimeDesk_Content.xlsx  (or ContentSheets/csv/*.csv, one UTF-8 file per sheet)
        |  Tools > TimeDesk > Import Content Spreadsheet   (or Import Content Sheets (CSV))
        v
Assets/Data/World/world_source.json  (same layout: 2-space indent, its own line endings)
        |  Generate World (run by the import), then its validator
        v
the generated world assets
```

- **Export Content Spreadsheet** writes today's `world_source.json` to `ContentSheets/TimeDesk_Content.xlsx`: a README sheet, then one sheet per table. Start from this, so the workbook holds what exists.
- **Export Content Sheets (CSV)** writes the same as `ContentSheets/csv/<sheet>.csv` (UTF-8 with a byte-order mark, so spreadsheets open it as UTF-8).
- **Write Content Spreadsheet Template** writes `ContentSheets/TimeDesk_Content_Template.xlsx`: every sheet with two example rows per table (and their child rows), taken from today's content.
- **Import Content Spreadsheet** reads the workbook, writes `world_source.json`, then runs **Generate World**. It writes nothing if any check fails.
- The .xlsx is read and written with `System.IO.Compression` and `System.Xml` only (an .xlsx is a zip of XML parts). There is no third-party package. A workbook re-saved by Excel imports back byte for byte, and so do sheets saved by Excel as "CSV UTF-8".

## Row rules (every sheet)

- One record per row, one table per sheet, one field per column. Headers are the JSON field names. A dotted header (`culture.language`) is a field of a nested object.
- A **child sheet** (a place's facts, a dialog's nodes, a node's lines) starts with one column per parent level that names its parent row: `place` (a place is named `{country}_{era}`, for example `egypt_ancient`), `dialog`, `node`, `choice`, and so on. Its rows can sit anywhere in the sheet; within one parent they keep their order.
- **Lists** go in one cell with `|` between the items (`Papers|Answer`, a place's names). Long sentences (small talk, asset lists) get their own one-column sheet instead.
- **A blank cell** takes the column's documented default (the README's "blank means" column). An empty text, 0 or false, or an empty list. Some columns are left out of the JSON when they hold their default, and the README says which.
- **Numbers**: a whole-number column takes `12`. A decimal column writes `1` as `1.0`. A "number" column keeps what you type: `1` stays whole and `0.5` stays a decimal. Use a dot for decimals.
- **true/false** in any case (`TRUE` from Excel is fine).

## What the import checks

Each problem names its sheet, row and column (for example `places: row 12, column E (year): 'abc' is not a whole number.`). If there is any problem, nothing is written.

- An unknown column (with a "did you mean" hint), a missing column, a column appearing twice, or values under a blank header.
- A missing sheet, or an unknown sheet. The README sheet is skipped.
- A cell of the wrong type, a blank required cell, or a value outside its allowed set (a wardrobe `gender` is `m` or `f`, a `slot` is `outfit|hair|facialHair|headwear|accessory`).
- A duplicate row name (within its parent), a child row whose parent row does not exist, or a name no row of the referred sheet has (a premade's `place`, a day's `premades`, a dialog line's `dialog`).
- A one-row sheet (`world`, `interview`, `ui`, ...) with more rows.
- The current `world_source.json` holds content the map does not cover. The import refuses, so it never drops a section another change added.

Generate World and its validator then run their own checks, which cover the game's rules.

## The map (how a new section gets a sheet)

Everything is driven by one declarative map, `Assets/Scripts/Domain/ContentSheets/ContentSheetMap.cs`. It lists one entry per sheet and one per column, in the JSON's own order, with each column's type and rules as data. The engine (`ContentSheets.cs`) has no per-table code, so a new JSON section is a new map entry:

| The JSON gains | The map entry |
|---|---|
| a field of an existing record | `Text("kinds")`, `Int("weight")`, `Num("weight")`, `Float("chance")`, `Bool("honest")`, `List("kinds")` (`a\|b` in a cell), placed where the JSON orders it; `.Omit()` if the JSON leaves it out at its default, `.Required()`, `.OneOf(...)`, `.Ref("sheet")`, `.Note("...")` for the README |
| a list of records | `Rows("dayKinds", "kinds", columns...)` under its parent sheet; `Rows(name, path, Key("id", "childHeader"), ...)` when rows have child sheets or other sheets name them |
| a record keyed by name (`{ "m": {...}, "f": {...} }`) | `Keyed("sheet", "path", Text("keyColumn").OneOf(...), columns...)` |
| a list of sentences | `Values("sheet", "path", Text("text"))` |
| a nested object | dotted column paths (`"agency.name"`), or `Single("sheet", "path", ...)` under a one-row sheet |
| a list or object the JSON may leave out when empty | `.OmitEmpty()` on the sheet |

`ContentSheetMapTests` exports today's `world_source.json` and imports it back. It fails, naming the JSON path, when the source holds a key that no map entry covers, or when the round trip is not byte-identical. The template and the README sheet are generated from the map, so they follow it.

## The sheets (today)

The README sheet of any export is the live list, with every column's type, default and rules. Today there are 100 sheets. The story tables:

- **Characters and stories:** `premades` (+ `premadeImpacts`; a 2150 story character's `kind`, `family`, `citizenId`, `debt` and `employer`, days 7-15, blank for the famous); `places` (+ `placeFacts`, `placeSmallTalk`, `wardrobeSets`, `wardrobe`, `placeHair`); `present` (the neutral present, 2150: + `presentFacts`; its clothes, `presentWardrobeSets`, `presentWardrobe`, and its 2150 accessory kit, `presentKit`, for costume errors); `countries`; `eras` (+ `eraSmallTalk`).
- **Dialogue and interactions:** `dialogs`, `dialogConditions`, `dialogNodes`, `dialogLines`, `dialogChoices`, `choiceLines`; `questions` (one per category, asked of every traveller in the same words), `questionConditions`, `questionOverrides` (an answer for some kinds, a claimed era or both: the most specific wins); `interview` (+ the small-talk weights), `claims` (the claim per traveller kind), `interviewRequests`; the voices (the personalities spec's §9): `personalities` (the cast: id, name, weight, a note for authors), `kindSmallTalk` (the kinds' small talk), and one sheet per slot naming a `personality` or a `premade` (exactly one), the slot's key, `kinds` and `era` (blank: any) and the line: `voiceClaims`, `voiceHandOver` (`request`, blank: any), `voiceMissingForms` (`request`, `variant`), `voiceSpoken` (`request`), `voiceAnswers` (`question`), `voiceSmallTalk`, `voiceReactions` (`verdict`, `intent`, `reason`, `then`) and `voiceSlips` (`lie`); the defaults `interviewReactions` (the four verdict x intent rows with a blank reason, kinds and era are required) and `interviewSlips` (one with a blank lie is required); `days.slipChance` (a liar's chance of a slip after small talk). A voice with no matching row says the defaults, so a line is added one row at a time; Generate World logs each personality's fallbacks.
- **The days and history:** `days`, `dayKinds` (each day's traveller mix), `dayEras`, `dayForced` (a slot's appearance: its premade or blueprint, and from days 7-15 its `id`, an authored `lie` or `directive` fault, its `dialog` and `intro`; a slot may list alternatives, named by `forced` = `{slot}{id}` in the child sheet) with `dayForcedConditions` (when an appearance stands: the gate conditions, left out when a slot has none), `dayPortals` (the Directorate's route for each departure portal that day: `portal`, `country`, `era`, a place of the day's world, distinct, portal 01's open; a route a closure forbids for every traveller shows CLOSED on the board and in the Portals app), `rules`; `history`, `historyRules` (a rule with no edit is a story rule, which prints its news line; `stability` moves stability the night it fires), `historyConditions`, `historyEdits`.
- **The PC:** the steps checklist's sets, `pcStepSets` (a set per traveller kind and `default`: the set it starts from, its data-only mark) with `pcSteps` (each set's steps: what each waits for, what it counts, where a click goes; its label is the UI string `steps.{id}`); `pc` (whether the premades get Lineage Archive cards), `pcSites`, `pcPages` (+ `pcPageBlocks`: a Static site's authored pages), `pcPeople` and `pcRelations` (the Lineage Archive's people of the past), and Mail's authored messages, `pcMail` (+ `pcMailBody`: each message's paragraphs).
- **Home:** `home` (which house upgrade is the radio), `homeRadio` (the radio's lines, one a night in order) and `homeUpgrades` (the House tree, the Home upgrades spec: each upgrade's category, one-off `cost`, nightly `upkeep`, `requires` (other rows' ids, `a|b`), its household effects `householdExpense`, `sicknessChance`, `careCost`, `medicalDrain`, `mood`, `breakInChance`, `breakInShare`, and its `blurb`). Generate World writes one upgrade and one effect per row into `Assets/Data/World/Home`.
- **Presentation (rarely edited):** `world`, `agency` (+ `agencyAccounts`, `agencyStatuses` and `agencyTransponders`: the ranges and transponder models 2150 citizens' accounts are drawn from; `agencyPortals`: the hall's five portals, each ring's `number`, `name`, `role` (Departures or Returns, the Return Gate), `fromDay` (the day it enters service by itself; blank: only by its repair) and `repair` (the upgrade whose delivery puts it in service)), `looks`, `faceBands`, `confusable`, `content` and its asset lists (+ `contentBlueprints`, each traveller kind's case blueprint); the country `culture` blocks (`countryFonts`, `countrySeeds`, `countryOverrides`, `countryArt`); `ui`, `uiPalette`, `uiNeutral` (+ its four sheets), `uiStrings`, `uiLanguages`, `uiLanguageEntries`; `translation` (with the key words that stay English), `scripts`, `scriptFonts`, `packs`, `tongues`.
