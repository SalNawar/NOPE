using System;
using System.Collections;
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
/// found through Font.GetPathsToOSFonts, then the family name), ending
/// in a runtime LiberationSans asset built in memory from the project's own
/// TTF (Greece's font). Fonts are read from the player's system at runtime and
/// never saved, never added to TMP_Settings and never chained to the tracked
/// LiberationSans assets. One asset per font file and face (or family and
/// style), shared by every key that resolves to it (a culture and a
/// translation script on the same OS font, CODE-81); results are cached per key (a culture id, or
/// "script:{id}") for the session; Dispose destroys every asset created.
/// Resolving never bakes a glyph: whether a font draws a sample is read from
/// the font file itself (FontEngine), and the atlas fills as text shows or,
/// for a culture the papers read, across frames (<see cref="Prewarm"/>), so
/// the first CJK culture of a session no longer bakes ~700 glyphs in one
/// frame (CODE-81).
/// </summary>
public sealed class RuntimeFonts : IDisposable
{
    /// <summary>The atlas parameters for OS fonts: glyphs sampled at 48 pt (TMP's 90 made a CJK atlas of 8-10 pages, CODE-81) with TMP's padding ratio, on 1024 px pages.</summary>
    private const int SamplingPointSize = 48, AtlasPadding = 5, AtlasSize = 1024;

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
    private readonly Dictionary<string, TMP_FontAsset> _byFile = new Dictionary<string, TMP_FontAsset>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<TMP_FontAsset, Func<bool>> _faces = new Dictionary<TMP_FontAsset, Func<bool>>();
    private TMP_FontAsset _latin;
    private bool _latinTried;
    private string[] _osPaths;

    /// <summary>A cache whose chains end in a runtime asset of <paramref name="latinSource"/> (the project's LiberationSans.ttf).</summary>
    public RuntimeFonts(Font latinSource)
    {
        _latinSource = latinSource;
    }

    /// <summary>The theme's font: none for a theme without runtime fonts; else its candidates resolved under its culture id (<paramref name="sample"/> is built only when the culture is not cached yet).</summary>
    public Result Resolve(ThemeSO theme, Func<string> sample)
    {
        if (theme == null || !theme.runtimeFont)
            return new Result(null, "default", true, string.Empty, string.Empty);
        return Resolve(theme.cultureId, theme.fonts, sample);
    }

