using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The desk's plan (the desk layout spec; the desk home map Saleh approved on
/// 2026-10-20): yaw-aware footprints, the home map's gate (one home per id,
/// none on the mat, apart, clear of the fixed art, clean slide sweeps, seen
/// from the office camera, every two shown objects' click boxes apart; the
/// real boxes judged by ClickBoxClashes: BUG-4a), the approved map itself, a decoration starting
/// in the decor zone and resting where it is let go off the mat (nudged a few
/// centimetres at most), and the saved places.
/// </summary>
public class DeskArrangementTests
{
    private static readonly DeskRect Zone = new DeskRect(0f, 0f, 0.6f, 0.4f);

    [Test]
    public void ADecoration_LetGoInsideAZone_StaysWhereItWasLetGo()
    {
        Assert.IsTrue(DeskArrangement.TrySettle(0.1f, 0.1f, 0.05f, 0.02f, new[] { Zone }, new DeskRect[0], 0.01f, out float x, out float y));
        Assert.AreEqual(0.05f, x, 1e-4f);
        Assert.AreEqual(0.02f, y, 1e-4f);
    }

    [Test]
    public void ADecoration_LetGoOutside_SettlesOnTheZonesEdge_WhollyInside()
    {
        Assert.IsTrue(DeskArrangement.TrySettle(0.1f, 0.1f, 2f, 0f, new[] { Zone }, new DeskRect[0], 0.01f, out float x, out float y));
        Assert.AreEqual(0.25f, x, 1e-4f, "its right edge on the zone's");
        Assert.IsTrue(DeskArrangement.Holds(Zone, new DeskRect(x, y, 0.1f, 0.1f)));
    }

    [Test]
    public void ADecoration_NeverLandsOnAnother()
    {
        var other = new DeskRect(0f, 0f, 0.1f, 0.1f);
        Assert.IsTrue(DeskArrangement.TrySettle(0.1f, 0.1f, 0f, 0f, new[] { Zone }, new[] { other }, 0.01f, out float x, out float y));
        Assert.IsFalse(DeskArrangement.Overlap(other, new DeskRect(x, y, 0.1f, 0.1f)));
        Assert.AreEqual(0.1f, Math.Max(Math.Abs(x), Math.Abs(y)), 0.011f, "right beside it");
    }

    [Test]
    public void ADecoration_TakesTheNearestZone()
    {
        var far = new DeskRect(5f, 0f, 0.6f, 0.4f);
        Assert.IsTrue(DeskArrangement.TrySettle(0.1f, 0.1f, 4.5f, 0f, new[] { Zone, far }, new DeskRect[0], 0.01f, out float x, out _));
        Assert.Greater(x, 4f);
    }

    [Test]
    public void NoRoom_IsNowhere()
    {
        Assert.IsFalse(DeskArrangement.TrySettle(1f, 1f, 0f, 0f, new[] { Zone }, new DeskRect[0], 0.01f, out _, out _), "too big for the zone");
        Assert.IsFalse(DeskArrangement.TrySettle(0.1f, 0.1f, 0f, 0f, new[] { Zone }, new[] { Zone }, 0.01f, out _, out _), "the zone is full");
    }

    [Test]
    public void AFootprint_IsYawAware()
    {
        DeskRect straight = DeskArrangement.Footprint(0f, 0f, 0.32f, 0.12f, 0f);
        DeskRect turned = DeskArrangement.Footprint(0f, 0f, 0.32f, 0.12f, 90f);
        Assert.AreEqual(0.32f, straight.Width, 1e-4f);
        Assert.AreEqual(0.12f, turned.Width, 1e-4f, "turned a quarter, its depth lies across");
        Assert.AreEqual(0.32f, turned.Height, 1e-4f);
        DeskRect slanted = DeskArrangement.Footprint(0f, 0f, 1f, 0f, 45f);
        Assert.AreEqual(Math.Sqrt(0.5), slanted.Width, 1e-4f, "a slanted object covers its bounding box");
    }

