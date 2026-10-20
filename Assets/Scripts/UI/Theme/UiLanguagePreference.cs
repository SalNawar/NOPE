using UnityEngine;

/// <summary>
/// The player's language choice for the papers (piece 6 U12; D7: only the
/// papers' printed words and the newspaper take the world's language, the PC
/// and the chrome never do): "Follow history" (the culture's labels) or
/// "Always English". A per-player preference kept in PlayerPrefs, outside the
/// run save, so New Run keeps it. It never locks and applies at once (BUG-3a):
/// a player who never chose reads English until the culture day and the
/// world's language from then on (CultureChoice.EnglishBySetting).
/// </summary>
public static class UiLanguagePreference
{
    /// <summary>The PlayerPrefs key.</summary>
    private const string Key = "TimeDesk.UiLanguage";

    /// <summary>The stored choice (CultureChoice.AlwaysEnglishChoice or FollowHistoryChoice), or null when the player never chose.</summary>
    public static string Stored => PlayerPrefs.HasKey(PlayerPrefKeys.For(Key)) ? PlayerPrefs.GetString(PlayerPrefKeys.For(Key)) : null;

    /// <summary>Stores the choice: "Always English" when <paramref name="alwaysEnglish"/>, else "Follow history".</summary>
    public static void Choose(bool alwaysEnglish)
    {
        PlayerPrefs.SetString(PlayerPrefKeys.For(Key), alwaysEnglish ? CultureChoice.AlwaysEnglishChoice : CultureChoice.FollowHistoryChoice);
        PlayerPrefs.Save();
    }
}
