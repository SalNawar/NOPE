using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// One bay of the desk's plan: where an object stands in the desk's own
/// frame (metres right of and away from the chair from the mat's centre,
/// along the office view's level axes: DeskFrame), its own footprint there
/// (across its front, front to back) and its turn from facing the chair. A
/// home of the desk home map as a placement (DeskLayoutSettings.Bay).
/// </summary>
[Serializable]
public sealed class DeskBay
{
    /// <summary>The bay's centre: metres right of (x) and away from the chair (y) from the mat's centre.</summary>
    public Vector2 at;

    /// <summary>The object's own footprint (metres across its front, metres front to back), before its turn.</summary>
    public Vector2 size;

    /// <summary>The turn from facing the chair (degrees; + turns its front toward the viewer's left).</summary>
    public float yaw;

    /// <summary>An empty bay (for the serializer).</summary>
    public DeskBay()
    {
    }

    /// <summary>A bay at <paramref name="x"/>, <paramref name="y"/> for an object <paramref name="width"/> by <paramref name="depth"/>, turned <paramref name="yaw"/>.</summary>
    public DeskBay(float x, float y, float width, float depth, float yaw = 0f)
    {
        at = new Vector2(x, y);
        size = new Vector2(width, depth);
        this.yaw = yaw;
    }

    /// <summary>The bay a home gives its object.</summary>
    public DeskBay(DeskHome home) : this(home.x, home.y, home.width, home.depth, home.yaw)
    {
    }

    /// <summary>The rectangle the turned object covers in the desk's frame (yaw-aware).</summary>
    public DeskRect Rect => DeskArrangement.Footprint(at.x, at.y, size.x, size.y, yaw);
}

/// <summary>A small decoration of the art's desk the player may move (the desk layout spec §3: "small decorations he can"): its id (its saved place's key) and the art's parts it is made of (every mesh whose name starts with one of the prefixes and "__").</summary>
[Serializable]
public sealed class DeskDecorItem
{
    /// <summary>The id its place is saved under (DeskDecor).</summary>
    public string id;

    /// <summary>The art's part prefixes ("Clean_FormSorter" takes every "Clean_FormSorter__*" mesh).</summary>
    public string[] parts;

    /// <summary>An empty item (for the serializer).</summary>
    public DeskDecorItem()
    {
    }

    /// <summary>The decoration <paramref name="id"/> made of <paramref name="parts"/>.</summary>
    public DeskDecorItem(string id, params string[] parts)
    {
        this.id = id;
        this.parts = parts;
    }
}

/// <summary>
/// The desk's plan (the desk home map Saleh approved on 2026-10-20, "DESK
/// APPROVED": "Yes, build it"; "Mix: B + A rail + C lamps"; "keep a big mat
/// size"; the stamp rack's "daters front-to-back"; "we will cut all but decor
/// objects later will go on clear places like restory"): every object's one
/// home (DeskHome, DeskHomeMap.Approved: the big art mat holds papers only;
/// the deal tray, DETAIN and the bell on the counter row; the intercom, the
/// BABEL tuner, the rulebook's pocket and the UV torch's pouch left of the
/// mat; A's machine rail right of it in the order of use; the later and
/// Phase B objects' homes kept, positions only), the fixed art the plan
/// keeps clear of, the art props cut (their parts switched off at bind), the
/// decorations the player moves, the plan's gate (Problems: the builder
/// fails on any) and the office camera's framing (58 degrees, 5 further
/// down: Saleh's "Yes"). Read at bind (OfficeSceneBinder, DeskLayout). A
/// section of DeskConfigSO.
/// </summary>
[Serializable]
public sealed class DeskLayoutSettings
{
    [Header("The office view (Saleh 2026-10-18: \"Yes\" to 58 degrees, 5 further down)")]
    /// <summary>The office camera's vertical field of view at 16:9 (degrees; 0 keeps the art's).</summary>
    [Range(0f, 90f)] public float officeFieldOfView = 58f;

