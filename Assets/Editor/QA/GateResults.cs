using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// The measured results gate checks read (Library/QA/results/&lt;id&gt;.json,
/// <see cref="QaGateResult"/>): written when a play run finishes (its
/// run.json's assertions, <see cref="Import"/>), by tools/qa (the built
/// player's runs and the frame analyses) and read by the Play and
/// BuiltPlayer checks through <see cref="Require"/>. A result counts only
/// for the commit it measured: one from an older HEAD is stale, and so is one
/// measured on uncommitted changes (<see cref="QaGateResult.dirty"/>).
/// </summary>
public static class GateResults
{
    /// <summary>The project's folder (the parent of Assets).</summary>
    public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

    /// <summary>The results folder: &lt;project&gt;/Library/QA/results.</summary>
    public static string Folder => Path.Combine(ProjectRoot, "Library", "QA", "results");

    /// <summary>Every result file in <paramref name="folder"/> (default <see cref="Folder"/>); unreadable files are skipped.</summary>
    public static List<QaGateResult> ReadAll(string folder = null)
    {
        folder = folder ?? Folder;
        var all = new List<QaGateResult>();
        if (!Directory.Exists(folder))
            return all;
        foreach (string file in Directory.GetFiles(folder, "*.json"))
        {
            try
            {
                QaGateResult r = JsonUtility.FromJson<QaGateResult>(File.ReadAllText(file));
                if (r != null && !string.IsNullOrEmpty(r.id))
                    all.Add(r);
            }
            catch (Exception e) when (e is IOException || e is ArgumentException)
            {
            }
        }
        return all;
    }

    /// <summary>Writes <paramref name="result"/> to <paramref name="folder"/> (default <see cref="Folder"/>) as &lt;id&gt;.json, replacing an older one.</summary>
    public static void Write(QaGateResult result, string folder = null)
    {
        folder = folder ?? Folder;
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, FileName(result.id)), JsonUtility.ToJson(result, true));
    }

    /// <summary>The file name for result <paramref name="id"/>.</summary>
    public static string FileName(string id) => Regex.Replace(id ?? "unnamed", @"[^A-Za-z0-9._-]", "_") + ".json";

    /// <summary>Turns a run's assertions into gate results stamped with <paramref name="commit"/> (<paramref name="dirty"/>: measured on uncommitted changes).</summary>
    public static List<QaGateResult> FromRun(QaRunReport report, string source, string commit, bool builtPlayer, bool dirty = false)
    {
        string now = DateTime.Now.ToString("s");
        return (report.assertions ?? Array.Empty<QaAssertion>()).Select(a => new QaGateResult
        {
            id = (builtPlayer ? "bp." : string.Empty) + a.id,
            owner = a.owner,
            pass = a.pass,
            measured = a.measured,
            detail = a.detail,
            commit = commit ?? string.Empty,
            dirty = dirty,
            written = now,
            source = source,
            builtPlayer = builtPlayer,
        }).ToList();
    }

    /// <summary>Writes every assertion of a finished run into the results folder, stamped with HEAD and whether the tree is dirty (the editor harness calls it).</summary>
    public static int Import(QaRunReport report, string source, bool builtPlayer)
    {
        List<QaGateResult> results = FromRun(report, source, GitHead.Read(ProjectRoot), builtPlayer, GitDirty.Changed(ProjectRoot).Count > 0);
        foreach (QaGateResult r in results)
            Write(r);
        return results.Count;
    }

    /// <summary>
    /// The gate verdict on the results matching <paramref name="idPattern"/>
    /// ('*' matches any run of characters) for check <paramref name="checkId"/>
    /// of <paramref name="owner"/>: it passes only when at least one result
    /// matches and every match passed, was measured at HEAD on a clean tree
    /// and (when <paramref name="builtPlayer"/>) on a built player.
    /// </summary>
    public static GateResult Require(string checkId, string owner, string idPattern, bool builtPlayer = false) =>
        Judge(checkId, owner, idPattern, ReadAll(), GitHead.Read(ProjectRoot), builtPlayer);

    /// <summary>The verdict of <see cref="Require"/> on given results and HEAD (pure; unit-tested).</summary>
    public static GateResult Judge(string checkId, string owner, string idPattern, IEnumerable<QaGateResult> results, string head, bool builtPlayer)
    {
        List<QaGateResult> matches = results.Where(r => Matches(idPattern, r.id)).OrderBy(r => r.id, StringComparer.Ordinal).ToList();
        if (matches.Count == 0)
            return GateResult.Fail(checkId, owner, "not measured", $"No result matches '{idPattern}': run the recipe (docs/pending/QA-RIG.md).");
        var red = matches.Where(r => !r.pass).ToList();
        var stale = string.IsNullOrEmpty(head) ? new List<QaGateResult>() : matches.Where(r => !string.Equals(r.commit, head, StringComparison.OrdinalIgnoreCase)).ToList();
        // A result taken on uncommitted changes measured no commit (a stale one is already counted).
        var dirty = matches.Where(r => r.dirty && !stale.Contains(r)).ToList();
        var notBp = builtPlayer ? matches.Where(r => !r.builtPlayer).ToList() : new List<QaGateResult>();
        string measured = $"{matches.Count} result(s): {matches.Count - red.Count} pass, {red.Count} fail, {stale.Count} stale, {dirty.Count} dirty" + (builtPlayer ? $", {notBp.Count} not on a built player" : string.Empty);
        if (red.Count == 0 && stale.Count == 0 && dirty.Count == 0 && notBp.Count == 0)
            return GateResult.Ok(checkId, owner, measured, string.Join("; ", matches.Select(r => $"{r.id} {r.measured}")));
        var lines = red.Select(r => $"FAIL {r.id} [{r.owner}] {r.measured} {r.detail}")
                       .Concat(stale.Select(r => $"STALE {r.id} measured at {Short(r.commit)}, HEAD is {Short(head)}"))
                       .Concat(dirty.Select(r => $"DIRTY {r.id} measured on uncommitted changes at {Short(r.commit)}: commit, then measure again"))
                       .Concat(notBp.Select(r => $"NOT-BP {r.id} was measured in the editor"));
        return GateResult.Fail(checkId, owner, measured, string.Join("; ", lines));
    }

    /// <summary>True when <paramref name="id"/> matches <paramref name="pattern"/> ('*' = any run of characters; case-insensitive).</summary>
    public static bool Matches(string pattern, string id)
    {
        if (pattern == null || id == null)
            return false;
        string regex = "^" + string.Join(".*", pattern.Split('*').Select(Regex.Escape)) + "$";
        return Regex.IsMatch(id, regex, RegexOptions.IgnoreCase);
    }

    private static string Short(string commit) => string.IsNullOrEmpty(commit) ? "unknown" : commit.Substring(0, Math.Min(8, commit.Length));
}

