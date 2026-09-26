using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The clerk's Citizen Account (redesign phase 25; the PC spec's AC1, the
/// traveller-types spec's D1-D3): the statement's rows written at the shift's
/// end and at Home, the cap, OWED following the instalments, and the Record
/// Extract's rows with "–" where no source gives a value yet.
/// </summary>
public class AccountTests
{
    /// <summary>A stand-in for the engine's source.</summary>
    private sealed class Source : IClerkAccountSource
    {
        public ClerkContent Profile { get; set; } = new ClerkContent { citizenId = "773-2840-19", name = "Clerk", born = "9 Feb 2121", lineage = "Victorian Britain", employment = "Temporal Customs · Desk 3", note = "No remarks on file.", startDebt = 125430, garnishShare = 0.25f, reliefEmployer = "Tyburn Mills Consortium", reliefWorksite = "Victorian Britain", reliefWage = 420 };
        public int Balance { get; set; }
        public ClerkDebtState Debt { get; set; } = ClerkDebtState.Unknown;
        public int ShiftInstalment { get; set; } = Account.Unknown;
    }

    private static ShiftLedger Ledger(int pay, int penalty)
    {
        var ledger = new ShiftLedger();
        ledger.verdicts.Add(new CaseVerdict { payAwarded = pay, moneyPenalty = penalty });
        return ledger;
    }

    private static string Text(string key) => key == "account.unknown" ? "–" : key == "account.instalmentRate" ? "{0}% of pay" : key;

    private static string Cr(int amount) => amount + " cr";

    [Test]
    public void RecordShift_WritesTheLedgerAndTheSource()
    {
        var days = new List<AccountDay>();
        var source = new Source { Balance = 180 };

        AccountDay row = Account.RecordShift(days, 1, Ledger(100, 20), source, 60);

        Assert.AreSame(row, days.Single());
        Assert.AreEqual(1, row.day);
        Assert.AreEqual(100, row.wages);
        Assert.AreEqual(20, row.fines);
        Assert.AreEqual(Account.Unknown, row.debtRelief, "no instalment source before phase 13");
        Assert.AreEqual(180, row.balance);
        Assert.AreEqual(Account.Unknown, row.owed, "no debt source before phase 13");
    }

    [Test]
    public void RecordShift_AReplayedShiftReplacesTheDay()
    {
        var days = new List<AccountDay>();
        var source = new Source { Balance = 50 };
        Account.RecordShift(days, 2, Ledger(10, 0), source, 60);
        Account.RecordHome(days, 2, 30, 5, source, 60);

        Account.RecordShift(days, 2, Ledger(40, 0), source, 60);

        Assert.AreEqual(1, days.Count);
        Assert.AreEqual(40, days[0].wages);
        Assert.AreEqual(0, days[0].household, "Home's part starts empty again");
        Assert.AreEqual(0, days[0].purchases);
    }

    [Test]
    public void RecordHome_AddsHomesPartToTheShiftsRow()
    {
        var days = new List<AccountDay>();
        var source = new Source { Balance = 200 };
        Account.RecordShift(days, 1, Ledger(100, 0), source, 60);

        source.Balance = 120;
        Account.RecordHome(days, 1, 50, 30, source, 60);

        AccountDay row = days.Single();
        Assert.AreEqual(100, row.wages, "the shift's part stays");
        Assert.AreEqual(50, row.household);
        Assert.AreEqual(30, row.purchases);
        Assert.AreEqual(120, row.balance, "the wallet after Home");
    }

    [Test]
    public void Record_KeepsDayOrderAndDropsTheOldestPastTheCap()
    {
        var days = new List<AccountDay>();
        var source = new Source();
        Account.RecordShift(days, 3, Ledger(0, 0), source, 3);
        Account.RecordShift(days, 1, Ledger(0, 0), source, 3);
        Account.RecordShift(days, 2, Ledger(0, 0), source, 3);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, days.Select(d => d.day));

