using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One of the Investigation app's sidebar lists (the PC redesign PR1, PR2):
/// Pinned (the pins in pin order; a right-click on one unpins it) or Recent
/// (the last items opened or jumped to, newest first). A row reads the
/// item's label; a click on it jumps there (the app shows its tab, its item
/// and its row). An empty list shows its hint. Rows are pooled from the
/// template; the app redraws the list when the pins or recent items change.
/// </summary>
public sealed class SidebarEntryList : MonoBehaviour
{
    /// <summary>The app (a row's jump, a pin's unpin).</summary>
    [SerializeField] private InvestigationApp app;

    /// <summary>True for the pins (a right-click unpins); false for the recent items.</summary>
    [SerializeField] private bool pins;

    /// <summary>Where the rows go (a vertical layout).</summary>
    [SerializeField] private RectTransform rowsRoot;

    /// <summary>A row (inactive), cloned per item.</summary>
    [SerializeField] private SidebarEntryRow rowTemplate;

    /// <summary>The hint shown while the list is empty.</summary>
    [SerializeField] private GameObject emptyHint;

    private readonly List<SidebarEntryRow> _rows = new List<SidebarEntryRow>();
    private readonly List<SidebarEntryRow> _shown = new List<SidebarEntryRow>();

    /// <summary>The rows shown, top to bottom (the app's focus ring walks them).</summary>
    public IReadOnlyList<SidebarEntryRow> Rows => _shown;

    /// <summary>Lists <paramref name="items"/> (the hint when there are none).</summary>
    public void Show(IReadOnlyList<EntryItem> items)
    {
        if (rowTemplate != null && rowTemplate.gameObject.activeSelf)
            rowTemplate.gameObject.SetActive(false);
        _shown.Clear();
        int count = items != null ? items.Count : 0;
        while (_rows.Count < count && rowTemplate != null && rowsRoot != null)
        {
            SidebarEntryRow row = Instantiate(rowTemplate, rowsRoot);
            row.gameObject.name = "Entry";
            _rows.Add(row);
        }
        for (int i = 0; i < _rows.Count; i++)
        {
            bool used = i < count;
            if (_rows[i].gameObject.activeSelf != used)
                _rows[i].gameObject.SetActive(used);
            if (!used)
                continue;
            _rows[i].Bind(this, items[i]);
            _shown.Add(_rows[i]);
        }
        if (emptyHint != null && emptyHint.activeSelf != (count == 0))
            emptyHint.SetActive(count == 0);
    }

    /// <summary>A row was clicked: the app jumps to its item.</summary>
    public void Open(EntryItem item)
    {
        if (app != null)
            app.Jump(item);
    }

    /// <summary>A row was right-clicked: a pin is unpinned.</summary>
    public void RightClicked(EntryItem item)
    {
        if (pins && app != null)
            app.Unpin(item.Ref.Key);
    }
}
