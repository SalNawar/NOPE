using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A window that lists rows a page at a time (reference books, the interview
/// transcript): a footer with prev/next and "Page n/m", and rows cloned from
/// <see cref="entryRowTemplate"/> (a disabled row with two TMP texts, an Image
/// background and a Button). Subclasses say how many rows there are and fill
/// each one. The serialized field names are the ones the builder wires.
/// </summary>
public abstract class PagedRowsWindow : MonoBehaviour
{
    /// <summary>Title in the window's header.</summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>Footer "Page n/m" text.</summary>
    [SerializeField] private TMP_Text pageText;

    /// <summary>Footer button to the previous page.</summary>
    [SerializeField] private Button prevButton;

    /// <summary>Footer button to the next page.</summary>
    [SerializeField] private Button nextButton;

    /// <summary>Container the page's rows are cloned under.</summary>
    [SerializeField] private Transform entryRowsRoot;

    /// <summary>Disabled row cloned per shown row.</summary>
    [SerializeField] private GameObject entryRowTemplate;

    /// <summary>Rows per page.</summary>
    [SerializeField, Min(1)] private int entriesPerPage = 6;

    /// <summary>The page shown (0-based).</summary>
    private int _page;

    /// <summary>The current page's row clones.</summary>
    private readonly List<GameObject> _rows = new();

    /// <summary>Wires the footer buttons and hides the template.</summary>
    protected virtual void Awake()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(() => ShowPage(_page - 1));

        if (nextButton != null)
            nextButton.onClick.AddListener(() => ShowPage(_page + 1));

        if (entryRowTemplate != null)
            entryRowTemplate.SetActive(false);
    }

    /// <summary>How many rows the window lists in all.</summary>
    protected abstract int RowCount { get; }

    /// <summary>Fills one cloned row (texts in child order: [0] heading/speaker, [1] value/sentence).</summary>
    protected abstract void FillRow(int index, GameObject row, TMP_Text[] texts, Image background, Button button);

    /// <summary>Sets the header title.</summary>
    protected void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }

    /// <summary>Switches to a page (clamped), updates the footer and rebuilds its rows.</summary>
    public void ShowPage(int page)
    {
        int pages = PageCount();
        _page = Mathf.Clamp(page, 0, pages - 1);

        if (pageText != null)
            pageText.text = UiText.Format("window.page", _page + 1, pages);

        if (prevButton != null)
            prevButton.interactable = _page > 0;

        if (nextButton != null)
            nextButton.interactable = _page < pages - 1;

        Rebuild();
    }

    /// <summary>Shows the newest page.</summary>
    public void ShowLastPage() => ShowPage(PageCount() - 1);

    /// <summary>Pages needed for every row (at least 1).</summary>
    private int PageCount()
    {
        int count = RowCount;
        return count == 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt(count / (float)Mathf.Max(1, entriesPerPage)));
    }

    /// <summary>Replaces the row clones with the current page's rows.</summary>
    private void Rebuild()
    {
        foreach (GameObject r in _rows)
            if (r != null)
                Destroy(r);

        _rows.Clear();

        int count = RowCount;
        if (count == 0 || entryRowsRoot == null || entryRowTemplate == null)
            return;

        int per = Mathf.Max(1, entriesPerPage);
        int start = _page * per;
        int end = Mathf.Min(start + per, count);

        for (int i = start; i < end; i++)
        {
            GameObject row = Instantiate(entryRowTemplate, entryRowsRoot);
            row.SetActive(true);
            _rows.Add(row);
            FillRow(i, row, row.GetComponentsInChildren<TMP_Text>(true), row.GetComponent<Image>(), row.GetComponent<Button>());
        }
    }
}
