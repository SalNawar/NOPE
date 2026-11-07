#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The art slots' scenario ops (the night task gpt-art-slots; tools/qa/scenarios/art_slots.json walks every
/// place a by-name slot shows, ArtSlots):
/// <list type="bullet">
/// <item><c>artSlotsTab</c>: shows the Investigation app's tab named by text (an AppTab name, "Reference") in
/// its target pane, as a click on its menu entry does (InvestigationApp.ShowTab).</item>
/// <item><c>artSlotsSeals</c>: counts the PC's form pages whose faint seal is the agency's (FormView's seal image
/// shown, not an office's) by the sprite each draws; with text it asserts that every one draws that sprite
/// ("agency_seal": the slot's art, Forms/agency_seal; the code-drawn ring is "form_seal") and that there is one.</item>
/// </list>
/// </summary>
public static class ArtSlotsOps
{
    /// <summary>Registers the ops before the first scene loads (editor and QA player alike), and in the editor on every domain load too, so the edit-mode scenario test knows them. Registering twice replaces the same bodies.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register()
    {
        QaRunner.RegisterOp("artSlotsTab", Tab);
        QaRunner.RegisterOp("artSlotsSeals", Seals);
    }

    /// <summary>The Investigation app's tab named by text, on the open PC.</summary>
    private static IEnumerator Tab(QaRunner runner, QaStep s)
    {
        InvestigationApp app = QaGame.Find<InvestigationApp>() ?? throw new InvalidOperationException("No Investigation app is loaded.");
        app.ShowTab((AppTab)Enum.Parse(typeof(AppTab), s.text, true));
        yield return QaGame.Frames(15);
    }

    /// <summary>The agency seals shown on the PC's form pages, counted by sprite; asserted against text when it is set.</summary>
    private static IEnumerator Seals(QaRunner runner, QaStep s)
    {
        yield return QaGame.Frames(2);
        var bySprite = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (FormView view in UnityEngine.Object.FindObjectsByType<FormView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var seal = (Image)QaGame.Field(view, "seal");
            if (seal == null || !seal.isActiveAndEnabled)
                continue;
            string name = seal.sprite != null ? seal.sprite.name : "none";
            bySprite[name] = bySprite.TryGetValue(name, out int n) ? n + 1 : 1;
        }

        string measured = bySprite.Count == 0 ? "no seal shown" : string.Join(", ", bySprite.Select(p => $"{p.Key} {p.Value}"));
        string id = "seals." + (string.IsNullOrEmpty(s.name) ? "pc" : s.name);
        if (string.IsNullOrEmpty(s.text))
            runner.Assert(id, "gpt-art-slots", true, measured, "the agency seals on the PC's pages, by sprite (not asserted: no expected sprite)");
        else
            runner.Assert(id, "gpt-art-slots", bySprite.Count == 1 && bySprite.ContainsKey(s.text), measured,
                $"every agency seal shown on the PC's pages draws {s.text} (ArtSlots.AgencySeal's art), and one is shown");
    }
}
#endif
