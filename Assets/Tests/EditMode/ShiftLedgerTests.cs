using NUnit.Framework;

public class ShiftLedgerTests
{
    private static CaseVerdict Verdict(bool correct, int pay = 0, int penalty = 0, float stability = 0f, bool unproven = false) =>
        new CaseVerdict { correct = correct, payAwarded = pay, moneyPenalty = penalty, stabilityDelta = stability, unprovenDenial = unproven };

    [Test]
    public void Totals_SumAcrossVerdicts()
    {
        var ledger = new ShiftLedger();
        ledger.verdicts.Add(Verdict(true, pay: 10, stability: 1f));
        ledger.verdicts.Add(Verdict(false, penalty: 5, stability: -5f));
        ledger.verdicts.Add(Verdict(true, pay: 10));

        Assert.AreEqual(20, ledger.TotalPay);
        Assert.AreEqual(5, ledger.TotalPenalties);
        Assert.AreEqual(15, ledger.NetMoney);
        Assert.AreEqual(2, ledger.CorrectCount);
        Assert.AreEqual(1, ledger.WrongCount);
        Assert.AreEqual(-4f, ledger.TotalStabilityDelta, 0.0001f);
    }

    [Test]
    public void UnprovenDenialCount_CountsOnlyFlaggedVerdicts()
    {
        var ledger = new ShiftLedger();
        ledger.verdicts.Add(Verdict(false, unproven: true));
        ledger.verdicts.Add(Verdict(false));
        ledger.verdicts.Add(Verdict(true));

        Assert.AreEqual(1, ledger.UnprovenDenialCount);
    }

    [Test]
    public void EmptyLedger_IsAllZeroes()
    {
        var ledger = new ShiftLedger();

        Assert.AreEqual(0, ledger.TotalPay);
        Assert.AreEqual(0, ledger.NetMoney);
        Assert.AreEqual(0, ledger.CorrectCount);
        Assert.AreEqual(0, ledger.WrongCount);
        Assert.AreEqual(0, ledger.UnprovenDenialCount);
    }
}
