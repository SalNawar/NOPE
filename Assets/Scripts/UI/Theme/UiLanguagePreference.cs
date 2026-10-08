using UnityEngine;

/// <summary>
/// The player's UI language choice (piece 6 U12): follow history (the
/// culture's labels) or always English, the default (CultureChoice.ChoseEnglish:
/// a player who never chose reads English until the Translation Lens's day).
/// A per-player preference kept in PlayerPrefs, outside the run save, so New
/// Run keeps it. Colours, fonts and the wallpaper follow history either way;
/// the choice applies at the next scene load; from the lens's day the labels
/// follow history whatever it holds (CultureChoice.EnglishBySetting).
/// </summary>
public static class UiLanguagePreference
{
    /// <summary>The PlayerPrefs key.</summary>
    private const string Key = "TimeDesk.UiLanguage";

    /// <summary>The stored choice (CultureChoice.AlwaysEnglishChoice or FollowHistoryChoice), or null when the player never chose.</summary>
    public static string Stored => PlayerPrefs.HasKey(PlayerPrefKeys.For(Key)) ? PlayerPrefs.GetString(PlayerPrefKeys.For(Key)) : null;

    /// <summary>True when the choice is "Always English" (the default; CultureChoice.ChoseEnglish); setting it stores the choice.</summary>
    public static bool AlwaysEnglish
    {
        get => CultureChoice.ChoseEnglish(Stored);
        set
        {
            PlayerPrefs.SetString(PlayerPrefKeys.For(Key), value ? CultureChoice.AlwaysEnglishChoice : CultureChoice.FollowHistoryChoice);
            PlayerPrefs.Save();
        }
    }
}
