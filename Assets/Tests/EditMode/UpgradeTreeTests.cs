using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Orders app's upgrade tree (Saleh 2026-09-29: a real tree with
/// prerequisites on everything but each branch's root, drawn; the portals
/// spec v3 OR2, OR3, §6): one band per branch in branch order, a node's tier
/// the longest prerequisite chain inside its band, a tier's nodes stacked
/// under their prerequisite by cost then id (a chain keeps its row), the
/// links from a prerequisite to its dependant (across bands too), a node
/// unlocked once every prerequisite is owned, the orders a chain takes, and
/// the catalogue's checks.
/// </summary>
public class UpgradeTreeTests
{
    private static TreeNode Node(string id, int cost, UpgradeBranch branch, params string[] requires) =>
        new TreeNode(id, UpgradeVenue.Orders, branch, cost, requires);

    private static TreeCell Cell(TreeLayout layout, string id) => layout.Cells.Single(c => c.Id == id);

    /// <summary>The catalogue (Saleh 2026-09-29, "prerequisites on everything": each branch one root, every other node in a chain).</summary>
    private static readonly TreeNode[] Catalogue =
    {
        Node("scanner_autofeed", 200, UpgradeBranch.Desk),
        Node("adv_scanner", 300, UpgradeBranch.Desk, "scanner_autofeed"),
        Node("interview_protocols", 120, UpgradeBranch.Interview),
        Node("diplo_contacts", 350, UpgradeBranch.Contacts),
        Node("repair_portal_02", 150, UpgradeBranch.Portals),
        Node("repair_return_gate", 200, UpgradeBranch.Portals, "repair_portal_02"),
        Node("repair_portal_04", 250, UpgradeBranch.Portals, "repair_portal_02"),
        Node("repair_portal_05", 350, UpgradeBranch.Portals, "repair_portal_04"),
        Node("tr_near_east_spoken", 80, UpgradeBranch.Interview, "interview_protocols"),
        Node("tr_mediterranean_spoken", 80, UpgradeBranch.Interview, "interview_protocols"),
        Node("tr_east_asia_spoken", 80, UpgradeBranch.Interview, "interview_protocols"),
        Node("tr_north_europe_spoken", 80, UpgradeBranch.Interview, "interview_protocols"),
    };

