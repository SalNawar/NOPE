using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the Home phase (Phase 4): on scene load, the night's break-in, the
/// bill and the family's night (DayCycle.OpenHome), then walks the player
/// through Expenses -> House -> Slot Machine -> Sleep. The House shows only
/// the Home upgrades (UpgradeSO.venue Home: the house upgrades' tree; the
/// office's moved to the PC's Orders app, Saleh 2026-09-29) and is skipped
/// while none is stocked; with the radio owned the sleep panel plays the
/// night's radio line (ContentLibrarySO.Home). Sleep hands off to RunManager.Sleep() (day-boundary endings,
/// else nightly resolve, day++ and the Orders app's deliveries, back to Office).
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

    /// <summary>Tonight's evening (the bill, the break-in, the family's night, the mood), kept for re-showing the panel after a Treat.</summary>
    private HomeEconomy.Evening _evening;

    /// <summary>Tonight's slot spins, drawn in turn from the run's own stream for the day (Seeds.ForSlot): a run replays, and Continue (Home again from the save made before it) cannot reroll a spin.</summary>
    private IRandomSource _slotRandom;

    /// <summary>Tonight's household costs so far (the expenses and any care), for the clerk's statement.</summary>
    private int _household;

    /// <summary>Tonight's purchases at Home so far (Home upgrades and slot spins), for the clerk's statement (the shift's orders are added by ClerkAccountSource.RecordHome).</summary>
    private int _purchases;

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

        Debug.Log($"[HomeManager] Day {_world.day} home phase starting: money={_world.money}, stability={_world.timelineStability:0.00}, familyMembers={_world.family.members.Count}.");

        // The break-in, the bill and the family's night, deterministically
        // seeded by the day so it's stable on reload.
        _evening = DayCycle.OpenHome(_world, _lib, _config, run.GetDaySeed());
        _slotRandom = new SeededRandom(Seeds.ForSlot(run.GetDaySeed()));
        _household = _evening.bill.total;
        RecordStatement();

        RefreshHud();

        Debug.Log("[HomeManager] <<< Exiting Start (showing Expenses panel).");

        ShowExpenses();
    }

    /// <summary>Step 1: expense breakdown + family conditions (Treat option).</summary>
    private void ShowExpenses()
    {
        Debug.Log("[HomeManager] >>> Entering ShowExpenses.");

        if (homeUI != null && homeUI.HasExpensesPanel)
            homeUI.ShowExpenses(_world, _evening, _config, HomeEconomy.GetCareCost(_world, _lib, _config), HandleTreatFamilyMember, ShowShop, ShopNext);
        else
        {
            Debug.Log("[HomeManager] ShowExpenses: no expenses panel, skipping to Shop.");
            ShowShop();
        }
    }

    /// <summary>The upgrades Home sells (venue Home: household improvements), in the library's order.</summary>
    private List<UpgradeSO> HomeUpgrades()
    {
        var upgrades = new List<UpgradeSO>();
        if (_lib != null)
            foreach (UpgradeSO u in _lib.Upgrades)
                if (u != null && u.venue == UpgradeVenue.Home)
                    upgrades.Add(u);
        return upgrades;
    }

    /// <summary>True when the shop step shows (its panel is wired and Home stocks an upgrade).</summary>
    private bool ShopNext => homeUI != null && homeUI.HasShopPanel && HomeUpgrades().Count > 0;

    /// <summary>Treats a family member, then refreshes the expenses panel.</summary>
    private void HandleTreatFamilyMember(int memberIndex)
    {
        Debug.Log($"[HomeManager] >>> Entering HandleTreatFamilyMember (memberIndex={memberIndex}).");

        int careCost = HomeEconomy.GetCareCost(_world, _lib, _config);
        if (HomeEconomy.TreatFamilyMember(_world, _lib, _config, memberIndex))
        {
            _household += careCost;
            RecordStatement();
            RefreshHud();

            // Refresh the panel in place (report numbers don't change; rows do).
            if (homeUI != null && homeUI.HasExpensesPanel)
                homeUI.ShowExpenses(_world, _evening, _config, careCost, HandleTreatFamilyMember, ShowShop, ShopNext);

            Debug.Log($"[HomeManager] <<< Exiting HandleTreatFamilyMember (treated, money={_world.money}).");
        }
        else
        {
            Debug.Log("[HomeManager] <<< Exiting HandleTreatFamilyMember (treatment not applied).");
        }
    }

    /// <summary>Step 2: the House (the Home upgrades' tree), skipped while Home stocks none.</summary>
    private void ShowShop()
    {
        Debug.Log("[HomeManager] >>> Entering ShowShop.");

        RefreshHud();

        if (ShopNext)
            homeUI.ShowShop(_world, _lib, HomeUpgrades(), HandleBuyUpgrade, ShowSlot);
        else
        {
            Debug.Log("[HomeManager] ShowShop: no shop panel or no Home upgrade stocked (the office's are in the PC's Orders app), skipping to Slot.");
            ShowSlot();
        }
    }

    /// <summary>Buys a Home upgrade when it is buyable (HomeEconomy.BuyHouseUpgrade: its prerequisites owned, the wallet covering its price): owned at once, its effects from the next night; then refreshes the House.</summary>
    private void HandleBuyUpgrade(UpgradeSO upgrade)
    {
        Debug.Log($"[HomeManager] >>> Entering HandleBuyUpgrade (upgrade='{upgrade?.displayName}').");

        int cost = HomeEconomy.BuyHouseUpgrade(_world, _lib, upgrade);
        if (cost < 0)
        {
            Debug.Log("[HomeManager] <<< Exiting HandleBuyUpgrade — not a buyable Home upgrade (owned, locked or too dear).");
            return;
        }

        _purchases += cost;
        RefreshHud();
        RecordStatement();

        // Re-show to refresh rows (costs/owned state) without advancing the flow.
        if (homeUI != null && homeUI.HasShopPanel)
            homeUI.ShowShop(_world, _lib, HomeUpgrades(), HandleBuyUpgrade, ShowSlot);

        Debug.Log($"[HomeManager] <<< Exiting HandleBuyUpgrade (bought '{upgrade.displayName}' for {cost}, money={_world.money}).");
    }

    /// <summary>Step 3: slot machine.</summary>
    private void ShowSlot()
    {
        Debug.Log("[HomeManager] >>> Entering ShowSlot.");

        RefreshHud();

        if (homeUI != null && homeUI.HasSlotPanel)
            homeUI.ShowSlot(_world, _config, HandleSpin, ShowSleep);
        else
        {
            Debug.Log("[HomeManager] ShowSlot: no slot panel, skipping to Sleep.");
            ShowSleep();
        }
    }

    /// <summary>
    /// Spends the spin cost (if affordable), picks a weighted SlotOutcomeSO
    /// from tonight's seeded stream, and applies its money/modifier/effect
    /// results. Returns the line shown to the player.
    /// </summary>
    private string HandleSpin()
    {
        Debug.Log($"[HomeManager] >>> Entering HandleSpin (money={_world.money}).");

        int spinCost = _config != null ? _config.slotSpinCost : 0;

        if (_world.money < spinCost)
        {
            Debug.Log($"[HomeManager] <<< Exiting HandleSpin — not enough credits ({_world.money} < {spinCost}).");
            return $"Not enough {UiText.Currency(UiText.WalletForm.Inline)} to spin.";
        }

        IReadOnlyList<SlotOutcomeSO> outcomes = _lib != null ? _lib.SlotOutcomes : System.Array.Empty<SlotOutcomeSO>();

        if (outcomes.Count == 0)
        {
            Debug.Log("[HomeManager] <<< Exiting HandleSpin — no slot outcomes configured.");
            return "The slot machine is out of order.";
        }

        _world.money -= spinCost;
        _purchases += spinCost;

        SlotOutcomeSO outcome = WeightedRandom.Pick(outcomes, o => o != null ? o.weight : 0f, _slotRandom);

        if (outcome == null)
        {
            RefreshHud();
            RecordStatement();
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

        RefreshHud();
        RecordStatement();

        string line = !string.IsNullOrEmpty(outcome.resultLine) ? outcome.resultLine : outcome.displayName;

        Debug.Log($"[HomeManager] <<< Exiting HandleSpin (outcome='{outcome.displayName}', spinCost={spinCost}, moneyDelta={outcome.moneyDelta}, money={_world.money}).");

        return outcome.moneyDelta != 0
            ? $"{line} ({outcome.moneyDelta:+0;-0} {UiText.Currency(UiText.WalletForm.Inline)})"
            : line;
    }

    /// <summary>Writes tonight's household costs, purchases and the wallet into the day's row of the clerk's statement (redesign phase 25; saved when Sleep saves).</summary>
    private void RecordStatement() =>
        ClerkAccountSource.RecordHome(_world, _household, _purchases, _lib, _config);

    /// <summary>Step 4: sleep prompt.</summary>
    private void ShowSleep()
    {
        Debug.Log("[HomeManager] >>> Entering ShowSleep.");

        RefreshHud();

        if (homeUI != null && homeUI.HasSleepPanel)
            homeUI.ShowSleep(_world, RadioLine(), HandleSleep);
        else
        {
            Debug.Log("[HomeManager] ShowSleep: no sleep panel, sleeping immediately.");
            HandleSleep();
        }
    }

    /// <summary>Tonight's radio line when the house owns the radio (ContentLibrarySO.Home: HomeContent.RadioLine by the day), else "".</summary>
    private string RadioLine()
    {
        HomeContent home = _lib != null ? _lib.Home : null;
        return home != null && _world.HasUpgrade(home.radioUpgrade) ? home.RadioLine(_world.day) : string.Empty;
    }

    /// <summary>
    /// Ends the day through RunManager.Sleep: the day-boundary ending check
    /// (failures, Retirement, the attribute epilogues), else the nightly
    /// resolve, day++, save, back to Office.
    /// </summary>
    private void HandleSleep()
    {
        Debug.Log($"[HomeManager] HandleSleep (day {_world?.day}): handing off to RunManager.Sleep.");

        if (!RunManager.HasInstance)
        {
            Debug.LogError("HomeManager.HandleSleep called with no RunManager instance.");
            return;
        }

        RunManager.Instance.Sleep();
    }

    /// <summary>Refreshes the HUD when the UI is wired (Unity's own null check, never ?. on a serialized reference: audit R2-013).</summary>
    private void RefreshHud()
    {
        if (homeUI != null)
            homeUI.UpdateHud(_world);
    }
}
