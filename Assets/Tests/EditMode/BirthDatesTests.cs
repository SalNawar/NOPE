using NUnit.Framework;

public class BirthDatesTests
{
    [TestCase(12, 2, 830, "12 Mar 830")]
    [TestCase(3, 5, -1450, "3 Jun 1450 BCE")]
    [TestCase(28, 11, 1962, "28 Dec 1962")]
    public void Format_WritesDayMonthYear_WithBceForNegativeYears(int day, int month, int year, string expected)
    {
        Assert.AreEqual(expected, BirthDates.Format(day, month, year));
    }

    [TestCase("12 Mar 830", 12, 2, 830)]
    [TestCase("3 Jun 1450 BCE", 3, 5, -1450)]
    public void TryParse_ReadsBothForms(string text, int day, int month, int year)
    {
        Assert.IsTrue(BirthDates.TryParse(text, out int d, out int m, out int y));
        Assert.AreEqual(day, d);
        Assert.AreEqual(month, m);
        Assert.AreEqual(year, y);
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("12 Foo 830")]
    [TestCase("1 Jan")]
    [TestCase("1 Jan 5 BC")]
    [TestCase("x Jan 5")]
    [TestCase("1 Jan 0")]
    public void TryParse_RejectsMalformedText(string text)
    {
        Assert.IsFalse(BirthDates.TryParse(text, out _, out _, out _));
    }

    [Test]
    public void Generate_StaysInRange_AndNeverProducesYearZero()
    {
        var rng = new SeededRandom(11);
        for (int i = 0; i < 2000; i++)
        {
            string date = BirthDates.Generate(-5, 5, rng);
            Assert.IsTrue(BirthDates.TryParse(date, out int d, out int m, out int y), date);
            Assert.That(y, Is.InRange(-5, 5));
            Assert.AreNotEqual(0, y);
            Assert.That(d, Is.InRange(1, 28));
            Assert.That(m, Is.InRange(0, 11));
        }
    }

    [Test]
    public void Generate_BceRange_WritesBce()
    {
        var rng = new SeededRandom(3);
        for (int i = 0; i < 200; i++)
            StringAssert.EndsWith(" BCE", BirthDates.Generate(-1520, -1452, rng));
    }

    [Test]
    public void Generate_ReversedRange_IsTolerated()
    {
        Assert.IsTrue(BirthDates.TryParse(BirthDates.Generate(900, 800, new SeededRandom(1)), out _, out _, out int y));
        Assert.That(y, Is.InRange(800, 900));
    }

    [Test]
    public void Forge_ShiftsTheYearWithinTheShiftRange_KeepingDayAndMonth()
    {
        var rng = new SeededRandom(5);
        for (int i = 0; i < 500; i++)
        {
            string forged = BirthDates.Forge("9 Apr 1843", 2, 24, 1780, 1900, rng);
            Assert.IsTrue(BirthDates.TryParse(forged, out int d, out int m, out int y), forged);
            Assert.AreEqual(9, d);
            Assert.AreEqual(3, m);
            Assert.That(System.Math.Abs(y - 1843), Is.InRange(2, 24));
        }
    }

