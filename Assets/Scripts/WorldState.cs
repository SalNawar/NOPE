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

    /// <summary>Seed for this run; per-day seeds derive from it (see RunManager.GetDaySeed).</summary>
    public int runSeed;

    // -----------------------------
    // Economy + standing
    // -----------------------------

    /// <summary>Player money (credits).</summary>
    public int money;

    /// <summary>Timeline stability gauge (0..100). Reaching 0 means fired.</summary>
    public float timelineStability = 100f;

    /// <summary>Total citations issued across the run.</summary>
    public int totalCitations;

    /// <summary>Citations issued during the current day (resets each morning).</summary>
    public int citationsToday;

    // -----------------------------
    // Run outcome (Phase 5)
    // -----------------------------

    /// <summary>
    /// ID of the EndingSO reached (empty = run still in progress). Set by
    /// EndingService when a game-over condition matches; read by the title
    /// scene to display the ending and gate Continue vs New Run.
    /// </summary>
    public string endingId = string.Empty;

    // -----------------------------
    // Tomorrow modifiers (slot machine / effects write these)
    // -----------------------------

    /// <summary>Bonus added to legendary chance per case (0..1).</summary>
    public float legendaryChanceBonus;

    /// <summary>Additive modifier to contradiction/forgery chance for generated cases.</summary>
    public float forgeryChanceModifier;

    /// <summary>Multiplier applied to case pay (1 = normal).</summary>
    public float payRateMultiplier = 1f;

    // -----------------------------
    // Unlocks, flags, counters
    // -----------------------------

    /// <summary>Unlocked upgrade IDs for gating clue generation and shop state.</summary>
    public List<string> unlockedUpgradeIds = new();

    /// <summary>Arbitrary boolean story/consequence flags (e.g., "Tyrant_Rises").</summary>
    public List<string> flags = new();

    /// <summary>
    /// Generic named counters. Key conventions (Phase 2):
    /// "sent:era:{eraId}", "sent:tag:{archetypeTag}:{eraId}", etc.
    /// </summary>
    public List<CounterEntry> counters = new();

    // -----------------------------
    // Subsystem state blocks
    // -----------------------------

    /// <summary>Timeline variation scores + active effects (Phase 2).</summary>
    public TimelineStateData timeline = new();

    /// <summary>Family / household state (Phase 4).</summary>
    public FamilyStateData family = new();

    /// <summary>Resolved "tomorrow package" computed at sleep (briefing, news, modifiers).</summary>
    public TomorrowPackage tomorrow = new();

    // -----------------------------
    // Flag helpers
    // -----------------------------

    /// <summary>Returns true if the flag is set.</summary>
    public bool HasFlag(string flag) =>
        !string.IsNullOrEmpty(flag) && flags.Contains(flag);

    /// <summary>Sets a flag (no duplicates).</summary>
    public void SetFlag(string flag)
    {
        if (!string.IsNullOrEmpty(flag) && !flags.Contains(flag))
            flags.Add(flag);
    }

    /// <summary>Clears a flag if present.</summary>
    public void ClearFlag(string flag) => flags.Remove(flag);

    // -----------------------------
    // Counter helpers
    // -----------------------------

    /// <summary>Returns the value of a named counter (0 if missing).</summary>
    public int GetCounter(string key)
    {
        if (string.IsNullOrEmpty(key))
            return 0;

        foreach (CounterEntry c in counters)
            if (c.key == key)
                return c.value;

        return 0;
    }

    /// <summary>Adds to a named counter, creating it if missing. Returns the new value.</summary>
    public int AddCounter(string key, int delta)
    {
        if (string.IsNullOrEmpty(key))
            return 0;

        foreach (CounterEntry c in counters)
        {
            if (c.key == key)
            {
                c.value += delta;
                return c.value;
            }
        }

        var entry = new CounterEntry { key = key, value = delta };
        counters.Add(entry);
        return entry.value;
    }

    // -----------------------------
    // Upgrade helpers
    // -----------------------------

    /// <summary>Returns true if the upgrade is unlocked.</summary>
    public bool HasUpgrade(string upgradeId) =>
        !string.IsNullOrEmpty(upgradeId) && unlockedUpgradeIds.Contains(upgradeId);

    /// <summary>Unlocks an upgrade (no duplicates).</summary>
    public void UnlockUpgrade(string upgradeId)
    {
        if (!string.IsNullOrEmpty(upgradeId) && !unlockedUpgradeIds.Contains(upgradeId))
            unlockedUpgradeIds.Add(upgradeId);
    }
}

