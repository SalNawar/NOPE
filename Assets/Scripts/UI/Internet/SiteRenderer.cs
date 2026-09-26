using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a site page (SitePage, P spec IN1/IN6) into the browser's scrolling
/// page with uGUI: each block from three builder-made templates (a text, a
/// link, a filled panel), laid out by layout groups, in the site's fixed
/// style (SiteStyles; the theme never touches page content, its templates
/// carry diegetic roles). A link raises <see cref="LinkClicked"/> with its
/// address. The form engine's renderer (FormView, phase 4/5) replaces this
/// drawing when it lands; the page model stays.
/// </summary>
public sealed class SiteRenderer : MonoBehaviour
{
    /// <summary>The masthead's size (canvas units; 1 u is 0.52 px at 720p, so the smallest text below stays about 8 px there).</summary>
    private const float MastheadSize = 40f;

    /// <summary>A headline's size.</summary>
    private const float HeadlineSize = 31f;

    /// <summary>A section heading's size.</summary>
    private const float HeadingSize = 25f;

    /// <summary>Body text and links.</summary>
    private const float BodySize = 21f;

    /// <summary>Table cells and box lines.</summary>
    private const float CellSize = 19f;

    /// <summary>Notes and revised notes.</summary>
    private const float NoteSize = 17f;

    /// <summary>Box titles, field labels and column heads.</summary>
    private const float LabelSize = 16f;

    /// <summary>Infobox cells and start-page tiles per row.</summary>
    private const int CellsPerRow = 3;

    [SerializeField] private ScrollRect scroll;
    [SerializeField] private RectTransform content;

    /// <summary>The page behind the content (takes the site's paper colour).</summary>
    [SerializeField] private Image paper;

    /// <summary>Inactive templates, cloned per block.</summary>
    [SerializeField] private TMP_Text textTemplate;
    [SerializeField] private Button linkTemplate;
    [SerializeField] private Image panelTemplate;

    /// <summary>A link was clicked: its address.</summary>
    public event Action<string> LinkClicked;

    /// <summary>The blocks drawn now (top level under the content).</summary>
    private readonly List<GameObject> _built = new List<GameObject>();

    /// <summary>The style being drawn with.</summary>
    private SiteStyle _style;

