using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A row of the Investigation app's views. Two roles, one component:
/// <para>Picks and links (phase 18, the PC redesign CM3, LK2): its pick fill
/// follows its key, not the object (CompareController.IsPicked and
/// PicksChanged), so a value shown in both panes lights in both and a row
/// redrawn after a page flip is lit again; its ↗ (shown only when the row has
/// a smart link) follows the link into the other pane (Ctrl held: this pane)
/// through the pane it sits in, and never picks; its found mark (an outline)
/// shows the row a link or Back went to. The views' rows are clones of a
/// template carrying this component, bound as they are drawn.</para>
/// <para>Entries (phase 20, CP1, PR1, KB4): a document's field, a transcript
/// line, a book's row or a record's evidence row is marked by its page
/// component as it is filled (Mark), with the row's key and title (its pick's
/// label: "Visa · Visa Class"), its label and value (the texts it shows, or a
/// form box's field) and its pick button; an untranslated transcript line also
/// carries its tongue and its canonical text. The app's focus ring walks these
/// rows in reading order (the page's child order); Space picks the row (its
/// button, the same as a click), Ctrl+C copies its value as shown and
/// Ctrl+Shift+C "Label: value", Ctrl+P pins it; a right-click opens the row's
/// context menu (Copy value, Copy row, Pin or Unpin, Pick for compare).</para>
/// </summary>
public sealed class AppRow : MonoBehaviour, IPointerClickHandler
{
    /// <summary>The row's background: tinted with the compare's highlight while its key is picked.</summary>
    [SerializeField] private Image fill;

    /// <summary>The ↗ after the value (hidden when the row has no link).</summary>
    [SerializeField] private Button link;

    /// <summary>The ↗'s hover hint text (where the link goes).</summary>
    [SerializeField] private TMP_Text linkHint;

    /// <summary>The found mark: an outline around the row a link went to.</summary>
    [SerializeField] private GameObject found;

    private CompareController _compare;
    private string _pickKey;
    private LinkTarget _target;
    private Color _own;
    private bool _ownRead;
    private bool _wired;

    private TMP_Text _labelText;
    private TMP_Text _valueText;
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

    /// <summary>The row's label as shown.</summary>
    public string Label => _labelText != null ? _labelText.text : _label ?? string.Empty;

    /// <summary>The row's value as shown.</summary>
    public string Value => _valueText != null ? _valueText.text : _value ?? string.Empty;

    /// <summary>The row's smart link (None: no ↗); Enter follows it from the keyboard.</summary>
    public LinkTarget Link => _target;

    /// <summary>True when Space (or the menu's Pick) can pick the row: its button is live.</summary>
    public bool Pickable => _pick != null && _pick.isActiveAndEnabled && _pick.interactable;

    /// <summary>Lights the row while <paramref name="key"/> is picked in <paramref name="compare"/> (null key: never), now and on every change of the picks.</summary>
    public void Bind(CompareController compare, string key)
    {
        if (_compare != compare)
        {
            if (_compare != null)
                _compare.PicksChanged -= Relight;
            _compare = compare;
            if (_compare != null)
                _compare.PicksChanged += Relight;
        }
        _pickKey = key;
        Relight();
    }

    /// <summary>Shows the ↗ for <paramref name="target"/> with its hover hint (None: no ↗).</summary>
    public void SetLink(LinkTarget target, string hint)
    {
        Wire();
        _target = target;
        if (link != null && link.gameObject.activeSelf != !target.IsNone)
            link.gameObject.SetActive(!target.IsNone);
        if (linkHint != null)
            linkHint.text = hint ?? string.Empty;
    }

    /// <summary>Shows or hides the found mark.</summary>
    public void SetFound(bool on)
    {
        if (found != null && found.activeSelf != on)
            found.SetActive(on);
    }

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

    private void OnDestroy()
    {
        if (_compare != null)
            _compare.PicksChanged -= Relight;
    }

    /// <summary>The pick fill: the compare's highlight while the key is picked, else the row's own colour. A row destroyed before it ever woke (drawn in a hidden view) gets no OnDestroy: it stops listening here.</summary>
    private void Relight()
    {
        if (this == null)
        {
            _compare.PicksChanged -= Relight;
            return;
        }
        if (fill == null)
            return;
        if (!_ownRead)
        {
            _own = fill.color;
            _ownRead = true;
        }
        fill.color = _compare != null && _compare.IsPicked(_pickKey) ? _compare.HighlightColor : _own;
    }

    /// <summary>The ↗'s click, once.</summary>
    private void Wire()
    {
        if (_wired || link == null)
            return;
        _wired = true;
        link.onClick.AddListener(Follow);
    }

    /// <summary>The ↗: the link, through the pane the row is in (LK2: the other pane; Ctrl held, this one).</summary>
    private void Follow()
    {
        AppPane pane = GetComponentInParent<AppPane>();
        if (!_target.IsNone && pane != null)
            pane.FollowLink(_target);
    }
}
