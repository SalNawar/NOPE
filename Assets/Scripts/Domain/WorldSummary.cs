using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>One place's fact as history left it: the place, the fact, the value now and before, and its day.</summary>
public readonly struct PlaceChange
{
    /// <summary>A place's changed (or incoming) fact.</summary>
    public PlaceChange(string place, ClueCategory category, string value, string was, int day)
    {
        Place = place;
        Category = category;
        Value = value;
        Was = was;
        Day = day;
    }

    /// <summary>The place's label ("Florentine Republic (Medieval)").</summary>
    public string Place { get; }

    /// <summary>The fact (one of History.EditableCategories).</summary>
    public ClueCategory Category { get; }

    /// <summary>Its value now (a pending carry: the value on its way).</summary>
    public string Value { get; }

    /// <summary>Its authored value (a pending carry: its value now).</summary>
    public string Was { get; }

    /// <summary>The day the change applies from (a pending carry: the day it was recorded).</summary>
    public int Day { get; }
}

/// <summary>A part of the world summary: a heading over its lines.</summary>
public sealed class SummarySection
{
    /// <summary>The heading.</summary>
    public string Heading;

    /// <summary>Its lines, in order.</summary>
    public readonly List<string> Lines = new List<string>();
}

/// <summary>
/// The world a run leaves, as the world summary reads it (WorldSummary): the
/// run's history and places, the present, the attribute totals, each place's
/// dominant attribute and the history rules that fired. Filled from the run
/// and the content (WorldOutcomes.From).
/// </summary>
public sealed class WorldOutcome
{
    /// <summary>Every place of the content, Future places included, by country then era (SiteWorldBuilder.Places).</summary>
    public IReadOnlyList<PlaceInfo> Places = Array.Empty<PlaceInfo>();

    /// <summary>The run's history (the leader, last night's influence ranking, the latched edits, the pending carries); null = none.</summary>
    public HistoryState History;

    /// <summary>A nation's display name by id (the id itself when unknown).</summary>
    public Func<string, string> NationName = id => id;

    /// <summary>The present (the leader's Future place or the neutral present, its facts with history applied); null = none.</summary>
    public PresentPlace Present;

    /// <summary>Each attribute's name and its total across the timeline, in content order.</summary>
    public IReadOnlyList<KeyValuePair<string, float>> Attributes = Array.Empty<KeyValuePair<string, float>>();

    /// <summary>Each dominant (attribute name, place name) pair from last night's tiers, in content order (attribute, then place).</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Dominant = Array.Empty<KeyValuePair<string, string>>();

    /// <summary>The names of the one-shot history rules and timeline triggers that fired, in content order.</summary>
    public IReadOnlyList<string> Events = Array.Empty<string>();
}

/// <summary>
/// The neutral world summary of the run's last day (Saleh, 2026-09-29: "we
/// dont make judgements ... the outcome depends on your choices"): every
/// history factor the world model tracks, described as it stands, never
/// ranked or scored. The present and who shapes it, each country's influence
/// (in content order), each attribute's total, each place's dominant
/// attribute, every place fact history rewrote and every carry still on its
/// way, and the history rules that fired. The words are world_source.json
/// ui.strings (<see cref="WordKeys"/>); pure, so it is tested headless.
/// </summary>
public static class WorldSummary
{
    /// <summary>Every UI string key the summary writes with (Generate World and the validator require each one); the fact labels are ClueLabels.Key's.</summary>
    public static readonly string[] WordKeys =
    {
        "ending.world.present", "ending.world.leader", "ending.world.noLeader", "ending.world.presentPlace", "ending.world.fact",
        "ending.world.influenceHeading", "ending.world.influence", "ending.world.noInfluence",
        "ending.world.attributesHeading", "ending.world.attribute", "ending.world.noAttributes",
        "ending.world.dominantHeading", "ending.world.dominant", "ending.world.noDominant",
        "ending.world.changesHeading", "ending.world.change", "ending.world.pending", "ending.world.noChanges",
        "ending.world.eventsHeading", "ending.world.noEvents"
    };

    /// <summary>The separator between the places of one dominant line.</summary>
    private const string ListSeparator = ", ";

    /// <summary>
    /// Every place fact history left different from its authored value
    /// (History.IsRevised), in place order then History.EditableCategories
    /// order: its value now, its authored value and the day its latest edit
    /// applies from. None for a null history.
    /// </summary>
    public static List<PlaceChange> Changes(IEnumerable<PlaceInfo> places, HistoryState history)
    {
        var result = new List<PlaceChange>();
        if (places == null || history == null)
            return result;

        foreach (PlaceInfo place in places.Where(p => p != null))
        {
            foreach (ClueCategory category in History.EditableCategories)
            {
                string authored = place.BaseValue(category);
                if (authored == null)
                    continue;
                string now = History.Resolve(history, place.NationId, place.EraId, category, authored);
                if (!History.IsRevised(now, authored))
                    continue;
                FactEdit edit = History.LatestEdit(history, place.NationId, place.EraId, category);
                result.Add(new PlaceChange(place.Label, category, now, authored, edit != null ? edit.sinceDay : 0));
            }
        }

        return result;
    }

