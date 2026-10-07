using System;
using System.Collections.Generic;

/// <summary>
/// One way to find a stand-in for a character layer with no art yet
/// (LookArtFallbackTable). Serialized in CharacterArtFallbackSO: append only.
/// </summary>
public enum LookArtFallbackStep
{
    /// <summary>A head: the same skin tone with another face (the faces in the face bands' order).</summary>
    OtherFace,

    /// <summary>A body or head: the nearest other skin tone (the lighter first on a tie), the same face first.</summary>
    OtherSkin,

    /// <summary>A garment: the same nation in the nearest other era (the earlier first on a tie).</summary>
    OtherEra,

    /// <summary>A garment: the same era for each of the nation's neighbours, in the table's order.</summary>
    NeighbourNation,

    /// <summary>A garment: any place, the nearest era first, then the nation's neighbours before the other nations.</summary>
    AnyPlace,

    /// <summary>A premade's whole image: the same premade's neutral expression.</summary>
    NeutralExpression,

    /// <summary>A garment of an ID photo's 2150 civilian dress (LookKeys.CivilNation): the same layer and colour, no variant, under each of the "civil" row's neighbours in the latest era (today's 2150 clothes), in the table's order.</summary>
    CivilDress
}

/// <summary>A nation's neighbours for the character art fallback (the nearest first).</summary>
[Serializable]
public sealed class LookArtNeighbours
{
    /// <summary>The nation (a LookKeys nation token: a country id, or a shared art nation such as "neutral").</summary>
    public string nation;

    /// <summary>Its neighbours, the nearest first.</summary>
    public List<string> near = new();
}

/// <summary>The fallback steps of one layer, tried in order.</summary>
[Serializable]
public sealed class LookArtLayerSteps
{
    /// <summary>The layer.</summary>
    public LookLayer layer;

    /// <summary>The steps, tried in order after the key itself; none: a layer without art is not drawn.</summary>
    public List<LookArtFallbackStep> steps = new();
}

/// <summary>
/// Where a character layer with no art yet borrows its picture from
/// (CharacterArtFallbackSO, the knob Saleh edits): each layer's steps and the
/// nations' neighbours. A layer whose steps find nothing is not drawn.
/// </summary>
[Serializable]
public sealed class LookArtFallbackTable
{
    /// <summary>Each nation's neighbours (NeighbourNation, AnyPlace).</summary>
    public List<LookArtNeighbours> neighbours = new();

    /// <summary>Each layer's steps (a layer not listed has none).</summary>
    public List<LookArtLayerSteps> layers = new();

    /// <summary>The steps of a layer (empty when it is not listed).</summary>
    public IReadOnlyList<LookArtFallbackStep> StepsFor(LookLayer layer)
    {
        if (layers != null)
            foreach (LookArtLayerSteps row in layers)
                if (row != null && row.layer == layer && row.steps != null)
                    return row.steps;
        return Array.Empty<LookArtFallbackStep>();
    }

    /// <summary>A nation's neighbours, the nearest first (empty when it is not listed).</summary>
    public IReadOnlyList<string> NeighboursOf(string nation)
    {
        if (neighbours != null)
            foreach (LookArtNeighbours row in neighbours)
                if (row != null && row.nation == nation && row.near != null)
                    return row.near;
        return Array.Empty<string>();
    }
}

/// <summary>What the fallback can choose among: the nations, the eras in order and the faces (from the content library).</summary>
public sealed class LookArtUniverse
{
    /// <summary>Creates a universe; null lists are empty.</summary>
    public LookArtUniverse(IReadOnlyList<string> nations, IReadOnlyList<string> erasInOrder, IReadOnlyList<string> faces)
    {
        Nations = nations ?? Array.Empty<string>();
        Eras = erasInOrder ?? Array.Empty<string>();
        Faces = faces ?? Array.Empty<string>();
    }

    /// <summary>Every nation token a garment can be filed under, in the library's order.</summary>
    public IReadOnlyList<string> Nations { get; }

    /// <summary>Every era id, earliest first.</summary>
    public IReadOnlyList<string> Eras { get; }

    /// <summary>Every face token, in the face bands' order.</summary>
    public IReadOnlyList<string> Faces { get; }
}

/// <summary>
/// The character art fallback (Saleh 2026-09-30: travellers use only the
/// ChatGPT art): a key with no art is drawn with its nearest key that has
/// art, found by its layer's steps in the table; nothing is drawn when no
/// step finds one (the procedural placeholder is retired). A stand-in keeps
/// everything but what its step changes: the layer, the gender, the hair
/// colour (a wig's uncoloured name only meets wigs) and the art variant.
/// Pure, so the order is tested.
/// </summary>
public static class LookArtFallback
{
    /// <summary>
    /// The keys to try for <paramref name="requested"/>, in order: the key
    /// itself, then each step's keys (each name once).
    /// </summary>
    public static IEnumerable<LookKey> Candidates(LookKey requested, LookArtFallbackTable table, LookArtUniverse universe)
    {
        var seen = new HashSet<string> { requested.Name };
        yield return requested;

        if (table == null || universe == null)
            yield break;

        foreach (LookArtFallbackStep step in table.StepsFor(requested.Layer))
            foreach (LookKey key in StepKeys(step, requested, table, universe))
                if (seen.Add(key.Name))
                    yield return key;
    }

