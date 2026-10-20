using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// JSON save/load for WorldState. One save slot for Alpha.
/// File lives at Folder/nope_save.json (SaveLocation: persistentDataPath in a
/// player, the project's Library/EditorSaves in the editor, so each worktree's
/// editor keeps its own save). A save is
/// written whole to a temp file first, flushed to the disk, then swapped in
/// atomically (File.Replace, the old save kept as a backup); a load that
/// finds the save missing or unreadable recovers the run from the temp file
/// or the backup (SaveFiles.Pick; audit R3-003: a crash between deleting the
/// old save and moving the new one in used to lose the run).
/// Nothing in the slot is ever deleted (RUN-SAFE, BUG-35a): a save this build
/// cannot continue (unreadable, or from before its format) and the run a new
/// run replaces are set aside beside the slot under names of their own
/// (SaveSafety); a failed write or a file set aside is reported
/// (<see cref="Problem"/>, shown by SaveNotice). The day's morning is kept in
/// its own file (<see cref="SaveMorning"/>) for the cheat menu's restart.
/// </summary>
public static class SaveSystem
{
    /// <summary>Bump when WorldState shape changes incompatibly.</summary>
    private const int SaveVersion = 3;

    /// <summary>
    /// Oldest save version that can still be continued. Version 2 replaced the
    /// made-up world with real places (all place and era ids changed); version
    /// 3 keeps stability as whole hundredths on its compounding scale
    /// (WorldState.stabilityHundredths, redesign phase 23 part 1b), so older
    /// saves are set aside and the Title offers only New Run.
    /// </summary>
    private const int MinCompatibleVersion = 3;

    /// <summary>Save file name (single slot).</summary>
    private const string FileName = "nope_save.json";

    /// <summary>The day's morning snapshot's file name (the morning save, kept apart from the slot the shift's end overwrites).</summary>
    private const string MorningFileName = "nope_morning.json";

    /// <summary>Added to a file's path for the temp file it is written to first.</summary>
    private const string TempSuffix = ".tmp";

    /// <summary>Added to the save's path for the backup the replace keeps.</summary>
    private const string BackupSuffix = ".bak";

    /// <summary>
    /// The folder the slot lives in instead of <see cref="Folder"/>'s
    /// default when set (null: the default): the save tests point it at a
    /// scratch folder, so they never touch the player's or the editor's save.
    /// </summary>
    public static string FolderOverride { get; set; }

    /// <summary>The save slot's folder (SaveLocation): Application.persistentDataPath in a player; in the editor the project's Library/EditorSaves, so parallel plays in several worktrees' editors never overwrite one another's save (FolderOverride when set). Tools that set the slot aside keep their copies here too.</summary>
    public static string Folder => FolderOverride ?? SaveLocation.Folder(Application.isEditor, Application.dataPath, Application.persistentDataPath);

    /// <summary>Full save file path.</summary>
    public static string SavePath => Path.Combine(Folder, FileName);

    /// <summary>The file a save is written to before it replaces the save.</summary>
    private static string TempPath => SavePath + TempSuffix;

    /// <summary>The save before the last one, kept by the replace.</summary>
    private static string BackupPath => SavePath + BackupSuffix;

    /// <summary>The day's morning snapshot.</summary>
    private static string MorningPath => Path.Combine(Folder, MorningFileName);

    /// <summary>Every file of the save slot: the save, the temp file, the backup and the morning snapshot (a new run sets them aside; a tool standing in for the player backs them up).</summary>
    public static IReadOnlyList<string> Files => new[] { SavePath, TempPath, BackupPath, MorningPath };

    /// <summary>The worst save problem met this session (SaveSafety.Worse; None when all is well): SaveNotice tells the player.</summary>
    public static SaveProblem Problem { get; private set; }

    /// <summary>What the problem was: the file set aside, or the error of a failed write.</summary>
    public static string ProblemDetail { get; private set; } = string.Empty;

    /// <summary>Raised when <see cref="Problem"/> changes (SaveNotice shows it).</summary>
    public static event Action ProblemChanged;

    /// <summary>
    /// Wrapper so we can version the payload and migrate later.
    /// </summary>
    [Serializable]
    private sealed class SaveFile
    {
        public int version;
        public WorldState world;
    }

    /// <summary>A save file read for its version only: the world is not built, so checking every file of the slot stays cheap (audit R3-022: a boot parsed the whole world about six times).</summary>
    [Serializable]
    private sealed class SaveVersionOnly
    {
        /// <summary>The save's version (0 when the file has none).</summary>
        public int version;
    }

