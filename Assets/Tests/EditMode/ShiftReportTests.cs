using NUnit.Framework;

/// <summary>
/// Papers Please lesson 5 (wave 5 track C): the shift report's money at a
/// glance, every number the player needs, none hidden in a total
/// (ShiftReport.From over the shift's ledger).
/// </summary>
public class ShiftReportTests
{
    private static CaseVerdict Right(int pay) => new CaseVerdict { correct = true, payAwarded = pay };

    private static CaseVerdict Wrong(int penalty, bool free) => new CaseVerdict { correct = false, moneyPenalty = penalty, wasFreeWarning = free };

    [Test]
    public void From_TheSpeedAndTheAccuracyAtAGlance()
    {
        var ledger = new ShiftLedger();
        for (int i = 0; i < 7; i++)
            ledger.verdicts.Add(Right(10));
        ledger.verdicts.Add(Wrong(0, true));
        ledger.verdicts.Add(Wrong(25, false));
        ledger.verdicts.Add(Wrong(25, false));
        ledger.strandedCount = 1;
        ledger.strandingFines = 100;
        ledger.debtInstalment = 17;
        ledger.debtOwed = 125000;

        ShiftReport r = ShiftReport.From(ledger, 12, 83, 62);

        Assert.AreEqual(10, r.Processed);
        Assert.AreEqual(2, r.Waiting, "the queue less the processed: the travellers the clock left waiting");
        Assert.AreEqual(7, r.Right);
        Assert.AreEqual(10, r.PayRate, "every right call paid the same");
        Assert.AreEqual(70, r.Pay);
        Assert.AreEqual(1, r.Warned, "the day's free warning costs nothing");
        Assert.AreEqual(2, r.Fined);
        Assert.AreEqual(3, r.Wrong);
        Assert.AreEqual(25, r.PenaltyRate);
        Assert.AreEqual(50, r.Penalties);
        Assert.AreEqual(1, r.Stranded);
        Assert.AreEqual(100, r.StrandingFines);
        Assert.AreEqual(17, r.Instalment);
        Assert.AreEqual(125000, r.DebtOwed);
        Assert.AreEqual(70 - 50 - 100 - 17, r.Net);
        Assert.AreEqual(83 - r.Net, r.WalletBefore, "the wallet when the shift began");
        Assert.AreEqual(83 - 62, r.WalletAfterBills, "the wallet once tonight's bills are paid");
    }

    [Test]
    public void From_ARateIsShownOnlyWhenEveryAmountShares_It()
    {
        var ledger = new ShiftLedger();
        ledger.verdicts.Add(Right(10));
        ledger.verdicts.Add(Right(25));
        ShiftReport r = ShiftReport.From(ledger, 2, 0, 0);
        Assert.AreEqual(0, r.PayRate, "a premade's bonus: the rows show the total");
        Assert.AreEqual(35, r.Pay);
        Assert.AreEqual(0, r.PenaltyRate, "none fined");
        Assert.AreEqual(0, r.Waiting);
    }

    [Test]
    public void From_OtherMoney_JoinsTheNet_AsTheLedgerCountsIt()
    {
        var ledger = new ShiftLedger();
        ledger.verdicts.Add(Right(10));
        ledger.otherMoney = 200;
        ShiftReport r = ShiftReport.From(ledger, 1, 260, 45);
        Assert.AreEqual(200, r.Other, "a bribe taken at the desk is shown, never hidden");
        Assert.AreEqual(210, r.Net);
        Assert.AreEqual(ledger.NetMoney, r.Net, "the report and the ledger count the same net");
    }

    [Test]
    public void From_NoLedger_IsAnEmptyReport()
    {
        ShiftReport r = ShiftReport.From(null, -3, 50, -5);
        Assert.AreEqual(0, r.Queued);
        Assert.AreEqual(0, r.Processed);
        Assert.AreEqual(0, r.Net);
        Assert.AreEqual(0, r.Bills, "a bill is never negative");
        Assert.AreEqual(Account.Unknown, r.DebtOwed);
        Assert.AreEqual(50, r.WalletAfterBills);
    }
}
