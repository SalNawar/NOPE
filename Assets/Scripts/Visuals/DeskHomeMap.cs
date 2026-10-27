using System;
using System.Collections.Generic;

/// <summary>When a home's object shows on the desk (the desk home map, DESK-PLAN; Saleh's D2: "a place for everything even if it is not unlocked yet").</summary>
public enum HomeShow
{
    /// <summary>From day 1 (the stamp rack, DETAIN, the intercom, the deal tray, the mouse).</summary>
    Always,

    /// <summary>While its machine or upgrade is issued or owned (the scanner, the chip reader, the UV torch, the Rolodex, the protocol cards, the BABEL rack, the RING SERVICE panel).</summary>
    WhenOwned,

    /// <summary>A Phase B object's home (the bell, the glasses, the rulebook's pocket): its position only, nothing shown until its track lands (FIX_PLAN_v2's DESK-PLAN delta; DESK-HOMES designs the empty spot).</summary>
    Reserved,

    /// <summary>A later machine (design only, not in days 1-15): its position kept, nothing shown.</summary>
    Later,

    /// <summary>The supply crate's spot: only on a delivery morning.</summary>
    Delivery,

    /// <summary>The decorations' start zone (they rest wherever the player lets them go off the mat).</summary>
    Decor,
}

/// <summary>Which way a machine slides in when the cursor comes near it (Saleh's D2: "the machine will slide in a little bit"): sideways toward the mat, never onto it.</summary>
public enum HomeSlide
{
    /// <summary>It never slides.</summary>
    None,

    /// <summary>Toward the chair's left (-x): a machine right of the mat.</summary>
    Left,

    /// <summary>Toward the chair's right (+x): a machine left of the mat.</summary>
    Right,
}

/// <summary>
/// One object's home on the desk (the desk home map): where it stands in the
/// desk's frame (metres right of and away from the chair from the mat's
/// centre), its own footprint (across its front, front to back) and its turn
/// from facing the chair, its height (what the office camera must see over,
/// what a carried thing rises over), when it shows, which way it slides in
/// and, for an art object the plan moves, the prefix of its parts. Pure and
/// serializable (a row of DeskLayoutSettings.homes).
/// </summary>
[Serializable]
public sealed class DeskHome
{
    /// <summary>The home's id (DeskHomeMap's constants; Phase B tracks bind by it, DeskLayout.HomeOf).</summary>
    public string id = string.Empty;

    /// <summary>The centre: metres right of (x) and away from the chair (y) from the mat's centre.</summary>
    public float x, y;

    /// <summary>The object's own footprint: metres across its front and front to back (before its turn).</summary>
    public float width, depth;

    /// <summary>The turn from facing the chair (degrees; + turns its front toward the viewer's left).</summary>
    public float yaw;

    /// <summary>How tall the object stands over the desk (metres): the plan's sight check and the obstacles a carried thing rises over.</summary>
    public float height;

    /// <summary>When the object shows.</summary>
    public HomeShow show;

    /// <summary>Which way it slides in (DeskHomeMap.SlideReach at most).</summary>
    public HomeSlide slide;

    /// <summary>The art's part prefix moved onto this home at bind ("Clean_Mouse" takes every "Clean_Mouse__*" mesh); empty for an object the gameplay builds.</summary>
    public string artParts = string.Empty;

    /// <summary>An empty home (for the serializer).</summary>
    public DeskHome()
    {
    }

    /// <summary>The home <paramref name="id"/> centred at (<paramref name="x"/>, <paramref name="y"/>) for an object <paramref name="width"/> by <paramref name="depth"/>, turned <paramref name="yaw"/>, <paramref name="height"/> tall.</summary>
    public DeskHome(string id, float x, float y, float width, float depth, float yaw, float height, HomeShow show, HomeSlide slide = HomeSlide.None, string artParts = "")
    {
        this.id = id;
        this.x = x;
        this.y = y;
        this.width = width;
        this.depth = depth;
        this.yaw = yaw;
        this.height = height;
        this.show = show;
        this.slide = slide;
        this.artParts = artParts ?? string.Empty;
    }

    /// <summary>The rectangle the turned object covers on the desk (yaw-aware).</summary>
    public DeskRect Footprint => DeskArrangement.Footprint(x, y, width, depth, yaw);

    /// <summary>The rectangle it covers while sliding in by up to <paramref name="reach"/> metres (its footprint alone when it never slides).</summary>
    public DeskRect Swept(float reach)
    {
        DeskRect f = Footprint;
        float dx = slide == HomeSlide.Left ? -reach : slide == HomeSlide.Right ? reach : 0f;
        return new DeskRect(f.CentreX + dx / 2f, f.CentreY, f.Width + Math.Abs(dx), f.Height);
    }

    /// <summary>True when the object shows in Phase A at some point (Always, WhenOwned, Delivery): the plan's sight check covers it.</summary>
    public bool Shown => show == HomeShow.Always || show == HomeShow.WhenOwned || show == HomeShow.Delivery;

