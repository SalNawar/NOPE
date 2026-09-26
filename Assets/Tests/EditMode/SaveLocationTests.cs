using System.IO;
using NUnit.Framework;

/// <summary>
/// Where the save slot lives: in the editor, the project's Library/EditorSaves,
/// so each worktree's editor keeps its own save (six editors used to share the
/// one persistent data folder and parallel plays overwrote one another's
/// save); in a player, the persistent data folder, unchanged.
/// </summary>
public class SaveLocationTests
{
    private const string Persistent = "/users/player/AppData/LocalLow/DefaultCompany/NOPE";

    [Test]
    public void Editor_KeepsTheSaveInTheProjectsLibrary()
    {
        Assert.AreEqual(Normal("/projects/NOPE-gpt/Library/EditorSaves"),
                        Normal(SaveLocation.Folder(true, "/projects/NOPE-gpt/Assets", Persistent)));
    }

    [TestCase("/projects/NOPE-gpt/Assets/")]
    [TestCase(@"\projects\NOPE-gpt\Assets")]
    public void Editor_ReadsTheProjectRootFromAnyFormOfTheAssetsPath(string assetsPath)
    {
        Assert.AreEqual(Normal("/projects/NOPE-gpt/Library/EditorSaves"), Normal(SaveLocation.Folder(true, assetsPath, Persistent)));
    }

    [Test]
    public void Editor_TwoWorktreesNeverShareASave()
    {
        string a = Normal(SaveLocation.Folder(true, "/projects/NOPE-gpt/Assets", Persistent));
        string b = Normal(SaveLocation.Folder(true, "/projects/NOPE-art/Assets", Persistent));
        Assert.AreNotEqual(a, b);
        Assert.AreNotEqual(Normal(Persistent), a);
    }

    [Test]
    public void Player_KeepsTheSaveInThePersistentDataFolder()
    {
        Assert.AreEqual(Persistent, SaveLocation.Folder(false, "/game/NOPE_Data", Persistent));
    }

    /// <summary>A path in one form (full, one separator, no trailing separator), so the tests hold on any OS.</summary>
    private static string Normal(string path) =>
        Path.GetFullPath(path.Replace('\\', '/')).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
