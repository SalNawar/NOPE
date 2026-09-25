/// <summary>Which of the save slot's files a load reads.</summary>
public enum SaveSource
{
    /// <summary>No file holds a save this build can continue.</summary>
    None,

    /// <summary>The save itself.</summary>
    Save,

    /// <summary>The temp file a save is written to before it replaces the save.</summary>
    Temp,

    /// <summary>The backup of the save before, kept by the replace.</summary>
    Backup
}

/// <summary>
/// The save slot's recovery rule (audit R3-003: a crash between deleting the
/// old save and moving the new one in lost the run, because nothing read the
/// temp file). Pure, so every case is tested headless; SaveSystem applies it.
/// </summary>
public static class SaveFiles
{
    /// <summary>
    /// The file a load recovers the run from, given which files hold a save
    /// this build can continue: the save itself; else the temp file (a crash
    /// after it was written whole but before it replaced the save: the newest
    /// state); else the backup the last replace kept (the save before); else
    /// none. A temp file never beats a readable save: it was never committed.
    /// </summary>
    public static SaveSource Pick(bool save, bool temp, bool backup) =>
        save ? SaveSource.Save : temp ? SaveSource.Temp : backup ? SaveSource.Backup : SaveSource.None;
}
