using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The PC's look applied at run time (D2, the night of 2026-11-07): the
/// desktop and the monitor wear the look of the theme the desktop's config
/// names (DesktopConfigSO.theme, a PcThemeSO). In the cyberpunk look every
/// cel kit sprite on the PC is swapped by name for the theme's kit's (PcKit:
/// drawn glass pieces and neon glyphs), every chrome graphic takes its role's
/// token (PcPalette: dark glass, light ink; the evidence keeps its paper), the
/// wallpaper is the theme's skyline, the bezel is redrawn dark with a lit
/// edge round the glass (PcChromeArt.Bezel), its power switch is the neon
/// glyph, the power move's line and afterglow glow phosphor over the deep
/// glass (the boot screen's look; the move itself is PcFrame's), the glass
/// wears its scan rows and bloom (PcGlass), and the views that draw kit
/// pieces later are handed the theme's kit (IKitUser). The leading culture
/// tints the accent (PcThemeSO.cultureAccent) and the wallpaper. The look is
/// applied when the culture theme has themed the scene (CultureThemeService
/// .Applied: the office's load and every change of leader), so the culture's
/// colours never paint over it. In the cel look nothing is redrawn: every
/// sprite and colour this skin changed goes back, and the culture theme
/// themes the PC again. The DesktopWindowManager installs it on the desktop
/// canvas (<see cref="Install"/>); no scene holds it.
/// </summary>
[DisallowMultipleComponent]
public sealed class PcSkin : MonoBehaviour
{
    /// <summary>The bezel art's scale: drawn at half the frame's reference size, as the cel bezel is.</summary>
    private const int BezelArtDivisor = 2;

    /// <summary>The desktop's knobs (their theme names the look).</summary>
    private DesktopConfigSO _config;

    /// <summary>True while the live compare shows the cel look over a cyberpunk theme (<see cref="ToggleLook"/>).</summary>
    private bool _celShown;

    /// <summary>The theme's kit, and the accent it was drawn for (rebuilt when the culture's accent changes).</summary>
    private UiKitSO _kit;

    /// <summary>The accent <see cref="_kit"/> was drawn with.</summary>
    private Color _kitAccent;

    /// <summary>Everything the kit and the bezel made (textures, sprites, the kit), destroyed when they are drawn again or the skin goes.</summary>
    private readonly List<Object> _made = new List<Object>(), _bezelMade = new List<Object>();

    /// <summary>The colour each graphic had before this skin first recoloured it (the cel look puts them back).</summary>
    private readonly Dictionary<Graphic, Color> _colours = new Dictionary<Graphic, Color>();

    /// <summary>The sprite each image had before this skin first gave it the theme's art (the wallpaper, the bezel, the power switch).</summary>
    private readonly Dictionary<Image, Sprite> _sprites = new Dictionary<Image, Sprite>();

    /// <summary>The frame camera's clear colour before this skin (the tube behind the picture).</summary>
    private Color? _tube;

    /// <summary>Each window title's left offset before its glyph moved it right (the cel look puts it back).</summary>
    private readonly Dictionary<RectTransform, float> _titleInsets = new Dictionary<RectTransform, float>();

    /// <summary>The glyph's name beside a window's title (D2: "titles in Big Shoulders caps with a glyph").</summary>
    private const string TitleGlyphName = "TitleGlyph";

    /// <summary>The title glyph's height as a share of the title bar's, and its gap to the title (desktop units).</summary>
    private const float TitleGlyphShare = 0.82f, TitleGlyphGap = 6f;

    /// <summary>The look in use: the theme's, unless the live compare shows the cel look.</summary>
    public PcLook Look => _config != null && _config.theme != null && _config.theme.IsCyberpunk && !_celShown ? PcLook.Cyberpunk : PcLook.Cel;

    /// <summary>The kit in use on the PC: the theme's in the cyberpunk look, else the cel kit (null with no theme).</summary>
    public UiKitSO Kit => Look == PcLook.Cyberpunk ? _kit : _config != null && _config.theme != null ? _config.theme.baseKit : null;

