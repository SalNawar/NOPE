using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// One day of the clerk's statement (the PC spec's AC1, form TC-960): the
/// shift's wages and fines, the Debt Relief instalment, Home's household
/// costs and purchases, the wallet after, and the debt still owed. Written
/// at the shift's end and at Home into WorldState.accountDays (additive; an
/// old save loads none). <see cref="Account.Unknown"/> marks a value no
/// source gives yet (the instalment and the debt until phase 13).
/// </summary>
[Serializable]
public sealed class AccountDay
{
    /// <summary>The shift day.</summary>
    public int day;

    /// <summary>The shift's pay (ShiftLedger.TotalPay).</summary>
    public int wages;

    /// <summary>The shift's citation penalties (ShiftLedger.TotalPenalties; phase 13 adds stranding fines).</summary>
    public int fines;

    /// <summary>The Debt Relief instalment taken from the pay, or Account.Unknown.</summary>
    public int debtRelief = Account.Unknown;

    /// <summary>Home's household costs: the day's living expenses and any care paid.</summary>
    public int household;

    /// <summary>Home's purchases: upgrades bought and slot spins paid.</summary>
    public int purchases;

    /// <summary>The wallet after the day's last entry.</summary>
    public int balance;

    /// <summary>The debt still owed after the day, or Account.Unknown.</summary>
    public int owed = Account.Unknown;
}

/// <summary>
/// The clerk's own account rows as authored (world_source.json
/// "agency.clerk"; the traveller-types spec's D1 and §4.4). The name, birth
/// date and lineage are Saleh's to author. Phase 13 adds the starting debt
/// and the instalment share to this block.
/// </summary>
[Serializable]
public sealed class ClerkContent
{
    /// <summary>The clerk's Citizen ID ("773-2840-19").</summary>
    public string citizenId = string.Empty;

    /// <summary>The clerk's name.</summary>
    public string name = string.Empty;

    /// <summary>The clerk's birth date, written as BirthDates writes dates.</summary>
    public string born = string.Empty;

    /// <summary>The clerk's lineage (a past place, flavour).</summary>
    public string lineage = string.Empty;

    /// <summary>The employment row ("Temporal Customs · Desk 3").</summary>
    public string employment = string.Empty;

    /// <summary>The record's note ("No remarks on file.").</summary>
    public string note = string.Empty;

    /// <summary>What Generate World and the validator refuse: a blank Citizen ID, name or employment. Empty when sound.</summary>
    public List<string> Problems()
    {
        var problems = new List<string>();
        if (string.IsNullOrWhiteSpace(citizenId))
            problems.Add("agency.clerk.citizenId is blank: the clerk's own Citizen ID.");
        if (string.IsNullOrWhiteSpace(name))
            problems.Add("agency.clerk.name is blank: the clerk's name on the Citizen Account.");
        if (string.IsNullOrWhiteSpace(employment))
            problems.Add("agency.clerk.employment is blank: the clerk's employment row.");
        return problems;
    }
}

/// <summary>
/// What the clerk's account knows about the debt (the traveller-types spec's
/// D1-D3). Phase 13 supplies it (agency.clerk.startDebt, the garnish share and
/// WorldState.clerkDebtPaid); until then it is <see cref="Unknown"/>, and the
/// Citizen Account shows "–" for the debt and the instalments.
/// </summary>
public readonly struct ClerkDebtState
{
    /// <summary>True when a source gives the debt.</summary>
    public readonly bool Known;

    /// <summary>The debt the clerk started with.</summary>
    public readonly int StartDebt;

    /// <summary>What the instalments have paid so far.</summary>
    public readonly int Paid;

    /// <summary>The share of each shift's pay that goes to the debt, in percent.</summary>
    public readonly int SharePercent;

    /// <summary>True once the bankrupt ending froze the account (the clerk's own Debt Relief departure is booked).</summary>
    public readonly bool Frozen;

    private ClerkDebtState(bool known, int startDebt, int paid, int sharePercent, bool frozen)
    {
        Known = known;
        StartDebt = startDebt;
        Paid = paid;
        SharePercent = sharePercent;
        Frozen = frozen;
    }

    /// <summary>No source gives the debt yet.</summary>
    public static ClerkDebtState Unknown => default;

    /// <summary>A known debt: <paramref name="startDebt"/> less <paramref name="paid"/>.</summary>
    public static ClerkDebtState Of(int startDebt, int paid, int sharePercent, bool frozen) =>
        new ClerkDebtState(true, startDebt, paid, sharePercent, frozen);

    /// <summary>The debt still owed (never below 0), or Account.Unknown.</summary>
    public int Owed => Known ? Math.Max(0, StartDebt - Math.Max(0, Paid)) : Account.Unknown;

    /// <summary>True when a known debt is paid off (the status then reads Standard).</summary>
    public bool PaidOff => Known && Owed == 0;
}

