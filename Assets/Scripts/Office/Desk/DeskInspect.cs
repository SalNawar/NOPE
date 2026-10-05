using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Inspection at the desk (the desk-first redesign, Saleh 2026-10-05, item
/// 11: "the player should be able to check documents at the desk, highlight
/// clear mistakes and approve; scanning allows using additional features").
/// Papers, Please's inspect mode (Saleh 2026-10-06): the red inspect button
/// at the overlay's bottom right ("SPACE" printed on it) and the SPACE key
/// (OfficeControls) toggle it (Toggle); a right-click or Esc lets go of a held
/// value, then leaves it (ControlRules.BackOut). In inspect mode every
/// comparable thing highlights (the papers' values, the rulebook's rows; the
/// calendar and the traveller outline under the pointer) and a left-click on
/// one holds it, a second judges the pair on the one workbench (MatchBoard:
/// the same findings, the same log, the same evidence for the decision, and a
/// difference unlocks its wheel question): a value on a paper
/// (DeskController routes it into the compare), the calendar on the desk
/// (today's date, once the calendar is introduced), the traveller's face, or
/// a row of the rulebook on the desk (today's directives, once the rulebook
/// is introduced). Outside inspect mode no click compares: the traveller
/// opens the wheel, the calendar and the rulebook's rows are only read.
/// Leaving inspect mode lets go of a held value. It is live while
/// BoothRules.InspectLive (false leaves it). The workbench's line is drawn over the office too,
/// between the two values where they lie (MatchLines over two proxy ends
/// placed each frame on the values' places on the screen), dashed from a held
/// value to the pointer; it shows while the PC frame is closed and both
/// values are in the office (an end off the screen, the calendar or the face
/// seen from the desk view, waits at the screen's edge toward it with an arrow
/// pointing the way; a value only on the PC draws nothing here). While the line
/// labels a pair, the office compare strip does not repeat it
/// (CompareController.SetStripCovered; the desk-first polish). A logged
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

    /// <summary>The arrow at the first end, shown while that end waits at the screen's edge for a value off the screen (it points the way); its graphic points up unrotated.</summary>
    [SerializeField] private RectTransform arrowA;

    /// <summary>The arrow at the second end.</summary>
    [SerializeField] private RectTransform arrowB;

    /// <summary>A clear mistake's mark on a paper's box (a translucent red; it stays for the case).</summary>
    [SerializeField] private Color mistakeMark = new Color(0.86f, 0.16f, 0.12f, 0.32f);

    /// <summary>The smallest end on the screen, in canvas px (a far value still gets a visible box).</summary>
    [SerializeField] private Vector2 minimumEnd = new Vector2(24f, 18f);

    /// <summary>The red inspect button ("SPACE" printed on it) at the overlay's bottom right: a click toggles inspect mode.</summary>
    [SerializeField] private Button inspectButton;

    /// <summary>What shows while inspect mode is on (the button's lit ring and the hint beside it: "click two things to compare").</summary>
    [SerializeField] private GameObject onState;

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
    private bool _live;

    /// <summary>True while inspect mode is on: a click on a comparable thing compares.</summary>
    public bool IsOn { get; private set; }

    /// <summary>Raised when inspect mode turns on or off (the booth tints the papers and the rulebook).</summary>
    public event Action Changed;

    /// <summary>True while a value is held on the workbench (the first of a comparison): a right-click or Esc lets go of it first.</summary>
    public bool ValueHeld => board != null && board.IsHolding;

    /// <summary>Lets go of the value held on the workbench (a right-click or Esc: ControlRules.BackOut).</summary>
    public void DropValue()
    {
        if (board != null && board.IsHolding)
            board.Release();
    }

    /// <summary>Raised when a paper not handed over is flagged missing on the rulebook (its request's id): the controller flags it as the PC's Papers menu does.</summary>
    public event Action<string> MissingFlagged;

    private void Awake()
    {
        _canvas = endA != null ? OverlayProjection.CanvasRectOf(endA) : null;
        if (inspectButton != null)
            inspectButton.onClick.AddListener(Toggle);
        if (onState != null)
            onState.SetActive(false);
        Wire();
    }

    /// <summary>The red button, SPACE: inspect mode on, or off (nothing while it is not live).</summary>
    public void Toggle()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        if (_live || IsOn)
            SetOn(!IsOn);
    }

    /// <summary>Turns inspect mode on (only while live) or off; off lets go of a value held on the workbench.</summary>
    public void SetOn(bool on)
    {
        on &= _live;
        if (on == IsOn)
            return;
        IsOn = on;
        if (!on && board != null && board.IsHolding)
            board.Release();
        if (onState != null)
            onState.SetActive(on);
        Changed?.Invoke();
    }

    /// <summary>Lets inspect mode be used (<paramref name="live"/>: BoothRules.InspectLive; false leaves it) and shows the red button (<paramref name="shown"/>: BoothRules.PropsLive).</summary>
    public void SetLive(bool live, bool shown)
    {
        _live = live;
        if (inspectButton != null)
        {
            if (inspectButton.gameObject.activeSelf != shown)
                inspectButton.gameObject.SetActive(shown);
            inspectButton.interactable = live;
        }
        if (!live)
            SetOn(false);
    }

    private void OnDestroy()
    {
        if (!_wired)
            return;
        if (board != null)
            board.Logged -= Mark;
        if (rulebook != null)
        {
            rulebook.RowClicked -= PickRule;
            rulebook.PaperFlagged -= FlagPaper;
        }
    }

    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (board != null)
            board.Logged += Mark;
        if (rulebook != null)
        {
            rulebook.RowClicked += PickRule;
            rulebook.PaperFlagged += FlagPaper;
        }
    }

    /// <summary>The papers the traveller has not handed over and where each stands (the controller, whenever they change): the rulebook lists them to flag.</summary>
    public void SetMissing(MissingPapers missing, CasePapers papers)
    {
        if (rulebook != null)
            rulebook.ShowMissing(missing, papers);
    }

    /// <summary>A paper flagged missing on the rulebook.</summary>
    private void FlagPaper(string requestId) => MissingFlagged?.Invoke(requestId);

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

    /// <summary>A click on the traveller: in inspect mode their face is picked (held, or judged against the value held: the photo's check at the desk); otherwise the wheel opens.</summary>
    public void TravellerClicked()
    {
        if (IsOn)
        {
            if (compare != null && _case != null && _case.look != null)
                compare.Select(EvidencePicks.ForFace(_case.look), null);
            return;
        }
        if (wheel != null)
            wheel.Open();
    }

    /// <summary>A click on the desk's calendar: in inspect mode today's date is held on the workbench (or judged against the value held), once the calendar is introduced.</summary>
    public void CalendarClicked()
    {
        if (IsOn && board != null && _today != null && _known.Has(_day, Feature.Calendar))
            board.PickToday(_today);
    }

    /// <summary>A rulebook row clicked: in inspect mode the directive is held on the workbench (or judged against the value held).</summary>
    private void PickRule(int index, TravelRuleSO rule)
    {
        if (IsOn && board != null && _known.Has(_day, Feature.Rulebook))
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
            bool shown = Place(endA, arrowA, hold);
            if (hold != _drawnHold)
                lines.ShowHold(endA);
            _drawnHold = hold;
            _drawnA = _drawnB = null;
            if (!shown)
                endA.gameObject.SetActive(false);
            Cover(false);
            return;
        }
        _drawnHold = null;
        if (a != null && b != null)
        {
            bool shownA = Place(endA, arrowA, a);
            bool shownB = Place(endB, arrowB, b);
            if (a != _drawnA || b != _drawnB)
                lines.ShowLink(endA, endB, look, label);
            _drawnA = a;
            _drawnB = b;
            Cover(shownA && shownB);
            return;
        }
        Cover(false);
        if (_drawnA != null || endA.gameObject.activeSelf || endB.gameObject.activeSelf)
        {
            lines.Clear();
            endA.gameObject.SetActive(false);
            endB.gameObject.SetActive(false);
            _drawnA = _drawnB = null;
        }
    }

    /// <summary>Tells the office compare strip whether the line over the office labels the pair now.</summary>
    private void Cover(bool covered)
    {
        if (compare != null)
            compare.SetStripCovered(covered);
    }

    /// <summary>Puts <paramref name="end"/> over the value with <paramref name="key"/> on the screen (shown), with its <paramref name="arrow"/> pointing the way when the value is off the screen, or hides it when the value is not on the desk or not in view.</summary>
    private bool Place(RectTransform end, RectTransform arrow, string key)
    {
        Rect r = default;
        Vector2 toward = default;
        bool on = _canvas != null && TryBounds(key, out Bounds bounds) && TryScreenRect(bounds, out r, out toward);
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
        if (arrow != null)
        {
            bool edge = toward != Vector2.zero;
            if (arrow.gameObject.activeSelf != edge)
                arrow.gameObject.SetActive(edge);
            if (edge)
                arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg - 90f);
        }
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

    /// <summary>A small rectangle at the screen's edge in the direction of <paramref name="world"/> as the camera sees it (a value beside or behind the view: the calendar or the face seen from the steep desk view), and that direction on the screen (<paramref name="toward"/>, unit).</summary>
    private static bool TryEdgeToward(Camera cam, Vector3 world, out Rect rect, out Vector2 toward)
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
        toward = direction;
        return true;
    }

    /// <summary>World bounds as a rectangle on the screen through the office camera; a value off the screen gets a small rectangle at the screen's edge toward it and the way to it (<paramref name="toward"/>, unit; zero for a value on the screen). False without a camera.</summary>
    private bool TryScreenRect(Bounds bounds, out Rect rect, out Vector2 toward)
    {
        rect = default;
        toward = default;
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
                return TryEdgeToward(cam, bounds.center, out rect, out toward);
            minX = Mathf.Min(minX, s.x);
            minY = Mathf.Min(minY, s.y);
            maxX = Mathf.Max(maxX, s.x);
            maxY = Mathf.Max(maxY, s.y);
        }
        rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        if (rect.xMax > 0f && rect.yMax > 0f && rect.xMin < Screen.width && rect.yMin < Screen.height)
            return true;
        // Off the screen (the calendar or the face seen from the desk view): the end waits at the screen's edge in the value's direction as the camera sees it (the projection of a point nearly beside the camera swings wide), so the line still points there, and its arrow points the way.
        return TryEdgeToward(cam, bounds.center, out rect, out toward);
    }
}
