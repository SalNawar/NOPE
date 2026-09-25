using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates the office investigation: shows the visitor's travel claim and
/// today's directives, runs the interview on the traveller wheel (document
/// requests, today's questions and narrative dialogs, with the transcript
/// window and the traveller's replies in the wheel's bubble), hands each
/// document over as a physical paper on the desk (whose scan opens its
/// window) or, where no desk is wired, straight to its draggable window,
/// builds a shelf of reference books the player can open/stow, and offers the
/// binary Accept/Deny.
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

    [Header("Interaction / records")]
    /// <summary>The traveller wheel's ring: shows the current interview node's choices (requests, questions, dialog replies).</summary>
    [SerializeField] private InteractionPanelController interactionPanel;

    /// <summary>Citizen Records app (registry injected per day).</summary>
    [SerializeField] private CitizenRecordsWindowController recordsWindow;

    [Header("Interview")]
    /// <summary>Case Notes: Interview, the current traveller's transcript (answer rows are compare-clickable).</summary>
    [SerializeField] private TranscriptWindowController transcriptWindow;

    /// <summary>The transcript window's chrome; every interview choice but a document request opens it.</summary>
    [SerializeField] private OSWindowChrome transcriptChrome;

    [Header("Desk")]
    /// <summary>The physical papers and the scanner (optional: without it documents open on request, straight to their windows).</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The traveller wheel: closed after a hand-over, and it shows the traveller's replies.</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>Shown on the desktop between travellers.</summary>
    [SerializeField] private GameObject idleScreen;

    [Header("Window layout")]
    /// <summary>Where the first document window opens (desktop units from the centre). The builder writes the 4:3 layout; this default is the 16:9 one.</summary>
    [SerializeField] private Vector2 documentWindowOrigin = new Vector2(-330f, 140f);

    /// <summary>Offset from one document window to the next.</summary>
    [SerializeField] private Vector2 documentWindowStep = new Vector2(620f, 0f);

    /// <summary>Where the first book window opens.</summary>
    [SerializeField] private Vector2 bookWindowOrigin = new Vector2(-380f, -150f);

    /// <summary>Horizontal step between the three book windows of a row.</summary>
    [SerializeField] private float bookWindowColumnStep = 320f;

    /// <summary>Offset from one row of book windows to the next.</summary>
    [SerializeField] private Vector2 bookWindowRowStep = new Vector2(40f, 40f);

    private Action<bool> _onDecision;
    private readonly List<GameObject> _docWindows = new();
    private readonly List<GameObject> _docIcons = new();

    /// <summary>The current traveller's documents in paper order (name, holder, hand-over).</summary>
    private readonly List<CaseDocument> _caseDocuments = new();

    /// <summary>Papers whose window already has a desktop icon this case.</summary>
    private readonly HashSet<int> _iconedDocuments = new();
    private bool _booksBuilt;
    private string _directives = "Directives: all destinations cleared.";

    /// <summary>Today's facts (set by GameManager; the books render these rows).</summary>
    private FactTable _facts;

    /// <summary>Today's citizen registry (set by GameManager; the text fallback prints the current traveller's record).</summary>
    private CitizenRegistry _registry;

    /// <summary>Documented contradictions for the current case.</summary>
    private readonly DiscrepancyLog _discrepancies = new();

    /// <summary>The case currently on the desk (null between cases).</summary>
    private CaseInstance _currentCase;

    /// <summary>Today's interview (set by GameManager): askable questions, offered dialogs, wording and the shift's dialog outcomes.</summary>
    private InterviewDay _day;

    /// <summary>The current traveller's interview (null before the first case).</summary>
    private DialogRunner _runner;

    /// <summary>Number of discrepancies documented for the current case.</summary>
    public int EvidenceCount => _discrepancies.Count;

    /// <summary>
    /// True when the evidence loop is playable (rich desk + compare wired), so
    /// scoring may gate denials on documented evidence.
    /// </summary>
    public bool EvidenceSystemActive => RichMode && compareController != null;

    /// <summary>
    /// True when a traveller's answers can be read: always in the text
    /// fallback; in the rich desk only when the wheel's ring, the transcript
    /// window and its chrome are wired. When false, GameManager computes no answers
    /// and generates no spoken tell that day. (Serialized references are
    /// compared with != null: an unassigned one is Unity's fake null.)
    /// </summary>
    public bool InterviewReachable =>
        !RichMode || (interactionPanel != null && transcriptWindow != null && transcriptChrome != null);

    /// <summary>
    /// True when a traveller's garments can be looked at and compared: always
    /// in the text fallback (which prints the dress); in the rich desk only
    /// when the wheel's ring (its "Look >" menu) and the compare bar are wired.
    /// When false, GameManager generates no dress tell that day.
    /// </summary>
    public bool AppearanceReachable =>
        !RichMode || (interactionPanel != null && compareController != null);

    // Fallback state
    private bool _fallbackBuilt;
    private GameObject _fallbackPanel;
    private TMP_Text _fallbackClaim;
    private TMP_Text _fallbackBody;

    private bool RichMode =>
        documentWindowTemplate != null && windowLayer != null &&
        acceptButton != null && denyButton != null;

    /// <summary>True when documents become physical papers: the rich desk with the desk and all its parts wired (a partly wired desk takes the window path, so papers always reach the PC).</summary>
    private bool DeskReachable => RichMode && desk != null && desk.IsReachable;

    private void Awake()
    {
        if (documentWindowTemplate != null) documentWindowTemplate.gameObject.SetActive(false);
        if (bookWindowTemplate != null) bookWindowTemplate.gameObject.SetActive(false);
        if (bookShelfButtonTemplate != null) bookShelfButtonTemplate.gameObject.SetActive(false);
        if (root != null) root.SetActive(false);

        if (compareController != null)
            compareController.PairCompared += HandlePairCompared;

        // Birth-date tells are proven only against Citizen Records (RecordMismatch).
        if (EvidenceSystemActive && recordsWindow == null)
            Debug.LogWarning("[InvestigationUIController] Citizen Records not wired: birth-date tells cannot be proven. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the transcript nothing a traveller says could be read, so the day speaks no tell.
        if (RichMode && !InterviewReachable)
            Debug.LogWarning("[InvestigationUIController] Traveller wheel or interview transcript not wired: questions are hidden and no tell is spoken today. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the wheel's look menu or the compare bar no garment could be compared, so the day leaks no dress.
        if (RichMode && !AppearanceReachable)
            Debug.LogWarning("[InvestigationUIController] Traveller wheel or compare bar not wired (interactionPanel or compareController): garments cannot be looked at and no dress tell is generated today. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the desk every document still reaches the PC, as its window.
        if (RichMode && !DeskReachable)
            Debug.LogWarning("[InvestigationUIController] Desk scanner not wired: documents open on the PC when handed over (no physical papers). Run Tools > TimeDesk > Build Office UI.", this);

        if (DeskReachable)
            desk.ScanFinished += OpenDocumentWindow;

        if (idleScreen != null)
            idleScreen.SetActive(true);
    }

    private void OnDestroy()
    {
        if (compareController != null)
            compareController.PairCompared -= HandlePairCompared;

        if (DeskReachable)
            desk.ScanFinished -= OpenDocumentWindow;
    }

    /// <summary>
    /// Documents a true contradiction when the player compares a liar's tell
    /// against the reference entry or record that disproves it; proving an
    /// already documented category again only says so in the compare bar.
    /// </summary>
    private void HandlePairCompared(CompareEvidence a, CompareEvidence b)
    {
        if (_currentCase == null)
            return;

        Discrepancy proof = DiscrepancyLog.Prove(a, b,
            _currentCase.claimedNation != null ? _currentCase.claimedNation.id : null,
            _currentCase.claimedEra != null ? _currentCase.claimedEra.id : null);
        if (proof == null)
            return;

        if (!_discrepancies.Add(proof))
        {
            if (compareController != null)
                compareController.ShowAlreadyDocumented(ClueLabels.Report(proof.category));
            return;
        }

        RefreshScannerText();

        if (compareController != null)
            compareController.ShowDeviation(proof.Summary);

        if (scannerWindow != null)
            scannerWindow.Open();
    }

    /// <summary>Injects the day's citizen registry into the Records app and the text fallback.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry)
    {
        _registry = registry;
        if (recordsWindow != null)
            recordsWindow.SetRegistry(registry);
    }

    /// <summary>Injects today's facts (the reference books render these rows).</summary>
    public void SetFacts(FactTable facts)
    {
        _facts = facts;
    }

    /// <summary>Injects today's interview (questions, dialogs and wording, fixed at day start).</summary>
    public void SetInterviewDay(InterviewDay day)
    {
        _day = day;
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
                "Compare a document field or a traveller's answer against the claimed place's reference entry, " +
                "the entry it really belongs to, or the Citizen Record to log evidence.";
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

    /// <summary>Hides the investigation overlay (between cases); the desktop shows its idle line.</summary>
    public void Hide()
    {
        if (root != null) root.SetActive(false);
        if (_fallbackPanel != null) _fallbackPanel.SetActive(false);
        if (idleScreen != null) idleScreen.SetActive(true);
    }

    // -----------------------------
    // Rich mode
    // -----------------------------

    private void ShowRich(CaseInstance inst, ContentLibrarySO lib)
    {
        if (root != null) root.SetActive(true);
        if (idleScreen != null) idleScreen.SetActive(false);

        if (claimText != null)
            claimText.text = inst != null ? $"{inst.visitorDisplayName}\n\"{inst.claimLine}\"" : string.Empty;

        if (directivesText != null)
            directivesText.text = _directives;

        // A new visitor clears the desk: every open window closes.
        // (A pin system will later let the player keep chosen windows open.)
        CloseAllWindows();

        foreach (GameObject w in _docWindows)
            if (w != null)
                Destroy(w);
        _docWindows.Clear();

        foreach (GameObject ic in _docIcons)
            if (ic != null)
                Destroy(ic);
        _docIcons.Clear();
        _iconedDocuments.Clear();
        _caseDocuments.Clear();

        // Documents are handed over, never taken: those marked "on arrival" when
        // the traveller steps up, the others through the traveller wheel. With
        // the desk, each becomes a paper whose scan opens its window; without
        // it, the window opens at the hand-over. Windows spawn hidden.
        if (inst != null)
        {
            int i = 0;
            foreach (DocumentInstance doc in inst.documents)
            {
                DocumentWindowController clone = Instantiate(documentWindowTemplate, windowLayer);
                clone.gameObject.SetActive(false);
                if (clone.transform is RectTransform rt)
                    rt.anchoredPosition = documentWindowOrigin + i * documentWindowStep;
                clone.SetDocument(doc, compareController);
                _docWindows.Add(clone.gameObject);
                _caseDocuments.Add(new CaseDocument
                {
                    name = doc != null && doc.template != null ? doc.template.displayName : "Document",
                    holder = inst.visitorGivenName,
                    handOver = doc != null && doc.template != null ? doc.template.handOver : DocumentHandOver.OnRequest
                });
                i++;
            }
        }

        if (DeskReachable)
        {
            desk.BeginCase(_caseDocuments);
        }
        else
        {
            foreach (int i in CaseDocuments.ArrivalIndices(_caseDocuments))
                OpenDocumentWindow(i);
        }

        StartInterview(inst, _caseDocuments);

        BuildBookShelf(lib);

        if (compareController != null)
            compareController.Clear();

        WireDecisionButtons(acceptButton, denyButton);
    }

    /// <summary>
    /// Starts the traveller's interview: the hub with a request per document
    /// handed over on request, and, when the interview is reachable, today's questions, small talk and
    /// offered dialogs (without a wired transcript nothing spoken could be read,
    /// so only the requests remain). The transcript starts with the opener and
    /// the claim.
    /// </summary>
    private void StartInterview(CaseInstance inst, IReadOnlyList<CaseDocument> documents)
    {
        _runner = null;
        if (_day == null)
        {
            Debug.LogError("[InvestigationUIController] No interview day was injected (GameManager.SetInterviewDay), so the traveller wheel is empty.", this);
            if (interactionPanel != null)
                interactionPanel.Clear();
            return;
        }

        bool reachable = InterviewReachable;
        var interviewCase = new InterviewCase
        {
            introLine = inst != null ? inst.introLine : null,
            claimLine = inst != null ? inst.claimLine : null,
            claimedEraId = inst != null && inst.claimedEra != null ? inst.claimedEra.id : null,
            documents = documents,
            answers = inst != null ? inst.answers : null,
            smallTalk = reachable && inst != null ? inst.smallTalk : null
        };

        DialogGraph graph = InterviewScript.Build(_day.Lines,
            reachable ? _day.Questions : Array.Empty<InterviewQuestion>(),
            reachable ? _day.OfferedDialogs(null) : Array.Empty<AuthoredDialog>(),
            interviewCase);
        _runner = new DialogRunner(graph, InterviewScript.Opening(interviewCase));

        if (transcriptWindow != null)
            transcriptWindow.Bind(_runner.Transcript, _day.Lines.deskName, inst != null ? inst.visitorGivenName : string.Empty, compareController);

        RefreshChoices();
    }

    /// <summary>Shows the current interview node's choices on the traveller wheel ("&lt; Back" in its centre).</summary>
    private void RefreshChoices()
    {
        if (interactionPanel == null || _runner == null)
            return;

        var actions = new List<InteractionAction>();
        foreach (DialogChoice choice in _runner.Choices)
        {
            string id = choice.Id;
            actions.Add(new InteractionAction { label = choice.Label, centre = choice.Kind == DialogChoiceKind.Back, execute = () => Choose(id) });
        }

        interactionPanel.SetActions(actions);
    }

    /// <summary>
    /// Plays one interview choice: the transcript shows its lines; a document
    /// request hands that document over (a paper onto the desk, or straight to
    /// its window where no desk is wired) and closes the wheel so the player can
    /// take it; any other choice opens the transcript; the traveller's reply, when
    /// the choice adds one, goes to the wheel's bubble (the spoken reveal point;
    /// a choice without one, such as "Ask about home >" or "&lt; Back", leaves the
    /// last reply up); a finished dialog is recorded for the end of the shift.
    /// </summary>
    private void Choose(string choiceId)
    {
        if (_runner == null)
            return;

        int before = _runner.Transcript.Count;
        DialogChoice choice = _runner.Choose(choiceId);
        if (choice == null)
            return;

        if (transcriptWindow != null)
            transcriptWindow.Refresh();

        if (choice.Action == DialogAction.HandOverDocument)
        {
            if (DeskReachable)
                desk.HandOver(choice.DocumentIndex);
            else
                OpenDocumentWindow(choice.DocumentIndex);

            if (wheel != null)
                wheel.Close();
        }
        else if (transcriptChrome != null)
        {
            transcriptChrome.Open();
        }

        string reply = InterviewScript.SpokenSince(_runner.Transcript, before);
        if (wheel != null && reply.Length > 0)
            wheel.Say(DisplayText.For(reply, TextMedium.Spoken));

        if (choice.Action == DialogAction.CompleteDialog)
            _day.Complete(choice.DialogId, choice.EffectName);

        RefreshChoices();
    }

    /// <summary>
    /// Opens a paper's scanned window and raises it (the desk's ScanFinished,
    /// or a hand-over where no desk is wired: the written reveal point). The
    /// first time it opens this case, the paper also gets a desktop icon at
    /// the top of the grid, which reopens the window after it is closed.
    /// </summary>
    private void OpenDocumentWindow(int index)
    {
        GameObject window = index >= 0 && index < _docWindows.Count ? _docWindows[index] : null;
        if (window == null)
            return;

        window.SetActive(true);
        window.transform.SetAsLastSibling();
        if (_iconedDocuments.Add(index))
            AddDesktopIcon(_caseDocuments[index].name, window, true);
    }

    /// <summary>
    /// Closes every window on the window layer (templates are already
    /// inactive; per-case document clones are destroyed separately).
    /// </summary>
    private void CloseAllWindows()
    {
        if (windowLayer == null)
            return;

        for (int i = 0; i < windowLayer.childCount; i++)
        {
            GameObject child = windowLayer.GetChild(i).gameObject;
            if (child.activeSelf)
                child.SetActive(false);
        }
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
            win.SetBook(book, _facts, compareController);
            if (win.transform is RectTransform rt)
                rt.anchoredPosition = bookWindowOrigin + new Vector2((i % 3) * bookWindowColumnStep, 0f) + (i / 3) * bookWindowRowStep;
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
        if (DeskReachable)
            desk.EndCase();
        Hide();

        // No case is on the desk from here: cleared before the callback, which
        // may present the next traveller at once (no READY sign wired).
        _currentCase = null;
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
            _fallbackBody.text = BuildFallbackBody(inst, lib, _facts, _registry, _day);
    }

    /// <summary>
    /// The text fallback's body: the papers, the traveller's agency record (so
    /// a birth-date tell can be spotted without the Records app), their answers
    /// to today's questions, and the claimed place's entry in each book.
    /// </summary>
    private static string BuildFallbackBody(CaseInstance inst, ContentLibrarySO lib, FactTable facts, CitizenRegistry registry, InterviewDay day)
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

            sb.AppendLine("— AGENCY RECORD —");
            CitizenRecord record = registry != null ? registry.Find(inst.visitorGivenName) : null;
            if (record == null)
            {
                sb.AppendLine("    No record on file.");
            }
            else
            {
                sb.AppendLine($"    Name: {record.fullName}");
                sb.AppendLine($"    Born: {record.birthDate}");
                sb.AppendLine($"    Origin: {record.origin}");
            }
            sb.AppendLine();

            if (day != null)
            {
                sb.AppendLine("— INTERVIEW —");
                string eraId = inst.claimedEra != null ? inst.claimedEra.id : null;
                foreach (InterviewQuestion q in day.Questions)
                {
                    InterviewAnswer answer = inst.answers.Find(a => a.category == q.category);
                    if (answer == null)
                        continue;
                    sb.AppendLine(InterviewScript.PromptLine(q, eraId).Text);
                    sb.AppendLine($"    {inst.visitorGivenName}: {InterviewScript.AnswerLine(q, eraId, answer).Text}");
                }
                sb.AppendLine();
            }
        }

        if (lib != null && lib.ReferenceBooks.Count > 0)
        {
            sb.AppendLine("— REFERENCE (claimed place) —");
            string nationId = inst != null && inst.claimedNation != null ? inst.claimedNation.id : null;
            string eraId = inst != null && inst.claimedEra != null ? inst.claimedEra.id : null;
            foreach (ReferenceBookSO book in lib.ReferenceBooks)
                if (book != null)
                    sb.AppendLine($"{book.displayName}: {(facts != null ? facts.Get(nationId, eraId, book.category) : null) ?? "(no entry)"}");
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
