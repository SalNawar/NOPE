using System;
using System.Collections.Generic;

/// <summary>What the office install does with one object of the painted desk (DeskPaintedMap.Objects).</summary>
public enum PaintedRole
{
    /// <summary>The desk itself (slab, back step, trays and tracks, rail, wall, mat, glass): it never moves.</summary>
    Shell,

    /// <summary>A fixed object on its home (the PC, the AVAILABLE sign, the bell, a pouch, a stand, a locked home's insert): it never moves.</summary>
    Fixed,

    /// <summary>A painted object that slides on its own (no gameplay object stands for it yet): the slide-in moves its home root's Slide child.</summary>
    Slider,

    /// <summary>The look of a gameplay object (the scanner, the chip reader, the intercom, the UV torch, DETAIN): it rides that object's slide and the object's own meshes hide.</summary>
    Reskin,

    /// <summary>Not shown yet: its owner's object stands there (STAMP's rack and daters) or it waits for its home (the folded rulebook waits for DESK-HOMES' pocket).</summary>
    Hidden,
}

/// <summary>One object of the painted desk's bake: its name in the FBX files, the home it belongs to and what the install does with it.</summary>
public sealed class PaintedObject
{
    /// <summary>The object's name in DeskShell.fbx, DeskObjects.fbx or Desk_PC.fbx.</summary>
    public readonly string name;

    /// <summary>The home it belongs to (DeskHomeMap's ids; a fixed art id when <see cref="fixedArt"/>); null for the shell and the lamps.</summary>
    public readonly string home;

    /// <summary>What the install does with it.</summary>
    public readonly PaintedRole role;

    /// <summary>True when its home is fixed art (the PC, the keyboard, the AVAILABLE sign): the plan keeps every home clear of it.</summary>
    public readonly bool fixedArt;

    /// <summary>The object <paramref name="name"/> of the home <paramref name="home"/>, used as <paramref name="role"/> says.</summary>
    public PaintedObject(string name, string home, PaintedRole role, bool fixedArt = false)
    {
        this.name = name;
        this.home = home;
        this.role = role;
        this.fixedArt = fixedArt;
    }
}

/// <summary>
/// The painted desk's map (desk v3, Saleh 2026-10-20 on mock_office.png:
/// "yes but they need to slide"; UNITY_PLAN 4 and 8): every object of the
/// bake (E:\unity\NOPE-tools\design\desk\pipeline\export\v2) with its home
/// and its role in the office, which way each machine's track slides toward
/// the mat (desk v3's layout.SLIDES: the scanner and the chip reader left,
/// the intercom and the BABEL tuner right), how a painted home is
/// made from desk v3's layout (Homes: the rectangles of layout.py, exported
/// to desk_layout_v3.json, and the heights the importer measures on the
/// meshes) and how the painted homes take over DESK-PLAN's map while the
/// painted desk shows (Merge: replaced by id, the classic homes desk v3 has
/// no room for dropped or moved). Pure.
/// </summary>
public static class DeskPaintedMap
{
    /// <summary>The fixed art ids of the painted PC's keyboard and mouse (DeskHomeMap.Pc is the CRT, DeskHomeMap.Sign the AVAILABLE sign).</summary>
    public const string Keyboard = "keyboard", PcMouse = "pcMouse";

    /// <summary>The bake's shell, mat and glass objects (DeskShell.fbx).</summary>
    public const string Shell = "DeskShell", Mat = "DeskMat", Glass = "DeskGlass";

    /// <summary>The bake's lamp bezel (one mesh, placed once per lamp of anchors.json).</summary>
    public const string LampBezel = "Desk_LampBezel";

    /// <summary>The bake's hinged and swaying children: the scanner's lid, the intercom's mic, the pocket's flap (each named under its parent).</summary>
    public const string Lid = "Lid", Mic = "Mic", Flap = "Flap";

    /// <summary>The painted scanner's lid as the install renames it, so nothing that looks for the gameplay scanner's own "Lid" finds the painted one.</summary>
    public const string PaintedLid = "PaintedLid";