/// <summary>
/// The uncommitted changes that make a measurement not one of HEAD: tracked
/// files under Assets, Packages and ProjectSettings whose content differs
/// from HEAD (staged or not; line-ending-only differences do not count, as
/// with git diff), less the files Unity rewrites on its own
/// (<see cref="Churn"/>). Untracked files do not count (the job runner's
/// _TimeDesk* scripts are untracked by rule). Runs git; when git cannot run,
/// it returns nothing (a result is then judged by its commit alone).
/// </summary>
public static class GitDirty
{
    /// <summary>The paths whose changes are Unity's own churn, never a change to what is measured.</summary>
    public static readonly string[] Churn =
    {
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset",
    };

    /// <summary>The changed paths in <paramref name="root"/> (see the class), empty when clean or when git cannot run.</summary>
    public static List<string> Changed(string root)
    {
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo("git", "-c core.quotepath=off diff --name-only HEAD -- Assets Packages ProjectSettings")
            {
                WorkingDirectory = root,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(start))
            {
                string output = p.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                p.WaitForExit(60000);
                return p.HasExited && p.ExitCode == 0 ? Filter(output) : new List<string>();
            }
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception || e is InvalidOperationException || e is IOException)
        {
            return new List<string>();
        }
    }

    /// <summary>git diff --name-only's output less the churn paths (pure; unit-tested).</summary>
    public static List<string> Filter(string nameOnlyOutput) =>
        (nameOnlyOutput ?? string.Empty).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !Churn.Contains(l, StringComparer.OrdinalIgnoreCase)).ToList();
}

/// <summary>Reads the checked-out commit from a repository or worktree folder without running git (pure file reads).</summary>
public static class GitHead
{
    /// <summary>The full hash HEAD points at in <paramref name="root"/>, or empty when it cannot be read.</summary>
    public static string Read(string root)
    {
        try
        {
            string gitDir = GitDir(root);
            if (gitDir == null)
                return string.Empty;
            string head = File.ReadAllText(Path.Combine(gitDir, "HEAD")).Trim();
            if (!head.StartsWith("ref:", StringComparison.Ordinal))
                return head;
            string reference = head.Substring(4).Trim();
            string common = CommonDir(gitDir);
            foreach (string dir in new[] { gitDir, common })
            {
                string loose = Path.Combine(dir, reference.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(loose))
                    return File.ReadAllText(loose).Trim();
            }
            string packed = Path.Combine(common, "packed-refs");
            if (File.Exists(packed))
                foreach (string line in File.ReadAllLines(packed))
                    if (line.EndsWith(" " + reference, StringComparison.Ordinal))
                        return line.Substring(0, line.IndexOf(' ')).Trim();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        return string.Empty;
    }

    /// <summary>The git folder of <paramref name="root"/>: .git itself, or the folder a worktree's .git file points at.</summary>
    private static string GitDir(string root)
    {
        string dotGit = Path.Combine(root, ".git");
        if (Directory.Exists(dotGit))
            return dotGit;
        if (!File.Exists(dotGit))
            return null;
        string line = File.ReadAllText(dotGit).Trim();
        if (!line.StartsWith("gitdir:", StringComparison.Ordinal))
            return null;
        string dir = line.Substring(7).Trim();
        return Path.IsPathRooted(dir) ? dir : Path.GetFullPath(Path.Combine(root, dir));
    }

    /// <summary>A worktree's shared git folder (its commondir file), else the folder itself.</summary>
    private static string CommonDir(string gitDir)
    {
        string file = Path.Combine(gitDir, "commondir");
        if (!File.Exists(file))
            return gitDir;
        string dir = File.ReadAllText(file).Trim();
        return Path.IsPathRooted(dir) ? dir : Path.GetFullPath(Path.Combine(gitDir, dir));
    }
}
