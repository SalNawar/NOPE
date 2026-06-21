using System;
using System.IO;
using UnityEngine;

/// <summary>
/// JSON save/load for WorldState. One save slot for Alpha.
/// File lives at Application.persistentDataPath/nope_save.json.
/// </summary>
public static class SaveSystem
{
    /// <summary>Bump when WorldState shape changes incompatibly.</summary>
    private const int SaveVersion = 1;

    /// <summary>Save file name (single slot).</summary>
    private const string FileName = "nope_save.json";

    /// <summary>Full save file path.</summary>
    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>
    /// Wrapper so we can version the payload and migrate later.
    /// </summary>
    [Serializable]
    private sealed class SaveFile
    {
        public int version;
        public WorldState world;
    }

    /// <summary>Returns true if a save file exists.</summary>
    public static bool HasSave()
    {
        bool exists = File.Exists(SavePath);
        Debug.Log($"[SaveSystem] HasSave: {exists} ('{SavePath}').");
        return exists;
    }

    /// <summary>
    /// Writes the world state to disk. Returns true on success.
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

            // Write to a temp file first, then move — avoids corrupt saves on crash.
            string tmp = SavePath + ".tmp";
            File.WriteAllText(tmp, json);

            if (File.Exists(SavePath))
                File.Delete(SavePath);

            File.Move(tmp, SavePath);

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
    /// Loads the world state from disk. Returns null if missing or unreadable.
    /// </summary>
    public static WorldState Load()
    {
        Debug.Log("[SaveSystem] >>> Entering Load.");

        if (!HasSave())
        {
            Debug.Log("[SaveSystem] <<< Exiting Load — no save file present.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveFile file = JsonUtility.FromJson<SaveFile>(json);

            if (file == null || file.world == null)
            {
                Debug.LogError("SaveSystem.Load: save file was empty or malformed.");
                return null;
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

    /// <summary>Deletes the save file (used by "New Run").</summary>
    public static void Delete()
    {
        Debug.Log("[SaveSystem] >>> Entering Delete.");

        try
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] <<< Exiting Delete (save file removed).");
            }
            else
            {
                Debug.Log("[SaveSystem] <<< Exiting Delete (no save file to remove).");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveSystem.Delete failed: {e.Message}");
        }
    }
}
