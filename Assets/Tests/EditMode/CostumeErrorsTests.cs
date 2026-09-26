using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The costume roll (traveller types C2, K5 and §6.4): who may err, the draw
/// order on the fault stream (the roll, the variant by content weights, the
/// source), what a variant needs to show, a planned or pinned error, and the
/// panic wording the dress lines and the citation carry.
/// </summary>
public class CostumeErrorsTests
{
    /// <summary>Another place's item 2, 2150 clothes 1, a 2150 accessory 2 (world_source.json looks.costumeErrors).</summary>
    private static CostumeErrorWeights Weights() => new CostumeErrorWeights { otherPlace = 2f, presentClothes = 1f, presentAccessory = 2f };

    private static ScriptStep V(float roll) => ScriptStep.Value(roll);
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    /// <summary>Plans with three other places, the present's clothes and two kit items open, at a chance of 0.1.</summary>
    private static CostumePlan Plan(ScriptedRandom rng, bool planned = false, CostumeError pinned = CostumeError.None,
                                    int otherPlaces = 3, bool presentClothes = true, int kitItems = 2, float chance = 0.1f) =>
        CostumeErrors.Plan(chance, planned, pinned, Weights(), otherPlaces, presentClothes, kitItems, rng);

    [TestCase(TravellerKind.RichTourist, true)]
    [TestCase(TravellerKind.PoorTourist, true)]
    [TestCase(TravellerKind.Labourer, true)]
    [TestCase(TravellerKind.Displaced, false)]
    public void OnlyA2150Citizen_MayErr(TravellerKind kind, bool expected)
    {
        Assert.AreEqual(expected, CostumeErrors.MayErr(kind));
    }

    [Test]
    public void ARollAtOrAboveTheChance_IsNoError_AfterOneDraw()
    {
        var rng = new ScriptedRandom(V(0.1f));
        CostumePlan plan = Plan(rng);
        Assert.IsTrue(rng.Done);
        Assert.AreEqual(CostumeError.None, plan.Error);
        Assert.IsFalse(plan.Rolled);
        Assert.AreEqual(-1, plan.SourceIndex);

        var zero = new ScriptedRandom(V(0f));
        Assert.AreEqual(CostumeError.None, Plan(zero, chance: 0f).Error, "a chance of 0 never errs");
        Assert.IsTrue(zero.Done);
    }

    [Test]
    public void AnError_DrawsTheRoll_TheVariant_ThenAnotherPlacesIndex()
    {
        var rng = new ScriptedRandom(V(0.05f), V(0f), R(2));
        CostumePlan plan = Plan(rng);
        Assert.IsTrue(rng.Done, "three draws");
        Assert.AreEqual(CostumeError.OtherPlace, plan.Error);
        Assert.AreEqual(2, plan.SourceIndex);
        Assert.IsTrue(plan.Rolled);
    }

    [Test]
    public void The2150Clothes_DrawNoSource()
    {
        var rng = new ScriptedRandom(V(0.05f), V(0.5f));
        CostumePlan plan = Plan(rng);
        Assert.IsTrue(rng.Done, "0.5 of weights 2, 1, 2 falls on the clothes; no third draw");
        Assert.AreEqual(CostumeError.PresentClothes, plan.Error);
        Assert.AreEqual(-1, plan.SourceIndex);
    }

    [Test]
    public void A2150Accessory_DrawsAKitIndex()
    {
        var rng = new ScriptedRandom(V(0.05f), V(0.9f), R(1));
        CostumePlan plan = Plan(rng);
        Assert.IsTrue(rng.Done);
        Assert.AreEqual(CostumeError.PresentAccessory, plan.Error);
        Assert.AreEqual(1, plan.SourceIndex);
    }

    [Test]
    public void AVariantThatCannotShow_IsNeverDrawn()
    {
        var rng = new ScriptedRandom(V(0.05f), V(0f));
        Assert.AreEqual(CostumeError.PresentClothes, Plan(rng, otherPlaces: 0, kitItems: 0).Error, "only the clothes can show");
        Assert.IsTrue(rng.Done);

        var noClothes = new ScriptedRandom(V(0.05f), V(0.6f), R(0));
        Assert.AreEqual(CostumeError.PresentAccessory, Plan(noClothes, presentClothes: false).Error, "0.6 of weights 2, 0, 2 falls past the other places");
        Assert.IsTrue(noClothes.Done);
    }

