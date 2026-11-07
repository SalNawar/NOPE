#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// The QA rig's 30 fps recorder (FIX_PLAN "SEQ capture"; promoted from the
/// DOC audit's _DocAuditRec). It records a sequence one of two ways.
/// With <see cref="FixedStep"/> on (the default) Time.captureFramerate is 30,
/// so the game advances exactly 1/30 s per frame however slow the capture
/// is, and every frame is captured at the end of the frame, scaled to
/// <see cref="CaptureWidth"/> wide and saved as f0000.png, f0001.png ...
/// With it off (the realtime op) the sequence is a timing one: no picture is
/// captured and nothing is written or allocated per frame (the rows wait in a
/// buffer reserved up front and are written by <see cref="Stop"/>), so its
/// frame times are the game's, not the recorder's; the frame <see cref="Begin"/>
/// ran on is left out. Either way frames.tsv has one row per frame: the frame,
/// game and unscaled time, the time scale (the hit-stop log), the screen and
/// capture sizes (0 in a timing sequence), the virtual pointer (capture
/// pixels, origin top-left, as the PNGs), the GC bytes allocated in the
/// frame and the main thread's time (-1 where the player cannot read them),
/// the real time since the previous frame ended (dtMs; the hitch
/// measurement, which reads timing sequences only), and in a captured
/// sequence the tracked screen rects (the expected-motion mask for the
/// flicker detector) and any extra columns a track adds. events.tsv gets
/// every <see cref="QaSignals.Event"/>, every press the game saw, every
/// sound cue (channel "cue": each cue the sound bank was asked for,
/// Sounds.Fired, and each sound the game plays outside the bank, which
/// raises QaSignals.Event("cue", ...)) and every still taken while a
/// captured sequence records (channel "still", on the frame that holds the
/// same picture), on the frame it happened (the latency and press-to-sound
/// probes' input). A captured frame keeps the screen's colour exactly as a
/// still does: the scale-down reads the screenshot and writes the frame in
/// the same colour space, so the picture is sRGB-encoded once (GATE-05 of
/// 2026-10-28: a second encode made every frame about twice as bright as
/// the game). meta.json says which kind the sequence is (fixedStep, frames),
/// how many sRGB encodes its frames carry (srgbEncodes 1; a sequence without
/// the key is from before the fix and carries 2) and whether motion was off
/// (motionStill: Reduced Motion or intensity 0, when the views cut by
/// design). tools/qa/seqtools.py reads the folder.
/// </summary>
public sealed class SeqRecorder : MonoBehaviour
{
    /// <summary>The capture rate.</summary>
    public const int Fps = 30;

    /// <summary>The rows a timing sequence keeps without allocating (about 4.5 minutes at 60 fps); a longer one grows the buffer.</summary>
    public const int TimingCapacity = 16384;

    /// <summary>The captured frames' width in pixels (the height follows the screen's aspect).</summary>
    public int CaptureWidth = 960;

    /// <summary>True: the next sequence is a fixed-step capture (Time.captureFramerate = 30, PNG frames); false: a timing sequence (real frame times, no capture).</summary>
    public bool FixedStep = true;

    /// <summary>The folder of the sequence recording now, or null.</summary>
    public string Dir { get; private set; }

    /// <summary>The next frame's index in the sequence.</summary>
    public int Index { get; private set; }

    /// <summary>True while a sequence records.</summary>
    public bool Recording => Dir != null;

    /// <summary>True while a timing sequence (real frame times, no capture) records.</summary>
    public bool Timing => Dir != null && !_capturing;

    private readonly Dictionary<string, Func<Rect?>> _tracked = new Dictionary<string, Func<Rect?>>();
    private readonly Dictionary<string, Func<string>> _extra = new Dictionary<string, Func<string>>();
    private readonly List<TimingRow> _timings = new List<TimingRow>();
    private StreamWriter _frames;
    private StreamWriter _events;
    private string _stillPath;
    private bool _capturing;
    private bool _skipBeginFrame;
    private int _oldCaptureFramerate;
    private double _lastFrameEnd = -1;
    private Texture2D _small;
    private byte[] _screenBytes, _frameBytes;
    private ProfilerRecorder _gc;
    private ProfilerRecorder _mainThread;

    /// <summary>One timing-sequence row, kept as values until <see cref="Stop"/> writes it.</summary>
    private struct TimingRow
    {
        public int Index, Frame, ScreenW, ScreenH, Down;
        public float Time, Unscaled, TimeScale, Mx, My, DtMs;
        public long GcBytes;
        public double MainMs;
    }

