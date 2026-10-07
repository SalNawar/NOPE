using UnityEngine;

/// <summary>Short sounds made in code until sound assets exist: the stamps' thump and thunk, a delivered paper's thud.</summary>
public static class CodeTones
{
    /// <summary>A short decaying tone (<paramref name="hertz"/>, <paramref name="seconds"/> long, at <paramref name="volume"/>), named <paramref name="name"/>.</summary>
    public static AudioClip Tone(string name, float hertz, float seconds, float volume)
    {
        const int rate = 22050;
        int n = Mathf.Max(1, (int)(rate * seconds));
        var samples = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / rate;
            samples[i] = volume * Mathf.Sin(2f * Mathf.PI * hertz * t) * Mathf.Exp(-t * 6f / seconds);
        }
        AudioClip clip = AudioClip.Create(name, n, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
