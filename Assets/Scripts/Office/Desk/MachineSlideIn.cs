using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One slider of the desk's slide-in (desk v3, Saleh: "yes but they need to
/// slide"; UNITY_PLAN 5.3, DESK-HOMES): the object on a home that slides
/// toward the mat or rises in place when the cursor nears it. Its kind and
/// travel are its row of DeskSlides; its axis is its home's
/// (DeskLayout.HomeOf: DeskHome.slide, the map's one source of the
/// direction), so a track whose home declares no slide is never attached. It
/// writes only its slide transform: the object's "Slide" child when it has
/// one (the painted desk's prefabs: home root, slide child, mesh), else the
/// object itself. Where the slide puts it is SlideCurve.Offset (a track
/// along its axis in the desk's frame, DeskLayout.Frame; a lift up); a lift
/// also squashes over its anticipation about its base, whatever its pivot
/// (SlideCurve.SquashScale and SquashDrop: the base stays down, the top
/// comes down), and tips as
/// it rises (the glasses); a "Mic" under the slide sways at the stop (the
/// intercom's prefab). Under it lies its contact shadow (SlideShadow): a
/// lift's at its rest footprint, a track's travelling with its base
/// (SlideCurve.ShadowCentre). For an object whose owner poses it every frame
/// (the UV torch's body, which UvTorch sets each Update), the offset is a
/// layer: put on after the owner (LateUpdate), taken off before it (an early
/// Update), so the two never fight. The tip and the squash are always such a
/// layer. Its rest (footprint, base, height and shadow) is measured only once
/// its scale has stood still for a few frames at home, so an object popping
/// in from the crate (DeskUpgrades' elastic pop) is never measured mid-pop,
/// and again if its scale later settles somewhere else (a reaction's pulse,
/// which ends where it began, measures nothing). In a forced retract the paper docked
/// in it rides it home (Rides: one mover carries both). The desk's
/// DeskSlideIn decides every slider's pose each frame (ProximityAssist);
/// Attach is the hook every owner binds through (the binder for today's
/// machines; BABEL-TUNER and GLASSES for theirs).
/// </summary>
[DefaultExecutionOrder(-200)]
public sealed class MachineSlideIn : MonoBehaviour
{
    /// <summary>How many frames in a row its scale must stand still before its rest is measured (an elastic pop's turning point never holds still that long).</summary>
    private const int SettleFrames = 3;

    private DeskSlider _slider;
    private DeskSlides _slides;
    private DeskHome _home;
    private float _way;
    private Transform _slide, _secondary, _shadowParent;
    private bool _layered, _laid, _measured, _riderSought;
    private Func<bool> _busy, _taken;
    private IPaperDock _dock;
    private DeskDocument _rider;
    private Material _shadowMaterial;
    private SlideShadow _shadow;
    private Renderer[] _renderers = Array.Empty<Renderer>();
    private Vector3 _restLocal, _secondaryRest, _prePos, _preScale, _moved, _lastScale, _measuredScale;
    private Quaternion _preRot;
    private Vector2 _restCentre;
    private float _height, _restBase, _pivotAboveBase;
    private int _stillFrames;

    /// <summary>The squash's drop is on the slide's position (TakeBack takes it off), and what the pose last put on: the lift's rise and how far the squash brought the top down (metres; Raised).</summary>
    private bool _dropped;
    private float _shownUp, _shownSquash;

    /// <summary>The slider's id (DeskSlides).</summary>
    public string Id => _slider != null ? _slider.id : null;

    /// <summary>The pose DeskSlideIn gave it this frame (its position, squash, sway and contact shadow): put on in LateUpdate.</summary>
    public SlidePose Pose { get; set; }

    /// <summary>True while it is busy (scanning, reading, receiving a paper): it stays as it is (ProximityAssist).</summary>
    public bool Busy => _busy != null && _busy();

    /// <summary>True while its object is in the player's hand (the UV torch picked up): it gives way at once and is not summoned.</summary>
    public bool Taken => _taken != null && _taken();

    /// <summary>True while it stands on the desk: bound, active and not waiting at zero scale (for the crate).</summary>
    public bool Present => _slider != null && _slide != null && isActiveAndEnabled && _slide.gameObject.activeInHierarchy && _slide.lossyScale.sqrMagnitude > 1e-8f;

