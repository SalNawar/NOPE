using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// RUN-SAFE's ledger (BUG-28a, BUG-28c): a cheat's day change lands on the
/// world play's own steps make, field by field (the whole saved world:
/// money, citations, the pet, orders, premades met, the statement, the
/// history). A skip is the shift's close as played, Home's night on the
/// default care and Sleep; a jump closes today as played and plays every
/// day between (the whole queue decided correctly), so it reaches the run's
/// last day as a played run would; a jump back is a new run with the same
/// seed and pet played to the day; a restart is the morning; a
/// route from Home starts from the end-of-shift save with the cheat changes
/// made since replayed on it; the live shift is closed on its own ledger
/// once; a jump keeps the next generation's switches for the day it lands on;
/// a cheat grant cancels its pending order. "Play" here is DayCycle's steps
/// (the ones GameManager and HomeManager run between their screens), not the
/// scenes themselves: what the scenes add (the News site's back issues, the
/// desk guide, the desk upgrades' day) is listed in docs/pending/RUN-SAFE.md.
/// "NeedsEditor": the steps read the run's content library and game config
/// (Resources/RunConfig), so these run in Unity's EditMode suite.
/// </summary>
public class CheatLedgerTests
{
    private const int Seed = 12345;

    private RunConfigSO _run;
    private ContentLibrarySO _lib;
    private GameConfigSO _config;

    /// <summary>The run's config, library and tuning (as BalanceSimulation loads them); the cheat switches off.</summary>
    [SetUp]
    public void Load()
    {
        _run = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        Assert.IsNotNull(_run, "Resources/RunConfig");
        _lib = _run.contentLibrary;
        _config = _run.gameConfig;
        Assert.IsNotNull(_lib, "RunConfig's content library");
        Assert.IsNotNull(_config, "RunConfig's game config");
        DevToolsState.ResetAll();
    }

    /// <summary>The cheat switches off again (they are static).</summary>
    [TearDown]
    public void Clean() => DevToolsState.ResetAll();

    /// <summary>A new run's world on the golden seed, its pet adopted as the Title would.</summary>
    private WorldState Fresh()
    {
        WorldState world = DayCycle.NewWorld(_run, _lib, Seed);
        DayCycle.Adopt(world, _lib, PetKind.Cat, "Biscuit", null);
        return world;
    }

    private static WorldState Copy(WorldState world) => JsonUtility.FromJson<WorldState>(JsonUtility.ToJson(world));

    /// <summary>One day by hand: its shift closed as played with nothing decided (<paramref name="played"/> false: an empty ledger), or played with its whole queue decided correctly; Home's night on the default care; Sleep. False when the run ended.</summary>
    private bool PlayDay(WorldState world, bool played)
    {
        if (played)
        {
            if (DayCycle.PlayShiftCorrectly(world, _lib, _config) != null)
                return false;
        }
        else if (DayCycle.EndShift(world, new ShiftLedger(), Array.Empty<CaseInstance>(), null, _lib, _config)
                 && EndingService.Evaluate(world, _lib, _config, EndingMoment.Immediate) != null)
            return false;
        DayCycle.HomeNight(world, _lib, _config);
        return DayCycle.Sleep(world, _lib, _config) == null;
    }

    /// <summary>The two worlds are the same, field by field (their saved JSON), the cheated mark aside; a difference names its first line.</summary>
    private static void AssertSame(WorldState expected, WorldState actual, string what)
    {
        WorldState e = Copy(expected), a = Copy(actual);
        e.cheated = a.cheated = false;
        string[] el = JsonUtility.ToJson(e, true).Split('\n'), al = JsonUtility.ToJson(a, true).Split('\n');
        for (int i = 0; i < Math.Min(el.Length, al.Length); i++)
            Assert.AreEqual(el[i], al[i], $"{what}: the first difference, at line {i + 1} of the saved world");
        Assert.AreEqual(el.Length, al.Length, $"{what}: the saved worlds differ in length");
    }

