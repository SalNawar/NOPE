using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// JSON save/load for WorldState. One save slot for Alpha.
/// File lives at Application.persistentDataPath/nope_save.json. A save is
/// written whole to a temp file first, then swapped in atomically
/// (File.Replace, the old save kept as a backup); a load that finds the save
/// missing or unreadable recovers the run from the temp file or the backup
/// (SaveFiles.Pick; audit R3-003: a crash between deleting the old save and
/// moving the new one in used to lose the run).
/// </summary>
public static class SaveSystem
{
    /// <summary>Bump when WorldState shape changes incompatibly.</summary>
    private const int SaveVersion = 2;

    /// <summary>
    /// Oldest save version that can still be continued. Version 2 replaced the
    /// made-up world with real places (all place and era ids changed), so
    /// version 1 saves are ignored and the Title offers only New Run.
    /// </summary>
    private const int MinCompatibleVersion = 2;

    /// <summary>Save file name (single slot).</summary>
    private const string FileName = "nope_save.json";

    /// <summary>Added to the save's path for the temp file a save is written to first.</summary>
    private const string TempSuffix = ".tmp";

    /// <summary>Added to the save's path for the backup the replace keeps.</summary>
    private const string BackupSuffix = ".bak";

    /// <summary>Full save file path.</summary>
    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>The file a save is written to before it replaces the save.</summary>
    private static string TempPath => SavePath + TempSuffix;

    /// <summary>The save before the last one, kept by the replace.</summary>
    private static string BackupPath => SavePath + BackupSuffix;

    /// <summary>Every file of the save slot: the save, the temp file and the backup (New Run deletes them all; a tool standing in for the player backs them up).</summary>
    public static IReadOnlyList<string> Files => new[] { SavePath, TempPath, BackupPath };

    /// <summary>
    /// Wrapper so we can version the payload and migrate later.
    /// </summary>
    [Serializable]
    private sealed class SaveFile
    {
        public int version;
        public WorldState world;
    }

    /// <summary>Returns true if a file of the slot holds a save this build can continue (older versions are ignored with a warning).</summary>
    public static bool HasSave()
    {
        SaveSource source = Source();
        if (source == SaveSource.None)
        {
            Debug.Log($"[SaveSystem] HasSave: false (no save this build can continue at '{SavePath}').");
            return false;
        }

        Debug.Log($"[SaveSystem] HasSave: true ('{PathOf(source)}', version {ReadVersion(PathOf(source))}).");
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
            ? $"[SaveSystem] Ignoring the save at '{path}': version {version} predates the real-world content (needs {MinCompatibleVersion}+). Start a new run."
            : $"[SaveSystem] Ignoring the save file at '{path}': it cannot be read.");
        return false;
    }

    /// <summary>The file's save version, or -1 if it is missing or unreadable.</summary>
    private static int ReadVersion(string path)
    {
        try
        {
            SaveFile file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
            return file != null ? file.version : -1;
        }
        catch (Exception)
        {
            return -1;
        }
    }

    /// <summary>
    /// Writes the world state to disk: whole to the temp file, then swapped in
    /// for the save (Commit). Returns true on success.
    /// </summary>
    public static bool Save(WorldState world)
    {
        Debug.Log($"[SaveSystem] >>> Entering Save (day {world?.day}).");

        if (world == null)
        {
            Debug.LogError("SaveSystem.Save called with null WorldState.");
            return false;
        }

        try
        {
            var file = new SaveFile { version = SaveVersion, world = world };
            string json = JsonUtility.ToJson(file, prettyPrint: true);

            File.WriteAllText(TempPath, json);
            Commit();

            Debug.Log($"[SaveSystem] <<< Exiting Save (success, day {world.day}, money={world.money}, path='{SavePath}').");

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.Save failed: {e.Message}");
            return false;
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
    /// left: SaveFiles.Pick; a recovered file is copied back as the save).
    /// Returns null if no file holds a save this build can continue.
    /// </summary>
    public static WorldState Load()
    {
        Debug.Log("[SaveSystem] >>> Entering Load.");

        SaveSource source = Source();
        if (source == SaveSource.None)
        {
            Debug.Log("[SaveSystem] <<< Exiting Load — no save this build can continue.");
            return null;
        }

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
            }

            if (file.version != SaveVersion)
                Debug.LogWarning($"SaveSystem.Load: save version {file.version} != current {SaveVersion}. Loading with defaults for new fields.");

            Debug.Log($"[SaveSystem] <<< Exiting Load (success, day {file.world.day}, money={file.world.money}, version={file.version}).");

            return file.world;
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.Load failed: {e.Message}");
            return null;
        }
    }

    /// <summary>Deletes the save slot's files, the temp file and the backup included, so no older run can be recovered (used by "New Run").</summary>
    public static void Delete()
    {
        Debug.Log("[SaveSystem] >>> Entering Delete.");

        try
        {
            int removed = 0;
            foreach (string path in Files)
            {
                if (!File.Exists(path))
                    continue;
                File.Delete(path);
                removed++;
            }

            Debug.Log($"[SaveSystem] <<< Exiting Delete ({removed} save file(s) removed).");
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.Delete failed: {e.Message}");
        }
    }
}
