using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// The Settings display basics (BUG-62, CODE-104), kept per player in
/// PlayerPrefs (outside the run save) and applied at once: the window mode
/// and the resolution (Screen.SetResolution; only once the player chose one,
/// so a build keeps its launch settings until then), vsync
/// (QualitySettings.vSyncCount), brightness (a global volume's post
/// exposure, ±1 EV: SettingsRules.Exposure; it shows where the camera renders
/// post-processing, LIGHT's BUG-51) and Reduce flashing, the static
/// preference the flashing effects read (the hall's city windows and portal,
/// the Helix River monitor: HALL-FX; the lights: LIGHT; the feel bible's G3:
/// a flash becomes a static tint).
/// </summary>
public static class ScreenSettings
{
    /// <summary>The PlayerPrefs keys.</summary>
    private const string ModeKey = "TimeDesk.WindowMode", WidthKey = "TimeDesk.ResolutionWidth", HeightKey = "TimeDesk.ResolutionHeight",
                         VsyncKey = "TimeDesk.Vsync", BrightnessKey = "TimeDesk.Brightness", FlashKey = "TimeDesk.ReduceFlashing";

    /// <summary>The stored Reduce flashing once read (every change goes through its setter), so per-frame readers allocate nothing.</summary>
    private static bool? _reduceFlashing;

    /// <summary>The global volume brightness drives (made on the first change away from the middle).</summary>
    private static Volume _brightness;

    /// <summary>The window mode: the stored one, else the screen's now.</summary>
    public static WindowMode Mode
    {
        get
        {
            if (PlayerPrefs.HasKey(PlayerPrefKeys.For(ModeKey)))
                return (WindowMode)Mathf.Clamp(PlayerPrefs.GetInt(PlayerPrefKeys.For(ModeKey)), 0, (int)WindowMode.Fullscreen);
            return Screen.fullScreenMode == FullScreenMode.Windowed ? WindowMode.Windowed
                : Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen ? WindowMode.Fullscreen : WindowMode.Borderless;
        }
    }

    /// <summary>The resolution: the stored one, else the screen's now.</summary>
    public static ScreenSize Resolution =>
        PlayerPrefs.HasKey(PlayerPrefKeys.For(WidthKey))
            ? new ScreenSize(PlayerPrefs.GetInt(PlayerPrefKeys.For(WidthKey)), PlayerPrefs.GetInt(PlayerPrefKeys.For(HeightKey)))
            : new ScreenSize(Screen.width, Screen.height);

    /// <summary>True when vsync is on: the stored choice, else the quality level's.</summary>
    public static bool Vsync =>
        PlayerPrefs.HasKey(PlayerPrefKeys.For(VsyncKey)) ? PlayerPrefs.GetInt(PlayerPrefKeys.For(VsyncKey)) != 0 : QualitySettings.vSyncCount > 0;

    /// <summary>The brightness slider's share (0.5, the game as painted, until moved).</summary>
    public static float Brightness => Mathf.Clamp01(PlayerPrefs.GetFloat(PlayerPrefKeys.For(BrightnessKey), 0.5f));

    /// <summary>True when the player chose Reduce flashing (off by default; saved at once when set; read once, then cached).</summary>
    public static bool ReduceFlashing
    {
        get => _reduceFlashing ??= PlayerPrefs.GetInt(PlayerPrefKeys.For(FlashKey), 0) != 0;
        set
        {
            _reduceFlashing = value;
            PlayerPrefs.SetInt(PlayerPrefKeys.For(FlashKey), value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>The resolutions Settings offers (SettingsRules.Resolutions over the screen's).</summary>
    public static System.Collections.Generic.List<ScreenSize> Resolutions()
    {
        var offered = new System.Collections.Generic.List<ScreenSize>();
        foreach (UnityEngine.Resolution r in Screen.resolutions)
            offered.Add(new ScreenSize(r.width, r.height));
        return SettingsRules.Resolutions(offered, Resolution);
    }

    /// <summary>Stores and applies a window mode.</summary>
    public static void SetMode(WindowMode mode)
    {
        PlayerPrefs.SetInt(PlayerPrefKeys.For(ModeKey), (int)mode);
        StoreSize(Resolution);
        ApplyWindow();
    }

    /// <summary>Stores and applies a resolution.</summary>
    public static void SetResolution(ScreenSize size)
    {
        if (!PlayerPrefs.HasKey(PlayerPrefKeys.For(ModeKey)))
            PlayerPrefs.SetInt(PlayerPrefKeys.For(ModeKey), (int)Mode);
        StoreSize(size);
        ApplyWindow();
    }

    /// <summary>Stores and applies vsync.</summary>
    public static void SetVsync(bool on)
    {
        PlayerPrefs.SetInt(PlayerPrefKeys.For(VsyncKey), on ? 1 : 0);
        PlayerPrefs.Save();
        QualitySettings.vSyncCount = on ? 1 : 0;
    }

    /// <summary>Stores and applies the brightness share.</summary>
    public static void SetBrightness(float share)
    {
        PlayerPrefs.SetFloat(PlayerPrefKeys.For(BrightnessKey), Mathf.Clamp01(share));
        PlayerPrefs.Save();
        ApplyBrightness();
    }

    /// <summary>Applies every stored choice (at startup: SettingsApplier).</summary>
    public static void Apply()
    {
        if (PlayerPrefs.HasKey(PlayerPrefKeys.For(ModeKey)))
            ApplyWindow();
        if (PlayerPrefs.HasKey(PlayerPrefKeys.For(VsyncKey)))
            QualitySettings.vSyncCount = Vsync ? 1 : 0;
        ApplyBrightness();
    }

    /// <summary>Stores a resolution.</summary>
    private static void StoreSize(ScreenSize size)
    {
        PlayerPrefs.SetInt(PlayerPrefKeys.For(WidthKey), size.Width);
        PlayerPrefs.SetInt(PlayerPrefKeys.For(HeightKey), size.Height);
        PlayerPrefs.Save();
    }

    /// <summary>The stored window mode and resolution on the screen.</summary>
    private static void ApplyWindow()
    {
        ScreenSize size = Resolution;
        FullScreenMode mode = Mode == WindowMode.Windowed ? FullScreenMode.Windowed
            : Mode == WindowMode.Fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.FullScreenWindow;
        if (size.Width > 0 && size.Height > 0)
            Screen.SetResolution(size.Width, size.Height, mode);
    }

    /// <summary>The brightness's exposure on a global volume (made once, kept across scenes, off in the middle).</summary>
    private static void ApplyBrightness()
    {
        float exposure = SettingsRules.Exposure(Brightness);
        bool neutral = Mathf.Approximately(exposure, 0f);
        if (_brightness == null)
        {
            if (neutral || !Application.isPlaying)
                return;
            var host = new GameObject("BrightnessVolume");
            Object.DontDestroyOnLoad(host);
            _brightness = host.AddComponent<Volume>();
            _brightness.isGlobal = true;
            _brightness.priority = 1000f;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.DontSave;
            profile.Add<ColorAdjustments>();
            _brightness.sharedProfile = profile;
        }
        if (_brightness.sharedProfile.TryGet(out ColorAdjustments adjust))
            adjust.postExposure.Override(exposure);
        _brightness.enabled = !neutral;
    }
}
