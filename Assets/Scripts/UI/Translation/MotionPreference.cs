using UnityEngine;

/// <summary>
/// The player's motion choice (piece 9 R17): Full (translations flip letter
/// by letter) or Reduced (a translation shows at once). A per-player
/// preference kept in PlayerPrefs, outside the run save (the
/// UiLanguagePreference pattern); it is read each time a traveller is presented.
/// </summary>
public static class MotionPreference
{
    /// <summary>The PlayerPrefs key.</summary>
    private const string Key = "TimeDesk.ReducedMotion";

    /// <summary>The stored value for "reduced" ("full" or absent = full motion).</summary>
    private const string ReducedValue = "reduced";

    /// <summary>The stored value for "full".</summary>
    private const string FullValue = "full";

    /// <summary>The stored choice once read (every change goes through <see cref="Reduced"/>'s setter), so the per-frame readers (the hall's art, the city view) allocate nothing.</summary>
    private static bool? _reduced;

    /// <summary>True when the player chose Reduced motion (saved at once when set; read from PlayerPrefs once, then cached).</summary>
    public static bool Reduced
    {
        get => _reduced ??= PlayerPrefs.GetString(Key, FullValue) == ReducedValue;
        set
        {
            PlayerPrefs.SetString(Key, value ? ReducedValue : FullValue);
            PlayerPrefs.Save();
            _reduced = value;
            Changed?.Invoke();
        }
    }

    /// <summary>Raised after the choice is set, so a reader that runs every frame (the hall's lights and dust, HallLightingRig) caches it instead of reading PlayerPrefs each frame.</summary>
    public static event System.Action Changed;
}
