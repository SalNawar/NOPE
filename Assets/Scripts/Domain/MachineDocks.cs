using System;
using System.Collections.Generic;

/// <summary>The desk machines a paper can be fed to (Saleh's 1018a playtest: "when you move a document to a machine it should move to the correct elevation and orientation"). Append only.</summary>
public enum DeskMachine
{
    /// <summary>The cream scanner: a paper lies on its glass bed under the lid.</summary>
    Scanner,

    /// <summary>The chip reader: a card slides into its slot, a passport lies on its RF pad.</summary>
    ChipReader
}

/// <summary>Where a machine takes what is dropped on it, or that it refuses it.</summary>
public enum MachineDock
{
    /// <summary>The machine cannot take it: it bounces back with the machine's "no" and never passes through the casing.</summary>
    Refused,

    /// <summary>It lies on the machine's bed (the scanner's glass).</summary>
    Bed,

    /// <summary>It lies on the machine's pad (the chip reader's RF pad, a passport).</summary>
    Pad,

    /// <summary>It slides into the machine's slot at the slot's height and angle (the chip reader's card slot).</summary>
    Slot
}

/// <summary>What a thing dropped on a machine is, as the machine sees it.</summary>
public readonly struct MachineFeed
{
    /// <summary>A thing: a case paper or not (the rulebook folder), carrying a chip or not (its form, HiddenContent.CarriesChip), a card or not (a card goes into a slot short edge first).</summary>
    public MachineFeed(bool casePaper, bool carriesChip, bool card)
    {
        CasePaper = casePaper;
        CarriesChip = carriesChip;
        Card = card;
    }

    /// <summary>True for one of the traveller's papers (the scanner copies only those).</summary>
    public bool CasePaper { get; }

    /// <summary>True when it carries a chip (a passport, a transponder card).</summary>
    public bool CarriesChip { get; }

    /// <summary>True when it is a card (it fits a card slot).</summary>
    public bool Card { get; }

    /// <summary>The rulebook folder (no case paper, no chip, no card).</summary>
    public static MachineFeed Folder => new MachineFeed(false, false, false);
}

/// <summary>
/// Which documents each desk machine takes and where (Saleh's 1018a
/// playtest: "if is the wrong document it shouldnt go through the
/// machience"; the desk machines spec §1): the scanner copies any of the
/// traveller's papers on its bed and refuses anything else (the rulebook
/// folder); the chip reader reads a paper that carries a chip, a card in
/// its slot and a passport on its pad, and refuses a paper with no chip (a
/// ticket, a permit, a waiver) and anything else. A refused thing bounces
/// back with the machine's "no" (DockPath keeps it over the casing). Pure.
/// </summary>
public static class MachineIntake
{
    /// <summary>Where <paramref name="machine"/> takes <paramref name="feed"/>, or Refused.</summary>
    public static MachineDock Dock(DeskMachine machine, MachineFeed feed)
    {
        switch (machine)
        {
            case DeskMachine.Scanner:
                return feed.CasePaper ? MachineDock.Bed : MachineDock.Refused;
            case DeskMachine.ChipReader:
                if (!feed.CasePaper || !feed.CarriesChip)
                    return MachineDock.Refused;
                return feed.Card ? MachineDock.Slot : MachineDock.Pad;
            default:
                return MachineDock.Refused;
        }
    }

    /// <summary>True when <paramref name="machine"/> takes <paramref name="feed"/>.</summary>
    public static bool Accepts(DeskMachine machine, MachineFeed feed) => Dock(machine, feed) != MachineDock.Refused;
}

