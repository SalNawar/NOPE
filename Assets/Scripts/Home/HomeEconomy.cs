using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Household economy for the Home phase (Phase 4): daily living expenses,
/// family member condition drift, and treating conditions for credits.
/// Stateless static helpers operating on WorldState + GameConfigSO.
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
    /// Computes and deducts today's living expenses from world.money.
    /// Safe to call with a null config (falls back to zero expenses).
    /// </summary>
    public static ExpenseReport ApplyDailyExpenses(WorldState world, GameConfigSO config)
    {
        Debug.Log($"[HomeEconomy] >>> Entering ApplyDailyExpenses (day {world?.day}).");

        if (world == null)
        {
            Debug.LogWarning("[HomeEconomy] <<< Exiting ApplyDailyExpenses early — null world.");
            return default;
        }

        int memberCount = world.family.members.Count;

        int baseAmount = config != null ? config.baseDailyExpense : 0;
        int memberAmount = (config != null ? config.expensePerFamilyMember : 0) * memberCount;

        int conditionTotal = 0;
        foreach (FamilyMemberData m in world.family.members)
            if (m != null)
                conditionTotal += Mathf.Max(0, m.condition);

        int conditionAmount = (config != null ? config.expensePerConditionPoint : 0) * conditionTotal;

        var report = new ExpenseReport(baseAmount, memberAmount, conditionAmount, memberCount);
        world.money -= report.total;

        Debug.Log($"[HomeEconomy] <<< Exiting ApplyDailyExpenses (base={report.baseAmount}, members={report.memberAmount} x{report.memberCount}, conditions={report.conditionAmount}, total={report.total}, money={world.money}).");

        return report;
    }

    /// <summary>Credits cost to treat one point of condition off a family member.</summary>
    public static int GetCareCost(GameConfigSO config) =>
        config != null ? config.conditionCareCost : 0;

    /// <summary>
    /// Spends credits to reduce a family member's condition by 1.
    /// Returns true if the treatment was applied (enough money, condition &gt; 0).
    /// </summary>
    public static bool TreatFamilyMember(WorldState world, GameConfigSO config, int memberIndex)
    {
        Debug.Log($"[HomeEconomy] >>> Entering TreatFamilyMember (memberIndex={memberIndex}).");

        if (world == null || memberIndex < 0 || memberIndex >= world.family.members.Count)
        {
            Debug.Log("[HomeEconomy] <<< Exiting TreatFamilyMember — invalid world or member index.");
            return false;
        }

        FamilyMemberData member = world.family.members[memberIndex];

        if (member == null || member.condition <= 0)
        {
            Debug.Log($"[HomeEconomy] <<< Exiting TreatFamilyMember — member {memberIndex} has no condition to treat.");
            return false;
        }

        int cost = GetCareCost(config);

        if (world.money < cost)
        {
            Debug.Log($"[HomeEconomy] <<< Exiting TreatFamilyMember — not enough money ({world.money} < {cost}).");
            return false;
        }

        world.money -= cost;
        int before = member.condition;
        member.condition = Mathf.Max(0, member.condition - 1);

        Debug.Log($"[HomeEconomy] <<< Exiting TreatFamilyMember (member {memberIndex}: condition {before}->{member.condition}, cost={cost}, money={world.money}).");

        return true;
    }

    /// <summary>
    /// Deterministically rolls condition drift for each family member based on the
    /// given seed (e.g., RunManager.GetDaySeed()). Untreated members have a chance
    /// to worsen by 1, capped at config.maxFamilyCondition.
    /// </summary>
    public static void AdvanceFamilyConditions(WorldState world, GameConfigSO config, int seed)
    {
        Debug.Log($"[HomeEconomy] >>> Entering AdvanceFamilyConditions (seed={seed}).");

        if (world == null || world.family.members.Count == 0)
        {
            Debug.Log("[HomeEconomy] <<< Exiting AdvanceFamilyConditions — no family members.");
            return;
        }

        float chance = config != null ? config.conditionWorsenChance : 0f;
        int cap = config != null ? config.maxFamilyCondition : 10;

        if (chance <= 0f)
        {
            Debug.Log("[HomeEconomy] <<< Exiting AdvanceFamilyConditions — worsen chance is 0.");
            return;
        }

        int worsened = 0;

        for (int i = 0; i < world.family.members.Count; i++)
        {
            FamilyMemberData member = world.family.members[i];

            if (member == null)
                continue;

            unchecked
            {
                int memberSeed = seed * 397 ^ (i + 1) * 104729;
                var rng = new System.Random(memberSeed);

                if (rng.NextDouble() < chance)
                {
                    int before = member.condition;
                    member.condition = Mathf.Min(cap, member.condition + 1);

                    if (member.condition != before)
                        worsened++;
                }
            }
        }

        Debug.Log($"[HomeEconomy] <<< Exiting AdvanceFamilyConditions ({worsened}/{world.family.members.Count} member(s) worsened, chance={chance:0.##}, cap={cap}).");
    }
}
