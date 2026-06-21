using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a reference book as a flippable window. Each entry (Nation — Era :
/// value) is a clickable row the player can compare against a document field.
/// Rows are cloned from <see cref="entryRowTemplate"/> (a disabled row with two
/// TMP texts — heading then value — an Image background, and a Button).
/// </summary>
public sealed class ReferenceBookWindowController : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Transform entryRowsRoot;
    [SerializeField] private GameObject entryRowTemplate;
    [SerializeField, Min(1)] private int entriesPerPage = 6;

    private ReferenceBookSO _book;
    private CompareController _compare;
    private int _page;
    private readonly List<GameObject> _rows = new();

    private void Awake()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(() => ShowPage(_page - 1));

        if (nextButton != null)
            nextButton.onClick.AddListener(() => ShowPage(_page + 1));

        if (entryRowTemplate != null)
            entryRowTemplate.SetActive(false);
    }

    /// <summary>Binds a book and renders its first page.</summary>
    public void SetBook(ReferenceBookSO book, CompareController compare)
    {
        _book = book;
        _compare = compare;
        _page = 0;

        if (titleText != null)
            titleText.text = book != null ? book.displayName : "Reference";

        ShowPage(0);
    }

    private int PageCount()
    {
        if (_book == null || _book.entries == null || _book.entries.Count == 0)
            return 1;

        return Mathf.Max(1, Mathf.CeilToInt(_book.entries.Count / (float)Mathf.Max(1, entriesPerPage)));
    }

    /// <summary>Switches to a page (clamped) and rebuilds its rows.</summary>
    public void ShowPage(int page)
    {
        int pages = PageCount();
        _page = Mathf.Clamp(page, 0, pages - 1);

        if (pageText != null)
            pageText.text = $"Page {_page + 1}/{pages}";

        if (prevButton != null)
            prevButton.interactable = _page > 0;

        if (nextButton != null)
            nextButton.interactable = _page < pages - 1;

        Rebuild();
    }

    private void Rebuild()
    {
        foreach (GameObject r in _rows)
            if (r != null)
                Destroy(r);

        _rows.Clear();

        if (_book == null || _book.entries == null || entryRowsRoot == null || entryRowTemplate == null)
            return;

        int per = Mathf.Max(1, entriesPerPage);
        int start = _page * per;
        int end = Mathf.Min(start + per, _book.entries.Count);

        for (int i = start; i < end; i++)
        {
            ReferenceEntry e = _book.entries[i];
            if (e == null)
                continue;

            GameObject row = Instantiate(entryRowTemplate, entryRowsRoot);
            row.SetActive(true);
            _rows.Add(row);

            string nation = e.nation != null ? e.nation.displayName : "Any";
            string era = e.era != null ? e.era.displayName : "?";

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && texts[0] != null)
                texts[0].text = $"{nation} — {era}";
            if (texts.Length > 1 && texts[1] != null)
                texts[1].text = e.value;

            Image bg = row.GetComponent<Image>();
            Button btn = row.GetComponent<Button>();

            string bookName = _book != null ? _book.displayName : "Reference";
            string label = $"{bookName}: {nation}/{era}";
            string value = e.value;

            if (btn != null && _compare != null)
                btn.onClick.AddListener(() => _compare.Select(label, value, bg));
        }
    }
}
