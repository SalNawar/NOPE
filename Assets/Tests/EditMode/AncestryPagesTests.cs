using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Lineage Archive: cards for the premades who are who they claim and
/// for authored people (never for an impostor), the name search with country
/// and era filters, a card's fields and links, and the content rules.
/// </summary>
public class AncestryPagesTests
{
    private static SitePage Open(SiteWorld w, string address)
    {
        SitePage page = Sites.Page(w, address);
        Assert.IsTrue(page.Found, address);
        return page;
    }

    [Test]
    public void Cards_HonestPremadesThenPeople_WithTheirRelations()
    {
        List<PersonCard> cards = AncestryPages.Cards(SiteFixture.Premades(), SiteFixture.Ancestry());
        CollectionAssert.AreEqual(new[] { "senenmut", "aspasia", "hatnefer", "neferure" }, cards.Select(c => c.Id), "Socrates is an impostor: no card");
        PersonCard senenmut = cards[0];
        Assert.AreEqual(("Senenmut", "14 Mar 1505 BCE", "egypt_ancient", "Steward of the Pharaoh's household."), (senenmut.Name, senenmut.Born, senenmut.PlaceId, senenmut.Note));
        CollectionAssert.AreEqual(new[] { ("Mother", "hatnefer") }, senenmut.Relations);
        Assert.AreEqual("c. 1480 BCE", cards[3].Died);

        AncestryContent noPremades = SiteFixture.Ancestry();
        noPremades.includePremades = false;
        CollectionAssert.AreEqual(new[] { "hatnefer", "neferure" }, AncestryPages.Cards(SiteFixture.Premades(), noPremades).Select(c => c.Id));
        CollectionAssert.IsEmpty(AncestryPages.Cards(SiteFixture.Premades(), noPremades)[1].Relations, "a relation to a card that is not there is dropped");
    }

    [Test]
    public void Search_ByName_IgnoringCase_SortedByName()
    {
        SiteWorld w = SiteFixture.World();
        CollectionAssert.AreEqual(new[] { "Senenmut" }, AncestryPages.Search(w.People, " senen ", null, null, w.PlaceById).Select(c => c.Name));
        CollectionAssert.AreEqual(new[] { "Aspasia", "Hatnefer", "Neferure", "Senenmut" }, AncestryPages.Search(w.People, "", null, null, w.PlaceById).Select(c => c.Name));
        CollectionAssert.IsEmpty(AncestryPages.Search(w.People, "Socrates", null, null, w.PlaceById));
    }

    [Test]
    public void Search_CountryAndEraFilters()
    {
        SiteWorld w = SiteFixture.World();
        CollectionAssert.AreEqual(new[] { "Aspasia" }, AncestryPages.Search(w.People, null, "greece", null, w.PlaceById).Select(c => c.Name));
        CollectionAssert.AreEqual(new[] { "Aspasia", "Hatnefer", "Neferure", "Senenmut" }, AncestryPages.Search(w.People, null, null, "ancient", w.PlaceById).Select(c => c.Name));
        CollectionAssert.AreEqual(new[] { "Hatnefer", "Neferure" }, AncestryPages.Search(w.People, "e", "egypt", "ancient", w.PlaceById).Where(c => c.Name != "Senenmut").Select(c => c.Name));
        CollectionAssert.IsEmpty(AncestryPages.Search(w.People, null, "egypt", "medieval", w.PlaceById));

        var lost = new List<PersonCard> { new PersonCard { Id = "x", Name = "Nobody", PlaceId = "atlantis" } };
        Assert.AreEqual(1, AncestryPages.Search(lost, null, null, null, w.PlaceById).Count, "an unknown place matches no filter only");
        CollectionAssert.IsEmpty(AncestryPages.Search(lost, null, "egypt", null, w.PlaceById));
    }

