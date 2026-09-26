using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Notes app's rules (redesign phase 25; the PC spec's NT1): one page per
/// day with its own text, the day cap, clippings with their cap, grouping by
/// traveller, the character limit.
/// </summary>
public class NotesTests
{
    [Test]
    public void Page_OnePagePerDayInDayOrder()
    {
        var pages = new List<NotePage>();
        NotePage two = Notes.Page(pages, 2, 30);
        two.text = "day two";
        NotePage one = Notes.Page(pages, 1, 30);
        one.text = "day one";

        Assert.AreSame(two, Notes.Page(pages, 2, 30), "the same day gives the same page");
        Assert.AreEqual("day two", Notes.Page(pages, 2, 30).text, "each day keeps its own text");
        CollectionAssert.AreEqual(new[] { 1, 2 }, pages.Select(p => p.day));
        Assert.IsNull(Notes.Page(pages, 0, 30));
    }

    [Test]
    public void Page_DropsTheOldestPastTheCap()
    {
        var pages = new List<NotePage>();
        for (int day = 1; day <= 4; day++)
            Notes.Page(pages, day, 3);

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, pages.Select(p => p.day));
    }

    [Test]
    public void Clip_AddsUpToTheCapAndRefusesBlankClips()
    {
        var page = new NotePage { day = 1 };

        Assert.IsTrue(Notes.Clip(page, new Clipping { text = "Periclean Athens" }, 2));
        Assert.IsFalse(Notes.Clip(page, new Clipping { text = "  " }, 2));
        Assert.IsTrue(Notes.Clip(page, new Clipping { text = "Premium" }, 2));
        Assert.IsFalse(Notes.Clip(page, new Clipping { text = "third" }, 2), "the page is full");
        Assert.AreEqual(2, page.clippings.Count);
    }

    [Test]
    public void Unclip_RemovesOneCard()
    {
        var page = new NotePage { day = 1 };
        Notes.Clip(page, new Clipping { text = "a" }, 40);
        Notes.Clip(page, new Clipping { text = "b" }, 40);

        Assert.IsTrue(Notes.Unclip(page, 0));
        Assert.IsFalse(Notes.Unclip(page, 5));
        Assert.AreEqual("b", page.clippings.Single().text);
    }

    [Test]
    public void Groups_ByTravellerInFirstOrderLooseLast()
    {
        var page = new NotePage { day = 1 };
        Notes.Clip(page, new Clipping { text = "1", traveller = "Aspasia" }, 40);
        Notes.Clip(page, new Clipping { text = "2" }, 40);
        Notes.Clip(page, new Clipping { text = "3", traveller = "Ramose" }, 40);
        Notes.Clip(page, new Clipping { text = "4", traveller = "Aspasia" }, 40);

        List<ClippingGroup> groups = Notes.Groups(page);

        CollectionAssert.AreEqual(new[] { "Aspasia", "Ramose", "" }, groups.Select(g => g.Traveller));
        CollectionAssert.AreEqual(new[] { 0, 3 }, groups[0].Indexes);
        CollectionAssert.AreEqual(new[] { 1 }, groups[2].Indexes);
    }

    [Test]
    public void Trim_CutsAtTheLimit()
    {
        Assert.AreEqual("abc", Notes.Trim("abcdef", 3));
        Assert.AreEqual("ab", Notes.Trim("ab", 3));
        Assert.AreEqual(string.Empty, Notes.Trim(null, 3));
    }

    [Test]
    public void IsEmpty_WithoutTextOrClippings()
    {
        var page = new NotePage { day = 1 };
        Assert.IsTrue(Notes.IsEmpty(page));
        page.text = "x";
        Assert.IsFalse(Notes.IsEmpty(page));
        page.text = "";
        Notes.Clip(page, new Clipping { text = "y" }, 40);
        Assert.IsFalse(Notes.IsEmpty(page));
    }
}
