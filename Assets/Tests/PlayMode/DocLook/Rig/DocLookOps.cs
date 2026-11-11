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
        QaRunner.RegisterOp("readRest", ReadRestOp);
        QaRunner.RegisterOp("readLift", ReadLiftOp);
        QaRunner.RegisterOp("readPick", ReadPickOp);
        QaRunner.RegisterOp("readSpread", ReadSpreadOp);
    }

    /// <summary>
    /// doc-read-close's setup: drags every paper on the counter onto the mat,
    /// each to its own place of a 3 by 2 grid (screen shares 0.45, 0.6, 0.75
    /// across, 0.62 and 0.32 up), so the rest and the lift see them on the desk.
    /// </summary>
    private static IEnumerator ReadSpreadOp(QaRunner runner, QaStep step)
    {
        yield return null;
        Camera camera = QaGame.PointerCamera();
        int n = 0;
        foreach (DeskDocument paper in Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude))
        {
            if (camera == null || paper.Sheet == null || paper.Zone != DeskZone.Counter || !TopPoint(paper, camera, out Vector2 from))
                continue;
            var to = new Vector2(Screen.width * (0.45f + 0.15f * (n % 3)), Screen.height * (n < 3 ? 0.62f : 0.32f));
            yield return runner.Input.Play(QaPaths.Drag(QaPathKind.Plain, from, to, new Vector2(Screen.width, Screen.height)));
            for (float end = Time.unscaledTime + 1f; Time.unscaledTime < end;)
                yield return null;
            n++;
        }
        runner.Assert("readSpread." + (string.IsNullOrEmpty(step.name) ? "papers" : step.name), "DOC-LOOK", n > 0, $"{n} papers moved to the desk", string.Empty);
    }

    /// <summary>
    /// doc-read-close (D65) compare across two lifted papers, in inspect mode:
    /// the pointer goes onto paper A, waits for the lift, clicks A's first box;
    /// leaves A through the bottom band (A settles), goes onto paper B, waits,
    /// clicks B's first box. Asserts
    /// <c>readPick.&lt;name&gt;</c>: A's box is still picked once A is back at
    /// rest, and the compare holds a pair. A still after each click.
    /// </summary>
    private static IEnumerator ReadPickOp(QaRunner runner, QaStep step)
    {
        yield return null;
        string id = "readPick." + (string.IsNullOrEmpty(step.name) ? "pair" : step.name);
        Camera camera = QaGame.PointerCamera();
        var papers = new List<(DeskDocument paper, Vector2 on)>();
        foreach (DeskDocument p in Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude))
            if (camera != null && p.Sheet != null && p.Zone == DeskZone.Desk && SlotOf(p) != null && TopPoint(p, camera, out Vector2 on))
                papers.Add((p, on));
        if (papers.Count < 2)
        {
            runner.Assert(id, "DOC-LOOK", false, $"{papers.Count} papers with a box on the desk", "Two are needed.");
            yield break;
        }
        var notes = new List<string>();
        for (int k = 0; k < 2; k++)
        {
            DeskDocument paper = papers[k].paper;
            // off the lifted paper first (it covers the others), through the bottom band where nothing lifts, until it settles
            yield return runner.Input.MoveTo(new Vector2(Screen.width * 0.5f, Screen.height * 0.03f), 1);
            if (k > 0)
                yield return Settle(papers[0].paper);
            yield return runner.Input.MoveTo(papers[k].on, 1);
            for (float end = Time.unscaledTime + 0.4f; Time.unscaledTime < end;)
                yield return null;
            Vector3 slot = camera.WorldToScreenPoint(SlotOf(paper).position);
            GameObject top = QaGame.TopAt(slot);
            notes.Add($"{paper.name} lift {(float)QaGame.Field(paper, "_readNow"):0.00}, box at ({slot.x:0},{slot.y:0}) meets {(top != null ? top.name : "nothing")}");
            yield return runner.Input.Play(QaPaths.Click(slot));
            for (float end = Time.unscaledTime + 0.4f; Time.unscaledTime < end;)
                yield return null;
            runner.Recorder.Still(System.IO.Path.Combine(runner.OutDir, "stills", $"{QaRunner.Safe(id + "_click" + k)}_{Screen.width}x{Screen.height}.png"));
            yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the still");
        }
        DeskDocument a = papers[0].paper;
        float aNow = (float)QaGame.Field(a, "_readNow");
        bool aPicked = false;
        foreach (object view in (IList)QaGame.Field(a, "_slots"))
            aPicked |= (bool)QaGame.Field(view, "Picked");
        CompareController compare = Object.FindAnyObjectByType<CompareController>(FindObjectsInactive.Exclude);
        bool paired = compare != null && compare.Paired;
        runner.Assert(id, "DOC-LOOK", aPicked && aNow <= 0f && paired,
                      $"A ({a.name}) lift {aNow:0.00}, picked {aPicked}; pair {paired}", string.Join("; ", notes));
    }

    /// <summary>The first pickable box of <paramref name="paper"/> (Slot_0), or null.</summary>
    private static Transform SlotOf(DeskDocument paper)
    {
        foreach (Transform t in paper.Sheet.GetComponentsInChildren<Transform>(false))
            if (t.name == "Slot_0")
                return t;
        return null;
    }

    /// <summary>
    /// doc-read-close (D65) at rest: each desk paper's world width against the
    /// table's at its common scale (DeskConfigSO.readClose, within 10 %),
    /// and the folder wider and taller on the screen than every paper. Asserts
    /// <c>readRest.&lt;name&gt;</c>; the detail lists each paper's world and
    /// screen size.
    /// </summary>
    private static IEnumerator ReadRestOp(QaRunner runner, QaStep step)
    {
        yield return null;
        string id = "readRest." + (string.IsNullOrEmpty(step.name) ? "papers" : step.name);
        Camera camera = QaGame.PointerCamera();
        DeskConfigSO config = null;
        var rows = new List<(DeskDocument paper, FormFrame frame, float w, float h, Rect screen)>();
        foreach (DeskDocument paper in Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude))
        {
            if (paper.Sheet == null || paper.Zone != DeskZone.Desk || camera == null)
                continue;
            config ??= (DeskConfigSO)QaGame.Field(paper, "_config");
            rows.Add((paper, (FormFrame)QaGame.Field(paper, "_frame"), paper.Sheet.TransformVector(paper.Size.x, 0f, 0f).magnitude,
                      paper.Sheet.TransformVector(0f, paper.Size.y, 0f).magnitude, SheetRect(paper, camera)));
        }
        var wrong = new List<string>();
        var detail = new List<string>();
        if (config == null || rows.Count == 0)
            wrong.Add("no paper on the desk");
        else
            foreach (var r in rows)
            {
                float want = config.readClose.RestWidth(r.frame, r.paper.Size.x / r.paper.Size.y);
                detail.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1}: {2:0}x{3:0} mm (table {4:0} mm wide), {5:0}x{6:0} px",
                                         r.paper.name, r.frame, r.w * 1000f, r.h * 1000f, want * 1000f, r.screen.width, r.screen.height));
                if (Mathf.Abs(r.w / want - 1f) > 0.1f)
                    wrong.Add($"{r.frame}: {r.w * 1000f:0} mm wide, the table {want * 1000f:0} mm");
            }
        DeskRulebook folder = Object.FindAnyObjectByType<DeskRulebook>(FindObjectsInactive.Exclude);
        Rect? fr = folder != null ? QaGame.ScreenRect(folder.gameObject) : null;
        if (fr != null)
        {
            // on the screen: the reading view looks straight down, so pixels there are proportional to millimetres
            detail.Add(string.Format(CultureInfo.InvariantCulture, "folder: {0:0}x{1:0} px", fr.Value.width, fr.Value.height));
            foreach (var r in rows)
                if (r.screen.width >= fr.Value.width || r.screen.height >= fr.Value.height)
                    wrong.Add($"{r.frame} ({r.screen.width:0}x{r.screen.height:0} px) is not smaller than the folder ({fr.Value.width:0}x{fr.Value.height:0} px)");
        }
        else
            wrong.Add("no folder shown");
        runner.Assert(id, "DOC-LOOK", wrong.Count == 0, $"{rows.Count} papers on the desk, {wrong.Count} wrong", string.Join("; ", detail) + (wrong.Count > 0 ? " | WRONG: " + string.Join("; ", wrong) : string.Empty));
    }

    /// <summary>
    /// doc-read-close (D65) in inspect mode: the pointer jumps onto each desk
    /// paper in turn (a point where that paper is on top), stays <c>f</c>
    /// seconds (0.5 by default), then jumps into the bottom band and stays as
    /// long. Asserts <c>readLift.&lt;name&gt;</c>: the lifted paper's smallest
    /// value at least 11 px cap height at 1080 lines and over (9 under), its
    /// smallest label 8 px at 1080 and over (unless the paper fills the
    /// frame), no edge off the screen, its bottom above readClose.bottomMargin,
    /// one paper lifted at a time, no frame of the lift or the settle moving
    /// more than 20 % of its travel, and the paper back at rest after the
    /// settle. The detail lists each paper's numbers.
    /// </summary>
    private static IEnumerator ReadLiftOp(QaRunner runner, QaStep step)
    {
        yield return null;
        string id = "readLift." + (string.IsNullOrEmpty(step.name) ? "papers" : step.name);
        Camera camera = QaGame.PointerCamera();
        float hold = step.f > 0f ? step.f : 0.5f;
        bool big = Screen.height >= 1080;
        float valueBar = big ? 11f : 9f, labelBar = big ? 8f : 0f;
        var wrong = new List<string>();
        var detail = new List<string>();
        var papers = new List<DeskDocument>();
        foreach (DeskDocument p in Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude))
            if (p.Sheet != null && p.Zone == DeskZone.Desk)
                papers.Add(p);
        Vector2 away = new Vector2(Screen.width * 0.5f, Screen.height * 0.03f);
        int lifted = 0;
        foreach (DeskDocument paper in papers)
        {
            yield return runner.Input.MoveTo(away, 1);
            yield return Settle(paper, camera);
            if (camera == null || !TopPoint(paper, camera, out Vector2 on))
            {
                detail.Add($"{paper.name}: no point where it is on top");
                continue;
            }
            Rect rest = SheetRect(paper, camera);
            yield return runner.Input.MoveTo(on, 1);
            var up = new List<Rect>();
            int most = 0;
            for (float end = Time.unscaledTime + hold; Time.unscaledTime < end;)
            {
                yield return null;
                up.Add(SheetRect(paper, camera));
                int now = 0;
                foreach (DeskDocument p in papers)
                    if (p != null && (float)QaGame.Field(p, "_readNow") > 0f)
                        now++;
                most = Mathf.Max(most, now);
            }
            float readNow = (float)QaGame.Field(paper, "_readNow");
            if (readNow < 1f)
            {
                detail.Add($"{paper.name}: not lifted (clock {readNow:0.00})");
                wrong.Add($"{paper.name} did not lift");
                continue;
            }
            lifted++;
            Rect top = up[up.Count - 1];
            (float value, float label) = LeastCaps(paper, camera);
            float bottomPx = ((DeskConfigSO)QaGame.Field(paper, "_config")).readClose.bottomMargin * Screen.height;
            bool fills = top.width >= Screen.width * 0.97f || top.height >= (Screen.height - bottomPx) * 0.97f;
            bool cropped = top.height > Screen.height - bottomPx + 1f; // the lift crops a tall paper's top (readClose.topCrop)
            float upStep = MostStep(rest, up);
            int frames = up.FindIndex(r => Travel(r, top) <= 0.02f * Mathf.Max(1f, Travel(rest, top))) + 1;
            runner.Recorder.Still(System.IO.Path.Combine(runner.OutDir, "stills", $"{QaRunner.Safe(id + "_" + papers.IndexOf(paper) + "_" + QaGame.Field(paper, "_frame"))}_{Screen.width}x{Screen.height}.png"));
            yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the still");
            // text "key": inspect mode ends with the paper lifted (SPACE), then starts again for the next paper
            if (step.text == "key")
                yield return runner.Input.Tap(UnityEngine.InputSystem.Key.Space);
            else
                yield return runner.Input.MoveTo(away, 1);
            var down = new List<Rect>();
            for (float end = Time.unscaledTime + hold; Time.unscaledTime < end;)
            {
                yield return null;
                down.Add(SheetRect(paper, camera));
            }
            if (step.text == "key")
                yield return runner.Input.Tap(UnityEngine.InputSystem.Key.Space);
            float downStep = MostStep(top, down), back = Travel(down[down.Count - 1], rest);
            detail.Add(string.Format(CultureInfo.InvariantCulture,
                "{0}: rest {1:0}x{2:0} px, lifted {3:0}x{4:0} px at ({5:0},{6:0})-({7:0},{8:0}), value {9:0.0} px, label {10:0.0} px{11}, lift {12} frames, most step {13:0}% up {14:0}% down, back {15:0.0} px",
                paper.name, rest.width, rest.height, top.width, top.height, top.xMin, top.yMin, top.xMax, top.yMax, value, label, fills ? " (fills the frame)" : string.Empty,
                frames, upStep * 100f, downStep * 100f, back));
            if (value + 1e-3f < valueBar)
                wrong.Add($"{paper.name} value {value:0.0} px < {valueBar}");
            if (!fills && label + 1e-3f < labelBar)
                wrong.Add($"{paper.name} label {label:0.0} px < {labelBar}");
            if (top.xMin < -1f || top.xMax > Screen.width + 1f || (top.yMax > Screen.height + 1f && !cropped))
                wrong.Add($"{paper.name} leaves the screen");
            if (top.yMin < bottomPx - 1f)
                wrong.Add($"{paper.name} bottom {top.yMin:0} px under the band {bottomPx:0} px");
            if (most > 1)
                wrong.Add($"{most} papers lifted at once");
            if (!MotionPreference.Reduced && (upStep > 0.2f || downStep > 0.2f))
                wrong.Add($"{paper.name} jumps {Mathf.Max(upStep, downStep) * 100f:0}% of the travel in one frame");
            if (back > 2f)
                wrong.Add($"{paper.name} is {back:0.0} px off its rest after the settle");
        }
        if (lifted == 0)
            wrong.Add("no paper lifted");
        runner.Assert(id, "DOC-LOOK", wrong.Count == 0,
                      string.Format(CultureInfo.InvariantCulture, "{0}x{1}{2}: {3} of {4} papers lifted, {5} wrong", Screen.width, Screen.height, MotionPreference.Reduced ? " reduced" : string.Empty, lifted, papers.Count, wrong.Count),
                      string.Join("; ", detail) + (wrong.Count > 0 ? " | WRONG: " + string.Join("; ", wrong) : string.Empty));
    }

    /// <summary>
    /// Waits until <paramref name="paper"/>'s lift clock is 0 (at most 2 s) and, given a <paramref name="camera"/>, until its sheet
    /// rect on that camera has held within 0.3 px for 1 s (at most 6 s): the reading view's framing focus springs the lens back
    /// (46.0 to 47.5 degrees) up to 1.4 s after the view's cut, with a still gap before it, so a rest read at once, or after a
    /// short still, is a rest on a moving lens (lift-settle-reading).
    /// </summary>
    private static IEnumerator Settle(DeskDocument paper, Camera camera = null)
    {
        for (float end = Time.unscaledTime + 2f; Time.unscaledTime < end && (float)QaGame.Field(paper, "_readNow") > 0f;)
            yield return null;
        yield return QaGame.Frames(2);
        if (camera == null)
            yield break;
        Rect last = SheetRect(paper, camera);
        float since = Time.unscaledTime;
        for (float end = Time.unscaledTime + 6f; Time.unscaledTime < end && Time.unscaledTime - since < 1f;)
        {
            yield return null;
            Rect now = SheetRect(paper, camera);
            if (Travel(now, last) > 0.3f)
                since = Time.unscaledTime;
            last = now;
        }
    }

    /// <summary>The screen rect (pixels, y up) of <paramref name="paper"/>'s sheet as drawn now: its four corners.</summary>
    private static Rect SheetRect(DeskDocument paper, Camera camera)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = camera.WorldToScreenPoint(paper.Sheet.TransformPoint(new Vector3((i % 2 - 0.5f) * paper.Size.x, (i / 2 - 0.5f) * paper.Size.y, 0f)));
            minX = Mathf.Min(minX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxX = Mathf.Max(maxX, p.x);
            maxY = Mathf.Max(maxY, p.y);
        }
        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    /// <summary>How far apart two rects are (pixels): the larger of the centre's move and the size's change.</summary>
    private static float Travel(Rect a, Rect b) => Mathf.Max(Vector2.Distance(a.center, b.center), Mathf.Max(Mathf.Abs(a.width - b.width), Mathf.Abs(a.height - b.height)));

    /// <summary>The largest one-frame move of <paramref name="frames"/> after <paramref name="from"/>, as a share of the whole travel (to the last rect); 0 for no travel.</summary>
    private static float MostStep(Rect from, List<Rect> frames)
    {
        float whole = frames.Count > 0 ? Travel(from, frames[frames.Count - 1]) : 0f;
        if (whole < 1f)
            return 0f;
        float most = 0f;
        Rect before = from;
        foreach (Rect r in frames)
        {
            most = Mathf.Max(most, Travel(before, r) / whole);
            before = r;
        }
        return most;
    }

    /// <summary>A screen point where <paramref name="paper"/> is what the pointer meets: its centre, else a point of a 5 by 5 grid inside it, above the bottom band.</summary>
    private static bool TopPoint(DeskDocument paper, Camera camera, out Vector2 at)
    {
        Rect r = SheetRect(paper, camera);
        for (int k = 0; k < 25; k++)
        {
            int i = (k + 12) % 25; // the centre first
            at = new Vector2(Mathf.Lerp(r.xMin, r.xMax, 0.1f + 0.2f * (i % 5)), Mathf.Lerp(r.yMin, r.yMax, 0.1f + 0.2f * (i / 5)));
            if (at.x < 0f || at.y < Screen.height * 0.1f || at.x > Screen.width || at.y > Screen.height)
                continue;
            GameObject hit = QaGame.TopAt(at);
            if (hit != null && hit.transform.IsChildOf(paper.transform))
                return true;
        }
        at = default;
        return false;
    }

    /// <summary>The smallest value's (Value or Hand) and the smallest label's cap height on the screen on <paramref name="paper"/> (pixels; 0: none).</summary>
    private static (float value, float label) LeastCaps(DeskDocument paper, Camera camera)
    {
        float value = float.MaxValue, label = float.MaxValue;
        foreach (TMP_Text text in paper.Sheet.GetComponentsInChildren<TMP_Text>(false))
        {
            if (string.IsNullOrWhiteSpace(text.text) || !(text.GetComponent<Renderer>() is Renderer r) || !r.enabled || r.forceRenderingOff || text.font == null)
                continue;
            float px = CapPixels(text, camera);
            if (px <= 0f)
                continue;
            if (text.name == nameof(FormTextRole.Value) || text.name == nameof(FormTextRole.Hand))
                value = Mathf.Min(value, px);
            else if (text.name == nameof(FormTextRole.Label))
                label = Mathf.Min(label, px);
        }
        return (value == float.MaxValue ? 0f : value, label == float.MaxValue ? 0f : label);
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
