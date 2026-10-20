using System;
using NUnit.Framework;

/// <summary>
/// DOC-HANDLE's P0-A seam DeskEvents: each Raise* reaches the event's
/// listeners with its arguments (nothing in the game raises them yet).
/// </summary>
public class DocHandleSeamTests
{
    [Test]
    public void DeskEvents_EachRaiseReachesItsListeners()
    {
        int landed = 0, ended = 0, rank = -1;
        string upgrade = null;
        Action<DeskDocument> onLanded = paper => landed++;
        Action onEnded = () => ended++;
        Action<string> onUpgrade = id => upgrade = id;
        Action<DeskDocument, int> onStack = (paper, r) => rank = r;
        DeskEvents.DocumentLanded += onLanded;
        DeskEvents.CaseEnded += onEnded;
        DeskEvents.UpgradeArrived += onUpgrade;
        DeskEvents.StackChanged += onStack;
        try
        {
            DeskEvents.RaiseDocumentLanded(null);
            DeskEvents.RaiseCaseEnded();
            DeskEvents.RaiseUpgradeArrived("seam.upgrade");
            DeskEvents.RaiseStackChanged(null, 2);
            Assert.AreEqual(1, landed);
            Assert.AreEqual(1, ended);
            Assert.AreEqual("seam.upgrade", upgrade);
            Assert.AreEqual(2, rank);
        }
        finally
        {
            DeskEvents.DocumentLanded -= onLanded;
            DeskEvents.CaseEnded -= onEnded;
            DeskEvents.UpgradeArrived -= onUpgrade;
            DeskEvents.StackChanged -= onStack;
        }

        Assert.DoesNotThrow(() =>
        {
            DeskEvents.RaiseDocumentLanded(null);
            DeskEvents.RaiseCaseEnded();
            DeskEvents.RaiseUpgradeArrived(null);
            DeskEvents.RaiseStackChanged(null, 0);
        }, "raised with no listener");
        Assert.AreEqual(1, landed, "a removed listener hears nothing");
    }
}
