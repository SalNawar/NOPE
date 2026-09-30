using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The shift report's money at a glance (Papers Please lesson 5, "the shift
/// report makes the speed-vs-accuracy money trade-off obvious"; wave 5 track
/// C): how many travellers the shift processed out of its queue, the right
/// calls times their pay, the wrong calls (the free warnings apart) times
/// their fine, the stranding fines, the Debt Relief instalment and any other
/// money the shift's dialogs moved (a bribe), the net, the bills due tonight
/// and the wallet before, now and after the bills. Every number the player
/// needs is here; none is hidden in a total. Built from the shift's ledger
/// by <see cref="From"/>; pure, so the arithmetic is tested headless.
/// </summary>
public sealed class ShiftReport
{
    /// <summary>The travellers the day queued (the plan's queue as generated).</summary>
    public int Queued;

    /// <summary>The travellers the shift decided.</summary>
    public int Processed;

    /// <summary>The travellers still waiting when the shift closed (the queue less the processed; never below 0): the speed side of the trade-off.</summary>
    public int Waiting => Math.Max(0, Queued - Processed);

    /// <summary>The right calls (paid).</summary>
    public int Right;

    /// <summary>The wrong calls that were the day's free warnings (GameConfigSO.freeWarningsPerDay): no fine.</summary>
    public int Warned;

    /// <summary>The wrong calls fined.</summary>
    public int Fined;

    /// <summary>The wrong calls: warned and fined.</summary>
    public int Wrong => Warned + Fined;

    /// <summary>The pay the right calls earned.</summary>
    public int Pay;

    /// <summary>What every right call paid when they all paid the same; 0 when they differ (a premade's bonus) or none was right.</summary>
    public int PayRate;

    /// <summary>The wrong-decision penalties.</summary>
    public int Penalties;

    /// <summary>What every fined wrong call cost when they all cost the same; 0 when they differ or none was fined.</summary>
    public int PenaltyRate;

    /// <summary>The accepted travellers stranded at the shift's end.</summary>
    public int Stranded;

    /// <summary>The stranding fines (no valid signed waiver on file).</summary>
    public int StrandingFines;

    /// <summary>The Debt Relief instalment taken from the pay.</summary>
    public int Instalment;

    /// <summary>The clerk's debt still owed after the instalment (Account.Unknown when no source gives it: no instalment row).</summary>
    public int DebtOwed = Account.Unknown;

    /// <summary>Other money the shift moved (the dialogs' effects at its end: a bribe taken), signed.</summary>
    public int Other;

    /// <summary>Tonight's bills (rent and utilities, the family, their care, the house's upkeep), known at the shift's end; a break-in is never foretold.</summary>
    public int Bills;

    /// <summary>The wallet as the report shows it (after the shift, before the bills).</summary>
    public int WalletNow;

    /// <summary>The shift's net: the pay less the penalties, the stranding fines and the instalment, plus the other money.</summary>
    public int Net => Pay - Penalties - StrandingFines - Instalment + Other;

    /// <summary>The wallet when the shift began (now less the net).</summary>
    public int WalletBefore => WalletNow - Net;

    /// <summary>The wallet once tonight's bills are paid.</summary>
    public int WalletAfterBills => WalletNow - Bills;

    /// <summary>
    /// The report of <paramref name="ledger"/>'s shift (null: an empty one):
    /// <paramref name="queued"/> travellers queued, the wallet
    /// <paramref name="walletNow"/> after the shift and
    /// <paramref name="bills"/> due tonight. A wrong call is warned when it
    /// was a free warning (CaseVerdict.wasFreeWarning), else fined; a rate is the one amount
    /// every paid (or fined) verdict shares, 0 when they differ.
    /// </summary>
    public static ShiftReport From(ShiftLedger ledger, int queued, int walletNow, int bills)
    {
        IReadOnlyList<CaseVerdict> verdicts = ledger != null ? ledger.verdicts : new List<CaseVerdict>();
        List<CaseVerdict> right = verdicts.Where(v => v != null && v.correct).ToList();
        List<CaseVerdict> wrong = verdicts.Where(v => v != null && !v.correct).ToList();
        List<CaseVerdict> fined = wrong.Where(v => !v.wasFreeWarning).ToList();
        return new ShiftReport
        {
            Queued = Math.Max(0, queued),
            Processed = verdicts.Count(v => v != null),
            Right = right.Count,
            Warned = wrong.Count - fined.Count,
            Fined = fined.Count,
            Pay = right.Sum(v => v.payAwarded),
            PayRate = Common(right.Select(v => v.payAwarded)),
            Penalties = fined.Sum(v => v.moneyPenalty),
            PenaltyRate = Common(fined.Select(v => v.moneyPenalty)),
            Stranded = ledger != null ? ledger.strandedCount : 0,
            StrandingFines = ledger != null ? ledger.strandingFines : 0,
            Instalment = ledger != null ? ledger.debtInstalment : 0,
            DebtOwed = ledger != null ? ledger.debtOwed : Account.Unknown,
            Other = ledger != null ? ledger.otherMoney : 0,
            Bills = Math.Max(0, bills),
            WalletNow = walletNow
        };
    }

    /// <summary>The one value every amount shares; 0 for none or when two differ.</summary>
    private static int Common(IEnumerable<int> amounts)
    {
        int? first = null;
        foreach (int a in amounts)
        {
            if (first == null)
                first = a;
            else if (a != first.Value)
                return 0;
        }
        return first ?? 0;
    }
}
