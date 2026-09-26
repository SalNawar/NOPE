using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The interview graph (hub, ask menu, merged dialogs) and the structure and
/// capacity rules. Traveller under test: claims an Ancient place, carries a
/// Travel Passport and a Transit Permit (both handed over on request unless a
/// test says otherwise), answers Currency honestly ("Deben")
/// and Geography with an Answer tell ("Babylon"), has no Politics answer, and
/// has a small-talk line. The rumour dialog is shaped like dlg_rumour. With a
/// key-word rule, each traveller line carries the spans that stay English
/// when it shows untranslated (the traveller-types spec's §8.1).
/// </summary>
public class InterviewScriptTests
{
    private static InterviewLines Lines() => new InterviewLines
    {
        deskName = "DESK",
        claim = new LineText("interview.claim", "I request passage home to {place}."),
        requestLabel = "Request {document}",
        requestPrompt = new LineText("interview.requestPrompt", "Your {document}, please."),
        requestReply = new LineText("interview.requestReply", "Here you are."),
        askLabel = "Ask about home >",
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
            prompt = new LineText("q_currency.ancient.prompt", "What do you trade with at home?"),
            answer = new LineText("q_currency.ancient.answer", "We trade with {value}.")
        });

        return new List<InterviewQuestion>
        {
            currency,
            Question("q_capital", ClueCategory.Geography, "Capital", "Our capital is {value}."),
            Question("q_ruler", ClueCategory.Politics, "Ruler", "We are ruled by {value}.")
        };
    }

    private static CaseDocument Doc(string name, DocumentHandOver handOver = DocumentHandOver.OnRequest) =>
        new CaseDocument { name = name, handOver = handOver };

    /// <summary>The key-word rule as world_source.json authors it (translation.keyWords).</summary>
    private static KeyWordRule KeyWordRule() => new KeyWordRule
    {
        slots = { "place", "name", "document" },
        words = { "home", "please", "papers", "yes", "no", "Temporal Customs" },
        digits = true
    };

    private static InterviewCase Case(bool smallTalk = true, string intro = "Next! Step forward, sir.", CaseDocument[] documents = null, KeyWordRule keyWords = null) => new InterviewCase
    {
        introLine = intro,
        claimPlace = "New Kingdom Egypt (Ancient)",
        keyWords = keyWords,
        claimedEraId = "ancient",
        documents = documents ?? new[] { Doc("Travel Passport"), Doc("Transit Permit") },
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

    private static string[] Ids(IEnumerable<DialogChoice> choices) => choices.Select(c => c.Id).ToArray();

    private static string[] LineIds(IEnumerable<DialogLine> lines) => lines.Select(l => l.Id).ToArray();

    // -----------------------------
    // Hub and ask menu
    // -----------------------------

    [Test]
    public void Hub_RequestsInPaperOrder_ThenAsk_ThenOneEntryPerDialog()
    {
        DialogNode hub = Build().Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "request:0", "request:1", "ask", "dlg:dlg_rumour" }, Ids(hub.Choices));
        CollectionAssert.AreEqual(new[] { "Request Travel Passport", "Request Transit Permit", "Ask about home >", "Any news from home? >" },
                                  hub.Choices.Select(c => c.Label).ToArray());

        DialogChoice ask = hub.Choices[2];
        Assert.AreEqual(InterviewScript.AskNodeId, ask.Next);
        CollectionAssert.IsEmpty(ask.Lines);

        DialogChoice dialog = hub.Choices[3];
        Assert.AreEqual("dlg_rumour/start", dialog.Next);
        Assert.IsTrue(dialog.OneShot);
    }

    [Test]
    public void Request_SpeaksPromptAndReply_HandsItsDocumentOver_AndIsOneShot()
    {
        DialogChoice permit = Build().Node(InterviewScript.HubNodeId).Choices[1];
        Assert.AreEqual(DialogAction.HandOverDocument, permit.Action);
        Assert.AreEqual(1, permit.DocumentIndex);
        Assert.IsTrue(permit.OneShot, "a paper once handed over never goes back mid-case");
        Assert.IsTrue(string.IsNullOrEmpty(permit.Next), "stays on the hub");
        CollectionAssert.AreEqual(new[] { "interview.requestPrompt", "interview.requestReply" }, LineIds(permit.Lines));
        Assert.AreEqual("Your Transit Permit, please.", permit.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, permit.Lines[0].Speaker);
        Assert.AreEqual("Here you are.", permit.Lines[1].Text);
        Assert.AreEqual(DialogSpeaker.Traveller, permit.Lines[1].Speaker);
    }

    [Test]
    public void ARequest_LeavesTheHubOnceChosen()
    {
        var runner = new DialogRunner(Build(), InterviewScript.Opening(Lines(), Case()));
        Assert.IsNotNull(runner.Choose("request:1"));
        CollectionAssert.AreEqual(new[] { "request:0", "ask", "dlg:dlg_rumour" }, Ids(runner.Choices));
        Assert.IsNull(runner.Choose("request:1"), "it cannot be chosen twice");
    }

    [Test]
    public void Hub_OnlyDocumentsHandedOverOnRequestGetARequest_KeepingThePaperIndex()
    {
        InterviewCase c = Case(documents: new[] { Doc("Travel Passport", DocumentHandOver.OnArrival), Doc("Transit Permit") });
        DialogNode hub = Build(c).Node(InterviewScript.HubNodeId);
        CollectionAssert.AreEqual(new[] { "request:1", "ask", "dlg:dlg_rumour" }, Ids(hub.Choices));
        Assert.AreEqual("Request Transit Permit", hub.Choices[0].Label);
        Assert.AreEqual(1, hub.Choices[0].DocumentIndex, "the index stays the paper's place in the case");
    }

    [Test]
    public void Hub_HasNoRequest_WhenEveryDocumentIsHandedOverOnArrival()
    {
        InterviewCase c = Case(documents: new[] { Doc("Travel Passport", DocumentHandOver.OnArrival), Doc("Transit Permit", DocumentHandOver.OnArrival) });
        CollectionAssert.AreEqual(new[] { "ask", "dlg:dlg_rumour" }, Ids(Build(c).Node(InterviewScript.HubNodeId).Choices));
    }

    [Test]
    public void EveryChoice_HasItsKind_ASubMenuEntryTakingTheKindOfWhatItOpens()
    {
        DialogGraph graph = Build(Dressed(new Garment(LookSlot.Hair, "Caesar crop", "Caesar crop / nodus roll", false)), lines: LinesWithRequests());
        var expected = new Dictionary<string, DialogChoiceKind>
        {
            ["request:0"] = DialogChoiceKind.Request, ["request:1"] = DialogChoiceKind.Request,
            ["act:step_closer"] = DialogChoiceKind.Request, ["act:speak_up"] = DialogChoiceKind.Request,
            ["ask"] = DialogChoiceKind.Question, ["look"] = DialogChoiceKind.Look, ["dlg:dlg_rumour"] = DialogChoiceKind.Dialog,
            ["back"] = DialogChoiceKind.Back, ["q:q_currency"] = DialogChoiceKind.Question, ["q:q_capital"] = DialogChoiceKind.Question,
            ["smalltalk"] = DialogChoiceKind.Question, ["look:0"] = DialogChoiceKind.Look,
            ["dlg_rumour.more"] = DialogChoiceKind.Normal, ["dlg_rumour.ignore"] = DialogChoiceKind.Normal, ["dlg_rumour.noted"] = DialogChoiceKind.Normal
        };

        var seen = new HashSet<string>();
        foreach (string node in new[] { InterviewScript.HubNodeId, InterviewScript.AskNodeId, InterviewScript.LookNodeId, "dlg_rumour/start", "dlg_rumour/detail" })
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
        CollectionAssert.AreEqual(new[] { "request:0", "request:1", "act:step_closer", "act:speak_up", "ask", "dlg:dlg_rumour" }, Ids(hub.Choices));
        CollectionAssert.AreEqual(new[] { "Step closer", "Speak up" }, hub.Choices.Skip(2).Take(2).Select(c => c.Label).ToArray());
    }

    [Test]
    public void ASpokenRequest_SpeaksPromptAndReply_IsOneShot_AndDoesNothingElse()
    {
        DialogChoice closer = Build(lines: LinesWithRequests()).Node(InterviewScript.HubNodeId).Choices[2];
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
        CollectionAssert.AreEqual(new[] { "request:0", "request:1" }, Ids(hub.Choices));
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
    public void AnswerLines_CarryTheFact_AndTheClaimedErasWordingWins()
    {
        DialogNode ask = Build().Node(InterviewScript.AskNodeId);

        DialogChoice currency = ask.Choices[1];
        CollectionAssert.AreEqual(new[] { "q_currency.ancient.prompt", "q_currency.ancient.answer" }, LineIds(currency.Lines));
        Assert.AreEqual("What do you trade with at home?", currency.Lines[0].Text);
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
    }

    [Test]
    public void Opening_IsTheIntroThenTheClaim_OrTheClaimAloneWhenTheIntroIsBlank()
    {
        IReadOnlyList<DialogLine> both = InterviewScript.Opening(Lines(), Case());
        CollectionAssert.AreEqual(new[] { "case.intro", "case.claim" }, LineIds(both));
        Assert.AreEqual(DialogSpeaker.Desk, both[0].Speaker);
        Assert.AreEqual(DialogSpeaker.Traveller, both[1].Speaker);
        Assert.AreEqual("I request passage home to New Kingdom Egypt (Ancient).", both[1].Text);

        CollectionAssert.AreEqual(new[] { "case.claim" }, LineIds(InterviewScript.Opening(Lines(), Case(intro: " "))));
    }

    [Test]
    public void Opening_TheClaimIsFilledFromItsTemplate_OrIsThePlaceAloneWhenBlank()
    {
        InterviewLines blank = Lines();
        blank.claim = null;
        Assert.AreEqual("New Kingdom Egypt (Ancient)", InterviewScript.Opening(blank, Case())[1].Text);
        Assert.AreEqual(Interview.Claim(Lines(), "New Kingdom Egypt (Ancient)"), InterviewScript.Opening(Lines(), Case())[1].Text, "the banner's text");
    }

    /// <summary>The English parts of a line (its key-word spans), joined by "|".</summary>
    private static string English(DialogLine line) =>
        string.Join("|", line.English.Select(s => line.Text.Substring(s.start, s.length)));

    [Test]
    public void TheClaim_KeepsHomeAndThePlaceEnglish_TheDesksOpenerHasNoSpans()
    {
        IReadOnlyList<DialogLine> opening = InterviewScript.Opening(Lines(), Case(keyWords: KeyWordRule()));
        Assert.AreEqual("home|New Kingdom Egypt (Ancient)", English(opening[1]));
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

        DialogLine requestReply = hub.Choices.First(ch => ch.Id == "request:0").Lines[1];
        Assert.AreEqual(DialogSpeaker.Traveller, requestReply.Speaker);
        CollectionAssert.IsEmpty(requestReply.English, "\"Here you are.\" holds no key word");
        CollectionAssert.IsEmpty(hub.Choices.First(ch => ch.Id == "request:0").Lines[0].English, "the desk's prompt");

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

        int before = runner.Transcript.Count;
        runner.Choose("request:0");
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
        CollectionAssert.AreEqual(new[] { "request:0", "request:1", "ask" }, Ids(runner.Choices), "back at the hub, the dialog entry used");
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
    public void MenuProblems_TheHub_RequestedDocumentsPlusAskPlusLookPlusDialogs_PremadeDialogsCountOnce()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, maxRequestedDocuments: 2, spokenRequests: 0, dialogs: 3, premadeDialogs: 3, maxChoices: 8),
                                 "2 requests + ask + look + 3 dialogs + one premade's dialog = 8");
        StringAssert.Contains("The hub holds 9 choices", Only(DialogChecks.MenuProblems(1, false, maxRequestedDocuments: 2, spokenRequests: 0, dialogs: 4, premadeDialogs: 3, maxChoices: 8), "the traveller wheel shows at most 8"));
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, maxRequestedDocuments: 2, spokenRequests: 0, dialogs: 3, premadeDialogs: 0, maxChoices: 7), "no premade dialog adds nothing");
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(99, true, 99, 99, 99, 99, 0), "no capacity, no check");
    }

    [Test]
    public void MenuProblems_TheHub_CountsEverySpokenRequest()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, maxRequestedDocuments: 1, spokenRequests: 2, dialogs: 2, premadeDialogs: 1, maxChoices: 8),
                                 "the starter content: 1 document request + 2 spoken requests + ask + look + 2 dialogs + one premade's dialog = 8");
        string problem = Only(DialogChecks.MenuProblems(1, false, maxRequestedDocuments: 1, spokenRequests: 3, dialogs: 2, premadeDialogs: 1, maxChoices: 8),
                              "the traveller wheel shows at most 8");
        StringAssert.Contains("The hub holds 9 choices (1 document request(s), 3 spoken request(s), the ask and look entries, 2 dialog(s), one premade's dialog)", problem);
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
        CollectionAssert.AreEqual(new[] { "request:0", "request:1", "ask", "look", "dlg:dlg_rumour" }, Ids(hub.Choices));
        DialogChoice look = hub.Choices[3];
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
}
