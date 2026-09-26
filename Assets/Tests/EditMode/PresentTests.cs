using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The present (traveller types H1): the home of 2150 citizens is the
/// leader's Future place, or the neutral present with no leader; its row is
/// in today's facts (every book lists it from day 1), once.
/// </summary>
public class PresentTests
{
    private static PresentPlace Neutral() => new PresentPlace(Present.NeutralNationId, "future", "Temporal Customs Zone (Future)", 2150, 2080, 2132,
        new[]
        {
            new KeyValuePair<ClueCategory, string>(ClueCategory.Currency, "Credits"),
            new KeyValuePair<ClueCategory, string>(ClueCategory.Technology, "Wrist comm")
        });

    private static PresentPlace Leader() => new PresentPlace("japan", "future", "Neo-Tokyo Bay (Future)", 2150, 2080, 2132,
        new[] { new KeyValuePair<ClueCategory, string>(ClueCategory.Currency, "Digital yen") });

    [Test]
    public void Choose_WithALeader_IsTheLeadersFuturePlace()
    {
        PresentPlace present = Present.Choose("japan", new[] { Leader() }, Neutral());
        Assert.AreEqual("japan", present.NationId);
        Assert.AreEqual("Digital yen", present.Fact(ClueCategory.Currency));
        Assert.IsFalse(present.IsNeutral);
    }

    [Test]
    public void Choose_WithNoLeader_IsTheNeutralPresent()
    {
        PresentPlace present = Present.Choose(null, new[] { Leader() }, Neutral());
        Assert.AreEqual(Present.NeutralNationId, present.NationId);
        Assert.AreEqual("Credits", present.Fact(ClueCategory.Currency));
        Assert.IsTrue(present.IsNeutral);
        Assert.AreEqual("Temporal Customs Zone (Future)", present.Label);
        Assert.AreEqual(2150, present.Year);
        Assert.AreEqual(2080, present.BirthYearMin);
        Assert.AreEqual(2132, present.BirthYearMax);
    }

    [Test]
    public void Choose_ALeaderWithoutAFuturePlace_FallsBackToTheNeutralPresent()
    {
        Assert.IsTrue(Present.Choose("egypt", new[] { Leader() }, Neutral()).IsNeutral, "only the leader's own Future place stands in");
        Assert.IsTrue(Present.Choose("japan", null, Neutral()).IsNeutral);
    }

    [Test]
    public void Fact_IsNullForACategoryThePresentHasNot()
    {
        Assert.IsNull(Neutral().Fact(ClueCategory.Politics));
        Assert.AreEqual(2, Neutral().Facts.Count);
    }

    [Test]
    public void AddRow_ListsThePresentInEveryBook_Once()
    {
        var table = new FactTable();
        table.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");

        Assert.IsTrue(Present.AddRow(table, Neutral()));
        CollectionAssert.AreEqual(new[] { "Deben", "Credits" }, table.Rows(ClueCategory.Currency).Select(r => r.Value).ToArray(), "after today's places");
        Assert.AreEqual("Temporal Customs Zone (Future)", table.OriginLabel(Present.NeutralNationId, "future"));
        Assert.AreEqual("Wrist comm", table.Get(Present.NeutralNationId, "future", ClueCategory.Technology));

        Assert.IsFalse(Present.AddRow(table, Neutral()), "a place the table lists is not added twice");
        Assert.AreEqual(2, table.Rows(ClueCategory.Currency).Count);
    }

    [Test]
    public void AddRow_SkipsALeadersFuturePlaceThatIsADestinationToday()
    {
        var table = new FactTable();
        table.Add("japan", "future", "Neo-Tokyo Bay (Future)", ClueCategory.Currency, "Digital yen");
        Assert.IsFalse(Present.AddRow(table, Leader()));
        Assert.AreEqual(1, table.Rows(ClueCategory.Currency).Count);
        Assert.IsFalse(Present.AddRow(table, null));
        Assert.IsFalse(Present.AddRow(null, Neutral()));
    }
}
