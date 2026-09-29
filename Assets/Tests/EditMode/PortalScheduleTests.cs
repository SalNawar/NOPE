using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The hall's portals (the portals spec v3 PO1-PO7, RT1-RT4): which portal is
/// in service on a day (its first day, or its repair delivered), the day's
/// schedule over the Directorate's routes and today's closures, how each ring
/// looks, where an accepted traveller leaves, and what Generate World refuses
/// in the portals and each day's routes.
/// </summary>
public class PortalScheduleTests
{
    private static readonly PlaceRef Athens = new PlaceRef("greece", "ancient");
    private static readonly PlaceRef Thebes = new PlaceRef("egypt", "ancient");
    private static readonly PlaceRef Florence = new PlaceRef("italy", "medieval");
    private static readonly PlaceRef Wessex = new PlaceRef("britain", "medieval");
    private static readonly PlaceRef Babylon = new PlaceRef("iraq", "ancient");

    private const string Embargo = "Embargo: no travel to New Kingdom Egypt today.";

    private static PortalSpec Portal(int number, PortalRole role, int fromDay, string repair) =>
        new PortalSpec { number = number, name = "Ring " + number, role = role, fromDay = fromDay, repair = repair };

    /// <summary>The content's five: 01 from day 1, 02 and 04 and 05 by their repairs, the Return Gate (03) by its own.</summary>
    private static List<PortalSpec> Hall() => new List<PortalSpec>
    {
        Portal(1, PortalRole.Departures, 1, null),
        Portal(2, PortalRole.Departures, 0, "repair_portal_02"),
        Portal(3, PortalRole.Returns, 0, "repair_return_gate"),
        Portal(4, PortalRole.Departures, 0, "repair_portal_04"),
        Portal(5, PortalRole.Departures, 0, "repair_portal_05"),
    };

    /// <summary>Day 2's routes (the spec §3): 01 Athens, 02 Thebes (closed today), 04 Florence, 05 Wessex.</summary>
    private static List<PortalRequest> DayTwo() => new List<PortalRequest>
    {
        new PortalRequest(1, Athens), new PortalRequest(2, Thebes), new PortalRequest(4, Florence), new PortalRequest(5, Wessex)
    };

    private static string ClosesThebes(PlaceRef place) => place.Equals(Thebes) ? Embargo : null;

    private static System.Func<string, bool> Owning(params string[] ids) => id => ids.Contains(id);

    private static PortalDay Day(int day, params string[] repaired) =>
        PortalSchedule.Resolve(Hall(), day, Owning(repaired), DayTwo(), ClosesThebes);

    private static PortalRoute Route(PortalDay day, int number) => day.Portals.Single(p => p.Number == number);

    // --- Places ---

    [Test]
    public void PlaceRef_IsEqualByItsIds()
    {
        Assert.AreEqual(new PlaceRef("greece", "ancient"), Athens);
        Assert.IsTrue(new PlaceRef("greece", "ancient") == Athens);
        Assert.AreNotEqual(new PlaceRef("greece", "medieval"), Athens);
        Assert.AreNotEqual(new PlaceRef("egypt", "ancient"), Athens);
        Assert.AreEqual(Athens.GetHashCode(), new PlaceRef("greece", "ancient").GetHashCode());
        Assert.AreEqual("greece:ancient", Athens.ToString(), "a route reads as country:era in the checks' messages");
    }

    // --- Service (SM2's seam) ---

    [Test]
    public void InService_ByDayFromItsFirstDay()
    {
        PortalSpec p = Portal(1, PortalRole.Departures, 3, null);
        Assert.IsFalse(PortalSchedule.InService(p, 2, Owning()));
        Assert.IsTrue(PortalSchedule.InService(p, 3, Owning()));
        Assert.IsTrue(PortalSchedule.InService(p, 9, Owning()));
    }

    [Test]
    public void InService_ByADeliveredRepair()
    {
        PortalSpec p = Portal(2, PortalRole.Departures, 0, "repair_portal_02");
        Assert.IsFalse(PortalSchedule.InService(p, 5, Owning("repair_portal_04")));
        Assert.IsTrue(PortalSchedule.InService(p, 1, Owning("repair_portal_02")));
    }

