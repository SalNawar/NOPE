using System;
using System.IO;

/// <summary>
/// Where the save slot's files live. A player keeps them in the platform's
/// persistent data folder. The editor keeps them inside the project, in
/// Library/EditorSaves (Library is ignored by git and every worktree has its
/// own), so each worktree's editor has its own save: several editors used to
/// share the product's one persistent data folder, and parallel plays
/// overwrote one another's save. Pure, so the rule is tested headless;
/// SaveSystem applies it. A player started with <see cref="FolderSwitch"/>
/// keeps its save in the folder after the switch instead (the build's boot
/// check, BLD-02: it starts each new player without touching the player's
/// own save).
/// </summary>
public static class SaveLocation
{
    /// <summary>
    /// The save slot's folder. In the editor: Library/EditorSaves under the
    /// project root, the parent of <paramref name="assetsPath"/> (the project's
    /// Assets folder: Application.dataPath; either separator, a trailing one
    /// allowed). In a player: <paramref name="persistentDataPath"/>, unchanged.
    /// </summary>
    public static string Folder(bool inEditor, string assetsPath, string persistentDataPath)
    {
        if (!inEditor)
            return persistentDataPath;

        string root = Path.GetDirectoryName(assetsPath.Replace('\\', '/').TrimEnd('/'));
        return Path.Combine(root, "Library", "EditorSaves");
    }

    /// <summary>The player's command-line switch whose next argument is the save folder (the build's boot check).</summary>
    public const string FolderSwitch = "-saveFolder";

    /// <summary>The folder after <see cref="FolderSwitch"/> in <paramref name="args"/>, or null when the switch is absent or has no folder after it.</summary>
    public static string FolderFromArgs(string[] args)
    {
        if (args == null)
            return null;
        int i = Array.IndexOf(args, FolderSwitch);
        return i >= 0 && i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]) && !args[i + 1].StartsWith("-") ? args[i + 1] : null;
    }
}
