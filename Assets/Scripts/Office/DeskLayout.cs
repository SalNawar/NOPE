using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One bay of the desk's plan (the desk layout spec): where a machine, an
/// upgrade's object, a tool or a shelf stands, in the desk's own frame (metres
/// right of and away from the chair from the mat's centre, along the office
/// view's level axes: DeskFrame), its footprint there, its turn from facing
/// the chair and whether it stands on a back shelf (DeskLayoutSettings.shelfHeight up).
/// </summary>
[Serializable]
public sealed class DeskBay
{
    /// <summary>The bay's centre: metres right of (x) and away from the chair (y) from the mat's centre.</summary>
    public Vector2 at;

    /// <summary>The bay's footprint (metres across, metres deep): what the plan keeps for it (DeskArrangement.Problems).</summary>
    public Vector2 size;

    /// <summary>The turn from facing the chair (degrees; + turns its front toward the viewer's left).</summary>
    public float yaw;

    /// <summary>True when it stands on a back shelf (its riser's height above the desk's top).</summary>
    public bool onShelf;

    /// <summary>An empty bay (for the serializer).</summary>
    public DeskBay()
    {
    }

    /// <summary>A bay at <paramref name="x"/>, <paramref name="y"/>, <paramref name="width"/> by <paramref name="depth"/>, turned <paramref name="yaw"/>, on a back shelf or not.</summary>
    public DeskBay(float x, float y, float width, float depth, float yaw = 0f, bool onShelf = false)
    {
        at = new Vector2(x, y);
        size = new Vector2(width, depth);
        this.yaw = yaw;
        this.onShelf = onShelf;
    }

    /// <summary>The bay's rectangle in the desk's frame.</summary>
    public DeskRect Rect => new DeskRect(at.x, at.y, size.x, size.y);
}

/// <summary>A small decoration of the art's desk the player may move (the desk layout spec §3: "small decorations he can"): its id (its saved place's key) and the art's parts it is made of (every mesh whose name starts with one of the prefixes and "__").</summary>
[Serializable]
public sealed class DeskDecorItem
{
    /// <summary>The id its place is saved under (DeskDecorPreference).</summary>
    public string id;

    /// <summary>The art's part prefixes ("Clean_Calculator" takes every "Clean_Calculator__*" mesh).</summary>
    public string[] parts;

    /// <summary>The decor zone it starts in (an index into DeskLayoutSettings.decorZones).</summary>
    public int zone;

    /// <summary>An empty item (for the serializer).</summary>
    public DeskDecorItem()
    {
    }

    /// <summary>The decoration <paramref name="id"/> made of <paramref name="parts"/>, starting in zone <paramref name="zone"/>.</summary>
    public DeskDecorItem(string id, int zone, params string[] parts)
    {
        this.id = id;
        this.zone = zone;
        this.parts = parts;
    }
}

/// <summary>
/// The desk's plan (the desk layout spec, docs/superpowers/specs/2026-10-18-desk-layout-design.md;
/// Saleh's 1018a playtest: "every machine should have a space pre planed for
/// it ... reorgnize the desk its so messy. look at restory and learn from it";
/// plan v2, his "go with plan v2"): every bay in the desk's frame (DeskBay,
/// DeskFrame) rather than a point of the office view, so a new camera never
/// moves a machine. One paper mat in the middle; the paper machines at its
/// right edge, their beds reaching into the reading view (the scanner, the
/// chip reader); the fixed stamp rack beside the counter; the tool pouch under
/// the papers (the UV torch, the lens stand); the intercom on the counter;
/// two low back shelves (comms on the left with the phone, the BABEL rack, the
/// Rolodex and the protocol cards; the hall on the right with the RING SERVICE
/// panel); the four later machines' bays kept empty (hidden until their
/// machine comes); two decor zones the art's small props live in, the player
/// moving them there (DeskDecor). The office camera's new framing (58 degrees,
/// 5 further down: Saleh's "Yes") and the machine close-ups live here too.
/// Read at bind (OfficeSceneBinder); the builder checks the bays
/// (DeskArrangement.Problems). A section of DeskConfigSO.
/// </summary>
[Serializable]
public sealed class DeskLayoutSettings
{
    [Header("The office view (Saleh 2026-10-18: \"Yes\" to 58 degrees, 5 further down)")]
    /// <summary>The office camera's vertical field of view at 16:9 (degrees; 0 keeps the art's).</summary>
    [Range(0f, 90f)] public float officeFieldOfView = 58f;

