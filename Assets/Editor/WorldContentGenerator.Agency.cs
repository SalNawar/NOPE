using System;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Generate World's agency block (redesign phase 2; the traveller-types spec's
/// F6): world_source.json "agency" (the agency's printed name, its programme
/// line and day 1's date) is checked (AgencyContent.Problems) and written into
/// the content library, where the desk calendar and the Records app read it;
/// with it the clerk's own account ("agency.clerk", redesign phase 25:
/// ClerkContent.Problems), which the Citizen Account app shows.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>The agency block as authored ("agency"; phase 3 adds the displaced's day ranges, "displaced"; phase 25 the clerk's own account, "clerk").</summary>
    [Serializable] private sealed class AgencyData { public string name; public string programme; public string firstDate; public DisplacementRanges displaced; public ClerkData clerk; }

    /// <summary>The clerk's own account as authored ("agency.clerk").</summary>
    [Serializable] private sealed class ClerkData { public string citizenId; public string name; public string born; public string lineage; public string employment; public string note; }

    /// <summary>The agency block's content (its fields verbatim).</summary>
    private static AgencyContent BuildAgency(AgencyData a) =>
        new AgencyContent { name = a.name, programme = a.programme, firstDate = a.firstDate, displaced = a.displaced, clerk = BuildClerk(a.clerk) };

    /// <summary>The clerk's rows (verbatim; a missing block reads blank and fails ClerkContent.Problems).</summary>
    private static ClerkContent BuildClerk(ClerkData c) =>
        c == null
            ? new ClerkContent()
            : new ClerkContent { citizenId = c.citizenId, name = c.name, born = c.born, lineage = c.lineage, employment = c.employment, note = c.note };

    /// <summary>A missing "agency" section, or its problems (AgencyContent.Problems, the validator's rule).</summary>
    private static void CheckAgency(WorldSource src, List<string> errors)
    {
        if (src.agency == null)
        {
            errors.Add($"'{SourcePath}' has no \"agency\" section (name, programme, firstDate).");
            return;
        }
        AgencyContent agency = BuildAgency(src.agency);
        errors.AddRange(agency.Problems());
        errors.AddRange(agency.clerk.Problems());
    }

    /// <summary>Writes the agency block into the content library.</summary>
    private static void WireAgency(ContentLibrarySO lib, AgencyData agency)
    {
        var so = new SerializedObject(lib);
        so.FindProperty("agency").boxedValue = BuildAgency(agency);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(lib);
    }
}
