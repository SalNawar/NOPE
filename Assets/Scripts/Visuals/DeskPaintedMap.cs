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

    /// <summary>The look of a gameplay object (the scanner, the chip reader, the intercom, the UV torch, DETAIN, STAMP's rack and its two daters): it rides that object's slide and the object's own meshes hide.</summary>
    Reskin,

    /// <summary>Not shown: the glass's sheen (its alpha holds the painted traveller) and the locked homes' inserts (the shell's plate already carries those homes as painted: drawn too, they doubled; until DESK-ART bakes the shell from the empty plate under them).</summary>
    Hidden,
}

/// <summary>How the painted desk shows one part of a gameplay object that keeps its own moving parts (DeskPaintedMap.LookOf; DeskPainted.WearBoxes).</summary>
public enum PartLook
{
    /// <summary>Drawn as it is (a live label: the crate's tag with what came).</summary>
    Keep,

    /// <summary>A painting-matched box over the part's own mesh bounds, riding the part (its flaps still open), the old mesh hidden.</summary>
    Box,

    /// <summary>Hidden, with no box (a part a box cannot draw, or none the painting would show).</summary>
    Hide,
}

/// <summary>Where a stand-in stands (DeskPaintedMap.StandIns).</summary>
public enum StandInAnchor
{
    /// <summary>On its home's floor (DeskPainted.TryFloor), at the home's centre.</summary>
    Home,

    /// <summary>On the front face (toward the chair) of its home's painted object, from that object's base (the scanner's add-ons ride the scanner).</summary>
    Front,
}

/// <summary>One box of a stand-in: its part's name, its centre across and along its anchor (metres: x to the right, y away from the chair), its base over the anchor, its size and its colour (sRGB, painting-matched).</summary>
public readonly struct StandInBox
{
    public readonly string name;
    public readonly float x, y, z, width, depth, height, r, g, b;

    /// <summary>The box <paramref name="name"/> of <paramref name="size"/> (width, depth, height) centred at (<paramref name="x"/>, <paramref name="y"/>) from its anchor, its base <paramref name="z"/> over it, in <paramref name="colour"/>.</summary>
    public StandInBox(string name, float x, float y, float z, (float w, float d, float h) size, (float r, float g, float b) colour)
    {
        this.name = name;
        this.x = x;
        this.y = y;
        this.z = z;
        width = size.w;
        depth = size.d;
        height = size.h;
        r = colour.r;
        g = colour.g;
        b = colour.b;
    }

    /// <summary>Its top over its anchor.</summary>
    public float Top => z + height;
}

/// <summary>
/// A neutral, painting-matched stand-in for a gameplay object the painted
/// desk has no painted model of yet (Saleh: a clean, organised desk where
/// every object matches the painting; never the old model): a few plain
/// boxes in the painting's palette, lit by the painted desk's own shader,
/// sized to the painted slot it stands in, until DESK-ART's painted model
/// (its art request, DESK-V3.md) replaces it.
/// </summary>
public sealed class StandIn
{
    /// <summary>What it stands for: the gameplay object's home id, or an add-on's id (DeskPaintedMap.AutoFeed, AnalysisBeacon).</summary>
    public readonly string id;

    /// <summary>The home it stands on (DeskHomeMap's ids).</summary>
    public readonly string home;

    /// <summary>Where it stands on that home.</summary>
    public readonly StandInAnchor anchor;

    /// <summary>How tall the painted slot lets it stand (metres over its floor; 0: no cap): the BABEL slats 2.5 cm, the Rolodex's and the protocol cards' homes 6 cm.</summary>
    public readonly float maxHeight;

    /// <summary>Its boxes.</summary>
    public readonly StandInBox[] boxes;

    /// <summary>
    /// Where the painting draws its home's slot from the home's centre in
    /// desk v3's layout (metres: x to the right, y away from the chair, at its
    /// floor) and how far below the floor DeskPainted.TryFloor gives (the
    /// locked insert's top) the slot's own surface lies: the shell carries the
    /// slots as painted, and seen from the office camera the Rolodex's oval
    /// and the cards' square sit 6 to 7 cm right of the layout's homes at the
    /// inserts' top, the BABEL slats on the shell itself (3.1 cm under its
    /// insert's top). Measured on the bake (DESK-V3.md, round 5). Home anchors only.
    /// </summary>
    public readonly float offsetX, offsetY, floorDrop;

