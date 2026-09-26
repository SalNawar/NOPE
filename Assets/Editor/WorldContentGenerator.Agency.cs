using System;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Generate World's agency block (redesign phase 2; the traveller-types spec's
/// F6): world_source.json "agency" (the agency's printed name, its programme
/// line and day 1's date) is checked (AgencyContent.Problems) and written into
/// the content library, where the desk calendar and the Records app read it.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>The agency block as authored ("agency"; phase 3 adds the displaced's day ranges, "displaced").</summary>
    [Serializable] private sealed class AgencyData { public string name; public string programme; public string firstDate; public DisplacementRanges displaced; }

    /// <summary>The agency block's content (its fields verbatim).</summary>
    private static AgencyContent BuildAgency(AgencyData a) =>
        new AgencyContent { name = a.name, programme = a.programme, firstDate = a.firstDate, displaced = a.displaced };

    /// <summary>A missing "agency" section, or its problems (AgencyContent.Problems, the validator's rule).</summary>
    private static void CheckAgency(WorldSource src, List<string> errors)
    {
        if (src.agency == null)
        {
            errors.Add($"'{SourcePath}' has no \"agency\" section (name, programme, firstDate).");
            return;
        }
        errors.AddRange(BuildAgency(src.agency).Problems());
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
