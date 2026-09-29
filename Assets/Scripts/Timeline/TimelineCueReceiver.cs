using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for ANY system that reacts to timeline effects:
/// background visuals, music, UI skins, chatter, props, tech tree panels...
/// Subclass, pick a channel in the inspector, implement OnCuesChanged.
/// Multiple receivers and multiple cues coexist freely (effects stack).
/// A receiver refreshes at its scene's start and, while enabled, at the end
/// of any frame in which the run's effects changed (RunManager.EffectsChanged).
///
/// Example: a HomeOfficeDecorator on channel Visuals receives
/// ["robot_office", "steam_props"] and enables matching scene objects.
/// </summary>
public abstract class TimelineCueReceiver : MonoBehaviour
{
    /// <summary>Channel this receiver listens to.</summary>
    [SerializeField] private EffectChannel channel = EffectChannel.Visuals;

    /// <summary>
    /// The channel Refresh reads: the inspector's by default; a subclass
    /// created at runtime (the culture theme service, on UI) overrides it.
    /// </summary>
    protected virtual EffectChannel ListenChannel => channel;

    /// <summary>Set when the run's effects changed since the last refresh (RunManager.EffectsChanged); the next LateUpdate refreshes once.</summary>
    private bool _cuesStale;

    /// <summary>
    /// Refreshes cues on scene start (i.e., each day / scene load).
    /// </summary>
    protected virtual void Start()
    {
        Refresh();
    }

    /// <summary>Listens for the run's effect changes while enabled (audit R3-040: receivers used to refresh only at Start).</summary>
    protected virtual void OnEnable() => RunManager.EffectsChanged += MarkCuesStale;

    /// <summary>Stops listening.</summary>
    protected virtual void OnDisable() => RunManager.EffectsChanged -= MarkCuesStale;

    /// <summary>Refreshes once in the frame after the run's effects changed, however many changed (a night activates several at once).</summary>
    protected virtual void LateUpdate()
    {
        if (!_cuesStale)
            return;

        _cuesStale = false;
        Refresh();
    }

    /// <summary>The run's effects changed: refresh at the end of the frame.</summary>
    private void MarkCuesStale() => _cuesStale = true;

    /// <summary>
    /// Re-queries active cues for this channel and notifies the subclass.
    /// Runs at Start and after the run's effects change; callable directly too.
    /// </summary>
    public void Refresh()
    {
        if (!RunManager.HasInstance)
        {
            OnCuesChanged(new List<string>());
            return;
        }

        RunManager run = RunManager.Instance;
        List<string> cues = TimelineEffects.GetCues(run.World, run.Library, ListenChannel);
        OnCuesChanged(cues);
    }

    /// <summary>
    /// Receives the full list of active cue ids on this channel (possibly empty).
    /// Implementations decide which cues they understand and how to react.
    /// </summary>
    protected abstract void OnCuesChanged(IReadOnlyList<string> cues);
}