    [Test]
    public void SearchPage_ChipsHitsAndLinks()
    {
        SiteWorld w = SiteFixture.World();
        SitePage page = Open(w, "chronet://lineage/search?name=a&country=greece");
        Assert.AreEqual("chronet://lineage/search?name=a&country=greece", page.Address);

        List<PageBlock> chips = page.Blocks.Where(b => b.Kind == PageBlockKind.Chips).ToList();
        CollectionAssert.AreEqual(new[] { "site.lineage.all", "Egypt", "Greece", "Italy" }, chips[0].Links.Select(l => l.Text), "past countries only");
        CollectionAssert.AreEqual(new[] { false, false, true, false }, chips[0].Links.Select(l => l.Active));
        Assert.AreEqual("chronet://lineage/search?name=a", chips[0].Links[0].Address, "All clears the country, keeps the name");
        CollectionAssert.AreEqual(new[] { "site.lineage.all", "Ancient", "Medieval" }, chips[1].Links.Select(l => l.Text), "past eras only, in order");
        Assert.AreEqual("chronet://lineage/search?name=a&country=greece&era=medieval", chips[1].Links[2].Address);

        CollectionAssert.Contains(page.Blocks.Select(b => b.Text).ToList(), "site.lineage.hits(1|a)");
        PageBlock table = page.Blocks.Single(b => b.Kind == PageBlockKind.Table);
        List<PageCell> aspasia = table.Rows.Single();
        Assert.AreEqual(("Aspasia", "chronet://lineage/person/aspasia"), (aspasia[0].Text, aspasia[0].Address));
        Assert.AreEqual("12 Sep 470 BCE", aspasia[1].Text);
        Assert.AreEqual(("Periclean Athens (Ancient)", "chronet://chronopedia/greece/ancient"), (aspasia[2].Text, aspasia[2].Address));

        SitePage root = Open(w, "chronet://lineage");
        CollectionAssert.Contains(root.Blocks.Select(b => b.Text).ToList(), "site.lineage.count(4)");
        CollectionAssert.Contains(Open(w, "chronet://lineage/search?name=zzz").Blocks.Select(b => b.Text).ToList(), "site.lineage.noHits");
    }

    [Test]
    public void Card_FieldsRelationsAndLinks()
    {
        SiteWorld w = SiteFixture.World();
        SitePage page = Open(w, "chronet://lineage/person/hatnefer");
        Assert.AreEqual("chronet://lineage/person/hatnefer", page.Address);
        Assert.AreEqual("Hatnefer", page.Title);
        List<PageField> f = page.Blocks.Single(b => b.Kind == PageBlockKind.Fields).Fields;
        CollectionAssert.AreEqual(new[] { "site.lineage.field.name", "site.lineage.field.born", "site.lineage.field.died", "site.lineage.field.place", "site.lineage.field.note" }, f.Select(x => x.Label));
        CollectionAssert.AreEqual(new[] { "Hatnefer", "c. 1540 BCE", "site.lineage.unknown", "New Kingdom Egypt (Ancient)", "Mother of Senenmut." }, f.Select(x => x.Value));
        Assert.AreEqual("chronet://chronopedia/egypt/ancient", f[3].Address, "the place links to its article");

        PageBlock son = page.Blocks.Single(b => b.Kind == PageBlockKind.Link && b.Text == "site.lineage.relation(Son|Senenmut)");
        Assert.AreEqual("chronet://lineage/person/senenmut", son.Address);
        CollectionAssert.Contains(page.Links().ToList(), "chronet://lineage");

        Assert.AreEqual("Senenmut", Open(w, "chronet://lineage/person/SENENMUT").Title, "ids ignore case");
        Assert.IsFalse(Sites.Page(w, "chronet://lineage/person/socrates").Found, "no card of an impostor");
        Assert.IsFalse(Sites.Page(w, "chronet://lineage/people").Found);
    }

    [Test]
    public void Problems_EveryRule()
    {
        var places = new HashSet<string> { "egypt_ancient" };
        var premades = new[] { "senenmut" };
        var travellers = new[] { "Senenmut", "Nakht" };
        CollectionAssert.IsEmpty(AncestryPages.Problems(SiteFixture.Ancestry(), premades, places, travellers));

        var bad = new AncestryContent
        {
            people = new List<PersonEntry>
            {
                new PersonEntry { id = "senenmut", name = "Again", place = "egypt_ancient" },
                new PersonEntry { id = "", name = "", place = "atlantis" },
                new PersonEntry { id = "nakht", name = " NAKHT ", place = "egypt_ancient" },
            },
            relations = new List<RelationEntry> { new RelationEntry { person = "ghost", kind = "", other = "senenmut" } }
        };
        List<string> p = AncestryPages.Problems(bad, premades, places, travellers);
        void Has(string fragment) => Assert.IsTrue(p.Any(x => x.Contains(fragment)), $"expected '{fragment}' in [{string.Join(" | ", p)}]");
        Has("'senenmut': the id is used twice");
        Has("has no id");
        Has("no name");
        Has("unknown place 'atlantis'");
        Has("unknown person 'ghost'");
        Has("no kind");
        Has("' NAKHT ' is a traveller's name");
        Assert.AreEqual(7, p.Count, string.Join(" | ", p));
    }
}
