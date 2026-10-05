using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Household economy for the Home phase (Phase 4; the house upgrades, the
/// Home upgrades spec; the pet, the Home pet spec): the night's break-in, the
/// fixed bill (rent and utilities, the sick pet's extra care, the house's
/// upkeep), the night's optional bills for the pet (food, heating,
/// electricity, TV, medicine; priced in world_source.json home.bills), and the
/// pet's night (its needs settled by tonight's care, the sickness and
/// recovery rolls, the Animal Welfare Office). Stateless static helpers
/// applying the Domain's HomeRules and PetRules to WorldState + GameConfigSO,
/// with the house upgrades' and the toys' effect ops (TimelineEffects.SumFloat
/// over the content library) added to each knob through HomeRules.Adjusted
/// and Cost.
/// </summary>
public static class HomeEconomy
{
    /// <summary>Breakdown of one night's fixed household costs, for display in HomeUIController and the statement.</summary>
    public readonly struct ExpenseReport
    {
        /// <summary>Rent and utilities: the base daily expense with the house's HouseholdExpense ops.</summary>
        public readonly int baseAmount;

        /// <summary>The sick pet's extra care (per step of sickness with the house's MedicalDrain ops).</summary>
        public readonly int conditionAmount;

        /// <summary>The upkeep of what the house owns (its Upkeep ops: food plans, subscriptions).</summary>
        public readonly int upkeepAmount;

        /// <summary>What tonight's break-in took from the wallet (0 without one).</summary>
        public readonly int breakInLoss;

        /// <summary>Sum of all of the above; what left the wallet when Home opened.</summary>
        public readonly int total;

        /// <summary>Creates a report; the total is the sum of the four amounts.</summary>
        public ExpenseReport(int baseAmount, int conditionAmount, int upkeepAmount, int breakInLoss)
        {
            this.baseAmount = baseAmount;
            this.conditionAmount = conditionAmount;
            this.upkeepAmount = upkeepAmount;
            this.breakInLoss = breakInLoss;
            total = baseAmount + conditionAmount + upkeepAmount + breakInLoss;
        }
    }

    /// <summary>The house upgrades' and toys' summed <paramref name="op"/> in force (TimelineEffects.SumFloat); 0 without a library.</summary>
    public static float HouseSum(WorldState world, ContentLibrarySO lib, EffectOpType op) =>
        world != null && lib != null ? TimelineEffects.SumFloat(world, lib, op) : 0f;

    /// <summary>The worst level of the pet's needs (GameConfigSO.petNeedMax; 3 without a config).</summary>
    public static int NeedMax(GameConfigSO config) => config != null ? Mathf.Max(1, config.petNeedMax) : 3;

    /// <summary>
    /// Rolls tonight's break-in (HomeRules.BreakIn on the night's own stream,
    /// from config.breakInFromDay, at breakInChance with the house's
    /// BreakInChance ops) and takes its loss from world.money
    /// (HomeRules.BreakInLoss: breakInShare with the BreakInShare ops of a
    /// positive wallet, at most breakInMaxLoss). Returns the loss (0 without
    /// a break-in or a config).
    /// </summary>
    public static int RollBreakIn(WorldState world, ContentLibrarySO lib, GameConfigSO config, int seed)
    {
        if (world == null || config == null)
            return 0;

        float chance = HomeRules.Adjusted(config.breakInChance, HouseSum(world, lib, EffectOpType.BreakInChance));
        if (!HomeRules.BreakIn(world.day, config.breakInFromDay, seed, chance))
            return 0;

        float share = HomeRules.Adjusted(config.breakInShare, HouseSum(world, lib, EffectOpType.BreakInShare));
        int loss = HomeRules.BreakInLoss(world.money, share, config.breakInMaxLoss);
        world.money -= loss;
        return loss;
    }

    /// <summary>
    /// Computes and deducts tonight's fixed household costs from world.money
    /// (<see cref="DailyExpenses"/>); <paramref name="breakInLoss"/>, already
    /// taken, joins the report. Safe to call with a null config (falls back
    /// to zero expenses).
    /// </summary>
    public static ExpenseReport ApplyDailyExpenses(WorldState world, ContentLibrarySO lib, GameConfigSO config, int breakInLoss)
    {
        if (world == null)
        {
            Debug.LogWarning("[HomeEconomy] ApplyDailyExpenses: no world, so nothing is billed.");
            return default;
        }

        ExpenseReport report = DailyExpenses(world, lib, config, breakInLoss);
        world.money -= report.total - breakInLoss;
        return report;
    }

    /// <summary>
    /// Tonight's fixed household costs, without paying them: rent and
    /// utilities (the base expense with the HouseholdExpense ops), the sick
    /// pet's extra care per step of its sickness (the per-step cost with the
    /// MedicalDrain ops) and the house's upkeep (its Upkeep ops), each through
    /// HomeRules.Cost, never below 0, with <paramref name="breakInLoss"/> in
    /// the report. Home pays it when it opens (ApplyDailyExpenses); the
    /// night's optional bills come after (PayBills). Zero without a world or a
    /// config.
    /// </summary>
    public static ExpenseReport DailyExpenses(WorldState world, ContentLibrarySO lib, GameConfigSO config, int breakInLoss)
    {
        if (world == null)
            return default;

        int baseAmount = HomeRules.Cost(config != null ? config.baseDailyExpense : 0, HouseSum(world, lib, EffectOpType.HouseholdExpense));
        int perStep = HomeRules.Cost(config != null ? config.expensePerConditionPoint : 0, HouseSum(world, lib, EffectOpType.MedicalDrain));
        int sickness = world.pet != null ? HomeRules.DrainPoints(world.pet.sickness) : 0;
        int upkeep = HomeRules.Cost(0, HouseSum(world, lib, EffectOpType.Upkeep));

        return new ExpenseReport(baseAmount, perStep * sickness, upkeep, breakInLoss);
    }

    /// <summary>A night's bill's price in cr: its row's (home.bills; 0 without one), the Medicine's with the house's CareCost ops (HomeRules.Cost).</summary>
    public static int BillPrice(WorldState world, ContentLibrarySO lib, HomeBill bill)
    {
        BillRow row = lib != null ? lib.Home.Bill(bill) : null;
        int price = row != null ? row.price : 0;
        return bill == HomeBill.Medicine ? HomeRules.Cost(price, HouseSum(world, lib, EffectOpType.CareCost)) : Mathf.Max(0, price);
    }

    /// <summary>What <paramref name="care"/>'s bills cost tonight (PetRules.Total at <see cref="BillPrice"/>).</summary>
    public static int BillsTotal(WorldState world, ContentLibrarySO lib, PetCare care) =>
        PetRules.Total(care, bill => BillPrice(world, lib, bill));

    /// <summary>The pet's essentials tonight (food, heating and electricity): what the shift report adds to the fixed bill (lesson 5).</summary>
    public static int EssentialsPrice(WorldState world, ContentLibrarySO lib) =>
        BillsTotal(world, lib, new PetCare(true, true, true, false, false));

    /// <summary>
    /// Pays tonight's bills for <paramref name="care"/> (the bills step's Pay):
    /// their total leaves the wallet when it covers them (nothing is bought on
    /// credit; paying nothing is always allowed). Returns the total paid, or
    /// -1 when refused.
    /// </summary>
    public static int PayBills(WorldState world, ContentLibrarySO lib, PetCare care)
    {
        if (world == null)
            return -1;
        int total = BillsTotal(world, lib, care);
        if (total > 0 && world.money < total)
            return -1;
        world.money -= total;
        return total;
    }

    /// <summary>The toys the clerk owns (Orders upgrades in the Toys band, delivered), in the library's order.</summary>
    public static List<UpgradeSO> OwnedToys(WorldState world, ContentLibrarySO lib)
    {
        var toys = new List<UpgradeSO>();
        if (world == null || lib == null)
            return toys;
        foreach (UpgradeSO u in lib.Upgrades)
            if (u != null && u.branch == UpgradeBranch.Toys && world.HasUpgrade(u.id))
                toys.Add(u);
        return toys;
    }

    /// <summary>
    /// The pet's night at Sleep (the Home pet spec PS5, PS6), on
    /// <paramref name="seed"/> (the day's) so a replay is the same: its needs
    /// settled by tonight's <paramref name="care"/> (PetRules.Settle); its
    /// sickness rolled on the household's stream (HomeRules.Worsens, place 0)
    /// against PetRules.SicknessChance (conditionWorsenChance, sicknessPerNeed
    /// a step of hunger and cold, the house's SicknessChance ops, the mood's
    /// share) and, when sick, its recovery on its own stream
    /// (HomeRules.Recovers) against PetRules.RecoveryChance; the change kept
    /// for the next Home; then the Welfare Office's count (PetRules.Neglected,
    /// WelfareNights) and, at config.welfareNights, the pet taken (the
    /// failure ending). Nothing without an adopted pet or a config.
    /// </summary>
    public static void PetNight(WorldState world, ContentLibrarySO lib, GameConfigSO config, int seed, PetCare care)
    {
        PetState pet = world != null ? world.pet : null;
        if (pet == null || !pet.Adopted || config == null)
            return;

        int max = NeedMax(config);
        PetNeeds settled = PetRules.Settle(pet.Needs, care, max);
        float mood = HouseSum(world, lib, EffectOpType.Mood);
        float worsen = PetRules.SicknessChance(config.conditionWorsenChance, settled, config.sicknessPerNeed, HouseSum(world, lib, EffectOpType.SicknessChance),
                                               mood, config.sicknessPerMood, config.maxMoodSicknessCut);
        float recover = PetRules.RecoveryChance(settled, mood, config.recoveryPerMood, config.maxRecoveryChance);
        int sickness = PetRules.Sickness(settled.Sickness, care.Medicine, HomeRules.Worsens(seed, 0, worsen), HomeRules.Recovers(seed, 0, recover), max);

        pet.lastChange = sickness > pet.sickness ? 1 : sickness < pet.sickness ? -1 : 0;
        var after = new PetNeeds(settled.Hunger, settled.Cold, settled.Boredom, sickness);
        pet.SetNeeds(after);
        pet.welfareNights = PetRules.WelfareNights(pet.welfareNights, PetRules.Neglected(after, max));
        pet.taken = pet.taken || PetRules.Taken(pet.welfareNights, config.welfareNights);
        Debug.Log($"[HomeEconomy] PetNight (day {world.day}): '{pet.name}' hunger={after.Hunger} cold={after.Cold} boredom={after.Boredom} sickness={after.Sickness} (worsen {worsen:0.###}, recover {recover:0.###}), welfareNights={pet.welfareNights}, taken={pet.taken}.");
    }

    /// <summary>
    /// Buys a house upgrade (the Home upgrades spec HU5): only a Home upgrade
    /// whose state is buyable (OrderBook.StateOf: not owned, every
    /// prerequisite owned, the wallet covering its price). The price leaves
    /// the wallet, the upgrade is owned at once and its unlock effect starts
    /// today (TimelineService.ActivateEffect), so tonight's fixed bill,
    /// settled when Home opened, is untouched, and tonight's pet night at
    /// Sleep and the next evening read it. Returns the price paid, or -1 when
    /// refused. The House and the balance simulation's buyer both buy through
    /// here.
    /// </summary>
    public static int BuyHouseUpgrade(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade)
    {
        if (world == null || upgrade == null || upgrade.venue != UpgradeVenue.Home || OrderBook.StateOf(world, lib, upgrade) != OrderState.Orderable)
            return -1;

        int price = OrderBook.Price(world, lib, upgrade);
        world.money -= price;
        world.UnlockUpgrade(upgrade.id);
        if (upgrade.unlockEffect != null)
            TimelineService.ActivateEffect(world, upgrade.unlockEffect, $"Upgrade: {upgrade.displayName}", world.day, upgrade.unlockEffect.defaultDurationDays, applyInstantOps: true);
        return price;
    }

    /// <summary>
    /// What <paramref name="upgrade"/> costs tonight: its listed cost with the
    /// active shop discount off (TimelineEffects.GetShopDiscountPercent, none
    /// without a library) through ShopPrices.Discounted; the one place the
    /// price the shop shows and the price a purchase charges come from (audit
    /// R2-006, R4-013). <paramref name="discountPercent"/> is that discount, for
    /// the row's label.
    /// </summary>
    public static int UpgradeCost(WorldState world, ContentLibrarySO lib, UpgradeSO upgrade, out float discountPercent)
    {
        discountPercent = world != null && lib != null && upgrade != null ? TimelineEffects.GetShopDiscountPercent(world, lib, upgrade.id) : 0f;
        return ShopPrices.Discounted(upgrade != null ? upgrade.cost : 0, discountPercent);
    }
}
