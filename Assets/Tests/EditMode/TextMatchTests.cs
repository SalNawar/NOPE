using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Search's matcher (redesign phase 19, the PC spec's SE3 and SE5): the fold
/// (lower case, marks dropped, punctuation read as spaces, with the map back
/// to the shown text), word starts, quoted phrases, the whole-value rank, the
/// marks mapped back through accents, and the untranslated rules (typed words
/// read only the key words and the speaker; a chip matches only the same
/// tongue and the same canonical line).
/// </summary>
public class TextMatchTests
{
    private static IndexEntry Plain(string text, string label = "", string title = "") =>
        new IndexEntry { Key = "k", Source = AppTab.Reference, Title = title, Label = label, Text = text };

    private static bool Match(string typed, IndexEntry e) => TextMatch.Matches(SearchQuery.Parse(typed), e, out _, new List<Mark>());

    private static int Score(string typed, IndexEntry e)
    {
        Assert.IsTrue(TextMatch.Matches(SearchQuery.Parse(typed), e, out int score, new List<Mark>()), $"'{typed}' should match '{e.Text}'");
        return score;
    }

    /// <summary>The marked parts of the entry's text, joined by "|".</summary>
    private static string Marked(string typed, IndexEntry e)
    {
        var marks = new List<Mark>();
        Assert.IsTrue(TextMatch.Matches(SearchQuery.Parse(typed), e, out _, marks));
        return string.Join("|", marks.Select(m => e.Text.Substring(m.Start, m.Length)));
    }

    [Test]
    public void Fold_LowerCase_MarksDropped_PunctuationAsSpaces()
    {
        var map = new List<int>();
        Assert.AreEqual("hatnefer", TextMatch.Fold("Ḥatnefer", map));
        Assert.AreEqual("552 1804 33", TextMatch.Fold("552-1804-33", map));
        Assert.AreEqual("aster vale   born", TextMatch.Fold("Aster Vale · Born", map));
        Assert.AreEqual("zoe", TextMatch.Fold("Zoë", map));
        Assert.AreEqual(string.Empty, TextMatch.Fold(null, map));
    }

    [Test]
    public void Fold_MapsEachFoldedCharacterBackToTheShownText()
    {
        var map = new List<int>();
        // A decomposed accent (e + U+0301) is dropped: the fold is shorter than the text.
        string folded = TextMatch.Fold("Zéno", map);
        Assert.AreEqual("zeno", folded);
        CollectionAssert.AreEqual(new[] { 0, 1, 3, 4 }, map);
    }

    [Test]
    public void Words_MustStartAWord()
    {
        Assert.IsTrue(Match("drach", Plain("Drachma")));
        Assert.IsTrue(Match("tetra", Plain("Silver tetradrachm")));
        Assert.IsFalse(Match("rachma", Plain("Drachma")), "the middle of a word is no match");
        Assert.IsFalse(Match("drach", Plain("Silver tetradrachm")));
    }

    [Test]
    public void EveryWordMustMatch_InTheLabelOrTheText()
    {
        IndexEntry born = Plain("3 May 1131", label: "Born", title: "Bjorn · Born");
        Assert.IsTrue(Match("may 1131", born));
        Assert.IsTrue(Match("born 1131", born), "the label is read too");
        Assert.IsFalse(Match("bjorn 1131", born), "the title is not read, only ranked");
        Assert.IsFalse(Match("june 1131", born));
    }

    [Test]
    public void CaseAndAccentsAreIgnored_BothWays()
    {
        Assert.IsTrue(Match("hatnefer", Plain("Ḥatnefer")));
        Assert.IsTrue(Match("ḤATNEFER", Plain("Hatnefer")));
        Assert.IsTrue(Match("dp-0412", Plain("DP-0412-07")), "punctuation reads as spaces");
    }

    [Test]
    public void QuotedPhrase_MustAppearAsWritten()
    {
        IndexEntry line = Plain("We paid in drachmas, of course.");
        Assert.IsTrue(Match("\"paid in drachmas\"", line));
        Assert.IsTrue(Match("\"drachmas of\"", line), "punctuation inside reads as a space");
        Assert.IsFalse(Match("\"in paid\"", line), "the words' order counts");
        Assert.IsFalse(Match("\"paid in drach\"", line), "a phrase's words are whole");
        Assert.IsTrue(Match("course \"paid in\"", line), "words and phrases together");
    }

    [Test]
    public void Rank_WholeValueFirst_ThenTitle_ThenText_ThenLabel()
    {
        Assert.AreEqual(100 + 20, Score("Marcus", Plain("Marcus")));
        Assert.AreEqual(20, Score("Marcus", Plain("Marcus II")));
        Assert.AreEqual(40 + 20, Score("drach", Plain("Drachma", title: "Currency Ledger · Drachma")));
        Assert.AreEqual(10, Score("currency", Plain("Drachma", label: "Currency Ledger")));
        Assert.AreEqual(20 + 10, Score("born", Plain("born", label: "Born") ) - 100, "whole value, the text and the label");
    }

