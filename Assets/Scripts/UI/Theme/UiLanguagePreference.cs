using UnityEngine;

/// <summary>
/// The player's UI language choice (piece 6 U12): follow history (the
/// culture's labels) or always English. A per-player preference kept in
/// PlayerPrefs, outside the run save, so New Run keeps it. Colours, fonts and
/// the wallpaper follow history either way; the choice applies at the next
/// scene load.
/// </summary>
public static class UiLanguagePreference
{
    /// <summary>The PlayerPrefs key.</summary>
    private const string Key = "TimeDesk.UiLanguage";

    /// <summary>The stored value for "always English" ("history" or absent = follow history).</summary>
    private const string English = "english";

    /// <summary>The stored value for "follow history".</summary>
    private const string FollowHistory = "history";

    /// <summary>True when the player chose "Always English".</summary>
    public static bool AlwaysEnglish
    {
        get => PlayerPrefs.GetString(Key, FollowHistory) == English;
        set
        {
            PlayerPrefs.SetString(Key, value ? English : FollowHistory);
            PlayerPrefs.Save();
        }
    }
}
