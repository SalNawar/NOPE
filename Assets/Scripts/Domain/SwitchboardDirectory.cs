using System;
using System.Collections.Generic;

/// <summary>One of today's places as the switchboard lists it: its ids, its words on the board and its era's place in time. Not serialized.</summary>
public readonly struct SwitchPlace
{
    /// <summary>The destination's nation id ("greece").</summary>
    public readonly string NationId;

    /// <summary>The period's era id ("ancient").</summary>
    public readonly string EraId;

    /// <summary>The destination's words on its name plate ("GREECE").</summary>
    public readonly string NationLabel;

    /// <summary>The period's words on its tag ("ANCIENT").</summary>
    public readonly string EraLabel;

    /// <summary>The era's place in time (EraSO.order): a destination's periods read oldest first.</summary>
    public readonly int EraOrder;

    /// <summary>The place's moment year (negative = BCE): the tag reads it when two periods of one destination share their words.</summary>
    public readonly int Year;

    /// <summary>A place on the board.</summary>
    public SwitchPlace(string nationId, string eraId, string nationLabel, string eraLabel, int eraOrder, int year)
    {
        NationId = nationId ?? string.Empty;
        EraId = eraId ?? string.Empty;
        NationLabel = nationLabel ?? string.Empty;
        EraLabel = eraLabel ?? string.Empty;
        EraOrder = eraOrder;
        Year = year;
    }
}

/// <summary>One period jack's place in the directory: its era and the words on its tag. Not serialized.</summary>
public sealed class SwitchPeriod
{
    /// <summary>The era's id.</summary>
    public string EraId { get; }

    /// <summary>The words on its tag.</summary>
    public string Label { get; internal set; }

    /// <summary>The era's place in time.</summary>
    public int EraOrder { get; }

    /// <summary>The place's moment year.</summary>
    public int Year { get; }

    /// <summary>A period from its place.</summary>
    public SwitchPeriod(SwitchPlace place)
    {
        EraId = place.EraId;
        Label = place.EraLabel;
        EraOrder = place.EraOrder;
        Year = place.Year;
    }
}

/// <summary>One destination slot of the directory: its nation, the words on its name plate and its period jacks in order. Not serialized.</summary>
public sealed class SwitchDestination
{
    private readonly List<SwitchPeriod> _periods = new List<SwitchPeriod>();

    /// <summary>A destination from its first place.</summary>
    public SwitchDestination(SwitchPlace place)
    {
        NationId = place.NationId;
        Label = place.NationLabel;
    }

    /// <summary>The nation's id.</summary>
    public string NationId { get; }

    /// <summary>The words on its name plate.</summary>
    public string Label { get; }

    /// <summary>Its period jacks, top first.</summary>
    public IReadOnlyList<SwitchPeriod> Periods => _periods;

    /// <summary>The row of <paramref name="eraId"/>, or -1.</summary>
    public int RowOf(string eraId)
    {
        for (int i = 0; i < _periods.Count; i++)
            if (string.Equals(_periods[i].EraId, eraId, StringComparison.Ordinal))
                return i;
        return -1;
    }

    internal void Add(SwitchPlace place) => _periods.Add(new SwitchPeriod(place));

    internal void SortByTime() => _periods.Sort((a, b) => a.EraOrder != b.EraOrder ? a.EraOrder.CompareTo(b.EraOrder) : a.Year.CompareTo(b.Year));

    /// <summary>Two periods with the same words read their years instead (BirthDates.FormatYear), so every tag on a destination differs.</summary>
    internal void Disambiguate()
    {
        var shared = new List<SwitchPeriod>();
        foreach (SwitchPeriod p in _periods)
            foreach (SwitchPeriod q in _periods)
                if (p != q && string.Equals(p.Label, q.Label, StringComparison.OrdinalIgnoreCase))
                {
                    shared.Add(p);
                    break;
                }
        foreach (SwitchPeriod p in shared)
            p.Label = BirthDates.FormatYear(p.Year);
    }
}