    /// <summary>The C lamp's point (Saleh's "Mix: B + A rail + C lamps": the next tool lights up): the middle of the footprint's front edge, metres in the frame.</summary>
    public (float x, float y) LampPoint
    {
        get
        {
            DeskRect f = Footprint;
            return (f.CentreX, f.CentreY - f.Height / 2f);
        }
    }
}

/// <summary>
/// The desk home map Saleh approved on 2026-10-20 ("DESK APPROVED": "Yes,
/// build it"; "Mix: B + A rail + C lamps"; "keep a big mat size"; the stamp
/// rack's "daters front-to-back"; the props cut: "we will cut all but decor
/// objects later will go on clear places like restory"). The big art mat
/// (1.46 x 1.03 m) holds papers only. The traveller's papers come in and go
/// back over the DEAL TRAY at the counter. DETAIN, the AVAILABLE sign and the
/// bell sit on the counter row; the Rolodex, the glasses and the protocol
/// cards on its left. Left of the mat: the intercom, the BABEL tuner, the
/// rulebook's pocket and the UV torch's pouch, beside the PC. Right of the
/// mat: A's machine rail in the order of use, front to back (the chip
/// reader, the scanner, the stamp rack with its two daters standing front to
/// back), ending at the deal tray; the machines slide in sideways toward the
/// mat. Every later or Phase B object already has its home (positions only).
/// The positions are ours (proposal, from the approved plan
/// desk_simple_1.png and concept B); a change is a data change. Pure: the
/// builder and the tests check it (DeskArrangement.HomeProblems).
/// </summary>
public static class DeskHomeMap
{
    /// <summary>The homes' ids.</summary>
    public const string StampRack = "stampRack", Scanner = "scanner", ChipReader = "chipReader", DealTray = "dealTray", Bell = "bell", Detain = "detain",
                        Intercom = "intercom", BabelTuner = "babelTuner", RulebookPocket = "rulebookPocket", UvTorch = "uvTorch",
                        Rolodex = "rolodex", Glasses = "glasses", ProtocolRing = "protocolRing", Switchboard = "switchboard",
                        Thumbprint = "thumbprint", TransponderTuner = "transponderTuner", BoothCamera = "boothCamera", Xray = "xray",
                        Crate = "crate", Decor = "decor", Mouse = "mouse";

    /// <summary>The fixed art the plan keeps clear of (never moved): the PC island, the till, the AVAILABLE sign and the lamp.</summary>
    public const string Pc = "pc", Till = "till", Sign = "sign", Lamp = "lamp";

    /// <summary>How far a machine slides in (metres; the Game Feel Bible's C6: "≤6 cm").</summary>
    public const float SlideReach = 0.06f;

    /// <summary>How far a built object's click box reaches past its mesh on every side (metres; the builder's PropClick): the plan keeps every two shown objects' boxes apart with it (DeskArrangement.ClickBoxProblems, BUG-4a).</summary>
    public const float ClickPad = 0.01f;

    /// <summary>The shown homes whose object takes no click (the deal tray's plate, the art's mouse): no click box to keep apart.</summary>
    public static readonly string[] NoClickBox = { DealTray, Mouse };

    /// <summary>A's machine rail in the order of use, front (by the chair) to back (by the counter and its deal tray).</summary>
    public static readonly string[] Rail = { ChipReader, Scanner, StampRack };

    /// <summary>The art's paper mat in the frame (Saleh: "keep a big mat size", 1.46 x 1.03 m): papers and the open rulebook only.</summary>
    public static DeskRect Mat => new DeskRect(0f, 0f, 1.46f, 1.03f);

    /// <summary>The art desk's top in the frame.</summary>
    public static DeskRect DeskTop => new DeskRect(0f, 0.515f, 5.8f, 2.53f);

    /// <summary>The office camera's eye in the frame for the plan's sight check: metres right, metres from the mat's centre toward the traveller (negative: behind the chair), metres over the desk (the office view: 58 degrees, 9 down; R7 LAY's measured camera).</summary>
    public static (float x, float y, float h) OfficeEye => (0f, -2.10f, 1.09f);

