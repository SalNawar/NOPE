using System;
using System.Collections.Generic;

/// <summary>
/// The r17 flying traffic's simulation (CITY-R17), a line-for-line port of the look-dev's
/// <c>city/live/js/r17traffic.js</c> so the city in Unity moves exactly as the approved page (EditMode tests replay
/// the page's own golden record: every vehicle after the prefill and after 48 frames).
/// <list type="bullet">
/// <item>Slot clocks: each root lane's r14 conveyor decides which slot reaches the lane start when and which
/// vehicle type it carries (the lane's table); spawns happen at the hidden lane starts, despawns at the hidden
/// lane ends; diverge and merge ramps take their slots of the parent's clock.</item>
/// <item>Car-following: IDM per vehicle (desired speed, time gap, s0, a, b) with a curve-speed cap looked ahead
/// along each lane; merging ramp vehicles pick their place in the parent stream once.</item>
/// <item>Lane changes (MOBIL-lite) between the two decks of a corridor, at most one start per corridor every few
/// seconds, with the indicator first.</item>
/// <item>Attitude per frame: heading through a critically damped spring, bank from the lateral acceleration,
/// thrust pitch from the longitudinal one, a hover bob; the pinhole projection, the parallax factor, the slot
/// (which plane a vehicle draws after: the lane slot rules), the haze and the ink weight.</item>
/// </list>
/// The sim runs in fixed substeps (1/120 s) of each frame's dt on its own clock. The frame path allocates
/// nothing (vehicles are pooled, lists keep their capacity).
/// </summary>
public sealed class CityR17Traffic
{
    private const double Deg = Math.PI / 180.0, G = 9.81;

    private readonly CityR17TrafficData _data;
    private readonly CityR17Models _models;
    private readonly CityR17Rng _rnd;
    private readonly List<Lane> _lanes = new List<Lane>();
    private readonly Dictionary<string, Lane> _byId = new Dictionary<string, Lane>(StringComparer.Ordinal);
    private readonly List<Vehicle> _all = new List<Vehicle>(2048);
    private readonly Stack<Vehicle> _pool = new Stack<Vehicle>(256);
    private readonly Dictionary<string, double> _corrClock = new Dictionary<string, double>(StringComparer.Ordinal);
    private readonly TypeInfo[] _types;
    private readonly Dictionary<string, int> _typeIndex = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly double[] _pA = new double[3], _pB = new double[3], _tA = new double[3], _tB = new double[3], _kA = new double[3], _kB = new double[3];
    private readonly double[] _a = new double[3], _b = new double[3], _t1 = new double[3], _t2 = new double[3], _c = new double[3];
    private int _nextId = 1;
    private double _simT;

