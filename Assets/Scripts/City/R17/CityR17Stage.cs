using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

/// <summary>
/// Draws the r17 city into a render texture (CITY-R17): one command buffer a frame, run with
/// <see cref="Graphics.ExecuteCommandBuffer"/> before the cameras render, so the city needs no camera, no layer and
/// no renderer entry (the forward renderer's contact-shading feature and the 2D renderer's sorting never touch it).
/// The frame follows the page's own order (<see cref="CityR17Order"/>): each plane, then what the page draws in the
/// slot after it. World units are plate px with y up (a plate point (x, y) is (x, -y)); everything at parallax f is
/// placed as the page's setDepth places it. The city composites in the look-dev's gamma space (the textures are
/// imported without sRGB decoding and the target is a linear-format texture), so its blends match the canvas; the
/// two readers (the city view's image and the hall's windows) decode it once.
/// </summary>
public sealed class CityR17Stage : IDisposable
{
    private static readonly int VpId = Shader.PropertyToID("_CityVP");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int UvId = Shader.PropertyToID("_UV");
    private static readonly int Uv2Id = Shader.PropertyToID("_UV2");
    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int HazeId = Shader.PropertyToID("_Haze");
    private static readonly int ClipXId = Shader.PropertyToID("_ClipX");
    private static readonly int MorphId = Shader.PropertyToID("_Morph");

    /// <summary>The sprite shader's passes.</summary>
    public const int PassAlpha = 0, PassAdd = 1, PassDepthMask = 2, PassMorph = 3;

    private readonly CityR17ConfigSO _config;
    private readonly CityR17Scene _scene;
    private readonly CityR17Art _art;
    private readonly List<CityR17Order.Step> _steps;
    private readonly Material _sprite, _lamp;
    private readonly Mesh _quad;
    private readonly Mesh[] _lampMeshes;
    private readonly Color32[][] _lampColors;
    private readonly List<CityR17Lamp>[] _lampsByPlane;
    private readonly int[][] _adFrames;
    private readonly List<MaterialPropertyBlock> _blocks = new List<MaterialPropertyBlock>(128);
    private readonly CommandBuffer _cmd = new CommandBuffer { name = "City R17" };
    private readonly Texture2D[] _planeTex;
    private readonly CityR17Rect[] _planeRect;
    private readonly Texture2D[] _cloudTex, _adTex, _trainCars, _trainGlows;
    private readonly Texture2D _fireTex, _spillBase, _spill, _parapet, _paint;
    private readonly Texture2D[] _smokeTex, _mouthTex;
    private int _blocksUsed;
    private RenderTexture _target, _output;

    /// <summary>The traffic's draws (null without a fleet).</summary>
    public CityR17TrafficDraw Traffic;