    /// <summary>How much further down than the art's pose the office camera looks (degrees).</summary>
    [Range(-20f, 30f)] public float officeExtraPitch = 5f;

    [Header("Paper machines (their beds reach into the reading view's right edge)")]
    /// <summary>The scanner, turned so its front faces the mat (its bed's near end shows at the reading view's right edge).</summary>
    public DeskBay scanner = new DeskBay(0.74f, 0.13f, 0.56f, 0.36f, 90f);

    /// <summary>The chip reader under the scanner, its pad at the reading view's right edge.</summary>
    public DeskBay chipReader = new DeskBay(0.60f, -0.155f, 0.21f, 0.21f, 90f);

    /// <summary>The chip reader's size against its model's life size (smaller than the other machines: it lives in the reading view's corner).</summary>
    [Min(0.1f)] public float chipReaderScale = 0.9f;

    [Header("Verdict and tools")]
    /// <summary>The fixed stamp rack (the daters, DENIED and APPROVED), beside the counter at the papers' top right: stamp, then a short drag up hands the papers back.</summary>
    public DeskBay stampRack = new DeskBay(0.28f, 0.24f, 0.32f, 0.12f);

    /// <summary>The intercom on the counter strip's right: a click opens the questions where the player is (no trip to the office view).</summary>
    public DeskBay intercom = new DeskBay(0.50f, 0.37f, 0.12f, 0.08f);

    /// <summary>The tool pouch under the papers, its UV torch's cradle (inside the reading view's bottom edge).</summary>
    public DeskBay uvTorch = new DeskBay(0.18f, -0.22f, 0.24f, 0.10f, -90f);

    /// <summary>The lens stand, above the torch and right of the rulebook, clear of both and inside the reading view (its magnifiers taken up over the papers).</summary>
    public DeskBay lensKit = new DeskBay(0.10f, -0.09f, 0.20f, 0.15f);

    /// <summary>The DETAIN button, beside the AVAILABLE sign (rare: one trip to the office view).</summary>
    public DeskBay detain = new DeskBay(-0.51f, 0.74f, 0.26f, 0.28f);

    /// <summary>Where the supply crate stands on a delivery morning: off the mat, at its front right.</summary>
    public DeskBay crate = new DeskBay(1.04f, -0.52f, 0.48f, 0.34f, 8f);

    [Header("The back shelves (low risers along the desk's back edge)")]
    /// <summary>The comms shelf at the back left.</summary>
    public DeskBay commsShelf = new DeskBay(-1.35f, 1.29f, 1.90f, 0.34f);

    /// <summary>The hall shelf at the back right.</summary>
    public DeskBay hallShelf = new DeskBay(1.57f, 1.21f, 1.58f, 0.50f);

    /// <summary>The shelves' riser height (metres): what stands on them stands this high.</summary>
    [Min(0f)] public float shelfHeight = 0.05f;

    /// <summary>The comms shelf's bays: the phone (the art's, moved here), the BABEL rack, the Rolodex, the protocol cards.</summary>
    public DeskBay phone = new DeskBay(-2.01f, 1.29f, 0.50f, 0.30f, 0f, true), babelRack = new DeskBay(-1.50f, 1.29f, 0.40f, 0.30f, 0f, true),
                   rolodex = new DeskBay(-1.10f, 1.29f, 0.28f, 0.30f, -16f, true), protocolRing = new DeskBay(-0.76f, 1.29f, 0.28f, 0.30f, 10f, true);

    /// <summary>The art's phone: the part prefix of its meshes (moved onto the comms shelf at bind).</summary>
    public string phoneParts = "Clean_Phone";

    /// <summary>The hall shelf's RING SERVICE panel.</summary>
    public DeskBay ringPanel = new DeskBay(1.08f, 1.21f, 0.56f, 0.45f, -20f, true);

    [Header("Later machines (the desk machines spec §8): bays kept empty, nothing shown until they come")]
    /// <summary>The transponder tuner's bay (beside the scanner), the thumbprint pad's (beside the chip reader), the booth camera's and the X-ray belt's (on the hall shelf).</summary>
    public DeskBay tuner = new DeskBay(1.22f, 0.06f, 0.36f, 0.20f), thumbprint = new DeskBay(1.10f, -0.12f, 0.40f, 0.12f),
                   boothCamera = new DeskBay(1.60f, 1.21f, 0.36f, 0.45f, 0f, true), xray = new DeskBay(2.09f, 1.21f, 0.50f, 0.45f, 0f, true);

