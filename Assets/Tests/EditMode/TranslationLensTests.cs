using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The Translation Lens's rules (Saleh 2026-10-06): the language lock and the first level come with the lens's day; the further levels are owned in order; what a hover covers; the cheats' grant.</summary>
public class TranslationLensTests
{
    private static readonly string[] Levels = { "lens_word", "lens_sentence", "lens_object" };

    /// <summary>The ramp's lens on day 8 (and something else on day 1).</summary>
    private static Introductions Ramp() => new Introductions(new (int, IEnumerable<string>)[]
    {
        (1, new[] { Feature.Calendar }),
        (8, new[] { Feature.Lens, Feature.Field(ClueCategory.Employer) }),
    });

    private static System.Func<string, bool> Owning(params string[] ids) => id => System.Array.IndexOf(ids, id) >= 0;

    [Test]
    public void LanguageLocked_FromTheLensDayOn_FreeInWeekOne()
    {
        Introductions ramp = Ramp();
        Assert.IsFalse(TranslationLens.LanguageLocked(1, ramp));
        Assert.IsFalse(TranslationLens.LanguageLocked(7, ramp), "day 7: the player may still switch");
        Assert.IsTrue(TranslationLens.LanguageLocked(8, ramp), "day 8: locked");
        Assert.IsTrue(TranslationLens.LanguageLocked(15, ramp), "and stays locked");
        Assert.AreEqual(8, TranslationLens.LockDay(ramp));
    }

    [Test]
    public void LanguageLocked_Never_WhenNoDayIntroducesTheLens()
    {
        Assert.IsFalse(TranslationLens.LanguageLocked(15, Introductions.None));
        Assert.IsFalse(TranslationLens.LanguageLocked(15, null));
        Assert.AreEqual(0, TranslationLens.LockDay(Introductions.None));
    }

    [Test]
    public void Reach_None_BeforeTheLensDay_EvenWithLaterLevelsBought()
    {
        Assert.AreEqual(LensReach.None, TranslationLens.Reach(7, Ramp(), Levels, Owning()));
        Assert.AreEqual(LensReach.None, TranslationLens.Reach(7, Ramp(), Levels, Owning("lens_sentence", "lens_object")),
                        "a level bought early waits for the lens itself");
    }

    [Test]
    public void Reach_Word_IssuedOnTheLensDay()
    {
        Assert.AreEqual(LensReach.Word, TranslationLens.Reach(8, Ramp(), Levels, Owning()));
        Assert.AreEqual(LensReach.Word, TranslationLens.Reach(12, Ramp(), Levels, Owning()));
    }

    [Test]
    public void Reach_GrowsWithEachLevelOwnedInOrder()
    {
        Assert.AreEqual(LensReach.Sentence, TranslationLens.Reach(8, Ramp(), Levels, Owning("lens_sentence")));
        Assert.AreEqual(LensReach.Object, TranslationLens.Reach(8, Ramp(), Levels, Owning("lens_sentence", "lens_object")));
        Assert.AreEqual(LensReach.Word, TranslationLens.Reach(8, Ramp(), Levels, Owning("lens_object")), "Object without Sentence counts for nothing yet");
    }

    [Test]
    public void Reach_OwningTheFirstLevel_WorksBeforeItsDay()
    {
        Assert.AreEqual(LensReach.Word, TranslationLens.Reach(2, Ramp(), Levels, Owning("lens_word")), "a granted lens (the cheats) reads in week 1 too");
        Assert.AreEqual(LensReach.Object, TranslationLens.Reach(2, Ramp(), Levels, Owning("lens_word", "lens_sentence", "lens_object")));
    }

    [Test]
    public void Reach_None_WithoutLevels()
    {
        Assert.AreEqual(LensReach.None, TranslationLens.Reach(9, Ramp(), new string[0], Owning()));
        Assert.AreEqual(LensReach.None, TranslationLens.Reach(9, Ramp(), null, Owning()));
    }

