using System;

/// <summary>
/// The word a dater's rubber prints (FLOW, BUG-38b: the daters printed no
/// word under the Greek, Arabic, Japanese and Chinese eras). The rubber is
/// bureau equipment, cut in the bureau's English: it prints the verdict word
/// it is given when every letter of it can print (the rubber's own slab
/// capitals, or the date font's glyphs), and otherwise its English verdict
/// word, APPROVED or DENIED, so an impression always carries its word. Pure;
/// DaterImpressionArt asks it before painting.
/// </summary>
public static class DaterRubber
{
    /// <summary>The rubber's English verdict words.</summary>
    public const string Approved = "APPROVED", Denied = "DENIED";

    /// <summary>
    /// The word to print for <paramref name="word"/> (upper case, as the
    /// rubber cuts it): itself when it has a letter and every letter but a
    /// space is <paramref name="printable"/>, else <see cref="Approved"/> or
    /// <see cref="Denied"/> by <paramref name="approved"/>.
    /// </summary>
    public static string Word(string word, bool approved, Func<char, bool> printable)
    {
        string upper = (word ?? string.Empty).Trim().ToUpperInvariant();
        bool any = false, all = true;
        foreach (char c in upper)
        {
            if (c == ' ')
                continue;
            any = true;
            if (printable == null || !printable(c))
            {
                all = false;
                break;
            }
        }
        return any && all ? upper : approved ? Approved : Denied;
    }
}
