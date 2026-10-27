using UnityEngine;

/// <summary>
/// The r17 city's switch, data, art and knobs (CITY-R17). Loaded from Resources
/// (<c>Assets/Resources/CityR17/CityR17Config.asset</c>) by <see cref="CityR17.Attach"/>, so the city view and the
/// hall's windows pick it up with no scene change. <see cref="live"/> off (or data that fails to parse) leaves the
/// old living-city painting exactly as it was: the switch is the fallback, never a revert.
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/City R17 Config", fileName = "CityR17Config")]
public sealed class CityR17ConfigSO : ScriptableObject
{
    [Header("Switch")]
    [Tooltip("The r17 city as the city view's backdrop and in the hall's windows. Off: the old living-city painting.")]
    public bool live = true;

    [Header("Data (Assets/Data/City/R17, exported from the r17 page)")]
    public TextAsset sceneJson;
    public TextAsset trafficJson;
    public TextAsset vehiclesJson;
    public TextAsset artJson;

    [Header("Art (Assets/Art/City/R17; matched by file name)")]
    public Texture2D[] art = new Texture2D[0];

    [Header("Shaders")]
    public Shader spriteShader;
    public Shader vehicleShader;
    public Shader lampShader;
    public Shader windowShader;
    public Shader presentShader;

    [Header("Framing")]
    [Tooltip("Off: the whole plate letterboxed (21:9, the look-dev's frame; Saleh: 'you should be able to see the FULL picture'). On: fill the screen's height and pan across the plate with the look (no bars).")]
    public bool fillScreen;

    [Tooltip("The render texture's size against the framed output (1: one texel per screen pixel).")]
    [Range(0.5f, 2f)] public float renderScale = 1f;

    [Tooltip("MSAA samples for the 3D traffic's edges (1, 2, 4 or 8; the look-dev drew them antialiased).")]
    public int msaa = 4;

    [Header("Look")]
    [Tooltip("The nearest plane never moves faster than this (output px a second; BUG-69: 1 px per 30 fps frame).")]
    public float parallaxRateCap = 30f;

    [Tooltip("Seconds without pointer motion before the city view's look eases back to rest (the page's 4).")]
    public float idleSeconds = 4f;

    [Header("Traffic")]
    [Tooltip("How far (m) the ink hull sits behind its body in depth, so it only shows round the silhouette (the look-dev culls front faces; the port draws both and pushes the ink back).")]
    public float hullPush = 0.8f;

    [Tooltip("The windscreens' soft sun glint (0 off, 1 the look-dev's).")]
    [Range(0f, 1f)] public float glint = 1f;

    [Header("Hall windows")]
    [Tooltip("The hall's windows show the r17 city (their panels' material swapped at runtime; the old flyers hidden).")]
    public bool windowCity = true;

    [Tooltip("How much the hall's turn moves the city's camera (0: none, 1: the full look).")]
    [Range(0f, 1f)] public float windowParallax = 0.6f;

    [Tooltip("The hall windows' city redraws at most this often (fps) while the city view is not up (the loops run at 24).")]
    public float windowFps = 30f;

    [Tooltip("How much the hall's hour tints the city in its windows (the hall's own _StateWeights; 0: always noon). The city view itself has no hour grade (Saleh: one daytime look).")]
    [Range(0f, 1f)] public float windowHourTint = 1f;

    [Tooltip("The left wall's windows (mask red): their box on the hall canvas (uv, y down: x, y, w, h).")]
    public Vector4 leftWindowsBox = new Vector4(0f, 0f, 0.3131f, 0.6809f);

    [Tooltip("The plate rect the left wall's windows look out on (plate px, y down: x, y, w, h).")]
    public Vector4 leftWindowsPlate = new Vector4(300f, 20f, 1500f, 1086f);

    [Tooltip("The front clerestory windows (mask green): their box on the hall canvas (uv, y down).")]
    public Vector4 frontWindowsBox = new Vector4(0.4503f, 0.2942f, 0.1156f, 0.1671f);

    [Tooltip("The plate rect the front clerestory windows look out on (plate px, y down).")]
    public Vector4 frontWindowsPlate = new Vector4(1280f, 60f, 1000f, 480f);

    [Header("Budget")]
    [Tooltip("The city's CPU budget a frame (ms): over it, the hall windows' redraw rate halves until it fits.")]
    public float budgetMs = 2f;

    /// <summary>The art texture with a file's name (without extension), or null.</summary>
    public Texture2D Art(string file)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(file);
        foreach (Texture2D t in art)
            if (t != null && t.name == name)
                return t;
        return null;
    }
}
