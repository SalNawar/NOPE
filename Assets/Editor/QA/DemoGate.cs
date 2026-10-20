using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Which gate runs: the one after every merge, or the stricter one before a demo build.</summary>
public enum GateLevel
{
    /// <summary>After every merge (INTEGRATION's regression): every check except the DemoOnly ones.</summary>
    Merge,
    /// <summary>Before a demo build (DemoBuild): every check.</summary>
    Demo,
}

/// <summary>A gate run's verdict: every result, and whether the gate refuses.</summary>
public sealed class DemoGateReport
{
    /// <summary>The level that ran.</summary>
    public GateLevel Level { get; set; }

    /// <summary>The commit the gate ran at (set by DemoGate.Run).</summary>
    public string Head { get; set; } = string.Empty;

    /// <summary>Every result, in id order.</summary>
    public List<GateResult> Results { get; } = new List<GateResult>();

    /// <summary>True when the gate refuses: a red result, or nothing registered at all.</summary>
    public bool Refused { get; set; }

    /// <summary>Why it refused (one line per red result), empty when it passed.</summary>
    public List<string> Reasons { get; } = new List<string>();

    /// <summary>Red results on the known-red list, which a merge gate reports without refusing (the demo gate refuses them).</summary>
    public List<string> KnownRed { get; } = new List<string>();

    /// <summary>Known-red entries whose check now passes: the list must drop them (INTEGRATION).</summary>
    public List<string> StaleKnownRed { get; } = new List<string>();

    /// <summary>The report as Markdown (Library/QA/DemoGate/report.md).</summary>
    public string ToMarkdown()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# DemoGate ({Level}): {(Refused ? "REFUSED" : "PASS")}");
        sb.AppendLine();
        sb.AppendLine($"Written {DateTime.Now:yyyy-MM-dd HH:mm:ss}, HEAD {(string.IsNullOrEmpty(Head) ? "unknown" : Head)}. {Results.Count(r => r.Pass)} pass, {Results.Count(r => !r.Pass)} fail.");
        sb.AppendLine();
        if (Reasons.Count > 0)
        {
            sb.AppendLine("## Refused because");
            foreach (string reason in Reasons)
                sb.AppendLine("- " + reason);
            sb.AppendLine();
        }
        if (KnownRed.Count > 0)
        {
            sb.AppendLine("## Known red (tools/qa/gate_known_red.txt; open BUG parts with an owner; the demo gate refuses them)");
            foreach (string line in KnownRed)
                sb.AppendLine("- " + line);
            sb.AppendLine();
        }
        if (StaleKnownRed.Count > 0)
        {
            sb.AppendLine("## Known-red entries that now pass (drop them from tools/qa/gate_known_red.txt)");
            foreach (string line in StaleKnownRed)
                sb.AppendLine("- " + line);
            sb.AppendLine();
        }
        sb.AppendLine("## Results by owner");
        foreach (IGrouping<string, GateResult> owner in Results.GroupBy(r => r.Owner).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"### {owner.Key}");
            foreach (GateResult r in owner)
                sb.AppendLine($"- {(r.Pass ? "PASS" : "**FAIL**")} `{r.Id}`: {r.Measured}{(string.IsNullOrEmpty(r.Detail) ? string.Empty : " - " + r.Detail)}");
        }
        return sb.ToString();
    }
}

/// <summary>
/// The demo gate (P0-B QA-RIG; FIX_PLAN §5 exit gate #5, v2 rules 9 and 13):
/// runs every registered <see cref="GateCheckAttribute"/> check (each track's
/// Assets/Editor/QA/Checks/&lt;Track&gt;Checks.cs), and refuses when any is
/// red, a check is badly registered, or none is registered. DemoBuild runs
/// it at <see cref="GateLevel.Demo"/> before building and refuses to build a
/// demo when it refuses; INTEGRATION runs it at <see cref="GateLevel.Merge"/>
/// after every merge (the job QaJobs.Gate). The report goes to
/// Library/QA/DemoGate/report.md and the console.
/// </summary>
public static class DemoGate
{
    /// <summary>The report's folder.</summary>
    public static string ReportFolder => Path.Combine(GateResults.ProjectRoot, "Library", "QA", "DemoGate");

    /// <summary>Runs the merge gate from the menu.</summary>
    [MenuItem("Time Sorter/QA/Run Merge Gate")]
    private static void RunMergeFromMenu() => Run(GateLevel.Merge);

