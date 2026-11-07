using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The run's famous draw (D12, premades-52): FamousDraw.Pick's rules, and
/// the published content (world_source.json) makes every one of the 52
/// premades reachable within 21 days: drawn by some of 50 run seeds, or
/// standing in an authored story beat.
/// </summary>
public class FamousDrawTests
{
    /// <summary>A fixed stream: each Range returns its lowest value.</summary>
    private sealed class FirstRandom : IRandomSource
    {
        public int Range(int minInclusive, int maxExclusive) => minInclusive;
        public float Value() => 0f;
    }

    [Test]
    public void Pick_DrawsPerDayDistinctIdsAndSkipsTheUnavailable()
    {
        var pool = new List<string> { "a", "b", "c", "d" };
        List<string> picked = FamousDraw.Pick(pool, new HashSet<string> { "a" }, 2, new FirstRandom());
        CollectionAssert.AreEqual(new[] { "b", "c" }, picked);
    }

    [Test]
    public void Pick_ClampsPerDayAndStopsWhenThePoolRunsOut()
    {
        var pool = new List<string> { "a", "b", "c", "d" };
        Assert.AreEqual(FamousDraw.MaxPerDay, FamousDraw.Pick(pool, null, 9, new SeededRandom(7)).Count);
        Assert.AreEqual(0, FamousDraw.Pick(pool, null, 0, new SeededRandom(7)).Count);
        Assert.AreEqual(1, FamousDraw.Pick(pool, new HashSet<string> { "a", "b", "c" }, 2, new SeededRandom(7)).Count);
    }

    [Test]
    public void Pick_IsTheSameForTheSameSeed()
    {
        var pool = ContentFixture.PremadeIds();
        CollectionAssert.AreEqual(FamousDraw.Pick(pool, null, 2, new SeededRandom(Seeds.ForFamous(Seeds.Day(42, 9)))),
                                  FamousDraw.Pick(pool, null, 2, new SeededRandom(Seeds.ForFamous(Seeds.Day(42, 9)))));
    }

    [Test]
    public void EveryPremadeIsReachableWithin21DaysOver50Seeds()
    {
        List<ContentFixture.DayPremades> days = ContentFixture.DayPremadeFields().Where(d => d.day <= 21).OrderBy(d => d.day).ToList();
        Assert.AreEqual(21, days.Count, "the source has 21 day plans");
        Assert.IsTrue(days.All(d => d.famousPerDay >= 0 && d.famousPerDay <= FamousDraw.MaxPerDay && (d.famousPerDay == 0 || d.pool.Count > 0)));

        var drawn = new HashSet<string>();
        var forced = new HashSet<string>(days.SelectMany(d => d.forced));
        for (int seed = 1; seed <= 50; seed++)
        {
            var met = new HashSet<string>();
            foreach (ContentFixture.DayPremades day in days)
            {
                var unavailable = new HashSet<string>(met.Concat(day.forced));
                List<string> today = FamousDraw.Pick(day.pool, unavailable, day.famousPerDay, new SeededRandom(Seeds.ForFamous(Seeds.Day(seed, day.day))));
                Assert.LessOrEqual(today.Count, day.famousPerDay);
                drawn.UnionWith(today);
                met.UnionWith(today);
                met.UnionWith(day.forced);
            }
        }

        var pooled = new HashSet<string>(days.SelectMany(d => d.pool));
        // A pooled premade met first in a story beat (Caesar and Ramesses stand on day 6) is reached there, so the draw skips them after.
        CollectionAssert.IsEmpty(pooled.Except(drawn).Except(forced).OrderBy(id => id), "pooled premades neither drawn nor in a story beat within 21 days");
        CollectionAssert.IsEmpty(new[] { "senenmut", "aspasia", "banzhao", "arib", "lanyer", "gallerani", "elytis", "fellini", "hezehui", "malaika", "meitner", "sayyab", "tahtawi", "toyoda" }.Except(drawn),
                                 "the 14 premades with no story beat are drawn by some seed");
        List<string> all = ContentFixture.PremadeIds();
        Assert.AreEqual(52, all.Count, "the 52 characters of Saleh's list");
        CollectionAssert.IsEmpty(all.Except(drawn).Except(forced).OrderBy(id => id), "premades neither drawn nor in a story beat");
    }
}
