using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The CITY-R17 scene contract (the exported r17 page), its draw order, the imported art map, the camera's comfort
/// rules and the framing.
/// </summary>
public class CityR17SceneTests
{
    [Test]
    public void TheScene_IsTheR17Page()
    {
        CityR17Scene s = CityR17Fixture.Scene;
        Assert.AreEqual(2688, s.PlateW);
        Assert.AreEqual(1152, s.PlateH);
        Assert.AreEqual(11, s.Planes.Length, "the 11 planes");
        for (int i = 0; i < s.Planes.Length; i++)
        {
            Assert.AreEqual($"P{i:00}", s.Planes[i].Id);
            if (i > 0)
                Assert.Greater(s.Planes[i].Parallax, s.Planes[i - 1].Parallax, "nearer planes move more");
            Assert.LessOrEqual(s.Planes[i].Haze, 0.08, "buildings stay clear: light distance haze only");
        }
        Assert.AreEqual(8, s.Clouds.List.Length, "8 clouds at three depths");
        Assert.AreEqual(3, s.Train.Cars.Length);
        Assert.AreEqual(2, s.Train.Passes.Length, "twice every 48 s");
        Assert.AreEqual(24, s.Fps);
        Assert.AreEqual(2, s.Smoke.Stacks.Length);
    }

    [Test]
    public void TheDrawOrder_IsThePagesDrawers()
    {
        CityR17Scene s = CityR17Fixture.Scene;
        List<CityR17Order.Step> steps = CityR17Order.Steps(s);
        string order = string.Join(" ", steps);
        StringAssert.StartsWith("P00 clouds@0 traffic@0 P01 lights@1 traffic@1 P02 lights@2 traffic@2 P03 traffic@3 P04 lights@4 traffic@4 " +
                                "P05 traffic@5 ads@5 P06 traffic@6 P07 train@7 lights@7 traffic@7 P08 laundry@8 traffic@8 smoke@8 " +
                                "P09 fire@9 smokeMouth@9 traffic@9 P10 traffic@10", order);
        for (int p = 0; p < s.Planes.Length; p++)
            Assert.IsTrue(CityR17Order.Has(s, "traffic", p), "every plane's slot carries traffic: " + p);
        CollectionAssert.IsEmpty(CityR17Order.Unported(s), "every drawer of the page is ported");
        Assert.IsNotNull(s.Laundry, "the laundry sway");
        Assert.AreEqual(8, s.Laundry.Plane, "on the tenements");
        Assert.AreEqual(44, s.Laundry.Blobs.Length, "44 cloth panels");
    }

    [Test]
    public void TheArtMap_CoversEveryFileTheSceneNames()
    {
        CityR17Scene s = CityR17Fixture.Scene;
        CityR17Art art = CityR17Fixture.Art;
        foreach (CityR17Plane p in s.Planes)
        {
            CityR17Art.Entry e = art.Get("Planes/" + p.File);
            Assert.IsNotNull(e, p.File);
            Assert.IsTrue(e.HasPlate, p.File + " is cropped and placed");
            Assert.GreaterOrEqual(e.Plate.X, p.Frame.X - 1e-6);
            Assert.LessOrEqual(e.Plate.X + e.Plate.W, p.Frame.X + p.Frame.W + 1e-3);
        }
        foreach (CityR17Cloud c in s.Clouds.List)
        {
            CityR17Art.Entry e = art.Get("Clouds/" + c.File);
            Assert.IsNotNull(e, c.File);
            CityR17Rect last = CityR17Loops.CloudCell(c, c.N - 1);
            Assert.LessOrEqual(last.X + last.W, e.UsedW, c.Id + "'s last cell is in its sheet");
            Assert.LessOrEqual(last.Y + last.H, e.UsedH, c.Id);
        }
        foreach (string sheet in s.Ads.Sheets)
            Assert.IsNotNull(art.Get("Ads/" + sheet), sheet);
        foreach (string sheet in s.Fire.Sheets)
            Assert.IsNotNull(art.Get("Fire/" + sheet), sheet);
        Assert.IsNotNull(art.Get("Fire/" + s.Fire.SpillBase));
        Assert.IsNotNull(art.Get("Fire/" + s.Fire.Spill));
        foreach (CityR17SmokeStack st in s.Smoke.Stacks)
        {
            foreach (string sheet in st.Sheets)
                Assert.IsNotNull(art.Get("Smoke/" + sheet), sheet);
            foreach (string sheet in st.MouthSheets)
                Assert.IsNotNull(art.Get("Smoke/" + sheet), sheet);
        }
        foreach (CityR17TrainCar car in s.Train.Cars)
        {
            Assert.IsNotNull(art.Get("Train/" + car.File), car.File);
            Assert.IsNotNull(art.Get("Train/" + car.Glow), car.Glow);
        }
        Assert.IsNotNull(art.Get("Train/" + s.Train.ParapetFile));
        Assert.IsTrue(art.Get("Train/" + s.Train.PaintFile).HasPlate, "the viaduct paint is cropped and placed");
        Assert.IsTrue(art.Get("Hive/" + s.Laundry.Clean).HasPlate, "the laundry's clean patch is cropped and placed");
        Assert.IsTrue(art.Get("Hive/" + s.Laundry.Cloth).HasPlate, "the laundry's cloth is cropped and placed");
        foreach (KeyValuePair<string, CityR17Art.Entry> kv in art.Files)
        {
            Assert.AreEqual(0, kv.Value.SizeW % 4, kv.Key + " pads to a multiple of 4 (BC7)");
            Assert.AreEqual(0, kv.Value.SizeH % 4, kv.Key);
            Assert.LessOrEqual(Math.Max(kv.Value.SizeW, kv.Value.SizeH), 4096, kv.Key + " fits the import's max size");
            Assert.IsTrue(CityR17Fixture.Exists("Assets/Art/City/R17/" + kv.Key), kv.Key + " is imported");
        }
    }

