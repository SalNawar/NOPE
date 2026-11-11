using System;
using NUnit.Framework;
using UnityEngine;

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

    /// <summary>The decision's money (ShiftScoring.ResolveDecision over VerdictRules): a right verdict pays the base pay; the day's first wrong one is the free warning; the next one costs the penalty. NeedsEditor: the tuning is a ScriptableObject.</summary>
    [Test, Category("NeedsEditor")]
    public void ADecision_PaysWhenRight_WarnsOnce_ThenFines()
    {
        GameConfigSO config = ScriptableObject.CreateInstance<GameConfigSO>();
        try
        {
            config.requireEvidenceToDeny = false;
            var world = new WorldState { money = 100 };
            var honest = new CaseInstance();

            CaseVerdict right = ShiftScoring.ResolveDecision(honest, DeskStamp.Approved, 1, world, config);
            Assert.IsTrue(right.correct, "an honest traveller approved");
            Assert.AreEqual(config.basePayPerCorrect, right.payAwarded);
            Assert.AreEqual(100 + config.basePayPerCorrect, world.money);

            int before = world.money;
            CaseVerdict warned = ShiftScoring.ResolveDecision(honest, DeskStamp.Denied, 2, world, config);
            Assert.IsFalse(warned.correct, "an honest traveller denied");
            Assert.IsTrue(warned.wasFreeWarning, "the day's first citation is the free warning");
            Assert.AreEqual(before, world.money, "a warning costs nothing");

            CaseVerdict fined = ShiftScoring.ResolveDecision(honest, DeskStamp.Denied, 3, world, config);
            Assert.IsFalse(fined.wasFreeWarning);
            Assert.AreEqual(config.wrongDecisionPenalty, fined.moneyPenalty);
            Assert.AreEqual(before - config.wrongDecisionPenalty, world.money, "one penalty for one wrong decision");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(config);
        }
    }
}
