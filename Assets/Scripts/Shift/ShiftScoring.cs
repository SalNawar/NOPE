using UnityEngine;

/// <summary>
/// Stateless verdict resolver: applies pay, citations, and stability rules
/// to a player decision and mutates the WorldState accordingly.
/// </summary>
public static class ShiftScoring
{
    /// <summary>Library used to resolve active pay-rate effects (set per Resolve call).</summary>
    private static ContentLibrarySO _lib;

    /// <summary>
    /// Resolves a player decision into a CaseVerdict and applies its
    /// money/stability/citation consequences to the world state.
    /// </summary>
    public static CaseVerdict Resolve(
        CaseInstance inst,
        EraSO chosenEra,
        int caseIndex1Based,
        WorldState world,
        GameConfigSO config,
        ContentLibrarySO lib = null)
    {
        _lib = lib;

        Debug.Log($"[ShiftScoring] >>> Entering Resolve (case {caseIndex1Based}, chosenEra='{chosenEra?.id}', trueEra='{inst?.trueEra?.id}').");

        var verdict = new CaseVerdict
        {
            caseIndex1Based = caseIndex1Based,
            visitorName = inst != null ? inst.visitorDisplayName : "Unknown",
            chosenEraId = chosenEra != null ? chosenEra.id : string.Empty,
            trueEraId = inst != null && inst.trueEra != null ? inst.trueEra.id : string.Empty,
            wasLegendary = inst != null && inst.isLegendary,
            correct = inst != null && inst.trueEra == chosenEra
        };

        if (world == null || config == null)
        {
            Debug.LogError("ShiftScoring.Resolve missing world/config — verdict recorded without consequences.");
            return verdict;
        }

        if (verdict.correct)
            ApplyCorrect(verdict, world, config);
        else
            ApplyWrong(verdict, world, config);

        // Track sends for the timeline system (Phase 2 reads these).
        if (chosenEra != null && !string.IsNullOrEmpty(chosenEra.id))
            world.AddCounter($"sent:era:{chosenEra.id}", 1);

        // Clamp and check firing condition.
        world.timelineStability = Mathf.Clamp(world.timelineStability, 0f, 100f);
        verdict.firedNow = world.timelineStability <= config.firedAtStability;

        Debug.Log($"[ShiftScoring] <<< Exiting Resolve (case {caseIndex1Based}, correct={verdict.correct}, pay={verdict.payAwarded}, penalty={verdict.moneyPenalty}, stabilityDelta={verdict.stabilityDelta:+0.#;-0.#}, stability={world.timelineStability:0.#}, firedNow={verdict.firedNow}).");

        return verdict;
    }

    /// <summary>
    /// Resolves a binary ACCEPT/DENY decision (investigation feature) into a
    /// CaseVerdict and applies its consequences. Correct = the player's choice
    /// matches CaseInstance.ShouldAccept (accept a genuine, permitted traveler;
    /// deny a forgery or a rule-breaking destination).
    /// </summary>
    public static CaseVerdict ResolveDecision(
        CaseInstance inst,
        bool accepted,
        int caseIndex1Based,
        WorldState world,
        GameConfigSO config,
        ContentLibrarySO lib = null)
    {
        _lib = lib;

        bool shouldAccept = inst != null && inst.ShouldAccept;

        Debug.Log($"[ShiftScoring] >>> Entering ResolveDecision (case {caseIndex1Based}, accepted={accepted}, shouldAccept={shouldAccept}, forged={inst?.isForged}, claimAllowed={inst?.claimAllowedByRules}).");

        var verdict = new CaseVerdict
        {
            caseIndex1Based = caseIndex1Based,
            visitorName = inst != null ? inst.visitorDisplayName : "Unknown",
            chosenEraId = inst != null && inst.claimedEra != null ? inst.claimedEra.id : string.Empty,
            trueEraId = inst != null && inst.claimedEra != null ? inst.claimedEra.id : string.Empty,
            wasLegendary = inst != null && inst.isLegendary,
            accepted = accepted,
            shouldAccept = shouldAccept,
            wasForged = inst != null && inst.isForged,
            claimAllowed = inst == null || inst.claimAllowedByRules,
            claimSummary = inst != null ? inst.claimLine : string.Empty,
            correct = inst != null && accepted == shouldAccept
        };

        if (world == null || config == null)
        {
            Debug.LogError("ShiftScoring.ResolveDecision missing world/config — verdict recorded without consequences.");
            return verdict;
        }

        if (verdict.correct)
            ApplyCorrect(verdict, world, config);
        else
            ApplyWrongDecision(verdict, world, config);

        // Track sends only when the traveler is actually dispatched (accepted).
        if (accepted && inst != null && inst.claimedEra != null && !string.IsNullOrEmpty(inst.claimedEra.id))
            world.AddCounter($"sent:era:{inst.claimedEra.id}", 1);

        world.timelineStability = Mathf.Clamp(world.timelineStability, 0f, 100f);
        verdict.firedNow = world.timelineStability <= config.firedAtStability;

        Debug.Log($"[ShiftScoring] <<< Exiting ResolveDecision (case {caseIndex1Based}, correct={verdict.correct}, pay={verdict.payAwarded}, penalty={verdict.moneyPenalty}, stability={world.timelineStability:0.#}, firedNow={verdict.firedNow}).");

        return verdict;
    }