/// <summary>
/// Which of today's places the switchboard's blank plates carry (LOOP_v5:
/// "today's destinations, each with its period jacks under it"; the
/// seconds-to-learn rule: labels change per day on fixed slots): up to
/// maxDestinations name plates and maxPeriods period jacks each (the v3
/// board's 4 x 3), filled from the places in the order given (the view puts
/// the day's open destinations first, then the portal routes, then the rest
/// of today's places), each destination's periods oldest first. A call for a
/// place that is not on the board takes its nation's next free period jack or
/// the next free slot (Place); with neither, there is no jack for it. Pure.
/// </summary>
public sealed class SwitchboardDirectory
{
    private readonly List<SwitchDestination> _destinations = new List<SwitchDestination>();

    /// <summary>An empty directory of <paramref name="maxDestinations"/> slots of <paramref name="maxPeriods"/> periods.</summary>
    public SwitchboardDirectory(int maxDestinations, int maxPeriods)
    {
        MaxDestinations = Math.Max(0, maxDestinations);
        MaxPeriods = Math.Max(0, maxPeriods);
    }

    /// <summary>How many name plates the board has.</summary>
    public int MaxDestinations { get; }

    /// <summary>How many period jacks each destination has.</summary>
    public int MaxPeriods { get; }

    /// <summary>The destinations on the board, left first.</summary>
    public IReadOnlyList<SwitchDestination> Destinations => _destinations;

    /// <summary>The board's directory from <paramref name="places"/> in priority order (repeats and blank ids skipped), each destination's periods sorted oldest first and made distinct.</summary>
    public static SwitchboardDirectory Build(IEnumerable<SwitchPlace> places, int maxDestinations, int maxPeriods)
    {
        var directory = new SwitchboardDirectory(maxDestinations, maxPeriods);
        if (places != null)
            foreach (SwitchPlace place in places)
                directory.Add(place, out _);
        foreach (SwitchDestination d in directory._destinations)
        {
            d.SortByTime();
            d.Disambiguate();
        }
        return directory;
    }

    /// <summary>The route of the place (<paramref name="nationId"/>, <paramref name="eraId"/>) when it is on the board.</summary>
    public bool TryFind(string nationId, string eraId, out SwitchRoute route)
    {
        for (int d = 0; d < _destinations.Count; d++)
        {
            if (!string.Equals(_destinations[d].NationId, nationId, StringComparison.Ordinal))
                continue;
            int row = _destinations[d].RowOf(eraId);
            if (row >= 0)
            {
                route = new SwitchRoute(d, row);
                return true;
            }
        }
        route = default;
        return false;
    }

    /// <summary>The route of <paramref name="place"/>: found, or added at its nation's next free period jack or the next free slot (never reordering a jack already printed); false when the board has no room for it.</summary>
    public bool Place(SwitchPlace place, out SwitchRoute route) => TryFind(place.NationId, place.EraId, out route) || Add(place, out route);

    private bool Add(SwitchPlace place, out SwitchRoute route)
    {
        route = default;
        if (string.IsNullOrEmpty(place.NationId) || string.IsNullOrEmpty(place.EraId))
            return false;
        if (TryFind(place.NationId, place.EraId, out route))
            return true;
        int d = _destinations.FindIndex(x => string.Equals(x.NationId, place.NationId, StringComparison.Ordinal));
        if (d < 0)
        {
            if (_destinations.Count >= MaxDestinations)
                return false;
            _destinations.Add(new SwitchDestination(place));
            d = _destinations.Count - 1;
        }
        SwitchDestination destination = _destinations[d];
        if (destination.Periods.Count >= MaxPeriods)
            return false;
        destination.Add(place);
        destination.Disambiguate();
        route = new SwitchRoute(d, destination.Periods.Count - 1);
        return true;
    }
}
