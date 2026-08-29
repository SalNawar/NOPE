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

    [Header("Scripted identity (optional pins for forced-case anchors)")]
    /// <summary>
    /// If set (non-empty), the case's true era is picked uniformly from this
    /// pool instead of the DayPlan's era weights. A single entry pins the era
    /// exactly; multiple entries give a blueprint its own candidate set.
    /// </summary>
    [SerializeField] private EraSO[] pinnedEras;

    /// <summary>If set, overrides the destination nation derived from the era's profiles.</summary>
    [SerializeField] private NationSO pinnedNation;

    /// <summary>If set, the visitor's name (and citizen-records key) is exactly this.</summary>
    [SerializeField] private string pinnedGivenName;

    /// <summary>If set, the visitor's TRUE birth date (what the agency has on file).</summary>
    [SerializeField] private string pinnedBirthDate;

    /// <summary>If set, replaces the default "Next subject for reassignment." intro.</summary>
    [SerializeField] private string pinnedIntroLine;

    /// <summary>
    /// When true, this case ALWAYS carries exactly one forged field, defined by
    /// the category + value below — no random roll. Use for authored anchors
    /// whose lie must be provable and specific.
    /// </summary>
    [SerializeField] private bool forceForgery;

    /// <summary>Which field category gets the authored forgery.</summary>
    [SerializeField] private ClueCategory forcedForgeryCategory = ClueCategory.Currency;

    /// <summary>
    /// The forged value printed on the papers. Leave empty to derive one
    /// randomly like the procedural path does.
    /// </summary>
    [SerializeField] private string forcedForgeryValue;

    /// <summary>Public read-only archetype pool.</summary>
    public ArchetypeSO[] ArchetypePool => archetypePool;

    /// <summary>Public read-only authored impacts.</summary>
    public TimelineImpact[] AuthoredImpacts => authoredImpacts;

    /// <summary>Public read-only pinned era pool (null/empty = use DayPlan weights).</summary>
    public EraSO[] PinnedEras => pinnedEras;

    /// <summary>Public read-only pinned nation (null = derive from era profiles).</summary>
    public NationSO PinnedNation => pinnedNation;

    /// <summary>Public read-only pinned given name (null/empty = generated).</summary>
    public string PinnedGivenName => pinnedGivenName;

    /// <summary>Public read-only pinned true birth date (null/empty = generated).</summary>
    public string PinnedBirthDate => pinnedBirthDate;

    /// <summary>Public read-only pinned intro line (null/empty = default).</summary>
    public string PinnedIntroLine => pinnedIntroLine;

    /// <summary>True when this blueprint carries an authored, guaranteed forgery.</summary>
    public bool ForceForgery => forceForgery;

    /// <summary>Public read-only forced forgery category.</summary>
    public ClueCategory ForcedForgeryCategory => forcedForgeryCategory;

    /// <summary>Public read-only forced forgery value (may be empty = derive).</summary>
    public string ForcedForgeryValue => forcedForgeryValue;

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
