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
