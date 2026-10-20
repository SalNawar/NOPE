using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// The game feel's tuning (Saleh 2026-10-07: "every single button, every
/// single action to feel this satisfying"): the five named spring feels
/// (Firm, Balanced, Elastic, Paper, Heavy), how far each control state, popup,
/// pill, paper and stamp moves, and the settle thresholds (MotionKnobs). One
/// asset (Assets/Data/Config/MotionTuning_Default.asset, made by the builders
/// when missing and assigned to RunConfig) that every motion reads through
/// UiMotion; an Inspector knob, never written by Generate World.
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/UI/Motion Tuning", fileName = "MotionTuning_Default", order = 22)]
public sealed class MotionTuningSO : ScriptableObject
{
    /// <summary>The motion's knobs (MotionKnobs; UI lengths in reference px, desk lengths in metres).</summary>
    public MotionKnobs knobs = new MotionKnobs();

    /// <summary>The desk cameras' idle breathing noise profile (Cinemachine's mild handheld one when the builders made the asset; none: no breathing).</summary>
    public NoiseSettings breathingNoise;

    /// <summary>The screen transitions' one length in seconds (Transit's kit: fades, slides, the fade through black of a scene change; BUG-32a, the bible's J5), kept within TransitCurve's 0.2-0.3 s.</summary>
    [Range(TransitCurve.MinSeconds, TransitCurve.MaxSeconds)] public float transitSeconds = 0.25f;

    /// <summary>The longest the shift report waits for the last traveller's reaction and walk-off (real seconds; BUG-32a: the payoff plays before the report, never stuck behind a figure that does not leave).</summary>
    [Min(0f)] public float payoffWaitSeconds = 8f;
}
