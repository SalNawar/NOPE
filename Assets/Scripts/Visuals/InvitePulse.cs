using System;

/// <summary>
/// The AVAILABLE sign's invitation (HALL-FX, BUG-67: it "pulses forever"; the
/// fix: "pulse only when it wants a click, then settle after 2-3 cycles"): from
/// the moment the sign starts wanting its press, its ink dips from lit toward
/// the paused ink and back, <see cref="Cycles"/> times, one beat each, then
/// holds lit and steady until it wants a press again. The pure part of
/// AvailableSignLink (the beat is DeskConfigSO.readyInviteSeconds; the cycle
/// count is a constant here because DeskConfigSO is DOC-HANDLE's: a knob for it
/// is on HALL-FX's request list). No UnityEngine, so it is tested headless
/// (InvitePulseTests).
/// </summary>
public static class InvitePulse
{
    /// <summary>How many beats the invitation pulses before it settles (FIX_PLAN: 2-3; proposal, ours: 3).</summary>
    public const int Cycles = 3;

    /// <summary>
    /// The sign's light (1 lit, 0 the paused ink) <paramref name="elapsed"/>
    /// seconds after it began inviting, at <paramref name="beatSeconds"/> a
    /// beat: a cosine dip and rise each beat for <see cref="Cycles"/> beats
    /// (starting and ending lit, so it never jumps), then 1; 1 at once with
    /// <paramref name="reduced"/> motion.
    /// </summary>
    public static float Level(float elapsed, float beatSeconds, bool reduced)
    {
        if (reduced || !(elapsed > 0f) || !(beatSeconds > 0f))
            return 1f;
        float beats = elapsed / beatSeconds;
        if (beats >= Cycles)
            return 1f;
        return 0.5f + 0.5f * (float)Math.Cos(2.0 * Math.PI * beats);
    }

    /// <summary>True once the invitation has settled (it needs no more frames).</summary>
    public static bool Settled(float elapsed, float beatSeconds, bool reduced) =>
        reduced || !(beatSeconds > 0f) || elapsed >= beatSeconds * Cycles;
}
