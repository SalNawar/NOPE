using System;
using System.Collections.Generic;

/// <summary>
/// Persistent run state. Fully serializable via JsonUtility (see SaveSystem).
/// Everything that must survive sleep/quit lives here — nothing else should
/// hold cross-day state.
/// </summary>
[Serializable]
public sealed class WorldState
{
    // -----------------------------
    // Run identity
    // -----------------------------

    /// <summary>Current day number (1-based).</summary>
    public int day = 1;

    /// <summary>Seed for this run; per-day seeds derive from