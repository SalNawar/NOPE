using System.Collections.Generic;

/// <summary>
/// What the supply crate unpacks on a morning (BUG-28c/28d, DESK-PLAN's
/// part: "Deliver skips owned items, so a cheat grant or a duplicate order
/// can't deliver twice"): the day's deliveries (Orders.DeliveredOn) without
/// any upgrade whose object already stood on the desk when the day began
/// before this one (granted by a cheat while its order was in transit, or
/// delivered twice), each once, in delivery order. Pure; DeskUpgrades
/// applies it.
/// </summary>
public static class DeskDeliveries
{
    /// <summary>The ids of <paramref name="delivered"/> the crate brings: each once, none of <paramref name="alreadyOnDesk"/>.</summary>
    public static List<string> ToUnpack(IReadOnlyList<string> delivered, IReadOnlyCollection<string> alreadyOnDesk)
    {
        var unpack = new List<string>();
        foreach (string id in delivered ?? System.Array.Empty<string>())
        {
            if (string.IsNullOrEmpty(id) || unpack.Contains(id))
                continue;
            if (alreadyOnDesk != null && Contains(alreadyOnDesk, id))
                continue;
            unpack.Add(id);
        }
        return unpack;
    }

    private static bool Contains(IReadOnlyCollection<string> ids, string id)
    {
        foreach (string other in ids)
            if (other == id)
                return true;
        return false;
    }
}
