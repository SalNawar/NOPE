using System;

/// <summary>
/// The r17 city's camera (CITY-R17): the look-dev's mouse parallax (js/core.js updateCamera and setDepth) with the
/// game's comfort rules on top, and the framing of the plate in the output.
/// <list type="bullet">
/// <item>The camera offset (X, Y in -1..1) follows its target through the page's critically damped spring (time
/// constant <see cref="Ease"/>, 8 ms substeps); something at parallax f shifts by -X ParallaxPx f (and
/// -Y ParallaxPx ParallaxY f) plate px.</item>
/// <item>BUG-69 ("the panorama lurches with the real mouse"): the shift of the nearest plane (f = 1) never moves more
/// than <see cref="RateCapPx"/> output px a second (30: 1 px per 30 fps frame).</item>
/// <item>Reduced Motion: no parallax (the camera rests at 0).</item>
/// <item>No idle drift and no dolly (Saleh's motion-sickness rule: sway is off; the page's drift is opt-in).</item>
/// </list>
/// Pure; tested in EditMode.
/// </summary>
public sealed class CityR17Look
{
    /// <summary>Plate px of shift at parallax 1 for an offset of 1 (the page's parallaxPx, 45).</summary>
    public double ParallaxPx = 45;

    /// <summary>The vertical share of the parallax (the page's parallaxY, 0.25).</summary>
    public double ParallaxY = 0.25;

    /// <summary>The follow spring's time constant (s; the page's ease, 0.45).</summary>
    public double Ease = 0.45;

    /// <summary>The fastest the nearest plane may move (output px a second; BUG-69: 1 px per 30 fps frame).</summary>
    public double RateCapPx = 30;

    /// <summary>The camera offset (-1..1 each way).</summary>
    public double X, Y;

    private double _vx, _vy;

    /// <summary>A look at rest with the scene's knobs.</summary>
    public static CityR17Look For(CityR17Scene s) => new CityR17Look { ParallaxPx = s.ParallaxPx, ParallaxY = s.ParallaxY, Ease = s.Ease };

    /// <summary>
    /// One frame: follow (tx, ty) through the spring, then hold the nearest plane under the rate cap at this
    /// output scale. Reduced Motion snaps to rest. Returns how far the nearest plane moved (output px).
    /// </summary>
    public double Step(double dt, double tx, double ty, double pxPerPlate, bool reducedMotion)
    {
        if (reducedMotion)
        {
            X = Y = _vx = _vy = 0;
            return 0;
        }
        if (dt <= 0)
            return 0;
        double x0 = X, y0 = Y;
        double w = 2 / Math.Max(0.05, Ease), k = w * w, c = 2 * w;
        int steps = Math.Max(1, (int)Math.Ceiling(dt / 0.008));
        double h = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            _vx += (k * (tx - X) - c * _vx) * h;
            X += _vx * h;
            _vy += (k * (ty - Y) - c * _vy) * h;
            Y += _vy * h;
        }
        double perUnitX = ParallaxPx * pxPerPlate, perUnitY = ParallaxPx * ParallaxY * pxPerPlate;
        if (RateCapPx > 0 && perUnitX > 0)
        {
            double cap = RateCapPx * dt / perUnitX;
            if (Math.Abs(X - x0) > cap)
            {
                X = x0 + Math.Sign(X - x0) * cap;
                _vx = (X - x0) / dt;
            }
        }
        if (RateCapPx > 0 && perUnitY > 0)
        {
            double cap = RateCapPx * dt / perUnitY;
            if (Math.Abs(Y - y0) > cap)
            {
                Y = y0 + Math.Sign(Y - y0) * cap;
                _vy = (Y - y0) / dt;
            }
        }
        return Math.Max(Math.Abs(X - x0) * perUnitX, Math.Abs(Y - y0) * perUnitY);
    }

    /// <summary>Puts the camera at an offset with no motion (a cut).</summary>
    public void Snap(double x, double y)
    {
        X = x;
        Y = y;
        _vx = _vy = 0;
    }

    /// <summary>The horizontal shift (plate px) of something at parallax f.</summary>
    public double ShiftX(double f) => -X * ParallaxPx * f;

    /// <summary>The vertical shift (plate px) of something at parallax f.</summary>
    public double ShiftY(double f) => -Y * ParallaxPx * ParallaxY * f;
}

