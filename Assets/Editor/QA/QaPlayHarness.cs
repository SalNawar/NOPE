using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

/// <summary>
/// The editor's play harness (P0-B: both test mechanisms run on it). It sets
/// the editor up like the player (<see cref="Prepare"/>: the Standalone
/// quality level, the game view at 1920x1080, all input to the game view,
/// the player's saves set aside, the start scene open), starts a QaRunner on
/// the scenario in play mode (<see cref="BeginRun"/>), imports the run's
/// assertions as gate results when it ends (<see cref="Collect"/>) and puts
/// everything back on an empty scene in edit mode (<see cref="Restore"/>).
/// <see cref="Start"/> chains them for the job runner (the fallback
/// mechanism: the job returns at once, and the run finishes on its own);
/// QaPlayTest chains them inside a UTF [UnityTest] (the preferred one). State
/// lives in SessionState, so it survives the domain reload of entering play
/// mode; the saves set aside are also named in a file beside them, so an
/// editor that crashed or restarted mid-run puts them back when it next loads
/// (<see cref="RecoverSaves"/>). One run at a time; its folder is
/// Library/QA/runs/&lt;scenario&gt;_&lt;time&gt;, named by Library/QA/last_run.txt.
/// After the restore, <see cref="Analyze"/> runs tools/qa/seqtools.py on
/// every recorded sequence (flicker, flash, cuts, latency, press-to-sound
/// and the frame-against-still colour on a captured one; hitches on a timing
/// one; analysis.txt) and only then writes harness.txt, whose first word is
/// the verdict on the run and its frames together (HarnessVerdict: the run's
/// own line against the known-red list, DemoGate.JudgeRun, joined with every
/// red analysis line; "ok" only when nothing failed but known red; GATE-07
/// of 2026-10-28: the line was written before the analysis and ignored it).
/// harness.txt is the run's last file: whoever waits for it reads a run whose
/// frames are judged.
/// </summary>
[InitializeOnLoad]
public static class QaPlayHarness
{
    private const string StepKey = "QaRig.Step";
    private const string ScenarioKey = "QaRig.Scenario";
    private const string OutKey = "QaRig.Out";
    private const string BegunKey = "QaRig.Begun";
    private const string DeadlineKey = "QaRig.Deadline";
    private const string ViewWKey = "QaRig.ViewW";
    private const string ViewHKey = "QaRig.ViewH";
    private const string InputKey = "QaRig.Input";
    private const string BackgroundKey = "QaRig.Background";
    private const string QualityKey = "QaRig.Quality";
    private const string ResultKey = "QaRig.Result";
    private const string PreparedKey = "QaRig.Prepared";
    private const string DirtyKey = "QaRig.Dirty";
    private const string RunLineKey = "QaRig.RunLine";
    private const string RunOkKey = "QaRig.RunOk";
    private const string ImportedKey = "QaRig.Imported";
    private const string VerdictKey = "QaRig.Verdict";
    private const string VerdictOkKey = "QaRig.VerdictOk";
    private const int Idle = 0, Playing = 1, Finishing = 2;

    /// <summary>The game view's resolution name while QA plays.</summary>
    public const string ViewName = "QA";

    /// <summary>The scene a game run starts from (the player boots it).</summary>
    public const string TitleScene = "Assets/Scenes/TitleScene.unity";

    /// <summary>True while a run is prepared and not yet restored.</summary>
    public static bool Busy => SessionState.GetBool(PreparedKey, false);

    /// <summary>The runs' folder: &lt;project&gt;/Library/QA/runs.</summary>
    public static string RunsFolder => Path.Combine(GateResults.ProjectRoot, "Library", "QA", "runs");

    /// <summary>The current (or last) run's folder.</summary>
    public static string OutDir => SessionState.GetString(OutKey, string.Empty);

    static QaPlayHarness()
    {
        if (SessionState.GetInt(StepKey, Idle) != Idle)
            EditorApplication.update += Tick;
        EditorApplication.delayCall += RecoverSaves;
    }

