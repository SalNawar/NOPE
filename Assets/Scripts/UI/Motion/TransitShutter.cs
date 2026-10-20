using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A scene change's veil (FLOW; BUG-31a's "in-world transition", the bible's
/// J5): the booth's roll shutter drawn over the screen in the 80s cel kit
/// (painted steel slats with inked edges, a brass bottom bar). It rolls down
/// over the scene being left and up over the new one in the transition kit's
/// length (SceneTransit), and while it is down the hall's warm light moves
/// through its slat gaps and glints on its faces (ShutterRoll.Sweep), so the
/// screen never holds a frame without motion while the next scene loads.
/// Under Reduced Motion it fades in and out where it hangs instead of rolling,
/// and its light moves slower. It blocks clicks while shown. Built once, at
/// runtime, by SceneTransit's runner; its textures are made here and destroyed
/// with it.
/// </summary>
public sealed class TransitShutter : MonoBehaviour
{
    /// <summary>Slats on the screen's height, at any resolution.</summary>
    private const int Slats = 30;

    /// <summary>The bottom bar's share of the screen's height.</summary>
    private const float BarShare = 0.045f;

    /// <summary>One slat's rows, from its top (texture rows; the slat texture repeats them).</summary>
    private const int SlatRows = 32;

    /// <summary>The kit's ink, the slats' paint and the bar's brass.</summary>
    private static readonly Color32 Ink = new Color32(21, 18, 15, 255), Lip = new Color32(163, 154, 131, 255),
        FaceTop = new Color32(122, 115, 100, 255), FaceBottom = new Color32(106, 100, 86, 255), Shadow = new Color32(74, 69, 60, 255),
        Brass = new Color32(176, 138, 70, 255), BrassDark = new Color32(107, 83, 38, 255), Dark = new Color32(30, 26, 22, 255),
        Light = new Color32(232, 184, 103, 255);

    /// <summary>The veil's overlay canvas (its scale turns screen pixels into its units).</summary>
    private Canvas _canvas;

    /// <summary>The veil's group (its fade under Reduced Motion, and its click block).</summary>
    private CanvasGroup _group;

    /// <summary>The drawn shutter (moves up out of view to roll up).</summary>
    private RectTransform _shutter;

    /// <summary>The light behind the gaps, and its glint over the faces (both scroll down).</summary>
    private RawImage _light, _sheen;

    /// <summary>The textures made here (destroyed with the veil).</summary>
    private Texture2D _lightTexture, _slatTexture, _sheenTexture;

    /// <summary>Where the light stands (0..1 of a screen height).</summary>
    private float _phase;

    /// <summary>How far down the shutter is (0 rolled up out of view, 1 covering the screen).</summary>
    private float _cover;

    /// <summary>Builds the veil under <paramref name="parent"/> (an overlay canvas over everything at <paramref name="sortingOrder"/>), rolled up and off.</summary>
    public static TransitShutter Build(Transform parent, int sortingOrder)
    {
        var root = new GameObject("TransitShutter", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster), typeof(TransitShutter));
        root.transform.SetParent(parent, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        TransitShutter veil = root.GetComponent<TransitShutter>();
        veil.Make();
        root.SetActive(false);
        return veil;
    }

    /// <summary>Rolls the shutter down over the screen in <paramref name="seconds"/> (real time, the kit's ease; a fade under Reduced Motion), blocking clicks from its first frame.</summary>
    public IEnumerator Lower(float seconds)
    {
        gameObject.SetActive(true);
        _group.blocksRaycasts = true;
        Place(_cover); // its first frame shows it where it is (up, out of view), never a stale pose
        yield return Roll(1f, seconds);
    }

