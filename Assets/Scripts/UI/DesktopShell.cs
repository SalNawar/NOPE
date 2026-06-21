using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fake-OS desktop shell: the Start button toggles a small menu offering
/// Settings (opens an empty stub window) and Power (quits the game). Wire from
/// the editor builder. All fields are optional / null-safe.
/// </summary>
public sealed class DesktopShell : MonoBehaviour
{
    /// <summary>Taskbar Start button.</summary>
    [SerializeField] private Button startButton;

    /// <summary>The pop-up Start menu panel (hidden on start).</summary>
    [SerializeField] private GameObject startMenu;

    /// <summary>Start-menu "Settings" entry.</summary>
    [SerializeField] private Button settingsButton;

    /// <summary>Start-menu "Power" entry (quits).</summary>
    [SerializeField] private Button powerButton;

    /// <summary>Settings window opened by the Settings entry (empty stub).</summary>
    [SerializeField] private OSWindowChrome settingsWindow;

    private void Start()
    {
        if (startMenu != null)
            startMenu.SetActive(false);

        if (startButton != null)
            startButton.onClick.AddListener(ToggleStartMenu);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);
        if (powerButton != null)
            powerButton.onClick.AddListener(Quit);
    }

    /// <summary>Shows/hides the Start menu.</summary>
    public void ToggleStartMenu()
    {
        if (startMenu != null)
            startMenu.SetActive(!startMenu.activeSelf);
    }

    /// <summary>Opens the Settings window and closes the Start menu.</summary>
    public void OpenSettings()
    {
        if (settingsWindow != null)
            settingsWindow.Open();
        if (startMenu != null)
            startMenu.SetActive(false);
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
