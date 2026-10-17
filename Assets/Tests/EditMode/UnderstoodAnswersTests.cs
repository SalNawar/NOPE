using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Saleh after demo 1016a: "travellers still answer questions when they speak
/// in a language you don't understand". A traveller whose tongue is foreign
/// today, without its Speech translator, answers in the tongue: the answer's
/// value is wholly in it (no key word or digit inside it stays English), and
/// held against anything it is not understood, so it proves nothing, matches
/// nothing and logs nothing; with the translator it reads and proves as before.
/// SpeechTranslation.Understood, KeyWords.Spans (the tongue slot),
/// CompareEvidence.notUnderstood, DiscrepancyLog.Prove, FindingRules.Classify
/// and ComparePair.
/// </summary>
public class UnderstoodAnswersTests
{
    private const string Greek = "ολμναξπρηστφχψυςάέβγιδζθωκ";
    private const string Traveller = "Nikias";

    private static SpeechTranslation Foreign(bool translated) =>
        new SpeechTranslation(new ForeignText(Pseudoscript.ParseTable(Greek, out _), false), translated, null, false, "(untranslated)");

    [Test]
    public void Understood_TheDeskAlways_TheTravellerOnlyInAReadTongue()
    {
        Assert.IsTrue(SpeechTranslation.None.Understood(true), "a native or unknown tongue");
        Assert.IsFalse(Foreign(false).Understood(true), "a foreign tongue without its translator");
        Assert.IsTrue(Foreign(false).Understood(false), "the desk speaks English");
        Assert.IsTrue(Foreign(true).Understood(true), "with the region's Speech translator");
        Assert.AreEqual("(untranslated)", Foreign(false).Shown(true, "12 Mar 1520 BCE"));
        Assert.AreEqual("12 Mar 1520 BCE", Foreign(true).Shown(true, "12 Mar 1520 BCE"));
    }

    [Test]
    public void TheTongueSlot_KeepsNoKeyWordInsideItsFill_ButTheRestOfTheLineDoes()
    {
        var rule = new KeyWordRule { slots = new List<string> { Interview.PlaceToken }, words = new List<string> { "home" }, digits = true };
        var fills = new Dictionary<string, string> { { Interview.ValueToken, "home on 12 Mar 1520" }, { Interview.PlaceToken, "Thebes" } };
        const string template = "In {place}, home since 1490: {value}.";
        // "In Thebes, home since 1490: home on 12 Mar 1520."
        CollectionAssert.AreEqual(new[] { (3, 6), (11, 4), (22, 4), (28, 4), (36, 2), (43, 4) }, KeyWords.Spans(template, fills, rule));
        CollectionAssert.AreEqual(new[] { (3, 6), (11, 4), (22, 4) }, KeyWords.Spans(template, fills, rule, Interview.ValueToken),
            "the value (28..47) is wholly in the tongue; the place, 'home' and 1490 outside it stay English");
    }

    private const string Nation = "egypt";
    private const string Era = "ancient";

    private static CompareEvidence Said(bool understood) => CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 Jun 1801 BCE", true, understood);
    private static CompareEvidence Record() => CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 Jun 1510 BCE", Traveller);

    [Test]
    public void AnAnswerNotUnderstood_ProvesNothing_AndIsNotLogged()
    {
        Assert.NotNull(DiscrepancyLog.Prove(Said(true), Record(), Nation, Era, Traveller), "understood, the liar's answer is proven against the record");
        Assert.IsNull(DiscrepancyLog.Prove(Said(false), Record(), Nation, Era, Traveller));
        Assert.IsNull(DiscrepancyLog.Prove(Record(), Said(false), Nation, Era, Traveller), "either side");

        FindingKind kind = FindingRules.Classify(Said(false), Record(), null, Nation, Era, Traveller);
        Assert.AreEqual(FindingKind.NotUnderstood, kind);
        Assert.IsFalse(FindingRules.IsLogged(kind));
        Assert.IsFalse(FindingRules.IsDifference(kind));
        Assert.AreEqual(FindingKind.NotUnderstood, FindingRules.Classify(Record(), CompareEvidence.ForAnswer(ClueCategory.Currency, "Deben", false, false), null, Nation, Era, Traveller),
            "whatever it is held against, before the details are compared");
    }

    [Test]
    public void APairWithAnAnswerNotUnderstood_NeitherMatchesNorDiffers()
    {
        var pair = new ComparePair();
        pair.Select(new ComparePick("line:3", "Traveller", "(untranslated)", CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 Jun 1510 BCE", false, false)));
        pair.Select(new ComparePick("record:X:BirthDate", "Records", "3 Jun 1510 BCE", Record()));
        Assert.IsFalse(pair.Understood);
        Assert.IsFalse(pair.Matches, "the same value, yet nothing understood matches");

        pair.Clear();
        pair.Select(new ComparePick("line:3", "Traveller", "3 Jun 1510 BCE", CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 Jun 1510 BCE", false)));
        pair.Select(new ComparePick("record:X:BirthDate", "Records", "3 Jun 1510 BCE", Record()));
        Assert.IsTrue(pair.Understood);
        Assert.IsTrue(pair.Matches);
    }
}
