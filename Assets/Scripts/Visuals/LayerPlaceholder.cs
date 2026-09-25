using System.Collections.Generic;

/// <summary>Where a placeholder layer is drawn on the character canvas.</summary>
public enum PlaceholderRegion
{
    /// <summary>Hair falling behind the head and shoulders.</summary>
    HairBehind,

    /// <summary>The mannequin: neck, torso, arms, hands and legs.</summary>
    Body,

    /// <summary>Clothes from the shoulders to the knees, with sleeves.</summary>
    Clothes,

    /// <summary>The head oval.</summary>
    Head,

    /// <summary>A beard below the mouth.</summary>
    Beard,

    /// <summary>A hair cap over the top of the head.</summary>
    HairCap,

    /// <summary>A hat: a crown in the headroom and a brim at the head top.</summary>
    Hat,

    /// <summary>A collar band at the shoulders.</summary>
    Collar,

    /// <summary>A whole figure (a premade).</summary>
    WholeFigure
}

/// <summary>An expression mark drawn on a placeholder's head.</summary>
public enum PlaceholderMark
{
    /// <summary>No mark.</summary>
    None,

    /// <summary>A flat mouth.</summary>
    Neutral,

    /// <summary>A smile.</summary>
    Happy,

    /// <summary>A flat mouth under brows slanted down to the middle.</summary>
    Angry,

    /// <summary>A frown under brows raised in the middle.</summary>
    Worried
}

/// <summary>
/// Draws a character layer placeholder: a flat shape per layer on the
/// canvas at quarter size (so it lines up with final art), filled with one
/// colour and bordered (or, for a whole figure, striped outside the head) with
/// another, plus an optional expression mark on the head. RGBA32, row 0 = bottom (like Texture2D raw
/// data), transparent outside the shape. Pure, so it is tested headless;
/// CharacterArt turns the bytes into a texture when a key has no final art.
/// </summary>
public static class LayerPlaceholder
{
    /// <summary>Placeholder width (the canvas at quarter size; not a gameplay knob).</summary>
    public const int Width = LookCanvas.Width / Scale;

    /// <summary>Placeholder height.</summary>
    public const int Height = LookCanvas.Height / Scale;

    /// <summary>Canvas pixels per placeholder pixel.</summary>
    private const int Scale = 4;

    /// <summary>Border width in placeholder pixels.</summary>
    private const int Border = 3;

    /// <summary>Stripe period of a whole figure, in placeholder pixels.</summary>
    private const int StripePeriod = 12;

    private const int CX = LookCanvas.CenterX;
    private const int S = LookCanvas.Shoulders;
    private const int W = LookCanvas.Waist;
    private const int Hi = LookCanvas.Hips;
    private const int K = LookCanvas.Knees;
    private const int Ankle = LookCanvas.Feet - 60;

    /// <summary>The guide's torso (canvas pixels, top-left origin).</summary>
    private static readonly (float x, float y)[] Torso =
        { (CX - 170, S), (CX + 170, S), (CX + 120, W), (CX + 140, Hi), (CX - 140, Hi), (CX - 120, W) };

    /// <summary>The guide's arms.</summary>
    private static readonly (float x, float y)[][] Arms =
    {
        new (float x, float y)[] { (CX - 170, S), (CX - 130, S + 30), (CX - 175, 930), (CX - 215, 925) },
        new (float x, float y)[] { (CX + 170, S), (CX + 130, S + 30), (CX + 175, 930), (CX + 215, 925) }
    };

    /// <summary>The guide's legs, down to the soles.</summary>
    private static readonly (float x, float y)[][] Legs =
    {
        new (float x, float y)[] { (CX - 140, Hi), (CX - 8, Hi), (CX - 26, LookCanvas.Feet), (CX - 124, LookCanvas.Feet) },
        new (float x, float y)[] { (CX + 8, Hi), (CX + 140, Hi), (CX + 124, LookCanvas.Feet), (CX + 26, LookCanvas.Feet) }
    };

    /// <summary>Clothes: the torso widened, down to the knees.</summary>
    private static readonly (float x, float y)[] Dress =
        { (CX - 182, S - 8), (CX + 182, S - 8), (CX + 138, W), (CX + 175, K), (CX - 175, K), (CX - 138, W) };

    /// <summary>Sleeves: the arms widened, to the elbows.</summary>
    private static readonly (float x, float y)[][] Sleeves =
    {
        new (float x, float y)[] { (CX - 182, S - 8), (CX - 120, S + 20), (CX - 150, 740), (CX - 205, 730) },
        new (float x, float y)[] { (CX + 182, S - 8), (CX + 120, S + 20), (CX + 150, 740), (CX + 205, 730) }
    };

    /// <summary>The head oval's centre height and radii.</summary>
    private const float HeadCy = (LookCanvas.HeadTop + LookCanvas.Chin) / 2f, HeadRx = 62f, HeadRy = (LookCanvas.Chin - LookCanvas.HeadTop) / 2f;

    /// <summary>The mouth's height (canvas pixels from the top).</summary>
    private const float MouthY = 392f;

    /// <summary>
    /// Renders a region with a fill, an inner border (stripes for a whole
    /// figure) in the accent, and a mark in the accent on the head.
    /// </summary>
    public static byte[] Render(PlaceholderRegion region, (byte r, byte g, byte b) fill, (byte r, byte g, byte b) accent, PlaceholderMark mark)
    {
        var inside = new bool[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            float cy = (Height - 1 - y + 0.5f) * Scale;
            for (int x = 0; x < Width; x++)
                inside[y * Width + x] = In(region, (x + 0.5f) * Scale, cy);
        }

        var rgba = new byte[Width * Height * 4];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!inside[y * Width + x])
                    continue;

