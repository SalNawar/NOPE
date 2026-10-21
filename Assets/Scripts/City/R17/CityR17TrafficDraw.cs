using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The r17 traffic's draws (CITY-R17): the four fleet meshes (taxi, bus, truck, coupe) drawn GPU-instanced with one
/// shader (<c>NOPE/City R17 Vehicle</c>: three-tone toon shading from the plate sun, unshaded lamps, livery colours,
/// a windscreen glint, depth haze, and an ink hull of constant screen width), in chunks of up to 250 instances whose
/// per-instance data goes through material property block arrays. Each instance's matrix is its model-to-camera
/// frame (metres; mirrored, as the page's: x is the car's left), and the shader projects it through the page's
/// pinhole with its own parallax, so no camera is involved.
/// Occlusion is the page's slot rule: a slot's vehicles draw right after their plane, in two groups (the K skyway
/// first, then the rest), each on a cleared depth buffer as the page's per-group WebGL passes; slots 1-4 are first
/// cut by the P01 and P02 towers. LOD-free: a far vehicle is a few px of the same mesh; ink-free vehicles skip the
/// hull pass.
/// </summary>
public sealed class CityR17TrafficDraw : IDisposable
{
    /// <summary>The most instances one draw carries.</summary>
    public const int Chunk = 250;

    private const int Groups = 22, Models = 4;

    private static readonly int VaId = Shader.PropertyToID("_VA");
    private static readonly int VbId = Shader.PropertyToID("_VB");
    private static readonly int VmId = Shader.PropertyToID("_VM");
    private static readonly int VpId = Shader.PropertyToID("_VP");
    private static readonly int CamId = Shader.PropertyToID("_CityCam");
    private static readonly int FocusId = Shader.PropertyToID("_CityFocus");
    private static readonly int PinId = Shader.PropertyToID("_CityPin");
    private static readonly int SunId = Shader.PropertyToID("_CitySun");
    private static readonly int HazeId = Shader.PropertyToID("_CityHaze");
    private static readonly int InkId = Shader.PropertyToID("_CityInk");

    private readonly CityR17Traffic _sim;
    private readonly CityR17TrafficData _data;
    private readonly Mesh[] _meshes = new Mesh[Models];
    private readonly Material _mat;
    private readonly Matrix4x4[] _m = new Matrix4x4[Chunk];
    private readonly Vector4[] _va = new Vector4[Chunk], _vb = new Vector4[Chunk], _vm = new Vector4[Chunk], _vp = new Vector4[Chunk];
    private readonly int[] _start = new int[Groups * Models + 1], _count = new int[Groups * Models], _inked = new int[Groups * Models];
    private readonly int[] _fillInk = new int[Groups * Models], _fillFree = new int[Groups * Models];
    private CityR17Traffic.Vehicle[] _order = new CityR17Traffic.Vehicle[2048];
    private CityR17Stage _stage;
    private float _hullPush, _glint;

    /// <summary>Instances drawn last frame.</summary>
    public int Drawn { get; private set; }

    /// <summary>Draw calls issued last frame.</summary>
    public int Calls { get; private set; }

    /// <summary>The meshes built from the fleet, one instanced material on the vehicle shader.</summary>
    public CityR17TrafficDraw(CityR17Traffic sim, CityR17TrafficData data, CityR17Models models, Shader shader, float hullPush, float glint)
    {
        _sim = sim;
        _data = data;
        _hullPush = hullPush;
        _glint = glint;
        if (shader == null)
            throw new InvalidOperationException("City R17: the vehicle shader is missing from the config.");
        _mat = new Material(shader) { name = "City R17 vehicles", hideFlags = HideFlags.DontSave, enableInstancing = true };
        for (int i = 0; i < Models; i++)
            _meshes[i] = BuildMesh(models.Models[i]);
    }

    /// <summary>The knobs that may change while running.</summary>
    public void SetKnobs(float hullPush, float glint)
    {
        _hullPush = hullPush;
        _glint = glint;
    }

