using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The one place displayed text may differ from its canonical value (piece 9
/// T3, T8, R5): plain text shows as it is, untranslated text in its tongue's
/// glyphs, and a flipping text turns into English letter by letter; reduced
/// motion shows the English at the reveal; right-to-left text is shaped.
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
        Assert.AreEqual(Shekel, For(Shekel, Reveal.Flipping(null, 0f, 0)));
        Assert.AreEqual(0f, DisplayText.Remaining(Shekel, Reveal.Untranslated(null), Timing, false));
        Assert.IsFalse(DisplayText.ShowsForeign(Shekel, Reveal.Flipping(null, 0f, 0), Timing, false));
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
        Assert.AreEqual(foreign, For(Shekel, Reveal.Flipping(greek, 0f, 0)));

        float duration = FlipSequence.Duration(20, 0, Timing);
        Assert.AreEqual(Shekel, For(Shekel, Reveal.Flipping(greek, duration, 0)));
        Assert.AreEqual(0f, DisplayText.Remaining(Shekel, Reveal.Flipping(greek, duration, 0), Timing, false));
        Assert.IsFalse(DisplayText.ShowsForeign(Shekel, Reveal.Flipping(greek, duration, 0), Timing, false));

        // At 0.665 s letters 0-6 ("Silver s") have landed (the seventh at 0.66 s),
        // letters 7-9 are flipping and letter 10 (the 'e' at index 11) has not started.
        string mid = For(Shekel, Reveal.Flipping(greek, 0.665f, 0));
        StringAssert.StartsWith("Silver s", mid);
        Assert.AreNotEqual('h', mid[8], "the eighth letter is on its way");
        Assert.AreEqual(foreign.Substring(11), mid.Substring(11), "the letters that have not started are still foreign");
        Assert.AreEqual(duration - 0.665f, DisplayText.Remaining(Shekel, Reveal.Flipping(greek, 0.665f, 0), Timing, false), 1e-5f);
        Assert.IsTrue(DisplayText.ShowsForeign(Shekel, Reveal.Flipping(greek, 0.665f, 0), Timing, false));
    }

    [Test]
    public void AFlippingLetter_ShowsItsScrambleGlyph()
    {
        ForeignText greek = Foreign(Greek);
        // 'S' (letter 18) at 0.301 s: step 0, cell (18 + 7) % 26 = 25 ('κ'), upper-cased like its letter.
        Assert.AreEqual("Κ", For("S", Reveal.Flipping(greek, 0.301f, 0)));
        // At 0.361 s: step 1, cell (18 + 14) % 26 = 6 ('π').
        Assert.AreEqual("π", For("s", Reveal.Flipping(greek, 0.361f, 0)));
    }

    [Test]
    public void ARowStartsLater()
    {
        ForeignText greek = Foreign(Greek);
        Assert.AreEqual(For("Deben", Reveal.Untranslated(greek)), For("Deben", Reveal.Flipping(greek, 0.44f, 1)));
        Assert.AreEqual(FlipSequence.Duration(5, 1, Timing), DisplayText.Remaining("Deben", Reveal.Flipping(greek, 0f, 1), Timing, false), 1e-5f);
    }

    [Test]
    public void ReducedMotion_ShowsTheCanonicalTextAtTheReveal()
    {
        ForeignText greek = Foreign(Greek);
        Assert.AreEqual(Shekel, For(Shekel, Reveal.Flipping(greek, 0f, 0), true));
        Assert.AreEqual(0f, DisplayText.Remaining(Shekel, Reveal.Flipping(greek, 0f, 0), Timing, true));
        Assert.IsFalse(DisplayText.ShowsForeign(Shekel, Reveal.Flipping(greek, 0f, 0), Timing, true));
        Assert.AreEqual(For(Shekel, Reveal.Untranslated(greek)), For(Shekel, Reveal.Untranslated(greek), true), "reduced motion translates nothing by itself");
    }

    [Test]
    public void Progress_ChangesExactlyWhenTheShownTextDoes()
    {
        ForeignText greek = Foreign(Greek);
        int previousProgress = DisplayText.Progress(Shekel, Reveal.Flipping(greek, 0f, 0), Timing, false);
        string previousText = For(Shekel, Reveal.Flipping(greek, 0f, 0));
        for (float e = 0.0005f; e < 1.3f; e += 0.001f)
        {
            int progress = DisplayText.Progress(Shekel, Reveal.Flipping(greek, e, 0), Timing, false);
            string text = For(Shekel, Reveal.Flipping(greek, e, 0));
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

        string half = For(Shekel, Reveal.Flipping(arabic, 0.665f, 0));
        StringAssert.Contains("Silver", half, "the English run keeps its order");
        Assert.AreEqual(Shekel, For(Shekel, Reveal.Flipping(arabic, 5f, 0)));
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