    /// <summary>Puts the skin on the desktop canvas <paramref name="desktop"/> with its knobs (DesktopWindowManager.Awake); a second call keeps the first.</summary>
    public static PcSkin Install(GameObject desktop, DesktopConfigSO config)
    {
        if (desktop == null)
            return null;
        PcSkin skin = desktop.TryGetComponent(out PcSkin found) ? found : desktop.AddComponent<PcSkin>();
        skin._config = config;
        return skin;
    }

    /// <summary>Applies the look whenever the culture theme has themed the scene.</summary>
    private void OnEnable() => CultureThemeService.Applied += Apply;

    /// <summary>Stops following the culture theme.</summary>
    private void OnDisable() => CultureThemeService.Applied -= Apply;

    /// <summary>Applies the look once without a culture theme (a scene the service did not theme).</summary>
    private void Start()
    {
        if (CultureThemeService.Instance == null)
            Apply();
    }

    /// <summary>The textures and sprites it made go with it.</summary>
    private void OnDestroy()
    {
        Release(_made);
        Release(_bezelMade);
    }

    /// <summary>
    /// Swaps between the theme's look and the cel look, live (a designer's
    /// side-by-side, and the QA rig's call op for the cel still); a cel theme
    /// stays cel. The next office load wears the theme's look again.
    /// </summary>
    public void ToggleLook()
    {
        _celShown = !_celShown;
        Apply();
    }

    /// <summary>Applies the look in use to the desktop and the monitor (see the class summary).</summary>
    public void Apply()
    {
        PcThemeSO theme = _config != null ? _config.theme : null;
        if (theme == null || theme.baseKit == null)
            return;
        PcFrame frame = FindFirstObjectByType<PcFrame>(FindObjectsInactive.Include);
        bool cyber = Look == PcLook.Cyberpunk;
        Color accent = Accent(theme);
        UiKitSO kit = cyber ? KitFor(theme, accent) : theme.baseKit;

        Swap(transform, kit);
        if (frame != null)
            Swap(frame.transform, kit);
        foreach (IKitUser user in GetComponentsInChildren<IKitUser>(true))
            user.UseKit(kit);

        if (cyber)
        {
            Recolour(transform, theme, accent);
            Wallpaper(theme, accent);
            Monitor(frame, theme);
        }
        else
            Restore();
        TitleGlyphs(cyber, theme);
        RectTransform glass = frame != null ? frame.transform.Find("Root/Frame/Glass") as RectTransform : null;
        PcGlass overlay = cyber ? PcGlass.On(glass) : glass != null ? glass.GetComponent<PcGlass>() : null;
        if (overlay != null)
            overlay.Show(cyber ? theme : null);
    }

    /// <summary>The accent now: the theme's magenta with the leading culture's colour (its theme's PrimaryAction) blended in by the theme's share; the magenta with no leader.</summary>
    private static Color Accent(PcThemeSO theme)
    {
        CultureThemeService culture = CultureThemeService.Instance;
        PaletteEntry lead = culture != null && culture.ActiveCultureId != null && culture.ActiveTheme != null ? culture.ActiveTheme.Get(ThemeRoleId.PrimaryAction) : null;
        if (lead == null || !lead.hasFill)
            return theme.accent;
        Rgba blended = PcPalette.CultureAccent(PcKit.ToRgba(theme.accent), PcKit.ToRgba(lead.fill), theme.cultureAccent);
        return new Color(blended.R, blended.G, blended.B, blended.A);
    }

    /// <summary>The theme's kit for <paramref name="accent"/>: drawn once, again only when the accent changes (the old one's textures go once the PC wears the new).</summary>
    private UiKitSO KitFor(PcThemeSO theme, Color accent)
    {
        if (_kit != null && _kitAccent == accent)
            return _kit;
        var made = new List<Object>();
        UiKitSO kit = PcKit.Build(theme, accent, made);
        if (kit == null)
            return theme.baseKit;
        Swap(transform, kit); // off the old kit's sprites before they go
        Release(_made);
        _made.AddRange(made);
        _kit = kit;
        _kitAccent = accent;
        return kit;
    }

