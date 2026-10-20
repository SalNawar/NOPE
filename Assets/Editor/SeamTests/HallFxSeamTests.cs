using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// HALL-FX's P0-A seam HelixRiverMonitor.SetWarning (FLOW and CONTENT call
/// it): the stub does nothing, so the monitor's serialized state is the same
/// after a warning on and off. The test creates a Unity object
/// ("NeedsEditor") and runs only inside Unity.
/// </summary>
public class HallFxSeamTests
{
    [Test, Category("NeedsEditor")]
    public void SetWarning_StubChangesNothing()
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
