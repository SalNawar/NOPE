using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The PC desktop's windows (the PC redesign WN1-WN3, DK8, section 3.2; on
/// the desktop canvas). It owns the one WindowStack and applies it: a window
/// shows while it is open and not minimised, the visible ones take the
/// stack's z-order as their sibling order on the window layer, and the
/// taskbar has one button per open window in open order (the focused
/// window's pressed, a minimised one's faded; a click minimises the focused
/// window, else restores and focuses it). While the desktop takes input (its
/// raycaster is on: BoothRules.DesktopInteractive), a press anywhere on the
/// desktop closes the Start menu when it lands outside it, focuses the window
/// under the pointer (its own raycast through the frame camera: a button
/// inside a window takes the pointer-down itself), and on the empty desktop
/// leaves nothing focused; Escape runs the desktop's part of the chain
/// (DesktopEscapeRule: leave a field, close the Start menu, cancel a drag)
/// and stamps its frame, so the PC frame's Escape (OfficeViewController)
/// skips that press. It runs before the EventSystem (whose script order is
/// -1000; its cancel would leave a field first) and the office's pollers.
/// </summary>
[DefaultExecutionOrder(-1100)]
public sealed class DesktopWindowManager : MonoBehaviour
{
    /// <summary>The desktop's knobs (the maximise area, the double-click).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>The desktop canvas's raycaster (on only while the desktop takes input; the press focus uses it).</summary>
    [SerializeField] private GraphicRaycaster raycaster;

    /// <summary>The taskbar's strip of window buttons (a horizontal layout that shrinks them to fit).</summary>
    [SerializeField] private RectTransform taskbarButtons;

    /// <summary>The window button template (inactive, in the strip), cloned per open window.</summary>
    [SerializeField] private Button taskbarButtonTemplate;

    /// <summary>The Start menu's shell (a press outside the menu closes it; Escape closes it).</summary>
    [SerializeField] private DesktopShell shell;

    /// <summary>The empty desktop's graphics (the wallpaper and the case dim): a press on one leaves no window focused.</summary>
    [SerializeField] private Graphic[] emptyDesktop;

    /// <summary>The focused window's button tint (pressed).</summary>
    [SerializeField] private Color focusedTint = new Color(0.72f, 0.72f, 0.72f, 1f);

    /// <summary>A minimised window's button tint (flat, faded).</summary>
    [SerializeField] private Color minimisedTint = new Color(1f, 1f, 1f, 0.55f);

    private readonly WindowStack _stack = new WindowStack();

    /// <summary>Each window put under the stack, by its id, and each id by its window.</summary>
    private readonly Dictionary<string, DesktopWindow> _windows = new Dictionary<string, DesktopWindow>();
    private readonly Dictionary<DesktopWindow, string> _ids = new Dictionary<DesktopWindow, string>();

    private readonly Dictionary<string, TaskbarButton> _buttons = new Dictionary<string, TaskbarButton>();
    private readonly List<string> _scratch = new List<string>();
    private readonly List<RaycastResult> _hits = new List<RaycastResult>();
    private PointerEventData _press;
    private WindowDrag _drag;

    /// <summary>Numbers the windows' ids.</summary>
    private int _registered;

    /// <summary>One taskbar button and its label.</summary>
    private struct TaskbarButton
    {
        public Button Button;
        public TMP_Text Label;
    }

    /// <summary>The frame in which the desktop last took an Escape press (-1: never); the PC frame's Escape skips that frame.</summary>
    public int EscapeTakenFrame { get; private set; } = -1;

    /// <summary>A maximised window's bottom edge above the desktop's bottom (the taskbar and the dock).</summary>
    public float MaximisedBottom => config != null ? config.MaximisedBottom : 0f;

    /// <summary>The double-click's time, in seconds.</summary>
    public float DoubleClickSeconds => config != null ? config.doubleClickSeconds : 0f;

    /// <summary>The double-click's distance, in desktop units.</summary>
    public float DoubleClickDistance => config != null ? config.doubleClickDistance : 0f;

