using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a reference book as a flippable window. Its rows are today's facts
/// for the book's category (FactTable.Rows), each a clickable "place : value"
/// row the player can compare against a document field. Rows are cloned from
/// <see cref="entryRowTemplate"/> (a disabled row with two TMP texts — heading
/// then value — an Image background, and a Button).
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
    private FactTable _facts;
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

    /// <summary>Binds a book cover to today's facts and renders its first page.</summary>
    public void SetBook(ReferenceBookSO book, FactTable facts, CompareController compare)
    {
        _book = book;
        _facts = facts;
        _compare = compare;
        _page = 0;

        if (titleText != null)
            titleText.text = book != null ? book.displayName : "Reference";

        ShowPage(0);
    }

    /// <summary>Today's rows for this book (empty when unbound).</summary>
    private IReadOnlyList<FactRow> Rows() =>
        _book != null && _facts != null ? _facts.Rows(_book.category) : System.Array.Empty<FactRow>();

    private int PageCount()
    {
        int count = Rows().Count;
        return count == 0 ? 1 : Mathf.Max(1, Mathf.CeilToInt(count / (float)Mathf.Max(1, entriesPerPage)));
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

        IReadOnlyList<FactRow> rows = Rows();
        if (rows.Count == 0 || entryRowsRoot == null || entryRowTemplate == null)
            return;

        int per = Mathf.Max(1, entriesPerPage);
        int start = _page * per;
        int end = Mathf.Min(start + per, rows.Count);
        string bookName = _book != null ? _book.displayName : "Reference";

        for (int i = start; i < end; i++)
        {
            FactRow fact = rows[i];

            GameObject row = Instantiate(entryRowTemplate, entryRowsRoot);
            row.SetActive(true);
            _rows.Add(row);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && texts[0] != null)
                texts[0].text = fact.OriginLabel;
            if (texts.Length > 1 && texts[1] != null)
                texts[1].text = fact.Value;

            Image bg = row.GetComponent<Image>();
            Button btn = row.GetComponent<Button>();

            string label = $"{bookName}: {fact.OriginLabel}";
            string value = fact.Value;
            CompareEvidence evidence = fact.ToEvidence();

            if (btn != null && _compare != null)
                btn.onClick.AddListener(() => _compare.Select(label, value, bg, evidence));
        }
    }
}
