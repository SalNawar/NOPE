using NUnit.Framework;

/// <summary>
/// The supply crate never brings what already stands on the desk (BUG-28c/28d,
/// DESK-PLAN's part: "Deliver skips owned items, so a cheat grant or a
/// duplicate order can't deliver twice"), and brings each delivery once.
/// </summary>
public class DeskDeliveriesTests
{
    [Test]
    public void TheCrate_SkipsWhatAlreadyStoodOnTheDesk()
    {
        CollectionAssert.AreEqual(new[] { "adv_scanner" }, DeskDeliveries.ToUnpack(new[] { "diplo_contacts", "adv_scanner" }, new[] { "diplo_contacts" }));
    }

    [Test]
    public void TheCrate_BringsEachDeliveryOnce_InOrder()
    {
        CollectionAssert.AreEqual(new[] { "b", "a" }, DeskDeliveries.ToUnpack(new[] { "b", "a", "b", "", null }, null));
    }

    [Test]
    public void NothingDelivered_NoCrate()
    {
        CollectionAssert.IsEmpty(DeskDeliveries.ToUnpack(null, new[] { "a" }));
        CollectionAssert.IsEmpty(DeskDeliveries.ToUnpack(new[] { "a" }, new[] { "a" }), "a cheat grant before its order arrived: no second crate");
    }
}
