using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The screen transitions (P0-A seams Transit and SceneTransit, FLOW's): the
/// stub cuts at once (the end state, then done) and loads a scene the way
/// RunManager does today (SceneManager.LoadScene). FLOW replaces these with
/// the real fades when it fills the seam. Tests in the "NeedsEditor"
/// category create Unity objects or load scenes and run only inside Unity.
/// </summary>
public class FlowSeamTests
{
    [Test]
    public void Transit_StubCallsDoneAtOnce_WithNoTarget()
    {
        int done = 0;
        Transit.Wait(2f, () => done++);
        Transit.Fade(null, 0f, 0.5f, () => done++);
        Transit.Slide(null, Vector2.one, 0.5f, () => done++);
        Assert.AreEqual(3, done);
        Assert.DoesNotThrow(() => Transit.Fade(null, 1f, 0.5f));
        Assert.DoesNotThrow(() => Transit.Slide(null, Vector2.zero, 0.5f));
    }

    [Test, Category("NeedsEditor")]
    public void Transit_StubCutsToTheEndState_ThenCallsDone()
    {
        var go = new GameObject("FlowSeamTests", typeof(RectTransform), typeof(CanvasGroup));
        try
        {
            var group = go.GetComponent<CanvasGroup>();
            var rect = go.GetComponent<RectTransform>();
            float alphaAtDone = -1f;
            Vector2 positionAtDone = Vector2.zero;
            Transit.Fade(group, 0.25f, 1f, () => alphaAtDone = group.alpha);
            Transit.Slide(rect, new Vector2(12f, -7f), 1f, () => positionAtDone = rect.anchoredPosition);
            Assert.AreEqual(0.25f, alphaAtDone, 1e-5f);
            Assert.AreEqual(new Vector2(12f, -7f), positionAtDone);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test, Category("NeedsEditor")]
    public void SceneTransit_StubLoadsAsSceneManagerDoes()
    {
        const string scene = "__FlowSeamTests_NoSuchScene__";
        bool ignored = LogAssert.ignoreFailingMessages;
        LogAssert.ignoreFailingMessages = true;
        try
        {
            Assert.AreEqual(Outcome(() => SceneManager.LoadScene(scene)), Outcome(() => SceneTransit.Load(scene)));
        }
        finally
        {
            LogAssert.ignoreFailingMessages = ignored;
        }
    }

    /// <summary>What a call does, as text: the exception it throws (type and message), or "" when it returns.</summary>
    private static string Outcome(Action call)
    {
        try
        {
            call();
            return string.Empty;
        }
        catch (Exception e)
        {
            return $"{e.GetType().FullName}: {e.Message}";
        }
    }
}
