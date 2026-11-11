using System;
using NUnit.Framework;

/// <summary>
/// The shift's close (DayCycle.CloseShift) and the report's other money: the
/// close adds what its dialogs moved to the line, so the money the shift put
/// there before the close (the switchboard's group bonuses) stays on the
/// report and the statement (the audit of 2026-11-11, finding 1).
/// </summary>
public class ShiftCloseTests
{
    [Test]
    public void TheClose_KeepsTheOtherMoneyTheShiftAlreadyHolds()
    {
        var world = new WorldState { money = 60 };
        var ledger = new ShiftLedger { otherMoney = 10 };

        DayCycle.CloseShift(world, ledger, Array.Empty<CaseInstance>(), null, null, null);

        Assert.AreEqual(10, ledger.otherMoney, "a switchboard bonus of the day stays on the report's other money");
        Assert.AreEqual(60, world.money, "a close with no dialog moves no money");
    }
}
