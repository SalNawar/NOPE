using UnityEngine;

/// <summary>
/// One portal ring in the anime hall, drawn as a gate (Saleh's demo 1018a:
/// "the vfx for portals looks bad and is not even alligned"; the portals spec
/// v3 VX1, VX3-VX6; HALL-FX BUG-26a, 64c; the night of 2026-11-07, DECISIONS D6:
/// "portals", the Stargate idea): a quad drawn with TimeDesk/PortalGlow on its
/// ring's opening as the hall drawing paints it, so it sits exactly in the
/// ring at every aspect (it follows the drawing's transform each frame,
/// through the art's pan). AnimeHallPortalLink feeds it each frame
/// (<see cref="Drive"/>: its switchboard module, or the day's look) and its
/// PortalGateTrack turns that into the gate's states: offline, nothing (a
/// dark ring, no halo); locked in, the chevrons on the ring's metal light one
/// by one over the calibration, each in one step and held; open, the event
/// horizon, a slow rippling pool in two cel tones of the ring's light (the
/// Return Gate's warm one) with a soft rim glow, every chevron lit; a send
/// (<see cref="Send"/>), the kawoosh, a burst out from the core and a settle,
/// with the flare of the bloom and its sound (the cue portal_kawoosh, else a
/// placeholder); closing, the horizon and the chevrons fade out. The event
/// horizon fills exactly the hole the ring's metal encloses, clipped by the
/// ring's painted-aperture mask (HallPortalsSO.masks): the rails and bars in
/// front keep their painted pixels and the part behind the bay's glass shows
/// through it; only the chevrons are drawn on the metal. With
/// HallPortalsSO.gate.eventHorizon off an open ring shows the old cel swirl
/// instead (its flare at a send). At night it reads brighter
/// (<see cref="Night"/>, HallLightingRig.PortalNight). The motion preference
/// is read live every frame (BUG-64c: Reduced Motion keeps steady states and
/// cuts between them, the pool still), and a frame allocates nothing. It runs
/// after the link (its execution order), so a feed and a send set this frame
/// are drawn this frame. The gameplay layer builds one per HallPortalsSO ring
/// (Build Office UI), hidden until the link places it.
/// </summary>
[DefaultExecutionOrder(10)]
public sealed class PortalEffect : MonoBehaviour
{
    /// <summary>The vortex's quad (the built-in quad, its material TimeDesk/PortalGlow), disabled while nothing shows.</summary>
    [SerializeField] private MeshRenderer vortex;

    /// <summary>The hall's portals (HallPortals.asset: the rings, their masks, the vortex's and the gate's tuning), the same on every effect.</summary>
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
    private static readonly int GateId = Shader.PropertyToID("_Gate");
    private static readonly int PoolId = Shader.PropertyToID("_Pool");
    private static readonly int KawooshId = Shader.PropertyToID("_Kawoosh");
    private static readonly int ChevronRingId = Shader.PropertyToID("_ChevronRing");
    private static readonly int ChevronId = Shader.PropertyToID("_Chevron");
    private static readonly int ChevronShapeId = Shader.PropertyToID("_ChevronShape");
    private static readonly int ChevronLitId = Shader.PropertyToID("_ChevronLit");
    private static readonly int ChevronDarkId = Shader.PropertyToID("_ChevronDark");

    private readonly PortalGateTrack _gate = new PortalGateTrack();
    private PortalGateFeed _feed;
    private MaterialPropertyBlock _block;
    private Transform _frame;
    private Rect _local;
    private Texture _mask;
    private Vector4 _maskMap;
    private Vector4 _maskRect;
    private Vector4 _chevronRing = new Vector4(0f, 0f, 1.22f, 1.22f);
    private Vector3 _chevronShape = new Vector3(40f * Mathf.Deg2Rad, 0.14f, 1f);
    private float _reach = 1f;
    private bool _returnGate;
    private Color _tone = new Color(0.35f, 0.85f, 1f);
    private float _fadeSeconds = 0.8f;
    private float _reveal;
    private float _angle;
    private float _drift;
    private float _ripple;
    private float _wander;
    private AudioSource _sound;

