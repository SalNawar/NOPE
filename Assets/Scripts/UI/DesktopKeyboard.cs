using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The desktop's one keyboard poller (the PC redesign KB1-KB3, section 3.4,
/// 3.5; on the desktop canvas). While the desktop takes input (its raycaster
/// is on: the frame open, the screen on; BoothRules.DesktopInteractive) it
/// reads the Input System's keyboard, turns each key pressed this frame into
/// a chord and resolves it through the one table, ShortcutMap, in the
/// desktop's context (the focused window, a text field, an open menu, the
/// app's focus ring), then runs the command: Escape runs the one chain
/// (DesktopEscapeRule: a context menu, the shortcut card, the search field's
/// text, a focused field, the Start menu, a drag) and stamps the frame when
/// it takes the press, so the PC frame's Escape (OfficeViewController) skips
/// it; F1 (and the app's Keys button) shows or hides the shortcut card; the
/// icons take the arrows and Enter while no window has the focus; Ctrl+V
/// with Notes focused adds the clip as a clipping; everything else goes to
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
        (Key.F, ShortcutKey.F), (Key.Digit1, ShortcutKey.Digit1), (Key.Digit2, ShortcutKey.Digit2), (Key.Digit3, ShortcutKey.Digit3),
        (Key.Digit4, ShortcutKey.Digit4), (Key.Digit5, ShortcutKey.Digit5), (Key.Digit6, ShortcutKey.Digit6), (Key.Tab, ShortcutKey.Tab),
        (Key.PageUp, ShortcutKey.PageUp), (Key.PageDown, ShortcutKey.PageDown), (Key.F6, ShortcutKey.F6), (Key.Backslash, ShortcutKey.Backslash),
        (Key.B, ShortcutKey.B), (Key.S, ShortcutKey.S), (Key.LeftArrow, ShortcutKey.Left), (Key.RightArrow, ShortcutKey.Right),
        (Key.UpArrow, ShortcutKey.Up), (Key.DownArrow, ShortcutKey.Down), (Key.Home, ShortcutKey.Home), (Key.End, ShortcutKey.End),
        (Key.Enter, ShortcutKey.Enter), (Key.NumpadEnter, ShortcutKey.Enter), (Key.Space, ShortcutKey.Space), (Key.C, ShortcutKey.C),
        (Key.V, ShortcutKey.V), (Key.P, ShortcutKey.P), (Key.Equals, ShortcutKey.Equals), (Key.NumpadPlus, ShortcutKey.Equals),
        (Key.Minus, ShortcutKey.Minus), (Key.NumpadMinus, ShortcutKey.Minus), (Key.Digit0, ShortcutKey.Digit0), (Key.F1, ShortcutKey.F1),
        (Key.Escape, ShortcutKey.Escape),
    };

    /// <summary>The desktop canvas's raycaster (on only while the desktop takes input).</summary>
    [SerializeField] private GraphicRaycaster raycaster;

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

    /// <summary>The shortcut card (F1, the app's Keys button, Settings' Show shortcuts).</summary>
    [SerializeField] private DesktopWindow card;

    private DesktopWindow _notesWindow;

    /// <summary>A text field that saw the Escape the desktop took for something else: it gets its focus back after the EventSystem ran.</summary>
    private TMP_InputField _refocus;

    /// <summary>The frame in which the desktop last took an Escape press (-1: never); the PC frame's Escape skips that frame.</summary>
    public int EscapeTakenFrame { get; private set; } = -1;

    /// <summary>True while the shortcut card shows.</summary>
    public bool CardOpen => card != null && card.IsOpen && !card.IsMinimised;

    private void Awake()
    {
        if (notes != null)
            _notesWindow = notes.GetComponent<DesktopWindow>();
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

    /// <summary>Polls the keys (and a press, which hides the app's focus ring) while the desktop takes input.</summary>
    private void Update()
    {
        if (raycaster == null || !raycaster.isActiveAndEnabled)
            return;

        Mouse mouse = Mouse.current;
        if (mouse != null && app != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame))
            app.PointerPressed();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;
        bool ctrl = keyboard.ctrlKey.isPressed;
        bool shift = keyboard.shiftKey.isPressed;
        bool alt = keyboard.altKey.isPressed;
        foreach ((Key key, ShortcutKey shortcut) in Keys)
            if (keyboard[key].wasPressedThisFrame)
                Press(new KeyChord(shortcut, ctrl, shift, alt));
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
            tabStripFocused: appFocused && app.TabStripFocused);
        if (ShortcutMap.Resolve(chord, context, out AppCommand command))
            Run(command, field);
    }

    /// <summary>Runs a resolved command: the desktop's own ones here, the rest in the app.</summary>
    private void Run(AppCommand command, TMP_InputField field)
    {
        switch (command)
        {
            case AppCommand.Escape:
                Escape(field);
                break;
            case AppCommand.Help:
                ToggleCard();
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
            default:
                if (app != null)
                    app.Run(command);
                break;
        }
    }

    /// <summary>The desktop's part of the Escape chain; a press it takes is stamped with the frame.</summary>
    private void Escape(TMP_InputField field)
    {
        var state = new DesktopEscapeState(
            contextMenu != null && contextMenu.IsOpen,
            CardOpen,
            false, // The search results panel comes with search (redesign phase 19).
            app != null && app.IsSearchField(field),
            app != null && app.SearchHasText,
            field != null,
            shell != null && shell.StartMenuOpen,
            manager != null && manager.Dragging);
        DesktopEscape step = DesktopEscapeRule.Resolve(state);
        if (step == DesktopEscape.None)
            return;

        // The focused field sees this press too (TMP's cancel, after this poller): it must not undo the typing.
        if (field != null)
            field.restoreOriginalTextOnEscape = false;
        switch (step)
        {
            case DesktopEscape.CloseMenu:
                contextMenu.Close();
                _refocus = field;
                break;
            case DesktopEscape.CloseCard:
                card.Close();
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
