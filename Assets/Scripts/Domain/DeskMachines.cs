using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>What a scan layer tab shows for a paper (the desk machines spec §1): nothing yet, the machine's reading, or that the paper carries none.</summary>
public enum LayerState
{
    /// <summary>The machine is not on the desk today (before its day): the tab is hidden.</summary>
    Unavailable,

    /// <summary>The machine is on the desk but has not read this paper yet (no chip read, no mark seen under the torch).</summary>
    NotYet,

    /// <summary>The machine's reading shows.</summary>
    Shown,

    /// <summary>The paper carries no chip (the CHIP tab only).</summary>
    None
}

/// <summary>
/// What the desk machines have read on one case's papers (the desk machines
/// spec §1): the papers whose chip the reader has read, and the UV marks the
/// torch's cone has shown (each by its paper and its index in the paper's
/// marks). The scanned copy's UV and CHIP tabs show only these, so a machine
/// is what reveals them. Cleared at each new case. Pure.
/// </summary>
public sealed class DeskReadings
{
    private readonly HashSet<int> _chips = new HashSet<int>();
    private readonly HashSet<(int document, int mark)> _seen = new HashSet<(int, int)>();

    /// <summary>Raised when a chip is read or a mark is first seen, with the paper.</summary>
    public event Action<int> Changed;

    /// <summary>Forgets every reading (a new case).</summary>
    public void Clear()
    {
        _chips.Clear();
        _seen.Clear();
    }

    /// <summary>The reader has read paper <paramref name="document"/>'s chip (once; false when it was already read).</summary>
    public bool ReadChip(int document)
    {
        if (document < 0 || !_chips.Add(document))
            return false;
        Changed?.Invoke(document);
        return true;
    }

    /// <summary>The torch's cone has shown mark <paramref name="mark"/> of paper <paramref name="document"/> (once; false when it was already seen).</summary>
    public bool See(int document, int mark)
    {
        if (document < 0 || mark < 0 || !_seen.Add((document, mark)))
            return false;
        Changed?.Invoke(document);
        return true;
    }

    /// <summary>True when paper <paramref name="document"/>'s chip has been read.</summary>
    public bool ChipRead(int document) => _chips.Contains(document);

    /// <summary>True when mark <paramref name="mark"/> of paper <paramref name="document"/> has been seen.</summary>
    public bool Seen(int document, int mark) => _seen.Contains((document, mark));

    /// <summary>The indices of paper <paramref name="document"/>'s marks seen so far, in mark order.</summary>
    public List<int> SeenMarks(int document) => _seen.Where(s => s.document == document).Select(s => s.mark).OrderBy(m => m).ToList();
}

/// <summary>
/// The scan layers as the desk machines reveal them (the desk machines spec
/// §1): the case's hidden layers (CaseHidden) seen through what the reader and
/// the torch have read today (DeskReadings), each tab shown only from its
/// machine's day. Before the torch's day the UV tab is hidden; after it, it
/// lists only the marks seen under the cone. Before the reader's day the CHIP
/// tab is hidden; after it, a chip shows once read. Pure.
/// </summary>
public sealed class MachineLayers : IScanLayers
{
    private readonly CaseHidden _hidden;
    private readonly DeskReadings _readings;

    /// <summary>A view of <paramref name="hidden"/> through <paramref name="readings"/>; <paramref name="torch"/> and <paramref name="reader"/>: each machine is on the desk today.</summary>
    public MachineLayers(CaseHidden hidden, DeskReadings readings, bool torch, bool reader)
    {
        _hidden = hidden ?? CaseHidden.None;
        _readings = readings ?? new DeskReadings();
        Torch = torch;
        Reader = reader;
    }

    /// <summary>True when the UV torch is on the desk today.</summary>
    public bool Torch { get; }

    /// <summary>True when the chip reader is on the desk today.</summary>
    public bool Reader { get; }

    /// <summary>What the UV tab shows for paper <paramref name="document"/>.</summary>
    public LayerState UvState(int document) =>
        !Torch ? LayerState.Unavailable : _readings.SeenMarks(document).Count > 0 ? LayerState.Shown : LayerState.NotYet;

    /// <summary>What the CHIP tab shows for paper <paramref name="document"/>.</summary>
    public LayerState ChipState(int document) =>
        !Reader ? LayerState.Unavailable : !_hidden.HasChip(document) ? LayerState.None : _readings.ChipRead(document) ? LayerState.Shown : LayerState.NotYet;

