using System;
using System.Collections.Generic;

/// <summary>
/// The two fixed camera poses at the screen's aspect (CAM: BUG-41, 1b, 7b;
/// FIX_PLAN_v2: "Two fixed poses per aspect ... LensFit solves both"). Pure.
/// <para>
/// The office pose (<see cref="Fit"/>): the art office is framed for 16:9. A
/// field of view is vertical, so a screen narrower than 16:9 sees less of the
/// desk's width; there the lens widens toward the 16:9 width. The art is
/// painted to the 16:9 frame's top edge (the hall above it is bare), so the
/// view keeps that top edge and tilts down by as much as it widens, showing
/// more of the desk below; it widens no further than the desk reaches below
/// the camera (<c>bottom</c>: past it lies the floor under the desk). A
/// screen wider than the hall art covers (<see cref="ArtAspect"/>; 21:9
/// showed ~225 black columns in the top corners, BUG-41) narrows the lens so
/// the view is no wider than the art, keeping its bottom edge (the desk) and
/// giving up the bare top of the hall.
/// </para>
/// <para>
/// The reading pose (<see cref="Reading"/>): the tilted pose a desk drop
/// tilts to (D1), fixed per aspect and never re-aimed per paper. At its
/// pitch and lens it backs out, and slides along the desk, just far enough
/// that every target point (the big mat, the stamp rack and the intercom
/// whole, the inner strips of the other side homes, the deal tray) lies
/// inside the screen's margins (the top one the overlay's band of chrome),
/// centred between them, and the screen's bottom row never passes the desk's
/// front edge (under the desk is never shown).
/// </para>
/// </summary>
public static class LensFit
{
    /// <summary>The aspect the art office is framed for (16:9).</summary>
    public const float DesignAspect = 16f / 9f;

    /// <summary>The widest aspect the hall art covers at the office pose's lens (BUG-41: at 2560x1080 the art's top row ended ~225 columns short, 2335 / 1080 = 2.16; kept a little under it).</summary>
    public const float ArtAspect = 2.1f;

    /// <summary>The farthest the reading pose backs out (metres).</summary>
    public const float MaxDistance = 20f;

    /// <summary>The nearest a target may be to the camera along its view (metres).</summary>
    private const double Near = 0.05;

    /// <summary>Bisection steps (each halves the interval: 48 reach well under a micron).</summary>
    private const int Iterations = 48;

    /// <summary>The reading solve's first distance tried (metres) and the factor each next one grows by (2%: no stretch of fitting distances is missed between two tries).</summary>
    private const double ScanFrom = 0.05, ScanStep = 1.02;

    /// <summary>
    /// The vertical field of view (degrees) at <paramref name="aspect"/> and
    /// how far (degrees) the view tilts down from the art's: as wide as
    /// <paramref name="fov"/> shows at <paramref name="designAspect"/>, its
    /// top edge where the art's is, at most down to <paramref name="bottom"/>
    /// degrees below the art's forward; past <see cref="ArtAspect"/>, no wider
    /// than the art, its bottom edge where the art's is; <paramref name="fov"/>
    /// and no tilt from the design aspect to the art's (or for no aspect).
    /// </summary>
    public static (float fov, float tilt) Fit(float fov, float designAspect, float aspect, float bottom)
    {
        float half = fov / 2f;
        if (aspect <= 0f || designAspect <= 0f)
            return (fov, 0f);
        if (aspect > ArtAspect && ArtAspect >= designAspect)
        {
            float narrow = Degrees(Math.Atan(Math.Tan(Radians(half)) * ArtAspect / aspect));
            return (narrow * 2f, half - narrow);
        }
        if (aspect >= designAspect)
            return (fov, 0f);
        float wide = Degrees(Math.Atan(Math.Tan(Radians(half)) * designAspect / aspect));
        float fitted = Math.Max(half, Math.Min(wide, (half + bottom) / 2f));
        return (fitted * 2f, fitted - half);
    }

