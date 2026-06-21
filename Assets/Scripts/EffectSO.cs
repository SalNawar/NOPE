// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Channel an effect broadcasts on. Receivers (visuals, music, UI, visitor pool,
/// shop, newsletter, tech tree, chatter...) query the active-effect registry for
/// their channel and react. New systems = new channel or new receiver, no core changes.
/// </summary>
public enum EffectChannel
{
    General,
    Visuals,
    Music,
    UI,
    VisitorPool,
    Shop,
    Newsletter,
    TechTree,
    SpecialCases,
    Chatter
}

/// <summary>
/// What a single effect operation does.
/// INSTANT ops run once when the effect activates.
/// CONTINUOUS ops are aggregated by TimelineEffects queries while the effect is active.
/// </summary>
public enum EffectOpType
{
    // ---- Instant (applied once on activation) ----
    SetFlag,                 // stringParam = flag
    ClearFlag,               // stringParam = flag
    AddCounter,              // stringParam = counter key, floatParam = amount
    AddMoney,                // floatParam = credits (can be negative)
    AddStability,            // floatParam = stability delta
    UnlockUpgrade,           // stringParam = upgrade id
    AddAttributeScore,       // profile + attribute + floatParam
    AddNationScore,          // nation + floatParam

    // ---- Continuous (queried while active) ----
    LegendaryChanceBonus,    // floatParam = +chance (0..1)
    ForgeryChanceBonus,      // floatParam = +contradiction chance (0..1)
    PayRateBonus,            // floatParam = +multiplier (0.25 = +25% pay)
    VisitorTagWeight,        // stringParam = archetype tag, floatParam = weight multiplier
    ShopDiscountPercent,     // stringParam = upgrade id ("" = all), floatParam = percent off
    CaseBlueprintWeight,     // stringParam = blueprint name, floatParam = weight multiplier
    Cue,                     // stringParam = cue id, consumed by receivers of this effect's channel
    BriefingLine,            // stringParam = line added to tomorrow's briefing
    NewsLine                 // stringParam = line added to tomorrow's newsletter
}

/// <summary>
/// A generic, channel-tagged bundle of operations. Activated by dominance tiers,
/// timeline triggers, slot outcomes, or scripted events; multiple effects stack
/// freely in WorldState.timeline.activeEffects, each with its own duration.
/// Looked up by asset name (ContentLibrarySO.GetEffectByAssetName).
/// </summary>
[CreateAssetMenu(fileName = "Effect_", menuName = "TimeDesk/Effect", order = 14)]
public sealed class EffectSO : ScriptableObject
{
    /// <summary>Display name for debugging/UI lists.</summary>
    public string displayName;

    /// <summary>Channel receivers use to find this effect's cues.</summary>
    public EffectChannel channel = EffectChannel.General;

    /// <summary>
    /// Default duration in days when activated (-1 = permanent until removed).
    /// Activators (triggers, slot outcomes) may override.
    /// </summary>
    public int defaultDurationDays = -1;

    /// <summary>The operations this effect performs.</summary>
    public List<EffectOp> ops = new();
}

/// <summary>One operation inside an effect. Unused fields are ignored per op type.</summary>
[Serializable]
public sealed class EffectOp
{
    /// <summary>Operation type (see EffectOpType for parameter meanings).</summary>
    public EffectOpType type;

    /// <summary>String parameter (flag, tag, upgrade id, cue id, line text...).</summary>
    public string stringParam;

    /// <summary>Numeric parameter (amount, multiplier, percent...).</summary>
    public float floatParam;

    /// <summary>Target attribute (AddAttributeScore).</summary>
    public AttributeSO attribute;

    /// <summary>Target nation (AddNationScore).</summary>
    public NationSO nation;

    /// <summary>Target profile (AddAttributeScore).</summary>
    public NationEraProfileSO profile;
}
