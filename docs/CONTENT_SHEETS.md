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

The README sheet of any export is the live list, with every column's type, default and rules. Today there are 60 sheets. The story tables:

- **Characters and stories:** `premades` (+ `premadeImpacts`); `places` (+ `placeFacts`, `placeSmallTalk`, `wardrobeSets`, `wardrobe`, `placeHair`); `countries`; `eras` (+ `eraSmallTalk`).
- **Dialogue and interactions:** `dialogs`, `dialogConditions`, `dialogNodes`, `dialogLines`, `dialogChoices`, `choiceLines`; `questions`, `questionConditions`, `questionOverrides`; `interview`, `interviewRequests`.
- **The days and history:** `days`, `dayEras`, `dayForced`, `rules`; `history`, `historyRules`, `historyConditions`, `historyEdits`.
- **Presentation (rarely edited):** `world`, `agency`, `looks`, `faceBands`, `confusable`, `content` and its asset lists; the country `culture` blocks (`countryFonts`, `countrySeeds`, `countryOverrides`, `countryArt`); `ui`, `uiPalette`, `uiNeutral` (+ its four sheets), `uiStrings`, `uiLanguages`, `uiLanguageEntries`; `translation` (with the key words that stay English), `scripts`, `scriptFonts`, `packs`, `tongues`.
