using NUnit.Framework;

/// <summary>
/// A passport's machine-readable zone (the travel documents spec, TD3):
/// MachineZone's two lines of 36 repeat the page in ICAO 9303's TD2 style,
/// with its check digits.
/// </summary>
public class MachineZoneTests
{
    [Test]
    public void TheCheckDigit_IsIcaos_SevenThreeOne()
    {
        Assert.AreEqual('6', MachineZone.Check("L898902C3"), "ICAO 9303's specimen passport number");
        Assert.AreEqual('2', MachineZone.Check("740812"), "its birth date");
        Assert.AreEqual('9', MachineZone.Check("120415"), "its expiry");
        Assert.AreEqual('0', MachineZone.Check("<<<<<<"));
    }

    [Test]
    public void ADate_ReadsAsYymmdd_AndOneThatDoesNotReadIsFiller()
    {
        Assert.AreEqual("010503", MachineZone.Date("3 May 2101"));
        Assert.AreEqual("660208", MachineZone.Date("8 Feb 466 BCE"));
        Assert.AreEqual("<<<<<<", MachineZone.Date("Unknown"));
        Assert.AreEqual("<<<<<<", MachineZone.Date(null));
    }

    [Test]
    public void TheLines_AreThirtySixLong_AndRepeatThePage()
    {
        var lines = MachineZone.Lines("EGY", "Lysimachē of Athens", "C-4471-0213", "3 May 2101", "9 Jun 2150");
        Assert.AreEqual(2, lines.Count);
        Assert.AreEqual(MachineZone.LineLength, lines[0].Length);
        Assert.AreEqual(MachineZone.LineLength, lines[1].Length);
        StringAssert.StartsWith("P<EGYLYSIMACHE<OF<ATHENS<", lines[0], "capitals without accents, words parted by the filler");
        StringAssert.StartsWith("C44710213", lines[1], "the number without its dashes");
        Assert.AreEqual(MachineZone.Check("C44710213"), lines[1][9]);
        Assert.AreEqual("EGY", lines[1].Substring(10, 3));
        Assert.AreEqual("010503", lines[1].Substring(13, 6));
        Assert.AreEqual('<', lines[1][20], "the sex is not printed");
        Assert.AreEqual("500609", lines[1].Substring(21, 6));
        foreach (char c in lines[0] + lines[1])
            Assert.IsTrue((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '<', $"'{c}' is a zone character");
    }

    [Test]
    public void AMissingCodeOrNumber_IsFiller_AndTheLinesKeepTheirLength()
    {
        var lines = MachineZone.Lines(null, null, null, null, null);
        Assert.AreEqual(MachineZone.LineLength, lines[0].Length);
        Assert.AreEqual(MachineZone.LineLength, lines[1].Length);
        StringAssert.StartsWith("P<<<<", lines[0]);
    }
}
