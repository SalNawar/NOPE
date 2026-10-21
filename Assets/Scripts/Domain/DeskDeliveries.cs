using System.Collections.Generic;

/// <summary>
/// What the supply crate unpacks on a morning (BUG-28c/28d, DESK-PLAN's
/// part: "Deliver skips owned items, so a cheat grant or a duplicate order
/// can't deliver twice"): the day's deliveries (Orders.DeliveredOn) without
/// any upgrade whose object already stood on the desk when the day began
/// before this one (granted by a cheat while its order was in transit, or
/// delivered twice), each once, in delivery order (ToUnpack); and which
/// RING SERVICE stickers wait for the crate on a panel that already stood
/// on the desk (StickersToWait: BUG-60a, a second ring repair skips no
/// crate). Pure; DeskUpgrades applies it.
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

    /// <summary>
    /// The RING SERVICE modules (0-based, 01 to 05) whose SERVICED sticker
    /// came this morning on a panel that already stood on the desk: each waits
    /// for the crate on its own (BUG-60a: "some parts skip the crate"). None
    /// when the panel itself came this morning (UpgradeObjects.PanelNew: the
    /// whole panel waits) or when no sticker is new.
    /// </summary>
    public static List<int> StickersToWait(IReadOnlyList<UpgradeObjects.Module> modules)
    {
        var wait = new List<int>();
        if (modules == null || UpgradeObjects.PanelNew(modules))
            return wait;
        for (int i = 0; i < modules.Count; i++)
            if (modules[i].NewToday)
                wait.Add(i);
        return wait;
    }

    private static bool Contains(IReadOnlyCollection<string> ids, string id)
    {
        foreach (string other in ids)
            if (other == id)
                return true;
        return false;
    }
}
