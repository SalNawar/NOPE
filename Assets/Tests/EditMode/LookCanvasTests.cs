using NUnit.Framework;

/// <summary>The character canvas geometry (equal to make_guide_v2.py and coverage.json).</summary>
public class LookCanvasTests
{
    [Test]
    public void Constants_EqualTheGuide()
    {
        Assert.AreEqual((1024, 1536, 512), (LookCanvas.Width, LookCanvas.Height, LookCanvas.CenterX));
        Assert.AreEqual((260, 424, 500, 760, 900, 1170, 1490),
                        (LookCanvas.HeadTop, LookCanvas.Chin, LookCanvas.Shoulders, LookCanvas.Waist, LookCanvas.Hips, LookCanvas.Knees, LookCanvas.Feet));
        Assert.AreEqual((362, 215, 662, 590), (LookCanvas.PhotoLeft, LookCanvas.PhotoTop, LookCanvas.PhotoRight, LookCanvas.PhotoBottom));
    }

    [Test]
    public void FeetPivot_AndPhotoAspect()
    {
        Assert.AreEqual(46f / 1536f, LookCanvas.FeetPivotY, 1e-6f);
        Assert.AreEqual(0.8f, LookCanvas.PhotoAspect, 1e-6f);
    }

    [Test]
    public void PhotoRect_IsTheCropInBottomLeftFractions()
    {
        (float x, float y, float w, float h) = LookCanvas.PhotoRect;
        Assert.AreEqual(362f / 1024f, x, 1e-6f);
        Assert.AreEqual(946f / 1536f, y, 1e-6f, "the crop's bottom edge (590 from the top) is 946 from the bottom");
        Assert.AreEqual(300f / 1024f, w, 1e-6f);
        Assert.AreEqual(375f / 1536f, h, 1e-6f);
        Assert.AreEqual(LookCanvas.PhotoAspect, w * LookCanvas.Width / (h * LookCanvas.Height), 1e-6f);
    }

    [Test]
    public void LocalCoordinates_AreCanvasHeightsFromTheFeetAndTheCentreLine()
    {
        Assert.AreEqual(0f, LookCanvas.LocalY(LookCanvas.Feet), 1e-6f);
        Assert.AreEqual(1230f / 1536f, LookCanvas.LocalY(LookCanvas.HeadTop), 1e-6f);
        Assert.AreEqual(0f, LookCanvas.LocalX(LookCanvas.CenterX), 1e-6f);
        Assert.AreEqual(-232f / 1536f, LookCanvas.LocalX(LookCanvas.CenterX - LookCanvas.ArmReach), 1e-6f);
    }
}
