/// <summary>
/// The clerk's account in the running game (IClerkAccountSource; the PC
/// spec's AC1): the authored rows from the content library's agency.clerk and
/// the wallet from the run. The debt and the instalment are phase 13's: until
/// then <see cref="Debt"/> is ClerkDebtState.Unknown and
/// <see cref="ShiftInstalment"/> is Account.Unknown, and the Citizen Account
/// shows "–" for them. Phase 13 returns ClerkDebtState.Of(agency.clerk's
/// starting debt, WorldState.clerkDebtPaid, the garnish share, the bankrupt
/// ending) and the shift's ClerkDebt.Instalment here, and nothing else changes.
/// Also writes the statement's rows (GameManager at the shift's end, HomeManager at Home).
/// </summary>
public sealed class ClerkAccountSource : IClerkAccountSource
{
    private readonly WorldState _world;
    private readonly ContentLibrarySO _library;

    /// <summary>The clerk's account for a run's state and content.</summary>
    public ClerkAccountSource(WorldState world, ContentLibrarySO library)
    {
        _world = world;
        _library = library;
    }

    /// <summary>The authored rows (agency.clerk; blank without a library).</summary>
    public ClerkContent Profile => _library != null && _library.Agency.clerk != null ? _library.Agency.clerk : new ClerkContent();

    /// <summary>The wallet now.</summary>
    public int Balance => _world != null ? _world.money : 0;

    /// <summary>The debt: unknown until phase 13 supplies it.</summary>
    public ClerkDebtState Debt => ClerkDebtState.Unknown;

    /// <summary>The shift's Debt Relief instalment: unknown until phase 13 supplies it.</summary>
    public int ShiftInstalment => Account.Unknown;

    /// <summary>Writes the day's statement row from the shift's ledger (the shift's end, before the end-of-shift save).</summary>
    public static void RecordShift(WorldState world, ShiftLedger ledger, ContentLibrarySO library, GameConfigSO config)
    {
        if (world != null)
            Account.RecordShift(world.accountDays, world.day, ledger, new ClerkAccountSource(world, library), Kept(config));
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
