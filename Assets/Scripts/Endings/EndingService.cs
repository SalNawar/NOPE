using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Evaluates EndingSO conditions against the current WorldState: failures
/// after every verdict and at the end of a shift that applied dialog
/// consequences (GameManager, EndingMoment.Immediate); failures, the
/// Retirement milestone and the attribute epilogues at the day boundary
/// (RunManager.Sleep, EndingMoment.DayBoundary). Which one wins is the Domain
/// rule EndingRules.Select.
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
        Debug.Log($"[EndingService] >>> Entering Evaluate ({moment}; day {world?.day}, money={world?.money}, stability={world?.timelineStability:0.#}).");

        if (world == null || lib == null)
        {
            Debug.LogWarning("[EndingService] <<< Exiting Evaluate early — null world/library.");
            return null;
        }

        var endings = new List<EndingSO>();
        var candidates = new List<EndingCandidate>();
        foreach (EndingSO ending in lib.Endings)
        {
            if (ending == null)
                continue;

            bool met = Matches(ending, world, config);
            if (met)
                Debug.Log($"[EndingService] Evaluate: ending '{ending.id}' ({ending.displayName}) matches (priority={ending.priority}, {EndingRules.KindOf(ending.conditionType)}).");

            endings.Add(ending);
            candidates.Add(new EndingCandidate(EndingRules.KindOf(ending.conditionType), ending.priority, met));
        }

        int winner = EndingRules.Select(candidates, moment);
        EndingSO best = winner >= 0 ? endings[winner] : null;

        if (best != null)
            Debug.Log($"[EndingService] <<< Exiting Evaluate (selected '{best.id}' ({best.displayName}), priority={best.priority}).");
        else
            Debug.Log("[EndingService] <<< Exiting Evaluate (no ending matched, run continues).");

        return best;
    }

    /// <summary>Returns true if the given ending's condition currently holds.</summary>
    private static bool Matches(EndingSO ending, WorldState world, GameConfigSO config)
    {
        switch (ending.conditionType)
        {
            case EndingConditionType.Fired:
            {
                float firedAt = config != null ? config.firedAtStability : 0f;
                bool pass = world.timelineStability <= firedAt;
                Debug.Log($"[EndingService] Matches '{ending.id}' (Fired): stability={world.timelineStability:0.#} <= {firedAt:0.#} -> {pass}.");
                return pass;
            }

            case EndingConditionType.Bankrupt:
            {
                int threshold = config != null ? config.bankruptcyMoneyThreshold : -100;
                bool pass = world.money <= threshold;
                Debug.Log($"[EndingService] Matches '{ending.id}' (Bankrupt): money={world.money} <= {threshold} -> {pass}.");
                return pass;
            }

            case EndingConditionType.AttrTotalAtLeast:
            {
                if (ending.attribute == null)
                {
                    Debug.Log($"[EndingService] Matches '{ending.id}' (AttrTotalAtLeast): no attribute configured -> false.");
                    return false;
                }

                float score = world.timeline.GetScore(TimelineKeys.GlobalAttr(ending.attribute));
                bool pass = score >= ending.threshold;
                Debug.Log($"[EndingService] Matches '{ending.id}' (AttrTotalAtLeast): {ending.attribute.displayName}={score:0.#} >= {ending.threshold:0.#} -> {pass}.");
                return pass;
            }

            case EndingConditionType.DayAtLeast:
            {
                bool pass = world.day >= ending.threshold;
                Debug.Log($"[EndingService] Matches '{ending.id}' (DayAtLeast): day={world.day} >= {ending.threshold:0.#} -> {pass}.");
                return pass;
            }

            default:
                Debug.Log($"[EndingService] Matches '{ending.id}': unknown conditionType '{ending.conditionType}' -> false.");
                return false;
        }
    }
}
