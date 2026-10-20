using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// The office cameras' feel through Cinemachine (Saleh 2026-10-07: the
/// camera through Cinemachine, built on the reading view's cameras, no second
/// camera system): DeskView.Bind attaches it with the art office's camera and
/// the reading camera. Both breathe at idle (a CinemachineBasicMultiChannelPerlin
/// with MotionTuningSO.breathingNoise, very subtle: breathingAmplitude /
/// breathingFrequency) only while the player turned the camera sway on, and
/// both listen for impulses (a CinemachineImpulseListener each).
/// <para>
/// Shake draws the game feel's shake table (CameraShakeTable,
/// GAME_FEEL_BIBLE §1.3; CAM owns every shake cell): the caller's force picks
/// the row, and the row sets the size (at most its cap in pixels at 1080p,
/// with headroom), the length and the direction (a stamp bumps down, a
/// breach rumbles), on unscaled time so a hit-stop never freezes it; each
/// shake is tagged for the QA camera logger as an impulse with its cap.
/// </para>
/// <para>
/// As the reading view comes in, its lens pushes in softly to the solved
/// reading pose (DeskView.ReadingFov: it starts readingPushIn wider and
/// narrows on the Heavy spring), tagged with the view's cause while it moves.
/// The Motion intensity scales every amplitude, live; Reduced Motion turns
/// the breathing, the impulses and the push-in off, live.
/// </para>
/// </summary>
public sealed class CameraFeel : MonoBehaviour, IMotionTick
{
    /// <summary>A shake's force for a camera move of one metre: the impulse's Bump and Rumble shapes peak at about their velocity (calibrated by the CAM recipe's impulse caps; QaCameraMoves measures).</summary>
    private const float ShapePeak = 1f;

    /// <summary>How long each re-tag of a moving push-in covers (unscaled seconds): it is renewed before it runs out while the spring moves.</summary>
    private const float PushTagSeconds = 0.25f;

    private static CameraFeel _current;

    private CinemachineCamera _office, _desk;
    private CinemachineImpulseSource _source;
    private DeskView _view;
    private Spring _push;
    private float _pushIn;
    private float _taggedUntil;

    /// <summary>
    /// Gives the reading view's object the cameras' feel: the impulse source,
    /// a listener and the breathing noise on <paramref name="office"/> and
    /// <paramref name="desk"/>, and the push-in while <paramref name="view"/>
    /// is on. Idempotent: a rebind (the screen's aspect changed) re-reads the
    /// reading lens and keeps the push where it is.
    /// </summary>
    public static void Attach(DeskView view, CinemachineCamera office, CinemachineCamera desk)
    {
        if (view == null || desk == null)
            return;
        if (!view.TryGetComponent(out CameraFeel feel))
            feel = view.gameObject.AddComponent<CameraFeel>();
        feel.Bind(view, office, desk);
    }

    /// <summary>
    /// A camera shake asked for with <paramref name="force"/> over
    /// <paramref name="seconds"/> (a rumble when <paramref name="rumble"/>):
    /// the shake table's row the force picks against the MotionKnobs shake
    /// knobs (CameraShakeTable.Pick) is drawn, its length, direction and size
    /// (its cap less headroom, at the live camera's lens), scaled by the
    /// Motion intensity and tagged as an impulse for the QA camera logger;
    /// none under Reduced Motion or without the office's cameras.
    /// </summary>
    public static void Shake(float force, float seconds, bool rumble)
    {
        MotionAmount amount = UiMotion.Amount;
        if (_current == null || _current._source == null || amount.Still || force <= 0f)
            return;
        MotionKnobs knobs = UiMotion.Knobs;
        CameraShakeRow row = CameraShakeTable.Pick(force, rumble, knobs.shakeStamp, knobs.shakeCitation, knobs.shakeBreach);
        Camera live = Camera.main;
        float fov = live != null ? live.fieldOfView : _current._office != null ? _current._office.Lens.FieldOfView : 60f;
        float metres = CameraShakeTable.Metres(row.CapPx * CameraShakeTable.Headroom * amount.Share, fov) / ShapePeak;
        CinemachineImpulseDefinition definition = _current._source.ImpulseDefinition;
        definition.ImpulseShape = row.Rumble ? CinemachineImpulseDefinition.ImpulseShapes.Rumble : CinemachineImpulseDefinition.ImpulseShapes.Bump;
        definition.ImpulseDuration = Mathf.Max(0.01f, row.Seconds);
        CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
        QaSignals.TagCamera(CameraMoveCause.Impulse, row.Seconds, row.CapPx);
        _current._source.GenerateImpulseWithVelocity(new Vector3(row.X, row.Y, 0f) * metres);
    }

