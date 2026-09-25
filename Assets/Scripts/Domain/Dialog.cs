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
    CompleteDialog
}

/// <summary>What a choice is, for renderers: the wheel puts Back in its centre. Piece 8 appends kinds.</summary>
public enum DialogChoiceKind
{
    /// <summary>An ordinary choice (a ring item).</summary>
    Normal,

    /// <summary>The way back to the hub (the wheel's centre).</summary>
    Back
}

/// <summary>One transcript line. Immutable; an answer line also carries the fact it states.</summary>
public sealed class DialogLine
{
    /// <summary>A spoken line.</summary>
    public DialogLine(string id, DialogSpeaker speaker, string text)
        : this(id, speaker, text, false, default, null, false)
    {
    }

    private DialogLine(string id, DialogSpeaker speaker, string text, bool isAnswer, ClueCategory category, string value, bool isTell)
    {
        Id = id;
        Speaker = speaker;
        Text = text;
        IsAnswer = isAnswer;
        Category = category;
        Value = value;
        IsTell = isTell;
    }

    /// <summary>A traveller's answer line: its sentence, plus the answer's category, canonical value and tell flag.</summary>
    public static DialogLine Answer(string id, string text, InterviewAnswer a) =>
        new DialogLine(id, DialogSpeaker.Traveller, text, true, a != null ? a.category : default, a != null ? a.value : null, a != null && a.isTell);

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
