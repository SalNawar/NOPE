using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// The investigation's interview (the PC redesign RF1): the day's interview and
/// translation (set by GameManager through the façade), the current
/// traveller's dialog runner on the traveller wheel's ring, the transcript
/// (the app's Transcript tab, one per pane; its answers link by the case's
/// claim and record lookup: SmartLinks), and the wheel's bubble. A document request
/// hands the document over (through the case's documents) and closes the
/// wheel; a look at a garment puts it into the compare and closes the wheel;
/// a choice that adds lines tells the app (the Transcript tab's badge;
/// nothing opens: WN5); the traveller's new lines go to the bubble; a
/// finished dialog is recorded for the shift; each answer heard and each look
/// at a garment is announced (Answered, LookedAt: the steps checklist). Each
/// line joins search's case layer as it is spoken (redesign phase 19, SE5): as
/// shown, so a line the player hears untranslated is found only by its key
/// words and its speaker, and shows its glyphs. The bubble's answer, picked at
/// the desk, goes
/// into the compare as its transcript row would. It subscribes to the wheel
/// it was given and unsubscribes from that same instance (audit R4-003).
/// Plain C#; InvestigationUIController owns it.
/// </summary>
public sealed class InterviewPresenter
{
    private readonly InteractionPanelController _ring;
    private readonly IReadOnlyList<TranscriptView> _transcripts;
    private readonly Action _spoke;
    private readonly TravellerWheel _wheel;
    private readonly CompareController _compare;
    private readonly Action<int> _handOver;
    private readonly Action _signWaiver;
    private readonly Func<CaseInstance> _currentCase;
    private readonly Object _context;
    private readonly CaseIndex _index;

    /// <summary>Today's interview: askable questions, offered dialogs, wording and the shift's dialog outcomes.</summary>
    private InterviewDay _day;

    /// <summary>The current traveller's interview (null before the first case).</summary>
    private DialogRunner _runner;

    /// <summary>Today's translation (null = everything plain).</summary>
    private TranslationPresenter _translation;

    /// <summary>What of a traveller's lines stays English when they show untranslated (the library's translation.keyWords).</summary>
    private KeyWordRule _keyWords;

    /// <summary>The current traveller's translation (None between cases and when nothing is foreign).</summary>
    private CaseTranslation _caseTranslation = CaseTranslation.None;

    /// <summary>The wheel whose bubble this listens to (null while detached).</summary>
    private TravellerWheel _listening;

    /// <summary>The current traveller's name (the transcript's speaker) and tongue (search's clip match).</summary>
    private string _travellerName = string.Empty;
    private string _tongueId;

    /// <summary>The categories of the questions offered to the current traveller (none when the interview is not reachable).</summary>
    private IReadOnlyList<ClueCategory> _questionCategories = Array.Empty<ClueCategory>();

    /// <summary>The current traveller's graph (the differences menu joins it as the clerk logs differences) and the traveller as the script reads them.</summary>
    private DialogGraph _graph;
    private InterviewCase _interviewCase;

    /// <summary>True while the current traveller's spoken lines can be read (a question about a difference needs the transcript).</summary>
    private bool _interviewReachable;

    /// <summary>True once the current traveller cracked over a difference (a liar who confessed once confesses again).</summary>
    private bool _cracked;

    /// <summary>The papers the clerk can flag missing for the current traveller (the desk-first redesign, item 7).</summary>
    private MissingPapers _missing = MissingPapers.None;

    /// <summary>Raised when the traveller, asked for a paper flagged missing, says they do not carry it (the workbench logs it: FindingKind.PaperMissing).</summary>
    public event Action<FormRequest> NotCarried;

    /// <summary>Raised when a paper is flagged missing or its state changes (the PC's Papers menu redraws).</summary>
    public event Action MissingChanged;

    /// <summary>The papers the clerk can flag missing for the current traveller (MissingPapers.None between travellers).</summary>
    public MissingPapers Missing => _missing;

    /// <summary>Raised for each answer the traveller gives, with its category.</summary>
    public event Action<ClueCategory> Answered;

    /// <summary>Raised when the player looks at one of the traveller's garments.</summary>
    public event Action LookedAt;

    /// <summary>The categories of the questions offered to the current traveller (none when the interview is not reachable).</summary>
    public IReadOnlyList<ClueCategory> QuestionCategories => _questionCategories;

