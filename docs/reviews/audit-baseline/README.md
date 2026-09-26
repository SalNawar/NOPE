# Audit baseline (Phase 0: freeze and baseline)

The safety net for the code audit and overhaul
(`docs/superpowers/plans/2026-09-25-code-audit-overhaul-plan.md`, §2 Phase 0).
Everything here was captured on commit `ff3a6e0` (local tag `audit-baseline`,
branch `overhaul/audit`) and must stay identical after every slice, except
changes that a slice documents as intended.

## What is here

| Path | What | Deterministic |
|---|---|---|
| `metrics/metrics_baseline.json.gz`, `.md` | Static metrics: size per type and method, cyclomatic complexity (Roslyn CA1502, plus a token-count approximation calibrated against it), compiler warnings, unused private members (IDE0051/IDE0052/CA1823), duplicate blocks (token-window hash, exact and normalised), magic numbers and strings in rule code, Find/GetComponent and allocations on per-frame paths, static mutable state, singleton access sites | yes |
| `metrics/inventory.json.gz`, `.md` | Every type in `Assets/Scripts`, `Assets/Editor` (without `OfficeArt/`) and `Assets/Tests`: assembly, files, lines, public surface, callers (token scan) and scene/prefab/asset references (script GUIDs) | yes |
| `metrics/analyzer.json.gz` | The raw analyzer build output the metrics use | yes |
| `golden/cases.txt.gz` | Case generation, days 1-6 x 20 run seeds, plus day 6 under each of the 8 possible leaders: every traveller's claim, name, papers, tells, answers, look and garments, premade, violator, citizen record; each variant's world (plan, places, rules, interview, every fact) | yes |
| `golden/world_generate.txt`, `validator.txt`, `data_hashes.txt` | Generate World run twice (files it changed; content hashes of `Assets/Data`), the validator's output | yes |
| `golden/scene_*.txt`, `scenes_summary.txt`, `contract.txt` | Semantic dumps of the committed OfficeGameplay, HomeScene and TitleScene; each builder's rebuild compared with them; the office scene contract report | yes |
| `golden/play_transcript.txt`, `play_warnings.txt`, `play_saves/*.json` | Scripted play-through, seed 12345, days 1-6 in the art office and Home between them, to day 7's morning paper: briefings, every traveller, the interview, the evidence, the verdict, the ledger, Home and the save after each shift and each night | yes |
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
   (`playA`, `playB`; play mode, about 80 s, done when `play_report.txt` ends with `done`),
   `_TimeDeskAuditProfile.RunA` / `RunB` (`profA`, `profB`; about 50 s).
3. After each static job, restore what the builders and Generate World rewrote. The job
   ends on an untitled scene; never restore a scene that is open in the editor, or it
   stops with a modal "modified externally" dialog:
   `git checkout -- Assets/Data Assets/Scenes ProjectSettings "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset" *.csproj`.
   The play and profile jobs back up and restore the player's save slot (`SaveSystem.Files`: the save, its temp file and its backup), the UI language and
   motion preferences, and the run config's seed (set in memory only).
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

Title, New Run with run seed 12345 (`RunConfig.fixedRunSeed`, set in memory), days 1-6.

- **Each traveller:**
  1. READY.
  2. Every request (documents and spoken requests), then every question in the ask menu. Slot 1 also runs the first offered dialog, taking the first choice at each step.
  3. For a liar, each tell is proven through the real compare path: the tell (paper row, answer line, or a garment through Look) against the claimed place's book row. A birth date goes against the citizen record, and the true home's row is the fallback.
  4. The verdict: odd slots at the stamp tray, even slots on the PC.
- **Verdicts are right except four planned mistakes:**
  - Day 1: the first honest traveller from slot 2 on is denied.
  - Day 2: the first liar is accepted.
  - Day 3: the first liar is denied without evidence.
  - Day 4: the first rule violator is accepted.
- **Home each night:**
  1. Treat every family member at condition 2 or worse, if affordable.
  2. Buy Interview Protocols, then the Near East Speech translator, each once there is money to spare (the Near East Papers translator until the redesign's phase 1 retired it).
  3. Never spin the slot machine (its draw is unseeded, see below).
  4. Sleep.

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
