using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The screen transitions (owner FLOW; BUG-32a, the bible's J5: "one shared
/// transition kit, 0.2-0.3 s"): a fade, a slide and a wait, each eased
/// (TransitCurve) on real time over <see cref="Seconds"/> or the given
/// length, then calling its done callback. Screen changes (the PC, Home's
/// panels and Sleep, the report, the briefing, Start Shift) go through it
/// instead of a hard cut. A new transition of the same target replaces the
/// running one (whose done is then never called), and a target destroyed
/// mid-way ends its transition without its done. Outside play mode, or for
/// no length, the end state comes at once and then done (the EditMode tests,
/// edit-mode builders). Reduced Motion cuts a slide (its end state, then
/// done); fades and waits keep their length.
/// </summary>
public static class Transit
{
    /// <summary>The kit's one length in seconds: MotionTuningSO.transitSeconds (0.25 s by default), kept within TransitCurve's 0.2-0.3 s.</summary>
    public static float Seconds => TransitCurve.KitSeconds(UiMotion.Tuning != null ? UiMotion.Tuning.transitSeconds : 0.25f);

    /// <summary>Fades <paramref name="group"/> to <paramref name="alpha"/> over <paramref name="seconds"/> (eased, real time), then <paramref name="done"/>; with no group, done at once.</summary>
    public static void Fade(CanvasGroup group, float alpha, float seconds, Action done = null)
    {
        if (group == null)
        {
            done?.Invoke();
            return;
        }
        if (TransitCurve.Instant(seconds, Application.isPlaying))
        {
            group.alpha = alpha;
            done?.Invoke();
            return;
        }
        float from = group.alpha;
        TransitRunner.Play(group, seconds, t => group.alpha = Mathf.LerpUnclamped(from, alpha, t), done);
    }

    /// <summary>Slides <paramref name="rect"/> to <paramref name="to"/> (its anchored position) over <paramref name="seconds"/> (eased, real time; cut under Reduced Motion), then <paramref name="done"/>; with no rect, done at once.</summary>
    public static void Slide(RectTransform rect, Vector2 to, float seconds, Action done = null)
    {
        if (rect == null)
        {
            done?.Invoke();
            return;
        }
        if (TransitCurve.Instant(seconds, Application.isPlaying) || MotionPreference.Reduced)
        {
            rect.anchoredPosition = to;
            done?.Invoke();
            return;
        }
        Vector2 from = rect.anchoredPosition;
        TransitRunner.Play(rect, seconds, t => rect.anchoredPosition = Vector2.LerpUnclamped(from, to, t), done);
    }

    /// <summary>Waits <paramref name="seconds"/> of real time, then <paramref name="done"/> (at once outside play mode).</summary>
    public static void Wait(float seconds, Action done)
    {
        if (TransitCurve.Instant(seconds, Application.isPlaying))
        {
            done?.Invoke();
            return;
        }
        TransitRunner.Play(null, seconds, null, done);
    }
}

/// <summary>
/// The scene change with its transition (owner FLOW; RUN-SAFE's RunManager
/// loads every scene through it; BUG-31a, BUG-32a, the bible's J5): the screen
/// fades through black (<see cref="Transit.Seconds"/>), the scene loads
/// asynchronously behind the black, and the black lifts once every loaded
/// scene (the office's gameplay layer comes on top of the art office) has
/// loaded and run its first frames, so a load never shows a frozen frame of
/// the scene being left nor a half-built one. Clicks are blocked while the
/// black is up. Outside play mode, or for a scene that cannot load, it is
/// SceneManager.LoadScene (its own error for a missing scene).
/// </summary>
public static class SceneTransit
{
    /// <summary>True from a Load's first frame of fade until the new scene has settled (Esc opens nothing meanwhile: PauseMenu).</summary>
    public static bool Loading => TransitRunner.SceneLoading;

    /// <summary>Loads <paramref name="scene"/> (by name, replacing every loaded scene) behind a fade through black. A second Load while one runs is ignored, with a warning.</summary>
    public static void Load(string scene)
    {
        if (!Application.isPlaying || !Application.CanStreamedLevelBeLoaded(scene))
        {
            SceneManager.LoadScene(scene);
            return;
        }
        TransitRunner.LoadScene(scene);
    }
}

/// <summary>
/// Plays Transit's transitions and SceneTransit's loads: a hidden persistent
/// object (made on first use in play mode) with one coroutine per target, and
/// the black veil of a scene change (an overlay canvas over everything, off
/// while unused). Real time (UiMotion.Delta: the capture's step while one
/// records), so hit-stop never stretches a transition.
/// </summary>
public sealed class TransitRunner : MonoBehaviour
{
    /// <summary>The veil's sorting order: above every scene canvas.</summary>
    private const int VeilOrder = 32000;

    /// <summary>The longest a load waits for the scenes it brings to finish loading (real seconds) before the veil lifts anyway.</summary>
    private const float SceneSettleTimeout = 10f;

    /// <summary>Frames the veil stays up after the scenes loaded (their Awake, Start and first layout).</summary>
    private const int FramesAfterLoad = 2;

    private static TransitRunner _runner;

    /// <summary>Each target's running transition (a new one of the same target replaces it).</summary>
    private readonly Dictionary<UnityEngine.Object, Coroutine> _running = new Dictionary<UnityEngine.Object, Coroutine>();

    /// <summary>The scene change's black veil (made on the first load).</summary>
    private CanvasGroup _veil;

    /// <summary>The scene being loaded (null when none).</summary>
    private string _loading;

    /// <summary>True while a scene change runs, until the new scene has settled (SceneTransit.Loading).</summary>
    public static bool SceneLoading => _runner != null && _runner._loading != null;

    /// <summary>Plays a transition of <paramref name="seconds"/> on <paramref name="target"/> (null: a plain wait), applying the eased share each frame, then <paramref name="done"/>.</summary>
    public static void Play(UnityEngine.Object target, float seconds, Action<float> apply, Action done)
    {
        TransitRunner runner = Runner();
        if (target != null && runner._running.TryGetValue(target, out Coroutine old) && old != null)
            runner.StopCoroutine(old);
        Coroutine routine = runner.StartCoroutine(runner.Run(target, seconds, apply, done));
        if (target != null)
            runner._running[target] = routine;
    }

    /// <summary>Starts a scene change to <paramref name="scene"/> (ignored, with a warning, while another runs).</summary>
    public static void LoadScene(string scene)
    {
        TransitRunner runner = Runner();
        if (runner._loading != null)
        {
            Debug.LogWarning($"[SceneTransit] '{scene}' was asked for while '{runner._loading}' is loading; the second load is ignored.");
            return;
        }
        runner._loading = scene;
        runner.StartCoroutine(runner.LoadRoutine(scene));
    }

    /// <summary>The runner, made on first use (hidden, kept across scenes).</summary>
    private static TransitRunner Runner()
    {
        if (_runner == null)
        {
            var host = new GameObject("Transit") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(host);
            _runner = host.AddComponent<TransitRunner>();
        }
        return _runner;
    }

    /// <summary>Steps one transition to its end (a destroyed target ends it without done).</summary>
    private IEnumerator Run(UnityEngine.Object target, float seconds, Action<float> apply, Action done)
    {
        bool hasTarget = !ReferenceEquals(target, null);
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            yield return null;
            if (hasTarget && target == null)
                yield break;
            elapsed += UiMotion.Delta(Time.unscaledDeltaTime);
            apply?.Invoke(TransitCurve.At(elapsed, seconds));
        }
        if (hasTarget)
            _running.Remove(target);
        done?.Invoke();
    }

    /// <summary>The scene change: the veil fades in, the scene loads behind it, every scene settles, the veil fades out.</summary>
    private IEnumerator LoadRoutine(string scene)
    {
        CanvasGroup veil = Veil();
        veil.gameObject.SetActive(true);
        veil.blocksRaycasts = true;
        float seconds = Transit.Seconds;
        yield return FadeVeil(veil, 1f, seconds);

        AsyncOperation load = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        if (load == null)
            SceneManager.LoadScene(scene);
        else
            while (!load.isDone)
                yield return null;

        float waited = 0f;
        while (AnySceneLoading() && waited < SceneSettleTimeout)
        {
            yield return null;
            waited += Time.unscaledDeltaTime;
        }
        for (int i = 0; i < FramesAfterLoad; i++)
            yield return null;

        _loading = null;
        yield return FadeVeil(veil, 0f, seconds);
        veil.blocksRaycasts = false;
        veil.gameObject.SetActive(false);
    }

    /// <summary>Fades the veil to <paramref name="alpha"/> over <paramref name="seconds"/> of real time, eased.</summary>
    private static IEnumerator FadeVeil(CanvasGroup veil, float alpha, float seconds)
    {
        float from = veil.alpha, elapsed = 0f;
        while (elapsed < seconds)
        {
            yield return null;
            elapsed += UiMotion.Delta(Time.unscaledDeltaTime);
            veil.alpha = Mathf.LerpUnclamped(from, alpha, TransitCurve.At(elapsed, seconds));
        }
        veil.alpha = alpha;
    }

    /// <summary>True while a loaded scene has not finished loading (the office's gameplay layer after the art office).</summary>
    private static bool AnySceneLoading()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (!SceneManager.GetSceneAt(i).isLoaded)
                return true;
        return false;
    }

    /// <summary>The black veil (an overlay canvas over everything that blocks clicks while up), made once.</summary>
    private CanvasGroup Veil()
    {
        if (_veil != null)
            return _veil;
        var root = new GameObject("TransitVeil", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = VeilOrder;
        var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
        black.transform.SetParent(root.transform, false);
        var rect = (RectTransform)black.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        black.GetComponent<Image>().color = Color.black;
        _veil = root.GetComponent<CanvasGroup>();
        _veil.alpha = 0f;
        _veil.blocksRaycasts = false;
        root.SetActive(false);
        return _veil;
    }

    /// <summary>Forgets the runner when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _runner = null;
}
