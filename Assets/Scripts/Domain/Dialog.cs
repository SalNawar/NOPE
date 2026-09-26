using System;
using System.Collections.Generic;

/// <summary>Who speaks a line in the interview transcript.</summary>
public enum DialogSpeaker
{
    /// <summary>The player's desk.</summary>
    Desk,

    /// <summary>The traveller.</summary>
    Traveller
}

/// <summary>What choosing a dialog choice does beyond appending lines and moving on.</summary>
public enum DialogAction
{
    /// <summary>Nothing else.</summary>
    None,

    /// <summary>The traveller hands a document over (DialogChoice.DocumentIndex): onto the desk, or straight to its window where no desk is wired.</summary>
    HandOverDocument,

    /// <summary>A narrative dialog ends: it is recorded for the end of the shift (DialogChoice.DialogId, EffectName).</summary>
    CompleteDialog,

    /// <summary>The player looks at one of the traveller's garments (DialogChoice.GarmentIndex): it goes into the compare bar.</summary>
    InspectGarment
}

/// <summary>
/// What a choice is, for renderers: the wheel orders its ring by kind, shows
/// each kind's icon and puts Back in its centre. A sub-menu entry takes the
/// kind of what it opens ("Ask about home >" is a Question). Append only.
/// </summary>
public enum DialogChoiceKind
{
    /// <summary>A reply inside a narrative dialog (and any choice not given a kind).</summary>
    Normal,

    /// <summary>The way back to the hub (the wheel's centre).</summary>
    Back,

    /// <summary>Something the desk asks the traveller to do: hand a document over, or a spoken request.</summary>
    Request,

    /// <summary>A question about home, small talk, or the ask menu's entry.</summary>
    Question,

    /// <summary>A look at a garment, or the look menu's entry.</summary>
    Look,

    /// <summary>The entry that starts a narrative dialog.</summary>
    Dialog
}

/// <summary>The wheel's rules for kinds: one order for every menu, and each kind's icon file name. Pure, so both are tested headless.</summary>
public static class DialogChoiceKinds
{
    /// <summary>Where a kind sorts: Back 0, Request 1, Question 2, Look 3, Dialog 4, Normal 5.</summary>
    public static int Rank(DialogChoiceKind kind)
    {
        switch (kind)
        {
            case DialogChoiceKind.Back: return 0;
            case DialogChoiceKind.Request: return 1;
            case DialogChoiceKind.Question: return 2;
            case DialogChoiceKind.Look: return 3;
            case DialogChoiceKind.Dialog: return 4;
            default: return 5;
        }
    }

    /// <summary>
    /// The choices as the wheel shows them: grouped by <see cref="Rank"/>, in
    /// their given order within a kind (a stable sort), nulls dropped; a new
    /// list (empty for null).
    /// </summary>
    public static IReadOnlyList<DialogChoice> Arrange(IReadOnlyList<DialogChoice> choices)
    {
        var order = new List<int>();
        for (int i = 0; choices != null && i < choices.Count; i++)
            if (choices[i] != null)
                order.Add(i);

        // List.Sort is not stable: ties keep their given position.
        order.Sort((a, b) =>
        {
            int byKind = Rank(choices[a].Kind).CompareTo(Rank(choices[b].Kind));
            return byKind != 0 ? byKind : a.CompareTo(b);
        });

        var arranged = new List<DialogChoice>(order.Count);
        foreach (int i in order)
            arranged.Add(choices[i]);
        return arranged;
    }

    /// <summary>The kind's icon file name, "wheel_" + the kind in lower case ("wheel_request"); final art by that name replaces the placeholder.</summary>
    public static string IconName(DialogChoiceKind kind) => "wheel_" + kind.ToString().ToLowerInvariant();
}

