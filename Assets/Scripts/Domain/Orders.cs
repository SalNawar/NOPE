using System;
using System.Collections.Generic;

/// <summary>
/// One order of the Orders app (WorldState.orders; the portals spec v3 OR7,
/// §8.2): the upgrade, the day it was placed and paid, the price charged,
/// and the day it was delivered (0 while in transit). A cancelled order
/// leaves the log; a delivered one stays, for the node's "Delivered day N"
/// and the delivery memo.
/// </summary>
[Serializable]
public sealed class OrderEntry
{
    /// <summary>The upgrade ordered (UpgradeSO.id).</summary>
    public string upgradeId = string.Empty;

    /// <summary>The shift day it was placed (and paid).</summary>
    public int orderedDay;

    /// <summary>The price charged, refunded as charged by a same-day cancel.</summary>
    public int price;

    /// <summary>The day it was delivered (the start of the day after it was placed), or 0 while in transit.</summary>
    public int deliveredDay;
}

/// <summary>A node's state in the Orders app (the portals spec v3 OR3; never serialized).</summary>
public enum OrderState
{
    /// <summary>A prerequisite is not owned yet: greyed, a padlock, "Needs: …".</summary>
    Locked,

    /// <summary>It can be ordered: its price, the Order button.</summary>
    Orderable,

    /// <summary>Unlocked, but the wallet is short: "Not enough cr", the Order button disabled.</summary>
    TooDear,

    /// <summary>Ordered and paid, arriving at the start of the next day; cancellable on the day it was placed.</summary>
    InTransit,

    /// <summary>Owned: delivered, or unlocked another way (an effect, the debug panel).</summary>
    Owned
}

/// <summary>
/// The Orders app's order log (Saleh 2026-09-29: "items are paid when
/// ordered (a same-day cancel refunds), arrive at the start of the next day,
/// and a delivery notice arrives in Mail"; the portals spec v3 OR4-OR7,
/// §6.3): a node's state, placing an order (paid at once, the caller moves
/// the money), a same-day cancel (the price charged comes back), the orders
/// due at a day's start (DayCycle.AdvanceNight delivers them, in log order),
/// what arrived on a day (the delivery memo) and what a day's orders cost
/// (the statement's purchases). Pure, so every rule is tested headless.
/// </summary>
public static class Orders
{
    /// <summary>
    /// A node's state: Owned when <paramref name="owned"/> (however it came),
    /// else InTransit while an order for it is pending, else Locked unless
    /// <paramref name="unlocked"/> (UpgradeTree.Unlocked), else TooDear when
    /// <paramref name="money"/> is under <paramref name="price"/>, else Orderable.
    /// </summary>
    public static OrderState StateOf(string upgradeId, bool owned, bool unlocked, IReadOnlyList<OrderEntry> log, int money, int price)
    {
        if (owned)
            return OrderState.Owned;
        if (Pending(log, upgradeId) != null)
            return OrderState.InTransit;
        if (!unlocked)
            return OrderState.Locked;
        return money < price ? OrderState.TooDear : OrderState.Orderable;
    }

    /// <summary>
    /// Places an order when its state is Orderable: a pending entry for
    /// <paramref name="upgradeId"/> placed on <paramref name="day"/> at
    /// <paramref name="price"/> joins the log and is returned (the caller takes
    /// the price from the wallet); otherwise nothing changes and null is returned.
    /// </summary>
    public static OrderEntry Place(List<OrderEntry> log, string upgradeId, bool owned, bool unlocked, int money, int price, int day)
    {
        if (log == null || string.IsNullOrEmpty(upgradeId) || StateOf(upgradeId, owned, unlocked, log, money, price) != OrderState.Orderable)
            return null;

        var entry = new OrderEntry { upgradeId = upgradeId, orderedDay = day, price = price };
        log.Add(entry);
        return entry;
    }

    /// <summary>True when an order for <paramref name="upgradeId"/> placed on <paramref name="day"/> is still in transit (only then may it be cancelled).</summary>
    public static bool CanCancel(IReadOnlyList<OrderEntry> log, string upgradeId, int day)
    {
        OrderEntry pending = Pending(log, upgradeId);
        return pending != null && pending.orderedDay == day;
    }

    /// <summary>Cancels the order for <paramref name="upgradeId"/> placed on <paramref name="day"/> and still in transit: it leaves the log and its charged price is returned (the refund); 0 when there is none.</summary>
    public static int Cancel(List<OrderEntry> log, string upgradeId, int day)
    {
        if (log == null || !CanCancel(log, upgradeId, day))
            return 0;

        OrderEntry pending = Pending(log, upgradeId);
        log.Remove(pending);
        return pending.price;
    }

    /// <summary>The orders to deliver at the start of <paramref name="day"/>: placed before it and not delivered, in log order.</summary>
    public static List<OrderEntry> Due(IReadOnlyList<OrderEntry> log, int day)
    {
        var due = new List<OrderEntry>();
        foreach (OrderEntry e in log ?? Array.Empty<OrderEntry>())
            if (e != null && e.deliveredDay <= 0 && e.orderedDay < day)
                due.Add(e);
        return due;
    }

    /// <summary>The ids delivered on <paramref name="day"/>, in log order (the day's delivery memo).</summary>
    public static List<string> DeliveredOn(IReadOnlyList<OrderEntry> log, int day)
    {
        var ids = new List<string>();
        foreach (OrderEntry e in log ?? Array.Empty<OrderEntry>())
            if (e != null && e.deliveredDay > 0 && e.deliveredDay == day)
                ids.Add(e.upgradeId);
        return ids;
    }

    /// <summary>What the orders placed on <paramref name="day"/> cost, at their charged prices (a cancelled order has left the log): the statement's purchases for that day.</summary>
    public static int SpentOn(IReadOnlyList<OrderEntry> log, int day)
    {
        int spent = 0;
        foreach (OrderEntry e in log ?? Array.Empty<OrderEntry>())
            if (e != null && e.orderedDay == day)
                spent += e.price;
        return spent;
    }

    /// <summary>The pending order for <paramref name="upgradeId"/>, or null.</summary>
    public static OrderEntry Pending(IReadOnlyList<OrderEntry> log, string upgradeId)
    {
        foreach (OrderEntry e in log ?? Array.Empty<OrderEntry>())
            if (e != null && e.deliveredDay <= 0 && e.upgradeId == upgradeId)
                return e;
        return null;
    }

    /// <summary>The delivered order for <paramref name="upgradeId"/> (its "Delivered day N"), or null when it came another way or has not arrived.</summary>
    public static OrderEntry Delivery(IReadOnlyList<OrderEntry> log, string upgradeId)
    {
        foreach (OrderEntry e in log ?? Array.Empty<OrderEntry>())
            if (e != null && e.deliveredDay > 0 && e.upgradeId == upgradeId)
                return e;
        return null;
    }

    /// <summary>The day an order arrives: the start of the day after it was placed.</summary>
    public static int ArrivalDay(OrderEntry entry) => entry != null ? entry.orderedDay + 1 : 0;
}
