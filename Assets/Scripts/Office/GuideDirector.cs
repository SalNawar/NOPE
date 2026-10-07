using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// The desk's guide at run time (Saleh 2026-10-06: "we need to create an FTUE
/// and a help guide that gets expanded every day like Papers, Please that
/// explains the new rule, not just have it in a document, at least for the
/// first week until we teach the player to keep checking new rules"). The
/// rules are the Domain's Guide (the content library's, over the ramp); this
/// listens to the desk and shows them:
/// <list type="bullet">
/// <item>day 1's FTUE (or a replay from Settings or the F1 card): one prompt a
/// step (GuidePrompt: a line and an arrow at the real thing), each completed
/// when the player does its action (a traveller called, a paper on the desk,
/// inspect mode on, a comparison logged about its detail, the stamp bar out,
/// the passport stamped, the papers handed back); Skip ends it; an open FTUE
/// ends with the shift;</item>
/// <item>on a guided day (2 to GuideContent.guidedThroughDay) with a new page:
/// at the shift's start the rulebook opens itself on that page, the reading
/// view tilts in to show it, and the prompt names it and points where to
/// check it (Got it returns); the first traveller who
/// carries it gets the page's one-step practice;</item>
/// <item>every day: the rulebook's GUIDE holds BASICS and the pages
/// introduced so far (DeskRulebook.SetGuide); its tab wears a NEW badge while
/// a page added today is unread (from day 8 that badge is all there is).</item>
/// </list>
/// Progress is the run's (WorldState.guide), saved with the day. It never
/// blocks: the prompt takes no clicks but its buttons.
/// </summary>
public sealed class GuideDirector : MonoBehaviour
{
    [Header("Parts")]
    /// <summary>The prompt on the office overlay.</summary>
    [SerializeField] private GuidePrompt prompt;

    /// <summary>The rulebook (its GUIDE tab).</summary>
    [SerializeField] private DeskRulebook rulebook;

    /// <summary>The desk (a paper on the desk, a scan, where a paper's field lies).</summary>
    [SerializeField] private DeskController desk;

    /// <summary>Inspect mode.</summary>
    [SerializeField] private DeskInspect inspect;

    /// <summary>The stamp bar (out, the passport stamped).</summary>
    [SerializeField] private DeskStampTray stamps;

    /// <summary>The workbench (each comparison logged).</summary>
    [SerializeField] private MatchBoard board;

    /// <summary>The office view (a replay from the PC returns to the desk; the prompt shows on the PC only when it points at it).</summary>
    [SerializeField] private OfficeViewController view;

    /// <summary>The reading view: the new rule's moment tilts into it, so the rulebook's open page is read; Got it returns.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The city view (optional): while the player looks at the city the arrow hides (what it points at is behind the panorama); the plate stays.</summary>
    [SerializeField] private CityView city;

    [Header("Where the arrows point (GuideTargets)")]
    /// <summary>The AVAILABLE sign's click box ("sign").</summary>
    [SerializeField] private Transform sign;

    /// <summary>The calendar's click box ("calendar").</summary>
    [SerializeField] private Transform calendar;

    /// <summary>The Departure Board ("board").</summary>
    [SerializeField] private Transform departureBoard;

    /// <summary>The scanner ("scanner").</summary>
    [SerializeField] private Transform scanner;

    /// <summary>The counter ("counter").</summary>
    [SerializeField] private Transform counter;

    /// <summary>The traveller's hit zone ("traveller").</summary>
    [SerializeField] private Collider traveller;

    /// <summary>The red inspect button ("inspect").</summary>
    [SerializeField] private RectTransform inspectButton;

    /// <summary>The grey stamp tab ("stamps").</summary>
    [SerializeField] private RectTransform stampTab;

    /// <summary>The PC's grey tab ("pc").</summary>
    [SerializeField] private RectTransform pcTab;

    private enum Showing { None, Step, Done, Moment, Practice }

    private Camera _camera;
    private Guide _guide;
    private GuideState _state;
    private int _day;
    private bool _shift;
    private bool _replayed;
    private Showing _showing;
    private string _target;
    private CaseInstance _case;
    private GuidePage _practice;
    private bool _inspecting, _barOut, _stamped;

    /// <summary>What the prompt shows: "step", "done", "moment", "practice" or "" (the probe and the audit read it).</summary>
    public string ShowingNow => _showing == Showing.None ? string.Empty : _showing.ToString().ToLowerInvariant();

