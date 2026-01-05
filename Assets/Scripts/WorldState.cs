using System.Collections.Generic;

/// <summary>
/// Persistent run/session state (save/load later).
/// Keep it simple and serializable.
/// </summary>
public sealed class WorldState
{
    /// <summary>Current day number (1-based).</summary>
    public int day = 1;

    /// <summary>Bonus added to legendary chance per case (0..1).</summary>
    public float legendaryChanceBonus = 0f;

    /// <summary>Unlocked upgrade IDs for gating clue generation.</summary>
    public readonly HashSet<string> unlockedUpgradeIds = new();
}