    [Test]
    public void Layout_OneBandPerBranchInEnumOrder()
    {
        TreeLayout layout = UpgradeTree.Layout(Catalogue);

        CollectionAssert.AreEqual(new[] { UpgradeBranch.Desk, UpgradeBranch.Interview, UpgradeBranch.Portals, UpgradeBranch.Contacts },
                                  layout.Bands.Select(b => b.Branch).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 4, 2, 1 }, layout.Bands.Select(b => b.Slots).ToArray(), "a band is as tall as its tallest tier");
        Assert.AreEqual(3, layout.Tiers, "the portal chain 02, 04, 05");
        Assert.AreEqual(2, Cell(layout, "repair_portal_02").Band);
    }

    [Test]
    public void Layout_AnEmptyBranchHasNoBand()
    {
        TreeLayout layout = UpgradeTree.Layout(new[] { Node("a", 10, UpgradeBranch.Contacts), Node("b", 10, UpgradeBranch.Desk) });

        CollectionAssert.AreEqual(new[] { UpgradeBranch.Desk, UpgradeBranch.Contacts }, layout.Bands.Select(b => b.Branch).ToArray());
        Assert.AreEqual(1, Cell(layout, "a").Band);
    }

    [Test]
    public void Layout_TierIsTheLongestChainInTheBand()
    {
        TreeLayout layout = UpgradeTree.Layout(Catalogue);

        Assert.AreEqual(0, Cell(layout, "scanner_autofeed").Tier);
        Assert.AreEqual(1, Cell(layout, "adv_scanner").Tier);
        Assert.AreEqual(0, Cell(layout, "repair_portal_02").Tier);
        Assert.AreEqual(1, Cell(layout, "repair_portal_04").Tier);
        Assert.AreEqual(2, Cell(layout, "repair_portal_05").Tier);
        Assert.AreEqual(1, Cell(layout, "repair_return_gate").Tier);
        Assert.AreEqual(1, Cell(layout, "tr_near_east_spoken").Tier);

        TreeLayout two = UpgradeTree.Layout(new[]
        {
            Node("a", 10, UpgradeBranch.Desk), Node("b", 10, UpgradeBranch.Desk, "a"), Node("x", 5, UpgradeBranch.Desk),
            Node("c", 10, UpgradeBranch.Desk, "x", "b")
        });
        Assert.AreEqual(2, Cell(two, "c").Tier, "past its longest chain, not its shortest");
    }

    [Test]
    public void Layout_SiblingsByCostThenId()
    {
        TreeLayout layout = UpgradeTree.Layout(Catalogue);

        string[] translators = layout.Cells.Where(c => c.Tier == 1 && c.Branch == UpgradeBranch.Interview).OrderBy(c => c.Slot).Select(c => c.Id).ToArray();
        CollectionAssert.AreEqual(new[] { "tr_east_asia_spoken", "tr_mediterranean_spoken", "tr_near_east_spoken", "tr_north_europe_spoken" }, translators, "same cost: by id");
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, layout.Cells.Where(c => c.Tier == 1 && c.Branch == UpgradeBranch.Interview).Select(c => c.Slot).OrderBy(s => s).ToArray());
        Assert.AreEqual(0, Cell(layout, "repair_return_gate").Slot, "200 cr before Portal 04's 250");
        Assert.AreEqual(1, Cell(layout, "repair_portal_04").Slot);
    }

    [Test]
    public void Layout_AChainKeepsItsRow()
    {
        TreeLayout layout = UpgradeTree.Layout(Catalogue);

        Assert.AreEqual(Cell(layout, "repair_portal_04").Slot, Cell(layout, "repair_portal_05").Slot, "05 beside the 04 it needs, not at the band's top");
        Assert.AreEqual(Cell(layout, "scanner_autofeed").Slot, Cell(layout, "adv_scanner").Slot);
    }

    [Test]
    public void Layout_ACrossBranchPrerequisiteKeepsItsOwnBand()
    {
        TreeLayout layout = UpgradeTree.Layout(new[] { Node("protocols", 120, UpgradeBranch.Interview), Node("contacts", 350, UpgradeBranch.Contacts, "protocols") });

        Assert.AreEqual(0, Cell(layout, "contacts").Tier, "a prerequisite in another band does not move it right");
        Assert.AreEqual(1, Cell(layout, "contacts").Band);
        Assert.AreEqual(("protocols", "contacts"), (layout.Links.Single().From, layout.Links.Single().To), "its link runs across the bands");
    }

    [Test]
    public void Layout_LinksRunFromEachPrerequisiteToItsDependant()
    {
        TreeLayout layout = UpgradeTree.Layout(Catalogue);

        Assert.AreEqual(8, layout.Links.Count);
        Assert.AreEqual(4, layout.Links.Count(l => l.From == "interview_protocols"), "the four translators");
        CollectionAssert.IsSubsetOf(new[] { "scanner_autofeed>adv_scanner", "repair_portal_02>repair_return_gate", "repair_portal_02>repair_portal_04", "repair_portal_04>repair_portal_05" },
                                    layout.Links.Select(l => l.From + ">" + l.To).ToArray());
    }

    [Test]
    public void Layout_LeavesOutHomesUpgrades()
    {
        TreeLayout layout = UpgradeTree.Layout(new[] { Node("a", 10, UpgradeBranch.Desk), new TreeNode("house_air_filter", UpgradeVenue.Home, UpgradeBranch.Desk, 120, null) });

        CollectionAssert.AreEqual(new[] { "a" }, layout.Cells.Select(c => c.Id).ToArray());
    }

    [Test]
    public void Layout_ACycleOrAnUnknownPrerequisite_StillPlacesEveryNode()
    {
        TreeLayout layout = UpgradeTree.Layout(new[]
        {
            Node("a", 10, UpgradeBranch.Desk, "b"), Node("b", 10, UpgradeBranch.Desk, "a"), Node("c", 10, UpgradeBranch.Desk, "nowhere")
        });

        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, layout.Cells.Select(c => c.Id).ToArray());
        Assert.AreEqual(0, Cell(layout, "c").Tier, "an unknown prerequisite is no chain");
        Assert.IsFalse(layout.Links.Any(l => l.From == "nowhere"), "and no link");
    }

    [Test]
    public void Layout_AnEmptyCatalogue()
    {
        CollectionAssert.IsEmpty(UpgradeTree.Layout(new TreeNode[0]).Cells);
        CollectionAssert.IsEmpty(UpgradeTree.Layout(null).Bands);
        Assert.AreEqual(0, UpgradeTree.Layout(null).Tiers);
    }

    [Test]
    public void Unlocked_EveryPrerequisiteOwned()
    {
        TreeNode two = Node("c", 10, UpgradeBranch.Desk, "a", "b");

        Assert.IsTrue(UpgradeTree.Unlocked(Node("a", 10, UpgradeBranch.Desk), id => false), "no prerequisite");
        Assert.IsFalse(UpgradeTree.Unlocked(two, id => id == "a"));
        Assert.IsTrue(UpgradeTree.Unlocked(two, id => id == "a" || id == "b"));
        CollectionAssert.AreEqual(new[] { "b" }, UpgradeTree.Missing(two, id => id == "a"));
    }

    [Test]
    public void Unlocked_APrerequisiteInTransitIsNotOwned()
    {
        var log = new List<OrderEntry> { new OrderEntry { upgradeId = "scanner_autofeed", orderedDay = 3, price = 200 } };
        var owned = new HashSet<string>();

        Assert.AreEqual(OrderState.InTransit, Orders.StateOf("scanner_autofeed", false, true, log, 0, 200));
        Assert.IsFalse(UpgradeTree.Unlocked(Catalogue[1], owned.Contains), "the dependant waits for the delivery");
    }

    [Test]
    public void ChainLength_IsTheOrdersFromNothingOwned_OneADay()
    {
        Assert.AreEqual(1, UpgradeTree.ChainLength("scanner_autofeed", Catalogue));
        Assert.AreEqual(2, UpgradeTree.ChainLength("adv_scanner", Catalogue));
        Assert.AreEqual(3, UpgradeTree.ChainLength("repair_portal_05", Catalogue));
        Assert.AreEqual(0, UpgradeTree.ChainLength("ghost", Catalogue));
        TreeNode[] loop = { Node("a", 10, UpgradeBranch.Desk, "b"), Node("b", 10, UpgradeBranch.Desk, "a") };
        Assert.AreEqual(2, UpgradeTree.ChainLength("a", loop), "a cycle's back edge counts as nothing");
    }

    [Test]
    public void Step_TheArrowsFollowTheLinksAndTheTiers()
    {
        TreeLayout layout = UpgradeTree.Layout(Catalogue);

        Assert.AreEqual("scanner_autofeed", UpgradeTree.Step(layout, null, 1, 0), "no selection: the first node");
        Assert.AreEqual("adv_scanner", UpgradeTree.Step(layout, "scanner_autofeed", 1, 0), "right: its dependant");
        Assert.AreEqual("scanner_autofeed", UpgradeTree.Step(layout, "adv_scanner", -1, 0), "left: its prerequisite");
        Assert.IsNull(UpgradeTree.Step(layout, "adv_scanner", 1, 0), "nothing needs it");
        Assert.AreEqual("tr_east_asia_spoken", UpgradeTree.Step(layout, "interview_protocols", 1, 0), "the first of its dependants");
        Assert.AreEqual("interview_protocols", UpgradeTree.Step(layout, "scanner_autofeed", 0, 1), "down: the next node of the tier, across bands");
        Assert.AreEqual("tr_mediterranean_spoken", UpgradeTree.Step(layout, "tr_east_asia_spoken", 0, 1));
        Assert.AreEqual("adv_scanner", UpgradeTree.Step(layout, "tr_east_asia_spoken", 0, -1), "up: the tier's node above");
        Assert.IsNull(UpgradeTree.Step(layout, "scanner_autofeed", 0, -1));
        Assert.AreEqual("repair_portal_05", UpgradeTree.Step(layout, "repair_portal_04", 1, 0));
    }

    [Test]
    public void Problems_NoneForTheCatalogue()
    {
        CollectionAssert.IsEmpty(UpgradeTree.Problems(Catalogue));
    }

    [Test]
    public void Problems_ABlankOrRepeatedId()
    {
        List<string> problems = UpgradeTree.Problems(new[] { Node("", 10, UpgradeBranch.Desk), Node("a", 10, UpgradeBranch.Desk), Node("a", 20, UpgradeBranch.Desk) });

        Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("no id")));
        Assert.IsTrue(problems.Any(p => p.Contains("'a'") && p.Contains("twice")));
    }

    [Test]
    public void Problems_AnUnknownPrerequisite()
    {
        List<string> problems = UpgradeTree.Problems(new[] { Node("a", 10, UpgradeBranch.Desk, "ghost"), Node("b", 10, UpgradeBranch.Desk, "b") });

        Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'a'") && p.Contains("'ghost'")));
        Assert.IsTrue(problems.Any(p => p.Contains("'b'") && p.Contains("itself")));
    }

    [Test]
    public void Problems_ACycle()
    {
        List<string> problems = UpgradeTree.Problems(new[] { Node("a", 10, UpgradeBranch.Desk, "c"), Node("b", 10, UpgradeBranch.Desk, "a"), Node("c", 10, UpgradeBranch.Desk, "b") });

        Assert.AreEqual(1, problems.Count, string.Join("\n", problems));
        StringAssert.Contains("cycle", problems[0]);
    }

    [Test]
    public void Problems_APrerequisiteAtAnotherVenue()
    {
        List<string> problems = UpgradeTree.Problems(new[]
        {
            new TreeNode("house_air_filter", UpgradeVenue.Home, UpgradeBranch.Desk, 120, new[] { "a" }),
            Node("a", 10, UpgradeBranch.Desk, "house_insulation"),
            new TreeNode("house_insulation", UpgradeVenue.Home, UpgradeBranch.Desk, 180, null),
        });

        Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("'house_air_filter'") && p.Contains("Home")), "Home's list is no tree");
        Assert.IsTrue(problems.Any(p => p.Contains("'a'") && p.Contains("'house_insulation'")), "an Orders node cannot need a Home item");
    }

    [Test]
    public void Problems_ANegativeCost()
    {
        List<string> problems = UpgradeTree.Problems(new[] { Node("a", -1, UpgradeBranch.Desk) });

        Assert.AreEqual(1, problems.Count);
        StringAssert.Contains("cost", problems[0]);
    }
}
