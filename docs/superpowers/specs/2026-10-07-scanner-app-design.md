# The scanner and its PC app: convenience, utility and the citizen file (2026-10-07)

**What Saleh asked for (2026-10-07):**
- On the scanner: "I want more utility to the scanner, it should be more convenient … I mean the PC app."
- He chose:
  - auto record lookup;
  - a dates and rules check;
  - a cross-check table;
  - a layer switch;
  - overlay compare;
  - seen-before history.
- He added: "In citizen lookup we need to have more lore and storytelling. We should have small snippets about the person. This will help us learn about historical figures, but also tell stories with randoms."

This spec replaces the scanner's *interaction* in the desk-machine spec (`2026-10-07-desk-machine-design.md` §5): the lid and handle are dropped. The hidden layer and the chip from that section stay.

**Saleh's** lines are his rules. **Decision** lines are the orchestrator's and can be overruled.

## 1. The scanner on the desk: drop and go

- Drop a paper, or several, on the glass and it scans by itself.
  - A fast glowing sweep of about 0.8 s per paper, with the whirr and a done beep.
  - A stack feeds through one paper at a time.
- No lid, no handle, no button.
- Auto-Feed (an upgrade) still pulls handed-over papers in on its own.
- Analysis Scanner (an upgrade) now makes sweeps faster and flags the first fault automatically (spec §5 of desk-machine).
- **Decision:** scanning stays optional. Everything the app shows can still be found by hand, just more slowly.

## 2. The app: Investigation > Papers, rebuilt around a scan

When a scan lands, the app opens to that traveller's **case board**: every scanned paper plus their citizen file.

1. **Auto record lookup.**
   - Scanning any paper that carries a citizen ID or name opens the matching citizen record. No typing.
   - The scanned fields are pre-linked to the record's rows (the existing smart links).
   - No match gives a stamp-like "NO RECORD" plate. That is itself a finding.
2. **Rules check.**
   - A panel lists today's rules, one row each, with the paper's relevant value and a verdict chip: `VALID` / `EXPIRED` / `CLOSED` / `MISSING`.
   - Examples: the passport's VALID UNTIL against today's date; the destination against today's open portals; a required form present.
   - Clicking a failing row logs it as evidence.
   - **Decision:** it checks dates, open destinations and required papers only. Lies, costume errors and forgeries stay the player's job.
3. **Cross-check table.**
   - One table with a row per shared field (name, citizen ID, birth date, destination, era, dates) and a column per source (each scanned paper, plus the record).
   - Cells that disagree glow; clicking one logs the discrepancy as evidence.
   - It replaces opening papers two at a time. Click-compare stays available.
4. **Layer switch.**
   - Each scan has tabs `PRINT` / `UV` / `CHIP` with a wipe transition between them.
   - UV shows the hidden layer: watermark, the ghost of an erased value, microprint.
   - CHIP shows the chip's stored data in the same layout as the print, so differences line up.
5. **Overlay compare.**
   - Drag one scan or photo onto another and a fade slider blends them.
   - Differences shimmer: the passport photo against the record photo, a seal against the Books seal, a signature against the record's.
   - **Decision:** shimmer marks only areas that really differ in the data, so it never gives a false alarm.
6. **Seen-before history.**
   - The record shows past visits: dates, destinations, past verdicts, past citations against this person.
   - A recurring face carries a "DENIED 3 DAYS AGO" style flag.
   - **Decision:** history covers this run only. Famous people also list their historical life events in the file (below).

**Feel:**
- the case board slides in;
- table cells pop as they fill;
- each glow comes with a soft tick;
- logging evidence plays a pin-to-corkboard thunk.

All of it runs on Track J's springs and sound cues.

## 3. The citizen file: lore and storytelling (Saleh's request)

Every citizen record gets a **FILE** section of 2-4 short snippets, one line each, written as registry and clerk notes.

- **Famous travellers (historical figures).**
  - Authored snippets: real, accurate facts about the person, phrased as a 2150 registry would note them ("Registry note: credited with the first public demonstration of …").
  - Plus one line on why they're travelling.
  - Purpose (Saleh): the player *learns about historical figures*.
  - **Decision:** facts only. Contested claims are attributed ("Archive sources claim …"). No invented history about real people.
- **Random travellers (stories).**
  - Snippets are generated from their traits: traveller type (tourist / labourer / displaced), personality, debt, job, family, destination and trip reason.
  - Templates are slot-filled and seeded per run, so replays match.
  - Examples:
    - "Missed two debt instalments (2149). Repayment plan: labour transfer."
    - "Sister registered displaced, Sector 9, since the Drive."
    - "Third trip to 1920s Paris this year. Declares: 'research'."
  - **Threads:**
    - When a random comes back on a later day, their file grows a new line about what happened since. This includes your last verdict: "Returned after denial; fined for a missed departure."
    - Some lines echo the morning paper's news.
- **Clues versus flavour.**
  - Most snippets are flavour.
  - **Decision:** a few are deliberate clues that agree with the case's real faults. Example: "Labour contract lapsed 2150-02" on a labourer whose permit claims an active contract.
  - Clue snippets come only from the case's real data, so a file never lies about a case.
- **Content.**
  - Snippets and templates live in `world_source.json` under a new `lore` section: famous snippets per premade, random templates by type and trait.
  - A `lore` sheet in the narrative workbook (ContentSheetMap) lets Saleh or GPT write more without code.
  - Generate World validates every template slot.

## 4. Build

This is part of desk-machine step 2, track (b) (paper physics, scanner, movable props). The app and the lore system together are big enough to be a **third parallel track**, Track SA, owning the PC Investigation app and the `lore` content.

**Track SA needs:**
- the desk-machine prototype's verdict types (`Detained`);
- the document track's `hidden` / `chip` data.

**Order:**
1. **Lore system and citizen file:**
   - the content data, generator and file section;
   - authored snippets for every premade;
   - 30+ random templates.
2. **Auto lookup, rules check and cross-check table:** the convenience core.
3. **Layer switch** (needs the hidden-layer and chip data).
4. **Overlay compare.**
5. **Seen-before history.**

**Verify:**
- Domain tests:
  - the rules check's verdicts;
  - the cross-check's mismatches;
  - the lore generator is deterministic and its slots are valid;
  - clue snippets agree with the case's data.
- Audit play days 1-15 A/B identical. The audit uses the cross-check table to log evidence on one case per day.
- Probe captures of each panel at 1080p and 720p.
