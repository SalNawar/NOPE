using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The papers menu's rules (traveller types I2, phase 8): which forms a kind
/// may be asked for, one entry per form or request group, which paper each
/// entry hands over, how many papers a blueprint's traveller carries, and
/// the content rules for groups and missing-form replies.
/// </summary>
public class FormRequestsTests
{
    private static readonly TravellerKind[] Citizens = { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer };

    private static AskableForm Form(string number, string name, string group, bool requested, params TravellerKind[] askableBy) =>
        new AskableForm(number, name, group, requested, askableBy);

    /// <summary>The ten forms' relevant part: the visa on arrival; the manifest, the waiver and the three proofs (the proof group) askable by every citizen; the displaced's two on request.</summary>
    private static List<AskableForm> Forms() => new List<AskableForm>
    {
        Form("TC-101", "Leisure Departure Visa", "", false, Citizens),
        Form("TC-230", "Departure Manifest", "", true, Citizens),
        Form("TC-310", "Stranding Waiver", "", true, Citizens),
        Form("TC-415", "Holiday Credit Agreement", AccountMaker.ProofGroup, true, Citizens),
        Form("TC-416", "Proof of Funds", AccountMaker.ProofGroup, true, Citizens),
        Form("TC-417", "Travel Insurance Certificate", AccountMaker.ProofGroup, true, Citizens),
        Form("TC-610", "Displacement Certificate", "", false, TravellerKind.Displaced),
        Form("TC-620", "Intake Declaration", "", true, TravellerKind.Displaced),
        Form("TC-630", "Return Order", "", true, TravellerKind.Displaced)
    };

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

    [Test]
    public void For_TheFormsOnRequest_AskableByTheKind_InOrder()
    {
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "TC-415", "TC-416", "TC-417" }, FormRequests.For(TravellerKind.PoorTourist, Forms()).Select(f => f.FormNumber));
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "TC-415", "TC-416", "TC-417" }, FormRequests.For(TravellerKind.RichTourist, Forms()).Select(f => f.FormNumber), "a rich tourist may be asked for the waiver and a proof too: their honest reply shows they need none");
        CollectionAssert.AreEqual(new[] { "TC-620", "TC-630" }, FormRequests.For(TravellerKind.Displaced, Forms()).Select(f => f.FormNumber));
        CollectionAssert.IsEmpty(FormRequests.For(TravellerKind.Displaced, null));
    }

    [Test]
    public void Count_OnePerFormOutsideAGroup_OnePerGroup()
    {
        Assert.AreEqual(3, FormRequests.Count(FormRequests.For(TravellerKind.PoorTourist, Forms())), "Manifest, Waiver, Proof of means");
        Assert.AreEqual(2, FormRequests.Count(FormRequests.For(TravellerKind.Displaced, Forms())));
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
        List<FormRequest> requests = FormRequests.Build(FormRequests.For(TravellerKind.PoorTourist, Forms()), PoorPapers(), Groups());
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof" }, requests.Select(r => r.Id));
        CollectionAssert.AreEqual(new[] { "Departure Manifest", "Stranding Waiver", "Proof of means" }, requests.Select(r => r.Label));
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, requests.Select(r => r.Document), "the group's entry hands over the one proof the traveller holds, whichever of the three");
        Assert.IsTrue(requests.All(r => r.Carried));
    }

    [Test]
    public void Build_ARichTourist_CarriesNoWaiverAndNoProof()
    {
        List<FormRequest> requests = FormRequests.Build(FormRequests.For(TravellerKind.RichTourist, Forms()), RichPapers(), Groups());
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310", "proof" }, requests.Select(r => r.Id));
        CollectionAssert.AreEqual(new[] { 1, -1, -1 }, requests.Select(r => r.Document));
        Assert.IsFalse(requests[1].Carried);
        Assert.AreEqual("Stranding Waiver", requests[1].Label);
        Assert.AreEqual("Proof of means", requests[2].Label);
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
        List<FormRequest> requests = FormRequests.Build(FormRequests.For(TravellerKind.PoorTourist, Forms()), papers, Groups());
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
        forms[5].AskableBy = new[] { TravellerKind.PoorTourist };            // different kinds
        forms.Add(Form("TC-520", "Labour Contract", "contract", true, TravellerKind.Labourer)); // another group, unlabelled
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
        StringAssert.Contains("'TC-417' share the request group 'proof' but are askable by different kinds", all);
        StringAssert.Contains("'TC-520' is in the request group 'contract'; the only group is 'proof'", all);
        StringAssert.Contains("'TC-520' is in the request group 'contract', which interview.askGroups does not label", all);
    }

    // ---- ReplyProblems ----

    private static MissingFormReply Reply(TravellerKind kind, string request, MissingFormVariant variant, string text = "A line.") =>
        new MissingFormReply { kind = kind, request = request, variant = variant, line = new LineText("id", text) };

    /// <summary>The kinds in play on day 1: the rich tourist's blueprint (visa, manifest) and the poor tourist's (all six).</summary>
    private static List<KindForms> Kinds()
    {
        List<AskableForm> forms = Forms();
        return new List<KindForms>
        {
            new KindForms { Kind = TravellerKind.RichTourist, Askable = FormRequests.For(TravellerKind.RichTourist, forms), Carried = forms.Take(2).ToList() },
            new KindForms { Kind = TravellerKind.PoorTourist, Askable = FormRequests.For(TravellerKind.PoorTourist, forms), Carried = forms.Take(6).ToList() },
            new KindForms { Kind = TravellerKind.Displaced, Askable = FormRequests.For(TravellerKind.Displaced, forms), Carried = forms.Skip(6).ToList() }
        };
    }

    [Test]
    public void ReplyProblems_NoneWhenEveryUncarriedRequestHasItsHonestReply()
    {
        var replies = new List<MissingFormReply>
        {
            Reply(TravellerKind.RichTourist, "TC-310", MissingFormVariant.Honest, "It's a Premium unit, I don't need one."),
            Reply(TravellerKind.RichTourist, "proof", MissingFormVariant.Honest, "I pay my own way."),
            Reply(TravellerKind.PoorTourist, "proof", MissingFormVariant.Missing, "I... didn't get round to that one.")
        };
        CollectionAssert.IsEmpty(FormRequests.ReplyProblems(replies, Forms(), Kinds()));
    }

    [Test]
    public void ReplyProblems_ARichTouristAskedForAWaiverOrAProof_NeedsTheHonestLine()
    {
        List<string> problems = FormRequests.ReplyProblems(new List<MissingFormReply>(), Forms(), Kinds());
        Assert.AreEqual(2, problems.Count, string.Join("\n", problems));
        StringAssert.Contains("a RichTourist traveller may be asked for 'TC-310' but carries none; add their Honest reply", problems[0]);
        StringAssert.Contains("a RichTourist traveller may be asked for 'proof' but carries none", problems[1]);
        Assert.IsFalse(problems.Any(p => p.Contains("PoorTourist") || p.Contains("Displaced")), "they carry every form they can be asked for");
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
        string all = string.Join("\n", FormRequests.ReplyProblems(replies, Forms(), Kinds()));
        StringAssert.Contains("RichTourist / 'TC-310' / Honest is listed twice", all);
        StringAssert.Contains("RichTourist / 'proof' / Honest line is blank", all);
        StringAssert.Contains("a Labourer reply names no request", all);
        StringAssert.Contains("names the request 'TC-999', which is no form number and no request group", all);
    }
}
