using UnityEngine;

/// <summary>
/// The PC desktop's layout and timing knobs (the PC redesign section 4.9;
/// DeskConfigSO stays the physical desk): the taskbar and the compare dock
/// under the icon area, the windows' title bars, the taskbar's window
/// buttons, the double-click, the Internet's caps (the news back issues,
/// the browser's history), the app windows' sizes and the Notes limits.
/// Created and assigned by Tools > TimeDesk >
/// Build Office UI (Assets/Data/Config/Desktop_Default.asset). The builder
/// reads the sizes (re-run it after changing one); DesktopWindowManager
/// reads the maximise area and the double-click at runtime.
/// </summary>
[CreateAssetMenu(fileName = "Desktop_Default", menuName = "TimeDesk/Office/Desktop Config")]
public sealed class DesktopConfigSO : ScriptableObject
{
    [Header("The bars under the icon area (desktop units)")]
    /// <summary>The taskbar's height at the bottom of the desktop.</summary>
    [Min(1f)] public float taskbarHeight = 36f;

    /// <summary>The compare dock's height, right above the taskbar (reserved even while it is hidden, so a maximised window never changes size).</summary>
    [Min(1f)] public float dockHeight = 56f;

    /// <summary>The gap between the dock and the Start menu above it.</summary>
    [Min(0f)] public float startMenuGap = 4f;

    [Header("Windows")]
    /// <summary>A window's title bar height.</summary>
    [Min(1f)] public float titleBarHeight = 36f;

    /// <summary>A window's title size.</summary>
    [Min(1f)] public float titleFontSize = 20f;

    [Header("Taskbar buttons (one per open window)")]
    /// <summary>A button's width while there is room.</summary>
    [Min(1f)] public float taskbarButtonMaxWidth = 200f;

    /// <summary>The width buttons shrink to when many windows are open.</summary>
    [Min(1f)] public float taskbarButtonMinWidth = 48f;

    [Header("Double-click (title bars)")]
    /// <summary>The most time between the two clicks of a double-click, in seconds.</summary>
    [Min(0f)] public float doubleClickSeconds = 0.4f;

    /// <summary>The farthest the second click may be from the first, in desktop units.</summary>
    [Min(0f)] public float doubleClickDistance = 6f;

    [Header("Internet")]
    /// <summary>The morning papers the News site keeps as back issues (WorldState.newsArchive; the oldest go first).</summary>
    [Min(1)] public int newsArchiveIssues = 30;

    /// <summary>The pages the browser's Back can return through.</summary>
    [Min(1)] public int browserHistory = 30;

    [Header("App windows (redesign phase 25; restored sizes, desktop units)")]
    /// <summary>The Mail window's size.</summary>
    public Vector2 mailWindowSize = new Vector2(920f, 720f);

    /// <summary>The Citizen Account window's size.</summary>
    public Vector2 accountWindowSize = new Vector2(720f, 780f);

    /// <summary>The Notes window's size.</summary>
    public Vector2 notesWindowSize = new Vector2(780f, 720f);

    /// <summary>The Settings window's size.</summary>
    public Vector2 settingsWindowSize = new Vector2(640f, 720f);

    [Header("Notes (redesign phase 25)")]
    /// <summary>The most day pages Notes keeps (WorldState.notes); the oldest go first.</summary>
    [Min(1)] public int notesDaysKept = 30;

    /// <summary>The most characters a page's typed notes hold.</summary>
    [Min(1)] public int notesMaxChars = 2000;

    /// <summary>The most clippings a page holds.</summary>
    [Min(1)] public int notesMaxClippings = 40;

    /// <summary>A maximised window's bottom edge above the desktop's bottom: the taskbar and the dock (the icon area starts there).</summary>
    public float MaximisedBottom => taskbarHeight + dockHeight;
}
