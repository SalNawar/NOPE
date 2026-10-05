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
/// ranges and the transponder models, phase 6; the waiver prefix and the
/// proofs of means, phase 8; the issuing offices and the fault canon, the
/// document design spec's D4 and D9, checked by CheckDocuments) is checked
/// (AgencyContent.Problems) and written into the content library, where the
/// desk calendar, the Records app, the Citizen Account app and case
/// generation read it.
/// </summary>
public static partial class WorldContentGenerator
{
    /// <summary>The agency block as authored ("agency"; phase 3 adds the displaced's day ranges, "displaced"; phase 25 the clerk's own account, "clerk"; phase 6 the accounts' ranges and the transponder models; phase 8 the proofs of means, "proofs"; phase 13b the stranding chance; phase 9 the employers).</summary>
    [Serializable] private sealed class AgencyData { public string name; public string programme; public string firstDate; public DisplacementRanges displaced; public ClerkData clerk; public AccountsData accounts; public TransponderData[] transponders; public ProofData[] proofs; public float strandChance; public FateData[] strandingFates; public StrandingReportContent strandingReport; public Employer[] employers; public PortalData[] portals; public AgencyOffice[] offices; public FaultEntry[] faults; }

    /// <summary>One stranding fate as authored ("agency.strandingFates"; the fate by name; the endings and strandings spec §6.2).</summary>
    [Serializable] private sealed class FateData { public string id; public string fate; public float weightWaivered; public float weightUnwaivered; public float stability; public string status; public StrandingFateLine[] lines; }

    /// <summary>The clerk's own account as authored ("agency.clerk").</summary>
    [Serializable] private sealed class ClerkData
    {
        public string citizenId; public string name; public string born; public string lineage; public string employment; public string note;
        public int startDebt; public float garnishShare; public string reliefEmployer; public string reliefWorksite; public int reliefWage;
    }

    /// <summary>The accounts' ranges as authored ("agency.accounts"; statuses by name; the waiver prefix, phase 8; the freeze window and the contract ranges, phase 9).</summary>
    [Serializable] private sealed class AccountsData { public int validDaysMin; public int validDaysMax; public int tripsWithinDays; public int frozenWithinDays; public string waiverPrefix; public StatusData[] statuses; public ContractRanges contract; }

    /// <summary>One proof of means as authored ("agency.proofs"; the category by name).</summary>
    [Serializable] private sealed class ProofData { public string form; public string category; public float weight; public int amountMin; public int amountMax; public string prefix; }

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
                waiverPrefix = a.accounts.waiverPrefix ?? string.Empty,
                frozenWithinDays = a.accounts.frozenWithinDays,
                statuses = (a.accounts.statuses ?? Array.Empty<StatusData>())
                    .Select(s => new StatusRanges
                    {
                        status = ParseEnum(s.status, out CitizenStatus status) ? status : default,
                        debtMin = s.debtMin,
                        debtMax = s.debtMax,
                        tripsMin = s.tripsMin,
                        tripsMax = s.tripsMax
                    })
                    .ToList(),
                contract = a.accounts.contract
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
                .ToList(),
            proofs = (a.proofs ?? Array.Empty<ProofData>())
                .Select(p => new ProofOfMeans
                {
                    form = p.form,
                    category = ParseEnum(p.category, out ClueCategory category) ? category : default,
                    weight = p.weight,
                    amountMin = p.amountMin,
                    amountMax = p.amountMax,
                    prefix = p.prefix ?? string.Empty
                })
                .ToList(),
            strandChance = a.strandChance,
            strandingFates = (a.strandingFates ?? Array.Empty<FateData>())
                .Select(f => f == null ? null : new StrandingFateRow
                {
                    id = f.id ?? string.Empty,
                    fate = ParseEnum(f.fate, out StrandingFate fate) ? fate : default,
                    weightWaivered = f.weightWaivered,
                    weightUnwaivered = f.weightUnwaivered,
                    stability = f.stability,
                    status = f.status ?? string.Empty,
                    lines = (f.lines ?? Array.Empty<StrandingFateLine>()).Where(l => l != null)
                        .Select(l => new StrandingFateLine { era = l.era ?? string.Empty, text = l.text ?? string.Empty }).ToList()
                })
                .ToList(),
            strandingReport = a.strandingReport ?? new StrandingReportContent(),
            employers = (a.employers ?? Array.Empty<Employer>()).Where(e => e != null).ToList(),
            portals = BuildPortals(a.portals),
            offices = (a.offices ?? Array.Empty<AgencyOffice>()).Where(o => o != null).ToList(),
            faults = (a.faults ?? Array.Empty<FaultEntry>()).Where(f => f != null).ToList()
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
        foreach (ProofData p in src.agency.proofs ?? Array.Empty<ProofData>())
            if (!ParseEnum(p.category, out ClueCategory _))
                errors.Add($"agency.proofs '{p.form}': '{p.category}' is not a category (Credit, Funds or PolicyNo).");

        // The strandings' fates and the failure report (the endings and strandings spec §6).
        foreach (FateData f in src.agency.strandingFates ?? Array.Empty<FateData>())
            if (f != null && !ParseEnum(f.fate, out StrandingFate _))
                errors.Add($"agency.strandingFates '{f.id}': '{f.fate}' is not a fate ({string.Join(", ", Enum.GetNames(typeof(StrandingFate)))}).");
        errors.AddRange(StrandingFates.Problems(agency.strandingFates, (src.eras ?? Array.Empty<EraData>()).Select(e => e.id).ToList()));
        errors.AddRange(StrandingFates.ReportProblems(agency.strandingReport));

        // The employers (phase 9): each of a known era, and every past main era with at least one, so a labourer bound anywhere has a contract;
        // a second moment (EraGroups) needs one only when a day weights it, since only then can a drawn labourer be bound there.
        var eraIds = new HashSet<string>((src.eras ?? Array.Empty<EraData>()).Select(e => e.id));
        foreach (Employer e in src.agency.employers ?? Array.Empty<Employer>())
            if (e != null && !string.IsNullOrWhiteSpace(e.era) && !eraIds.Contains(e.era))
                errors.Add($"agency.employers '{e.id}' hires for unknown era '{e.era}'.");
        var weighted = new HashSet<string>((src.days ?? Array.Empty<DayData>()).SelectMany(d => d.eras ?? Array.Empty<EraWeightData>()).Where(w => w != null && w.weight > 0f).Select(w => w.era));
        foreach (EraData era in (src.eras ?? Array.Empty<EraData>()).Where(e => !e.future && (string.IsNullOrWhiteSpace(e.group) || weighted.Contains(e.id))))
            if (agency.EmployersOf(era.id).Count == 0)
                errors.Add($"agency.employers has no employer for the era '{era.id}', so a labourer bound there would have no registered contract.");
    }

    /// <summary>The issuing offices and the published fault canon against the kinds' forms (the document design spec, D4, D9; DocumentContentChecks, the validator's rule).</summary>
    private static void CheckDocuments(WorldSource src, Authored authored, List<string> errors)
    {
        if (src.agency == null)
            return;
        var carried = authored.blueprints.SelectMany(b => (b.Value != null ? b.Value.DocumentTemplates ?? Array.Empty<DocumentTemplateSO>() : Array.Empty<DocumentTemplateSO>())
                                                            .Select(t => (b.Key, t)));
        errors.AddRange(DocumentContentChecks.Problems(BuildAgency(src.agency), carried));
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
