using System.Linq;
using System.Reflection;
using NUnit.Framework;

/// <summary>
/// RUN-SAFE's cheat switches (BUG-27a, BUG-25b): every switch has exactly one
/// scope; a morning (ResetDay) clears the day's (the forced hour, auto-decide,
/// the forced leader, the hall's overrides) and keeps the run's and the next
/// generation's, which the badge names loudly, with the language lock kept
/// elsewhere (CultureThemeService.LockOverride); a jump holds the next
/// generation's aside and puts them back; the cheat menu's Dusk is the
/// one day cycle's dusk (HallDayCycle.DuskHour). Pure statics: these run
/// outside Unity as well as in its EditMode suite, except the one in the
/// "NeedsEditor" category (clearing a switch logs it, and the log needs the
/// engine).
/// </summary>
public class DevToolsStateTests
{
    /// <summary>Every switch back off, before and after each test (they are static).</summary>
    [SetUp, TearDown]
    public void Clean()
    {
        DevToolsState.ForceLegendaryNextCase = false;
        DevToolsState.ForcedLeaderId = null;
        DevToolsState.ForcedCostumeError = CostumeError.None;
        DevToolsState.ForcedLie = null;
        DevToolsState.ForceStrandings = false;
        DevToolsState.ForcedStrandingFate = null;
        DevToolsState.ForcedPersonality = null;
        DevToolsState.AutoDecide = false;
        DevToolsState.UnlockEverything = false;
        DevToolsState.ForcedAppearance = null;
        DevToolsState.ForcedHour = null;
        CultureThemeService.LockOverride = null;
        HallSlotsDev.ShowAllStandIns = false;
        HallSlotsDev.Culture = null;
        HallSlotsDev.Tier = null;
        HallSlotsDev.Phase = null;
        HallSlotsDev.Event = null;
        HallSlotsDev.Exhibit = null;
    }

    [Test]
    public void EverySwitch_HasExactlyOneScope()
    {
        string[] switches = typeof(DevToolsState).GetFields(BindingFlags.Public | BindingFlags.Static)
                                                 .Where(f => !f.IsInitOnly && !f.IsLiteral).Select(f => f.Name).ToArray();
        string[] scoped = DevToolsState.DayScoped.Concat(DevToolsState.RunScoped).Concat(DevToolsState.NextGeneration).ToArray();
        CollectionAssert.AreEquivalent(switches, scoped, "a new switch must be put in one scope (day, run or next generation)");
        Assert.AreEqual(scoped.Length, scoped.Distinct().Count(), "a switch in two scopes");
    }

    [Test, Category("NeedsEditor")]
    public void AMorning_ClearsTheDaysSwitches_AndTheHallsOverrides()
    {
        DevToolsState.ForcedHour = 22f;
        DevToolsState.AutoDecide = true;
        DevToolsState.ForcedLeaderId = "rome";
        HallSlotsDev.Tier = StabilityTier.Strained;
        HallSlotsDev.Culture = "rome";
        HallSlotsDev.ShowAllStandIns = true;
        int version = HallSlotsDev.Version;

        DevToolsState.ResetDay();

        Assert.IsNull(DevToolsState.ForcedHour, "the hall shows the clock's hour again");
        Assert.IsFalse(DevToolsState.AutoDecide, "the new day is played by hand");
        Assert.IsNull(DevToolsState.ForcedLeaderId, "the culture's day reset");
        Assert.IsNull(HallSlotsDev.Tier);
        Assert.IsNull(HallSlotsDev.Culture);
        Assert.IsFalse(HallSlotsDev.ShowAllStandIns);
        Assert.Greater(HallSlotsDev.Version, version, "the hall re-reads its state");
    }

    [Test]
    public void AMorning_KeepsTheRunsSwitches_AndTheNextGenerations()
    {
        DevToolsState.UnlockEverything = true;
        DevToolsState.ForceStrandings = true;
        DevToolsState.ForcedPersonality = "blunt";
        DevToolsState.ForceLegendaryNextCase = true;
        DevToolsState.ForcedLie = LieKind.SwappedPhoto;
        DevToolsState.ForcedCostumeError = (CostumeError)1;

        DevToolsState.ResetDay();

        Assert.IsTrue(DevToolsState.UnlockEverything);
        Assert.IsTrue(DevToolsState.ForceStrandings);
        Assert.AreEqual("blunt", DevToolsState.ForcedPersonality);
        Assert.IsTrue(DevToolsState.ForceLegendaryNextCase, "the jump or restart that applies it keeps it");
        Assert.AreEqual(LieKind.SwappedPhoto, DevToolsState.ForcedLie);
        Assert.AreEqual((CostumeError)1, DevToolsState.ForcedCostumeError);
    }

    [Test]
    public void TheBadge_NamesEveryStickySwitch_Loudly()
    {
        Assert.AreEqual(string.Empty, DevToolsState.StickyLine(false), "a clean run shows nothing");
        Assert.AreEqual("CHEATS ON", DevToolsState.StickyLine(true));

        DevToolsState.UnlockEverything = true;
        DevToolsState.ForceStrandings = true;
        DevToolsState.ForcedLie = LieKind.SwappedPhoto;
        string line = DevToolsState.StickyLine(false);
        StringAssert.StartsWith("CHEATS ON", line, "a switch on marks the run even before the first cheat lands");
        StringAssert.Contains("Unlock everything", line);
        StringAssert.Contains("Strandings forced", line);
        StringAssert.Contains("Lie next", line);
    }

