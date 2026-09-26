using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A row of the Investigation app's views (the PC redesign CM3, LK2): its
/// pick fill follows its key, not the object (CompareController.IsPicked and
/// PicksChanged), so a value shown in both panes lights in both and a row
/// redrawn after a page flip is lit again; its ↗ (shown only when the row has
/// a smart link) follows the link into the other pane (Ctrl held: this pane)
/// through the pane it sits in, and never picks; its found mark (an outline)
/// shows the row a link or Back went to. The views' rows are clones of a
/// template carrying this component, bound as they are drawn.
/// </summary>
public sealed class AppRow : MonoBehaviour
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
    private string _key;
    private LinkTarget _target;
    private Color _own;
    private bool _ownRead;
    private bool _wired;

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
        _key = key;
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
        fill.color = _compare != null && _compare.IsPicked(_key) ? _compare.HighlightColor : _own;
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
