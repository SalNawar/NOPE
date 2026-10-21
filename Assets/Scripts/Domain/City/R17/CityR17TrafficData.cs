using System;
using System.Collections.Generic;

/// <summary>
/// The r17 traffic's data (CITY-R17): <c>Assets/Data/City/R17/city_r17_traffic.json</c>, the lanes exactly as the
/// r17 page runs them (after its load-time edits: the dropped C, E and G-DIVE/G-RISE lanes, the K skyway, the
/// thinned heavies on J-E2/J-W2, D-A2 lowered 4 m), the r14 camera and depth table, the vehicle types and dims,
/// the slot rules (LANE_SLOT, LANE_CAP, SLOT_FIX), the type-to-mesh and livery tables and the sim constants
/// (js/r17traffic.js). Pure; tested in EditMode.
/// </summary>
public sealed class CityR17TrafficData
{
    public double CamCx, CamCy, CamF;

    /// <summary>The planes' depths (m) from the farthest (P01) to the nearest (P10); PF their parallax factors (P00 first).</summary>
    public double[] PZ, PF;

    /// <summary>The r14 vehicle types, in table order (a lane table's letter 'a' + i is type i).</summary>
    public string[] Types;

    /// <summary>Each type's dims: length, width, height (m).</summary>
    public Dictionary<string, double[]> Dims;

    public CityR17LaneDef[] Lanes;
    public Dictionary<string, int> LaneSlot, LaneCap;
    public Dictionary<string, double[][]> SlotFix;
    public Dictionary<string, string> ModelOf, LiveryOf;
    public HashSet<string> Heavy;

    /// <summary>Each livery family's colour pairs (body, body2), 0..1.</summary>
    public Dictionary<string, double[][][]> Liveries;

    public uint Seed;
    public double T0, ACurve, Substep, MinZ, CullMargin;
    public int Settle30, Settle120;

    /// <summary>The look: the plate sun (camera space, normalised by the shader), the far air (haze), the ink, the glint switch.</summary>
    public double[] Sun, Haze, Ink;
    public double Glint;

    /// <summary>Parses the traffic file.</summary>
    public static CityR17TrafficData Parse(string json)
    {
        ContentNode root = ContentJson.Parse(json);
        var d = new CityR17TrafficData();
        ContentNode cam = CityR17Json.Req(root, "cam");
        d.CamCx = CityR17Json.Num(cam, "cx");
        d.CamCy = CityR17Json.Num(cam, "cy");
        d.CamF = CityR17Json.Num(cam, "F");
        d.PZ = CityR17Json.Nums(root, "PZ");
        d.PF = CityR17Json.Nums(root, "PF");
        d.Types = CityR17Json.Strs(CityR17Json.Req(root, "types"));
        d.Dims = new Dictionary<string, double[]>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ContentNode> kv in CityR17Json.Req(root, "dims").Members)
            d.Dims[kv.Key] = CityR17Json.Nums(kv.Value);

        IReadOnlyList<ContentNode> lanes = CityR17Json.Items(root, "lanes");
        d.Lanes = new CityR17LaneDef[lanes.Count];
        for (int i = 0; i < lanes.Count; i++)
        {
            ContentNode l = lanes[i];
            var def = new CityR17LaneDef
            {
                Id = CityR17Json.ReqStr(l, "id"),
                Corridor = CityR17Json.ReqStr(l, "corridor"),
                Dir = CityR17Json.ReqStr(l, "dir"),
                Speed = CityR17Json.Num(l, "speed"),
                Headway = CityR17Json.Num(l, "headway"),
                Phase = CityR17Json.Num(l, "phase"),
                K0 = (long)CityR17Json.Num(l, "k0", 0),
                Tab = CityR17Json.ReqStr(l, "tab"),
                Parent = CityR17Json.Str(l, "parent"),
                Pts = CityR17Json.Nums(l, "pts"),
            };
            ContentNode br = l.Get("branch");
            if (br != null && br.Kind == ContentNodeKind.Object)
                def.Branch = new CityR17Branch(CityR17Json.ReqStr(br, "kind") == "merge", (long)CityR17Json.Num(br, "every"), (long)CityR17Json.Num(br, "slot"), CityR17Json.Num(br, "s"));
            d.Lanes[i] = def;
        }