    /// <summary>A stage for the parsed scene and art map, with the config's shaders and textures.</summary>
    public CityR17Stage(CityR17ConfigSO config, CityR17Scene scene, CityR17Art art)
    {
        _config = config;
        _scene = scene;
        _art = art;
        _steps = CityR17Order.Steps(scene);
        _sprite = NewMaterial(config.spriteShader, "City R17 sprites");
        _lamp = NewMaterial(config.lampShader, "City R17 lamps");
        _quad = UnitQuad();

        _planeTex = new Texture2D[scene.Planes.Length];
        _planeRect = new CityR17Rect[scene.Planes.Length];
        for (int i = 0; i < scene.Planes.Length; i++)
        {
            CityR17Art.Entry e = Entry("Planes/" + scene.Planes[i].File);
            _planeTex[i] = Tex(scene.Planes[i].File);
            _planeRect[i] = CityR17Art.PaddedPlate(e);
        }
        _cloudTex = new Texture2D[scene.Clouds.List.Length];
        for (int i = 0; i < _cloudTex.Length; i++)
            _cloudTex[i] = Tex(scene.Clouds.List[i].File);
        _adTex = new Texture2D[scene.Ads.Sheets.Length];
        for (int i = 0; i < _adTex.Length; i++)
            _adTex[i] = Tex(scene.Ads.Sheets[i]);
        _adFrames = new int[scene.Ads.Screens.Length][];
        for (int i = 0; i < _adFrames.Length; i++)
            _adFrames[i] = CityR17Loops.Expand(scene.Ads.Screens[i].Rle, scene.Ads.N);
        _fireTex = Tex(scene.Fire.Sheets[0]);
        _spillBase = Tex(scene.Fire.SpillBase);
        _spill = Tex(scene.Fire.Spill);
        _smokeTex = new Texture2D[scene.Smoke.Stacks.Length];
        _mouthTex = new Texture2D[scene.Smoke.Stacks.Length];
        for (int i = 0; i < _smokeTex.Length; i++)
        {
            _smokeTex[i] = Tex(scene.Smoke.Stacks[i].Sheets[0]);
            _mouthTex[i] = Tex(scene.Smoke.Stacks[i].MouthSheets[0]);
        }
        _trainCars = new Texture2D[scene.Train.Cars.Length];
        _trainGlows = new Texture2D[scene.Train.Cars.Length];
        for (int i = 0; i < _trainCars.Length; i++)
        {
            _trainCars[i] = Tex(scene.Train.Cars[i].File);
            _trainGlows[i] = Tex(scene.Train.Cars[i].Glow);
        }
        _parapet = Tex(scene.Train.ParapetFile);
        _paint = Tex(scene.Train.PaintFile);

        _lampsByPlane = new List<CityR17Lamp>[scene.Planes.Length];
        foreach (CityR17Lamp lamp in scene.Lights.Lamps)
            if (lamp.Plane >= 0 && lamp.Plane < _lampsByPlane.Length)
                (_lampsByPlane[lamp.Plane] ??= new List<CityR17Lamp>()).Add(lamp);
        _lampMeshes = new Mesh[scene.Planes.Length];
        _lampColors = new Color32[scene.Planes.Length][];
        for (int p = 0; p < _lampsByPlane.Length; p++)
            if (_lampsByPlane[p] != null)
                _lampMeshes[p] = LampMesh(_lampsByPlane[p], out _lampColors[p]);
    }

    /// <summary>What the city view and the hall's windows show (the resolved, mipmapped frame).</summary>
    public RenderTexture Output => _output;

    /// <summary>The dolly's focus (plate px), slightly above the Hall plinth.</summary>
    public double FocusX => _scene.FocusX;

    /// <summary>The dolly's focus (plate px).</summary>
    public double FocusY => _scene.FocusY;

    /// <summary>The framing the targets were sized for.</summary>
    public CityR17Framing Framing { get; private set; }

    /// <summary>The plate rect the last frame showed (x, y, w, h; plate px, y down), with the fill mode's pan.</summary>
    public Vector4 View { get; private set; }

    /// <summary>
    /// Sizes the targets for a framing (recreating them when the size changes; the output keeps its object, so a
    /// reader holding it stays valid).
    /// </summary>
    public void Resize(CityR17Framing framing)
    {
        Framing = framing;
        int w = framing.OutW, h = framing.OutH, msaa = Mathf.ClosestPowerOfTwo(Mathf.Clamp(_config.msaa, 1, 8));
        if (_target == null || _target.width != w || _target.height != h || _target.antiAliasing != msaa)
        {
            if (_target != null)
            {
                _target.Release();
                UnityEngine.Object.Destroy(_target);
            }
            var desc = new RenderTextureDescriptor(w, h, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D24_UNorm_S8_UInt)
            {
                msaaSamples = msaa,
                useMipMap = false,
                autoGenerateMips = false,
                sRGB = false,
            };
            _target = new RenderTexture(desc) { name = "City R17 (draw)", hideFlags = HideFlags.DontSave };
            _target.Create();
        }
        if (_output == null)
        {
            var desc = new RenderTextureDescriptor(w, h, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None)
            {
                useMipMap = true,
                autoGenerateMips = false,
                sRGB = false,
            };
            _output = new RenderTexture(desc) { name = "City R17", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp };
            _output.Create();
        }
        else if (_output.width != w || _output.height != h)
        {
            _output.Release();
            _output.width = w;
            _output.height = h;
            _output.Create();
        }
    }

    // ------------------------------------------------------------------ the frame

