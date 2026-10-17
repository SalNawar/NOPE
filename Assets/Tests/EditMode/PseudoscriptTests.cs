using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Untranslated text's glyphs (piece 9 T3, R1-R4): a tongue's 26-cell table
/// maps letters a..z case-insensitively (Latin-1 letters folded first, the
/// case kept where the script has one); everything else passes through.
/// </summary>
public class PseudoscriptTests
{
    private const string Greek = "ολμναξπρηστφχψυςάέβγιδζθωκ";
    private const string Chinese = "安文山水衣日月天一地木火金石欧田中大小上乌下口心由王";
    private const string Egyptian = "𓄿𓃀𓍿𓂧𓇋𓆑𓎼𓉔𓇌𓆓𓎡𓃭𓅓𓈖𓂝𓊪𓈎𓂋𓋴𓏏𓅱𓎛𓏲𓐍𓏭𓊃";
    private const string Fallback = "olmnupqrystvwxazbcdfeghjik";

    /// <summary>The starter tables (spec §2.8).</summary>
    private static readonly string[] Starters =
    {
        Egyptian,
        "𒀀𒁀𒆠𒁕𒂊𒉿𒂵𒄩𒄿𒅖𒅗𒆷𒈠𒈾𒌑𒉺𒋡𒊏𒊓𒋫𒌋𒉌𒊒𒆪𒅀𒍝",
        "عكسريقجحاشتنملوبطدفخهزغضءذ",
        Greek,
        "ifghojklumnpqrystvwxazbced",
        "ohjkulmnèpqrstavwxzbecdfig",
        Chinese,
        "あかきくえけこさいしすせそたおちつてとなうにのはやま",
        "wpqrastveyxzbcidfghjoklmun",
        "umnpyqrsæþvƿxzebcðfgihjkol",
        "ᛟᚷᚹᚺᚨᚾᛃᛈᛖᛉᛊᛏᛒᛗᚢᛚᛜᛞᚴᛦᛁᚠᚦᚱᛇᚲ",
        "eklminpqörßtvwuxzbcdüfghäj",
    };

    private static IReadOnlyList<string> Table(string glyphs)
    {
        IReadOnlyList<string> table = Pseudoscript.ParseTable(glyphs, out string problem);
        Assert.IsNotNull(table, problem);
        return table;
    }

    private static string Render(string text, IReadOnlyList<string> table) => string.Concat(text.Select(c => Pseudoscript.Cell(c, table)));

    [Test]
    public void ParseTable_26BmpCells_Parse()
    {
        IReadOnlyList<string> table = Table(Greek);
        Assert.AreEqual(Pseudoscript.TableSize, table.Count);
        Assert.AreEqual("ο", table[0]);
        Assert.AreEqual("κ", table[25]);
    }

    [Test]
    public void ParseTable_26SurrogatePairCells_ParseAs26()
    {
        IReadOnlyList<string> table = Table(Egyptian);
        Assert.AreEqual(26, table.Count);
        Assert.AreEqual("𓄿", table[0]);
        Assert.AreEqual(2, table[0].Length);
    }

    [TestCase(25)]
    [TestCase(27)]
    public void ParseTable_TheWrongCount_Fails(int count)
    {
        string glyphs = new string(Enumerable.Range(0, count).Select(i => (char)('一' + i)).ToArray());
        Assert.IsNull(Pseudoscript.ParseTable(glyphs, out string problem));
        StringAssert.Contains($"{count} cells", problem);
    }

    [Test]
    public void ParseTable_ADuplicateCell_FailsAndIsNamed()
    {
        Assert.IsNull(Pseudoscript.ParseTable("κλμναξπρηστφχψυςάέβγιδζθωκ", out string problem));
        StringAssert.Contains("'κ'", problem);
    }