    [Test]
    public void Marks_AreTheMatchedWordStartsAndPhrases_InTheShownText()
    {
        Assert.AreEqual("Drach", Marked("drach", Plain("Drachma")));
        Assert.AreEqual("paid in drachmas", Marked("\"paid in drachmas\"", Plain("We paid in drachmas, of course.")));
        Assert.AreEqual("Ḥat", Marked("hat", Plain("Ḥatnefer")));
        Assert.AreEqual("én", Marked("en", Plain("Zéno éno")).Split('|')[0], "a decomposed accent stays inside its mark");
        Assert.AreEqual(string.Empty, Marked("born", Plain("3 May 1131", label: "Born")), "a label-only match marks nothing in the text");
    }

    [Test]
    public void Searchable_FromTwoCharacters_OrOneDigit_OrAChip()
    {
        Assert.IsFalse(SearchQuery.Parse("").IsSearchable);
        Assert.IsFalse(SearchQuery.Parse("  a ").IsSearchable);
        Assert.IsTrue(SearchQuery.Parse("ab").IsSearchable);
        Assert.IsTrue(SearchQuery.Parse("7").IsSearchable);
        Assert.IsFalse(SearchQuery.Parse("\"").IsSearchable);
        Assert.IsTrue(SearchQuery.Parse("", new SearchChip("greek", "We paid.")).IsSearchable);
        Assert.IsTrue(SearchQuery.Parse(null).IsEmpty);
    }

    [Test]
    public void Parse_WordsAndPhrases_Folded()
    {
        SearchQuery q = SearchQuery.Parse("Drach \"Classical  Athens\" 1131 \"unclosed words");
        CollectionAssert.AreEqual(new[] { "drach", "1131", "unclosed", "words" }, q.Words);
        CollectionAssert.AreEqual(new[] { "classical athens" }, q.Phrases);
        Assert.IsFalse(q.Chip.HasValue);
    }

    private static IndexEntry Untranslated(string keyWords, string speaker, string canonical, string tongue = "greek") =>
        new IndexEntry
        {
            Key = "line:3", Source = AppTab.Transcript, Title = speaker + " · line 4", Label = speaker, Text = keyWords,
            Foreign = true, ForeignShown = "ΩΨΞ ΣΦ", TongueId = tongue, Canonical = canonical
        };

    [Test]
    public void Untranslated_TypedWordsReadOnlyTheKeyWordsAndTheSpeaker()
    {
        IndexEntry line = Untranslated("Drachma", "Lysimache", "We paid in Drachma, of course.");
        Assert.IsTrue(Match("drachma", line));
        Assert.IsTrue(Match("lysimache", line));
        Assert.IsFalse(Match("paid", line), "the hidden English is never read");
        Assert.IsFalse(Match("course", line));
    }

    [Test]
    public void Chip_MatchesOnlyTheSameTongueAndTheSameLine()
    {
        IndexEntry line = Untranslated("Drachma", "Lysimache", "We paid in Drachma, of course.");
        var chip = new SearchChip("greek", "we paid in drachma of course");
        Assert.IsTrue(TextMatch.Matches(SearchQuery.Parse(null, chip), line, out int score, new List<Mark>()), "equal once folded");
        Assert.AreEqual(100, score);
        Assert.IsFalse(TextMatch.Matches(SearchQuery.Parse(null, new SearchChip("latin", "We paid in Drachma, of course.")), line, out _, new List<Mark>()), "another tongue");
        Assert.IsFalse(TextMatch.Matches(SearchQuery.Parse(null, new SearchChip("greek", "We paid in Drachma")), line, out _, new List<Mark>()), "a part is not the line");
        Assert.IsFalse(TextMatch.Matches(SearchQuery.Parse(null, chip), Plain("We paid in Drachma, of course."), out _, new List<Mark>()), "a plain row never matches a chip");
        Assert.IsTrue(TextMatch.Matches(SearchQuery.Parse("lysim", chip), line, out _, new List<Mark>()), "typed words and a chip together");
        Assert.IsFalse(TextMatch.Matches(SearchQuery.Parse("paid", chip), line, out _, new List<Mark>()));
    }

    [Test]
    public void Untranslated_MarksNothing_ItsSnippetIsTheGlyphs()
    {
        var marks = new List<Mark>();
        Assert.IsTrue(TextMatch.Matches(SearchQuery.Parse("drachma"), Untranslated("Drachma", "Lysimache", "We paid in Drachma."), out _, marks));
        CollectionAssert.IsEmpty(marks);
    }

    [Test]
    public void MatchesText_ForPlainNames_BlankMatchesEverything()
    {
        Assert.IsTrue(TextMatch.MatchesText(SearchQuery.Parse("senen"), "Senenmut"));
        Assert.IsTrue(TextMatch.MatchesText(SearchQuery.Parse(""), "Senenmut"));
        Assert.IsFalse(TextMatch.MatchesText(SearchQuery.Parse("nmut"), "Senenmut"));
        Assert.IsTrue(TextMatch.MatchesText(SearchQuery.Parse("hat"), "Ḥatnefer"));
    }
}
