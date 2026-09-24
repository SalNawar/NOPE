using NUnit.Framework;

public class OriginLabelsTests
{
    [Test]
    public void PlaceAndEra_AreWrittenAsPlaceThenEraInBrackets()
    {
        Assert.AreEqual("Abbasid Baghdad (Medieval)", OriginLabels.Format("Abbasid Baghdad", "Medieval"));
    }

    [Test]
    public void BlankEra_LeavesThePlaceAlone()
    {
        Assert.AreEqual("Babylonia", OriginLabels.Format("Babylonia", null));
        Assert.AreEqual("Babylonia", OriginLabels.Format("Babylonia", " "));
    }
}