/// <summary>
/// Where the Citizen Account app and the statement read the clerk (the PC
/// spec's AC1). Phase 25 feeds it from the authored profile and the wallet;
/// phase 13 fills <see cref="Debt"/> and <see cref="ShiftInstalment"/>.
/// </summary>
public interface IClerkAccountSource
{
    /// <summary>The authored rows (agency.clerk).</summary>
    ClerkContent Profile { get; }

    /// <summary>The wallet now (WorldState.money).</summary>
    int Balance { get; }

    /// <summary>The debt (ClerkDebtState.Unknown until phase 13).</summary>
    ClerkDebtState Debt { get; }

    /// <summary>The Debt Relief instalment taken from this shift's pay (Account.Unknown until phase 13).</summary>
    int ShiftInstalment { get; }
}

/// <summary>One row of the clerk's Record Extract (TC-901): its group, label and value, already worded. It carries no pick key: the clerk is nobody's case.</summary>
public readonly struct AccountRow
{
    /// <summary>The group heading (Records, Forms on file, Travel).</summary>
    public readonly string Group;

    /// <summary>The row's label.</summary>
    public readonly string Label;

    /// <summary>The row's value ("–" when no source gives it).</summary>
    public readonly string Value;

    /// <summary>A worded row.</summary>
    public AccountRow(string group, string label, string value)
    {
        Group = group;
        Label = label;
        Value = value;
    }
}

/// <summary>
/// The clerk's statement rules (AC1): one row per day, written at the shift's
/// end (from the ShiftLedger and the source) and at Home (household costs and
/// purchases), the oldest dropped past the cap; and the Record Extract's rows
/// and the statement's cells, worded through the view's lookups. Pure.
/// </summary>
public static class Account
{
    /// <summary>A value no source gives yet; shown as "–".</summary>
    public const int Unknown = -1;

    /// <summary>
    /// Writes the shift's part of <paramref name="day"/>'s row (a replayed
    /// shift replaces it): the ledger's pay and penalties, the source's
    /// instalment, wallet and debt; Home's part starts empty.
    /// </summary>
    public static AccountDay RecordShift(List<AccountDay> days, int day, ShiftLedger ledger, IClerkAccountSource source, int cap)
    {
        AccountDay row = Row(days, day, cap);
        if (row == null)
            return null;
        row.wages = ledger != null ? ledger.TotalPay : 0;
        row.fines = ledger != null ? ledger.TotalPenalties : 0;
        row.debtRelief = source != null ? source.ShiftInstalment : Unknown;
        row.household = 0;
        row.purchases = 0;
        row.balance = source != null ? source.Balance : 0;
        row.owed = source != null ? source.Debt.Owed : Unknown;
        return row;
    }

    /// <summary>Writes Home's part of <paramref name="day"/>'s row (Home's running totals so far) and the wallet after.</summary>
    public static AccountDay RecordHome(List<AccountDay> days, int day, int household, int purchases, IClerkAccountSource source, int cap)
    {
        AccountDay row = Row(days, day, cap);
        if (row == null)
            return null;
        row.household = household;
        row.purchases = purchases;
        row.balance = source != null ? source.Balance : row.balance;
        if (source != null && source.Debt.Known)
            row.owed = source.Debt.Owed;
        return row;
    }

