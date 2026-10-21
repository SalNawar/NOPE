using System;
using System.Collections.Generic;

/// <summary>
/// The r17 city's scene (CITY-R17): <c>Assets/Data/City/R17/city_r17_scene.json</c>, exported from the running
/// r17 look-dev page (<c>E:\unity\NOPE-tools\city\live\r17.html</c>) by <c>tools/art/city_r17/export_r17.js</c>.
/// Everything the port places comes from here: the plate and its pinhole camera, the 11 planes with their
/// parallax and haze, the clouds' paths and morph cycles, the train's passes and mattes, the ad screens'
/// tile sequences and holds, the fire and smoke flipbooks, the noon lamps and the draw order the page runs.
/// Pure (no engine types); tested in EditMode.
/// </summary>
public sealed class CityR17Scene
{
    public double PlateW, PlateH;

    /// <summary>The fitted plate camera: x = Cx + F X / Z, y = Cy - F Y / Z (plate px; metres, Z into the screen).</summary>
    public double CamCx, CamCy, CamF;

    /// <summary>The dolly's focus (plate px).</summary>
    public double FocusX, FocusY;

    /// <summary>The look: plate px of shift at parallax 1 for a camera offset of 1, its vertical share, the follow ease (s).</summary>
    public double ParallaxPx, ParallaxY, Ease;

    /// <summary>The loops' frame clock (24).</summary>
    public int Fps;

    /// <summary>The far planes' air colour (the haze tint).</summary>
    public double[] HazeColor;

    public CityR17Plane[] Planes;
    public CityR17CloudSet Clouds;
    public CityR17TrainData Train;
    public CityR17AdsData Ads;
    public CityR17FireData Fire;
    public CityR17SmokeData Smoke;
    public CityR17LightsData Lights;

    /// <summary>The page's drawers in order: (system, plane index, order).</summary>
    public CityR17OrderEntry[] Order;

    /// <summary>Parses the scene file.</summary>
    public static CityR17Scene Parse(string json)
    {
        ContentNode root = ContentJson.Parse(json);
        var s = new CityR17Scene();
        ContentNode plate = CityR17Json.Req(root, "plate");
        s.PlateW = CityR17Json.Num(plate, "w");
        s.PlateH = CityR17Json.Num(plate, "h");
        ContentNode cam = CityR17Json.Req(root, "camera");
        s.CamCx = CityR17Json.Num(cam, "cx");
        s.CamCy = CityR17Json.Num(cam, "cy");
        s.CamF = CityR17Json.Num(cam, "F");
        double[] focus = CityR17Json.Nums(root, "focus");
        s.FocusX = focus[0];
        s.FocusY = focus[1];
        ContentNode look = CityR17Json.Req(root, "look");
        s.ParallaxPx = CityR17Json.Num(look, "parallaxPx");
        s.ParallaxY = CityR17Json.Num(look, "parallaxY");
        s.Ease = CityR17Json.Num(look, "ease");
        s.Fps = CityR17Json.Int(look, "fps");
        s.HazeColor = CityR17Json.Nums(root, "hazeColor");

        IReadOnlyList<ContentNode> planes = CityR17Json.Items(root, "planes");
        s.Planes = new CityR17Plane[planes.Count];
        for (int i = 0; i < planes.Count; i++)
        {
            ContentNode p = planes[i];
            s.Planes[i] = new CityR17Plane
            {
                Id = CityR17Json.ReqStr(p, "id"),
                Name = CityR17Json.Str(p, "name") ?? string.Empty,
                Parallax = CityR17Json.Num(p, "parallax"),
                Haze = CityR17Json.Num(p, "haze", 0),
                Frame = CityR17Json.Rect(p, "frame"),
                File = CityR17Json.ReqStr(p, "file"),
            };
        }

        s.Clouds = ParseClouds(CityR17Json.Req(root, "clouds"));
        s.Train = ParseTrain(CityR17Json.Req(root, "train"));
        s.Ads = ParseAds(CityR17Json.Req(root, "ads"));
        s.Fire = ParseFire(CityR17Json.Req(root, "fire"));
        s.Smoke = ParseSmoke(CityR17Json.Req(root, "smoke"));
        s.Lights = ParseLights(CityR17Json.Req(root, "lights"));

        IReadOnlyList<ContentNode> order = CityR17Json.Items(root, "order");
        s.Order = new CityR17OrderEntry[order.Count];
        for (int i = 0; i < order.Count; i++)
        {
            IReadOnlyList<ContentNode> e = order[i].Items;
            s.Order[i] = new CityR17OrderEntry(e[0].Text, (int)CityR17Json.Num(e[1]), CityR17Json.Num(e[2]));
        }
        return s;
    }

