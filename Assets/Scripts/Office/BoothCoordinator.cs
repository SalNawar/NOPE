using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies BoothRules and the wake rules to the office: from the view (the PC
/// frame open or not), the screen's power, the shift's phase (set by
/// GameManager), the wheel, a pending citation slip and the reading view, it
/// decides which of the desktop, the PC, the power buttons, the desk props,
/// the documents, the traveller, the wheel, the stamp bar, inspect mode, the
/// mat, the reading view's "▲ Back" control and the mouse wheel, the city
/// view and the PC's grey tab take input or show, whether the office case HUD
/// shows, and whether the documents and the rulebook show their values as
/// comparable (inspect mode: DeskInspect); it returns the reading view when
/// the next traveller is called, a newsletter shows or the wheel or the PC
/// frame opens, wakes the screen for a presented traveller and a finished
/// scan, holds it on for a citation slip, and shows the day-1 wheel note.
/// What a right-click or Esc backs out of is ControlRules' (OfficeControls).
/// Every reference is optional: a missing view counts as the office view; a
/// missing screen counts as on. Event-driven (no per-frame code).
/// </summary>
public sealed class BoothCoordinator : MonoBehaviour
{
    /// <summary>The office view (the PC frame open or not).</summary>
    [SerializeField] private OfficeViewController view;

    /// <summary>The PC's screen (power, desktop input).</summary>
    [SerializeField] private MonitorScreen screen;

    /// <summary>The papers and the scanner.</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The traveller wheel.</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>The office PC's click (opens the frame).</summary>
    [SerializeField] private Clickable crt;

    /// <summary>The office PC's power knob.</summary>
    [SerializeField] private Clickable powerButton;

    /// <summary>The PC frame's power button.</summary>
    [SerializeField] private Selectable framePowerButton;

    /// <summary>The traveller's hit zone (opens the wheel, or picks the face in inspect mode).</summary>
    [SerializeField] private Clickable travellerHitZone;

    /// <summary>The desk props (stamp, intercom, scanner, till, stability monitor, calendar, clock, the flavour props and the rulebook's clicks).</summary>
    [SerializeField] private Clickable[] props;

    /// <summary>The day-1 note above the traveller.</summary>
    [SerializeField] private TMP_Text wheelHint;

    /// <summary>The desk tuning (the wheel note's text and last day).</summary>
    [SerializeField] private DeskConfigSO config;

    /// <summary>The stamp bar (optional): the verdict at the desk.</summary>
    [SerializeField] private DeskStampTray stampTray;

    /// <summary>Inspection at the desk (optional): inspect mode and its red button.</summary>
    [SerializeField] private DeskInspect inspect;

    /// <summary>The rulebook on the desk (optional): its rows are comparable in inspect mode.</summary>
    [SerializeField] private DeskRulebook rulebook;

    /// <summary>The one input model (optional): the PC's grey tab.</summary>
    [SerializeField] private OfficeControls controls;

    /// <summary>The office case HUD (piece 10; optional): the office compare strip.</summary>
    [SerializeField] private OfficeCaseHud hud;

    /// <summary>The reading view (piece 10; optional): the camera tilted forward over the desk.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The city view (the desk-first redesign, item 6; optional): the hall turned left and the whole city faded in.</summary>
    [SerializeField] private CityView cityView;

    private BoothPhase _phase = BoothPhase.NoTraveller;
    private int _day;
    private bool _citationPending;
    private bool _wheelOpenedToday;

    /// <summary>Where the shift is (GameManager): the one input model reads it.</summary>
    public BoothPhase Phase => _phase;

    private void Awake()
    {
        PrintWheelHint();
        CultureThemeService.LabelsChanged += PrintWheelHint;
    }

    /// <summary>Stops following the labels' language.</summary>
    private void OnDestroy() => CultureThemeService.LabelsChanged -= PrintWheelHint;

    /// <summary>The wheel hint's words in the reading language (again whenever the labels' language changes).</summary>
    private void PrintWheelHint()
    {
        if (wheelHint != null && config != null)
            wheelHint.text = UiText.Get(config.wheelHintKey);
    }

    private void OnEnable()
    {
        if (view != null)
            view.ViewChanged += HandleView;
        if (screen != null)
            screen.PowerChanged += Apply;
        if (wheel != null)
            wheel.OpenChanged += HandleWheel;
        if (stampTray != null)
            stampTray.Changed += Apply;
        if (inspect != null)
            inspect.Changed += Apply;
        if (desk != null)
            desk.ScanFinished += HandleScanFinished;
        if (deskView != null)
            deskView.Changed += Apply;
        if (cityView != null)
            cityView.Changed += Apply;
    }

    private void OnDisable()
    {
        if (view != null)
            view.ViewChanged -= HandleView;
        if (screen != null)
            screen.PowerChanged -= Apply;
        if (wheel != null)
            wheel.OpenChanged -= HandleWheel;
        if (stampTray != null)
            stampTray.Changed -= Apply;
        if (inspect != null)
            inspect.Changed -= Apply;
        if (desk != null)
            desk.ScanFinished -= HandleScanFinished;
        if (deskView != null)
            deskView.Changed -= Apply;
        if (cityView != null)
            cityView.Changed -= Apply;
    }

    /// <summary>The first application, once every component has woken (Awake runs before any Start).</summary>
    private void Start() => Apply();

    /// <summary>Where the shift is (GameManager); presenting a traveller (the AVAILABLE sign calls them) also wakes the screen and returns the reading view and the city view, so the arrival is seen.</summary>
    public void SetPhase(BoothPhase phase)
    {
        _phase = phase;
        if (phase == BoothPhase.TravellerAtDesk)
        {
            if (screen != null)
                screen.Wake(WakeReason.TravellerPresented);
            if (deskView != null)
                deskView.Return();
            if (cityView != null)
                cityView.Return();
        }
        Apply();
    }

    /// <summary>Starts a day with its scanner upgrades (<paramref name="scanners"/>, the desk's): the day-1 notes may show again on their days.</summary>
    public void BeginDay(int day, ScannerDay scanners)
    {
        _day = day;
        _wheelOpenedToday = false;
        if (desk != null)
            desk.BeginDay(day, scanners);
        Apply();
    }

    /// <summary>A citation slip waits for Acknowledge (or no longer does): it holds the screen on and makes the power buttons inert.</summary>
    public void SetCitationPending(bool pending)
    {
        _citationPending = pending;
        if (screen != null)
            screen.SetHeld(pending);
        Apply();
    }

    private void HandleView(OfficeView _) => Apply();

    private void HandleWheel()
    {
        if (wheel.IsOpen)
            _wheelOpenedToday = true;
        Apply();
    }

    /// <summary>A scan finished (the paper and its pass are the PC's, CaseDocumentsPresenter): the screen wakes and the rules re-apply.</summary>
    private void HandleScanFinished(int paper, ScanPass pass)
    {
        if (screen != null)
            screen.Wake(WakeReason.ScanFinished);
        Apply();
    }

    /// <summary>The rules' input for the office as it is now.</summary>
    private BoothContext Context() => new BoothContext(
        focused: view != null && view.Current == OfficeView.MonitorFocus,
        screenOn: screen == null || screen.IsOn,
        phase: _phase,
        wheelOpen: wheel != null && wheel.IsOpen,
        citationPending: _citationPending,
        deskView: deskView != null && deskView.IsOn);

    /// <summary>Applies the rules. The wheel, the stamp bar, inspect mode and the reading view first: closing, stowing, leaving or returning changes the context the rest reads (their events re-apply too, harmlessly).</summary>
    private void Apply()
    {
        BoothInput first = BoothRules.Evaluate(Context());
        if (wheel != null)
            wheel.SetCanOpen(first.WheelAllowed);
        if (stampTray != null)
            stampTray.SetLive(first.StampsLive, first.PropsLive);
        if (inspect != null)
            inspect.SetLive(first.InspectLive, first.PropsLive);
        if (deskView != null && !first.DeskViewAllowed)
            deskView.Return();

        BoothInput input = BoothRules.Evaluate(Context());
        bool inspecting = inspect != null && inspect.IsOn;
        if (cityView != null)
            cityView.SetLive(input.NormalViewLive);
        if (screen != null)
            screen.SetInteractive(input.DesktopInteractive);
        if (crt != null)
            crt.Interactable = input.CrtFocusable;
        if (powerButton != null)
            powerButton.Interactable = input.PowerButtonLive;
        if (framePowerButton != null)
            framePowerButton.interactable = input.PowerButtonLive;
        if (props != null)
            foreach (Clickable prop in props)
                if (prop != null)
                    prop.Interactable = input.PropsLive;
        if (desk != null)
        {
            desk.SetPapersLive(input.PapersLive);
            desk.SetInspecting(inspecting);
        }
        if (rulebook != null)
            rulebook.SetInspecting(inspecting);
        if (hud != null)
            hud.SetVisible(input.CaseHudVisible);
        if (deskView != null)
        {
            deskView.SetToggleLive(input.DeskViewAllowed);
            deskView.SetBackLive(input.DeskViewBackLive);
            deskView.SetScrollInLive(input.NormalViewLive);
        }
        if (controls != null)
            controls.SetPcTab(input.PcSwitchLive, view != null && view.Current == OfficeView.MonitorFocus);
        if (travellerHitZone != null)
            travellerHitZone.Interactable = input.TravellerLive;
        if (wheelHint != null)
            wheelHint.gameObject.SetActive(config != null &&
                DeskHints.WheelHintVisible(config.wheelHintKey, _day, config.wheelHintUntilDay, _wheelOpenedToday, _phase == BoothPhase.TravellerAtDesk));
    }
}
