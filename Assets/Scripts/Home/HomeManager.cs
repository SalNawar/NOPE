using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the Home phase (Phase 4): on scene load, bills today's living
/// expenses and rolls family condition drift, then walks the player through
/// Expenses -> Shop -> Slot Machine -> Sleep. Sleep hands off to
/// RunManager.AdvanceToNextDay() (nightly resolve, day++, back to Office).
/// All HomeUIController panels are optional; unwired panels are skipped.
/// </summary>
public sealed class HomeManager : MonoBehaviour
{
    /// <summary>UI controller for the Home panels.</summary>
    [SerializeField] private HomeUIController homeUI;

    /// <summary>Active run's world state.</summary>
    private WorldState _world;

    /// <summary>Content library (upgrades, slot outcomes, effects).</summary>
    private ContentLibrarySO _lib;

    /// <summary>Gameplay tuning (expenses, slot cost).</summary>
    private GameConfigSO _config;

    /// <summary>Today's expense breakdown, kept for re-showing the panel after a Treat.</summary>
    private HomeEconomy.ExpenseReport _expenseReport;

    /// <summary>Acquires the run, bills expenses, and starts the panel flow.</summary>
    private void Start()
    {
        Debug.Log("[HomeManager] >>> Entering Start.");

        RunManager run = RunManager.GetOrCreate();

        if (run == null)
        {
            Debug.LogError("HomeManager could not acquire a RunManager (missing Resources/RunConfig).");
            return;
        }

        _world = run.World;
        _lib = run.Library;
        _config = run.Config != null ? run.Config.gameConfig : null;

        Debug.Log($"[HomeManager] Day {_world.day} home phase starting: money={_world.money}, stability={_world.timelineStability:0.#}, familyMembers={_world.family.members.Count}.");

        // Bill today's living costs and let untreated conditions drift,
        // deterministically seeded by the day so it's stable on reload.
        _expenseReport = HomeEconomy.ApplyDailyExpenses(_world, _config);
        HomeEconomy.AdvanceFamilyConditions(_world, _config, run.GetDaySeed());

        homeUI?.UpdateHud(_world);

        Debug.Log("[HomeManager] <<< Exiting Start (showing Expenses panel).");

        ShowExpenses();
    }

    /// <summary>Step 1: expense breakdown + family conditions (Treat option).</summary>
    private void ShowExpenses()
    {
        Debug.Log("[HomeManager] >>> Entering ShowExpenses.");

        if (homeUI != null && homeUI.HasExpensesPanel)
            homeUI.ShowExpenses(_world, _expenseReport, _config, HandleTreatFamilyMember, ShowShop);
        else
        {
            Debug.Log("[HomeManager] ShowExpenses: no expenses panel, skipping to Shop.");
            ShowShop();
        }
    }

    /// <summary>Treats a family member, then refreshes the expenses panel.</summary>
    private void HandleTreatFamilyMember(int memberIndex)
    {
        Debug.Log($"[HomeManager] >>> Entering HandleTreatFamilyMember (memberIndex={memberIndex}).");

        if (HomeEconomy.TreatFamilyMember(_world, _config, memberIndex))
        {
            homeUI?.UpdateHud(_world);

            // Refresh the panel in place (report numbers don't change; rows do).
            if (homeUI != null && homeUI.HasExpensesPanel)
                homeUI.ShowExpenses(_world, _expenseReport, _config, HandleTreatFamilyMember, ShowShop);

            Debug.Log($"[HomeManager] <<< Exiting HandleTreatFamilyMember (treated, money={_world.money}).");
        }
        else
        {
            Debug.Log("[HomeManager] <<< Exiting HandleTreatFamilyMember (treatment not applied).");
        }
    }

    /// <summary>Step 2: upgrade shop.</summary>
    private void ShowShop()
    {
        Debug.Log("[HomeManager] >>> Entering ShowShop.");

        homeUI?.UpdateHud(_world);

        if (homeUI != null && homeUI.HasShopPanel)
            homeUI.ShowShop(_world, _lib, HandleBuyUpgrade, ShowSlot);
        else
        {
            Debug.Log("[HomeManager] ShowShop: no shop panel, skipping to Slot.");
            ShowSlot();
        }
    }

    /// <summary>Purchases an upgrade (if affordable and not already owned), then refreshes the shop.</summary>
    private void HandleBuyUpgrade(UpgradeSO upgrade)
    {
        Debug.Log($"[HomeManager] >>> Entering HandleBuyUpgrade (upgrade='{upgrade?.displayName}').");

        if (upgrade == null || _world.HasUpgrade(upgrade.id))
        {
            Debug.Log("[HomeManager] <<< Exiting HandleBuyUpgrade — null upgrade or already owned.");
            return;
        }

        float discountPercent = _lib != null
            ? TimelineEffects.GetShopDiscountPercent(_world, _lib, upgrade.id)
            : 0f;
        int cost = Mathf.RoundToInt(upgrade.cost * (1f - discountPercent / 100f));

        if (_world.money < cost)
        {
            Debug.Log($"[HomeManager] <<< Exiting HandleBuyUpgrade — not enough money ({_world.money} < {cost}).");
            return;
        }

        _world.money -= cost;
        _world.UnlockUpgrade(upgrade.id);

        if (upgrade.unlockEffect != null)
        {
            TimelineService.ActivateEffect(
                _world, upgrade.unlockEffect, $"Upgrade: {upgrade.displayName}",
                _world.day, upgrade.unlockEffect.defaultDurationDays, applyInstantOps: true);
        }

        homeUI?.UpdateHud(_world);

        // Re-show to refresh rows (costs/owned state) without advancing the flow.
        if (homeUI != null && homeUI.HasShopPanel)
            homeUI.ShowShop(_world, _lib, HandleBuyUpgrade, ShowSlot);

        Debug.Log($"[HomeManager] <<< Exiting HandleBuyUpgrade (bought '{upgrade.displayName}' for {cost} [discount={discountPercent:0.#}%], money={_world.money}).");
    }