    /// <summary>How far night has come for this ring (0 by day to 1; HallLightingRig.PortalNight, set by AnimeHallPortalLink each frame).</summary>
    public float Night { get; set; }

    /// <summary>The hall's portals this effect was built from (AnimeHallPortalLink reads the rings from it).</summary>
    public HallPortalsSO Portals => portals;

    /// <summary>The gate's track: its state, chevrons, event horizon and kawoosh this frame (the QA rig's columns read it).</summary>
    public PortalGateTrack Gate => _gate;

    /// <summary>True while its event horizon shows (opening, open, sending or fading out).</summary>
    public bool Showing => _gate.Pool > 0f;

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
    /// Puts the chevrons on the ring's metal: <paramref name="ring"/> is their
    /// ellipse in the opening's radii (PortalVortex.ChevronRing), the sites
    /// <paramref name="stepDegrees"/> apart, each <paramref name="halfWidth"/>
    /// of the mean opening radius wide each side, on an opening whose radius
    /// across is <paramref name="aspect"/> times its radius up.
    /// </summary>
    public void PlaceChevrons(PortalVortex.Box ring, float stepDegrees, float halfWidth, float aspect)
    {
        _chevronRing = new Vector4(ring.X, ring.Y, ring.W, ring.H);
        _chevronShape = new Vector3(stepDegrees * Mathf.Deg2Rad, Mathf.Max(1e-3f, halfWidth), Mathf.Max(1e-3f, aspect));
    }

    /// <summary>
    /// Sets the ring's colours: the Return Gate's look when
    /// <paramref name="returnGate"/> (the old swirl's amber), the event
    /// horizon's two tones made from <paramref name="tone"/> (the ring's light:
    /// HallLightingSO.portalOpenLight or returnGateLight), and the close's
    /// <paramref name="fadeSeconds"/> (HallLightingSO.portalFadeSeconds).
    /// </summary>
    public void Look(bool returnGate, Color tone, float fadeSeconds)
    {
        _returnGate = returnGate;
        _tone = tone;
        _fadeSeconds = Mathf.Max(0f, fadeSeconds);
    }

    /// <summary>What drives the ring this frame (AnimeHallPortalLink): its module's dial, or the day's look.</summary>
    public void Drive(in PortalGateFeed feed) => _feed = feed;

    /// <summary>A group went through this ring: the kawoosh plays once its event horizon is open and the dwell has passed (nothing once it has closed).</summary>
    public void Send() => _gate.Send();

    private void LateUpdate()
    {
        if (vortex == null || portals == null)
            return;
        PortalGateTuning g = portals.gate;
        PortalVortexTuning k = portals.vortex;
        float dt = Time.deltaTime;
        bool reduced = MotionPreference.Reduced;
        PortalGateTimes times = Times(g);
        _gate.Step(_feed, times, dt, reduced);
        if (_gate.BurstStarted)
            Sound(g);

        bool chevrons = g.showChevrons && _gate.Chevrons > 0f;
        bool shown = _gate.Pool > 0f || chevrons;
        if (vortex.enabled != shown)
            vortex.enabled = shown;
        if (!shown)
            return;

        if (!reduced)
        {
            float spin = _returnGate ? -k.spinDegrees * 0.8f : k.spinDegrees;
            _angle = PortalVortex.Turn(_angle, spin, dt, false);
            _drift = Mathf.Repeat(_drift + k.sparkleDrift * dt, 64f);
            _ripple = Mathf.Repeat(_ripple + g.rippleHz * dt, 64f);
            _wander = Mathf.Repeat(_wander + g.wobbleHz * dt, 1f);
        }
        _reveal = PortalVortex.Reveal(_reveal, _feed.Horizon, dt, k.irisSeconds, reduced);

        Follow();
        Write(g, k, times, reduced, chevrons);
    }

    /// <summary>The gate's times from its knobs (the old swirl's flare on HallPortalsSO.pulseSeconds), the close on the hall's own fade (through Look).</summary>
    private PortalGateTimes Times(PortalGateTuning g) =>
        new PortalGateTimes(g.chevrons, g.openSeconds, _fadeSeconds, g.eventHorizon ? g.kawooshSeconds : portals.pulseSeconds,
                            g.eventHorizon ? g.kawooshPeak : 0.5f, g.dwellSeconds);