    /// <summary>
    /// Tracks <paramref name="screenRect"/> (screen pixels, origin
    /// bottom-left; null when not on screen) under <paramref name="name"/>:
    /// written per captured frame, so the flicker detector ignores the area
    /// while it moves (the expected-motion mask).
    /// </summary>
    public void Track(string name, Func<Rect?> screenRect) => _tracked[name] = screenRect;

    /// <summary>Adds a column <paramref name="name"/>=value to every captured frame's row (a track's own measurement).</summary>
    public void Column(string name, Func<string> value) => _extra[name] = value;

    /// <summary>Stops tracking or writing <paramref name="name"/>.</summary>
    public void Forget(string name)
    {
        _tracked.Remove(name);
        _extra.Remove(name);
    }

    /// <summary>Starts a sequence into <paramref name="dir"/> (created), of the kind <see cref="FixedStep"/> says; a running one is stopped first.</summary>
    public void Begin(string dir)
    {
        Stop();
        Directory.CreateDirectory(dir);
        Dir = dir;
        Index = 0;
        _capturing = FixedStep;
        _frames = new StreamWriter(Path.Combine(dir, "frames.tsv"));
        _frames.WriteLine("i\tframe\ttime\tunscaled\ttimeScale\tscreenW\tscreenH\tcapW\tcapH\tmx\tmy\tdown\tgcBytes\tmainMs\tdtMs\ttracked\textra");
        _events = new StreamWriter(Path.Combine(dir, "events.tsv"));
        _events.WriteLine("frame\ti\tunscaled\tchannel\tname");
        // How the sequence was taken: a fixed-step capture with frames, or a timing sequence (the hitch analysis reads only those);
        // its frames' colour (one sRGB encode, as a still's) and whether the game's motions were off (the cuts analysis skips those).
        File.WriteAllText(Path.Combine(dir, "meta.json"),
            $"{{\"fixedStep\": {(_capturing ? "true" : "false")}, \"frames\": {(_capturing ? "true" : "false")}, \"fps\": {Fps}, \"captureWidth\": {CaptureWidth}, \"screen\": \"{Screen.width}x{Screen.height}\", \"srgbEncodes\": 1, \"motionStill\": {(UiMotion.Amount.Still ? "true" : "false")}}}\n");
        if (_capturing)
        {
            _oldCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = Fps;
        }
        else
        {
            _timings.Clear();
            if (_timings.Capacity < TimingCapacity)
                _timings.Capacity = TimingCapacity;
            _skipBeginFrame = true;
        }
    }

    /// <summary>Ends the sequence (if one records): restores the frame rate, or writes a timing sequence's rows.</summary>
    public void Stop()
    {
        if (Dir == null)
            return;
        if (_capturing)
            Time.captureFramerate = _oldCaptureFramerate;
        else
            WriteTimings();
        _frames?.Dispose();
        _events?.Dispose();
        _frames = _events = null;
        Dir = null;
    }

    /// <summary>Saves the next frame at full resolution to <paramref name="path"/> (a still; recording or not).</summary>
    public void Still(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        _stillPath = path;
    }

    /// <summary>True until the still asked for by <see cref="Still"/> is written.</summary>
    public bool StillPending => _stillPath != null;

    /// <summary>Writes an event row now (also called for <see cref="QaSignals.Event"/> and the presses).</summary>
    public void Mark(string channel, string name)
    {
        if (_events == null)
            return;
        _events.WriteLine($"{Time.frameCount}\t{Index}\t{F(Time.unscaledTime)}\t{Clean(channel)}\t{Clean(name)}");
    }

    /// <summary>The events.tsv channel of a still taken while a captured sequence records: its row's frame index holds the same picture (seqtools colour).</summary>
    public const string StillChannel = "still";

    /// <summary>A cue the sound bank was asked for (Sounds.Fired): a "cue" row named by its id, and "(no bank clip)" when the bank had none to play (the caller's own placeholder, if any, plays then).</summary>
    private void OnCue(string id, bool clip) => Mark(QaSignals.Cue, clip ? id : id + " (no bank clip)");

    private void OnEnable()
    {
        QaSignals.Raised += Mark;
        Sounds.Fired += OnCue;
        if (QaInput.Current != null)
            QaInput.Current.Pressed += Mark;
        _gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        StartCoroutine(EndOfFrames());
    }

    private void OnDisable()
    {
        QaSignals.Raised -= Mark;
        Sounds.Fired -= OnCue;
        if (QaInput.Current != null)
            QaInput.Current.Pressed -= Mark;
        Stop();
        _gc.Dispose();
        _mainThread.Dispose();
        if (_small != null)
            Destroy(_small);
    }

