using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// The UV torch (the desk machines spec §1; Saleh 2026-10-17: "uv light should
/// be an actual uv flashlight you can use to see the hidden watermarks and
/// symbols"): a handheld blacklight resting in its cradle on the desk. Press
/// on it and it lifts out with its thumb switch's click and switches on; drag
/// and it follows the pointer, held over the desk, its violet cone lighting
/// the spot under the pointer (a spot light and the BeamFX cone), with a soft
/// hum and a slight flicker; let go and it clicks off and springs back to its
/// cradle. No toggle, no second step (Papers, Please's one hand: while it is
/// held, no paper moves). Every frame it sets the cone the papers' hidden
/// layers are revealed in (the TimeDesk/UvReveal shaders' _UvCone and
/// _UvConeOn) and lets each paper count its lit marks (DeskHardware.TrackCone).
/// Its parts are the UvTorch art's (Build Office UI binds them); the office
/// binder lays it on the desk.
/// </summary>
public sealed class UvTorch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The shader globals the reveal reads: the cone (centre xyz, radius w) and whether the torch is on.</summary>
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

    /// <summary>The cone's pool of violet light on the desk (a flat additive disc laid over the papers under the cone, on while lit: the papers read unlit in the reading view, so the light is drawn too).</summary>
    [SerializeField] private Transform pool;

    /// <summary>The hum's looping source and the switch's one-shots.</summary>
    [SerializeField] private AudioSource hum, sound;

    /// <summary>The lens centre in the torch's own space (where the cone starts), and the slide's travel when on (in the torch's own space).</summary>
    [SerializeField] private Vector3 lensLocal = new Vector3(0f, 0f, -0.1285f), switchTravel = new Vector3(0f, 0f, -0.01f);

    /// <summary>The lens's glow when on (its colour's multiplier) and off.</summary>
    [SerializeField] private Color lensOn = new Color(1.6f, 1.3f, 2.2f, 1f), lensOff = Color.white;

    private DeskHardware _hardware;
    private Vector3 _restPos, _switchRest;
    private Quaternion _restRot;
    private bool _held, _rested;
    private Spring _lift;
    private Vector3 _aim;
    private Plane _desk;
    private MaterialPropertyBlock _block;
    private float _spotIntensity = 1f;

    /// <summary>True while the torch is held (switched on).</summary>
    public bool Held => _held;

    /// <summary>The spot the cone lights on the desk (while held).</summary>
    public Vector3 Aim => _aim;

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
        Switch(false, true);
    }

    /// <summary>Pressed: it lifts out and switches on, lighting the spot under the pointer.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || torch == null)
            return;
        Rest();
        _desk = new Plane(Vector3.up, transform.position);
        if (!Project(eventData, out _aim))
            _aim = transform.position;
        Switch(true, false);
    }

    /// <summary>A drag begins (it is already held).</summary>
    public void OnBeginDrag(PointerEventData eventData) { }

    /// <summary>Dragged: the cone follows the pointer over the desk.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_held && Project(eventData, out Vector3 at))
            _aim = at;
    }

    /// <summary>Let go (a drag's end or a plain release): it clicks off and goes back to its cradle.</summary>
    public void OnEndDrag(PointerEventData eventData) => Release();

    /// <inheritdoc cref="OnEndDrag"/>
    public void OnPointerUp(PointerEventData eventData) => Release();

    /// <summary>Switches off and sends it back to its cradle (also at a case's end).</summary>
    public void Release()
    {
        if (_held)
            Switch(false, false);
    }

    private bool Project(PointerEventData eventData, out Vector3 at)
    {
        at = default;
        Camera cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        if (cam == null)
            return false;
        Ray ray = cam.ScreenPointToRay(eventData.position);
        if (!_desk.Raycast(ray, out float d))
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
        if (pool != null)
            pool.gameObject.SetActive(on);
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
            Shader.SetGlobalFloat(ConeOnId, 0f);
    }

    /// <summary>Moves between the cradle and the held pose on its spring (a snap under Reduced Motion), sets the cone and lets the papers count what it lights.</summary>
    private void Update()
    {
        if (torch == null || _hardware == null)
            return;
        Rest();
        MotionKnobs knobs = UiMotion.Knobs;
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        if (UiMotion.Amount.Still)
            _lift.Snap(_lift.Target);
        else if (!_lift.AtRest)
            _lift.Step(dt, knobs.Get(knobs.deskMoveFeel), 1e-3f, 0.02f);
        float t = Mathf.Clamp01(_lift.Value);
        DeskConfigSO config = _hardware.Config;
        float radius = config != null ? config.uvConeRadius : 0.075f;

        Vector3 restWorld = transform.TransformPoint(_restPos);
        Quaternion restRot = transform.rotation * _restRot;
        if (t <= 1e-3f && !_held)
        {
            torch.SetPositionAndRotation(restWorld, restRot);
            _hardware.TrackCone(_aim, radius, false, dt);
            return;
        }

        // Held as a right hand holds it: the lens above and to the right of the lit spot, a little toward the chair, pointing down at it, so the cone and what it shows stay in view.
        Camera cam = Camera.main;
        Vector3 toChair = cam != null ? Vector3.ProjectOnPlane(cam.transform.position - _aim, Vector3.up).normalized : -transform.forward;
        Vector3 right = Vector3.Cross(Vector3.up, -toChair).normalized;
        float height = config != null ? config.uvHoldHeight : 0.12f, back = config != null ? config.uvHoldBack : 0.04f, side = config != null ? config.uvHoldSide : 0.14f;
        Vector3 lensAt = _aim + Vector3.up * height + toChair * back + right * side;
        Vector3 dir = (_aim - lensAt).normalized;
        Quaternion heldRot = Quaternion.LookRotation(-dir, Vector3.up);
        Vector3 heldPos = lensAt - heldRot * lensLocal;
        torch.SetPositionAndRotation(Vector3.Lerp(restWorld, heldPos, t), Quaternion.Slerp(restRot, heldRot, t));

        bool on = _held && t > 0.6f;
        float flicker = UiMotion.Amount.Still ? 1f : 1f + 0.025f * Mathf.Sin(Time.unscaledTime * 37f) + 0.015f * Mathf.Sin(Time.unscaledTime * 91f);
        Shader.SetGlobalVector(ConeId, new Vector4(_aim.x, _aim.y, _aim.z, radius * flicker));
        Shader.SetGlobalFloat(ConeOnId, on ? 1f : 0f);
        if (spot != null)
        {
            Vector3 from = torch.TransformPoint(lensLocal);
            spot.transform.SetPositionAndRotation(from, Quaternion.LookRotation(_aim - from, Vector3.up));
            float distance = Vector3.Distance(from, _aim);
            spot.spotAngle = 2f * Mathf.Atan2(radius * 1.15f, Mathf.Max(0.01f, distance)) * Mathf.Rad2Deg;
            spot.range = distance * 1.6f;
            spot.intensity = (on ? _spotIntensity : 0f) * flicker;
        }
        if (pool != null)
        {
            pool.SetPositionAndRotation(_aim + Vector3.up * PoolLift, Quaternion.Euler(90f, 0f, 0f));
            pool.localScale = new Vector3(radius * 2.5f * flicker / Mathf.Max(1e-4f, transform.lossyScale.x), radius * 2.5f * flicker / Mathf.Max(1e-4f, transform.lossyScale.x), 1f);
        }
        _hardware.TrackCone(_aim, radius, on, dt);
    }

    /// <summary>How high over the desk the cone's pool lies (metres): over a stack of papers, under a carried one.</summary>
    private const float PoolLift = 0.012f;

    private void OnDisable()
    {
        if (_held)
            Switch(false, true);
        Shader.SetGlobalFloat(ConeOnId, 0f);
    }
}