    [Test]
    public void ParseTable_Whitespace_Fails()
    {
        Assert.IsNull(Pseudoscript.ParseTable("ολμναξπρηστφχψ ςάέβγιδζθωκ", out string problem));
        StringAssert.Contains("whitespace", problem);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void ParseTable_NullOrBlank_Fails(string glyphs)
    {
        Assert.IsNull(Pseudoscript.ParseTable(glyphs, out string problem));
        Assert.IsFalse(string.IsNullOrEmpty(problem));
    }

    [Test]
    public void ParseTable_ALoneSurrogate_Fails()
    {
        Assert.IsNull(Pseudoscript.ParseTable("\ud80c" + Greek.Substring(1), out string problem));
        Assert.IsFalse(string.IsNullOrEmpty(problem));
    }

    [TestCase('a', 0)]
    [TestCase('A', 0)]
    [TestCase('z', 25)]
    [TestCase('Z', 25)]
    [TestCase('é', 4)]
    [TestCase('É', 4)]
    [TestCase('ä', 0)]
    [TestCase('ç', 2)]
    [TestCase('ñ', 13)]
    [TestCase('ø', 14)]
    [TestCase('ÿ', 24)]
    [TestCase('ß', 18)]
    [TestCase('7', -1)]
    [TestCase(' ', -1)]
    [TestCase('(', -1)]
    [TestCase('£', -1)]
    [TestCase('¥', -1)]
    [TestCase('κ', -1)]
    public void LetterIndex_FoldsLatin1_AndIgnoresTheRest(char c, int expected)
    {
        Assert.AreEqual(expected, Pseudoscript.LetterIndex(c));
    }

    [Test]
    public void Cell_IsCaseInsensitive_AndKeepsTheCaseWhereTheScriptHasOne()
    {
        IReadOnlyList<string> greek = Table(Greek);
        Assert.AreEqual("β", Pseudoscript.Cell('s', greek));
        Assert.AreEqual("Β", Pseudoscript.Cell('S', greek), "an upper-case letter upper-cases a Greek cell");

        IReadOnlyList<string> han = Table(Chinese);
        Assert.AreEqual(Pseudoscript.Cell('s', han), Pseudoscript.Cell('S', han), "Han has no case");

        IReadOnlyList<string> egyptian = Table(Egyptian);
        Assert.AreEqual("𓄿", Pseudoscript.Cell('A', egyptian), "a surrogate-pair cell is never case-mapped");
    }

    [Test]
    public void Cell_NonLettersAndNonDigitsPassThrough()
    {
        IReadOnlyList<string> han = Table(Chinese);
        foreach (char c in " ().,;:'-&£¥")
            Assert.AreEqual(c.ToString(), Pseudoscript.Cell(c, han), c.ToString());
    }

    /// <summary>Saleh 2026-10-17: a digit outside the key words is a glyph too, each digit its own cell (7d + 3 mod 26), so an untranslated date is not understood.</summary>
    [Test]
    public void Cell_EachDigitHasItsOwnCell()
    {
        IReadOnlyList<string> han = Table(Chinese);
        var cells = new HashSet<string>();
        for (char c = '0'; c <= '9'; c++)
        {
            Assert.AreEqual(((c - '0') * 7 + 3) % 26, Pseudoscript.GlyphIndex(c));
            Assert.AreEqual(han[Pseudoscript.GlyphIndex(c)], Pseudoscript.Cell(c, han));
            cells.Add(Pseudoscript.Cell(c, han));
        }
        Assert.AreEqual(10, cells.Count, "ten digits, ten cells");
        Assert.AreEqual(-1, Pseudoscript.GlyphIndex('.'));
        Assert.AreEqual(Pseudoscript.LetterIndex('q'), Pseudoscript.GlyphIndex('q'));
    }

    [Test]
    public void EveryStarterTable_Parses_AndTheFallbackIsAsciiLetters()
    {
        foreach (string glyphs in Starters)
            Assert.AreEqual(26, Table(glyphs).Count, glyphs);
        Assert.IsTrue(Pseudoscript.IsAsciiLetters(Table(Fallback)));
        Assert.IsFalse(Pseudoscript.IsAsciiLetters(Table(Greek)));
        Assert.IsFalse(Pseudoscript.IsAsciiLetters(Table("ifghojklumnpqrystvwxazbceD")), "upper case is not the fallback's");
        Assert.IsFalse(Pseudoscript.IsAsciiLetters(null));
    }

    [Test]
    public void TheEgyptianForm_OfAShekel_IsTheSpecsExample()
    {
        Assert.AreEqual("𓋴𓇌𓃭𓎛𓇋𓂋 𓋴𓉔𓇋𓎡𓇋𓃭 (𓃀𓏭 𓏲𓇋𓇌𓎼𓉔𓏏)", Render("Silver shekel (by weight)", Table(Egyptian)));
        Assert.AreEqual("Βηφδαέ βραταφ (λω ζαηπργ)", Render("Silver shekel (by weight)", Table(Greek)));
        Assert.AreEqual("小一火下衣大 小天衣木衣火 (文由 口衣一月天上)", Render("Silver shekel (by weight)", Table(Chinese)));
    }

    /// <summary>The key words (the traveller-types spec's §8.1): a letter or digit inside an English span is never a glyph; one outside is; nothing else ever is.</summary>
    [Test]
    public void IsGlyph_ALetterOutsideTheEnglishSpans()
    {
        const string line = "Go home, 12 x";
        var english = new List<(int start, int length)> { (3, 4) };
        bool[] glyphs = Enumerable.Range(0, line.Length).Select(i => Pseudoscript.IsGlyph(line, i, english)).ToArray();
        CollectionAssert.AreEqual(new[] { true, true, false, false, false, false, false, false, false, true, true, false, true }, glyphs,
            "G o | the space | h o m e (kept) | , space (no glyphs) | 1 2 (digits are glyphs) | space | x");
        Assert.IsTrue(Pseudoscript.IsGlyph(line, 3, null), "no spans: every letter is a glyph");
        Assert.IsFalse(Pseudoscript.IsGlyph(line, 99, null), "past the end");
        Assert.IsFalse(Pseudoscript.IsGlyph(null, 0, null));
    }

    [Test]
    public void ADifferentTable_GivesADifferentString()
    {
        Assert.AreNotEqual(Render("Deben (copper, by weight)", Table(Greek)), Render("Deben (copper, by weight)", Table(Fallback)));
    }
}
