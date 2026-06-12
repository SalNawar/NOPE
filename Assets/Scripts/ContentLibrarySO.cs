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

    /// <summary>Public read-only access to eras.</summary>
    public IReadOnlyList<EraSO> Eras => eras;

    /// <summary>Public read-only access to clues.</summary>
    public IReadOnlyList<ClueSO> Clues => clues;

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

    /// <summary>
    /// Clears cached lookups when the asset is loaded/reloaded.
    /// This prevents stale dictionaries after domain reloads or inspector edits.
    /// </summary>
    private void OnEnable()
    {
        _eraById = null;
        _effectByName = null;
        _upgradeById = null;
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
    /// Returns an Effect by its asset name (effect.name).
    /// If you later add a stable id f