    [Test]
    public void HomeProblems_NameEveryKindOfFault()
    {
        var desk = new DeskRect(0f, 0f, 4f, 2f);
        var mat = new DeskRect(0f, 0f, 1f, 0.6f);
        var art = new[] { new DeskHome("pc", -1.5f, 0f, 0.4f, 0.4f, 0f, 0.5f, HomeShow.Always) };
        var homes = new[]
        {
            new DeskHome("onMat", 0f, 0f, 0.1f, 0.1f, 0f, 0.05f, HomeShow.Always),
            new DeskHome("offDesk", 1.95f, 0f, 0.2f, 0.2f, 0f, 0.05f, HomeShow.Always),
            new DeskHome("onArt", -1.5f, 0f, 0.1f, 0.1f, 0f, 0.05f, HomeShow.Always),
            new DeskHome("slider", 0.56f, 0f, 0.1f, 0.1f, 0f, 0.05f, HomeShow.Always, HomeSlide.Left),
            new DeskHome("twin", 1.2f, 0.5f, 0.1f, 0.1f, 0f, 0.05f, HomeShow.Always),
            new DeskHome("twin", 1.2f, -0.5f, 0.1f, 0.1f, 0f, 0.05f, HomeShow.Always),
            new DeskHome("hidden", 1.6f, 0.9f, 0.1f, 0.1f, 0f, 0.05f, HomeShow.Later),
            new DeskHome("wall", 0f, 0.8f, 0.6f, 0.05f, 0f, 0.9f, HomeShow.Always),
            new DeskHome("behind", 0f, 0.95f, 0.1f, 0.05f, 0f, 0.05f, HomeShow.Always),
        };
        string all = string.Join("\n", DeskArrangement.HomeProblems(homes, art, mat, desk, 0.06f, (0f, -3f, 1f), 0.9f));
        StringAssert.Contains("'onMat' lies on the paper mat", all);
        StringAssert.Contains("'offDesk' runs off the desk", all);
        StringAssert.Contains("'onArt' lies on the fixed art 'pc'", all);
        StringAssert.Contains("'slider' slides onto the paper mat", all);
        StringAssert.Contains("'twin' has two homes", all);
        StringAssert.Contains("sees 0% of 'behind'", all);
        StringAssert.DoesNotContain("'hidden'", all, "a home that shows nothing yet is not looked for");
    }

    [Test]
    public void TheSightCheck_SeesOverLowThings_AndNotThroughTallOnes()
    {
        var target = new DeskHome("target", 0f, 1f, 0.2f, 0.2f, 0f, 0.05f, HomeShow.Always);
        var low = new DeskHome("low", 0f, 0.5f, 0.4f, 0.2f, 0f, 0.02f, HomeShow.Always);
        var wall = new DeskHome("wall", 0f, 0.7f, 0.6f, 0.1f, 0f, 0.8f, HomeShow.Always);
        (float x, float y, float h) eye = (0f, -2f, 1f);
        Assert.AreEqual(1f, DeskArrangement.VisibleShare(target, eye, new[] { target, low }), 1e-4f);
        Assert.AreEqual(0f, DeskArrangement.VisibleShare(target, eye, new[] { target, low, wall }), 1e-4f);
        Assert.IsFalse(DeskArrangement.SightBlocked(eye, (0f, 1f, 0.05f), target, new[] { target }), "an object never hides itself");
    }

    [Test]
    public void ADecoration_LetGoOnAFreeSpot_RestsExactlyThere()
    {
        var desk = new DeskRect(0f, 0f, 4f, 2f);
        var blocked = new[] { new DeskRect(0f, 0f, 1f, 0.6f) };
        Assert.IsTrue(DeskArrangement.TrySettleFree(0.1f, 0.1f, 1.2f, 0.3f, desk, blocked, 0.03f, 0.005f, out float x, out float y));
        Assert.AreEqual(1.2f, x, 1e-4f);
        Assert.AreEqual(0.3f, y, 1e-4f);
    }

