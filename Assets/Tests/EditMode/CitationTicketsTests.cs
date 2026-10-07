using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The citation (TC-900, Saleh 2026-10-07): one per wrong decision, a row per
/// box of the papers that shows the fault, the one penalty on the first row,
/// four rows to a sheet with continuation sheets numbered on, the total on
/// the last sheet only; and the delivered paper's flight onto the desk.
/// </summary>
public class CitationTicketsTests
{
    private static readonly string[] Forms = { "TC-101", "TC-230" };

    private static readonly IReadOnlyList<string>[] Labels =
    {
        new[] { "Full Name", "Citizen ID", "Valid Until", "Issuing Seal" },
        new[] { "Citizen ID", "Departure" }
    };

    private static CitationTicket Ticket(int rows, bool warning = false)
    {
        var boxes = Enumerable.Range(0, rows).Select(i => (i % 2, i % 2 == 0 ? i % 4 : i % 2)).ToList();
        var t = new CitationTicket { Number = "C-02-0007", Date = "15 MAR 2150", Desk = "3", Clerk = "TMW-773", Penalty = warning ? 0 : 120, Warning = warning };
        t.Rows.AddRange(CitationTickets.Rows("Approved an expired paper.", "Directive 2: papers must be valid.", boxes, Forms, Labels,
                                             warning ? "WARNING" : "120 CR", "INCL."));
        return t;
    }

    [Test]
    public void ARow_PerBoxThatShowsTheFault_ReferringToItsPaperAndBox_ThePenaltyOnTheFirstOnly()
    {
        var rows = CitationTickets.Rows("Approved forged papers.", "", new[] { (0, 2), (1, 0) }, Forms, Labels, "120 CR", "INCL.");
        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("TC-101 · VALID UNTIL", rows[0].Ref);
        Assert.AreEqual("TC-230 · CITIZEN ID", rows[1].Ref);
        Assert.AreEqual("120 CR", rows[0].Penalty, "one penalty per wrong decision");
        Assert.AreEqual("INCL.", rows[1].Penalty);
    }

    [Test]
    public void ADecisionReferringToNoBox_IsOneRowWithoutAReference()
    {
        var rows = CitationTickets.Rows("Denied a legitimate, permitted traveler.", "Directive 1", new (int, int)[0], Forms, Labels, "WARNING", "INCL.");
        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(string.Empty, rows[0].Ref);
        Assert.AreEqual("WARNING", rows[0].Penalty);
    }

    [Test]
    public void FourRowsFitOneSheet_TheTotalAndTheFineBoxOnIt()
    {
        List<string[]> sheets = CitationTickets.Sheets(Ticket(4), "WARNING", "SEE NEXT SHEET");
        Assert.AreEqual(1, sheets.Count);
        string[] s = sheets[0];
        Assert.AreEqual(CitationTickets.FieldCount, s.Length);
        Assert.AreEqual("120 CR", s[(int)CitationTickets.Field.Total]);
        Assert.AreEqual("120 CR", s[(int)CitationTickets.Field.StubTotal]);
        Assert.AreEqual("C-02-0007", s[(int)CitationTickets.Field.StubNumber]);
        Assert.AreEqual(CitationTickets.Tick, s[(int)CitationTickets.Field.Fine]);
        Assert.AreEqual(string.Empty, s[(int)CitationTickets.Field.Warning]);
        Assert.AreEqual(string.Empty, s[(int)CitationTickets.Field.Docked]);
        Assert.AreEqual("4", s[CitationTickets.IndexOf(3, CitationTickets.RowField.Number)]);
    }