    /// <summary>The FTUE step shown (its id), or empty.</summary>
    public string StepShown => _showing == Showing.Step ? _guide?.CurrentStep(_state)?.id ?? string.Empty : string.Empty;

    /// <summary>The prompt.</summary>
    public GuidePrompt Prompt => prompt;

    private void OnEnable()
    {
        if (desk != null)
        {
            desk.PaperExamined += HandleExamined;
            desk.ScanFinished += HandleScan;
        }
        if (inspect != null)
            inspect.Changed += HandleInspect;
        if (stamps != null)
            stamps.Changed += HandleStamps;
        if (board != null)
            board.Logged += HandleLogged;
        if (rulebook != null)
            rulebook.GuideRead += HandleRead;
        if (prompt != null)
        {
            prompt.Skipped += Skip;
            prompt.Acknowledged += Dismiss;
        }
    }

    private void OnDisable()
    {
        if (desk != null)
        {
            desk.PaperExamined -= HandleExamined;
            desk.ScanFinished -= HandleScan;
        }
        if (inspect != null)
            inspect.Changed -= HandleInspect;
        if (stamps != null)
            stamps.Changed -= HandleStamps;
        if (board != null)
            board.Logged -= HandleLogged;
        if (rulebook != null)
            rulebook.GuideRead -= HandleRead;
        if (prompt != null)
        {
            prompt.Skipped -= Skip;
            prompt.Acknowledged -= Dismiss;
        }
    }

    /// <summary>The office camera the arrows are placed through (the office binder's).</summary>
    public void SetCamera(Camera office) => _camera = office;

    /// <summary>
    /// The shift starts (after the briefing; GameManager): the rulebook's
    /// GUIDE gets the day's sheets and badge; day 1 (or a replay) starts the
    /// FTUE at its first open step; a guided day opens the rulebook on its new
    /// page with the moment's prompt. An FTUE left open by an older save on a
    /// later day is closed, never shown.
    /// </summary>
    public void BeginShift(int day, GuideState state, ContentLibrarySO library)
    {
        _day = day;
        _state = state ?? new GuideState();
        _guide = library != null ? library.Guide : null;
        _shift = true;
        _case = null;
        _practice = null;
        if (_guide == null)
            return;
        if (_replayed)
            Guide.Replay(_state);
        else if (day > 1)
            Guide.CloseShift(_state);
        RefreshGuide();
        _practice = _guide.Practice(day, _state);

        if (Guide.FtueOpen(_state))
        {
            ShowStep();
            return;
        }
        GuidePage page = _guide.MomentPage(day);
        if (page != null)
        {
            if (rulebook != null)
                rulebook.OpenGuide(page.id);
            if (deskView != null)
                deskView.TiltIn();
            Show(Showing.Moment, UiText.Get("guide.newToday"),
                 UiText.Format("guide.moment", page.title, GuideText.Keys(page.check)),
                 false, page.point);
        }
        else
        {
            Hide();
        }
    }

    /// <summary>A traveller is at the desk (GameManager): the FTUE's call is done; a moment still up gives way; the day's practice shows when they carry its feature.</summary>
    public void CaseShown(CaseInstance inst)
    {
        _case = inst;
        _stamped = false;
        if (_guide == null)
            return;
        if (_showing == Showing.Moment || _showing == Showing.Done)
            Hide();
        Record(new GuideEvent(GuideAction.Call));
        if (!Guide.FtueOpen(_state))
            ShowPractice();
    }

    /// <summary>The traveller is decided (the papers handed back; GameManager): the FTUE's hand-back is done; a practice not done waits for the next traveller who carries it.</summary>
    public void CaseDecided()
    {
        Record(new GuideEvent(GuideAction.HandBack));
        if (_showing == Showing.Practice)
            Hide();
        _case = null;
    }

    /// <summary>The shift ends (GameManager, before the save): an FTUE still open is over; the prompt hides.</summary>
    public void EndShift()
    {
        if (_state != null)
            Guide.CloseShift(_state);
        _shift = false;
        _replayed = false;
        _case = null;
        Hide();
    }

    /// <summary>Settings' and the F1 card's "Replay the desk tutorial": the FTUE from its first step, on any day (a traveller already at the desk counts as called); the PC closes so the desk is seen.</summary>
    public void Replay()
    {
        _replayed = true;
        if (_state == null)
            return;
        Guide.Replay(_state);
        if (view != null)
            view.FocusOffice();
        if (!_shift || _guide == null)
            return;
        if (_case != null)
            _guide.Record(_state, new GuideEvent(GuideAction.Call));
        ShowStep();
    }