    [Test]
    public void ADecoration_LetGoJustOverAnEdge_IsNudgedAFewCentimetresAtMost()
    {
        var desk = new DeskRect(0f, 0f, 4f, 2f);
        var mat = new DeskRect(0f, 0f, 1f, 0.6f);
        Assert.IsTrue(DeskArrangement.TrySettleFree(0.1f, 0.1f, 0.53f, 0f, desk, new[] { mat }, 0.03f, 0.005f, out float x, out float y), "2 cm onto the mat");
        Assert.IsFalse(DeskArrangement.Overlap(mat, new DeskRect(x, y, 0.1f, 0.1f)));
        Assert.LessOrEqual(Math.Sqrt((x - 0.53f) * (x - 0.53f) + y * y), 0.0301);
        Assert.IsFalse(DeskArrangement.TrySettleFree(0.1f, 0.1f, 0.2f, 0f, desk, new[] { mat }, 0.03f, 0.005f, out _, out _), "deep on the mat: it goes back");
        Assert.IsFalse(DeskArrangement.TrySettleFree(0.1f, 0.1f, 1.99f, 0f, desk, new DeskRect[0], 0.03f, 0.005f, out _, out _), "over the desk's edge: it goes back");
    }

    // -----------------------------
    // The approved home map (DeskHomeMap; Saleh's DESK APPROVED, 2026-10-20)
    // -----------------------------

    private static readonly string[] EveryId =
    {
        DeskHomeMap.StampRack, DeskHomeMap.Scanner, DeskHomeMap.ChipReader, DeskHomeMap.DealTray, DeskHomeMap.Bell, DeskHomeMap.Detain,
        DeskHomeMap.Intercom, DeskHomeMap.BabelTuner, DeskHomeMap.RulebookPocket, DeskHomeMap.UvTorch, DeskHomeMap.Rolodex, DeskHomeMap.Glasses,
        DeskHomeMap.ProtocolRing, DeskHomeMap.Switchboard, DeskHomeMap.Thumbprint, DeskHomeMap.TransponderTuner, DeskHomeMap.BoothCamera,
        DeskHomeMap.Xray, DeskHomeMap.Crate, DeskHomeMap.Decor, DeskHomeMap.Mouse,
    };

    [Test]
    public void TheApprovedMap_GivesEveryObjectExactlyOneHome()
    {
        CollectionAssert.AreEquivalent(EveryId, DeskHomeMap.Approved().Select(h => h.id), "kept, locked, later and Phase B objects: one home each");
    }

    [Test]
    public void TheApprovedMap_HasNoProblem_OffTheMat_ApartAndSeen()
    {
        List<string> problems = DeskArrangement.HomeProblems(DeskHomeMap.Approved(), DeskHomeMap.FixedArt(), DeskHomeMap.Mat, DeskHomeMap.DeskTop,
                                                             DeskHomeMap.SlideReach, DeskHomeMap.OfficeEye, 0.9f, DeskSlides.Approved().TrackTravel());
        CollectionAssert.IsEmpty(problems, string.Join("\n", problems));
    }

    [Test]
    public void TheApprovedMap_KeepsEveryTwoShownObjectsClickBoxesApart()
    {
        List<string> problems = DeskArrangement.ClickBoxProblems(DeskHomeMap.Approved(), DeskHomeMap.FixedArt(), DeskHomeMap.ClickPad, DeskHomeMap.NoClickBox);
        CollectionAssert.IsEmpty(problems, string.Join("\n", problems));
    }

