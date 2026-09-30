using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The interview graph (hub, ask menu, merged dialogs) and the structure and
/// capacity rules. Traveller under test: a displaced person who claims an
/// Ancient place and carries the displaced's three forms (the Displacement
/// Certificate handed over on arrival, the Intake Declaration and the Return
/// Order on request, unless a test says otherwise), answers Currency honestly ("Deben")
/// and Geography with an Answer tell ("Babylon"), has no Politics answer, and
/// has a small-talk line. The rumour dialog is shaped like dlg_rumour. With a
/// key-word rule, each traveller line carries the spans that stay English
/// when it shows untranslated (the traveller-types spec's §8.1). Every
/// traveller of a day is offered the same wheel (the personalities spec's
/// W1-W5): a request entry's id names the request, whatever they carry.
/// </summary>
public class InterviewScriptTests
{
    private static InterviewLines Lines() => new InterviewLines
    {
        deskName = "DESK",
        claims = { new KindLine { kind = TravellerKind.Displaced, line = new LineText("interview.claims.Displaced", "Please. Send me home to {place}.") } },
        requestLabel = "Request {document}",
        papersLabel = "Request papers >",
        requestPrompt = new LineText("interview.requestPrompt", "Your {document}, please."),
        requestReply = new LineText("interview.requestReply", "Here you are."),
        askLabel = "Ask about the trip >",
        lookLabel = "Look >",
        backLabel = "< Back",
        smallTalkLabel = "Small talk",
        smallTalkPrompt = new LineText("interview.smallTalkPrompt", "How is life back home?")
    };

    private static InterviewQuestion Question(string id, ClueCategory category, string label, string answer) => new InterviewQuestion
    {
        id = id,
        category = category,
        label = label,
        prompt = new LineText(id + ".prompt", "About " + label + "?"),
        answer = new LineText(id + ".answer", answer)
    };

    private static List<InterviewQuestion> Questions()
    {
        InterviewQuestion currency = Question("q_currency", ClueCategory.Currency, "Currency", "We pay in {value}.");
        currency.overrides.Add(new WordingOverride
        {
            eraId = "ancient",
            kinds = { TravellerKind.Displaced },
            answer = new LineText("q_currency.overrides.1.answer", "We trade with {value}.")
        });

        return new List<InterviewQuestion>
        {
            currency,
            Question("q_capital", ClueCategory.Geography, "Capital", "Our capital is {value}."),
            Question("q_ruler", ClueCategory.Politics, "Ruler", "We are ruled by {value}.")
        };
    }

    private static CaseDocument Doc(string name, DocumentHandOver handOver = DocumentHandOver.OnRequest, string formNumber = null, string askGroup = null) =>
        new CaseDocument { name = name, handOver = handOver, formNumber = formNumber, askGroup = askGroup };

    /// <summary>The papers menu of days 1-4 (the personalities spec's W4): the manifest, the waiver and the proof of means (its group's first form).</summary>
    private static AskableForm[] CitizenAskable() => new[]
    {
        new AskableForm("TC-230", "Departure Manifest", "", true),
        new AskableForm("TC-310", "Stranding Waiver", "", true),
        new AskableForm("TC-415", "Holiday Credit Agreement", AccountMaker.ProofGroup, true)
    };

    /// <summary>A poor tourist's four papers: the visa on arrival, the manifest, the waiver and the Proof of Funds they hold.</summary>
    private static CaseDocument[] PoorForms() => new[]
    {
        Doc("Leisure Departure Visa", DocumentHandOver.OnArrival, "TC-101"), Doc("Departure Manifest", formNumber: "TC-230"),
        Doc("Stranding Waiver", formNumber: "TC-310"), Doc("Proof of Funds", formNumber: "TC-416", askGroup: AccountMaker.ProofGroup)
    };

    /// <summary>A rich tourist's two papers.</summary>
    private static CaseDocument[] RichForms() => new[] { Doc("Leisure Departure Visa", DocumentHandOver.OnArrival, "TC-101"), Doc("Departure Manifest", formNumber: "TC-230") };

    /// <summary>The wording with the proof group's label and the rich tourist's honest replies (world_source.json interview.askGroups, interview.missingFormReplies).</summary>
    private static InterviewLines LinesWithGroups()
    {
        InterviewLines lines = Lines();
        lines.askGroups.Add(new AskGroupLabel { id = AccountMaker.ProofGroup, label = "Proof of means" });
        lines.missingFormReplies.Add(new MissingFormReply { kind = TravellerKind.RichTourist, request = "TC-310", variant = MissingFormVariant.Honest, line = new LineText("interview.missingFormReplies.RichTourist.TC-310.Honest", "It's a Premium unit, I don't need one.") });
        lines.missingFormReplies.Add(new MissingFormReply { kind = TravellerKind.RichTourist, request = AccountMaker.ProofGroup, variant = MissingFormVariant.Honest, line = new LineText("interview.missingFormReplies.RichTourist.proof.Honest", "I pay my own way.") });
        lines.missingFormReplies.Add(new MissingFormReply { kind = TravellerKind.PoorTourist, request = AccountMaker.ProofGroup, variant = MissingFormVariant.Missing, line = new LineText("interview.missingFormReplies.PoorTourist.proof.Missing", "I... didn't get round to that one.") });
        return lines;
    }

    /// <summary>A 2150 citizen of <paramref name="kind"/> with <paramref name="documents"/>, asked for the citizens' forms.</summary>
    private static InterviewCase Citizen(TravellerKind kind, CaseDocument[] documents, MissingFormVariant variant = MissingFormVariant.Honest)
    {
        InterviewCase c = Case(documents: documents);
        c.kind = kind;
        c.askable = CitizenAskable();
        c.missingVariant = variant;
        return c;
    }

    /// <summary>The key-word rule as world_source.json authors it (translation.keyWords).</summary>
    private static KeyWordRule KeyWordRule() => new KeyWordRule
    {
        slots = { "place", "name", "document" },
        words = { "home", "please", "papers", "yes", "no", "Temporal Customs" },
        digits = true
    };

    /// <summary>The displaced's three forms: the certificate on arrival, the declaration and the return order on request.</summary>
    private static CaseDocument[] DisplacedForms() => new[]
    {
        Doc("Displacement Certificate", DocumentHandOver.OnArrival, "TC-610"), Doc("Intake Declaration", formNumber: "TC-620"), Doc("Return Order", formNumber: "TC-630")
    };

    private static InterviewCase Case(bool smallTalk = true, string intro = "Next! Step forward, sir.", CaseDocument[] documents = null, KeyWordRule keyWords = null) => new InterviewCase
    {
        introLine = intro,
        kind = TravellerKind.Displaced,
        claimPlace = "New Kingdom Egypt (Ancient)",
        keyWords = keyWords,
        claimedEraId = "ancient",
        documents = documents ?? DisplacedForms(),
        answers = new[]
        {
            new InterviewAnswer { category = ClueCategory.Currency, value = "Deben", isTell = false },
            new InterviewAnswer { category = ClueCategory.Geography, value = "Babylon", isTell = true }
        },
        smallTalk = smallTalk ? new LineText("egypt_ancient.smalltalk.1", "The Nile rose right on time.") : null
    };

    private static ScriptLine Said(string id, string text) => new ScriptLine { id = id, speaker = DialogSpeaker.Traveller, text = text };

    private static ScriptChoice Reply(string id, string label, string next = "", string effect = "") =>
        new ScriptChoice { id = id, label = label, next = next, effect = effect };

    /// <summary>start: "more" (to detail) or "ignore" (ends); detail: "noted" (ends, with the rumour effect).</summary>
    private static AuthoredDialog Rumour() => new AuthoredDialog
    {
        id = "dlg_rumour",
        label = "Any news from home? >",
        oneShot = true,
        nodes =
        {
            new ScriptNode
            {
                id = "start",
                lines = { Said("dlg_rumour.start.1", "News? Only a rumour.") },
                choices = { Reply("more", "Tell me more.", "detail"), Reply("ignore", "Not my business.") }
            },
            new ScriptNode
            {
                id = "detail",
                lines = { Said("dlg_rumour.detail.1", "They say a courier carries them.") },
                choices = { Reply("noted", "Noted. Thank you.", effect: "Effect_Dialog_RumourHeard") }
            }
        }
    };

    private static InterviewRequest Spoken(string id, string label, string prompt, string reply) => new InterviewRequest
    {
        id = id,
        label = label,
        prompt = new LineText($"interview.requests.{id}.prompt", prompt),
        reply = new LineText($"interview.requests.{id}.reply", reply)
    };

    /// <summary>The wording plus two spoken requests, "Step closer" and "Speak up".</summary>
    private static InterviewLines LinesWithRequests()
    {
        InterviewLines lines = Lines();
        lines.requests.Add(Spoken("step_closer", "Step closer", "Step closer to the glass, please.", "Like this?"));
        lines.requests.Add(Spoken("speak_up", "Speak up", "Speak up, please.", "Sorry. Is this better?"));
        return lines;
    }

    private static DialogGraph Build(InterviewCase c = null, IReadOnlyList<InterviewQuestion> questions = null, IReadOnlyList<AuthoredDialog> dialogs = null,
                                     InterviewLines lines = null) =>
        InterviewScript.Build(lines ?? Lines(), questions ?? Questions(), dialogs ?? new[] { Rumour() }, c ?? Case());

    // -----------------------------
    // The voice (the personalities spec's V1, V3, T2-T3)
    // -----------------------------

    /// <summary>A row of curt's for a slot.</summary>
    private static VoiceLine V(string text, string key = null, MissingFormVariant variant = MissingFormVariant.Honest, params TravellerKind[] kinds) =>
        new VoiceLine { personality = "curt", kinds = kinds.ToList(), key = key ?? string.Empty, variant = variant, line = new LineText("interview.voices.x.curt." + text.Length, text) };

