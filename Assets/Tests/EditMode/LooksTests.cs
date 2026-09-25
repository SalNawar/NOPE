using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Composing looks, the leak rules and the Culture value. Claim: Victorian
/// London (britain_industrial; men: frock coat, side-parted curls,
/// mutton-chop whiskers, top hat (signature, leakable), watch chain; women:
/// day dress, ringlets and bun (with a hair back), poke bonnet (signature,
/// leakable), Paisley shawl). True home: Tokugawa Edo (japan_earlymodern;
/// men: kosode, chasen-mage (signature hair, leakable); women: kosode,
/// hyogo-mage (hair back), Nagoya-obi (signature accessory, leakable)).
/// Claim year 1843; bands 18: a, b; 35: c; 60: d; grey from 60.
/// </summary>
public class LooksTests
{
    private const int Year = 1843;
    private const string Born25 = "3 Jun 1818";

    private static LookItem Item(string label, bool leakable = false, bool wig = false, bool back = false, params LookSlot[] covers) =>
        new LookItem { label = label, leakable = leakable, wig = wig, back = back, covers = covers.ToList() };

    private static GenderLook Look(LookSlot signature, LookItem outfit, LookItem hair, LookItem facialHair = null, LookItem headwear = null, LookItem accessory = null) =>
        new GenderLook
        {
            signature = signature,
            outfit = outfit ?? new LookItem(),
            hair = hair ?? new LookItem(),
            facialHair = facialHair ?? new LookItem(),
            headwear = headwear ?? new LookItem(),
            accessory = accessory ?? new LookItem()
        };

    private static PlaceWardrobe London() => new PlaceWardrobe
    {
        male = Look(LookSlot.Headwear, Item("frock coat"), Item("side-parted curls"), Item("mutton-chop whiskers", true), Item("top hat", true), Item("watch chain")),
        female = Look(LookSlot.Headwear, Item("day dress"), Item("ringlets and bun", back: true), null, Item("poke bonnet", true), Item("Paisley shawl", true))
    };

    private static PlaceWardrobe Edo() => new PlaceWardrobe
    {
        male = Look(LookSlot.Hair, Item("kosode"), Item("chasen-mage", true)),
        female = Look(LookSlot.Accessory, Item("kosode"), Item("hyogo-mage", back: true), accessory: Item("Nagoya-obi", true))
    };

    private static LookSource Source(string nation, string era, PlaceWardrobe w) =>
        new LookSource { NationId = nation, EraId = era, PlaceId = $"{nation}_{era}", Wardrobe = w, CultureValue = Looks.CultureValue(w) };

    private static LookSource Claim(PlaceWardrobe w = null) => Source("britain", "industrial", w ?? London());
    private static LookSource Home(PlaceWardrobe w = null) => Source("japan", "earlymodern", w ?? Edo());

    private static LookRules Rules() => new LookRules
    {
        faceBands =
        {
            new FaceBand { minAge = 18, faces = { "a", "b" } },
            new FaceBand { minAge = 35, faces = { "c" } },
            new FaceBand { minAge = 60, faces = { "d" } }
        },
        greyFromAge = 60,
        wholeFigureLabel = "Period dress"
    };

    private static LookWeights Weights() => new LookWeights
    {
        skin = new[] { 5f, 4f, 1f, 0f, 0f },
        hair = { new HairColourWeight { colour = "black", weight = 1f }, new HairColourWeight { colour = "brown", weight = 5f } }
    };

    private static ScriptStep V(float roll) => ScriptStep.Value(roll);
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    /// <summary>A known-gender draw script: skin, face, hair.</summary>
    private static ScriptedRandom Draws(float skin = 0f, int face = 0, float hair = 0.9f) => new ScriptedRandom(V(skin), R(face), V(hair));

    private static TravellerLook Compose(TravellerGender g, LookSource leak = null, string born = Born25, IRandomSource rng = null,
                                         LookSource claim = null, LookWeights weights = null, LookRules rules = null) =>
        Looks.Compose(claim ?? Claim(), leak, g, born, Year, weights ?? Weights(), rules ?? Rules(), rng ?? Draws());

