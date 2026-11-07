using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Stargate dialer's dial (the director's D1): a ring of 14 glyph slots
/// (the origin, 8 nations, 5 eras) under 7 chevrons, one every 2 slots; the
/// ring's rule (a nation under the top chevron locks PLACE, then an era under
/// the second locks PERIOD, anything else there starts over); the strobe of
/// one chevron every calibration / 7; the board's layout; the code-drawn art.
/// </summary>
public class GateDialTests
{
    private static readonly GateGlyph Egypt = new GateGlyph(GateGlyphKind.Nation, 0);
    private static readonly GateGlyph Ancient = new GateGlyph(GateGlyphKind.Era, 0);
    private static readonly GateGlyph Origin = new GateGlyph(GateGlyphKind.Origin, 0);

    [Test]
    public void TheRing_HasTheOrigin_EightNations_AndFiveEras_InFourteenSlots_AChevronEveryTwo()
    {
        GateGlyph[] ring = GateDial.Ring(8, 5);
        Assert.AreEqual(14, ring.Length);
        Assert.AreEqual(GateGlyphKind.Origin, ring[0].Kind);
        Assert.AreEqual(8, ring.Count(g => g.Kind == GateGlyphKind.Nation));
        Assert.AreEqual(5, ring.Count(g => g.Kind == GateGlyphKind.Era));
        Assert.AreEqual(2, GateDial.Spacing(ring.Length));
        CollectionAssert.AreEqual(Enumerable.Range(0, 8).ToArray(), ring.Where(g => g.Kind == GateGlyphKind.Nation).Select(g => g.Index).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(0, 5).ToArray(), ring.Where(g => g.Kind == GateGlyphKind.Era).Select(g => g.Index).ToArray());
        Assert.AreEqual(GateGlyphKind.Era, ring[2].Kind, "nations and eras take turns");
    }

    [Test]
    public void AGrownRing_PadsToTheChevrons_SoEveryDetentStillPutsAGlyphUnderEveryChevron()
    {
        GateGlyph[] ring = GateDial.Ring(20, 5);
        Assert.AreEqual(0, ring.Length % GateDial.Chevrons);
        Assert.GreaterOrEqual(ring.Length, 26);
        Assert.AreEqual(20, ring.Count(g => g.Kind == GateGlyphKind.Nation));
        Assert.AreEqual(GateGlyphKind.Blank, ring.Last().Kind);
    }

    [Test]
    public void AtEveryDetent_AGlyphSitsExactlyUnderEveryChevron()
    {
        const int slots = 14;
        for (int step = -20; step <= 20; step++)
        {
            float rotation = GateDial.Detent(step * 25.7142857f + 3f, slots);
            for (int c = 0; c < GateDial.Chevrons; c++)
            {
                int s = GateDial.SlotUnderChevron(rotation, c, slots);
                Assert.AreEqual(0f, GateDial.Delta(GateDial.ChevronAngle(c), GateDial.SlotAngle(s, rotation, slots)), 1e-3f, $"rotation {rotation}, chevron {c}");
            }
        }
    }

    [Test]
    public void RotationFor_PutsTheSlotUnderTheChevron_AndTheDetentIsTheNearest()
    {
        const int slots = 14;
        for (int s = 0; s < slots; s++)
            for (int c = 0; c < GateDial.Chevrons; c++)
                Assert.AreEqual(s, GateDial.SlotUnderChevron(GateDial.RotationFor(s, c, slots), c, slots));
        Assert.AreEqual(25.714f, GateDial.Detent(20f, slots), 1e-2f);
        Assert.AreEqual(0f, GateDial.Detent(12f, slots), 1e-3f);
        Assert.AreEqual(-25.714f, GateDial.Detent(-14f, slots), 1e-2f, "not wrapped: the ring eases the short way");
    }

