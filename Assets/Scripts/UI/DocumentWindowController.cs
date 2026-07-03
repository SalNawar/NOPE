using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders one visitor document as a flippable, multi-page window. Each field
/// is a clickable row (label + value) that registers with the CompareController.
/// Rows are cloned from <see cref="fieldRowTemplate"/> (a disabled row with two
/// TMP texts — label then value — an Image background, and a Button).
/// </summary>
public sealed class DocumentWindowController : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Transform fieldRowsRoot;
    [SerializeField] private GameObject fieldRowTemplate;

    private DocumentInstance _doc;
    private CompareController _compare;
    private int _page;
    private readonly List<GameObject> _rows = new();

    private void Awake()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(() => ShowPage(_page - 1));

        if (nextButton != null)
            nextButton.onClick.AddListener(() => ShowPage(_page + 1));

        if (fieldRowTemplate != null)
            fieldRowTemplate.SetActive(false);
    }

    /// <summary>Binds a document and renders its first page.</summary>
    public void SetDocument(DocumentInstance doc, CompareController compare)
    {
        _doc = doc;
        _compare = compare;
        _page = 0;

        if (titleText != null)
            titleText.text = doc != null && doc.template != null ? doc.template.displayName : "Document";

        ShowPage(0);
    }

    /// <summary>Switches to a page (clamped) and rebuilds its rows.</summary>
    public void ShowPage(int page)
    {
        if (_doc == null)
            return;

        int pages = _doc.PageCount;
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

        if (_doc == null || fieldRowsRoot == null || fieldRowTemplate == null)
            return;

        foreach (DocumentField f in _doc.fields)
        {
            if (f == null || f.page != _page)
                continue;

            GameObject row = Instantiate(fieldRowTemplate, fieldRowsRoot);
            row.SetActive(true);
            _rows.Add(row);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && texts[0] != null)
                texts[0].text = f.label;
            if (texts.Length > 1 && texts[1] != null)
                texts[1].text = f.value;

            Image bg = row.GetComponent<Image>();
            Button btn = row.GetComponent<Button>();

            string docName = _doc.template != null ? _doc.template.displayName : "Document";
            string label = $"{docName} · {f.label}";
            string value = f.value;
            DocumentField field = f;

            if (btn != null && _compare != null)
                btn.onClick.AddListener(() => _compare.Select(label, value, bg, CompareEvidence.FromDocumentField(field)));
        }
    }
}
