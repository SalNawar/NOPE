using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HALL-FX's P0-A seam HelixRiverMonitor.SetWarning (FLOW, CONTENT and
/// TIMELINE-FAIL call it): it holds the warning as runtime state only, so a
/// caller can switch it on and off freely and nothing it does is ever saved
/// into the scene (the monitor's serialized state is the same after a
/// warning on and off). What the held warning shows is HelixRiver's
/// (HelixRiverTests.TheWarning_ShowsSteadilyNearTheDestructionPoint_AndCanBeHeld).
/// The test creates a Unity object ("NeedsEditor") and runs only inside Unity.
/// </summary>
public class HallFxSeamTests
{
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
