using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
/// The scene change with its transition (owner FLOW; BUG-31a, BUG-32a, the
/// bible's J5): the booth's roll shutter comes down over the screen
/// (TransitShutter, in <see cref="Transit.Seconds"/>), the scene loads
/// asynchronously behind it (the office's gameplay layer too: OfficeScenes),
/// and the shutter goes up once every loaded scene has loaded and run its
/// first frames, so a load never shows a frozen frame of the scene being
/// left nor a half-built one; while it is down the light moves through its
/// slats, so no frame is held without motion. Clicks are blocked while it is
/// down. Outside play mode, or for a scene that cannot load, it is
/// SceneManager.LoadScene (its own error for a missing scene). RunManager's
/// loads (the office, Home, the Title) all come through it (RUN-SAFE).
/// The shutter's light moves on real time, but a long frame on the main
/// thread (a scene's activation, GameManager.Start making the day's cases)
/// still holds the screen for that frame: the QA rig's real-time load watch
/// (FlowLoadWatch, flow_load) measures the worst frame of each load against
/// 250 ms, from the transit events marked here (QaSignals "transit":
/// start, rise, up).
/// </summary>
public static class SceneTransit
{
    /// <summary>True from the frame a Load starts lowering the shutter until the new scene has settled (Esc opens nothing meanwhile: PauseMenu). It clears before the shutter rises; <see cref="Changing"/> holds until the shutter is off.</summary>
    public static bool Loading => TransitRunner.SceneLoading;

    /// <summary>
    /// True for the whole scene change: from the frame a Load is accepted
    /// until the shutter is off again, a load queued behind it included
    /// (<see cref="Loading"/> clears before the rise). The scene being left
    /// stays loaded under the shutter for the lowering and the load, while
    /// RunManager's world has already moved on, so the QA rig waits on this
    /// after a cheat that reloads the office: a wait on the office's day and
    /// briefing alone passes on the office being replaced.
    /// </summary>
    public static bool Changing => TransitRunner.SceneChanging;

    /// <summary>Loads <paramref name="scene"/> (by name, replacing every loaded scene) behind the booth's roll shutter (TransitShutter). A Load while another scene change runs is never dropped: it loads next behind the same shutter, and the latest request wins (RunManager has already moved its world for it).</summary>
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
/// the veil of a scene change (TransitShutter, an overlay canvas over
/// everything, off while unused). Real time (UiMotion.Delta: the capture's
/// step while one records), so hit-stop never stretches a transition.
/// </summary>
public sealed class TransitRunner : MonoBehaviour
{
    /// <summary>The veil's sorting order: above every scene canvas, and one under RUN-SAFE's SaveNotice (32000), so a failed-save strip raised just before a load (GoHomeOrAdvance's save, the morning's, an ending's) stays readable over the shutter.</summary>
    private const int VeilOrder = 31999;

    /// <summary>The longest a load waits for the scenes it brings to finish loading (real seconds) before the veil lifts anyway.</summary>
    private const float SceneSettleTimeout = 10f;

    /// <summary>Frames the veil stays up after the scenes loaded (their Awake, Start and first layout).</summary>
    private const int FramesAfterLoad = 2;

    /// <summary>
    /// The QA channel of a scene change's moments (QaSignals.Event, free when
    /// nothing listens): "start:&lt;scene&gt;" as the load is accepted (the
    /// click's frame), "rise:&lt;scene&gt;" as the shutter starts up over the
    /// settled scene, "up:&lt;scene&gt;" once it is off. FLOW's load watch and
    /// the recorder's events.tsv read them.
    /// </summary>
    public const string TransitSignal = "transit";

    private static TransitRunner _runner;

    /// <summary>Each target's running transition (a new one of the same target replaces it).</summary>
    private readonly Dictionary<UnityEngine.Object, Coroutine> _running = new Dictionary<UnityEngine.Object, Coroutine>();

    /// <summary>The scene change's veil, the drawn booth shutter (made on the first load).</summary>
    private TransitShutter _veil;

    /// <summary>The scene being loaded (null when none, and while the shutter rises).</summary>
    private string _loading;

    /// <summary>The scene changes, one at a time: a request during one waits, the latest winning (SceneChangeQueue).</summary>
    private readonly SceneChangeQueue _scenes = new SceneChangeQueue();

    /// <summary>True while a scene change runs, until the new scene has settled (SceneTransit.Loading).</summary>
    public static bool SceneLoading => _runner != null && _runner._loading != null;

    /// <summary>True while a scene change runs, until the shutter is off (SceneTransit.Changing).</summary>
    public static bool SceneChanging => _runner != null && _runner._scenes.Changing;

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

    /// <summary>
    /// Starts a scene change to <paramref name="scene"/>. While another runs,
    /// the request is queued instead (a later one replaces it): RunManager has
    /// already moved its world for it (a second cheat while the first one's
    /// office loads), so the scene asked for last is the one that must end up
    /// loaded. It loads behind the same shutter, or the shutter comes down
    /// again for it when it was already rising.
    /// </summary>
    public static void LoadScene(string scene)
    {
        TransitRunner runner = Runner();
        string waiting = runner._scenes.Queued;
        if (!runner._scenes.Request(scene))
        {
            Debug.Log($"[SceneTransit] '{scene}' was asked for during a scene change; it loads next, behind the same shutter{(waiting != null ? $" (instead of '{waiting}', asked for earlier)" : string.Empty)}.");
            return;
        }
        runner._loading = scene;
        QaSignals.Event(TransitSignal, "start:" + scene);
        runner.StartCoroutine(runner.LoadRoutine(scene));
    }

    /// <summary>Takes the waiting request, if any, into <paramref name="scene"/> (marked as a new start); false when none waits.</summary>
    private bool TakeQueued(ref string scene)
    {
        if (!_scenes.TakeQueued(ref scene))
            return false;
        _loading = scene;
        QaSignals.Event(TransitSignal, "start:" + scene);
        return true;
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

    /// <summary>
    /// The scene change: the shutter comes down, the scene loads behind it,
    /// every scene settles, the shutter goes up. A request queued meanwhile
    /// (LoadScene) is loaded before the shutter rises (one asked for while it
    /// came down replaces the scene before any load), and one asked for while
    /// it rose brings it down again.
    /// </summary>
    private IEnumerator LoadRoutine(string scene)
    {
        if (_veil == null)
            _veil = TransitShutter.Build(transform, VeilOrder);
        float seconds = Transit.Seconds;
        while (scene != null)
        {
            yield return _veil.Lower(seconds);
            TakeQueued(ref scene);
            do
            {
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
            }
            while (TakeQueued(ref scene));

            _loading = null;
            QaSignals.Event(TransitSignal, "rise:" + scene);
            yield return _veil.Raise(seconds);
            QaSignals.Event(TransitSignal, "up:" + scene);
            if (!TakeQueued(ref scene))
                scene = null;
        }
        _scenes.Finish();
    }

    /// <summary>True while a loaded scene has not finished loading (the office's gameplay layer after the art office).</summary>
    private static bool AnySceneLoading()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (!SceneManager.GetSceneAt(i).isLoaded)
                return true;
        return false;
    }

    /// <summary>Forgets the runner when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _runner = null;
}
