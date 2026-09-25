using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A traveller's layers in render order, bottom first. The views' serialized
/// arrays are indexed by it, so a change needs Build Office UI. Append only.
/// </summary>
public enum LookLayer
{
    /// <summary>The part of the hairstyle behind the body.</summary>
    HairBack,

    /// <summary>The body (skin tone).</summary>
    Body,

    /// <summary>The outfit.</summary>
    Outfit,

    /// <summary>The head (skin tone and face).</summary>
    Head,

    /// <summary>The facial hair.</summary>
    FacialHair,

    /// <summary>The hairstyle.</summary>
    Hair,

    /// <summary>The headwear.</summary>
    Headwear,

    /// <summary>The accessory.</summary>
    Accessory,

    /// <summary>A premade's whole picture.</summary>
    Whole
}

/// <summary>One layer's art key: its file name and what its placeholder is coloured by. Built only by LookKeys.</summary>
public readonly struct LookKey
{
    /// <summary>The file name (no extension).</summary>
    public readonly string Name;

    /// <summary>The layer it is drawn on.</summary>
    public readonly LookLayer Layer;

    /// <summary>A garment's art nation (its item's LookItem.ArtNation) or a premade's claimed nation id; null for body and head.</summary>
    public readonly string NationId;

    /// <summary>A garment's (or a premade's claimed) era id; null for body and head.</summary>
    public readonly string EraId;

    /// <summary>Body and head: the skin tone (1..5); 0 otherwise.</summary>
    public readonly int SkinTone;

    /// <summary>Hair, hair back and facial hair: the colour (null for a wig and every other layer).</summary>
    public readonly string HairColour;

    /// <summary>A premade's expression (Whole only).</summary>
    public readonly string Expression;

    /// <summary>Creates a key (LookKeys only).</summary>
    internal LookKey(string name, LookLayer layer, string nationId, string eraId, int skinTone, string hairColour, string expression)
    {
        Name = name;
        Layer = layer;
        NationId = nationId;
        EraId = eraId;
        SkinTone = skinTone;
        HairColour = hairColour;
        Expression = expression;
    }
}

/// <summary>One drawn layer of a look.</summary>
public readonly struct LookPart
{
    /// <summary>The layer.</summary>
    public readonly LookLayer Layer;

    /// <summary>Its art key.</summary>
    public readonly LookKey Key;

    /// <summary>Index of its garment in TravellerLook.Garments; -1 for body and head.</summary>
    public readonly int GarmentIndex;

    /// <summary>Creates a part.</summary>
    public LookPart(LookLayer layer, LookKey key, int garmentIndex)
    {
        Layer = layer;
        Key = key;
        GarmentIndex = garmentIndex;
    }
}

/// <summary>One visible garment: what the player can look at and compare with the Costume Guide.</summary>
public sealed class Garment
{
    /// <summary>Its slot.</summary>
    public LookSlot Slot { get; }

    /// <summary>The item's own name ("top hat"), shown when the player looks at it.</summary>
    public string Label { get; }

    /// <summary>The Culture fact of the place the item comes from ("top hat / poke bonnet"): the evidence it is compared on.</summary>
    public string Value { get; }

    /// <summary>True for a liar's dress tell (the true home's signature item).</summary>
    public bool IsTell { get; }

    /// <summary>Creates a garment.</summary>
    public Garment(LookSlot slot, string label, string value, bool isTell)
    {
        Slot = slot;
        Label = label;
        Value = value;
        IsTell = isTell;
    }
}

/// <summary>One place as the look composer sees it: its ids, its wardrobe and its Culture fact.</summary>
public sealed class LookSource
{
    /// <summary>Nation id (keys and placeholder colours).</summary>
    public string NationId;

    /// <summary>Era id.</summary>
    public string EraId;

    /// <summary>Place id (NationEraProfileSO.id; confusable pairs).</summary>
    public string PlaceId;

    /// <summary>What the place wears (null = no wardrobe authored).</summary>
    public PlaceWardrobe Wardrobe;

    /// <summary>The place's Culture fact today (the Costume Guide row).</summary>
    public string CultureValue;
}

/// <summary>How a traveller looks: the layers in render order and the garments the player can look at.</summary>
public sealed class TravellerLook
{
    /// <summary>Creates a look (Looks only).</summary>
    internal TravellerLook(IReadOnlyList<LookPart> parts, IReadOnlyList<Garment> garments, string premadeId,
                           TravellerGender gender, int skinTone, string face, string hairColour)
    {
        Parts = parts;
        Garments = garments;
        PremadeId = premadeId;
        Gender = gender;
        SkinTone = skinTone;
        Face = face;
        HairColour = hairColour;
    }