/// <summary>One transcript line. Immutable; an answer line also carries the fact it states, and a traveller's line the spans that stay English when it shows untranslated.</summary>
public sealed class DialogLine
{
    /// <summary>A spoken line, optionally with the expression a premade shows while saying it and its key-word spans (KeyWords.Spans; null for none).</summary>
    public DialogLine(string id, DialogSpeaker speaker, string text, string expression = null, IReadOnlyList<(int start, int length)> english = null)
        : this(id, speaker, text, false, default, null, false, expression, english)
    {
    }

    private DialogLine(string id, DialogSpeaker speaker, string text, bool isAnswer, ClueCategory category, string value, bool isTell, string expression,
                       IReadOnlyList<(int start, int length)> english)
    {
        Id = id;
        Speaker = speaker;
        Text = text;
        IsAnswer = isAnswer;
        Category = category;
        Value = value;
        IsTell = isTell;
        Expression = expression;
        English = english ?? Array.Empty<(int start, int length)>();
    }

    /// <summary>A traveller's answer line: its sentence and key-word spans (null for none), plus the answer's category, canonical value and tell flag (no expression).</summary>
    public static DialogLine Answer(string id, string text, InterviewAnswer a, IReadOnlyList<(int start, int length)> english = null) =>
        new DialogLine(id, DialogSpeaker.Traveller, text, true, a != null ? a.category : default, a != null ? a.value : null, a != null && a.isTell, null, english);

    /// <summary>Stable id (the transcript names its row Line_{Id}).</summary>
    public string Id { get; }

    /// <summary>Who says it.</summary>
    public DialogSpeaker Speaker { get; }

    /// <summary>The sentence.</summary>
    public string Text { get; }

    /// <summary>True for an answer to a question (the only compare-clickable rows).</summary>
    public bool IsAnswer { get; }

    /// <summary>The answered category (answers only).</summary>
    public ClueCategory Category { get; }

    /// <summary>The canonical value the sentence contains verbatim (answers only).</summary>
    public string Value { get; }

    /// <summary>True when the answer is a liar's Answer tell.</summary>
    public bool IsTell { get; }

    /// <summary>The expression a premade shows while saying the line (a LookKeys.Expressions token), or null.</summary>
    public string Expression { get; }

    /// <summary>
    /// The spans of <see cref="Text"/> that stay English when the line shows
    /// untranslated (the key words: KeyWords.Spans, from the formatter),
    /// sorted and apart; empty for none (a desk line: always English). Never null.
    /// </summary>
    public IReadOnlyList<(int start, int length)> English { get; }
}

/// <summary>One choice the player can pick at a node.</summary>
public sealed class DialogChoice
{
    /// <summary>Id, unique within the graph.</summary>
    public string Id;

    /// <summary>The choice's label on the traveller wheel.</summary>
    public string Label;

    /// <summary>What the choice is, for renderers (Normal unless set).</summary>
    public DialogChoiceKind Kind;

    /// <summary>Lines appended to the transcript when chosen.</summary>
    public List<DialogLine> Lines = new List<DialogLine>();

    /// <summary>Node to move to; null or empty stays on the current node.</summary>
    public string Next;

    /// <summary>True when the choice leaves the menu once picked (per traveller).</summary>
    public bool OneShot;

    /// <summary>What else choosing it does.</summary>
    public DialogAction Action;

    /// <summary>HandOverDocument: index of the document in paper order.</summary>
    public int DocumentIndex = -1;

    /// <summary>InspectGarment: index of the garment in the traveller's look (TravellerLook.Garments).</summary>
    public int GarmentIndex = -1;

    /// <summary>CompleteDialog: the finished dialog's id.</summary>
    public string DialogId;

    /// <summary>CompleteDialog: the EffectSO asset name to apply at the end of the shift (empty = none).</summary>
    public string EffectName;
}

/// <summary>One node of the runtime graph.</summary>
public sealed class DialogNode
{
    /// <summary>Id, unique within the graph.</summary>
    public string Id;

    /// <summary>Lines appended every time the node is entered.</summary>
    public List<DialogLine> Lines = new List<DialogLine>();

