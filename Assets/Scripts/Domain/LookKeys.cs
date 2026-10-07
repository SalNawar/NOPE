using System;
using System.Collections.Generic;

/// <summary>
/// The character art's file-name grammar: the only code that writes a
/// character file name (its key). Final art lives at
/// Assets/Art/Characters/Resources/Characters/{key}.png, and a key without art
/// is drawn with its nearest delivered stand-in (LookArtFallback), so art
/// drops in by name. Pure, so every name form and the enumerations (the
/// validator's art report) are tested.
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
        new LookKey($"body_{GenderToken(gender)}_skin{skin}", LookLayer.Body, gender, null, null, skin, null, null, null, null, null);

    /// <summary>The head: "head_{g}_skin{N}_face{v}".</summary>
    public static LookKey Head(TravellerGender gender, int skin, string face) =>
        new LookKey($"head_{GenderToken(gender)}_skin{skin}_face{face}", LookLayer.Head, gender, null, null, skin, face, null, null, null, null);

    /// <summary>
    /// A garment layer: "{layer}_{g}_{nation}_{era}", plus "_{variant}" when
    /// <paramref name="variant"/> is not blank (LookItem.artVariant), then
    /// "_{colour}" when <paramref name="colour"/> is not null.
    /// </summary>
    public static LookKey Garment(LookLayer layer, TravellerGender gender, string nationId, string eraId, string colour, string variant = null)
    {
        string name = $"{LayerToken(layer)}_{GenderToken(gender)}_{nationId}_{eraId}";
        bool hasVariant = !string.IsNullOrWhiteSpace(variant);
        if (hasVariant)
            name += "_" + variant;
        if (colour != null)
            name += "_" + colour;
        return new LookKey(name, layer, gender, nationId, eraId, 0, null, colour, hasVariant ? variant : null, null, null);
    }

    /// <summary>A premade's whole image: "premade_{id}_{expression}".</summary>
    public static LookKey Premade(string premadeId, string expression) =>
        new LookKey($"premade_{premadeId}_{expression}", LookLayer.Whole, TravellerGender.Unknown, null, null, 0, null, null, null, premadeId, expression);

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
                foreach (string colour in Looks.TakesHairColour(slot, item) ? HairColours : new string[] { null })
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
            yield return Premade(premadeId, expression).Name;
    }

    /// <summary>What separates a key's name from its pose frame's id ("outfit_m_egypt_ancient__explaining_a"): two underscores, which no token holds.</summary>
    public const string PoseSeparator = "__";

    /// <summary>
    /// A key drawn in a pose frame (TravellerPose: an instant still frame on a
    /// dialogue beat): "{key}__{pose}", the same layer and parts; a premade's
    /// whole picture is "premade_{id}__{pose}" whatever its expression (GPT
    /// draws a premade's pose frames with the neutral face).
    /// </summary>
    public static LookKey Posed(LookKey key, string pose)
    {
        bool whole = key.Layer == LookLayer.Whole && key.PremadeId != null;
        string name = (whole ? $"premade_{key.PremadeId}" : key.Name) + PoseSeparator + pose;
        return new LookKey(name, key.Layer, key.Gender, key.NationId, key.EraId, key.SkinTone, key.Face, key.HairColour, key.Variant,
                           key.PremadeId, whole ? NeutralExpression : key.Expression, pose);
    }

    /// <summary>The hands of a pose that crosses the face, drawn over everything (TravellerPose.CrossesFace): "hands_{g}_skin{N}__{pose}".</summary>
    public static LookKey Hands(TravellerGender gender, int skin, string pose) =>
        new LookKey($"hands_{GenderToken(gender)}_skin{skin}{PoseSeparator}{pose}", LookLayer.Hands, gender, null, null, skin, null, null, null, null, null, pose);

    /// <summary>
    /// Splits a key name at its pose separator: true with the neutral key's
    /// name ("premade_caesar" for a premade's frame) and the pose id
    /// ("explaining_a"); false, with the name itself and no pose, for a
    /// neutral key or a blank pose.
    /// </summary>
    public static bool TryParsePose(string keyName, out string neutralName, out string pose)
    {
        int at = keyName != null ? keyName.IndexOf(PoseSeparator, StringComparison.Ordinal) : -1;
        if (at <= 0 || at + PoseSeparator.Length >= keyName.Length)
        {
            neutralName = keyName;
            pose = null;
            return false;
        }
        neutralName = keyName.Substring(0, at);
        pose = keyName.Substring(at + PoseSeparator.Length);
        return true;
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
