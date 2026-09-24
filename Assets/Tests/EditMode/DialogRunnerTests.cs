using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The dialog runner. Graph under test: node "a" (line "a.enter") offers
/// "go" (to "b"), "stay" (one-shot, no Next), "again" (repeatable, empty
/// Next) and "lost" (Next names no node); node "b" (line "b.enter") offers
/// "back" (to "a"). Each choice speaks one line, "{choice}.said".
/// </summary>
public class DialogRunnerTests
{
    private static DialogLine Line(string id) => new DialogLine(id, DialogSpeaker.Traveller, id);

    private static DialogChoice Choice(string id, string next, bool oneShot = false) => new DialogChoice
    {
        Id = id,
        Label = id,
        Next = next,
        OneShot = oneShot,
        Lines = new List<DialogLine> { Line(id + ".said") }
    };

    private static DialogGraph Graph()
    {
        var graph = new DialogGraph("a");
        graph.Add(new DialogNode
        {
            Id = "a",
            Lines = new List<DialogLine> { Line("a.enter") },
            Choices = new List<DialogChoice> { Choice("go", "b"), Choice("stay", null, oneShot: true), Choice("again", ""), Choice("lost", "nowhere") }
        });
        graph.Add(new DialogNode
        {
            Id = "b",
            Lines = new List<DialogLine> { Line("b.enter") },
            Choices = new List<DialogChoice> { Choice("back", "a") }
        });
        return graph;
    }

    private static DialogRunner Runner() => new DialogRunner(Graph(), new[] { Line("open.1"), Line("open.2") });

    private static string[] Ids(IEnumerable<DialogLine> lines) => lines.Select(l => l.Id).ToArray();

    private static string[] ChoiceIds(DialogRunner r) => r.Choices.Select(c => c.Id).ToArray();

    [Test]
    public void TheOpeningComesFirst_ThenTheStartNodesLines()
    {
        DialogRunner r = Runner();
        CollectionAssert.AreEqual(new[] { "open.1", "open.2", "a.enter" }, Ids(r.Transcript));
        CollectionAssert.AreEqual(new[] { "go", "stay", "again", "lost" }, ChoiceIds(r));
    }

    [Test]
    public void Choose_AppendsTheChoicesLines_ThenEntersNext_OnEveryEntry()
    {
        DialogRunner r = Runner();
        Assert.AreEqual("go", r.Choose("go").Id);
        CollectionAssert.AreEqual(new[] { "open.1", "open.2", "a.enter", "go.said", "b.enter" }, Ids(r.Transcript));
        CollectionAssert.AreEqual(new[] { "back" }, ChoiceIds(r));

        r.Choose("back");
        CollectionAssert.AreEqual(new[] { "open.1", "open.2", "a.enter", "go.said", "b.enter", "back.said", "a.enter" }, Ids(r.Transcript),
                                  "re-entering a node appends its lines again");
        CollectionAssert.AreEqual(new[] { "go", "stay", "again", "lost" }, ChoiceIds(r));
    }

    [Test]
    public void ANullEmptyOrUnknownNext_StaysOnTheNode()
    {
        DialogRunner r = Runner();
        r.Choose("stay");
        r.Choose("again");
        r.Choose("lost");
        CollectionAssert.AreEqual(new[] { "open.1", "open.2", "a.enter", "stay.said", "again.said", "lost.said" }, Ids(r.Transcript));
        CollectionAssert.AreEqual(new[] { "go", "again", "lost" }, ChoiceIds(r), "still on node a");
    }

    [Test]
    public void AOneShotChoice_LeavesTheMenu_ARepeatableOneStays()
    {
        DialogRunner r = Runner();
        r.Choose("stay");
        r.Choose("again");
        CollectionAssert.DoesNotContain(ChoiceIds(r), "stay");
        CollectionAssert.Contains(ChoiceIds(r), "again");

        Assert.IsNotNull(r.Choose("again"), "a repeatable choice can be picked again");
        CollectionAssert.Contains(ChoiceIds(r), "again");
    }

    [Test]
    public void AnIdNotOffered_ReturnsNull_AndChangesNothing()
    {
        DialogRunner r = Runner();
        r.Choose("stay");
        string[] before = Ids(r.Transcript);

        Assert.IsNull(r.Choose("back"), "a choice of another node");
        Assert.IsNull(r.Choose("stay"), "a used one-shot choice");
        Assert.IsNull(r.Choose("missing"));
        Assert.IsNull(r.Choose(null));

        CollectionAssert.AreEqual(before, Ids(r.Transcript));
        CollectionAssert.AreEqual(new[] { "go", "again", "lost" }, ChoiceIds(r));
    }

    [Test]
    public void Choices_IsAFreshListEachTime()
    {
        DialogRunner r = Runner();
        Assert.AreNotSame(r.Choices, r.Choices);
        ((List<DialogChoice>)r.Choices).Clear();
        Assert.AreEqual(4, r.Choices.Count);
    }

    [Test]
    public void TheGraph_RefusesADuplicateNodeId_AndFindsNodesById()
    {
        DialogGraph graph = Graph();
        Assert.Throws<ArgumentException>(() => graph.Add(new DialogNode { Id = "a" }));
        Assert.AreEqual("a", graph.StartNodeId);
        Assert.AreEqual("b", graph.Node("b").Id);
        Assert.IsNull(graph.Node("nowhere"));
        Assert.IsNull(graph.Node(null));
    }

    [Test]
    public void AnswerLines_CarryTheirFact_SpokenLinesDoNot()
    {
        var a = new InterviewAnswer { category = ClueCategory.Geography, value = "Babylon", isTell = true };
        DialogLine answer = DialogLine.Answer("q_capital.answer", "Our capital is Babylon.", a);
        Assert.AreEqual(DialogSpeaker.Traveller, answer.Speaker);
        Assert.IsTrue(answer.IsAnswer);
        Assert.AreEqual(ClueCategory.Geography, answer.Category);
        Assert.AreEqual("Babylon", answer.Value);
        Assert.IsTrue(answer.IsTell);

        var spoken = new DialogLine("case.intro", DialogSpeaker.Desk, "Next!");
        Assert.IsFalse(spoken.IsAnswer);
        Assert.IsFalse(spoken.IsTell);
        Assert.IsNull(spoken.Value);
    }
}
