using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Generate World's Home part (the Home upgrades spec HU3; the Home pet spec):
/// world_source.json "home" is checked (the rows, the House tree through
/// UpgradeTree.Problems, the toys, HomeContent.Problems: the radio, the
/// bills, the pet's words) and written: one house upgrade (venue Home, its
/// category as its branch, its prerequisites) and one permanent effect with
/// a household op per non-zero effect column for each row, and one toy
/// (venue Orders, the Toys band, its Mood op) for each home.toys row, into
/// the owned Assets/Data/World/Home, listed in the library after the
/// hand-authored upgrades and effects; the radio, the bills and the pet's
/// words go into the library's Home content.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>Folder of the generated house upgrades and their effects.</summary>
    private const string HomeFolder = WorldRoot + "/Home";

    /// <summary>world_source.json "home": the radio's upgrade and lines, the House tree's rows, the night's bills, the pet's words and the toys.</summary>
    [Serializable] private sealed class HomeData
    {
        public string radioUpgrade;
        public string[] radio;
        public HouseUpgradeData[] upgrades;
        public BillData[] bills;
        public PetData pet;
        public ToyData[] toys;
    }

    /// <summary>One night's bill: which (a HomeBill name), its name, price and line.</summary>
    [Serializable] private sealed class BillData
    {
        public string bill;
        public string name;
        public int price;
        public string line;
    }

    /// <summary>The pet's words (home.pet).</summary>
    [Serializable] private sealed class PetData
    {
        public int nameMaxLength;
        public PetKindData[] kinds;
        public string[] hunger;
        public string[] cold;
        public string[] boredom;
        public string[] sickness;
        public string worse;
        public string better;
        public string welfareNotice;
        public string paperAdopted;
        public string paperWelfare;
    }

    /// <summary>One kind's words (home.pet.kinds): which (a PetKind name), its word, suggested name, reactions and toy lines.</summary>
    [Serializable] private sealed class PetKindData
    {
        public string kind;
        public string word;
        public string suggestedName;
        public string[] reactions;
        public string[] toyLines;
    }

    /// <summary>One toy (home.toys): sold at the PC's Orders app in the Toys band, its Mood op owned for good.</summary>
    [Serializable] private sealed class ToyData
    {
        public string id;
        public string name;
        public int cost;
        public float mood;
        public string blurb;
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
        CheckToys(src, others, rows, errors);
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
        foreach (BillData b in src.home?.bills ?? Array.Empty<BillData>())
            if (b != null && !Enum.TryParse(b.bill, out HomeBill _))
                errors.Add($"home.bills: '{b.bill}' is none of {string.Join(", ", Enum.GetNames(typeof(HomeBill)))}.");
        foreach (PetKindData k in src.home?.pet?.kinds ?? Array.Empty<PetKindData>())
            if (k != null && !Enum.TryParse(k.kind, out PetKind _))
                errors.Add($"home.pet.kinds: '{k.kind}' is none of {string.Join(", ", Enum.GetNames(typeof(PetKind)))}.");
        errors.AddRange(BuildHome(src.home).Problems(rows.Select(u => u.id)));
    }

    /// <summary>The toy rows (none when the section is missing).</summary>
    private static ToyData[] ToyRows(WorldSource src) =>
        (src.home?.toys ?? Array.Empty<ToyData>()).Where(t => t != null).ToArray();

    /// <summary>Checks home.toys (errors added): an id blank, repeated, or another upgrade's (the library's, the translators', a house upgrade's); a cost below 0; a mood below 0.</summary>
    private static void CheckToys(WorldSource src, HashSet<string> others, HouseUpgradeData[] house, List<string> errors)
    {
        var seen = new HashSet<string>(house.Select(u => u.id ?? string.Empty));
        seen.UnionWith(others);
        foreach (ToyData t in ToyRows(src))
        {
            if (string.IsNullOrWhiteSpace(t.id))
                errors.Add("home.toys: a toy has no id.");
            else if (!seen.Add(t.id))
                errors.Add($"home.toys '{t.id}': another upgrade (or toy) has this id.");
            if (t.cost < 0)
                errors.Add($"home.toys '{t.id}': its cost is below 0 cr.");
            if (t.mood < 0f)
                errors.Add($"home.toys '{t.id}': its mood is below 0 (a toy cheers the pet up).");
        }
    }

    /// <summary>The library's Home content from the source: the radio's upgrade and lines, the bills and the pet's words, verbatim (an unknown bill or kind name reads as the first, and CheckHome refuses it).</summary>
    private static HomeContent BuildHome(HomeData h)
    {
        PetData p = h?.pet;
        return new HomeContent
        {
            radioUpgrade = h?.radioUpgrade ?? string.Empty,
            radio = (h?.radio ?? Array.Empty<string>()).ToList(),
            bills = (h?.bills ?? Array.Empty<BillData>()).Where(b => b != null).Select(b => new BillRow
            {
                bill = Enum.TryParse(b.bill, out HomeBill bill) ? bill : default,
                name = b.name ?? string.Empty,
                price = b.price,
                line = b.line ?? string.Empty
            }).ToList(),
            pet = p == null ? new PetContent() : new PetContent
            {
                nameMaxLength = p.nameMaxLength,
                kinds = (p.kinds ?? Array.Empty<PetKindData>()).Where(k => k != null).Select(k => new PetKindContent
                {
                    kind = Enum.TryParse(k.kind, out PetKind kind) ? kind : default,
                    word = k.word ?? string.Empty,
                    suggestedName = k.suggestedName ?? string.Empty,
                    reactions = (k.reactions ?? Array.Empty<string>()).ToList(),
                    toyLines = (k.toyLines ?? Array.Empty<string>()).ToList()
                }).ToList(),
                hunger = (p.hunger ?? Array.Empty<string>()).ToList(),
                cold = (p.cold ?? Array.Empty<string>()).ToList(),
                boredom = (p.boredom ?? Array.Empty<string>()).ToList(),
                sickness = (p.sickness ?? Array.Empty<string>()).ToList(),
                worse = p.worse ?? string.Empty,
                better = p.better ?? string.Empty,
                welfareNotice = p.welfareNotice ?? string.Empty,
                paperAdopted = p.paperAdopted ?? string.Empty,
                paperWelfare = p.paperWelfare ?? string.Empty
            }
        };
    }

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

        // The toys (the Home pet spec PS7): Orders upgrades in the Toys band, each a permanent Mood op once delivered.
        foreach (ToyData t in ToyRows(src))
        {
            EffectSO fx = LoadOrCreate<EffectSO>($"{HomeFolder}/Effect_{Pascal(t.id)}.asset", written);
            fx.displayName = t.name;
            fx.channel = EffectChannel.General;
            fx.defaultDurationDays = -1;
            fx.ops = new List<EffectOp>();
            AddOp(fx, EffectOpType.Mood, t.mood);
            EditorUtility.SetDirty(fx);

            UpgradeSO so = LoadOrCreate<UpgradeSO>($"{HomeFolder}/Upgrade_{Pascal(t.id)}.asset", written);
            so.id = t.id;
            so.displayName = t.name;
            so.description = t.blurb;
            so.cost = t.cost;
            so.unlockEffect = fx;
            so.venue = UpgradeVenue.Orders;
            so.branch = UpgradeBranch.Toys;
            so.requires = Array.Empty<string>();
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

    /// <summary>Writes the Home content (the radio, the bills, the pet's words) into the content library.</summary>
    private static void WireHome(ContentLibrarySO lib, HomeData home)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("home").boxedValue = BuildHome(home);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }
}
