using NUnit.Framework;

/// <summary>The art brief's skin swatches and hair colours (the numbers tools/characters bakes the art to).</summary>
public class CharacterSwatchesTests
{
    [Test]
    public void Skin_TheBriefsFiveSwatches_GreyOutsideTheRange()
    {
        Assert.AreEqual(((byte)0xF1, (byte)0xD3, (byte)0xC0), CharacterSwatches.Skin(1));
        Assert.AreEqual(((byte)0x5C, (byte)0x3A, (byte)0x24), CharacterSwatches.Skin(5));
        Assert.AreEqual(CharacterSwatches.Unknown, CharacterSwatches.Skin(0));
        Assert.AreEqual(CharacterSwatches.Unknown, CharacterSwatches.Skin(6));
    }

    [Test]
    public void Hair_OneColourPerToken_GreyForAWig()
    {
        foreach (string colour in LookKeys.HairColours)
            Assert.AreNotEqual(CharacterSwatches.Unknown, CharacterSwatches.Hair(colour), colour);
        Assert.AreEqual(CharacterSwatches.Unknown, CharacterSwatches.Hair(null));
    }
}
