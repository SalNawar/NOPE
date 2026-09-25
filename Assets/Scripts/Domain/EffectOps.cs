/// <summary>
/// What an effect op does. Serialized as ints: append only.
/// INSTANT ops run once when the effect activates.
/// CONTINUOUS ops are aggregated by TimelineEffects queries while the effect is active.
/// </summary>
public enum EffectOpType
{
    // ---- Instant (applied once on activation) ----

    /// <summary>stringParam = flag</summary>
    SetFlag,

    /// <summary>stringParam = flag</summary>
    ClearFlag,

    /// <summary>stringParam = counter key, floatParam = amount</summary>
    AddCounter,

    /// <summary>floatParam = credits (can be negative)</summary>
    AddMoney,

    /// <summary>floatParam = stability delta</summary>
    AddStability,

    /// <summary>stringParam = upgrade id</summary>
    UnlockUpgrade,

    /// <summary>profile + attribute + floatParam</summary>
    AddAttributeScore,

    /// <summary>nation + floatParam</summary>
    AddNationScore,

    // ---- Continuous (queried while active) ----

    /// <summary>floatParam = +chance (0..1)</summary>
    LegendaryChanceBonus,

    /// <summary>floatParam = +liar chance (0..1)</summary>
    ForgeryChanceBonus,

    /// <summary>floatParam = +multiplier (0.25 = +25% pay)</summary>
    PayRateBonus,

    /// <summary>stringParam = archetype tag, floatParam = weight multiplier</summary>
    VisitorTagWeight,

    /// <summary>stringParam = upgrade id ("" = all), floatParam = percent off</summary>
    ShopDiscountPercent,

    /// <summary>stringParam = blueprint name, floatParam = weight multiplier</summary>
    CaseBlueprintWeight,

    /// <summary>stringParam = cue id, consumed by receivers of this effect's channel</summary>
    Cue,

    /// <summary>stringParam = line added to tomorrow's briefing</summary>
    BriefingLine,

    /// <summary>stringParam = line added to tomorrow's newsletter</summary>
    NewsLine,

    // ---- Instant, history rules only ----

    /// <summary>Instant: profile = the target place, category = the fact, stringParam = the new value. History rules only (EffectOps.HistoryOnly).</summary>
    SetFact
}

/// <summary>Rules over effect ops, pure so they are tested headless.</summary>
public static class EffectOps
{
    /// <summary>
    /// True for the continuous ops that change play for as long as the effect
    /// is active (legendary, liar and pay bonuses, visitor and blueprint
    /// weights, shop discounts, cues); false for the instant ops (SetFact
    /// included, see <see cref="HistoryOnly"/>) and for BriefingLine/NewsLine,
    /// which act once or only through the next morning's paper. A narrative
    /// dialog's effect may hold only the latter.
    /// </summary>
    public static bool ActsWhileActive(EffectOpType type)
    {
        switch (type)
        {
            case EffectOpType.LegendaryChanceBonus:
            case EffectOpType.ForgeryChanceBonus:
            case EffectOpType.PayRateBonus:
            case EffectOpType.VisitorTagWeight:
            case EffectOpType.ShopDiscountPercent:
            case EffectOpType.CaseBlueprintWeight:
            case EffectOpType.Cue:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Ops only a history rule's effect may hold: a night-latched fact write
    /// (SetFact). Generate World rejects them in dialog effects; the content
    /// validator rejects them in slot-outcome, upgrade, tier, leader and dialog
    /// effects and in a trigger that is not one-shot.
    /// </summary>
    public static bool HistoryOnly(EffectOpType type) => type == EffectOpType.SetFact;
}
