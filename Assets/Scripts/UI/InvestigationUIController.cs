using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office investigation's façade (the PC redesign RF1, audit R4-001): the
/// one component GameManager talks to, with the scene's references. It shows
/// the visitor's travel claim, presents each case and offers the binary
/// Accept/Deny (the PC's buttons and the desk's stamp tray, wired once); the
/// work is its presenters': CaseDocumentsPresenter (the documents' windows,
/// the hand-over and the scan), InterviewPresenter (the dialog runner on the
/// traveller wheel, the transcript, the bubble), EvidencePresenter (the
/// discrepancy log, the Deviation Report, the compare's DEVIATION LOGGED) and
/// DayReference (the directives, facts, registry and the reference books).
/// Every document is filled in English (the redesign's F5); from
/// translation's first day a traveller's speech is in their claimed place's
/// tongue (piece 9). A held paper's row picked at the desk goes into the same
/// compare as the PC's rows. What the day can generate follows what is wired
/// (InvestigationWiring: InterviewReachable, AppearanceReachable,
/// EvidenceSystemActive).
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
    [SerializeField] private DesktopWindow scannerWindow;

    [Header("Interaction / records")]
    /// <summary>The traveller wheel's ring: shows the current interview node's choices (requests, questions, dialog replies).</summary>
    [SerializeField] private InteractionPanelController interactionPanel;

    /// <summary>Citizen Records app (registry injected per day).</summary>
    [SerializeField] private CitizenRecordsWindowController recordsWindow;

    [Header("Interview")]
    /// <summary>Case Notes: Interview, the current traveller's transcript (answer rows are compare-clickable).</summary>
    [SerializeField] private TranscriptWindowController transcriptWindow;

    /// <summary>The transcript window's chrome; every interview choice but a document request opens it.</summary>
    [SerializeField] private DesktopWindow transcriptChrome;

    [Header("Desk")]
    /// <summary>The physical papers and the scanner (optional: without it documents open on request, straight to their windows).</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The office case HUD (piece 10; optional): the claim tag shows the claim banner's text in the office.</summary>
    [SerializeField] private OfficeCaseHud hud;

    /// <summary>The stamp tray (piece 10; optional): its Accept and Deny decide the case like the PC's buttons.</summary>
    [SerializeField] private StampTray stampTray;

    /// <summary>The traveller wheel: closed after a hand-over; it gives the ring its icons and says the traveller's lines (the claim on arrival, then each reply).</summary>
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

    /// <summary>The decision's callback for the case on the desk (fired once: OneShot).</summary>
    private Action<bool> _onDecision;

    /// <summary>The case currently on the desk (null between cases).</summary>
    private CaseInstance _currentCase;

    /// <summary>The day's directives, facts, registry and the reference books.</summary>
    private DayReference _reference;

    /// <summary>The current traveller's documents: their windows, the hand-over and the scan.</summary>
    private CaseDocumentsPresenter _documents;

    /// <summary>The current traveller's interview on the wheel and in the transcript.</summary>
    private InterviewPresenter _interview;

    /// <summary>The current case's evidence and the Deviation Report.</summary>
    private EvidencePresenter _evidence;

    /// <summary>The stamp tray whose decisions this listens to (null while detached; audit R4-003).</summary>
    private StampTray _stampTrayListening;

    /// <summary>Number of discrepancies documented for the current case.</summary>
    public int EvidenceCount => _evidence.Count;

    /// <summary>
    /// True when the evidence loop is playable (rich desk + compare wired), so
    /// scoring may gate denials on documented evidence.
    /// </summary>
    public bool EvidenceSystemActive => Wiring.EvidenceSystemActive;

    /// <summary>
    /// True when a traveller's answers can be read: always in the text
    /// fallback; in the rich desk only when the wheel's ring, the transcript
    /// window and its chrome are wired. When false, GameManager computes no answers
    /// and generates no spoken tell that day.
    /// </summary>
    public bool InterviewReachable => Wiring.InterviewReachable;

    /// <summary>
    /// True when a traveller's garments can be looked at and compared: always
    /// in the text fallback (which prints the dress); in the rich desk only
    /// when the wheel's ring (its "Look >" menu) and the compare bar are wired.
    /// When false, GameManager generates no dress tell that day.
    /// </summary>
    public bool AppearanceReachable => Wiring.AppearanceReachable;

    // Fallback state
    private bool _fallbackBuilt;
    private GameObject _fallbackPanel;
    private TMP_Text _fallbackClaim;
    private TMP_Text _fallbackBody;

    /// <summary>
    /// What the wired references make reachable (InvestigationWiring).
    /// Serialized references are compared with != null: an unassigned one is
    /// Unity's fake null.
    /// </summary>
    private InvestigationWiring Wiring => new InvestigationWiring(
        documentWindowTemplate != null, windowLayer != null, acceptButton != null, denyButton != null, compareController != null,
        interactionPanel != null, transcriptWindow != null, transcriptChrome != null, desk != null && desk.IsReachable, recordsWindow != null);

    private bool RichMode => Wiring.RichMode;

    private void Awake()
    {
        InvestigationWiring wiring = Wiring;
        BuildPresenters(wiring);

        // A leftover copy in the art office (from before the gameplay moved into its own scene) does nothing.
        if (OfficeScenes.IsArtOffice(gameObject.scene))
        {
            enabled = false;
            return;
        }

        if (documentWindowTemplate != null) documentWindowTemplate.gameObject.SetActive(false);
        if (bookWindowTemplate != null) bookWindowTemplate.gameObject.SetActive(false);
        if (bookShelfButtonTemplate != null) bookShelfButtonTemplate.gameObject.SetActive(false);
        if (root != null) root.SetActive(false);

        _evidence.Attach();
        WarnAboutWiring(wiring);
        _documents.Attach();
        _interview.Attach();

        if (stampTray != null)
        {
            _stampTrayListening = stampTray;
            _stampTrayListening.Decided += Decide;
        }

        // The PC's Accept and Deny are wired once (audit R4-004); the decision reads the case's callback.
        if (wiring.RichMode)
        {
            acceptButton.onClick.AddListener(Accept);
            denyButton.onClick.AddListener(Deny);
        }

        if (idleScreen != null)
            idleScreen.SetActive(true);
    }

    /// <summary>The presenters over this component's references (the desk only when it is reachable).</summary>
    private void BuildPresenters(InvestigationWiring wiring)
    {
        var tiles = new DesktopTiles(bookShelfButtonTemplate, bookShelfRoot);
        _reference = new DayReference(directivesText, recordsWindow, compareController, tiles, new DayReference.BookShelf
        {
            template = bookWindowTemplate, windowLayer = windowLayer,
            origin = bookWindowOrigin, columnStep = bookWindowColumnStep, rowStep = bookWindowRowStep
        });
        _documents = new CaseDocumentsPresenter(new CaseDocumentsPresenter.Windows
        {
            template = documentWindowTemplate, windowLayer = windowLayer, origin = documentWindowOrigin, step = documentWindowStep
        }, wiring.DeskReachable ? desk : null, compareController, tiles);
        _interview = new InterviewPresenter(interactionPanel, transcriptWindow, transcriptChrome, wheel, compareController,
                                            _documents.HandOver, () => _currentCase, this);
        _evidence = new EvidencePresenter(compareController, scannerText, scannerWindow, () => _currentCase);
    }

    /// <summary>The rich desk's start-up warnings for what is not wired (each changes what the day can generate).</summary>
    private void WarnAboutWiring(InvestigationWiring wiring)
    {
        // Birth-date tells are proven only against Citizen Records (RecordMismatch).
        if (wiring.RecordsMissing)
            Debug.LogWarning("[InvestigationUIController] Citizen Records not wired: birth-date tells cannot be proven. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the transcript nothing a traveller says could be read, so the day speaks no tell.
        if (wiring.InterviewMissing)
            Debug.LogWarning("[InvestigationUIController] Traveller wheel or interview transcript not wired: questions are hidden and no tell is spoken today. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the wheel's look menu or the compare bar no garment could be compared, so the day leaks no dress.
        if (wiring.AppearanceMissing)
            Debug.LogWarning("[InvestigationUIController] Traveller wheel or compare bar not wired (interactionPanel or compareController): garments cannot be looked at and no dress tell is generated today. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the desk every document still reaches the PC, as its window.
        if (wiring.DeskMissing)
            Debug.LogWarning("[InvestigationUIController] Desk scanner not wired: documents open on the PC when handed over (no physical papers). Run Tools > TimeDesk > Build Office UI.", this);
    }

    /// <summary>Unsubscribes from the very instances subscribed to (audit R4-003).</summary>
    private void OnDestroy()
    {
        if (_evidence == null)
            return;

        _evidence.Detach();
        _documents.Detach();
        _interview.Detach();
        if (_stampTrayListening != null)
        {
            _stampTrayListening.Decided -= Decide;
            _stampTrayListening = null;
        }
    }

    /// <summary>Injects the day's citizen registry into the Records app and the text fallback.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry) => _reference.SetCitizenRegistry(registry);

    /// <summary>Injects today's facts (the reference books render these rows).</summary>
    public void SetFacts(FactTable facts) => _reference.SetFacts(facts);

    /// <summary>Injects today's interview (questions, dialogs and wording, fixed at day start).</summary>
    public void SetInterviewDay(InterviewDay day) => _interview.SetInterviewDay(day);

    /// <summary>Injects the character art the passport photos are drawn with.</summary>
    public void SetCharacterArt(CharacterArt art) => _documents.SetCharacterArt(art);

    /// <summary>Injects the day-start translation (which tongues are foreign and translated today) and the library's translation settings (their key-word rule included).</summary>
    public void SetTranslation(TranslationDay day, TranslationSettings settings) => _interview.SetTranslation(day, settings);

    /// <summary>Sets the day's travel directives (shown during every case).</summary>
    public void SetDirectives(IReadOnlyList<TravelRuleSO> rules) => _reference.SetDirectives(rules);

    /// <summary>Presents a case and waits for the player's Accept/Deny.</summary>
    public void ShowCase(CaseInstance inst, ContentLibrarySO lib, Action<bool> onDecision)
    {
        _onDecision = onDecision;
        _currentCase = inst;
        _evidence.BeginCase();

        if (RichMode)
            ShowRich(inst, lib);
        else
            ShowFallback(inst, lib);
    }

    /// <summary>Hides the investigation overlay (between cases) and closes its windows (so the taskbar keeps no button for them); the desktop shows its idle line and the office's claim tag empties.</summary>
    public void Hide()
    {
        CloseAllWindows();
        if (root != null) root.SetActive(false);
        if (hud != null) hud.SetClaim(string.Empty);
        if (_fallbackPanel != null) _fallbackPanel.SetActive(false);
        if (idleScreen != null) idleScreen.SetActive(true);
    }

    // -----------------------------
    // Rich mode
    // -----------------------------

    /// <summary>
    /// The rich desk's case: the claim on the PC's banner and the office's tag
    /// (one text), today's directives, every window closed (a new visitor
    /// clears the desk; a pin system will later let the player keep chosen
    /// windows open), the documents presented, the interview started, the book
    /// shelf built the first time, the compare cleared.
    /// </summary>
    private void ShowRich(CaseInstance inst, ContentLibrarySO lib)
    {
        if (root != null) root.SetActive(true);
        if (idleScreen != null) idleScreen.SetActive(false);

        string claim = inst != null ? UiText.Format("claim.banner", inst.visitorDisplayName, inst.claimLine) : string.Empty;
        if (claimText != null)
            claimText.text = claim;
        if (hud != null)
            hud.SetClaim(claim);

        _reference.ShowDirectives();
        CloseAllWindows();
        _documents.Clear();
        _interview.BeginCase(inst);
        _documents.Present(inst);
        _interview.Start(inst, _documents.Documents, InterviewReachable, AppearanceReachable);
        _reference.BuildBookShelf(lib);

        if (compareController != null)
            compareController.Clear();
    }

    /// <summary>
    /// Closes every window on the window layer through the desktop's window
    /// manager, minimised ones too (templates are never opened; per-case
    /// document clones are destroyed separately).
    /// </summary>
    private void CloseAllWindows()
    {
        if (windowLayer == null)
            return;

        for (int i = 0; i < windowLayer.childCount; i++)
            if (windowLayer.GetChild(i).TryGetComponent(out DesktopWindow window))
                window.Close();
    }

    /// <summary>The fallback panel's Accept and Deny (created once).</summary>
    private void WireDecisionButtons(Button accept, Button deny)
    {
        if (accept != null)
        {
            accept.onClick.RemoveAllListeners();
            accept.onClick.AddListener(Accept);
        }

        if (deny != null)
        {
            deny.onClick.RemoveAllListeners();
            deny.onClick.AddListener(Deny);
        }
    }

    private void Accept() => Decide(true);

    private void Deny() => Decide(false);

    /// <summary>
    /// The decision (the PC's buttons, the stamp tray, the fallback's): the
    /// desk's papers leave, the overlay hides, and no case is on the desk from
    /// here: cleared before the callback, which may present the next traveller
    /// at once (no READY sign wired).
    /// </summary>
    private void Decide(bool accepted)
    {
        _documents.EndCase();
        Hide();
        _currentCase = null;
        OneShot.Fire(ref _onDecision, accepted);
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
                ? UiText.Format("fallback.claim", inst.visitorDisplayName, inst.claimLine, _reference.Directives)
                : string.Empty;

        if (_fallbackBody != null)
            _fallbackBody.text = BuildFallbackBody(inst, lib, _reference.Facts, _reference.Registry, _interview.Day);
    }

    /// <summary>
    /// The text fallback's body: the papers, the traveller's agency record (so
    /// a birth-date tell can be spotted without the Records app), the
    /// traveller's dress (each garment with its place's Culture value), their
    /// answers to today's questions, and the claimed place's entry in each book.
    /// </summary>
    private static string BuildFallbackBody(CaseInstance inst, ContentLibrarySO lib, FactTable facts, CitizenRegistry registry, InterviewDay day)
    {
        var sb = new StringBuilder();

        if (inst != null)
        {
            sb.AppendLine(UiText.Get("fallback.documents"));
            foreach (DocumentInstance doc in inst.documents)
            {
                sb.AppendLine(UiText.Format("fallback.document", doc.template != null ? doc.template.displayName : UiText.Get("document.untitled")));
                foreach (DocumentField f in doc.fields)
                    sb.AppendLine(UiText.Format("fallback.field", f.label, f.value));
            }
            sb.AppendLine();

            sb.AppendLine(UiText.Get("fallback.record"));
            CitizenRecord record = registry != null ? registry.Find(inst.visitorGivenName) : null;
            if (record == null)
            {
                sb.AppendLine(UiText.Get("fallback.noRecord"));
            }
            else
            {
                sb.AppendLine(UiText.Format("fallback.recordName", record.fullName));
                sb.AppendLine(UiText.Format("fallback.recordBorn", record.birthDate));
                sb.AppendLine(UiText.Format("fallback.recordOrigin", record.origin));
            }
            sb.AppendLine();

            if (inst.look != null && inst.look.Garments.Count > 0)
            {
                sb.AppendLine(UiText.Get("fallback.dress"));
                foreach (Garment g in inst.look.Garments)
                    sb.AppendLine(UiText.Format("fallback.garment", UiText.Slot(g.Slot), g.Label, g.Value));
                sb.AppendLine();
            }

            if (day != null)
            {
                sb.AppendLine(UiText.Get("fallback.interview"));
                string eraId = inst.claimedEra != null ? inst.claimedEra.id : null;
                foreach (InterviewQuestion q in day.Questions)
                {
                    InterviewAnswer answer = inst.answers.Find(a => a.category == q.category);
                    if (answer == null)
                        continue;
                    sb.AppendLine(InterviewScript.PromptLine(q, eraId).Text);
                    sb.AppendLine(UiText.Format("fallback.answer", inst.visitorGivenName, InterviewScript.AnswerLine(q, eraId, answer).Text));
                }
                sb.AppendLine();
            }
        }

        if (lib != null && lib.ReferenceBooks.Count > 0)
        {
            sb.AppendLine(UiText.Get("fallback.reference"));
            string nationId = inst != null && inst.claimedNation != null ? inst.claimedNation.id : null;
            string eraId = inst != null && inst.claimedEra != null ? inst.claimedEra.id : null;
            foreach (ReferenceBookSO book in lib.ReferenceBooks)
                if (book != null)
                    sb.AppendLine(UiText.Format("fallback.bookEntry", book.displayName, (facts != null ? facts.Get(nationId, eraId, book.category) : null) ?? UiText.Get("fallback.noEntry")));
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
        Tag(_fallbackPanel, ThemeRoleId.Panel, ThemeTextKind.Body, null);

        _fallbackClaim = NewText(_fallbackPanel.transform, "Claim", 24, TextAlignmentOptions.TopLeft,
            new Vector2(0.04f, 0.78f), new Vector2(0.96f, 0.97f));
        Tag(_fallbackClaim.gameObject, ThemeRoleId.Panel, ThemeTextKind.Body, null);
        _fallbackBody = NewText(_fallbackPanel.transform, "Body", 20, TextAlignmentOptions.TopLeft,
            new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.76f));
        Tag(_fallbackBody.gameObject, ThemeRoleId.Panel, ThemeTextKind.Body, null);

        Button accept = NewButton(_fallbackPanel.transform, "AcceptButton", "fallback.accept", ThemeRoleId.AcceptButton,
            new Vector2(0.06f, 0.04f), new Vector2(0.48f, 0.13f), new Color(0.15f, 0.4f, 0.2f, 1f));
        Button deny = NewButton(_fallbackPanel.transform, "DenyButton", "fallback.deny", ThemeRoleId.DenyButton,
            new Vector2(0.52f, 0.04f), new Vector2(0.94f, 0.13f), new Color(0.45f, 0.16f, 0.16f, 1f));

        WireDecisionButtons(accept, deny);

        // The panel is built after the scene loaded: theme it now.
        if (CultureThemeService.Instance != null)
            CultureThemeService.Instance.ApplyTo(_fallbackPanel);
    }

    /// <summary>Tags a fallback graphic with its theme role (a label key only for keyed button labels).</summary>
    private static void Tag(GameObject go, ThemeRoleId role, ThemeTextKind kind, string labelKey)
    {
        ThemeTag tag = go.AddComponent<ThemeTag>();
        bool isText = go.TryGetComponent(out TMP_Text _);
        tag.Configure(role, isText ? ThemePart.Ink : ThemePart.Fill, labelKey, FontStyles.Normal, kind, !string.IsNullOrEmpty(labelKey));
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

    private static Button NewButton(Transform parent, string name, string labelKey, ThemeRoleId role, Vector2 min, Vector2 max, Color color)
    {
        GameObject go = NewUI(name, parent);
        Stretch((RectTransform)go.transform, min, max);
        Image img = go.AddComponent<Image>();
        img.color = color;
        Tag(go, role, ThemeTextKind.Button, null);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        TMP_Text t = NewText(go.transform, "Label", 22, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        t.text = UiText.Get(labelKey);
        Tag(t.gameObject, role, ThemeTextKind.Button, labelKey);
        return btn;
    }
}