    /// <summary>The prompt's Skip and the cheat menu's "Skip tutorial": an open FTUE is over (it can be replayed); the prompt goes.</summary>
    public void Skip()
    {
        if (Guide.FtueOpen(_state))
            Guide.Skip(_state);
        Hide();
        if (_case != null)
            ShowPractice();
    }

    /// <summary>The prompt's Got it: the moment (back from the reading view to the desk) or the tutorial's last line goes; a practice dismissed counts as done (it never comes back).</summary>
    private void Dismiss()
    {
        if (_showing == Showing.Moment && deskView != null)
            deskView.Return();
        if (_showing == Showing.Practice && _practice != null && _state != null && !_state.practiced.Contains(_practice.id))
            _state.practiced.Add(_practice.id);
        Hide();
    }

    // ---- What the player does ----

    private void HandleExamined(int paper)
    {
        string form = _case != null && paper >= 0 && paper < _case.documents.Count && _case.documents[paper].template != null
            ? _case.documents[paper].template.formNumber
            : string.Empty;
        Record(new GuideEvent(GuideAction.OnDesk, form: form));
    }

    private void HandleScan(int paper, ScanPass pass) => Record(new GuideEvent(GuideAction.Scan));

    private void HandleInspect()
    {
        bool on = inspect != null && inspect.IsOn;
        if (on && !_inspecting)
            Record(new GuideEvent(GuideAction.Inspect));
        _inspecting = on;
    }

    private void HandleStamps()
    {
        bool barOut = stamps != null && stamps.BarOut, stamped = stamps != null && stamps.HasVerdict;
        if (barOut && !_barOut)
            Record(new GuideEvent(GuideAction.StampsOut));
        if (stamped && !_stamped)
            Record(new GuideEvent(GuideAction.Stamp));
        _barOut = barOut;
        _stamped = stamped;
    }

    private void HandleLogged(Finding finding) => Record(new GuideEvent(GuideAction.Compare, finding?.Category));

    /// <summary>A guide sheet was shown: it is read, and the badge follows.</summary>
    private void HandleRead(string pageId)
    {
        if (_state == null || _guide == null || string.IsNullOrEmpty(pageId))
            return;
        Guide.Read(_state, pageId);
        if (rulebook != null)
            rulebook.SetGuideBadge(_guide.Badge(_day, _state));
    }

    /// <summary>One thing done: the FTUE's steps it completes (then the next step, or the last line), and the practice it completes.</summary>
    private void Record(GuideEvent e)
    {
        if (_guide == null || !_shift)
            return;
        if (Guide.FtueOpen(_state))
        {
            if (_guide.Record(_state, e))
            {
                if (_state.ftueDone)
                    Show(Showing.Done, UiText.Get("guide.done"), GuideText.Keys(_guide.Content.ftueDone), false, null);
                else
                    ShowStep();
            }
            return;
        }
        if (_showing == Showing.Practice && Guide.Practise(_state, _practice, e))
            Hide();
    }

    // ---- What shows ----

    /// <summary>The rulebook's GUIDE for the day: BASICS, then every page introduced so far (today's marked NEW), and the badge.</summary>
    private void RefreshGuide()
    {
        if (rulebook == null)
            return;
        GuideContent content = _guide.Content;
        var sheets = new List<GuideSheet>
        {
            new GuideSheet(string.Empty, content.basicsTitle, string.Join("\n", content.basics.Select(l => "• " + GuideText.Keys(l))), false)
        };
        foreach (GuidePage page in _guide.PagesOn(_day))
            sheets.Add(new GuideSheet(page.id, page.title,
                                      UiText.Format("desk.guide.body", GuideText.Keys(page.check), GuideText.Keys(page.against), GuideText.Keys(page.fault)),
                                      _guide.IsNew(_day, page)));
        rulebook.SetGuide(sheets);
        rulebook.SetGuideBadge(_guide.Badge(_day, _state));
    }

    private void ShowStep()
    {
        GuideStep step = _guide.CurrentStep(_state);
        if (step == null)
        {
            Hide();
            return;
        }
        Show(Showing.Step, UiText.Format("guide.step", _guide.StepNumber(_state), _guide.Content.ftue.Count), GuideText.Keys(step.text), true, step.target);
    }

