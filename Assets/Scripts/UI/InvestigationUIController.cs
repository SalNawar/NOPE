using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The office investigation's façade (the PC redesign RF1, audit R4-001): the
/// one component GameManager talks to, with the scene's references. It
/// presents each case in the Investigation app (InvestigationApp, the PC
/// workbench: the traveller's face, name and counters in its header, never
/// the claim, which the traveller only says; the guided steps; the shelf;
/// two panes; every source has one view per pane and the presenters fill
/// them all) and offers the binary Accept/Deny (the app's decision step, whose
/// Deny waits for a logged difference or broken rule, MatchBoard's findings;
/// and the desk's physical stamps, the papers handed back with the passport's
/// verdict, ungated; wired once); the work is its
/// presenters': CaseDocumentsPresenter (the papers,
/// the hand-over and the scan: the Documents tab), InterviewPresenter (the
/// dialog runner on the traveller wheel, the Transcript tab, the bubble),
/// EvidencePresenter (the discrepancy log, the Report tab, the compare's
/// DEVIATION LOGGED) and DayReference (the Rules, the Reference books' facts,
/// the Records tab's registry, the Calendar); it feeds the guided steps
/// (GuideBar) the case and its events (papers handed over, asked for and
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

    /// <summary>The decision step's Accept (the desk's stamps decide too).</summary>
    [SerializeField] private Button acceptButton;

    /// <summary>The decision step's Deny (interactable once a difference or a broken rule is logged).</summary>
    [SerializeField] private Button denyButton;

    /// <summary>The one compare: every pickable row on the PC, on a held paper, in the bubble and through the Look menu.</summary>
    [SerializeField] private CompareController compareController;

    [Header("The app's tabs (one view per pane, the left pane's first)")]
    /// <summary>The Documents tabs: a chip per paper, the scanned copies.</summary>
    [SerializeField] private DocumentsView[] documentsViews = new DocumentsView[0];

    /// <summary>The Records tabs: the lookup and the Record Extract (Citizen Records; the registry injected per day).</summary>
    [SerializeField] private RecordsView[] recordsViews = new RecordsView[0];

    /// <summary>The Reference tabs: a chip per book, the registers.</summary>
    [SerializeField] private ReferenceView[] referenceViews = new ReferenceView[0];

    /// <summary>The Transcript tabs: the Interview Record (answer rows are compare-clickable).</summary>
    [SerializeField] private TranscriptView[] transcriptViews = new TranscriptView[0];

    /// <summary>The Report tabs: the Deviation Report.</summary>
    [SerializeField] private ReportView[] reportViews = new ReportView[0];

    /// <summary>The Rules tabs: the day's Directive Memo.</summary>
    [SerializeField] private RulesView[] rulesViews = new RulesView[0];

    /// <summary>The Calendar tabs: today in the agency's calendar.</summary>
    [SerializeField] private CalendarView[] calendarViews = new CalendarView[0];

    [Header("Office")]
    /// <summary>The traveller wheel's ring: shows the current interview node's choices (requests, questions, dialog replies).</summary>
    [SerializeField] private InteractionPanelController interactionPanel;

    /// <summary>The physical papers and the scanner (optional: without it documents reach the PC when handed over).</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The office case HUD (piece 10; optional): the office compare strip's host (it prints no claim).</summary>
    [SerializeField] private OfficeCaseHud hud;

    /// <summary>The physical stamps (the desk-first redesign, item 12; optional): the papers handed back with the passport's verdict decide the case like the PC's buttons.</summary>
    [SerializeField] private DeskStampTray stampTray;

    /// <summary>Inspection at the desk (the desk-first redesign, item 11; optional): told the day, the rules and the case.</summary>
    [SerializeField] private DeskInspect deskInspect;

    /// <summary>The traveller wheel: closed after a hand-over; it gives the ring its icons and says the traveller's lines (the claim on arrival, then each reply).</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>Shown on the desktop between travellers.</summary>
    [SerializeField] private GameObject idleScreen;

    /// <summary>The decision's callback for the case on the desk (fired once: OneShot).</summary>
    private Action<bool> _onDecision;

    /// <summary>The case currently on the desk (null between cases).</summary>
    private CaseInstance _currentCase;

    /// <summary>The agency block every page of the app prints (the library's, from the first case shown).</summary>
    private AgencyContent _agency;

    /// <summary>The day's directives, facts, registry and the reference books.</summary>
    private DayReference _reference;

    /// <summary>The current traveller's papers: the Documents tab, the hand-over and the scan.</summary>
    private CaseDocumentsPresenter _documents;

    /// <summary>The current traveller's interview on the wheel and in the Transcript tab.</summary>
    private InterviewPresenter _interview;

    /// <summary>The current case's evidence and the Report tab.</summary>
    private EvidencePresenter _evidence;

    /// <summary>The stamps whose decisions this listens to (null while detached; audit R4-003).</summary>
    private DeskStampTray _stampTrayListening;

    /// <summary>The evidence documented for the current case (or the case just decided): the deviations proven (DiscrepancyLog) and the directive faults' findings (a rule broken, a date that fails: FindingLog.DirectiveEvidence, counted as the decision is made, before the case's findings clear), which a denial needs (VerdictRules.IsUnprovenDenial).</summary>
    public int EvidenceCount => _evidence.Count + (_currentCase != null ? DirectiveEvidence : _directiveEvidenceDecided);

    /// <summary>The directive faults' findings logged for the case at the desk.</summary>
    private int DirectiveEvidence => Board != null ? Board.Log.DirectiveEvidence : 0;

    /// <summary>The directive faults' findings the last decided case logged (Decide), read by the verdict after the case closes.</summary>
    private int _directiveEvidenceDecided;

    /// <summary>The app's guided steps (null without the app).</summary>
    private GuideBar Steps => app != null ? app.Guide : null;

    /// <summary>The app's workbench (null without the app).</summary>
    private MatchBoard Board => app != null ? app.Board : null;

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
    /// Unity's fake null. Build Office UI checks it on the scene it builds
    /// (audit R4-022), so a partly wired desk fails the build, not the day.
    /// </summary>
    public InvestigationWiring Wiring => new InvestigationWiring(
        First(documentsViews) != null && First(documentsViews).Ready, app != null, acceptButton != null, denyButton != null, compareController != null,
        interactionPanel != null, First(transcriptViews) != null, app != null && app.Hosts(AppTab.Transcript), desk != null && desk.IsReachable,
        First(recordsViews) != null);

    /// <summary>The left pane's view of a tab (the first wired one), or null.</summary>
    private static T First<T>(T[] views) where T : UnityEngine.Object
    {
        if (views != null)
            foreach (T view in views)
                if (view != null)
                    return view;
        return null;
    }

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
        _interview.NotCarried += LogNotCarried;
        _interview.MissingChanged += ShowMissing;
        _documents.PapersChanged += ShowMissing;
        _reference.RecordLookedUp += StepsRecordViewed;
        if (compareController != null)
            compareController.PairCompared += StepsCompared;
        if (Board != null)
        {
            Board.Changed += RefreshDecision;
            Board.Logged += Confront;
        }
        if (app != null)
            app.MissingFlagged += FlagMissingFromApp;
        if (deskInspect != null)
            deskInspect.MissingFlagged += FlagMissingFromApp;

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
        if (Steps != null)
            Steps.EndCase();
    }

    /// <summary>The presenters over this component's references (the desk only when it is reachable), each filling the app's search index.</summary>
    private void BuildPresenters(InvestigationWiring wiring)
    {
        CaseIndex index = app != null ? app.Index : null;
        _reference = new DayReference(rulesViews, recordsViews, compareController, referenceViews, calendarViews, index);
        _documents = new CaseDocumentsPresenter(documentsViews, wiring.DeskReachable ? desk : null, compareController, () => _evidence.DocumentedCategories, index);
        _interview = new InterviewPresenter(interactionPanel, transcriptViews, () => Arrived(AppTab.Transcript), wheel, compareController,
                                            RequestPaper, SignWaiver, () => _currentCase, this, index);
        // No Report badge (the desk-first redesign: the Deviation Report leaves the player's view; the findings column is the evidence).
        _evidence = new EvidencePresenter(compareController, reportViews, () => { }, () => _currentCase, () => _agency, () => _reference.Day);
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
        _interview.NotCarried -= LogNotCarried;
        _interview.MissingChanged -= ShowMissing;
        _documents.PapersChanged -= ShowMissing;
        _reference.RecordLookedUp -= StepsRecordViewed;
        if (compareController != null)
            compareController.PairCompared -= StepsCompared;
        if (Board != null)
        {
            Board.Changed -= RefreshDecision;
            Board.Logged -= Confront;
        }
        if (app != null)
            app.MissingFlagged -= FlagMissingFromApp;
        if (deskInspect != null)
            deskInspect.MissingFlagged -= FlagMissingFromApp;
        if (_stampTrayListening != null)
        {
            _stampTrayListening.Decided -= Decide;
            _stampTrayListening = null;
        }
    }

    /// <summary>Injects the day's citizen registry into the Records tab, with the agency block and today's date (<paramref name="day"/> in the agency's calendar) its extract prints; a new day's registry drops the app's pins and recent items (PR2).</summary>
    public void SetCitizenRegistry(CitizenRegistry registry, AgencyContent agency, int day)
    {
        _reference.SetCitizenRegistry(registry, agency, day);
        if (deskInspect != null)
            deskInspect.SetDay(agency, day);
        if (app != null)
            app.BeginDay();
    }

    /// <summary>
    /// The day's introductions (the desk-first redesign, items 3 and 9): the
    /// fields the papers print (Introductions.ShowsField), the shelf's agency
    /// documents and the desktop's apps. A paper's copy reaches the PC only
    /// when scanned (the papers are checked at the desk: track B).
    /// </summary>
    public void SetIntroductions(Introductions introductions, int day)
    {
        Introductions known = introductions ?? Introductions.None;
        _documents.SetDay((form, category) => known.ShowsField(day, form, category));
        if (app != null)
            app.SetIntroductions(known, day);
        if (deskInspect != null)
            deskInspect.SetIntroductions(known, day);
    }

    /// <summary>Injects today's facts (the Reference tab's registers render these rows).</summary>
    public void SetFacts(FactTable facts) => _reference.SetFacts(facts);

    /// <summary>Injects today's interview (questions, dialogs and wording, fixed at day start).</summary>
    public void SetInterviewDay(InterviewDay day) => _interview.SetInterviewDay(day);

    /// <summary>Injects the character art the passport photos are drawn with.</summary>
    public void SetCharacterArt(CharacterArt art) => _documents.SetCharacterArt(art);

    /// <summary>Injects the day-start translation (which tongues are foreign and translated today) and the library's translation settings (their key-word rule included).</summary>
    public void SetTranslation(TranslationDay day, TranslationSettings settings) => _interview.SetTranslation(day, settings);

    /// <summary>Sets the day's travel directives (the Rules tab and the rulebook on the desk).</summary>
    public void SetDirectives(IReadOnlyList<TravelRuleSO> rules)
    {
        _reference.SetDirectives(rules);
        if (deskInspect != null)
            deskInspect.SetRules(rules);
    }

    /// <summary>Presents a case and waits for the player's Accept/Deny (nothing shows when the app is not wired: Awake logged why).</summary>
    public void ShowCase(CaseInstance inst, ContentLibrarySO lib, Action<bool> onDecision)
    {
        _onDecision = onDecision;
        _currentCase = inst;
        _agency = lib != null ? lib.Agency : _agency;
        _evidence.BeginCase();
        if (deskInspect != null)
            deskInspect.BeginCase(inst);

        if (Wiring.Wired)
            ShowRich(inst, lib);
    }

    /// <summary>Between cases: the desktop shows its idle line, the app's case sources show the no-case state, the workbench empties and the steps go. No window closes.</summary>
    public void Hide()
    {
        ShowCaseLayers(false);
        if (app != null)
            app.EndCase();
        if (Steps != null)
            Steps.EndCase();
    }

    /// <summary>Accept and Deny on (a traveller is at the desk; Deny once something can be cited) or off with the desktop's idle line shown.</summary>
    private void ShowCaseLayers(bool on)
    {
        if (idleScreen != null) idleScreen.SetActive(!on);
        RefreshDecision();
    }

    /// <summary>
    /// The decision step's buttons (the PC workbench spec IA10; Saleh's
    /// prototype: "Deny needs a logged difference or a broken rule to cite"):
    /// Accept while a traveller is at the desk, Deny once the findings hold a
    /// difference (FindingLog.HasDifference). The desk's stamp tray is not
    /// gated (a moral choice can still deny anyone).
    /// </summary>
    private void RefreshDecision()
    {
        bool on = _currentCase != null;
        if (acceptButton != null) acceptButton.interactable = on;
        if (denyButton != null) denyButton.interactable = on && Board != null && Board.Log.HasDifference;
    }

    /// <summary>
    /// A finding the workbench just logged (MatchBoard.Logged; wave 5, lesson
    /// 3; the desk-first redesign, item 7): the traveller wheel's question
    /// about its detail unlocks (InterviewUnlocks.About), and when it is
    /// evidence of a difference (Confrontations.About) the wheel gains its
    /// question about exactly it.
    /// </summary>
    private void Confront(Finding finding)
    {
        if (finding != null && finding.Category.HasValue)
            _interview.Unlock(InterviewUnlocks.About(finding.Category.Value));
        _interview.Confront(Confrontations.About(finding));
    }

    /// <summary>
    /// The papers the clerk can flag missing for the traveller at the desk
    /// (the desk-first redesign, item 7: the PC's Papers menu and the desk
    /// read the same list; MissingPapers.Open over <see cref="Papers"/>
    /// gives those not handed over yet). MissingPapers.None between
    /// travellers.
    /// </summary>
    public MissingPapers MissingPapers => _interview != null ? _interview.Missing : MissingPapers.None;

    /// <summary>Where each of the current traveller's papers is (not handed over, on the desk, scanned).</summary>
    public CasePapers Papers => _documents != null ? _documents.Papers : null;

    /// <summary>
    /// Flags the current traveller's paper <paramref name="requestId"/>
    /// (a FormRequest.Id of <see cref="MissingPapers"/>) missing: the wheel
    /// then offers "Hand me your ..." (the desk-first redesign, item 7). The
    /// PC's Papers menu calls it, and the desk's rulebook may (a line that
    /// requires a paper: "Entry ticket missing"). True when it was flaggable
    /// and not flagged yet.
    /// </summary>
    public bool FlagMissing(string requestId) => _currentCase != null && _interview.FlagMissing(requestId);

    /// <summary>The Papers menu (or the desk's rulebook) flagged a paper missing.</summary>
    private void FlagMissingFromApp(string requestId) => FlagMissing(requestId);

    /// <summary>The traveller does not carry a paper they were asked for: the workbench logs it as missing (a difference Deny can cite).</summary>
    private void LogNotCarried(FormRequest request)
    {
        if (Board != null)
            Board.LogMissing(request);
    }

    /// <summary>The missing papers' list or a paper's place changed: the PC's Papers menu redraws its flags, and the desk hears of it.</summary>
    private void ShowMissing()
    {
        MissingPapers missing = _currentCase != null ? _interview.Missing : MissingPapers.None;
        if (app != null)
            app.SetMissing(missing, _documents.Papers);
        if (deskInspect != null)
            deskInspect.SetMissing(missing, _documents.Papers);
    }

    /// <summary>
    /// A case on the desk: the app's title names the traveller (no claim is
    /// printed anywhere: the traveller says it, the personalities spec's B1),
    /// the app on Documents with its badges cleared, today's
    /// directives, the papers presented, the interview started, the reference
    /// books built the first time and turned to the claim, the steps listed
    /// (the default set until the arrival paper is read, then the kind's; the
    /// papers handed over on arrival received),
    /// the compare cleared. No window opens or closes.
    /// </summary>
    private void ShowRich(CaseInstance inst, ContentLibrarySO lib)
    {
        app.BeginCase(inst != null ? inst.visitorDisplayName : string.Empty);
        if (Board != null)
            Board.BeginCase(inst);
        ShowCaseLayers(true);

        _reference.ShowDirectives();
        _interview.BeginCase(inst);
        app.SetSpeechScript(_interview.Translation.Font);
        _documents.Present(inst, lib != null ? lib.Agency : null, lib != null ? lib.Interview : null, lib != null ? lib.LongestOriginLabel : 0);
        _interview.Start(inst, _documents.Documents, InterviewReachable, AppearanceReachable, _agency, _reference.Day);
        _reference.BuildBooks(lib);
        _reference.SetClaim(inst);
        if (compareController != null)
            compareController.Clear();
        if (Steps != null && inst != null)
        {
            Steps.BeginCase(lib != null ? lib.Pc.steps : null, inst.kind, _reference.Day, StepPapers(inst), _interview.QuestionCategories,
                            lib != null ? lib.ReferenceBooks.Where(b => b != null).Select(b => b.category) : null);
            StepsReceived();
        }
    }

    /// <summary>The traveller's papers as the steps count them: each form's number, whether it is asked for, whether it is the photo paper, its fields.</summary>
    private static List<StepPaper> StepPapers(CaseInstance inst) =>
        inst.documents.Select(d => new StepPaper(FormOf(d), d != null && d.template != null && d.template.handOver == DocumentHandOver.OnRequest,
                                                 d != null && d.template != null && d.template.showsPhoto, d != null ? d.fields : null)).ToList();

    /// <summary>A document's form number (blank without a template).</summary>
    private static string FormOf(DocumentInstance doc) => doc != null && doc.template != null ? doc.template.formNumber : string.Empty;

    /// <summary>A waiver signed from the desk's pad was filed (the endings and strandings spec §7.3): GameManager spends the shift minutes it takes.</summary>
    public event Action WaiverSigned;

    /// <summary>The traveller signed the pad's blank: the desk files it on their case (CaseInstance.SignWaiverAtDesk: a valid signed waiver on file, a missing or unsigned waiver's fault cured) and the shift hears of it.</summary>
    private void SignWaiver()
    {
        if (_currentCase == null || !_currentCase.SignWaiverAtDesk())
            return;
        Debug.Log($"[InvestigationUIController] '{_currentCase.visitorDisplayName}' signed a waiver from the pad; filed (directive fault now {_currentCase.directiveFault}, cured {_currentCase.curedAtDesk}).", this);
        WaiverSigned?.Invoke();
    }

    /// <summary>A paper asked for through the wheel: it is handed over and the steps hear of the request.</summary>
    private void RequestPaper(int index)
    {
        _documents.HandOver(index);
        if (_currentCase == null || index < 0 || index >= _currentCase.documents.Count)
            return;
        if (Steps != null)
            Steps.Requested(FormOf(_currentCase.documents[index]));
    }

    /// <summary>The steps hear of every paper handed over so far (on arrival, or asked for).</summary>
    private void StepsReceived()
    {
        if (Steps == null || _currentCase == null)
            return;
        CasePapers papers = _documents.Papers;
        for (int i = 0; i < papers.Count; i++)
            if (papers.State(i) != PaperState.NotHandedOver)
                Steps.Received(i);
    }

    /// <summary>A paper lifted into the hand: the steps count it read.</summary>
    private void StepsRead(int paper)
    {
        if (Steps != null && _currentCase != null)
            Steps.Read(paper);
    }

    /// <summary>An answer heard: the steps count its category asked.</summary>
    private void StepsAsked(ClueCategory category)
    {
        if (Steps != null && _currentCase != null)
            Steps.Asked(category);
    }

    /// <summary>A garment looked at: the steps hear of it.</summary>
    private void StepsLookedAt()
    {
        if (Steps != null && _currentCase != null)
            Steps.LookedAt();
    }

    /// <summary>A record looked up in a Records tab (either pane): the steps hear of it.</summary>
    private void StepsRecordViewed()
    {
        if (Steps != null && _currentCase != null)
            Steps.RecordViewed();
    }

    /// <summary>A pair compared (whatever it showed): the steps hear of the check.</summary>
    private void StepsCompared(CompareEvidence a, CompareEvidence b)
    {
        if (Steps != null && _currentCase != null)
            Steps.Compared(a, b);
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

    /// <summary>The app header's counters: papers received and scanned.</summary>
    private void ShowCounters()
    {
        if (app != null && _currentCase != null)
            app.SetCounters(_documents.Papers);
    }

    /// <summary>
    /// The decided traveller's reaction (the personalities spec's R1-R4): the
    /// verdict, their intent (ReactionIntents.Of: a place lie or a record lie
    /// is Lying) and fault reason choose one or two lines in their voice,
    /// appended to the transcript and said in the bubble in turn. Returns how
    /// long the traveller stays (0: they leave at once). Scores nothing.
    /// </summary>
    public float React(CaseInstance inst, bool accepted)
    {
        if (inst == null || _interview == null)
            return 0f;

        ReactionVerdict verdict = accepted ? ReactionVerdict.Accepted : ReactionVerdict.Denied;
        ReactionIntent intent = ReactionIntents.Of(inst.IsLiar, inst.IsForger);
        IReadOnlyList<DialogLine> lines = _interview.React(inst, verdict, intent);
        Debug.Log($"[InvestigationUIController] Reaction ({verdict} · {intent}, reason '{inst.FaultReason}'): {string.Join(" / ", lines.Select(l => l.Text))}");
        return wheel != null ? wheel.React(lines) : 0f;
    }

    /// <summary>The decided traveller has left (or the next is called): their reaction's bubble hides.</summary>
    public void EndReaction()
    {
        if (wheel != null)
            wheel.EndReaction();
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

        _directiveEvidenceDecided = DirectiveEvidence;
        _documents.EndCase(accepted);
        if (deskInspect != null)
            deskInspect.EndCase();
        _currentCase = null;
        Hide();
        OneShot.Fire(ref _onDecision, accepted);
    }
}
