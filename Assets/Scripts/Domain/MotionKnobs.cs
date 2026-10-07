using System;

/// <summary>
/// The game feel's tuning (MotionTuningSO holds one; Saleh edits it in the
/// Inspector, Generate World never touches it): the five named spring feels,
/// how far each control state moves (hover, press, release, click, a
/// disabled click's "no"), how panels, pills and popups come and go, and the
/// desk's physical layer (papers, stamps). Lengths on the UI are reference
/// pixels (the 1920×1080 canvas), on the desk metres. Every motion goes
/// through a Spring with one of the feels; the player's Motion intensity and
/// Reduced Motion scale it (MotionAmount).
/// </summary>
[Serializable]
public sealed class MotionKnobs
{
    /// <summary>Quick and nearly flat (damping ratio 0.8): a press going down, a stamp's slam.</summary>
    public SpringTuning firm = SpringTuning.WithRatio(700f, 0.8f);

    /// <summary>The default (ratio 0.55, about 12 % overshoot): hover, a pill sliding across.</summary>
    public SpringTuning balanced = SpringTuning.WithRatio(480f, 0.55f);

    /// <summary>Jelly (ratio 0.3, about a third overshoot): a release, a pop, a "no" shake.</summary>
    public SpringTuning elastic = SpringTuning.WithRatio(420f, 0.3f);

    /// <summary>Slow and soft (ratio 0.62): a paper lifting and settling, the newspaper dropping in.</summary>
    public SpringTuning paper = SpringTuning.WithRatio(160f, 0.62f);

    /// <summary>Weighty (ratio 0.85, heavier mass): a window, a panel, the camera's last bit of travel.</summary>
    public SpringTuning heavy = SpringTuning.WithRatio(380f, 0.85f, 1.6f);

    /// <summary>A control's scale under the pointer (the lift; the kit's hover face adds the glint).</summary>
    public float hoverScale = 1.03f;

    /// <summary>A control's scale while pressed: wider and shorter (the anticipation squash).</summary>
    public float pressScaleX = 1.06f, pressScaleY = 0.92f;

    /// <summary>How much a confirmed click pops the control (1.0 to 1 + this and back).</summary>
    public float popAmount = 0.08f;

    /// <summary>How far a click on a disabled control shakes it sideways (px): a short "no".</summary>
    public float refuseShake = 7f;

    /// <summary>The feel of the press going down, the release springing back, the hover lift, the click's pop and the "no" shake.</summary>
    public MotionFeel pressFeel = MotionFeel.Firm, releaseFeel = MotionFeel.Elastic, hoverFeel = MotionFeel.Balanced,
                      popFeel = MotionFeel.Balanced, refuseFeel = MotionFeel.Elastic;

    /// <summary>The scale a popup (a tooltip, a toast, a window, a wheel's pill) grows from as it opens; it shrinks back to it as it closes.</summary>
    public float appearFromScale = 0.6f;

    /// <summary>How far a sliding piece (a toast, a ribbon, the newspaper) comes in from (px).</summary>
    public float slideDistance = 80f;

    /// <summary>The seconds between one pill of the dialogue wheel and the next as they pop out (the stagger).</summary>
    public float staggerSeconds = 0.035f;

    /// <summary>The feel of a popup opening, a slide coming in and a sliding selection pill.</summary>
    public MotionFeel appearFeel = MotionFeel.Balanced, slideFeel = MotionFeel.Elastic, pillFeel = MotionFeel.Balanced;

    /// <summary>A selection pill's stretch along its travel per px/s of speed, and its most (0.3: 30 % longer, thinner by the area's rule).</summary>
    public float stretchPerSpeed = 0.00035f, maxStretch = 0.3f;

    /// <summary>The seconds a fade takes in place of a motion under Reduced Motion.</summary>
    public float reducedFadeSeconds = 0.12f;

    /// <summary>How high a dragged paper lifts off the desk.</summary>
    public float paperLift = 0.012f;

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

    /// <summary>How far the desk (the office camera) shakes on a slam.</summary>
    public float stampShake = 0.0035f;

    /// <summary>Seconds the ink mark takes to bloom in after a slam.</summary>
    public float inkBloomSeconds = 0.08f;

    /// <summary>The feel of a paper's lift and drop, a stamp's rebound and the shake.</summary>
    public MotionFeel paperFeel = MotionFeel.Paper, stampFeel = MotionFeel.Elastic, shakeFeel = MotionFeel.Elastic;

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

    /// <summary>A scale factor <paramref name="factor"/> scaled down toward 1 by the share (1.08 at half intensity is 1.04).</summary>
    public float Scale(float factor) => 1f + (factor - 1f) * Share;
}
