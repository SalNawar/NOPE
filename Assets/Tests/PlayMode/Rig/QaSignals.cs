using System;
using UnityEngine;

/// <summary>
/// Why the camera moved (GAME_FEEL_BIBLE G9; FIX_PLAN_v2 P0-B delta). The QA
/// rig's camera logger (CameraMoveLog) attributes every camera move to the
/// causes tagged while it ran; a move with no cause fails the gate.
/// Append-only: the numbers are written into QA logs.
/// </summary>
public enum CameraMoveCause
{
    /// <summary>No cause tagged: a gate failure.</summary>
    Untagged = 0,
    /// <summary>The player pressed a view key (the desk, the city, the PC, the dispatch wall).</summary>
    ViewKey = 1,
    /// <summary>D1: a document dropped on the desk tilts the camera into the reading pose.</summary>
    DeskDropTilt = 2,
    /// <summary>D6 (Phase B): the subtle framing push-in when a machine slides in.</summary>
    SlideInFocus = 3,
    /// <summary>An impulse shake from the shake table (GAME_FEEL_BIBLE §1.3), capped in pixels at 1080p.</summary>
    Impulse = 4,
}

/// <summary>
/// The game's side of the QA rig (P0-B QA-RIG seam): the calls game code
/// makes so the rig can measure it. Always compiled (the rig itself is only in
/// the editor and in QA player builds), and free when nothing listens: a call
/// only raises a static event.
/// - <see cref="TagCamera"/>: CAM, DOC-HANDLE and the Phase B framing tag
///   each camera move with its cause and length, so the logger passes it.
/// - <see cref="Event"/>: a named moment (an audio cue played, a flash shown,
///   a hit-stop started) that the recorder writes on the frame it happened,
///   for the press-to-cue latency check and the audio and flash logs.
/// </summary>
public static class QaSignals
{
    /// <summary>Raised by <see cref="TagCamera"/>: the cause, its length in unscaled seconds and its pixel cap at 1080p (0: no cap).</summary>
    public static event Action<CameraMoveCause, float, float> CameraTagged;

    /// <summary>Raised by <see cref="Event"/>: the channel ("audio", "flash", "hitstop", ...) and the name.</summary>
    public static event Action<string, string> Raised;

    /// <summary>
    /// Says the camera moves now for <paramref name="cause"/>, for the next
    /// <paramref name="seconds"/> of unscaled time (a blend's length, a
    /// shake's length). <paramref name="capPx"/> is the most the move may
    /// shift the picture, in pixels at 1080p, measured from the pose at the
    /// tag (the shake table's cap; 0 means no cap). Call it when the move
    /// starts; calling it again extends or overlaps.
    /// </summary>
    public static void TagCamera(CameraMoveCause cause, float seconds, float capPx = 0f)
    {
        CameraTagged?.Invoke(cause, Mathf.Max(0f, seconds), Mathf.Max(0f, capPx));
    }

    /// <summary>Marks a named moment on this frame (for example <c>Event("audio", "stamp_land")</c> where the cue plays).</summary>
    public static void Event(string channel, string name)
    {
        Raised?.Invoke(channel ?? string.Empty, name ?? string.Empty);
    }
}
