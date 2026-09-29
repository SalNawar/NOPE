// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// A premade character: a named traveller drawn whole (four expressions),
/// generated from world_source.json "premades" by Tools > TimeDesk > Generate
/// World into Assets/Data/World/Premades. The claim is <see cref="nation"/> +
/// <see cref="trueEra"/>; a premade is honest unless <see cref="truePlace"/>
/// is set. Day plans schedule premades (forced slots and a random pool).
/// </summary>
public sealed class LegendarySO : ScriptableObject
{
    /// <summary>Stable id (a LookKeys token): art keys (premade_{id}_{expression}) and the met flag (FlagKeys.PremadeMet).</summary>
    public string id;

    /// <summary>The name on papers, in Citizen Records and on the banner.</summary>
    public string displayName = "Legendary Visitor";

    /// <summary>The registered birth date ("14 Mar 1505 BCE"), inside the claimed place's birth years.</summary>
    public string birthDate;

    /// <summary>The premade's gender (authored).</summary>
    public TravellerGender gender;

    /// <summary>The era the premade claims.</summary>
    public EraSO trueEra;

    [Header("Timeline")]
    /// <summary>Archetype for this premade (tags + default impacts; the role on the banner).</summary>
    public ArchetypeSO archetype;

    /// <summary>The nation the premade claims (timeline impacts land here).</summary>
    public NationSO nation;

    /// <summary>Authored timeline impacts (added on top of archetype defaults).</summary>
    public System.Collections.Generic.List<TimelineImpact> authoredImpacts = new();

    [Header("Premade")]
    /// <summary>Null = honest; else an authored liar who really comes from this place (tells on papers or answers, never dress).</summary>
    public NationEraProfileSO truePlace;

    /// <summary>The desk's opener for this premade; blank = the interview's legendary opener.</summary>
    public string introLine;

    /// <summary>The Citizen Records note; blank = the sealed-records note.</summary>
    public string recordNote;

    /// <summary>A narrative dialog offered only while this premade is at the desk (blank = none).</summary>
    public string dialogId;

    /// <summary>Met once, never again this run (written by Generate World as !repeatable).</summary>
    public bool oncePerRun = true;

    /// <summary>The kind the premade stands as (premades[].kind; days 7-15 B2): Displaced for the famous (blank in the source), a 2150 citizen kind for a story character (Premades.IsFamous).</summary>
    public TravellerKind kind = TravellerKind.Displaced;

    [Header("Story character (a 2150 citizen)")]
    /// <summary>A story character's family country (premades[].family; null: the claimed place's nation): their lineage and, until their art lands, the looks their generated stand-in is drawn with.</summary>
    public NationSO family;

    /// <summary>A story character's Citizen ID (premades[].citizenId; blank: drawn), reserved before slot 1, the same at every appearance.</summary>
    public string citizenId;

    /// <summary>A story character's debt in cr (premades[].debt; 0: drawn from its status's range).</summary>
    public int debt;

    /// <summary>A labourer story character's employer (premades[].employer, an agency.employers id; blank: drawn among its worksite era's).</summary>
    public string employer;

    /// <summary>A famous traveller's pull on the world when accepted (premades[].pulls; the endings spec §4.1): stands instead of their role's pull and is not scaled by their kind (WorldPulls.ForAccept); none: the role's pull.</summary>
    public System.Collections.Generic.List<OutcomePull> pulls = new();
}
