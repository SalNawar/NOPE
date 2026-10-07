using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The character art fallback: which delivered key stands in for a key with no art, in which order.</summary>
public class LookArtFallbackTests
{
    private static readonly LookArtUniverse Universe = new LookArtUniverse(
        new[] { "egypt", "iraq", "greece", "italy", "britain", "neutral" },
        new[] { "ancient", "medieval", "earlymodern", "industrial", "modern", "future" },
        new[] { "a", "b", "c", "d" });

    private static LookArtFallbackTable Table(LookLayer layer, params LookArtFallbackStep[] steps) => new LookArtFallbackTable
    {
        neighbours = new List<LookArtNeighbours>
        {
            new LookArtNeighbours { nation = "egypt", near = new List<string> { "iraq", "greece" } },
            new LookArtNeighbours { nation = "britain", near = new List<string> { "italy" } }
        },
        layers = new List<LookArtLayerSteps> { new LookArtLayerSteps { layer = layer, steps = steps.ToList() } }
    };

    private static List<string> Names(LookKey key, LookArtFallbackTable table) =>
        LookArtFallback.Candidates(key, table, Universe).Select(k => k.Name).ToList();

    [Test]
    public void TheKeyItselfComesFirst_AndAloneWithoutSteps()
    {
        LookKey outfit = LookKeys.Garment(LookLayer.Outfit, TravellerGender.Male, "egypt", "modern", null);
        CollectionAssert.AreEqual(new[] { "outfit_m_egypt_modern" }, Names(outfit, new LookArtFallbackTable()));
        CollectionAssert.AreEqual(new[] { "outfit_m_egypt_modern" }, Names(outfit, null));
        Assert.AreEqual("outfit_m_egypt_modern", Names(outfit, Table(LookLayer.Outfit, LookArtFallbackStep.OtherEra))[0]);
    }

    [Test]
    public void OtherEra_TheSameNation_NearestEraFirst_EarlierOnATie()
    {
        LookKey outfit = LookKeys.Garment(LookLayer.Outfit, TravellerGender.Female, "egypt", "earlymodern", null);
        CollectionAssert.AreEqual(
            new[] { "outfit_f_egypt_earlymodern", "outfit_f_egypt_medieval", "outfit_f_egypt_industrial", "outfit_f_egypt_ancient", "outfit_f_egypt_modern", "outfit_f_egypt_future" },
            Names(outfit, Table(LookLayer.Outfit, LookArtFallbackStep.OtherEra)));
    }

    [Test]
    public void NeighbourNation_TheSameEra_InTheTablesOrder_NoneForAnUnlistedNation()
    {
        LookArtFallbackTable table = Table(LookLayer.Headwear, LookArtFallbackStep.NeighbourNation);
        CollectionAssert.AreEqual(new[] { "headwear_m_egypt_medieval", "headwear_m_iraq_medieval", "headwear_m_greece_medieval" },
                                  Names(LookKeys.Garment(LookLayer.Headwear, TravellerGender.Male, "egypt", "medieval", null), table));
        CollectionAssert.AreEqual(new[] { "headwear_m_iraq_medieval" },
                                  Names(LookKeys.Garment(LookLayer.Headwear, TravellerGender.Male, "iraq", "medieval", null), table));
    }

    [Test]
    public void AnyPlace_NearestEraFirst_ThenNeighboursBeforeTheOtherNations()
    {
        List<string> names = Names(LookKeys.Garment(LookLayer.Outfit, TravellerGender.Male, "britain", "industrial", null),
                                   Table(LookLayer.Outfit, LookArtFallbackStep.AnyPlace));
        CollectionAssert.AreEqual(
            new[] { "outfit_m_britain_industrial", "outfit_m_italy_industrial", "outfit_m_egypt_industrial", "outfit_m_iraq_industrial", "outfit_m_greece_industrial", "outfit_m_neutral_industrial",
                    "outfit_m_britain_earlymodern", "outfit_m_italy_earlymodern" },
            names.Take(8).ToList());
        Assert.AreEqual(6 * 6, names.Count, "every nation in every era, each once");
        Assert.AreEqual(names.Count, names.Distinct().Count());
    }

    [Test]
    public void AStandIn_KeepsTheLayerGenderColourAndVariant()
    {
        LookKey hair = LookKeys.Garment(LookLayer.Hair, TravellerGender.Female, "iraq", "medieval", "red");
        foreach (LookKey k in LookArtFallback.Candidates(hair, Table(LookLayer.Hair, LookArtFallbackStep.AnyPlace), Universe))
        {
            Assert.AreEqual(LookLayer.Hair, k.Layer);
            Assert.AreEqual(TravellerGender.Female, k.Gender);
            Assert.AreEqual("red", k.HairColour);
            StringAssert.EndsWith("_red", k.Name);
        }

        LookKey kit = LookKeys.Garment(LookLayer.Accessory, TravellerGender.Male, "neutral", "future", null, "comm");
        Assert.IsTrue(Names(kit, Table(LookLayer.Accessory, LookArtFallbackStep.OtherEra)).All(n => n.EndsWith("_comm")));

        LookKey wig = LookKeys.Garment(LookLayer.Hair, TravellerGender.Female, "egypt", "modern", null);
        Assert.IsTrue(Names(wig, Table(LookLayer.Hair, LookArtFallbackStep.OtherEra)).All(n => !LookKeys.HairColours.Any(c => n.EndsWith("_" + c))),
                      "an uncoloured wig only meets uncoloured names");
    }