    private static string[] Names(TravellerLook look) => look.Keys.Select(k => k.Name).ToArray();

    // -----------------------------
    // Draw order
    // -----------------------------

    [Test]
    public void Compose_KnownGender_DrawsSkinFaceHair_InThatOrder()
    {
        var rng = new ScriptedRandom(V(0.6f), R(1), V(0.0f));
        TravellerLook look = Compose(TravellerGender.Male, rng: rng);
        Assert.IsTrue(rng.Done, "three draws");
        Assert.AreEqual(2, look.SkinTone, "0.6 of weights 5,4,1 falls on tone 2");
        Assert.AreEqual("b", look.Face);
        Assert.AreEqual("black", look.HairColour);
    }

    [Test]
    public void Compose_UnknownGender_DrawsTheGenderFirst()
    {
        var rng = new ScriptedRandom(V(0.7f), V(0f), R(0), V(0.9f));
        TravellerLook look = Compose(TravellerGender.Unknown, rng: rng);
        Assert.IsTrue(rng.Done, "four draws");
        Assert.AreEqual(TravellerGender.Female, look.Gender);
        Assert.AreEqual(TravellerGender.Male, Compose(TravellerGender.Unknown, rng: new ScriptedRandom(V(0.2f), V(0f), R(0), V(0.9f))).Gender);
    }

    // -----------------------------
    // Honest and leaked looks
    // -----------------------------

    [Test]
    public void AnHonestLook_WearsTheClaim_InStackAndSlotOrder()
    {
        TravellerLook look = Compose(TravellerGender.Male);
        CollectionAssert.AreEqual(new[]
        {
            "body_m_skin1", "outfit_m_britain_industrial", "head_m_skin1_facea", "facialhair_m_britain_industrial_brown",
            "hair_m_britain_industrial_brown", "headwear_m_britain_industrial", "accessory_m_britain_industrial"
        }, Names(look));
        CollectionAssert.AreEqual(new[] { LookSlot.Outfit, LookSlot.Hair, LookSlot.FacialHair, LookSlot.Headwear, LookSlot.Accessory }, look.Garments.Select(g => g.Slot).ToArray());
        Assert.IsTrue(look.Garments.All(g => g.Value == "top hat / poke bonnet" && !g.IsTell));
        CollectionAssert.AreEqual(new[] { "frock coat", "side-parted curls", "mutton-chop whiskers", "top hat", "watch chain" }, look.Garments.Select(g => g.Label).ToArray());
        Assert.IsNull(look.PremadeId);
    }

    [Test]
    public void ALeak_PutsTheHomesSignatureInItsSlot_KeepingTheTravellersColour()
    {
        TravellerLook look = Compose(TravellerGender.Male, Home());
        Garment tell = look.Garments.Single(g => g.IsTell);
        Assert.AreEqual(LookSlot.Hair, tell.Slot);
        Assert.AreEqual("chasen-mage", tell.Label);
        Assert.AreEqual("chasen-mage / Nagoya-obi", tell.Value);
        Assert.AreEqual("hair_m_japan_earlymodern_brown", look.PartOn(LookLayer.Hair).Value.Key.Name, "the leaked hairstyle in the traveller's colour");
        Assert.AreEqual(1, look.PartOn(LookLayer.Hair).Value.GarmentIndex);
        Assert.IsTrue(look.Garments.Where(g => !g.IsTell).All(g => g.Value == "top hat / poke bonnet"), "every other garment is the claim's");
        Assert.AreEqual("headwear_m_britain_industrial", look.PartOn(LookLayer.Headwear).Value.Key.Name);
    }

