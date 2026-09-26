using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The news back issues: recorded at the briefing, a replayed day replacing its issue, day order and the cap.</summary>
public class NewsArchiveTests
{
    [Test]
    public void Record_CopiesTheDaysLines()
    {
        var archive = new List<NewsIssue>();
        var briefing = new List<string> { "Notice", " " };
        var news = new List<string> { "Lead", "Second" };
        NewsArchive.Record(archive, 1, briefing, news, 30);
        briefing.Add("added later");

        NewsIssue issue = archive.Single();
        Assert.AreEqual(1, issue.day);
        CollectionAssert.AreEqual(new[] { "Notice" }, issue.briefing, "a copy, blank lines dropped");
        CollectionAssert.AreEqual(new[] { "Lead", "Second" }, issue.news);
        Assert.AreSame(issue, NewsArchive.Find(archive, 1));
        Assert.IsNull(NewsArchive.Find(archive, 2));
        Assert.IsNull(NewsArchive.Find(null, 1));
    }

    [Test]
    public void Record_AReplayedDayReplacesItsIssue_AndKeepsDayOrder()
    {
        var archive = new List<NewsIssue>();
        NewsArchive.Record(archive, 2, new[] { "two" }, null, 30);
        NewsArchive.Record(archive, 1, null, new[] { "one" }, 30);
        NewsArchive.Record(archive, 2, new[] { "two again" }, null, 30);
        CollectionAssert.AreEqual(new[] { 1, 2 }, archive.Select(i => i.day));
        CollectionAssert.AreEqual(new[] { "two again" }, NewsArchive.Find(archive, 2).briefing);
        CollectionAssert.IsEmpty(NewsArchive.Find(archive, 2).news);
    }

    [Test]
    public void Record_PastTheCap_TheOldestGo()
    {
        var archive = new List<NewsIssue>();
        for (int day = 1; day <= 5; day++)
            NewsArchive.Record(archive, day, null, new[] { $"day {day}" }, 3);
        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, archive.Select(i => i.day));

        NewsArchive.Record(archive, 6, null, null, 0);
        CollectionAssert.AreEqual(new[] { 6 }, archive.Select(i => i.day), "a cap below 1 keeps one issue");
        Assert.DoesNotThrow(() => NewsArchive.Record(null, 1, null, null, 30));
    }
}
