using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The run's day steps that need no scene (redesign phase 23): a new run's
/// world, one decision at the desk, the shift's close, Home's bills and the
/// night (with the Orders app's deliveries at the next day's start).
/// RunManager, GameManager and HomeManager run them between their UI
/// and their saves; the balance simulation (BalanceSimulation, Tools >
/// TimeDesk > Balance) runs the same steps headless, so what it measures is
/// the game's own flow, never a copy of it. The cheat menu's skips, jumps
/// and restarts (RUN-SAFE, BUG-28a) run them too, with Home's night on the
/// default care (HomeNight), so a cheat lands where play would.
/// </summary>
public static class DayCycle
{
    /// <summary>
    /// A new run's world: <paramref name="run"/>'s starting day, money and
    /// stability, its default pet (RunConfigSO.startingPetKind with the
    /// content's suggested name; the Title's adoption replaces it, Adopt),
    /// <paramref name="runSeed"/>, and the places' baselines ranked, so the
    /// first night reports only what day 1 changed.
    /// </summary>
    public static WorldState NewWorld(RunConfigSO run, ContentLibrarySO lib, int runSeed)
    {
        var world = new WorldState
        {
            day = run.startingDay,
            money = run.startingMoney,
            timelineStability = run.startingStability,
            runSeed = runSeed
        };

        Adopt(world, lib, run.startingPetKind, null, null);
        TimelineService.SeedDominance(world, lib, run.gameConfig);
        return world;
    }

    /// <summary>
    /// The run's pet (the Home pet spec PS1, PS11): a fresh <paramref name="kind"/>
    /// with every need met, called <paramref name="name"/> (PetNames.Clean),
    /// or the content's suggested name for the kind when the name is blank or
    /// refused (PetNames.Check at home.pet.nameMaxLength: the Title checks
    /// before it adopts, so only a run started elsewhere gets the suggestion),
    /// in <paramref name="coat"/> when the kind lists it, else the kind's
    /// first coat (PetContent.CoatOf).
    /// </summary>
    public static void Adopt(WorldState world, ContentLibrarySO lib, PetKind kind, string name, string coat)
    {
        if (world == null)
            return;
        PetContent words = lib != null ? lib.Home.pet : new PetContent();
        PetKindContent k = words.Kind(kind);
        string chosen = PetNames.Check(name, words.nameMaxLength) == PetNameProblem.None ? PetNames.Clean(name) : k != null ? PetNames.Clean(k.suggestedName) : kind.ToString();
        world.pet = new PetState { kind = kind, name = chosen, coat = words.CoatOf(kind, coat), adoptedDay = world.day };
        Debug.Log($"[DayCycle] Adopted a {kind} called '{chosen}' ({world.pet.coat} coat).");
    }

    /// <summary>
    /// A traveller comes to the desk: a once-per-run premade is marked met
    /// (FlagKeys.PremadeMet), so they never roll again and a later forced slot
    /// for them holds an ordinary traveller. The game calls it when it shows
    /// the case; a repeatable premade is never marked. A returning traveller
    /// (wave 5, lesson 9) is marked returned: they come back once.
    /// </summary>
    public static void Present(WorldState world, CaseInstance inst)
    {
        if (world != null && inst != null && inst.isLegendary && inst.legendarySource != null && inst.legendarySource.oncePerRun)
            world.SetFlag(FlagKeys.PremadeMet(inst.legendarySource.id));
        if (world != null && inst != null && inst.returning != null)
            inst.returning.returned = true;
    }

