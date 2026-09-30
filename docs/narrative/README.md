# The narrative workbook

Saleh (2026-09-30): "I want you to create a narrative excel tool. the tool should track all cases on all days, if they are generated or not, the lines they use, if a case has a narrative I can write the lines, when do they trigger etc."

## Use it

1. In Unity: **Time Sorter > Narrative Workbook > Export**. It writes `ContentSheets/NarrativeWorkbook.xlsx` and opens it (it asks before replacing one: edits since the last import would be lost).
2. Edit the **yellow** cells (Narrative's `text` column; Lines' yellow columns). Save.
3. In Unity: **Time Sorter > Narrative Workbook > Import**. It writes your edits into `Assets/Data/World/world_source.json`, runs Generate World and the validator, and writes `ContentSheets/NarrativeWorkbook_import.txt` (every change and every problem; Explorer opens on it).

**Time Sorter > Narrative Workbook > Show in Explorer** finds the file. The job runner and batch mode call `NarrativeWorkbookMenu.ExportBatch` and `NarrativeWorkbookMenu.ImportBatch` (no questions asked).

## The sheets

| Sheet | Editable | What it holds |
|---|---|---|
| README | no | How it works, the colours, how the reference run was played. |
| Days | no | Days 1-15: the queue, the authored slots (forced appearances with their fault, dialog and conditions; alternatives with "else"), how many slots are generated, the premade pool and its chance, the kind and era mix, lies, rules and closures, portals, questions, dialogs and mail that start that day, the story beats that can fire that night, and what the reference run met. |
| Cases | no | Every traveller of the reference run, day by day and slot by slot: generated or authored (forced slot or premade pool), name, kind, personality, claimed and true home, lie, fault, the correct call, and every line: opener, claim, small talk, slip, each answer, each paper request (hand-over, refusal, the waiver pad), each spoken request, the reaction to Accepted and to Denied, the dialogs offered. Built by the game's own code (below). |
| Narrative | `text` | One block per story character (by first appearance: Pell, Ines, Rook, Ada, Hollis, the auditor, ...), then the other premades (the famous: He Zehui, Senenmut, ...), the dialogs no character owns (the rumour), Strandings, Mail, the paper and the radio, and the world's story beats. A block starts with a blue banner saying when the character appears; then one row per editable cell: the premade's name, intro and record note; each appearance's intro, dialog, lie, directive and conditions; each dialog's label, conditions, lines (with the speaker), choices (label, choice lines, next node, effect with what the effect does); the character's voice lines; each story beat's name, news line, section, stability change, conditions and world pulls; the premade's world pulls. |
| Lines | yellow columns | Every personality line and every default line, by voice slot (`voiceClaims` ... `voiceWaiverPad`, then the defaults `claims`, `kindSmallTalk`, `missingFormReplies`, `interviewReactions`, `interviewSlips`, `waiverPadReplies`). `pool` says which of N lines one pick chooses among. The premades' own voice lines are in their Narrative block. |
| Triggers | no | Every appearance, premade pool, dialog, story beat, question and mail: its day, its conditions in words, the player choices it depends on ("the clerk's stamp on Pell Quimby", "choosing 'Take the 200 cr.' in dlg_rook"), what it does, and when the reference run saw it (a beat's night, a dialog's days). |
| Lists | hidden | The drop-downs' values. |

Colours: yellow cells with amber headers are editable and the only cells the import reads; grey cells are views; blue bands start a block; shaded cells do not apply to their row. The sheets are protected without a password (Review > Unprotect Sheet), so filters, sorting, resizing and inserting rows still work. Sorting or filtering never breaks the import: each row carries its own binding.

## Editing rules