    /// <summary>Everything a frame needs: the clock, the look, and the comfort switches.</summary>
    public struct FrameState
    {
        /// <summary>The loops' clock (s).</summary>
        public double Seconds;

        /// <summary>Plate px of shift per unit parallax (x, y) from the look.</summary>
        public double ShiftX, ShiftY;

        /// <summary>The fill mode's pan (plate px; 0 when letterboxed).</summary>
        public double Pan;

        /// <summary>Reduced Motion: the smoke and the fire hold one drawing.</summary>
        public bool Reduced;
    }

    private FrameState _f;
    private double _sc, _x0, _y0;

    /// <summary>Records and runs the whole frame into <see cref="Output"/>.</summary>
    public void Render(in FrameState state)
    {
        if (_target == null)
            return;
        _f = state;
        _blocksUsed = 0;
        CityR17Framing fr = Framing;
        _sc = fr.PxPerPlate;
        _x0 = fr.X + state.Pan;
        _y0 = fr.Y;
        View = new Vector4((float)_x0, (float)_y0, (float)fr.W, (float)fr.H);
        Matrix4x4 proj = Matrix4x4.Ortho((float)_x0, (float)(_x0 + fr.W), (float)-(_y0 + fr.H), (float)-_y0, -100f, 100f);
        Matrix4x4 vp = GL.GetGPUProjectionMatrix(proj, true);

        CommandBuffer cmd = _cmd;
        cmd.Clear();
        cmd.SetRenderTarget(_target);
        cmd.ClearRenderTarget(true, true, Color.black);
        cmd.SetGlobalMatrix(VpId, vp);
        Traffic?.Begin(cmd, this, state.ShiftX, state.ShiftY, _sc);
        for (int i = 0; i < _steps.Count; i++)
        {
            CityR17Order.Step s = _steps[i];
            switch (s.System)
            {
                case "plane": DrawPlane(cmd, s.Plane); break;
                case "clouds": DrawClouds(cmd); break;
                case "traffic": Traffic?.Draw(cmd, s.Plane); break;
                case "lights": DrawLamps(cmd, s.Plane); break;
                case "ads": DrawAds(cmd); break;
                case "train": DrawTrain(cmd); break;
                case "smoke": DrawSmoke(cmd, false); break;
                case "fire": DrawFire(cmd); break;
                case "smokeMouth": DrawSmoke(cmd, true); break;
            }
        }
        cmd.Blit(_target, _output);
        cmd.GenerateMips(_output);
        Graphics.ExecuteCommandBuffer(cmd);
    }

    // ------------------------------------------------------------------ placement (the page's setDepth)

    /// <summary>The world x of plate x at parallax f.</summary>
    public double WorldX(double x, double f) => _scene.FocusX + (x - _scene.FocusX) * DollyScale(f) + _f.ShiftX * f;

    /// <summary>The plate y (down) of plate y at parallax f.</summary>
    public double PlacedY(double y, double f) => _scene.FocusY + (y - _scene.FocusY) * DollyScale(f) + _f.ShiftY * f;

    /// <summary>The dolly's scale at parallax f (the dolly is the opt-in sway's, off: 1).</summary>
    public double DollyScale(double f) => 1;

    /// <summary>Snaps a placed plate x to the output's pixel grid (held drawings never resample).</summary>
    private double SnapX(double x) => _x0 + Math.Round((x - _x0) * _sc) / _sc;

    private double SnapY(double y) => _y0 + Math.Round((y - _y0) * _sc) / _sc;

    /// <summary>The matrix of the unit quad over a placed plate rect (x, y the top-left, plate px, y down).</summary>
    private static Matrix4x4 RectMatrix(double x, double y, double w, double h)
    {
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = (float)w;
        m.m03 = (float)x;
        m.m11 = (float)h;
        m.m13 = (float)-(y + h);
        return m;
    }

    /// <summary>The matrix that moves rest-position geometry (plate (x, -y)) as a plane with parallax f moves.</summary>
    private Matrix4x4 Glue(double f)
    {
        double s = DollyScale(f);
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = (float)s;
        m.m11 = (float)s;
        m.m03 = (float)(_scene.FocusX * (1 - s) + _f.ShiftX * f);
        m.m13 = (float)-(_scene.FocusY * (1 - s) + _f.ShiftY * f);
        return m;
    }

