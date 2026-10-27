/// <summary>
/// Where the chip reader's readout line lies on the painted reader (desk v3
/// round 12's S1: "the chip reader result has no readout on the painted
/// desk"; the night of 2026-10-28, seam D): the old reader's live text
/// (ChipReader.Show writes it, the one source) re-shown as a line on the
/// painted reader's top face, on its near third (toward the chair), facing
/// up, its top edge away from the chair; and the readout's click box over
/// the line, raised so its top stands above the painted body's padded click
/// box (DeskHomeMap.ClickPad) and takes the ray first. Pure (no engine): the
/// painted reader's bounds in the painted desk's frame (x across, y up, z
/// away from the chair, metres) in, the line's and the box's pose out;
/// DeskPainted.WearReadout applies it, with the facing it names.
/// </summary>
public static class PaintedReadout
{
    /// <summary>The line floats this far over the top face (metres; the old readout over its quad).</summary>
    public const float Lift = 0.0008f;

    /// <summary>The line's width as a share of the top face's width.</summary>
    public const float WidthShare = 0.9f;

    /// <summary>The line's height as a share of the top face's depth.</summary>
    public const float DepthShare = 0.32f;

    /// <summary>Where the line's centre lies along the top face's depth, from its near edge (a share): the near third.</summary>
    public const float NearShare = 0.3f;

    /// <summary>The click box's centre over the line (metres) and its thickness: its top at BoxRise + BoxThickness / 2 = 2.7 cm over the face, above the body box's 1 cm pad.</summary>
    public const float BoxRise = 0.012f, BoxThickness = 0.03f;

    /// <summary>The box's top over the face (metres): it must stand above the body's padded box, so a click on the line meets the readout's box first.</summary>
    public const float BoxTop = BoxRise + BoxThickness / 2f;

    /// <summary>The painted reader's bounds in the painted desk's frame (metres): x across, y up, z away from the chair.</summary>
    public readonly struct Box
    {
        public readonly float MinX, MaxX, MinY, MaxY, MinZ, MaxZ;

        public Box(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
        {
            MinX = minX; MaxX = maxX; MinY = minY; MaxY = maxY; MinZ = minZ; MaxZ = maxZ;
        }

        public float Width => MaxX - MinX;
        public float Depth => MaxZ - MinZ;
        public float CentreX => (MinX + MaxX) / 2f;
        public float CentreZ => (MinZ + MaxZ) / 2f;
    }

    /// <summary>The line's centre in the frame of <paramref name="reader"/>: centred across, over the top face by the lift, on the near third.</summary>
    public static (float x, float y, float z) LinePosition(Box reader) =>
        (reader.CentreX, reader.MaxY + Lift, reader.MinZ + reader.Depth * NearShare);

    /// <summary>The line's rectangle (width across, height along the depth).</summary>
    public static (float width, float height) LineSize(Box reader) => (reader.Width * WidthShare, reader.Depth * DepthShare);

    /// <summary>The readout's click box's centre: over the line, BoxRise up.</summary>
    public static (float x, float y, float z) BoxCentre(Box reader)
    {
        (float x, float y, float z) = LinePosition(reader);
        return (x, y + BoxRise, z);
    }

    /// <summary>The readout's click box's size: the line's footprint, BoxThickness tall.</summary>
    public static (float x, float y, float z) BoxSize(Box reader)
    {
        (float width, float height) = LineSize(reader);
        return (width, BoxThickness, height);
    }
}
