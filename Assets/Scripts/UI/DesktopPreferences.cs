using UnityEngine;

/// <summary>
/// The player's desktop preferences (the PC redesign DK4, DK6, ST1, section
/// 4.7): where the icons sit, whether one click or two opens an icon, and
/// whether the Investigation app's steps checklist shows.
/// Per-player values in PlayerPrefs (the UiLanguagePreference pattern),
/// outside the run save, so New Run keeps them; saved at once when set.
/// Later phases add their own keys here (the app's tabs, steps, zoom).
/// </summary>
public static class DesktopPreferences
{
    /// <summary>The icons' layout key ("id:x,y;..." in desktop units, DesktopLayout.Save).</summary>
    private const string IconsKey = "TimeDesk.DesktopIcons";

    /// <summary>The icon opening key.</summary>
    private const string IconOpenKey = "TimeDesk.IconOpen";

    /// <summary>The stored value for "Single click" ("double" or absent = double click).</summary>
    private const string Single = "single";

    /// <summary>The stored value for "Double click".</summary>
    private const string Double = "double";

    /// <summary>The steps checklist's key (the PC redesign ST1).</summary>
    private const string StepsKey = "TimeDesk.StepsShown";

    /// <summary>The stored value for hidden steps ("shown" or absent = shown).</summary>
    private const string Hidden = "hidden";

    /// <summary>The stored value for shown steps.</summary>
    private const string Shown = "shown";

    /// <summary>The saved icon layout, or "" when the player never moved an icon (the default arrangement).</summary>
    public static string IconPositions
    {
        get => PlayerPrefs.GetString(IconsKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(IconsKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    /// <summary>True when one click opens a desktop icon (Settings' accessibility choice); false (the default) when it takes a double click.</summary>
    public static bool OpenIconsWithSingleClick
    {
        get => PlayerPrefs.GetString(IconOpenKey, Double) == Single;
        set
        {
            PlayerPrefs.SetString(IconOpenKey, value ? Single : Double);
            PlayerPrefs.Save();
        }
    }

    /// <summary>True (the default) when the Investigation app's sidebar shows the steps checklist; false when the player hid it (the toolbar's Steps, Settings).</summary>
    public static bool StepsShown
    {
        get => PlayerPrefs.GetString(StepsKey, Shown) != Hidden;
        set
        {
            PlayerPrefs.SetString(StepsKey, value ? Shown : Hidden);
            PlayerPrefs.Save();
        }
    }
}
