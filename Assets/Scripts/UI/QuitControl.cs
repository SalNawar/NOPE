using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A Quit game control (BUG-35c; QuitGuard): outside a shift a press quits;
/// during one the first press shows the control's keyed warning label
/// ("Today's shift is forfeit") in place of its label and a second press
/// within the warning's time quits; the warning lapses on its own. Each label
/// keeps its own key, so a theme refresh never writes one over the other.
/// The Start menu's Quit game (DesktopShell) and the Settings window's
/// (SettingsWindowController; UI-16: the PC's Settings carries Quit) each put
/// one on their button at run time (<see cref="On"/>).
/// </summary>
public sealed class QuitControl : MonoBehaviour
{
    /// <summary>The warning label's name under the button (built keyed startmenu.quitForfeit, hidden).</summary>
    public const string WarningName = "Warning";

    /// <summary>The press rule.</summary>
    private QuitGuard _guard;

    /// <summary>The button's label ("Quit game") and its warning label, one shown at a time.</summary>
    private GameObject _label, _warning;

    /// <summary>True while the control reads its warning.</summary>
    private bool _warned;

    /// <summary>The control on <paramref name="button"/> (added on first use) with a warning held <paramref name="seconds"/> seconds; null for no button.</summary>
    public static QuitControl On(Button button, float seconds)
    {
        if (button == null)
            return null;
        if (button.TryGetComponent(out QuitControl control))
            return control;
        control = button.gameObject.AddComponent<QuitControl>();
        control._guard = new QuitGuard(seconds);
        Transform label = button.transform.Find("Label"), warning = button.transform.Find(WarningName);
        control._label = label != null ? label.gameObject : null;
        control._warning = warning != null ? warning.gameObject : null;
        if (control._warning != null)
            control._warning.SetActive(false);
        button.onClick.AddListener(control.Press);
        return control;
    }

    /// <summary>A warning lapses on its own: the control reads Quit game again.</summary>
    private void Update()
    {
        if (_warned && !_guard.Warning(Time.unscaledTime))
            ShowWarning(false);
    }

    /// <summary>Drops a standing warning (the menu that holds the control closed).</summary>
    public void Clear()
    {
        _guard?.Reset();
        if (_warned)
            ShowWarning(false);
    }

    /// <summary>Outside a shift it quits; during one the first press warns that the day is forfeit and a second press quits (QuitGuard).</summary>
    private void Press()
    {
        if (_guard.Press(ShiftRunning(), Time.unscaledTime) == QuitStep.Quit)
        {
            Quit();
            return;
        }
        ShowWarning(true);
        Sounds.Play(SoundCues.UiError);
    }

    /// <summary>The control shows its warning label, or its Quit game label again (without a built warning the label stays).</summary>
    private void ShowWarning(bool warn)
    {
        _warned = warn;
        if (_warning == null)
            return;
        _warning.SetActive(warn);
        if (_label != null)
            _label.SetActive(!warn);
    }

    /// <summary>True while today's shift runs (started and not closed; a pause counts): quitting now forfeits the day.</summary>
    private static bool ShiftRunning()
    {
        ShiftClockDriver driver = FindFirstObjectByType<ShiftClockDriver>();
        ShiftClock clock = driver != null ? driver.Clock : null;
        return clock != null && clock.IsStarted && !clock.IsClosed;
    }

    /// <summary>Quits the game (exits play mode in the editor).</summary>
    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
