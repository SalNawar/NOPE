using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The desk's swept boxes (desk v3; UNITY_PLAN 5.3, DESK-HOMES section 6's
/// precondition; the code review's round 3, findings 5 and 7): every shown
/// home is an obstacle at its objects' height at rest (a lift raised when
/// they are measured counted once), the homes something slides or lifts in
/// keep their swept box (a track's footprint to its overshoot peak toward the
/// mat, a lift's top raised by its peak), a carried paper over a machine's
/// strip keeps over its box, and a paper let go has each track's strip to
/// keep clear of. On DESK-PLAN's approved map and the approved slides.
/// </summary>
public class DeskSweepsTests
{
    private static readonly DeskHome[] Map = DeskHomeMap.Approved();

    private static DeskHome Home(string id) => DeskHomeMap.Find(Map, id);

    /// <summary>A refresh with the scanner, the chip reader, the UV torch and DETAIN shown at these heights at rest.</summary>
    private static (DeskSweeps sweeps, List<(string id, DeskRect footprint, float height)> obstacles) Refreshed(DeskSlides slides)
    {
        var sweeps = new DeskSweeps();
        var obstacles = new List<(string, DeskRect, float)>();
        sweeps.Refresh(slides, new List<(DeskHome, float)>
        {
            (Home(DeskHomeMap.Scanner), 0.12f), (Home(DeskHomeMap.ChipReader), 0.07f), (Home(DeskHomeMap.UvTorch), 0.05f), (Home(DeskHomeMap.Detain), 0.09f),
        }, obstacles);
        return (sweeps, obstacles);
    }

    [Test]
    public void ARestHeight_IsTheTallestPartAtRest_ALiftsRiseTakenOff_ElseTheHomesOwnHeight()
    {
        // The code review (round 3, 7): measured while a lift was raised (a crate's pop), its home's height counted the lift twice.
        Assert.AreEqual(0.195f, DeskSweeps.RestHeight(new List<(float, float)> { (0.21f, 0.015f), (0.18f, 0f) }, 0.05f), 1e-6f, "the raised part counted at its rest");
        Assert.AreEqual(0.21f, DeskSweeps.RestHeight(new List<(float, float)> { (0.21f, 0f), (0.18f, 0f) }, 0.05f), 1e-6f);
        Assert.AreEqual(0.05f, DeskSweeps.RestHeight(new List<(float, float)>(), 0.05f), 1e-6f, "nothing drawn: the home's own height");
        Assert.AreEqual(0.05f, DeskSweeps.RestHeight(null, 0.05f), 1e-6f);
    }

    [Test]
    public void EveryShownHome_IsAnObstacle_AndTheHomesSomethingMovesIn_KeepTheirSweptBox()
    {
        DeskSlides slides = DeskSlides.Approved();
        (DeskSweeps sweeps, List<(string id, DeskRect footprint, float height)> obstacles) = Refreshed(slides);
        CollectionAssert.AreEqual(new[] { DeskHomeMap.Scanner, DeskHomeMap.ChipReader, DeskHomeMap.UvTorch, DeskHomeMap.Detain }, obstacles.Select(o => o.id), "every shown home, in order");
        foreach ((string id, DeskRect footprint, float height) in obstacles)
        {
            (DeskRect expected, float top) = slides.Obstacle(Home(id), id == DeskHomeMap.Scanner ? 0.12f : id == DeskHomeMap.ChipReader ? 0.07f : id == DeskHomeMap.UvTorch ? 0.05f : 0.09f);
            Assert.AreEqual(expected.Width, footprint.Width, 1e-6f, id);
            Assert.AreEqual(expected.CentreX, footprint.CentreX, 1e-6f, id);
            Assert.AreEqual(top, height, 1e-6f, id);
        }
        Assert.AreEqual(3, sweeps.Count, "the scanner, the chip reader and the torch move; DETAIN never does");

        sweeps.Refresh(slides, new List<(DeskHome, float)> { (Home(DeskHomeMap.Detain), 0.09f) }, obstacles);
        Assert.AreEqual(0, sweeps.Count, "a refresh replaces the last one's boxes");
        Assert.AreEqual(1, obstacles.Count);
    }

    [Test]
    public void ACarriedPaper_OverAMachinesStrip_KeepsOverItsBoxAtThePeak_EvenBeyondItsRestFootprint()
    {
        DeskSlides slides = DeskSlides.Approved();
        (DeskSweeps sweeps, _) = Refreshed(slides);
        DeskRect scanner = Home(DeskHomeMap.Scanner).Footprint;
        DeskSlider s = slides.Find(DeskHomeMap.Scanner);
        float way = s.Way(Home(DeskHomeMap.Scanner)), reach = System.Math.Min(s.travel * s.Peak, s.Reach(slides.feltGap));
        Assert.AreEqual(-1f, way, "the scanner slides toward the mat, to its left");
        float beyond = scanner.CentreX - scanner.Width / 2f - reach / 2f;
        Assert.IsTrue(sweeps.TryTop(beyond, scanner.CentreY, 0f, out float top), "over the strip the scanner slides into, outside its rest footprint");
        Assert.AreEqual(0.12f, top, 1e-6f, "its box's height");
        Assert.IsFalse(sweeps.TryTop(scanner.CentreX - scanner.Width / 2f - reach - 0.05f, scanner.CentreY, 0f, out _), "past its peak: nothing");
        Assert.IsTrue(sweeps.TryTop(scanner.CentreX - scanner.Width / 2f - reach - 0.05f, scanner.CentreY, 0.06f, out _), "a paper whose corner reaches it");

        DeskRect torch = Home(DeskHomeMap.UvTorch).Footprint;
        DeskSlider t = slides.Find(DeskHomeMap.UvTorch);
        Assert.IsTrue(sweeps.TryTop(torch.CentreX, torch.CentreY, 0f, out float lifted));
        Assert.AreEqual(0.05f + System.Math.Min(t.travel * t.Peak, t.Reach(slides.feltGap)), lifted, 1e-6f, "a lift's box is raised by its peak");
    }

    [Test]
    public void APaperLetGo_HasEachTracksSweptStrip_ToKeepClearOf_NeverALifts()
    {
        DeskSlides slides = DeskSlides.Approved();
        (DeskSweeps sweeps, _) = Refreshed(slides);
        var strips = new List<DeskRect>();
        sweeps.Strips(strips);
        Assert.AreEqual(2, strips.Count, "the scanner's and the chip reader's (the torch rises in place, DETAIN never moves)");
        foreach ((DeskRect strip, string id) in strips.Zip(new[] { DeskHomeMap.Scanner, DeskHomeMap.ChipReader }, (a, b) => (a, b)))
        {
            DeskRect rest = Home(id).Footprint;
            Assert.Greater(strip.Width, rest.Width, id + ": wider than its home");
            Assert.Less(strip.CentreX - strip.Width / 2f, rest.CentreX - rest.Width / 2f - 1e-4f, id + ": reaching past it toward the mat");
            Assert.AreEqual(rest.CentreX + rest.Width / 2f, strip.CentreX + strip.Width / 2f, 1e-5f, id + ": never behind it");
        }
        sweeps.Clear();
        strips.Clear();
        sweeps.Strips(strips);
        Assert.IsEmpty(strips, "unbound: none");
    }
}