    /// <summary>
    /// When no run is prepared in this editor session but saves are still set
    /// aside (an editor that crashed or restarted mid-run: SessionState is
    /// gone, the files are not), puts them back and drops what the run saved.
    /// </summary>
    private static void RecoverSaves()
    {
        if (Busy || !File.Exists(AsideManifest))
            return;
        PutBack();
        Debug.Log($"[QA] Put back the saves a QA run had set aside in {SaveSystem.Folder} (the run did not finish: the editor stopped mid-run).");
    }

    /// <summary>
    /// The job runner's way: prepares the scenario at
    /// <paramref name="scenarioPath"/> from <paramref name="startScene"/>
    /// (empty: an empty scene, for the self-test), enters play mode and lets
    /// the run finish on its own, stopping it after
    /// <paramref name="maxMinutes"/>. Returns the run's folder, or null when
    /// it refused (the reason is logged).
    /// </summary>
    public static string Start(string scenarioPath, string startScene = TitleScene, float maxMinutes = 30f)
    {
        string outDir = Prepare(scenarioPath, startScene, maxMinutes);
        if (outDir == null)
            return null;
        SessionState.SetInt(StepKey, Playing);
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
        return outDir;
    }

    /// <summary>
    /// In edit mode: reads the scenario, makes the run's folder, sets the
    /// editor up like the player and opens <paramref name="startScene"/>.
    /// Returns the run's folder, or null when it refused (a run in progress,
    /// play mode, an unreadable scenario).
    /// </summary>
    public static string Prepare(string scenarioPath, string startScene = TitleScene, float maxMinutes = 30f)
    {
        if (Busy || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[QA] A run is in progress or the editor is in play mode: not started.");
            return null;
        }
        QaScenario scenario;
        try
        {
            scenario = QaScenario.Load(scenarioPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[QA] Cannot read the scenario {scenarioPath}: {e.Message}");
            return null;
        }
        string outDir = Path.Combine(RunsFolder, $"{QaRunner.Safe(scenario.name)}_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(outDir);
        File.Copy(scenarioPath, Path.Combine(outDir, "scenario.json"), true);
        File.WriteAllText(Path.Combine(GateResults.ProjectRoot, "Library", "QA", "last_run.txt"), outDir);

        SessionState.SetBool(PreparedKey, true);
        SessionState.SetString(ScenarioKey, scenarioPath);
        SessionState.SetString(OutKey, outDir);
        SessionState.SetBool(BegunKey, false);
        SessionState.SetFloat(DeadlineKey, (float)(EditorApplication.timeSinceStartup + maxMinutes * 60f));
        SessionState.SetString(ResultKey, string.Empty);
        SessionState.SetString(RunLineKey, string.Empty);
        SessionState.SetBool(RunOkKey, false);
        SessionState.SetInt(ImportedKey, -1);
        SessionState.SetString(VerdictKey, string.Empty);
        SessionState.SetBool(VerdictOkKey, false);
        // The tree's state before the rig touches a setting (the quality level below is saved into QualitySettings.asset).
        SessionState.SetBool(DirtyKey, GitDirty.Changed(GateResults.ProjectRoot).Count > 0);

        SetAside();
        PlayModeWindow.GetRenderingResolution(out uint w, out uint h);
        SessionState.SetInt(ViewWKey, (int)w);
        SessionState.SetInt(ViewHKey, (int)h);
        PlayModeWindow.SetCustomRenderingResolution(1920, 1080, ViewName);
        SessionState.SetInt(InputKey, (int)InputSystem.settings.editorInputBehaviorInPlayMode);
        SessionState.SetInt(BackgroundKey, (int)InputSystem.settings.backgroundBehavior);
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        SessionState.SetInt(QualityKey, QualitySettings.GetQualityLevel());
        QaQuality.Levels levels = QaQuality.Read(GateResults.ProjectRoot);
        if (levels.StandaloneDefault >= 0)
            QualitySettings.SetQualityLevel(levels.StandaloneDefault, true);

        if (string.IsNullOrEmpty(startScene))
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        else
            EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
        Debug.Log($"[QA] Prepared {scenario.name} into {outDir}.");
        return outDir;
    }

    /// <summary>In play mode: starts the prepared scenario once (later calls return the same run), asserting it plays at the Standalone player's quality level.</summary>
    public static QaRunner BeginRun()
    {
        if (SessionState.GetBool(BegunKey, false) && QaRunner.Active != null)
            return QaRunner.Active;
        SessionState.SetBool(BegunKey, true);
        QaQuality.Levels levels = QaQuality.Read(GateResults.ProjectRoot);
        return QaRunner.Begin(QaScenario.Load(SessionState.GetString(ScenarioKey, string.Empty)), OutDir, false,
            levels.StandaloneDefault >= 0 ? levels.Name(levels.StandaloneDefault) : null);
    }

    /// <summary>True when the prepared run's deadline has passed.</summary>
    public static bool PastDeadline => EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, float.MaxValue);

    /// <summary>
    /// After the run ended (still in play mode): imports its assertions as
    /// gate results and keeps the run's own verdict against the known-red
    /// list (<see cref="DemoGate.JudgeRun"/>: "ok" when only known-red
    /// assertions failed; run.json's ok stays the strict one) for
    /// <see cref="Analyze"/>, which writes harness.txt once the frames are
    /// judged too. Returns its report (null when it did not finish).
    /// </summary>
    public static QaRunReport Collect()
    {
        QaRunner runner = QaRunner.Active;
        if (runner == null || !runner.Done)
        {
            KeepRunVerdict("timeout: the run did not finish before its deadline", false, "timeout");
            return null;
        }
        SessionState.SetInt(ImportedKey, GateResults.Import(runner.Report, OutDir, false, SessionState.GetBool(DirtyKey, false)));
        RunVerdict verdict = DemoGate.JudgeRun(runner.Report, DemoGate.KnownRedRunPatterns());
        KeepRunVerdict(verdict.Line, verdict.Ok, verdict.Ok ? "ok" : "failed");
        return runner.Report;
    }

    /// <summary>Keeps the run's own verdict line for harness.txt (written by <see cref="Analyze"/>).</summary>
    private static void KeepRunVerdict(string line, bool ok, string result)
    {
        SessionState.SetString(ResultKey, result);
        SessionState.SetString(RunLineKey, line);
        SessionState.SetBool(RunOkKey, ok);
    }

    /// <summary>The verdict harness.txt's first line gave on the last analysed run (HarnessVerdict), or empty before <see cref="Analyze"/>.</summary>
    public static string VerdictLine => SessionState.GetString(VerdictKey, string.Empty);

    /// <summary>True when the last analysed run and its frames passed but for known red (harness.txt's first word "ok").</summary>
    public static bool VerdictOk => SessionState.GetBool(VerdictOkKey, false);

    /// <summary>The finished run's report, read back from its run.json (works after a domain reload), or null.</summary>
    public static QaRunReport ReadReport()
    {
        string file = Path.Combine(OutDir, "run.json");
        return File.Exists(file) ? JsonUtility.FromJson<QaRunReport>(File.ReadAllText(file)) : null;
    }

    private static void Tick()
    {
        int step = SessionState.GetInt(StepKey, Idle);
        if (step == Finishing)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetInt(StepKey, Idle);
                EditorApplication.update -= Tick;
                Restore();
                Analyze();
            }
            return;
        }
        if (step != Playing)
        {
            EditorApplication.update -= Tick;
            return;
        }
        if (PastDeadline)
        {
            Collect();
            Stop();
            return;
        }
        if (!EditorApplication.isPlaying)
            return;
        QaRunner runner;
        try
        {
            runner = BeginRun();
        }
        catch (Exception e)
        {
            KeepRunVerdict($"failed: the run could not start: {e.Message}", false, "failed");
            Stop();
            return;
        }
        if (!runner.Done)
            return;
        Collect();
        Stop();
    }

    /// <summary>Gets the editor out of a run that cannot finish (a job that died mid-run): leaves play mode if needed and restores everything.</summary>
    public static void Reset()
    {
        SessionState.SetInt(StepKey, Idle);
        EditorApplication.update -= Tick;
        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
        else
            Restore();
    }

    private static void Stop()
    {
        SessionState.SetInt(StepKey, Finishing);
        if (EditorApplication.isPlaying)
            EditorApplication.ExitPlaymode();
    }

    /// <summary>In edit mode, after the run: the game view, the input routing and devices, the quality level and the saves as they were, on an empty scene.</summary>
    public static void Restore()
    {
        if (!Busy)
            return;
        PlayModeWindow.SetCustomRenderingResolution((uint)SessionState.GetInt(ViewWKey, 1920), (uint)SessionState.GetInt(ViewHKey, 1080), ViewName);
        InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(InputKey, 0);
        // A run stopped from outside may not have given the input back: the real mouse and keyboard, no virtual device, the focus rule.
        int devices = QaInput.ReleaseStray();
        if (devices > 0)
            Debug.Log($"[QA] Gave back {devices} input device(s) a stopped run had left disabled or added.");
        InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)SessionState.GetInt(BackgroundKey, (int)InputSystem.settings.backgroundBehavior);
        QualitySettings.SetQualityLevel(SessionState.GetInt(QualityKey, QualitySettings.GetQualityLevel()), true);
        PutBack();
        SessionState.SetBool(PreparedKey, false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log($"[QA] Run finished ({SessionState.GetString(ResultKey, "?")}): {OutDir}");
    }

    /// <summary>
    /// Runs tools/qa/seqtools.py (python on the PATH) on every sequence the
    /// run recorded (<c>all</c>: flicker, flash, cuts, latency, press-to-sound
    /// and colour on a captured one, hitches on a timing one), writing gate
    /// results stamped with HEAD and the tree's state (ids
    /// &lt;scenario&gt;.seq.&lt;name&gt;.&lt;analysis&gt;) and the output to the run's
    /// analysis.txt; then writes harness.txt: its first line the verdict on
    /// the run and its frames together (<see cref="HarnessVerdict"/>, kept as
    /// <see cref="VerdictLine"/>), then the results imported and each red
    /// analysis line. A run that recorded no sequence gets its own verdict.
    /// Returns false when an analysis failed or could not run.
    /// </summary>
    public static bool Analyze()
    {
        string seqRoot = Path.Combine(OutDir, "seq");
        var analyses = new List<SeqAnalysis>();
        if (Directory.Exists(seqRoot))
        {
            string tool = Path.Combine(GateResults.ProjectRoot, "tools", "qa", "seqtools.py");
            string head = GitHead.Read(GateResults.ProjectRoot);
            bool dirty = SessionState.GetBool(DirtyKey, false);
            var log = new StringBuilder();
            foreach (string seq in Directory.GetDirectories(seqRoot))
                analyses.Add(Python(log, tool, "all", seq, head, dirty));
            File.WriteAllText(Path.Combine(OutDir, "analysis.txt"), log.ToString());
        }
        bool ok = analyses.All(a => a.Ran && a.ExitOk);
        if (analyses.Count > 0)
            Debug.Log($"[QA] Frame analysis {(ok ? "passed" : "FAILED")}: {Path.Combine(OutDir, "analysis.txt")}");
        WriteHarness(analyses);
        return ok;
    }

    /// <summary>harness.txt, the run's last file: the verdict on the run and its frames (line 1), the results imported, and every red analysis line.</summary>
    private static void WriteHarness(List<SeqAnalysis> analyses)
    {
        HarnessVerdict verdict = HarnessVerdict.Judge(SessionState.GetString(RunLineKey, string.Empty), SessionState.GetBool(RunOkKey, false), OutDir,
                                                      analyses, DemoGate.KnownRedRunPatterns());
        SessionState.SetString(VerdictKey, verdict.Line);
        SessionState.SetBool(VerdictOkKey, verdict.Ok);
        var text = new StringBuilder(verdict.Line).Append('\n');
        int imported = SessionState.GetInt(ImportedKey, -1);
        if (imported >= 0)
            text.Append($"{imported} result(s) imported into {GateResults.Folder}\n");
        if (analyses.Count > 0)
            text.Append($"frame analysis of {analyses.Count} sequence(s) (analysis.txt): {verdict.Unexpected.Count} red, {verdict.KnownRed.Count} known red\n");
        foreach (string line in verdict.Unexpected)
            text.Append("RED ").Append(line).Append('\n');
        foreach (string line in verdict.KnownRed)
            text.Append("KNOWN RED ").Append(line).Append('\n');
        File.WriteAllText(Path.Combine(OutDir, "harness.txt"), text.ToString());
    }

    private static SeqAnalysis Python(StringBuilder log, string tool, string command, string seq, string head, bool dirty)
    {
        var start = new ProcessStartInfo("python", $"\"{tool}\" {command} \"{seq}\" --result-dir \"{GateResults.Folder}\" --commit \"{head}\"{(dirty ? " --dirty" : string.Empty)}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        try
        {
            using (Process p = Process.Start(start))
            {
                string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                p.WaitForExit(300000);
                log.AppendLine($"$ seqtools {command} {seq}").AppendLine(output);
                return new SeqAnalysis { Seq = seq, Ran = true, ExitOk = p.HasExited && p.ExitCode == 0, Output = output };
            }
        }
        catch (Exception e)
        {
            log.AppendLine($"$ seqtools {command} {seq}: could not run python ({e.Message})");
            return new SeqAnalysis { Seq = seq, Ran = false, Output = $"could not run python ({e.Message})" };
        }
    }

    /// <summary>
    /// Moves the player's save files aside (SaveSystem.Folder, per project in
    /// the editor) so a run starts clean. Which slots had a file is written to
    /// <see cref="AsideManifest"/> first, on disk, so the saves can be put back
    /// even when the editor dies mid-run (<see cref="RecoverSaves"/>).
    /// </summary>
    private static void SetAside()
    {
        if (File.Exists(AsideManifest))
            PutBack();
        int had = 0;
        for (int i = 0; i < SaveSystem.Files.Count; i++)
            if (File.Exists(SaveSystem.Files[i]))
                had |= 1 << i;
        Directory.CreateDirectory(SaveSystem.Folder);
        File.WriteAllText(AsideManifest, had.ToString(System.Globalization.CultureInfo.InvariantCulture));
        for (int i = 0; i < SaveSystem.Files.Count; i++)
        {
            if ((had & (1 << i)) == 0)
                continue;
            File.Copy(SaveSystem.Files[i], Backup(i), true);
            File.Delete(SaveSystem.Files[i]);
        }
    }

    /// <summary>Puts the player's saves back exactly as they were (and drops what the run saved), as <see cref="AsideManifest"/> records; nothing when no saves are aside.</summary>
    private static void PutBack()
    {
        if (!File.Exists(AsideManifest))
            return;
        int.TryParse(File.ReadAllText(AsideManifest).Trim(), out int had);
        for (int i = 0; i < SaveSystem.Files.Count; i++)
        {
            if ((had & (1 << i)) != 0 && File.Exists(Backup(i)))
            {
                File.Copy(Backup(i), SaveSystem.Files[i], true);
                File.Delete(Backup(i));
            }
            else if ((had & (1 << i)) == 0 && File.Exists(SaveSystem.Files[i]))
            {
                File.Delete(SaveSystem.Files[i]);
            }
        }
        File.Delete(AsideManifest);
    }

    /// <summary>The file naming which save slots a run set aside (a bit per slot), beside the copies; present only while saves are aside.</summary>
    private static string AsideManifest => Path.Combine(SaveSystem.Folder, "qa_rig_aside.txt");

    private static string Backup(int i) => Path.Combine(SaveSystem.Folder, $"qa_rig_backup_{i}.json");
}
