using NUnit.Framework;

/// <summary>
/// The found flash of a search jump (redesign phase 19, the PC spec's SE4):
/// the pulses' strength over their time, and the scroll that puts the found
/// item in the middle of its view.
/// </summary>
public class FoundFlashTests
{
    [Test]
    public void Pulse_RisesAndFallsOncePerPulse_OverItsTime()
    {
        Assert.AreEqual(0f, FoundFlash.Pulse(0f, 1.2f, 2), 1e-4f);
        Assert.AreEqual(1f, FoundFlash.Pulse(0.3f, 1.2f, 2), 1e-4f, "the first pulse's peak");
        Assert.AreEqual(0f, FoundFlash.Pulse(0.6f, 1.2f, 2), 1e-4f, "between the pulses");
        Assert.AreEqual(1f, FoundFlash.Pulse(0.9f, 1.2f, 2), 1e-4f, "the second pulse's peak");
        Assert.AreEqual(0f, FoundFlash.Pulse(1.2f, 1.2f, 2), 1e-4f);
        Assert.AreEqual(0.5f, FoundFlash.Pulse(0.15f, 1.2f, 2), 1e-4f);
    }

    [Test]
    public void Pulse_IsNothing_BeforeAfterOrWithoutTime()
    {
        Assert.AreEqual(0f, FoundFlash.Pulse(-0.1f, 1.2f, 2));
        Assert.AreEqual(0f, FoundFlash.Pulse(1.3f, 1.2f, 2));
        Assert.AreEqual(0f, FoundFlash.Pulse(0.3f, 0f, 2));
        Assert.AreEqual(0f, FoundFlash.Pulse(0.3f, 1.2f, 0));
        Assert.IsTrue(FoundFlash.Done(1.2f, 1.2f));
        Assert.IsFalse(FoundFlash.Done(1.1f, 1.2f));
    }

    [Test]
    public void CentredScroll_PutsTheItemInTheMiddle_Clamped()
    {
        Assert.AreEqual(0.5f, FoundFlash.CentredScroll(1000f, 400f, 500f), 1e-4f);
        Assert.AreEqual(1f, FoundFlash.CentredScroll(1000f, 400f, 100f), 1e-4f, "near the top: the top");
        Assert.AreEqual(0f, FoundFlash.CentredScroll(1000f, 400f, 950f), 1e-4f, "near the bottom: the bottom");
        Assert.AreEqual(1f, FoundFlash.CentredScroll(300f, 400f, 200f), 1e-4f, "content that fits stays at the top");
    }
}
