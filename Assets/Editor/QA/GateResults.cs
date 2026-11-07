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

    /// <summary>
    /// Writes every assertion of a finished run into the results folder,
    /// stamped with HEAD and <paramref name="dirty"/> (the editor harness passes
    /// the tree's state it snapshotted before it touched any setting).
    /// </summary>
    public static int Import(QaRunReport report, string source, bool builtPlayer, bool dirty)
    {
        List<QaGateResult> results = FromRun(report, source, GitHead.Read(ProjectRoot), builtPlayer, dirty);
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

    /// <summary>
    /// The prefix of the <see cref="GateResult.Unmet"/> entry that <see cref="Judge"/>
    /// writes when no result matches its pattern: a known-red entry never spares
    /// a check for a result that was never measured (DemoGate.Evaluate; GATE-18).
    /// </summary>
    public const string NotMeasuredPrefix = "not measured: ";

    /// <summary>The default <see cref="SameMeasurement"/>: a result counts only at the commit it measured.</summary>
    public static readonly Func<string, string, bool> ExactCommitOnly = (commit, head) => false;

    /// <summary>
    /// True when a result measured at the first commit still measures the
    /// second (HEAD), though the hashes differ. <see cref="ExactCommitOnly"/>
    /// by default (pure, for the tests); DemoGate.Run sets
    /// <see cref="MeasuredPaths.Resolver"/> for the length of a gate run, so a
    /// commit that changes no measured path (the docs, the known-red list) does
    /// not make every result stale (GATE-01).
    /// </summary>
    public static Func<string, string, bool> SameMeasurement { get; set; } = ExactCommitOnly;

    /// <summary>
    /// The verdict of <see cref="Require"/> on given results and HEAD (pure
    /// with the default <see cref="SameMeasurement"/>; unit-tested). A result
    /// is stale when its commit is not HEAD and <see cref="SameMeasurement"/>
    /// does not carry it. When nothing matches, the check fails "not measured"
    /// and names the pattern in <see cref="GateResult.Unmet"/> after
    /// <see cref="NotMeasuredPrefix"/>.
    /// </summary>
    public static GateResult Judge(string checkId, string owner, string idPattern, IEnumerable<QaGateResult> results, string head, bool builtPlayer)
    {
        List<QaGateResult> matches = results.Where(r => Matches(idPattern, r.id)).OrderBy(r => r.id, StringComparer.Ordinal).ToList();
        if (matches.Count == 0)
        {
            GateResult none = GateResult.Fail(checkId, owner, "not measured", $"No result matches '{idPattern}': run the recipe (docs/pending/QA-RIG.md).");
            none.Unmet.Add(NotMeasuredPrefix + idPattern);
            return none;
        }
        var red = matches.Where(r => !r.pass).ToList();
        var stale = string.IsNullOrEmpty(head) ? new List<QaGateResult>() : matches.Where(r => !IsCurrent(r.commit, head)).ToList();
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
        GateResult fail = GateResult.Fail(checkId, owner, measured, string.Join("; ", lines));
        fail.Unmet.AddRange(red.Concat(stale).Concat(dirty).Concat(notBp).Select(r => r.id).Distinct(StringComparer.OrdinalIgnoreCase));
        return fail;
    }

    /// <summary>True when a result measured at <paramref name="commit"/> counts at <paramref name="head"/>: the same commit, or one <see cref="SameMeasurement"/> carries. Checks that pick results by commit use it, never a plain comparison.</summary>
    public static bool IsCurrent(string commit, string head) =>
        string.Equals(commit, head, StringComparison.OrdinalIgnoreCase)
        || (!string.IsNullOrEmpty(commit) && SameMeasurement(commit, head));

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
/// (<see cref="Churn"/>: an editor rewrites the build scene list and the
/// current quality level by itself, HOUSE_RULES "after Unity runs", and a
/// player build or an import rewrites the URP assets, the volume profile and
/// the graphics, project, Shader Graph and connect settings: the night
/// fleet's noise list, GATE-16; the generated *.csproj/*.sln at the root are
/// outside the paths and dropped anyway). The fleet never commits those files
/// (AGENT_RULES: never stage Unity noise), so their state in a working tree
/// is never part of a measured commit. Untracked files do not count (the job
/// runner's _TimeDesk* scripts are untracked by rule). The rig's own setting
/// changes (the QA quality level, the QA product name) never count: the
/// harness and the QA build take this snapshot before they touch a setting.
/// Runs git; when git cannot run, it returns nothing (a result is then judged
/// by its commit alone). tools/qa/seqtools.py (CHURN, dirty_paths, git_dirty)
/// mirrors it for analyses run by hand.
/// </summary>
public static class GitDirty
{
    /// <summary>The paths whose changes are Unity's own churn, never a change to what is measured (the night fleet's noise list, AGENT_RULES).</summary>
    public static readonly string[] Churn =
    {
        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset",
        "Assets/DefaultVolumeProfile.asset",
        "Assets/Settings/UniversalRP.asset",
        "Assets/UniversalRenderPipelineGlobalSettings.asset",
        "ProjectSettings/EditorBuildSettings.asset",
        "ProjectSettings/GraphicsSettings.asset",
        "ProjectSettings/ProjectSettings.asset",
        "ProjectSettings/QualitySettings.asset",
        "ProjectSettings/ShaderGraphSettings.asset",
        "ProjectSettings/UnityConnectSettings.asset",
    };

    /// <summary>The generated project files Unity rewrites at every import (never part of a measurement).</summary>
    private static readonly string[] ChurnExtensions = { ".csproj", ".sln" };

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

    /// <summary>git diff --name-only's output less the churn paths and generated project files (pure; unit-tested).</summary>
    public static List<string> Filter(string nameOnlyOutput) =>
        (nameOnlyOutput ?? string.Empty).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !IsChurn(l)).ToList();

    private static bool IsChurn(string path) =>
        Churn.Contains(path, StringComparer.OrdinalIgnoreCase)
        || ChurnExtensions.Any(e => path.EndsWith(e, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Which older commits a result still measures (GATE-01: a commit that only
/// touched the docs made every result stale). A result taken at commit C
/// counts at HEAD when no measured path differs between the two trees
/// (git diff --no-renames --name-only C HEAD: a rename lists both paths).
/// Every path is measured except the docs
/// folder, Markdown files outside Assets and the known-red list (it decides
/// the verdict, not what is measured), so the rule fails closed: a change
/// anywhere under Assets, Packages, ProjectSettings or tools makes the result
/// stale. When git cannot compare the two (an unknown commit), the result
/// stays stale.
/// </summary>
public static class MeasuredPaths
{
    /// <summary>The known-red list's path: it changes the verdict, never a measurement.</summary>
    public const string KnownRedFile = "tools/qa/gate_known_red.txt";

    /// <summary>True when a change to <paramref name="path"/> (repository-relative, '/' separated) can change a measurement.</summary>
    public static bool Affects(string path)
    {
        string p = (path ?? string.Empty).Trim().Replace('\\', '/');
        if (p.Length == 0)
            return false;
        if (p.StartsWith("docs/", StringComparison.OrdinalIgnoreCase) || string.Equals(p, KnownRedFile, StringComparison.OrdinalIgnoreCase))
            return false;
        return !(p.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && !p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>True when no path in git diff --name-only's output can change a measurement (pure; unit-tested).</summary>
    public static bool NoneAffected(string nameOnlyOutput) =>
        !(nameOnlyOutput ?? string.Empty).Split('\n').Any(Affects);

    /// <summary>
    /// A <see cref="GateResults.SameMeasurement"/> for the repository at
    /// <paramref name="root"/>: it runs git diff once per commit pair and
    /// keeps the answer for the gate run.
    /// </summary>
    public static Func<string, string, bool> Resolver(string root)
    {
        var cache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        return (commit, head) =>
        {
            if (string.IsNullOrEmpty(commit) || string.IsNullOrEmpty(head))
                return false;
            string key = commit + ".." + head;
            if (!cache.TryGetValue(key, out bool same))
                cache[key] = same = Compare(root, commit, head);
            return same;
        };
    }

    /// <summary>
    /// The git arguments that list every path changed between
    /// <paramref name="commit"/> and <paramref name="head"/>. Rename detection
    /// is off (--no-renames, whatever diff.renames says), so a measured file
    /// moved into docs/ lists its old path too and the result stays stale
    /// (the round 0 verifier's probe 0e47682: a scenario moved into docs/
    /// carried its results).
    /// </summary>
    public static string DiffArguments(string commit, string head) =>
        $"-c core.quotepath=off diff --no-renames --name-only {commit} {head}";

    private static bool Compare(string root, string commit, string head)
    {
        try
        {
            var start = new System.Diagnostics.ProcessStartInfo("git", DiffArguments(commit, head))
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
                return p.HasExited && p.ExitCode == 0 && NoneAffected(output);
            }
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception || e is InvalidOperationException || e is IOException)
        {
            return false;
        }
    }
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
