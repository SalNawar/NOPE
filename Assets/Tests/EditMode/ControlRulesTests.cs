using System;
using NUnit.Framework;

/// <summary>
/// The one input model (Saleh 2026-10-06, Papers, Please's controls): SPACE,
/// TAB and Q do the same from every desk state; right-click and Esc back out
/// of the innermost mode, one per press, in one order everywhere.
/// </summary>
public class ControlRulesTests
{
    private static readonly ControlState Desk = new ControlState(travellerAtDesk: true);

    [Test]
    public void SpaceAndTab_ToggleInspectAndTheStamps_FromEveryDeskState()
    {
        ControlState[] states =
        {
            Desk, new ControlState(travellerAtDesk: true, deskView: true), new ControlState(travellerAtDesk: true, inspecting: true),
            new ControlState(travellerAtDesk: true, stampsOut: true), new ControlState(travellerAtDesk: true, wheelOpen: true),
            new ControlState(travellerAtDesk: true, cityView: true), new ControlState(travellerAtDesk: true, dragging: true),
            new ControlState(travellerAtDesk: true, valueHeld: true, inspecting: true, stampsOut: true, deskView: true),
        };
        foreach (ControlState s in states)
        {
            Assert.AreEqual(ControlAction.ToggleInspect, ControlRules.Resolve(ControlKey.Inspect, s));
            Assert.AreEqual(ControlAction.ToggleStamps, ControlRules.Resolve(ControlKey.Stamps, s));
            Assert.AreEqual(ControlAction.OpenPc, ControlRules.Resolve(ControlKey.Pc, s));
        }
    }

    [Test]
    public void WithNoTraveller_SpaceAndTabDoNothing_ButThePcKeyWorks()
    {
        var empty = new ControlState();
        Assert.AreEqual(ControlAction.None, ControlRules.Resolve(ControlKey.Inspect, empty));
        Assert.AreEqual(ControlAction.None, ControlRules.Resolve(ControlKey.Stamps, empty));
        Assert.AreEqual(ControlAction.OpenPc, ControlRules.Resolve(ControlKey.Pc, empty));
    }

    [Test]
    public void OnThePc_OnlyQ_GoesBack_AndNotWhileAFieldTypesIt()
    {
        var pc = new ControlState(pcOpen: true, travellerAtDesk: true);
        Assert.AreEqual(ControlAction.ClosePc, ControlRules.Resolve(ControlKey.Pc, pc));
        Assert.AreEqual(ControlAction.None, ControlRules.Resolve(ControlKey.Inspect, pc), "the PC keeps SPACE for its rows");
        Assert.AreEqual(ControlAction.None, ControlRules.Resolve(ControlKey.Stamps, pc), "the PC keeps TAB for its regions");
        Assert.AreEqual(ControlAction.None, ControlRules.Resolve(ControlKey.CityLeft, pc));
        Assert.AreEqual(ControlAction.None, ControlRules.Resolve(ControlKey.Pc, new ControlState(pcOpen: true, textFieldFocused: true)));
    }

    [Test]
    public void ANewsletter_TakesNoKeys_AndNoBackOut()
    {
        var news = new ControlState(newsletter: true, travellerAtDesk: true, deskView: true);
        foreach (ControlKey key in Enum.GetValues(typeof(ControlKey)))
            Assert.AreEqual(ControlAction.None, ControlRules.Resolve(key, news), key.ToString());
        Assert.AreEqual(BackOutStep.None, ControlRules.BackOut(news));
    }

    [Test]
    public void TheCityKeys_TurnAndTurnBack()
    {
        Assert.AreEqual(ControlAction.LookAtCity, ControlRules.Resolve(ControlKey.CityLeft, Desk));
        Assert.AreEqual(ControlAction.None, ControlRules.Resolve(ControlKey.CityRight, Desk));
        var city = new ControlState(travellerAtDesk: true, cityView: true);
        Assert.AreEqual(ControlAction.LeaveCity, ControlRules.Resolve(ControlKey.CityRight, city));
        Assert.AreEqual(ControlAction.None, ControlRules.Resolve(ControlKey.CityLeft, city));
        Assert.AreEqual(ControlAction.ShowKeys, ControlRules.Resolve(ControlKey.Help, Desk));
    }

    [Test]
    public void BackOut_PeelsOneModeAtATime_InnermostFirst()
    {
        var s = new ControlState(travellerAtDesk: true, dragging: true, wheelOpen: true, pcOpen: true, valueHeld: true, inspecting: true,
                                 stampsOut: true, cityView: true, deskView: true);
        BackOutStep[] order =
        {
            BackOutStep.CancelDrag, BackOutStep.CloseWheel, BackOutStep.ClosePc, BackOutStep.DropValue, BackOutStep.LeaveInspect,
            BackOutStep.StowStamps, BackOutStep.LeaveCity, BackOutStep.LeaveDeskView, BackOutStep.None
        };
        foreach (BackOutStep expected in order)
        {
            BackOutStep step = ControlRules.BackOut(s);
            Assert.AreEqual(expected, step);
            s = Without(s, step);
        }
    }

    [Test]
    public void BackOut_InMatching_LetsGoOfTheValue_ThenLeavesInspect()
    {
        Assert.AreEqual(BackOutStep.DropValue, ControlRules.BackOut(new ControlState(travellerAtDesk: true, inspecting: true, valueHeld: true)));
        Assert.AreEqual(BackOutStep.LeaveInspect, ControlRules.BackOut(new ControlState(travellerAtDesk: true, inspecting: true)));
        Assert.AreEqual(BackOutStep.None, ControlRules.BackOut(Desk));
    }

    [Test]
    public void TheDeskCard_PrintsTheSameKeysAsTheTabs()
    {
        var keys = new System.Collections.Generic.List<string>();
        foreach (ShortcutCardRow row in ControlRules.DeskCard)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.TextKey));
            Assert.IsEmpty(row.Commands, "the desk's rows are no PC commands");
            keys.Add(row.Keys);
        }
        CollectionAssert.Contains(keys, ControlRules.InspectKey);
        CollectionAssert.Contains(keys, ControlRules.StampsKey);
        CollectionAssert.Contains(keys, ControlRules.PcKey);
        CollectionAssert.Contains(keys, ControlRules.BackKeys);
    }

    /// <summary>The state once <paramref name="step"/> backed out.</summary>
    private static ControlState Without(ControlState s, BackOutStep step) => new ControlState(
        pcOpen: s.PcOpen && step != BackOutStep.ClosePc, newsletter: s.Newsletter, travellerAtDesk: s.TravellerAtDesk,
        wheelOpen: s.WheelOpen && step != BackOutStep.CloseWheel, valueHeld: s.ValueHeld && step != BackOutStep.DropValue,
        inspecting: s.Inspecting && step != BackOutStep.LeaveInspect, stampsOut: s.StampsOut && step != BackOutStep.StowStamps,
        cityView: s.CityView && step != BackOutStep.LeaveCity, deskView: s.DeskView && step != BackOutStep.LeaveDeskView,
        dragging: s.Dragging && step != BackOutStep.CancelDrag);
}
