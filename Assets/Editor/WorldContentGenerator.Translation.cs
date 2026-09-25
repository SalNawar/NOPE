using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generate World's translation part (piece 9): checks world_source.json
/// "translation" and every place's tongue, writes one Papers and one Speech
/// translator upgrade per pack and the one-shot notice trigger into the owned
/// Assets/Data/World/Translation, and the library's TranslationSettings.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>Folder of the generated translation assets (translator upgrades, the notice trigger).</summary>
    private const string TranslationFolder = WorldRoot + "/Translation";

    /// <summary>The notice trigger's id (the validator looks for it when translation starts after day 1).</summary>
    public const string TranslationNoticeId = "translation_notice";

    /// <summary>The UI strings translation reads (ui.strings must hold them).</summary>
    private static readonly string[] TranslationKeys = { "compare.untranslated", "settings.motion", "settings.motionFull", "settings.motionReduced" };

    /// <summary>
    /// Checks the translation source (errors added, nothing written): the
    /// shared rules (TranslationSettings.Problems: tongues, packs, scripts,
    /// tables, the fallback cipher, the flip knobs, every place's tongue),
    /// costs of at least 0, no generated upgrade id equal to a hand-authored
    /// one, an ASCII notice when translation starts after day 1, and the UI
    /// strings it reads.
    /// </summary>
    private static void CheckTranslation(WorldSource src, Authored authored, List<string> errors)
    {
        TranslationData t = src.translation;
        if (t == null)
        {
            errors.Add("world_source.json has no \"translation\" section (piece 9).");
            return;
        }

        foreach (string problem in BuildTranslation(t).Problems(src.places.Select(place => new KeyValuePair<string, string>(PlaceId(place), place.tongue))))
            errors.Add(problem);

        foreach (PackData pack in t.packs ?? Array.Empty<PackData>())
            if (pack != null && (pack.writtenCost < 0 || pack.spokenCost < 0))
                errors.Add($"translation.packs: pack '{pack.id}' has a cost below 0.");

        var generated = new HashSet<string>((t.packs ?? Array.Empty<PackData>()).Where(p => p != null)
            .SelectMany(p => new[] { Translation.UpgradeId(p.id, TranslatorKind.Written), Translation.UpgradeId(p.id, TranslatorKind.Spoken) }));
        if (authored.library != null)
            foreach (UpgradeSO u in HandAuthored(new SerializedObject(authored.library), "upgrades").OfType<UpgradeSO>())
                if (generated.Contains(u.id))
                    errors.Add($"Upgrade '{AssetDatabase.GetAssetPath(u)}' has the id '{u.id}', which Generate World writes for a translator.");

        if (t.fromDay > 1 && (string.IsNullOrWhiteSpace(t.announce) || !IsAscii(t.announce)))
            errors.Add("translation.announce must be non-blank ASCII (the morning paper announces foreign text the day it starts).");

        var keys = new HashSet<string>((src.ui?.strings ?? Array.Empty<StringData>()).Where(s => s != null).Select(s => s.key));
        foreach (string key in TranslationKeys)
            RequireKey(keys, key, "translation (piece 9)", errors);
    }

    /// <summary>The library's translation settings from the source (tongues copied as authored; packs without their costs).</summary>
    private static TranslationSettings BuildTranslation(TranslationData t) => new TranslationSettings
    {
        rules = new TranslationRules
        {
            fromDay = t.fromDay,
            tongues = (t.tongues ?? Array.Empty<Tongue>()).ToList(),
            packs = (t.packs ?? Array.Empty<PackData>()).Select(p => p == null ? null : new TranslatorPack { id = p.id, displayName = p.displayName }).ToList()
        },
        scripts = (t.scripts ?? Array.Empty<ScriptData>()).Select(s => s == null ? null : new TranslationScript
        {
            id = s.id,
            rightToLeft = s.rightToLeft,
            fonts = (s.fonts ?? Array.Empty<FontData>())
                .Select(f => f == null ? null : new FontCandidate { file = f.file ?? string.Empty, face = f.face, family = f.family ?? string.Empty, style = string.IsNullOrWhiteSpace(f.style) ? "Regular" : f.style })
                .ToList()
        }).ToList(),
        flip = t.flip ?? new FlipTiming(),
        fallbackGlyphs = t.fallbackGlyphs
    };

    /// <summary>
    /// Writes Translation/Upgrade_Tr_{Pack}_{Papers|Speech}.asset: the pack's
    /// translator of one kind (id Translation.UpgradeId, "{Pack} Translator:
    /// Papers" or "Speech", a description naming its tongues, the pack's
    /// cost, no unlock effect).
    /// </summary>
    private static UpgradeSO MakeTranslator(PackData pack, TranslatorKind kind, TranslationData t, HashSet<string> written)
    {
        bool papers = kind == TranslatorKind.Written;
        UpgradeSO so = LoadOrCreate<UpgradeSO>($"{TranslationFolder}/Upgrade_Tr_{Pascal(pack.id)}_{(papers ? "Papers" : "Speech")}.asset", written);
        string tongues = JoinNames((t.tongues ?? Array.Empty<Tongue>()).Where(g => g != null && g.pack == pack.id).Select(g => g.displayName).ToList());
        so.id = Translation.UpgradeId(pack.id, kind);
        so.displayName = $"{pack.displayName} Translator: {(papers ? "Papers" : "Speech")}";
        so.description = papers ? $"Translates {tongues} on scanned papers." : $"Translates {tongues} in speech.";
        so.cost = papers ? pack.writtenCost : pack.spokenCost;
        so.unlockEffect = null;
        EditorUtility.SetDirty(so);
        return so;
    }

    /// <summary>
    /// Writes Translation/Trigger_TranslationNotice.asset when foreign text
    /// starts after day 1: a one-shot trigger whose news line is the notice,
    /// firing the night before fromDay (DayAtLeast Gates.UnlockNight(fromDay)),
    /// so that morning's paper carries it. None when fromDay is 1.
    /// </summary>
    private static TimelineTriggerSO[] MakeTranslationNotice(TranslationData t, HashSet<string> written)
    {
        if (t.fromDay <= 1)
            return Array.Empty<TimelineTriggerSO>();

        TimelineTriggerSO trigger = LoadOrCreate<TimelineTriggerSO>($"{TranslationFolder}/Trigger_TranslationNotice.asset", written);
        trigger.id = TranslationNoticeId;
        trigger.displayName = "Translation notice";
        trigger.description = "Generated by Generate World: announces in the morning paper the first day foreign papers and speech reach the desk untranslated.";
        trigger.oneShot = true;
        trigger.newsLineOnFire = t.announce;
        trigger.conditions = DayGate(t.fromDay, Gates.UnlockNight(t.fromDay)).ToList();
        trigger.outcomes = new List<TriggerOutcome>();
        EditorUtility.SetDirty(trigger);
        return new[] { trigger };
    }

    /// <summary>"near_east" as "NearEast" (asset names).</summary>
    private static string Pascal(string id) =>
        string.Concat((id ?? string.Empty).Split('_').Where(w => w.Length > 0).Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));

    /// <summary>"A", "A and B", "A, B and C".</summary>
    private static string JoinNames(IReadOnlyList<string> names) =>
        names.Count <= 1 ? string.Concat(names) : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];

    // -----------------------------
    // Source file shape (JsonUtility)
    // -----------------------------

    /// <summary>world_source.json "translation" (piece 9).</summary>
    [Serializable] private sealed class TranslationData
    {
        public int fromDay;
        public string announce;
        public FlipTiming flip;
        public string fallbackGlyphs;
        public ScriptData[] scripts;
        public PackData[] packs;
        public Tongue[] tongues;
    }

    /// <summary>A script: its direction and font candidates (piece 6's font shape).</summary>
    [Serializable] private sealed class ScriptData { public string id; public bool rightToLeft; public FontData[] fonts; }

    /// <summary>A translator pack and the prices of its two upgrades.</summary>
    [Serializable] private sealed class PackData { public string id; public string displayName; public int writtenCost; public int spokenCost; }
}
