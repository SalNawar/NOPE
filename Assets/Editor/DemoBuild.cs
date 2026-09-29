using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// The standalone Windows demo build (Saleh 2026-09-30: play the demo as a
/// Windows exe). Builds StandaloneWindows64, a release player (no development
/// build, so the dev overlay and its cheats are compiled out: RunManager adds
/// DebugPanelController only under UNITY_EDITOR or DEVELOPMENT_BUILD), from
/// the enabled scenes of the build settings in their order. It refuses before
/// building when the first scene is not RunConfig's title scene, when a scene
/// the game loads by name (RunConfig's title, art office, gameplay layer and
/// Home) is not enabled, or when the output would land inside Assets. The
/// product name, company and version come from PlayerSettings. Every run writes
/// a summary (result, size, time, errors) to a text file beside the output
/// folder (<see cref="ReportPathFor"/>). Callable from the menu, from the
/// editor's job runner (<see cref="BuildWindowsDemo"/>, parameterless) and
/// from the command line: -executeMethod DemoBuild.BuildWindowsDemo
/// [-demoBuildPath &lt;exe path&gt;].
/// </summary>
public static class DemoBuild
{
    /// <summary>Where the player goes when no path is given: outside the project, so no build output is ever imported or committed.</summary>
    public const string DefaultExePath = @"E:\unity\NOPE-builds\TimeSorter_Demo\TimeSorter.exe";

    /// <summary>The command-line switch whose next argument overrides <see cref="DefaultExePath"/>.</summary>
    public const string PathArgument = "-demoBuildPath";

    /// <summary>Builds the demo to <see cref="DefaultExePath"/>.</summary>
    [MenuItem("Time Sorter/Build Windows Demo")]
    private static void BuildFromMenu() => Build(DefaultExePath);

    /// <summary>
    /// Builds the demo to the command line's <see cref="PathArgument"/>, else
    /// <see cref="DefaultExePath"/>. In batch mode it exits the editor with 0
    /// when the build succeeded and 1 otherwise; in a running editor it returns.
    /// </summary>
    public static void BuildWindowsDemo()
    {
        bool ok = Build(ArgumentAfter(PathArgument) ?? DefaultExePath) == BuildResult.Succeeded;
        if (Application.isBatchMode)
            EditorApplication.Exit(ok ? 0 : 1);
    }

    /// <summary>
    /// Builds the Windows 64-bit release player to <paramref name="exePath"/>
    /// (its folder is created) and writes the summary to <see cref="ReportPathFor"/>.
    /// Returns the build's result (Failed when it refused to start).
    /// </summary>
    public static BuildResult Build(string exePath)
    {
        exePath = Path.GetFullPath(exePath);
        string reportPath = ReportPathFor(exePath);
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        List<string> problems = Problems(exePath, scenes);
        if (problems.Count > 0)
        {
            string refused = Summary(exePath, scenes, null, problems, 0L);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, refused);
            Debug.LogError("[DemoBuild] Refused to build:\n" + refused);
            return BuildResult.Failed;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(exePath));
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None,
        };

        Debug.Log($"[DemoBuild] Building {PlayerSettings.productName} {PlayerSettings.bundleVersion} to {exePath}.");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        string summary = Summary(exePath, scenes, report, problems, FolderBytes(Path.GetDirectoryName(exePath)));
        File.WriteAllText(reportPath, summary);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log("[DemoBuild] " + summary);
        else
            Debug.LogError("[DemoBuild] " + summary);
        return report.summary.result;
    }

    /// <summary>The summary file for a build to <paramref name="exePath"/>: &lt;output folder&gt;_BuildReport.txt beside the output folder, so it never ships inside it.</summary>
    public static string ReportPathFor(string exePath)
    {
        string folder = Path.GetDirectoryName(Path.GetFullPath(exePath));
        string parent = Path.GetDirectoryName(folder);
        return parent == null ? Path.Combine(folder, "BuildReport.txt") : Path.Combine(parent, Path.GetFileName(folder) + "_BuildReport.txt");
    }

    /// <summary>Why the build must not start (none when it may): the output inside the project's Assets, no enabled scene, the title not first, or a scene the game loads by name not enabled.</summary>
    private static List<string> Problems(string exePath, string[] scenes)
    {
        var problems = new List<string>();
        string assets = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (exePath.StartsWith(assets, StringComparison.OrdinalIgnoreCase))
            problems.Add($"The output {exePath} is inside Assets; build outside the project.");

        if (scenes.Length == 0)
        {
            problems.Add("No scene is enabled in the build settings.");
            return problems;
        }

        RunConfigSO config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        if (config == null)
        {
            problems.Add($"Resources/{RunManager.ConfigResourcePath} is missing, so the scenes the game loads cannot be checked.");
            return problems;
        }

        var names = new HashSet<string>(scenes.Select(Path.GetFileNameWithoutExtension));
        if (Path.GetFileNameWithoutExtension(scenes[0]) != config.titleSceneName)
            problems.Add($"The first enabled scene is {scenes[0]}, not the title scene '{config.titleSceneName}' (the player boots scene 0).");
        foreach (string needed in new[] { config.titleSceneName, config.officeSceneName, config.officeGameplaySceneName, config.homeSceneName })
            if (!names.Contains(needed))
                problems.Add($"The game loads the scene '{needed}' by name (RunConfig) but it is not an enabled scene of the build settings.");
        return problems;
    }

    /// <summary>The report's text: what was built, from what, with what result, size, time and messages.</summary>
    private static string Summary(string exePath, string[] scenes, BuildReport report, List<string> problems, long folderBytes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Time Sorter demo build report");
        sb.AppendLine($"Written:  {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Result:   {(report == null ? "Refused" : report.summary.result.ToString())}");
        sb.AppendLine($"Output:   {exePath}");
        sb.AppendLine("Target:   StandaloneWindows64, release (not a development build)");
        sb.AppendLine($"Product:  {PlayerSettings.productName}   Company: {PlayerSettings.companyName}   Version: {PlayerSettings.bundleVersion}   Unity: {Application.unityVersion}");
        sb.AppendLine("Scenes:");
        for (int i = 0; i < scenes.Length; i++)
            sb.AppendLine($"  {i}: {scenes[i]}");

        foreach (string problem in problems)
            sb.AppendLine("Refused:  " + problem);

        if (report != null)
        {
            BuildSummary s = report.summary;
            sb.AppendLine($"Started:  {s.buildStartedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Duration: {s.totalTime:hh\\:mm\\:ss}");
            sb.AppendLine($"Size:     {Megabytes(folderBytes)} on disk (output folder), {Megabytes((long)s.totalSize)} reported by the build");
            sb.AppendLine($"Errors:   {s.totalErrors}   Warnings: {s.totalWarnings}");
            foreach (BuildStepMessage message in report.steps.SelectMany(step => step.messages))
                if (message.type == LogType.Error || message.type == LogType.Exception || message.type == LogType.Assert)
                    sb.AppendLine($"  [{message.type}] {message.content}");
        }
        return sb.ToString();
    }

    /// <summary>A byte count in megabytes, one decimal.</summary>
    private static string Megabytes(long bytes) => $"{bytes / (1024.0 * 1024.0):0.0} MB";

    /// <summary>The bytes of every file under <paramref name="folder"/> (0 when it does not exist).</summary>
    private static long FolderBytes(string folder) =>
        Directory.Exists(folder) ? new DirectoryInfo(folder).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length) : 0L;

    /// <summary>The command-line argument after <paramref name="name"/>, or null when it is absent.</summary>
    private static string ArgumentAfter(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
