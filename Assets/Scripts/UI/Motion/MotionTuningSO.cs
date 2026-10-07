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
}
