using UnityEngine;

/// <summary>
/// Tuning for the physical desk (piece 7): the monitor push-in, screen power,
/// the scanner, the papers, the sorting bands, the traveller wheel and its
/// reply bubble, and the day-1 desk notes. Geometry that belongs to the art
/// (the glass rectangle, the desk rectangle, the scanner's drop area, the
/// anchors) stays in scene components, so another office supplies its own.
/// Created and assigned by Tools > TimeDesk > Build Office UI
/// (Assets/Data/Config/Desk_Default.asset).
/// </summary>
[CreateAssetMenu(fileName = "Desk_Default", menuName = "TimeDesk/Office/Desk Config")]
public sealed class DeskConfigSO : ScriptableObject
{
    [Header("Monitor focus")]
    /// <summary>How much of the view's height (or width, on narrow screens) the CRT's glass fills when focused.</summary>
    [Range(0.5f, 1f)] public float monitorFill = 0.85f;

    /// <summary>Seconds the camera takes to push in or pull back (the brain's default blend).</summary>
    [Min(0f)] public float focusBlendSeconds = 0.6f;

    [Header("Screen power")]
    /// <summary>True when the screen is on at the start of a shift.</summary>
    public bool screenStartsOn = true;

    /// <summary>Which events turn a dark screen on.</summary>
    public PcWakeRules wake = new PcWakeRules();

    /// <summary>The bezel LED's colour while the screen is on.</summary>
    public Color ledOnColor = new Color(0.35f, 0.95f, 0.45f, 1f);

    /// <summary>The bezel LED's colour while the screen is off.</summary>
    public Color ledOffColor = new Color(0.15f, 0.17f, 0.15f, 1f);

    [Header("Scanner")]
    /// <summary>Seconds a desk scan takes (the shift clock keeps running).</summary>
    [Min(0.1f)] public float scanSeconds = 1.5f;

    /// <summary>The day-1 note on the scanner tray (ASCII; empty = no note).</summary>
    public string scanHint = "Drag papers onto the scanner to read them on the PC.";

    /// <summary>The last day the scanner note shows (0 = never).</summary>
    [Min(0)] public int scanHintUntilDay = 1;

    [Header("Papers")]
    /// <summary>Where handed-over papers land, 0..1 across the desk rectangle (reused in order when a traveller has more papers).</summary>
    public Vector2[] paperSpawnSlots =
    {
        new Vector2(0.58f, 0.71f), new Vector2(0.76f, 0.66f), new Vector2(0.62f, 0.29f), new Vector2(0.84f, 0.26f)
    };

    /// <summary>Seconds a paper takes to slide (hand-over, back from the scanner, away at the decision).</summary>
    [Min(0f)] public float paperSlideSeconds = 0.25f;

    [Header("Sorting bands (Default layer)")]
    /// <summary>The focus exit zone's order: above every desk prop.</summary>
    public int focusExitOrder = 10;

    /// <summary>The glass zone's order: above the exit zone, below the bezel.</summary>
    public int glassOrder = 11;

    /// <summary>The CRT bezel's power button and LED.</summary>
    public int bezelOrder = 12;

    /// <summary>The desktop canvas on the glass.</summary>
    public int screenCanvasOrder = 20;

    /// <summary>The bottom paper of the stack (each paper above adds 1).</summary>
    public int paperBaseOrder = 30;

    /// <summary>The paper being dragged: above every stacked paper.</summary>
    public int heldPaperOrder = 60;

    [Header("Traveller wheel (overlay reference px)")]
    /// <summary>The ring's horizontal and vertical radii.</summary>
    public Vector2 wheelRadii = new Vector2(300f, 200f);

    /// <summary>Every ring item's size.</summary>
    public Vector2 wheelItemSize = new Vector2(240f, 44f);

    /// <summary>The centre slot's size ("&lt; Back").</summary>
    public Vector2 wheelCentreSize = new Vector2(150f, 44f);

    /// <summary>The least gap between two ring items, or an item and the centre (the builder's fit check).</summary>
    [Min(0f)] public float wheelItemGap = 8f;

    /// <summary>The day-1 note above the traveller (ASCII; empty = no note).</summary>
    public string wheelHint = "Click the traveller to talk and ask for papers.";

    /// <summary>The last day the wheel note shows (0 = never).</summary>
    [Min(0)] public int wheelHintUntilDay = 1;

    [Header("Reply bubble")]
    /// <summary>Seconds the traveller's reply stays up.</summary>
    [Min(0.1f)] public float bubbleSeconds = 4f;

    /// <summary>Where the bubble sits from the traveller's anchor (overlay reference px).</summary>
    public Vector2 bubbleOffset = new Vector2(650f, 100f);
}
