/// <summary>
/// DOC-HANDLE's gate check (FOLD-01, Saleh's playtest of 2026-10-28, point
/// 4: "You move the folder and it snaps somewhere else"; the audit of
/// 2026-10-28 found no assertion that measured the folder's rest). Played,
/// at merge level, from the QA rig's run of tools/qa/scenarios/fold_rest.json
/// (QaJobs.Scenario): every drop rests within its step's limit of where it
/// was let go (the drop op, the root measured), every rest lies in its area
/// and over no keep-out and moves no more than its step allows (restTrace),
/// no slider lies over a thing resting in its strip (slideClear), and the run
/// reached its last probe ("end": a run that stopped early is red). FOLD-01
/// round 3: also tools/qa/scenarios/counter_drag.json, a paper pressed on the
/// counter follows the pointer one to one onto the desk and rests with the
/// pressed point under it, in the office view and the reading view
/// (counterDrag's follow and rest), and that run reached its last probe.
/// </summary>
public static class DocHandleChecks
{
    private const string Owner = "DOC-HANDLE";

    /// <summary>The fold_rest scenario's drops, rests and slides, and counter_drag's drags from the counter, measured at HEAD on a clean tree.</summary>
    [GateCheck("doc.play.rests", Owner, GateKind.Play)]
    public static GateResult Rests() =>
        CamChecks.Verdict("doc.play.rests", Owner, false, GateResults.ReadAll(), GitHead.Read(GateResults.ProjectRoot),
                          "fold_rest.drop.*", "fold_rest.restTrace.*", "fold_rest.slideClear.*", "fold_rest.missingScripts.end",
                          "counter_drag.counterDrag.*", "counter_drag.missingScripts.end");
}