    private static Mesh BuildMesh(CityR17Model m)
    {
        int n = m.Verts;
        var pos = new Vector3[n];
        var nrm = new Vector3[n];
        var col = new Color32[n];
        var att = new List<Vector4>(n);
        var hull = new List<Vector3>(n);
        for (int i = 0; i < n; i++)
        {
            pos[i] = new Vector3(m.Pos[3 * i], m.Pos[3 * i + 1], m.Pos[3 * i + 2]);
            nrm[i] = new Vector3(m.Nrm[4 * i] / 127f, m.Nrm[4 * i + 1] / 127f, m.Nrm[4 * i + 2] / 127f);
            col[i] = new Color32(m.Col[4 * i], m.Col[4 * i + 1], m.Col[4 * i + 2], m.Col[4 * i + 3]);
            att.Add(new Vector4(m.Att[4 * i], m.Att[4 * i + 1], m.Att[4 * i + 2], m.Att[4 * i + 3]));
            hull.Add(new Vector3(m.Hull[4 * i] / 127f, m.Hull[4 * i + 1] / 127f, m.Hull[4 * i + 2] / 127f));
        }
        var idx = new int[m.Idx.Length];
        for (int i = 0; i < idx.Length; i++)
            idx[i] = m.Idx[i];
        var mesh = new Mesh { name = "City R17 " + m.Name, hideFlags = HideFlags.DontSave };
        mesh.vertices = pos;
        mesh.normals = nrm;
        mesh.colors32 = col;
        mesh.SetUVs(0, att);
        mesh.SetUVs(1, hull);
        mesh.triangles = idx;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f);
        mesh.UploadMeshData(true);
        return mesh;
    }

    /// <summary>The frame's shared constants and the buckets: visible vehicles by (slot group, mesh), the inked ones first.</summary>
    public void Begin(CommandBuffer cmd, CityR17Stage stage, double shiftX, double shiftY, double pxPerPlate)
    {
        _stage = stage;
        Drawn = 0;
        Calls = 0;
        double[] sun = _data.Sun;
        double sn = Math.Sqrt(sun[0] * sun[0] + sun[1] * sun[1] + sun[2] * sun[2]);
        cmd.SetGlobalVector(CamId, new Vector4((float)shiftX, (float)shiftY, 0f, (float)pxPerPlate));
        cmd.SetGlobalVector(FocusId, new Vector4((float)stage.FocusX, (float)stage.FocusY, 0, 0));
        cmd.SetGlobalVector(PinId, new Vector4((float)_data.CamCx, (float)_data.CamCy, (float)_data.CamF, _hullPush));
        cmd.SetGlobalVector(SunId, new Vector4((float)(sun[0] / sn), (float)(sun[1] / sn), (float)(sun[2] / sn), (float)_data.Glint * _glint));
        cmd.SetGlobalVector(HazeId, new Vector4((float)_data.Haze[0], (float)_data.Haze[1], (float)_data.Haze[2], 1));
        cmd.SetGlobalVector(InkId, new Vector4((float)_data.Ink[0], (float)_data.Ink[1], (float)_data.Ink[2], 1));

        Array.Clear(_count, 0, _count.Length);
        Array.Clear(_inked, 0, _inked.Length);
        IReadOnlyList<CityR17Traffic.Vehicle> all = _sim.Vehicles;
        int vis = 0;
        for (int i = 0; i < all.Count; i++)
        {
            CityR17Traffic.Vehicle q = all[i];
            if (!q.Vis)
                continue;
            int key = Key(q);
            if (key < 0)
                continue;
            _count[key]++;
            if (q.Ink > 0)
                _inked[key]++;
            vis++;
        }
        if (_order.Length < vis)
            _order = new CityR17Traffic.Vehicle[Mathf.NextPowerOfTwo(vis)];
        int s = 0;
        for (int k = 0; k < _count.Length; k++)
        {
            _start[k] = s;
            _fillInk[k] = 0;
            _fillFree[k] = 0;
            s += _count[k];
        }
        _start[_count.Length] = s;
        for (int i = 0; i < all.Count; i++)
        {
            CityR17Traffic.Vehicle q = all[i];
            if (!q.Vis)
                continue;
            int key = Key(q);
            if (key < 0)
                continue;
            // inked vehicles fill the bucket from the front, ink-free ones from the back (the hull pass draws the front part)
            int at = q.Ink > 0 ? _start[key] + _fillInk[key]++ : _start[key] + _count[key] - 1 - _fillFree[key]++;
            _order[at] = q;
        }
    }

    private static int Key(CityR17Traffic.Vehicle q)
    {
        int g = CityR17Traffic.Group(q);
        if (g < 0 || g >= Groups)
            return -1;
        return g * Models + q.Model;
    }

    /// <summary>Draws a slot's vehicles: the K skyway group, then the rest, each on cleared depth.</summary>
    public void Draw(CommandBuffer cmd, int slot)
    {
        if (slot < 0 || slot * 2 + 1 >= Groups)
            return;
        DrawGroup(cmd, slot, slot * 2 + 1);
        DrawGroup(cmd, slot, slot * 2);
    }

    private void DrawGroup(CommandBuffer cmd, int slot, int group)
    {
        int total = 0;
        for (int m = 0; m < Models; m++)
            total += _count[group * Models + m];
        if (total == 0)
            return;
        cmd.ClearRenderTarget(true, false, Color.clear);
        if (slot >= 1 && slot <= 4)
            _stage.TowerMasks(cmd, slot);
        for (int m = 0; m < Models; m++)
        {
            int key = group * Models + m, start = _start[key], count = _count[key], inked = _inked[key];
            for (int c = 0; c < count; c += Chunk)
            {
                int n = Math.Min(Chunk, count - c);
                Fill(start + c, n);
                MaterialPropertyBlock b = _stage.NextBlock();
                b.SetVectorArray(VaId, _va);
                b.SetVectorArray(VbId, _vb);
                b.SetVectorArray(VmId, _vm);
                b.SetVectorArray(VpId, _vp);
                cmd.DrawMeshInstanced(_meshes[m], 0, _mat, 0, _m, n, b);
                Calls++;
                int hulls = Math.Min(n, Math.Max(0, inked - c));
                if (hulls > 0)
                {
                    cmd.DrawMeshInstanced(_meshes[m], 0, _mat, 1, _m, hulls, b);
                    Calls++;
                }
                Drawn += n;
            }
        }
    }

    private void Fill(int from, int n)
    {
        for (int i = 0; i < n; i++)
        {
            CityR17Traffic.Vehicle q = _order[from + i];
            Matrix4x4 m = Matrix4x4.identity;
            // model to camera metres: x is the car's left (-right), y up, z the nose, each scaled by the type's dims
            m.m00 = (float)(-q.R0 * q.Sx); m.m10 = (float)(-q.R1 * q.Sx); m.m20 = (float)(-q.R2 * q.Sx);
            m.m01 = (float)(q.U0 * q.Sy); m.m11 = (float)(q.U1 * q.Sy); m.m21 = (float)(q.U2 * q.Sy);
            m.m02 = (float)(q.F0 * q.Sz); m.m12 = (float)(q.F1 * q.Sz); m.m22 = (float)(q.F2 * q.Sz);
            m.m03 = (float)q.X; m.m13 = (float)q.Y; m.m23 = (float)q.Z;
            _m[i] = m;
            _va[i] = new Vector4((float)q.Ar, (float)q.Ag, (float)q.Ab, (float)q.Haze);
            _vb[i] = new Vector4((float)q.Br, (float)q.Bg, (float)q.Bb, (float)q.Ink);
            _vm[i] = new Vector4((float)q.Brake, q.Sign, q.Blink, (float)q.LenPx);
            _vp[i] = new Vector4((float)q.F, 0, 0, 0);
        }
    }

    /// <summary>Releases the meshes and the material.</summary>
    public void Dispose()
    {
        foreach (Mesh m in _meshes)
            if (m != null)
                UnityEngine.Object.Destroy(m);
        UnityEngine.Object.Destroy(_mat);
    }
}
