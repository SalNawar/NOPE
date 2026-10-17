using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A scan's layer switch (the scanner app spec §2.4): tabs PRINT, UV and
/// CHIP over the scanned copy, with a wipe between them. PRINT is the copy
/// itself; UV lays the paper's hidden layer over it (the watermark, the
/// ghost of an erased value, the microprint; a mark that shows a fault
/// glows); CHIP lays the chip's stored record over it, a line per printed
/// field in the print's order, so differences line up (a field whose chip
/// value differs glows: ScanLayers.ChipDifferences). The data are the
/// case's hidden layers as the desk machines reveal them (MachineLayers; the
/// desk machines spec §1), set by CaseDocumentsPresenter: the UV tab shows
/// only from the UV torch's day and lists only the marks its cone has shown,
/// the CHIP tab only from the chip reader's day and a chip once read; a
/// glowing line is clicked to log it (Picked: the machine's reading held
/// against its box). Each Documents view has one; a paper shown anew opens
/// on PRINT.
/// </summary>
public sealed class ScanLayerSwitch : MonoBehaviour
{
    [Header("Tabs (each with its child \"Selected\")")]
    /// <summary>PRINT: the copy as printed.</summary>
    [SerializeField] private Button printTab;

    /// <summary>UV: the hidden layer.</summary>
    [SerializeField] private Button uvTab;

    /// <summary>CHIP: the chip's stored record.</summary>
    [SerializeField] private Button chipTab;

    [Header("The layer over the copy")]
    /// <summary>The panel over the copy (hidden on PRINT).</summary>
    [SerializeField] private GameObject overlay;

    /// <summary>The layer's heading ("UV LIGHT", "CHIP RECORD").</summary>
    [SerializeField] private TMP_Text title;

    /// <summary>Where the lines go (a vertical layout).</summary>
    [SerializeField] private RectTransform rows;

    /// <summary>A line (inactive; cloned).</summary>
    [SerializeField] private TMP_Text rowTemplate;

    /// <summary>A line that glows: a fault, a difference (inactive; its child "Text").</summary>
    [SerializeField] private GameObject rowGlowTemplate;

    /// <summary>The bar that wipes across as the layer changes.</summary>
    [SerializeField] private RectTransform wipe;

    /// <summary>How long the wipe takes (seconds).</summary>
    [SerializeField, Min(0.01f)] private float wipeSeconds = 0.25f;

    private readonly List<GameObject> _lines = new List<GameObject>();
    private MachineLayers _provider = new MachineLayers(CaseHidden.None, null, false, false);
    private IReadOnlyList<DocumentField> _fields;
    private int _document = -1;
    private bool _wired;

    /// <summary>The layer shown.</summary>
    public ScanLayer Layer { get; private set; }

    /// <summary>A glowing line clicked (the desk machines spec §1): the paper, the box's index in its fields, what the machine reads there and on which layer; CaseDocumentsPresenter logs it against the box.</summary>
    public event Action<int, int, string, ScanLayer> Picked;

