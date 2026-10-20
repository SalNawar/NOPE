using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The screen transitions (Transit and SceneTransit, FLOW's; BUG-32a). In
/// play mode they ease over the kit's 0.2-0.3 s (TransitCurve, tested in
/// FlowRulesTests; the transitions' recordings are FLOW's QA recipes);
/// outside play mode they cut at once (the end state, then done) and load a
/// scene as SceneManager.LoadScene does, which these tests pin. Tests in the
/// "NeedsEditor" category create Unity objects or load scenes and run only
/// inside Unity.
/// </summary>
public class FlowSeamTests
{
    [Test]
    public void Transit_WithNoTarget_CallsDoneAtOnce()
    {
        int done = 0;
        Transit.Fade(null, 0f, 0.5f, () => done++);
        Transit.Slide(null, Vector2.one, 0.5f, () => done++);
        Assert.AreEqual(2, done);
        Assert.DoesNotThrow(() => Transit.Fade(null, 1f, 0.5f));
        Assert.DoesNotThrow(() => Transit.Slide(null, Vector2.zero, 0.5f));
    }

    [Test, Category("NeedsEditor")]
    public void Transit_OutsidePlay_WaitsNoTime_AndTheKitLengthIsInRange()
    {
        int done = 0;
        Transit.Wait(2f, () => done++);
        Assert.AreEqual(1, done);
        Assert.That(Transit.Seconds, Is.InRange(TransitCurve.MinSeconds, TransitCurve.MaxSeconds));
    }

    [Test, Category("NeedsEditor")]
    public void Transit_OutsidePlay_CutsToTheEndState_ThenCallsDone()
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
    public void SceneTransit_OutsidePlay_LoadsAsSceneManagerDoes()
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

    [Test, Category("NeedsEditor")]
    public void SealArt_PaintsEachOutlineOnce_TheSecondRequestIsTheSameTexture()
    {
        // BUG-31a: the seals are painted once a session (the office's load spent most of its time painting them).
        foreach (SealShape shape in (SealShape[])Enum.GetValues(typeof(SealShape)))
        {
            Texture2D first = SealArt.Texture(shape);
            Assert.IsNotNull(first, shape.ToString());
            Assert.AreSame(first, SealArt.Texture(shape), $"{shape}: no repaint on the second request");
        }
    }

    [Test, Category("NeedsEditor")]
    public void DaterImpressionArt_AWordTheRubberCannotPrint_PrintsTheEnglishVerdict()
    {
        // BUG-38b: a Greek (or Arabic, Japanese, Chinese) verdict word printed blank; the bureau's rubber prints its English word.
        Texture2D greek = DaterImpressionArt.Paint("ΕΓΚΡΙΘΗΚΕ", DaterImpressionArt.ApprovedInk, "", "", null, 1f, 7, false);
        Texture2D english = DaterImpressionArt.Paint("APPROVED", DaterImpressionArt.ApprovedInk, "", "", null, 1f, 7, false);
        Texture2D arabic = DaterImpressionArt.Paint("مرفوض", DaterImpressionArt.DeniedInk, "", "", null, 1f, 7, false);
        Texture2D denied = DaterImpressionArt.Paint("DENIED", DaterImpressionArt.DeniedInk, "", "", null, 1f, 7, false);
        try
        {
            CollectionAssert.AreEqual(english.GetPixels32(), greek.GetPixels32(), "the Greek word prints APPROVED");
            CollectionAssert.AreEqual(denied.GetPixels32(), arabic.GetPixels32(), "the Arabic word prints DENIED");
            Assert.IsTrue(Array.Exists(english.GetPixels32(), p => p.a > 0), "the word is inked");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(greek);
            UnityEngine.Object.DestroyImmediate(english);
            UnityEngine.Object.DestroyImmediate(arabic);
            UnityEngine.Object.DestroyImmediate(denied);
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
