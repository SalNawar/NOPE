using NUnit.Framework;

/// <summary>The AVAILABLE sign's state machine (Saleh 2026-09-30): one click and travellers come one after another; a second click pauses after the current one.</summary>
public class DeskAvailabilityTests
{
    private static DeskAvailability Desk(out int[] calls, out int[] changes)
    {
        var desk = new DeskAvailability();
        int[] c = { 0 }, ch = { 0 };
        desk.Called += () => c[0]++;
        desk.Changed += () => ch[0]++;
        calls = c;
        changes = ch;
        return desk;
    }

    [Test]
    public void TheShiftStarts_Paused_AndNobodyIsCalled_UntilTheSignIsClicked()
    {
        DeskAvailability desk = Desk(out int[] calls, out _);
        Assert.IsFalse(desk.IsAvailable, "the sign starts off");

        desk.Arm();
        Assert.AreEqual(0, calls[0], "a waiting traveller is not called while paused");
        Assert.IsTrue(desk.IsWaiting);

        desk.Toggle();
        Assert.AreEqual(1, calls[0], "turning the desk available calls the waiting traveller at once");
        Assert.IsFalse(desk.IsWaiting);
    }

    [Test]
    public void OneClick_CallsTravellers_OneAfterAnother_AsEachLeaves()
    {
        DeskAvailability desk = Desk(out int[] calls, out _);
        desk.Toggle();
        Assert.AreEqual(0, calls[0], "nobody waits yet");

        for (int traveller = 1; traveller <= 3; traveller++)
        {
            desk.Arm();
            Assert.AreEqual(traveller, calls[0], $"traveller {traveller} is called as soon as their slot starts");
            desk.SetDeparting(true);
            desk.SetDeparting(false);
        }

        Assert.IsTrue(desk.IsAvailable, "the sign stays on without another click");
    }

    [Test]
    public void TheNextTraveller_WaitsForTheLastOneToLeave()
    {
        DeskAvailability desk = Desk(out int[] calls, out _);
        desk.Toggle();
        desk.SetDeparting(true);
        desk.Arm();
        Assert.AreEqual(0, calls[0], "the last traveller's reaction still lingers");

        desk.SetDeparting(false);
        Assert.AreEqual(1, calls[0], "they left: the next is called");
    }

    [Test]
    public void ASecondClick_Pauses_NobodyNewIsCalled_UntilClickedAgain()
    {
        DeskAvailability desk = Desk(out int[] calls, out int[] changes);
        desk.Toggle();
        desk.Arm();
        Assert.AreEqual(1, calls[0]);

        desk.Toggle();
        Assert.IsFalse(desk.IsAvailable);
        desk.SetDeparting(true);
        desk.SetDeparting(false);
        desk.Arm();
        Assert.AreEqual(1, calls[0], "paused: the next traveller waits");

        desk.Toggle();
        Assert.AreEqual(2, calls[0], "resumed: the waiting traveller is called");
        Assert.AreEqual(3, changes[0], "each click changes the sign");
    }

    [Test]
    public void Close_NeverCallsTheWaitingTraveller_AndTheSignStaysOff()
    {
        DeskAvailability desk = Desk(out int[] calls, out int[] changes);
        desk.Toggle();
        desk.SetDeparting(true);
        desk.Arm();

        desk.Close();
        Assert.IsTrue(desk.IsClosed);
        Assert.IsFalse(desk.IsAvailable, "the sign goes off at closing");
        Assert.AreEqual(2, changes[0], "on, then off at closing");

        desk.SetDeparting(false);
        desk.Toggle();
        desk.Arm();
        Assert.AreEqual(0, calls[0], "nobody is called after closing");
        Assert.IsFalse(desk.IsAvailable, "the sign no longer turns on");
    }

    [Test]
    public void ClosingWhilePaused_ChangesNothingOnTheSign()
    {
        DeskAvailability desk = Desk(out _, out int[] changes);
        desk.Close();
        Assert.AreEqual(0, changes[0]);
    }
}
