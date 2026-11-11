using NUnit.Framework;

/// <summary>
/// The papers' real sizes and the read-close lift (doc-read-close, Saleh
/// 2026-11-11: "A: lift to read"): the table at rest, the lift's scale against
/// the type bars, the clamp inside the frame, and a pick that outlives a
/// settle. The arts' aspects are the templates' (FormLook.aspect); the type
/// shares are measured in the doc-legibility report (cap pixels over the
/// paper's pixel height at 1920x1080).
/// </summary>
public class ReadCloseTests
{
    /// <summary>One paper kind for the lift: its frame, its art's aspect, and its smallest value's and label's cap height as shares of its height on the screen (0: not measured).</summary>
    private static readonly (string name, FormFrame frame, float aspect, float value, float label)[] Kinds =
    {
        ("TC-101 passport (measured lifted in the built player, 2026-11-11: 8.9 and 7.6 px on 1376 px)", FormFrame.Booklet, 0.65435f, 8.9f / 1376f, 7.6f / 1376f),
        ("TC-240 card", FormFrame.Card, 1.75055f, 6.0f / 317.0f, 2.8f / 317.0f),
        ("TC-230 ticket", FormFrame.Ticket, 1.2938f, 7.8f / 428.9f, 4.5f / 428.9f),
        ("TC-610 permit", FormFrame.Folded, 1.08368f, 5.7f / 400.4f, 0f),
        ("TC-520 contract", FormFrame.BottomBand, 0.7728f, 7.3f / 449.0f, 3.0f / 449.0f),
    };

    /// <summary>The reading view's pixels per metre at the paper (doc-legibility REPORT 3.2): 756 at 1920x1080, 509 at 1280x1024.</summary>
    private static readonly (int w, int h, float ppm, float valueBar)[] Screens = { (1920, 1080, 756f, 11f), (1280, 1024, 509f, 9f) };

    [Test]
    public void TheSizeTable_KeepsTheRealWidthRatios_AndTheFolderIsTheLargest()
    {
        var table = new PaperRealSizes();
        float passport = table.RestWidth(FormFrame.Booklet, 0.65435f);
        Assert.AreEqual(0.125f * table.realScale, passport, 1e-5f, "the passport's art opens top to bottom: its width is 125 mm");
        (FormFrame frame, float aspect, float realWidth)[] papers =
        {
            (FormFrame.Card, 1.75055f, 86f),
            (FormFrame.Ticket, 1.2938f, 190f),
            (FormFrame.Folded, 1.08368f, 210f),
            (FormFrame.BottomBand, 0.7728f, 148f),
            (FormFrame.SideBand, 0.74991f, 148f),
        };
        // the folder keeps its built size, about 240 x 320 mm at realScale (not a knob)
        float folderW = 0.240f * table.realScale, folderH = 0.320f * table.realScale;
        foreach ((FormFrame frame, float aspect, float realWidth) in papers)
        {
            float width = table.RestWidth(frame, aspect);
            Assert.AreEqual(realWidth / 125f, width / passport, realWidth / 125f * 0.01f, $"{frame}: its width over the passport's");
            Assert.Less(width, folderW, $"{frame}: narrower than the folder");
            Assert.Less(width / aspect, folderH, $"{frame}: shorter than the folder");
        }
        Assert.Less(passport, folderW);
        Assert.Less(passport / 0.65435f, folderH);
    }

    [Test]
    public void TheLift_BringsEveryKindsSmallestValueToTheBar_AtBothScreens()
    {
        var table = new PaperRealSizes();
        foreach ((int w, int h, float ppm, float valueBar) in Screens)
            foreach ((string name, FormFrame frame, float aspect, float value, float label) in Kinds)
            {
                float heightPx = table.RestWidth(frame, aspect) / aspect * ppm, widthPx = heightPx * aspect;
                float bottom = table.bottomMargin * h;
                float magnify = ReadClose.Magnify(value * heightPx, label * heightPx, table.valueCapPx, table.labelCapPx, widthPx, heightPx, w, h, bottom, table.topCrop);
                Assert.GreaterOrEqual(value * heightPx * magnify, valueBar - 1e-3f, $"{name} at {w}x{h}: its smallest value");
                Assert.LessOrEqual(widthPx * magnify, w + 1e-2f, $"{name} at {w}x{h}: inside the frame");
                Assert.LessOrEqual(heightPx * magnify, (h - bottom) * (1f + table.topCrop) + 1e-2f, $"{name} at {w}x{h}: at most the crop share over the room");
                bool fills = widthPx * magnify >= w - 1e-2f || heightPx * magnify >= (h - bottom) * (1f + table.topCrop) - 1e-2f;
                if (w == 1920 && label > 0f)
                    Assert.IsTrue(label * heightPx * magnify >= table.labelCapPx - 1e-3f || fills, $"{name}: its smallest label reaches 8 px, or the paper fills the frame");
            }
    }

