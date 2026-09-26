/// <summary>
/// The clerk's account in the running game (IClerkAccountSource; the PC
/// spec's AC1): the authored rows from the content library's agency.clerk, the
/// wallet from the run, and the debt (redesign phase 13, ClerkDebt.State:
/// agency.clerk's starting debt less WorldState.clerkDebtPaid, its share,
/// frozen once the run ended on the bankrupt ending). Takes the shift's Debt
/// Relief instalment at the shift's end (<see cref="TakeInstalment"/>) and
/// writes the statement's rows (GameManager at the shift's end, HomeManager at
/// Home). Without a run or a library the debt reads Unknown ("–").
/// </summary>
public sealed class ClerkAccountSource : IClerkAccountSource
{
    private readonly WorldState _world;
    private readonly ContentLibrarySO _library;
    private readonly ShiftLedger _shift;

    /// <summary>The clerk's account for a run's state and content; <paramref name="shift"/> is the ending shift's ledger when the statement's shift row is written.</summary>
    public ClerkAccountSource(WorldState world, ContentLibrarySO library, ShiftLedger shift = null)
    {
        _world = world;
        _library = library;
        _shift = shift;
    }

    /// <summary>The authored rows (agency.clerk; blank without a library).</summary>
    public ClerkContent Profile => _library != null && _library.Agency.clerk != null ? _library.Agency.clerk : new ClerkContent();

    /// <summary>The wallet now.</summary>
    public int Balance => _world != null ? _world.money : 0;

    /// <summary>The debt: agency.clerk's starting debt less what the instalments paid, frozen on the bankrupt ending; Unknown without a run or a library.</summary>
    public ClerkDebtState Debt =>
        _world != null && _library != null ? ClerkDebt.State(Profile, _world.clerkDebtPaid, Frozen) : ClerkDebtState.Unknown;

    /// <summary>The ending shift's Debt Relief instalment (its ledger's), or Account.Unknown outside a shift's end or without a debt.</summary>
    public int ShiftInstalment => _shift != null && Debt.Known ? _shift.debtInstalment : Account.Unknown;

    /// <summary>True once the run ended on an ending that freezes the account (ClerkDebt.Freezes: the bankrupt one).</summary>
    private bool Frozen
    {
        get
        {
            EndingSO ending = !string.IsNullOrEmpty(_world.endingId) ? _library.GetEndingById(_world.endingId) : null;
            return ending != null && ClerkDebt.Freezes(ending.conditionType);
        }
    }

    /// <summary>
    /// The shift's end (before the dialog consequences, the ending check, the
    /// statement row and the end-of-shift save; the save makes Continue resume
    /// at Home, so it is never taken twice): garnishes the Debt Relief
    /// instalment from the shift's pay (ClerkDebt.Instalment), takes it from the
    /// wallet, adds it to WorldState.clerkDebtPaid and writes it and the debt
    /// still owed into the ledger for the shift report. Returns the instalment
    /// (0 without a known debt).
    /// </summary>
    public static int TakeInstalment(WorldState world, ShiftLedger ledger, ContentLibrarySO library)
    {
        if (world == null || ledger == null)
            return 0;

        var source = new ClerkAccountSource(world, library);
        ClerkDebtState debt = source.Debt;
        if (!debt.Known)
            return 0;

        int instalment = ClerkDebt.Instalment(ledger.TotalPay, debt.SharePercent, debt.Owed);
        world.money -= instalment;
        world.clerkDebtPaid += instalment;
        ledger.debtInstalment = instalment;
        ledger.debtOwed = source.Debt.Owed;
        return instalment;
    }

    /// <summary>Writes the day's statement row from the shift's ledger, its instalment included (the shift's end, before the end-of-shift save).</summary>
    public static void RecordShift(WorldState world, ShiftLedger ledger, ContentLibrarySO library, GameConfigSO config)
    {
        if (world != null)
            Account.RecordShift(world.accountDays, world.day, ledger, new ClerkAccountSource(world, library, ledger), Kept(config));
    }

    /// <summary>Writes Home's part of the day's statement row: its household costs and purchases so far, and the wallet after.</summary>
    public static void RecordHome(WorldState world, int household, int purchases, ContentLibrarySO library, GameConfigSO config)
    {
        if (world != null)
            Account.RecordHome(world.accountDays, world.day, household, purchases, new ClerkAccountSource(world, library), Kept(config));
    }

    /// <summary>The statement's day cap (0 keeps every day when no config is wired).</summary>
    private static int Kept(GameConfigSO config) => config != null ? config.accountDaysKept : 0;
}
