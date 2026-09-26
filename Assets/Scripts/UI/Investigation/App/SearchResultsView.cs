using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Search's results panel (the PC redesign SE1, §2.3): a drop-down over the
/// Investigation app's panes under the toolbar's search field. Its header
/// holds a chip per source with hits ("Reference (3)") after "All", the chosen
/// one pressed (a chip filters to its source); under it the groups in the
/// tab order, each a heading over its hits (the source's glyph, the title and
/// the snippet with the matched text marked; an untranslated line's snippet
/// shows its glyphs in the script's font, unmarked) and "Show all n in
/// Reference" when the group has more; with no hit, one line says nothing
/// matches. A click on a hit chooses it (the app jumps there); ✕ closes the
/// panel. SearchBox fills it; its rows are clones of inactive templates.
/// </summary>
public sealed class SearchResultsView : MonoBehaviour
{
    /// <summary>The header's chip row.</summary>
    [SerializeField] private RectTransform chipRow;

    /// <summary>A source chip (inactive), cloned per source with hits and for "All".</summary>
    [SerializeField] private Button chipTemplate;

    /// <summary>The scrolling list the groups are laid out in.</summary>
    [SerializeField] private ScrollRect scroll;

    /// <summary>A group's heading (inactive), cloned per group.</summary>
    [SerializeField] private TMP_Text headingTemplate;

    /// <summary>A hit (inactive): children Glyph, Title and Snippet texts; cloned per hit.</summary>
    [SerializeField] private Button hitTemplate;

    /// <summary>"Show all n in …" (inactive), cloned per group with more hits.</summary>
    [SerializeField] private Button moreTemplate;

    /// <summary>The line shown when nothing matches.</summary>
    [SerializeField] private TMP_Text emptyText;

    /// <summary>✕: closes the panel.</summary>
    [SerializeField] private Button closeButton;

    /// <summary>The marked text's highlight in a snippet.</summary>
    [SerializeField] private Color markColour = new Color(1f, 0.84f, 0.2f, 0.6f);

    /// <summary>The chosen chip's tint (pressed).</summary>
    [SerializeField] private Color chosenTint = new Color(0.72f, 0.72f, 0.72f, 1f);

    private readonly List<GameObject> _rows = new List<GameObject>();
    private SearchHit _first;
    private bool _hasFirst;
    private bool _wired;

    /// <summary>Raised when a hit is clicked.</summary>
    public event Action<SearchHit> Chosen;

    /// <summary>Raised when a chip or "Show all" filters to a source (null: All).</summary>
    public event Action<AppTab?> Filtered;

    /// <summary>True while the panel shows.</summary>
    public bool IsOpen => gameObject.activeSelf;

    /// <summary>The first hit listed (Enter opens it); false when none is.</summary>
    public bool TryFirst(out SearchHit hit)
    {
        hit = _first;
        return _hasFirst;
    }

    /// <summary>
    /// Shows the results of <paramref name="typed"/>: the chips from
    /// <paramref name="all"/> (every source's group; <paramref name="only"/>
    /// pressed, or All), the groups of <paramref name="shown"/> in their
    /// order, an untranslated snippet in <paramref name="script"/> (null: the
    /// text's own font); with no group, the line saying nothing matches.
    /// </summary>
    public void Show(string typed, bool chip, IReadOnlyList<ResultGroup> all, IReadOnlyList<ResultGroup> shown, AppTab? only, TMP_FontAsset script)
    {
        Wire();
        Clear();
        gameObject.SetActive(true);

        if (all.Count > 0)
        {
            Chip(UiText.Get("search.all"), !only.HasValue, null);
            foreach (ResultGroup group in all)
                Chip(UiText.Format("search.sourceChip", TabName(group.Source), group.Total), only == group.Source, group.Source);
        }

        foreach (ResultGroup group in shown)
        {
            TMP_Text heading = Clone(headingTemplate.gameObject).GetComponent<TMP_Text>();
            heading.text = TabName(group.Source).ToUpperInvariant();
            foreach (SearchHit hit in group.Hits)
                Hit(hit, script);
            if (group.More > 0 && !only.HasValue)
            {
                Button more = Clone(moreTemplate.gameObject).GetComponent<Button>();
                more.GetComponentInChildren<TMP_Text>(true).text = UiText.Format("search.more", group.Total, TabName(group.Source));
                AppTab source = group.Source;
                more.onClick.AddListener(() => Filtered?.Invoke(source));
            }
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(shown.Count == 0);
            emptyText.text = chip && string.IsNullOrWhiteSpace(typed) ? UiText.Get("search.noneChip") : UiText.Format("search.none", (typed ?? string.Empty).Trim());
        }
        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
    }

