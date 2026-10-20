using UnityEngine;

/// <summary>
/// The night with the default care, as the Home phase plays it (P0-A carve:
/// moved out of HomeManager; owner HOME): tonight's starting care (the default
/// care for the pet's needs, or what the wallet covers when the default is
/// more than the wallet), and tonight's bills paid on a care (one the wallet
/// cannot cover falls back to what it covers). HomeManager plays the night
/// step by step through these; RUN-SAFE's cheat jumps play the whole night
/// with the default care through <see cref="DefaultNight"/> without touching
/// HomeManager (BUG-28a). Stateless static helpers over HomeEconomy and the
/// Domain's PetRules and PetPolicy.
/// </summary>
public static class NightRules
{
    /// <summary>Tonight's starting care: the default care for the pet's needs (PetRules.DefaultCare), or what the wallet covers (AffordableCare) when the default is more than the wallet.</summary>
    public static PetCare StartingCare(WorldState world, ContentLibrarySO lib)
    {
        PetCare care = PetRules.DefaultCare(Pet(world).Needs);
        if (HomeEconomy.BillsTotal(world, lib, care) > Mathf.Max(0, world.money))
            care = AffordableCare(world, lib);
        return care;
    }

    /// <summary>What the wallet covers, in the careful carer's order (PetPolicy: food, the heating, medicine when unwell, the electricity; never the TV): the starting choice when the default is more than the wallet, so Pay is never offered beyond it.</summary>
    public static PetCare AffordableCare(WorldState world, ContentLibrarySO lib) =>
        PetPolicy.Care(Pet(world).Needs, world.money, bill => HomeEconomy.BillPrice(world, lib, bill), int.MaxValue, false);

    /// <summary>Tonight's bills on <paramref name="care"/> leave the wallet (HomeEconomy.PayBills); a care the wallet cannot cover becomes what it covers (AffordableCare) and is paid. Returns what was paid.</summary>
    public static int PayBills(WorldState world, ContentLibrarySO lib, ref PetCare care)
    {
        int paid = HomeEconomy.PayBills(world, lib, care);
        if (paid < 0)
        {
            // Unwired panels pay what the wallet covers; a wired panel never offers Pay it cannot cover.
            care = AffordableCare(world, lib);
            paid = Mathf.Max(0, HomeEconomy.PayBills(world, lib, care));
        }
        return paid;
    }

    /// <summary>The night with the default care in one call (RUN-SAFE's cheat jumps, BUG-28a): the starting care, its bills paid, the pet's night on it (HomeEconomy.PetNight). Returns what the bills cost.</summary>
    public static int DefaultNight(WorldState world, ContentLibrarySO lib, GameConfigSO config, int seed)
    {
        PetCare care = StartingCare(world, lib);
        int paid = PayBills(world, lib, ref care);
        HomeEconomy.PetNight(world, lib, config, seed, care);
        return paid;
    }

    /// <summary>The run's pet (never null), as HomeManager's.</summary>
    private static PetState Pet(WorldState world) => world.pet ?? (world.pet = new PetState());
}
