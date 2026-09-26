using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// The clerk's own debt (the traveller-types spec's D1-D3; redesign phase 13),
/// pure so it is tested headless: the share of each shift's pay garnished for
/// Debt Relief at the shift's end, the account's debt (agency.clerk's starting
/// debt less WorldState.clerkDebtPaid), the ending that freezes the account
/// (the bankrupt one, its condition unchanged) and the clerk's own Debt Relief
/// Labour Contract (TC-520) that ending's screen shows. "Trapped with massive
/// debt": the number barely moves, which is the point.
/// </summary>
public static class ClerkDebt
{
    /// <summary>A share of pay (agency.clerk.garnishShare, 0.25) in whole percent (25), rounded to the nearest and held within 0 to 100.</summary>
    public static int SharePercent(float share) =>
        Math.Max(0, Math.Min(100, (int)Math.Round(share * 100.0, MidpointRounding.AwayFromZero)));

    /// <summary>
    /// The shift's Debt Relief instalment (D2): <paramref name="sharePercent"/>
    /// of the shift's pay, rounded down, never more than what is still
    /// <paramref name="owed"/> and never below 0, so a paid-off debt stops the
    /// instalments.
    /// </summary>
    public static int Instalment(int pay, int sharePercent, int owed)
    {
        if (pay <= 0 || sharePercent <= 0 || owed <= 0)
            return 0;
        long share = (long)pay * Math.Min(100, sharePercent) / 100;
        return (int)Math.Min(share, owed);
    }

    /// <summary>
    /// The clerk's debt as authored and paid: <paramref name="clerk"/>'s
    /// starting debt (never below 0) less what the instalments have
    /// <paramref name="paid"/>, the share in percent, and whether the account is
    /// frozen; Unknown without a profile.
    /// </summary>
    public static ClerkDebtState State(ClerkContent clerk, int paid, bool frozen) =>
        clerk == null
            ? ClerkDebtState.Unknown
            : ClerkDebtState.Of(Math.Max(0, clerk.startDebt), paid, SharePercent(clerk.garnishShare), frozen);

    /// <summary>
    /// True for the ending that freezes the clerk's account and books their
    /// own Debt Relief departure (D3): the bankrupt ending, reached on its
    /// unchanged condition (the wallet at or below the bankruptcy line).
    /// </summary>
    public static bool Freezes(EndingConditionType ending) => ending == EndingConditionType.Bankrupt;

    /// <summary>The Debt Relief contract's term: the days the debt still owed takes at the day wage, rounded up; 0 when nothing is owed or no wage is set.</summary>
    public static int TermDays(int owed, int dayWage)
    {
        if (owed <= 0 || dayWage <= 0)
            return 0;
        return (int)(((long)owed + dayWage - 1) / dayWage);
    }

    /// <summary>
    /// The clerk's own Labour Contract (TC-520, the spec's §3.7 rows) for the
    /// Debt Relief ending: Worker, Citizen ID, Employer, Worksite, Term (the
    /// debt owed worked off at the day wage, "–" when the debt is unknown) and
    /// Day Wage, worded through <paramref name="text"/> (a UI key; "contract.term"
    /// takes the days) and <paramref name="amount"/> (credits). One block with
    /// no groups and no pick keys: the clerk is nobody's case. Empty without a
    /// profile.
    /// </summary>
    public static List<AccountRow> ContractRows(ClerkContent clerk, ClerkDebtState debt, Func<string, string> text, Func<int, string> amount)
    {
        var rows = new List<AccountRow>();
        if (clerk == null || text == null || amount == null)
            return rows;

        string term = debt.Known
            ? string.Format(CultureInfo.InvariantCulture, text("contract.term"), TermDays(debt.Owed, clerk.reliefWage))
            : text("account.unknown");
        rows.Add(new AccountRow(string.Empty, text("contract.row.worker"), Or(clerk.name, text)));
        rows.Add(new AccountRow(string.Empty, text("contract.row.citizenId"), Or(clerk.citizenId, text)));
        rows.Add(new AccountRow(string.Empty, text("contract.row.employer"), Or(clerk.reliefEmployer, text)));
        rows.Add(new AccountRow(string.Empty, text("contract.row.worksite"), Or(clerk.reliefWorksite, text)));
        rows.Add(new AccountRow(string.Empty, text("contract.row.term"), term));
        rows.Add(new AccountRow(string.Empty, text("contract.row.wage"), amount(clerk.reliefWage)));
        return rows;
    }

    /// <summary>An authored value, or "–" when blank.</summary>
    private static string Or(string value, Func<string, string> text) =>
        string.IsNullOrWhiteSpace(value) ? text("account.unknown") : value;
}
