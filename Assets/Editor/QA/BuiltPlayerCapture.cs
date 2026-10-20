using System;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// The QA player ([BP]; BUG-53: "the editor renders differently: quality 0,
/// frozen SSAO, half resolution", so flicker, SSAO, mipmap, load-time and GC
/// criteria are measured on a built player). Builds the demo's scenes as a
/// development player with the rig compiled in (TIMEDESK_QA) and the cheat
/// menu (DEMO_CHEATS, for the cheat paths) to a folder outside the project,
/// <see cref="DefaultExePath"/>, and writes qa_build.json beside the exe
/// with the commit it was built from. The player plays a scenario when
/// started with -qaScenario &lt;file&gt; -qaOut &lt;folder&gt; and quits at its
/// end; tools/qa/run_player.py launches it, waits, imports the run's
/// assertions as "bp."-prefixed gate results stamped with that commit, and
/// runs the frame analyses on its sequences. Never a demo: DemoBuild's gate
/// does not run (DemoBuild.BuildScratch).
/// </summary>
public static class BuiltPlayerCapture
{
    /// <summary>The scripting define that compiles the rig into a player.</summary>
    public const string QaDefine = "TIMEDESK_QA";

    /// <summary>Where the QA player goes: E:\unity\NOPE-builds\QA_&lt;project folder&gt;\TimeSorter.exe (one per worktree).</summary>
    public static string DefaultExePath =>
        Path.Combine(@"E:\unity\NOPE-builds", "QA_" + Path.GetFileName(GateResults.ProjectRoot), "TimeSorter.exe");

    /// <summary>Builds the QA player to <see cref="DefaultExePath"/>; returns the build's result.</summary>
    public static BuildResult Build()
    {
        string exe = DefaultExePath;
        BuildResult result = DemoBuild.BuildScratch(exe, new[] { DemoBuild.CheatsDefine, QaDefine }, true);
        if (result == BuildResult.Succeeded)
        {
            var info = new QaBuildInfo
            {
                commit = GitHead.Read(GateResults.ProjectRoot),
                project = GateResults.ProjectRoot,
                built = DateTime.Now.ToString("s"),
                quality = QaQuality.Read(GateResults.ProjectRoot).StandaloneDefault,
            };
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(exe), "qa_build.json"), JsonUtility.ToJson(info, true));
            Debug.Log($"[QA] QA player built at {exe} from {info.commit}. Run it with tools/qa/run_player.py.");
        }
        return result;
    }

    /// <summary>qa_build.json: what the QA player was built from (run_player.py stamps imported results with it).</summary>
    [Serializable]
    public sealed class QaBuildInfo
    {
        /// <summary>The commit built.</summary>
        public string commit;

        /// <summary>The project folder it was built from.</summary>
        public string project;

        /// <summary>When (local, ISO).</summary>
        public string built;

        /// <summary>The Standalone quality level the player boots at.</summary>
        public int quality;
    }
}
