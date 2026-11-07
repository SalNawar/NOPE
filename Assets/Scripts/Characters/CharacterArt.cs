using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Loads character layer sprites by key (LookKeys) from
/// Resources/Characters/{key} (Assets/Art/Characters/Resources/Characters,
/// the ChatGPT art imported by CharacterArtImporter). A key with no art is
/// drawn with its nearest key that has art (LookArtFallback, by the steps of
/// CharacterArtFallbackSO), and not drawn at all when there is none: nothing
/// is drawn by code. Development builds log each stand-in once. Also makes
/// each layer's passport-photo crop (LookCanvas.PhotoRect). Every sprite is
/// one unit tall with its pivot at the feet. Two art sets (LookArtSets): the
/// traveller at the desk (PresentTraveller) is drawn from the 80s set (Resources/
/// Characters/80s) when every key of their look has an 80s drawing, else from
/// the classic set with its stand-ins, so one traveller never mixes the two
/// styles (their papers' photo included). Retain keeps only the current
/// traveller's textures; Dispose releases everything.
/// </summary>
public sealed class CharacterArt : IDisposable
{
    /// <summary>The Resources sub-folder character art loads from.</summary>
    public const string ResourcesFolder = "Characters";

    /// <summary>Where character art goes ({key}.png).</summary>
    public const string AssetFolder = "Assets/Art/Characters/Resources/" + ResourcesFolder;

    /// <summary>One drawn key's sprites.</summary>
    private sealed class Entry
    {
        /// <summary>The full-canvas sprite (loaded from Resources).</summary>
        public Sprite Full;

        /// <summary>The photo crop (made on first use), drawn from <see cref="PhotoTexture"/>.</summary>
        public Sprite Photo;

        /// <summary>The photo crop's own mipmapped texture (MakePhotoTexture); destroyed with <see cref="Photo"/>.</summary>
        public Texture2D PhotoTexture;
    }

    /// <summary>
    /// The height in pixels of a photo's own texture, at most: twice the
    /// largest size a photo is drawn at (D53). The reading size is about 72 px
    /// tall at 1080p (VERIFY1), so 256 px keeps a photo sharp up to 128 px on
    /// screen. A crop that is smaller (the 680 x 1020 texture's crop is 249 px
    /// tall, a premade's whole picture's 173 px) keeps its own height.
    /// </summary>
    public const int PhotoTextureHeight = 256;

    /// <summary>The loaded art, by the name of the key actually drawn.</summary>
    private readonly Dictionary<string, Entry> _cache = new Dictionary<string, Entry>();

    /// <summary>Each asked key's drawn path in its set (null: nothing to draw), resolved once per run: the art does not change while the game runs.</summary>
    private readonly Dictionary<string, string> _drawn = new Dictionary<string, string>();

    /// <summary>The art set the traveller at the desk is drawn from (LookArtSets; PresentTraveller).</summary>
    private string _set = LookArtSets.Classic;

    /// <summary>The art set the traveller at the desk is drawn from (LookArtSets.Classic or Retro).</summary>
    public string Set => _set;

    private readonly LookArtFallbackTable _table;
    private readonly LookArtUniverse _universe;

    /// <summary>
    /// Reads what the fallback chooses among from the library (its nations and
    /// the shared "neutral" art nation, its eras in order, the face bands'
    /// faces) and the fallback table from Resources (CharacterArtFallbackSO;
    /// without it a key with no art is not drawn).
    /// </summary>
    public CharacterArt(ContentLibrarySO library)
    {
        var nations = new List<string>();
        var eras = new List<string>();
        var faces = new List<string>();
        if (library != null)
        {
            nations.AddRange(library.Nations.Where(n => n != null && !string.IsNullOrEmpty(n.id)).Select(n => n.id));
            eras.AddRange(library.Eras.Where(e => e != null && !string.IsNullOrEmpty(e.id)).OrderBy(e => e.order).Select(e => e.id));
            if (library.LookRules?.faceBands != null)
                foreach (FaceBand band in library.LookRules.faceBands)
                    if (band?.faces != null)
                        faces.AddRange(band.faces.Where(f => !faces.Contains(f)));
        }
        if (!nations.Contains(Present.NeutralNationId))
            nations.Add(Present.NeutralNationId);
        _universe = new LookArtUniverse(nations, eras, faces);

        _table = Resources.Load<CharacterArtFallbackSO>(CharacterArtFallbackSO.ResourcePath)?.table;
        if (_table == null)
            Debug.LogWarning($"[CharacterArt] No fallback table at Resources/{CharacterArtFallbackSO.ResourcePath}: a layer with no art is not drawn.");
    }