    /// <summary>
    /// How far its top stands over where it stands at rest at this moment
    /// (metres; a lift risen, less what its squash brought the top down):
    /// what is on its transforms now, the layers counted only while they are
    /// on (between LateUpdate and the next early Update). DeskLayout's
    /// RefreshObstacles takes it off a measured top, so a lift raised when the
    /// obstacles are measured (a crate's pop) is not counted twice.
    /// </summary>
    public float Raised => (_layered && !_laid ? 0f : _shownUp) - (_laid ? _shownSquash : 0f);

    /// <summary>
    /// Binds the object <paramref name="root"/> on the desk as the slider
    /// <paramref name="id"/> of the bound desk (DeskSlideIn.Current; nothing
    /// without one, an unknown id, or a slider that does not move where it
    /// rests: DeskSlider.MovesOn its home). It slides <paramref name="slide"/>
    /// (default: root's "Slide" child, else root); <paramref name="busy"/>
    /// says when it is scanning, reading or receiving a paper;
    /// <paramref name="layered"/>: its owner poses the slide every frame, so
    /// the offset is a layer over it; <paramref name="taken"/> says when it is
    /// in the player's hand; <paramref name="dock"/> is the machine a paper
    /// docks in (a forced retract carries that paper). Its rest is where it
    /// stands now. Returns the slider, or null.
    /// </summary>
    public static MachineSlideIn Attach(string id, Transform root, Transform slide = null, Func<bool> busy = null, bool layered = false, Func<bool> taken = null,
                                        IPaperDock dock = null)
    {
        DeskSlideIn desk = DeskSlideIn.Current;
        DeskSlider slider = desk != null && desk.Slides != null ? desk.Slides.Find(id) : null;
        DeskHome home = slider != null ? DeskLayout.HomeOf(slider.Home) : null;
        if (root == null || slider == null || !slider.MovesOn(home))
            return null;
        if (!root.TryGetComponent(out MachineSlideIn machine))
            machine = root.gameObject.AddComponent<MachineSlideIn>();
        Transform child = root.Find("Slide");
        machine.Bind(desk, slider, home, slide != null ? slide : child != null ? child : root, busy, layered, taken, dock);
        desk.Add(machine);
        return machine;
    }

    /// <summary>True when <paramref name="part"/> belongs to this object (one of its click boxes).</summary>
    public bool Owns(Component part) => part != null && part.transform.IsChildOf(transform);

    /// <summary>True when the desk point <paramref name="deskPoint"/> (DeskLayout.Frame's x, y) lies in its home's footprint.</summary>
    public bool RestsOn(Vector2 deskPoint) => _home != null && _home.Footprint.Contains(deskPoint.x, deskPoint.y);

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

    /// <summary>
    /// True while the paper docked in it rides it home (the shift is over:
    /// DeskSlideIn asks only then, for a busy slider): the first ask finds
    /// that paper (TakeRider); it rides until its own carry takes it (never
    /// two movers) or the slide is home. False when none can ride, and the
    /// slider then stays out until it is empty.
    /// </summary>
    public bool Rides()
    {
        if (!_riderSought)
        {
            _riderSought = true;
            TakeRider();
        }
        return _rider != null && _rider.isActiveAndEnabled && !_rider.IsSliding;
    }

    /// <summary>
    /// Finds the paper docked in it (SlideOccupancy.Rider): of the desk's
    /// papers lying still inside its dock's drop area, the one nearest where
    /// the dock puts a paper (its bed, pad or slot point), so a paper lying
    /// beside the machine never rides while the docked one stays behind. Runs
    /// once per forced retract, not per frame (it looks through the scene's
    /// papers).
    /// </summary>
    private void TakeRider()
    {
        _rider = null;
        if (_dock == null)
            return;
        DeskDocument[] papers = FindObjectsByType<DeskDocument>();
        var facts = new List<(float distance, bool inside, bool still)>(papers.Length);
        foreach (DeskDocument paper in papers)
        {
            bool live = paper != null && paper.isActiveAndEnabled;
            Vector3 at = live ? paper.transform.position : Vector3.zero;
            facts.Add((live ? Mathf.Min(Flat(at, _dock.DockPoint(MachineDock.Bed)), Mathf.Min(Flat(at, _dock.DockPoint(MachineDock.Pad)), Flat(at, _dock.DockPoint(MachineDock.Slot)))) : float.PositiveInfinity,
                       live && _dock.Contains(at), live && !paper.IsSliding));
        }
        int rider = SlideOccupancy.Rider(facts);
        _rider = rider >= 0 ? papers[rider] : null;
    }

