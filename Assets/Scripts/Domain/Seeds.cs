/// <summary>
/// Seed derivation for a run. <see cref="Day"/> is the original RunManager
/// formula (unchanged, so existing saves keep their schedules); every other
/// stream is salted through <see cref="Mix"/> so streams never replay each other.
/// </summary>
public static class Seeds
{
    /// <summary>Salt for per-traveller case-generation streams ("CASE").</summary>
    public const int CaseSalt = 0x43415345;

    /// <summary>Salt for the day's guaranteed rule-violator stream ("VIOL").</summary>
    public const int ViolatorSalt = 0x56494F4C;

    /// <summary>
    /// Salt of the retired legacy clue stream ("CLUE"; audit R3-012 removed
    /// the clue path). Kept so the salt is never reused: SeedsTests holds every
    /// salt distinct.
    /// </summary>
    public const int ClueSalt = 0x434C5545;

    /// <summary>Salt for a traveller's lie stream ("LIES").</summary>
    public const int LieSalt = 0x4C494553;

    /// <summary>Salt for a traveller's dialog stream ("DIAG").</summary>
    public const int DialogSalt = 0x44494147;

    /// <summary>Salt for a traveller's look stream ("LOOK").</summary>
    public const int LookSalt = 0x4C4F4F4B;

    /// <summary>Salt for a slot's premade roll ("LGND").</summary>
    public const int LegendarySalt = 0x4C474E44;

    /// <summary>The day's seed: same run + same day = same seed.</summary>
    public static int Day(int runSeed, int day)
    {
        unchecked
        {
            return runSeed * 397 ^ day * 7919;
        }
    }

    /// <summary>Avalanche-mixes a seed with a salt (SplitMix64 finaliser).</summary>
    public static int Mix(int seed, int salt)
    {
        unchecked
        {
            ulong z = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + (ulong)(uint)salt;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (int)z;
        }
    }

    /// <summary>Seed for one traveller's generation stream (independent of the other slots).</summary>
    public static int ForCase(int daySeed, int caseIndex1Based) => Mix(Mix(daySeed, CaseSalt), caseIndex1Based);

    /// <summary>Seed for the day's rule-violator placement (which slots, which places).</summary>
    public static int ForViolators(int daySeed) => Mix(daySeed, ViolatorSalt);

    /// <summary>
    /// Seed for one traveller's lie stream: the liar roll, the true-home pick
    /// and the tell picks. Kept apart from the case stream so lie tuning never
    /// changes who travellers are.
    /// </summary>
    public static int ForLies(int caseSeed) => Mix(caseSeed, LieSalt);

    /// <summary>
    /// Seed for one traveller's dialog variant picks (small talk), apart from
    /// the case and lie streams so content never changes who travellers are
    /// or who lies.
    /// </summary>
    public static int ForDialog(int caseSeed) => Mix(caseSeed, DialogSalt);

    /// <summary>
    /// Seed for one traveller's look draws (gender when unknown, skin, face,
    /// hair colour), apart from the case and lie streams so look tuning never
    /// changes who travellers are or who lies.
    /// </summary>
    public static int ForLooks(int caseSeed) => Mix(caseSeed, LookSalt);

    /// <summary>
    /// Seed for one slot's premade roll and pick, apart from the case stream so
    /// authoring a premade never reshuffles a day's travellers.
    /// </summary>
    public static int ForLegendary(int caseSeed) => Mix(caseSeed, LegendarySalt);

    /// <summary>Salt for a traveller's forms seed ("FORM").</summary>
    public const int FormsSalt = 0x464F524D;

    /// <summary>
    /// One traveller's forms seed (redesign phase 4, PC spec FO5): the value
    /// their papers' serials come from (FormSerials). A value, never a stream
    /// draw, so printing a serial moves no other draw.
    /// </summary>
    public static int ForForms(int caseSeed) => Mix(caseSeed, FormsSalt);

    /// <summary>Salt for the night's slot-machine spins at Home ("SLOT").</summary>
    public const int SlotSalt = 0x534C4F54;

    /// <summary>
    /// Seed for the night's slot-machine spins at Home, drawn in turn (audit
    /// R2-004: the spins were unseeded), so a run replays and Continue, which
    /// reloads Home from the save made before it, cannot reroll a spin; apart
    /// from the day's raw stream the family conditions draw from.
    /// </summary>
    public static int ForSlot(int daySeed) => Mix(daySeed, SlotSalt);

    /// <summary>Salt for a traveller's account stream ("ACCT").</summary>
    public const int AccountSalt = 0x41434354;

    /// <summary>
    /// Seed for one traveller's agency numbers and dates (traveller types,
    /// R2 and §4.3; AgencyNumbers): for the displaced, their Displacement
    /// No., incident, found date and the certificate's Valid Until. Apart from
    /// the case and lie streams, so tuning an amount or a range never changes
    /// who travels or who lies.
    /// </summary>
    public static int ForAccount(int caseSeed) => Mix(caseSeed, AccountSalt);

    /// <summary>Salt for the run's debt-news order ("DEBT").</summary>
    public const int DebtNewsSalt = 0x44454254;

    /// <summary>
    /// Seed for the order in which the morning paper runs through the debt
    /// lines (redesign phase 13; DebtNews.Line): one stream for the whole run,
    /// apart from every day's and traveller's, so the paper never shifts who
    /// travels.
    /// </summary>
    public static int ForDebtNews(int runSeed) => Mix(runSeed, DebtNewsSalt);
}
