using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The monitor's glass overlay in the cyberpunk look (D2): low scan rows that
/// roll slowly down the picture, a phosphor bloom rising towards the glass's
/// edges and a vignette in its corners, over the desktop's picture and under
/// the power move's veil, glow and afterglow (PcFrame's own parts, which stay
/// on top). Its strengths are the theme's knobs (PcThemeSO); under Reduced
/// Motion the rows hold still. It takes no clicks. PcSkin puts it on the
/// glass and shows or hides it with the look.
/// </summary>
public sealed class PcGlass : MonoBehaviour
{
    /// <summary>The overlay textures' size (the bloom and the vignette stretch over the glass).</summary>
    private const int FieldSize = 64;

    /// <summary>The overlay's three layers.</summary>
    private RawImage _scan, _bloom, _vignette;

    /// <summary>The textures it made (destroyed with it).</summary>
    private Texture2D _scanTexture, _bloomTexture, _vignetteTexture;

    /// <summary>The theme shown (null: hidden).</summary>
    private PcThemeSO _theme;

    /// <summary>How far the rows have rolled, in rows.</summary>
    private float _roll;

    /// <summary>The glass's corners on the screen (reused).</summary>
    private readonly Vector3[] _corners = new Vector3[4];

    /// <summary>The glass overlay on <paramref name="glass"/> (made on first use).</summary>
    public static PcGlass On(RectTransform glass)
    {
        if (glass == null)
            return null;
        return glass.TryGetComponent(out PcGlass found) ? found : glass.gameObject.AddComponent<PcGlass>();
    }

    /// <summary>Shows the overlay in <paramref name="theme"/>'s strengths and colours (its textures drawn again), or hides it for null.</summary>
    public void Show(PcThemeSO theme)
    {
        _theme = theme;
        if (theme == null)
        {
            SetVisible(false);
            return;
        }
        Build();
        Paint(theme);
        SetVisible(true);
    }

    /// <summary>The three layers, made once as the glass's first children (the power move's parts stay over them).</summary>
    private void Build()
    {
        if (_scan != null)
            return;
        _vignette = Layer("GlassVignette");
        _bloom = Layer("GlassBloom");
        _scan = Layer("GlassScan");
    }

    /// <summary>A layer stretched over the glass, first among its children, taking no clicks.</summary>
    private RawImage Layer(string layerName)
    {
        var go = new GameObject(layerName, typeof(RectTransform), typeof(RawImage));
        go.layer = gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.SetAsFirstSibling();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<RawImage>();
        image.raycastTarget = false;
        return image;
    }

    /// <summary>The layers' textures from the theme's knobs (PcChromeArt: the scan row, the bloom, the vignette).</summary>
    private void Paint(PcThemeSO theme)
    {
        int pitch = Mathf.Max(2, theme.scanlinePitch);
        Replace(ref _scanTexture, new Texture2D(1, pitch, TextureFormat.RGBA32, false) { name = "PcGlass scan", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point });
        for (int y = 0; y < pitch; y++)
            _scanTexture.SetPixel(0, y, new Color(0f, 0f, 0f, PcChromeArt.ScanRow(y, pitch, theme.scanlineStrength)));
        _scanTexture.Apply(false, false);
        _scan.texture = _scanTexture;

        Replace(ref _bloomTexture, Field("PcGlass bloom", theme.phosphor, (u, v) => PcChromeArt.Bloom(u, v, theme.bloomStrength)));
        _bloom.texture = _bloomTexture;
        Replace(ref _vignetteTexture, Field("PcGlass vignette", Color.black, (u, v) => PcChromeArt.Vignette(u, v, theme.vignetteStrength)));
        _vignette.texture = _vignetteTexture;
    }

    /// <summary>A field texture of <paramref name="colour"/> whose alpha is <paramref name="alpha"/> at each (u, v).</summary>
    private static Texture2D Field(string textureName, Color colour, System.Func<float, float, float> alpha)
    {
        var texture = new Texture2D(FieldSize, FieldSize, TextureFormat.RGBA32, false) { name = textureName, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < FieldSize; y++)
            for (int x = 0; x < FieldSize; x++)
                texture.SetPixel(x, y, new Color(colour.r, colour.g, colour.b, alpha((x + 0.5f) / FieldSize, (y + 0.5f) / FieldSize)));
        texture.Apply(false, false);
        return texture;
    }

    /// <summary>Puts <paramref name="made"/> in <paramref name="slot"/>, destroying the texture there before.</summary>
    private static void Replace(ref Texture2D slot, Texture2D made)
    {
        if (slot != null)
            Destroy(slot);
        slot = made;
    }

    /// <summary>Shows or hides the three layers.</summary>
    private void SetVisible(bool on)
    {
        foreach (RawImage layer in new[] { _scan, _bloom, _vignette })
            if (layer != null && layer.gameObject.activeSelf != on)
                layer.gameObject.SetActive(on);
    }

    /// <summary>The rows tile the glass one per pitch of screen pixels and roll at the theme's speed (held still under Reduced Motion).</summary>
    private void LateUpdate()
    {
        if (_theme == null || _scan == null)
            return;
        ((RectTransform)transform).GetWorldCorners(_corners);
        float rows = Mathf.Max(1f, (_corners[2].y - _corners[0].y) / Mathf.Max(2, _theme.scanlinePitch));
        if (!MotionPreference.Reduced)
            _roll = Mathf.Repeat(_roll - Time.unscaledDeltaTime * _theme.scanlineSpeed / Mathf.Max(2, _theme.scanlinePitch), 1f);
        _scan.uvRect = new Rect(0f, _roll, 1f, rows);
    }

    /// <summary>The textures go with the overlay.</summary>
    private void OnDestroy()
    {
        foreach (Texture2D t in new[] { _scanTexture, _bloomTexture, _vignetteTexture })
            if (t != null)
                Destroy(t);
    }
}