    /// <summary>
    /// The reading pose that frames <paramref name="targets"/> (points in the
    /// desk's frame: x metres right, y metres away from the chair, z metres
    /// above the desk's top) for a camera pitched <paramref name="pitch"/>
    /// degrees below the horizon toward +y, with a vertical field of view of
    /// <paramref name="fov"/> at <paramref name="aspect"/>: the least
    /// distance at which every target lies inside the screen less the
    /// margins (shares of the half screen, as in normalised device
    /// coordinates: <paramref name="top"/>, <paramref name="bottom"/>, and
    /// <paramref name="side"/> left and right), the camera centred across the
    /// targets and slid along y to centre them between the top and bottom
    /// margins, but never so far toward the chair that the screen's bottom
    /// row meets the desk's plane nearer the chair than
    /// <paramref name="frontY"/>. False for no target, a bad lens or pitch,
    /// or no pose within <see cref="MaxDistance"/>.
    /// </summary>
    public static bool Reading(IReadOnlyList<(float x, float y, float z)> targets, float pitch, float fov, float aspect,
                               float top, float bottom, float side, float frontY, out ReadingPose pose)
    {
        pose = default;
        if (targets == null || targets.Count == 0 || fov <= 0f || fov >= 179f || aspect <= 0f || pitch <= 0f || pitch >= 90f)
            return false;
        var lens = new Lens(targets, pitch, fov, aspect, top, bottom, side, frontY);
        // The distances that fit are one stretch, not every distance past the least: far back, the desk's front edge
        // holds the camera so far forward that the targets slip under the bottom margin. Step out to the first fit,
        // then narrow down to where it starts.
        double lo = 0.0, hi = 0.0;
        for (double d = ScanFrom; ; d *= ScanStep)
        {
            if (d > MaxDistance)
                return false;
            if (lens.TryAim(d, out _))
            {
                hi = d;
                break;
            }
            lo = d;
        }
        for (int i = 0; i < Iterations; i++)
        {
            double mid = (lo + hi) / 2.0;
            if (lens.TryAim(mid, out _))
                hi = mid;
            else
                lo = mid;
        }
        lens.TryAim(hi, out double aimY);
        pose = new ReadingPose((float)lens.CentreX, (float)aimY, (float)hi, (float)lens.BottomRowY(hi, aimY));
        return true;
    }

    /// <summary>
    /// Where <paramref name="point"/> (desk frame, as for <see cref="Reading"/>)
    /// shows on the screen of <paramref name="pose"/> pitched
    /// <paramref name="pitch"/> with <paramref name="fov"/> at
    /// <paramref name="aspect"/>, in normalised device coordinates (-1..1
    /// each, (0, 0) the centre, +y up); false when it lies behind the camera.
    /// </summary>
    public static bool Project(ReadingPose pose, float pitch, float fov, float aspect, (float x, float y, float z) point, out (float x, float y) ndc)
    {
        ndc = default;
        double p = Radians(pitch), t = Math.Tan(Radians(fov / 2f));
        double dy = point.y - (pose.AimY - pose.Distance * Math.Cos(p)), dz = point.z - pose.Distance * Math.Sin(p);
        double depth = dy * Math.Cos(p) - dz * Math.Sin(p);
        if (depth <= 1e-6 || t <= 0.0 || aspect <= 0f)
            return false;
        ndc = ((float)((point.x - pose.AimX) / (depth * t * aspect)), (float)((dy * Math.Sin(p) + dz * Math.Cos(p)) / (depth * t)));
        return true;
    }

    private static double Radians(double degrees) => degrees * Math.PI / 180.0;

    private static float Degrees(double radians) => (float)(radians * 180.0 / Math.PI);

