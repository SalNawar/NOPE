using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>What tools/qa/seqtools.py said about one recorded sequence (QaPlayHarness.Analyze).</summary>
public sealed class SeqAnalysis
{
    /// <summary>The sequence's folder.</summary>
    public string Seq { get; set; } = string.Empty;

    /// <summary>True when python started.</summary>
    public bool Ran { get; set; }

    /// <summary>True when seqtools exited 0 (every analysis passed).</summary>
    public bool ExitOk { get; set; }

    /// <summary>Everything seqtools printed (its PASS, FAIL and SKIP lines), or why it could not run.</summary>
    public string Output { get; set; } = string.Empty;
}

/// <summary>
/// The verdict harness.txt's first line gives (GATE-07 of 2026-10-28: the
/// line was written before the frame analysis ran and said "ok" over red
/// flicker and latency lines). It joins the run's own verdict
/// (DemoGate.JudgeRun's line) with the frame analysis of every sequence the
/// run recorded: each seqtools "FAIL &lt;analysis&gt; &lt;seq&gt;: ..." line is a red
/// result with the id seqtools writes (&lt;scenario&gt;.seq.&lt;sequence&gt;.&lt;analysis&gt;),
/// known red when the known-red list's run patterns match that id; a
/// sequence whose analysis could not run, or ended with an error and no
/// FAIL line, is red. The line starts "ok" only when the run was ok and no
/// analysis line is red but known red. Pure; HarnessVerdictTests.
/// </summary>
public sealed class HarnessVerdict
{
    /// <summary>The first line of harness.txt.</summary>
    public string Line { get; private set; } = string.Empty;

    /// <summary>True when the run was ok and every red analysis line is known red.</summary>
    public bool Ok { get; private set; }

    /// <summary>Red analysis lines not on the known-red list, and analyses that did not run or ended with an error.</summary>
    public List<string> Unexpected { get; } = new List<string>();

    /// <summary>Red analysis lines the known-red list expects.</summary>
    public List<string> KnownRed { get; } = new List<string>();

    /// <summary>
    /// The verdict on a run whose own line is <paramref name="runLine"/>
    /// (ok when <paramref name="runOk"/>) and whose sequences were analysed
    /// as <paramref name="analyses"/> say, under run folder
    /// <paramref name="runFolder"/> (its name gives the scenario) and the
    /// known-red run patterns <paramref name="knownRedPatterns"/>.
    /// </summary>
    public static HarnessVerdict Judge(string runLine, bool runOk, string runFolder, IEnumerable<SeqAnalysis> analyses, IEnumerable<string> knownRedPatterns)
    {
        var v = new HarnessVerdict();
        List<string> patterns = (knownRedPatterns ?? Enumerable.Empty<string>()).ToList();
        string scenario = ScenarioOf(runFolder);
        foreach (SeqAnalysis a in analyses ?? Enumerable.Empty<SeqAnalysis>())
        {
            string seqName = Path.GetFileName((a.Seq ?? string.Empty).TrimEnd('\\', '/'));
            if (!a.Ran)
            {
                v.Unexpected.Add($"{seqName}: the analysis did not run ({First(a.Output)})");
                continue;
            }
            int fails = 0;
            foreach (string raw in (a.Output ?? string.Empty).Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (!line.StartsWith("FAIL ", StringComparison.Ordinal))
                    continue;
                fails++;
                string[] words = line.Split(' ');
                string analysis = words.Length > 1 ? words[1] : "?";
                int colon = line.IndexOf(": ", StringComparison.Ordinal);
                string measured = colon >= 0 ? line.Substring(colon + 2) : line;
                string id = string.IsNullOrEmpty(scenario) ? $"seq.{seqName}.{analysis}" : $"{scenario}.seq.{seqName}.{analysis}";
                string entry = $"{id}: {measured}";
                if (patterns.Any(p => GateResults.Matches(p, id)))
                    v.KnownRed.Add(entry);
                else
                    v.Unexpected.Add(entry);
            }
            if (!a.ExitOk && fails == 0)
                v.Unexpected.Add($"{seqName}: the analysis ended with an error ({First(a.Output)})");
        }
        bool hasRun = !string.IsNullOrWhiteSpace(runLine);
        string run = hasRun ? runLine.Trim() : "failed: the run gave no verdict";
        v.Ok = runOk && hasRun && v.Unexpected.Count == 0;
        string known = v.KnownRed.Count > 0 ? $"{v.KnownRed.Count} known red: {string.Join("; ", v.KnownRed)}" : string.Empty;
        if (v.Ok)
            v.Line = known.Length == 0 ? run : $"{run} (frame analysis: {known})";
        else if (runOk && hasRun)
            v.Line = $"failed (frame analysis: {v.Unexpected.Count} red{(known.Length > 0 ? ", " + known : string.Empty)}): {string.Join("; ", v.Unexpected)}; the run itself: {run}";
        else
            v.Line = v.Unexpected.Count == 0 ? run : $"{run}; frame analysis: {v.Unexpected.Count} red: {string.Join("; ", v.Unexpected)}";
        return v;
    }

    /// <summary>The scenario of run folder <paramref name="runFolder"/> (a path or a name &lt;scenario&gt;_&lt;yyyyMMdd&gt;_&lt;HHmmss&gt;): the name less its last two parts, as seqtools' default_id reads it.</summary>
    public static string ScenarioOf(string runFolder)
    {
        string name = Path.GetFileName((runFolder ?? string.Empty).TrimEnd('\\', '/'));
        int last = name.LastIndexOf('_');
        int before = last > 0 ? name.LastIndexOf('_', last - 1) : -1;
        return before > 0 ? name.Substring(0, before) : name;
    }

    private static string First(string text)
    {
        string line = (text ?? string.Empty).Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0) ?? "no output";
        return line.Length > 160 ? line.Substring(0, 160) : line;
    }
}