    private void Awake()
    {
        _stack.Changed += Apply;
        _press = new PointerEventData(EventSystem.current);
        if (taskbarButtonTemplate != null)
            taskbarButtonTemplate.gameObject.SetActive(false);
        if (config == null)
            Debug.LogWarning("[DesktopWindowManager] No DesktopConfigSO wired: maximise fills the whole desktop and title bars take no double-click. Run Tools > TimeDesk > Build Office UI.", this);
    }

    private void OnDestroy() => _stack.Changed -= Apply;

    /// <summary>Shows the window (a minimised one restores), raised and focused.</summary>
    public void Open(DesktopWindow window)
    {
        if (window != null)
            _stack.Open(Register(window));
    }

    /// <summary>Raises and focuses an open window.</summary>
    public void Focus(DesktopWindow window)
    {
        if (window != null)
            _stack.Focus(Register(window));
    }

    /// <summary>Hides the window, keeping its taskbar button.</summary>
    public void Minimise(DesktopWindow window)
    {
        if (window != null)
            _stack.Minimise(Register(window));
    }

    /// <summary>Closes the window (its taskbar button goes).</summary>
    public void Close(DesktopWindow window)
    {
        if (window != null)
            _stack.Close(Register(window));
    }

    /// <summary>A title-bar drag started (Escape may cancel it).</summary>
    public void BeginDrag(WindowDrag drag) => _drag = drag;

    /// <summary>A title-bar drag ended.</summary>
    public void EndDrag(WindowDrag drag)
    {
        if (_drag == drag)
            _drag = null;
    }

    /// <summary>
    /// Puts a window under the stack (a new one gets an id and is hidden until
    /// opened) and returns its id; windows destroyed since (a case's document
    /// windows, closed first) are dropped.
    /// </summary>
    private string Register(DesktopWindow window)
    {
        if (_ids.TryGetValue(window, out string known))
            return known;

        _scratch.Clear();
        foreach (KeyValuePair<string, DesktopWindow> pair in _windows)
            if (pair.Value == null && !_stack.IsOpen(pair.Key))
                _scratch.Add(pair.Key);
        foreach (string gone in _scratch)
        {
            _ids.Remove(_windows[gone]);
            _windows.Remove(gone);
        }

        string id = window.name + "#" + (++_registered).ToString(CultureInfo.InvariantCulture);
        _windows.Add(id, window);
        _ids.Add(window, id);
        if (window.gameObject.activeSelf)
            window.gameObject.SetActive(false);
        return id;
    }

    /// <summary>Applies the stack: which windows show, their order, the taskbar's buttons.</summary>
    private void Apply()
    {
        foreach (KeyValuePair<string, DesktopWindow> pair in _windows)
        {
            if (pair.Value == null)
                continue;
            bool visible = _stack.IsOpen(pair.Key) && !_stack.IsMinimised(pair.Key);
            if (pair.Value.gameObject.activeSelf != visible)
                pair.Value.gameObject.SetActive(visible);
        }

        IReadOnlyList<string> z = _stack.ZOrder;
        for (int i = 0; i < z.Count; i++)
            if (_windows.TryGetValue(z[i], out DesktopWindow w) && w != null)
                w.transform.SetAsLastSibling();

        ApplyTaskbar();
    }

    /// <summary>One button per open window in open order: the title, pressed when focused, faded when minimised.</summary>
    private void ApplyTaskbar()
    {
        if (taskbarButtons == null || taskbarButtonTemplate == null)
            return;

        RemoveClosedButtons();
        IReadOnlyList<string> order = _stack.TaskbarOrder;
        for (int i = 0; i < order.Count; i++)
            if (_windows.TryGetValue(order[i], out DesktopWindow window) && window != null)
                ShowButton(order[i], window);
    }

