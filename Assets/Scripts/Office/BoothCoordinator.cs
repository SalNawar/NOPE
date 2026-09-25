using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies BoothRules and the wake rules to the office: from the view (the PC
/// frame open or not), the screen's power, the shift's phase (set by
/// GameManager), the wheel and a pending citation slip, it decides which of the
/// desktop, the PC, the power buttons, the desk props, the papers, the
/// traveller and the wheel take input; it wakes the screen for a presented
/// traveller and a finished scan, holds it on for a citation slip, and shows
/// the day-1 wheel note. Every reference is optional: a missing view counts as
/// the office view; a missing screen counts as on. Event-driven (no per-frame code).
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

    /// <summary>The traveller's hit zone (opens the wheel).</summary>
    [SerializeField] private Clickable travellerHitZone;

    /// <summary>The desk props (stamp, intercom, scanner, till, stability monitor, calendar, clock and the flavour props).</summary>
    [SerializeField] private Clickable[] props;

    /// <summary>The day-1 note above the traveller.</summary>
    [SerializeField] private TMP_Text wheelHint;

    /// <summary>The desk tuning (the wheel note's text and last day).</summary>
    [SerializeField] private DeskConfigSO config;

    private BoothPhase _phase = BoothPhase.NoTraveller;
    private int _day;
    private bool _citationPending;
    private bool _wheelOpenedToday;

    private void Awake()
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
        if (desk != null)
            desk.ScanFinished += HandleScanFinished;
    }

    private void OnDisable()
    {
        if (view != null)
            view.ViewChanged -= HandleView;
        if (screen != null)
            screen.PowerChanged -= Apply;
        if (wheel != null)
            wheel.OpenChanged -= HandleWheel;
        if (desk != null)
            desk.ScanFinished -= HandleScanFinished;
    }

    /// <summary>The first application, once every component has woken (Awake runs before any Start).</summary>
    private void Start() => Apply();

    /// <summary>Where the shift is (GameManager); presenting a traveller also wakes the screen.</summary>
    public void SetPhase(BoothPhase phase)
    {
        _phase = phase;
        if (phase == BoothPhase.TravellerAtDesk && screen != null)
            screen.Wake(WakeReason.TravellerPresented);
        Apply();
    }

    /// <summary>Starts a day: the day-1 notes may show again on their days.</summary>
    public void BeginDay(int day)
    {
        _day = day;
        _wheelOpenedToday = false;
        if (desk != null)
            desk.BeginDay(day);
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

    private void HandleScanFinished(int _)
    {
        if (screen != null)
            screen.Wake(WakeReason.ScanFinished);
        Apply();
    }

    /// <summary>The rules' input for the office as it is now.</summary>
    private BoothContext Context() => new BoothContext(
        view != null && view.Current == OfficeView.MonitorFocus,
        screen == null || screen.IsOn,
        _phase,
        wheel != null && wheel.IsOpen,
        _citationPending,
        false,
        false);

    /// <summary>Applies the rules. The wheel first: closing it changes the context the rest reads (its OpenChanged re-applies too, harmlessly).</summary>
    private void Apply()
    {
        if (wheel != null)
            wheel.SetCanOpen(BoothRules.Evaluate(Context()).WheelAllowed);

        BoothInput input = BoothRules.Evaluate(Context());
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
            desk.SetPapersLive(input.PapersLive);
        if (travellerHitZone != null)
            travellerHitZone.Interactable = input.TravellerLive;
        if (wheelHint != null)
            wheelHint.gameObject.SetActive(config != null &&
                DeskHints.WheelHintVisible(config.wheelHintKey, _day, config.wheelHintUntilDay, _wheelOpenedToday, _phase == BoothPhase.TravellerAtDesk));
    }
}