    /// <summary>A plate rect at parallax f, placed.</summary>
    private Matrix4x4 Placed(double x, double y, double w, double h, double f)
    {
        double s = DollyScale(f);
        return RectMatrix(WorldX(x, f), PlacedY(y, f), w * s, h * s);
    }

    private bool InView(double x, double y, double w, double h) =>
        x + w >= _x0 - 2 && x <= _x0 + Framing.W + 2 && y + h >= _y0 - 2 && y <= _y0 + Framing.H + 2;

    // ------------------------------------------------------------------ draws

    private MaterialPropertyBlock Block()
    {
        if (_blocksUsed == _blocks.Count)
            _blocks.Add(new MaterialPropertyBlock());
        MaterialPropertyBlock b = _blocks[_blocksUsed++];
        return b;
    }

    /// <summary>A pooled property block for this frame (the traffic's chunks share the pool).</summary>
    public MaterialPropertyBlock NextBlock() => Block();

    /// <summary>
    /// Draws a texture region (px, y down, in a texture of the art map's padded size) over a placed plate rect, with a
    /// tint (rgb multiply, a opacity), an optional haze tint (rgb, amount) and a world-x window.
    /// </summary>
    private void Sprite(CommandBuffer cmd, int pass, Texture tex, double sizeW, double sizeH, CityR17Rect src, Matrix4x4 m, Vector4 tint,
        Vector4 haze, bool mirror = false, float clipMin = -1e9f, float clipMax = 1e9f)
    {
        if (tex == null)
            return;
        MaterialPropertyBlock b = Block();
        b.SetTexture(MainTexId, tex);
        b.SetVector(UvId, Uv(src, sizeW, sizeH, mirror));
        b.SetVector(TintId, tint);
        b.SetVector(HazeId, haze);
        b.SetVector(ClipXId, new Vector4(clipMin, clipMax, 0, 0));
        cmd.DrawMesh(_quad, m, _sprite, 0, pass, b);
    }

    private static Vector4 Uv(CityR17Rect src, double sizeW, double sizeH, bool mirror)
    {
        float u0 = (float)(src.X / sizeW), du = (float)(src.W / sizeW);
        float v0 = (float)(1 - (src.Y + src.H) / sizeH), dv = (float)(src.H / sizeH);
        return mirror ? new Vector4(u0 + du, v0, -du, dv) : new Vector4(u0, v0, du, dv);
    }

    private static readonly Vector4 White = new Vector4(1, 1, 1, 1), NoHaze = Vector4.zero;

    private void DrawPlane(CommandBuffer cmd, int p)
    {
        CityR17Plane plane = _scene.Planes[p];
        CityR17Rect r = _planeRect[p];
        Texture2D tex = _planeTex[p];
        if (tex == null)
            return;
        double[] hz = _scene.HazeColor;
        var haze = new Vector4((float)hz[0], (float)hz[1], (float)hz[2], (float)Math.Min(0.85, plane.Haze));
        CityR17Art.Entry e = Entry("Planes/" + plane.File);
        Sprite(cmd, PassAlpha, tex, e.SizeW, e.SizeH, new CityR17Rect(0, 0, e.SizeW, e.SizeH), Placed(r.X, r.Y, r.W, r.H, plane.Parallax), White, haze);
    }

    /// <summary>The far planes P01 (and P02 from slot 2) written into depth as the nearest thing, so the far traffic never draws on a megatower face (the page cuts slots 1-4 with the towers' own alpha).</summary>
    public void TowerMasks(CommandBuffer cmd, int slot)
    {
        for (int li = 1; li <= Math.Min(2, slot); li++)
        {
            CityR17Plane plane = _scene.Planes[li];
            CityR17Rect r = _planeRect[li];
            CityR17Art.Entry e = Entry("Planes/" + plane.File);
            Sprite(cmd, PassDepthMask, _planeTex[li], e.SizeW, e.SizeH, new CityR17Rect(0, 0, e.SizeW, e.SizeH), Placed(r.X, r.Y, r.W, r.H, plane.Parallax), White, NoHaze);
        }
    }

