using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Orders app's order log (Saleh 2026-09-29; the portals spec v3 OR4,
/// OR5, OR7, §6.3): a node's state (owned, in transit, locked, too dear,
/// orderable), an order paid when placed, a same-day cancel that refunds
/// the price charged, the deliveries due at the start of the next day, what
/// arrived on a day and what the day's orders cost.
/// </summary>
public class OrdersTests
{
    private static List<OrderEntry> Log(params OrderEntry[] entries) => entries.ToList();

    private static OrderEntry Entry(string id, int orderedDay, int price, int deliveredDay = 0) =>
        new OrderEntry { upgradeId = id, orderedDay = orderedDay, price = price, deliveredDay = deliveredDay };

    [Test]
    public void StateOf_OrderableWhenUnlockedNotOwnedNotInTransitAndAffordable()
    {
        Assert.AreEqual(OrderState.Orderable, Orders.StateOf("scanner_autofeed", false, true, Log(), 200, 200));
    }

    [Test]
    public void StateOf_TooDearWhenTheWalletIsShort()
    {
        Assert.AreEqual(OrderState.TooDear, Orders.StateOf("scanner_autofeed", false, true, Log(), 199, 200));
        Assert.AreEqual(OrderState.TooDear, Orders.StateOf("scanner_autofeed", false, true, Log(), -40, 200), "a wallet in debt orders nothing");
    }

    [Test]
    public void StateOf_LockedBeforeOrderableOrTooDear()
    {
        Assert.AreEqual(OrderState.Locked, Orders.StateOf("adv_scanner", false, false, Log(), 1000, 300));
        Assert.AreEqual(OrderState.Locked, Orders.StateOf("adv_scanner", false, false, Log(), 0, 300));
    }

    [Test]
    public void StateOf_InTransitWhilePending()
    {
        Assert.AreEqual(OrderState.InTransit, Orders.StateOf("scanner_autofeed", false, true, Log(Entry("scanner_autofeed", 3, 200)), 0, 200));
    }

    [Test]
    public void StateOf_OwnedWhenOwned_HoweverItCame()
    {
        Assert.AreEqual(OrderState.Owned, Orders.StateOf("scanner_autofeed", true, true, Log(Entry("scanner_autofeed", 1, 200, 2)), 0, 200));
        Assert.AreEqual(OrderState.Owned, Orders.StateOf("scanner_autofeed", true, true, Log(), 0, 200), "an effect or the debug panel unlocked it");
        Assert.AreEqual(OrderState.Owned, Orders.StateOf("adv_scanner", true, false, Log(), 0, 300), "owned is a fact, whatever it needs");
        Assert.AreEqual(OrderState.Owned, Orders.StateOf("scanner_autofeed", true, true, Log(Entry("scanner_autofeed", 4, 200)), 0, 200),
                        "owned wins over a pending order");
    }

    [Test]
    public void Place_AddsAPendingEntryWithTheDayAndPrice()
    {
        List<OrderEntry> log = Log();

        OrderEntry placed = Orders.Place(log, "scanner_autofeed", false, true, 250, 200, 3);

        Assert.IsNotNull(placed);
        Assert.AreSame(placed, log.Single());
        Assert.AreEqual("scanner_autofeed", placed.upgradeId);
        Assert.AreEqual(3, placed.orderedDay);
        Assert.AreEqual(200, placed.price, "the price charged is the price shown");
        Assert.AreEqual(0, placed.deliveredDay, "in transit");
        Assert.AreEqual(4, Orders.ArrivalDay(placed), "it arrives at the start of the next day");
    }

    [Test]
    public void Place_RefusesALockedOwnedPendingOrTooDearUpgrade()
    {
        List<OrderEntry> pending = Log(Entry("scanner_autofeed", 2, 200));
        Assert.IsNull(Orders.Place(pending, "scanner_autofeed", false, true, 1000, 200, 2), "in transit");
        Assert.AreEqual(1, pending.Count);

        List<OrderEntry> log = Log();
        Assert.IsNull(Orders.Place(log, "scanner_autofeed", true, true, 1000, 200, 2), "owned");
        Assert.IsNull(Orders.Place(log, "adv_scanner", false, false, 1000, 300, 2), "locked");
        Assert.IsNull(Orders.Place(log, "scanner_autofeed", false, true, 199, 200, 2), "too dear");
        Assert.IsNull(Orders.Place(log, "", false, true, 1000, 0, 2), "no id");
        CollectionAssert.IsEmpty(log);
    }

