using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The screen transitions (Transit and SceneTransit, FLOW's; BUG-32a). In
/// play mode they ease over the kit's 0.2-0.3 s (TransitCurve, tested in
/// FlowRulesTests; the transitions' recordings are FLOW's QA recipes);
/// outside play mode they cut at once (the end state, then done) and load a
/// scene as SceneManager.LoadScene does, which these tests pin, as they pin
/// the scene change's shutter under the save notice. Tests in the
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

    [Test]
    public void TheSceneChangeShutter_SortsUnderTheSaveNotice()
    {
        // Round 4: at the same order the two overlay canvases fell back to hierarchy order, so a failed-save strip raised just before a load (RUN-SAFE's SaveNotice) could sit under the shutter.
        int veil = PrivateConstant(typeof(TransitRunner), "VeilOrder");
        int notice = PrivateConstant(typeof(SaveNotice), "SortingOrder");
        Assert.Less(veil, notice, "the shutter sorts under the save notice's strip");
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

    [Test, Category("NeedsEditor")]
    public void DaterImpressionArt_TheVerdictNotTheInk_ChoosesTheWordARubberCannotPrintFallsBackTo()
    {
        // A tinted ink with the verdict passed prints that verdict's English word; an ink that is neither verdict's is refused, never read as DENIED.
        var tint = new Color32(60, 140, 200, 255);
        Texture2D tinted = DaterImpressionArt.Paint("ΕΓΚΡΙΘΗΚΕ", true, tint, "", "", null, 1f, 7, false);
        Texture2D approved = DaterImpressionArt.Paint("APPROVED", true, DaterImpressionArt.ApprovedInk, "", "", null, 1f, 7, false);
        try
        {
            Color32[] a = tinted.GetPixels32(), b = approved.GetPixels32();
            Assert.AreEqual(b.Length, a.Length);
            for (int i = 0; i < a.Length; i++)
                Assert.AreEqual(b[i].a, a[i].a, $"pixel {i}: the tinted approval prints APPROVED's outline");
            Assert.Throws<ArgumentException>(() => DaterImpressionArt.Paint("APPROVED", tint, "", "", null, 1f, 7, false));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(tinted);
            UnityEngine.Object.DestroyImmediate(approved);
        }
    }

    [Test, Category("NeedsEditor")]
    public void DaterImpressionArt_TheSecondPrintOfTheSameTexts_ReusesItsLayers_AndANewDateReusesTheWord()
    {
        // BUG-31a: a print's clean layers are painted once per texts, and the word's outline (the costly layer) once per word.
        IDictionary layers = PrivateCache("Cache"), words = PrivateCache("Words");
        layers.Clear();
        words.Clear();
        var made = new List<Texture2D>();
        try
        {
            made.Add(DaterImpressionArt.Paint("APPROVED", true, DaterImpressionArt.ApprovedInk, "20 OCT 2150", "BY: 7", null, 1f, 1));
            Assert.AreEqual(1, layers.Count);
            Assert.AreEqual(1, words.Count);
            object first = Single(layers);
            made.Add(DaterImpressionArt.Paint("APPROVED", true, DaterImpressionArt.ApprovedInk, "20 OCT 2150", "BY: 7", null, 0.6f, 2));
            Assert.AreEqual(1, layers.Count, "the same texts: no repaint");
            Assert.AreSame(first, Single(layers));
            object word = Single(words);
            made.Add(DaterImpressionArt.Paint("APPROVED", true, DaterImpressionArt.ApprovedInk, "21 OCT 2150", "BY: 7", null, 1f, 3));
            Assert.AreEqual(2, layers.Count, "a new date paints its own layers");
            Assert.AreEqual(1, words.Count, "the word is not painted again");
            Assert.AreSame(word, Single(words));
        }
        finally
        {
            foreach (Texture2D t in made)
                UnityEngine.Object.DestroyImmediate(t);
        }
    }

    /// <summary>DaterImpressionArt's private static cache <paramref name="name"/> (the tests read it; nothing else does).</summary>
    private static IDictionary PrivateCache(string name)
    {
        FieldInfo field = typeof(DaterImpressionArt).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, name);
        return (IDictionary)field.GetValue(null);
    }

    /// <summary>The private int constant <paramref name="name"/> of <paramref name="type"/> (the tests read it; nothing else does).</summary>
    private static int PrivateConstant(Type type, string name)
    {
        FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, $"{type.Name}.{name}");
        Assert.IsTrue(field.IsLiteral, $"{type.Name}.{name} is a constant");
        return (int)field.GetRawConstantValue();
    }

    /// <summary>The only value of <paramref name="cache"/>.</summary>
    private static object Single(IDictionary cache)
    {
        Assert.AreEqual(1, cache.Count);
        foreach (DictionaryEntry entry in cache)
            return entry.Value;
        return null;
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