    private void DrawClouds(CommandBuffer cmd)
    {
        CityR17CloudSet set = _scene.Clouds;
        double f = _f.Seconds * _scene.Fps;
        for (int i = set.List.Length - 1; i >= 0; i--)   // the config lists near, mid, far: far first
        {
            CityR17Cloud c = set.List[i];
            Texture2D tex = _cloudTex[i];
            if (tex == null)
                continue;
            double x = CityR17Loops.CloudX(c, f, set.ScRef) - set.Pad, y = c.Top - set.Pad;
            double wx = WorldX(x, c.Par), wy = PlacedY(y, c.Par);
            if (!InView(wx, wy, c.W, c.H))
                continue;
            CityR17Loops.CloudDrawing(c, f, out int j, out double w);
            CityR17Art.Entry e = Entry("Clouds/" + c.File);
            CityR17Rect a = CityR17Loops.CloudCell(c, j), b = CityR17Loops.CloudCell(c, (j + 1) % c.N);
            MaterialPropertyBlock blk = Block();
            blk.SetTexture(MainTexId, tex);
            blk.SetVector(UvId, Uv(a, e.SizeW, e.SizeH, false));
            blk.SetVector(Uv2Id, Uv(b, e.SizeW, e.SizeH, false));
            blk.SetFloat(MorphId, (float)w);
            blk.SetVector(TintId, White);
            blk.SetVector(HazeId, NoHaze);
            blk.SetVector(ClipXId, new Vector4(-1e9f, 1e9f, 0, 0));
            cmd.DrawMesh(_quad, RectMatrix(wx, wy, c.W, c.H), _sprite, 0, PassMorph, blk);
        }
    }

    private void DrawLamps(CommandBuffer cmd, int p)
    {
        Mesh mesh = _lampMeshes[p];
        if (mesh == null)
            return;
        List<CityR17Lamp> lamps = _lampsByPlane[p];
        Color32[] cols = _lampColors[p];
        double t = _f.Seconds;
        for (int i = 0; i < lamps.Count; i++)
        {
            byte a = (byte)Math.Round(Math.Max(0, Math.Min(1, CityR17Loops.LampAlpha(lamps[i], t))) * 255);
            int v = i * 8;   // each lamp: its cel lamp quad (4) then its base disc quad (4)
            for (int k = 0; k < 4; k++)
                cols[v + k].a = a;
        }
        mesh.colors32 = cols;
        cmd.DrawMesh(mesh, Glue(_scene.ParallaxOf(p)), _lamp, 0, 0);
    }

    private void DrawAds(CommandBuffer cmd)
    {
        CityR17AdsData ads = _scene.Ads;
        long f = CityR17Loops.Frame(_f.Seconds, ads.Fps);
        int fi = (int)CityR17Json.Mod(f, ads.N);
        double par = _scene.ParallaxOf(ads.Plane);
        for (int i = 0; i < ads.Screens.Length; i++)
        {
            int t = _adFrames[i][fi] * 5;
            int sheet = ads.Tiles[t];
            CityR17Art.Entry e = Entry("Ads/" + ads.Sheets[sheet]);
            var src = new CityR17Rect(ads.Tiles[t + 1], ads.Tiles[t + 2], ads.Tiles[t + 3], ads.Tiles[t + 4]);
            if (!CityR17Loops.ClipTile(src, e.UsedW, e.UsedH, out CityR17Rect clip, out double dx, out double dy))
                continue;
            CityR17Rect r = ads.Screens[i].Rect;
            Sprite(cmd, PassAlpha, _adTex[sheet], e.SizeW, e.SizeH, clip, Placed(r.X + dx, r.Y + dy, clip.W, clip.H, par), White, NoHaze);
        }
    }

    private void Flip(CommandBuffer cmd, int[] seq5, string[] sheets, Texture2D tex, CityR17Rect at, long f, double par, string folder)
    {
        CityR17Loops.Tile(seq5, f, out int sheet, out CityR17Rect src);
        CityR17Art.Entry e = Entry(folder + "/" + sheets[sheet]);
        if (!CityR17Loops.ClipTile(src, e.UsedW, e.UsedH, out CityR17Rect clip, out double dx, out double dy))
            return;
        Sprite(cmd, PassAlpha, tex, e.SizeW, e.SizeH, clip, Placed(at.X + dx, at.Y + dy, clip.W, clip.H, par), White, NoHaze);
    }

