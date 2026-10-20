using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// CONTENT's P0-A seam GuideDirector.IsShowing (FEEDBACK's callouts yield to
/// it): true while the guide plate shows anything (a step, a done line, a
/// moment or practice), false with none; it agrees with ShowingNow. The test
/// creates a Unity object ("NeedsEditor") and runs only inside Unity.
/// </summary>
public class ContentSeamTests
{
    [Test, Category("NeedsEditor")]
    public void IsShowing_IsTrueWhileThePlateShowsAnything()
    {
        var go = new GameObject("ContentSeamTests");
        try
        {
            GuideDirector guide = go.AddComponent<GuideDirector>();
            Assert.IsFalse(guide.IsShowing, "a new director shows nothing");

            FieldInfo showing = typeof(GuideDirector).GetField("_showing", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(showing, "GuideDirector._showing");
            foreach (string name in Enum.GetNames(showing.FieldType))
            {
                showing.SetValue(guide, Enum.Parse(showing.FieldType, name));
                Assert.AreEqual(name != "None", guide.IsShowing, name);
                Assert.AreEqual(guide.ShowingNow.Length > 0, guide.IsShowing, name);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
