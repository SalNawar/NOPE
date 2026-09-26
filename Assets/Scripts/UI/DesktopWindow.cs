using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A desktop window's chrome (was OSWindowChrome; the PC redesign WN1, WN3).
/// Open, Minimise and Close go through the desktop's window manager (one
/// window stack: the only way a window shows or hides, audit R4-017), so a
/// minimised window keeps its taskbar button. Maximise fills the window
/// layer exactly, which is the icon area above the compare dock and the
/// taskbar, with no insets (R4-015);
/// a double-click on the title bar (WindowDrag) toggles it, and a maximised
/// window does not drag. The restored rect and the maximised state are
/// remembered while the window lives (the office session; never saved).
/// Attach to the window root and wire the title-bar buttons; WindowDrag sits
/// on the title bar.
/// </summary>
public sealed class DesktopWindow : MonoBehaviour
{
    /// <summary>The panel that is shown, hidden and resized (defaults to this object).</summary>
    [SerializeField] private RectTransform window;

    /// <summary>The title bar's minimise button: hides the window, keeping its taskbar button.</summary>
    [SerializeField] private Button minimizeButton;

    /// <summary>The title bar's maximise button: toggles between the restored rect and the icon area.</summary>
    [SerializeField] private Button maximizeButton;

    /// <summary>The title bar's close button.</summary>
    [SerializeField] private Button closeButton;

    /// <summary>The title bar's text (the taskbar button shows it).</summary>
    [SerializeField] private TMP_Text titleText;

    /// <summary>The desktop's window manager (the builder wires it; a window without one, a leftover in the art office, does nothing).</summary>
    [SerializeField] private DesktopWindowManager manager;

    private bool _maximised;
    private Vector2 _restoreMin;
    private Vector2 _restoreMax;
    private Vector2 _restoreSize;
    private Vector2 _restorePos;

    /// <summary>The title bar's text, or the object's name when it has none.</summary>
    public string Title => titleText != null ? titleText.text : name;

    /// <summary>True while maximised.</summary>
    public bool IsMaximised => _maximised;

    /// <summary>True while the window is open, shown or minimised (it has a taskbar button).</summary>
    public bool IsOpen => manager != null && manager.IsOpen(this);

    /// <summary>True while the window is open and minimised.</summary>
    public bool IsMinimised => manager != null && manager.IsMinimised(this);

    /// <summary>The desktop's window manager (null in a leftover without one).</summary>
    public DesktopWindowManager Manager => manager;

    private void Awake()
    {
        if (window == null)
            window = transform as RectTransform;

        OnClick(minimizeButton, Minimise);
        OnClick(maximizeButton, ToggleMaximise);
        OnClick(closeButton, Close);
    }

    /// <summary>Wires a title-bar button (a missing one is skipped).</summary>
    private static void OnClick(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    /// <summary>Shows the window (a minimised one restores), raised and focused.</summary>
    public void Open()
    {
        if (manager != null)
            manager.Open(this);
    }

    /// <summary>Closes the window (its taskbar button goes).</summary>
    public void Close()
    {
        if (manager != null)
            manager.Close(this);
    }

    /// <summary>Hides the window, keeping its taskbar button (a click on it restores the window).</summary>
    public void Minimise()
    {
        if (manager != null)
            manager.Minimise(this);
    }

    /// <summary>Toggles between the restored rect and the whole window layer (the icon area: the desktop above the dock and the taskbar), and focuses the window.</summary>
    public void ToggleMaximise()
    {
        if (window == null || manager == null)
            return;

        if (!_maximised)
        {
            _restoreMin = window.anchorMin;
            _restoreMax = window.anchorMax;
            _restoreSize = window.sizeDelta;
            _restorePos = window.anchoredPosition;

            window.anchorMin = Vector2.zero;
            window.anchorMax = Vector2.one;
            window.offsetMin = Vector2.zero;
            window.offsetMax = Vector2.zero;
            _maximised = true;
        }
        else
        {
            window.anchorMin = _restoreMin;
            window.anchorMax = _restoreMax;
            window.sizeDelta = _restoreSize;
            window.anchoredPosition = _restorePos;
            _maximised = false;
        }

        manager.Focus(this);
    }
}