    /// <summary>The drawn layers, bottom first.</summary>
    public IReadOnlyList<LookPart> Parts { get; }

    /// <summary>The visible garments, in LookSlot order.</summary>
    public IReadOnlyList<Garment> Garments { get; }

    /// <summary>The premade's id, or null for a generated traveller.</summary>
    public string PremadeId { get; }

    /// <summary>The gender the look was drawn for (drawn when the traveller's was Unknown).</summary>
    public TravellerGender Gender { get; }

    /// <summary>Skin tone 1..5 (0 for a premade).</summary>
    public int SkinTone { get; }

    /// <summary>Face token (null for a premade).</summary>
    public string Face { get; }

    /// <summary>Hair colour (null for a premade).</summary>
    public string HairColour { get; }

    /// <summary>The part on a layer, or null.</summary>
    public LookPart? PartOn(LookLayer layer)
    {
        foreach (LookPart part in Parts)
            if (part.Layer == layer)
                return part;
        return null;
    }

    /// <summary>Every part's key, bottom first.</summary>
    public IEnumerable<LookKey> Keys => Parts.Select(p => p.Key);

    /// <summary>A premade's whole image for an expression (blank or unknown = neutral).</summary>
    /// <exception cref="System.InvalidOperationException">The look is not a premade's.</exception>
    public LookKey WholeKey(string expression)
    {
        LookPart? whole = PartOn(LookLayer.Whole);
        if (PremadeId == null || whole == null)
            throw new System.InvalidOperationException("Only a premade has a whole image.");

        string e = expression != null && LookKeys.Expressions.Contains(expression) ? expression : LookKeys.NeutralExpression;
        return LookKeys.Premade(PremadeId, e, whole.Value.Key.NationId, whole.Value.Key.EraId);
    }

    /// <summary>For the case log: "m skin3 face-b brown; dress tell: Headwear 'top hat'" or "premade socrates".</summary>
    public string Describe()
    {
        if (PremadeId != null)
            return $"premade {PremadeId}";

        string g = Gender == TravellerGender.Unknown ? "?" : LookKeys.GenderToken(Gender);
        string text = $"{g} skin{SkinTone} face-{Face} {HairColour}";
        foreach (Garment garment in Garments)
            if (garment.IsTell)
                text += $"; dress tell: {garment.Slot} '{garment.Label}'";
        return text;
    }
}

/// <summary>
/// How travellers look: composing a generated traveller's layers from the
/// claimed place (and, for a liar with a dress tell, one garment of the true
/// home), a premade's whole picture, which garment may leak, and the Culture
/// value a wardrobe gives. Pure and seeded, so every rule and the draw order
/// are tested headless.
/// </summary>
public static class Looks
{
    /// <summary>The one home of "dress evidence is Culture".</summary>
    public const ClueCategory EvidenceCategory = ClueCategory.Culture;

    /// <summary>The longest item label (the wheel's look menu and the compare bar show it).</summary>
    public const int MaxLabelLength = 24;

    /// <summary>Every garment slot, in order.</summary>
    public static readonly IReadOnlyList<LookSlot> Slots =
        new[] { LookSlot.Outfit, LookSlot.Hair, LookSlot.FacialHair, LookSlot.Headwear, LookSlot.Accessory };

    /// <summary>The player-facing name of a slot.</summary>
    public static string SlotLabel(LookSlot slot)
    {
        switch (slot)
        {
            case LookSlot.Outfit: return "Outfit";
            case LookSlot.Hair: return "Hair";
            case LookSlot.FacialHair: return "Facial hair";
            case LookSlot.Headwear: return "Headwear";
            default: return "Accessory";
        }
    }

    /// <summary>The layer a slot's item is drawn on.</summary>
    public static LookLayer LayerOf(LookSlot slot)
    {
        switch (slot)
        {
            case LookSlot.Outfit: return LookLayer.Outfit;
            case LookSlot.Hair: return LookLayer.Hair;
            case LookSlot.FacialHair: return LookLayer.FacialHair;
            case LookSlot.Headwear: return LookLayer.Headwear;
            default: return LookLayer.Accessory;
        }
    }