    /// <summary>
    /// Scales the screenshot <paramref name="screen"/> down into
    /// <paramref name="frame"/> (RGB24) on the CPU: each frame pixel is the
    /// mean of the screen pixels under it, taken from the screenshot's own
    /// bytes. Nothing decodes or encodes a colour on the way (GATE-05: the old
    /// GPU blit read the sRGB bytes as linear and wrote them through an sRGB
    /// target, encoding them a second time), so a frame carries the screen's
    /// one sRGB encode, as a still does. Rows stay bottom-up, as the textures
    /// keep them (EncodeToPNG writes them upright). The byte buffers are the
    /// recorder's, kept between frames.
    /// </summary>
    private void ScaleDown(Texture2D screen, Texture2D frame)
    {
        if (Layout(screen.format, out int stride, out int r, out int g, out int b))
        {
            NativeArray<byte> raw = screen.GetRawTextureData<byte>();
            Fit(ref _screenBytes, raw.Length);
            NativeArray<byte>.Copy(raw, _screenBytes, raw.Length);
        }
        else
        {
            // Any other format: its pixels as Color32 (an allocation per frame; the screenshot is RGBA32 on the editor and players).
            Color32[] px = screen.GetPixels32();
            Fit(ref _screenBytes, px.Length * 4);
            for (int i = 0; i < px.Length; i++)
            {
                _screenBytes[i * 4] = px[i].r;
                _screenBytes[i * 4 + 1] = px[i].g;
                _screenBytes[i * 4 + 2] = px[i].b;
            }
        }
        Fit(ref _frameBytes, frame.width * frame.height * 3);
        Box(_screenBytes, stride, r, g, b, screen.width, screen.height, _frameBytes, frame.width, frame.height);
        frame.SetPixelData(_frameBytes, 0);
    }

    /// <summary>The byte layout of a screenshot format read raw: bytes per pixel and the red, green and blue offsets; false (and RGBA32's layout, for the Color32 copy) for any other format.</summary>
    private static bool Layout(TextureFormat format, out int stride, out int r, out int g, out int b)
    {
        stride = 4;
        r = 0;
        g = 1;
        b = 2;
        switch (format)
        {
            case TextureFormat.RGBA32: return true;
            case TextureFormat.ARGB32: r = 1; g = 2; b = 3; return true;
            case TextureFormat.BGRA32: r = 2; g = 1; b = 0; return true;
            case TextureFormat.RGB24: stride = 3; return true;
            default: return false;
        }
    }

    private static void Fit(ref byte[] buffer, int length)
    {
        if (buffer == null || buffer.Length != length)
            buffer = new byte[length];
    }

    /// <summary>The box filter behind <see cref="ScaleDown"/>: <paramref name="dst"/> (RGB24, <paramref name="fw"/> x <paramref name="fh"/>) pixel by pixel as the rounded mean of the source pixels its footprint covers (at least one).</summary>
    private static void Box(byte[] src, int stride, int r, int g, int b, int sw, int sh, byte[] dst, int fw, int fh)
    {
        for (int y = 0; y < fh; y++)
        {
            int y0 = (int)((long)y * sh / fh), y1 = Math.Max(y0 + 1, (int)((long)(y + 1) * sh / fh));
            for (int x = 0; x < fw; x++)
            {
                int x0 = (int)((long)x * sw / fw), x1 = Math.Max(x0 + 1, (int)((long)(x + 1) * sw / fw));
                int sr = 0, sg = 0, sb = 0, n = 0;
                for (int yy = y0; yy < y1; yy++)
                {
                    int row = yy * sw;
                    for (int xx = x0; xx < x1; xx++)
                    {
                        int i = (row + xx) * stride;
                        sr += src[i + r];
                        sg += src[i + g];
                        sb += src[i + b];
                        n++;
                    }
                }
                int o = (y * fw + x) * 3;
                dst[o] = (byte)((sr + n / 2) / n);
                dst[o + 1] = (byte)((sg + n / 2) / n);
                dst[o + 2] = (byte)((sb + n / 2) / n);
            }
        }
    }

