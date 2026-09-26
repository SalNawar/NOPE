using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates the office investigation: shows the visitor's travel claim and
/// today's directives, runs the interview on the traveller wheel (document
/// requests, today's questions, a look at the traveller's garments, which go
/// into the compare bar, and narrative dialogs, with the transcript window and
/// the traveller's claim and replies in the wheel's bubble), hands each
/// document over as a physical paper on the desk (whose scan opens its
/// window) or, where no desk is wired, straight to its draggable window,
/// builds a shelf of reference books the player can open/stow, and offers the
/// binary Accept/Deny. From translation's first day a traveller's papers and
/// speech are in their claimed place's tongue (piece 9): the day's
/// TranslationPresenter says how each traveller's text shows. A document's
/// written reveal (one RevealClock for its paper and its scanned copy) starts
/// at its first sighting (piece 10 X25): its paper lifted into the hand, the
/// PC frame opening while its scanned window is open, or a scan opening its
/// window while the frame is open. A held paper's row picked at the desk goes
/// into the same compare as the PC's rows.
///
/// It needs the desk the office builder wires (Tools &gt; TimeDesk &gt; Build
/// Office UI: the document window template, the window layer, Accept and
/// Deny); without it, it logs one error and shows no case (the text-mode
/// fallback no scene could reach was deleted: audit R4-002).
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

    private Action<bool> _onDecision;

    /// <summary>The current traveller's document windows, in paper order.</summary>
    private readonly List<DocumentWindowController> _docWindows = new();
    private readonly List<GameObject> _docIcons = new();

    /// <summary>The current traveller's documents in paper order (name, fields, hand-over, photo).</summary>
    private readonly List<CaseDocument> _caseDocuments = new();

    /// <summary>Each current document's written reveal, in paper order (shared by its scanned window and its desk paper).</summary>
    private readonly List<RevealClock> _clocks = new();

    /// <summary>Papers whose window already has a desktop icon this case.</summary>
    private readonly HashSet<int> _iconedDocuments = new();
    private bool _booksBuilt;
    private string _directives = string.Empty;

    /// <summary>Today's facts (set by GameManager; the books render these rows).</summary>
    private FactTable _facts;

    /// <summary>Documented contradictions for the current case.</summary>
    private readonly DiscrepancyLog _discrepancies = new();

    /// <summary>The case currently on the desk (null between cases).</summary>
    private CaseInstance _currentCase;

    /// <summary>Today's interview (set by GameManager): askable questions, offered dialogs, wording and the shift's dialog outcomes.</summary>
    private InterviewDay _day;

    /// <summary>The current traveller's interview (null before the first case).</summary>
    private DialogRunner _runner;

    /// <summary>Character art (set by GameManager): the passport photos on the papers and the scanned pages.</summary>
    private CharacterArt _art;

    /// <summary>Today's translation (set by GameManager; null = everything plain).</summary>
    private TranslationPresenter _translation;

    /// <summary>The current traveller's translation (None between cases and when nothing is foreign).</summary>
    private CaseTranslation _caseTranslation = CaseTranslation.None;

    /// <summary>True while the PC frame is open (GameManager, from the office view): a scanned window shown then is seen up close.</summary>
    private bool _frameOpen;

    /// <summary>Number of discrepancies documented for the current case.</summary>
    public int EvidenceCount => _discrepancies.Count;

    /// <summary>
    /// True when the evidence loop is playable (the desk and the compare wired),
    /// so scoring may gate denials on documented evidence.
    /// </summary>
    public bool EvidenceSystemActive => Wired && compareController != null;

    /// <summary>
    /// True when a traveller's answers can be read: the wheel's ring, the
    /// transcript window and its chrome are wired. When false, GameManager
    /// computes no answers and generates no spoken tell that day. (Serialized
    /// references are compared with != null: an unassigned one is Unity's fake null.)
    /// </summary>
    public bool InterviewReachable =>
        interactionPanel != null && transcriptWindow != null && transcriptChrome != null;

    /// <summary>
    /// True when a traveller's garments can be looked at and compared: the
    /// wheel's ring (its "Look >" menu) and the compare bar are wired. When
    /// false, GameManager generates no dress tell that day.
    /// </summary>
    public bool AppearanceReachable =>
        interactionPanel != null && compareController != null;

    /// <summary>True when the office builder wired the desk windows a case needs (the document window template, the window layer, Accept and Deny).</summary>
    private bool Wired =>
        documentWindowTemplate != null && windowLayer != null &&
        acceptButton != null && denyButton != null;

    /// <summary>True when documents become physical papers: the wired desk windows and the desk with all its parts (a partly wired desk takes the window path, so papers always reach the PC).</summary>
    private bool DeskReachable => Wired && desk != null && desk.IsReachable;

    private void Awake()
    {
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

        if (compareController != null)
            compareController.PairCompared += HandlePairCompared;

        // Without the desk windows no case can be shown (there is no text fallback any more).
        if (!Wired)
            Debug.LogError("[InvestigationUIController] The desk windows are not wired (documentWindowTemplate, windowLayer, acceptButton or denyButton): no case can be shown. Run Tools > TimeDesk > Build Office UI.", this);

        // Birth-date tells are proven only against Citizen Records (RecordMismatch).
        if (EvidenceSystemActive && recordsWindow == null)
            Debug.LogWarning("[InvestigationUIController] Citizen Records not wired: birth-date tells cannot be proven. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the transcript nothing a traveller says could be read, so the day speaks no tell.
        if (Wired && !InterviewReachable)
            Debug.LogWarning("[InvestigationUIController] Traveller wheel or interview transcript not wired: questions are hidden and no tell is spoken today. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the wheel's look menu or the compare bar no garment could be compared, so the day leaks no dress.
        if (Wired && !AppearanceReachable)
            Debug.LogWarning("[InvestigationUIController] Traveller wheel or compare bar not wired (interactionPanel or compareController): garments cannot be looked at and no dress tell is generated today. Run Tools > TimeDesk > Build Office UI.", this);

        // Without the desk every document still reaches the PC, as its window.
        if (Wired && !DeskReachable)
            Debug.LogWarning("[InvestigationUIController] Desk scanner not wired: documents open on the PC when handed over (no physical papers). Run Tools > TimeDesk > Build Office UI.", this);

        if (DeskReachable)
        {
            desk.ScanFinished += OpenDocumentWindow;
            desk.PaperExamined += Sighted;
            desk.FieldPicked += HandleFieldPicked;
        }

        if (wheel != null)
            wheel.LineClicked += HandleLineClicked;

        if (stampTray != null)
            stampTray.Decided += Decide;

        if (idleScreen != null)
            idleScreen.SetActive(true);
    }

    private void OnDestroy()
    {
        if (compareController != null)
            compareController.PairCompared -= HandlePairCompared;

        if (DeskReachable)
        {
            desk.ScanFinished -= OpenDocumentWindow;
            desk.PaperExamined -= Sighted;
            desk.FieldPicked -= HandleFieldPicked;
        }

        if (wheel != null)
            wheel.LineClicked -= HandleLineClicked;

        if (stampTray != null)
            stampTray.Decided -= Decide;
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
            _currentCase.claimedEra != null ? _currentCase.claimedEra.id : null,
            _currentCase.visitorGivenName);
        if (proof == null)
            return;

        if (!_discrepancies.Add(proof))
        {
            if (compareController != null)
                compareController.ShowAlreadyDocumented(UiText.Category(proof.category));
            return;
        }

        RefreshScannerText();

        if (compareController != null)
            compareController.ShowDeviation(UiText.Deviation(proof));

        if (scannerWindow != null)
            scannerWindow.Open();
    }

    /// <summary>Injects the day's citizen registry into the Records app.</summary>
    public void SetCitizenRegistry(CitizenRegistry registry)
    {
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

    /// <summary>Injects the character art the passport photos are drawn with.</summary>
    public void SetCharacterArt(CharacterArt art)
    {
        _art = art;
    }

    /// <summary>Injects the day-start translation (which tongues are foreign and translated today) and the library's translation settings.</summary>
    public void SetTranslation(TranslationDay day, TranslationSettings settings)
    {
        _translation = new TranslationPresenter(day, settings);
    }

    /// <summary>Rewrites the Scanner window body from the discrepancy log.</summary>
    private void RefreshScannerText()
    {
        if (scannerText == null)
            return;

        if (_discrepancies.Count == 0)
        {
            scannerText.text = UiText.Get("scanner.idle");
            return;
        }

        var sb = new StringBuilder();
        foreach (Discrepancy d in _discrepancies.Items)
            sb.AppendLine(UiText.Format("list.bullet", UiText.Deviation(d)));

        sb.AppendLine();
        sb.AppendLine(UiText.Format("scanner.summary", _discrepancies.Count));
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
            return UiText.Get("directives.none");

        var sb = new StringBuilder(UiText.Get("directives.header") + "\n");
        foreach (TravelRuleSO r in rules)
            if (r != null)
                sb.AppendLine(UiText.Format("list.bullet", r.Summary()));
        return sb.ToString();
    }

    /// <summary>Presents a case and waits for the player's Accept/Deny (nothing shows when the desk windows are not wired: Awake logged why).</summary>
    public void ShowCase(CaseInstance inst, ContentLibrarySO lib, Action<bool> onDecision)
    {
        _onDecision = onDecision;
        _currentCase = inst;
        _discrepancies.Clear();
        RefreshScannerText();

        if (Wired)
            ShowRich(inst, lib);
    }

    /// <summary>Hides the investigation overlay (between cases); the desktop shows its idle line and the office's claim tag empties.</summary>
    public void Hide()
    {
        if (root != null) root.SetActive(false);
        if (hud != null) hud.SetClaim(string.Empty);
        if (idleScreen != null) idleScreen.SetActive(true);
    }

    // -----------------------------
    // Rich mode
    // -----------------------------

    private void ShowRich(CaseInstance inst, ContentLibrarySO lib)
    {
        if (root != null) root.SetActive(true);
        if (idleScreen != null) idleScreen.SetActive(false);

        // The claim banner on the PC and the claim tag in the office: one text.
        string claim = inst != null ? UiText.Format("claim.banner", inst.visitorDisplayName, inst.claimLine) : string.Empty;
        if (claimText != null)
            claimText.text = claim;
        if (hud != null)
            hud.SetClaim(claim);

        if (directivesText != null)
            directivesText.text = _directives;

        // A new visitor clears the desk: every open window closes.
        // (A pin system will later let the player keep chosen windows open.)
        CloseAllWindows();

        foreach (DocumentWindowController w in _docWindows)
            if (w != null)
                Destroy(w.gameObject);
        _docWindows.Clear();

        foreach (GameObject ic in _docIcons)
            if (ic != null)
                Destroy(ic);
        _docIcons.Clear();
        _iconedDocuments.Clear();
        _caseDocuments.Clear();
        _clocks.Clear();

        // The traveller's tongue decides how their papers and speech show today.
        _caseTranslation = _translation != null ? _translation.ForCase(inst) : CaseTranslation.None;

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
                var clock = new RevealClock();
                clone.SetDocument(doc, i, compareController, inst.look, _art, _caseTranslation, clock);
                _docWindows.Add(clone);
                _clocks.Add(clock);
                _caseDocuments.Add(new CaseDocument
                {
                    name = doc != null && doc.template != null ? doc.template.displayName : UiText.Get("document.untitled"),
                    fields = doc != null ? doc.fields : null,
                    handOver = doc != null && doc.template != null ? doc.template.handOver : DocumentHandOver.OnRequest,
                    showsPhoto = doc != null && doc.template != null && doc.template.showsPhoto
                });
                i++;
            }
        }

        if (DeskReachable)
        {
            desk.BeginCase(_caseDocuments, inst != null ? inst.look : null, _art, _caseTranslation, _clocks);
        }
        else
        {
            foreach (int i in CaseDocuments.ArrivalIndices(_caseDocuments))
                OpenDocumentWindow(i);
        }

        if (wheel != null)
            wheel.SetTranslation(_caseTranslation);
        StartInterview(inst, _caseDocuments);

        BuildBookShelf(lib);

        if (compareController != null)
            compareController.Clear();

        WireDecisionButtons(acceptButton, denyButton);
    }

    /// <summary>
    /// Starts the traveller's interview: the hub with a request per document
    /// handed over on request, "Look >" (the traveller's garments) when
    /// garments can be compared, and, when the interview is reachable, today's
    /// questions, small talk and offered dialogs (a premade's own dialog only
    /// while they are at the desk; without a wired transcript nothing spoken
    /// could be read, so only the requests and the look remain). The
    /// transcript starts with the opener and the claim, and the traveller says
    /// the claim in the wheel's bubble.
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
            smallTalk = reachable && inst != null ? inst.smallTalk : null,
            garments = AppearanceReachable && inst != null && inst.look != null ? inst.look.Garments : null
        };

        string premadeDialog = inst != null && inst.legendarySource != null ? inst.legendarySource.dialogId : null;
        DialogGraph graph = InterviewScript.Build(_day.Lines,
            reachable ? _day.Questions : Array.Empty<InterviewQuestion>(),
            reachable ? _day.OfferedDialogs(premadeDialog) : Array.Empty<AuthoredDialog>(),
            interviewCase);
        _runner = new DialogRunner(graph, InterviewScript.Opening(interviewCase));

        if (transcriptWindow != null)
            transcriptWindow.Bind(_runner.Transcript, _day.Lines.deskName, inst != null ? inst.visitorGivenName : string.Empty, compareController, _caseTranslation);

        RefreshChoices();

        if (wheel != null)
            wheel.Say(InterviewScript.SaidSince(_runner.Transcript, 0));
    }

    /// <summary>
    /// Shows the current interview node's choices on the traveller wheel,
    /// grouped by kind (DialogChoiceKinds.Arrange), each with its kind's icon
    /// when the wheel is wired ("&lt; Back" in its centre).
    /// </summary>
    private void RefreshChoices()
    {
        if (interactionPanel == null || _runner == null)
            return;

        var actions = new List<InteractionAction>();
        foreach (DialogChoice choice in DialogChoiceKinds.Arrange(_runner.Choices))
        {
            string id = choice.Id;
            actions.Add(new InteractionAction
            {
                label = choice.Label,
                centre = choice.Kind == DialogChoiceKind.Back,
                icon = wheel != null ? wheel.IconFor(choice.Kind) : null,
                execute = () => Choose(id)
            });
        }

        interactionPanel.SetActions(actions);
    }

    /// <summary>
    /// Plays one interview choice: the transcript shows its lines; a document
    /// request hands that document over (a paper onto the desk, or straight to
    /// its window where no desk is wired) and closes the wheel so the player can
    /// take it; a look at a garment puts it into the compare bar (the player
    /// then compares it with a Costume Guide row on the PC) and closes the
    /// wheel; any other choice opens the transcript; the traveller's lines, when
    /// the choice adds some, go to the wheel's bubble (the spoken reveal point),
    /// queued after what they are saying, each changing a premade's picture as
    /// it starts (a choice without one, such as "Ask about home >" or "&lt;
    /// Back", adds nothing); a finished dialog is recorded for the end of the
    /// shift.
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
        else if (choice.Action == DialogAction.InspectGarment)
        {
            LookAt(choice.GarmentIndex);
            if (wheel != null)
                wheel.Close();
        }
        else if (transcriptChrome != null)
        {
            transcriptChrome.Open();
        }

        if (wheel != null)
            wheel.Say(InterviewScript.SaidSince(_runner.Transcript, before));

        if (choice.Action == DialogAction.CompleteDialog)
            _day.Complete(choice.DialogId, choice.EffectName);

        RefreshChoices();
    }

    /// <summary>
    /// Puts one of the current traveller's garments into the compare bar: its
    /// slot as the label, its item name as the shown value, and as evidence its
    /// place's Culture value (a tell for a liar's dress tell).
    /// </summary>
    private void LookAt(int garmentIndex)
    {
        IReadOnlyList<Garment> garments = _currentCase != null && _currentCase.look != null ? _currentCase.look.Garments : null;
        if (compareController == null || garments == null || garmentIndex < 0 || garmentIndex >= garments.Count)
            return;

        compareController.Select(EvidencePicks.ForGarment(garmentIndex, garments[garmentIndex]), null);
    }

    /// <summary>
    /// Opens a paper's scanned window and raises it (the desk's ScanFinished,
    /// or a hand-over where no desk is wired); shown in the open frame, it is a
    /// sighting (its translation's reveal), else it waits untranslated on the
    /// office PC's small screen. The first time it opens this case, the paper
    /// also gets a desktop icon at the top of the grid, which reopens the
    /// window after it is closed.
    /// </summary>
    private void OpenDocumentWindow(int index)
    {
        DocumentWindowController window = index >= 0 && index < _docWindows.Count ? _docWindows[index] : null;
        if (window == null)
            return;

        window.gameObject.SetActive(true);
        window.transform.SetAsLastSibling();
        if (_frameOpen)
            Sighted(index);
        if (_iconedDocuments.Add(index))
            AddDesktopIcon(_caseDocuments[index].name, window.gameObject, true);
    }

    /// <summary>
    /// The PC frame opened or closed (GameManager, from the office view):
    /// opening it is a sighting of every scanned window that is open.
    /// </summary>
    public void SetFrameOpen(bool open)
    {
        _frameOpen = open;
        if (!open)
            return;

        for (int i = 0; i < _docWindows.Count; i++)
            if (_docWindows[i] != null && _docWindows[i].gameObject.activeSelf)
                Sighted(i);
    }

    /// <summary>
    /// Document <paramref name="index"/> is seen up close (piece 10 X25): its
    /// written reveal starts the first time (DocumentReveal.Begin), and its
    /// scanned window and its paper redraw from the shared clock.
    /// </summary>
    private void Sighted(int index)
    {
        if (index < 0 || index >= _clocks.Count || index >= _caseDocuments.Count)
            return;
        if (!DocumentReveal.Begin(_clocks[index], _caseTranslation, _caseDocuments[index].fields, Time.unscaledTime))
            return;

        if (index < _docWindows.Count && _docWindows[index] != null)
            _docWindows[index].Refresh();
        if (DeskReachable)
            desk.RefreshPaper(index);
    }

    /// <summary>The bubble's answer picked at the desk: it goes into the compare as the transcript's row would (the same pick), lighting the bubble while it shows.</summary>
    private void HandleLineClicked(DialogLine line)
    {
        if (_runner == null || compareController == null || line == null || !line.IsAnswer)
            return;

        IReadOnlyList<DialogLine> transcript = _runner.Transcript;
        for (int i = 0; i < transcript.Count; i++)
            if (transcript[i] == line)
            {
                compareController.Select(EvidencePicks.ForAnswer(i, line, _caseTranslation), wheel.BubbleHighlightNow);
                return;
            }
    }

    /// <summary>A held paper's row picked at the desk: the document's flip finishes on both surfaces, then the row goes into the compare (the same pick as its scanned copy's row).</summary>
    private void HandleFieldPicked(int index, DocumentRow row, ICompareHighlight highlight)
    {
        if (index < 0 || index >= _caseDocuments.Count || compareController == null)
            return;

        _clocks[index].Finish();
        compareController.Select(EvidencePicks.ForField(index, row, _caseDocuments[index].name, _caseTranslation), highlight);
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
}
