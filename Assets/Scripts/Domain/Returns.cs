using System;
using System.Collections.Generic;

/// <summary>
/// What a returning traveller comes back with (wave 5, Papers, Please lesson
/// 9). Serialized in WorldState.returns: append only (SerializedEnumsTests
/// pins every value).
/// </summary>
public enum ReturnStory
{
    /// <summary>Corrected papers: the same claim, honest this time (no lie, no fault, no costume error rolled).</summary>
    Corrected,

    /// <summary>A new story: the same claim, every roll made again (they may lie again, or differently).</summary>
    NewStory
}

/// <summary>
/// A generated traveller the clerk denied who may come back on a later day
/// (WorldState.returns; wave 5, lesson 9): who they are (name, kind, role,
/// claimed place, gender, birth date, personality, Citizen ID and the first
/// visit's case seed, which brings back their face and their account's
/// draws), when they may come back, with what, and how the second visit went
/// (for the paper's desk section). An old save loads none.
/// </summary>
[Serializable]
public sealed class ReturningTraveller
{
    /// <summary>Their registered given name (CaseInstance.visitorGivenName): unique within their return day.</summary>
    public string name = string.Empty;

    /// <summary>Their name as the desk and the paper show it (CaseInstance.visitorDisplayName: the name and the role).</summary>
    public string displayName = string.Empty;

    /// <summary>Their kind.</summary>
    public TravellerKind kind;

    /// <summary>Their role's archetype id (blank: drawn again).</summary>
    public string archetypeId = string.Empty;

    /// <summary>The claimed place's nation id.</summary>
    public string nationId = string.Empty;

    /// <summary>The claimed place's era id.</summary>
    public string eraId = string.Empty;

    /// <summary>Their gender.</summary>
    public TravellerGender gender;

    /// <summary>Their registered date of birth.</summary>
    public string birthDate = string.Empty;

    /// <summary>Their personality's id (blank: none).</summary>
    public string personality = string.Empty;

    /// <summary>A 2150 citizen's Citizen ID (blank for the displaced): the same account number on their return.</summary>
    public string citizenId = string.Empty;

    /// <summary>Their first visit's case seed (Seeds.ForCase): their return reseeds their look and account streams with it, so the same face comes back.</summary>
    public int caseSeed;

    /// <summary>The day the clerk denied them.</summary>
    public int deniedDay;

    /// <summary>The first day they may come back.</summary>
    public int fromDay;

    /// <summary>The last day they may come back (they give up after it).</summary>
    public int untilDay;

    /// <summary>What they come back with.</summary>
    public ReturnStory story;

    /// <summary>True once they stood at the desk again (DayCycle.Present).</summary>
    public bool returned;

    /// <summary>The day of their second verdict (0: none yet).</summary>
    public int backDay;

    /// <summary>Their second verdict: accepted or denied again.</summary>
    public bool acceptedBack;

    /// <summary>True once the paper's desk section told their second visit.</summary>
    public bool reported;
}

/// <summary>
/// The recurring faces' rules (wave 5, lesson 9), pure and seeded: whether
/// and when a denied traveller comes back, who comes back on a day, into
/// which slots, and the paper's line about their second visit.
/// </summary>
public static class Returns
{
    /// <summary>The token of the day they were first denied.</summary>
    public const string DeniedDayToken = "day";

    /// <summary>The token of the day they came back.</summary>
    public const string BackDayToken = "back";

