using System;
using System.Collections.Generic;

/// <summary>A house upgrade as the simulation's buyer sees it tonight: its id, the price it would be charged, whether it is owned and unlocked (every prerequisite owned), and its prerequisites.</summary>
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

    /// <summary>The ids of its prerequisites (UpgradeSO.requires; empty when it needs nothing).</summary>
    public readonly IReadOnlyList<string> Requires;

    /// <summary>An offer.</summary>
    public HouseOffer(string id, int price, bool owned, bool unlocked, IReadOnlyList<string> requires = null)
    {
        Id = id ?? string.Empty;
        Price = price;
        Owned = owned;
        Unlocked = unlocked;
        Requires = requires ?? Array.Empty<string>();
    }
}

/// <summary>
/// The balance simulation's buyer at Home (the Home upgrades spec HU10, §9;
/// Tools > TimeDesk > Balance): each night, after the pet's bills
/// (PetPolicy), the cheapest house upgrade it may buy, only while the wallet
/// keeps a reserve (a night's household), so upkeep never walks it into
/// bankruptcy on purpose. A second buyer, the climber (Saleh's
/// Q6: "balance so you can buy one or two if you take bribes"), saves for
/// the top tier instead. Pure, so the picks are tested headless.
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

    /// <summary>
    /// The climber's next purchase: of the top-tier offers not owned (priced
    /// at <paramref name="topTierPrice"/> or more), the one whose path (itself
    /// and every prerequisite not owned, followed through the offers) costs
    /// least (a tie goes to the first id in ordinal order); once
    /// <paramref name="money"/> covers that whole path and still keeps
    /// <paramref name="reserve"/>, the path's cheapest unlocked offer (its first
    /// step), else nothing: the climber buys nothing else, it saves. Null when
    /// every top-tier offer is owned or the path is out of reach tonight.
    /// </summary>
    public static string Climb(IReadOnlyList<HouseOffer> offers, int money, int reserve, int topTierPrice)
    {
        var byId = new Dictionary<string, HouseOffer>();
        foreach (HouseOffer o in offers ?? Array.Empty<HouseOffer>())
            if (o.Id.Length > 0 && !byId.ContainsKey(o.Id))
                byId[o.Id] = o;

        List<HouseOffer> best = null;
        int bestCost = int.MaxValue;
        string bestId = null;
        foreach (HouseOffer o in byId.Values)
        {
            if (o.Owned || o.Price < topTierPrice)
                continue;
            List<HouseOffer> path = Path(o, byId);
            int cost = 0;
            foreach (HouseOffer step in path)
                cost += step.Price;
            if (cost < bestCost || (cost == bestCost && string.CompareOrdinal(o.Id, bestId) < 0))
            {
                best = path;
                bestCost = cost;
                bestId = o.Id;
            }
        }
        if (best == null || money - bestCost < reserve)
            return null;

        HouseOffer? next = null;
        foreach (HouseOffer step in best)
            if (step.Unlocked && (next == null || step.Price < next.Value.Price || (step.Price == next.Value.Price && string.CompareOrdinal(step.Id, next.Value.Id) < 0)))
                next = step;
        return next?.Id;
    }

    /// <summary>An offer and every prerequisite of it not owned, each once, followed through <paramref name="byId"/> (a prerequisite missing from it is skipped).</summary>
    private static List<HouseOffer> Path(HouseOffer target, IReadOnlyDictionary<string, HouseOffer> byId)
    {
        var path = new List<HouseOffer>();
        var seen = new HashSet<string>();
        var open = new Stack<HouseOffer>();
        open.Push(target);
        while (open.Count > 0)
        {
            HouseOffer o = open.Pop();
            if (o.Owned || !seen.Add(o.Id))
                continue;
            path.Add(o);
            foreach (string need in o.Requires)
                if (need != null && byId.TryGetValue(need, out HouseOffer n))
                    open.Push(n);
        }
        return path;
    }
}
