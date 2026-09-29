using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// One upgraded scanner at a time (Saleh 2026-09-29: "you can only have one
/// type of upgraded scanner installed at a time"): upgrades sharing an
/// install slot may all be owned, but only the slot's installed one is in
/// force; a delivery arrives installed; the player chooses another owned one
/// in Orders and it goes in at the next day's start, like a delivery.
/// </summary>
public class InstallsTests
{
    private const string Feed = "scanner_autofeed", Analysis = "adv_scanner";

    private static string SlotOf(string id) => id == Feed || id == Analysis ? "scanner" : string.Empty;

    private static List<InstallEntry> Installed(string id, string next = "") =>
        new List<InstallEntry> { new InstallEntry { slot = "scanner", installed = id, next = next } };

    [Test]
    public void InForce_EveryOwnedUpgradeWithoutASlot_AndEachSlotsInstalledOne()
    {
        List<string> inForce = Installs.InForce(new[] { "interview_protocols", Feed, Analysis, "diplo_contacts" }, SlotOf, Installed(Feed));

        CollectionAssert.AreEqual(new[] { "interview_protocols", Feed, "diplo_contacts" }, inForce, "the Analysis Scanner is owned but in storage");
    }

    [Test]
    public void InstalledIn_WithoutARecord_IsTheLastOwnedOfTheSlot()
    {
        // A save from before installs owned both from Home's shop (Analysis bought last), or the debug panel unlocked one.
        Assert.AreEqual(Analysis, Installs.InstalledIn(new List<InstallEntry>(), "scanner", new[] { Feed, "x", Analysis }, SlotOf));
        Assert.AreEqual(Feed, Installs.InstalledIn(null, "scanner", new[] { Feed }, SlotOf));
        Assert.AreEqual(string.Empty, Installs.InstalledIn(null, "scanner", new[] { "x" }, SlotOf), "nothing owned for the slot");
    }

    [Test]
    public void InstalledIn_ARecordNoLongerOwned_FallsBackToTheOwnedOnes()
    {
        Assert.AreEqual(Feed, Installs.InstalledIn(Installed(Analysis), "scanner", new[] { Feed }, SlotOf));
    }

    [Test]
    public void Arrive_ADeliveryGoesInAtOnce_AndDropsAPendingSwap()
    {
        List<InstallEntry> installs = Installed(Feed, Feed);

        Installs.Arrive(installs, "scanner", Analysis);

        Assert.AreEqual(Analysis, installs.Single().installed);
        Assert.AreEqual(string.Empty, installs.Single().next);
        var fresh = new List<InstallEntry>();
        Installs.Arrive(fresh, "scanner", Feed);
        Assert.AreEqual(Feed, fresh.Single().installed, "the slot's first delivery makes its record");
    }

    [Test]
    public void Choose_AnOwnedStoredUpgrade_SwapsInTomorrow()
    {
        List<InstallEntry> installs = Installed(Analysis);
        string[] owned = { Feed, Analysis };

        Assert.IsTrue(Installs.Choose(installs, "scanner", Feed, owned, SlotOf));

        Assert.AreEqual(Analysis, Installs.InstalledIn(installs, "scanner", owned, SlotOf), "still today's scanner");
        Assert.AreEqual(Feed, Installs.NextIn(installs, "scanner"));
        Assert.AreEqual(InstallState.InstallsTomorrow, Installs.StateOf(Feed, "scanner", installs, owned, SlotOf));
        Assert.AreEqual(InstallState.LeavesTomorrow, Installs.StateOf(Analysis, "scanner", installs, owned, SlotOf));
    }

    [Test]
    public void Choose_TheInstalledOne_CancelsTheSwap()
    {
        List<InstallEntry> installs = Installed(Analysis, Feed);
        string[] owned = { Feed, Analysis };

        Assert.IsTrue(Installs.Choose(installs, "scanner", Analysis, owned, SlotOf));

        Assert.AreEqual(string.Empty, Installs.NextIn(installs, "scanner"));
        Assert.AreEqual(InstallState.Installed, Installs.StateOf(Analysis, "scanner", installs, owned, SlotOf));
        Assert.AreEqual(InstallState.Stored, Installs.StateOf(Feed, "scanner", installs, owned, SlotOf));
    }

    [Test]
    public void Choose_RefusesAnUpgradeNotOwnedOrWithoutASlot()
    {
        List<InstallEntry> installs = Installed(Feed);

        Assert.IsFalse(Installs.Choose(installs, "scanner", Analysis, new[] { Feed }, SlotOf), "not owned");
        Assert.IsFalse(Installs.Choose(installs, "", "diplo_contacts", new[] { Feed, "diplo_contacts" }, SlotOf), "no slot");
        Assert.AreEqual(string.Empty, Installs.NextIn(installs, "scanner"));
        Assert.AreEqual(InstallState.None, Installs.StateOf("diplo_contacts", "", installs, new[] { "diplo_contacts" }, SlotOf));
        Assert.AreEqual(InstallState.None, Installs.StateOf(Analysis, "scanner", installs, new[] { Feed }, SlotOf), "not owned: nothing to install");
    }

    [Test]
    public void Turn_TheNightPutsEachSwapIn()
    {
        List<InstallEntry> installs = Installed(Analysis, Feed);

        Installs.Turn(installs);

        Assert.AreEqual(Feed, installs.Single().installed);
        Assert.AreEqual(string.Empty, installs.Single().next);
        Installs.Turn(installs);
        Assert.AreEqual(Feed, installs.Single().installed, "no swap, no change");
    }

    [Test]
    public void ScannerDay_ReadsOnlyTheInstalledScanner()
    {
        ScannerDay one = ScannerDay.From(Installs.InForce(new[] { Feed, Analysis }, SlotOf, Installed(Analysis)));

        Assert.IsFalse(one.AutoFeed, "the Auto-Feed is in storage: no feeder tray, no feed");
        Assert.IsTrue(one.Analysis);
    }
}
