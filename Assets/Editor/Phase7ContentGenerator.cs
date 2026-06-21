// ReSharper disable InconsistentNaming
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Phase 7 content pass. Creates the full alpha content roster (attributes, eras,
/// nations, nation-era profiles + dominance effects, archetypes, legendaries,
/// timeline triggers, upgrades, slot outcomes, endings, and Day 2/3 plans) and
/// wires everything into ContentLibrary_Main. Idempotent: re-running finds and
/// reuses existing assets by path rather than duplicating them.
/// </summary>
public static class Phase7ContentGenerator
{
    private const string DataRoot = "Assets/Data";

    /// <summary>Entry point. Run from Tools > TimeDesk > Generate Phase 7 Content.</summary>
    [MenuItem("Tools/TimeDesk/Generate Phase 7 Content")]
    public static void Generate()
    {
        Debug.Log("[Phase7ContentGenerator] >>> Entering Generate");

        AttributeSO attrDemocracy = CreateAttribute("democracy", "Democracy",
            "Measures civic trust in democratic institutions versus authoritarian control. High values favor open, participatory rule; low values favor centralized, fascist control.");
        AttributeSO attrScience = CreateAttribute("science", "Science",
            "Tracks the era's dominant technological focus, from steam and gravity-bound engineering to nuclear and electric power.");
        AttributeSO attrArt = CreateAttribute("art", "Art",
            "Tracks the dominant artistic movement and cultural aesthetic of the era, from classical sculpture to modern and pop art.");

        Debug.Log("[Phase7ContentGenerator] Created/verified 3 attributes");

        (EraSO greece, EraSO ngermany, EraSO japan, EraSO egypt, EraSO china) = CreateEras();
        (NationSO nGreece, NationSO nGermany, NationSO nJapan, NationSO nEgypt, NationSO nChina) = CreateNations();

        Debug.Log("[Phase7ContentGenerator] Created/verified eras and nations");

        NationEraProfileSO[] profiles = CreateProfilesAndEffects(
            attrDemocracy, attrScience, attrArt,
            greece, ngermany, japan, egypt, china,
            nGreece, nGermany, nJapan, nEgypt, nChina);

        Debug.Log("[Phase7ContentGenerator] Created/verified 5 nation-era profiles + 30 dominance effects");

        ArchetypeSO[] archetypes = CreateArchetypes(attrDemocracy, attrScience, attrArt);

        Debug.Log("[Phase7ContentGenerator] Created/verified 6 archetypes");

        LegendarySO[] legendaries = CreateLegendaries(
            attrDemocracy, attrScience, attrArt,
            greece, ngermany, japan, egypt, china,
            nGreece, nGermany, nJapan, nEgypt, nChina,
            archetypes);

        Debug.Log("[Phase7ContentGenerator] Created/verified 21 legendaries");

        (TimelineTriggerSO[] triggers, EffectSO[] triggerEffects) = CreateTriggers(attrDemocracy, attrScience, attrArt);

        Debug.Log("[Phase7ContentGenerator] Created/verified 3 timeline triggers + effects");

        (UpgradeSO[] upgrades, EffectSO[] upgradeEffects) = CreateUpgrades();

        Debug.Log("[Phase7ContentGenerator] Created/verified 3 upgrades + effects");

        SlotOutcomeSO[] slotOutcomes = CreateSlotOutcomes();

        Debug.Log("[Phase7ContentGenerator] Created/verified 5 slot outcomes");

        EndingSO[] endings = CreateEndings(attrDemocracy, attrScience, attrArt);

        Debug.Log("[Phase7ContentGenerator] Created/verified 6 endings");

        DayPlanSO[] dayPlans = CreateDayPlans(
            greece, ngermany, japan, egypt, china, legendaries);

        Debug.Log("[Phase7ContentGenerator] Created/verified Day 2 + Day 3 plans");

        WireContentLibrary(
            new AttributeSO[] { attrDemocracy, attrScience, attrArt },
            new EraSO[] { japan, egypt, china },
            new NationSO[] { nGreece, nGermany, nJapan, nEgypt, nChina },
            profiles, archetypes, legendaries, triggers, triggerEffects,
            upgrades, upgradeEffects, slotOutcomes, endings, dayPlans,
            CollectAllDominanceEffects(profiles));

        Debug.Log("[Phase7ContentGenerator] Wired ContentLibrary_Main");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Phase7ContentGenerator] <<< Exiting Generate");
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    /// <summary>Loads an existing asset at <paramref name="path"/>, or creates a new one.</summary>
    private static T CreateAsset<T>(string path) where T : ScriptableObject
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
            return existing;

        EnsureFolder(Path.GetDirectoryName(path));
        T asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    /// <summary>Recursively creates the folder at <paramref name="folderPath"/> if missing.</summary>
    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath))
            return;

        folderPath = folderPath.Replace("\\", "/");
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
        string leaf = Path.GetFileName(folderPath);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }

    /// <summary>Overwrites a private object-array field via SerializedObject.</summary>
    private static void SetArray(Object target, string fieldName, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        prop.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    /// <summary>Appends new, non-duplicate entries to a private object-array field.</summary>
    private static void AppendArray(Object target, string fieldName, Object[] newValues)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        int oldSize = prop.arraySize;

        var existing = new HashSet<Object>();
        for (int i = 0; i < oldSize; i++)
            existing.Add(prop.GetArrayElementAtIndex(i).objectReferenceValue);

        var toAdd = new List<Object>();
        foreach (Object v in newValues)
            if (v != null && !existing.Contains(v))
                toAdd.Add(v);

        prop.arraySize = oldSize + toAdd.Count;
        for (int i = 0; i < toAdd.Count; i++)
            prop.GetArrayElementAtIndex(oldSize + i).objectReferenceValue = toAdd[i];

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    /// <summary>Sets DayPlanSO.eraWeights (struct array) via SerializedObject.</summary>
    private static void SetEraWeights(DayPlanSO plan, (EraSO era, float weight)[] weights)
    {
        var so = new SerializedObject(plan);
        SerializedProperty prop = so.FindProperty("eraWeights");
        prop.arraySize = weights.Length;

        for (int i = 0; i < weights.Length; i++)
        {
            SerializedProperty element = prop.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("era").objectReferenceValue = weights[i].era;
            element.FindPropertyRelative("weight").floatValue = weights[i].weight;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(plan);
    }

    /// <summary>Builds a single-op General/BriefingLine flavor effect (1-day duration).</summary>
    private static EffectSO CreateBriefingEffect(string path, string displayName, string briefingLine)
    {
        EffectSO fx = CreateAsset<EffectSO>(path);
        fx.displayName = displayName;
        fx.channel = EffectChannel.General;
        fx.defaultDurationDays = 1;
        fx.ops = new List<EffectOp>
        {
            new EffectOp { type = EffectOpType.BriefingLine, stringParam = briefingLine }
        };
        EditorUtility.SetDirty(fx);
        return fx;
    }

    // -----------------------------------------------------------------
    // Attributes
    // -----------------------------------------------------------------

    private static AttributeSO CreateAttribute(string id, string displayName, string description)
    {
        AttributeSO attr = CreateAsset<AttributeSO>($"{DataRoot}/Attributes/Attr_{Capitalize(id)}.asset");
        attr.id = id;
        attr.displayName = displayName;
        attr.description = description;
        EditorUtility.SetDirty(attr);
        return attr;
    }

    private static string Capitalize(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

    // -----------------------------------------------------------------
    // Eras
    // -----------------------------------------------------------------

    private static (EraSO greece, EraSO ngermany, EraSO japan, EraSO egypt, EraSO china) CreateEras()
    {
        // Greece and NGermany already exist from earlier phases; load them by path.
        EraSO greece = AssetDatabase.LoadAssetAtPath<EraSO>($"{DataRoot}/Eras/Era_Greece.asset");
        EraSO ngermany = AssetDatabase.LoadAssetAtPath<EraSO>($"{DataRoot}/Eras/Era_NGermany.asset");

        if (greece == null)
            Debug.LogWarning("[Phase7ContentGenerator] Era_Greece.asset not found — expected from Phase 2.");
        if (ngermany == null)
            Debug.LogWarning("[Phase7ContentGenerator] Era_NGermany.asset not found — expected from Phase 2.");

        EraSO japan = CreateAsset<EraSO>($"{DataRoot}/Eras/Era_Japan.asset");
        japan.id = "Japan";
        japan.displayName = "Imperial Japan";
        EditorUtility.SetDirty(japan);

        EraSO egypt = CreateAsset<EraSO>($"{DataRoot}/Eras/Era_Egypt.asset");
        egypt.id = "Egypt";
        egypt.displayName = "Ancient Egypt";
        EditorUtility.SetDirty(egypt);

        EraSO china = CreateAsset<EraSO>($"{DataRoot}/Eras/Era_China.asset");
        china.id = "China";
        china.displayName = "Imperial China";
        EditorUtility.SetDirty(china);

        return (greece, ngermany, japan, egypt, china);
    }

    // -----------------------------------------------------------------
    // Nations
    // -----------------------------------------------------------------

    private static (NationSO greece, NationSO germany, NationSO japan, NationSO egypt, NationSO china) CreateNations()
    {
        NationSO greece = CreateNation("greece", "Greece");
        NationSO germany = CreateNation("germany", "Germany");
        NationSO japan = CreateNation("japan", "Japan");
        NationSO egypt = CreateNation("egypt", "Egypt");
        NationSO china = CreateNation("china", "China");

        return (greece, germany, japan, egypt, china);
    }

    private static NationSO CreateNation(string id, string displayName)
    {
        NationSO nation = CreateAsset<NationSO>($"{DataRoot}/Nations/Nation_{Capitalize(id)}.asset");
        nation.id = id;
        nation.displayName = displayName;
        EditorUtility.SetDirty(nation);
        return nation;
    }

    // -----------------------------------------------------------------
    // Nation-era profiles + 30 dominance/supporting effects
    // -----------------------------------------------------------------

    /// <summary>Tracks every dominance/supporting effect created, for ContentLibrary wiring.</summary>
    private static readonly List<EffectSO> _dominanceEffects = new();

    private static NationEraProfileSO[] CreateProfilesAndEffects(
        AttributeSO democracy, AttributeSO science, AttributeSO art,
        EraSO greece, EraSO ngermany, EraSO japan, EraSO egypt, EraSO china,
        NationSO nGreece, NationSO nGermany, NationSO nJapan, NationSO nEgypt, NationSO nChina)
    {
        _dominanceEffects.Clear();

        NationEraProfileSO greeceProfile = CreateProfile(
            "greece_classical", "Classical Greece", nGreece, greece,
            new[]
            {
                AttrBaseline(democracy, 75f, "greece", "democracy",
                    "Athenian assemblies dominate civic life; democratic ideals radiate outward across the timeline.",
                    "Citizen councils still murmur beneath the surface of Greek politics."),
                AttrBaseline(science, 40f, "greece", "science",
                    "Greek philosophers chart the heavens, laying gravity's first foundations.",
                    "Geometers and stargazers quietly refine their instruments."),
                AttrBaseline(art, 60f, "greece", "art",
                    "Classical sculpture and architecture define the era's aesthetic ideal.",
                    "Marble friezes and painted amphorae circulate through every household."),
            });

        NationEraProfileSO germanyProfile = CreateProfile(
            "germany_n", "Nazi Germany", nGermany, ngermany,
            new[]
            {
                AttrBaseline(democracy, 8f, "germany", "democracy",
                    "Fascist rule has crushed dissent; democratic voices are silenced across the era.",
                    "Underground pamphlets hint at a fragile democratic resistance."),
                AttrBaseline(science, 82f, "germany", "science",
                    "Nuclear and electrical research dominates state priorities, racing toward new power.",
                    "Engineers tinker with experimental reactors and electrical grids in secret labs."),
                AttrBaseline(art, 55f, "germany", "art",
                    "Stark modernist propaganda art lines every boulevard and broadcast.",
                    "Modernist posters compete uneasily with banned avant-garde works."),
            });

        NationEraProfileSO japanProfile = CreateProfile(
            "japan_imperial", "Imperial Japan", nJapan, japan,
            new[]
            {
                AttrBaseline(democracy, 45f, "japan", "democracy",
                    "Reformist factions push for a constitutional voice in an era of rapid change.",
                    "Local assemblies quietly debate the limits of imperial authority."),
                AttrBaseline(science, 70f, "japan", "science",
                    "Electric industry and steam-driven works power a sweeping wave of modernization.",
                    "New factories experiment with electric motors and steam turbines."),
                AttrBaseline(art, 85f, "japan", "art",
                    "Bold, anime-inspired illustration styles sweep across the era's culture.",
                    "Traditional ink art mingles with bright new illustrated forms."),
            });

        NationEraProfileSO egyptProfile = CreateProfile(
            "egypt_ancient", "Ancient Egypt", nEgypt, egypt,
            new[]
            {
                AttrBaseline(democracy, 18f, "egypt", "democracy",
                    "Pharaonic decree governs all; the people hold no formal voice in the era.",
                    "Local councils of elders quietly mediate village disputes."),
                AttrBaseline(science, 35f, "egypt", "science",
                    "Monumental engineering bends gravity itself into towering stone.",
                    "Surveyors and architects steadily refine their measuring techniques."),
                AttrBaseline(art, 78f, "egypt", "art",
                    "Hieroglyphic murals and gilded iconography define every wall and tomb.",
                    "Painted tomb reliefs preserve the era's artistic conventions."),
            });

        NationEraProfileSO chinaProfile = CreateProfile(
            "china_imperial", "Imperial China", nChina, china,
            new[]
            {
                AttrBaseline(democracy, 14f, "china", "democracy",
                    "Imperial decree governs the realm with absolute, unquestioned authority.",
                    "Scholar-officials quietly debate reforms within the bureaucracy."),
                AttrBaseline(science, 60f, "china", "science",
                    "Steam-driven workshops and gunpowder craft define the era's innovation.",
                    "Inventors refine printing presses and steam-powered tools."),
                AttrBaseline(art, 80f, "china", "art",
                    "Flowing scroll paintings and calligraphy define the era's cultural ideal.",
                    "Scroll painters and poets circulate their work among scholars."),
            });

        return new[] { greeceProfile, germanyProfile, japanProfile, egyptProfile, chinaProfile };
    }

    /// <summary>Builds one AttributeBaseline plus its dominant/supporting BriefingLine effects.</summary>
    private static AttributeBaseline AttrBaseline(
        AttributeSO attribute, float baseScore,
        string profileKey, string attrKey,
        string dominantLine, string supportingLine)
    {
        EffectSO dominant = CreateBriefingEffect(
            $"{DataRoot}/Effects/Effect_Dom_{Capitalize(profileKey)}_{Capitalize(attrKey)}.asset",
            $"{Capitalize(profileKey)} - {Capitalize(attrKey)} Dominant",
            dominantLine);

        EffectSO supporting = CreateBriefingEffect(
            $"{DataRoot}/Effects/Effect_Sup_{Capitalize(profileKey)}_{Capitalize(attrKey)}.asset",
            $"{Capitalize(profileKey)} - {Capitalize(attrKey)} Supporting",
            supportingLine);

        _dominanceEffects.Add(dominant);
        _dominanceEffects.Add(supporting);

        return new AttributeBaseline
        {
            attribute = attribute,
            baseScore = baseScore,
            dominantEffect = dominant,
            supportingEffect = supporting,
        };
    }

    private static NationEraProfileSO CreateProfile(string id, string displayName, NationSO nation, EraSO era, AttributeBaseline[] baselines)
    {
        NationEraProfileSO profile = CreateAsset<NationEraProfileSO>($"{DataRoot}/Profiles/Profile_{Capitalize(id)}.asset");
        profile.id = id;
        profile.displayName = displayName;
        profile.nation = nation;
        profile.era = era;
        profile.baselines = new List<AttributeBaseline>(baselines);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    /// <summary>Returns all 30 dominance/supporting effects created by CreateProfilesAndEffects.</summary>
    private static EffectSO[] CollectAllDominanceEffects(NationEraProfileSO[] profiles) => _dominanceEffects.ToArray();

    // -----------------------------------------------------------------
    // Archetypes
    // -----------------------------------------------------------------

    private static ArchetypeSO[] CreateArchetypes(AttributeSO democracy, AttributeSO science, AttributeSO art)
    {
        ArchetypeSO diplomat = CreateArchetype("diplomat", "Diplomat",
            new[] { "diplomat", "politics", "democracy" },
            new[] { "Ambassador Velos", "Consul Adina", "Envoy Mireth", "Magistrate Soren", "Attache Lyris" },
            1f,
            new List<TimelineImpact>
            {
                new TimelineImpact { attribute = democracy, deltaOnCorrect = 2f, deltaOnWrong = -1f, alsoAffectsNationScore = true },
            });

        ArchetypeSO scientist = CreateArchetype("scientist", "Scientist",
            new[] { "scientist", "science", "tech" },
            new[] { "Dr. Halvor Stane", "Professor Mira Quill", "Engineer Toma Reyk", "Researcher Lin Wei", "Inventor Petra Voss" },
            1f,
            new List<TimelineImpact>
            {
                new TimelineImpact { attribute = science, deltaOnCorrect = 2f, deltaOnWrong = -1f, alsoAffectsNationScore = true },
            });

        ArchetypeSO artist = CreateArchetype("artist", "Artist",
            new[] { "artist", "art", "culture" },
            new[] { "Painter Yuki Saemon", "Sculptor Demos Kallis", "Calligrapher Mei Lan", "Muralist Anwar Sett", "Illustrator Greta Holm" },
            1f,
            new List<TimelineImpact>
            {
                new TimelineImpact { attribute = art, deltaOnCorrect = 2f, deltaOnWrong = -1f, alsoAffectsNationScore = true },
            });

        ArchetypeSO soldier = CreateArchetype("soldier", "Soldier",
            new[] { "soldier", "military", "authority" },
            new[] { "Captain Olen Brask", "Commander Hideo Mura", "Officer Karim Nazari", "Sergeant Liu Bo", "Colonel Greta Vance" },
            1f,
            new List<TimelineImpact>
            {
                new TimelineImpact { attribute = democracy, deltaOnCorrect = -1.5f, deltaOnWrong = 0.5f, alsoAffectsNationScore = true },
            });

        ArchetypeSO merchant = CreateArchetype("merchant", "Merchant",
            new[] { "merchant", "trade", "economy" },
            new[] { "Trader Niko Pell", "Merchant Fatima Idris", "Broker Wen Cheng", "Vendor Aiko Tanaka", "Dealer Markos Iliad" },
            1f,
            new List<TimelineImpact>
            {
                new TimelineImpact { attribute = art, deltaOnCorrect = 1f, deltaOnWrong = -0.5f, alsoAffectsNationScore = true },
            });

        // Wanderer: baseWeight 0 so it is never picked procedurally, but can be
        // assigned directly to unaffiliated legendaries via LegendarySO.archetype.
        ArchetypeSO wanderer = CreateArchetype("wanderer", "Wanderer",
            new[] { "wanderer", "legendary", "unaffiliated" },
            System.Array.Empty<string>(),
            0f,
            new List<TimelineImpact>());

        return new[] { diplomat, scientist, artist, soldier, merchant, wanderer };
    }

    private static ArchetypeSO CreateArchetype(string id, string displayName, string[] tags, string[] namePool, float baseWeight, List<TimelineImpact> defaultImpacts)
    {
        ArchetypeSO archetype = CreateAsset<ArchetypeSO>($"{DataRoot}/Archetypes/Archetype_{Capitalize(id)}.asset");
        archetype.id = id;
        archetype.displayName = displayName;
        archetype.tags = tags;
        archetype.namePool = namePool;
        archetype.baseWeight = baseWeight;
        archetype.defaultImpacts = defaultImpacts;
        EditorUtility.SetDirty(archetype);
        return archetype;
    }

    // -----------------------------------------------------------------
    // Legendaries (15 nation-tied + 6 unaffiliated)
    // -----------------------------------------------------------------

    private static LegendarySO[] CreateLegendaries(
        AttributeSO democracy, AttributeSO science, AttributeSO art,
        EraSO greece, EraSO ngermany, EraSO japan, EraSO egypt, EraSO china,
        NationSO nGreece, NationSO nGermany, NationSO nJapan, NationSO nEgypt, NationSO nChina,
        ArchetypeSO[] archetypes)
    {
        ArchetypeSO diplomat = archetypes[0];
        ArchetypeSO scientist = archetypes[1];
        ArchetypeSO artist = archetypes[2];
        ArchetypeSO soldier = archetypes[3];
        ArchetypeSO wanderer = archetypes[5];

        var result = new List<LegendarySO>();

        // --- Greece (3) ---
        result.Add(CreateLegendary("Greece_Philosopher", "Theron the Stargazer", greece, nGreece, scientist,
            Impact(science, 3f, -1f, true), Impact(democracy, 1f, 0f, true)));
        result.Add(CreateLegendary("Greece_Orator", "Calliope Demarch", greece, nGreece, diplomat,
            Impact(democracy, 3f, -1f, true)));
        result.Add(CreateLegendary("Greece_Sculptor", "Phidian Marcos", greece, nGreece, artist,
            Impact(art, 3f, -1f, true)));

        // --- Germany (3) ---
        result.Add(CreateLegendary("Germany_Engineer", "Doctor Ilse Vahn", ngermany, nGermany, scientist,
            Impact(science, 3f, -1f, true)));
        result.Add(CreateLegendary("Germany_Officer", "Colonel Reinhardt Voss", ngermany, nGermany, soldier,
            Impact(democracy, -3f, 1f, true)));
        result.Add(CreateLegendary("Germany_Painter", "Greta Brandt", ngermany, nGermany, artist,
            Impact(art, 3f, -1f, true)));

        // --- Japan (3) ---
        result.Add(CreateLegendary("Japan_Inventor", "Hideo Kanzaki", japan, nJapan, scientist,
            Impact(science, 3f, -1f, true)));
        result.Add(CreateLegendary("Japan_Reformer", "Sachiko Imari", japan, nJapan, diplomat,
            Impact(democracy, 3f, -1f, true)));
        result.Add(CreateLegendary("Japan_Illustrator", "Renji Tachibana", japan, nJapan, artist,
            Impact(art, 3f, -1f, true)));

        // --- Egypt (3) ---
        result.Add(CreateLegendary("Egypt_Architect", "Senmet Khare", egypt, nEgypt, scientist,
            Impact(science, 3f, -1f, true)));
        result.Add(CreateLegendary("Egypt_Vizier", "Nefru Tahan", egypt, nEgypt, diplomat,
            Impact(democracy, 2f, -1f, true)));
        result.Add(CreateLegendary("Egypt_Painter", "Amara Sett", egypt, nEgypt, artist,
            Impact(art, 3f, -1f, true)));

        // --- China (3) ---
        result.Add(CreateLegendary("China_Inventor", "Wen Zhirong", china, nChina, scientist,
            Impact(science, 3f, -1f, true)));
        result.Add(CreateLegendary("China_Official", "Magistrate Lian Bo", china, nChina, diplomat,
            Impact(democracy, 2f, -1f, true)));
        result.Add(CreateLegendary("China_Painter", "Mei Qiulan", china, nChina, artist,
            Impact(art, 3f, -1f, true)));

        // --- Unaffiliated (6) — nation = null, trueEra still set so CaseFactory
        // can build documents/clues; alsoAffectsNationScore = false so they don't
        // spuriously boost a nation's score. ---

        // Democracy pair
        result.Add(CreateLegendary("Wanderer_Idealist", "The Idealist", greece, null, wanderer,
            Impact(democracy, 4f, -2f, false)));
        result.Add(CreateLegendary("Wanderer_Tyrant", "The Tyrant", ngermany, null, wanderer,
            Impact(democracy, -4f, 2f, false)));

        // Science pair
        result.Add(CreateLegendary("Wanderer_Visionary", "The Visionary", japan, null, wanderer,
            Impact(science, 4f, -2f, false)));
        result.Add(CreateLegendary("Wanderer_Alchemist", "The Alchemist", egypt, null, wanderer,
            Impact(science, 3f, -1f, false)));

        // Art pair (Pop Art / Chinese Scroll flavor reserved for these two)
        result.Add(CreateLegendary("Wanderer_Muse", "The Muse", china, null, wanderer,
            Impact(art, 4f, -2f, false)));
        result.Add(CreateLegendary("Wanderer_Iconoclast", "The Iconoclast", greece, null, wanderer,
            Impact(art, 3f, -1f, false)));

        return result.ToArray();
    }

    private static TimelineImpact Impact(AttributeSO attribute, float deltaOnCorrect, float deltaOnWrong, bool alsoAffectsNationScore)
    {
        return new TimelineImpact
        {
            attribute = attribute,
            deltaOnCorrect = deltaOnCorrect,
            deltaOnWrong = deltaOnWrong,
            alsoAffectsNationScore = alsoAffectsNationScore,
        };
    }

    private static LegendarySO CreateLegendary(string assetKey, string displayName, EraSO trueEra, NationSO nation, ArchetypeSO archetype, params TimelineImpact[] impacts)
    {
        LegendarySO legendary = CreateAsset<LegendarySO>($"{DataRoot}/Legendaries/Legendary_{assetKey}.asset");
        legendary.displayName = displayName;
        legendary.minDay = 1;
        legendary.maxDay = 999;
        legendary.trueEra = trueEra;
        legendary.blueprintOverride = null;
        legendary.archetype = archetype;
        legendary.nation = nation;
        legendary.authoredImpacts = new List<TimelineImpact>(impacts);
        EditorUtility.SetDirty(legendary);
        return legendary;
    }

    // -----------------------------------------------------------------
    // Timeline triggers + their effects
    // -----------------------------------------------------------------

    private static (TimelineTriggerSO[] triggers, EffectSO[] effects) CreateTriggers(AttributeSO democracy, AttributeSO science, AttributeSO art)
    {
        EffectSO scienceDiscount = CreateAsset<EffectSO>($"{DataRoot}/Effects/Effect_Trigger_ScienceDiscount.asset");
        scienceDiscount.displayName = "Science Boom Discount";
        scienceDiscount.channel = EffectChannel.Shop;
        scienceDiscount.defaultDurationDays = 3;
        scienceDiscount.ops = new List<EffectOp>
        {
            new EffectOp { type = EffectOpType.ShopDiscountPercent, stringParam = "", floatParam = 15f }
        };
        EditorUtility.SetDirty(scienceDiscount);

        EffectSO forgeryRisk = CreateAsset<EffectSO>($"{DataRoot}/Effects/Effect_Trigger_ForgeryRisk.asset");
        forgeryRisk.displayName = "Democratic Collapse Forgery Risk";
        forgeryRisk.channel = EffectChannel.SpecialCases;
        forgeryRisk.defaultDurationDays = 3;
        forgeryRisk.ops = new List<EffectOp>
        {
            new EffectOp { type = EffectOpType.ForgeryChanceBonus, floatParam = 0.15f }
        };
        EditorUtility.SetDirty(forgeryRisk);

        EffectSO legendaryBoost = CreateAsset<EffectSO>($"{DataRoot}/Effects/Effect_Trigger_LegendaryBoost.asset");
        legendaryBoost.displayName = "Art Renaissance Legendary Boost";
        legendaryBoost.channel = EffectChannel.VisitorPool;
        legendaryBoost.defaultDurationDays = 3;
        legendaryBoost.ops = new List<EffectOp>
        {
            new EffectOp { type = EffectOpType.LegendaryChanceBonus, floatParam = 0.05f }
        };
        EditorUtility.SetDirty(legendaryBoost);

        TimelineTriggerSO scienceBoom = CreateAsset<TimelineTriggerSO>($"{DataRoot}/Triggers/Trigger_ScienceBoom.asset");
        scienceBoom.id = "science_boom";
        scienceBoom.displayName = "Science Boom";
        scienceBoom.description = "Fires when the global Science attribute total reaches a high threshold, granting a temporary shop discount.";
        scienceBoom.oneShot = true;
        scienceBoom.newsLineOnFire = "Breaking: a wave of scientific breakthroughs sweeps across the timeline!";
        scienceBoom.conditions = new List<TriggerCondition>
        {
            new TriggerCondition { type = TriggerConditionType.AttributeScoreAtLeast, attribute = science, threshold = 150f }
        };
        scienceBoom.outcomes = new List<TriggerOutcome>
        {
            new TriggerOutcome { effect = scienceDiscount, durationDaysOverride = 3 }
        };
        EditorUtility.SetDirty(scienceBoom);

        TimelineTriggerSO democracyCollapse = CreateAsset<TimelineTriggerSO>($"{DataRoot}/Triggers/Trigger_DemocracyCollapse.asset");
        democracyCollapse.id = "democracy_collapse";
        democracyCollapse.displayName = "Democratic Collapse";
        democracyCollapse.description = "Fires when the global Democracy attribute total falls to a low threshold, increasing forgery risk.";
        democracyCollapse.oneShot = true;
        democracyCollapse.newsLineOnFire = "Reports indicate democratic institutions are crumbling across the timeline.";
        democracyCollapse.conditions = new List<TriggerCondition>
        {
            new TriggerCondition { type = TriggerConditionType.AttributeScoreAtMost, attribute = democracy, threshold = 20f }
        };
        democracyCollapse.outcomes = new List<TriggerOutcome>
        {
            new TriggerOutcome { effect = forgeryRisk, durationDaysOverride = 3 }
        };
        EditorUtility.SetDirty(democracyCollapse);

        TimelineTriggerSO artRenaissance = CreateAsset<TimelineTriggerSO>($"{DataRoot}/Triggers/Trigger_ArtRenaissance.asset");
        artRenaissance.id = "art_renaissance";
        artRenaissance.displayName = "Art Renaissance";
        artRenaissance.description = "Fires when the global Art attribute total reaches a high threshold, boosting legendary visitor chance.";
        artRenaissance.oneShot = true;
        artRenaissance.newsLineOnFire = "A renaissance of artistic movements ripples through every era.";
        artRenaissance.conditions = new List<TriggerCondition>
        {
            new TriggerCondition { type = TriggerConditionType.AttributeScoreAtLeast, attribute = art, threshold = 150f }
        };
        artRenaissance.outcomes = new List<TriggerOutcome>
        {
            new TriggerOutcome { effect = legendaryBoost, durationDaysOverride = 3 }
        };
        EditorUtility.SetDirty(artRenaissance);

        return (
            new[] { scienceBoom, democracyCollapse, artRenaissance },
            new[] { scienceDiscount, forgeryRisk, legendaryBoost });
    }

    // -----------------------------------------------------------------
    // Upgrades + their effects
    // -----------------------------------------------------------------

    private static (UpgradeSO[] upgrades, EffectSO[] effects) CreateUpgrades()
    {
        EffectSO scannerBoost = CreateAsset<EffectSO>($"{DataRoot}/Effects/Effect_Upgrade_ScannerBoost.asset");
        scannerBoost.displayName = "Advanced Scanner Active";
        scannerBoost.channel = EffectChannel.SpecialCases;
        scannerBoost.defaultDurationDays = -1;
        scannerBoost.ops = new List<EffectOp>
        {
            new EffectOp { type = EffectOpType.SetFlag, stringParam = "upgrade:adv_scanner" }
        };
        EditorUtility.SetDirty(scannerBoost);

        EffectSO payBoost = CreateAsset<EffectSO>($"{DataRoot}/Effects/Effect_Upgrade_PayBoost.asset");
        payBoost.displayName = "Diplomatic Contacts Pay Boost";
        payBoost.channel = EffectChannel.General;
        payBoost.defaultDurationDays = -1;
        payBoost.ops = new List<EffectOp>
        {
            new EffectOp { type = EffectOpType.PayRateBonus, floatParam = 0.1f }
        };
        EditorUtility.SetDirty(payBoost);

        EffectSO shopDiscount = CreateAsset<EffectSO>($"{DataRoot}/Effects/Effect_Upgrade_ShopDiscount.asset");
        shopDiscount.displayName = "Archive Access Shop Discount";
        shopDiscount.channel = EffectChannel.Shop;
        shopDiscount.defaultDurationDays = -1;
        shopDiscount.ops = new List<EffectOp>
        {
            new EffectOp { type = EffectOpType.ShopDiscountPercent, stringParam = "", floatParam = 10f }
        };
        EditorUtility.SetDirty(shopDiscount);

        UpgradeSO advScanner = CreateAsset<UpgradeSO>($"{DataRoot}/Upgrades/Upgrade_AdvancedScanner.asset");
        advScanner.id = "adv_scanner";
        advScanner.displayName = "Advanced Scanner";
        advScanner.description = "Reveals an additional clue category during inspections.";
        advScanner.cost = 200;
        advScanner.unlockEffect = scannerBoost;
        EditorUtility.SetDirty(advScanner);

        UpgradeSO diploContacts = CreateAsset<UpgradeSO>($"{DataRoot}/Upgrades/Upgrade_DiplomaticContacts.asset");
        diploContacts.id = "diplo_contacts";
        diploContacts.displayName = "Diplomatic Contacts";
        diploContacts.description = "Improves your pay rate through favors owed by well-placed contacts.";
        diploContacts.cost = 350;
        diploContacts.unlockEffect = payBoost;
        EditorUtility.SetDirty(diploContacts);

        UpgradeSO archiveAccess = CreateAsset<UpgradeSO>($"{DataRoot}/Upgrades/Upgrade_ArchiveAccess.asset");
        archiveAccess.id = "archive_access";
        archiveAccess.displayName = "Archive Access";
        archiveAccess.description = "Grants access to historical archives, reducing shop prices.";
        archiveAccess.cost = 300;
        archiveAccess.unlockEffect = shopDiscount;
        EditorUtility.SetDirty(archiveAccess);

        return (
            new[] { advScanner, diploContacts, archiveAccess },
            new[] { scannerBoost, payBoost, shopDiscount });
    }

    // -----------------------------------------------------------------
    // Slot outcomes
    // -----------------------------------------------------------------

    private static SlotOutcomeSO[] CreateSlotOutcomes()
    {
        SlotOutcomeSO jackpot = CreateSlotOutcome("jackpot_cash", "Jackpot",
            "The reels align — a windfall of credits!", 1f, 500, 0f, 0f, 0f, null, 0);

        SlotOutcomeSO smallWin = CreateSlotOutcome("small_win", "Small Win",
            "A modest payout clatters into the tray.", 4f, 100, 0f, 0f, 0f, null, 0);

        SlotOutcomeSO legendaryOmen = CreateSlotOutcome("legendary_omen", "Legendary Omen",
            "A strange resonance hints a legendary visitor approaches tomorrow.", 2f, 0, 0.1f, 0f, 0f, null, 0);

        SlotOutcomeSO forgeryWarning = CreateSlotOutcome("forgery_warning", "Forgery Warning",
            "Static crackles across the line — forgeries may slip through tomorrow.", 2f, 0, 0f, 0.1f, 0f, null, 0);

        SlotOutcomeSO bustedMachine = CreateSlotOutcome("busted_machine", "Busted Machine",
            "The machine sputters and eats your credits.", 3f, -50, 0f, 0f, 0f, null, 0);

        return new[] { jackpot, smallWin, legendaryOmen, forgeryWarning, bustedMachine };
    }

    private static SlotOutcomeSO CreateSlotOutcome(string id, string displayName, string resultLine, float weight, int moneyDelta, float legendaryChanceBonus, float forgeryChanceModifier, float payRateMultiplierDelta, EffectSO effect, int durationDaysOverride)
    {
        SlotOutcomeSO outcome = CreateAsset<SlotOutcomeSO>($"{DataRoot}/SlotOutcomes/SlotOutcome_{Capitalize(id)}.asset");
        outcome.id = id;
        outcome.displayName = displayName;
        outcome.resultLine = resultLine;
        outcome.weight = weight;
        outcome.moneyDelta = moneyDelta;
        outcome.legendaryChanceBonus = legendaryChanceBonus;
        outcome.forgeryChanceModifier = forgeryChanceModifier;
        outcome.payRateMultiplierDelta = payRateMultiplierDelta;
        outcome.effect = effect;
        outcome.durationDaysOverride = durationDaysOverride;
        EditorUtility.SetDirty(outcome);
        return outcome;
    }

    // -----------------------------------------------------------------
    // Endings
    // -----------------------------------------------------------------

    private static EndingSO[] CreateEndings(AttributeSO democracy, AttributeSO science, AttributeSO art)
    {
        EndingSO fired = CreateEnding("fired", "Fired",
            "Your superiors have seen enough. Badge, keys, and timeline clearance — all revoked, effective immediately.",
            EndingConditionType.Fired, null, 0f, 100);

        EndingSO bankrupt = CreateEnding("bankrupt", "Bankrupt",
            "The agency's coffers run dry, and with them, your position. The timeline will have to sort itself out.",
            EndingConditionType.Bankrupt, null, 0f, 90);

        EndingSO democracyTriumphant = CreateEnding("democracy_triumphant", "Democracy Triumphant",
            "Era after era, you tipped the balance toward open assemblies and free voices. Democratic ideals now ripple across the whole timeline.",
            EndingConditionType.AttrTotalAtLeast, democracy, 200f, 50);

        EndingSO scientificAge = CreateEnding("scientific_age", "Age of Science",
            "Your decisions accelerated discovery after discovery. The timeline hums with new machines, new power, new possibilities.",
            EndingConditionType.AttrTotalAtLeast, science, 200f, 50);

        EndingSO artisticGoldenAge = CreateEnding("artistic_golden_age", "Artistic Golden Age",
            "From classical marble to scroll paintings to bold new illustration, a golden age of art blossoms across every era you touched.",
            EndingConditionType.AttrTotalAtLeast, art, 200f, 50);

        EndingSO retirement = CreateEnding("retirement", "Retirement",
            "You made it. Years at the desk, thousands of cases, and the timeline still holding together. Time to put your feet up.",
            EndingConditionType.DayAtLeast, null, 15f, 10);

        return new[] { fired, bankrupt, democracyTriumphant, scientificAge, artisticGoldenAge, retirement };
    }

    private static EndingSO CreateEnding(string id, string displayName, string bodyText, EndingConditionType conditionType, AttributeSO attribute, float threshold, int priority)
    {
        EndingSO ending = CreateAsset<EndingSO>($"{DataRoot}/Endings/Ending_{Capitalize(id)}.asset");
        ending.id = id;
        ending.displayName = displayName;
        ending.bodyText = bodyText;
        ending.conditionType = conditionType;
        ending.attribute = attribute;
        ending.threshold = threshold;
        ending.priority = priority;
        EditorUtility.SetDirty(ending);
        return ending;
    }

    // -----------------------------------------------------------------
    // Day plans 2 + 3
    // -----------------------------------------------------------------

    private static DayPlanSO[] CreateDayPlans(EraSO greece, EraSO ngermany, EraSO japan, EraSO egypt, EraSO china, LegendarySO[] legendaries)
    {
        CaseBlueprintSO basicBlueprint = AssetDatabase.LoadAssetAtPath<CaseBlueprintSO>($"{DataRoot}/Case Blueprints/CaseBlueprint_Basic.asset");
        if (basicBlueprint == null)
            Debug.LogWarning("[Phase7ContentGenerator] CaseBlueprint_Basic.asset not found — expected from Phase 2/3.");

        // Legendaries array order from CreateLegendaries:
        // 0-2 Greece, 3-5 Germany, 6-8 Japan, 9-11 Egypt, 12-14 China,
        // 15 Wanderer_Idealist, 16 Wanderer_Tyrant, 17 Wanderer_Visionary,
        // 18 Wanderer_Alchemist, 19 Wanderer_Muse, 20 Wanderer_Iconoclast.

        DayPlanSO day2 = CreateAsset<DayPlanSO>($"{DataRoot}/Dayplan/DayPlan_2.asset");
        SetDayPlanScalars(day2, dayNumber: 2, visitorsCount: 6, legendaryBaseChance: 0.08f);
        SetArray(day2, "possibleBlueprints", basicBlueprint != null ? new Object[] { basicBlueprint } : System.Array.Empty<Object>());
        SetEraWeights(day2, new[]
        {
            (greece, 1f), (ngermany, 1f), (japan, 1f), (egypt, 0.5f), (china, 0.5f)
        });
        SetArray(day2, "availableLegendaries", new Object[]
        {
            legendaries[0], legendaries[1], legendaries[2],   // Greece
            legendaries[3], legendaries[4], legendaries[5],   // Germany
            legendaries[6], legendaries[7], legendaries[8],   // Japan
            legendaries[15],                                  // Wanderer_Idealist
            legendaries[17],                                  // Wanderer_Visionary
        });
        EditorUtility.SetDirty(day2);

        DayPlanSO day3 = CreateAsset<DayPlanSO>($"{DataRoot}/Dayplan/DayPlan_3.asset");
        SetDayPlanScalars(day3, dayNumber: 3, visitorsCount: 7, legendaryBaseChance: 0.1f);
        SetArray(day3, "possibleBlueprints", basicBlueprint != null ? new Object[] { basicBlueprint } : System.Array.Empty<Object>());
        SetEraWeights(day3, new[]
        {
            (greece, 1f), (ngermany, 1f), (japan, 1f), (egypt, 1f), (china, 1f)
        });
        var allLegendaries = new Object[legendaries.Length];
        for (int i = 0; i < legendaries.Length; i++)
            allLegendaries[i] = legendaries[i];
        SetArray(day3, "availableLegendaries", allLegendaries);
        EditorUtility.SetDirty(day3);

        return new[] { day2, day3 };
    }

    /// <summary>Sets DayPlanSO's private dayNumber/visitorsCount/legendaryBaseChance via SerializedObject.</summary>
    private static void SetDayPlanScalars(DayPlanSO plan, int dayNumber, int visitorsCount, float legendaryBaseChance)
    {
        var so = new SerializedObject(plan);
        so.FindProperty("dayNumber").intValue = dayNumber;
        so.FindProperty("visitorsCount").intValue = visitorsCount;
        so.FindProperty("legendaryBaseChance").floatValue = legendaryBaseChance;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(plan);
    }

    // -----------------------------------------------------------------
    // Wire everything into ContentLibrary_Main
    // -----------------------------------------------------------------

    private static void WireContentLibrary(
        AttributeSO[] attributes, EraSO[] newEras, NationSO[] nations,
        NationEraProfileSO[] profiles, ArchetypeSO[] archetypes, LegendarySO[] legendaries,
        TimelineTriggerSO[] triggers, EffectSO[] triggerEffects,
        UpgradeSO[] upgrades, EffectSO[] upgradeEffects,
        SlotOutcomeSO[] slotOutcomes, EndingSO[] endings, DayPlanSO[] dayPlans,
        EffectSO[] dominanceEffects)
    {
        ContentLibrarySO library = AssetDatabase.LoadAssetAtPath<ContentLibrarySO>($"{DataRoot}/Content Library/ContentLibrary_Main.asset");
        if (library == null)
        {
            Debug.LogError("[Phase7ContentGenerator] ContentLibrary_Main.asset not found — cannot wire content.");
            return;
        }

        // New top-level Timeline Variation arrays (previously empty/unused).
        SetArray(library, "attributes", attributes);
        SetArray(library, "nations", nations);
        SetArray(library, "nationEraProfiles", profiles);
        SetArray(library, "archetypes", archetypes);
        SetArray(library, "timelineTriggers", triggers);
        SetArray(library, "slotOutcomes", slotOutcomes);
        SetArray(library, "endings", endings);

        // Append to existing arrays so Phase 2/3 content (Greece/NGermany eras,
        // DayPlan_1, etc.) is preserved.
        AppendArray(library, "eras", newEras);
        AppendArray(library, "legendaries", legendaries);
        AppendArray(library, "upgrades", upgrades);
        AppendArray(library, "dayPlans", dayPlans);

        var allEffects = new List<Object>();
        allEffects.AddRange(dominanceEffects);
        allEffects.AddRange(triggerEffects);
        allEffects.AddRange(upgradeEffects);
        AppendArray(library, "effects", allEffects.ToArray());

        EditorUtility.SetDirty(library);
    }
}