    /// <summary>Returns true if a file of the slot holds a save this build can continue (older versions are ignored with a warning). HOME's NEW RUN confirm asks it before a new run replaces the saved one (BUG-35b).</summary>
    public static bool HasSave()
    {
        SaveSource source = Source();
        if (source == SaveSource.None)
        {
            Debug.Log($"[SaveSystem] HasSave: false (no save this build can continue at '{SavePath}').");
            return false;
        }

        Debug.Log($"[SaveSystem] HasSave: true ('{PathOf(source)}').");
        return true;
    }

    /// <summary>The file a load reads now (SaveFiles.Pick over the slot's three files).</summary>
    private static SaveSource Source() => SaveFiles.Pick(Continuable(SavePath), Continuable(TempPath), Continuable(BackupPath));

    /// <summary>The path of a slot file.</summary>
    private static string PathOf(SaveSource source) =>
        source == SaveSource.Temp ? TempPath : source == SaveSource.Backup ? BackupPath : SavePath;

    /// <summary>True when the file exists and holds a save this build can continue; an older version or an unreadable file is ignored with a warning.</summary>
    private static bool Continuable(string path)
    {
        if (!File.Exists(path))
            return false;

        int version = ReadVersion(path);
        if (version >= MinCompatibleVersion)
            return true;

        Debug.LogWarning(version >= 0
            ? $"[SaveSystem] Ignoring the save at '{path}': version {version} predates this build's save format (needs {MinCompatibleVersion}+). Start a new run."
            : $"[SaveSystem] Ignoring the save file at '{path}': it cannot be read.");
        return false;
    }

    /// <summary>The file's save version, or -1 if it is missing or unreadable (the world in it is not built).</summary>
    private static int ReadVersion(string path)
    {
        try
        {
            SaveVersionOnly file = JsonUtility.FromJson<SaveVersionOnly>(File.ReadAllText(path));
            return file != null ? file.version : -1;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    /// <summary>
    /// Writes the world state to disk: whole to the temp file, flushed, then
    /// swapped in for the save (Commit). Returns true on success; a failure
    /// (a full disk, a read-only folder) is logged and reported
    /// (<see cref="Problem"/>: the player is told, the last good save stands),
    /// and a later good save clears it.
    /// </summary>
    public static bool Save(WorldState world)
    {
        if (world == null)
        {
            Debug.LogError("SaveSystem.Save called with null WorldState.");
            return false;
        }

        try
        {
            Write(TempPath, world);
            Commit();
            Settle(SaveSafety.AfterGoodSave(Problem));
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.Save failed: {e.Message}");
            Report(SaveProblem.WriteFailed, e.Message);
            return false;
        }
    }

    /// <summary>
    /// Keeps <paramref name="world"/> as the day's morning (RunManager's morning
    /// save, every morning): written whole to its temp file, flushed, then moved
    /// in. True on success; a failure is reported as a failed write.
    /// </summary>
    public static bool SaveMorning(WorldState world)
    {
        if (world == null)
            return false;

        try
        {
            string temp = MorningPath + TempSuffix;
            Write(temp, world);
            if (File.Exists(MorningPath))
                File.Replace(temp, MorningPath, null);
            else
                File.Move(temp, MorningPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.SaveMorning failed: {e.Message}");
            Report(SaveProblem.WriteFailed, e.Message);
            return false;
        }
    }

    /// <summary>The morning snapshot on disk, or null when there is none this build can read (the next morning writes a new one).</summary>
    public static WorldState LoadMorning()
    {
        if (!File.Exists(MorningPath) || ReadVersion(MorningPath) < MinCompatibleVersion)
            return null;
        try
        {
            SaveFile file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(MorningPath));
            return file != null ? file.world : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] The morning snapshot at '{MorningPath}' cannot be read ({e.Message}); the next morning writes a new one.");
            return null;
        }
    }

    /// <summary>Writes <paramref name="world"/> (with the save version) to <paramref name="path"/>, flushed through to the disk before it returns (BUG-35a: a save the OS still buffers is not a save).</summary>
    private static void Write(string path, WorldState world)
    {
        var file = new SaveFile { version = SaveVersion, world = world };
        byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(file, prettyPrint: true));

        Directory.CreateDirectory(Folder);
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
    }