    /// <summary>Runs the demo gate from the menu (what DemoBuild runs).</summary>
    [MenuItem("Time Sorter/QA/Run Demo Gate")]
    private static void RunDemoFromMenu() => Run(GateLevel.Demo);

    /// <summary>Runs every check registered for <paramref name="level"/>, writes the report and logs the verdict.</summary>
    public static DemoGateReport Run(GateLevel level)
    {
        List<RegisteredCheck> checks = GateChecks.All();
        var results = new List<GateResult>(GateChecks.Problems(checks));
        foreach (RegisteredCheck c in checks.Where(c => level == GateLevel.Demo || !c.Info.DemoOnly))
        {
            try
            {
                EditorUtility.DisplayProgressBar("DemoGate", c.Info.Id, 0.5f);
                results.AddRange(c.Run());
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
        DemoGateReport report = Evaluate(level, results, checks.Count, KnownRedList());
        report.Head = GitHead.Read(GateResults.ProjectRoot);
        Directory.CreateDirectory(ReportFolder);
        File.WriteAllText(Path.Combine(ReportFolder, level == GateLevel.Demo ? "report_demo.md" : "report.md"), report.ToMarkdown());
        string verdict = $"[DemoGate] {level}: {(report.Refused ? "REFUSED" : "PASS")} ({report.Results.Count(r => r.Pass)} pass, {report.Results.Count(r => !r.Pass)} fail). Report: {ReportFolder}";
        if (report.Refused)
            Debug.LogError(verdict + "\n" + string.Join("\n", report.Reasons));
        else
            Debug.Log(verdict);
        return report;
    }

    /// <summary>The known-red list's file (QA-RIG's; INTEGRATION keeps it current).</summary>
    public static string KnownRedPath => Path.Combine(GateResults.ProjectRoot, "tools", "qa", "gate_known_red.txt");

    /// <summary>The known-red list: check id (or id pattern with '*') → why it is red and who fixes it.</summary>
    public static Dictionary<string, string> KnownRedList() =>
        File.Exists(KnownRedPath) ? ParseKnownRed(File.ReadAllText(KnownRedPath)) : new Dictionary<string, string>();

    /// <summary>Parses the known-red list: "id  # BUG-n, OWNER: why" per line; blank lines and lines starting with '#' are skipped.</summary>
    public static Dictionary<string, string> ParseKnownRed(string text)
    {
        var list = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string raw in (text ?? string.Empty).Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                continue;
            int hash = line.IndexOf('#');
            string id = (hash >= 0 ? line.Substring(0, hash) : line).Trim();
            if (id.Length > 0)
                list[id] = hash >= 0 ? line.Substring(hash + 1).Trim() : string.Empty;
        }
        return list;
    }

    /// <summary>
    /// The verdict on <paramref name="results"/> (pure; DemoGateTests):
    /// refused on any red result, or when <paramref name="registered"/> is 0.
    /// At <see cref="GateLevel.Merge"/> a red result on
    /// <paramref name="knownRed"/> (an open BUG part a later track owns) is
    /// listed but does not refuse, so the merge gate catches regressions only;
    /// the demo gate refuses it like any other.
    /// </summary>
    public static DemoGateReport Evaluate(GateLevel level, IEnumerable<GateResult> results, int registered, IDictionary<string, string> knownRed = null)
    {
        knownRed = knownRed ?? new Dictionary<string, string>();
        var report = new DemoGateReport { Level = level };
        report.Results.AddRange(results.OrderBy(r => r.Owner, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal));
        if (registered == 0)
            report.Reasons.Add("No gate check is registered: nothing was measured.");
        foreach (GateResult r in report.Results.Where(r => !r.Pass))
        {
            string line = $"{r.Id} [{r.Owner}]: {r.Measured}{(string.IsNullOrEmpty(r.Detail) ? string.Empty : " - " + r.Detail)}";
            string known = knownRed.Keys.FirstOrDefault(k => GateResults.Matches(k, r.Id));
            if (known != null && level == GateLevel.Merge)
                report.KnownRed.Add($"{line} (known: {knownRed[known]})");
            else
                report.Reasons.Add(known != null ? $"{line} (known red: {knownRed[known]})" : line);
        }
        foreach (KeyValuePair<string, string> k in knownRed)
        {
            List<GateResult> matching = report.Results.Where(r => GateResults.Matches(k.Key, r.Id)).ToList();
            if (matching.Count > 0 && matching.All(r => r.Pass))
                report.StaleKnownRed.Add($"{k.Key} ({k.Value})");
        }
        report.Refused = report.Reasons.Count > 0;
        return report;
    }
}
