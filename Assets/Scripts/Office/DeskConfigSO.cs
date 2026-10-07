using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Tuning for the physical desk in the office (pieces 7 and the office move):
/// screen power, the desktop's clone on the PC, the scanner, the papers on the
/// desk, the traveller, the traveller wheel and its speech bubble's pacing
/// (piece 8), the day-1 desk notes, the counter and the desk (Papers,
/// Please's zones: a document small on the counter, full size on the desk; a
/// paper's printed form is FormStyleSO's), the stamp bar and the desk view. Geometry that belongs to the art
/// (where the desk, the PC, the scanner and the traveller are) comes from the
/// art scene's anchors (OfficeSceneContractSO), so another office supplies its
/// own. Created and assigned by Tools > TimeDesk > Build Office UI
/// (Assets/Data/Config/Desk_Default.asset). Every knob is read at runtime; the
/// checks on them (the counter's spots, the wheel's fit, the paper's size
/// against the form style's aspect) run only in the builder: re-run it after
/// changing the counter's spots, a wheel size or the paper.
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

    /// <summary>The deepest shadow behind the scanner, in metres, where its body hides a paper from the office camera (ScannerClearance: a paper left there moves out to the left, right or front; the desk-first redesign, item 4).</summary>
    [Min(0f)] public float scannerShadowMax = 0.35f;

    /// <summary>The day-1 note above the scanner: a UI string key (world_source.json ui.strings; empty = no note).</summary>
    public string scanHintKey = "desk.scanHint";

    /// <summary>The last day the scanner note shows (0 = never).</summary>
    [Min(0)] public int scanHintUntilDay = 1;

    [Header("Papers")]
    /// <summary>A paper's size on the desk in metres (width, depth): larger than life, so its title reads from the chair.</summary>
    public Vector2 paperSize = new Vector2(0.26f, 0.34f);

    /// <summary>How many spots papers handed over land on along the counter (DeskZones.CounterSpot; reused in turn when a traveller has more papers). Read at runtime; Build Office UI checks that every paper a traveller carries has a spot.</summary>
    [Min(1)] public int counterSpots = 4;

    /// <summary>The distance between two of the counter's spots, in metres (closer when the counter is too short).</summary>
    [Min(0f)] public float counterSpacing = 0.17f;

    /// <summary>Seconds a paper takes to slide (hand-over, back from the scanner, away at the decision).</summary>
    [Min(0f)] public float paperSlideSeconds = 0.25f;

    /// <summary>Height between two stacked papers in metres (each paper above the desk adds one step).</summary>
    [Min(0.0002f)] public float paperStackStep = 0.0015f;

    /// <summary>How high a dragged paper is lifted above the stack, in metres.</summary>
    [FormerlySerializedAs("heldPaperLift"), Min(0f)] public float dragLift = 0.02f;

    [Header("The counter and the desk (Papers, Please's zones, Saleh 2026-10-06)")]
    /// <summary>The counter: the slim strip this deep (metres) at the desk's far edge along the office view, on the traveller's side (Saleh 2026-10-06: "reduce the size of the top counter line"; it was 0.14); a paper dropped there once the passport carries its verdict hands the papers back, before that it bounces back to the desk.</summary>
    [FormerlySerializedAs("handBackDepth"), Min(0.02f)] public float counterDepth = 0.05f;

    /// <summary>How far nearer than the counter's far edge the papers handed over land (metres: their centres' row, DeskZones.CounterSpot); small papers there reach over the slim strip onto the desk.</summary>
    [Min(0f)] public float counterSpotInset = 0.07f;

    /// <summary>A document's scale on the counter (small: a share of its own size).</summary>
    [Range(0.2f, 1f)] public float counterScale = 0.6f;

    /// <summary>A document's height on the desk, full size, in metres (DeskZones.ReadingScale: every paper this tall, a wider one by its width): it reads in the reading view at 1280x720.</summary>
    [Min(0.05f)] public float readingHeight = 0.34f;

    /// <summary>The photo's tint while its paper lies on the desk, full size (evenly lit, unlike travellerTint on the counter).</summary>
    [FormerlySerializedAs("examineTint")] public Color readingTint = Color.white;

    [Header("Traveller")]
    /// <summary>The traveller figure's height in metres (feet at the traveller anchor).</summary>
    [Min(0.5f)] public float travellerHeight = 1.8f;

    /// <summary>Tint on the traveller's layers and photo (the art is unlit; this sits it into the room's light).</summary>
    public Color travellerTint = new Color(0.9f, 0.88f, 0.84f, 1f);

    [Header("Traveller wheel (overlay reference px; Build Office UI checks the fit)")]
    /// <summary>The ring's horizontal and vertical radii (read at runtime; Build Office UI checks that the content's menu fits): 365 x 225 fits nine choices, the hub's worst case with the differences entry (wave 5, lesson 3).</summary>
    public Vector2 wheelRadii = new Vector2(365f, 225f);

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

    [Header("The city view (the desk-first redesign, item 6; Saleh 2026-10-07)")]
    /// <summary>Seconds of the hall's turn toward its window wall (the art's left pan) when the player looks at the city; the turn back takes as long (a cut under Reduced Motion).</summary>
    [Min(0f)] public float citySeconds = 0.6f;

    /// <summary>The share of the turn (0..1) after which the whole city panorama starts fading in (on the way back it fades out first, then the hall turns back).</summary>
    [Range(0f, 1f)] public float cityFadeFrom = 0.5f;

    /// <summary>Seconds of the city panorama's fade in and out (0: a cut at the fade's start).</summary>
    [Min(0f)] public float cityFadeSeconds = 0.7f;

    /// <summary>The colour round the city panorama where it does not fill the screen (it is shown whole, fitted inside the screen; the palette's ink).</summary>
    public Color cityMatte = new Color(0.169f, 0.11f, 0.141f, 1f);

    [Header("Inspection at the desk (the desk-first redesign, item 11)")]
    /// <summary>Where the rulebook card lies: metres right of and ahead of the mat's centre along the office view's level right and forward (inside the desk view's frame; negative right: left of the mat).</summary>
    public Vector2 rulebookAt = new Vector2(-0.3f, -0.05f);

    [Header("Stamps (Papers, Please's stamp bar, Saleh 2026-10-06: the art's 3D stamps)")]
    /// <summary>How high above the desk the stamps' dies hang while the bar is out (metres): a paper slides under them (above a dragged paper's lift), and a press dips them down onto it.</summary>
    [Min(0.005f)] public float stampHover = 0.03f;

    /// <summary>Where the stamp bar's middle (between its two stamps) hangs out over the desk: the point of the desk the reading view shows there (viewport x, y; low on the right, so a passport whose visa box is under a stamp stands in the view above it).</summary>
    public Vector2 stampBarView = new Vector2(0.72f, 0.36f);

    /// <summary>How far the stamp bar slides out from the desk's right (metres along the office view's right): in, it waits that far right of where it hangs out, out of the reading view.</summary>
    [Min(0.1f)] public float stampBarTravel = 0.6f;

    /// <summary>Seconds the stamp bar takes to slide out or back (a cut under Reduced Motion).</summary>
    [FormerlySerializedAs("stampTraySeconds"), Min(0f)] public float stampBarSeconds = 0.3f;

    /// <summary>Seconds a stamp dragged out over the desk takes to go back to its place in the rack once pressed or let go (a cut under Reduced Motion).</summary>
    [Min(0f)] public float stampReturnSeconds = 0.2f;

    /// <summary>Seconds a refused press's note, or the counter's "Stamp the passport first", stays up.</summary>
    [Min(0.5f)] public float stampNoteSeconds = 2.5f;

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

    [Header("Anime hall: the swappable slots (the hall slots spec)")]
    /// <summary>The hall's swappable slots (Assets/Data/Config/HallSlots_Default.asset): the binder's HallSlotsLink swaps their art as the hall's variables shift; none: the hall stays as painted.</summary>
    public HallSlotsSO hallSlots;

    [Header("AVAILABLE sign")]
    /// <summary>The caption the game writes on the AVAILABLE sign's label (the art's NEXT sign; "AVAILABLE"): a UI string key (world_source.json ui.strings).</summary>
    public string readyCaptionKey = "desk.readyCaption";

    /// <summary>The caption's ink while the desk is paused (the shift's start, a break, after closing; AvailableSignLink): the art's lit ink turned down, like an unlit sign, that still reads on the sign's dark glass (large text: 3.2:1 as drawn in the hall at 1080p, against the lit ink's 16.7:1). Lit, the label keeps the art's own ink.</summary>
    public Color readyPausedInk = new Color(0.42f, 0.4f, 0.37f, 1f);
}

/// <summary>One portal's art layers in the anime hall (DeskConfigSO.hallPortalLayers), by their AnimeHallPresentation ids.</summary>
[System.Serializable]
public sealed class HallPortalLayers
{
    /// <summary>The portal's number (agency.portals[].number).</summary>
    public int portal;

    /// <summary>The secure bay's layer (its fence and panels stand in front of the ring's lower half); the effect draws at its order less one.</summary>
    public string bay;

    /// <summary>The metal ring's layer (the gate frame, with the wall and bay pixels around it): never tinted; its opaque rect places the effect.</summary>
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
