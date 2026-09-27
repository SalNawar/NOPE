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
/// desktop closes the context menu and the Start menu when it lands outside
/// them, focuses the window under the pointer (its own raycast through the
/// frame camera: a button inside a window takes the pointer-down itself),
/// and on the empty desktop (the wallpaper, the icons and their layer)
/// leaves nothing focused, then tells who listens what was pressed (Pressed:
/// the Investigation app's active pane). It knows the window or icon drag under way, which
/// the Escape chain (DesktopKeyboard, redesign phase 20) may cancel. It runs
/// before the EventSystem (whose script order is -1000) and the office's
/// pollers.
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

    /// <summary>The desktop's right-click menu (a press outside it closes it; Escape closes it first).</summary>
    [SerializeField] private DesktopContextMenu contextMenu;

    /// <summary>The empty desktop's graphics (the wallpaper and the icon layer): a press on one, or on anything in it (an icon), leaves no window focused.</summary>
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
    private IDesktopDrag _drag;

    /// <summary>Numbers the windows' ids.</summary>
    private int _registered;

    /// <summary>One taskbar button and its label.</summary>
    private struct TaskbarButton
    {
        public Button Button;
        public TMP_Text Label;
    }

    /// <summary>The focused window, or null (no window has the focus: the desktop's icons take the arrows and Enter).</summary>
    public DesktopWindow FocusedWindow => _stack.Focused != null && _windows.TryGetValue(_stack.Focused, out DesktopWindow w) ? w : null;

    /// <summary>Raised on each press on the desktop with the top graphic under the pointer (null: nothing), before the press's click: the Investigation app makes the pane pressed in its active one.</summary>
    public event System.Action<GameObject> Pressed;

    /// <summary>True while a title-bar or icon drag is under way (Escape cancels it).</summary>
    public bool Dragging => _drag != null;

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

    /// <summary>A window's title changed: its taskbar button reads the new one.</summary>
    public void Retitled(DesktopWindow window)
    {
        if (window != null && _ids.TryGetValue(window, out string id) && _buttons.TryGetValue(id, out TaskbarButton button) && button.Label != null)
            button.Label.text = window.Title;
    }

    /// <summary>True while the window is open, shown or minimised (a window never opened is not).</summary>
    public bool IsOpen(DesktopWindow window) => window != null && _ids.TryGetValue(window, out string id) && _stack.IsOpen(id);

    /// <summary>True while the window is open and minimised.</summary>
    public bool IsMinimised(DesktopWindow window) => window != null && _ids.TryGetValue(window, out string id) && _stack.IsMinimised(id);

    /// <summary>A title-bar or icon drag started (Escape may cancel it).</summary>
    public void BeginDrag(IDesktopDrag drag) => _drag = drag;

    /// <summary>A title-bar or icon drag ended.</summary>
    public void EndDrag(IDesktopDrag drag)
    {
        if (_drag == drag)
            _drag = null;
    }

    /// <summary>Cancels the drag under way: the window or icon goes back where it started (the Escape chain's CancelDrag).</summary>
    public void CancelDrag()
    {
        if (_drag != null)
            _drag.CancelDrag();
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

    /// <summary>Polls the press while the desktop takes input.</summary>
    private void Update()
    {
        if (raycaster == null || !raycaster.isActiveAndEnabled)
            return;

        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            Press(mouse.position.ReadValue());
    }

    /// <summary>
    /// A press on the desktop at a screen point: outside the context menu or
    /// the Start menu it closes that menu; in a window it focuses that window;
    /// on the empty desktop it leaves nothing focused.
    /// </summary>
    private void Press(Vector2 screen)
    {
        GameObject top = TopHit(screen);
        Pressed?.Invoke(top);
        if (contextMenu != null && contextMenu.IsOpen && !contextMenu.IsPart(top))
            contextMenu.Close();
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
            if (g != null && top.transform.IsChildOf(g.transform))
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
}
