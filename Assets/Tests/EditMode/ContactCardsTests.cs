using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The Diplomatic Contacts' Rolodex (demo 1018a note 14; the desk machines spec §10): each contact's tip or quiet line from today's travellers, the lead on top, and the content's checks.</summary>
public class ContactCardsTests
{
    private static ContactEntry Contact(string id, string tip, params LieKind[] lies) => new ContactEntry
    {
        id = id, name = id + " name", post = id + " post", lies = lies.Select(l => l.ToString()).ToList(), tip = tip, quiet = id + " is quiet."
    };

    private static readonly ContactEntry[] Contacts =
    {
        Contact("seals", "A seal is off on papers to {place}.", LieKind.ForgedSeal),
        Contact("photos", "A borrowed face is going to {place}.", LieKind.SwappedPhoto),
        Contact("machines", "A clone is headed for {place}.", LieKind.ClonedChip, LieKind.WashedPaper),
    };

    [Test]
    public void AContactWhoseFaultTravelsToday_TipsTheClerkOff_WithThePlace_AndTheRestAreQuiet()
    {
        var travellers = new[] { new ContactLead(null, "Rome"), new ContactLead(LieKind.WashedPaper, "Edo"), new ContactLead(LieKind.DoctoredIdentity, "Ur") };
        List<ContactCard> cards = ContactCards.ForDay(Contacts, travellers, new SeededRandom(3));
        Assert.AreEqual(3, cards.Count, "one card per contact");
        Assert.AreEqual("machines name", cards[0].Name, "the one tip is the lead, on top");
        Assert.IsTrue(cards[0].Tip);
        Assert.AreEqual("A clone is headed for Edo.", cards[0].Line);
        Assert.AreEqual("machines post", cards[0].Post);
        CollectionAssert.AreEqual(new[] { "seals is quiet.", "photos is quiet." }, cards.Skip(1).Select(c => c.Line).ToArray(), "the rest in authored order");
        Assert.IsFalse(cards[1].Tip);
    }

    [Test]
    public void TheDraws_AreSeeded_AndATipNamesOneOfItsTravellers()
    {
        var travellers = new[] { new ContactLead(LieKind.ForgedSeal, "Rome"), new ContactLead(LieKind.ForgedSeal, "Edo"), new ContactLead(LieKind.SwappedPhoto, "Ur") };
        List<ContactCard> a = ContactCards.ForDay(Contacts, travellers, new SeededRandom(7));
        List<ContactCard> b = ContactCards.ForDay(Contacts, travellers, new SeededRandom(7));
        CollectionAssert.AreEqual(a.Select(c => c.Line).ToArray(), b.Select(c => c.Line).ToArray(), "same seed, same cards");
        string seals = a.Single(c => c.Name == "seals name").Line;
        Assert.That(seals, Is.EqualTo("A seal is off on papers to Rome.").Or.EqualTo("A seal is off on papers to Edo."));
        Assert.IsTrue(a[0].Tip && a[1].Tip, "two tips: one of them leads");
        var leads = new HashSet<string>(Enumerable.Range(1, 40).Select(s => ContactCards.ForDay(Contacts, travellers, new SeededRandom(s))[0].Name));
        CollectionAssert.AreEquivalent(new[] { "seals name", "photos name" }, leads, "either tip may lead");
    }

    [Test]
    public void NoFaultToday_EveryContactIsQuiet_InAuthoredOrder()
    {
        List<ContactCard> cards = ContactCards.ForDay(Contacts, new[] { new ContactLead(null, "Rome") }, new SeededRandom(1));
        CollectionAssert.AreEqual(new[] { "seals name", "photos name", "machines name" }, cards.Select(c => c.Name).ToArray());
        Assert.IsTrue(cards.All(c => !c.Tip));
        CollectionAssert.IsEmpty(ContactCards.ForDay(null, null, null));
    }

    [Test]
    public void Problems_RefuseBlankLines_ATipWithoutItsPlace_AnUnknownLie_AndALieWatchedTwice()
    {
        CollectionAssert.IsEmpty(ContactCards.Problems(Contacts));
        var bad = new List<ContactEntry>
        {
            Contact("a", "no place here", LieKind.ForgedSeal),
            Contact("a", "{place}", LieKind.ForgedSeal),
            new ContactEntry { id = "c", name = "", post = "p", lies = new List<string> { "Nonsense" }, tip = "{place}", quiet = "" },
        };
        List<string> problems = ContactCards.Problems(bad);
        Assert.IsTrue(problems.Any(p => p.Contains("{place}")), "the tip names no place");
        Assert.IsTrue(problems.Any(p => p.Contains("listed twice")));
        Assert.IsTrue(problems.Any(p => p.Contains("already watched")));
        Assert.IsTrue(problems.Any(p => p.Contains("'Nonsense' is not a LieKind")));
        Assert.IsTrue(problems.Any(p => p.Contains("quiet line is blank")));
        Assert.IsTrue(problems.Any(p => p.Contains("neither may be blank")));
    }
}
