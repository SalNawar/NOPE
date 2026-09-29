using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A portal's job (the portals spec v3 PO2; world_source.json agency.portals[].role).
/// Serialized in the content library's agency block: append only (SerializedEnumsTests).
/// </summary>
public enum PortalRole
{
    /// <summary>Runs the route the Directorate sets for it each day (RT2).</summary>
    Departures,

    /// <summary>The Return Gate: the displaced go home through it; it runs no route (it tunes to each traveller's incident).</summary>
    Returns
}

/// <summary>One ring of the hall as the content authors it (agency.portals[]: its number, the ring's name, its role, the day it enters service by itself and the repair that puts it in service).</summary>
[Serializable]
public sealed class PortalSpec
{
    /// <summary>The portal's number, as the art numbers its rings (01 front ... 05 upper right).</summary>
    public int number;

    /// <summary>The ring's name ("Front", "Rear left", ...), printed in the Portals app and the board's tooltip.</summary>
    public string name = string.Empty;

    /// <summary>What it does.</summary>
    public PortalRole role;

    /// <summary>The day it enters service by itself (0: only by its repair).</summary>
    public int fromDay;

    /// <summary>The upgrade id whose delivery puts it in service (blank: none; an Orders node).</summary>
    public string repair = string.Empty;
}

/// <summary>A portal's state today (runtime only; a breakdown pass may append, SM2).</summary>
public enum PortalState
{
    /// <summary>In service: its first day came or its repair was delivered.</summary>
    InService,

    /// <summary>Under maintenance: waiting for its repair (the Orders tree).</summary>
    UnderMaintenance
}

/// <summary>How a ring looks today (the portals spec v3 VX1-VX5; runtime only).</summary>
public enum PortalLook
{
    /// <summary>Under maintenance: a closed portal, nothing inside the ring (the hall leaves the ring's layer as the art drew it: the layer carries the wall around the frame, so a tint would grey that whole disc).</summary>
    Dimmed,

    /// <summary>In service with no open route (CLOSED today, or no route): a closed portal, nothing inside the ring.</summary>
    Empty,

    /// <summary>A departure portal in service on an open route: the glow inside the ring.</summary>
    Glow,

    /// <summary>The Return Gate in service: its own spiral inside the ring.</summary>
    ReturnGate
}

/// <summary>A route requested for a portal on a day: the Directorate's (days[].portals[]; the clerk's later, SM1).</summary>
public readonly struct PortalRequest
{
    /// <summary>The portal's number.</summary>
    public readonly int Portal;

    /// <summary>The place it runs to.</summary>
    public readonly PlaceRef Place;

    /// <summary>A request.</summary>
    public PortalRequest(int portal, PlaceRef place)
    {
        Portal = portal;
        Place = place;
    }
}

/// <summary>One portal on the day (PortalSchedule.Resolve): its state and, for a departure portal in service, its route and whether a closure forbids it today.</summary>
public readonly struct PortalRoute
{
    /// <summary>The portal's number.</summary>
    public readonly int Number;

    /// <summary>The ring's name.</summary>
    public readonly string Name;

    /// <summary>Its role.</summary>
    public readonly PortalRole Role;

    /// <summary>In service or under maintenance today.</summary>
    public readonly PortalState State;

    /// <summary>Its route today (a departure portal in service with a request), else null.</summary>
    public readonly PlaceRef? Place;

    /// <summary>The line of the closure that forbids its route today (CLOSED), else null.</summary>
    public readonly string Closure;

    /// <summary>The upgrade id of its repair (blank: none).</summary>
    public readonly string Repair;

    /// <summary>A portal's day.</summary>
    public PortalRoute(int number, string name, PortalRole role, PortalState state, PlaceRef? place, string closure, string repair)
    {
        Number = number;
        Name = name ?? string.Empty;
        Role = role;
        State = state;
        Place = place;
        Closure = string.IsNullOrWhiteSpace(closure) ? null : closure;
        Repair = repair ?? string.Empty;
    }

    /// <summary>True when a closure forbids its route today.</summary>
    public bool Closed => Closure != null;

    /// <summary>How its ring looks (VX1-VX5): Dimmed under maintenance (nothing inside), the Return Gate's spiral, the glow on an open route, else empty.</summary>
    public PortalLook Look =>
        State == PortalState.UnderMaintenance ? PortalLook.Dimmed
        : Role == PortalRole.Returns ? PortalLook.ReturnGate
        : Place.HasValue && !Closed ? PortalLook.Glow
        : PortalLook.Empty;
}