    [Test]
    public void Angles_RunClockwiseFromTheTop_OnThePlate()
    {
        var c = new BoardPoint(500f, 500f);
        Assert.AreEqual(0f, GateDial.AngleOf(c, new BoardPoint(500f, 400f)), 1e-3f);
        Assert.AreEqual(90f, GateDial.AngleOf(c, new BoardPoint(600f, 500f)), 1e-3f);
        Assert.AreEqual(180f, GateDial.AngleOf(c, new BoardPoint(500f, 600f)), 1e-3f);
        Assert.AreEqual(270f, GateDial.AngleOf(c, new BoardPoint(400f, 500f)), 1e-3f);
        Assert.AreEqual(-20f, GateDial.Delta(10f, 350f), 1e-3f);
        Assert.AreEqual(20f, GateDial.Delta(350f, 10f), 1e-3f);
    }

    [Test]
    public void TheRingsRule_NationThenEra_AWrongEraCostsOneDrag_AndAnythingElseStartsOver()
    {
        Assert.AreEqual(DialAction.LockPlace, GateDial.Resolve(false, true, Egypt, Ancient));
        Assert.AreEqual(DialAction.None, GateDial.Resolve(false, true, Ancient, Egypt), "an era at the top locks nothing");
        Assert.AreEqual(DialAction.None, GateDial.Resolve(false, true, Origin, Egypt));
        Assert.AreEqual(DialAction.LockPeriod, GateDial.Resolve(true, true, Egypt, Ancient));
        Assert.AreEqual(DialAction.LockPeriod, GateDial.Resolve(true, false, Origin, Ancient), "let go in place: the era there locks");
        Assert.AreEqual(DialAction.Reset, GateDial.Resolve(true, true, Ancient, Egypt), "moved on with no era under chevron 2: start over");
        Assert.AreEqual(DialAction.None, GateDial.Resolve(true, false, Ancient, Egypt), "a click on the ring changes nothing");
    }

    [Test]
    public void TheStrobe_EngagesOneChevronEvery20Over7Seconds_AllSevenAsItOpens()
    {
        Assert.AreEqual(0, GateDial.Engaged(0f, 20f));
        Assert.AreEqual(0, GateDial.Engaged(2.8f, 20f));
        Assert.AreEqual(1, GateDial.Engaged(2.9f, 20f));
        Assert.AreEqual(3, GateDial.Engaged(10f, 20f));
        Assert.AreEqual(6, GateDial.Engaged(19.9f, 20f));
        Assert.AreEqual(7, GateDial.Engaged(20f, 20f));
        Assert.AreEqual(7, GateDial.Engaged(99f, 20f));
        Assert.AreEqual(0.5f, GateDial.SinceEngaged(2, 9.0714f, 20f), 1e-3f);
    }

    [Test]
    public void TheLayout_KeepsEveryPartOnTheCover_ClearOfMasterAndOfEachOther()
    {
        var layout = new GateBoardLayout(8, 4, 3);
        BoardRect cover = layout.Cover;
        Assert.LessOrEqual(cover.X + cover.W, 1440f, "MASTER's panel stays the plate's");
        for (int d = 0; d < 8; d++)
        {
            BoardRect plate = layout.CallPlate(d);
            Assert.GreaterOrEqual(plate.X, cover.X);
            Assert.LessOrEqual(plate.X + plate.W, cover.X + cover.W);
            if (d > 0)
                Assert.Greater(plate.X, layout.CallPlate(d - 1).X + layout.CallPlate(d - 1).W, "the call plates do not touch");
        }
        for (int m = 0; m < 5; m++)
        {
            BoardPoint c = layout.DialCentre(m);
            float r = GateBoardLayout.Big(m) ? GateBoardLayout.ChevronRadius + 18f : layout.DialRadius(m);
            Assert.GreaterOrEqual(c.X - r, cover.X, "dial " + m);
            Assert.LessOrEqual(c.X + r, cover.X + cover.W, "dial " + m);
            Assert.LessOrEqual(c.Y + r, cover.Y + cover.H, "dial " + m);
            if (GateBoardLayout.Big(m))
            {
                Assert.Greater(c.Y - r, layout.CallPlate(0).Y + layout.CallPlate(0).H, "the chevrons stay under the call row");
                Assert.Greater(layout.CradleSlot(m, 0).Y, c.Y + r, "the cradle hangs under its dial");
            }
            if (m > 0 && GateBoardLayout.Big(m))
                Assert.Greater(c.X - layout.DialCentre(m - 1).X, 2f * (GateBoardLayout.ChevronRadius + 18f), "the dials do not touch");
        }
        for (int b = 0; b < 4; b++)
            Assert.IsTrue(layout.Rack.Contains(layout.Bay(b)));
        Assert.IsTrue(layout.OnRing(0, layout.DialCentre(0).Plus(0f, -GateBoardLayout.GlyphRadius)));
        Assert.IsFalse(layout.OnRing(0, layout.DialCentre(0)), "the hub is not the ring");
        Assert.IsTrue(layout.Cradle(1).Contains(layout.CradleSlot(1, 2).Centre));
        Assert.AreEqual(layout.CradleSlot(0, 2).Centre.X, layout.Mark(0, 3).X, 1e-3f, "the mark stands over the third bar for Ancient");
    }

