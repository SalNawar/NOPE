using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Which fields can carry a liar's tell: only those the player can disprove.
/// Today holds the claim Egypt, the home Iraq, and Greece (whose Language
/// equals Iraq's under the scanner comparison); books exist for Currency and
/// Language only.
/// </summary>
public class ForgeryTests
{
    /// <summary>Books for the tell tests: Currency and Language (no Geography book).</summary>
    private static readonly HashSet<ClueCategory> BookCategories = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language };

    /// <summary>The home under test: Babylonia, born 1810..1790 BCE.</summary>
    private static readonly HomeCandidate IraqHome = new HomeCandidate("iraq", "ancient", -1810, -1790);

    /// <summary>
    /// Today for the tell tests: the claim Egypt, the home Iraq, and Greece,
    /// whose Language equals Iraq's under the scanner comparison.
    /// </summary>
    private static FactTable World()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Language, "Middle Egyptian");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Geography, "Thebes");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Currency, "Silver shekel");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Language, "Old Babylonian");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Geography, "Babylon");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Currency, "Silver drachma");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Language, " old babylonian ");
        return t;
    }

    private static bool IsTell(ClueCategory category, HomeCandidate home, string cover = "3 Jun 1450 BCE", FactTable facts = null) =>
        Forgery.IsProvableTell(category, "egypt", "ancient", cover, home, facts ?? World(), BookCategories);

    [Test]
    public void PlaceFact_WithABook_AndAHomeValueOnlyTheHomeHas_IsATell()
    {
        // Iraq's own Currency row matches the value: the home's own row is never a collision.
        Assert.IsTrue(IsTell(ClueCategory.Currency, IraqHome));
    }

    [Test]
    public void PlaceFact_WithoutABook_IsNotATell()
    {
        Assert.IsFalse(IsTell(ClueCategory.Geography, IraqHome));
    }

    [Test]
    public void PlaceFact_EqualToTheClaimUnderTheScannerComparison_IsNotATell()
    {
        FactTable t = World();
        t.Add("italy", "ancient", "Republican Rome (Ancient)", ClueCategory.Currency, " DEBEN ");
        Assert.IsFalse(IsTell(ClueCategory.Currency, new HomeCandidate("italy", "ancient", 0, 0), facts: t));
    }

    [Test]
    public void PlaceFact_TheHomeOrTheClaimLacksToday_IsNotATell()
    {
        Assert.IsFalse(IsTell(ClueCategory.Currency, new HomeCandidate("china", "ancient", 0, 0)));
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "china", "ancient", "1 Jan 5", IraqHome, World(), BookCategories));
    }

    [Test]
    public void PlaceFact_SharedWithAThirdPlaceToday_IsNotATell()
    {
        // Greece's " old babylonian " equals Iraq's Language under the scanner comparison.
        Assert.IsFalse(IsTell(ClueCategory.Language, IraqHome));
    }

    [Test]
    public void BirthDate_IsATell_OnlyWhenTheHomeRangeHoldsAnotherYear()
    {
        Assert.IsTrue(IsTell(ClueCategory.BirthDate, IraqHome));
        Assert.IsFalse(IsTell(ClueCategory.BirthDate, IraqHome, cover: "Unknown"));
        Assert.IsFalse(IsTell(ClueCategory.BirthDate, new HomeCandidate("greece", "ancient", 0, 0)));
        Assert.IsFalse(IsTell(ClueCategory.BirthDate, new HomeCandidate("italy", "ancient", -1450, -1450)));
    }

    [Test]
    public void Name_IsNeverATell()
    {
        Assert.IsFalse(IsTell(ClueCategory.Name, IraqHome));
    }

    [Test]
    public void MissingTableOrBooks_IsNotATell()
    {
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, null, BookCategories));
        Assert.IsFalse(Forgery.IsProvableTell(ClueCategory.Currency, "egypt", "ancient", "1 Jan 5", IraqHome, World(), null));
    }

    // -----------------------------
    // IsProvableCategory: the one rule questions and tells share
    // -----------------------------

    /// <summary>Every category, as a book set covering all of them.</summary>
    private static HashSet<ClueCategory> EveryBook() =>
        new HashSet<ClueCategory>((ClueCategory[])System.Enum.GetValues(typeof(ClueCategory)));

    [Test]
    public void IsProvableCategory_Name_IsNever_EvenWithEveryBook()
    {
        Assert.IsFalse(Forgery.IsProvableCategory(ClueCategory.Name, EveryBook()));
    }

    [Test]
    public void IsProvableCategory_BirthDate_IsAlways_TheRecordProvesIt()
    {
        Assert.IsTrue(Forgery.IsProvableCategory(ClueCategory.BirthDate, new HashSet<ClueCategory>()));
        Assert.IsTrue(Forgery.IsProvableCategory(ClueCategory.BirthDate, null));
    }

    [Test]
    public void IsProvableCategory_APlaceFact_ExactlyWhenABookCoversIt()
    {
        Assert.IsTrue(Forgery.IsProvableCategory(ClueCategory.Currency, BookCategories));
        Assert.IsFalse(Forgery.IsProvableCategory(ClueCategory.Geography, BookCategories));
        Assert.IsTrue(Forgery.IsProvableCategory(ClueCategory.Geography, EveryBook()));
    }

    [Test]
    public void IsProvableCategory_ANullBookSet_ProvesNoPlaceFact()
    {
        foreach (ClueCategory category in new[] { ClueCategory.Language, ClueCategory.Material, ClueCategory.Politics, ClueCategory.Technology, ClueCategory.Currency, ClueCategory.Geography, ClueCategory.Culture })
            Assert.IsFalse(Forgery.IsProvableCategory(category, null), category.ToString());
    }

    /// <summary>Redesign phase 7 (traveller types L2, §6.2): every record category is provable as the birth date is, whatever the books: the record proves it.</summary>
    [TestCase(ClueCategory.BirthDate)]
    [TestCase(ClueCategory.CitizenId)]
    [TestCase(ClueCategory.Destination)]
    [TestCase(ClueCategory.Incident)]
    [TestCase(ClueCategory.AccountStatus)]
    [TestCase(ClueCategory.TransponderId)]
    [TestCase(ClueCategory.TransponderClass)]
    [TestCase(ClueCategory.Debt)]
    [TestCase(ClueCategory.WaiverNo)]
    [TestCase(ClueCategory.Credit)]
    [TestCase(ClueCategory.Funds)]
    [TestCase(ClueCategory.PolicyNo)]
    public void IsProvableCategory_ARecordCategory_IsAlways_TheRecordProvesIt(ClueCategory category)
    {
        Assert.IsTrue(Forgery.IsRecordCategory(category));
        Assert.IsTrue(Forgery.IsProvableCategory(category, null));
        Assert.IsTrue(Forgery.IsProvableCategory(category, new HashSet<ClueCategory>()));
    }

    /// <summary>A directive-only category (read against the calendar) is never a tell, even with a book for it.</summary>
    [TestCase(ClueCategory.DepartureDate)]
    [TestCase(ClueCategory.Expiry)]
    [TestCase(ClueCategory.Signature)]
    public void IsProvableCategory_ADirectiveOnlyCategory_IsNever_EvenWithEveryBook(ClueCategory category)
    {
        Assert.IsTrue(Forgery.IsDirectiveOnly(category));
        Assert.IsFalse(Forgery.IsRecordCategory(category));
        Assert.IsFalse(Forgery.IsProvableCategory(category, EveryBook()));
    }

    /// <summary>Every category is exactly one of: a name (never), directive-only, a record category, or a place fact (a book decides), so a new category must be placed.</summary>
    [Test]
    public void EveryCategory_IsClassifiedOnce()
    {
        foreach (ClueCategory category in (ClueCategory[])System.Enum.GetValues(typeof(ClueCategory)))
        {
            int kinds = (category == ClueCategory.Name ? 1 : 0) + (Forgery.IsDirectiveOnly(category) ? 1 : 0) + (Forgery.IsRecordCategory(category) ? 1 : 0)
                        + (Forgery.IsProvableCategory(category, EveryBook()) && !Forgery.IsRecordCategory(category) ? 1 : 0);
            Assert.AreEqual(1, kinds, category.ToString());
        }
    }
}
