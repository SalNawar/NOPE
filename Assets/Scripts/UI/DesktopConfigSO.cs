using UnityEngine;

/// <summary>
/// The PC desktop's layout and timing knobs (the PC redesign section 4.9;
/// DeskConfigSO stays the physical desk): the taskbar and the compare dock
/// under the icon area, the windows' title bars, the taskbar's window
/// buttons, the desktop's icons (their cell, the arrange grid, the default
/// order, the drop's overlap share), the double-click, the Internet's caps
/// (the news back issues, the browser's history), the app windows' sizes,
/// the Investigation app's scan toast, its search and found flash, and the
/// Notes limits. Created and assigned by Tools > TimeDesk > Build Office
/// UI (Assets/Data/Config/Desktop_Default.asset). The builder reads the sizes
/// (re-run it after changing one); DesktopWindowManager and DesktopIcons read
/// the double-click, DesktopIcons the icon knobs, InvestigationApp the
/// toast's time, SearchBox the search knobs and FoundMark the flash's, at
/// runtime.
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

    [Header("Desktop icons (the PC redesign DK2-DK5; desktop units)")]
    /// <summary>An icon's cell: the glyph over its label.</summary>
    public Vector2 iconCellSize = new Vector2(120f, 132f);

    /// <summary>The glyph's square at the top of the cell.</summary>
    [Min(1f)] public float iconGlyphSize = 72f;

    /// <summary>The label's text size (at most two lines under the glyph).</summary>
    [Min(1f)] public float iconLabelSize = 20f;

    /// <summary>The badge's circle at the glyph's top right.</summary>
    [Min(1f)] public float iconBadgeSize = 28f;

    /// <summary>The first arrange spot's top-left, from the icon area's top-left.</summary>
    public Vector2 iconOrigin = new Vector2(20f, 20f);

    /// <summary>From one arrange column to the next.</summary>
    [Min(1f)] public float iconColumnStep = 132f;

    /// <summary>From one arrange row to the next.</summary>
    [Min(1f)] public float iconRowStep = 140f;

    /// <summary>The icons' default order (DesktopAppIds): Arrange lays them out column-first in it, and the Start menu lists the apps in it.</summary>
    public string[] iconOrder = System.Linq.Enumerable.ToArray(DesktopAppIds.DefaultOrder);

    /// <summary>A dropped icon covering more than this share of another icon's cell moves to the nearest free arrange spot.</summary>
    [Range(0f, 1f)] public float iconDropOverlap = 0.25f;

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

    [Header("The Investigation app (redesign phase 16)")]
    /// <summary>The app window's restored size (it opens maximised; P spec WN4).</summary>
    public Vector2 investigationWindowSize = new Vector2(1120f, 820f);

    /// <summary>How long a scan's toast shows, in seconds (WN5).</summary>
    [Min(0.5f)] public float toastSeconds = 4f;

    [Header("Notes (redesign phase 25)")]
    /// <summary>The most day pages Notes keeps (WorldState.notes); the oldest go first.</summary>
    [Min(1)] public int notesDaysKept = 30;

    /// <summary>The most characters a page's typed notes hold.</summary>
    [Min(1)] public int notesMaxChars = 2000;

    /// <summary>The most clippings a page holds.</summary>
    [Min(1)] public int notesMaxClippings = 40;

    [Header("Search (redesign phase 19; the PC spec's SE1, SE4)")]
    /// <summary>How long typing pauses before the results update, in seconds.</summary>
    [Min(0f)] public float searchDebounceSeconds = 0.15f;

    /// <summary>The hits a source's group shows before "Show all n".</summary>
    [Min(1)] public int searchPerGroup = 5;

    /// <summary>How long the found flash's pulses take, in seconds (then its outline stays).</summary>
    [Min(0.1f)] public float foundSeconds = 1.2f;

    /// <summary>How many times the found item pulses.</summary>
    [Min(1)] public int foundPulses = 2;

    /// <summary>The pulse's strongest fill: this share of the found colour's opacity.</summary>
    [Range(0f, 1f)] public float foundPulseAlpha = 0.4f;

    /// <summary>A maximised window's bottom edge above the desktop's bottom: the taskbar and the dock (the icon area starts there).</summary>
    public float MaximisedBottom => taskbarHeight + dockHeight;
}
