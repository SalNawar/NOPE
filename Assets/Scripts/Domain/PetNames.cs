using System;
using System.Text;

/// <summary>Why a pet's name is refused at the adoption (PetNames.Check); each has its line in ui.strings ("adopt.problem.{value}").</summary>
public enum PetNameProblem
{
    /// <summary>The name is fine.</summary>
    None,

    /// <summary>Nothing but spaces.</summary>
    Empty,

    /// <summary>Longer than the content's limit (home.pet.nameMaxLength).</summary>
    TooLong,

    /// <summary>Something other than letters, spaces, hyphens and apostrophes (digits, symbols, rich-text tags).</summary>
    BadCharacters
}

/// <summary>
/// The adoption's name rule (the Home pet spec PS1): the name the player
/// types is trimmed and its runs of spaces made one, then refused when it is
/// empty, longer than the limit, or holds anything but letters (any script),
/// spaces, hyphens and apostrophes, so it prints safely on the paper and at
/// Home. Pure, so it is tested headless.
/// </summary>
public static class PetNames
{
    /// <summary>The name as it is kept: trimmed, every run of white space one space.</summary>
    public static string Clean(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;
        var sb = new StringBuilder(raw.Length);
        bool space = false;
        foreach (char c in raw.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                if (!space)
                    sb.Append(' ');
                space = true;
                continue;
            }
            sb.Append(c);
            space = false;
        }
        return sb.ToString();
    }

    /// <summary>What is wrong with <paramref name="raw"/> once cleaned (<see cref="Clean"/>), at most <paramref name="maxLength"/> characters (below 1: one); None when it is fine.</summary>
    public static PetNameProblem Check(string raw, int maxLength)
    {
        string name = Clean(raw);
        if (name.Length == 0)
            return PetNameProblem.Empty;
        if (name.Length > Math.Max(1, maxLength))
            return PetNameProblem.TooLong;
        foreach (char c in name)
            if (!char.IsLetter(c) && c != ' ' && c != '-' && c != '\'' && c != '’' && !IsMark(c))
                return PetNameProblem.BadCharacters;
        return PetNameProblem.None;
    }

    /// <summary>A combining mark (an accent or a vowel sign that belongs to the letter before it).</summary>
    private static bool IsMark(char c)
    {
        var category = char.GetUnicodeCategory(c);
        return category == System.Globalization.UnicodeCategory.NonSpacingMark || category == System.Globalization.UnicodeCategory.SpacingCombiningMark;
    }
}