    /// <summary>
    /// The wheel's ring, the transcripts (one per pane; null entries are
    /// skipped), the wheel and the compare (any may be missing), what new
    /// transcript lines tell (the app's Transcript tab), the hand-over of a
    /// document by index (CaseDocumentsPresenter.HandOver), the desk's pad
    /// filing a waiver the traveller signed, the façade's current case, the
    /// object the logs name and search's index (null: nothing indexed).
    /// </summary>
    public InterviewPresenter(InteractionPanelController ring, IReadOnlyList<TranscriptView> transcripts, Action spoke,
                              TravellerWheel wheel, CompareController compare, Action<int> handOver, Action signWaiver, Func<CaseInstance> currentCase, Object context,
                              CaseIndex index)
    {
        _signWaiver = signWaiver ?? throw new ArgumentNullException(nameof(signWaiver));
        _index = index;
        _ring = ring;
        _transcripts = transcripts ?? Array.Empty<TranscriptView>();
        _spoke = spoke ?? throw new ArgumentNullException(nameof(spoke));
        _wheel = wheel;
        _compare = compare;
        _handOver = handOver ?? throw new ArgumentNullException(nameof(handOver));
        _currentCase = currentCase ?? throw new ArgumentNullException(nameof(currentCase));
        _context = context;
    }

    /// <summary>The case's documents' fields, in paper order (the answers' record lookup).</summary>
    private static IEnumerable<IReadOnlyList<DocumentField>> Fields(IReadOnlyList<CaseDocument> documents)
    {
        if (documents == null)
            yield break;
        foreach (CaseDocument document in documents)
            yield return document != null ? document.fields : null;
    }

    /// <summary>Starts listening to the wheel's bubble.</summary>
    public void Attach()
    {
        if (_wheel == null)
            return;
        _listening = _wheel;
        _listening.LineClicked += HandleLineClicked;
    }

    /// <summary>Stops listening (to the instance it attached to).</summary>
    public void Detach()
    {
        if (_listening == null)
            return;
        _listening.LineClicked -= HandleLineClicked;
        _listening = null;
    }

    /// <summary>Sets today's interview (questions, dialogs and wording, fixed at day start).</summary>
    public void SetInterviewDay(InterviewDay day) => _day = day;

    /// <summary>Sets the day-start translation and the library's translation settings (their key-word rule included).</summary>
    public void SetTranslation(TranslationDay day, TranslationSettings settings)
    {
        _translation = new TranslationPresenter(day, settings);
        _keyWords = settings != null && settings.rules != null ? settings.rules.keyWords : null;
    }

    /// <summary>The current traveller's translation (their script's font draws their untranslated lines in search's results too).</summary>
    public CaseTranslation Translation => _caseTranslation;

    /// <summary>A new traveller: their tongue decides how their speech shows today (their papers are always English).</summary>
    public void BeginCase(CaseInstance inst)
    {
        _caseTranslation = _translation != null ? _translation.ForCase(inst) : CaseTranslation.None;
        _travellerName = inst != null ? inst.visitorGivenName : string.Empty;
        _tongueId = inst != null ? inst.tongueId : null;
    }

    /// <summary>
    /// Starts the traveller's interview: the wheel takes the case's translation;
    /// the hub has a request per form or group of the day's papers menu, "Look >"
    /// (the traveller's garments) when garments can be compared, and, when the
    /// interview is reachable, today's questions (the same for every
    /// traveller, the personalities spec's W3), small talk and offered
    /// dialogs (a premade's own dialog only while they are at the desk; without
    /// a wired transcript nothing spoken could be read, so only the requests
    /// and the look remain). The transcript starts with the opener and the
    /// claim (its record headed with <paramref name="agency"/>'s block and
    /// today's <paramref name="day"/>), and the traveller says the claim in
    /// the wheel's bubble.
    /// </summary>
    public void Start(CaseInstance inst, IReadOnlyList<CaseDocument> documents, bool interviewReachable, bool appearanceReachable, AgencyContent agency, int day)
    {
        if (_wheel != null)
            _wheel.SetTranslation(_caseTranslation);

        _runner = null;
        _graph = null;
        _interviewCase = null;
        _cracked = false;
        _missing = MissingPapers.None;
        _interviewReachable = interviewReachable;
        _questionCategories = Array.Empty<ClueCategory>();
        if (_day == null)
        {
            Debug.LogError("[InvestigationUIController] No interview day was injected (GameManager.SetInterviewDay), so the traveller wheel is empty.", _context);
            if (_ring != null)
                _ring.Clear();
            return;
        }

        _questionCategories = interviewReachable ? _day.AskableCategories : Array.Empty<ClueCategory>();
        InterviewCase interviewCase = CaseFor(inst, documents, interviewReachable, appearanceReachable);
        string premadeDialog = inst != null ? inst.premadeDialogId : null;
        DialogGraph graph = InterviewScript.Build(_day.Lines,
            interviewReachable ? _day.Questions : Array.Empty<InterviewQuestion>(),
            interviewReachable ? _day.OfferedDialogs(premadeDialog) : Array.Empty<AuthoredDialog>(),
            interviewCase);
        _graph = graph;
        _interviewCase = interviewCase;
        _runner = new DialogRunner(graph, InterviewScript.Opening(_day.Lines, interviewCase));
        _missing = new MissingPapers(interviewCase.askable, FormRequests.Build(interviewCase.askable, documents, _day.Lines.askGroups));
        MissingChanged?.Invoke();

        CaseClaim claim = AppLinks.Claim(inst);
        string lookup = SmartLinks.CaseLookup(Fields(documents), inst != null ? inst.visitorGivenName : null);
        foreach (TranscriptView transcript in _transcripts)
            if (transcript != null)
                transcript.Bind(_runner.Transcript, _day.Lines.deskName, inst != null ? inst.visitorGivenName : string.Empty, _compare, _caseTranslation, claim, lookup,
                                agency, day);
        IndexLines(0);

        RefreshChoices();

        if (_wheel != null)
            _wheel.Say(InterviewScript.SaidSince(_runner.Transcript, 0));
    }

