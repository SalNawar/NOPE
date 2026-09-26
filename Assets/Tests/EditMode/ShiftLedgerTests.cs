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

    /// <summary>The citation's mistake key (the plan's phase 7 generalises the fault reasons; phase 10 brings the first, the costume error's panic).</summary>
    [TestCase(true, false, "", "citation.acceptedWrong")]
    [TestCase(true, false, null, "citation.acceptedWrong")]
    [TestCase(true, false, "panic", "citation.acceptedWrong.panic")]
    [TestCase(false, false, "panic", "citation.deniedWrong")]
    [TestCase(false, true, "panic", "citation.unproven")]
    [TestCase(false, true, "", "citation.unproven")]
    public void MistakeKey_ByTheDecisionAndTheFaultReason(bool accepted, bool unproven, string reason, string expected)
    {
        Assert.AreEqual(expected, new CaseVerdict { accepted = accepted, unprovenDenial = unproven, faultReason = reason }.MistakeKey);
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
        Assert.AreEqual(0, ledger.debtInstalment);
        Assert.AreEqual(Account.Unknown, ledger.debtOwed, "no debt known before the shift's end");
    }

    [Test]
    public void NetMoney_IsPayLessPenaltiesLessTheDebtInstalment()
    {
        var ledger = new ShiftLedger();
        ledger.verdicts.Add(Verdict(true, pay: 220));
        ledger.verdicts.Add(Verdict(false, penalty: 15));
        ledger.debtInstalment = 55;

        Assert.AreEqual(220, ledger.TotalPay, "the pay stays the whole pay (the statement's WAGES)");
        Assert.AreEqual(150, ledger.NetMoney, "the wallet's change: 220 - 15 - 55");
    }
}
