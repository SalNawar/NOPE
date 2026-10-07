using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Saleh's sound list read as data (SoundList: ids, variants, loops,
/// priorities, mixer groups, file names, which missing P1 one-shots get a
/// placeholder), the placeholders themselves (SoundSynth: mixed low, start at
/// once, the same every time) and the hall's ambience mix (HallAmbience: day,
/// rush, night from the clock, rain from the weather, the booth always).
/// </summary>
public class SoundListTests
{
    private const string Sample =
        "# Time Sorter: sound list\n" +
        "Drop each file ... `not_a_row` P1\n" +
        "## 1. Desk: papers and objects\n" +
        "\n" +
        "| # | File | When it plays | Length | Character | Var | P |\n" +
        "|---|---|---|---|---|---|---|\n" +
        "| 1 | `stamp_approve` | APPROVED stamp hits | 0.3 s | heavy thunk | ×3 | P1 |\n" +
        "| 8 | `paper_drop` | paper dropped | 0.3 s | soft paper flop | ×3 | P1 |\n" +
        "| 20 | `scanner_sweep` | scan light sweeps (loopable 1 s) | 1.0 s | whirr | loop | P1 |\n" +
        "| 28 | `mug_set` | coffee mug put down | 0.2 s | ceramic on wood | ×2 | P3 |\n" +
        "## 4. Interface (menus, buttons)\n" +
        "| # | File | When it plays | Length | Character | Var | P |\n" +
        "|---|---|---|---|---|---|---|\n" +
        "| 49 | `ui_press` | button pressed down | 0.06 s | plastic switch down | ×2 | P1 |\n" +
        "| 49 | `ui_press` | again | 0.06 s | dup | ×2 | P1 |\n" +
        "## 5. Shift and time\n" +
        "| 57 | `last_hour_alarm` | the last hour begins | 1.5 s | ding | ×1 | P1 |\n" +
        "## 7. Ambience beds (loops, 30-60 s, seamless)\n" +
        "| # | File | When it plays | Character | P |\n" +
        "|---|---|---|---|---|\n" +
        "| 66 | `amb_hall_day` | the hall in the daytime | crowd murmur | P1 |\n" +
        "## 8. Music (optional, later)\n" +
        "| # | File | When | P |\n" +
        "|---|---|---|---|\n" +
        "| 74 | `mus_title` | title screen loop | P2 |\n";

    private static SoundListEntry Row(List<SoundListEntry> rows, string id) => rows.Find(r => r.Id == id);

    [Test]
    public void Parse_ReadsIdsVariantsLoopsPrioritiesAndGroups()
    {
        var problems = new List<string>();
        List<SoundListEntry> rows = SoundList.Parse(Sample, problems);
        CollectionAssert.AreEqual(new[] { "stamp_approve", "paper_drop", "scanner_sweep", "mug_set", "ui_press", "last_hour_alarm", "amb_hall_day", "mus_title" },
                                  rows.ConvertAll(r => r.Id));
        Assert.AreEqual(1, problems.Count, "the repeated ui_press is named");
        StringAssert.Contains("ui_press", problems[0]);

        Assert.AreEqual(3, Row(rows, "paper_drop").Variants);
        Assert.AreEqual(1, Row(rows, "paper_drop").Section);
        Assert.AreEqual(SoundBus.Desk, Row(rows, "paper_drop").Bus);
        Assert.IsTrue(Row(rows, "scanner_sweep").Loop, "a 'loop' row loops");
        Assert.AreEqual(1, Row(rows, "scanner_sweep").Variants);
        Assert.AreEqual(3, Row(rows, "mug_set").Priority);
        Assert.AreEqual(SoundBus.Ui, Row(rows, "ui_press").Bus);
        Assert.AreEqual(SoundBus.Desk, Row(rows, "last_hour_alarm").Bus);
        Assert.IsTrue(Row(rows, "amb_hall_day").Loop, "the ambience beds loop");
        Assert.AreEqual(SoundBus.Ambience, Row(rows, "amb_hall_day").Bus);
        Assert.AreEqual(1, Row(rows, "amb_hall_day").Priority);
        Assert.IsTrue(Row(rows, "mus_title").Loop);
        Assert.AreEqual(SoundBus.Music, Row(rows, "mus_title").Bus);
        Assert.AreEqual(2, Row(rows, "mus_title").Priority);
    }

    [Test]
    public void TryMatchFile_TakesTheNameOrItsVariants()
    {
        var ids = new HashSet<string> { "paper_drop", "pc_on", "ui_v2_test" };
        Assert.IsTrue(SoundList.TryMatchFile("paper_drop_v2", ids, out string id, out int v));
        Assert.AreEqual(("paper_drop", 2), (id, v));
        Assert.IsTrue(SoundList.TryMatchFile("pc_on", ids, out id, out v));
        Assert.AreEqual(("pc_on", 1), (id, v));
        Assert.IsTrue(SoundList.TryMatchFile("pc_on_v1", ids, out id, out v));
        Assert.AreEqual(("pc_on", 1), (id, v));
        Assert.IsFalse(SoundList.TryMatchFile("paper_drop_v0", ids, out _, out _), "variants count from 1");
        Assert.IsFalse(SoundList.TryMatchFile("stamp_thud", ids, out _, out _), "a file the list does not name");
        Assert.IsFalse(SoundList.TryMatchFile("", ids, out _, out _));
    }

