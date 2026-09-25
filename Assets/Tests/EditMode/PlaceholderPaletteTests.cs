using NUnit.Framework;

/// <summary>Placeholder colours: HSV, skin swatches and hair colours.</summary>
public class PlaceholderPaletteTests
{
    [TestCase(0f, 255, 0, 0)]
    [TestCase(1f / 3f, 0, 255, 0)]
    [TestCase(2f / 3f, 0, 0, 255)]
    [TestCase(1f / 6f, 255, 255, 0)]
    [TestCase(0.5f, 0, 255, 255)]
    [TestCase(5f / 6f, 255, 0, 255)]
    [TestCase(1f, 255, 0, 0)]
    [TestCase(-1f / 3f, 0, 0, 255)]
    public void FromHsv_PrimariesAndSecondaries_HueWraps(float h, int r, int g, int b)
    {
        Assert.AreEqual(((byte)r, (byte)g, (byte)b), PlaceholderPalette.FromHsv(h, 1f, 1f));
    }

    [Test]
    public void FromHsv_ZeroSaturationIsGreyAtV_ZeroValueIsBlack_InputsClamp()
    {
        Assert.AreEqual(((byte)128, (byte)128, (byte)128), PlaceholderPalette.FromHsv(0.3f, 0f, 0.5f));
        Assert.AreEqual(((byte)0, (byte)0, (byte)0), PlaceholderPalette.FromHsv(0.7f, 1f, 0f));
        Assert.AreEqual(PlaceholderPalette.FromHsv(0f, 1f, 1f), PlaceholderPalette.FromHsv(0f, 5f, 3f), "s and v clamp to 1");
        Assert.AreEqual(((byte)0, (byte)0, (byte)0), PlaceholderPalette.FromHsv(0f, -1f, -2f), "clamp to 0");
    }

    [Test]
    public void Skin_TheBriefsFiveSwatches_GreyOutsideTheRange()
    {
        Assert.AreEqual(((byte)0xF1, (byte)0xD3, (byte)0xC0), PlaceholderPalette.Skin(1));
        Assert.AreEqual(((byte)0x5C, (byte)0x3A, (byte)0x24), PlaceholderPalette.Skin(5));
        Assert.AreEqual(PlaceholderPalette.Unknown, PlaceholderPalette.Skin(0));
        Assert.AreEqual(PlaceholderPalette.Unknown, PlaceholderPalette.Skin(6));
    }

    [Test]
    public void Hair_OneColourPerToken_GreyForAWig()
    {
        foreach (string colour in LookKeys.HairColours)
            Assert.AreNotEqual(PlaceholderPalette.Unknown, PlaceholderPalette.Hair(colour), colour);
        Assert.AreEqual(PlaceholderPalette.Unknown, PlaceholderPalette.Hair(null));
    }

    [Test]
    public void Darker_ScalesEveryChannel()
    {
        Assert.AreEqual(((byte)100, (byte)50, (byte)0), PlaceholderPalette.Darker((200, 100, 0), 0.5f));
        Assert.AreEqual(((byte)200, (byte)100, (byte)0), PlaceholderPalette.Darker((200, 100, 0), 2f), "clamped to 1");
    }
}
