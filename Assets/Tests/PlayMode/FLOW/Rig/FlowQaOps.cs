#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// The QA ops of FLOW's endings, saves and loads sweep (qa-endings-saves,
/// 2026-11-07; GATE-09: the FLOW checks never measured). Each asserts
/// "&lt;scenario&gt;.&lt;id&gt;" for FLOW. Other tracks' parts (the
/// switchboard, the desk, Home) are read by name through reflection, so a
/// rename there makes a part read "unreadable", never a compile error here.
/// <list type="bullet">
/// <item><c>qaSnap</c> (name; text: an earlier snapshot to compare with;
/// target: the parts to compare, comma-separated, default every part but
/// <c>world</c>): records the run as it stands (day, phase, money, stability,
/// ending, the shift clock's minute, the save file, the world in memory, the
/// switchboard, the case at the desk, the papers on the desk, the pet and
/// Home's bills) into snaps.txt; with text, asserts <c>same.&lt;name&gt;</c>:
/// every named part equals that snapshot's (the saves and worlds compared
/// without what presenting a traveller may mark).</item>
/// <item><c>qaTexts</c> (name; n 1 asserts, 0 reports only; text
/// <c>ending</c>: the judgement words too): reads every text shown (TMP),
/// writes texts_&lt;name&gt;_&lt;w&gt;x&lt;h&gt;.tsv and asserts
/// <c>texts.&lt;name&gt;</c> (no em dash, no cut text: truncated, an
/// ellipsis, overflowing its box, or past the screen's edge; with
/// <c>ending</c>, no judgement word, EndingWords) and
/// <c>overlap.&lt;name&gt;</c> (no two screen texts on top of each
/// other).</item>
/// <item><c>qaMotion</c> (n 1 Reduced Motion on, 0 back to what the player
/// had): the player's choice is kept in a marker preference and put back at
/// the end of the run, or at the next play when the run was stopped.</item>
/// <item><c>qaSkipUntil</c> (name; n the most skips, default 12; f the wallet
/// set before each skip, -1 to leave it; text the ending expected): the cheat
/// menu's Skip day until the run ends, every night's wallet and pet in
/// nights.txt; asserts <c>ending.&lt;name&gt;</c>.</item>
/// <item><c>qaOldSave</c> (text <c>aged</c> or <c>v2</c>): rewrites the
/// test checkout's own save slot (SaveSystem.Folder, never a player's):
/// <c>aged</c>, the save as a 2026-10-09 build left it (an upgrade and an order
/// of an id this build does not have, a flag nobody reads, no coat, no guide
/// progress, no world pulls, no statement rows, no back issues), the morning
/// snapshot too; <c>v2</c>, the save at version 2 with the slot's other files
/// moved aside (renamed, never deleted).</item>
/// <item><c>qaTitle</c> (name; n 1 Continue expected, 0 hidden): asserts
/// <c>title.&lt;name&gt;</c>: the Title's panel shows, with Continue as
/// expected.</item>
/// <item><c>qaHome</c> (name; f timeout, default 90 s): plays Home's evening
/// as the player does, clicking each panel's continue button with the virtual
/// mouse (a still of each panel) through Sleep; asserts
/// <c>home.&lt;name&gt;</c>: every press reached its button and Sleep was
/// pressed.</item>
/// </list>
/// </summary>
public static class FlowQaOps
{
    private const string Owner = "FLOW";

    /// <summary>The preference that holds the player's own Reduced Motion choice while a run has changed it.</summary>
    private const string MotionMarker = "TimeDesk.QaMotionRestore";

    /// <summary>The ellipsis TMP puts at the end of a text it cut (U+2026).</summary>
    private static readonly char Ellipsis = (char)0x2026;

    /// <summary>The parts qaSnap compares when the step names none (the world in memory is compared only when named).</summary>
    private static readonly string[] DefaultParts = { "scene", "day", "phase", "money", "stability", "ending", "hour", "save", "board", "desk", "papers", "pet", "home" };

    /// <summary>Home's continue buttons in the evening's order, Sleep last (HomeUIController's fields).</summary>
    private static readonly string[] HomeButtons = { "expensesContinueButton", "petContinueButton", "shopContinueButton", "slotContinueButton", "sleepButton" };

    /// <summary>This play session's snapshots, by name.</summary>
    private static readonly Dictionary<string, Dictionary<string, string>> Snaps = new Dictionary<string, Dictionary<string, string>>();

#if UNITY_EDITOR
    /// <summary>Also known in edit mode, so the scenario tests read the recipes with known ops.</summary>
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterInEditor() => Register();
#endif

