using System;
using System.Collections.Generic;

/// <summary>
/// One of the case's papers as the steps checklist counts it: its form number
/// (a step's forms name it), whether it is handed over on request, whether it
/// is the kind's primary form (the photo paper: a record lookup reads its
/// Citizen ID or name) and its fields.
/// </summary>
public sealed class StepPaper
{
    /// <summary>A paper of form <paramref name="kind"/> ("TC-610").</summary>
    public StepPaper(string kind, bool onRequest, bool primary, IReadOnlyList<DocumentField> fields)
    {
        Kind = kind ?? string.Empty;
        OnRequest = onRequest;
        Primary = primary;
        Fields = fields ?? Array.Empty<DocumentField>();
    }

    /// <summary>Its form number.</summary>
    public string Kind { get; }

    /// <summary>Handed over when asked for (not on arrival).</summary>
    public bool OnRequest { get; }

    /// <summary>The kind's primary form.</summary>
    public bool Primary { get; }

    /// <summary>Its fields, in form order.</summary>
    public IReadOnlyList<DocumentField> Fields { get; }
}

/// <summary>
/// What the player has done for the traveller at the desk (the PC redesign
/// §3.1's events, fed by the Investigation app), beside the case's shape (its
/// papers, today's question categories, the books' categories) and the ticks
/// the player set by hand. It records that a check was made, never what it
/// found. Each recording method returns true when it changed something (the
/// panel redraws then). Pure; one per case.
/// </summary>
public sealed class CaseProgress
{
    private readonly List<StepPaper> _papers;
    private readonly bool[] _received;
    private readonly bool[] _read;
    private readonly List<ClueCategory> _paperCategories = new List<ClueCategory>();
    private readonly List<ClueCategory> _questions = new List<ClueCategory>();
    private readonly HashSet<ClueCategory> _books;
    private readonly HashSet<string> _requested = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<(StatementKind, ClueCategory, TruthKind)> _compared = new HashSet<(StatementKind, ClueCategory, TruthKind)>();
    private readonly List<ClueCategory> _asked = new List<ClueCategory>();
    private readonly Dictionary<string, bool> _manual = new Dictionary<string, bool>(StringComparer.Ordinal);

    /// <summary>A case of <paramref name="papers"/> (in paper order), with today's question categories and the reference books' categories.</summary>
    public CaseProgress(IReadOnlyList<StepPaper> papers, IEnumerable<ClueCategory> questionCategories, IEnumerable<ClueCategory> bookCategories)
    {
        _papers = new List<StepPaper>();
        if (papers != null)
            foreach (StepPaper p in papers)
                if (p != null)
                    _papers.Add(p);
        _received = new bool[_papers.Count];
        _read = new bool[_papers.Count];
        foreach (StepPaper p in _papers)
            foreach (DocumentField f in p.Fields)
                if (f != null && !_paperCategories.Contains(f.category))
                    _paperCategories.Add(f.category);
        if (questionCategories != null)
            foreach (ClueCategory c in questionCategories)
                if (!_questions.Contains(c))
                    _questions.Add(c);
        _books = new HashSet<ClueCategory>(bookCategories ?? Array.Empty<ClueCategory>());
    }

    /// <summary>The case's papers, in paper order.</summary>
    public IReadOnlyList<StepPaper> Papers => _papers;

    /// <summary>The categories the papers state, each once, in paper and field order.</summary>
    public IReadOnlyList<ClueCategory> PaperCategories => _paperCategories;

    /// <summary>Today's question categories, each once.</summary>
    public IReadOnlyList<ClueCategory> QuestionCategories => _questions;

    /// <summary>The categories the traveller has answered in, in the order heard.</summary>
    public IReadOnlyList<ClueCategory> AnsweredCategories => _asked;

    /// <summary>True when the Rules tab was seen while the traveller was at the desk.</summary>
    public bool ViewedRules { get; private set; }