    /// <summary>
    /// A denied traveller's return: none when they may not come back
    /// (<paramref name="mayReturn"/>: a premade, whose returns are the day
    /// plans', or a traveller already on their second visit: one return
    /// each). Otherwise one draw on their own return stream
    /// (<paramref name="returnSeed"/>, Seeds.ForReturn) against
    /// <paramref name="chance"/>, then one against
    /// <paramref name="correctedChance"/> for the story (Corrected below it,
    /// NewStory otherwise); the window runs from
    /// <paramref name="deniedDay"/> + <paramref name="minDays"/> (at least 1)
    /// to <paramref name="deniedDay"/> + <paramref name="maxDays"/> (at least
    /// the first day). Null when they do not come back.
    /// </summary>
    public static ReturningTraveller Plan(bool mayReturn, float chance, int minDays, int maxDays, float correctedChance, int deniedDay, int returnSeed)
    {
        if (!mayReturn)
            return null;
        var rng = new SeededRandom(returnSeed);
        if (!(rng.Value() < chance))
            return null;
        ReturnStory story = rng.Value() < correctedChance ? ReturnStory.Corrected : ReturnStory.NewStory;
        int from = deniedDay + Math.Max(1, minDays);
        return new ReturningTraveller
        {
            deniedDay = deniedDay,
            fromDay = from,
            untilDay = Math.Max(from, deniedDay + maxDays),
            story = story
        };
    }

    /// <summary>
    /// Who comes back on <paramref name="day"/>: the records not yet returned
    /// whose window holds the day and who fit it (<paramref name="fits"/>: the
    /// day plan takes their kind, their place is in today's world and open to
    /// them, their name and number are free), in record order (the earliest
    /// denied first), at most <paramref name="max"/> (0 or less: none).
    /// </summary>
    public static List<ReturningTraveller> Due(IReadOnlyList<ReturningTraveller> records, int day, Func<ReturningTraveller, bool> fits, int max)
    {
        var due = new List<ReturningTraveller>();
        if (records == null || max <= 0)
            return due;
        foreach (ReturningTraveller r in records)
        {
            if (due.Count >= max)
                break;
            if (r != null && !r.returned && r.fromDay <= day && day <= r.untilDay && (fits == null || fits(r)))
                due.Add(r);
        }
        return due;
    }

    /// <summary>
    /// The slots (1 to <paramref name="total"/>) <paramref name="count"/>
    /// returning travellers stand in: each one Range over the slots still free
    /// (<paramref name="taken"/>: the standing appearances' and the planned
    /// faulty travellers'), in order, on the day's return stream
    /// (Seeds.ForReturnSlots); fewer when fewer are free.
    /// </summary>
    public static List<int> Slots(int total, ICollection<int> taken, int count, IRandomSource rng)
    {
        var free = new List<int>();
        for (int slot = 1; slot <= total; slot++)
            if (taken == null || !taken.Contains(slot))
                free.Add(slot);
        var slots = new List<int>();
        while (slots.Count < count && free.Count > 0)
        {
            int i = rng.Range(0, free.Count);
            slots.Add(free[i]);
            free.RemoveAt(i);
        }
        return slots;
    }

    /// <summary>
    /// The paper's desk-section lines about the second visits verdicted on
    /// <paramref name="day"/> and not yet told, in record order: the accepted
    /// template for a traveller let through, the denied one for a traveller
    /// turned away again ({name} their display name, {day} the first denial, {back} the return);
    /// each told is marked reported. None for a blank template.
    /// </summary>
    public static List<string> Lines(IReadOnlyList<ReturningTraveller> records, int day, string accepted, string denied)
    {
        var lines = new List<string>();
        foreach (ReturningTraveller r in records ?? Array.Empty<ReturningTraveller>())
        {
            if (r == null || r.reported || r.backDay <= 0 || r.backDay != day)
                continue;
            string template = r.acceptedBack ? accepted : denied;
            if (string.IsNullOrWhiteSpace(template))
                continue;
            string name = string.IsNullOrWhiteSpace(r.displayName) ? r.name : r.displayName;
            lines.Add(Interview.Fill(Interview.Fill(Interview.Fill(template, Interview.NameToken, name), DeniedDayToken, Day(r.deniedDay)), BackDayToken, Day(r.backDay)));
            r.reported = true;
        }
        return lines;
    }

    /// <summary>A day number as the papers print it (invariant digits).</summary>
    public static string Day(int day) => day.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
