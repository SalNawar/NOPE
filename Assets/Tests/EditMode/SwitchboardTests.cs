using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The portal switchboard's rules (LOOP_v5 on Saleh's v3 board): an approved
/// traveller is a call that blinks until a module routes it; MASTER locks
/// every patched route in at once; a locked-in route calibrates 20 real
/// seconds only while one of the two power cords is in it, then sends
/// everyone waiting on it as one group and keeps the route; a wrong period
/// pops the fuse and costs time only.
/// </summary>
public class SwitchboardTests
{
    private static readonly SwitchRoute GreeceAncient = new SwitchRoute(0, 0);
    private static readonly SwitchRoute GreeceMedieval = new SwitchRoute(0, 1);
    private static readonly SwitchRoute ItalyAncient = new SwitchRoute(1, 0);

    /// <summary>A board of 5 modules (01 to 03 in service), 4 destinations of 3 periods, 2 power cords.</summary>
    private static Switchboard Board(SwitchboardRules rules = null)
    {
        var board = new Switchboard(rules ?? new SwitchboardRules(), 5, 4, 3);
        for (int m = 0; m < 3; m++)
            board.SetInService(m, true);
        return board;
    }

    private static List<SwitchEvent> Events(Switchboard board)
    {
        var events = new List<SwitchEvent>();
        board.TakeEvents(events);
        return events;
    }

    /// <summary>Routes <paramref name="module"/> to <paramref name="route"/> (its place and its period).</summary>
    private static void Patch(Switchboard board, int module, SwitchRoute route)
    {
        Assert.IsTrue(board.PlugPlace(module, route.Destination));
        Assert.IsTrue(board.PlugPeriod(module, route));
    }

    [Test]
    public void ACall_BlinksUntilAnswered_ThenCalibrates20Seconds_ThenTheGroupLeaves_AndTheLampShowsDone()
    {
        Switchboard board = Board();
        board.AddCall(GreeceAncient);
        Assert.AreEqual(SwitchCallLamp.Calling, board.CallLamp(0));
        Assert.AreEqual(SwitchEventKind.CallArrived, Events(board).Single().Kind);

        Patch(board, 0, GreeceAncient);
        Assert.IsTrue(board.PlugPower(0, 0));
        Assert.IsTrue(board.MasterPending, "a patched module asks for MASTER");
        MasterPull pull = board.PullMaster();
        Assert.AreEqual(1, pull.Committed);
        Assert.AreEqual(SwitchModuleState.Committed, board.Modules[0].State);
        Assert.AreEqual(SwitchCallLamp.Connected, board.CallLamp(0), "steady while it calibrates");
        Assert.AreEqual(20f, board.Modules[0].CalibrationLeft, 1e-4f, "Saleh: 20 s");

        board.Tick(19.5f);
        Assert.AreEqual(SwitchModuleState.Committed, board.Modules[0].State);
        Assert.AreEqual(1, board.Waiting(GreeceAncient), "nobody leaves before calibration ends");
        board.Tick(0.5f);
        Assert.AreEqual(SwitchModuleState.Open, board.Modules[0].State);
        Assert.AreEqual(0, board.Waiting(GreeceAncient));
        List<SwitchEvent> events = Events(board);
        CollectionAssert.AreEqual(new[] { SwitchEventKind.Committed, SwitchEventKind.Opened, SwitchEventKind.Departed }, events.Select(e => e.Kind).ToArray());
        Assert.AreEqual(1, events.Last().Travellers);
        Assert.AreEqual(0, events.Last().Bonus, "one traveller: no group bonus");
        Assert.AreEqual(SwitchCallLamp.Done, board.CallLamp(0), "DONE until the connection is cleared");
    }

