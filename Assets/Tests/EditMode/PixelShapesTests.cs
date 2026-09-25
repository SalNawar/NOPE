using NUnit.Framework;

/// <summary>Point-in-shape tests (the polygon test moved from the office builder's cursor placeholders).</summary>
public class PixelShapesTests
{
    private static readonly (float x, float y)[] Square = { (0, 0), (10, 0), (10, 10), (0, 10) };

    /// <summary>The builder's placeholder arrow cursor.</summary>
    private static readonly (float x, float y)[] Arrow = { (0, 0), (0, 22), (5, 17), (9, 26), (12, 25), (8, 16), (15, 16) };

    [TestCase(5f, 5f, true)]
    [TestCase(0.5f, 9.5f, true)]
    [TestCase(-0.5f, 5f, false)]
    [TestCase(10.5f, 5f, false)]
    [TestCase(5f, 10.5f, false)]
    public void InPolygon_Square(float x, float y, bool expected)
    {
        Assert.AreEqual(expected, PixelShapes.InPolygon(Square, x, y));
    }

    [TestCase(1.5f, 5.5f, true)]
    [TestCase(1f, 19.5f, true)]
    [TestCase(10.5f, 24.5f, true)]
    [TestCase(14.5f, 5.5f, false)]
    [TestCase(10.5f, 19.5f, false)]
    public void InPolygon_TheArrowCursor(float x, float y, bool expected)
    {
        Assert.AreEqual(expected, PixelShapes.InPolygon(Arrow, x, y));
    }

    [Test]
    public void InPolygon_EvenOdd_ASelfOverlappingBowTie()
    {
        (float x, float y)[] bowTie = { (0, 0), (10, 10), (10, 0), (0, 10) };
        Assert.IsTrue(PixelShapes.InPolygon(bowTie, 2f, 5f));
        Assert.IsFalse(PixelShapes.InPolygon(bowTie, 5f, 2f));
    }

    [TestCase(0f, 0f, true)]
    [TestCase(10f, 0f, true)]
    [TestCase(0f, 5f, true)]
    [TestCase(0f, 5.1f, false)]
    [TestCase(8f, 4f, false)]
    public void InEllipse(float x, float y, bool expected)
    {
        Assert.AreEqual(expected, PixelShapes.InEllipse(0f, 0f, 10f, 5f, x, y));
    }

    [Test]
    public void InEllipse_ADegenerateEllipseHoldsNothing()
    {
        Assert.IsFalse(PixelShapes.InEllipse(0f, 0f, 0f, 5f, 0f, 0f));
    }
}