    [Test]
    public void AWig_IsUncoloured_ButTheBeardKeepsItsColour_AndBothKeysAreRequired()
    {
        PlaceWardrobe egypt = Edo();
        egypt.male.hair = Item("bobbed wig", true, wig: true, back: true);
        TravellerLook look = Compose(TravellerGender.Male, Home(egypt));
        Assert.AreEqual("hair_m_japan_earlymodern", look.PartOn(LookLayer.Hair).Value.Key.Name);
        Assert.AreEqual("hairback_m_japan_earlymodern", look.PartOn(LookLayer.HairBack).Value.Key.Name, "a wig's back is uncoloured too");
        Assert.AreEqual("facialhair_m_britain_industrial_brown", look.PartOn(LookLayer.FacialHair).Value.Key.Name);

        var required = new HashSet<string>(LookKeys.Required("britain", "industrial", London()).Concat(LookKeys.Required("japan", "earlymodern", egypt)));
        foreach (LookPart part in look.Parts.Where(p => Looks.IsGarmentLayer(p.Layer)))
            Assert.IsTrue(required.Contains(part.Key.Name), part.Key.Name);
    }

    [Test]
    public void ACoveredItem_IsNeitherAPartNorAGarment_AndALeakCanCover()
    {
        PlaceWardrobe turban = London();
        turban.male.headwear = Item("imama turban", true, false, false, LookSlot.Hair);
        TravellerLook look = Compose(TravellerGender.Male, claim: Claim(turban));
        Assert.IsNull(look.PartOn(LookLayer.Hair));
        Assert.IsFalse(look.Garments.Any(g => g.Slot == LookSlot.Hair));

        PlaceWardrobe home = Edo();
        home.male.signature = LookSlot.Headwear;
        home.male.headwear = Item("imama turban", true, false, false, LookSlot.Hair);
        TravellerLook leaked = Compose(TravellerGender.Male, Home(home));
        Assert.IsNull(leaked.PartOn(LookLayer.Hair), "the leaked turban hides the claim's hair");
        Assert.AreEqual("imama turban", leaked.Garments.Single(g => g.IsTell).Label);
    }

    [Test]
    public void TheHairBack_IsUnderTheBody_PointsAtTheHair_AndGoesWithCoveredHair()
    {
        TravellerLook look = Compose(TravellerGender.Female);
        Assert.AreEqual(LookLayer.HairBack, look.Parts[0].Layer);
        Assert.AreEqual(LookLayer.Body, look.Parts[1].Layer);
        Assert.AreEqual("hairback_f_britain_industrial_brown", look.Parts[0].Key.Name);
        int hair = look.Garments.ToList().FindIndex(g => g.Slot == LookSlot.Hair);
        Assert.AreEqual(hair, look.Parts[0].GarmentIndex);

        PlaceWardrobe hooded = London();
        hooded.female.headwear = Item("hood", true, false, false, LookSlot.Hair);
        Assert.IsNull(Compose(TravellerGender.Female, claim: Claim(hooded)).PartOn(LookLayer.HairBack));
    }

    // -----------------------------
    // Faces, grey, weights
    // -----------------------------

    [TestCase(18, "a", "b")]
    [TestCase(34, "a", "b")]
    [TestCase(35, "c", "c")]
    [TestCase(59, "c", "c")]
    [TestCase(60, "d", "d")]
    [TestCase(70, "d", "d")]
    public void TheFace_FollowsTheAgeBand(int age, string first, string second)
    {
        string born = $"1 Jan {Year - age}";
        Assert.AreEqual(first, Compose(TravellerGender.Male, born: born, rng: Draws(face: 0)).Face);
        Assert.AreEqual(second, Compose(TravellerGender.Male, born: born, rng: Draws(face: 1)).Face);
    }

    [Test]
    public void AnUnreadableDate_UsesTheFirstBand_AndBceAgesHaveNoYearZero()
    {
        TravellerLook unknown = Compose(TravellerGender.Male, born: "Unknown", rng: Draws(face: 1));
        Assert.AreEqual("b", unknown.Face);
        Assert.AreNotEqual(LookKeys.Grey, unknown.HairColour);

        LookSource rome = Source("italy", "ancient", London());
        TravellerLook bce = Looks.Compose(rome, null, TravellerGender.Male, "1 Jan 25 BCE", 10, Weights(), Rules(), Draws(face: 0));
        Assert.AreEqual("a", bce.Face, "25 BCE to 10 CE is 34 years (there is no year 0), so band a/b, not c");
    }