    /// <summary>How much further down than the art's pose the office camera looks (degrees).</summary>
    [Range(-20f, 30f)] public float officeExtraPitch = 5f;

    [Header("The home map (DeskHomeMap; Saleh's DESK APPROVED, 2026-10-20)")]
    /// <summary>Every object's one home (ids: DeskHomeMap's constants).</summary>
    public DeskHome[] homes = DeskHomeMap.Approved();

    /// <summary>The fixed art the homes keep clear of (the PC island, the till, the AVAILABLE sign, the lamp), with their heights.</summary>
    public DeskHome[] fixedArt = DeskHomeMap.FixedArt();

    [Header("The slide-in (desk v3, Saleh: \"yes but they need to slide\"; DESK-HOMES)")]
    /// <summary>The slide-in: the trigger, the timings, the clearances, the D6 focus and every slider (DeskSlides.Approved: the desk pipeline's render/slides.json); a track slides the way its home declares (DeskHome.slide).</summary>
    public DeskSlides slides = DeskSlides.Approved();

    /// <summary>The chip reader's size against its model's life size.</summary>
    [Min(0.1f)] public float chipReaderScale = 0.9f;

    [Header("The painted desk (desk v3, Saleh: \"yes but they need to slide\"; UNITY_PLAN 8: the DeskArt.Painted toggle)")]
    /// <summary>
    /// DeskArt.Painted: when on and the gameplay scene holds the painted desk
    /// (DeskPainted, put there by Build Office UI), it replaces the art's desk
    /// when the office binds: its homes take over the map (UsePainted), the
    /// art desk's parts hide (paintedCutParts) and the gameplay objects take
    /// its look; off, the art's desk as before (A/B and rollback for one
    /// integration cycle).
    /// </summary>
    public bool paintedDesk = true;

    /// <summary>
    /// The art desk's parts the painted desk replaces: every renderer of the
    /// art office whose name starts with one hides once the office is bound
    /// (the desk and its rails, the mat, the PC, keyboard and mouse, the
    /// phone, the till and its credits, the NEXT sign and its caption, the
    /// lamp, the old stamp and ink pad, the binder, the booth's partitions,
    /// the art's own shadow cards, and the right cork board's stability
    /// monitor, which the painted booth wall stands in front of: its readouts
    /// go to the fallback HUD), and the art's decorations, the form sorter and
    /// the tray with its paper bundle (the painting has none: on the painted
    /// desk they are neither drawn, gathered nor dragged: DeskDecor never
    /// sees them; Saleh: a clean, organised desk where every object matches
    /// the painting), and both corkboards whole (an entry naming a parent
    /// exactly takes every part under it: the painting has no corkboard; the
    /// left one's clock card and calendar hung over the city window; the
    /// clock and the date are the PC's CRT clock's, the calendar's click lies
    /// on its date line: OfficeSceneBinder.BindPaintedCalendar).
    /// </summary>
    public string[] paintedCutParts =
    {
        "Finish_Desk__", "Finish_DeskRail__", "Clean_Blotter", "CRT2_", "Clean_Keyboard", "Clean_Mouse", "Clean_Phone", "Clean_Till", "CreditsNumber",
        "Clean_Next", "NextLabel", "Clean_Lamp", "Clean_Stamp", "Clean_Inkpad", "Clean_Binder", "Office_Partition__", "Clean Art",
        "Office_Stability__", "StabilityPercent", "Finish_RightEphemera__", "Clean_FormSorter__", "Clean_Tray__", "Clean_PaperBundle__",
        "Left board assembly", "Right board assembly",
    };

    [Header("Cut (Saleh 2026-10-20: \"we will cut all but decor objects later will go on clear places like restory\")")]
    /// <summary>The art's props cut from the desk: their parts ("prefix__*") are switched off when the art office binds.</summary>
    public string[] cutParts = { "Clean_Calculator", "Clean_Binder", "Clean_PenPot", "Clean_Stapler", "Clean_Inkpad" };

    [Header("The plan's check (the builder fails on a problem)")]
    /// <summary>The art desk's top in the frame (every home lies on it).</summary>
    public DeskBay deskTop = new DeskBay(0f, 0.515f, 5.8f, 2.53f);

