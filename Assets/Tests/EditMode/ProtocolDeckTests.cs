using System.Linq;
using NUnit.Framework;

/// <summary>The Interview Protocols' card ring as a deck (demo 1018a note 14; the desk machines spec §10): the front card, a flick, a card asked.</summary>
public class ProtocolDeckTests
{
    private static ProtocolCard[] Cards(params string[] ids) => ids.Select(id => new ProtocolCard(id, id.ToUpperInvariant())).ToArray();

    [Test]
    public void AnEmptyDeck_HasNoFront_AndDoesNotFlick()
    {
        var deck = new ProtocolDeck();
        Assert.IsTrue(deck.Empty);
        Assert.AreEqual(string.Empty, deck.Front.Id);
        Assert.IsFalse(deck.Flick());
        deck.Set(null);
        Assert.IsTrue(deck.Empty);
    }

    [Test]
    public void AFlick_BringsTheNextCardToTheFront_AndWrapsRound()
    {
        var deck = new ProtocolDeck();
        deck.Set(Cards("q:a", "q:b", "q:c"));
        Assert.AreEqual("q:a", deck.Front.Id);
        Assert.AreEqual("Q:A", deck.Front.Label);
        Assert.IsTrue(deck.Flick());
        Assert.AreEqual("q:b", deck.Front.Id);
        deck.Flick();
        deck.Flick();
        Assert.AreEqual("q:a", deck.Front.Id, "after the last, the first");
        deck.Set(Cards("q:a"));
        Assert.IsFalse(deck.Flick(), "one card does not flick");
    }

    [Test]
    public void TheFrontCard_StaysTheFront_WhileTheDeckChangesRoundIt()
    {
        var deck = new ProtocolDeck();
        deck.Set(Cards("q:a", "q:b", "q:c"));
        deck.Flick();
        deck.Set(Cards("q:x", "q:a", "q:b", "q:c"));
        Assert.AreEqual("q:b", deck.Front.Id, "a question unlocked before it");
        Assert.AreEqual(2, deck.FrontIndex);
    }

    [Test]
    public void ACardAsked_LeavesTheDeck_AndTheNextTakesTheFront()
    {
        var deck = new ProtocolDeck();
        deck.Set(Cards("q:a", "q:b", "q:c"));
        deck.Flick();
        deck.Set(Cards("q:a", "q:c"));
        Assert.AreEqual("q:c", deck.Front.Id, "the card after the one asked");
        deck.Set(Cards("q:a"));
        Assert.AreEqual("q:a", deck.Front.Id, "the last asked: the first again");
        deck.Set(Cards());
        Assert.IsTrue(deck.Empty);
    }
}
