using UnityEngine;

/// <summary>
/// The portal switchboard's knobs (LOOP_v5; Saleh's numbers where he gave
/// them, ours otherwise, see docs/pending/SWITCHBOARD.md): the rules'
/// timings and bonus, which modules each day opens, the day-1 hint card,
/// how the view eases in, how the cords hang and seat, MASTER's weight and
/// where the painted board sits on the office view. Loaded from
/// Resources/SwitchboardConfig (SwitchboardView); without the asset the
/// view runs on these defaults.
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/Switchboard Config", fileName = "SwitchboardConfig")]
public sealed class SwitchboardConfigSO : ScriptableObject
{
    /// <summary>Where the view loads the asset from (Resources).</summary>
    public const string ResourcePath = "SwitchboardConfig";

    [Header("Rules")]
    /// <summary>Real seconds a locked-in route calibrates while powered (Saleh: 20 s, real seconds).</summary>
    public float calibrationSeconds = 20f;

    /// <summary>Seconds a popped fuse keeps its module dark (Saleh: time only).</summary>
    public float fuseSeconds = 2f;

    /// <summary>Credits per extra traveller in one group sent (small).</summary>
    public int bonusPerExtraTraveller = 5;

    /// <summary>How many power cords the board has (Saleh: power is the cable you keep moving).</summary>
    public int powerCords = 2;

    /// <summary>A fresh call's blink rate (blinks a second).</summary>
    public float blinkHz = 1.2f;

    /// <summary>The fastest blink, after blinkRampSeconds of waiting.</summary>
    public float blinkHzMax = 4f;

    /// <summary>Seconds from the fresh blink to the fastest.</summary>
    public float blinkRampSeconds = 90f;

    [Header("Ramp")]
    /// <summary>The first day each module (01 first) is in service; 0 = never (04 and 05 wear their LOCKED and OUT OF SERVICE placards on the plate).</summary>
    public int[] moduleFromDay = { 1, 3, 5, 0, 0 };

    /// <summary>The last day the in-world hint card hangs on the board.</summary>
    public int hintUntilDay = 1;

    [Header("View")]
    /// <summary>Seconds the view takes to turn to the board and back (cut under Reduced Motion).</summary>
    public float easeSeconds = 0.45f;

    /// <summary>The painted board on the office view (the desk v3 painting at 16:9; viewport, 0 to 1 from the bottom left): the click area and the start of the turn.</summary>
    public Rect wallRect = new Rect(0.867f, 0.324f, 0.133f, 0.296f);

    /// <summary>Where the office view shows a waiting call: the painted board's top lamp (viewport at 16:9).</summary>
    public Vector2 wallLamp = new Vector2(0.889f, 0.574f);

    /// <summary>A call's soft buzz repeats every this many seconds while one waits (0: only when it comes in).</summary>
    public float reminderBuzzSeconds = 25f;

    /// <summary>The last seconds of a calibration that tick.</summary>
    public int tickLastSeconds = 3;

    [Header("Cords")]
    /// <summary>How much longer than the gap between its plugs a cord is (plate pixels): how far it hangs.</summary>
    public float cordSlack = 40f;

    /// <summary>How far a cord runs along its plugs' axis before it turns (a share of the gap between its plugs).</summary>
    public float cordReach = 0.3f;

    /// <summary>The longest that run gets (plate pixels).</summary>
    public float cordReachMax = 120f;

    /// <summary>The cord's rubber width (plate pixels).</summary>
    public float cordWidth = 8f;

    /// <summary>Points along each drawn cord.</summary>
    public int cordSegments = 28;

    /// <summary>A dropped plug seats in a socket within its hit radius times this.</summary>
    public float snapMagnet = 1.5f;

    /// <summary>A plug's size in a module's socket (the art: 0.85; full size on the destinations' and the power row's jacks).</summary>
    public float moduleScale = 0.85f;

    /// <summary>A held plug's size (lifted toward the eye).</summary>
    public float heldScale = 1.12f;

    [Header("MASTER")]
    /// <summary>How far down MASTER must be let go for the pull to count (the portals spec: before 80 % it does nothing).</summary>
    [Range(0.5f, 1f)] public float masterCommitAt = 0.8f;

    /// <summary>MASTER's spring stiffness (its weight under the hand).</summary>
    public float leverStiffness = 260f;

    /// <summary>MASTER's damping ratio (below 1 it swings back past rest).</summary>
    public float leverDamping = 0.55f;

    /// <summary>MASTER's mass (heavier lags the hand more).</summary>
    public float leverMass = 1.6f;

    /// <summary>The board's shake when MASTER clunks home (plate pixels; none under Reduced Motion).</summary>
    public float clunkShake = 4f;

    /// <summary>The board's shake when a fuse pops (plate pixels).</summary>
    public float fuseShake = 7f;

    /// <summary>The rules from the knobs.</summary>
    public SwitchboardRules Rules() => new SwitchboardRules
    {
        CalibrationSeconds = calibrationSeconds,
        FuseSeconds = fuseSeconds,
        BonusPerExtraTraveller = bonusPerExtraTraveller,
        PowerCords = powerCords,
        BlinkHz = blinkHz,
        BlinkHzMax = blinkHzMax,
        BlinkRampSeconds = blinkRampSeconds,
        ModuleFromDay = moduleFromDay != null ? (int[])moduleFromDay.Clone() : new int[0],
    };
}
