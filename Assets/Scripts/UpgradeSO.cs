// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Upgrade definition used for gating clue generation.
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_", menuName = "TimeDesk/Upgrade", order = 13)]
public sealed class UpgradeSO : ScriptableObject
{
    /// <summary>Stable upgrade ID used in WorldState (e.g., "scanner").</summary>
    public string id;

    /// <summary>Display name shown in UI.</summary>
    public string displayName;
}