- **Change a line**: type over the yellow cell. Keep the tokens: `{place}` (the claimed place; required in claims), `{value}` (required in answers), `{document}` (the paper asked for), and the ones each note names.
- **Add a voice line**: in Lines, type a new row below the data: `slot`, `personality` or `premade` (exactly one; both blank for a default slot), the slot's keys (`request`, `variant`, `question`, `verdict`, `intent`, `reason`, `lie`, `reply`; `kind` for `claims` and `missingFormReplies`), `kinds` and `era` (blank: any), `text` (and `then` for a reaction). It is added after that voice's last line of the slot.
- **Remove a voice line**: choose `remove` in its `remove` cell. Deleting a row from the workbook deletes nothing.
- **New structure** (a premade, a dialog, a node, a choice, a forced slot, a story beat): the content spreadsheet (Tools > TimeDesk > Export Content Spreadsheet, `docs/CONTENT_SHEETS.md`), the same source and the same import. Then export this workbook again.
- The waiver form's printed promise and Time Police clause are shown read-only in the Strandings block: they live in the form template (`Assets/Data/Investigation/DocTemplate_TC310.asset`), edited in its Inspector.

## What the import checks

It writes nothing when any of these holds; the report names the workbook sheet and row:

- text in a Narrative row without a `ref` (a new row there is not imported);
- a damaged `ref` or hidden `was` cell;
- a **conflict**: the author changed a cell whose content row changed in `world_source.json` since the export (another change landed). Export again and redo the edit. A row whose source moved on but that the author did not edit is skipped quietly (the report counts it);
- two rows editing the same cell differently;
- a changed `slot` on an existing line, or a value in a column the slot does not have;
- anything the content spreadsheet's import refuses (types, allowed values, names of other sheets' rows, required cells), with the workbook row the bad cell was typed in;
- anything Generate World refuses: `world_source.json` is then restored as it was.

With no edits, an import changes 0 bytes of `world_source.json` and Generate World does not run.

## How it is built (decisions)

- **One source, one format.** The workbook is a view of the content spreadsheet's own tables: `ContentSheets.Export` of `world_source.json` through `ContentSheetMap` (the same map, sheets, columns, rules and import path; `ContentSheetsMenu.Tables` and `ContentSheetsMenu.ImportTables`). Each editable row names its content table and row (`ref`: `dialogLines#57:text`, the content workbook's own row number and column) and keeps a hidden copy of that row as exported (`was`). The import finds the row at its number, or else as the one row that still equals the copy (a row that moved because another change inserted rows above it still takes the edit), writes only the cells that differ from the copy, and hands the tables to the content spreadsheet's import. Domain: `NarrativeWorkbook` (the sheets), `NarrativeImport` (the binding), `SheetLook` (the look); tests: `NarrativeWorkbookTests` (offline-runnable).
- **The same .xlsx writer.** No new package: `Xlsx` (System.IO.Compression and System.Xml) gained styled sheets (`SheetLook`: Arial, fills, locked and unlocked cells, protection, filters, frozen panes, widths, hidden columns and sheets, drop-down lists). A workbook of plain tables (the content spreadsheet, its template) is written exactly as before. Python (openpyxl) was not used: the project's tooling for content is C#, and the workbook must be exported from the game's own code.
- **The game's own lines.** The Cases sheet plays the reference run through the balance simulation's steps (`BalanceSimulation.Observe` over `DayCycle`, with a `RunObserver`), so the travellers are `CaseFactory`'s and every line comes from `InterviewScript.Build` and `InterviewScript.Reaction` over the case as the interview reads it (`InterviewPresenter.CaseFor`, `CaseDocumentsPresenter.DocumentOf`, both shared with the game, not copied). The run seeds (12345, the smoke play's and the simulation's example seed, then the simulation's first two run seeds, 7919 and 15838) and the play style (Perfect: the correct call every time, so the story follows a careful player's path) are Inspector knobs, `Assets/Data/Config/NarrativeWorkbookSettings.asset`.
- **Where the file lives.** `ContentSheets/NarrativeWorkbook.xlsx`, next to the content spreadsheet, opened by the export. It is not committed (`.gitignore`): it is derived, and goes stale the moment the source changes; `world_source.json` is the truth. The import report sits beside it, also not committed.
- **Which cells are editable.** The words (lines, labels, news, notes, intros, subjects, paragraphs), the story's wiring the author tunes (dialogs, next nodes, effects, authored lies and directives, condition types, keys and thresholds, world pulls, stranding weights) and every column of a voice line. Ids and parent names stay locked: renaming them changes the structure (the content spreadsheet does that).
