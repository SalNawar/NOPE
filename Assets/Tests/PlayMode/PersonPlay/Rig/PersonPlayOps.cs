#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

/// <summary>
/// The rig ops of person_d1 (night 2026-11-11, the "person play" proof): day 1 played as the tutorial tells a new
/// player, by real input only. Every op presses and drags with the virtual mouse and keys, never calls a game
/// method in place of a click. Where a step cannot be done by real input, the op writes a FINDING line, says
/// "RIG HELP" and uses the smallest help; the findings go to person_report.txt (never to a red assertion, so a
/// game fault never hides the rest of the run).
/// <list type="bullet">
/// <item><c>personWatch</c>: starts listening for the verdict of each case (GameManager.Resolved) and clears the
/// readouts and the decision.</item>
/// <item><c>personStep</c> (n = the tutorial step the plate must show, name = the still): writes the plate's header,
/// line and arrow, the size of each paper's click box on the screen, and takes the still.</item>
/// <item><c>personPick</c> (text = Destination or Valid Until, name): in inspect mode the pointer goes onto the
/// passport, waits for the lift, and presses the named value box on the lifted paper.</item>
/// <item><c>personRulebook</c> (name): takes the folder out of its pocket by a click on the painted book when it is
/// in, turns the reading view on, and presses the day's rule row (a drag of the folder clear of the passport when
/// the passport covers the rows).</item>
/// <item><c>personCalendar</c> (name): presses the calendar's date line (the office view when the reading view does
/// not show it).</item>
/// <item><c>personFace</c> (name): the office view, the traveller's hit zone pressed in inspect mode (their face is held;
/// <c>personPick</c> with the text Photo then pairs it with the passport's photo, the one pair that draws a readout).</item>
/// <item><c>personLook</c> (name): the pointer goes onto the passport in inspect mode and a still is taken lifted (the stamp's ink at a readable size).</item>
/// <item><c>personSettle</c>: waits until the camera is still (a stamp aimed on a moving camera lands off the pointer's spot).</item>
/// <item><c>personReadout</c> (text = a label, name): reads the compare readout at the bottom centre and the line's
/// look, keeps them for the decision and takes the still.</item>
/// <item><c>personDecide</c>: the decision of the case from the readouts kept (a MISMATCH, or a red line when no
/// readout is drawn: deny; else approve); <c>stampAim</c> with the text "readout" stamps it.</item>
/// <item><c>personVerdict</c> (name): asserts the case was scored correct and no citation slip came.</item>
/// </list>
/// </summary>
public static class PersonPlayOps
{
    private const string Owner = "PERSON-PLAY";

    /// <summary>The decision the readouts give ("Approved" or "Denied"); null before personDecide. StampRig reads it for the text "readout".</summary>
    public static string Decision { get; private set; }

    private static readonly List<string> Readouts = new List<string>();
    private static bool _mismatch;
    private static CaseVerdict _verdict;
    private static bool _accepted;
    private static GameManager _watched;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
#endif
    private static void Register()
    {
        QaRunner.RegisterOp("personWatch", Watch);
        QaRunner.RegisterOp("personStep", StepOp);
        QaRunner.RegisterOp("personPick", PickOp);
        QaRunner.RegisterOp("personRulebook", RulebookOp);
        QaRunner.RegisterOp("personCalendar", CalendarOp);
        QaRunner.RegisterOp("personReadout", ReadoutOp);
        QaRunner.RegisterOp("personFace", FaceOp);
        QaRunner.RegisterOp("personLook", LookOp);
        QaRunner.RegisterOp("personSettle", SettleOp);
        QaRunner.RegisterOp("personDecide", DecideOp);
        QaRunner.RegisterOp("personVerdict", VerdictOp);
    }

    // ---------------- helpers ----------------

    private static string Name(QaStep s) => QaRunner.Safe(string.IsNullOrEmpty(s.name) ? "step" : s.name);