    [Test]
    public void TheEraGlyphs_AreInked_AndDifferFromEachOther()
    {
        const int n = 32;
        var masks = new bool[6][];
        for (int era = -1; era < 5; era++)
        {
            masks[era + 1] = new bool[n * n];
            int inked = 0;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    if (masks[era + 1][y * n + x] = GatePaint.EraInked(era, (x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f))
                        inked++;
            Assert.Greater(inked, n * n / 10, "glyph " + era + " has ink");
            Assert.Less(inked, n * n * 3 / 4, "glyph " + era + " is not a blot");
        }
        for (int a = 0; a < 6; a++)
            for (int b = a + 1; b < 6; b++)
                Assert.Greater(Enumerable.Range(0, n * n).Count(i => masks[a][i] != masks[b][i]), n * n / 12, $"glyphs {a - 1} and {b - 1} differ");
        Assert.IsFalse(GatePaint.EraInked(7, 0f, 0f));
    }

    [Test]
    public void TheFace_IsSlateUnderTheDials_AndClearNowhere()
    {
        var layout = new GateBoardLayout(8, 4, 3);
        GateCanvas face = GatePaint.Face(layout, 5, 5);
        Assert.AreEqual((int)layout.Cover.W, face.Width);
        (byte r, byte g, byte b, byte a) mid = face.At(face.Width / 2, face.Height / 2);
        Assert.AreEqual(255, mid.a, "the cover hides the Classic jacks");
        BoardPoint hub = layout.DialCentre(0);
        (byte r, byte g, byte b, byte a) atHub = face.At((int)(hub.X - layout.Cover.X), (int)(hub.Y - layout.Cover.Y));
        Assert.Less(atHub.r + atHub.g + atHub.b, 120, "the hub is a dark well");
        BoardPoint rim = hub.Plus(0f, -(GateBoardLayout.RingOuter + GateBoardLayout.OuterRing) / 2f);
        (byte r, byte g, byte b, byte a) atRim = face.At((int)(rim.X - layout.Cover.X), (int)(rim.Y - layout.Cover.Y));
        Assert.Greater(atRim.r, atRim.b + 40, "the outer ring is brass");
    }

    [Test]
    public void TheLiveParts_ArePainted_AtTheirSizes()
    {
        GateCanvas ring = GatePaint.Ring(14);
        Assert.AreEqual((int)(2f * GateBoardLayout.RingOuter + 4f), ring.Width);
        Assert.AreEqual(0, ring.At(ring.Width / 2, ring.Height / 2).a, "the hub shows through the ring");
        Assert.AreEqual(255, ring.At(ring.Width / 2, (int)(ring.Height / 2 - GateBoardLayout.GlyphRadius)).a, "slot 0's disc at the top");
        Assert.AreEqual(255, GatePaint.Cell().At(16, 32).a);
        Assert.AreEqual(0, GatePaint.Horizon(40).At(0, 0).a);
        Assert.Greater(GatePaint.Horizon(40).At(41, 41).a, 100);
        Assert.Greater(GatePaint.Glyph(-1, 48).Pixels.Count(p => p > 0), 100);
    }
}
