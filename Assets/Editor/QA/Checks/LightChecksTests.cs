using NUnit.Framework;

/// <summary>
/// LIGHT's gate checks (LightChecks) are green on the shipped assets and are
/// registered the way QA-RIG's DemoGate finds them. They load assets through
/// the AssetDatabase, so they run only inside Unity ("NeedsEditor").
/// </summary>
public class LightChecksTests
{
    [Test, Category("NeedsEditor")]
    public void TheSsaoIsCut() => AssertPass(LightChecks.SsaoIsCut());

    [Test, Category("NeedsEditor")]
    public void TheDeskStaysReadableAtNight() => AssertPass(LightChecks.DeskStaysReadableAtNight());

    [Test, Category("NeedsEditor")]
    public void TheLampSwitchesOnAtTheDip() => AssertPass(LightChecks.LampSwitchesOnAtTheDip());

    [Test, Category("NeedsEditor")]
    public void TheFinishHasBloomGradeAndVignette() => AssertPass(LightChecks.FinishHasBloomGradeAndVignette());

    [Test, Category("NeedsEditor")]
    public void EveryDiffuserCanLight() => AssertPass(LightChecks.EveryDiffuserCanLight());

    [Test, Category("NeedsEditor")]
    public void TheHallsNightIsShared() => AssertPass(LightChecks.HallNightIsShared());

    [Test, Category("NeedsEditor")]
    public void TheFourStateInstallerIsGone() => AssertPass(LightChecks.NoFourStateInstaller());

    [Test, Category("NeedsEditor")]
    public void DemoGateFindsEveryLightCheck_WithNoRegistrationProblem()
    {
        var light = GateChecks.All().FindAll(c => c.Info.Owner == "LIGHT");
        Assert.AreEqual(7, light.Count, "the seven light.* checks");
        foreach (RegisteredCheck c in light)
            StringAssert.StartsWith("light.", c.Info.Id);
        CollectionAssert.IsEmpty(GateChecks.Problems(light));
    }

    private static void AssertPass(GateResult result) => Assert.IsTrue(result.Pass, result.ToString());
}
