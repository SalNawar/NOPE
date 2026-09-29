using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The run's day steps that need no scene (redesign phase 23): a new run's
/// world, one decision at the desk, the shift's close, Home's bills and the
/// night. RunManager, GameManager and HomeManager run them between their UI
/// and their saves; the balance simulation (BalanceSimulation, Tools >
/// TimeDesk > Balance) runs the same steps headless, so what it measures is
/// the game's own flow, never a copy of it.
/// </summary>
public static class DayCycle
{
    /// <summary>
    /// A new run's world: <paramref name="run"/>'s starting day, money,
    /// stability and family, <paramref name="runSeed"/>, and the places'
    /// baselines ranked, so the first night reports only what day 1 changed.
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

        if (run.startingFamilyMembers != null)
        {
            foreach (string name in run.startingFamilyMembers)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                world.family.members.Add(new FamilyMemberData { name = name, condition = 0 });
            }
        }

        TimelineService.SeedDominance(world, lib, run.gameConfig);
        return world;
    }

    /// <summary>
    /// One decision at the desk: ShiftScoring's verdict (the pay, or the one
    /// wrong-decision penalty and the stability loss; the evidence gate reads
    /// <paramref name="evidenceCount"/>, -1 when the evidence system is not
    /// active) goes onto <paramref name="ledger"/>; an accepted traveller is
    /// dispatched: the timeline impacts land on the claimed place, their tell
    /// source's carry is recorded and a costume error's panic is noted.
    /// </summary>
    public static CaseVerdict Decide(CaseInstance inst, bool accepted, int caseIndex1Based, int evidenceCount,
                                     WorldState world, TodaysWorld today, ShiftLedger ledger, ContentLibrarySO lib, GameConfigSO config)
    {
        CaseVerdict verdict = ShiftScoring.ResolveDecision(inst, accepted, caseIndex1Based, world, config, lib, evidenceCount);
        ledger.verdicts.Add(verdict);

        if (accepted)
        {
            TimelineService.ApplyVerdictImpacts(inst, inst.claimedEra, verdict.correct, world, lib);
            HistoryService.RecordCarry(world, inst, today.Facts, config);
            HistoryService.RecordPanic(world, inst);
        }

        return verdict;
    }

    /// <summary>
    /// The shift's close, applied before the end-of-shift save so a Continue
    /// replay of the day never applies it twice: the strandings among the
    /// accepted travellers (their carries and news; no money), the count of
    /// the shift's Debt Relief departures for the next morning's paper
    /// (WorldState.debtReliefYesterday, redesign phase 9), the clerk's
    /// Debt Relief instalment out of the shift's pay, and the narrative
    /// dialogs' outcomes. True when the instalment was taken or a dialog's
    /// effect applied, so the caller checks the endings (the wallet may have moved).
    /// </summary>
    public static bool CloseShift(WorldState world, ShiftLedger ledger, IReadOnlyList<CaseInstance> cases, TodaysWorld today, ContentLibrarySO lib, GameConfigSO config)
    {
        ShiftStrandings.Resolve(world, ledger, cases, today, lib, config);
        world.debtReliefYesterday = ledger != null ? ledger.DebtReliefDepartures : 0;
        bool instalmentTaken = ClerkAccountSource.TakeInstalment(world, ledger, lib) > 0;
        bool effectApplied = ApplyDialogOutcomes(world, ledger, lib);
        return instalmentTaken || effectApplied;
    }

    /// <summary>
    /// Home's arrival: bills the day's living costs (HomeEconomy) and lets the
    /// family's untreated conditions drift on <paramref name="daySeed"/> (so a
    /// reload bills and drifts the same); returns the bill.
    /// </summary>
    public static HomeEconomy.ExpenseReport OpenHome(WorldState world, GameConfigSO config, int daySeed)
    {
        HomeEconomy.ExpenseReport bill = HomeEconomy.ApplyDailyExpenses(world, config);
        HomeEconomy.AdvanceFamilyConditions(world, config, daySeed);
        return bill;
    }

    /// <summary>
    /// The night after Sleep found no ending: the nightly timeline resolve
    /// (before the day turns, so triggers read today), then the next morning
    /// at the office with no citations yet.
    /// </summary>
    public static void AdvanceNight(WorldState world, ContentLibrarySO lib, GameConfigSO config)
    {
        TimelineService.NightlyResolve(world, lib, config);

        world.day++;
        world.citationsToday = 0;
        world.phase = RunPhase.Office;
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
