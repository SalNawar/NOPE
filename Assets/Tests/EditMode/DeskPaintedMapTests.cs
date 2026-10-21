using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The painted desk's map (desk v3, Saleh: "yes but they need to slide";
/// DeskPaintedMap): its homes are desk v3's layout (the importer's copy of
/// desk_layout_v3.json, written by the pipeline's export_layout.py from
/// layout.py), every baked object stands in its home there (anchors.json's
/// pivots), merged over DESK-PLAN's map they pass the desk plan's gate but the
/// box sight check (DeskLayoutSettings.PaintedProblems; desk v3's sight was
/// gated on its real render), every slider moves on its painted home and each
/// track slides toward the mat, a gameplay object keeps its classic turn, and
/// the merge drops and moves the classic homes desk v3 has no room for.
/// </summary>
public class DeskPaintedMapTests
{
    private const string Data = "Assets/Art/Office/DeskPainted/Data/";

    /// <summary>A file of the project, from the tests' working folder or this file's.</summary>
    private static ContentNode Json(string relative, [CallerFilePath] string here = "") =>
        ContentJson.Parse(File.ReadAllText(File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative)));

    private static float Num(ContentNode node) => float.Parse(node.Text, CultureInfo.InvariantCulture);

    /// <summary>The home <paramref name="id"/>'s rectangle in desk v3's layout, or null.</summary>
    private static (float x0, float y0, float x1, float y1)? LayoutRect(string id)
    {
        ContentNode rect = Json(Data + "desk_layout_v3.json").Get("homes").Get(DeskPaintedMap.LayoutId(id))?.Get("rect");
        return rect != null ? (Num(rect.Items[0]), Num(rect.Items[1]), Num(rect.Items[2]), Num(rect.Items[3])) : ((float, float, float, float)?)null;
    }

    /// <summary>The painted homes and fixed art as the importer lays them out (the PC's mouse, measured on its mesh, left out; heights do not matter but to the box sight check).</summary>
    private static (DeskHome[] homes, DeskHome[] fixedArt) Painted() =>
        DeskPaintedMap.Homes(id => id == DeskPaintedMap.PcMouse ? null : LayoutRect(id), id => 0.05f, DeskHomeMap.Approved());

    [Test]
    public void ThePaintedMap_PassesTheDeskPlansGate_ButTheBoxSightCheck()
    {
        (DeskHome[] homes, DeskHome[] fixedArt) = Painted();
        DeskHome[] map = DeskPaintedMap.Merge(DeskHomeMap.Approved(), homes);
        (float x, float y, float h) eye = DeskHomeMap.OfficeEye;
        List<string> problems = DeskArrangement.HomeProblems(map, fixedArt, DeskHomeMap.Mat, DeskHomeMap.DeskTop, DeskHomeMap.SlideReach, eye, 0f);
        problems.AddRange(DeskSlides.Approved().Problems(map, DeskHomeMap.SlideReach));
        problems.AddRange(DeskArrangement.ClickBoxProblems(map, fixedArt, DeskHomeMap.ClickPad, DeskHomeMap.NoClickBox));
        CollectionAssert.IsEmpty(problems);
        Assert.That(fixedArt.Select(f => f.id), Is.EquivalentTo(new[] { DeskHomeMap.Sign, DeskHomeMap.Pc, DeskPaintedMap.Keyboard }));
    }

    [Test]
    public void EveryBakedObject_StandsInTheHomeDeskV3sLayoutGivesIt()
    {
        ContentNode objects = Json(Data + "anchors.json").Get("objects");
        const float slack = 0.01f;
        foreach (PaintedObject o in DeskPaintedMap.Objects.Where(o => o.home != null && o.home != DeskPaintedMap.PcMouse))
        {
            ContentNode pivot = objects.Get(o.name)?.Get("pivot_frame");
            Assert.IsNotNull(pivot, $"anchors.json has {o.name}");
            (float x0, float y0, float x1, float y1)? rect = LayoutRect(o.home);
            Assert.IsNotNull(rect, $"desk v3's layout has {o.home}");
            float x = Num(pivot.Items[0]), y = Num(pivot.Items[1]);
            Assert.That(x, Is.InRange(rect.Value.x0 - slack, rect.Value.x1 + slack), $"{o.name} across {o.home}");
            Assert.That(y, Is.InRange(rect.Value.y0 - slack, rect.Value.y1 + slack), $"{o.name} along {o.home}");
        }
    }

    [Test]
    public void EverySlider_MovesOnItsPaintedHome_EachTrackTowardTheMat()
    {
        DeskHome[] map = DeskPaintedMap.Merge(DeskHomeMap.Approved(), Painted().homes);
        foreach (DeskSlider slider in DeskSlides.Approved().sliders.Where(s => s.Moves))
        {
            DeskHome home = DeskHomeMap.Find(map, slider.Home);
            Assert.IsNotNull(home, $"{slider.id} has its home {slider.Home}");
            Assert.IsTrue(slider.MovesOn(home), $"{slider.id} moves on its painted home");
            if (slider.kind != SlideKind.Track)
                continue;
            DeskRect f = home.Footprint;
            float toMat = f.CentreX > 0f ? -1f : 1f;
            Assert.AreEqual(toMat, slider.Way(home), $"{slider.id} slides toward the mat");
        }
        Assert.AreEqual(HomeSlide.Right, DeskHomeMap.Find(map, DeskHomeMap.Intercom).slide, "desk v3's intercom slides right (DESK-PLAN's map declared none)");
    }

    [Test]
    public void APaintedHome_KeepsItsClassicQuarterTurn_CoveringTheSameRectangle()
    {
        DeskHome classic = new DeskHome(DeskHomeMap.StampRack, 0f, 0f, 0.32f, 0.12f, 90f, 0.08f, HomeShow.Always);
        DeskHome rack = DeskPaintedMap.Home(DeskHomeMap.StampRack, 0.815f, 0.33f, 1.11f, 0.545f, 0.2f, classic);
        Assert.AreEqual(90f, rack.yaw);
        DeskRect f = rack.Footprint;
        Assert.That(f.Width, Is.EqualTo(0.295f).Within(1e-4f));
        Assert.That(f.Height, Is.EqualTo(0.215f).Within(1e-4f));
        Assert.That(f.CentreX, Is.EqualTo(0.9625f).Within(1e-4f));

        DeskHome crate = new DeskHome(DeskHomeMap.Crate, 0f, 0f, 0.4f, 0.3f, 8f, 0.3f, HomeShow.Delivery);
        Assert.AreEqual(0f, DeskPaintedMap.Home(DeskHomeMap.Crate, 0f, 0f, 0.4f, 0.3f, 0.3f, crate).yaw, "a turn other than a quarter is dropped");
    }

    [Test]
    public void APaintedObjectOnTheDesk_ShowsFromDayOne_ElseTheClassicRule()
    {
        DeskHome[] classic = DeskHomeMap.Approved();
        Assert.AreEqual(HomeShow.Reserved, DeskHomeMap.Find(classic, DeskHomeMap.Glasses).show);
        Assert.AreEqual(HomeShow.Always, DeskPaintedMap.ShowOf(DeskHomeMap.Glasses, DeskHomeMap.Find(classic, DeskHomeMap.Glasses)));
        Assert.AreEqual(HomeShow.WhenOwned, DeskPaintedMap.ShowOf(DeskHomeMap.Scanner, DeskHomeMap.Find(classic, DeskHomeMap.Scanner)));
        Assert.AreEqual(HomeShow.Always, DeskPaintedMap.ShowOf("available", null));
    }

    [Test]
    public void Merge_ReplacesById_DropsAndMovesWhatDeskV3HasNoRoomFor_AndAddsTheRest()
    {
        DeskHome[] classic = DeskHomeMap.Approved();
        var scanner = new DeskHome(DeskHomeMap.Scanner, 1f, 0f, 0.4f, 0.2f, 0f, 0.1f, HomeShow.WhenOwned, HomeSlide.Left);
        var extra = new DeskHome("extra", 2f, 0f, 0.1f, 0.1f, 0f, 0.1f, HomeShow.Always);
        DeskHome[] map = DeskPaintedMap.Merge(classic, new[] { scanner, extra });

        Assert.AreSame(scanner, DeskHomeMap.Find(map, DeskHomeMap.Scanner));
        Assert.IsNull(DeskHomeMap.Find(map, DeskHomeMap.Mouse), "the art's mouse is cut");
        Assert.IsNull(DeskHomeMap.Find(map, DeskHomeMap.Thumbprint), "the later thumbprint reader lies on desk v3's chip reader tray");
        DeskHome crate = DeskHomeMap.Find(map, DeskHomeMap.Crate), was = DeskHomeMap.Find(classic, DeskHomeMap.Crate);
        Assert.That(crate.x, Is.EqualTo(was.x + 0.03f).Within(1e-5f));
        Assert.AreEqual(was.y, crate.y);
        Assert.AreSame(DeskHomeMap.Find(classic, DeskHomeMap.Decor), DeskHomeMap.Find(map, DeskHomeMap.Decor));
        Assert.AreSame(extra, map.Last());
        Assert.AreEqual(classic.Length - 2 + 1, map.Length);
    }
    [Test]
    public void TheOldModels_NeverShow_EveryGameplayObjectWearsItsPaintedModel_TheInsertsAndTheSheenStayHidden()
    {
        // The review of the Unity capture (S1, 2-7): STAMP's old drawer and daters, the scanner's add-ons, the upgrades' old models.
        Assert.AreEqual(PaintedRole.Reskin, DeskPaintedMap.Find(DeskPaintedMap.StampRack).role, "the painted two-step rack is STAMP's rack");
        Assert.AreEqual(PaintedRole.Reskin, DeskPaintedMap.Find(DeskPaintedMap.StampApprove).role, "the painted APPROVED dater is STAMP's");
        Assert.AreEqual(PaintedRole.Reskin, DeskPaintedMap.Find(DeskPaintedMap.StampDeny).role, "the painted DENIED dater is STAMP's");
        Assert.AreEqual(PaintedRole.Slider, DeskPaintedMap.Find(DeskPaintedMap.RulebookFolded).role, "the painting's red book shows in its pocket and peeks up");
        foreach (string insert in new[] { "Desk_BabelHome", "Desk_RolodexHome", "Desk_CardsHome", DeskPaintedMap.Glass })
            Assert.AreEqual(PaintedRole.Hidden, DeskPaintedMap.Find(insert).role, insert + ": the shell already carries it as painted (drawn too, it doubled)");
        CollectionAssert.AreEquivalent(new[] { "SweepBar" }, DeskPaintedMap.Kept(DeskHomeMap.Scanner), "only the scanner's sweep light stays: its add-ons, its lid glow and its readout were the old model");
        CollectionAssert.AreEquivalent(new[] { "UvPool", "BeamFX" }, DeskPaintedMap.Kept(DeskHomeMap.UvTorch), "the torch's light");
        CollectionAssert.IsEmpty(DeskPaintedMap.Kept(DeskHomeMap.ChipReader));
        foreach (string id in DeskPaintedMap.Inserts)
            Assert.IsNotNull(DeskPaintedMap.StandInFor(id), id + ": no painted model yet, so a stand-in, never the old one");
        Assert.IsNotNull(DeskPaintedMap.StandInFor(DeskPaintedMap.AutoFeed));
        Assert.IsNotNull(DeskPaintedMap.StandInFor(DeskPaintedMap.AnalysisBeacon));
    }

    [Test]
    public void EveryStandIn_FitsThePaintedSlotItStandsIn_InThePaintingsPalette()
    {
        foreach (StandIn s in DeskPaintedMap.StandIns)
        {
            Assert.IsNotEmpty(s.boxes, s.id);
            foreach (StandInBox b in s.boxes)
            {
                Assert.Greater(b.width, 0f, $"{s.id}/{b.name}");
                Assert.Greater(b.depth, 0f, $"{s.id}/{b.name}");
                Assert.Greater(b.height, 0f, $"{s.id}/{b.name}");
                Assert.GreaterOrEqual(b.z, 0f, $"{s.id}/{b.name}: on its anchor, never under it");
                foreach (float c in new[] { b.r, b.g, b.b })
                    Assert.That(c, Is.InRange(0f, 1f), $"{s.id}/{b.name}");
            }
            (float x0, float y0, float x1, float y1) e = s.Extent;
            (float x0, float y0, float x1, float y1)? rect = LayoutRect(s.home);
            Assert.IsNotNull(rect, s.home);
            float halfW = (rect.Value.x1 - rect.Value.x0) / 2f, halfD = (rect.Value.y1 - rect.Value.y0) / 2f;
            Assert.That(e.x0, Is.GreaterThanOrEqualTo(-halfW), s.id + ": inside its home across");
            Assert.That(e.x1, Is.LessThanOrEqualTo(halfW), s.id + ": inside its home across");
            if (s.anchor == StandInAnchor.Home)
            {
                Assert.That(e.y0, Is.GreaterThanOrEqualTo(-halfD), s.id + ": inside its painted slot along");
                Assert.That(e.y1, Is.LessThanOrEqualTo(halfD), s.id + ": inside its painted slot along");
                Assert.LessOrEqual(s.Top, s.maxHeight + 1e-6f, s.id + ": no taller than its painted home (the review: the BABEL box was 3x its 2.5 cm, the Rolodex drum 2x its 6 cm)");
                Assert.LessOrEqual(Math.Abs(s.offsetX), 0.08f, s.id + ": its painted slot lies within 8 cm of its layout home");
                Assert.LessOrEqual(Math.Abs(s.offsetY), 0.08f, s.id);
                Assert.That(s.floorDrop, Is.InRange(0f, 0.04f), s.id + ": the slot's surface at or under its insert's top");
            }
            else
            {
                Assert.LessOrEqual(e.y1, 0f, s.id + ": on the front face, never into the machine");
                Assert.Greater(e.y0, -0.02f, s.id + ": flat on the face (under 2 cm proud)");
                Assert.LessOrEqual(s.Top, 0.12f, s.id + ": below the painted scanner's top (its bed)");
            }
        }
        Assert.LessOrEqual(DeskPaintedMap.StandInFor(DeskHomeMap.BabelTuner).Top, 0.025f, "BABEL: a low tuner inside its 31 x 15 cm slats");
    }

    [Test]
    public void TheNextToolLamp_LightsItsHomesLampOnly_EveryLampHasAHome()
    {
        ContentNode lamps = Json(Data + "anchors.json").Get("lamps");
        var named = new HashSet<string>(lamps.Members.Select(m => m.Key));
        var homes = new[] { DeskHomeMap.DealTray, DeskHomeMap.Bell, DeskHomeMap.StampRack, DeskHomeMap.Scanner, DeskHomeMap.ChipReader, DeskHomeMap.Intercom };
        CollectionAssert.AreEquivalent(named, homes.Select(DeskPaintedMap.LampOf), "one lamp per home that has one, each in anchors.json");
        foreach (CaseLampState state in new[] { new CaseLampState(false, true, false), new CaseLampState(true, true, true), new CaseLampState(true, true, false, chipOwed: true), new CaseLampState(true, true, false, scanOwed: true) })
            Assert.IsNotNull(DeskPaintedMap.LampOf(NextToolLamp.For(state)), "every next tool has its lamp");
        Assert.IsNull(DeskPaintedMap.LampOf(null), "nothing next: every lamp unlit (slides.json: a lamp is guidance)");
        Assert.IsNull(DeskPaintedMap.LampOf(DeskHomeMap.Rolodex));
    }
    [Test]
    public void AStandInsFaces_CarryThePaintingsLight_AndItsEdgesAreInked()
    {
        var colour = (r: 0.8f, g: 0.7f, b: 0.6f);
        int mid = DeskPaintedMap.StandInCell / 2;
        float Lum(int face) => DeskPaintedMap.StandInTexel(colour, face, mid, mid).r;
        Assert.AreEqual(0.8f, Lum(0), 1e-6f, "the top in full light");
        for (int face = 1; face < 6; face++)
            Assert.Less(Lum(face), Lum(0), $"face {face}: darker than the top");
        Assert.Less(Lum(3), Lum(2), "the left side in shade, the light from the right (the shader's _RefLightDir)");
        Assert.AreEqual(DeskPaintedMap.FaceShade.Min(), DeskPaintedMap.FaceShade[5], "the bottom the darkest");
        foreach ((int x, int y) in new[] { (0, mid), (mid, 0), (DeskPaintedMap.StandInCell - 1, mid), (mid, DeskPaintedMap.StandInCell - 1), (1, 1) })
            Assert.AreEqual(DeskPaintedMap.InkLine, DeskPaintedMap.StandInTexel(colour, 1, x, y), $"({x}, {y}): an inked edge");
    }
    [Test]
    public void TheRingPanel_ShowsNothing_TheCrateWearsPaintingMatchedBoxes_AndBothStayOnThePaintedMap()
    {
        // The code review of round 6 (1): the RING SERVICE panel (the old RingPanel.fbx, 5 breaker boxes) stood on the switchboard's
        // home and the supply crate (kraft, tape, flaps, tag) on its spot every delivery morning, neither hidden, re-skinned nor stood in for.
        CollectionAssert.Contains(DeskPaintedMap.Unshown, DeskHomeMap.Switchboard, "the painted booth wall carries the painting's switchboard on its home");
        CollectionAssert.Contains(DeskPaintedMap.Boxed, DeskHomeMap.Crate, "the crate keeps its moving parts, in painting-matched boxes");
        DeskHome[] map = DeskPaintedMap.Merge(DeskHomeMap.Approved(), Painted().homes);
        foreach (string id in DeskPaintedMap.Unshown.Concat(DeskPaintedMap.Boxed))
            Assert.IsNotNull(DeskHomeMap.Find(map, id), id + ": its home stays on the painted map, so its object is laid and tracked there (the install finds it by its home)");

        foreach (string part in new[] { "Crate", "Flap_Left", "Flap_Right", "Flap_Front", "Flap_Back" })
        {
            Assert.AreEqual(PartLook.Box, DeskPaintedMap.LookOf(DeskHomeMap.Crate, part, out var colour, out _), part);
            Assert.AreEqual(DeskPaintedMap.Kraft, colour, part + ": kraft");
        }
        DeskPaintedMap.LookOf(DeskHomeMap.Crate, "Crate", out _, out float inset);
        Assert.That(inset, Is.InRange(0.0015f, 0.003f), "the body's top under its front and back flaps (1 mm under its walls' top): never two faces in one plane");
        DeskPaintedMap.LookOf(DeskHomeMap.Crate, "Flap_Front", out _, out float flapInset);
        Assert.AreEqual(0f, flapInset);
        Assert.AreEqual(PartLook.Box, DeskPaintedMap.LookOf(DeskHomeMap.Crate, "Label", out var label, out _));
        Assert.AreEqual(DeskPaintedMap.Cream, label, "its label in the painting's cream");
        Assert.AreEqual(PartLook.Hide, DeskPaintedMap.LookOf(DeskHomeMap.Crate, "Tape", out _, out _), "its tape (three bent strips) no box draws");
        Assert.AreEqual(PartLook.Hide, DeskPaintedMap.LookOf(DeskHomeMap.Crate, "AnyOtherPart", out _, out _), "nothing else of the old model");
        CollectionAssert.AreEqual(new[] { "Tag" }, DeskPaintedMap.Kept(DeskHomeMap.Crate), "its tag, the delivery's live label, stays");
        Assert.AreEqual(PartLook.Keep, DeskPaintedMap.LookOf(DeskHomeMap.Scanner, "Crate", out _, out _), "only a boxed object's parts are boxed");
        foreach ((float k, float lo, float hi) in new[] { (DeskPaintedMap.Kraft.r, DeskPaintedMap.Brass.r, DeskPaintedMap.Cream.r), (DeskPaintedMap.Kraft.g, DeskPaintedMap.Brass.g, DeskPaintedMap.Cream.g), (DeskPaintedMap.Kraft.b, DeskPaintedMap.Brass.b, DeskPaintedMap.Cream.b) })
            Assert.That(k, Is.InRange(Math.Min(lo, hi), Math.Max(lo, hi)), "kraft lies between the painting's brass and cream");
    }

    [Test]
    public void ThePaintedDesksAudit_ReadsEveryObjectTheInstallDresses_EachOnce()
    {
        // The binder's audit at bind (OfficeSceneBinder.AuditPainted) warns of any old part still drawn or clicked on these.
        CollectionAssert.AreEquivalent(new[]
        {
            DeskHomeMap.Scanner, DeskHomeMap.ChipReader, DeskHomeMap.Intercom, DeskHomeMap.UvTorch, DeskHomeMap.StampRack, DeskHomeMap.Detain,
            DeskHomeMap.BabelTuner, DeskHomeMap.Rolodex, DeskHomeMap.ProtocolRing, DeskHomeMap.Crate, DeskHomeMap.Switchboard,
        }, DeskPaintedMap.Dressed());
    }

    [Test]
    public void TheOldModelsClickBoxes_ThePaintedLookHasNoPartFor_TakeNoClick()
    {
        // The code review of round 6 (2): each dater's side button and the chip reader's readout box stayed live at the old
        // geometry's spots under the painted daters and the painted chip reader: invisible click areas.
        CollectionAssert.AreEqual(new[] { "Button" }, DeskPaintedMap.DeadClicks(DeskHomeMap.StampRack), "the daters' side buttons (the painted daters have none)");
        CollectionAssert.AreEqual(new[] { "ReadoutClick" }, DeskPaintedMap.DeadClicks(DeskHomeMap.ChipReader), "the readout's box (the painted reader has no readout yet)");
        foreach (string id in new[] { DeskHomeMap.Scanner, DeskHomeMap.Detain, DeskHomeMap.UvTorch, DeskHomeMap.Intercom })
            CollectionAssert.IsEmpty(DeskPaintedMap.DeadClicks(id), id + ": its boxes are its painted look's (fitted, or its own)");
    }

    [Test]
    public void ThePaintedSliders_EachLiftOnItsOwn_TheGlassesAndTheBook_AsTheGatesSlides()
    {
        // The review of the Unity capture (S2, 13 and 14): the painting's red book shows in its pocket and peeks up; the glasses lift.
        DeskSlides slides = DeskSlides.Approved();
        PaintedObject[] sliders = DeskPaintedMap.Objects.Where(o => o.role == PaintedRole.Slider).ToArray();
        CollectionAssert.AreEquivalent(new[] { "Desk_Glasses", DeskPaintedMap.RulebookFolded }, sliders.Select(o => o.name));
        foreach (PaintedObject o in sliders)
        {
            DeskSlider lift = slides.Find(DeskPaintedMap.LiftOf(o.name));
            Assert.IsNotNull(lift, o.name + ": its own lift in slides.json");
            Assert.AreEqual(SlideKind.Lift, lift.kind, o.name);
            Assert.AreEqual(o.home, lift.Home, o.name + ": lifting on its own home");
        }
        Assert.IsNull(DeskPaintedMap.LiftOf("Desk_Bell"), "a fixed object never lifts");
    }

    [Test]
    public void OnlyTheNextToolsLamp_IsLit_NoneAtRest()
    {
        // The review of the Unity capture (S1, 8): all six jewels glowed at rest.
        string[] lamps = Json(Data + "anchors.json").Get("lamps").Members.Select(m => m.Key).ToArray();
        var homes = new[] { DeskHomeMap.DealTray, DeskHomeMap.Bell, DeskHomeMap.StampRack, DeskHomeMap.Scanner, DeskHomeMap.ChipReader, DeskHomeMap.Intercom };
        foreach (string next in homes)
            CollectionAssert.AreEqual(new[] { DeskPaintedMap.LampOf(next) }, lamps.Where(l => DeskPaintedMap.LampLit(l, next)), next + ": its lamp alone");
        Assert.IsEmpty(lamps.Where(l => DeskPaintedMap.LampLit(l, null)), "nothing next: every lamp unlit");
        Assert.IsEmpty(lamps.Where(l => DeskPaintedMap.LampLit(l, DeskHomeMap.Rolodex)), "a home with no lamp: none");
    }

    [Test]
    public void TheAvailableSign_GlowsAtAThird_AndItsMaterialSaysSo()
    {
        // The review of the Unity capture (S3, 18): the AVAILABLE glow blew the panel out to pale lime.
        Assert.That(DeskPaintedMap.Glow("Desk_AvailableSign"), Is.InRange(0.2f, 0.5f), "the sign's glow, lowered");
        Assert.AreEqual(1f, DeskPaintedMap.Glow("Desk_Scanner"), "every other object's glow mask at full strength");
        string material = File.ReadAllText(Path.Combine(ProjectRoot(), "Assets/Art/Office/DeskPainted/Materials/Desk_AvailableSign.mat"));
        System.Text.RegularExpressions.Match glow = System.Text.RegularExpressions.Regex.Match(material, @"- _Glow: ([0-9.]+)");
        Assert.IsTrue(glow.Success, "the sign's material has its glow");
        Assert.AreEqual(DeskPaintedMap.Glow("Desk_AvailableSign"), float.Parse(glow.Groups[1].Value, CultureInfo.InvariantCulture), 1e-5f, "the imported material is the importer's (DeskPaintedImporter writes DeskPaintedMap.Glow)");
    }

    [Test]
    public void ThePaintedDesk_CutsTheArtsDecorations_AndBothCorkboards_ByNameOrByParent()
    {
        // The review of the Unity capture (S1, 6): the beige folder sorter and the lavender tray with its paper behind the scanner.
        string[] cut = DeskPaintedMap.CutParts();
        foreach (string part in new[] { "Clean_FormSorter__Body", "Clean_Tray__Base", "Clean_PaperBundle__Sheet", "Finish_Desk__Top", "CRT2_Glass", "Office_Stability__Screen" })
            Assert.IsTrue(DeskPaintedMap.Cuts(part, Array.Empty<string>(), cut), part);
        Assert.IsTrue(DeskPaintedMap.Cuts("Pin", new[] { "Card", "Left board assembly", "Office" }, cut), "a part under a cut assembly (the corkboards)");
        Assert.IsTrue(DeskPaintedMap.Cuts("Clock", new[] { "Right board assembly" }, cut));
        Assert.IsFalse(DeskPaintedMap.Cuts("Clean_Calculator__Body", Array.Empty<string>(), cut), "not the painted desk's (the art's cut props are cutParts')");
        Assert.IsFalse(DeskPaintedMap.Cuts("Left board", new[] { "Office" }, cut), "an assembly is cut by its exact name only");
        cut[0] = "changed";
        Assert.AreEqual("Finish_Desk__", DeskPaintedMap.CutParts()[0], "a fresh copy each call");
    }

    /// <summary>The project's folder (the tests' working folder, or three up from this file's).</summary>
    private static string ProjectRoot([CallerFilePath] string here = "") => File.Exists(Data + "anchors.json") ? "." : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..");
}
