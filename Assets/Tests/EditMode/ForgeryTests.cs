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
}
