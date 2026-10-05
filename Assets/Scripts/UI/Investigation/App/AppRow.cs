using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A row of the Investigation app's forms (phase 20, CP1, PR1, KB4; phase 16:
/// every view is a form): a document's field, a transcript line, a book's
/// row or a record's evidence row is marked on its form box's button as the
/// page is drawn (Mark), with the row's key and title (its pick's label:
/// "Visa · Visa Class"), its label and value as printed, and its pick button;
/// an untranslated transcript line also carries its tongue and its canonical
/// text. The app's focus ring walks these rows in reading order (the form's
/// slot order); Space picks the row (its button, the same as a click), Enter
/// follows its smart link (SetLink; the ↗ itself is the form's, FormView),
/// Ctrl+C copies its value as shown and Ctrl+Shift+C "Label: value", Ctrl+P
/// pins it; Shift+F10 or the Menu key opens the row's context menu (Copy
/// value, Copy row, Pin or Unpin, Pick for compare; a right-click backs out
/// instead, ControlRules). The pick tint and the found mark are the
/// form's, by key (CM3): nothing here draws.
/// </summary>
public sealed class AppRow : MonoBehaviour
{
    private LinkTarget _target;
    private string _label;
    private string _value;
    private Button _pick;
    private string _tongueId;
    private string _tongueName;
    private string _canonical;

    /// <summary>The row's entry key (PickKeys').</summary>
    public string Key { get; private set; }

    /// <summary>What the row is, for a pin, a recent item or a clip's source ("Visa · Visa Class").</summary>
    public string Title { get; private set; }

    /// <summary>The tab the row lives in.</summary>
    public AppTab Source { get; private set; }

    /// <summary>True for an untranslated transcript line (its copy is a foreign clip).</summary>
    public bool Foreign { get; private set; }

    /// <summary>The row's label as printed.</summary>
    public string Label => _label ?? string.Empty;

    /// <summary>The row's value as printed.</summary>
    public string Value => _value ?? string.Empty;

    /// <summary>The row's smart link (None: no ↗); Enter follows it from the keyboard.</summary>
    public LinkTarget Link => _target;

    /// <summary>True when Space (or the menu's Pick) can pick the row: its button is live.</summary>
    public bool Pickable => _pick != null && _pick.isActiveAndEnabled && _pick.interactable;

    /// <summary>Marks a form's box of <paramref name="source"/> with its key, title, its label and value as printed, and its pick button; a plain row with no link until SetLink and MarkUntranslated.</summary>
    public static AppRow Mark(GameObject row, AppTab source, string key, string title, string label, string value, Button pick)
    {
        if (!row.TryGetComponent(out AppRow marked))
            marked = row.AddComponent<AppRow>();
        marked.Key = key;
        marked.Title = title ?? string.Empty;
        marked.Source = source;
        marked._label = label;
        marked._value = value;
        marked._pick = pick;
        marked._target = LinkTarget.None;
        marked.Foreign = false;
        marked._tongueId = marked._tongueName = marked._canonical = null;
        return marked;
    }

    /// <summary>The row's smart link, for Enter (None: nothing to follow).</summary>
    public void SetLink(LinkTarget target) => _target = target;

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
}