    /// <summary>
    /// One decision at the desk: ShiftScoring's verdict (the pay, or the one
    /// wrong-decision penalty and the stability loss; the evidence gate reads
    /// <paramref name="evidenceCount"/>, -1 when the evidence system is not
    /// active) goes onto <paramref name="ledger"/>; a premade's verdict is
    /// remembered (FlagKeys.PremadeVerdictChange: the latest decision wins),
    /// for the forced slots and story rules that read it; the decision pulls
    /// the world's outcomes (WorldOutcomeService.RecordDecision: an accepted
    /// traveller toward their destination's leanings, a denial toward "as you
    /// found it"); an accepted traveller is dispatched: the timeline impacts
    /// land on the claimed place, their tell source's carry is recorded and a
    /// costume error's panic is noted. A returning traveller's second verdict
    /// is kept for the paper's desk section; a generated traveller denied on
    /// a first visit may come back (PlanReturn; wave 5, lesson 9). A detained
    /// traveller (the third verdict, the desk machine spec §2) counts as
    /// denied for the world and the premade's memory, never comes back (only
    /// a denial plans a return) and is added to the run's detentions.
    /// </summary>
    public static CaseVerdict Decide(CaseInstance inst, DeskStamp decision, int caseIndex1Based, int evidenceCount,
                                     WorldState world, TodaysWorld today, ShiftLedger ledger, ContentLibrarySO lib, GameConfigSO config)
    {
        bool accepted = decision == DeskStamp.Approved;
        CaseVerdict verdict = ShiftScoring.ResolveDecision(inst, decision, caseIndex1Based, world, config, lib, evidenceCount);
        ledger.verdicts.Add(verdict);

        if (inst != null && inst.isLegendary && inst.legendarySource != null && world != null)
        {
            (string set, string clear) memory = FlagKeys.PremadeVerdictChange(inst.legendarySource.id, accepted);
            world.SetFlag(memory.set);
            world.ClearFlag(memory.clear);
        }

        WorldOutcomeService.RecordDecision(world, inst, accepted, lib, config);

        if (inst != null && world != null)
            Visits.Record(world.visits ??= new List<VisitEntry>(), inst.RecordKey, world.day, lib != null ? AgencyCalendar.Today(lib.Agency.firstDate, world.day) : null,
                          inst.originLabel, accepted ? Visits.Accepted : Visits.Denied, verdict.citationIssued ? verdict.citationReason : null);

        if (accepted)
        {
            TimelineService.ApplyVerdictImpacts(inst, inst.claimedEra, verdict.correct, world, lib);
            HistoryService.RecordCarry(world, inst, today.Facts, config);
            HistoryService.RecordPanic(world, inst);
        }

        if (inst != null && inst.returning != null && world != null)
        {
            inst.returning.backDay = world.day;
            inst.returning.acceptedBack = accepted;
            inst.returning.reported = false;
        }
        else if (decision == DeskStamp.Denied)
        {
            PlanReturn(world, inst, config);
        }

        if (decision == DeskStamp.Detained && world != null)
            world.totalDetained++;

        return verdict;
    }

    /// <summary>
    /// A generated traveller denied on a first visit may come back (wave 5,
    /// lesson 9): Returns.Plan on their own return stream (Seeds.ForReturn of
    /// their case seed) with GameConfigSO's recurring-faces knobs; when they
    /// will, who they are is kept in WorldState.returns (name, role, kind,
    /// claim, gender, birth date, personality, Citizen ID, case seed). A
    /// premade never enters (their returns are the day plans').
    /// </summary>
    private static void PlanReturn(WorldState world, CaseInstance inst, GameConfigSO config)
    {
        if (world == null || inst == null || config == null || inst.claimedNation == null || inst.claimedEra == null)
            return;
        ReturningTraveller back = Returns.Plan(!inst.isLegendary, config.returnChance, config.returnAfterDaysMin, config.returnAfterDaysMax,
                                               config.returnCorrectedChance, world.day, Seeds.ForReturn(inst.caseSeed));
        if (back == null)
            return;
        back.name = inst.visitorGivenName ?? string.Empty;
        back.displayName = inst.visitorDisplayName ?? string.Empty;
        back.kind = inst.kind;
        back.archetypeId = inst.archetype != null ? inst.archetype.id : string.Empty;
        back.nationId = inst.claimedNation.id;
        back.eraId = inst.claimedEra.id;
        back.gender = inst.gender;
        back.birthDate = inst.trueBirthDate ?? string.Empty;
        back.personality = inst.personality ?? string.Empty;
        back.citizenId = inst.account != null ? inst.account.CitizenId ?? string.Empty : string.Empty;
        back.caseSeed = inst.caseSeed;
        world.returns ??= new List<ReturningTraveller>();
        world.returns.Add(back);
        Debug.Log($"[DayCycle] '{back.displayName}' may come back ({back.story}) between day {back.fromDay} and day {back.untilDay}.");
    }

