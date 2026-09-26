using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// The investigation's interview (the PC redesign RF1): the day's interview and
/// translation (set by GameManager through the façade), the current
/// traveller's dialog runner on the traveller wheel's ring, the transcript
/// (the app's Transcript tab), and the wheel's bubble. A document request
/// hands the document over (through the case's documents) and closes the
/// wheel; a look at a garment puts it into the compare and closes the wheel;
/// a choice that adds lines tells the app (the Transcript tab's badge;
/// nothing opens: WN5); the traveller's new lines go to the bubble; a
/// finished dialog is recorded for the shift. Each line joins search's case
/// layer as it is spoken (redesign phase 19, SE5): as shown, so a line the
/// player hears untranslated is found only by its key words and its speaker,
/// and shows its glyphs. The bubble's answer, picked at the desk, goes
/// into the compare as its transcript row would. It subscribes to the wheel
/// it was given and unsubscribes from that same instance (audit R4-003).
/// Plain C#; InvestigationUIController owns it.
/// </summary>
public sealed class InterviewPresenter
{
    private readonly InteractionPanelController _ring;
    private readonly TranscriptWindowController _transcript;
    private readonly Action _spoke;
    private readonly TravellerWheel _wheel;
    private readonly CompareController _compare;
    private readonly Action<int> _handOver;
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

    /// <summary>
    /// The wheel's ring, the transcript, the wheel and the compare (any may be
    /// missing), what new transcript lines tell (the app's Transcript tab), the
    /// hand-over of a document by index (CaseDocumentsPresenter.HandOver), the
    /// façade's current case, the object the logs name and search's index
    /// (null: nothing indexed).
    /// </summary>
    public InterviewPresenter(InteractionPanelController ring, TranscriptWindowController transcript, Action spoke,
                              TravellerWheel wheel, CompareController compare, Action<int> handOver, Func<CaseInstance> currentCase, Object context,
                              CaseIndex index)
    {
        _index = index;
        _ring = ring;
        _transcript = transcript;
        _spoke = spoke ?? throw new ArgumentNullException(nameof(spoke));
        _wheel = wheel;
        _compare = compare;
        _handOver = handOver ?? throw new ArgumentNullException(nameof(handOver));
        _currentCase = currentCase ?? throw new ArgumentNullException(nameof(currentCase));
        _context = context;
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
    /// the hub has a request per document handed over on request, "Look >"
    /// (the traveller's garments) when garments can be compared, and, when the
    /// interview is reachable, today's questions, small talk and offered
    /// dialogs (a premade's own dialog only while they are at the desk; without
    /// a wired transcript nothing spoken could be read, so only the requests
    /// and the look remain). The transcript starts with the opener and the
    /// claim, and the traveller says the claim in the wheel's bubble.
    /// </summary>
    public void Start(CaseInstance inst, IReadOnlyList<CaseDocument> documents, bool interviewReachable, bool appearanceReachable)
    {
        if (_wheel != null)
            _wheel.SetTranslation(_caseTranslation);

        _runner = null;
        if (_day == null)
        {
            Debug.LogError("[InvestigationUIController] No interview day was injected (GameManager.SetInterviewDay), so the traveller wheel is empty.", _context);
            if (_ring != null)
                _ring.Clear();
            return;
        }

        InterviewCase interviewCase = CaseFor(inst, documents, interviewReachable, appearanceReachable);
        string premadeDialog = inst != null && inst.legendarySource != null ? inst.legendarySource.dialogId : null;
        DialogGraph graph = InterviewScript.Build(_day.Lines,
            interviewReachable ? _day.Questions : Array.Empty<InterviewQuestion>(),
            interviewReachable ? _day.OfferedDialogs(premadeDialog) : Array.Empty<AuthoredDialog>(),
            interviewCase);
        _runner = new DialogRunner(graph, InterviewScript.Opening(_day.Lines, interviewCase));

        if (_transcript != null)
            _transcript.Bind(_runner.Transcript, _day.Lines.deskName, inst != null ? inst.visitorGivenName : string.Empty, _compare, _caseTranslation);
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
            _index.Add(IndexEntries.Line(i, UiText.Format("search.title.line", speaker, i + 1), speaker, line.Text, line.English, foreign));
        }
    }

    /// <summary>The traveller as the interview script reads them: small talk only when the interview is reachable, the garments only when the look is.</summary>
    private InterviewCase CaseFor(CaseInstance inst, IReadOnlyList<CaseDocument> documents, bool interviewReachable, bool appearanceReachable) =>
        new InterviewCase
        {
            introLine = inst != null ? inst.introLine : null,
            kind = inst != null ? inst.kind : default,
            claimPlace = inst != null ? inst.originLabel : null,
            keyWords = _keyWords,
            claimedEraId = inst != null && inst.claimedEra != null ? inst.claimedEra.id : null,
            documents = documents,
            answers = inst != null ? inst.answers : null,
            smallTalk = interviewReachable && inst != null ? inst.smallTalk : null,
            garments = appearanceReachable && inst != null && inst.look != null ? inst.look.Garments : null
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
                label = choice.Label,
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
    /// a Costume Guide row on the PC) and closes the wheel; the traveller's lines, when
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

        if (_transcript != null)
            _transcript.Refresh();
        IndexLines(before);
        if (_runner.Transcript.Count > before)
            _spoke();

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

        if (_wheel != null)
            _wheel.Say(InterviewScript.SaidSince(_runner.Transcript, before));

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
        CaseInstance current = _currentCase();
        IReadOnlyList<Garment> garments = current != null && current.look != null ? current.look.Garments : null;
        if (_compare == null || garments == null || garmentIndex < 0 || garmentIndex >= garments.Count)
            return;

        _compare.Select(EvidencePicks.ForGarment(garmentIndex, garments[garmentIndex]), null);
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
