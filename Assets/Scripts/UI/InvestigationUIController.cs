using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office investigation's façade (the PC redesign RF1, audit R4-001): the
/// one component GameManager talks to, with the scene's references. It
/// presents each case in the Investigation app (InvestigationApp: the claim
/// in its case header, the counters, the six tabs) and offers the binary
/// Accept/Deny (the app header's buttons and the desk's stamp tray, wired
/// once); the work is its presenters': CaseDocumentsPresenter (the papers,
/// the hand-over and the scan: the Documents tab), InterviewPresenter (the
/// dialog runner on the traveller wheel, the Transcript tab, the bubble),
/// EvidencePresenter (the discrepancy log, the Report tab, the compare's
/// DEVIATION LOGGED) and DayReference (the Rules, the Reference books' facts,
/// the Records tab's registry); it feeds the sidebar's steps checklist
/// (StepsPanel) the case and its events (papers handed over, asked for and
/// read at the desk, pairs compared, answers heard, garments looked at).
/// Nothing opens or closes a window by itself
/// but a scan (the app's ScanArrival): new lines and deviations badge their
/// tabs, and the decision leaves every window as it is (the case's tabs show
/// the no-case state). Every document is filled in English (the redesign's
/// F5); from translation's first day a traveller's speech is in their claimed
/// place's tongue (piece 9). A held paper's row picked at the desk goes into
/// the same compare as the PC's rows. What the day can generate follows what
/// is wired (InvestigationWiring: InterviewReachable, AppearanceReachable,
/// EvidenceSystemActive). It needs the app the office builder wires (Tools
/// &gt; TimeDesk &gt; Build Office UI: the Documents tab's page, the app,
/// Accept and Deny); without it, it logs one error and shows no case (the
/// text-mode fallback no scene could reach was deleted: audit R4-002).
/// </summary>
public sealed class InvestigationUIController : MonoBehaviour
{
    [Header("The app")]
    /// <summary>The Investigation app: its window, header, counters, badges, toast and pane.</summary>
    [SerializeField] private InvestigationApp app;

    /// <summary>The app header's Accept (and the stamp tray's) decide the case.</summary>
    [SerializeField] private Button acceptButton;

    /// <summary>The app header's Deny.</summary>
    [SerializeField] private Button denyButton;

    /// <summary>The one compare: every pickable row on the PC, on a held paper, in the bubble and through the Look menu.</summary>
    [SerializeField] private CompareController compareController;

    /// <summary>The PC's compare dock (DK9), above the window layer so no window covers it; shown while a traveller is at the desk.</summary>
    [SerializeField] private GameObject compareDock;

    /// <summary>The sidebar's steps checklist (optional: without it no steps show).</summary>
    [SerializeField] private StepsPanel stepsPanel;

    [Header("The app's tabs")]
    /// <summary>The Documents tab: a chip per paper, the scanned copies.</summary>
    [SerializeField] private DocumentsView documentsView;

    /// <summary>The Records tab's lookup (Citizen Records; the registry injected per day).</summary>
    [SerializeField] private CitizenRecordsWindowController recordsWindow;

    /// <summary>The Reference tab: a chip per book, the registers.</summary>
    [SerializeField] private ReferenceView referenceView;

    /// <summary>The Transcript tab's transcript (answer rows are compare-clickable).</summary>
    [SerializeField] private TranscriptWindowController transcriptWindow;

    /// <summary>The Report tab's text: the documented deviations.</summary>
    [SerializeField] private TMP_Text reportText;

    /// <summary>The Rules tab's text: the day's travel directives.</summary>
    [SerializeField] private TMP_Text directivesText;

    [Header("Office")]
    /// <summary>The traveller wheel's ring: shows the current interview node's choices (requests, questions, dialog replies).</summary>
    [SerializeField] private InteractionPanelController interactionPanel;

    /// <summary>The physical papers and the scanner (optional: without it documents reach the PC when handed over).</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The office case HUD (piece 10; optional): the claim tag shows the claim banner's text in the office.</summary>
    [SerializeField] private OfficeCaseHud hud;

    /// <summary>The stamp tray (piece 10; optional): its Accept and Deny decide the case like the PC's buttons.</summary>
    [SerializeField] private StampTray stampTray;

    /// <summary>The traveller wheel: closed after a hand-over; it gives the ring its icons and says the traveller's lines (the claim on arrival, then each reply).</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>Shown on the desktop between travellers.</summary>
    [SerializeField] private GameObject idleScreen;

