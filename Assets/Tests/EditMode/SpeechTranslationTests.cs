using NUnit.Framework;

/// <summary>
/// One traveller's speech translation (piece 9, speech only since the
/// redesign's phase 1), the decision table the transcript, the bubble and the
/// compare bar read (audit R4-021: it moved out of the untestable
/// CaseTranslation): a line is plain unless the tongue is foreign today and the
/// line is in it; untranslated without the Speech translator; with it, the
/// bubble flips and the transcript shows the settled English; the bar shows
/// the placeholder for an untranslated answer only.
/// </summary>
public class SpeechTranslationTests
{
    private const string Greek = "ολμναξπρηστφχψυςάέβγιδζθωκ";
    private const string Placeholder = "(untranslated Greek; Mediterranean Translator)";

    private static ForeignText Look() => new ForeignText(Pseudoscript.ParseTable(Greek, out _), false);

    private static SpeechTranslation Foreign(bool translated) =>
        new SpeechTranslation(Look(), translated, new FlipTiming { startDelay = 0.5f }, false, Placeholder);

    [Test]
    public void None_ShowsEverythingPlain()
    {
        SpeechTranslation none = SpeechTranslation.None;
        Assert.AreEqual(RevealKind.Plain, none.Line(true, null).Kind);
        Assert.AreEqual(RevealKind.Plain, none.Bubble(1f, null).Kind);
        Assert.AreEqual("Deben", none.Shown(true, "Deben"));
        Assert.IsNotNull(none.Timing);
        Assert.IsFalse(none.ReducedMotion);
    }

    [Test]
    public void Untranslated_TheTravellersLinesShowTheirTongue_TheDesksStayPlain()
    {
        SpeechTranslation tr = Foreign(false);
        Reveal line = tr.Line(true, null);
        Assert.AreEqual(RevealKind.Untranslated, line.Kind);
        Assert.AreEqual(Pseudoscript.TableSize, line.Foreign.Table.Count, "in the tongue's look");
        Assert.AreEqual(RevealKind.Plain, tr.Line(false, null).Kind, "the desk speaks English");
        Assert.AreEqual(RevealKind.Untranslated, tr.Bubble(3f, null).Kind, "without the Speech translator the bubble never flips");
    }

    [Test]
    public void Translated_TheBubbleFlipsOnTheLinesClock_TheTranscriptIsSettled()
    {
        SpeechTranslation tr = Foreign(true);
        Reveal bubble = tr.Bubble(0.25f, null);
        Assert.AreEqual(RevealKind.Flipping, bubble.Kind);
        Assert.AreEqual(0.25f, bubble.Elapsed);
        Assert.AreEqual(RevealKind.Plain, tr.Line(true, null).Kind, "the transcript is the record: never animated");
        Assert.AreEqual(0.5f, tr.Timing.startDelay, "the flip's knobs");
    }

    /// <summary>The line's key-word spans go with its reveal, untranslated or flipping; a plain line needs none.</summary>
    [Test]
    public void TheLinesEnglishSpans_GoWithItsReveal()
    {
        var keys = new System.Collections.Generic.List<(int start, int length)> { (0, 4) };
        Assert.AreSame(keys, Foreign(false).Line(true, keys).English);
        Assert.AreSame(keys, Foreign(false).Bubble(1f, keys).English);
        Reveal flipping = Foreign(true).Bubble(1f, keys);
        Assert.AreEqual(RevealKind.Flipping, flipping.Kind);
        Assert.AreSame(keys, flipping.English);
        Assert.AreEqual(RevealKind.Plain, Foreign(true).Line(true, keys).Kind);
    }

    [Test]
    public void Shown_ThePlaceholderOnlyForAnUntranslatedTravellersAnswer()
    {
        Assert.AreEqual(Placeholder, Foreign(false).Shown(true, "Deben"));
        Assert.AreEqual("Deben", Foreign(false).Shown(false, "Deben"), "not in the tongue");
        Assert.AreEqual("Deben", Foreign(true).Shown(true, "Deben"), "translated");
    }

    [Test]
    public void NullKnobsAndPlaceholder_FallBackToDefaults()
    {
        var tr = new SpeechTranslation(Look(), false, null, true, null);
        Assert.IsNotNull(tr.Timing);
        Assert.IsTrue(tr.ReducedMotion);
        Assert.AreEqual(string.Empty, tr.Shown(true, "Deben"), "no placeholder authored: an empty bar side, never the glyphs");
    }
}
