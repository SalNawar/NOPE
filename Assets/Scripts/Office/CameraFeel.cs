using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// The office cameras' feel through Cinemachine (Saleh 2026-10-07: the
/// camera through Cinemachine, built on the desk view's cameras, no second
/// camera system): DeskView.Bind attaches it with the art office's camera and
/// the desk camera. Both breathe at idle (a CinemachineBasicMultiChannelPerlin
/// with MotionTuningSO.breathingNoise, very subtle: breathingAmplitude /
/// breathingFrequency) and both listen for impulses (a
/// CinemachineImpulseListener each); Shake fires this object's
/// CinemachineImpulseSource (uniform, a bump, or a rumble for the river's
/// breach), on unscaled time so a hit-stop never freezes it. While a paper
/// is read the desk camera pushes in softly (its field of view narrows by
/// readingPushIn on the Heavy spring; DeskView's own view geometry keeps the
/// bound field of view). The Motion intensity scales every amplitude;
/// Reduced Motion turns the breathing, the impulses and the push-in off.
/// </summary>
public sealed class CameraFeel : MonoBehaviour, IMotionTick
{
    private static CameraFeel _current;

    private CinemachineCamera _office, _desk;
    private CinemachineImpulseSource _source;
    private DeskView _view;
    private Spring _push;
    private float _deskFov;

    /// <summary>
    /// Gives the desk view's object the cameras' feel: the impulse source, a
    /// listener and the breathing noise on <paramref name="office"/> and
    /// <paramref name="desk"/>, and the push-in while <paramref name="view"/>
    /// is on. Idempotent (a rebind re-reads the desk camera's field of view).
    /// </summary>
    public static void Attach(DeskView view, CinemachineCamera office, CinemachineCamera desk)
    {
        if (view == null || desk == null)
            return;
        if (!view.TryGetComponent(out CameraFeel feel))
            feel = view.gameObject.AddComponent<CameraFeel>();
        feel.Bind(view, office, desk);
    }

    /// <summary>A camera impulse of <paramref name="force"/> over <paramref name="seconds"/> (a rumble when <paramref name="rumble"/>), scaled by the Motion intensity; none under Reduced Motion or without the office's cameras.</summary>
    public static void Shake(float force, float seconds, bool rumble)
    {
        MotionAmount amount = UiMotion.Amount;
        if (_current == null || _current._source == null || amount.Still || force <= 0f)
            return;
        CinemachineImpulseDefinition definition = _current._source.ImpulseDefinition;
        definition.ImpulseShape = rumble ? CinemachineImpulseDefinition.ImpulseShapes.Rumble : CinemachineImpulseDefinition.ImpulseShapes.Bump;
        definition.ImpulseDuration = Mathf.Max(0.01f, seconds);
        CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
        _current._source.GenerateImpulseWithVelocity(new Vector3(0.3f, -1f, 0f) * (force * amount.Share));
    }

    private void Bind(DeskView view, CinemachineCamera office, CinemachineCamera desk)
    {
        _current = this;
        if (_view != view)
        {
            if (_view != null)
                _view.Changed -= HandleViewChanged;
            _view = view;
            _view.Changed += HandleViewChanged;
        }
        _office = office;
        _desk = desk;
        _deskFov = desk.Lens.FieldOfView;
        _push = Spring.At(0f);
        if (_source == null && !TryGetComponent(out _source))
        {
            _source = gameObject.AddComponent<CinemachineImpulseSource>();
            _source.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            _source.ImpulseDefinition.ImpulseChannel = 1;
        }
        Listen(office);
        Listen(desk);
        Breathe();
    }

    private void OnEnable() => MotionPreference.Changed += Breathe;

    private void OnDisable() => MotionPreference.Changed -= Breathe;

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

    /// <summary>The desk view turned on (a paper raised to read, the stamps) or off: the push-in springs in or back out.</summary>
    private void HandleViewChanged()
    {
        MotionKnobs knobs = UiMotion.Knobs;
        MotionAmount amount = UiMotion.Amount;
        float target = _view.IsOn ? knobs.readingPushIn * amount.Share : 0f;
        if (amount.Still)
            _push.Snap(target);
        else
            _push.Target = target;
        Apply();
        UiMotion.Run(this);
    }

    /// <summary>Steps the push-in; false once it settled.</summary>
    public bool TickMotion(float dt)
    {
        if (this == null || _desk == null)
            return false;
        MotionKnobs knobs = UiMotion.Knobs;
        bool moving = _push.Step(dt, knobs.Get(knobs.cameraFeel), 1e-3f, 1e-2f);
        Apply();
        return moving;
    }

    /// <summary>The desk camera's field of view: the bound one, narrowed by the push-in.</summary>
    private void Apply()
    {
        if (_desk == null)
            return;
        LensSettings lens = _desk.Lens;
        lens.FieldOfView = _deskFov - _push.Value;
        _desk.Lens = lens;
    }
}
