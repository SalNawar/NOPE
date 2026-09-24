using System.Collections.Generic;

/// <summary>
/// Condition type for gates (timeline triggers, interview questions, dialogs).
/// Serialized as ints: append only.
/// </summary>
public enum TriggerConditionType
{
    /// <summary>WorldState counter >= threshold. key = counter key (e.g., "sent:tag:greek-warrior:greece480").</summary>
    CounterAtLeast,

    /// <summary>Flag is set. key = flag name.</summary>
    FlagSet,

    /// <summary>Flag is NOT set. key = flag name.</summary>
    FlagNotSet,

    /// <summary>Attribute score in a profile >= threshold (baseline + accumulated).</summary>
    AttributeScoreAtLeast,

    /// <summary>Attribute score in a profile <= threshold.</summary>
    AttributeScoreAtMost,

    /// <summary>Attribute is currently DOMINANT in the profile.</summary>
    AttributeIsDominant,

    /// <summary>Attribute is currently SUPPORTING in the profile.</summary>
    AttributeIsSupporting,

    /// <summary>Nation global score >= threshold.</summary>
    NationScoreAtLeast,

    /// <summary>
    /// Current day >= threshold, read from the snapshot's day. The nightly
    /// resolve snapshots before day++ (the day just played), so a trigger
    /// gated "DayAtLeast N" fires the night of day N and its effects start on
    /// day N+1; the office's day-start snapshot holds the day being played, so
    /// a question gated "DayAtLeast N" is askable from day N
    /// (Gates.UnlockNight converts one reading into the other).
    /// </summary>
    DayAtLeast,

    /// <summary>Timeline stability <= threshold.</summary>
    StabilityAtMost,

    /// <summary>The upgrade is owned (WorldState.HasUpgrade). key = UpgradeSO.id; never the 'upgrade:x' flag an unlock effect may set.</summary>
    UpgradeOwned
}

/// <summary>One gate condition as the rules read it: its type and the plain key and threshold it compares.</summary>
public readonly struct GateCondition
{
    /// <summary>What the condition tests.</summary>
    public readonly TriggerConditionType Type;

    /// <summary>The counter, flag, upgrade, score or dominance key; null means an unresolved reference.</summary>
    public readonly string Key;

    /// <summary>Numeric threshold (counters, scores, day, stability).</summary>
    public readonly float Threshold;

    /// <summary>Creates a condition.</summary>
    public GateCondition(TriggerConditionType type, string key, float threshold)
    {
        Type = type;
        Key = key;
        Threshold = threshold;
    }
}

/// <summary>
/// A frozen copy of what gates read: day, stability, flags, owned upgrades,
/// counters, scores and dominance tiers. The constructor copies every
/// collection, so later changes to the world never change an evaluation. A
/// null or blank argument reads false or 0, and a missing counter or score
/// reads 0 (like WorldState.HasFlag/GetCounter and TimelineStateData.GetScore).
/// </summary>
public sealed class GateSnapshot
{
    /// <summary>Set flags.</summary>
    private readonly HashSet<string> _flags;

    /// <summary>Owned upgrade ids.</summary>
    private readonly HashSet<string> _upgrades;

    /// <summary>Dominant tier keys ("profileId:attrId").</summary>
    private readonly HashSet<string> _dominant;

    /// <summary>Supporting tier keys ("profileId:attrId").</summary>
    private readonly HashSet<string> _supporting;

    /// <summary>Counter values by key (the first entry of a key wins, like WorldState.GetCounter).</summary>
    private readonly Dictionary<string, int> _counters = new Dictionary<string, int>();

    /// <summary>Score values by key (the first entry of a key wins, like TimelineStateData.GetScore).</summary>
    private readonly Dictionary<string, float> _scores = new Dictionary<string, float>();

    /// <summary>Copies the given state. Null collections count as empty.</summary>
    public GateSnapshot(int day, float stability,
                        IEnumerable<string> flags, IEnumerable<string> upgradeIds,
                        IEnumerable<KeyValuePair<string, int>> counters,
                        IEnumerable<KeyValuePair<string, float>> scores,
                        IEnumerable<string> dominantKeys, IEnumerable<string> supportingKeys)
    {
        Day = day;
        Stability = stability;
        _flags = Copy(flags);
        _upgrades = Copy(upgradeIds);
        _dominant = Copy(dominantKeys);
        _supporting = Copy(supportingKeys);

        if (counters != null)
            foreach (KeyValuePair<string, int> c in counters)
                if (!string.IsNullOrEmpty(c.Key) && !_counters.ContainsKey(c.Key))
                    _counters.Add(c.Key, c.Value);

        if (scores != null)
            foreach (KeyValuePair<string, float> s in scores)
                if (!string.IsNullOrEmpty(s.Key) && !_scores.ContainsKey(s.Key))
                    _scores.Add(s.Key, s.Value);
    }

    /// <summary>The day the snapshot was taken on.</summary>
    public int Day { get; }

    /// <summary>Timeline stability (0..100).</summary>
    public float Stability { get; }

    /// <summary>True when the flag is set.</summary>
    public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && _flags.Contains(flag);

