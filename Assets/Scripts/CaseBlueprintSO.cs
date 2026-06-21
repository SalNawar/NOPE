// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// Defines how a procedural case is assembled:
/// - which document templates are included
/// - how many clues to inject
/// - how often contradictions/red herrings appear
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

    /// <summary>Minimum number of clue lines to inject into the case.</summary>
    [Header("Clue Targets")]
    [SerializeField, Min(0)] private int totalCluesMin = 2;

    /// <summary>Maximum number of clue lines to inject into the case.</summary>
    [SerializeField, Min(0)] private int totalCluesMax = 4;

    /// <summary>Chance per clue line to be a contradiction (lie/anachronism).</summary>
    [Header("Lie / Misdirection")]
    [SerializeField, Range(0f, 1f)] private float contradictionChance = 0.25f;

    /// <summary>Chance per clue line to be a red herring (plausible but irrelevant).</summary>
    [SerializeField, Range(0f, 1f)] private float redHerringChance = 0.10f;

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

    /// <summary>Public read-only min clues.</summary>
    public int TotalCluesMin => totalCluesMin;

    /// <summary>Public read-only max clues.</summary>
    public int TotalCluesMax => totalCluesMax;

    /// <summary>Public read-only contradiction chance.</summary>
    public float ContradictionChance => contradictionChance;

    /// <summary>Public read-only red herring chance.</summary>
    public float RedHerringChance => redHerringChance;

    /// <summary>
    /// Ensures min/max are sensible at edit-time.
    /// </summary>
    private void OnValidate()
    {
        if (totalCluesMax < totalCluesMin)
            totalCluesMax = totalCluesMin;
    }
}