    /// <summary>
    /// True when art is delivered for <paramref name="keyName"/>
    /// (Resources/Characters/{key}), with no fallback: a premade shows its
    /// whole picture once its neutral one is, and its layered stand-in look
    /// until then (days 7-15 B4). Loads the sprite to look, then releases its
    /// texture (a later Get loads it again).
    /// </summary>
    public static bool HasFinalArt(string keyName)
    {
        Sprite sprite = Load(keyName) ?? Load(LookArtSets.PathOf(LookArtSets.Retro, keyName));
        if (sprite == null)
            return false;
        Resources.UnloadAsset(sprite.texture);
        return true;
    }

    /// <summary>
    /// The traveller now at the desk: their art set is chosen (the 80s set
    /// when every key of <paramref name="look"/> has an 80s drawing, else the
    /// classic set; the classic set for no look) and only their textures are
    /// kept (Retain). Every Get and GetPhoto until the next PresentTraveller draws from
    /// that set, their papers' photo too.
    /// </summary>
    public void PresentTraveller(TravellerLook look)
    {
        Retain(look != null ? look.Keys : null);
        _set = LookArtSets.For(look, name => HasArtIn(LookArtSets.Retro, name));
    }

    /// <summary>True when <paramref name="keyName"/> has its own drawing in the current set (no stand-in): what a pose frame needs (TravellerPose.Posed).</summary>
    public bool HasOwnArt(string keyName) => HasArtIn(_set, keyName);

    /// <summary>The layer's full-canvas sprite: its own art, else its nearest stand-in's; null when there is none (the layer is not drawn).</summary>
    public Sprite Get(LookKey key) => EntryOf(key)?.Full;

    /// <summary>
    /// The layer's passport-photo crop (the head and shoulders; a premade's
    /// whole picture the head and neck, LookCanvas.PhotoRectFor) of the sprite
    /// Get draws; null when Get has none. The crop gets its own mipmapped,
    /// bilinear texture, made once per drawing (D53): the character textures
    /// have no mipmaps (D11), and the photo is drawn 3.5 to 13 times smaller
    /// than its crop, so a crop of them shimmers when the papers move.
    /// </summary>
    public Sprite GetPhoto(LookKey key)
    {
        Entry e = EntryOf(key);
        if (e == null)
            return null;
        if (e.Photo == null)
        {
            Rect r = e.Full.rect;
            (float x, float y, float w, float h) = LookCanvas.PhotoRectFor(key.Layer == LookLayer.Whole);
            var crop = new Rect(r.x + x * r.width, r.y + y * r.height, w * r.width, h * r.height);
            e.PhotoTexture = MakePhotoTexture(e.Full.texture, crop, e.Full.name + "_photo");
            if (e.PhotoTexture != null)
                e.Photo = Sprite.Create(e.PhotoTexture, new Rect(0, 0, e.PhotoTexture.width, e.PhotoTexture.height),
                                        new Vector2(0.5f, 0.5f), e.PhotoTexture.height, 0, SpriteMeshType.FullRect);
            else
                e.Photo = Sprite.Create(e.Full.texture, crop, new Vector2(0.5f, 0.5f), crop.height, 0, SpriteMeshType.FullRect);
            e.Photo.name = e.Full.name + "_photo";
        }
        return e.Photo;
    }

    /// <summary>
    /// The <paramref name="crop"/> of <paramref name="source"/> as its own
    /// texture (D53): drawn by the GPU into a render texture at most
    /// <see cref="PhotoTextureHeight"/> px tall, read back, mipmapped,
    /// bilinear. Null without a graphics device (a batch run with no
    /// graphics): the photo then crops the source as before.
    /// </summary>
    private static Texture2D MakePhotoTexture(Texture2D source, Rect crop, string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            return null;
        int height = Mathf.Max(1, Mathf.Min(PhotoTextureHeight, Mathf.RoundToInt(crop.height)));
        int width = Mathf.Max(1, Mathf.RoundToInt(height * crop.width / crop.height));
        RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        RenderTexture active = RenderTexture.active;
        try
        {
            Graphics.Blit(source, target, new Vector2(crop.width / source.width, crop.height / source.height),
                          new Vector2(crop.x / source.width, crop.y / source.height));
            RenderTexture.active = target;
            var photo = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            photo.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            photo.Apply(true, true);
            return photo;
        }
        finally
        {
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(target);
        }
    }

