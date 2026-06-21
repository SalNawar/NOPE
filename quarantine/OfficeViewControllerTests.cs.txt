using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class OfficeViewControllerTests
{
    private sealed class FakeRig : ICameraRig
    {
        public List<string> Calls = new List<string>();
        public void ShowOffice() => Calls.Add("office");
        public void ShowMonitor() => Calls.Add("monitor");
    }

    private static OfficeViewController NewController(out FakeRig rig, out GameObject desktop)
    {
        var go = new GameObject("view");
        var c = go.AddComponent<OfficeViewController>();
        rig = new FakeRig();
        desktop = new GameObject("desktop");
        c.InitForTest(rig, desktop);
        return c;
    }

    [Test]
    public void DefaultState_IsOfficeFocus_WithDesktopHidden()
    {
        var c = NewController(out var rig, out var desktop);
        Assert.AreEqual(OfficeView.OfficeFocus, c.Current);
        Assert.IsFalse(desktop.activeSelf);
        Assert.AreEqual("office", rig.Calls[rig.Calls.Count - 1]);
        Object.DestroyImmediate(c.gameObject);
        Object.DestroyImmediate(desktop);
    }

    [Test]
    public void FocusMonitor_SwitchesState_ShowsDesktop_RaisesEvent()
    {
        var c = NewController(out var rig, out var desktop);
        OfficeView? raised = null;
        c.ViewChanged += v => raised = v;

        c.FocusMonitor();

        Assert.AreEqual(OfficeView.MonitorFocus, c.Current);
        Assert.IsTrue(desktop.activeSelf);
        Assert.AreEqual("monitor", rig.Calls[rig.Calls.Count - 1]);
        Assert.AreEqual(OfficeView.MonitorFocus, raised);
        Object.DestroyImmediate(c.gameObject);
        Object.DestroyImmediate(desktop);
    }

    [Test]
    public void FocusMonitor_WhenAlreadyMonitor_IsNoOp()
    {
        var c = NewController(out var rig, out var desktop);
        c.FocusMonitor();
        int callsBefore = rig.Calls.Count;
        int events = 0;
        c.ViewChanged += _ => events++;

        c.FocusMonitor();

        Assert.AreEqual(callsBefore, rig.Calls.Count);
        Assert.AreEqual(0, events);
        Object.DestroyImmediate(c.gameObject);
        Object.DestroyImmediate(desktop);
    }

    [Test]
    public void FocusOffice_FromMonitor_HidesDesktop_AndReturns()
    {
        var c = NewController(out var rig, out var desktop);
        c.FocusMonitor();

        c.FocusOffice();

        Assert.AreEqual(OfficeView.OfficeFocus, c.Current);
        Assert.IsFalse(desktop.activeSelf);
        Assert.AreEqual("office", rig.Calls[rig.Calls.Count - 1]);
        Object.DestroyImmediate(c.gameObject);
        Object.DestroyImmediate(desktop);
    }
}