/// <summary>
/// How the plate fills the output (CITY-R17). The look-dev overscans just enough that the largest parallax never
/// shows a plane's edge (the planes carry about 85 px of painted margin each side but none top and bottom, so the
/// vertical parallax sets it: 1.0213), then either shows that whole frame letterboxed (21:9, the look-dev's video)
/// or fills the output's height and pans across the plate with the look (no bars).
/// </summary>
public readonly struct CityR17Framing
{
    /// <summary>The plate rect the output shows at rest (plate px).</summary>
    public readonly double X, Y, W, H;

    /// <summary>Output px per plate px.</summary>
    public readonly double PxPerPlate;

    /// <summary>How far the shown rect can pan each way (fill mode; plate px).</summary>
    public readonly double MaxPan;

    /// <summary>The output size (px) to render: the letterboxed frame or the whole output.</summary>
    public readonly int OutW, OutH;

    public CityR17Framing(double x, double y, double w, double h, double pxPerPlate, double maxPan, int outW, int outH)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
        PxPerPlate = pxPerPlate;
        MaxPan = maxPan;
        OutW = outW;
        OutH = outH;
    }

    /// <summary>The look-dev's overscan: the scale over the plate the planes' margins and the parallax need.</summary>
    public static double Overscan(CityR17Scene s)
    {
        double PW = s.PlateW, PH = s.PlateH, need = 1;
        foreach (CityR17Plane p in s.Planes)
        {
            double f = p.Parallax;
            CityR17Rect R = p.Frame;
            double hx = Math.Min(-R.X, R.X + R.W - PW), hy = Math.Min(-R.Y, R.Y + R.H - PH);
            double nx = s.ParallaxPx * f + 1 - hx, ny = s.ParallaxPx * s.ParallaxY * f + 1 - hy;
            need = Math.Max(need, Math.Max((PW + 2 * Math.Max(0, nx)) / PW, (PH + 2 * Math.Max(0, ny)) / PH));
        }
        return need;
    }

    /// <summary>
    /// The framing for a screen of w x h px, at a render scale (1: native), letterboxed (the whole frame inside the
    /// screen) or filling it (the screen's aspect, panning across the plate).
    /// </summary>
    public static CityR17Framing For(CityR17Scene s, int screenW, int screenH, bool fill, double renderScale = 1)
    {
        screenW = Math.Max(16, screenW);
        screenH = Math.Max(16, screenH);
        double need = Overscan(s), fw = s.PlateW / need, fh = s.PlateH / need, aspect = (double)screenW / screenH;
        double vw, vh;
        int ow, oh;
        if (fill)
        {
            vh = fh;
            vw = vh * aspect;
            if (vw > fw)
            {
                vw = fw;
                vh = vw / aspect;
            }
            ow = screenW;
            oh = screenH;
        }
        else
        {
            vw = fw;
            vh = fh;
            if (aspect > fw / fh)
            {
                oh = screenH;
                ow = (int)Math.Round(screenH * fw / fh);
            }
            else
            {
                ow = screenW;
                oh = (int)Math.Round(screenW * fh / fw);
            }
        }
        ow = Math.Max(16, (int)Math.Round(ow * renderScale));
        oh = Math.Max(16, (int)Math.Round(oh * renderScale));
        double pan = Math.Max(0, (fw - vw) / 2);
        return new CityR17Framing((s.PlateW - vw) / 2, (s.PlateH - vh) / 2, vw, vh, ow / vw, pan, ow, oh);
    }
}
