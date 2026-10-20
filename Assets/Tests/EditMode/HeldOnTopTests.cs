using NUnit.Framework;

/// <summary>
/// A held thing drawn above everything (DOC-HANDLE, BUG-8: the rack drew over
/// the held passport): HeldOnTop moves it toward the camera along the camera's
/// own rays and shrinks it by the same ratio, so its picture is unchanged and
/// it is in front of every prop up to the clearance height.
/// </summary>
public class HeldOnTopTests
{
    [Test]
    public void TheRatio_PutsItInFrontOfEveryPropUpToTheClearance()
    {
        // An eye 0.7 m over the desk, props up to 0.28 m: 1 - 0.28 / 0.7 = 0.6 of the way out.
        Assert.AreEqual(0.6f, HeldOnTop.Ratio(0.7f, 0.28f, 0.3f), 1e-5f);
        Assert.AreEqual(0.3f, HeldOnTop.Ratio(0.7f, 0.65f, 0.3f), 1e-5f, "never nearer the eye than its least");
        Assert.AreEqual(1f, HeldOnTop.Ratio(0.7f, 0f, 0.3f), "nothing to clear: it stays where it is");
        Assert.AreEqual(1f, HeldOnTop.Ratio(0.2f, 0.3f, 0.3f), "an eye no higher than the props: nothing helps");
    }

    [Test]
    public void AHeldPoint_StaysOnItsRayFromTheEye_InFrontOfTheProps()
    {
        // The eye at (0, 0.7, -0.5); a paper corner on the desk at (0.2, 0, 0.3).
        (float ex, float ey, float ez) = (0f, 0.7f, -0.5f);
        (float x, float y, float z) = (0.2f, 0f, 0.3f);
        float s = HeldOnTop.Ratio(ey, 0.28f, 0.3f);
        (float hx, float hy, float hz) = HeldOnTop.Toward(x, y, z, ex, ey, ez, s);
        // the same ray: the direction from the eye is unchanged (the picture is the same), only the distance is s of it
        float dx = x - ex, dy = y - ey, dz = z - ez, len = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        float hdx = hx - ex, hdy = hy - ey, hdz = hz - ez, hlen = (float)System.Math.Sqrt(hdx * hdx + hdy * hdy + hdz * hdz);
        Assert.AreEqual(dx / len, hdx / hlen, 1e-5f);
        Assert.AreEqual(dy / len, hdy / hlen, 1e-5f);
        Assert.AreEqual(dz / len, hdz / hlen, 1e-5f);
        Assert.AreEqual(s * len, hlen, 1e-5f);
        Assert.GreaterOrEqual(hy, 0.28f - 1e-5f, "as high as the tallest prop: no prop on that ray stands in front of it");
        Assert.AreEqual((x, y, z), HeldOnTop.Toward(x, y, z, ex, ey, ez, 1f), "let go (1): exactly where it lies");
    }
}