    [Test]
    public void Cancel_ReturnsTodaysPendingOrdersPriceAndRemovesIt()
    {
        List<OrderEntry> log = Log(Entry("interview_protocols", 1, 120, 2), Entry("scanner_autofeed", 4, 180));

        Assert.IsTrue(Orders.CanCancel(log, "scanner_autofeed", 4));
        Assert.AreEqual(180, Orders.Cancel(log, "scanner_autofeed", 4), "the price charged, even if today's price differs");
        CollectionAssert.AreEqual(new[] { "interview_protocols" }, log.Select(e => e.upgradeId).ToArray());
        Assert.AreEqual(0, Orders.Cancel(log, "scanner_autofeed", 4), "nothing left to cancel");
    }

    [Test]
    public void Cancel_RefusesAnOrderFromAnEarlierDay()
    {
        List<OrderEntry> log = Log(Entry("scanner_autofeed", 3, 200));

        Assert.IsFalse(Orders.CanCancel(log, "scanner_autofeed", 4));
        Assert.AreEqual(0, Orders.Cancel(log, "scanner_autofeed", 4));
        Assert.AreEqual(1, log.Count);
    }

    [Test]
    public void Cancel_RefusesADeliveredOrder()
    {
        List<OrderEntry> log = Log(Entry("scanner_autofeed", 4, 200, 4));

        Assert.IsFalse(Orders.CanCancel(log, "scanner_autofeed", 4));
        Assert.AreEqual(0, Orders.Cancel(log, "scanner_autofeed", 4));
        Assert.AreEqual(1, log.Count);
    }

    [Test]
    public void Due_OrdersPlacedBeforeTheDayNotYetDelivered()
    {
        List<OrderEntry> log = Log(Entry("a", 1, 10, 2), Entry("b", 2, 10), Entry("c", 3, 10));

        CollectionAssert.AreEqual(new[] { "b" }, Orders.Due(log, 3).Select(e => e.upgradeId).ToArray(), "c was ordered today and arrives tomorrow");
        CollectionAssert.AreEqual(new[] { "b", "c" }, Orders.Due(log, 4).Select(e => e.upgradeId).ToArray());
    }

    [Test]
    public void Due_KeepsLogOrder()
    {
        List<OrderEntry> log = Log(Entry("z", 2, 10), Entry("a", 1, 10), Entry("m", 2, 10));

        CollectionAssert.AreEqual(new[] { "z", "a", "m" }, Orders.Due(log, 3).Select(e => e.upgradeId).ToArray());
    }

    [Test]
    public void DeliveredOn_TheDaysArrivalsInLogOrder()
    {
        List<OrderEntry> log = Log(Entry("b", 2, 10, 3), Entry("a", 1, 10, 2), Entry("c", 2, 10, 3), Entry("d", 3, 10));

        CollectionAssert.AreEqual(new[] { "b", "c" }, Orders.DeliveredOn(log, 3));
        CollectionAssert.AreEqual(new[] { "a" }, Orders.DeliveredOn(log, 2));
        CollectionAssert.IsEmpty(Orders.DeliveredOn(log, 4), "a pending order has arrived nowhere yet");
        Assert.AreEqual(3, Orders.Delivery(log, "c").deliveredDay, "a node's delivery, for its \"Delivered day N\"");
        Assert.IsNull(Orders.Delivery(log, "d"));
    }

    [Test]
    public void SpentOn_TheDaysOrdersAtTheirChargedPrices()
    {
        List<OrderEntry> log = Log(Entry("a", 2, 120, 3), Entry("b", 3, 80), Entry("c", 3, 200));

        Assert.AreEqual(280, Orders.SpentOn(log, 3));
        Assert.AreEqual(120, Orders.SpentOn(log, 2), "a delivered order still counts on the day it was paid");
        Assert.AreEqual(0, Orders.SpentOn(log, 5));
        Assert.AreEqual(0, Orders.SpentOn(null, 5));
    }

    [Test]
    public void AnOlderLogsNullEntries_AreSkipped()
    {
        List<OrderEntry> log = Log(null, Entry("b", 2, 10));

        Assert.AreEqual(OrderState.InTransit, Orders.StateOf("b", false, true, log, 0, 10));
        Assert.AreEqual(1, Orders.Due(log, 3).Count);
        Assert.AreEqual(10, Orders.SpentOn(log, 2));
        Assert.IsTrue(Orders.CanCancel(log, "b", 2));
    }
}
