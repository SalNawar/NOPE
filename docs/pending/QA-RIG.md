# P0-B QA-RIG: the standing player-like QA

Branch `fix/p0-qa-rig`, cut from `fix/p0-carve` (f347060). Source: FIX_PLAN.md §3 "P0-B QA-RIG", FIX_PLAN_v2.md "P0-B QA-RIG: delta" (v2 wins), and the GAME_FEEL_BIBLE proposals given to QA-RIG (G9 and gap 3: the impulse cause; gap 2: the flash meter; gap 6: the input-latency probe; §5.2 #4).

**What it is:** the tools every later track verifies with, so a build that feels broken can no longer pass (BUG-53). A virtual mouse and keyboard drive the game through the Input System. A 30 fps recorder captures what the player sees. A camera logger names the cause of every camera move. Offline tools measure the frames. DemoGate collects every check, and DemoBuild refuses to build a demo while any check is red. It also closes BUG-30 (the lever, swept by the builder, and the missing-script gate), measures BUG-65 (65a), and adds the SO-field-reader test (73a).

**What it is not:** no player-facing change. Nothing in the game reads the rig. The rig is compiled only in the editor and in QA player builds (`#if UNITY_EDITOR || TIMEDESK_QA`). The one exception is `QaSignals`, a two-method seam that costs nothing when nobody listens.

---

## 1. Decisions (ours; INTEGRATION folds them in)

1. **Test mechanism.** P0-B asks for one test proven failing then passing under each candidate, and for the choice to be recorded here.
   - **Chosen: both, on one harness. UTF stays the preferred mechanism until the Unity proof.**
   - Both candidates run the same code:
     - `QaPlayHarness` (Assets/Editor/QA) prepares the editor like the player and imports the results.
     - `QaRunner` (Assets/Tests/PlayMode/Rig) plays the scenario.
   - **Preferred: UTF `[UnityTest]`** in Assembly-CSharp-Editor. The base class is `QaPlayTest` (Assets/Tests/PlayMode/Editor). A test yields `EnterPlayMode`, then `Play()`, then `ExitPlayMode`. Its state lives in SessionState and in the run folder, so the domain reload loses nothing.
   - **Fallback: the job runner.** `QaJobs.<Recipe>` returns at once, and the run finishes on its own.
   - **Not proven yet (deferred, Unity):** that UTF resumes a test enumerator across EnterPlayMode's domain reload, and that it accepts a nested `yield return Play()`. If it fails, use the job runner and drop `QaPlayTest`. The job runner needs neither.
2. **No PlayMode asmdef.** A test asmdef cannot reference Assembly-CSharp. The play tests would then see no game type (BUG-53's "circular probe" alternative). So the framework lives in three places:
   - the runtime rig, in Assets/Tests/PlayMode/Rig (Assembly-CSharp, compiled only in the editor and QA builds);
   - the base class, in Assets/Tests/PlayMode/Editor (Assembly-CSharp-Editor);
   - each track's play tests, in `Assets/Tests/PlayMode/<Track>/Editor/` (also Assembly-CSharp-Editor). Its runtime ops go in `Assets/Tests/PlayMode/<Track>/Rig/`.
3. **Quality: per run, not per project.** P0-B asks for parity "during QA". `QaPlayHarness.Prepare` sets the Standalone default (Ultra) for every run and `Restore` puts the editor's level back; the run asserts `<scenario>.quality` (it started and ended at that level), and `qa.play.smoke` reads `smoke_day1.quality`. `QualitySettings.asset` is not changed (`m_CurrentQuality` stays 0): a project-wide Ultra would make the four persistent editors render at Ultra all day, would collide with the local QualitySettings change in Saleh's main checkout, and any quality switch in an editor would redden the gate. `qa.quality.parity` (static) only checks that the file names an existing Standalone level.
4. **The lever (BUG-30).**
   - `OfficeSceneUIBuilder.Build()` first sweeps the objects it has retired. `QaSceneScan.Retired` lists only `Office/GateLever` for now.
   - Every build ends with the integrity gate: errors for missing scripts, null or error materials, and retired objects.
   - **Why not a generic "unknown children" sweep:** the builder fills gaps by name and keeps hand-placed objects, so a generic sweep could delete something we want. It cannot be proven safe without a Unity run. The named list plus the missing-script gate catches the BUG-30 class: an object whose script was deleted fails the gate until it is added to the list.
   - OfficeGameplay.unity is not committed here. INTEGRATION's rebuild removes the lever.
5. **Known-red list (`tools/qa/gate_known_red.txt`).**
   - Some checks are red because a later track has not merged yet: flicker is HALL-FX's (#4), the camera tags are CAM's and DOC-HANDLE's, LiberationSans is TEXT-LANG's.
   - The merge gate lists these checks without refusing, so it refuses only on a regression.
   - The demo gate refuses them like any red check.
   - Every entry names its BUG part and owner. The gate report lists entries that now pass, so INTEGRATION can drop them.
   - **Run patterns.** A line is `<check id> [<run pattern> ...]  # why`. The patterns (`*.camera`, `*.camera.*`, `*.liberationSans.*`, `*.seq.*.flicker`, `bp.*.seq.*.flicker`) name the run assertions and analysis results that feed the check. A play run whose only failures match them is "ok (N known red ...)" in `harness.txt`, `QaPlayTest.AssertRun` and `run_player.py` (`DemoGate.JudgeRun`), so the smoke day is not "failed" on every run until CAM and TEXT-LANG merge, and anything else red still fails it. `run.json`'s own `ok` and `done.txt` stay strict. The patterns leave with their entry.
6. **Dead config (BUG-73a).**
   - The scanner covers every ScriptableObject class and every `[Serializable]` type an SO holds, such as MotionKnobs inside MotionTuning.
   - On f347060 it finds 41 SO classes, 139 held types, 1,495 fields and 8 unread fields: `SoundBankSO.mixer` and 7 MotionKnobs stamp and drawer knobs.
   - Those 8 are on `tools/qa/so_reader_allowlist.txt`, each with its BUG part and owner.
   - A new dead field fails the merge gate (`qa.so.readers`). Any field still on the list fails the demo gate (`qa.so.cleanup`).
7. **Retired:** `tools/audit/unity/_TimeDeskQASweep.cs.txt` is deleted. It teleported papers and called handlers directly, which is BUG-53's root cause. `docs/FEATURES.md` still names it, and INTEGRATION drops that line. Its coverage list survives in git history (34e87b1) for the tracks that port its checks as scenarios.
8. **Fixes after the 66f29e1 review (all ours):**
   - **The QA player has its own product name** (the game's + `" QA"`, `QaRunner.QaProductSuffix`). `BuiltPlayerCapture.Build` sets it for the build only and puts `ProjectSettings.asset` back byte for byte. Its `persistentDataPath` (saves) and its registry key (PlayerPrefs, the window size `res` and `-screen-*` set) are its own, so a [BP] run never touches the demo's save or settings. The player refuses `-qaScenario` under any other name (exit 3), and `run_player.py` refuses a player whose `qa_build.json` does not name a QA product.
   - **The rig does not measure itself.** `CameraMoveLog` is allocation-free and writes nothing while `QuietWhen` holds (a `gc` window or a timing sequence): rows wait in a buffer reserved before the window and are written after it. `realtime 1` now records a **timing sequence**: no screenshot, no PNG, no per-frame write or allocation (rows buffered, written at `recStop`; the start frame is left out); `dtMs` is real time between frame ends. The `gc` op reserves its samples and skips its first frame. So `bp.bp_smoke.gc.steady` (0 B) and the call-next hitches measure the game.
   - **A blocked press fails.** When the target of `newRun`, `startShift`, `sign`, `button` or `click` (target) is not under the pointer, the rig still delivers it (so the run goes on) but counts it as blocked: `<scenario>.presses` fails and names it. `"direct": true` on a step allows it (noted, not failed) for a target meant to be reached so. `qa.play.smoke` and `qa.bp.smoke` read `presses`.
   - **Drags run at the fixed step.** `drag` and `drop` set `Time.captureFramerate = 30` for the path and the settle (as a recording does), so the P-* speeds and the 45-frame (1.5 s) settle hold at any real frame rate.
   - **A stopped run cleans up.** `QaRunner.OnDestroy` (deadline, `QaJobs.Reset`, play mode left by hand, player closed) gives the real mouse and keyboard back, removes the virtual devices, restores the focus rule, the frame rate, the seed and the hour, and writes a failed `run.json` (done.txt `aborted`). `QaPlayHarness.Restore` also calls `QaInput.ReleaseStray()` and restores the background behaviour from SessionState. The saves set aside are named in `<SaveSystem.Folder>/qa_rig_aside.txt` on disk; an editor that crashed or restarted mid-run puts them back when it loads (`RecoverSaves`).
   - **Dirty results are stale.** A result carries `dirty` (uncommitted content changes under Assets, Packages or ProjectSettings, less Unity's own churn: the LiberationSans fallback, EditorBuildSettings.asset, QualitySettings.asset and the generated *.csproj/*.sln; untracked files do not count). The editor stamps it from git, snapshotted when a run is prepared or the QA player is built, before the rig touches a setting (the QA quality level, the QA product name); the QA player's results from `qa_build.json` (`dirty` at build time). `GateResults.Judge` fails dirty results as `DIRTY`, like stale ones.
9. **Reference clips (v2 rule 8):**
   - Papers, Please and ReStory clips stay local, for review only, in `R7\REFERENCE\PapersPlease\` and `R7\REFERENCE\ReStory\`. They are never committed.
   - Extract a clip's frames to PNG at 30 fps with any tool.
   - `seqtools.py side <seq> <frames> out.gif` puts our sequence beside the reference.

## 2. Files (all QA-RIG's; the map for docs/pending/CARVE.md)

| Path | What |
|---|---|
| `Assets/Tests/PlayMode/Rig/QaSignals.cs` | **Seam, always compiled.** `CameraMoveCause` (Untagged, ViewKey, DeskDropTilt, SlideInFocus, Impulse), `QaSignals.TagCamera(cause, seconds, capPx)`, `QaSignals.Event(channel, name)` |
| `.../Rig/QaPaths.cs` | The P-* paths (Plain, Fast ≥2,000 px/s, Slow ≤150, Wobble ±30, Diag, Flick ~1,500 at release, Edge 5 px, Off, Onto), one sample per frame at the 30 fps game step (`drag`/`drop` fix it), a hover, a press and a 2-frame grab |
| `.../Rig/QaInput.cs` | Virtual Input System mouse and keyboard (`QueueStateEvent`). Disables every other pointer and keyboard during a run. Reports the frame each press is seen |
| `.../Rig/SeqRecorder.cs` | 30 fps recorder: PNG frames, `frames.tsv`, `events.tsv` and `meta.json`, plus full-resolution stills; or a timing sequence (real frame times, no capture) |
| `.../Rig/CameraMoveLog.cs`, `QaCameraMoves.cs` | The camera logger: picture shift per frame (px at 1080p), cause windows, impulse cap. Writes `camera.tsv`; quiet (no allocation, no write) in GC windows and timing sequences |
| `.../Rig/QaRunner.cs`, `QaScenario.cs`, `QaGame.cs`, `QaSelfTest.cs` | The scenario player (ops in §4), its JSON model and reports, the game access and probes, and the self-test |
| `Assets/Tests/PlayMode/Editor/QaPlayTest.cs` | The UTF base class for every track's play tests |
| `Assets/Tests/PlayMode/QaRig/Editor/QaRigPlayTests.cs` | QA-RIG's UTF play tests: the self-test and the smoke day (Explicit, category QaPlay) |
| `Assets/Editor/QA/GateCheck.cs`, `DemoGate.cs`, `GateResults.cs` | The check registry, the gate (refusal, known-red, report), and the result files with their commit stamps |
| `Assets/Editor/QA/Checks/QaRigChecks.cs` | QA-RIG's checks (§5) |
| `Assets/Editor/QA/QaPlayHarness.cs`, `QaJobs.cs` | The editor harness (set-up, play, import, restore, frame analysis), and the job and menu entry points |
| `Assets/Editor/QA/QaQuality.cs`, `QaSceneScan.cs`, `SoFieldReaders.cs`, `PhaseBGate.cs`, `QaGcSites.cs`, `BuiltPlayerCapture.cs` | Quality parity, scene integrity, dead config, Phase B rules, GC per call site, and the QA player |
| `Assets/Editor/QA/Tests/*` | QA-RIG's EditMode tests. They are pure except `QaEditorGateTests`, which is NeedsEditor |
| `Assets/Editor/DemoBuild.cs` | Runs `DemoGate.Run(Demo)` first and refuses on red. `BuildScratch` builds a non-demo player without the gate |
| `Assets/Editor/OfficeSceneUIBuilder.cs` (root) | `SweepRetired` and `CheckSceneIntegrity` |
| `tools/qa/seqlib.py`, `seqtools.py`, `run_player.py`, `test_seqlib.py` | The offline frame analysis (§6), the [BP] launcher, and their unit tests |
| `tools/qa/scenarios/*.json` | The standing recipes (§3) |
| `tools/qa/editor_runner/` | The offline runner for the pure Editor-side tests (Assembly-CSharp-Editor) |
| `tools/qa/gate_known_red.txt`, `so_reader_allowlist.txt` | INTEGRATION-kept lists (§1) |

Tracks own `Assets/Editor/QA/Checks/<Track>Checks.cs`, `Assets/Tests/PlayMode/<Track>/**` and their own scenario files. A track scenario goes in `tools/qa/scenarios/<track>_*.json`: the folder is QA-RIG's, and each file belongs to the track that adds it.

## 3. How to run

All Unity runs go through the persistent editor of the worktree, by job file. Never launch Unity. Nothing runs while Saleh is on the PC.

| Job (`Library/ClaudeJobs/<name>.job` holding) | Plays | Ends with |
|---|---|---|
| `QaJobs.SelfTest` | `selftest.json` on an empty scene | `selftest.selftest.<Path>` results |
| `QaJobs.Smoke` | `smoke_day1.json`: Title, New Run, day 1, the hand-over sequence, one traveller decided, 10 s idle at H2, the probes | `smoke_day1.*` plus the frame analyses |
| `QaJobs.AspectSweep` | `aspect_sweep.json`: A1-A4 × H1-H5, one still and the probes each | `aspect_sweep.*` |
| `QaJobs.DayJumps` | `day_jumps.json`: Restart d1, Jump d5, d7 Restart and Skip to d8, Jump d12 and Restart, Jump d15 | `day_jumps.*` |
| `QaJobs.DaySequence` | `day_sequence.json`: days 1→15 in sequence (AutoDecide, EndShift, report, Go Home, Sleep) | `day_sequence.*` |
| `QaJobs.GcSites` | `perf_day12.json`: day 12 with every item, GC per call site (editor profiler), drags, call-next frame times | `perf.gcsites` and `gc_sites.tsv` |
| `QaJobs.Scenario` | The scenario named in `Library/QA/job_scenario.txt` (any track's recipe) | its results |
| `QaJobs.Gate` / `QaJobs.GateDemo` | The merge or demo gate | `Library/QA/DemoGate/report.md` / `report_demo.md` |
| `QaJobs.BuildQaPlayer` | Builds the QA player (DEMO_CHEATS + TIMEDESK_QA, development, product name + " QA") to `E:\unity\NOPE-builds\QA_<worktree>\` with `qa_build.json` (commit, dirty, product, quality) | the exe |
| `QaJobs.Reset` | Unsticks the harness after a run that died: leaves play mode (the runner gives the input back as it is destroyed), then (run it again) restores the saves, view, input devices and quality | an empty scene, in edit mode |

- **Play jobs** return at once. `Library/QA/last_run.txt` names the run folder `Library/QA/runs/<scenario>_<time>/`.
- **The run folder holds:** `run.json`, `warnings.txt`, `camera.tsv`, `drags.tsv`, `seq/<name>/`, `stills/`, `analysis.txt` and `harness.txt`. Wait for `harness.txt`, then read `run.json` and `analysis.txt`. `harness.txt`'s first line is the verdict against the known-red list: `ok`, `ok (N known red: ...)` or `failed (...)`.
- **Built player [BP]:** run `python tools/qa/run_player.py tools/qa/scenarios/bp_smoke.json` after `QaJobs.BuildQaPlayer`. It opens a window, so run it only when Saleh is away. It plays in the QA player's own save folder and window settings (never the demo's). It imports `bp.`-prefixed results stamped with the built commit (dirty when the build was), runs `seqtools all` on every sequence (flicker, flash and latency on a captured one, hitches on a timing one), and exits 0 when nothing but known red failed.
- **Outside Unity (every change):**
  - compile check;
  - the house runner;
  - `dotnet run --project tools/qa/editor_runner -c Release -- <cc>\Temp\Bin\Debug` with `QA_PROJECT=<worktree>`: QA's pure tests plus the carve's Editor-side seam tests, with NeedsEditor skipped;
  - `python -m unittest discover -s tools/qa -p "test_*.py"`.

## 4. Scenario ops (QaRunner.Ops; JSON, `tools/qa/scenarios/<name>.json`, `"name"` = the file name)

Screen positions are fractions of the screen (0..1, origin bottom-left), so a step means the same place at every aspect. A step that throws or times out stops the run and fails it.

| Op | Fields | Does |
|---|---|---|
| `title` | f timeout | Waits for the title, then sets RunConfig's seed to the scenario's (restored at the end) |
| `newRun` | text = pet name, direct | Clicks New Run, names the pet and clicks Adopt, with the virtual mouse |
| `waitOffice` | n day (0 any), f timeout | Waits for the art office, the gameplay layer and that day's briefing |
| `startShift` / `sign` / `waitTraveller` | f, direct | Clicks Start Shift / clicks the AVAILABLE sign / waits for a traveller at the desk |
| `hour` | f hour (-1 = clock) | `DevToolsState.ForcedHour` (H1 9, H2 12, H3 16.75, H4 18.5, H5 22) |
| `jumpDay` / `cheat` | n / name (DevCheat), n, f, text | `RunManager.JumpToDay` / `DevCheats.Run` |
| `call` / `button` / `waitActive` | target `Type.Member`, direct | Calls a parameterless method / clicks a Button where it shows / waits until a GameObject or component is shown |
| `click` | target (object path or name) or x, y, direct | A virtual click at the object's screen point |
| `drag` | path (P-*), target or x, y → target2 or x2, y2, name | Plays the path at the fixed 30 fps game step. From an object, the grab offset is the real one. Logged in `drags.tsv` |
| `drop` | as `drag`, n settle frames (45), f max px (8) | Asserts the dragged object rests within f px at 1080p of where it was on the release frame (BUG-53 per-drop assertion; 1019a's ClearOfBlockers push fails it). Path and settle run at the fixed step, so 45 frames are 1.5 s of game time |
| `key` / `move` / `wait` | key + n hold / x, y + n / n frames or f seconds | Keyboard tap / hover move / wait |
| `waitScene` | name, f | Waits for a loaded scene |
| `res` | w, h | Sets the game view (editor) or the window (player) to w×h |
| `recStart` / `recStop` / `record` | name / - / name + f seconds | A 30 fps sequence into `seq/<name>/` (a timing one after `realtime 1`) |
| `realtime` | n (1 = timing sequences) | The next sequences are timing ones: real frame times only, no capture, nothing allocated or written per frame (the hitch measurement). Set it before `recStart`; `realtime 0` goes back to captures |
| `still` | name | A full-resolution PNG in `stills/<name>_<w>x<h>.png` |
| `track` | name, target | The object's screen rect goes into every frame row: the flicker detector's expected-motion mask, and a track's rect tests |
| `probe` | name | Assertions: `missingScripts.<name>` and `materials.<name>` (QA-RIG), `liberationSans.<name>` (TEXT-LANG: a visible glyph from a LiberationSans asset) |
| `gc` | name, f seconds, n byte budget | Asserts `gc.<name>`: GC Allocated In Frame, max ≤ n. The rig's own logging is quiet in the window. In the editor the number includes the editor's own allocations, so the gate is [BP] |
| `mark` | name | `QaSignals.Event("qa", name)`. `gcsites.start` and `gcsites.stop` bound the per-call-site GC window |
| `cameraCheck` | name | Asserts `camera.<name>` now. Every run also asserts `camera`, `console` and `presses` at its end, and in the editor `quality` |
| `selftest` | - | The rig's self-test (`selftest.<Path>`) |
| `note` / `quit` | text / - | A note in the step's report / ends the run there |

**`direct`** (bool, any press op): when the target is not under the pointer, deliver it anyway without failing `presses` (noted in the step). Without it a covered target is delivered but fails `presses` (BUG-53, BUG-68's class).

**Tracks' own ops:** `QaRunner.RegisterOp("name", (runner, step) => IEnumerator)`, called from the track's rig file in a `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` method under `#if UNITY_EDITOR || TIMEDESK_QA`. The body gets `runner.Input`, `runner.Recorder` (`Track`, `Column`, `Mark`), `runner.Cameras` and `runner.Assert(id, owner, pass, measured, detail)`. The scenario tests read the registered names, so a typo fails offline.

**What a run writes:**
- **`frames.tsv`, one row per frame:** i, frame, time, unscaled, timeScale (the hit-stop log, G4), screenW/H, capW/H (0 in a timing sequence), mx/my (the pointer, in capture pixels; screen pixels in a timing sequence), down, gcBytes, mainMs, dtMs (real time since the previous frame ended), tracked (`name:x0,y0,x1,y1;`), and extra (`key=value;` from `Recorder.Column`); a timing sequence leaves tracked and extra empty.
- **`meta.json`:** `fixedStep` and `frames` (false for a timing sequence), fps, capture width, screen.
- **`events.tsv`:** frame, i, unscaled, channel, name. It holds every `QaSignals.Event`, every press the game saw (channel `mouse` or `key`), and the marks.
- **`camera.tsv`:** frame, unscaled, camera, stepPx, causes, pose and fov.

## 5. The gate

| Check | Kind | Owner | Pass |
|---|---|---|---|
| `qa.quality.parity` | Static | QA-RIG | QualitySettings names an existing Standalone level (every run plays at it and asserts `quality`) |
| `qa.hall.previewHourOff` | Static | QA-RIG | No HallLightingSO has previewHourOn |
| `qa.content.validator` | Static | CONTENT | `ContentLibraryValidator.Validate` logs no warning or error |
| `qa.scenes.missingScripts` / `materials` / `retired` | Static | QA-RIG | 0 in every enabled build scene (opened additively, read only, never saved) |
| `qa.scenes.liberationSans` | Static | TEXT-LANG | 0 texts set in LiberationSans (known red) |
| `qa.so.readers` / `qa.so.cleanup` (demo) | Static | QA-RIG / INTEGRATION | No unread SO field off the allowlist / the allowlist is empty |
| `qa.phaseb.approvals` / `qa.phaseb.bundles` (demo) | Static | INTEGRATION | Every merged Phase B track has an approval line in `docs/phaseb/APPROVALS.md` (or `<R7>\PHASEB\APPROVALS.md`, with `Library/QA/r7.txt` naming R7) / the language bundle is whole or absent |
| `qa.play.selftest` | Play | QA-RIG | Every P-* path's press and release are delivered within 1 px, with a hover before and drag events between |
| `qa.play.smoke` | Play | QA-RIG | The smoke day's console has 0 errors, every press reached its target under the pointer, it played at the player's quality level, and its probes are clean |
| `qa.play.camera` | Play | CAM | 0 untagged camera moves, impulses under their caps (known red) |
| `qa.play.liberationSans` | Play | TEXT-LANG | 0 LiberationSans glyphs on screen at the probes (known red) |
| `qa.play.flicker` | Play | HALL-FX | The smoke day's sequences pass flicker (known red) |
| `qa.play.flash` / `qa.play.latency` (demo) | Play | UI-PC / QA-RIG | ≤3 flashes/s and no red full-field flash / every press visible within 1 frame |
| `qa.perf.gcsites` | Play | QA-RIG | The per-call-site baseline was recorded at HEAD |
| `qa.bp.smoke`, `qa.bp.flicker`, `qa.bp.flash`, `qa.bp.latency`, `qa.bp.perf` (demo) | BuiltPlayer | QA-RIG, HALL-FX, UI-PC, QA-RIG, QA-RIG | The same on the QA player (`smoke` with `presses`). `perf`: 0 B/frame steady-state, and no call-next frame over 33.4 ms (a timing sequence) |

**How results are read:**
- A Play or BuiltPlayer check reads `Library/QA/results/*.json` (`QaGateResult`).
- It passes only when at least one result matches, every match passed, and every match was measured at HEAD on a clean tree (stale and dirty results fail).
- A BuiltPlayer check also needs every match measured on a player.

**Registering a check (each track, in `Assets/Editor/QA/Checks/<Track>Checks.cs`):**

```csharp
public static class DocHandleChecks
{
    [GateCheck("doc.drops", "DOC-HANDLE", GateKind.Play)]
    public static GateResult Drops() => GateResults.Require("doc.drops", "DOC-HANDLE", "doc_drops.drop.*");

    [GateCheck("doc.callnext", "DOC-HANDLE", GateKind.BuiltPlayer, DemoOnly = true)]
    public static GateResult CallNext() => GateResults.Require("doc.callnext", "DOC-HANDLE", "bp.doc_drops.seq.callnext.hitches", true);
}
```

**Rules for a check:**
- A static check reads assets and code and returns `GateResult.Of(id, owner, pass, measured, detail)`.
- A check that throws, returns nothing, takes parameters or names no owner is red.
- Duplicate ids are red.
- **Phase B:** a track counts as merged when any registered check names it as owner (definition of done #3). From then on it needs its approval line.

## 6. Offline analysis (`tools/qa/seqtools.py`; pure functions in `seqlib.py`, unit-tested on synthetic frames)

| Command | Measures | Default pass |
|---|---|---|
| `flicker <seq> [--static] [--mask m.png]` | **A-B-A toggle:** luma moves more than 24/255 and returns within 8 of it within 3 frames. The expected-motion mask covers tracked rects that moved (±4 px) and the pointer (±24 px) | No pixel toggles more than twice in any 10 s (FIX_PLAN). Writes `flicker_heat.png` and `flicker.txt` |
| `flash <seq>` | **Flash meter (G3, gap 2):** per 8×6 cell, fast luma swings ≥20 (steep part ≤6 frames); flashes/s = changes/2 in any 1 s; full-field changes (≥50% of cells together); red ones (≥25% of the screen saturated red) | ≤3 flashes/s, no red full-field flash |
| `latency <seq> [--roi] [--track name] [--need-cue]` | **Latency probe (check 2, gap 6):** per press in `events.tsv`, frames to the first change (≥12 px over 12 luma; the pointer's area is excluded), and to the nearest `audio` event | First change ≤1 frame. With `--need-cue`, the cue is on the press frame ±1 |
| `hitches <seq>` | `dtMs` of a timing sequence (recorded after `realtime 1`) | No frame over 33.4 ms |
| `frozen <seq>` / `cuts <seq>` | BUG-31's longest still run / BUG-32's single-frame full-screen jumps (≥60% of pixels change by more than 40) | ≤0.25 s / none |
| `all <seq>` | flicker + flash + latency (when there are presses); on a timing sequence, hitches | All |
| `gif`, `sheet`, `side`, `blockiness`, `import-run` | GIF with the pointer drawn; contact sheet; side by side with a reference; the 8×8 block ratio; run.json → results | - |

- **Result ids:** with `--result-dir`, each analysis writes `<scenario>.seq.<name>.<analysis>` (`bp.`-prefixed with `--bp`), stamped with git HEAD and the tree's state, or `--commit` and `--dirty`.
- **In the editor**, the harness runs `all` on every sequence after every run, with python on the PATH.

## 7. Feel automation (GAME_FEEL_BIBLE §5.2 #4)

- **Camera causes (G9, gap 3).**
  - Game code calls `QaSignals.TagCamera(cause, seconds, capPx)` when a move starts:
    - CAM: `ViewKey` for each view-key blend, and `Impulse` with the §1.3 cap for each shake from CameraFeel;
    - DOC-HANDLE: `DeskDropTilt` for the D1 drop tilt;
    - DESK-HOMES: `SlideInFocus` for the D6 push-in.
  - The logger fails any move with no open window. An open window lasts its seconds plus 0.1 s of settle.
  - **Measurement:** the picture shift is the mean screen motion of five points 2 m ahead, in px at 1080p, so it covers rotation, translation and zoom. Below 0.1 px is noise.
  - An impulse's largest offset from its starting pose must stay ≤ its cap.
  - A switch to another camera is a cut, not a move.
  - **Reduced Motion check (#3):** an RM clip must show no `Impulse` tags, or 0 px offsets.
- **Flash meter (G3, gap 2).** `seqtools flash` on any sequence. FlashPolicy (UI-PC) owns the effects. Each flash should also raise `QaSignals.Event("flash", name)`, so reviewers can line the meter up with its cause.
- **Input-latency probe (check 2, gap 6).** Presses are logged on the frame the game sees them. Cues come from `QaSignals.Event("audio", cue)`, which FEEDBACK raises where it plays a cue. Then run `seqtools latency --track <object>` on the element's sequence.
- **Hit-stop log (check 6).** The `timeScale` column of `frames.tsv`. FEEDBACK can add `QaSignals.Event("hitstop", moment)`.
- **Audio coverage (check 11, BUG-33).** `events.tsv` channel `audio`. FEEDBACK owns the coverage check, as a gate in its own Checks file.

## 8. Recipes by track (use these; each adds its own scenario and checks)

Every recipe:
1. Writes `tools/qa/scenarios/<track>_<what>.json`, owner = the track.
2. Runs it with `QaJobs.Scenario` in the editor, and with `run_player.py` for [BP].
3. Registers its checks in `Checks/<Track>Checks.cs` on the result ids.
4. Puts captures and GIFs in `R7\FIX\<TRACK>\` with a RESULT.md giving the numbers.

The pass lines are the plan's. The rig only measures them.

| Track | Recipe on the rig | Pass line (FIX_PLAN v1 + v2) |
|---|---|---|
| **DESK-PLAN** | `res` A1/A2 (A3/A4 at #6); `click` each kept object by path; `track` it; a registered op for the ID-buffer visibility. Decor: `drop` with paths Fast/Slow/Onto/Edge, `f` 8 | ≥90% visible and centre-clickable; decor at rest ≤3 cm (`drop` ≤8 px); 0 drag-clicks |
| **CAM** | Tag every move (§7). `aspect_sweep.json` days 1 and 12; a reading-toggle and city `recStart`; `cuts` on the city sequence; `Recorder.Column("alpha:<control>", ...)` for the control fade | 0 untagged moves (`qa.play.camera` turns green, DOC-HANDLE's tilt at #8); no cut over 40% luma; parallax ≤1 px/frame; RM: 0 animated frames |
| **STAMP** | `drag` dater paths onto `target2` = the aimed paper, ×10 per case; `track` the rack; a 10 s rack `record` at H2/H5 → `flicker` [BP] | 30/30 on the aimed paper; rack-over-paper 0 (a rect test in its op); rack flicker pass |
| **DOC-HANDLE** | The 40-drop matrix: one `drop` per path and target (counter, desk, folder, beside each machine, Edge, Off), A1/A2, days 1, 5, 12 (`jumpDay` + `cheat GiveAllItems`), H2/H5; `track` each paper; Esc/Q/A/Tab with `key` during a drag; call-next `realtime 1` + `hitches` [BP]; per-call-site GC | Drop ≤8 px (v2 D1: release frame); 0 backward; scale ±1%; 0 camera from drops; no frame over 33 ms at call-next; 0 B/frame from ChipReader and DeskController |
| **DOC-LOOK** | `still` per form at A1/A2; a registered cap-height op on `TMP_Text` rects; folder and passport `recStart` with `track` → `flicker --mask` (print region); a hover sweep with `move` | Cap ≥10/7 px; print-region flicker pass; hover IoU ≥0.9 (its op) |
| **TOOLS** | Torch and scanner `drag` over a planted paper (`cheat ForceAppearance`); `click` the chip reader ×10 | 10/10 clicks; reveal ≥95% unoccluded (its op) |
| **HALL-FX** | [BP] `record` 10 s in office and city at H1-H5, A1-A4 → `flicker`; the Helix monitor before and after a wrong call (`cheat SetStability`) | Flicker pass (`qa.play.flicker` and `qa.bp.flicker` leave the known-red list); the AVAILABLE sign steady ±3 luma |
| **LIGHT** | `hour` ladder stills [BP]; the lamp switch-on `record` (clock at speed); load sequences at 13:00 and 16:00 | Mat luma ≥55 at every hour; frame 0 at the clock's hour; GPU ≤4 ms (profiler) |
| **TEXT-LANG** | `probe` on every still (`liberationSans.*`); days 7-9 in sequence (`day_sequence.json` cut down); the TMP dump in its op | 0 LiberationSans (`qa.*.liberationSans` leave the list) |
| **UI-PC** | A PC session: `click` the PC, `button` by `Type.field`, `key` F1; `cuts` and `frozen` on open/close; FlashPolicy events | No single-frame PC jump; ≤3 flashes/s (`qa.play.flash`) |
| **HOME** | `waitScene HomeScene`; `still` and `probe` at A1-A4; `call RunManager.Sleep` | On-screen rects ×4 aspects |
| **RUN-SAFE** | `day_jumps.json` (owner RUN-SAFE) plus restarts from days 1, 7 and 12; the overlay-block op (cheat panel open, `click` behind it) | Each lands at the day's briefing; console clean (`day_jumps.console`); 0 input behind the overlay |
| **FLOW** | `frozen` and `cuts` on every transition sequence; `realtime 1` for loads; the last traveller's payoff before the report | No frame held >0.25 s; no single-frame full-screen jump; load ≤1.0 s [BP] |
| **FEEDBACK** | `QaSignals.Event("audio", cue)` at every cue; `latency --need-cue`; `QaSignals.Event("hitstop", ...)` with the `timeScale` column | Cue on the press frame ±1; hit-stop sum ≤150 ms per case |
| **CONTENT** | `day_sequence.json` days 1-15 (D21); the guide plate stills | Reachability and teaching coverage (its static checks) |
| **INTEGRATION** | After each merge: `QaJobs.SelfTest`, `QaJobs.Smoke`, every landed recipe, then `QaJobs.Gate` | The merge gate passes. Known-red entries leave as their owners merge |
| **Phase B** | §4.0 item 6: the criteria in this vocabulary; their checks name the track, and DemoGate then needs the approval line | Per the approved spec |

## 9. Verification

**Done here (outside Unity; Saleh was on the PC):**

| Check | Result |
|---|---|
| `compile_check_t6.py` | 0 errors (Assembly-CSharp-Editor and TimeDeskEditMode) |
| House runner | passed 3714, failed 0 (unchanged: the rig adds no TimeDeskEditMode test) |
| `tools/qa/editor_runner` (QA_PROJECT set) | passed 68 (59 QA-RIG + the carve's 9 Editor-side seam tests), failed 0; 16 NeedsEditor skipped (12 carve + 4 QA-RIG) |
| `python -m unittest discover -s tools/qa` | 34 tests OK (flicker, flash, latency, hitches, frozen, cuts, blockiness, results and dirty, the known-red verdict, timing sequences, the CLI end to end, scenario ops) |
| QA player compile (Assembly-CSharp without UNITY_EDITOR, with TIMEDESK_QA + DEMO_CHEATS) | 0 errors; the demo configuration (no TIMEDESK_QA) also 0 errors (a scratch csproj, editor DLLs removed) |
| TDD | The SO scan found the 8 dead fields, now listed. After the review: the known-red verdict, dirty results and the churn filter, and the quality file's Standalone level have pure tests (DemoGateTests, QaQualityTests, test_seqlib) |

**Deferred to Unity:** the persistent editor, a QA player build, and Saleh away from the PC. Also from the review fixes: an aborted run (stop `QaJobs.Smoke` with `QaJobs.Reset` mid-run) leaves no disabled device, no QaMouse and a failed run.json; a smoke day's `presses` passes with no `direct` step (any scenario step that hits a covered target needs a look before it gets `direct`); `bp.bp_smoke.gc.steady` reads only the game's allocations; the QA player writes under `LocalLow/<company>/<product> QA` and leaves the demo's save and registry key untouched.

1. **Mechanism proof (§1.1).**
   - Run `QaRigPlayTests.SelfTest_EveryPathLandsWithin1Px` by name, then the job `QaJobs.SelfTest`.
   - Plant a 2 px offset in `QaInput.Set` on a scratch copy. Both must fail, then pass once the offset is removed.
   - Record the result here.
2. **The four NeedsEditor tests:**
   - `QaEditorGateTests` (missing-script gate, integrity scan, scenarios read, registry);
   - the EditMode suite as a whole, including the UnitySkills known failure.
3. **The lever:**
   - Build Office UI. `Office/GateLever` is removed, the semantic dump differs from CARVE's only by it, and the builder logs no integrity error.
   - Office at A4, H2 (`aspect_sweep.json`): no magenta, 0 missing-script warnings.
4. **Planted bugs, on a scratch branch: the gate must fail on each, and name it.**
   - ClearOfBlockers push: a `drop` on a paper fails `drop.*`.
   - CityHash zero blink: [BP] `flicker` fails.
   - LiberationSans on DETAIN: the `probe` fails `liberationSans.*`, and the demo gate refuses.
5. **The DOC audit re-run:** port the audit's 38 sequences (`R7\AUDIT\DOC\tools\_TimeDeskDocAudit.cs.txt`: handover, drags, folder, stamp, per day) as `doc_audit.json`. This uses drag paths from paper objects; papers are named at runtime, so they need a registered op that finds a paper by index (DOC-HANDLE's). The numbers must reproduce PLAY-2 and DOC-4 within 10%.
6. **The perf baseline [BP]:**
   - `QaJobs.GcSites` in the editor, then `bp_smoke.json` on the QA player.
   - Pass: `gc_sites.tsv` names the PLAY-20 sites (the IMGUI overlay → RUN-SAFE, the machines → DOC-HANDLE and TOOLS, MachineCloseUp → CAM, cut in P0-C), the call-next frame times are recorded, and hall GPU time is taken from the profiler on the reference machine.
7. **The known-red list:** the first `QaJobs.Smoke` + `QaJobs.Gate` confirms each entry. Drop any entry that is green, and add any red one with its BUG and owner.

## 10. For INTEGRATION (FEATURES.md and the shared docs)

**FEATURES.md lines (new):**
- "DemoBuild refuses to build a demo while DemoGate (every registered gate check) is red; the report is Library/QA/DemoGate/report_demo.md."
- "The Office UI builder removes retired objects (Office/GateLever) and logs missing scripts and null or error materials in the layer."
- "Every QA run plays at the Standalone quality level (Ultra); the editor's own level is unchanged."
- "The QA player is built under its own product name (+ " QA"), so its saves and settings never touch the game's."

**Drop:** the `_TimeDeskQASweep` mention.

**CHEATS.md:** no change. The rig uses the existing cheats.

**After the merge:**
- Rebuild OfficeGameplay. The lever is gone.
- Run the deferred list above.