    /// <summary>The first of <paramref name="requested"/>'s candidates that <paramref name="hasArt"/> accepts, or null (the layer is not drawn).</summary>
    public static LookKey? Resolve(LookKey requested, LookArtFallbackTable table, LookArtUniverse universe, Func<string, bool> hasArt)
    {
        foreach (LookKey key in Candidates(requested, table, universe))
            if (hasArt(key.Name))
                return key;
        return null;
    }

    /// <summary>One step's keys for a request (none when the step does not apply to its layer).</summary>
    private static IEnumerable<LookKey> StepKeys(LookArtFallbackStep step, LookKey r, LookArtFallbackTable table, LookArtUniverse u)
    {
        bool garment = r.Layer != LookLayer.Body && r.Layer != LookLayer.Head && r.Layer != LookLayer.Whole;
        switch (step)
        {
            case LookArtFallbackStep.OtherFace:
                if (r.Layer == LookLayer.Head)
                    foreach (string face in u.Faces)
                        yield return LookKeys.Head(r.Gender, r.SkinTone, face);
                break;

            case LookArtFallbackStep.OtherSkin:
                if (r.Layer == LookLayer.Body || r.Layer == LookLayer.Head)
                    foreach (int tone in TonesByDistance(r.SkinTone))
                    {
                        if (r.Layer == LookLayer.Body)
                        {
                            yield return LookKeys.Body(r.Gender, tone);
                            continue;
                        }
                        yield return LookKeys.Head(r.Gender, tone, r.Face);
                        foreach (string face in u.Faces)
                            yield return LookKeys.Head(r.Gender, tone, face);
                    }
                break;

            case LookArtFallbackStep.OtherEra:
                if (garment)
                    foreach (string era in ErasByDistance(r.EraId, u.Eras))
                        yield return Same(r, r.NationId, era);
                break;

            case LookArtFallbackStep.NeighbourNation:
                if (garment)
                    foreach (string nation in table.NeighboursOf(r.NationId))
                        yield return Same(r, nation, r.EraId);
                break;

            case LookArtFallbackStep.AnyPlace:
                if (garment)
                    foreach (string era in Prepend(r.EraId, ErasByDistance(r.EraId, u.Eras)))
                        foreach (string nation in NationsByNearness(r.NationId, table, u.Nations))
                            yield return Same(r, nation, era);
                break;

            case LookArtFallbackStep.NeutralExpression:
                if (r.Layer == LookLayer.Whole)
                    yield return LookKeys.Premade(r.PremadeId, LookKeys.NeutralExpression);
                break;

            case LookArtFallbackStep.CivilDress:
                if (garment && r.NationId == LookKeys.CivilNation && u.Eras.Count > 0)
                    foreach (string nation in table.NeighboursOf(LookKeys.CivilNation))
                        yield return LookKeys.Garment(r.Layer, r.Gender, nation, u.Eras[u.Eras.Count - 1], r.HairColour);
                break;
        }
    }

    /// <summary>A garment key like <paramref name="r"/> filed under another nation and era.</summary>
    private static LookKey Same(LookKey r, string nation, string era) =>
        LookKeys.Garment(r.Layer, r.Gender, nation, era, r.HairColour, r.Variant);

    /// <summary>The other skin tones, the nearest first, the lighter (lower) first on a tie.</summary>
    private static IEnumerable<int> TonesByDistance(int tone)
    {
        for (int d = 1; d < LookKeys.SkinTones; d++)
        {
            if (tone - d >= 1)
                yield return tone - d;
            if (tone + d <= LookKeys.SkinTones)
                yield return tone + d;
        }
    }

    /// <summary>The other eras, the nearest first, the earlier first on a tie (none when the era is not listed).</summary>
    private static IEnumerable<string> ErasByDistance(string era, IReadOnlyList<string> eras)
    {
        int at = -1;
        for (int i = 0; i < eras.Count; i++)
            if (eras[i] == era)
                at = i;
        if (at < 0)
            yield break;

        for (int d = 1; d < eras.Count; d++)
        {
            if (at - d >= 0)
                yield return eras[at - d];
            if (at + d < eras.Count)
                yield return eras[at + d];
        }
    }

    /// <summary>The nation itself, its neighbours in the table's order, then every other nation in the universe's order.</summary>
    private static IEnumerable<string> NationsByNearness(string nation, LookArtFallbackTable table, IReadOnlyList<string> nations)
    {
        var seen = new HashSet<string>();
        foreach (string n in Prepend(nation, table.NeighboursOf(nation)))
            if (n != null && seen.Add(n))
                yield return n;
        foreach (string n in nations)
            if (n != null && seen.Add(n))
                yield return n;
    }

    /// <summary>One item, then a sequence.</summary>
    private static IEnumerable<string> Prepend(string first, IEnumerable<string> rest)
    {
        yield return first;
        foreach (string s in rest)
            yield return s;
    }
}
