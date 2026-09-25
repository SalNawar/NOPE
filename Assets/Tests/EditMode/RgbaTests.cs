using NUnit.Framework;

/// <summary>Theme colours parse from the hex written in world_source.json.</summary>
public class RgbaTests
{
    private const float Tolerance = 0.0001f;

    [Test]
    public void TryParseHex_ReadsRgb_WithOpaqueAlpha()
    {
        Assert.IsTrue(Rgba.TryParseHex("#ECE9D8", out Rgba c));
        Assert.AreEqual(236f / 255f, c.R, Tolerance);
        Assert.AreEqual(233f / 255f, c.G, Tolerance);
        Assert.AreEqual(216f / 255f, c.B, Tolerance);
        Assert.AreEqual(1f, c.A, Tolerance);
    }

    [Test]
    public void TryParseHex_ReadsAlpha_AndLowerCase()
    {
        Assert.IsTrue(Rgba.TryParseHex("#FFFFFFB3", out Rgba c));
        Assert.AreEqual(179f / 255f, c.A, Tolerance);
        Assert.IsTrue(Rgba.TryParseHex("#ece9d8", out Rgba lower));
        Assert.AreEqual(236f / 255f, lower.R, Tolerance);
    }

    [TestCase("ECE9D8", Description = "missing #")]
    [TestCase("#ECE9D", Description = "wrong length")]
    [TestCase("#ECE9D8F", Description = "wrong length")]
    [TestCase("#ECE9DG", Description = "a non-hex digit")]
    [TestCase("", Description = "empty")]
    [TestCase(null)]
    public void TryParseHex_RejectsEverythingElse(string hex)
    {
        Assert.IsFalse(Rgba.TryParseHex(hex, out _));
    }

    [Test]
    public void WithAlpha_KeepsTheColour()
    {
        Rgba c = new Rgba(0.1f, 0.2f, 0.3f).WithAlpha(0.5f);
        Assert.AreEqual(0.1f, c.R, Tolerance);
        Assert.AreEqual(0.3f, c.B, Tolerance);
        Assert.AreEqual(0.5f, c.A, Tolerance);
    }
}
