using System.Linq;
using NUnit.Framework;

/// <summary>The character file-name grammar (it must equal the art brief's coverage.json grammar).</summary>
public class LookKeysTests
{
    [Test]
    public void EveryNameForm()
    {
        Assert.AreEqual("body_m_skin3", LookKeys.Body(TravellerGender.Male, 3).Name);
        Assert.AreEqual("head_f_skin5_facec", LookKeys.Head(TravellerGender.Female, 5, "c").Name);
        Assert.AreEqual("premade_socrates_worried", LookKeys.Premade("socrates", "worried", "greece", "ancient").Name);
    }

    [TestCase(LookLayer.HairBack, null, "hairback_m_egypt_ancient")]
    [TestCase(LookLayer.HairBack, "black", "hairback_m_egypt_ancient_black")]
    [TestCase(LookLayer.Outfit, null, "outfit_m_egypt_ancient")]
    [TestCase(LookLayer.FacialHair, "grey", "facialhair_m_egypt_ancient_grey")]
    [TestCase(LookLayer.Hair, null, "hair_m_egypt_ancient")]
    [TestCase(LookLayer.Hair, "red", "hair_m_egypt_ancient_red")]
    [TestCase(LookLayer.Headwear, null, "headwear_m_egypt_ancient")]
    [TestCase(LookLayer.Accessory, null, "accessory_m_egypt_ancient")]
    public void GarmentNames(LookLayer layer, string colour, string expected)
    {
        LookKey key = LookKeys.Garment(layer, TravellerGender.Male, "egypt", "ancient", colour);
        Assert.AreEqual(expected, key.Name);
        Assert.AreEqual(layer, key.Layer);
        Assert.AreEqual("egypt", key.NationId);
        Assert.AreEqual(colour, key.HairColour);
    }

    [Test]
    public void BodyHeadAndWhole_AreNotGarmentKeys()
    {
        Assert.Throws<System.ArgumentException>(() => LookKeys.Garment(LookLayer.Body, TravellerGender.Male, "a", "b", null));
        Assert.Throws<System.ArgumentException>(() => LookKeys.Garment(LookLayer.Whole, TravellerGender.Male, "a", "b", null));
    }

    [TestCase("egypt", true)]
    [TestCase("earlymodern", true)]
    [TestCase("abc123", true)]
    [TestCase("", false)]
    [TestCase(null, false)]
    [TestCase("early_modern", false)]
    [TestCase("Egypt", false)]
    [TestCase("gal lerani", false)]
    public void IsToken(string s, bool expected)
    {
        Assert.AreEqual(expected, LookKeys.IsToken(s));
    }

    [Test]
    public void GenderToken_UnknownThrows()
    {
        Assert.AreEqual("m", LookKeys.GenderToken(TravellerGender.Male));
        Assert.AreEqual("f", LookKeys.GenderToken(TravellerGender.Female));
        Assert.Throws<System.ArgumentException>(() => LookKeys.GenderToken(TravellerGender.Unknown));
    }

    private static PlaceWardrobe Fixture(bool wig) => new PlaceWardrobe
    {
        male = new GenderLook
        {
            signature = LookSlot.Accessory,
            outfit = new LookItem { label = "kilt" },
            hair = new LookItem { label = "bob", wig = wig, back = true },
            facialHair = new LookItem { label = "goatee" },
            accessory = new LookItem { label = "collar", leakable = true }
        },
        female = new GenderLook
        {
            signature = LookSlot.Accessory,
            outfit = new LookItem { label = "sheath" },
            hair = new LookItem { label = "long" },
            accessory = new LookItem { label = "collar", leakable = true }
        }
    };

    [Test]
    public void Required_AllColoursForHairAndItsBack_OneUncolouredKeyEachForAWig_FacialHairAlwaysInEveryColour()
    {
        string[] plain = LookKeys.Required("egypt", "ancient", Fixture(false)).ToArray();
        // Men: outfit 1 + (hairback + hair) x 5 + facial hair 5 + accessory 1; women: outfit 1 + hair 5 + accessory 1.
        Assert.AreEqual(1 + 10 + 5 + 1 + 1 + 5 + 1, plain.Length);
        CollectionAssert.Contains(plain, "hairback_m_egypt_ancient_grey");
        CollectionAssert.Contains(plain, "facialhair_m_egypt_ancient_red");
        CollectionAssert.AllItemsAreUnique(plain);

        string[] wig = LookKeys.Required("egypt", "ancient", Fixture(true)).ToArray();
        Assert.AreEqual(1 + 2 + 5 + 1 + 1 + 5 + 1, wig.Length);
        CollectionAssert.Contains(wig, "hair_m_egypt_ancient");
        CollectionAssert.Contains(wig, "hairback_m_egypt_ancient");
        CollectionAssert.Contains(wig, "facialhair_m_egypt_ancient_black", "a wig never strips the beard's colour");
        CollectionAssert.IsEmpty(LookKeys.Required("egypt", "ancient", null));
    }

