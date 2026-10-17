using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The desk machines spec §4: what the upgrades' desk objects show
/// (UpgradeObjects: the BABEL rack's bays, the RING SERVICE panel's modules)
/// and the props' art contracts (PropArt).
/// </summary>
public class UpgradeObjectsTests
{
    private static readonly string[] Packs = { "near_east", "mediterranean", "east_asia", "north_europe" };

    [Test]
    public void Bays_SeatACartridgePerOwnedPack_InThePacksOrder()
    {
        var owned = new HashSet<string> { "tr_mediterranean_spoken", "tr_north_europe_spoken" };
        UpgradeObjects.Bay[] bays = UpgradeObjects.Bays(Packs, owned.Contains, new HashSet<string>());
        CollectionAssert.AreEqual(new[] { false, true, false, true }, bays.Select(b => b.Owned).ToArray());
        Assert.IsFalse(bays.Any(b => b.NewToday));
        Assert.IsFalse(UpgradeObjects.RackNew(bays), "nothing came today");
    }

    [Test]
    public void Bays_ACartridgeDeliveredToday_IsNew_TheRackNewOnlyWhenAllAre()
    {
        var owned = new HashSet<string> { "tr_near_east_spoken", "tr_east_asia_spoken" };
        UpgradeObjects.Bay[] bays = UpgradeObjects.Bays(Packs, owned.Contains, new HashSet<string> { "tr_east_asia_spoken", "tr_north_europe_spoken" });
        Assert.IsTrue(bays[2].NewToday);
        Assert.IsFalse(bays[3].NewToday, "not owned: never new");
        Assert.IsFalse(UpgradeObjects.RackNew(bays), "the Near East cartridge was already seated: only the new one waits");

        UpgradeObjects.Bay[] first = UpgradeObjects.Bays(Packs, new HashSet<string> { "tr_near_east_spoken" }.Contains, new HashSet<string> { "tr_near_east_spoken" });
        Assert.IsTrue(UpgradeObjects.RackNew(first), "the first translator: the whole rack came in the crate");
        Assert.IsFalse(UpgradeObjects.RackNew(UpgradeObjects.Bays(Packs, _ => false, null)), "no translator: no rack");
    }

    private static PortalRoute Ring(int n, PortalState state, string repair) => new PortalRoute(n, "Ring " + n, PortalRole.Departures, state, null, null, repair);

    [Test]
    public void Modules_ShowEachPortalByItsNumber_LitInService_StickeredOnceRepaired()
    {
        var portals = new List<PortalRoute>
        {
            Ring(1, PortalState.InService, ""), Ring(2, PortalState.InService, "repair_portal_02"), Ring(3, PortalState.UnderMaintenance, "repair_return_gate"),
            Ring(5, PortalState.UnderMaintenance, "repair_portal_05"), Ring(9, PortalState.InService, "x"),
        };
        UpgradeObjects.Module[] m = UpgradeObjects.Modules(portals, new HashSet<string> { "repair_portal_02" }.Contains, new HashSet<string> { "repair_portal_02" });
        Assert.AreEqual(UpgradeObjects.RingModules, m.Length);
        CollectionAssert.AreEqual(new[] { true, true, false, false, false }, m.Select(x => x.InService).ToArray(), "module 4 has no portal today: dark; portal 9 has no module");
        CollectionAssert.AreEqual(new[] { false, true, false, false, false }, m.Select(x => x.Serviced).ToArray(), "portal 1 needs no repair: no sticker");
        Assert.IsTrue(UpgradeObjects.PanelShown(m));
        Assert.IsTrue(UpgradeObjects.PanelNew(m), "the first repair came today: the panel with it");
    }

    [Test]
    public void Panel_IsOnTheDeskOnceAnyRepairIsOwned_NewOnlyWithItsFirst()
    {
        var portals = new List<PortalRoute> { Ring(2, PortalState.InService, "repair_portal_02"), Ring(4, PortalState.InService, "repair_portal_04") };
        Assert.IsFalse(UpgradeObjects.PanelShown(UpgradeObjects.Modules(portals, _ => false, null)));
        UpgradeObjects.Module[] second = UpgradeObjects.Modules(portals, _ => true, new HashSet<string> { "repair_portal_04" });
        Assert.IsTrue(UpgradeObjects.PanelShown(second));
        Assert.IsFalse(UpgradeObjects.PanelNew(second), "portal 02's repair came before: the panel stood already");
    }

    [Test]
    public void PropContracts_NameEachObjectsMovingParts()
    {
        foreach (string[] contract in new[] { PropArt.SupplyCrate, PropArt.ScannerFeeder, PropArt.AnalysisBeacon, PropArt.ProtocolRing, PropArt.BabelRack, PropArt.LensKit, PropArt.Rolodex, PropArt.RingPanel })
        {
            var all = new HashSet<string>(contract);
            Assert.IsTrue(PropArt.UseArt(true, contract, all.Contains));
            Assert.AreEqual(contract.Length, all.Count, "no part named twice");
            all.Remove(contract[contract.Length - 1]);
            Assert.IsFalse(PropArt.UseArt(true, contract, all.Contains), "a missing part: the object is left out");
        }
        CollectionAssert.Contains(PropArt.BabelRack, "BabelRack/Cartridge_3/CartridgeLED_3");
        CollectionAssert.Contains(PropArt.RingPanel, "RingPanel/Sticker_5");
    }
}