    /// <summary>The marks of paper <paramref name="document"/> seen under the torch, in mark order (none before the torch's day).</summary>
    public IReadOnlyList<UvMark> Uv(int document, IReadOnlyList<DocumentField> print)
    {
        if (!Torch)
            return Array.Empty<UvMark>();
        IReadOnlyList<UvMark> marks = _hidden.Marks(document);
        return _readings.SeenMarks(document).Where(i => i < marks.Count).Select(i => marks[i]).ToList();
    }

    /// <summary>Paper <paramref name="document"/>'s chip once the reader has read it; null before (or with no chip).</summary>
    public IReadOnlyList<DocumentField> Chip(int document, IReadOnlyList<DocumentField> print) =>
        ChipState(document) == LayerState.Shown ? _hidden.ChipOf(document) : null;
}

/// <summary>A step of the chip reader's cycle (the desk machines spec §1).</summary>
public enum ChipReaderStep
{
    /// <summary>Waiting for a paper ("READY").</summary>
    Idle,

    /// <summary>The card slides into the slot, or the passport settles on the pad.</summary>
    Insert,

    /// <summary>The reader reads ("READING" with its dots, the READ lamp lit).</summary>
    Reading,

    /// <summary>The result shows (CHIP OK, the differing lines, NO CHIP) with its beep and lamp.</summary>
    Result,

    /// <summary>The paper comes back out.</summary>
    Eject
}

/// <summary>What a read found (the desk machines spec §1).</summary>
public enum ChipOutcome
{
    /// <summary>The paper carries no chip (amber).</summary>
    NoChip,

    /// <summary>The chip holds what the paper prints (green).</summary>
    Match,

    /// <summary>The chip holds another value than the print in at least one box (red).</summary>
    Mismatch
}

/// <summary>One line the reader's readout shows for a differing box: the box's index in the paper, its category and what the chip holds.</summary>
public readonly struct ChipLine
{
    /// <summary>A line.</summary>
    public ChipLine(int field, ClueCategory category, string chip)
    {
        Field = field;
        Category = category;
        Chip = chip ?? string.Empty;
    }

    /// <summary>The box's index in the paper's fields.</summary>
    public int Field { get; }

    /// <summary>The box's category.</summary>
    public ClueCategory Category { get; }

    /// <summary>What the chip holds there.</summary>
    public string Chip { get; }
}

/// <summary>
/// The chip reader's cycle and readout (the desk machines spec §1; Saleh
/// 2026-10-17: "an actual machine that reads the chip"): a paper dropped on it
/// goes in (Insert), is read (Reading), shows its result (Result) and comes
/// out (Eject), each step for its own seconds (DeskConfigSO's knobs); the
/// result is the chip against the print (ScanLayers.ChipDifferences, the
/// photo and the seal never on a chip's readout). Pure: the machine's
/// MonoBehaviour plays what <see cref="Tick"/> says.
/// </summary>
public sealed class ChipReaderCycle
{
    private readonly float[] _seconds;
    private float _at;

    /// <summary>A reader idle, with the seconds of Insert, Reading, Result and Eject (each at least 0).</summary>
    public ChipReaderCycle(float insert, float reading, float result, float eject)
    {
        _seconds = new[] { Math.Max(0f, insert), Math.Max(0f, reading), Math.Max(0f, result), Math.Max(0f, eject) };
        Step = ChipReaderStep.Idle;
        Document = -1;
    }

    /// <summary>The step it is in.</summary>
    public ChipReaderStep Step { get; private set; }

    /// <summary>The paper in it (-1 when idle).</summary>
    public int Document { get; private set; }

    /// <summary>The current read's outcome (Match while idle).</summary>
    public ChipOutcome Outcome { get; private set; }

    /// <summary>The current read's differing lines (none when it matches or reads no chip).</summary>
    public IReadOnlyList<ChipLine> Lines { get; private set; } = Array.Empty<ChipLine>();

    /// <summary>True while a paper is in it.</summary>
    public bool Busy => Step != ChipReaderStep.Idle;

    /// <summary>How far through the current step it is (0 to 1; 0 while idle).</summary>
    public float Progress => Step == ChipReaderStep.Idle ? 0f : Clamp01(_at / Math.Max(1e-4f, _seconds[(int)Step - 1]));