    private void DrawSmoke(CommandBuffer cmd, bool mouth)
    {
        CityR17SmokeData sm = _scene.Smoke;
        long f = _f.Reduced ? 0 : CityR17Loops.Frame(_f.Seconds, _scene.Fps);
        double par = _scene.ParallaxOf(sm.Plane);
        for (int i = 0; i < sm.Stacks.Length; i++)
        {
            CityR17SmokeStack st = sm.Stacks[i];
            if (mouth)
                Flip(cmd, st.MouthSeq, st.MouthSheets, _mouthTex[i], st.MouthRect, f, par, "Smoke");
            else
                Flip(cmd, st.Seq, st.Sheets, _smokeTex[i], st.Rect, f, par, "Smoke");
        }
    }

    private void DrawFire(CommandBuffer cmd)
    {
        CityR17FireData fire = _scene.Fire;
        long f = _f.Reduced ? 0 : CityR17Loops.Frame(_f.Seconds, _scene.Fps);
        double par = _scene.ParallaxOf(fire.Plane);
        CityR17Art.Entry be = Entry("Fire/" + fire.SpillBase), se = Entry("Fire/" + fire.Spill);
        CityR17Rect B = fire.SpillBaseRect, P = fire.SpillRect;
        Sprite(cmd, PassAlpha, _spillBase, be.SizeW, be.SizeH, new CityR17Rect(0, 0, be.UsedW, be.UsedH), Placed(B.X, B.Y, B.W, B.H, par), White, NoHaze);
        float gain = (float)CityR17Loops.SpillGain(fire, f);
        Sprite(cmd, PassAdd, _spill, se.SizeW, se.SizeH, new CityR17Rect(0, 0, se.UsedW, se.UsedH), Placed(P.X, P.Y, P.W, P.H, par), new Vector4(1, 1, 1, gain), NoHaze);
        Flip(cmd, fire.Seq, fire.Sheets, _fireTex, fire.Rect, f, par, "Fire");
    }