    /// <summary>Citation + stability loss for a wrong accept/deny decision.</summary>
    private static void ApplyWrongDecision(CaseVerdict v, WorldState world, GameConfigSO config)
    {
        world.citationsToday++;
        world.totalCitations++;
        v.citationIssued = true;

        float stabilityLoss = config.stabilityLossPerWrong;
        if (v.wasLegendary)
            stabilityLoss += config.extraStabilityLossLegendary;

        v.stabilityDelta = -stabilityLoss;
        world.timelineStability += v.stabilityDelta;

        string mistake = v.accepted
            ? "Approved travel on forged or forbidden papers."
            : "Denied a legitimate, permitted traveler.";

        if (world.citationsToday <= config.freeWarningsPerDay)
        {
            v.wasFreeWarning = true;
            v.citationText =
                $"TIMELINE DEVIATION NOTICE\n{mistake}\n" +
                $"Warning {world.citationsToday}/{config.freeWarningsPerDay} — no pay deduction.\n" +
                $"Stability {v.stabilityDelta:+0.#;-0.#}";
        }
        else
        {
            int penalizedIndex = world.citationsToday - config.freeWarningsPerDay;
            v.moneyPenalty = config.GetCitationPenalty(penalizedIndex);
            world.money -= v.moneyPenalty;
            v.citationText =
                $"TIMELINE DEVIATION NOTICE\n{mistake}\n" +
                $"Penalty: -{v.moneyPenalty} credits.\n" +
                $"Stability {v.stabilityDelta:+0.#;-0.#}";
        }

        Debug.Log($"[ShiftScoring] ApplyWrongDecision: accepted={v.accepted}, citationsToday={world.citationsToday}, penalty={v.moneyPenalty}, stabilityDelta={v.stabilityDelta:0.#}, money={world.money}.");
    }

    /// <summary>Pay + optional stability gain for a correct send.</summary>
    private static void ApplyCorrect(CaseVerdict v, WorldState world, GameConfigSO config)
    {
        // Pay rate = base multiplier (slot machine) + stacked PayRateBonus effects.
        float payRate = Mathf.Max(0f, world.payRateMultiplier)
                        + (_lib != null ? TimelineEffects.SumFloat(world, _lib, EffectOpType.PayRateBonus) : 0f);

        float pay = config.basePayPerCorrect * Mathf.Max(0f, payRate);

        if (v.wasLegendary)
            pay += config.legendaryBonusPay;

        v.payAwarded = Mathf.RoundToInt(pay);
        v.stabilityDelta = config.stabilityGainPerCorrect;

        world.money += v.payAwarded;
        world.timelineStability += v.stabilityDelta;

        Debug.Log($"[ShiftScoring] ApplyCorrect: payRate={payRate:0.##}, basePay={config.basePayPerCorrect}, legendaryBonus={(v.wasLegendary ? config.legendaryBonusPay : 0)}, payAwarded={v.payAwarded}, stabilityDelta=+{v.stabilityDelta:0.#}, money={world.money}.");
    }

    /// <summary>Citation (warning or penalized) + stability loss for a wrong send.</summary>
    private static void ApplyWrong(CaseVerdict v, WorldState world, GameConfigSO config)
    {
        world.citationsToday++;
        world.totalCitations++;
        v.citationIssued = true;

        float stabilityLoss = config.stabilityLossPerWrong;
        if (v.wasLegendary)
            stabilityLoss += config.extraStabilityLossLegendary;

        v.stabilityDelta = -stabilityLoss;
        world.timelineStability += v.stabilityDelta;

        if (world.citationsToday <= config.freeWarningsPerDay)
        {
            v.wasFreeWarning = true;
            v.citationText =
                $"TIMELINE DEVIATION NOTICE\n" +
                $"Subject misrouted: sent to '{v.chosenEraId}', belonged to '{v.trueEraId}'.\n" +
                $"Warning {world.citationsToday}/{config.freeWarningsPerDay} — no pay deduction.\n" +
                $"Stability {v.stabilityDelta:+0.#;-0.#}";

            Debug.Log($"[ShiftScoring] ApplyWrong: free warning {world.citationsToday}/{config.freeWarningsPerDay}, stabilityDelta={v.stabilityDelta:0.#} (legendary={v.wasLegendary}), no pay deduction.");
        }
        else
        {
            int penalizedIndex = world.citationsToday - config.freeWarningsPerDay;
            v.moneyPenalty = config.GetCitationPenalty(penalizedIndex);
            world.money -= v.moneyPenalty;

            v.citationText =
                $"TIMELINE DEVIATION NOTICE\n" +
                $"Subject misrouted: sent to '{v.chosenEraId}', belonged to '{v.trueEraId}'.\n" +
                $"Penalty: -{v.moneyPenalty} credits.\n" +
                $"Stability {v.stabilityDelta:+0.#;-0.#}";

            Debug.Log($"[ShiftScoring] ApplyWrong: citation #{world.citationsToday} (penalized index {penalizedIndex}), moneyPenalty={v.moneyPenalty}, stabilityDelta={v.stabilityDelta:0.#} (legendary={v.wasLegendary}), money={world.money}.");
        }
    }
}
