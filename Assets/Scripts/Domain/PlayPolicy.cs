/// <summary>How the balance simulation plays a day (redesign phase 23; never serialized).</summary>
public enum PlayStyle
{
    /// <summary>Every call right, every denial documented.</summary>
    Perfect,

    /// <summary>One wrong call a day: on odd days the first faulty traveller is let through, on even days the first denial of a deviation fault is left unproven.</summary>
    Imperfect,

    /// <summary>Both wrong calls every day.</summary>
    Careless
}

/// <summary>One decision of a <see cref="PlayPolicy"/>: accept or deny, and whether a denial is backed by documented evidence.</summary>
public readonly struct PlayDecision
{
    /// <summary>True to accept the traveller.</summary>
    public readonly bool Accept;

    /// <summary>True when the decision is backed by documented evidence (a deviation logged); false leaves a denial unproven.</summary>
    public readonly bool Documented;

    /// <summary>A decision.</summary>
    public PlayDecision(bool accept, bool documented)
    {
        Accept = accept;
        Documented = documented;
    }
}

/// <summary>
/// A player for the balance simulation (Tools > TimeDesk > Balance): decides
/// each traveller from the truth (should they be accepted, is their fault a
/// deviation that needs evidence), making the style's planned mistakes at
/// most once a day each. Pure, so each style is tested headless.
/// </summary>
public sealed class PlayPolicy
{
    private bool _letThroughToday;
    private bool _unprovenToday;
    private bool _letThroughTaken;
    private bool _unprovenTaken;

    /// <summary>A player of <paramref name="style"/>; call <see cref="StartDay"/> before each day's first decision.</summary>
    public PlayPolicy(PlayStyle style)
    {
        Style = style;
    }

    /// <summary>The way this player plays.</summary>
    public PlayStyle Style { get; }

    /// <summary>
    /// True when a player at <paramref name="cap"/> travellers a shift reaches
    /// the queue's <paramref name="slot1Based"/>th traveller before the clock
    /// closes (days 7-15 X3, Saleh's Q11: the simulation's throughput cap,
    /// BalanceSimSettingsSO.travellersPerShift); a cap of 0 or less reaches the
    /// whole queue.
    /// </summary>
    public static bool Reaches(int slot1Based, int cap) => cap <= 0 || slot1Based <= cap;

    /// <summary>Starts <paramref name="day"/>: the day's planned mistakes are not made yet.</summary>
    public void StartDay(int day)
    {
        bool odd = day % 2 != 0;
        _letThroughToday = Style == PlayStyle.Careless || (Style == PlayStyle.Imperfect && odd);
        _unprovenToday = Style == PlayStyle.Careless || (Style == PlayStyle.Imperfect && !odd);
        _letThroughTaken = false;
        _unprovenTaken = false;
    }

    /// <summary>
    /// The decision on a traveller who <paramref name="shouldAccept"/> (or
    /// not) and whose fault is a deviation (<paramref name="hasDeviationFault"/>)
    /// or a directive one: right, except the day's first faulty traveller when
    /// the day lets one through, and the day's first deviation denial left
    /// undocumented when the day leaves one unproven.
    /// </summary>
    public PlayDecision Decide(bool shouldAccept, bool hasDeviationFault)
    {
        if (shouldAccept)
            return new PlayDecision(true, true);

        if (_letThroughToday && !_letThroughTaken)
        {
            _letThroughTaken = true;
            return new PlayDecision(true, true);
        }

        if (_unprovenToday && !_unprovenTaken && hasDeviationFault)
        {
            _unprovenTaken = true;
            return new PlayDecision(false, false);
        }

        return new PlayDecision(false, true);
    }
}