        d.LaneSlot = IntMap(CityR17Json.Req(root, "laneSlot"));
        d.LaneCap = IntMap(CityR17Json.Req(root, "laneCap"));
        d.SlotFix = new Dictionary<string, double[][]>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ContentNode> kv in CityR17Json.Req(root, "slotFix").Members)
        {
            IReadOnlyList<ContentNode> spans = kv.Value.Items;
            var a = new double[spans.Count][];
            for (int k = 0; k < spans.Count; k++)
                a[k] = CityR17Json.Nums(spans[k]);
            d.SlotFix[kv.Key] = a;
        }
        d.ModelOf = StrMap(CityR17Json.Req(root, "modelOf"));
        d.LiveryOf = StrMap(CityR17Json.Req(root, "liveryOf"));
        d.Heavy = new HashSet<string>(CityR17Json.Strs(CityR17Json.Req(root, "heavy")), StringComparer.Ordinal);
        d.Liveries = new Dictionary<string, double[][][]>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ContentNode> kv in CityR17Json.Req(root, "liveries").Members)
        {
            IReadOnlyList<ContentNode> pairs = kv.Value.Items;
            var a = new double[pairs.Count][][];
            for (int k = 0; k < pairs.Count; k++)
                a[k] = new[] { CityR17Json.Hex(pairs[k].Items[0].Text), CityR17Json.Hex(pairs[k].Items[1].Text) };
            d.Liveries[kv.Key] = a;
        }
        ContentNode sim = CityR17Json.Req(root, "sim");
        d.Seed = (uint)CityR17Json.Num(sim, "seed");
        d.T0 = CityR17Json.Num(sim, "t0");
        d.ACurve = CityR17Json.Num(sim, "acurve");
        d.Substep = CityR17Json.Num(sim, "substep");
        d.Settle30 = CityR17Json.Int(sim, "settle30");
        d.Settle120 = CityR17Json.Int(sim, "settle120");
        d.MinZ = CityR17Json.Num(sim, "minZ");
        d.CullMargin = CityR17Json.Num(sim, "cullMargin");
        ContentNode look = CityR17Json.Req(root, "look");
        d.Sun = CityR17Json.Nums(look, "sun");
        d.Haze = CityR17Json.Nums(look, "haze");
        d.Ink = CityR17Json.Nums(look, "ink");
        d.Glint = CityR17Json.Num(look, "glint");
        return d;
    }

    private static Dictionary<string, int> IntMap(ContentNode n)
    {
        var m = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ContentNode> kv in n.Members)
            m[kv.Key] = (int)CityR17Json.Num(kv.Value);
        return m;
    }

    private static Dictionary<string, string> StrMap(ContentNode n)
    {
        var m = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, ContentNode> kv in n.Members)
            m[kv.Key] = kv.Value.Text;
        return m;
    }
}

/// <summary>
/// One lane as the page runs it: its corridor and direction, its r14 speed (m/s) and slot headway (m), its slot
/// clock's phase, its vehicle table (one letter per slot, '.' a gap) and the table's offset, its parent and branch
/// (a diverge or merge ramp: which slots of the parent's clock it takes, where on the parent it joins), and its
/// centreline (x, y, z metres, triplets).
/// </summary>
public sealed class CityR17LaneDef
{
    public string Id, Corridor, Dir, Tab, Parent;
    public double Speed, Headway, Phase;
    public long K0;
    public CityR17Branch? Branch;
    public double[] Pts;
}

/// <summary>A ramp's place on its parent: merge or diverge, the slots it takes (k mod Every == Slot), the parent arc length S where it leaves or joins.</summary>
public readonly struct CityR17Branch
{
    public readonly bool Merge;
    public readonly long Every, Slot;
    public readonly double S;

    public CityR17Branch(bool merge, long every, long slot, double s)
    {
        Merge = merge;
        Every = every;
        Slot = slot;
        S = s;
    }
}