    [Test]
    public void InService_WithBothWhicheverComesFirst()
    {
        PortalSpec p = Portal(5, PortalRole.Departures, 9, "repair_portal_05");
        Assert.IsFalse(PortalSchedule.InService(p, 4, Owning()), "neither yet");
        Assert.IsTrue(PortalSchedule.InService(p, 4, Owning("repair_portal_05")), "the repair came first");
        Assert.IsTrue(PortalSchedule.InService(p, 9, Owning()), "its first day came first");
    }

    [Test]
    public void InService_NeverWithNeither()
    {
        PortalSpec p = Portal(4, PortalRole.Departures, 0, null);
        Assert.IsFalse(PortalSchedule.InService(p, 1, Owning("repair_portal_04")));
        Assert.IsFalse(PortalSchedule.InService(p, 99, Owning()));
        Assert.IsFalse(PortalSchedule.InService(null, 1, Owning()));
    }

    // --- Resolution (SM1's seam) ---

    [Test]
    public void Resolve_APortalInServiceRunsItsRequest()
    {
        PortalDay day = Day(2, "repair_portal_04");
        Assert.AreEqual(PortalState.InService, Route(day, 1).State);
        Assert.AreEqual(Athens, Route(day, 1).Place);
        Assert.AreEqual(Florence, Route(day, 4).Place);
        Assert.IsFalse(Route(day, 4).Closed);
    }

    [Test]
    public void Resolve_APortalUnderMaintenanceRunsNothing()
    {
        PortalRoute two = Route(Day(2), 2);
        Assert.AreEqual(PortalState.UnderMaintenance, two.State);
        Assert.IsNull(two.Place, "a portal under maintenance runs no route today");
        Assert.IsFalse(two.Closed);
        Assert.AreEqual("repair_portal_02", two.Repair, "its repair, for the app's state line");
    }

    [Test]
    public void Resolve_AClosedRouteIsMarked()
    {
        PortalRoute two = Route(Day(2, "repair_portal_02"), 2);
        Assert.AreEqual(PortalState.InService, two.State);
        Assert.AreEqual(Thebes, two.Place, "a closed route is still the portal's route");
        Assert.IsTrue(two.Closed);
        Assert.AreEqual(Embargo, two.Closure, "with the closure's line");
        Assert.IsNull(Route(Day(2, "repair_portal_02"), 1).Closure, "an open route has no line");
    }

    [Test]
    public void Resolve_TheReturnsPortalHasNoRoute()
    {
        var requests = new List<PortalRequest>(DayTwo()) { new PortalRequest(3, Babylon) };
        PortalDay day = PortalSchedule.Resolve(Hall(), 2, Owning("repair_return_gate"), requests, ClosesThebes);
        Assert.AreEqual(PortalState.InService, Route(day, 3).State);
        Assert.AreEqual(PortalRole.Returns, Route(day, 3).Role);
        Assert.IsNull(Route(day, 3).Place, "the Return Gate tunes to each traveller's incident: no route");
    }