/// <summary>
/// A desk machine's casing as a moving paper sees it: a box standing on the
/// desk, its footprint a rectangle centred at (<see cref="CentreX"/>,
/// <see cref="CentreZ"/>) turned so its width runs along
/// (<see cref="AxisX"/>, <see cref="AxisZ"/>), up to <see cref="Top"/>
/// (all world metres; y up).
/// </summary>
public readonly struct MachineBox
{
    /// <summary>A box: its footprint's centre, its width's direction (normalised here), its half width and half depth, and its top's height.</summary>
    public MachineBox(float centreX, float centreZ, float axisX, float axisZ, float halfWidth, float halfDepth, float top)
    {
        float length = (float)Math.Sqrt(axisX * axisX + axisZ * axisZ);
        CentreX = centreX;
        CentreZ = centreZ;
        AxisX = length > 1e-6f ? axisX / length : 1f;
        AxisZ = length > 1e-6f ? axisZ / length : 0f;
        HalfWidth = Math.Max(0f, halfWidth);
        HalfDepth = Math.Max(0f, halfDepth);
        Top = top;
    }

    /// <summary>The footprint's centre (world x, z).</summary>
    public float CentreX { get; }

    /// <inheritdoc cref="CentreX"/>
    public float CentreZ { get; }

    /// <summary>The direction its width runs along (a unit vector in the desk's plane, world x, z); its depth runs at right angles to it.</summary>
    public float AxisX { get; }

    /// <inheritdoc cref="AxisX"/>
    public float AxisZ { get; }

    /// <summary>Half its footprint's width and depth (metres).</summary>
    public float HalfWidth { get; }

    /// <inheritdoc cref="HalfWidth"/>
    public float HalfDepth { get; }

    /// <summary>Its top's height (world y).</summary>
    public float Top { get; }

    /// <summary>A world point in the box's own frame: across its width and along its depth from its centre.</summary>
    public (float across, float along) Local(float x, float z)
    {
        float dx = x - CentreX, dz = z - CentreZ;
        return (dx * AxisX + dz * AxisZ, -dx * AxisZ + dz * AxisX);
    }

    /// <summary>True when (<paramref name="x"/>, <paramref name="z"/>) lies over the footprint grown by <paramref name="grow"/> on every side.</summary>
    public bool Over(float x, float z, float grow)
    {
        (float a, float b) = Local(x, z);
        return Math.Abs(a) <= HalfWidth + grow && Math.Abs(b) <= HalfDepth + grow;
    }

    /// <summary>The share of the way from (<paramref name="x0"/>, <paramref name="z0"/>) to (<paramref name="x1"/>, <paramref name="z1"/>) where it comes over the footprint grown by <paramref name="grow"/> and where it leaves it; false when it never does.</summary>
    public bool Crossing(float x0, float z0, float x1, float z1, float grow, out float enter, out float leave)
    {
        (float a0, float b0) = Local(x0, z0);
        (float a1, float b1) = Local(x1, z1);
        enter = 0f;
        leave = 1f;
        return Clip(a0, a1 - a0, HalfWidth + grow, ref enter, ref leave) && Clip(b0, b1 - b0, HalfDepth + grow, ref enter, ref leave);
    }

    /// <summary>Clips the way's share to where its coordinate (from <paramref name="from"/>, changing by <paramref name="by"/>) lies within ±<paramref name="half"/> (Liang-Barsky).</summary>
    private static bool Clip(float from, float by, float half, ref float enter, ref float leave)
    {
        if (Math.Abs(by) < 1e-9f)
            return Math.Abs(from) <= half;
        float t0 = (-half - from) / by, t1 = (half - from) / by;
        if (t0 > t1)
            (t0, t1) = (t1, t0);
        enter = Math.Max(enter, t0);
        leave = Math.Min(leave, t1);
        return enter <= leave;
    }
}

/// <summary>
/// The way a paper moves to, into, out of and past the desk machines (Saleh's
/// 1018a playtest: "when you move a document to a machine it should move to
/// the correct elevation and orintation"): it never passes through a
/// casing. A way over the desk that comes over a machine (its footprint grown
/// by the paper's half size) rises to the highest such machine's top plus a
/// clearance before it comes over it and stays there until it has left it,
/// then comes down; a way onto a machine's bed or pad comes down onto it from
/// straight above; a way into a slot comes down in front of the slot's mouth,
/// at the slot's height, clear of the casing, and slides in along the slot;
/// out, the same way back. Points are world metres (x, y up, z). Pure.
/// </summary>
public static class DockPath
{
    /// <summary>The way from <paramref name="start"/> to <paramref name="end"/> that rises over every machine in <paramref name="boxes"/> it comes over (footprints grown by <paramref name="grow"/>), <paramref name="clearance"/> over its top; the straight way when it comes over none.</summary>
    public static List<(float x, float y, float z)> Over((float x, float y, float z) start, (float x, float y, float z) end, IReadOnlyList<MachineBox> boxes, float grow, float clearance)
    {
        var way = new List<(float x, float y, float z)> { start };
        float high = float.MinValue, enter = 1f, leave = 0f;
        bool crosses = false;
        if (boxes != null)
            foreach (MachineBox box in boxes)
                if (box.Crossing(start.x, start.z, end.x, end.z, grow, out float a, out float b))
                {
                    crosses = true;
                    high = Math.Max(high, box.Top + clearance);
                    enter = Math.Min(enter, a);
                    leave = Math.Max(leave, b);
                }
        if (crosses)
        {
            if (start.y < high)
                Add(way, (Lerp(start.x, end.x, enter), high, Lerp(start.z, end.z, enter)));
            Add(way, (Lerp(start.x, end.x, leave), high, Lerp(start.z, end.z, leave)));
        }
        Add(way, end);
        return way;
    }

