using System;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The r17 traffic port against the r17 page itself (CITY-R17): the C# sim, fed the exported lanes, replays the
/// page's golden record (every vehicle after the prefill and after 48 frames of 1/24 s, recorded by
/// tools/art/city_r17/export_r17.js with the page's frame loop held), plus the rules the page states.
/// </summary>
public class CityR17TrafficTests
{
    private static readonly double[] Mulberry20261021 = { 0.5792593690566719, 0.07661646720953286, 0.49269441328942776, 0.8162170618306845, 0.4773140742909163 };

    [Test]
    public void TheRandomStream_IsThePagesMulberry32()
    {
        var r = new CityR17Rng(20261021);
        foreach (double expected in Mulberry20261021)
            Assert.AreEqual(expected, r.Next(), 0);
        var k = new CityR17Rng(17017);
        Assert.AreEqual(0.4017650233581662, k.Next(), 0);
        Assert.AreEqual(0.9542687844950706, k.Next(), 0);
        Assert.AreEqual(0.23100808239541948, k.Next(), 0);
    }

    [Test]
    public void TheLanes_AreThePagesAfterItsLoadTimeEdits()
    {
        CityR17TrafficData d = CityR17Fixture.Traffic;
        Assert.AreEqual(33, d.Lanes.Length);
        var ids = new HashSet<string>();
        foreach (CityR17LaneDef l in d.Lanes)
            ids.Add(l.Id);
        foreach (string id in ids)
        {
            StringAssert.DoesNotStartWith("C-", id, "the C avenue is dropped (r17 review S1)");
            StringAssert.DoesNotStartWith("E-", id, "the E stream is dropped (r17 review S1)");
            Assert.AreNotEqual("G-DIVE", id);
            Assert.AreNotEqual("G-RISE", id);
        }
        foreach (string k in new[] { "K-E1", "K-E2", "K-W1", "K-W2" })
            Assert.IsTrue(ids.Contains(k), "the K skyway replaces them: " + k);
        Assert.AreEqual(0, d.LaneSlot["H1-E1"], "H1 draws behind P01 (LANE_SLOT)");
        Assert.AreEqual(5, d.LaneCap["D-A2"], "the D lanes never draw nearer than slot 5 (LANE_CAP)");
    }

    [Test]
    public void ThePrefill_MatchesThePagesGolden()
    {
        CityR17Traffic sim = CityR17Fixture.NewSim();
        CompareState(sim, CityR17Fixture.Golden.Get("states").Items[0], "after the prefill");
    }

    [Test]
    public void FortyEightFrames_MatchThePagesGolden()
    {
        CityR17Traffic sim = CityR17Fixture.NewSim();
        for (int f = 0; f < 48; f++)
            sim.Update(1.0 / 24, 0.7294766865079365);
        CompareState(sim, CityR17Fixture.Golden.Get("states").Items[1], "after 48 frames");
    }

    private static void CompareState(CityR17Traffic sim, ContentNode state, string when)
    {
        Assert.AreEqual(CityR17Json.Num(state, "simT"), sim.SimT, 1e-9, "the sim clock " + when);
        Assert.AreEqual((int)CityR17Json.Num(state, "count"), sim.Count, "live vehicles " + when);
        var byId = new Dictionary<int, CityR17Traffic.Vehicle>();
        foreach (CityR17Traffic.Vehicle q in sim.Vehicles)
            byId[q.Id] = q;
        int checkedAll = 0, checkedVis = 0;
        foreach (ContentNode row in state.Get("all").Items)
        {
            IReadOnlyList<ContentNode> r = row.Items;
            int id = (int)CityR17Json.Num(r[0]);
            Assert.IsTrue(byId.TryGetValue(id, out CityR17Traffic.Vehicle q), $"vehicle {id} {when}");
            Assert.AreEqual(r[1].Text, q.Lane.Id, $"vehicle {id}'s lane {when}");
            Assert.AreEqual(r[2].Text, sim.TypeName(q.Type), $"vehicle {id}'s type {when}");
            Assert.AreEqual(CityR17Json.Num(r[3]), q.S, 2e-5, $"vehicle {id}'s s {when}");
            Assert.AreEqual(CityR17Json.Num(r[4]), q.V, 2e-5, $"vehicle {id}'s v {when}");
            string lc = r[5].Kind == ContentNodeKind.Null ? null : r[5].Text;
            Assert.AreEqual(lc, q.LcFrom?.Id, $"vehicle {id}'s lane change {when}");
            checkedAll++;
        }
        var visible = new HashSet<int>();
        foreach (ContentNode row in state.Get("visible").Items)
        {
            IReadOnlyList<ContentNode> r = row.Items;
            int id = (int)CityR17Json.Num(r[0]);
            visible.Add(id);
            CityR17Traffic.Vehicle q = byId[id];
            Assert.IsTrue(q.Vis, $"vehicle {id} is drawn {when}");
            Assert.AreEqual(CityR17Json.Num(r[1]), q.Px, 2e-3, $"vehicle {id}'s x {when}");
            Assert.AreEqual(CityR17Json.Num(r[2]), q.Py, 2e-3, $"vehicle {id}'s y {when}");
            Assert.AreEqual(CityR17Json.Num(r[3]), q.Z, 2e-3, $"vehicle {id}'s Z {when}");
            Assert.AreEqual((int)CityR17Json.Num(r[4]), q.Slot, $"vehicle {id}'s slot {when}");
            Assert.AreEqual(CityR17Json.Num(r[5]), q.Roll, 2e-5, $"vehicle {id}'s roll {when}");
            Assert.AreEqual(CityR17Json.Num(r[6]), q.Pitch, 2e-5, $"vehicle {id}'s pitch {when}");
            checkedVis++;
        }
        foreach (CityR17Traffic.Vehicle q in sim.Vehicles)
            if (q.Vis)
                Assert.IsTrue(visible.Contains(q.Id), $"vehicle {q.Id} is drawn only where the page draws it {when}");
        Assert.Greater(checkedAll, 900);
        Assert.Greater(checkedVis, 900);
    }