    [Test]
    public void Grey_FromTheGreyAge_OnHairHairBackAndBeard()
    {
        TravellerLook old = Compose(TravellerGender.Male, born: $"1 Jan {Year - 60}");
        Assert.AreEqual(LookKeys.Grey, old.HairColour);
        Assert.AreEqual("hair_m_britain_industrial_grey", old.PartOn(LookLayer.Hair).Value.Key.Name);
        Assert.AreEqual("facialhair_m_britain_industrial_grey", old.PartOn(LookLayer.FacialHair).Value.Key.Name);
        Assert.AreEqual("hairback_f_britain_industrial_grey", Compose(TravellerGender.Female, born: $"1 Jan {Year - 61}").Parts[0].Key.Name);
        Assert.AreEqual("brown", Compose(TravellerGender.Male, born: $"1 Jan {Year - 59}").HairColour);
    }

    [Test]
    public void ZeroSkinWeights_AreNeverDrawn_AllZeroGivesToneThree_NoHairWeightsGiveBrown()
    {
        var weights = new LookWeights { skin = new[] { 0f, 1f, 0f, 1f, 0f }, hair = Weights().hair };
        for (int seed = 0; seed < 200; seed++)
        {
            int tone = Compose(TravellerGender.Female, weights: weights, rng: new SeededRandom(seed)).SkinTone;
            Assert.IsTrue(tone == 2 || tone == 4, $"seed {seed}: tone {tone}");
        }

        var noSkin = new LookWeights { skin = new float[5], hair = Weights().hair };
        var rng = new ScriptedRandom(R(0), V(0.9f));
        Assert.AreEqual(3, Compose(TravellerGender.Male, weights: noSkin, rng: rng).SkinTone);
        Assert.IsTrue(rng.Done, "no skin draw");

        var noHair = new LookWeights { skin = Weights().skin };
        var rng2 = new ScriptedRandom(V(0f), R(0));
        Assert.AreEqual(LookKeys.Brown, Compose(TravellerGender.Male, weights: noHair, rng: rng2).HairColour);
        Assert.IsTrue(rng2.Done, "no hair draw");
    }

    [Test]
    public void NoWardrobe_GivesBodyAndHeadOnly_WithNoDraws()
    {
        var rng = new ScriptedRandom();
        TravellerLook look = Looks.Compose(new LookSource { NationId = "x", EraId = "y" }, null, TravellerGender.Unknown, Born25, Year, Weights(), Rules(), rng);
        CollectionAssert.AreEqual(new[] { "body_m_skin3", "head_m_skin3_facea" }, Names(look));
        CollectionAssert.IsEmpty(look.Garments);
        Assert.AreEqual(0, rng.Draws);
    }

    // -----------------------------
    // Premades
    // -----------------------------

    [Test]
    public void Whole_IsOnePictureAndOneGarment_WithNoDraws()
    {
        TravellerLook look = Looks.Whole("socrates", Source("greece", "ancient", London()), Rules());
        Assert.AreEqual(1, look.Parts.Count);
        Assert.AreEqual(LookLayer.Whole, look.Parts[0].Layer);
        Assert.AreEqual("premade_socrates_neutral", look.Parts[0].Key.Name);
        Assert.AreEqual("greece", look.Parts[0].Key.NationId);
        Garment garment = look.Garments.Single();
        Assert.AreEqual(LookSlot.Outfit, garment.Slot);
        Assert.AreEqual("Period dress", garment.Label);
        Assert.AreEqual("top hat / poke bonnet", garment.Value);
        Assert.IsFalse(garment.IsTell);
        Assert.AreEqual("socrates", look.PremadeId);
        Assert.AreEqual("premade socrates", look.Describe());
    }

