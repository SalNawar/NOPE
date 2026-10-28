using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The three-week progression (Saleh 2026-10-28: "extend progression to 3
/// weeks instead of two with a new feature every 2 to 3 days"; the spec
/// PROGRESSION_21.md section 5, the day map 1,3,3,7,5,7,9,11,13,15,17,18,21,19,21),
/// read from the content source and the last-day ending as Generate World
/// reads them. A feature day brings a new paper, a new check (a rule that is
/// not the open destinations and not a closure for every kind:
/// DayPacing.RuleKey) or a new named key (a tool, an app, a book, a field, a
/// wheel entry, a PC feature). A mastery day brings none of those: a lie at
/// most. Feature days are never consecutive after day 1, no gap exceeds 3
/// days, the run's last day is the last feature day, and every day gate
/// outside days[] sits on a feature day or past the run.
/// </summary>
public class ProgressionRhythmTests
{
    private const string SourcePath = "Assets/Data/World/world_source.json";
    private const string EndingPath = "Assets/Data/Endings/Ending_WorldReport.asset";

    /// <summary>The schedule's shape: the run's days, its feature days and the queue of each day (PROGRESSION_21.md sections 5 and 5.1).</summary>
    private const int LastDay = 21;
    private static readonly int[] FeatureDays = { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21 };
    private static readonly int[] Queues = { 8, 10, 12, 12, 13, 14, 14, 14, 14, 16, 16, 16, 16, 17, 17, 17, 18, 18, 18, 18, 18 };

    private sealed class Day
    {
        public int Number;
        public int Queue;
        public string ShiftStart, ShiftEnd, Bulletin;
        public List<string> Papers = new List<string>(), Rules = new List<string>(), Introduces = new List<string>(), Lies = new List<string>();
    }

    private static string Read(string relative, [CallerFilePath] string here = "")
    {
        string path = File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    private static List<string> Texts(ContentNode node) => node == null ? new List<string>() : node.Items.Select(i => i.Text).ToList();

    private static List<Day> Days(ContentNode root) => root.Get("days").Items.Select(d => new Day
    {
        Number = int.Parse(d.Get("day").Text),
        Queue = int.Parse(d.Get("queue").Text),
        ShiftStart = d.Get("shiftStart")?.Text,
        ShiftEnd = d.Get("shiftEnd")?.Text,
        Bulletin = d.Get("bulletin")?.Text ?? string.Empty,
        Papers = Texts(d.Get("papers")),
        Rules = Texts(d.Get("rules")),
        Introduces = Texts(d.Get("introduces")),
        Lies = Texts(d.Get("lies")),
    }).OrderBy(d => d.Number).ToList();

    /// <summary>Each rule's pacing key (DayPacing.RuleKey): the open destinations and the closures for every kind share DayPacing.Closures, every other rule is its own check.</summary>
    private static Dictionary<string, string> RuleKeys(ContentNode root)
    {
        var keys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ContentNode r in root.Get("rules").Items)
        {
            string asset = r.Get("asset").Text;
            var type = (TravelRuleType)Enum.Parse(typeof(TravelRuleType), r.Get("type").Text);
            bool listsKinds = r.Get("kinds") != null && r.Get("kinds").Items.Count > 0;
            keys[asset] = DayPacing.RuleKey(asset, Directives.IsClosure(type), listsKinds, type == TravelRuleType.OpenDestinations);
        }
        return keys;
    }

    /// <summary>The days that bring a feature: a new paper, a new check or a new named key, each against every earlier day (DayPacing.NewByDay).</summary>
    private static List<int> FeatureDaysOf(List<Day> days, Dictionary<string, string> ruleKeys)
    {
        List<List<string>> papers = DayPacing.NewByDay(days.Select(d => (IEnumerable<string>)d.Papers).ToList());
        List<List<string>> checks = DayPacing.NewByDay(days.Select(d => (IEnumerable<string>)d.Rules.Select(r => ruleKeys[r]).ToList()).ToList());
        List<List<string>> named = DayPacing.NewByDay(days.Select(d => (IEnumerable<string>)d.Introduces).ToList());
        var feature = new List<int>();
        for (int i = 0; i < days.Count; i++)
            if (papers[i].Count > 0 || checks[i].Count > 0 || named[i].Count > 0)
                feature.Add(days[i].Number);
        return feature;
    }

    private static int EndingThreshold()
    {
        string line = Read(EndingPath).Split('\n').FirstOrDefault(l => l.TrimStart().StartsWith("threshold:", StringComparison.Ordinal));
        Assert.IsNotNull(line, "Ending_WorldReport.asset has a threshold line");
        return (int)float.Parse(line.Split(':')[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
    }

    [Test]
    public void TheRun_IsTwentyOneDays_EachPlanned_TheEndingOnTheLast()
    {
        List<Day> days = Days(ContentJson.Parse(Read(SourcePath)));
        CollectionAssert.AreEqual(Enumerable.Range(1, LastDay), days.Select(d => d.Number), "one plan per day, 1 to 21");
        CollectionAssert.IsEmpty(DayPlans.Gaps(days.Select(d => d.Number)));
        CollectionAssert.IsEmpty(DayPlans.Unplanned(days.Select(d => d.Number), LastDay));
        Assert.AreEqual(LastDay, EndingThreshold(), "the last-day ending (ContentLibrarySO.LastDay) fires at day 21's night");
        for (int i = 0; i < days.Count; i++)
            Assert.AreEqual($"DayPlan_Inv_Day{i + 1}", ContentJson.Parse(Read(SourcePath)).Get("days").Items[i].Get("asset").Text);
    }

    [Test]
    public void FeatureDays_AreTheOddDays_NeverConsecutive_NoGapOverThree_TheLastDayTheLast()
    {
        ContentNode root = ContentJson.Parse(Read(SourcePath));
        List<Day> days = Days(root);
        List<int> feature = FeatureDaysOf(days, RuleKeys(root));

        CollectionAssert.AreEqual(FeatureDays, feature, "the feature days of PROGRESSION_21.md section 5");
        for (int i = 1; i < feature.Count; i++)
        {
            Assert.Greater(feature[i] - feature[i - 1], 1, $"feature days {feature[i - 1]} and {feature[i]} are consecutive");
            Assert.LessOrEqual(feature[i] - feature[i - 1], 3, $"the gap from day {feature[i - 1]} to day {feature[i]} is longer than 3 days");
        }
        Assert.AreEqual(LastDay, feature[feature.Count - 1], "the run ends on a feature day (the Return Order and the range limit)");
    }

    [Test]
    public void MasteryDays_IntroduceALieAtMost_KeepTheirPapersAndChecks()
    {
        ContentNode root = ContentJson.Parse(Read(SourcePath));
        List<Day> days = Days(root);
        Dictionary<string, string> ruleKeys = RuleKeys(root);
        List<int> feature = FeatureDaysOf(days, ruleKeys);
        List<List<string>> lies = DayPacing.NewByDay(days.Select(d => (IEnumerable<string>)d.Lies).ToList());

        for (int i = 1; i < days.Count; i++)
        {
            Day day = days[i], before = days[i - 1];
            if (feature.Contains(day.Number))
                continue;
            CollectionAssert.AreEqual(before.Papers, day.Papers, $"day {day.Number} (mastery) keeps yesterday's papers");
            CollectionAssert.IsEmpty(day.Introduces, $"day {day.Number} (mastery) introduces no named key");
            CollectionAssert.AreEquivalent(before.Rules.Select(r => ruleKeys[r]).Distinct(), day.Rules.Select(r => ruleKeys[r]).Distinct(), $"day {day.Number} (mastery) keeps yesterday's checks");
            Assert.LessOrEqual(lies[i].Count, 1, $"day {day.Number} (mastery) brings a lie at most: {string.Join(", ", lies[i])}");
            StringAssert.DoesNotStartWith("NEW:", day.Bulletin, $"day {day.Number} (mastery) carries a notice, never a NEW line");
        }
        foreach (int number in FeatureDays.Skip(1))
            StringAssert.StartsWith("NEW:", days[number - 1].Bulletin, $"day {number} (feature) opens its briefing with its new thing");
    }

    [Test]
    public void TheKnobs_GrowByWeek_QueueHoursAndChances()
    {
        List<Day> days = Days(ContentJson.Parse(Read(SourcePath)));
        CollectionAssert.AreEqual(Queues, days.Select(d => d.Queue), "the queue by day (PROGRESSION_21.md section 5.1)");
        foreach (Day day in days)
        {
            (string start, string end) expected = day.Number <= 7 ? (null, null) : day.Number <= 14 ? ("13:00", "21:00") : ("16:00", "24:00");
            Assert.AreEqual(expected, (day.ShiftStart, day.ShiftEnd), $"day {day.Number}'s hours: the standard day in week 1, 13:00-21:00 in week 2, 16:00-24:00 in week 3");
        }
    }

    [Test]
    public void TheDayGatesOutsideThePlans_SitOnFeatureDays_OrPastTheRun()
    {
        ContentNode root = ContentJson.Parse(Read(SourcePath));
        foreach (ContentNode q in root.Get("questions").Items)
        {
            int fromDay = int.Parse(q.Get("fromDay").Text);
            Assert.IsTrue(FeatureDays.Contains(fromDay) || fromDay > LastDay, $"question '{q.Get("id").Text}' opens on day {fromDay}, neither a feature day nor past the run");
        }
        Assert.AreEqual(11, int.Parse(root.Get("translation").Get("fromDay").Text), "the lens's day is the Work Permit's (the day map's 8)");
        Assert.AreEqual(9, int.Parse(root.Get("guide").Get("guidedThroughDay").Text), "the guide runs through the Stranding Waiver's day (the day map's 7)");
        foreach (ContentNode set in root.Get("pc").Get("steps").Get("sets").Items)
            foreach (ContentNode step in set.Get("steps").Items)
                if (step.Get("fromDay") != null)
                    Assert.LessOrEqual(int.Parse(step.Get("fromDay").Text), LastDay, $"pc.steps '{set.Get("type").Text}/{step.Get("id").Text}' opens inside the run");
        foreach (ContentNode rule in root.Get("history").Get("rules").Items)
            foreach (ContentNode c in rule.Get("conditions")?.Items ?? Array.Empty<ContentNode>())
                if (c.Get("type")?.Text == "DayAtLeast")
                    Assert.Less(int.Parse(c.Get("threshold").Text), LastDay, $"history rule '{rule.Get("id").Text}' fires at a night before the last (DayAtLeast N fires the night of day N)");
    }
}
