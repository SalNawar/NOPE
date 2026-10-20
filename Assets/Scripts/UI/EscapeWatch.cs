using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PauseMenu's early half (FLOW, D20): on an Esc press it notes the office's
/// controls state before the PC's and the office's own pollers act on the
/// press (DesktopKeyboard at -1090, OfficeControls at -1080), so PauseMenu,
/// which runs after them, can tell whether they let go of something (a mode
/// left, a drag cancelled) and Settings waits for the next press. Added by
/// PauseMenu on its own host.
/// </summary>
[DefaultExecutionOrder(-1100)]
public sealed class EscapeWatch : MonoBehaviour
{
    /// <summary>The host's PauseMenu.</summary>
    private PauseMenu _menu;

    private void Awake() => _menu = GetComponent<PauseMenu>();

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (_menu != null && kb != null && kb.escapeKey.wasPressedThisFrame)
            _menu.WatchEscape();
    }
}
