using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// TEXT-LANG's seams: CultureThemeService.ResetForNewDay (the forced leader
/// and the cheat's culture-day override cleared for a new day, BUG-27a),
/// UiKeys (each key is one the string tables already hold, so it resolves to
/// today's string in every language) and SettingsWindowController.Open (for
/// FLOW's Esc).
/// </summary>
public class TextLangSeamTests
{
    private const string Tables = "Assets/Data/World/Culture";

    [Test]
    public void ResetForNewDay_ClearsTheForcedLeaderAndTheCultureDayOverride()
    {
        string before = DevToolsState.ForcedLeaderId;
        bool? lockBefore = CultureThemeService.LockOverride;
        try
        {
            DevToolsState.ForcedLeaderId = "seam-test-leader";
            CultureThemeService.LockOverride = true;
            CultureThemeService.ResetForNewDay();
            Assert.IsNull(DevToolsState.ForcedLeaderId);
            Assert.IsNull(CultureThemeService.LockOverride, "the cheat's culture day lasts one day (BUG-27a)");
        }
        finally
        {
            DevToolsState.ForcedLeaderId = before;
            CultureThemeService.LockOverride = lockBefore;
        }
    }

    [Test]
    public void SettingsOpen_WithNoSettingsWindowLoaded_IsFalse()
    {
        Assert.IsFalse(SettingsWindowController.Open());
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
