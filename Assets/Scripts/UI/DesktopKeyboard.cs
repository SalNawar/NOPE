using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// The desktop's one keyboard poller (the PC redesign KB1-KB3, section 3.4,
/// 3.5; on the desktop canvas). While the desktop takes input
/// (MonitorScreen.IsInteractive: the frame open, the screen on;
/// BoothRules.DesktopInteractive), from the press frame on, also while the
/// frame's picture still powers on and the desktop's raycaster is held off
/// (PcFrame.PictureMoving: clicks wait for the whole picture, the keys and
/// the back-out never do; Saleh's R3/R4, "Esc = Settings everywhere"), it
/// reads the Input System's keyboard, turns each key pressed this frame into
/// a chord and resolves it through the one table, ShortcutMap, in the
/// desktop's context (the focused window, a text field, an open menu, the
/// app's focus ring), then runs the command: Escape runs the one chain
/// (DesktopEscapeRule: a context menu or the shelf's Books menu, the shortcut card, the search results,
/// the search field's text, a focused field, the Start menu, a drag), and with
/// nothing left to close brings the Settings app to the front (a right-click
/// with nothing to close does nothing: PcBackOut; Saleh's R3/R4: Esc opens
/// Settings, only Q and the bezel leave the PC); it stamps every back-out
/// press, so the office's back-out (OfficeControls) never closes the PC.
/// With the PC open and its screen off (the desktop takes no input) it
/// still takes the back-out: Esc turns the screen on and brings Settings
/// forward, a right-click does nothing (PcBackOut.ScreenOff; R3/R4: "Esc
/// opens the same Settings window everywhere"). F1 (and the app's Keys button) shows or hides the shortcut card; the
/// icons take the arrows and Enter while no window has the focus; Ctrl+V
/// with Notes focused adds the clip as a clipping; with Orders focused the
/// arrows walk its tree, Enter acts on the selected node and Ctrl+=, Ctrl+-
/// and Ctrl+0 zoom the tree; everything else goes to
/// the Investigation app. A text field that sees the same Escape (TMP's own
/// cancel) keeps its text and, unless the press was for it, its focus. A
/// press of a mouse button hands the pointer back (the app's focus ring
/// hides). It runs after the window manager's press and before the
/// EventSystem (script order -1000) and the office's pollers; it allocates
/// nothing per frame.
/// </summary>
[DefaultExecutionOrder(-1090)]
public sealed class DesktopKeyboard : MonoBehaviour
{
    /// <summary>The keys the table uses, as the Input System names them (the numpad's Enter, plus and minus count as Enter, = and -).</summary>
    private static readonly (Key key, ShortcutKey shortcut)[] Keys =
    {
        (Key.F, ShortcutKey.F), (Key.K, ShortcutKey.K), (Key.Digit1, ShortcutKey.Digit1), (Key.Digit2, ShortcutKey.Digit2), (Key.Digit3, ShortcutKey.Digit3),
        (Key.Digit4, ShortcutKey.Digit4), (Key.Digit5, ShortcutKey.Digit5), (Key.Digit6, ShortcutKey.Digit6), (Key.Tab, ShortcutKey.Tab),
        (Key.PageUp, ShortcutKey.PageUp), (Key.PageDown, ShortcutKey.PageDown), (Key.F6, ShortcutKey.F6), (Key.Backslash, ShortcutKey.Backslash),
        (Key.B, ShortcutKey.B), (Key.S, ShortcutKey.S), (Key.LeftArrow, ShortcutKey.Left), (Key.RightArrow, ShortcutKey.Right),
        (Key.UpArrow, ShortcutKey.Up), (Key.DownArrow, ShortcutKey.Down), (Key.Home, ShortcutKey.Home), (Key.End, ShortcutKey.End),
        (Key.Enter, ShortcutKey.Enter), (Key.NumpadEnter, ShortcutKey.Enter), (Key.Space, ShortcutKey.Space), (Key.C, ShortcutKey.C),
        (Key.V, ShortcutKey.V), (Key.P, ShortcutKey.P), (Key.Equals, ShortcutKey.Equals), (Key.NumpadPlus, ShortcutKey.Equals),
        (Key.Minus, ShortcutKey.Minus), (Key.NumpadMinus, ShortcutKey.Minus), (Key.Digit0, ShortcutKey.Digit0), (Key.F1, ShortcutKey.F1),
        (Key.Escape, ShortcutKey.Escape), (Key.F10, ShortcutKey.F10), (Key.ContextMenu, ShortcutKey.Menu),
    };

