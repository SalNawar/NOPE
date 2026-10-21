using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// The UV torch (the desk machines spec §1; Saleh 2026-10-17: "uv light should
/// be an actual uv flashlight you can use to see the hidden watermarks and
/// symbols"): a handheld blacklight resting in its cradle on the desk. The
/// pointer over it tips its lens up out of the cradle (GAME_FEEL_BIBLE C3).
/// Press on it and it lifts out with its thumb switch's click and switches
/// on; drag and it follows the pointer, held by its grip (Saleh 2026-10-18:
/// "when you hold the UV the mouse should be on the base not the light"),
/// trailing it by at most 8 mm and tracking it every frame (a camera blend
/// included: BUG-55); its violet cone lights the spot ahead of the grip on the
/// desk (UvCone.Ahead; a spot light and the BeamFX cone), with a soft hum and
/// a slight flicker; the reveal follows the cone, not the pointer. The hold
/// is chosen for the camera (UvTorchHold.Hold, BUG-2c): the body never covers
/// the patch it lights. The lit pool and the reveal show only where the cone
/// meets the desk (DeskSurface: never floating past its edge), and the
/// revealed marks fluoresce in over 120 ms. Let go and it clicks off and
/// springs back to its cradle. No toggle, no second step (Papers, Please's one
/// hand: while it is held, no paper moves). While held it is the desk's hold
/// (HoldRegistry, BUG-55b): a cancel (Esc, right-click, focus loss: the
/// control model's) sends it back as a release does. Every frame it sets the
/// cone the papers' hidden layers are revealed in (the TimeDesk/UvReveal
/// shaders' _UvCone and _UvConeOn) and lets each paper count its lit marks
/// (DeskHardware.TrackCone). Its parts are the UvTorch art's (Build Office UI
/// binds them; its click box hugs the torch, BUG-4b); the office binder lays
/// it on the desk.
/// </summary>
public sealed class UvTorch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler,
                             IPointerEnterHandler, IPointerExitHandler, IHoldable
{
    /// <summary>The shader globals the reveal reads: the cone (centre xyz, radius w) and how lit it is (0 off .. 1 on, fluorescing in).</summary>
    public static readonly int ConeId = Shader.PropertyToID("_UvCone"), ConeOnId = Shader.PropertyToID("_UvConeOn");

    /// <summary>The torch's body (the art's UvTorch: it lifts out of the cradle; its origin the grip).</summary>
    [SerializeField] private Transform torch;

    /// <summary>The thumb slide (it moves toward the lens when on).</summary>
    [SerializeField] private Transform switchKnob;

    /// <summary>The lens face (it glows while on).</summary>
    [SerializeField] private Renderer lens;

    /// <summary>The visible violet cone from the lens (on while lit).</summary>
    [SerializeField] private GameObject beam;

    /// <summary>The violet spot light the lens throws on the desk (on while lit).</summary>
    [SerializeField] private Light spot;

    /// <summary>The cone's pool of violet light on the desk (a flat additive disc laid over the papers under the cone, shown only while lit on the desk: the papers read unlit in the reading view, so the light is drawn too).</summary>
    [SerializeField] private Transform pool;

    /// <summary>The hum's looping source and the switch's one-shots.</summary>
    [SerializeField] private AudioSource hum, sound;

    /// <summary>The lens centre in the torch's own space (where the cone starts), and the slide's travel when on (in the torch's own space: toward the lens and 1 mm down, the click).</summary>
    [SerializeField] private Vector3 lensLocal = new Vector3(0f, 0f, -0.1285f), switchTravel = new Vector3(0f, -0.001f, -0.01f);

    /// <summary>The lens's glow when on (its colour's multiplier) and off.</summary>
    [SerializeField] private Color lensOn = new Color(1.6f, 1.3f, 2.2f, 1f), lensOff = Color.white;

    /// <summary>How high over the desk the cone's pool lies (metres): over a stack of papers, under a carried one.</summary>
    private const float PoolLift = 0.012f;

    private DeskHardware _hardware;
    private DeskSurface _surface;
    private bool _surfaceSought;
    private Vector3 _restPos, _switchRest;
    private Quaternion _restRot;
    private bool _held, _rested, _hovered, _poolShown;
    private Spring _lift, _tip;
    private Vector3 _grip, _pointerGrip, _aim, _lit;
    private Camera _camera;
    private MaterialPropertyBlock _block;
    private float _spotIntensity = 1f, _glow, _tilt, _yaw;
    private float _tailLength = 0.066f, _lensLength = 0.1285f, _bodyRadius = 0.0245f;
    private TorchAim _solved;
    private bool _hasSolve;
    private Vector3 _solvedGrip, _solvedEye, _solvedAway;

    /// <summary>True while the torch is held (switched on).</summary>
    public bool Held => _held;

    /// <summary>The spot the cone lights on the desk (while held): ahead of the grip, with the cone's soft lag.</summary>
    public Vector3 Aim => _lit;

    /// <summary>Wires it to its machines' hub (DeskHardware).</summary>
    public void Bind(DeskHardware hardware)
    {
        _hardware = hardware;
        Rest();
    }

    private void Rest()
    {
        if (_rested || torch == null)
            return;
        _rested = true;
        _restPos = torch.localPosition;
        _restRot = torch.localRotation;
        if (switchKnob != null)
            _switchRest = switchKnob.localPosition;
        if (spot != null)
            _spotIntensity = spot.intensity;
        MeasureBody();
        Switch(false, true);
    }

    /// <summary>The body's capsule from its own meshes (the beam's left out), in the torch's space scaled to the world: the lens end ahead of the grip (-Z), the tail cap behind it, the widest radius round the axis.</summary>
    private void MeasureBody()
    {
        float front = 0f, back = 0f, radius = 0f;
        bool any = false;
        foreach (MeshFilter f in torch.GetComponentsInChildren<MeshFilter>(true))
        {
            if (f.sharedMesh == null || (beam != null && f.transform.IsChildOf(beam.transform)))
                continue;
            Bounds b = f.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                Vector3 p = torch.InverseTransformPoint(f.transform.TransformPoint(corner));
                front = Mathf.Max(front, -p.z);
                back = Mathf.Max(back, p.z);
                radius = Mathf.Max(radius, Mathf.Abs(p.x), Mathf.Abs(p.y));
                any = true;
            }
        }
        float scale = Mathf.Abs(torch.lossyScale.x);
        if (!any || scale <= 0f)
            return;
        _lensLength = Mathf.Max(front, -lensLocal.z) * scale;
        _tailLength = back * scale;
        _bodyRadius = radius * scale;
    }

    /// <summary>The pointer comes over it at rest: it tips up out of its cradle.</summary>
    public void OnPointerEnter(PointerEventData eventData) => _hovered = true;

    /// <summary>The pointer leaves it: it settles back.</summary>
    public void OnPointerExit(PointerEventData eventData) => _hovered = false;

    /// <summary>Pressed: it lifts out by its grip under the pointer and switches on, lighting the spot ahead; it becomes the desk's hold.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || torch == null)
            return;
        Rest();
        _camera = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        _pointerGrip = Project(eventData.position, out Vector3 grip) ? grip : new Vector3(transform.position.x, DeskY + HoldHeight, transform.position.z);
        _grip = _pointerGrip;
        _hasSolve = false;
        AimBeam(true);
        _lit = _aim;
        Switch(true, false);
        HoldRegistry.Desk.Begin(this);
    }

    /// <summary>A drag begins (it is already held).</summary>
    public void OnBeginDrag(PointerEventData eventData) { }

    /// <summary>Dragged: the grip follows the pointer over the desk (Update also follows it every frame, a camera blend included).</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_held && Project(eventData.position, out Vector3 grip))
            _pointerGrip = grip;
    }

    /// <summary>Let go (a drag's end or a plain release): it clicks off and goes back to its cradle.</summary>
    public void OnEndDrag(PointerEventData eventData) => Release();

    /// <inheritdoc cref="OnEndDrag"/>
    public void OnPointerUp(PointerEventData eventData) => Release();

    /// <summary>The hold is cancelled (HoldRegistry: Esc, right-click, a lost focus, another hold): it goes back as on a release.</summary>
    public void CancelHold() => Release();

    /// <summary>Switches off and sends it back to its cradle (also at a case's end); the desk's hold ends.</summary>
    public void Release()
    {
        if (!_held)
            return;
        Switch(false, false);
        HoldRegistry.Desk.End(this);
    }

    /// <summary>How high over the desk the pointer holds the grip (DeskConfigSO.uvHoldHeight).</summary>
    private float HoldHeight => _hardware != null && _hardware.Config != null ? _hardware.Config.uvHoldHeight : 0.14f;

    /// <summary>The built tilt of the held lens below the level (DeskConfigSO.uvHoldTilt, degrees); the hold leaves it only when the camera would see the body over the patch.</summary>
    private float HoldTilt => _hardware != null && _hardware.Config != null ? _hardware.Config.uvHoldTilt : 40f;

    /// <summary>The desk's top, where the cone lands (the DeskSurface plane; the cradle's own height without one).</summary>
    private float DeskY => Surface != null ? Surface.transform.position.y : transform.position.y;

    /// <summary>The desk the papers lie on (sought once: in the office this torch belongs to, else anywhere loaded; null without one).</summary>
    private DeskSurface Surface
    {
        get
        {
            if (!_surfaceSought)
            {
                _surfaceSought = true;
                _surface = transform.root.GetComponentInChildren<DeskSurface>(true);
                if (_surface == null)
                    _surface = FindAnyObjectByType<DeskSurface>(FindObjectsInactive.Include);
            }
            return _surface;
        }
    }

    /// <summary>The camera the torch is held under (the one it was pressed under, else the main one).</summary>
    private Camera ViewCamera => _camera != null && _camera.isActiveAndEnabled ? _camera : Camera.main;

    /// <summary>
    /// Works out where the cone lands from the grip: the hold for this view
    /// (UvTorchHold.Hold: the built tilt straight ahead unless the camera would
    /// see the body over the patch), worked out again once the grip or the eye
    /// has moved ResolveDistance; the torch swings to it over SwingSeconds
    /// (<paramref name="snap"/>: at once).
    /// </summary>
    private void AimBeam(bool snap, float dt = 0f)
    {
        Vector3 away = Away();
        Camera cam = ViewCamera;
        Vector3 eye = cam != null ? cam.transform.position : _grip + Vector3.up;
        if (!_hasSolve || (_grip - _solvedGrip).sqrMagnitude > UvTorchHold.ResolveDistance * UvTorchHold.ResolveDistance
            || (eye - _solvedEye).sqrMagnitude > UvTorchHold.ResolveDistance * UvTorchHold.ResolveDistance || Vector3.Dot(away, _solvedAway) < 0.9999f)
        {
            _solved = UvTorchHold.Hold(new TorchPoint(eye.x, eye.y, eye.z), _grip.x, _grip.z, DeskY, HoldHeight, away.x, away.z, HoldTilt,
                                       _tailLength, _lensLength, _bodyRadius, ConeRadius);
            _solvedGrip = _grip;
            _solvedEye = eye;
            _solvedAway = away;
            _hasSolve = true;
        }
        float k = snap || UiMotion.Amount.Still ? 1f : UvTorchHold.Ease(dt, UvTorchHold.SwingSeconds);
        _tilt = Mathf.Lerp(snap ? _solved.Tilt : _tilt, _solved.Tilt, k);
        _yaw = Mathf.Lerp(snap ? _solved.Yaw : _yaw, _solved.Yaw, k);
        Vector3 dir = Quaternion.AngleAxis(_yaw, Vector3.up) * away;
        (float x, float z) = UvCone.Ahead(_grip.x, _grip.z, dir.x, dir.z, HoldHeight, _tilt);
        _aim = new Vector3(x, DeskY, z);
    }

    /// <summary>The share of the patch round <paramref name="lit"/> the view's camera sees past the body held from the grip toward it (UvTorchHold.Shown, the torch's measured capsule).</summary>
    private float ShownAt(Vector3 lit)
    {
        Camera cam = ViewCamera;
        Vector3 eye = cam != null ? cam.transform.position : _grip + Vector3.up;
        return UvTorchHold.Shown(new TorchPoint(eye.x, eye.y, eye.z), new TorchPoint(_grip.x, _grip.y, _grip.z), new TorchPoint(lit.x, lit.y, lit.z),
                                 _tailLength, _lensLength, _bodyRadius, ConeRadius);
    }

    /// <summary>The cone's radius on the desk (DeskConfigSO.uvConeRadius).</summary>
    private float ConeRadius => _hardware != null && _hardware.Config != null ? _hardware.Config.uvConeRadius : 0.075f;

    /// <summary>Away from the chair on the desk, as the view sees it: up the screen, levelled (the office view's forward, the reading view's up).</summary>
    private Vector3 Away()
    {
        Camera cam = ViewCamera;
        if (cam == null)
            return Vector3.forward;
        Vector3 away = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up) + Vector3.ProjectOnPlane(cam.transform.up, Vector3.up);
        return away.sqrMagnitude > 1e-8f ? away.normalized : Vector3.forward;
    }

    /// <summary>Where the pointer at <paramref name="screen"/> holds the grip: on the plane HoldHeight over the desk.</summary>
    private bool Project(Vector2 screen, out Vector3 at)
    {
        at = default;
        Camera cam = ViewCamera;
        if (cam == null)
            return false;
        Ray ray = cam.ScreenPointToRay(screen);
        var hold = new Plane(Vector3.up, new Vector3(0f, DeskY + HoldHeight, 0f));
        if (!hold.Raycast(ray, out float d))
            return false;
        at = ray.GetPoint(d);
        return true;
    }

    private void Switch(bool on, bool quiet)
    {
        _held = on;
        _lift.Target = on ? 1f : 0f;
        if (!quiet)
            CueSounds.Play(on ? SoundCues.UvOn : SoundCues.UvOff, sound);
        if (beam != null)
            beam.SetActive(on);
        if (spot != null)
            spot.enabled = on;
        if (!on)
            ShowPool(false);
        if (hum != null)
        {
            if (on)
            {
                hum.clip = CueSounds.ClipOf(SoundCues.UvHum);
                hum.loop = true;
                if (hum.clip != null)
                    hum.Play();
            }
            else
                hum.Stop();
        }
        if (switchKnob != null)
            switchKnob.localPosition = _switchRest + (on ? switchTravel : Vector3.zero);
        if (lens != null)
        {
            _block ??= new MaterialPropertyBlock();
            lens.GetPropertyBlock(_block);
            Color base_ = lens.sharedMaterial != null && lens.sharedMaterial.HasProperty("_BaseColor") ? lens.sharedMaterial.GetColor("_BaseColor") : Color.white;
            _block.SetColor("_BaseColor", base_ * (on ? lensOn : lensOff));
            lens.SetPropertyBlock(_block);
        }
        if (!on)
        {
            _glow = 0f;
            Shader.SetGlobalFloat(ConeOnId, 0f);
        }
    }

    private void ShowPool(bool show)
    {
        if (_poolShown == show && (pool == null || pool.gameObject.activeSelf == show))
            return;
        _poolShown = show;
        if (pool != null)
            pool.gameObject.SetActive(show);
    }

    /// <summary>Moves between the cradle and the held pose on its spring (a snap under Reduced Motion), follows the pointer, sets the cone and lets the papers count what it lights.</summary>
    private void Update()
    {
        if (torch == null || _hardware == null)
            return;
        Rest();
        MotionKnobs knobs = UiMotion.Knobs;
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        bool still = UiMotion.Amount.Still;
        if (still)
            _lift.Snap(_lift.Target);
        else if (!_lift.AtRest)
            _lift.Step(dt, knobs.Get(knobs.deskMoveFeel), 1e-3f, 0.02f);
        _tip.Target = _hovered && !_held ? 1f : 0f;
        if (still)
            _tip.Snap(_tip.Target);
        else if (!_tip.AtRest)
            _tip.Step(dt, knobs.Get(knobs.hoverFeel), 1e-3f, 0.02f);
        float t = Mathf.Clamp01(_lift.Value);
        float radius = ConeRadius;

        Vector3 restWorld = transform.TransformPoint(_restPos);
        Quaternion restRot = transform.rotation * _restRot * Quaternion.Euler(UvTorchHold.HoverTipDegrees * Mathf.Clamp01(_tip.Value), 0f, 0f);
        if (t <= 1e-3f && !_held)
        {
            torch.SetPositionAndRotation(restWorld, restRot);
            _hardware.TrackCone(_lit, radius, false, dt);
            return;
        }

        if (_held)
        {
            // Every frame, not only on a pointer move: the grip stays on the cursor while the camera blends (BUG-55).
            if (Pointer.current != null && Project(Pointer.current.position.ReadValue(), out Vector3 pointer))
                _pointerGrip = pointer;
            (float gx, float gz) = still ? (_pointerGrip.x, _pointerGrip.z) : UvTorchHold.Follow(_grip.x, _grip.z, _pointerGrip.x, _pointerGrip.z, dt);
            _grip = new Vector3(gx, DeskY + HoldHeight, gz);
            AimBeam(false, dt);
            _lit = still ? _aim : Vector3.Lerp(_lit, _aim, UvTorchHold.Ease(dt, UvTorchHold.TrailSeconds));
            // BUG-2c between two aims too: a swing or the cone's lag that would show the body over the patch goes straight to the solved aim.
            if (!still && UvTorchHold.SwingCovers(ShownAt(_lit), _solved.Unoccluded))
            {
                AimBeam(true);
                _lit = _aim;
            }
        }

        // Held by its grip under the pointer (its origin is the grip), the lens pointing down along the line to the lit spot.
        Vector3 dir = _lit - _grip;
        dir = dir.sqrMagnitude > 1e-8f ? dir.normalized : Vector3.down;
        Quaternion heldRot = Quaternion.LookRotation(-dir, Vector3.up);
        torch.SetPositionAndRotation(Vector3.Lerp(restWorld, _grip, t), Quaternion.Slerp(restRot, heldRot, t));

        bool lit = _held && t > 0.6f;
        Vector3 onDesk = Surface != null ? Surface.Clamp(_lit) : _lit;
        bool onSurface = UvTorchHold.PoolShows(_lit.x, _lit.z, onDesk.x, onDesk.z);
        bool on = lit && onSurface;
        _glow = UvTorchHold.Fluoresce(_glow, on, dt, still);
        float flicker = still ? 1f : 1f + 0.025f * Mathf.Sin(Time.unscaledTime * 37f) + 0.015f * Mathf.Sin(Time.unscaledTime * 91f);
        Shader.SetGlobalVector(ConeId, new Vector4(_lit.x, _lit.y, _lit.z, radius * flicker));
        Shader.SetGlobalFloat(ConeOnId, _glow);
        if (spot != null)
        {
            Vector3 from = torch.TransformPoint(lensLocal);
            spot.transform.SetPositionAndRotation(from, Quaternion.LookRotation(_lit - from, Vector3.up));
            float distance = Vector3.Distance(from, _lit);
            spot.spotAngle = 2f * Mathf.Atan2(radius * 1.15f, Mathf.Max(0.01f, distance)) * Mathf.Rad2Deg;
            spot.range = distance * 1.6f;
            spot.intensity = (lit ? _spotIntensity : 0f) * flicker;
        }
        ShowPool(on);
        if (pool != null && on)
        {
            float side = radius * 2.5f * flicker / Mathf.Max(1e-4f, transform.lossyScale.x);
            pool.SetPositionAndRotation(_lit + Vector3.up * PoolLift, Quaternion.Euler(90f, 0f, 0f));
            pool.localScale = new Vector3(side, side, 1f);
        }
        _hardware.TrackCone(_lit, radius, on, dt);
    }

    private void OnDisable()
    {
        if (_held)
        {
            Switch(false, true);
            HoldRegistry.Desk.End(this);
        }
        _hovered = false;
        Shader.SetGlobalFloat(ConeOnId, 0f);
    }
}
