using System;

/// <summary>
/// Where the chip reader's readout line lies on the painted reader (desk v3
/// round 12's S1: "the chip reader result has no readout on the painted
/// desk"; the night of 2026-10-28, seam D): the old reader's live text
/// (ChipReader.Show writes it, the one source) re-shown as a line on the
/// painted reader's top face, on its near third (toward the chair), its near
/// edge on the face and the line leant toward the chair (Tilt), its top edge
/// away from the chair; and the readout's click box over the line, its top
/// above the painted body's padded click box (DeskHomeMap.ClickPad), so it
/// takes the ray first. The type may grow to fill the line (FontSizeMax; the
/// code review's R3 of 2026-10-28: the old reader's auto-size cap of 0.12
/// left READY about 21 x 3 px at 1080p, and the flat line was seen at about
/// 20 degrees from the chair). The line is a VFD window (Saleh's playtest of
/// 2026-11-07, item 15: "Chip reader has text on it instead of actual
/// machine"): a dark glass the line's size behind the text, a thin bezel
/// round it (BezelSize), and the text inset in the glass (TextSize), so
/// every state reads inside the window, over the painted card slot line.
/// Pure (no engine): the painted reader's bounds in the painted desk's frame
/// (x across, y up, z away from the chair, metres) in, the line's and the
/// box's pose out; DeskPainted.WearReadout applies it, with the facing
/// LineFacing names.
/// </summary>
public static class PaintedReadout
{
    /// <summary>The line's near edge floats this far over the top face (metres): room for the bezel behind the glass (BezelDepth) at the near edge.</summary>
    public const float Lift = 0.002f;

    /// <summary>
    /// The line's width as a share of the top face's width. The leant line
    /// rides up to 3.5 cm over the face, and the reading view's camera, left
    /// of the reader, shifts it right on screen by about 30 px at 1080p
    /// (the verifier of 2026-11-08: at 0.9 the window's top right corner stood
    /// up to 29 px past the body over the brass); at 0.6 the right edge comes
    /// in 36 px and stays inside the painted body in every row, about 8 px
    /// clear at the top right corner where the reader rests at its home.
    /// </summary>
    public const float WidthShare = 0.6f;

    /// <summary>The line's height as a share of the top face's depth.</summary>
    public const float DepthShare = 0.32f;

    /// <summary>Where the flat line's centre lay along the top face's depth, from its near edge (a share): above the painted card slot line (the near third's; item 15: the slot stays in view under the window). The leant line keeps that line's near edge.</summary>
    public const float NearShare = 0.46f;

    /// <summary>
    /// How far the line leans toward the chair (degrees, about its near edge;
    /// 0 lays it flat on the face): between the office eye (the face seen at
    /// about 24 degrees) and the reading view (about 80 degrees), so it reads
    /// in both. 25 degrees gives the line 1.6 times its flat height on screen
    /// from the chair and raises its far edge 3.5 cm on the 26 cm reader.
    /// </summary>
    public const float Tilt = 25f;

    /// <summary>The click box's margin over the line's far (top) edge (metres).</summary>
    public const float BoxRise = 0.012f;

    /// <summary>
    /// A world-space TextMeshPro's em in metres for each unit of font size (TMP
    /// scales a 3D text's point size by 0.1: a size of 1 sets a 0.1 m em).
    /// </summary>
    public const float EmPerFontSize = 0.1f;

    /// <summary>The text's inset from the glass's edge on every side (metres): the letters stay off the bezel.</summary>
    public const float WindowPad = 0.006f;

    /// <summary>The bezel's width round the glass (metres): about one pixel from the office eye at 1080p, two in the reading view.</summary>
    public const float Bezel = 0.0012f;

    /// <summary>How far behind the text's plane the glass lies (metres; the text shows over it).</summary>
    public const float GlassDepth = 0.0003f;

    /// <summary>How far behind the text's plane the bezel lies (metres; behind the glass).</summary>
    public const float BezelDepth = 0.0006f;

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

