using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The PC's screen: the desktop canvas (shown in the PC frame and cloned live
/// onto the office PC's glass), its power (PcScreen: the display and the
/// desktop's input go dark on both, the PC keeps running) and the frame's LED.
/// BoothCoordinator gates the desktop's input (SetInteractive) and holds the
/// screen on while a citation slip waits.
/// </summary>
public sealed class MonitorScreen : MonoBehaviour
{
    /// <summary>The World Space desktop canvas (on the desktop's own layer, drawn by the frame and clone cameras).</summary>
    [SerializeField] private Canvas desktopCanvas;

    /// <summary>The desktop canvas's raycaster (off unless the desktop may take input).</summary>
    [SerializeField] private GraphicRaycaster desktopRaycaster;

    /// <summary>The frame's power LED, tinted with the power state.</summary>
    [SerializeField] private Graphic powerLed;

    /// <summary>The desktop's clone on the office PC (dark with the screen).</summary>
    [SerializeField] private PcScreenClone clone;

    /// <summary>The desk tuning (the start state, the wake rules, the LED colours).</summary>
    [SerializeField] private DeskConfigSO config;

    private PcScreen _screen;

    /// <summary>True while the screen is on.</summary>
    public bool IsOn => _screen.IsOn;

    /// <summary>Raised after the screen turns on or off.</summary>
    public event Action PowerChanged;

    private void Awake()
    {
        if (config != null)
        {
            _screen = new PcScreen(config.screenStartsOn, config.wake);
        }
        else
        {
            _screen = new PcScreen(true, null);
            Debug.LogWarning("[MonitorScreen] No DeskConfigSO wired: the screen starts on and never wakes itself. Run Tools > TimeDesk > Build Office UI.", this);
        }

        _screen.Changed += HandleChanged;
        Apply();
        SetInteractive(false);
    }

    private void OnDestroy()
    {
        _screen.Changed -= HandleChanged;
    }

    /// <summary>Turns the screen on or off (the power buttons' persistent call); a held screen stays on.</summary>
    public void TogglePower() => _screen.Toggle();

    /// <summary>Turns the screen off (Start > Turn off screen); a held screen stays on.</summary>
    public void TurnOff() => _screen.TurnOff();

    /// <summary>Turns a dark screen on when the reason's wake rule is on.</summary>
    public void Wake(WakeReason reason) => _screen.Wake(reason);

    /// <summary>Holds the screen on (a pending citation slip) or releases it.</summary>
    public void SetHeld(bool held) => _screen.SetHeld(held);

    /// <summary>
    /// Lets the desktop take input or not (its raycaster). Turning it off also
    /// clears the EventSystem's selection when it is on the desktop, so a focused
    /// field (the Records search) stops taking keys.
    /// </summary>
    public void SetInteractive(bool on)
    {
        if (desktopRaycaster != null)
            desktopRaycaster.enabled = on;
        if (on || desktopCanvas == null)
            return;

        EventSystem events = EventSystem.current;
        GameObject selected = events != null ? events.currentSelectedGameObject : null;
        if (selected != null && selected.transform.IsChildOf(desktopCanvas.transform))
            events.SetSelectedGameObject(null);
    }

    private void HandleChanged()
    {
        Apply();
        PowerChanged?.Invoke();
    }

    /// <summary>Shows or hides the desktop in the frame and on the office PC, and tints the LED.</summary>
    private void Apply()
    {
        bool on = _screen.IsOn;
        if (desktopCanvas != null)
            desktopCanvas.enabled = on;
        if (clone != null)
            clone.SetOn(on);
        if (powerLed != null && config != null)
            powerLed.color = on ? config.ledOnColor : config.ledOffColor;
    }
}
