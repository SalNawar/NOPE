using System.Collections.Generic;

/// <summary>
/// One agency form as the interview's requests see them (a DocumentTemplateSO
/// of the day plans' blueprints): its number, its name, its request group and
/// whether the traveller hands it over on request (traveller types I2). Any
/// traveller may be asked for any form of the papers menu (the personalities
/// spec's W4: FormRequests.MetSoFar).
/// </summary>
public sealed class AskableForm
{
    /// <summary>The agency form number ("TC-310").</summary>
    public string FormNumber;

    /// <summary>The form's name ("Stranding Waiver"): a request entry's label when the form is in no group.</summary>
    public string Name;

    /// <summary>The request group (DocumentTemplateSO.askGroup), or blank.</summary>
    public string AskGroup;

    /// <summary>True when the traveller hands it over on request (a form handed over on arrival is never asked for).</summary>
    public bool Requested;

    /// <summary>Creates a form.</summary>
    public AskableForm(string formNumber, string name, string askGroup, bool requested)
    {
        FormNumber = formNumber;
        Name = name;
        AskGroup = askGroup;
        Requested = requested;
    }
}

/// <summary>
/// One entry of the papers menu (traveller types I2): a form the desk may
/// ask any traveller of the day for, or a request group (the proofs of means
/// as one "Proof of means"), with the paper this traveller hands over, or
/// none when they do not carry it (they answer with their missing-form line).
/// </summary>
public sealed class FormRequest
{
    /// <summary>The request's id: the group's id, or the form number outside a group (FormRequests.IdOf); a missing-form reply names it.</summary>
    public string Id;

    /// <summary>The entry's label: the group's label, or the form's name.</summary>
    public string Label;

    /// <summary>The carried paper's index in the case, or -1 when the traveller carries no form of the request.</summary>
    public int Document = -1;

    /// <summary>True when the traveller carries a form of the request (a request hands it over).</summary>
    public bool Carried => Document >= 0;
}

/// <summary>The forms of one traveller kind in play on a day as the missing-form rule sees them: the day's papers menu, and what the kind's blueprint carries.</summary>
public sealed class KindForms
{
    /// <summary>The kind.</summary>
    public TravellerKind Kind;

    /// <summary>The day's papers menu, which the desk may ask every traveller of the day for (FormRequests.MetSoFar).</summary>
    public IReadOnlyList<AskableForm> Askable;

    /// <summary>The kind's blueprint's forms (every form the blueprint lists; of a group the traveller carries one).</summary>
    public IReadOnlyList<AskableForm> Carried;
}

/// <summary>
/// The papers menu's rules (traveller types I2; the personalities spec's
/// W4-W5): one menu for every traveller of a day, every on-request form of
/// the days so far (MetSoFar), one entry per form or request group, which
/// paper each entry hands over, and the content rules Generate World and the
/// validator share (a group needs its label; every request the menu offers a
/// kind in play that never carries it needs the kind's honest reply). Pure,
/// so every rule is tested headless.
/// </summary>
public static class FormRequests
{
    /// <summary>A request's id: the group's id when the form is in a group, else the form number.</summary>
    public static string IdOf(string askGroup, string formNumber) => string.IsNullOrEmpty(askGroup) ? formNumber : askGroup;

    /// <summary>The label of a group (its authored label, or its id when none is authored), or null when <paramref name="askGroup"/> is blank.</summary>
    public static string GroupLabel(string askGroup, IReadOnlyList<AskGroupLabel> groups)
    {
        if (string.IsNullOrEmpty(askGroup))
            return null;
        foreach (AskGroupLabel g in groups ?? System.Array.Empty<AskGroupLabel>())
            if (g != null && g.id == askGroup)
                return string.IsNullOrWhiteSpace(g.label) ? askGroup : g.label;
        return askGroup;
    }

    /// <summary>The name the desk asks for a form by: its group's label when it is in a group, else its own name (the PC's chip names a paper not handed over the same way).</summary>
    public static string RequestLabel(string askGroup, string name, IReadOnlyList<AskGroupLabel> groups) => GroupLabel(askGroup, groups) ?? name;

