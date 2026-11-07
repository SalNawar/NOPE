using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// The words a screen of the run's end never shows (EndingWords; Saleh
/// 2026-09-29: "we dont make judgements ... you dont retire at the end of the
/// game ... the outcome depends on your choices"; his standing rule: never an
/// em dash). The rule itself, then every ending asset's name, body and closing
/// card read from disk (qa-endings-saves, 2026-11-07: Ending_Fired's body
/// carried an em dash onto the ending page and the evening edition).
/// </summary>
public class EndingWordsTests
{
    [TestCase("Badge, keys, and timeline clearance — all revoked", "em dash")]
    [TestCase("You retire with a pension.", "retire")]
    [TestCase("Retirement awaits.", "Retirement")]
    [TestCase("Your world ranked third.", "ranked")]
    [TestCase("Final score: 12", "score")]
    [TestCase("Grade A clerk", "Grade")]
    [TestCase("A good ending for the Directorate.", "good ending")]
    [TestCase("You won.", "You won")]
    [TestCase("Congratulations!", "Congratulations")]
    [TestCase("GAME OVER", "GAME OVER")]
    public void Problems_NameTheDashAndEachJudgementWord(string text, string named)
    {
        List<string> problems = EndingWords.Problems(text);
        Assert.AreEqual(1, problems.Count, string.Join("; ", problems));
        StringAssert.Contains(named, problems[0]);
    }

    [TestCase("Fired")]
    [TestCase("Your superiors have seen enough. Badge, keys and timeline clearance: all revoked, effective immediately.")]
    [TestCase("Frank's scorched passport, upgraded by nobody.")]
    [TestCase("Who runs 2150: The Directorate")]
    [TestCase("A Fresh Start")]
    [TestCase("")]
    [TestCase(null)]
    public void Problems_PassPlainWords(string text)
    {
        CollectionAssert.IsEmpty(EndingWords.Problems(text));
    }

    [Test]
    public void Problems_CountEveryDashAndWord()
    {
        List<string> problems = EndingWords.Problems("rank — score — retire");
        Assert.AreEqual(4, problems.Count, string.Join("; ", problems));
        StringAssert.Contains("2 em dash", problems[0]);
    }

    /// <summary>Every ending asset's player-facing text (its displayName, bodyText and closingCard, read from the YAML) holds no em dash and no judgement word.</summary>
    [Test]
    public void EveryEndingAsset_ShowsNoEmDashAndNoJudgementWord()
    {
        string[] assets = Directory.GetFiles(Folder("Assets/Data/Endings"), "*.asset");
        Assert.GreaterOrEqual(assets.Length, 4, "Fired, Bankrupt, the Welfare Office and the world report");
        var bad = new List<string>();
        foreach (string path in assets)
        {
            string shown = PlayerText(File.ReadAllText(path));
            Assert.IsNotEmpty(shown, $"{Path.GetFileName(path)} has a displayName");
            bad.AddRange(EndingWords.Problems(shown).Select(p => $"{Path.GetFileName(path)}: {p}"));
        }
        CollectionAssert.IsEmpty(bad, string.Join("\n", bad));
    }

    /// <summary>The folder at <paramref name="relative"/>: from the working folder (Unity) or from this file's place (the offline runner).</summary>
    private static string Folder(string relative, [CallerFilePath] string here = "")
    {
        return Directory.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative);
    }

    /// <summary>The lines from displayName up to the picture (displayName, bodyText, closingCard), with YAML's escapes decoded: what the ending screens show.</summary>
    private static string PlayerText(string yaml)
    {
        string[] lines = yaml.Replace("\r\n", "\n").Split('\n');
        int from = System.Array.FindIndex(lines, l => l.TrimStart().StartsWith("displayName:", System.StringComparison.Ordinal));
        int to = System.Array.FindIndex(lines, l => l.TrimStart().StartsWith("picture:", System.StringComparison.Ordinal));
        if (from < 0)
            return string.Empty;
        IEnumerable<string> text = lines.Skip(from).Take((to > from ? to : lines.Length) - from);
        string joined = string.Join(" ", text.Select(l => l.Trim()));
        return Regex.Unescape(joined.Replace("''", "'"));
    }
}
