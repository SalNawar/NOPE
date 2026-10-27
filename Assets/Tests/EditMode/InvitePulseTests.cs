using NUnit.Framework;

/// <summary>
/// The AVAILABLE sign's invitation (HALL-FX, BUG-67; InvitePulse): it pulses a few
/// beats from the moment it wants a press and can be pressed (after the morning
/// briefing, never behind it), starting and ending lit, then holds lit
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
    public void TheBeats_StartWhenTheSignCanBePressed_NotBehindTheBriefing()
    {
        // The office loads at 0 s under the morning briefing; Start Shift closes it at 20 s (the sign can be pressed).
        // A 30 fps run, frame by frame, as AvailableSignLink polls it.
        const float beat = 1.6f, briefingCloses = 20f, dt = 1f / 30f;
        float since = InvitePulse.Since(float.NaN, true, false, 0f); // Configure / Apply at load never starts it
        Assert.IsNaN(since, "not at load");
        int dips = 0;
        float previous = 1f, falling = 0f;
        bool wasFalling = false;
        for (float t = 0f; t < 40f; t += dt)
        {
            since = InvitePulse.Since(since, true, t >= briefingCloses, t);
            float level = float.IsNaN(since) ? 1f : InvitePulse.Level(t - since, beat, false);
            if (t < briefingCloses)
            {
                Assert.IsNaN(since, $"the beats wait behind the briefing ({t:0.00} s)");
                Assert.IsFalse(InvitePulse.Settled(float.NaN, beat, false), "a link still waiting is not settled: it keeps polling");
                Assert.AreEqual(1f, level, "lit and steady under the briefing");
                continue;
            }
            Assert.AreEqual(briefingCloses, since, dt, "the beats start on the first frame the sign can be pressed");
            bool isFalling = level < previous - 1e-6f;
            if (wasFalling && !isFalling)
                dips++;
            if (isFalling)
                falling = t;
            wasFalling = isFalling;
            previous = level;
        }
        Assert.AreEqual(InvitePulse.Cycles, dips, "every beat is seen after Start Shift");
        Assert.That(falling, Is.LessThan(briefingCloses + beat * InvitePulse.Cycles), "then it holds lit");
        Assert.AreEqual(0f, InvitePulse.Level(briefingCloses + beat * 0.5f - since, beat, false), 1e-3f, "the first dip half a beat after Start Shift");
    }

    [Test]
    public void TheInvitation_HoldsOnceStarted_AndRestartsOnlyAfterItStopsInviting()
    {
        float since = InvitePulse.Since(float.NaN, true, true, 5f);
        Assert.AreEqual(5f, since);
        Assert.AreEqual(5f, InvitePulse.Since(since, true, false, 9f), "a briefing up again later never restarts it");
        Assert.AreEqual(5f, InvitePulse.Since(since, true, true, 9f), "held");
        Assert.IsNaN(InvitePulse.Since(since, false, true, 9f), "the first press (or closing) ends the invitation");
        Assert.AreEqual(12f, InvitePulse.Since(float.NaN, true, true, 12f));
    }

    [Test]
    public void ReducedMotion_HoldsItLitAndSteady()
    {
        for (float t = 0f; t < 10f; t += 0.1f)
            Assert.AreEqual(1f, InvitePulse.Level(t, 1.6f, true));
        Assert.IsTrue(InvitePulse.Settled(0f, 1.6f, true), "no frames needed");
    }
}