    /// <summary>Registers the ops (each a literal <c>QaRunner.RegisterOp("name"</c> call: tools/qa/test_seqlib.py reads them), and at each play puts back a Reduced Motion choice a stopped run left changed.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterAtPlay()
    {
        Register();
        Snaps.Clear();
        PutMotionBack();
    }

    private static void Register()
    {
        if (!QaRunner.IsKnownOp("qaSnap"))
            QaRunner.RegisterOp("qaSnap", Snap);
        if (!QaRunner.IsKnownOp("qaTexts"))
            QaRunner.RegisterOp("qaTexts", Texts);
        if (!QaRunner.IsKnownOp("qaMotion"))
            QaRunner.RegisterOp("qaMotion", Motion);
        if (!QaRunner.IsKnownOp("qaSkipUntil"))
            QaRunner.RegisterOp("qaSkipUntil", SkipUntil);
        if (!QaRunner.IsKnownOp("qaOldSave"))
            QaRunner.RegisterOp("qaOldSave", OldSave);
        if (!QaRunner.IsKnownOp("qaTitle"))
            QaRunner.RegisterOp("qaTitle", TitleOp);
        if (!QaRunner.IsKnownOp("qaHome"))
            QaRunner.RegisterOp("qaHome", Home);
    }

    // -----------------------------------------------------------------
    // qaSnap
    // -----------------------------------------------------------------

    private static IEnumerator Snap(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(2);
        Dictionary<string, string> now = Take();
        string name = string.IsNullOrEmpty(step.name) ? "snap" : step.name;
        Snaps[name] = now;
        var log = new StringBuilder($"== {name} ({Screen.width}x{Screen.height}, frame {Time.frameCount})\n");
        foreach (KeyValuePair<string, string> part in now)
            log.Append(part.Key).Append(": ").Append(part.Key == "save" || part.Key == "world" ? Digest(part.Value) : part.Value).Append('\n');
        File.AppendAllText(Path.Combine(runner.OutDir, "snaps.txt"), log.ToString());
        if (string.IsNullOrEmpty(step.text))
            yield break;

        string[] parts = string.IsNullOrEmpty(step.target) ? DefaultParts : step.target.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
        bool known = Snaps.TryGetValue(step.text, out Dictionary<string, string> then);
        var differ = new List<string>();
        if (known)
            foreach (string part in parts)
            {
                now.TryGetValue(part, out string a);
                then.TryGetValue(part, out string b);
                if (a != b)
                    differ.Add(part == "save" || part == "world" ? $"{part}: {FirstDifference(b, a)}" : $"{part}: '{b}' then, '{a}' now");
            }
        runner.Assert($"same.{name}", Owner, known && differ.Count == 0,
            !known ? $"no snapshot named '{step.text}'" : differ.Count == 0 ? $"{parts.Length} part(s) as at '{step.text}': {string.Join(", ", parts)}" : $"{differ.Count} of {parts.Length} part(s) differ from '{step.text}': {string.Join(", ", differ.Select(d => d.Split(':')[0]))}",
            "Saves and loads (qa-endings-saves): a quit to the Title and Continue resume the run where its save was made, the day's morning or Home after the shift (the day's shift replays from its briefing: quitting forfeits the day)"
            + (differ.Count > 0 ? ". " + string.Join(" | ", differ) : string.Empty));
    }

    /// <summary>The run as it stands, part by part (a part that cannot be read says why).</summary>
    private static Dictionary<string, string> Take()
    {
        RunManager run = RunManager.HasInstance ? RunManager.Instance : null;
        WorldState w = run != null ? run.World : null;
        var parts = new Dictionary<string, string>();
        void Part(string key, Func<string> read)
        {
            try
            {
                parts[key] = read();
            }
            catch (Exception e)
            {
                parts[key] = $"unreadable ({e.GetType().Name}: {e.Message})";
            }
        }
        Part("scene", () => SceneManager.GetActiveScene().name);
        Part("day", () => w != null ? w.day.ToString() : "no run");
        Part("phase", () => w != null ? w.phase.ToString() : "no run");
        Part("money", () => w != null ? w.money.ToString() : "no run");
        Part("stability", () => w != null ? w.stabilityHundredths.ToString() : "no run");
        Part("ending", () => w != null ? w.endingId ?? string.Empty : "no run");
        Part("hour", () => ShiftClockDriver.Live != null ? ShiftClockDriver.Live.MinuteOfDay.ToString("0.0") : "no clock");
        Part("save", () => File.Exists(SaveSystem.SavePath) ? Comparable(File.ReadAllText(SaveSystem.SavePath)) : "missing");
        Part("world", () => w != null ? Comparable("{\"version\":0,\"world\":" + JsonUtility.ToJson(w) + "}") : "no run");
        Part("board", Board);
        Part("desk", Desk);
        Part("papers", Papers);
        Part("pet", () => w != null && w.pet != null ? $"{w.pet.kind} '{w.pet.name}' coat '{w.pet.coat}' hunger {w.pet.hunger} cold {w.pet.cold} boredom {w.pet.boredom} sickness {w.pet.sickness} welfare {w.pet.welfareNights} taken {w.pet.taken}" : "no pet");
        Part("home", HomeBills);
        return parts;
    }