    /// <summary>
    /// Puts the written temp file in the save's place. With a save there,
    /// File.Replace swaps it in atomically and keeps the old save as the
    /// backup; where Replace is unsupported or fails, the old save is copied
    /// to the backup and removed, then the temp file moved in (a crash in
    /// between leaves the new save whole in the temp file, which Load reads).
    /// </summary>
    private static void Commit()
    {
        if (!File.Exists(SavePath))
        {
            File.Move(TempPath, SavePath);
            return;
        }

        try
        {
            File.Replace(TempPath, SavePath, BackupPath);
        }
        catch (Exception e) when (e is IOException || e is PlatformNotSupportedException)
        {
            Debug.LogWarning($"[SaveSystem] File.Replace failed ({e.Message}); keeping the old save as the backup and moving the new one in.");
            if (File.Exists(SavePath))
            {
                File.Copy(SavePath, BackupPath, true);
                File.Delete(SavePath);
            }
            File.Move(TempPath, SavePath);
        }
    }

    /// <summary>
    /// Loads the world state from the slot (the save, else the file a crash
    /// left: SaveFiles.Pick). A save this build cannot continue (unreadable,
    /// or from before its format) is set aside first, kept and reported, never
    /// written over; a run recovered from the temp file or the backup is
    /// copied back as the save and reported. Returns null if no file holds a
    /// save this build can continue.
    /// </summary>
    public static WorldState Load()
    {
        SaveProblem saveProblem = File.Exists(SavePath) ? SaveSafety.Check(ReadVersion(SavePath), MinCompatibleVersion) : SaveProblem.None;
        if (saveProblem != SaveProblem.None)
            SetAside(SavePath, SaveSafety.ReasonOf(saveProblem), saveProblem);

        SaveSource source = Source();
        if (source == SaveSource.None)
            return null;

        try
        {
            string path = PathOf(source);
            string json = File.ReadAllText(path);
            SaveFile file = JsonUtility.FromJson<SaveFile>(json);

            if (file == null || file.world == null)
            {
                Debug.LogError("SaveSystem.Load: save file was empty or malformed.");
                return null;
            }

            if (source != SaveSource.Save)
            {
                Debug.LogWarning($"[SaveSystem] The save at '{SavePath}' is missing or unreadable; recovered the run from '{path}'.");
                File.Copy(path, SavePath, true);
                Report(SaveProblem.Recovered, path);
            }

            if (file.version != SaveVersion)
                Debug.LogWarning($"SaveSystem.Load: save version {file.version} != current {SaveVersion}. Loading with defaults for new fields.");

            return file.world;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.Load failed: {e.Message}");
            return null;
        }
    }

    /// <summary>A new run replaces the saved one (RunManager.NewRun, after the Title's confirm): every file of the slot is set aside as "replaced", kept beside it, never deleted, so no older run is recovered into the new one.</summary>
    public static void SetAsideForNewRun()
    {
        int moved = 0;
        foreach (string path in Files)
            if (File.Exists(path) && SetAside(path, SaveSafety.Replaced, SaveProblem.None))
                moved++;
        Debug.Log($"[SaveSystem] New run: {moved} file(s) of the old run set aside in '{Folder}'.");
    }

    /// <summary>Moves <paramref name="path"/> aside under SaveSafety.AsideName with <paramref name="reason"/> (a free name: a second set-aside in the same second gets its own); a <paramref name="problem"/> is reported. True when moved.</summary>
    private static bool SetAside(string path, string reason, SaveProblem problem)
    {
        try
        {
            string folder = Path.GetDirectoryName(path) ?? Folder;
            DateTime now = DateTime.Now;
            string aside = Path.Combine(folder, SaveSafety.AsideName(Path.GetFileName(path), reason, now));
            for (int copy = 2; File.Exists(aside); copy++)
                aside = Path.Combine(folder, SaveSafety.AsideName(Path.GetFileName(path), reason, now, copy));
            File.Move(path, aside);
            Debug.LogWarning($"[SaveSystem] Set '{path}' aside as '{aside}' ({reason}); it is kept, never deleted.");
            if (problem != SaveProblem.None)
                Report(problem, aside);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Could not set '{path}' aside ({e.Message}); it is left where it is.");
            if (problem != SaveProblem.None)
                Report(problem, path);
            return false;
        }
    }

    /// <summary>Records <paramref name="problem"/> (the worse one stands; SaveSafety.Worse) with its detail and tells SaveNotice.</summary>
    private static void Report(SaveProblem problem, string detail)
    {
        SaveProblem worse = SaveSafety.Worse(Problem, problem);
        if (worse == problem)
            ProblemDetail = detail ?? string.Empty;
        Problem = worse;
        ProblemChanged?.Invoke();
    }

    /// <summary>Sets the problem to <paramref name="problem"/> (a good save clearing a failed write), telling SaveNotice when it changed.</summary>
    private static void Settle(SaveProblem problem)
    {
        if (problem == Problem)
            return;
        Problem = problem;
        if (problem == SaveProblem.None)
            ProblemDetail = string.Empty;
        ProblemChanged?.Invoke();
    }
}
