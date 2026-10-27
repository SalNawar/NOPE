using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

/// <summary>
/// The painted desk's measured map for the headless tests (desk v3; CAM's
/// reading pose on the painted desk): the homes and the fixed art the
/// importer wrote into the prefab (DeskPainted.homes and .fixedArt, the one
/// source the game reads: DeskLayoutSettings.UsePainted at bind), read off
/// the prefab's text (Unity's YAML: one "- id:" block per home with its
/// fields, under the component's "homes:" and "fixedArt:" keys), so the pins
/// on the pose are the engine's numbers, heights included. No Unity: the
/// house runner runs the tests that read it.
/// </summary>
public static class PaintedDeskFixture
{
    /// <summary>The importer's prefab (DeskPaintedImporter.PrefabPath, which the editor assembly holds).</summary>
    public const string PrefabPath = "Assets/Art/Office/DeskPainted/Prefabs/DeskPainted.prefab";

    /// <summary>A file of the project, from the tests' working folder or this file's.</summary>
    private static string[] Lines(string relative, [CallerFilePath] string here = "") =>
        File.ReadAllLines(File.Exists(relative) ? relative : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", relative));

    /// <summary>The painted homes and fixed art as the prefab carries them (the importer's measured map: desk v3's rectangles, the heights measured on the meshes).</summary>
    public static (DeskHome[] homes, DeskHome[] fixedArt) Prefab()
    {
        var homes = new List<DeskHome>();
        var fixedArt = new List<DeskHome>();
        List<DeskHome> into = null;
        DeskHome home = null;
        foreach (string raw in Lines(PrefabPath))
        {
            string line = raw.TrimEnd();
            if (line == "  homes:")
            {
                into = homes;
                continue;
            }
            if (line == "  fixedArt:")
            {
                into = fixedArt;
                continue;
            }
            if (into == null)
                continue;
            if (line.StartsWith("  - id: ", StringComparison.Ordinal))
            {
                home = new DeskHome { id = line.Substring(8).Trim() };
                into.Add(home);
                continue;
            }
            if (home == null || !line.StartsWith("    ", StringComparison.Ordinal))
            {
                // The list ended: the component's next key.
                into = null;
                home = null;
                continue;
            }
            int colon = line.IndexOf(':');
            if (colon < 0)
                continue;
            string key = line.Substring(4, colon - 4).Trim(), value = line.Substring(colon + 1).Trim();
            switch (key)
            {
                case "x": home.x = Num(value); break;
                case "y": home.y = Num(value); break;
                case "width": home.width = Num(value); break;
                case "depth": home.depth = Num(value); break;
                case "yaw": home.yaw = Num(value); break;
                case "height": home.height = Num(value); break;
                case "show": home.show = (HomeShow)int.Parse(value, CultureInfo.InvariantCulture); break;
                case "slide": home.slide = (HomeSlide)int.Parse(value, CultureInfo.InvariantCulture); break;
                case "artParts": home.artParts = value; break;
            }
        }
        if (homes.Count == 0)
            throw new InvalidOperationException($"{PrefabPath} carries no painted homes (Tools > TimeDesk > Import Painted Desk writes them).");
        return (homes.ToArray(), fixedArt.ToArray());
    }

    /// <summary>The map in use while the painted desk shows: the prefab's homes merged over the plan's (DeskPaintedMap.Merge, as DeskLayoutSettings.UsePainted does).</summary>
    public static DeskHome[] Map() => DeskPaintedMap.Merge(DeskHomeMap.Approved(), Prefab().homes);

    private static float Num(string value) => float.Parse(value, CultureInfo.InvariantCulture);
}
