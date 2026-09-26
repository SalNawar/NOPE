using UnityEngine;

/// <summary>
/// The player's desktop preferences (the PC redesign DK4, DK6, KB5, section
/// 4.7): where the icons sit, whether one click or two opens an icon, the
/// Investigation app's default zoom (Settings' Text size) and whether its
/// sidebar shows. Per-player values in PlayerPrefs (the UiLanguagePreference
/// pattern), outside the run save, so New Run keeps them; saved at once when
/// set. Later phases add their own keys here (the app's tabs, steps).
/// </summary>
public static class DesktopPreferences
{
    /// <summary>The icons' layout key ("id:x,y;..." in desktop units, DesktopLayout.Save).</summary>
    private const string IconsKey = "TimeDesk.DesktopIcons";

    /// <summary>The icon opening key.</summary>
    private const string IconOpenKey = "TimeDesk.IconOpen";

    /// <summary>The app's default zoom key ("100", "125" or "150"; AppZoom.Parse reads it).</summary>
    private const string ZoomKey = "TimeDesk.AppZoom";

    /// <summary>The app's sidebar key ("shown" or "hidden").</summary>
    private const string SidebarKey = "TimeDesk.SidebarShown";

    /// <summary>The stored value for "Single click" ("double" or absent = double click).</summary>
    private const string Single = "single";

    /// <summary>The stored value for "Double click".</summary>
    private const string Double = "double";

    /// <summary>The stored value for a hidden sidebar ("shown" or absent = shown).</summary>
    private const string Hidden = "hidden";

    /// <summary>The stored value for a shown sidebar.</summary>
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

    /// <summary>The Investigation app's default zoom as saved ("" when never set: 100 %); AppZoom.Parse reads it against the levels.</summary>
    public static string DefaultZoom
    {
        get => PlayerPrefs.GetString(ZoomKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(ZoomKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    /// <summary>True (the default) while the Investigation app's sidebar shows; Ctrl+B hides it.</summary>
    public static bool SidebarShown
    {
        get => PlayerPrefs.GetString(SidebarKey, Shown) != Hidden;
        set
        {
            PlayerPrefs.SetString(SidebarKey, value ? Shown : Hidden);
            PlayerPrefs.Save();
        }
    }
}
