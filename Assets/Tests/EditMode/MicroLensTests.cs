using NUnit.Framework;

/// <summary>
/// Reading a paper's microprint (FIX_PLAN_v2 TOOLS, BUG-2b; Saleh D5: the
/// magnifier is "not sold", so "the scanner's hidden-layer view reveals the
/// microprint of the ReprintedPaper lie"): a read is credited only once the
/// player has opened the paper's hidden layer and the line rendered there
/// large enough to read, measured from the drawn glyphs; the scan view sizes
/// the line to reach that.
/// </summary>
public class MicroLensTests
{
    [Test]
    public void Readable_OnlyWithTheLayerOpen_AndTheLineDrawnAtTheCapOrMore()
    {
        Assert.IsTrue(MicroLens.Readable(true, ScanMicroprint.MinCapPx), "the layer open, the capitals 12 px tall");
        Assert.IsTrue(MicroLens.Readable(true, 18f));
        Assert.IsFalse(MicroLens.Readable(true, 11.9f), "drawn too small to read: no credit (1019a credited a 0.024 px line by geometry)");
        Assert.IsFalse(MicroLens.Readable(false, 40f), "the layer closed: nothing was read, however large it would draw");
        Assert.IsFalse(MicroLens.Readable(true, 0f), "not drawn (the PC closed, the row culled): no credit");
        Assert.IsFalse(MicroLens.Readable(true, float.NaN));
    }

    [Test]
    public void FontSize_ReachesTheCapWithHeadroom_WhereverTheViewDrawsIt()
    {
        // A UI font whose capitals are 0.7 of its size, drawn at 0.667 px per unit (the 1920x1080 canvas at 1280x720).
        float size = ScanMicroprint.FontSize(0.7f, 0.667f);
        float cap = ScanMicroprint.CapPx(size, 0.7f, 0.667f);
        Assert.AreEqual(ScanMicroprint.MinCapPx * ScanMicroprint.Headroom, cap, 1e-3f, "the capitals land on the cap with headroom");
        Assert.IsTrue(MicroLens.Readable(true, cap));

        float shrunk = ScanMicroprint.FontSize(0.7f, 0.25f);
        Assert.Greater(shrunk, size, "a window drawn smaller asks for a larger size, so the line still reads");
        Assert.AreEqual(ScanMicroprint.MaxFontSize, ScanMicroprint.FontSize(0.7f, 0f), "nothing measured yet: the largest size, never infinity");
        Assert.AreEqual(ScanMicroprint.MaxFontSize, ScanMicroprint.FontSize(0f, 1f), "no font metrics: the largest size");
    }

    [Test]
    public void Text_IsOneRepeatOfTheLine_TheMarkTheFormAndTheId()
    {
        var line = new MicroLine(3, "4471-208");
        string text = ScanMicroprint.Text(line, "TC-101");
        StringAssert.Contains("TCB", text);
        StringAssert.Contains("TC-101", text);
        StringAssert.Contains("4471-208", text, "the Citizen ID the line repeats: what the player holds against the box");
        Assert.AreEqual(text.Trim(), text, "no trailing space");
        Assert.IsFalse(text.EndsWith("·"), "no dangling dot after the ID");
        StringAssert.StartsWith(text, line.Printed("TC-101", 1), "the line the paper prints, once");
    }
}
