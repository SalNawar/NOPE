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
    public void ViolatorAndClueStreams_AreDistinctFromEveryCaseStream()
    {
        int daySeed = Seeds.Day(12345, 2);
        var cases = new HashSet<int>(Enumerable.Range(1, 20).Select(c => Seeds.ForCase(daySeed, c)));
        Assert.IsFalse(cases.Contains(Seeds.ForViolators(daySeed)));
        Assert.IsFalse(cases.Contains(Seeds.ForClues(Seeds.ForCase(daySeed, 1))));
        Assert.AreEqual(Seeds.ForViolators(daySeed), Seeds.ForViolators(daySeed));
    }

    [Test]
    public void LieStream_IsDistinctFromCaseClueAndViolatorStreams()
    {
        int daySeed = Seeds.Day(12345, 2);
        var cases = new HashSet<int>(Enumerable.Range(1, 20).Select(c => Seeds.ForCase(daySeed, c)));
        var lies = new HashSet<int>();
        for (int c = 1; c <= 20; c++)
        {
            int caseSeed = Seeds.ForCase(daySeed, c);
            int lie = Seeds.ForLies(caseSeed);
            Assert.IsFalse(cases.Contains(lie), $"case {c}: the lie seed is a case seed");
            Assert.AreNotEqual(Seeds.ForClues(caseSeed), lie, $"case {c}: lie seed equals the clue seed");
            Assert.AreNotEqual(Seeds.ForViolators(daySeed), lie, $"case {c}: lie seed equals the violator seed");
            Assert.AreEqual(lie, Seeds.ForLies(caseSeed), $"case {c}: not deterministic");
            lies.Add(lie);
        }
        Assert.AreEqual(20, lies.Count, "lie seeds repeat across cases");
    }

    [Test]
    public void DialogStream_IsDistinctFromCaseClueLieAndViolatorStreams()
    {
        int daySeed = Seeds.Day(12345, 2);
        var cases = new HashSet<int>(Enumerable.Range(1, 20).Select(c => Seeds.ForCase(daySeed, c)));
        var dialogs = new HashSet<int>();
        for (int c = 1; c <= 20; c++)
        {
            int caseSeed = Seeds.ForCase(daySeed, c);
            int dialog = Seeds.ForDialog(caseSeed);
            Assert.IsFalse(cases.Contains(dialog), $"case {c}: the dialog seed is a case seed");
            Assert.AreNotEqual(Seeds.ForClues(caseSeed), dialog, $"case {c}: dialog seed equals the clue seed");
            Assert.AreNotEqual(Seeds.ForLies(caseSeed), dialog, $"case {c}: dialog seed equals the lie seed");
            Assert.AreNotEqual(Seeds.ForViolators(daySeed), dialog, $"case {c}: dialog seed equals the violator seed");
            Assert.AreEqual(dialog, Seeds.ForDialog(caseSeed), $"case {c}: not deterministic");
            dialogs.Add(dialog);
        }
        Assert.AreEqual(20, dialogs.Count, "dialog seeds repeat across cases");
    }

    [Test]
    public void LookAndLegendaryStreams_AreDistinct()
    {
        int daySeed = Seeds.Day(12345, 3);
        var cases = new HashSet<int>(Enumerable.Range(1, 20).Select(c => Seeds.ForCase(daySeed, c)));
        var looks = new HashSet<int>();
        var legendaries = new HashSet<int>();
        for (int c = 1; c <= 20; c++)
        {
            int caseSeed = Seeds.ForCase(daySeed, c);
            int look = Seeds.ForLooks(caseSeed);
            int legendary = Seeds.ForLegendary(caseSeed);
            foreach ((string name, int seed) in new[] { ("look", look), ("legendary", legendary) })
            {
                Assert.IsFalse(cases.Contains(seed), $"case {c}: the {name} seed is a case seed");
                Assert.AreNotEqual(Seeds.ForClues(caseSeed), seed, $"case {c}: {name} seed equals the clue seed");
                Assert.AreNotEqual(Seeds.ForLies(caseSeed), seed, $"case {c}: {name} seed equals the lie seed");
                Assert.AreNotEqual(Seeds.ForDialog(caseSeed), seed, $"case {c}: {name} seed equals the dialog seed");
                Assert.AreNotEqual(Seeds.ForViolators(daySeed), seed, $"case {c}: {name} seed equals the violator seed");
            }

            Assert.AreNotEqual(look, legendary, $"case {c}: look and legendary seeds are equal");
            Assert.AreEqual(look, Seeds.ForLooks(caseSeed), $"case {c}: look seed not deterministic");
            Assert.AreEqual(legendary, Seeds.ForLegendary(caseSeed), $"case {c}: legendary seed not deterministic");
            looks.Add(look);
            legendaries.Add(legendary);
        }
        Assert.AreEqual(20, looks.Count, "look seeds repeat across cases");
        Assert.AreEqual(20, legendaries.Count, "legendary seeds repeat across cases");
    }

    [Test]
    public void Mix_IsDeterministic_AndSaltSensitive()
    {
        Assert.AreEqual(Seeds.Mix(5, 9), Seeds.Mix(5, 9));
        Assert.AreNotEqual(Seeds.Mix(5, 9), Seeds.Mix(5, 10));
        Assert.AreNotEqual(Seeds.Mix(5, 9), Seeds.Mix(6, 9));
    }
}