/// <summary>Serializable named counter.</summary>
[Serializable]
public sealed class CounterEntry
{
    /// <summary>Counter key (see WorldState.counters conventions).</summary>
    public string key;

    /// <summary>Current value.</summary>
    public int value;
}

/// <summary>
/// Timeline variation state: attribute/nation scores and stacked active effects.
/// Score keys (Phase 2 conventions): "attr:{profileId}:{attributeId}", "nation:{nationId}".
/// </summary>
[Serializable]
public sealed class TimelineStateData
{
    /// <summary>All timeline scores by key.</summary>
    public List<ScoreEntry> scores = new();

    /// <summary>Currently active timeline effects (stackable, with expiry).</summary>
    public List<ActiveEffectEntry> activeEffects = new();

    /// <summary>Dominance keys ("profileId:attrId") currently DOMINANT (recomputed nightly).</summary>
    public List<string> dominantKeys = new();

    /// <summary>Dominance keys currently SUPPORTING (recomputed nightly).</summary>
    public List<string> supportingKeys = new();

    /// <summary>Returns the score for a key (0 if missing).</summary>
    public float GetScore(string key)
    {
        if (string.IsNullOrEmpty(key))
            return 0f;

        foreach (ScoreEntry s in scores)
            if (s.key == key)
                return s.value;

        return 0f;
    }

    /// <summary>Adds to a score, creating the entry if missing. Returns the new value.</summary>
    public float AddScore(string key, float delta)
    {
        if (string.IsNullOrEmpty(key))
            return 0f;

        foreach (ScoreEntry s in scores)
        {
            if (s.key == key)
            {
                s.value += delta;
                return s.value;
            }
        }

        var entry = new ScoreEntry { key = key, value = delta };
        scores.Add(entry);
        return entry.value;
    }
}

/// <summary>Serializable named score.</summary>
[Serializable]
public sealed class ScoreEntry
{
    /// <summary>Score key (see TimelineStateData conventions).</summary>
    public string key;

    /// <summary>Current value.</summary>
    public float value;
}

/// <summary>
/// A running effect instance produced by dominance tiers, triggers, or slot outcomes.
/// Resolved against EffectSO assets by id at load time (Phase 2).
/// </summary>
[Serializable]
public sealed class ActiveEffectEntry
{
    /// <summary>Stable id of the EffectSO asset driving this effect.</summary>
    public string effectId;

    /// <summary>Human-readable source ("Dominant: Robots (Japan-Modern)", "Trigger: ScientistSurge").</summary>
    public string sourceLabel;

    /// <summary>Day the effect started (1-based).</summary>
    public int startDay;

    /// <summary>Duration in days. -1 = permanent until removed.</summary>
    public int durationDays = -1;

    /// <summary>Returns true if the effect is still active on the given day.</summary>
    public bool IsActiveOnDay(int dayNumber) =>
        durationDays < 0 || dayNumber < startDay + durationDays;
}

/// <summary>Household state (expenses pressure). Fleshed out in Phase 4.</summary>
[Serializable]
public sealed class FamilyStateData
{
    /// <summary>Family members and their condition.</summary>
    public List<FamilyMemberData> members = new();
}

/// <summary>One family member.</summary>
[Serializable]
public sealed class FamilyMemberData
{
    /// <summary>Display name.</summary>
    public string name;

    /// <summary>0 = healthy, higher = worse. Interpreted by Phase 4 systems.</summary>
    public int condition;
}

/// <summary>
/// Deterministic "what tomorrow looks like" package, computed once at sleep
/// (nightly resolve) and stored in the save. Phase 2/3 fill and consume this.
/// </summary>
[Serializable]
public sealed class TomorrowPackage
{
    /// <summary>Lines shown in the morning briefing.</summary>
    public List<string> briefingLines = new();

    /// <summary>World news / newsletter lines generated by timeline shifts.</summary>
    public List<string> newsLines = new();
}
