// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central content registry (data only).
/// You create one ContentLibrary asset and assign all your DayPlans, Eras, Clues, etc.
/// Runtime systems query this instead of hardcoding references.
/// </summary>
[CreateAssetMenu(fileName = "ContentLibrary_", menuName = "TimeDesk/Content Library", order = 0)]
public sealed class ContentLibrarySO : ScriptableObject
{
    /// <summary>All day plans (D1..D7..etc).</summary>
    [SerializeField] private DayPlanSO[] dayPlans;

    /// <summary>All eras available in the game.</summary>
    [SerializeField] private EraSO[] eras;

    /// <summary>All clues that can appear in documents.</summary>
    [SerializeField] private ClueSO[] clues;

    /// <summary>All legendary characters available in the game.</summary>
    [SerializeField] private LegendarySO[] legendaries;

    /// <summary>Optional: effects list for lookups/UI.</summary>
    [SerializeField] private EffectSO[] effects;

    /// <summary>Optional: upgrades list for lookups.</summary>
    [SerializeField] private UpgradeSO[] upgrades;

    [Header("Timeline Variation")]
    /// <summary>All key attributes (cultures/traits) tracked by the timeline.</summary>
    [SerializeField] private AttributeSO[] attributes;

    /// <summary>All nations.</summary>
    [SerializeField] private NationSO[] nations;

    /// <summary>All nation-era profiles (nation at a time period).</summary>
    [SerializeField] private NationEraProfileSO[] nationEraProfiles;

    /// <summary>All visitor archetypes.</summary>
    [SerializeField] private ArchetypeSO[] archetypes;

    /// <summary>All timeline triggers (special conditions).</summary>
    [SerializeField] private TimelineTriggerSO[] timelineTriggers;

    [Header("Home (Phase 4)")]
    /// <summary>All slot machine outcomes available at Home.</summary>
    [SerializeField] private SlotOutcomeSO[] slotOutcomes;

    [Header("Endings (Phase 5)")]
    /// <summary>All possible run endings (fired, bankrupt, score/day thresholds).</summary>
    [SerializeField] private EndingSO[] endings;

    [Header("Investigation")]
    /// <summary>Reference books the player consults (one per clue category).</summary>
    [SerializeField] private ReferenceBookSO[] referenceBooks;

    /// <summary>Public read-only access to reference books.</summary>
    public IReadOnlyList<ReferenceBookSO> ReferenceBooks => referenceBooks ?? System.Array.Empty<ReferenceBookSO>();

    /// <summary>Returns the first reference book for a category, or null.</summary>
    public ReferenceBookSO GetReferenceBook(ClueCategory category)
    {
        if (referenceBooks == null)
            return null;

        foreach (ReferenceBookSO book in referenceBooks)
            if (book != null && book.category == category)
                return book;

        return null;
    }

    /// <summary>Public read-only access to day plans.</summary>
    public IReadOnlyList<DayPlanSO> DayPlans => dayPlans ?? System.Array.Empty<DayPlanSO>();

    /// <summary>Public read-only access to eras.</summary>
    public IReadOnlyList<EraSO> Eras => eras;

    /// <summary>Public read-only access to clues.</summary>
    public IReadOnlyList<ClueSO> Clues => clues;

    /// <summary>Public read-only access to legendaries.</summary>
    public IReadOnlyList<LegendarySO> Legendaries => legendaries ?? System.Array.Empty<LegendarySO>();

    /// <summary>Public read-only access to effects.</summary>
    public IReadOnlyList<EffectSO> Effects => effects ?? System.Array.Empty<EffectSO>();

    /// <summary>Public read-only access to upgrades (Home shop).</summary>
    public IReadOnlyList<UpgradeSO> Upgrades => upgrades ?? System.Array.Empty<UpgradeSO>();

    /// <summary>Public read-only access to slot machine outcomes (Home).</summary>
    public IReadOnlyList<SlotOutcomeSO> SlotOutcomes => slotOutcomes ?? System.Array.Empty<SlotOutcomeSO>();

    /// <summary>Public read-only access to endings.</summary>
    public IReadOnlyList<EndingSO> Endings => endings ?? System.Array.Empty<EndingSO>();

    /// <summary>Public read-only access to attributes.</summary>
    public IReadOnlyList<AttributeSO> Attributes => attributes ?? System.Array.Empty<AttributeSO>();

    /// <summary>Public read-only access to nations.</summary>
    public IReadOnlyList<NationSO> Nations => nations ?? System.Array.Empty<NationSO>();

    /// <summary>Public read-only access to nation-era profiles.</summary>
    public IReadOnlyList<NationEraProfileSO> Profiles => nationEraProfiles ?? System.Array.Empty<NationEraProfileSO>();

