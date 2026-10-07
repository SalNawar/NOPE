using System.Collections.Generic;
using System.Linq;

/// <summary>What the desktop's wallpaper shows (DesktopWallpaper.Pick): a world factor's outcome, or a culture's (neutral when none leads).</summary>
public readonly struct WallpaperPick
{
    /// <summary>A pick of <paramref name="factor"/>'s outcome <paramref name="outcome"/> (the culture factor: a culture id or "neutral").</summary>
    public WallpaperPick(string factor, string outcome)
    {
        Factor = factor;
        Outcome = outcome;
    }

    /// <summary>The factor whose wallpaper shows ("government", "future", "money"), or <see cref="DesktopWallpaper.CultureFactor"/>.</summary>
    public string Factor { get; }

    /// <summary>The outcome id ("democracy"), or for the culture the leading culture's id ("japan") or <see cref="DesktopWallpaper.Neutral"/>.</summary>
    public string Outcome { get; }

    /// <summary>True when the culture's wallpaper shows (no factor has left the world as the run found it).</summary>
    public bool IsCulture => Factor == DesktopWallpaper.CultureFactor;
}

/// <summary>
/// The desktop shows the wallpaper of the most recent world change (Saleh
/// 2026-10-07: wallpapers "immediately recognizable which culture or nation
/// or whatever is influencing it, not only nations"; the rule is Claude's
/// decision, recorded in docs/FEATURES.md "Fake-OS desktop"). When one or
/// more factors answered by pulls have left their "as you found it" outcome
/// (WorldState.leads, latched each night), the one that changed most
/// recently (the latest FactorLead.sinceDay) wins, ties going to content
/// order (government, then future, then money); a split shows its newer half
/// (<see cref="Newer"/>). Otherwise the leading culture's wallpaper shows, or
/// the neutral one when no culture leads. Pure; tested (DesktopWallpaperTests).
/// </summary>
public static class DesktopWallpaper
{
    /// <summary>The factor id of a culture pick.</summary>
    public const string CultureFactor = "culture";

    /// <summary>The culture pick's outcome while no culture leads.</summary>
    public const string Neutral = "neutral";

    /// <summary>
    /// The wallpaper for <paramref name="leads"/> (the latched answers) over
    /// <paramref name="factors"/> (the pull factors, content order), with
    /// <paramref name="leaderCultureId"/> the leading culture (null or blank:
    /// none).
    /// </summary>
    public static WallpaperPick Pick(IEnumerable<PullFactor> factors, IEnumerable<FactorLead> leads, string leaderCultureId)
    {
        List<FactorLead> held = (leads ?? Enumerable.Empty<FactorLead>()).Where(l => l != null).ToList();
        WallpaperPick? best = null;
        int bestDay = int.MinValue;
        foreach (PullFactor f in factors ?? Enumerable.Empty<PullFactor>())
        {
            FactorLead lead = held.FirstOrDefault(l => l.factor == f.Id);
            if (lead == null || string.IsNullOrEmpty(lead.outcome) || (!lead.IsSplit && lead.outcome == f.StatusQuo))
                continue;
            if (best.HasValue && lead.sinceDay <= bestDay)
                continue;
            best = new WallpaperPick(f.Id, Newer(lead, f.StatusQuo));
            bestDay = lead.sinceDay;
        }
        return best ?? new WallpaperPick(CultureFactor, string.IsNullOrWhiteSpace(leaderCultureId) ? Neutral : leaderCultureId);
    }

    /// <summary>
    /// The newer half of an answer: a single outcome itself; of a split, the
    /// half that is not the "as you found it" outcome, else the first half
    /// (the one that passed the held answer: WorldPulls.Lead puts it first).
    /// </summary>
    public static string Newer(FactorLead lead, string statusQuo)
    {
        if (lead == null)
            return null;
        if (!lead.IsSplit)
            return lead.outcome;
        return lead.outcome == statusQuo ? lead.split : lead.outcome;
    }
}
