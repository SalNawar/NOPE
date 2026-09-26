using UnityEngine;

/// <summary>
/// The player's desktop preferences (the PC redesign DK4, DK6, AP3, AP4,
/// section 4.7): where the icons sit, whether one click or two opens an icon,
/// the Investigation app's tab order and whether it shows two panes.
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

    /// <summary>The Investigation app's tab order key (TabOrder.Save: "Documents,Records,...").</summary>
    private const string AppTabsKey = "TimeDesk.AppTabs";

    /// <summary>The Investigation app's split key ("on" or "off").</summary>
    private const string AppSplitKey = "TimeDesk.AppSplit";

    /// <summary>The stored value for two panes.</summary>
    private const string On = "on";

    /// <summary>The stored value for one pane.</summary>
    private const string Off = "off";

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

    /// <summary>The Investigation app's saved tab order (TabOrder.Parse reads it), or "" for the default order.</summary>
    public static string AppTabs
    {
        get => PlayerPrefs.GetString(AppTabsKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(AppTabsKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    /// <summary>True (the default) when the player wants the Investigation app's two panes side by side (it shows them while the window is wide enough).</summary>
    public static bool AppSplit
    {
        get => PlayerPrefs.GetString(AppSplitKey, On) != Off;
        set
        {
            PlayerPrefs.SetString(AppSplitKey, value ? On : Off);
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
}