/// <summary>
/// The day's portals (the portals spec v3 RT3), fixed at the day's start
/// (ContentLibrarySO.BuildToday): the board, its tooltip, the Portals app,
/// the rings and the departures read this one object, so the hall and the PC
/// never disagree.
/// </summary>
public sealed class PortalDay
{
    /// <summary>A day with no portals (content without them): nothing shows and nobody leaves through a ring.</summary>
    public static readonly PortalDay None = new PortalDay(Array.Empty<PortalRoute>(), 0);

    /// <summary>A day of <paramref name="portals"/> (in number order) whose default departures portal is <paramref name="defaultPortal"/> (0: none).</summary>
    public PortalDay(IReadOnlyList<PortalRoute> portals, int defaultPortal)
    {
        Portals = portals ?? Array.Empty<PortalRoute>();
        Default = defaultPortal;
    }

    /// <summary>Every portal, in number order.</summary>
    public IReadOnlyList<PortalRoute> Portals { get; }

    /// <summary>The default departures portal (01: in service from day 1 by itself; PortalSchedule.DefaultPortal), 0 when none.</summary>
    public int Default { get; }

    /// <summary>
    /// Where an accepted traveller leaves (PO4): the displaced through the
    /// Returns portal in service, else the default portal; a 2150 citizen
    /// through the departure portal in service whose open route is
    /// <paramref name="claim"/>, else the default portal (never a CLOSED
    /// route's). 0 when there is no default. The delayed destination check
    /// replaces the fallback for a traveller no open portal serves (SK3).
    /// </summary>
    public int DepartureFor(PlaceRef claim, bool displaced)
    {
        foreach (PortalRoute p in Portals)
        {
            if (p.State != PortalState.InService)
                continue;
            if (displaced ? p.Role == PortalRole.Returns : p.Role == PortalRole.Departures && !p.Closed && p.Place.HasValue && p.Place.Value == claim)
                return p.Number;
        }
        return Default;
    }
}

/// <summary>
/// The portals' rules (the portals spec v3 PO3, RT2, RT4): which portal is in
/// service, the day's schedule over the route requests and today's closures,
/// and the content checks Generate World and the validator run. Pure.
/// </summary>
public static class PortalSchedule
{
    /// <summary>
    /// True when <paramref name="portal"/> is in service on <paramref name="day"/>:
    /// its first day has come, or <paramref name="repaired"/> owns its repair
    /// (whichever comes first). The one place a portal's state is decided: a
    /// breakdown rule goes here (SM2).
    /// </summary>
    public static bool InService(PortalSpec portal, int day, Func<string, bool> repaired) =>
        portal != null &&
        ((portal.fromDay > 0 && day >= portal.fromDay) ||
         (!string.IsNullOrWhiteSpace(portal.repair) && repaired != null && repaired(portal.repair)));

    /// <summary>The default departures portal: the first departures portal (by number) in service from day 1 by itself; 0 when none.</summary>
    public static int DefaultPortal(IReadOnlyList<PortalSpec> portals) =>
        (portals ?? Array.Empty<PortalSpec>())
            .Where(p => p != null && p.role == PortalRole.Departures && p.fromDay == 1)
            .Select(p => p.number)
            .DefaultIfEmpty(0)
            .Min();

    /// <summary>
    /// The day's schedule (RT2): every portal in number order, in service or
    /// not; a departure portal in service runs the first of
    /// <paramref name="requests"/> for it (today the Directorate's routes; the
    /// clerk's requests join here, SM1), marked CLOSED with the line
    /// <paramref name="closure"/> gives for its place (null: open); a portal
    /// under maintenance and the Returns portal run nothing. Pure, no draw.
    /// </summary>
    public static PortalDay Resolve(IReadOnlyList<PortalSpec> portals, int day, Func<string, bool> repaired,
                                    IReadOnlyList<PortalRequest> requests, Func<PlaceRef, string> closure)
    {
        var routes = new List<PortalRoute>();
        foreach (PortalSpec p in (portals ?? Array.Empty<PortalSpec>()).Where(p => p != null).OrderBy(p => p.number))
        {
            bool inService = InService(p, day, repaired);
            PlaceRef? place = null;
            string closedBy = null;
            if (inService && p.role == PortalRole.Departures)
            {
                foreach (PortalRequest r in requests ?? Array.Empty<PortalRequest>())
                {
                    if (r.Portal != p.number)
                        continue;
                    place = r.Place;
                    closedBy = closure?.Invoke(r.Place);
                    break;
                }
            }
            routes.Add(new PortalRoute(p.number, p.name, p.role, inService ? PortalState.InService : PortalState.UnderMaintenance, place, closedBy, p.repair));
        }
        return new PortalDay(routes, DefaultPortal(portals));
    }