    /// <summary>Public read-only access to archetypes.</summary>
    public IReadOnlyList<ArchetypeSO> Archetypes => archetypes ?? System.Array.Empty<ArchetypeSO>();

    /// <summary>Public read-only access to timeline triggers.</summary>
    public IReadOnlyList<TimelineTriggerSO> Triggers => timelineTriggers ?? System.Array.Empty<TimelineTriggerSO>();

    /// <summary>
    /// Finds the authored profile for a nation at an era (null if none authored).
    /// </summary>
    public NationEraProfileSO GetProfile(NationSO nation, EraSO era)
    {
        if (nation == null || era == null || nationEraProfiles == null)
            return null;

        foreach (NationEraProfileSO p in nationEraProfiles)
            if (p != null && p.nation == nation && p.era == era)
                return p;

        return null;
    }

    /// <summary>Cached lookup: era id -> era asset.</summary>
    private Dictionary<string, EraSO> _eraById;

    /// <summary>Cached lookup: effect asset name -> effect asset.</summary>
    private Dictionary<string, EffectSO> _effectByName;

    /// <summary>Cached lookup: upgrade id -> upgrade asset.</summary>
    private Dictionary<string, UpgradeSO> _upgradeById;

    /// <summary>Cached lookup: ending id -> ending asset.</summary>
    private Dictionary<string, EndingSO> _endingById;

    /// <summary>
    /// Clears cached lookups when the asset is loaded/reloaded.
    /// This prevents stale dictionaries after domain reloads or inspector edits.
    /// </summary>
    private void OnEnable()
    {
        _eraById = null;
        _effectByName = null;
        _upgradeById = null;
        _endingById = null;
    }

    /// <summary>
    /// Returns an Era by its string ID (e.g., "rome").
    /// Useful for saves or ID-based references.
    /// </summary>
    public EraSO GetEraById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        EnsureLookups();
        return _eraById.TryGetValue(id, out EraSO era) ? era : null;
    }

    /// <summary>
    /// Returns an Upgrade by its string ID (e.g., "scanner").
    /// Used when loading save data that stores upgrade IDs.
    /// </summary>
    public UpgradeSO GetUpgradeById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        EnsureLookups();
        return _upgradeById.TryGetValue(id, out UpgradeSO upgrade) ? upgrade : null;
    }

    /// <summary>
    /// Returns an Ending by its string ID (e.g., "fired", "bankrupt").
    /// Used by the title scene to display WorldState.endingId.
    /// </summary>
    public EndingSO GetEndingById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        EnsureLookups();
        return _endingById.TryGetValue(id, out EndingSO ending) ? ending : null;
    }

    /// <summary>
    /// Returns an Effect by its asset name (effect.name).
    /// If you later add a stable id field to EffectSO, switch to that.
    /// </summary>
    public EffectSO GetEffectByAssetName(string assetName)
    {
        if (string.IsNullOrWhiteSpace(assetName))
            return null;

        EnsureLookups();
        return _effectByName.TryGetValue(assetName, out EffectSO effect) ? effect : null;
    }

    /// <summary>
    /// Finds the DayPlan matching a given dayNumber.
    /// Keeps day progression data-driven (D1..D7...).
    /// </summary>
    public DayPlanSO GetDayPlan(int dayNumber)
    {
        if (dayPlans == null)
            return null;

        foreach (DayPlanSO plan in dayPlans)
        {
            if (plan != null && plan.DayNumber == dayNumber)
                return plan;
        }

        return null;
    }

    /// <summary>
    /// Builds internal dictionaries the first time they are needed.
    /// </summary>
    private void EnsureLookups()
    {
        if (_eraById != null)
            return;

        _eraById = new Dictionary<string, EraSO>(StringComparer.OrdinalIgnoreCase);
        _effectByName = new Dictionary<string, EffectSO>(StringComparer.OrdinalIgnoreCase);
        _upgradeById = new Dictionary<string, UpgradeSO>(StringComparer.OrdinalIgnoreCase);
        _endingById = new Dictionary<string, EndingSO>(StringComparer.OrdinalIgnoreCase);

        if (eras != null)
        {
            foreach (EraSO e in eras)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.id))
                    continue;

                _eraById.TryAdd(e.id, e);
            }
        }

        if (effects != null)
        {
            foreach (EffectSO fx in effects)
            {
                if (fx == null)
                    continue;

                _effectByName.TryAdd(fx.name, fx);
            }
        }

        if (upgrades != null)
        {
            foreach (UpgradeSO u in upgrades)
            {
                if (u == null || string.IsNullOrWhiteSpace(u.id))
                    continue;

                _upgradeById.TryAdd(u.id, u);
            }
        }

        if (endings != null)
        {
            foreach (EndingSO e in endings)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.id))
                    continue;

                _endingById.TryAdd(e.id, e);
            }
        }
    }
}
