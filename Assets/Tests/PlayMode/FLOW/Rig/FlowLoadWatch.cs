#if UNITY_EDITOR || TIMEDESK_QA
using UnityEngine;

/// <summary>
/// FLOW's real-time load watch (BUG-31a [BP]; flow_load, FlowOps'
/// flowLoadClock and flowLoadTime). A captured sequence cannot see a hitch:
/// it runs at Time.captureFramerate = 30 and the shutter's light steps on
/// UiMotion.Delta, so a multi-second activation frame (a scene's Awake and
/// Start, GameManager.Start making the day's cases) records as one ordinary
/// moving frame, while the player sees that frame held. This watch measures
/// the real time instead: it is a hidden object kept across scene loads that
/// times every frame on Time.realtimeSinceStartupAsDouble (which
/// captureFramerate does not touch), from SceneTransit's "start" mark (the
/// click's frame) until its "up" mark (the shutter off), and stamps the
/// "rise" mark (the new scene first shown as the shutter starts up). The
/// marks are TransitRunner's QaSignals events on the "transit" channel.
/// Run it with no sequence recording: a capture's own frame cost would be
/// counted.
/// </summary>
public sealed class FlowLoadWatch : MonoBehaviour
{
    /// <summary>Real seconds (realtimeSinceStartup) at the load's start mark; negative until it is seen.</summary>
    public double Started { get; private set; } = -1;

    /// <summary>Real seconds at the rise mark (the new scene first shown); negative until it is seen.</summary>
    public double Rising { get; private set; } = -1;

    /// <summary>Real seconds at the up mark (the shutter off); negative until it is seen.</summary>
    public double Up { get; private set; } = -1;

    /// <summary>The scene the start mark named.</summary>
    public string Scene { get; private set; } = string.Empty;

    /// <summary>The longest real frame between the start and up marks, in milliseconds.</summary>
    public double WorstFrameMs { get; private set; }

    /// <summary>Frames timed between the start and up marks.</summary>
    public int Frames { get; private set; }

    /// <summary>Frames over <see cref="LongFrameMs"/> between the marks.</summary>
    public int LongFrames { get; private set; }

    /// <summary>The frame length counted in <see cref="LongFrames"/> (a frame a player reads as held: 250 ms, the [BP] line).</summary>
    public double LongFrameMs = 250.0;

    /// <summary>True once the load has run to its up mark.</summary>
    public bool Done => Up >= 0;

    private double _lastFrame = -1;

    /// <summary>Starts a watch for the next scene change (a hidden object kept across loads).</summary>
    public static FlowLoadWatch Begin()
    {
        var host = new GameObject("FlowLoadWatch") { hideFlags = HideFlags.HideInHierarchy };
        DontDestroyOnLoad(host);
        return host.AddComponent<FlowLoadWatch>();
    }

    /// <summary>Ends the watch.</summary>
    public void End() => Destroy(gameObject);

    private void OnEnable()
    {
        QaSignals.Raised += OnSignal;
        _lastFrame = Time.realtimeSinceStartupAsDouble;
    }

    private void OnDisable() => QaSignals.Raised -= OnSignal;

    private void OnSignal(string channel, string name)
    {
        if (channel != TransitRunner.TransitSignal || string.IsNullOrEmpty(name))
            return;
        double now = Time.realtimeSinceStartupAsDouble;
        if (Started < 0 && name.StartsWith("start:"))
        {
            Started = now;
            Scene = name.Substring("start:".Length);
        }
        else if (Started >= 0 && Rising < 0 && name.StartsWith("rise:"))
            Rising = now;
        else if (Started >= 0 && Up < 0 && name.StartsWith("up:"))
            Up = now;
    }

    // Each frame's real length, between this component's Updates on consecutive frames: a frame that overlaps the
    // load (it ends after the start mark and begins before the up mark) is counted.
    private void Update()
    {
        double now = Time.realtimeSinceStartupAsDouble;
        if (Started >= 0 && now > Started && (Up < 0 || _lastFrame < Up))
        {
            double ms = (now - _lastFrame) * 1000.0;
            Frames++;
            if (ms > WorstFrameMs)
                WorstFrameMs = ms;
            if (ms > LongFrameMs)
                LongFrames++;
        }
        _lastFrame = now;
    }
}
#endif
