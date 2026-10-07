using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The desk hardware's sound cues with their placeholders (the desk machine
/// spec; docs SOUND_LIST: a file not delivered yet "plays a generated
/// placeholder"): Play(cue, source) plays the cue's clip from UiSoundSO when
/// it has one (UiSounds.Play), else a short sound made in code once per cue
/// through <paramref name="source"/> (a click, a thunk, a whoosh, a buzz, a
/// chirp). Tone and Noise make the placeholders, also for the stamps' own
/// refusal thunk.
/// </summary>
public static class CueSounds
{
    private const int Rate = 22050;

    private static readonly Dictionary<UiSoundCue, AudioClip> Placeholders = new Dictionary<UiSoundCue, AudioClip>();

    /// <summary>Plays <paramref name="cue"/>: its delivered clip, else its placeholder through <paramref name="source"/> at <paramref name="pitch"/> (nothing without a source).</summary>
    public static void Play(UiSoundCue cue, AudioSource source, float pitch = 1f)
    {
        if (UiSounds.Play(cue) || source == null)
            return;
        if (!Placeholders.TryGetValue(cue, out AudioClip clip) || clip == null)
            Placeholders[cue] = clip = Make(cue);
        if (clip == null)
            return;
        source.pitch = pitch;
        source.PlayOneShot(clip);
    }

    /// <summary>The placeholder of <paramref name="cue"/> (null for a cue that has none).</summary>
    private static AudioClip Make(UiSoundCue cue)
    {
        switch (cue)
        {
            case UiSoundCue.WheelClick: return Noise("WheelClick", 0.025f, 0.35f, 2600f);
            case UiSoundCue.Reink: return Noise("Reink", 0.18f, 0.35f, 500f);
            case UiSoundCue.LeverRatchet: return Noise("LeverRatchet", 0.04f, 0.6f, 1800f);
            case UiSoundCue.LeverHome: return Tone("LeverHome", 62f, 0.28f, 0.9f);
            case UiSoundCue.PortalThrough: return Sweep("PortalThrough", 180f, 900f, 1.2f, 0.4f);
            case UiSoundCue.Return: return Sweep("Return", 130f, 110f, 0.45f, 0.5f);
            case UiSoundCue.Detain: return Sweep("Detain", 1400f, 900f, 0.5f, 0.35f);
            case UiSoundCue.Error: return Tone("Error", 70f, 0.16f, 0.7f);
            default: return null;
        }
    }

    /// <summary>A short decaying tone (<paramref name="hertz"/>, <paramref name="seconds"/> long, at <paramref name="volume"/>): a thump or a thunk.</summary>
    public static AudioClip Tone(string name, float hertz, float seconds, float volume)
    {
        int n = Mathf.Max(1, (int)(Rate * seconds));
        var samples = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            samples[i] = volume * Mathf.Sin(2f * Mathf.PI * hertz * t) * Mathf.Exp(-t * 6f / seconds);
        }
        return Clip(name, samples);
    }

    /// <summary>A burst of filtered noise (<paramref name="seconds"/> long; <paramref name="hertz"/> sets how bright): a click, a ratchet, a squish.</summary>
    public static AudioClip Noise(string name, float seconds, float volume, float hertz)
    {
        int n = Mathf.Max(1, (int)(Rate * seconds));
        var samples = new float[n];
        float a = Mathf.Clamp01(2f * Mathf.PI * hertz / Rate), y = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Rate;
            float white = DaterInk.Hash01(i, 17, 3) * 2f - 1f;
            y += a * (white - y);
            samples[i] = volume * y * 3f * Mathf.Exp(-t * 7f / seconds);
        }
        return Clip(name, samples);
    }

    /// <summary>A tone gliding from <paramref name="from"/> to <paramref name="to"/> Hz over <paramref name="seconds"/>, swelling then fading: a whoosh, a buzzer, a chirp.</summary>
    private static AudioClip Sweep(string name, float from, float to, float seconds, float volume)
    {
        int n = Mathf.Max(1, (int)(Rate * seconds));
        var samples = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / n;
            phase += 2f * Mathf.PI * Mathf.Lerp(from, to, k) / Rate;
            float square = Mathf.Sin(phase) + 0.3f * Mathf.Sin(3f * phase);
            samples[i] = volume * square * Mathf.Sin(Mathf.PI * k);
        }
        return Clip(name, samples);
    }

    private static AudioClip Clip(string name, float[] samples)
    {
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, Rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>Forgets the placeholders when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Placeholders.Clear();
}
