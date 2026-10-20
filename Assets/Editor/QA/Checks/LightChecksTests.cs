using NUnit.Framework;

/// <summary>
/// LIGHT's demo-gate checks (LightChecks) are green on the shipped assets. They
/// load assets through the AssetDatabase, so they run only inside Unity
/// ("NeedsEditor").
/// </summary>
public class LightChecksTests
{
    [Test, Category("NeedsEditor")]
    public void TheSsaoIsCut() => CollectionAssert.IsEmpty(LightChecks.SsaoIsCut());

    [Test, Category("NeedsEditor")]
    public void TheDeskStaysReadableAtNight() => CollectionAssert.IsEmpty(LightChecks.DeskStaysReadableAtNight());

    [Test, Category("NeedsEditor")]
    public void TheLampSwitchesOnAtTheDip() => CollectionAssert.IsEmpty(LightChecks.LampSwitchesOnAtTheDip());

    [Test, Category("NeedsEditor")]
    public void TheFinishHasBloomGradeAndVignette() => CollectionAssert.IsEmpty(LightChecks.FinishHasBloomGradeAndVignette());

    [Test, Category("NeedsEditor")]
    public void EveryDiffuserCanLight() => CollectionAssert.IsEmpty(LightChecks.EveryDiffuserCanLight());

    [Test, Category("NeedsEditor")]
    public void TheFourStateInstallerIsGone() => CollectionAssert.IsEmpty(LightChecks.NoFourStateInstaller());

    [Test, Category("NeedsEditor")]
    public void AllLightChecksAreGreen() => CollectionAssert.IsEmpty(LightChecks.Failures());
}