    [TestCase("happy", "premade_socrates_happy")]
    [TestCase("angry", "premade_socrates_angry")]
    [TestCase("worried", "premade_socrates_worried")]
    [TestCase("neutral", "premade_socrates_neutral")]
    [TestCase("", "premade_socrates_neutral")]
    [TestCase(null, "premade_socrates_neutral")]
    [TestCase("furious", "premade_socrates_neutral")]
    public void WholeKey_PicksTheExpression_NeutralWhenBlankOrUnknown(string expression, string expected)
    {
        Assert.AreEqual(expected, Looks.Whole("socrates", Source("greece", "ancient", London()), Rules()).WholeKey(expression).Name);
    }

    [Test]
    public void WholeKey_OfAGeneratedLook_Throws()
    {
        Assert.Throws<System.InvalidOperationException>(() => Compose(TravellerGender.Male).WholeKey("happy"));
    }

    // -----------------------------
    // CanLeak
    // -----------------------------

    [Test]
    public void CanLeak_DecisionTable()
    {
        LookRules rules = Rules();
        Assert.IsTrue(Looks.CanLeak(Claim(), Home(), TravellerGender.Male, rules), "the true row: a leakable hairstyle");
        Assert.IsTrue(Looks.CanLeak(Claim(), Home(), TravellerGender.Female, rules), "the true row: a leakable accessory");

        Assert.IsFalse(Looks.CanLeak(Claim(), Home(), TravellerGender.Unknown, rules), "unknown gender");
        Assert.IsFalse(Looks.CanLeak(Claim(), new LookSource { PlaceId = "x" }, TravellerGender.Male, rules), "no wardrobe");

        PlaceWardrobe absent = Edo();
        absent.male.signature = LookSlot.Headwear;
        Assert.IsFalse(Looks.CanLeak(Claim(), Home(absent), TravellerGender.Male, rules), "the signature is an absence");

        PlaceWardrobe outfit = Edo();
        outfit.male.signature = LookSlot.Outfit;
        outfit.male.outfit.leakable = true;
        Assert.IsFalse(Looks.CanLeak(Claim(), Home(outfit), TravellerGender.Male, rules), "never the whole outfit");

        PlaceWardrobe fixedItem = Edo();
        fixedItem.male.hair.leakable = false;
        Assert.IsFalse(Looks.CanLeak(Claim(), Home(fixedItem), TravellerGender.Male, rules), "not leakable");

        PlaceWardrobe turban = London();
        turban.male.headwear = Item("imama turban", true, false, false, LookSlot.Hair);
        Assert.IsFalse(Looks.CanLeak(Claim(turban), Home(), TravellerGender.Male, rules), "hidden by the disguise's headwear");

        PlaceWardrobe same = London();
        same.male.hair = Item(" CHASEN-MAGE ");
        Assert.IsFalse(Looks.CanLeak(Claim(same), Home(), TravellerGender.Male, rules), "the disguise's own item is labelled the same");

        foreach ((string a, string b, string gender) in new[] { ("britain_industrial", "japan_earlymodern", "m"), ("japan_earlymodern", "britain_industrial", "m"), ("britain_industrial", "japan_earlymodern", "") })
        {
            LookRules confusable = Rules();
            confusable.confusable.Add(new ConfusablePair { placeA = a, placeB = b, slot = LookSlot.Hair, gender = gender });
            Assert.IsFalse(Looks.CanLeak(Claim(), Home(), TravellerGender.Male, confusable), $"confusable {a}/{b} '{gender}'");
        }

        LookRules otherGender = Rules();
        otherGender.confusable.Add(new ConfusablePair { placeA = "britain_industrial", placeB = "japan_earlymodern", slot = LookSlot.Hair, gender = "f" });
        Assert.IsTrue(Looks.CanLeak(Claim(), Home(), TravellerGender.Male, otherGender), "a women's pair leaves men alone");
        LookRules otherSlot = Rules();
        otherSlot.confusable.Add(new ConfusablePair { placeA = "britain_industrial", placeB = "japan_earlymodern", slot = LookSlot.Headwear, gender = "" });
        Assert.IsTrue(Looks.CanLeak(Claim(), Home(), TravellerGender.Male, otherSlot), "another slot's pair");
    }