    [Test]
    public void ClickBoxProblems_GrowBuiltBoxesByThePad_NotTheArts()
    {
        // Two built objects 1.5 cm apart: their 1 cm pads overlap.
        var built = new[]
        {
            new DeskHome("a", 0f, 0f, 0.10f, 0.10f, 0f, 0.1f, HomeShow.Always),
            new DeskHome("b", 0.115f, 0f, 0.10f, 0.10f, 0f, 0.1f, HomeShow.WhenOwned),
        };
        StringAssert.Contains("'a' and 'b' overlap", string.Join("\n", DeskArrangement.ClickBoxProblems(built, null, 0.01f, null)));
        // A built object 1.5 cm from the fixed art (the art's box is its bounds, no pad): apart.
        var nearArt = new[] { new DeskHome("a", 0f, 0f, 0.10f, 0.10f, 0f, 0.1f, HomeShow.Always) };
        var art = new[] { new DeskHome("pc", 0.115f, 0f, 0.10f, 0.10f, 0f, 0.6f, HomeShow.Always) };
        CollectionAssert.IsEmpty(DeskArrangement.ClickBoxProblems(nearArt, art, 0.01f, null));
        // ... but 0.5 cm from it: the built box reaches into the art's.
        var touching = new[] { new DeskHome("a", 0f, 0f, 0.10f, 0.10f, 0f, 0.1f, HomeShow.Always) };
        var close = new[] { new DeskHome("pc", 0.105f, 0f, 0.10f, 0.10f, 0f, 0.6f, HomeShow.Always) };
        StringAssert.Contains("'a' and 'pc' overlap", string.Join("\n", DeskArrangement.ClickBoxProblems(touching, close, 0.01f, null)));
        // A moved art object (artParts) keeps the art's bounds, and a home that shows nothing has no box.
        var moved = new[]
        {
            new DeskHome("phone", 0f, 0f, 0.10f, 0.10f, 0f, 0.1f, HomeShow.Always, HomeSlide.None, "Clean_Phone"),
            new DeskHome("mouse", 0.115f, 0f, 0.10f, 0.10f, 0f, 0.1f, HomeShow.Always, HomeSlide.None, "Clean_Mouse"),
            new DeskHome("bell", -0.115f, 0f, 0.10f, 0.10f, 0f, 0.1f, HomeShow.Reserved),
        };
        CollectionAssert.IsEmpty(DeskArrangement.ClickBoxProblems(moved, null, 0.01f, null));
        // A shown object that takes no click (the deal tray's plate) has no box to keep apart.
        CollectionAssert.IsEmpty(DeskArrangement.ClickBoxProblems(touching, close, 0.01f, new[] { "a" }));
    }

    [Test]
    public void ClickBoxProblems_LeaveFootprintOverlapsToHomeProblems()
    {
        var overlapping = new[]
        {
            new DeskHome("a", 0f, 0f, 0.10f, 0.10f, 0f, 0.1f, HomeShow.Always),
            new DeskHome("b", 0.05f, 0f, 0.10f, 0.10f, 0f, 0.1f, HomeShow.Always),
        };
        CollectionAssert.IsEmpty(DeskArrangement.ClickBoxProblems(overlapping, null, 0.01f, null), "one line per fault: HomeProblems names it");
        StringAssert.Contains("'a' and 'b' overlap", string.Join("\n", DeskArrangement.HomeProblems(overlapping, null, new DeskRect(0f, -5f, 1f, 1f), new DeskRect(0f, 0f, 4f, 4f), 0.06f, (0f, -3f, 1f), 0f)));
    }

    [Test]
    public void ClickBoxClashes_NameEveryPairOfDifferentObjectsDeeperThanTheTolerance()
    {
        var names = new[] { "Rolodex/Click", "Intercom", "RingPanel/Breaker1Click", "RingPanel/Breaker2Click", "Lamp" };
        var owners = new[] { "rolodex", "intercom", "switchboard", "switchboard", null };
        float Depth(int i, int j)
        {
            if (i == 0 && j == 1)
                return 0.012f; // the Rolodex's box 12 mm into the intercom's
            if (i == 2 && j == 3)
                return 0.02f; // two breakers of one panel: never counted
            if (i == 1 && j == 4)
                return 0.004f; // within the tolerance
            return 0f;
        }
        List<string> clashes = DeskArrangement.ClickBoxClashes(names, owners, Depth, 0.005f);
        Assert.AreEqual(1, clashes.Count, string.Join("\n", clashes));
        StringAssert.Contains("'Rolodex/Click' and 'Intercom' overlap by 12 mm", clashes[0]);
    }

