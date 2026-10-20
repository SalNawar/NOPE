using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// BUG-65a's measurement names the owning track of each allocating call site
/// (the PLAY-20 sites: the IMGUI cheat overlay, the machines' ChipReader, the
/// close-ups), and totals sites across frames. Pure; runs outside Unity.
/// </summary>
public class QaGcSitesTests
{
    [Test]
    public void OwnerOf_NamesThePlay20Sites()
    {
        Assert.AreEqual("RUN-SAFE", QaGcSites.OwnerOf("PlayerLoop > Update.ScriptRunBehaviourUpdate > DebugPanelController.OnGUI"));
        Assert.AreEqual("RUN-SAFE", QaGcSites.OwnerOf("PlayerLoop > PreLateUpdate > GUIUtility.OnGUI"));
        Assert.AreEqual("DOC-HANDLE", QaGcSites.OwnerOf("PlayerLoop > Update > ChipReader.Update"));
        Assert.AreEqual("CAM", QaGcSites.OwnerOf("PlayerLoop > LateUpdate > MachineCloseUp.LateUpdate"));
        Assert.AreEqual("unassigned", QaGcSites.OwnerOf("PlayerLoop > Update > Something.Else"));
    }

    [Test]
    public void Accumulate_AndTable_TotalPerSite_HeaviestFirst()
    {
        var totals = new Dictionary<string, QaGcSites.Site>();
        QaGcSites.Accumulate(totals, new Dictionary<string, (double, double)> { ["a > ChipReader"] = (100, 2), ["b"] = (40, 1) });
        QaGcSites.Accumulate(totals, new Dictionary<string, (double, double)> { ["a > ChipReader"] = (100, 2) });
        List<string> table = QaGcSites.Table(totals.Values, 2);
        Assert.AreEqual(3, table.Count);
        StringAssert.StartsWith("100\t2\t2\t2\tDOC-HANDLE\ta > ChipReader", table[1]);
        StringAssert.StartsWith("20\t1\t2\t1\tunassigned\tb", table[2]);
        Assert.AreEqual(1, totals.Values.Single(s => s.Path == "b").Frames);
    }
}