    /// <summary>
    /// What Generate World and the validator refuse in agency.portals: a
    /// number not positive or listed twice, a blank name, a negative first
    /// day, a portal never in service (no first day, no repair), a repair
    /// <paramref name="upgradeExists"/> does not know or that two portals
    /// share, more than one Returns portal, and no departures portal in
    /// service on day 1 by itself (the default every fallback departure uses).
    /// Empty when sound, or when there are no portals.
    /// </summary>
    public static List<string> PortalProblems(IReadOnlyList<PortalSpec> portals, Func<string, bool> upgradeExists)
    {
        var problems = new List<string>();
        List<PortalSpec> list = (portals ?? Array.Empty<PortalSpec>()).Where(p => p != null).ToList();
        if (list.Count == 0)
            return problems;

        var numbers = new HashSet<int>();
        var repairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (PortalSpec p in list)
        {
            string at = $"agency.portals {p.number}";
            if (p.number <= 0)
                problems.Add($"{at}: the number must be positive (the art's ring number).");
            else if (!numbers.Add(p.number))
                problems.Add($"{at}: the number is listed twice.");
            if (string.IsNullOrWhiteSpace(p.name))
                problems.Add($"{at}: the ring's name is blank.");
            if (p.fromDay < 0)
                problems.Add($"{at}: fromDay is {p.fromDay}; a first day is 1 or more, or blank for a portal only its repair puts in service.");
            bool hasRepair = !string.IsNullOrWhiteSpace(p.repair);
            if (p.fromDay <= 0 && !hasRepair)
                problems.Add($"{at}: it is never in service (no fromDay and no repair).");
            if (hasRepair && (upgradeExists == null || !upgradeExists(p.repair)))
                problems.Add($"{at}: its repair '{p.repair}' is not an upgrade of the content library.");
            else if (hasRepair && !repairs.Add(p.repair))
                problems.Add($"{at}: its repair '{p.repair}' already repairs another portal.");
        }

        if (list.Count(p => p.role == PortalRole.Returns) > 1)
            problems.Add("agency.portals: more than one portal has the role Returns; the hall has one Return Gate.");
        if (DefaultPortal(list) == 0)
            problems.Add("agency.portals: no Departures portal is in service from day 1 by itself (fromDay 1); every fallback departure leaves through it.");
        return problems;
    }

    /// <summary>
    /// What Generate World and the validator refuse in one day's routes
    /// (days[].portals[], <paramref name="day"/> names the day in the
    /// messages): a route for a portal that does not exist or runs no routes
    /// (the Returns portal), a departure portal with no route or two, a place
    /// outside the day's world (<paramref name="inWorld"/>), one place on two
    /// portals, and the default portal's route closed today
    /// (<paramref name="closed"/>; RT4). A closed route on another portal is
    /// allowed: it shows CLOSED on purpose. Empty when sound, or when there
    /// are no portals.
    /// </summary>
    public static List<string> DayProblems(string day, IReadOnlyList<PortalSpec> portals, IReadOnlyList<PortalRequest> routes,
                                           Func<PlaceRef, bool> inWorld, Func<PlaceRef, bool> closed)
    {
        var problems = new List<string>();
        List<PortalSpec> list = (portals ?? Array.Empty<PortalSpec>()).Where(p => p != null).ToList();
        if (list.Count == 0)
            return problems;

        IReadOnlyList<PortalRequest> all = routes ?? Array.Empty<PortalRequest>();
        var places = new HashSet<PlaceRef>();
        foreach (PortalRequest r in all)
        {
            PortalSpec portal = list.FirstOrDefault(p => p.number == r.Portal);
            if (portal == null)
                problems.Add($"{day}.portals: portal {r.Portal} is not one of agency.portals.");
            else if (portal.role != PortalRole.Departures)
                problems.Add($"{day}.portals: portal {r.Portal} is the Return Gate; it takes no route.");
            if (inWorld == null || !inWorld(r.Place))
                problems.Add($"{day}.portals: portal {r.Portal}'s route {r.Place} is not a place of the day's world (its eras and countries, never the Future).");
            if (!places.Add(r.Place))
                problems.Add($"{day}.portals: the route {r.Place} is on two portals; a day's routes are distinct.");
        }

        int fallback = DefaultPortal(list);
        foreach (PortalSpec p in list.Where(p => p.role == PortalRole.Departures).OrderBy(p => p.number))
        {
            List<PortalRequest> mine = all.Where(r => r.Portal == p.number).ToList();
            if (mine.Count == 0)
                problems.Add($"{day}.portals: portal {p.number} has no route; every departure portal runs one each day.");
            else if (mine.Count > 1)
                problems.Add($"{day}.portals: portal {p.number} is given a route twice.");
            else if (p.number == fallback && closed != null && closed(mine[0].Place))
                problems.Add($"{day}.portals: portal {p.number}'s route {mine[0].Place} is closed today; the default departures portal's route is always open.");
        }
        return problems;
    }
}
