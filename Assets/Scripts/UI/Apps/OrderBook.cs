using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Orders app's rules applied to the run (Saleh 2026-09-29: the upgrade
/// tree moves from Home's shop to the PC; the portals spec v3 OR1-OR9): the
/// catalogue (the content library's upgrades sold in Orders) and its tree
/// (UpgradeTree), a node's price (HomeEconomy.UpgradeCost, the price shown is
/// the price charged) and state (Orders.StateOf), ordering (paid at once) and
/// the same-day cancel (refunded as charged) on WorldState.orders, choosing
/// which owned scanner is installed (Installs), and the delivery at the start
/// of each day (<see cref="Deliver"/>, DayCycle.AdvanceNight), which the game
/// and the balance simulation share. The Orders window, the golden play and
/// any buyer policy order through here, so there is one purchase path.
/// </summary>
public static class OrderBook
{
    /// <summary>Every upgrade of <paramref name="lib"/> sold in Orders, in the library's order (the tree's nodes).</summary>
    public static List<UpgradeSO> Catalogue(ContentLibrarySO lib)
    {
        var catalogue = new List<UpgradeSO>();
        if (lib != null)
            foreach (UpgradeSO u in lib.Upgrades)
                if (u != null && u.venue == UpgradeVenue.Orders)
                    catalogue.Add(u);
        return catalogue;
    }

    /// <summary>Where each Orders node sits (UpgradeTree.Layout over the whole library: Home's upgrades are left out there).</summary>
    public static TreeLayout Layout(ContentLibrarySO lib) => UpgradeTree.Layout(Nodes(lib));

    /// <summary>What <paramref name="upgrade"/> costs today: its listed cost less any shop discount in force (HomeEconomy.UpgradeCost), shown on its node and charged when ordered.</summary>
    public static int Price(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade) => HomeEconomy.UpgradeCost(world, lib, upgrade, out _);

    /// <summary>The node's state today (Orders.StateOf: owned, in transit, locked until every prerequisite is owned, too dear, orderable).</summary>
    public static OrderState StateOf(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade)
    {
        if (world == null || upgrade == null)
            return OrderState.Locked;
        return Orders.StateOf(upgrade.id, world.HasUpgrade(upgrade.id), UpgradeTree.Unlocked(upgrade.Node, world.HasUpgrade), world.orders, world.money,
                              Price(world, lib, upgrade));
    }

    /// <summary>Orders <paramref name="upgrade"/> when it is an Orders node that can be ordered: the price leaves the wallet now and the order joins the log, arriving at the start of tomorrow's shift. True when placed.</summary>
    public static bool Order(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade)
    {
        if (world == null || upgrade == null || upgrade.venue != UpgradeVenue.Orders)
            return false;

        int price = Price(world, lib, upgrade);
        OrderEntry placed = Orders.Place(world.orders, upgrade.id, world.HasUpgrade(upgrade.id), UpgradeTree.Unlocked(upgrade.Node, world.HasUpgrade),
                                         world.money, price, world.day);
        if (placed == null)
            return false;

        world.money -= placed.price;
        Debug.Log($"[OrderBook] Day {world.day}: ordered '{upgrade.id}' for {placed.price} (money={world.money}); arrives day {Orders.ArrivalDay(placed)}.");
        return true;
    }

    /// <summary>Cancels today's order for <paramref name="upgrade"/> while it is in transit: the price charged comes back. True when cancelled.</summary>
    public static bool Cancel(WorldState world, UpgradeSO upgrade)
    {
        if (world == null || upgrade == null || !Orders.CanCancel(world.orders, upgrade.id, world.day))
            return false;

        int refund = Orders.Cancel(world.orders, upgrade.id, world.day);
        world.money += refund;
        Debug.Log($"[OrderBook] Day {world.day}: cancelled '{upgrade.id}', refunded {refund} (money={world.money}).");
        return true;
    }

