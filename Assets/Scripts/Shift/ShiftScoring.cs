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
    /// deny a deviation fault or a directive fault, traveller types P1), or
    /// denies a traveller whose waiver fault the desk's pad cured
    /// (VerdictRules.IsCorrect; the endings and strandings spec §7.3).
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
            wasLegendary = inst != null && inst.IsFamous,
            accepted = accepted,
            kind = inst != null ? inst.kind : default,
            debt = inst?.account != null ? inst.account.Debt : 0,
            shouldAccept = shouldAccept,
            faultReason = inst != null ? inst.FaultReason : string.Empty,
            destinationLabel = inst != null ? inst.originLabel ?? string.Empty : string.Empty,
            evidenceCount = Mathf.Max(0, evidenceCount),
            correct = inst != null && VerdictRules.IsCorrect(accepted, shouldAccept, inst.curedAtDesk != DirectiveFault.None)
        };

        if (world == null || config == null)
        {
            Debug.LogError("ShiftScoring.ResolveDecision missing world/config — verdict recorded without consequences.");
            return verdict;
        }

        // Evidence gate (Saleh, 2026-10-05): any right denial must be backed
        // by logged evidence, a deviation's proof or a directive fault's
        // finding; evidenceCount < 0 means the evidence system is not active
        // in this scene (fallback UI) so the gate is skipped.
        if (inst != null && VerdictRules.IsUnprovenDenial(config.requireEvidenceToDeny, evidenceCount, accepted, inst.HasDeviationFault, inst.HasDirectiveFault))
        {
            verdict.correct = false;
            verdict.unprovenDenial = true;
            Debug.Log($"[ShiftScoring] Case {caseIndex1Based}: deny was factually right but had no documented evidence — treating as unproven denial.");
        }

        if (verdict.correct)
            ApplyCorrect(verdict, world, config, lib);
        else
            ApplyWrongDecision(verdict, world, config, verdict.unprovenDenial ? Unproven(verdict.evidenceCount) : inst?.citation);

        verdict.firedNow = EndingRules.IsFired(world.timelineStability, config.firedAtStability);

        Debug.Log($"[ShiftScoring] <<< Exiting ResolveDecision (case {caseIndex1Based}, correct={verdict.correct}, pay={verdict.payAwarded}, penalty={verdict.moneyPenalty}, stability={StabilityRules.Format(world.timelineStability)}, firedNow={verdict.firedNow}).");

        return verdict;
    }

    /// <summary>
    /// Citation + stability loss for a wrong accept/deny decision (the mistake
    /// line is the verdict's MistakeKey: the fault's reason for a wrong accept;
    /// then the rule and the exact values of <paramref name="facts"/>, lesson
    /// 6: the traveller's fault for a wrong accept, the open destination for a
    /// wrong denial, the logged deviations for an unproven one); the money is
    /// the one penalty for any mistake (VerdictRules.WrongDecisionPenalty),
    /// the day's first mistakes free warnings (GameConfigSO.freeWarningsPerDay).
    /// </summary>
    private static void ApplyWrongDecision(CaseVerdict v, WorldState world, GameConfigSO config, CitationFacts facts)
    {
        world.citationsToday++;
        world.totalCitations++;
        v.citationIssued = true;

        float points = config.stabilityLossPerWrong;
        if (v.wasLegendary)
            points += config.extraStabilityLossLegendary;

        ChangeStability(v, world, -points, config);

        string mistake = UiText.Format(v.MistakeKey, v.destinationLabel);

        if (VerdictRules.IsFreeWarning(world.citationsToday, config.freeWarningsPerDay))
        {
            v.wasFreeWarning = true;
            v.citationText = Citation(v, mistake, facts, UiText.Format("citation.warning", world.citationsToday, config.freeWarningsPerDay));
        }
        else
        {
            v.moneyPenalty = VerdictRules.WrongDecisionPenalty(world.citationsToday, config.freeWarningsPerDay, config.wrongDecisionPenalty);
            world.money -= v.moneyPenalty;
            v.citationText = Citation(v, mistake, facts, UiText.Format("citation.penalty", v.moneyPenalty, UiText.Currency(UiText.WalletForm.Inline)));
        }

        Debug.Log($"[ShiftScoring] ApplyWrongDecision: accepted={v.accepted}, mistake='{v.MistakeKey}', citationsToday={world.citationsToday}, penalty={v.moneyPenalty}, stabilityDelta={StabilityRules.FormatChange(v.stabilityDelta)}, money={world.money}.");
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

        world.money += v.payAwarded;
        ChangeStability(v, world, config.stabilityGainPerCorrect, config);

        Debug.Log($"[ShiftScoring] ApplyCorrect: payRate={payRate:0.##}, basePay={config.basePayPerCorrect}, legendaryBonus={(v.wasLegendary ? config.legendaryBonusPay : 0)}, payAwarded={v.payAwarded}, stabilityDelta={StabilityRules.FormatChange(v.stabilityDelta)}, money={world.money}.");
    }

    /// <summary>Moves stability by <paramref name="points"/> at the config's rate (StabilityRules.Apply: a share of where it stands, in hundredths) and records the change on the verdict.</summary>
    private static void ChangeStability(CaseVerdict v, WorldState world, float points, GameConfigSO config)
    {
        float before = StabilityRules.Round(world.timelineStability);
        world.timelineStability = StabilityRules.Apply(before, points, config.stabilityChangeRate);
        v.stabilityDelta = world.timelineStability - before;
    }

    /// <summary>What an unproven denial's slip names: the evidence rule and the deviations logged (lesson 6).</summary>
    private static CitationFacts Unproven(int evidenceCount)
    {
        var facts = new CitationFacts { RuleKey = "citation.rule.evidence" };
        facts.Values.Add(new CitationValue(UiText.Get("citation.label.logged"), evidenceCount.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return facts;
    }

    /// <summary>
    /// A citation slip (UI string keys; piece 6, lesson 6): its lines on the
    /// verdict for the printed slip (the mistake, the rule it broke with its
    /// Directive Memo row and the exact values involved (Citations), the
    /// warning or penalty), and returned as one text for Mail's copy: the
    /// title and those lines (no stability number: the Helix River shows the damage).
    /// </summary>
    private static string Citation(CaseVerdict v, string mistake, CitationFacts facts, string consequence)
    {
        string rule = Citations.RuleLine(facts, UiText.Get, UiText.Get("citation.rule.numbered"));
        string values = Citations.ValuesLine(facts?.Values, UiText.Get("citation.value"), UiText.Get("citation.value.separator"));
        v.citationReason = mistake;
        v.citationDetail = string.IsNullOrEmpty(values) ? rule : rule + "\n" + values;
        v.citationConsequence = consequence;
        return UiText.Format("citation.layout", UiText.Get("citation.title"), mistake, rule, values, consequence);
    }
}