    private void DrawTrain(CommandBuffer cmd)
    {
        CityR17TrainData d = _scene.Train;
        double f7 = _scene.ParallaxOf(d.Plane);
        CityR17Art.Entry pe = Entry("Train/" + d.PaintFile);
        if (pe != null && _paint != null)
        {
            CityR17Rect pr = CityR17Art.PaddedPlate(pe);
            Sprite(cmd, PassAlpha, _paint, pe.SizeW, pe.SizeH, new CityR17Rect(0, 0, pe.SizeW, pe.SizeH), Placed(pr.X, pr.Y, pr.W, pr.H, f7), White, NoHaze);
        }
        double frames = _f.Seconds * _scene.Fps;
        CityR17Loops.TrainPass pass = CityR17Loops.Pass(d, frames, Math.Max(0.05, _sc));
        if (pass.Active)
        {
            float xl = (float)WorldX(d.LeftMatte, _scene.ParallaxOf(d.LeftMattePlane)), xr = (float)WorldX(d.ClipX, _scene.ParallaxOf(d.ClipPlane));
            double west = CityR17Loops.TrainWestEnd(d, pass, Math.Max(0.05, _sc));
            double secondsOnFrame = Math.Floor(frames + 1e-4) / _scene.Fps;
            int cars = d.Cars.Length;
            Span<double> tops = stackalloc double[8];
            Span<double> xs = stackalloc double[8];
            Span<int> which = stackalloc int[8];
            Span<bool> mirrored = stackalloc bool[8];
            for (int i = 0; i < cars && i < 8; i++)
            {
                CityR17Loops.TrainCar(d, pass.West, i, out int car, out double ox, out bool mir);
                double w = d.Cars[car].W, xc = west + ox + w / 2;
                double top = CityR17Loops.TrainCarTop(d, xc, w, pass.Index, i, secondsOnFrame, out bool lifted);
                tops[i] = SnapY(PlacedY(top, f7)) - (lifted ? 1 / _sc : 0);
                xs[i] = SnapX(WorldX(west + ox, f7));
                which[i] = car;
                mirrored[i] = mir;
            }
            // gangway bellows behind the car ends
            double[] g = d.GangColor;
            var gang = new Vector4((float)g[0], (float)g[1], (float)g[2], 1);
            for (int j = 0; j < d.Gangways.Length && j + 1 < cars; j++)
            {
                double gx = CityR17Loops.TrainGangway(d, pass.West, j);
                double y0 = Math.Max(tops[j], tops[j + 1]);
                double x = SnapX(WorldX(west + gx, f7)) - 1 / _sc;
                Sprite(cmd, PassAlpha, Texture2D.whiteTexture, 4, 4, new CityR17Rect(1, 1, 2, 2),
                    RectMatrix(x, y0 + d.GangRows[0], d.Gangways[j][1] + 2 / _sc, d.GangRows[1] - d.GangRows[0]), gang, NoHaze, false, xl, xr);
            }
            for (int i = 0; i < cars && i < 8; i++)
            {
                CityR17TrainCar c = d.Cars[which[i]];
                CityR17Art.Entry ce = Entry("Train/" + c.File);
                Sprite(cmd, PassAlpha, _trainCars[which[i]], ce.SizeW, ce.SizeH, new CityR17Rect(0, 0, ce.UsedW, ce.UsedH),
                    RectMatrix(xs[i], tops[i], ce.UsedW, ce.UsedH), White, NoHaze, mirrored[i], xl, xr);
            }
            if (d.GlowAlpha > 0)
                for (int i = 0; i < cars && i < 8; i++)
                {
                    CityR17TrainCar c = d.Cars[which[i]];
                    CityR17Art.Entry ge = Entry("Train/" + c.Glow);
                    Sprite(cmd, PassAdd, _trainGlows[which[i]], ge.SizeW, ge.SizeH, new CityR17Rect(0, 0, ge.UsedW, ge.UsedH),
                        RectMatrix(xs[i] - d.Pad, tops[i] - d.Pad, ge.UsedW, ge.UsedH), new Vector4(1, 1, 1, (float)d.GlowAlpha), NoHaze, mirrored[i], xl, xr);
                }
        }
        // the r9 parapet BOOK on every frame: the band at the train's plane, its top right at the industry's
        CityR17Art.Entry be = Entry("Train/" + d.ParapetFile);
        if (be == null || _parapet == null)
            return;
        double bw = be.UsedW, bh = be.UsedH, sx = d.BookSx, sy = d.BookSy, X0 = d.ParapetX, Y0 = d.ParapetY, f9 = _scene.ParallaxOf(d.ClipPlane);
        Sprite(cmd, PassAlpha, _parapet, be.SizeW, be.SizeH, new CityR17Rect(0, sy, bw, bh - sy), Placed(X0, Y0 + sy, bw, bh - sy, f7), White, NoHaze);
        if (sx > 0)
            Sprite(cmd, PassAlpha, _parapet, be.SizeW, be.SizeH, new CityR17Rect(0, 0, sx, sy), Placed(X0, Y0, sx, sy, f7), White, NoHaze);
        if (sx < bw)
            Sprite(cmd, PassAlpha, _parapet, be.SizeW, be.SizeH, new CityR17Rect(sx, 0, bw - sx, sy), Placed(X0 + sx, Y0, bw - sx, sy, f9), White, NoHaze);
    }

    // ------------------------------------------------------------------ build helpers

    private CityR17Art.Entry Entry(string path) => _art.Get(path);

    private Texture2D Tex(string file)
    {
        Texture2D t = _config.Art(file);
        if (t == null)
            Debug.LogWarning("City R17: the config has no texture for " + file + "; it is skipped.");
        return t;
    }

    private static Material NewMaterial(Shader shader, string name)
    {
        if (shader == null)
            throw new InvalidOperationException("City R17: a shader is missing from the config (" + name + ").");
        return new Material(shader) { name = name, hideFlags = HideFlags.DontSave, enableInstancing = true };
    }