    /// <summary>The day's row, created in day order when missing; the oldest rows past <paramref name="cap"/> are dropped. Null for a day below 1.</summary>
    private static AccountDay Row(List<AccountDay> days, int day, int cap)
    {
        if (days == null || day < 1)
            return null;

        AccountDay row = days.Find(d => d != null && d.day == day);
        if (row == null)
        {
            row = new AccountDay { day = day };
            int at = days.FindIndex(d => d != null && d.day > day);
            if (at < 0)
                days.Add(row);
            else
                days.Insert(at, row);
        }
        days.RemoveAll(d => d == null);
        while (cap > 0 && days.Count > cap)
            days.RemoveAt(0);
        return row;
    }

    /// <summary>
    /// The clerk's Record Extract rows (the traveller-types spec's §4.4), in
    /// group order: Records (name, born, lineage, Citizen ID, status,
    /// standing, debt, balance), Forms on file (employment and the instalment
    /// rate), Travel (the booked departure), then the note. <paramref name="text"/>
    /// words a UI key, <paramref name="amount"/> an amount of credits.
    /// </summary>
    public static List<AccountRow> ExtractRows(IClerkAccountSource source, Func<string, string> text, Func<int, string> amount)
    {
        var rows = new List<AccountRow>();
        if (source == null || text == null || amount == null)
            return rows;

        ClerkContent p = source.Profile ?? new ClerkContent();
        ClerkDebtState debt = source.Debt;
        string records = text("account.group.records");
        rows.Add(new AccountRow(records, text("account.row.name"), Or(p.name, text)));
        rows.Add(new AccountRow(records, text("account.row.born"), Or(p.born, text)));
        rows.Add(new AccountRow(records, text("account.row.lineage"), Or(p.lineage, text)));
        rows.Add(new AccountRow(records, text("account.row.citizenId"), Or(p.citizenId, text)));
        rows.Add(new AccountRow(records, text("account.row.status"), text(debt.PaidOff ? "account.status.standard" : "account.status.eligible")));
        rows.Add(new AccountRow(records, text("account.row.standing"), text(debt.Frozen ? "account.standing.frozen" : "account.standing.good")));
        rows.Add(new AccountRow(records, text("account.row.debt"), Amount(debt.Owed, text, amount)));
        rows.Add(new AccountRow(records, text("account.row.balance"), amount(source.Balance)));

        string forms = text("account.group.forms");
        string employment = Or(p.employment, text);
        if (debt.Known)
            employment += "; " + string.Format(CultureInfo.InvariantCulture, text("account.instalmentRate"), debt.SharePercent);
        rows.Add(new AccountRow(forms, text("account.row.employment"), employment));

        rows.Add(new AccountRow(text("account.group.travel"), text("account.row.departure"),
                                text(debt.Frozen ? "account.departure.booked" : "account.departure.none")));
        rows.Add(new AccountRow(string.Empty, text("account.row.note"), Or(p.note, text)));
        return rows;
    }

    /// <summary>A statement row's cells in column order (DAY, WAGES, FINES, DEBT RELIEF, HOUSEHOLD, PURCHASES, BALANCE, OWED); an unknown value reads "–".</summary>
    public static string[] StatementCells(AccountDay d, Func<string, string> text)
    {
        if (d == null || text == null)
            return Array.Empty<string>();
        return new[]
        {
            d.day.ToString(CultureInfo.InvariantCulture),
            Number(d.wages, text), Number(d.fines, text), Number(d.debtRelief, text),
            Number(d.household, text), Number(d.purchases, text),
            d.balance.ToString("N0", CultureInfo.InvariantCulture),
            Number(d.owed, text)
        };
    }

    /// <summary>A known amount through <paramref name="amount"/>, else "–".</summary>
    private static string Amount(int value, Func<string, string> text, Func<int, string> amount) =>
        value == Unknown ? text("account.unknown") : amount(value);

    /// <summary>A known count with thousands separators, else "–" (statement amounts are never negative except the balance).</summary>
    private static string Number(int value, Func<string, string> text) =>
        value == Unknown ? text("account.unknown") : value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>An authored value, or "–" when blank.</summary>
    private static string Or(string value, Func<string, string> text) =>
        string.IsNullOrWhiteSpace(value) ? text("account.unknown") : value;
}
