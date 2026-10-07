using System.Collections.Generic;
using UnityEngine;

/// <summary>Something the motion driver steps each frame while it moves (a control's springs, a popup's, a pill's, a paper's).</summary>
public interface IMotionTick
{
    /// <summary>Advances the motion by <paramref name="dt"/> unscaled seconds and applies it; false once it has settled (the driver drops it).</summary>
    bool TickMotion(float dt);
}

/// <summary>
/// The game feel's one driver and its settings: a hidden persistent object
/// that steps only the motions that move (Run registers one; it leaves the
/// list when it settles, so an idle control costs nothing per frame), with
/// unscaled time (or the capture step while one records: Time.captureDeltaTime); and the tuning (RunConfig's MotionTuningSO, else the
/// defaults with a warning) and the player's amount of motion
/// (MotionPreference: the Motion intensity, Reduced Motion). No per-frame
/// allocation: the list is reused and swapped down in place.
/// </summary>
public sealed class UiMotion : MonoBehaviour
{
    /// <summary>The driver (made on the first Run in play mode).</summary>
    private static UiMotion _driver;

    /// <summary>The tuning asset and its knobs once read.</summary>
    private static MotionTuningSO _tuning;
    private static MotionKnobs _knobs;

    /// <summary>The motions that move.</summary>
    private readonly List<IMotionTick> _running = new List<IMotionTick>(64);

    /// <summary>The motion's tuning asset (RunConfig.motionTuning), or null (a warning once: the defaults play).</summary>
    public static MotionTuningSO Tuning
    {
        get
        {
            if (_knobs == null)
                Load();
            return _tuning;
        }
    }

    /// <summary>The motion's knobs (the tuning asset's; the defaults when it is not wired).</summary>
    public static MotionKnobs Knobs
    {
        get
        {
            if (_knobs == null)
                Load();
            return _knobs;
        }
    }

    /// <summary>Reads the tuning from RunConfig once.</summary>
    private static void Load()
    {
        var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        _tuning = config != null ? config.motionTuning : null;
        if (_tuning != null)
            _knobs = _tuning.knobs;
        else
        {
            Debug.LogWarning("[UiMotion] RunConfig has no MotionTuningSO, so the game feel uses the default springs. Run Tools > TimeDesk > Build Office UI (it creates and assigns MotionTuning_Default).");
            _knobs = new MotionKnobs();
        }
    }

    /// <summary>How much of each motion plays now (the player's Motion intensity and Reduced Motion).</summary>
    public static MotionAmount Amount => new MotionAmount(MotionPreference.Intensity, MotionPreference.Reduced);

    /// <summary>
    /// The shape of a motion of fixed length (<paramref name="seconds"/>; a
    /// paper's slide, the stamp bar, the desk camera's blend) at
    /// <paramref name="t"/> (0..1): <paramref name="feel"/>'s spring curve
    /// (SpringCurve), toward the critically damped one (no overshoot) as the
    /// Motion intensity falls, that one under Reduced Motion. 0 at the start,
    /// exactly 1 at the end, so the length a rule counts on stays.
    /// </summary>
    public static float Ease(float t, MotionFeel feel, float seconds)
    {
        SpringTuning tuning = Knobs.Get(feel);
        float calm = SpringCurve.Ease(t, SpringTuning.Critical(tuning.stiffness, tuning.mass), seconds);
        float share = Amount.Share;
        return share <= 0f ? calm : calm + (SpringCurve.Ease(t, tuning, seconds) - calm) * share;
    }

    /// <summary>Steps <paramref name="motion"/> every frame until it settles (nothing outside play mode; a motion already running is not added twice).</summary>
    public static void Run(IMotionTick motion)
    {
        if (motion == null || !Application.isPlaying)
            return;
        if (_driver == null)
        {
            var host = new GameObject("UiMotion") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(host);
            _driver = host.AddComponent<UiMotion>();
        }
        if (!_driver._running.Contains(motion))
            _driver._running.Add(motion);
    }

    /// <summary>True while <paramref name="motion"/> is being stepped (the probes wait for it).</summary>
    public static bool IsRunning(IMotionTick motion) => _driver != null && motion != null && _driver._running.Contains(motion);

    /// <summary>How many motions move now (the probes wait for 0; the performance check reads it).</summary>
    public static int RunningCount => _driver != null ? _driver._running.Count : 0;

    /// <summary>Steps every moving motion; drops those that settled or were destroyed (swapped down in place).</summary>
    private void Update()
    {
        // Recording at a fixed frame rate (Time.captureDeltaTime: a capture, a trailer) steps the springs by it too, so every recorded frame is one step of the motion.
        float dt = Time.captureDeltaTime > 0f ? Time.captureDeltaTime : Time.unscaledDeltaTime;
        for (int i = _running.Count - 1; i >= 0; i--)
        {
            IMotionTick motion = _running[i];
            bool alive = !(motion is Object unityObject) || unityObject != null;
            if (alive && motion.TickMotion(dt))
                continue;
            int last = _running.Count - 1;
            _running[i] = _running[last];
            _running.RemoveAt(last);
        }
    }

    private void OnDestroy()
    {
        if (_driver == this)
            _driver = null;
    }

    /// <summary>Forgets the tuning when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _knobs = null;
        _tuning = null;
        _driver = null;
    }
}
