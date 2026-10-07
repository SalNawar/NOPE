using System;
using UnityEngine;

/// <summary>The game feel's sound cues, one per kind of interaction (UiSoundSO gives each its clip). Serialized on UiSoundSO: append only.</summary>
public enum UiSoundCue
{
    /// <summary>The pointer comes over a live control.</summary>
    Hover,

    /// <summary>A control pressed down.</summary>
    Press,

    /// <summary>A control let go.</summary>
    Release,

    /// <summary>A toggle or a segmented pair switched.</summary>
    Toggle,

    /// <summary>A tab chosen or a pull tab slid (the stamps' bar).</summary>
    Tab,

    /// <summary>A stamp slammed onto a paper.</summary>
    StampSlam,

    /// <summary>A paper picked up off the desk.</summary>
    PaperPickup,

    /// <summary>A paper dropped onto the desk.</summary>
    PaperDrop,

    /// <summary>A PC window opened.</summary>
    WindowOpen,

    /// <summary>A PC window closed.</summary>
    WindowClose,

    /// <summary>A refused action (a disabled control clicked, a stamp refused).</summary>
    Error
}

/// <summary>
/// The game feel's sounds (Assets/Data/Config/UiSounds_Default.asset, made by
/// the builders when missing and assigned to RunConfig): a clip and a volume
/// per UiSoundCue. A cue with no clip plays nothing (the game has almost no
/// sound effects yet); an Inspector knob, never written by Generate World.
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/UI/UI Sounds", fileName = "UiSounds_Default", order = 23)]
public sealed class UiSoundSO : ScriptableObject
{
    /// <summary>One cue's sound.</summary>
    [Serializable]
    public sealed class Entry
    {
        /// <summary>The interaction.</summary>
        public UiSoundCue cue;

        /// <summary>Its clip (none: silent).</summary>
        public AudioClip clip;

        /// <summary>Its volume, 0 to 1 (mixed low under the hall's ambience).</summary>
        [Range(0f, 1f)] public float volume = 0.5f;
    }

    /// <summary>The cues' sounds (a cue not listed is silent).</summary>
    public Entry[] entries = new Entry[0];

    /// <summary>All the cues' volume, 0 to 1.</summary>
    [Range(0f, 1f)] public float masterVolume = 0.6f;

    /// <summary>The entry of <paramref name="cue"/> with a clip, or null.</summary>
    public Entry Find(UiSoundCue cue)
    {
        if (entries == null)
            return null;
        foreach (Entry entry in entries)
            if (entry != null && entry.cue == cue && entry.clip != null)
                return entry;
        return null;
    }
}

/// <summary>
/// The game feel's audio router: Play(cue) plays the cue's clip from
/// RunConfig's UiSoundSO through one persistent 2D AudioSource (made on the
/// first cue that has a clip), and plays nothing when the cue has none.
/// </summary>
public static class UiSounds
{
    private static UiSoundSO _sounds;
    private static bool _looked;
    private static AudioSource _source;

    /// <summary>Plays <paramref name="cue"/>'s clip (outside play mode, or without a clip, nothing); true when a clip played (a caller with its own placeholder plays that instead).</summary>
    public static bool Play(UiSoundCue cue)
    {
        if (!Application.isPlaying)
            return false;
        if (!_looked)
        {
            _looked = true;
            var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
            _sounds = config != null ? config.uiSounds : null;
        }
        UiSoundSO.Entry entry = _sounds != null ? _sounds.Find(cue) : null;
        if (entry == null)
            return false;
        if (_source == null)
        {
            var host = new GameObject("UiSounds") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(host);
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }
        _source.PlayOneShot(entry.clip, entry.volume * _sounds.masterVolume);
        return true;
    }

    /// <summary>Forgets the sounds when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _sounds = null;
        _looked = false;
        _source = null;
    }
}