    /// <summary>Replaces the page's blocks with <paramref name="page"/>'s, in <paramref name="style"/>, scrolled to the top.</summary>
    public void Render(SitePage page, SiteStyle style)
    {
        _style = style;
        foreach (GameObject go in _built)
        {
            go.SetActive(false);
            Destroy(go);
        }
        _built.Clear();

        if (paper != null)
            paper.color = ToColor(style.Paper);
        foreach (PageBlock block in page.Blocks)
            Add(block);

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>One block under the content.</summary>
    private void Add(PageBlock b)
    {
        switch (b.Kind)
        {
            case PageBlockKind.Masthead:
                Text(content, b.Text, MastheadSize, FontStyles.Bold | FontStyles.SmallCaps, TextAlignmentOptions.Center, _style.Accent);
                Panel(content, _style.Rule, false, 0, 0).gameObject.AddComponent<LayoutElement>().preferredHeight = 3f;
                break;
            case PageBlockKind.Headline:
                Text(content, b.Text, HeadlineSize, FontStyles.Bold, TextAlignmentOptions.Left, _style.Ink);
                break;
            case PageBlockKind.Heading:
                Text(content, b.Text, HeadingSize, FontStyles.Bold, TextAlignmentOptions.Left, _style.Accent);
                break;
            case PageBlockKind.Paragraph:
                Text(content, b.Text, BodySize, FontStyles.Normal, TextAlignmentOptions.Left, _style.Ink);
                break;
            case PageBlockKind.Note:
                Text(content, b.Text, NoteSize, FontStyles.Italic, TextAlignmentOptions.Left, _style.Muted);
                break;
            case PageBlockKind.Link:
                Link(content, b.Text, b.Address, BodySize);
                break;
            case PageBlockKind.Box:
                RectTransform box = Panel(content, _style.Box, false, 12, 6);
                if (!string.IsNullOrEmpty(b.Text))
                    Text(box, b.Text, LabelSize, FontStyles.Bold, TextAlignmentOptions.Left, _style.Accent);
                foreach (string line in b.Lines)
                    Text(box, line, CellSize, FontStyles.Normal, TextAlignmentOptions.Left, _style.Ink);
                foreach (PageLink l in b.Links)
                    Link(box, l.Text, l.Address, CellSize);
                break;
            case PageBlockKind.Fields:
                AddFields(b.Fields);
                break;
            case PageBlockKind.Table:
                AddTable(b);
                break;
            case PageBlockKind.Tiles:
                AddTiles(b.Links);
                break;
            case PageBlockKind.Chips:
                AddChips(b);
                break;
        }
    }

    /// <summary>Boxed label-over-value cells, three to a row; a value may link; a note sits under it.</summary>
    private void AddFields(List<PageField> fields)
    {
        RectTransform row = null;
        for (int i = 0; i < fields.Count; i++)
        {
            if (i % CellsPerRow == 0)
                row = Row(content, 8);
            PageField f = fields[i];
            RectTransform cell = Panel(row, _style.Box, false, 8, 2);
            Flexible(cell);
            Text(cell, f.Label, LabelSize, FontStyles.Bold, TextAlignmentOptions.Left, _style.Muted);
            if (string.IsNullOrEmpty(f.Address))
                Text(cell, f.Value, BodySize, FontStyles.Normal, TextAlignmentOptions.Left, _style.Ink);
            else
                Link(cell, f.Value, f.Address, BodySize);
            if (!string.IsNullOrEmpty(f.Note))
                Text(cell, f.Note, NoteSize, FontStyles.Italic, TextAlignmentOptions.Left, _style.Accent);
        }
    }

    /// <summary>The column heads on a box row, then a row per table row; a cell may link.</summary>
    private void AddTable(PageBlock b)
    {
        if (b.Columns.Count > 0)
        {
            RectTransform head = Panel(content, _style.Box, true, 6, 8);
            foreach (string column in b.Columns)
                Flexible(Text(head, column, LabelSize, FontStyles.Bold, TextAlignmentOptions.Left, _style.Muted));
        }

        foreach (List<PageCell> cells in b.Rows)
        {
            RectTransform row = Row(content, 8);
            ((HorizontalLayoutGroup)row.GetComponent<LayoutGroup>()).padding = new RectOffset(6, 6, 0, 0);
            foreach (PageCell c in cells)
            {
                if (string.IsNullOrEmpty(c.Address))
                    Flexible(Text(row, c.Text, CellSize, FontStyles.Normal, TextAlignmentOptions.Left, _style.Ink));
                else
                    Flexible(Link(row, c.Text, c.Address, CellSize));
            }
        }
    }

    /// <summary>The start page's tiles, three to a row: the glyph's placeholder (the site's initial), the name, the blurb and the address.</summary>
    private void AddTiles(List<PageLink> tiles)
    {
        RectTransform row = null;
        for (int i = 0; i < tiles.Count; i++)
        {
            if (i % CellsPerRow == 0)
                row = Row(content, 14);
            PageLink t = tiles[i];
            Button tile = Link(row, t.Text, t.Address, HeadingSize);
            Flexible(tile);
            tile.GetComponent<Image>().color = ToColor(_style.Box);
            var layout = tile.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 4;
            TMP_Text name = Label(tile);
            name.fontStyle = FontStyles.Bold;
            name.color = ToColor(_style.Link);
            string initial = string.IsNullOrEmpty(t.Text) ? "?" : t.Text.Substring(0, 1);
            Text(tile.transform, initial, MastheadSize, FontStyles.Bold, TextAlignmentOptions.Left, _style.Accent).transform.SetAsFirstSibling();
            if (!string.IsNullOrEmpty(t.Detail))
                Text(tile.transform, t.Detail, CellSize, FontStyles.Normal, TextAlignmentOptions.Left, _style.Ink);
            Text(tile.transform, t.Address, NoteSize, FontStyles.Normal, TextAlignmentOptions.Left, _style.Muted);
        }
    }

    /// <summary>A caption and a row of filter chips; the chosen one filled with the accent.</summary>
    private void AddChips(PageBlock b)
    {
        RectTransform row = Row(content, 6);
        var layout = (HorizontalLayoutGroup)row.GetComponent<LayoutGroup>();
        layout.childForceExpandWidth = false;
        layout.childControlWidth = true;
        TMP_Text caption = Text(row, b.Text, LabelSize, FontStyles.Bold, TextAlignmentOptions.Left, _style.Muted);
        caption.textWrappingMode = TextWrappingModes.NoWrap;
        foreach (PageLink chip in b.Links)
        {
            Button button = Link(row, chip.Text, chip.Address, NoteSize);
            button.GetComponent<Image>().color = ToColor(chip.Active ? _style.Accent : _style.Box);
            button.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(10, 10, 4, 4);
            TMP_Text label = Label(button);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.fontStyle = chip.Active ? FontStyles.Bold : FontStyles.Normal;
            label.color = ToColor(chip.Active ? _style.Paper : _style.Link);
        }
    }

    /// <summary>A text from the template.</summary>
    private TMP_Text Text(Transform parent, string text, float size, FontStyles style, TextAlignmentOptions align, Rgba ink)
    {
        TMP_Text t = Instantiate(textTemplate, parent);
        t.gameObject.SetActive(true);
        t.text = text ?? string.Empty;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = ToColor(ink);
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        Track(t.gameObject, parent);
        return t;
    }

    /// <summary>A link from the template (underlined in the link colour); a blank address draws plain text.</summary>
    private Button Link(Transform parent, string text, string address, float size)
    {
        Button b = Instantiate(linkTemplate, parent);
        b.gameObject.SetActive(true);
        TMP_Text label = Label(b);
        label.text = text ?? string.Empty;
        label.fontSize = size;
        label.textWrappingMode = TextWrappingModes.Normal;
        bool live = !string.IsNullOrWhiteSpace(address);
        label.fontStyle = live ? FontStyles.Underline : FontStyles.Normal;
        label.color = ToColor(live ? _style.Link : _style.Ink);
        b.interactable = live;
        if (live)
            b.onClick.AddListener(() => LinkClicked?.Invoke(address));
        Track(b.gameObject, parent);
        return b;
    }

    /// <summary>A filled panel from the template, laying its children out vertically or in a row.</summary>
    private RectTransform Panel(Transform parent, Rgba fill, bool horizontal, int padding, float spacing)
    {
        Image img = Instantiate(panelTemplate, parent);
        img.gameObject.SetActive(true);
        img.color = ToColor(fill);
        img.raycastTarget = false;
        HorizontalOrVerticalLayoutGroup layout = horizontal
            ? img.gameObject.AddComponent<HorizontalLayoutGroup>()
            : img.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        Track(img.gameObject, parent);
        return (RectTransform)img.transform;
    }

    /// <summary>An invisible row laying its children out side by side, top-aligned.</summary>
    private RectTransform Row(Transform parent, float spacing)
    {
        var go = new GameObject("Row", typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        Track(go, parent);
        return (RectTransform)go.transform;
    }

    /// <summary>Gives an element an equal share of its row's width.</summary>
    private static void Flexible(Component c)
    {
        LayoutElement le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 1f;
        le.flexibleWidth = 1f;
    }

    /// <summary>A link's label.</summary>
    private static TMP_Text Label(Button b) => b.transform.Find("Label").GetComponent<TMP_Text>();

    /// <summary>Remembers a top-level block (its children go with it).</summary>
    private void Track(GameObject go, Transform parent)
    {
        if (parent == content)
            _built.Add(go);
    }

    /// <summary>A page colour as a Unity colour.</summary>
    private static Color ToColor(Rgba c) => new Color(c.R, c.G, c.B, c.A);
}