    [Test]
    public void Magnify_NeverShrinks_AndAsksNothingForNoText()
    {
        Assert.AreEqual(1f, ReadClose.Magnify(20f, 10f, 11f, 8f, 100f, 100f, 1920f, 1080f, 97f), "print already readable");
        Assert.AreEqual(1f, ReadClose.Magnify(0f, 0f, 11f, 8f, 100f, 100f, 1920f, 1080f, 97f), "no text");
        Assert.AreEqual(1f, ReadClose.Magnify(2f, 0f, 11f, 8f, 2000f, 100f, 1920f, 1080f, 97f), "a paper wider than the screen does not shrink");
        Assert.AreEqual(5.5f, ReadClose.Magnify(2f, 0f, 11f, 8f, 100f, 100f, 1920f, 1080f, 97f), 1e-4f);
        Assert.AreEqual(4f, ReadClose.Magnify(5f, 2f, 11f, 8f, 100f, 100f, 1920f, 1080f, 97f), 1e-4f, "the label asks more than the value");
        Assert.AreEqual(1.4f, ReadClose.Magnify(1f, 0f, 11f, 8f, 100f, 1000f, 1920f, 1080f, 80f, 0.4f), 1e-3f, "a tall paper may pass the room by the crop share (1000 * 1.4 px)");
    }

    [Test]
    public void TheClamp_KeepsTheLiftedPaperInsideTheFrame_AboveTheBottomBand()
    {
        const float W = 1920f, H = 1080f, Bottom = 97f;
        foreach ((float halfW, float halfH) in new[] { (100f, 80f), (500f, 400f), (900f, 491f) })
            for (float px = -200f; px <= W + 200f; px += 97f)
                for (float py = -200f; py <= H + 200f; py += 83f)
                {
                    (float x, float y) = ReadClose.Clamp(px, py, halfW, halfH, W, H, Bottom);
                    Assert.GreaterOrEqual(x - halfW, -1e-3f);
                    Assert.LessOrEqual(x + halfW, W + 1e-3f);
                    Assert.GreaterOrEqual(y - halfH, Bottom - 1e-3f, "clear of the compare readout");
                    Assert.LessOrEqual(y + halfH, H + 1e-3f);
                }
        Assert.AreEqual((400f, 500f), ReadClose.Clamp(400f, 500f, 100f, 80f, W, H, Bottom), "inside: unmoved");
        Assert.AreEqual((W / 2f, Bottom + 600f), ReadClose.Clamp(10f, 10f, 1000f, 600f, W, H, Bottom), "too tall: on the bottom band, top cropped");
    }

    [Test]
    public void APick_SurvivesASettle_AndPairsWithAValueOnTheNextLiftedPaper()
    {
        var turn = new ReadTurn();
        var pair = new ComparePair();
        Assert.IsTrue(turn.Want(0, true), "paper 0 lifts");
        Assert.AreEqual(CompareStep.Pending, pair.Select(new ComparePick(PickKeys.Field(0, 2), "a", "x", default)));
        Assert.IsFalse(turn.Want(0, false), "paper 0 settles");
        Assert.IsTrue(turn.Want(1, true), "paper 1 lifts");
        Assert.IsTrue(pair.HasA, "the pick stays picked");
        Assert.AreEqual(PickKeys.Field(0, 2), pair.A.Key);
        Assert.AreEqual(CompareStep.Paired, pair.Select(new ComparePick(PickKeys.Field(1, 3), "b", "y", default)), "a pair across two papers");
        Assert.AreEqual(PickKeys.Field(0, 2), pair.A.Key);
    }

    [Test]
    public void OnePaperIsLiftedAtATime()
    {
        var turn = new ReadTurn();
        Assert.AreEqual(-1, turn.Lifted);
        turn.Want(4, true);
        turn.Want(7, true);
        Assert.AreEqual(7, turn.Lifted, "the next paper takes the lift");
        Assert.IsFalse(turn.Want(4, false), "the one before lets go of nothing");
        Assert.AreEqual(7, turn.Lifted);
        turn.Want(7, false);
        Assert.AreEqual(-1, turn.Lifted);
    }

    [Test]
    public void APickedPaperSettlesAndLiftsAgainOnlyAfterThePointerLeaves()
    {
        var turn = new ReadTurn();
        Assert.IsTrue(turn.Want(3, true));
        turn.Settle(3);
        Assert.AreEqual(-1, turn.Lifted, "the pick puts the paper down");
        Assert.IsFalse(turn.Want(3, true), "the pointer still on it: no new lift");
        Assert.IsTrue(turn.Want(5, true), "another paper may lift");
        turn.Leave(3);
        Assert.IsTrue(turn.Want(3, true), "left and back: it lifts again");
    }
}
