using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One of the lens stand's magnifiers (the desk machines spec §10; Saleh
/// 2026-10-18, demo 1018a note 14: "pick up the loupe, bar or Fresnel and use
/// it over a paper to magnify microprint that has a meaning"): its glass
/// shows what lies under it on the desk, larger. The loupe (round, the
/// strongest) and the bar magnifier (a strip, a whole line at once) are
/// held: press on one and it lifts off the stand, its glass under the
/// pointer, held over the desk; drag and it follows; let go and it springs
/// back to the stand (Papers, Please's one hand; no toggle). The Fresnel
/// sheet stays on its hinged arm: folded down over the desk (a click on the
/// stand) it magnifies whatever is slid under it, hands-free. The view is a
/// second camera looking from the office camera through the glass with a
/// narrower field (the magnification), its picture drawn on the glass;
/// everything nearer than the glass is clipped from it, the magnifier
/// itself included. Each frame it magnifies, the papers' microprint lines
/// under its glass count toward being read (DeskHardware.TrackLens,
/// MicroLens). LensKit owns them; Build Office UI wires each part.
/// </summary>
public sealed class DeskMagnifier : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>The glass's centre (its view is drawn there) and its shape.</summary>
    [SerializeField] private Transform glass;

    /// <inheritdoc cref="glass"/>
    [SerializeField] private LensShape shape = LensShape.Round;

    /// <summary>The glass's half width and half height in the glass's own space (a round glass: its radius twice).</summary>
    [SerializeField] private Vector2 halfSize = new Vector2(0.02f, 0.02f);

    /// <summary>How much larger it shows the desk (2.5 the loupe, 1.8 the bar, 1.4 the Fresnel).</summary>
    [SerializeField] private float magnification = 2.5f;

    /// <summary>True for a magnifier picked up and held (the loupe, the bar); false for the Fresnel on its arm.</summary>
    [SerializeField] private bool held = true;

    /// <summary>How high over the desk the held glass rides (metres).</summary>
    [SerializeField] private float holdHeight = 0.06f;

    /// <summary>The cues' placeholder source.</summary>
    [SerializeField] private AudioSource sound;

    private DeskHardware _hardware;
    private Vector3 _restPos;
    private bool _rested, _holding, _laid;
    private Spring _lift;
    private Vector3 _at;
    private Plane _hold;
    private Camera _view;
    private RenderTexture _texture;
    private bool _rendererMatched;
    private GameObject _screen;
    private Material _screenMaterial;

    /// <summary>True while it magnifies (held, or the Fresnel folded down).</summary>
    public bool Magnifying => _holding || _laid;

    /// <summary>Wires it to the desk machines' hub (its readings and the papers' microprint lines).</summary>
    public void Bind(DeskHardware hardware)
    {
        _hardware = hardware;
        Rest();
    }

    private void Rest()
    {
        if (_rested)
            return;
        _rested = true;
        _restPos = transform.localPosition;
    }

    /// <summary>The Fresnel's arm is folded down over the desk (true) or up (false): it magnifies while down.</summary>
    public void Lay(bool down) => _laid = !held && down;

    /// <summary>Pressed: a held magnifier lifts off the stand, its glass under the pointer.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!held || eventData.button != PointerEventData.InputButton.Left)
            return;
        Rest();
        Vector3 desk = transform.parent != null ? transform.parent.position : transform.position;
        _hold = new Plane(Vector3.up, new Vector3(0f, desk.y + holdHeight, 0f));
        if (!Project(eventData, out _at))
            _at = transform.position;
        _holding = true;
        _lift.Target = 1f;
        CueSounds.Play(SoundCues.LoupeTick, sound, 1.2f);
    }

    /// <inheritdoc/>
    public void OnBeginDrag(PointerEventData eventData) { }

    /// <summary>Dragged: the glass follows the pointer over the desk.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_holding && Project(eventData, out Vector3 at))
            _at = at;
    }

    /// <summary>Let go: it springs back to the stand.</summary>
    public void OnEndDrag(PointerEventData eventData) => Release();

    /// <inheritdoc cref="OnEndDrag"/>
    public void OnPointerUp(PointerEventData eventData) => Release();

    /// <summary>Back to the stand (also at a case's end).</summary>
    public void Release()
    {
        if (!_holding)
            return;
        _holding = false;
        _lift.Target = 0f;
        CueSounds.Play(SoundCues.LoupeTick, sound, 0.9f);
    }

    private bool Project(PointerEventData eventData, out Vector3 at)
    {
        at = default;
        Camera cam = eventData.pressEventCamera != null ? eventData.pressEventCamera : eventData.enterEventCamera;
        if (cam == null || !_hold.Raycast(cam.ScreenPointToRay(eventData.position), out float d))
            return false;
        at = cam.ScreenPointToRay(eventData.position).GetPoint(d);
        return true;
    }

    private void LateUpdate()
    {
        Rest();
        float dt = UiMotion.Delta(Time.unscaledDeltaTime);
        if (held)
        {
            if (UiMotion.Amount.Still)
                _lift.Snap(_lift.Target);
            else if (!_lift.AtRest)
                _lift.Step(dt, UiMotion.Knobs.Get(UiMotion.Knobs.deskMoveFeel), 1e-3f, 0.02f);
            float t = Mathf.Clamp01(_lift.Value);
            Transform parent = transform.parent;
            Vector3 restWorld = parent != null ? parent.TransformPoint(_restPos) : _restPos;
            Vector3 glassOffset = glass != null ? transform.position - glass.position : Vector3.zero;
            transform.position = Vector3.Lerp(restWorld, _at + glassOffset, t); // its turn stays the stand's (the loupe still ticks round)
        }
        bool on = Magnifying && glass != null && (!held || _lift.Value > 0.6f);
        Camera main = Camera.main;
        if (!on || main == null)
        {
            Show(false);
            if (_hardware != null && Magnifying)
                _hardware.TrackLens(null, shape, 0f, 0f, false, dt);
            return;
        }
        Show(true);
        Vector3 centre = glass.position;
        Vector2 half = new Vector2(halfSize.x * glass.lossyScale.x, halfSize.y * glass.lossyScale.y);
        Vector3 toGlass = centre - main.transform.position;
        float d = toGlass.magnitude;
        Aim(main, centre, half, d);

        // The glass's footprint on the desk: along the camera's ray through it, scaled by how much further the desk lies.
        Vector3 right = Vector3.ProjectOnPlane(main.transform.right, Vector3.up).normalized;
        Vector3 away = Vector3.Cross(right, Vector3.up).normalized * -1f;
        float deskY = (transform.parent != null ? transform.parent.position.y : centre.y - holdHeight);
        Vector3 ray = toGlass / Mathf.Max(1e-4f, d);
        float along = Mathf.Abs(ray.y) > 1e-4f ? (deskY - main.transform.position.y) / ray.y : d;
        Vector3 spot = main.transform.position + ray * along;
        float grow = along / Mathf.Max(1e-4f, d) / Mathf.Max(1f, magnification);
        Vector2 footprint = half * grow;
        _hardware?.TrackLens(p => new Vector2(Vector3.Dot(p - spot, right), Vector3.Dot(p - spot, away)), shape, footprint.x, footprint.y, true, dt);
    }

    /// <summary>Points the view camera through the glass: from the office camera, at the glass's centre, its field the glass's narrowed by the magnification; nothing nearer than the glass drawn.</summary>
    private void Aim(Camera main, Vector3 centre, Vector2 half, float distance)
    {
        if (_view == null)
            return;
        if (!_rendererMatched)
            _rendererMatched = MatchRenderer(_view, main);
        _view.cullingMask = main.cullingMask;
        _view.orthographic = main.orthographic;
        _view.nearClipPlane = distance + 0.004f;
        _view.farClipPlane = main.farClipPlane;
        _view.aspect = half.x / Mathf.Max(1e-4f, half.y);
        if (main.orthographic)
        {
            _view.transform.SetPositionAndRotation(centre - main.transform.forward * distance, main.transform.rotation);
            _view.orthographicSize = half.y / magnification;
        }
        else
        {
            _view.transform.SetPositionAndRotation(main.transform.position, Quaternion.LookRotation(centre - main.transform.position, main.transform.up));
            _view.fieldOfView = 2f * Mathf.Atan(half.y / Mathf.Max(1e-4f, distance) / magnification) * Mathf.Rad2Deg;
        }
        if (_screen != null)
        {
            _screen.transform.SetPositionAndRotation(centre, Quaternion.LookRotation(main.transform.forward, main.transform.up));
            Vector3 parentScale = _screen.transform.parent != null ? _screen.transform.parent.lossyScale : Vector3.one;
            _screen.transform.localScale = new Vector3(2f * half.x / Mathf.Max(1e-5f, parentScale.x), 2f * half.y / Mathf.Max(1e-5f, parentScale.y), 1f);
        }
    }

    /// <summary>
    /// The view draws with <paramref name="main"/>'s renderer (the office
    /// camera's forward renderer): a camera made at run time takes the
    /// pipeline's default, the 2D renderer, which draws nothing of the 3D
    /// desk. False when <paramref name="main"/>'s renderer is not in the
    /// pipeline's list (the default is kept).
    /// </summary>
    private static bool MatchRenderer(Camera view, Camera main)
    {
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp))
            return true;
        ScriptableRenderer wanted = main.GetUniversalAdditionalCameraData().scriptableRenderer;
        for (int i = 0; i < urp.rendererDataList.Length; i++)
        {
            if (urp.GetRenderer(i) != wanted)
                continue;
            view.GetUniversalAdditionalCameraData().SetRenderer(i);
            return true;
        }
        return false;
    }

    /// <summary>Shows the view on the glass (made the first time: its camera, its picture and the glass's screen) or hides it.</summary>
    private void Show(bool on)
    {
        if (!on)
        {
            if (_view != null)
                _view.enabled = false;
            if (_screen != null)
                _screen.SetActive(false);
            return;
        }
        if (_view == null)
        {
            int w = 512, h = Mathf.Max(64, Mathf.RoundToInt(512f * halfSize.y / Mathf.Max(1e-4f, halfSize.x)));
            _texture = new RenderTexture(w, h, 24) { name = name + "_View" };
            var cam = new GameObject(name + "_ViewCamera");
            cam.transform.SetParent(transform, false);
            _view = cam.AddComponent<Camera>();
            _view.targetTexture = _texture;
            _view.clearFlags = CameraClearFlags.SolidColor;
            _view.backgroundColor = new Color(0.17f, 0.11f, 0.14f, 1f);
            _view.depth = -10f;

            _screen = new GameObject(name + "_Screen");
            _screen.transform.SetParent(glass, false);
            _screen.AddComponent<MeshFilter>().sharedMesh = shape == LensShape.Round ? Disc() : DeskHardware.QuadMesh;
            var r = _screen.AddComponent<MeshRenderer>();
            _screenMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { mainTexture = _texture };
            if (_screenMaterial.HasProperty("_BaseMap"))
                _screenMaterial.SetTexture("_BaseMap", _texture);
            _screenMaterial.renderQueue = 3050;
            r.sharedMaterial = _screenMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        _view.enabled = true;
        _screen.SetActive(true);
    }

    /// <summary>A unit disc facing -Z (radius 0.5, the quad's size), its picture's square mapped onto it.</summary>
    private static Mesh Disc()
    {
        const int n = 40;
        var v = new Vector3[n + 1];
        var uv = new Vector2[n + 1];
        var tris = new int[n * 3];
        v[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            v[i + 1] = new Vector3(0.5f * Mathf.Cos(a), 0.5f * Mathf.Sin(a), 0f);
            uv[i + 1] = new Vector2(0.5f + 0.5f * Mathf.Cos(a), 0.5f + 0.5f * Mathf.Sin(a));
            tris[i * 3] = 0;
            tris[i * 3 + 1] = (i + 1) % n + 1;
            tris[i * 3 + 2] = i + 1;
        }
        var mesh = new Mesh { name = "LensDisc", vertices = v, uv = uv, triangles = tris };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDisable()
    {
        _holding = false;
        _lift = Spring.At(0f);
        Show(false);
    }

    private void OnDestroy()
    {
        if (_texture != null)
            _texture.Release();
        Destroy(_texture);
        Destroy(_screenMaterial);
    }
}