    [Test]
    public void TwoCallsOnOneRoute_LeaveAsOneGroup_WithTheSmallBonus()
    {
        Switchboard board = Board();
        board.AddCall(GreeceAncient);
        board.AddCall(GreeceAncient);
        board.AddCall(GreeceAncient);
        Patch(board, 0, GreeceAncient);
        board.PlugPower(0, 0);
        board.PullMaster();
        board.Tick(20f);
        SwitchEvent departed = Events(board).Single(e => e.Kind == SwitchEventKind.Departed);
        Assert.AreEqual(3, departed.Travellers);
        Assert.AreEqual(10, departed.Bonus, "5 credits per extra traveller (the portals spec: 5(k-1))");
        Assert.AreEqual(10, board.GroupBonus(3));
        Assert.AreEqual(0, board.GroupBonus(1));
    }

    [Test]
    public void AnOpenPoweredRoute_AnswersTheNextCallAtOnce_AndAFinishedConnectionStaysPlugged()
    {
        Switchboard board = Board();
        Patch(board, 0, GreeceAncient);
        board.PlugPower(0, 0);
        Assert.AreEqual(1, board.PullMaster().Committed, "a route nobody calls yet may be opened ahead");
        board.Tick(20f);
        Assert.AreEqual(SwitchModuleState.Open, board.Modules[0].State);
        Events(board);

        board.AddCall(GreeceAncient);
        board.Tick(0.01f);
        Assert.AreEqual(0, board.Waiting(GreeceAncient), "no second calibration");
        Assert.AreEqual(1, Events(board).Count(e => e.Kind == SwitchEventKind.Departed));
        Assert.AreEqual(0, board.Modules[0].PlaceJack, "the connection stays plugged until it is moved");
    }

    [Test]
    public void Calibration_CountsOnlyWhilePowered_AndPowerIsACordYouMove()
    {
        Switchboard board = Board();
        board.AddCall(GreeceAncient);
        Patch(board, 0, GreeceAncient);
        board.PullMaster();
        board.Tick(30f);
        Assert.AreEqual(SwitchModuleState.Committed, board.Modules[0].State, "no power: no calibration");
        Assert.AreEqual(20f, board.Modules[0].CalibrationLeft, 1e-4f);

        board.PlugPower(1, 0);
        board.Tick(12f);
        board.UnplugPower(1);
        board.Tick(30f);
        Assert.AreEqual(8f, board.Modules[0].CalibrationLeft, 1e-3f, "it pauses with the cord out and keeps its progress");
        board.PlugPower(1, 0);
        board.Tick(8f);
        Assert.AreEqual(SwitchModuleState.Open, board.Modules[0].State);
        Assert.AreEqual(0, board.Waiting(GreeceAncient));
    }

    [Test]
    public void AnOpenRouteWithoutPower_HoldsItsCall_UntilACordComesBack()
    {
        Switchboard board = Board();
        Patch(board, 0, GreeceAncient);
        board.PlugPower(0, 0);
        board.PullMaster();
        board.Tick(20f);
        board.UnplugPower(0);
        board.AddCall(GreeceAncient);
        board.Tick(5f);
        Assert.AreEqual(1, board.Waiting(GreeceAncient));
        Assert.AreEqual(SwitchCallLamp.Connected, board.CallLamp(0), "its route is open: steady, not blinking");
        board.PlugPower(0, 0);
        board.Tick(0.01f);
        Assert.AreEqual(0, board.Waiting(GreeceAncient));
    }

    [Test]
    public void TheBoardHasTwoPowerCords_OneSocketTakesOne_AndMovingACordUnpowersWhereItWas()
    {
        Switchboard board = Board();
        Assert.AreEqual(2, board.PowerCords, "Saleh: 2 power cords for 5 modules");
        Assert.IsTrue(board.PlugPower(0, 0));
        Assert.IsFalse(board.CanPlugPower(1, 0), "module 01's socket holds cord A");
        Assert.IsTrue(board.PlugPower(1, 1));
        Assert.IsTrue(board.PlugPower(0, 2), "cord A moves from 01 to 03");
        Assert.IsFalse(board.Modules[0].Powered);
        Assert.IsTrue(board.Modules[2].Powered);
        Assert.AreEqual(2, board.PowerCordModule(0));
        Assert.IsFalse(board.PlugPower(2, 0), "there is no third cord");
        Assert.IsFalse(board.CanPlugPower(0, 3), "module 04 is LOCKED");
        board.UnplugPower(0);
        Assert.AreEqual(-1, board.PowerCordModule(0));
    }

