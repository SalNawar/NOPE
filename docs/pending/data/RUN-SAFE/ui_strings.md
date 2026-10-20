# RUN-SAFE: UI strings for the save notice (data fragment)

For the owner of `Assets/Data/World/world_source.json` `ui.strings` (CONTENT in Phase A; INTEGRATION as data steward in Phase B). Add the four keys to the reading table (English below), give the six culture tables their translations, run Tools > TimeDesk > Generate World, and add the keys to `ContentSheetMap` if the map lists UI strings one by one.

Until they are in the tables, `SaveNotice` shows the English below (its fallback), and `UiText` logs one "No UI string" warning per key the first time a notice shows.

| Key | English | When |
|---|---|---|
| `save.notice.writeFailed` | RECORDS: today's file could not be saved (is the disk full or read-only?). Your run goes on; the last good save stands. | A save could not be written (`SaveProblem.WriteFailed`). |
| `save.notice.unreadable` | RECORDS: a damaged save was found. It is kept beside your saves, untouched; this run starts fresh. | The save could not be read and was set aside (`SaveProblem.Unreadable`). |
| `save.notice.tooOld` | RECORDS: a save from an older build was found. It is kept beside your saves; this build starts a new run. | The save predates the save format and was set aside (`SaveProblem.TooOld`). |
| `save.notice.recovered` | RECORDS: your last save could not be read, so the run was restored from its backup. | The run came back from the temp file or the backup (`SaveProblem.Recovered`). |

The "RECORDS:" lead frames the notice as the Bureau's records office (proposal, ours); the copy is ours too and may be rewritten freely, the keys may not.
