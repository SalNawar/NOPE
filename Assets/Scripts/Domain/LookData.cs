using System;
using System.Collections.Generic;

// Serializable look content. NationEraProfileSO (wardrobe, weights) and
// ContentLibrarySO (rules) hold these directly, so the rules read them with
// no projection. Generate World writes them from world_source.json; an empty
// label means "no item" (Unity serialises an absent item as a default object).

/// <summary>A garment slot of a traveller's look. Serialized: append only.</summary>
public enum LookSlot
{
    /// <summary>The whole outfit (clothes from the shoulders down).</summary>
    Outfit,

    /// <summary>The hairstyle (or a wig).</summary>
    Hair,

    /// <summary>A beard or moustache.</summary>
    FacialHair,

    /// <summary>A hat, cap, veil or headdress.</summary>
    Headwear,

    /// <summary>A collar, necklace, brooch, bag or other worn item.</summary>
    Accessory
}

/// <summary>One item a place's travellers wear in a slot ("top hat").</summary>
[Serializable]
public sealed class LookItem
{
    /// <summary>The item's short name, shown when the player looks at it; empty = no item in this slot.</summary>
    public string label;

    /// <summary>True when the item can leak onto a disguise (readable on its own, not hanging off another layer).</summary>
    public bool leakable;

    /// <summary>Hair only: a wig, drawn in its own colour (no colour variants).</summary>
    public bool wig;

    /// <summary>Hair only: the hairstyle has a part behind the body (the hair-back layer).</summary>
    public bool back;

    /// <summary>Garment slots this item hides (a turban covers the hair).</summary>
    public List<LookSlot> covers = new();

    /// <summary>True when there is an item (a non-blank label).</summary>
    public bool IsPresent => !string.IsNullOrWhiteSpace(label);
}

/// <summary>What one gender of a place wears, and which slot holds its signature item.</summary>
[Serializable]
public sealed class GenderLook
{
    /// <summary>The slot of the signature item: the Costume Guide entry and the only item a disguise can leak.</summary>
    public LookSlot signature;

    /// <summary>The outfit.</summary>
    public LookItem outfit = new();

    /// <summary>The hairstyle.</summary>
    public LookItem hair = new();

    /// <summary>The facial hair (usually none for women).</summary>
    public LookItem facialHair = new();

    /// <summary>The headwear.</summary>
    public LookItem headwear = new();

    /// <summary>The accessory.</summary>
    public LookItem accessory = new();

    /// <summary>The item in a slot (never null for a deserialised look).</summary>
    public LookItem Item(LookSlot slot)
    {
        switch (slot)
        {
            case LookSlot.Outfit: return outfit;
            case LookSlot.Hair: return hair;
            case LookSlot.FacialHair: return facialHair;
            case LookSlot.Headwear: return headwear;
            default: return accessory;
        }
    }

    /// <summary>The signature item.</summary>
    public LookItem Signature => Item(signature);
}

/// <summary>A place's wardrobe: what its men and its women wear.</summary>
[Serializable]
public sealed class PlaceWardrobe
{
    /// <summary>The men's look.</summary>
    public GenderLook male = new();

    /// <summary>The women's look.</summary>
    public GenderLook female = new();

    /// <summary>The look of a gender; null for Unknown (callers resolve the gender first).</summary>
    public GenderLook For(TravellerGender gender) =>
        gender == TravellerGender.Male ? male : gender == TravellerGender.Female ? female : null;
}

/// <summary>One hair colour's weight among a place's travellers.</summary>
[Serializable]
public sealed class HairColourWeight
{
    /// <summary>A LookKeys hair colour other than grey (grey comes only with age).</summary>
    public string colour;

    /// <summary>Relative weight (non-negative).</summary>
    public float weight;
}

/// <summary>A place's skin-tone and hair-colour weights (never a tell).</summary>
[Serializable]
public sealed class LookWeights
{
    /// <summary>Weights of skin tones 1..5 (index 0 = tone 1).</summary>
    public float[] skin = new float[LookKeys.SkinTones];

    /// <summary>Hair colour weights.</summary>
    public List<HairColourWeight> hair = new();
}

/// <summary>Faces of travellers from an age up (until the next band).</summary>
[Serializable]
public sealed class FaceBand
{
    /// <summary>The youngest age of the band.</summary>
    public int minAge;

    /// <summary>One-letter face tokens drawn for this band (at least one).</summary>
    public List<string> faces = new();
}

/// <summary>Two places whose items in a slot look alike, so neither may leak onto the other's disguise in that slot.</summary>
[Serializable]
public sealed class ConfusablePair
{
    /// <summary>One place's id (NationEraProfileSO.id).</summary>
    public string placeA;

    /// <summary>The other place's id.</summary>
    public string placeB;

    /// <summary>The slot whose items look alike.</summary>
    public LookSlot slot;

    /// <summary>"m", "f", or empty for both genders.</summary>
    public string gender;
}

/// <summary>The look knobs shared by every traveller (face bands, grey hair, the premade garment label, confusable pairs).</summary>
[Serializable]
public sealed class LookRules
{
    /// <summary>Face bands by ascending minimum age.</summary>
    public List<FaceBand> faceBands = new();

    /// <summary>From this age a traveller's hair and beard are grey.</summary>
    public int greyFromAge;

    /// <summary>The one garment a premade shows ("Period dress").</summary>
    public string wholeFigureLabel;

    /// <summary>Pairs of places whose items in a slot look alike.</summary>
    public List<ConfusablePair> confusable = new();

    /// <summary>True when the two places (either order) are confusable in the slot for the gender (a pair with no gender counts for both).</summary>
    public bool IsConfusable(string placeA, string placeB, LookSlot slot, TravellerGender gender)
    {
        if (confusable == null)
            return false;

        foreach (ConfusablePair pair in confusable)
        {
            if (pair == null || pair.slot != slot)
                continue;

            bool samePlaces = (pair.placeA == placeA && pair.placeB == placeB) || (pair.placeA == placeB && pair.placeB == placeA);
            bool forGender = string.IsNullOrEmpty(pair.gender) ||
                             (gender != TravellerGender.Unknown && pair.gender == LookKeys.GenderToken(gender));
            if (samePlaces && forGender)
                return true;
        }

        return false;
    }
}