    /// <summary>
    /// The font for a key (a culture id, or "script:{id}" for a translation
    /// script): the first candidate that loads (with the runtime Latin asset as
    /// its fallback), or the Latin asset itself; then whether it draws
    /// <paramref name="sample"/>'s text, read from the font files (nothing is
    /// baked). Cached per key, so the first sample for a key must hold
    /// everything it will draw; the sample is built only on that first call
    /// (audit R4-018: building it on every theme refresh cost a string table's
    /// worth of work each time).
    /// </summary>
    public Result Resolve(string key, IReadOnlyList<FontCandidate> candidates, Func<string> sample)
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
                    asset = FromFile(path, c.face, latin);
                if (asset != null)
                {
                    name = "file " + c.file;
                    break;
                }
            }
            if (!string.IsNullOrWhiteSpace(c.family))
            {
                tried.Add("family " + c.family);
                asset = FromFamily(c.family, string.IsNullOrWhiteSpace(c.style) ? "Regular" : c.style, latin);
                if (asset != null)
                {
                    name = "family " + c.family;
                    break;
                }
            }
        }

        if (asset == null)
        {
            asset = latin;
            name = "LiberationSans";
        }

        bool covers = false;
        string missing = "(no font)";
        if (asset != null)
        {
            List<uint> lacking = Lacking(asset, sample?.Invoke() ?? string.Empty);
            covers = lacking.Count == 0;
            missing = string.Join(" ", lacking.Select(u => $"U+{u:X4}"));
        }

        var result = new Result(asset, name, covers, missing, string.Join(", ", tried.Append("LiberationSans")));
        _cache[key] = result;
        return result;
    }

    /// <summary>
    /// Adds <paramref name="characters"/> to a dynamic <paramref name="asset"/>'s
    /// atlas, <paramref name="perFrame"/> at a time, one batch a frame (a
    /// coroutine: CODE-81, the culture's glyphs baked off the load's frame;
    /// a text that shows one sooner bakes just that one). Stops when the
    /// asset is gone (Dispose).
    /// </summary>
    public IEnumerator Prewarm(TMP_FontAsset asset, string characters, int perFrame)
    {
        if (asset == null || asset.atlasPopulationMode == AtlasPopulationMode.Static)
            yield break;
        string chars = new string((characters ?? string.Empty).Where(c => !char.IsControl(c) && !char.IsSurrogate(c)).Distinct()
                                                               .Where(c => !asset.characterLookupTable.ContainsKey(c)).ToArray());
        int step = Mathf.Max(1, perFrame);
        for (int i = 0; i < chars.Length; i += step)
        {
            if (asset == null)
                yield break;
            asset.TryAddCharacters(chars.Substring(i, Mathf.Min(step, chars.Length - i)), out _);
            yield return null;
        }
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
        _byFile.Clear();
        _faces.Clear();
        _latin = null;
        _latinTried = false;
    }

    /// <summary>The runtime asset of a font file's face, created once per file and face (shared by every key), with the Latin asset as its fallback; null when the file does not load.</summary>
    private TMP_FontAsset FromFile(string path, int face, TMP_FontAsset latin)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        return Shared("file:" + path + "#" + face, () => TMP_FontAsset.CreateFontAsset(path, face, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize),
                      () => FontEngine.LoadFontFace(path, SamplingPointSize, face) == FontEngineError.Success, latin);
    }

    /// <summary>The runtime asset of an installed family's style (TMP finds its file), created once per family and style, with the Latin asset as its fallback; null when no such font is installed.</summary>
    private TMP_FontAsset FromFamily(string family, string style, TMP_FontAsset latin) =>
        Shared("family:" + family + "/" + style, () => TMP_FontAsset.CreateFontAsset(family, style, SamplingPointSize),
               () => FontEngine.LoadFontFace(family, style) == FontEngineError.Success, latin);

    /// <summary>The asset under <paramref name="id"/>, made once by <paramref name="create"/> (named, never saved, the Latin asset its fallback), its face loaded for coverage by <paramref name="loadFace"/>.</summary>
    private TMP_FontAsset Shared(string id, Func<TMP_FontAsset> create, Func<bool> loadFace, TMP_FontAsset latin)
    {
        if (_byFile.TryGetValue(id, out TMP_FontAsset shared))
            return shared;
        TMP_FontAsset asset = create();
        if (asset == null)
            return null;
        asset.name = "Runtime " + id;
        asset.hideFlags = HideFlags.DontSave;
        if (latin != null)
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { latin };
        _created.Add(asset);
        _byFile[id] = asset;
        _faces[asset] = loadFace;
        return asset;
    }

    /// <summary>The sample's characters (line breaks and other controls aside) that neither <paramref name="asset"/>'s face nor its Latin fallback's draws, read from the font files.</summary>
    private List<uint> Lacking(TMP_FontAsset asset, string sample)
    {
        List<uint> codes = sample.Where(c => !char.IsControl(c) && !char.IsSurrogate(c)).Distinct()
                                 .Select(c => (uint)c).Where(u => !asset.characterLookupTable.ContainsKey(u)).ToList();
        codes = NotIn(asset, codes);
        if (asset != _latin)
            codes = NotIn(_latin, codes);
        return codes;
    }

    /// <summary>The codes <paramref name="asset"/>'s font face has no glyph for (all of them when its face does not load).</summary>
    private List<uint> NotIn(TMP_FontAsset asset, List<uint> codes)
    {
        if (codes.Count == 0 || asset == null || !LoadFace(asset))
            return codes;
        return codes.Where(u => !(FontEngine.TryGetGlyphIndex(u, out uint glyph) && glyph != 0)).ToList();
    }

    /// <summary>Loads <paramref name="asset"/>'s face into the font engine (TMP loads its own again before it adds glyphs).</summary>
    private bool LoadFace(TMP_FontAsset asset)
    {
        if (_faces.TryGetValue(asset, out Func<bool> load))
            return load();
        return asset == _latin && _latinSource != null && FontEngine.LoadFontFace(_latinSource, SamplingPointSize) == FontEngineError.Success;
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