    /// <summary>
    /// A place's Culture fact: "{men's signature label} / {women's signature
    /// label}", or the one label when both match (the scanner comparison).
    /// Null when the wardrobe or either signature item is missing.
    /// </summary>
    public static string CultureValue(PlaceWardrobe wardrobe)
    {
        LookItem m = wardrobe?.male?.Signature;
        LookItem f = wardrobe?.female?.Signature;
        if (m == null || f == null || !m.IsPresent || !f.IsPresent)
            return null;

        string male = m.label.Trim();
        string female = f.label.Trim();
        return DiscrepancyLog.ValuesMatch(male, female) ? male : $"{male} / {female}";
    }

    /// <summary>
    /// Label clashes that would let a garment read as another place's
    /// (compared with the scanner comparison), one message each naming both
    /// places, the gender and the label: two places with the same signature
    /// label for a gender; an item of one place (any slot) labelled like
    /// another place's signature for the same gender; any label holding "/".
    /// </summary>
    public static List<string> LabelProblems(IReadOnlyList<(string placeId, PlaceWardrobe wardrobe)> places)
    {
        var problems = new List<string>();
        if (places == null)
            return problems;

        foreach ((string placeId, PlaceWardrobe wardrobe) in places)
        {
            if (wardrobe == null)
                continue;

            foreach (TravellerGender gender in new[] { TravellerGender.Male, TravellerGender.Female })
                foreach (LookSlot slot in Slots)
                {
                    LookItem item = wardrobe.For(gender).Item(slot);
                    if (item != null && item.IsPresent && item.label.Contains("/"))
                        problems.Add($"'{placeId}' {LookKeys.GenderToken(gender)} {SlotLabel(slot)} '{item.label}' holds '/', which separates the two labels of a Culture value.");
                }
        }

        for (int a = 0; a < places.Count; a++)
        {
            for (int b = 0; b < places.Count; b++)
            {
                if (a == b || places[a].wardrobe == null || places[b].wardrobe == null)
                    continue;

                foreach (TravellerGender gender in new[] { TravellerGender.Male, TravellerGender.Female })
                {
                    string g = LookKeys.GenderToken(gender);
                    LookItem signature = places[b].wardrobe.For(gender).Signature;
                    if (signature == null || !signature.IsPresent)
                        continue;

                    GenderLook other = places[a].wardrobe.For(gender);
                    if (a < b && other.Signature != null && other.Signature.IsPresent && DiscrepancyLog.ValuesMatch(other.Signature.label, signature.label))
                    {
                        problems.Add($"'{places[a].placeId}' and '{places[b].placeId}' share the {g} signature label '{signature.label}'.");
                        continue;
                    }

                    foreach (LookSlot slot in Slots)
                    {
                        LookItem item = other.Item(slot);
                        if (slot == other.signature || item == null || !item.IsPresent)
                            continue;
                        if (DiscrepancyLog.ValuesMatch(item.label, signature.label))
                            problems.Add($"'{places[a].placeId}' {g} {SlotLabel(slot)} '{item.label}' is labelled like the {g} signature of '{places[b].placeId}'.");
                    }
                }
            }
        }

        return problems;
    }

    /// <summary>
    /// Whether the true home's signature item for <paramref name="gender"/> can
    /// leak onto the claimed look, in order: not for an Unknown gender or a
    /// missing wardrobe; not when the signature is an absence, the whole
    /// outfit, or not leakable; not when a claim item in another slot covers
    /// the signature slot; not when the claim's own item in that slot is drawn
    /// from the same art (the same file name, both wigs or neither: the leak
    /// would be invisible); not when that item is labelled the same; not when
    /// the two places are confusable in that slot; otherwise yes.
    /// </summary>
    public static bool CanLeak(LookSource claim, LookSource home, TravellerGender gender, LookRules rules)
    {
        if (gender == TravellerGender.Unknown || claim?.Wardrobe == null || home?.Wardrobe == null)
            return false;

        GenderLook homeLook = home.Wardrobe.For(gender);
        LookSlot slot = homeLook.signature;
        LookItem item = homeLook.Signature;
        if (item == null || !item.IsPresent)
            return false;
        if (slot == LookSlot.Outfit || !item.leakable)
            return false;

        GenderLook claimLook = claim.Wardrobe.For(gender);
        foreach (LookSlot other in Slots)
        {
            LookItem worn = claimLook.Item(other);
            if (other != slot && worn != null && worn.IsPresent && worn.covers != null && worn.covers.Contains(slot))
                return false;
        }

        LookItem own = claimLook.Item(slot);
        if (own != null && own.IsPresent && own.wig == item.wig && ArtName(slot, gender, own, claim) == ArtName(slot, gender, item, home))
            return false;

        if (own != null && own.IsPresent && DiscrepancyLog.ValuesMatch(own.label, item.label))
            return false;

        if (rules != null && rules.IsConfusable(claim.PlaceId, home.PlaceId, slot, gender))
            return false;

        return true;
    }