    [Test, Category("NeedsEditor")]
    public void Skip_IsTheShiftsClose_HomesNightOnTheDefaultCare_AndSleep()
    {
        WorldState cheat = Fresh(), play = Copy(cheat);
        int money = cheat.money;

        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Skip(false), _run));
        Assert.IsTrue(PlayDay(play, false));

        AssertSame(play, cheat, "skip vs day 1 played through");
        Assert.AreEqual(2, cheat.day);
        Assert.Less(cheat.money, money, "rent and the night's bills were charged (a skip used to charge nothing)");
        Assert.IsTrue(cheat.cheated);
    }

    [Test, Category("NeedsEditor")]
    public void JumpForward_ToDay6_ClosesTodayAsPlayed_AndPlaysEveryDayBetween()
    {
        WorldState cheat = Fresh(), play = Copy(cheat);

        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Jump(1, 6, _lib.LastDay, false), _run));
        Assert.IsTrue(PlayDay(play, false), "day 1 ended the run");
        for (int day = 2; day < 6; day++)
            Assert.IsTrue(PlayDay(play, true), $"day {day} ended the run");

        AssertSame(play, cheat, "jump to 6 vs day 1 closed and days 2-5 played");
        Assert.AreEqual(6, cheat.day);
    }

    [Test, Category("NeedsEditor")]
    public void AJumpToTheLastDay_LandsThere_AsAPlayedRunWould()
    {
        WorldState cheat = Fresh();
        EndingSO ending = CheatRoute.Play(ref cheat, RunRoutes.Jump(1, _lib.LastDay, _lib.LastDay, false), _run);
        Assert.IsNull(ending, $"the jump ended the run on day {cheat.day}: '{ending?.id}' (QA reaches days 8, 12 and 15 by jumps)");
        Assert.AreEqual(_lib.LastDay, cheat.day);
        Assert.AreEqual(RunPhase.Office, cheat.phase);
        Assert.Greater(cheat.accountDays.Count, 0, "the statement has the days played");
    }

    [Test, Category("NeedsEditor")]
    public void JumpBack_ToDay3_IsANewRunPlayedToDay3_WithTheSamePet()
    {
        WorldState cheat = Fresh();
        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Jump(1, 6, _lib.LastDay, false), _run));
        cheat.pet.hunger = 3;
        cheat.money += 999;

        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Jump(6, 3, _lib.LastDay, false), _run));

        WorldState play = Fresh();
        Assert.IsTrue(PlayDay(play, true));
        Assert.IsTrue(PlayDay(play, true));
        AssertSame(play, cheat, "jump back to 3 vs a new run played to day 3");
        Assert.AreEqual("Biscuit", cheat.pet.name, "the same pet, adopted afresh");
        Assert.AreEqual(PetKind.Cat, cheat.pet.kind);
    }

    [Test, Category("NeedsEditor")]
    public void Restart_RestoresTheMorning_MoneyCitationsPremadesAndThePet()
    {
        WorldState cheat = Fresh();
        string morning = JsonUtility.ToJson(cheat);
        cheat.money += 77;
        cheat.citationsToday = 2;
        cheat.totalCitations = 2;
        cheat.SetFlag(FlagKeys.PremadeMet("ledger-test"));
        cheat.pet.hunger = 3;
        cheat.phase = RunPhase.Home;
        cheat.endingId = "fired";

        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Restart(), _run, morning: () => JsonUtility.FromJson<WorldState>(morning)));

        AssertSame(JsonUtility.FromJson<WorldState>(morning), cheat, "restart vs the morning");
        Assert.IsFalse(cheat.HasMetPremade("ledger-test"), "a premade met today comes again");
    }

    [Test, Category("NeedsEditor")]
    public void Restart_WithNoMorningSnapshot_KeepsTheWorld_AndLiftsTheEnding()
    {
        WorldState cheat = Fresh();
        cheat.phase = RunPhase.Home;
        cheat.endingId = "fired";
        UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("No morning snapshot"));

        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Restart(), _run, morning: () => null));

        Assert.AreEqual(RunPhase.Office, cheat.phase);
        Assert.AreEqual(string.Empty, cheat.endingId);
    }

    [Test, Category("NeedsEditor")]
    public void ASkipFromHome_StartsItsNightFromTheEndOfShiftSave_NeverChargingTheArrivalTwice()
    {
        WorldState endOfShift = Fresh();
        DayCycle.EndShift(endOfShift, new ShiftLedger(), Array.Empty<CaseInstance>(), null, _lib, _config);
        WorldState cheat = Copy(endOfShift);
        DayCycle.OpenHome(cheat, _lib, _config, Seeds.Day(cheat.runSeed, cheat.day));   // the Home scene charged the arrival in memory

        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Skip(true), _run, endOfShift: () => Copy(endOfShift)));

        WorldState play = Copy(endOfShift);
        DayCycle.HomeNight(play, _lib, _config);
        Assert.IsNull(DayCycle.Sleep(play, _lib, _config));
        AssertSame(play, cheat, "skip from Home vs the night from the end-of-shift save");
    }

    [Test, Category("NeedsEditor")]
    public void TheLiveShift_IsClosedOnItsOwnLedger_Once_TheDaysJumpedOverOnEmptyOnes()
    {
        WorldState cheat = Fresh(), play = Copy(cheat);
        int closes = 0;
        Func<bool> live = () =>
        {
            closes++;
            return DayCycle.EndShift(cheat, new ShiftLedger(), Array.Empty<CaseInstance>(), null, _lib, _config);
        };

        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Jump(1, 4, _lib.LastDay, false), _run, closeLive: live));

        Assert.AreEqual(1, closes, "the live shift closes once, on day 1");
        Assert.IsTrue(PlayDay(play, false));
        for (int day = 2; day < 4; day++)
            Assert.IsTrue(PlayDay(play, true));
        AssertSame(play, cheat, "jump with a live day 1");
    }

    [Test, Category("NeedsEditor")]
    public void ACheatGrant_CancelsThePendingOrder_SoItIsNeverDeliveredAgain()
    {
        WorldState world = Fresh();
        world.money = 100000;
        UpgradeSO item = OrderBook.Catalogue(_lib).FirstOrDefault(u => OrderBook.StateOf(world, _lib, u) == OrderState.Orderable);
        Assert.IsNotNull(item, "an orderable item on day 1");
        Assert.IsTrue(OrderBook.Order(world, _lib, item));
        Assert.IsNotNull(Orders.Pending(world.orders, item.id));

        DevCheats.GrantAll(world, _lib);

        Assert.IsNull(Orders.Pending(world.orders, item.id), "the order was cancelled");
        Assert.IsTrue(world.HasUpgrade(item.id));
        world.day++;
        List<UpgradeSO> delivered = OrderBook.Deliver(world, _lib);
        CollectionAssert.DoesNotContain(delivered, item, "never delivered again");
    }

    [Test, Category("NeedsEditor")]
    public void ASkipFromHome_KeepsTheCheatChangesMadeAtHome()
    {
        WorldState endOfShift = Fresh();
        DayCycle.EndShift(endOfShift, new ShiftLedger(), Array.Empty<CaseInstance>(), null, _lib, _config);
        WorldState cheat = Copy(endOfShift);
        DayCycle.OpenHome(cheat, _lib, _config, Seeds.Day(cheat.runSeed, cheat.day));   // the Home scene charged the arrival in memory
        var edits = new CheatEdits();
        void AtHome(string label, Action<WorldState> edit)   // as DevCheats.Run does: the change made, and noted
        {
            edit(cheat);
            edits.Note(label, edit);
        }
        float stability = StabilityRules.Round(cheat.timelineStability - 1f);
        AtHome("stability set", w => w.timelineStability = stability);
        AtHome("money +100", w => w.money += 100);
        AtHome("flag set", w => w.SetFlag("ledger-test-rook"));

        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Skip(true), _run, endOfShift: () => edits.ReplayOn(Copy(endOfShift))));

        WorldState play = edits.ReplayOn(Copy(endOfShift));
        DayCycle.HomeNight(play, _lib, _config);
        Assert.IsNull(DayCycle.Sleep(play, _lib, _config));
        AssertSame(play, cheat, "skip from Home with cheats vs the cheats on the end-of-shift save, then the night");
        Assert.IsTrue(cheat.HasFlag("ledger-test-rook"), "the flag cheat survives the skip");
    }

    [Test, Category("NeedsEditor")]
    public void AJump_KeepsTheNextGenerationsSwitches_ForTheDayItLandsOn()
    {
        LegendarySO premade = _lib.Legendaries.FirstOrDefault(l => l != null && l.oncePerRun && !string.IsNullOrWhiteSpace(l.id));
        Assert.IsNotNull(premade, "a once-per-run premade in the library");
        WorldState plain = Fresh();
        Assert.IsTrue(PlayDay(plain, false), "day 1 ended the run");
        Assert.AreEqual(2, plain.day);
        WorldState cheat = Copy(plain);
        Assert.IsNull(CheatRoute.Play(ref plain, RunRoutes.Jump(2, 8, _lib.LastDay, false), _run), "the jump with no switch set");

        // Force appearance (a premade) and Law-breaker next on day 2, then the jump to 8 (DevCheats.Run sets the switch, then jumps).
        ForcedCaseSlot appearance = DevCheats.Appearance(_lib, "premade:" + premade.id);
        DevToolsState.ForcedAppearance = appearance;
        DevToolsState.ForcedLie = LieKind.SwappedPhoto;
        Assert.IsNull(CheatRoute.Play(ref cheat, RunRoutes.Jump(2, 8, _lib.LastDay, false), _run));

        Assert.AreEqual(8, cheat.day);
        Assert.AreSame(appearance, DevToolsState.ForcedAppearance, "the forced appearance waits for day 8 (days 3-7 never took it)");
        Assert.AreEqual(LieKind.SwappedPhoto, DevToolsState.ForcedLie, "Law-breaker next waits for day 8");
        Assert.AreEqual(plain.HasMetPremade(premade.id), cheat.HasMetPremade(premade.id), "the premade was not met on a day the jump passed over");
        AssertSame(plain, cheat, "the days passed over are the same with the switches waiting as without them");

        Assert.IsNull(DayCycle.PlayShiftCorrectly(cheat, _lib, _config), "day 8 played");
        Assert.IsNull(DevToolsState.ForcedAppearance, "day 8's generation used it");
        Assert.IsTrue(cheat.HasMetPremade(premade.id), "they stood first on day 8");
    }
}