    [Test]
    public void TheBadgesKey_ChangesExactlyWhenItsLineWould()
    {
        int clean = DevToolsState.StickyKey(false);
        Assert.AreEqual(clean, DevToolsState.StickyKey(false));
        Assert.AreNotEqual(clean, DevToolsState.StickyKey(true));
        DevToolsState.ForceStrandings = true;
        Assert.AreNotEqual(clean, DevToolsState.StickyKey(false));
        DevToolsState.ForceStrandings = false;
        DevToolsState.ForcedHour = 22f;
        Assert.AreEqual(clean, DevToolsState.StickyKey(false), "a day switch is not on the badge");
    }

    [Test]
    public void TheCheatMenusDusk_IsTheDayCyclesDusk()
    {
        Assert.AreEqual(HallDayCycle.DuskHour, DevCheats.HourOf(2), "BUG-25b: one dusk");
        Assert.AreEqual("Dusk", DevCheats.TimeNames[2]);
        Assert.IsNull(DevCheats.HourOf(0), "Clock: the shift clock's hour");
    }

    [Test]
    public void TheTimeOfDayChoice_FollowsTheForcedHour()
    {
        Assert.AreEqual(0, DevCheats.TimeChoice);
        DevToolsState.ForcedHour = DevCheats.HourOf(2);
        Assert.AreEqual(2, DevCheats.TimeChoice);
        DevToolsState.ForcedHour = 3.25f;
        Assert.AreEqual(0, DevCheats.TimeChoice, "an hour no choice has reads as the clock's");
    }

    [Test]
    public void TheLanguageLock_IsKeptElsewhere_AndNamedOnTheBadge()
    {
        CollectionAssert.Contains(DevToolsState.Elsewhere, "CultureThemeService.LockOverride");
        Assert.IsNotNull(typeof(CultureThemeService).GetProperty(nameof(CultureThemeService.LockOverride), BindingFlags.Public | BindingFlags.Static), "the switch Elsewhere names");
        int clean = DevToolsState.StickyKey(false);

        CultureThemeService.LockOverride = true;
        StringAssert.Contains("Language locked", DevToolsState.StickyLine(false), "BUG-27: the language lock is never silent");
        int locked = DevToolsState.StickyKey(false);
        Assert.AreNotEqual(clean, locked);

        CultureThemeService.LockOverride = false;
        StringAssert.Contains("Language unlocked", DevToolsState.StickyLine(false));
        Assert.AreNotEqual(locked, DevToolsState.StickyKey(false));
    }

    [Test, Category("NeedsEditor")]
    public void ANewRun_ClearsTheLanguageLock()
    {
        CultureThemeService.LockOverride = true;
        DevToolsState.ResetAll();
        Assert.IsNull(CultureThemeService.LockOverride, "the language follows the ramp again");
        Assert.AreEqual(string.Empty, DevToolsState.StickyLine(false));
    }

    [Test]
    public void HoldingTheNextGeneration_TurnsItOff_AndRestoringPutsItBack()
    {
        var appearance = new ForcedCaseSlot { caseIndex1Based = 1, id = "beat" };
        DevToolsState.ForceLegendaryNextCase = true;
        DevToolsState.ForcedCostumeError = (CostumeError)1;
        DevToolsState.ForcedLie = LieKind.SwappedPhoto;
        DevToolsState.ForcedAppearance = appearance;
        DevToolsState.UnlockEverything = true;

        DevToolsState.HeldSwitches held = DevToolsState.HoldNextGeneration();

        Assert.IsFalse(DevToolsState.ForceLegendaryNextCase, "a day a jump passes over generates without them");
        Assert.AreEqual(CostumeError.None, DevToolsState.ForcedCostumeError);
        Assert.IsNull(DevToolsState.ForcedLie);
        Assert.IsNull(DevToolsState.ForcedAppearance);
        Assert.IsTrue(DevToolsState.UnlockEverything, "only the next generation's are held");
        Assert.AreEqual(DevToolsState.NextGeneration.Length, held.Values.Length, "every next-generation switch is held, by name");

        DevToolsState.ForcedLie = LieKind.SwappedPhoto;   // a held day's generation never sees it, but a restore wins over anything left
        DevToolsState.RestoreNextGeneration(held);

        Assert.IsTrue(DevToolsState.ForceLegendaryNextCase);
        Assert.AreEqual((CostumeError)1, DevToolsState.ForcedCostumeError);
        Assert.AreEqual(LieKind.SwappedPhoto, DevToolsState.ForcedLie);
        Assert.AreSame(appearance, DevToolsState.ForcedAppearance, "the same appearance reaches the day the jump lands on");
    }

    [Test]
    public void RestoringNothingHeld_ChangesNothing()
    {
        DevToolsState.ForcedLie = LieKind.SwappedPhoto;
        DevToolsState.RestoreNextGeneration(default);
        Assert.AreEqual(LieKind.SwappedPhoto, DevToolsState.ForcedLie);
    }
}
