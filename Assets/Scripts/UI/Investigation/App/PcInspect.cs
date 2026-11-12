using UnityEngine;

/// <summary>
/// The one gate on the PC's picks (the Lens screen): a click on a value, a
/// rule line, the date, a chip line or the Registry's "No entry" line picks
/// only while inspect is on. With inspect off the app's notice says "Press
/// Space to inspect." and nothing is picked. The desk has its own inspect
/// mode: it does not pass here.
/// </summary>
public static class PcInspect
{
    /// <summary>True when a pick that starts in <paramref name="from"/> (a view of the app) may go on. False: the notice shows.</summary>
    public static bool Allows(Component from)
    {
        InvestigationApp app = from != null ? from.GetComponentInParent<InvestigationApp>() : null;
        return app == null || app.AllowPick();
    }
}
