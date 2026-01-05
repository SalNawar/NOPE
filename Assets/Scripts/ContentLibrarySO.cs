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

    /// <summary>Public read-only access to eras.</summary>
    public IReadOnlyList<EraSO> Eras => eras;

    /// <summary>Public read-only access to clues.</summary>
    public IReadOnlyList<ClueSO> Clues => clues;

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
    }
}