    [Test]
    public void ClickBoxClashes_ABoxWithNoOwner_IsItsOwnObject()
    {
        List<string> clashes = DeskArrangement.ClickBoxClashes(new[] { "Lamp", "Till" }, new string[] { null, null }, (i, j) => 0.03f, 0.005f);
        Assert.AreEqual(1, clashes.Count);
        CollectionAssert.IsEmpty(DeskArrangement.ClickBoxClashes(null, null, (i, j) => 1f, 0.005f));
    }

    [Test]
    public void TheMat_IsTheBigArtMat_PapersOnly()
    {
        Assert.AreEqual(1.46f, DeskHomeMap.Mat.Width, 1e-4f, "Saleh: keep a big mat size");
        Assert.AreEqual(1.03f, DeskHomeMap.Mat.Height, 1e-4f);
        foreach (DeskHome home in DeskHomeMap.Approved())
            Assert.IsFalse(DeskArrangement.Overlap(home.Footprint, DeskHomeMap.Mat), home.id);
    }

    [Test]
    public void TheRail_RunsRightOfTheMat_InTheOrderOfUse_FrontToBack_EndingByTheDealTray()
    {
        DeskHome[] homes = DeskHomeMap.Approved();
        float last = float.MinValue;
        foreach (string id in DeskHomeMap.Rail)
        {
            DeskRect f = DeskHomeMap.Find(homes, id).Footprint;
            Assert.Greater(f.CentreX - f.Width / 2f, DeskHomeMap.Mat.Width / 2f, id + " stands right of the mat");
            Assert.Greater(f.CentreY, last, id + " stands behind the one used before it");
            last = f.CentreY;
        }
        DeskRect rack = DeskHomeMap.Find(homes, DeskHomeMap.StampRack).Footprint;
        DeskRect tray = DeskHomeMap.Find(homes, DeskHomeMap.DealTray).Footprint;
        Assert.GreaterOrEqual(rack.CentreY + rack.Height / 2f, DeskHomeMap.Mat.Height / 2f - 0.01f, "the rack reaches the counter at the mat's far edge");
        Assert.AreEqual(0f, tray.CentreX, 1e-4f, "the deal tray sits right above the mat");
        Assert.AreEqual(DeskHomeMap.Mat.Height / 2f, tray.CentreY - tray.Height / 2f, 1e-3f, "flush with the counter's far edge");
    }

    [Test]
    public void TheStampRack_StandsWithItsDatersFrontToBack()
    {
        DeskRect rack = DeskHomeMap.Find(DeskHomeMap.Approved(), DeskHomeMap.StampRack).Footprint;
        Assert.Greater(rack.Height, rack.Width, "Saleh: daters front-to-back");
    }

    [Test]
    public void TheMachines_SlideSidewaysTowardTheMat_AndOnlyThey()
    {
        foreach (DeskHome home in DeskHomeMap.Approved())
        {
            bool machine = home.id == DeskHomeMap.Scanner || home.id == DeskHomeMap.ChipReader || home.id == DeskHomeMap.BabelTuner || home.id == DeskHomeMap.Intercom; // desk v3: the intercom slides too
            Assert.AreEqual(machine, home.slide != HomeSlide.None, home.id);
            if (!machine)
                continue;
            bool right = home.Footprint.CentreX > 0f;
            Assert.AreEqual(right ? HomeSlide.Left : HomeSlide.Right, home.slide, home.id + " slides toward the mat");
            Assert.AreEqual(home.Footprint.Width + DeskHomeMap.SlideReach, home.Swept(DeskHomeMap.SlideReach).Width, 1e-4f);
        }
    }