    /// <summary>A sim on the page's lanes, types and seed, lanes built and not yet filled (<see cref="Prefill"/>).</summary>
    public CityR17Traffic(CityR17TrafficData data, CityR17Models models)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _models = models ?? throw new ArgumentNullException(nameof(models));
        _rnd = new CityR17Rng(data.Seed);
        _types = new TypeInfo[data.Types.Length];
        for (int i = 0; i < data.Types.Length; i++)
        {
            string type = data.Types[i];
            _typeIndex[type] = i;
            _types[i] = MakeType(type);
        }
        InitLanes();
    }

    /// <summary>The sim clock (s): the page's T0 plus everything stepped since.</summary>
    public double SimT => _simT;

    /// <summary>Live vehicles (ghosts of a lane change counted once).</summary>
    public int Count => _all.Count;

    /// <summary>Lanes in the page's order.</summary>
    public int LaneCount => _lanes.Count;

    /// <summary>A lane's id.</summary>
    public string LaneId(int lane) => _lanes[lane].Id;

    /// <summary>Vehicles spawned since the last prefill (hidden lane starts).</summary>
    public int Spawned { get; private set; }

    /// <summary>Vehicles despawned since the last prefill (hidden lane ends).</summary>
    public int Despawned { get; private set; }

    /// <summary>The live vehicles in the sim's own order (read-only use: tests, the renderer).</summary>
    public IReadOnlyList<Vehicle> Vehicles => _all;

    // ------------------------------------------------------------------ types

    private sealed class TypeInfo
    {
        public string Name, Model;
        public int ModelIndex;
        public bool Heavy;
        public double Len, Sx, Sy, Sz;
        public double[][][] Livery;
    }

    private TypeInfo MakeType(string type)
    {
        string model = _data.ModelOf.TryGetValue(type, out string m) ? m : "coupe";
        int mi = CityR17Models.IndexOf(model);
        double[] nominal = _models.Models[mi].Nominal;
        double[] dims = _data.Dims.TryGetValue(type, out double[] d) ? d : nominal;
        string fam = _data.LiveryOf.TryGetValue(type, out string f) ? f : null;
        double[][][] liv = fam != null && _data.Liveries.TryGetValue(fam, out double[][][] l) ? l : _data.Liveries["car"];
        return new TypeInfo
        {
            Name = type,
            Model = model,
            ModelIndex = mi,
            Heavy = _data.Heavy.Contains(type),
            Len = dims[0],
            Sx = dims[1] / nominal[1],
            Sy = dims[2] / nominal[2],
            Sz = dims[0] / nominal[0],
            Livery = liv,
        };
    }

    // ------------------------------------------------------------------ lanes

    internal sealed class SpawnEntry
    {
        public int Type;
        public Lane ExitTo;
    }

    internal struct MergeEntry
    {
        public double At;
        public int Type;
    }

    /// <summary>A lane as the sim runs it.</summary>
    public sealed class Lane
    {
        public string Id, Corridor, Dir;
        public CityR17LaneDef D;
        public double[] P, S;
        public int N;
        public double L, H, Speed, Tbase;
        public float[] Cap;
        public Lane Parent, Sib;
        public readonly List<Lane> Kids = new List<Lane>();
        public readonly List<Vehicle> Vs = new List<Vehicle>(64);
        internal readonly List<SpawnEntry> Queue = new List<SpawnEntry>(8);
        internal readonly List<MergeEntry> MergeQ = new List<MergeEntry>(32);
        public bool HasLastK;
        public long LastK;
        public bool IsK, Pinned, Capped;
        public int PinSlot, CapSlot;
        public double[][] Fix;
    }

    private readonly Stack<SpawnEntry> _spawnPool = new Stack<SpawnEntry>(64);

    private SpawnEntry NewSpawn(int type, Lane exitTo)
    {
        SpawnEntry e = _spawnPool.Count > 0 ? _spawnPool.Pop() : new SpawnEntry();
        e.Type = type;
        e.ExitTo = exitTo;
        return e;
    }

    private void InitLanes()
    {
        foreach (CityR17LaneDef d in _data.Lanes)
        {
            int n = d.Pts.Length / 3;
            var P = (double[])d.Pts.Clone();
            var Sx = new double[n];
            for (int i = 1; i < n; i++)
            {
                double dx = P[3 * i] - P[3 * i - 3], dy = P[3 * i + 1] - P[3 * i - 2], dz = P[3 * i + 2] - P[3 * i - 1];
                Sx[i] = Sx[i - 1] + Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            var l = new Lane
            {
                Id = d.Id, D = d, P = P, S = Sx, N = n, L = Sx[n - 1], Corridor = d.Corridor, Dir = d.Dir, H = d.Headway, Speed = d.Speed,
                IsK = d.Corridor == "K",
            };
            if (_data.LaneSlot.TryGetValue(d.Id, out int pin))
            {
                l.Pinned = true;
                l.PinSlot = pin;
            }
            if (_data.LaneCap.TryGetValue(d.Id, out int cap))
            {
                l.Capped = true;
                l.CapSlot = cap;
            }
            _data.SlotFix.TryGetValue(d.Id, out l.Fix);
            _lanes.Add(l);
            _byId[d.Id] = l;
        }
        foreach (Lane l in _lanes)
            if (l.D.Parent != null && _byId.TryGetValue(l.D.Parent, out Lane p))
            {
                l.Parent = p;
                p.Kids.Add(l);
            }
        // lane-change siblings: the two decks of one corridor direction (both roots, same length; the last match wins, as the page)
        foreach (Lane a in _lanes)
        {
            if (a.Parent != null)
                continue;
            foreach (Lane b in _lanes)
                if (b != a && b.Parent == null && b.Corridor == a.Corridor && b.Dir == a.Dir && Math.Abs(b.L - a.L) < 0.02 * a.L)
                    a.Sib = b;
        }
        foreach (Lane l in _lanes)
        {
            Lane root = l.Parent ?? l;
            l.Tbase = CityR17Json.Clamp((root.H - 5.2 - 3) / root.Speed, 0.18, 1.2);
        }
        // curve speed: a 5 m grid of caps (lateral acceleration at most ACURVE), looked ahead with 1.5 m/s2, as a fraction of the
        // lane speed (float32, as the page's Float32Array); ramps have none
        double acurve = _data.ACurve;
        foreach (Lane l in _lanes)
        {
            Lane root = l.Parent ?? l;
            int n = (int)Math.Ceiling(l.L / 5) + 1;
            var cap = new float[n];
            for (int i = 0; i < n; i++)
            {
                double s = Math.Min(l.L, i * 5);
                PosAt(l, s - 20, _a);
                PosAt(l, s, _b);
                PosAt(l, s + 20, _c);
                double ux = _b[0] - _a[0], uy = _b[1] - _a[1], uz = _b[2] - _a[2], vx = _c[0] - _b[0], vy = _c[1] - _b[1], vz = _c[2] - _b[2];
                double nu = Math.Sqrt(ux * ux + uy * uy + uz * uz), nv = Math.Sqrt(vx * vx + vy * vy + vz * vz);
                if (nu == 0)
                    nu = 1;
                if (nv == 0)
                    nv = 1;
                double cs = CityR17Json.Clamp((ux * vx + uy * vy + uz * vz) / (nu * nv), -1, 1), k = Math.Acos(cs) / 20;
                cap[i] = (float)Math.Min(root.Speed * 1.2, k > 1e-6 ? Math.Sqrt(acurve / k) : 1e9);
            }
            for (int i = n - 2; i >= 0; i--)
                cap[i] = (float)Math.Min(cap[i], Math.Sqrt((double)cap[i + 1] * cap[i + 1] + 2 * 1.5 * 5));
            for (int i = 0; i < n; i++)
                cap[i] = l.Parent != null ? 1.2f : (float)(cap[i] / root.Speed);
            l.Cap = cap;
        }
    }

    private static double CapAt(Lane l, double s)
    {
        int i = (int)CityR17Json.Clamp(CityR17Json.JsRound(s / 5), 0, l.Cap.Length - 1);
        return l.Cap[i];
    }

    /// <summary>A lane's centreline point at arc length s (straight extrapolation beyond the ends).</summary>
    public static void PosAt(Lane l, double s, double[] o)
    {
        double[] P = l.P;
        int i;
        if (s <= 0)
            i = 0;
        else if (s >= l.L)
            i = l.N - 2;
        else
        {
            int lo = 0, hi = l.N - 1;
            while (hi - lo > 1)
            {
                int m = (lo + hi) >> 1;
                if (l.S[m] <= s)
                    lo = m;
                else
                    hi = m;
            }
            i = lo;
        }
        double dx = P[3 * i + 3] - P[3 * i], dy = P[3 * i + 4] - P[3 * i + 1], dz = P[3 * i + 5] - P[3 * i + 2];
        double nn = l.S[i + 1] - l.S[i];
        if (nn == 0)
            nn = 1e-9;
        double t = (s - l.S[i]) / nn;
        o[0] = P[3 * i] + dx * t;
        o[1] = P[3 * i + 1] + dy * t;
        o[2] = P[3 * i + 2] + dz * t;
    }

    /// <summary>
    /// <see cref="PosAt"/> walking from a remembered segment instead of a binary search: the same segment (the unique i
    /// with S[i] &lt;= s &lt; S[i + 1]), so the same point, at a fraction of the cost for a vehicle that moved a little.
    /// </summary>
    private static void PosAtH(Lane l, double s, ref int hint, double[] o)
    {
        double[] P = l.P, S = l.S;
        int i;
        if (s <= 0)
            i = 0;
        else if (s >= l.L)
            i = l.N - 2;
        else
        {
            i = hint < 0 ? 0 : hint > l.N - 2 ? l.N - 2 : hint;
            while (i > 0 && S[i] > s)
                i--;
            while (i < l.N - 2 && S[i + 1] <= s)
                i++;
            hint = i;
        }
        double dx = P[3 * i + 3] - P[3 * i], dy = P[3 * i + 4] - P[3 * i + 1], dz = P[3 * i + 5] - P[3 * i + 2];
        double nn = S[i + 1] - S[i];
        if (nn == 0)
            nn = 1e-9;
        double t = (s - S[i]) / nn;
        o[0] = P[3 * i] + dx * t;
        o[1] = P[3 * i + 1] + dy * t;
        o[2] = P[3 * i + 2] + dz * t;
    }

    private void TanAtH(Lane l, double s, ref int hint, double[] o)
    {
        PosAtH(l, s + 4, ref hint, _a);
        PosAtH(l, s - 4, ref hint, _b);
        double x = _a[0] - _b[0], y = _a[1] - _b[1], z = _a[2] - _b[2], n = Math.Sqrt(x * x + y * y + z * z);
        if (n == 0)
            n = 1;
        o[0] = x / n;
        o[1] = y / n;
        o[2] = z / n;
    }

    private void CurvAtH(Lane l, double s, ref int hint, double[] o)
    {
        TanAtH(l, s + 6, ref hint, _t1);
        TanAtH(l, s - 6, ref hint, _t2);
        o[0] = (_t1[0] - _t2[0]) / 12;
        o[1] = (_t1[1] - _t2[1]) / 12;
        o[2] = (_t1[2] - _t2[2]) / 12;
    }

    // ------------------------------------------------------------------ vehicles

    /// <summary>One vehicle (pooled; <see cref="Id"/> is never reused).</summary>
    public sealed class Vehicle
    {
        public int Id, Type, Model;
        public bool Heavy;
        public double Len, Sx, Sy, Sz;
        public double Ar, Ag, Ab, Br, Bg, Bb;
        public Lane Lane, ExitTo;
        public double S, V, Acc, V0, T, S0, Am, Bc;
        public int Sign;
        public Lane LcFrom;
        public double LcT, LcDur, LcCheck, Brake;
        public double BobA, BobP, BobPh, WobA, WobPh;
        public double Roll, RollV, Pitch, PitchV;
        public bool HasFw;
        public int Hint, HintFrom;
        public double Fw0, Fw1, Fw2, FwV0, FwV1, FwV2;
        public double Gap, Vl;
        public Vehicle MA;
        public int MAId;
        public bool Dead;

        // the frame's attitude (Attitude)
        public bool Vis;
        public double X, Y, Z, Px, Py, LenPx, F, Haze, Ink;
        public int Slot, Blink;
        public double R0, R1, R2, U0, U1, U2, F0, F1, F2;

        /// <summary>True while it moves to the other deck of its corridor (a ghost on the deck it left).</summary>
        public bool Changing => LcFrom != null;
    }

    private Vehicle MakeVehicle(int type, Lane lane, double s, double v)
    {
        TypeInfo ti = _types[type];
        double[][] pick = ti.Livery[(int)Math.Floor(_rnd.Next() * ti.Livery.Length)];
        double wear = 0.86 + 0.14 * _rnd.Next();
        Lane root = lane.Parent ?? lane;
        Vehicle q = _pool.Count > 0 ? _pool.Pop() : new Vehicle();
        q.Id = _nextId++;
        q.Type = type;
        q.Model = ti.ModelIndex;
        q.Heavy = ti.Heavy;
        q.Len = ti.Len;
        q.Sx = ti.Sx;
        q.Sy = ti.Sy;
        q.Sz = ti.Sz;
        q.Ar = pick[0][0] * wear;
        q.Ag = pick[0][1] * wear;
        q.Ab = pick[0][2] * wear;
        q.Br = pick[1][0] * (0.9 + 0.1 * wear);
        q.Bg = pick[1][1] * (0.9 + 0.1 * wear);
        q.Bb = pick[1][2] * (0.9 + 0.1 * wear);
        q.Lane = lane;
        q.S = s;
        q.V = v;
        q.Acc = 0;
        q.ExitTo = null;
        q.V0 = root.Speed * (1 + CityR17Json.Clamp(0.055 * Gauss(), -0.12, 0.12)) * (ti.Heavy ? 0.95 : 1);
        q.T = lane.Tbase * (ti.Heavy ? 1.15 : 1) * (0.8 + 0.3 * _rnd.Next());
        q.S0 = ti.Heavy ? 4 : 2.5;
        q.Am = ti.Heavy ? 0.6 : 1.2;
        q.Bc = 2.0;
        q.Sign = ti.Name == "taxi" && _rnd.Next() < 0.4 ? 1 : 0;
        q.LcFrom = null;
        q.LcT = 0;
        q.LcDur = 0;
        q.LcCheck = _rnd.Next() * 0.5;
        q.Brake = 0;
        q.BobA = ti.Heavy ? 0.05 + 0.03 * _rnd.Next() : 0.08 + 0.07 * _rnd.Next();
        q.BobP = ti.Heavy ? 4 + 1.5 * _rnd.Next() : 2.5 + 1.5 * _rnd.Next();
        q.BobPh = _rnd.Next() * 6.283;
        q.WobA = (ti.Heavy ? 0.3 : 0.5 + 0.3 * _rnd.Next()) * Deg;
        q.WobPh = _rnd.Next() * 6.283;
        q.Roll = q.RollV = q.Pitch = q.PitchV = 0;
        q.HasFw = false;
        q.Hint = q.HintFrom = 0;
        q.Fw0 = q.Fw1 = q.Fw2 = q.FwV0 = q.FwV1 = q.FwV2 = 0;
        q.Gap = 1e9;
        q.Vl = 0;
        q.MA = null;
        q.MAId = 0;
        q.Dead = false;
        q.Vis = false;
        q.Slot = 0;
        q.Blink = 0;
        _all.Add(q);
        Spawned++;
        return q;
    }

    private double Gauss()
    {
        double u = 1 - _rnd.Next(), v = _rnd.Next();
        return Math.Sqrt(-2 * Math.Log(u)) * Math.Cos(2 * Math.PI * v);
    }

    private static double SOn(Lane lane, Vehicle q) => q.Lane == lane ? q.S : q.S * lane.L / q.Lane.L;

    private static void SortLane(Lane l)
    {
        List<Vehicle> a = l.Vs;
        for (int i = 1; i < a.Count; i++)
        {
            Vehicle x = a[i];
            double sx = SOn(l, x);
            int j = i - 1;
            while (j >= 0 && SOn(l, a[j]) < sx)
            {
                a[j + 1] = a[j];
                j--;
            }
            a[j + 1] = x;
        }
    }

    private static double SStar(Vehicle q, double v, double dv) => q.S0 + Math.Max(0, v * q.T + v * dv / (2 * Math.Sqrt(q.Am * q.Bc)));

    private static double Idm(Vehicle q, double v, double gap, double vl)
    {
        double v0 = q.V0 * Math.Min(1, CapAt(q.Lane, q.S)), fr = v / Math.Max(1, v0), a = q.Am * (1 - fr * fr * fr * fr);
        if (gap < 1e8)
        {
            double ss = SStar(q, v, v - vl);
            a -= q.Am * (ss / Math.Max(0.5, gap)) * (ss / Math.Max(0.5, gap));
        }
        return CityR17Json.Clamp(a, -6, q.Am);
    }

    // ------------------------------------------------------------------ slot clocks

    private int SlotType(Lane l, long k)
    {
        string t = l.D.Tab;
        char ch = t[(int)CityR17Json.Mod(k - l.D.K0, t.Length)];
        return ch == '.' ? -1 : ch - 'a';
    }

    private static double BaseOf(Lane l, double t) => l.Speed * t + l.D.Phase * l.H;

    private Lane KidFor(Lane l, long k)
    {
        for (int q = 0; q < l.Kids.Count; q++)
        {
            CityR17Branch br = l.Kids[q].D.Branch.Value;
            if (CityR17Json.Mod(k, br.Every) == br.Slot)
                return l.Kids[q];
        }
        return null;
    }

    private void ClockLane(Lane l, double t)
    {
        double h = l.H;
        long kNow = (long)Math.Floor(BaseOf(l, t) / h);
        if (!l.HasLastK)
        {
            l.HasLastK = true;
            l.LastK = kNow;
            return;
        }
        for (long k = l.LastK + 1; k <= kNow; k++)
        {
            Lane kid = KidFor(l, k);
            if (kid == null)
            {
                int ty = SlotType(l, k);
                if (ty >= 0)
                    l.Queue.Add(NewSpawn(ty, null));
                continue;
            }
            CityR17Branch bd = kid.D.Branch.Value;
            int kt = SlotType(kid, k);
            if (kt < 0)
                continue;
            if (!bd.Merge)
                l.Queue.Add(NewSpawn(kt, kid));
            else
                kid.MergeQ.Add(new MergeEntry { At = k * h + (bd.S - kid.L), Type = kt });
        }
        l.LastK = kNow;
        for (int i = 0; i < l.Kids.Count; i++)
        {
            Lane kid = l.Kids[i];
            if (!kid.D.Branch.Value.Merge)
                continue;
            double b = BaseOf(l, t);
            while (kid.MergeQ.Count > 0 && kid.MergeQ[0].At <= b)
            {
                MergeEntry e = kid.MergeQ[0];
                kid.MergeQ.RemoveAt(0);
                kid.Queue.Add(NewSpawn(e.Type, null));
            }
        }
    }

    private void TrySpawn(Lane l)
    {
        if (l.Queue.Count == 0)
            return;
        while (l.Queue.Count > 4)
        {
            _spawnPool.Push(l.Queue[0]);
            l.Queue.RemoveAt(0);   // a jam at a hidden start sheds, never shows
        }
        SpawnEntry e = l.Queue[0];
        Vehicle last = null;
        double len = _types[e.Type].Len;
        for (int i = 0; i < l.Vs.Count; i++)
            if (last == null || SOn(l, l.Vs[i]) < SOn(l, last))
                last = l.Vs[i];
        double v = (l.Parent ?? l).Speed;
        if (last != null)
        {
            double gap = SOn(l, last) - (last.Len + len) / 2;
            if (gap < 3 + 0.35 * v * l.Tbase + 2)
                return;   // wait (hidden) until the start is clear
            v = Math.Min(v, last.V + 1.5);
        }
        l.Queue.RemoveAt(0);
        Lane exitTo = e.ExitTo;
        int type = e.Type;
        _spawnPool.Push(e);
        Vehicle q = MakeVehicle(type, l, 0, v);
        q.ExitTo = exitTo;
        l.Vs.Add(q);
    }

    // ------------------------------------------------------------------ the step

    private void LaneStep(double h)
    {
        for (int i = 0; i < _all.Count; i++)
        {
            _all[i].Gap = 1e9;
            _all[i].Vl = 0;
        }
        for (int i = 0; i < _lanes.Count; i++)
        {
            Lane l = _lanes[i];
            SortLane(l);
            List<Vehicle> a = l.Vs;
            for (int j = 1; j < a.Count; j++)
            {
                Vehicle o = a[j - 1], q = a[j];
                double g = SOn(l, o) - SOn(l, q) - (o.Len + q.Len) / 2;
                if (g < q.Gap)
                {
                    q.Gap = g;
                    q.Vl = o.V;
                }
            }
        }
        // merge ramps: 150 m before the merge the ramp vehicle picks its place behind mA once; parent vehicles behind its
        // projection (within 80 m) follow it
        for (int i = 0; i < _lanes.Count; i++)
        {
            Lane l = _lanes[i];
            if (l.Parent == null || !l.D.Branch.Value.Merge)
                continue;
            Lane par = l.Parent;
            double off = l.D.Branch.Value.S - l.L;
            for (int j = 0; j < l.Vs.Count; j++)
            {
                Vehicle q = l.Vs[j];
                if (q.S < l.L - 150)
                    continue;
                double qp = q.S + off;
                if (q.MA != null && (q.MA.Lane != par || q.MA.Dead || q.MA.Id != q.MAId))
                    q.MA = null;
                if (q.MA == null)
                {
                    for (int m = par.Vs.Count - 1; m >= 0; m--)
                    {
                        Vehicle p = par.Vs[m];
                        if (p.Lane == par && p.S >= qp)
                        {
                            q.MA = p;
                            q.MAId = p.Id;
                            break;
                        }
                    }
                }
                if (q.MA != null)
                {
                    double g = q.MA.S - qp - (q.MA.Len + q.Len) / 2;
                    if (g < q.Gap)
                    {
                        q.Gap = g;
                        q.Vl = q.MA.V;
                    }
                }
                for (int m = 0; m < par.Vs.Count; m++)
                {
                    Vehicle p = par.Vs[m];
                    if (p.Lane != par || p.S >= qp || qp - p.S > 80)
                        continue;
                    double g2 = qp - p.S - (p.Len + q.Len) / 2;
                    if (g2 < p.Gap)
                    {
                        p.Gap = g2;
                        p.Vl = q.V;
                    }
                }
            }
        }
        for (int i = 0; i < _all.Count; i++)
        {
            Vehicle q = _all[i];
            double a = Idm(q, q.V, q.Gap, q.Vl);
            q.Acc = a;
            q.V = Math.Max(0, q.V + a * h);
            q.S += q.V * h;
            if (q.LcFrom != null)
            {
                q.LcT += h;
                if (q.LcT >= q.LcDur)
                {
                    q.LcFrom.Vs.Remove(q);
                    q.LcFrom = null;
                }
            }
        }
        // lane transfers: diverge onto the ramp, ramp onto the parent, off the end
        for (int i = _all.Count - 1; i >= 0; i--)
        {
            Vehicle q = _all[i];
            Lane l = q.Lane;
            if (q.ExitTo != null && l.Parent == null && q.S >= q.ExitTo.D.Branch.Value.S)
            {
                Lane kid = q.ExitTo;
                l.Vs.Remove(q);
                if (q.LcFrom != null)
                {
                    q.LcFrom.Vs.Remove(q);
                    q.LcFrom = null;
                }
                q.S -= kid.D.Branch.Value.S;
                q.Lane = kid;
                q.ExitTo = null;
                kid.Vs.Add(q);
                continue;
            }
            if (l.Parent != null && l.D.Branch.Value.Merge && q.S >= l.L)
            {
                l.Vs.Remove(q);
                q.S = l.D.Branch.Value.S + (q.S - l.L);
                q.Lane = l.Parent;
                q.MA = null;
                l.Parent.Vs.Add(q);
                continue;
            }
            if (q.S >= l.L)
            {
                l.Vs.Remove(q);
                if (q.LcFrom != null)
                    q.LcFrom.Vs.Remove(q);
                q.Dead = true;
                _all.RemoveAt(i);
                _pool.Push(q);
                Despawned++;
            }
        }
    }

    private void LaneChanges(double h)
    {
        for (int i = 0; i < _all.Count; i++)
        {
            Vehicle q = _all[i];
            q.LcCheck -= h;
            if (q.LcCheck > 0)
                continue;
            q.LcCheck = 0.5;
            Lane l = q.Lane, B = l.Sib;
            if (B == null || q.LcFrom != null || q.ExitTo != null || l.Parent != null || q.Heavy)
                continue;
            if (q.S < 0.12 * l.L || q.S > 0.85 * l.L)
                continue;
            if (q.V0 - q.V < 1.2 || q.Gap > 2.2 * SStar(q, q.V, q.V - q.Vl))
                continue;
            string key = CorrKey(l);
            if (_corrClock.TryGetValue(key, out double until) && (until != 0 ? until : -1e9) > _simT)
                continue;
            double sB = q.S * B.L / l.L;
            Vehicle ahead = null, behind = null;
            for (int j = 0; j < B.Vs.Count; j++)
            {
                Vehicle o = B.Vs[j];
                double so = SOn(B, o);
                if (so >= sB)
                    ahead = o;
                else
                {
                    behind = o;
                    break;
                }
            }
            double gA = ahead != null ? SOn(B, ahead) - sB - (ahead.Len + q.Len) / 2 : 1e9, vA = ahead != null ? ahead.V : 0;
            if (gA < SStar(q, q.V, q.V - vA) + 10)
                continue;
            if (behind != null)
            {
                double gB = sB - SOn(B, behind) - (behind.Len + q.Len) / 2;
                if (gB < behind.S0 + behind.V * behind.T + 6)
                    continue;
                if (Idm(behind, behind.V, gB, q.V) < -1.2)
                    continue;
            }
            if (Idm(q, q.V, gA, vA) - q.Acc < 0.15)
                continue;
            // go: the vehicle now belongs to deck B and stays a ghost on deck A until the move ends
            l.Vs.Remove(q);
            q.LcFrom = l;
            q.LcT = -0.9;
            q.LcDur = 3.6 + 1.2 * _rnd.Next();
            q.S = sB;
            q.Lane = B;
            B.Vs.Add(q);
            l.Vs.Add(q);
            _corrClock[key] = _simT + 3.5 + 3 * _rnd.Next();
        }
    }

    private readonly Dictionary<Lane, string> _corrKeys = new Dictionary<Lane, string>();

    private string CorrKey(Lane l)
    {
        if (!_corrKeys.TryGetValue(l, out string k))
            _corrKeys[l] = k = l.Corridor + l.Dir;
        return k;
    }

    private void SimStep(double h)
    {
        _simT += h;
        for (int i = 0; i < _lanes.Count; i++)
            if (_lanes[i].Parent == null)
                ClockLane(_lanes[i], _simT);
        for (int i = 0; i < _lanes.Count; i++)
            TrySpawn(_lanes[i]);
        LaneStep(h);
        LaneChanges(h);
    }

    /// <summary>
    /// Fills every lane from its conveyor at T0 (the slots the page showed), then lets the car-following settle off
    /// the clock (24 s at 30 Hz and 2 s at 120 Hz), so the city opens already in motion with no empty sky.
    /// </summary>
    public void Prefill()
    {
        _simT = _data.T0;
        foreach (Lane l in _lanes)
        {
            if (l.Parent != null)
                continue;
            double h = l.H, bse = BaseOf(l, _data.T0);
            long kmax = (long)Math.Floor(bse / h);
            double kidL = 0;
            foreach (Lane b in l.Kids)
                kidL = Math.Max(kidL, b.L);
            long nj = (long)Math.Floor((l.L + kidL + 2 * h) / h) + 2;
            for (long j = 0; j < nj; j++)
            {
                long k = kmax - j;
                double p = bse - k * h;
                Lane kid = KidFor(l, k);
                if (kid == null)
                {
                    int ty = SlotType(l, k);
                    if (ty >= 0 && p >= 0 && p <= l.L)
                        l.Vs.Add(MakeVehicle(ty, l, p, l.Speed));
                    continue;
                }
                CityR17Branch bd = kid.D.Branch.Value;
                int kt = SlotType(kid, k);
                if (kt < 0)
                    continue;
                if (!bd.Merge)
                {
                    if (p >= 0 && p < bd.S)
                    {
                        Vehicle v1 = MakeVehicle(kt, l, p, l.Speed);
                        v1.ExitTo = kid;
                        l.Vs.Add(v1);
                    }
                    else if (p - bd.S >= 0 && p - bd.S <= kid.L)
                        kid.Vs.Add(MakeVehicle(kt, kid, p - bd.S, l.Speed));
                }
                else
                {
                    double sk = p - (bd.S - kid.L);
                    if (p >= bd.S && p <= l.L)
                        l.Vs.Add(MakeVehicle(kt, l, p, l.Speed));
                    else if (sk >= 0 && sk < kid.L)
                        kid.Vs.Add(MakeVehicle(kt, kid, sk, l.Speed));
                    else if (sk < 0)
                        kid.MergeQ.Add(new MergeEntry { At = k * h + (bd.S - kid.L), Type = kt });
                }
            }
            l.HasLastK = true;
            l.LastK = kmax;
        }
        foreach (Lane l in _lanes)
            foreach (Lane kd in l.Kids)
                kd.MergeQ.Sort((x, y) => x.At.CompareTo(y.At));
        for (int n = 0; n < _data.Settle30; n++)
            SimStep(1.0 / 30);
        for (int n = 0; n < _data.Settle120; n++)
            SimStep(1.0 / 120);
        Spawned = 0;
        Despawned = 0;
    }

    /// <summary>
    /// One frame of dt seconds: the sim in substeps of the page's 1/120 s (dt / n each, n = round(dt * 120), at least
    /// one), then the attitude of every vehicle for this frame. <paramref name="pxPerPlate"/> (output px per plate px)
    /// sets the ink weight by on-screen size, as the page's device scale does.
    /// </summary>
    public void Update(double dt, double pxPerPlate)
    {
        double h = _data.Substep;
        int n = (int)Math.Max(0, CityR17Json.JsRound(dt / h));
        if (dt > 0 && n == 0)
            n = 1;
        double hh = n > 0 ? dt / n : 0;
        for (int i = 0; i < n; i++)
            SimStep(hh);
        Attitude(dt > 0 ? dt : 1e-6, pxPerPlate);
    }

    // ------------------------------------------------------------------ attitude

    /// <summary>The parallax factor of a depth (the planes' factors interpolated in 1/Z), as the page's parOf.</summary>
    public double ParOf(double z)
    {
        double[] PZ = _data.PZ, PF = _data.PF;
        double inv = 1 / z;
        int n = PZ.Length;
        if (inv <= 1 / PZ[0])
            return PF[1];
        if (inv >= 1 / PZ[n - 1])
            return PF[n];
        for (int i = 0; i < n - 1; i++)
        {
            double a = 1 / PZ[i], b = 1 / PZ[i + 1];
            if (inv >= a && inv <= b)
                return PF[i + 1] + (PF[i + 2] - PF[i + 1]) * (inv - a) / (b - a);
        }
        return 0.3;
    }

    /// <summary>The plane a depth draws after: one past the nearest plane still farther than it (r14's slot rule).</summary>
    public int SlotOf(double z)
    {
        int s = 0;
        for (int i = 0; i < _data.PZ.Length; i++)
            if (_data.PZ[i] > z)
                s = i + 1;
        return s;
    }

    /// <summary>A lane's slot at arc length s and depth z: LANE_SLOT pins, SLOT_FIX moves a hand-over, LANE_CAP caps.</summary>
    public int LaneSlot(Lane l, double s, double z)
    {
        if (l.Pinned)
            return l.PinSlot;
        int slot = SlotOf(z);
        if (l.Fix != null)
            for (int k = 0; k < l.Fix.Length; k++)
                if (s >= l.Fix[k][0] && s < l.Fix[k][1])
                    slot = (int)l.Fix[k][2];
        if (l.Capped)
            slot = Math.Min(slot, l.CapSlot);
        return slot;
    }

    /// <summary>The page's critically damped spring (exact, Holden), with its rate y and decay e = exp(-y dt) given (<see cref="Decay"/>).</summary>
    private static void Spring(ref double x, ref double v, double goal, in Decay d, double dt)
    {
        double y = d.Y, j0 = x - goal, j1 = v + j0 * y, e = d.E;
        x = e * (j0 + j1 * dt) + goal;
        v = e * (v - j1 * y * dt);
    }

    /// <summary>A spring's rate for a half-life and its decay over the frame (shared by every vehicle with that half-life: the same numbers the page computes per vehicle).</summary>
    private readonly struct Decay
    {
        public readonly double Y, E;

        public Decay(double hl, double dt)
        {
            Y = 0.6931 * 2 / Math.Max(1e-4, hl) / 2;
            E = Math.Exp(-Y * dt);
        }
    }

    private static double Smoother(double u) => u * u * u * (u * (u * 6 - 15) + 10);

    private static double Smoother1(double u) => 30 * u * u * (u - 1) * (u - 1);

    private static double Smoother2(double u) => 60 * u * (2 * u - 1) * (u - 1);

    /// <summary>
    /// Every vehicle's frame for this frame (the page's attitude()): position on its (possibly blended) path, heading
    /// spring, bank and thrust pitch springs, hover bob, brake lamp; then, if in front of the camera and near the plate,
    /// its projection, parallax, slot, haze, ink and indicator.
    /// </summary>
    private void Attitude(double dt, double pxPerPlate)
    {
        double cx = _data.CamCx, cy = _data.CamCy, F = _data.CamF, minZ = _data.MinZ, margin = _data.CullMargin;
        var heavyHeading = new Decay(0.3, dt);   // heading: heavies 0.3 s, cars 0.15 s; bank and pitch: heavies 0.7 s, cars 0.3 s
        var carHeading = new Decay(0.15, dt);
        var heavyAttitude = new Decay(0.7, dt);
        var carAttitude = heavyHeading;
        double brakeK = 1 - Math.Exp(-dt / 0.12);
        for (int i = 0; i < _all.Count; i++)
        {
            Vehicle q = _all[i];
            Lane l = q.Lane;
            double v = q.V;
            PosAtH(l, q.S, ref q.Hint, _pA);
            TanAtH(l, q.S, ref q.Hint, _tA);
            CurvAtH(l, q.S, ref q.Hint, _kA);
            double X = _pA[0], Y = _pA[1], Z = _pA[2], vx = _tA[0] * v, vy = _tA[1] * v, vz = _tA[2] * v;
            double ax = _kA[0] * v * v + _tA[0] * q.Acc, ay = _kA[1] * v * v + _tA[1] * q.Acc, az = _kA[2] * v * v + _tA[2] * q.Acc;
            if (q.LcFrom != null)
            {
                Lane Fl = q.LcFrom;
                double sF = q.S * Fl.L / l.L, u = CityR17Json.Clamp(q.LcT / q.LcDur, 0, 1), w = Smoother(u), w1 = Smoother1(u) / q.LcDur, w2 = Smoother2(u) / (q.LcDur * q.LcDur);
                PosAtH(Fl, sF, ref q.HintFrom, _pB);
                TanAtH(Fl, sF, ref q.HintFrom, _tB);
                CurvAtH(Fl, sF, ref q.HintFrom, _kB);
                double Dx = _pA[0] - _pB[0], Dy = _pA[1] - _pB[1], Dz = _pA[2] - _pB[2];
                X = _pB[0] + Dx * w;
                Y = _pB[1] + Dy * w;
                Z = _pB[2] + Dz * w;
                vx = (_tB[0] + (_tA[0] - _tB[0]) * w) * v + Dx * w1;
                vy = (_tB[1] + (_tA[1] - _tB[1]) * w) * v + Dy * w1;
                vz = (_tB[2] + (_tA[2] - _tB[2]) * w) * v + Dz * w1;
                ax = (_kB[0] + (_kA[0] - _kB[0]) * w) * v * v + Dx * w2 + _tA[0] * q.Acc;
                ay = (_kB[1] + (_kA[1] - _kB[1]) * w) * v * v + Dy * w2 + _tA[1] * q.Acc;
                az = (_kB[2] + (_kA[2] - _kB[2]) * w) * v * v + Dz * w2 + _tA[2] * q.Acc;
            }
            double vn = Math.Sqrt(vx * vx + vy * vy + vz * vz);
            if (vn == 0)
                vn = 1;
            double fx = vx / vn, fy = vy / vn, fz = vz / vn;
            // heading spring (weight: the body lags its path a touch, and polyline vertices never show)
            Decay hd = q.Heavy ? heavyHeading : carHeading;
            if (!q.HasFw)
            {
                q.HasFw = true;
                q.Fw0 = fx;
                q.Fw1 = fy;
                q.Fw2 = fz;
            }
            Spring(ref q.Fw0, ref q.FwV0, fx, hd, dt);
            Spring(ref q.Fw1, ref q.FwV1, fy, hd, dt);
            Spring(ref q.Fw2, ref q.FwV2, fz, hd, dt);
            double fwn = Math.Sqrt(q.Fw0 * q.Fw0 + q.Fw1 * q.Fw1 + q.Fw2 * q.Fw2);
            if (fwn == 0)
                fwn = 1;
            fx = q.Fw0 / fwn;
            fy = q.Fw1 / fwn;
            fz = q.Fw2 / fwn;
            // frame: right = up x fwd (X right, Y up, Z into the screen), up = fwd x right
            double rx = fz, ry = 0, rz = -fx, rn = Math.Sqrt(rx * rx + rz * rz);
            if (rn == 0)
                rn = 1;
            rx /= rn;
            rz /= rn;
            double ux = fy * rz - fz * ry, uy = fz * rx - fx * rz, uz = fx * ry - fy * rx;
            // bank from lateral acceleration, thrust pitch from longitudinal, both on springs
            double aLat = ax * rx + ay * ry + az * rz, aLon = ax * fx + ay * fy + az * fz;
            double rollMax = (q.Heavy ? 8 : 14) * Deg, rollT = CityR17Json.Clamp(0.75 * Math.Atan2(aLat, G), -rollMax, rollMax);
            double pitchT = CityR17Json.Clamp(2 * Deg * aLon, -6 * Deg, 6 * Deg);
            Decay ad = q.Heavy ? heavyAttitude : carAttitude;
            Spring(ref q.Roll, ref q.RollV, rollT, ad, dt);
            Spring(ref q.Pitch, ref q.PitchV, pitchT, ad, dt);
            double tt = _simT, ph = 2 * Math.PI * tt / q.BobP + q.BobPh;
            double roll = q.Roll + q.WobA * Math.Sin(ph * 1.37 + q.WobPh), pitch = q.Pitch + q.WobA * 0.7 * Math.Sin(ph * 0.83 + 1.1 * q.WobPh);
            // pitch (positive = nose down) about right, then roll (positive = right side down) about fwd
            double cp = Math.Cos(pitch), sp = Math.Sin(pitch);
            double f2x = fx * cp - ux * sp, f2y = fy * cp - uy * sp, f2z = fz * cp - uz * sp;
            double u2x = ux * cp + fx * sp, u2y = uy * cp + fy * sp, u2z = uz * cp + fz * sp;
            double cr = Math.Cos(roll), sr = Math.Sin(roll);
            double u3x = u2x * cr + rx * sr, u3y = u2y * cr + ry * sr, u3z = u2z * cr + rz * sr;
            double r3x = rx * cr - u2x * sr, r3y = ry * cr - u2y * sr, r3z = rz * cr - u2z * sr;
            double bob = q.BobA * Math.Sin(ph);
            X += u3x * bob;
            Y += u3y * bob;
            Z += u3z * bob;
            q.X = X;
            q.Y = Y;
            q.Z = Z;
            // brake lamp
            q.Brake += ((q.Acc < -1.5 ? 1 : 0) - q.Brake) * brakeK;
            if (Z < minZ)
            {
                q.Vis = false;
                continue;
            }
            double xp = cx + F * X / Z, yp = cy - F * Y / Z, ppm = F / Z, lenPx = q.Len * ppm;
            double rad = lenPx * 0.75 + 6;
            q.Px = xp;
            q.Py = yp;
            q.LenPx = lenPx;
            if (xp + rad < -margin || xp - rad > PlateW + margin || yp + rad < -margin || yp - rad > PlateH + margin)
            {
                q.Vis = false;
                continue;
            }
            q.Vis = true;
            q.F = ParOf(Z);
            q.Slot = LaneSlot(l, q.S, Z);
            q.Haze = CityR17Json.Clamp(0.5 * (Z - 220) / 2400, 0, 0.5);
            double dev = lenPx * pxPerPlate;
            q.Ink = dev < 7 ? 0 : dev < 16 ? 0.5 * (dev - 7) / 9 : dev < 26 ? 0.5 + 0.5 * (dev - 16) / 10 : dev < 64 ? 1 : dev < 84 ? 1 + 0.5 * (dev - 64) / 20 : 1.5;
            int blink = 0;
            if (q.LcFrom != null && q.LcT < q.LcDur * 0.6)
            {
                PosAtH(q.LcFrom, q.S * q.LcFrom.L / l.L, ref q.HintFrom, _pB);
                double side = (_pA[0] - _pB[0]) * r3x + (_pA[1] - _pB[1]) * r3y + (_pA[2] - _pB[2]) * r3z;
                int on = (long)Math.Floor((q.LcT + 0.9) * 3) % 2 == 0 ? 1 : 0;   // 1.5 Hz
                blink = on * (side >= 0 ? 2 : 1);                                // 2 = right, 1 = left
            }
            q.Blink = blink;
            q.R0 = r3x;
            q.R1 = r3y;
            q.R2 = r3z;
            q.U0 = u3x;
            q.U1 = u3y;
            q.U2 = u3z;
            q.F0 = f2x;
            q.F1 = f2y;
            q.F2 = f2z;
        }
    }

    /// <summary>The plate's width the cull tests against (the page's SCENE.plate.w).</summary>
    public double PlateW { get; set; } = 2688;

    /// <summary>The plate's height the cull tests against.</summary>
    public double PlateH { get; set; } = 1152;

    /// <summary>The first frame's attitude after <see cref="Prefill"/> (the page's install: attitude(1/60)).</summary>
    public void Settle(double pxPerPlate) => Attitude(1.0 / 60, pxPerPlate);

    /// <summary>A vehicle type's name (the r14 table's).</summary>
    public string TypeName(int type) => _types[type].Name;

    /// <summary>The draw group of a visible vehicle: its slot, and whether it flies the K skyway (drawn first in its slot, with its own depth).</summary>
    public static int Group(Vehicle q) => q.Slot * 2 + (q.Lane.IsK ? 1 : 0);
}
