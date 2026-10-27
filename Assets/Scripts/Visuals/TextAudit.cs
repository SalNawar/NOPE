using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The text checks TEXT-LANG's verification reads a screen's text dump with
/// (FIX_PLAN TEXT-LANG: the language takeover grep, BUG-3a; the Greek
/// capitals scan, BUG-38a / UI-19): which player-language texts hold a
/// culture label, and which Greek capitals keep an accent. Pure, so the scans
/// are tested headless (TextAuditTests); TextLangAudit runs them on the
/// scenes and a play's texts.
/// </summary>
public static class TextAudit
{
    /// <summary>The Greek capitals with a tonos or a dialytika (Ά Έ Ή Ί Ό Ύ Ώ Ϊ Ϋ): ArtLayout.Capitals drops the accent, TMP's UpperCase keeps it.</summary>
    private const string AccentedCapitals = "ΆΈΉΊΌΎΏΪΫ";

    /// <summary>The accented Greek capitals in <paramref name="text"/>, in order (empty for none).</summary>
    public static List<char> AccentedGreekCapitals(string text) =>
        (text ?? string.Empty).Where(c => AccentedCapitals.IndexOf(c) >= 0).ToList();

    /// <summary>
    /// The texts shown on a surface that reads the player's language (the
    /// chrome, the PC: CultureChoice.FollowsWorld false) that hold a culture
    /// label: one of <paramref name="cultureLabels"/> (each a culture string
    /// that differs from its English, at least <paramref name="shortest"/>
    /// characters, so a word both languages share never counts). BUG-3a's
    /// pass: none.
    /// </summary>
    public static List<string> CultureOnPlayerSurfaces(IEnumerable<(string text, TextSurface surface)> shown, IEnumerable<(string culture, string english)> cultureLabels, int shortest = 3)
    {
        List<string> labels = (cultureLabels ?? Array.Empty<(string, string)>())
                              .Where(l => !string.IsNullOrWhiteSpace(l.culture) && l.culture.Trim().Length >= shortest && !string.Equals(l.culture, l.english, StringComparison.Ordinal))
                              .Select(l => l.culture.Trim()).Distinct().ToList();
        var found = new List<string>();
        foreach ((string text, TextSurface surface) in shown ?? Array.Empty<(string, TextSurface)>())
        {
            if (string.IsNullOrEmpty(text) || CultureChoice.FollowsWorld(surface))
                continue;
            string hit = labels.FirstOrDefault(l => text.IndexOf(l, StringComparison.Ordinal) >= 0);
            if (hit != null)
                found.Add($"{surface}: \"{text}\" holds \"{hit}\"");
        }
        return found;
    }
}