    [Test]
    public void ASweep_IsEachTracksOwnTravel_NeverPastTheReach()
    {
        var desk = new DeskRect(0f, 0f, 4f, 2f);
        var mat = new DeskRect(0f, 0f, 1f, 0.6f);
        var homes = new[] { new DeskHome("near", -0.62f, 0f, 0.2f, 0.2f, 0f, 0.05f, HomeShow.Always, HomeSlide.Right) };
        StringAssert.Contains("'near' slides onto the paper mat", string.Join("\n", DeskArrangement.HomeProblems(homes, null, mat, desk, 0.06f, (0f, -3f, 1f), 0f)), "the 6 cm reach");
        var travel = new Dictionary<string, float> { ["near"] = 0.015f };
        CollectionAssert.IsEmpty(DeskArrangement.HomeProblems(homes, null, mat, desk, 0.06f, (0f, -3f, 1f), 0f, travel), "its own 1.5 cm stops short of the mat");
        travel["near"] = 0.08f;
        StringAssert.Contains("'near' slides 8.0 cm, further than 6.0 cm", string.Join("\n", DeskArrangement.HomeProblems(homes, null, mat, desk, 0.06f, (0f, -3f, 1f), 0f, travel)));
    }

    [Test]
    public void TheCountersRow_HoldsDetainAndTheBell_AndTheLeftHoldsTalkAndLookUp()
    {
        DeskHome[] homes = DeskHomeMap.Approved();
        float back = DeskHomeMap.Mat.Height / 2f;
        foreach (string id in new[] { DeskHomeMap.Detain, DeskHomeMap.Bell, DeskHomeMap.Rolodex, DeskHomeMap.Glasses, DeskHomeMap.ProtocolRing })
            Assert.GreaterOrEqual(DeskHomeMap.Find(homes, id).Footprint.CentreY, back, id + " stands on the counter's row");
        Assert.Less(DeskHomeMap.Find(homes, DeskHomeMap.Detain).x, 0f, "DETAIN left of the sign");
        Assert.Greater(DeskHomeMap.Find(homes, DeskHomeMap.Bell).x, 0f, "the bell right of it");
        foreach (string id in new[] { DeskHomeMap.Intercom, DeskHomeMap.BabelTuner, DeskHomeMap.RulebookPocket, DeskHomeMap.UvTorch })
            Assert.Less(DeskHomeMap.Find(homes, id).Footprint.CentreX, -DeskHomeMap.Mat.Width / 2f, id + " stands left of the mat");
    }

    [Test]
    public void ALampPoint_IsTheMiddleOfTheFootprintsFrontEdge()
    {
        var home = new DeskHome("x", 1f, 2f, 0.4f, 0.2f, 0f, 0.1f, HomeShow.Always);
        (float x, float y) lamp = home.LampPoint;
        Assert.AreEqual(1f, lamp.x, 1e-4f);
        Assert.AreEqual(1.9f, lamp.y, 1e-4f);
    }

    [Test]
    public void Places_RoundTrip_InTheSavedText()
    {
        var places = new Dictionary<string, (float x, float y)> { ["calculator"] = (-2.5f, 1.02f), ["pen_pot"] = (1.5f, -0.25f) };
        string text = DeskArrangement.FormatPlaces(places);
        Assert.AreEqual("calculator:-2.5,1.02;pen_pot:1.5,-0.25", text);
        CollectionAssert.AreEquivalent(places, DeskArrangement.ParsePlaces(text));
    }

    [Test]
    public void ADamagedSave_LosesOnlyTheBrokenEntries()
    {
        Dictionary<string, (float x, float y)> places = DeskArrangement.ParsePlaces("calculator:1,2;broken;pen_pot:x,1;:3,4;stapler:0.5,NaN;binder:0.25,-1");
        CollectionAssert.AreEquivalent(new[] { "calculator", "binder" }, places.Keys);
        Assert.AreEqual((0.25f, -1f), places["binder"]);
        Assert.IsEmpty(DeskArrangement.ParsePlaces(null));
    }
}
