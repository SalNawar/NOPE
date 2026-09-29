using UnityEngine;

/// <summary>
/// Tuning for the physical desk in the office (pieces 7 and the office move):
/// screen power, the desktop's clone on the PC, the scanner, the papers on the
/// desk, the traveller, the traveller wheel and its speech bubble's pacing
/// (piece 8), the day-1 desk notes, papers read in the hand (piece 10: the
/// examine pose; a paper's printed form is FormStyleSO's) and the desk view. Geometry that belongs to the art
/// (where the desk, the PC, the scanner and the traveller are) comes from the
/// art scene's anchors (OfficeSceneContractSO), so another office supplies its
/// own. Created and assigned by Tools > TimeDesk > Build Office UI
/// (Assets/Data/Config/Desk_Default.asset). Every knob is read at runtime; the
/// checks on them (paper spawn slots, the wheel's fit, the paper's size
/// against the form style's aspect) run only in the builder: re-run it after
/// changing the spawn slots, a wheel size or the paper.
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

    /// <summary>Seconds a scan by hand takes with the Analysis Scanner (the analysis pass, the PC redesign SC4); a scan the Auto-Feed Scanner feeds itself keeps scanSeconds.</summary>
    [Min(0.1f)] public float analysisScanSeconds = 3f;

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

    /// <summary>Where a handed-over paper may land when no spawn slot shows whole on the screen (papers held, the overlay over the mat): a grid of this many columns and rows of spots over the landing area, nearest its centre first (PaperLanding.GridSpots).</summary>
    public Vector2Int landingGrid = new Vector2Int(5, 4);

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

    /// <summary>Seconds the traveller stays after their reaction's last line is fully shown, then leaves (the personalities spec's R4; 0 leaves at once, the reaction only in the transcript); calling the next traveller ends it at once. Authored here: Generate World never writes it.</summary>
    [Min(0f)] public float reactionSeconds = 2.5f;

    [Header("Examine (piece 10)")]
    /// <summary>Papers held in the hand: the office slots, the dip under the wheel, the region beside the PC frame, the distance from the camera and the rise's time (screen heights, metres, seconds).</summary>
    public ExamineTuning examine = new ExamineTuning();

    /// <summary>The photo's tint while its paper is held (evenly lit, unlike travellerTint on the desk).</summary>
    public Color examineTint = Color.white;

    /// <summary>Where the stamp tray's centre sits from the stamp (overlay reference px): above it.</summary>
    public Vector2 stampTrayOffset = new Vector2(0f, 140f);

    [Header("Desk view (piece 10)")]
    /// <summary>The camera tilted forward over the desk (a click on the mat): how far it moves from the art office's view (forward and up, metres), how much further it pitches than aiming at the mat's centre (degrees), and the blend's seconds (a cut under Reduced Motion).</summary>
    public DeskViewTuning deskView = new DeskViewTuning();

    [Header("Anime hall (the art's SetTime hook, AnimeHallShiftLink)")]
    /// <summary>Shift progress (0 opening, 1 closing) at which the anime hall's daylight starts turning to evening (the crowds' curve, CrowdPaletteBlend).</summary>
    [Range(0f, 1f)] public float hallEveningStartsAt = 0.5f;

    /// <summary>Shift progress at which the anime hall shows its full evening.</summary>
    [Range(0f, 1f)] public float hallEveningFullAt = 0.9f;

    /// <summary>The hall calendar's ink by day (its date on the art's paper, AnimeHallShiftLink): black, so it still reads 4.5:1 on screen as the evening dims the paper toward the switch (the art's own dark grey reads under 4.5:1 from an evening of 0.44).</summary>
    public Color hallCalendarDayInk = Color.black;

    /// <summary>The hall calendar's ink once the evening has dimmed its paper (from hallCalendarEveningInkFrom on): white, 10:1 on the full evening's paper.</summary>
    public Color hallCalendarEveningInk = Color.white;

    /// <summary>The evening blend (0 morning to 1 full evening: the hall's SetTime) from which the calendar takes its evening ink: where its paper reads as well under either ink, near a luminance of 0.18 on screen (the project draws in linear colour); measured in the hall, both inks read 4.5:1 or better only from 0.548 to 0.562 of the blend.</summary>
    [Range(0f, 1f)] public float hallCalendarEveningInkFrom = 0.555f;

    [Header("Anime hall: the Departure Board and the portal rings (the portals spec v3 BD2, VX1-VX7)")]
    /// <summary>The board's rows' ink (FFF2D9, the ivory of the hall's other glasses; the text is unlit, the display darkens with the evening, so its contrast only rises).</summary>
    public Color hallBoardInk = new Color(1f, 0.949f, 0.851f, 1f);

    /// <summary>The ink of the board's state words (CLOSED, UNDER MAINTENANCE, NO ROUTE): an amber-red (5.9:1 on the display by day as drawn, more as the evening darkens it).</summary>
    public Color hallBoardStateInk = new Color(1f, 0.62f, 0.4f, 1f);

    /// <summary>The share of the display's width and height the rows keep clear on each side (so they stay below the claim strip).</summary>
    [Range(0f, 0.45f)] public float hallBoardInset = 0.08f;

    /// <summary>Each portal's art layers by their ids in the hall's AnimeHallPresentation (a renamed or renumbered layer is an edit here): the secure bay (its order less one is the effect's), the metal ring (tinted; its opaque rect places the effect) and the painted glass.</summary>
    public HallPortalLayers[] hallPortalLayers =
    {
        new HallPortalLayers(1, "38 Portal 01 front secure bay", "39 Portal 01 front metal ring", "53 Portal 01 front painted glass"),
        new HallPortalLayers(2, "40 Portal 02 rear left secure bay", "41 Portal 02 rear left metal ring", "54 Portal 02 rear left painted glass"),
        new HallPortalLayers(3, "42 Portal 03 rear right secure bay", "43 Portal 03 rear right metal ring", "55 Portal 03 rear right painted glass"),
        new HallPortalLayers(4, "44 Portal 04 upper left secure bay", "45 Portal 04 upper left metal ring", "56 Portal 04 upper left painted glass"),
        new HallPortalLayers(5, "46 Portal 05 upper right secure bay", "47 Portal 05 upper right metal ring", "57 Portal 05 upper right painted glass"),
    };

    /// <summary>A ring under maintenance: its metal ring's tint (a 45 % grey, multiplied with the art's own lighting, so the evening still darkens it).</summary>
    public Color hallPortalIdleTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    /// <summary>An open departure ring's glow (an unlit tint, brightened by the PortalGlow material: a pale cyan).</summary>
    public Color hallPortalGlowTint = new Color(0.35f, 0.85f, 1f, 0.9f);

    /// <summary>The Return Gate's spiral (an unlit tint, brightened by the PortalGlow material: an amber).</summary>
    public Color hallReturnGateTint = new Color(1f, 0.6f, 0.15f, 0.95f);

    /// <summary>The effect's diameter as a share of its ring's opaque width, so its edge hides under the frame.</summary>
    [Range(0.1f, 1f)] public float hallPortalGlowSize = 0.85f;

    /// <summary>How fast a glow turns (degrees a second; still with reduced motion).</summary>
    public float hallPortalSpinDegrees = 12f;

    /// <summary>A departure's flare: the effect's scale at its peak.</summary>
    [Min(1f)] public float hallPortalPulseScale = 1.3f;

    /// <summary>A departure's flare: seconds, up and down (with reduced motion: one step up, then down at its end).</summary>
    [Min(0.05f)] public float hallPortalPulseSeconds = 0.8f;

    [Header("READY sign")]
    /// <summary>The caption the game writes on the READY sign's label (the art's NEXT sign): a UI string key (world_source.json ui.strings).</summary>
    public string readyCaptionKey = "desk.readyCaption";
}

/// <summary>One portal's art layers in the anime hall (DeskConfigSO.hallPortalLayers), by their AnimeHallPresentation ids.</summary>
[System.Serializable]
public sealed class HallPortalLayers
{
    /// <summary>The portal's number (agency.portals[].number).</summary>
    public int portal;

    /// <summary>The secure bay's layer (its fence and panels stand in front of the ring's lower half); the effect draws at its order less one.</summary>
    public string bay;

    /// <summary>The metal ring's layer (the gate frame): tinted under maintenance; its opaque rect places the effect.</summary>
    public string ring;

    /// <summary>The painted glass's layer (drawn over the effect; listed so a renamed layer is one edit here).</summary>
    public string glass;

    /// <summary>An empty entry (for the serializer and the inspector).</summary>
    public HallPortalLayers()
    {
    }

    /// <summary>A portal's layers.</summary>
    public HallPortalLayers(int portal, string bay, string ring, string glass)
    {
        this.portal = portal;
        this.bay = bay;
        this.ring = ring;
        this.glass = glass;
    }
}