    [Test]
    public void APeriodJackOfAnotherDestination_PopsTheFuse_ForTimeOnly()
    {
        var rules = new SwitchboardRules { FuseSeconds = 2f };
        Switchboard board = Board(rules);
        board.AddCall(GreeceAncient);
        Events(board);
        board.PlugPlace(0, 0);
        board.PlugPeriod(0, ItalyAncient);
        board.PlugPower(0, 0);
        MasterPull pull = board.PullMaster();
        Assert.AreEqual(1, pull.Fused);
        Assert.AreEqual(SwitchModuleState.Blown, board.Modules[0].State);
        Assert.AreEqual(SwitchEventKind.FusePopped, Events(board).Single().Kind);
        Assert.AreEqual(1, board.Waiting(GreeceAncient), "the traveller still waits: only time is lost");

        Assert.IsTrue(board.PullMaster().Idle, "a blown module takes no pull");
        board.Tick(2f);
        Assert.AreEqual(SwitchModuleState.Patching, board.Modules[0].State, "the fuse resets; the cords are still in");
        Assert.AreEqual(SwitchEventKind.FuseReset, Events(board).Single().Kind);

        board.PlugPeriod(0, GreeceAncient);
        Assert.AreEqual(1, board.PullMaster().Committed, "fixed and pulled again");
    }

    [Test]
    public void ThePeriodNobodyCallsFor_AtACallingDestination_PopsTheFuse()
    {
        Switchboard board = Board();
        board.AddCall(GreeceAncient);
        Patch(board, 0, GreeceMedieval);
        board.PlugPower(0, 0);
        Assert.AreEqual(1, board.PullMaster().Fused, "Saleh: a wrong period pops a fuse");
    }

    [Test]
    public void AJackTakesOnePlug_SoOneDestinationsTwoPeriodsLeaveOneAfterTheOther()
    {
        Switchboard board = Board();
        board.AddCall(GreeceAncient);
        board.AddCall(GreeceMedieval);
        Patch(board, 0, GreeceAncient);
        Assert.AreEqual(0, board.PlaceJackHolder(0));
        Assert.AreEqual(0, board.PeriodJackHolder(GreeceAncient));
        Assert.IsFalse(board.CanPlugPlace(1, 0), "one plug per jack");
        Assert.IsFalse(board.CanPlugPeriod(1, GreeceAncient));
        Assert.IsTrue(board.CanPlugPeriod(1, GreeceMedieval));
        board.PlugPower(0, 0);
        Assert.AreEqual(1, board.PullMaster().Committed, "Ancient is one of the periods calling there");
        board.Tick(20f);
        Assert.AreEqual(SwitchCallLamp.Calling, board.CallLamp(0), "Medieval still waits");
        board.UnplugPeriod(0);
        board.PlugPeriod(0, GreeceMedieval);
        Assert.AreEqual(1, board.PullMaster().Committed);
        board.Tick(20f);
        Assert.AreEqual(0, board.WaitingAt(0));
    }

    [Test]
    public void ALockedInRoute_ServesItsCall_SoAnotherModuleMayOpenAnotherPeriodAhead()
    {
        Switchboard board = Board();
        board.AddCall(ItalyAncient);
        Patch(board, 0, ItalyAncient);
        board.PullMaster();
        Assert.AreEqual(SwitchCallLamp.Connected, board.CallLamp(1), "every waiting route there is locked in");
        Patch(board, 1, GreeceMedieval);
        Assert.AreEqual(1, board.PullMaster().Committed, "nobody calls Greece: opened ahead");
        Assert.AreEqual(SwitchCallLamp.Connected, board.CallLamp(0), "a route locked in with nobody sent yet");
    }

