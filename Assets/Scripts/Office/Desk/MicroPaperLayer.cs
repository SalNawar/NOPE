using TMPro;
using UnityEngine;

/// <summary>
/// One desk paper's microprint line (the desk machines spec §10; Saleh
/// 2026-10-18, demo 1018a note 14: "microprint that has a meaning, such as
/// microtext that must match"): along the paper's foot, the Bureau's mark,
/// the form and the Citizen ID its office issued (CaseHidden.MicroOf), over
/// and over, printed too fine to read by eye (a hairline of grey). The
/// magnifiers that once read it were cut (P0-C; Saleh D5: "not sold"): the
/// scanner's hidden-layer view prints it large (ScanMicroprint) and credits
/// the read once it rendered there (MicroLens.Readable, DeskReadings.ReadMicro;
/// FIX_PLAN_v2 BUG-2b). When it is read the desk line answers: a tick pops at
/// its end, and in inspect mode a click on the line picks its reading against
/// the Citizen ID box (DeskHardware.Pick, ScanLayer.Micro), which a reprinted
/// paper's mismatch proves. DeskHardware adds one to each paper the desk
/// hands over whose form prints one.
/// </summary>
public sealed class MicroPaperLayer : MonoBehaviour
{
    /// <summary>The line's place on the sheet (shares of the paper's width and height from its centre): its length, how far up from the foot, and its height.</summary>
    private const float LengthShare = 0.84f, FootShare = 0.035f, HeightShare = 0.008f;

    /// <summary>The ink: a dark grey, a hairline to the eye.</summary>
    private static readonly Color Ink = new Color(0.17f, 0.11f, 0.14f, 0.85f);

    private DeskDocument _paper;
    private DeskHardware _hardware;
    private MicroLine _line;
    private Rect _band;
    private GameObject _tick;
    private BoxCollider _box;
    private float _pop;

    /// <summary>The paper's index in the case.</summary>
    public int Document => _paper != null ? _paper.Index : -1;

    /// <summary>Prints <paramref name="line"/> along <paramref name="paper"/>'s foot (form <paramref name="formNumber"/>) for <paramref name="hardware"/> (its tick texture, its readings, its pick).</summary>
    public void Build(DeskDocument paper, DeskHardware hardware, MicroLine line, string formNumber)
    {
        _paper = paper;
        _hardware = hardware;
        _line = line;
        if (paper == null || paper.Sheet == null || hardware == null || paper.TextFont == null)
            return;
        Vector2 size = paper.Size;
        float height = Mathf.Max(0.0006f, size.y * HeightShare);
        _band = new Rect(-size.x * LengthShare / 2f, -size.y / 2f + size.y * FootShare, size.x * LengthShare, height);

        var root = new GameObject("MicroLayer").transform;
        root.SetParent(paper.Sheet, false);
        root.gameObject.layer = paper.Sheet.gameObject.layer;

        var go = new GameObject("Microprint", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(root, false);
        go.layer = root.gameObject.layer;
        var text = go.GetComponent<TextMeshPro>();
        text.font = paper.TextFont;
        int repeats = Mathf.Max(1, Mathf.RoundToInt(_band.width / (height * 0.55f * (line.Printed(formNumber, 1).Length))));
        text.text = line.Printed(formNumber, repeats);
        text.enableAutoSizing = true;
        text.fontSizeMin = 0.0001f;
        text.fontSizeMax = 10f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.alignment = TextAlignmentOptions.Midline;
        text.characterSpacing = 2f;
        text.color = Ink;
        text.margin = Vector4.zero;
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(_band.width, _band.height);
        text.rectTransform.localPosition = new Vector3(_band.center.x, _band.center.y, -(PaperLayers.Text + 0.00004f));
        var r = go.GetComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        // Read: a tick at its end; in inspect mode a click on the line logs it.
        var hit = new GameObject("MicroClick");
        hit.transform.SetParent(root, false);
        hit.layer = OfficeLayers.InteractableLayer;
        hit.transform.localPosition = new Vector3(_band.center.x, _band.center.y, -(PaperLayers.Ink + 0.0001f));
        _box = hit.AddComponent<BoxCollider>();
        _box.size = new Vector3(_band.width, Mathf.Max(_band.height * 3f, size.y * 0.03f), 0.002f);
        hit.AddComponent<Clickable>().onClick.AddListener(Clicked);

        float side = Mathf.Max(0.004f, size.y * 0.035f);
        _tick = new GameObject("MicroTick");
        _tick.transform.SetParent(root, false);
        _tick.layer = root.gameObject.layer;
        _tick.transform.localPosition = new Vector3(_band.xMax + side * 0.7f, _band.center.y, -(PaperLayers.Ink + 0.0001f));
        _tick.transform.localScale = new Vector3(side, side, 1f);
        _tick.AddComponent<MeshFilter>().sharedMesh = DeskHardware.QuadMesh;
        var tr = _tick.AddComponent<MeshRenderer>();
        tr.sharedMaterial = hardware.MarkMaterial(hardware.TickTexture, 3006, true);
        tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Shown(hardware.Readings.MicroRead(Document));
        hardware.Readings.Changed += OnRead;
    }

    /// <summary>
    /// A desk magnifier this frame: nothing. The magnifiers were cut (P0-C),
    /// so nothing at the desk reads the line; the scan view credits it
    /// (MicroLens.Readable) and the tick answers through DeskReadings.Changed.
    /// Kept only because DeskHardware.TrackLens (DESK-PLAN's file) still calls
    /// it: INTEGRATION's cleanup list deletes both (docs/pending/TOOLS.md).
    /// </summary>
    public void Track(System.Func<Vector3, Vector2> toLens, LensShape shape, float halfWidth, float halfHeight, bool on, float dt)
    {
    }

    /// <summary>A reading of the case changed: when it is this paper's microprint, read in the scan view, its tick pops with the inspect tick's sound and the line becomes clickable.</summary>
    private void OnRead(int document)
    {
        if (document != Document || _hardware == null || _tick == null || _tick.activeSelf || !_hardware.Readings.MicroRead(Document))
            return;
        Shown(true);
        _pop = 1f;
        Sounds.Play(SoundCues.InspectLink);
    }

    /// <summary>The paper goes (its case ended): it stops listening.</summary>
    private void OnDestroy()
    {
        if (_hardware != null)
            _hardware.Readings.Changed -= OnRead;
    }

    private void Shown(bool read)
    {
        if (_tick != null)
            _tick.SetActive(read);
        if (_box != null)
            _box.enabled = read;
    }

    /// <summary>The line clicked: in inspect mode, once read, its reading is picked against the Citizen ID box.</summary>
    private void Clicked()
    {
        if (_hardware == null || !_hardware.Inspecting || !_hardware.Readings.MicroRead(Document))
            return;
        _hardware.Pick(Document, _line.Field, _line.Value, ScanLayer.Micro);
    }

    /// <summary>A popped tick eases from a swell back to its size.</summary>
    private void Update()
    {
        if (_tick == null || _pop <= 0f)
            return;
        _pop = UiMotion.Amount.Still ? 0f : Mathf.Max(0f, _pop - Time.unscaledDeltaTime * 3f);
        float side = Mathf.Max(0.004f, _paper.Size.y * 0.035f) * (1f + 0.6f * _pop);
        _tick.transform.localScale = new Vector3(side, side, 1f);
    }
}
