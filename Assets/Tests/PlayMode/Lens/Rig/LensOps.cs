#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>
/// The scenario ops of the PC Lens case screen (docs/superpowers/specs/2026-11-12-pc-lens-case-screen.md; the scenarios
/// lens_place, lens_drag and lens_inspect). Each plays the player's input through the rig's virtual mouse; none calls a handler.
/// <list type="bullet">
/// <item><c>lensScan</c>: the PC receives the copies of the first n papers (default 2) of the case, as the scanner's finish does (the case's presenter, by reflection). The app puts a copy on the selected side when that side is empty.</item>
/// <item><c>lensClick</c>: a click on the target named by text: "dock:Documents:1" (a dock item: AppTab name, optional item), "tag:left" or "tag:right" (a side's tag strip), "row:left:Name" or "row:right:Name" (the first pickable row of that side whose label contains the word; "~regex" matches a pattern, "!word" a label without the word, "#" any), "noentry" (the Registry's no entry line). Fails with a message when the target is missing or covered.</item>
/// <item><c>lensDrag</c>: a drag named by text "from>to". From: a dock item as above, "left" or "right" (the middle of that side). To: "left", "right" or "header" (outside both sides).</item>
/// <item><c>lensRightClick</c>: a right click where the pointer is.</item>
/// <item><c>lensRegistry</c>: the Registry's lookup of the text, typed in its field and run (as Enter does).</item>
/// <item><c>lensExpect</c>: asserts <c>lens.&lt;name&gt;</c> over the app's state. The text holds words "key=value", all of which must hold: left, right (none, "Tab" or "Tab:item"), selected (left, right), inspect (on, off), findings (a number), new (findings gained since mark), mark (1: remember the findings now), papers&gt;=N (papers available in the dock), pick (held, none), line (yes, no), plate (hold, match, differ, info, none; "a|b" for either), notice (hint, none).</item>
/// </list>
/// </summary>
public static class LensOps
{
    private const string Owner = "pc-lens";
    private static int _mark;
    private static string _lastLabel = string.Empty;

    /// <summary>Registers the ops before the first scene loads (and on every editor domain load, so the edit-mode scenario test knows them).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register()
    {
        QaRunner.RegisterOp("lensScan", Scan);
        QaRunner.RegisterOp("lensClick", Click);
        QaRunner.RegisterOp("lensDrag", Drag);
        QaRunner.RegisterOp("lensRightClick", RightClick);
        QaRunner.RegisterOp("lensRegistry", Registry);
        QaRunner.RegisterOp("lensExpect", Expect);
    }

    private static InvestigationApp App() => QaGame.Find<InvestigationApp>() ?? throw new InvalidOperationException("No Investigation app is loaded.");

    private static AppPane Pane(InvestigationApp app, string side) =>
        (AppPane)QaGame.Field(app, string.Equals(side, "left", StringComparison.OrdinalIgnoreCase) ? "leftPane" : "rightPane");

