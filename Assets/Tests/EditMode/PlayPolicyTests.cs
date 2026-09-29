using NUnit.Framework;

/// <summary>Redesign phase 23: the balance simulation's three ways to play a day, one decision at a time.</summary>
public class PlayPolicyTests
{
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void Perfect_AlwaysRight_AndEveryDenialDocumented(bool shouldAccept, bool deviation)
    {
        var policy = new PlayPolicy(PlayStyle.Perfect);
        for (int day = 1; day <= 3; day++)
        {
            policy.StartDay(day);
            for (int i = 0; i < 4; i++)
            {
                PlayDecision d = policy.Decide(shouldAccept, deviation);
                Assert.AreEqual(shouldAccept, d.Accept);
                Assert.IsTrue(d.Documented);
            }
        }
    }

    [Test]
    public void Imperfect_OddDays_LetTheFirstFaultyTravellerThrough_Once()
    {
        var policy = new PlayPolicy(PlayStyle.Imperfect);
        policy.StartDay(1);

        Assert.IsTrue(policy.Decide(true, false).Accept, "an honest traveller is accepted");
        Assert.IsTrue(policy.Decide(false, true).Accept, "the day's first faulty traveller is let through");
        PlayDecision next = policy.Decide(false, true);
        Assert.IsFalse(next.Accept, "only one mistake a day");
        Assert.IsTrue(next.Documented, "odd days never leave a denial unproven");

        policy.StartDay(3);
        Assert.IsTrue(policy.Decide(false, false).Accept, "a new odd day: the first faulty traveller again, a directive fault too");
    }

    [Test]
    public void Imperfect_EvenDays_LeaveTheFirstDeviationDenialUnproven_Once()
    {
        var policy = new PlayPolicy(PlayStyle.Imperfect);
        policy.StartDay(2);

        PlayDecision directive = policy.Decide(false, false);
        Assert.IsFalse(directive.Accept);
        Assert.IsTrue(directive.Documented, "a directive denial needs no evidence, so it is never the day's mistake");

        PlayDecision first = policy.Decide(false, true);
        Assert.IsFalse(first.Accept, "even days deny every faulty traveller");
        Assert.IsFalse(first.Documented, "the day's first deviation denial is left unproven");

        Assert.IsTrue(policy.Decide(false, true).Documented, "only one a day");
    }

    /// <summary>Days 7-15 X3 (Q11): at the shift clock's pace the simulation reaches the first cap slots of the queue; the rest go home.</summary>
    [Test]
    public void Reaches_OnlyTheFirstCapSlots()
    {
        Assert.IsTrue(PlayPolicy.Reaches(1, 10));
        Assert.IsTrue(PlayPolicy.Reaches(10, 10));
        Assert.IsFalse(PlayPolicy.Reaches(11, 10));
        Assert.IsFalse(PlayPolicy.Reaches(18, 10));
        Assert.IsTrue(PlayPolicy.Reaches(7, 10), "the beats stand in slots 4-7, inside the cap");
    }

    [Test]
    public void Reaches_ACapOfZeroReachesEverySlot()
    {
        for (int slot = 1; slot <= 18; slot++)
            Assert.IsTrue(PlayPolicy.Reaches(slot, 0), $"slot {slot}: no cap, the whole queue");
        Assert.IsTrue(PlayPolicy.Reaches(3, -1), "a negative cap reads as none");
    }

    [Test]
    public void Careless_BothMistakes_EveryDay()
    {
        var policy = new PlayPolicy(PlayStyle.Careless);
        for (int day = 1; day <= 2; day++)
        {
            policy.StartDay(day);
            Assert.IsTrue(policy.Decide(false, true).Accept, "the first faulty traveller let through");
            PlayDecision unproven = policy.Decide(false, true);
            Assert.IsFalse(unproven.Accept);
            Assert.IsFalse(unproven.Documented, "the first deviation denial left unproven");
            PlayDecision third = policy.Decide(false, true);
            Assert.IsFalse(third.Accept);
            Assert.IsTrue(third.Documented);
        }
    }
}
