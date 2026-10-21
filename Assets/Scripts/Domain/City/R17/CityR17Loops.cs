using System;
using System.Collections.Generic;

/// <summary>
/// The r17 city's loops as pure functions of time (CITY-R17): where each cloud is and which two drawings it is
/// between, which train pass runs and where its cars are, which tile every ad screen shows, the fire's and the
/// smoke's flipbook tiles and the spill's gain, and each noon lamp's brightness. Each is a port of the r17 page's
/// drawer (js/r17clouds.js, js/r17train.js, js/r17ads.js, js/r16.js, js/r16lights.js); the frame clock is the
/// page's 24 fps (frame f = floor(t * 24 + 1e-4)). Pure; tested in EditMode.
/// </summary>
public static class CityR17Loops
{
    /// <summary>The page's frame index at t seconds on its 24 fps clock.</summary>
    public static long Frame(double seconds, int fps = 24) => (long)Math.Floor(seconds * fps + 1e-4);

    // ------------------------------------------------------------------ clouds

    /// <summary>
    /// A cloud's left edge on the plate (plate px, before its parallax) at frame f (fractional frames move it
    /// continuously): x = A + u / a with u = (a (x0 - A) - f / k) mod a L, a the review scale the speeds were set at.
    /// The wrap from A to A + L happens off frame or behind a building.
    /// </summary>
    public static double CloudX(CityR17Cloud c, double f, double a)
    {
        double u = CityR17Json.Mod(a * (c.X0 - c.A) - f / c.K, a * c.L);
        return c.A + u / a;
    }

    /// <summary>The cloud's morph at frame f: drawing j, cross-dissolved toward drawing (j + 1) mod n by w.</summary>
    public static void CloudDrawing(CityR17Cloud c, double f, out int j, out double w)
    {
        double p = (f + c.Mph) / c.Step, fl = Math.Floor(p);
        j = (int)CityR17Json.Mod((long)fl, c.N);
        w = p - fl;
    }

    /// <summary>Drawing j's cell in its sheet (px, y down): the sheet's 2 px gutters between cells.</summary>
    public static CityR17Rect CloudCell(CityR17Cloud c, int j) => new CityR17Rect(j % c.Cols * (c.W + 2), j / c.Cols * (c.H + 2), c.W, c.H);

    // ------------------------------------------------------------------ train

    /// <summary>A train pass in progress: its direction, frames since it started (fractional allowed), its index in the cycle.</summary>
    public readonly struct TrainPass
    {
        public readonly bool Active, West;
        public readonly double K;
        public readonly int Index;

        public TrainPass(bool west, double k, int index)
        {
            Active = true;
            West = west;
            K = k;
            Index = index;
        }
    }

    /// <summary>The train's speed in device px a frame at a device scale: a whole number (round(speed sc), at least 1), so the slide never stutters.</summary>
    public static double TrainDevSpeed(CityR17TrainData d, double sc) => Math.Max(1, CityR17Json.JsRound(d.Speed * sc));

    /// <summary>The frames one pass takes at a device scale (the page's len: from beyond one matte to beyond the other, plus one).</summary>
    public static double TrainPassFrames(CityR17TrainData d, double sc) =>
        Math.Ceiling((d.ClipX - d.LeftMatte + 2 * d.Margin + d.Length) * sc / TrainDevSpeed(d, sc)) + 1;

    /// <summary>The active pass at frame f (the page's pass(f, sc)); inactive when the line is quiet.</summary>
    public static TrainPass Pass(CityR17TrainData d, double f, double sc)
    {
        double c = CityR17Json.Mod(f, d.Cycle), len = TrainPassFrames(d, sc);
        for (int i = 0; i < d.Passes.Length; i++)
        {
            double k = c - d.Passes[i].F0;
            if (k >= 0 && k < len)
                return new TrainPass(d.Passes[i].West, k, i);
        }
        return default;
    }

    /// <summary>
    /// The set's west end on the plate (plate px in the train plane's own space, before its parallax) for a pass k
    /// frames in: westbound it enters beyond the right matte and slides left; eastbound the mirror. The speed is the
    /// page's whole device px a frame, expressed in plate px.
    /// </summary>
    public static double TrainWestEnd(CityR17TrainData d, TrainPass p, double sc)
    {
        double v = TrainDevSpeed(d, sc) / sc;
        return p.West ? d.ClipX + d.Margin - v * p.K : d.LeftMatte - d.Margin - d.Length + v * p.K;
    }

