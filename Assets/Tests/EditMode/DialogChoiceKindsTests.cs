using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The wheel's kinds: one order for every menu, and an icon name per kind that draws a placeholder.</summary>
public class DialogChoiceKindsTests
{
    private static DialogChoice Choice(string id, DialogChoiceKind kind) => new DialogChoice { Id = id, Kind = kind };

    private static string[] Ids(IEnumerable<DialogChoice> choices) => choices.Select(c => c.Id).ToArray();

    private static IEnumerable<DialogChoiceKind> Kinds() => Enum.GetValues(typeof(DialogChoiceKind)).Cast<DialogChoiceKind>();

    [Test]
    public void Rank_IsBack_Request_Question_Look_Dialog_ThenNormal()
    {
        DialogChoiceKind[] order = Kinds().OrderBy(DialogChoiceKinds.Rank).ToArray();
        CollectionAssert.AreEqual(new[]
        {
            DialogChoiceKind.Back, DialogChoiceKind.Request, DialogChoiceKind.Question,
            DialogChoiceKind.Look, DialogChoiceKind.Dialog, DialogChoiceKind.Normal
        }, order);
        Assert.AreEqual(Kinds().Count(), Kinds().Select(DialogChoiceKinds.Rank).Distinct().Count(), "one rank per kind");
    }

    [Test]
    public void Arrange_GroupsByKind_KeepingTheOrderWithinAKind()
    {
        var choices = new[]
        {
            Choice("dlg:a", DialogChoiceKind.Dialog), Choice("ask", DialogChoiceKind.Question), Choice("request:1", DialogChoiceKind.Request),
            Choice("reply", DialogChoiceKind.Normal), Choice("back", DialogChoiceKind.Back), Choice("act:x", DialogChoiceKind.Request),
            Choice("look", DialogChoiceKind.Look), Choice("dlg:b", DialogChoiceKind.Dialog)
        };
        CollectionAssert.AreEqual(new[] { "back", "request:1", "act:x", "ask", "look", "dlg:a", "dlg:b", "reply" }, Ids(DialogChoiceKinds.Arrange(choices)));
        Assert.AreEqual("dlg:a", choices[0].Id, "the input is left as it was");
    }

    [Test]
    public void Arrange_DropsNulls_AndGivesEmptyForNone()
    {
        CollectionAssert.AreEqual(new[] { "back", "q" }, Ids(DialogChoiceKinds.Arrange(new[] { null, Choice("q", DialogChoiceKind.Question), null, Choice("back", DialogChoiceKind.Back) })));
        CollectionAssert.IsEmpty(DialogChoiceKinds.Arrange(null));
        CollectionAssert.IsEmpty(DialogChoiceKinds.Arrange(new DialogChoice[0]));
    }

    [Test]
    public void Arrange_LeavesEveryBuiltMenu_InItsBuiltOrder()
    {
        var lines = new InterviewLines { requestLabel = "Request {document}", askLabel = "Ask >", lookLabel = "Look >", backLabel = "< Back", smallTalkLabel = "Small talk" };
        lines.requests.Add(new InterviewRequest { id = "step_closer", label = "Step closer" });
        var question = new InterviewQuestion { id = "q_currency", category = ClueCategory.Currency, label = "Currency" };
        var dialog = new AuthoredDialog
        {
            id = "dlg_a",
            label = "Talk >",
            nodes = { new ScriptNode { id = "start", choices = { new ScriptChoice { id = "bye", label = "Bye." }, new ScriptChoice { id = "stay", label = "Stay." } } } }
        };
        var c = new InterviewCase
        {
            documents = new[] { new CaseDocument { name = "Passport", handOver = DocumentHandOver.OnRequest }, new CaseDocument { name = "Permit", handOver = DocumentHandOver.OnRequest } },
            answers = new[] { new InterviewAnswer { category = ClueCategory.Currency, value = "Deben" } },
            smallTalk = new LineText("x.smalltalk.1", "Fine."),
            garments = new[] { new Garment(LookSlot.Outfit, "kilt", "v", false), new Garment(LookSlot.Hair, "crop", "v", false) }
        };

        DialogGraph graph = InterviewScript.Build(lines, new[] { question }, new[] { dialog }, c);
        foreach (string node in new[] { InterviewScript.HubNodeId, InterviewScript.AskNodeId, InterviewScript.LookNodeId, "dlg_a/start" })
            CollectionAssert.AreEqual(Ids(graph.Node(node).Choices), Ids(DialogChoiceKinds.Arrange(graph.Node(node).Choices)), node);
    }

    [Test]
    public void IconName_IsWheelThenTheKind_InLowerCase_OnePerKind()
    {
        Assert.AreEqual("wheel_request", DialogChoiceKinds.IconName(DialogChoiceKind.Request));
        Assert.AreEqual("wheel_back", DialogChoiceKinds.IconName(DialogChoiceKind.Back));
        Assert.AreEqual("wheel_normal", DialogChoiceKinds.IconName(DialogChoiceKind.Normal));
        Assert.AreEqual(Kinds().Count(), Kinds().Select(DialogChoiceKinds.IconName).Distinct().Count());
    }

    [Test]
    public void EveryKind_HasADistinctPlaceholderIcon()
    {
        var drawn = new List<byte[]>();
        foreach (DialogChoiceKind kind in Kinds())
        {
            byte[] rgba = WheelIconPlaceholder.Render(DialogChoiceKinds.IconName(kind));
            Assert.IsNotNull(rgba, $"{kind} has a placeholder");
            Assert.IsFalse(drawn.Any(d => d.SequenceEqual(rgba)), $"{kind}'s glyph is its own");
            drawn.Add(rgba);
        }
    }
}
