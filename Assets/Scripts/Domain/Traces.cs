using System;
using System.Collections.Generic;

/// <summary>
/// The last traveller whose verdict pulled one world outcome
/// (WorldState.pullTraces; wave 5, Papers, Please lesson 10): the morning
/// paper's world-outcome headline names them and the day, so the change
/// traces back to a face. An old save loads none.
/// </summary>
[Serializable]
public sealed class PullTrace
{
    /// <summary>The factor's id.</summary>
    public string factor = string.Empty;

    /// <summary>The outcome's id.</summary>
    public string outcome = string.Empty;

    /// <summary>The traveller's name as the paper prints it.</summary>
    public string traveller = string.Empty;

    /// <summary>The day of the verdict.</summary>
    public int day;
}

/// <summary>
/// The morning paper's traces (wave 5, lesson 10): each headline about a
/// change travellers caused names the traveller the clerk stamped and the
/// day ({name}, {day}): a carry (Carries.Line), a panic (History.PanicLines)
/// and a world outcome (the last traveller whose verdict pulled it). Pure.
/// </summary>
public static class Traces
{
    /// <summary>The token of the stamp's day ({name} is Interview.NameToken).</summary>
    public const string DayToken = "day";

    /// <summary>
    /// Records <paramref name="traveller"/>'s verdict on <paramref name="day"/>
    /// as the last to pull each outcome of <paramref name="pulls"/> with an
    /// amount above 0 (one entry per outcome, replaced by each later verdict).
    /// Nothing for a null list or a blank name. Returns how many outcomes it
    /// traced.
    /// </summary>
    public static int Record(List<PullTrace> traces, IEnumerable<OutcomePull> pulls, string traveller, int day)
    {
        if (traces == null || pulls == null || string.IsNullOrWhiteSpace(traveller))
            return 0;
        int traced = 0;
        foreach (OutcomePull p in pulls)
        {
            if (p == null || string.IsNullOrWhiteSpace(p.factor) || string.IsNullOrWhiteSpace(p.outcome) || !(p.amount > 0f))
                continue;
            PullTrace t = traces.Find(x => x != null && x.factor == p.factor && x.outcome == p.outcome);
            if (t == null)
                traces.Add(t = new PullTrace { factor = p.factor, outcome = p.outcome });
            t.traveller = traveller;
            t.day = day;
            traced++;
        }
        return traced;
    }

    /// <summary>
    /// The trace of a changed answer: its outcome's, or for a split the later
    /// of its two outcomes' (the first on the same day); null when none was
    /// traced.
    /// </summary>
    public static PullTrace Of(IReadOnlyList<PullTrace> traces, FactorLead lead)
    {
        if (traces == null || lead == null || string.IsNullOrEmpty(lead.outcome))
            return null;
        PullTrace a = Find(traces, lead.factor, lead.outcome);
        PullTrace b = lead.IsSplit ? Find(traces, lead.factor, lead.split) : null;
        return b != null && (a == null || b.day > a.day) ? b : a;
    }

    /// <summary><paramref name="template"/> with {name} and {day} filled; null for a blank template or a blank name.</summary>
    public static string Fill(string template, string name, int day) =>
        string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(name) ? null
        : Interview.Fill(Interview.Fill(template, Interview.NameToken, name), DayToken, Returns.Day(day));

    /// <summary>
    /// A world-outcome headline traced to its face: <paramref name="headline"/>,
    /// a space and <paramref name="template"/> filled with the trace's name and
    /// day; the headline alone when there is no trace or no template.
    /// </summary>
    public static string Traced(string headline, string template, PullTrace trace)
    {
        string tail = trace != null ? Fill(template, trace.traveller, trace.day) : null;
        return string.IsNullOrWhiteSpace(headline) || tail == null ? headline : headline + " " + tail;
    }

    private static PullTrace Find(IReadOnlyList<PullTrace> traces, string factor, string outcome)
    {
        foreach (PullTrace t in traces)
            if (t != null && t.factor == factor && t.outcome == outcome)
                return t;
        return null;
    }
}
