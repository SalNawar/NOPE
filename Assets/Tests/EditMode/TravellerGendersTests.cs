using NUnit.Framework;

/// <summary>
/// Gender from the claimed place's name lists. Male: Marcus, Gaius, Sasha.
/// Female: Lucia, Anna Maria, Sasha (Sasha is on both).
/// </summary>
public class TravellerGendersTests
{
    private static readonly string[] Male = { "Marcus", "Gaius", "Sasha" };
    private static readonly string[] Female = { "Lucia", "Anna Maria", "Sasha" };

    private static TravellerGender Of(string name) => TravellerGenders.FromNameLists(name, Male, Female);

    [Test]
    public void NameOnTheMaleList_IsMale()
    {
        Assert.AreEqual(TravellerGender.Male, Of("Gaius"));
    }

    [Test]
    public void NameOnTheFemaleList_IsFemale()
    {
        Assert.AreEqual(TravellerGender.Female, Of("Lucia"));
    }

    [Test]
    public void SuffixedName_CountsAsItsPoolName()
    {
        Assert.AreEqual(TravellerGender.Male, Of("Marcus II"));
        Assert.AreEqual(TravellerGender.Female, Of("Lucia XIV"));
    }

    [Test]
    public void MultiWordName_IsMatchedWhole()
    {
        Assert.AreEqual(TravellerGender.Female, Of("Anna Maria"));
    }

    [Test]
    public void Comparison_IgnoresCaseAndSurroundingSpaces()
    {
        Assert.AreEqual(TravellerGender.Male, Of(" marcus "));
    }

    [Test]
    public void NameOnBothLists_IsUnknown()
    {
        Assert.AreEqual(TravellerGender.Unknown, Of("Sasha"));
    }

    [Test]
    public void SubjectFallback_IsUnknown()
    {
        Assert.AreEqual(TravellerGender.Unknown, Of("Subject #3"));
    }

    [Test]
    public void NullOrBlankName_IsUnknown()
    {
        Assert.AreEqual(TravellerGender.Unknown, Of(null));
        Assert.AreEqual(TravellerGender.Unknown, Of("  "));
    }

    [Test]
    public void ANullListCountsAsEmpty()
    {
        Assert.AreEqual(TravellerGender.Unknown, TravellerGenders.FromNameLists("Marcus", null, Female));
        Assert.AreEqual(TravellerGender.Female, TravellerGenders.FromNameLists("Lucia", null, Female));
    }
}