    // -----------------------------
    // Culture value and labels
    // -----------------------------

    [Test]
    public void CultureValue_IsMenSlashWomen_OneLabelWhenTheyMatch_NullWhenOneIsMissing()
    {
        Assert.AreEqual("top hat / poke bonnet", Looks.CultureValue(London()));
        PlaceWardrobe both = London();
        both.female.headwear = Item(" TOP HAT ");
        Assert.AreEqual("top hat", Looks.CultureValue(both));
        PlaceWardrobe missing = London();
        missing.female.signature = LookSlot.FacialHair;
        Assert.IsNull(Looks.CultureValue(missing));
        Assert.IsNull(Looks.CultureValue(null));
    }

    [Test]
    public void LabelProblems_PerGenderRule()
    {
        CollectionAssert.IsEmpty(Looks.LabelProblems(new[] { ("britain_industrial", London()), ("japan_earlymodern", Edo()) }));

        PlaceWardrobe twin = Edo();
        twin.male.signature = LookSlot.Headwear;
        twin.male.headwear = Item("Top Hat", true);
        List<string> shared = Looks.LabelProblems(new[] { ("britain_industrial", London()), ("japan_earlymodern", twin) });
        Assert.AreEqual(1, shared.Count, string.Join(" | ", shared));
        StringAssert.Contains("'britain_industrial' and 'japan_earlymodern' share the m signature label", shared[0]);

        PlaceWardrobe women = Edo();
        women.female.headwear = Item("poke bonnet");
        List<string> item = Looks.LabelProblems(new[] { ("britain_industrial", London()), ("japan_earlymodern", women) });
        Assert.AreEqual(1, item.Count, string.Join(" | ", item));
        StringAssert.Contains("'japan_earlymodern' f Headwear 'poke bonnet' is labelled like the f signature of 'britain_industrial'", item[0]);

        PlaceWardrobe men = Edo();
        men.male.headwear = Item("poke bonnet");
        CollectionAssert.IsEmpty(Looks.LabelProblems(new[] { ("britain_industrial", London()), ("japan_earlymodern", men) }), "the other gender's signature");

        PlaceWardrobe slash = Edo();
        slash.male.outfit = Item("kosode/hakama");
        StringAssert.Contains("holds '/'", Looks.LabelProblems(new[] { ("japan_earlymodern", slash) }).Single());
    }

    [Test]
    public void IsGarmentLayer_AllButBodyAndHead()
    {
        foreach (LookLayer layer in System.Enum.GetValues(typeof(LookLayer)))
            Assert.AreEqual(layer != LookLayer.Body && layer != LookLayer.Head, Looks.IsGarmentLayer(layer), layer.ToString());
    }

    [Test]
    public void EveryKeyComposeEmits_IsRequiredOrABase()
    {
        var known = new HashSet<string>(LookKeys.Required("britain", "industrial", London())
                                              .Concat(LookKeys.Required("japan", "earlymodern", Edo()))
                                              .Concat(LookKeys.Bases(Rules())));
        for (int seed = 0; seed < 300; seed++)
        {
            foreach (TravellerGender g in new[] { TravellerGender.Male, TravellerGender.Female, TravellerGender.Unknown })
            {
                string born = $"1 Jan {Year - 18 - seed % 60}";
                foreach (LookSource leak in new[] { null, Home() })
                    foreach (string name in Names(Compose(g, leak, born, new SeededRandom(seed))))
                        Assert.IsTrue(known.Contains(name), $"seed {seed} {g}: {name}");
            }
        }
    }

    [Test]
    public void Describe_NamesTheDrawsAndTheTell()
    {
        Assert.AreEqual("m skin1 face-a brown; dress tell: Hair 'chasen-mage'", Compose(TravellerGender.Male, Home()).Describe());
        Assert.AreEqual("f skin1 face-a brown", Compose(TravellerGender.Female).Describe());
    }
}