    /// <summary>The art's paper mat in the frame for the builder's check (at run time the art's own mat is used: DeskLayout.Mat).</summary>
    public DeskBay mat = new DeskBay(0f, 0f, 1.46f, 1.03f);

    /// <summary>The office camera's eye for the sight check: metres right, metres from the mat's centre toward the traveller, metres over the desk.</summary>
    public Vector3 officeEye = new Vector3(0f, -2.10f, 1.09f);

    /// <summary>How much of every shown object's top the office camera must see (BUG-4: "≥90% visible").</summary>
    [Range(0f, 1f)] public float minVisible = 0.9f;

    [Header("Decor (the art's small props the player moves, DeskDecor)")]
    /// <summary>The decorations: the art's small props kept (the rest are cut), each starting in the decor home.</summary>
    public DeskDecorItem[] decor =
    {
        new DeskDecorItem("form_sorter", "Clean_FormSorter"),
        new DeskDecorItem("tray", "Clean_Tray", "Clean_PaperBundle"),
    };

    /// <summary>The grid the decorations start on in the decor home (metres, DeskArrangement.TrySettle).</summary>
    [Min(0.005f)] public float decorStep = 0.02f;

    /// <summary>How far a decoration let go on a taken spot is nudged at most (metres; BUG-23: "nudged at most 3 cm"), on a 5 mm grid; further, it goes back.</summary>
    [Min(0f)] public float decorNudge = 0.03f;

    /// <summary>How high a dragged decoration lifts off the desk (metres), over whatever lies under it.</summary>
    [Min(0f)] public float decorLift = 0.03f;

    /// <summary>The map while the painted desk shows (UsePainted), else null.</summary>
    [NonSerialized] private DeskHome[] _paintedHomes;

    /// <summary>The painted desk's fixed art while it shows, else null.</summary>
    [NonSerialized] private DeskHome[] _paintedFixedArt;

    /// <summary>The homes in use: the painted desk's map while it shows (UsePainted), else <see cref="homes"/>.</summary>
    public DeskHome[] Homes => _paintedHomes ?? homes;

    /// <summary>The fixed art in use: the painted desk's while it shows, else <see cref="fixedArt"/>.</summary>
    public DeskHome[] FixedArt => _paintedFixedArt ?? fixedArt;

    /// <summary>
    /// The painted desk's homes <paramref name="painted"/> take over the map
    /// (DeskPaintedMap.Merge over <see cref="homes"/>) with its
    /// <paramref name="paintedFixedArt"/>, until called with null (the
    /// office binder: at bind, and when the office goes). Kept off the asset
    /// (never serialized).
    /// </summary>
    public void UsePainted(IReadOnlyList<DeskHome> painted, IReadOnlyList<DeskHome> paintedFixedArt)
    {
        _paintedHomes = painted != null ? DeskPaintedMap.Merge(homes, painted) : null;
        _paintedFixedArt = painted != null ? (paintedFixedArt ?? Array.Empty<DeskHome>()).ToArray() : null;
    }

    /// <summary>The home <paramref name="id"/> (DeskHomeMap's constants) in the map in use, or null.</summary>
    public DeskHome HomeOf(string id) => DeskHomeMap.Find(Homes, id);

    /// <summary>The placement the home <paramref name="id"/> gives its object, or null without that home.</summary>
    public DeskBay Bay(string id)
    {
        DeskHome home = HomeOf(id);
        return home != null ? new DeskBay(home) : null;
    }

    /// <summary>What is wrong with the plan in use (DeskArrangement.HomeProblems against the art desk, the mat, the fixed art, the slide reach and the office camera's sight; DeskSlides.Problems: the sliders against the homes; ClickBoxProblems with the builder's click pad); empty when it holds.</summary>
    public List<string> Problems() => Problems(Homes, FixedArt, minVisible);