    /// <summary>Releases every loaded texture the keys in <paramref name="keys"/> (the traveller now at the desk) do not draw.</summary>
    public void Retain(IEnumerable<LookKey> keys)
    {
        var keep = new HashSet<string>();
        if (keys != null)
            foreach (LookKey key in keys)
                if (_drawn.TryGetValue(LookArtSets.PathOf(_set, key.Name), out string drawn) && drawn != null)
                    keep.Add(drawn);

        foreach (string name in _cache.Keys.Where(n => !keep.Contains(n)).ToList())
        {
            Release(_cache[name]);
            _cache.Remove(name);
        }
    }

    /// <summary>Releases everything (and draws from the classic set until the next PresentTraveller).</summary>
    public void Dispose()
    {
        _set = LookArtSets.Classic;
        Retain(null);
    }

    /// <summary>The entry a key draws (its own art or its stand-in's), loading it on first use; null when nothing is drawn.</summary>
    private Entry EntryOf(LookKey key)
    {
        string asked = LookArtSets.PathOf(_set, key.Name);
        if (!_drawn.TryGetValue(asked, out string drawn))
            drawn = _drawn[asked] = Resolve(key);
        if (drawn == null)
            return null;

        if (_cache.TryGetValue(drawn, out Entry cached))
            return cached;

        Sprite full = Load(drawn);
        if (full == null)
            return null;
        var entry = new Entry { Full = full };
        _cache[drawn] = entry;
        return entry;
    }

    /// <summary>The path of the drawing for <paramref name="key"/>: in the 80s set its own 80s drawing; otherwise (or when the 80s set lacks it, logged) the first of its classic fallback candidates with art, logged once in development builds when it is a stand-in or nothing.</summary>
    private string Resolve(LookKey key)
    {
        if (_set != LookArtSets.Classic)
        {
            string own = LookArtSets.PathOf(_set, key.Name);
            if (HasArt(own))
                return own;
            if (Debug.isDebugBuild)
                Debug.Log($"[CharacterArt] The {_set} set has no '{key.Name}'; drawing it from the classic set.");
        }

        foreach (LookKey candidate in LookArtFallback.Candidates(key, _table, _universe))
        {
            if (HasArt(candidate.Name))
            {
                if (candidate.Name != key.Name && Debug.isDebugBuild)
                    Debug.Log($"[CharacterArt] No ChatGPT art for '{key.Name}' yet; drawing '{candidate.Name}' in its place (CharacterArtFallback, docs/CHARACTER_ART_CONTRACT.md section 9).");
                return candidate.Name;
            }
        }

        if (Debug.isDebugBuild)
            Debug.Log($"[CharacterArt] No ChatGPT art for '{key.Name}' and no stand-in; the layer is not drawn (CharacterArtFallback, docs/CHARACTER_ART_CONTRACT.md section 9).");
        return null;
    }

    /// <summary>True when the key has its own drawing in <paramref name="set"/>; the loaded sprite is kept for its first Get.</summary>
    private bool HasArtIn(string set, string keyName) => HasArt(LookArtSets.PathOf(set, keyName));

    /// <summary>True when the path (a key in the classic set, "{set}/{key}" in another) has art; the loaded sprite is kept for its first Get.</summary>
    private bool HasArt(string keyName)
    {
        if (_cache.ContainsKey(keyName))
            return true;
        Sprite sprite = Load(keyName);
        if (sprite == null)
            return false;
        _cache[keyName] = new Entry { Full = sprite };
        return true;
    }

    /// <summary>The key's sprite from Resources, or null.</summary>
    private static Sprite Load(string keyName) =>
        string.IsNullOrEmpty(keyName) ? null : Resources.Load<Sprite>($"{ResourcesFolder}/{keyName}");

    /// <summary>Frees one entry: its photo crop and the photo's own texture, and its texture back to Resources.</summary>
    private static void Release(Entry e)
    {
        DestroyMade(e.Photo);
        DestroyMade(e.PhotoTexture);
        if (e.Full != null)
            Resources.UnloadAsset(e.Full.texture);
    }

    /// <summary>Destroys an object this class made (Destroy in play, DestroyImmediate in the editor); nothing for null.</summary>
    private static void DestroyMade(Object made)
    {
        if (made == null)
            return;
        if (Application.isPlaying)
            Object.Destroy(made);
        else
            Object.DestroyImmediate(made);
    }
}
