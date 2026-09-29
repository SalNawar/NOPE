# Audit baseline (Phase 0: freeze and baseline)

The safety net for the code audit and overhaul
(`docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md`, §2 Phase 0).
Everything here was captured on commit `ff3a6e0` (local tag `audit-baseline`,
branch `overhaul/audit`) and must stay identical after every slice, except
changes that a slice documents as intended. A slice with such a change re-packs
`golden/` (see "Re-baselines" below); the metrics stay those of `ff3a6e0`.

## What is here

| Path | What | Deterministic |
|---|---|---|
| `metrics/metrics_baseline.json.gz`, `.md` | Static metrics: size per type and method, cyclomatic complexity (Roslyn CA1502, plus a token-count approximation calibrated against it), compiler warnings, unused private members (IDE0051/IDE0052/CA1823), duplicate blocks (token-window hash, exact and normalised), magic numbers and strings in rule code, Find/GetComponent and allocations on per-frame paths, static mutable state, singleton access sites | yes |
| `metrics/inventory.json.gz`, `.md` | Every type in `Assets/Scripts`, `Assets/Editor` (without the art side's `OfficeArt/` and `TerminalArt/`) and `Assets/Tests`: assembly, files, lines, public surface, callers (token scan) and scene/prefab/asset references (script GUIDs) | yes |
| `metrics/analyzer.json.gz` | The raw analyzer build output the metrics use | yes |
| `golden/cases.txt.gz` | Case generation, days 1-15 x 20 run seeds (packed at the end-of-epic pass), plus day 6 under each of the 8 possible leaders: every traveller's claim, name, papers, tells, answers, look and garments, premade, violator, citizen record; each variant's world (plan, places, rules, interview, every fact) | yes |
| `golden/world_generate.txt`, `validator.txt`, `data_hashes.txt` | Generate World run twice (files it changed; content hashes of `Assets/Data`), the validator's output | yes |
| `golden/scene_*.txt`, `scenes_summary.txt`, `contract.txt` | Semantic dumps of the committed OfficeGameplay, HomeScene and TitleScene; each builder's rebuild compared with them; the office scene contract report | yes |
| `golden/play_transcript.txt.gz`, `play_warnings.txt`, `play_saves/*.json` | Scripted play-through, seed 12345, days 1-15 in the art office (the anime hall) and Home between them, to the day-15 ending on the Title: briefings, the day's portals and the Departure Board, every traveller, the interview, the evidence, the verdict and its reaction, the orders placed at the PC, the ledger, Home's evening and the House, and the save after each shift and each night | yes |
| `golden/profile_A.txt`, `profile_B.txt` | Profiled play-through (two runs): PlayerLoop GC alloc and frame time per window, the allocation sites, scene load times | GC per frame yes; times are measurements |
| `golden/MANIFEST.sha256` | sha256 of every deterministic golden file (uncompressed, LF line endings; `golden.py` compares with line endings normalised) | |

Tools: `tools/audit/*.py` (static metrics, `compare.py` gate, `golden.py` pack/diff)
and `tools/audit/unity/*.cs.txt` (the Unity jobs, kept as text).

## Re-run and diff

Paths below: `REPO` = the worktree; `CC` = a folder of Unity-generated csproj
files whose `<Compile>` items point at `REPO` (the scratchpad's
`compile_check_art.py REPO` regenerates them in its `cc_art` folder).

**1. Compile and offline tests.** `python compile_check_art.py REPO` (0 errors), then the
offline NUnit runner on `CC/Temp/Bin/Debug`. Baseline: 0 errors, **1067 passed, 0 failed**.

**2. Static metrics** (about 20 s, byte-identical between runs):

    python tools/audit/run_static_baseline.py --repo REPO --out OUT --cc-dir CC \
        --check docs/reviews/audit-baseline/metrics/metrics_baseline.json.gz

`--check` prints the regressions per metric (a bigger count, a new warning, clone group,
per-frame site, literal, static field, singleton site or unused member; line moves are
ignored) and exits 1. `--allow key,...` accepts named, documented regressions.

**3. Unity golden masters** (the one persistent editor, through `Library/ClaudeJobs`):

1. Copy `tools/audit/unity/_TimeDeskAudit{Static,Play,Profile}.cs.txt` to
   `Assets/Editor/_TimeDeskAudit*.cs` (untracked; never commit them there). Outputs go to
   `Library/AuditBaseline/<tag>/`, or to the folder named in
   `Library/AuditBaseline/out_root.txt`.
2. Queue one job at a time: `_TimeDeskAuditStatic.RunA`, then `_TimeDeskAuditStatic.RunB`
   (tags `runA`, `runB`; synchronous, about 25 s), `_TimeDeskAuditPlay.RunA` / `RunB`
   (`playA`, `playB`; play mode, about 4.5 min for the 15 days, done when `play_report.txt` ends with `done`;
   `_TimeDeskAuditPlay.RunShots`, tag `playShots`, plays the same run with the screenshots, about 10 min),
   `_TimeDeskAuditProfile.RunA` / `RunB` (`profA`, `profB`; about 50 s).
3. After each static job, restore what the builders and Generate World rewrote. The job
   ends on an untitled scene; never restore a scene that is open in the editor, or it
   stops with a modal "modified externally" dialog:
   `git checkout -- Assets/Data Assets/Scenes ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" *.csproj`.
   The play and profile jobs back up and restore the player's save slot (`SaveSystem.Files`: the save, its temp file and its backup), the UI language and
   motion preferences, and the run config's seed (set in memory only). In the editor the slot lives in the project's
   `Library/EditorSaves` (`SaveSystem.Folder`), so each worktree's editor has its own; the jobs set it aside there too,
   never in the shared `Application.persistentDataPath`.
4. Diff against the baseline:

       python tools/audit/golden.py diff --baseline docs/reviews/audit-baseline/golden \
           --static OUT/runA --play OUT/playA --profile OUT/profA

   Every deterministic file must be byte-identical; each builder's rebuild must equal the
   baseline's committed dump; the profile may not allocate more per frame, may not gain
   an allocation site in our assemblies, and no scene load may be more than 25% slower.
   `--allow name,...` accepts a documented, intended change.
5. Determinism: `python tools/audit/golden.py pack --static OUT/runA --play OUT/playA --out TMP`,
   then `golden.py diff --baseline TMP --static OUT/runB --play OUT/playB` must be clean.
6. Delete `Assets/Editor/_TimeDeskAudit*.cs` and their `.meta` files.

To re-baseline after an intended change: `golden.py pack --static ... --play ... --profile A --profile B --out docs/reviews/audit-baseline/golden`
and commit with the reason.

## The scripted play-through

Title, New Run with run seed 12345 (`RunConfig.fixedRunSeed`, set in memory), days 1-15 in the art office
RunConfig names (the anime hall), Home between them, until night 15's sleep ends the run on the Title.

- **Each morning:** the paper, the world line (day, seeds, wallet, leader, upgrades), the day's rules, the
  portal schedule (`GameManager.Portals`) and the Departure Board's rows as drawn.
- **Each traveller:**
  1. READY.
  2. Every request (documents, through the papers menu when there are two or more; a request group, the
     proof of means, as one entry; a form the traveller does not carry gets its missing-form reply), the
     spoken requests, then every question in the ask menu. Slot 1 also runs the first offered dialog,
     taking the first choice at each step.
  3. For a deviation fault (a liar, a forger, a costume error), each tell is proven through the real
     compare path: two papers that disagree first (the cross proof), then the tell (paper row, answer
     line, or a garment through Look) against the claimed place's book row, else the row its value
     belongs to (the present's for a smuggler or a fake displaced person); a record category against the
     traveller's own record row. A category already documented is not proven twice.
  4. The verdict (odd slots at the stamp tray, even slots on the PC) and the traveller's reaction line.
     A directive fault (a closure, a wrong date, an expired paper, an incomplete paper set, a frozen
     account, a recalled transponder) is denied without evidence; the rest are accepted.
- **Verdicts are right except four planned mistakes** (each checked in the report):
  - Day 1: the first honest traveller from slot 2 on is denied.
  - Day 2: the first deviation fault is accepted.
  - Day 3: the first deviation fault is denied without evidence.
  - Day 4: the first directive fault is accepted.
- **During each shift, at the PC (the Orders app's rule, `OrderBook.Order`):** Interview Protocols, then
  the Near East Speech translator (it needs the Protocols), at most one order a day, once the wallet holds
  its price and 20 cr more; it arrives the next morning.
- **Home each night:**
  1. Treat every family member at condition 2 or worse, if affordable.
  2. The House: every Home upgrade's state and price is recorded; the first buyable one with 60 cr to
     spare is bought (one a night).
  3. Never spin the slot machine (its draw is seeded, `Seeds.ForSlot`, but kept out of the run).
  4. Sleep.
- **The end:** night 15's sleep reaches the day-15 ending; the ending screen's title and body are recorded,
  and the report checks the demo ends on the END OF DEMO card.
- **The report's checks** (`play_report.txt`, not a golden file): each verdict right unless planned, each
  deviation fault proven before its denial, every traveller decided before closing, the log free of errors,
  the four mistakes made, the run's coverage (`COVER` lines: kinds, lies, faults, proofs, strandings).
- **RunShots** (tag `playShots`) plays the same run and saves, in `<tag>/shots/`, the screenshots of the
  redesign's states at 1920 x 1080 and 1280 x 720 (each day's paper and shift report, each traveller kind's
  desk, wheel and forms held, the app's tabs, two panes and one, search, the steps, each lie kind caught on
  the office strip and in the app, each citation, the Orders and Portals windows, the Departure Board's
  tooltip, the House, the ending) and, while the PC frame is open, a render of the desktop itself; its
  transcript must equal RunA's.

## Re-baselines

- **Redesign phase 1, speech-only translation** (`redesign/p01-speech-translation`):
  - `world_generate.txt`, `data_hashes.txt`: Generate World prunes the four Papers translators (`Assets/Data` 441 → 433 files) and rewrites the library and the translation notice.
  - `play_transcript.txt` and 11 of the 12 saves (all but day 1's shift): Home buys the Near East Speech translator (80) on night 1, where the retired Papers one (100) was bought on night 3. Interview Protocols follow on night 3 instead of night 2, so day 3 asks no birth date and the protocols notice moves to day 4's paper. The notice text changes; every verdict is unchanged; money shifts by the prices.
  - `scenes_summary.txt`: the build-settings line of phase 0's fix 1, documented in `docs/reviews/2026-09-26-audit-hotfix-gate.md` and packed here.
  - `profile_A.txt`, `profile_B.txt`: re-measured. The office views allocate 736 B per frame (phase 0's fix 7); no new allocation site; every load within 25% of the old baseline.
  - Unchanged: `cases.txt`, `validator.txt`, the three scene dumps (each rebuild equal), `contract.txt`, `play_warnings.txt`. Runs A and B were identical.

- **The art clean-up and the character pilot** (`verify/art-cleanup`, with main at `8487338` and art at `f7d9d49` merged in). The pack also takes in redesign phases 14 and 2, which reached main without re-packing:
  - `contract.txt`: the office scene contract on the new desk layout. HandOver's default moves to (0.050, 1.070, 0.450), behind the NEXT sign; NextSign, Intercom, ReadoutNext and Calculator report the art's moved props. Still 0 anchors, 20 fallbacks, 3 defaults, 0 missing.
  - `data_hashes.txt`: `OfficeSceneContract.asset` (the HandOver and NextSign defaults); phase 14's new `Desktop_Default.asset` and its meta; phase 2's agency block (`world_source.json`, `ContentLibrary_Main.asset`, `Strings_en.asset`).
  - `world_generate.txt`: `Assets/Data` 433 → 435 files (phase 14's `Desktop_Default.asset` and its meta).
  - `scene_OfficeGameplay.txt`: phase 14's window manager (DesktopWindowManager, the taskbar buttons, DesktopWindow in place of OSWindowChrome, the compare dock) and phase 2's record rows and the calendar's date readout. Each rebuild equals the committed scene.
  - `scenes_summary.txt`: the art scene's file hash (the art clean-up); the art office stays byte-unchanged by the builders.
  - `validator.txt`: 42 of 880 character keys have final art (the pilot's batch 1).
  - Unchanged: `cases.txt`, the Home and Title dumps, the play transcript, the 12 saves and `play_warnings.txt`. The profiles were not re-measured here.

- **Redesign phase 3, traveller kinds and the displaced's forms** (`redesign/p03-traveller-kinds`, main at `4291de2` merged in):
  - `cases.txt`: every traveller carries TC-610 Displacement Certificate, TC-620 Intake Declaration and TC-630 Return Order in place of the Travel Passport and the Transit Permit (the permit's Bond Currency goes); each form prints the traveller's Displacement No., the certificate their incident and Valid Until, the return order today's date; the record is the Displacement Registry entry (Displacement No., Incident, Found, Status "Awaiting return" added to Name, Born, Origin and Note); the claim line is the displaced's "Please. Send me home to {place}.". Nothing else moves: over all 6570 lines only the papers, the claim line and the record differ. Every claim, name, birth date, role, gender, liar, home, tell, answer, look, garment, premade and violator is the baseline's (the case, lie, dialog and look streams draw as before; the new values come from the new account stream).
  - `play_transcript.txt`: the same papers and claim lines; each traveller's requests go through "Request papers >" (the Intake Declaration and the Return Order, one more "Your ..., please." / "Here you are." pair each); the verdicts' `claimSummary`. Every verdict, proof, payment and ending is unchanged, and so are the 12 saves. The transcript is now over 200 kB, so it is stored gzipped (`play_transcript.txt.gz`).
  - `data_hashes.txt`, `world_generate.txt`: `Assets/Data` 435 → 437 files (the three TC templates in place of the passport and the permit; `CaseBlueprint_Investigation` renamed `CaseBlueprint_Displaced`, same GUID); the library (the claim per kind, the papers label, the agency's displaced ranges, no clue list), `Strings_en.asset` (the category words, the registry rows, CaseFactory's fallbacks) and `world_source.json`.
  - `profile_A.txt`, `profile_B.txt`: re-measured. No new per-frame allocation or allocation site; every load within 25% of the old baseline (a first run B measured home → office at 1099 ms, 66% over; its re-run measured 803 ms and is the one packed).
  - Unchanged: `validator.txt`, `contract.txt`, `scenes_summary.txt`, the three scene dumps (each rebuild equal: the builder's content checks moved into the validator, its output did not change) and `play_warnings.txt`. Runs A and B were identical.

- **Redesign phase 6, the present, Citizen Accounts and rich tourists** (`redesign/p06-present-accounts`, main at `cca8efe` merged in). No re-pack happened on main after phase 3, so this pack also takes in phases 4 (the forms engine), 17 (desktop icons), 25 (Mail, the Citizen Account, Notes, Settings), 16 (the Investigation app), 10 (dress for the destination), 13a (the clerk's debt), 5 (scanned copies as forms), 27 (the art hooks) and the overhaul slice "untouched"; each effect is named:
  - `cases.txt`: phase 6: every world lists the neutral present's row after today's places (six facts: Credits, Agency Standard English, Wrist comm, Directorate Tower, The Customs Directorate, "tech jacket / coat-dress"; a lead's Future place is never listed twice). Day 1 is rich tourists beside the displaced: over the 20 seeds, 74 slots are rich tourists (the baseline slot's claim, role and allowed kept; a 2150 name and birth year; TC-101 and TC-230 printing their Citizen Account; their record the account's three groups; honest, so the 36 of them that were liars in the baseline are not), 60 day-1 displaced slots are identical, and 26 differ only by name (the day's name roster no longer loses the pool names the rich tourists took; 15 of them change gender and so their look with it). Days 2-6, every leader variant included, are the baseline's travellers. Phase 4: every paper prints its serial ("[TC-610/048078]"). Phase 10: the rules line of days 2-6 adds the standing "Dress for the destination" directive (no traveller changes: costume errors are 2150 citizens' and there are none after day 1 yet).
  - `play_transcript.txt` and the 12 saves: phase 6: day 1's travellers 1, 2 and 4 are honest rich tourists (visa on arrival, "Request Departure Manifest" at the hub), all accepted, so day 1's accepted impacts make Iraq lead from night 1 instead of night 2 (day 2's briefing reports the lead and the al-jabr history rule a day earlier; the wallet reads the Mesopotamian dinar token and the labels are Arabic from day 2; the rule's Technology value reaches the day-2 to day-5 answers, and a day-5 liar's Language tell moves from the papers to the answer). Phase 13a: each shift's Debt Relief instalment and the debt news; the smaller wallet buys Interview Protocols before the Near East translator, so the birth-date question is asked earlier. Phases 25 and 16: the saves' new fields and app state. Every verdict's correctness is the baseline's (65 correct, the 4 planned mistakes).
  - `scene_OfficeGameplay.txt`, `scene_TitleScene.txt`, `scenes_summary.txt`: phases 4, 17, 25, 16, 13a (the Title's Debt Relief papers), 5 (the scanned-copy `FormView`; phase 5 changes nothing else here) and 27 (the by-name art slots in the office, Home and the Title; with the six endings' art slots in `data_hashes.txt`, phase 27 changes nothing else here); phase 6 does not change a builder, and each rebuild equals its committed scene.
  - `validator.txt`: phase 10's costume and kit checks (the character art line).
  - `data_hashes.txt`, `world_generate.txt`: `Assets/Data` 437 -> 448 files: phase 6's TC-101, TC-230 and `CaseBlueprint_RichTourist` (with metas), phase 4's `Forms/FormStyle_Agency`, phase 10's `Rule_DressForDestination`; the library (the present with its clothes and kit, the accounts' ranges, the transponders, the kinds per day, the clerk and its debt, the debt news), `Strings_en.asset`, the day plans, the places' wardrobes and `world_source.json`.
  - `profile_A.txt`, `profile_B.txt`: re-measured. No new per-frame allocation or allocation site; every load within 25% of the old baseline in the runs packed (title -> office 1126 / 1122 ms, office -> home 364 / 421 ms, home -> office 937 / 940 ms). Loads were noisy while five other editors worked: earlier runs of this phase each had one load over 25% (home -> office 917 ms; title -> office 1384 and 4767 ms; office -> home 470 ms), a different load each time.
  - Unchanged: `contract.txt`, `play_warnings.txt`, the Home dump. Runs A and B were identical.

- **The end-of-epic pass** (`epic/pass-1`, main at `eb11080` merged in: redesign phases 7-9, 11-13b, 16 part 2,
  18-23, the anime hall, days 7-15, Orders, personalities, portals and the Home upgrades). No re-pack happened
  since the readable tabs (`e68498d`), so every effect of those phases is in this pack:
  - `cases.txt`: days 1-15 (11,785 lines, was days 1-6); every traveller kind, lie, costume error, directive
    fault and procedure of the phases above, the personalities' voices and the spoken claim.
  - `world_generate.txt`, `data_hashes.txt`: `Assets/Data` 456 -> 715 files (the new forms, rules, day plans
    7-15, questions, voices, portals, Orders and Home upgrades, themes and string tables).
  - `validator.txt`: the personalities' checks and the character-art line.
  - `scene_OfficeGameplay.txt`: the panes, search, keys, steps, scanners, the app's form views, Orders,
    Portals and the Departure Board (16,106 lines); `scene_HomeScene.txt`: the House panel. Each rebuild
    equals its committed scene; the Title dump is unchanged.
  - `scenes_summary.txt`, `contract.txt`: the live art office is the anime hall (`RunConfig.officeSceneName`);
    the static job now reports its contract (4 anchors, 20 fallbacks, 0 defaults, 0 missing) and checks both
    art offices stay byte-unchanged.
  - `play_transcript.txt` and the 30 saves: the play-through as described above, days 1-15 (215 travellers),
    to the day-15 ending (`scientific_age` in this run). Runs A and B and the screenshot run were identical.
  - `play_warnings.txt`: 104 missing-glyph warnings from Home's `StabilityText` (Arabic labels in
    LiberationSans while Iraq leads). A known fault, packed so the file is exact; it is not accepted as intended.
  - `profile_A.txt`, `profile_B.txt`: re-measured. The office windows allocate 398 B per frame: URP's 368 B
    and 30 B from `PortalEffect.LateUpdate` (new site; a known fault, not accepted). Loads: title -> office
    1119 / 1099 ms, office -> Home 365 / 384 ms (run B 28% over the old 299 ms), Home -> office 760 / 768 ms.
  - The static metrics leave out `Assets/Editor/TerminalArt/` (the art side's tooling) exactly as they leave
    out `Assets/Editor/OfficeArt/` (`tools/audit/sources.py`); the `metrics/` baseline stays `ff3a6e0`'s.

## Baseline results (ff3a6e0)

- **Tests:** compile 0 errors; offline EditMode 1067 passed, 0 failed.
- **Size:** 301 files, 534 types, 47,275 lines (31,975 code lines), 2,485 method-like units.
- **Compiler warnings in our code:** 16, all CS0618 (obsolete `FindFirstObjectByType`, `FindObjectsByType`, `FindObjectsSortMode`).
  - By file: OfficeSceneUIBuilder 7, HomeSceneBuilder 4, TitleSceneBuilder 4, InvestigationUIController 1.
  - The csproj also suppresses CS0169 and CS0649, as Unity does.
- **Structure:** 12 production types over 400 lines, 40 methods over 60 lines, 73 production units with complexity over 15.
  - Biggest types: OfficeSceneUIBuilder 3107, WorldContentGenerator 2377, ContentLibraryValidator 1290, InvestigationUIController 960, CaseFactory 866, GameManager 765.
  - Biggest methods: OfficeSceneUIBuilder.Build 386 lines (CC 26), WorldContentGenerator.CheckInterview 315 (CC 109), WorldContentGenerator.CheckCharacters 230 (CC 137), GameManager.Start 149 (CC 26).
- **Duplication (production, window of 60 tokens):**
  - Exact: 14 groups, 371 lines. The largest is 103 lines shared by HomeSceneBuilder and TitleSceneBuilder.
  - Normalised: 119 groups, 1,467 lines.
- **Rule-code literals:** 52 magic numbers and 201 string literals.
- **Per-frame paths:**
  - 9 Find/GetComponent sites; 5 of them are the non-allocating TryGetComponent.
  - 69 allocation sites; 61 of them are in the dev DebugPanelController.
- **Static state and singletons:**
  - 63 static mutable state items: 8 fields, 2 settable static properties, 53 static readonly collections.
  - 40 singleton access sites: RunManager 29, CultureThemeService 11.
- **Unused members:** 1 unused private member, `GameManager.ResolveCurrentCase`. 3 serialized fields are never read in code.
- **GC alloc per frame, PlayerLoop, steady state:**
  - Office views: 838 B, the same in the office view, on the desktop in the PC frame, with the wheel open and between travellers.
  - Title and Home: 368 B.
  - Our code's share is `OfficeReadouts.Update`: 102 B and 4 allocations every frame (it re-formats the day, stability and credits texts each frame). `ShiftClockReadouts.Update` allocates once per clock minute.
  - The rest is URP's `ExecuteRenderGraph > Draw` in the editor: 736 B in the office, 368 B in Title and Home.
  - The EditorLoop allocates 0 B.
- **Frame time, PlayerLoop median:**
  - Office views: 2.1 to 2.5 ms, with frame times of 3.3 to 3.9 ms.
  - Title and Home: 0.6 to 0.8 ms.
- **Scene loads:** title to office 937-975 ms, office to Home 332-344 ms, Home to office 679-683 ms.
- **Determinism:** every golden master was run twice and was identical.
  - Case generation was identical, also when run twice in one job and after Generate World.
    The committed dump adds each traveller's citizen record to the pair of runs that was
    compared. Without that field it hashes equal to both of them, and the field was
    identical between the two generations inside its own run. A second full run with the
    field could not be taken: the editor was stopped by a modal dialog.
  - Generate World changes 0 files, compared by EOL-normalised content.
  - Every builder's rebuild equals the committed dump, and a second rebuild equals the first.
  - The play transcript and all 12 saves were identical.
  - GC per frame was identical; only times vary.
- **Nondeterminism and noise found:**
  - The slot machine draws from unseeded `UnityEngine.Random` (`HomeManager.HandleSpin` → `UnityRandomSource`). Its outcomes change the liar, pay and legendary modifiers, and so the next day's travellers, so the same run seed with a spin does not replay.
  - `RunManager.NewRun` draws the run seed from `UnityEngine.Random` when `fixedRunSeed` is 0, which is by design.
  - Generate World rewrites 30 `Assets/Data` assets with LF endings over the CRLF checkout (`core.autocrlf=true`). This is byte churn with no content change.
  - The office builder re-saves `OfficeGameplay.unity` with new file IDs; its semantic dump is unchanged.
  - The shift clock runs in real time. The scripted play decides every traveller in about 12 s per day (checked each day), so closing time never cuts it.
  - Every office load warns that the art office lacks `Anchor_Scanner`, `Anchor_Traveller` and `Anchor_HandOver`, so the default poses are used.
