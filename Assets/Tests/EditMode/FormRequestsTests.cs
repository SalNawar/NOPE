using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The papers menu's rules (traveller types I2, phase 8; the personalities
/// spec's W4-W5): one menu for every traveller, every on-request form of the
/// days so far, one entry per form or request group, which paper each entry
/// hands over, how many papers a blueprint's traveller carries, and the
/// content rules for groups and missing-form replies.
/// </summary>
public class FormRequestsTests
{
    private static AskableForm Form(string number, string name, string group, bool requested) => new AskableForm(number, name, group, requested);

    /// <summary>The nine forms' relevant part: the visa on arrival; the manifest, the waiver and the three proofs (the proof group) on request; the displaced's certificate on arrival and their two on request.</summary>
    private static List<AskableForm> Forms() => new List<AskableForm>
    {
        Form("TC-101", "Leisure Departure Visa", "", false),
        Form("TC-230", "Departure Manifest", "", true),
        Form("TC-310", "Stranding Waiver", "", true),
        Form("TC-415", "Holiday Credit Agreement", AccountMaker.ProofGroup, true),
        Form("TC-416", "Proof of Funds", AccountMaker.ProofGroup, true),
        Form("TC-417", "Travel Insurance Certificate", AccountMaker.ProofGroup, true),
        Form("TC-610", "Displacement Certificate", "", false),
        Form("TC-620", "Intake Declaration", "", true),
        Form("TC-630", "Return Order", "", true)
    };

    /// <summary>The tourists' forms (the rich tourist's and the poor tourist's blueprints): the first six.</summary>
    private static List<AskableForm> Tourists() => Forms().Take(6).ToList();

    /// <summary>The labourer's blueprint: the contract on arrival, the manifest and the waiver.</summary>
    private static List<AskableForm> Labourers() => new List<AskableForm> { Form("TC-520", "Labour Contract", "", false), Forms()[1], Forms()[2] };

    /// <summary>The displaced's blueprint: the certificate on arrival, the declaration and the return order.</summary>
    private static List<AskableForm> Displaced() => Forms().Skip(6).ToList();

    /// <summary>
    /// Each day's forms, as the day plans' blueprints list them (the
    /// traveller-types ramp): days 1-2 the tourists', days 3-4 with the
    /// labourers', day 5 the displaced's listed first, day 6 everyone's.
    /// </summary>
    private static List<IReadOnlyList<AskableForm>> Days() => new List<IReadOnlyList<AskableForm>>
    {
        Tourists(), Tourists(), Tourists().Concat(Labourers()).ToList(), Tourists().Concat(Labourers()).ToList(),
        Displaced().Concat(Tourists()).Concat(Labourers()).ToList(), Tourists().Concat(Labourers()).Concat(Displaced()).ToList()
    };

    private static string[] Ids(IEnumerable<AskableForm> forms) => forms.Select(f => FormRequests.IdOf(f.AskGroup, f.FormNumber)).ToArray();

    private static List<AskGroupLabel> Groups() => new List<AskGroupLabel> { new AskGroupLabel { id = AccountMaker.ProofGroup, label = "Proof of means" } };

    private static CaseDocument Doc(string number, string name, string group = "", DocumentHandOver handOver = DocumentHandOver.OnRequest) =>
        new CaseDocument { name = name, formNumber = number, askGroup = group, handOver = handOver };

    /// <summary>A poor tourist's four papers: the visa on arrival, then the manifest, the waiver and the Proof of Funds they hold.</summary>
    private static List<CaseDocument> PoorPapers() => new List<CaseDocument>
    {
        Doc("TC-101", "Leisure Departure Visa", handOver: DocumentHandOver.OnArrival), Doc("TC-230", "Departure Manifest"), Doc("TC-310", "Stranding Waiver"), Doc("TC-416", "Proof of Funds", AccountMaker.ProofGroup)
    };

    /// <summary>A rich tourist's two papers.</summary>
    private static List<CaseDocument> RichPapers() => new List<CaseDocument>
    {
        Doc("TC-101", "Leisure Departure Visa", handOver: DocumentHandOver.OnArrival), Doc("TC-230", "Departure Manifest")
    };

