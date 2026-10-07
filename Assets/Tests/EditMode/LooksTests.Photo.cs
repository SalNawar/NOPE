using System.Linq;
using NUnit.Framework;

/// <summary>
/// The papers' photo (Saleh 2026-10-07: "if someone is in disguise, their
/// passport pic shouldn't have them in old costumes"): the same person in
/// 2150 dress, no era headwear or accessory, the identity kept.
/// </summary>
public partial class LooksTests
{
    /// <summary>The present's 2150 clothes: a jumpsuit and a crop (no headwear, no accessory).</summary>
    private static LookSource Present2150() => Source("neutral", "future", new PlaceWardrobe
    {
        male = Look(LookSlot.Outfit, Item("civic jumpsuit"), Item("short crop")),
        female = Look(LookSlot.Outfit, Item("civic tunic"), Item("bob", back: true))
    });

    [Test]
    public void ThePhoto_ReplacesTheCostume_WithThe2150Outfit_AndDropsHeadwearAndAccessory()
    {
        TravellerLook look = Compose(TravellerGender.Male);
        TravellerLook photo = Looks.PhotoLook(look, Present2150());

        Assert.AreEqual("outfit_m_neutral_future", photo.PartOn(LookLayer.Outfit).Value.Key.Name);
        Assert.IsNull(photo.PartOn(LookLayer.Headwear), "no top hat");
        Assert.IsNull(photo.PartOn(LookLayer.Accessory), "no watch chain");
        Assert.AreEqual(look.PartOn(LookLayer.Body).Value.Key.Name, photo.PartOn(LookLayer.Body).Value.Key.Name);
        Assert.AreEqual(look.PartOn(LookLayer.Head).Value.Key.Name, photo.PartOn(LookLayer.Head).Value.Key.Name);
        Assert.AreEqual(look.PartOn(LookLayer.Hair).Value.Key.Name, photo.PartOn(LookLayer.Hair).Value.Key.Name, "their own hair");
        Assert.AreEqual(look.PartOn(LookLayer.FacialHair).Value.Key.Name, photo.PartOn(LookLayer.FacialHair).Value.Key.Name);
        Assert.AreEqual(Looks.IdentityKey(look), Looks.IdentityKey(photo), "the same person: the face check is unchanged");
        Assert.IsEmpty(photo.Garments);
        CollectionAssert.AreEqual(new[] { LookLayer.Body, LookLayer.Outfit, LookLayer.Head, LookLayer.FacialHair, LookLayer.Hair }, photo.Parts.Select(p => p.Layer).ToArray(), "drawn bottom first");
    }

    [Test]
    public void HairAHatHid_IsThe2150Hair_InTheirColour()
    {
        PlaceWardrobe hooded = London();
        hooded.female.headwear = Item("hood", true, false, false, LookSlot.Hair);
        TravellerLook look = Compose(TravellerGender.Female, claim: Claim(hooded));
        Assert.IsNull(look.PartOn(LookLayer.Hair), "the hood hides the hair at the desk");

        TravellerLook photo = Looks.PhotoLook(look, Present2150());
        Assert.AreEqual($"hair_f_neutral_future_{look.HairColour}", photo.PartOn(LookLayer.Hair).Value.Key.Name);
        Assert.AreEqual(LookLayer.HairBack, photo.Parts[0].Layer, "the bob's back under the body");
        Assert.IsNull(photo.PartOn(LookLayer.Headwear));
    }

    [Test]
    public void AStrangersPhoto_IsStillAStranger_In2150DressToo_AndThePhotoIsDeterministic()
    {
        TravellerLook own = Compose(TravellerGender.Male);
        TravellerLook stranger = Compose(TravellerGender.Male, rng: Draws(skin: 0.95f, face: 1));
        TravellerLook photo = Looks.PhotoLook(stranger, Present2150());
        Assert.AreNotEqual(Looks.IdentityKey(own), Looks.IdentityKey(photo));
        Assert.AreEqual(Looks.IdentityKey(stranger), Looks.IdentityKey(photo));
        CollectionAssert.AreEqual(Names(photo), Names(Looks.PhotoLook(stranger, Present2150())));
    }

    [Test]
    public void WithoutADress_ThePhotoKeepsTheOutfit_AndAPremadeKeepsItsWholePicture()
    {
        TravellerLook look = Compose(TravellerGender.Male);
        TravellerLook bare = Looks.PhotoLook(look, null);
        Assert.AreEqual(look.PartOn(LookLayer.Outfit).Value.Key.Name, bare.PartOn(LookLayer.Outfit).Value.Key.Name);
        Assert.IsNull(bare.PartOn(LookLayer.Headwear));
        Assert.IsNull(Looks.PhotoLook(null, Present2150()));
        TravellerLook premade = Looks.Whole("socrates", Claim(), Rules());
        Assert.AreSame(premade, Looks.PhotoLook(premade, Present2150()), "a premade's whole picture has no outfit to change");
    }
}
