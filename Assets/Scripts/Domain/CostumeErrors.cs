using System;
using System.Collections.Generic;

/// <summary>
/// What a 2150 citizen's costume gets wrong (traveller types C2): one wrong
/// item worn to the destination, or the present's clothes. Runtime only (not
/// serialized).
/// </summary>
public enum CostumeError
{
    /// <summary>Dressed right for the destination.</summary>
    None,

    /// <summary>One signature item of another of today's places (Looks.CanLeak), over the destination's costume.</summary>
    OtherPlace,

    /// <summary>The present's whole look: 2150 clothes, every garment wrong.</summary>
    PresentClothes,

    /// <summary>One item of the 2150 accessory kit, over an otherwise right costume.</summary>
    PresentAccessory
}

/// <summary>The weight of each costume error variant (world_source.json looks.costumeErrors; 0 = never).</summary>
[Serializable]
public sealed class CostumeErrorWeights
{
    /// <summary>Weight of another place's item.</summary>
    public float otherPlace;

    /// <summary>Weight of the present's clothes.</summary>
    public float presentClothes;

    /// <summary>Weight of a 2150 accessory.</summary>
    public float presentAccessory;

    /// <summary>A variant's weight (0 for None).</summary>
    public float Of(CostumeError error)
    {
        switch (error)
        {
            case CostumeError.OtherPlace: return otherPlace;
            case CostumeError.PresentClothes: return presentClothes;
            case CostumeError.PresentAccessory: return presentAccessory;
            default: return 0f;
        }
    }
}

/// <summary>A traveller's planned costume error: the variant and its source (an index into the caller's candidates).</summary>
public readonly struct CostumePlan
{
    /// <summary>The variant (None: dressed right).</summary>
    public readonly CostumeError Error;

    /// <summary>The other place's or the kit item's index among the candidates; -1 for None and the present's clothes.</summary>
    public readonly int SourceIndex;

    /// <summary>True when the traveller was to err (the roll said yes, or the error was planned), even if no variant could show.</summary>
    public readonly bool Rolled;

    /// <summary>Creates a plan.</summary>
    public CostumePlan(CostumeError error, int sourceIndex, bool rolled)
    {
        Error = error;
        SourceIndex = sourceIndex;
        Rolled = rolled;
    }
}

/// <summary>
/// The costume roll (traveller types C2, K5, P4 and §6.4): a 2150 citizen
/// may wear a wrong item to the destination, which would cause a panic
/// there (P5). It is a deviation fault: proven by comparing a garment with
/// the Costume Guide (DiscrepancyLog, unchanged), never read against a rule.
/// Pure and seeded on the fault stream (Seeds.ForFaults), so every draw is
/// tested headless.
/// </summary>
public static class CostumeErrors
{
    /// <summary>The fault reason of a costume error (CaseVerdict.faultReason; its citation is "citation.acceptedWrong.panic").</summary>
    public const string FaultReason = "panic";

    /// <summary>The variants in draw order.</summary>
    private static readonly IReadOnlyList<CostumeError> Variants =
        new[] { CostumeError.OtherPlace, CostumeError.PresentClothes, CostumeError.PresentAccessory };

    /// <summary>True for a 2150 citizen (tourists and labourers): the displaced are dressed by their origin, whose leak is a lie (L7), not a costume error.</summary>
    public static bool MayErr(TravellerKind kind) => kind != TravellerKind.Displaced;

    /// <summary>
    /// A traveller's costume error, in this fixed draw order: the roll (one
    /// Value against <paramref name="chance"/>; skipped when
    /// <paramref name="planned"/>, a guaranteed or forced error); the variant
    /// (none when <paramref name="pinned"/> can show, else one weighted pick
    /// by <paramref name="weights"/> among the variants that can show:
    /// another place with <paramref name="otherPlaces"/> above 0, the present's
    /// clothes when <paramref name="presentClothes"/>, an accessory with
    /// <paramref name="kitItems"/> above 0); then the source (one Range over
    /// the other places or the kit items; none for the clothes). When the
    /// traveller was to err but no variant can show, the plan is None but
    /// Rolled, so the caller can warn. A null <paramref name="rng"/> is None
    /// with no draw. The caller skips the call (no draw) for a traveller with
    /// another fault (K5).
    /// </summary>
    public static CostumePlan Plan(float chance, bool planned, CostumeError pinned, CostumeErrorWeights weights,
                                   int otherPlaces, bool presentClothes, int kitItems, IRandomSource rng)
    {
        if (rng == null)
            return new CostumePlan(CostumeError.None, -1, false);

        if (!planned && !(rng.Value() < chance))
            return new CostumePlan(CostumeError.None, -1, false);

        bool CanShow(CostumeError e) =>
            e == CostumeError.OtherPlace ? otherPlaces > 0
            : e == CostumeError.PresentClothes ? presentClothes
            : e == CostumeError.PresentAccessory && kitItems > 0;

        CostumeError variant = pinned != CostumeError.None && CanShow(pinned)
            ? pinned
            : WeightedRandom.Pick(Variants, e => CanShow(e) && weights != null ? weights.Of(e) : 0f, rng);

        switch (variant)
        {
            case CostumeError.OtherPlace:
                return new CostumePlan(variant, rng.Range(0, otherPlaces), true);
            case CostumeError.PresentAccessory:
                return new CostumePlan(variant, rng.Range(0, kitItems), true);
            default:
                return new CostumePlan(variant, -1, true);
        }
    }
}
