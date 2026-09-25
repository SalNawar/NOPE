using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The UI-channel cue that carries the present culture (piece 5 emits it, piece 6 reads it).</summary>
public class CultureCueTests
{
    [Test]
    public void Format_And_TryParse_RoundTrip()
    {
        Assert.AreEqual("culture:china", CultureCue.Format("china"));
        Assert.IsTrue(CultureCue.TryParse(CultureCue.Format("japan"), out string id));
        Assert.AreEqual("japan", id);
    }

    [TestCase("Culture:china", Description = "the prefix is case-sensitive")]
    [TestCase("culture:", Description = "a blank id")]
    [TestCase("culture:  ")]
    [TestCase(null)]
    [TestCase("booth:x")]
    public void TryParse_RejectsOtherCues(string cue)
    {
        Assert.IsFalse(CultureCue.TryParse(cue, out string id));
        Assert.IsNull(id);
    }

    [Test]
    public void Pick_NoCues_IsNeutral()
    {
        Assert.IsNull(CultureCue.Pick(null, out int none));
        Assert.AreEqual(0, none);
        Assert.IsNull(CultureCue.Pick(new List<string>(), out int empty));
        Assert.AreEqual(0, empty);
    }

    [Test]
    public void Pick_TheFirstCultureCueInListOrder_CountingEveryMatch()
    {
        Assert.AreEqual("x", CultureCue.Pick(new List<string> { "a", "culture:x" }, out int one));
        Assert.AreEqual(1, one);
        Assert.AreEqual("x", CultureCue.Pick(new List<string> { "culture:x", "culture:y" }, out int two));
        Assert.AreEqual(2, two);
        Assert.AreEqual("y", CultureCue.Pick(new List<string> { "culture:", "culture:y" }, out int blank), "a blank culture cue is skipped");
        Assert.AreEqual(1, blank);
    }
}
