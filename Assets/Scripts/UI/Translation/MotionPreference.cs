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

    /// <summary>True when the player chose Reduced motion (saved at once when set).</summary>
    public static bool Reduced
    {
        get => PlayerPrefs.GetString(Key, FullValue) == ReducedValue;
        set
        {
            PlayerPrefs.SetString(Key, value ? ReducedValue : FullValue);
            PlayerPrefs.Save();
        }
    }
}
