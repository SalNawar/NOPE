using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Generate World's citizen file part (the scanner app spec §3; Saleh: "in
/// citizen lookup we need to have more lore and storytelling"):
/// world_source.json "lore" (each premade's lines, the random travellers'
/// templates, the threads) is checked (every kind and trait named, then
/// CitizenFile.Problems: every template slot valid for its kinds and traits,
/// every premade with 2 to 4 lines, plain ASCII, one line long at most) and
/// written verbatim into the library's lore content, which CaseFactory reads
/// to write each traveller's file.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>world_source.json "lore".</summary>
    [Serializable] private sealed class LoreData
    {
        public float clueChance;
        public int linesMin;
        public int linesMax;
        public int yearMin;
        public int yearMax;
        public string[] relatives;
        public LorePremadeData[] premades;
        public LoreTemplateData[] templates;
        public LoreThreadData[] threads;
    }

    /// <summary>One premade's lines (lore.premades).</summary>
    [Serializable] private sealed class LorePremadeData
    {
        public string premade;
        public string[] lines;
    }

    /// <summary>One template (lore.templates): kinds as TravellerKind names.</summary>
    [Serializable] private sealed class LoreTemplateData
    {
        public string id;
        public string[] kinds;
        public string personality;
        public string[] traits;
        public string clue;
        public string text;
    }

    /// <summary>One thread (lore.threads).</summary>
    [Serializable] private sealed class LoreThreadData
    {
        public string id;
        public string verdict;
        public string text;
    }

    /// <summary>Checks the lore: every kind a TravellerKind, then CitizenFile.Problems against the source's premades and cast (every premade has its lines).</summary>
    private static void CheckLore(WorldSource src, List<string> errors)
    {
        if (src.lore == null)
        {
            errors.Add("world_source.json has no \"lore\" (the citizen file's lines: the scanner app spec §3).");
            return;
        }
        foreach (LoreTemplateData t in src.lore.templates ?? Array.Empty<LoreTemplateData>())
            foreach (string kind in t?.kinds ?? Array.Empty<string>())
                if (!ParseEnum(kind, out TravellerKind _))
                    errors.Add($"lore.templates: '{t.id}' kind '{kind}' is none of {string.Join(", ", Enum.GetNames(typeof(TravellerKind)))}.");
        errors.AddRange(CitizenFile.Problems(BuildLore(src.lore),
                                             (src.premades ?? Array.Empty<PremadeData>()).Select(p => p.id),
                                             (src.personalities ?? Array.Empty<PersonalityData>()).Select(p => p.id), true));
    }

    /// <summary>The library's lore from the source, verbatim (an unknown kind is dropped; CheckLore refuses it).</summary>
    private static LoreContent BuildLore(LoreData l) => new LoreContent
    {
        clueChance = l?.clueChance ?? 0f,
        linesMin = l?.linesMin ?? 0,
        linesMax = l?.linesMax ?? 0,
        yearMin = l?.yearMin ?? 0,
        yearMax = l?.yearMax ?? 0,
        relatives = (l?.relatives ?? Array.Empty<string>()).ToList(),
        premades = (l?.premades ?? Array.Empty<LorePremadeData>()).Where(p => p != null).Select(p => new LorePremade
        {
            premade = p.premade ?? string.Empty,
            lines = (p.lines ?? Array.Empty<string>()).ToList()
        }).ToList(),
        templates = (l?.templates ?? Array.Empty<LoreTemplateData>()).Where(t => t != null).Select(t => new LoreTemplate
        {
            id = t.id ?? string.Empty,
            kinds = (t.kinds ?? Array.Empty<string>()).Select(k => ParseEnum(k, out TravellerKind kind) ? (TravellerKind?)kind : null)
                .Where(k => k.HasValue).Select(k => k.Value).ToList(),
            personality = t.personality ?? string.Empty,
            traits = (t.traits ?? Array.Empty<string>()).ToList(),
            clue = t.clue ?? string.Empty,
            text = t.text ?? string.Empty
        }).ToList(),
        threads = (l?.threads ?? Array.Empty<LoreThreadData>()).Where(t => t != null).Select(t => new LoreThread
        {
            id = t.id ?? string.Empty,
            verdict = t.verdict ?? string.Empty,
            text = t.text ?? string.Empty
        }).ToList()
    };

    /// <summary>Writes the lore into the library (authoritative).</summary>
    private static void WireLore(ContentLibrarySO lib, LoreData lore)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("lore").boxedValue = BuildLore(lore);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }
}
