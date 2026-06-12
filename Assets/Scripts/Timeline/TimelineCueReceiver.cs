using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for ANY system that reacts to timeline effects:
/// background visuals, music, UI skins, chatter, props, tech tree panels...
/// Subclass, pick a channel in the inspector, implement OnCuesChanged.
/// Multiple receivers and multiple cues coexist freely (effects stack).
///
/// Example: a HomeOfficeDecorator on channel Visuals receives
/// ["robot_office", "steam_props"] and enables matching scene objects.
/// </summary>
public abstract class TimelineCueReceiver : MonoBehaviour
{
    /// <summary>Channel this receiver listens to.</summary>
    [SerializeField] private EffectChannel channel = EffectChannel.Visuals;

    /// <summary>Channel this receiver listens to (read-only).</summary>
    public EffectChannel Channel => channel;

    /// <summary>
    /// Refreshes cues on scene start (i.e., each day / scene load).
    /// </summary>
    protected virtual void Start()
    {
        Refresh();
    }

    /// <summary>
    /// Re-queries active cues for this channel and notifies the subclass.
    /// Call manually if effects change mid-day (e.g., debug tools).
    /// </summary>
    public void Refresh()
    {
        if (!RunManager.HasInstance)
        {
            OnCuesChanged(new List<string>());
            return;
        }

        RunManager run = RunManager.Instance;
        List<string> cues = TimelineEffects.GetCues(run.World, run.Library, channel);
        OnCuesChanged(cues);
    }

    /// <summary>
    /// Receives the full list of active cue ids on this channel (possibly empty).
    /// Implementations decide which cues they understand and how to react.
    /// </summary>
    protected abstract void OnCuesChanged(IReadOnlyList<string> cues);
}
