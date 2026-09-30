using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// One row of the published fault canon (the document design spec, D9;
/// world_source.json agency.faults; docs/DOCUMENT_FAULTS.md): a fault one
/// document can carry, what proves it, and which lie or directive puts it
/// there. Rows of one lie and one variant are forged together, in row order
/// (a record lie's variant, a seal's forgery, the photo). Content, written
/// into the content library by Generate World; the case factory draws
/// only from it (FaultCanon).
/// </summary>
[Serializable]
public sealed class FaultEntry
{
    /// <summary>The row's id ("visaClass").</summary>
    public string id = string.Empty;

    /// <summary>The form it is on ("TC-101"), or Any for every paper the traveller carries (a forged seal).</summary>
    public string form = string.Empty;

    /// <summary>The field's category (a ClueCategory name), or blank for a whole paper (a missing form).</summary>
    public string field = string.Empty;

    /// <summary>The lie that prints it (a LieKind name), or blank for a directive fault.</summary>
    public string lie = string.Empty;

    /// <summary>The directive fault it breaks (a DirectiveFault name), or blank for a lie.</summary>
    public string directive = string.Empty;

    /// <summary>The variant it belongs to: a lie's rows of one variant are forged together (RecordLies' draw picks among the variants that can show); a directive's names its maker's case ("unsigned").</summary>
    public string variant = string.Empty;

    /// <summary>True when the variant shows without it: forged only when its form is carried and prints it.</summary>
    public bool optional;

    /// <summary>What proves it, in the player's words ("Citizen Account · Status", "the Seal Register", "the traveller at the desk").</summary>
    public string against = string.Empty;

    /// <summary>One line on what the forger did, for the docs and the narrative workbook.</summary>
    public string note = string.Empty;

    /// <summary>The field's category; false for a blank or unknown name.</summary>
    public bool TryField(out ClueCategory category) =>
        Enum.TryParse(field, false, out category) && Enum.IsDefined(typeof(ClueCategory), category);

    /// <summary>The lie; false for a blank or unknown name.</summary>
    public bool TryLie(out LieKind kind) =>
        Enum.TryParse(lie, false, out kind) && Enum.IsDefined(typeof(LieKind), kind);

    /// <summary>The directive fault; false for a blank or unknown name.</summary>
    public bool TryDirective(out DirectiveFault fault) =>
        Enum.TryParse(directive, false, out fault) && Enum.IsDefined(typeof(DirectiveFault), fault) && fault != DirectiveFault.None;

    /// <summary>True when the row applies to form <paramref name="formNumber"/> (its own form, or Any).</summary>
    public bool OnForm(string formNumber) => form == FaultCanon.Any || form == formNumber;
}

/// <summary>One variant of a lie in the canon: its name and its rows, in row order.</summary>
public sealed class FaultVariant
{
    /// <summary>A variant from its name and rows.</summary>
    public FaultVariant(string name, IReadOnlyList<FaultEntry> rows)
    {
        Name = name ?? string.Empty;
        Rows = rows ?? Array.Empty<FaultEntry>();
    }

    /// <summary>The variant's name ("richForged").</summary>
    public string Name { get; }

    /// <summary>Its rows, in canon order.</summary>
    public IReadOnlyList<FaultEntry> Rows { get; }
}

/// <summary>
/// The published fault canon (the document design spec, D9): a small table of
/// the faults each document can carry. The makers draw only from it: a record
/// lie's variants (RecordLies), a forged seal's forgeries and papers, the
/// photo's forms, the forms a place lie may print a tell on and the dates a
/// directive may falsify (CaseFactory). Problems is the validator's rule.
/// Pure, so the table's reading is tested headless.
/// </summary>
public static class FaultCanon
{
    /// <summary>A row's form for every paper the traveller carries (a forged seal can be on any).</summary>
    public const string Any = "*";

    /// <summary>The variants of <paramref name="lie"/>, in the order their first rows appear; empty without rows.</summary>
    public static List<FaultVariant> Variants(IReadOnlyList<FaultEntry> canon, LieKind lie)
    {
        var order = new List<string>();
        var rows = new Dictionary<string, List<FaultEntry>>();
        foreach (FaultEntry e in canon ?? Array.Empty<FaultEntry>())
        {
            if (e == null || !e.TryLie(out LieKind k) || k != lie)
                continue;
            string name = e.variant ?? string.Empty;
            if (!rows.TryGetValue(name, out List<FaultEntry> list))
            {
                rows[name] = list = new List<FaultEntry>();
                order.Add(name);
            }
            list.Add(e);
        }
        return order.Select(n => new FaultVariant(n, rows[n])).ToList();
    }

