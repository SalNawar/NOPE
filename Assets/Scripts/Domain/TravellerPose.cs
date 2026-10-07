using System;
using System.Collections.Generic;

/// <summary>
/// A traveller's pose (Saleh 2026-10-07): an instant swap of still frames on
/// the interview's dialogue beats, never a tween. The frame holds until the
/// next beat: neutral on arrival, while idle and after a response;
/// explaining on their answer lines; thinking while the wheel is open (they
/// wait on a question); objecting on a challenge's reply and on their
/// reaction to a denial. A frame swaps the moving set of layers (the body,
/// the outfit, the accessory and, for a pose that crosses the face, the
/// hands) for its drawings ("{key}__{pose}", LookKeys.Posed), or a premade's
/// whole picture for its frame; the head, hair and headwear stay. Each
/// outfit is drawn in neutral plus one variant per category (a, b or c), so
/// the frame is the first variant whose outfit drawing exists; when any
/// moving layer of it has no drawing the whole set stays neutral, never a
/// mix. Pure, so every beat and fallback is tested.
/// </summary>
public static class TravellerPose
{
    /// <summary>The resting frame (arms relaxed): arrival, idle, after a response.</summary>
    public const string Neutral = "neutral";

    /// <summary>The answer lines' frame.</summary>
    public const string Explaining = "explaining";

    /// <summary>The frame while the wheel is open (waiting on a question).</summary>
    public const string Thinking = "thinking";

    /// <summary>The frame of a challenge's reply and a reaction to a denial.</summary>
    public const string Objecting = "objecting";

    /// <summary>The categories that have frames, in order.</summary>
    public static readonly IReadOnlyList<string> Categories = new[] { Explaining, Thinking, Objecting };

    /// <summary>A category's drawn variants, in the order a frame is looked for.</summary>
    public static readonly IReadOnlyList<string> Variants = new[] { "a", "b", "c" };

    /// <summary>The poses whose hand crosses the face (GPT's pose families: fingers under the chin, at the cheek): they also need their hands layer, drawn over the head.</summary>
    public static readonly IReadOnlyList<string> CrossesFace = new[] { Id(Thinking, "a"), Id(Thinking, "b") };

    /// <summary>A pose id: "{category}_{variant}" ("thinking_b").</summary>
    public static string Id(string category, string variant) => category + "_" + variant;

    /// <summary>Splits a pose id into its category and variant; false for anything else (neutral included).</summary>
    public static bool TryParse(string pose, out string category, out string variant)
    {
        category = variant = null;
        int at = pose != null ? pose.LastIndexOf('_') : -1;
        if (at <= 0)
            return false;
        string c = pose.Substring(0, at), v = pose.Substring(at + 1);
        if (!Contains(Categories, c) || !Contains(Variants, v))
            return false;
        category = c;
        variant = v;
        return true;
    }

    /// <summary>The category of a line the traveller says: its own gesture (DialogLine.Gesture), else explaining for an answer and neutral for any other line; neutral for none.</summary>
    public static string ForLine(DialogLine line)
    {
        if (line == null)
            return Neutral;
        if (!string.IsNullOrEmpty(line.Gesture))
            return line.Gesture;
        return line.IsAnswer ? Explaining : Neutral;
    }

    /// <summary>
    /// The beat's category: the line the traveller is saying (<paramref name="saying"/>,
    /// null when the bubble shows nothing) when it has a gesture of its own;
    /// otherwise thinking while the wheel is open, else neutral.
    /// </summary>
    public static string ForBeat(DialogLine saying, bool wheelOpen)
    {
        string line = ForLine(saying);
        if (line != Neutral)
            return line;
        return wheelOpen ? Thinking : Neutral;
    }