    /// <summary>True when a record was looked up.</summary>
    public bool ViewedRecord { get; private set; }

    /// <summary>True when a garment was looked at.</summary>
    public bool Looked { get; private set; }

    /// <summary>True when a reference book holds <paramref name="category"/>.</summary>
    public bool IsBookCategory(ClueCategory category) => _books.Contains(category);

    /// <summary>Paper <paramref name="paper"/> was handed over.</summary>
    public bool Received(int paper) => Mark(_received, paper);

    /// <summary>Paper <paramref name="paper"/> was read (examined at the desk, or shown in a pane while the PC is looked at).</summary>
    public bool Read(int paper) => Mark(_read, paper);

    /// <summary>A paper of form <paramref name="kind"/> was asked for.</summary>
    public bool Requested(string kind) => !string.IsNullOrEmpty(kind) && _requested.Add(kind);

    /// <summary>The Rules tab was seen.</summary>
    public bool RulesViewed()
    {
        if (ViewedRules)
            return false;
        ViewedRules = true;
        return true;
    }

    /// <summary>A record was looked up, whatever it found.</summary>
    public bool RecordViewed()
    {
        if (ViewedRecord)
            return false;
        ViewedRecord = true;
        return true;
    }

    /// <summary>A garment was looked at.</summary>
    public bool LookedAt()
    {
        if (Looked)
            return false;
        Looked = true;
        return true;
    }

    /// <summary>A pair was compared: recorded when it checks something (CaseSteps.Classify), whether it showed MATCH or MISMATCH.</summary>
    public bool Compared(CompareEvidence a, CompareEvidence b) =>
        CaseSteps.Classify(a, b, out StatementKind statement, out ClueCategory category, out TruthKind truth) && _compared.Add((statement, category, truth));

    /// <summary>The traveller answered in <paramref name="category"/>.</summary>
    public bool Asked(ClueCategory category)
    {
        if (_asked.Contains(category))
            return false;
        _asked.Add(category);
        return true;
    }

    /// <summary>The player ticked (<paramref name="done"/>) or unticked step <paramref name="id"/> by hand: it holds for the rest of the case.</summary>
    public void SetManual(string id, bool done)
    {
        if (!string.IsNullOrEmpty(id))
            _manual[id] = done;
    }

    /// <summary>True with the hand-set tick of step <paramref name="id"/>, false when the player never set it.</summary>
    public bool TryManual(string id, out bool done)
    {
        done = false;
        return !string.IsNullOrEmpty(id) && _manual.TryGetValue(id, out done);
    }

    /// <summary>True when paper <paramref name="paper"/> was handed over.</summary>
    public bool HasReceived(int paper) => paper >= 0 && paper < _received.Length && _received[paper];

    /// <summary>True when paper <paramref name="paper"/> was read.</summary>
    public bool HasRead(int paper) => paper >= 0 && paper < _read.Length && _read[paper];

    /// <summary>True when a paper of form <paramref name="kind"/> was asked for.</summary>
    public bool WasRequested(string kind) => kind != null && _requested.Contains(kind);

    /// <summary>True when an answer in <paramref name="category"/> was heard.</summary>
    public bool WasAsked(ClueCategory category) => _asked.Contains(category);

    /// <summary>True when a statement of <paramref name="statement"/> (Any: any) in <paramref name="category"/> was compared with a truth of <paramref name="truth"/> (Any: any).</summary>
    public bool WasCompared(StatementKind statement, ClueCategory category, TruthKind truth)
    {
        foreach ((StatementKind s, ClueCategory c, TruthKind t) in _compared)
            if (c == category && (statement == StatementKind.Any || s == statement) && (truth == TruthKind.Any || t == truth))
                return true;
        return false;
    }

    private static bool Mark(bool[] marks, int index)
    {
        if (index < 0 || index >= marks.Length || marks[index])
            return false;
        marks[index] = true;
        return true;
    }
}
