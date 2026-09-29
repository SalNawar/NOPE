using System.Collections.Generic;
using NUnit.Framework;

/// <summary>Home's content block (the Home upgrades spec §5, HU3): the radio's line of the night and what Generate World refuses.</summary>
public class HomeContentTests
{
    private static HomeContent Radio(params string[] lines) => new HomeContent { radioUpgrade = "house_radio", radio = new List<string>(lines) };

    [TestCase(1, "a")]
    [TestCase(2, "b")]
    [TestCase(3, "c")]
    [TestCase(4, "a", Description = "round again")]
    [TestCase(0, "c", Description = "a day before the first wraps back")]
    public void RadioLine_OneANightInOrderByTheDay(int day, string expected)
    {
        Assert.AreEqual(expected, Radio("a", "b", "c").RadioLine(day));
    }

    [Test]
    public void RadioLine_EmptyWithoutLines()
    {
        Assert.AreEqual(string.Empty, Radio().RadioLine(3));
        Assert.AreEqual(string.Empty, new HomeContent { radio = null }.RadioLine(3));
    }

    [Test]
    public void Problems_ABlankLine_AndARadioThatIsNoHouseUpgrade()
    {
        CollectionAssert.IsEmpty(Radio("a").Problems(new[] { "house_radio" }));
        Assert.AreEqual(1, Radio("a", " ").Problems(new[] { "house_radio" }).Count, "a blank line");
        Assert.AreEqual(1, Radio("a").Problems(new[] { "house_plant" }).Count, "the radio is no house upgrade");
        CollectionAssert.IsEmpty(new HomeContent { radioUpgrade = "", radio = new List<string>() }.Problems(new string[0]), "no radio at all is fine");
    }
}
