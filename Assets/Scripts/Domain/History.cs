using System;
using System.Collections.Generic;

/// <summary>Why a fact changed. Serialized as ints in saves: append only.</summary>
public enum EditCause
{
    /// <summary>An authored history rule fired (its SetFact op).</summary>
    Rule,

    /// <summary>An accepted liar carried their true home's value into the claimed place.</summary>
    Carry
}

/// <summary>One latched change of a place's fact ("from day 5, Florentine Republic's Technology is ...").</summary>
[Serializable]
public sealed class FactEdit
{
    /// <summary>The place's nation id.</summary>
    public string nationId;

    /// <summary>The place's era id.</summary>
    public string eraId;

    /// <summary>The fact that changes (one of History.EditableCategories).</summary>
    public ClueCategory category;

    /// <summary>The new value.</summary>
    public string value;

    /// <summary>First day the edit applies (for the inspector and logs; every edit applies from the next day).</summary>
    public int sinceDay;

    /// <summary>What caused the edit.</summary>
    public EditCause cause;

    /// <summary>The trigger label for a rule, the origin place's label for a carry.</summary>
    public string source;

    /// <summary>An empty edit (for serialization).</summary>
    public FactEdit()
    {
    }

    /// <summary>An edit of a place's fact.</summary>
    public FactEdit(string nationId, string eraId, ClueCategory category, string value, int sinceDay, EditCause cause, string source)
    {
        this.nationId = nationId;
        this.eraId = eraId;
        this.category = category;
        this.value = value;
        this.sinceDay = sinceDay;
        this.cause = cause;
        this.source = source;
    }
}

/// <summary>An accepted liar's carry: recorded at accept, promoted at night (Carries.Promote).</summary>
[Serializable]
public sealed class CarryRecord
{
    /// <summary>The liar's true home: nation id.</summary>
    public string fromNationId;

    /// <summary>The liar's true home: era id.</summary>
    public string fromEraId;

    /// <summary>The claimed place: nation id.</summary>
    public string toNationId;

    /// <summary>The claimed place: era id.</summary>
    public string toEraId;

    /// <summary>The carried fact.</summary>
    public ClueCategory category;

    /// <summary>The true home's value for it on the day of the accept.</summary>
    public string value;

    /// <summary>The day of the accept.</summary>
    public int day;
}

/// <summary>
/// The saved history of the run (WorldState.history): the timeline leader
/// (the present culture), the ranking behind it, the latched fact edits and
/// the pending carries. An empty block is "no history yet" (an old save).
/// </summary>
[Serializable]
public sealed class HistoryState
{
    /// <summary>The leading nation's id (the present culture); "" = none.</summary>
    public string leaderId = string.Empty;

    /// <summary>First day under this leader (0 = none).</summary>
    public int leaderSinceDay;

    /// <summary>Last night's influence ranking, highest first.</summary>
    public List<RankedScore> ranking = new();

    /// <summary>Latched fact edits, oldest first (the newest of a place and category wins).</summary>
    public List<FactEdit> factEdits = new();

    /// <summary>Carries recorded at accept and not yet promoted, in record order.</summary>
    public List<CarryRecord> pendingCarries = new();
}

/// <summary>Wording of the templated history news (content; English text in v1).</summary>
[Serializable]
public sealed class HistoryLines
{
    /// <summary>A nation starts leading; tokens {nation} and {place} (its Future place).</summary>
    public LineText leaderGained = new();

    /// <summary>The leader loses the lead with no successor; token {nation}.</summary>
    public LineText leaderLost = new();

    /// <summary>A carry latched; tokens {value} and {place}.</summary>
    public LineText carry = new();
}

/// <summary>
/// The rules of history, pure so they are tested headless: which categories
/// history may edit, which Future place is in the world, how a place's fact
/// resolves from the latched edits, what may be latched, and the news cap.
/// </summary>
public static class History
{
    /// <summary>The categories history may change (the five book categories; origin labels and Culture never change).</summary>
    public static readonly ClueCategory[] EditableCategories =
        { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Politics };

    /// <summary>The nation's display name in a history line ("{nation}"; {place} and {value} are Interview.PlaceToken and ValueToken).</summary>
    public const string NationToken = "nation";

    /// <summary>True for the categories history may change.</summary>
    public static bool IsEditable(ClueCategory category) => Array.IndexOf(EditableCategories, category) >= 0;

    /// <summary>Whose Future place is in the world: the leader's; none (null) without a leader.</summary>
    public static string FutureNation(HistoryState history) =>
        history != null && !string.IsNullOrWhiteSpace(history.leaderId) ? history.leaderId : null;

    /// <summary>
    /// Whether a place the day plan allows is in today's world: every place
    /// outside the Future; a Future place only for <paramref name="futureNationId"/>
    /// (at most one Future place, the leader's; none for null).
    /// </summary>
    public static bool InWorld(bool isFutureEra, string nationId, string futureNationId) =>
        !isFutureEra || (!string.IsNullOrEmpty(futureNationId) && string.Equals(nationId, futureNationId, StringComparison.Ordinal));

    /// <summary>
    /// A place's fact with history applied: the newest latched edit of that
    /// place and category with a non-blank value, else <paramref name="baseValue"/>
    /// (also for a null state). Every edit applies from the day after it
    /// latched, and facts are only built for that day or later, so no day filter.
    /// </summary>
    public static string Resolve(HistoryState history, string nationId, string eraId, ClueCategory category, string baseValue)
    {
        if (history == null || history.factEdits == null)
            return baseValue;

        for (int i = history.factEdits.Count - 1; i >= 0; i--)
        {
            FactEdit e = history.factEdits[i];
            if (e != null && e.category == category && !string.IsNullOrWhiteSpace(e.value) &&
                string.Equals(e.nationId, nationId, StringComparison.Ordinal) && string.Equals(e.eraId, eraId, StringComparison.Ordinal))
                return e.value;
        }

        return baseValue;
    }

    /// <summary>
    /// True when history gives this place's category a value that differs from
    /// its authored value (DiscrepancyLog.ValuesMatch decides "differs"); the
    /// reference books mark such rows "revised" for as long as it lasts (piece 6 R14).
    /// </summary>
    public static bool IsRevised(HistoryState history, string nationId, string eraId, ClueCategory category, string baseValue) =>
        !DiscrepancyLog.ValuesMatch(Resolve(history, nationId, eraId, category, baseValue), baseValue);

    /// <summary>Appends an edit and returns true, unless the state or edit is null, its value is blank, or its category is not editable.</summary>
    public static bool Latch(HistoryState history, FactEdit edit)
    {
        if (history == null || edit == null || string.IsNullOrWhiteSpace(edit.value) || !IsEditable(edit.category))
            return false;

        history.factEdits ??= new List<FactEdit>();
        history.factEdits.Add(edit);
        return true;
    }

    /// <summary>
    /// The history news cap: how many of <paramref name="count"/> further
    /// templated lines fit tonight after <paramref name="linesSoFar"/> (the
    /// leader line comes first, then carry lines while they fit). Negative
    /// inputs count as 0.
    /// </summary>
    public static int NewsSlots(int linesSoFar, int cap, int count) =>
        Math.Max(0, Math.Min(Math.Max(0, count), Math.Max(0, cap) - Math.Max(0, linesSoFar)));
}
