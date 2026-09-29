// ReSharper disable InconsistentNaming
using UnityEngine;

/// <summary>
/// An upgrade: what it unlocks (clue generation, interview questions through
/// UpgradeOwned, the scanners, the translators), where it is sold (the PC's
/// Orders app, a node of the upgrade tree, or Home's evening list; Saleh
/// 2026-09-29), its band and prerequisites in the tree, and its price. Every
/// field is an Inspector knob on a hand-authored upgrade (Assets/Data/Upgrades);
/// Generate World writes the Speech translators' from world_source.json
/// "translation.packs".
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_", menuName = "TimeDesk/Upgrade", order = 13)]
public sealed class UpgradeSO : ScriptableObject
{
    /// <summary>Stable upgrade ID used in WorldState (e.g., "scanner").</summary>
    public string id;

    /// <summary>Display name shown in UI.</summary>
    public string displayName;

    /// <summary>The blurb: the Orders app's detail card, or under the name in Home's list.</summary>
    [TextArea]
    public string description;

    /// <summary>Base credits cost (before TimelineEffects discounts: HomeEconomy.UpgradeCost is the price shown and charged).</summary>
    [Min(0)]
    public int cost;

    /// <summary>Optional effect applied once when this upgrade arrives (an order's delivery at the start of the next day) or is bought at Home, with its instant ops.</summary>
    public EffectSO unlockEffect;

    /// <summary>Where it is sold: the PC's Orders app (a node of the upgrade tree, delivered the day after it is ordered) or Home's House panel (bought and owned at once).</summary>
    public UpgradeVenue venue = UpgradeVenue.Orders;

    /// <summary>Its band: one of the Orders tree's (Desk equipment, Interview, Portals: a portal's repair, Contacts) or Home's categories (Food, Housing, Security, Health, Comfort), per its venue (UpgradeTree.BranchesOf).</summary>
    public UpgradeBranch branch = UpgradeBranch.Desk;

    /// <summary>The upgrade ids that must be owned (delivered) before it can be ordered or bought; empty for a root of the tree. Each is sold at the same venue (UpgradeTree.Problems).</summary>
    public string[] requires = System.Array.Empty<string>();

    /// <summary>Its install slot ("scanner" for both upgraded scanners; empty for none): of the owned upgrades sharing a slot only the installed one is in force (Installs; Saleh 2026-09-29: one upgraded scanner at a time).</summary>
    public string installSlot = string.Empty;

    /// <summary>This upgrade as the tree reads it (its listed cost, so a discount never moves a node).</summary>
    public TreeNode Node => new TreeNode(id, venue, branch, cost, requires);
}
