using NUnit.Framework;

/// <summary>
/// Which of the save slot's files a load recovers the run from (audit R3-003:
/// a crash between deleting the old save and moving the new one in lost the
/// run, because the temp file was never read): the save itself when it holds
/// a save this build can continue, else the temp file, else the backup.
/// </summary>
public class SaveFilesTests
{
    [TestCase(true, true, true, SaveSource.Save)]
    [TestCase(true, false, false, SaveSource.Save)]
    [TestCase(true, true, false, SaveSource.Save, Description = "a stale temp file never beats the save: it was never committed")]
    [TestCase(true, false, true, SaveSource.Save)]
    [TestCase(false, true, true, SaveSource.Temp, Description = "the crash between the old save's removal and the new one's move: the new one is whole in the temp file")]
    [TestCase(false, true, false, SaveSource.Temp, Description = "the first save's crash before its move")]
    [TestCase(false, false, true, SaveSource.Backup, Description = "the save and the temp file unreadable: the save before")]
    [TestCase(false, false, false, SaveSource.None)]
    public void Pick_TheSave_ElseTheTempFile_ElseTheBackup(bool save, bool temp, bool backup, SaveSource expected)
    {
        Assert.AreEqual(expected, SaveFiles.Pick(save, temp, backup));
    }
}