    /// <summary>
    /// The shift's close, applied before the end-of-shift save so a Continue
    /// replay of the day never applies it twice: the strandings among the
    /// accepted travellers (their fates: carries, tremors and the paper's
    /// lines; the stranding fine where no valid signed waiver was on file), the count of
    /// the shift's Debt Relief departures for the next morning's paper
    /// (WorldState.debtReliefYesterday, redesign phase 9), the clerk's
    /// Debt Relief instalment out of the shift's pay, and the narrative
    /// dialogs' outcomes (the money they move kept as the ledger's
    /// otherMoney, the shift report's other money). True when a stranding's fine or tremor, the
    /// instalment or a dialog's effect applied, so the caller checks the
    /// endings (the wallet or stability may have moved).
    /// </summary>
    public static bool CloseShift(WorldState world, ShiftLedger ledger, IReadOnlyList<CaseInstance> cases, TodaysWorld today, ContentLibrarySO lib, GameConfigSO config)
    {
        bool stranded = ShiftStrandings.Resolve(world, ledger, cases, today, lib, config);
        world.debtReliefYesterday = ledger != null ? ledger.DebtReliefDepartures : 0;
        bool instalmentTaken = ClerkAccountSource.TakeInstalment(world, ledger, lib) > 0;
        int walletBefore = world.money;
        bool effectApplied = ApplyDialogOutcomes(world, ledger, lib);
        if (ledger != null)
            ledger.otherMoney = world.money - walletBefore;
        return stranded || instalmentTaken || effectApplied;
    }

    /// <summary>
    /// The shift's end as play ends it (GameManager's day end), less its
    /// screens: the close (CloseShift), the day's row on the clerk's
    /// statement, Home as the resume point, and the one-day tomorrow
    /// modifiers reset (today's shift consumed them). The cheat menu's skips
    /// and jumps end a shift here (RUN-SAFE, BUG-28a): the live one on its
    /// ledger, a day jumped over on an empty one. True when the close moved
    /// the wallet or stability (CloseShift), so the caller checks the endings.
    /// </summary>
    public static bool EndShift(WorldState world, ShiftLedger ledger, IReadOnlyList<CaseInstance> cases, TodaysWorld today, ContentLibrarySO lib, GameConfigSO config)
    {
        bool moved = CloseShift(world, ledger, cases, today, lib, config);
        ClerkAccountSource.RecordShift(world, ledger, lib, config);
        world.phase = RunPhase.Home;
        ResetTomorrowModifiers(world);
        return moved;
    }