    /// <summary>
    /// The transcript's lines from <paramref name="from"/> into search's case
    /// layer, as the transcript shows them: "speaker · line n"; a line shown
    /// untranslated (settled: the Speech translator is owned at the day's
    /// start or not) by its key words, its glyphs as its snippet and its
    /// tongue for a pasted clip.
    /// </summary>
    private void IndexLines(int from)
    {
        if (_index == null || _runner == null || _day == null)
            return;
        IReadOnlyList<DialogLine> lines = _runner.Transcript;
        SpeechTranslation speech = _caseTranslation.Speech;
        for (int i = from; i < lines.Count; i++)
        {
            DialogLine line = lines[i];
            string speaker = line.Speaker == DialogSpeaker.Desk ? _day.Lines.deskName : _travellerName;
            Reveal shown = _caseTranslation.Line(line);
            ForeignLine? foreign = DisplayText.ShowsForeign(line.Text, shown, speech.Timing, speech.ReducedMotion)
                ? new ForeignLine(_tongueId, DisplayText.For(line.Text, shown, speech.Timing, speech.ReducedMotion))
                : (ForeignLine?)null;
            _index.Add(IndexEntries.Line(i, UiText.Format("app.row.line", speaker, i + 1), speaker, line.Text, line.English, foreign));
        }
    }

    /// <summary>The traveller as the interview script reads them, with this presenter's day and key words (<see cref="CaseFor(CaseInstance, IReadOnlyList{CaseDocument}, InterviewDay, KeyWordRule, bool, bool)"/>).</summary>
    private InterviewCase CaseFor(CaseInstance inst, IReadOnlyList<CaseDocument> documents, bool interviewReachable, bool appearanceReachable) =>
        CaseFor(inst, documents, _day, _keyWords, interviewReachable, appearanceReachable);

    /// <summary>
    /// The traveller as the interview script reads them: <paramref name="day"/>'s
    /// papers menu (the same for everyone), small talk and the slip only when the
    /// interview is reachable, the garments only when the look is. The game's
    /// interview and the narrative workbook's Cases sheet both build it here.
    /// </summary>
    public static InterviewCase CaseFor(CaseInstance inst, IReadOnlyList<CaseDocument> documents, InterviewDay day, KeyWordRule keyWords, bool interviewReachable, bool appearanceReachable) =>
        new InterviewCase
        {
            introLine = inst != null ? inst.introLine : null,
            kind = inst != null ? inst.kind : default,
            claimPlace = inst != null ? inst.originLabel : null,
            keyWords = keyWords,
            claimedEraId = inst != null && inst.claimedEra != null ? inst.claimedEra.id : null,
            documents = documents,
            askable = inst != null ? day?.AskableForms : null,
            missingVariant = inst != null ? inst.missingFormVariant : MissingFormVariant.Honest,
            answers = inst != null ? inst.answers : null,
            smallTalk = interviewReachable && inst != null ? inst.smallTalk : null,
            voice = inst != null ? inst.Voice : null,
            slip = interviewReachable && inst != null ? inst.slip : null,
            garments = appearanceReachable && (day == null || day.Clothes) && inst != null && inst.look != null ? inst.look.Garments : null,
            face = appearanceReachable && inst != null && inst.look != null && documents != null && documents.Any(d => d != null && d.showsPhoto),
            padReply = inst != null ? inst.waiverPadReply : WaiverPadReply.NotNeeded
        };

