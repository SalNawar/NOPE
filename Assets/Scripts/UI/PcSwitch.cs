using UnityEngine;

/// <summary>
/// Marks a PC control as a switch (a Settings choice: Follow history /
/// Always English, Off / On, ...): a press on it plays its toggle instead of
/// the desktop's mouse click (PcPressCue; GAME_FEEL_BIBLE F3: "ui_toggle on
/// each switch", with F2's one press, one sound). A Toggle counts as a
/// switch without it. Added by Build Office UI's Settings window.
/// </summary>
[DisallowMultipleComponent]
public sealed class PcSwitch : MonoBehaviour
{
}
