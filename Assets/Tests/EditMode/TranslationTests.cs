using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Translation's game rules (piece 9): the translator upgrade ids, which
/// document fields and speakers use the claimed place's tongue, which tongues
/// are foreign or translated today (read from the day-start snapshot), and the
/// content problems the generator and the validator report.
/// </summary>
public class TranslationTests
{
    /// <summary>English (native), Egyptian and Arabic (Near East), Greek (Mediterranean); foreign text from day 2.</summary>
    private static TranslationRules Rules() => new TranslationRules
    {
        fromDay = 2,
        tongues = new List<Tongue>
        {
            new Tongue { id = "english", displayName = "English", script = "latin", pack = "", glyphs = "" },
            new Tongue { id = "egyptian", displayName = "Egyptian", script = "hieroglyphs", pack = "near_east", glyphs = "x" },
            new Tongue { id = "arabic", displayName = "Arabic", script = "arabic", pack = "near_east", glyphs = "x" },
            new Tongue { id = "greek", displayName = "Greek", script = "greek", pack = "mediterranean", glyphs = "x" },
        },
        packs = new List<TranslatorPack>
        {
            new TranslatorPack { id = "near_east", displayName = "Near East" },
            new TranslatorPack { id = "mediterranean", displayName = "Mediterranean" },
        }
    };

    private static readonly string[] Scripts = { "latin", "hieroglyphs", "arabic", "greek" };

    private static IEnumerable<KeyValuePair<string, string>> Places(params (string place, string tongue)[] places) =>
        places.Select(p => new KeyValuePair<string, string>(p.place, p.tongue));

    private static readonly IEnumerable<KeyValuePair<string, string>> GoodPlaces =
        Places(("egypt_ancient", "egyptian"), ("egypt_medieval", "arabic"), ("greece_ancient", "greek"), ("britain_future", "english"));

    /// <summary>A day-start snapshot on <paramref name="day"/> owning <paramref name="upgrades"/> and setting <paramref name="flags"/>.</summary>
    private static GateSnapshot Snap(int day, string[] upgrades = null, string[] flags = null) =>
        new GateSnapshot(day, 100f, flags, upgrades, null, null, null, null);

    [Test]
    public void UpgradeId_IsTrPackKind()
    {
        Assert.AreEqual("tr_near_east_written", Translation.UpgradeId("near_east", TranslatorKind.Written));
        Assert.AreEqual("tr_near_east_spoken", Translation.UpgradeId("near_east", TranslatorKind.Spoken));
    }

    [TestCase(ClueCategory.Language, true)]
    [TestCase(ClueCategory.Material, true)]
    [TestCase(ClueCategory.Politics, true)]
    [TestCase(ClueCategory.Technology, true)]
    [TestCase(ClueCategory.Currency, true)]
    [TestCase(ClueCategory.Geography, true)]
    [TestCase(ClueCategory.Culture, true)]
    [TestCase(ClueCategory.Name, false)]
    [TestCase(ClueCategory.BirthDate, false)]
    public void InTongue_EveryPlaceFact_NeverTheNameOrBirthDate(ClueCategory category, bool expected)
    {
        Assert.AreEqual(expected, Translation.InTongue(category));
    }

    [Test]
    public void InTongue_TheTravellerSpeaksIt_TheDeskSpeaksEnglish()
    {
        Assert.IsTrue(Translation.InTongue(DialogSpeaker.Traveller));
        Assert.IsFalse(Translation.InTongue(DialogSpeaker.Desk));
    }