    /// <summary>The wording with the groups, the spoken requests and curt's rows: a displaced claim, a hand-over, a rich tourist's waiver refusal, "Speak up", the currency and capital answers.</summary>
    private static InterviewLines VoicedLines()
    {
        InterviewLines lines = LinesWithGroups();
        lines.requests.Add(Spoken("step_closer", "Step closer", "Step closer to the glass, please.", "Like this?"));
        lines.requests.Add(Spoken("speak_up", "Speak up", "Speak up, please.", "Sorry. Is this better?"));
        lines.voices.claims.Add(V("{place}. Home. Now.", kinds: TravellerKind.Displaced));
        lines.voices.handOver.Add(V("Here."));
        lines.voices.missingForms.Add(V("Premium units are exempt from the {document}.", "TC-310", kinds: TravellerKind.RichTourist));
        lines.voices.spoken.Add(V("Said it once. That was billed.", "speak_up"));
        lines.voices.answers.Add(V("{value}. Not that it'll be enough.", "q_currency"));
        lines.voices.answers.Add(V("{value}, I'm sure.", "q_capital"));
        return lines;
    }

    private static InterviewCase Voiced(InterviewCase c)
    {
        c.voice = new Voice("curt", null, 7);
        return c;
    }

    [Test]
    public void Opening_SaysTheVoicesClaim()
    {
        IReadOnlyList<DialogLine> opening = InterviewScript.Opening(VoicedLines(), Voiced(Case()));
        Assert.AreEqual((InterviewScript.ClaimLineId, DialogSpeaker.Traveller, "New Kingdom Egypt (Ancient). Home. Now."), (opening[1].Id, opening[1].Speaker, opening[1].Text));
        Assert.AreEqual(opening[1].Text, InterviewScript.Claim(VoicedLines(), Voiced(Case())));
    }

    [Test]
    public void Opening_ClaimKeyWordsAreTakenOverItsTemplate()
    {
        DialogLine claim = InterviewScript.Opening(VoicedLines(), Voiced(Case(keyWords: KeyWordRule())))[1];
        Assert.AreEqual("New Kingdom Egypt (Ancient)|Home", English(claim), "the place's fill and the key word of the voice's own template");
    }

    [Test]
    public void Opening_NoVoiceSaysTheKindsClaim()
    {
        Assert.AreEqual("Please. Send me home to New Kingdom Egypt (Ancient).", InterviewScript.Opening(VoicedLines(), Case())[1].Text);
        InterviewCase chatty = Case();
        chatty.voice = new Voice("chatty", null, 7);
        Assert.AreEqual("Please. Send me home to New Kingdom Egypt (Ancient).", InterviewScript.Opening(VoicedLines(), chatty)[1].Text, "a personality with no row: the default");
    }

    [Test]
    public void Build_TheHandOverInTheVoicesWords()
    {
        DialogChoice order = Build(Voiced(Case()), lines: VoicedLines()).Node(InterviewScript.PapersNodeId).Choices.First(x => x.Id == "request:TC-630");
        Assert.AreEqual(DialogAction.HandOverDocument, order.Action);
        Assert.AreEqual("Your Return Order, please.", order.Lines[0].Text, "the desk's words are everyone's");
        Assert.AreEqual("Here.", order.Lines[1].Text);
        Assert.AreEqual("Here you are.", Build(Case(), lines: VoicedLines()).Node(InterviewScript.PapersNodeId).Choices.First(x => x.Id == "request:TC-630").Lines[1].Text);
    }

    [Test]
    public void Build_ARefusalByKindVariantAndVoice()
    {
        DialogNode rich = Build(Voiced(Citizen(TravellerKind.RichTourist, RichForms())), lines: VoicedLines()).Node(InterviewScript.PapersNodeId);
        Assert.AreEqual("Premium units are exempt from the Stranding Waiver.", rich.Choices.First(x => x.Id == "request:TC-310").Lines[1].Text, "{document} is the request's label");
        Assert.AreEqual("I pay my own way.", rich.Choices.First(x => x.Id == "request:proof").Lines[1].Text, "no row for the proof: the kind's default");

        DialogNode poor = Build(Voiced(Citizen(TravellerKind.PoorTourist, PoorForms().Take(3).ToArray(), MissingFormVariant.Missing)), lines: VoicedLines()).Node(InterviewScript.PapersNodeId);
        Assert.AreEqual("I... didn't get round to that one.", poor.Choices.First(x => x.Id == "request:proof").Lines[1].Text, "the Missing variant: the rich tourist's row is not theirs");
    }

    [Test]
    public void Build_ASpokenRequestsReply()
    {
        DialogNode hub = Build(Voiced(Case()), lines: VoicedLines()).Node(InterviewScript.HubNodeId);
        Assert.AreEqual("Said it once. That was billed.", hub.Choices.First(x => x.Id == "act:speak_up").Lines[1].Text);
        Assert.AreEqual("Like this?", hub.Choices.First(x => x.Id == "act:step_closer").Lines[1].Text, "no row for Step closer: its reply");
        Assert.AreEqual("Step closer to the glass, please.", hub.Choices.First(x => x.Id == "act:step_closer").Lines[0].Text);
    }

    [Test]
    public void Build_AnAnswerKeepsItsCanonicalValueAndFact()
    {
        DialogLine answer = Build(Voiced(Case()), lines: VoicedLines()).Node(InterviewScript.AskNodeId).Choices.First(x => x.Id == "q:q_currency").Lines[1];
        Assert.AreEqual("Deben. Not that it'll be enough.", answer.Text);
        Assert.IsTrue(answer.IsAnswer);
        Assert.AreEqual((ClueCategory.Currency, "Deben", false), (answer.Category, answer.Value, answer.IsTell), "the value and the fact are the answer's, whatever the voice");
        Assert.AreEqual("About Currency?", Build(Voiced(Case()), lines: VoicedLines()).Node(InterviewScript.AskNodeId).Choices.First(x => x.Id == "q:q_currency").Lines[0].Text, "the desk's prompt is everyone's");
    }

