using NUnit.Framework;

/// <summary>The daters' outline face (the desk machine spec §1): the letters of APPROVED and DENIED are drawn as signed distances; any other letter falls back to the font.</summary>
public class DaterLettersTests
{
    [Test]
    public void TheVerdictWords_AreAllDrawn()
    {
        foreach (char c in "APPROVEDDENIED")
        {
            Assert.IsTrue(DaterLetters.Has(c), c.ToString());
            Assert.Greater(DaterLetters.Width(c), 0.3f, c.ToString());
        }
        Assert.IsTrue(DaterLetters.Has('a'), "lower case reads as its capital");
    }

    [Test]
    public void AnotherLetter_FallsBackToTheFont()
    {
        Assert.IsFalse(DaterLetters.Has('Z'));
        Assert.AreEqual(0f, DaterLetters.Width('Z'));
        Assert.Greater(DaterLetters.Distance('Z', 0.3f, 0.5f), 1f);
    }

    [TestCase('I', 0.22f, 0.5f, Description = "the stem")]
    [TestCase('E', 0.17f, 0.5f, Description = "the stem")]
    [TestCase('D', 0.17f, 0.5f, Description = "the stem")]
    [TestCase('P', 0.17f, 0.2f, Description = "the stem")]
    [TestCase('R', 0.17f, 0.2f, Description = "the stem")]
    [TestCase('N', 0.16f, 0.5f, Description = "the left stem")]
    [TestCase('O', 0.11f, 0.5f, Description = "the left side")]
    [TestCase('A', 0.42f, 0.95f, Description = "the apex")]
    [TestCase('V', 0.44f, 0.04f, Description = "the foot")]
    public void AStroke_IsInsideTheLetter(char c, float x, float y)
    {
        Assert.Less(DaterLetters.Distance(c, x, y), 0f);
    }

    [TestCase('O', 0.41f, 0.5f, Description = "the counter")]
    [TestCase('D', 0.45f, 0.5f, Description = "the counter")]
    [TestCase('P', 0.42f, 0.7f, Description = "the bowl's counter")]
    [TestCase('A', 0.43f, 0.6f, Description = "the counter over the bar")]
    [TestCase('E', 0.5f, 0.75f, Description = "between the arms")]
    public void ACounter_IsOutsideTheLetter(char c, float x, float y)
    {
        Assert.Greater(DaterLetters.Distance(c, x, y), 0f);
    }

    [Test]
    public void FarAway_IsOutside_ByAboutTheDistance()
    {
        Assert.AreEqual(1f, DaterLetters.Distance('I', 0.22f, 2f), 0.01f, "a cap height above the top serif");
    }
}