    [Test]
    public void NothingCanShow_IsNoError_AfterTheRoll_ButRolled()
    {
        var rng = new ScriptedRandom(V(0.05f));
        CostumePlan plan = Plan(rng, otherPlaces: 0, presentClothes: false, kitItems: 0);
        Assert.IsTrue(rng.Done);
        Assert.AreEqual(CostumeError.None, plan.Error);
        Assert.IsTrue(plan.Rolled, "the caller warns: the roll said yes but nothing could show");

        var zeroWeights = new ScriptedRandom(V(0.05f));
        Assert.AreEqual(CostumeError.None, CostumeErrors.Plan(0.1f, false, CostumeError.None, new CostumeErrorWeights(), 3, true, 2, zeroWeights).Error);
        Assert.IsTrue(zeroWeights.Done);
    }

    [Test]
    public void APlannedError_SkipsTheRoll()
    {
        var rng = new ScriptedRandom(V(0.9f), R(1));
        CostumePlan plan = Plan(rng, planned: true, chance: 0f);
        Assert.IsTrue(rng.Done, "the variant and the source only");
        Assert.AreEqual(CostumeError.PresentAccessory, plan.Error);
        Assert.AreEqual(1, plan.SourceIndex);
        Assert.IsTrue(plan.Rolled);
    }

    [Test]
    public void APinnedVariant_DrawsNoVariant_UnlessItCannotShow()
    {
        var rng = new ScriptedRandom(R(2));
        CostumePlan plan = Plan(rng, planned: true, pinned: CostumeError.OtherPlace);
        Assert.IsTrue(rng.Done, "the source only");
        Assert.AreEqual((CostumeError.OtherPlace, 2), (plan.Error, plan.SourceIndex));

        var clothes = new ScriptedRandom();
        Assert.AreEqual(CostumeError.PresentClothes, Plan(clothes, planned: true, pinned: CostumeError.PresentClothes).Error);
        Assert.IsTrue(clothes.Done, "no draw at all");

        var fallback = new ScriptedRandom(V(0f), R(0));
        Assert.AreEqual(CostumeError.OtherPlace, Plan(fallback, planned: true, pinned: CostumeError.PresentAccessory, kitItems: 0).Error, "no kit item: the weighted draw");
        Assert.IsTrue(fallback.Done);
    }

    [Test]
    public void NoRandomSource_IsNoError()
    {
        Assert.AreEqual(CostumeError.None, CostumeErrors.Plan(1f, true, CostumeError.PresentClothes, Weights(), 3, true, 2, null).Error);
    }

    [Test]
    public void WeightOf_EachVariant_NoneIsZero()
    {
        CostumeErrorWeights w = Weights();
        Assert.AreEqual((2f, 1f, 2f, 0f), (w.Of(CostumeError.OtherPlace), w.Of(CostumeError.PresentClothes), w.Of(CostumeError.PresentAccessory), w.Of(CostumeError.None)));
    }

    // -----------------------------
    // The panic wording (content: world_source.json ui.strings)
    // -----------------------------

    /// <summary>The English UI string of <paramref name="key"/> in world_source.json (null when missing).</summary>
    private static string UiString(string key, [CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        string path = File.Exists(source) ? source : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", source);
        ContentNode strings = ContentJson.Parse(File.ReadAllText(path)).Get("ui").Get("strings");
        ContentNode entry = strings.Items.FirstOrDefault(s => s.Get("key").Text == key);
        return entry?.Get("text").Text;
    }

    [TestCase("deviation.claimMismatch.worn")]
    [TestCase("deviation.foreignOrigin.worn")]
    public void EveryDressLine_SaysItWouldCauseAPanic(string key)
    {
        StringAssert.Contains("would cause a panic", UiString(key));
    }

    [Test]
    public void AWrongAcceptOfACostumeError_IsCitedAsAPanic_InThePlace()
    {
        Assert.AreEqual("citation.acceptedWrong.panic", new CaseVerdict { accepted = true, faultReason = CostumeErrors.FaultReason }.MistakeKey);
        string text = UiString("citation.acceptedWrong.panic");
        StringAssert.Contains("would cause a panic in {0}", text);
    }
}
