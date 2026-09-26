using System.Collections.Generic;

/// <summary>
/// Untranslated text's glyphs (piece 9 T3): a tongue's authored table of 26
/// cells stands for the letters a..z. Letters map case-insensitively after
/// Latin-1 letters are folded to their base letter; an upper-case letter
/// upper-cases a one-character cell (Greek β becomes Β; scripts without case
/// are unchanged); digits, spaces, punctuation and symbols pass through. One
/// cell per canonical character, so the flip can turn cell i into character
/// i. A line's English spans (its key words, the traveller-types spec's
/// §8.1) are never glyphs: IsGlyph says which characters are. The same text
/// always gives the same glyphs: no random stream.
/// </summary>
public static class Pseudoscript
{
    /// <summary>Cells in a table: one per letter a..z.</summary>
    public const int TableSize = 26;

    /// <summary>Latin-1 letters and their base letter (spec R2).</summary>
    private const string Folded = "àáâãäåçèéêëìíîïñòóôõöøùúûüýÿßÀÁÂÃÄÅÇÈÉÊËÌÍÎÏÑÒÓÔÕÖØÙÚÛÜÝ";

    /// <summary>The base letter of each <see cref="Folded"/> character.</summary>
    private const string Base = "aaaaaaceeeeiiiinoooooouuuuyysaaaaaaceeeeiiiinoooooouuuuy";

    /// <summary>
    /// The table's cells (a surrogate pair is one cell), or null with the
    /// <paramref name="problem"/> when it breaks a rule: exactly 26 cells, all
    /// distinct, no whitespace, no lone surrogate.
    /// </summary>
    public static IReadOnlyList<string> ParseTable(string glyphs, out string problem)
    {
        problem = null;
        if (string.IsNullOrWhiteSpace(glyphs))
        {
            problem = "the table is blank";
            return null;
        }

        var cells = new List<string>(TableSize);
        for (int i = 0; i < glyphs.Length; i++)
        {
            char c = glyphs[i];
            if (char.IsWhiteSpace(c))
            {
                problem = "whitespace in the table";
                return null;
            }
            if (char.IsHighSurrogate(c) && i + 1 < glyphs.Length && char.IsLowSurrogate(glyphs[i + 1]))
            {
                cells.Add(glyphs.Substring(i, 2));
                i++;
            }
            else if (char.IsSurrogate(c))
            {
                problem = $"a lone surrogate at position {i}";
                return null;
            }
            else
            {
                cells.Add(c.ToString());
            }
        }

        if (cells.Count != TableSize)
        {
            problem = $"{cells.Count} cells, need {TableSize}";
            return null;
        }

        var seen = new HashSet<string>();
        foreach (string cell in cells)
        {
            if (!seen.Add(cell))
            {
                problem = $"cell '{cell}' appears twice";
                return null;
            }
        }
        return cells;
    }

    /// <summary>0..25 for a..z and A..Z and for the Latin-1 letters after folding; -1 for anything else.</summary>
    public static int LetterIndex(char c)
    {
        int folded = Folded.IndexOf(c);
        char letter = folded >= 0 ? Base[folded] : char.ToLowerInvariant(c);
        return letter >= 'a' && letter <= 'z' ? letter - 'a' : -1;
    }

    /// <summary>The cell a character shows as: its letter's cell (upper-cased for an upper-case letter when the cell is one character), else the character itself.</summary>
    public static string Cell(char c, IReadOnlyList<string> table) => Cell(c, table, LetterIndex(c));

    /// <summary>Cell <paramref name="index"/> shown in place of letter <paramref name="c"/>, in the letter's case (the flip's scramble glyphs); the character itself when it is not a letter or the index or table is out of range.</summary>
    public static string Cell(char c, IReadOnlyList<string> table, int index)
    {
        if (LetterIndex(c) < 0 || table == null || table.Count != TableSize || index < 0 || index >= TableSize)
            return c.ToString();

        string cell = table[index];
        return char.IsUpper(c) && cell.Length == 1 ? char.ToUpperInvariant(cell[0]).ToString() : cell;
    }

    /// <summary>
    /// True when character <paramref name="index"/> of <paramref name="text"/>
    /// shows as a glyph: a letter (LetterIndex) outside every span of
    /// <paramref name="english"/> (start, length; null: none), which stay
    /// English. False for anything else and for an index outside the text.
    /// </summary>
    public static bool IsGlyph(string text, int index, IReadOnlyList<(int start, int length)> english)
    {
        if (text == null || index < 0 || index >= text.Length || LetterIndex(text[index]) < 0)
            return false;
        for (int i = 0; english != null && i < english.Count; i++)
            if (index >= english[i].start && index < english[i].start + english[i].length)
                return false;
        return true;
    }

    /// <summary>True when the table's 26 cells are distinct lower-case ASCII letters (the fallback cipher's rule).</summary>
    public static bool IsAsciiLetters(IReadOnlyList<string> table)
    {
        if (table == null || table.Count != TableSize)
            return false;

        var seen = new HashSet<char>();
        foreach (string cell in table)
            if (cell == null || cell.Length != 1 || cell[0] < 'a' || cell[0] > 'z' || !seen.Add(cell[0]))
                return false;
        return true;
    }
}