    [Test]
    public void ANativeTongue_IsNeverForeign()
    {
        var day = new TranslationDay(Rules(), Snap(5));
        Assert.IsFalse(day.Foreign("english"));
        Assert.IsFalse(day.Translated("english", TranslatorKind.Written));
        Assert.IsNull(day.PackOf(day.TongueOf("english")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("klingon")]
    public void ABlankOrUnknownTongue_IsNotForeign(string id)
    {
        var day = new TranslationDay(Rules(), Snap(5));
        Assert.IsNull(day.TongueOf(id));
        Assert.IsFalse(day.Foreign(id));
    }

    [Test]
    public void BeforeFromDay_NothingIsForeign_EvenOwningBothTranslators()
    {
        var day = new TranslationDay(Rules(), Snap(1, new[] { "tr_near_east_written", "tr_near_east_spoken" }));
        Assert.IsFalse(day.Foreign("egyptian"));
        Assert.IsFalse(day.Translated("egyptian", TranslatorKind.Written));
        Assert.IsFalse(day.Translated("egyptian", TranslatorKind.Spoken));
    }

    [Test]
    public void OnFromDay_WithNoTranslator_AForeignTongueIsTranslatedForNeitherKind()
    {
        var day = new TranslationDay(Rules(), Snap(2));
        Assert.IsTrue(day.Foreign("egyptian"));
        Assert.IsFalse(day.Translated("egyptian", TranslatorKind.Written));
        Assert.IsFalse(day.Translated("egyptian", TranslatorKind.Spoken));
        Assert.AreEqual("near_east", day.PackOf(day.TongueOf("egyptian")).id);
    }

    [Test]
    public void OwningPapersOnly_TranslatesWritten_NotSpoken_AndTheReverse()
    {
        var papers = new TranslationDay(Rules(), Snap(3, new[] { "tr_near_east_written" }));
        Assert.IsTrue(papers.Translated("arabic", TranslatorKind.Written));
        Assert.IsFalse(papers.Translated("arabic", TranslatorKind.Spoken));

        var speech = new TranslationDay(Rules(), Snap(3, new[] { "tr_near_east_spoken" }));
        Assert.IsFalse(speech.Translated("arabic", TranslatorKind.Written));
        Assert.IsTrue(speech.Translated("arabic", TranslatorKind.Spoken));
    }

    [Test]
    public void AnotherPacksTranslator_TranslatesNothingHere()
    {
        var day = new TranslationDay(Rules(), Snap(3, new[] { "tr_mediterranean_written", "tr_mediterranean_spoken" }));
        Assert.IsFalse(day.Translated("egyptian", TranslatorKind.Written));
        Assert.IsTrue(day.Translated("greek", TranslatorKind.Written));
    }

    [Test]
    public void TheUpgradeFlag_Alone_TranslatesNothing()
    {
        var day = new TranslationDay(Rules(), Snap(3, null, new[] { "upgrade:tr_near_east_written" }));
        Assert.IsFalse(day.Translated("egyptian", TranslatorKind.Written), "it reads owned upgrades, never flags");
    }

    [Test]
    public void NullRules_OrANullSnapshot_MakeNothingForeign()
    {
        var noRules = new TranslationDay(null, Snap(5));
        Assert.IsFalse(noRules.Foreign("egyptian"));
        Assert.IsNull(noRules.TongueOf("egyptian"));

        var noSnapshot = new TranslationDay(Rules(), null);
        Assert.IsFalse(noSnapshot.Foreign("egyptian"));
    }

    [Test]
    public void EditingTheRulesAfterwards_ChangesNothing()
    {
        TranslationRules rules = Rules();
        var day = new TranslationDay(rules, Snap(2, new[] { "tr_near_east_written" }));
        rules.fromDay = 9;
        rules.tongues[1].pack = "";
        rules.tongues[1].displayName = "Changed";
        rules.packs[0].id = "elsewhere";
        rules.tongues.Clear();

        Assert.IsTrue(day.Foreign("egyptian"));
        Assert.IsTrue(day.Translated("egyptian", TranslatorKind.Written));
        Assert.AreEqual("Egyptian", day.TongueOf("egyptian").displayName);
        Assert.AreEqual("Near East", day.PackOf(day.TongueOf("egyptian")).displayName);
    }

    [Test]
    public void Problems_ACleanSet_HasNone()
    {
        CollectionAssert.IsEmpty(Translation.Problems(Rules(), Scripts, GoodPlaces));
    }

    [Test]
    public void Problems_NullRules()
    {
        Assert.AreEqual(1, Translation.Problems(null, Scripts, GoodPlaces).Count);
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Problems_FromDayBelowOne(int fromDay)
    {
        TranslationRules rules = Rules();
        rules.fromDay = fromDay;
        AssertOne(rules, GoodPlaces, "fromDay");
    }

    [Test]
    public void Problems_BlankTongueId()
    {
        TranslationRules rules = Rules();
        rules.tongues.Add(new Tongue { id = " ", displayName = "Nothing", script = "latin", pack = "" });
        AssertOne(rules, GoodPlaces, "blank id");
    }

    [Test]
    public void Problems_DuplicateTongueId()
    {
        TranslationRules rules = Rules();
        rules.tongues.Add(new Tongue { id = "greek", displayName = "Greek again", script = "greek", pack = "mediterranean", glyphs = "x" });
        AssertOne(rules, GoodPlaces, "'greek'");
    }

    [Test]
    public void Problems_BlankDisplayName()
    {
        TranslationRules rules = Rules();
        rules.tongues[3].displayName = "";
        AssertOne(rules, GoodPlaces, "'greek'");
    }

    [Test]
    public void Problems_UnknownScript()
    {
        TranslationRules rules = Rules();
        rules.tongues[3].script = "linear_b";
        AssertOne(rules, GoodPlaces, "'linear_b'");
    }

    [Test]
    public void Problems_UnknownPack()
    {
        TranslationRules rules = Rules();
        rules.tongues[3].pack = "aegean";
        List<string> problems = Translation.Problems(rules, Scripts, GoodPlaces);
        Assert.IsTrue(problems.Any(p => p.Contains("'aegean'")), string.Join(" | ", problems));
    }

    [Test]
    public void Problems_AForeignTongueWithoutGlyphs()
    {
        TranslationRules rules = Rules();
        rules.tongues[1].glyphs = "";
        AssertOne(rules, GoodPlaces, "'egyptian'");
    }

    [Test]
    public void Problems_BlankOrDuplicatePackIdOrName()
    {
        TranslationRules blankId = Rules();
        blankId.packs.Add(new TranslatorPack { id = "", displayName = "Nowhere" });
        StringAssert.Contains("blank id", Translation.Problems(blankId, Scripts, GoodPlaces).Single());

        TranslationRules duplicateId = Rules();
        duplicateId.packs.Add(new TranslatorPack { id = "near_east", displayName = "Levant" });
        StringAssert.Contains("'near_east'", Translation.Problems(duplicateId, Scripts, GoodPlaces).Single());

        TranslationRules blankName = Rules();
        blankName.packs[1].displayName = " ";
        StringAssert.Contains("'mediterranean'", Translation.Problems(blankName, Scripts, GoodPlaces).Single());

        TranslationRules duplicateName = Rules();
        duplicateName.packs[1].displayName = "Near East";
        StringAssert.Contains("'Near East'", Translation.Problems(duplicateName, Scripts, GoodPlaces).Single());
    }

    [Test]
    public void Problems_APackNoTongueUses()
    {
        TranslationRules rules = Rules();
        rules.packs.Add(new TranslatorPack { id = "east_asia", displayName = "East Asia" });
        AssertOne(rules, GoodPlaces, "'east_asia'");
    }

    [Test]
    public void Problems_APlaceWithABlankOrUnknownTongue()
    {
        AssertOne(Rules(), GoodPlaces.Append(new KeyValuePair<string, string>("china_ancient", "")), "'china_ancient'");
        List<string> unknown = Translation.Problems(Rules(), Scripts, GoodPlaces.Append(new KeyValuePair<string, string>("china_ancient", "chinese")));
        Assert.AreEqual(1, unknown.Count, string.Join(" | ", unknown));
        StringAssert.Contains("'china_ancient'", unknown[0]);
        StringAssert.Contains("'chinese'", unknown[0]);
    }

    /// <summary>Exactly one problem, naming <paramref name="fragment"/>.</summary>
    private static void AssertOne(TranslationRules rules, IEnumerable<KeyValuePair<string, string>> places, string fragment)
    {
        List<string> problems = Translation.Problems(rules, Scripts, places);
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains(fragment, problems[0]);
    }
}