    /// <summary>
    /// What is wrong with the painted desk's plan: its homes
    /// <paramref name="painted"/> merged over <see cref="homes"/>
    /// (DeskPaintedMap.Merge), against its <paramref name="paintedFixedArt"/>,
    /// by the same checks as Problems but the office camera's box sight check:
    /// desk v3's sight was gated on its real render with the real occlusion
    /// (the pipeline's render/legibility_office.txt and its slid states), where
    /// the boxes miss that the turned PC leaves the back step's homes in view;
    /// UNITY_PLAN 10.1 gates it again on Unity's ID render.
    /// </summary>
    public List<string> PaintedProblems(IReadOnlyList<DeskHome> painted, IReadOnlyList<DeskHome> paintedFixedArt) =>
        Problems(DeskPaintedMap.Merge(homes, painted), (paintedFixedArt ?? Array.Empty<DeskHome>()).ToArray(), 0f);

    private List<string> Problems(DeskHome[] map, DeskHome[] art, float sight)
    {
        List<string> problems = DeskArrangement.HomeProblems(map, art, mat.Rect, deskTop.Rect, DeskHomeMap.SlideReach, (officeEye.x, officeEye.y, officeEye.z), sight);
        if (slides != null)
            problems.AddRange(slides.Problems(map, DeskHomeMap.SlideReach));
        problems.AddRange(DeskArrangement.ClickBoxProblems(map, art, DeskHomeMap.ClickPad, DeskHomeMap.NoClickBox));
        return problems;
    }
}

/// <summary>
/// The desk's frame (the desk layout spec): the mat's centre on the desk's
/// top and the office view's level right and forward, so a DeskBay's place
/// becomes a world pose (the office binder makes it once the desk is bound).
/// </summary>
public readonly struct DeskFrame
{
    /// <summary>The frame at <paramref name="matCentre"/> (its height: the desk's top), facing along <paramref name="levelForward"/> (made level).</summary>
    public DeskFrame(Vector3 matCentre, Vector3 levelForward)
    {
        Origin = matCentre;
        Vector3 level = Vector3.ProjectOnPlane(levelForward, Vector3.up);
        Forward = level.sqrMagnitude > 1e-6f ? level.normalized : Vector3.forward;
        Right = Vector3.Cross(Vector3.up, Forward);
    }

    /// <summary>The mat's centre on the desk's top.</summary>
    public Vector3 Origin { get; }

    /// <summary>The office view's level forward (away from the chair) and right.</summary>
    public Vector3 Forward { get; }

    /// <inheritdoc cref="Forward"/>
    public Vector3 Right { get; }

    /// <summary>The world point at <paramref name="at"/> in the frame, <paramref name="lift"/> above the desk's top.</summary>
    public Vector3 Point(Vector2 at, float lift = 0f) => Origin + Right * at.x + Forward * at.y + Vector3.up * lift;

    /// <summary>A thing facing the chair, turned by <paramref name="yaw"/> degrees.</summary>
    public Quaternion Facing(float yaw) => Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.LookRotation(Forward, Vector3.up);

    /// <summary>Where <paramref name="world"/> lies in the frame (metres right, metres away from the chair).</summary>
    public Vector2 Local(Vector3 world)
    {
        Vector3 d = world - Origin;
        return new Vector2(Vector3.Dot(d, Right), Vector3.Dot(d, Forward));
    }

    /// <summary>The rectangle in the frame <paramref name="world"/> covers seen from above (its corners along the frame's axes).</summary>
    public DeskRect Footprint(Bounds world)
    {
        float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            Vector2 c = Local(new Vector3((i & 1) == 0 ? world.min.x : world.max.x, world.min.y, (i & 2) == 0 ? world.min.z : world.max.z));
            x0 = Mathf.Min(x0, c.x);
            x1 = Mathf.Max(x1, c.x);
            y0 = Mathf.Min(y0, c.y);
            y1 = Mathf.Max(y1, c.y);
        }
        return new DeskRect((x0 + x1) / 2f, (y0 + y1) / 2f, x1 - x0, y1 - y0);
    }

    /// <summary>Puts <paramref name="target"/> on <paramref name="bay"/>: at its centre, <paramref name="lift"/> above the desk, facing the chair turned by its yaw; false (it stays) without a target or a bay.</summary>
    public bool Place(Transform target, DeskBay bay, float lift)
    {
        if (target == null || bay == null)
            return false;
        target.SetPositionAndRotation(Point(bay.at, lift), Facing(bay.yaw));
        return true;
    }
}