    /// <summary>The upgrades in force today: every owned one without an install slot, and each slot's installed one (Installs.InForce; what ScannerDay reads at the day's start).</summary>
    public static List<string> InForce(WorldState world, ContentLibrarySO lib) =>
        world != null ? Installs.InForce(world.unlockedUpgradeIds, id => SlotOf(lib, id), world.installs) : new List<string>();

    /// <summary>Where an owned upgrade with an install slot stands (installed, in storage, going in or out tomorrow); None otherwise.</summary>
    public static InstallState InstallStateOf(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade) =>
        world != null && upgrade != null
            ? Installs.StateOf(upgrade.id, upgrade.installSlot, world.installs, world.unlockedUpgradeIds, id => SlotOf(lib, id))
            : InstallState.None;

    /// <summary>Chooses <paramref name="upgrade"/> for its install slot: an owned one in storage goes in at the start of tomorrow's shift; the installed one keeps its place (cancels a swap). True when chosen.</summary>
    public static bool Install(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade)
    {
        if (world == null || upgrade == null)
            return false;
        bool chosen = Installs.Choose(world.installs, upgrade.installSlot, upgrade.id, world.unlockedUpgradeIds, id => SlotOf(lib, id));
        if (chosen)
            Debug.Log($"[OrderBook] Day {world.day}: '{upgrade.id}' chosen for the {upgrade.installSlot} slot (installed tomorrow: '{Installs.NextIn(world.installs, upgrade.installSlot)}').");
        return chosen;
    }

    /// <summary>
    /// The start of a day (DayCycle.AdvanceNight, after the day turns): the
    /// scanner the clerk chose goes in (Installs.Turn), then every order due
    /// (Orders.Due: placed before today, in log order) arrives: owned, its
    /// unlock effect activated from today with its instant ops (only when it
    /// was not owned already), installed when it has a slot, and marked
    /// delivered today (the delivery memo reads it). Returns what arrived;
    /// a portal repair is a Portals node like any other.
    /// </summary>
    public static List<UpgradeSO> Deliver(WorldState world, ContentLibrarySO lib)
    {
        var delivered = new List<UpgradeSO>();
        if (world == null)
            return delivered;

        Installs.Turn(world.installs);
        foreach (OrderEntry entry in Orders.Due(world.orders, world.day))
        {
            bool had = world.HasUpgrade(entry.upgradeId);
            world.UnlockUpgrade(entry.upgradeId);
            entry.deliveredDay = world.day;

            UpgradeSO upgrade = lib != null ? lib.GetUpgradeById(entry.upgradeId) : null;
            if (upgrade == null)
            {
                Debug.LogWarning($"[OrderBook] Delivered '{entry.upgradeId}', which the content library does not list: owned, with no effect.");
                continue;
            }

            if (!had && upgrade.unlockEffect != null)
                TimelineService.ActivateEffect(world, upgrade.unlockEffect, $"Upgrade: {upgrade.displayName}", world.day,
                                               upgrade.unlockEffect.defaultDurationDays, applyInstantOps: true);
            Installs.Arrive(world.installs, upgrade.installSlot, upgrade.id);
            delivered.Add(upgrade);
            Debug.Log($"[OrderBook] Day {world.day}: '{upgrade.id}' delivered.");
        }
        return delivered;
    }

    /// <summary>Every library upgrade as the tree reads it (both venues, so the checks see Home's too).</summary>
    public static List<TreeNode> Nodes(ContentLibrarySO lib)
    {
        var nodes = new List<TreeNode>();
        if (lib != null)
            foreach (UpgradeSO u in lib.Upgrades)
                if (u != null)
                    nodes.Add(u.Node);
        return nodes;
    }

    /// <summary>An upgrade's install slot by its id ("" when it has none or is not in the library).</summary>
    private static string SlotOf(ContentLibrarySO lib, string upgradeId)
    {
        UpgradeSO upgrade = lib != null ? lib.GetUpgradeById(upgradeId) : null;
        return upgrade != null && upgrade.installSlot != null ? upgrade.installSlot : string.Empty;
    }
}
