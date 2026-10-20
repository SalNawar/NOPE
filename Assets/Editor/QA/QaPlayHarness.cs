using System;
using System.Diagnostics;
using System.IO;
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
/// mode. One run at a time; its folder is Library/QA/runs/&lt;scenario&gt;_&lt;time&gt;,
/// named by Library/QA/last_run.txt, and it ends with harness.txt. After the
/// restore, <see cref="Analyze"/> runs tools/qa/seqtools.py on every recorded
/// sequence (flicker, flash, latency; hitches when recorded in real time), so
/// the editor's runs are gated on their frames too (analysis.txt).
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
    private const string QualityKey = "QaRig.Quality";
    private const string SavesKey = "QaRig.Saves";
    private const string ResultKey = "QaRig.Result";
    private const string PreparedKey = "QaRig.Prepared";
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

        SetAside();
        PlayModeWindow.GetRenderingResolution(out uint w, out uint h);
        SessionState.SetInt(ViewWKey, (int)w);
        SessionState.SetInt(ViewHKey, (int)h);
        PlayModeWindow.SetCustomRenderingResolution(1920, 1080, ViewName);
        SessionState.SetInt(InputKey, (int)InputSystem.settings.editorInputBehaviorInPlayMode);
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

    /// <summary>In play mode: starts the prepared scenario once (later calls return the same run).</summary>
    public static QaRunner BeginRun()
    {
        if (SessionState.GetBool(BegunKey, false) && QaRunner.Active != null)
            return QaRunner.Active;
        SessionState.SetBool(BegunKey, true);
        return QaRunner.Begin(QaScenario.Load(SessionState.GetString(ScenarioKey, string.Empty)), OutDir);
    }

    /// <summary>True when the prepared run's deadline has passed.</summary>
    public static bool PastDeadline => EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, float.MaxValue);

    /// <summary>After the run ended (still in play mode): imports its assertions as gate results and writes harness.txt. Returns its report (null when it did not finish).</summary>
    public static QaRunReport Collect()
    {
        QaRunner runner = QaRunner.Active;
        if (runner == null || !runner.Done)
        {
            SessionState.SetString(ResultKey, "timeout");
            File.WriteAllText(Path.Combine(OutDir, "harness.txt"), "timeout: the run did not finish before its deadline");
            return null;
        }
        int imported = GateResults.Import(runner.Report, OutDir, false);
        SessionState.SetString(ResultKey, runner.Report.ok ? "ok" : "failed");
        File.WriteAllText(Path.Combine(OutDir, "harness.txt"), $"{(runner.Report.ok ? "ok" : "failed")}: {imported} result(s) imported into {GateResults.Folder}");
        return runner.Report;
    }

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
            SessionState.SetString(ResultKey, "failed");
            File.WriteAllText(Path.Combine(OutDir, "harness.txt"), $"failed: the run could not start: {e.Message}");
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

    /// <summary>In edit mode, after the run: the game view, the input routing, the quality level and the saves as they were, on an empty scene.</summary>
    public static void Restore()
    {
        if (!Busy)
            return;
        PlayModeWindow.SetCustomRenderingResolution((uint)SessionState.GetInt(ViewWKey, 1920), (uint)SessionState.GetInt(ViewHKey, 1080), ViewName);
        InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(InputKey, 0);
        QualitySettings.SetQualityLevel(SessionState.GetInt(QualityKey, QualitySettings.GetQualityLevel()), true);
        PutBack();
        SessionState.SetBool(PreparedKey, false);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log($"[QA] Run finished ({SessionState.GetString(ResultKey, "?")}): {OutDir}");
    }

    /// <summary>
    /// Runs tools/qa/seqtools.py (python on the PATH) on every sequence the
    /// run recorded, writing gate results stamped with HEAD (ids
    /// &lt;scenario&gt;.seq.&lt;name&gt;.&lt;analysis&gt;) and the output to the run's
    /// analysis.txt. Returns false when an analysis failed or could not run.
    /// </summary>
    public static bool Analyze()
    {
        string seqRoot = Path.Combine(OutDir, "seq");
        if (!Directory.Exists(seqRoot))
            return true;
        string tool = Path.Combine(GateResults.ProjectRoot, "tools", "qa", "seqtools.py");
        string head = GitHead.Read(GateResults.ProjectRoot);
        var log = new StringBuilder();
        bool ok = true;
        foreach (string seq in Directory.GetDirectories(seqRoot))
        {
            ok &= Python(log, tool, "all", seq, head);
            string meta = Path.Combine(seq, "meta.json");
            if (File.Exists(meta) && File.ReadAllText(meta).Replace(" ", string.Empty).Contains("\"fixedStep\":false"))
                ok &= Python(log, tool, "hitches", seq, head);
        }
        File.WriteAllText(Path.Combine(OutDir, "analysis.txt"), log.ToString());
        Debug.Log($"[QA] Frame analysis {(ok ? "passed" : "FAILED")}: {Path.Combine(OutDir, "analysis.txt")}");
        return ok;
    }

    private static bool Python(StringBuilder log, string tool, string command, string seq, string head)
    {
        var start = new ProcessStartInfo("python", $"\"{tool}\" {command} \"{seq}\" --result-dir \"{GateResults.Folder}\" --commit \"{head}\"")
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
                return p.HasExited && p.ExitCode == 0;
            }
        }
        catch (Exception e)
        {
            log.AppendLine($"$ seqtools {command} {seq}: could not run python ({e.Message})");
            return false;
        }
    }

    /// <summary>Moves the player's save files aside (SaveSystem.Folder, per project in the editor) so a run starts clean.</summary>
    private static void SetAside()
    {
        int had = 0;
        for (int i = 0; i < SaveSystem.Files.Count; i++)
        {
            if (!File.Exists(SaveSystem.Files[i]))
                continue;
            had |= 1 << i;
            File.Copy(SaveSystem.Files[i], Backup(i), true);
            File.Delete(SaveSystem.Files[i]);
        }
        SessionState.SetInt(SavesKey, had);
    }

    /// <summary>Puts the player's saves back exactly as they were (and drops what the run saved).</summary>
    private static void PutBack()
    {
        int had = SessionState.GetInt(SavesKey, 0);
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
    }

    private static string Backup(int i) => Path.Combine(SaveSystem.Folder, $"qa_rig_backup_{i}.json");
}