    [Test]
    public void AMinuteOfTraffic_SpawnsAndDespawnsHidden_AndKeepsTheDensity()
    {
        CityR17Traffic sim = CityR17Fixture.NewSim();
        int start = sim.Count;
        var seen = new HashSet<int>();
        foreach (CityR17Traffic.Vehicle q in sim.Vehicles)
            seen.Add(q.Id);
        for (int f = 0; f < 60 * 60; f++)
        {
            sim.Update(1.0 / 60, 0.73);
            foreach (CityR17Traffic.Vehicle q in sim.Vehicles)
            {
                if (seen.Add(q.Id))
                {
                    // a new vehicle starts at its lane's hidden start: off the plate, or at a ramp start behind a plane
                    Assert.LessOrEqual(q.S, 40, $"vehicle {q.Id} starts at its lane start");
                }
            }
        }
        Assert.Greater(sim.Spawned, 500, "the conveyors keep spawning");
        Assert.Greater(sim.Despawned, 500, "and the lane ends keep taking them");
        Assert.That(sim.Count, Is.InRange(start * 0.8, start * 1.25), "the density holds");
    }

    [Test]
    public void NoVehicleEverFlipsItsNose_OrBanksPastItsCap()
    {
        CityR17Traffic sim = CityR17Fixture.NewSim();
        var lastF = new Dictionary<int, (double, double, double)>();
        for (int f = 0; f < 24 * 20; f++)
        {
            sim.Update(1.0 / 24, 0.73);
            foreach (CityR17Traffic.Vehicle q in sim.Vehicles)
            {
                if (!q.Vis)
                    continue;
                double cap = (q.Heavy ? 8 : 14) * Math.PI / 180 + 1e-9;
                Assert.LessOrEqual(Math.Abs(q.Roll), cap, $"vehicle {q.Id}'s bank");
                if (lastF.TryGetValue(q.Id, out (double x, double y, double z) p))
                    Assert.Greater(p.x * q.F0 + p.y * q.F1 + p.z * q.F2, 0.9, $"vehicle {q.Id}'s nose never swings round in a frame");
                lastF[q.Id] = (q.F0, q.F1, q.F2);
            }
        }
    }

    [Test]
    public void TheSlotRules_PinCapAndDepth()
    {
        CityR17Traffic sim = CityR17Fixture.NewSim();
        Assert.AreEqual(0, sim.SlotOf(4000), "beyond the farthest plane: behind P01");
        Assert.AreEqual(10, sim.SlotOf(20), "nearer than the deck");
        Assert.AreEqual(7, sim.SlotOf(300), "between P07 (520 m) and P08 (200 m)");
        Assert.AreEqual(0.02 * 0 + sim.ParOf(5000), CityR17Fixture.Traffic.PF[1], 1e-12);
        foreach (CityR17Traffic.Vehicle q in sim.Vehicles)
        {
            if (!q.Vis)
                continue;
            if (q.Lane.Id.StartsWith("H1-"))
                Assert.AreEqual(0, q.Slot, "H1 is pinned behind P01");
            if (q.Lane.Id.StartsWith("D-"))
                Assert.LessOrEqual(q.Slot, 5, "D is capped at slot 5");
        }
    }

    [Test]
    public void TheFramePath_AllocatesNothing()
    {
        CityR17Traffic sim = CityR17Fixture.NewSim();
        for (int f = 0; f < 240; f++)
            sim.Update(1.0 / 60, 0.73);   // warm the pools and the lists' capacity
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int f = 0; f < 60; f++)
            sim.Update(1.0 / 60, 0.73);
        long after = GC.GetAllocatedBytesForCurrentThread();
        if (before == 0 && after == 0)
            return;   // a runtime without the per-thread counter
        Assert.LessOrEqual(after - before, 4096, "steady state: pooled vehicles, no per-frame garbage");
    }

    [Test]
    public void TheFleet_DropsItsHoverPods()
    {
        CityR17Models m = CityR17Models.Parse(CityR17Fixture.Read("Assets/Data/City/R17/city_r17_vehicles.json"));
        foreach (CityR17Model model in m.Models)
        {
            int pods = 0;
            for (int i = 0; i < model.Verts; i++)
                if (model.Att[4 * i] == 6 || model.Att[4 * i + 2] == 191)
                    pods++;
            Assert.AreEqual(0, pods, model.Name + " keeps no pod vertex after the load");
            Assert.AreEqual(model.Tris * 3, model.Idx.Length);
        }
        Assert.AreEqual(new[] { "taxi", "bus", "truck", "coupe" }, CityR17Models.Names);
    }
}
