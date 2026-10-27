/// <summary>
/// The one sound of a press on the PC (BUG-21c: "one press, one click
/// sound"; GAME_FEEL_BIBLE F2 "one click = one sound" and F3 "ui_toggle on
/// each switch"): a press on a switch (a Settings choice) plays its toggle,
/// any other press the desktop's mouse click; never both, and the control's
/// own juice adds no press sound on the PC (UiJuice). A refused control's
/// error cue is the refusal's, not the press's. Pure; DesktopWindowManager
/// plays it.
/// </summary>
public static class PcPressCue
{
    /// <summary>The cue of a press on the PC, on a switch (<paramref name="onSwitch"/>) or not.</summary>
    public static string For(bool onSwitch) => onSwitch ? SoundCues.UiToggle : SoundCues.MouseClick;
}
