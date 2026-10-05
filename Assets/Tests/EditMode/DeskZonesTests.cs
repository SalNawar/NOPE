using NUnit.Framework;

/// <summary>The desk's counter and reading area in numbers (Papers, Please's zones, Saleh 2026-10-06).</summary>
public class DeskZonesTests
{
    private const float Eps = 1e-5f;

    [Test]
    public void TheCounter_IsTheStripAtTheFarEdge()
    {
        Assert.IsTrue(DeskZones.OnCounter(0.3f, 0.3f, 0.14f), "the far edge");
        Assert.IsTrue(DeskZones.OnCounter(0.17f, 0.3f, 0.14f), "inside the strip");
        Assert.IsFalse(DeskZones.OnCounter(0.15f, 0.3f, 0.14f), "nearer: the desk");
        Assert.IsFalse(DeskZones.OnCounter(-0.2f, 0.3f, 0.14f));
    }

    [Test]
    public void PapersHandedOver_LineUpAlongTheCountersMiddleLine()
    {
        (float x0, float y0) = DeskZones.CounterSpot(0, 3, 0f, -0.5f, 0.5f, 0.3f, 0.14f, 0.16f);
        (float x1, float y1) = DeskZones.CounterSpot(1, 3, 0f, -0.5f, 0.5f, 0.3f, 0.14f, 0.16f);
        (float x2, _) = DeskZones.CounterSpot(2, 3, 0f, -0.5f, 0.5f, 0.3f, 0.14f, 0.16f);
        Assert.AreEqual(0.23f, y0, Eps, "the strip's middle line");
        Assert.AreEqual(y0, y1, Eps);
        Assert.AreEqual(-0.16f, x0, Eps);
        Assert.AreEqual(0f, x1, Eps, "centred on the middle");
        Assert.AreEqual(0.16f, x2, Eps);
        Assert.AreEqual(x0, DeskZones.CounterSpot(3, 3, 0f, -0.5f, 0.5f, 0.3f, 0.14f, 0.16f).x, Eps, "the spots are reused in turn");
        Assert.AreEqual(0f, DeskZones.CounterSpot(0, 1, 0f, -0.5f, 0.5f, 0.3f, 0.14f, 0.16f).x, Eps, "one paper: the middle");
    }

    [Test]
    public void TheRow_StaysOnTheCounter_NarrowerWhenItMust()
    {
        (float x0, _) = DeskZones.CounterSpot(0, 3, 0.45f, -0.5f, 0.5f, 0.3f, 0.14f, 0.16f);
        (float x2, _) = DeskZones.CounterSpot(2, 3, 0.45f, -0.5f, 0.5f, 0.3f, 0.14f, 0.16f);
        Assert.AreEqual(0.5f, x2, Eps, "moved back inside");
        Assert.AreEqual(0.18f, x0, Eps);
        (float n0, _) = DeskZones.CounterSpot(0, 5, 0f, -0.1f, 0.1f, 0.3f, 0.14f, 0.16f);
        (float n4, _) = DeskZones.CounterSpot(4, 5, 0f, -0.1f, 0.1f, 0.3f, 0.14f, 0.16f);
        Assert.AreEqual(-0.1f, n0, Eps, "squeezed");
        Assert.AreEqual(0.1f, n4, Eps);
    }

    [Test]
    public void OnTheDesk_EveryPaperReadsAtOneHeight_AWidePaperByItsWidth()
    {
        Assert.AreEqual(0.34f / 0.218f, DeskZones.ReadingScale(0.144f, 0.218f, 0.34f, 0.765f), 1e-4f, "a passport by its height");
        float ticket = DeskZones.ReadingScale(0.195f, 0.17f, 0.34f, 0.765f);
        Assert.AreEqual(0.34f * 0.765f, 0.195f * ticket, 1e-4f, "a ticket wider than the desk paper by its width");
        Assert.AreEqual(1f, DeskZones.ReadingScale(0f, 0f, 0.34f, 0.765f), "no size");
    }

    [Test]
    public void Ease_IsSmoothstep_Clamped()
    {
        Assert.AreEqual(0f, DeskZones.Ease(-1f));
        Assert.AreEqual(0.5f, DeskZones.Ease(0.5f), Eps);
        Assert.AreEqual(1f, DeskZones.Ease(2f));
    }
}