    /// <summary>The window stack (the focused window, the drag Escape cancels).</summary>
    [SerializeField] private DesktopWindowManager manager;

    /// <summary>The desktop's icons (the arrows and Enter while no window has the focus).</summary>
    [SerializeField] private DesktopIcons icons;

    /// <summary>The Start menu (Escape closes it).</summary>
    [SerializeField] private DesktopShell shell;

    /// <summary>The desktop's right-click menu (Escape closes it first).</summary>
    [SerializeField] private DesktopContextMenu contextMenu;

    /// <summary>The Investigation app (its shortcuts, its search field, its focus ring).</summary>
    [SerializeField] private InvestigationApp app;

    /// <summary>The Notes app (Ctrl+V with it focused adds the clip as a clipping).</summary>
    [SerializeField] private NotesWindow notes;

    /// <summary>The Orders app (the arrows, Enter and the zoom chords with it focused).</summary>
    [SerializeField] private OrdersWindow orders;

    /// <summary>The shortcut card (F1, the app's Keys button, Settings' Show shortcuts).</summary>
    [SerializeField] private DesktopWindow card;

    /// <summary>The desktop's apps (Esc with nothing left to close opens Settings); on the same canvas, found there when not wired.</summary>
    [SerializeField] private DesktopApps apps;

    /// <summary>The PC frame (is the PC open while its screen is off?); found in the scene when not wired.</summary>
    [SerializeField] private PcFrame frame;

    /// <summary>The PC's screen (does the desktop take input? Esc turns it on when it is off); the desktop canvas's parent's, found there when not wired.</summary>
    [SerializeField] private MonitorScreen screen;

    private DesktopWindow _notesWindow;

    /// <summary>A text field that saw the Escape the desktop took for something else: it gets its focus back after the EventSystem ran.</summary>
    private TMP_InputField _refocus;

    /// <summary>False while a right-click runs the back-out (it never opens Settings); true for the Esc key.</summary>
    private bool _backOutFromKey = true;

    /// <summary>The frame in which the desktop last took an Escape press (-1: never); the PC frame's Escape skips that frame.</summary>
    public int EscapeTakenFrame { get; private set; } = -1;

    /// <summary>True while a text field on the desktop has the keyboard (the PC key, Q, is typed there: ControlRules).</summary>
    public bool TextFieldFocused => FocusedField() != null;

    /// <summary>True while the shortcut card shows.</summary>
    public bool CardOpen => card != null && card.IsOpen && !card.IsMinimised;

    private void Awake()
    {
        if (notes != null)
            _notesWindow = notes.GetComponent<DesktopWindow>();
        if (apps == null)
            apps = GetComponent<DesktopApps>(); // a scene built before the field: the apps share the canvas
        if (frame == null)
            frame = FindFirstObjectByType<PcFrame>(FindObjectsInactive.Include); // a scene built before the fields
        if (screen == null)
            screen = GetComponentInParent<MonitorScreen>();
    }

    /// <summary>F1 and the app's Keys button: shows the shortcut card, or hides it.</summary>
    public void ToggleCard()
    {
        if (card == null)
            return;
        if (CardOpen)
            card.Close();
        else
            card.Open();
    }

