using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Which fields may be forged: only those the player can disprove. Today holds
/// two Ancient places; books exist for Currency and Language only.
/// </summary>
public class ForgeryTests
{
    private static readonly HashSet<ClueCategory> Books = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language };

    private static FactTable Today()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Language, "Middle Egyptian");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Geography, "Thebes");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Currency, "Silver shekel");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Geography, "Babylon");
        return t;
    }

    private static bool Provable(ClueCategory category, string birthDate = "3 Jun 1450 BCE") =>
        Forgery.IsProvable(category, "egypt", "ancient", Today(), Books, birthDate);

    [Test]
    public void PlaceFact_WithABook_TheTruthAndAnotherValue_IsProvable()
    {
        Assert.IsTrue(Provable(ClueCategory.Currency));
    }

    [Test]
    public void PlaceFact_WithoutABook_IsNotProvable()
    {
        // Geography has a truth and another value, but no book to prove it with.
        Assert.IsFalse(Provable(ClueCategory.Geography));
    }

    [Test]
    public void PlaceFact_WithNoOtherValueToday_IsNotProvable()
    {
        // Only Egypt lists a language today.
        Assert.IsFalse(Provable(ClueCategory.Language));
    }

    [Test]
    public void PlaceFact_TheClaimedPlaceLacks_IsNotProvable()
    {
        Assert.IsFalse(Forgery.IsProvable(ClueCategory.Currency, "china", "ancient", Today(), Books, "1 Jan 5"));
        Assert.IsFalse(Forgery.IsProvable(ClueCategory.Currency, null, null, Today(), Books, "1 Jan 5"));
    }

    [Test]
    public void BirthDate_IsProvable_OnlyWhenReadable()
    {
        Assert.IsTrue(Provable(ClueCategory.BirthDate));
        Assert.IsFalse(Provable(ClueCategory.BirthDate, "Unknown"));
    }

    [Test]
    public void Name_IsNeverForged()
    {
        Assert.IsFalse(Provable(ClueCategory.Name));
    }

    [Test]
    public void MissingTableOrBooks_IsNotProvable()
    {
        Assert.IsFalse(Forgery.IsProvable(ClueCategory.Currency, "egypt", "ancient", null, Books, "1 Jan 5"));
        Assert.IsFalse(Forgery.IsProvable(ClueCategory.Currency, "egypt", "ancient", Today(), null, "1 Jan 5"));
    }

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
}
