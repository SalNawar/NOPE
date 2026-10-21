using System;
using UnityEngine;

/// <summary>
/// One slider of the desk's slide-in (desk v3, Saleh: "yes but they need to
/// slide"; UNITY_PLAN 5.3, DESK-HOMES): the object on a home that slides
/// toward the mat or rises in place when the cursor nears it. It writes only
/// its slide transform: the object's "Slide" child when it has one (the
/// painted desk's prefabs: home root, slide child, mesh), else the object
/// itself. A track moves along its axis in the desk's frame (DeskLayout.Frame),
/// a lift rises; a lift also squashes over its anticipation and tips as it
/// rises (the glasses); a secondary part sways at the stop (the intercom's
/// mic); a paper docked on it rides along (Rider: one mover carries both).
/// For an object whose owner poses it every frame (the UV torch's body, which
/// UvTorch sets each Update), the offset is a layer: put on after the owner
/// (LateUpdate), taken off before it (an early Update), so the two never
/// fight. The tip and the squash are always such a layer. The desk's
/// DeskSlideIn decides every slider's pose each frame (ProximityAssist);
/// Attach is the hook every owner binds through (the binder for today's
/// machines; BABEL-TUNER and GLASSES for theirs).
/// </summary>
[DefaultExecutionOrder(-200)]
public sealed class MachineSlideIn : MonoBehaviour
{
    private DeskSlider _slider;
    private Transform _slide, _secondary;
    private bool _layered, _laid;
    private Func<bool> _busy, _taken;
    private Renderer[] _renderers = Array.Empty<Renderer>();
    private Vector3 _restLocal, _secondaryRest, _prePos, _preScale, _moved;
    private Quaternion _preRot;
    private float _height;

    /// <summary>The slider's id (DeskSlides).</summary>
    public string Id => _slider != null ? _slider.id : null;

    /// <summary>The slider's data.</summary>
    public DeskSlider Slider => _slider;

    /// <summary>The pose DeskSlideIn gave it this frame (its position, squash, sway, shadow and lamp): the shadow decal under it reads it (DESK-ART).</summary>
    public SlidePose Pose { get; set; }

    /// <summary>A paper docked on it (DOC-HANDLE sets it while one lies in its bed, pad or slot; null: none): it moves with the slide, and the slider counts as busy.</summary>
    public Transform Rider { get; set; }

    /// <summary>True while it is busy (scanning, reading, or holding a paper): it stays as it is (ProximityAssist).</summary>
    public bool Busy => Rider != null || (_busy != null && _busy());

    /// <summary>True while its object is in the player's hand (the UV torch picked up): it gives way at once and is not summoned.</summary>
    public bool Taken => _taken != null && _taken();

    /// <summary>True while it stands on the desk: bound, active and not waiting at zero scale (for the crate).</summary>
    public bool Present => _slider != null && _slide != null && isActiveAndEnabled && _slide.gameObject.activeInHierarchy && _slide.lossyScale.sqrMagnitude > 1e-8f;

    /// <summary>
    /// Binds the object <paramref name="root"/> on the desk as the slider
    /// <paramref name="id"/> of the bound desk (DeskSlideIn.Current; nothing
    /// without one, an unknown id or a slider that never moves). It slides
    /// <paramref name="slide"/> (default: root's "Slide" child, else root);
    /// <paramref name="busy"/> says when it is scanning or reading;
    /// <paramref name="layered"/>: its owner poses the slide every frame, so the
    /// offset is a layer over it; <paramref name="taken"/> says when it is in
    /// the player's hand; <paramref name="secondary"/> sways at the stop. Its
    /// rest is where it stands now. Returns the slider, or null.
    /// </summary>
    public static MachineSlideIn Attach(string id, Transform root, Transform slide = null, Func<bool> busy = null, bool layered = false, Func<bool> taken = null,
                                        Transform secondary = null)
    {
        DeskSlideIn desk = DeskSlideIn.Current;
        DeskSlider slider = desk != null && desk.Slides != null ? desk.Slides.Find(id) : null;
        if (root == null || slider == null || !slider.Moves)
            return null;
        if (!root.TryGetComponent(out MachineSlideIn machine))
            machine = root.gameObject.AddComponent<MachineSlideIn>();
        Transform child = root.Find("Slide");
        machine.Bind(slider, slide != null ? slide : child != null ? child : root, busy, layered, taken, secondary);
        desk.Add(machine);
        return machine;
    }

    /// <summary>True when <paramref name="part"/> belongs to this object (one of its click boxes).</summary>
    public bool Owns(Component part) => part != null && part.transform.IsChildOf(transform);

