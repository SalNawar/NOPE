using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The player build's scene list (audit R3-001 / R6-025: a player build booted
/// the legacy Test_DayLoop prototype, which sat at index 0): the title boots
/// first, then the art office, its gameplay layer and Home, each enabled;
/// every other scene stays listed, disabled.
/// </summary>
public class BuildScenesTests
{
    private const string Title = "Assets/Scenes/TitleScene.unity";
    private const string Office = "Assets/Scenes/OfficeScene.unity";
    private const string Gameplay = "Assets/Scenes/OfficeGameplay.unity";
    private const string Home = "Assets/Scenes/HomeScene.unity";
    private const string Prototype = "Assets/Scenes/Test_DayLoop.unity";

    private static List<BuildScene> Order(params BuildScene[] current) => BuildScenes.Order(current, Title, Office, Gameplay, Home);

    private static string Describe(IEnumerable<BuildScene> scenes) =>
        string.Join(", ", scenes.Select(s => s.Path + (s.Enabled ? "" : "(off)")));

    [Test]
    public void TheBaselineList_BootsTheTitle_ThenTheOffice_ItsGameplayLayer_AndHome_WithThePrototypeOff()
    {
        List<BuildScene> order = Order(
            new BuildScene(Prototype, true),
            new BuildScene(Office, true),
            new BuildScene(Gameplay, true),
            new BuildScene(Home, true),
            new BuildScene(Title, true));

        Assert.AreEqual($"{Title}, {Office}, {Gameplay}, {Home}, {Prototype}(off)", Describe(order));
    }

    [Test]
    public void EveryOtherScene_StaysListed_InItsOrder_ButDisabled()
    {
        List<BuildScene> order = Order(
            new BuildScene("Assets/Scenes/A.unity", true),
            new BuildScene(Home, true),
            new BuildScene("Assets/Scenes/B.unity", false),
            new BuildScene(Title, true));

        Assert.AreEqual($"{Title}, {Office}, {Gameplay}, {Home}, Assets/Scenes/A.unity(off), Assets/Scenes/B.unity(off)", Describe(order));
    }

    [Test]
    public void AShippedScene_IsEnabled_AndAddedWhenMissing()
    {
        Assert.AreEqual($"{Title}, {Office}, {Gameplay}, {Home}",
            Describe(Order(new BuildScene(Title, false), new BuildScene(Gameplay, false))));
        Assert.AreEqual($"{Title}, {Office}, {Gameplay}, {Home}", Describe(BuildScenes.Order(null, Title, Office, Gameplay, Home)));
    }

    [Test]
    public void RepeatsAndBlankPaths_AreDropped()
    {
        List<BuildScene> order = Order(
            new BuildScene(Prototype, true),
            new BuildScene(" ", true),
            new BuildScene(Title, true),
            new BuildScene(Prototype, false),
            new BuildScene(Title, false));

        Assert.AreEqual($"{Title}, {Office}, {Gameplay}, {Home}, {Prototype}(off)", Describe(order));
    }

    [Test]
    public void TheOrder_IsStable_ApplyingItAgainChangesNothing()
    {
        List<BuildScene> once = Order(new BuildScene(Prototype, true), new BuildScene(Home, true), new BuildScene(Title, true));
        List<BuildScene> twice = BuildScenes.Order(once, Title, Office, Gameplay, Home);

        Assert.AreEqual(Describe(once), Describe(twice));
    }

    [Test]
    public void OnlyTheFourShippedScenes_AreEverEnabled_AndTheTitleIsFirst()
    {
        List<BuildScene> order = Order(
            new BuildScene("Assets/Scenes/Sandbox.unity", true),
            new BuildScene(Prototype, true),
            new BuildScene(Office, false));

        Assert.AreEqual(Title, order[0].Path);
        CollectionAssert.AreEqual(new[] { Title, Office, Gameplay, Home }, order.Where(s => s.Enabled).Select(s => s.Path).ToArray());
    }
}