    /// <summary>
    /// The papers menu of day <paramref name="today"/> (the personalities
    /// spec's W4): every form handed over on request among the forms of days
    /// 1 to <paramref name="today"/> (<paramref name="days"/>[i] is day i + 1's:
    /// its plan's blueprints' templates; a day past the list keeps them all),
    /// in first-appearance order, a request group once (at its first form). It
    /// grows as new kinds arrive, never lists a form before its day and never
    /// takes an entry away on a later day with a smaller mix. Every traveller
    /// of the day is offered it. Empty for null or a day before the first.
    /// </summary>
    public static List<AskableForm> MetSoFar(IReadOnlyList<IReadOnlyList<AskableForm>> days, int today)
    {
        var menu = new List<AskableForm>();
        var ids = new HashSet<string>();
        for (int d = 0; days != null && d < days.Count && d < today; d++)
            foreach (AskableForm f in days[d] ?? System.Array.Empty<AskableForm>())
                if (f != null && f.Requested && ids.Add(IdOf(f.AskGroup, f.FormNumber) ?? string.Empty))
                    menu.Add(f);
        return menu;
    }

    /// <summary>How many papers a traveller with these forms carries (<paramref name="askGroups"/>: each form's group, blank outside one): every form outside a group, and one per group (AccountMaker.Carries: the account's one form of the group). The desk's paper spawn slots are checked against it.</summary>
    public static int CarriedCount(IReadOnlyList<string> askGroups)
    {
        int count = 0;
        var groups = new HashSet<string>();
        foreach (string g in askGroups ?? System.Array.Empty<string>())
            if (string.IsNullOrEmpty(g))
                count++;
            else
                groups.Add(g);
        return count + groups.Count;
    }

    /// <summary>How many request entries <paramref name="askable"/> makes: one per form outside a group, one per group (the hub and the papers menu count them).</summary>
    public static int Count(IReadOnlyList<AskableForm> askable)
    {
        var ids = new HashSet<string>();
        foreach (AskableForm f in askable ?? System.Array.Empty<AskableForm>())
            if (f != null)
                ids.Add(IdOf(f.AskGroup, f.FormNumber) ?? string.Empty);
        return ids.Count;
    }

    /// <summary>
    /// A traveller's request entries: one per form of <paramref name="askable"/>
    /// (the day's papers menu, MetSoFar), a group once at its first
    /// form, in that order, labelled by the group's label or the form's name,
    /// each handing over the first paper of <paramref name="documents"/> that
    /// is handed over on request and is of the form (or of the group), or
    /// none; then, so every paper can be asked for, one entry per remaining
    /// paper handed over on request (named by its form, keyed by its form
    /// number, or by its index when it has none). Null lists count as empty.
    /// </summary>
    public static List<FormRequest> Build(IReadOnlyList<AskableForm> askable, IReadOnlyList<CaseDocument> documents, IReadOnlyList<AskGroupLabel> groups)
    {
        var requests = new List<FormRequest>();
        var ids = new HashSet<string>();
        int count = documents != null ? documents.Count : 0;
        var matched = new bool[count];

        foreach (AskableForm f in askable ?? System.Array.Empty<AskableForm>())
        {
            if (f == null)
                continue;
            string id = IdOf(f.AskGroup, f.FormNumber) ?? string.Empty;
            if (!ids.Add(id))
                continue;

            int document = -1;
            for (int i = 0; i < count && document < 0; i++)
            {
                CaseDocument d = documents[i];
                if (d == null || !d.Requested || matched[i])
                    continue;
                bool same = string.IsNullOrEmpty(f.AskGroup) ? d.formNumber == f.FormNumber : d.askGroup == f.AskGroup;
                if (same)
                    document = i;
            }
            if (document >= 0)
                matched[document] = true;
            requests.Add(new FormRequest { Id = id, Label = RequestLabel(f.AskGroup, f.Name, groups), Document = document });
        }

        for (int i = 0; i < count; i++)
        {
            CaseDocument d = documents[i];
            if (d == null || !d.Requested || matched[i])
                continue;
            string id = IdOf(d.askGroup, d.formNumber);
            if (string.IsNullOrEmpty(id))
                id = "doc:" + i;
            if (!ids.Add(id))
                continue;
            requests.Add(new FormRequest { Id = id, Label = d.name, Document = i });
        }

        return requests;
    }