    /// <summary>Hides the panel (its rows go).</summary>
    public void Hide()
    {
        Clear();
        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    /// <summary>Wires ✕ and hides the templates (once).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        foreach (Component template in new Component[] { chipTemplate, headingTemplate, hitTemplate, moreTemplate })
            if (template != null)
                template.gameObject.SetActive(false);
    }

    /// <summary>The rows and chips go; no first hit.</summary>
    private void Clear()
    {
        foreach (GameObject row in _rows)
            if (row != null)
                Destroy(row);
        _rows.Clear();
        _hasFirst = false;
        _first = default;
    }

    /// <summary>A header chip reading <paramref name="label"/>, pressed when <paramref name="chosen"/>; a click filters to <paramref name="source"/>.</summary>
    private void Chip(string label, bool chosen, AppTab? source)
    {
        if (chipTemplate == null || chipRow == null)
            return;
        Button chip = Instantiate(chipTemplate, chipRow);
        chip.gameObject.SetActive(true);
        _rows.Add(chip.gameObject);
        chip.GetComponentInChildren<TMP_Text>(true).text = label;
        ColorBlock colours = chip.colors;
        colours.normalColor = chosen ? chosenTint : Color.white;
        colours.selectedColor = colours.normalColor;
        chip.colors = colours;
        chip.onClick.AddListener(() => Filtered?.Invoke(source));
    }

    /// <summary>A hit's row: its source's glyph, its title and its snippet (marked; an untranslated one in the script's font).</summary>
    private void Hit(SearchHit hit, TMP_FontAsset script)
    {
        Button row = Clone(hitTemplate.gameObject).GetComponent<Button>();
        row.name = "Hit_" + hit.Entry.Key;
        Transform t = row.transform;
        SetText(t, "Glyph", UiText.Get("search.glyph." + hit.Entry.Source.ToString().ToLowerInvariant()));
        SetText(t, "Title", Escape(hit.Title));
        TMP_Text snippet = t.Find("Snippet") != null ? t.Find("Snippet").GetComponent<TMP_Text>() : null;
        if (snippet != null)
        {
            if (hit.Foreign)
            {
                if (script != null)
                    snippet.font = script;
                snippet.text = Escape(hit.Snippet);
            }
            else
            {
                snippet.text = Marked(hit.Snippet, hit.Marks);
            }
        }
        SearchHit chosen = hit;
        row.onClick.AddListener(() => Chosen?.Invoke(chosen));
        if (!_hasFirst)
        {
            _first = hit;
            _hasFirst = true;
        }
    }

    /// <summary>An active clone of a template in the list.</summary>
    private GameObject Clone(GameObject template)
    {
        GameObject clone = Instantiate(template, template.transform.parent);
        clone.SetActive(true);
        _rows.Add(clone);
        return clone;
    }

    /// <summary>Sets the text of <paramref name="row"/>'s child <paramref name="child"/>.</summary>
    private static void SetText(Transform row, string child, string text)
    {
        Transform t = row.Find(child);
        TMP_Text label = t != null ? t.GetComponent<TMP_Text>() : null;
        if (label != null)
            label.text = text;
    }

    /// <summary>The snippet with its marks highlighted (TextMeshPro's mark tag); the rest shown as written.</summary>
    private string Marked(string text, IReadOnlyList<Mark> marks)
    {
        text ??= string.Empty;
        var sb = new StringBuilder(text.Length + 48 * marks.Count);
        string open = "<mark=#" + ColorUtility.ToHtmlStringRGBA(markColour) + ">";
        int at = 0;
        foreach (Mark mark in marks)
        {
            if (mark.Start < at || mark.Start + mark.Length > text.Length)
                continue;
            sb.Append(Escape(text.Substring(at, mark.Start - at)));
            sb.Append(open).Append(Escape(text.Substring(mark.Start, mark.Length))).Append("</mark>");
            at = mark.Start + mark.Length;
        }
        sb.Append(Escape(text.Substring(at)));
        return sb.ToString();
    }

    /// <summary>A text shown as written: its rich-text tags are not read.</summary>
    private static string Escape(string text) => string.IsNullOrEmpty(text) ? string.Empty : "<noparse>" + text + "</noparse>";

    /// <summary>A source's tab name ("Reference").</summary>
    private static string TabName(AppTab source) => UiText.Get("app.tab." + source.ToString().ToLowerInvariant());
}
