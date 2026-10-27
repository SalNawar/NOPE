using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// C's next-tool lamps (Saleh, DESK APPROVED: "Mix: B + A rail + C lamps"),
/// the data hook FEEDBACK and DESK-HOMES render: which home lights for the
/// case's next step, and one lamp lit at a time with an event on change.
/// </summary>
public class DeskLampsTests
{
    [Test]
    public void BetweenTravellers_TheBellLights_OnlyWhileTheBoothIsOpen()
    {
        Assert.AreEqual(DeskHomeMap.Bell, NextToolLamp.For(new CaseLampState(false, true, false)));
        Assert.IsNull(NextToolLamp.For(new CaseLampState(false, false, false)), "a closed booth calls no one");
    }

    [Test]
    public void DuringACase_TheOwedMachineLights_InTheRailsOrder_ThenTheDealTrayOnceStamped()
    {
        Assert.IsNull(NextToolLamp.For(new CaseLampState(true, true, false)), "reading on the mat: no tool");
        Assert.AreEqual(DeskHomeMap.ChipReader, NextToolLamp.For(new CaseLampState(true, true, false, chipOwed: true, scanOwed: true)), "the rail's first machine first");
        Assert.AreEqual(DeskHomeMap.Scanner, NextToolLamp.For(new CaseLampState(true, true, false, scanOwed: true)));
        Assert.AreEqual(DeskHomeMap.DealTray, NextToolLamp.For(new CaseLampState(true, true, true, scanOwed: true)), "stamped: it goes back over the deal tray");
    }

    [Test]
    public void TheLamps_LightOneHomeAtATime_AndSayWhenItChanges()
    {
        var lamps = new DeskLamps();
        var heard = new List<string>();
        lamps.Changed += id => heard.Add(id ?? "none");
        Assert.IsTrue(lamps.Show(DeskHomeMap.Bell));
        Assert.IsFalse(lamps.Show(DeskHomeMap.Bell), "no change, no event");
        Assert.IsTrue(lamps.Show(DeskHomeMap.DealTray));
        Assert.IsTrue(lamps.Show(""));
        Assert.IsNull(lamps.Lit);
        CollectionAssert.AreEqual(new[] { DeskHomeMap.Bell, DeskHomeMap.DealTray, "none" }, heard);
    }

    [Test]
    public void EveryLampHome_IsAHomeOfTheApprovedMap()
    {
        DeskHome[] homes = DeskHomeMap.Approved();
        foreach (string id in new[] { DeskHomeMap.Bell, DeskHomeMap.ChipReader, DeskHomeMap.Scanner, DeskHomeMap.DealTray })
            Assert.IsNotNull(DeskHomeMap.Find(homes, id), id);
    }
}