    /// <summary>The plane with an id, or -1.</summary>
    public int PlaneIndex(string id)
    {
        for (int i = 0; i < Planes.Length; i++)
            if (Planes[i].Id == id)
                return i;
        return -1;
    }

    /// <summary>A plane's parallax factor (0 when out of range).</summary>
    public double ParallaxOf(int plane) => plane >= 0 && plane < Planes.Length ? Planes[plane].Parallax : 0;

    private static CityR17CloudSet ParseClouds(ContentNode n)
    {
        IReadOnlyList<ContentNode> list = CityR17Json.Items(n, "list");
        var set = new CityR17CloudSet
        {
            ScRef = CityR17Json.Num(n, "scRef"),
            Pad = CityR17Json.Num(n, "pad"),
            Plane = CityR17Json.Int(n, "slot"),
            List = new CityR17Cloud[list.Count],
        };
        for (int i = 0; i < list.Count; i++)
        {
            ContentNode c = list[i];
            set.List[i] = new CityR17Cloud
            {
                Id = CityR17Json.ReqStr(c, "id"),
                Band = CityR17Json.Str(c, "band") ?? string.Empty,
                File = CityR17Json.ReqStr(c, "file"),
                W = CityR17Json.Num(c, "w"),
                H = CityR17Json.Num(c, "h"),
                N = CityR17Json.Int(c, "n"),
                Cols = CityR17Json.Int(c, "cols"),
                Top = CityR17Json.Num(c, "top"),
                A = CityR17Json.Num(c, "A"),
                L = CityR17Json.Num(c, "L"),
                K = CityR17Json.Num(c, "k"),
                X0 = CityR17Json.Num(c, "x0"),
                Par = CityR17Json.Num(c, "par"),
                Step = CityR17Json.Num(c, "step"),
                Mph = CityR17Json.Num(c, "mph"),
            };
        }
        return set;
    }

    private static CityR17TrainData ParseTrain(ContentNode n)
    {
        IReadOnlyList<ContentNode> cars = CityR17Json.Items(n, "cars");
        var t = new CityR17TrainData { Cars = new CityR17TrainCar[cars.Count] };
        for (int i = 0; i < cars.Count; i++)
            t.Cars[i] = new CityR17TrainCar
            {
                Name = CityR17Json.Str(cars[i], "name") ?? string.Empty,
                File = CityR17Json.ReqStr(cars[i], "file"),
                Glow = CityR17Json.ReqStr(cars[i], "glow"),
                Ox = CityR17Json.Num(cars[i], "ox"),
                W = CityR17Json.Num(cars[i], "w"),
                H = CityR17Json.Num(cars[i], "h"),
            };
        IReadOnlyList<ContentNode> gang = CityR17Json.Items(n, "gangways");
        t.Gangways = new double[gang.Count][];
        for (int i = 0; i < gang.Count; i++)
            t.Gangways[i] = CityR17Json.Nums(gang[i]);
        t.GangRows = CityR17Json.Nums(n, "gangRows");
        t.Length = CityR17Json.Num(n, "length");
        t.SkirtRow = CityR17Json.Num(n, "skirtRow");
        t.Pad = CityR17Json.Num(n, "pad");
        double[] deck = CityR17Json.Nums(n, "deckLine");
        t.DeckA = deck[0];
        t.DeckB = deck[1];
        t.SkirtAbove = CityR17Json.Num(n, "skirtAbove");
        t.LeftMatte = CityR17Json.Num(n, "leftMatte");
        t.LeftMattePlane = CityR17Json.Int(n, "leftMattePlane");
        t.ClipX = CityR17Json.Num(n, "clipX");
        t.ClipPlane = CityR17Json.Int(n, "clipPlane");
        t.Margin = CityR17Json.Num(n, "margin");
        t.Speed = CityR17Json.Num(n, "speed");
        t.Cycle = CityR17Json.Int(n, "cycle");
        IReadOnlyList<ContentNode> passes = CityR17Json.Items(n, "passes");
        t.Passes = new CityR17TrainPassDef[passes.Count];
        for (int i = 0; i < passes.Count; i++)
            t.Passes[i] = new CityR17TrainPassDef(CityR17Json.Int(passes[i], "f0"), CityR17Json.ReqStr(passes[i], "dir") == "W");
        ContentNode bob = CityR17Json.Req(n, "bob");
        t.BobPeriod = CityR17Json.Num(bob, "period");
        t.BobPhase = CityR17Json.Num(bob, "phase");
        t.BobThresh = CityR17Json.Num(bob, "thresh");
        t.GlowAlpha = CityR17Json.Num(n, "glowAlpha");
        t.GangColor = CityR17Json.Nums(n, "gangColor");
        t.Plane = CityR17Json.Int(n, "plane");
        ContentNode para = CityR17Json.Req(n, "parapet");
        t.ParapetFile = CityR17Json.ReqStr(para, "file");
        double[] at = CityR17Json.Nums(para, "at");
        t.ParapetX = at[0];
        t.ParapetY = at[1];
        double[] book = CityR17Json.Nums(para, "bookP09");
        t.BookSx = book[0];
        t.BookSy = book[1];
        t.PaintFile = CityR17Json.ReqStr(n, "paint");
        return t;
    }

