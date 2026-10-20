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
/// kept; decorations resting where let go (drop ≤8 px). The crate mornings
/// and the delivery-once criterion are recipes in docs/pending/DESK-PLAN.md.
/// </summary>
public static class DeskPlanChecks
{
    private const string Owner = "DESK-PLAN";

    /// <summary>The live desk config's home map has no problem (DeskLayoutSettings.Problems; the builder also stops on one).</summary>
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

    /// <summary>The intercom opens the wheel from the office view and from the reading view, and the reading view stays (FIX_PLAN_v2's DESK-PLAN delta).</summary>
    [GateCheck("desk.play.intercom", Owner, GateKind.Play)]
    public static GateResult Intercom() => GateResults.Require("desk.play.intercom", Owner, "desk_plan_homes.expect.*");

    /// <summary>Each decoration let go on a free spot rests where it was let go (≤8 px at 1080p, BUG-23).</summary>
    [GateCheck("desk.play.decor", Owner, GateKind.Play)]
    public static GateResult Decor() => GateResults.Require("desk.play.decor", Owner, "desk_plan_decor.drop.*");
}
