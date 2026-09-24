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
    public void Forge_ShiftsTheYearByTwoToTwentyFour_KeepingDayAndMonth()
    {
        var rng = new SeededRandom(5);
        for (int i = 0; i < 500; i++)
        {
            string forged = BirthDates.Forge("9 Apr 1843", rng);
            Assert.IsTrue(BirthDates.TryParse(forged, out int d, out int m, out int y), forged);
            Assert.AreEqual(9, d);
            Assert.AreEqual(3, m);
            Assert.That(System.Math.Abs(y - 1843), Is.InRange(2, 24));
        }
    }

    [Test]
    public void Forge_AroundYearOne_NeverWritesYearZero()
    {
        var rng = new SeededRandom(8);
        for (int i = 0; i < 500; i++)
        {
            Assert.IsTrue(BirthDates.TryParse(BirthDates.Forge("1 Jan 3", rng), out _, out _, out int y));
            Assert.AreNotEqual(0, y);
            Assert.AreNotEqual(3, y);
        }
    }

    [Test]
    public void Forge_UnreadableDate_IsMarked()
    {
        Assert.AreEqual("sometime (?)", BirthDates.Forge("sometime", new SeededRandom(1)));
    }
}
