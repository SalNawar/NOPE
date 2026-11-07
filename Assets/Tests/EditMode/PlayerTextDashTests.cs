using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The long dash is nowhere in the game (Saleh: "never use em dashes i dont like them"). Every source
/// of a string the player reads is scanned for U+2014 in any form, and for U+2013 set between spaces
/// as a long dash (EndingWords.DashLines): the C# code (its string literals and, to leave nothing
/// behind, its comments), the data assets (the culture string tables, the document templates, the
/// slot outcomes, every ScriptableObject), the scenes and prefabs (the TextMeshPro text of the
/// screens), the JSON and text content (the world source, the QA scenarios). qa-endings-saves (f060e23)
/// fixed the Fired text; the evening edition still showed "SHIFT LEDGER" and "DAY 1" with a long dash
/// until endings-emdash (2026-11-07).
/// </summary>
public class PlayerTextDashTests
{
    /// <summary>One group of sources: its name, the folders (from the project root) and the file patterns it scans.</summary>
    private readonly struct Group
    {
        public readonly string Name;
        public readonly string[] Folders;
        public readonly string[] Patterns;

        public Group(string name, string[] folders, string[] patterns)
        {
            Name = name;
            Folders = folders;
            Patterns = patterns;
        }
    }

    private static readonly Group[] Groups =
    {
        new Group("C# code and comments", new[] { "Assets/Scripts", "Assets/Editor", "Assets/Tests" }, new[] { "*.cs" }),
        new Group("data assets (ScriptableObjects, string tables)", new[] { "Assets" }, new[] { "*.asset" }),
        new Group("scenes and prefabs (TextMeshPro text)", new[] { "Assets" }, new[] { "*.unity", "*.prefab" }),
        new Group("JSON and text content under Assets", new[] { "Assets" }, new[] { "*.json", "*.txt", "*.csv", "*.xml", "*.uxml", "*.uss" }),
        new Group("QA scenarios and content under tools/qa", new[] { "tools/qa" }, new[] { "*.json", "*.txt", "*.csv" }),
    };

    /// <summary>Folders of third-party assets: not our text.</summary>
    private static readonly string[] ThirdParty = { "3DreaMax Studio", "80s_Office", "TextMesh Pro" };

    /// <summary>The files that name the long dash on purpose (the rule and its tests).</summary>
    private static readonly string[] NamesTheDash = { "EndingWords.cs", "EndingWordsTests.cs", "PlayerTextDashTests.cs" };

    /// <summary>Files the scan must reach: a wrong root or pattern fails the test instead of passing on nothing.</summary>
    private static readonly string[] MustScan =
    {
        "world_source.json", "Strings_en.asset", "Strings_ar.asset", "OfficeGameplay.unity", "HomeScene.unity", "DayFlowUIController.cs",
        "HomeUIController.cs", "SlotOutcome_Jackpot_cash.asset", "DocTemplate_TC520.asset", "OfficeSceneContract.asset",
    };

    [TestCase("Badge \u2014 revoked", 1)]
    [TestCase("Badge \\u2014 revoked", 1)]
    [TestCase("Badge &mdash; revoked", 1)]
    [TestCase("Badge &#8212; revoked", 1)]
    [TestCase("Badge &#x2014; revoked", 1)]
    [TestCase("WORK PERMIT \u2013 DEBT RELIEF LABOUR", 1)]
    [TestCase("WORK PERMIT \\u2013 DEBT RELIEF LABOUR", 1)]
    [TestCase("a\nb \u2014 c\nd \u2014 e", 2)]
    [TestCase("expected: \"x\" \\u2014\n      would cause a panic", 1)]
    public void DashLines_FindEveryForm(string source, int lines)
    {
        Assert.AreEqual(lines, EndingWords.DashLines(source).Count, source);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("SHIFT LEDGER: EVENING EDITION")]
    [TestCase("text: \"\u2013\"", Description = "a lone en dash is a table cell")]
    [TestCase("the Nara\u2013Osaka plain", Description = "an en dash in a range")]
    [TestCase("Anime terminal - registered art")]
    [TestCase("\u25CF  DEVIATION LOGGED: {0} \u00B7 papers")]
    public void DashLines_PassPlainText(string source)
    {
        CollectionAssert.IsEmpty(EndingWords.DashLines(source));
    }

    /// <summary>No source of the game's text holds a long dash. Fails with the file, the line and the group of each hit.</summary>
    [Test]
    public void NoSourceOfPlayerText_HoldsALongDash()
    {
        string root = ProjectRoot();
        var scanned = new HashSet<string>();
        var hits = new List<string>();
        var report = new List<string>();

        foreach (Group g in Groups)
        {
            int count = 0;
            foreach (string folder in g.Folders)
            {
                string dir = Path.Combine(root, folder);
                if (!Directory.Exists(dir))
                    continue;
                foreach (string pattern in g.Patterns)
                    foreach (string path in Directory.EnumerateFiles(dir, pattern, SearchOption.AllDirectories))
                    {
                        string rel = Path.GetRelativePath(root, path).Replace('\\', '/');
                        if (IsThirdParty(rel) || NamesTheDash.Contains(Path.GetFileName(path)) || !scanned.Add(rel))
                            continue;
                        count++;
                        foreach (int line in EndingWords.DashLines(File.ReadAllText(path)))
                            hits.Add($"{rel}:{line} ({g.Name})");
                    }
            }
            report.Add($"{g.Name}: {count} files in {string.Join(", ", g.Folders)}");
            Assert.Greater(count, 0, $"the scan of '{g.Name}' reached no file (root {root})");
        }

        TestContext.Out.WriteLine("Scanned for a long dash (U+2014 in any form, U+2013 between spaces):\n  " + string.Join("\n  ", report) + $"\n  {scanned.Count} files in all; kept apart: {string.Join(", ", NamesTheDash)} (they name the dash)");
        foreach (string name in MustScan)
            Assert.IsTrue(scanned.Any(s => s.EndsWith("/" + name)), $"{name} was not scanned");
        CollectionAssert.IsEmpty(hits, $"{hits.Count} long dash line(s) in the sources of player text:\n" + string.Join("\n", hits.Take(60)));
    }

    /// <summary>True for a path inside a third-party folder.</summary>
    private static bool IsThirdParty(string relative)
    {
        return ThirdParty.Any(t => relative.Contains("/" + t + "/"));
    }

    /// <summary>The project root: the working folder (Unity) or three folders above this file (the offline runner).</summary>
    private static string ProjectRoot([CallerFilePath] string here = "")
    {
        if (Directory.Exists("Assets/Scripts"))
            return Path.GetFullPath(".");
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", "..", ".."));
    }
}