    /// <summary>The approved homes (a fresh copy each call: DeskLayoutSettings' default).</summary>
    public static DeskHome[] Approved() => new[]
    {
        // The machine rail, right of the mat, in the order of use (chip reader, scanner, stamp rack by the counter).
        new DeskHome(ChipReader, 0.895f, -0.375f, 0.21f, 0.21f, 0f, 0.07f, HomeShow.WhenOwned, HomeSlide.Left),
        new DeskHome(Scanner, 1.10f, -0.07f, 0.46f, 0.34f, 0f, 0.13f, HomeShow.WhenOwned, HomeSlide.Left), // 8 cm right of the chip reader: the Auto-Feed tray stands 13 cm out of the scanner's left side, and from 1.02 it lay 6 cm on the mat
        new DeskHome(StampRack, 0.83f, 0.35f, 0.32f, 0.12f, 90f, 0.08f, HomeShow.Always),
        // The counter row: the deal tray over the mat, DETAIN left of the AVAILABLE sign, the bell right of it.
        new DeskHome(DealTray, 0f, 0.545f, 0.44f, 0.06f, 0f, 0.01f, HomeShow.Always),
        new DeskHome(Detain, -0.51f, 0.74f, 0.26f, 0.28f, 0f, 0.09f, HomeShow.Always),
        new DeskHome(Bell, 0.43f, 0.67f, 0.14f, 0.14f, 0f, 0.08f, HomeShow.Reserved),
        new DeskHome(Rolodex, 0.91f, 0.71f, 0.16f, 0.16f, 0f, 0.17f, HomeShow.WhenOwned), // right of the bell, behind the stamp rack: left, behind the intercom, the office camera saw 60% of it (DESK-PLAN recipe 4's fallback)
        new DeskHome(Glasses, -0.73f, 0.575f, 0.14f, 0.09f, 0f, 0.03f, HomeShow.Reserved),
        new DeskHome(ProtocolRing, -0.74f, 0.74f, 0.12f, 0.12f, 0f, 0.21f, HomeShow.WhenOwned),
        // Left of the mat, beside the PC: ask and look up. The intercom is the art's phone moved here, which waits for Saleh's OK (he said "cut the phone"; docs/pending/DESK-PLAN.md, open call 1).
        new DeskHome(Intercom, -0.88f, 0.40f, 0.24f, 0.20f, 0f, 0.12f, HomeShow.Always, HomeSlide.None, "Clean_Phone"),
        new DeskHome(BabelTuner, -0.90f, 0.12f, 0.18f, 0.20f, 0f, 0.15f, HomeShow.WhenOwned, HomeSlide.Right),
        new DeskHome(RulebookPocket, -0.88f, -0.12f, 0.24f, 0.26f, 0f, 0.03f, HomeShow.Reserved),
        new DeskHome(UvTorch, -0.84f, -0.44f, 0.24f, 0.12f, -90f, 0.06f, HomeShow.WhenOwned),
        new DeskHome(Mouse, -1.145f, -0.58f, 0.19f, 0.28f, 0f, 0.04f, HomeShow.Always, HomeSlide.None, "Clean_Mouse"),
        // The later machines beside the rail and at the back right (positions only).
        new DeskHome(Thumbprint, 1.16f, -0.375f, 0.20f, 0.21f, 0f, 0.05f, HomeShow.Later),
        new DeskHome(TransponderTuner, 1.45f, -0.43f, 0.28f, 0.26f, 0f, 0.10f, HomeShow.Later),
        new DeskHome(BoothCamera, 1.08f, 1.225f, 0.36f, 0.45f, 0f, 0.30f, HomeShow.Later),
        new DeskHome(Xray, 1.57f, 1.225f, 0.50f, 0.45f, 0f, 0.30f, HomeShow.Later),
        // The booth's right end: the portals' switchboard (Saleh: "on the booth wall, right of the desk"); the RING SERVICE panel stands there in Phase A.
        new DeskHome(Switchboard, 2.22f, 1.30f, 0.56f, 0.45f, -20f, 0.40f, HomeShow.WhenOwned),
        // Deliveries and decorations, right of the rail. The decor zone stands left of the office camera's line to the switchboard's home (the form sorter, 36 cm tall, hid 80% of the RING SERVICE panel from 1.80, 0.55), deep enough for the sorter behind the tray, and clear of the crate.
        new DeskHome(Crate, 1.64f, 0f, 0.48f, 0.34f, 8f, 0.30f, HomeShow.Delivery),
        new DeskHome(Decor, 1.28f, 0.545f, 0.56f, 0.65f, 0f, 0f, HomeShow.Decor),
    };

    /// <summary>The fixed art's footprints and heights in the frame (measured on the day-12 top-down capture, R7 AUDIT DESK; rounded out), as homes the plan keeps clear of.</summary>
    public static DeskHome[] FixedArt() => new[]
    {
        new DeskHome(Pc, -1.57f, 0.26f, 1.10f, 1.16f, 0f, 0.62f, HomeShow.Always),
        new DeskHome(Till, -1.085f, 0.90f, 0.49f, 0.42f, 0f, 0.30f, HomeShow.Always),
        new DeskHome(Sign, 0f, 0.685f, 0.56f, 0.21f, 0f, 0.23f, HomeShow.Always),
        new DeskHome(Lamp, 0.46f, 1.25f, 0.52f, 0.50f, 0f, 0.50f, HomeShow.Always),
    };

    /// <summary>The home <paramref name="id"/> in <paramref name="homes"/>, or null.</summary>
    public static DeskHome Find(IReadOnlyList<DeskHome> homes, string id)
    {
        if (homes == null || string.IsNullOrEmpty(id))
            return null;
        foreach (DeskHome home in homes)
            if (home != null && home.id == id)
                return home;
        return null;
    }
}