    [Test]
    public void AHead_OtherFaceOfItsSkin_ThenTheNearestSkins_TheLighterOnATie()
    {
        List<string> names = Names(LookKeys.Head(TravellerGender.Male, 3, "c"),
                                   Table(LookLayer.Head, LookArtFallbackStep.OtherFace, LookArtFallbackStep.OtherSkin));
        CollectionAssert.AreEqual(new[] { "head_m_skin3_facec", "head_m_skin3_facea", "head_m_skin3_faceb", "head_m_skin3_faced", "head_m_skin2_facec", "head_m_skin2_facea" },
                                  names.Take(6).ToList());
        Assert.AreEqual(5 * 4, names.Count);

        CollectionAssert.AreEqual(new[] { "body_f_skin3", "body_f_skin2", "body_f_skin4", "body_f_skin1", "body_f_skin5" },
                                  Names(LookKeys.Body(TravellerGender.Female, 3), Table(LookLayer.Body, LookArtFallbackStep.OtherSkin)));
    }

    [Test]
    public void AStepOnlyAppliesToItsKindOfLayer()
    {
        LookArtFallbackTable table = Table(LookLayer.Body, LookArtFallbackStep.OtherEra, LookArtFallbackStep.OtherFace, LookArtFallbackStep.NeutralExpression);
        CollectionAssert.AreEqual(new[] { "body_m_skin2" }, Names(LookKeys.Body(TravellerGender.Male, 2), table));

        LookArtFallbackTable garment = Table(LookLayer.Outfit, LookArtFallbackStep.OtherSkin, LookArtFallbackStep.OtherFace);
        Assert.AreEqual(1, Names(LookKeys.Garment(LookLayer.Outfit, TravellerGender.Male, "egypt", "modern", null), garment).Count);
    }

    [Test]
    public void APremadesExpression_FallsBackToItsNeutralPicture()
    {
        CollectionAssert.AreEqual(new[] { "premade_ada_worried", "premade_ada_neutral" },
                                  Names(LookKeys.Premade("ada", "worried"), Table(LookLayer.Whole, LookArtFallbackStep.NeutralExpression)));
    }

    [Test]
    public void Resolve_TheFirstCandidateWithArt_NullWhenNone()
    {
        LookArtFallbackTable table = Table(LookLayer.Outfit, LookArtFallbackStep.OtherEra, LookArtFallbackStep.NeighbourNation, LookArtFallbackStep.AnyPlace);
        LookKey outfit = LookKeys.Garment(LookLayer.Outfit, TravellerGender.Female, "egypt", "modern", null);

        var art = new HashSet<string> { "outfit_f_greece_ancient", "outfit_f_egypt_medieval" };
        Assert.AreEqual("outfit_f_egypt_medieval", LookArtFallback.Resolve(outfit, table, Universe, art.Contains)?.Name, "the same nation beats a neighbour");

        art.Remove("outfit_f_egypt_medieval");
        Assert.AreEqual("outfit_f_greece_ancient", LookArtFallback.Resolve(outfit, table, Universe, art.Contains)?.Name);

        art.Add("outfit_f_egypt_modern");
        Assert.AreEqual("outfit_f_egypt_modern", LookArtFallback.Resolve(outfit, table, Universe, art.Contains)?.Name, "its own art first");

        Assert.IsNull(LookArtFallback.Resolve(outfit, table, Universe, _ => false));
    }

    [Test]
    public void TheTable_UnlistedLayerOrNation_IsEmpty()
    {
        LookArtFallbackTable table = Table(LookLayer.Hair, LookArtFallbackStep.OtherEra);
        Assert.AreEqual(0, table.StepsFor(LookLayer.Outfit).Count);
        Assert.AreEqual(0, table.NeighboursOf("japan").Count);
        CollectionAssert.AreEqual(new[] { "iraq", "greece" }, table.NeighboursOf("egypt").ToList());
    }

    [Test]
    public void TheCivilDress_FallsBackToTheCivilRowsNations_InTheLatestEra_WithoutItsVariant_InItsColour()
    {
        var table = new LookArtFallbackTable
        {
            neighbours = new List<LookArtNeighbours> { new LookArtNeighbours { nation = LookKeys.CivilNation, near = new List<string> { "neutral", "britain" } } },
            layers = new List<LookArtLayerSteps>
            {
                new LookArtLayerSteps { layer = LookLayer.Outfit, steps = new List<LookArtFallbackStep> { LookArtFallbackStep.CivilDress } },
                new LookArtLayerSteps { layer = LookLayer.Hair, steps = new List<LookArtFallbackStep> { LookArtFallbackStep.CivilDress } }
            }
        };
        CollectionAssert.AreEqual(new[] { "outfit_m_civil_2150_v2", "outfit_m_neutral_future", "outfit_m_britain_future" },
                                  Names(LookKeys.CivilOutfit(TravellerGender.Male, "v2"), table));
        CollectionAssert.AreEqual(new[] { "hair_f_civil_2150_red", "hair_f_neutral_future_red", "hair_f_britain_future_red" },
                                  Names(LookKeys.CivilHair(TravellerGender.Female, "red"), table));
        Assert.AreEqual("outfit_f_britain_future", LookArtFallback.Resolve(LookKeys.CivilOutfit(TravellerGender.Female, "v1"), table, Universe, n => n == "outfit_f_britain_future")?.Name,
                        "today's 2150 outfit stands in until the civilian one lands");
        CollectionAssert.AreEqual(new[] { "outfit_m_egypt_modern" }, Names(LookKeys.Garment(LookLayer.Outfit, TravellerGender.Male, "egypt", "modern", null), table), "a place's garment takes no civil step");
    }

    [Test]
    public void APremadesPhoto_FallsBackToTheirNeutralPicture()
    {
        CollectionAssert.AreEqual(new[] { "premade_socrates_photo", "premade_socrates_neutral" },
                                  Names(LookKeys.PremadePhoto("socrates"), Table(LookLayer.Whole, LookArtFallbackStep.NeutralExpression)));
    }
}
