using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The r17 loops (CITY-R17) against the page: the clouds' placement and morph, the train's passes on every frame of
/// its cycle (the page's own functions, recorded in the golden), the ads' tiles and schedule, the flipbooks and the
/// noon lamps.
/// </summary>
public class CityR17LoopsTests
{
    [Test]
    public void TheClouds_AreWhereThePagePutsThem()
    {
        CityR17Scene s = CityR17Fixture.Scene;
        ContentNode g = CityR17Fixture.Golden.Get("clouds");
        double a = CityR17Json.Num(g, "sc");
        Assert.AreEqual(s.Clouds.ScRef, a, 0);
        int rows = 0;
        foreach (ContentNode row in g.Get("rows").Items)
        {
            IReadOnlyList<ContentNode> r = row.Items;
            double f = CityR17Json.Num(r[0]);
            CityR17Cloud c = Array.Find(s.Clouds.List, x => x.Id == r[1].Text);
            Assert.AreEqual(CityR17Json.Num(r[2]), CityR17Loops.CloudX(c, f, a), 1e-6, $"{c.Id} x at f{f}");
            Assert.AreEqual(CityR17Json.Num(r[3]), c.Top, 0, $"{c.Id} top");
            CityR17Loops.CloudDrawing(c, f, out int j, out double w);
            Assert.AreEqual((int)CityR17Json.Num(r[4]), j, $"{c.Id} drawing at f{f}");
            Assert.AreEqual(CityR17Json.Num(r[5]), w, 1e-9, $"{c.Id} morph weight at f{f}");
            rows++;
        }
        Assert.AreEqual(11 * 8, rows);
    }

    [Test]
    public void TheClouds_MoveContinuously_AndDissolveWithoutAPop()
    {
        CityR17Scene s = CityR17Fixture.Scene;
        foreach (CityR17Cloud c in s.Clouds.List)
        {
            double step = 1 / (c.K * s.Clouds.ScRef);
            for (double f = 0; f < 600; f += 0.25)
            {
                double x0 = CityR17Loops.CloudX(c, f, s.Clouds.ScRef), x1 = CityR17Loops.CloudX(c, f + 0.25, s.Clouds.ScRef);
                if (x1 < x0 + c.L / 2)   // not the wrap
                    Assert.AreEqual(-step * 0.25, x1 - x0, 1e-6, $"{c.Id} drifts west at 1/k device px a frame");
                CityR17Loops.CloudDrawing(c, f, out int j, out double w);
                Assert.That(j, Is.InRange(0, c.N - 1));
                Assert.That(w, Is.InRange(0.0, 1.0));
            }
        }
    }

    [Test]
    public void TheTrain_RunsThePagesPasses_OnEveryFrame()
    {
        CityR17TrainData d = CityR17Fixture.Scene.Train;
        ContentNode g = CityR17Fixture.Golden.Get("train");
        double sc = CityR17Json.Num(g, "sc");
        IReadOnlyList<ContentNode> passes = g.Get("passes").Items;
        Assert.AreEqual(d.Cycle, passes.Count);
        int active = 0;
        for (int f = 0; f < passes.Count; f++)
        {
            CityR17Loops.TrainPass p = CityR17Loops.Pass(d, f, sc);
            ContentNode e = passes[f];
            if (e.Kind == ContentNodeKind.Null)
            {
                Assert.IsFalse(p.Active, $"quiet at f{f}");
                continue;
            }
            Assert.IsTrue(p.Active, $"a pass at f{f}");
            Assert.AreEqual(e.Items[0].Text == "W", p.West, $"direction at f{f}");
            Assert.AreEqual(CityR17Json.Num(e.Items[1]), p.K, 0, $"k at f{f}");
            Assert.AreEqual((int)CityR17Json.Num(e.Items[2]), p.Index, $"pass index at f{f}");
            active++;
        }
        Assert.Greater(active, 200, "twice every 48 s");
    }

    [Test]
    public void TheTrain_EntersAndLeavesBeyondItsMattes()
    {
        CityR17TrainData d = CityR17Fixture.Scene.Train;
        double sc = 0.73, len = CityR17Loops.TrainPassFrames(d, sc);
        foreach (CityR17TrainPassDef def in d.Passes)
        {
            var first = new CityR17Loops.TrainPass(def.West, 0, 0);
            var last = new CityR17Loops.TrainPass(def.West, len - 1, 0);
            double x0 = CityR17Loops.TrainWestEnd(d, first, sc), x1 = CityR17Loops.TrainWestEnd(d, last, sc);
            if (def.West)
            {
                Assert.GreaterOrEqual(x0, d.ClipX, "westbound starts wholly right of the industry matte");
                Assert.LessOrEqual(x1 + d.Length, d.LeftMatte, "and ends wholly left of the pagoda matte");
            }
            else
            {
                Assert.LessOrEqual(x0 + d.Length, d.LeftMatte, "eastbound starts wholly left of the pagoda matte");
                Assert.GreaterOrEqual(x1, d.ClipX, "and ends wholly right of the industry matte");
            }
        }
        // eastbound is the westbound set mirrored: the cars reversed, each in the mirrored place
        for (int i = 0; i < d.Cars.Length; i++)
        {
            CityR17Loops.TrainCar(d, false, i, out int car, out double ox, out bool mirrored);
            Assert.IsTrue(mirrored);
            Assert.AreEqual(d.Cars.Length - 1 - i, car);
            Assert.AreEqual(d.Length - d.Cars[car].Ox - d.Cars[car].W, ox, 1e-9);
        }
    }

