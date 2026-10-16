using NUnit.Framework;

/// <summary>The office camera's lens at the screen's aspect (leftover 2026-10-08: at 5:4 the office view cut off the PC and the phone).</summary>
public class LensFitTests
{
    private static float Tan(float degrees) => (float)System.Math.Tan(degrees * System.Math.PI / 180.0);

    [Test]
    public void Fit_KeepsTheArtsLensAtTheDesignAspectAndWider()
    {
        Assert.AreEqual((55f, 0f), LensFit.Fit(55f, LensFit.DesignAspect, 16f / 9f, 42f));
        Assert.AreEqual((55f, 0f), LensFit.Fit(55f, LensFit.DesignAspect, 21f / 9f, 42f));
        Assert.AreEqual((55f, 0f), LensFit.Fit(55f, LensFit.DesignAspect, 0f, 42f));
    }

    [Test]
    public void Fit_ANarrowerScreenSeesAsWideAsTheDesignAspect_ItsTopEdgeKept()
    {
        // 16:10 has room below: the full 16:9 width, tilted down so the top edge stays the art's.
        (float fov, float tilt) = LensFit.Fit(55f, LensFit.DesignAspect, 16f / 10f, 42f);
        Assert.AreEqual(Tan(27.5f) * LensFit.DesignAspect, Tan(fov / 2f) * 1.6f, 1e-4f);
        Assert.AreEqual(27.5f, fov / 2f - tilt, 1e-4f);
    }

    [Test]
    public void Fit_WidensNoFurtherThanTheDeskReachesBelow()
    {
        // 5:4 would need 73 degrees; the desk ends 42 degrees below: half of 27.5 + 42, the top edge kept, the bottom on the desk's edge.
        (float fov, float tilt) = LensFit.Fit(55f, LensFit.DesignAspect, 5f / 4f, 42f);
        Assert.AreEqual(69.5f, fov, 1e-4f);
        Assert.AreEqual(7.25f, tilt, 1e-4f);
        Assert.AreEqual(42f, fov / 2f + tilt, 1e-4f);
        Assert.Greater(Tan(fov / 2f) * 1.25f, Tan(27.5f) * LensFit.DesignAspect * 0.9f);
    }
}
