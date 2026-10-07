using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The PlayerPrefs name of a per-player preference in this build or project
/// (PrefKeys.Scoped: the name itself in a player, scoped to the project in the
/// editor, so parallel worktrees' editors never change one another's
/// preferences). Every PlayerPrefs read and write of a preference goes through it.
/// </summary>
public static class PlayerPrefKeys
{
    /// <summary>The names worked out so far (a preference read often builds its name once).</summary>
    private static readonly Dictionary<string, string> Names = new Dictionary<string, string>();

    /// <summary>The stored name of preference <paramref name="key"/>.</summary>
    public static string For(string key)
    {
        if (!Names.TryGetValue(key, out string name))
            Names[key] = name = PrefKeys.Scoped(key, Application.isEditor, Application.dataPath);
        return name;
    }
}
