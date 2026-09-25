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
}
