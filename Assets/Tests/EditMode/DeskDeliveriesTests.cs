using NUnit.Framework;

/// <summary>
/// The supply crate never brings what already stands on the desk (BUG-28c/28d,
/// DESK-PLAN's part: "Deliver skips owned items, so a cheat grant or a
/// duplicate order can't deliver twice"), and brings each delivery once;
/// a RING SERVICE sticker delivered to a panel already on the desk waits for
/// the crate (BUG-60a).
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

    private static UpgradeObjects.Module Serviced(bool newToday) => new UpgradeObjects.Module(true, true, newToday, RingLamp.Open);

    [Test]
    public void ASecondRingRepair_OnAPanelAlreadyOnTheDesk_WaitsForTheCrate()
    {
        var modules = new[] { Serviced(false), Serviced(true), default, default, default };
        CollectionAssert.AreEqual(new[] { 1 }, DeskDeliveries.StickersToWait(modules), "module 02's sticker came today: it pops with the crate, not at once");
    }

    [Test]
    public void TheFirstRingRepair_ThePanelWaitsWhole_NoStickerOnItsOwn()
    {
        var modules = new[] { default, Serviced(true), default, Serviced(true), default };
        CollectionAssert.IsEmpty(DeskDeliveries.StickersToWait(modules), "the panel itself came today: it waits whole (UpgradeObjects.PanelNew)");
    }

    [Test]
    public void NoNewRepair_NoStickerWaits()
    {
        CollectionAssert.IsEmpty(DeskDeliveries.StickersToWait(new[] { Serviced(false), default, Serviced(false), default, default }));
        CollectionAssert.IsEmpty(DeskDeliveries.StickersToWait(null));
    }

    [Test]
    public void TwoNewRepairs_OnAStandingPanel_BothWait()
    {
        var modules = new[] { Serviced(true), Serviced(false), default, default, Serviced(true) };
        CollectionAssert.AreEqual(new[] { 0, 4 }, DeskDeliveries.StickersToWait(modules));
    }
}
