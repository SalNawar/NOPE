using System.Collections.Generic;

/// <summary>
/// The traveller's speech bubble as a queue of lines, paced in time: each line
/// types out at a set speed; once fully shown it stays the hold when nothing
/// follows, or the minimum (never more than the hold) before the next queued
/// line replaces it. A new line never cuts the one being shown before its
/// minimum. Each line may carry a premade's expression and a reveal time (its
/// translation flip, piece 9): it counts as fully shown at the later of its
/// typing and its reveal, and EndReveal ends the reveal early. Pure, so the
/// pacing is tested headless; TravellerWheel ticks it and draws it.
/// </summary>
public sealed class SpeechQueue
{
    private readonly float _charsPerSecond;
    private readonly float _minSeconds;
    private readonly float _holdSeconds;
    private readonly Queue<(string text, string expression, float reveal)> _waiting = new Queue<(string text, string expression, float reveal)>();
    private float _elapsed;

    /// <summary>Seconds from the shown line's start until its reveal is done.</summary>
    private float _reveal;

    /// <summary>
    /// A queue typing <paramref name="charsPerSecond"/> characters a second (0
    /// shows a line at once), holding a lone line <paramref name="holdSeconds"/>
    /// once fully shown and a followed one <paramref name="minSeconds"/>.
    /// Negative values count as 0; a minimum above the hold counts as the hold.
    /// </summary>
    public SpeechQueue(float charsPerSecond, float minSeconds, float holdSeconds)
    {
        _charsPerSecond = charsPerSecond > 0f ? charsPerSecond : 0f;
        _holdSeconds = holdSeconds > 0f ? holdSeconds : 0f;
        _minSeconds = minSeconds > 0f ? (minSeconds < _holdSeconds ? minSeconds : _holdSeconds) : 0f;
    }

    /// <summary>True while a line is up.</summary>
    public bool Showing => Text != null;

    /// <summary>The line being shown (all of it), or null.</summary>
    public string Text { get; private set; }

    /// <summary>How many of the shown line's characters are typed out so far (0 when nothing shows).</summary>
    public int VisibleCharacters
    {
        get
        {
            if (Text == null)
                return 0;
            if (_charsPerSecond <= 0f || _elapsed >= TypeSeconds)
                return Text.Length;
            return (int)(_elapsed * _charsPerSecond);
        }
    }

    /// <summary>How many lines have started so far (never counts back): a change means a new line is up.</summary>
    public int LineNumber { get; private set; }

    /// <summary>The expression of the latest started line that carries one (a blank one keeps it); null before any, and after <see cref="Clear"/>.</summary>
    public string Expression { get; private set; }

    /// <summary>Seconds since the shown line started (0 when nothing shows): the clock of its translation flip.</summary>
    public float LineSeconds => Text != null ? _elapsed : 0f;

    /// <summary>Seconds the shown line takes to type out.</summary>
    private float TypeSeconds => _charsPerSecond > 0f && Text != null ? Text.Length / _charsPerSecond : 0f;

    /// <summary>When the shown line goes, from its start: once typed and revealed, the minimum when a line waits, else the hold.</summary>
    private float EndSeconds => (TypeSeconds > _reveal ? TypeSeconds : _reveal) + (_waiting.Count > 0 ? _minSeconds : _holdSeconds);

    /// <summary>
    /// Queues a line (a blank one is ignored) that counts as fully shown
    /// <paramref name="revealSeconds"/> after it starts if its typing ends
    /// sooner (negative or NaN counts as 0). It starts at once when nothing
    /// shows, or when the shown line has already been up its minimum.
    /// </summary>
    public void Say(string text, string expression, float revealSeconds = 0f)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _waiting.Enqueue((text, expression, revealSeconds > 0f ? revealSeconds : 0f));
        if (Text == null || _elapsed >= EndSeconds)
            StartNext(0f);
    }

    /// <summary>The shown line's reveal counts as done now: its hold starts at the later of now and the end of its typing (the skip).</summary>
    public void EndReveal()
    {
        if (Text != null && _reveal > _elapsed)
            _reveal = _elapsed;
    }

    /// <summary>Advances time by <paramref name="seconds"/> (non-positive or NaN is ignored), starting queued lines as their turn comes; one tick may pass several lines.</summary>
    public void Tick(float seconds)
    {
        if (!(seconds > 0f) || Text == null)
            return;

        _elapsed += seconds;
        while (Text != null && _elapsed >= EndSeconds)
        {
            float leftover = _elapsed - EndSeconds;
            if (_waiting.Count > 0)
            {
                StartNext(leftover);
            }
            else
            {
                Text = null;
                _elapsed = 0f;
            }
        }
    }

    /// <summary>
    /// Drops the shown line and every queued one. Returns the expression of the
    /// last queued line that carries one (null when none), so the caller can
    /// show at once the face the traveller would have ended on; <see cref="Expression"/>
    /// becomes null.
    /// </summary>
    public string Clear()
    {
        string pending = null;
        foreach (var (_, expression, _) in _waiting)
            if (!string.IsNullOrWhiteSpace(expression))
                pending = expression;

        _waiting.Clear();
        Text = null;
        _elapsed = 0f;
        _reveal = 0f;
        Expression = null;
        return pending;
    }

    /// <summary>Puts the next queued line up, <paramref name="elapsed"/> seconds into it.</summary>
    private void StartNext(float elapsed)
    {
        (string text, string expression, float reveal) = _waiting.Dequeue();
        Text = text;
        _elapsed = elapsed;
        _reveal = reveal;
        LineNumber++;
        if (!string.IsNullOrWhiteSpace(expression))
            Expression = expression;
    }
}