    [Header("The plan's check")]
    /// <summary>The art desk's top in the frame (the builder's check that every bay lies on it: DeskArrangement.Problems).</summary>
    public DeskBay deskTop = new DeskBay(0f, 0.515f, 5.8f, 2.53f);

    [Header("Decor (the art's small props; the player moves them, DeskDecor)")]
    /// <summary>The zones decorations live in: the back left corner and the right, between the machines and the hall shelf.</summary>
    public DeskBay[] decorZones = { new DeskBay(-2.57f, 0.98f, 0.46f, 0.92f), new DeskBay(1.55f, 0.56f, 0.90f, 0.64f) };

    /// <summary>The decorations: the art's small props, each starting in its zone (settled there in this order, DeskArrangement.TrySettle).</summary>
    public DeskDecorItem[] decor =
    {
        new DeskDecorItem("form_sorter", 1, "Clean_FormSorter"),
        new DeskDecorItem("tray", 1, "Clean_Tray", "Clean_PaperBundle"),
        new DeskDecorItem("calculator", 0, "Clean_Calculator"),
        new DeskDecorItem("binder", 0, "Clean_Binder"),
        new DeskDecorItem("pen_pot", 0, "Clean_PenPot"),
        new DeskDecorItem("stapler", 0, "Clean_Stapler"),
        new DeskDecorItem("ink_pad", 0, "Clean_Inkpad"),
        new DeskDecorItem("old_stamp", 0, "Clean_Stamp"),
    };

    /// <summary>The grid a dropped decoration settles on (metres, DeskArrangement.TrySettle).</summary>
    [Min(0.005f)] public float decorStep = 0.02f;

    /// <summary>How high a dragged decoration lifts off the desk (metres).</summary>
    [Min(0f)] public float decorLift = 0.03f;

    [Header("Machine close-ups (Settings: Machine close-ups, on by default)")]
    /// <summary>The close-up's camera: how far from the machine's centre it stands (metres) and how far above the desk it looks from, as a share of that distance; its field of view.</summary>
    [Min(0.1f)] public float closeUpDistance = 0.75f;

    /// <inheritdoc cref="closeUpDistance"/>
    [Range(0.1f, 2f)] public float closeUpHeight = 0.8f;

    /// <inheritdoc cref="closeUpDistance"/>
    [Range(10f, 90f)] public float closeUpFieldOfView = 38f;

    /// <summary>Seconds of the push-in and of the way back (a cut under Reduced Motion).</summary>
    [Min(0f)] public float closeUpBlendSeconds = 0.35f;

    /// <summary>How long a close-up holds after a click on an upgrade's object (seconds; a machine's read or scan holds it while it works).</summary>
    [Min(0f)] public float closeUpHoldSeconds = 1.6f;

    /// <summary>Every bay the plan keeps, by name (the builder's check: on the desk, none overlapping; the back shelves hold their own bays, so they are left out).</summary>
    public IEnumerable<KeyValuePair<string, DeskRect>> Bays()
    {
        yield return Named("scanner", scanner);
        yield return Named("chipReader", chipReader);
        yield return Named("stampRack", stampRack);
        yield return Named("intercom", intercom);
        yield return Named("uvTorch", uvTorch);
        yield return Named("lensKit", lensKit);
        yield return Named("detain", detain);
        yield return Named("crate", crate);
        yield return Named("phone", phone);
        yield return Named("babelRack", babelRack);
        yield return Named("rolodex", rolodex);
        yield return Named("protocolRing", protocolRing);
        yield return Named("ringPanel", ringPanel);
        yield return Named("tuner", tuner);
        yield return Named("thumbprint", thumbprint);
        yield return Named("boothCamera", boothCamera);
        yield return Named("xray", xray);
        for (int i = 0; i < decorZones.Length; i++)
            yield return Named("decor" + i, decorZones[i]);
    }

    private static KeyValuePair<string, DeskRect> Named(string name, DeskBay bay) => new KeyValuePair<string, DeskRect>(name, bay.Rect);
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

    /// <summary>Puts <paramref name="target"/> on <paramref name="bay"/>: at its centre on the desk (or, on a shelf, <paramref name="shelfHeight"/> up), facing the chair turned by its yaw; false (it stays) without a target or a bay.</summary>
    public bool Place(Transform target, DeskBay bay, float shelfHeight)
    {
        if (target == null || bay == null)
            return false;
        target.SetPositionAndRotation(Point(bay.at, bay.onShelf ? shelfHeight : 0f), Facing(bay.yaw));
        return true;
    }
}
