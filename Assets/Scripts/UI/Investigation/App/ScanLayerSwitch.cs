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
/// value differs glows: ScanLayers.ChipDifferences). The data come from an
/// IScanLayers (the document track's hidden and chip data; StubScanLayers
/// until they land), set by CaseDocumentsPresenter. Each Documents view has
/// one; a paper shown anew opens on PRINT.
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
    private IScanLayers _provider = new StubScanLayers();
    private IReadOnlyList<DocumentField> _fields;
    private int _document = -1;
    private bool _wired;

    /// <summary>The layer shown.</summary>
    public ScanLayer Layer { get; private set; }

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

    /// <summary>Where the layers' data come from (the document track's, or the stub).</summary>
    public void SetProvider(IScanLayers provider) => _provider = provider ?? new StubScanLayers();

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
        if (_document < 0)
            return;
        Apply(layer, layer != Layer && isActiveAndEnabled);
        Sounds.Play(SoundCues.UiTab);
    }

    private void Apply(ScanLayer layer, bool animate)
    {
        Layer = layer;
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

    /// <summary>The hidden layer's marks: what each is, where and what it reads; a fault glows.</summary>
    private void DrawUv()
    {
        if (title != null)
            title.text = UiText.Get("layers.uv.title");
        IReadOnlyList<UvMark> marks = _provider.Uv(_document, _fields);
        if (marks == null || marks.Count == 0)
        {
            Line(UiText.Get("layers.uv.none"), false);
            return;
        }
        foreach (UvMark mark in marks)
        {
            string where = mark.Field >= 0 && _fields != null && mark.Field < _fields.Count && _fields[mark.Field] != null ? UiText.DocumentWord(_fields[mark.Field].label) : UiText.Get("layers.uv.wholePaper");
            Line(UiText.Format("layers.uv." + mark.Kind, where, mark.Text), mark.Fault);
        }
    }

    /// <summary>The chip's record, a line per printed field in the print's order; a value that differs from the print glows.</summary>
    private void DrawChip()
    {
        if (title != null)
            title.text = UiText.Get("layers.chip.title");
        IReadOnlyList<DocumentField> chip = _provider.Chip(_document, _fields);
        if (chip == null)
        {
            Line(UiText.Get("layers.chip.none"), false);
            return;
        }
        List<int> differ = ScanLayers.ChipDifferences(_fields, chip);
        for (int i = 0; i < chip.Count; i++)
            if (chip[i] != null && chip[i].category != ClueCategory.Photo && chip[i].category != ClueCategory.Seal)
                Line(UiText.Format("layers.chip.line", UiText.DocumentWord(chip[i].label), chip[i].value), differ.Contains(i));
    }

    private void Line(string text, bool glows)
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