    private void Awake() => Wire();

    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (printTab != null)
            printTab.onClick.AddListener(() => Select(ScanLayer.Print));
        if (uvTab != null)
            uvTab.onClick.AddListener(() => Select(ScanLayer.Uv));
        if (chipTab != null)
            chipTab.onClick.AddListener(() => Select(ScanLayer.Chip));
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
        if (rowGlowTemplate != null)
            rowGlowTemplate.SetActive(false);
        if (wipe != null)
            wipe.gameObject.SetActive(false);
    }

    /// <summary>Where the layers' data come from (the case's hidden layers through the desk machines' readings); the tabs show by the machines on the desk today.</summary>
    public void SetProvider(MachineLayers provider)
    {
        _provider = provider ?? new MachineLayers(CaseHidden.None, null, false, false);
        Refresh();
    }

    /// <summary>The shown layer drawn again (a machine read something: the presenter's call), with no wipe.</summary>
    public void Refresh()
    {
        Wire();
        if (_document >= 0)
            Apply(Available(Layer) ? Layer : ScanLayer.Print, false);
    }

    /// <summary>True when <paramref name="layer"/>'s machine is on the desk today (PRINT always is).</summary>
    private bool Available(ScanLayer layer) => layer == ScanLayer.Print || (layer == ScanLayer.Uv ? _provider.Torch : _provider.Reader);

    /// <summary>The copy of paper <paramref name="document"/> (its printed <paramref name="fields"/>) shows, on PRINT; -1: no copy shows, the tabs hide.</summary>
    public void Show(int document, IReadOnlyList<DocumentField> fields)
    {
        Wire();
        bool changed = document != _document;
        _document = document;
        _fields = fields;
        gameObject.SetActive(document >= 0);
        if (document >= 0)
            transform.SetAsLastSibling(); // over the copies, which are cloned in after it
        if (changed || document < 0)
            Apply(ScanLayer.Print, false);
    }

    /// <summary>A tab clicked: its layer over the copy, with the wipe.</summary>
    public void Select(ScanLayer layer)
    {
        Wire();
        if (_document < 0 || !Available(layer))
            return;
        Apply(layer, layer != Layer && isActiveAndEnabled);
        Sounds.Play(SoundCues.UiTab);
    }

    private void Apply(ScanLayer layer, bool animate)
    {
        Layer = layer;
        if (uvTab != null && uvTab.gameObject.activeSelf != _provider.Torch)
            uvTab.gameObject.SetActive(_provider.Torch);
        if (chipTab != null && chipTab.gameObject.activeSelf != _provider.Reader)
            chipTab.gameObject.SetActive(_provider.Reader);
        Mark(printTab, layer == ScanLayer.Print);
        Mark(uvTab, layer == ScanLayer.Uv);
        Mark(chipTab, layer == ScanLayer.Chip);
        foreach (GameObject line in _lines)
            if (line != null)
                Destroy(line);
        _lines.Clear();
        if (overlay != null)
            overlay.SetActive(layer != ScanLayer.Print);
        if (layer == ScanLayer.Uv)
            DrawUv();
        else if (layer == ScanLayer.Chip)
            DrawChip();
        if (animate && wipe != null)
            StartCoroutine(Wipe());
    }

    /// <summary>The marks the UV torch's cone has shown on the paper: what each is, where and what it reads; a fault glows and is clicked to log it. None seen yet: how to see them.</summary>
    private void DrawUv()
    {
        if (title != null)
            title.text = UiText.Get("layers.uv.title");
        if (_provider.UvState(_document) != LayerState.Shown)
        {
            Line(UiText.Get("layers.uv.unseen"), false, null);
            return;
        }
        int document = _document;
        foreach (UvMark mark in _provider.Uv(document))
        {
            string where = mark.Field >= 0 && _fields != null && mark.Field < _fields.Count && _fields[mark.Field] != null ? UiText.DocumentWord(_fields[mark.Field].label) : UiText.Get("layers.uv.wholePaper");
            bool missing = mark.Kind == UvMarkKind.Microprint && mark.Fault && string.IsNullOrEmpty(mark.Text);
            string text = missing ? UiText.Format("layers.uv.MicroprintMissing", where) : UiText.Format("layers.uv." + mark.Kind, where, mark.Text);
            UvMark picked = mark;
            Line(text, mark.Fault, mark.Fault && mark.Field >= 0 ? () => Picked?.Invoke(document, picked.Field, picked.Text, ScanLayer.Uv) : (Action)null);
        }
    }

    /// <summary>The chip's record, a line per printed field in the print's order; a value that differs from the print glows.</summary>
    private void DrawChip()
    {
        if (title != null)
            title.text = UiText.Get("layers.chip.title");
        LayerState state = _provider.ChipState(_document);
        IReadOnlyList<DocumentField> chip = _provider.Chip(_document);
        if (state != LayerState.Shown || chip == null)
        {
            Line(UiText.Get(state == LayerState.NotYet ? "layers.chip.unread" : "layers.chip.none"), false, null);
            return;
        }
        int document = _document;
        List<int> differ = ScanLayers.ChipDifferences(_fields, chip);
        for (int i = 0; i < chip.Count; i++)
            if (chip[i] != null && chip[i].category != ClueCategory.Photo && chip[i].category != ClueCategory.Seal)
            {
                int field = i;
                string value = chip[i].value;
                bool glows = differ.Contains(i);
                Line(UiText.Format("layers.chip.line", UiText.DocumentWord(chip[i].label), value), glows, glows ? () => Picked?.Invoke(document, field, value, ScanLayer.Chip) : (Action)null);
            }
    }

    /// <summary>A line of the layer; a glowing one, clicked, runs <paramref name="clicked"/> (logs the reading).</summary>
    private void Line(string text, bool glows, Action clicked)
    {
        GameObject template = glows ? rowGlowTemplate : rowTemplate != null ? rowTemplate.gameObject : null;
        if (template == null || rows == null)
            return;
        GameObject line = Instantiate(template, rows, false);
        line.SetActive(true);
        Transform child = line.transform.Find("Text");
        TMP_Text label = child != null ? child.GetComponent<TMP_Text>() : line.GetComponent<TMP_Text>();
        if (label != null)
            label.text = text;
        if (clicked != null && line.GetComponent<Graphic>() != null)
        {
            Button button = line.GetComponent<Button>() ?? line.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                Sounds.Play(SoundCues.UiPress);
                clicked();
            });
        }
        _lines.Add(line);
    }

    /// <summary>A tab's selected mark.</summary>
    private static void Mark(Button tab, bool on)
    {
        if (tab == null)
            return;
        Transform selected = tab.transform.Find("Selected");
        if (selected != null && selected.gameObject.activeSelf != on)
            selected.gameObject.SetActive(on);
    }

    /// <summary>The wipe: a bar crosses the copy left to right.</summary>
    private IEnumerator Wipe()
    {
        wipe.gameObject.SetActive(true);
        for (float t = 0f; t < wipeSeconds; t += Time.unscaledDeltaTime)
        {
            float x = Mathf.Lerp(0f, 1f, t / wipeSeconds);
            wipe.anchorMin = new Vector2(x, 0f);
            wipe.anchorMax = new Vector2(Mathf.Min(1f, x + 0.04f), 1f);
            yield return null;
        }
        wipe.gameObject.SetActive(false);
    }
}