    /// <summary>The distance between <paramref name="a"/> and <paramref name="b"/> across the desk (x and z).</summary>
    private static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

    /// <summary>Binds it (see Attach): bound before and still where it put it (nobody laid it again), it comes home first; then where it stands is its rest, measured (its footprint, height and shadow) once it stands still there.</summary>
    private void Bind(DeskSlideIn desk, DeskSlider slider, DeskHome home, Transform slide, Func<bool> busy, bool layered, Func<bool> taken, IPaperDock dock)
    {
        TakeBack();
        if (_slide != null && !_layered && (_slide.localPosition - (_restLocal + _moved)).sqrMagnitude < 1e-10f)
            _slide.localPosition = _restLocal;
        _moved = Vector3.zero;
        if (_secondary != null)
            _secondary.localPosition = _secondaryRest;
        _slider = slider;
        _slides = desk.Slides;
        _home = home;
        _way = slider.Way(home);
        _slide = slide;
        _busy = busy;
        _layered = layered;
        _taken = taken;
        _dock = dock;
        _rider = null;
        _riderSought = false;
        _secondary = slide.Find("Mic");
        _restLocal = slide.localPosition;
        if (_secondary != null)
            _secondaryRest = _secondary.localPosition;
        _shadowMaterial = desk.ShadowMaterial;
        _shadowParent = desk.transform;
        if (_shadow != null)
            Destroy(_shadow.gameObject);
        _shadow = null;
        _measured = false;
        _stillFrames = 0;
        _lastScale = slide.lossyScale;
        Pose = default;
        _renderers = GetComponentsInChildren<Renderer>(true);
    }