    /// <summary>Step 3: slot machine.</summary>
    private void ShowSlot()
    {
        Debug.Log("[HomeManager] >>> Entering ShowSlot.");

        homeUI?.UpdateHud(_world);

        if (homeUI != null && homeUI.HasSlotPanel)
            homeUI.ShowSlot(_world, _config, HandleSpin, ShowSleep);
        else
        {
            Debug.Log("[HomeManager] ShowSlot: no slot panel, skipping to Sleep.");
            ShowSleep();
        }
    }

    /// <summary>
    /// Spends the spin cost (if affordable), picks a weighted SlotOutcomeSO,
    /// and applies its money/modifier/effect results. Returns the line shown
    /// to the player.
    /// </summary>
    private string HandleSpin()
    {
        Debug.Log($"[HomeManager] >>> Entering HandleSpin (money={_world.money}).");

        int spinCost = _config != null ? _config.slotSpinCost : 0;

        if (_world.money < spinCost)
        {
            Debug.Log($"[HomeManager] <<< Exiting HandleSpin — not enough credits ({_world.money} < {spinCost}).");
            return "Not enough credits to spin.";
        }

        IReadOnlyList<SlotOutcomeSO> outcomes = _lib != null ? _lib.SlotOutcomes : System.Array.Empty<SlotOutcomeSO>();

        if (outcomes.Count == 0)
        {
            Debug.Log("[HomeManager] <<< Exiting HandleSpin — no slot outcomes configured.");
            return "The slot machine is out of order.";
        }

        _world.money -= spinCost;

        SlotOutcomeSO outcome = WeightedRandom.Pick(outcomes, o => o != null ? o.weight : 0f);

        if (outcome == null)
        {
            homeUI?.UpdateHud(_world);
            Debug.Log("[HomeManager] <<< Exiting HandleSpin — no outcome picked (nothing happens).");
            return "...nothing happens.";
        }

        _world.money += outcome.moneyDelta;
        _world.legendaryChanceBonus += outcome.legendaryChanceBonus;
        _world.forgeryChanceModifier += outcome.forgeryChanceModifier;
        _world.payRateMultiplier += outcome.payRateMultiplierDelta;

        if (outcome.effect != null)
        {
            int duration = outcome.durationDaysOverride != 0
                ? outcome.durationDaysOverride
                : outcome.effect.defaultDurationDays;

            TimelineService.ActivateEffect(
                _world, outcome.effect, $"Slot: {outcome.displayName}",
                _world.day, duration, applyInstantOps: true);
        }

        homeUI?.UpdateHud(_world);

        string line = !string.IsNullOrEmpty(outcome.resultLine) ? outcome.resultLine : outcome.displayName;

        Debug.Log($"[HomeManager] <<< Exiting HandleSpin (outcome='{outcome.displayName}', spinCost={spinCost}, moneyDelta={outcome.moneyDelta}, money={_world.money}).");

        return outcome.moneyDelta != 0
            ? $"{line} ({outcome.moneyDelta:+0;-0} credits)"
            : line;
    }

    /// <summary>Step 4: sleep prompt.</summary>
    private void ShowSleep()
    {
        Debug.Log("[HomeManager] >>> Entering ShowSleep.");

        homeUI?.UpdateHud(_world);

        if (homeUI != null && homeUI.HasSleepPanel)
            homeUI.ShowSleep(_world, HandleSleep);
        else
        {
            Debug.Log("[HomeManager] ShowSleep: no sleep panel, sleeping immediately.");
            HandleSleep();
        }
    }

    /// <summary>
    /// Ends the day. Checks for a game-over ending (bankruptcy, score/day
    /// thresholds) before the nightly resolve; if one matches, records it
    /// and hands off to the title scene instead of advancing to tomorrow.
    /// Otherwise: nightly resolve, day++, save, back to Office.
    /// </summary>
    private void HandleSleep()
    {
        Debug.Log($"[HomeManager] >>> Entering HandleSleep (day {_world?.day}, money={_world?.money}, stability={_world?.timelineStability:0.#}).");

        if (!RunManager.HasInstance)
        {
            Debug.LogError("HomeManager.HandleSleep called with no RunManager instance.");
            return;
        }

        EndingSO ending = EndingService.Evaluate(_world, _lib, _config);

        if (ending != null)
        {
            Debug.Log($"[HomeManager] HandleSleep: ending matched '{ending.id}' ({ending.displayName}).");

            _world.endingId = ending.id;
            RunManager.Instance.SaveNow();

            Debug.Log("[HomeManager] <<< Exiting HandleSleep (loading title scene for ending).");

            RunManager.Instance.LoadTitleScene();
            return;
        }

        Debug.Log("[HomeManager] <<< Exiting HandleSleep (no ending, advancing to next day).");

        RunManager.Instance.AdvanceToNextDay();
    }
}
