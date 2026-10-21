using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The desk machines (the desk machines spec §1-2; Saleh 2026-10-17: "the chip
/// reader should be an actual machine that reads the chip when you scan it.
/// uv light should be an actual uv flashlight you can use to see the hidden
/// watermarks and symbols"): the chip reader and the UV torch, each on the
/// desk from the day the ramp issues it (Feature.ChipReader,
/// Feature.UvTorch), with its desk note on that first day. It holds the
/// current case's hidden layers (CaseHidden) and what the machines have read
/// of them (DeskReadings), which the scanned copies' UV and CHIP tabs show
/// (Layers); gives each paper the desk hands over its UV layer
/// (UvPaperLayer); runs the reader on a paper dropped on it; and raises a
/// reading picked at the desk in inspect mode (a reader's line, a seen
/// mark's tick) for the investigation to log against its box (Picked). The
/// office binder lays both machines on the desk (Lay) and their notes over them. Build Office UI
/// builds it under the office with both machines.
/// </summary>
public sealed class DeskHardware : MonoBehaviour
{
    /// <summary>The chip reader (optional: none without its art).</summary>
    [SerializeField] private ChipReader chipReader;

    /// <summary>The UV torch (optional: none without its art).</summary>
    [SerializeField] private UvTorch uvTorch;

    /// <summary>The upgrades' objects on the desk (optional; laid with the machines).</summary>
    [SerializeField] private DeskUpgrades upgrades;

    /// <summary>The desk tuning (the machines' places, the reader's cycle, the cone, the notes).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The note by the UV torch on its first day, and the one by the chip reader on its (floating desk notes; their text from the config's keys).</summary>
    [SerializeField] private TMP_Text uvHint, chipHint;

    /// <summary>The reveal materials: a mark's quad (TimeDesk/UvReveal) and a ghost's text (TimeDesk/UvRevealText); each is copied per texture or font atlas.</summary>
    [SerializeField] private Material markMaterial, textMaterial;

    /// <summary>The reveal's textures: the Bureau's watermark, a seal's microprint ring, a found mark's tick.</summary>
    [SerializeField] private Texture2D watermarkTexture, microprintTexture, tickTexture;

    private readonly Dictionary<(Texture, int, bool), Material> _marks = new Dictionary<(Texture, int, bool), Material>();
    private readonly Dictionary<TMP_FontAsset, Material> _texts = new Dictionary<TMP_FontAsset, Material>();
    private readonly List<UvPaperLayer> _layers = new List<UvPaperLayer>();
    private readonly List<MicroPaperLayer> _micro = new List<MicroPaperLayer>();
    private bool _uvUsed, _chipUsed;

    /// <summary>What the machines have read of the current case.</summary>
    public DeskReadings Readings { get; } = new DeskReadings();

    /// <summary>The current case's hidden layers.</summary>
    public CaseHidden Hidden { get; private set; } = CaseHidden.None;

    /// <summary>True when the UV torch is on the desk today.</summary>
    public bool TorchToday { get; private set; }

    /// <summary>True when the chip reader is on the desk today.</summary>
    public bool ReaderToday { get; private set; }

    /// <summary>True while inspect mode is on (DeskController.SetInspecting): a tick or the reader's readout then logs its reading.</summary>
    public bool Inspecting { get; private set; }

    /// <summary>The scanned copies' UV and CHIP tabs: the case's hidden layers through what the machines have read, each tab from its machine's day.</summary>
    public MachineLayers Layers => new MachineLayers(Hidden, Readings, TorchToday, ReaderToday);

    /// <summary>The chip reader (null without one).</summary>
    public ChipReader Reader => chipReader;

    /// <summary>The UV torch (null without one).</summary>
    public UvTorch Torch => uvTorch;

    /// <summary>The desk tuning.</summary>
    public DeskConfigSO Config => config;

    /// <summary>A reading picked at the desk in inspect mode: the paper, the box's index in its fields, what the machine reads there and on which layer (CaseDocumentsPresenter logs it against the box).</summary>
    public event Action<int, int, string, ScanLayer> Picked;

    /// <summary>The reveal textures.</summary>
    public Texture2D WatermarkTexture => watermarkTexture;

    /// <inheritdoc cref="WatermarkTexture"/>
    public Texture2D MicroprintTexture => microprintTexture;

    /// <inheritdoc cref="WatermarkTexture"/>
    public Texture2D TickTexture => tickTexture;

    /// <summary>A unit quad facing -Z (the paper's sheet faces that way), made once.</summary>
    public static Mesh QuadMesh => _quad != null ? _quad : _quad = MakeQuad();

    private static Mesh _quad;

    private void Awake()
    {
        if (config != null)
        {
            if (uvHint != null)
                uvHint.text = UiText.Get(config.uvHintKey);
            if (chipHint != null)
                chipHint.text = UiText.Get(config.chipHintKey);
        }
        if (uvTorch != null)
            uvTorch.Bind(this);
        if (chipReader != null)
            chipReader.Bind(this);
    }

    /// <summary>
    /// Starts a day (GameManager, with the day's introductions): each machine
    /// on the desk from the day the ramp issues it, its note shown on that
    /// first day until it is first used.
    /// </summary>
    public void BeginDay(int day, Introductions introductions)
    {
        TorchToday = introductions != null && introductions.Has(day, Feature.UvTorch);
        ReaderToday = introductions != null && introductions.Has(day, Feature.ChipReader);
        if (uvTorch != null)
            uvTorch.gameObject.SetActive(TorchToday);
        if (chipReader != null)
            chipReader.gameObject.SetActive(ReaderToday);
        _uvUsed = _chipUsed = false;
        if (uvHint != null)
            uvHint.gameObject.SetActive(TorchToday && introductions.IsNew(day, Feature.UvTorch));
        if (chipHint != null)
            chipHint.gameObject.SetActive(ReaderToday && introductions.IsNew(day, Feature.ChipReader));
    }

    /// <summary>A case begins with its hidden layers: nothing read yet.</summary>
    public void BeginCase(CaseHidden hidden)
    {
        Hidden = hidden ?? CaseHidden.None;
        Readings.Clear();
        _layers.Clear();
        _micro.Clear();
        if (chipReader != null)
            chipReader.ResetCase();
        if (upgrades != null)
            upgrades.BeginCase();
    }

    /// <summary>The case ends: the reader stops, the torch goes back, the papers' layers go with their papers.</summary>
    public void EndCase()
    {
        if (chipReader != null)
            chipReader.Cancel();
        if (uvTorch != null)
            uvTorch.Release();
        _layers.Clear();
        _micro.Clear();
        if (upgrades != null)
            upgrades.EndCase();
    }

    /// <summary>A paper handed over at the desk (form <paramref name="formNumber"/>): its UV layer (the case's marks for it) and, when its form prints one, its microprint line (the desk machines spec §10).</summary>
    public void PaperAdded(DeskDocument paper, string formNumber)
    {
        if (paper == null)
            return;
        UvPaperLayer layer = paper.gameObject.AddComponent<UvPaperLayer>();
        layer.Build(paper, this, Hidden.Marks(paper.Index));
        _layers.Add(layer);
        MicroLine? line = Hidden.MicroOf(paper.Index);
        if (!line.HasValue)
            return;
        MicroPaperLayer micro = paper.gameObject.AddComponent<MicroPaperLayer>();
        micro.Build(paper, this, line.Value, formNumber);
        _micro.Add(micro);
    }

    /// <summary>A magnifier this frame (the lens stand's DeskMagnifier, cut by P0-C: no caller until the scanner shows the microprint, FIX_PLAN_v2 #17; the desk machines spec §10): every paper's microprint line counts how long it has been readable under it (MicroPaperLayer.Track); <paramref name="toLens"/> takes a desk point into the lens's frame (null, or <paramref name="on"/> false: nothing magnified).</summary>
    public void TrackLens(Func<Vector3, Vector2> toLens, LensShape shape, float halfWidth, float halfHeight, bool on, float dt)
    {
        for (int i = _micro.Count - 1; i >= 0; i--)
        {
            if (_micro[i] == null)
            {
                _micro.RemoveAt(i);
                continue;
            }
            _micro[i].Track(toLens ?? (p => new Vector2(1e6f, 1e6f)), shape, halfWidth, halfHeight, on && toLens != null, dt);
        }
    }

    /// <summary>Inspect mode on or off.</summary>
    public void SetInspecting(bool inspecting) => Inspecting = inspecting;

    /// <summary>The torch's cone this frame: every paper's layer counts its lit marks (UvPaperLayer.Track); the first use hides the torch's note.</summary>
    public void TrackCone(Vector3 centre, float radius, bool on, float dt)
    {
        if (on && !_uvUsed)
        {
            _uvUsed = true;
            if (uvHint != null)
                uvHint.gameObject.SetActive(false);
        }
        for (int i = _layers.Count - 1; i >= 0; i--)
        {
            if (_layers[i] == null)
            {
                _layers.RemoveAt(i);
                continue;
            }
            _layers[i].Track(centre, radius, on, dt);
        }
    }

    /// <summary>The chip reader as a dock for the desk's papers (IPaperDock: where a card or a passport goes into it, its casing, its "no"); null without its art.</summary>
    public IPaperDock ReaderDock => chipReader;

    /// <summary>True when the reader is busy with a paper (it refuses another).</summary>
    public bool ReaderBusy => chipReader != null && chipReader.Busy;

    /// <summary>True when paper <paramref name="document"/> of the case carries a chip (its form: HiddenContent.chipForms): the reader takes only those.</summary>
    public bool CarriesChip(int document) => Hidden.HasChip(document);


    /// <summary>The chip reader reads <paramref name="paper"/> (its print, and its chip from the case's layers); <paramref name="done"/> runs as it comes back out. False when the reader is busy (the paper goes back).</summary>
    public bool Read(DeskDocument paper, IReadOnlyList<DocumentField> print, Action done)
    {
        if (chipReader == null || paper == null || chipReader.Busy)
            return false;
        if (!_chipUsed)
        {
            _chipUsed = true;
            if (chipHint != null)
                chipHint.gameObject.SetActive(false);
        }
        return chipReader.Read(paper, print, Hidden.ChipOf(paper.Index), done);
    }

    /// <summary>A seen mark's tick clicked: in inspect mode its reading is picked against its box.</summary>
    public void TickClicked(int document, UvMark mark)
    {
        if (!Inspecting || mark.Field < 0)
            return;
        Pick(document, mark.Field, mark.Text, ScanLayer.Uv);
    }

    /// <summary>A reading picked (the reader's line, a tick): raised for the investigation to log.</summary>
    public void Pick(int document, int field, string value, ScanLayer layer) => Picked?.Invoke(document, field, value, layer);

    /// <summary>
    /// Lays the machines on their homes (the office binder, once the desk is
    /// placed; the desk home map, DeskLayoutSettings in <paramref name="frame"/>):
    /// the chip reader at the front of the rail right of the mat (at
    /// chipReaderScale, its readout facing the chair), the UV torch's cradle
    /// in its pouch left of the mat; the upgrades' objects with them
    /// (DeskUpgrades.Lay). Each is tracked on its home (DeskLayout.Track).
    /// Returns the laid roots.
    /// </summary>
    public List<Transform> Lay(DeskFrame frame)
    {
        var laid = new List<Transform>();
        if (config == null)
            return laid;
        DeskLayoutSettings layout = config.layout;
        if (chipReader != null && frame.Place(chipReader.transform, layout.Bay(DeskHomeMap.ChipReader), 0f))
        {
            chipReader.transform.localScale = Vector3.one * layout.chipReaderScale;
            laid.Add(chipReader.transform);
            DeskLayout.Track(DeskHomeMap.ChipReader, chipReader.transform);
        }
        if (uvTorch != null && frame.Place(uvTorch.transform, layout.Bay(DeskHomeMap.UvTorch), 0f))
        {
            uvTorch.transform.localScale = Vector3.one * config.uvTorchScale;
            laid.Add(uvTorch.transform);
            DeskLayout.Track(DeskHomeMap.UvTorch, uvTorch.transform);
        }
        if (upgrades != null)
            upgrades.Lay(frame, laid);
        return laid;
    }

    /// <summary>The UV torch's cradle's footprint on the desk (the corners of everything drawn under it, at rest), so no paper is laid over it; false without the torch on the desk today.</summary>
    public bool TryTorchFootprint(out Vector3[] corners)
    {
        corners = null;
        if (!TorchToday || uvTorch == null || !uvTorch.isActiveAndEnabled || uvTorch.Held)
            return false;
        Bounds b = default;
        bool any = false;
        foreach (Renderer r in uvTorch.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r.GetComponent<TMP_Text>() != null)
                continue;
            if (any)
                b.Encapsulate(r.bounds);
            else
                b = r.bounds;
            any = true;
        }
        if (!any)
            return false;
        corners = new[] { new Vector3(b.min.x, b.min.y, b.min.z), new Vector3(b.max.x, b.min.y, b.min.z), new Vector3(b.min.x, b.min.y, b.max.z), new Vector3(b.max.x, b.min.y, b.max.z) };
        return true;
    }

    /// <summary>The UV torch's first-day note (the binder lays it over the torch).</summary>
    public Transform UvNote => uvHint != null ? uvHint.transform : null;

    /// <summary>The chip reader's first-day note (the binder lays it over the reader).</summary>
    public Transform ChipNote => chipHint != null ? chipHint.transform : null;

    /// <summary>The reveal material for a mark's quad: <paramref name="texture"/>, drawn at <paramref name="queue"/>, only in the cone unless <paramref name="always"/> (a tick); one per kind, shared.</summary>
    public Material MarkMaterial(Texture texture, int queue, bool always)
    {
        var key = (texture, queue, always);
        if (_marks.TryGetValue(key, out Material m) && m != null)
            return m;
        m = markMaterial != null ? new Material(markMaterial) : new Material(Shader.Find("Sprites/Default"));
        m.mainTexture = texture;
        m.renderQueue = queue;
        if (m.HasProperty("_Always"))
            m.SetFloat("_Always", always ? 1f : 0f);
        _marks[key] = m;
        return m;
    }

    /// <summary>The reveal material for a ghost's text in <paramref name="font"/> (its atlas), one per font, shared.</summary>
    public Material TextMaterial(TMP_FontAsset font)
    {
        if (_texts.TryGetValue(font, out Material m) && m != null)
            return m;
        m = textMaterial != null ? new Material(textMaterial) : new Material(font.material);
        m.mainTexture = font.atlasTexture;
        m.renderQueue = 3002;
        _texts[font] = m;
        return m;
    }

    private static Mesh MakeQuad()
    {
        var mesh = new Mesh { name = "UvQuad" };
        mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDestroy()
    {
        foreach (Material m in _marks.Values)
            if (m != null)
                Destroy(m);
        foreach (Material m in _texts.Values)
            if (m != null)
                Destroy(m);
        Shader.SetGlobalFloat(UvTorch.ConeOnId, 0f);
    }
}