    /// <summary>The kawoosh's sound: the bank's portal_kawoosh, else its placeholder through this ring's own source.</summary>
    private void Sound(PortalGateTuning g)
    {
        if (_sound == null)
        {
            _sound = gameObject.AddComponent<AudioSource>();
            _sound.playOnAwake = false;
            _sound.spatialBlend = 0f;
            if (Sounds.Bank != null)
                _sound.outputAudioMixerGroup = Sounds.Bank.deskGroup;
        }
        _sound.volume = g.kawooshVolume;
        CueSounds.Play(SoundCues.PortalKawoosh, _sound);
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
    private void Write(PortalGateTuning g, PortalVortexTuning k, PortalGateTimes times, bool reduced, bool chevrons)
    {
        _block ??= new MaterialPropertyBlock();
        vortex.GetPropertyBlock(_block);
        if (_mask != null)
            _block.SetTexture(MaskId, _mask);
        _block.SetVector(MaskStId, _maskMap);
        _block.SetVector(MaskRectId, _maskRect);
        _block.SetVector(ShapeId, new Vector4(_reach, k.glassOpacity, k.opacity, _mask != null ? 1f : 0f));
        _block.SetColor(InkId, k.ink);

        float envelope = _gate.Envelope(times, reduced);
        if (g.eventHorizon)
        {
            // The event horizon: two cel tones of the ring's light; a lift toward white for the rim glow and the kawoosh's heart.
            Color dark = _tone * g.poolDark;
            dark.a = 1f;
            _block.SetColor(DeepId, dark);
            _block.SetColor(MidId, dark);
            _block.SetColor(LightId, Color.Lerp(_tone, Color.white, g.poolLift));
            _block.SetColor(RimId, Color.Lerp(_tone, Color.white, 0.65f));
            float front = _gate.Burst >= 0f ? PortalGate.Front(_gate.Burst, times.KawooshPeak, 0.2f, g.kawooshReach) : 0.2f;
            _block.SetVector(GateId, new Vector4(1f, _ripple, _gate.Pool, envelope));
            _block.SetVector(PoolId, new Vector4(g.rippleRings, g.wobble, _wander, g.rippleShare));
            _block.SetVector(KawooshId, new Vector4(front, g.kawooshFlare, g.rimGlow, g.poolCore));
            _block.SetVector(LifeId, new Vector4(_gate.Iris, 0f, Mathf.Clamp01(Night), -1f));
        }
        else
        {
            // The old cel swirl (HallPortalsSO.vortex), its flare at a send.
            _block.SetColor(DeepId, _returnGate ? k.returnDeep : k.departureDeep);
            _block.SetColor(MidId, _returnGate ? k.returnMid : k.departureMid);
            _block.SetColor(LightId, _returnGate ? k.returnLight : k.departureLight);
            _block.SetColor(RimId, _returnGate ? k.returnRim : k.departureRim);
            float shock = !reduced && _gate.Burst >= 0f ? 0.2f + 0.85f * _gate.Burst : -1f;
            _block.SetVector(GateId, new Vector4(0f, 0f, _gate.Pool, 0f));
            _block.SetVector(LifeId, new Vector4(PortalVortex.Iris(_reveal), envelope, Mathf.Clamp01(Night), shock));
        }
        float twist = _returnGate ? -k.twist : k.twist;
        _block.SetVector(SwirlId, new Vector4(_returnGate ? k.returnArms : k.departureArms, twist, _angle, _drift));
        _block.SetVector(GlowId, new Vector4(k.ringGlow, k.sparkleDensity, k.brightness, k.nightBoost));

        _block.SetVector(ChevronRingId, _chevronRing);
        _block.SetVector(ChevronId, new Vector4(_chevronShape.x, _chevronShape.y, _gate.Lit, chevrons ? _gate.Chevrons : 0f));
        _block.SetVector(ChevronShapeId, new Vector4(g.chevrons, _chevronShape.z, 0f, 0f));
        _block.SetColor(ChevronLitId, g.chevronLit);
        _block.SetColor(ChevronDarkId, g.chevronDark);
        vortex.SetPropertyBlock(_block);
    }
}
