using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Tools > TimeDesk > Generate World. The one authoritative world generator:
/// reads the researched world (Assets/Data/World/world_source.json) and
/// creates or updates the eras, nations, places (NationEraProfileSO with facts,
/// names and birth years), travel rules and day plans, points the case
/// blueprint at the listed archetypes, then sets every world array of the
/// content library explicitly. Idempotent: re-running converges to the source
/// file. It owns the Eras/Nations/Places/Rules folders under Assets/Data/World
/// (assets there that the source no longer lists go to the OS trash) and only
/// drops missing references elsewhere, so hand-authored content (legendaries,
/// effects, triggers) survives a re-run. The authored assets the source points
/// at (library, blueprint, attributes, archetypes, books) must already exist;
/// every reference is checked before anything is written.
/// </summary>
public static class WorldContentGenerator
{
    /// <summary>The researched world data.</summary>
    private const string SourcePath = "Assets/Data/World/world_source.json";

    /// <summary>Folder for generated world assets.</summary>
    private const string WorldRoot = "Assets/Data/World";

    /// <summary>Generator-owned folders (under <see cref="WorldRoot"/>).</summary>
    private static readonly string[] OwnedFolders = { "Eras", "Nations", "Places", "Rules" };

    /// <summary>Reads the source, checks every reference, then writes the world. Aborts (writing nothing) on any error.</summary>
    [MenuItem("Tools/TimeDesk/Generate World")]
    public static void Generate()
    {
        WorldSource src = LoadSource();
        if (src == null)
            return;

        var errors = new List<string>();
        Authored authored = LoadAuthored(src.content, errors);
        CheckReferences(src, authored, errors);
        if (errors.Count > 0)
        {
            foreach (string e in errors)
                Debug.LogError($"[WorldContentGenerator] {e}");
            Debug.LogError($"[WorldContentGenerator] Aborted with {errors.Count} error(s); nothing was changed.");
            return;
        }

        foreach (string folder in OwnedFolders)
            EnsureFolder($"{WorldRoot}/{folder}");

        var written = new HashSet<string>();

        // --- Eras and nations ---
        var eras = src.eras.ToDictionary(e => e.id, e => MakeEra(e, written));
        var nations = src.countries.ToDictionary(c => c.id, c => MakeNation(c, written));

        // --- Places ---
        var places = src.places
            .Select(p => MakePlace(p, nations[p.country], eras[p.era], src.countries.First(c => c.id == p.country),
                                   authored.attributes, src.travellerAgeMin, src.travellerAgeMax, written))
            .ToArray();

        // --- Rules, blueprint, day plans ---
        var rules = src.rules.ToDictionary(r => r.asset, r => MakeRule(r, nations, eras, written));

        var soBlueprint = new SerializedObject(authored.blueprint);
        SetArray(soBlueprint, "archetypePool", authored.archetypes);
        soBlueprint.ApplyModifiedProperties();
        EditorUtility.SetDirty(authored.blueprint);

        DayPlanSO[] days = src.days.Select(d => MakeDay(d, src.content.dayPlanFolder, authored.blueprint, eras, nations, rules)).ToArray();

        // Re-saving the book covers keeps their YAML in the current shape.
        foreach (ReferenceBookSO book in authored.books)
            EditorUtility.SetDirty(book);

        WireLibrary(authored.library, days, src.eras.Select(e => eras[e.id]).ToArray(), src.countries.Select(c => nations[c.id]).ToArray(),
                    places, authored.archetypes, src.content.attributes.Select(a => authored.attributes[a.id]).ToArray(), authored.books);

        int pruned = PruneOwnedFolders(written);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WorldContentGenerator] World generated: {eras.Count} eras, {nations.Count} nations, {places.Length} places, {rules.Count} rules, {days.Length} day plans; {pruned} unlisted generated asset(s) moved to the trash.");
    }

    // -----------------------------
    // Checks (nothing is written until these pass)
    // -----------------------------

    /// <summary>The authored assets the source points at.</summary>
    private sealed class Authored
    {
        public ContentLibrarySO library;
        public CaseBlueprintSO blueprint;
        public Dictionary<string, AttributeSO> attributes = new Dictionary<string, AttributeSO>();
        public ArchetypeSO[] archetypes;
        public ReferenceBookSO[] books;
    }

    private static Authored LoadAuthored(ContentData content, List<string> errors)
    {
        var a = new Authored();
        if (content == null)
        {
            errors.Add($"'{SourcePath}' has no \"content\" section (library, blueprint, dayPlanFolder, attributes, archetypes, books).");
            return a;
        }

        a.library = Require<ContentLibrarySO>(content.library, "content library", errors);
        a.blueprint = Require<CaseBlueprintSO>(content.blueprint, "case blueprint", errors);
        if (!AssetDatabase.IsValidFolder(content.dayPlanFolder ?? string.Empty))
            errors.Add($"Day plan folder '{content.dayPlanFolder}' does not exist.");

        foreach (AttributeData attr in content.attributes ?? Array.Empty<AttributeData>())
        {
            var so = Require<AttributeSO>(attr.asset, $"attribute '{attr.id}'", errors);
            if (so != null)
                a.attributes[attr.id] = so;
        }

        a.archetypes = (content.archetypes ?? Array.Empty<string>()).Select(p => Require<ArchetypeSO>(p, "archetype", errors)).Where(x => x != null).ToArray();
        a.books = (content.books ?? Array.Empty<string>()).Select(p => Require<ReferenceBookSO>(p, "reference book", errors)).Where(x => x != null).ToArray();
        return a;
    }

    private static T Require<T>(string path, string what, List<string> errors) where T : Object
    {
        T asset = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            errors.Add($"Missing {what} at '{path}'.");
        return asset;
    }

    /// <summary>Every id the source uses must resolve; enum strings must parse.</summary>
    private static void CheckReferences(WorldSource src, Authored authored, List<string> errors)
    {
        var eraIds = new HashSet<string>(src.eras.Select(e => e.id));
        var countryIds = new HashSet<string>(src.countries.Select(c => c.id));
        var ruleIds = new HashSet<string>(src.rules.Select(r => r.asset));

        if (eraIds.Count != src.eras.Length || eraIds.Any(string.IsNullOrWhiteSpace))
            errors.Add("Era ids must be unique and non-blank.");
        if (countryIds.Count != src.countries.Length || countryIds.Any(string.IsNullOrWhiteSpace))
            errors.Add("Country ids must be unique and non-blank.");
        if (ruleIds.Count != src.rules.Length || ruleIds.Any(string.IsNullOrWhiteSpace))
            errors.Add("Rule asset names must be unique and non-blank.");

        foreach (CountryData c in src.countries)
            foreach (BaselineData b in c.baselines ?? Array.Empty<BaselineData>())
                if (!authored.attributes.ContainsKey(b.attribute))
                    errors.Add($"Country '{c.id}' has a baseline for unknown attribute '{b.attribute}'.");

        var placeKeys = new HashSet<string>();
        foreach (PlaceData p in src.places)
        {
            if (!countryIds.Contains(p.country) || !eraIds.Contains(p.era))
                errors.Add($"Place '{p.displayName}' references unknown country '{p.country}' or era '{p.era}'.");
            if (!placeKeys.Add($"{p.country}_{p.era}"))
                errors.Add($"Place '{p.country}_{p.era}' is listed twice.");
            foreach (FactData f in p.facts ?? Array.Empty<FactData>())
                if (!Enum.TryParse(f.category, out ClueCategory _))
                    errors.Add($"Place '{p.displayName}' has unknown fact category '{f.category}'.");
        }

        foreach (RuleData r in src.rules)
        {
            if (!Enum.TryParse(r.type, out TravelRuleType _))
                errors.Add($"Rule '{r.asset}' has unknown type '{r.type}'.");
            if (!string.IsNullOrEmpty(r.country) && !countryIds.Contains(r.country))
                errors.Add($"Rule '{r.asset}' references unknown country '{r.country}'.");
            if (!string.IsNullOrEmpty(r.era) && !eraIds.Contains(r.era))
                errors.Add($"Rule '{r.asset}' references unknown era '{r.era}'.");
        }

        foreach (DayData d in src.days)
        {
            foreach (EraWeightData w in d.eras ?? Array.Empty<EraWeightData>())
                if (!eraIds.Contains(w.era))
                    errors.Add($"Day '{d.asset}' weights unknown era '{w.era}'.");
            foreach (string c in d.countries ?? Array.Empty<string>())
                if (!countryIds.Contains(c))
                    errors.Add($"Day '{d.asset}' allows unknown country '{c}'.");
            foreach (string r in d.rules ?? Array.Empty<string>())
                if (!ruleIds.Contains(r))
                    errors.Add($"Day '{d.asset}' uses unknown rule '{r}'.");
        }
    }

    // -----------------------------
    // Builders
    // -----------------------------

    private static EraSO MakeEra(EraData e, HashSet<string> written)
    {
        EraSO era = LoadOrCreate<EraSO>($"{WorldRoot}/Eras/Era_{e.id}.asset", written);
        era.id = e.id;
        era.displayName = e.displayName;
        era.order = e.order;
        EditorUtility.SetDirty(era);
        return era;
    }

    private static NationSO MakeNation(CountryData c, HashSet<string> written)
    {
        NationSO nation = LoadOrCreate<NationSO>($"{WorldRoot}/Nations/Nation_{c.id}.asset", written);
        nation.id = c.id;
        nation.displayName = c.displayName;
        EditorUtility.SetDirty(nation);
        return nation;
    }

    private static NationEraProfileSO MakePlace(PlaceData p, NationSO nation, EraSO era, CountryData country,
                                                Dictionary<string, AttributeSO> attributes, int ageMin, int ageMax,
                                                HashSet<string> written)
    {
        NationEraProfileSO place = LoadOrCreate<NationEraProfileSO>($"{WorldRoot}/Places/Place_{p.country}_{p.era}.asset", written);
        place.id = $"{p.country}_{p.era}";
        place.displayName = p.displayName;
        place.nation = nation;
        place.era = era;
        place.birthYearMin = p.year - ageMax;
        place.birthYearMax = p.year - ageMin;
        place.maleNames = p.maleNames ?? Array.Empty<string>();
        place.femaleNames = p.femaleNames ?? Array.Empty<string>();

        place.facts = (p.facts ?? Array.Empty<FactData>())
            .Select(f => new ProfileFact { category = (ClueCategory)Enum.Parse(typeof(ClueCategory), f.category), value = f.value })
            .ToList();

        // Starting attribute scores follow the country's theme. No tier effects:
        // 40 places x tiers would flood the morning paper (history is piece 5).
        place.baselines = (country.baselines ?? Array.Empty<BaselineData>())
            .Select(b => new AttributeBaseline { attribute = attributes[b.attribute], baseScore = b.score })
            .ToList();

        EditorUtility.SetDirty(place);
        return place;
    }

    private static TravelRuleSO MakeRule(RuleData r, Dictionary<string, NationSO> nations, Dictionary<string, EraSO> eras, HashSet<string> written)
    {
        TravelRuleSO rule = LoadOrCreate<TravelRuleSO>($"{WorldRoot}/Rules/{r.asset}.asset", written);
        rule.type = (TravelRuleType)Enum.Parse(typeof(TravelRuleType), r.type);
        rule.nation = !string.IsNullOrEmpty(r.country) ? nations[r.country] : null;
        rule.era = !string.IsNullOrEmpty(r.era) ? eras[r.era] : null;
        rule.description = r.description;
        EditorUtility.SetDirty(rule);
        return rule;
    }

    /// <summary>
    /// Writes the day's queue, eras, countries and rules. Legendary settings are
    /// left to their authors (missing legendaries are dropped).
    /// </summary>
    private static DayPlanSO MakeDay(DayData d, string folder, CaseBlueprintSO blueprint, Dictionary<string, EraSO> eras,
                                     Dictionary<string, NationSO> nations, Dictionary<string, TravelRuleSO> rules)
    {
        DayPlanSO plan = LoadOrCreate<DayPlanSO>($"{folder}/{d.asset}.asset", null);
        var so = new SerializedObject(plan);
        so.FindProperty("dayNumber").intValue = d.day;
        so.FindProperty("visitorsCount").intValue = d.queue;
        SetArray(so, "possibleBlueprints", new Object[] { blueprint });
        DropMissing(so, "availableLegendaries");
        SetArray(so, "allowedNations", (d.countries ?? Array.Empty<string>()).Select(c => (Object)nations[c]).ToArray());
        SetArray(so, "activeTravelRules", (d.rules ?? Array.Empty<string>()).Select(r => (Object)rules[r]).ToArray());

        EraWeightData[] weightsData = d.eras ?? Array.Empty<EraWeightData>();
        SerializedProperty weights = so.FindProperty("eraWeights");
        weights.arraySize = weightsData.Length;
        for (int i = 0; i < weightsData.Length; i++)
        {
            SerializedProperty el = weights.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("era").objectReferenceValue = eras[weightsData[i].era];
            el.FindPropertyRelative("weight").floatValue = weightsData[i].weight;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(plan);
        return plan;
    }

    /// <summary>Sets every world array of the library (authoritative) and drops missing references from the rest.</summary>
    private static void WireLibrary(ContentLibrarySO lib, DayPlanSO[] days, EraSO[] eras, NationSO[] nations, NationEraProfileSO[] places,
                                    ArchetypeSO[] archetypes, AttributeSO[] attributes, ReferenceBookSO[] books)
    {
        var so = new SerializedObject(lib);
        SetArray(so, "dayPlans", days);
        SetArray(so, "eras", eras);
        SetArray(so, "nations", nations);
        SetArray(so, "nationEraProfiles", places);
        SetArray(so, "archetypes", archetypes);
        SetArray(so, "attributes", attributes);
        SetArray(so, "referenceBooks", books);
        DropMissing(so, "clues");
        DropMissing(so, "legendaries");
        DropMissing(so, "effects");
        DropMissing(so, "timelineTriggers");
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }

    /// <summary>Moves generated assets the source no longer lists to the OS trash. Returns how many.</summary>
    private static int PruneOwnedFolders(HashSet<string> written)
    {
        int pruned = 0;
        foreach (string folder in OwnedFolders)
        {
            foreach (string guid in AssetDatabase.FindAssets(string.Empty, new[] { $"{WorldRoot}/{folder}" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path) || written.Contains(path))
                    continue;

                Debug.LogWarning($"[WorldContentGenerator] '{path}' is no longer in the source; moving it to the trash.");
                if (AssetDatabase.MoveAssetToTrash(path))
                    pruned++;
            }
        }

        return pruned;
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
        {
            Debug.LogError($"[WorldContentGenerator] '{so.targetObject.name}' has no serialized field '{prop}'.");
            return;
        }

        var kept = new List<Object>();
        for (int i = 0; i < p.arraySize; i++)
        {
            Object o = p.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o != null)
                kept.Add(o);
        }
        SetArray(so, prop, kept);
    }

    /// <summary>Loads or creates an asset; records its path in <paramref name="written"/> (when given) so pruning keeps it.</summary>
    private static T LoadOrCreate<T>(string path, HashSet<string> written) where T : ScriptableObject
    {
        written?.Add(path);
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
        public ContentData content;
        public EraData[] eras;
        public CountryData[] countries;
        public PlaceData[] places;
        public RuleData[] rules;
        public DayData[] days;
    }

    /// <summary>Authored assets the world is wired into (asset paths).</summary>
    [Serializable] private sealed class ContentData
    {
        public string library;
        public string blueprint;
        public string dayPlanFolder;
        public AttributeData[] attributes;
        public string[] archetypes;
        public string[] books;
    }

    [Serializable] private sealed class AttributeData { public string id; public string asset; }

    [Serializable] private sealed class EraData { public string id; public string displayName; public int order; }

    [Serializable] private sealed class CountryData { public string id; public string displayName; public BaselineData[] baselines; }

    [Serializable] private sealed class BaselineData { public string attribute; public float score; }

    [Serializable] private sealed class FactData { public string category; public string value; }

    /// <summary>A place; "moment" in the source is research context only (not generated).</summary>
    [Serializable] private sealed class PlaceData
    {
        public string country;
        public string era;
        public string displayName;
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
