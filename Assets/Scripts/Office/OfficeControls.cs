using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The office's one input model (Saleh 2026-10-06: "copy the controls of
/// Papers, Please 1:1 ... always have some shortcuts to pull the stamp ...
/// similarly for the pc ... I should hold to pick and sometimes right click to
/// cancel but not all actions like matching"): the one poller of the
/// office's keys and of the right mouse button, applying ControlRules. A
/// right-click or Esc backs out of the innermost mode, one per press
/// (BackOut: a drag, the wheel, the PC, the value held, inspect mode, the
/// city, the reading view), after the PC's own chain had its
/// turn (DesktopKeyboard runs first and stamps the frame it took the press:
/// a menu, the card, the search, a field, a held value on the PC). SPACE
/// toggles inspect mode, TAB the reading view over the stamp rack, Q the PC (its grey tab on the
/// overlay's left edge does the same: TogglePc), A and D (or the arrows) look
/// at the city and back, F1 shows the keys (the PC's shortcut card, whose
/// desk section is ControlRules.DeskCard); each closes what is in its way
/// first (the wheel, the city), so a key does the same from every desk
/// state. While anything is held (the desk's HoldRegistry: a paper, the
/// folder, a dater, a tool), a right-click, Esc or a view key (TAB, Q, A, D,
/// F1) lets go of it first and does nothing else (BUG-55: no view change under
/// a held thing, nothing committed), and losing the focus (Alt+Tab) lets go
/// of it without committing it. While an overlay holds the input
/// (InputGate.Blocked) it takes no input at all; F1 does nothing while the PC
/// cannot open (PcFrame.CanOpen: the screen is off, BUG-58c).
/// Left-clicks act where they land (the documents, the stamps, the
/// tabs, the buttons). It runs after the desktop's poller and before the
/// EventSystem; Build Office UI builds it and the PC's tab; BoothCoordinator
/// says when the tab is live.
/// </summary>
[DefaultExecutionOrder(-1080)]
public sealed class OfficeControls : MonoBehaviour
{
    /// <summary>The office view (the PC frame open or not).</summary>
    [SerializeField] private OfficeViewController view;

    /// <summary>The desktop's keyboard poller (optional): it takes a back-out press first while the PC is open, and says when a text field types.</summary>
    [SerializeField] private DesktopKeyboard keyboard;

    /// <summary>The booth (the shift's phase: a traveller at the desk, a newsletter).</summary>
    [SerializeField] private BoothCoordinator booth;

    /// <summary>The traveller wheel.</summary>
    [SerializeField] private TravellerWheel wheel;

    /// <summary>Inspect mode (and the value held on the workbench).</summary>
    [SerializeField] private DeskInspect inspect;

    /// <summary>The stamp rack.</summary>
    [SerializeField] private DeskStampTray stamps;

    /// <summary>The reading view.</summary>
    [SerializeField] private DeskView deskView;

    /// <summary>The city view.</summary>
    [SerializeField] private CityView cityView;

    /// <summary>The documents (a drag a back-out cancels; a carried stamp is the stamp rack's).</summary>
    [SerializeField] private DeskController desk;

    /// <summary>The PC's grey tab (on the overlay's left edge): a click switches to the PC and back.</summary>
    [SerializeField] private Button pcTab;

    /// <summary>The tab's label: "PC" at the desk, "DESK" on the PC, with the key.</summary>
    [SerializeField] private TMP_Text pcTabLabel;

    private bool _pcLive;
    private bool _pcShownOpen;

    /// <summary>What the last right-click or Esc backed out of (the probes read it).</summary>
    public BackOutStep LastBackOut { get; private set; }

    private void Awake()
    {
        if (pcTab != null)
            pcTab.onClick.AddListener(TogglePc);
        LabelPcTab(false);
    }

    /// <summary>The office as the rules read it now.</summary>
    public ControlState State() => new ControlState(
        pcOpen: view != null && view.Current == OfficeView.MonitorFocus,
        newsletter: booth != null && booth.Phase == BoothPhase.Newsletter,
        travellerAtDesk: booth != null && booth.Phase == BoothPhase.TravellerAtDesk,
        wheelOpen: wheel != null && wheel.IsOpen,
        valueHeld: inspect != null && inspect.ValueHeld,
        inspecting: inspect != null && inspect.IsOn,
        cityView: cityView != null && cityView.IsOn,
        deskView: deskView != null && deskView.IsOn,
        dragging: HoldRegistry.Desk.IsHolding || (desk != null && desk.IsDragging) || (stamps != null && stamps.IsCarrying),
        textFieldFocused: keyboard != null && keyboard.TextFieldFocused);

