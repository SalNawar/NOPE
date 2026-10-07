using NUnit.Framework;

/// <summary>The morning paper's front page from the day's data (MorningPaper) and the Bureau memo clipped to it (BureauMemo).</summary>
public class MorningPaperTests
{
    private static readonly string[] None = new string[0];

    [Test]
    public void FirstNewsLeads_NotesAreTheDeck_NeverTheBulletin()
    {
        MorningPaper p = MorningPaper.Compose(new[] { "Be quick.", "Be right." }, new[] { "Driftbox 3 recall widens!", "Rain in Rome." }, new[] { "Desk 3 is short-handed." }, "Debt Relief Departures reach a record high.");
        Assert.AreEqual("Driftbox 3 recall widens!", p.Headline);
        Assert.AreEqual("Be quick. Be right.", p.Deck);
        Assert.AreEqual("briefing.newsHeader", p.StoryTitleKey);
        CollectionAssert.AreEqual(new[] { "Rain in Rome.", "Desk 3 is short-handed." }, p.StoryLines);
    }

    [Test]
    public void OnlyDeskLinesLeft_TakeTheDeskHeader()
    {
        MorningPaper p = MorningPaper.Compose(new[] { "Stay sharp." }, None, new[] { "The clerk at Desk 2 retires.", "  " }, null);
        Assert.AreEqual("Stay sharp", p.Headline);
        Assert.AreEqual("briefing.deskHeader", p.StoryTitleKey);
        CollectionAssert.AreEqual(new[] { "The clerk at Desk 2 retires." }, p.StoryLines);
    }

    [Test]
    public void NoNewsNoNotes_TheQuietDaysStoryLeads()
    {
        // Day 1 (Saleh 2026-10-07): the world's paper, never "NEW: your desk".
        MorningPaper p = MorningPaper.Compose(null, None, None, "  Debt Relief Departures reach a record high.  ");
        Assert.AreEqual("Debt Relief Departures reach a record high", p.Headline);
        Assert.AreEqual(string.Empty, p.Deck);
        Assert.IsNull(p.StoryTitleKey);
        Assert.AreEqual(0, p.StoryLines.Count);
    }

    [Test]
    public void EmptyDay_PrintsNothing()
    {
        MorningPaper p = MorningPaper.Compose(null, null, null, " ");
        Assert.AreEqual(string.Empty, p.Headline);
        Assert.IsNull(p.StoryTitleKey);
    }

    [Test]
    public void Memo_TheBulletinsFirstSentenceIsItsTitle_TheRestItsBody()
    {
        BureauMemo m = BureauMemo.Of("NEW: Debt Relief labourers. They carry a Work Permit. New hours from today: the desk opens at 09:00 and closes at 18:00.");
        Assert.IsFalse(m.IsEmpty);
        Assert.AreEqual("NEW: Debt Relief labourers", m.Title);
        Assert.AreEqual("They carry a Work Permit. New hours from today: the desk opens at 09:00 and closes at 18:00.", m.Body);
        BureauMemo one = BureauMemo.Of("  Gate 3 is closed today.  ");
        Assert.AreEqual("Gate 3 is closed today", one.Title);
        Assert.AreEqual(string.Empty, one.Body);
    }

    [Test]
    public void Memo_NoBulletin_NoMemo()
    {
        Assert.IsTrue(BureauMemo.Of(null).IsEmpty);
        Assert.IsTrue(BureauMemo.Of("  ").IsEmpty);
    }
}
