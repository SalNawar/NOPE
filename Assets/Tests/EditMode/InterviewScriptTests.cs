using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The interview graph (hub, ask menu, merged dialogs) and the structure and
/// capacity rules. Traveller under test: claims an Ancient place, carries a
/// Travel Passport and a Transit Permit, answers Currency honestly ("Deben")
/// and Geography with an Answer tell ("Babylon"), has no Politics answer, and
/// has a small-talk line. The rumour dialog is shaped like dlg_rumour.
/// </summary>
public class InterviewScriptTests
{
    private static InterviewLines Lines() => new InterviewLines
    {
        deskName = "DESK",
        requestLabel = "Request {document}",
        requestPrompt = new LineText("interview.requestPrompt", "Your {document}, please."),
        requestReply = new LineText("interview.requestReply", "Here you are."),
        askLabel = "Ask about home >",
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

    private static InterviewCase Case(bool smallTalk = true, string intro = "Next! Step forward, sir.") => new InterviewCase
    {
        introLine = intro,
        claimLine = "I request passage home to New Kingdom Egypt (Ancient).",
        claimedEraId = "ancient",
        documentNames = new[] { "Travel Passport", "Transit Permit" },
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

    private static DialogGraph Build(InterviewCase c = null, IReadOnlyList<InterviewQuestion> questions = null, IReadOnlyList<AuthoredDialog> dialogs = null) =>
        InterviewScript.Build(Lines(), questions ?? Questions(), dialogs ?? new[] { Rumour() }, c ?? Case());

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
    public void Request_SpeaksPromptAndReply_OpensItsDocument_AndIsRepeatable()
    {
        DialogChoice permit = Build().Node(InterviewScript.HubNodeId).Choices[1];
        Assert.AreEqual(DialogAction.OpenDocument, permit.Action);
        Assert.AreEqual(1, permit.DocumentIndex);
        Assert.IsFalse(permit.OneShot);
        Assert.IsTrue(string.IsNullOrEmpty(permit.Next), "stays on the hub");
        CollectionAssert.AreEqual(new[] { "interview.requestPrompt", "interview.requestReply" }, LineIds(permit.Lines));
        Assert.AreEqual("Your Transit Permit, please.", permit.Lines[0].Text);
        Assert.AreEqual(DialogSpeaker.Desk, permit.Lines[0].Speaker);
        Assert.AreEqual("Here you are.", permit.Lines[1].Text);
        Assert.AreEqual(DialogSpeaker.Traveller, permit.Lines[1].Speaker);
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
        IReadOnlyList<DialogLine> both = InterviewScript.Opening(Case());
        CollectionAssert.AreEqual(new[] { "case.intro", "case.claim" }, LineIds(both));
        Assert.AreEqual(DialogSpeaker.Desk, both[0].Speaker);
        Assert.AreEqual(DialogSpeaker.Traveller, both[1].Speaker);
        Assert.AreEqual("I request passage home to New Kingdom Egypt (Ancient).", both[1].Text);

        CollectionAssert.AreEqual(new[] { "case.claim" }, LineIds(InterviewScript.Opening(Case(intro: " "))));
    }

    [Test]
    public void TheRuntimeLineIds_HaveOneHome_ThatGenerateWorldReservesAndChecks()
    {
        Assert.AreEqual("case.intro", InterviewScript.IntroLineId);
        Assert.AreEqual("case.claim", InterviewScript.ClaimLineId);
        CollectionAssert.AreEqual(new[] { InterviewScript.IntroLineId, InterviewScript.ClaimLineId }, LineIds(InterviewScript.Opening(Case())));

        Assert.AreEqual("dlg_rumour.more", InterviewScript.ChoiceLineId("dlg_rumour", "more"));
        DialogChoice more = Build().Node("dlg_rumour/start").Choices[0];
        Assert.AreEqual(InterviewScript.ChoiceLineId("dlg_rumour", "more"), more.Id, "the runtime choice id");
        Assert.AreEqual(InterviewScript.ChoiceLineId("dlg_rumour", "more"), more.Lines[0].Id, "the label line the desk speaks");
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
        var runner = new DialogRunner(Build(), InterviewScript.Opening(Case()));
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
    public void Problems_ANodeWithMoreChoicesThanTheIntercomShows_UnlessTheCapacityIsZero()
    {
        AuthoredDialog d = Rumour();
        Only(DialogChecks.Problems(d, 1), "node 'start' offers 2 choices; the intercom shows at most 1");
        CollectionAssert.IsEmpty(DialogChecks.Problems(d, 0));
    }

    // -----------------------------
    // DialogChecks.MenuProblems
    // -----------------------------

    [Test]
    public void MenuProblems_TheAskMenu_BackPlusQuestionsPlusSmallTalk()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(6, true, 2, 2, 8), "< Back + 6 questions + small talk = 8");
        StringAssert.Contains("The ask menu holds 9 choices", Only(DialogChecks.MenuProblems(7, true, 2, 2, 8), "the intercom shows at most 8"));
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(7, false, 2, 2, 8), "without small talk, 7 questions fit");
    }

    [Test]
    public void MenuProblems_TheHub_DocumentsPlusAskPlusDialogs()
    {
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(1, false, 2, 5, 8), "2 requests + ask + 5 dialogs = 8");
        StringAssert.Contains("The hub holds 9 choices", Only(DialogChecks.MenuProblems(1, false, 2, 6, 8), "the intercom shows at most 8"));
        CollectionAssert.IsEmpty(DialogChecks.MenuProblems(99, true, 99, 99, 0), "no capacity, no check");
    }
}
