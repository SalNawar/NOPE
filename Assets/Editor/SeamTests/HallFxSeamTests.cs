using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HALL-FX's P0-A seam HelixRiverMonitor.SetWarning (FLOW, CONTENT and
/// TIMELINE-FAIL call it): it holds the warning as runtime state only, so a
/// caller can switch it on and off freely and nothing it does is ever saved
/// into the scene (the monitor's serialized state is the same after a
/// warning on and off). What the held warning shows is HelixRiver's
/// (HelixRiverTests.TheWarning_ShowsSteadilyNearTheDestructionPoint_AndCanBeHeld).
/// The second test is the seam with UI-PC's tray: the PC's taskbar keeps
/// the Helix River's strip, "TimelineStrip", in its tray (FEATURES "The Helix
/// River": it shows "in the PC tray"; hall_fx_helix tracks it once Q opens
/// the PC). The night of 2026-10-28: UI-PC's tray clock (BUG-21a) dropped the
/// strip's call from BuildTaskbar, the scene rebuilt on the night head held
/// no "TimelineStrip", and the scenario stopped at its track op.
/// The tests create Unity objects ("NeedsEditor") and run only inside Unity.
/// </summary>
public class HallFxSeamTests
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;

    [Test, Category("NeedsEditor")]
    public void BuildTaskbar_KeepsTheHelixRiversStrip_InTheTray_BeforeTheClock()
    {
        var canvas = new GameObject("HallFxSeamTests_Desktop", typeof(RectTransform), typeof(Canvas));
        try
        {
            MethodInfo build = typeof(OfficeSceneUIBuilder).GetMethod("BuildTaskbar", PrivateStatic);
            Assert.IsNotNull(build, "OfficeSceneUIBuilder.BuildTaskbar");
            build.Invoke(null, new object[] { canvas.transform, null, null, null, null });

            Transform tray = canvas.transform.Find("Taskbar/TrayRow/Tray");
            Assert.IsNotNull(tray, "the taskbar's tray");
            Transform strip = tray.Find("TimelineStrip");
            Assert.IsNotNull(strip, "the Helix River's strip 'TimelineStrip' in the tray (hall_fx_helix tracks it by that name)");
            Transform clock = tray.Find("DateButton");
            Assert.IsNotNull(clock, "UI-PC's tray clock");
            Assert.Less(strip.GetSiblingIndex(), clock.GetSiblingIndex(), "the strip sits before the clock, at the tray's left");

            var width = (float)typeof(OfficeSceneUIBuilder).GetField("TrayRiverWidth", PrivateStatic).GetRawConstantValue();
            LayoutElement size = strip.GetComponent<LayoutElement>();
            Assert.IsNotNull(size, "the strip's layout element");
            Assert.AreEqual(width, size.preferredWidth, "the strip is TrayRiverWidth wide in the tray's row");
            Assert.AreEqual(width, size.minWidth, "and never narrower");

            HelixRiverMonitor river = strip.GetComponentInChildren<HelixRiverMonitor>(true);
            Assert.IsNotNull(river, "a HelixRiverMonitor under the strip (WireHelixRivers gives it the tuning and the shader)");
            Assert.AreEqual("River", river.name, "the monitor is the strip's River");
            Assert.IsNotNull(strip.GetComponent<HoverHint>(), "the strip's hover hint (tray.stability)");
        }
        finally
        {
            Object.DestroyImmediate(canvas);
        }
    }

    [Test, Category("NeedsEditor")]
    public void SetWarning_IsRuntimeStateOnly()
    {
        var go = new GameObject("HallFxSeamTests");
        try
        {
            HelixRiverMonitor monitor = go.AddComponent<HelixRiverMonitor>();
            string before = EditorJsonUtility.ToJson(monitor);
            monitor.SetWarning(true);
            Assert.AreEqual(before, EditorJsonUtility.ToJson(monitor), "warning on");
            monitor.SetWarning(false);
            Assert.AreEqual(before, EditorJsonUtility.ToJson(monitor), "warning off");
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