    /// <summary>
    /// Shows the current interview node's choices on the traveller wheel,
    /// grouped by kind (DialogChoiceKinds.Arrange), each with its kind's icon
    /// when the wheel is wired ("&lt; Back" in its centre).
    /// </summary>
    private void RefreshChoices()
    {
        if (_ring == null || _runner == null)
            return;

        var actions = new List<InteractionAction>();
        foreach (DialogChoice choice in DialogChoiceKinds.Arrange(_runner.Choices))
        {
            string id = choice.Id;
            actions.Add(new InteractionAction
            {
                label = choice.Kind == DialogChoiceKind.Look ? UiText.DocumentWord(choice.Label) : choice.Label,
                centre = choice.Kind == DialogChoiceKind.Back,
                icon = _wheel != null ? _wheel.IconFor(choice.Kind) : null,
                execute = () => Choose(id)
            });
        }

        _ring.SetActions(actions);
    }

    /// <summary>
    /// Plays one interview choice: the transcript shows its lines (new lines
    /// tell the app: the Transcript tab's badge); a document request hands that
    /// document over (a paper onto the desk, or straight to the PC where no
    /// desk is wired) and closes the wheel so the player can take it; a look at
    /// a garment puts it into the compare bar (the player then compares it with
    /// a Costume Guide row on the PC) and closes the wheel; a waiver signed from
    /// the desk's pad is filed (the wheel stays); the traveller's lines, when
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

        foreach (TranscriptView transcript in _transcripts)
            if (transcript != null)
                transcript.Refresh();
        IndexLines(before);
        if (_runner.Transcript.Count > before)
            _spoke();
        for (int i = before; i < _runner.Transcript.Count; i++)
            if (_runner.Transcript[i].IsAnswer)
                Answered?.Invoke(_runner.Transcript[i].Category);

        if (choice.Action == DialogAction.HandOverDocument)
        {
            _handOver(choice.DocumentIndex);
            if (_wheel != null)
                _wheel.Close();
        }
        else if (choice.Action == DialogAction.InspectGarment)
        {
            LookAt(choice.GarmentIndex);
            if (_wheel != null)
                _wheel.Close();
        }
        else if (choice.Action == DialogAction.InspectFace)
        {
            LookAtFace();
            if (_wheel != null)
                _wheel.Close();
        }
        else if (choice.Action == DialogAction.SignWaiver)
        {
            _signWaiver();
        }
        else if (choice.Action == DialogAction.NotCarried && choice.Request != null && _missing.NotCarried(choice.Request.Id))
        {
            NotCarried?.Invoke(choice.Request);
            MissingChanged?.Invoke();
        }

        if (_wheel != null)
            _wheel.Say(InterviewScript.SaidSince(_runner.Transcript, before));

        if (choice.Action == DialogAction.CompleteDialog)
            _day.Complete(choice.DialogId, choice.EffectName);