    /// <summary>The px from <paramref name="pointer"/> to its silhouette on <paramref name="camera"/>'s screen (its renderers' bounds as they stand now; 0 on it, infinite when none shows).</summary>
    public float ScreenDistance(Camera camera, Vector2 pointer)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        bool any = false;
        foreach (Renderer r in _renderers)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                continue;
            Bounds b = r.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 s = camera.WorldToScreenPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
                if (s.z <= 0f)
                    continue;
                x0 = Mathf.Min(x0, s.x);
                x1 = Mathf.Max(x1, s.x);
                y0 = Mathf.Min(y0, s.y);
                y1 = Mathf.Max(y1, s.y);
                any = true;
            }
        }
        if (!any)
            return float.PositiveInfinity;
        float dx = Mathf.Max(x0 - pointer.x, 0f, pointer.x - x1), dy = Mathf.Max(y0 - pointer.y, 0f, pointer.y - y1);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Binds it (see Attach): bound before and still where it put it (nobody laid it again), it comes home first; then where it stands is its rest.</summary>
    private void Bind(DeskSlider slider, Transform slide, Func<bool> busy, bool layered, Func<bool> taken, Transform secondary)
    {
        TakeBack();
        if (_slide != null && !_layered && (_slide.localPosition - (_restLocal + _moved)).sqrMagnitude < 1e-10f)
            _slide.localPosition = _restLocal;
        _moved = Vector3.zero;
        if (_secondary != null)
            _secondary.localPosition = _secondaryRest;
        _slider = slider;
        _slide = slide;
        _busy = busy;
        _layered = layered;
        _taken = taken;
        _secondary = secondary;
        _restLocal = slide.localPosition;
        if (secondary != null)
            _secondaryRest = secondary.localPosition;
        Pose = default;
        _renderers = GetComponentsInChildren<Renderer>(true);
        _height = 0f;
    }

    /// <summary>How tall the slide stands (metres; the squash's base), measured once it shows (one waiting for the crate stands at zero scale until then).</summary>
    private float Height()
    {
        if (_height <= 1e-4f)
            foreach (Renderer r in _slide.GetComponentsInChildren<Renderer>(false))
                _height = Mathf.Max(_height, r.bounds.size.y);
        return _height;
    }

    /// <summary>Early in the frame (before the owners move it): the tip, the squash and a layered offset come off.</summary>
    private void Update() => TakeBack();

    /// <summary>Late in the frame (after the owners and DeskSlideIn): the pose goes on.</summary>
    private void LateUpdate() => Show();

    private void OnDisable()
    {
        TakeBack();
        if (_slide != null && !_layered)
            _slide.localPosition = _restLocal;
        if (_secondary != null)
            _secondary.localPosition = _secondaryRest;
    }

    /// <summary>Takes off what Show put on as a layer.</summary>
    private void TakeBack()
    {
        if (!_laid || _slide == null)
            return;
        _laid = false;
        _slide.localRotation = _preRot;
        _slide.localScale = _preScale;
        if (_layered)
            _slide.localPosition = _prePos;
    }

    /// <summary>Puts this frame's pose on the slide: the travel along its axis (a track's in the desk's frame, a lift's up), never past its felt gap (ProximityAssist); the rider by the same move; the tip, the squash and the secondary sway.</summary>
    private void Show()
    {
        if (_slide == null || _slider == null || !DeskLayout.Bound)
            return;
        DeskFrame frame = DeskLayout.Frame;
        SlidePose pose = Pose;
        bool lift = _slider.kind == SlideKind.Lift;
        Vector3 axis = lift ? Vector3.up : frame.Right * _slider.dirX + frame.Forward * _slider.dirY;
        Vector3 move = ToParent(axis * (pose.Position * _slider.travel));
        float metresPerPx = _slider.travelPx > 0f ? _slider.travel / _slider.travelPx : 0f;

        if (!_layered)
        {
            Vector3 was = _slide.position;
            _slide.localPosition = _restLocal + move;
            _moved = move;
            if (Rider != null)
                Rider.position += _slide.position - was;
        }

        _preRot = _slide.localRotation;
        _preScale = _slide.localScale;
        _prePos = _slide.localPosition;
        _laid = true;
        if (_layered)
            _slide.localPosition = _prePos + move;
        float tip = SlideCurve.Tip(_slider, pose.Position);
        if (Mathf.Abs(tip) > 1e-4f)
        {
            Vector3 toward = frame.Right * _slider.tipX + frame.Forward * _slider.tipY;
            if (toward.sqrMagnitude > 1e-8f)
                _slide.rotation = Quaternion.AngleAxis(tip, Vector3.Cross(Vector3.up, toward.normalized)) * _slide.rotation;
        }
        if (pose.Squash > 0f && Height() > 1e-4f)
            _slide.localScale = new Vector3(_preScale.x, _preScale.y * Mathf.Max(0.5f, 1f - pose.Squash * metresPerPx / _height), _preScale.z);
        if (_secondary != null)
            _secondary.localPosition = _secondaryRest + ToParent(axis * (pose.Sway * metresPerPx), _secondary);
    }

    /// <summary>A world move in the slide's parent's space.</summary>
    private Vector3 ToParent(Vector3 world) => ToParent(world, _slide);

    private static Vector3 ToParent(Vector3 world, Transform t) => t.parent != null ? t.parent.InverseTransformVector(world) : world;
}