    /// <summary>The reading solve's geometry: the targets seen through a pitched lens, the camera a distance back from an aim point on the desk.</summary>
    private readonly struct Lens
    {
        private readonly IReadOnlyList<(float x, float y, float z)> _points;
        private readonly double _cos, _sin, _tan, _aspect, _top, _bottom, _side, _front;

        /// <summary>The middle of the targets across the desk (the camera's x).</summary>
        public readonly double CentreX;

        public Lens(IReadOnlyList<(float x, float y, float z)> points, float pitch, float fov, float aspect, float top, float bottom, float side, float frontY)
        {
            _points = points;
            double p = Radians(pitch);
            _cos = Math.Cos(p);
            _sin = Math.Sin(p);
            _tan = Math.Tan(Radians(fov / 2f));
            _aspect = aspect;
            _top = 1.0 - Clamp01(top);
            _bottom = -(1.0 - Clamp01(bottom));
            _side = 1.0 - Clamp01(side);
            _front = frontY;
            double x0 = double.MaxValue, x1 = double.MinValue;
            foreach ((float x, float y, float z) in points)
            {
                x0 = Math.Min(x0, x);
                x1 = Math.Max(x1, x);
            }
            CentreX = (x0 + x1) / 2.0;
        }

        /// <summary>The aim along y at <paramref name="distance"/> that holds every target inside the margins and the bottom row on the desk, centred where it can be; false when none does.</summary>
        public bool TryAim(double distance, out double aimY)
        {
            aimY = 0.0;
            // The camera stays behind every target (aim below each one's limit); far enough toward the chair to see them all.
            double hi = double.MaxValue, lo = double.MaxValue;
            foreach ((float x, float y, float z) in _points)
            {
                hi = Math.Min(hi, y + (distance - z * _sin - Near) / Math.Max(_cos, 1e-4));
                lo = Math.Min(lo, y);
            }
            lo -= 3.0 * distance + 3.0;
            if (hi <= lo)
                return false;

            // Sliding the aim away from the chair moves every target down the screen, and the bottom row away from the chair.
            Lens self = this;
            bool TopHolds(double a) => self.Highest(distance, a) <= self._top && self.BottomRowY(distance, a) >= self._front;
            bool BottomHolds(double a) => self.Lowest(distance, a) >= self._bottom;
            if (!TopHolds(hi) || !BottomHolds(lo))
                return false;
            double least = Boundary(lo, hi, TopHolds);
            double most = Boundary(lo, hi, a => !BottomHolds(a));
            if (least > most)
                return false;
            double centred = Boundary(lo, hi, a => self.Highest(distance, a) + self.Lowest(distance, a) <= self._top + self._bottom);
            aimY = Math.Min(Math.Max(centred, least), most);
            return Widest(distance, aimY) <= _side;
        }

        /// <summary>Where the screen's bottom row meets the desk's plane (y), the camera <paramref name="distance"/> back from <paramref name="aimY"/>.</summary>
        public double BottomRowY(double distance, double aimY)
        {
            double down = _sin + _tan * _cos;
            return down <= 1e-9 ? double.MinValue : aimY - distance * _cos + distance * _sin / down * (_cos - _tan * _sin);
        }

        private double Highest(double distance, double aimY)
        {
            double most = double.MinValue;
            foreach ((float x, float y, float z) in _points)
                most = Math.Max(most, Up(distance, aimY, y, z));
            return most;
        }

        private double Lowest(double distance, double aimY)
        {
            double least = double.MaxValue;
            foreach ((float x, float y, float z) in _points)
                least = Math.Min(least, Up(distance, aimY, y, z));
            return least;
        }

        private double Widest(double distance, double aimY)
        {
            double most = 0.0;
            foreach ((float x, float y, float z) in _points)
            {
                double depth = Depth(distance, aimY, y, z);
                if (depth <= 1e-9)
                    return double.MaxValue;
                most = Math.Max(most, Math.Abs(x - CentreX) / (depth * _tan * _aspect));
            }
            return most;
        }

        private double Up(double distance, double aimY, double y, double z)
        {
            double depth = Depth(distance, aimY, y, z);
            double dy = y - (aimY - distance * _cos), dz = z - distance * _sin;
            return depth <= 1e-9 ? double.MaxValue : (dy * _sin + dz * _cos) / (depth * _tan);
        }

        private double Depth(double distance, double aimY, double y, double z) =>
            (y - (aimY - distance * _cos)) * _cos - (z - distance * _sin) * _sin;

        /// <summary>The least value in <paramref name="lo"/>..<paramref name="hi"/> for which <paramref name="holds"/> (false below it, true above).</summary>
        private static double Boundary(double lo, double hi, Func<double, bool> holds)
        {
            if (holds(lo))
                return lo;
            for (int i = 0; i < Iterations; i++)
            {
                double mid = (lo + hi) / 2.0;
                if (holds(mid))
                    hi = mid;
                else
                    lo = mid;
            }
            return hi;
        }

        private static double Clamp01(double v) => Math.Min(1.0, Math.Max(0.0, v));
    }
}

/// <summary>The reading pose <see cref="LensFit.Reading"/> solved, in the desk's frame (metres).</summary>
public readonly struct ReadingPose
{
    /// <summary>A pose aiming at (<paramref name="aimX"/>, <paramref name="aimY"/>) on the desk's top from <paramref name="distance"/> back along the view; its screen's bottom row meets the desk's plane at <paramref name="bottomRowY"/>.</summary>
    public ReadingPose(float aimX, float aimY, float distance, float bottomRowY)
    {
        AimX = aimX;
        AimY = aimY;
        Distance = distance;
        BottomRowY = bottomRowY;
    }

    /// <summary>Where the view's centre ray meets the desk's top: metres right of the mat's centre.</summary>
    public float AimX { get; }

    /// <summary>Where the view's centre ray meets the desk's top: metres away from the chair.</summary>
    public float AimY { get; }

    /// <summary>How far the camera sits back from the aim point along its view (metres).</summary>
    public float Distance { get; }

    /// <summary>Where the screen's bottom row meets the desk's plane (metres away from the chair; at or past the desk's front edge).</summary>
    public float BottomRowY { get; }
}
