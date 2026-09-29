using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Generate World's world part (the endings spec E0-E1): world_source.json
/// "world" is checked (each answer a FactorAnswer name, WorldContent.Problems;
/// every place's leaning, famous traveller's pull and history rule's pull
/// names a factor answered by pulls and one of its outcomes; every role an
/// archetype of the content) and written into the library's world block
/// (ContentLibrarySO.World): the factors the end of the demo lists, their
/// outcomes and the roles' pulls. The leanings are written on the places, the
/// premades' pulls on the premades and the rules' pulls as PullOutcome ops.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>world_source.json "world": the factors the end of the demo answers, their outcomes and the roles' pulls (the endings spec E0-E1).</summary>
    [Serializable] private sealed class WorldData
    {
        public WorldFactorData[] factors;
        public WorldOutcome[] outcomes;
        public WorldRole[] roles;
    }

    /// <summary>One factor's row; its answer is a FactorAnswer name; a Pulls factor names its "as you found it" outcome and its split lines.</summary>
    [Serializable] private sealed class WorldFactorData
    {
        public string id;
        public string question;
        public string answer;
        public string foundAs;
        public string statusQuo;
        public string splitLine;
        public string splitHeadline;
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
            world.factors.Add(new WorldFactor
            {
                id = f.id, question = f.question, answer = answer, foundAs = f.foundAs ?? string.Empty,
                statusQuo = f.statusQuo ?? string.Empty, splitLine = f.splitLine ?? string.Empty, splitHeadline = f.splitHeadline ?? string.Empty
            });
        }
        world.outcomes = (w?.outcomes ?? Array.Empty<WorldOutcome>()).Where(o => o != null).ToList();
        world.roles = (w?.roles ?? Array.Empty<WorldRole>()).Where(r => r != null).ToList();
        return world;
    }

    /// <summary>
    /// The world's references from the rest of the source (the endings spec
    /// §8.1): each place leans at most once per factor, on a factor answered
    /// by pulls and one of its outcomes; each famous traveller's and history
    /// rule's pull names one, by more than 0; each role names an archetype of
    /// the content (content.archetypes) at most once.
    /// </summary>
    private static void CheckWorldRefs(WorldSource src, WorldContent world, Authored authored, List<string> errors)
    {
        foreach (PlaceData p in src.places ?? Array.Empty<PlaceData>())
        {
            var leaned = new HashSet<string>();
            foreach (OutcomeRef l in p.leanings ?? Array.Empty<OutcomeRef>())
            {
                errors.AddRange(world.RefProblems($"Place '{PlaceId(p)}' leaning", l?.factor, l?.outcome, null));
                if (l != null && !leaned.Add(l.factor ?? string.Empty))
                    errors.Add($"Place '{PlaceId(p)}' leans twice on '{l.factor}'; a place leans at most once per factor.");
            }
        }

        foreach (PremadeData m in src.premades ?? Array.Empty<PremadeData>())
            foreach (OutcomePull pull in m.pulls ?? Array.Empty<OutcomePull>())
                errors.AddRange(world.RefProblems($"Premade '{m.id}' pull", pull?.factor, pull?.outcome, pull?.amount ?? 0f));

        foreach (HistoryRuleData r in src.history?.rules ?? Array.Empty<HistoryRuleData>())
            foreach (OutcomePull pull in r.pulls ?? Array.Empty<OutcomePull>())
                errors.AddRange(world.RefProblems($"History rule '{r.id}' pull", pull?.factor, pull?.outcome, pull?.amount ?? 0f));

        var archetypes = new HashSet<string>((authored.archetypes ?? Array.Empty<ArchetypeSO>()).Where(a => a != null).Select(a => a.id));
        var roles = new HashSet<string>();
        foreach (WorldRole role in world.roles)
        {
            if (!string.IsNullOrWhiteSpace(role.archetype) && !archetypes.Contains(role.archetype))
                errors.Add($"world.roles '{role.archetype}' is no archetype of content.archetypes ({string.Join(", ", archetypes)}).");
            if (!roles.Add(role.archetype ?? string.Empty))
                errors.Add($"world.roles lists '{role.archetype}' twice; a role pulls one factor.");
        }
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
