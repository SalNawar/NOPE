using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Tools > TimeDesk > Generate World. The one authoritative content generator:
/// reads the researched world (Assets/Data/World/world_source.json) and
/// creates or updates the eras, nations, 40 places (NationEraProfileSO with
/// facts, names and birth years), travel rules and the three day plans, then
/// sets every world array of ContentLibrary_Main explicitly. It also deletes the
/// retired made-up world and the unreachable Phase 7 content. Idempotent:
/// re-running converges to the source file.
/// </summary>
public static class WorldContentGenerator
{
    /// <summary>The researched world data.</summary>
    private const string SourcePath = "Assets/Data/World/world_source.json";

    /// <summary>Folder for generated world assets.</summary>
    private const string WorldRoot = "Assets/Data/World";

    /// <summary>The single content library.</summary>
    private const string LibraryPath = "Assets/Data/Content Library/ContentLibrary_Main.asset";

    /// <summary>The investigation case blueprint (kept; its archetype pool is set here).</summary>
    private const string BlueprintPath = "Assets/Data/Investigation/CaseBlueprint_Investigation.asset";

    /// <summary>Day plans live here (kept assets: OfficeScene falls back to Day 1).</summary>
    private const string DayPlanRoot = "Assets/Data/Investigation";

    /// <summary>Visitor archetypes (their impacts target Democracy / Science / Art).</summary>
    private static readonly string[] ArchetypePaths =
    {
        "Assets/Data/Archetypes/Archetype_Artist.asset",
        "Assets/Data/Archetypes/Archetype_Diplomat.asset",
        "Assets/Data/Archetypes/Archetype_Merchant.asset",
        "Assets/Data/Archetypes/Archetype_Scientist.asset",
        "Assets/Data/Archetypes/Archetype_Soldier.asset",
        "Assets/Data/Archetypes/Archetype_Wanderer.asset",
    };

    /// <summary>Timeline attributes by source id (the endings reference these assets).</summary>
    private static readonly (string id, string path)[] AttributePaths =
    {
        ("democracy", "Assets/Data/Attributes/Attr_Democracy.asset"),
        ("science", "Assets/Data/Attributes/Attr_Science.asset"),
        ("art", "Assets/Data/Attributes/Attr_Art.asset"),
    };

    /// <summary>Reference-book covers (their stale row data is dropped on re-save).</summary>
    private static readonly string[] BookPaths =
    {
        "Assets/Data/Investigation/RefBook_Currency.asset",
        "Assets/Data/Investigation/RefBook_Language.asset",
        "Assets/Data/Investigation/RefBook_Technology.asset",
    };

    /// <summary>Retired single assets: the made-up world (nations, eras, places, attributes, archetypes, rules).</summary>
    private static readonly string[] RetiredAssets =
    {
        "Assets/Data/Investigation/Archetype_Diplomat.asset",
        "Assets/Data/Investigation/Archetype_Merchant.asset",
        "Assets/Data/Investigation/Archetype_Mystic.asset",
        "Assets/Data/Investigation/Archetype_Warrior.asset",
        "Assets/Data/Investigation/Attr_Industry.asset",
        "Assets/Data/Investigation/Attr_Militarism.asset",
        "Assets/Data/Investigation/Attr_Mysticism.asset",
        "Assets/Data/Investigation/Attr_Philosophy.asset",
        "Assets/Data/Investigation/Era_Future.asset",
        "Assets/Data/Investigation/Era_Medieval.asset",
        "Assets/Data/Investigation/Era_Rome.asset",
        "Assets/Data/Investigation/Nation_Aegyptus.asset",
        "Assets/Data/Investigation/Nation_Albion.asset",
        "Assets/Data/Investigation/Nation_Helios.asset",
        "Assets/Data/Investigation/Nation_Latia.asset",
        "Assets/Data/Investigation/Nation_Norvik.asset",
        "Assets/Data/Investigation/Nation_Solaris.asset",
        "Assets/Data/Investigation/Profile_Aegyptus_Rome.asset",
        "Assets/Data/Investigation/Profile_Albion_Med.asset",
        "Assets/Data/Investigation/Profile_Helios_Fut.asset",
        "Assets/Data/Investigation/Profile_Latia_Rome.asset",
        "Assets/Data/Investigation/Profile_Norvik_Med.asset",
        "Assets/Data/Investigation/Profile_Solaris_Fut.asset",
        "Assets/Data/Investigation/Rule_NoAegyptus.asset",
        "Assets/Data/Investigation/Rule_NoFuture.asset",
        "Assets/Data/Investigation/Rule_NoHeliosFuture.asset",
    };