    [Test]
    public void ArtNation_IsTheItemsOverride_ElseThePlacesNation()
    {
        Assert.AreEqual("egypt", new LookItem { label = "bob" }.ArtNation("egypt"));
        Assert.AreEqual("egypt", new LookItem { label = "bob", artNation = " " }.ArtNation("egypt"));
        Assert.AreEqual("neutral", new LookItem { label = "bob", artNation = "neutral" }.ArtNation("egypt"));
    }

    /// <summary>A Future place's wardrobe: its own outfit, the hair (with a back) and the men's facial hair filed under <paramref name="artNation"/>.</summary>
    private static PlaceWardrobe Future(string outfit, string artNation) => new PlaceWardrobe
    {
        male = new GenderLook
        {
            signature = LookSlot.Outfit,
            outfit = new LookItem { label = outfit + " coat" },
            hair = new LookItem { label = "neat short cut", back = true, artNation = artNation },
            facialHair = new LookItem { label = "short stubble", artNation = artNation }
        },
        female = new GenderLook
        {
            signature = LookSlot.Outfit,
            outfit = new LookItem { label = outfit + " gown" },
            hair = new LookItem { label = "sleek bob", artNation = artNation }
        }
    };

    [Test]
    public void Required_FilesAnArtNationItemUnderIt_SoPlacesSharingItShareTheNames_WithTheSameCounts()
    {
        string[] china = LookKeys.Required("china", "future", Future("pankou", "neutral")).ToArray();
        string[] egypt = LookKeys.Required("egypt", "future", Future("yoke", "neutral")).ToArray();

        // Men: outfit 1 + (hairback + hair) x 5 + facial hair 5; women: outfit 1 + hair 5.
        Assert.AreEqual(1 + 10 + 5 + 1 + 5, china.Length);
        Assert.AreEqual(LookKeys.Required("china", "future", Future("pankou", null)).Count(), china.Length, "the override changes names, not counts");
        CollectionAssert.Contains(china, "hair_m_neutral_future_black");
        CollectionAssert.Contains(china, "hairback_m_neutral_future_grey");
        CollectionAssert.Contains(china, "facialhair_m_neutral_future_red");
        CollectionAssert.Contains(china, "hair_f_neutral_future_blond");
        CollectionAssert.Contains(china, "outfit_m_china_future");
        CollectionAssert.AreEqual(china.Where(k => !k.StartsWith("outfit_")).ToArray(), egypt.Where(k => !k.StartsWith("outfit_")).ToArray(), "one shared drawing set");
        CollectionAssert.IsEmpty(china.Intersect(egypt).Where(k => k.StartsWith("outfit_")), "each place keeps its own outfit");
        CollectionAssert.DoesNotContain(LookKeys.Required("china", "future", Future("pankou", null)).ToArray(), "hair_m_neutral_future_black");
    }

    [Test]
    public void Bases_TenBodiesAndAHeadPerSkinAndFace()
    {
        var rules = new LookRules
        {
            faceBands =
            {
                new FaceBand { minAge = 18, faces = { "a", "b" } },
                new FaceBand { minAge = 35, faces = { "c" } },
                new FaceBand { minAge = 60, faces = { "d" } }
            }
        };
        string[] bases = LookKeys.Bases(rules).ToArray();
        Assert.AreEqual(10, bases.Count(b => b.StartsWith("body_")));
        Assert.AreEqual(40, bases.Count(b => b.StartsWith("head_")));
        CollectionAssert.Contains(bases, "head_f_skin5_faced");
        CollectionAssert.AllItemsAreUnique(bases);
    }

    [Test]
    public void PremadeSet_TheFourExpressions()
    {
        CollectionAssert.AreEqual(new[] { "premade_arib_neutral", "premade_arib_happy", "premade_arib_angry", "premade_arib_worried" },
                                  LookKeys.PremadeSet("arib").ToArray());
    }
}