    /// <summary>
    /// A car's x offset in the set and its width for a direction: westbound the drawings as painted, eastbound the
    /// same set mirrored (the cars reversed, each drawing flipped).
    /// </summary>
    public static void TrainCar(CityR17TrainData d, bool west, int slot, out int car, out double ox, out bool mirrored)
    {
        if (west)
        {
            car = slot;
            ox = d.Cars[slot].Ox;
            mirrored = false;
            return;
        }
        car = d.Cars.Length - 1 - slot;
        ox = d.Length - d.Cars[car].Ox - d.Cars[car].W;
        mirrored = true;
    }

    /// <summary>A gangway's x offset in the set for a direction (eastbound mirrored and reversed, as the cars).</summary>
    public static double TrainGangway(CityR17TrainData d, bool west, int slot)
    {
        if (west)
            return d.Gangways[slot][0];
        double[] g = d.Gangways[d.Gangways.Length - 1 - slot];
        return d.Length - g[0] - g[1];
    }

    /// <summary>
    /// A car's top on the plate: level on the deck line at the car's own centre (y = a + b x), the skirt row on the
    /// deck. <paramref name="lifted"/>: the car's slow hover is high this frame (the page lifts it one device px).
    /// </summary>
    public static double TrainCarTop(CityR17TrainData d, double carCentreX, double carW, int passIndex, int slot, double seconds, out bool lifted)
    {
        double err = d.DeckB * carW / 2;
        double y = d.DeckA + d.DeckB * carCentreX - d.SkirtAbove - err - 0.5 - d.SkirtRow;
        lifted = Math.Sin(2 * Math.PI * seconds / d.BobPeriod + (passIndex * 3 + slot) * d.BobPhase) > d.BobThresh;
        return y;
    }

    // ------------------------------------------------------------------ ads

    /// <summary>Expands a screen's run-length sequence to its tile index per frame of the master cycle.</summary>
    public static int[] Expand(int[] rle, int n)
    {
        var a = new int[n];
        int f = 0;
        for (int i = 0; i + 1 < rle.Length && f < n; i += 2)
            for (int r = 0; r < rle[i + 1] && f < n; r++)
                a[f++] = rle[i];
        for (; f < n; f++)
            a[f] = f > 0 ? a[f - 1] : 0;
        return a;
    }

    /// <summary>The total frames a run-length sequence covers.</summary>
    public static int RleFrames(int[] rle)
    {
        int n = 0;
        for (int i = 1; i < rle.Length; i += 2)
            n += rle[i];
        return n;
    }

    /// <summary>
    /// Every pair of screens that show the same ad at once (the r17 schedule's own rule, loops/ads17 build17.check):
    /// a hold lasts from its start until the next hold's start plus that change's transition (the outgoing ad is on
    /// screen until the change completes), on the cyclic master clock. Empty when the schedule is clean.
    /// </summary>
    public static List<string> DuplicateAds(CityR17AdsData ads)
    {
        var occ = new Dictionary<string, List<(string sid, double a, double b)>>(StringComparer.Ordinal);
        int L = ads.N;
        foreach (KeyValuePair<string, CityR17Hold[]> kv in ads.Holds)
        {
            CityR17Hold[] hs = kv.Value;
            for (int i = 0; i < hs.Length; i++)
            {
                CityR17Hold h = hs[i], nx = hs[(i + 1) % hs.Length];
                int tr = ads.TransitionFrames.TryGetValue(nx.Kind, out int t) ? t : 8;
                double e = h.Start + CityR17Json.Mod(nx.Start - h.Start, (long)L) + tr;
                if (!occ.TryGetValue(h.Ad, out var list))
                    occ[h.Ad] = list = new List<(string, double, double)>();
                list.Add((kv.Key, h.Start, e));
            }
        }
        var dup = new List<string>();
        foreach (KeyValuePair<string, List<(string sid, double a, double b)>> kv in occ)
        {
            List<(string sid, double a, double b)> lst = kv.Value;
            for (int i = 0; i < lst.Count; i++)
                for (int j = i + 1; j < lst.Count; j++)
                    if (lst[i].sid != lst[j].sid && Overlap(lst[i].a, lst[i].b, lst[j].a, lst[j].b, L))
                        dup.Add($"{kv.Key}: {lst[i].sid}@{lst[i].a} and {lst[j].sid}@{lst[j].a}");
        }
        return dup;
    }

    private static bool Overlap(double a0, double a1, double b0, double b1, int L)
    {
        for (int sh = -L; sh <= L; sh += L)
            if (a0 < b1 + sh && b0 + sh < a1)
                return true;
        return false;
    }

    // ------------------------------------------------------------------ flipbooks

    /// <summary>A flipbook's tile at frame f: (sheet, x, y, w, h) from a 5-per-frame sequence of n frames.</summary>
    public static void Tile(int[] seq5, long f, out int sheet, out CityR17Rect rect)
    {
        int n = seq5.Length / 5;
        int i = (int)CityR17Json.Mod(f, n) * 5;
        sheet = seq5[i];
        rect = new CityR17Rect(seq5[i + 1], seq5[i + 2], seq5[i + 3], seq5[i + 4]);
    }

