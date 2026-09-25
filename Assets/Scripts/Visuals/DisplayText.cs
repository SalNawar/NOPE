/// <summary>How a text reaches the player: printed on a document, or said aloud.</summary>
public enum TextMedium
{
    /// <summary>Printed (a document value).</summary>
    Written,

    /// <summary>Said (a transcript sentence, the traveller's reply).</summary>
    Spoken
}

/// <summary>
/// The one place displayed text may differ from its canonical value (item 8's
/// translation reveals, piece 9): the scanned document window's values, the
/// transcript's sentences and the traveller's reply in the speech bubble go
/// through it. Never pass its result to CompareController or CompareEvidence:
/// evidence stays canonical.
/// </summary>
public static class DisplayText
{
    /// <summary>The text to show for a canonical string; today the canonical string itself ("" for null).</summary>
    public static string For(string canonical, TextMedium medium) => canonical ?? string.Empty;
}