/// <summary>
/// The desk home map at run time (DESK-PLAN's seam; FIX_PLAN_v2: "Phase B
/// objects bind to a home by id and never edit DeskLayout"): the bound
/// frame, the art mat (DeskMat: papers only), every home by id (HomeOf, Pose,
/// LampPoint), the desk's obstacles (DeskObstacles: each shown object's
/// footprint and height, the fixed art's too; a carried thing rises over
/// them) and C's next-tool lamp (Lamps: which home is lit; FEEDBACK and
/// DESK-HOMES draw it). Bound by the office binder once the art desk is
/// arranged; the objects standing on homes are tracked so their obstacles
/// follow what shows (RefreshObstacles: at bind, at each day's start, when
/// the crate opens).
/// </summary>
public static class DeskLayout
{
    private static DeskLayoutSettings _settings;
    private static readonly Dictionary<string, Transform> Objects = new Dictionary<string, Transform>();
    private static readonly HashSet<string> Registered = new HashSet<string>();

    /// <summary>True once the office binder has bound the desk.</summary>
    public static bool Bound { get; private set; }

    /// <summary>The desk's frame (valid when Bound).</summary>
    public static DeskFrame Frame { get; private set; }

    /// <summary>The art's paper mat in the frame (papers and the open rulebook only).</summary>
    public static DeskMat Mat { get; private set; }

    /// <summary>The desk's obstacles (registered through Register: every shown object on its home, the fixed art, the decorations); a carried thing reads HeightAt.</summary>
    public static DeskObstacles Obstacles { get; } = new DeskObstacles();

    /// <summary>Registers (or moves) the desk obstacle <paramref name="id"/>: its <paramref name="footprint"/> in the frame and its <paramref name="height"/> over the desk (forgotten at Unbind).</summary>
    public static void Register(string id, DeskRect footprint, float height)
    {
        if (string.IsNullOrEmpty(id))
            return;
        Obstacles.Register(id, footprint, height);
        Registered.Add(id);
    }

    /// <summary>Forgets the desk obstacle <paramref name="id"/>.</summary>
    public static void Unregister(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;
        Obstacles.Unregister(id);
        Registered.Remove(id);
    }

    /// <summary>C's next-tool lamp (the booth coordinator lights it; NextToolLamp).</summary>
    public static DeskLamps Lamps { get; } = new DeskLamps();

    /// <summary>Binds the desk: its <paramref name="frame"/>, the art <paramref name="mat"/> and the plan's <paramref name="settings"/>; the fixed art becomes obstacles.</summary>
    public static void Bind(DeskFrame frame, DeskRect mat, DeskLayoutSettings settings)
    {
        Unbind();
        Frame = frame;
        Mat = new DeskMat(mat);
        _settings = settings;
        foreach (DeskHome art in settings != null && settings.FixedArt != null ? settings.FixedArt : Array.Empty<DeskHome>())
            if (art != null)
                Register(art.id, art.Footprint, art.height);
        Bound = settings != null;
    }

    /// <summary>The desk is gone (the office unloaded): nothing is bound, tracked, registered or lit.</summary>
    public static void Unbind()
    {
        foreach (string id in Registered)
            Obstacles.Unregister(id);
        Registered.Clear();
        SweptBoxes.Clear();
        Objects.Clear();
        _settings = null;
        Bound = false;
        Lamps.Show(null);
    }

    /// <summary>The home <paramref name="id"/> (DeskHomeMap's constants), or null (unbound, or no such home).</summary>
    public static DeskHome HomeOf(string id) => _settings != null ? _settings.HomeOf(id) : null;

