using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates a full, PLAYABLE investigation world and wires it into
/// ContentLibrary_Main: 3 eras, 4 cultures, 6 nations (with era-appropriate
/// name pools), 6 nation-era profiles (so accepting moves the timeline),
/// 4 archetypes with culture impacts, nation-keyed reference books, document
/// templates, a blueprint, escalating travel rules, and Day 1/2/3 plans.
/// Idempotent: re-running reuses assets by path. Run from
/// Tools > TimeDesk > Generate Investigation Sample.
/// </summary>
public static class InvestigationContentGenerator
{
    private const string Root = "Assets/Data/Investigation";

    [MenuItem("Tools/TimeDesk/Generate Investigation Sample")]
    public static void Generate()
    {
        EnsureFolder("Assets/Data");
        EnsureFolder(Root);

        // --- Eras ---
        EraSO rome = MakeEra("Era_Rome", "rome", "Ancient Rome");
        EraSO medieval = MakeEra("Era_Medieval", "medieval", "Medieval Era");
        EraSO future = MakeEra("Era_Future", "future", "The Future");

        // --- Cultures (attributes) ---
        AttributeSO militarism = MakeAttr("Attr_Militarism", "militarism", "Militarism");
        AttributeSO philosophy = MakeAttr("Attr_Philosophy", "philosophy", "Philosophy");
        AttributeSO industry = MakeAttr("Attr_Industry", "industry", "Industry");
        AttributeSO mysticism = MakeAttr("Attr_Mysticism", "mysticism", "Mysticism");

        // --- Nations (with era-themed name pools) ---
        NationSO latia = MakeNation("Nation_Latia", "latia", "Latia", new[] { "Marcus", "Lucia", "Gaius", "Octavia", "Cassia", "Valerius" });
        NationSO aegyptus = MakeNation("Nation_Aegyptus", "aegyptus", "Aegyptus", new[] { "Amenhotep", "Nefari", "Khufu", "Isetnofret", "Senmut" });
        NationSO norvik = MakeNation("Nation_Norvik", "norvik", "Norvik", new[] { "Bjorn", "Sigrid", "Ivar", "Greta", "Hakon", "Astrid" });
        NationSO albion = MakeNation("Nation_Albion", "albion", "Albion", new[] { "Aldric", "Maud", "Edmund", "Rowena", "Godwin" });
        NationSO solaris = MakeNation("Nation_Solaris", "solaris", "Solaris", new[] { "Zara-7", "Kael", "Nyx", "Orion", "Vesper" });
        NationSO helios = MakeNation("Nation_Helios", "helios", "Helios", new[] { "Vega", "Caspian", "Lyra", "Atlas", "Mira" });

        // --- Nation-era profiles (baseline culture scores -> timeline has data) ---
        NationEraProfileSO pLatia = MakeProfile("Profile_Latia_Rome", "latia_rome", "Latia (Rome)", latia, rome, (militarism, 3f), (philosophy, 2f));
        NationEraProfileSO pAeg = MakeProfile("Profile_Aegyptus_Rome", "aegyptus_rome", "Aegyptus (Rome)", aegyptus, rome, (mysticism, 3f), (industry, 1f));
        NationEraProfileSO pNor = MakeProfile("Profile_Norvik_Med", "norvik_med", "Norvik (Medieval)", norvik, medieval, (militarism, 3f), (industry, 1f));
        NationEraProfileSO pAlb = MakeProfile("Profile_Albion_Med", "albion_med", "Albion (Medieval)", albion, medieval, (industry, 2f), (philosophy, 2f));
        NationEraProfileSO pSol = MakeProfile("Profile_Solaris_Fut", "solaris_fut", "Solaris (Future)", solaris, future, (industry, 3f), (philosophy, 2f));
        NationEraProfileSO pHel = MakeProfile("Profile_Helios_Fut", "helios_fut", "Helios (Future)", helios, future, (mysticism, 2f), (industry, 2f));

        // --- Archetypes (culture impacts so accepts move the timeline) ---
        ArchetypeSO diplomat = MakeArchetype("Archetype_Diplomat", "diplomat", "Diplomat", philosophy);
        ArchetypeSO warrior = MakeArchetype("Archetype_Warrior", "warrior", "Warrior", militarism);
        ArchetypeSO merchant = MakeArchetype("Archetype_Merchant", "merchant", "Merchant", industry);
        ArchetypeSO mystic = MakeArchetype("Archetype_Mystic", "mystic", "Mystic", mysticism);

        // --- Reference books (nation+era keyed) ---
        ReferenceBookSO currency = MakeBook("RefBook_Currency", "Currency Ledger", ClueCategory.Currency, new[]
        {
            (latia, rome, "Denarius"), (aegyptus, rome, "Deben"),
            (norvik, medieval, "Silver Mark"), (albion, medieval, "Sterling"),
            (solaris, future, "Cred-Chits"), (helios, future, "Sol-Tokens")
        });
        ReferenceBookSO language = MakeBook("RefBook_Language", "Tongues & Scripts", ClueCategory.Language, new[]
        {
            (latia, rome, "Latin"), (aegyptus, rome, "Coptic"),
            (norvik, medieval, "Old Norse"), (albion, medieval, "Middle English"),
            (solaris, future, "Interlingua"), (helios, future, "Binary Cant")
        });
        ReferenceBookSO tech = MakeBook("RefBook_Technology", "Index of Devices", ClueCategory.Technology, new[]
        {
            (latia, rome, "Aqueduct"), (aegyptus, rome, "Shaduf"),
            (norvik, medieval, "Longship"), (albion, medieval, "Waterwheel"),
            (solaris, future, "Fusion Cell"), (helios, future, "Solar Sail")
        });

        // --- Document templates ---
        DocumentTemplateSO passport = MakeTemplate("DocTemplate_Passport", "Travel Passport", new[]
        {
            (ClueCategory.Currency, "Coin of Issue", 0),
            (ClueCategory.Language, "Native Tongue", 0)
        });
        DocumentTemplateSO permit = MakeTemplate("DocTemplate_Permit", "Transit Permit", new[]
        {
            (ClueCategory.Technology, "Declared Device", 0),
            (ClueCategory.Currency, "Bond Currency", 1) // page 1 -> multi-page
        });

        // --- Blueprint ---
        CaseBlueprintSO blueprint = LoadOrCreate<CaseBlueprintSO>($"{Root}/CaseBlueprint_Investigation.asset");
        var soBp = new SerializedObject(blueprint);
        SetArray(soBp, "documentTemplates", new Object[] { passport, permit });
        SetArray(soBp, "archetypePool", new Object[] { diplomat, warrior, merchant, mystic });
        SetFloat(soBp, "contradictionChance", 0.5f);
        SetInt(soBp, "totalCluesMin", 0);
        SetInt(soBp, "totalCluesMax", 0);
        SetInt(soBp, "difficulty", 2);
        soBp.ApplyModifiedProperties();
        EditorUtility.SetDirty(blueprint);

        // --- Travel rules ---
        TravelRuleSO noAegyptus = MakeRule("Rule_NoAegyptus", TravelRuleType.NationForbidden, null, aegyptus, "Embargo: no travel to Aegyptus today.");
        TravelRuleSO noFuture = MakeRule("Rule_NoFuture", TravelRuleType.EraForbidden, future, null, "TimeGate sealed: no travel to The Future today.");
        TravelRuleSO noHeliosFuture = MakeRule("Rule_NoHeliosFuture", TravelRuleType.NationEraForbidden, future, helios, "Quarantine: no travel to Helios in The Future.");

        // --- Day plans (escalating rules) ---
        DayPlanSO day1 = MakeDay("DayPlan_Inv_Day1", 1, 5, blueprint, new[] { (rome, 1f), (medieval, 1f), (future, 1f) }, new Object[0]);
        DayPlanSO day2 = MakeDay("DayPlan_Inv_Day2", 2, 6, blueprint, new[] { (rome, 1f), (medieval, 1f), (future, 1f) }, new Object[] { noAegyptus });
        DayPlanSO day3 = MakeDay("DayPlan_Inv_Day3", 3, 7, blueprint, new[] { (rome, 1f), (medieval, 1f), (future, 1f) }, new Object[] { noFuture, noHeliosFuture });

        // --- Wire into ContentLibrary_Main ---
        ContentLibrarySO lib = FindByName<ContentLibrarySO>("ContentLibrary_Main") ?? FindFirst<ContentLibrarySO>();
        if (lib == null)
        {
            lib = LoadOrCreate<ContentLibrarySO>($"{Root}/ContentLibrary_Main.asset");
            Debug.LogWarning("[TimeDesk] No ContentLibrary_Main found — created one in Assets/Data/Investigation. Point RunConfig at it.");
        }

        var soLib = new SerializedObject(lib);
        AppendUnique(soLib, "eras", rome, medieval, future);
        AppendUnique(soLib, "attributes", militarism, philosophy, industry, mysticism);
        AppendUnique(soLib, "nations", latia, aegyptus, norvik, albion, solaris, helios);
        AppendUnique(soLib, "nationEraProfiles", pLatia, pAeg, pNor, pAlb, pSol, pHel);
        AppendUnique(soLib, "archetypes", diplomat, warrior, merchant, mystic);
        AppendUnique(soLib, "referenceBooks", currency, language, tech);
        // Insert so Day 1/2/3 are the first matches for their day number.
        InsertFirstUnique(soLib, "dayPlans", day3);
        InsertFirstUnique(soLib, "dayPlans", day2);
        InsertFirstUnique(soLib, "dayPlans", day1);
        soLib.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TimeDesk] Investigation world generated: 3 eras, 4 cultures, 6 nations, 6 profiles, 4 archetypes, 3 books, 3 days. Wired into ContentLibrary_Main. Re-run 'Build Office UI', then New Run.");
    }

    // -----------------------------
    // Asset factories
    // -----------------------------

    private static EraSO MakeEra(string file, string id, string display)
    {
        var e = LoadOrCreate<EraSO>($"{Root}/{file}.asset");
        e.id = id; e.displayName = display;
        EditorUtility.SetDirty(e);
        return e;
    }

    private static AttributeSO MakeAttr(string file, string id, string display)
    {
        var a = LoadOrCreate<AttributeSO>($"{Root}/{file}.asset");
        a.id = id; a.displayName = display;
        EditorUtility.SetDirty(a);
        return a;
    }

    private static NationSO MakeNation(string file, string id, string display, string[] names)
    {
        var n = LoadOrCreate<NationSO>($"{Root}/{file}.asset");
        n.id = id; n.displayName = display; n.namePool = names;
        EditorUtility.SetDirty(n);
        return n;
    }

    private static NationEraProfileSO MakeProfile(string file, string id, string display, NationSO nation, EraSO era, params (AttributeSO attr, float score)[] baselines)
    {
        var p = LoadOrCreate<NationEraProfileSO>($"{Root}/{file}.asset");
        p.id = id; p.displayName = display; p.nation = nation; p.era = era;
        p.baselines = new List<AttributeBaseline>();
        foreach (var (attr, score) in baselines)
            p.baselines.Add(new AttributeBaseline { attribute = attr, baseScore = score });
        EditorUtility.SetDirty(p);
        return p;
    }

    private static ArchetypeSO MakeArchetype(string file, string id, string display, AttributeSO culture)
    {
        var a = LoadOrCreate<ArchetypeSO>($"{Root}/{file}.asset");
        a.id = id;
        a.displayName = display;
        a.tags = new[] { id };
        a.baseWeight = 1f;
        a.defaultImpacts = new List<TimelineImpact>
        {
            new TimelineImpact { attribute = culture, deltaOnCorrect = 1f, deltaOnWrong = -1f, alsoAffectsNationScore = true }
        };
        EditorUtility.SetDirty(a);
        return a;
    }

    private static ReferenceBookSO MakeBook(string file, string display, ClueCategory cat, (NationSO nation, EraSO era, string value)[] rows)
    {
        var b = LoadOrCreate<ReferenceBookSO>($"{Root}/{file}.asset");
        b.displayName = display; b.category = cat;
        b.entries = new List<ReferenceEntry>();
        foreach (var (nation, era, value) in rows)
            b.entries.Add(new ReferenceEntry { nation = nation, era = era, value = value });
        EditorUtility.SetDirty(b);
        return b;
    }

    private static DocumentTemplateSO MakeTemplate(string file, string display, (ClueCategory cat, string label, int page)[] fields)
    {
        var t = LoadOrCreate<DocumentTemplateSO>($"{Root}/{file}.asset");
        t.displayName = display;
        var specs = new List<DocumentFieldSpec>();
        foreach (var (cat, label, page) in fields)
            specs.Add(new DocumentFieldSpec { category = cat, label = label, page = page });
        t.fieldSpecs = specs.ToArray();
        EditorUtility.SetDirty(t);
        return t;
    }

    private static TravelRuleSO MakeRule(string file, TravelRuleType type, EraSO era, NationSO nation, string desc)
    {
        var r = LoadOrCreate<TravelRuleSO>($"{Root}/{file}.asset");
        r.type = type; r.era = era; r.nation = nation; r.description = desc;
        EditorUtility.SetDirty(r);
        return r;
    }

    private static DayPlanSO MakeDay(string file, int dayNumber, int visitors, CaseBlueprintSO blueprint, (EraSO era, float weight)[] eras, Object[] rules)
    {
        var plan = LoadOrCreate<DayPlanSO>($"{Root}/{file}.asset");
        var so = new SerializedObject(plan);
        SetInt(so, "dayNumber", dayNumber);
        SetInt(so, "visitorsCount", visitors);
        SetArray(so, "possibleBlueprints", new Object[] { blueprint });
        SetArray(so, "activeTravelRules", rules);
        SetEraWeights(so, "eraWeights", eras);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(plan);
        return plan;
    }

    // -----------------------------
    // Serialized helpers
    // -----------------------------

    private static void SetInt(SerializedObject so, string prop, int v) { var p = so.FindProperty(prop); if (p != null) p.intValue = v; }
    private static void SetFloat(SerializedObject so, string prop, float v) { var p = so.FindProperty(prop); if (p != null) p.floatValue = v; }

    private static void SetArray(SerializedObject so, string prop, Object[] values)
    {
        var p = so.FindProperty(prop);
        if (p == null) return;
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void SetEraWeights(SerializedObject so, string prop, (EraSO era, float weight)[] rows)
    {
        var p = so.FindProperty(prop);
        if (p == null) return;
        p.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; i++)
        {
            var el = p.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("era").objectReferenceValue = rows[i].era;
            el.FindPropertyRelative("weight").floatValue = rows[i].weight;
        }
    }

    private static void AppendUnique(SerializedObject so, string prop, params Object[] values)
    {
        var p = so.FindProperty(prop);
        if (p == null) return;
        var present = new HashSet<Object>();
        for (int i = 0; i < p.arraySize; i++) present.Add(p.GetArrayElementAtIndex(i).objectReferenceValue);
        foreach (Object v in values)
        {
            if (v == null || present.Contains(v)) continue;
            p.arraySize++;
            p.GetArrayElementAtIndex(p.arraySize - 1).objectReferenceValue = v;
            present.Add(v);
        }
    }

    private static void InsertFirstUnique(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p == null || value == null) return;
        for (int i = 0; i < p.arraySize; i++)
            if (p.GetArrayElementAtIndex(i).objectReferenceValue == value) return;
        p.InsertArrayElementAtIndex(0);
        p.GetArrayElementAtIndex(0).objectReferenceValue = value;
    }

    // -----------------------------
    // Asset DB plumbing
    // -----------------------------

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        T created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    private static T FindByName<T>(string assetName) where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"{assetName} t:{typeof(T).Name}"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == assetName)
                return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        return null;
    }

    private static T FindFirst<T>() where T : Object
    {
        foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }
}
