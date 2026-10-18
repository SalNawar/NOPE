using TMPro;
using UnityEngine;

/// <summary>
/// One desk paper's microprint line (the desk machines spec §10; Saleh
/// 2026-10-18, demo 1018a note 14: the lens stand's magnifiers "magnify
/// microprint that has a meaning, such as microtext that must match"): along
/// the paper's foot, the Bureau's mark, the form and the Citizen ID its
/// office issued (CaseHidden.MicroOf), over and over, printed too fine to
/// read by eye (a hairline of grey). A magnifier held or laid over it shows
/// it large; once one has shown enough of it for a beat (MicroLens) it is
/// read (DeskReadings.ReadMicro): a tick pops at its end, and in inspect
/// mode a click on the line picks its reading against the Citizen ID box
/// (DeskHardware.Pick, ScanLayer.Micro), which a reprinted paper's
/// mismatch proves. DeskHardware adds one to each paper the desk hands over
/// whose form prints one.
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
    private float _held, _pop;
    private int _readableFrame = -2;

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
    }

    /// <summary>
    /// A magnifier this frame: <paramref name="toLens"/> takes a desk point
    /// into its frame (metres, x along its width), its <paramref name="shape"/>
    /// of <paramref name="halfWidth"/> by <paramref name="halfHeight"/>
    /// (<paramref name="on"/> while it magnifies). The line held under it long
    /// enough is read once: its tick pops with the inspect tick's sound.
    /// </summary>
    public void Track(System.Func<Vector3, Vector2> toLens, LensShape shape, float halfWidth, float halfHeight, bool on, float dt)
    {
        if (_paper == null || _paper.Sheet == null || _tick == null)
            return;
        Transform sheet = _paper.Sheet;
        Vector2 a = toLens(sheet.TransformPoint(new Vector3(_band.xMin, _band.center.y, 0f)));
        Vector2 b = toLens(sheet.TransformPoint(new Vector3(_band.xMax, _band.center.y, 0f)));
        float length = _band.width * sheet.lossyScale.x;
        float shown = on ? MicroLens.Shown(a.x, a.y, b.x, b.y, shape, halfWidth, halfHeight) : 0f;
        // Two magnifiers may look at once (the loupe held, the Fresnel laid): the line counts while any of them shows it, once a frame.
        int frame = Time.frameCount;
        bool readable = MicroLens.Readable(shown, length);
        if (readable && _readableFrame != frame)
        {
            _held = MicroLens.Held(_readableFrame < frame - 1 ? 0f : _held, true, dt);
            _readableFrame = frame;
        }
        else if (!readable && _readableFrame < frame - 1)
            _held = 0f;
        if (!UvCone.IsSeen(_held) || !_hardware.Readings.ReadMicro(Document))
            return;
        Shown(true);
        _pop = 1f;
        Sounds.Play(SoundCues.InspectLink);
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
