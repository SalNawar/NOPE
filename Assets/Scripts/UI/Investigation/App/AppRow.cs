using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// One row of an Investigation app view that has an entry key (the PC
/// redesign CP1, PR1, KB4): a document's field, a transcript line, a book's
/// row, a record's evidence row. The views' page components mark each such
/// row as they fill it (Mark), with the row's key and title (its pick's
/// label: "Visa · Visa Class"), its label and value (the texts it shows, or
/// a form box's field) and its pick button; an untranslated transcript line
/// also carries its tongue and its
/// canonical text. The app's focus ring walks these rows in reading order
/// (the page's child order); Space picks the row (its button, the same as a
/// click), Ctrl+C copies its value as shown and Ctrl+Shift+C "Label: value",
/// Ctrl+P pins it; a right-click opens the row's context menu (Copy value,
/// Copy row, Pin or Unpin, Pick for compare).
/// </summary>
public sealed class AppRow : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text _labelText;
    private TMP_Text _valueText;
    private string _label;
    private string _value;
    private Button _pick;

    /// <summary>The row's entry key (PickKeys').</summary>
    public string Key { get; private set; }

    /// <summary>What the row is, for a pin, a recent item or a clip's source ("Visa · Visa Class").</summary>
    public string Title { get; private set; }

    /// <summary>The tab the row lives in.</summary>
    public AppTab Source { get; private set; }

    /// <summary>True for an untranslated transcript line (its copy is a foreign clip).</summary>
    public bool Foreign { get; private set; }

    private string _tongueId;
    private string _tongueName;
    private string _canonical;

    /// <summary>The row's label as shown.</summary>
    public string Label => _labelText != null ? _labelText.text : _label ?? string.Empty;

    /// <summary>The row's value as shown.</summary>
    public string Value => _valueText != null ? _valueText.text : _value ?? string.Empty;

    /// <summary>True when Space (or the menu's Pick) can pick the row: its button is live.</summary>
    public bool Pickable => _pick != null && _pick.isActiveAndEnabled && _pick.interactable;

    /// <summary>Marks a filled row of <paramref name="source"/> with its key, title, the texts showing its label and value, and its pick button (null: not pickable); a plain row until MarkUntranslated.</summary>
    public static AppRow Mark(GameObject row, AppTab source, string key, string title, TMP_Text label, TMP_Text value, Button pick)
    {
        AppRow marked = Mark(row, source, key, title, pick);
        marked._labelText = label;
        marked._valueText = value;
        return marked;
    }

    /// <summary>Marks a form's box of <paramref name="source"/> with its key, title, its field's label and value as printed, and its pick button.</summary>
    public static AppRow Mark(GameObject row, AppTab source, string key, string title, string label, string value, Button pick)
    {
        AppRow marked = Mark(row, source, key, title, pick);
        marked._label = label;
        marked._value = value;
        return marked;
    }

    private static AppRow Mark(GameObject row, AppTab source, string key, string title, Button pick)
    {
        if (!row.TryGetComponent(out AppRow marked))
            marked = row.AddComponent<AppRow>();
        marked.Key = key;
        marked.Title = title ?? string.Empty;
        marked.Source = source;
        marked._labelText = marked._valueText = null;
        marked._label = marked._value = null;
        marked._pick = pick;
        marked.Foreign = false;
        marked._tongueId = marked._tongueName = marked._canonical = null;
        return marked;
    }

    /// <summary>The row shows an untranslated line of <paramref name="tongueName"/>: its copy keeps the tongue and the canonical text (never shown or pasted).</summary>
    public void MarkUntranslated(string tongueId, string tongueName, string canonical)
    {
        Foreign = true;
        _tongueId = tongueId;
        _tongueName = tongueName;
        _canonical = canonical;
    }

    /// <summary>Picks the row for compare, as a click on it does (nothing when it is not pickable).</summary>
    public void Pick()
    {
        if (Pickable)
            _pick.onClick.Invoke();
    }

    /// <summary>The row's clip: its value as shown (a foreign clip for an untranslated line), or "Label: value" (<paramref name="wholeRow"/>; always plain).</summary>
    public Clip ToClip(bool wholeRow, string traveller)
    {
        if (wholeRow)
            return Clip.Plain(UiText.Format("app.copyRow", Label, Value), Key, Title, traveller);
        return Foreign
            ? Clip.Untranslated(Value, Key, Title, traveller, _tongueId, _tongueName, _canonical)
            : Clip.Plain(Value, Key, Title, traveller);
    }

    /// <summary>A right-click opens the row's context menu.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;
        InvestigationApp app = GetComponentInParent<InvestigationApp>();
        if (app != null)
            app.ShowRowMenu(this, eventData);
    }
}
