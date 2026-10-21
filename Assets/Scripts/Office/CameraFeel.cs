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
/// bound field of view). On top of it, DESK-HOMES' D6 framing focus (Focus:
/// FramingFocus picks the machine; CAM handed this seam over, "one lens
/// writer") narrows the reading view a little more, the two together never
/// past their cap, and turns it a little toward the machine (a
/// CinemachineRecomposer on the desk camera, applied after its body so the
/// impulses still add on top), on the same spring. The Motion intensity
/// scales every amplitude; Reduced Motion turns the breathing, the impulses,
/// the push-in and the focus off.
/// </summary>
public sealed class CameraFeel : MonoBehaviour, IMotionTick
{
    private static CameraFeel _current;

    private CinemachineCamera _office, _desk;
    private CinemachineImpulseSource _source;
    private DeskView _view;
    private CinemachineRecomposer _turn;
    private Spring _push, _focus, _pan, _tilt;
    private float _deskFov, _focusCap;

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

    /// <summary>
    /// DESK-HOMES' D6 framing focus (DESK-HOMES P3; CAM.md hands this seam
    /// over: one lens writer): the reading view narrows by
    /// <paramref name="share"/> of its bound field of view on top of its
    /// push-in, the two never past <paramref name="cap"/> of it, and turns
    /// toward <paramref name="aim"/> (shares of the frame from its centre, x
    /// right, y up), on the push-in's spring; a cut under Reduced Motion. The
    /// caller scales the share by the Motion intensity. Nothing without the
    /// office's cameras.
    /// </summary>
    public static void Focus(float share, Vector2 aim, float cap)
    {
        if (_current != null)
            _current.SetFocus(share, aim, cap);
    }

    /// <summary>The D6 framing focus lets go: the narrowing and the turn ease back out.</summary>
    public static void ReleaseFocus()
    {
        if (_current != null)
            _current.SetFocus(0f, Vector2.zero, _current._focusCap);
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
        _focus = Spring.At(0f);
        _pan = Spring.At(0f);
        _tilt = Spring.At(0f);
        if (!desk.TryGetComponent(out _turn))
        {
            // Set up as its Reset would (a runtime add skips Reset: a zoom scale of 0 would collapse the lens), applied after the body so the impulses compose on top.
            _turn = desk.gameObject.AddComponent<CinemachineRecomposer>();
            _turn.ApplyAfter = CinemachineCore.Stage.Body;
            _turn.ZoomScale = 1f;
            _turn.FollowAttachment = 1f;
            _turn.LookAtAttachment = 1f;
        }
        _turn.Pan = _turn.Tilt = 0f;
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

    /// <summary>The focus's targets: its narrowing within the cap (SlideFocus.Narrowing, against the push-in's target) and its turn (SlideFocus.Turn at the desk camera's aspect); a snap when motion is still.</summary>
    private void SetFocus(float share, Vector2 aim, float cap)
    {
        if (_desk == null)
            return;
        _focusCap = cap;
        _focus.Target = SlideFocus.Narrowing(_deskFov, _push.Target, share, cap);
        (float pan, float tilt) turn = share > 0f ? SlideFocus.Turn((aim.x, aim.y), _deskFov, _desk.Lens.Aspect) : (0f, 0f);
        _pan.Target = turn.pan;
        _tilt.Target = turn.tilt;
        if (UiMotion.Amount.Still)
        {
            _focus.Snap(_focus.Target);
            _pan.Snap(_pan.Target);
            _tilt.Snap(_tilt.Target);
        }
        Apply();
        UiMotion.Run(this);
    }

    /// <summary>Steps the push-in and the focus; false once they settled.</summary>
    public bool TickMotion(float dt)
    {
        if (this == null || _desk == null)
            return false;
        MotionKnobs knobs = UiMotion.Knobs;
        SpringTuning feel = knobs.Get(knobs.cameraFeel);
        bool moving = _push.Step(dt, feel, 1e-3f, 1e-2f);
        moving |= _focus.Step(dt, feel, 1e-3f, 1e-2f);
        moving |= _pan.Step(dt, feel, 1e-3f, 1e-2f);
        moving |= _tilt.Step(dt, feel, 1e-3f, 1e-2f);
        Apply();
        return moving;
    }

    /// <summary>The desk camera's field of view: the bound one, narrowed by the push-in and the focus (the focus never past its cap with the push-in); its turn toward the focused machine.</summary>
    private void Apply()
    {
        if (_desk == null)
            return;
        LensSettings lens = _desk.Lens;
        float narrow = _push.Value + Mathf.Max(0f, _focus.Value);
        if (_focusCap > 0f)
            narrow = Mathf.Min(narrow, Mathf.Max(_push.Value, _focusCap * _deskFov));
        lens.FieldOfView = _deskFov - narrow;
        _desk.Lens = lens;
        if (_turn != null)
        {
            _turn.Pan = _pan.Value;
            _turn.Tilt = _tilt.Value;
        }
    }
}
