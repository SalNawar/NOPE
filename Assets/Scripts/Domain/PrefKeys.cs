/// <summary>
/// The names the per-player preferences (Settings' language and motion, the
/// desktop's layout) are stored under in PlayerPrefs. A player uses each name
/// as it is. The editor adds the project's root
/// ("TimeDesk.UiLanguage@E:/unity/NOPE-art"): every worktree's editor shares
/// the product's one PlayerPrefs store, and a probe that chose "Always
/// English" in one editor switched a play in another editor to English
/// labels halfway through (run 7: two audit plays of the same seed differed
/// on days 4-7). The SaveLocation pattern; pure, so the rule is tested
/// headless; PlayerPrefKeys applies it.
/// </summary>
public static class PrefKeys
{
    /// <summary>
    /// The stored name of preference <paramref name="key"/>: unchanged in a
    /// player; in the editor, <paramref name="key"/>, "@" and the project's
    /// root, the parent of <paramref name="assetsPath"/> (the project's Assets
    /// folder: Application.dataPath; either separator, a trailing one allowed),
    /// written with forward slashes.
    /// </summary>
    public static string Scoped(string key, bool inEditor, string assetsPath)
    {
        if (!inEditor || string.IsNullOrEmpty(assetsPath))
            return key;
        string path = assetsPath.Replace('\\', '/').TrimEnd('/');
        int slash = path.LastIndexOf('/');
        return key + "@" + (slash > 0 ? path.Substring(0, slash) : path);
    }
}
