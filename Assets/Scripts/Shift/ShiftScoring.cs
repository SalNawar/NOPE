using UnityEngine;

/// <summary>
/// Stateless verdict resolver: applies pay, citations, and stability rules
/// to a player's Accept/Deny decision and mutates the WorldState accordingly
/// (one decision path, audit R3-011 and R3-019: the legacy era-pick verdict
/// is gone).
/// </summary>
public static class ShiftScoring
{
    /// <summary>
    /// Resolves a binary ACCEPT/DENY decision (investigation feature) into a
    /// CaseVerdict and applies its consequences. Correct = the player's choice
    /// matches CaseInstance.ShouldAccept (accept a traveller with no fault;
    /// deny a deviation fault or a directive fault, traveller types P1).
    /// </summary>
    public static CaseVerdict ResolveDecision(
        CaseInstance inst,
        bool accepted,
        int caseIndex1Based,
        WorldState world,
        GameConfigSO config,
        ContentLibrarySO lib = null,
        int evidenceCount = -1)
    {
        bool shouldAccept = inst != null && inst.ShouldAccept;

        Debug.Log($"[ShiftScoring] >>> Entering ResolveDecision (case {caseIndex1Based}, accepted={accepted}, shouldAccept={shouldAccept}, fault='{inst?.FaultReason}', directive={inst?.directiveFault}, evidence={evidenceCount}).");

        var verdict = new CaseVerdict
        {
            caseIndex1Based = caseIndex1Based,
            visitorName = inst != null ? inst.visitorDisplayName : "Unknown",
            wasLegendary = inst != null && inst.isLegendary,
            accepted = accepted,
            kind = inst != null ? inst.kind : default,
            debt = inst?.account != null ? inst.account.Debt : 0,
            shouldAccept = shouldAccept,
            faultReason = inst != null ? inst.FaultReason : string.Empty,
            destinationLabel = inst != null ? inst.originLabel ?? string.Empty : string.Empty,
            claimSummary = inst != null ? inst.claimLine : string.Empty,
            evidenceCount = Mathf.Max(0, evidenceCount),
            correct = inst != null && accepted == shouldAccept
        };

        if (world == null || config == null)
        {
            Debug.LogError("ShiftScoring.ResolveDecision missing world/config — verdict recorded without consequences.");
            return verdict;
        }

        // Evidence gate: denying a deviation fault (a liar, a forger, a costume
        // error) must be backed by documented scanner evidence. Directive
        // faults are exempt (the daily rules are public knowledge), and
        // evidenceCount < 0 means the evidence system is not active in this
        // scene (fallback UI) so the gate is skipped.
        if (inst != null && VerdictRules.IsUnprovenDenial(config.requireEvidenceToDeny, evidenceCount, accepted, inst.HasDeviationFault, inst.HasDirectiveFault))
        {
            verdict.correct = false;
            verdict.unprovenDenial = true;
            Debug.Log($"[ShiftScoring] Case {caseIndex1Based}: deny was factually right but had no documented evidence — treating as unproven denial.");
        }

        if (verdict.correct)
            ApplyCorrect(verdict, world, config, lib);
        else
            ApplyWrongDecision(verdict, world, config);

        world.timelineStability = Mathf.Clamp(world.timelineStability, 0f, 100f);
        verdict.firedNow = world.timelineStability <= config.firedAtStability;

        Debug.Log($"[ShiftScoring] <<< Exiting ResolveDecision (case {caseIndex1Based}, correct={verdict.correct}, pay={verdict.payAwarded}, penalty={verdict.moneyPenalty}, stability={world.timelineStability:0.#}, firedNow={verdict.firedNow}).");

        return verdict;
    }

    /// <summary>Citation + stability loss for a wrong accept/deny decision (the mistake line is the verdict's MistakeKey: the fault's reason for a wrong accept).</summary>
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

        string mistake = UiText.Format(v.MistakeKey, v.destinationLabel);

        if (world.citationsToday <= config.freeWarningsPerDay)
        {
            v.wasFreeWarning = true;
            v.citationText = Citation(mistake, UiText.Format("citation.warning", world.citationsToday, config.freeWarningsPerDay), v.stabilityDelta);
        }
        else
        {
            int penalizedIndex = world.citationsToday - config.freeWarningsPerDay;
            v.moneyPenalty = config.GetCitationPenalty(penalizedIndex);
            world.money -= v.moneyPenalty;
            v.citationText = Citation(mistake, UiText.Format("citation.penalty", v.moneyPenalty, UiText.Currency(UiText.WalletForm.Inline)), v.stabilityDelta);
        }

        Debug.Log($"[ShiftScoring] ApplyWrongDecision: accepted={v.accepted}, mistake='{v.MistakeKey}', citationsToday={world.citationsToday}, penalty={v.moneyPenalty}, stabilityDelta={v.stabilityDelta:0.#}, money={world.money}.");
    }

    /// <summary>Pay + optional stability gain for a correct send.</summary>
    private static void ApplyCorrect(CaseVerdict v, WorldState world, GameConfigSO config, ContentLibrarySO lib)
    {
        // Pay rate = base multiplier (slot machine) + stacked PayRateBonus effects.
        float payRate = Mathf.Max(0f, world.payRateMultiplier)
                        + (lib != null ? TimelineEffects.SumFloat(world, lib, EffectOpType.PayRateBonus) : 0f);

        float pay = config.basePayPerCorrect * Mathf.Max(0f, payRate);

        if (v.wasLegendary)
            pay += config.legendaryBonusPay;

        v.payAwarded = Mathf.RoundToInt(pay);
        v.stabilityDelta = config.stabilityGainPerCorrect;

        world.money += v.payAwarded;
        world.timelineStability += v.stabilityDelta;

        Debug.Log($"[ShiftScoring] ApplyCorrect: payRate={payRate:0.##}, basePay={config.basePayPerCorrect}, legendaryBonus={(v.wasLegendary ? config.legendaryBonusPay : 0)}, payAwarded={v.payAwarded}, stabilityDelta=+{v.stabilityDelta:0.#}, money={world.money}.");
    }

    /// <summary>A citation slip's text: the title, the mistake, the warning or penalty line and the stability change (UI string keys; piece 6).</summary>
    private static string Citation(string mistake, string consequence, float stabilityDelta) =>
        UiText.Format("citation.layout", UiText.Get("citation.title"), mistake, consequence, UiText.Format("citation.stability", stabilityDelta));
}
