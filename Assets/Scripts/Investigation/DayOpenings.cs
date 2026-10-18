using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The destinations today's repaired portals open (Saleh 2026-10-18,
/// answering the demo 1018a fix: "repairing a portal also opens its
/// destination for travellers"): the open routes of the repaired departure
/// portals (PortalDay.RepairedRoutes), set when the day's world is built
/// (ContentLibrarySO.BuildToday), as place keys and as the names the
/// rulebook prints. TravelRuleSO reads them, so an open-destinations rule
/// opens them for every reader at once: the case generation's claimed
/// destinations and verdicts (Directive, Allows, DayPlanSO.ClaimAllowed),
/// the rulebook, the Rules app, search and the case board (Summary). Cleared
/// at each play start and back in edit mode, so the content checks read the
/// rules as authored.
/// </summary>
public static class DayOpenings
{
    private static readonly string[] None = Array.Empty<string>();

    /// <summary>The opened places' keys ("country:era", Directives.PlaceKey); empty when no repair opens one.</summary>
    public static IReadOnlyList<string> Keys { get; private set; } = None;

    /// <summary>The opened places' names, in portal order (the rulebook's line).</summary>
    public static IReadOnlyList<string> Names { get; private set; } = None;

    /// <summary>The day they were set for (0: none).</summary>
    public static int Day { get; private set; }

    /// <summary>Today's openings from <paramref name="portals"/>' repaired routes on <paramref name="day"/>, each named by <paramref name="name"/>.</summary>
    public static void Set(int day, PortalDay portals, Func<PlaceRef, string> name)
    {
        IReadOnlyList<PlaceRef> routes = portals != null ? portals.RepairedRoutes : null;
        Day = day;
        if (routes == null || routes.Count == 0)
        {
            Keys = None;
            Names = None;
            return;
        }
        var keys = new string[routes.Count];
        var names = new string[routes.Count];
        for (int i = 0; i < routes.Count; i++)
        {
            keys[i] = Directives.PlaceKey(routes[i].NationId, routes[i].EraId);
            names[i] = name != null ? name(routes[i]) : routes[i].ToString();
        }
        Keys = keys;
        Names = names;
    }

    /// <summary>No openings (the rules as authored).</summary>
    public static void Clear()
    {
        Keys = None;
        Names = None;
        Day = 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => Clear();

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    private static void ResetInEditMode() =>
        UnityEditor.EditorApplication.playModeStateChanged += state =>
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
                Clear();
        };
#endif
}