    /// <summary>
    /// Its rest, once it stands still there: the footprint and base of its
    /// slide's showing renderers in the desk's frame, its height (the
    /// squash's base), and its contact shadow made for that footprint (made
    /// again when measured again).
    /// </summary>
    private void Measure(DeskFrame frame)
    {
        bool any = false;
        Bounds world = default;
        foreach (Renderer r in _slide.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled)
                continue;
            if (any)
                world.Encapsulate(r.bounds);
            else
                world = r.bounds;
            any = true;
        }
        _measured = true;
        _measuredScale = _slide.lossyScale;
        if (_shadow != null)
            Destroy(_shadow.gameObject);
        _shadow = null;
        if (!any)
            return;
        DeskRect footprint = frame.Footprint(world);
        _restCentre = new Vector2(footprint.CentreX, footprint.CentreY);
        _restBase = world.min.y - frame.Origin.y;
        _height = world.size.y;
        _pivotAboveBase = _slide.position.y - world.min.y;
        if (_shadowMaterial == null || _slides == null)
            return;
        float blur = _slider.kind == SlideKind.Lift ? _slides.liftShadowBlurPx : _slides.trackShadowBlurPx;
        _shadow = SlideShadow.Make(_shadowParent, $"{_slider.id}Shadow", _shadowMaterial, new Vector2(footprint.Width, footprint.Height), blur * _slider.MetresPerPx);
    }

    /// <summary>Early in the frame (before the owners move it): the tip, the squash and a layered offset come off.</summary>
    private void Update() => TakeBack();

    /// <summary>Late in the frame (after the owners and DeskSlideIn): whether its scale stands still, then the pose goes on.</summary>
    private void LateUpdate()
    {
        Watch();
        Show();
    }

    private void OnDisable()
    {
        TakeBack();
        if (_slide != null && !_layered)
            _slide.localPosition = _restLocal;
        if (_secondary != null)
            _secondary.localPosition = _secondaryRest;
        _rider = null;
        _stillFrames = 0;
        if (_shadow != null)
            _shadow.Hide();
    }

    private void OnDestroy()
    {
        if (_shadow != null)
            Destroy(_shadow.gameObject);
    }

    /// <summary>Counts the frames its scale (its owners' and parents', the squash taken off) has stood still: one popping in from the crate is measured only once it settles.</summary>
    private void Watch()
    {
        if (_slide == null)
            return;
        Vector3 scale = _slide.lossyScale;
        _stillFrames = Same(scale, _lastScale) ? Mathf.Min(_stillFrames + 1, SettleFrames) : 0;
        _lastScale = scale;
    }

    /// <summary>True when two scales agree within a ten-thousandth.</summary>
    private static bool Same(Vector3 a, Vector3 b) => (a - b).sqrMagnitude <= 1e-8f * Mathf.Max(1f, b.sqrMagnitude);

    /// <summary>Takes off what Show put on as a layer.</summary>
    private void TakeBack()
    {
        if (!_laid || _slide == null)
            return;
        _laid = false;
        _slide.localRotation = _preRot;
        _slide.localScale = _preScale;
        if (_layered || _dropped)
            _slide.localPosition = _prePos;
        _dropped = false;
    }

    /// <summary>Puts this frame's pose on the slide: measured first once it stands still at home (again if its scale settled elsewhere); the travel (SlideCurve.Offset: a track's along its home's way in the desk's frame, a lift's up), never past its felt gap (ProximityAssist); a riding paper by the same move; the tip, the squash, the mic's sway and the contact shadow.</summary>
    private void Show()
    {
        if (_slide == null || _slider == null || !DeskLayout.Bound)
            return;
        DeskFrame frame = DeskLayout.Frame;
        SlidePose pose = Pose;
        if (Present && _stillFrames >= SettleFrames && pose.Phase == SlidePhase.Rest && Mathf.Abs(pose.Position) < 1e-6f && (!_measured || !Same(_slide.lossyScale, _measuredScale)))
            Measure(frame);
        (float right, float up) offset = SlideCurve.Offset(_slider, _way, pose.Position);
        Vector3 move = ToParent(frame.Right * offset.right + Vector3.up * offset.up);
        float metresPerPx = _slider.MetresPerPx;

        if (!_layered)
        {
            // The slide's own step since its last frame (not a read of the transform, which another writer such as a reaction's pulse may have touched): a riding paper takes the same step.
            Vector3 step = move - _moved;
            _slide.localPosition = _restLocal + move;
            _moved = move;
            if (_rider != null && (!_rider.isActiveAndEnabled || _rider.IsSliding))
                _rider = null; // its own carry took it: never two movers
            if (_rider != null)
                _rider.transform.position += _slide.parent != null ? _slide.parent.TransformVector(step) : step;
        }
        if (pose.Phase == SlidePhase.Rest)
            _rider = null;

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
        // The squash about its base (UNITY_PLAN 5.1: base down, the top comes down): the height scales about the pivot, and the pivot comes down by what that lifts the base.
        float squash = SlideCurve.SquashScale(pose.Squash, metresPerPx, _height);
        _shownUp = offset.up;
        _shownSquash = (1f - squash) * _height;
        if (squash < 1f)
        {
            _slide.localScale = new Vector3(_preScale.x, _preScale.y * squash, _preScale.z);
            float drop = SlideCurve.SquashDrop(squash, _pivotAboveBase);
            if (drop > 0f)
            {
                _slide.localPosition += ToParent(Vector3.down * drop);
                _dropped = true;
            }
        }
        if (_secondary != null)
            _secondary.localPosition = _secondaryRest + ToParent(frame.Right * (_way * pose.Sway * metresPerPx), _secondary);
        if (_shadow != null)
        {
            (float x, float y) centre = SlideCurve.ShadowCentre(_slider, _way, pose.Position, (_restCentre.x, _restCentre.y));
            _shadow.Show(frame, frame.Point(new Vector2(centre.x, centre.y), _restBase + (_slides != null ? _slides.shadowLift : 0f)), pose.Shadow, pose.ShadowBlur * metresPerPx, pose.ShadowWiden);
        }
    }

    /// <summary>A world move in the slide's parent's space.</summary>
    private Vector3 ToParent(Vector3 world) => ToParent(world, _slide);

    private static Vector3 ToParent(Vector3 world, Transform t) => t.parent != null ? t.parent.InverseTransformVector(world) : world;
}