    [Test]
    public void SixViolations_TakeAContinuationSheet_NumberedOn_TheTotalOnTheLastOnly()
    {
        List<string[]> sheets = CitationTickets.Sheets(Ticket(6), "WARNING", "SEE NEXT SHEET");
        Assert.AreEqual(2, sheets.Count);
        Assert.AreEqual("SEE NEXT SHEET", sheets[0][(int)CitationTickets.Field.Total]);
        Assert.AreEqual("SEE NEXT SHEET", sheets[0][(int)CitationTickets.Field.StubTotal]);
        Assert.AreEqual("120 CR", sheets[1][(int)CitationTickets.Field.Total]);
        Assert.AreEqual("C-02-0007 · CONT. 2/2", sheets[1][(int)CitationTickets.Field.StubNumber]);
        Assert.AreEqual("5", sheets[1][CitationTickets.IndexOf(0, CitationTickets.RowField.Number)]);
        Assert.AreEqual("6", sheets[1][CitationTickets.IndexOf(1, CitationTickets.RowField.Number)]);
        Assert.AreEqual(string.Empty, sheets[1][CitationTickets.IndexOf(2, CitationTickets.RowField.Violation)], "rows past the violations stay blank");
        Assert.AreEqual("Approved an expired paper.", sheets[1][CitationTickets.IndexOf(1, CitationTickets.RowField.Violation)]);
    }

    [Test]
    public void AFreeWarning_TicksTheWarningBox_AndCostsNothing()
    {
        string[] s = CitationTickets.Sheets(Ticket(1, warning: true), "WARNING · NO DEDUCTION", "SEE NEXT SHEET")[0];
        Assert.AreEqual(CitationTickets.Tick, s[(int)CitationTickets.Field.Warning]);
        Assert.AreEqual(string.Empty, s[(int)CitationTickets.Field.Fine]);
        Assert.AreEqual("WARNING · NO DEDUCTION", s[(int)CitationTickets.Field.Total]);
    }

    [Test]
    public void TheNumber_IsTheDayAndTheRunsCount()
    {
        Assert.AreEqual("C-03-0012", CitationTickets.Number(3, 12));
    }

    [Test]
    public void TheFlight_StartsTwistedAndAloft_OvershootsOnce_AndEndsExactlyOnItsSpot()
    {
        FlightPose start = PaperFlight.At(0f, 25f, 30f, false);
        Assert.AreEqual(0f, start.Along, 1e-4f);
        Assert.AreEqual(25f, start.Twist, 1e-3f);
        Assert.IsFalse(start.Landed);

        float most = 0f;
        for (int i = 0; i <= 100; i++)
            most = System.Math.Max(most, PaperFlight.At(i / 100f, 25f, 30f, false).Along);
        Assert.Greater(most, 1.02f, "a springy overshoot");
        Assert.Less(most, 1.2f);
        Assert.Greater(PaperFlight.At(PaperFlight.LandAt * 0.5f, 25f, 30f, false).Lift, 0.9f, "high mid-arc");

        FlightPose end = PaperFlight.At(1f, 25f, 30f, false);
        Assert.AreEqual(1f, end.Along, 1e-4f);
        Assert.AreEqual(0f, end.Twist, 1e-3f);
        Assert.AreEqual(0f, end.Tumble, 1e-3f);
        Assert.AreEqual(0f, end.Lift, 1e-4f);
        Assert.AreEqual(1f, end.Squash, 1e-4f);
        Assert.AreEqual(1f, end.Shadow, 1e-4f);
        Assert.IsTrue(end.Landed);
        Assert.Less(PaperFlight.At(PaperFlight.LandAt + 0.1f, 25f, 30f, false).Squash, 1f, "a squash as it lands");
    }

    [Test]
    public void TheFlight_UnderReducedMotion_IsAShortStraightSlide()
    {
        Assert.Less(PaperFlight.Length(true), PaperFlight.Length(false));
        for (int i = 0; i <= 10; i++)
        {
            FlightPose p = PaperFlight.At(i / 10f, 25f, 30f, true);
            Assert.AreEqual(0f, p.Twist);
            Assert.AreEqual(0f, p.Tumble);
            Assert.AreEqual(0f, p.Lift);
            Assert.LessOrEqual(p.Along, 1f);
        }
    }
}