    /// <summary>The save as JSON without what presenting a traveller may legitimately mark (a premade met, a returning traveller come back, the desk guide's progress), as FlowOps' flowRunState compares; the text itself when it does not read as a save.</summary>
    private static string Comparable(string json)
    {
        SaveCopy copy;
        try
        {
            copy = JsonUtility.FromJson<SaveCopy>(json);
        }
        catch (ArgumentException)
        {
            return json;
        }
        if (copy == null || copy.world == null)
            return json;
        WorldState w = copy.world;
        string marker = FlagKeys.PremadeMet("\u0001");
        int at = marker.IndexOf('\u0001');
        string prefix = marker.Substring(0, at), suffix = marker.Substring(at + 1);
        w.flags?.RemoveAll(f => f != null && f.StartsWith(prefix, StringComparison.Ordinal) && f.EndsWith(suffix, StringComparison.Ordinal));
        if (w.returns != null)
            foreach (ReturningTraveller r in w.returns)
                if (r != null)
                    r.returned = false;
        w.guide = new GuideState();
        return copy.version + "|" + JsonUtility.ToJson(w);
    }

    /// <summary>A save file read for comparison and rewriting (the world without its version wrapper's private type).</summary>
    [Serializable]
    private sealed class SaveCopy
    {
        public int version;
        public WorldState world;
    }

    /// <summary>A long text's length and a short hash, for snaps.txt.</summary>
    private static string Digest(string text)
    {
        if (text == null)
            return "null";
        unchecked
        {
            uint h = 2166136261;
            foreach (char c in text)
                h = (h ^ c) * 16777619;
            return $"{text.Length} chars, hash {h:x8}";
        }
    }

    /// <summary>Where two texts first differ, with a little of each around it.</summary>
    private static string FirstDifference(string a, string b)
    {
        if (a == null || b == null)
            return a == null ? "the first is missing" : "the second is missing";
        int i = 0;
        while (i < a.Length && i < b.Length && a[i] == b[i])
            i++;
        int from = Math.Max(0, i - 50);
        string Around(string s) => i >= s.Length ? "(end)" : s.Substring(from, Math.Min(s.Length - from, 140));
        return $"first differ at character {i}: '{Around(a)}' then, '{Around(b)}' now";
    }

    /// <summary>The switchboard's modules and calls (SwitchboardView's board, read by name).</summary>
    private static string Board()
    {
        Type view = QaGame.TypeNamed("SwitchboardView");
        PropertyInfo current = view != null ? view.GetProperty("Current", BindingFlags.Public | BindingFlags.Static) : null;
        object live = current != null ? current.GetValue(null) : null;
        if (live == null || (live is Object o && o == null))
            return "no board";
        object board = QaGame.Field(live, "_board");
        if (board == null)
            return "not built";
        var text = new StringBuilder();
        PropertyInfo waiting = board.GetType().GetProperty("WaitingTotal");
        if (waiting != null)
            text.Append("waiting ").Append(waiting.GetValue(board)).Append(';');
        if (board.GetType().GetProperty("Modules")?.GetValue(board) is IEnumerable modules)
            foreach (object m in modules)
                text.Append(" [").Append(Dump(m)).Append(']');
        return text.ToString();
    }

