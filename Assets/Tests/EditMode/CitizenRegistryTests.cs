using NUnit.Framework;

/// <summary>Lookup rules for the agency's citizen master record.</summary>
public class CitizenRegistryTests
{
    private static CitizenRegistry Registry()
    {
        var r = new CitizenRegistry();
        r.Add(new CitizenRecord { fullName = "Bjorn", birthDate = "3 May 1131", origin = "Norvik — Medieval Era", note = "No remarks on file." });
        r.Add(new CitizenRecord { fullName = "Zara-7", birthDate = "14 Sep 2401", origin = "Solaris — The Future", note = "No remarks on file." });
        return r;
    }

    [Test]
    public void Find_ExactName_ReturnsRecord()
    {
        Assert.AreEqual("3 May 1131", Registry().Find("Bjorn").birthDate);
    }

    [Test]
    public void Find_IsCaseAndWhitespaceInsensitive()
    {
        Assert.NotNull(Registry().Find("  bJORN "));
    }

    [Test]
    public void Find_PartialName_FallsBackToContains()
    {
        Assert.AreEqual("Zara-7", Registry().Find("zara").fullName);
    }

    [Test]
    public void Find_UnknownName_ReturnsNull()
    {
        Assert.IsNull(Registry().Find("Cassia"));
    }

    [Test]
    public void Find_NullOrEmpty_ReturnsNull()
    {
        Assert.IsNull(Registry().Find(null));
        Assert.IsNull(Registry().Find("   "));
    }

    [Test]
    public void Add_IgnoresNullAndUnnamed()
    {
        var r = new CitizenRegistry();
        r.Add(null);
        r.Add(new CitizenRecord { fullName = "  " });
        Assert.AreEqual(0, r.Count);
    }
}
