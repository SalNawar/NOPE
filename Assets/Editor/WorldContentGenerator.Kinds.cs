using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

/// <summary>
/// Generate World's traveller-kinds part (the redesign's phases 3 and 6):
/// each kind's case blueprint (content.blueprints, archetype pools wired),
/// each day's traveller mix (days[].kinds, written into DayPlanSO kinds), and
/// the claim line per kind (world_source.json interview.claims, one row per
/// kind, written into the library as KindLine entries "interview.claims.{Kind}"),
/// checked through the Domain rule the validator shares (Interview.ClaimProblems)
/// against the kinds the wired blueprints make.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>One row of content.blueprints: a kind's name (TravellerKind) and its blueprint's asset path.</summary>
    [Serializable] private sealed class KindBlueprintData
    {
        /// <summary>The kind's name ("RichTourist").</summary>
        public string kind;

        /// <summary>The blueprint asset's path.</summary>
        public string asset;
    }

    /// <summary>One row of days[].kinds: a kind's name and its weight in the day's mix.</summary>
    [Serializable] private sealed class KindWeightData
    {
        /// <summary>The kind's name ("Displaced").</summary>
        public string kind;

        /// <summary>Its weight (0 or more).</summary>
        public float weight;
    }

    /// <summary>Loads content.blueprints: each row a known kind, listed once, whose asset is a blueprint of that kind.</summary>
    private static void LoadBlueprints(ContentData content, Authored a, List<string> errors)
    {
        KindBlueprintData[] rows = content.blueprints ?? Array.Empty<KindBlueprintData>();
        if (rows.Length == 0)
            errors.Add("content.blueprints is empty: each traveller kind a day lists needs its case blueprint.");

        foreach (KindBlueprintData row in rows)
        {
            if (row == null)
                continue;
            if (!ParseEnum(row.kind, out TravellerKind kind))
            {
                errors.Add($"content.blueprints: '{row.kind}' is not a traveller kind ({string.Join(", ", Enum.GetNames(typeof(TravellerKind)))}).");
                continue;
            }
            if (a.blueprints.ContainsKey(kind))
            {
                errors.Add($"content.blueprints lists {kind} twice.");
                continue;
            }

            CaseBlueprintSO blueprint = Require<CaseBlueprintSO>(row.asset, $"{kind} case blueprint", errors);
            if (blueprint == null)
                continue;
            if (blueprint.Kind != kind)
                errors.Add($"content.blueprints: '{row.asset}' makes {blueprint.Kind} travellers, not {kind}.");
            a.blueprints[kind] = blueprint;
        }
    }

    /// <summary>Gives every kind's blueprint the content's archetype pool.</summary>
    private static void WireBlueprints(Authored authored)
    {
        foreach (CaseBlueprintSO blueprint in authored.blueprints.Values)
        {
            var so = new SerializedObject(blueprint);
            SerializedArrays.Set(so, "archetypePool", authored.archetypes);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(blueprint);
        }
    }

    /// <summary>Writes a day's traveller mix (days[].kinds) into its plan's kinds: each kind's blueprint and weight, in authored order (CheckDayKinds ran first).</summary>
    private static void WriteKinds(SerializedObject so, DayData d, Authored authored)
    {
        KindWeightData[] kinds = d.kinds ?? Array.Empty<KindWeightData>();
        SerializedProperty list = so.FindProperty("kinds");
        list.arraySize = kinds.Length;
        for (int i = 0; i < kinds.Length; i++)
        {
            SerializedProperty el = list.GetArrayElementAtIndex(i);
            ParseEnum(kinds[i].kind, out TravellerKind kind);
            el.FindPropertyRelative("blueprint").objectReferenceValue = authored.blueprints.TryGetValue(kind, out CaseBlueprintSO b) ? b : (Object)null;
            el.FindPropertyRelative("weight").floatValue = kinds[i].weight;
        }
    }

    /// <summary>
    /// Checks every day's traveller mix (days[].kinds): at least one kind with
    /// a positive weight, each a known kind listed once with a blueprint in
    /// content.blueprints and a weight of 0 or more; and a day whose premades
    /// may stand (forced or pooled) lists the displaced with a positive weight
    /// (a premade's slot draws only the displaced, TravellerKinds.PickWeight).
    /// </summary>
    private static void CheckDayKinds(WorldSource src, Authored authored, List<string> errors)
    {
        foreach (DayData d in src.days ?? Array.Empty<DayData>())
        {
            KindWeightData[] kinds = d.kinds ?? Array.Empty<KindWeightData>();
            var seen = new HashSet<TravellerKind>();
            foreach (KindWeightData k in kinds)
            {
                if (k == null || !ParseEnum(k.kind, out TravellerKind kind))
                {
                    errors.Add($"Day '{d.asset}' kinds: '{k?.kind}' is not a traveller kind ({string.Join(", ", Enum.GetNames(typeof(TravellerKind)))}).");
                    continue;
                }
                if (!seen.Add(kind))
                    errors.Add($"Day '{d.asset}' lists the kind {kind} twice.");
                if (!authored.blueprints.ContainsKey(kind))
                    errors.Add($"Day '{d.asset}' lists {kind} travellers, but content.blueprints has no {kind} blueprint.");
                if (k.weight < 0f)
                    errors.Add($"Day '{d.asset}' gives {kind} the weight {k.weight}; a weight is 0 or more.");
            }

            if (!kinds.Any(k => k != null && k.weight > 0f && ParseEnum(k.kind, out TravellerKind _)))
                errors.Add($"Day '{d.asset}' has no traveller kind with a positive weight (days[].kinds).");

            bool premades = (d.premades ?? Array.Empty<string>()).Length > 0 || (d.forced ?? Array.Empty<ForcedData>()).Any(f => !string.IsNullOrEmpty(f.premade));
            bool displaced = kinds.Any(k => k != null && k.weight > 0f && ParseEnum(k.kind, out TravellerKind kind) && kind == TravellerKind.Displaced);
            if (premades && !displaced)
                errors.Add($"Day '{d.asset}' has premades but no Displaced kind with a positive weight; a premade stands only as a displaced traveller.");
        }
    }
    /// <summary>One row of interview.claims: a kind's name (TravellerKind) and its claim ({place}).</summary>
    [Serializable] private sealed class ClaimData
    {
        /// <summary>The kind's name ("Displaced").</summary>
        public string kind;

        /// <summary>The kind's claim line ({place}).</summary>
        public string text;
    }

    /// <summary>The id of a kind's claim line, "interview.claims.{kind}": BuildClaims writes it, CheckClaims checks it.</summary>
    private static string ClaimLineId(string kind) => InterviewLineId($"claims.{kind}");

    /// <summary>
    /// Checks interview.claims (errors added, nothing written): each row names
    /// a TravellerKind, its line is ASCII and its id is unique (through
    /// <paramref name="id"/>, CheckInterview's one id set), and
    /// Interview.ClaimProblems holds (a line per kind the wired blueprints
    /// make, none twice, each with {place}).
    /// </summary>
    private static void CheckClaims(InterviewData iv, Authored authored, List<string> errors, Action<string, string> id)
    {
        ClaimData[] claims = iv.claims ?? Array.Empty<ClaimData>();
        foreach (ClaimData c in claims)
        {
            if (c == null)
                continue;
            if (!ParseEnum(c.kind, out TravellerKind _))
                errors.Add($"interview.claims: '{c.kind}' is not a traveller kind ({string.Join(", ", Enum.GetNames(typeof(TravellerKind)))}).");
            CheckAscii(ClaimLineId(c.kind), c.text, errors);
            id(ClaimLineId(c.kind), $"interview.claims '{c.kind}'");
        }

        errors.AddRange(Interview.ClaimProblems(BuildClaims(claims), Blueprints(authored).Select(b => b.Kind).Distinct()));
    }

    /// <summary>The claim rows as the library holds them (rows naming no kind are left out; CheckClaims reports them).</summary>
    private static List<KindLine> BuildClaims(ClaimData[] claims) =>
        (claims ?? Array.Empty<ClaimData>())
            .Select(c => c == null ? null
                : ParseEnum(c.kind, out TravellerKind kind) ? new KindLine { kind = kind, line = new LineText(ClaimLineId(c.kind), c.text) }
                : null)
            .Where(k => k != null)
            .ToList();
}