    /// <summary>Removes the buttons of closed (or destroyed) windows.</summary>
    private void RemoveClosedButtons()
    {
        _scratch.Clear();
        foreach (KeyValuePair<string, TaskbarButton> pair in _buttons)
            if (!_stack.IsOpen(pair.Key) || !_windows.TryGetValue(pair.Key, out DesktopWindow w) || w == null)
                _scratch.Add(pair.Key);
        foreach (string gone in _scratch)
        {
            if (_buttons[gone].Button != null)
                Destroy(_buttons[gone].Button.gameObject);
            _buttons.Remove(gone);
        }
    }

    /// <summary>A window's button, last in the strip so far: its title and its look.</summary>
    private void ShowButton(string id, DesktopWindow window)
    {
        if (!_buttons.TryGetValue(id, out TaskbarButton button))
        {
            Button clone = Instantiate(taskbarButtonTemplate, taskbarButtons);
            clone.gameObject.name = "WindowButton";
            clone.gameObject.SetActive(true);
            clone.onClick.AddListener(() => _stack.TaskbarClick(id));
            button = new TaskbarButton { Button = clone, Label = clone.GetComponentInChildren<TMP_Text>(true) };
            _buttons.Add(id, button);
        }

        button.Button.transform.SetAsLastSibling();
        if (button.Label != null)
            button.Label.text = window.Title;
        ColorBlock colours = button.Button.colors;
        colours.normalColor = _stack.Focused == id ? focusedTint : _stack.IsMinimised(id) ? minimisedTint : Color.white;
        colours.selectedColor = colours.normalColor;
        button.Button.colors = colours;
    }

    /// <summary>Polls the press and Escape while the desktop takes input.</summary>
    private void Update()
    {
        if (raycaster == null || !raycaster.isActiveAndEnabled)
            return;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            Press(mouse.position.ReadValue());

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            Escape();
    }

    /// <summary>
    /// A press on the desktop at a screen point: outside the Start menu it
    /// closes the menu; in a window it focuses that window; on the empty
    /// desktop it leaves nothing focused.
    /// </summary>
    private void Press(Vector2 screen)
    {
        GameObject top = TopHit(screen);
        if (shell != null && shell.StartMenuOpen && !shell.IsStartMenuPart(top))
            shell.CloseStartMenu();
        if (top == null)
            return;

        foreach (KeyValuePair<string, DesktopWindow> pair in _windows)
            if (pair.Value != null && top.transform.IsChildOf(pair.Value.transform))
            {
                _stack.Focus(pair.Key);
                return;
            }

        if (emptyDesktop == null)
            return;
        foreach (Graphic g in emptyDesktop)
            if (g != null && g.gameObject == top)
            {
                _stack.ClearFocus();
                return;
            }
    }

    /// <summary>The top graphic under a screen point on the desktop canvas (through its event camera), or null.</summary>
    private GameObject TopHit(Vector2 screen)
    {
        _press.position = screen;
        _hits.Clear();
        raycaster.Raycast(_press, _hits);

        GameObject top = null;
        int depth = int.MinValue;
        foreach (RaycastResult hit in _hits)
            if (hit.gameObject != null && hit.depth > depth)
            {
                depth = hit.depth;
                top = hit.gameObject;
            }
        return top;
    }

    /// <summary>The desktop's part of the Escape chain; a press it takes is stamped with the frame.</summary>
    private void Escape()
    {
        TMP_InputField field = FocusedField();
        var state = new DesktopEscapeState(field != null, shell != null && shell.StartMenuOpen, _drag != null);
        switch (DesktopEscapeRule.Resolve(state))
        {
            case DesktopEscape.LeaveField:
                field.DeactivateInputField();
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);
                break;
            case DesktopEscape.CloseStartMenu:
                shell.CloseStartMenu();
                break;
            case DesktopEscape.CancelDrag:
                _drag.CancelDrag();
                break;
            default:
                return;
        }
        EscapeTakenFrame = Time.frameCount;
    }

    /// <summary>The desktop's text field that has the keyboard, or null.</summary>
    private TMP_InputField FocusedField()
    {
        GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        return selected != null && selected.transform.IsChildOf(transform) && selected.TryGetComponent(out TMP_InputField field) && field.isFocused
            ? field
            : null;
    }
}
