using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The validator's portals check (the portals spec v3 PO3, RT2, RT4): the
/// library's portals hold what Generate World requires of them
/// (PortalSchedule.PortalProblems, each repair an upgrade of the library), and
/// each day plan's Directorate routes too (PortalSchedule.DayProblems over the
/// plan's own places and closures), every route naming a place.
/// </summary>
public static partial class ContentLibraryValidator
{
    /// <summary>Reports each problem of the portals and the day plans' routes; returns how many.</summary>
    private static int CheckPortals(ContentLibrarySO lib)
    {
        var upgrades = new HashSet<string>(lib.Upgrades.Where(u => u != null).Select(u => u.id));
        List<PortalSpec> portals = lib.Agency.portals ?? new List<PortalSpec>();
        var problems = new List<string>(PortalSchedule.PortalProblems(portals, upgrades.Contains));
        foreach (DayPlanSO plan in lib.DayPlans.Where(p => p != null))
        {
            if (plan.DirectorateRoutes.Any(r => r == null || r.place == null))
                problems.Add($"{plan.name}: a Directorate route names no place.");
            var world = new HashSet<PlaceRef>(lib.TodaysProfiles(plan).Select(p => new PlaceRef(p.nation.id, p.era.id)));
            problems.AddRange(PortalSchedule.DayProblems(plan.name, portals, plan.PortalRequests(), world.Contains,
                                                         place => plan.ClosureOf(lib.GetNationById(place.NationId), lib.GetEraById(place.EraId)) != null));
        }

        foreach (string problem in problems)
            Debug.LogError($"[ContentLibraryValidator] Portals: {problem.TrimEnd('.')} in '{lib.name}' (Tools > TimeDesk > Generate World writes world_source.json \"agency.portals\" and \"days[].portals\").", lib);
        return problems.Count;
    }
}
