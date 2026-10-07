using NUnit.Framework;

/// <summary>Screen power: the start state, toggling, turning off, the wake rules and the citation hold.</summary>
public class PcScreenTests
{
    private static PcWakeRules Rules(bool onTraveller = true, bool onScan = true) =>
        new PcWakeRules { onTravellerPresented = onTraveller, onScanFinished = onScan };

    private static (PcScreen screen, int[] changes) Watch(bool startsOn, PcWakeRules rules)
    {
        var screen = new PcScreen(startsOn, rules);
        var changes = new int[1];
        screen.Changed += () => changes[0]++;
        return (screen, changes);
    }

    [Test]
    public void TheStartState_FollowsStartsOn()
    {
        Assert.IsTrue(new PcScreen(true, Rules()).IsOn);
        Assert.IsFalse(new PcScreen(false, Rules()).IsOn);
    }

    [Test]
    public void TheWakeRules_WakeOnBothByDefault()
    {
        var rules = new PcWakeRules();
        Assert.IsTrue(rules.onTravellerPresented);
        Assert.IsTrue(rules.onScanFinished);
    }

    [Test]
    public void ToggleTwice_ReturnsToTheStart_TrueEachTime_RaisingChangedTwice()
    {
        (PcScreen screen, int[] changes) = Watch(true, Rules());
        Assert.IsTrue(screen.Toggle());
        Assert.IsFalse(screen.IsOn);
        Assert.IsTrue(screen.Toggle());
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(2, changes[0]);
    }

    [Test]
    public void TurnOff_TurnsALitScreenOff_AndADarkOneNot()
    {
        (PcScreen screen, int[] changes) = Watch(true, Rules());
        Assert.IsTrue(screen.TurnOff());
        Assert.IsFalse(screen.IsOn);
        Assert.AreEqual(1, changes[0]);

        Assert.IsFalse(screen.TurnOff(), "already off");
        Assert.AreEqual(1, changes[0], "nothing changed, nothing raised");
    }

    [TestCase(WakeReason.TravellerPresented, true, false, true)]
    [TestCase(WakeReason.TravellerPresented, false, false, false)]
    [TestCase(WakeReason.TravellerPresented, true, true, false)]
    [TestCase(WakeReason.TravellerPresented, false, true, false)]
    [TestCase(WakeReason.ScanFinished, true, false, true)]
    [TestCase(WakeReason.ScanFinished, false, false, false)]
    [TestCase(WakeReason.ScanFinished, true, true, false)]
    [TestCase(WakeReason.ScanFinished, false, true, false)]
    public void Wake_TurnsOnOnlyADarkScreen_WhenTheReasonsOwnRuleIsOn(WakeReason reason, bool ruleOn, bool startsOn, bool wakes)
    {
        // The other reason's rule is always the opposite, so only the reason's own rule can decide.
        bool traveller = reason == WakeReason.TravellerPresented ? ruleOn : !ruleOn;
        (PcScreen screen, int[] changes) = Watch(startsOn, Rules(traveller, !traveller));
        Assert.AreEqual(wakes, screen.Wake(reason));
        Assert.AreEqual(startsOn || wakes, screen.IsOn);
        Assert.AreEqual(wakes ? 1 : 0, changes[0]);
    }

    [Test]
    public void NullRules_NeverWake()
    {
        (PcScreen screen, int[] changes) = Watch(false, null);
        Assert.IsFalse(screen.Wake(WakeReason.TravellerPresented));
        Assert.IsFalse(screen.Wake(WakeReason.ScanFinished));
        Assert.IsFalse(screen.IsOn);
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void AnUndefinedReason_NeverWakes()
    {
        (PcScreen screen, int[] changes) = Watch(false, Rules());
        Assert.IsFalse(screen.Wake((WakeReason)99));
        Assert.IsFalse(screen.IsOn);
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void Holding_TurnsADarkScreenOn_RaisingChangedOnce()
    {
        (PcScreen screen, int[] changes) = Watch(false, null);
        screen.SetHeld(true);
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(1, changes[0]);
    }

    [Test]
    public void Holding_ALitScreen_RaisesNothing()
    {
        (PcScreen screen, int[] changes) = Watch(true, Rules());
        screen.SetHeld(true);
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void AHeldScreen_RefusesToggleAndTurnOff_StayingOnAndRaisingNothing()
    {
        (PcScreen screen, int[] changes) = Watch(true, Rules());
        screen.SetHeld(true);
        Assert.IsFalse(screen.Toggle());
        Assert.IsFalse(screen.TurnOff());
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(0, changes[0]);
    }

    [Test]
    public void Releasing_ChangesNothing_ThenToggleAndTurnOffWorkAgain()
    {
        (PcScreen screen, int[] changes) = Watch(false, null);
        screen.SetHeld(true);
        screen.SetHeld(false);
        Assert.IsTrue(screen.IsOn, "releasing leaves the screen on");
        Assert.AreEqual(1, changes[0], "only the hold's own wake was raised");

        Assert.IsTrue(screen.TurnOff());
        Assert.IsFalse(screen.IsOn);
        Assert.IsTrue(screen.Toggle());
        Assert.IsTrue(screen.IsOn);
        Assert.AreEqual(3, changes[0]);
    }
}
