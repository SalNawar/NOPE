#if UNITY_EDITOR || TIMEDESK_QA
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// The QA rig's camera logger (CAM's "camera-transform logger"; FIX_PLAN_v2
/// P0-B delta; GAME_FEEL_BIBLE G9 and gap 3). At the end of every frame it
/// measures how far the watched camera's picture moved
/// (<see cref="QaCameraMoves.ShiftPx"/>), keeps the cause windows game code
/// opens with <see cref="QaSignals.TagCamera"/>, measures each capped
/// (impulse) window's largest offset from the pose at its tag, and writes a
/// row per frame to camera.tsv. <see cref="Report"/> applies the rule: every
/// move has a cause, every impulse stays under its cap. The watched camera is
/// <see cref="Camera.main"/> unless <see cref="Watch"/> names one; a switch to
/// another camera is a cut, not a move. While <see cref="QuietWhen"/> is true
/// (the runner's GC windows and real-time sequences) a frame allocates nothing
/// and writes nothing: its row waits in a buffer reserved up front and is
/// written on the first frame after, so the rig's own logging is never
/// counted as the game's GC or frame time.
/// </summary>
public sealed class CameraMoveLog : MonoBehaviour
{
    /// <summary>The rows a quiet stretch keeps without allocating (about 4.5 minutes at 60 fps); a longer one grows the buffer.</summary>
    public const int QuietCapacity = 16384;

    /// <summary>The camera to watch; null watches <see cref="Camera.main"/>.</summary>
    public Camera Watch;

    /// <summary>True while the logger must not allocate or write (null: never); asked every frame.</summary>
    public Func<bool> QuietWhen;

    private readonly List<QaCameraFrame> _frames = new List<QaCameraFrame>();
    private readonly List<QaCameraTag> _tags = new List<QaCameraTag>();
    private readonly List<QaCameraPose> _tagPoses = new List<QaCameraPose>();
    private readonly List<Row> _pending = new List<Row>();
    private readonly Dictionary<int, string> _cameraNames = new Dictionary<int, string>();
    private readonly StringBuilder _line = new StringBuilder(256);
    private QaCameraPose? _last;
    private Camera _lastCamera;
    private int _lastCameraId;
    private bool _wasQuiet;
    private StreamWriter _tsv;

    /// <summary>One camera.tsv row, kept as values until it is written.</summary>
    private struct Row
    {
        public int Frame;
        public float Unscaled;
        public int CameraId;
        public float StepPx;
        public Vector3 Position;
        public Vector3 Euler;
        public float Fov;
    }

    /// <summary>The frames logged so far.</summary>
    public IReadOnlyList<QaCameraFrame> Frames => _frames;

    /// <summary>The cause windows opened so far.</summary>
    public IReadOnlyList<QaCameraTag> Tags => _tags;

    /// <summary>Starts writing camera.tsv into <paramref name="dir"/> (the frames before it are kept in memory only).</summary>
    public void WriteTo(string dir)
    {
        _tsv?.Dispose();
        _pending.Clear();
        Directory.CreateDirectory(dir);
        _tsv = new StreamWriter(Path.Combine(dir, "camera.tsv"));
        _tsv.WriteLine("frame\tunscaled\tcamera\tstepPx\tcauses\tposX\tposY\tposZ\trotX\trotY\trotZ\tfov");
    }

    /// <summary>Writes every buffered row to camera.tsv now (the runner calls it before its report; a quiet stretch's rows otherwise wait for its end).</summary>
    public void Flush()
    {
        if (_tsv == null)
        {
            _pending.Clear();
            return;
        }
        foreach (Row row in _pending)
            WriteRow(row);
        _pending.Clear();
        _tsv.Flush();
    }

    /// <summary>The rule applied to everything logged so far.</summary>
    public QaCameraReport Report() => QaCameraMoves.Classify(_frames, _tags);

    private void OnEnable()
    {
        QaSignals.CameraTagged += OnTag;
        StartCoroutine(EndOfFrames());
    }

    private void OnDisable()
    {
        QaSignals.CameraTagged -= OnTag;
        Flush();
        _tsv?.Dispose();
        _tsv = null;
    }

    private Camera Watched()
    {
        if (Watch != null && Watch.isActiveAndEnabled)
            return Watch;
        return Camera.main;
    }

