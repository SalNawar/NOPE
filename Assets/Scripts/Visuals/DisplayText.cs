using System.Collections.Generic;
using System.Text;

/// <summary>One tongue's look: its parsed glyph table (Pseudoscript) and its direction.</summary>
public sealed class ForeignText
{
    /// <summary>A tongue's look from its 26-cell table and whether its script reads right to left.</summary>
    public ForeignText(IReadOnlyList<string> table, bool rightToLeft)
    {
        Table = table;
        RightToLeft = rightToLeft;
    }

    /// <summary>The 26 cells for a..z.</summary>
    public IReadOnlyList<string> Table { get; }

    /// <summary>True for a right-to-left script (shaped through ArabicShaper).</summary>
    public bool RightToLeft { get; }
}

/// <summary>How a text is revealed.</summary>
public enum RevealKind
{
    /// <summary>As it is (canonical).</summary>
    Plain,

    /// <summary>In its tongue's glyphs.</summary>
    Untranslated,

    /// <summary>Flipping from its tongue's glyphs into English.</summary>
    Flipping
}

/// <summary>How a text shows: plain (canonical), untranslated (the foreign form), or flipping from the foreign form to canonical.</summary>
public readonly struct Reveal
{
    private Reveal(RevealKind kind, ForeignText foreign, float elapsed, int row)
    {
        Kind = kind;
        Foreign = foreign;
        Elapsed = elapsed;
        Row = row;
    }

    /// <summary>The canonical text as it is.</summary>
    public static Reveal Plain => default;

    /// <summary>The text in a tongue's glyphs.</summary>
    public static Reveal Untranslated(ForeignText foreign) => new Reveal(RevealKind.Untranslated, foreign, 0f, 0);

    /// <summary>The text <paramref name="elapsed"/> seconds into its flip, as document row <paramref name="row"/> (0 for speech).</summary>
    public static Reveal Flipping(ForeignText foreign, float elapsed, int row) => new Reveal(RevealKind.Flipping, foreign, elapsed, row);

    /// <summary>Plain, untranslated or flipping.</summary>
    public RevealKind Kind { get; }

    /// <summary>The tongue's look (null reads as plain).</summary>
    public ForeignText Foreign { get; }

    /// <summary>Seconds since the reveal (flipping only).</summary>
    public float Elapsed { get; }

    /// <summary>The document row, which delays its flip (flipping only).</summary>
    public int Row { get; }
}

/// <summary>
/// The one place displayed text may differ from its canonical value (piece 9
/// T3, T8): the scanned document window's values, the transcript's sentences
/// and the traveller's line in the speech bubble go through it. Plain text
/// shows as it is; untranslated text shows each letter as its tongue's cell
/// (Pseudoscript); a flipping text turns into English letter by letter
/// (FlipSequence), each letter passing through scramble glyphs of its tongue;
/// reduced motion shows the English from the reveal. Right-to-left text is
/// composed in logical order and shaped by ArabicShaper; a line typing out
/// shows its characters in reading order (Typed). Never pass its
/// result to CompareController or CompareEvidence: evidence stays canonical.
/// </summary>
public static class DisplayText
{
    /// <summary>The default knobs (FlipTiming's field defaults), used when a caller passes none.</summary>
    private static readonly FlipTiming DefaultTiming = new FlipTiming();

    /// <summary>
    /// The text to show ("" for null). Plain, or a null Foreign: the canonical
    /// text. Untranslated: every letter's cell. Flipping: per FlipSequence
    /// (reduced motion: the canonical text once elapsed is at least 0, the
    /// foreign form before). Right to left: the composed logical string
    /// through ArabicShaper.ToVisual.
    /// </summary>
    public static string For(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion) =>
        Compose(canonical, reveal, timing, reducedMotion, null);

    /// <summary>
    /// True when For's text reads right to left: a right-to-left tongue's text
    /// that still shows a foreign or scramble cell. It is then in visual order
    /// (ArabicShaper), so typing it out cannot show its first characters by a
    /// count from the left: Typed shows them in reading order.
    /// </summary>
    public static bool ReadsRightToLeft(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion) =>
        ShowsForeign(canonical, reveal, timing, reducedMotion) && reveal.Foreign.RightToLeft;

    /// <summary>
    /// For's text <paramref name="typed"/> characters into typing the line out,
    /// in reading order (audit R2-001: an untranslated right-to-left line typed
    /// out from its end): every character keeps its place, and those whose
    /// source is not among the first <paramref name="typed"/> characters of
    /// <paramref name="canonical"/> are wrapped in <paramref name="hideOpen"/>
    /// and <paramref name="hideClose"/> (the caller's invisible markup). A
    /// right-to-left text's first letter is its rightmost (digits and English
    /// inside it keep their own order); a left-to-right text shows its first
    /// characters. Typed to the end (or past it), it is For's text.
    /// </summary>
    public static string Typed(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion, int typed, string hideOpen, string hideClose)
    {
        var sources = new List<int>();
        string shown = Compose(canonical, reveal, timing, reducedMotion, sources);
        if (canonical == null || typed >= canonical.Length)
            return shown;

        var sb = new StringBuilder(shown.Length + 2 * ((hideOpen?.Length ?? 0) + (hideClose?.Length ?? 0)));
        bool hidden = false;
        for (int i = 0; i < shown.Length; i++)
        {
            bool hide = sources[i] >= typed;
            if (hide != hidden)
            {
                sb.Append(hide ? hideOpen : hideClose);
                hidden = hide;
            }
            sb.Append(shown[i]);
        }
        if (hidden)
            sb.Append(hideClose);
        return sb.ToString();
    }

