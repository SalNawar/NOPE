using NUnit.Framework;

/// <summary>
/// The desk's obstacles (DOC-HANDLE's registry, BUG-8): the height over a desk
/// point is the tallest registered footprint holding it, 0 over the bare desk;
/// the tallest anywhere is what a held paper is drawn above; the footprints are
/// what a resting paper keeps clear of.
/// </summary>
public class DeskObstaclesTests
{
    [Test]
    public void DeskObstacles_HeightIsTheTallestFootprintHoldingThePoint()
    {
        var obstacles = new DeskObstacles();
        Assert.AreEqual(0f, obstacles.HeightAt(0f, 0f));

        obstacles.Register("scanner", new DeskRect(0f, 0f, 0.2f, 0.2f), 0.05f);
        obstacles.Register("lamp", new DeskRect(0.05f, 0f, 0.1f, 0.1f), 0.3f);
        Assert.AreEqual(2, obstacles.Count);
        Assert.AreEqual(0.3f, obstacles.HeightAt(0.05f, 0f));
        Assert.AreEqual(0.05f, obstacles.HeightAt(-0.08f, 0f));
        Assert.AreEqual(0f, obstacles.HeightAt(0.5f, 0.5f));

        Assert.IsTrue(obstacles.Unregister("lamp"));
        Assert.AreEqual(0.05f, obstacles.HeightAt(0.05f, 0f));
    }

    [Test]
    public void DeskObstacles_TheTallestAndTheFootprints()
    {
        var obstacles = new DeskObstacles();
        Assert.AreEqual(0f, obstacles.MaxHeight, "an empty desk");
        obstacles.Register("scanner", new DeskRect(0f, 0f, 0.3f, 0.25f), 0.31f);
        obstacles.Register("reader", new DeskRect(0.4f, 0f, 0.2f, 0.2f), 0.07f);
        obstacles.Register("shadow", new DeskRect(0f, 0.3f, 0.3f, 0.2f), -1f);
        Assert.AreEqual(0.31f, obstacles.MaxHeight);
        Assert.AreEqual(0f, obstacles.HeightAt(0f, 0.3f), "a footprint papers keep off that nothing stands on");
        var footprints = new System.Collections.Generic.List<DeskRect> { new DeskRect(9f, 9f, 1f, 1f) };
        obstacles.Footprints(footprints);
        Assert.AreEqual(3, footprints.Count, "every footprint, the list cleared first");
        obstacles.Register("scanner", new DeskRect(0.1f, 0f, 0.3f, 0.25f), 0.05f);
        Assert.AreEqual(0.07f, obstacles.MaxHeight, "registered again: moved and lowered");
        Assert.IsNotNull(DeskObstacles.Desk);
    }
}
