using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// RUN-SAFE's save safety (BUG-35a) on a scratch slot (SaveSystem.FolderOverride):
/// a save is written whole and read back; nothing in the slot is ever
/// deleted: an unreadable save and one from before the format are set aside
/// and reported, a run recovered from the backup keeps the damaged save
/// aside, a new run sets the old run's files aside; a failed write is
/// reported and the last good save stands, and a later good save clears the
/// report; the morning snapshot is kept apart from the slot. "NeedsEditor":
/// the saves are JsonUtility's, which needs the engine, so these run in
/// Unity's EditMode suite.
/// </summary>
public class SaveSystemTests
{
    private string _folder;

    /// <summary>A fresh scratch slot folder for each test.</summary>
    [SetUp]
    public void UseScratchSlot()
    {
        _folder = Path.Combine(Path.GetTempPath(), "nope_runsafe_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        SaveSystem.FolderOverride = _folder;
        typeof(SaveSystem).GetProperty(nameof(SaveSystem.Problem)).GetSetMethod(true).Invoke(null, new object[] { SaveProblem.None });
    }

    /// <summary>The player's slot again, the scratch folder removed.</summary>
    [TearDown]
    public void BackToThePlayersSlot()
    {
        SaveSystem.FolderOverride = null;
        try { Directory.Delete(_folder, true); } catch (IOException) { }
    }

    private static WorldState World(int day, int money) => new WorldState { day = day, money = money, runSeed = 12345 };

    private string[] FilesInSlot() => Directory.GetFiles(_folder).Select(Path.GetFileName).OrderBy(n => n).ToArray();

    [Test, Category("NeedsEditor")]
    public void ASave_IsReadBack_AndASecondKeepsTheFirstAsTheBackup()
    {
        Assert.IsTrue(SaveSystem.Save(World(1, 100)));
        Assert.IsTrue(SaveSystem.Save(World(2, 80)));
        WorldState loaded = SaveSystem.Load();
        Assert.AreEqual(2, loaded.day);
        Assert.AreEqual(80, loaded.money);
        Assert.IsTrue(File.Exists(SaveSystem.SavePath + ".bak"));
        Assert.IsFalse(File.Exists(SaveSystem.SavePath + ".tmp"), "the temp file was swapped in");
    }

    [Test, Category("NeedsEditor")]
    public void AnUnreadableSave_IsSetAside_KeptAndReported_NeverDeleted()
    {
        File.WriteAllText(SaveSystem.SavePath, "{ this is not a save");
        Assert.IsNull(SaveSystem.Load());
        Assert.IsFalse(File.Exists(SaveSystem.SavePath), "the slot is free for a new run");
        string aside = FilesInSlot().Single(n => n.Contains(SaveSafety.Unreadable));
        Assert.AreEqual("{ this is not a save", File.ReadAllText(Path.Combine(_folder, aside)), "kept as it was");
        Assert.AreEqual(SaveProblem.Unreadable, SaveSystem.Problem);
        Assert.IsFalse(SaveSystem.HasSave());
    }

    [Test, Category("NeedsEditor")]
    public void ASaveFromBeforeTheFormat_IsSetAside_NotWrittenOver()
    {
        File.WriteAllText(SaveSystem.SavePath, "{\"version\":2,\"world\":{\"day\":9}}");
        Assert.IsNull(SaveSystem.Load());
        Assert.IsTrue(FilesInSlot().Any(n => n.Contains(SaveSafety.TooOldReason)));
        Assert.IsTrue(SaveSystem.Save(World(1, 5)), "the next save takes the free slot");
        Assert.IsTrue(FilesInSlot().Any(n => n.Contains(SaveSafety.TooOldReason)), "the old save is still there");
    }

    [Test, Category("NeedsEditor")]
    public void ARunRecoveredFromTheBackup_KeepsTheDamagedSaveAside()
    {
        SaveSystem.Save(World(3, 30));
        SaveSystem.Save(World(4, 40));
        File.WriteAllText(SaveSystem.SavePath, "garbage");
        WorldState loaded = SaveSystem.Load();
        Assert.AreEqual(3, loaded.day, "the backup: the save before");
        Assert.IsTrue(FilesInSlot().Any(n => n.Contains(SaveSafety.Unreadable)), "the damaged save is kept");
        Assert.AreEqual(3, SaveSystem.Load().day, "the recovered run is the save now");
        Assert.AreEqual(SaveProblem.Unreadable, SaveSystem.Problem, "the worse problem is the one told");
    }

    [Test, Category("NeedsEditor")]
    public void ANewRun_SetsTheOldRunAside_AndDeletesNothing()
    {
        SaveSystem.Save(World(6, 60));
        SaveSystem.Save(World(7, 70));
        SaveSystem.SaveMorning(World(7, 70));
        int before = FilesInSlot().Length;

        SaveSystem.SetAsideForNewRun();

        Assert.AreEqual(before, FilesInSlot().Length, "every file kept");
        Assert.IsTrue(FilesInSlot().All(n => n.Contains(SaveSafety.Replaced)));
        Assert.IsNull(SaveSystem.Load(), "nothing of the old run is recovered into the new one");
        Assert.IsNull(SaveSystem.LoadMorning());
    }

    [Test, Category("NeedsEditor")]
    public void AFailedWrite_IsReported_TheLastGoodSaveStands_AndAGoodSaveClearsIt()
    {
        SaveSystem.Save(World(2, 20));
        Directory.CreateDirectory(SaveSystem.SavePath + ".tmp");   // the temp file cannot be written: a folder is in its place

        LogAssert.Expect(LogType.Error, new Regex("SaveSystem.Save failed"));
        Assert.IsFalse(SaveSystem.Save(World(3, 30)));
        Assert.AreEqual(SaveProblem.WriteFailed, SaveSystem.Problem);
        Assert.AreEqual(2, SaveSystem.Load().day, "the last good save stands");

        Directory.Delete(SaveSystem.SavePath + ".tmp");
        Assert.IsTrue(SaveSystem.Save(World(3, 30)));
        Assert.AreNotEqual(SaveProblem.WriteFailed, SaveSystem.Problem, "the write works again: the notice goes");
    }

    [Test, Category("NeedsEditor")]
    public void TheMorningSnapshot_IsKeptApartFromTheSlot()
    {
        SaveSystem.SaveMorning(World(5, 500));
        SaveSystem.Save(World(5, 420));
        Assert.AreEqual(500, SaveSystem.LoadMorning().money);
        Assert.AreEqual(420, SaveSystem.Load().money);
        CollectionAssert.Contains(SaveSystem.Files, Path.Combine(_folder, "nope_morning.json"), "tools that set the slot aside take the snapshot too");
    }
}
