using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// The game feel's hits (the desk machine spec §1-2: a dater's impression
/// lands, the gate lever thunks home): Hit(strength) freezes the game's time
/// for a moment (the hit-stop: MotionKnobs.hitStopSeconds × strength of
/// real time, Time.timeScale 0; the motions that should freeze with it read
/// Stopped) and kicks the cameras through Cinemachine's impulse (one
/// CinemachineImpulseSource on the director, a CinemachineImpulseListener
/// given to every Cinemachine camera loaded that lacks one at each hit:
/// the desk view's and the office's), MotionKnobs.stampShake × strength
/// metres, scaled by the player's Motion intensity; Reduced Motion keeps the
/// hit-stop and drops the shake. A hidden persistent object made on the
/// first hit. Shared: every hit of the game goes through here.
/// </summary>
public sealed class FeelDirector : MonoBehaviour
{
    /// <summary>The director (made on the first hit in play mode).</summary>
    private static FeelDirector _director;

    private CinemachineImpulseSource _impulse;
    private float _stopUntil;
    private bool _stopping;

    /// <summary>True while a hit-stop freezes the game's time (a motion stepped in real time holds still: StepDelta).</summary>
    public static bool Stopped => _director != null && _director._stopping;

    /// <summary>
    /// The game feel's frame time from <paramref name="dt"/> (a real-time
    /// frame: the daters and the lever step their springs by it): 0 while a
    /// hit-stop freezes the game; while frames are captured at a fixed rate
    /// (Time.captureDeltaTime, which Unity's unscaled time ignores) the
    /// capture's frame, so a recording plays at the true speed.
    /// </summary>
    public static float StepDelta(float dt) => Stopped ? 0f : Time.captureDeltaTime > 0f ? Time.captureDeltaTime : dt;

    /// <summary>The game feel's clock (seconds): real time, or the game's while frames are captured at a fixed rate (StepDelta).</summary>
    public static float Now => Time.captureDeltaTime > 0f ? Time.time : Time.unscaledTime;

    /// <summary>A hit of <paramref name="strength"/> (1: a dater's impression): the hit-stop and the camera impulse (nothing outside play mode).</summary>
    public static void Hit(float strength)
    {
        if (!Application.isPlaying || strength <= 0f)
            return;
        if (_director == null)
        {
            var host = new GameObject("FeelDirector") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(host);
            _director = host.AddComponent<FeelDirector>();
        }
        _director.Strike(strength);
    }

    private void Strike(float strength)
    {
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        float stop = knobs.hitStopSeconds * strength;
        if (stop > 0f)
        {
            _stopUntil = Mathf.Max(_stopUntil, Time.unscaledTime + stop);
            if (!_stopping)
                StartCoroutine(HitStop());
        }
        if (amount.Still || knobs.stampShake <= 0f)
            return;
        Listen();
        if (_impulse == null)
        {
            _impulse = gameObject.AddComponent<CinemachineImpulseSource>();
            _impulse.ImpulseDefinition.ImpulseChannel = 1;
            _impulse.ImpulseDefinition.ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Recoil;
            _impulse.ImpulseDefinition.ImpulseDuration = 0.18f;
            _impulse.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            _impulse.DefaultVelocity = Vector3.down;
        }
        _impulse.GenerateImpulseWithForce(knobs.stampShake * strength * amount.Share);
    }

    /// <summary>The time frozen until the hit-stop's end (in real time), then given back.</summary>
    private IEnumerator HitStop()
    {
        _stopping = true;
        float before = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        while (Time.unscaledTime < _stopUntil)
            yield return null;
        if (Time.timeScale == 0f)
            Time.timeScale = before;
        _stopping = false;
    }

    /// <summary>Every Cinemachine camera loaded has an impulse listener (the office's and the desk view's; a day's new office gets them at its first hit).</summary>
    private static void Listen()
    {
        foreach (CinemachineCamera cam in FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (cam.GetComponent<CinemachineImpulseListener>() == null)
            {
                CinemachineImpulseListener listener = cam.gameObject.AddComponent<CinemachineImpulseListener>();
                listener.ChannelMask = 1;
            }
    }

    private void OnDisable()
    {
        if (_stopping && Time.timeScale == 0f)
            Time.timeScale = 1f;
        _stopping = false;
    }

    private void OnDestroy()
    {
        if (_director == this)
            _director = null;
    }

    /// <summary>Forgets the director when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _director = null;
}
