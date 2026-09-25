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
/// composed in logical order and shaped by ArabicShaper. Never pass its
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
    public static string For(string canonical, Reveal reveal, FlipTiming timing, bool reducedMotion)
    {
        if (canonical == null)
            return string.Empty;
        if (IsPlain(reveal) || IsSettled(canonical, reveal, timing, reducedMotion))
            return canonical;

        IReadOnlyList<string> table = reveal.Foreign.Table;
        bool flipping = reveal.Kind == RevealKind.Flipping && !reducedMotion;
        var sb = new StringBuilder(canonical.Length * 2);
        int rank = 0;
        foreach (char c in canonical)
        {
            int letter = Pseudoscript.LetterIndex(c);
            if (letter < 0)
            {
                sb.Append(c);
                continue;
            }

            int step = 0;
            CellState state = flipping ? FlipSequence.StateAt(rank, reveal.Row, timing ?? DefaultTiming, reveal.Elapsed, out step) : CellState.Foreign;
            if (state == CellState.English)
                sb.Append(c);
            else if (state == CellState.Flipping)
                sb.Append(Pseudoscript.Cell(c, table, FlipSequence.ScrambleIndex(letter, step)));
            else
                sb.Append(Pseudoscript.Cell(c, table));
            rank++;
        }

        string logical = sb.ToString();
        return reveal.Foreign.RightToLeft ? ArabicShaper.ToVisual(logical) : logical;
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
