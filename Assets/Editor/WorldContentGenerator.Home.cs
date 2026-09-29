using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Generate World's Home part (the Home upgrades spec HU3): world_source.json
/// "home" is checked (the rows, the House tree through UpgradeTree.Problems,
/// HomeContent.Problems) and written: one house upgrade (venue Home, its
/// category as its branch, its prerequisites) and one permanent effect with
/// a household op per non-zero effect column for each row, into the owned
/// Assets/Data/World/Home, listed in the library after the hand-authored
/// upgrades and effects; the radio block goes into the library's Home content.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>Folder of the generated house upgrades and their effects.</summary>
    private const string HomeFolder = WorldRoot + "/Home";

    /// <summary>world_source.json "home": the radio's upgrade and lines, and the House tree's rows.</summary>
    [Serializable] private sealed class HomeData
    {
        public string radioUpgrade;
        public string[] radio;
        public HouseUpgradeData[] upgrades;
    }

    /// <summary>One house upgrade's row: its price, upkeep, prerequisites, household effects and blurb.</summary>
    [Serializable] private sealed class HouseUpgradeData
    {
        public string id;
        public string name;
        public string category;
        public int cost;
        public int upkeep;
        public string[] requires;
        public float householdExpense;
        public float sicknessChance;
        public float careCost;
        public float medicalDrain;
        public float mood;
        public float breakInChance;
        public float breakInShare;
        public string blurb;
    }

    /// <summary>The house rows (none when the section is missing).</summary>
    private static HouseUpgradeData[] HouseRows(WorldSource src) =>
        (src.home?.upgrades ?? Array.Empty<HouseUpgradeData>()).Where(u => u != null).ToArray();

    /// <summary>A row's category as its branch, when it names one of Home's (UpgradeTree.BranchesOf).</summary>
    private static bool HouseBranch(HouseUpgradeData u, out UpgradeBranch branch) =>
        Enum.TryParse(u.category, out branch) && Array.IndexOf(UpgradeTree.BranchesOf(UpgradeVenue.Home), branch) >= 0;

    /// <summary>
    /// Checks "home" (errors added, nothing written): each row's category is
    /// one of Home's, its cost and upkeep at least 0, its id none of the
    /// hand-authored upgrades' or the translators'; the House tree
    /// (UpgradeTree.Problems: blank or repeated ids, unknown or cyclic
    /// prerequisites) and the radio (HomeContent.Problems).
    /// </summary>
    private static void CheckHome(WorldSource src, Authored authored, List<string> errors)
    {
        HouseUpgradeData[] rows = HouseRows(src);
        var others = new HashSet<string>(CatalogueNodes(authored, src.translation ?? new TranslationData()).Select(n => n.Id));
        var nodes = new List<TreeNode>();
        foreach (HouseUpgradeData u in rows)
        {
            if (!HouseBranch(u, out UpgradeBranch branch))
                errors.Add($"home.upgrades '{u.id}': category '{u.category}' is none of {string.Join(", ", UpgradeTree.BranchesOf(UpgradeVenue.Home))}.");
            if (u.cost < 0 || u.upkeep < 0)
                errors.Add($"home.upgrades '{u.id}': cost and upkeep are 0 cr or more.");
            if (others.Contains(u.id ?? string.Empty))
                errors.Add($"home.upgrades '{u.id}': an upgrade the library or the translators already have has this id.");
            nodes.Add(new TreeNode(u.id, UpgradeVenue.Home, branch, u.cost, u.requires));
        }
        foreach (string problem in UpgradeTree.Problems(nodes))
            errors.Add("The House tree (home.upgrades): " + problem);
        errors.AddRange(BuildHome(src.home).Problems(rows.Select(u => u.id)));
    }

    /// <summary>The library's Home content from the source (the radio's upgrade and lines verbatim).</summary>
    private static HomeContent BuildHome(HomeData h) =>
        new HomeContent { radioUpgrade = h?.radioUpgrade ?? string.Empty, radio = (h?.radio ?? Array.Empty<string>()).ToList() };

    /// <summary>
    /// Writes Home/Upgrade_{Id}.asset and Home/Effect_{Id}.asset per row: the
    /// upgrade (id, name, blurb, cost, venue Home, its category's branch, its
    /// prerequisites, the effect as its unlock effect) and the effect (the
    /// name, permanent, one op per non-zero column in EffectOpType's order:
    /// HouseholdExpense, SicknessChance, CareCost, Upkeep, MedicalDrain, Mood,
    /// BreakInChance, BreakInShare). Returns the upgrades and effects in row order.
    /// </summary>
    private static (UpgradeSO[] upgrades, EffectSO[] effects) MakeHouse(WorldSource src, HashSet<string> written)
    {
        var upgrades = new List<UpgradeSO>();
        var effects = new List<EffectSO>();
        foreach (HouseUpgradeData u in HouseRows(src))
        {
            HouseBranch(u, out UpgradeBranch branch);
            EffectSO fx = LoadOrCreate<EffectSO>($"{HomeFolder}/Effect_{Pascal(u.id)}.asset", written);
            fx.displayName = u.name;
            fx.channel = EffectChannel.General;
            fx.defaultDurationDays = -1;
            fx.ops = new List<EffectOp>();
            AddOp(fx, EffectOpType.HouseholdExpense, u.householdExpense);
            AddOp(fx, EffectOpType.SicknessChance, u.sicknessChance);
            AddOp(fx, EffectOpType.CareCost, u.careCost);
            AddOp(fx, EffectOpType.Upkeep, u.upkeep);
            AddOp(fx, EffectOpType.MedicalDrain, u.medicalDrain);
            AddOp(fx, EffectOpType.Mood, u.mood);
            AddOp(fx, EffectOpType.BreakInChance, u.breakInChance);
            AddOp(fx, EffectOpType.BreakInShare, u.breakInShare);
            EditorUtility.SetDirty(fx);

            UpgradeSO so = LoadOrCreate<UpgradeSO>($"{HomeFolder}/Upgrade_{Pascal(u.id)}.asset", written);
            so.id = u.id;
            so.displayName = u.name;
            so.description = u.blurb;
            so.cost = u.cost;
            so.unlockEffect = fx;
            so.venue = UpgradeVenue.Home;
            so.branch = branch;
            so.requires = u.requires ?? Array.Empty<string>();
            so.installSlot = string.Empty;
            EditorUtility.SetDirty(so);

            upgrades.Add(so);
            effects.Add(fx);
        }
        return (upgrades.ToArray(), effects.ToArray());
    }

    /// <summary>Adds an op of <paramref name="type"/> carrying <paramref name="value"/> to <paramref name="fx"/> unless it is 0.</summary>
    private static void AddOp(EffectSO fx, EffectOpType type, float value)
    {
        if (value != 0f)
            fx.ops.Add(new EffectOp { type = type, floatParam = value });
    }

    /// <summary>Writes the Home content (the radio) into the content library.</summary>
    private static void WireHome(ContentLibrarySO lib, HomeData home)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("home").boxedValue = BuildHome(home);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }

    /// <summary>Writes world_source.json "world" (the factors the end of the demo answers, the endings spec E0; checked by WorldContent.Problems) into the library verbatim.</summary>
    private static void WireWorld(ContentLibrarySO lib, WorldContent world)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("world").boxedValue = world ?? new WorldContent();
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }
}
