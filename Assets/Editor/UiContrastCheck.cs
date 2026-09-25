using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The build-time readability check (the readability fix: "lots of text ...
/// is not readable because the font is light color and the bg is light
/// color"): every text on a canvas a builder made is checked against what it
/// is drawn on, in every theme it can take (the neutral one and each culture's;
/// a scene with no themed graphics has one look). What it is drawn on is the
/// graphics drawn before it: its own container's earlier children whose rect
/// holds its centre, then each parent's own graphic (a label is drawn on its
/// button even where a layout sizes them at runtime; Contrast.WorstRatio composites them;
/// through a translucent stack the worse of black and white behind counts). A
/// themed graphic takes its role's colour in the theme, the Desktop role the
/// theme's wallpaper; a sprite counts as its texture's mean under the text.
/// The size class comes from the text's smallest size as drawn on a
/// 1920x1080 screen (Contrast.ClassFor); a Glyph or Hint role needs 3:1. Each
/// text below its minimum is logged as an error, so light-on-light cannot come
/// back through a builder unseen.
/// </summary>
public static class UiContrastCheck
{
    /// <summary>Loaded textures (a sprite's file read once per check).</summary>
    private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

    /// <summary>
    /// Checks every text under <paramref name="canvas"/> (inactive ones too)
    /// in each of <paramref name="themes"/> (null or empty: the scene's own
    /// colours), <paramref name="pixelsPerUnit"/> being how many 1080p screen
    /// pixels one canvas unit is drawn at, with the content library's
    /// minimums and shrink-to-fit floor (<paramref name="ui"/>; null: the
    /// defaults). Logs one error per failing text (its worst look) and
    /// returns how many failed.
    /// </summary>
    public static int Check(Canvas canvas, float pixelsPerUnit, IReadOnlyList<ThemeSO> themes, CultureUiSettings ui)
    {
        if (canvas == null)
            return 0;

        ui = ui ?? new CultureUiSettings();
        ContrastRules rules = ui.contrast ?? new ContrastRules();
        IReadOnlyList<ThemeSO> looks = themes != null && themes.Count > 0 ? themes : new ThemeSO[] { null };
        int failed = 0;
        try
        {
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                ThemeTag tag = text.GetComponent<ThemeTag>();
                float size = text.enableAutoSizing ? Mathf.Min(text.fontSizeMin, text.fontSizeMax) : text.fontSize;
                if (tag != null && tag.ShrinkToFit)
                    size *= ui.labelMinScale;
                bool bold = (text.fontStyle & FontStyles.Bold) != 0;
                ContrastClass cls = RoleClass(tag, looks[0]) is ContrastClass fixedClass && (fixedClass == ContrastClass.Glyph || fixedClass == ContrastClass.Hint)
                    ? fixedClass
                    : Contrast.ClassFor(size * pixelsPerUnit, bold);
                float need = rules.Min(cls);

                double worst = double.MaxValue;
                string worstTheme = null;
                foreach (ThemeSO theme in looks)
                {
                    double ratio = Contrast.WorstRatio(ToRgba(Ink(text, tag, theme)), Behind(text, theme));
                    if (ratio < worst)
                    {
                        worst = ratio;
                        worstTheme = theme != null ? theme.cultureId : "scene";
                    }
                }
                if (worst + 0.005 < need)
                {
                    failed++;
                    Debug.LogError($"[TimeDesk] '{PathOf(text.transform)}' ({(tag != null ? tag.Role.ToString() : "untagged")} text, {size * pixelsPerUnit:0.#} px) is {worst:0.00}:1 on what it is drawn on in the '{worstTheme}' look; it needs {need:0.#}:1 (the readability check). Give it a darker ink or a backing in the builder or the theme.", text);
                }
            }
        }
        finally
        {
            foreach (Texture2D t in Textures.Values)
                Object.DestroyImmediate(t);
            Textures.Clear();
        }
        return failed;
    }

    /// <summary>A role's contrast class in a theme (null when untagged or the theme has none).</summary>
    private static ContrastClass? RoleClass(ThemeTag tag, ThemeSO theme)
    {
        PaletteEntry e = tag != null && theme != null ? theme.Get(tag.Role) : null;
        return e != null ? e.textClass : (ContrastClass?)null;
    }

    /// <summary>The text's colour in a theme: its role's ink unless the role is diegetic (never themed) or the theme has none.</summary>
    private static Color Ink(TMP_Text text, ThemeTag tag, ThemeSO theme)
    {
        PaletteEntry e = tag != null && theme != null && !ThemeRoles.IsDiegetic(tag.Role) ? theme.Get(tag.Role) : null;
        return e != null && e.hasInk ? e.ink : text.color;
    }

    /// <summary>The colours drawn behind a text, nearest first: its container's earlier children (and theirs, last first), then the container itself, then up.</summary>
    private static List<Rgba> Behind(TMP_Text text, ThemeSO theme)
    {
        var layers = new List<Rgba>();
        Vector3 centre = text.rectTransform.TransformPoint(text.rectTransform.rect.center);
        Rect rect = WorldRect(text.rectTransform);
        Transform node = text.transform;
        while (node.parent != null && node.GetComponent<Canvas>() == null)
        {
            Transform parent = node.parent;
            for (int i = node.GetSiblingIndex() - 1; i >= 0; i--)
                if (Collect(parent.GetChild(i), centre, rect, theme, layers))
                    return layers;
            if (parent.GetComponent<Graphic>() is Graphic own && !(own is TMP_Text) && Add(own, centre, rect, theme, layers, true))
                return layers;
            node = parent;
        }
        return layers;
    }

    /// <summary>A subtree drawn before the text: its graphics that hold the text's centre, the last drawn first; true once opaque.</summary>
    private static bool Collect(Transform root, Vector3 centre, Rect rect, ThemeSO theme, List<Rgba> layers)
    {
        if (!root.gameObject.activeSelf)
            return false;
        for (int i = root.childCount - 1; i >= 0; i--)
            if (Collect(root.GetChild(i), centre, rect, theme, layers))
                return true;
        return root.GetComponent<Graphic>() is Graphic g && !(g is TMP_Text) && !(g is TMP_SubMeshUI) && Add(g, centre, rect, theme, layers, false);
    }

    /// <summary>Adds a graphic behind the text (an <paramref name="ancestor"/>'s always, another only when it holds the text's centre) in its colour in the theme; true once the layers are opaque.</summary>
    private static bool Add(Graphic g, Vector3 centre, Rect rect, ThemeSO theme, List<Rgba> layers, bool ancestor)
    {
        if (!g.enabled || (!ancestor && !g.rectTransform.rect.Contains(g.rectTransform.InverseTransformPoint(centre))))
            return false;

        Color colour = g.color;
        ThemeTag tag = g.GetComponent<ThemeTag>();
        Sprite sprite = g is Image img ? img.sprite : null;
        if (tag != null && theme != null && !ThemeRoles.IsDiegetic(tag.Role) && theme.Get(tag.Role) is PaletteEntry e)
        {
            if (tag.Role == ThemeRoleId.Desktop)
            {
                sprite = theme.wallpaper;
                colour = sprite != null ? Color.white : e.fill;
            }
            else if (tag.Part == ThemePart.Ink ? e.hasInk : e.hasFill)
            {
                colour = tag.Part == ThemePart.Ink ? e.ink : e.fill;
            }
        }
        if (colour.a <= 0.002f)
            return false;
        if (g is RawImage)
            return false; // a picture: what shows is unknown, the black and white bounds decide
        if (sprite != null && AssetDatabase.GetAssetPath(sprite) != "Resources/unity_builtin_extra")
        {
            if (!(Mean(sprite, g.rectTransform, rect) is Color mean))
                return false;
            colour *= mean;
        }

        float a = 0f;
        foreach (Rgba l in layers)
            a += (1f - a) * l.A;
        layers.Add(ToRgba(colour));
        return a + (1f - a) * colour.a >= 0.995f;
    }

    /// <summary>A sprite's mean colour under a world rect (read from its file), or null when the file cannot be read.</summary>
    private static Color? Mean(Sprite sprite, RectTransform image, Rect world)
    {
        string path = AssetDatabase.GetAssetPath(sprite.texture);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return null;
        if (!Textures.TryGetValue(path, out Texture2D tex))
        {
            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path)))
            {
                Object.DestroyImmediate(tex);
                return null;
            }
            Textures[path] = tex;
        }

        // The text rect in the image's local rect, as a share of it, onto the sprite's pixels (scaled to the file's size).
        Rect local = image.rect;
        Vector2 min = image.InverseTransformPoint(new Vector3(world.xMin, world.yMin, 0f));
        Vector2 max = image.InverseTransformPoint(new Vector3(world.xMax, world.yMax, 0f));
        float u0 = Mathf.Clamp01((Mathf.Min(min.x, max.x) - local.xMin) / local.width), u1 = Mathf.Clamp01((Mathf.Max(min.x, max.x) - local.xMin) / local.width);
        float v0 = Mathf.Clamp01((Mathf.Min(min.y, max.y) - local.yMin) / local.height), v1 = Mathf.Clamp01((Mathf.Max(min.y, max.y) - local.yMin) / local.height);
        Rect px = sprite.textureRect;
        float sx = (float)tex.width / sprite.texture.width, sy = (float)tex.height / sprite.texture.height;
        int x0 = Mathf.FloorToInt((px.xMin + u0 * px.width) * sx), x1 = Mathf.CeilToInt((px.xMin + u1 * px.width) * sx);
        int y0 = Mathf.FloorToInt((px.yMin + v0 * px.height) * sy), y1 = Mathf.CeilToInt((px.yMin + v1 * px.height) * sy);
        x1 = Mathf.Clamp(Mathf.Max(x1, x0 + 1), 1, tex.width);
        y1 = Mathf.Clamp(Mathf.Max(y1, y0 + 1), 1, tex.height);
        x0 = Mathf.Clamp(x0, 0, x1 - 1);
        y0 = Mathf.Clamp(y0, 0, y1 - 1);
        int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt((x1 - x0) * (y1 - y0) / 4096f)));
        float r = 0f, g = 0f, b = 0f, a = 0f;
        int n = 0;
        for (int y = y0; y < y1; y += step)
            for (int x = x0; x < x1; x += step)
            {
                Color c = tex.GetPixel(x, y);
                r += c.r * c.a;
                g += c.g * c.a;
                b += c.b * c.a;
                a += c.a;
                n++;
            }
        return n == 0 || a <= 0f ? new Color(0f, 0f, 0f, 0f) : new Color(r / a, g / a, b / a, a / n);
    }

    /// <summary>A rect transform's world-space bounds (the canvases here are axis-aligned).</summary>
    private static Rect WorldRect(RectTransform rt)
    {
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return Rect.MinMaxRect(Mathf.Min(corners[0].x, corners[2].x), Mathf.Min(corners[0].y, corners[2].y), Mathf.Max(corners[0].x, corners[2].x), Mathf.Max(corners[0].y, corners[2].y));
    }

    private static Rgba ToRgba(Color c) => new Rgba(c.r, c.g, c.b, c.a);

    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
}
