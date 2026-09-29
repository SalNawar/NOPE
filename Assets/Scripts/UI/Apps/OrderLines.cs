using System;
using System.Collections.Generic;

/// <summary>
/// The Orders app's words (the portals spec v3 OR3, OR9): the wallet line, a
/// node's state line (its price or "not enough", "Needs: …", in transit,
/// delivered or owned, and for an owned scanner where it stands), the
/// requisition form's Requires box, a band's name and an amount in the
/// wallet's currency, each from the UI strings (UiText). OrdersWindow writes
/// them only when what they show changes.
/// </summary>
public static class OrderLines
{
    /// <summary>Each branch's name key ("app.orders.branch.desk"), by the branch's number, made once.</summary>
    private static readonly string[] BranchKeys = MakeBranchKeys();

    /// <summary>The wallet line over the tree.</summary>
    public static string Wallet(WorldState world) => UiText.Format("app.orders.wallet", Money(world.money));

    /// <summary>A branch's name (a band's head and the form's section).</summary>
    public static string Branch(UpgradeBranch branch) => UiText.Get(BranchKeys[(int)branch]);

    /// <summary>A node's state line: its price (or "not enough"), "Needs: …", "In transit · arrives day N", "Delivered day N" or "Owned"; for an owned scanner where it stands (on the node alone, where room is short; after the delivery day on the <paramref name="full"/> detail card).</summary>
    public static string State(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade, OrderState state, bool full)
    {
        switch (state)
        {
            case OrderState.Locked:
                return UiText.Format("app.orders.needs", string.Join(", ", UpgradeTree.Missing(upgrade.Node, world.HasUpgrade).ConvertAll(id => NameOf(lib, id))));
            case OrderState.TooDear:
                return UiText.Format("app.orders.tooDear", OrderBook.Price(world, lib, upgrade), UiText.Currency(UiText.WalletForm.Short));
            case OrderState.InTransit:
                return UiText.Format("app.orders.inTransit", Orders.ArrivalDay(Orders.Pending(world.orders, upgrade.id)));
            case OrderState.Owned:
                OrderEntry delivery = Orders.Delivery(world.orders, upgrade.id);
                string owned = delivery != null ? UiText.Format("app.orders.delivered", delivery.deliveredDay) : UiText.Get("app.orders.owned");
                string install = Install(OrderBook.InstallStateOf(world, lib, upgrade));
                return install == null ? owned : full ? owned + " · " + install : install;
            default:
                return Money(OrderBook.Price(world, lib, upgrade));
        }
    }

    /// <summary>The form's Requires box: every prerequisite by name ("(owned)" after one owned), "None" for a root.</summary>
    public static string Requires(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade)
    {
        var names = new List<string>();
        foreach (string id in upgrade.requires ?? Array.Empty<string>())
            names.Add(NameOf(lib, id) + (world.HasUpgrade(id) ? " (" + UiText.Get("app.orders.owned") + ")" : string.Empty));
        return names.Count > 0 ? string.Join(", ", names) : UiText.Get("app.orders.none");
    }

    /// <summary>An amount in the wallet's currency ("200 cr").</summary>
    public static string Money(int amount) => UiText.Format("app.orders.price", amount, UiText.Currency(UiText.WalletForm.Short));

    /// <summary>An owned scanner's place: Installed, In storage, Installs tomorrow, Swapped out tomorrow; null without a slot.</summary>
    private static string Install(InstallState state) =>
        state switch
        {
            InstallState.Installed => UiText.Get("app.orders.installed"),
            InstallState.Stored => UiText.Get("app.orders.stored"),
            InstallState.InstallsTomorrow => UiText.Get("app.orders.installsTomorrow"),
            InstallState.LeavesTomorrow => UiText.Get("app.orders.leavesTomorrow"),
            _ => null
        };

    /// <summary>An upgrade's name by its id (the id when the library lacks it).</summary>
    private static string NameOf(ContentLibrarySO lib, string id)
    {
        UpgradeSO upgrade = lib != null ? lib.GetUpgradeById(id) : null;
        return upgrade != null ? upgrade.displayName : id;
    }

    /// <summary>The branch name keys, by the branch's number (the enum is append-only, so the numbers stay).</summary>
    private static string[] MakeBranchKeys()
    {
        var branches = (UpgradeBranch[])Enum.GetValues(typeof(UpgradeBranch));
        int count = 0;
        foreach (UpgradeBranch b in branches)
            count = Math.Max(count, (int)b + 1);
        var keys = new string[count];
        foreach (UpgradeBranch b in branches)
            keys[(int)b] = "app.orders.branch." + ArtSlots.Key(b.ToString());
        return keys;
    }
}
