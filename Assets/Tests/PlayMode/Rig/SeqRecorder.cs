#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
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
/// every <see cref="QaSignals.Event"/> and every press the game saw, on the
/// frame it happened (the latency probe's input). meta.json says which kind
/// the sequence is (fixedStep, frames). tools/qa/seqtools.py reads the folder.
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
    private RenderTexture _rt;
    private Texture2D _small;
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
        // How the sequence was taken: a fixed-step capture with frames, or a timing sequence (the hitch analysis reads only those).
        File.WriteAllText(Path.Combine(dir, "meta.json"),
            $"{{\"fixedStep\": {(_capturing ? "true" : "false")}, \"frames\": {(_capturing ? "true" : "false")}, \"fps\": {Fps}, \"captureWidth\": {CaptureWidth}, \"screen\": \"{Screen.width}x{Screen.height}\"}}\n");
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

    private void OnEnable()
    {
        QaSignals.Raised += Mark;
        if (QaInput.Current != null)
            QaInput.Current.Pressed += Mark;
        _gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        StartCoroutine(EndOfFrames());
    }

    private void OnDisable()
    {
        QaSignals.Raised -= Mark;
        if (QaInput.Current != null)
            QaInput.Current.Pressed -= Mark;
        Stop();
        _gc.Dispose();
        _mainThread.Dispose();
        if (_rt != null)
            _rt.Release();
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
            if (_rt == null || _rt.width != capW || _rt.height != capH)
            {
                if (_rt != null)
                    _rt.Release();
                _rt = new RenderTexture(capW, capH, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                _small = new Texture2D(capW, capH, TextureFormat.RGB24, false);
            }
            Graphics.Blit(tex, _rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _rt;
            _small.ReadPixels(new Rect(0, 0, capW, capH), 0, 0);
            _small.Apply();
            RenderTexture.active = previous;
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
