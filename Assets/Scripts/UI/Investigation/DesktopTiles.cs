using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The desktop's icon grid as the investigation fills it (a scanned document's
/// tile, the reference books' tiles): each tile is a clone of the builder's
/// template, labelled, that opens its window (a minimised one restores),
/// raised and focused, like every icon. Plain C#, shared by
/// CaseDocumentsPresenter and DayReference; the redesign's desktop icons
/// (phase 17) replace it.
/// </summary>
public sealed class DesktopTiles
{
    private readonly Button _template;
    private readonly Transform _grid;

    /// <summary>The tile template (inactive) and the grid it is cloned into; either missing adds no tile.</summary>
    public DesktopTiles(Button template, Transform grid)
    {
        _template = template;
        _grid = grid;
    }

    /// <summary>True when tiles can be added (the template and the grid are wired).</summary>
    public bool Ready => _template != null && _grid != null;

    /// <summary>Adds a tile opening <paramref name="window"/>, first in the grid or last; returns it, or null when nothing can be added.</summary>
    public GameObject Add(string label, DesktopWindow window, bool first)
    {
        if (!Ready || window == null)
            return null;

        Button btn = Object.Instantiate(_template, _grid);
        btn.gameObject.SetActive(true);
        if (first)
            btn.transform.SetAsFirstSibling();

        TMP_Text text = btn.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;

        btn.onClick.AddListener(window.Open);
        return btn.gameObject;
    }
}
