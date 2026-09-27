// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Defines how a procedural case is assembled: the traveller's kind (one
/// blueprint per kind, traveller types K1), which document templates they
/// carry, how often they lie, and which archetypes they draw from. How often
/// a kind comes is the day's (DayPlanSO kinds).
/// </summary>
[CreateAssetMenu(fileName = "CaseBlueprint_", menuName = "TimeDesk/Case Blueprint", order = 4)]
public sealed class CaseBlueprintSO : ScriptableObject
{
    /// <summary>The kind of traveller this blueprint makes (their papers, their claim line, what the desk may ask them for).</summary>
    [Header("Kind")]
    [SerializeField] private TravellerKind kind = TravellerKind.Displaced;

    /// <summary>Document templates included in this case.</summary>
    [Header("Documents")]
    [SerializeField] private DocumentTemplateSO[] documentTemplates;

    /// <summary>Chance per traveller to be a liar (plus the WorldState and effect modifiers).</summary>
    [Header("Lie")]
    [SerializeField, Range(0f, 1f)] private float contradictionChance = 0.25f;

    /// <summary>Optional archetype pool for this blueprint (empty = pick from library).</summary>
    [Header("Timeline")]
    [SerializeField] private ArchetypeSO[] archetypePool;

    /// <summary>Authored timeline impacts added to every case from this blueprint.</summary>
    [SerializeField] private TimelineImpact[] authoredImpacts;

    /// <summary>Public read-only archetype pool.</summary>
    public ArchetypeSO[] ArchetypePool => archetypePool;

    /// <summary>Public read-only authored impacts.</summary>
    public TimelineImpact[] AuthoredImpacts => authoredImpacts;

    /// <summary>The kind of traveller this blueprint makes.</summary>
    public TravellerKind Kind => kind;

    /// <summary>Public read-only templates.</summary>
    public DocumentTemplateSO[] DocumentTemplates => documentTemplates;

    /// <summary>Public read-only contradiction chance.</summary>
    public float ContradictionChance => contradictionChance;
}