    [Test]
    public void IdOf_TheGroup_ElseTheFormNumber()
    {
        Assert.AreEqual("proof", FormRequests.IdOf("proof", "TC-416"));
        Assert.AreEqual("TC-310", FormRequests.IdOf("", "TC-310"));
        Assert.AreEqual("TC-310", FormRequests.IdOf(null, "TC-310"));
    }

    // ---- MetSoFar: the one papers menu ----

    [Test]
    public void MetSoFar_ListsEveryOnRequestFormOfTheDaysUpToToday()
    {
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof" }, Ids(FormRequests.MetSoFar(Days(), 1)));
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof", "TC-620", "TC-630" }, Ids(FormRequests.MetSoFar(Days(), 5)),
                                  "in first-appearance order: day 5 lists the displaced's forms first, but the tourists' came on day 1");
        CollectionAssert.AreEqual(Ids(FormRequests.MetSoFar(Days(), 6)), Ids(FormRequests.MetSoFar(Days(), 9)), "a day past the plans lists every day's");
        CollectionAssert.IsEmpty(FormRequests.MetSoFar(Days(), 0));
        CollectionAssert.IsEmpty(FormRequests.MetSoFar(null, 3));
    }

    [Test]
    public void MetSoFar_NeverListsALaterDaysForm()
    {
        foreach (int day in new[] { 1, 2, 3, 4 })
            CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof" }, Ids(FormRequests.MetSoFar(Days(), day)), $"day {day}: the displaced's forms wait for day 5");
    }

    [Test]
    public void MetSoFar_KeepsAnEarlierDaysFormOnADayWithASmallerMix()
    {
        List<IReadOnlyList<AskableForm>> days = Days();
        days[1] = new List<AskableForm> { Forms()[0], Forms()[1] };
        days[5] = Displaced();

        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof" }, Ids(FormRequests.MetSoFar(days, 2)), "day 2 has only rich tourists, but the waiver and the proof were met on day 1");
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof", "TC-620", "TC-630" }, Ids(FormRequests.MetSoFar(days, 6)), "no entry is taken away");
    }

    [Test]
    public void MetSoFar_OneEntryPerGroup()
    {
        List<AskableForm> menu = FormRequests.MetSoFar(Days(), 6);

        Assert.AreEqual(1, menu.Count(f => f.AskGroup == AccountMaker.ProofGroup), "the three proofs of means are one entry");
        Assert.AreEqual("TC-415", menu.Single(f => f.AskGroup == AccountMaker.ProofGroup).FormNumber, "the group's first form stands for it");
        Assert.AreEqual(menu.Count, FormRequests.Count(menu));
        Assert.AreEqual(5, FormRequests.Count(menu), "< Back and five requests fit the wheel's eight");
    }

    [Test]
    public void MetSoFar_SkipsFormsHandedOverOnArrival()
    {
        string[] ids = Ids(FormRequests.MetSoFar(Days(), 6));

        foreach (string arrival in new[] { "TC-101", "TC-520", "TC-610" })
            CollectionAssert.DoesNotContain(ids, arrival);
        CollectionAssert.IsEmpty(FormRequests.MetSoFar(new List<IReadOnlyList<AskableForm>> { new List<AskableForm> { null, Forms()[0] } }, 1), "null forms are skipped");
    }

    [Test]
    public void Count_OnePerFormOutsideAGroup_OnePerGroup()
    {
        Assert.AreEqual(3, FormRequests.Count(Tourists().Where(f => f.Requested).ToList()), "Manifest, Waiver, Proof of means");
        Assert.AreEqual(2, FormRequests.Count(Displaced().Where(f => f.Requested).ToList()));
        Assert.AreEqual(0, FormRequests.Count(null));
    }

    [Test]
    public void CarriedCount_EveryUngroupedForm_AndOnePerGroup()
    {
        Assert.AreEqual(4, FormRequests.CarriedCount(new[] { "", "", "", "proof", "proof", "proof" }), "the poor tourist's blueprint: four papers");
        Assert.AreEqual(2, FormRequests.CarriedCount(new[] { null, "" }));
        Assert.AreEqual(0, FormRequests.CarriedCount(null));
    }

    [Test]
    public void GroupLabel_AndRequestLabel()
    {
        Assert.AreEqual("Proof of means", FormRequests.GroupLabel("proof", Groups()));
        Assert.AreEqual("proof", FormRequests.GroupLabel("proof", null), "an unlabelled group reads by its id");
        Assert.IsNull(FormRequests.GroupLabel("", Groups()));
        Assert.AreEqual("Proof of means", FormRequests.RequestLabel("proof", "Proof of Funds", Groups()), "a grouped paper is asked for by the group");
        Assert.AreEqual("Stranding Waiver", FormRequests.RequestLabel("", "Stranding Waiver", Groups()));
    }

    [Test]
    public void Build_APoorTourist_ManifestWaiverAndOneProofEntry_EachHandingOverItsPaper()
    {
        List<FormRequest> requests = FormRequests.Build(FormRequests.MetSoFar(Days(), 1), PoorPapers(), Groups());
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof" }, requests.Select(r => r.Id));
        CollectionAssert.AreEqual(new[] { "Departure Manifest", "Stranding Waiver", "Proof of means" }, requests.Select(r => r.Label));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, requests.Select(r => r.Document), "the group's entry hands over the one proof the traveller holds, whichever of the three");
        Assert.IsTrue(requests.All(r => r.Carried));
    }

    [Test]
    public void Build_ARichTourist_CarriesNoWaiverAndNoProof()
    {
        List<FormRequest> requests = FormRequests.Build(FormRequests.MetSoFar(Days(), 1), RichPapers(), Groups());
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof" }, requests.Select(r => r.Id));
        CollectionAssert.AreEqual(new[] { 1, -1, -1 }, requests.Select(r => r.Document));
        Assert.IsFalse(requests[1].Carried);
        Assert.AreEqual("Stranding Waiver", requests[1].Label);
        Assert.AreEqual("Proof of means", requests[2].Label);
    }

    [Test]
    public void Build_ADisplacedTravellerOnDay5_TheSameFiveEntries_HandingOverTheirTwo()
    {
        var papers = new List<CaseDocument> { Doc("TC-610", "Displacement Certificate", handOver: DocumentHandOver.OnArrival), Doc("TC-620", "Intake Declaration"), Doc("TC-630", "Return Order") };
        List<FormRequest> requests = FormRequests.Build(FormRequests.MetSoFar(Days(), 5), papers, Groups());

        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof", "TC-620", "TC-630" }, requests.Select(r => r.Id));
        CollectionAssert.AreEqual(new[] { -1, -1, -1, 1, 2 }, requests.Select(r => r.Document));
    }

    [Test]
    public void Build_WithNoAskableForms_OneEntryPerPaperOnRequest_SoEveryPaperCanBeAskedFor()
    {
        List<FormRequest> requests = FormRequests.Build(null, PoorPapers(), null);
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof" }, requests.Select(r => r.Id));
        CollectionAssert.AreEqual(new[] { "Departure Manifest", "Stranding Waiver", "Proof of Funds" }, requests.Select(r => r.Label), "named by the form without the group's label");
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, requests.Select(r => r.Document));

        var unnumbered = new List<CaseDocument> { Doc(null, "Passport"), Doc(null, "Permit") };
        CollectionAssert.AreEqual(new[] { "doc:0", "doc:1" }, FormRequests.Build(null, unnumbered, null).Select(r => r.Id), "papers without a form number are keyed by their index");
        CollectionAssert.IsEmpty(FormRequests.Build(null, null, null));
        CollectionAssert.IsEmpty(FormRequests.Build(null, new[] { Doc("TC-101", "Visa", handOver: DocumentHandOver.OnArrival) }, null), "a paper handed over on arrival is never asked for");
    }

    [Test]
    public void Build_APaperOfAnUnlistedForm_IsAppendedOnce()
    {
        List<CaseDocument> papers = PoorPapers();
        papers.Add(Doc("TC-999", "Mystery Form"));
        List<FormRequest> requests = FormRequests.Build(FormRequests.MetSoFar(Days(), 1), papers, Groups());
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof", "TC-999" }, requests.Select(r => r.Id));
        Assert.AreEqual(4, requests[3].Document);
    }

    // ---- GroupProblems ----

    [Test]
    public void GroupProblems_NoneForTheProofGroup()
    {
        CollectionAssert.IsEmpty(FormRequests.GroupProblems(Forms(), Groups()));
    }

    [Test]
    public void GroupProblems_NameEachBrokenGroup()
    {
        List<AskableForm> forms = Forms();
        forms[4].Requested = false;                                         // a grouped form on arrival
        forms.Add(Form("TC-520", "Labour Contract", "contract", true));     // another group, unlabelled
        List<AskGroupLabel> groups = Groups();
        groups.Add(new AskGroupLabel { id = "proof", label = "Again" });
        groups.Add(new AskGroupLabel { id = "", label = "Blank" });
        groups.Add(new AskGroupLabel { id = "unused", label = "" });
        List<string> problems = FormRequests.GroupProblems(forms, groups);
        string all = string.Join("\n", problems);
        StringAssert.Contains("'proof' is listed twice", all);
        StringAssert.Contains("blank id", all);
        StringAssert.Contains("'unused' has a blank label", all);
        StringAssert.Contains("labels the group 'unused', but no form is in it", all);
        StringAssert.Contains("'TC-416' is in the request group 'proof' but is handed over on arrival", all);
        StringAssert.Contains("'TC-520' is in the request group 'contract'; the only group is 'proof'", all);
        StringAssert.Contains("'TC-520' is in the request group 'contract', which interview.askGroups does not label", all);
    }

    // ---- ReplyProblems ----

    private static MissingFormReply Reply(TravellerKind kind, string request, MissingFormVariant variant, string text = "A line.") =>
        new MissingFormReply { kind = kind, request = request, variant = variant, line = new LineText("id", text) };

    /// <summary>Each kind in play on each of <paramref name="days"/> (1-based) with that day's menu (MetSoFar) and its blueprint's carried forms, as Generate World and the validator build them.</summary>
    private static List<KindForms> KindsInPlay(params int[] days)
    {
        var kinds = new List<KindForms>();
        foreach (int day in days)
        {
            List<AskableForm> menu = FormRequests.MetSoFar(Days(), day);
            kinds.Add(new KindForms { Kind = TravellerKind.RichTourist, Askable = menu, Carried = Forms().Take(2).ToList() });
            kinds.Add(new KindForms { Kind = TravellerKind.PoorTourist, Askable = menu, Carried = Tourists() });
            if (day >= 3)
                kinds.Add(new KindForms { Kind = TravellerKind.Labourer, Askable = menu, Carried = Labourers() });
            if (day >= 5)
                kinds.Add(new KindForms { Kind = TravellerKind.Displaced, Askable = menu, Carried = Displaced() });
        }
        return kinds;
    }

    /// <summary>Every Honest line the one menu needs by day 6 (the personalities spec's §3.3 and today's rows).</summary>
    private static List<MissingFormReply> HonestReplies() => new List<MissingFormReply>
    {
        Reply(TravellerKind.RichTourist, "TC-310", MissingFormVariant.Honest, "It's a Premium unit, I don't need one."),
        Reply(TravellerKind.RichTourist, "proof", MissingFormVariant.Honest, "I pay my own way."),
        Reply(TravellerKind.Labourer, "proof", MissingFormVariant.Honest, "Debt Relief pays my way."),
        Reply(TravellerKind.Displaced, "TC-230", MissingFormVariant.Honest), Reply(TravellerKind.Displaced, "TC-310", MissingFormVariant.Honest),
        Reply(TravellerKind.Displaced, "proof", MissingFormVariant.Honest),
        Reply(TravellerKind.RichTourist, "TC-620", MissingFormVariant.Honest), Reply(TravellerKind.RichTourist, "TC-630", MissingFormVariant.Honest),
        Reply(TravellerKind.PoorTourist, "TC-620", MissingFormVariant.Honest), Reply(TravellerKind.PoorTourist, "TC-630", MissingFormVariant.Honest),
        Reply(TravellerKind.Labourer, "TC-620", MissingFormVariant.Honest), Reply(TravellerKind.Labourer, "TC-630", MissingFormVariant.Honest),
        Reply(TravellerKind.PoorTourist, "proof", MissingFormVariant.Missing, "I... didn't get round to that one.")
    };

    [Test]
    public void ReplyProblems_NoneWhenEveryUncarriedRequestHasItsHonestReply()
    {
        CollectionAssert.IsEmpty(FormRequests.ReplyProblems(HonestReplies(), Forms().Concat(Labourers()).ToList(), KindsInPlay(1, 2, 3, 4, 5, 6)));
    }

    [Test]
    public void ReplyProblems_EveryKindInPlayNeedsAnHonestLineForEachRequestItNeverCarries()
    {
        List<string> day1 = FormRequests.ReplyProblems(new List<MissingFormReply>(), Forms(), KindsInPlay(1));
        CollectionAssert.AreEqual(new[] { "RichTourist/TC-310", "RichTourist/proof" }, day1.Select(Pair), "a poor tourist carries all three");

        List<string> week = FormRequests.ReplyProblems(new List<MissingFormReply>(), Forms().Concat(Labourers()).ToList(), KindsInPlay(1, 2, 3, 4, 5, 6));
        CollectionAssert.AreEqual(new[]
        {
            "RichTourist/TC-310", "RichTourist/proof", "Labourer/proof", "RichTourist/TC-620", "RichTourist/TC-630", "PoorTourist/TC-620", "PoorTourist/TC-630",
            "Labourer/TC-620", "Labourer/TC-630", "Displaced/TC-230", "Displaced/TC-310", "Displaced/proof"
        }, week.Select(Pair), "each (kind, request) once, from the first day the menu offers it to a kind that never carries it");
        StringAssert.Contains("interview.missingFormReplies: a Displaced traveller may be asked for 'TC-230' but carries none; add their Honest reply.", string.Join("\n", week));

        List<MissingFormReply> onlyToday = HonestReplies().Where(r => r.request != "TC-620").ToList();
        CollectionAssert.AreEqual(new[] { "RichTourist/TC-620", "PoorTourist/TC-620", "Labourer/TC-620" },
                                  FormRequests.ReplyProblems(onlyToday, Forms().Concat(Labourers()).ToList(), KindsInPlay(5)).Select(Pair));
    }

    /// <summary>"Kind/request" of a missing-reply problem.</summary>
    private static string Pair(string problem)
    {
        int a = problem.IndexOf("a ", System.StringComparison.Ordinal) + 2;
        string kind = problem.Substring(a, problem.IndexOf(' ', a) - a);
        int q = problem.IndexOf('\'') + 1;
        return kind + "/" + problem.Substring(q, problem.IndexOf('\'', q) - q);
    }

    [Test]
    public void ReplyProblems_NameEachBrokenReply()
    {
        var replies = new List<MissingFormReply>
        {
            Reply(TravellerKind.RichTourist, "TC-310", MissingFormVariant.Honest),
            Reply(TravellerKind.RichTourist, "TC-310", MissingFormVariant.Honest),
            Reply(TravellerKind.RichTourist, "proof", MissingFormVariant.Honest, " "),
            Reply(TravellerKind.Labourer, "", MissingFormVariant.Honest),
            Reply(TravellerKind.Labourer, "TC-999", MissingFormVariant.Missing)
        };
        string all = string.Join("\n", FormRequests.ReplyProblems(replies, Forms(), KindsInPlay(1)));
        StringAssert.Contains("RichTourist / 'TC-310' / Honest is listed twice", all);
        StringAssert.Contains("RichTourist / 'proof' / Honest line is blank", all);
        StringAssert.Contains("a Labourer reply names no request", all);
        StringAssert.Contains("names the request 'TC-999', which is no form number and no request group", all);
    }
}