                bool edge = !At(inside, x - Border, y) || !At(inside, x + Border, y) || !At(inside, x, y - Border) || !At(inside, x, y + Border);
                bool stripe = region == PlaceholderRegion.WholeFigure && (x + y) % StripePeriod < Border &&
                              !PixelShapes.InEllipse(CX, HeadCy, HeadRx, HeadRy, (x + 0.5f) * Scale, (Height - 1 - y + 0.5f) * Scale);
                Put(rgba, x, y, edge || stripe ? accent : fill);
            }
        }

        if (mark != PlaceholderMark.None)
            foreach ((int x, int y) in MarkPixels(mark))
                Put(rgba, x, y, accent);

        return rgba;
    }

    /// <summary>Whether a canvas point (top-left pixels) lies in the region.</summary>
    private static bool In(PlaceholderRegion region, float px, float py)
    {
        switch (region)
        {
            case PlaceholderRegion.HairBehind:
                return PixelShapes.InEllipse(CX, 330f, 78f, 95f, px, py) ||
                       (px >= CX - 78 && px <= CX + 78 && py >= 330f && py <= 620f);
            case PlaceholderRegion.Body:
                return InBody(px, py);
            case PlaceholderRegion.Clothes:
                return PixelShapes.InPolygon(Dress, px, py) || PixelShapes.InPolygon(Sleeves[0], px, py) || PixelShapes.InPolygon(Sleeves[1], px, py);
            case PlaceholderRegion.Head:
                return PixelShapes.InEllipse(CX, HeadCy, HeadRx, HeadRy, px, py);
            case PlaceholderRegion.Beard:
                return py >= MouthY + 6f && PixelShapes.InEllipse(CX, HeadCy + 10f, HeadRx + 6f, HeadRy + 22f, px, py);
            case PlaceholderRegion.HairCap:
                return py <= HeadCy - 18f && PixelShapes.InEllipse(CX, HeadCy - 6f, HeadRx + 8f, HeadRy + 8f, px, py);
            case PlaceholderRegion.Hat:
                return (px >= CX - 58 && px <= CX + 58 && py >= 150f && py <= 275f) ||
                       (px >= CX - 96 && px <= CX + 96 && py >= 255f && py <= 282f);
            case PlaceholderRegion.Collar:
                return PixelShapes.InEllipse(CX, S + 18f, 128f, 62f, px, py) && !PixelShapes.InEllipse(CX, S - 12f, 64f, 40f, px, py);
            default:
                return InBody(px, py) || PixelShapes.InEllipse(CX, HeadCy, HeadRx, HeadRy, px, py) || PixelShapes.InPolygon(Dress, px, py);
        }
    }

    /// <summary>The mannequin: neck, torso, arms, hands and legs.</summary>
    private static bool InBody(float px, float py)
    {
        if (px >= CX - 26 && px <= CX + 26 && py >= LookCanvas.Chin - 10 && py <= S + 10)
            return true;
        if (PixelShapes.InPolygon(Torso, px, py) || PixelShapes.InPolygon(Arms[0], px, py) || PixelShapes.InPolygon(Arms[1], px, py))
            return true;
        if (PixelShapes.InEllipse(CX - 200, 950, 32, 35, px, py) || PixelShapes.InEllipse(CX + 200, 950, 32, 35, px, py))
            return true;
        return py <= Ankle + 60 && (PixelShapes.InPolygon(Legs[0], px, py) || PixelShapes.InPolygon(Legs[1], px, py));
    }

    /// <summary>The mark's pixels (placeholder coordinates, row 0 = bottom): a mouth curve, and brows for angry and worried.</summary>
    private static IEnumerable<(int x, int y)> MarkPixels(PlaceholderMark mark)
    {
        const float half = 24f;
        float curve = mark == PlaceholderMark.Happy ? -12f : mark == PlaceholderMark.Worried ? 12f : 0f;
        for (float dx = -half; dx <= half; dx += 1f)
        {
            float t = dx / half;
            float y = MouthY + curve * t * t;
            foreach ((int x, int y) p in Dot(CX + dx, y))
                yield return p;
        }

        if (mark != PlaceholderMark.Angry && mark != PlaceholderMark.Worried)
            yield break;

        // Brows: angry slopes down towards the middle, worried up towards it.
        float slope = mark == PlaceholderMark.Angry ? 0.35f : -0.35f;
        for (float dx = 10f; dx <= 36f; dx += 1f)
        {
            float y = 312f - slope * (36f - dx);
            foreach ((int x, int y) p in Dot(CX - dx, y))
                yield return p;
            foreach ((int x, int y) p in Dot(CX + dx, y))
                yield return p;
        }
    }

    /// <summary>A 2 x 2 placeholder-pixel dot at a canvas point.</summary>
    private static IEnumerable<(int x, int y)> Dot(float px, float py)
    {
        int x = (int)(px / Scale);
        int y = Height - 1 - (int)(py / Scale);
        for (int dy = 0; dy < 2; dy++)
            for (int dx = 0; dx < 2; dx++)
                if (x + dx >= 0 && x + dx < Width && y - dy >= 0 && y - dy < Height)
                    yield return (x + dx, y - dy);
    }

    /// <summary>The mask at a pixel; false outside the grid.</summary>
    private static bool At(bool[] mask, int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height && mask[y * Width + x];

    /// <summary>Writes an opaque pixel.</summary>
    private static void Put(byte[] rgba, int x, int y, (byte r, byte g, byte b) c)
    {
        int i = (y * Width + x) * 4;
        rgba[i] = c.r;
        rgba[i + 1] = c.g;
        rgba[i + 2] = c.b;
        rgba[i + 3] = 255;
    }
}