    /// <summary>True when the upgrade is owned.</summary>
    public bool HasUpgrade(string upgradeId) => !string.IsNullOrEmpty(upgradeId) && _upgrades.Contains(upgradeId);

    /// <summary>A counter's value (0 when missing).</summary>
    public int Counter(string key) => !string.IsNullOrEmpty(key) && _counters.TryGetValue(key, out int v) ? v : 0;

    /// <summary>A score's value (0 when missing).</summary>
    public float Score(string key) => !string.IsNullOrEmpty(key) && _scores.TryGetValue(key, out float v) ? v : 0f;

    /// <summary>True when the tier key is currently DOMINANT.</summary>
    public bool IsDominant(string key) => !string.IsNullOrEmpty(key) && _dominant.Contains(key);

    /// <summary>True when the tier key is currently SUPPORTING.</summary>
    public bool IsSupporting(string key) => !string.IsNullOrEmpty(key) && _supporting.Contains(key);

    /// <summary>A set of the non-blank items (empty for null).</summary>
    private static HashSet<string> Copy(IEnumerable<string> items)
    {
        var set = new HashSet<string>();
        if (items != null)
            foreach (string item in items)
                if (!string.IsNullOrEmpty(item))
                    set.Add(item);
        return set;
    }
}

/// <summary>
/// Evaluates gate conditions over a snapshot. Timeline triggers (at the
/// nightly resolve) and the office's day-start interview (questions,
/// dialogs) share this one evaluator, so every rule is tested headless.
/// </summary>
public static class Gates
{
    /// <summary>Whether one condition passes on the snapshot (a null snapshot passes nothing; an unknown type never passes).</summary>
    public static bool Passes(GateCondition c, GateSnapshot s)
    {
        if (s == null)
            return false;

        switch (c.Type)
        {
            case TriggerConditionType.CounterAtLeast: return s.Counter(c.Key) >= c.Threshold;
            case TriggerConditionType.FlagSet: return s.HasFlag(c.Key);
            case TriggerConditionType.FlagNotSet: return !s.HasFlag(c.Key);
            case TriggerConditionType.AttributeScoreAtLeast: return c.Key != null && s.Score(c.Key) >= c.Threshold;
            case TriggerConditionType.AttributeScoreAtMost: return c.Key != null && s.Score(c.Key) <= c.Threshold;
            case TriggerConditionType.AttributeIsDominant: return c.Key != null && s.IsDominant(c.Key);
            case TriggerConditionType.AttributeIsSupporting: return c.Key != null && s.IsSupporting(c.Key);
            case TriggerConditionType.NationScoreAtLeast: return c.Key != null && s.Score(c.Key) >= c.Threshold;
            case TriggerConditionType.DayAtLeast: return s.Day >= c.Threshold;
            case TriggerConditionType.StabilityAtMost: return s.Stability <= c.Threshold;
            case TriggerConditionType.UpgradeOwned: return s.HasUpgrade(c.Key);
            default: return false;
        }
    }

    /// <summary>True when every condition passes (a null or empty list passes); false as soon as one fails.</summary>
    public static bool AllPass(IEnumerable<GateCondition> conditions, GateSnapshot s)
    {
        if (conditions == null)
            return true;

        foreach (GateCondition c in conditions)
            if (!Passes(c, s))
                return false;

        return true;
    }

    /// <summary>True when every condition is a DayAtLeast gate (a null or empty list counts): only such questions carry spoken tells.</summary>
    public static bool DayOnly(IEnumerable<GateCondition> conditions)
    {
        if (conditions == null)
            return true;

        foreach (GateCondition c in conditions)
            if (c.Type != TriggerConditionType.DayAtLeast)
                return false;

        return true;
    }

    /// <summary>
    /// The nightly-resolve day on which to announce something askable from
    /// <paramref name="fromDay"/>: the night before, since the resolve reads
    /// the day just played (see TriggerConditionType.DayAtLeast).
    /// </summary>
    public static int UnlockNight(int fromDay) => fromDay - 1;
}

/// <summary>Pairs content with its projected gate, so availability is decided in Domain.</summary>
public readonly struct Gated<T>
{
    /// <summary>The gated content (a question, a dialog).</summary>
    public readonly T Item;

    /// <summary>Its conditions (null counts as none).</summary>
    public readonly IReadOnlyList<GateCondition> Conditions;

    /// <summary>Creates a gated item.</summary>
    public Gated(T item, IReadOnlyList<GateCondition> conditions)
    {
        Item = item;
        Conditions = conditions;
    }
}

/// <summary>Flag names the run writes into WorldState.flags; one home for the format.</summary>
public static class FlagKeys
{
    /// <summary>Set after a one-shot timeline trigger fires ("trig:{id}:fired", the format saves already hold).</summary>
    public static string TriggerFired(string triggerId) => $"trig:{triggerId}:fired";

    /// <summary>Set at the end of the shift that completed a one-shot narrative dialog ("dlg:{id}:done").</summary>
    public static string DialogDone(string dialogId) => $"dlg:{dialogId}:done";
}
