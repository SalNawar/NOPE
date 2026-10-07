using UnityEngine;

/// <summary>
/// The player's desktop preferences (the PC redesign DK4, DK6, AP3, AP4, KB5,
/// ST1, section 4.7): where the icons sit, whether one click or two opens an
/// icon, whether the Investigation app shows two panes, its default zoom
/// (Settings' Text size), whether its findings column shows and whether its
/// step hints show. Per-player values in PlayerPrefs (the
/// UiLanguagePreference pattern), outside the run save, so New Run keeps
/// them; saved at once when set.
/// </summary>
public static class DesktopPreferences
{
    /// <summary>The icons' layout key ("id:x,y;..." in desktop units, DesktopLayout.Save).</summary>
    private const string IconsKey = "TimeDesk.DesktopIcons";

    /// <summary>The icon opening key.</summary>
    private const string IconOpenKey = "TimeDesk.IconOpen";

    /// <summary>The app's default zoom key ("100", "125" or "150"; AppZoom.Parse reads it).</summary>
    private const string ZoomKey = "TimeDesk.AppZoom";

    /// <summary>The app's findings column key ("shown" or "hidden"; the sidebar's key kept, so a player's choice carries over).</summary>
    private const string SidebarKey = "TimeDesk.SidebarShown";

    /// <summary>The stored value for "Single click" ("double" or absent = double click).</summary>
    private const string Single = "single";

    /// <summary>The stored value for "Double click".</summary>
    private const string Double = "double";

    /// <summary>The Investigation app's split key ("on" or "off").</summary>
    private const string AppSplitKey = "TimeDesk.AppSplit";

    /// <summary>The stored value for two panes.</summary>
    private const string On = "on";

    /// <summary>The stored value for one pane.</summary>
    private const string Off = "off";

    /// <summary>The stored value for a hidden sidebar ("shown" or absent = shown).</summary>
    private const string Hidden = "hidden";

    /// <summary>The stored value for a shown sidebar.</summary>
    private const string Shown = "shown";

    /// <summary>The saved icon layout, or "" when the player never moved an icon (the default arrangement).</summary>
    public static string IconPositions
    {
        get => PlayerPrefs.GetString(PlayerPrefKeys.For(IconsKey), string.Empty);
        set
        {
            PlayerPrefs.SetString(PlayerPrefKeys.For(IconsKey), value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    /// <summary>True (the default) when the player wants the Investigation app's two panes side by side (it shows them while the window is wide enough).</summary>
    public static bool AppSplit
    {
        get => PlayerPrefs.GetString(PlayerPrefKeys.For(AppSplitKey), On) != Off;
        set
        {
            PlayerPrefs.SetString(PlayerPrefKeys.For(AppSplitKey), value ? On : Off);
            PlayerPrefs.Save();
        }
    }

    /// <summary>True when one click opens a desktop icon (Settings' accessibility choice); false (the default) when it takes a double click.</summary>
    public static bool OpenIconsWithSingleClick
    {
        get => PlayerPrefs.GetString(PlayerPrefKeys.For(IconOpenKey), Double) == Single;
        set
        {
            PlayerPrefs.SetString(PlayerPrefKeys.For(IconOpenKey), value ? Single : Double);
            PlayerPrefs.Save();
        }
    }

    /// <summary>The Investigation app's default zoom as saved ("" when never set: 100 %); AppZoom.Parse reads it against the levels.</summary>
    public static string DefaultZoom
    {
        get => PlayerPrefs.GetString(PlayerPrefKeys.For(ZoomKey), string.Empty);
        set
        {
            PlayerPrefs.SetString(PlayerPrefKeys.For(ZoomKey), value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }

    /// <summary>True (the default) while the Investigation app's findings column shows; Ctrl+B hides it.</summary>
    public static bool SidebarShown
    {
        get => PlayerPrefs.GetString(PlayerPrefKeys.For(SidebarKey), Shown) != Hidden;
        set
        {
            PlayerPrefs.SetString(PlayerPrefKeys.For(SidebarKey), value ? Shown : Hidden);
            PlayerPrefs.Save();
        }
    }
}
