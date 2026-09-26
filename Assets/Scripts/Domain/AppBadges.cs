using System.Collections.Generic;

/// <summary>
/// The Investigation app's tab badges (the PC redesign WN5: nothing steals the
/// view). Something new for a tab (a scan for Documents, a line for the
/// Transcript, a deviation for the Report) badges it unless the player is
/// looking at that tab; showing the tab clears its badge; a new case clears
/// them all. The badge says where to look; nothing switches the tab. Pure;
/// InvestigationApp applies it.
/// </summary>
public sealed class AppBadges
{
    private readonly HashSet<AppTab> _badged = new HashSet<AppTab>();

    /// <summary>
    /// Something new arrived for <paramref name="tab"/>; <paramref name="visible"/>
    /// is the tab the player sees (null while the app is closed or minimised).
    /// Returns true when the tab is badged now.
    /// </summary>
    public bool Arrived(AppTab tab, AppTab? visible)
    {
        if (visible == tab)
            return false;
        _badged.Add(tab);
        return true;
    }

    /// <summary>The player sees the tab: its badge goes.</summary>
    public void Seen(AppTab tab) => _badged.Remove(tab);

    /// <summary>A new case: every badge goes.</summary>
    public void Clear() => _badged.Clear();

    /// <summary>True while the tab is badged.</summary>
    public bool IsBadged(AppTab tab) => _badged.Contains(tab);
}
