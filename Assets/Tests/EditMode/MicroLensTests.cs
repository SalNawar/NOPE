using NUnit.Framework;

/// <summary>Reading a microprint line under the lens stand's magnifiers (demo 1018a note 14; the desk machines spec §10): how much of the line a lens shows, and when that reads it.</summary>
public class MicroLensTests
{
    [Test]
    public void ARoundLens_ShowsTheChordOfTheLine_InsideIt()
    {
        Assert.AreEqual(0.04f, MicroLens.Shown(-0.1f, 0f, 0.1f, 0f, LensShape.Round, 0.02f, 0f), 1e-5f, "through its centre: the diameter");
        Assert.AreEqual(2f * (float)System.Math.Sqrt(0.02 * 0.02 - 0.01 * 0.01), MicroLens.Shown(-0.1f, 0.01f, 0.1f, 0.01f, LensShape.Round, 0.02f, 0f), 1e-5f, "off centre: a shorter chord");
        Assert.AreEqual(0f, MicroLens.Shown(-0.1f, 0.03f, 0.1f, 0.03f, LensShape.Round, 0.02f, 0f), "clear of it");
        Assert.AreEqual(0.02f, MicroLens.Shown(0f, 0f, 0.1f, 0f, LensShape.Round, 0.02f, 0f), 1e-5f, "a line that starts inside");
        Assert.AreEqual(0.01f, MicroLens.Shown(-0.005f, 0f, 0.005f, 0f, LensShape.Round, 0.02f, 0f), 1e-5f, "a short line wholly inside");
    }

    [Test]
    public void ARectangularLens_ClipsTheLineToItsSides()
    {
        Assert.AreEqual(0.12f, MicroLens.Shown(-0.2f, 0.001f, 0.2f, 0.001f, LensShape.Rectangle, 0.06f, 0.01f), 1e-5f, "the bar across the line: its width");
        Assert.AreEqual(0f, MicroLens.Shown(-0.2f, 0.02f, 0.2f, 0.02f, LensShape.Rectangle, 0.06f, 0.01f), "above it");
        Assert.AreEqual(0.02f, MicroLens.Shown(0f, -0.1f, 0f, 0.1f, LensShape.Rectangle, 0.06f, 0.01f), 1e-5f, "a line across the bar: its height");
        Assert.AreEqual(0.03f, MicroLens.Shown(0.03f, 0f, 0.2f, 0f, LensShape.Rectangle, 0.06f, 0.01f), 1e-5f);
    }

    [Test]
    public void ALine_IsReadOnceOneRepeatOfItHasShownForAQuarterSecond()
    {
        Assert.IsFalse(MicroLens.Readable(0.02f, 0.12f), "less than one repeat");
        Assert.IsTrue(MicroLens.Readable(MicroLens.ReadLength, 0.12f));
        Assert.IsTrue(MicroLens.Readable(0.019f, 0.02f), "a short line: nearly all of it");
        Assert.IsFalse(MicroLens.Readable(0f, 0f));
        float held = MicroLens.Held(0f, true, 0.2f);
        Assert.IsFalse(UvCone.IsSeen(held));
        held = MicroLens.Held(held, true, 0.1f);
        Assert.IsTrue(UvCone.IsSeen(held));
        Assert.AreEqual(0f, MicroLens.Held(held, false, 0.1f), "moved off: it starts over");
    }
}
