// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// One possible result of a Home slot-machine spin (Phase 4). Outcomes are
/// picked with WeightedRandom and can move money immediately, set tomorrow's
/// slot modifiers (legendary/forgery/pay), and/or activate a generic EffectSO
/// (so a spin can trigger visuals, music, shop discounts, briefing lines...).
/// </summary>
[CreateAssetMenu(fileName = "SlotOutcome_", menuName = "TimeDesk/Home/Slot Outcome", order = 20)]
public sealed class SlotOutcomeSO : ScriptableObject
{
    /// <summary>Stable id (for debugging/save references).</summary>
    public string id;

    /// <summary>Display name shown in lists.</summary>
    public string displayName;

    /// <summary>Line shown to the player when this outcome is rolled.</summary>
    [TextArea]
    public string resultLine;

    /// <summary>Selection weight (relative). 0 = never selected.</summary>
    [Min(0f)]
    public float weight = 1f;

    [Header("Immediate")]
    /// <summary>Credits gained (positive) or lost (negative) immediately.</summary>
    public int moneyDelta;

    [Header("Tomorrow modifiers (added to WorldState, consumed by next shift)")]
    /// <summary>Added to WorldState.legendaryChanceBonus.</summary>
    public float legendaryChanceBonus;

    /// <summary>Added to WorldState.forgeryChanceModifier.</summary>
    public float forgeryChanceModifier;

    /// <summary>Added to WorldState.payRateMultiplier.</summary>
    public float payRateMultiplierDelta;

    [Header("Effect (optional)")]
    /// <summary>Effect activated when this outcome is rolled (any channel).</summary>
    public EffectSO effect;

    /// <summary>Override for the effect's duration in days. 0 = use the effect's default.</summary>
    public int durationDaysOverride;
}
