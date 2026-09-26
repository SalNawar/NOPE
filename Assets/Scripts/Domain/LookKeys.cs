using System;
using System.Collections.Generic;

/// <summary>
/// The character art's file-name grammar: the only code that writes a
/// character file name (its key). Final art lives at
/// Assets/Art/Characters/Resources/Characters/{key}.png, and every key without
/// art is drawn as a placeholder, so art drops in by name. Pure, so every
/// name form and the enumerations (the validator's art report) are tested.
/// </summary>
public static class LookKeys
{
    /// <summary>The men's token.</summary>
    public const string Male = "m";

    /// <summary>The women's token.</summary>
    public const string Female = "f";

    /// <summary>Skin tones 1..SkinTones.</summary>
    public const int SkinTones = 5;

    /// <summary>The grey hair colour (only with age, never weighted).</summary>
    public const string Grey = "grey";

    /// <summary>The brown hair colour (the fallback when a place has no hair weights).</summary>
    public const string Brown = "brown";

    /// <summary>Every hair colour, grey last.</summary>
    public static readonly IReadOnlyList<string> HairColours = new[] { "black", Brown, "blond", "red", Grey };

    /// <summary>The expression a premade shows when nothing else is asked for.</summary>
    public const string NeutralExpression = "neutral";

    /// <summary>Every premade expression, neutral first.</summary>
    public static readonly IReadOnlyList<string> Expressions = new[] { NeutralExpression, "happy", "angry", "worried" };

    /// <summary>The gender's token; Unknown throws (callers resolve the gender first).</summary>
    /// <exception cref="ArgumentException">The gender is Unknown.</exception>
    public static string GenderToken(TravellerGender gender)
    {
        switch (gender)
        {
            case TravellerGender.Male: return Male;
            case TravellerGender.Female: return Female;
            default: throw new ArgumentException("A look key needs a known gender.");
        }
    }

    /// <summary>True for a non-empty token of lowercase ASCII letters and digits (ids in keys must pass: "_" separates tokens).</summary>
    public static bool IsToken(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        foreach (char c in s)
            if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')))
                return false;

        return true;
    }

    /// <summary>The body: "body_{g}_skin{N}".</summary>
    public static LookKey Body(TravellerGender gender, int skin) =>
        new LookKey($"body_{GenderToken(gender)}_skin{skin}", LookLayer.Body, null, null, skin, null, null);

    /// <summary>The head: "head_{g}_skin{N}_face{v}".</summary>
    public static LookKey Head(TravellerGender gender, int skin, string face) =>
        new LookKey($"head_{GenderToken(gender)}_skin{skin}_face{face}", LookLayer.Head, null, null, skin, null, null);

    /// <summary>
    /// A garment layer: "{layer}_{g}_{nation}_{era}", plus "_{variant}" when
    /// <paramref name="variant"/> is not blank (LookItem.artVariant), then
    /// "_{colour}" when <paramref name="colour"/> is not null.
    /// </summary>
    public static LookKey Garment(LookLayer layer, TravellerGender gender, string nationId, string eraId, string colour, string variant = null)
    {
        string name = $"{LayerToken(layer)}_{GenderToken(gender)}_{nationId}_{eraId}";
        if (!string.IsNullOrWhiteSpace(variant))
            name += "_" + variant;
        if (colour != null)
            name += "_" + colour;
        return new LookKey(name, layer, nationId, eraId, 0, colour, null);
    }

    /// <summary>A premade's whole image: "premade_{id}_{expression}" (the claim's ids only colour its placeholder).</summary>
    public static LookKey Premade(string premadeId, string expression, string claimNationId, string claimEraId) =>
        new LookKey($"premade_{premadeId}_{expression}", LookLayer.Whole, claimNationId, claimEraId, 0, null, expression);

    /// <summary>
    /// Every garment key a place's wardrobe can need, both genders: one key per
    /// outfit, headwear and accessory; hair (and hair back) in every colour, or
    /// one uncoloured key each for a wig; facial hair in every colour (a leaked
    /// hairstyle keeps the traveller's colour, so any colour can meet any item).
    /// Each item is filed under its art nation (LookItem.ArtNation), so places
    /// that share a drawing list the same names.
    /// </summary>
    public static IEnumerable<string> Required(string nationId, string eraId, PlaceWardrobe wardrobe)
    {
        if (wardrobe == null)
            yield break;

        foreach (TravellerGender gender in new[] { TravellerGender.Male, TravellerGender.Female })
        {
            GenderLook look = wardrobe.For(gender);
            foreach (LookSlot slot in Looks.Slots)
            {
                LookItem item = look.Item(slot);
                if (item == null || !item.IsPresent)
                    continue;

                LookLayer layer = Looks.LayerOf(slot);
                bool coloured = slot == LookSlot.FacialHair || (slot == LookSlot.Hair && !item.wig);
                foreach (string colour in coloured ? HairColours : new string[] { null })
                {
                    if (slot == LookSlot.Hair && item.back)
                        yield return Garment(LookLayer.HairBack, gender, item.ArtNation(nationId), eraId, colour, item.artVariant).Name;
                    yield return Garment(layer, gender, item.ArtNation(nationId), eraId, colour, item.artVariant).Name;
                }
            }
        }
    }

    /// <summary>
    /// Every key the present's look can need (costume errors): its clothes'
    /// keys (the wardrobe's Required), then each kit accessory's key, men's
    /// then women's, each for its own gender only.
    /// </summary>
    public static IEnumerable<string> PresentRequired(string nationId, string eraId, PresentLook present)
    {
        if (present == null)
            yield break;

        foreach (string key in Required(nationId, eraId, present.wardrobe))
            yield return key;

        foreach (TravellerGender gender in new[] { TravellerGender.Male, TravellerGender.Female })
            foreach (LookItem item in present.Kit(gender))
                if (item != null && item.IsPresent)
                    yield return Garment(LookLayer.Accessory, gender, item.ArtNation(nationId), eraId, null, item.artVariant).Name;
    }

    /// <summary>Every body (2 genders x SkinTones) and every head (2 x SkinTones x every face of the bands).</summary>
    public static IEnumerable<string> Bases(LookRules rules)
    {
        var faces = new List<string>();
        if (rules != null && rules.faceBands != null)
            foreach (FaceBand band in rules.faceBands)
                if (band != null && band.faces != null)
                    foreach (string face in band.faces)
                        if (!faces.Contains(face))
                            faces.Add(face);

        foreach (TravellerGender gender in new[] { TravellerGender.Male, TravellerGender.Female })
        {
            for (int skin = 1; skin <= SkinTones; skin++)
                yield return Body(gender, skin).Name;
            for (int skin = 1; skin <= SkinTones; skin++)
                foreach (string face in faces)
                    yield return Head(gender, skin, face).Name;
        }
    }

    /// <summary>A premade's four expression images.</summary>
    public static IEnumerable<string> PremadeSet(string premadeId)
    {
        foreach (string expression in Expressions)
            yield return Premade(premadeId, expression, null, null).Name;
    }

    /// <summary>The file-name token of a layer.</summary>
    private static string LayerToken(LookLayer layer)
    {
        switch (layer)
        {
            case LookLayer.HairBack: return "hairback";
            case LookLayer.Outfit: return "outfit";
            case LookLayer.FacialHair: return "facialhair";
            case LookLayer.Hair: return "hair";
            case LookLayer.Headwear: return "headwear";
            case LookLayer.Accessory: return "accessory";
            default: throw new ArgumentException($"{layer} is not a garment layer.");
        }
    }
}
