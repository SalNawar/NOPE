using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One desk paper's hidden layer under the UV torch (the desk machines spec
/// §1; Saleh 2026-10-17: "an actual uv flashlight you can use to see the
/// hidden watermarks and symbols"): each of the paper's UV marks (CaseHidden)
/// drawn on its sheet in the TimeDesk/UvReveal shaders, which show it only
/// inside the torch's cone: the Bureau's watermark across the paper, a
/// genuine seal's microprint ring, the ghost of a value washed off a box (in
/// the paper's own font). A forged seal shows no ring: its absence is the
/// fault. Each frame the torch is lit, Track counts how long each mark has
/// lain in the cone (UvCone); a mark lit long enough is seen
/// (DeskReadings.See): a fault mark then leaves a violet tick on its box
/// for the case, which in inspect mode is clicked to log the reading
/// against the box (DeskHardware.Pick). DeskHardware adds one to each
/// paper the desk hands over.
/// </summary>
public sealed class UvPaperLayer : MonoBehaviour
{
    /// <summary>A mark: its index in the paper's marks, its data, its rectangle in the sheet's plane, its drawing, its tick and how long it has been lit.</summary>
    private sealed class Mark
    {
        public int Index;
        public UvMark Data;
        public Rect Local;
        public GameObject Tick;
        public float Lit;
        public float Pop;
    }

    private readonly List<Mark> _marks = new List<Mark>();
    private DeskDocument _paper;
    private DeskHardware _hardware;

    /// <summary>The paper's index in the case.</summary>
    public int Document => _paper != null ? _paper.Index : -1;

    /// <summary>Draws <paramref name="marks"/> on <paramref name="paper"/>'s sheet for <paramref name="hardware"/> (its materials and textures; it hears a tick's click).</summary>
    public void Build(DeskDocument paper, DeskHardware hardware, IReadOnlyList<UvMark> marks)
    {
        _paper = paper;
        _hardware = hardware;
        if (paper == null || paper.Sheet == null || hardware == null)
            return;
        var root = new GameObject("UvLayer").transform;
        root.SetParent(paper.Sheet, false);
        root.gameObject.layer = paper.Sheet.gameObject.layer;
        Vector2 size = paper.Size;
        for (int i = 0; i < (marks?.Count ?? 0); i++)
        {
            UvMark data = marks[i];
            Rect local;
            if (data.Field < 0)
            {
                float side = Mathf.Min(size.x, size.y) * 0.8f;
                local = new Rect(-side / 2f, -side / 2f, side, side);
            }
            else if (!paper.TryLocalRectOf(paper.SlotOfRow(data.Field), out local))
                continue;
            var mark = new Mark { Index = i, Data = data, Local = local };
            switch (data.Kind)
            {
                case UvMarkKind.Watermark:
                    Quad(root, "Watermark", local, hardware.WatermarkTexture, PaperLayers.Fill + 0.00005f, 2985, false);
                    break;
                case UvMarkKind.Microprint:
                    if (!data.Fault)
                    {
                        float side = Mathf.Max(local.width, local.height) * 1.3f;
                        Quad(root, "Microprint", new Rect(local.center.x - side / 2f, local.center.y - side / 2f, side, side), hardware.MicroprintTexture, PaperLayers.Seal + 0.00005f, 2986, false);
                    }
                    break;
                case UvMarkKind.Ghost:
                    Ghost(root, local, data.Text);
                    break;
            }
            if (data.Fault)
            {
                float side = Mathf.Max(0.004f, Mathf.Min(local.height, local.width) * 0.9f);
                mark.Tick = Quad(root, "Tick", new Rect(local.xMax - side * 0.55f, local.center.y - side / 2f, side, side), hardware.TickTexture, PaperLayers.Ink + 0.0001f, 3006, true);
                var box = mark.Tick.AddComponent<BoxCollider>();
                box.size = new Vector3(1f, 1f, 0.002f);
                mark.Tick.layer = OfficeLayers.InteractableLayer;
                var click = mark.Tick.AddComponent<Clickable>();
                Mark picked = mark;
                click.onClick.AddListener(() => _hardware.TickClicked(Document, picked.Data));
                mark.Tick.SetActive(hardware.Readings.Seen(Document, i));
            }
            _marks.Add(mark);
        }
    }

