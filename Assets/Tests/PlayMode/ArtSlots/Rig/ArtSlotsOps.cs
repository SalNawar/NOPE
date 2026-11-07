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
/// <item><c>artSlotsPaper</c>: drags the case's one paper of the kind named by text (a PaperKind name, "Booklet": the
/// passport) from a point where the pointer meets it to (x2, y2), screen fractions with y from the bottom, on the rig's
/// plain path, settles n frames (default 45) and asserts <c>paper.&lt;name&gt;</c>: that paper lies on the desk, a
/// passport lies open, and the pointer meets it at its data page (a passport) or its middle. The drag op takes the first object named
/// PaperTemplate(Clone), the passport in one run and the ticket in the next (the round 1 verifier's F3); this op takes
/// the same paper in every run.</item>
/// <item><c>artSlotsCoverSeal</c>: asserts <c>coverSeal.&lt;name&gt;</c>: the rulebook folder lies shut and its cover's
/// seal quad (FolderCover's seal) is drawn with the texture named by text ("agency_seal": the slot's art; "form_seal"
/// is the code-drawn ring the cover's material held before round 2).</item>
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
        QaRunner.RegisterOp("artSlotsPaper", Paper);
        QaRunner.RegisterOp("artSlotsCoverSeal", CoverSeal);
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
        foreach (FormView view in UnityEngine.Object.FindObjectsByType<FormView>(FindObjectsInactive.Exclude))
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
            runner.Assert(id, Owner, true, measured, "the agency seals on the PC's pages, by sprite (not asserted: no expected sprite)");
        else
            runner.Assert(id, Owner, bySprite.Count == 1 && bySprite.ContainsKey(s.text), measured,
                $"every agency seal shown on the PC's pages draws {s.text} (ArtSlots.AgencySeal's art), and one is shown");
    }

    /// <summary>The case's paper of the kind named by text, dragged to (x2, y2), then asserted: on the desk, open when a passport, on top (the pointer meets it at its data page or its middle).</summary>
    private static IEnumerator Paper(QaRunner runner, QaStep s)
    {
        string id = "paper." + (string.IsNullOrEmpty(s.name) ? "kind" : QaRunner.Safe(s.name));
        if (!Enum.TryParse(s.text, true, out PaperKind kind) || runner.Input == null)
        {
            runner.Assert(id, Owner, false, "no paper kind", $"The step's text must name a PaperKind (\"Booklet\"), and the rig needs its virtual mouse; the text says '{s.text}'.");
            yield break;
        }
        yield return QaGame.UntilCameraStill(10f);
        DeskDocument[] papers = UnityEngine.Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude)
            .Where(d => d.isActiveAndEnabled).OrderBy(d => d.Index).ToArray();
        DeskDocument[] ofKind = papers.Where(d => d.Kind == kind).ToArray();
        if (ofKind.Length != 1)
        {
            runner.Assert(id, Owner, false, $"{ofKind.Length} papers of kind {kind}", $"The case shows {papers.Length} papers ({Papers(papers)}); the step needs exactly one {kind}.");
            yield break;
        }
        DeskDocument paper = ofKind[0];
        Vector2? from = PressPoint(paper);
        if (from == null)
        {
            runner.Assert(id, Owner, false, "the pointer meets no part of it", $"No point of {paper.name} ({kind}, paper {paper.Index}, {paper.Zone}) takes the pointer: it is off the screen or covered.");
            yield break;
        }
        var to = new Vector2(s.x2 * Screen.width, s.y2 * Screen.height);
        QaPath path = QaPaths.Drag(QaPathKind.Plain, from.Value, to, new Vector2(Screen.width, Screen.height));
        int rate = Time.captureFramerate;
        Time.captureFramerate = SeqRecorder.Fps; // the rig's drags play at its fixed step
        yield return runner.Input.Play(path);
        yield return QaGame.Frames(s.n > 0 ? s.n : 45);
        Time.captureFramerate = rate;

        PaperCover cover = paper.Sheet != null ? paper.Sheet.GetComponentInChildren<PaperCover>(false) : null;
        bool? shut = cover != null ? (bool)QaGame.Field(cover, "_shut") : (bool?)null;
        // The probe point: a passport's data page (between its foot and its spine, as coverHits probes it), else the sheet's middle.
        Vector3 probe = paper.Sheet != null ? paper.Sheet.position : paper.transform.position;
        if (cover != null)
            probe = paper.Sheet.TransformPoint(new Vector3(0f, (-paper.Size.y / 2f + (float)QaGame.Field(cover, "_spine")) / 2f, 0f));
        Camera camera = QaGame.PointerCamera();
        Vector3 middle = camera != null ? camera.WorldToScreenPoint(probe) : Vector3.back;
        GameObject top = middle.z > 0f ? QaGame.TopAt(middle) : null;
        bool onTop = top != null && top.transform.IsChildOf(paper.transform);
        string state = shut == null ? string.Empty : shut.Value ? ", shut" : ", open";
        string over = onTop ? $"on top at {(cover != null ? "its data page" : "its middle")}" : "under " + (top != null ? QaGame.Where(top) : "nothing");
        runner.Assert(id, Owner, paper.Zone == DeskZone.Desk && shut != true && onTop,
            $"{paper.name} ({kind}, paper {paper.Index}) on the {paper.Zone}{state}, {over}",
            FormattableString.Invariant($"pressed at {from.Value.x:0.0},{from.Value.y:0.0}, let go at {path.End.x:0.0},{path.End.y:0.0} (screen px, y from the bottom); the case's papers after the drag: {Papers(papers)}"));
    }

    /// <summary>A point of <paramref name="paper"/> the pointer meets: its middle first, then its sheet's, then a 3 by 3 grid over its screen rect; null when none.</summary>
    private static Vector2? PressPoint(DeskDocument paper)
    {
        var tries = new List<Vector2>();
        Vector2? middle = QaGame.ScreenPoint(paper.gameObject);
        if (middle != null)
            tries.Add(middle.Value);
        Camera camera = QaGame.PointerCamera();
        if (camera != null && paper.Sheet != null)
        {
            Vector3 sheet = camera.WorldToScreenPoint(paper.Sheet.position);
            if (sheet.z > 0f)
                tries.Add(sheet);
        }
        Rect? rect = QaGame.ScreenRect(paper.gameObject);
        if (rect != null)
            for (int y = 1; y <= 3; y++)
                for (int x = 1; x <= 3; x++)
                    tries.Add(new Vector2(rect.Value.xMin + rect.Value.width * x / 4f, rect.Value.yMin + rect.Value.height * y / 4f));
        foreach (Vector2 p in tries)
        {
            GameObject top = QaGame.TopAt(p);
            if (top != null && top.transform.IsChildOf(paper.transform))
                return p;
        }
        return null;
    }

    /// <summary>The papers as "name kind index zone" items, for a report.</summary>
    private static string Papers(IEnumerable<DeskDocument> papers) =>
        string.Join("; ", papers.Select(d => $"{d.name} {d.Kind} {d.Index} {d.Zone}"));

    /// <summary>The rulebook folder shut, its cover's seal drawn with the texture named by text.</summary>
    private static IEnumerator CoverSeal(QaRunner runner, QaStep s)
    {
        yield return QaGame.Frames(2);
        string id = "coverSeal." + (string.IsNullOrEmpty(s.name) ? "folder" : QaRunner.Safe(s.name));
        DeskRulebook folder = UnityEngine.Object.FindAnyObjectByType<DeskRulebook>(FindObjectsInactive.Exclude);
        FolderCover cover = folder != null ? folder.GetComponentInChildren<FolderCover>(true) : null;
        Transform seal = cover != null ? QaGame.Field(cover, "seal") as Transform : null;
        Renderer quad = seal != null ? seal.GetComponent<Renderer>() : null;
        Material material = quad != null ? quad.sharedMaterial : null;
        if (material == null)
        {
            runner.Assert(id, Owner, false, "no cover seal", "No rulebook folder with a cover seal quad and its material is loaded.");
            yield break;
        }
        Texture art = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
        string drawn = art != null ? art.name : "none";
        runner.Assert(id, Owner, folder.Shut && quad.isVisible && drawn == s.text,
            $"the folder {(folder.Shut ? "shut" : "open")}, its cover seal {(quad.isVisible ? "drawn" : "not drawn")} with {drawn} ({material.name})",
            $"the rulebook folder lies shut and its cover seal is drawn with {s.text} (ArtSlots.AgencySeal's art, through the material Rulebook_CoverSeal)");
    }

    /// <summary>The owner the art slot asserts name.</summary>
    private const string Owner = "gpt-art-slots";
}
#endif