    /// <summary>
    /// The carries recorded and not yet latched (they latch at night, and the
    /// last day's night never comes), in record order: the claimed place, the
    /// value on its way, the place's value now and the day it was recorded.
    /// </summary>
    public static List<PlaceChange> Pending(IReadOnlyList<PlaceInfo> places, HistoryState history)
    {
        var result = new List<PlaceChange>();
        if (history == null || history.pendingCarries == null)
            return result;

        foreach (CarryRecord r in history.pendingCarries.Where(r => r != null && !string.IsNullOrWhiteSpace(r.value)))
        {
            PlaceInfo to = places?.FirstOrDefault(p => p != null && p.NationId == r.toNationId && p.EraId == r.toEraId);
            string now = History.Resolve(history, r.toNationId, r.toEraId, r.category, to?.BaseValue(r.category) ?? string.Empty);
            result.Add(new PlaceChange(to?.Label ?? $"{r.toNationId}_{r.toEraId}", r.category, r.value, now, r.day));
        }

        return result;
    }

    /// <summary>
    /// The summary's sections, in order: the present, influence, the
    /// timeline's attributes, the dominant attributes, the places history
    /// rewrote, and what happened. Each section says so when it has nothing to
    /// list. A null outcome is an empty summary.
    /// </summary>
    public static List<SummarySection> Sections(WorldOutcome outcome, IPageWords words)
    {
        var sections = new List<SummarySection>();
        if (outcome == null || words == null)
            return sections;

        sections.Add(PresentSection(outcome, words));
        sections.Add(InfluenceSection(outcome, words));
        sections.Add(List(words, "ending.world.attributesHeading", "ending.world.noAttributes",
                          outcome.Attributes.Select(a => words.Get("ending.world.attribute", a.Key, a.Value))));
        sections.Add(List(words, "ending.world.dominantHeading", "ending.world.noDominant",
                          outcome.Dominant.GroupBy(d => d.Key).Select(g => words.Get("ending.world.dominant", g.Key, string.Join(ListSeparator, g.Select(d => d.Value))))));
        sections.Add(List(words, "ending.world.changesHeading", "ending.world.noChanges",
                          Changes(outcome.Places, outcome.History).Select(c => words.Get("ending.world.change", c.Place, words.Get(ClueLabels.Key(c.Category)), c.Value, c.Was, c.Day))
                              .Concat(Pending(outcome.Places, outcome.History).Select(c => words.Get("ending.world.pending", c.Place, words.Get(ClueLabels.Key(c.Category)), c.Value, c.Was, c.Day)))));
        sections.Add(List(words, "ending.world.eventsHeading", "ending.world.noEvents", outcome.Events.Where(e => !string.IsNullOrWhiteSpace(e))));
        return sections;
    }

    /// <summary>The present: who shapes the timeline and since when (or nobody), the present's place and its facts.</summary>
    private static SummarySection PresentSection(WorldOutcome outcome, IPageWords words)
    {
        var section = new SummarySection { Heading = words.Get("ending.world.present") };
        string leader = History.FutureNation(outcome.History);
        section.Lines.Add(leader != null
            ? words.Get("ending.world.leader", outcome.NationName(leader), outcome.History.leaderSinceDay)
            : words.Get("ending.world.noLeader"));
        if (outcome.Present != null)
        {
            section.Lines.Add(words.Get("ending.world.presentPlace", outcome.Present.Label));
            foreach (KeyValuePair<ClueCategory, string> fact in outcome.Present.Facts)
                section.Lines.Add(words.Get("ending.world.fact", words.Get(ClueLabels.Key(fact.Key)), fact.Value));
        }
        return section;
    }

    /// <summary>Each country's influence from last night's ranking, in content order (never by rank); a country the places do not list comes last, in ranking order.</summary>
    private static SummarySection InfluenceSection(WorldOutcome outcome, IPageWords words)
    {
        List<string> order = outcome.Places.Where(p => p != null).Select(p => p.NationId).Distinct().ToList();
        IEnumerable<RankedScore> ranking = outcome.History?.ranking ?? new List<RankedScore>();
        IEnumerable<string> lines = ranking.Where(r => !string.IsNullOrEmpty(r.id))
                                           .Select((r, i) =>
                                           {
                                               int at = order.IndexOf(r.id);
                                               return (r, key: at >= 0 ? at : order.Count + i);
                                           })
                                           .OrderBy(x => x.key)
                                           .Select(x => words.Get("ending.world.influence", outcome.NationName(x.r.id), x.r.score));
        return List(words, "ending.world.influenceHeading", "ending.world.noInfluence", lines);
    }

    /// <summary>A section of <paramref name="lines"/> under <paramref name="headingKey"/>'s words, or the <paramref name="noneKey"/> line when there are none.</summary>
    private static SummarySection List(IPageWords words, string headingKey, string noneKey, IEnumerable<string> lines)
    {
        var section = new SummarySection { Heading = words.Get(headingKey) };
        section.Lines.AddRange(lines);
        if (section.Lines.Count == 0)
            section.Lines.Add(words.Get(noneKey));
        return section;
    }
}
