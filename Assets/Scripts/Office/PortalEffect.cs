using UnityEngine;

/// <summary>
/// One portal ring's vortex in the anime hall (Saleh's demo 1018a: "the vfx
/// for portals looks bad and is not even alligned"; the portals spec v3 VX1,
/// VX3-VX6; HALL-FX BUG-26a, 64c): a quad drawn with TimeDesk/PortalGlow on its
/// ring's opening as the hall drawing paints it, so it sits exactly in the
/// ring at every aspect (it follows the drawing's transform each frame,
/// through the art's pan): the layered cel swirl, ink line, rim and sparkles
/// of an open departure portal (cyan), the Return Gate's own (amber, winding
/// inward), or nothing (CLOSED, under maintenance). It fills exactly the hole
/// the ring's metal encloses, clipped by the ring's painted-aperture mask
/// (HallPortalsSO.masks): the rails and bars in front keep their painted
/// pixels and the part behind the bay's glass shows through it; nothing is
/// drawn on the metal. A change of state springs the iris open or shut
/// (PortalVortex.Iris over the tuning's irisSeconds); a departure flares it
/// (<see cref="Pulse"/>: brighter, a shock ring running to the rim, over the
/// asset's seconds; one step with reduced motion); at night it reads
/// brighter, with more sparkles (<see cref="Night"/>, HallLightingRig.PortalNight).
/// The motion preference is read live every frame (BUG-64c: turning Reduced
/// Motion on stills an open vortex at once and finishes its iris), and a
/// frame allocates nothing. AnimeHallPortalLink places and shows it; the
/// gameplay layer builds one per HallPortalsSO ring (Build Office UI), hidden
/// until then.
/// </summary>
public sealed class PortalEffect : MonoBehaviour
{
    /// <summary>The vortex's quad (the built-in quad, its material TimeDesk/PortalGlow), disabled while shut.</summary>
    [SerializeField] private MeshRenderer vortex;

    /// <summary>The hall's portals (HallPortals.asset: the rings, their masks and the vortex's tuning), the same on every effect.</summary>
    [SerializeField] private HallPortalsSO portals;

    private static readonly int MaskId = Shader.PropertyToID("_Mask");
    private static readonly int MaskStId = Shader.PropertyToID("_MaskST");
    private static readonly int MaskRectId = Shader.PropertyToID("_MaskRect");
    private static readonly int ShapeId = Shader.PropertyToID("_Shape");
    private static readonly int DeepId = Shader.PropertyToID("_Deep");
    private static readonly int MidId = Shader.PropertyToID("_Mid");
    private static readonly int LightId = Shader.PropertyToID("_Light");
    private static readonly int RimId = Shader.PropertyToID("_Rim");
    private static readonly int InkId = Shader.PropertyToID("_Ink");
    private static readonly int SwirlId = Shader.PropertyToID("_Swirl");
    private static readonly int LifeId = Shader.PropertyToID("_Life");
    private static readonly int GlowId = Shader.PropertyToID("_Glow");

    private MaterialPropertyBlock _block;
    private Transform _frame;
    private Rect _local;
    private Texture _mask;
    private Vector4 _maskMap;
    private Vector4 _maskRect;
    private float _reach = 1f;
    private PortalVortexTuning _tuning;
    private bool _open;
    private bool _returnGate;
    private float _reveal;
    private float _angle;
    private float _drift;
    private float _pulseSeconds = 1f;
    private float _pulseAge = -1f;

    /// <summary>How far night has come for this ring (0 by day to 1; HallLightingRig.PortalNight, set by AnimeHallPortalLink each frame).</summary>
    public float Night { get; set; }

    /// <summary>The hall's portals this effect was built from (AnimeHallPortalLink reads the rings from it).</summary>
    public HallPortalsSO Portals => portals;

    /// <summary>
    /// Puts the vortex on its opening: <paramref name="local"/> is its quad
    /// (PortalVortex.LocalQuad) in <paramref name="frame"/>'s local space,
    /// followed each frame; <paramref name="mask"/> is the ring's mask atlas
    /// (null: no mask, the vortex fills its ellipse), read at the quad's uv
    /// times <paramref name="maskMap"/>'s (W, H) plus its (X, Y)
    /// (PortalVortex.MaskMap) inside the ring's crop <paramref name="maskRect"/>;
    /// the hole reaches <paramref name="reach"/> opening radii; drawn on the
    /// sorting layer <paramref name="sortingLayerId"/> at <paramref name="sortingOrder"/>.
    /// </summary>
    public void Place(Transform frame, Rect local, Texture mask, PortalVortex.Box maskMap, Rect maskRect, float reach, int sortingLayerId, int sortingOrder)
    {
        _frame = frame;
        _local = local;
        _mask = mask;
        _maskMap = new Vector4(maskMap.W, maskMap.H, maskMap.X, maskMap.Y);
        _maskRect = new Vector4(maskRect.x, maskRect.y, maskRect.width, maskRect.height);
        _reach = Mathf.Clamp(reach, 1f, PortalVortex.Margin);
        if (vortex != null)
        {
            vortex.sortingLayerID = sortingLayerId;
            vortex.sortingOrder = sortingOrder;
        }
        Follow();
    }

