using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One row of a sidebar list (Pinned or Recent): its item's label; a click (or Enter on it) jumps to the item (Ctrl+P on it unpins a pin; a right-click backs out, ControlRules).</summary>
public sealed class SidebarEntryRow : MonoBehaviour
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
}
