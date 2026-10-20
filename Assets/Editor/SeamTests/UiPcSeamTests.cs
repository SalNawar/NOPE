using NUnit.Framework;
using UnityEngine;

/// <summary>
/// UI-PC's P0-A seams, as UI-PC ships them: PcFrame.CanOpen (true while the
/// screen is on, MonitorScreen telling it; true with no screen; the F1 guard
/// reads it, BUG-58c) and OrderBook.CancelPendingFor (OrderBook's own
/// same-day cancel and refund, Cancel: a pending order is always today's).
/// Tests in the "NeedsEditor" category create Unity objects and run only
/// inside Unity.
/// </summary>
public class UiPcSeamTests
{
    private const string Item = "seam.test.item";

    [Test]
    public void PcFrame_CanOpen_FollowsTheScreen()
    {
        Assert.IsTrue(PcFrame.CanOpen, "no screen has said otherwise");
        try
        {
            PcFrame.ScreenPowered(false);
            Assert.IsFalse(PcFrame.CanOpen, "F1 never opens a dark frame");
            PcFrame.ScreenPowered(true);
            Assert.IsTrue(PcFrame.CanOpen);
        }
        finally
        {
            PcFrame.ScreenPowered(true);
        }
    }

    [Test]
    public void CancelPendingFor_RefusesWithoutAWorldOrAnItem_AsCancel()
    {
        var world = new WorldState();
        Assert.IsFalse(OrderBook.CancelPendingFor(null, null));
        Assert.IsFalse(OrderBook.CancelPendingFor(world, null));
        Assert.AreEqual(OrderBook.Cancel(world, null), OrderBook.CancelPendingFor(world, null));
    }

    [Test, Category("NeedsEditor")]
    public void CancelPendingFor_IsCancel_TodaysOrderRefunded_YesterdaysKept()
    {
        var item = ScriptableObject.CreateInstance<UpgradeSO>();
        item.id = Item;
        try
        {
            foreach (int orderedDay in new[] { 3, 2 })
            {
                WorldState viaSeam = Ordered(orderedDay), viaCancel = Ordered(orderedDay);
                bool seam = OrderBook.CancelPendingFor(viaSeam, item);
                bool cancel = OrderBook.Cancel(viaCancel, item);
                Assert.AreEqual(cancel, seam, $"ordered on day {orderedDay}");
                Assert.AreEqual(viaCancel.money, viaSeam.money, $"ordered on day {orderedDay}");
                Assert.AreEqual(viaCancel.orders.Count, viaSeam.orders.Count, $"ordered on day {orderedDay}");
            }

            WorldState today = Ordered(3);
            Assert.IsTrue(OrderBook.CancelPendingFor(today, item));
            Assert.AreEqual(50, today.money, "the price charged comes back");
            Assert.AreEqual(0, today.orders.Count);
        }
        finally
        {
            Object.DestroyImmediate(item);
        }
    }

    /// <summary>A world on day 3 with 10 cr and the test item ordered (40 cr, in transit) on <paramref name="orderedDay"/>.</summary>
    private static WorldState Ordered(int orderedDay)
    {
        var world = new WorldState { day = 3, money = 10 };
        world.orders.Add(new OrderEntry { upgradeId = Item, orderedDay = orderedDay, price = 40 });
        return world;
    }
}
