using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The key-word rule (the traveller-types spec's §8.1, I4): the character
/// spans of a traveller's line that stay English when the line shows
/// untranslated: the fill of each listed slot, each whole-word match of a
/// listed word (case- and accent-insensitive) and, when asked, digits; sorted
/// and merged. Tuning is an edit to the lists ("this we will tune later").
/// </summary>
public class KeyWordsTests
{
    private const string Claim = "I request passage home to {place}.";

    private static KeyWordRule Rule(string[] slots = null, string[] words = null, bool digits = false) => new KeyWordRule
    {
        slots = (slots ?? new string[0]).ToList(),
        words = (words ?? new string[0]).ToList(),
        digits = digits
    };

    private static Dictionary<string, string> Fills(params (string slot, string value)[] fills) =>
        fills.ToDictionary(f => f.slot, f => f.value);

    /// <summary>The text the spans index (the template filled as Interview.Fill fills it).</summary>
    private static string Filled(string template, Dictionary<string, string> fills)
    {
        string text = template;
        foreach (KeyValuePair<string, string> f in fills ?? new Dictionary<string, string>())
            text = Interview.Fill(text, f.Key, f.Value);
        return text;
    }

    /// <summary>The English parts of the filled text, joined by "|".</summary>
    private static string English(string template, Dictionary<string, string> fills, KeyWordRule rule)
    {
        string text = Filled(template, fills);
        return string.Join("|", KeyWords.Spans(template, fills, rule).Select(s => text.Substring(s.start, s.length)));
    }

    [Test]
    public void ASlotsFill_StaysEnglish()
    {
        Dictionary<string, string> fills = Fills(("place", "Periclean Athens (Ancient)"));
        Assert.AreEqual("Periclean Athens (Ancient)", English(Claim, fills, Rule(new[] { "place" })));
    }

    [Test]
    public void AValueIsNotAKeySlot_ByDefault_SoItStaysInTheTongue()
    {
        Assert.AreEqual(string.Empty, English("We pay in {value}.", Fills(("value", "Deben")), Rule(new[] { "place", "name", "document" })));
    }

    [Test]
    public void ASlotFilledTwice_GivesTwoSpans_AndAnEmptyFillNone()
    {
        Assert.AreEqual("Rome|Rome", English("{place} and {place}", Fills(("place", "Rome")), Rule(new[] { "place" })));
        Assert.AreEqual(string.Empty, English(Claim, Fills(("place", "")), Rule(new[] { "place" })));
        Assert.AreEqual(string.Empty, English(Claim, Fills(("place", null)), Rule(new[] { "place" })));
    }

    [Test]
    public void TheSpans_IndexTheTextInterviewFillGives_OtherTokensStayLiteral()
    {
        Dictionary<string, string> fills = Fills(("value", "Thebes"), ("place", "Egypt"));
        Assert.AreEqual("Egypt", English("Our capital is {value}, near {place} ({honorific}).", fills, Rule(new[] { "place" })));
        Assert.AreEqual("Rome", English("{honorific}, home to {place}", Fills(("place", "Rome")), Rule(new[] { "place" })),
            "an unfilled token is text like any other, before the fill");
    }

    [Test]
    public void Words_MatchWholeWordsOnly_IgnoringCaseAndAccents()
    {
        KeyWordRule rule = Rule(null, new[] { "home", "yes" });
        Assert.AreEqual("Home|HOME|hôme|Yes", English("Home is not homeland; HOME, hôme. Yes!", null, rule));
        Assert.AreEqual("Cafe", English("Cafe au lait.", null, Rule(null, new[] { "café" })), "an accented list word matches the plain text");
    }

    [Test]
    public void AWordOfSeveralWords_MatchesAsOnePhrase()
    {
        Assert.AreEqual("temporal customs", English("To the temporal customs zone.", null, Rule(null, new[] { "Temporal Customs" })));
        Assert.AreEqual(string.Empty, English("Temporal matters and customs.", null, Rule(null, new[] { "Temporal Customs" })));
    }

    [Test]
    public void Digits_StayEnglishWhenAsked()
    {
        Assert.AreEqual("1450|12", English("We left in 1450, aged 12.", null, Rule(digits: true)));
        Assert.AreEqual(string.Empty, English("We left in 1450, aged 12.", null, Rule()));
    }

    [Test]
    public void OverlappingSpans_Merge_AndComeInOrder()
    {
        KeyWordRule rule = Rule(new[] { "place" }, new[] { "home", "Temporal Customs", "zone" });
        IReadOnlyList<(int start, int length)> spans = KeyWords.Spans(Claim, Fills(("place", "Temporal Customs Zone")), rule);
        Assert.AreEqual("home|Temporal Customs Zone", English(Claim, Fills(("place", "Temporal Customs Zone")), rule), "the words inside the fill merge into it");
        Assert.AreEqual(2, spans.Count);
        Assert.Less(spans[0].start + spans[0].length, spans[1].start);
    }

    [Test]
    public void TheEmptyRule_KeepsNothingEnglish()
    {
        CollectionAssert.IsEmpty(KeyWords.Spans(Claim, Fills(("place", "Rome")), null));
        CollectionAssert.IsEmpty(KeyWords.Spans(Claim, Fills(("place", "Rome")), new KeyWordRule()));
        CollectionAssert.IsEmpty(KeyWords.Spans(null, null, Rule(new[] { "place" }, new[] { "home" }, true)));
        CollectionAssert.IsEmpty(KeyWords.Spans("home", null, Rule(null, new[] { " ", null })), "blank words match nothing");
    }

    [Test]
    public void Problems_ACleanRuleHasNone()
    {
        CollectionAssert.IsEmpty(KeyWords.Problems(Rule(new[] { "place", "name", "document" }, new[] { "home", "please", "papers", "yes", "no", "Temporal Customs" }, true)));
    }

    [Test]
    public void Problems_AnUnknownOrBlankSlot()
    {
        List<string> unknown = KeyWords.Problems(Rule(new[] { "place", "planet" }));
        Assert.AreEqual(1, unknown.Count, string.Join(" | ", unknown));
        StringAssert.Contains("'planet'", unknown[0]);
        Assert.AreEqual(1, KeyWords.Problems(Rule(new[] { " " })).Count);
    }

    [Test]
    public void Problems_ABlankWord()
    {
        List<string> problems = KeyWords.Problems(Rule(null, new[] { "home", "" }));
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains("blank", problems[0]);
    }

    [Test]
    public void Problems_ADuplicateWord_IgnoringCaseAndAccents()
    {
        List<string> problems = KeyWords.Problems(Rule(null, new[] { "Home", "yes", "hôme" }));
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains("'hôme'", problems[0]);
    }

    [Test]
    public void Problems_NoRule()
    {
        Assert.AreEqual(1, KeyWords.Problems(null).Count);
    }
}