    private static QaCameraPose Pose(Camera cam) =>
        new QaCameraPose(cam.transform.position, cam.transform.rotation, cam.fieldOfView, cam.aspect, cam.orthographic, cam.orthographicSize);

    private void OnTag(CameraMoveCause cause, float seconds, float capPx)
    {
        Camera cam = Watched();
        float now = Time.unscaledTime;
        _tags.Add(new QaCameraTag { Cause = cause, Start = now, End = now + seconds, CapPx = capPx });
        _tagPoses.Add(cam != null ? Pose(cam) : _last ?? default);
    }

    private IEnumerator EndOfFrames()
    {
        var eof = new WaitForEndOfFrame();
        while (true)
        {
            yield return eof;
            bool quiet = QuietWhen != null && QuietWhen();
            if (quiet && !_wasQuiet)
            {
                // Reserved before the first quiet frame is measured, so quiet frames add without growing a list.
                if (_pending.Capacity < QuietCapacity)
                    _pending.Capacity = QuietCapacity;
                if (_frames.Capacity < _frames.Count + QuietCapacity)
                    _frames.Capacity = _frames.Count + QuietCapacity;
            }
            _wasQuiet = quiet;
            Camera cam = Watched();
            if (cam == null)
            {
                _last = null;
                continue;
            }
            QaCameraPose pose = Pose(cam);
            float now = Time.unscaledTime;
            // A different camera (a scene load, the PC frame's camera) is a cut between cameras, not a move of one.
            float step = _last.HasValue && _lastCamera == cam ? QaCameraMoves.ShiftPx(_last.Value, pose) : 0f;
            if (_lastCamera != cam)
            {
                // A camera switch is a scene event, never a steady frame: the name is read once, here.
                _lastCameraId = cam.GetInstanceID();
                if (!_cameraNames.ContainsKey(_lastCameraId))
                    _cameraNames[_lastCameraId] = cam.name;
            }
            _last = pose;
            _lastCamera = cam;
            _frames.Add(new QaCameraFrame(Time.frameCount, now, step));
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].CapPx > 0f && QaCameraMoves.Covers(_tags[i], now))
                    _tags[i].MaxOffsetPx = Mathf.Max(_tags[i].MaxOffsetPx, QaCameraMoves.ShiftPx(_tagPoses[i], pose));
            if (_tsv == null)
                continue;
            Transform t = cam.transform;
            _pending.Add(new Row
            {
                Frame = Time.frameCount,
                Unscaled = now,
                CameraId = _lastCameraId,
                StepPx = step,
                Position = t.position,
                Euler = t.eulerAngles,
                Fov = cam.fieldOfView,
            });
            if (!quiet)
                Flush();
        }
    }

    private void WriteRow(Row row)
    {
        _line.Clear();
        _line.Append(row.Frame).Append('\t').Append(F(row.Unscaled)).Append('\t')
             .Append(_cameraNames.TryGetValue(row.CameraId, out string name) ? name : "?").Append('\t').Append(F(row.StepPx)).Append('\t');
        // The causes are the tag windows covering the row's time (tags are only ever added, so a buffered row reads them late).
        int written = 0;
        for (int i = 0; i < _tags.Count; i++)
        {
            if (!QaCameraMoves.Covers(_tags[i], row.Unscaled))
                continue;
            bool seen = false;
            for (int j = 0; j < i && !seen; j++)
                seen = _tags[j].Cause == _tags[i].Cause && QaCameraMoves.Covers(_tags[j], row.Unscaled);
            if (seen)
                continue;
            if (written++ > 0)
                _line.Append('+');
            _line.Append(_tags[i].Cause.ToString());
        }
        _line.Append('\t').Append(F(row.Position.x)).Append('\t').Append(F(row.Position.y)).Append('\t').Append(F(row.Position.z))
             .Append('\t').Append(F(row.Euler.x)).Append('\t').Append(F(row.Euler.y)).Append('\t').Append(F(row.Euler.z)).Append('\t').Append(F(row.Fov));
        _tsv.WriteLine(_line.ToString());
    }

    private static string F(float v) => v.ToString("0.####", CultureInfo.InvariantCulture);
}
#endif
