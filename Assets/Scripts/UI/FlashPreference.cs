using UnityEngine;

/// <summary>
/// The player's Reduce flashing choice (GAME_FEEL_BIBLE G3, gap 2; BUG-62's
/// flashing setting): kept per player in PlayerPrefs, outside the run save
/// (the MotionPreference pattern), and handed to FlashPolicy, the one flash
/// budget every flash asks, when the game starts and whenever it changes.
/// Off by default.
/// </summary>
public static class FlashPreference
{
    /// <summary>The PlayerPrefs key ("on", or absent/"off").</summary>
    private const string Key = "TimeDesk.ReduceFlashing";

    /// <summary>True when the player chose Reduce flashing (saved at once when set).</summary>
    public static bool Reduce
    {
        get => FlashPolicy.ReduceFlashing;
        set
        {
            FlashPolicy.ReduceFlashing = value;
            PlayerPrefs.SetString(PlayerPrefKeys.For(Key), value ? "on" : "off");
            PlayerPrefs.Save();
        }
    }

    /// <summary>At start-up, before any scene: the stored choice goes to FlashPolicy.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Load() => FlashPolicy.ReduceFlashing = PlayerPrefs.GetString(PlayerPrefKeys.For(Key), "off") == "on";
}
