using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// DOC-LOOK's rig op coverHits (DocLookOps; the re-verify of ce69276, point
/// 1): a step reads the state it checks from its text (DocLookOps.Expects),
/// and every coverHits step of doclook_print says both states, the open one
/// among them, so a missed open or shut fails the step instead of checking
/// the state before it again. Pure: the QA editor runner runs it offline
/// (with QA_PROJECT set to the project for the scenario's file).
/// </summary>
public class DocLookOpsTests
{
    [Test]
    public void Expects_ReadsEachThingsState_AndNothingTheTextDoesNotSay()
    {
        Assert.AreEqual(true, DocLookOps.Expects("booklet=shut folder=open", "booklet"));
        Assert.AreEqual(false, DocLookOps.Expects("booklet=shut folder=open", "folder"));
        Assert.AreEqual(false, DocLookOps.Expects("Folder=OPEN, booklet=shut", "folder"), "any order, a comma between, any case");
        Assert.IsNull(DocLookOps.Expects("folder=open", "booklet"), "a step that does not say the booklet's state fails the op");
        Assert.IsNull(DocLookOps.Expects("booklet=ajar", "booklet"), "neither shut nor open");
        Assert.IsNull(DocLookOps.Expects("booklets=shut", "booklet"), "another word");
        Assert.IsNull(DocLookOps.Expects(null, "folder"));
    }

    [Test]
    public void DocLookPrint_EveryCoverHitsStep_SaysTheStateItChecks_TheOpenOneAmongThem()
    {
        string json = File.ReadAllText(Path.Combine(SoFieldReadersTests.ProjectRoot(), "tools", "qa", "scenarios", "doclook_print.json"));
        // a step is one flat JSON object; JsonUtility (QaScenario.Parse) needs the engine, so the steps are read as text
        var steps = Regex.Matches(json, "\\{[^{}]*\"op\"\\s*:\\s*\"coverHits\"[^{}]*\\}").Cast<Match>().Select(m => m.Value).ToList();
        string Field(string step, string key) => Regex.Match(step, $"\"{key}\"\\s*:\\s*\"([^\"]*)\"").Groups[1].Value;
        string Say(bool? shut) => shut == null ? "unsaid" : shut.Value ? "shut" : "open";
        CollectionAssert.AreEqual(new[] { "shut: shut/shut", "folder_reshut: shut/shut", "open: open/open" },
                                  steps.Select(s => $"{Field(s, "name")}: {Say(DocLookOps.Expects(Field(s, "text"), "booklet"))}/{Say(DocLookOps.Expects(Field(s, "text"), "folder"))}"),
                                  "each step checks the booklet's and the folder's state by name; the last checks the restore on open");
    }
}