    [Test]
    public void Grant_AddsEachLevelUpToIt_Once()
    {
        var owned = new List<string> { "interview_protocols" };
        Assert.AreEqual(2, TranslationLens.Grant(LensReach.Sentence, owned, Levels));
        CollectionAssert.AreEqual(new[] { "interview_protocols", "lens_word", "lens_sentence" }, owned);
        Assert.AreEqual(1, TranslationLens.Grant(LensReach.Object, owned, Levels));
        Assert.AreEqual(0, TranslationLens.Grant(LensReach.Object, owned, Levels), "nothing twice");
        Assert.AreEqual(0, TranslationLens.Grant(LensReach.None, owned, Levels));
        Assert.AreEqual(LensReach.Object, TranslationLens.Reach(1, Ramp(), Levels, owned.Contains));
    }

    [Test]
    public void Covered_Word_TheHoveredWordOnly()
    {
        var covered = new List<LensWordRef>();
        TranslationLens.Covered(LensReach.Word, false, 0, 1, new[] { 3 }, covered);
        CollectionAssert.AreEqual(new[] { new LensWordRef(0, 1) }, covered);

        TranslationLens.Covered(LensReach.Word, false, 0, -1, new[] { 3 }, covered);
        Assert.IsEmpty(covered, "on the label but between words (a digit, a space): nothing");

        TranslationLens.Covered(LensReach.Word, false, -1, -1, new[] { 3 }, covered);
        Assert.IsEmpty(covered);
    }

    [Test]
    public void Covered_Sentence_EveryWordOfTheHoveredLabel()
    {
        var covered = new List<LensWordRef>();
        TranslationLens.Covered(LensReach.Sentence, false, 0, -1, new[] { 3 }, covered);
        CollectionAssert.AreEqual(new[] { new LensWordRef(0, 0), new LensWordRef(0, 1), new LensWordRef(0, 2) }, covered);

        TranslationLens.Covered(LensReach.Sentence, true, -1, -1, new[] { 3 }, covered);
        Assert.IsEmpty(covered, "over the object but no label: a sentence needs its label");
    }

    [Test]
    public void Covered_Object_EveryWordOfEveryLabel_WhereverThePointerIsOnTheObject()
    {
        var covered = new List<LensWordRef>();
        TranslationLens.Covered(LensReach.Object, true, -1, -1, new[] { 1, 2 }, covered);
        CollectionAssert.AreEqual(new[] { new LensWordRef(0, 0), new LensWordRef(1, 0), new LensWordRef(1, 1) }, covered);

        TranslationLens.Covered(LensReach.Object, false, -1, -1, new[] { 1, 2 }, covered);
        Assert.IsEmpty(covered, "off the object: nothing");

        TranslationLens.Covered(LensReach.None, true, 0, 0, new[] { 1 }, covered);
        Assert.IsEmpty(covered, "no lens");
    }

    [Test]
    public void Problems_ThreeDistinctNamedLevels()
    {
        Assert.IsEmpty(TranslationLens.Problems(new LensRules { levelIds = new List<string>(Levels) }));
        Assert.AreEqual(1, TranslationLens.Problems(new LensRules { levelIds = new List<string> { "a", "b" } }).Count);
        Assert.AreEqual(1, TranslationLens.Problems(new LensRules { levelIds = new List<string> { "a", "a", "b" } }).Count);
        Assert.AreEqual(1, TranslationLens.Problems(new LensRules { levelIds = new List<string> { "a", " ", "b" } }).Count);
        Assert.AreEqual(1, TranslationLens.Problems(null).Count);
    }

    [Test]
    public void Feature_Lens_IsANamedKey()
    {
        Assert.IsTrue(Introductions.IsNamedKey(Feature.Lens));
        Assert.AreEqual("tool:lens", Feature.Lens);
    }
}
