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
/// DOC audit's _DocAuditRec). While a sequence records, every frame is
/// captured at the end of the frame, scaled to <see cref="CaptureWidth"/>
/// wide, and saved as f0000.png, f0001.png ... with one row per frame in
/// frames.tsv: the frame, game and unscaled time, the time scale (the
/// hit-stop log), the screen and capture sizes, the virtual pointer (capture
/// pixels, origin top-left, as the PNGs), the GC bytes allocated in the frame
/// and the main thread's time (-1 where the player cannot read them), the
/// tracked screen rects (the expected-motion mask for the flicker detector)
/// and any extra columns a track adds. events.tsv gets every
/// <see cref="QaSignals.Event"/> and every press the game saw, on the frame
/// it happened (the latency probe's input). With <see cref="FixedStep"/> on
/// (the default) Time.captureFramerate is 30, so the game advances exactly
/// 1/30 s per captured frame however slow the capture is; turn it off to
/// measure real frame times (meta.json says which). tools/qa/seqtools.py
/// reads the folder.
/// </summary>
public sealed class SeqRecorder : MonoBehaviour
{
    /// <summary>The capture rate.</summary>
    public const int Fps = 30;

    /// <summary>The captured frames' width in pixels (the height follows the screen's aspect).</summary>
    public int CaptureWidth = 960;

    /// <summary>True: Time.captureFramerate = 30 while recording (a fixed game step per captured frame).</summary>
    public bool FixedStep = true;

    /// <summary>The folder of the sequence recording now, or null.</summary>
    public string Dir { get; private set; }

    /// <summary>The next frame's index in the sequence.</summary>
    public int Index { get; private set; }

    /// <summary>True while a sequence records.</summary>
    public bool Recording => Dir != null;

    private readonly Dictionary<string, Func<Rect?>> _tracked = new Dictionary<string, Func<Rect?>>();
    private readonly Dictionary<string, Func<string>> _extra = new Dictionary<string, Func<string>>();
    private StreamWriter _frames;
    private StreamWriter _events;
    private string _stillPath;
    private int _oldCaptureFramerate;
    private RenderTexture _rt;
    private Texture2D _small;
    private ProfilerRecorder _gc;
    private ProfilerRecorder _mainThread;

    /// <summary>
    /// Tracks <paramref name="screenRect"/> (screen pixels, origin
    /// bottom-left; null when not on screen) under <paramref name="name"/>:
    /// written per frame, so the flicker detector ignores the area while it
    /// moves (the expected-motion mask).
    /// </summary>
    public void Track(string name, Func<Rect?> screenRect) => _tracked[name] = screenRect;

    /// <summary>Adds a column <paramref name="name"/>=value to every frame row (a track's own measurement).</summary>
    public void Column(string name, Func<string> value) => _extra[name] = value;

    /// <summary>Stops tracking or writing <paramref name="name"/>.</summary>
    public void Forget(string name)
    {
        _tracked.Remove(name);
        _extra.Remove(name);
    }

    /// <summary>Starts a sequence into <paramref name="dir"/> (created); a running one is stopped first.</summary>
    public void Begin(string dir)
    {
        Stop();
        Directory.CreateDirectory(dir);
        Dir = dir;
        Index = 0;
        _frames = new StreamWriter(Path.Combine(dir, "frames.tsv"));
        _frames.WriteLine("i\tframe\ttime\tunscaled\ttimeScale\tscreenW\tscreenH\tcapW\tcapH\tmx\tmy\tdown\tgcBytes\tmainMs\tdtMs\ttracked\textra");
        _events = new StreamWriter(Path.Combine(dir, "events.tsv"));
        _events.WriteLine("frame\ti\tunscaled\tchannel\tname");
        // How the sequence was taken: a fixed-step capture, or real frame times (the hitch analysis reads only those).
        File.WriteAllText(Path.Combine(dir, "meta.json"),
            $"{{\"fixedStep\": {(FixedStep ? "true" : "false")}, \"fps\": {Fps}, \"captureWidth\": {CaptureWidth}, \"screen\": \"{Screen.width}x{Screen.height}\"}}\n");
        if (FixedStep)
        {
            _oldCaptureFramerate = Time.captureFramerate;
            Time.captureFramerate = Fps;
        }
    }

    /// <summary>Ends the sequence (if one records) and restores the frame rate.</summary>
    public void Stop()
    {
        if (Dir == null)
            return;
        if (FixedStep)
            Time.captureFramerate = _oldCaptureFramerate;
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
            if (_stillPath != null)
            {
                Texture2D full = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(_stillPath, full.EncodeToPNG());
                Destroy(full);
                _stillPath = null;
            }
            if (Dir == null)
                continue;
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
            WriteRow(capW, capH);
            Index++;
        }
    }

    private void WriteRow(int capW, int capH)
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
            F(Time.unscaledDeltaTime * 1000f), tracked.ToString(), extra.ToString()));
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
