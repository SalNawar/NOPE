using System;
using System.Collections.Generic;

/// <summary>
/// Where an upgrade is sold (Saleh 2026-09-29; the portals spec v3 OR1):
/// the office's upgrades in the PC's Orders app, the house's at Home.
/// Serialized in UpgradeSO.venue: append only (SerializedEnumsTests).
/// </summary>
public enum UpgradeVenue
{
    /// <summary>The PC's Orders app: paid when ordered, delivered at the start of the next day, a node of the upgrade tree.</summary>
    Orders,

    /// <summary>Home's evening list (household improvements): bought and owned at once, no tree.</summary>
    Home
}

/// <summary>
/// The Orders tree's band an upgrade sits in (the portals spec v3 OR2):
/// the desk's equipment, the interview, the hall's portals (the repair
/// nodes), the clerk's contacts. The bands are drawn in this order.
/// Serialized in UpgradeSO.branch: append only (SerializedEnumsTests).
/// </summary>
public enum UpgradeBranch
{
    /// <summary>Desk equipment: the scanners.</summary>
    Desk,

    /// <summary>The interview: its protocols and the Speech translators.</summary>
    Interview,

    /// <summary>The hall's portals: a node here is a portal's repair (its delivery puts the portal in service; the portals track reads it).</summary>
    Portals,

    /// <summary>The clerk's contacts.</summary>
    Contacts
}

/// <summary>One upgrade as the tree reads it: its id, venue, branch, listed cost and the ids it requires.</summary>
public readonly struct TreeNode
{
    /// <summary>The upgrade's id (UpgradeSO.id).</summary>
    public readonly string Id;

    /// <summary>Where it is sold; only the Orders venue is drawn.</summary>
    public readonly UpgradeVenue Venue;

    /// <summary>Its band.</summary>
    public readonly UpgradeBranch Branch;

    /// <summary>Its listed cost (UpgradeSO.cost): the tier's stacking order, so a discount never moves a node.</summary>
    public readonly int Cost;

    /// <summary>The upgrade ids it needs owned first (never null).</summary>
    public readonly IReadOnlyList<string> Requires;

    /// <summary>A node; <paramref name="requires"/> may be null (nothing required).</summary>
    public TreeNode(string id, UpgradeVenue venue, UpgradeBranch branch, int cost, IReadOnlyList<string> requires)
    {
        Id = id ?? string.Empty;
        Venue = venue;
        Branch = branch;
        Cost = cost;
        Requires = requires ?? Array.Empty<string>();
    }
}

/// <summary>Where a node sits: its band (an index into <see cref="TreeLayout.Bands"/>), its tier (the column, left to right) and its slot (the row in its band, top to bottom).</summary>
public readonly struct TreeCell
{
    /// <summary>The upgrade's id.</summary>
    public readonly string Id;

    /// <summary>Its branch.</summary>
    public readonly UpgradeBranch Branch;

    /// <summary>Its band's index among the bands drawn.</summary>
    public readonly int Band;

    /// <summary>Its tier: the length of its longest prerequisite chain inside its band (0 at the left).</summary>
    public readonly int Tier;

    /// <summary>Its row in its band: under the prerequisite that sets its tier, siblings by cost, then id.</summary>
    public readonly int Slot;

    /// <summary>A cell.</summary>
    public TreeCell(string id, UpgradeBranch branch, int band, int tier, int slot)
    {
        Id = id;
        Branch = branch;
        Band = band;
        Tier = tier;
        Slot = slot;
    }
}

/// <summary>A band of the tree: its branch and its height in node rows (its tallest tier).</summary>
public readonly struct TreeBand
{
    /// <summary>The band's branch.</summary>
    public readonly UpgradeBranch Branch;

    /// <summary>How many node rows it holds.</summary>
    public readonly int Slots;

    /// <summary>A band.</summary>
    public TreeBand(UpgradeBranch branch, int slots)
    {
        Branch = branch;
        Slots = slots;
    }
}