    [Test]
    public void Forge_StaysInsideTheBirthRange_SoTheTravellerIsNeverBornAfterTheirMoment()
    {
        // Abbasid Baghdad (830): born 760..812. A true 812 may only be forged downwards.
        var rng = new SeededRandom(11);
        var years = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < 500; i++)
        {
            Assert.IsTrue(BirthDates.TryParse(BirthDates.Forge("5 May 812", 2, 24, 760, 812, rng), out _, out _, out int y));
            Assert.That(y, Is.InRange(788, 810));
            years.Add(y);
        }
        Assert.Greater(years.Count, 10);
    }

    [Test]
    public void Forge_WhenNoShiftFitsTheRange_StillForgesWithinTheShiftRange()
    {
        var rng = new SeededRandom(3);
        Assert.IsTrue(BirthDates.TryParse(BirthDates.Forge("5 May 812", 2, 24, 812, 812, rng), out _, out _, out int y));
        Assert.That(System.Math.Abs(y - 812), Is.InRange(2, 24));
    }

    [Test]
    public void Forge_AroundYearOne_NeverWritesYearZero()
    {
        var rng = new SeededRandom(8);
        for (int i = 0; i < 500; i++)
        {
            Assert.IsTrue(BirthDates.TryParse(BirthDates.Forge("1 Jan 3", 2, 24, -30, 30, rng), out _, out _, out int y));
            Assert.AreNotEqual(0, y);
            Assert.AreNotEqual(3, y);
        }
    }

    [Test]
    public void Forge_AcrossYearZero_CountsTheMissingYear()
    {
        // 2 CE shifted back by 2 is 1 BCE (-1), because there is no year 0.
        var rng = new SeededRandom(1);
        Assert.IsTrue(BirthDates.TryParse(BirthDates.Forge("1 Jan 2", 2, 2, -5, 1, rng), out _, out _, out int y));
        Assert.AreEqual(-1, y);
    }

    [Test]
    public void Forge_UnreadableDate_IsMarked()
    {
        Assert.AreEqual("sometime (?)", BirthDates.Forge("sometime", 2, 24, 0, 0, new SeededRandom(1)));
    }

    [Test]
    public void PickOtherYear_KeepsDayAndMonth_AndStaysInTheRange()
    {
        var rng = new SeededRandom(5);
        for (int i = 0; i < 500; i++)
        {
            string date = BirthDates.PickOtherYear("9 Apr 1843", 1780, 1900, rng);
            Assert.IsTrue(BirthDates.TryParse(date, out int d, out int m, out int y), date);
            Assert.AreEqual(9, d);
            Assert.AreEqual(3, m);
            Assert.That(y, Is.InRange(1780, 1900));
        }
    }

    [Test]
    public void PickOtherYear_NeverTakesTheCoverYear_WhenTheRangesOverlap()
    {
        // Early-modern Egypt and Greece both span 1630..1682.
        var rng = new SeededRandom(11);
        var years = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < 2000; i++)
        {
            Assert.IsTrue(BirthDates.TryParse(BirthDates.PickOtherYear("5 May 1650", 1630, 1682, rng), out _, out _, out int y));
            Assert.AreNotEqual(1650, y);
            years.Add(y);
        }
        Assert.Greater(years.Count, 40);
    }

    [Test]
    public void PickOtherYear_AcrossTheBceCeBoundary_NeverWritesYearZero()
    {
        var rng = new SeededRandom(8);
        for (int i = 0; i < 500; i++)
        {
            Assert.IsTrue(BirthDates.TryParse(BirthDates.PickOtherYear("1 Jan 3", -30, 30, rng), out _, out _, out int y));
            Assert.AreNotEqual(0, y);
            Assert.AreNotEqual(3, y);
        }
    }

    [Test]
    public void PickOtherYear_WithOneCandidateLeft_AlwaysTakesIt()
    {
        var rng = new SeededRandom(3);
        for (int i = 0; i < 50; i++)
            Assert.AreEqual("5 May 811", BirthDates.PickOtherYear("5 May 812", 811, 812, rng));
    }

    [Test]
    public void PickOtherYear_ReversedBounds_AreTolerated()
    {
        Assert.IsTrue(BirthDates.TryParse(BirthDates.PickOtherYear("5 May 850", 900, 800, new SeededRandom(1)), out _, out _, out int y));
        Assert.That(y, Is.InRange(800, 900));
        Assert.AreNotEqual(850, y);
    }

    [Test]
    public void PickOtherYear_UsesOneDraw_IndexingTheEligibleYearsInAscendingOrder()
    {
        // 1630..1634 without the cover year 1632: 1630, 1631, 1633, 1634.
        var rng = new ScriptedRandom(ScriptStep.Range(2));
        Assert.AreEqual("5 May 1633", BirthDates.PickOtherYear("5 May 1632", 1630, 1634, rng));
        Assert.IsTrue(rng.Done);

        // 2 BCE..2 CE without year 0 and the cover year 1 CE: 2 BCE, 1 BCE, 2 CE.
        var zero = new ScriptedRandom(ScriptStep.Range(2));
        Assert.AreEqual("1 Jan 2", BirthDates.PickOtherYear("1 Jan 1", -2, 2, zero));
        Assert.IsTrue(zero.Done);
    }

    [Test]
    public void PickOtherYear_WithNoOtherYear_ReturnsNull_WithoutDrawing()
    {
        var rng = new ScriptedRandom();
        Assert.IsNull(BirthDates.PickOtherYear("Unknown", 1630, 1682, rng));
        Assert.IsNull(BirthDates.PickOtherYear("5 May 1650", 0, 0, rng));
        Assert.IsNull(BirthDates.PickOtherYear("5 May 1650", 1650, 1650, rng));
        Assert.AreEqual(0, rng.Draws);
    }

    [TestCase("Unknown", 1630, 1682, false)]
    [TestCase("5 May 1650", 0, 0, false)]
    [TestCase("5 May 1650", 1650, 1650, false)]
    [TestCase("1 Jan 1", 0, 1, false)]
    [TestCase("5 May 1650", 1630, 1682, true)]
    [TestCase("3 Jun 1450 BCE", -1460, -1440, true)]
    public void HasOtherYear_DecisionTable(string cover, int yearMin, int yearMax, bool expected)
    {
        Assert.AreEqual(expected, BirthDates.HasOtherYear(cover, yearMin, yearMax));
    }
}