    /// <summary>
    /// Every problem of the request groups, the one rule Generate World and the
    /// validator share: a label row with a blank id or a blank label, an id
    /// listed twice, a label for a group no form is in; a form in a group
    /// handed over on arrival; a group that is not the proof group (the only
    /// group whose carried form the account decides, AccountMaker.ProofGroup).
    /// Empty when sound.
    /// </summary>
    public static List<string> GroupProblems(IReadOnlyList<AskableForm> forms, IReadOnlyList<AskGroupLabel> groups)
    {
        var problems = new List<string>();
        var labelled = new HashSet<string>();
        foreach (AskGroupLabel g in groups ?? System.Array.Empty<AskGroupLabel>())
        {
            if (g == null)
                continue;
            if (string.IsNullOrWhiteSpace(g.id))
                problems.Add("interview.askGroups: a group has a blank id.");
            else if (!labelled.Add(g.id))
                problems.Add($"interview.askGroups: the group '{g.id}' is listed twice.");
            if (string.IsNullOrWhiteSpace(g.label))
                problems.Add($"interview.askGroups: the group '{g.id}' has a blank label (the papers menu's entry).");
        }

        var used = new HashSet<string>();
        foreach (AskableForm f in forms ?? System.Array.Empty<AskableForm>())
        {
            if (f == null || string.IsNullOrEmpty(f.AskGroup))
                continue;
            if (f.AskGroup != AccountMaker.ProofGroup)
                problems.Add($"Form '{f.FormNumber}' is in the request group '{f.AskGroup}'; the only group is '{AccountMaker.ProofGroup}' (the proofs of means, of which the account decides the carried one).");
            if (!labelled.Contains(f.AskGroup))
                problems.Add($"Form '{f.FormNumber}' is in the request group '{f.AskGroup}', which interview.askGroups does not label.");
            if (!f.Requested)
                problems.Add($"Form '{f.FormNumber}' is in the request group '{f.AskGroup}' but is handed over on arrival; a grouped form is asked for.");
            used.Add(f.AskGroup);
        }

        foreach (string id in labelled)
            if (!used.Contains(id))
                problems.Add($"interview.askGroups labels the group '{id}', but no form is in it.");

        return problems;
    }

    /// <summary>
    /// Every problem of the missing-form replies, the one rule Generate World
    /// and the validator share: a reply with a blank request or a blank line,
    /// a (kind, request, variant) listed twice, a request that names no form
    /// number and no group of <paramref name="forms"/>; and, for each entry of
    /// <paramref name="kinds"/> (a kind in play on a day, with that day's
    /// papers menu, MetSoFar), each request of the menu that the kind's
    /// blueprint carries no form of, without the kind's Honest reply (the
    /// traveller never needed it; the personalities spec's W5), each
    /// (kind, request) reported once. Empty when sound.
    /// </summary>
    public static List<string> ReplyProblems(IReadOnlyList<MissingFormReply> replies, IReadOnlyList<AskableForm> forms, IReadOnlyList<KindForms> kinds)
    {
        var problems = new List<string>();
        var known = new HashSet<string>();
        foreach (AskableForm f in forms ?? System.Array.Empty<AskableForm>())
            if (f != null)
                known.Add(IdOf(f.AskGroup, f.FormNumber) ?? string.Empty);

        var listed = new HashSet<(TravellerKind, string, MissingFormVariant)>();
        foreach (MissingFormReply r in replies ?? System.Array.Empty<MissingFormReply>())
        {
            if (r == null)
                continue;
            if (string.IsNullOrWhiteSpace(r.request))
                problems.Add($"interview.missingFormReplies: a {r.kind} reply names no request (a form number or a group id).");
            else if (!known.Contains(r.request))
                problems.Add($"interview.missingFormReplies: the {r.kind} reply names the request '{r.request}', which is no form number and no request group.");
            if (!listed.Add((r.kind, r.request ?? string.Empty, r.variant)))
                problems.Add($"interview.missingFormReplies: {r.kind} / '{r.request}' / {r.variant} is listed twice.");
            if (r.line == null || string.IsNullOrWhiteSpace(r.line.text))
                problems.Add($"interview.missingFormReplies: the {r.kind} / '{r.request}' / {r.variant} line is blank.");
        }

        var reported = new HashSet<(TravellerKind, string)>();
        foreach (KindForms k in kinds ?? System.Array.Empty<KindForms>())
        {
            if (k == null)
                continue;
            var carried = new HashSet<string>();
            foreach (AskableForm f in k.Carried ?? System.Array.Empty<AskableForm>())
                if (f != null)
                    carried.Add(IdOf(f.AskGroup, f.FormNumber) ?? string.Empty);
            foreach (AskableForm f in k.Askable ?? System.Array.Empty<AskableForm>())
            {
                if (f == null)
                    continue;
                string id = IdOf(f.AskGroup, f.FormNumber) ?? string.Empty;
                if (carried.Contains(id) || !reported.Add((k.Kind, id)))
                    continue;
                if (!listed.Contains((k.Kind, id, MissingFormVariant.Honest)))
                    problems.Add($"interview.missingFormReplies: a {k.Kind} traveller may be asked for '{id}' but carries none; add their {MissingFormVariant.Honest} reply.");
            }
        }

        return problems;
    }
}
