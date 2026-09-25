using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generate World's culture part (piece 6): from world_source.json "ui" and
/// countries[].culture it checks and writes the UI string tables
/// (Culture/Strings_{language}), one theme per culture plus the neutral one
/// (Culture/Theme_{id}), and a text-free placeholder wallpaper wherever the
/// art is missing (never replacing a file). Every theme must pass the
/// contrast check, or the whole generation aborts. Part of <see cref="WorldContentGenerator"/>.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>Folder of the generated culture assets (themes and string tables).</summary>
    private const string CultureFolder = WorldRoot + "/Culture";

    /// <summary>The neutral theme's culture id.</summary>
    private const string NeutralCultureId = "neutral";

    /// <summary>Placeholder wallpaper size (the XP wallpaper's).</summary>
    private const int WallpaperWidth = 960, WallpaperHeight = 540;

    /// <summary>The placeholder painter's colours and the seeds they default to (the art list may name others).</summary>
    private static readonly (string art, string seed)[] WallpaperColours =
    {
        ("skyTop", "chrome"), ("skyBottom", "wallpaper"), ("groundLow", "chromeDeep"), ("groundHigh", "accent"), ("cloud", "paper")
    };

    /// <summary>Everything the culture part writes, checked.</summary>
    private sealed class CulturePlan
    {
        public UiData ui;
        public List<UiStringEntry> reading;
        public List<(LanguageData data, List<UiStringEntry> entries)> languages = new List<(LanguageData, List<UiStringEntry>)>();
        public ThemePlan neutral;
        public List<ThemePlan> themes = new List<ThemePlan>();
    }

    /// <summary>One theme, resolved and checked.</summary>
    private sealed class ThemePlan
    {
        public string id;
        public CultureData data;
        public List<ResolvedRole> roles;
        public Rgba ringDark, ringLight;
        public FontStyles headingAdd, buttonAdd;
        public Rgba[] wallpaperColours;
    }

    /// <summary>
    /// Checks the culture source and resolves every theme (errors added, nothing
    /// written): the ui section, the string tables (UiStrings.TableProblems), the
    /// keys the code derives from Domain enums, each culture's language, fonts,
    /// styles and art path, the palettes (Palette.Resolve and Missing) and the
    /// contrast of every theme (Contrast.Problems over its pairs and the
    /// diegetic pairs). Returns null when the ui section is missing.
    /// </summary>
    private static CulturePlan PlanCulture(WorldSource src, List<string> errors)
    {
        UiData ui = src.ui;
        if (ui == null)
        {
            errors.Add("world_source.json has no \"ui\" section (the culture themes and UI strings, piece 6).");
            return null;
        }

        var plan = new CulturePlan { ui = ui };
        if (string.IsNullOrWhiteSpace(ui.readingLanguage))
            errors.Add("ui.readingLanguage is blank.");
        if (ui.contrast == null)
            errors.Add("ui.contrast is missing.");
        if (AssetDatabase.LoadAssetAtPath<Font>(ui.latinFallbackFont ?? string.Empty) == null)
            errors.Add($"ui.latinFallbackFont '{ui.latinFallbackFont}' is not a font asset.");

        // --- String tables ---
        plan.reading = (ui.strings ?? Array.Empty<StringData>()).Select(s => ReadingEntry(s, errors)).ToList();
        foreach (string p in UiStrings.TableProblems(plan.reading, null, false))
            errors.Add($"ui.strings: {p}");
        var keys = new HashSet<string>(plan.reading.Where(e => e.key != null).Select(e => e.key));
        foreach (ClueCategory c in Enum.GetValues(typeof(ClueCategory)))
            RequireKey(keys, ClueLabels.Key(c), "ClueLabels.Key", errors);
        foreach (DiscrepancyProof proof in Enum.GetValues(typeof(DiscrepancyProof)))
            foreach (EvidenceKind kind in Enum.GetValues(typeof(EvidenceKind)))
                RequireKey(keys, Discrepancy.ReportKeyFor(proof, kind), "Discrepancy.ReportKeyFor", errors);

        foreach (LanguageData l in ui.languages ?? Array.Empty<LanguageData>())
        {
            if (l == null || string.IsNullOrWhiteSpace(l.language) || l.language == ui.readingLanguage || plan.languages.Any(x => x.data.language == l.language))
            {
                errors.Add($"ui.languages: '{l?.language}' is blank, the reading language or listed twice.");
                continue;
            }
            List<UiStringEntry> entries = (l.entries ?? Array.Empty<EntryData>()).Select(e => new UiStringEntry { key = e?.key, text = e?.text }).ToList();
            foreach (string p in UiStrings.TableProblems(plan.reading, entries, l.rtl))
                errors.Add($"ui.languages '{l.language}': {p}");
            plan.languages.Add((l, entries));
        }

        // --- Themes ---
        List<PaletteRule> map = (ui.paletteMap ?? Array.Empty<RoleRuleData>()).Select(r => PaletteRuleOf(r, errors)).ToList();
        Dictionary<string, Rgba> baseSeeds = Seeds(ui.neutral?.seeds, "ui.neutral", errors);
        plan.neutral = PlanTheme(NeutralCultureId, ui.neutral, map, baseSeeds, true, null, plan, errors);

        foreach (CountryData country in src.countries)
        {
            if (country.culture == null)
            {
                errors.Add($"Country '{country.id}' has no culture block (piece 6).");
                continue;
            }
            ThemePlan theme = PlanTheme(country.id, country.culture, map, baseSeeds, false, plan.neutral, plan, errors);
            if (theme != null)
                plan.themes.Add(theme);
        }
        return plan;
    }

    /// <summary>Resolves and checks one theme; null when its block is missing.</summary>
    private static ThemePlan PlanTheme(string id, CultureData data, List<PaletteRule> map, Dictionary<string, Rgba> baseSeeds, bool isNeutral,
                                       ThemePlan neutral, CulturePlan plan, List<string> errors)
    {
        string where = isNeutral ? "ui.neutral" : $"Culture '{id}'";
        if (data == null)
        {
            errors.Add($"{where} is missing.");
            return null;
        }

        var theme = new ThemePlan { id = id, data = data };
        Dictionary<string, Rgba> seeds = isNeutral ? baseSeeds : Seeds(data.seeds, where, errors);

        if (data.language != plan.ui.readingLanguage && plan.languages.All(l => l.data.language != data.language))
            errors.Add($"{where}: language '{data.language}' has no table in ui.languages.");
        if (!TryStyle(data.headingAdd, out theme.headingAdd) || !TryStyle(data.buttonAdd, out theme.buttonAdd))
            errors.Add($"{where}: headingAdd '{data.headingAdd}' / buttonAdd '{data.buttonAdd}' is not a FontStyles name.");
        if (data.runtimeFont && NeedsOsFont(plan, data.language) && (data.fonts == null || data.fonts.Length == 0))
            errors.Add($"{where}: its labels hold characters the Latin fallback cannot draw, so it needs at least one font candidate.");
        if (data.fonts != null && data.fonts.Any(f => f == null || string.IsNullOrWhiteSpace(f.file) && string.IsNullOrWhiteSpace(f.family)))
            errors.Add($"{where}: a font candidate names neither a file nor a family.");
        if (string.IsNullOrWhiteSpace(data.wallpaper) || !data.wallpaper.StartsWith("Assets/Art/") || !data.wallpaper.EndsWith(".png"))
            errors.Add($"{where}: wallpaper '{data.wallpaper}' must be a .png under Assets/Art/.");

        var problems = new List<string>();
        theme.roles = Palette.Resolve(map, baseSeeds, seeds, data.overrides ?? Array.Empty<PaletteOverride>(), isNeutral, problems);
        foreach (ThemeRoleId missing in Palette.Missing(theme.roles, isNeutral))
            problems.Add($"role '{missing}' has no colour (add a ui.paletteMap rule{(isNeutral ? " or a neutral override" : string.Empty)}).");
        theme.ringDark = Seed(seeds, baseSeeds, "ringDark", where, problems);
        theme.ringLight = Seed(seeds, baseSeeds, "ringLight", where, problems);

        Dictionary<string, Rgba> art = Seeds(data.art, where, problems);
        theme.wallpaperColours = WallpaperColours.Select(c => art.TryGetValue(c.art, out Rgba v) ? v : Seed(seeds, baseSeeds, c.seed, where, problems)).ToArray();

        if (plan.ui.contrast != null)
        {
            List<ResolvedRole> diegetic = isNeutral ? theme.roles : neutral?.roles;
            var pairs = Palette.Pairs(theme.roles).Concat(Palette.DiegeticPairs(theme.roles, diegetic)).ToList();
            problems.AddRange(Contrast.Problems(pairs, theme.ringDark, theme.ringLight, plan.ui.contrast));
        }
        errors.AddRange(problems.Select(p => $"{where} theme: {p}"));
        return theme;
    }

    /// <summary>Writes the string tables, the themes and any missing wallpaper; returns the neutral theme, the culture themes (country order) and the tables.</summary>
    private static (ThemeSO neutral, ThemeSO[] themes, UiStringTableSO[] tables) WriteCulture(CulturePlan plan, HashSet<string> written)
    {
        var tables = new List<UiStringTableSO> { MakeStringTable(plan.ui.readingLanguage, false, plan.reading, written) };
        tables.AddRange(plan.languages.Select(l => MakeStringTable(l.data.language, l.data.rtl, l.entries, written)));

        ThemeSO neutral = MakeTheme(plan.neutral, written);
        ThemeSO[] themes = plan.themes.Select(t => MakeTheme(t, written)).ToArray();
        return (neutral, themes, tables.ToArray());
    }

    /// <summary>Creates or updates Culture/Strings_{language}.</summary>
    private static UiStringTableSO MakeStringTable(string language, bool rightToLeft, List<UiStringEntry> entries, HashSet<string> written)
    {
        UiStringTableSO table = LoadOrCreate<UiStringTableSO>($"{CultureFolder}/Strings_{language}.asset", written);
        table.language = language;
        table.rightToLeft = rightToLeft;
        table.entries = entries.Select(e => new UiStringEntry { key = e.key, text = e.text, gloss = e.gloss, tier = e.tier }).ToList();
        EditorUtility.SetDirty(table);
        return table;
    }

    /// <summary>Creates or updates Culture/Theme_{id} (its wallpaper created first when missing).</summary>
    private static ThemeSO MakeTheme(ThemePlan plan, HashSet<string> written)
    {
        CultureData data = plan.data;
        Sprite wallpaper = EnsureWallpaper(data.wallpaper, plan.wallpaperColours);

        ThemeSO theme = LoadOrCreate<ThemeSO>($"{CultureFolder}/Theme_{plan.id}.asset", written);
        theme.cultureId = plan.id;
        theme.displayName = data.displayName;
        theme.language = data.language;
        theme.runtimeFont = data.runtimeFont;
        theme.fonts = (data.fonts ?? Array.Empty<FontData>())
            .Select(f => new FontCandidate { file = f.file ?? string.Empty, face = f.face, family = f.family ?? string.Empty, style = string.IsNullOrWhiteSpace(f.style) ? "Regular" : f.style })
            .ToList();
        theme.headingAdd = plan.headingAdd;
        theme.buttonAdd = plan.buttonAdd;
        theme.stripItalic = data.stripItalic;
        theme.palette = plan.roles.Select(r => new PaletteEntry
        {
            role = r.Role,
            hasFill = r.Fill.HasValue,
            fill = r.Fill.HasValue ? ToColor(r.Fill.Value) : Color.clear,
            hasInk = r.Ink.HasValue,
            ink = r.Ink.HasValue ? ToColor(r.Ink.Value) : Color.clear,
            textClass = r.TextClass
        }).ToList();
        theme.ringDark = ToColor(plan.ringDark);
        theme.ringLight = ToColor(plan.ringLight);
        theme.wallpaper = wallpaper;
        EditorUtility.SetDirty(theme);
        return theme;
    }

    /// <summary>
    /// The wallpaper sprite at a path: a text-free placeholder is painted when
    /// no file exists (an existing file is never replaced); the import is kept
    /// a single sprite with mipmaps (the live monitor shows it small, K18).
    /// </summary>
    private static Sprite EnsureWallpaper(string path, Rgba[] colours)
    {
        if (!System.IO.File.Exists(System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, path)))
        {
            PlaceholderPng.Write(path, WallpaperWidth, WallpaperHeight,
                                 CulturePlaceholders.Wallpaper(WallpaperWidth, WallpaperHeight, colours[0], colours[1], colours[2], colours[3], colours[4]));
            Debug.Log($"[WorldContentGenerator] No art at {path}; generated a text-free placeholder. Replace the PNG in place (keep its .meta) with final text-free art.");
        }

        if (AssetImporter.GetAtPath(path) is TextureImporter imp &&
            (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || !imp.mipmapEnabled))
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    /// <summary>The reading table's entry of a source string (gloss and tier parsed).</summary>
    private static UiStringEntry ReadingEntry(StringData s, List<string> errors)
    {
        var e = new UiStringEntry { key = s?.key, text = s?.text };
        if (!ParseEnum(string.IsNullOrEmpty(s?.gloss) ? nameof(GlossStyle.None) : s.gloss, out e.gloss))
            errors.Add($"ui.strings '{s?.key}': gloss '{s?.gloss}' is not None, Below or Inline.");
        if (!ParseEnum(string.IsNullOrEmpty(s?.tier) ? nameof(StringTier.Full) : s.tier, out e.tier))
            errors.Add($"ui.strings '{s?.key}': tier '{s?.tier}' is not Full or Flavour.");
        return e;
    }

    /// <summary>A palette-map rule (its text class parsed).</summary>
    private static PaletteRule PaletteRuleOf(RoleRuleData r, List<string> errors)
    {
        var rule = new PaletteRule { role = r?.role, fill = r?.fill, alpha = r?.alpha ?? 1f, ink = r?.ink };
        if (!ParseEnum(string.IsNullOrEmpty(r?.textClass) ? nameof(ContrastClass.None) : r.textClass, out rule.textClass))
            errors.Add($"ui.paletteMap '{r?.role}': textClass '{r?.textClass}' is not a ContrastClass.");
        return rule;
    }

    /// <summary>Named hex colours by name; bad hex or a repeated name adds an error.</summary>
    private static Dictionary<string, Rgba> Seeds(NamedHexData[] named, string where, List<string> errors)
    {
        var seeds = new Dictionary<string, Rgba>(StringComparer.Ordinal);
        foreach (NamedHexData n in named ?? Array.Empty<NamedHexData>())
        {
            if (n == null || string.IsNullOrWhiteSpace(n.name) || seeds.ContainsKey(n.name) || !Rgba.TryParseHex(n.hex, out Rgba c))
                errors.Add($"{where}: colour '{n?.name}' = '{n?.hex}' is blank, repeated or not #RRGGBB(AA).");
            else
                seeds[n.name] = c;
        }
        return seeds;
    }

    /// <summary>A seed by name (the theme's, else the neutral's); a missing one adds a problem.</summary>
    private static Rgba Seed(Dictionary<string, Rgba> seeds, Dictionary<string, Rgba> baseSeeds, string name, string where, List<string> problems)
    {
        if (seeds.TryGetValue(name, out Rgba c) || baseSeeds.TryGetValue(name, out c))
            return c;
        problems.Add($"no seed '{name}'.");
        return default;
    }

    /// <summary>Adds an error when a key the code derives is not in the reading table.</summary>
    private static void RequireKey(HashSet<string> keys, string key, string source, List<string> errors)
    {
        if (!keys.Contains(key))
            errors.Add($"ui.strings has no '{key}', which {source} asks for.");
    }

    /// <summary>A FontStyles addition by name (empty = Normal).</summary>
    private static bool TryStyle(string name, out FontStyles style)
    {
        style = FontStyles.Normal;
        return string.IsNullOrWhiteSpace(name) || ParseEnum(name, out style);
    }

    /// <summary>True when a language's labels need an OS font (CultureChoice.NeedsOsFont).</summary>
    private static bool NeedsOsFont(CulturePlan plan, string language)
    {
        List<UiStringEntry> entries = plan.languages.FirstOrDefault(l => l.data.language == language).entries;
        return entries != null && entries.Any(e => CultureChoice.NeedsOsFont(e.text));
    }

    /// <summary>An engine colour from a theme colour.</summary>
    private static Color ToColor(Rgba c) => new Color(c.R, c.G, c.B, c.A);

    // -----------------------------
    // Source file shape (JsonUtility)
    // -----------------------------

    /// <summary>world_source.json "ui".</summary>
    [Serializable] private sealed class UiData
    {
        public string readingLanguage;
        public int glossPercent = 60;
        public float labelMinScale = 0.55f;
        public ContrastRules contrast;
        public string latinFallbackFont;
        public RoleRuleData[] paletteMap;
        public CultureData neutral;
        public StringData[] strings;
        public LanguageData[] languages;
    }

    /// <summary>One palette-map rule (textClass as a ContrastClass name).</summary>
    [Serializable] private sealed class RoleRuleData { public string role; public string fill; public float alpha = 1f; public string ink; public string textClass; }

    /// <summary>A culture block (countries[].culture) or ui.neutral.</summary>
    [Serializable] private sealed class CultureData
    {
        public string displayName;
        public string language;
        public bool runtimeFont;
        public FontData[] fonts;
        public string headingAdd;
        public string buttonAdd;
        public bool stripItalic;
        public NamedHexData[] seeds;
        public PaletteOverride[] overrides;
        public NamedHexData[] art;
        public string wallpaper;
    }

    /// <summary>One font candidate.</summary>
    [Serializable] private sealed class FontData { public string file; public int face; public string family; public string style; }

    /// <summary>A named colour.</summary>
    [Serializable] private sealed class NamedHexData { public string name; public string hex; }

    /// <summary>One reading-table string (gloss and tier as names).</summary>
    [Serializable] private sealed class StringData { public string key; public string text; public string gloss; public string tier; }

    /// <summary>One culture language's table.</summary>
    [Serializable] private sealed class LanguageData { public string language; public bool rtl; public EntryData[] entries; }

    /// <summary>One translated string.</summary>
    [Serializable] private sealed class EntryData { public string key; public string text; }
}
