using System;

/// <summary>What a click on DETAIN's button does (DetainCover.OnButton).</summary>
public enum DetainClick
{
    /// <summary>The cover is up: the button is pressed (DETAINED while a traveller is at the desk).</summary>
    Press,

    /// <summary>The cover is shut over the button: it flips up first (the safety cover's first click).</summary>
    LiftCover,
}

/// <summary>
/// DETAIN's safety cover as the player sees it (Saleh's playtest of
/// 2026-10-28, point 2: "detain button case open and cant be closed its
/// collider is small too"). On the painted desk the cover is the painting's
/// orange guard, drawn open (raised 70 degrees on its hinge, the bake's
/// Desk_DetainGuard); it swings with DetainButton's cover spring between shut
/// (over the button) and the painting's open pose (Share). With the guard
/// shut over the button, the office camera sees the button's housing, not its
/// top: a click on the housing lifts the cover (OnButton), and the next one
/// presses, the safety cover's two deliberate clicks. Pure.
/// </summary>
public static class DetainCover
{
    /// <summary>How far open the cover stands (0 shut, 1 as far as its open angle; past 1 by its spring's overshoot) at hinge angle <paramref name="angle"/> of <paramref name="openAngle"/> (degrees). Never under 0: a spring's overshoot past shut never swings the guard into the button. 0 without an open angle.</summary>
    public static float Share(float angle, float openAngle) => openAngle > 0f ? Math.Max(0f, angle / openAngle) : 0f;

    /// <summary>What a click on the button does with the cover <paramref name="coverOpen"/>: a press while it is up, else it lifts the cover.</summary>
    public static DetainClick OnButton(bool coverOpen) => coverOpen ? DetainClick.Press : DetainClick.LiftCover;
}
