using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generate World's translation part (piece 9; speech only since the
/// redesign's phase 1: papers are always English): checks world_source.json
/// "translation" and every place's tongue, writes one Speech translator
/// upgrade per pack (an Orders node in the Interview band, its prerequisites
/// from the pack's "requires": Saleh 2026-09-29) and the one-shot notice
/// trigger (as many days ahead as the translator's chain of orders) into the
/// owned Assets/Data/World/Translation (piece 9's Papers translators are no
/// longer listed, so the folder's pruning moves them to the trash), and the
/// library's TranslationSettings.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>Folder of the generated translation assets (the Speech translators, the notice trigger).</summary>
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
    /// one, the Orders tree with the translators in it (UpgradeTree.Problems:
    /// a pack's requires must name an Orders upgrade, no cycle), an ASCII
    /// notice when translation starts after day 1 with room before fromDay for
    /// the translator's chain of orders (Translation.NoticeProblem), and the
    /// UI strings it reads.
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
            if (pack != null && pack.spokenCost < 0)
                errors.Add($"translation.packs: pack '{pack.id}' has a cost below 0.");

        var generated = new HashSet<string>((t.packs ?? Array.Empty<PackData>()).Where(p => p != null).Select(p => Translation.UpgradeId(p.id)));
        if (authored.library != null)
            foreach (UpgradeSO u in HandAuthored(new SerializedObject(authored.library), "upgrades").OfType<UpgradeSO>())
                if (generated.Contains(u.id))
                    errors.Add($"Upgrade '{AssetDatabase.GetAssetPath(u)}' has the id '{u.id}', which Generate World writes for a translator.");

        List<TreeNode> catalogue = CatalogueNodes(authored, t);
        foreach (string problem in UpgradeTree.Problems(catalogue))
            errors.Add("The Orders tree with the translation.packs translators: " + problem);
        string timing = Translation.NoticeProblem(t.fromDay, TranslatorOrders(catalogue, t));
        if (timing != null)
            errors.Add(timing);

        if (t.fromDay > 1 && (string.IsNullOrWhiteSpace(t.announce) || !IsAscii(t.announce)))
            errors.Add("translation.announce must be non-blank ASCII (the morning paper announces foreign speech, as many days ahead as the translator's chain of orders).");

        var keys = new HashSet<string>((src.ui?.strings ?? Array.Empty<StringData>()).Where(s => s != null).Select(s => s.key));
        foreach (string key in TranslationKeys)
            RequireKey(keys, key, "translation (piece 9)", errors);
    }

    /// <summary>The library's translation settings from the source (tongues and the key-word rule copied as authored; packs without their costs).</summary>
    private static TranslationSettings BuildTranslation(TranslationData t) => new TranslationSettings
    {
        rules = new TranslationRules
        {
            fromDay = t.fromDay,
            tongues = (t.tongues ?? Array.Empty<Tongue>()).ToList(),
            packs = (t.packs ?? Array.Empty<PackData>()).Select(p => p == null ? null : new TranslatorPack { id = p.id, displayName = p.displayName }).ToList(),
            keyWords = t.keyWords
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
    /// Writes Translation/Upgrade_Tr_{Pack}_Speech.asset: the pack's Speech
    /// translator (id Translation.UpgradeId, "{Pack} Translator: Speech", a
    /// description naming its tongues, the pack's cost, no unlock effect),
    /// sold in the Orders app in the Interview band, after the pack's
    /// "requires" (Saleh 2026-09-29: Interview Protocols), with no install slot.
    /// </summary>
    private static UpgradeSO MakeTranslator(PackData pack, TranslationData t, HashSet<string> written)
    {
        UpgradeSO so = LoadOrCreate<UpgradeSO>($"{TranslationFolder}/Upgrade_Tr_{Pascal(pack.id)}_Speech.asset", written);
        string tongues = JoinNames((t.tongues ?? Array.Empty<Tongue>()).Where(g => g != null && g.pack == pack.id).Select(g => g.displayName).ToList());
        so.id = Translation.UpgradeId(pack.id);
        so.displayName = $"{pack.displayName} Translator: Speech";
        so.description = $"Translates {tongues} in speech.";
        so.cost = pack.spokenCost;
        so.unlockEffect = null;
        so.venue = UpgradeVenue.Orders;
        so.branch = UpgradeBranch.Interview;
        so.requires = pack.requires ?? Array.Empty<string>();
        so.installSlot = string.Empty;
        EditorUtility.SetDirty(so);
        return so;
    }

    /// <summary>The catalogue as the tree reads it once this run writes the translators: the library's hand-authored upgrades, then one Orders node per pack (Interview, its cost and requires).</summary>
    private static List<TreeNode> CatalogueNodes(Authored authored, TranslationData t)
    {
        var nodes = new List<TreeNode>();
        if (authored.library != null)
            foreach (UpgradeSO u in HandAuthored(new SerializedObject(authored.library), "upgrades").OfType<UpgradeSO>())
                nodes.Add(u.Node);
        foreach (PackData p in t.packs ?? Array.Empty<PackData>())
            if (p != null)
                nodes.Add(new TreeNode(Translation.UpgradeId(p.id), UpgradeVenue.Orders, UpgradeBranch.Interview, p.spokenCost, p.requires));
        return nodes;
    }

    /// <summary>The orders a Speech translator takes from nothing owned, one a day (the longest of the packs' chains, UpgradeTree.ChainLength; at least 1): the notice runs that many days ahead.</summary>
    private static int TranslatorOrders(IReadOnlyList<TreeNode> catalogue, TranslationData t) =>
        Math.Max(1, (t.packs ?? Array.Empty<PackData>()).Where(p => p != null).Select(p => UpgradeTree.ChainLength(Translation.UpgradeId(p.id), catalogue)).DefaultIfEmpty(1).Max());

    /// <summary>
    /// Writes Translation/Trigger_TranslationNotice.asset when foreign speech
    /// starts after day 1: a one-shot trigger whose news line is the notice,
    /// firing so that the paper <paramref name="orders"/> days before fromDay
    /// carries it (DayAtLeast Translation.NoticeNight: one day per order in
    /// the translator's chain; with Interview Protocols first, day 3's paper
    /// for speech from day 5), so a clerk who reads it and orders the chain
    /// one link a day has a translator in force when foreign speech reaches
    /// the desk (traveller types I3). None when fromDay is 1.
    /// </summary>
    private static TimelineTriggerSO[] MakeTranslationNotice(TranslationData t, int orders, HashSet<string> written)
    {
        if (t.fromDay <= 1)
            return Array.Empty<TimelineTriggerSO>();

        TimelineTriggerSO trigger = LoadOrCreate<TimelineTriggerSO>($"{TranslationFolder}/Trigger_TranslationNotice.asset", written);
        trigger.id = TranslationNoticeId;
        trigger.displayName = "Translation notice";
        trigger.description = "Generated by Generate World: announces in the morning paper the first day foreign speech reaches the desk untranslated.";
        trigger.oneShot = true;
        trigger.newsLineOnFire = t.announce;
        trigger.conditions = DayGate(t.fromDay, Translation.NoticeNight(t.fromDay, orders)).ToList();
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

    /// <summary>world_source.json "translation" (piece 9; keyWords: the traveller-types spec's §8.1).</summary>
    [Serializable] private sealed class TranslationData
    {
        public int fromDay;
        public string announce;
        public FlipTiming flip;
        public KeyWordRule keyWords;
        public string fallbackGlyphs;
        public ScriptData[] scripts;
        public PackData[] packs;
        public Tongue[] tongues;
    }

    /// <summary>A script: its direction and font candidates (piece 6's font shape).</summary>
    [Serializable] private sealed class ScriptData { public string id; public bool rightToLeft; public FontData[] fonts; }

    /// <summary>A translator pack and the price of its Speech translator.</summary>
    [Serializable] private sealed class PackData { public string id; public string displayName; public int spokenCost; public string[] requires; }
}
