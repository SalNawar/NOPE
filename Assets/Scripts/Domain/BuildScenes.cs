using System;
using System.Collections.Generic;

/// <summary>One entry of the player build's scene list: a scene asset's path and whether builds include it.</summary>
public readonly struct BuildScene
{
    /// <summary>An entry for the scene at <paramref name="path"/>.</summary>
    public BuildScene(string path, bool enabled)
    {
        Path = path;
        Enabled = enabled;
    }

    /// <summary>The scene asset's path ("Assets/Scenes/TitleScene.unity").</summary>
    public string Path { get; }

    /// <summary>True when player builds include the scene.</summary>
    public bool Enabled { get; }
}

/// <summary>
/// The player build's scene list (audit R3-001 / R6-025): a player build boots
/// the list's first enabled scene, which was the legacy Test_DayLoop prototype.
/// Pure, so the order is tested headless; Build Office UI applies it.
/// </summary>
public static class BuildScenes
{
    /// <summary>
    /// The list in boot order: the title, the art office, its gameplay layer
    /// (loaded with it) and Home first, each enabled (a missing one added);
    /// every other listed scene after them in its old order, disabled, so no
    /// prototype ships or boots. Blank paths and repeats are dropped; a null
    /// list reads as empty.
    /// </summary>
    public static List<BuildScene> Order(IEnumerable<BuildScene> current, string title, string office, string gameplay, string home)
    {
        string[] shipped = { title, office, gameplay, home };
        var order = new List<BuildScene>();
        var listed = new HashSet<string>(StringComparer.Ordinal);
        foreach (string path in shipped)
            if (!string.IsNullOrWhiteSpace(path) && listed.Add(path))
                order.Add(new BuildScene(path, true));

        foreach (BuildScene scene in current ?? Array.Empty<BuildScene>())
            if (!string.IsNullOrWhiteSpace(scene.Path) && listed.Add(scene.Path))
                order.Add(new BuildScene(scene.Path, false));

        return order;
    }
}
