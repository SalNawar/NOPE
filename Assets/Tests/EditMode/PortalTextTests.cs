using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The portals' words (the portals spec v3 BD3, BD4, PA2): the Departure
/// Board's rows in number order (era then place in capitals, CLOSED, RETURN
/// GATE, UNDER MAINTENANCE apart), its tooltip's lines with each ring's name,
/// the Portals app's table and a repair's state line.
/// </summary>
public class PortalTextTests
{
    private static readonly PlaceRef Athens = new PlaceRef("greece", "ancient");
    private static readonly PlaceRef Thebes = new PlaceRef("egypt", "ancient");

    private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
    {
        { "board.returnGate", "RETURN GATE" }, { "board.closed", "CLOSED" }, { "board.maintenance", "UNDER MAINTENANCE" }, { "board.noRoute", "NO ROUTE" },
        { "board.tooltip.title", "DEPARTURE BOARD" }, { "board.tooltip.route", "{0} {1}: {2}, {3}" }, { "board.tooltip.closed", "{0} {1}: {2}, {3}: CLOSED today" },
        { "board.tooltip.returnGate", "{0} {1}: Return Gate" }, { "board.tooltip.returnGateMaintenance", "{0} {1}: Return Gate, under maintenance" },
        { "board.tooltip.maintenance", "{0} {1}: under maintenance" }, { "board.tooltip.noRoute", "{0} {1}: no route today" },
        { "portals.none", "-" }, { "portals.returnGate", "Return Gate" }, { "portals.state.inService", "In service" },
        { "portals.state.noRoute", "In service, no route today" }, { "portals.state.closed", "CLOSED today: {0}" },
        { "portals.state.underMaintenance", "Under maintenance" }, { "portals.state.maintenance", "Under maintenance, repair it in Orders ({0})" },
        { "portals.state.locked", "Under maintenance, locked: repair {0} first" }, { "portals.state.transit", "Under maintenance, repair in transit" },
    };

    private static string Text(string key) => Strings[key];
    private static string Era(PlaceRef p) => p.EraId == "ancient" ? "Ancient" : "?";
    private static string Place(PlaceRef p) => p.Equals(Athens) ? "Periclean Athens" : p.Equals(Thebes) ? "New Kingdom Egypt" : "?";

    /// <summary>Day 2 of the spec §3: 01 Athens, 02 Thebes (closed), the Return Gate repaired, 04 and 05 under maintenance.</summary>
    private static PortalDay DayTwo(bool gate = true) => new PortalDay(new[]
    {
        new PortalRoute(1, "Front", PortalRole.Departures, PortalState.InService, Athens, null, ""),
        new PortalRoute(2, "Rear left", PortalRole.Departures, PortalState.InService, Thebes, "Embargo.", "repair_portal_02"),
        new PortalRoute(3, "Rear right", PortalRole.Returns, gate ? PortalState.InService : PortalState.UnderMaintenance, null, null, "repair_return_gate"),
        new PortalRoute(4, "Upper left", PortalRole.Departures, PortalState.UnderMaintenance, null, null, "repair_portal_04"),
        new PortalRoute(5, "Upper right", PortalRole.Departures, PortalState.InService, null, null, "repair_portal_05"),
    }, 1);

    [Test]
    public void Rows_EveryPortalInNumberOrder_NumberEraThenPlaceUpperCase()
    {
        List<BoardRow> rows = PortalText.BoardRows(DayTwo(), Era, Place, Text);
        Assert.AreEqual(5, rows.Count);
        Assert.AreEqual("01  ANCIENT  PERICLEAN ATHENS", rows[0].Text);
        Assert.IsNull(rows[0].State, "an open route has no state word");
    }

    [Test]
    public void Rows_AClosedRouteEndsWithClosed()
    {
        BoardRow two = PortalText.BoardRows(DayTwo(), Era, Place, Text)[1];
        Assert.AreEqual("02  ANCIENT  NEW KINGDOM EGYPT", two.Text);
        Assert.AreEqual("CLOSED", two.State);
    }

    [Test]
    public void Rows_TheReturnGateRow()
    {
        Assert.AreEqual("03  RETURN GATE", PortalText.BoardRows(DayTwo(), Era, Place, Text)[2].Text);
        Assert.IsNull(PortalText.BoardRows(DayTwo(), Era, Place, Text)[2].State);
        Assert.AreEqual("UNDER MAINTENANCE", PortalText.BoardRows(DayTwo(false), Era, Place, Text)[2].State);
    }

