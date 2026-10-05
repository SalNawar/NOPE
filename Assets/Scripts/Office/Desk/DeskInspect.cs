using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inspection at the desk (the desk-first redesign, Saleh 2026-10-05, item
/// 11: "the player should be able to check documents at the desk, highlight
/// clear mistakes and approve; scanning allows using additional features").
/// The PC's click-and-match works on the desk with the one workbench
/// (MatchBoard: the same findings, the same log, the same evidence for the
/// decision): in the desk view a box of a paper lying on the desk is clicked
/// where it lies (DeskController routes it into the compare), and it is held
/// against another paper's box, the calendar on the desk (today's date, once
/// the calendar is introduced), the traveller's face (a click on the
/// traveller while a value is held; otherwise it opens the wheel as before)
/// or a row of the rulebook on the desk (today's directives, once the
/// rulebook is introduced). The workbench's line is drawn over the office too,
/// between the two values where they lie (MatchLines over two proxy ends
/// placed each frame on the values' places on the screen), dashed from a held
/// value to the pointer; it shows while the PC frame is closed and both
/// values are in the office (an end off the screen, the calendar or the face
/// seen from the desk view, waits at the screen's edge toward it; a value only
/// on the PC draws nothing here). A logged
/// difference marks its papers' boxes (FindingMarks) for the rest of the
/// case. Scanning stays optional: a scanned paper's copy, search and links
/// reach the PC (CaseDocumentsPresenter). InvestigationUIController hands it
/// the day and the case; Build Office UI wires it.
/// </summary>
public sealed class DeskInspect : MonoBehaviour
{
    /// <summary>The one workbench (the PC's: its findings are the case's evidence).</summary>
    [SerializeField] private MatchBoard board;

    /// <summary>The one compare (the traveller's face goes into it).</summary>
    [SerializeField] private CompareController compare;

    /// <summary>The papers on the desk (their boxes' places, the marks).</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The traveller wheel (a click on the traveller opens it when nothing is held).</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>The traveller figure (the face's place).</summary>
    [SerializeField] private TravellerView traveller;

    /// <summary>The calendar prop's click box (today's place on the desk).</summary>
    [SerializeField] private Transform calendar;

    /// <summary>The rulebook on the desk.</summary>
    [SerializeField] private DeskRulebook rulebook;

    /// <summary>The office view (the line hides while the PC frame is open).</summary>
    [SerializeField] private OfficeViewController view;

    /// <summary>The city view (optional): the line hides while the view looks at the city.</summary>
    [SerializeField] private CityView city;

    /// <summary>The line over the office (the workbench's MatchLines, on the office overlay).</summary>
    [SerializeField] private MatchLines lines;

    /// <summary>The line's two ends: proxies placed over the two values' places on the screen each frame.</summary>
    [SerializeField] private RectTransform endA;

    /// <summary>The second end.</summary>
    [SerializeField] private RectTransform endB;

    /// <summary>A clear mistake's mark on a paper's box (a translucent red; it stays for the case).</summary>
    [SerializeField] private Color mistakeMark = new Color(0.86f, 0.16f, 0.12f, 0.32f);

    /// <summary>The smallest end on the screen, in canvas px (a far value still gets a visible box).</summary>
    [SerializeField] private Vector2 minimumEnd = new Vector2(24f, 18f);

    /// <summary>How far inside the screen's edge (px) the end of a value off the screen waits.</summary>
    private const float EdgeMargin = 24f;

    private Camera _camera;
    private CaseInstance _case;
    private Introductions _known = Introductions.None;
    private int _day;
    private string _today;
    private RectTransform _canvas;
    private readonly Vector3[] _corners = new Vector3[8];
    private string _drawnA, _drawnB, _drawnHold;
    private bool _wired;

    private void Awake()
    {
        _canvas = endA != null ? OverlayProjection.CanvasRectOf(endA) : null;
        Wire();
    }

    private void OnDestroy()
    {
        if (!_wired)
            return;
        if (board != null)
            board.Logged -= Mark;
        if (rulebook != null)
            rulebook.RowClicked -= PickRule;
    }

    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (board != null)
            board.Logged += Mark;
        if (rulebook != null)
            rulebook.RowClicked += PickRule;
    }

    /// <summary>The office camera the values' places are seen through (the office binder's).</summary>
    public void SetCamera(Camera office) => _camera = office;

    /// <summary>The day's introductions (the calendar and the rulebook show and take clicks from their days: Feature.Calendar, Feature.Rulebook).</summary>
    public void SetIntroductions(Introductions known, int day)
    {
        _known = known ?? Introductions.None;
        _day = day;
        if (rulebook != null)
            rulebook.gameObject.SetActive(_known.Has(day, Feature.Rulebook));
    }

    /// <summary>Today in the agency's calendar (<paramref name="agency"/>, shift day <paramref name="day"/>), as the papers print dates: what the desk's calendar holds.</summary>
    public void SetDay(AgencyContent agency, int day) => _today = agency != null ? AgencyCalendar.Today(agency.firstDate, day) : null;

    /// <summary>Today's directives, printed in the rulebook.</summary>
    public void SetRules(IReadOnlyList<TravelRuleSO> rules)
    {
        if (rulebook != null)
            rulebook.Show(rules);
    }

    /// <summary>A traveller is presented (their face is the one held against).</summary>
    public void BeginCase(CaseInstance inst) => _case = inst;

    /// <summary>The decision: no case until the next.</summary>
    public void EndCase() => _case = null;

    /// <summary>A click on the traveller: with a value held on the workbench, the traveller's face is held against it (the photo's check at the desk); otherwise the wheel opens.</summary>
    public void TravellerClicked()
    {
        if (board != null && compare != null && board.IsHolding && compare.Holding && _case != null && _case.look != null)
        {
            compare.Select(EvidencePicks.ForFace(_case.look), null);
            return;
        }
        if (wheel != null)
            wheel.Open();
    }

    /// <summary>A click on the desk's calendar: today's date is held on the workbench (or judged against the value held), once the calendar is introduced.</summary>
    public void CalendarClicked()
    {
        if (board != null && _today != null && _known.Has(_day, Feature.Calendar))
            board.PickToday(_today);
    }

    /// <summary>A rulebook row clicked: the directive is held on the workbench (or judged against the value held).</summary>
    private void PickRule(int index, TravelRuleSO rule)
    {
        if (board != null && _known.Has(_day, Feature.Rulebook))
            board.PickRule(index, rule);
    }

    /// <summary>A finding logged: a difference marks its papers' boxes for the case (FindingMarks).</summary>
    private void Mark(Finding finding)
    {
        if (desk == null)
            return;
        foreach ((int document, int field) in FindingMarks.Fields(finding))
            desk.MarkField(document, field, mistakeMark);
    }

    /// <summary>The workbench's line over the office, each frame: from the held value to the pointer, or between the two values compared, while the frame is closed and the values lie on the desk.</summary>
    private void LateUpdate()
    {
        if (board == null || lines == null)
            return;
        bool office = (view == null || view.Current == OfficeView.OfficeFocus) && (city == null || !city.IsOn);
        string hold = office ? board.HoldKey : null;
        (string a, string b, FindingLook look, string label) = board.Line;
        if (!office)
            a = b = null;

        if (hold != null)
        {
            bool shown = Place(endA, hold);
            if (hold != _drawnHold)
                lines.ShowHold(endA);
            _drawnHold = hold;
            _drawnA = _drawnB = null;
            if (!shown)
                endA.gameObject.SetActive(false);
            return;
        }
        _drawnHold = null;
        if (a != null && b != null)
        {
            Place(endA, a);
            Place(endB, b);
            if (a != _drawnA || b != _drawnB)
                lines.ShowLink(endA, endB, look, label);
            _drawnA = a;
            _drawnB = b;
            return;
        }
        if (_drawnA != null || endA.gameObject.activeSelf || endB.gameObject.activeSelf)
        {
            lines.Clear();
            endA.gameObject.SetActive(false);
            endB.gameObject.SetActive(false);
            _drawnA = _drawnB = null;
        }
    }

    /// <summary>Puts <paramref name="end"/> over the value with <paramref name="key"/> on the screen (shown), or hides it when the value is not on the desk or not in view.</summary>
    private bool Place(RectTransform end, string key)
    {
        Rect r = default;
        bool on = _canvas != null && TryBounds(key, out Bounds bounds) && TryScreenRect(bounds, out r);
        if (end.gameObject.activeSelf != on)
            end.gameObject.SetActive(on);
        if (!on)
            return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, r.min, null, out Vector2 min) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, r.max, null, out Vector2 max))
            return false;
        Vector2 size = Vector2.Max(max - min, minimumEnd);
        end.anchoredPosition = (min + max) / 2f;
        end.sizeDelta = size;
        return true;
    }

    /// <summary>Where a value lies in the office: a paper's box, the traveller's face, the calendar, a rulebook row; false for a value only on the PC.</summary>
    private bool TryBounds(string key, out Bounds bounds)
    {
        bounds = default;
        if (string.IsNullOrEmpty(key))
            return false;
        if (PickKeys.TryField(key, out int document, out int field))
            return desk != null && desk.TryFieldBounds(document, field, out bounds);
        if (key == PickKeys.Face)
        {
            if (traveller == null || _case == null)
                return false;
            bounds = traveller.Face;
            return true;
        }
        if (key == EntryKeys.CalendarToday)
        {
            if (calendar == null || !calendar.TryGetComponent(out Collider box) || !calendar.gameObject.activeInHierarchy)
                return false;
            bounds = box.bounds;
            return true;
        }
        return EntryKeys.TryRule(key, out int rule) && rulebook != null && rulebook.TryRowBounds(rule, out bounds);
    }

    /// <summary>A small rectangle at the screen's edge in the direction of <paramref name="world"/> as the camera sees it (a value beside or behind the view: the calendar or the face seen from the steep desk view).</summary>
    private static bool TryEdgeToward(Camera cam, Vector3 world, out Rect rect)
    {
        Vector3 local = cam.transform.InverseTransformDirection(world - cam.transform.position);
        Vector2 direction = new Vector2(local.x, local.y);
        if (direction.sqrMagnitude < 1e-8f)
            direction = Vector2.up;
        direction.Normalize();
        Vector2 half = new Vector2(Screen.width / 2f - EdgeMargin, Screen.height / 2f - EdgeMargin);
        float scale = Mathf.Min(Mathf.Abs(direction.x) > 1e-5f ? half.x / Mathf.Abs(direction.x) : float.MaxValue,
                                Mathf.Abs(direction.y) > 1e-5f ? half.y / Mathf.Abs(direction.y) : float.MaxValue);
        Vector2 centre = new Vector2(Screen.width / 2f, Screen.height / 2f) + direction * scale;
        rect = new Rect(centre - Vector2.one * EdgeMargin / 2f, Vector2.one * EdgeMargin);
        return true;
    }

    /// <summary>World bounds as a rectangle on the screen through the office camera (false when behind it or off the screen).</summary>
    private bool TryScreenRect(Bounds bounds, out Rect rect)
    {
        rect = default;
        Camera cam = _camera;
        if (cam == null)
            return false;
        Vector3 c = bounds.center, e = bounds.extents;
        int n = 0;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    _corners[n++] = c + Vector3.Scale(e, new Vector3(x, y, z));
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (Vector3 corner in _corners)
        {
            Vector3 s = cam.WorldToScreenPoint(corner);
            if (s.z <= 0f)
                return TryEdgeToward(cam, bounds.center, out rect);
            minX = Mathf.Min(minX, s.x);
            minY = Mathf.Min(minY, s.y);
            maxX = Mathf.Max(maxX, s.x);
            maxY = Mathf.Max(maxY, s.y);
        }
        rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        if (rect.xMax > 0f && rect.yMax > 0f && rect.xMin < Screen.width && rect.yMin < Screen.height)
            return true;
        // Off the screen (the calendar or the face seen from the desk view): the end waits at the screen's edge toward it, so the line still points there.
        Vector2 centre = new Vector2(Mathf.Clamp(rect.center.x, EdgeMargin, Screen.width - EdgeMargin), Mathf.Clamp(rect.center.y, EdgeMargin, Screen.height - EdgeMargin));
        rect = new Rect(centre - Vector2.one * EdgeMargin / 2f, Vector2.one * EdgeMargin);
        return true;
    }
}
