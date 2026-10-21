using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The switchboard plate's measured layout (board_layout.json, the art's own
/// measure of the finished plate) reads as the v3 board Saleh locked: 4
/// destinations of 3 period jacks, 5 POWER sockets, 5 modules (04 LOCKED and
/// 05 OUT OF SERVICE under their placards), one MASTER whose pull runs down;
/// and the cords' geometry: the sag, the hanging curve and where a dropped
/// plug seats.
/// </summary>
public class SwitchboardLayoutTests
{
    private const string LayoutPath = "Assets/Art/UI/Resources/Switchboard/board_layout.json";

    /// <summary>A file of the project, from the tests' working folder or this file's.</summary>
    private static string Repo(string relative, [CallerFilePath] string here = "") =>
        File.ReadAllText(File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative));

    /// <summary>The imported board_layout.json.</summary>
    private static SwitchboardLayout Layout() => SwitchboardLayout.Parse(Repo(LayoutPath));

    [Test]
    public void TheLayout_IsTheV3Board()
    {
        SwitchboardLayout layout = Layout();
        Assert.AreEqual(1920f, layout.Width);
        Assert.AreEqual(1080f, layout.Height);
        Assert.AreEqual(4, layout.Destinations.Count);
        Assert.AreEqual(3, layout.PeriodRows);
        Assert.IsTrue(layout.Destinations.All(d => d.Periods.Count == 3), "the uniform 4 x 3 grid");
        Assert.AreEqual(5, layout.PowerRow.Count);
        Assert.AreEqual(5, layout.Modules.Count);
        CollectionAssert.AreEqual(new[] { "01", "02", "03", "04", "05" }, layout.Modules.Select(m => m.Id).ToArray());
        CollectionAssert.AreEqual(new[] { false, false, false, true, true }, layout.Modules.Select(m => m.Placarded).ToArray());
        Assert.IsTrue(layout.Modules[3].Power.Covered && layout.Modules[4].Period.Covered, "the placards cover 04's and 05's sockets");
        Assert.IsFalse(layout.Modules[0].Place.Covered);
    }

    [Test]
    public void EveryJack_SitsOnTheBoard_AndNoTwoHitAreasOverlap()
    {
        SwitchboardLayout layout = Layout();
        var sockets = new List<BoardSocket>();
        foreach (LayoutDestination d in layout.Destinations)
        {
            sockets.Add(d.Place);
            sockets.AddRange(d.Periods.Select(p => p.Jack));
            Assert.IsTrue(d.NameText.W > 0f && d.NameText.H > 0f);
        }
        sockets.AddRange(layout.PowerRow);
        foreach (LayoutModule m in layout.Modules)
            sockets.AddRange(new[] { m.Power, m.Place, m.Period });
        foreach (BoardSocket s in sockets)
            Assert.IsTrue(layout.Board.Contains(s.Centre), s.Centre.ToString());
        for (int i = 0; i < sockets.Count; i++)
            for (int j = i + 1; j < sockets.Count; j++)
                Assert.Greater(sockets[i].Centre.DistanceTo(sockets[j].Centre), sockets[i].HitRadius + sockets[j].HitRadius - 1f,
                               $"{sockets[i].Centre} and {sockets[j].Centre}: a dropped plug seats in one only");
    }

    [Test]
    public void Master_PullsDown_FromRestToPulled()
    {
        LayoutMaster master = Layout().Master;
        Assert.Greater(master.To.Y, master.From.Y + 300f, "pulled is far below rest");
        Assert.IsTrue(master.HandleRest.Contains(master.From));
        Assert.AreEqual(2, master.RodPivots.Count);
        Assert.AreEqual(2, master.RodTops.Count);
        Assert.AreEqual(0f, master.PullOf(master.From), 1e-4f);
        Assert.AreEqual(1f, master.PullOf(master.To), 1e-4f);
        Assert.AreEqual(0.5f, master.PullOf(master.HandleAt(0.5f)), 1e-4f);
        Assert.AreEqual(1f, master.PullOf(master.To.Plus(0f, 500f)), 1e-4f, "clamped below");
        Assert.AreEqual(0f, master.PullOf(master.From.Plus(0f, -500f)), 1e-4f, "clamped above");
        Assert.Greater(master.RodPivotY, master.RodTopY);
    }

    [Test]
    public void APlugsCord_LeavesItsEndCap_DownLeftOfTheSocket()
    {
        BoardPoint tail = Layout().PlugTail;
        Assert.Less(tail.X, 0f);
        Assert.Greater(tail.Y, 0f);
    }

    [Test]
    public void TheBoardsWords_AreUiStrings_EnglishInEveryCulture()
    {
        var tiers = new Dictionary<string, string>();
        foreach (ContentNode e in ContentJson.Parse(Repo("Assets/Data/World/world_source.json")).Get("ui").Get("strings").Items)
            tiers[e.Get("key").Text] = e.Get("tier").Text;
        foreach (string key in SwitchboardWords.All)
        {
            Assert.IsTrue(tiers.ContainsKey(key), key);
            Assert.AreEqual("Full", tiers[key], key + ": printed in English until its translations come");
        }
    }

    [Test]
    public void AMissingPart_IsNamed()
    {
        var e = Assert.Throws<FormatException>(() => SwitchboardLayout.Parse("{\"image_size\": [1920, 1080]}"));
        StringAssert.Contains("board_rect", e.Message);
    }

    [Test]
    public void ACord_SagsMoreTheLongerItIs_AndLeavesEachPlugAlongItsAxis()
    {
        Assert.AreEqual(0f, SwitchCords.Sag(500f, 0f));
        Assert.AreEqual(0f, SwitchCords.Sag(0f, 60f));
        Assert.Greater(SwitchCords.Sag(600f, 40f), SwitchCords.Sag(200f, 40f));
        Assert.AreEqual(MathF.Sqrt(3f * 400f * 40f / 8f), SwitchCords.Sag(400f, 40f), 1e-3f);
        Assert.AreEqual(SwitchCords.MinReach, SwitchCords.Reach(10f, 0.3f, 120f), "a short cord still leaves along its plug");
        Assert.AreEqual(60f, SwitchCords.Reach(200f, 0.3f, 120f), 1e-4f);
        Assert.AreEqual(120f, SwitchCords.Reach(900f, 0.3f, 120f), "capped");

        var a = new BoardPoint(100f, 700f);
        var b = new BoardPoint(500f, 200f);
        var down = new BoardPoint(0f, 1f);
        BoardPoint start = SwitchCords.At(a, b, down, 50f, 30f, 0f), end = SwitchCords.At(a, b, down, 50f, 30f, 1f);
        Assert.AreEqual(a.X, start.X, 1e-3f);
        Assert.AreEqual(a.Y, start.Y, 1e-3f);
        Assert.AreEqual(b.X, end.X, 1e-3f);
        Assert.AreEqual(b.Y, end.Y, 1e-3f);
        BoardPoint middle = SwitchCords.At(a, b, new BoardPoint(0f, 0f), 0f, 40f, 0.5f);
        Assert.AreEqual(300f, middle.X, 1e-3f);
        Assert.AreEqual(450f + 30f, middle.Y, 1e-3f, "y is down: with no reach the middle hangs three quarters of the sag below the chord");
        BoardPoint leaving = SwitchCords.At(a, b, new BoardPoint(-0.6f, 0.8f), 100f, 0f, 0.05f);
        Assert.Less(leaving.X, a.X, "it leaves down-left, along the plugs' axis");
        Assert.Greater(leaving.Y, a.Y);

        var points = new List<BoardPoint>();
        SwitchCords.Sample(a, b, down, 50f, 30f, 16, points);
        Assert.AreEqual(17, points.Count);
        Assert.AreEqual(b.X, points[16].X, 1e-3f);
    }

    [Test]
    public void ADroppedPlug_SeatsInTheNearestAcceptingSocket_WithinItsHitRadius()
    {
        var sockets = new[]
        {
            new BoardSocket(new BoardPoint(100f, 100f), 22.5f, 26f, false),
            new BoardSocket(new BoardPoint(150f, 100f), 22.5f, 26f, false),
            new BoardSocket(new BoardPoint(400f, 100f), 22.5f, 26f, false),
        };
        Assert.AreEqual(1, SwitchCords.Snap(sockets, new BoardPoint(126f, 100f), _ => true), "both reach: the nearer wins");
        Assert.AreEqual(0, SwitchCords.Snap(sockets, new BoardPoint(126f, 100f), i => i != 1), "a socket that refuses is skipped");
        Assert.AreEqual(-1, SwitchCords.Snap(sockets, new BoardPoint(300f, 100f), _ => true), "too far from every socket: it goes back");
        Assert.AreEqual(-1, SwitchCords.Snap(sockets, new BoardPoint(440f, 100f), _ => true));
        Assert.AreEqual(2, SwitchCords.Snap(sockets, new BoardPoint(440f, 100f), _ => true, 1.6f), "the magnet widens the reach");
        Assert.AreEqual(-1, SwitchCords.Snap(null, new BoardPoint(0f, 0f), null));
    }

    [Test]
    public void EverySound_IsShort_AtThePlaceholdersLevel_AndTheSameEachTime()
    {
        foreach (SwitchSound sound in Enum.GetValues(typeof(SwitchSound)))
        {
            float[] samples = SwitchboardSounds.Make(sound, 22050);
            Assert.Greater(samples.Length, 0, sound.ToString());
            Assert.LessOrEqual(samples.Length, 22050 * 2, sound + " lasts under 2 s");
            Assert.AreEqual(SoundSynth.Level, samples.Max(Math.Abs), 1e-3f, sound.ToString());
            CollectionAssert.AreEqual(samples, SwitchboardSounds.Make(sound, 22050), sound + " is the same every time");
            StringAssert.StartsWith("switch_", SwitchboardSounds.CueId(sound));
        }
        Assert.AreEqual(Enum.GetValues(typeof(SwitchSound)).Length,
                        Enum.GetValues(typeof(SwitchSound)).Cast<SwitchSound>().Select(SwitchboardSounds.CueId).Distinct().Count(), "one cue id each");
    }
}