    /// <summary>
    /// A tile's source rect clipped to its sheet's painted pixels, as the canvas clips a drawImage source rect (a few
    /// of the page's tiles reach past their sheet's edge): the destination shrinks by the same pixels, so the part
    /// that draws lands where it did. dx, dy: the destination's offset from the tile's place. False when nothing is left.
    /// </summary>
    public static bool ClipTile(CityR17Rect src, double usedW, double usedH, out CityR17Rect clipped, out double dx, out double dy)
    {
        double x0 = Math.Max(0, src.X), y0 = Math.Max(0, src.Y);
        double x1 = Math.Min(usedW, src.X + src.W), y1 = Math.Min(usedH, src.Y + src.H);
        dx = x0 - src.X;
        dy = y0 - src.Y;
        clipped = new CityR17Rect(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0));
        return x1 > x0 && y1 > y0;
    }

    /// <summary>The furnace spill's additive gain at frame f (it flickers with the fire's brightness).</summary>
    public static double SpillGain(CityR17FireData fire, long f) => fire.Gain[(int)CityR17Json.Mod(CityR17Json.Mod(f, fire.N), fire.Gain.Length)];

    // ------------------------------------------------------------------ noon lamps (js/r16lights.js)

    private static double Ss(double a, double b, double x)
    {
        double u = Math.Max(0, Math.Min(1, (x - a) / (b - a)));
        return u * u * (3 - 2 * u);
    }

    /// <summary>A pulse over one period: rise r, hold h, fall d, then off.</summary>
    public static double Pulse(double t, double r, double h, double d) => t < r ? Ss(0, r, t) : t < r + h ? 1 : 1 - Ss(r + h, r + h + d, t);

    /// <summary>A soft bump: a 0..w window with eased edges of width e.</summary>
    public static double Bump(double x, double w, double e) => Ss(0, e, x) * (1 - Ss(w - e, w, x));

    /// <summary>The noon look of a lamp kind: its cel lamp radius (plate px) and colour (0..1).</summary>
    public static void LampLook(CityR17LampKind kind, out double radius, out double r, out double g, out double b)
    {
        switch (kind)
        {
            case CityR17LampKind.Seam: radius = 1.4; r = 0xff / 255.0; g = 0xf6 / 255.0; b = 0xd6 / 255.0; break;
            case CityR17LampKind.Band: radius = 1.3; r = 0xcf / 255.0; g = 0xef / 255.0; b = 0xff / 255.0; break;
            case CityR17LampKind.Ring: radius = 1.9; r = 0xff / 255.0; g = 0xf4 / 255.0; b = 0xd0 / 255.0; break;
            case CityR17LampKind.ExTop: radius = 1.8; r = 0xff / 255.0; g = 0x3b / 255.0; b = 0x30 / 255.0; break;
            default: radius = 2.0; r = 0xff / 255.0; g = 0x3b / 255.0; b = 0x30 / 255.0; break;
        }
    }

    /// <summary>
    /// A noon lamp's brightness at t seconds (no hard blinks: every lamp eases in and out):
    /// the Hall column seam rises as a soft bump (5.2 s), the plinth band chases (half speed), the ring's chase head is a
    /// soft bump (7.5 s), the Exchange spire markers pulse every 2.4 s, the top-row aviation warnings every 3 s per tower.
    /// </summary>
    public static double LampAlpha(CityR17Lamp lamp, double t)
    {
        switch (lamp.Kind)
        {
            case CityR17LampKind.Seam:
            {
                double seamPh = t / 5.2 % 1, u = 1 - (lamp.Y - 494) / (800 - 494);
                return 0.45 * Bump(seamPh * 1.3 - 0.15 - u, 0.12, 0.05);
            }
            case CityR17LampKind.Band:
            {
                double ph = ((t * 0.35 - lamp.X / 300) % 1 + 1) % 1;
                return 0.45 * Bump(ph, 0.16, 0.06);
            }
            case CityR17LampKind.Ring:
            {
                double ringHead = t / 7.5 % 1 * 1.25 - 0.1, u = lamp.Index / (double)Math.Max(1, lamp.Count - 1);
                return 0.5 * Bump(ringHead - u, 0.12, 0.04);
            }
            case CityR17LampKind.ExTop:
                return 0.65 * Pulse((t + lamp.Index * 0.5) % 2.4, 0.21, 0.5, 0.25);
            default:
                return 0.7 * Pulse((t + lamp.Group % 7 * 0.63) % 3.0, 0.21, 0.35, 0.25);
        }
    }
}
