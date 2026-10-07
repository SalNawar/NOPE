using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Saleh's playtest 2026-10-07 ("documents overlap"): one stacking rule for
/// every part of a paper (PaperLayers) and the spread of the papers landing on
/// the desk (PaperSpread), and the counter's staggered second row (DeskZones).
/// </summary>
public class PaperSpreadTests
{
    private const float Eps = 1e-5f;

    [Test]
    public void EveryPartOfAPaper_LiesUnderTheNextPaper()
    {
        float last = 0f;
        foreach (float lift in PaperLayers.Order)
        {
            Assert.Greater(lift, last, "the parts lie in their order");
            Assert.LessOrEqual(lift, PaperLayers.PartsDepth, "each part under the parts' depth");
            last = lift;
        }
        Assert.Greater(PaperLayers.StackStep(0.0002f), PaperLayers.PartsDepth, "even the least step the config allows keeps the paper above clear of every part of the one below");
        Assert.AreEqual(0.0015f, PaperLayers.StackStep(0.0015f), Eps, "a roomier step stays as configured");
    }

    [Test]
    public void TheFirstPaper_LiesWhereItIsPreferred()
    {
        var area = new DeskRect(0f, 0f, 1f, 0.6f);
        (float x, float y) = PaperSpread.Place(area, 0.3f, 0.4f, new List<SpreadTaken>(), 0.1f, 0.05f);
        Assert.AreEqual(0.1f, x, Eps);
        Assert.AreEqual(0.05f, y, Eps);
    }

    [Test]
    public void TheNextPaper_LiesBeside_WhenTheDeskHasRoom()
    {
        var area = new DeskRect(0f, 0f, 1f, 0.6f);
        var first = new DeskRect(-0.25f, 0f, 0.3f, 0.4f);
        var taken = new List<SpreadTaken> { new SpreadTaken(first, PaperSpread.Keys(first.CentreX, first.CentreY, 0.3f, 0.4f, new[] { (0f, 0f, 1f, 0.2f) }), 1f) };
        (float x, float y) = PaperSpread.Place(area, 0.3f, 0.4f, taken, -0.25f, 0f);
        Assert.AreEqual(0f, PaperSpread.Overlap(new DeskRect(x, y, 0.3f, 0.4f), first), Eps, "no overlap where the desk has room");
    }

    [Test]
    public void OnACrowdedDesk_ThePapersOverlapButKeepTheirHeadersAndPhotos()
    {
        var area = new DeskRect(0f, 0f, 0.9f, 0.5f);
        var taken = new List<SpreadTaken>();
        var placed = new List<DeskRect>();
        var shares = new[] { (0f, 0f, 1f, 0.2f), (0.05f, 0.3f, 0.35f, 0.3f) };
        for (int k = 0; k < 3; k++)
        {
            (float x, float y) = PaperSpread.Place(area, 0.3f, 0.4f, taken, 0f, 0f);
            var at = new DeskRect(x, y, 0.3f, 0.4f);
            foreach (SpreadTaken t in taken)
                foreach (DeskRect key in t.Keys)
                    Assert.Less(PaperSpread.Overlap(at, key), 0.25f * key.Width * key.Height, $"paper {k} leaves most of each earlier header and photo in view");
            taken.Add(new SpreadTaken(at, PaperSpread.Keys(x, y, 0.3f, 0.4f, shares), 1f));
            placed.Add(at);
        }
        Assert.AreNotEqual(placed[0].CentreX, placed[1].CentreX, "a staggered fan, not a pile");
    }

    [Test]
    public void TheSpread_AvoidsTheRulebookFolder()
    {
        var area = new DeskRect(0f, 0f, 1f, 0.6f);
        var folder = new DeskRect(-0.3f, 0f, 0.4f, 0.6f);
        (float x, float y) = PaperSpread.Place(area, 0.3f, 0.4f, new List<SpreadTaken> { new SpreadTaken(folder, null, 3f) }, -0.3f, 0f);
        Assert.AreEqual(0f, PaperSpread.Overlap(new DeskRect(x, y, 0.3f, 0.4f), folder), Eps);
    }

    [Test]
    public void KeyShares_MapFromThePapersTopLeft()
    {
        List<DeskRect> keys = PaperSpread.Keys(1f, 2f, 0.4f, 0.5f, new[] { (0f, 0f, 1f, 0.2f) });
        Assert.AreEqual(1f, keys[0].CentreX, Eps);
        Assert.AreEqual(2f + 0.25f - 0.05f, keys[0].CentreY, Eps, "the header band is the far edge's");
        Assert.AreEqual(0.1f, keys[0].Height, Eps);
    }

    [Test]
    public void PapersPastTheCountersSpots_LieInAStaggeredRowNearer()
    {
        (float x0, float y0) = DeskZones.CounterSpot(0, 3, 0f, -0.5f, 0.5f, 0.3f, 0.07f, 0.16f, 0.05f);
        (float x3, float y3) = DeskZones.CounterSpot(3, 3, 0f, -0.5f, 0.5f, 0.3f, 0.07f, 0.16f, 0.05f);
        Assert.AreEqual(x0 + 0.08f, x3, Eps, "half a spot across");
        Assert.AreEqual(y0 - 0.05f, y3, Eps, "a row nearer");
        Assert.AreEqual(x0, DeskZones.CounterSpot(3, 3, 0f, -0.5f, 0.5f, 0.3f, 0.07f, 0.16f).x, Eps, "no row depth: the same spots as before");
    }
}
