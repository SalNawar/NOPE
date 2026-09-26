using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Generate World's agency block (redesign phase 2; the traveller-types spec's
/// F6): world_source.json "agency" (the agency's printed name, its programme
/// line and day 1's date; the displaced's day ranges, phase 3; the clerk's
/// own account, "agency.clerk", phase 25: ClerkContent.Problems, which the
/// Citizen Account app shows, and from phase 13 the clerk's debt, its share
/// of pay and the clerk's own Debt Relief Labour Contract; the accounts'
/// ranges and the transponder models, phase 6) is checked
/// (AgencyContent.Problems) and written into the content library, where the
/// desk calendar, the Records app, the Citizen Account app and case
/// generation read it.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>The agency block as authored ("agency"; phase 3 adds the displaced's day ranges, "displaced"; phase 25 the clerk's own account, "clerk"; phase 6 the accounts' ranges and the transponder models).</summary>
    [Serializable] private sealed class AgencyData { public string name; public string programme; public string firstDate; public DisplacementRanges displaced; public ClerkData clerk; public AccountsData accounts; public TransponderData[] transponders; }

    /// <summary>The clerk's own account as authored ("agency.clerk").</summary>
    [Serializable] private sealed class ClerkData
    {
        public string citizenId; public string name; public string born; public string lineage; public string employment; public string note;
        public int startDebt; public float garnishShare; public string reliefEmployer; public string reliefWorksite; public int reliefWage;
    }

    /// <summary>The accounts' ranges as authored ("agency.accounts"; statuses by name).</summary>
    [Serializable] private sealed class AccountsData { public int validDaysMin; public int validDaysMax; public int tripsWithinDays; public StatusData[] statuses; }

    /// <summary>One status's ranges as authored ("agency.accounts.statuses"; the status by name).</summary>
    [Serializable] private sealed class StatusData { public string status; public int debtMin; public int debtMax; public int tripsMin; public int tripsMax; }

    /// <summary>One transponder model as authored ("agency.transponders"; the class by name).</summary>
    [Serializable] private sealed class TransponderData { public string id; public string transponderClass; public string model; public string prefix; public float weight; }

    /// <summary>The agency block's content (its fields verbatim; a status or class name that is no enum value reads as the first value, and CheckAgency reports it).</summary>
    private static AgencyContent BuildAgency(AgencyData a) =>
        new AgencyContent
        {
            name = a.name,
            programme = a.programme,
            firstDate = a.firstDate,
            displaced = a.displaced,
            clerk = BuildClerk(a.clerk),
            accounts = a.accounts == null ? null : new AccountRanges
            {
                validDaysMin = a.accounts.validDaysMin,
                validDaysMax = a.accounts.validDaysMax,
                tripsWithinDays = a.accounts.tripsWithinDays,
                statuses = (a.accounts.statuses ?? Array.Empty<StatusData>())
                    .Select(s => new StatusRanges
                    {
                        status = ParseEnum(s.status, out CitizenStatus status) ? status : default,
                        debtMin = s.debtMin,
                        debtMax = s.debtMax,
                        tripsMin = s.tripsMin,
                        tripsMax = s.tripsMax
                    })
                    .ToList()
            },
            transponders = (a.transponders ?? Array.Empty<TransponderData>())
                .Select(t => new TransponderModel
                {
                    id = t.id,
                    transponderClass = ParseEnum(t.transponderClass, out TransponderClass grade) ? grade : default,
                    model = t.model,
                    prefix = t.prefix,
                    weight = t.weight
                })
                .ToList()
        };

    /// <summary>The clerk's rows (verbatim; a missing block reads blank and fails ClerkContent.Problems).</summary>
    private static ClerkContent BuildClerk(ClerkData c) =>
        c == null
            ? new ClerkContent()
            : new ClerkContent
            {
                citizenId = c.citizenId, name = c.name, born = c.born, lineage = c.lineage, employment = c.employment, note = c.note,
                startDebt = c.startDebt, garnishShare = c.garnishShare, reliefEmployer = c.reliefEmployer, reliefWorksite = c.reliefWorksite, reliefWage = c.reliefWage
            };

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
        foreach (StatusData s in src.agency.accounts?.statuses ?? Array.Empty<StatusData>())
            if (!ParseEnum(s.status, out CitizenStatus _))
                errors.Add($"agency.accounts.statuses: '{s.status}' is not an account status ({string.Join(", ", Enum.GetNames(typeof(CitizenStatus)))}).");
        foreach (TransponderData t in src.agency.transponders ?? Array.Empty<TransponderData>())
            if (!ParseEnum(t.transponderClass, out TransponderClass _))
                errors.Add($"agency.transponders '{t.id}': '{t.transponderClass}' is not a transponder class ({string.Join(", ", Enum.GetNames(typeof(TransponderClass)))}).");
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
