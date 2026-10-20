using NUnit.Framework;

/// <summary>
/// The AVAILABLE sign's invitation (HALL-FX, BUG-67; InvitePulse): it pulses a few
/// beats from the moment it wants a press, starting and ending lit, then holds lit
/// and steady (the audit measured it breathing between 40 and 95 luma forever);
/// lit and steady at once under Reduced Motion.
/// </summary>
public class InvitePulseTests
{
    [Test]
    public void ItPulses_ThenSettlesLit_AfterItsCycles()
    {
        const float beat = 1.6f;
        Assert.AreEqual(1f, InvitePulse.Level(0f, beat, false), 1e-6f, "it starts lit: no jump");
        Assert.AreEqual(0f, InvitePulse.Level(beat * 0.5f, beat, false), 1e-5f, "it dips to the paused ink mid-beat");
        Assert.AreEqual(1f, InvitePulse.Level(beat, beat, false), 1e-5f, "and is lit again at the beat's end");
        Assert.That(InvitePulse.Cycles, Is.InRange(2, 3), "FIX_PLAN: settle after 2-3 cycles");
        Assert.IsFalse(InvitePulse.Settled(beat * InvitePulse.Cycles - 0.01f, beat, false));
        Assert.IsTrue(InvitePulse.Settled(beat * InvitePulse.Cycles, beat, false));
        for (float t = beat * InvitePulse.Cycles; t < 60f; t += 0.05f)
            Assert.AreEqual(1f, InvitePulse.Level(t, beat, false), $"steady lit at {t} s (a 10 s idle capture reads ±0 luma)");
    }

    [Test]
    public void ReducedMotion_HoldsItLitAndSteady()
    {
        for (float t = 0f; t < 10f; t += 0.1f)
            Assert.AreEqual(1f, InvitePulse.Level(t, 1.6f, true));
        Assert.IsTrue(InvitePulse.Settled(0f, 1.6f, true), "no frames needed");
    }
}