    private static CityR17AdsData ParseAds(ContentNode n)
    {
        var a = new CityR17AdsData
        {
            Fps = CityR17Json.Int(n, "fps"),
            N = CityR17Json.Int(n, "n"),
            Plane = CityR17Json.Int(n, "plane"),
            Sheets = CityR17Json.Strs(CityR17Json.Req(n, "sheets")),
        };
        IReadOnlyList<ContentNode> tiles = CityR17Json.Items(n, "tiles");
        a.Tiles = new int[tiles.Count * 5];
        for (int i = 0; i < tiles.Count; i++)
        {
            int[] t = CityR17Json.Ints(tiles[i]);
            Array.Copy(t, 0, a.Tiles, i * 5, 5);
        }
        IReadOnlyList<ContentNode> screens = CityR17Json.Items(n, "screens");
        a.Screens = new CityR17Screen[screens.Count];
        for (int i = 0; i < screens.Count; i++)
        {
            ContentNode sc = screens[i];
            IReadOnlyList<ContentNode> seq = CityR17Json.Items(sc, "seq");
            var rle = new int[seq.Count * 2];
            for (int k = 0; k < seq.Count; k++)
            {
                rle[2 * k] = (int)CityR17Json.Num(seq[k].Items[0]);
                rle[2 * k + 1] = (int)CityR17Json.Num(seq[k].Items[1]);
            }
            a.Screens[i] = new CityR17Screen { Id = CityR17Json.ReqStr(sc, "id"), Rect = CityR17Json.Rect(sc, "rect"), Rle = rle };
        }
        ContentNode tf = CityR17Json.Req(n, "transitionFrames");
        a.TransitionFrames = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ContentNode> kv in tf.Members)
            a.TransitionFrames[kv.Key] = (int)CityR17Json.Num(kv.Value);
        a.Holds = new Dictionary<string, CityR17Hold[]>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ContentNode> kv in CityR17Json.Req(n, "holds").Members)
        {
            IReadOnlyList<ContentNode> hs = kv.Value.Items;
            var arr = new CityR17Hold[hs.Count];
            for (int k = 0; k < hs.Count; k++)
                arr[k] = new CityR17Hold(CityR17Json.Int(hs[k], "start"), CityR17Json.ReqStr(hs[k], "ad"), CityR17Json.Str(hs[k], "kind") ?? string.Empty);
            a.Holds[kv.Key] = arr;
        }
        return a;
    }

    private static int[] Seq5(ContentNode n, string key)
    {
        IReadOnlyList<ContentNode> seq = CityR17Json.Items(n, key);
        var a = new int[seq.Count * 5];
        for (int i = 0; i < seq.Count; i++)
        {
            int[] t = CityR17Json.Ints(seq[i]);
            Array.Copy(t, 0, a, i * 5, 5);
        }
        return a;
    }

    private static CityR17FireData ParseFire(ContentNode n)
    {
        ContentNode spill = CityR17Json.Req(n, "spill");
        return new CityR17FireData
        {
            N = CityR17Json.Int(n, "n"),
            Plane = CityR17Json.Int(n, "plane"),
            Rect = CityR17Json.Rect(n, "rect"),
            Sheets = CityR17Json.Strs(CityR17Json.Req(n, "sheets")),
            Seq = Seq5(n, "seq"),
            SpillBase = CityR17Json.ReqStr(spill, "base"),
            SpillBaseRect = CityR17Json.Rect(spill, "baseRect"),
            Spill = CityR17Json.ReqStr(spill, "spill"),
            SpillRect = CityR17Json.Rect(spill, "spillRect"),
            Gain = CityR17Json.Nums(spill, "gain"),
        };
    }

    private static CityR17SmokeData ParseSmoke(ContentNode n)
    {
        IReadOnlyList<ContentNode> stacks = CityR17Json.Items(n, "stacks");
        var s = new CityR17SmokeData { N = CityR17Json.Int(n, "n"), Plane = CityR17Json.Int(n, "plane"), Stacks = new CityR17SmokeStack[stacks.Count] };
        for (int i = 0; i < stacks.Count; i++)
        {
            ContentNode st = stacks[i];
            s.Stacks[i] = new CityR17SmokeStack
            {
                Id = CityR17Json.ReqStr(st, "id"),
                Rect = CityR17Json.Rect(st, "rect"),
                Sheets = CityR17Json.Strs(CityR17Json.Req(st, "sheets")),
                Seq = Seq5(st, "seq"),
                MouthRect = CityR17Json.Rect(st, "mouthRect"),
                MouthSheets = CityR17Json.Strs(CityR17Json.Req(st, "mouthSheets")),
                MouthSeq = Seq5(st, "mouthSeq"),
            };
        }
        return s;
    }

    private static CityR17LightsData ParseLights(ContentNode n)
    {
        var list = new List<CityR17Lamp>();
        void Add(string key, CityR17LampKind kind, int planeIndex)
        {
            IReadOnlyList<ContentNode> items = n.Get(key)?.Items;
            if (items == null)
                return;
            for (int i = 0; i < items.Count; i++)
            {
                double[] a = CityR17Json.Nums(items[i]);
                list.Add(new CityR17Lamp
                {
                    Kind = kind,
                    X = a[0],
                    Y = a[1],
                    Plane = (int)a[planeIndex],
                    Group = kind == CityR17LampKind.Warn ? (int)a[2] : 0,
                    Index = i,
                    Count = items.Count,
                });
            }
        }
        Add("hallSeam", CityR17LampKind.Seam, 2);
        Add("hallBand", CityR17LampKind.Band, 2);
        Add("ringLamps", CityR17LampKind.Ring, 2);
        Add("exTop", CityR17LampKind.ExTop, 2);
        Add("warn", CityR17LampKind.Warn, 3);
        return new CityR17LightsData { Lamps = list.ToArray() };
    }
}

