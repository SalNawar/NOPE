using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The run's day steps that need no scene (redesign phase 23): a new run's
/// world, one decision at the desk, the shift's close, Home's bills and the
/// night (with the Orders app's deliveries at the next day's start).
/// RunManager, GameManager and HomeManager run them between their UI
/// and their saves; the balance simulation (BalanceSimulation, Tools >
/// TimeDesk > Balance) runs the same steps headless, so what it measures is
/// the game's own flow, never a copy of it.
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

        Adopt(world, lib, run.startingPetKind, null);
        TimelineService.SeedDominance(world, lib, run.gameConfig);
        return world;
    }

    /// <summary>
    /// The run's pet (the Home pet spec PS1): a fresh <paramref name="kind"/>
    /// with every need met, called <paramref name="name"/> (PetNames.Clean),
    /// or the content's suggested name for the kind when the name is blank or
    /// refused (PetNames.Check at home.pet.nameMaxLength: the Title checks
    /// before it adopts, so only a run started elsewhere gets the suggestion).
    /// </summary>
    public static void Adopt(WorldState world, ContentLibrarySO lib, PetKind kind, string name)
    {
        if (world == null)
            return;
        PetContent words = lib != null ? lib.Home.pet : new PetContent();
        PetKindContent k = words.Kind(kind);
        string chosen = PetNames.Check(name, words.nameMaxLength) == PetNameProblem.None ? PetNames.Clean(name) : k != null ? PetNames.Clean(k.suggestedName) : kind.ToString();
        world.pet = new PetState { kind = kind, name = chosen, adoptedDay = world.day };
        Debug.Log($"[DayCycle] Adopted a {kind} called '{chosen}'.");
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
    /// a first visit may come back (PlanReturn; wave 5, lesson 9).
    /// </summary>
    public static CaseVerdict Decide(CaseInstance inst, bool accepted, int caseIndex1Based, int evidenceCount,
                                     WorldState world, TodaysWorld today, ShiftLedger ledger, ContentLibrarySO lib, GameConfigSO config)
    {
        CaseVerdict verdict = ShiftScoring.ResolveDecision(inst, accepted, caseIndex1Based, world, config, lib, evidenceCount);
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
        else if (!accepted)
        {
            PlanReturn(world, inst, config);
        }

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