/// <summary>A link drawn from a prerequisite to the node that needs it.</summary>
public readonly struct TreeLink
{
    /// <summary>The prerequisite's id.</summary>
    public readonly string From;

    /// <summary>The dependant's id.</summary>
    public readonly string To;

    /// <summary>A link.</summary>
    public TreeLink(string from, string to)
    {
        From = from;
        To = to;
    }
}

/// <summary>The tree as drawn: every Orders node's cell, the bands in branch order, the links and the number of tiers.</summary>
public sealed class TreeLayout
{
    /// <summary>One cell per Orders node, in the catalogue's order.</summary>
    public IReadOnlyList<TreeCell> Cells { get; }

    /// <summary>The non-empty bands, in UpgradeBranch order.</summary>
    public IReadOnlyList<TreeBand> Bands { get; }

    /// <summary>One link per prerequisite a node lists that is itself a node (across bands too).</summary>
    public IReadOnlyList<TreeLink> Links { get; }

    /// <summary>The widest band's tier count (the tree's columns).</summary>
    public int Tiers { get; }

    /// <summary>A layout.</summary>
    public TreeLayout(IReadOnlyList<TreeCell> cells, IReadOnlyList<TreeBand> bands, IReadOnlyList<TreeLink> links, int tiers)
    {
        Cells = cells;
        Bands = bands;
        Links = links;
        Tiers = tiers;
    }
}

/// <summary>
/// The Orders app's upgrade tree (Saleh 2026-09-29: "a real tree with
/// prerequisites, drawn visually"; the portals spec v3 OR2, OR3, §6.2):
/// where each node sits (<see cref="Layout"/>: one band per branch in
/// branch order, a node's tier the longest prerequisite chain inside its
/// band, a tier's nodes stacked under the prerequisite that sets their tier,
/// by cost then id, so a chain keeps its row; a link from each prerequisite
/// to its dependant), whether a node can be ordered yet
/// (<see cref="Unlocked"/>: every prerequisite owned, so one in transit does
/// not count until it arrives), and what Generate World and the validator
/// refuse (<see cref="Problems"/>). Every price and prerequisite is data (the
/// upgrades' Inspector fields, the translators' content). Pure.
/// </summary>
public static class UpgradeTree
{
    /// <summary>The layout of the Orders nodes among <paramref name="nodes"/> (Home's are left out); a cycle or an unknown prerequisite never hangs it (Problems reports them).</summary>
    public static TreeLayout Layout(IReadOnlyList<TreeNode> nodes)
    {
        var drawn = new List<TreeNode>();
        var byId = new Dictionary<string, TreeNode>(StringComparer.Ordinal);
        foreach (TreeNode n in nodes ?? Array.Empty<TreeNode>())
            if (n.Venue == UpgradeVenue.Orders && n.Id.Length > 0 && !byId.ContainsKey(n.Id))
            {
                drawn.Add(n);
                byId.Add(n.Id, n);
            }

        var tiers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (TreeNode n in drawn)
            TierOf(n, byId, tiers, new HashSet<string>(StringComparer.Ordinal));

        var bands = new List<TreeBand>();
        var cellOf = new Dictionary<string, TreeCell>(StringComparer.Ordinal);
        int widest = 0;
        foreach (UpgradeBranch branch in (UpgradeBranch[])Enum.GetValues(typeof(UpgradeBranch)))
        {
            var members = drawn.FindAll(n => n.Branch == branch);
            if (members.Count == 0)
                continue;

            // Tier by tier, left to right, so each node's prerequisites have their slots: a node stacks under the
            // prerequisite that sets its tier (its anchor), siblings by cost then id, each in the first free slot
            // at or below its anchor's, so a chain keeps its row.
            int height = 0, bandTiers = 0;
            foreach (TreeNode n in members)
                bandTiers = Math.Max(bandTiers, tiers[n.Id] + 1);
            var slotOf = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int tier = 0; tier < bandTiers; tier++)
            {
                var column = members.FindAll(n => tiers[n.Id] == tier);
                var anchor = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (TreeNode n in column)
                    anchor[n.Id] = Anchor(n, byId, tiers, slotOf);
                column.Sort((a, b) => anchor[a.Id] != anchor[b.Id] ? anchor[a.Id].CompareTo(anchor[b.Id])
                                    : a.Cost != b.Cost ? a.Cost.CompareTo(b.Cost) : string.CompareOrdinal(a.Id, b.Id));
                int free = 0;
                foreach (TreeNode n in column)
                {
                    int slot = Math.Max(free, anchor[n.Id]);
                    free = slot + 1;
                    slotOf[n.Id] = slot;
                    height = Math.Max(height, slot + 1);
                    cellOf[n.Id] = new TreeCell(n.Id, branch, bands.Count, tier, slot);
                }
            }
            widest = Math.Max(widest, bandTiers);
            bands.Add(new TreeBand(branch, height));
        }

