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
}
