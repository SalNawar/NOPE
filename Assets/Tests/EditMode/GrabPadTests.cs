using System.Collections.Generic;
using NUnit.Framework;

public sealed class GrabPadTests
{
    [Test]
    public void AloneTheSmallSheetGrowsToTheWantedSize()
    {
        (float hx, float hy) = GrabPad.Half(25f, 0f, 13.5f, 5f, 0f, 0f, null);
        Assert.AreEqual(25f, hx);
        Assert.AreEqual(25f, hy);
    }

    [Test]
    public void ABigSheetKeepsItsOwnSize()
    {
        (float hx, float hy) = GrabPad.Half(25f, 0f, 40f, 30f, 0f, 0f, new List<(float, float)>());
        Assert.AreEqual(40f, hx);
        Assert.AreEqual(30f, hy);
    }

    [Test]
    public void ANeighbourOnTheSideStopsThePadAtHalfTheWay()
    {
        var others = new List<(float, float)> { (30f, 2f) };
        (float hx, float hy) = GrabPad.Half(25f, 0f, 13.5f, 5f, 0f, 0f, others);
        Assert.AreEqual(15f, hx);
        Assert.AreEqual(25f, hy);
    }

    [Test]
    public void ANeighbourAboveStopsThePadHeight()
    {
        var others = new List<(float, float)> { (3f, 20f) };
        (float hx, float hy) = GrabPad.Half(25f, 0f, 13.5f, 5f, 0f, 0f, others);
        Assert.AreEqual(25f, hx);
        Assert.AreEqual(10f, hy);
    }

    [Test]
    public void ANeighbourVeryCloseLeavesTheSheetAlone()
    {
        var others = new List<(float, float)> { (10f, 0f) };
        (float hx, float hy) = GrabPad.Half(25f, 0f, 13.5f, 5f, 0f, 0f, others);
        Assert.AreEqual(13.5f, hx);
        Assert.AreEqual(25f, hy);
    }

    [Test]
    public void TheMarginPutsThePadPastTheSheetOnEverySide()
    {
        (float hx, float hy) = GrabPad.Half(25f, 22f, 13.5f, 5f, 0f, 0f, null);
        Assert.AreEqual(35.5f, hx);
        Assert.AreEqual(27f, hy);
    }
}
