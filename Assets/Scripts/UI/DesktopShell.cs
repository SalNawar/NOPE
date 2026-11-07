using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Fake-OS desktop shell (the PC redesign DK8): the Start button toggles a
/// small menu listing the apps in the desktop's default order (the builder
/// wires each to DesktopApps.OpenApp, which closes the menu), then
/// "Arrange icons" (DesktopIcons.Arrange) and "Quit game". The menu lists
/// only today's apps and fits them: it grows up from the taskbar to its
/// entries' height (BUG-21a: no blank slab on days 1-4). Quit game is a
/// QuitControl (during a shift it first warns on the entry that the day is
/// forfeit, and quits on a second press within the config's time; BUG-35c);
/// closing the menu drops a standing warning. An entry closes the menu. The desktop's
/// window manager closes the menu on a press outside it and on Escape. Wire
/// from the editor builder. All fields are optional / null-safe.
/// </summary>
public sealed class DesktopShell : MonoBehaviour
{
    /// <summary>The name prefix of a Start menu entry that opens an app ("App_" + its id; Build Office UI names them so).</summary>
    private const string AppEntryPrefix = "App_";

    /// <summary>Taskbar Start button.</summary>
    [SerializeField] private Button startButton;

    /// <summary>The pop-up Start menu panel (hidden on start).</summary>
    [SerializeField] private GameObject startMenu;

    /// <summary>Start-menu "Arrange icons" entry.</summary>
    [SerializeField] private Button arrangeButton;

    /// <summary>The desktop's icons (Arrange icons).</summary>
    [SerializeField] private DesktopIcons icons;

    /// <summary>Start-menu "Quit game" entry; quits (during a shift, after its warning). (Formerly "Power", which quit too, so every scene's saved entry keeps quitting.)</summary>
    [FormerlySerializedAs("powerButton")]
    [SerializeField] private Button quitButton;

    /// <summary>The desktop's knobs (the quit warning's time; a scene built before the field: the knob's own default).</summary>
    [SerializeField] private DesktopConfigSO config;

    /// <summary>Retired (BUG-50a: "Turn off screen" was a sixth way out of the PC; the bezel's power button stays the screen's switch): the menu no longer has the entry, and one in a scene built before is hidden. Kept only while the root builder (QA-RIG's file) still wires it; INTEGRATION drops both.</summary>
    [SerializeField] private Button screenOffButton;

    /// <summary>Retired with <see cref="screenOffButton"/> (the monitor it darkened); INTEGRATION drops both.</summary>
    [SerializeField] private MonitorScreen monitorScreen;

    /// <summary>The quit warning's time (the config's), read once.</summary>
    private float _quitSeconds;

    /// <summary>The Quit game entry's control (made at Start).</summary>
    private QuitControl _quit;

    /// <summary>The menu's bottom edge (its built place, right above the taskbar), which the fitted menu keeps; NaN until first fitted.</summary>
    private float _menuBottom = float.NaN;

    /// <summary>True while the Start menu is open.</summary>
    public bool StartMenuOpen => startMenu != null && startMenu.activeSelf;

    private void Awake()
    {
        DesktopConfigSO knobs = config != null ? config : ScriptableObject.CreateInstance<DesktopConfigSO>();
        _quitSeconds = knobs.quitWarningSeconds;
        if (knobs != config)
            Destroy(knobs);
    }

    private void Start()
    {
        if (startMenu != null)
            startMenu.SetActive(false);
        if (screenOffButton != null)
            screenOffButton.gameObject.SetActive(false);

        if (startButton != null)
            startButton.onClick.AddListener(ToggleStartMenu);
        if (arrangeButton != null)
            arrangeButton.onClick.AddListener(ArrangeIcons);
        _quit = QuitControl.On(quitButton, _quitSeconds);
    }

    /// <summary>Shows/hides the Start menu; it lists only the apps whose icons show today (DesktopIcons.IsShown: the apps introduced), and fits them.</summary>
    public void ToggleStartMenu()
    {
        if (startMenu == null)
            return;
        bool opening = !startMenu.activeSelf;
        if (opening && icons != null)
            foreach (Transform entry in startMenu.transform)
                if (entry.name.StartsWith(AppEntryPrefix, System.StringComparison.Ordinal))
                    entry.gameObject.SetActive(icons.IsShown(entry.name.Substring(AppEntryPrefix.Length)));
        ResetQuit();
        startMenu.SetActive(opening);
        if (opening)
            FitMenu();
    }

    /// <summary>Closes the Start menu (a standing quit warning goes with it).</summary>
    public void CloseStartMenu()
    {
        if (startMenu != null)
            startMenu.SetActive(false);
        ResetQuit();
    }

    /// <summary>True for the Start menu, the Start button and their parts (a press there leaves the menu to them).</summary>
    public bool IsStartMenuPart(GameObject go) =>
        go != null && ((startMenu != null && go.transform.IsChildOf(startMenu.transform)) ||
                       (startButton != null && go.transform.IsChildOf(startButton.transform)));

    /// <summary>The menu as tall as its shown entries (its layout's preferred height), its bottom edge where it was built.</summary>
    private void FitMenu()
    {
        var rect = (RectTransform)startMenu.transform;
        if (float.IsNaN(_menuBottom))
            _menuBottom = rect.anchoredPosition.y - rect.sizeDelta.y * rect.pivot.y;
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        float height = LayoutUtility.GetPreferredHeight(rect);
        if (height <= 0f)
            return;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, _menuBottom + height * rect.pivot.y);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    /// <summary>Arranges the desktop's icons and closes the Start menu.</summary>
    private void ArrangeIcons()
    {
        CloseStartMenu();
        if (icons != null)
            icons.Arrange();
    }

    /// <summary>Drops a standing quit warning.</summary>
    private void ResetQuit()
    {
        if (_quit != null)
            _quit.Clear();
    }
}
