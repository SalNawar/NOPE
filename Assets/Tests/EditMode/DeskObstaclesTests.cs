using NUnit.Framework;

/// <summary>
/// The desk's obstacles (P0-A seam, DOC-HANDLE's): the height over a desk
/// point is the tallest registered footprint holding it, 0 over the bare desk.
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
}
