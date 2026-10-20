using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// TEXT-LANG's P0-A seams: CultureThemeService.ResetForNewDay (the stub
/// clears the forced leader, the culture's part of DevToolsState.ResetAll)
/// and UiKeys (each key is one the string tables already hold, so it
/// resolves to today's string in every language).
/// </summary>
public class TextLangSeamTests
{
    private const string Tables = "Assets/Data/World/Culture";

    [Test]
    public void ResetForNewDay_StubClearsTheForcedLeader()
    {
        string before = DevToolsState.ForcedLeaderId;
        try
        {
            DevToolsState.ForcedLeaderId = "seam-test-leader";
            CultureThemeService.ResetForNewDay();
            Assert.IsNull(DevToolsState.ForcedLeaderId);
        }
        finally
        {
            DevToolsState.ForcedLeaderId = before;
        }
    }

    [Test]
    public void UiKeys_AreDistinct_AndInEveryStringTable()
    {
        List<string> keys = typeof(UiKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
                                          .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                                          .Select(f => (string)f.GetRawConstantValue())
                                          .ToList();
        Assert.AreEqual(5, keys.Count, "the P0 stub's five keys");
        Assert.AreEqual(keys.Count, keys.Distinct().Count(), "one key per text");

        string[] tables = Directory.GetFiles(Root(Tables), "Strings_*.asset");
        Assert.Greater(tables.Length, 0, "the string tables");
        var entry = new Regex(@"^\s*- key: (\S+)\s*$", RegexOptions.Multiline);
        foreach (string table in tables)
        {
            var held = new HashSet<string>(entry.Matches(File.ReadAllText(table)).Cast<Match>().Select(m => m.Groups[1].Value));
            foreach (string key in keys)
                Assert.IsTrue(held.Contains(key), $"{Path.GetFileName(table)} holds '{key}'");
        }
    }

    /// <summary>A project folder, found from this file's folder (so it is this checkout's under Unity and outside it alike).</summary>
    private static string Root(string projectPath, [CallerFilePath] string here = "") =>
        Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", projectPath);
}
