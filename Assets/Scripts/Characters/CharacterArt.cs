using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Loads character layer sprites by key (LookKeys): final art from
/// Resources/Characters/{key} (Assets/Art/Characters/Resources/Characters,
/// imported by CharacterArtImporter), else a runtime placeholder drawn by
/// LayerPlaceholder and kept in memory only (nothing is written to disk).
/// Also makes each layer's passport-photo crop (LookCanvas.PhotoRect). Every
/// sprite is one unit tall with its pivot at the feet. Retain keeps only the
/// current traveller's textures; Dispose releases everything.
/// </summary>
public sealed class CharacterArt : IDisposable
{
    /// <summary>The Resources sub-folder character art loads from.</summary>
    public const string ResourcesFolder = "Characters";

    /// <summary>Where final character art goes ({key}.png).</summary>
    public const string AssetFolder = "Assets/Art/Characters/Resources/" + ResourcesFolder;

    /// <summary>Saturation of garment placeholders.</summary>
    private const float GarmentSaturation = 0.6f;

    /// <summary>Brightness of the earliest era's garments; each later era is darker by <see cref="EraShadeStep"/>.</summary>
    private const float FirstEraShade = 0.92f;

    /// <summary>How much darker each later era's garments are.</summary>
    private const float EraShadeStep = 0.11f;

    /// <summary>One key's sprites.</summary>
    private sealed class Entry
    {
        /// <summary>The full-canvas sprite.</summary>
        public Sprite Full;

        /// <summary>The photo crop (made on first use).</summary>
        public Sprite Photo;

        /// <summary>The placeholder's texture (null for final art).</summary>
        public Texture2D Placeholder;
    }

    private readonly Dictionary<string, Entry> _cache = new Dictionary<string, Entry>();
    private readonly Dictionary<string, float> _nationHue = new Dictionary<string, float>();
    private readonly Dictionary<string, int> _eraOrder = new Dictionary<string, int>();
    private bool _toldAboutPlaceholders;

    /// <summary>Reads the placeholder colours from the library: a nation's index in its nations sets the hue, an era's order the shade.</summary>
    public CharacterArt(ContentLibrarySO library)
    {
        if (library == null)
            return;

        IReadOnlyList<NationSO> nations = library.Nations;
        for (int i = 0; i < nations.Count; i++)
            if (nations[i] != null && !string.IsNullOrEmpty(nations[i].id))
                _nationHue[nations[i].id] = i / (float)Math.Max(1, nations.Count);

        foreach (EraSO era in library.Eras ?? Array.Empty<EraSO>())
            if (era != null && !string.IsNullOrEmpty(era.id))
                _eraOrder[era.id] = era.order;
    }

    /// <summary>The layer's full-canvas sprite (final art, else a placeholder).</summary>
    public Sprite Get(LookKey key) => EntryOf(key).Full;

    /// <summary>The layer's passport-photo crop (the head and shoulders).</summary>
    public Sprite GetPhoto(LookKey key)
    {
        Entry e = EntryOf(key);
        if (e.Photo == null)
        {
            Rect r = e.Full.rect;
            (float x, float y, float w, float h) = LookCanvas.PhotoRect;
            var crop = new Rect(r.x + x * r.width, r.y + y * r.height, w * r.width, h * r.height);
            e.Photo = Sprite.Create(e.Full.texture, crop, new Vector2(0.5f, 0.5f), crop.height, 0, SpriteMeshType.FullRect);
            e.Photo.name = key.Name + "_photo";
        }
        return e.Photo;
    }

    /// <summary>Releases every cached key not in <paramref name="keys"/> (the traveller now at the desk).</summary>
    public void Retain(IEnumerable<LookKey> keys)
    {
        var keep = new HashSet<string>();
        if (keys != null)
            foreach (LookKey key in keys)
                keep.Add(key.Name);

        var drop = new List<string>();
        foreach (KeyValuePair<string, Entry> pair in _cache)
            if (!keep.Contains(pair.Key))
                drop.Add(pair.Key);

        foreach (string name in drop)
        {
            Release(_cache[name]);
            _cache.Remove(name);
        }
    }

    /// <summary>Releases everything.</summary>
    public void Dispose() => Retain(null);

