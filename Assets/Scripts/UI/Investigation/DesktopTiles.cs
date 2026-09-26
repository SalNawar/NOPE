using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The desktop's icon grid as the investigation fills it (a scanned document's
/// tile, the reference books' tiles): each tile is a clone of the builder's
/// template, labelled, that opens its window (a minimised one restores),
/// raised and focused, like every icon; a reference book's tile shows its
/// cover left of its label when the cover's art exists (the template's
/// inactive Cover child, redesign phase 27). Plain C#, shared by
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

    /// <summary>Adds a tile opening <paramref name="window"/>, first in the grid or last, showing <paramref name="cover"/> when given; returns it, or null when nothing can be added.</summary>
    public GameObject Add(string label, DesktopWindow window, bool first, Sprite cover = null)
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

        Transform coverSlot = cover != null ? btn.transform.Find("Cover") : null;
        if (coverSlot != null && coverSlot.TryGetComponent(out Image image))
        {
            image.sprite = cover;
            coverSlot.gameObject.SetActive(true);
            if (text != null && coverSlot is RectTransform coverRect)
                text.margin = new Vector4(2f * coverRect.anchoredPosition.x + coverRect.sizeDelta.x, text.margin.y, text.margin.z, text.margin.w);
        }

        btn.onClick.AddListener(window.Open);
        return btn.gameObject;
    }
}
