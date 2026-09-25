using UnityEngine;

/// <summary>
/// Tuning for the physical desk in the office (pieces 7 and the office move):
/// screen power, the desktop's clone on the PC, the scanner, the papers on the
/// desk, the traveller, the traveller wheel and its speech bubble's pacing
/// (piece 8), the day-1 desk notes, and papers read in the hand (piece 10:
/// the paper's face, the examine pose). Geometry that belongs to the art
/// (where the desk, the PC, the scanner and the traveller are) comes from the
/// art scene's anchors (OfficeSceneContractSO), so another office supplies its
/// own. Created and assigned by Tools > TimeDesk > Build Office UI
/// (Assets/Data/Config/Desk_Default.asset). Every knob is read at runtime; the
/// checks on them (paper spawn slots, the wheel's fit, the paper face's
/// capacity) run only in the builder: re-run it after changing the spawn
/// slots, a wheel size or the face.
/// </summary>
[CreateAssetMenu(fileName = "Desk_Default", menuName = "TimeDesk/Office/Desk Config")]
public sealed class DeskConfigSO : ScriptableObject
{
    [Header("Screen power")]
    /// <summary>True when the screen is on at the start of a shift.</summary>
    public bool screenStartsOn = true;

    /// <summary>Which events turn a dark screen on.</summary>
    public PcWakeRules wake = new PcWakeRules();

    /// <summary>The frame's power LED colour while the screen is on.</summary>
    public Color ledOnColor = new Color(0.35f, 0.95f, 0.45f, 1f);

    /// <summary>The frame's power LED colour while the screen is off.</summary>
    public Color ledOffColor = new Color(0.15f, 0.17f, 0.15f, 1f);

    [Header("The desktop's clone on the PC")]
    /// <summary>Pixel size of the render texture the office PC's screen shows (the 4:3 desktop).</summary>
    public Vector2Int cloneResolution = new Vector2Int(1024, 768);

    /// <summary>Brightness of the clone on the glass (1 = as rendered; a CRT glows a little under the room's light).</summary>
    [Range(0.2f, 2f)] public float cloneBrightness = 1f;

    /// <summary>How much of the glass the fitted desktop fills (below 1 keeps the picture off a curved glass's rounded edges).</summary>
    [Range(0.5f, 1f)] public float cloneFill = 0.92f;

    [Header("Scanner")]
    /// <summary>Seconds a desk scan takes (the shift clock keeps running).</summary>
    [Min(0.1f)] public float scanSeconds = 1.5f;

    /// <summary>The day-1 note above the scanner: a UI string key (world_source.json ui.strings; empty = no note).</summary>
    public string scanHintKey = "desk.scanHint";

    /// <summary>The last day the scanner note shows (0 = never).</summary>
    [Min(0)] public int scanHintUntilDay = 1;

    [Header("Papers")]
    /// <summary>A paper's size on the desk in metres (width, depth): larger than life, so its title reads from the chair.</summary>
    public Vector2 paperSize = new Vector2(0.26f, 0.34f);

    /// <summary>Where handed-over papers land, 0..1 across the desk anchor's rectangle (reused in order when a traveller has more papers). Read at runtime; Build Office UI checks that every paper a traveller carries has a slot.</summary>
    public Vector2[] paperSpawnSlots =
    {
        new Vector2(0.5f, 0.62f), new Vector2(0.74f, 0.5f), new Vector2(0.27f, 0.45f), new Vector2(0.55f, 0.28f)
    };

    /// <summary>Seconds a paper takes to slide (hand-over, back from the scanner, away at the decision).</summary>
    [Min(0f)] public float paperSlideSeconds = 0.25f;

    /// <summary>Height between two stacked papers in metres (each paper above the desk adds one step).</summary>
    [Min(0.0002f)] public float paperStackStep = 0.0015f;

    /// <summary>How high a dragged paper is lifted above the stack, in metres.</summary>
    [Min(0f)] public float heldPaperLift = 0.02f;

    [Header("Traveller")]
    /// <summary>The traveller figure's height in metres (feet at the traveller anchor).</summary>
    [Min(0.5f)] public float travellerHeight = 1.8f;

    /// <summary>Tint on the traveller's layers and photo (the art is unlit; this sits it into the room's light).</summary>
    public Color travellerTint = new Color(0.9f, 0.88f, 0.84f, 1f);

    [Header("Traveller wheel (overlay reference px; Build Office UI checks the fit)")]
    /// <summary>The ring's horizontal and vertical radii (read at runtime; Build Office UI checks that the content's menu fits).</summary>
    public Vector2 wheelRadii = new Vector2(300f, 200f);

    /// <summary>Every ring item's size (read at runtime; Build Office UI checks that the content's menu fits).</summary>
    public Vector2 wheelItemSize = new Vector2(240f, 44f);

    /// <summary>The centre slot's size ("&lt; Back"; read at runtime; Build Office UI checks that the content's menu fits).</summary>
    public Vector2 wheelCentreSize = new Vector2(150f, 44f);

    /// <summary>The least gap between two ring items, or an item and the centre (the builder's fit check).</summary>
    [Min(0f)] public float wheelItemGap = 8f;

    /// <summary>The day-1 note above the traveller: a UI string key (world_source.json ui.strings; empty = no note).</summary>
    public string wheelHintKey = "desk.wheelHint";

    /// <summary>The last day the wheel note shows (0 = never).</summary>
    [Min(0)] public int wheelHintUntilDay = 1;

    [Header("Speech bubble (the traveller's lines, one after another)")]
    /// <summary>Seconds the traveller's last line stays up once fully shown, when no other line follows.</summary>
    [Min(0.1f)] public float bubbleSeconds = 4f;

    /// <summary>Characters a line types out per second (0 = the whole line at once).</summary>
    [Min(0f)] public float bubbleCharsPerSecond = 40f;

    /// <summary>The least seconds a line stays up once fully shown before the next line replaces it (never more than bubbleSeconds).</summary>
    [Min(0f)] public float bubbleMinSeconds = 1.5f;

    /// <summary>Where the bubble's centre sits from the traveller's anchor (overlay reference px): above the head, clear of the wheel's top item (radius y + half an item + half the bubble).</summary>
    public Vector2 bubbleOffset = new Vector2(0f, 290f);

    [Header("Examine (piece 10)")]
    /// <summary>A desk paper's face: its title band, rows (a label over a value) and photo, as fractions of the paper (read at runtime; Build Office UI builds the photo frame from it and checks that every document template's rows fit).</summary>
    public PaperFaceTuning face = new PaperFaceTuning();

    /// <summary>Papers held in the hand: the office slots, the dip under the wheel, the region beside the PC frame, the distance from the camera and the rise's time (screen heights, metres, seconds).</summary>
    public ExamineTuning examine = new ExamineTuning();

    /// <summary>The photo's tint while its paper is held (evenly lit, unlike travellerTint on the desk).</summary>
    public Color examineTint = Color.white;

    /// <summary>The tint on a held paper's row under the pointer (a picked row shows the compare highlight instead).</summary>
    public Color rowHoverTint = new Color(0f, 0f, 0f, 0.06f);

    [Header("READY sign")]
    /// <summary>The caption the game writes on the READY sign's label (the art's NEXT sign): a UI string key (world_source.json ui.strings).</summary>
    public string readyCaptionKey = "desk.readyCaption";
}