    /// <summary>
    /// The torch's cone this frame (<paramref name="centre"/> on the desk,
    /// <paramref name="radius"/>; <paramref name="on"/> while lit): each mark's
    /// lit time grows while enough of it lies in the cone (UvCone), and a mark
    /// lit long enough is seen once: a fault's tick pops onto its box with the
    /// inspect tick's sound. Returns how many marks it saw for the first time.
    /// </summary>
    public int Track(Vector3 centre, float radius, bool on, float dt)
    {
        int found = 0;
        if (_paper == null || _paper.Sheet == null)
            return 0;
        Transform sheet = _paper.Sheet;
        float scale = sheet.lossyScale.x;
        foreach (Mark m in _marks)
        {
            Vector3 c = sheet.TransformPoint(m.Local.center);
            float covered = on ? UvCone.Covered(c.x, c.z, m.Local.width * scale, m.Local.height * scale, centre.x, centre.z, radius) : 0f;
            m.Lit = UvCone.Lit(m.Lit, covered, on, dt);
            if (!UvCone.IsSeen(m.Lit) || !_hardware.Readings.See(Document, m.Index))
                continue;
            found++;
            if (m.Tick != null)
            {
                m.Tick.SetActive(true);
                m.Pop = 1f;
                Sounds.Play(SoundCues.InspectLink);
            }
        }
        return found;
    }

    /// <summary>Each popped tick eases from a swell back to its size.</summary>
    private void Update()
    {
        foreach (Mark m in _marks)
        {
            if (m.Tick == null || m.Pop <= 0f)
                continue;
            m.Pop = UiMotion.Amount.Still ? 0f : Mathf.Max(0f, m.Pop - Time.unscaledDeltaTime * 3f);
            float s = 1f + 0.6f * m.Pop;
            m.Tick.transform.localScale = new Vector3(TickSide(m) * s, TickSide(m) * s, 1f);
        }
    }

    private static float TickSide(Mark m) => Mathf.Max(0.004f, Mathf.Min(m.Local.height, m.Local.width) * 0.9f);

    /// <summary>A flat quad over <paramref name="local"/> (the sheet's plane) at <paramref name="lift"/>, drawn in the reveal shader with <paramref name="texture"/> (always, for a tick; only in the cone otherwise).</summary>
    private GameObject Quad(Transform root, string name, Rect local, Texture texture, float lift, int queue, bool always)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.layer = root.gameObject.layer;
        go.transform.localPosition = new Vector3(local.center.x, local.center.y, -lift);
        go.transform.localScale = new Vector3(local.width, local.height, 1f);
        go.AddComponent<MeshFilter>().sharedMesh = DeskHardware.QuadMesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = _hardware.MarkMaterial(texture, queue, always);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    /// <summary>The ghost of a washed-off value: the value written in the paper's font over its box, a little askew, in the reveal text shader.</summary>
    private void Ghost(Transform root, Rect local, string value)
    {
        TMP_FontAsset font = _paper.TextFont;
        if (font == null || string.IsNullOrEmpty(value))
            return;
        var go = new GameObject("Ghost");
        go.transform.SetParent(root, false);
        go.layer = root.gameObject.layer;
        var text = go.AddComponent<TextMeshPro>();
        text.font = font;
        text.fontSharedMaterial = _hardware.TextMaterial(font);
        text.text = value;
        text.enableAutoSizing = true;
        text.fontSizeMin = 0.001f;
        text.fontSizeMax = 10f;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.Midline;
        text.color = Color.white;
        text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(local.width * 0.8f, local.height * 0.9f);
        text.rectTransform.localPosition = new Vector3(local.center.x + local.width * 0.04f, local.center.y, -(PaperLayers.Text + 0.00005f));
        text.margin = Vector4.zero;
        text.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 3.5f);
        var r = go.GetComponent<MeshRenderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
    }
}
