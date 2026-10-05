using NUnit.Framework;

/// <summary>
/// The city's stand-in skyline (the desk-first redesign, item 6): the same
/// seed paints the same layer; towers stand on the bottom edge across the
/// whole width within their height range; windows are lit only inside
/// bodies, and none when the lit share is 0.
/// </summary>
public class CitySkylineTests
{
    [Test]
    public void TheSameSeed_PaintsTheSameLayer_AnotherSeedAnother()
    {
        CitySkyline.Layer a = CitySkyline.Paint(256, 64, 7, 0.3f, 0.8f, 0.4f), b = CitySkyline.Paint(256, 64, 7, 0.3f, 0.8f, 0.4f);
        CollectionAssert.AreEqual(a.Bodies, b.Bodies);
        CollectionAssert.AreEqual(a.Windows, b.Windows);
        CollectionAssert.AreNotEqual(a.Bodies, CitySkyline.Paint(256, 64, 8, 0.3f, 0.8f, 0.4f).Bodies);
    }

    [Test]
    public void Towers_StandOnTheBottomEdge_AcrossTheWidth_WithinTheirHeights()
    {
        CitySkyline.Layer layer = CitySkyline.Paint(256, 100, 3, 0.3f, 0.6f, 0.4f);
        int filledColumns = 0;
        for (int x = 0; x < layer.Width; x++)
        {
            if (layer.Bodies[x] == 255)
                filledColumns++;
            Assert.AreEqual(0, layer.Bodies[(layer.Height - 1) * layer.Width + x], "nothing reaches the top row (spires stop at 0.68)");
        }
        Assert.Greater(filledColumns, layer.Width * 0.85f, "the towers stand across the width, with narrow gaps");
        for (int x = 0; x < layer.Width; x++)
            if (layer.Bodies[x] == 255)
                Assert.AreEqual(255, layer.Bodies[29 * layer.Width + x], $"the tower over column {x} is at least 0.3 of the height");
    }

    [Test]
    public void Windows_AreLitOnlyInsideBodies_NoneAtAShareOfZero()
    {
        CitySkyline.Layer layer = CitySkyline.Paint(256, 64, 11, 0.4f, 0.9f, 0.5f);
        int lit = 0;
        for (int i = 0; i < layer.Windows.Length; i++)
            if (layer.Windows[i] == 255)
            {
                lit++;
                Assert.AreEqual(255, layer.Bodies[i], "a lit window lies inside a building");
            }
        Assert.Greater(lit, 0);
        CollectionAssert.DoesNotContain(CitySkyline.Paint(256, 64, 11, 0.4f, 0.9f, 0f).Windows, (byte)255);
    }
}