    [Test]
    public void EveryTileOfEveryFlipbook_StartsInItsSheet_AndClipsAsTheCanvasDoes()
    {
        CityR17Scene s = CityR17Fixture.Scene;
        CityR17Art art = CityR17Fixture.Art;
        void Check(int[] seq5, string folder, string[] sheets, string what)
        {
            for (int i = 0; i < seq5.Length; i += 5)
            {
                CityR17Art.Entry e = art.Get(folder + "/" + sheets[seq5[i]]);
                var src = new CityR17Rect(seq5[i + 1], seq5[i + 2], seq5[i + 3], seq5[i + 4]);
                Assert.Less(src.X, e.UsedW, what);
                Assert.Less(src.Y, e.UsedH, what);
                Assert.LessOrEqual(src.X + src.W - e.UsedW, 16, what + " reaches at most a few px past its sheet");
                Assert.LessOrEqual(src.Y + src.H - e.UsedH, 16, what);
                Assert.IsTrue(CityR17Loops.ClipTile(src, e.UsedW, e.UsedH, out CityR17Rect c, out double dx, out double dy), what);
                Assert.AreEqual(0, dx, what);
                Assert.AreEqual(0, dy, what);
                Assert.LessOrEqual(c.X + c.W, e.UsedW, what);
                Assert.LessOrEqual(c.Y + c.H, e.UsedH, what);
            }
        }
        Check(s.Fire.Seq, "Fire", s.Fire.Sheets, "fire");
        foreach (CityR17SmokeStack st in s.Smoke.Stacks)
        {
            Check(st.Seq, "Smoke", st.Sheets, "smoke " + st.Id);
            Check(st.MouthSeq, "Smoke", st.MouthSheets, "smoke mouth " + st.Id);
        }
        Check(s.Ads.Tiles, "Ads", s.Ads.Sheets, "ads");
    }

    [Test]
    public void TheFraming_OverscansLikeThePage_AndLetterboxesTheWholePlate()
    {
        CityR17Scene s = CityR17Fixture.Scene;
        Assert.AreEqual(1.0212673611111112, CityR17Framing.Overscan(s), 1e-9, "the page's overscan");
        CityR17Framing f = CityR17Framing.For(s, 1920, 1080, false);
        Assert.AreEqual(1920, f.OutW);
        Assert.AreEqual(823, f.OutH, "21:9 inside 16:9");
        Assert.AreEqual(0.7294766865079365, f.PxPerPlate, 2e-3, "the page's device scale at 1920");
        Assert.AreEqual(0, f.MaxPan);
        CityR17Framing wide = CityR17Framing.For(s, 2560, 1080, false);
        Assert.AreEqual(1080, wide.OutH);
        CityR17Framing fill = CityR17Framing.For(s, 1920, 1080, true);
        Assert.AreEqual(1920, fill.OutW);
        Assert.AreEqual(1080, fill.OutH, "fill: no bars");
        Assert.Greater(fill.MaxPan, 0, "and it pans across the plate");
        Assert.AreEqual(s.PlateH / CityR17Framing.Overscan(s), fill.H, 1e-9);
    }

    [Test]
    public void TheLook_FollowsSmoothly_UnderTheBug69Cap_AndRestsUnderReducedMotion()
    {
        CityR17Look look = CityR17Look.For(CityR17Fixture.Scene);
        double px = 0.73, dt = 1.0 / 30;
        for (int i = 0; i < 300; i++)
        {
            double moved = look.Step(dt, i < 150 ? 1 : -1, 0.5, px, false);
            Assert.LessOrEqual(moved, 1.0 + 1e-9, "the nearest plane moves at most 1 px a 30 fps frame (BUG-69)");
        }
        Assert.Less(Math.Abs(look.X + 1), 0.05, "it gets there");
        look.Step(dt, 1, 1, px, true);
        Assert.AreEqual(0, look.X, "Reduced Motion: no parallax");
        Assert.AreEqual(0, look.ShiftX(1));
        look.Snap(0.5, 0);
        Assert.AreEqual(-0.5 * 45 * 0.62, look.ShiftX(0.62), 1e-9, "a plane's shift is its parallax times the look");
    }
}