    /// <summary>The decision's callback for the case on the desk (fired once: OneShot).</summary>
    private Action<bool> _onDecision;

    /// <summary>The case currently on the desk (null between cases).</summary>
    private CaseInstance _currentCase;

    /// <summary>The day's directives, facts, registry and the reference books.</summary>
    private DayReference _reference;

    /// <summary>The current traveller's papers: the Documents tab, the hand-over and the scan.</summary>
    private CaseDocumentsPresenter _documents;

    /// <summary>The current traveller's interview on the wheel and in the Transcript tab.</summary>
    private InterviewPresenter _interview;

    /// <summary>The current case's evidence and the Report tab.</summary>
    private EvidencePresenter _evidence;

    /// <summary>The stamp tray whose decisions this listens to (null while detached; audit R4-003).</summary>
    private StampTray _stampTrayListening;

    /// <summary>Number of discrepancies documented for the current case.</summary>
    public int EvidenceCount => _evidence.Count;

    /// <summary>
    /// True when the evidence loop is playable (the app and the compare wired),
    /// so scoring may gate denials on documented evidence.
    /// </summary>
    public bool EvidenceSystemActive => Wiring.EvidenceSystemActive;

    /// <summary>
    /// True when a traveller's answers can be read: the wheel's ring, the
    /// transcript and the app's Transcript tab are wired. When false,
    /// GameManager computes no answers and generates no spoken tell that day.
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
        documentsView != null && documentsView.Ready, app != null, acceptButton != null, denyButton != null, compareController != null,
        interactionPanel != null, transcriptWindow != null, app != null && app.Hosts(AppTab.Transcript), desk != null && desk.IsReachable,
        recordsWindow != null);

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

        _evidence.Attach();
        WarnAboutWiring(wiring);
        _documents.Attach();
        _interview.Attach();
        _documents.Scanned += HandleScanned;
        _documents.PapersChanged += ShowCounters;
        _documents.PapersChanged += StepsReceived;
        _documents.Examined += StepsRead;
        _interview.Answered += StepsAsked;
        _interview.LookedAt += StepsLookedAt;
        if (compareController != null)
            compareController.PairCompared += StepsCompared;

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

        ShowCaseLayers(false);
        if (app != null)
            app.EndCase();
        if (stepsPanel != null)
            stepsPanel.EndCase();
    }

    /// <summary>The presenters over this component's references (the desk only when it is reachable).</summary>
    private void BuildPresenters(InvestigationWiring wiring)
    {
        _reference = new DayReference(directivesText, recordsWindow, compareController, referenceView);
        _documents = new CaseDocumentsPresenter(documentsView, wiring.DeskReachable ? desk : null, compareController);
        _interview = new InterviewPresenter(interactionPanel, transcriptWindow, () => Arrived(AppTab.Transcript), wheel, compareController,
                                            RequestPaper, () => _currentCase, this);
        _evidence = new EvidencePresenter(compareController, reportText, () =>
        {
            Arrived(AppTab.Report);
            ShowCounters();
        }, () => _currentCase);
    }

    /// <summary>The start-up error and warnings for what is not wired (each changes what the day can show or generate).</summary>
    private void WarnAboutWiring(InvestigationWiring wiring)
    {
        // Without the app no case can be shown (there is no text fallback any more).
        if (!wiring.Wired)
            Debug.LogError("[InvestigationUIController] The Investigation app is not wired (the app, its Documents tab's page, acceptButton or denyButton): no case can be shown. Run Tools > TimeDesk > Build Office UI.", this);

        // Birth-date tells are proven only against Citizen Records (RecordMismatch).
        if (wiring.RecordsMissing)
            Debug.LogWarning("[InvestigationUIController] Citizen Records not wired: birth-date tells cannot be proven. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the transcript nothing a traveller says could be read, so the day speaks no tell.
        if (wiring.InterviewMissing)
            Debug.LogWarning("[InvestigationUIController] Traveller wheel or the app's Transcript tab not wired: questions are hidden and no tell is spoken today. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the wheel's look menu or the compare bar no garment could be compared, so the day leaks no dress.
        if (wiring.AppearanceMissing)
            Debug.LogWarning("[InvestigationUIController] Traveller wheel or compare bar not wired (interactionPanel or compareController): garments cannot be looked at and no dress tell is generated today. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the desk every document still reaches the PC, at its hand-over.
        if (wiring.DeskMissing)
            Debug.LogWarning("[InvestigationUIController] Desk scanner not wired: documents reach the PC when handed over (no physical papers). Run Tools > TimeDesk > Build Office UI.", this);
    }

    /// <summary>Unsubscribes from the very instances subscribed to (audit R4-003).</summary>
    private void OnDestroy()
    {
        if (_evidence == null)
            return;

        _evidence.Detach();
        _documents.Detach();
        _interview.Detach();
        _documents.Scanned -= HandleScanned;
        _documents.PapersChanged -= ShowCounters;
        _documents.PapersChanged -= StepsReceived;
        _documents.Examined -= StepsRead;
        _interview.Answered -= StepsAsked;
        _interview.LookedAt -= StepsLookedAt;
        if (compareController != null)
            compareController.PairCompared -= StepsCompared;
        if (_stampTrayListening != null)
        {
            _stampTrayListening.Decided -= Decide;
            _stampTrayListening = null;
        }
    }

    /// <summary>Injects the day's citizen registry into the Records tab, with the agency block and today's date (<paramref name="day"/> in the agency's calendar) its extract prints.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry, AgencyContent agency, int day) => _reference.SetCitizenRegistry(registry, agency, day);

    /// <summary>Injects today's facts (the Reference tab's registers render these rows).</summary>
    public void SetFacts(FactTable facts) => _reference.SetFacts(facts);

    /// <summary>Injects today's interview (questions, dialogs and wording, fixed at day start).</summary>
    public void SetInterviewDay(InterviewDay day) => _interview.SetInterviewDay(day);

    /// <summary>Injects the character art the passport photos are drawn with.</summary>
    public void SetCharacterArt(CharacterArt art) => _documents.SetCharacterArt(art);

    /// <summary>Injects the day-start translation (which tongues are foreign and translated today) and the library's translation settings (their key-word rule included).</summary>
    public void SetTranslation(TranslationDay day, TranslationSettings settings) => _interview.SetTranslation(day, settings);

    /// <summary>Sets the day's travel directives (the Rules tab).</summary>
    public void SetDirectives(IReadOnlyList<TravelRuleSO> rules) => _reference.SetDirectives(rules);

    /// <summary>Presents a case and waits for the player's Accept/Deny (nothing shows when the app is not wired: Awake logged why).</summary>
    public void ShowCase(CaseInstance inst, ContentLibrarySO lib, Action<bool> onDecision)
    {
        _onDecision = onDecision;
        _currentCase = inst;
        _evidence.BeginCase();

        if (Wiring.Wired)
            ShowRich(inst, lib);
    }

    /// <summary>Between cases: the compare dock hides, the desktop shows its idle line, the app's case tabs show the no-case state, the steps go and the office's claim tag empties. No window closes.</summary>
    public void Hide()
    {
        ShowCaseLayers(false);
        if (app != null)
            app.EndCase();
        if (stepsPanel != null)
            stepsPanel.EndCase();
        if (hud != null)
            hud.SetClaim(string.Empty);
    }

    /// <summary>Shows the compare dock and Accept and Deny (a traveller is at the desk), or hides the dock, turns the buttons off and shows the desktop's idle line.</summary>
    private void ShowCaseLayers(bool on)
    {
        if (compareDock != null) compareDock.SetActive(on);
        if (idleScreen != null) idleScreen.SetActive(!on);
        if (acceptButton != null) acceptButton.interactable = on;
        if (denyButton != null) denyButton.interactable = on;
    }

    /// <summary>
    /// A case on the desk: the claim in the app's header and the office's tag
    /// (one text), the app on Documents with its badges cleared, today's
    /// directives, the papers presented, the interview started, the reference
    /// books built the first time and turned to the claim, the steps of the
    /// traveller's kind listed (the papers handed over on arrival received),
    /// the compare cleared. No window opens or closes.
    /// </summary>
    private void ShowRich(CaseInstance inst, ContentLibrarySO lib)
    {
        ShowCaseLayers(true);

        string claim = inst != null ? UiText.Format("claim.banner", inst.visitorDisplayName, inst.claimLine) : string.Empty;
        app.BeginCase(claim, inst != null ? inst.visitorDisplayName : string.Empty);
        if (hud != null)
            hud.SetClaim(claim);

        _reference.ShowDirectives();
        _interview.BeginCase(inst);
        _documents.Present(inst, lib != null ? lib.Agency : null);
        _interview.Start(inst, _documents.Documents, InterviewReachable, AppearanceReachable);
        _reference.BuildBooks(lib);
        _reference.SetClaim(inst);
        if (stepsPanel != null && inst != null)
        {
            stepsPanel.BeginCase(lib != null ? lib.Pc.steps : null, inst.kind, _reference.Day, StepPapers(inst), _interview.QuestionCategories,
                                 lib != null ? lib.ReferenceBooks.Where(b => b != null).Select(b => b.category) : null);
            StepsReceived();
        }

        if (compareController != null)
            compareController.Clear();
    }

    /// <summary>The traveller's papers as the steps count them: each form's number, whether it is asked for, whether it is the photo paper, its fields.</summary>
    private static List<StepPaper> StepPapers(CaseInstance inst) =>
        inst.documents.Select(d => new StepPaper(FormOf(d), d != null && d.template != null && d.template.handOver == DocumentHandOver.OnRequest,
                                                 d != null && d.template != null && d.template.showsPhoto, d != null ? d.fields : null)).ToList();

    /// <summary>A document's form number (blank without a template).</summary>
    private static string FormOf(DocumentInstance doc) => doc != null && doc.template != null ? doc.template.formNumber : string.Empty;

    /// <summary>A paper asked for through the wheel: it is handed over, and the steps hear of the request.</summary>
    private void RequestPaper(int index)
    {
        _documents.HandOver(index);
        if (stepsPanel != null && _currentCase != null && index >= 0 && index < _currentCase.documents.Count)
            stepsPanel.Requested(FormOf(_currentCase.documents[index]));
    }

    /// <summary>The steps hear of every paper handed over so far (on arrival, or asked for).</summary>
    private void StepsReceived()
    {
        if (stepsPanel == null || _currentCase == null)
            return;
        CasePapers papers = _documents.Papers;
        for (int i = 0; i < papers.Count; i++)
            if (papers.State(i) != PaperState.NotHandedOver)
                stepsPanel.Received(i);
    }

    /// <summary>A paper lifted into the hand: the steps count it read.</summary>
    private void StepsRead(int paper)
    {
        if (stepsPanel != null && _currentCase != null)
            stepsPanel.Read(paper);
    }

    /// <summary>An answer heard: the steps count its category asked.</summary>
    private void StepsAsked(ClueCategory category)
    {
        if (stepsPanel != null && _currentCase != null)
            stepsPanel.Asked(category);
    }

    /// <summary>A garment looked at: the steps hear of it.</summary>
    private void StepsLookedAt()
    {
        if (stepsPanel != null && _currentCase != null)
            stepsPanel.LookedAt();
    }

    /// <summary>A pair compared (whatever it showed): the steps hear of the check.</summary>
    private void StepsCompared(CompareEvidence a, CompareEvidence b)
    {
        if (stepsPanel != null && _currentCase != null)
            stepsPanel.Compared(a, b);
    }

    /// <summary>Something new for a case tab: the app badges it unless the player sees it.</summary>
    private void Arrived(AppTab tab)
    {
        if (app != null && _currentCase != null)
            app.Arrived(tab);
    }

    /// <summary>A paper reached the PC: the app decides what that shows (ScanArrival).</summary>
    private void HandleScanned(int paper)
    {
        IReadOnlyList<CaseDocument> documents = _documents.Documents;
        if (app != null && paper >= 0 && paper < documents.Count)
            app.Scanned(paper, documents[paper].name);
    }

    /// <summary>The app header's counters: papers received and scanned, deviations logged.</summary>
    private void ShowCounters()
    {
        if (app != null && _currentCase != null)
            app.SetCounters(_documents.Papers, _evidence.Count);
    }

    private void Accept() => Decide(true);

    private void Deny() => Decide(false);

    /// <summary>
    /// The decision (the app header's buttons or the stamp tray; nothing
    /// without a case on the desk): the desk's papers leave, the case tabs show
    /// the no-case state, and no case is on the desk from here: cleared before
    /// the callback, which may present the next traveller at once (no READY
    /// sign wired).
    /// </summary>
    private void Decide(bool accepted)
    {
        if (_currentCase == null)
            return;

        _documents.EndCase();
        Hide();
        _currentCase = null;
        OneShot.Fire(ref _onDecision, accepted);
    }
}
