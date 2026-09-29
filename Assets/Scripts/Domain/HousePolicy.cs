using System;
using System.Collections.Generic;

/// <summary>A house upgrade as the simulation's buyer sees it tonight: its id, the price it would be charged, and whether it is owned and unlocked (every prerequisite owned).</summary>
public readonly struct HouseOffer
{
    /// <summary>The upgrade's id.</summary>
    public readonly string Id;

    /// <summary>Tonight's price (HomeEconomy.UpgradeCost).</summary>
    public readonly int Price;

    /// <summary>True when the house already owns it.</summary>
    public readonly bool Owned;

    /// <summary>True when every prerequisite is owned.</summary>
    public readonly bool Unlocked;

    /// <summary>An offer.</summary>
    public HouseOffer(string id, int price, bool owned, bool unlocked)
    {
        Id = id ?? string.Empty;
        Price = price;
        Owned = owned;
        Unlocked = unlocked;
    }
}

/// <summary>
/// The balance simulation's buyer at Home (the Home upgrades spec HU10, §9;
/// Tools > TimeDesk > Balance): each night, care first for the sickest member
/// at the threshold, then the cheapest house upgrade it may buy, each only
/// while the wallet keeps a reserve (a night's household), so upkeep never
/// walks it into bankruptcy on purpose. Pure, so the picks are tested headless.
/// </summary>
public static class HousePolicy
{
    /// <summary>The id of the cheapest offer not owned and unlocked whose price leaves at least <paramref name="reserve"/> of <paramref name="money"/> (a tie by price goes to the first id in ordinal order); null when none.</summary>
    public static string Purchase(IReadOnlyList<HouseOffer> offers, int money, int reserve)
    {
        string best = null;
        int bestPrice = int.MaxValue;
        foreach (HouseOffer o in offers ?? Array.Empty<HouseOffer>())
        {
            if (o.Owned || !o.Unlocked || o.Id.Length == 0 || money - o.Price < reserve)
                continue;
            if (o.Price < bestPrice || (o.Price == bestPrice && string.CompareOrdinal(o.Id, best) < 0))
            {
                best = o.Id;
                bestPrice = o.Price;
            }
        }
        return best;
    }

    /// <summary>The index of the sickest member whose condition is at least <paramref name="threshold"/> (a tie goes to the first) when treating them at <paramref name="careCost"/> leaves at least <paramref name="reserve"/> of <paramref name="money"/>; -1 otherwise.</summary>
    public static int Care(IReadOnlyList<int> conditions, int money, int careCost, int reserve, int threshold)
    {
        if (conditions == null || money - careCost < reserve)
            return -1;

        int best = -1;
        for (int i = 0; i < conditions.Count; i++)
            if (conditions[i] >= threshold && (best < 0 || conditions[i] > conditions[best]))
                best = i;
        return best;
    }
}
