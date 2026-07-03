using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates the office investigation: shows the visitor's travel claim and
/// today's directives, spawns a draggable window per document, builds a shelf of
/// reference books the player can open/stow, and offers the binary Accept/Deny.
///
/// Two modes:
/// - RICH: when the desk has been built (document/book/shelf templates wired by
///   the Tools &gt; TimeDesk office builder), it spawns draggable, comparable,
///   multi-page windows.
/// - FALLBACK: if those references are not wired yet, it builds a simple text
///   panel at runtime so the Accept/Deny loop is fully playable immediately.
/// </summary>
public sealed class InvestigationUIController : MonoBehaviour
{
    [Header("Shared")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text claimText;
    [SerializeField] private TMP_Text directivesText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button denyButton;
    [SerializeField] private CompareController compareController;

    [Header("Rich mode (optional — built by the office tool)")]
    [SerializeField] private RectTransform windowLayer;
    [SerializeField] private DocumentWindowController documentWindowTemplate;
    [SerializeField] private ReferenceBookWindowController bookWindowTemplate;
    [SerializeField] private Transform bookShelfRoot;
    [SerializeField] private Button bookShelfButtonTemplate;

    [Header("Scanner (deviation report)")]
    /// <summary>Body text of the Scanner window; lists documented discrepancies.</summary>
    [SerializeField] private TMP_Text scannerText;

    /// <summary>Scanner window chrome; opened when the first discrepancy registers.</summary>
    [SerializeField] private OSWindowChrome scannerWindow;

    private Action<bool> _onDecision;
    private readonly List<GameObject> _docWindows = new();
    private readonly List<GameObject> _docIcons = new();
    private bool _booksBuilt;
    private string _directives = "Directives: all destinations cleared.";

    /// <summary>Documented contradictions for the current case.</summary>
    private readonly DiscrepancyLog _discrepancies = new();

    /// <summary>The case currently on the desk (null between cases).</summary>
    private CaseInstance _currentCase;

    /// <summary>Number of discrepancies documented for the current case.</summary>
    public int EvidenceCount => _discrepancies.Count;

    /// <summary>
    /// True when the evidence loop is playable (rich desk + compare wired), so
    /// scoring may gate denials on documented evidence.
    /// </summary>
    public bool EvidenceSystemActive => RichMode && compareController != null;

    // Fallback state
    private bool _fallbackBuilt;
    private GameObject _fallbackPanel;
    private TMP_Text _fallbackClaim;
    private TMP_Text _fallbackBody;

    private bool RichMode =>
        documentWindowTemplate != null && windowLayer != null &&
        acceptButton != null && denyButton != null;

    private void Awake()
    {
        if (documentWindowTemplate != null) documentWindowTemplate.gameObject.SetActive(false);
        if (bookWindowTemplate != null) bookWindowTemplate.gameObject.SetActive(false);
        if (bookShelfButtonTemplate != null) bookShelfButtonTemplate.gameObject.SetActive(false);
        if (root != null) root.SetActive(false);

        if (compareController != null)
            compareController.PairCompared += HandlePairCompared;
    }

    private void OnDestroy()
    {
        if (compareController != null)
            compareController.PairCompared -= HandlePairCompared;
    }

    /// <summary>
    /// Auto-registers a true contradiction when the player compares a forged
    /// document field against the reference entry that disproves it.
    /// </summary>
    private void HandlePairCompared(CompareEvidence a, CompareEvidence b)
    {
        if (_currentCase == null)
            return;

        Discrepancy found = _discrepancies.TryRegister(a, b,
            _currentCase.claimedNation != null ? _currentCase.claimedNation.id : null,
            _currentCase.claimedEra != null ? _currentCase.claimedEra.id : null);
        if (found == null)
            return;

        RefreshScannerText();

        if (compareController != null)
            compareController.ShowLoggedNotice();

        if (scannerWindow != null)
            scannerWindow.Open();
    }

    /// <summary>Rewrites the Scanner window body from the discrepancy log.</summary>
    private void RefreshScannerText()
    {
        if (scannerText == null)
            return;

        if (_discrepancies.Count == 0)
        {
            scannerText.text =
                "No deviations documented.\n\n" +
                "Compare a document field against the matching reference entry " +
                "for the claimed era to log evidence.";
            return;
        }

        var sb = new StringBuilder();
        foreach (Discrepancy d in _discrepancies.Items)
            sb.AppendLine("• " + d.Summary);

        sb.AppendLine();
        sb.AppendLine($"{_discrepancies.Count} deviation(s) documented. Denial is justified.");
        scannerText.text = sb.ToString();
    }

    /// <summary>Sets the day's travel directives (shown during every case).</summary>
    public void SetDirectives(IReadOnlyList<TravelRuleSO> rules)
    {
        _directives = BuildDirectives(rules);
        if (directivesText != null)
            directivesText.text = _directives;
    }

    private static string BuildDirectives(IReadOnlyList<TravelRuleSO> rules)
    {
        if (rules == null || rules.Count == 0)
            return "Directives: all destinations cleared today.";

        var sb = new StringBuilder("Directives (deny violators):\n");
        foreach (TravelRuleSO r in rules)
            if (r != null)
                sb.AppendLine("• " + r.Summary());
        return sb.ToString();
    }

    /// <summary>Presents a case and waits for the player's Accept/Deny.</summary>
    public void ShowCase(CaseInstance inst, ContentLibrarySO lib, Action<bool> onDecision)
    {
        _onDecision = onDecision;
        _currentCase = inst;
        _discrepancies.Clear();
        RefreshScannerText();

        if (RichMode)
            ShowRich(inst, lib);
        else
            ShowFallback(inst, lib);
    }

    /// <summary>Hides the investigation overlay (between cases).</summary>
    public void Hide()
    {
        if (root != null) root.SetActive(false);
        if (_fallbackPanel != null) _fallbackPanel.SetActive(false);
    }

    // -----------------------------
    // Rich mode
    // -----------------------------

    private void ShowRich(CaseInstance inst, ContentLibrarySO lib)
    {
        if (root != null) root.SetActive(true);

        if (claimText != null)
            claimText.text = inst != null ? $"{inst.visitorDisplayName}\n\"{inst.claimLine}\"" : string.Empty;

        if (directivesText != null)
            directivesText.text = _directives;

        foreach (GameObject w in _docWindows)
            if (w != null)
                Destroy(w);
        _docWindows.Clear();

        foreach (GameObject ic in _docIcons)
            if (ic != null)
                Destroy(ic);
        _docIcons.Clear();

        if (inst != null)
        {
            int i = 0;
            foreach (DocumentInstance doc in inst.documents)
            {
                DocumentWindowController clone = Instantiate(documentWindowTemplate, windowLayer);
                clone.gameObject.SetActive(false); // opened from its desktop icon
                if (clone.transform is RectTransform rt)
                    rt.anchoredPosition = new Vector2(-330f + i * 620f, 140f);
                clone.SetDocument(doc, compareController);
                _docWindows.Add(clone.gameObject);

                string docName = doc != null && doc.template != null ? doc.template.displayName : "Document";
                AddDesktopIcon(docName, clone.gameObject, true);
                i++;
            }
        }

        BuildBookShelf(lib);

        if (compareController != null)
            compareController.Clear();

        WireDecisionButtons(acceptButton, denyButton);
    }

    private void BuildBookShelf(ContentLibrarySO lib)
    {
        if (_booksBuilt)
            return;

        _booksBuilt = true;

        if (lib == null || bookWindowTemplate == null || bookShelfButtonTemplate == null ||
            bookShelfRoot == null || windowLayer == null)
            return;

        int i = 0;
        foreach (ReferenceBookSO book in lib.ReferenceBooks)
        {
            if (book == null)
                continue;

            ReferenceBookWindowController win = Instantiate(bookWindowTemplate, windowLayer);
            win.SetBook(book, compareController);
            if (win.transform is RectTransform rt)
                rt.anchoredPosition = new Vector2(-380f + i * 320f, -150f);
            GameObject winGo = win.gameObject;
            winGo.SetActive(false);

            AddDesktopIcon(book.displayName, winGo, false);
            i++;
        }
    }

    /// <summary>
    /// Adds a desktop icon tile (in the icon grid) that toggles a window's
    /// visibility. Document icons are tracked so they can be cleared per case and
    /// sit at the top of the grid.
    /// </summary>
    private void AddDesktopIcon(string label, GameObject window, bool isDocument)
    {
        if (bookShelfButtonTemplate == null || bookShelfRoot == null || window == null)
            return;

        Button btn = Instantiate(bookShelfButtonTemplate, bookShelfRoot);
        btn.gameObject.SetActive(true);
        if (isDocument)
            btn.transform.SetAsFirstSibling();

        TMP_Text text = btn.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;

        GameObject captured = window;
        btn.onClick.AddListener(() =>
        {
            bool now = !captured.activeSelf;
            captured.SetActive(now);
            if (now)
                captured.transform.SetAsLastSibling();
        });

        if (isDocument)
            _docIcons.Add(btn.gameObject);
    }

    private void WireDecisionButtons(Button accept, Button deny)
    {
        if (accept != null)
        {
            accept.onClick.RemoveAllListeners();
            accept.onClick.AddListener(() => Decide(true));
        }

        if (deny != null)
        {
            deny.onClick.RemoveAllListeners();
            deny.onClick.AddListener(() => Decide(false));
        }
    }

    private void Decide(bool accepted)
    {
        Hide();
        Action<bool> cb = _onDecision;
        _onDecision = null;
        cb?.Invoke(accepted);
    }

    // -----------------------------
    // Fallback mode (no rich desk yet)
    // -----------------------------

    private void ShowFallback(CaseInstance inst, ContentLibrarySO lib)
    {
        EnsureFallback();

        _fallbackPanel.SetActive(true);
        _fallbackPanel.transform.SetAsLastSibling();

        if (_fallbackClaim != null)
            _fallbackClaim.text = inst != null
                ? $"{inst.visitorDisplayName}\n\"{inst.claimLine}\"\n\n{_directives}"
                : string.Empty;

        if (_fallbackBody != null)
            _fallbackBody.text = BuildFallbackBody(inst, lib);
    }

    private static string BuildFallbackBody(CaseInstance inst, ContentLibrarySO lib)
    {
        var sb = new StringBuilder();

        if (inst != null)
        {
            sb.AppendLine("— DOCUMENTS PRESENTED —");
            foreach (DocumentInstance doc in inst.documents)
            {
                sb.AppendLine($"[{(doc.template != null ? doc.template.displayName : "Document")}]");
                foreach (DocumentField f in doc.fields)
                    sb.AppendLine($"    {f.label}: {f.value}");
            }
            sb.AppendLine();
        }

        if (lib != null && lib.ReferenceBooks.Count > 0)
        {
            sb.AppendLine("— REFERENCE BOOKS (cross-check) —");
            foreach (ReferenceBookSO book in lib.ReferenceBooks)
            {
                if (book == null)
                    continue;
                sb.AppendLine($"[{book.displayName}]");
                if (book.entries != null)
                    foreach (ReferenceEntry e in book.entries)
                        if (e != null)
                            sb.AppendLine($"    {(e.nation != null ? e.nation.displayName : "Any")} — {(e.era != null ? e.era.displayName : "?")}: {e.value}");
            }
        }

        return sb.ToString();
    }

    private void EnsureFallback()
    {
        if (_fallbackBuilt)
            return;

        _fallbackBuilt = true;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();

        Transform parent = canvas != null ? canvas.transform : transform;

        _fallbackPanel = NewUI("InvestigationFallback", parent);
        Stretch((RectTransform)_fallbackPanel.transform, new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.92f));
        Image bg = _fallbackPanel.AddComponent<Image>();
        bg.color = new Color(0.09f, 0.11f, 0.16f, 0.98f);

        _fallbackClaim = NewText(_fallbackPanel.transform, "Claim", 24, TextAlignmentOptions.TopLeft,
            new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.97f));
        _fallbackBody = NewText(_fallbackPanel.transform, "Body", 20, TextAlignmentOptions.TopLeft,
            new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.76f));

        Button accept = NewButton(_fallbackPanel.transform, "AcceptButton", "ACCEPT (approve travel)",
            new Vector2(0.06f, 0.04f), new Vector2(0.48f, 0.13f), new Color(0.15f, 0.4f, 0.2f, 1f));
        Button deny = NewButton(_fallbackPanel.transform, "DenyButton", "DENY (refuse travel)",
            new Vector2(0.52f, 0.04f), new Vector2(0.94f, 0.13f), new Color(0.45f, 0.16f, 0.16f, 1f));

        WireDecisionButtons(accept, deny);
    }

    // -----------------------------
    // Tiny runtime UI helpers (fallback only)
    // -----------------------------

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TMP_Text NewText(Transform parent, string name, int size, TextAlignmentOptions align, Vector2 min, Vector2 max)
    {
        GameObject go = NewUI(name, parent);
        Stretch((RectTransform)go.transform, min, max);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        return t;
    }

    private static Button NewButton(Transform parent, string name, string label, Vector2 min, Vector2 max, Color color)
    {
        GameObject go = NewUI(name, parent);
        Stretch((RectTransform)go.transform, min, max);
        Image img = go.AddComponent<Image>();
        img.color = color;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        TMP_Text t = NewText(go.transform, "Label", 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        t.text = label;
        return btn;
    }
}