    [Test]
    public void Placeholders_OnlyForP1OneShotsOfTheSynthesisedKinds()
    {
        List<SoundListEntry> rows = SoundList.Parse(Sample);
        Assert.AreEqual(PlaceholderKind.Paper, SoundList.PlaceholderFor(Row(rows, "paper_drop")));
        Assert.AreEqual(PlaceholderKind.Click, SoundList.PlaceholderFor(Row(rows, "ui_press")));
        Assert.AreEqual(PlaceholderKind.Alarm, SoundList.PlaceholderFor(Row(rows, "last_hour_alarm")));
        Assert.AreEqual(PlaceholderKind.None, SoundList.PlaceholderFor(Row(rows, "stamp_approve")), "the stamps wait for Saleh's reference");
        Assert.AreEqual(PlaceholderKind.None, SoundList.PlaceholderFor(Row(rows, "scanner_sweep")), "no loop gets one");
        Assert.AreEqual(PlaceholderKind.None, SoundList.PlaceholderFor(Row(rows, "amb_hall_day")), "the ambience stays silent rather than cheap");
        Assert.AreEqual(PlaceholderKind.None, SoundList.PlaceholderFor(Row(rows, "mug_set")), "P3");
        Assert.AreEqual(PlaceholderKind.None, SoundList.PlaceholderFor(new SoundListEntry("ui_press", 4, 2, false, 2, SoundBus.Ui)), "P1 only");
    }

    [Test]
    public void Synth_EveryKind_IsLowStartsAtOnceAndRepeats()
    {
        const int rate = 22050;
        foreach (PlaceholderKind kind in System.Enum.GetValues(typeof(PlaceholderKind)))
        {
            float[] a = SoundSynth.Make(kind, "cue_" + kind, rate);
            if (kind == PlaceholderKind.None)
            {
                Assert.AreEqual(0, a.Length);
                continue;
            }
            Assert.Greater(a.Length, rate / 100, $"{kind} has a body");
            float peak = 0f, early = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                Assert.IsFalse(float.IsNaN(a[i]) || float.IsInfinity(a[i]), $"{kind} sample {i}");
                peak = System.Math.Max(peak, System.Math.Abs(a[i]));
                if (i < rate * 0.005f)
                    early = System.Math.Max(early, System.Math.Abs(a[i]));
            }
            Assert.AreEqual(SoundSynth.Level, peak, 1e-4f, $"{kind} peaks at the placeholder level");
            Assert.Greater(early, 0.01f, $"{kind} starts within 5 ms");
            CollectionAssert.AreEqual(a, SoundSynth.Make(kind, "cue_" + kind, rate), $"{kind} is the same every time");
        }
        Assert.AreNotEqual(SoundSynth.Hash("paper_drop"), SoundSynth.Hash("paper_pickup"));
        Assert.AreEqual(SoundSynth.Hash("paper_drop"), SoundSynth.Hash("paper_drop"));
    }

    private static AmbienceMix At(float minute, float rain = 0f) => HallAmbience.For(minute, rain, 20 * 60, 6 * 60, 30, new[] { 8 * 60, 10 * 60, 17 * 60, 19 * 60 });

    [Test]
    public void Ambience_FollowsTheClockAndTheWeather()
    {
        AmbienceMix noon = At(12 * 60);
        Assert.AreEqual((1f, 0f, 0f), (noon.Day, noon.Night, noon.Rush));
        AmbienceMix rush = At(9 * 60);
        Assert.AreEqual(1f, rush.Rush, "the rush layers over the day in its window");
        Assert.AreEqual(1f, rush.Day);
        AmbienceMix late = At(23 * 60);
        Assert.AreEqual((0f, 1f), (late.Day, late.Night), "a night shift's hall");
        Assert.AreEqual(1f, At(1439f).Night, "up to midnight");
        Assert.AreEqual(1f, At(2 * 60).Night, "and past it");
        AmbienceMix edge = At(20 * 60);
        Assert.AreEqual(0.5f, edge.Night, 0.01f, "the beds cross-fade at the edge");
        Assert.AreEqual(1f, edge.Day + edge.Night, 1e-5f);
        Assert.AreEqual(0f, At(18 * 60 + 50).Night);
        Assert.AreEqual(0.6f, At(12 * 60, 0.6f).Rain, 1e-5f);
        Assert.AreEqual(1f, At(12 * 60, 3f).Rain, "clamped");
        Assert.AreEqual((1f, 1f), (late.Booth, late.Portal), "the booth's tone and the portals' hum always play");
        Assert.AreEqual(1f, noon.Of(SoundCues.AmbBoothRoom));
        Assert.AreEqual(0f, noon.Of("ui_press"));
    }
}

/// <summary>The shift's last-hour alarm rings once as the clock crosses into the day's own last hour (a night shift's at 23:00).</summary>
public class ShiftBellsTests
{
    [Test]
    public void LastHour_RingsOnTheCrossing_FromTheDaysOwnHours()
    {
        var day = new ShiftHours(9 * 60, 17 * 60);
        Assert.IsTrue(ShiftBells.LastHourBegan(959.5f, 960f, day, 60), "16:00 on a 09:00-17:00 day");
        Assert.IsFalse(ShiftBells.LastHourBegan(960f, 961f, day, 60), "once");
        Assert.IsFalse(ShiftBells.LastHourBegan(900f, 959f, day, 60), "not before");
        Assert.IsTrue(ShiftBells.LastHourBegan(950f, 1000f, day, 60), "a long frame still crosses");
        var night = new ShiftHours(13 * 60, 24 * 60);
        Assert.IsTrue(ShiftBells.LastHourBegan(1379f, 1380f, night, 60), "a night shift closing at 24:00 rings at 23:00");
        Assert.IsFalse(ShiftBells.LastHourBegan(500f, 600f, new ShiftHours(540, 600), 60), "a shift no longer than its last hour has none");
        Assert.IsFalse(ShiftBells.LastHourBegan(959f, 960f, day, 0));
    }
}