    /// <summary>The stand-in for <paramref name="id"/> on <paramref name="home"/>.</summary>
    public StandIn(string id, string home, StandInAnchor anchor, float maxHeight, params StandInBox[] boxes)
        : this(id, home, anchor, maxHeight, (0f, 0f, 0f), boxes)
    {
    }

    /// <summary>The stand-in for <paramref name="id"/> on <paramref name="home"/>, in its painted <paramref name="slot"/> (offsetX, offsetY, floorDrop).</summary>
    public StandIn(string id, string home, StandInAnchor anchor, float maxHeight, (float x, float y, float drop) slot, params StandInBox[] boxes)
    {
        this.id = id;
        this.home = home;
        this.anchor = anchor;
        this.maxHeight = maxHeight;
        offsetX = slot.x;
        offsetY = slot.y;
        floorDrop = slot.drop;
        this.boxes = boxes ?? Array.Empty<StandInBox>();
    }

    /// <summary>Its top over its anchor (metres).</summary>
    public float Top
    {
        get
        {
            float top = 0f;
            foreach (StandInBox b in boxes)
                top = Math.Max(top, b.Top);
            return top;
        }
    }

    /// <summary>The rectangle its boxes cover across and along its anchor (metres).</summary>
    public (float x0, float y0, float x1, float y1) Extent
    {
        get
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (StandInBox b in boxes)
            {
                x0 = Math.Min(x0, b.x - b.width / 2f);
                x1 = Math.Max(x1, b.x + b.width / 2f);
                y0 = Math.Min(y0, b.y - b.depth / 2f);
                y1 = Math.Max(y1, b.y + b.depth / 2f);
            }
            return boxes.Length > 0 ? (x0, y0, x1, y1) : (0f, 0f, 0f, 0f);
        }
    }
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

    /// <summary>The bake's stamp rack, its two daters and the folded rulebook.</summary>
    public const string StampRack = "Desk_StampRack", StampApprove = "Desk_StampApprove", StampDeny = "Desk_StampDeny", RulebookFolded = "Desk_RulebookFolded";

    /// <summary>The scanner's two upgrades' add-ons (ScannerAddOns: the Auto-Feed tray, the Analysis beacon), as their stand-ins name them.</summary>
    public const string AutoFeed = "autoFeed", AnalysisBeacon = "analysisBeacon";

    /// <summary>The painting's palette for the stand-ins (sRGB, sampled from mock_office.png): the machines' cream, the trays' brass, the desk's dark wood, the felt's green, the dials' ink, the lamps' amber and the oxblood of the red accents.</summary>
    public static readonly (float r, float g, float b) Cream = (0.80f, 0.73f, 0.58f), Brass = (0.74f, 0.58f, 0.30f), Wood = (0.27f, 0.15f, 0.08f),
                                                       Felt = (0.19f, 0.33f, 0.21f), Ink = (0.12f, 0.09f, 0.07f), Amber = (0.95f, 0.64f, 0.24f), Oxblood = (0.55f, 0.18f, 0.15f);

    /// <summary>
    /// The neutral, painting-matched stand-ins of the gameplay objects the
    /// bake has no painted model of yet (each an art request in DESK-V3.md):
    /// the BABEL rack as a low cream tuner inside its slats' slot (31 x 15
    /// cm, at most 2.5 cm tall; BABEL-TUNER's own object replaces it), the
    /// Rolodex as a card file and the protocol cards as a card stack on their
    /// back-step homes (under 6 cm), and the scanner's Auto-Feed tray and
    /// Analysis beacon as two small plates on the painted scanner's front
    /// face (they ride its slide). Their old models never show.
    /// </summary>
    public static readonly StandIn[] StandIns =
    {
        new StandIn(DeskHomeMap.BabelTuner, DeskHomeMap.BabelTuner, StandInAnchor.Home, 0.025f, (0f, 0f, 0.031f),
                    new StandInBox("Body", 0f, 0f, 0f, (0.25f, 0.11f, 0.016f), Cream),
                    new StandInBox("Dial", -0.04f, -0.03f, 0.016f, (0.12f, 0.035f, 0.004f), Ink),
                    new StandInBox("Knob", 0.085f, -0.025f, 0.016f, (0.03f, 0.03f, 0.007f), Brass)),
        new StandIn(DeskHomeMap.Rolodex, DeskHomeMap.Rolodex, StandInAnchor.Home, 0.06f, (0.064f, -0.007f, 0f),
                    new StandInBox("Base", 0f, 0f, 0f, (0.15f, 0.10f, 0.008f), Brass),
                    new StandInBox("Cards", 0f, 0f, 0.008f, (0.12f, 0.07f, 0.038f), Cream),
                    new StandInBox("Band", 0f, 0f, 0.046f, (0.124f, 0.006f, 0.004f), Oxblood)),
        new StandIn(DeskHomeMap.ProtocolRing, DeskHomeMap.ProtocolRing, StandInAnchor.Home, 0.06f, (0.07f, -0.026f, 0f),
                    new StandInBox("Base", 0f, 0f, 0f, (0.10f, 0.15f, 0.008f), Wood),
                    new StandInBox("Cards", 0f, 0f, 0.008f, (0.08f, 0.12f, 0.024f), Cream),
                    new StandInBox("Ring", 0f, 0.04f, 0.032f, (0.086f, 0.012f, 0.006f), Brass)),
        new StandIn(AutoFeed, DeskHomeMap.Scanner, StandInAnchor.Front, 0f,
                    new StandInBox("Plate", -0.09f, -0.006f, 0.05f, (0.12f, 0.012f, 0.03f), Cream),
                    new StandInBox("Slot", -0.09f, -0.013f, 0.062f, (0.10f, 0.002f, 0.005f), Ink)),
        new StandIn(AnalysisBeacon, DeskHomeMap.Scanner, StandInAnchor.Front, 0f,
                    new StandInBox("Plate", 0.12f, -0.006f, 0.05f, (0.05f, 0.012f, 0.03f), Brass),
                    new StandInBox("Lamp", 0.12f, -0.013f, 0.058f, (0.025f, 0.002f, 0.014f), Amber)),
    };

    /// <summary>
    /// The stand-ins' painted light (NOPE/Desk Painted adds none at the
    /// painting's hour: the bake's objects carry theirs in their paint, so a
    /// box of one flat colour would read as a cut-out): each face's share of
    /// its colour, in the order top, front (toward the chair), right, left,
    /// back, bottom; the painting's light falls from above, to the right and
    /// toward the camera (the shader's _RefLightDir), as on its scanner and
    /// intercom: their tops lightest, their left sides in shade.
    /// </summary>
    public static readonly float[] FaceShade = { 1f, 0.84f, 0.92f, 0.7f, 0.62f, 0.5f };

    /// <summary>The ink of the stand-ins' edges (sRGB): the painting's dark brown line work.</summary>
    public static readonly (float r, float g, float b) InkLine = (0.16f, 0.09f, 0.06f);

    /// <summary>The texels of one face's cell of a stand-in's texture, and how many of them from its edge are ink.</summary>
    public const int StandInCell = 64, InkTexels = 2;

    /// <summary>The colour (sRGB) of the texel (<paramref name="x"/>, <paramref name="y"/>) of a stand-in's face <paramref name="face"/> (FaceShade's order) in <paramref name="colour"/>: ink along its cell's edge (so every box's edges are drawn as the painting's), else the colour in that face's light.</summary>
    public static (float r, float g, float b) StandInTexel((float r, float g, float b) colour, int face, int x, int y)
    {
        int edge = Math.Min(Math.Min(x, y), Math.Min(StandInCell - 1 - x, StandInCell - 1 - y));
        if (edge < InkTexels)
            return InkLine;
        float shade = FaceShade[Math.Max(0, Math.Min(FaceShade.Length - 1, face))];
        return (colour.r * shade, colour.g * shade, colour.b * shade);
    }

    /// <summary>The stand-in for <paramref name="id"/>, or null.</summary>
    public static StandIn StandInFor(string id)
    {
        foreach (StandIn s in StandIns)
            if (s.id == id)
                return s;
        return null;
    }

    /// <summary>
    /// The parts of a gameplay object that stay drawn under its painted look
    /// (DeskPainted.Reskin's and WearBoxes' keep): the scanner's sweep light,
    /// raised onto the painted bed, while a scan runs (its old lid-edge glow
    /// and its readout hide with the old model); the UV torch's pool and
    /// beam; the supply crate's tag (the delivery's live label: what came).
    /// Nothing else of an old model ever shows.
    /// </summary>
    public static string[] Kept(string home) =>
        home == DeskHomeMap.Scanner ? new[] { "SweepBar" }
        : home == DeskHomeMap.UvTorch ? new[] { "UvPool", "BeamFX" }
        : home == DeskHomeMap.Crate ? new[] { "Tag" }
        : Array.Empty<string>();

    /// <summary>
    /// The click boxes of a gameplay object's old model that its painted look
    /// has no part for (by name, under the object): switched off with the
    /// old model, so the painted desk never takes an invisible click from the
    /// old geometry (the code review of round 6). STAMP's daters' side
    /// buttons ("Button", the re-ink: the painted daters have none; STAMP's
    /// art request) and the chip reader's readout ("ReadoutClick": the
    /// painted reader has no live readout yet; DESK-PLAN's machines).
    /// </summary>
    public static string[] DeadClicks(string home) =>
        home == DeskHomeMap.StampRack ? new[] { "Button" } : home == DeskHomeMap.ChipReader ? new[] { "ReadoutClick" } : Array.Empty<string>();

    /// <summary>
    /// The gameplay objects the painted desk draws nothing of and takes no
    /// click on, by their home: the RING SERVICE panel on the switchboard's
    /// home at the booth's right end, where the painted booth wall shows the
    /// painting's own switchboard (round 5: a picture, until PORTALS' Time
    /// Organ replaces the wall). A panel or a stand-in standing in front of
    /// it would cover the board the painting draws there (Saleh: every object
    /// matches the painting). The rings' routes stay on the hall's departure
    /// board and the PC's Portals window.
    /// </summary>
    public static readonly string[] Unshown = { DeskHomeMap.Switchboard };

    /// <summary>
    /// The gameplay objects that keep their own moving parts and wear each
    /// one as a painting-matched box (DeskPainted.WearBoxes, LookOf), by
    /// their home: the supply crate of a delivery morning (the painting, a
    /// day with no delivery, has none), so its flaps still pop open, its tag
    /// still springs up and it still shrinks away into its spot.
    /// </summary>
    public static readonly string[] Boxed = { DeskHomeMap.Crate };

    /// <summary>
    /// Every home whose gameplay object the painted desk dresses (the
    /// binder's audit, OfficeSceneBinder.AuditPainted, reads each one's old
    /// parts): the homes of the objects it re-skins (PaintedRole.Reskin: the
    /// scanner, the chip reader, the intercom, the UV torch, STAMP's rack and
    /// its daters, DETAIN), the inserts its stand-ins stand on, the boxed
    /// crate and the unshown RING SERVICE panel. Each once, in that order.
    /// </summary>
    public static List<string> Dressed()
    {
        var homes = new List<string>();
        foreach (PaintedObject o in Objects)
            if (o.role == PaintedRole.Reskin && o.home != null && !homes.Contains(o.home))
                homes.Add(o.home);
        foreach (string id in Inserts)
            if (!homes.Contains(id))
                homes.Add(id);
        foreach (string id in Boxed)
            if (!homes.Contains(id))
                homes.Add(id);
        foreach (string id in Unshown)
            if (!homes.Contains(id))
                homes.Add(id);
        return homes;
    }

    /// <summary>The crate's kraft (sRGB): halfway between the painting's brass and its cream (#C4A770), the crate model's own kraft (#C4A070) in the painting's palette.</summary>
    public static readonly (float r, float g, float b) Kraft = ((Brass.r + Cream.r) / 2f, (Brass.g + Cream.g) / 2f, (Brass.b + Cream.b) / 2f);

    /// <summary>
    /// How the part named <paramref name="part"/> of the boxed object on
    /// <paramref name="home"/> shows (Boxed): a box in
    /// <paramref name="colour"/> over its mesh, its top
    /// <paramref name="topInset"/> metres lower (the crate's body under its
    /// front and back flaps, which lie 1 mm under its walls' top: never two
    /// faces in one plane), or hidden. The crate: its body and its four flaps
    /// in kraft, its label in cream, its tape hidden (three bent strips no
    /// box draws). Any other part of a boxed object hides; a kept part (Kept:
    /// the crate's tag) is drawn as it is.
    /// </summary>
    public static PartLook LookOf(string home, string part, out (float r, float g, float b) colour, out float topInset)
    {
        colour = default;
        topInset = 0f;
        if (home != DeskHomeMap.Crate)
            return PartLook.Keep;
        switch (part)
        {
            case "Crate":
                colour = Kraft;
                topInset = 0.002f;
                return PartLook.Box;
            case "Flap_Left":
            case "Flap_Right":
            case "Flap_Front":
            case "Flap_Back":
                colour = Kraft;
                return PartLook.Box;
            case "Label":
                colour = Cream;
                return PartLook.Box;
            default:
                return PartLook.Hide;
        }
    }

    /// <summary>The slider (DeskSlides) of the painted object <paramref name="name"/> that lifts on its own (PaintedRole.Slider: the glasses in their stand, the folded book in its pocket), or null.</summary>
    public static string LiftOf(string name) => name == "Desk_Glasses" ? DeskHomeMap.Glasses : name == RulebookFolded ? "rulebook" : null;

    /// <summary>
    /// The glow mask's strength of the painted object <paramref name="name"/>
    /// (NOPE/Desk Painted's _Glow; DeskPaintedImporter writes it): 1, but
    /// the AVAILABLE sign's 0.35 and the scanner's 0.15 (the review of the Unity
    /// capture: the sign's glow blew its panel out to pale lime and its white
    /// word lost the painting's contrast; the round 8 capture: the scanner's
    /// bed burned white, RGB 255, 255, 244, where the painting's is a pale
    /// phosphor green, 228, 241, 198).
    /// </summary>
    public static float Glow(string name) => name == "Desk_AvailableSign" ? 0.35f : name == "Desk_Scanner" ? 0.15f : 1f;

    /// <summary>True when the lamp <paramref name="lamp"/> is lit with the next tool on <paramref name="next"/> (DeskLayout.Lamps; null: nothing next): only that home's lamp (LampOf), every other one unlit (slides.json: a lamp is guidance, never lit at rest for nothing).</summary>
    public static bool LampLit(string lamp, string next)
    {
        string lit = LampOf(next);
        return lit != null && lamp == lit;
    }

    /// <summary>
    /// The lamps whose bezel the approved painting draws at rest (mock_office.png:
    /// a glass dome beside the scanner and one beside the chip reader). The
    /// painting has no lamp beside the deal tray, the bell or the stamp rack,
    /// and paints a brass knob, not a glass dome, at the intercom's tray: the
    /// round 8 capture showed those four bezels as grey discs the painting
    /// does not have.
    /// </summary>
    public static readonly string[] PaintedBezels = { "lamp_scanner", "lamp_chip" };

    /// <summary>True when the lamp <paramref name="lamp"/>'s bezel shows with the next tool on <paramref name="next"/>: always where the painting draws it (PaintedBezels), else only while it is lit (the next-tool lamp still guides from every home).</summary>
    public static bool BezelShown(string lamp, string next) => Array.IndexOf(PaintedBezels, lamp) >= 0 || LampLit(lamp, next);

    /// <summary>
    /// The art office's parts the painted desk replaces (DeskLayoutSettings.
    /// paintedCutParts' default; Cuts reads it): the desk and its rails, the
    /// mat, the PC, keyboard and mouse, the phone, the till and its credits,
    /// the NEXT sign and its caption, the lamp, the old stamp and ink pad,
    /// the binder, the booth's partitions, the art's own shadow cards, the
    /// right cork board's stability monitor (the painted booth wall stands in
    /// front of it: its readouts go to the fallback HUD), the art's
    /// decorations, the form sorter and the tray with its paper bundle (the
    /// painting has none: neither drawn, gathered nor dragged; Saleh: a
    /// clean, organised desk where every object matches the painting), and
    /// both corkboards whole (an entry naming a parent exactly takes every
    /// part under it: the painting has no corkboard). A fresh copy each call.
    /// </summary>
    public static string[] CutParts() => new[]
    {
        "Finish_Desk__", "Finish_DeskRail__", "Clean_Blotter", "CRT2_", "Clean_Keyboard", "Clean_Mouse", "Clean_Phone", "Clean_Till", "CreditsNumber",
        "Clean_Next", "NextLabel", "Clean_Lamp", "Clean_Stamp", "Clean_Inkpad", "Clean_Binder", "Office_Partition__", "Clean Art",
        "Office_Stability__", "StabilityPercent", "Finish_RightEphemera__", "Clean_FormSorter__", "Clean_Tray__", "Clean_PaperBundle__",
        "Left board assembly", "Right board assembly",
    };

    /// <summary>True when the painted desk replaces the art's part named <paramref name="name"/> under <paramref name="parents"/> (its parents' names, nearest first): its name starts with an entry of <paramref name="parts"/> (CutParts), or a parent is named exactly as one (a whole assembly).</summary>
    public static bool Cuts(string name, IEnumerable<string> parents, IReadOnlyList<string> parts)
    {
        if (parts == null)
            return false;
        foreach (string entry in parts)
        {
            if (string.IsNullOrEmpty(entry))
                continue;
            if (name != null && name.StartsWith(entry, StringComparison.Ordinal))
                return true;
            foreach (string parent in parents ?? Array.Empty<string>())
                if (parent == entry)
                    return true;
        }
        return false;
    }

    /// <summary>
    /// The painted lamp beside the home <paramref name="home"/>
    /// (anchors.json's lamps), or null: the next-tool lamp (DeskLayout.Lamps,
    /// NextToolLamp) lights it; every other lamp stays unlit (slides.json: a
    /// lamp is guidance).
    /// </summary>
    public static string LampOf(string home)
    {
        switch (home)
        {
            case DeskHomeMap.DealTray:
                return "lamp_tray";
            case DeskHomeMap.Bell:
                return "lamp_bell";
            case DeskHomeMap.StampRack:
                return "lamp_stamp";
            case DeskHomeMap.Scanner:
                return "lamp_scanner";
            case DeskHomeMap.ChipReader:
                return "lamp_chip";
            case DeskHomeMap.Intercom:
                return "lamp_intercom";
            default:
                return null;
        }
    }

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
        new PaintedObject(RulebookFolded, DeskHomeMap.RulebookPocket, PaintedRole.Slider), // the painting's red book in its pocket, peeking up on the cursor (the "rulebook" lift)
        new PaintedObject(StampRack, DeskHomeMap.StampRack, PaintedRole.Reskin), // STAMP's rack (its old drawer hides)
        new PaintedObject(StampApprove, DeskHomeMap.StampRack, PaintedRole.Reskin), // STAMP's APPROVED dater, upright on the rack's front step
        new PaintedObject(StampDeny, DeskHomeMap.StampRack, PaintedRole.Reskin), // STAMP's DENIED dater, on the back step
        new PaintedObject("Desk_Detain", DeskHomeMap.Detain, PaintedRole.Reskin),
        new PaintedObject("Desk_DetainGuard", DeskHomeMap.Detain, PaintedRole.Reskin),
        new PaintedObject("Desk_Bell", DeskHomeMap.Bell, PaintedRole.Fixed),
        new PaintedObject("Desk_BabelHome", DeskHomeMap.BabelTuner, PaintedRole.Hidden), // the inserts: the shell carries these homes as painted (art request: bake it from the empty plate under them)
        new PaintedObject("Desk_RolodexHome", DeskHomeMap.Rolodex, PaintedRole.Hidden),
        new PaintedObject("Desk_CardsHome", DeskHomeMap.ProtocolRing, PaintedRole.Hidden),
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
