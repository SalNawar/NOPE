using NUnit.Framework;

/// <summary>
/// The daters' ink (the desk machine spec §1): each print's density varies a
/// little; the pad empties over the shift, so prints get lighter, but never
/// runs out (a look, not a resource); re-inking restores it.
/// </summary>
public class DaterInkTests
{
    private const float Fade = 0.04f, Floor = 0.45f, Spread = 0.08f;

    [Test]
    public void AFreshPad_PrintsFullDensity()
    {
        Assert.AreEqual(1f, DaterInk.Density(0, Fade, Floor), 1e-6f);
    }

    [Test]
    public void EachPrint_FadesThePad()
    {
        Assert.AreEqual(1f - Fade, DaterInk.Density(1, Fade, Floor), 1e-6f);
        Assert.AreEqual(1f - 5 * Fade, DaterInk.Density(5, Fade, Floor), 1e-6f);
        Assert.Less(DaterInk.Density(6, Fade, Floor), DaterInk.Density(5, Fade, Floor));
    }

    [Test]
    public void ThePad_NeverRunsOut()
    {
        Assert.AreEqual(Floor, DaterInk.Density(1000, Fade, Floor), 1e-6f);
        Assert.Greater(DaterInk.Density(int.MaxValue, Fade, Floor), 0f);
    }

    [Test]
    public void ABadKnob_StaysInRange()
    {
        Assert.AreEqual(1f, DaterInk.Density(3, -1f, Floor), 1e-6f, "a negative fade fades nothing");
        Assert.AreEqual(0.2f, DaterInk.Density(1000, Fade, 0.2f), 1e-6f);
        Assert.AreEqual(1f, DaterInk.Density(0, Fade, 1.5f), 1e-6f, "a floor above 1 is 1");
    }

    [Test]
    public void APrint_VariesWithinItsSpread_TheSameForTheSamePrint()
    {
        for (int print = 0; print < 200; print++)
        {
            float d = DaterInk.Print(print, 7, Fade, Floor, Spread);
            float pad = DaterInk.Density(print, Fade, Floor);
            Assert.That(d, Is.InRange(pad - Spread - 1e-6f, pad + Spread + 1e-6f));
            Assert.That(d, Is.InRange(0f, 1f));
            Assert.AreEqual(d, DaterInk.Print(print, 7, Fade, Floor, Spread), "deterministic");
        }
    }

    [Test]
    public void Prints_DoVary()
    {
        Assert.AreNotEqual(DaterInk.Print(0, 7, Fade, Floor, Spread), DaterInk.Print(0, 8, Fade, Floor, Spread));
        Assert.AreNotEqual(DaterInk.Print(1, 7, 0f, Floor, Spread), DaterInk.Print(2, 7, 0f, Floor, Spread));
    }

    [Test]
    public void Hash01_IsInTheUnitRange_AndSpreadsOut()
    {
        float sum = 0f;
        for (int i = 0; i < 1000; i++)
        {
            float h = DaterInk.Hash01(i, 3, 11);
            Assert.That(h, Is.InRange(0f, 1f));
            sum += h;
        }
        Assert.AreEqual(0.5f, sum / 1000f, 0.05f);
    }
}