    /// <summary>The day's practice, when the traveller at the desk carries its feature.</summary>
    private void ShowPractice()
    {
        if (_case == null || _practice == null || _state.practiced.Contains(_practice.id) || _showing != Showing.None)
            return;
        IEnumerable<string> forms = _case.documents.Where(d => d != null && d.template != null).Select(d => d.template.formNumber);
        if (Guide.Carries(_practice, forms))
            Show(Showing.Practice, UiText.Get("guide.practice"), GuideText.Keys(_practice.practice.text), false, _practice.practice.target);
    }

    private void Show(Showing what, string header, string line, bool skip, string target)
    {
        _showing = what;
        _target = target;
        if (prompt != null)
            prompt.Show(header, line, skip);
    }

    private void Hide()
    {
        _showing = Showing.None;
        _target = null;
        if (prompt != null)
            prompt.Hide();
    }

    // ---- The arrow ----

    /// <summary>Places the arrow at the target this frame; on the PC only a prompt pointing at the PC shows; while the player looks at the city the arrow hides.</summary>
    private void LateUpdate()
    {
        if (prompt == null || _showing == Showing.None)
            return;
        bool pc = view != null && view.Current == OfficeView.MonitorFocus;
        if (pc)
        {
            if (_target == "pc")
                prompt.HideArrow();
            else
                prompt.Hide();
            return;
        }
        if (!prompt.IsShown)
            prompt.Show(prompt.Header, prompt.Line, _showing == Showing.Step);
        if (string.IsNullOrEmpty(_target) || (city != null && city.IsOn))
        {
            prompt.HideArrow();
            return;
        }
        if (TryUi(_target, out RectTransform ui))
            prompt.PointAt(ui);
        else if (TryWorld(_target, out Vector3 world) && _camera != null)
            prompt.PointAt(_camera, world);
        else
            prompt.HideArrow();
    }

    private bool TryUi(string target, out RectTransform ui)
    {
        ui = target switch
        {
            "inspect" => inspectButton,
            "stamps" => stampTab,
            "pc" => pcTab,
            _ => null
        };
        return ui != null;
    }

    /// <summary>A world target's point: a named prop, or a paper of the traveller's (its printed fields' middle) or one field of it; a paper not on the desk yet points at the rulebook (its PAPERS tab).</summary>
    private bool TryWorld(string target, out Vector3 world)
    {
        world = default;
        Transform t = target switch
        {
            "sign" => sign,
            "calendar" => calendar,
            "board" => departureBoard,
            "scanner" => scanner,
            "counter" => counter,
            _ => null
        };
        if (t != null)
        {
            world = t.position;
            return true;
        }
        if (target == "rulebook" || GuideTargets.TryForm(target, out _))
            if (TryPaper(target, out world) || TryRulebook(out world))
                return true;
        if (target == "traveller" && traveller != null)
        {
            world = traveller.bounds.center;
            return true;
        }
        return false;
    }

    /// <summary>The rulebook's tabs (its top edge: the arrow sits over the tabs, never over the page's text).</summary>
    private bool TryRulebook(out Vector3 world)
    {
        world = rulebook != null ? rulebook.transform.TransformPoint(RulebookTabs) : default;
        return rulebook != null;
    }

    /// <summary>Where the rulebook's tabs are in its own space (metres: its top edge; Build Office UI's booklet).</summary>
    private static readonly Vector3 RulebookTabs = new Vector3(0f, 0f, 0.165f);

    /// <summary>Where the traveller's paper a "paper:" or "field:" <paramref name="target"/> names lies on the desk: one field's box ("field:") or the middle of all its fields' boxes; false when it is not on the desk.</summary>
    private bool TryPaper(string target, out Vector3 world)
    {
        world = default;
        if (_case == null || desk == null || !GuideTargets.TryForm(target, out string form))
            return false;
        bool field = GuideTargets.TryField(target, out _, out ClueCategory category);
        for (int d = 0; d < _case.documents.Count; d++)
        {
            DocumentInstance doc = _case.documents[d];
            if (doc == null || doc.template == null || doc.template.formNumber != form)
                continue;
            bool any = false;
            Bounds all = default;
            for (int f = 0; f < doc.fields.Count; f++)
            {
                if (field && doc.fields[f].category != category)
                    continue;
                if (!desk.TryFieldBounds(d, f, out Bounds box))
                    continue;
                if (!any)
                    all = box;
                else
                    all.Encapsulate(box);
                any = true;
            }
            if (any)
            {
                world = all.center;
                return true;
            }
        }
        return false;
    }
}