    private static void Rep(QaRunner r, string line) => File.AppendAllText(Path.Combine(r.OutDir, "person_report.txt"), line + "\n");

    private static void Find(QaRunner r, string sev, string what, string evidence) => Rep(r, $"FINDING {sev}: {what} | {evidence}");

    private static string Px(Rect? rect) => rect is Rect x ? string.Format(CultureInfo.InvariantCulture, "{0:0}x{1:0} px at ({2:0},{3:0})", x.width, x.height, x.center.x, x.center.y) : "off screen";

    private static IEnumerator Secs(float seconds)
    {
        for (float end = Time.unscaledTime + seconds; Time.unscaledTime < end;)
            yield return null;
    }

    private static IEnumerator Still(QaRunner r, string name)
    {
        r.Recorder.Still(Path.Combine(r.OutDir, "stills", $"{QaRunner.Safe(name)}_{Screen.width}x{Screen.height}.png"));
        yield return QaGame.Until(() => !r.Recorder.StillPending, 10f, "the still");
    }

    private static bool Related(GameObject top, GameObject go) =>
        top != null && go != null && (top.transform.IsChildOf(go.transform) || go.transform.IsChildOf(top.transform));

    /// <summary>The reading view (true) or the office view (false) by TAB, as a player turns it; then the camera's blend.</summary>
    private static IEnumerator ToView(QaRunner r, bool reading)
    {
        DeskView view = QaGame.Find<DeskView>();
        if (view == null || view.IsOn == reading)
            yield break;
        yield return r.Input.Tap(Key.Tab);
        yield return QaGame.Until(() => view.IsOn == reading, 3f, reading ? "the reading view" : "the office view");
        yield return Secs(1.2f);
        yield return QaGame.UntilCameraStill(8f);
    }

    /// <summary>Presses <paramref name="go"/> at its screen point with the virtual mouse; false (and the press still made) when something else is under the pointer there.</summary>
    private static IEnumerator ClickOn(QaRunner r, GameObject go, string what, Action<bool> reached)
    {
        Vector2? p = QaGame.ScreenPoint(go);
        if (p == null)
        {
            Find(r, "S1", $"{what} is not on the screen", $"{go.name} has no screen point at {Screen.width}x{Screen.height}");
            reached(false);
            yield break;
        }
        GameObject top = QaGame.TopAt(p.Value);
        bool ok = Related(top, go);
        Rep(r, $"   press {what} at ({p.Value.x:0},{p.Value.y:0}): under the pointer {(top != null ? QaGame.Where(top) : "nothing")}{(ok ? string.Empty : " (NOT the target)")}; its box {Px(QaGame.ScreenRect(go))}");
        yield return r.Input.Play(QaPaths.Click(p.Value));
        reached(ok);
    }

    // ---------------- personWatch ----------------

    private static IEnumerator Watch(QaRunner r, QaStep s)
    {
        if (_watched != null)
            _watched.Resolved -= OnResolved;
        _watched = QaGame.Find<GameManager>();
        if (_watched != null)
            _watched.Resolved += OnResolved;
        _verdict = null;
        Decision = null;
        Readouts.Clear();
        _mismatch = false;
        Rep(r, $"== personWatch: {(_watched != null ? "listening" : "NO GameManager")} at {Screen.width}x{Screen.height}");
        yield return null;
    }

    private static void OnResolved(CaseInstance inst, CaseVerdict verdict, bool accepted, float stabilityBefore)
    {
        _verdict = verdict;
        _accepted = accepted;
    }

    // ---------------- personStep ----------------

