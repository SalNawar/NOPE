using System;
using System.Collections.Generic;
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
/// EvidenceSystemActive). It needs the desk the office builder wires (Tools
/// &gt; TimeDesk &gt; Build Office UI: the document window template, the
/// window layer, Accept and Deny); without it, it logs one error and shows no
/// case (the text-mode fallback no scene could reach was deleted: audit R4-002).
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

    [Header("Desk windows (built by the office tool)")]
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
    /// True when the evidence loop is playable (the desk and the compare wired),
    /// so scoring may gate denials on documented evidence.
    /// </summary>
    public bool EvidenceSystemActive => Wiring.EvidenceSystemActive;

    /// <summary>
    /// True when a traveller's answers can be read: the wheel's ring, the
    /// transcript window and its chrome are wired. When false, GameManager
    /// computes no answers and generates no spoken tell that day.
    /// </summary>
    public bool InterviewReachable => Wiring.InterviewReachable;

    /// <summary>
    /// True when a traveller's garments can be looked at and compared: the
    /// wheel's ring (its "Look >" menu) and the compare bar are wired. When
    /// false, GameManager generates no dress tell that day.
    /// </summary>
    public bool AppearanceReachable => Wiring.AppearanceReachable;

    /// <summary>
    /// What the wired references make reachable (InvestigationWiring).
    /// Serialized references are compared with != null: an unassigned one is
    /// Unity's fake null.
    /// </summary>
    private InvestigationWiring Wiring => new InvestigationWiring(
        documentWindowTemplate != null, windowLayer != null, acceptButton != null, denyButton != null, compareController != null,
        interactionPanel != null, transcriptWindow != null, transcriptChrome != null, desk != null && desk.IsReachable, recordsWindow != null);

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
        if (wiring.Wired)
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

    /// <summary>The start-up error and warnings for what is not wired (each changes what the day can show or generate).</summary>
    private void WarnAboutWiring(InvestigationWiring wiring)
    {
        // Without the desk windows no case can be shown (there is no text fallback any more).
        if (!wiring.Wired)
            Debug.LogError("[InvestigationUIController] The desk windows are not wired (documentWindowTemplate, windowLayer, acceptButton or denyButton): no case can be shown. Run Tools > TimeDesk > Build Office UI.", this);

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

    /// <summary>Injects the day's citizen registry into the Records app, with the agency block and today's date (<paramref name="day"/> in the agency's calendar) its extract prints.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry, AgencyContent agency, int day) => _reference.SetCitizenRegistry(registry, agency, day);

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

    /// <summary>Presents a case and waits for the player's Accept/Deny (nothing shows when the desk windows are not wired: Awake logged why).</summary>
    public void ShowCase(CaseInstance inst, ContentLibrarySO lib, Action<bool> onDecision)
    {
        _onDecision = onDecision;
        _currentCase = inst;
        _evidence.BeginCase();

        if (Wiring.Wired)
            ShowRich(inst, lib);
    }

    /// <summary>Hides the investigation overlay (between cases) and closes its windows (so the taskbar keeps no button for them); the desktop shows its idle line and the office's claim tag empties.</summary>
    public void Hide()
    {
        CloseAllWindows();
        if (root != null) root.SetActive(false);
        if (hud != null) hud.SetClaim(string.Empty);
        if (idleScreen != null) idleScreen.SetActive(true);
    }

    /// <summary>
    /// A case on the desk: the claim on the PC's banner and the office's tag
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

    private void Accept() => Decide(true);

    private void Deny() => Decide(false);

    /// <summary>
    /// The decision (the PC's buttons or the stamp tray): the
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
}