    /// <summary>Polls the back-out (Esc, the right mouse button) and the office's keys, once per press.</summary>
    private void Update()
    {
        if (InputGate.Blocked)
            return;
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool back = (kb != null && kb.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame);
        if (back && (keyboard == null || keyboard.EscapeTakenFrame != Time.frameCount))
            BackOut();
        if (kb == null)
            return;
        if (kb.spaceKey.wasPressedThisFrame)
            Press(ControlKey.Inspect);
        if (kb.tabKey.wasPressedThisFrame)
            Press(ControlKey.Stamps);
        if (kb.qKey.wasPressedThisFrame)
            Press(ControlKey.Pc);
        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)
            Press(ControlKey.CityLeft);
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
            Press(ControlKey.CityRight);
        if (kb.f1Key.wasPressedThisFrame)
            Press(ControlKey.Help);
    }

    /// <summary>One office key: what ControlRules says it does here, done.</summary>
    public void Press(ControlKey key) => Do(ControlRules.Resolve(key, State()));

    /// <summary>A right-click or Esc: backs out of the innermost mode (ControlRules.BackOut) and returns what it backed out of.</summary>
    public BackOutStep BackOut()
    {
        LastBackOut = ControlRules.BackOut(State());
        switch (LastBackOut)
        {
            case BackOutStep.CancelDrag:
                LetGo();
                break;
            case BackOutStep.CloseWheel:
                wheel.Close();
                break;
            case BackOutStep.ClosePc:
                view.FocusOffice();
                break;
            case BackOutStep.DropValue:
                inspect.DropValue();
                break;
            case BackOutStep.LeaveInspect:
                inspect.SetOn(false);
                break;
            case BackOutStep.LeaveCity:
                cityView.Return();
                break;
            case BackOutStep.LeaveDeskView:
                deskView.Return();
                break;
        }
        return LastBackOut;
    }

    /// <summary>Lets go of what is held: the hold registry's hold (it cancels its drag: back where it was picked up), else a paper's drag or a carried dater.</summary>
    private void LetGo()
    {
        if (HoldRegistry.Desk.IsHolding)
            HoldRegistry.Desk.Cancel();
        else if ((desk == null || !desk.CancelDrag()) && stamps != null)
            stamps.CancelCarry();
    }

    /// <summary>The game lost the focus (Alt+Tab, another window): whatever is held is let go without being committed (BUG-55: the Input System's release on focus loss never drops it).</summary>
    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
            HoldRegistry.Desk.OnFocusLost();
    }

    /// <summary>The PC's grey tab: to the PC, or back to the desk (only while the switch is live).</summary>
    public void TogglePc()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        if (_pcLive)
            Do(view != null && view.Current == OfficeView.MonitorFocus ? ControlAction.ClosePc : ControlAction.OpenPc);
    }

    /// <summary>Shows the PC's tab as live or not (BoothRules.PcSwitchLive) and labelled for where it goes (<paramref name="pcOpen"/>: back to the desk).</summary>
    public void SetPcTab(bool live, bool pcOpen)
    {
        _pcLive = live;
        if (pcTab != null)
        {
            if (pcTab.gameObject.activeSelf != live)
                pcTab.gameObject.SetActive(live);
            pcTab.interactable = live;
        }
        if (pcOpen != _pcShownOpen)
            LabelPcTab(pcOpen);
    }

    /// <summary>Does <paramref name="action"/>, closing what is in its way first (the wheel; the city for the desk's tools), so a key does the same from every desk state.</summary>
    private void Do(ControlAction action)
    {
        switch (action)
        {
            case ControlAction.CancelHold:
                LetGo();
                break;
            case ControlAction.ToggleInspect:
                ClearTheDesk();
                if (inspect != null)
                    inspect.Toggle();
                break;
            case ControlAction.ToggleStamps:
                ClearTheDesk();
                if (stamps != null)
                    stamps.ToggleRack();
                break;
            case ControlAction.OpenPc:
                if (wheel != null)
                    wheel.Close();
                if (view != null)
                    view.FocusMonitor();
                break;
            case ControlAction.ClosePc:
                if (view != null)
                    view.FocusOffice();
                break;
            case ControlAction.LookAtCity:
                if (cityView != null)
                    cityView.Look();
                break;
            case ControlAction.LeaveCity:
                if (cityView != null)
                    cityView.Return();
                break;
            case ControlAction.ShowKeys:
                if (!PcFrame.CanOpen)
                    break;
                if (wheel != null)
                    wheel.Close();
                if (view != null)
                    view.FocusMonitor();
                if (keyboard != null && !keyboard.CardOpen)
                    keyboard.ToggleCard();
                break;
        }
    }

    /// <summary>The desk's tools need the desk: the wheel closes and the view turns back from the city.</summary>
    private void ClearTheDesk()
    {
        if (wheel != null)
            wheel.Close();
        if (cityView != null)
            cityView.Return();
    }

    /// <summary>The PC's tab reads "PC · Q" at the desk and "DESK · Q" on the PC.</summary>
    private void LabelPcTab(bool pcOpen)
    {
        _pcShownOpen = pcOpen;
        if (pcTabLabel != null)
            pcTabLabel.text = UiText.Format(pcOpen ? "controls.deskTab" : "controls.pcTab", ControlRules.PcKey);
    }
}
