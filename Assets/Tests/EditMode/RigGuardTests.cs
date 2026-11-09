using System.IO;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The QA rig compiles only in the editor or the QA player (the four test levels, 2026-11-09): every C# file under
/// Assets/Tests/PlayMode, outside an Editor folder, starts with <c>#if UNITY_EDITOR || TIMEDESK_QA</c> (or
/// <c>#if UNITY_EDITOR</c>), unless it is one of the player-safe files listed here. The release build at 7bf43b3
/// failed because BriefingLayoutOps.cs landed without the guard and referenced QaRunner in the player.
/// </summary>
public class RigGuardTests
{
    /// <summary>Rig files the player holds on purpose: they reference no editor or QA-only type (the build at 5483e42 proves it).</summary>
    private static readonly string[] PlayerSafe =
    {
        "Assets/Tests/PlayMode/Cam/Rig/CamOps.cs",
        "Assets/Tests/PlayMode/Rig/QaSignals.cs",
    };

    [Test]
    public void EveryRigFile_StartsWithThePlayerGuard_OrIsListedPlayerSafe()
    {
        string assets = ProjectRoot();
        string root = Path.Combine(assets, "Assets", "Tests", "PlayMode");
        var unguarded = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Select(f => f.Replace('\\', '/'))
            .Where(f => !f.Contains("/Editor/"))
            .Select(f => new { path = f, rel = f.Substring(assets.Replace('\\', '/').Length).TrimStart('/') })
            .Where(x => !PlayerSafe.Contains(x.rel))
            .Where(x => !FirstCodeLine(x.path).StartsWith("#if UNITY_EDITOR"))
            .Select(x => x.rel)
            .ToList();
        Assert.IsEmpty(unguarded, "rig files that the release player would compile: " + string.Join(", ", unguarded) +
            ". Start the file with #if UNITY_EDITOR || TIMEDESK_QA, or list it in RigGuardTests.PlayerSafe with the build that proves it.");
    }

    [Test]
    public void ThePlayerSafeList_NamesFilesThatExist()
    {
        string assets = ProjectRoot();
        foreach (string rel in PlayerSafe)
            Assert.IsTrue(File.Exists(Path.Combine(assets, rel)), rel + " is listed player-safe but does not exist");
    }

    /// <summary>The project folder: the nearest parent of the working directory or of this assembly that holds Assets/Tests/PlayMode (the offline runner has no UnityEngine).</summary>
    private static string ProjectRoot()
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), Path.GetDirectoryName(typeof(RigGuardTests).Assembly.Location) })
        {
            var dir = new DirectoryInfo(start);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Assets", "Tests", "PlayMode"))) return dir.FullName.Replace('\\', '/');
                dir = dir.Parent;
            }
        }
        Assert.Inconclusive("no project folder with Assets/Tests/PlayMode above the working directory or the test assembly");
        return string.Empty;
    }

    private static string FirstCodeLine(string path)
    {
        foreach (string line in File.ReadLines(path))
        {
            string t = line.Trim();
            if (t.Length == 0 || t.StartsWith("//")) continue;
            return t;
        }
        return string.Empty;
    }
}
