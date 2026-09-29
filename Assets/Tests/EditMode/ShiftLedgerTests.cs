using NUnit.Framework;

public class ShiftLedgerTests
{
    private static CaseVerdict Verdict(bool correct, int pay = 0, int penalty = 0, float stability = 0f, bool unproven = false) =>
        new CaseVerdict { correct = correct, payAwarded = pay, moneyPenalty = penalty, stabilityDelta = stability, unprovenDenial = unproven };

    private static CaseVerdict Departure(TravellerKind kind, bool accepted, int debt = 0) =>
        new CaseVerdict { kind = kind, accepted = accepted, debt = debt, correct = true };

    /// <summary>The departure lines (traveller types §10; phase 9): accepted tourists are leisure departures, accepted labourers Debt Relief departures whose debts are put to work; denials and the displaced count for neither.</summary>
    [Test]
    public void Departures_CountTheAcceptedTouristsAndLabourers_AndSumTheLabourersDebt()
    {
        var ledger = new ShiftLedger();
        ledger.verdicts.Add(Departure(TravellerKind.RichTourist, true));
        ledger.verdicts.Add(Departure(TravellerKind.PoorTourist, true, debt: 12_000));
        ledger.verdicts.Add(Departure(TravellerKind.RichTourist, false));
        ledger.verdicts.Add(Departure(TravellerKind.Labourer, true, debt: 212_000));
        ledger.verdicts.Add(Departure(TravellerKind.Labourer, true, debt: 40_000));
        ledger.verdicts.Add(Departure(TravellerKind.Labourer, false, debt: 90_000));
        ledger.verdicts.Add(Departure(TravellerKind.Displaced, true));

        Assert.AreEqual(2, ledger.LeisureDepartures);
        Assert.AreEqual(2, ledger.DebtReliefDepartures);
        Assert.AreEqual(252_000, ledger.DebtPutToWork, "the accepted labourers' debts; a tourist's debt is not put to work");
        Assert.AreEqual(0, new ShiftLedger().LeisureDepartures);
        Assert.AreEqual(0, new ShiftLedger().DebtPutToWork);
    }

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
        Assert.AreEqual(0, ledger.strandedCount);
    }

    /// <summary>Redesign phase 23 (one penalty for any mistake): a stranding is a world consequence, never a fine, so the net line is the pay, the penalties and the instalment alone.</summary>
    [Test]
    public void NetMoney_AStrandingMovesNoMoney()
    {
        var ledger = new ShiftLedger();
        ledger.verdicts.Add(Verdict(true, pay: 220));
        ledger.verdicts.Add(Verdict(false, penalty: 15));
        ledger.debtInstalment = 55;
        ledger.strandedCount = 2;

        Assert.AreEqual(15, ledger.TotalPenalties, "the one wrong-decision penalty");
        Assert.AreEqual(150, ledger.NetMoney, "the wallet's change: 220 - 15 - 55, the two strandings costing nothing");
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
