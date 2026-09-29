using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Evaluates EndingSO conditions against the current WorldState: failures
/// after every verdict and at the end of a shift that applied dialog
/// consequences (GameManager, EndingMoment.Immediate); failures and the run's
/// last day (the neutral "world you made" ending) at the day boundary
/// (RunManager.Sleep, EndingMoment.DayBoundary). Whether a condition holds is
/// the Domain rule EndingRules.Met, which one wins EndingRules.Select.
/// </summary>
public static class EndingService
{
    /// <summary>
    /// Returns the EndingSO that ends the run at this moment (EndingRules.Select
    /// over every library ending: its kind, priority and whether its condition
    /// matches now), or null if none does (run continues).
    /// </summary>
    public static EndingSO Evaluate(WorldState world, ContentLibrarySO lib, GameConfigSO config, EndingMoment moment)
    {
        if (world == null || lib == null)
        {
            Debug.LogWarning("[EndingService] Evaluate: no world or content library, so no ending is checked.");
            return null;
        }

        var now = new EndingCheck(world.timelineStability, world.money, world.day,
            config != null ? config.firedAtStability : 0f,
            config != null ? config.bankruptcyMoneyThreshold : -100);

        var endings = new List<EndingSO>();
        var candidates = new List<EndingCandidate>();
        foreach (EndingSO ending in lib.Endings)
        {
            if (ending == null)
                continue;

            endings.Add(ending);
            candidates.Add(new EndingCandidate(EndingRules.KindOf(ending.conditionType), ending.priority, EndingRules.Met(ending.conditionType, ending.threshold, now)));
        }

        int winner = EndingRules.Select(candidates, moment);
        EndingSO best = winner >= 0 ? endings[winner] : null;

        if (best != null)
            Debug.Log($"[EndingService] {moment}: the run ends with '{best.id}' ({best.displayName}), priority {best.priority}.");

        return best;
    }
}