    /// <summary>The world pose of the home <paramref name="id"/>: its centre on the desk's top and its facing; false when unbound or there is no such home.</summary>
    public static bool TryPose(string id, out Vector3 position, out Quaternion rotation)
    {
        DeskHome home = HomeOf(id);
        position = default;
        rotation = Quaternion.identity;
        if (!Bound || home == null)
            return false;
        position = Frame.Point(new Vector2(home.x, home.y));
        rotation = Frame.Facing(home.yaw);
        return true;
    }

    /// <summary>The world point of the home <paramref name="id"/>'s lamp (DeskHome.LampPoint on the desk's top); false when unbound or there is no such home.</summary>
    public static bool TryLampPoint(string id, out Vector3 point)
    {
        DeskHome home = HomeOf(id);
        point = default;
        if (!Bound || home == null)
            return false;
        (float x, float y) lamp = home.LampPoint;
        point = Frame.Point(new Vector2(lamp.x, lamp.y));
        return true;
    }

    /// <summary>The object <paramref name="target"/> stands on the home <paramref name="id"/>: its obstacle follows whether it shows (RefreshObstacles).</summary>
    public static void Track(string id, Transform target)
    {
        if (string.IsNullOrEmpty(id) || target == null)
            return;
        Objects[id] = target;
    }

    /// <summary>The object standing on the home <paramref name="id"/> (DeskLayout.Track), or null: the slide-in binds to it (OfficeSceneBinder.BindSlideIn).</summary>
    public static Transform ObjectOf(string id) => id != null && Objects.TryGetValue(id, out Transform target) ? target : null;

    /// <summary>The home id of the tracked object <paramref name="t"/> stands under (itself or a part of it: one desk object's click boxes), or null (the QA rig's click-box check, BUG-4a).</summary>
    public static string HomeIdOf(Transform t)
    {
        if (t == null)
            return null;
        foreach (KeyValuePair<string, Transform> tracked in Objects)
            if (tracked.Value != null && t.IsChildOf(tracked.Value))
                return tracked.Key;
        return null;
    }