    private static IEnumerator Scan(QaRunner runner, QaStep s)
    {
        Component ui = QaGame.FindComponent("InvestigationUIController");
        object documents = QaGame.Field(ui, "_documents");
        MethodInfo scan = documents.GetType().GetMethod("Scan", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(int) }, null)
                          ?? throw new MissingMethodException(documents.GetType().Name, "Scan(int)");
        for (int i = 0; i < (s.n > 0 ? s.n : 2); i++)
            scan.Invoke(documents, new object[] { i });
        yield return QaGame.Frames(20);
    }

    /// <summary>The object a target names, or null with the reason.</summary>
    private static GameObject Resolve(InvestigationApp app, string target, out string why)
    {
        why = null;
        string[] part = (target ?? string.Empty).Split(':');
        switch (part[0].ToLowerInvariant())
        {
            case "dock":
            {
                LensDock dock = QaGame.Find<LensDock>();
                if (dock == null || part.Length < 2 || !Enum.TryParse(part[1], true, out AppTab tab))
                {
                    why = $"'{target}' is not dock:<AppTab>[:item], or no dock is loaded.";
                    return null;
                }
                int item = part.Length > 2 ? int.Parse(part[2]) : -1;
                for (int i = 0; i < dock.Items.Count; i++)
                {
                    ShelfItem x = dock.Items[i];
                    if (x.Action != ShelfAction.Open || x.Target.Tab != tab || (item >= 0 && x.Target.Item != item))
                        continue;
                    Transform found = dock.transform.Find("Row/Item_" + i);
                    if (found != null && x.Available)
                        return found.gameObject;
                }
                why = $"the dock has no available item for {part[1]} {item}; its items: {string.Join(", ", dock.Items.Select(x => $"{x.Label} ({x.Target.Tab}:{x.Target.Item}{(x.Available ? string.Empty : ", not available")})"))}.";
                return null;
            }
            case "tag":
                return ((Button)QaGame.Field(Pane(app, part.Length > 1 ? part[1] : "left"), "headerButton")).gameObject;
            case "noentry":
                return QaGame.GameObjectAt("NoEntryLine");
            case "row":
            {
                string word = part.Length > 2 ? part[2] : "#";
                var seen = new List<string>();
                foreach (AppRow row in Pane(app, part[1]).GetComponentsInChildren<AppRow>(false))
                {
                    if (!row.Pickable)
                        continue;
                    seen.Add(row.Label);
                    if (!Matches(row.Label, word))
                        continue;
                    Vector2? at = QaGame.ScreenPoint(row.gameObject);
                    GameObject top = at.HasValue ? QaGame.TopAt(at.Value) : null;
                    if (top != null && top.transform.IsChildOf(row.transform))
                    {
                        _lastLabel = row.Label;
                        return row.gameObject;
                    }
                }
                why = $"no pickable, uncovered row on the {part[1]} side matches '{word}'; its pickable rows: {(seen.Count == 0 ? "none" : string.Join(", ", seen))}.";
                return null;
            }
        }
        why = $"unknown target '{target}'.";
        return null;
    }

    private static bool Matches(string label, string word)
    {
        if (word == "#" || word.Length == 0)
            return true;
        if (word[0] == '~')
            return Regex.IsMatch(label, word.Substring(1), RegexOptions.IgnoreCase);
        if (word[0] == '!')
            return label.IndexOf(word.Substring(1), StringComparison.OrdinalIgnoreCase) < 0;
        return label.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static IEnumerator Click(QaRunner runner, QaStep s)
    {
        string id = "click." + QaRunner.Safe(string.IsNullOrEmpty(s.name) ? s.text : s.name);
        GameObject go = Resolve(App(), s.text, out string why);
        Vector2? at = go != null ? QaGame.ScreenPoint(go) : null;
        GameObject top = at.HasValue ? QaGame.TopAt(at.Value) : null;
        if (go == null || top == null || !top.transform.IsChildOf(go.transform))
        {
            runner.Assert(id, Owner, false, go == null ? "no such target" : "covered by " + (top != null ? QaGame.Where(top) : "nothing"), why ?? $"{s.text} is off the screen or covered.");
            yield break;
        }
        yield return runner.Input.Play(QaPaths.Click(at.Value));
        yield return QaGame.Frames(s.n > 0 ? s.n : 12);
    }

    private static Vector2? PointOf(InvestigationApp app, string spot, out string why)
    {
        why = null;
        if (spot == "left" || spot == "right")
            return QaGame.ScreenRect(Pane(app, spot).gameObject)?.center;
        if (spot == "header")
            return QaGame.ScreenRect(QaGame.GameObjectAt("LensHeader"))?.center;
        GameObject go = Resolve(app, spot, out why);
        return go != null ? QaGame.ScreenPoint(go) : null;
    }

    private static IEnumerator Drag(QaRunner runner, QaStep s)
    {
        string id = "drag." + QaRunner.Safe(string.IsNullOrEmpty(s.name) ? s.text : s.name);
        InvestigationApp app = App();
        string[] ends = (s.text ?? string.Empty).Split('>');
        Vector2? from = ends.Length == 2 ? PointOf(app, ends[0].Trim(), out string whyFrom) : null;
        Vector2? to = ends.Length == 2 ? PointOf(app, ends[1].Trim(), out string whyTo) : null;
        if (from == null || to == null)
        {
            runner.Assert(id, Owner, false, "an end is missing", $"lensDrag needs text 'from>to' with both on the screen; the text says '{s.text}'.");
            yield break;
        }
        QaPath path = QaPaths.Drag(QaPathKind.Plain, from.Value, to.Value, new Vector2(Screen.width, Screen.height));
        int rate = Time.captureFramerate;
        Time.captureFramerate = SeqRecorder.Fps; // the rig's drags play at its fixed step
        yield return runner.Input.Play(path);
        yield return QaGame.Frames(s.n > 0 ? s.n : 20);
        Time.captureFramerate = rate;
    }

    private static IEnumerator RightClick(QaRunner runner, QaStep s)
    {
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position }.WithButton(MouseButton.Right));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(runner.Input.Mouse, new MouseState { position = runner.Input.Position });
        yield return QaGame.Frames(s.n > 0 ? s.n : 12);
    }

    private static IEnumerator Registry(QaRunner runner, QaStep s)
    {
        RecordsView view = UnityEngine.Object.FindObjectsByType<RecordsView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).FirstOrDefault() ?? throw new InvalidOperationException("No Registry is open on a side."); // each side has its own view: the open one
        ((TMP_InputField)QaGame.Field(view, "searchInput")).text = s.text;
        view.Search();
        yield return QaGame.Frames(15);
    }

    private static string Doc(LinkTarget t) => t.IsNone ? "none" : t.Item >= 0 ? $"{t.Tab}:{t.Item}" : t.Tab.ToString();

    private static bool DocIs(LinkTarget t, string want) =>
        want.Equals("none", StringComparison.OrdinalIgnoreCase) ? t.IsNone : !t.IsNone && (want.Contains(':') ? Doc(t).Equals(want, StringComparison.OrdinalIgnoreCase) : t.Tab.ToString().Equals(want, StringComparison.OrdinalIgnoreCase));

    private static string Plate(MatchBoard board)
    {
        var active = new List<string>();
        foreach (string field in new[] { "holdText", "matchText", "differText", "infoText" })
            if (QaGame.Field(board, field) is TMP_Text t && t != null && t.transform.parent.gameObject.activeSelf)
                active.Add(field.Replace("Text", string.Empty));
        return active.Count == 0 ? "none" : string.Join("+", active);
    }

    private static IEnumerator Expect(QaRunner runner, QaStep s)
    {
        yield return QaGame.Frames(3);
        InvestigationApp app = App();
        var sides = (LensSides)QaGame.Field(app, "_sides");
        MatchBoard board = app.Board;
        int findings = board != null ? board.Log.Items.Count : 0;
        string plate = board != null ? Plate(board) : "none";
        var toast = QaGame.Field(app, "toast") as Component;
        bool notice = toast != null && toast.gameObject.activeSelf && QaGame.Field(toast, "text") is TMP_Text line && line.text == UiText.Pc.Get("lens.inspect.hint");
        LensDock dock = QaGame.Find<LensDock>();
        int papers = dock == null ? 0 : dock.Items.Count(x => x.Action == ShelfAction.Open && x.Available && x.Target.Tab == AppTab.Documents);
        string measured = $"left {Doc(sides.Left)}, right {Doc(sides.Right)}, selected {sides.Selected}, inspect {(app.Inspecting ? "on" : "off")}, findings {findings}, papers {papers}, pick {(app.Holding ? "held" : "none")}, line {(board != null && board.Line.a != null ? "yes" : "no")}, plate {plate}, notice {(notice ? "hint" : "none")}";
        var bad = new List<string>();
        foreach (string word in (s.text ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            Match m = Regex.Match(word, @"^(\w+)(>=|=)(.*)$");
            if (!m.Success)
            {
                bad.Add($"'{word}' is not key=value");
                continue;
            }
            string key = m.Groups[1].Value.ToLowerInvariant(), op = m.Groups[2].Value, want = m.Groups[3].Value;
            bool ok;
            switch (key)
            {
                case "left": ok = DocIs(sides.Left, want); break;
                case "right": ok = DocIs(sides.Right, want); break;
                case "selected": ok = sides.Selected.ToString().Equals(want, StringComparison.OrdinalIgnoreCase); break;
                case "inspect": ok = app.Inspecting == (want == "on"); break;
                case "findings": ok = findings == int.Parse(want); break;
                case "new": ok = findings - _mark == int.Parse(want); break;
                case "mark": _mark = findings; ok = true; break;
                case "papers": ok = op == ">=" ? papers >= int.Parse(want) : papers == int.Parse(want); break;
                case "pick": ok = app.Holding == (want == "held"); break;
                case "line": ok = (board != null && board.Line.a != null) == (want == "yes"); break;
                case "plate": ok = want.Split('|').Contains(plate); break;
                case "notice": ok = notice == (want == "hint"); break;
                default: ok = false; break;
            }
            if (!ok)
                bad.Add(word);
        }
        runner.Assert("lens." + QaRunner.Safe(string.IsNullOrEmpty(s.name) ? "state" : s.name), Owner, bad.Count == 0, measured,
            bad.Count == 0 ? $"every check held: {s.text}" : $"expected {s.text}; failed: {string.Join(", ", bad)}");
    }
}
#endif