/// <summary>One parallax plane: its id, parallax factor (0 far .. 1 near), haze tint share and where its full image sits on the plate.</summary>
public sealed class CityR17Plane
{
    public string Id, Name, File;
    public double Parallax, Haze;
    public CityR17Rect Frame;
}

/// <summary>The clouds: the review scale their speeds were set at, the soft cells' padding, the plane they draw after.</summary>
public sealed class CityR17CloudSet
{
    public double ScRef, Pad;
    public int Plane;
    public CityR17Cloud[] List;
}

/// <summary>
/// One cloud: its morph sheet (n drawings of W x H in Cols columns with 2 px gutters), its path (plate x from A
/// to A + L, wrapping off frame or behind a building; 1 / k device px a frame at the review scale), its height,
/// its parallax, and its morph cycle (drawing i shows for Step frames, offset Mph).
/// </summary>
public sealed class CityR17Cloud
{
    public string Id, Band, File;
    public double W, H, Top, A, L, K, X0, Par, Step, Mph;
    public int N, Cols;
}

/// <summary>One car of the train set (its held drawing and window glow, its offset in the set).</summary>
public sealed class CityR17TrainCar
{
    public string Name, File, Glow;
    public double Ox, W, H;
}

/// <summary>A scheduled pass: its first frame in the cycle and its direction (westbound: right to left).</summary>
public readonly struct CityR17TrainPassDef
{
    public readonly int F0;
    public readonly bool West;

    public CityR17TrainPassDef(int f0, bool west)
    {
        F0 = f0;
        West = west;
    }
}

