using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// DESK-PLAN's gate checks (BUG-4a, 7a, 23, 36b; the desk home map Saleh
/// approved on 2026-10-20). Static: the live desk config's home map holds
/// (one home per id, none on the mat, apart, clear of the fixed art, clean
/// slide-ins, every shown object seen from the office camera); every home of
/// the approved map is in it; the cut props are gone from the scene contract.
/// Play (read from the QA rig's runs of tools/qa/scenarios/desk_plan_*.json,
/// QaJobs.Scenario): every kept object ≥90% seen (the ID-buffer op
/// deskvisible) at A1 and A2 and its centre click reaching it; the intercom
/// opening the wheel in the office and the reading view, the reading view
/// kept; no two desk objects' real click boxes overlapping (the op
/// deskclickboxes, days 1 and 12); decorations resting where let go (drop
/// ≤8 px). The crate mornings
/// and the delivery-once criterion are recipes in docs/pending/DESK-PLAN.md.
/// </summary>
public static class DeskPlanChecks
{
    private const string Owner = "DESK-PLAN";

    /// <summary>The live desk config's home map has no problem (DeskLayoutSettings.Problems: the homes and their click boxes with the builder's pad; the builder also stops on one).</summary>
    [GateCheck("desk.plan.homes", Owner)]
    public static GateResult Homes()
    {
        DeskConfigSO config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(OfficeSceneUIBuilder.DeskConfigPath);
        if (config == null)
            return GateResult.Fail("desk.plan.homes", Owner, "no config", $"No DeskConfigSO at {OfficeSceneUIBuilder.DeskConfigPath}.");
        List<string> problems = config.layout.Problems();
        return GateResult.Of("desk.plan.homes", Owner, problems.Count == 0, $"{config.layout.homes.Length} homes, {problems.Count} problem(s)", string.Join("; ", problems));
    }

    /// <summary>Every home of the approved map is in the live config (a home removed from the asset strands its Phase B object).</summary>
    [GateCheck("desk.plan.everyHome", Owner)]
    public static GateResult EveryHome()
    {
        DeskConfigSO config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>(OfficeSceneUIBuilder.DeskConfigPath);
        if (config == null)
            return GateResult.Fail("desk.plan.everyHome", Owner, "no config", $"No DeskConfigSO at {OfficeSceneUIBuilder.DeskConfigPath}.");
        List<string> missing = DeskHomeMap.Approved().Select(h => h.id).Where(id => config.layout.HomeOf(id) == null).ToList();
        return GateResult.Of("desk.plan.everyHome", Owner, missing.Count == 0, $"{missing.Count} home(s) missing", string.Join(", ", missing));
    }

    /// <summary>The cut props (Saleh: "we will cut all but decor objects") have no anchor spec left in the scene contract, so the binder neither binds nor warns about them.</summary>
    [GateCheck("desk.plan.cutProps", Owner)]
    public static GateResult CutProps()
    {
        OfficeSceneContractSO contract = AssetDatabase.LoadAssetAtPath<OfficeSceneContractSO>(OfficeSceneUIBuilder.OfficeContractPath);
        if (contract == null)
            return GateResult.Fail("desk.plan.cutProps", Owner, "no contract", $"No OfficeSceneContractSO at {OfficeSceneUIBuilder.OfficeContractPath}.");
        var cut = new[] { OfficeAnchorId.Calculator, OfficeAnchorId.PenPot, OfficeAnchorId.Stapler };
        List<string> left = cut.Where(id => contract.Spec(id) != null).Select(id => id.ToString()).ToList();
        return GateResult.Of("desk.plan.cutProps", Owner, left.Count == 0, $"{left.Count} cut prop(s) still in the contract", string.Join(", ", left));
    }

    /// <summary>Every kept object on its home is ≥90% seen from the office camera (ID buffer) at A1 and A2, days 1 and 12 (BUG-4a).</summary>
    [GateCheck("desk.play.visible", Owner, GateKind.Play)]
    public static GateResult Visible() => GateResults.Require("desk.play.visible", Owner, "desk_plan_homes.visible.*");

    /// <summary>Every click of the home run reached its object under the pointer (centre-clickable; nothing covers it).</summary>
    [GateCheck("desk.play.clicks", Owner, GateKind.Play)]
    public static GateResult Clicks() => GateResults.Require("desk.play.clicks", Owner, "desk_plan_homes.presses");

