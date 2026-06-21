// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Upgrade definition used for gating clue generation and for the Home shop (Phase 4).
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_", menuName = "TimeDesk/Upgrade", order = 13)]
public sealed class UpgradeSO : ScriptableObject
{
    /// <summary>Stable upgrade ID used in WorldState (e.g., "scanner").</summary>
    public string id;

    /// <summary>Display name shown in UI.</summary>
    public string displayName;

    /// <summary>Shop blurb shown under the display name.</summary>
    [TextArea]
    public string description;

    /// <summary>Base credits cost in the Home shop (before TimelineEffects discounts).</summary>
    [Min(0)]
    public int cost;

    /// <summary>Optional effect applied once when this upgrade is purchased.</summary>
    public EffectSO unlockEffect;
}
