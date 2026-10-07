using System;

/// <summary>
/// The game feel's tuning (MotionTuningSO holds one; Saleh edits it in the
/// Inspector, Generate World never touches it): the five named spring feels,
/// how far each control state moves (hover, press, release, click, a
/// disabled click's "no"), how panels, pills and popups come and go, and the
/// desk's physical layer (papers, stamps). Lengths on the UI are reference
/// pixels (the 1920×1080 canvas), on the desk metres (a dragged paper's
/// lift is the desk's own knob, DeskConfigSO.dragLift). Every motion goes
/// through a Spring with one of the feels; the player's Motion intensity and
/// Reduced Motion scale it (MotionAmount).
/// </summary>
[Serializable]
public sealed class MotionKnobs
{
    /// <summary>Heavy desk objects (damping ratio 0.8, nearly flat): a stamp's rebound, the desk's timed moves.</summary>
    public SpringTuning firm = SpringTuning.WithRatio(700f, 0.8f);

    /// <summary>A kit button going down (Saleh 2026-10-07, round 2: "too fast"; ratio 0.6, its peak at about 90 ms) and the hover's lift.</summary>
    public SpringTuning balanced = SpringTuning.WithRatio(1900f, 0.6f);

    /// <summary>Jelly (ratio 0.28): a release settling over about 400 ms in two or three visible, decaying wobbles (the reel's Elastic toggle), a pop, a "no" shake, everything that travels.</summary>
    public SpringTuning elastic = SpringTuning.WithRatio(645f, 0.28f);

    /// <summary>Slow and soft (ratio 0.62): a paper lifting and settling, the newspaper dropping in.</summary>
    public SpringTuning paper = SpringTuning.WithRatio(160f, 0.62f);

    /// <summary>Weighty (ratio 0.85, heavier mass): a window, a panel, the camera's last bit of travel.</summary>
    public SpringTuning heavy = SpringTuning.WithRatio(380f, 0.85f, 1.6f);

    /// <summary>How far a control's face lifts under the pointer (px; Saleh 2026-10-07, round 2: a 1-2 px lift and the glint, no growth).</summary>
    public float hoverLift = 1.5f;

    /// <summary>How far a press pushes the face down into its bezel (px): its top comes down, its bottom stays.</summary>
    public float pressDepth = 2.5f;

    /// <summary>How much a pressed face darkens (0 to 1).</summary>
    public float pressDarken = 0.14f;

    /// <summary>How far a confirmed click pops the face's top up before it wobbles back (px).</summary>
    public float popLift = 2f;

    /// <summary>How far a click on a disabled control shakes its face sideways (px): a short "no", inside its room.</summary>
    public float refuseShake = 3f;

    /// <summary>The most a face may grow past its rest rect on any side (px: the kit's border inset), and never into a neighbour's rest rect (ControlRoom).</summary>
    public float faceRoom = 3f;

    /// <summary>How far a screen-edge pull tab slides out under the pointer (px), and pops out on a click.</summary>
    public float pullHover = 12f, pullPop = 8f;

    /// <summary>A pull tab's stretch along its slide per px/s and its squash per px/s² (its slide is short, so it stretches harder than a long traveller), at most maxStretch.</summary>
    public float pullStretchPerSpeed = 0.0006f, pullSquashPerAccel = 0.000006f;

    /// <summary>The feel of the press going down, the release springing back, the hover lift, the click's pop and the "no" shake (one tuning for every kit button).</summary>
    public MotionFeel pressFeel = MotionFeel.Balanced, releaseFeel = MotionFeel.Elastic, hoverFeel = MotionFeel.Balanced,
                      popFeel = MotionFeel.Elastic, refuseFeel = MotionFeel.Elastic;

    /// <summary>The scale a popup (a tooltip, a toast, a window, a wheel's pill) grows from as it opens; it shrinks back to it as it closes.</summary>
    public float appearFromScale = 0.6f;

    /// <summary>The most a popup or a panel grows past its rest size on either axis as it overshoots or stretches (px): a big window grows by little, a pill by up to its share.</summary>
    public float appearGrowMax = 24f;

    /// <summary>How far a sliding piece (a toast, a ribbon, the newspaper) comes in from (px).</summary>
    public float slideDistance = 80f;

    /// <summary>The seconds between one pill of the dialogue wheel and the next as they pop out (the stagger).</summary>
    public float staggerSeconds = 0.035f;

    /// <summary>How many lines a second a printed slip (the citation) shows as it prints.</summary>
    public float printLinesPerSecond = 10f;

    /// <summary>The feel of a popup opening, a slide coming in and a sliding selection pill (Elastic: it overshoots and wobbles into place).</summary>
    public MotionFeel appearFeel = MotionFeel.Elastic, slideFeel = MotionFeel.Elastic, pillFeel = MotionFeel.Elastic;

    /// <summary>
    /// What travels stretches along its travel by its speed and squashes by its
    /// acceleration (the launch's anticipation, the arrival, each turn of the
    /// wobble), keeping its area (SquashStretch.FromMotion): per px/s, per px/s²,
    /// and the most either way (0.25: 25 % longer or shorter).
    /// </summary>
    public float stretchPerSpeed = 0.00012f, squashPerAccel = 0.0000012f, maxStretch = 0.25f;