    private static IEnumerator StepOp(QaRunner r, QaStep s)
    {
        yield return QaGame.Frames(10);
        string name = Name(s);
        GuidePrompt prompt = QaGame.Find<GuidePrompt>();
        string header = prompt != null ? prompt.Header : "(no prompt)";
        string line = prompt != null ? prompt.Line : string.Empty;
        bool shown = prompt != null && prompt.IsShown;
        object plate = prompt != null ? QaGame.Field(prompt, "plate") : null;
        Rect? plateRect = plate is Component c ? QaGame.ScreenRect(c.gameObject) : null;
        Rep(r, $"== step {name} ({Screen.width}x{Screen.height}): plate {(shown ? "shown" : "NOT shown")}, header '{header}', line '{line}', arrow {(prompt != null && prompt.ArrowShown ? "shown at " + prompt.ArrowAt : "hidden")}, plate {Px(plateRect)}");
        foreach (DeskDocument p in Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude).Where(p => p.isActiveAndEnabled && p.Sheet != null).OrderBy(p => p.Index))
            Rep(r, $"   paper {p.Index} {p.name} zone {p.Zone}: click box {Px(QaGame.ScreenRect(p.gameObject))}, sheet {Px(QaGame.ScreenRect(p.Sheet.gameObject))}");
        bool expected = s.n <= 0 || (shown && header.Contains(s.n + "/"));
        if (!expected)
            Find(r, "S2", $"tutorial step {s.n} is not the one shown", $"header '{header}', line '{line}', still {name}");
        if (!string.IsNullOrEmpty(s.text) && !line.Contains(s.text))
            Find(r, "S3", $"the plate's line lacks '{s.text}'", $"line '{line}', still {name}");
        yield return Still(r, name);
    }

    /// <summary>The compare readout at the bottom centre as it shows (empty when its bar is hidden).</summary>
    private static string StripText(CompareController compare)
    {
        GameObject bar = compare != null ? QaGame.Field(compare, "officeBar") as GameObject : null;
        TMP_Text text = compare != null ? QaGame.Field(compare, "officeText") as TMP_Text : null;
        return bar != null && bar.activeInHierarchy && text != null ? text.text : string.Empty;
    }

    // ---------------- personPick ----------------

    private static ClueCategory CategoryOf(string text)
    {
        string t = (text ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        if (t == "validuntil" || t == "expiry")
            return ClueCategory.Expiry;
        return Enum.TryParse(t, true, out ClueCategory c) ? c : ClueCategory.Destination;
    }

    private static DeskDocument Passport()
    {
        DeskStampTray tray = QaGame.Find<DeskStampTray>();
        return tray == null ? null : Object.FindObjectsByType<DeskDocument>(FindObjectsInactive.Exclude).FirstOrDefault(p => p.isActiveAndEnabled && p.Sheet != null && p.Index == tray.Passport);
    }

    /// <summary>A screen point where <paramref name="paper"/> is what the pointer meets (a 5 by 5 grid over its sheet, its middle first), above the bottom band.</summary>
    private static bool TopPoint(DeskDocument paper, out Vector2 at)
    {
        Rect? rect = QaGame.ScreenRect(paper.Sheet.gameObject);
        at = default;
        if (rect == null)
            return false;
        Rect rr = rect.Value;
        for (int k = 0; k < 25; k++)
        {
            int i = (k + 12) % 25;
            Vector2 q = new Vector2(Mathf.Lerp(rr.xMin, rr.xMax, 0.1f + 0.2f * (i % 5)), Mathf.Lerp(rr.yMin, rr.yMax, 0.1f + 0.2f * (i / 5)));
            if (q.x < 0f || q.y < Screen.height * 0.12f || q.x > Screen.width || q.y > Screen.height)
                continue;
            GameObject hit = QaGame.TopAt(q);
            if (hit != null && hit.transform.IsChildOf(paper.transform))
            {
                at = q;
                return true;
            }
        }
        return false;
    }

    private static IEnumerator Away(QaRunner r)
    {
        yield return r.Input.MoveTo(new Vector2(Screen.width * 0.5f, Screen.height * 0.03f), 8);
        DeskDocument p = Passport();
        if (p != null)
            for (float end = Time.unscaledTime + 2f; Time.unscaledTime < end && (float)QaGame.Field(p, "_readNow") > 0f;)
                yield return null;
    }

    private static IEnumerator PickOp(QaRunner r, QaStep s)
    {
        string name = Name(s);
        string id = "person.pick." + name;
        DeskDocument paper = Passport();
        DeskInspect inspect = QaGame.Find<DeskInspect>();
        Camera cam = QaGame.PointerCamera();
        if (paper == null || paper.Zone != DeskZone.Desk || cam == null)
        {
            Find(r, "S1", "no passport on the desk to read", $"paper {(paper != null ? paper.Zone.ToString() : "none")}");
            r.Assert(id, Owner, false, "no passport on the desk", string.Empty);
            yield break;
        }
        yield return ToView(r, true);
        ClueCategory want = CategoryOf(s.text);
        int slot = -1;
        for (int i = 0; i < paper.SlotCount && slot < 0; i++)
            if (paper.FieldAt(i).Field.category == want)
                slot = i;
        if (slot < 0)
        {
            Find(r, "S1", $"the passport has no pickable box for {want}", $"{paper.SlotCount} boxes");
            r.Assert(id, Owner, false, $"no box for {want}", string.Empty);
            yield break;
        }
        DocumentField field = paper.FieldAt(slot).Field;
        Rep(r, $"== pick {name}: box {slot} '{field.label}' = '{field.value}' ({want}); inspect {(inspect != null && inspect.IsOn ? "on" : "OFF")}");
        yield return Away(r);
        if (!TopPoint(paper, out Vector2 on))
        {
            Find(r, "S1", "no point of the passport can be pressed", $"sheet {Px(QaGame.ScreenRect(paper.Sheet.gameObject))}");
            r.Assert(id, Owner, false, "no pressable point", string.Empty);
            yield break;
        }
        yield return r.Input.MoveTo(on, 8);
        float lift = 0f;
        for (float end = Time.unscaledTime + 3f; Time.unscaledTime < end;)
        {
            lift = (float)QaGame.Field(paper, "_readNow");
            if (lift >= 1f)
                break;
            yield return null;
        }
        yield return Secs(0.3f);
        Rep(r, $"   pointer at ({r.Input.Position.x:0},{r.Input.Position.y:0}) on the passport; lift clock {lift:0.00}; lifted sheet {Px(QaGame.ScreenRect(paper.Sheet.gameObject))}");
        if (lift < 1f)
            Find(r, "S1", "the passport does not lift under the pointer in inspect mode", $"clock {lift:0.00} after 3 s, inspect {(inspect != null && inspect.IsOn)}");
        yield return Still(r, name + "_lifted");

        Transform box = paper.Sheet.GetComponentsInChildren<Transform>(false).FirstOrDefault(t => t.name == $"Slot_{slot}");
        if (box == null)
        {
            Find(r, "S1", "the box has no quad", $"Slot_{slot}");
            r.Assert(id, Owner, false, "no box quad", string.Empty);
            yield break;
        }
        Vector3 sp = cam.WorldToScreenPoint(box.position);
        Vector2 at = new Vector2(sp.x, sp.y);
        Rect? boxRect = QaGame.ScreenRect(box.gameObject);
        GameObject top = QaGame.TopAt(at);
        Rep(r, $"   value box {Px(boxRect)}; under the pointer there {(top != null ? QaGame.Where(top) : "nothing")}");
        if (boxRect is Rect br && (br.width < 40f || br.height < 40f))
            Find(r, "S3", $"the {want} box on the lifted passport is {br.width:0}x{br.height:0} px", $"at {Screen.width}x{Screen.height}; still {name}_lifted");
        yield return r.Input.Play(QaPaths.Click(at));
        yield return Secs(0.5f);
        CompareController compare = QaGame.Find<CompareController>();
        bool held = (inspect != null && inspect.ValueHeld) || (compare != null && (compare.Holding || compare.Paired)) || StripText(compare).Length > 0;
        Rep(r, $"   after the press: value held {held}; compare {(compare != null ? (compare.Holding ? "holding " + compare.SideA.Shown : compare.Paired ? "paired" : "empty") : "none")}");
        if (!held)
            Find(r, "S1", $"a press on the {want} box holds nothing", $"pointer ({at.x:0},{at.y:0}), under it {(top != null ? QaGame.Where(top) : "nothing")}; still {name}_picked");
        yield return Still(r, name + "_picked");
        r.Assert(id, Owner, held, held ? $"{want} held" : $"{want} not held", $"box {slot} {Px(boxRect)}");
    }

    // ---------------- personRulebook ----------------

    private static IEnumerator RulebookOp(QaRunner r, QaStep s)
    {
        string name = Name(s);
        string id = "person.rule." + name;
        DeskRulebook rb = QaGame.Find<DeskRulebook>();
        DeskView view = QaGame.Find<DeskView>();
        if (rb == null || !rb.gameObject.activeInHierarchy)
        {
            Find(r, "S1", "the rulebook is not on the desk on day 1", $"{(rb == null ? "no DeskRulebook" : "inactive")}; the tutorial step 4 names it");
            r.Assert(id, Owner, false, "no rulebook", string.Empty);
            yield break;
        }
        Rep(r, $"== rulebook {name}: pocket {rb.HasPocket}, in the pocket {rb.Pocketed}, shut {rb.Shut}, reading view {(view != null && view.IsOn)}");
        yield return Away(r);
        if (rb.Pocketed)
        {
            GameObject book = QaGame.GameObjectAt("Desk_RulebookFolded");
            bool onScreen = QaGame.ScreenPoint(book) != null;
            Rep(r, $"   the painted book (Desk_RulebookFolded): {(onScreen ? Px(QaGame.ScreenRect(book)) : "not on the screen in this view")}");
            if (!onScreen)
            {
                Find(r, "S2", "the red book that holds the rulebook is not on the screen in the reading view", "a player must turn to the office view first (TAB); no step says so");
                yield return ToView(r, false);
            }
            yield return Still(r, name + "_book");
            bool ok = false;
            yield return ClickOn(r, book, "the red book", v => ok = v);
            for (float end = Time.unscaledTime + 5f; Time.unscaledTime < end && (rb.Pocketed || rb.PocketMoving);)
                yield return null;
            yield return Secs(1f);
            Rep(r, $"   after the click: in the pocket {rb.Pocketed}, shut {rb.Shut}");
            if (rb.Pocketed)
                Find(r, "S1", "a click on the red book does not bring the rulebook out", $"reached {ok}");
        }
        yield return ToView(r, true);
        yield return Away(r);
        if (rb.Shut)
        {
            Rep(r, "   the folder is shut: a click on it opens it");
            GameObject card = (QaGame.Field(rb, "card") as Component)?.gameObject;
            bool ok = false;
            if (card != null)
                yield return ClickOn(r, card, "the shut folder", v => ok = v);
            yield return Secs(1.5f);
        }
        yield return Still(r, name + "_folder");

        // the day's rule row: the one that names the destination, else the first
        Clickable[] rows = (Clickable[])QaGame.Field(rb, "rows");
        var shownRows = rows.Where(c => c != null && c.gameObject.activeInHierarchy).ToList();
        foreach (Clickable c in shownRows)
            Rep(r, $"   row {Array.IndexOf(rows, c)}: '{c.GetComponentInChildren<TMP_Text>()?.text}' {Px(QaGame.ScreenRect(c.gameObject))}");
        if (shownRows.Count == 0)
        {
            Find(r, "S1", "the rulebook shows no rule row", $"page rules {(QaGame.Field(rb, "rulesPage") is GameObject g && g.activeInHierarchy)}, shut {rb.Shut}");
            r.Assert(id, Owner, false, "no rule row", string.Empty);
            yield break;
        }
        Clickable row = shownRows.FirstOrDefault(c => { string t = c.GetComponentInChildren<TMP_Text>()?.text ?? string.Empty; return t.IndexOf("destination", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("open", StringComparison.OrdinalIgnoreCase) >= 0; }) ?? shownRows[0];
        Vector2? rp = QaGame.ScreenPoint(row.gameObject);
        GameObject under = rp.HasValue ? QaGame.TopAt(rp.Value) : null;
        if (rp == null || !Related(under, row.gameObject))
        {
            Find(r, "S2", "the passport (or something else) covers the rulebook's rule row", $"row point {(rp.HasValue ? rp.Value.ToString() : "off screen")}, under it {(under != null ? QaGame.Where(under) : "nothing")}; still {name}_folder; the folder must be dragged clear first");
            yield return Clear(r, rb);
            rp = QaGame.ScreenPoint(row.gameObject);
            under = rp.HasValue ? QaGame.TopAt(rp.Value) : null;
            yield return Still(r, name + "_folder_clear");
        }
        bool reached = false;
        Rep(r, $"   row chosen: {Array.IndexOf(rows, row)}");
        yield return ClickOn(r, row.gameObject, "the rule row", v => reached = v);
        yield return Secs(0.5f);
        yield return Still(r, name + "_row");
        r.Assert(id, Owner, reached, reached ? "the rule row pressed" : "the rule row was covered", $"row {Array.IndexOf(rows, row)}");
    }

    /// <summary>Drags the folder left by a real press on a visible part of it, as a player moves a book off a paper.</summary>
    private static IEnumerator Clear(QaRunner r, DeskRulebook rb)
    {
        Rect? fr = QaGame.ScreenRect(rb.gameObject);
        if (fr == null)
            yield break;
        Rect f = fr.Value;
        for (int k = 0; k < 144; k++)
        {
            Vector2 q = new Vector2(Mathf.Lerp(f.xMin, f.xMax, (k % 12 + 0.5f) / 12f), Mathf.Lerp(f.yMin, f.yMax, (k / 12 + 0.5f) / 12f));
            GameObject hit = QaGame.TopAt(q);
            if (hit == null || !hit.transform.IsChildOf(rb.transform))
                continue;
            Vector2 to = q + new Vector2(-Screen.width * 0.17f, 0f);
            Rep(r, $"   drag the folder from ({q.x:0},{q.y:0}) to ({to.x:0},{to.y:0})");
            yield return r.Input.Play(QaPaths.Drag(QaPathKind.Plain, q, to, new Vector2(Screen.width, Screen.height)));
            yield return Secs(1.5f);
            yield break;
        }
        Find(r, "S1", "no part of the rulebook folder can be pressed to move it", $"folder {Px(fr)}");
    }

    // ---------------- personCalendar ----------------

    private static IEnumerator CalendarOp(QaRunner r, QaStep s)
    {
        string name = Name(s);
        string id = "person.calendar." + name;
        DeskInspect inspect = QaGame.Find<DeskInspect>();
        DeskView view = QaGame.Find<DeskView>();
        Transform cal = inspect != null ? QaGame.Field(inspect, "calendar") as Transform : null;
        Rep(r, $"== calendar {name}: {(cal == null ? "no calendar" : cal.gameObject.activeInHierarchy ? "active" : "INACTIVE")}, reading view {(view != null && view.IsOn)}");
        if (cal == null || !cal.gameObject.activeInHierarchy)
        {
            Find(r, "S1", "the calendar's click box is not on the desk", "the tutorial step 5 names it");
            r.Assert(id, Owner, false, "no calendar", string.Empty);
            yield break;
        }
        yield return Away(r);
        bool turned = false;
        if (QaGame.ScreenPoint(cal.gameObject) == null)
        {
            Find(r, "S2", "the calendar is not on the screen in the reading view", "the player must turn to the office view (TAB) with a value held; no step says so");
            yield return ToView(r, false);
            turned = true;
        }
        yield return Still(r, name + "_before");
        bool reached = false;
        Rect? box = QaGame.ScreenRect(cal.gameObject);
        yield return ClickOn(r, cal.gameObject, "the calendar", v => reached = v);
        yield return Secs(0.5f);
        if (box is Rect b && (b.width < 40f || b.height < 40f))
            Find(r, "S3", $"the calendar's click box is {b.width:0}x{b.height:0} px", $"at {Screen.width}x{Screen.height}; still {name}_before");
        yield return Still(r, name + "_clicked");
        if (turned)
            yield return ToView(r, true);
        r.Assert(id, Owner, reached, reached ? "the calendar pressed" : "something else was under the pointer", $"box {Px(box)}");
    }

    // ---------------- personFace, personLook, personSettle ----------------

    private static IEnumerator FaceOp(QaRunner r, QaStep s)
    {
        string name = Name(s);
        string id = "person.face." + name;
        DeskInspect inspect = QaGame.Find<DeskInspect>();
        GuideDirector guide = QaGame.Find<GuideDirector>();
        Collider zone = guide != null ? QaGame.Field(guide, "traveller") as Collider : null;
        Rep(r, $"== face {name}: traveller zone {(zone == null ? "none" : zone.gameObject.activeInHierarchy ? "active" : "INACTIVE")}, inspect {(inspect != null && inspect.IsOn ? "on" : "OFF")}");
        if (zone == null || !zone.gameObject.activeInHierarchy)
        {
            Find(r, "S1", "the traveller cannot be pressed to hold their face", "no hit zone");
            r.Assert(id, Owner, false, "no traveller zone", string.Empty);
            yield break;
        }
        yield return Away(r);
        yield return ToView(r, false);
        Rect? box = QaGame.ScreenRect(zone.gameObject);
        bool reached = false;
        yield return ClickOn(r, zone.gameObject, "the traveller", v => reached = v);
        yield return Secs(0.5f);
        CompareController compare = QaGame.Find<CompareController>();
        bool held = compare != null && (compare.Holding || compare.Paired);
        Rep(r, $"   after the press: face held {held} ({(compare != null && compare.Holding ? compare.SideA.Shown : "-")}); strip '{StripText(compare)}'");
        if (!held)
            Find(r, "S1", "a press on the traveller in inspect mode holds no face", $"zone {Px(box)}, under the pointer reached {reached}; still {name}");
        yield return Still(r, name);
        r.Assert(id, Owner, held, held ? "face held" : "face not held", $"zone {Px(box)}");
    }

    private static IEnumerator LookOp(QaRunner r, QaStep s)
    {
        string name = Name(s);
        DeskDocument paper = Passport();
        if (paper == null || paper.Zone != DeskZone.Desk)
        {
            Rep(r, $"== look {name}: no passport on the desk");
            yield break;
        }
        yield return ToView(r, true);
        yield return Away(r);
        if (!TopPoint(paper, out Vector2 on))
        {
            Rep(r, $"== look {name}: no pressable point");
            yield break;
        }
        yield return r.Input.MoveTo(on, 8);
        for (float end = Time.unscaledTime + 3f; Time.unscaledTime < end && (float)QaGame.Field(paper, "_readNow") < 1f;)
            yield return null;
        yield return Secs(0.4f);
        Rep(r, $"== look {name}: pointer at ({r.Input.Position.x:0},{r.Input.Position.y:0}), lift clock {(float)QaGame.Field(paper, "_readNow"):0.00}, ink {(QaGame.Field(paper, "_stamps"))} print(s)");
        yield return Still(r, name);
        yield return Away(r);
    }

    private static IEnumerator SettleOp(QaRunner r, QaStep s)
    {
        yield return QaGame.UntilCameraStill(8f);
        yield return Secs(0.5f);
    }

    // ---------------- personReadout, personDecide ----------------

    private static IEnumerator ReadoutOp(QaRunner r, QaStep s)
    {
        string name = Name(s);
        yield return Secs(0.4f);
        CompareController compare = QaGame.Find<CompareController>();
        DeskInspect inspect = QaGame.Find<DeskInspect>();
        MatchBoard board = inspect != null ? QaGame.Field(inspect, "board") as MatchBoard : null;
        GameObject bar = compare != null ? QaGame.Field(compare, "officeBar") as GameObject : null;
        TMP_Text text = compare != null ? QaGame.Field(compare, "officeText") as TMP_Text : null;
        string strip = bar != null && bar.activeInHierarchy && text != null ? text.text : string.Empty;
        FindingLook look = board != null ? board.Line.look : FindingLook.Info;
        string a = board != null ? board.Line.a : null, b = board != null ? board.Line.b : null;
        string last = board != null && board.Log.Count > 0 ? board.Log.Items[board.Log.Count - 1].Kind.ToString() : "none";
        Rep(r, $"== readout {name} ({s.text}): strip '{strip}' (bar {(bar != null && bar.activeInHierarchy ? "shown" : "hidden")}), line {a} to {b} look {look}, last finding {last}, findings {(board != null ? board.Log.Count : 0)}");
        // every other text on the screen that says MATCH
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude))
            if (t.gameObject.activeInHierarchy && t != text && t.text != null && t.text.IndexOf("MATCH", StringComparison.OrdinalIgnoreCase) >= 0)
                Rep(r, $"   other text with MATCH: {QaGame.Where(t.gameObject)} '{t.text}'");
        bool mismatch;
        if (strip.Length > 0)
            mismatch = strip.IndexOf("MISMATCH", StringComparison.OrdinalIgnoreCase) >= 0;
        else
        {
            mismatch = look == FindingLook.Differ;
            Find(r, "S2", $"the {s.text} compare gives no readout at the bottom centre", $"the strip is empty; only the line's colour ({look}) says the result; still {name}");
        }
        Readouts.Add($"{s.text}: {(strip.Length > 0 ? strip : "(no readout, line " + look + ")")}");
        _mismatch |= mismatch;
        Rect? rect = text != null && strip.Length > 0 ? QaGame.ScreenRect(text.gameObject) : null;
        if (rect != null)
            Rep(r, $"   readout text box {Px(rect)}; cap height about {(text.fontSize > 0 ? text.fontSize * 0.7f : 0):0} px of font size {text.fontSize:0}");
        yield return Still(r, name);
    }

    private static IEnumerator DecideOp(QaRunner r, QaStep s)
    {
        Decision = _mismatch ? "Denied" : "Approved";
        Rep(r, $"== decide: {Decision} from the readouts: {string.Join("; ", Readouts)}");
        Readouts.Clear();
        _mismatch = false;
        yield return null;
    }

    // ---------------- personVerdict ----------------

    private static IEnumerator VerdictOp(QaRunner r, QaStep s)
    {
        string name = Name(s);
        yield return QaGame.Until(() => _verdict != null, 5f, "the case's verdict");
        bool ok = _verdict != null && _verdict.correct && !_verdict.citationIssued;
        string measured = _verdict == null ? "no verdict" : $"case {_verdict.caseIndex1Based} {_verdict.visitorName}: {(_accepted ? "Approved" : "Denied")}, correct {_verdict.correct}, citation {_verdict.citationIssued}, pay {_verdict.payAwarded}, penalty {_verdict.moneyPenalty}, decision from the readouts {Decision}";
        Rep(r, $"== verdict {name}: {measured}");
        if (_verdict != null && _verdict.citationIssued)
            Find(r, "S1", "a citation slip came for a call that followed the tutorial and the readouts", measured);
        r.Assert("person.verdict." + name, Owner, ok, measured, string.Empty);
        _verdict = null;
        Decision = null;
        yield return Still(r, name + "_after");
    }
}
#endif
