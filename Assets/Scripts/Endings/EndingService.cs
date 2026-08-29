using UnityEngine;

/// <summary>
/// Evaluates EndingSO conditions against the current WorldState. Called after
/// a verdict resolves (GameManager) and before sleep (HomeManager) to detect
/// game-over conditions (fired, bankrupt, score/day thresholds).
/// </summary>
public static class EndingService
{
    /// <summary>
    /// Returns the highest-priority EndingSO whose condition currently matches,
    /// or null if no ending in the library matches (run continues).
    /// </summary>
    public static EndingSO Evaluate(WorldState world, ContentLibrarySO lib, GameConfigSO config)
    {
        Debug.Log($"[EndingService] >>> Entering Evaluate (day {world?.day}, money={world?.money}, stability={world?.timelineStability:0.#}).");

        if (world == null || lib == null)
        {
            Debug.LogWarning("[EndingService] <<< Exiting Evaluate early — null world/library.");
            return null;
        }

        // Cushioning gate: before endingsMinDay, no ending may fire at all.
        // Both check sites (after verdict, before sleep) flow through here.
        if (config != null && !EndingGate.EvaluationAllowed(world.day, config.endingsMinDay))
        {
            Debug.Log($"[EndingService] <<< Exiting Evaluate — day {world.day} is below endingsMinDay={config.endingsMinDay}; run is cushioned.");
            return null;
        }

        EndingSO best = null;

        foreach (EndingSO ending in lib.Endings)
        {
            if (ending == null)
                continue;

            if (!Matches(ending, world, config))
                continue;

            Debug.Log($"[EndingService] Evaluate: ending '{ending.id}' ({ending.displayName}) matches (priority={ending.priority}).");

            if (best == null || ending.priority > best.priority)
                best = ending;
        }

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
