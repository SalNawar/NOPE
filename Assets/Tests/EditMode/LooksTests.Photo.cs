using System.Linq;
using NUnit.Framework;

/// <summary>
/// The papers' photo (Saleh 2026-10-07: "if someone is in disguise, their
/// passport pic shouldn't have them in old costumes"): the same person in the
/// 2150 civilian outfit of their kind and the civilian hair in their colour,
/// no era headwear or accessory, the identity kept; a premade's photo image.
/// </summary>
public partial class LooksTests
{
    [Test]
    public void ThePhoto_KeepsBodyHeadAndFacialHair_SwapsInTheCivilOutfitAndHair_DropsHeadwearAndAccessory()
    {
        TravellerLook look = Compose(TravellerGender.Male);
        TravellerLook photo = Looks.PhotoLook(look, "v2");

        CollectionAssert.AreEqual(new[] { LookLayer.Body, LookLayer.Outfit, LookLayer.Head, LookLayer.FacialHair, LookLayer.Hair }, photo.Parts.Select(p => p.Layer).ToArray(), "drawn bottom first");
        Assert.AreEqual("outfit_m_civil_2150_v2", photo.PartOn(LookLayer.Outfit).Value.Key.Name);
        Assert.AreEqual($"hair_m_civil_2150_{look.HairColour}", photo.PartOn(LookLayer.Hair).Value.Key.Name, "the civilian hair in their own colour");
        Assert.AreEqual(look.PartOn(LookLayer.Body).Value.Key.Name, photo.PartOn(LookLayer.Body).Value.Key.Name);
        Assert.AreEqual(look.PartOn(LookLayer.Head).Value.Key.Name, photo.PartOn(LookLayer.Head).Value.Key.Name);
        Assert.AreEqual(look.PartOn(LookLayer.FacialHair).Value.Key.Name, photo.PartOn(LookLayer.FacialHair).Value.Key.Name);
        Assert.IsNull(photo.PartOn(LookLayer.Headwear), "no top hat");
        Assert.IsNull(photo.PartOn(LookLayer.Accessory), "no watch chain");
        Assert.AreEqual(Looks.IdentityKey(look), Looks.IdentityKey(photo), "the same person: the face check is unchanged");
        Assert.IsEmpty(photo.Garments);
    }

    [Test]
    public void HairAHoodHid_IsStillTheCivilHair_AndTheHairsBackGoes()
    {
        PlaceWardrobe hooded = London();
        hooded.female.headwear = Item("hood", true, false, false, LookSlot.Hair);
        TravellerLook look = Compose(TravellerGender.Female, claim: Claim(hooded));
        Assert.IsNull(look.PartOn(LookLayer.Hair), "the hood hides the hair at the desk");

        TravellerLook photo = Looks.PhotoLook(look, "v1");
        Assert.AreEqual($"hair_f_civil_2150_{look.HairColour}", photo.PartOn(LookLayer.Hair).Value.Key.Name);
        Assert.IsNull(photo.PartOn(LookLayer.HairBack));
        Assert.AreEqual(LookLayer.Hair, photo.Parts.Last().Layer, "the hair over the head");
    }

    [Test]
    public void TheVariant_FollowsTheTravellersKind()
    {
        Assert.AreEqual("v1", Looks.CivilVariant(TravellerKind.RichTourist));
        Assert.AreEqual("v1", Looks.CivilVariant(TravellerKind.PoorTourist));
        Assert.AreEqual("v2", Looks.CivilVariant(TravellerKind.Labourer));
        Assert.AreEqual("v3", Looks.CivilVariant(TravellerKind.Displaced));
    }

    [Test]
    public void AStrangersPhoto_IsStillAStranger_InTheCivilDressToo_AndThePhotoIsDeterministic()
    {
        TravellerLook own = Compose(TravellerGender.Male);
        TravellerLook stranger = Compose(TravellerGender.Male, rng: Draws(skin: 0.95f, face: 1));
        TravellerLook photo = Looks.PhotoLook(stranger, "v1");
        Assert.AreNotEqual(Looks.IdentityKey(own), Looks.IdentityKey(photo));
        Assert.AreEqual(Looks.IdentityKey(stranger), Looks.IdentityKey(photo));
        CollectionAssert.AreEqual(Names(photo), Names(Looks.PhotoLook(stranger, "v1")));
    }

    [Test]
    public void APremadesPhoto_IsTheirPhotoImage_TheSamePerson()
    {
        TravellerLook premade = Looks.Whole("socrates", Claim(), Rules());
        TravellerLook photo = Looks.PhotoLook(premade, "v1");
        Assert.AreEqual("premade_socrates_photo", photo.PartOn(LookLayer.Whole).Value.Key.Name);
        Assert.AreEqual("premade_socrates_photo", photo.WholeKey("angry").Name, "a photo has one expression");
        Assert.AreEqual(Looks.IdentityKey(premade), Looks.IdentityKey(photo));
        Assert.IsNull(Looks.PhotoLook(null, "v1"));
    }
}