    private void Bind(DeskView view, CinemachineCamera office, CinemachineCamera desk)
    {
        _current = this;
        bool rebind = _view == view && _desk == desk;
        if (_view != view)
        {
            if (_view != null)
                _view.Changed -= HandleViewChanged;
            _view = view;
            _view.Changed += HandleViewChanged;
        }
        _office = office;
        _desk = desk;
        if (!rebind)
        {
            _push = Spring.At(0f);
            _pushIn = 0f;
        }
        if (_source == null && !TryGetComponent(out _source))
        {
            _source = gameObject.AddComponent<CinemachineImpulseSource>();
            _source.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            _source.ImpulseDefinition.ImpulseChannel = 1;
        }
        Listen(office);
        Listen(desk);
        Breathe();
        Apply();
    }

    private void OnEnable() => MotionPreference.Changed += HandleMotionChanged;

    private void OnDisable() => MotionPreference.Changed -= HandleMotionChanged;

    private void OnDestroy()
    {
        if (_view != null)
            _view.Changed -= HandleViewChanged;
        if (_current == this)
            _current = null;
    }

    /// <summary>A listener on <paramref name="cam"/> (added once; set up as its Reset would: channel 1, gain 1, camera space).</summary>
    private static void Listen(CinemachineCamera cam)
    {
        if (cam == null || cam.TryGetComponent(out CinemachineImpulseListener _))
            return;
        CinemachineImpulseListener listener = cam.gameObject.AddComponent<CinemachineImpulseListener>();
        listener.ChannelMask = 1;
        listener.Gain = 1f;
        listener.UseCameraSpace = true;
        listener.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction { AmplitudeGain = 1f, FrequencyGain = 1f, Duration = 1f };
    }

    /// <summary>The motion settings changed: the breathing follows, and under Reduced Motion (or no intensity) the push-in settles at once (BUG-64b: every camera motion follows the settings live).</summary>
    private void HandleMotionChanged()
    {
        Breathe();
        if (_view == null || !UiMotion.Amount.Still)
            return;
        _pushIn = 0f;
        _push.Snap(0f);
        Apply();
    }

    /// <summary>The idle breathing on both cameras at the Motion intensity: only while the player turned the camera sway on in Settings (MotionPreference.CameraSway, off by default: it made Saleh motion sick), never under Reduced Motion or without a noise profile.</summary>
    private void Breathe()
    {
        MotionTuningSO tuning = UiMotion.Tuning;
        MotionKnobs knobs = UiMotion.Knobs;
        float share = MotionPreference.CameraSway && !MotionPreference.Reduced ? UiMotion.Amount.Share : 0f;
        foreach (CinemachineCamera cam in new[] { _office, _desk })
        {
            if (cam == null)
                continue;
            if (!cam.TryGetComponent(out CinemachineBasicMultiChannelPerlin noise))
            {
                if (tuning == null || tuning.breathingNoise == null)
                    continue;
                noise = cam.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
            }
            if (tuning != null && tuning.breathingNoise != null)
                noise.NoiseProfile = tuning.breathingNoise;
            noise.AmplitudeGain = knobs.breathingAmplitude * share;
            noise.FrequencyGain = knobs.breathingFrequency;
            noise.enabled = share > 0f;
        }
    }

    /// <summary>The reading view turned on (a paper dropped, a view key) or off: the push-in springs in to the reading pose, or back out, tagged with the view's cause while it moves.</summary>
    private void HandleViewChanged()
    {
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        if (_view.IsOn)
            _pushIn = amount.Still ? 0f : knobs.readingPushIn * amount.Share;
        float target = _view.IsOn ? _pushIn : 0f;
        if (amount.Still)
            _push.Snap(target);
        else
            _push.Target = target;
        _taggedUntil = 0f;
        Apply();
        UiMotion.Run(this);
    }

    /// <summary>Steps the push-in (tagging it while it moves); false once it settled.</summary>
    public bool TickMotion(float dt)
    {
        if (this == null || _desk == null)
            return false;
        MotionKnobs knobs = UiMotion.Knobs;
        bool moving = _push.Step(dt, knobs.Get(knobs.cameraFeel), 1e-3f, 1e-2f);
        if (moving && Time.unscaledTime >= _taggedUntil - 0.05f)
        {
            QaSignals.TagCamera(_view != null ? _view.LastCause : CameraMoveCause.ViewKey, PushTagSeconds);
            _taggedUntil = Time.unscaledTime + PushTagSeconds;
        }
        Apply();
        return moving;
    }

    /// <summary>The reading camera's field of view: the solved reading lens, wider by what is left of the push-in.</summary>
    private void Apply()
    {
        if (_desk == null || _view == null)
            return;
        LensSettings lens = _desk.Lens;
        lens.FieldOfView = _view.ReadingFov + _pushIn - _push.Value;
        _desk.Lens = lens;
    }
}
