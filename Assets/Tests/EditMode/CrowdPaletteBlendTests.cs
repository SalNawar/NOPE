using NUnit.Framework;

/// <summary>
/// The hall crowds' morning-to-evening blend (CrowdPaletteBlend, read by the art
/// office's OfficeHallCrowdPalette), and that it follows the shift clock (R3-002).
/// </summary>
public class CrowdPaletteBlendTests
{
    /// <summary>OfficeHallCrowdPalette's defaults: darken from half the shift, fully evening at 90%.</summary>
    private const float StartsAt = 0.5f;
    private const float FullAt = 0.9f;
    private const float Tolerance = 1e-5f;

    [Test]
    public void BeforeTheEveningStarts_TheCrowdsKeepTheirMorningColours()
    {
        Assert.AreEqual(0f, CrowdPaletteBlend.Evening(0f, StartsAt, FullAt));
        Assert.AreEqual(0f, CrowdPaletteBlend.Evening(0.3f, StartsAt, FullAt));
        Assert.AreEqual(0f, CrowdPaletteBlend.Evening(StartsAt, StartsAt, FullAt));
    }

    [Test]
    public void FromTheFullPoint_TheCrowdsAreInTheirEveningColours()
    {
        Assert.AreEqual(1f, CrowdPaletteBlend.Evening(FullAt, StartsAt, FullAt), Tolerance);
        Assert.AreEqual(1f, CrowdPaletteBlend.Evening(1f, StartsAt, FullAt));
    }

    [Test]
    public void Between_TheBlendEasesInAndOut()
    {
        // Smoothstep t*t*(3-2t): a quarter of the way is 0.15625, halfway 0.5, three quarters 0.84375.
        Assert.AreEqual(0.15625f, CrowdPaletteBlend.Evening(0.6f, StartsAt, FullAt), Tolerance);
        Assert.AreEqual(0.5f, CrowdPaletteBlend.Evening(0.7f, StartsAt, FullAt), Tolerance);
        Assert.AreEqual(0.84375f, CrowdPaletteBlend.Evening(0.8f, StartsAt, FullAt), Tolerance);
    }

    [Test]
    public void ProgressOutsideTheShift_IsClamped_AndNaNIsMorning()
    {
        Assert.AreEqual(0f, CrowdPaletteBlend.Evening(-1f, StartsAt, FullAt));
        Assert.AreEqual(1f, CrowdPaletteBlend.Evening(2f, StartsAt, FullAt));
        Assert.AreEqual(0f, CrowdPaletteBlend.Evening(float.NaN, StartsAt, FullAt));
    }

    [Test]
    public void AFullPointAtOrBeforeTheStart_SwitchesRightAfterTheStart()
    {
        Assert.AreEqual(0f, CrowdPaletteBlend.Evening(0.5f, 0.5f, 0.5f));
        Assert.AreEqual(1f, CrowdPaletteBlend.Evening(0.502f, 0.5f, 0.5f));
        Assert.AreEqual(1f, CrowdPaletteBlend.Evening(0.6f, 0.5f, 0.2f));
    }

    [Test]
    public void TheBlendFollowsTheShiftClock_SoClosingTimeIsAlwaysEvening()
    {
        // R3-002: the palette used to follow the visitor queue, so on day 6 (14 visitors, 6 seen by
        // closing) the crowds still wore their morning colours at 17:00. The clock alone decides now.
        var clock = new ShiftClock(9 * 60, 17 * 60, 480f); // one game minute per real second
        clock.Start();
        Assert.AreEqual(0f, CrowdPaletteBlend.Evening(clock.Progress01, StartsAt, FullAt), "09:00");

        clock.Tick(240f);
        Assert.AreEqual(0f, CrowdPaletteBlend.Evening(clock.Progress01, StartsAt, FullAt), "13:00, half the shift");

        clock.Tick(96f);
        Assert.AreEqual(0.5f, CrowdPaletteBlend.Evening(clock.Progress01, StartsAt, FullAt), Tolerance, "14:36, 70% of the shift");

        clock.Tick(96f);
        Assert.AreEqual(1f, CrowdPaletteBlend.Evening(clock.Progress01, StartsAt, FullAt), Tolerance, "16:12, 90% of the shift");

        clock.Tick(48f);
        Assert.IsTrue(clock.IsClosed);
        Assert.AreEqual(1f, CrowdPaletteBlend.Evening(clock.Progress01, StartsAt, FullAt), "17:00, closing");
    }
}
