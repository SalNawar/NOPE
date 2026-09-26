// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Defines how a procedural case is assembled: which document templates the
/// traveller carries, how often they lie, and which archetypes they draw from.
/// </summary>
[CreateAssetMenu(fileName = "CaseBlueprint_", menuName = "TimeDesk/Case Blueprint", order = 4)]
public sealed class CaseBlueprintSO : ScriptableObject
{
    /// <summary>Used as a selection weight / difficulty marker (designer-controlled).</summary>
    [Header("Difficulty")]
    [SerializeField, Range(1, 10)] private int difficulty = 1;

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

    /// <summary>Public read-only difficulty.</summary>
    public int Difficulty => difficulty;

    /// <summary>Public read-only templates.</summary>
    public DocumentTemplateSO[] DocumentTemplates => documentTemplates;

    /// <summary>Public read-only contradiction chance.</summary>
    public float ContradictionChance => contradictionChance;
}
