using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The desk machine's hardware sounds with their placeholders (the desk
/// machine spec; SOUND_LIST: a file not delivered yet "plays a generated
/// placeholder"): Play(id, source) plays the cue through the game's sound
/// bank when it has a clip (Sounds.Play: each id is SoundCues' and a row
/// of SOUND_LIST.md), else a short sound made in code once per id through
/// the source (a click, a snap, a whoosh, a chirp) until its file
/// is delivered. Tone and Noise make the placeholders, also for the
/// stamps' own refusal thunk and a delivered paper's thud (PaperArrival).
/// </summary>
public static class CueSounds
{
    private const int Rate = 22050;

    private static readonly Dictionary<string, AudioClip> Placeholders = new Dictionary<string, AudioClip>();

    /// <summary>Plays <paramref name="cue"/>: its clip in the sound bank, else its placeholder through <paramref name="source"/> at <paramref name="pitch"/> (nothing without a source).</summary>
    public static void Play(string cue, AudioSource source, float pitch = 1f)
    {
        if (Sounds.Play(cue) || source == null)
            return;
        if (!Placeholders.TryGetValue(cue, out AudioClip clip) || clip == null)
            Placeholders[cue] = clip = Make(cue);
        if (clip == null)
            return;
        source.pitch = pitch;
        source.PlayOneShot(clip);
    }

    /// <summary>The placeholder of <paramref name="cue"/> (null for a cue that has none).</summary>
    private static AudioClip Make(string cue)
    {
        switch (cue)
        {
            case SoundCues.DaterWheelClick: return Noise("WheelClick", 0.025f, 0.35f, 2600f);
            case SoundCues.DaterReink: return Noise("Reink", 0.18f, 0.35f, 500f);
            case SoundCues.DetainCover: return Noise("CoverSnap", 0.05f, 0.55f, 1500f);
            case SoundCues.PortalThrough: return Sweep("PortalThrough", 180f, 900f, 1.2f, 0.4f);
            case SoundCues.Detain: return Sweep("Detain", 1400f, 900f, 0.5f, 0.35f);
            case SoundCues.UiError: return Tone("Error", 70f, 0.16f, 0.7f);
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
