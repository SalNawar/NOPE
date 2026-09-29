using System;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Generate World's world part (the endings spec E0): world_source.json
/// "world" is checked (each answer a FactorAnswer name, WorldContent.Problems)
/// and written into the library's world block (ContentLibrarySO.World), the
/// factors the end of the demo lists. Phase E1 adds the outcome rows here.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>world_source.json "world": the factors the end of the demo answers (the endings spec E0).</summary>
    [Serializable] private sealed class WorldData
    {
        public WorldFactorData[] factors;
    }

    /// <summary>One factor's row; its answer is a FactorAnswer name.</summary>
    [Serializable] private sealed class WorldFactorData
    {
        public string id;
        public string question;
        public string answer;
        public string foundAs;
    }

    /// <summary>The library's world block from the source, the rows in order; an answer that names no FactorAnswer is an error (added to <paramref name="errors"/> when given).</summary>
    private static WorldContent BuildWorld(WorldData w, List<string> errors)
    {
        var world = new WorldContent();
        foreach (WorldFactorData f in w?.factors ?? Array.Empty<WorldFactorData>())
        {
            if (f == null)
                continue;
            if (!ParseEnum(f.answer, out FactorAnswer answer))
                errors?.Add($"world.factors '{f.id}': answer '{f.answer}' is not one of {string.Join(", ", Enum.GetNames(typeof(FactorAnswer)))}.");
            world.factors.Add(new WorldFactor { id = f.id, question = f.question, answer = answer, foundAs = f.foundAs });
        }
        return world;
    }

    /// <summary>Writes the world block (checked by BuildWorld and WorldContent.Problems) into the library.</summary>
    private static void WireWorld(ContentLibrarySO lib, WorldContent world)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("world").boxedValue = world;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }
}
