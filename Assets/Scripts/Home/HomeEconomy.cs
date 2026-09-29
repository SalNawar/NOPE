using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Household economy for the Home phase (Phase 4; the house upgrades, the
/// Home upgrades spec): the night's break-in, daily living expenses and the
/// upkeep of what the house owns, family member condition drift and
/// recovery, and treating conditions for credits. Stateless static helpers
/// applying the Domain's HomeRules to WorldState + GameConfigSO, with the
/// house upgrades' household effect ops (TimelineEffects.SumFloat over the
/// content library) added to each knob through HomeRules.Adjusted and Cost.
/// </summary>
public static class HomeEconomy
{
    /// <summary>Breakdown of one night's household costs, for display in HomeUIController and the statement.</summary>
    public readonly struct ExpenseReport
    {
        /// <summary>Rent and utilities: the base daily expense with the house's HouseholdExpense ops.</summary>
        public readonly int baseAmount;

        /// <summary>Per-family-member upkeep total.</summary>
        public readonly int memberAmount;

        /// <summary>Medical drain from family member conditions (per point with the house's MedicalDrain ops).</summary>
        public readonly int conditionAmount;

        /// <summary>The upkeep of what the house owns (its Upkeep ops: food plans, subscriptions).</summary>
        public readonly int upkeepAmount;

        /// <summary>What tonight's break-in took from the wallet (0 without one).</summary>
        public readonly int breakInLoss;

        /// <summary>Sum of all of the above; what left the wallet when Home opened.</summary>
        public readonly int total;

        /// <summary>Number of family members at the time of billing.</summary>
        public readonly int memberCount;

        /// <summary>Creates a report; the total is the sum of the five amounts.</summary>
        public ExpenseReport(int baseAmount, int memberAmount, int conditionAmount, int upkeepAmount, int breakInLoss, int memberCount)
        {
            this.baseAmount = baseAmount;
            this.memberAmount = memberAmount;
            this.conditionAmount = conditionAmount;
            this.upkeepAmount = upkeepAmount;
            this.breakInLoss = breakInLoss;
            this.memberCount = memberCount;
            total = baseAmount + memberAmount + conditionAmount + upkeepAmount + breakInLoss;
        }
    }

    /// <summary>What happened when Home opened (DayCycle.OpenHome): the bill (the break-in included), who got worse or better overnight, and the household's mood.</summary>
    public sealed class Evening
    {
        /// <summary>Tonight's bill.</summary>
        public ExpenseReport bill;

        /// <summary>The members who got one point worse tonight, in family order.</summary>
        public readonly List<string> worse = new List<string>();

        /// <summary>The members who got one point better tonight, in family order.</summary>
        public readonly List<string> better = new List<string>();

        /// <summary>The household's mood (the house's Mood ops).</summary>
        public float mood;

        /// <summary>A sick member's nightly chance to recover at that mood (HomeRules.RecoveryChance).</summary>
        public float recoveryChance;
    }

    /// <summary>The house upgrades' summed <paramref name="op"/> in force (TimelineEffects.SumFloat); 0 without a library.</summary>
    public static float HouseSum(WorldState world, ContentLibrarySO lib, EffectOpType op) =>
        world != null && lib != null ? TimelineEffects.SumFloat(world, lib, op) : 0f;

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
    /// Computes and deducts today's living expenses from world.money: rent and
    /// utilities (the base expense with the HouseholdExpense ops), the upkeep
    /// per family member, the medical drain per condition point
    /// (HomeRules.DrainPoints; the per-point cost with the MedicalDrain ops)
    /// and the house's upkeep (its Upkeep ops), each through HomeRules.Cost,
    /// never below 0. <paramref name="breakInLoss"/>, already taken, joins the
    /// report. Safe to call with a null config (falls back to zero expenses).
    /// </summary>
    public static ExpenseReport ApplyDailyExpenses(WorldState world, ContentLibrarySO lib, GameConfigSO config, int breakInLoss)
    {
        if (world == null)
        {
            Debug.LogWarning("[HomeEconomy] ApplyDailyExpenses: no world, so nothing is billed.");
            return default;
        }

        int memberCount = world.family.members.Count;

        int baseAmount = HomeRules.Cost(config != null ? config.baseDailyExpense : 0, HouseSum(world, lib, EffectOpType.HouseholdExpense));
        int memberAmount = (config != null ? config.expensePerFamilyMember : 0) * memberCount;

        int conditionTotal = 0;
        foreach (FamilyMemberData m in world.family.members)
            if (m != null)
                conditionTotal += HomeRules.DrainPoints(m.condition);

        int perPoint = HomeRules.Cost(config != null ? config.expensePerConditionPoint : 0, HouseSum(world, lib, EffectOpType.MedicalDrain));
        int upkeep = HomeRules.Cost(0, HouseSum(world, lib, EffectOpType.Upkeep));

        var report = new ExpenseReport(baseAmount, memberAmount, perPoint * conditionTotal, upkeep, breakInLoss, memberCount);
        world.money -= report.total - breakInLoss;
        return report;
    }

    /// <summary>Credits cost to treat one point of condition off a family member: the config's care cost with the house's CareCost ops (HomeRules.Cost).</summary>
    public static int GetCareCost(WorldState world, ContentLibrarySO lib, GameConfigSO config) =>
        HomeRules.Cost(config != null ? config.conditionCareCost : 0, HouseSum(world, lib, EffectOpType.CareCost));

    /// <summary>
    /// Spends credits to reduce a family member's condition by 1.
    /// Returns true if the treatment was applied (HomeRules.CanTreat: a
    /// condition to treat and enough money for GetCareCost).
    /// </summary>
    public static bool TreatFamilyMember(WorldState world, ContentLibrarySO lib, GameConfigSO config, int memberIndex)
    {
        if (world == null || memberIndex < 0 || memberIndex >= world.family.members.Count)
            return false;

        FamilyMemberData member = world.family.members[memberIndex];
        int cost = GetCareCost(world, lib, config);

        if (member == null || !HomeRules.CanTreat(member.condition, world.money, cost))
            return false;

        world.money -= cost;
        member.condition = HomeRules.Treated(member.condition);
        return true;
    }

    /// <summary>
    /// The night for each family member (HomeRules.Night): worse by 1, capped
    /// at config.maxFamilyCondition, when their drift roll (HomeRules.Worsens)
    /// falls below conditionWorsenChance with the house's SicknessChance ops;
    /// else better by 1 when they are sick and their recovery roll
    /// (HomeRules.Recovers) falls below the household mood's recovery chance.
    /// Records the changes and the mood in <paramref name="evening"/> (when
    /// given). Nothing changes without a config.
    /// </summary>
    public static void AdvanceFamilyConditions(WorldState world, ContentLibrarySO lib, GameConfigSO config, int seed, Evening evening)
    {
        if (world == null || config == null)
            return;

        float worsen = HomeRules.Adjusted(config.conditionWorsenChance, HouseSum(world, lib, EffectOpType.SicknessChance));
        float mood = HouseSum(world, lib, EffectOpType.Mood);
        float recover = HomeRules.RecoveryChance(mood, config.recoveryPerMood, config.maxRecoveryChance);
        if (evening != null)
        {
            evening.mood = mood;
            evening.recoveryChance = recover;
        }

        for (int i = 0; i < world.family.members.Count; i++)
        {
            FamilyMemberData member = world.family.members[i];
            if (member == null)
                continue;

            int before = member.condition;
            member.condition = HomeRules.Night(before, HomeRules.Worsens(seed, i, worsen), HomeRules.Recovers(seed, i, recover), config.maxFamilyCondition);
            if (evening == null)
                continue;
            if (member.condition > before)
                evening.worse.Add(member.name);
            else if (member.condition < before)
                evening.better.Add(member.name);
        }
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
