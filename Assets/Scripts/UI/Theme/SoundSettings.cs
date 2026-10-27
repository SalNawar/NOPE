using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// The Settings volumes (BUG-62, CODE-104): Master, Music and Effects (the
/// UI, desk and ambience buses), each a share from 0 to 1 kept per player in
/// PlayerPrefs (outside the run save, the MotionPreference pattern) and set
/// on the mixer's exposed volumes at once (TimeDesk.mixer: MasterVolume,
/// MusicVolume, UiVolume, DeskVolume, AmbienceVolume; SettingsRules.Decibels).
/// Without the mixer (no sound bank) the master share sets the listener's volume.
/// </summary>
public static class SoundSettings
{
    /// <summary>The PlayerPrefs key of a channel's share.</summary>
    private static string Key(SoundChannel channel) => "TimeDesk.Volume." + channel;

    /// <summary>The mixer's exposed volume parameters (TimeDesk.mixer).</summary>
    public const string MasterParameter = "MasterVolume", MusicParameter = "MusicVolume", UiParameter = "UiVolume", DeskParameter = "DeskVolume", AmbienceParameter = "AmbienceVolume";

    /// <summary>A channel's share (1, full, until the player moves its slider).</summary>
    public static float Volume(SoundChannel channel) => Mathf.Clamp01(PlayerPrefs.GetFloat(PlayerPrefKeys.For(Key(channel)), 1f));

    /// <summary>Stores a channel's share and applies it at once.</summary>
    public static void SetVolume(SoundChannel channel, float share)
    {
        PlayerPrefs.SetFloat(PlayerPrefKeys.For(Key(channel)), Mathf.Clamp01(share));
        PlayerPrefs.Save();
        Apply();
    }

    /// <summary>Sets every stored share on the mixer (the sound bank's), else the master share on the listener.</summary>
    public static void Apply()
    {
        AudioMixer mixer = Sounds.Bank != null ? Sounds.Bank.mixer : null;
        if (mixer == null)
        {
            AudioListener.volume = Volume(SoundChannel.Master);
            return;
        }
        float effects = SettingsRules.Decibels(Volume(SoundChannel.Effects));
        mixer.SetFloat(MasterParameter, SettingsRules.Decibels(Volume(SoundChannel.Master)));
        mixer.SetFloat(MusicParameter, SettingsRules.Decibels(Volume(SoundChannel.Music)));
        mixer.SetFloat(UiParameter, effects);
        mixer.SetFloat(DeskParameter, effects);
        mixer.SetFloat(AmbienceParameter, effects);
    }
}
