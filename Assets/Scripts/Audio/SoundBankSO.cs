using System;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// The game's sound bank (Assets/Data/Config/SoundBank_Default.asset), filled
/// by Tools > Audio > Import Sound List from Saleh's list
/// (ArtDeliverables/TimeDesk/Audio/SOUND_LIST.md) and the files dropped next
/// to it: one cue per row with its clips (the variants, which rotate), its
/// mixer group, whether it loops, its priority and whether its clip is a
/// generated placeholder. The import rewrites the cues; the volumes, the
/// mixer's groups and the ambience's hours are Inspector knobs it keeps.
/// Played by Sounds (a cue without a clip plays nothing).
/// </summary>
[CreateAssetMenu(menuName = "TimeDesk/Audio/Sound Bank", fileName = "SoundBank_Default", order = 30)]
public sealed class SoundBankSO : ScriptableObject
{
    /// <summary>One cue of the list.</summary>
    [Serializable]
    public sealed class Cue
    {
        /// <summary>The cue's id (its file's name in the list: "paper_drop").</summary>
        public string id;

        /// <summary>Its mixer group.</summary>
        public SoundBus bus;

        /// <summary>True for a loop (an ambience bed, the music, the scanner's sweep).</summary>
        public bool loop;

        /// <summary>Its priority in the list (1 to 3).</summary>
        [Range(1, 3)] public int priority = 1;

        /// <summary>Its clips, one per variant (rotated, each nudged in pitch); empty: silent.</summary>
        public AudioClip[] clips = new AudioClip[0];

        /// <summary>True while its clip is a generated placeholder (SoundSynth), not Saleh's file.</summary>
        public bool placeholder;

        /// <summary>Its volume, 0 to 1 (kept across imports).</summary>
        [Range(0f, 1f)] public float volume = 1f;
    }

    /// <summary>The cues, in the list's order (the import rewrites them, keeping each one's volume).</summary>
    public Cue[] cues = new Cue[0];

    /// <summary>The mixer and its four groups (UI, Desk, Ambience, Music); null groups play straight to the listener.</summary>
    public AudioMixer mixer;

    /// <summary>The mixer group of each bus.</summary>
    public AudioMixerGroup uiGroup, deskGroup, ambienceGroup, musicGroup;

    /// <summary>The volume of every placeholder (mixed low under the real sounds to come).</summary>
    [Range(0f, 1f)] public float placeholderVolume = 0.5f;

    /// <summary>How far each play's pitch is nudged either way (0.04: ±4 %), so repeated variants never sound copied.</summary>
    [Range(0f, 0.2f)] public float pitchJitter = 0.04f;

    /// <summary>The hall's ambience under the PA chime (a share of its level), how long it stays down after the chime's clip, and how fast it moves (seconds to get most of the way).</summary>
    [Range(0f, 1f)] public float duckLevel = 0.35f;

    /// <summary>The seconds the duck holds past the chime's clip, and its fade's seconds.</summary>
    [Min(0f)] public float duckHold = 0.5f, duckFade = 0.25f;

    /// <summary>The ambience beds' fade when the hall's state changes (seconds).</summary>
    [Min(0.01f)] public float bedFade = 1.5f;

    /// <summary>The hall's night bed plays from this minute of the day (20:00) until dayFromMinute (06:00), the day bed the rest; they cross-fade over bedFadeMinutes of the clock.</summary>
    public int nightFromMinute = 20 * 60, dayFromMinute = 6 * 60, bedFadeMinutes = 30;

    /// <summary>The busy hours the rush bed layers over the day, in pairs of minutes of the day (08:00-10:00, 17:00-19:00).</summary>
    public int[] rushWindows = { 8 * 60, 10 * 60, 17 * 60, 19 * 60 };

    /// <summary>How many minutes before closing time the last-hour alarm rings (FeelDirector, ShiftBells: 60, the last hour).</summary>
    [Min(0)] public int lastHourMinutes = 60;

    /// <summary>The cue called <paramref name="id"/>, or null.</summary>
    public Cue Find(string id)
    {
        if (cues == null || string.IsNullOrEmpty(id))
            return null;
        foreach (Cue cue in cues)
            if (cue != null && cue.id == id)
                return cue;
        return null;
    }

    /// <summary>The mixer group of <paramref name="bus"/> (null without a mixer).</summary>
    public AudioMixerGroup Group(SoundBus bus)
    {
        switch (bus)
        {
            case SoundBus.Ui:
                return uiGroup;
            case SoundBus.Ambience:
                return ambienceGroup;
            case SoundBus.Music:
                return musicGroup;
            default:
                return deskGroup;
        }
    }
}