    private IEnumerator EndOfFrames()
    {
        var eof = new WaitForEndOfFrame();
        while (true)
        {
            yield return eof;
            // A run stopped from outside (QaJobs.Reset, play mode left by hand) resumes here once more while the editor
            // leaves play mode (isPlaying is still true then; the editor's will-change flag is not), when ScreenCapture can no
            // longer be called: the recording ends with the run.
            if (!Application.isPlaying)
                yield break;
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                yield break;
#endif
            // The real time since the previous frame ended (Time's own clocks follow captureFramerate, this one does not).
            double now = Time.realtimeSinceStartupAsDouble;
            float dtMs = _lastFrameEnd < 0 ? Time.unscaledDeltaTime * 1000f : (float)((now - _lastFrameEnd) * 1000.0);
            _lastFrameEnd = now;
            if (_stillPath != null)
            {
                Texture2D full = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(_stillPath, full.EncodeToPNG());
                Destroy(full);
                // A captured sequence's frame of this pass holds the same picture: the row names it (seqtools colour compares them).
                if (Dir != null && _capturing)
                    Mark(StillChannel, Path.GetFileName(_stillPath));
                _stillPath = null;
            }
            if (Dir == null)
                continue;
            if (!_capturing)
            {
                if (_skipBeginFrame)
                    _skipBeginFrame = false;
                else
                    AddTiming(dtMs);
                continue;
            }
            int capW = Mathf.Max(16, CaptureWidth);
            int capH = Mathf.Max(16, Mathf.RoundToInt(capW * Screen.height / (float)Mathf.Max(1, Screen.width)));
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (_small == null || _small.width != capW || _small.height != capH)
            {
                if (_small != null)
                    Destroy(_small);
                _small = new Texture2D(capW, capH, TextureFormat.RGB24, false);
            }
            ScaleDown(tex, _small);
            Destroy(tex);
            File.WriteAllBytes(Path.Combine(Dir, $"f{Index:0000}.png"), _small.EncodeToPNG());
            WriteRow(capW, capH, dtMs);
            Index++;
        }
    }

    /// <summary>A timing sequence's frame: values into the reserved buffer, nothing allocated or written.</summary>
    private void AddTiming(float dtMs)
    {
        QaInput input = QaInput.Current;
        Vector2 m = input != null ? input.Position : Vector2.zero;
        _timings.Add(new TimingRow
        {
            Index = Index,
            Frame = Time.frameCount,
            ScreenW = Screen.width,
            ScreenH = Screen.height,
            Down = input != null && input.Down ? 1 : 0,
            Time = Time.time,
            Unscaled = Time.unscaledTime,
            TimeScale = Time.timeScale,
            Mx = m.x,
            My = Screen.height - m.y,
            DtMs = dtMs,
            GcBytes = _gc.Valid ? _gc.LastValue : -1,
            MainMs = _mainThread.Valid ? _mainThread.LastValue * 1e-6 : -1,
        });
        Index++;
    }

    private void WriteTimings()
    {
        foreach (TimingRow r in _timings)
            _frames.WriteLine(string.Join("\t", r.Index, r.Frame, F(r.Time), F(r.Unscaled), F(r.TimeScale), r.ScreenW, r.ScreenH, 0, 0,
                F(r.Mx), F(r.My), r.Down, r.GcBytes, r.MainMs.ToString("0.###", CultureInfo.InvariantCulture), F(r.DtMs), string.Empty, string.Empty));
        _timings.Clear();
    }

    private void WriteRow(int capW, int capH, float dtMs)
    {
        float sx = capW / (float)Mathf.Max(1, Screen.width), sy = capH / (float)Mathf.Max(1, Screen.height);
        QaInput input = QaInput.Current;
        Vector2 m = input != null ? input.Position : Vector2.zero;
        var tracked = new StringBuilder();
        foreach (KeyValuePair<string, Func<Rect?>> t in _tracked)
        {
            Rect? r = SafeRect(t.Value);
            if (r == null)
                continue;
            Rect s = r.Value;
            // Screen rects are bottom-up; the PNGs are top-down.
            tracked.Append(Clean(t.Key).Replace(':', '-')).Append(':').Append(F(s.xMin * sx)).Append(',').Append(F((Screen.height - s.yMax) * sy))
                   .Append(',').Append(F(s.xMax * sx)).Append(',').Append(F((Screen.height - s.yMin) * sy)).Append(';');
        }
        var extra = new StringBuilder();
        foreach (KeyValuePair<string, Func<string>> c in _extra)
        {
            string v;
            try { v = c.Value(); } catch (Exception e) { v = "error " + e.GetType().Name; }
            extra.Append(Clean(c.Key)).Append('=').Append(Clean(v)).Append(';');
        }
        long gc = _gc.Valid ? _gc.LastValue : -1;
        double mainMs = _mainThread.Valid ? _mainThread.LastValue * 1e-6 : -1;
        _frames.WriteLine(string.Join("\t", Index, Time.frameCount, F(Time.time), F(Time.unscaledTime), F(Time.timeScale), Screen.width, Screen.height, capW, capH,
            F(m.x * sx), F((Screen.height - m.y) * sy), input != null && input.Down ? 1 : 0, gc, mainMs.ToString("0.###", CultureInfo.InvariantCulture),
            F(dtMs), tracked.ToString(), extra.ToString()));
    }

    private static Rect? SafeRect(Func<Rect?> f)
    {
        try { return f(); }
        catch (Exception) { return null; }
    }

    private static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Clean(string s) => (s ?? string.Empty).Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ').Replace(';', ',').Replace('=', '-');
}
#endif
