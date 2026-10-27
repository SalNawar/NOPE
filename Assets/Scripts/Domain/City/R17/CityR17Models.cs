using System;
using System.Collections.Generic;

/// <summary>
/// The r17 fleet (CITY-R17): <c>Assets/Data/City/R17/city_r17_vehicles.json</c>, the look-dev's
/// <c>data/r17veh.js</c> (traffic3d/tools/fleet_to_js.py): four toon meshes (taxi, bus, truck, coupe), each one
/// instanced mesh with per-vertex position, normal, colour (alpha = the toon floor tone), attributes (lamp id,
/// livery kind, hull ink alpha) and hull normal. Model frame: x = car left, y = up, z = nose; pivot at the origin.
/// Pure; tested in EditMode.
/// </summary>
public sealed class CityR17Models
{
    /// <summary>The mesh order the renderer and the sim share (js/r17traffic.js MODELS).</summary>
    public static readonly string[] Names = { "taxi", "bus", "truck", "coupe" };

    public CityR17Model[] Models;

    /// <summary>The index of a model name in <see cref="Names"/> (the coupe for an unknown name, as the page's fallback).</summary>
    public static int IndexOf(string name)
    {
        for (int i = 0; i < Names.Length; i++)
            if (Names[i] == name)
                return i;
        return 3;
    }

    /// <summary>Parses the fleet and removes the hover pods (<see cref="DropPods"/>), as the page does at load.</summary>
    public static CityR17Models Parse(string json)
    {
        ContentNode root = ContentJson.Parse(json);
        ContentNode models = CityR17Json.Req(root, "models");
        var m = new CityR17Models { Models = new CityR17Model[Names.Length] };
        for (int i = 0; i < Names.Length; i++)
        {
            ContentNode n = CityR17Json.Req(models, Names[i]);
            var model = new CityR17Model
            {
                Name = Names[i],
                Verts = CityR17Json.Int(n, "verts"),
                Tris = CityR17Json.Int(n, "tris"),
                Nominal = CityR17Json.Nums(n, "nominal"),
                Pos = Floats(Convert.FromBase64String(CityR17Json.ReqStr(n, "pos"))),
                Nrm = Signed(Convert.FromBase64String(CityR17Json.ReqStr(n, "nrm"))),
                Col = Convert.FromBase64String(CityR17Json.ReqStr(n, "col")),
                Att = Convert.FromBase64String(CityR17Json.ReqStr(n, "att")),
                Hull = Signed(Convert.FromBase64String(CityR17Json.ReqStr(n, "hull"))),
                Idx = Ushorts(Convert.FromBase64String(CityR17Json.ReqStr(n, "idx"))),
            };
            IReadOnlyList<ContentNode> bbox = CityR17Json.Items(n, "bbox");
            model.BoxMin = CityR17Json.Nums(bbox[0]);
            model.BoxMax = CityR17Json.Nums(bbox[1]);
            if (model.Pos.Length != model.Verts * 3 || model.Nrm.Length != model.Verts * 4 || model.Col.Length != model.Verts * 4 ||
                model.Att.Length != model.Verts * 4 || model.Hull.Length != model.Verts * 4 || model.Idx.Length != model.Tris * 3)
                throw new FormatException($"city r17 data: the {Names[i]} mesh's arrays do not match its counts.");
            DropPods(model.Pos, model.Att);
            m.Models[i] = model;
        }
        return m;
    }

    /// <summary>
    /// The r17 review's pod removal (js/r17traffic.js dropPods): the hover pods under each body (8-sided shells,
    /// hull alpha 191, the pod-glow lamp 6, or no ink below the lowest body vertex) read as wheel discs, so every
    /// pod vertex collapses onto one point inside the body with no ink: its triangles become degenerate in both passes.
    /// </summary>
    public static int DropPods(float[] pos, byte[] att)
    {
        int n = pos.Length / 3, dropped = 0;
        double body = 1e9, top = -1e9;
        for (int i = 0; i < n; i++)
        {
            if (att[4 * i + 2] == 128)
                body = Math.Min(body, pos[3 * i + 1]);
            top = Math.Max(top, pos[3 * i + 1]);
        }
        double yc = body + 0.25 * (top - body);
        for (int i = 0; i < n; i++)
        {
            int a = att[4 * i + 2];
            bool pod = att[4 * i] == 6 || a == 191 || (a == 0 && pos[3 * i + 1] < body - 1e-3);
            if (!pod)
                continue;
            pos[3 * i] = 0f;
            pos[3 * i + 1] = (float)yc;
            pos[3 * i + 2] = 0f;
            att[4 * i + 2] = 0;
            att[4 * i] = 0;
            dropped++;
        }
        return dropped;
    }

    private static float[] Floats(byte[] b)
    {
        var a = new float[b.Length / 4];
        Buffer.BlockCopy(b, 0, a, 0, a.Length * 4);
        return a;
    }

    private static sbyte[] Signed(byte[] b)
    {
        var a = new sbyte[b.Length];
        Buffer.BlockCopy(b, 0, a, 0, b.Length);
        return a;
    }

    private static ushort[] Ushorts(byte[] b)
    {
        var a = new ushort[b.Length / 2];
        Buffer.BlockCopy(b, 0, a, 0, a.Length * 2);
        return a;
    }
}

/// <summary>
/// One fleet mesh. Per vertex: Pos (x, y, z), Nrm (x, y, z, 0 as int8 / 127), Col (r, g, b, floor tone as bytes),
/// Att (lamp id: 0 body, 1 head, 2 brake, 3 left and 4 right indicator, 5 roof sign, 6 pod glow, 7 trim; livery
/// kind: 0 own colour, 1 body, 2 body2, 3 glass; hull ink alpha: 255 = width 2, 128 = 1, 64 = 0.5, 0 none), Hull
/// (the outline's push direction, int8 / 127). Nominal: the mesh's own length, width, height before a type's dims.
/// </summary>
public sealed class CityR17Model
{
    public string Name;
    public int Verts, Tris;
    public double[] Nominal, BoxMin, BoxMax;
    public float[] Pos;
    public sbyte[] Nrm, Hull;
    public byte[] Col, Att;
    public ushort[] Idx;
}
