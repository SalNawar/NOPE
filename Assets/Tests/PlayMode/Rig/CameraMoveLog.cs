#if UNITY_EDITOR || TIMEDESK_QA
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
/// another camera is a cut, not a move.
/// </summary>
public sealed class CameraMoveLog : MonoBehaviour
{
    /// <summary>The camera to watch; null watches <see cref="Camera.main"/>.</summary>
    public Camera Watch;

    private readonly List<QaCameraFrame> _frames = new List<QaCameraFrame>();
    private readonly List<QaCameraTag> _tags = new List<QaCameraTag>();
    private readonly List<QaCameraPose> _tagPoses = new List<QaCameraPose>();
    private QaCameraPose? _last;
    private Camera _lastCamera;
    private StreamWriter _tsv;

    /// <summary>The frames logged so far.</summary>
    public IReadOnlyList<QaCameraFrame> Frames => _frames;

    /// <summary>The cause windows opened so far.</summary>
    public IReadOnlyList<QaCameraTag> Tags => _tags;

    /// <summary>Starts writing camera.tsv into <paramref name="dir"/> (the frames before it are kept in memory only).</summary>
    public void WriteTo(string dir)
    {
        _tsv?.Dispose();
        Directory.CreateDirectory(dir);
        _tsv = new StreamWriter(Path.Combine(dir, "camera.tsv"));
        _tsv.WriteLine("frame\tunscaled\tcamera\tstepPx\tcauses\tposX\tposY\tposZ\trotX\trotY\trotZ\tfov");
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
            _last = pose;
            _lastCamera = cam;
            _frames.Add(new QaCameraFrame(Time.frameCount, now, step));
            for (int i = 0; i < _tags.Count; i++)
                if (_tags[i].CapPx > 0f && QaCameraMoves.Covers(_tags[i], now))
                    _tags[i].MaxOffsetPx = Mathf.Max(_tags[i].MaxOffsetPx, QaCameraMoves.ShiftPx(_tagPoses[i], pose));
            if (_tsv == null)
                continue;
            string causes = string.Join("+", _tags.Where(t => QaCameraMoves.Covers(t, now)).Select(t => t.Cause.ToString()).Distinct());
            Vector3 p = cam.transform.position, r = cam.transform.eulerAngles;
            var sb = new StringBuilder();
            sb.Append(Time.frameCount).Append('\t').Append(F(now)).Append('\t').Append(cam.name).Append('\t').Append(F(step)).Append('\t').Append(causes)
              .Append('\t').Append(F(p.x)).Append('\t').Append(F(p.y)).Append('\t').Append(F(p.z))
              .Append('\t').Append(F(r.x)).Append('\t').Append(F(r.y)).Append('\t').Append(F(r.z)).Append('\t').Append(F(cam.fieldOfView));
            _tsv.WriteLine(sb.ToString());
        }
    }

    private static string F(float v) => v.ToString("0.####", CultureInfo.InvariantCulture);
}
#endif