    /// <summary>
    /// A day's shift played headless with every traveller decided correctly
    /// (the cheat menu's Auto-decide: approved without a fault, denied with
    /// one, with the evidence a denial needs), the whole queue: the travellers
    /// the office generates (the day's plan, interview and world on the day's
    /// seed), each presented and decided (Present, Decide) with the ending
    /// check play runs after each verdict, then the shift's end (EndShift) and
    /// its check. RUN-SAFE (BUG-28a): the days a cheat jump passes over, so it
    /// lands with the pay, citations, premades met and pet a played run has.
    /// Returns the ending, or null.
    /// </summary>
    public static EndingSO PlayShiftCorrectly(WorldState world, ContentLibrarySO lib, GameConfigSO config)
    {
        var ledger = new ShiftLedger();
        DayPlanSO plan = lib != null ? lib.GetDayPlan(world.day) : null;
        var cases = new List<CaseInstance>();
        TodaysWorld today = null;
        if (plan != null)
        {
            InterviewDay interview = TimelineService.BuildInterviewDay(lib, world, ledger);
            today = lib.BuildToday(plan, world);
            cases = new CaseFactory(lib, today).GenerateDayCases(plan, world, Seeds.Day(world.runSeed, world.day), interview, true);
        }

        for (int i = 0; i < cases.Count; i++)
        {
            CaseInstance inst = cases[i];
            Present(world, inst);
            Decide(inst, inst.ShouldAccept ? DeskStamp.Approved : DeskStamp.Denied, i + 1, 1, world, today, ledger, lib, config);
            EndingSO now = config != null ? EndingService.Evaluate(world, lib, config, EndingMoment.Immediate) : null;
            if (now != null)
                return now;
        }

        bool moved = EndShift(world, ledger, cases, today, lib, config);
        return moved && config != null ? EndingService.Evaluate(world, lib, config, EndingMoment.Immediate) : null;
    }

    /// <summary>The one-day "tomorrow modifiers" (set by the slot machine for the next day) after a shift consumed them: back to none.</summary>
    public static void ResetTomorrowModifiers(WorldState world)
    {
        world.legendaryChanceBonus = 0f;
        world.forgeryChanceModifier = 0f;
        world.payRateMultiplier = 1f;
    }

    /// <summary>
    /// Home's night on the default care, as Home plays it with every choice
    /// left as offered (RUN-SAFE, BUG-28a: "simulate the night with default
    /// care"), on the day's seed: the arrival (OpenHome: the break-in and the
    /// fixed bill), tonight's bills on the starting care and the pet's night
    /// on it (NightRules.DefaultNight), and the night's row on the clerk's
    /// statement. Nothing is bought. Returns the household's cost.
    /// </summary>
    public static int HomeNight(WorldState world, ContentLibrarySO lib, GameConfigSO config)
    {
        int seed = Seeds.Day(world.runSeed, world.day);
        HomeEconomy.ExpenseReport bill = OpenHome(world, lib, config, seed);
        int household = bill.total + NightRules.DefaultNight(world, lib, config, seed);
        ClerkAccountSource.RecordHome(world, household, 0, lib, config);
        return household;
    }

    /// <summary>
    /// Home's arrival (the Home upgrades spec §5, the Home pet spec PS2), in
    /// this order on <paramref name="daySeed"/> so a reload replays it:
    /// tonight's break-in (it happened while the clerk was at work), then the
    /// fixed bill (rent and utilities, the sick pet's extra care, the house's
    /// upkeep; HomeEconomy, with the house upgrades' effects). The night's
    /// optional bills follow at the bills step (HomeEconomy.PayBills) and the
    /// pet's night at Sleep (HomeEconomy.PetNight). Returns the bill, the
    /// break-in included.
    /// </summary>
    public static HomeEconomy.ExpenseReport OpenHome(WorldState world, ContentLibrarySO lib, GameConfigSO config, int daySeed)
    {
        int breakIn = HomeEconomy.RollBreakIn(world, lib, config, daySeed);
        return HomeEconomy.ApplyDailyExpenses(world, lib, config, breakIn);
    }

    /// <summary>
    /// The night after Sleep found no ending: the nightly timeline resolve
    /// (before the day turns, so triggers read today), then the next morning
    /// at the office with no citations yet, and the Orders app's deliveries
    /// (OrderBook.Deliver: the chosen scanner goes in, every order placed
    /// before today arrives, owned with its unlock effect from today), so
    /// the office's day-start snapshot counts them.
    /// </summary>
    public static void AdvanceNight(WorldState world, ContentLibrarySO lib, GameConfigSO config)
    {
        CloseDay(world, lib, config);
        TurnDay(world, lib);
    }