/// <summary>The double-deck train on the viaduct (js/r17train.js's data).</summary>
public sealed class CityR17TrainData
{
    public CityR17TrainCar[] Cars;
    public double[][] Gangways;
    public double[] GangRows, GangColor;
    public double Length, SkirtRow, Pad, DeckA, DeckB, SkirtAbove, LeftMatte, ClipX, Margin, Speed;
    public int LeftMattePlane, ClipPlane, Cycle, Plane;
    public CityR17TrainPassDef[] Passes;
    public double BobPeriod, BobPhase, BobThresh, GlowAlpha;
    public string ParapetFile, PaintFile;
    public double ParapetX, ParapetY, BookSx, BookSy;
}

/// <summary>One ad screen: its id, its rect on the plate (its tiles' top-left) and its tile per frame (run-length: tile, count, ...).</summary>
public sealed class CityR17Screen
{
    public string Id;
    public CityR17Rect Rect;
    public int[] Rle;
}

/// <summary>One hold of a screen's schedule: from which frame which ad shows, and the change that brings it.</summary>
public readonly struct CityR17Hold
{
    public readonly int Start;
    public readonly string Ad, Kind;

    public CityR17Hold(int start, string ad, string kind)
    {
        Start = start;
        Ad = ad;
        Kind = kind;
    }
}

/// <summary>The ads: the master cycle, the sheets, every tile (sheet, x, y, w, h), the screens and their holds.</summary>
public sealed class CityR17AdsData
{
    public int Fps, N, Plane;
    public string[] Sheets;
    public int[] Tiles;
    public CityR17Screen[] Screens;
    public Dictionary<string, int> TransitionFrames;
    public Dictionary<string, CityR17Hold[]> Holds;
}

/// <summary>The furnace fire: the flipbook (tile per frame) and the paint's warm spill, gained per frame.</summary>
public sealed class CityR17FireData
{
    public int N, Plane;
    public CityR17Rect Rect, SpillBaseRect, SpillRect;
    public string[] Sheets;
    public int[] Seq;
    public string SpillBase, Spill;
    public double[] Gain;
}

/// <summary>One smoke stack: the body flipbook (drawn before its plane) and the mouth flipbook (after it).</summary>
public sealed class CityR17SmokeStack
{
    public string Id;
    public CityR17Rect Rect, MouthRect;
    public string[] Sheets, MouthSheets;
    public int[] Seq, MouthSeq;
}

/// <summary>The smoke: the loop length, the plane whose parallax it rides, the stacks.</summary>
public sealed class CityR17SmokeData
{
    public int N, Plane;
    public CityR17SmokeStack[] Stacks;
}

/// <summary>What a noon lamp is (js/r16lights.js).</summary>
public enum CityR17LampKind
{
    Seam,
    Band,
    Ring,
    ExTop,
    Warn,
}

/// <summary>One noon lamp: kind, plate position, the plane it sits on, its tower group (warning lights), its index in its kind.</summary>
public sealed class CityR17Lamp
{
    public CityR17LampKind Kind;
    public double X, Y;
    public int Plane, Group, Index, Count;
}

/// <summary>The noon lamps.</summary>
public sealed class CityR17LightsData
{
    public CityR17Lamp[] Lamps;
}

/// <summary>One of the page's drawers: which system, after which plane, at which order within that plane's slot.</summary>
public readonly struct CityR17OrderEntry
{
    public readonly string System;
    public readonly int Plane;
    public readonly double Order;

    public CityR17OrderEntry(string system, int plane, double order)
    {
        System = system;
        Plane = plane;
        Order = order;
    }
}
