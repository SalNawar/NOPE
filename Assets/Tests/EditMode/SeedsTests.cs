using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class SeedsTests
{
    [TestCase(12345, 3, 4887720)]
    [TestCase(-7, 1, -5174)]
    [TestCase(int.MaxValue, 30, 2147245681)]
    public void Day_MatchesTheOriginalRunManagerFormula(int runSeed, int day, int expected)
    {
        Assert.AreEqual(expected, Seeds.Day(runSeed, day));
    }

    [Test]
    public void Day_DiffersFromDayToDay()
    {
        var seeds = Enumerable.Range(1, 30).Select(d => Seeds.Day(12345, d)).ToList();
        Assert.AreEqual(seeds.Count, seeds.Distinct().Count());
    }

    [Test]
    public void ForCase_DiffersFromCaseToCase_AndFromTheDaySeed()
    {
        int daySeed = Seeds.Day(12345, 1);
        var seeds = new HashSet<int>(Enumerable.Range(1, 20).Select(c => Seeds.ForCase(daySeed, c)));
        Assert.AreEqual(20, seeds.Count);
        Assert.IsFalse(seeds.Contains(daySeed));
    }

    [Test]
    public void CaseStream_NeverReplaysTheRawDaySeedStream()
    {
        int daySeed = Seeds.Day(777, 2);
        var raw = new SeededRandom(daySeed);
        var firstCase = new SeededRandom(Seeds.ForCase(daySeed, 1));
        var a = Enumerable.Range(0, 10).Select(_ => raw.Range(0, 1000000)).ToArray();
        var b = Enumerable.Range(0, 10).Select(_ => firstCase.Range(0, 1000000)).ToArray();
        CollectionAssert.AreNotEqual(a, b);
    }

    [Test]
    public void Mix_IsDeterministic_AndSaltSensitive()
    {
        Assert.AreEqual(Seeds.Mix(5, 9), Seeds.Mix(5, 9));
        Assert.AreNotEqual(Seeds.Mix(5, 9), Seeds.Mix(5, 10));
        Assert.AreNotEqual(Seeds.Mix(5, 9), Seeds.Mix(6, 9));
    }
}