    /// <summary>
    /// Sleep (RunManager.Sleep, after Home; NightOrder, BUG-56b): the
    /// day-boundary ending check (failures, and the run's last day, which
    /// still gets its own night resolve: EndRun); else the night's resolve,
    /// then the failures checked again on the world the night left, so a
    /// night story event that pushes the run past a line ends it that night,
    /// never at the next morning's first correct stamp; else the next morning
    /// (as AdvanceNight). Returns the ending, or null when the day turned.
    /// </summary>
    public static EndingSO Sleep(WorldState world, ContentLibrarySO lib, GameConfigSO config) =>
        NightOrder.Run(
            () => EndingService.Evaluate(world, lib, config, EndingMoment.DayBoundary),
            ending => EndRun(world, ending, lib, config),
            () => CloseDay(world, lib, config),
            () => EndingService.Evaluate(world, lib, config, EndingMoment.Immediate),
            () => TurnDay(world, lib));

    /// <summary>The day turns: the next morning at the office with no citations yet, and the Orders app's deliveries (OrderBook.Deliver).</summary>
    private static void TurnDay(WorldState world, ContentLibrarySO lib)
    {
        world.day++;
        world.citationsToday = 0;
        world.phase = RunPhase.Office;
        OrderBook.Deliver(world, lib);
    }

    /// <summary>
    /// The day's night resolve (TimelineService.NightlyResolve: the leader,
    /// the story and history rules, the world's answers, the carries, the next
    /// morning's paper) without turning the day: AdvanceNight's first step,
    /// and the run's last night (EndRun).
    /// </summary>
    private static void CloseDay(WorldState world, ContentLibrarySO lib, GameConfigSO config)
    {
        TimelineService.NightlyResolve(world, lib, config);
    }

    /// <summary>
    /// The run ends at the day boundary on <paramref name="ending"/>
    /// (RunManager.Sleep, and the balance simulation): the run's last day
    /// (EndingKind.Milestone) still gets its own night resolve, so the world
    /// the end of the demo shows includes that day's choices (the leader, the
    /// story rules' pulls, the world's answers); a failure leaves the world as
    /// its last night latched it. The day does not turn.
    /// </summary>
    public static void EndRun(WorldState world, EndingSO ending, ContentLibrarySO lib, GameConfigSO config)
    {
        if (world != null && ending != null && EndingRules.KindOf(ending.conditionType) == EndingKind.Milestone)
            CloseDay(world, lib, config);
    }

    /// <summary>
    /// Applies what the shift's completed dialogs decided (DialogOutcomes):
    /// sets each one-shot dialog's done flag, and activates each named effect
    /// once, with its instant ops now and a start day of tomorrow (so its
    /// briefing and news lines reach the next morning's paper). Returns true
    /// when any effect was applied.
    /// </summary>
    private static bool ApplyDialogOutcomes(WorldState world, ShiftLedger ledger, ContentLibrarySO lib)
    {
        if (ledger == null || world == null)
            return false;

        foreach (string flag in DialogOutcomes.FlagsToSet(ledger.dialogOutcomes))
            world.SetFlag(flag);

        bool applied = false;
        foreach (DialogOutcome outcome in DialogOutcomes.EffectsToApply(ledger.dialogOutcomes))
        {
            EffectSO fx = lib != null ? lib.GetEffectByAssetName(outcome.effectName) : null;
            if (fx == null)
            {
                Debug.LogWarning($"[DayCycle] Dialog '{outcome.dialogId}' names effect '{outcome.effectName}', which ContentLibrary_Main does not list; add it to the library's effects.");
                continue;
            }

            TimelineService.ActivateEffect(world, fx, $"Dialog: {outcome.dialogId}", world.day + 1, fx.defaultDurationDays, applyInstantOps: true);
            applied = true;
        }

        return applied;
    }
}