    /// <summary>
    /// Registers every tracked object that shows as an obstacle on its home's
    /// footprint, as tall as its renderers stand over the desk at rest (its
    /// home's height when it has none showing; a part of a slider counted at
    /// its rest height, MachineSlideIn.Raised taken off, so a lift raised when
    /// this runs is not counted twice), swept by the slide-in of what rests
    /// in it (a track's footprint to its overshoot peak toward the mat, the
    /// top raised by its lifts' peak, the stamp rack by its daters':
    /// DeskSlides.Obstacle), and forgets those that do not show. The swept
    /// boxes of the homes something slides or lifts in are also kept apart
    /// (SweptTopAt, SweptStrips): the carry and the drop read them, so a paper
    /// is never covered or passed through by a machine that slides out after
    /// the drag began.
    /// </summary>
    public static void RefreshObstacles()
    {
        if (!Bound)
            return;
        SweptBoxes.Clear();
        foreach (KeyValuePair<string, Transform> tracked in Objects)
        {
            DeskHome home = HomeOf(tracked.Key);
            Transform target = tracked.Value;
            if (home == null || target == null || !target.gameObject.activeInHierarchy || target.lossyScale.sqrMagnitude < 1e-8f)
            {
                Unregister(tracked.Key);
                continue;
            }
            float top = home.height;
            bool any = false;
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>(false))
            {
                if (!r.enabled)
                    continue;
                MachineSlideIn slider = r.GetComponentInParent<MachineSlideIn>();
                float over = r.bounds.max.y - Frame.Origin.y - (slider != null ? slider.Raised : 0f);
                top = any ? Mathf.Max(top, over) : over;
                any = true;
            }
            (DeskRect footprint, float height) = _settings.slides != null ? _settings.slides.Obstacle(home, top) : (home.Footprint, top);
            Register(tracked.Key, footprint, height);
            if (_settings.slides != null && _settings.slides.Sweeps(home))
                SweptBoxes.Add((home, footprint, height));
        }
    }

    /// <summary>The swept boxes of the homes something slides or lifts in (RefreshObstacles): the home, its swept footprint in the frame and its height over the desk (metres).</summary>
    private static readonly List<(DeskHome home, DeskRect footprint, float height)> SweptBoxes = new List<(DeskHome, DeskRect, float)>();

    /// <summary>
    /// The world height of the tallest swept box (a home something slides or
    /// lifts in, at its overshoot peak) under <paramref name="world"/>, with
    /// its footprint grown by <paramref name="grow"/> metres (a carried
    /// paper's corners); false over none or unbound. A dragged paper keeps over
    /// it (DeskController's carry floor), so a machine that slides out under
    /// it after the drag began never passes through it, with no live read.
    /// Allocates nothing.
    /// </summary>
    public static bool TrySweptTop(Vector3 world, float grow, out float top)
    {
        top = float.NegativeInfinity;
        if (!Bound)
            return false;
        Vector2 at = Frame.Local(world);
        bool any = false;
        for (int i = 0; i < SweptBoxes.Count; i++)
        {
            DeskRect f = SweptBoxes[i].footprint;
            if (Mathf.Abs(at.x - f.CentreX) > f.Width / 2f + grow || Mathf.Abs(at.y - f.CentreY) > f.Height / 2f + grow)
                continue;
            top = Mathf.Max(top, Frame.Origin.y + SweptBoxes[i].height);
            any = true;
        }
        return any;
    }

    /// <summary>
    /// The world corners (on the desk) of each track's swept box: its home's
    /// footprint and the strip its slide reaches past it toward the mat
    /// (DESK-HOMES 6: 0 px over a resting paper). A paper let go keeps clear
    /// of each (DeskController.ClearOfBlockers). A fresh list.
    /// </summary>
    public static List<Vector3[]> SweptStrips()
    {
        var strips = new List<Vector3[]>();
        if (!Bound)
            return strips;
        for (int i = 0; i < SweptBoxes.Count; i++)
        {
            DeskRect rest = SweptBoxes[i].home.Footprint, swept = SweptBoxes[i].footprint;
            if (swept.Width <= rest.Width + 1e-4f)
                continue;
            float x0 = swept.CentreX - swept.Width / 2f, x1 = swept.CentreX + swept.Width / 2f, y0 = swept.CentreY - swept.Height / 2f, y1 = swept.CentreY + swept.Height / 2f;
            strips.Add(new[] { Frame.Point(new Vector2(x0, y0)), Frame.Point(new Vector2(x1, y0)), Frame.Point(new Vector2(x0, y1)), Frame.Point(new Vector2(x1, y1)) });
        }
        return strips;
    }

    /// <summary>
    /// The uniform scale that fits <paramref name="target"/>'s meshes (inactive
    /// ones too, from their own bounds) into the home <paramref name="home"/>'s
    /// footprint in <paramref name="frame"/>: 1 when it fits already or has no
    /// mesh, else smaller.
    /// </summary>
    public static float FitScale(Transform target, DeskHome home, DeskFrame frame)
    {
        if (target == null || home == null)
            return 1f;
        if (!TryMeshBounds(target, out Bounds world))
            return 1f;
        DeskRect have = frame.Footprint(world), room = home.Footprint;
        float k = 1f;
        if (have.Width > room.Width && have.Width > 1e-5f)
            k = Mathf.Min(k, room.Width / have.Width);
        if (have.Height > room.Height && have.Height > 1e-5f)
            k = Mathf.Min(k, room.Height / have.Height);
        return k;
    }

    /// <summary>The world bounds of every mesh under <paramref name="root"/> (inactive ones too, from the meshes' own bounds); false with none.</summary>
    public static bool TryMeshBounds(Transform root, out Bounds world)
    {
        world = default;
        bool any = false;
        foreach (MeshFilter mesh in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mesh.sharedMesh == null)
                continue;
            Bounds b = mesh.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.transform.TransformPoint(new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z));
                if (any)
                    world.Encapsulate(corner);
                else
                    world = new Bounds(corner, Vector3.zero);
                any = true;
            }
        }
        return any;
    }
}
