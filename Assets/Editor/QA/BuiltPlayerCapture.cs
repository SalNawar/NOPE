using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// The QA player ([BP]; BUG-53: "the editor renders differently: quality 0,
/// frozen SSAO, half resolution", so flicker, SSAO, mipmap, load-time and GC
/// criteria are measured on a built player). Builds the demo's scenes as a
/// development player with the rig compiled in (TIMEDESK_QA) and the cheat
/// menu (DEMO_CHEATS, for the cheat paths) to a folder outside the project,
/// <see cref="DefaultExePath"/>, and writes qa_build.json beside the exe
/// with the commit it was built from (and whether the tree was dirty). The
/// player is built under its own product name (the game's plus
/// <see cref="QaRunner.QaProductSuffix"/>), so its saves, PlayerPrefs and
/// window settings (persistentDataPath and the registry key both follow the
/// product name) never touch the real game's; the player refuses a scenario
/// under any other name. The project's PlayerSettings are put back after
/// the build, on disk too. The player plays a scenario when started with
/// -qaScenario &lt;file&gt; -qaOut &lt;folder&gt; and quits at its end;
/// tools/qa/run_player.py launches it, waits, imports the run's assertions
/// as "bp."-prefixed gate results stamped with that commit, and runs the
/// frame analyses on its sequences. Never a demo: DemoBuild's gate does not
/// run (DemoBuild.BuildScratch).
/// </summary>
public static class BuiltPlayerCapture
{
    /// <summary>The scripting define that compiles the rig into a player.</summary>
    public const string QaDefine = "TIMEDESK_QA";

    /// <summary>The project settings file that holds the product name (restored byte for byte after the build).</summary>
    private const string ProjectSettingsAsset = "ProjectSettings/ProjectSettings.asset";

    /// <summary>Where the QA player goes: E:\unity\NOPE-builds\QA_&lt;project folder&gt;\TimeSorter.exe (one per worktree).</summary>
    public static string DefaultExePath =>
        Path.Combine(@"E:\unity\NOPE-builds", "QA_" + Path.GetFileName(GateResults.ProjectRoot), "TimeSorter.exe");

    /// <summary>The QA player's product name for a game named <paramref name="product"/>.</summary>
    private static string QaProductName(string product) =>
        product.EndsWith(QaRunner.QaProductSuffix, StringComparison.Ordinal) ? product : product + QaRunner.QaProductSuffix;

    /// <summary>Builds the QA player to <see cref="DefaultExePath"/> under the QA product name; returns the build's result.</summary>
    public static BuildResult Build()
    {
        string exe = DefaultExePath;
        string product = PlayerSettings.productName;
        string settingsFile = Path.Combine(GateResults.ProjectRoot, ProjectSettingsAsset);
        byte[] settingsBefore = File.Exists(settingsFile) ? File.ReadAllBytes(settingsFile) : null;
        // The tree's state before the build sets the QA product name (it may save ProjectSettings.asset).
        bool dirty = GitDirty.Changed(GateResults.ProjectRoot).Count > 0;
        BuildResult result;
        try
        {
            PlayerSettings.productName = QaProductName(product);
            result = DemoBuild.BuildScratch(exe, new[] { DemoBuild.CheatsDefine, QaDefine }, true);
        }
        finally
        {
            PlayerSettings.productName = product;
            // The build may have saved the settings with the QA name: the file goes back as it was.
            if (settingsBefore != null && !File.ReadAllBytes(settingsFile).SequenceEqual(settingsBefore))
                File.WriteAllBytes(settingsFile, settingsBefore);
        }
        if (result == BuildResult.Succeeded)
        {
            QaQuality.Levels levels = QaQuality.Read(GateResults.ProjectRoot);
            var info = new QaBuildInfo
            {
                commit = GitHead.Read(GateResults.ProjectRoot),
                dirty = dirty,
                project = GateResults.ProjectRoot,
                product = QaProductName(product),
                built = DateTime.Now.ToString("s"),
                quality = levels.StandaloneDefault,
                qualityName = levels.Name(levels.StandaloneDefault),
            };
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(exe), "qa_build.json"), JsonUtility.ToJson(info, true));
            Debug.Log($"[QA] QA player '{info.product}' built at {exe} from {info.commit}{(info.dirty ? " with uncommitted changes (its results will count as dirty)" : string.Empty)}. Run it with tools/qa/run_player.py.");
        }
        return result;
    }

    /// <summary>qa_build.json: what the QA player was built from (run_player.py stamps imported results with it).</summary>
    [Serializable]
    public sealed class QaBuildInfo
    {
        /// <summary>The commit built.</summary>
        public string commit;

        /// <summary>True when the tree had uncommitted changes (GitDirty): the player is not a build of <see cref="commit"/>, and its results count as dirty.</summary>
        public bool dirty;

        /// <summary>The project folder it was built from.</summary>
        public string project;

        /// <summary>The player's product name (the game's plus " QA"): run_player.py refuses a player of another name.</summary>
        public string product;

        /// <summary>When (local, ISO).</summary>
        public string built;

        /// <summary>The Standalone quality level the player boots at.</summary>
        public int quality;

        /// <summary>That level's name (run_player.py checks the run played at it).</summary>
        public string qualityName;
    }
}