    [Test]
    public void EveryScreen_HasATileOnEveryFrame_FromItsSheets()
    {
        CityR17AdsData ads = CityR17Fixture.Scene.Ads;
        Assert.AreEqual(22, ads.Screens.Length, "22 screens");
        Assert.AreEqual(2304, ads.N, "a 96 s master cycle");
        int tiles = ads.Tiles.Length / 5;
        foreach (CityR17Screen sc in ads.Screens)
        {
            Assert.AreEqual(ads.N, CityR17Loops.RleFrames(sc.Rle), sc.Id + " covers the cycle");
            int[] per = CityR17Loops.Expand(sc.Rle, ads.N);
            foreach (int t in per)
            {
                Assert.That(t, Is.InRange(0, tiles - 1), sc.Id);
                Assert.That(ads.Tiles[t * 5], Is.InRange(0, ads.Sheets.Length - 1), sc.Id + " sheet");
            }
        }
    }

    [Test]
    public void NoAd_IsEverOnTwoScreensAtOnce()
    {
        CityR17AdsData ads = CityR17Fixture.Scene.Ads;
        Assert.Greater(ads.Holds.Count, 15);
        List<string> dup = CityR17Loops.DuplicateAds(ads);
        Assert.IsEmpty(dup, string.Join("; ", dup));
        foreach (KeyValuePair<string, CityR17Hold[]> kv in ads.Holds)
            for (int i = 0; i < kv.Value.Length; i++)
                Assert.AreNotEqual(kv.Value[i].Ad, kv.Value[(i + 1) % kv.Value.Length].Ad, kv.Key + " never follows an ad with itself");
    }

    [Test]
    public void TheFlipbooks_LoopTheirFrames()
    {
        CityR17Scene s = CityR17Fixture.Scene;
        Assert.AreEqual(120, s.Fire.N);
        Assert.AreEqual(144, s.Smoke.N);
        CityR17Loops.Tile(s.Fire.Seq, 0, out int sheet0, out CityR17Rect r0);
        CityR17Loops.Tile(s.Fire.Seq, s.Fire.N, out int sheetN, out CityR17Rect rN);
        Assert.AreEqual(sheet0, sheetN);
        Assert.AreEqual(r0.X, rN.X);
        Assert.AreEqual(r0.Y, rN.Y);
        Assert.AreEqual(s.Fire.Gain[5], CityR17Loops.SpillGain(s.Fire, 5 + 3 * s.Fire.N), 0);
        foreach (CityR17SmokeStack st in s.Smoke.Stacks)
        {
            Assert.AreEqual(s.Smoke.N * 5, st.Seq.Length, st.Id);
            Assert.AreEqual(s.Smoke.N * 5, st.MouthSeq.Length, st.Id);
        }
        CityR17Loops.Tile(s.Fire.Seq, -1, out _, out CityR17Rect last);
        CityR17Loops.Tile(s.Fire.Seq, s.Fire.N - 1, out _, out CityR17Rect lastToo);
        Assert.AreEqual(last.X, lastToo.X, "negative frames wrap");
    }

    [Test]
    public void TheLaundry_SwaysInGusts_MoreAtTheHem()
    {
        for (double t = 0; t < 60; t += 0.37)
        {
            double g = CityR17Loops.LaundryGust(t);
            Assert.That(g, Is.InRange(0.45, 1.0));
            Assert.LessOrEqual(Math.Abs(CityR17Loops.LaundryRow(3, 0, 30, t, g)), 0.8 + 1e-9, "the line end barely moves");
            Assert.LessOrEqual(Math.Abs(CityR17Loops.LaundryRow(3, 29, 30, t, g)), 4.4 + 1e-9, "the hem swings at most 4.4 px");
        }
        // continuous: a frame at 60 fps moves a row a fraction of a pixel
        double a = CityR17Loops.LaundryRow(5, 20, 30, 10, CityR17Loops.LaundryGust(10));
        double b = CityR17Loops.LaundryRow(5, 20, 30, 10 + 1.0 / 60, CityR17Loops.LaundryGust(10 + 1.0 / 60));
        Assert.Less(Math.Abs(a - b), 0.5);
    }

    [Test]
    public void TheNoonLamps_EaseInAndOut_NeverBlink()
    {
        CityR17LightsData L = CityR17Fixture.Scene.Lights;
        Assert.AreEqual(28 + 18 + 26 + 2 + 6, L.Lamps.Length, "the noon set: seam, band, ring, spire markers, top-row warnings");
        foreach (CityR17Lamp lamp in L.Lamps)
        {
            double prev = CityR17Loops.LampAlpha(lamp, 0), peak = 0;
            for (double t = 1.0 / 60; t < 20; t += 1.0 / 60)
            {
                double a = CityR17Loops.LampAlpha(lamp, t);
                Assert.That(a, Is.InRange(0.0, 0.7 + 1e-9), lamp.Kind.ToString());
                Assert.LessOrEqual(Math.Abs(a - prev), 0.2, $"{lamp.Kind} {lamp.Index} eases (no hard blink) at t {t}");
                prev = a;
                peak = Math.Max(peak, a);
            }
            Assert.Greater(peak, 0.2, $"{lamp.Kind} {lamp.Index} does light up");
        }
        Assert.AreEqual(1.0, CityR17Loops.Pulse(0.3, 0.21, 0.35, 0.25), 0);
        Assert.AreEqual(0.0, CityR17Loops.Pulse(2.0, 0.21, 0.35, 0.25), 0);
        Assert.AreEqual(0.0, CityR17Loops.Bump(-0.1, 0.12, 0.04), 0);
    }
}
