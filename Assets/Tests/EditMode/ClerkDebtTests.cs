using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The clerk's debt (redesign phase 13; the traveller-types spec's D1-D3): the
/// share of pay, the instalment (never past what is owed; a paid-off debt
/// stops them), the account's debt state across shifts, the ending that
/// freezes it, and the clerk's own Debt Relief Labour Contract (TC-520).
/// </summary>
public class ClerkDebtTests
{
    private static ClerkContent Clerk(int startDebt = 125430, float share = 0.25f) => new ClerkContent
    {
        citizenId = "773-2840-19", name = "Theo Marlow", born = "9 Feb 2121", lineage = "Victorian Britain",
        employment = "Temporal Customs · Desk 3", note = "No remarks on file.",
        startDebt = startDebt, garnishShare = share,
        reliefEmployer = "Tyburn Mills Consortium", reliefWorksite = "Victorian Britain", reliefWage = 420
    };

    private static string Text(string key) => key == "account.unknown" ? "–" : key == "contract.term" ? "{0:N0} days" : key;

    private static string Cr(int amount) => amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " cr";

    [TestCase(0.25f, 25)]
    [TestCase(0f, 0)]
    [TestCase(1f, 100)]
    [TestCase(0.333f, 33)]
    [TestCase(-0.1f, 0, Description = "never below 0")]
    [TestCase(1.5f, 100, Description = "never above the whole pay")]
    public void SharePercent_IsTheShareInWholePercent(float share, int expected)
    {
        Assert.AreEqual(expected, ClerkDebt.SharePercent(share));
    }

    [TestCase(220, 25, 125430, 55, Description = "the spec's example: 25% of 220 cr")]
    [TestCase(10, 25, 1000, 2, Description = "rounded down")]
    [TestCase(220, 25, 30, 30, Description = "never past what is owed")]
    [TestCase(220, 25, 0, 0, Description = "a paid-off debt stops the instalments")]
    [TestCase(0, 25, 1000, 0, Description = "no pay, no instalment")]
    [TestCase(-40, 25, 1000, 0)]
    [TestCase(220, 0, 1000, 0, Description = "a 0% share takes nothing")]
    [TestCase(220, 25, -5, 0)]
    public void Instalment_IsTheShareOfPayUpToWhatIsOwed(int pay, int percent, int owed, int expected)
    {
        Assert.AreEqual(expected, ClerkDebt.Instalment(pay, percent, owed));
    }

    [Test]
    public void Instalment_OfAHugePayDoesNotOverflow()
    {
        Assert.AreEqual(int.MaxValue / 2, ClerkDebt.Instalment(int.MaxValue, 50, int.MaxValue));
    }

    [Test]
    public void State_IsTheAuthoredDebtLessWhatWasPaid()
    {
        ClerkDebtState debt = ClerkDebt.State(Clerk(), 55, false);

        Assert.IsTrue(debt.Known);
        Assert.AreEqual(125430, debt.StartDebt);
        Assert.AreEqual(125375, debt.Owed);
        Assert.AreEqual(25, debt.SharePercent);
        Assert.IsFalse(debt.Frozen);
        Assert.IsFalse(debt.PaidOff);
        Assert.IsTrue(ClerkDebt.State(Clerk(), 0, true).Frozen, "the bankrupt ending freezes it");
        Assert.IsFalse(ClerkDebt.State(null, 0, false).Known, "no profile, no debt");
    }

    [Test]
    public void TheDebtGoesDownShiftByShift_AndStopsWhenPaidOff()
    {
        ClerkContent clerk = Clerk(startDebt: 100);
        int paid = 0;
        var owed = new List<int>();
        foreach (int pay in new[] { 220, 220, 220 })
        {
            ClerkDebtState before = ClerkDebt.State(clerk, paid, false);
            paid += ClerkDebt.Instalment(pay, before.SharePercent, before.Owed);
            owed.Add(ClerkDebt.State(clerk, paid, false).Owed);
        }

        CollectionAssert.AreEqual(new[] { 45, 0, 0 }, owed, "55 a shift, then the last 45, then nothing");
        Assert.AreEqual(100, paid, "never more than the starting debt");
        Assert.IsTrue(ClerkDebt.State(clerk, paid, false).PaidOff, "the status then reads Standard");
    }

    [Test]
    public void Freezes_OnlyTheBankruptEnding()
    {
        Assert.IsTrue(ClerkDebt.Freezes(EndingConditionType.Bankrupt));
        Assert.IsFalse(ClerkDebt.Freezes(EndingConditionType.Fired));
        Assert.IsFalse(ClerkDebt.Freezes(EndingConditionType.DayAtLeast));
        Assert.IsFalse(ClerkDebt.Freezes(EndingConditionType.AttrTotalAtLeast));
    }

    [TestCase(125375, 420, 299, Description = "298.5 days rounded up")]
    [TestCase(420, 420, 1)]
    [TestCase(1, 420, 1)]
    [TestCase(0, 420, 0, Description = "nothing owed, no term")]
    [TestCase(1000, 0, 0, Description = "no wage, no term")]
    public void TermDays_IsTheDebtWorkedOffAtTheDayWage(int owed, int wage, int expected)
    {
        Assert.AreEqual(expected, ClerkDebt.TermDays(owed, wage));
    }

    [Test]
    public void ContractRows_AreTheLabourContractsSixRows()
    {
        List<AccountRow> rows = ClerkDebt.ContractRows(Clerk(), ClerkDebt.State(Clerk(), 55, true), Text, Cr);

        CollectionAssert.AreEqual(
            new[] { "contract.row.worker", "contract.row.citizenId", "contract.row.employer", "contract.row.worksite", "contract.row.term", "contract.row.wage" },
            rows.Select(r => r.Label));
        CollectionAssert.AreEqual(
            new[] { "Theo Marlow", "773-2840-19", "Tyburn Mills Consortium", "Victorian Britain", "299 days", "420 cr" },
            rows.Select(r => r.Value));
        Assert.IsTrue(rows.All(r => r.Group == string.Empty), "one block, no groups");
    }

    [Test]
    public void ContractRows_WithoutAKnownDebtReadATermOfADash()
    {
        List<AccountRow> rows = ClerkDebt.ContractRows(Clerk(), ClerkDebtState.Unknown, Text, Cr);
        Assert.AreEqual("–", rows.Single(r => r.Label == "contract.row.term").Value);
        CollectionAssert.IsEmpty(ClerkDebt.ContractRows(null, ClerkDebtState.Unknown, Text, Cr));
    }

    [Test]
    public void ClerkContentProblems_CheckTheDebtAndTheContract()
    {
        CollectionAssert.IsEmpty(Clerk().Problems());
        CollectionAssert.IsEmpty(Clerk(startDebt: 0, share: 0f).Problems(), "a clerk may owe nothing and pay nothing");

        ClerkContent bad = Clerk(startDebt: -1, share: 1.2f);
        bad.reliefEmployer = " ";
        bad.reliefWorksite = string.Empty;
        bad.reliefWage = 0;
        List<string> problems = bad.Problems();

        Assert.AreEqual(5, problems.Count, string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("startDebt")));
        Assert.IsTrue(problems.Any(p => p.Contains("garnishShare")));
        Assert.IsTrue(problems.Any(p => p.Contains("reliefEmployer")));
        Assert.IsTrue(problems.Any(p => p.Contains("reliefWorksite")));
        Assert.IsTrue(problems.Any(p => p.Contains("reliefWage")));
    }
}