    /// <summary>The case at the desk: who, from where, and every paper's fields.</summary>
    private static string Desk()
    {
        Component ui = QaGame.FindComponent("InvestigationUIController");
        if (!(QaGame.Field(ui, "_currentCase") is CaseInstance c))
            return "nobody at the desk";
        var text = new StringBuilder($"#{c.caseIndex} seed {c.caseSeed} '{c.visitorDisplayName}' {c.kind} {(c.claimedNation != null ? c.claimedNation.id : "-")}/{(c.claimedEra != null ? c.claimedEra.id : "-")}");
        foreach (DocumentInstance d in c.documents)
        {
            text.Append(" | ").Append(d.template != null ? d.template.name : "?").Append(' ').Append(d.serial).Append(':');
            foreach (DocumentField f in d.fields)
                text.Append(' ').Append(f.category).Append('=').Append(f.value);
        }
        return text.ToString();
    }

    /// <summary>The papers lying in the office (DeskDocument, read by name): how many, by name.</summary>
    private static string Papers()
    {
        Type type = QaGame.TypeNamed("DeskDocument");
        if (type == null)
            return "no DeskDocument type";
        Object[] all = Object.FindObjectsByType(type, FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        return $"{all.Length}: " + string.Join(", ", all.Select(p => p.name).OrderBy(n => n, StringComparer.Ordinal));
    }

    /// <summary>Home's arrival and tonight's care (HomeManager's bill, care and household, read by name).</summary>
    private static string HomeBills()
    {
        Type type = QaGame.TypeNamed("HomeManager");
        Object home = type != null ? Object.FindAnyObjectByType(type) : null;
        if (home == null)
            return "not at Home";
        return $"bill [{Dump(QaGame.Field(home, "_bill"))}] care [{Dump(QaGame.Field(home, "_care"))}] household {QaGame.Field(home, "_household")}";
    }

    /// <summary>An object's public fields and readable properties as "name value" pairs (structs and classes alike).</summary>
    private static string Dump(object o)
    {
        if (o == null)
            return "null";
        Type t = o.GetType();
        var pairs = new List<string>();
        foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            pairs.Add($"{f.Name} {f.GetValue(o)}");
        foreach (PropertyInfo p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (p.CanRead && p.GetIndexParameters().Length == 0)
            {
                object v;
                try
                {
                    v = p.GetValue(o);
                }
                catch (Exception e)
                {
                    v = e.GetType().Name;
                }
                pairs.Add($"{p.Name} {v}");
            }
        return string.Join(", ", pairs);
    }

    // -----------------------------------------------------------------
    // qaTexts
    // -----------------------------------------------------------------

    /// <summary>One text shown: where, what, and what is wrong with it.</summary>
    private sealed class Shown
    {
        public TMP_Text Text;
        public Canvas Root;
        public bool Screen;
        public Rect Rect;
        public string Words;
        public readonly List<string> Flags = new List<string>();
    }

    private static IEnumerator Texts(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(3);
        string name = string.IsNullOrEmpty(step.name) ? "texts" : step.name;
        bool ending = string.Equals(step.text, "ending", StringComparison.OrdinalIgnoreCase);
        var screen = new Rect(0f, 0f, UnityEngine.Screen.width, UnityEngine.Screen.height);
        var shown = new List<Shown>();
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            Shown s = Read(t, screen);
            if (s != null)
                shown.Add(s);
        }

        var problems = new List<string>();
        foreach (Shown s in shown)
        {
            foreach (string p in EndingWords.Problems(s.Words))
                if (ending || p.Contains("em dash"))
                    s.Flags.Add(p);
            if (s.Flags.Any(f => f.StartsWith("covered", StringComparison.Ordinal)))
                continue; // under a screen above: the player does not see it
            foreach (string flag in s.Flags.Where(f => !f.StartsWith("world", StringComparison.Ordinal)))
                problems.Add($"{PathOf(s.Text)} '{Short(s.Words)}': {flag}");
        }

        var overlaps = new List<string>();
        List<Shown> flat = shown.Where(s => s.Screen && !s.Flags.Any(f => f.StartsWith("covered", StringComparison.Ordinal))).ToList();
        for (int i = 0; i < flat.Count; i++)
            for (int j = i + 1; j < flat.Count; j++)
            {
                Shown a = flat[i], b = flat[j];
                if (a.Text.transform.IsChildOf(b.Text.transform) || b.Text.transform.IsChildOf(a.Text.transform))
                    continue;
                float shared = Shared(a.Rect, b.Rect);
                float smaller = Mathf.Min(a.Rect.width * a.Rect.height, b.Rect.width * b.Rect.height);
                if (smaller > 1f && shared / smaller > 0.15f)
                    overlaps.Add($"'{Short(a.Words)}' ({PathOf(a.Text)}) and '{Short(b.Words)}' ({PathOf(b.Text)}): {shared / smaller:P0} of the smaller");
            }

        string file = Path.Combine(runner.OutDir, $"texts_{name}_{UnityEngine.Screen.width}x{UnityEngine.Screen.height}.tsv");
        using (var w = new StreamWriter(file, false, new UTF8Encoding(false)))
        {
            w.WriteLine("canvas\tsort\tx\ty\tw\th\tflags\tpath\ttext");
            foreach (Shown s in shown.OrderByDescending(s => s.Rect.yMax).ThenBy(s => s.Rect.xMin))
                w.WriteLine(string.Join("\t", s.Screen ? "screen" : "world", s.Root != null ? s.Root.sortingOrder.ToString() : "-", s.Rect.x.ToString("0"), s.Rect.y.ToString("0"),
                    s.Rect.width.ToString("0"), s.Rect.height.ToString("0"), string.Join("; ", s.Flags), PathOf(s.Text), s.Words.Replace("\t", " ").Replace("\r", string.Empty).Replace("\n", " / ")));
        }

        bool assert = step.n == 1;
        string where = $"{shown.Count} text(s) shown at {UnityEngine.Screen.width}x{UnityEngine.Screen.height} ({shown.Count(s => s.Screen)} on screen canvases); texts_{name}_*.tsv lists each";
        if (assert)
        {
            runner.Assert($"texts.{name}", Owner, problems.Count == 0, $"{problems.Count} problem(s); {where}",
                "Every text whole: no em dash, nothing truncated, no ellipsis, nothing past its box or the screen's edge" + (ending ? ", no judgement word (EndingWords: Saleh 2026-09-29, the outcome depends on your choices)" : string.Empty)
                + (problems.Count > 0 ? ". " + string.Join(" | ", problems.Take(30)) : string.Empty));
            runner.Assert($"overlap.{name}", Owner, overlaps.Count == 0, $"{overlaps.Count} overlap(s)",
                "No two texts on screen canvases cover each other by more than 15 % of the smaller" + (overlaps.Count > 0 ? ". " + string.Join(" | ", overlaps.Take(20)) : string.Empty));
        }
        else
            File.AppendAllText(Path.Combine(runner.OutDir, "texts_report.txt"),
                $"== {name} {where}\nproblems {problems.Count}:\n  {string.Join("\n  ", problems)}\noverlaps {overlaps.Count}:\n  {string.Join("\n  ", overlaps)}\n");
    }

    /// <summary>A text as the player sees it, or null when it shows nothing (inactive, empty, transparent, off screen, faded out with its group).</summary>
    private static Shown Read(TMP_Text t, Rect screen)
    {
        if (t == null || !t.isActiveAndEnabled)
            return null;
        string words = t.GetParsedText();
        if (string.IsNullOrWhiteSpace(words) || t.color.a < 0.05f || GroupAlpha(t.transform) < 0.05f)
            return null;
        Canvas canvas = t.canvas;
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        if (canvas != null && !canvas.enabled)
            return null;
        bool onScreen = root != null && root.renderMode != RenderMode.WorldSpace;
        Camera camera = root != null && root.renderMode == RenderMode.ScreenSpaceCamera ? root.worldCamera : root == null || root.renderMode == RenderMode.WorldSpace ? QaGame.PointerCamera() : null;
        Rect? rect = ScreenRectOf(t, t.textBounds, camera);
        if (rect == null || !rect.Value.Overlaps(screen))
            return null;
        var s = new Shown { Text = t, Root = root, Screen = onScreen, Rect = rect.Value, Words = words };

        if (!onScreen)
            s.Flags.Add("world space (read only)");
        if (t.isTextTruncated)
            s.Flags.Add("truncated");
        if (t.isTextOverflowing && t.overflowMode != TextOverflowModes.Overflow && t.overflowMode != TextOverflowModes.ScrollRect)
            s.Flags.Add($"overflowing ({t.overflowMode})");
        if (words.IndexOf(Ellipsis) >= 0 && (t.text ?? string.Empty).IndexOf(Ellipsis) < 0)
            s.Flags.Add("ellipsis added");
        if (onScreen && Masked(t.transform) == false)
        {
            if (rect.Value.xMin < -1f || rect.Value.yMin < -1f || rect.Value.xMax > screen.width + 1f || rect.Value.yMax > screen.height + 1f)
                s.Flags.Add("past the screen's edge");
            Rect? box = ScreenRectOf(t, new Bounds(t.rectTransform.rect.center, t.rectTransform.rect.size), camera);
            if (box != null && t.overflowMode == TextOverflowModes.Overflow && Spill(rect.Value, box.Value) > 4f * screen.height / 1080f)
                s.Flags.Add($"past its box by {Spill(rect.Value, box.Value):0} px");
        }
        if (onScreen && Covered(t, rect.Value))
            s.Flags.Add("covered by a screen above");
        return s;
    }

    /// <summary>The product of the CanvasGroup alphas over <paramref name="t"/> (a faded panel shows nothing).</summary>
    private static float GroupAlpha(Transform t)
    {
        float alpha = 1f;
        for (Transform p = t; p != null; p = p.parent)
            if (p.TryGetComponent(out CanvasGroup g) && g.enabled)
            {
                alpha *= g.alpha;
                if (g.ignoreParentGroups)
                    break;
            }
        return alpha;
    }

    /// <summary>True when a mask (RectMask2D or Mask) clips <paramref name="t"/>: a scroll view's text may run past it on purpose.</summary>
    private static bool Masked(Transform t)
    {
        for (Transform p = t.parent; p != null; p = p.parent)
            if (p.GetComponent<RectMask2D>() != null || p.GetComponent<Mask>() != null)
                return true;
        return false;
    }

    /// <summary>The screen rectangle of <paramref name="local"/> (in <paramref name="t"/>'s local space) through <paramref name="camera"/> (null: an overlay canvas); null when behind the camera.</summary>
    private static Rect? ScreenRectOf(TMP_Text t, Bounds local, Camera camera)
    {
        if (local.size.sqrMagnitude <= 0f)
            return null;
        float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
        Vector3 c = local.center, e = local.extents;
        for (int i = 0; i < 4; i++)
        {
            var corner = new Vector3(c.x + (i % 2 == 0 ? -e.x : e.x), c.y + (i < 2 ? -e.y : e.y), c.z);
            Vector3 world = t.transform.TransformPoint(corner);
            Vector2 p;
            if (camera == null)
                p = world;
            else
            {
                Vector3 sp = camera.WorldToScreenPoint(world);
                if (sp.z <= 0f)
                    return null;
                p = sp;
            }
            xMin = Mathf.Min(xMin, p.x);
            yMin = Mathf.Min(yMin, p.y);
            xMax = Mathf.Max(xMax, p.x);
            yMax = Mathf.Max(yMax, p.y);
        }
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    /// <summary>How far <paramref name="drawn"/> runs past <paramref name="box"/> on its farthest side (px; 0 inside).</summary>
    private static float Spill(Rect drawn, Rect box) =>
        Mathf.Max(0f, Mathf.Max(Mathf.Max(box.xMin - drawn.xMin, drawn.xMax - box.xMax), Mathf.Max(box.yMin - drawn.yMin, drawn.yMax - box.yMax)));

    /// <summary>True when the topmost thing the pointer would meet at the text's centre is on another root canvas sorted above the text's.</summary>
    private static bool Covered(TMP_Text t, Rect rect)
    {
        GameObject top = QaGame.TopAt(rect.center);
        if (top == null)
            return false;
        Canvas own = t.canvas != null ? t.canvas.rootCanvas : null;
        Canvas over = top.GetComponentInParent<Canvas>();
        over = over != null ? over.rootCanvas : null;
        return own != null && over != null && over != own && over.sortingOrder > own.sortingOrder;
    }

    /// <summary>The area two rectangles share.</summary>
    private static float Shared(Rect a, Rect b)
    {
        float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
        return w > 0f && h > 0f ? w * h : 0f;
    }

    private static string PathOf(Component c)
    {
        var names = new List<string>();
        for (Transform t = c.transform; t != null; t = t.parent)
            names.Add(t.name);
        names.Reverse();
        return string.Join("/", names);
    }

    private static string Short(string s)
    {
        s = (s ?? string.Empty).Replace("\n", " / ").Replace("\r", string.Empty);
        return s.Length > 60 ? s.Substring(0, 57) + "..." : s;
    }

    // -----------------------------------------------------------------
    // qaMotion
    // -----------------------------------------------------------------

    private static IEnumerator Motion(QaRunner runner, QaStep step)
    {
        if (step.n == 1)
        {
            string marker = PlayerPrefKeys.For(MotionMarker);
            if (!PlayerPrefs.HasKey(marker))
            {
                PlayerPrefs.SetString(marker, MotionPreference.Reduced ? "reduced" : "full");
                PlayerPrefs.Save();
            }
            QaRunner.Finished -= OnRunFinished;
            QaRunner.Finished += OnRunFinished;
            MotionPreference.Reduced = true;
        }
        else
            PutMotionBack();
        yield return QaGame.Frames(5);
    }

    private static void OnRunFinished(QaRunReport report)
    {
        QaRunner.Finished -= OnRunFinished;
        PutMotionBack();
    }

    /// <summary>Puts back the player's Reduced Motion choice a run changed (the marker preference), then drops the marker; nothing when no run changed it.</summary>
    private static void PutMotionBack()
    {
        string marker = PlayerPrefKeys.For(MotionMarker);
        if (!PlayerPrefs.HasKey(marker))
            return;
        bool reduced = PlayerPrefs.GetString(marker) == "reduced";
        PlayerPrefs.DeleteKey(marker);
        MotionPreference.Reduced = reduced;
    }

    // -----------------------------------------------------------------
    // qaSkipUntil
    // -----------------------------------------------------------------

    private static IEnumerator SkipUntil(QaRunner runner, QaStep step)
    {
        RunManager run = RunManager.Instance ?? throw new InvalidOperationException("No run.");
        string name = string.IsNullOrEmpty(step.name) ? "skips" : step.name;
        int most = step.n > 0 ? step.n : 12;
        var nights = new List<string>();
        string reached = null;
        for (int i = 0; i < most && reached == null; i++)
        {
            WorldState before = run.World;
            if (step.f >= 0f && before.money != Mathf.RoundToInt(step.f))
                DevCheats.Run(DevCheat.AddMoney, run, Mathf.RoundToInt(step.f) - before.money, 0f, null);
            int day = before.day, money = before.money;
            DevCheats.Run(DevCheat.SkipDay, run, 0, 0f, null);
            yield return QaGame.Until(() => !SceneTransit.Changing && (Ended(run)
                    ? SceneManager.GetSceneByName(run.Config.titleSceneName).isLoaded
                    : QaGame.OfficeLoaded() && QaGame.Day() == day + 1 && QaGame.BriefingShown()),
                120f, $"day {day}'s skip to land");
            WorldState after = run.World;
            PetState pet = after.pet;
            nights.Add($"day {day}: wallet {money} -> {after.money}, pet hunger {pet?.hunger} cold {pet?.cold} boredom {pet?.boredom} sickness {pet?.sickness} welfare {pet?.welfareNights} taken {pet?.taken}"
                       + (Ended(run) ? $", ending '{after.endingId}'" : string.Empty));
            if (Ended(run))
                reached = after.endingId;
        }
        File.AppendAllText(Path.Combine(runner.OutDir, "nights.txt"), $"== {name}\n{string.Join("\n", nights)}\n");
        bool expected = string.IsNullOrEmpty(step.text) || reached == step.text;
        runner.Assert($"ending.{name}", Owner, reached != null && expected,
            $"'{reached ?? "none"}' after {nights.Count} skip(s)" + (string.IsNullOrEmpty(step.text) ? string.Empty : $" (expected '{step.text}')"),
            "The endings by play (qa-endings-saves): Skip day (its close, Home's night on the default care, Sleep) until the run ends. " + string.Join("; ", nights));
    }

    private static bool Ended(RunManager run) => run.World != null && !string.IsNullOrEmpty(run.World.endingId);

    // -----------------------------------------------------------------
    // qaOldSave
    // -----------------------------------------------------------------

    private static IEnumerator OldSave(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(2);
        string path = SaveSystem.SavePath;
        if (!File.Exists(path))
            throw new InvalidOperationException($"No save at '{path}' to age.");
        SaveCopy copy = JsonUtility.FromJson<SaveCopy>(File.ReadAllText(path));
        string what;
        if (string.Equals(step.text, "v2", StringComparison.OrdinalIgnoreCase))
        {
            var moved = new List<string>();
            foreach (string file in SaveSystem.Files)
                if (file != path && File.Exists(file))
                {
                    string aside = file + ".qa-aside-" + DateTime.Now.ToString("HHmmssfff");
                    File.Move(file, aside);
                    moved.Add(Path.GetFileName(aside));
                }
            copy.version = 2;
            File.WriteAllText(path, JsonUtility.ToJson(copy, true));
            what = $"the save at version 2 (day {copy.world.day}); moved aside: {(moved.Count > 0 ? string.Join(", ", moved) : "nothing")}";
        }
        else
        {
            WorldState w = copy.world;
            const string gone = "upgrade_magnifier";
            w.unlockedUpgradeIds.Add(gone);
            w.orders.Add(new OrderEntry { upgradeId = gone, orderedDay = Math.Max(1, w.day - 1), price = 40 });
            w.SetFlag("legacy_1009_flag");
            if (w.pet != null)
                w.pet.coat = string.Empty;
            w.guide = new GuideState();
            w.pulls.Clear();
            w.leads.Clear();
            w.accountDays.Clear();
            w.newsArchive.Clear();
            string json = JsonUtility.ToJson(copy, true);
            File.WriteAllText(path, json);
            File.WriteAllText(Path.Combine(SaveSystem.Folder, "nope_morning.json"), json);
            what = $"day {w.day} aged as a 2026-10-09 build left it: upgrade and order '{gone}', flag legacy_1009_flag, no coat, no guide, no pulls or leads, no statement rows, no back issues";
        }
        File.AppendAllText(Path.Combine(runner.OutDir, "oldsave.txt"), $"{step.text}: {what} ({path})\n");
    }

    // -----------------------------------------------------------------
    // qaTitle
    // -----------------------------------------------------------------

    private static IEnumerator TitleOp(QaRunner runner, QaStep step)
    {
        yield return QaGame.Frames(5);
        Component title = QaGame.FindComponent("TitleUIController");
        var panel = QaGame.Field(title, "titlePanel") as GameObject;
        var resume = QaGame.Field(title, "continueButton") as Button;
        bool shown = resume != null && resume.isActiveAndEnabled;
        bool want = step.n == 1, open = panel != null && panel.activeInHierarchy;
        runner.Assert($"title.{(string.IsNullOrEmpty(step.name) ? "title" : step.name)}", Owner, open && shown == want,
            $"title panel shown {open}, Continue shown {shown} (expected {want}); save slot: {string.Join(", ", SaveSystem.Files.Where(File.Exists).Select(Path.GetFileName))}",
            "The Title offers Continue only for a save this build can continue (RUN-SAFE, BUG-35a; BLD-09: an old save)");
    }

    // -----------------------------------------------------------------
    // qaHome
    // -----------------------------------------------------------------

    private static IEnumerator Home(QaRunner runner, QaStep step)
    {
        string name = string.IsNullOrEmpty(step.name) ? "home" : step.name;
        Component ui = QaGame.FindComponent("HomeUIController");
        float end = Time.realtimeSinceStartup + (step.f > 0f ? step.f : 90f);
        var pressed = new List<string>();
        var blocked = new List<string>();
        bool slept = false;
        while (!slept && Time.realtimeSinceStartup < end && ui != null)
        {
            Button next = null;
            string which = null;
            foreach (string field in HomeButtons)
                if (QaGame.Field(ui, field) is Button b && b != null && b.isActiveAndEnabled && b.IsInteractable() && GroupAlpha(b.transform) > 0.95f)
                {
                    next = b;
                    which = field;
                    break;
                }
            if (next == null)
            {
                yield return null;
                continue;
            }
            yield return QaGame.Frames(8);
            if (next == null || !next.isActiveAndEnabled)
                continue;
            runner.Recorder.Still(Path.Combine(runner.OutDir, "stills", $"{name}_{which}_{UnityEngine.Screen.width}x{UnityEngine.Screen.height}.png"));
            yield return QaGame.Until(() => !runner.Recorder.StillPending, 10f, "the still");
            Vector2? at = QaGame.ScreenPoint(next.gameObject);
            GameObject top = at.HasValue ? QaGame.TopAt(at.Value) : null;
            if (top != null && top.transform.IsChildOf(next.transform))
                yield return runner.Input.Play(QaPaths.Click(at.Value));
            else
            {
                blocked.Add($"{which} (under the pointer: {(top != null ? QaGame.Where(top) : "nothing")})");
                next.onClick.Invoke();
            }
            pressed.Add(which);
            slept = which == "sleepButton";
            yield return QaGame.Frames(5);
        }
        runner.Assert($"home.{name}", Owner, slept && blocked.Count == 0,
            $"pressed {string.Join(" > ", pressed)}; slept {slept}; {blocked.Count} blocked",
            "Home's evening played by the virtual mouse, each panel's button where it shows, through Sleep" + (blocked.Count > 0 ? ". Blocked: " + string.Join(" | ", blocked) : string.Empty));
    }
}
#endif
