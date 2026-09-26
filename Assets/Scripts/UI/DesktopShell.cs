using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Fake-OS desktop shell: the Start button toggles a small menu offering
/// Settings (opens the Settings window: the UI language choice, piece 6), Turn off screen (darkens the live
/// monitor; only where one is wired) and Quit game. The desktop's window
/// manager closes the menu on a press outside it and on Escape. Wire from the
/// editor builder. All fields are optional / null-safe.
/// </summary>
public sealed class DesktopShell : MonoBehaviour
{
    /// <summary>Taskbar Start button.</summary>
    [SerializeField] private Button startButton;

    /// <summary>The pop-up Start menu panel (hidden on start).</summary>
    [SerializeField] private GameObject startMenu;

    /// <summary>Start-menu "Settings" entry.</summary>
    [SerializeField] private Button settingsButton;

    /// <summary>Start-menu "Quit game" entry; quits. (Formerly "Power", which quit too, so every scene's saved entry keeps quitting.)</summary>
    [FormerlySerializedAs("powerButton")]
    [SerializeField] private Button quitButton;

    /// <summary>Start-menu "Turn off screen" entry; optional, with the monitor screen.</summary>
    [SerializeField] private Button screenOffButton;

    /// <summary>The live monitor the "Turn off screen" entry darkens (wired with the entry).</summary>
    [SerializeField] private MonitorScreen monitorScreen;

    /// <summary>Settings window opened by the Settings entry (the UI language choice, SettingsWindowController).</summary>
    [SerializeField] private DesktopWindow settingsWindow;

    /// <summary>True while the Start menu is open.</summary>
    public bool StartMenuOpen => startMenu != null && startMenu.activeSelf;

    private void Start()
    {
        if (startMenu != null)
            startMenu.SetActive(false);

        if (startButton != null)
            startButton.onClick.AddListener(ToggleStartMenu);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null)
            quitButton.onClick.AddListener(Quit);
        if (screenOffButton != null)
            screenOffButton.onClick.AddListener(TurnOffScreen);
    }

    /// <summary>Shows/hides the Start menu.</summary>
    public void ToggleStartMenu()
    {
        if (startMenu != null)
            startMenu.SetActive(!startMenu.activeSelf);
    }

    /// <summary>Closes the Start menu.</summary>
    public void CloseStartMenu()
    {
        if (startMenu != null)
            startMenu.SetActive(false);
    }

    /// <summary>True for the Start menu, the Start button and their parts (a press there leaves the menu to them).</summary>
    public bool IsStartMenuPart(GameObject go) =>
        go != null && ((startMenu != null && go.transform.IsChildOf(startMenu.transform)) ||
                       (startButton != null && go.transform.IsChildOf(startButton.transform)));

    /// <summary>Opens the Settings window and closes the Start menu.</summary>
    public void OpenSettings()
    {
        if (settingsWindow != null)
            settingsWindow.Open();
        CloseStartMenu();
    }

    /// <summary>Turns the monitor's screen off (a screen held on by a pending citation slip stays on) and closes the Start menu.</summary>
    public void TurnOffScreen()
    {
        if (monitorScreen != null)
            monitorScreen.TurnOff();
        CloseStartMenu();
    }

    /// <summary>Quits the game (exits play mode in the editor).</summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
