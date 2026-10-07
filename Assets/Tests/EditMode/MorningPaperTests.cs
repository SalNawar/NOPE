using NUnit.Framework;

/// <summary>The morning paper's front page from the day's data (MorningPaper).</summary>
public class MorningPaperTests
{
    private static readonly string[] None = new string[0];

    [Test]
    public void Bulletin_LeadsAsHeadlineAndDeck()
    {
        MorningPaper p = MorningPaper.Compose("NEW: Debt Relief labourers. They carry a Work Permit.", new[] { "Process everyone." }, new[] { "Departures reach a record high." }, None);
        Assert.IsTrue(p.LeadIsBulletin);
        Assert.AreEqual("NEW: Debt Relief labourers", p.Headline);
        Assert.AreEqual("They carry a Work Permit.", p.Deck);
        Assert.AreEqual("briefing.newsHeader", p.StoryTitleKey);
        CollectionAssert.AreEqual(new[] { "Departures reach a record high.", "Process everyone." }, p.StoryLines);
    }

    [Test]
    public void OneSentenceBulletin_IsAllHeadline()
    {
        MorningPaper p = MorningPaper.Compose("  Gate 3 is closed today.  ", None, None, None);
        Assert.AreEqual("Gate 3 is closed today", p.Headline);
        Assert.AreEqual(string.Empty, p.Deck);
        Assert.IsNull(p.StoryTitleKey);
        Assert.AreEqual(0, p.StoryLines.Count);
    }

    [Test]
    public void NoBulletin_FirstNewsLeads_NotesAreTheDeck()
    {
        MorningPaper p = MorningPaper.Compose(null, new[] { "Be quick.", "Be right." }, new[] { "Driftbox 3 recall widens!", "Rain in Rome." }, new[] { "Desk 3 is short-handed." });
        Assert.IsFalse(p.LeadIsBulletin);
        Assert.AreEqual("Driftbox 3 recall widens!", p.Headline);
        Assert.AreEqual("Be quick. Be right.", p.Deck);
        Assert.AreEqual("briefing.newsHeader", p.StoryTitleKey);
        CollectionAssert.AreEqual(new[] { "Rain in Rome.", "Desk 3 is short-handed." }, p.StoryLines);
    }

    [Test]
    public void OnlyDeskLinesLeft_TakeTheDeskHeader()
    {
        MorningPaper p = MorningPaper.Compose(null, new[] { "Stay sharp." }, None, new[] { "The clerk at Desk 2 retires.", "  " });
        Assert.AreEqual("Stay sharp", p.Headline);
        Assert.AreEqual("briefing.deskHeader", p.StoryTitleKey);
        CollectionAssert.AreEqual(new[] { "The clerk at Desk 2 retires." }, p.StoryLines);
    }

    [Test]
    public void EmptyDay_PrintsNothing()
    {
        MorningPaper p = MorningPaper.Compose("", null, null, null);
        Assert.AreEqual(string.Empty, p.Headline);
        Assert.IsFalse(p.LeadIsBulletin);
        Assert.IsNull(p.StoryTitleKey);
    }
}