    /// <summary>The desk's travelling objects' stretch (the stamp bar sliding out and back): per metre per second and their squash per metre per second², at most maxStretch.</summary>
    public float deskStretchPerSpeed = 0.06f, deskSquashPerAccel = 0.0005f;

    /// <summary>The seconds a fade takes in place of a motion under Reduced Motion.</summary>
    public float reducedFadeSeconds = 0.12f;

    /// <summary>A dragged paper's most tilt toward its travel (degrees) and the tilt per metre per second of speed.</summary>
    public float paperTiltMax = 7f, paperTiltPerSpeed = 14f;

    /// <summary>How much a dropped paper squashes as it lands (0.06: 6 % flatter).</summary>
    public float paperDropSquash = 0.06f;

    /// <summary>How far a stamp rises before its slam (the anticipation).</summary>
    public float stampLift = 0.025f;

    /// <summary>Seconds of the anticipation rise, then of the slam down.</summary>
    public float stampRiseSeconds = 0.07f, stampSlamSeconds = 0.05f;

    /// <summary>How much the stamp squashes on impact.</summary>
    public float stampSquash = 0.16f;

    /// <summary>Seconds the ink mark takes to bloom in after a slam.</summary>
    public float inkBloomSeconds = 0.08f;

    /// <summary>The feel of a paper's lift (DeskConfigSO.dragLift high), tilt and drop, and a stamp's rebound.</summary>
    public MotionFeel paperFeel = MotionFeel.Paper, stampFeel = MotionFeel.Firm;

    /// <summary>The curve of the desk's timed moves (a paper's slide and change of size, a stamp's way back to the rack, the stamp bar): their seconds stay the desk's knobs, their shape this feel's (SpringCurve).</summary>
    public MotionFeel deskMoveFeel = MotionFeel.Firm;

    /// <summary>The curve of the desk camera's blend (the reading view, the PC zoom): a heavy settle with a slight overshoot.</summary>
    public MotionFeel cameraFeel = MotionFeel.Heavy;

    /// <summary>The hit-stop (FeelDirector: a stamp landing, a citation issued, a famous traveller let through): the time scale gameplay drops to, and for how many real seconds (the UI and the springs run on unscaled time).</summary>
    public float hitStopScale = 0.05f, hitStopSeconds = 0.075f;

    /// <summary>The desk cameras' idle breathing (Cinemachine noise: MotionTuningSO.breathingNoise): its amplitude and frequency gains at full Motion intensity.</summary>
    public float breathingAmplitude = 0.35f, breathingFrequency = 0.25f;

    /// <summary>How many degrees the desk camera's field of view narrows while a paper is read (the soft push-in), on the Heavy feel.</summary>
    public float readingPushIn = 2.5f;

    /// <summary>The camera impulses' strength (Cinemachine impulse force, scaled by the Motion intensity): a stamp's slam, a citation landing, the Helix River breaching.</summary>
    public float shakeStamp = 0.04f, shakeCitation = 0.1f, shakeBreach = 0.28f;

    /// <summary>The camera impulses' seconds: a slam's bump, a citation's, a breach's rumble.</summary>
    public float shakeStampSeconds = 0.18f, shakeCitationSeconds = 0.3f, shakeBreachSeconds = 0.9f;

    /// <summary>A spring settles (stops and leaves the motion driver) within this of its target and slower than settleSpeed.</summary>
    public float settleValue = 0.0005f, settleSpeed = 0.005f;

    /// <summary>The tuning of <paramref name="feel"/>.</summary>
    public SpringTuning Get(MotionFeel feel)
    {
        switch (feel)
        {
            case MotionFeel.Firm:
                return firm;
            case MotionFeel.Elastic:
                return elastic;
            case MotionFeel.Paper:
                return paper;
            case MotionFeel.Heavy:
                return heavy;
            default:
                return balanced;
        }
    }
}

/// <summary>
/// How much of each motion plays: the player's Motion intensity (0 to 1) and
/// Reduced Motion (no squash, no shake, no overshoot: a cut or a short fade).
/// </summary>
public readonly struct MotionAmount
{
    /// <summary>The player's Motion intensity, 0 to 1.</summary>
    public readonly float Intensity;

    /// <summary>True when the player chose Reduced Motion.</summary>
    public readonly bool Reduced;

    /// <summary>An amount from the intensity (clamped to 0..1) and the Reduced Motion choice.</summary>
    public MotionAmount(float intensity, bool reduced)
    {
        Intensity = intensity < 0f ? 0f : intensity > 1f ? 1f : intensity;
        Reduced = reduced;
    }

    /// <summary>Full motion.</summary>
    public static MotionAmount Full => new MotionAmount(1f, false);

    /// <summary>The share of an amplitude that plays (0 under Reduced Motion).</summary>
    public float Share => Reduced ? 0f : Intensity;

    /// <summary>True when nothing moves (Reduced Motion, or intensity 0): motions cut.</summary>
    public bool Still => Share <= 0f;
}
