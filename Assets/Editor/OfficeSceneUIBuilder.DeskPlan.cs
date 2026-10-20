using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The office builder's desk plan gate (CheckDeskPlan).
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.cs by P0-A (pure move). Owner: DESK-PLAN.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>
    /// The desk home map holds (DeskLayoutSettings.Problems: every id one
    /// home on the art desk's top, none on the paper mat, over another home
    /// or the fixed art, every slide-in clean, every shown object seen by the
    /// office camera; BUG-4a, BUG-7a): each problem is an error in the
    /// Console, and any stops the build (it never saves a scene over a broken
    /// plan).
    /// </summary>
    private static void CheckDeskPlan(DeskConfigSO config)
    {
        if (config == null)
            return;
        List<string> problems = config.layout.Problems();
        foreach (string problem in problems)
            Debug.LogError($"[TimeDesk] The desk's plan: {problem} Move it in Desk_Default's layout (DeskLayoutSettings.homes).", config);
        if (problems.Count > 0)
            throw new System.InvalidOperationException($"[TimeDesk] Build Office UI stopped: the desk's plan has {problems.Count} problem(s) (see the Console).");
    }
}
