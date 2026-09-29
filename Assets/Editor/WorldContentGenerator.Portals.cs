using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

/// <summary>
/// Generate World's portals (the portals spec v3 PO1-PO3, RT2, RT4, CN1-CN2):
/// world_source.json "agency.portals" (the hall's five rings: number, the
/// ring's name, role, first day and repair) is checked
/// (PortalSchedule.PortalProblems: a repair must be an upgrade of the content
/// library) and written into the library's agency block; each day's
/// "portals" (the Directorate's route per departure portal) is checked
/// (PortalSchedule.DayProblems over the day's world and its closures) and
/// written into its plan's directorateRoutes, each place by reference.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>One portal as authored ("agency.portals"; the role by name).</summary>
    [Serializable] private sealed class PortalData { public int number; public string name; public string role; public int fromDay; public string repair; }

    /// <summary>One day's route for a portal as authored ("days[].portals").</summary>
    [Serializable] private sealed class DayPortalData { public int portal; public string country; public string era; }

    /// <summary>The portals' specs (verbatim; a role that is no PortalRole reads as Departures, and CheckPortals reports it).</summary>
    private static List<PortalSpec> BuildPortals(PortalData[] portals) =>
        (portals ?? Array.Empty<PortalData>())
            .Where(p => p != null)
            .Select(p => new PortalSpec
            {
                number = p.number,
                name = p.name ?? string.Empty,
                role = ParseEnum(p.role, out PortalRole role) ? role : default,
                fromDay = p.fromDay,
                repair = p.repair ?? string.Empty
            })
            .ToList();

    /// <summary>A day's routes as the schedule's requests (a blank country or era makes a place no world holds, which DayProblems reports).</summary>
    private static List<PortalRequest> Routes(DayData d) =>
        (d.portals ?? Array.Empty<DayPortalData>()).Where(r => r != null).Select(r => new PortalRequest(r.portal, new PlaceRef(r.country, r.era))).ToList();

    /// <summary>The portals' problems (PortalSchedule.PortalProblems, a role by name) and each day's routes' (PortalSchedule.DayProblems: in the day's world, never the Future; closed by a closure every traveller is read for).</summary>
    private static void CheckPortals(WorldSource src, Authored authored, List<string> errors)
    {
        if (src.agency == null)
            return;
        foreach (PortalData p in src.agency.portals ?? Array.Empty<PortalData>())
            if (p != null && !ParseEnum(p.role, out PortalRole _))
                errors.Add($"agency.portals {p.number}: '{p.role}' is not a portal role ({string.Join(", ", Enum.GetNames(typeof(PortalRole)))}).");

        var upgrades = new HashSet<string>(authored.library != null ? authored.library.Upgrades.Where(u => u != null).Select(u => u.id) : Enumerable.Empty<string>());
        List<PortalSpec> portals = BuildPortals(src.agency.portals);
        errors.AddRange(PortalSchedule.PortalProblems(portals, upgrades.Contains));

        var future = new HashSet<string>((src.eras ?? Array.Empty<EraData>()).Where(e => e.future).Select(e => e.id));
        var places = new HashSet<PlaceRef>(src.places.Select(p => new PlaceRef(p.country, p.era)));
        var rules = (src.rules ?? Array.Empty<RuleData>()).ToDictionary(r => r.asset);
        foreach (DayData d in src.days ?? Array.Empty<DayData>())
        {
            var eras = new HashSet<string>((d.eras ?? Array.Empty<EraWeightData>()).Where(w => w.weight > 0f).Select(w => w.era));
            var countries = new HashSet<string>(d.countries ?? Array.Empty<string>());
            List<RuleData> closures = (d.rules ?? Array.Empty<string>())
                .Where(rules.ContainsKey).Select(r => rules[r])
                .Where(r => ParseEnum(r.type, out TravelRuleType type) && Directives.IsClosure(type) && (r.kinds == null || r.kinds.Length == 0))
                .ToList();
            bool InWorld(PlaceRef p) =>
                places.Contains(p) && !future.Contains(p.EraId) &&
                ((d.eras ?? Array.Empty<EraWeightData>()).Length == 0 || eras.Contains(p.EraId)) &&
                (countries.Count == 0 || countries.Contains(p.NationId));
            bool Closed(PlaceRef p) =>
                closures.Any(r => ParseEnum(r.type, out TravelRuleType type) && Directives.Closes(type, r.country, r.era, p.NationId, p.EraId));
            errors.AddRange(PortalSchedule.DayProblems($"days[{d.day}]", portals, Routes(d), InWorld, Closed));
        }
    }

    /// <summary>Writes a day's routes (days[].portals) into its plan's directorateRoutes, each place by reference (CheckPortals ran first).</summary>
    private static void WriteRoutes(SerializedObject so, DayData d, Dictionary<string, NationEraProfileSO> places)
    {
        DayPortalData[] routes = (d.portals ?? Array.Empty<DayPortalData>()).Where(r => r != null).ToArray();
        SerializedProperty list = so.FindProperty("directorateRoutes");
        list.arraySize = routes.Length;
        for (int i = 0; i < routes.Length; i++)
        {
            SerializedProperty el = list.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("portal").intValue = routes[i].portal;
            el.FindPropertyRelative("place").objectReferenceValue = places.TryGetValue($"{routes[i].country}_{routes[i].era}", out NationEraProfileSO place) ? place : (Object)null;
        }
    }
}
