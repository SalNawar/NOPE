// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A visitor archetype (warrior, scientist, artist...). Every visitor carries
/// one; its default impacts are applied to the destination automatically on
/// every send, so all decisions move the timeline even without authored cases.
/// </summary>
[CreateAssetMenu(fileName = "Archetype_", menuName = "TimeDesk/Timeline/Archetype", order = 23)]
public sealed class ArchetypeSO : ScriptableObject
{
    /// <summary>Stable ID (e.g., "scientist").</summary>
    public string id;

    /// <summary>Display name shown in UI ("Scientist").</summary>
    public string displayName;

    /// <summary>
    /// Tags used by counters and trigger conditions
    /// (e.g., "scientist", "greek-warrior"). Also targeted by VisitorTagWeight effects.
    /// </summary>
    public string[] tags;

    /// <summary>Optional visitor name pool; falls back to "Subject #N".</summary>
    public string[] namePool;

    /// <summary>Relative weight for procedural archetype selection (before effect modifiers).</summary>
    [Min(0f)]
    public float baseWeight = 1f;

    /// <summary>Timeline impacts applied on every send of this archetype.</summary>
    public List<TimelineImpact> defaultImpacts = new();
}

/// <summary>
/// One attribute movement caused by a send. Applied to the DESTINATION
/// (the era the player chose + the case's nation), with different deltas
/// for correct vs wrong routing.
/// </summary>
[Serializable]
public sealed class TimelineImpact
{
    /// <summary>Attribute moved by this impact.</summary>
    public AttributeSO attribute;

    /// <summary>Score delta when the send was CORRECT.</summary>
    public float deltaOnCorrect = 1f;

    /// <summary>Score delta when the send was WRONG (often disruptive).</summary>
    public float deltaOnWrong = -1f;

    /// <summary>If true, the same delta also moves the destination nation's global score.</summary>
    public bool alsoAffectsNationScore = true;
}
