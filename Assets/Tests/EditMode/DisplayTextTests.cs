using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The one place displayed text may differ from its canonical value (piece 9
/// T3, T8, R5): plain text shows as it is, untranslated text in its tongue's
/// glyphs, and a flipping text turns into English letter by letter; reduced
/// motion shows the English at the reveal; right-to-left text is shaped; a
/// typed line shows its characters in reading order (audit R2-001).
/// </summary>
public class DisplayTextTests
{
    private const string Greek = "ολμναξπρηστφχψυςάέβγιδζθωκ";
    private const string Arabic = "عكسريقجحاشتنملوبطدفخهزغضءذ";
    private const string Shekel = "Silver shekel (by weight)";

    private static readonly FlipTiming Timing = new FlipTiming();

    private static ForeignText Foreign(string glyphs, bool rtl = false) =>
        new ForeignText(Pseudoscript.ParseTable(glyphs, out _), rtl);

    private static string For(string text, Reveal reveal, bool reduced = false) => DisplayText.For(text, reveal, Timing, reduced);

    /// <summary>The typed form with "[" and "]" around the characters not typed yet.</summary>
    private static string Typed(string text, Reveal reveal, int typed) => DisplayText.Typed(text, reveal, Timing, false, typed, "[", "]");

    [Test]
    public void Plain_ShowsTheCanonicalText_AndNullShowsNothing()
    {
        Assert.AreEqual("Deben", For("Deben", Reveal.Plain));
        Assert.AreEqual(string.Empty, For(null, Reveal.Plain));
        Assert.AreEqual(string.Empty, For(null, Reveal.Untranslated(Foreign(Greek))));
        Assert.AreEqual(0f, DisplayText.Remaining("Deben", Reveal.Plain, Timing, false));
        Assert.IsFalse(DisplayText.ShowsForeign("Deben", Reveal.Plain, Timing, false));
    }

    [Test]
    public void ANullForeignText_BehavesAsPlain()
    {
        Assert.AreEqual(Shekel, For(Shekel, Reveal.Untranslated(null)));
        Assert.AreEqual(Shekel, For(Shekel, Reveal.Flipping(null, 0f)));
        Assert.AreEqual(0f, DisplayText.Remaining(Shekel, Reveal.Untranslated(null), Timing, false));
        Assert.IsFalse(DisplayText.ShowsForeign(Shekel, Reveal.Flipping(null, 0f), Timing, false));
    }

    [Test]
    public void Untranslated_ShowsEveryLettersCell_AndKeepsDigitsAndPunctuation()
    {
        Assert.AreEqual("Βηφδαέ βραταφ (λω ζαηπργ) 1897.", For(Shekel + " 1897.", Reveal.Untranslated(Foreign(Greek))));
        Assert.AreEqual(float.PositiveInfinity, DisplayText.Remaining(Shekel, Reveal.Untranslated(Foreign(Greek)), Timing, false));
        Assert.IsTrue(DisplayText.ShowsForeign(Shekel, Reveal.Untranslated(Foreign(Greek)), Timing, false));
        Assert.IsFalse(DisplayText.ShowsForeign("1897", Reveal.Untranslated(Foreign(Greek)), Timing, false), "no letter, nothing foreign");
        Assert.AreEqual(0, DisplayText.Progress(Shekel, Reveal.Untranslated(Foreign(Greek)), Timing, false));
    }

    [Test]
    public void Flipping_StartsForeign_EndsCanonical_AndIsEnglishFromTheStartMidway()
    {
        ForeignText greek = Foreign(Greek);
        string foreign = For(Shekel, Reveal.Untranslated(greek));
        Assert.AreEqual(foreign, For(Shekel, Reveal.Flipping(greek, 0f)));

        float duration = FlipSequence.Duration(20, Timing);
        Assert.AreEqual(Shekel, For(Shekel, Reveal.Flipping(greek, duration)));
        Assert.AreEqual(0f, DisplayText.Remaining(Shekel, Reveal.Flipping(greek, duration), Timing, false));
        Assert.IsFalse(DisplayText.ShowsForeign(Shekel, Reveal.Flipping(greek, duration), Timing, false));

        // At 0.665 s letters 0-6 ("Silver s") have landed (the seventh at 0.66 s),
        // letters 7-9 are flipping and letter 10 (the 'e' at index 11) has not started.
        string mid = For(Shekel, Reveal.Flipping(greek, 0.665f));
        StringAssert.StartsWith("Silver s", mid);
        Assert.AreNotEqual('h', mid[8], "the eighth letter is on its way");
        Assert.AreEqual(foreign.Substring(11), mid.Substring(11), "the letters that have not started are still foreign");
        Assert.AreEqual(duration - 0.665f, DisplayText.Remaining(Shekel, Reveal.Flipping(greek, 0.665f), Timing, false), 1e-5f);
        Assert.IsTrue(DisplayText.ShowsForeign(Shekel, Reveal.Flipping(greek, 0.665f), Timing, false));
    }

    [Test]
    public void AFlippingLetter_ShowsItsScrambleGlyph()
    {
        ForeignText greek = Foreign(Greek);
        // 'S' (letter 18) at 0.301 s: step 0, cell (18 + 7) % 26 = 25 ('κ'), upper-cased like its letter.
        Assert.AreEqual("Κ", For("S", Reveal.Flipping(greek, 0.301f)));
        // At 0.361 s: step 1, cell (18 + 14) % 26 = 6 ('π').
        Assert.AreEqual("π", For("s", Reveal.Flipping(greek, 0.361f)));
    }

    [Test]
    public void ReducedMotion_ShowsTheCanonicalTextAtTheReveal()
    {
        ForeignText greek = Foreign(Greek);
        Assert.AreEqual(Shekel, For(Shekel, Reveal.Flipping(greek, 0f), true));
        Assert.AreEqual(0f, DisplayText.Remaining(Shekel, Reveal.Flipping(greek, 0f), Timing, true));
        Assert.IsFalse(DisplayText.ShowsForeign(Shekel, Reveal.Flipping(greek, 0f), Timing, true));
        Assert.AreEqual(For(Shekel, Reveal.Untranslated(greek)), For(Shekel, Reveal.Untranslated(greek), true), "reduced motion translates nothing by itself");
    }

    [Test]
    public void Progress_ChangesExactlyWhenTheShownTextDoes()
    {
        ForeignText greek = Foreign(Greek);
        int previousProgress = DisplayText.Progress(Shekel, Reveal.Flipping(greek, 0f), Timing, false);
        string previousText = For(Shekel, Reveal.Flipping(greek, 0f));
        for (float e = 0.0005f; e < 1.3f; e += 0.001f)
        {
            int progress = DisplayText.Progress(Shekel, Reveal.Flipping(greek, e), Timing, false);
            string text = For(Shekel, Reveal.Flipping(greek, e));
            Assert.AreEqual(progress != previousProgress, text != previousText, $"at {e}");
            previousProgress = progress;
            previousText = text;
        }
        Assert.AreEqual(Shekel, previousText);
    }

    [Test]
    public void RightToLeft_IsShaped_AHalfFlippedLineKeepsItsEnglishRun_AndTheSettledLineIsCanonical()
    {
        ForeignText arabic = Foreign(Arabic, true);
        string logical = string.Concat(Shekel.Select(c => Pseudoscript.Cell(c, arabic.Table)));
        Assert.AreEqual(ArabicShaper.ToVisual(logical), For(Shekel, Reveal.Untranslated(arabic)));
        Assert.IsFalse(For(Shekel, Reveal.Untranslated(arabic)).Contains('\0'));

        string half = For(Shekel, Reveal.Flipping(arabic, 0.665f));
        StringAssert.Contains("Silver", half, "the English run keeps its order");
        Assert.AreEqual(Shekel, For(Shekel, Reveal.Flipping(arabic, 5f)));
    }

    /// <summary>
    /// Audit R2-001: an untranslated right-to-left line is shown in visual
    /// order (ArabicShaper reverses it), so typing it out by count showed its
    /// end first. Typed keeps every character in place and hides those whose
    /// source is not typed yet: the line's first letter is its rightmost.
    /// </summary>
    [Test]
    public void Typed_ARightToLeftLine_TypesFromItsRightEnd_InReadingOrder()
    {
        Reveal arabic = Reveal.Untranslated(Foreign(Arabic, true));
        string shown = For("ab cd", arabic);
        Assert.AreEqual(5, shown.Length);
        Assert.IsTrue(DisplayText.ReadsRightToLeft("ab cd", arabic, Timing, false));

        Assert.AreEqual("[" + shown + "]", Typed("ab cd", arabic, 0), "nothing typed, nothing shows");
        Assert.AreEqual("[" + shown.Substring(0, 4) + "]" + shown.Substring(4), Typed("ab cd", arabic, 1), "the first letter is the rightmost");
        Assert.AreEqual("[" + shown.Substring(0, 3) + "]" + shown.Substring(3), Typed("ab cd", arabic, 2));
        Assert.AreEqual("[" + shown.Substring(0, 1) + "]" + shown.Substring(1), Typed("ab cd", arabic, 4));
        Assert.AreEqual(shown, Typed("ab cd", arabic, 5), "typed out, the line is For's");
        Assert.AreEqual(shown, Typed("ab cd", arabic, 99));
    }

    /// <summary>Digits inside a right-to-left line keep their own order, so they type left to right where they stand; the text around them types from the right.</summary>
    [Test]
    public void Typed_DigitsInARightToLeftLine_TypeInTheirOwnOrder()
    {
        Reveal arabic = Reveal.Untranslated(Foreign(Arabic, true));
        string shown = For("ab 12", arabic);
        Assert.AreEqual("12", shown.Substring(0, 2), "the digit run is at the left, in its order");

        Assert.AreEqual("[12]" + shown.Substring(2), Typed("ab 12", arabic, 3), "a, b and the space show; the digits wait");
        Assert.AreEqual("1[2]" + shown.Substring(2), Typed("ab 12", arabic, 4), "the 1 before the 2");
    }

    /// <summary>A lam-alef ligature shows once its lam is typed (one glyph for two letters).</summary>
    [Test]
    public void Typed_ALamAlefLigature_ShowsWithItsLam()
    {
        Reveal arabic = Reveal.Untranslated(Foreign(Arabic, true));
        string shown = For("ni", arabic);
        Assert.AreEqual(1, shown.Length, "'n' and 'i' are lam and alef: one ligature");
        Assert.AreEqual("[" + shown + "]", Typed("ni", arabic, 0));
        Assert.AreEqual(shown, Typed("ni", arabic, 1));
    }

    /// <summary>A left-to-right text types its first characters (the bubble uses the text's visible count for it); a settled or plain text reads left to right.</summary>
    [Test]
    public void Typed_ALeftToRightText_TypesItsFirstCharacters()
    {
        Reveal greek = Reveal.Untranslated(Foreign(Greek));
        string shown = For("Deben", greek);
        Assert.AreEqual(shown.Substring(0, 2) + "[" + shown.Substring(2) + "]", Typed("Deben", greek, 2));
        Assert.AreEqual("Deb[en]", Typed("Deben", Reveal.Plain, 3));
        Assert.IsFalse(DisplayText.ReadsRightToLeft("Deben", greek, Timing, false));
        Assert.IsFalse(DisplayText.ReadsRightToLeft("Deben", Reveal.Plain, Timing, false));
        Assert.IsFalse(DisplayText.ReadsRightToLeft(Shekel, Reveal.Flipping(Foreign(Arabic, true), 5f), Timing, false), "settled into English");
        Assert.IsFalse(DisplayText.ReadsRightToLeft("1897", Reveal.Untranslated(Foreign(Arabic, true)), Timing, false), "no letter, nothing reversed");
        Assert.AreEqual(string.Empty, Typed(null, greek, 0));
    }

    /// <summary>At every step of a flipping right-to-left line, the typed form keeps For's characters in place and shows one more typed source each step.</summary>
    [Test]
    public void Typed_AFlippingRightToLeftLine_KeepsEveryCharacterInPlace_AndGrowsInReadingOrder()
    {
        Reveal half = Reveal.Flipping(Foreign(Arabic, true), 0.665f);
        string shown = For(Shekel, half);
        Assert.IsTrue(DisplayText.ReadsRightToLeft(Shekel, half, Timing, false));
        int visible = -1;
        for (int n = 0; n <= Shekel.Length; n++)
        {
            string typed = Typed(Shekel, half, n);
            Assert.AreEqual(shown, typed.Replace("[", "").Replace("]", ""), $"typed {n}: every character in place");
            int now = VisibleCount(typed);
            Assert.GreaterOrEqual(now, visible, $"typed {n}");
            visible = now;
        }
        Assert.AreEqual(shown.Length, visible);
    }

    private static int VisibleCount(string typed)
    {
        int count = 0;
        bool hidden = false;
        foreach (char c in typed)
        {
            if (c == '[')
                hidden = true;
            else if (c == ']')
                hidden = false;
            else if (!hidden)
                count++;
        }
        return count;
    }

    [Test]
    public void EveryCellStandsForOneCanonicalCharacter()
    {
        ForeignText egyptian = Foreign("𓄿𓃀𓍿𓂧𓇋𓆑𓎼𓉔𓇌𓆓𓎡𓃭𓅓𓈖𓂝𓊪𓈎𓂋𓋴𓏏𓅱𓎛𓏲𓐍𓏭𓊃");
        string shown = For(Shekel, Reveal.Untranslated(egyptian));
        var codePoints = new List<int>();
        for (int i = 0; i < shown.Length; i += char.IsSurrogatePair(shown, i) ? 2 : 1)
            codePoints.Add(char.ConvertToUtf32(shown, i));
        Assert.AreEqual(Shekel.Length, codePoints.Count);
    }
}