    /// <summary>The choices offered here.</summary>
    public List<DialogChoice> Choices = new List<DialogChoice>();
}

/// <summary>A traveller's interview as a graph of nodes, built per traveller (InterviewScript.Build).</summary>
public sealed class DialogGraph
{
    /// <summary>Nodes by id.</summary>
    private readonly Dictionary<string, DialogNode> _nodes = new Dictionary<string, DialogNode>();

    /// <summary>Creates a graph that starts at <paramref name="startNodeId"/>.</summary>
    public DialogGraph(string startNodeId)
    {
        StartNodeId = startNodeId;
    }

    /// <summary>Where a runner starts.</summary>
    public string StartNodeId { get; }

    /// <summary>Adds a node.</summary>
    /// <exception cref="ArgumentException">A node with the same id exists.</exception>
    public void Add(DialogNode node)
    {
        if (node == null || node.Id == null)
            throw new ArgumentException("A dialog node needs an id.");
        if (_nodes.ContainsKey(node.Id))
            throw new ArgumentException($"Dialog node '{node.Id}' is added twice.");
        _nodes.Add(node.Id, node);
    }

    /// <summary>The node with this id, or null.</summary>
    public DialogNode Node(string id) =>
        id != null && _nodes.TryGetValue(id, out DialogNode node) ? node : null;
}

/// <summary>
/// Plays a dialog graph: an append-only transcript and the choices of the
/// current node. Pure, so every step is tested headless.
/// </summary>
public sealed class DialogRunner
{
    private readonly DialogGraph _graph;
    private readonly List<DialogLine> _transcript = new List<DialogLine>();
    private readonly HashSet<string> _used = new HashSet<string>();
    private DialogNode _current;

    /// <summary>Starts with the opening lines, then enters the start node (appending its lines).</summary>
    public DialogRunner(DialogGraph graph, IEnumerable<DialogLine> opening)
    {
        _graph = graph;
        if (opening != null)
            foreach (DialogLine line in opening)
                if (line != null)
                    _transcript.Add(line);

        Enter(graph != null ? graph.Node(graph.StartNodeId) : null);
    }

    /// <summary>Every line so far, in order (append-only; a live view).</summary>
    public IReadOnlyList<DialogLine> Transcript => _transcript;

    /// <summary>The current node's choices minus the used one-shot ones, as a fresh list.</summary>
    public IReadOnlyList<DialogChoice> Choices
    {
        get
        {
            var choices = new List<DialogChoice>();
            if (_current != null)
                foreach (DialogChoice c in _current.Choices)
                    if (c != null && !(c.OneShot && _used.Contains(c.Id)))
                        choices.Add(c);
            return choices;
        }
    }

    /// <summary>
    /// Picks a currently offered choice: appends its lines, marks it used if
    /// one-shot, and moves to its Next node (a null, empty or unknown Next
    /// stays). Returns the choice, or null (changing nothing) when the id is
    /// not among <see cref="Choices"/>.
    /// </summary>
    public DialogChoice Choose(string choiceId)
    {
        DialogChoice choice = null;
        foreach (DialogChoice c in Choices)
        {
            if (c.Id == choiceId)
            {
                choice = c;
                break;
            }
        }

        if (choice == null)
            return null;

        foreach (DialogLine line in choice.Lines)
            if (line != null)
                _transcript.Add(line);

        if (choice.OneShot)
            _used.Add(choice.Id);

        DialogNode next = string.IsNullOrEmpty(choice.Next) ? null : _graph.Node(choice.Next);
        if (next != null)
            Enter(next);

        return choice;
    }

    /// <summary>Makes a node current and appends its lines.</summary>
    private void Enter(DialogNode node)
    {
        if (node == null)
            return;

        _current = node;
        foreach (DialogLine line in node.Lines)
            if (line != null)
                _transcript.Add(line);
    }
}