    /// <summary>Rolls the shutter up off the screen in <paramref name="seconds"/>, then hides it and lets clicks through.</summary>
    public IEnumerator Raise(float seconds)
    {
        yield return Roll(0f, seconds);
        _group.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>Moves the shutter to <paramref name="cover"/> over <paramref name="seconds"/> of real time (UiMotion.Delta), eased (TransitCurve).</summary>
    private IEnumerator Roll(float cover, float seconds)
    {
        float from = _cover, elapsed = 0f;
        while (elapsed < seconds)
        {
            yield return null;
            elapsed += UiMotion.Delta(Time.unscaledDeltaTime);
            Place(Mathf.LerpUnclamped(from, cover, TransitCurve.At(elapsed, seconds)));
        }
        Place(cover);
    }

    /// <summary>Shows the shutter <paramref name="cover"/> of the way down: rolled (the screen's height above it when up), or faded where it hangs under Reduced Motion.</summary>
    private void Place(float cover)
    {
        _cover = cover;
        bool fade = MotionPreference.Reduced;
        float height = Screen.height / Mathf.Max(0.0001f, _canvas.scaleFactor);
        _shutter.anchoredPosition = new Vector2(0f, fade ? 0f : (1f - cover) * height);
        _group.alpha = fade ? cover : 1f;
    }

    /// <summary>While shown: the light moves down the slats every frame (the veil is never a frozen frame).</summary>
    private void Update()
    {
        _phase = ShutterRoll.Sweep(_phase, UiMotion.Delta(Time.unscaledDeltaTime), MotionPreference.Reduced);
        var scroll = new Rect(0f, _phase, 1f, 1f);
        _light.uvRect = scroll;
        _sheen.uvRect = scroll;
    }

    private void OnDestroy()
    {
        Destroy(_lightTexture);
        Destroy(_slatTexture);
        Destroy(_sheenTexture);
    }

    /// <summary>Draws the veil: a click blocker, the shutter (the light, the slats, the glint, the bar) rolled up.</summary>
    private void Make()
    {
        _canvas = GetComponent<Canvas>();
        _group = GetComponent<CanvasGroup>();
        _group.blocksRaycasts = false;

        Image blocker = Layer<Image>("ClickBlocker", transform);
        blocker.color = new Color(0f, 0f, 0f, 0f);

        _shutter = Layer<RectTransform>("Shutter", transform);

        _lightTexture = Strip("TransitShutterLight", GlowRows, y => Color32.Lerp(Dark, Light, Bump(y, 0.9f)), TextureWrapMode.Repeat);
        _light = Layer<RawImage>("Light", _shutter);
        _light.texture = _lightTexture;
        _light.raycastTarget = false;

        _slatTexture = Strip("TransitShutterSlats", SlatRows, SlatRow, TextureWrapMode.Repeat);
        RawImage slats = Layer<RawImage>("Slats", _shutter);
        slats.texture = _slatTexture;
        slats.uvRect = new Rect(0f, 0f, 1f, Slats);
        slats.raycastTarget = false;

        _sheenTexture = Strip("TransitShutterSheen", GlowRows, y => new Color32(255, 240, 210, (byte)(255f * 0.16f * Bump(y, 0.8f))), TextureWrapMode.Repeat);
        _sheen = Layer<RawImage>("Sheen", _shutter);
        _sheen.texture = _sheenTexture;
        _sheen.raycastTarget = false;

        Image bar = Layer<Image>("Bar", _shutter);
        bar.color = Brass;
        bar.raycastTarget = false;
        Fit(bar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, BarShare));
        Image barTop = Layer<Image>("BarInk", bar.transform);
        barTop.color = Ink;
        barTop.raycastTarget = false;
        Fit(barTop.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
        barTop.rectTransform.sizeDelta = new Vector2(0f, 3f);
        barTop.rectTransform.pivot = new Vector2(0.5f, 1f);
        Image handle = Layer<Image>("Handle", bar.transform);
        handle.color = BrassDark;
        handle.raycastTarget = false;
        Fit(handle.rectTransform, new Vector2(0.44f, 0.3f), new Vector2(0.56f, 0.62f));

        Place(0f);
    }

    /// <summary>The slat texture's row <paramref name="row"/> (0 at the texture's bottom): from a slat's top, an ink edge, the lit lip, the face (shaded down), its shadow, an ink edge and the gap the light shows through (clear).</summary>
    private static Color32 SlatRow(int row)
    {
        int fromTop = SlatRows - 1 - row;
        if (fromTop == 0 || fromTop == 27)
            return Ink;
        if (fromTop <= 4)
            return Lip;
        if (fromTop <= 22)
            return Color32.Lerp(FaceTop, FaceBottom, (fromTop - 5) / 17f);
        if (fromTop <= 26)
            return Shadow;
        return new Color32(0, 0, 0, 0);
    }

    /// <summary>The light's and the glint's rows (one band a screen height).</summary>
    private const int GlowRows = 64;

    /// <summary>A smooth bump over the glow strip's height: 1 at its middle, falling to 0 at <paramref name="width"/>'s edges (a share of the height), 0 beyond.</summary>
    private static float Bump(int row, float width)
    {
        float x = Mathf.Abs((row + 0.5f) / GlowRows - 0.5f) / (0.5f * width);
        if (x >= 1f)
            return 0f;
        float s = 1f - x;
        return s * s * (3f - 2f * s);
    }

    /// <summary>A 4-pixel-wide vertical strip of <paramref name="rows"/> rows (row 0 at the bottom), filtered and wrapped as given.</summary>
    private static Texture2D Strip(string name, int rows, System.Func<int, Color32> colourOf, TextureWrapMode wrap)
    {
        var texture = new Texture2D(4, rows, TextureFormat.RGBA32, false) { name = name, wrapMode = wrap, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[4 * rows];
        for (int y = 0; y < rows; y++)
        {
            Color32 c = colourOf(y);
            for (int x = 0; x < 4; x++)
                pixels[y * 4 + x] = c;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    /// <summary>A full-size child of <paramref name="parent"/> named <paramref name="name"/> with a <typeparamref name="T"/>.</summary>
    private static T Layer<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Fit((RectTransform)go.transform, Vector2.zero, Vector2.one);
        return typeof(T) == typeof(RectTransform) ? go.GetComponent<T>() : go.AddComponent<T>();
    }

    /// <summary>Anchors <paramref name="rect"/> between <paramref name="min"/> and <paramref name="max"/> with no offsets.</summary>
    private static void Fit(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
