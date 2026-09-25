using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

/// <summary>
/// The culture themes' fonts (piece 6 U8, R5) and the translation scripts'
/// fonts (piece 9): each key's OS font candidates tried in order (a font file
/// found through Font.GetPathsToOSFonts, then the family name), ending in a
/// runtime LiberationSans asset built in memory from the project's own TTF
/// (Greece's font). Fonts are read from the player's system at runtime and
/// never saved, never added to TMP_Settings and never chained to the tracked
/// LiberationSans assets. Results are cached per key (a culture id, or
/// "script:{id}") for the session; Dispose destroys every asset created.
/// </summary>
public sealed class RuntimeFonts : IDisposable
{
    /// <summary>TMP's own atlas parameters for OS fonts (the family-name overload uses them), passed unchanged to the path overload.</summary>
    private const int SamplingPointSize = 90, AtlasPadding = 9, AtlasSize = 1024;

    /// <summary>One theme's font.</summary>
    public readonly struct Result
    {
        /// <summary>The text font; null = keep the project's default TMP font.</summary>
        public readonly TMP_FontAsset Asset;

        /// <summary>What was resolved ("default", "file msyh.ttc", "family Georgia", "LiberationSans").</summary>
        public readonly string Name;

        /// <summary>True when the font (with its fallbacks) draws every sample character.</summary>
        public readonly bool Covers;

        /// <summary>The sample characters no font drew, as U+XXXX codes.</summary>
        public readonly string Missing;

        /// <summary>The candidates tried, for warnings.</summary>
        public readonly string Tried;

        /// <summary>A result.</summary>
        public Result(TMP_FontAsset asset, string name, bool covers, string missing, string tried)
        {
            Asset = asset;
            Name = name;
            Covers = covers;
            Missing = missing;
            Tried = tried;
        }
    }

    private readonly Font _latinSource;
    private readonly Dictionary<string, Result> _cache = new Dictionary<string, Result>();
    private readonly List<TMP_FontAsset> _created = new List<TMP_FontAsset>();
    private TMP_FontAsset _latin;
    private bool _latinTried;
    private string[] _osPaths;

    /// <summary>A cache whose chains end in a runtime asset of <paramref name="latinSource"/> (the project's LiberationSans.ttf).</summary>
    public RuntimeFonts(Font latinSource)
    {
        _latinSource = latinSource;
    }

    /// <summary>The theme's font: none for a theme without runtime fonts; else its candidates resolved under its culture id.</summary>
    public Result Resolve(ThemeSO theme, string sample)
    {
        if (theme == null || !theme.runtimeFont)
            return new Result(null, "default", true, string.Empty, string.Empty);
        return Resolve(theme.cultureId, theme.fonts, sample);
    }

    /// <summary>
    /// The font for a key (a culture id, or "script:{id}" for a translation
    /// script): the first candidate that loads (with the runtime Latin asset as
    /// its fallback), or the Latin asset itself; then whether it draws
    /// <paramref name="sample"/> (which also prewarms its atlas). Cached per key,
    /// so the first sample for a key must hold everything it will draw.
    /// </summary>
    public Result Resolve(string key, IReadOnlyList<FontCandidate> candidates, string sample)
    {
        key ??= string.Empty;
        if (_cache.TryGetValue(key, out Result cached))
            return cached;

        TMP_FontAsset latin = Latin();
        TMP_FontAsset asset = null;
        string name = null;
        var tried = new List<string>();
        foreach (FontCandidate c in candidates ?? new List<FontCandidate>())
        {
            if (c == null)
                continue;
            if (!string.IsNullOrWhiteSpace(c.file))
            {
                tried.Add("file " + c.file);
                string path = OsPaths().FirstOrDefault(p => string.Equals(Path.GetFileName(p), c.file, StringComparison.OrdinalIgnoreCase));
                if (path != null)
                    asset = TMP_FontAsset.CreateFontAsset(path, c.face, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize);
                if (asset != null)
                {
                    name = "file " + c.file;
                    break;
                }
            }
            if (!string.IsNullOrWhiteSpace(c.family))
            {
                tried.Add("family " + c.family);
                asset = TMP_FontAsset.CreateFontAsset(c.family, string.IsNullOrWhiteSpace(c.style) ? "Regular" : c.style);
                if (asset != null)
                {
                    name = "family " + c.family;
                    break;
                }
            }
        }

        if (asset != null)
        {
            asset.name = $"Runtime {key} ({name})";
            asset.hideFlags = HideFlags.DontSave;
            _created.Add(asset);
            if (latin != null)
                asset.fallbackFontAssetTable = new List<TMP_FontAsset> { latin };
        }
        else
        {
            asset = latin;
            name = "LiberationSans";
        }

        bool covers = false;
        string missing = "(no font)";
        if (asset != null)
        {
            covers = asset.HasCharacters(sample ?? string.Empty, out uint[] missingCodes, true, true);
            missing = missingCodes == null ? string.Empty : string.Join(" ", missingCodes.Select(u => $"U+{u:X4}"));
        }

        var result = new Result(asset, name, covers, missing, string.Join(", ", tried.Append("LiberationSans")));
        _cache[key] = result;
        return result;
    }

    /// <summary>Destroys every font asset created (with its atlas textures and material).</summary>
    public void Dispose()
    {
        foreach (TMP_FontAsset asset in _created)
        {
            if (asset == null)
                continue;
            if (asset.atlasTextures != null)
                foreach (Texture2D t in asset.atlasTextures)
                    Release(t);
            Release(asset.material);
            Release(asset);
        }
        _created.Clear();
        _cache.Clear();
        _latin = null;
        _latinTried = false;
    }

    /// <summary>Destroys an object (immediately outside play mode, where Destroy is not allowed).</summary>
    private static void Release(Object o)
    {
        if (o == null)
            return;
        if (Application.isPlaying)
            Object.Destroy(o);
        else
            Object.DestroyImmediate(o);
    }

    /// <summary>The runtime LiberationSans asset (created once; null with a warning when the source is missing).</summary>
    private TMP_FontAsset Latin()
    {
        if (_latinTried)
            return _latin;
        _latinTried = true;

        if (_latinSource != null)
            _latin = TMP_FontAsset.CreateFontAsset(_latinSource);
        if (_latin == null)
        {
            Debug.LogWarning("[RuntimeFonts] No runtime Latin fallback font (ContentLibrarySO cultureUi.latinFallbackFont is empty or unreadable); culture fonts end without it. Run Tools > TimeDesk > Generate World.");
            return null;
        }

        _latin.name = "Runtime LiberationSans";
        _latin.hideFlags = HideFlags.DontSave;
        _created.Add(_latin);
        return _latin;
    }

    /// <summary>The installed OS font files (read once).</summary>
    private string[] OsPaths() => _osPaths ?? (_osPaths = Font.GetPathsToOSFonts() ?? Array.Empty<string>());
}