    [Test]
    public void Build_ATellsSentenceIsTheHonestSentence()
    {
        // The personalities spec's T3: two travellers alike but for one answer's tell say the same sentence around the value,
        // and the same claim, replies and small talk.
        InterviewCase honest = Voiced(Case()), liar = Voiced(Case());
        honest.answers = new[] { new InterviewAnswer { category = ClueCategory.Currency, value = "Deben" }, new InterviewAnswer { category = ClueCategory.Geography, value = "Thebes" } };
        liar.answers = new[] { new InterviewAnswer { category = ClueCategory.Currency, value = "Deben" }, new InterviewAnswer { category = ClueCategory.Geography, value = "Babylon", isTell = true } };

        List<string> a = Walk(honest), b = Walk(liar);
        Assert.AreEqual(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
            Assert.AreEqual(a[i].Replace("Thebes", "{value}"), b[i].Replace("Babylon", "{value}"), $"line {i}");
        CollectionAssert.Contains(a, "Thebes, I'm sure.");
        CollectionAssert.Contains(b, "Babylon, I'm sure.");
    }

    [Test]
    public void Build_NoVoiceSaysTodaysLines()
    {
        CollectionAssert.AreEqual(new[]
        {
            "Next! Step forward, sir.", "Please. Send me home to New Kingdom Egypt (Ancient).",
            "Your Intake Declaration, please.", "Here you are.", "Your Return Order, please.", "Here you are.",
            "Step closer to the glass, please.", "Like this?", "Speak up, please.", "Sorry. Is this better?",
            "About Currency?", "We trade with Deben.", "About Capital?", "Our capital is Babylon.",
            "How is life back home?", "The Nile rose right on time."
        }, Walk(Case()), "no voice: every line is today's, whatever rows the voices have");
    }

    /// <summary>Every line of a walk through the hub's requests, the papers menu, the spoken requests and the ask menu, in order.</summary>
    private static List<string> Walk(InterviewCase c)
    {
        var runner = new DialogRunner(Build(c, dialogs: new AuthoredDialog[0], lines: VoicedLines()), InterviewScript.Opening(VoicedLines(), c));
        runner.Choose("papers");
        foreach (string id in Ids(runner.Choices).Where(id => id.StartsWith("request:")).ToList())
            runner.Choose(id);
        runner.Choose("back");
        runner.Choose("act:step_closer");
        runner.Choose("act:speak_up");
        runner.Choose("ask");
        foreach (string id in Ids(runner.Choices).Where(id => id != "back").ToList())
            runner.Choose(id);
        return runner.Transcript.Select(l => l.Text).ToList();
    }

    // -----------------------------
    // The one wheel (the personalities spec's W1-W6)
    // -----------------------------

    /// <summary>The papers menu of day 5 (the personalities spec's W4): the manifest, the waiver, the proof of means, then the displaced's declaration and return order.</summary>
    private static AskableForm[] Day5Menu() => new[]
    {
        new AskableForm("TC-230", "Departure Manifest", "", true),
        new AskableForm("TC-310", "Stranding Waiver", "", true),
        new AskableForm("TC-415", "Holiday Credit Agreement", AccountMaker.ProofGroup, true),
        new AskableForm("TC-620", "Intake Declaration", "", true),
        new AskableForm("TC-630", "Return Order", "", true)
    };

    /// <summary>A labourer's three papers: the contract on arrival, the manifest and the waiver.</summary>
    private static CaseDocument[] LabourerForms() => new[]
    {
        Doc("Labour Contract", DocumentHandOver.OnArrival, "TC-520"), Doc("Departure Manifest", formNumber: "TC-230"), Doc("Stranding Waiver", formNumber: "TC-310")
    };

    /// <summary>The wording of day 5 (§3.3): the groups, the spoken requests and every Honest line the one menu needs.</summary>
    private static InterviewLines Day5Lines()
    {
        InterviewLines lines = LinesWithGroups();
        lines.requests.Add(Spoken("step_closer", "Step closer", "Step closer to the glass, please.", "Like this?"));
        lines.requests.Add(Spoken("speak_up", "Speak up", "Speak up, please.", "Sorry. Is this better?"));
        void Honest(TravellerKind kind, string request, string text) =>
            lines.missingFormReplies.Add(new MissingFormReply { kind = kind, request = request, variant = MissingFormVariant.Honest, line = new LineText($"interview.missingFormReplies.{kind}.{request}.Honest", text) });
        Honest(TravellerKind.Displaced, "TC-230", "A manifest? I was pulled out of my own time. I didn't pack.");
        Honest(TravellerKind.Displaced, "TC-310", "A waiver? Nobody asked me anything before the sky opened.");
        Honest(TravellerKind.Displaced, AccountMaker.ProofGroup, "Means? I have what was in my pockets when the sky opened.");
        foreach (TravellerKind kind in new[] { TravellerKind.RichTourist, TravellerKind.PoorTourist, TravellerKind.Labourer })
        {
            Honest(kind, "TC-620", "An intake declaration? I'm leaving, not arriving.");
            Honest(kind, "TC-630", "A return order? I have a return booking. Is that the same thing?");
        }
        Honest(TravellerKind.Labourer, AccountMaker.ProofGroup, "Debt Relief pays my way.");
        return lines;
    }

    /// <summary>A traveller of <paramref name="kind"/> on day 5 with their own <paramref name="documents"/>, every answer, small talk and one garment.</summary>
    private static InterviewCase Day5(TravellerKind kind, CaseDocument[] documents, MissingFormVariant variant = MissingFormVariant.Honest)
    {
        InterviewCase c = Case(documents: documents);
        c.kind = kind;
        c.askable = Day5Menu();
        c.missingVariant = variant;
        c.answers = new[]
        {
            new InterviewAnswer { category = ClueCategory.Currency, value = "Silver drachma" },
            new InterviewAnswer { category = ClueCategory.Geography, value = "Athens" },
            new InterviewAnswer { category = ClueCategory.Politics, value = "The Assembly" }
        };
        c.garments = new[] { new Garment(LookSlot.Outfit, kind + " outfit", "chiton", false) };
        return c;
    }

    private static string Menu(DialogGraph graph, string node) =>
        string.Join(" | ", graph.Node(node).Choices.Select(x => $"{x.Id}:{x.Label}:{x.Kind}"));

    [Test]
    public void Build_EveryKindGetsTheSameHubPapersAndAskMenus()
    {
        var travellers = new Dictionary<TravellerKind, InterviewCase>
        {
            [TravellerKind.RichTourist] = Day5(TravellerKind.RichTourist, RichForms()),
            [TravellerKind.PoorTourist] = Day5(TravellerKind.PoorTourist, PoorForms()),
            [TravellerKind.Labourer] = Day5(TravellerKind.Labourer, LabourerForms()),
            [TravellerKind.Displaced] = Day5(TravellerKind.Displaced, DisplacedForms())
        };

        foreach (string node in new[] { InterviewScript.HubNodeId, InterviewScript.PapersNodeId, InterviewScript.AskNodeId })
        {
            List<string> menus = travellers.Values.Select(c => Menu(Build(c, lines: Day5Lines()), node)).ToList();
            Assert.AreEqual(1, menus.Distinct().Count(), $"{node}:\n{string.Join("\n", menus)}");
        }

        DialogGraph rich = Build(travellers[TravellerKind.RichTourist], lines: Day5Lines());
        CollectionAssert.AreEqual(new[] { "papers", "act:step_closer", "act:speak_up", "ask", "look", "dlg:dlg_rumour" }, Ids(rich.Node(InterviewScript.HubNodeId).Choices));
        CollectionAssert.AreEqual(new[] { "back", "request:TC-230", "request:TC-310", "request:proof", "request:TC-620", "request:TC-630" }, Ids(rich.Node(InterviewScript.PapersNodeId).Choices),
                                  "a request's id names the request, carried or not");
        CollectionAssert.AreEqual(new[] { "< Back", "Departure Manifest", "Stranding Waiver", "Proof of means", "Intake Declaration", "Return Order" },
                                  rich.Node(InterviewScript.PapersNodeId).Choices.Select(x => x.Label));
        CollectionAssert.AreEqual(new[] { "back", "q:q_currency", "q:q_capital", "q:q_ruler", "smalltalk" }, Ids(rich.Node(InterviewScript.AskNodeId).Choices));
    }

    /// <summary>Day 5's wording with the waiver pad (the endings and strandings spec §7.3): its entry, the desk's words and a default reply per answer.</summary>
    private static InterviewLines Day5PadLines()
    {
        InterviewLines lines = Day5Lines();
        lines.waiverPad.label = "Waiver pad: sign here";
        lines.waiverPad.prompt = new LineText("interview.waiverPad.prompt", "A blank from the pad. Sign at the foot, please; I'll file it.");
        foreach ((WaiverPadReply reply, string text) in new[]
                 {
                     (WaiverPadReply.Signs, "Where do I... there? Lovely."), (WaiverPadReply.Refuses, "I'd rather not sign anything today."),
                     (WaiverPadReply.NotNeeded, "I don't need one of those, surely."), (WaiverPadReply.AlreadySigned, "I signed one already. It's in the pile.")
                 })
            lines.waiverPad.replies.Add(new VoiceLine { key = reply.ToString(), line = new LineText("interview.waiverPad.replies." + reply, text) });
        return lines;
    }

    [Test]
    public void Build_TheWaiverPadIsTheSamePapersEntryForEveryTraveller_OnlyTheReplyChanges()
    {
        var travellers = new[]
        {
            (Day5(TravellerKind.RichTourist, RichForms()), WaiverPadReply.NotNeeded),
            (Day5(TravellerKind.PoorTourist, PoorForms()), WaiverPadReply.AlreadySigned),
            (Day5(TravellerKind.Labourer, LabourerForms()), WaiverPadReply.Signs),
            (Day5(TravellerKind.PoorTourist, PoorForms()), WaiverPadReply.Refuses),
            (Day5(TravellerKind.Displaced, DisplacedForms()), WaiverPadReply.NotNeeded)
        };
        var menus = new List<string>();
        foreach ((InterviewCase c, WaiverPadReply reply) in travellers)
        {
            c.padReply = reply;
            DialogGraph graph = Build(c, lines: Day5PadLines());
            menus.Add(Menu(graph, InterviewScript.PapersNodeId));
            DialogChoice pad = graph.Node(InterviewScript.PapersNodeId).Choices.Last();
            Assert.AreEqual(InterviewScript.PadChoiceId, pad.Id);
            Assert.AreEqual("Waiver pad: sign here", pad.Label);
            Assert.IsTrue(pad.OneShot);
            Assert.AreEqual(DialogChoiceKind.Request, pad.Kind);
            Assert.AreEqual("A blank from the pad. Sign at the foot, please; I'll file it.", pad.Lines[0].Text, "the desk's words are everyone's");
            Assert.AreEqual(DialogSpeaker.Traveller, pad.Lines[1].Speaker);
            Assert.AreEqual("interview.waiverPad.replies." + reply, pad.Lines[1].Id);
            Assert.AreEqual(reply == WaiverPadReply.Signs ? DialogAction.SignWaiver : DialogAction.None, pad.Action, reply.ToString());
        }
        Assert.AreEqual(1, menus.Distinct().Count(), string.Join("\n", menus));
    }

    [Test]
    public void Build_NoWaiverOnTheDaysMenu_OrNoPadAuthored_NoPad()
    {
        InterviewCase c = Day5(TravellerKind.Labourer, LabourerForms());
        c.askable = c.askable.Where(f => f.FormNumber != Directives.Waiver).ToList();
        CollectionAssert.DoesNotContain(Ids(Build(c, lines: Day5PadLines()).Node(InterviewScript.PapersNodeId).Choices), InterviewScript.PadChoiceId);
        CollectionAssert.DoesNotContain(Ids(Build(Day5(TravellerKind.Labourer, LabourerForms()), lines: Day5Lines()).Node(InterviewScript.PapersNodeId).Choices), InterviewScript.PadChoiceId);
        Assert.IsFalse(InterviewScript.OffersPad(null, Day5PadLines()));
    }

    [Test]
    public void MenuProblems_ThePadIsOneMoreEntry()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(0, false, 6, 0, 0, 0, 8, pad: true), "< Back + 6 + the pad = 8");
        StringAssert.Contains("The papers menu holds 9 choices (< Back, 7 request(s), the waiver pad)", string.Join("\n", DialogChecks.MenuProblems(0, false, 7, 0, 0, 0, 8, pad: true)));
        StringAssert.Contains("the waiver pad, 3 spoken request(s)", string.Join("\n", DialogChecks.MenuProblems(0, false, 1, 3, 2, 0, 8, pad: true)), "one request: the pad joins the hub");
    }

    [Test]
    public void Build_TheAskEntryIsTheAskLabelForEveryKind()
    {
        foreach (TravellerKind kind in (TravellerKind[])System.Enum.GetValues(typeof(TravellerKind)))
        {
            InterviewCase c = Case();
            c.kind = kind;
            Assert.AreEqual("Ask about the trip >", Build(c).Node(InterviewScript.HubNodeId).Choices.First(x => x.Id == "ask").Label, kind.ToString());
        }
    }

    [Test]
    public void Build_ADisplacedTravellerAskedForAManifestSaysTheirHonestLine()
    {
        DialogChoice manifest = Build(Day5(TravellerKind.Displaced, DisplacedForms()), lines: Day5Lines()).Node(InterviewScript.PapersNodeId).Choices
            .First(x => x.Id == "request:TC-230");

        Assert.AreEqual(DialogAction.None, manifest.Action, "no hand-over: they carry none");
        CollectionAssert.AreEqual(new[] { "interview.requestPrompt", "interview.missingFormReplies.Displaced.TC-230.Honest" }, LineIds(manifest.Lines));
        Assert.AreEqual("Your Departure Manifest, please.", manifest.Lines[0].Text, "the desk's words are everyone's");
        Assert.AreEqual("A manifest? I was pulled out of my own time. I didn't pack.", manifest.Lines[1].Text);
        Assert.AreEqual(DialogSpeaker.Traveller, manifest.Lines[1].Speaker);

        DialogChoice order = Build(Day5(TravellerKind.Displaced, DisplacedForms()), lines: Day5Lines()).Node(InterviewScript.PapersNodeId).Choices.First(x => x.Id == "request:TC-630");
        Assert.AreEqual((DialogAction.HandOverDocument, 2), (order.Action, order.DocumentIndex), "the return order they carry is handed over");
    }

    [Test]
    public void Build_ACitizenAskedForAReturnOrderSaysTheirHonestLine()
    {
        foreach ((TravellerKind kind, CaseDocument[] papers) in new[] { (TravellerKind.RichTourist, RichForms()), (TravellerKind.PoorTourist, PoorForms()), (TravellerKind.Labourer, LabourerForms()) })
        {
            DialogChoice order = Build(Day5(kind, papers), lines: Day5Lines()).Node(InterviewScript.PapersNodeId).Choices.First(x => x.Id == "request:TC-630");
            Assert.AreEqual(DialogAction.None, order.Action, kind.ToString());
            Assert.AreEqual("A return order? I have a return booking. Is that the same thing?", order.Lines[1].Text, kind.ToString());
        }
    }

    [Test]
    public void Build_AMissingVariantAskedForAFormTheKindNeverCarries_SaysTheHonestLine()
    {
        InterviewCase poor = Day5(TravellerKind.PoorTourist, PoorForms().Take(3).ToArray(), MissingFormVariant.Missing);
        DialogNode papers = Build(poor, lines: Day5Lines()).Node(InterviewScript.PapersNodeId);

        Assert.AreEqual("I... didn't get round to that one.", papers.Choices.First(x => x.Id == "request:proof").Lines[1].Text, "the proof they left out: their Missing line");
        Assert.AreEqual("An intake declaration? I'm leaving, not arriving.", papers.Choices.First(x => x.Id == "request:TC-620").Lines[1].Text,
                        "no Missing line for a form their kind never carries: the Honest one");
    }

    [Test]
    public void MenuProblems_FiveRequestsFitThePapersMenu()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(6, true, 5, spokenRequests: 2, dialogs: 2, premadeDialogs: 1, maxChoices: 9),
                                 "< Back and five requests is six of the wheel's nine; the hub still counts the papers menu once");
        StringAssert.Contains("The papers menu holds 9 choices", string.Join("\n", DialogChecks.MenuProblems(6, true, 8, 2, 2, 1, 8)));
    }

    [Test]
    public void APromptWithThePlaceToken_NamesTheClaimedPlace()
    {
        InterviewQuestion trip = Question("q_trip_currency", ClueCategory.Currency, "Currency", "I've changed my money into {value}.");
        trip.prompt = new LineText("q_trip_currency.prompt", "What will you pay with in {place}?");
        DialogChoice choice = Build(questions: new[] { trip }).Node(InterviewScript.AskNodeId).Choices[1];
        Assert.AreEqual("What will you pay with in New Kingdom Egypt (Ancient)?", choice.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, choice.Lines[0].Speaker);
        Assert.AreEqual("I've changed my money into Deben.", choice.Lines[1].Text);
        Assert.AreEqual("About Capital?", Build().Node(InterviewScript.AskNodeId).Choices[2].Lines[0].Text, "a prompt without the token is as it is");
    }

    private static string[] Ids(IEnumerable<DialogChoice> choices) => choices.Select(c => c.Id).ToArray();

    private static string[] LineIds(IEnumerable<DialogLine> lines) => lines.Select(l => l.Id).ToArray();

    // -----------------------------
    // Hub and ask menu
    // -----------------------------

    [Test]
    public void Hub_ThePapersSubMenu_ThenAsk_ThenOneEntryPerDialog()
    {
        DialogNode hub = Build().Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "papers", "ask", "dlg:dlg_rumour" }, Ids(hub.Choices));
        CollectionAssert.AreEqual(new[] { "Request papers >", "Ask about the trip >", "Any news from home? >" },
                                  hub.Choices.Select(c => c.Label).ToArray());

        DialogChoice papers = hub.Choices[0];
        Assert.AreEqual(InterviewScript.PapersNodeId, papers.Next);
        Assert.AreEqual(DialogChoiceKind.Request, papers.Kind, "a sub-menu entry takes the kind of what it opens");
        Assert.IsFalse(papers.OneShot, "the menu stays reachable, as the ask menu does");
        CollectionAssert.IsEmpty(papers.Lines);

        DialogChoice ask = hub.Choices[1];
        Assert.AreEqual(InterviewScript.AskNodeId, ask.Next);
        CollectionAssert.IsEmpty(ask.Lines);

        DialogChoice dialog = hub.Choices[2];
        Assert.AreEqual("dlg_rumour/start", dialog.Next);
        Assert.IsTrue(dialog.OneShot);
    }

    [Test]
    public void Papers_BackFirst_ThenOneRequestPerDocumentOnRequest_InPaperOrder_NamedByTheForm()
    {
        DialogNode papers = Build().Node(InterviewScript.PapersNodeId);
        CollectionAssert.AreEqual(new[] { "back", "request:TC-620", "request:TC-630" }, Ids(papers.Choices), "the certificate came on arrival");
        CollectionAssert.AreEqual(new[] { "< Back", "Intake Declaration", "Return Order" }, papers.Choices.Select(c => c.Label).ToArray());
        Assert.AreEqual(DialogChoiceKind.Back, papers.Choices[0].Kind);
        Assert.AreEqual(InterviewScript.HubNodeId, papers.Choices[0].Next);
        Assert.IsFalse(papers.Choices[0].OneShot);
        CollectionAssert.AreEqual(new[] { 1, 2 }, papers.Choices.Skip(1).Select(c => c.DocumentIndex).ToArray(), "each index stays the paper's place in the case");
    }

    [Test]
    public void Papers_HoldsBackAndEveryRequest_StillOneHubEntry()
    {
        InterviewCase c = Case(documents: new[] { Doc("Leisure Departure Visa", DocumentHandOver.OnArrival), Doc("Departure Manifest"), Doc("Stranding Waiver"), Doc("Proof of Funds") });
        DialogNode papers = Build(c).Node(InterviewScript.PapersNodeId);
        Assert.AreEqual(4, papers.Choices.Count, "< Back + 3: the most one traveller is asked for (the spec's poor tourist)");
        CollectionAssert.AreEqual(new[] { "papers", "ask", "dlg:dlg_rumour" }, Ids(Build(c).Node(InterviewScript.HubNodeId).Choices), "still one hub entry");
    }

    // -----------------------------
    // Request groups and missing forms (traveller types I2, phase 8)
    // -----------------------------

    [Test]
    public void Papers_APoorTourist_TheProofGroupIsOneEntry_NamedByTheGroup_HandingOverTheHeldProof()
    {
        DialogNode papers = Build(Citizen(TravellerKind.PoorTourist, PoorForms()), lines: LinesWithGroups()).Node(InterviewScript.PapersNodeId);
        CollectionAssert.AreEqual(new[] { "back", "request:TC-230", "request:TC-310", "request:proof" }, Ids(papers.Choices));
        CollectionAssert.AreEqual(new[] { "< Back", "Departure Manifest", "Stranding Waiver", "Proof of means" }, papers.Choices.Select(c => c.Label).ToArray());
        DialogChoice proof = papers.Choices[3];
        Assert.AreEqual(DialogAction.HandOverDocument, proof.Action);
        Assert.AreEqual(3, proof.DocumentIndex, "the Proof of Funds the traveller holds");
        Assert.AreEqual("Your Proof of means, please.", proof.Lines[0].Text, "the desk asks for the group");
        Assert.AreEqual("Here you are.", proof.Lines[1].Text);
    }

    [Test]
    public void Papers_ARichTourist_AskedForAWaiverOrAProof_AnswersWithTheirHonestLine_OneShot_NoHandOver()
    {
        DialogGraph graph = Build(Citizen(TravellerKind.RichTourist, RichForms()), lines: LinesWithGroups());
        DialogNode papers = graph.Node(InterviewScript.PapersNodeId);
        CollectionAssert.AreEqual(new[] { "back", "request:TC-230", "request:TC-310", "request:proof" }, Ids(papers.Choices), "the three requests of days 1-4, whatever they carry");
        CollectionAssert.AreEqual(new[] { "< Back", "Departure Manifest", "Stranding Waiver", "Proof of means" }, papers.Choices.Select(c => c.Label).ToArray());

        DialogChoice waiver = papers.Choices[2];
        Assert.AreEqual(DialogAction.None, waiver.Action);
        Assert.AreEqual(-1, waiver.DocumentIndex);
        Assert.IsTrue(waiver.OneShot);
        Assert.AreEqual(DialogChoiceKind.Request, waiver.Kind);
        Assert.IsTrue(string.IsNullOrEmpty(waiver.Next), "stays in the papers menu");
        CollectionAssert.AreEqual(new[] { "interview.requestPrompt", "interview.missingFormReplies.RichTourist.TC-310.Honest" }, LineIds(waiver.Lines));
        Assert.AreEqual("Your Stranding Waiver, please.", waiver.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, waiver.Lines[0].Speaker);
        Assert.AreEqual("It's a Premium unit, I don't need one.", waiver.Lines[1].Text);
        Assert.AreEqual(DialogSpeaker.Traveller, waiver.Lines[1].Speaker);
        Assert.AreEqual("I pay my own way.", papers.Choices[3].Lines[1].Text);

        var runner = new DialogRunner(graph, InterviewScript.Opening(LinesWithGroups(), Case()));
        Assert.IsNotNull(runner.Choose("papers"));
        Assert.IsNotNull(runner.Choose("request:TC-310"));
        CollectionAssert.AreEqual(new[] { "back", "request:TC-230", "request:proof" }, Ids(runner.Choices), "asked once");
        Assert.IsNull(runner.Choose("request:TC-310"));
    }

    [Test]
    public void Papers_AMissingFormsReply_FollowsTheCasesVariant_AndIsOnlyThePromptWhenNoneIsAuthored()
    {
        InterviewCase poor = Citizen(TravellerKind.PoorTourist, PoorForms().Take(3).ToArray(), MissingFormVariant.Missing);
        DialogNode papers = Build(poor, lines: LinesWithGroups()).Node(InterviewScript.PapersNodeId);
        DialogChoice proof = papers.Choices[3];
        Assert.AreEqual("request:proof", proof.Id);
        Assert.AreEqual("I... didn't get round to that one.", proof.Lines[1].Text, "a poor tourist who left their proof out (phase 9's paper-set fault)");

        InterviewCase labourer = Citizen(TravellerKind.Labourer, RichForms());
        DialogChoice unauthored = Build(labourer, lines: LinesWithGroups()).Node(InterviewScript.PapersNodeId).Choices[2];
        CollectionAssert.AreEqual(new[] { "interview.requestPrompt" }, LineIds(unauthored.Lines), "no line authored for a labourer: the desk's prompt only (the validator demands the line)");
    }

    [Test]
    public void Hub_OneAskableRequest_IsADirectEntry_EvenWhenNotCarried()
    {
        InterviewCase c = Citizen(TravellerKind.RichTourist, new[] { Doc("Leisure Departure Visa", DocumentHandOver.OnArrival, "TC-101") });
        c.askable = new[] { new AskableForm("TC-310", "Stranding Waiver", "", true) };
        DialogNode hub = Build(c, lines: LinesWithGroups()).Node(InterviewScript.HubNodeId);
        Assert.AreEqual("request:TC-310", hub.Choices[0].Id);
        Assert.AreEqual("Request Stranding Waiver", hub.Choices[0].Label);
    }

    [Test]
    public void TheWheelsWorstCase_StaysNine_WithThreeRequestsForACitizen()
    {
        int requests = FormRequests.Count(CitizenAskable());
        Assert.AreEqual(3, requests, "Manifest, Waiver, Proof of means");
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(6, true, requests, spokenRequests: 2, dialogs: 2, premadeDialogs: 1, maxChoices: 9),
                                 "the papers menu + 2 spoken requests + ask + look + the differences entry (wave 5) + 2 dialogs + one premade's dialog = 9");
    }

    [Test]
    public void Request_SpeaksPromptAndReply_HandsItsDocumentOver_AndIsOneShot()
    {
        DialogChoice order = Build().Node(InterviewScript.PapersNodeId).Choices[2];
        Assert.AreEqual(DialogAction.HandOverDocument, order.Action);
        Assert.AreEqual(2, order.DocumentIndex);
        Assert.AreEqual(DialogChoiceKind.Request, order.Kind);
        Assert.IsTrue(order.OneShot, "a paper once handed over never goes back mid-case");
        Assert.IsTrue(string.IsNullOrEmpty(order.Next), "stays in the papers menu");
        CollectionAssert.AreEqual(new[] { "interview.requestPrompt", "interview.requestReply" }, LineIds(order.Lines));
        Assert.AreEqual("Your Return Order, please.", order.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, order.Lines[0].Speaker);
        Assert.AreEqual("Here you are.", order.Lines[1].Text);
        Assert.AreEqual(DialogSpeaker.Traveller, order.Lines[1].Speaker);
    }

    [Test]
    public void ARequest_LeavesThePapersMenuOnceChosen_AndBackReturnsToTheHub()
    {
        var runner = new DialogRunner(Build(), InterviewScript.Opening(Lines(), Case()));
        Assert.IsNotNull(runner.Choose("papers"));
        Assert.IsNotNull(runner.Choose("request:TC-630"));
        CollectionAssert.AreEqual(new[] { "back", "request:TC-620" }, Ids(runner.Choices), "still in the papers menu");
        Assert.IsNull(runner.Choose("request:TC-630"), "it cannot be chosen twice");
        Assert.IsNotNull(runner.Choose("request:TC-620"));
        CollectionAssert.AreEqual(new[] { "back" }, Ids(runner.Choices), "every paper handed over: only the way back");
        Assert.IsNotNull(runner.Choose("back"));
        CollectionAssert.AreEqual(new[] { "papers", "ask", "dlg:dlg_rumour" }, Ids(runner.Choices));
    }

    [Test]
    public void Hub_OneDocumentOnRequest_IsADirectRequest_KeepingThePaperIndex()
    {
        InterviewCase c = Case(documents: new[] { Doc("Displacement Certificate", DocumentHandOver.OnArrival, "TC-610"), Doc("Intake Declaration", formNumber: "TC-620") });
        DialogGraph graph = Build(c);
        DialogNode hub = graph.Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "request:TC-620", "ask", "dlg:dlg_rumour" }, Ids(hub.Choices), "no sub-menu for one form");
        Assert.AreEqual("Request Intake Declaration", hub.Choices[0].Label);
        Assert.AreEqual(1, hub.Choices[0].DocumentIndex, "the index stays the paper's place in the case");
        Assert.AreEqual(DialogChoiceKind.Request, hub.Choices[0].Kind);
        CollectionAssert.AreEqual(new[] { "back" }, Ids(graph.Node(InterviewScript.PapersNodeId).Choices), "the papers menu holds no request");
    }

    [Test]
    public void Hub_HasNoRequest_WhenEveryDocumentIsHandedOverOnArrival()
    {
        InterviewCase c = Case(documents: new[] { Doc("Displacement Certificate", DocumentHandOver.OnArrival), Doc("Intake Declaration", DocumentHandOver.OnArrival) });
        CollectionAssert.AreEqual(new[] { "ask", "dlg:dlg_rumour" }, Ids(Build(c).Node(InterviewScript.HubNodeId).Choices));
    }

    [Test]
    public void EveryChoice_HasItsKind_ASubMenuEntryTakingTheKindOfWhatItOpens()
    {
        DialogGraph graph = Build(Dressed(new Garment(LookSlot.Hair, "Caesar crop", "Caesar crop / nodus roll", false)), lines: LinesWithRequests());
        var expected = new Dictionary<string, DialogChoiceKind>
        {
            ["papers"] = DialogChoiceKind.Request, ["request:TC-620"] = DialogChoiceKind.Request, ["request:TC-630"] = DialogChoiceKind.Request,
            ["act:step_closer"] = DialogChoiceKind.Request, ["act:speak_up"] = DialogChoiceKind.Request,
            ["ask"] = DialogChoiceKind.Question, ["look"] = DialogChoiceKind.Look, ["dlg:dlg_rumour"] = DialogChoiceKind.Dialog,
            ["back"] = DialogChoiceKind.Back, ["q:q_currency"] = DialogChoiceKind.Question, ["q:q_capital"] = DialogChoiceKind.Question,
            ["smalltalk"] = DialogChoiceKind.Question, ["look:0"] = DialogChoiceKind.Look,
            ["dlg_rumour.more"] = DialogChoiceKind.Normal, ["dlg_rumour.ignore"] = DialogChoiceKind.Normal, ["dlg_rumour.noted"] = DialogChoiceKind.Normal
        };

        var seen = new HashSet<string>();
        foreach (string node in new[] { InterviewScript.HubNodeId, InterviewScript.PapersNodeId, InterviewScript.AskNodeId, InterviewScript.LookNodeId, "dlg_rumour/start", "dlg_rumour/detail" })
        {
            foreach (DialogChoice choice in graph.Node(node).Choices)
            {
                Assert.AreEqual(expected[choice.Id], choice.Kind, $"{node}: {choice.Id}");
                seen.Add(choice.Id);
            }
        }

        CollectionAssert.AreEquivalent(expected.Keys, seen, "every kind of choice was built");
    }

    [Test]
    public void Hub_SpokenRequests_FollowTheDocumentRequests_BeforeAsk()
    {
        DialogNode hub = Build(lines: LinesWithRequests()).Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "papers", "act:step_closer", "act:speak_up", "ask", "dlg:dlg_rumour" }, Ids(hub.Choices));
        CollectionAssert.AreEqual(new[] { "Step closer", "Speak up" }, hub.Choices.Skip(1).Take(2).Select(c => c.Label).ToArray());
    }

    [Test]
    public void ASpokenRequest_SpeaksPromptAndReply_IsOneShot_AndDoesNothingElse()
    {
        DialogChoice closer = Build(lines: LinesWithRequests()).Node(InterviewScript.HubNodeId).Choices[1];
        Assert.AreEqual(DialogAction.None, closer.Action, "no mechanic: the traveller only answers");
        Assert.AreEqual(-1, closer.DocumentIndex);
        Assert.IsTrue(closer.OneShot);
        Assert.IsTrue(string.IsNullOrEmpty(closer.Next), "stays on the hub");
        CollectionAssert.AreEqual(new[] { "interview.requests.step_closer.prompt", "interview.requests.step_closer.reply" }, LineIds(closer.Lines));
        Assert.AreEqual(DialogSpeaker.Desk, closer.Lines[0].Speaker);
        Assert.AreEqual("Step closer to the glass, please.", closer.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Traveller, closer.Lines[1].Speaker);
        Assert.AreEqual("Like this?", closer.Lines[1].Text);
        Assert.IsFalse(closer.Lines[1].IsAnswer, "a reply to a request is never evidence");

        var runner = new DialogRunner(Build(lines: LinesWithRequests()), null);
        Assert.IsNotNull(runner.Choose("act:step_closer"));
        CollectionAssert.DoesNotContain(Ids(runner.Choices), "act:step_closer", "asked once per traveller");
        CollectionAssert.Contains(Ids(runner.Choices), "act:speak_up");
    }

    [Test]
    public void Hub_HasNoSpokenRequest_WithoutAny_AndSkipsEmptyEntries()
    {
        CollectionAssert.IsEmpty(Ids(Build().Node(InterviewScript.HubNodeId).Choices).Where(id => id.StartsWith("act:")));

        InterviewLines lines = LinesWithRequests();
        lines.requests.Insert(0, null);
        CollectionAssert.AreEqual(new[] { "act:step_closer", "act:speak_up" },
                                  Ids(Build(lines: lines).Node(InterviewScript.HubNodeId).Choices).Where(id => id.StartsWith("act:")).ToArray());

        InterviewLines none = Lines();
        none.requests = null;
        CollectionAssert.IsEmpty(Ids(Build(lines: none).Node(InterviewScript.HubNodeId).Choices).Where(id => id.StartsWith("act:")));
    }

    [Test]
    public void Hub_HasNoAskEntry_WithoutQuestionsOrSmallTalk()
    {
        DialogNode hub = Build(Case(smallTalk: false), new InterviewQuestion[0], new AuthoredDialog[0]).Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "papers" }, Ids(hub.Choices));
    }

    [Test]
    public void Ask_BackFirst_ThenAnsweredQuestionsInOrder_ThenSmallTalk()
    {
        DialogNode ask = Build().Node(InterviewScript.AskNodeId);
        CollectionAssert.AreEqual(new[] { "back", "q:q_currency", "q:q_capital", "smalltalk" }, Ids(ask.Choices), "q_ruler has no answer, so it is skipped");
        CollectionAssert.AreEqual(new[] { "< Back", "Currency", "Capital", "Small talk" }, ask.Choices.Select(c => c.Label).ToArray());

        Assert.AreEqual(InterviewScript.HubNodeId, ask.Choices[0].Next);
        Assert.IsFalse(ask.Choices[0].OneShot);
        Assert.IsTrue(ask.Choices.Skip(1).All(c => c.OneShot && string.IsNullOrEmpty(c.Next)));

        DialogChoice smallTalk = ask.Choices[3];
        CollectionAssert.AreEqual(new[] { "interview.smallTalkPrompt", "egypt_ancient.smalltalk.1" }, LineIds(smallTalk.Lines));
        Assert.AreEqual(DialogSpeaker.Traveller, smallTalk.Lines[1].Speaker);
        Assert.IsFalse(smallTalk.Lines[1].IsAnswer, "small talk is never evidence");
    }

    [Test]
    public void Ask_OffersNoSmallTalk_WithoutALine()
    {
        DialogNode ask = Build(Case(smallTalk: false)).Node(InterviewScript.AskNodeId);
        CollectionAssert.AreEqual(new[] { "back", "q:q_currency", "q:q_capital" }, Ids(ask.Choices));
    }

    [Test]
    public void AnswerLines_CarryTheFact_AndTheKindsAndErasWordingWins()
    {
        DialogNode ask = Build().Node(InterviewScript.AskNodeId);

        DialogChoice currency = ask.Choices[1];
        CollectionAssert.AreEqual(new[] { "q_currency.prompt", "q_currency.overrides.1.answer" }, LineIds(currency.Lines), "the desk's words are the question's own; the answer the override's");
        Assert.AreEqual("About Currency?", currency.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, currency.Lines[0].Speaker);
        Assert.AreEqual("We trade with Deben.", currency.Lines[1].Text);
        Assert.IsTrue(currency.Lines[1].IsAnswer);
        Assert.AreEqual(ClueCategory.Currency, currency.Lines[1].Category);
        Assert.AreEqual("Deben", currency.Lines[1].Value);
        Assert.IsFalse(currency.Lines[1].IsTell);

        DialogLine capital = ask.Choices[2].Lines[1];
        Assert.AreEqual("q_capital.answer", capital.Id);
        Assert.AreEqual("Our capital is Babylon.", capital.Text);
        Assert.AreEqual("Babylon", capital.Value);
        Assert.IsTrue(capital.IsTell);

        InterviewCase medieval = Case();
        medieval.claimedEraId = "medieval";
        Assert.AreEqual("We pay in Deben.", Build(medieval).Node(InterviewScript.AskNodeId).Choices[1].Lines[1].Text, "no override for this era");

        InterviewCase tourist = Case();
        tourist.kind = TravellerKind.RichTourist;
        Assert.AreEqual("We pay in Deben.", Build(tourist).Node(InterviewScript.AskNodeId).Choices[1].Lines[1].Text, "the displaced's override is not a tourist's");
    }

    [Test]
    public void Opening_IsTheIntroThenTheClaim_OrTheClaimAloneWhenTheIntroIsBlank()
    {
        IReadOnlyList<DialogLine> both = InterviewScript.Opening(Lines(), Case());
        CollectionAssert.AreEqual(new[] { "case.intro", "case.claim" }, LineIds(both));
        Assert.AreEqual(DialogSpeaker.Desk, both[0].Speaker);
        Assert.AreEqual(DialogSpeaker.Traveller, both[1].Speaker);
        Assert.AreEqual("Please. Send me home to New Kingdom Egypt (Ancient).", both[1].Text, "the displaced's claim line");

        CollectionAssert.AreEqual(new[] { "case.claim" }, LineIds(InterviewScript.Opening(Lines(), Case(intro: " "))));
    }

    [Test]
    public void Opening_TheClaimIsFilledFromItsTemplate_OrIsThePlaceAloneWhenBlank()
    {
        InterviewLines blank = Lines();
        blank.claims = null;
        Assert.AreEqual("New Kingdom Egypt (Ancient)", InterviewScript.Opening(blank, Case())[1].Text);
        Assert.AreEqual(InterviewScript.Claim(Lines(), Case()), InterviewScript.Opening(Lines(), Case())[1].Text, "the claim's one text");

        InterviewCase tourist = Case();
        tourist.kind = TravellerKind.RichTourist;
        Assert.AreEqual("New Kingdom Egypt (Ancient)", InterviewScript.Opening(Lines(), tourist)[1].Text, "a kind with no claim line says the place alone");
    }

    /// <summary>The English parts of a line (its key-word spans), joined by "|".</summary>
    private static string English(DialogLine line) =>
        string.Join("|", line.English.Select(s => line.Text.Substring(s.start, s.length)));

    [Test]
    public void TheClaim_KeepsHomeAndThePlaceEnglish_TheDesksOpenerHasNoSpans()
    {
        IReadOnlyList<DialogLine> opening = InterviewScript.Opening(Lines(), Case(keyWords: KeyWordRule()));
        Assert.AreEqual("Please|home|New Kingdom Egypt (Ancient)", English(opening[1]));
        CollectionAssert.IsEmpty(opening[0].English, "the desk speaks English: nothing to keep");
    }

    [Test]
    public void AnAnswer_KeepsItsValueInTheTongue_ButItsDigitsAndKeyWords()
    {
        InterviewCase c = Case(keyWords: KeyWordRule());
        c.answers = new[] { new InterviewAnswer { category = ClueCategory.Currency, value = "No coin: 1000 deben of copper", isTell = false } };
        DialogLine answer = Build(c).Node(InterviewScript.AskNodeId).Choices[1].Lines[1];
        Assert.AreEqual("We trade with No coin: 1000 deben of copper.", answer.Text);
        Assert.AreEqual("No|1000", English(answer), "{value} is not a key slot; a listed word and digits inside it still are");
    }

    [Test]
    public void EveryOtherTravellerLine_CarriesItsSpans()
    {
        InterviewCase c = Case(keyWords: KeyWordRule());
        c.smallTalk = new LineText("egypt_ancient.smalltalk.1", "Yes, the Nile rose on time back home.");
        DialogGraph graph = Build(c, lines: LinesWithRequests());
        DialogNode hub = graph.Node(InterviewScript.HubNodeId);

        DialogNode papers = graph.Node(InterviewScript.PapersNodeId);
        DialogLine requestReply = papers.Choices.First(ch => ch.Id == "request:TC-620").Lines[1];
        Assert.AreEqual(DialogSpeaker.Traveller, requestReply.Speaker);
        CollectionAssert.IsEmpty(requestReply.English, "\"Here you are.\" holds no key word");
        CollectionAssert.IsEmpty(papers.Choices.First(ch => ch.Id == "request:TC-620").Lines[0].English, "the desk's prompt");
        CollectionAssert.IsEmpty(hub.Choices.First(ch => ch.Id == "act:step_closer").Lines[1].English, "\"Like this?\" holds no key word");

        DialogLine smallTalk = graph.Node(InterviewScript.AskNodeId).Choices.First(ch => ch.Id == "smalltalk").Lines[1];
        Assert.AreEqual("Yes|home", English(smallTalk));

        DialogLine rumour = graph.Node("dlg_rumour/start").Lines[0];
        Assert.AreEqual(DialogSpeaker.Traveller, rumour.Speaker);
        CollectionAssert.IsEmpty(rumour.English, "\"News? Only a rumour.\" holds no key word");
    }

    [Test]
    public void WithoutAKeyWordRule_NoLineKeepsAnything()
    {
        DialogLine claim = InterviewScript.Opening(Lines(), Case())[1];
        Assert.IsNotNull(claim.English);
        CollectionAssert.IsEmpty(claim.English);
        CollectionAssert.IsEmpty(Build().Node(InterviewScript.AskNodeId).Choices[1].Lines[1].English);
    }

    [Test]
    public void TheRuntimeLineIds_HaveOneHome_ThatGenerateWorldReservesAndChecks()
    {
        Assert.AreEqual("case.intro", InterviewScript.IntroLineId);
        Assert.AreEqual("case.claim", InterviewScript.ClaimLineId);
        CollectionAssert.AreEqual(new[] { InterviewScript.IntroLineId, InterviewScript.ClaimLineId }, LineIds(InterviewScript.Opening(Lines(), Case())));

        Assert.AreEqual("dlg_rumour.more", InterviewScript.ChoiceLineId("dlg_rumour", "more"));
        DialogChoice more = Build().Node("dlg_rumour/start").Choices[0];
        Assert.AreEqual(InterviewScript.ChoiceLineId("dlg_rumour", "more"), more.Id, "the runtime choice id");
        Assert.AreEqual(InterviewScript.ChoiceLineId("dlg_rumour", "more"), more.Lines[0].Id, "the label line the desk speaks");
    }

    // -----------------------------
    // The traveller's reply (SaidSince)
    // -----------------------------

    private static readonly DialogLine[] Said3 =
    {
        new DialogLine("case.intro", DialogSpeaker.Desk, "Next!"),
        new DialogLine("case.claim", DialogSpeaker.Traveller, "I request passage home."),
        new DialogLine("q.prompt", DialogSpeaker.Desk, "What do you trade with?"),
        new DialogLine("q.answer", DialogSpeaker.Traveller, "We trade with Deben."),
        new DialogLine("dlg.more", DialogSpeaker.Desk, "Tell me more."),
        new DialogLine("dlg.1", DialogSpeaker.Traveller, "Only a rumour."),
        new DialogLine("dlg.2", DialogSpeaker.Traveller, "A courier carries them.")
    };

    [Test]
    public void SaidSince_IsTheTravellersLines_InOrder_SkippingTheDesk()
    {
        CollectionAssert.AreEqual(new[] { "dlg.1", "dlg.2" }, LineIds(InterviewScript.SaidSince(Said3, 4)));
        CollectionAssert.AreEqual(new[] { "q.answer", "dlg.1", "dlg.2" }, LineIds(InterviewScript.SaidSince(Said3, 2)));
        Assert.AreSame(Said3[3], InterviewScript.SaidSince(Said3, 2)[0], "the lines themselves, with their expressions");
    }

    [Test]
    public void SaidSince_LeavesOutLinesBeforeFrom_AndCountsANegativeFromAsZero()
    {
        CollectionAssert.AreEqual(new[] { "dlg.2" }, LineIds(InterviewScript.SaidSince(Said3, 6)));
        CollectionAssert.AreEqual(new[] { "case.claim", "q.answer", "dlg.1", "dlg.2" }, LineIds(InterviewScript.SaidSince(Said3, -3)));
    }

    [Test]
    public void SaidSince_IsEmpty_AtOrPastTheEnd_ForDeskLinesOnly_AndForNoTranscript()
    {
        CollectionAssert.IsEmpty(InterviewScript.SaidSince(Said3, 7));
        CollectionAssert.IsEmpty(InterviewScript.SaidSince(Said3, 99));
        CollectionAssert.IsEmpty(InterviewScript.SaidSince(new[] { Said3[0], null, Said3[2] }, 0));
        CollectionAssert.IsEmpty(InterviewScript.SaidSince(null, 0));
    }

    [Test]
    public void SaidSince_AfterARequest_IsTheTravellersReply_AndFromTheStart_IsTheClaim()
    {
        var runner = new DialogRunner(Build(), InterviewScript.Opening(Lines(), Case()));
        CollectionAssert.AreEqual(new[] { InterviewScript.ClaimLineId }, LineIds(InterviewScript.SaidSince(runner.Transcript, 0)), "what the traveller says on arrival");

        runner.Choose("papers");
        int before = runner.Transcript.Count;
        runner.Choose("request:TC-620");
        Assert.AreEqual("Here you are.", InterviewScript.SaidSince(runner.Transcript, before).Single().Text);
    }

    // -----------------------------
    // Authored dialogs
    // -----------------------------

    [Test]
    public void AnAuthoredDialog_IsMerged_WithNamespacedIds_AndTheDeskSpeakingLabels()
    {
        DialogGraph graph = Build();
        DialogNode start = graph.Node("dlg_rumour/start");
        CollectionAssert.AreEqual(new[] { "dlg_rumour.start.1" }, LineIds(start.Lines));
        CollectionAssert.AreEqual(new[] { "dlg_rumour.more", "dlg_rumour.ignore" }, Ids(start.Choices));

        DialogChoice more = start.Choices[0];
        Assert.AreEqual("dlg_rumour/detail", more.Next);
        Assert.AreEqual(DialogAction.None, more.Action);
        Assert.AreEqual("dlg_rumour.more", more.Lines[0].Id);
        Assert.AreEqual("Tell me more.", more.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, more.Lines[0].Speaker);

        DialogChoice ignore = start.Choices[1];
        Assert.AreEqual(InterviewScript.HubNodeId, ignore.Next);
        Assert.AreEqual(DialogAction.CompleteDialog, ignore.Action);
        Assert.AreEqual("dlg_rumour", ignore.DialogId);
        Assert.IsTrue(string.IsNullOrEmpty(ignore.EffectName));

        DialogChoice noted = graph.Node("dlg_rumour/detail").Choices[0];
        Assert.AreEqual(DialogAction.CompleteDialog, noted.Action);
        Assert.AreEqual("Effect_Dialog_RumourHeard", noted.EffectName);
    }

    [Test]
    public void AFullWalkThroughTheRumour_EndsBackAtTheHub()
    {
        var runner = new DialogRunner(Build(), InterviewScript.Opening(Lines(), Case()));
        Assert.IsNotNull(runner.Choose("dlg:dlg_rumour"));
        Assert.IsNotNull(runner.Choose("dlg_rumour.more"));
        DialogChoice last = runner.Choose("dlg_rumour.noted");

        Assert.AreEqual(DialogAction.CompleteDialog, last.Action);
        CollectionAssert.AreEqual(new[] { "papers", "ask" }, Ids(runner.Choices), "back at the hub, the dialog entry used");
        CollectionAssert.AreEqual(
            new[] { "case.intro", "case.claim", "dlg_rumour.start.1", "dlg_rumour.more", "dlg_rumour.detail.1", "dlg_rumour.noted" },
            LineIds(runner.Transcript));
    }

    // -----------------------------
    // DialogChecks.Problems
    // -----------------------------

    private static string Only(List<string> problems, string expected)
    {
        Assert.AreEqual(1, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains(expected, problems[0]);
        return problems[0];
    }

    [Test]
    public void Problems_ASoundDialogHasNone()
    {
        CollectionAssert.IsEmpty(DialogChecks.Problems(Rumour(), 8));
    }

    [Test]
    public void Problems_NoNodes()
    {
        Only(DialogChecks.Problems(new AuthoredDialog { id = "x" }, 8), "no nodes");
        Only(DialogChecks.Problems(null, 8), "no nodes");
    }

    [Test]
    public void Problems_DuplicateNodeAndChoiceIds()
    {
        AuthoredDialog d = Rumour();
        d.nodes.Add(new ScriptNode { id = "detail", choices = { Reply("bye", "Bye.") } });
        Only(DialogChecks.Problems(d, 8), "node 'detail' is listed twice");

        AuthoredDialog c = Rumour();
        c.nodes[1].choices.Add(Reply("more", "More?"));
        Only(DialogChecks.Problems(c, 8), "choice 'more' is listed twice");
    }

    [Test]
    public void Problems_ANextThatNamesNoNode()
    {
        AuthoredDialog d = Rumour();
        d.nodes[0].choices[0].next = "detial";
        List<string> problems = DialogChecks.Problems(d, 8);
        Assert.IsTrue(problems.Any(p => p.Contains("choice 'more' leads to unknown node 'detial'")), string.Join(" | ", problems));
        Assert.IsTrue(problems.Any(p => p.Contains("node 'detail' cannot be reached")), string.Join(" | ", problems));
    }

    [Test]
    public void Problems_ANodeUnreachableFromTheStart()
    {
        AuthoredDialog d = Rumour();
        d.nodes.Add(new ScriptNode { id = "orphan", choices = { Reply("bye", "Bye.") } });
        Only(DialogChecks.Problems(d, 8), "node 'orphan' cannot be reached from 'start'");
    }

    [Test]
    public void Problems_ANodeWithoutChoices()
    {
        AuthoredDialog d = Rumour();
        d.nodes[1].choices.Clear();
        List<string> problems = DialogChecks.Problems(d, 8);
        Assert.IsTrue(problems.Any(p => p.Contains("node 'detail' has no choices")), string.Join(" | ", problems));
    }

    [Test]
    public void Problems_NoChoiceEndsTheDialog()
    {
        var d = new AuthoredDialog
        {
            id = "loop",
            nodes =
            {
                new ScriptNode { id = "a", choices = { Reply("to_b", "B", "b") } },
                new ScriptNode { id = "b", choices = { Reply("to_a", "A", "a") } }
            }
        };
        List<string> problems = DialogChecks.Problems(d, 8);
        Assert.IsTrue(problems.Contains("no choice ends the dialog"), string.Join(" | ", problems));
    }

    [Test]
    public void Problems_ALoopThatLeftTheOnlyEndingBehind_CannotReachAnEnding()
    {
        // start -> a -> b -> a; the only ending sits in "exit", a branch the player has left.
        var d = new AuthoredDialog
        {
            id = "trap",
            nodes =
            {
                new ScriptNode { id = "start", choices = { Reply("left", "Left.", "a"), Reply("right", "Right.", "exit") } },
                new ScriptNode { id = "a", choices = { Reply("to_b", "On.", "b") } },
                new ScriptNode { id = "b", choices = { Reply("to_a", "Back.", "a") } },
                new ScriptNode { id = "exit", choices = { Reply("bye", "Bye.") } }
            }
        };
        List<string> problems = DialogChecks.Problems(d, 8);
        CollectionAssert.AreEquivalent(new[] { "node 'a' cannot reach an ending", "node 'b' cannot reach an ending" }, problems);
    }

    [Test]
    public void Problems_AnEffectOnAChoiceThatDoesNotEndTheDialog()
    {
        AuthoredDialog d = Rumour();
        d.nodes[0].choices[0].effect = "Effect_Dialog_RumourHeard";
        Only(DialogChecks.Problems(d, 8), "choice 'more' has an effect but does not end the dialog");
    }

    [Test]
    public void Problems_AnEffectOnARepeatableDialog()
    {
        AuthoredDialog d = Rumour();
        d.oneShot = false;
        Only(DialogChecks.Problems(d, 8), "choice 'noted' has an effect, but the dialog is repeatable");
    }

    [Test]
    public void Problems_ANodeWithMoreChoicesThanTheWheelShows_UnlessTheCapacityIsZero()
    {
        AuthoredDialog d = Rumour();
        Only(DialogChecks.Problems(d, 1), "node 'start' offers 2 choices; the traveller wheel shows at most 1");
        CollectionAssert.IsEmpty(DialogChecks.Problems(d, 0));
    }

    // -----------------------------
    // DialogChecks.MenuProblems
    // -----------------------------

    [Test]
    public void MenuProblems_TheAskMenu_BackPlusQuestionsPlusSmallTalk()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(6, true, 2, 0, 2, 0, 8), "< Back + 6 questions + small talk = 8");
        StringAssert.Contains("The ask menu holds 9 choices", Only(DialogChecks.MenuProblems(7, true, 2, 0, 2, 0, 8), "the traveller wheel shows at most 8"));
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(7, false, 2, 0, 2, 0, 8), "without small talk, 7 questions fit");
    }

    [Test]
    public void MenuProblems_TheHub_PapersPlusAskPlusLookPlusDialogs_PremadeDialogsCountOnce()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, maxRequests: 1, spokenRequests: 0, dialogs: 3, premadeDialogs: 3, maxChoices: 8),
                                 "1 request + ask + look + the differences entry + 3 dialogs + one premade's dialog = 8");
        StringAssert.Contains("The hub holds 9 choices", Only(DialogChecks.MenuProblems(1, false, maxRequests: 1, spokenRequests: 0, dialogs: 4, premadeDialogs: 3, maxChoices: 8), "the traveller wheel shows at most 8"));
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, maxRequests: 1, spokenRequests: 0, dialogs: 4, premadeDialogs: 0, maxChoices: 8), "no premade dialog adds nothing");
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, maxRequests: 0, spokenRequests: 0, dialogs: 5, premadeDialogs: 0, maxChoices: 8), "no request adds nothing");
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(99, true, 99, 99, 99, 99, 0), "no capacity, no check");
    }

    [Test]
    public void MenuProblems_TheHub_CountsEverySpokenRequest_AndThePapersMenuAsOneEntry()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, maxRequests: 2, spokenRequests: 2, dialogs: 2, premadeDialogs: 1, maxChoices: 9),
                                 "the displaced: the papers menu + 2 spoken requests + ask + look + differences + 2 dialogs + one premade's dialog = 9");
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, maxRequests: 3, spokenRequests: 2, dialogs: 2, premadeDialogs: 1, maxChoices: 9),
                                 "the spec's worst case: three forms on request still take one hub entry");
        string problem = Only(DialogChecks.MenuProblems(1, false, maxRequests: 2, spokenRequests: 3, dialogs: 2, premadeDialogs: 1, maxChoices: 9),
                              "the traveller wheel shows at most 9");
        StringAssert.Contains("The hub holds 10 choices (the papers menu, 3 spoken request(s), the ask, look and differences entries, 2 dialog(s), one premade's dialog)", problem);
        StringAssert.Contains("(1 document request, 3 spoken request(s)", Only(DialogChecks.MenuProblems(1, false, 1, 3, 2, 1, 8), "the traveller wheel shows at most 8"), "one form on request is a direct entry");
    }

    [Test]
    public void MenuProblems_ThePapersMenu_BackPlusEveryRequest_OnlyWithTwoOrMore()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(0, false, maxRequests: 7, spokenRequests: 0, dialogs: 0, premadeDialogs: 0, maxChoices: 8), "< Back + 7 = 8");
        StringAssert.Contains("The papers menu holds 9 choices (< Back, 8 request(s)); the traveller wheel shows at most 8.",
                              Only(DialogChecks.MenuProblems(0, false, 8, 0, 0, 0, 8), "the traveller wheel shows at most 8"));
        StringAssert.Contains("The papers menu holds 7 choices", Only(DialogChecks.MenuProblems(0, false, 6, 0, 0, 0, 6), "the traveller wheel shows at most 6"));
    }

    [Test]
    public void MenuProblems_TheLookMenu_BackPlusOneChoicePerGarmentSlot()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(0, false, 0, 0, 0, 0, 6), "< Back + 5 slots = 6");
        StringAssert.Contains("The look menu holds up to 6 choices", Only(DialogChecks.MenuProblems(0, false, 0, 0, 0, 0, 5), "the traveller wheel shows at most 5"));
    }

    // -----------------------------
    // The look menu and expressions
    // -----------------------------

    private static InterviewCase Dressed(params Garment[] garments)
    {
        InterviewCase c = Case();
        c.garments = garments;
        return c;
    }

    [Test]
    public void Hub_OffersLook_AfterAsk_BeforeTheDialogs_WhenTheTravellerHasAGarment()
    {
        DialogGraph graph = Build(Dressed(new Garment(LookSlot.Outfit, "pleated linen kilt", "wesekh collar", false),
                                          new Garment(LookSlot.Headwear, "top hat", "top hat / poke bonnet", true)));
        DialogNode hub = graph.Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "papers", "ask", "look", "dlg:dlg_rumour" }, Ids(hub.Choices));
        DialogChoice look = hub.Choices[2];
        Assert.AreEqual("Look >", look.Label);
        Assert.AreEqual(InterviewScript.LookNodeId, look.Next);
        Assert.IsFalse(look.OneShot);
        CollectionAssert.IsEmpty(look.Lines);

        DialogNode menu = graph.Node(InterviewScript.LookNodeId);
        CollectionAssert.AreEqual(new[] { "back", "look:0", "look:1" }, Ids(menu.Choices));
        Assert.AreEqual(DialogChoiceKind.Back, menu.Choices[0].Kind);
        Assert.AreEqual(InterviewScript.HubNodeId, menu.Choices[0].Next);
        CollectionAssert.AreEqual(new[] { "< Back", "pleated linen kilt", "top hat" }, menu.Choices.Select(c => c.Label).ToArray());
        for (int i = 1; i < menu.Choices.Count; i++)
        {
            DialogChoice garment = menu.Choices[i];
            Assert.AreEqual(DialogAction.InspectGarment, garment.Action);
            Assert.AreEqual(i - 1, garment.GarmentIndex);
            Assert.IsFalse(garment.OneShot, "a garment can be looked at again");
            Assert.IsTrue(string.IsNullOrEmpty(garment.Next), "looking stays on the look menu");
            CollectionAssert.IsEmpty(garment.Lines, "looking adds no transcript line");
        }
    }

    [Test]
    public void Hub_HasNoLookEntry_WithoutGarments()
    {
        CollectionAssert.DoesNotContain(Ids(Build(Dressed()).Node(InterviewScript.HubNodeId).Choices), "look");
        CollectionAssert.DoesNotContain(Ids(Build().Node(InterviewScript.HubNodeId).Choices), "look", "no garments listed");
    }

    [Test]
    public void ALookChoice_StaysOffered_AfterItIsChosen()
    {
        var runner = new DialogRunner(Build(Dressed(new Garment(LookSlot.Hair, "Caesar crop", "Caesar crop / nodus roll", false))), null);
        runner.Choose("look");
        Assert.IsNotNull(runner.Choose("look:0"));
        Assert.IsNotNull(runner.Choose("look:0"), "again");
        CollectionAssert.AreEqual(new[] { "back", "look:0" }, Ids(runner.Choices));
    }

    [Test]
    public void Expressions_OfNodeLinesAndChoiceLines_ReachTheRuntimeLines_TheDeskAndAnswersHaveNone()
    {
        var dialog = new AuthoredDialog
        {
            id = "dlg_senenmut",
            label = "Ask about the temple >",
            nodes =
            {
                new ScriptNode
                {
                    id = "start",
                    lines = { new ScriptLine { id = "dlg_senenmut.start.1", speaker = DialogSpeaker.Traveller, text = "Three terraces.", expression = "happy" } },
                    choices =
                    {
                        new ScriptChoice
                        {
                            id = "more", label = "And if she is not?", next = "",
                            lines = { new ScriptLine { id = "dlg_senenmut.more.1", speaker = DialogSpeaker.Traveller, text = "Then my name is chiselled off.", expression = "worried" } }
                        }
                    }
                }
            }
        };

        DialogGraph graph = Build(dialogs: new[] { dialog });
        Assert.AreEqual("happy", graph.Node("dlg_senenmut/start").Lines[0].Expression);
        DialogChoice more = graph.Node("dlg_senenmut/start").Choices[0];
        Assert.IsNull(more.Lines[0].Expression, "the desk's label line");
        Assert.AreEqual("worried", more.Lines[1].Expression);

        DialogNode ask = graph.Node(InterviewScript.AskNodeId);
        Assert.IsTrue(ask.Choices.SelectMany(c => c.Lines).All(l => l.Expression == null), "prompts, answers and small talk carry no expression");
        Assert.IsTrue(graph.Node(InterviewScript.HubNodeId).Choices.SelectMany(c => c.Lines).All(l => l.Expression == null), "requests carry none");
    }

    // ---- The reaction (R1, §6) ----

    private static InterviewLines ReactingLines()
    {
        InterviewLines lines = VoicedLines();
        lines.reactions.Add(new VoiceLine { verdict = ReactionVerdict.Denied, intent = ReactionIntent.Honest, line = new LineText("interview.reactions.1", "But... I did everything right.") });
        lines.voices.reactions.Add(new VoiceLine
        {
            personality = "curt",
            verdict = ReactionVerdict.Denied,
            intent = ReactionIntent.Honest,
            line = new LineText("interview.voices.reactions.curt.1", "Unbelievable. Home, they said."),
            then = new LineText("interview.voices.reactions.curt.1.then", "And I paid for the queue to {place}.")
        });
        return lines;
    }

    [Test]
    public void Reaction_IsOneOrTwoTravellerLinesWithKeyWordSpans()
    {
        IReadOnlyList<DialogLine> said = InterviewScript.Reaction(ReactingLines(), Voiced(Case(keyWords: KeyWordRule())), ReactionVerdict.Denied, ReactionIntent.Honest, "closed");
        Assert.AreEqual(2, said.Count);
        Assert.AreEqual(("interview.voices.reactions.curt.1", DialogSpeaker.Traveller, "Unbelievable. Home, they said."), (said[0].Id, said[0].Speaker, said[0].Text));
        Assert.AreEqual(("interview.voices.reactions.curt.1.then", "And I paid for the queue to New Kingdom Egypt (Ancient)."), (said[1].Id, said[1].Text));
        Assert.AreEqual("Home", English(said[0]));
        Assert.AreEqual("New Kingdom Egypt (Ancient)", English(said[1]), "the place's fill stays English");
        Assert.IsFalse(said[0].IsAnswer || said[1].IsAnswer, "never evidence");
    }

    [Test]
    public void Reaction_NoVoiceSaysTheDefault()
    {
        IReadOnlyList<DialogLine> said = InterviewScript.Reaction(ReactingLines(), Case(), ReactionVerdict.Denied, ReactionIntent.Honest, string.Empty);
        Assert.AreEqual(1, said.Count, "no then line: one line");
        Assert.AreEqual("But... I did everything right.", said[0].Text);
        CollectionAssert.IsEmpty(InterviewScript.Reaction(ReactingLines(), Case(), ReactionVerdict.Accepted, ReactionIntent.Lying, string.Empty), "no row: nothing said");
    }

    // ---- The slip (T10) ----

    private static InterviewCase Slipped(InterviewCase c)
    {
        c.slip = new LineText("interview.slips.4", "Home. Yes. {place}. I say it every morning so I don't forget.");
        return c;
    }

    [Test]
    public void Build_ASlipFollowsTheSmallTalkReply()
    {
        DialogChoice talk = Build(Slipped(Case())).Node(InterviewScript.AskNodeId).Choices.Single(c => c.Id == "smalltalk");
        Assert.AreEqual(3, talk.Lines.Count);
        Assert.AreEqual("The Nile rose right on time.", talk.Lines[1].Text);
        Assert.AreEqual(("interview.slips.4", DialogSpeaker.Traveller, "Home. Yes. New Kingdom Egypt (Ancient). I say it every morning so I don't forget."),
                        (talk.Lines[2].Id, talk.Lines[2].Speaker, talk.Lines[2].Text));
    }

    [Test]
    public void Build_ASlipIsNotAnAnswerLine()
    {
        DialogChoice talk = Build(Slipped(Case())).Node(InterviewScript.AskNodeId).Choices.Single(c => c.Id == "smalltalk");
        Assert.IsFalse(talk.Lines[2].IsAnswer, "never compare-clickable, never evidence");
        Assert.IsNull(talk.Lines[2].Value);
    }

    [Test]
    public void Build_NoSlipLeavesSmallTalkAsBefore()
    {
        DialogChoice talk = Build(Case()).Node(InterviewScript.AskNodeId).Choices.Single(c => c.Id == "smalltalk");
        Assert.AreEqual(2, talk.Lines.Count);
        CollectionAssert.IsEmpty(Build(Slipped(Case(smallTalk: false))).Node(InterviewScript.AskNodeId).Choices.Where(c => c.Id == "smalltalk"),
                                 "no small talk: nowhere to slip");
    }
}