    /// <summary>
    /// Retired folders: the Phase 7 country-named eras, nations, profiles and
    /// legendaries, the legacy clue path and the shadowed legacy day plans.
    /// </summary>
    private static readonly string[] RetiredFolders =
    {
        "Assets/Data/Eras",
        "Assets/Data/Nations",
        "Assets/Data/Profiles",
        "Assets/Data/Legendaries",
        "Assets/Data/Clues",
        "Assets/Data/Documents",
        "Assets/Data/Case Blueprints",
        "Assets/Data/Dayplan",
    };

    /// <summary>Retired Phase 7 tier effects (named Effect_Dom_* / Effect_Sup_*; places have no tier effects).</summary>
    private const string EffectsFolder = "Assets/Data/Effects";

    [MenuItem("Tools/TimeDesk/Generate World")]
    public static void Generate()
    {
        WorldSource src = LoadSource();
        if (src == null)
            return;

        var attributes = new Dictionary<string, AttributeSO>();
        foreach ((string id, string path) in AttributePaths)
        {
            var attr = AssetDatabase.LoadAssetAtPath<AttributeSO>(path);
            if (attr == null)
            {
                Debug.LogError($"[WorldContentGenerator] Missing attribute asset '{path}'. Aborting.");
                return;
            }
            attributes[id] = attr;
        }

        ArchetypeSO[] archetypes = ArchetypePaths.Select(AssetDatabase.LoadAssetAtPath<ArchetypeSO>).Where(a => a != null).ToArray();
        ReferenceBookSO[] books = BookPaths.Select(AssetDatabase.LoadAssetAtPath<ReferenceBookSO>).Where(b => b != null).ToArray();
        var blueprint = AssetDatabase.LoadAssetAtPath<CaseBlueprintSO>(BlueprintPath);
        if (blueprint == null)
        {
            Debug.LogError($"[WorldContentGenerator] Missing case blueprint '{BlueprintPath}'. Aborting.");
            return;
        }

        EnsureFolder($"{WorldRoot}/Eras");
        EnsureFolder($"{WorldRoot}/Nations");
        EnsureFolder($"{WorldRoot}/Places");
        EnsureFolder($"{WorldRoot}/Rules");

        // --- Eras and nations ---
        var eras = new Dictionary<string, EraSO>();
        foreach (EraData e in src.eras)
            eras[e.id] = MakeEra(e);

        var nations = new List<NationSO>();
        var nationsById = new Dictionary<string, NationSO>();
        foreach (CountryData c in src.countries)
        {
            NationSO n = MakeNation(c);
            nations.Add(n);
            nationsById[c.id] = n;
        }

        // --- Places ---
        var places = new List<NationEraProfileSO>();
        foreach (PlaceData p in src.places)
        {
            if (!nationsById.TryGetValue(p.country, out NationSO nation) || !eras.TryGetValue(p.era, out EraSO era))
            {
                Debug.LogError($"[WorldContentGenerator] Place '{p.displayName}' references unknown country '{p.country}' or era '{p.era}'. Skipped.");
                continue;
            }

            CountryData country = src.countries.First(c => c.id == p.country);
            places.Add(MakePlace(p, nation, era, country, attributes, src.travellerAgeMin, src.travellerAgeMax));
        }

        // --- Rules, blueprint, day plans ---
        var rules = new Dictionary<string, TravelRuleSO>();
        foreach (RuleData r in src.rules)
            rules[r.asset] = MakeRule(r, nationsById, eras);

        var soBlueprint = new SerializedObject(blueprint);
        SetArray(soBlueprint, "archetypePool", archetypes);
        soBlueprint.ApplyModifiedProperties();
        EditorUtility.SetDirty(blueprint);

        var days = src.days.Select(d => MakeDay(d, blueprint, eras, nationsById, rules)).ToArray();

        // Re-saving the book covers drops their retired row data from the YAML.
        foreach (ReferenceBookSO book in books)
            EditorUtility.SetDirty(book);

        // --- Retire the made-up world, then wire the library ---
        int retired = Retire();
        WireLibrary(days, src.eras.Select(e => eras[e.id]).ToArray(), nations.ToArray(), places.ToArray(),
                    archetypes, AttributePaths.Select(a => attributes[a.id]).ToArray(), books);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WorldContentGenerator] World generated: {eras.Count} eras, {nations.Count} nations, {places.Count} places, {rules.Count} rules, {days.Length} day plans; {retired} retired asset(s) deleted.");
    }

    // -----------------------------
    // Builders
    // -----------------------------

    private static EraSO MakeEra(EraData e)
    {
        EraSO era = LoadOrCreate<EraSO>($"{WorldRoot}/Eras/Era_{Pascal(e.displayName)}.asset");
        era.id = e.id;
        era.displayName = e.displayName;
        era.order = e.order;
        EditorUtility.SetDirty(era);
        return era;
    }

    private static NationSO MakeNation(CountryData c)
    {
        NationSO nation = LoadOrCreate<NationSO>($"{WorldRoot}/Nations/Nation_{Pascal(c.displayName)}.asset");
        nation.id = c.id;
        nation.displayName = c.displayName;
        nation.namePool = Array.Empty<string>(); // names live on each place
        EditorUtility.SetDirty(nation);
        return nation;
    }

    private static NationEraProfileSO MakePlace(PlaceData p, NationSO nation, EraSO era, CountryData country,
                                                Dictionary<string, AttributeSO> attributes, int ageMin, int ageMax)
    {
        NationEraProfileSO place = LoadOrCreate<NationEraProfileSO>($"{WorldRoot}/Places/Place_{p.country}_{p.era}.asset");
        place.id = $"{p.country}_{p.era}";
        place.displayName = p.displayName;
        place.nation = nation;
        place.era = era;
        place.moment = p.moment;
        place.year = p.year;
        place.birthYearMin = p.year - ageMax;
        place.birthYearMax = p.year - ageMin;
        place.maleNames = p.maleNames ?? Array.Empty<string>();
        place.femaleNames = p.femaleNames ?? Array.Empty<string>();

        place.facts = new List<ProfileFact>();
        foreach (FactData f in p.facts)
        {
            if (Enum.TryParse(f.category, out ClueCategory category))
                place.facts.Add(new ProfileFact { category = category, value = f.value });
            else
                Debug.LogError($"[WorldContentGenerator] Place '{p.displayName}' has unknown fact category '{f.category}'.", place);
        }

        // Starting attribute scores follow the country's theme. No tier effects:
        // 40 places x tiers would flood the morning paper (history is piece 5).
        place.baselines = new List<AttributeBaseline>();
        foreach (BaselineData b in country.baselines)
        {
            if (attributes.TryGetValue(b.attribute, out AttributeSO attr))
                place.baselines.Add(new AttributeBaseline { attribute = attr, baseScore = b.score });
        }

        EditorUtility.SetDirty(place);
        return place;
    }

    private static TravelRuleSO MakeRule(RuleData r, Dictionary<string, NationSO> nations, Dictionary<string, EraSO> eras)
    {
        TravelRuleSO rule = LoadOrCreate<TravelRuleSO>($"{WorldRoot}/Rules/{r.asset}.asset");
        rule.type = (TravelRuleType)Enum.Parse(typeof(TravelRuleType), r.type);
        rule.nation = !string.IsNullOrEmpty(r.country) && nations.TryGetValue(r.country, out NationSO n) ? n : null;
        rule.era = !string.IsNullOrEmpty(r.era) && eras.TryGetValue(r.era, out EraSO e) ? e : null;
        rule.description = r.description;
        EditorUtility.SetDirty(rule);
        return rule;
    }

    private static DayPlanSO MakeDay(DayData d, CaseBlueprintSO blueprint, Dictionary<string, EraSO> eras,
                                     Dictionary<string, NationSO> nations, Dictionary<string, TravelRuleSO> rules)
    {
        DayPlanSO plan = LoadOrCreate<DayPlanSO>($"{DayPlanRoot}/{d.asset}.asset");
        var so = new SerializedObject(plan);
        so.FindProperty("dayNumber").intValue = d.day;
        so.FindProperty("visitorsCount").intValue = d.queue;
        so.FindProperty("legendaryBaseChance").floatValue = 0f;
        SetArray(so, "possibleBlueprints", new Object[] { blueprint });
        SetArray(so, "availableLegendaries", Array.Empty<Object>());
        SetArray(so, "allowedNations", d.countries.Select(c => (Object)nations[c]).ToArray());
        SetArray(so, "activeTravelRules", d.rules.Select(r => (Object)rules[r]).ToArray());

        SerializedProperty weights = so.FindProperty("eraWeights");
        weights.arraySize = d.eras.Length;
        for (int i = 0; i < d.eras.Length; i++)
        {
            SerializedProperty el = weights.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("era").objectReferenceValue = eras[d.eras[i].era];
            el.FindPropertyRelative("weight").floatValue = d.eras[i].weight;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(plan);
        return plan;
    }

    /// <summary>Deletes the retired assets and folders (no-op once gone). Returns how many were deleted.</summary>
    private static int Retire()
    {
        int deleted = 0;

        foreach (string path in RetiredAssets)
            if (AssetDatabase.LoadMainAssetAtPath(path) != null && AssetDatabase.DeleteAsset(path))
                deleted++;

        foreach (string folder in RetiredFolders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                continue;

            deleted += AssetDatabase.FindAssets(string.Empty, new[] { folder }).Length;
            AssetDatabase.DeleteAsset(folder);
        }

        if (AssetDatabase.IsValidFolder(EffectsFolder))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EffectSO", new[] { EffectsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                if ((file.StartsWith("Effect_Dom_") || file.StartsWith("Effect_Sup_")) && AssetDatabase.DeleteAsset(path))
                    deleted++;
            }
        }

        return deleted;
    }

    /// <summary>Sets every world array of the library (authoritative), keeping the rest.</summary>
    private static void WireLibrary(DayPlanSO[] days, EraSO[] eras, NationSO[] nations, NationEraProfileSO[] places,
                                    ArchetypeSO[] archetypes, AttributeSO[] attributes, ReferenceBookSO[] books)
    {
        var lib = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>(LibraryPath);
        if (lib == null)
        {
            Debug.LogError($"[WorldContentGenerator] Missing content library '{LibraryPath}'.");
            return;
        }

        var so = new SerializedObject(lib);
        SetArray(so, "dayPlans", days);
        SetArray(so, "eras", eras);
        SetArray(so, "nations", nations);
        SetArray(so, "nationEraProfiles", places);
        SetArray(so, "archetypes", archetypes);
        SetArray(so, "attributes", attributes);
        SetArray(so, "referenceBooks", books);
        SetArray(so, "clues", Array.Empty<Object>());
        SetArray(so, "legendaries", Array.Empty<Object>());
        DropMissing(so, "effects");
        DropMissing(so, "timelineTriggers");
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    private static WorldSource LoadSource()
    {
        string full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, SourcePath);
        if (!File.Exists(full))
        {
            Debug.LogError($"[WorldContentGenerator] Missing '{SourcePath}'.");
            return null;
        }

        WorldSource src = JsonUtility.FromJson<WorldSource>(File.ReadAllText(full));
        if (src == null || src.eras == null || src.countries == null || src.places == null || src.days == null || src.rules == null)
        {
            Debug.LogError($"[WorldContentGenerator] '{SourcePath}' is malformed.");
            return null;
        }
        return src;
    }

    /// <summary>"Early modern" -> "EarlyModern".</summary>
    private static string Pascal(string words) =>
        string.Concat(words.Split(' ').Where(w => w.Length > 0).Select(w => char.ToUpperInvariant(w[0]) + w.Substring(1)));

    private static void SetArray(SerializedObject so, string prop, IReadOnlyList<Object> values)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
        {
            Debug.LogError($"[WorldContentGenerator] '{so.targetObject.name}' has no serialized field '{prop}'.");
            return;
        }
        p.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    /// <summary>Removes null (deleted) references from an object array property.</summary>
    private static void DropMissing(SerializedObject so, string prop)
    {
        SerializedProperty p = so.FindProperty(prop);
        if (p == null)
            return;

        var kept = new List<Object>();
        for (int i = 0; i < p.arraySize; i++)
        {
            Object o = p.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o != null)
                kept.Add(o);
        }
        SetArray(so, prop, kept);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
            return asset;

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    // -----------------------------
    // Source file shape (JsonUtility)
    // -----------------------------

    [Serializable] private sealed class WorldSource
    {
        public int travellerAgeMin = 18;
        public int travellerAgeMax = 70;
        public EraData[] eras;
        public CountryData[] countries;
        public PlaceData[] places;
        public RuleData[] rules;
        public DayData[] days;
    }

    [Serializable] private sealed class EraData { public string id; public string displayName; public int order; }

    [Serializable] private sealed class CountryData { public string id; public string displayName; public BaselineData[] baselines; }

    [Serializable] private sealed class BaselineData { public string attribute; public float score; }

    [Serializable] private sealed class FactData { public string category; public string value; }

    [Serializable] private sealed class PlaceData
    {
        public string country;
        public string era;
        public string displayName;
        public string moment;
        public int year;
        public FactData[] facts;
        public string[] maleNames;
        public string[] femaleNames;
    }

    [Serializable] private sealed class RuleData { public string asset; public string type; public string country; public string era; public string description; }

    [Serializable] private sealed class EraWeightData { public string era; public float weight; }

    [Serializable] private sealed class DayData
    {
        public string asset;
        public int day;
        public int queue;
        public EraWeightData[] eras;
        public string[] countries;
        public string[] rules;
    }
}