        RefreshChoices();
    }

    /// <summary>
    /// A difference the clerk just logged as evidence on the workbench
    /// (MatchBoard.Logged, Confrontations.About; wave 5, lesson 3; null:
    /// nothing): the traveller wheel gains a question about exactly it in
    /// the differences menu (InterviewScript.Confront and AddConfront, up to
    /// the wheel's capacity), the same verb for every traveller; the answer is
    /// decided now in the traveller's voice (Confrontations.Outcome: the
    /// honest explain, a liar cracks by their personality's chance or doubles
    /// down, once cracked always). Asking it costs the shift the time the
    /// exchange takes, as every question does. Nothing without a traveller at
    /// the desk or a readable interview, or for a difference no question is
    /// about (Confrontations.Askable: a photo held against the face).
    /// </summary>
    public void Confront(Discrepancy difference)
    {
        CaseInstance inst = _currentCase();
        if (!Confrontations.Askable(difference) || inst == null || _runner == null || _graph == null || _day == null || !_interviewReachable)
            return;

        ReactionIntent intent = ReactionIntents.Of(inst.IsLiar, inst.IsForger);
        ConfrontOutcome outcome = Confrontations.Outcome(intent, inst.legendarySource != null, inst.confess, inst.dialogSeed, difference.category, _cracked);
        DialogChoice question = InterviewScript.Confront(_day.Lines, _interviewCase, difference, UiText.Category(difference.category), outcome, inst.FaultReason, inst.lie);
        if (question == null)
        {
            Debug.LogWarning($"[InvestigationUIController] No question about a {difference.provedBy} · {difference.source} difference is authored (world_source.json interview.confront.prompts), so the wheel cannot ask about it. Run Tools > TimeDesk > Generate World.", _context);
            return;
        }
        if (!InterviewScript.AddConfront(_graph, _day.Lines, question, _day.Lines.menuCapacity))
            return;
        _cracked |= outcome == ConfrontOutcome.Crack;
        RefreshChoices();
    }

    /// <summary>
    /// Flags the current traveller's paper <paramref name="requestId"/> missing
    /// (the desk-first redesign, item 7; the PC's Papers menu, or the desk):
    /// the wheel then offers its request, "Hand me your ..." (and the waiver
    /// pad for the waiver). True when it was flaggable and not flagged yet.
    /// </summary>
    public bool FlagMissing(string requestId)
    {
        if (_runner == null || !_missing.Flag(requestId))
            return false;
        Unlock(InterviewUnlocks.Missing(requestId));
        MissingChanged?.Invoke();
        return true;
    }

    /// <summary>Unlocks the wheel's choices locked behind <paramref name="key"/> (InterviewUnlocks: a finding about a detail, a paper flagged missing) and redraws the wheel; nothing between travellers.</summary>
    public void Unlock(string key)
    {
        if (_runner != null && _runner.Unlock(key))
            RefreshChoices();
    }

    /// <summary>
    /// The traveller's reaction to the stamp (the personalities spec's R1, §6):
    /// one or two lines in their voice (InterviewScript.Reaction by the
    /// verdict, the intent and the case's fault reason) appended to the
    /// transcript (the Transcript tab shows them and search indexes them).
    /// Returns them for the bubble; empty before any interview.
    /// </summary>
    public IReadOnlyList<DialogLine> React(CaseInstance inst, ReactionVerdict verdict, ReactionIntent intent)
    {
        if (_runner == null || _day == null || inst == null)
            return Array.Empty<DialogLine>();

        IReadOnlyList<DialogLine> lines = InterviewScript.Reaction(_day.Lines, CaseFor(inst, null, false, false), verdict, intent, inst.FaultReason);
        int before = _runner.Transcript.Count;
        _runner.Append(lines);
        foreach (TranscriptView transcript in _transcripts)
            if (transcript != null)
                transcript.Refresh();
        IndexLines(before);
        if (_runner.Transcript.Count > before)
            _spoke();
        return lines;
    }

    /// <summary>
    /// Puts one of the current traveller's garments into the compare bar: its
    /// slot as the label, its item name as the shown value, and as evidence its
    /// place's Culture value (a tell for a liar's dress tell).
    /// </summary>
    private void LookAt(int garmentIndex)
    {
        CaseInstance current = _currentCase();
        IReadOnlyList<Garment> garments = current != null && current.look != null ? current.look.Garments : null;
        if (garments == null || garmentIndex < 0 || garmentIndex >= garments.Count)
            return;

        LookedAt?.Invoke();
        if (_compare != null)
            _compare.Select(EvidencePicks.ForGarment(garmentIndex, garments[garmentIndex]), null);
    }

    /// <summary>Puts the current traveller's face into the compare bar (the document design spec, D8): who they are, to hold a paper's photo against (EvidencePicks.ForFace).</summary>
    private void LookAtFace()
    {
        CaseInstance current = _currentCase();
        if (current == null || current.look == null)
            return;

        LookedAt?.Invoke();
        if (_compare != null)
            _compare.Select(EvidencePicks.ForFace(current.look), null);
    }

    /// <summary>The bubble's answer picked at the desk: it goes into the compare as the transcript's row would (the same pick), lighting the bubble while it shows.</summary>
    private void HandleLineClicked(DialogLine line)
    {
        if (_runner == null || _compare == null || line == null || !line.IsAnswer)
            return;

        IReadOnlyList<DialogLine> transcript = _runner.Transcript;
        for (int i = 0; i < transcript.Count; i++)
            if (transcript[i] == line)
            {
                _compare.Select(EvidencePicks.ForAnswer(i, line, _caseTranslation), _wheel.BubbleHighlightNow);
                return;
            }
    }
}