    /// <summary>Starts reading paper <paramref name="document"/>, which prints <paramref name="print"/> and carries <paramref name="chip"/> (null: none); false, with nothing started, while busy.</summary>
    public bool Begin(int document, IReadOnlyList<DocumentField> print, IReadOnlyList<DocumentField> chip)
    {
        if (Busy || document < 0)
            return false;
        Document = document;
        Lines = Read(print, chip);
        Outcome = chip == null ? ChipOutcome.NoChip : Lines.Count > 0 ? ChipOutcome.Mismatch : ChipOutcome.Match;
        Step = ChipReaderStep.Insert;
        _at = 0f;
        return true;
    }

    /// <summary>Advances <paramref name="dt"/> seconds; returns the step it entered this tick (it may pass several), or null when it stays. After Eject it is Idle again.</summary>
    public ChipReaderStep? Tick(float dt)
    {
        if (!Busy)
            return null;
        _at += Math.Max(0f, dt);
        ChipReaderStep? entered = null;
        while (Busy && _at >= _seconds[(int)Step - 1])
        {
            _at -= _seconds[(int)Step - 1];
            Step = Step == ChipReaderStep.Eject ? ChipReaderStep.Idle : Step + 1;
            entered = Step;
            if (Step == ChipReaderStep.Idle)
            {
                _at = 0f;
                Document = -1;
            }
        }
        return entered;
    }

    /// <summary>Stops at once (the case ended): idle, nothing read.</summary>
    public void Cancel()
    {
        Step = ChipReaderStep.Idle;
        Document = -1;
        Lines = Array.Empty<ChipLine>();
        _at = 0f;
    }

    /// <summary>The lines where <paramref name="chip"/> holds another value than <paramref name="print"/> (ScanLayers.ChipDifferences), the photo and the seal skipped; none without a chip.</summary>
    public static List<ChipLine> Read(IReadOnlyList<DocumentField> print, IReadOnlyList<DocumentField> chip)
    {
        var lines = new List<ChipLine>();
        if (print == null || chip == null)
            return lines;
        foreach (int i in ScanLayers.ChipDifferences(print, chip))
            if (print[i].category != ClueCategory.Photo && print[i].category != ClueCategory.Seal)
                lines.Add(new ChipLine(i, print[i].category, chip[i].value));
        return lines;
    }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
}

/// <summary>
/// The UV torch's cone on the desk (the desk machines spec §1): a circle on the
/// desk plane (its centre and radius, in the desk's metres) and which marks it
/// shows. A mark is a rectangle on the desk (its box's centre and size); it
/// counts as seen once at least <see cref="SeenShare"/> of it has lain inside
/// the cone for <see cref="SeenSeconds"/> in a row. Pure: the torch's
/// MonoBehaviour feeds it the cone and the marks' rectangles each frame.
/// </summary>
public static class UvCone
{
    /// <summary>The share of a mark's area inside the cone that counts it as lit (the spec's 60 %).</summary>
    public const float SeenShare = 0.6f;

    /// <summary>The seconds a mark must stay lit to count as seen (the spec's 0.25 s).</summary>
    public const float SeenSeconds = 0.25f;

    /// <summary>The share (0 to 1) of the rectangle centred at (<paramref name="x"/>, <paramref name="y"/>) of <paramref name="width"/> by <paramref name="height"/> inside the circle at (<paramref name="cx"/>, <paramref name="cy"/>) of <paramref name="radius"/>: sampled on a <paramref name="grid"/> by grid of points (enough for a box's share; no allocation).</summary>
    public static float Covered(float x, float y, float width, float height, float cx, float cy, float radius, int grid = 6)
    {
        if (radius <= 0f || width <= 0f || height <= 0f || grid < 1)
            return 0f;
        int inside = 0;
        float r2 = radius * radius;
        for (int i = 0; i < grid; i++)
            for (int j = 0; j < grid; j++)
            {
                float px = x - width / 2f + width * (i + 0.5f) / grid - cx;
                float py = y - height / 2f + height * (j + 0.5f) / grid - cy;
                if (px * px + py * py <= r2)
                    inside++;
            }
        return inside / (float)(grid * grid);
    }

    /// <summary>A mark's lit time after a frame of <paramref name="dt"/>: it grows while <paramref name="covered"/> reaches SeenShare and the torch is on, and falls back to 0 otherwise.</summary>
    public static float Lit(float litSeconds, float covered, bool on, float dt) =>
        on && covered >= SeenShare ? litSeconds + Math.Max(0f, dt) : 0f;

    /// <summary>True when a mark lit for <paramref name="litSeconds"/> counts as seen.</summary>
    public static bool IsSeen(float litSeconds) => litSeconds >= SeenSeconds;
}