        Account.RecordShift(days, 4, Ledger(0, 0), source, 3);

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, days.Select(d => d.day));
        Assert.IsNull(Account.RecordShift(days, 0, Ledger(0, 0), source, 3), "no day 0");
    }

    [Test]
    public void Owed_FollowsTheInstalments()
    {
        var days = new List<AccountDay>();
        var source = new Source { Debt = ClerkDebtState.Of(1000, 0, 25, false), ShiftInstalment = 25 };

        Account.RecordShift(days, 1, Ledger(100, 0), source, 60);
        source.Debt = ClerkDebtState.Of(1000, 25, 25, false);
        Account.RecordShift(days, 2, Ledger(100, 0), source, 60);
        source.Debt = ClerkDebtState.Of(1000, 1200, 25, false);
        Account.RecordHome(days, 2, 0, 0, source, 60);

        Assert.AreEqual(1000, days[0].owed);
        Assert.AreEqual(25, days[0].debtRelief);
        Assert.AreEqual(0, days[1].owed, "never below 0 once paid off");
        Assert.IsTrue(source.Debt.PaidOff);
        Assert.AreEqual(Account.Unknown, ClerkDebtState.Unknown.Owed);
        Assert.IsFalse(ClerkDebtState.Unknown.PaidOff);
    }

    [Test]
    public void ExtractRows_ShowTheClerkWithUnknownDebtAsADash()
    {
        var source = new Source { Balance = 1250 };

        List<AccountRow> rows = Account.ExtractRows(source, Text, Cr);

        AccountRow Row(string label) => rows.Single(r => r.Label == label);
        Assert.AreEqual("773-2840-19", Row("account.row.citizenId").Value);
        Assert.AreEqual("Clerk", Row("account.row.name").Value);
        Assert.AreEqual("account.group.records", Row("account.row.citizenId").Group);
        Assert.AreEqual("account.status.eligible", Row("account.row.status").Value);
        Assert.AreEqual("account.standing.good", Row("account.row.standing").Value);
        Assert.AreEqual("–", Row("account.row.debt").Value);
        Assert.AreEqual("1250 cr", Row("account.row.balance").Value);
        Assert.AreEqual("Temporal Customs · Desk 3", Row("account.row.employment").Value, "no instalment rate without a debt source");
        Assert.AreEqual("account.group.forms", Row("account.row.employment").Group);
        Assert.AreEqual("account.departure.none", Row("account.row.departure").Value);
        Assert.AreEqual("account.group.travel", Row("account.row.departure").Group);
        Assert.AreEqual("No remarks on file.", rows.Last().Value, "the note comes last");
    }

    [Test]
    public void ExtractRows_WithADebtShowOwedRateStatusAndFrozen()
    {
        var source = new Source { Debt = ClerkDebtState.Of(125430, 55, 25, false) };
        List<AccountRow> rows = Account.ExtractRows(source, Text, Cr);
        Assert.AreEqual("125375 cr", rows.Single(r => r.Label == "account.row.debt").Value);
        Assert.AreEqual("Temporal Customs · Desk 3; 25% of pay", rows.Single(r => r.Label == "account.row.employment").Value);

        source.Debt = ClerkDebtState.Of(100, 100, 25, true);
        rows = Account.ExtractRows(source, Text, Cr);
        Assert.AreEqual("account.status.standard", rows.Single(r => r.Label == "account.row.status").Value, "Standard once paid off");
        Assert.AreEqual("account.standing.frozen", rows.Single(r => r.Label == "account.row.standing").Value);
        Assert.AreEqual("account.departure.booked", rows.Single(r => r.Label == "account.row.departure").Value);
    }

    [Test]
    public void ExtractRows_BlankAuthoredValuesReadAsADash()
    {
        var source = new Source { Profile = new ClerkContent { citizenId = "773-2840-19", name = "Clerk", employment = "Desk" } };
        Assert.AreEqual("–", Account.ExtractRows(source, Text, Cr).Single(r => r.Label == "account.row.lineage").Value);
    }

    [Test]
    public void StatementCells_AreTheColumnsInOrderWithDashes()
    {
        var d = new AccountDay { day = 3, wages = 1200, fines = 15, household = 60, purchases = 0, balance = -40 };

        CollectionAssert.AreEqual(new[] { "3", "1,200", "15", "–", "60", "0", "-40", "–" }, Account.StatementCells(d, Text));
    }

    [Test]
    public void ClerkContentProblems_RefuseABlankIdNameOrEmployment()
    {
        CollectionAssert.IsEmpty(new Source().Profile.Problems());
        List<string> problems = new ClerkContent().Problems();
        Assert.AreEqual(6, problems.Count, "the id, name and employment, and the Debt Relief contract's employer, worksite and wage (phase 13)");
        Assert.IsTrue(problems[0].Contains("citizenId"));
    }
}