    /// <summary>Every object of the bake (but the lamps), its home and its role.</summary>
    public static readonly PaintedObject[] Objects =
    {
        new PaintedObject(Shell, null, PaintedRole.Shell),
        new PaintedObject(Mat, null, PaintedRole.Shell),
        new PaintedObject(Glass, null, PaintedRole.Hidden), // its sheen's alpha holds the painting's edges, the traveller's outline too (up to 40%): it would ghost over every traveller (art request)
        new PaintedObject("Desk_Scanner", DeskHomeMap.Scanner, PaintedRole.Reskin),
        new PaintedObject("Desk_ChipReader", DeskHomeMap.ChipReader, PaintedRole.Reskin),
        new PaintedObject("Desk_Intercom", DeskHomeMap.Intercom, PaintedRole.Reskin),
        new PaintedObject("Desk_UvTorch", DeskHomeMap.UvTorch, PaintedRole.Reskin),
        new PaintedObject("Desk_UvPouch", DeskHomeMap.UvTorch, PaintedRole.Fixed),
        new PaintedObject("Desk_Glasses", DeskHomeMap.Glasses, PaintedRole.Slider),
        new PaintedObject("Desk_GlassesStand", DeskHomeMap.Glasses, PaintedRole.Fixed),
        new PaintedObject("Desk_Pocket", DeskHomeMap.RulebookPocket, PaintedRole.Fixed),
        new PaintedObject("Desk_RulebookFolded", DeskHomeMap.RulebookPocket, PaintedRole.Hidden),
        new PaintedObject("Desk_StampRack", DeskHomeMap.StampRack, PaintedRole.Hidden),
        new PaintedObject("Desk_StampApprove", DeskHomeMap.StampRack, PaintedRole.Hidden),
        new PaintedObject("Desk_StampDeny", DeskHomeMap.StampRack, PaintedRole.Hidden),
        new PaintedObject("Desk_Detain", DeskHomeMap.Detain, PaintedRole.Reskin),
        new PaintedObject("Desk_DetainGuard", DeskHomeMap.Detain, PaintedRole.Reskin),
        new PaintedObject("Desk_Bell", DeskHomeMap.Bell, PaintedRole.Fixed),
        new PaintedObject("Desk_BabelHome", DeskHomeMap.BabelTuner, PaintedRole.Fixed),
        new PaintedObject("Desk_RolodexHome", DeskHomeMap.Rolodex, PaintedRole.Fixed),
        new PaintedObject("Desk_CardsHome", DeskHomeMap.ProtocolRing, PaintedRole.Fixed),
        new PaintedObject("Desk_AvailableSign", DeskHomeMap.Sign, PaintedRole.Fixed, true),
        new PaintedObject("PC_Crt", DeskHomeMap.Pc, PaintedRole.Fixed, true),
        new PaintedObject("PC_Keyboard", Keyboard, PaintedRole.Fixed, true),
        new PaintedObject("PC_Mouse", PcMouse, PaintedRole.Fixed, true),
    };

    /// <summary>The homes whose painted object is a locked home's insert (the owned object stands in it, on its top): the BABEL tuner, the Rolodex and the protocol cards.</summary>
    public static readonly string[] Inserts = { DeskHomeMap.BabelTuner, DeskHomeMap.Rolodex, DeskHomeMap.ProtocolRing };

    /// <summary>The painted homes that show from day 1 whatever DESK-PLAN's map says (their painted object is on the desk: the glasses in their stand, the bell, the rulebook's pocket).</summary>
    private static readonly string[] Shown = { DeskHomeMap.Glasses, DeskHomeMap.Bell, DeskHomeMap.RulebookPocket };

    /// <summary>The homes the painted desk takes from its layout though no painted object stands for them: the deal tray (part of the painted shell, under the glass; the papers come in over it).</summary>
    public static readonly string[] ShellHomes = { DeskHomeMap.DealTray };

    /// <summary>
    /// DESK-PLAN's homes the painted desk leaves out: the art's mouse (cut;
    /// the painted PC island has its own mouse, fixed art) and the later
    /// thumbprint reader (positions only, not in days 1-15), whose home beside
    /// the rail lies on desk v3's chip reader tray (DESK-PLAN re-homes it when
    /// it comes).
    /// </summary>
    public static readonly string[] Dropped = { DeskHomeMap.Mouse, DeskHomeMap.Thumbprint };

    /// <summary>DESK-PLAN's homes the painted desk moves (metres in the desk's frame): the supply crate's spot 3 cm right, its click box clear of desk v3's scanner tray (its right edge at x 1.30).</summary>
    public static readonly (string id, float dx, float dy)[] Moved = { (DeskHomeMap.Crate, 0.03f, 0f) };

    /// <summary>The id desk v3's layout (layout.HOMES, desk_layout_v3.json) gives the home <paramref name="home"/>: the AVAILABLE sign is its "available".</summary>
    public static string LayoutId(string home) => home == DeskHomeMap.Sign ? "available" : home;

    /// <summary>The object named <paramref name="name"/>, or null.</summary>
    public static PaintedObject Find(string name)
    {
        foreach (PaintedObject o in Objects)
            if (o.name == name)
                return o;
        return null;
    }

    /// <summary>
    /// Which way the track on the painted home <paramref name="home"/> slides
    /// toward the mat (desk v3, layout.SLIDES: the rail's scanner and chip
    /// reader to the chair's left, the intercom and the BABEL tuner on the
    /// left of the mat to its right); None for every other home.
    /// </summary>
    public static HomeSlide SlideOf(string home)
    {
        switch (home)
        {
            case DeskHomeMap.Scanner:
            case DeskHomeMap.ChipReader:
                return HomeSlide.Left;
            case DeskHomeMap.Intercom:
            case DeskHomeMap.BabelTuner:
                return HomeSlide.Right;
            default:
                return HomeSlide.None;
        }
    }