    /// <summary>For's text; <paramref name="sources"/> (null skips it) receives, per character of it, the index in <paramref name="canonical"/> of the character it shows.</summary>
    private static string Compose(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion, List<int> sources)
    {
        if (canonical == null)
            return string.Empty;
        if (IsPlain(reveal) || IsSettled(canonical, reveal, timing, reducedMotion))
        {
            for (int k = 0; sources != null && k < canonical.Length; k++)
                sources.Add(k);
            return canonical;
        }

        List<int> logicalSources = sources != null ? new List<int>(canonical.Length * 2) : null;
        string logical = Logical(canonical, reveal, timing, reducedMotion, logicalSources);
        if (!reveal.Foreign.RightToLeft)
        {
            sources?.AddRange(logicalSources);
            return logical;
        }
        if (sources == null)
            return ArabicShaper.ToVisual(logical);

        var visualSources = new List<int>(logical.Length);
        string visual = ArabicShaper.ToVisual(logical, visualSources);
        foreach (int i in visualSources)
            sources.Add(logicalSources[i]);
        return visual;
    }

    /// <summary>
    /// The composed text in logical order: each letter as its tongue's cell,
    /// its scramble glyph while flipping, or English once landed; everything
    /// else as it is. <paramref name="sources"/> (null skips it) receives each
    /// character's index in <paramref name="canonical"/>.
    /// </summary>
    private static string Logical(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion, List<int> sources)
    {
        IReadOnlyList<string> table = reveal.Foreign.Table;
        bool flipping = reveal.Kind == RevealKind.Flipping && !reducedMotion;
        var sb = new StringBuilder(canonical.Length * 2);
        int rank = 0;
        for (int k = 0; k < canonical.Length; k++)
        {
            char c = canonical[k];
            int letter = Pseudoscript.LetterIndex(c);
            if (letter < 0)
            {
                sb.Append(c);
            }
            else
            {
                int step = 0;
                CellState state = flipping ? FlipSequence.StateAt(rank, reveal.Row, timing ?? DefaultTiming, reveal.Elapsed, out step) : CellState.Foreign;
                AppendLetter(sb, c, letter, table, state, step);
                rank++;
            }

            while (sources != null && sources.Count < sb.Length)
                sources.Add(k);
        }
        return sb.ToString();
    }

    /// <summary>Appends one letter as its state shows it: English, a scramble glyph at <paramref name="step"/>, or its tongue's cell.</summary>
    private static void AppendLetter(StringBuilder sb, char c, int letter, IReadOnlyList<string> table, CellState state, int step)
    {
        if (state == CellState.English)
            sb.Append(c);
        else if (state == CellState.Flipping)
            sb.Append(Pseudoscript.Cell(c, table, FlipSequence.ScrambleIndex(letter, step)));
        else
            sb.Append(Pseudoscript.Cell(c, table));
    }

    /// <summary>Seconds until the text settles: 0 when plain or settled, +infinity when untranslated (an unrevealed NaN time counts as not started).</summary>
    public static float Remaining(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion)
    {
        int letters = Letters(canonical);
        if (IsPlain(reveal) || letters == 0)
            return 0f;
        if (reveal.Kind == RevealKind.Untranslated)
            return float.PositiveInfinity;

        float end = reducedMotion ? 0f : FlipSequence.Duration(letters, reveal.Row, timing ?? DefaultTiming);
        if (float.IsNaN(reveal.Elapsed))
            return end;
        return reveal.Elapsed >= end ? 0f : end - reveal.Elapsed;
    }

    /// <summary>FlipSequence.Progress for this text (0 for plain and untranslated; reduced motion: 1 once revealed): recompose only when it changes.</summary>
    public static int Progress(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion)
    {
        if (IsPlain(reveal) || reveal.Kind != RevealKind.Flipping)
            return 0;
        if (reducedMotion)
            return reveal.Elapsed >= 0f ? 1 : 0;
        return FlipSequence.Progress(Letters(canonical), reveal.Row, timing ?? DefaultTiming, reveal.Elapsed);
    }

    /// <summary>True while any foreign or scramble cell shows (the text then needs its script's font).</summary>
    public static bool ShowsForeign(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion) =>
        !IsPlain(reveal) && Letters(canonical) > 0 && !IsSettled(canonical, reveal, timing, reducedMotion);

    /// <summary>Plain, or no tongue to show.</summary>
    private static bool IsPlain(Reveal reveal) => reveal.Kind == RevealKind.Plain || reveal.Foreign?.Table == null;

    /// <summary>A flipping text, revealed, whose every letter has landed.</summary>
    private static bool IsSettled(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion) =>
        reveal.Kind == RevealKind.Flipping && !float.IsNaN(reveal.Elapsed) && Remaining(canonical, reveal, timing, reducedMotion) <= 0f;

    /// <summary>How many letters the text holds (only letters flip and take time).</summary>
    private static int Letters(string canonical)
    {
        int letters = 0;
        foreach (char c in canonical ?? string.Empty)
            if (Pseudoscript.LetterIndex(c) >= 0)
                letters++;
        return letters;
    }
}