    /// <summary>Every kit sprite under <paramref name="root"/> (an image's, and a control's sprite swap) becomes <paramref name="kit"/>'s sprite of the same name; other sprites stay.</summary>
    private static void Swap(Transform root, UiKitSO kit)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
            image.sprite = Map(kit, image.sprite);
        foreach (Selectable control in root.GetComponentsInChildren<Selectable>(true))
        {
            if (control.transition != Selectable.Transition.SpriteSwap)
                continue;
            SpriteState s = control.spriteState;
            control.spriteState = new SpriteState
            {
                highlightedSprite = Map(kit, s.highlightedSprite),
                pressedSprite = Map(kit, s.pressedSprite),
                selectedSprite = Map(kit, s.selectedSprite),
                disabledSprite = Map(kit, s.disabledSprite)
            };
        }
    }

    /// <summary><paramref name="kit"/>'s sprite named as <paramref name="sprite"/>, else <paramref name="sprite"/> itself.</summary>
    private static Sprite Map(UiKitSO kit, Sprite sprite)
    {
        if (sprite == null)
            return null;
        Sprite mapped = kit.Get(sprite.name);
        return mapped != null ? mapped : sprite;
    }

    /// <summary>
    /// Every chrome graphic under <paramref name="root"/> takes its role's token
    /// (PcPalette; an image in its role's ink as a text), its alpha kept; a kit piece keeps the colours drawn in its
    /// sprite, a neon glyph glows phosphor, and a text with no role in the
    /// kit's dark ink (a line a view made at run time) takes the light ink
    /// unless it lies on evidence.
    /// </summary>
    private void Recolour(Transform root, PcThemeSO theme, Color accent)
    {
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            bool text = graphic is TMP_Text;
            if (graphic is Image image && image.sprite != null)
            {
                PcPart part = PcSkinRules.Of(image.sprite.name);
                if (part.Piece == PcPiece.Glyph || part.Piece == PcPiece.Tile)
                {
                    if (theme.Glyph(image.sprite.name) != null)
                        Paint(graphic, theme.phosphor);
                    continue;
                }
                if (part.Piece != PcPiece.Keep)
                    continue;
            }
            PcInk ink;
            if (graphic.TryGetComponent(out ThemeTag tag))
                ink = PcPalette.Of(tag.Role, text || tag.Part == ThemePart.Ink); // an image drawn in its role's ink (a glyph, a stroke) is light on the glass, as a text is
            else if (text && Luma(graphic.color) < 0.5f && !OnEvidence(graphic.transform))
                ink = PcInk.Ink;
            else
                continue;
            if (ink == PcInk.Keep)
                continue;
            Color colour = ink == PcInk.Clear ? Color.clear : theme.Token(ink, accent);
            Color before = Before(graphic);
            Paint(graphic, new Color(colour.r, colour.g, colour.b, ink == PcInk.Clear ? 0f : before.a * (text ? 1f : colour.a)));
        }
    }

    /// <summary>
    /// Each app window's title shows its app's glyph at its left (the
    /// desktop icon's, tinted phosphor), the title moved right of it; in the
    /// cel look the glyphs hide and the titles go back. A window with no app
    /// (the F1 card) has none.
    /// </summary>
    private void TitleGlyphs(bool on, PcThemeSO theme)
    {
        DesktopIcons icons = GetComponentInChildren<DesktopIcons>(true);
        foreach (DesktopWindow window in GetComponentsInChildren<DesktopWindow>(true))
        {
            var header = window.transform.Find("Header") as RectTransform;
            var title = header != null ? header.Find("TitleText") as RectTransform : null;
            if (title == null)
                continue;
            Sprite sprite = on && icons != null ? icons.GlyphOf(window) : null;
            var glyph = header.Find(TitleGlyphName) as RectTransform;
            if (sprite == null)
            {
                if (glyph != null)
                    glyph.gameObject.SetActive(false);
                if (_titleInsets.TryGetValue(title, out float inset))
                    title.offsetMin = new Vector2(inset, title.offsetMin.y);
                continue;
            }
            if (!_titleInsets.ContainsKey(title))
                _titleInsets.Add(title, title.offsetMin.x);
            float size = header.rect.height * TitleGlyphShare, left = _titleInsets[title];
            if (glyph == null)
            {
                glyph = (RectTransform)new GameObject(TitleGlyphName, typeof(RectTransform), typeof(Image)).transform;
                glyph.gameObject.layer = header.gameObject.layer;
                glyph.SetParent(header, false);
                glyph.anchorMin = glyph.anchorMax = new Vector2(0f, 0.5f);
            }
            glyph.gameObject.SetActive(true);
            glyph.sizeDelta = new Vector2(size, size);
            glyph.anchoredPosition = new Vector2(left + size / 2f, 0f);
            var image = glyph.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = theme.phosphor;
            title.offsetMin = new Vector2(left + size + TitleGlyphGap, title.offsetMin.y);
        }
    }

    /// <summary>The wallpaper is the theme's skyline, tinted by the leading culture's accent by the theme's share (white with no leader).</summary>
    private void Wallpaper(PcThemeSO theme, Color accent)
    {
        if (theme.wallpaper == null)
            return;
        bool leader = CultureThemeService.Instance != null && CultureThemeService.Instance.ActiveCultureId != null;
        Color tint = leader ? Color.Lerp(Color.white, accent, theme.wallpaperTint) : Color.white;
        foreach (ThemeTag tag in GetComponentsInChildren<ThemeTag>(true))
        {
            if (tag.Role != ThemeRoleId.Desktop || !tag.TryGetComponent(out Image image))
                continue;
            Give(image, theme.wallpaper);
            Paint(image, tint);
            if (tag.TryGetComponent(out AspectRatioFitter fitter))
                fitter.aspectRatio = theme.wallpaper.rect.width / theme.wallpaper.rect.height;
        }
    }

    /// <summary>
    /// The monitor in the theme's look: the bezel redrawn round the glass, the
    /// power switch the neon glyph, the brand plate's ink quiet, the power
    /// move's dark glass the deep glass and its line and afterglow phosphor,
    /// and the tube behind the picture the deep glass.
    /// </summary>
    private void Monitor(PcFrame frame, PcThemeSO theme)
    {
        Canvas desktop = GetComponentInParent<Canvas>();
        if (desktop != null && desktop.worldCamera != null)
        {
            if (!_tube.HasValue)
                _tube = desktop.worldCamera.backgroundColor;
            desktop.worldCamera.backgroundColor = theme.glassDeep;
        }
        if (frame == null)
            return;
        Transform casing = frame.transform.Find("Root/Frame");
        if (casing != null && casing.TryGetComponent(out Image bezel))
        {
            Sprite drawn = Bezel(theme, (RectTransform)casing, casing.Find("Glass") as RectTransform);
            if (drawn != null)
                Give(bezel, drawn);
        }
        Transform power = casing != null ? casing.Find("PowerButton") : null;
        if (power != null && theme.powerGlyph != null && power.TryGetComponent(out Image switchFace))
        {
            Give(switchFace, theme.powerGlyph);
            Paint(switchFace, theme.phosphor);
        }
        Transform brand = casing != null ? casing.Find("Brand") : null;
        if (brand != null && brand.TryGetComponent(out TMP_Text plate))
            Paint(plate, theme.inkMuted);
        Transform glass = casing != null ? casing.Find("Glass") : null;
        Tint(glass, "PowerVeil", theme.glassDeep);
        Tint(glass, "PowerGlow", theme.phosphor);
        Tint(glass, "Afterglow", theme.phosphor);
    }

    /// <summary>The power move's part <paramref name="partName"/> on the glass in <paramref name="colour"/>'s hue, its alpha left to PcFrame.</summary>
    private void Tint(Transform glass, string partName, Color colour)
    {
        Transform part = glass != null ? glass.Find(partName) : null;
        if (part != null && part.TryGetComponent(out Image image))
            Paint(image, new Color(colour.r, colour.g, colour.b, Before(image).a));
    }

    /// <summary>The bezel drawn for the frame <paramref name="casing"/> round <paramref name="glass"/> (PcChromeArt.Bezel at half the frame's size), once per size.</summary>
    private Sprite Bezel(PcThemeSO theme, RectTransform casing, RectTransform glass)
    {
        Vector2 size = casing.rect.size;
        int w = Mathf.RoundToInt(size.x / BezelArtDivisor), h = Mathf.RoundToInt(size.y / BezelArtDivisor);
        if (w <= 0 || h <= 0 || glass == null)
            return null;
        string key = $"PcBezel {w}x{h}";
        foreach (Object made in _bezelMade)
            if (made is Sprite s && s.name == key)
                return s;
        Release(_bezelMade);
        Vector2 min = glass.anchoredPosition - Vector2.Scale(glass.sizeDelta, glass.pivot), max = min + glass.sizeDelta;
        float x0 = min.x / BezelArtDivisor, y0 = min.y / BezelArtDivisor, x1 = max.x / BezelArtDivisor, y1 = max.y / BezelArtDivisor;
        Rgba body = PcKit.ToRgba(theme.bezel), low = PcKit.ToRgba(theme.bezelLow), lit = PcKit.ToRgba(theme.bezelLit), gasket = PcKit.ToRgba(theme.gasket);
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = key, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = PcKit.ToColor(PcChromeArt.Bezel(x, y, w, h, theme.bezelRadius, x0, y0, x1, y1, body, low, lit, gasket, theme.gasketWidth));
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f / BezelArtDivisor);
        sprite.name = key;
        _bezelMade.Add(texture);
        _bezelMade.Add(sprite);
        return sprite;
    }

    /// <summary>The cel look again: every sprite and colour this skin changed goes back, then the culture theme themes the PC again.</summary>
    private void Restore()
    {
        foreach (KeyValuePair<Image, Sprite> pair in _sprites)
            if (pair.Key != null)
                pair.Key.sprite = pair.Value;
        foreach (KeyValuePair<Graphic, Color> pair in _colours)
            if (pair.Key != null)
                pair.Key.color = pair.Value;
        _sprites.Clear();
        _colours.Clear();
        Canvas desktop = GetComponentInParent<Canvas>();
        if (_tube.HasValue && desktop != null && desktop.worldCamera != null)
            desktop.worldCamera.backgroundColor = _tube.Value;
        _tube = null;
        if (CultureThemeService.Instance != null)
        {
            CultureThemeService.Instance.ApplyTo(gameObject);
            PcFrame frame = FindFirstObjectByType<PcFrame>(FindObjectsInactive.Include);
            if (frame != null)
                CultureThemeService.Instance.ApplyTo(frame.gameObject);
        }
    }

    /// <summary>Gives <paramref name="image"/> the theme's <paramref name="sprite"/>, keeping the one it had for the cel look.</summary>
    private void Give(Image image, Sprite sprite)
    {
        if (!_sprites.ContainsKey(image))
            _sprites.Add(image, image.sprite);
        image.sprite = sprite;
    }

    /// <summary>Paints <paramref name="graphic"/> in <paramref name="colour"/>, keeping the colour it had for the cel look.</summary>
    private void Paint(Graphic graphic, Color colour)
    {
        Before(graphic);
        if (graphic.color != colour)
            graphic.color = colour;
    }

    /// <summary>The colour <paramref name="graphic"/> had before this skin first touched it (recorded now when it is new).</summary>
    private Color Before(Graphic graphic)
    {
        if (!_colours.TryGetValue(graphic, out Color before))
        {
            before = graphic.color;
            _colours.Add(graphic, before);
        }
        return before;
    }

    /// <summary>True when one of <paramref name="t"/>'s parents is evidence (a diegetic role: a paper, a form, a page of a site).</summary>
    private static bool OnEvidence(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
            if (p.TryGetComponent(out ThemeTag tag) && ThemeRoles.IsDiegetic(tag.Role))
                return true;
        return false;
    }

    /// <summary>A colour's luma (Rec. 601).</summary>
    private static float Luma(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;

    /// <summary>Destroys every object in <paramref name="made"/> and empties it.</summary>
    private static void Release(List<Object> made)
    {
        foreach (Object o in made)
            if (o != null)
                Destroy(o);
        made.Clear();
    }
}
