using UnityEngine;

/// <summary>
/// The player's motion choice (piece 9 R17): Full (translations flip letter
/// by letter) or Reduced (a translation shows at once; the game feel's
/// springs cut to a short fade or a snap: no squash, no shake), and the
/// Motion intensity (0 to 100 %: how far every spring of the game feel
/// moves, UiMotion), the cameras' idle sway (off by default) and the machine
/// close-ups (on by default). Per-player preferences kept in PlayerPrefs, outside the
/// run save (the UiLanguagePreference pattern); motion is read each time a
/// traveller is presented, the intensity at each motion.
/// </summary>
public static class MotionPreference
{
    /// <summary>The PlayerPrefs key.</summary>
    private const string Key = "TimeDesk.ReducedMotion";

    /// <summary>The stored value for "reduced" ("full" or absent = full motion).</summary>
    private const string ReducedValue = "reduced";

    /// <summary>The stored value for "full".</summary>
    private const string FullValue = "full";

    /// <summary>The Motion intensity's PlayerPrefs key.</summary>
    private const string IntensityKey = "TimeDesk.MotionIntensity";

    /// <summary>The stored choice once read (every change goes through <see cref="Reduced"/>'s setter), so the per-frame readers (the hall's art, the city view) allocate nothing.</summary>
    private static bool? _reduced;

    /// <summary>The stored intensity once read (every change goes through <see cref="Intensity"/>'s setter).</summary>
    private static float? _intensity;

    /// <summary>The camera sway's PlayerPrefs key ("on" or absent/"off").</summary>
    private const string SwayKey = "TimeDesk.CameraSway";

    /// <summary>The stored camera sway once read (every change goes through <see cref="CameraSway"/>'s setter).</summary>
    private static bool? _sway;

    /// <summary>True when the player chose Reduced motion (saved at once when set; read from PlayerPrefs once, then cached).</summary>
    public static bool Reduced
    {
        get => _reduced ??= PlayerPrefs.GetString(PlayerPrefKeys.For(Key), FullValue) == ReducedValue;
        set
        {
            PlayerPrefs.SetString(PlayerPrefKeys.For(Key), value ? ReducedValue : FullValue);
            PlayerPrefs.Save();
            _reduced = value;
            Changed?.Invoke();
        }
    }

    /// <summary>The Motion intensity, 0 to 1 (1, full, when never set): how far the game feel's springs move (saved at once when set; read from PlayerPrefs once, then cached).</summary>
    public static float Intensity
    {
        get => _intensity ??= Mathf.Clamp01(PlayerPrefs.GetFloat(PlayerPrefKeys.For(IntensityKey), 1f));
        set
        {
            float clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PlayerPrefKeys.For(IntensityKey), clamped);
            PlayerPrefs.Save();
            _intensity = clamped;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// True when the player turned the cameras' idle sway on (Saleh 2026-10-07:
    /// "the sway in the camera is not good, it causes motion sickness. Have it
    /// off by default and have an option to turn it on in the settings"): off
    /// unless chosen; Reduced Motion turns it off whatever is stored (CameraFeel).
    /// Saved at once when set; read from PlayerPrefs once, then cached.
    /// </summary>
    public static bool CameraSway
    {
        get => _sway ??= PlayerPrefs.GetString(PlayerPrefKeys.For(SwayKey), "off") == "on";
        set
        {
            PlayerPrefs.SetString(PlayerPrefKeys.For(SwayKey), value ? "on" : "off");
            PlayerPrefs.Save();
            _sway = value;
            Changed?.Invoke();
        }
    }

    /// <summary>The machine close-ups' PlayerPrefs key ("off", or absent/"on").</summary>
    private const string CloseUpKey = "TimeDesk.MachineCloseUps";

    /// <summary>The stored close-up choice once read (every change goes through <see cref="MachineCloseUps"/>'s setter).</summary>
    private static bool? _closeUps;

    /// <summary>
    /// True when a machine in use is shown close (Saleh's 1018a playtest: "you
    /// cant see what each machiene does the camera should be better"; his call
    /// 2026-10-18: a Settings toggle, on by default; MachineCloseUp). Saved at
    /// once when set; read from PlayerPrefs once, then cached.
    /// </summary>
    public static bool MachineCloseUps
    {
        get => _closeUps ??= PlayerPrefs.GetString(PlayerPrefKeys.For(CloseUpKey), "on") != "off";
        set
        {
            PlayerPrefs.SetString(PlayerPrefKeys.For(CloseUpKey), value ? "on" : "off");
            PlayerPrefs.Save();
            _closeUps = value;
            Changed?.Invoke();
        }
    }

    /// <summary>Raised after the choice or the intensity is set, so a reader that runs every frame (the hall's lights and dust, HallLightingRig) caches it instead of reading PlayerPrefs each frame.</summary>
    public static event System.Action Changed;
}
