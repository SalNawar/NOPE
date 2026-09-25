// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A place: a nation at a specific time period ("Abbasid Baghdad", Medieval).
/// It is the unit of the world model (its facts, names and birth years feed
/// case generation and the reference books through ContentLibrarySO.BuildFactTable),
/// of how its travellers look (wardrobe and look weights) and of timeline
/// state (baseline attribute scores and tier effects).
/// </summary>
[CreateAssetMenu(fileName = "Profile_", menuName = "TimeDesk/Timeline/Nation-Era Profile", order = 22)]
public sealed class NationEraProfileSO : ScriptableObject
{
    /// <summary>Stable ID used in score keys and saves ("{nation}_{era}", e.g. "iraq_medieval").</summary>
    public string id;

    /// <summary>Display name of the place at that moment ("Abbasid Baghdad").</summary>
    public string displayName;

    /// <summary>The nation this profile belongs to.</summary>
    public NationSO nation;

    /// <summary>The time period (era) this profile covers.</summary>
    public EraSO era;

    [Header("Place (world model)")]
    /// <summary>The place's moment year (negative = BCE); travellers' ages (their face) are measured against it.</summary>
    public int year;

    /// <summary>Earliest birth year of a traveller from here (negative = BCE).</summary>
    public int birthYearMin;

    /// <summary>Latest birth year of a traveller from here (negative = BCE).</summary>
    public int birthYearMax;

    /// <summary>World facts of this place, one per category (Currency, Language, Technology, Geography = capital, Politics = ruler).</summary>
    public List<ProfileFact> facts = new();

    /// <summary>Period-appropriate male given names.</summary>
    public string[] maleNames;

    /// <summary>Period-appropriate female given names.</summary>
    public string[] femaleNames;

    /// <summary>Small-talk lines of travellers claiming this place (flavour, never evidence).</summary>
    public List<LineText> smallTalk = new();

    [Header("Look")]
    /// <summary>What people of this place wear, per gender, and each gender's signature item (the Costume Guide entry and the only item a disguise can leak).</summary>
    public PlaceWardrobe wardrobe = new();

    /// <summary>Skin-tone and hair-colour weights of travellers claiming this place (never a tell).</summary>
    public LookWeights looks = new();

    [Header("Timeline")]
    /// <summary>Baseline attribute scores + tier effects for this profile.</summary>
    public List<AttributeBaseline> baselines = new();

    /// <summary>Label used in books, claims and Citizen Records: "Abbasid Baghdad (Medieval)".</summary>
    public string OriginLabel => OriginLabels.Format(displayName, era != null ? era.displayName : null);

    /// <summary>Every given name of this place (male then female).</summary>
    public IReadOnlyList<string> AllNames
    {
        get
        {
            var names = new List<string>();
            if (maleNames != null) names.AddRange(maleNames);
            if (femaleNames != null) names.AddRange(femaleNames);
            return names;
        }
    }

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

/// <summary>One world fact of a place: in this category, the value is X ("Currency" = "Deben").</summary>
[Serializable]
public sealed class ProfileFact
{
    /// <summary>Which fact (matches DocumentField and reference-book categories).</summary>
    public ClueCategory category;

    /// <summary>The fact's value as printed on papers and in books.</summary>
    public string value;
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
