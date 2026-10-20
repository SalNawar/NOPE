using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PauseMenu's early half (FLOW, D20; Saleh's R3: "Esc opens the same
/// Settings window everywhere ... To leave the PC, press Q or click the
/// bezel"). It runs between the PC's keyboard poller (DesktopKeyboard, -1090,
/// which takes an Esc for its own chain first: a menu, the card, a field) and
/// the office's (OfficeControls, -1080, whose back-out would close the PC on
/// Esc). On an Esc press it notes the office's controls state before the
/// office acts, so PauseMenu, which runs after them, can tell whether the
/// office let go of something (a drag cancelled) and Settings
/// waits for the next press; and when the press would leave a mode
/// (EscapeChain.KeepsTheOfficeOut: the PC, the wheel, inspect mode, the city,
/// the reading view; a drag is still cancelled), it keeps the office's poller
/// out of this one frame, so nothing closes and PauseMenu opens Settings (in
/// the PC, in front).
/// Added by PauseMenu on its own host.
/// </summary>
[DefaultExecutionOrder(-1085)]
public sealed class EscapeWatch : MonoBehaviour
{
    /// <summary>The host's PauseMenu.</summary>
    private PauseMenu _menu;

    private void Awake() => _menu = GetComponent<PauseMenu>();

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (_menu != null && kb != null && kb.escapeKey.wasPressedThisFrame)
            _menu.WatchEscape(Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
    }
}
