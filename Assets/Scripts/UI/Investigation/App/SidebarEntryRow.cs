using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>One row of a sidebar list (Pinned or Recent): its item's label; a click (or Enter on it) jumps to the item, a right-click tells the list (a pin unpins).</summary>
public sealed class SidebarEntryRow : MonoBehaviour, IPointerClickHandler
{
    /// <summary>The row's button (a click jumps).</summary>
    [SerializeField] private Button button;

    /// <summary>The item's label.</summary>
    [SerializeField] private TMP_Text label;

    private SidebarEntryList _list;
    private EntryItem _item;
    private bool _wired;

    /// <summary>The item the row shows.</summary>
    public EntryItem Item => _item;

    /// <summary>The row's button (the focus ring's Enter presses it).</summary>
    public Button Button => button;

    /// <summary>Shows <paramref name="item"/> in <paramref name="list"/>.</summary>
    public void Bind(SidebarEntryList list, EntryItem item)
    {
        _list = list;
        _item = item;
        if (label != null)
            label.text = item.Label;
        if (!_wired && button != null)
        {
            _wired = true;
            button.onClick.AddListener(() => _list.Open(_item));
        }
    }

    /// <summary>A right-click tells the list.</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && _list != null)
            _list.RightClicked(_item);
    }
}