    /// <summary>
    /// Opens the vortex (<paramref name="open"/>: an open departure ring, or the
    /// Return Gate's when <paramref name="returnGate"/>) or shuts it (CLOSED,
    /// under maintenance), in the <paramref name="tuning"/>'s look; a departure
    /// flares it over <paramref name="pulseSeconds"/>. The iris springs to the new state.
    /// </summary>
    public void Show(bool open, bool returnGate, PortalVortexTuning tuning, float pulseSeconds)
    {
        _open = open;
        _returnGate = returnGate;
        _tuning = tuning;
        _pulseSeconds = Mathf.Max(0.05f, pulseSeconds);
        _pulseAge = -1f;
    }

    /// <summary>True while it is open (its iris opening, open, or a departure portal's or the Return Gate's vortex showing).</summary>
    public bool Showing => _open;

    /// <summary>A traveller left through this ring: the vortex flares (nothing while it is shut).</summary>
    public void Pulse()
    {
        if (!_open)
            return;
        _pulseAge = 0f;
    }

    private void LateUpdate()
    {
        if (vortex == null || _tuning == null)
            return;
        float dt = Time.deltaTime;
        bool reduced = MotionPreference.Reduced;
        _reveal = PortalVortex.Reveal(_reveal, _open, dt, _tuning.irisSeconds, reduced);
        bool shown = _reveal > 0f;
        if (vortex.enabled != shown)
            vortex.enabled = shown;
        if (!shown)
            return;

        float spin = _returnGate ? -_tuning.spinDegrees * 0.8f : _tuning.spinDegrees;
        _angle = PortalVortex.Turn(_angle, spin, dt, reduced);
        if (!reduced)
            _drift = Mathf.Repeat(_drift + _tuning.sparkleDrift * dt, 64f);

        float flare = 0f, shock = -1f;
        if (_pulseAge >= 0f)
        {
            _pulseAge += dt;
            float progress = _pulseAge / _pulseSeconds;
            flare = PortalVortex.Pulse(progress, reduced);
            if (!reduced && progress < 1f)
                shock = 0.2f + 0.85f * progress;
            if (progress >= 1f)
                _pulseAge = -1f;
        }

        Follow();
        Write(flare, shock);
    }

    /// <summary>The quad on its opening: centred, turned and sized with the drawing it is measured on.</summary>
    private void Follow()
    {
        if (_frame == null)
            return;
        Transform t = transform;
        t.SetPositionAndRotation(_frame.TransformPoint(_local.center), _frame.rotation);
        Vector3 scale = _frame.lossyScale;
        t.localScale = new Vector3(scale.x * _local.width, scale.y * _local.height, 1f);
    }

    /// <summary>The shader's inputs for this frame (a property block: no allocation after the first).</summary>
    private void Write(float flare, float shock)
    {
        _block ??= new MaterialPropertyBlock();
        vortex.GetPropertyBlock(_block);
        PortalVortexTuning k = _tuning;
        if (_mask != null)
            _block.SetTexture(MaskId, _mask);
        _block.SetVector(MaskStId, _maskMap);
        _block.SetVector(MaskRectId, _maskRect);
        _block.SetVector(ShapeId, new Vector4(_reach, k.glassOpacity, k.opacity, _mask != null ? 1f : 0f));
        _block.SetColor(DeepId, _returnGate ? k.returnDeep : k.departureDeep);
        _block.SetColor(MidId, _returnGate ? k.returnMid : k.departureMid);
        _block.SetColor(LightId, _returnGate ? k.returnLight : k.departureLight);
        _block.SetColor(RimId, _returnGate ? k.returnRim : k.departureRim);
        _block.SetColor(InkId, k.ink);
        float twist = _returnGate ? -k.twist : k.twist;
        _block.SetVector(SwirlId, new Vector4(_returnGate ? k.returnArms : k.departureArms, twist, _angle, _drift));
        _block.SetVector(LifeId, new Vector4(PortalVortex.Iris(_reveal), flare, Mathf.Clamp01(Night), shock));
        _block.SetVector(GlowId, new Vector4(k.ringGlow, k.sparkleDensity, k.brightness, k.nightBoost));
        vortex.SetPropertyBlock(_block);
    }
}