    [Test]
    public void Resolve_ListsEveryPortalInNumberOrder()
    {
        List<PortalSpec> shuffled = Hall();
        shuffled.Reverse();
        PortalDay day = PortalSchedule.Resolve(shuffled, 1, Owning(), DayTwo(), ClosesThebes);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, day.Portals.Select(p => p.Number).ToArray());
        CollectionAssert.AreEqual(new[] { "Ring 1", "Ring 2", "Ring 3", "Ring 4", "Ring 5" }, day.Portals.Select(p => p.Name).ToArray());
        Assert.AreEqual(1, day.Default, "the default departures portal: the first in service from day 1 by itself");
    }

    [Test]
    public void Resolve_ADeparturePortalWithNoRequestRunsNothing()
    {
        PortalDay day = PortalSchedule.Resolve(Hall(), 2, Owning("repair_portal_02"), new List<PortalRequest> { new PortalRequest(1, Athens) }, ClosesThebes);
        Assert.AreEqual(PortalState.InService, Route(day, 2).State);
        Assert.IsNull(Route(day, 2).Place);
        Assert.AreEqual(PortalLook.Empty, Route(day, 2).Look);
    }

    // --- The rings (VX1-VX5) ---

    [Test]
    public void Look_EachStateDrawsItsOwnRing()
    {
        PortalDay day = Day(2, "repair_portal_02", "repair_return_gate");
        Assert.AreEqual(PortalLook.Glow, Route(day, 1).Look, "an open departure portal glows");
        Assert.AreEqual(PortalLook.Empty, Route(day, 2).Look, "a CLOSED portal shows nothing inside its ring");
        Assert.AreEqual(PortalLook.ReturnGate, Route(day, 3).Look, "the Return Gate in service has its own effect");
        Assert.AreEqual(PortalLook.Dimmed, Route(day, 4).Look, "under maintenance: dimmed");
        Assert.AreEqual(PortalLook.Dimmed, Route(Day(2), 3).Look, "the Return Gate under maintenance is dimmed too");
    }

    // --- Departures (PO4, SK3's seam) ---

    [Test]
    public void DepartureFor_TheDisplacedByTheReturnGateInService()
    {
        Assert.AreEqual(3, Day(5, "repair_return_gate").DepartureFor(Babylon, true));
    }

    [Test]
    public void DepartureFor_TheDisplacedByPortal01BeforeTheRepair()
    {
        Assert.AreEqual(1, Day(5).DepartureFor(Babylon, true));
        Assert.AreEqual(1, Day(5, "repair_portal_02").DepartureFor(Thebes, true), "whatever their home");
    }

    [Test]
    public void DepartureFor_ACitizenByTheOpenRoutesPortal()
    {
        PortalDay day = Day(2, "repair_portal_04", "repair_portal_05");
        Assert.AreEqual(4, day.DepartureFor(Florence, false));
        Assert.AreEqual(5, day.DepartureFor(Wessex, false));
        Assert.AreEqual(1, day.DepartureFor(Athens, false));
    }

    [Test]
    public void DepartureFor_ACitizenByPortal01WhenNoOpenRouteServesThem()
    {
        Assert.AreEqual(1, Day(2).DepartureFor(Babylon, false), "no portal runs Babylonia today");
        Assert.AreEqual(1, Day(2).DepartureFor(Florence, false), "04 runs Florence but is under maintenance");
    }

    [Test]
    public void DepartureFor_NeverThroughAClosedRoute()
    {
        Assert.AreEqual(1, Day(2, "repair_portal_02").DepartureFor(Thebes, false), "02 runs Thebes, closed today");
    }

    [Test]
    public void DepartureFor_NothingWithoutADefaultPortal()
    {
        var portals = new List<PortalSpec> { Portal(2, PortalRole.Departures, 0, "repair_portal_02") };
        PortalDay day = PortalSchedule.Resolve(portals, 1, Owning(), DayTwo(), ClosesThebes);
        Assert.AreEqual(0, day.Default);
        Assert.AreEqual(0, day.DepartureFor(Athens, false), "no portal: no pulse (the content check refuses such a hall)");
        Assert.AreEqual(0, PortalDay.None.DepartureFor(Athens, true));
        Assert.AreEqual(0, PortalDay.None.Portals.Count);
    }

    // --- The content checks ---

    private static List<string> PortalProblems(List<PortalSpec> portals) =>
        PortalSchedule.PortalProblems(portals, id => id != null && id.StartsWith("repair_"));

    [Test]
    public void PortalProblems_TheHallIsSound()
    {
        CollectionAssert.IsEmpty(PortalProblems(Hall()));
    }

    [Test]
    public void PortalProblems_NumbersUnique()
    {
        List<PortalSpec> portals = Hall();
        portals[3].number = 2;
        StringAssert.Contains("2", string.Join("\n", PortalProblems(portals)));
        Assert.AreEqual(1, PortalProblems(portals).Count(p => p.Contains("twice")));

        portals = Hall();
        portals[4].number = 0;
        Assert.AreEqual(1, PortalProblems(portals).Count(p => p.Contains("positive")));
    }

    [Test]
    public void PortalProblems_NeverInServiceIsAnError()
    {
        List<PortalSpec> portals = Hall();
        portals[3].repair = "";
        Assert.AreEqual(1, PortalProblems(portals).Count(p => p.Contains("never")));
        portals[3].fromDay = -2;
        Assert.IsTrue(PortalProblems(portals).Any(p => p.Contains("fromDay")));
    }

    [Test]
    public void PortalProblems_AnUnknownRepairIsAnError()
    {
        List<PortalSpec> portals = Hall();
        portals[1].repair = "fix_02";
        Assert.AreEqual(1, PortalProblems(portals).Count(p => p.Contains("fix_02")));

        portals = Hall();
        portals[4].repair = "repair_portal_04";
        Assert.AreEqual(1, PortalProblems(portals).Count(p => p.Contains("repair_portal_04")), "one repair puts one portal in service");
    }

    [Test]
    public void PortalProblems_AtMostOneReturnsPortal()
    {
        List<PortalSpec> portals = Hall();
        portals[4].role = PortalRole.Returns;
        Assert.AreEqual(1, PortalProblems(portals).Count(p => p.Contains("Returns")));
    }

    [Test]
    public void PortalProblems_ADeparturePortalInServiceOnDayOne()
    {
        List<PortalSpec> portals = Hall();
        portals[0].fromDay = 2;
        Assert.AreEqual(1, PortalProblems(portals).Count(p => p.Contains("day 1")));
        portals[0].fromDay = 1;
        portals[0].role = PortalRole.Returns;
        portals[2].role = PortalRole.Departures;
        Assert.AreEqual(1, PortalProblems(portals).Count(p => p.Contains("day 1")), "the one in service on day 1 must run departures");
    }

    [Test]
    public void PortalProblems_ABlankNameIsAnError()
    {
        List<PortalSpec> portals = Hall();
        portals[2].name = " ";
        Assert.AreEqual(1, PortalProblems(portals).Count(p => p.Contains("name")));
    }

    private static readonly HashSet<PlaceRef> World = new HashSet<PlaceRef> { Athens, Thebes, Florence, Wessex, Babylon };

    private static List<string> DayProblems(List<PortalRequest> routes) =>
        PortalSchedule.DayProblems("days[2]", Hall(), routes, World.Contains, p => p.Equals(Thebes));

    [Test]
    public void DayProblems_TheDaySoundWithAClosedRouteOnARepairPortal()
    {
        CollectionAssert.IsEmpty(DayProblems(DayTwo()), "02's route is closed today on purpose: allowed");
    }

    [Test]
    public void DayProblems_EachDeparturePortalNeedsOneRoute()
    {
        List<PortalRequest> routes = DayTwo();
        routes.RemoveAt(3);
        Assert.AreEqual(1, DayProblems(routes).Count(p => p.Contains("portal 5") && p.Contains("no route")));

        routes = DayTwo();
        routes.Add(new PortalRequest(4, Babylon));
        Assert.AreEqual(1, DayProblems(routes).Count(p => p.Contains("portal 4") && p.Contains("twice")));
    }

    [Test]
    public void DayProblems_ARouteOutsideTheWorldIsAnError()
    {
        List<PortalRequest> routes = DayTwo();
        routes[2] = new PortalRequest(4, new PlaceRef("japan", "modern"));
        Assert.AreEqual(1, DayProblems(routes).Count(p => p.Contains("japan:modern")));
    }

    [Test]
    public void DayProblems_ADuplicateRouteIsAnError()
    {
        List<PortalRequest> routes = DayTwo();
        routes[3] = new PortalRequest(5, Florence);
        Assert.AreEqual(1, DayProblems(routes).Count(p => p.Contains("italy:medieval")));
    }

    [Test]
    public void DayProblems_TheReturnsPortalTakesNoRoute()
    {
        List<PortalRequest> routes = DayTwo();
        routes.Add(new PortalRequest(3, Babylon));
        Assert.AreEqual(1, DayProblems(routes).Count(p => p.Contains("portal 3")));
    }

    [Test]
    public void DayProblems_AnUnknownPortalIsAnError()
    {
        List<PortalRequest> routes = DayTwo();
        routes.Add(new PortalRequest(7, Babylon));
        Assert.AreEqual(1, DayProblems(routes).Count(p => p.Contains("portal 7")));
    }

    [Test]
    public void DayProblems_Portal01sRouteMustBeOpen()
    {
        List<PortalRequest> routes = DayTwo();
        routes[0] = new PortalRequest(1, Thebes);
        routes[1] = new PortalRequest(2, Athens);
        Assert.AreEqual(1, DayProblems(routes).Count(p => p.Contains("portal 1") && p.Contains("closed")));
    }
}
