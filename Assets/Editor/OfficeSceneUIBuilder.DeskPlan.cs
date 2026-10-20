using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The office builder's desk plan gate (CheckDeskPlan).
/// Part of <see cref="OfficeSceneUIBuilder"/>; carved out of OfficeSceneUIBuilder.cs by P0-A (pure move). Owner: DESK-PLAN.
/// </summary>
public static partial class OfficeSceneUIBuilder
{
    /// <summary>The desk's plan holds (the desk layout spec: every bay on the art desk's top, none overlapping; DeskArrangement.Problems): each problem is an error in the Console.</summary>
    private static void CheckDeskPlan(DeskConfigSO config)
    {
        if (config == null)
            return;
        foreach (string problem in DeskArrangement.Problems(new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, DeskRect>>(config.layout.Bays()), config.layout.deskTop.Rect))
            Debug.LogError($"[TimeDesk] The desk's plan: {problem} Move it in Desk_Default's layout (DeskLayoutSettings).", config);
    }
}