    /// <summary>The cached entry of a key, loading or drawing it on first use.</summary>
    private Entry EntryOf(LookKey key)
    {
        if (_cache.TryGetValue(key.Name, out Entry cached))
            return cached;

        var entry = new Entry { Full = Resources.Load<Sprite>($"{ResourcesFolder}/{key.Name}") };
        if (entry.Full == null)
        {
            if (!_toldAboutPlaceholders)
            {
                Debug.Log($"[CharacterArt] No final art for '{key.Name}' (and maybe others); drawing a placeholder. Final art goes to {AssetFolder}/<key>.png (docs/CHARACTER_ART_CONTRACT.md).");
                _toldAboutPlaceholders = true;
            }

            entry.Placeholder = DrawPlaceholder(key);
            entry.Full = Sprite.Create(entry.Placeholder, new Rect(0f, 0f, LayerPlaceholder.Width, LayerPlaceholder.Height),
                                       new Vector2(0.5f, LookCanvas.FeetPivotY), LayerPlaceholder.Height, 0, SpriteMeshType.FullRect);
            entry.Full.name = key.Name;
        }

        _cache[key.Name] = entry;
        return entry;
    }

    /// <summary>A readable placeholder texture: the layer's region in the key's colours.</summary>
    private Texture2D DrawPlaceholder(LookKey key)
    {
        (byte r, byte g, byte b) nation = NationColour(key);
        (byte r, byte g, byte b) fill, accent;
        PlaceholderMark mark = PlaceholderMark.None;
        switch (key.Layer)
        {
            case LookLayer.Body:
            case LookLayer.Head:
                fill = PlaceholderPalette.Skin(key.SkinTone);
                accent = PlaceholderPalette.Darker(fill, 0.7f);
                break;
            case LookLayer.HairBack:
            case LookLayer.Hair:
            case LookLayer.FacialHair:
                fill = key.HairColour != null ? PlaceholderPalette.Hair(key.HairColour) : nation;
                accent = nation;
                break;
            case LookLayer.Whole:
                fill = nation;
                accent = (240, 236, 224);
                mark = Mark(key.Expression);
                break;
            default:
                fill = nation;
                accent = PlaceholderPalette.Darker(nation, 0.55f);
                break;
        }

        byte[] rgba = LayerPlaceholder.Render(RegionOf(key.Layer), fill, accent, mark);
        var tex = new Texture2D(LayerPlaceholder.Width, LayerPlaceholder.Height, TextureFormat.RGBA32, false)
        {
            name = key.Name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.LoadRawTextureData(rgba);
        tex.Apply(false, false);
        return tex;
    }

    /// <summary>A garment's (or premade's) colour: its nation's hue at its era's shade; grey for a nation the library does not list.</summary>
    private (byte r, byte g, byte b) NationColour(LookKey key)
    {
        if (key.NationId == null || !_nationHue.TryGetValue(key.NationId, out float hue))
            return PlaceholderPalette.Unknown;

        int order = key.EraId != null && _eraOrder.TryGetValue(key.EraId, out int o) ? o : 0;
        return PlaceholderPalette.FromHsv(hue, GarmentSaturation, FirstEraShade - EraShadeStep * order);
    }

    /// <summary>The placeholder region a layer is drawn in.</summary>
    private static PlaceholderRegion RegionOf(LookLayer layer)
    {
        switch (layer)
        {
            case LookLayer.HairBack: return PlaceholderRegion.HairBehind;
            case LookLayer.Body: return PlaceholderRegion.Body;
            case LookLayer.Outfit: return PlaceholderRegion.Clothes;
            case LookLayer.Head: return PlaceholderRegion.Head;
            case LookLayer.FacialHair: return PlaceholderRegion.Beard;
            case LookLayer.Hair: return PlaceholderRegion.HairCap;
            case LookLayer.Headwear: return PlaceholderRegion.Hat;
            case LookLayer.Accessory: return PlaceholderRegion.Collar;
            default: return PlaceholderRegion.WholeFigure;
        }
    }

    /// <summary>A premade expression's placeholder mark (the marks follow LookKeys.Expressions' order; unknown = neutral).</summary>
    private static PlaceholderMark Mark(string expression)
    {
        int index = 0;
        for (int i = 0; i < LookKeys.Expressions.Count; i++)
            if (LookKeys.Expressions[i] == expression)
                index = i;
        return PlaceholderMark.Neutral + index;
    }

    /// <summary>Frees one entry: the photo crop always; a placeholder's texture and sprite; a loaded sprite's texture back to Resources.</summary>
    private static void Release(Entry e)
    {
        DestroyObject(e.Photo);
        if (e.Placeholder != null)
        {
            DestroyObject(e.Full);
            DestroyObject(e.Placeholder);
        }
        else if (e.Full != null)
        {
            Resources.UnloadAsset(e.Full.texture);
        }
    }

    /// <summary>Destroys a runtime object (immediately outside play mode).</summary>
    private static void DestroyObject(Object o)
    {
        if (o == null)
            return;
        if (Application.isPlaying)
            Object.Destroy(o);
        else
            Object.DestroyImmediate(o);
    }
}