    /// <summary>When the painted home <paramref name="home"/>'s object shows: <paramref name="classic"/>'s rule (DESK-PLAN's map), but a home whose painted object stands on the desk from day 1 always shows; Always without a classic home.</summary>
    public static HomeShow ShowOf(string home, DeskHome classic)
    {
        if (Array.IndexOf(Shown, home) >= 0 || classic == null)
            return HomeShow.Always;
        return classic.show;
    }

    /// <summary>
    /// The painted home <paramref name="id"/> over the desk rectangle
    /// (<paramref name="x0"/>, <paramref name="y0"/>)-(<paramref name="x1"/>,
    /// <paramref name="y1"/>) in the desk's frame, standing
    /// <paramref name="height"/> over the desk: turned as
    /// <paramref name="classic"/> turns its object (the gameplay object laid
    /// there keeps its design: STAMP's rack its daters front to back, the UV
    /// torch across its pouch; a quarter turn swaps its own width and depth,
    /// so it covers the same rectangle; any other turn is dropped), its show
    /// rule from <paramref name="classic"/> (ShowOf), its slide SlideOf's, no
    /// art parts moved onto it.
    /// </summary>
    public static DeskHome Home(string id, float x0, float y0, float x1, float y1, float height, DeskHome classic)
    {
        HomeSlide slide = SlideOf(id);
        float yaw = classic != null ? classic.yaw : 0f;
        bool quarter = Math.Abs(Math.Abs(yaw) - 90f) < 0.01f;
        if (!quarter && Math.Abs(yaw) > 0.01f)
            yaw = 0f;
        float w = x1 - x0, d = y1 - y0;
        return new DeskHome(id, (x0 + x1) / 2f, (y0 + y1) / 2f, quarter ? d : w, quarter ? w : d, yaw, height, ShowOf(id, classic), slide);
    }

    /// <summary>
    /// Every painted home and fixed art (the painted desk's map before the
    /// merge): for each home of Objects and ShellHomes, its rectangle
    /// <paramref name="rectOf"/> gives (desk v3's layout; null leaves the
    /// home out), standing <paramref name="heightOf"/> over the desk, made by
    /// Home over <paramref name="classic"/>'s rule; the fixed art apart.
    /// </summary>
    public static (DeskHome[] homes, DeskHome[] fixedArt) Homes(Func<string, (float x0, float y0, float x1, float y1)?> rectOf, Func<string, float> heightOf, IReadOnlyList<DeskHome> classic)
    {
        var homes = new List<DeskHome>();
        var fixedArt = new List<DeskHome>();
        var ids = new List<(string id, bool fixedArt)>();
        foreach (PaintedObject o in Objects)
            if (o.home != null && !ids.Exists(i => i.id == o.home))
                ids.Add((o.home, o.fixedArt));
        foreach (string id in ShellHomes)
            ids.Add((id, false));
        foreach ((string id, bool isFixed) in ids)
        {
            (float x0, float y0, float x1, float y1)? rect = rectOf(id);
            if (rect == null)
                continue;
            (float x0, float y0, float x1, float y1) r = rect.Value;
            DeskHome home = Home(id, r.x0, r.y0, r.x1, r.y1, heightOf(id), DeskHomeMap.Find(classic, id));
            (isFixed ? fixedArt : homes).Add(home);
        }
        return (homes.ToArray(), fixedArt.ToArray());
    }

    /// <summary>
    /// The map while the painted desk shows: every home of
    /// <paramref name="classic"/> (DESK-PLAN's map) but the Dropped ones,
    /// each replaced by the painted home of its id in
    /// <paramref name="painted"/> or else moved as Moved says, in the
    /// classic order, then the painted homes the classic map lacks. A fresh
    /// array.
    /// </summary>
    public static DeskHome[] Merge(IReadOnlyList<DeskHome> classic, IReadOnlyList<DeskHome> painted)
    {
        var merged = new List<DeskHome>();
        var used = new HashSet<string>();
        foreach (DeskHome home in classic ?? Array.Empty<DeskHome>())
        {
            if (home == null || Array.IndexOf(Dropped, home.id) >= 0)
                continue;
            DeskHome over = DeskHomeMap.Find(painted, home.id);
            merged.Add(over ?? MovedHome(home));
            used.Add(home.id);
        }
        foreach (DeskHome home in painted ?? Array.Empty<DeskHome>())
            if (home != null && used.Add(home.id))
                merged.Add(home);
        return merged.ToArray();
    }

    /// <summary>The classic <paramref name="home"/> as the painted desk places it: moved when Moved names it (a copy), else itself.</summary>
    private static DeskHome MovedHome(DeskHome home)
    {
        foreach ((string id, float dx, float dy) in Moved)
            if (id == home.id)
                return new DeskHome(home.id, home.x + dx, home.y + dy, home.width, home.depth, home.yaw, home.height, home.show, home.slide, home.artParts);
        return home;
    }
}
