using NUnit.Framework;

/// <summary>
/// What breaking the law means (the desk machine spec §2; Saleh: "detain only
/// if the traveller breaks the law"): a forgery (a forged seal, a doctored
/// value), a false identity (someone else's photo) and contraband (smuggling)
/// are crimes; a lie about one's home, a costume error and a directive fault
/// (missing papers, a closed destination) are not.
/// </summary>
public class LawTests
{
    [TestCase(LieKind.ForgedSeal, true, Description = "a forged seal: forgery")]
    [TestCase(LieKind.DoctoredIdentity, true, Description = "a doctored value: forgery")]
    [TestCase(LieKind.PoorPosingAsRich, true, Description = "a forged visa class: forgery")]
    [TestCase(LieKind.DebtorPosingAsTourist, true, Description = "a doctored status: forgery")]
    [TestCase(LieKind.ForgedContract, true, Description = "forgery")]
    [TestCase(LieKind.FakeWaiver, true, Description = "forgery")]
    [TestCase(LieKind.ForgedProof, true, Description = "forgery")]
    [TestCase(LieKind.SwappedPhoto, true, Description = "someone else's photo: a false identity")]
    [TestCase(LieKind.Smuggling, true, Description = "contraband")]
    [TestCase(LieKind.FalseOrigin, false, Description = "a lie about one's home: a deviation, not a crime")]
    [TestCase(LieKind.FakeDisplaced, false, Description = "a lie: a deviation, not a crime")]
    public void ALie_BreaksTheLaw_OnlyAsForgeryAFalseIdentityOrContraband(LieKind lie, bool breaks)
    {
        Assert.AreEqual(breaks, Law.Breaks(lie));
    }

    /// <summary>A costume error, a directive fault (missing papers, a closed destination, ...) and an honest traveller carry no lie (K5): no law broken.</summary>
    [Test]
    public void NoLie_BreaksNoLaw_CostumeMissingClosedOrHonest()
    {
        Assert.IsFalse(Law.Breaks(null));
    }

    [Test]
    public void EveryLie_IsJudged_NoneIsLeftOut()
    {
        int crimes = 0;
        foreach (LieKind lie in System.Enum.GetValues(typeof(LieKind)))
            if (Law.Breaks(lie))
                crimes++;
        Assert.AreEqual(System.Enum.GetValues(typeof(LieKind)).Length - 2, crimes, "every lie but the two about one's home (a false origin, the fake displaced) is a crime");
    }
}
