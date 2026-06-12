// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Condition type for timeline triggers. All conditions on a trigger must pass.
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

    /// <summary>Current day >= threshold.</summary>
    DayAtLeast,

    /// <summary>Timeline stability <= threshold.</summary>
    StabilityAtMost
}

/// <summary>
/// A special condition of the timeline ("5+ greek warriors sent to the battle era",
/// "Tesla delivered to Germany") evaluated during the nightly resolve. When all
/// conditions pass, its outcome effects activate — e.g., "+scientist visitors for
/// 3 days" or "-30% on electric tech upgrades for 2 days".
/// </summary>
[CreateAssetMenu(fileName = "Trigger_", menuName = "TimeDesk/Timeline/Trigger", order = 24)]
public sealed class TimelineTriggerSO : ScriptableObject
{
    /// <summary>Stable ID used for the fired-flag ("trig:{id}:fired").</summary>
    public string id;

    /// <summary>Designer-facing name ("Battle of Thermopylae Turned").</summary>
    public string displayName;

    /// <summary>Notes / flavor; also usable as a news line source.</summary>
    [TextArea]
    public string description;

    /// <summary>If true, fires once per run (guarded by the fired-flag).</summary>
    public bool oneShot = true;

    /// <summary>News line added to tomorrow's briefing when this fires (optional).</summary>
    public string newsLineOnFire;

    /// <summary>All conditions must pass for the trigger to fire.</summary>
    public List<TriggerCondition> conditions = new();

    /// <summary>Effects activated when the trigger fires.</summary>
    public List<TriggerOutcome> outcomes = new();

    /// <summary>Flag set after a one-shot trigger fires.</summary>
    public string FiredFlag => $"trig:{id}:fired";
}

/// <summary>One condition; unused fields are ignored per type.</summary>
[Serializable]
public sealed class TriggerCondition
{
    /// <summary>Condition type (see TriggerConditionType for field meanings).</summary>
    public TriggerConditionType type;

    /// <summary>Counter key or flag name.</summary>
    public string key;

    /// <summary>Numeric threshold.</summary>
    public float threshold;

    /// <summary>Target attribute (attribute-based conditions).</summary>
    public AttributeSO attribute;

    /// <summary>Target profile (attribute-based conditions).</summary>
    public NationEraProfileSO profile;

    /// <summary>Target nation (NationScoreAtLeast).</summary>
    public NationSO nation;
}

/// <summary>An effect activation produced by a fired trigger.</summary>
[Serializable]
public sealed class TriggerOutcome
{
    /// <summary>Effect to activate.</summary>
    public EffectSO effect;

    /// <summary>Duration override in days; 0 = use the effect's defaultDurationDays.</summary>
    public int durationDaysOverride;
}
