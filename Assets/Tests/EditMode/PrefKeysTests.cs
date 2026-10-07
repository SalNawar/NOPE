using NUnit.Framework;

/// <summary>
/// The names the per-player preferences are stored under: in the editor each
/// worktree's project has its own (the editors share one PlayerPrefs store, and
/// a probe's "Always English" in one editor turned another editor's audit play
/// to English labels halfway, run 7); in a player, the name unchanged, so a
/// player's saved choices still load.
/// </summary>
public class PrefKeysTests
{
    private const string Key = "TimeDesk.UiLanguage";

    [Test]
    public void Player_KeepsTheNameAsItIs()
    {
        Assert.AreEqual(Key, PrefKeys.Scoped(Key, false, "C:/game/NOPE_Data"));
    }

    [Test]
    public void Editor_AddsTheProjectsRoot()
    {
        Assert.AreEqual(Key + "@E:/unity/NOPE-art", PrefKeys.Scoped(Key, true, "E:/unity/NOPE-art/Assets"));
    }

    [TestCase("E:/unity/NOPE-art/Assets/")]
    [TestCase(@"E:\unity\NOPE-art\Assets")]
    public void Editor_ReadsTheProjectRootFromAnyFormOfTheAssetsPath(string assetsPath)
    {
        Assert.AreEqual(Key + "@E:/unity/NOPE-art", PrefKeys.Scoped(Key, true, assetsPath));
    }

    [Test]
    public void Editor_TwoWorktreesNeverShareAPreference()
    {
        string art = PrefKeys.Scoped(Key, true, "E:/unity/NOPE-art/Assets");
        string gpt = PrefKeys.Scoped(Key, true, "E:/unity/NOPE-gpt/Assets");
        Assert.AreNotEqual(art, gpt);
        Assert.AreNotEqual(Key, art);
        Assert.AreEqual(art, PrefKeys.Scoped(Key, true, "E:/unity/NOPE-art/Assets"), "the same project always reads the same name");
    }

    [Test]
    public void Editor_WithNoAssetsPath_KeepsTheName()
    {
        Assert.AreEqual(Key, PrefKeys.Scoped(Key, true, null));
    }
}