    [Test]
    public void PullingACord_UnlocksTheRoute_AndItsCalibrationStartsOver()
    {
        Switchboard board = Board();
        board.AddCall(GreeceAncient);
        Patch(board, 0, GreeceAncient);
        board.PlugPower(0, 0);
        board.PullMaster();
        board.Tick(15f);
        board.UnplugPeriod(0);
        Assert.AreEqual(SwitchModuleState.Patching, board.Modules[0].State);
        Assert.AreEqual(SwitchCallLamp.Calling, board.CallLamp(0));
        board.PlugPeriod(0, GreeceAncient);
        board.PullMaster();
        Assert.AreEqual(20f, board.Modules[0].CalibrationLeft, 1e-4f);
        board.UnplugPlace(0);
        board.UnplugPeriod(0);
        Assert.AreEqual(SwitchModuleState.Free, board.Modules[0].State, "both cords stowed: free for the next call");
    }

    [Test]
    public void MasterWithNothingPatched_ChangesNothing()
    {
        Switchboard board = Board();
        Assert.IsFalse(board.MasterPending);
        Assert.IsTrue(board.PullMaster().Idle);
        board.PlugPlace(0, 0);
        Assert.IsFalse(board.MasterPending, "a half-patched module does not ask for MASTER");
        Assert.IsTrue(board.PullMaster().Idle);
    }

    [Test]
    public void CallsBlinkFaster_TheLongerTheyWait()
    {
        var rules = new SwitchboardRules { BlinkHz = 1f, BlinkHzMax = 4f, BlinkRampSeconds = 60f };
        Switchboard board = Board(rules);
        board.AddCall(GreeceAncient);
        Assert.AreEqual(1f, board.BlinkHz(0), 1e-4f);
        board.Tick(30f);
        Assert.AreEqual(2.5f, board.BlinkHz(0), 1e-4f);
        Assert.AreEqual(30f, board.OldestWait(0), 1e-4f);
        board.Tick(120f);
        Assert.AreEqual(4f, board.BlinkHz(0), 1e-4f);
        Assert.AreEqual(1, board.WaitingAt(0));
        Assert.AreEqual(1, board.WaitingTotal);
    }

    [Test]
    public void AClosedModule_TakesNoCord_AndClosingOneDropsItsCords()
    {
        Switchboard board = Board();
        Assert.AreEqual(SwitchModuleState.Closed, board.Modules[3].State);
        Assert.IsFalse(board.PlugPlace(3, 0));
        Patch(board, 1, GreeceAncient);
        board.PlugPower(1, 1);
        board.SetInService(1, false);
        Assert.AreEqual(SwitchModuleState.Closed, board.Modules[1].State);
        Assert.AreEqual(-1, board.PlaceJackHolder(0));
        Assert.AreEqual(-1, board.PeriodJackHolder(GreeceAncient));
        Assert.AreEqual(-1, board.PowerCordModule(1), "its power cord is parked");
    }

    [Test]
    public void TheRamp_Opens01OnDay1_02OnDay3_03OnDay5_And04And05Never()
    {
        var rules = new SwitchboardRules();
        Assert.IsTrue(rules.ModuleOpen(0, 1));
        Assert.IsFalse(rules.ModuleOpen(1, 2));
        Assert.IsTrue(rules.ModuleOpen(1, 3));
        Assert.IsFalse(rules.ModuleOpen(2, 4));
        Assert.IsTrue(rules.ModuleOpen(2, 5));
        Assert.IsFalse(rules.ModuleOpen(3, 15), "04 is LOCKED");
        Assert.IsFalse(rules.ModuleOpen(4, 15), "05 is OUT OF SERVICE");
        Assert.IsFalse(rules.ModuleOpen(5, 1));
        Assert.IsFalse(new SwitchboardRules { ModuleFromDay = null }.ModuleOpen(0, 1));
    }

    [Test]
    public void ACallOffTheBoard_IsIgnored()
    {
        Switchboard board = Board();
        board.AddCall(new SwitchRoute(4, 0));
        board.AddCall(new SwitchRoute(0, 3));
        Assert.AreEqual(0, board.WaitingTotal);
        Assert.AreEqual(0, Events(board).Count);
    }
}