    /// <summary>
    /// <paramref name="look"/> in a frame of <paramref name="category"/>: a
    /// premade's whole picture swapped for its first variant with a drawing;
    /// a generated traveller's body, outfit and accessory swapped for their
    /// frame of the outfit's first variant with a drawing, plus its hands for
    /// a pose that crosses the face. <paramref name="hasArt"/> says whether a
    /// key has its own drawing (no stand-in). The look itself (neutral) for
    /// the neutral or an unknown category, a look with no outfit or whole
    /// picture, or when any layer of the moving set has no drawing.
    /// </summary>
    public static TravellerLook Posed(TravellerLook look, string category, Func<string, bool> hasArt)
    {
        if (look == null || hasArt == null || !Contains(Categories, category))
            return look;

        if (look.PremadeId != null)
        {
            LookPart? whole = look.PartOn(LookLayer.Whole);
            if (whole == null)
                return look;
            foreach (string variant in Variants)
            {
                LookKey frame = LookKeys.Posed(whole.Value.Key, Id(category, variant));
                if (hasArt(frame.Name))
                    return Replace(look, new Dictionary<LookLayer, LookKey> { { LookLayer.Whole, frame } }, null);
            }
            return look;
        }

        LookPart? outfit = look.PartOn(LookLayer.Outfit);
        if (outfit == null)
            return look;
        foreach (string variant in Variants)
        {
            string pose = Id(category, variant);
            if (!hasArt(LookKeys.Posed(outfit.Value.Key, pose).Name))
                continue;

            var frames = new Dictionary<LookLayer, LookKey>();
            foreach (LookLayer layer in MovingLayers)
            {
                LookPart? part = look.PartOn(layer);
                if (part == null)
                    continue;
                LookKey frame = LookKeys.Posed(part.Value.Key, pose);
                if (!hasArt(frame.Name))
                    return look;
                frames[layer] = frame;
            }
            LookKey? hands = null;
            if (Contains(CrossesFace, pose))
            {
                LookKey h = LookKeys.Hands(look.Gender, look.SkinTone, pose);
                if (!hasArt(h.Name))
                    return look;
                hands = h;
            }
            return Replace(look, frames, hands);
        }
        return look;
    }

    /// <summary>The layers a pose moves (the arms and what they wear); the head, hair, facial hair and headwear stay.</summary>
    private static readonly LookLayer[] MovingLayers = { LookLayer.Body, LookLayer.Outfit, LookLayer.Accessory };

    /// <summary>The look with its parts on the given layers replaced by their frames, and the hands (when any) on top; its garments and identity unchanged.</summary>
    private static TravellerLook Replace(TravellerLook look, Dictionary<LookLayer, LookKey> frames, LookKey? hands)
    {
        var parts = new List<LookPart>(look.Parts.Count + 1);
        foreach (LookPart part in look.Parts)
            parts.Add(frames.TryGetValue(part.Layer, out LookKey frame) ? new LookPart(part.Layer, frame, part.GarmentIndex) : part);
        if (hands.HasValue)
            parts.Add(new LookPart(LookLayer.Hands, hands.Value, -1));
        return new TravellerLook(parts, look.Garments, look.PremadeId, look.Gender, look.SkinTone, look.Face, look.HairColour);
    }

    private static bool Contains(IReadOnlyList<string> list, string value)
    {
        foreach (string s in list)
            if (s == value)
                return true;
        return false;
    }
}

/// <summary>
/// The character art sets (Saleh 2026-10-07: the 1980s anime style replaces
/// the clean 2D anime one as GPT delivers it, place by place). The classic
/// set is Resources/Characters/{key}; the 80s set Resources/Characters/80s/{key}.
/// A traveller is drawn from one set only, never a mix: the 80s set when
/// every key of their look has its own 80s drawing, else the classic set
/// (with its stand-ins). Pure, so the choice is tested.
/// </summary>
public static class LookArtSets
{
    /// <summary>The classic (clean 2D anime) set: the Resources folder itself.</summary>
    public const string Classic = "";

    /// <summary>The 1980s anime set: its sub-folder.</summary>
    public const string Retro = "80s";

    /// <summary>The set to draw <paramref name="look"/> from: Retro when it has parts and <paramref name="hasRetro"/> holds for every key, else Classic.</summary>
    public static string For(TravellerLook look, Func<string, bool> hasRetro)
    {
        if (look == null || hasRetro == null || look.Parts.Count == 0)
            return Classic;
        foreach (LookPart part in look.Parts)
            if (!hasRetro(part.Key.Name))
                return Classic;
        return Retro;
    }

    /// <summary>A key's path inside the character Resources folder in a set ("80s/body_m_skin1"; the name alone in the classic set).</summary>
    public static string PathOf(string set, string keyName) => string.IsNullOrEmpty(set) ? keyName : set + "/" + keyName;
}