    private static float TiltRadians => Tilt * (float)Math.PI / 180f;

    /// <summary>The line's rectangle in its own plane (width across, height from its near edge to its far edge): the glass.</summary>
    public static (float width, float height) LineSize(Box reader) => (reader.Width * WidthShare, reader.Depth * DepthShare);

    /// <summary>The text's rectangle inside the glass: the line's, less WindowPad on every side.</summary>
    public static (float width, float height) TextSize(Box reader)
    {
        (float width, float height) = LineSize(reader);
        return (width - 2f * WindowPad, height - 2f * WindowPad);
    }

    /// <summary>The bezel's rectangle round the glass: the line's, plus Bezel on every side.</summary>
    public static (float width, float height) BezelSize(Box reader)
    {
        (float width, float height) = LineSize(reader);
        return (width + 2f * Bezel, height + 2f * Bezel);
    }

    /// <summary>The line's near edge (its centre point) in the frame of <paramref name="reader"/>: on the top face by the lift, where the flat line's near edge lay.</summary>
    public static (float x, float y, float z) NearEdge(Box reader)
    {
        (_, float height) = LineSize(reader);
        return (reader.CentreX, reader.MaxY + Lift, reader.MinZ + reader.Depth * NearShare - height / 2f);
    }

    /// <summary>The line's far edge (its centre point): the near edge, then the line's height up the lean.</summary>
    public static (float x, float y, float z) FarEdge(Box reader)
    {
        (float x, float y, float z) = NearEdge(reader);
        (_, float height) = LineSize(reader);
        return (x, y + height * (float)Math.Sin(TiltRadians), z + height * (float)Math.Cos(TiltRadians));
    }

    /// <summary>The line's centre in the frame of <paramref name="reader"/>: centred across, half way up the lean.</summary>
    public static (float x, float y, float z) LinePosition(Box reader)
    {
        (float x, float y, float z) = NearEdge(reader);
        (float fx, float fy, float fz) = FarEdge(reader);
        return ((x + fx) / 2f, (y + fy) / 2f, (z + fz) / 2f);
    }

    /// <summary>
    /// The line's facing in the frame: its forward (a TextMeshPro shows on its
    /// -Z, so forward points down and away from the chair; the text faces up
    /// and toward the chair) and its up (up the lean, away from the chair, so
    /// it reads from the chair). Flat (Tilt 0): forward down, up away from the chair.
    /// </summary>
    public static ((float x, float y, float z) forward, (float x, float y, float z) up) LineFacing()
    {
        float s = (float)Math.Sin(TiltRadians), c = (float)Math.Cos(TiltRadians);
        return ((0f, -c, s), (0f, s, c));
    }

    /// <summary>
    /// The readout text's largest font size: its em fills the text's height
    /// inside the glass (TextSize, EmPerFontSize), so a short line (READY,
    /// CHIP OK) fills the window and a long one shrinks only to the text's
    /// width (the text auto-sizes).
    /// </summary>
    public static float FontSizeMax(Box reader)
    {
        (_, float height) = TextSize(reader);
        return height / EmPerFontSize;
    }

    /// <summary>The readout's click box's centre: over the line's footprint, from the line's near edge up to BoxRise over its far edge.</summary>
    public static (float x, float y, float z) BoxCentre(Box reader)
    {
        (float x, float y, float z) = LinePosition(reader);
        (_, float bottom, _) = NearEdge(reader);
        (_, float far, _) = FarEdge(reader);
        return (x, (bottom + far + BoxRise) / 2f, z);
    }

    /// <summary>The readout's click box's size: the line's footprint (its width, its depth along the lean), from its near edge to BoxRise over its far edge.</summary>
    public static (float x, float y, float z) BoxSize(Box reader)
    {
        (float width, float height) = LineSize(reader);
        (_, float bottom, _) = NearEdge(reader);
        (_, float far, _) = FarEdge(reader);
        return (width, far + BoxRise - bottom, height * (float)Math.Cos(TiltRadians));
    }
}
