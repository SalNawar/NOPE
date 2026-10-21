#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// DOC-LOOK's rig op (docs/pending/QA-RIG.md "Tracks' own ops"; FIX_PLAN
/// DOC-LOOK, BUG-9): <c>capHeight</c> measures the cap height on the screen
/// of every text printed on every desk paper shown now, through the camera the
/// pointer looks through, and asserts <c>capHeight.&lt;name&gt;</c>: each
/// paper's smallest data (a Value or a Hand, DeskDocument.Print names each
/// text by its role) at least CapHeight.DataFloorA1 pixels at a screen 1080
/// lines tall or more and CapHeight.DataFloorA2 under it, its smallest label
/// at least CapHeight.LabelFloorA2 at 720 lines and under. The measure is
/// the font's cap line at the text's size, from the text's place, projected
/// up its own page: what a reader sees, the reading view's tilt and the
/// paper's world size included. <c>coverHits</c> asserts
/// <c>coverHits.&lt;name&gt;</c> (DOC-LOOK review, S3): what a click, a hover
/// or a drag would meet (QaGame.TopAt, the event system's own raycast) at the
/// middle of each half of every booklet and of the rulebook folder shown now.
/// The step's text says the state it checks ("booklet=shut folder=open",
/// Expects): every booklet and the folder must be in it, a shut booklet's visa
/// page and a shut folder's left board, hidden, meet nothing of theirs, and
/// the half that shows meets them; open, both halves meet them. A step that
/// does not say both states, or finds no booklet or no folder to probe, fails
/// (the re-verify of ce69276, point 1: a missed open or shut, or a day with no
/// passport on the counter, is red, never a second look at the state before).
/// </summary>
public static class DocLookOps
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register()
    {
        QaRunner.RegisterOp("capHeight", CapHeightOp);
        QaRunner.RegisterOp("coverHits", CoverHitsOp);
    }

    private static IEnumerator CoverHitsOp(QaRunner runner, QaStep step)
    {
        yield return null;
        string id = "coverHits." + (string.IsNullOrEmpty(step.name) ? "covers" : step.name);
        bool? bookletShut = Expects(step.text, "booklet"), folderShut = Expects(step.text, "folder");
        if (bookletShut == null || folderShut == null)
        {
            runner.Assert(id, "DOC-LOOK", false, "no expected state",
                          $"The step's text must say the state it checks of both, as \"booklet=shut folder=open\" (it says '{step.text}').");
            yield break;
        }
        Camera camera = QaGame.PointerCamera();
        if (camera == null)
        {
            runner.Assert(id, "DOC-LOOK", false, "no camera", "No pointer camera to probe through.");
            yield break;
        }
        var wrong = new List<string>();
        int probes = 0, booklets = 0;
        foreach (DeskDocument paper in Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude))
        {
            PaperCover cover = paper.Sheet != null ? paper.Sheet.GetComponentInChildren<PaperCover>(false) : null;
            if (cover == null)
                continue; // no booklet
            booklets++;
            bool shut = (bool)QaGame.Field(cover, "_shut");
            if (shut != bookletShut.Value)
                wrong.Add($"{paper.name} is {State(shut)}, the step checks it {State(bookletShut.Value)}");
            float spine = (float)QaGame.Field(cover, "_spine");
            float bottom = -paper.Size.y / 2f, top = paper.Size.y / 2f;
            Probe(paper.name + " visa page", paper.Sheet.TransformPoint(new Vector3(0f, (spine + top) / 2f, 0f)), paper.transform, !bookletShut.Value, camera, wrong, ref probes);
            Probe(paper.name + " data page", paper.Sheet.TransformPoint(new Vector3(0f, (bottom + spine) / 2f, 0f)), paper.transform, true, camera, wrong, ref probes);
        }
        if (booklets == 0)
            wrong.Add("no booklet is shown, so no passport's halves were probed");
        DeskRulebook folder = Object.FindAnyObjectByType<DeskRulebook>(FindObjectsInactive.Exclude);
        FolderCover board = folder != null ? folder.GetComponentInChildren<FolderCover>(true) : null;
        if (board != null && QaGame.Field(folder, "folderFace") is Renderer face && face != null)
        {
            if (folder.Shut != folderShut.Value)
                wrong.Add($"the folder is {State(folder.Shut)}, the step checks it {State(folderShut.Value)}");
            float left = DeskRulebook.BodyCentreX - DeskRulebook.BodyWidth / 2f, right = DeskRulebook.BodyCentreX + DeskRulebook.BodyWidth / 2f;
            Probe("folder left board", face.transform.TransformPoint(new Vector3((left + board.Spine) / 2f, DeskRulebook.BodyCentreY, 0f)), folder.transform, !folderShut.Value, camera, wrong, ref probes);
            Probe("folder right sheet", face.transform.TransformPoint(new Vector3((board.Spine + right) / 2f, DeskRulebook.BodyCentreY, 0f)), folder.transform, true, camera, wrong, ref probes);
        }
        else
            wrong.Add("no rulebook folder with its cover is shown, so its halves were not probed");
        runner.Assert(id, "DOC-LOOK", wrong.Count == 0,
                      $"booklet {State(bookletShut.Value)}, folder {State(folderShut.Value)}: {booklets} booklet(s), {probes} probes, {wrong.Count} wrong", string.Join("; ", wrong));
    }

    /// <summary>
    /// What a coverHits step's <paramref name="text"/> says it checks of
    /// <paramref name="what"/> ("booklet" or "folder"): pairs such as
    /// "booklet=shut folder=open", in any order and case, a space, a comma or a
    /// semicolon between. True for shut, false for open, null when the text
    /// does not say (or says neither word), which fails the step.
    /// </summary>
    public static bool? Expects(string text, string what)
    {
        foreach (string pair in (text ?? string.Empty).Split(new[] { ' ', ',', ';' }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = pair.IndexOf('=');
            if (eq <= 0 || !string.Equals(pair.Substring(0, eq), what, System.StringComparison.OrdinalIgnoreCase))
                continue;
            string state = pair.Substring(eq + 1);
            if (string.Equals(state, "shut", System.StringComparison.OrdinalIgnoreCase))
                return true;
            return string.Equals(state, "open", System.StringComparison.OrdinalIgnoreCase) ? false : (bool?)null;
        }
        return null;
    }

    /// <summary>"shut" or "open".</summary>
    private static string State(bool shut) => shut ? "shut" : "open";

    /// <summary>
    /// What the pointer meets at <paramref name="world"/> (through
    /// <paramref name="camera"/>) against what it should: a part of
    /// <paramref name="owner"/> when <paramref name="takes"/>, anything else
    /// (or nothing) when not; a wrong meeting goes into <paramref name="wrong"/>.
    /// </summary>
    private static void Probe(string what, Vector3 world, Transform owner, bool takes, Camera camera, List<string> wrong, ref int probes)
    {
        Vector3 screen = camera.WorldToScreenPoint(world);
        if (screen.z <= 0f || screen.x < 0f || screen.y < 0f || screen.x > Screen.width || screen.y > Screen.height)
        {
            wrong.Add($"{what}: off the screen");
            return;
        }
        probes++;
        GameObject top = QaGame.TopAt(screen);
        bool met = top != null && top.transform.IsChildOf(owner);
        if (met != takes)
            wrong.Add(takes ? $"{what} shows but the pointer meets {(top != null ? QaGame.Where(top) : "nothing")}" : $"{what} is hidden but the pointer meets {QaGame.Where(top)}");
    }

    private static IEnumerator CapHeightOp(QaRunner runner, QaStep step)
    {
        yield return null;
        Camera camera = QaGame.PointerCamera();
        string name = string.IsNullOrEmpty(step.name) ? "papers" : step.name;
        if (camera == null)
        {
            runner.Assert("capHeight." + name, "DOC-LOOK", false, "no camera", "No pointer camera to measure through.");
            yield break;
        }
        bool a1 = Screen.height >= 1000;
        float dataFloor = a1 ? CapHeight.DataFloorA1 : CapHeight.DataFloorA2;
        float labelFloor = a1 ? 0f : CapHeight.LabelFloorA2;
        float leastData = float.MaxValue, leastLabel = float.MaxValue;
        string leastDataAt = string.Empty, leastLabelAt = string.Empty;
        int papers = 0, texts = 0;
        foreach (DeskDocument paper in Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude))
        {
            if (paper.Sheet == null)
                continue;
            papers++;
            foreach (TMP_Text text in paper.Sheet.GetComponentsInChildren<TMP_Text>(false))
            {
                if (string.IsNullOrWhiteSpace(text.text) || !(text.GetComponent<Renderer>() is Renderer r) || !r.enabled || r.forceRenderingOff || text.font == null)
                    continue;
                bool data = text.name == nameof(FormTextRole.Value) || text.name == nameof(FormTextRole.Hand);
                bool label = text.name == nameof(FormTextRole.Label);
                if (!data && !label)
                    continue;
                float px = CapPixels(text, camera);
                if (px <= 0f)
                    continue;
                texts++;
                string at = $"{paper.name}:{text.text.Replace('\n', ' ')}";
                if (data && px < leastData)
                {
                    leastData = px;
                    leastDataAt = at;
                }
                if (label && px < leastLabel)
                {
                    leastLabel = px;
                    leastLabelAt = at;
                }
            }
        }
        bool dataOk = papers > 0 && leastData != float.MaxValue && leastData + 1e-3f >= dataFloor;
        bool labelOk = leastLabel == float.MaxValue || leastLabel + 1e-3f >= labelFloor;
        string measured = string.Format(CultureInfo.InvariantCulture, "{0}x{1}: data {2:0.0} px (floor {3:0}), label {4:0.0} px (floor {5:0}), {6} texts on {7} papers",
                                        Screen.width, Screen.height, leastData == float.MaxValue ? 0f : leastData, dataFloor,
                                        leastLabel == float.MaxValue ? 0f : leastLabel, labelFloor, texts, papers);
        runner.Assert("capHeight." + name, "DOC-LOOK", dataOk && labelOk, measured, $"smallest data '{leastDataAt}', smallest label '{leastLabelAt}'");
    }

    /// <summary>The on-screen pixels of <paramref name="text"/>'s capitals: its font's cap line at its size (a world-space em is TmpFormText.WorldEmPerFontSize per size unit), up its own page, through <paramref name="camera"/>.</summary>
    private static float CapPixels(TMP_Text text, Camera camera)
    {
        FaceInfo face = text.font.faceInfo;
        if (face.pointSize <= 0f)
            return 0f;
        float capLocal = face.capLine / face.pointSize * text.fontSize * TmpFormText.WorldEmPerFontSize;
        Transform t = text.transform;
        Vector3 baseline = t.position;
        Vector3 cap = t.TransformPoint(new Vector3(0f, capLocal, 0f));
        Vector3 a = camera.WorldToScreenPoint(baseline), b = camera.WorldToScreenPoint(cap);
        if (a.z <= 0f || b.z <= 0f)
            return 0f;
        return Vector2.Distance(a, b);
    }
}
#endif