        var cells = new List<TreeCell>();
        var links = new List<TreeLink>();
        foreach (TreeNode n in drawn)
        {
            cells.Add(cellOf[n.Id]);
            foreach (string need in n.Requires)
                if (need != n.Id && byId.ContainsKey(need))
                    links.Add(new TreeLink(need, n.Id));
        }
        return new TreeLayout(cells, bands, links, widest);
    }

    /// <summary>
    /// How many orders it takes to own <paramref name="id"/> from nothing
    /// owned, one a day (each arrives the day after it is placed): 1 for a
    /// root, 1 + the longest chain of its prerequisites otherwise; 0 for an
    /// unknown id (a cycle's back edge counts as nothing). The translation
    /// notice runs this many days ahead (Translation.NoticeNight).
    /// </summary>
    public static int ChainLength(string id, IReadOnlyList<TreeNode> nodes)
    {
        var byId = new Dictionary<string, TreeNode>(StringComparer.Ordinal);
        foreach (TreeNode n in nodes ?? Array.Empty<TreeNode>())
            if (n.Id.Length > 0 && !byId.ContainsKey(n.Id))
                byId.Add(n.Id, n);
        return Chain(id ?? string.Empty, byId, new HashSet<string>(StringComparer.Ordinal));
    }

    /// <summary>True when every prerequisite of <paramref name="node"/> is owned (a prerequisite in transit is not).</summary>
    public static bool Unlocked(TreeNode node, Func<string, bool> owned) => Missing(node, owned).Count == 0;

    /// <summary>The prerequisites of <paramref name="node"/> not owned yet, in the order it lists them (its "Needs: …" line).</summary>
    public static List<string> Missing(TreeNode node, Func<string, bool> owned)
    {
        var missing = new List<string>();
        foreach (string need in node.Requires)
            if (!string.IsNullOrEmpty(need) && (owned == null || !owned(need)))
                missing.Add(need);
        return missing;
    }

    /// <summary>
    /// What Generate World and the validator refuse in the catalogue (every
    /// upgrade, both venues): a blank or repeated id, a negative cost, a
    /// prerequisite that is unknown, the node itself or at another venue, a
    /// Home upgrade with prerequisites (Home's list is no tree), and a
    /// prerequisite cycle (reported once). Empty when sound.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<TreeNode> nodes)
    {
        var problems = new List<string>();
        var byId = new Dictionary<string, TreeNode>(StringComparer.Ordinal);
        foreach (TreeNode n in nodes ?? Array.Empty<TreeNode>())
        {
            if (string.IsNullOrWhiteSpace(n.Id))
            {
                problems.Add("An upgrade has no id.");
                continue;
            }
            if (byId.ContainsKey(n.Id))
            {
                problems.Add($"Upgrade '{n.Id}' is listed twice.");
                continue;
            }
            byId.Add(n.Id, n);
        }

        foreach (TreeNode n in byId.Values)
        {
            if (n.Cost < 0)
                problems.Add($"Upgrade '{n.Id}' has a cost of {n.Cost}: 0 cr or more.");
            if (n.Venue == UpgradeVenue.Home && n.Requires.Count > 0)
            {
                problems.Add($"Upgrade '{n.Id}' is sold at Home but requires {string.Join(", ", n.Requires)}: Home's list is no tree, so a Home upgrade requires nothing.");
                continue;
            }
            foreach (string need in n.Requires)
            {
                if (need == n.Id)
                    problems.Add($"Upgrade '{n.Id}' requires itself.");
                else if (!byId.TryGetValue(need ?? string.Empty, out TreeNode required))
                    problems.Add($"Upgrade '{n.Id}' requires '{need}', which no upgrade has as its id.");
                else if (required.Venue != n.Venue)
                    problems.Add($"Upgrade '{n.Id}' ({n.Venue}) requires '{need}', which is sold at {required.Venue}: a prerequisite is sold where its dependant is.");
            }
        }

        var done = new HashSet<string>(StringComparer.Ordinal);
        foreach (TreeNode n in byId.Values)
        {
            List<string> cycle = CycleFrom(n.Id, byId, done, new List<string>());
            if (cycle != null)
            {
                problems.Add($"Upgrades {string.Join(" -> ", cycle)} form a cycle of prerequisites: none of them could ever be ordered.");
                break;
            }
        }
        return problems;
    }

    /// <summary>The slot a node stacks under: that of its in-band prerequisite with the highest tier (the lowest slot among equals), 0 for a band's root.</summary>
    private static int Anchor(TreeNode node, Dictionary<string, TreeNode> byId, Dictionary<string, int> tiers, Dictionary<string, int> slotOf)
    {
        int bestTier = -1, anchor = 0;
        foreach (string need in node.Requires)
        {
            if (need == node.Id || !byId.TryGetValue(need ?? string.Empty, out TreeNode required) || required.Branch != node.Branch ||
                !slotOf.TryGetValue(need, out int slot))
                continue;
            int tier = tiers[need];
            if (tier > bestTier || (tier == bestTier && slot < anchor))
            {
                bestTier = tier;
                anchor = slot;
            }
        }
        return anchor;
    }

    /// <summary>A node's tier: 0 without a prerequisite in its band, else one past its deepest one there (memoised; a cycle's back edge counts as nothing).</summary>
    private static int TierOf(TreeNode node, Dictionary<string, TreeNode> byId, Dictionary<string, int> tiers, HashSet<string> visiting)
    {
        if (tiers.TryGetValue(node.Id, out int known))
            return known;
        if (!visiting.Add(node.Id))
            return -1;

        int tier = 0;
        foreach (string need in node.Requires)
            if (need != node.Id && byId.TryGetValue(need ?? string.Empty, out TreeNode required) && required.Branch == node.Branch)
                tier = Math.Max(tier, TierOf(required, byId, tiers, visiting) + 1);

        visiting.Remove(node.Id);
        tiers[node.Id] = tier;
        return tier;
    }

    /// <summary>The orders to own <paramref name="id"/> (ChainLength), with the ids on the current path in <paramref name="visiting"/>.</summary>
    private static int Chain(string id, Dictionary<string, TreeNode> byId, HashSet<string> visiting)
    {
        if (!byId.TryGetValue(id, out TreeNode node) || !visiting.Add(id))
            return 0;
        int longest = 0;
        foreach (string need in node.Requires)
            if (need != id)
                longest = Math.Max(longest, Chain(need ?? string.Empty, byId, visiting));
        visiting.Remove(id);
        return longest + 1;
    }

    /// <summary>The first cycle of prerequisites reached from <paramref name="id"/> (its ids, back to the first), or null; <paramref name="done"/> holds the ids already cleared.</summary>
    private static List<string> CycleFrom(string id, Dictionary<string, TreeNode> byId, HashSet<string> done, List<string> path)
    {
        if (done.Contains(id))
            return null;
        int at = path.IndexOf(id);
        if (at >= 0)
        {
            List<string> cycle = path.GetRange(at, path.Count - at);
            cycle.Add(id);
            return cycle;
        }
        if (!byId.TryGetValue(id, out TreeNode node))
            return null;

        path.Add(id);
        foreach (string need in node.Requires)
        {
            if (need == id)
                continue;
            List<string> cycle = CycleFrom(need ?? string.Empty, byId, done, path);
            if (cycle != null)
                return cycle;
        }
        path.RemoveAt(path.Count - 1);
        done.Add(id);
        return null;
    }
}
