using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>One row of the Departure Board: its text and, apart, the state word the board prints in its state ink (null: none).</summary>
public readonly struct BoardRow
{
    /// <summary>The row's text ("01  ANCIENT  PERICLEAN ATHENS").</summary>
    public readonly string Text;

    /// <summary>The state word after it ("CLOSED", "UNDER MAINTENANCE", "NO ROUTE"), or null.</summary>
    public readonly string State;

    /// <summary>A row.</summary>
    public BoardRow(string text, string state)
    {
        Text = text ?? string.Empty;
        State = string.IsNullOrEmpty(state) ? null : state;
    }
}

/// <summary>
/// The words the portals print (the portals spec v3 BD3, BD4, PA2), from the
/// day's PortalDay, so the board, its tooltip and the Portals app say the same
/// thing: the board's rows, the tooltip's lines and the app's table cells.
/// <paramref name="text"/> arguments are the UI string lookup (UiText.Get),
/// <paramref name="eraName"/> and <paramref name="placeName"/> a route's era and
/// place display names. In the Domain (not Visuals, which cannot see PortalDay),
/// pure and tested headless.
/// </summary>
public static class PortalText
{
    /// <summary>A portal's number as the art and the board print it ("01").</summary>
    public static string Number(int number) => number.ToString("00", CultureInfo.InvariantCulture);

    /// <summary>
    /// The board's rows, one per portal in number order: an open or CLOSED
    /// route's number, era and place in capitals ("CLOSED" apart); the Return
    /// Gate's "03  RETURN GATE" ("UNDER MAINTENANCE" apart while under
    /// maintenance); a departure portal under maintenance its number and
    /// "UNDER MAINTENANCE"; one in service without a route its number and "NO ROUTE".
    /// </summary>
    public static List<BoardRow> BoardRows(PortalDay day, Func<PlaceRef, string> eraName, Func<PlaceRef, string> placeName, Func<string, string> text)
    {
        var rows = new List<BoardRow>();
        foreach (PortalRoute p in Routes(day))
        {
            string number = Number(p.Number);
            bool maintenance = p.State == PortalState.UnderMaintenance;
            if (p.Role == PortalRole.Returns)
                rows.Add(new BoardRow(number + "  " + text("board.returnGate"), maintenance ? text("board.maintenance") : null));
            else if (maintenance)
                rows.Add(new BoardRow(number, text("board.maintenance")));
            else if (!p.Place.HasValue)
                rows.Add(new BoardRow(number, text("board.noRoute")));
            else
                rows.Add(new BoardRow(number + "  " + Upper(eraName(p.Place.Value)) + "  " + Upper(placeName(p.Place.Value)), p.Closed ? text("board.closed") : null));
        }
        return rows;
    }

    /// <summary>
    /// The board's tooltip (BD4): its title, then one line per portal with its
    /// number and ring ("01 Front: Periclean Athens, Ancient"; "…: CLOSED
    /// today"; "03 Rear right: Return Gate[, under maintenance]"; "04 Upper
    /// left: under maintenance"; "…: no route today").
    /// </summary>
    public static List<string> TooltipLines(PortalDay day, Func<PlaceRef, string> eraName, Func<PlaceRef, string> placeName, Func<string, string> text)
    {
        var lines = new List<string> { text("board.tooltip.title") };
        foreach (PortalRoute p in Routes(day))
        {
            string number = Number(p.Number);
            bool maintenance = p.State == PortalState.UnderMaintenance;
            if (p.Role == PortalRole.Returns)
                lines.Add(Format(text(maintenance ? "board.tooltip.returnGateMaintenance" : "board.tooltip.returnGate"), number, p.Name));
            else if (maintenance)
                lines.Add(Format(text("board.tooltip.maintenance"), number, p.Name));
            else if (!p.Place.HasValue)
                lines.Add(Format(text("board.tooltip.noRoute"), number, p.Name));
            else
                lines.Add(Format(text(p.Closed ? "board.tooltip.closed" : "board.tooltip.route"), number, p.Name, placeName(p.Place.Value), eraName(p.Place.Value)));
        }
        return lines;
    }

    /// <summary>
    /// The Portals app's table (PA2, the form TC-970): one row per portal,
    /// its number, ring, era, place and state. A departure portal in service
    /// shows its route ("In service", or "CLOSED today: " and the closure's
    /// line; "In service · no route today" without one); the Return Gate its
    /// name for a place; a portal under maintenance no era or place and
    /// <paramref name="repairLine"/>'s words for its repair.
    /// </summary>
    public static List<string[]> AppRows(PortalDay day, Func<PlaceRef, string> eraName, Func<PlaceRef, string> placeName, Func<string, string> text,
                                         Func<PortalRoute, string> repairLine)
    {
        var rows = new List<string[]>();
        string none = text("portals.none");
        foreach (PortalRoute p in Routes(day))
        {
            string state;
            if (p.State == PortalState.UnderMaintenance)
                state = repairLine != null ? repairLine(p) : string.Empty;
            else if (p.Role == PortalRole.Departures && !p.Place.HasValue)
                state = text("portals.state.noRoute");
            else if (p.Closed)
                state = Format(text("portals.state.closed"), p.Closure);
            else
                state = text("portals.state.inService");

            bool route = p.Role == PortalRole.Departures && p.State == PortalState.InService && p.Place.HasValue;
            rows.Add(new[]
            {
                Number(p.Number),
                p.Name,
                route ? eraName(p.Place.Value) : none,
                route ? placeName(p.Place.Value) : p.Role == PortalRole.Returns ? text("portals.returnGate") : none,
                state
            });
        }
        return rows;
    }

    /// <summary>
    /// A repair's words in the app (PA2) from its Orders node's state: in
    /// transit (or delivered today by other means) "repair in transit, in
    /// service tomorrow"; locked "locked: repair <paramref name="needs"/>
    /// first"; orderable or too dear "repair it in Orders (<paramref name="price"/>)";
    /// no repair: "Under maintenance".
    /// </summary>
    public static string RepairLine(bool hasRepair, OrderState state, string price, string needs, Func<string, string> text)
    {
        if (!hasRepair)
            return text("portals.state.underMaintenance");
        switch (state)
        {
            case OrderState.InTransit:
            case OrderState.Owned:
                return text("portals.state.transit");
            case OrderState.Locked:
                return Format(text("portals.state.locked"), needs);
            default:
                return Format(text("portals.state.maintenance"), price);
        }
    }

    private static IReadOnlyList<PortalRoute> Routes(PortalDay day) => (day ?? PortalDay.None).Portals;

    private static string Upper(string s) => (s ?? string.Empty).ToUpperInvariant();

    private static string Format(string template, params object[] args) => string.Format(CultureInfo.InvariantCulture, template ?? string.Empty, args);
}