    [Test]
    public void Rows_UnderMaintenance_AndNoRoute()
    {
        List<BoardRow> rows = PortalText.BoardRows(DayTwo(), Era, Place, Text);
        Assert.AreEqual("04", rows[3].Text);
        Assert.AreEqual("UNDER MAINTENANCE", rows[3].State);
        Assert.AreEqual("05", rows[4].Text);
        Assert.AreEqual("NO ROUTE", rows[4].State);
        CollectionAssert.IsEmpty(PortalText.BoardRows(PortalDay.None, Era, Place, Text), "no portals: no rows");
    }

    [Test]
    public void Tooltip_ListsEveryPortalWithItsRingAndState()
    {
        CollectionAssert.AreEqual(new[]
        {
            "DEPARTURE BOARD",
            "01 Front: Periclean Athens, Ancient",
            "02 Rear left: New Kingdom Egypt, Ancient: CLOSED today",
            "03 Rear right: Return Gate",
            "04 Upper left: under maintenance",
            "05 Upper right: no route today",
        }, PortalText.TooltipLines(DayTwo(), Era, Place, Text));
        Assert.AreEqual("03 Rear right: Return Gate, under maintenance", PortalText.TooltipLines(DayTwo(false), Era, Place, Text)[3]);
    }

    [Test]
    public void AppRows_NumberRingEraPlaceAndState()
    {
        List<string[]> rows = PortalText.AppRows(DayTwo(), Era, Place, Text, p => "repair " + p.Repair);
        CollectionAssert.AreEqual(new[] { "01", "Front", "Ancient", "Periclean Athens", "In service" }, rows[0]);
        CollectionAssert.AreEqual(new[] { "02", "Rear left", "Ancient", "New Kingdom Egypt", "CLOSED today: Embargo." }, rows[1]);
        CollectionAssert.AreEqual(new[] { "03", "Rear right", "-", "Return Gate", "In service" }, rows[2]);
        CollectionAssert.AreEqual(new[] { "04", "Upper left", "-", "-", "repair repair_portal_04" }, rows[3], "a portal under maintenance runs nothing: its repair's line");
        CollectionAssert.AreEqual(new[] { "05", "Upper right", "-", "-", "In service, no route today" }, rows[4]);
        CollectionAssert.AreEqual(new[] { "03", "Rear right", "-", "Return Gate", "repair repair_return_gate" }, PortalText.AppRows(DayTwo(false), Era, Place, Text, p => "repair " + p.Repair)[2]);
    }

    [Test]
    public void RepairLine_ByTheNodesState()
    {
        Assert.AreEqual("Under maintenance, repair it in Orders (250 cr)", PortalText.RepairLine(true, OrderState.Orderable, "250 cr", "02", Text));
        Assert.AreEqual("Under maintenance, repair it in Orders (250 cr)", PortalText.RepairLine(true, OrderState.TooDear, "250 cr", "02", Text));
        Assert.AreEqual("Under maintenance, locked: repair 02 first", PortalText.RepairLine(true, OrderState.Locked, "250 cr", "02", Text));
        Assert.AreEqual("Under maintenance, repair in transit", PortalText.RepairLine(true, OrderState.InTransit, "250 cr", "02", Text));
        Assert.AreEqual("Under maintenance", PortalText.RepairLine(false, OrderState.Orderable, "", "", Text));
    }

    [Test]
    public void Number_IsTwoDigits()
    {
        Assert.AreEqual("01", PortalText.Number(1));
        Assert.AreEqual("12", PortalText.Number(12));
    }

    [Test]
    public void EveryKeyTheWordsRead_IsInTheReadingTable()
    {
        var table = new HashSet<string>(ContentJson.Parse(System.IO.File.ReadAllText(SourcePath()))
                                             .Get("ui").Get("strings").Items.Select(e => e.Get("key").Text));
        foreach (string key in Strings.Keys)
            Assert.IsTrue(table.Contains(key), key);
    }

    private static string SourcePath([System.Runtime.CompilerServices.CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        return System.IO.File.Exists(source) ? source : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(here), "..", "..", "Data", "World", "world_source.json");
    }
}
