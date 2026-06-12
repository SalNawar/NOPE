// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A nation at a specific time period — the trackable unit of timeline state
/// (Victorian England vs Industrial England are two different profiles).
/// Carries baseline attribute scores and the effects fired when an attribute
/// reaches dominant or supporting tier here.
/// </summary>
[CreateAssetMenu(fileName = "Profile_", menuName = "TimeDesk/Timeline/Nation-Era Profile", order = 22)]
public sealed class NationEraProfileSO : ScriptableObject
{
    /// <summary>Stable ID used in score keys and saves (e.g., "england_victorian").</summary>
    public string id;

    /// <summary>Display name ("Victorian England").</summary>
    public string displayName;

    /// <summary>The nation this profile belongs to.</summary>
    public NationSO nation;

    /// <summary>The time period (era) this profile covers.</summary>
    public EraSO era;

    /// <summary>Baseline attribute scores + tier effects for this profile.</summary>
    public List<AttributeBaseline> baselines = new();

    /// <summary>
    /// Returns the baseline entry for an attribute (null if not authored here).
    /// </summary>
    public AttributeBaseline GetBaseline(AttributeSO attribute)
    {
        if (attribute == null)
            return null;

        foreach (AttributeBaseline b in baselines)
            if (b != null && b.attribute == attribute)
                return b;

        return null;
    }
}

/// <summary>
/// An attribute's starting score within a profile, plus the generic effects
/// activated while that attribute is dominant or supporting here.
/// Effects are channel-tagged so ANY system (visuals, music, UI, visitor pool,
/// newsletter, tech tree...) can react — see EffectSO.
/// </summary>
[Serializable]
public sealed class AttributeBaseline
{
    /// <summary>The attribute being scored.</summary>
    public AttributeSO attribute;

    /// <summary>Starting score before player influence.</summary>
    public float baseScore;

    /// <summary>Effect active while this attribute is DOMINANT in this profile (optional).</summary>
    public EffectSO dominantEffect;

    /// <summary>Effect active while this attribute is SUPPORTING in this profile (optional).</summary>
    public EffectSO supportingEffect;
}