    /// <summary>Polls the keys (and a press, which hides the app's focus ring) while the desktop takes input, from the press frame (never gated on the raycaster, which the picture's power-on holds off: an Esc or a right-click then must still be the PC's, never the office's way out).</summary>
    private void Update()
    {
        if (screen == null || !screen.IsInteractive)
        {
            BackOutScreenOff();
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && app != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            app.PointerPressed();
        // A right-click backs out as Esc does (Papers, Please's controls: ControlRules): the same chain, always stamped; with nothing to close it does nothing.
        if (mouse != null && mouse.rightButton.wasPressedThisFrame)
        {
            _backOutFromKey = false;
            Press(new KeyChord(ShortcutKey.Escape));
            _backOutFromKey = true;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        if (keyboard.anyKey.wasPressedThisFrame)
            Sounds.Play(SoundCues.KeyTap); // a mechanical key under every key the PC takes
        bool ctrl = keyboard.ctrlKey.isPressed;
        bool shift = keyboard.shiftKey.isPressed;
        bool alt = keyboard.altKey.isPressed;
        foreach ((Key key, ShortcutKey shortcut) in Keys)
            if (keyboard[key].wasPressedThisFrame)
                Press(new KeyChord(shortcut, ctrl, shift, alt));
        // An Esc no chord took (held with a modifier) is still the PC's: Settings, never the way out (R3/R4).
        if (keyboard.escapeKey.wasPressedThisFrame && EscapeTakenFrame != Time.frameCount)
        {
            EscapeTakenFrame = Time.frameCount;
            if (apps != null)
                apps.OpenApp(DesktopAppIds.Settings);
        }
    }

    /// <summary>The PC open with its screen off: the back-out is still the PC's (stamped, so the office's never closes it); Esc turns the screen on and brings Settings forward, a right-click does nothing (PcBackOut.ScreenOff).</summary>
    private void BackOutScreenOff()
    {
        if (frame == null || !frame.IsOpen || screen == null || screen.IsOn || InputGate.Blocked)
            return;
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool esc = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        if (!esc && (mouse == null || !mouse.rightButton.wasPressedThisFrame))
            return;
        EscapeTakenFrame = Time.frameCount;
        if (PcBackOut.ScreenOff(esc) != PcBackOutStep.WakeAndOpenSettings)
            return;
        screen.PowerOn();
        if (apps != null)
            apps.OpenApp(DesktopAppIds.Settings);
    }

    /// <summary>A field that saw an Escape taken for something else gets its focus back (after TMP's own cancel left it).</summary>
    private void LateUpdate()
    {
        if (_refocus == null)
            return;
        _refocus.ActivateInputField();
        _refocus = null;
    }

    /// <summary>One chord (the desktop takes input): resolved in the desktop's context, then run.</summary>
    private void Press(KeyChord chord)
    {
        TMP_InputField field = FocusedField();
        DesktopWindow focused = manager != null ? manager.FocusedWindow : null;
        bool appFocused = app != null && focused != null && focused == app.Window;
        var context = new ShortcutContext(
            frameOpen: true,
            desktopFocused: focused == null,
            iconSelected: icons != null && icons.HasSelection,
            appFocused: appFocused,
            notesFocused: _notesWindow != null && focused == _notesWindow,
            textFieldFocused: field != null,
            searchFocused: app != null && app.IsSearchField(field),
            menuOpen: (contextMenu != null && contextMenu.IsOpen) || (shell != null && shell.StartMenuOpen),
            listFocused: appFocused && app.ListFocused,
            resultsShown: appFocused && app.ResultsListed,
            ordersFocused: orders != null && focused != null && focused == orders.Window);
        if (ShortcutMap.Resolve(chord, context, out AppCommand command))
            Run(command, field, context.OrdersFocused);
    }

    /// <summary>Runs a resolved command: the desktop's own ones here (the zoom chords in the Orders tree while <paramref name="ordersFocused"/>), the rest in the app.</summary>
    private void Run(AppCommand command, TMP_InputField field, bool ordersFocused)
    {
        switch (command)
        {
            case AppCommand.Escape:
                Escape(field);
                break;
            case AppCommand.Help:
                ToggleCard();
                break;
            case AppCommand.ContextMenu:
                ShowContextMenu();
                break;
            case AppCommand.OpenIcon:
                icons.OpenSelected();
                break;
            case AppCommand.IconLeft:
                icons.Step(-1, 0);
                break;
            case AppCommand.IconRight:
                icons.Step(1, 0);
                break;
            case AppCommand.IconUp:
                icons.Step(0, -1);
                break;
            case AppCommand.IconDown:
                icons.Step(0, 1);
                break;
            case AppCommand.Paste:
                notes.PasteClipboard();
                break;
            case AppCommand.NodeLeft:
                orders.Step(-1, 0);
                break;
            case AppCommand.NodeRight:
                orders.Step(1, 0);
                break;
            case AppCommand.NodeUp:
                orders.Step(0, -1);
                break;
            case AppCommand.NodeDown:
                orders.Step(0, 1);
                break;
            case AppCommand.NodeAct:
                orders.Act();
                break;
            case AppCommand.ZoomIn when ordersFocused:
                orders.Zoom(1);
                break;
            case AppCommand.ZoomOut when ordersFocused:
                orders.Zoom(-1);
                break;
            case AppCommand.ZoomReset when ordersFocused:
                orders.Zoom(0);
                break;
            default:
                if (app != null)
                    app.Run(command);
                break;
        }
    }

    /// <summary>Shift+F10 or the Menu key: the focused row's context menu with the app focused, else (no window focused) the selected icon's or the desktop's.</summary>
    private void ShowContextMenu()
    {
        DesktopWindow focused = manager != null ? manager.FocusedWindow : null;
        if (app != null && focused != null && focused == app.Window)
            app.ShowRowMenu();
        else if (focused == null && icons != null)
            icons.ShowMenu();
    }

    /// <summary>The desktop's part of the Escape chain (Esc, and a right-click: the one back-out); a press it takes is stamped with the frame.</summary>
    private void Escape(TMP_InputField field)
    {
        var state = new DesktopEscapeState(
            (contextMenu != null && contextMenu.IsOpen) || (app != null && app.MenuOpen),
            CardOpen,
            app != null && app.ResultsOpen,
            app != null && app.IsSearchField(field),
            app != null && app.SearchHasText,
            field != null,
            shell != null && shell.StartMenuOpen,
            manager != null && manager.Dragging,
            app != null && app.Holding);
        DesktopEscape step = DesktopEscapeRule.Resolve(state);
        EscapeTakenFrame = Time.frameCount; // every back-out on the PC is the desktop's: it never closes the PC (only Q and the bezel do)
        switch (PcBackOut.Resolve(step, _backOutFromKey))
        {
            case PcBackOutStep.OpenSettings:
                if (apps != null)
                    apps.OpenApp(DesktopAppIds.Settings);
                return;
            case PcBackOutStep.Swallow:
                return;
        }

        // The focused field sees this press too (TMP's cancel, after this poller): it must not undo the typing.
        if (field != null)
            field.restoreOriginalTextOnEscape = false;
        switch (step)
        {
            case DesktopEscape.CloseMenu:
                if (contextMenu != null && contextMenu.IsOpen)
                    contextMenu.Close();
                else
                    app.CloseMenu();
                _refocus = field;
                break;
            case DesktopEscape.CloseCard:
                card.Close();
                _refocus = field;
                break;
            case DesktopEscape.CloseResults:
                app.CloseResults();
                _refocus = field;
                break;
            case DesktopEscape.ClearSearch:
                app.ClearSearch();
                _refocus = field;
                break;
            case DesktopEscape.LeaveField:
                field.DeactivateInputField();
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(null);
                if (app != null)
                    app.FieldLeft(field);
                break;
            case DesktopEscape.CloseStartMenu:
                shell.CloseStartMenu();
                break;
            case DesktopEscape.CancelDrag:
                manager.CancelDrag();
                break;
            case DesktopEscape.ReleaseHold:
                app.ReleaseHold();
                break;
        }
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