    /// <summary>No two desk objects' real click boxes go into each other deeper than 5 mm, days 1 and 12 (BUG-4a; the static half is desk.plan.homes' ClickBoxProblems).</summary>
    [GateCheck("desk.play.clickBoxes", Owner, GateKind.Play)]
    public static GateResult ClickBoxes() => GateResults.Require("desk.play.clickBoxes", Owner, "desk_plan_homes.clickboxes.*");

    /// <summary>The intercom opens the wheel from the office view and from the reading view, and the reading view stays (FIX_PLAN_v2's DESK-PLAN delta).</summary>
    [GateCheck("desk.play.intercom", Owner, GateKind.Play)]
    public static GateResult Intercom() => GateResults.Require("desk.play.intercom", Owner, "desk_plan_homes.expect.*");

    /// <summary>Each decoration let go on a free spot rests where it was let go (≤8 px at 1080p, BUG-23); on the painted desk, which has no decoration spots, the scenario's "not applicable" line (DeskPlanOps' deskhomeshown, seam C of 2026-10-28) is red: the decorations are cut there (BUG-23a, known red under DESK-V3 until Saleh rules; the code review's RG-03).</summary>
    [GateCheck("desk.play.decor", Owner, GateKind.Play)]
    public static GateResult Decor()
    {
        (IEnumerable<QaGateResult> results, string head) = Measured();
        return DecorVerdict(results, head);
    }

    /// <summary>The results and HEAD the decor check judges: every result in Library/QA/results and the checkout's commit (DeskPlanGateTests put given ones in their place).</summary>
    internal static Func<(IEnumerable<QaGateResult> Results, string Head)> Measured = () => (GateResults.ReadAll(), GitHead.Read(GateResults.ProjectRoot));

    /// <summary>The scenario's pass line on the painted desk (desk_plan_decor's deskhomeshown step, name decor_spots).</summary>
    public const string DecorSpotsId = "desk_plan_decor.shown.decor_spots";

    /// <summary>
    /// The decor check's verdict (pure; DeskPlanGateTests): the drops
    /// (<c>desk_plan_decor.drop.*</c>, GateResults.Judge) when any was
    /// measured at <paramref name="head"/>; else the scenario's
    /// <see cref="DecorSpotsId"/> line. A line that says the spots are not
    /// applicable (the painted desk: its decorations are cut, DESK-V3) is red,
    /// not green: Saleh asked for the decorations (the 1018a playtest), and the
    /// cut is the orchestrator's ruling 13, open to his veto (the code review's
    /// RG-03 of 2026-10-28: the check passed a cut feature as green). The
    /// known-red list spares that one line at the merge gate (BUG-23a,
    /// DESK-V3), and the result names it (<see cref="GateResult.Unmet"/>), so a
    /// red drop is never spared with it. A desk with spots and no drop
    /// measured at HEAD is red: run the recipe.
    /// </summary>
    public static GateResult DecorVerdict(IEnumerable<QaGateResult> results, string head)
    {
        const string id = "desk.play.decor";
        List<QaGateResult> all = (results ?? Enumerable.Empty<QaGateResult>()).ToList();
        bool dropsAtHead = all.Any(r => GateResults.Matches("desk_plan_decor.drop.*", r.id) && string.Equals(r.commit, head, StringComparison.OrdinalIgnoreCase));
        if (dropsAtHead)
            return GateResults.Judge(id, Owner, "desk_plan_decor.drop.*", all, head, false);
        GateResult shown = GateResults.Judge(id, Owner, DecorSpotsId, all, head, false);
        if (!shown.Pass)
            return GateResult.Fail(id, Owner, shown.Measured, $"no drop measured at HEAD and no '{DecorSpotsId}' line that passes: {shown.Detail}");
        QaGateResult line = all.First(r => string.Equals(r.id, DecorSpotsId, StringComparison.OrdinalIgnoreCase) && string.Equals(r.commit, head, StringComparison.OrdinalIgnoreCase));
        bool notApplicable = (line.measured ?? string.Empty).StartsWith("not applicable", StringComparison.OrdinalIgnoreCase);
        if (!notApplicable)
            return GateResult.Fail(id, Owner, line.measured, "the desk in use has decoration spots, but there is no drop measured at HEAD (desk_plan_decor.drop.*): run the recipe (docs/pending/QA-RIG.md)");
        GateResult cut = GateResult.Fail(id, Owner, line.measured,
            $"the decorations are cut on the painted desk (DESK-V3, DeskPaintedMap.CutParts), so no decoration can be placed: Saleh asked for them (1018a); BUG-23a, known red until he rules on the cut. {line.detail}");
        cut.Unmet.Add(line.id);
        return cut;
    }
}