    /// <summary>True when the canon lets <paramref name="fault"/> (a lie's or a directive's rows) print on <paramref name="category"/> of form <paramref name="formNumber"/>.</summary>
    public static bool Allows(IReadOnlyList<FaultEntry> canon, Func<FaultEntry, bool> fault, string formNumber, ClueCategory category)
    {
        foreach (FaultEntry e in canon ?? Array.Empty<FaultEntry>())
            if (e != null && fault(e) && e.OnForm(formNumber) && e.TryField(out ClueCategory c) && c == category)
                return true;
        return false;
    }

    /// <summary>A row filter for the rows of <paramref name="lie"/>.</summary>
    public static Func<FaultEntry, bool> Of(LieKind lie) => e => e.TryLie(out LieKind k) && k == lie;

    /// <summary>A row filter for the rows of <paramref name="directive"/>.</summary>
    public static Func<FaultEntry, bool> Of(DirectiveFault directive) => e => e.TryDirective(out DirectiveFault d) && d == directive;

    /// <summary>
    /// What Generate World and the validator refuse: a blank or repeated id;
    /// a row naming neither or both of a lie and a directive, or a name that
    /// is no value; a form the agency does not issue (<paramref name="forms"/>:
    /// each form number's field categories; Any only for a forged seal); a
    /// field its form does not print (blank only for a directive's whole
    /// paper); a lie row without a variant; blank proof words; a variant of a
    /// record lie whose rows are all optional (it could show with nothing
    /// forged). Empty when sound.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<FaultEntry> canon, IReadOnlyDictionary<string, IReadOnlyList<ClueCategory>> forms)
    {
        var problems = new List<string>();
        var ids = new HashSet<string>();
        forms = forms ?? new Dictionary<string, IReadOnlyList<ClueCategory>>();
        foreach (FaultEntry e in canon ?? Array.Empty<FaultEntry>())
        {
            if (e == null)
                continue;
            string who = $"agency.faults '{e.id}'";
            if (string.IsNullOrWhiteSpace(e.id) || !ids.Add(e.id))
                problems.Add($"{who}: the id is blank or listed twice.");
            bool isLie = !string.IsNullOrWhiteSpace(e.lie), isDirective = !string.IsNullOrWhiteSpace(e.directive);
            if (isLie == isDirective)
                problems.Add($"{who}: name a lie or a directive fault, not {(isLie ? "both" : "neither")}.");
            else if (isLie && !e.TryLie(out _))
                problems.Add($"{who}: '{e.lie}' is not a lie ({string.Join(", ", Enum.GetNames(typeof(LieKind)))}).");
            else if (isDirective && !e.TryDirective(out _))
                problems.Add($"{who}: '{e.directive}' is not a directive fault.");
            if (isLie && string.IsNullOrWhiteSpace(e.variant))
                problems.Add($"{who}: a lie's row names its variant.");
            if (string.IsNullOrWhiteSpace(e.against))
                problems.Add($"{who}: 'against' is blank: what proves the fault, in the player's words.");

            bool seal = e.TryLie(out LieKind lie) && lie == LieKind.ForgedSeal;
            if (e.form == Any)
            {
                if (!seal)
                    problems.Add($"{who}: only a forged seal's row may name every paper ('{Any}').");
            }
            else if (!forms.TryGetValue(e.form ?? string.Empty, out IReadOnlyList<ClueCategory> printed))
            {
                problems.Add($"{who}: '{e.form}' is not a form the agency hands out.");
            }
            else if (string.IsNullOrWhiteSpace(e.field))
            {
                if (isLie)
                    problems.Add($"{who}: a lie's row names the field it forges.");
            }
            else if (!e.TryField(out ClueCategory c))
                problems.Add($"{who}: '{e.field}' is not a category.");
            else if (!printed.Contains(c))
                problems.Add($"{who}: {e.form} prints no {e.field}.");
        }

        foreach (LieKind lie in Enum.GetValues(typeof(LieKind)))
            foreach (FaultVariant v in Variants(canon, lie))
                if (LieKinds.IsRecordLie(lie) && v.Rows.All(r => r.optional))
                    problems.Add($"agency.faults: {lie}'s variant '{v.Name}' has only optional rows; it could show with nothing forged.");
        return problems;
    }
}
