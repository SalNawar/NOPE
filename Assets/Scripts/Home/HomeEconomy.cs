using UnityEngine;

/// <summary>
/// Household economy for the Home phase (Phase 4): daily living expenses,
/// family member condition drift, and treating conditions for credits.
/// Stateless static helpers applying the Domain's HomeRules to WorldState +
/// GameConfigSO.
/// </summary>
public static class HomeEconomy
{
    /// <summary>Breakdown of one day's living expenses, for display in HomeUIController.</summary>
    public readonly struct ExpenseReport
    {
        /// <summary>Base daily living expense (rent/utilities).</summary>
        public readonly int baseAmount;

        /// <summary>Per-family-member upkeep total.</summary>
        public readonly int memberAmount;

        /// <summary>Medical drain from family member conditions.</summary>
        public readonly int conditionAmount;

        /// <summary>Sum of all of the above; what was actually deducted.</summary>
        public readonly int total;

        /// <summary>Number of family members at the time of billing.</summary>
        public readonly int memberCount;

        /// <summary>Creates a report; the total is the sum of the three amounts.</summary>
        public ExpenseReport(int baseAmount, int memberAmount, int conditionAmount, int memberCount)
        {
            this.baseAmount = baseAmount;
            this.memberAmount = memberAmount;
            this.conditionAmount = conditionAmount;
            this.memberCount = memberCount;
            total = baseAmount + memberAmount + conditionAmount;
        }
    }

    /// <summary>
    /// Computes and deducts today's living expenses from world.money: the
    /// base expense, the upkeep per family member and the medical drain per
    /// condition point (HomeRules.DrainPoints). Safe to call with a null
    /// config (falls back to zero expenses).
    /// </summary>
    public static ExpenseReport ApplyDailyExpenses(WorldState world, GameConfigSO config)
    {
        if (world == null)
        {
            Debug.LogWarning("[HomeEconomy] ApplyDailyExpenses: no world, so nothing is billed.");
            return default;
        }

        int memberCount = world.family.members.Count;

        int baseAmount = config != null ? config.baseDailyExpense : 0;
        int memberAmount = (config != null ? config.expensePerFamilyMember : 0) * memberCount;

        int conditionTotal = 0;
        foreach (FamilyMemberData m in world.family.members)
            if (m != null)
                conditionTotal += HomeRules.DrainPoints(m.condition);

        int conditionAmount = (config != null ? config.expensePerConditionPoint : 0) * conditionTotal;

        var report = new ExpenseReport(baseAmount, memberAmount, conditionAmount, memberCount);
        world.money -= report.total;
        return report;
    }

    /// <summary>Credits cost to treat one point of condition off a family member.</summary>
    public static int GetCareCost(GameConfigSO config) =>
        config != null ? config.conditionCareCost : 0;

    /// <summary>
    /// Spends credits to reduce a family member's condition by 1.
    /// Returns true if the treatment was applied (HomeRules.CanTreat: a
    /// condition to treat and enough money).
    /// </summary>
    public static bool TreatFamilyMember(WorldState world, GameConfigSO config, int memberIndex)
    {
        if (world == null || memberIndex < 0 || memberIndex >= world.family.members.Count)
            return false;

        FamilyMemberData member = world.family.members[memberIndex];
        int cost = GetCareCost(config);

        if (member == null || !HomeRules.CanTreat(member.condition, world.money, cost))
            return false;

        world.money -= cost;
        member.condition = HomeRules.Treated(member.condition);
        return true;
    }

    /// <summary>
    /// Deterministically rolls condition drift for each family member based on the
    /// given seed (e.g., RunManager.GetDaySeed()): a member worsens by 1, capped
    /// at config.maxFamilyCondition, when their roll (HomeRules.Worsens) falls
    /// below config.conditionWorsenChance. Nothing drifts without a config.
    /// </summary>
    public static void AdvanceFamilyConditions(WorldState world, GameConfigSO config, int seed)
    {
        if (world == null || config == null || config.conditionWorsenChance <= 0f)
            return;

        for (int i = 0; i < world.family.members.Count; i++)
        {
            FamilyMemberData member = world.family.members[i];

            if (member != null && HomeRules.Worsens(seed, i, config.conditionWorsenChance))
                member.condition = HomeRules.Worsened(member.condition, config.maxFamilyCondition);
        }
    }
}
