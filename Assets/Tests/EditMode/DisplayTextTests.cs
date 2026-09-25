using NUnit.Framework;

/// <summary>The display-text seam (item 8's translation reveals, piece 9): today it shows the canonical text as it is.</summary>
public class DisplayTextTests
{
    [Test]
    public void BothMedia_ShowTheCanonicalText()
    {
        Assert.AreEqual("Deben", DisplayText.For("Deben", TextMedium.Written));
        Assert.AreEqual("We trade with Deben.", DisplayText.For("We trade with Deben.", TextMedium.Spoken));
    }

    [Test]
    public void Null_ShowsNothing()
    {
        Assert.AreEqual(string.Empty, DisplayText.For(null, TextMedium.Written));
        Assert.AreEqual(string.Empty, DisplayText.For(null, TextMedium.Spoken));
    }
}