    private static Mesh UnitQuad()
    {
        var m = new Mesh { name = "City R17 quad", hideFlags = HideFlags.DontSave };
        m.SetVertices(new List<Vector3> { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) });
        m.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
        m.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
        m.bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f);
        return m;
    }

    /// <summary>
    /// One mesh for a plane's lamps at rest: per lamp the cel lamp (a quad of 2.6 r round its point: halo ring, core,
    /// white centre; uv.z 0) and, for the ring lamps, the small painted base disc it sits on (uv.z 1; others get a
    /// collapsed quad). The alpha is written per frame (<see cref="CityR17Loops.LampAlpha"/>).
    /// </summary>
    private static Mesh LampMesh(List<CityR17Lamp> lamps, out Color32[] colors)
    {
        int n = lamps.Count;
        var verts = new Vector3[n * 8];
        var uvs = new Vector3[n * 8];
        colors = new Color32[n * 8];
        var tris = new int[n * 12];
        for (int i = 0; i < n; i++)
        {
            CityR17Lamp lamp = lamps[i];
            CityR17Loops.LampLook(lamp.Kind, out double radius, out double r, out double g, out double b);
            var col = new Color32((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255), 0);
            float half = (float)(radius * 2.6), x = (float)lamp.X, y = (float)-lamp.Y;
            int v = i * 8;
            Corners(verts, uvs, v, x, y, half, 0);
            for (int k = 0; k < 4; k++)
                colors[v + k] = col;
            bool disc = lamp.Kind == CityR17LampKind.Ring;
            Corners(verts, uvs, v + 4, x, y, disc ? 1.1f : 0f, 1);
            for (int k = 0; k < 4; k++)
                colors[v + 4 + k] = new Color32(0xd8, 0xc7, 0x9a, 255);
            int t = i * 12;
            tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1; tris[t + 3] = v + 2; tris[t + 4] = v + 3; tris[t + 5] = v + 1;
            // the base disc first in the index order would draw over the lamp: the lamp's quad is listed after it
            tris[t + 6] = v + 4; tris[t + 7] = v + 6; tris[t + 8] = v + 5; tris[t + 9] = v + 6; tris[t + 10] = v + 7; tris[t + 11] = v + 5;
        }
        // draw order within the mesh: every base disc, then every lamp
        var ordered = new int[tris.Length];
        int o = 0;
        for (int i = 0; i < n; i++)
            for (int k = 6; k < 12; k++)
                ordered[o++] = tris[i * 12 + k];
        for (int i = 0; i < n; i++)
            for (int k = 0; k < 6; k++)
                ordered[o++] = tris[i * 12 + k];
        var mesh = new Mesh { name = "City R17 lamps", hideFlags = HideFlags.DontSave };
        mesh.vertices = verts;
        mesh.SetUVs(0, uvs);
        mesh.colors32 = colors;
        mesh.triangles = ordered;
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1e6f);
        mesh.MarkDynamic();
        return mesh;
    }

    private static void Corners(Vector3[] verts, Vector3[] uvs, int v, float x, float y, float half, float mode)
    {
        verts[v] = new Vector3(x - half, y - half, 0);
        verts[v + 1] = new Vector3(x + half, y - half, 0);
        verts[v + 2] = new Vector3(x - half, y + half, 0);
        verts[v + 3] = new Vector3(x + half, y + half, 0);
        uvs[v] = new Vector3(-1, -1, mode);
        uvs[v + 1] = new Vector3(1, -1, mode);
        uvs[v + 2] = new Vector3(-1, 1, mode);
        uvs[v + 3] = new Vector3(1, 1, mode);
    }

    /// <summary>Releases the targets, materials and meshes.</summary>
    public void Dispose()
    {
        _cmd.Release();
        Traffic?.Dispose();
        foreach (RenderTexture rt in new[] { _target, _output })
            if (rt != null)
            {
                rt.Release();
                UnityEngine.Object.Destroy(rt);
            }
        _target = _output = null;
        UnityEngine.Object.Destroy(_sprite);
        UnityEngine.Object.Destroy(_lamp);
        UnityEngine.Object.Destroy(_quad);
        foreach (Mesh m in _lampMeshes)
            if (m != null)
                UnityEngine.Object.Destroy(m);
    }
}
