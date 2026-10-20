using UnityEngine;

/// <summary>
/// The desk's one light (BUG-14), published for every desk shader
/// (Shaders/Include/DeskLight.hlsl): each frame it reads the hall's hour and
/// knobs (HallLightingRig, HallLightingSO's "The desk" header), evaluates
/// DeskLighting (the room's level and colour falling from the full day to the
/// night floor, plus the work lamp's warm share) and sets the globals
/// _DeskLightTint (its alpha marks the light as published), _DeskLightLevel,
/// _DeskLightKey, _DeskLampPool (the pool's centre on the mat and its radius)
/// and _DeskLampShare, writing them only when they change. The lamp switches
/// on at the afternoon dip (the hall's evening reaches the lamp's knob) with a
/// click and a filament's warm-up (at once with reduced motion; a lamp already
/// on when the office opens is simply on). <see cref="Light"/> is the same
/// uniform light for the traveller (AnimeHallShiftLink) that DeskLight() gives
/// the passport photo, so the photo check is fair at every hour. Made at run
/// time by the rig; while it is off, the globals fall back to today's look.
/// When it is bound it checks the knobs against the real mat once: at full
/// night the mat's corners (the farthest from the pool) must reach
/// DeskLighting.ReadableNightLuminance (luma 55), else it warns once.
/// </summary>
[DisallowMultipleComponent]
public sealed class DeskLightLevel : MonoBehaviour
{
    /// <summary>The desk light while one is enabled (the office in play); null otherwise.</summary>
    public static DeskLightLevel Live { get; private set; }

    /// <summary>The uniform desk light (colour times level; white while unpublished): what the traveller is tinted by.</summary>
    public Color Light { get; private set; } = Color.white;

    private static readonly int TintId = Shader.PropertyToID("_DeskLightTint");
    private static readonly int LevelId = Shader.PropertyToID("_DeskLightLevel");
    private static readonly int KeyId = Shader.PropertyToID("_DeskLightKey");
    private static readonly int PoolId = Shader.PropertyToID("_DeskLampPool");
    private static readonly int ShareId = Shader.PropertyToID("_DeskLampShare");

    /// <summary>The smallest change that is written again (half of one 8-bit step).</summary>
    private const float Step = 0.002f;

    private HallLightingRig _rig;
    private AudioSource _click;
    private Vector3 _poolCentre;
    private float _since = -1f;
    private bool _first = true;
    private Vector4 _tint = new Vector4(-1f, -1f, -1f, -1f), _pool;
    private float _level = -1f, _key = -1f, _share = -1f;

    /// <summary>Binds the hall's rig (its hour and knobs), the lamp pool's centre on the mat (world) and the mat's size (metres; zero when the art gives none), and checks the knobs against that mat.</summary>
    public void Configure(HallLightingRig rig, Vector3 poolCentre, Vector2 matSize)
    {
        _rig = rig;
        _poolCentre = poolCentre;
        _click = GetComponent<AudioSource>();
        _first = true;
        HallLightingSO settings = rig != null ? rig.Settings : null;
        if (settings == null || matSize.x <= 0f || matSize.y <= 0f)
            return;
        DeskLighting.Light night = DeskLighting.FullNight(settings.DeskKnobs);
        float corner = DeskLighting.DarkestOnMat(night, settings.deskLampRadius, matSize.x, matSize.y);
        if (corner < DeskLighting.ReadableNightLuminance)
            Debug.LogWarning($"[DeskLightLevel] At night the mat's corners read at about luma {DeskLighting.LumaAt(corner):0} (below {DeskLighting.ReadableMatLuma:0}) on this {matSize.x:0.00} x {matSize.y:0.00} m mat: raise deskNightFloor, deskLampLevel or deskLampRadius in {settings.name}.", this);
    }

    private void OnEnable() => Live = this;

    private void OnDisable()
    {
        if (ReferenceEquals(Live, this))
            Live = null;
        Unpublish();
    }

    private void LateUpdate()
    {
        HallLightingSO settings = _rig != null ? _rig.Settings : null;
        if (settings == null || !settings.lightingOn)
        {
            Unpublish();
            return;
        }

        float hour = _rig.Hour;
        HallDayCycle.Settings cycle = settings.Cycle;
        DeskLighting.Knobs knobs = settings.DeskKnobs;
        bool on = DeskLighting.LampOn(hour, cycle, knobs);
        if (!on)
            _since = -1f;
        else if (_since < 0f)
        {
            // Already on as the office opens: simply on. Switched on at the dip: the click and the warm-up.
            _since = _first ? 999f : 0f;
            if (!_first)
                CueSounds.Play(SoundCues.RelayClack, _click);
        }
        else
            _since += Time.deltaTime;
        _first = false;

        float warm = HallDayCycle.WarmUp(_since, settings.deskLampWarmSeconds, MotionPreference.Reduced);
        DeskLighting.Light l = DeskLighting.Evaluate(hour, cycle, knobs, warm);
        Light = new Color(l.R * l.Level, l.G * l.Level, l.B * l.Level, 1f);
        Publish(new Vector4(l.R, l.G, l.B, 1f), l.Level, settings.deskKeyAtDay,
                new Vector4(_poolCentre.x, _poolCentre.y, _poolCentre.z, settings.deskLampRadius), l.LampShare);
    }

    /// <summary>Sets the globals that changed.</summary>
    private void Publish(Vector4 tint, float level, float key, Vector4 pool, float share)
    {
        if (Far(tint, _tint)) Shader.SetGlobalVector(TintId, _tint = tint);
        if (Mathf.Abs(level - _level) > Step) Shader.SetGlobalFloat(LevelId, _level = level);
        if (Mathf.Abs(key - _key) > Step) Shader.SetGlobalFloat(KeyId, _key = key);
        if (Far(pool, _pool)) Shader.SetGlobalVector(PoolId, _pool = pool);
        if (Mathf.Abs(share - _share) > Step) Shader.SetGlobalFloat(ShareId, _share = share);
    }

    /// <summary>Marks the desk light unpublished (DeskLight() = 1: today's look) and forgets what was written.</summary>
    private void Unpublish()
    {
        Light = Color.white;
        if (_tint.w != 0f)
            Shader.SetGlobalVector(TintId, Vector4.zero);
        _tint = Vector4.zero;
        _level = _key = _share = -1f;
        _pool = new Vector4(float.NaN, 0f, 0f, 0f);
    }

    private static bool Far(Vector4 a, Vector4 b) =>
        !(Mathf.Abs(a.x - b.x) <= Step && Mathf.Abs(a.y - b.y) <= Step && Mathf.Abs(a.z - b.z) <= Step && Mathf.Abs(a.w - b.w) <= Step);
}
