using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws the Helix River (timeline stability without a number; the pure
/// HelixRiver maps it, the TimeDesk/HelixRiver shader draws it) on this
/// object: a uGUI graphic (the taskbar strip, the shift report's panel, the
/// fallback HUD, the Home HUD) or a renderer (the quad the office binder lays
/// on the desk's stability monitor, <see cref="Cover"/>). Each frame it reads
/// the run's stability, today's change (the shift ledger) and the firing and
/// warning lines (GameConfigSO), steps the river by unscaled time and sets
/// the material's parameters; a citation (GameManager.CitationAcknowledged,
/// when the slip is dismissed and the office shows again) sends its red
/// pulse; Reduced Motion (MotionPreference) slows it. Its material is its
/// own, made once at load; the frame allocates nothing.
/// </summary>
[DisallowMultipleComponent]
public sealed class HelixRiverMonitor : MonoBehaviour
{
    /// <summary>The river's knobs (HelixRiver_Default).</summary>
    [SerializeField] private HelixRiverSO settings;

    /// <summary>TimeDesk/HelixRiver.</summary>
    [SerializeField] private Shader shader;

    /// <summary>The shift (citations and today's change); null in Home, where neither happens.</summary>
    [SerializeField] private GameManager game;

    /// <summary>How much of the reference glass shows (1: all of it; 2: the middle half, for a thin strip).</summary>
    [SerializeField, Min(0.1f)] private float zoom = 1f;

    /// <summary>The glass's corner radius, as a share of its height (0: square).</summary>
    [SerializeField, Range(0f, 0.5f)] private float corner;

    private static readonly int CalmId = Shader.PropertyToID("_Calm");
    private static readonly int TimeId = Shader.PropertyToID("_RiverTime");
    private static readonly int MeanderId = Shader.PropertyToID("_Meander");
    private static readonly int TwistId = Shader.PropertyToID("_Twist");
    private static readonly int UnzipId = Shader.PropertyToID("_Unzip");
    private static readonly int UnzipStartId = Shader.PropertyToID("_UnzipStart");
    private static readonly int SnapId = Shader.PropertyToID("_Snap");
    private static readonly int MutateId = Shader.PropertyToID("_Mutate");
    private static readonly int OxbowsId = Shader.PropertyToID("_Oxbows");
    private static readonly int FrayId = Shader.PropertyToID("_Fray");
    private static readonly int FlowId = Shader.PropertyToID("_Flow");
    private static readonly int StrayId = Shader.PropertyToID("_Stray");
    private static readonly int DriftId = Shader.PropertyToID("_Drift");
    private static readonly int GlitchId = Shader.PropertyToID("_Glitch");
    private static readonly int FlickerId = Shader.PropertyToID("_Flicker");
    private static readonly int PulseId = Shader.PropertyToID("_Pulse");
    private static readonly int PulseGlowId = Shader.PropertyToID("_PulseGlow");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int ZoomId = Shader.PropertyToID("_Zoom");
    private static readonly int CornerId = Shader.PropertyToID("_Corner");

    private HelixRiver _river;
    private Material _material;
    private RectTransform _rect;
    private bool _reduced;

    private void Awake()
    {
        _river = new HelixRiver(settings != null ? settings.knobs : null);
        if (shader == null)
        {
            Debug.LogWarning($"[HelixRiverMonitor] '{name}' has no shader (TimeDesk/HelixRiver): the river is not drawn. Rebuild with Tools > TimeDesk > Build Office UI.", this);
            enabled = false;
            return;
        }

        _material = new Material(shader) { name = "HelixRiver (runtime)", hideFlags = HideFlags.DontSave };
        if (TryGetComponent(out Graphic graphic))
        {
            graphic.material = _material;
            _rect = graphic.rectTransform;
        }
        else if (TryGetComponent(out Renderer screen))
        {
            screen.sharedMaterial = _material;
        }
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }

    private void OnEnable()
    {
        _reduced = MotionPreference.Reduced;
        MotionPreference.Changed += HandleMotionChanged;
        if (game != null)
            game.CitationAcknowledged += HandleCitation;
    }

    private void OnDisable()
    {
        MotionPreference.Changed -= HandleMotionChanged;
        if (game != null)
            game.CitationAcknowledged -= HandleCitation;
    }

    private void HandleMotionChanged() => _reduced = MotionPreference.Reduced;

    private void HandleCitation(CaseVerdict verdict) => _river.Citation();

    /// <summary>
    /// Lays this river (a quad renderer) over <paramref name="readout"/> (the
    /// art office's stability text, ReadoutStability): its place, turn and
    /// box, its layer and sorting, then hides the text, so the monitor's
    /// glass shows the river instead of a number. The art object gets no
    /// component of ours; only its text is switched off.
    /// </summary>
    public void Cover(TMP_Text readout)
    {
        if (readout == null)
            return;
        RectTransform box = readout.rectTransform;
        Rect area = box.rect;
        transform.SetPositionAndRotation(box.TransformPoint(area.center), box.rotation);
        Vector3 boxScale = box.lossyScale;
        Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
        transform.localScale = new Vector3(area.width * boxScale.x / parentScale.x, area.height * boxScale.y / parentScale.y, 1f);
        gameObject.layer = readout.gameObject.layer;
        if (TryGetComponent(out Renderer screen) && readout.TryGetComponent(out Renderer text))
        {
            screen.sortingLayerID = text.sortingLayerID;
            screen.sortingOrder = text.sortingOrder;
        }
        readout.enabled = false;
    }

    private void Update()
    {
        if (_material == null || !RunManager.HasInstance)
            return;
        WorldState world = RunManager.Instance.World;
        if (world == null)
            return;

        GameConfigSO config = RunManager.Instance.Config != null ? RunManager.Instance.Config.gameConfig : null;
        ShiftLedger ledger = game != null ? game.Ledger : null;
        var input = new HelixRiverInput(world.timelineStability,
                                        config != null ? config.firedAtStability : 0f,
                                        config != null ? config.stabilityWarningMargin : 0f,
                                        config != null ? config.stabilityCriticalMargin : 0f,
                                        ledger != null ? ledger.TotalStabilityDelta : 0f,
                                        _reduced);
        Apply(_river.Step(Time.unscaledDeltaTime, input));
    }

    /// <summary>Hands one frame of the river to the material, with this display's shape.</summary>
    private void Apply(in HelixRiverFrame frame)
    {
        _material.SetFloat(CalmId, frame.Calm);
        _material.SetFloat(TimeId, frame.Time);
        _material.SetFloat(MeanderId, frame.Meander);
        _material.SetFloat(TwistId, frame.Twist);
        _material.SetFloat(UnzipId, frame.Unzip);
        _material.SetFloat(UnzipStartId, frame.UnzipStart);
        _material.SetFloat(SnapId, frame.Snap);
        _material.SetFloat(MutateId, frame.Mutate);
        _material.SetFloat(OxbowsId, frame.Oxbows);
        _material.SetFloat(FrayId, frame.Fray);
        _material.SetFloat(FlowId, frame.Flow);
        _material.SetFloat(StrayId, frame.Stray);
        _material.SetFloat(DriftId, frame.Drift);
        _material.SetFloat(GlitchId, frame.Glitch);
        _material.SetFloat(FlickerId, frame.Flicker);
        _material.SetFloat(PulseId, frame.Pulse);
        _material.SetFloat(PulseGlowId, frame.PulseGlow);
        _material.SetFloat(AspectId, Aspect());
        _material.SetFloat(ZoomId, zoom);
        _material.SetFloat(CornerId, corner);
    }

    /// <summary>The display's width over its height: the graphic's rect, or the quad's scale.</summary>
    private float Aspect()
    {
        if (_rect != null)
        {
            Rect r = _rect.rect;
            return r.height > 0.01f ? r.width / r.height : 1f;
        }
        Vector3 s = transform.lossyScale;
        return Mathf.Abs(s.y) > 1e-5f ? Mathf.Abs(s.x / s.y) : 1f;
    }
}