    /// <summary>
    /// A generated traveller's look. With no wardrobe: body and head only
    /// (skin 3, the first face, Male when Unknown), no garments, no draws.
    /// Otherwise the draws, in this fixed order: the gender when Unknown (one
    /// Value); the skin tone (one weighted pick; tone 3 and no draw when every
    /// weight is 0); the face (one Range over the age band's faces); the hair
    /// colour (one weighted pick; brown and no draw with no weights), grey from
    /// rules.greyFromAge. Age is the claim's year minus the cover birth year
    /// (the youngest band when unreadable). Every slot wears the claim's item,
    /// except that <paramref name="leakFrom"/> (a dress tell) puts its
    /// signature item in its signature slot; an item hidden by another's
    /// covers is not drawn. Each part is filed under its item's art nation
    /// (LookItem.ArtNation, else its source's nation) and its source's era.
    /// Hair and hair back take the colour unless the hair is a wig; facial
    /// hair always does. Garments are listed in slot order, valued with their
    /// source's Culture fact.
    /// </summary>
    public static TravellerLook Compose(LookSource claim, LookSource leakFrom, TravellerGender gender, string coverBirthDate,
                                        int claimYear, LookWeights weights, LookRules rules, IRandomSource rng)
    {
        rules = rules ?? new LookRules();

        if (claim == null || claim.Wardrobe == null)
        {
            TravellerGender g0 = gender == TravellerGender.Unknown ? TravellerGender.Male : gender;
            string face0 = FirstFace(rules);
            var minimal = new List<LookPart>
            {
                new LookPart(LookLayer.Body, LookKeys.Body(g0, DefaultSkin), -1),
                new LookPart(LookLayer.Head, LookKeys.Head(g0, DefaultSkin, face0), -1)
            };
            return new TravellerLook(minimal, new Garment[0], null, g0, DefaultSkin, face0, LookKeys.Brown);
        }

        // --- Draws (fixed order) ---
        TravellerGender g = gender;
        if (g == TravellerGender.Unknown)
            g = rng.Value() < 0.5f ? TravellerGender.Male : TravellerGender.Female;

        int skin = PickSkin(weights, rng);
        bool ageKnown = BirthDates.TryAgeAt(coverBirthDate, claimYear, out int age);
        FaceBand band = BandFor(rules, ageKnown, age);
        string face = band != null && band.faces != null && band.faces.Count > 0 ? band.faces[rng.Range(0, band.faces.Count)] : FirstFace(rules);
        HairColourWeight hair = weights != null ? WeightedRandom.Pick(weights.hair, w => w != null ? w.weight : 0f, rng) : null;
        string colour = hair != null && !string.IsNullOrEmpty(hair.colour) ? hair.colour : LookKeys.Brown;
        if (ageKnown && rules.greyFromAge > 0 && age >= rules.greyFromAge)
            colour = LookKeys.Grey;

        // --- Items: the claim's, one slot possibly from the true home ---
        GenderLook own = claim.Wardrobe.For(g);
        GenderLook homeLook = leakFrom?.Wardrobe?.For(g);
        var items = new Dictionary<LookSlot, (LookItem item, LookSource source)>();
        foreach (LookSlot slot in Slots)
        {
            bool leaked = homeLook != null && slot == homeLook.signature;
            LookItem item = leaked ? homeLook.Signature : own.Item(slot);
            if (item != null && item.IsPresent)
                items[slot] = (item, leaked ? leakFrom : claim);
        }

        // --- Coverage: an item another worn item covers is not drawn ---
        var covered = new HashSet<LookSlot>();
        foreach (KeyValuePair<LookSlot, (LookItem item, LookSource source)> worn in items)
            if (worn.Value.item.covers != null)
                foreach (LookSlot c in worn.Value.item.covers)
                    if (c != worn.Key)
                        covered.Add(c);
        foreach (LookSlot c in covered)
            items.Remove(c);

        // --- Garments, in slot order ---
        var garments = new List<Garment>();
        var garmentIndex = new Dictionary<LookSlot, int>();
        foreach (LookSlot slot in Slots)
        {
            if (!items.TryGetValue(slot, out (LookItem item, LookSource source) worn))
                continue;

            garmentIndex[slot] = garments.Count;
            garments.Add(new Garment(slot, worn.item.label.Trim(), worn.source.CultureValue, leakFrom != null && worn.source == leakFrom));
        }

        // --- Parts, bottom first ---
        var parts = new List<LookPart>();
        bool hairDrawn = items.TryGetValue(LookSlot.Hair, out (LookItem item, LookSource source) hairItem);
        string hairColour = hairDrawn && hairItem.item.wig ? null : colour;
        if (hairDrawn && hairItem.item.back)
            parts.Add(new LookPart(LookLayer.HairBack, LookKeys.Garment(LookLayer.HairBack, g, hairItem.item.ArtNation(hairItem.source.NationId), hairItem.source.EraId, hairColour), garmentIndex[LookSlot.Hair]));
        parts.Add(new LookPart(LookLayer.Body, LookKeys.Body(g, skin), -1));
        AddGarmentPart(parts, items, garmentIndex, LookSlot.Outfit, g, null);
        parts.Add(new LookPart(LookLayer.Head, LookKeys.Head(g, skin, face), -1));
        AddGarmentPart(parts, items, garmentIndex, LookSlot.FacialHair, g, colour);
        AddGarmentPart(parts, items, garmentIndex, LookSlot.Hair, g, hairColour);
        AddGarmentPart(parts, items, garmentIndex, LookSlot.Headwear, g, null);
        AddGarmentPart(parts, items, garmentIndex, LookSlot.Accessory, g, null);

        return new TravellerLook(parts, garments, null, g, skin, face, colour);
    }