    /// <summary>
    /// The way from <paramref name="start"/> onto a machine's bed or pad at
    /// <paramref name="dock"/>: over every machine in the way (as Over),
    /// coming down onto the dock from straight above it.
    /// </summary>
    public static List<(float x, float y, float z)> OnTo((float x, float y, float z) start, (float x, float y, float z) dock, IReadOnlyList<MachineBox> boxes, float grow, float clearance) =>
        Over(start, dock, boxes, grow, clearance);

    /// <summary>
    /// The point in front of a slot's mouth at <paramref name="mouth"/> (its
    /// way in along (<paramref name="inX"/>, <paramref name="inZ"/>), a unit
    /// vector) where a paper <paramref name="halfLength"/> long along it lies
    /// clear of <paramref name="box"/> grown by <paramref name="grow"/>, at the
    /// mouth's height: the paper comes down there, then slides in.
    /// </summary>
    public static (float x, float y, float z) SlotFront((float x, float y, float z) mouth, float inX, float inZ, float halfLength, MachineBox box, float grow)
    {
        float back = halfLength + 0.01f;
        for (int i = 0; i < 200 && box.Over(mouth.x - inX * back, mouth.z - inZ * back, grow); i++)
            back += 0.005f;
        return (mouth.x - inX * back, mouth.y, mouth.z - inZ * back);
    }

    /// <summary>Where a paper <paramref name="halfLength"/> long lies once it has gone <paramref name="depth"/> into a slot at <paramref name="mouth"/> along (<paramref name="inX"/>, <paramref name="inZ"/>): its centre (the rest of it stays out of the mouth).</summary>
    public static (float x, float y, float z) SlotInside((float x, float y, float z) mouth, float inX, float inZ, float halfLength, float depth)
    {
        float along = Math.Min(depth, 2f * halfLength) - halfLength;
        return (mouth.x + inX * along, mouth.y, mouth.z + inZ * along);
    }

    /// <summary>The way from <paramref name="start"/> into a slot at <paramref name="mouth"/> (see SlotFront, SlotInside): over every machine in the way down to in front of the mouth, at its height, then in.</summary>
    public static List<(float x, float y, float z)> Into((float x, float y, float z) start, (float x, float y, float z) mouth, float inX, float inZ, float halfLength, float depth, MachineBox box, IReadOnlyList<MachineBox> boxes, float grow, float clearance)
    {
        var way = Over(start, SlotFront(mouth, inX, inZ, halfLength, box, grow), boxes, grow, clearance);
        Add(way, SlotInside(mouth, inX, inZ, halfLength, depth));
        return way;
    }

    /// <summary>The way back out of a slot (the reverse of Into's last stroke) and on to <paramref name="end"/> over every machine in the way.</summary>
    public static List<(float x, float y, float z)> OutOf((float x, float y, float z) mouth, float inX, float inZ, float halfLength, float depth, MachineBox box, (float x, float y, float z) end, IReadOnlyList<MachineBox> boxes, float grow, float clearance)
    {
        var way = new List<(float x, float y, float z)> { SlotInside(mouth, inX, inZ, halfLength, depth) };
        (float x, float y, float z) front = SlotFront(mouth, inX, inZ, halfLength, box, grow);
        Add(way, front);
        List<(float x, float y, float z)> on = Over(front, end, boxes, grow, clearance);
        for (int i = 1; i < on.Count; i++)
            Add(way, on[i]);
        return way;
    }

    /// <summary>How long a way is (metres).</summary>
    public static float Length(IReadOnlyList<(float x, float y, float z)> way)
    {
        float total = 0f;
        for (int i = 1; i < way.Count; i++)
            total += Distance(way[i - 1], way[i]);
        return total;
    }

    /// <summary>The point <paramref name="share"/> (0 to 1) of the way along it, by length.</summary>
    public static (float x, float y, float z) At(IReadOnlyList<(float x, float y, float z)> way, float share)
    {
        if (way == null || way.Count == 0)
            return (0f, 0f, 0f);
        float total = Length(way);
        if (way.Count == 1 || total <= 0f)
            return way[way.Count - 1];
        float want = Math.Max(0f, Math.Min(1f, share)) * total;
        for (int i = 1; i < way.Count; i++)
        {
            float d = Distance(way[i - 1], way[i]);
            if (want <= d || i == way.Count - 1)
            {
                float t = d > 0f ? Math.Min(1f, want / d) : 1f;
                return (Lerp(way[i - 1].x, way[i].x, t), Lerp(way[i - 1].y, way[i].y, t), Lerp(way[i - 1].z, way[i].z, t));
            }
            want -= d;
        }
        return way[way.Count - 1];
    }

    private static void Add(List<(float x, float y, float z)> way, (float x, float y, float z) p)
    {
        if (way.Count == 0 || Distance(way[way.Count - 1], p) > 1e-5f)
            way.Add(p);
    }

    private static float Distance((float x, float y, float z) a, (float x, float y, float z) b) =>
        (float)Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