    /// <summary>A premade's look: one whole picture (neutral), one garment (the whole-figure label, the claim's Culture value, never a tell). No draws.</summary>
    public static TravellerLook Whole(string premadeId, LookSource claim, LookRules rules)
    {
        var parts = new[] { new LookPart(LookLayer.Whole, LookKeys.Premade(premadeId, LookKeys.NeutralExpression, claim?.NationId, claim?.EraId), 0) };
        var garments = new[] { new Garment(LookSlot.Outfit, rules?.wholeFigureLabel ?? string.Empty, claim?.CultureValue, false) };
        return new TravellerLook(parts, garments, premadeId, TravellerGender.Unknown, 0, null, null);
    }

    /// <summary>The skin tone of a look with no weights to draw from.</summary>
    private const int DefaultSkin = 3;

    /// <summary>Adds a garment slot's part when its item is drawn (its key uses the item's art nation and its source's era).</summary>
    private static void AddGarmentPart(List<LookPart> parts, Dictionary<LookSlot, (LookItem item, LookSource source)> items,
                                       Dictionary<LookSlot, int> garmentIndex, LookSlot slot, TravellerGender g, string colour)
    {
        if (!items.TryGetValue(slot, out (LookItem item, LookSource source) worn))
            return;

        LookLayer layer = LayerOf(slot);
        parts.Add(new LookPart(layer, LookKeys.Garment(layer, g, worn.item.ArtNation(worn.source.NationId), worn.source.EraId, colour), garmentIndex[slot]));
    }

    /// <summary>An item's uncoloured art name in a slot: its art nation and its place's era (CanLeak's same-art row compares two).</summary>
    private static string ArtName(LookSlot slot, TravellerGender gender, LookItem item, LookSource source) =>
        LookKeys.Garment(LayerOf(slot), gender, item.ArtNation(source.NationId), source.EraId, null).Name;

    /// <summary>One weighted pick of a skin tone (1..5); 3 with no draw when every weight is 0 or missing.</summary>
    private static int PickSkin(LookWeights weights, IRandomSource rng)
    {
        float[] skin = weights != null ? weights.skin : null;
        if (skin == null || skin.All(w => w <= 0f))
            return DefaultSkin;

        var tones = Enumerable.Range(0, LookKeys.SkinTones).ToList();
        return 1 + WeightedRandom.Pick(tones, i => i < skin.Length ? skin[i] : 0f, rng);
    }

    /// <summary>The last band whose minimum age the traveller reached (the first band when the age is unknown or below every band).</summary>
    private static FaceBand BandFor(LookRules rules, bool ageKnown, int age)
    {
        if (rules.faceBands == null || rules.faceBands.Count == 0)
            return null;

        FaceBand band = rules.faceBands[0];
        if (!ageKnown)
            return band;

        foreach (FaceBand b in rules.faceBands)
            if (b != null && b.minAge <= age)
                band = b;
        return band;
    }

    /// <summary>The first face of the first band ("a" when none is authored).</summary>
    private static string FirstFace(LookRules rules)
    {
        FaceBand first = rules.faceBands != null && rules.faceBands.Count > 0 ? rules.faceBands[0] : null;
        return first != null && first.faces != null && first.faces.Count > 0 ? first.faces[0] : "a";
    }
}
