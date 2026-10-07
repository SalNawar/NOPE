using System;

/// <summary>
/// The small procedural placeholders for missing P1 one-shots (Saleh's sound
/// list: "a file that isn't there yet plays a generated placeholder, or
/// nothing"): a click, a key tap, a paper rustle, a printer burst, a retro
/// beep, a bell, a station chime, an alarm ding-ding, each a few hundred
/// milliseconds of mono samples peaking at <see cref="Level"/> (mixed low),
/// starting at once, the same for the same cue every time (its noise seeded
/// from the cue's id). The importer writes them as WAVs tagged as
/// placeholders; a real file replaces one by being dropped in.
/// </summary>
public static class SoundSynth
{
    /// <summary>A placeholder's peak (about -10 dBFS: mixed low under the real sounds to come).</summary>
    public const float Level = 0.3f;

    /// <summary>The samples of <paramref name="kind"/>'s placeholder for cue <paramref name="id"/> at <paramref name="rate"/> samples a second (empty for None).</summary>
    public static float[] Make(PlaceholderKind kind, string id, int rate)
    {
        var noise = new SeededRandom(Hash(id));
        float[] s;
        bool low = id == "scanner_flag" || id == "pc_error" || id == "ui_error";
        switch (kind)
        {
            case PlaceholderKind.Click:
                s = Buffer(rate, 0.03f);
                Tick(s, rate, 0f, id == "ui_release" ? 2600f : id == "ui_press" ? 1700f : 2100f, noise);
                break;
            case PlaceholderKind.Key:
                s = Buffer(rate, 0.07f);
                Tick(s, rate, 0f, 1300f, noise);
                Tick(s, rate, 0.035f, 1900f, noise, 0.5f);
                break;
            case PlaceholderKind.Paper:
                s = Buffer(rate, id == "paper_slide" ? 0.45f : id == "citation_tear" ? 0.35f : 0.2f);
                Rustle(s, rate, noise, id == "citation_tear" ? 40f : 14f);
                break;
            case PlaceholderKind.Printer:
                s = Buffer(rate, 1.1f);
                for (float t = 0f; t < 1.05f; t += 0.04f)
                    Tone(s, rate, t, 0.022f, 1150f, 0.6f, 0f, true);
                break;
            case PlaceholderKind.Beep:
                s = Buffer(rate, 0.32f);
                Tone(s, rate, 0f, 0.12f, low ? 233f : 880f, 1f, 0f, low);
                Tone(s, rate, 0.15f, 0.14f, low ? 196f : 1320f, 1f, 0f, low);
                break;
            case PlaceholderKind.Bell:
                s = Buffer(rate, id == "shift_end_bell" ? 2.4f : 0.6f);
                Bell(s, rate, 0f, id == "shift_end_bell" ? 420f : 1650f, id == "shift_end_bell" ? 1.4f : 0.25f);
                break;
            case PlaceholderKind.Chime:
                s = Buffer(rate, 1.5f);
                Bell(s, rate, 0f, 784f, 0.45f);
                Bell(s, rate, 0.42f, 659f * 1.004f, 0.45f);
                Bell(s, rate, 0.84f, 523f * 0.996f, 0.6f);
                break;
            case PlaceholderKind.Alarm:
                s = Buffer(rate, 1.4f);
                Bell(s, rate, 0f, 1380f, 0.35f);
                Bell(s, rate, 0.3f, 1040f, 0.35f);
                Bell(s, rate, 0.7f, 1380f, 0.35f);
                Bell(s, rate, 1.0f, 1040f, 0.35f);
                break;
            default:
                return Array.Empty<float>();
        }
        Normalise(s);
        return s;
    }

    /// <summary>A stable hash of a cue's id (FNV-1a; string.GetHashCode differs between runtimes and runs).</summary>
    public static int Hash(string id)
    {
        unchecked
        {
            uint h = 2166136261;
            foreach (char c in id ?? string.Empty)
                h = (h ^ c) * 16777619;
            return (int)h;
        }
    }

    private static float[] Buffer(int rate, float seconds) => new float[Math.Max(1, (int)(rate * seconds))];

    /// <summary>A click at <paramref name="at"/> seconds: a noise burst and a ringing tick of <paramref name="hertz"/>, decaying within milliseconds.</summary>
    private static void Tick(float[] s, int rate, float at, float hertz, SeededRandom noise, float gain = 1f)
    {
        int start = (int)(at * rate);
        for (int i = start; i < s.Length; i++)
        {
            float t = (float)(i - start) / rate;
            float env = MathF.Exp(-t * 260f);
            if (env < 1e-4f)
                break;
            s[i] += gain * env * (0.6f * (noise.Value() * 2f - 1f) + MathF.Sin(2f * MathF.PI * hertz * t));
        }
    }

    /// <summary>Paper: noise roughened by a slow random flutter (<paramref name="grain"/> bursts a second), a fast attack and a soft tail.</summary>
    private static void Rustle(float[] s, int rate, SeededRandom noise, float grain)
    {
        float flutter = 1f, last = 0f;
        int every = Math.Max(1, (int)(rate / grain));
        for (int i = 0; i < s.Length; i++)
        {
            if (i % every == 0)
                flutter = 0.35f + noise.Value() * 0.65f;
            float t = (float)i / s.Length;
            float env = MathF.Min(1f, i / (0.002f * rate)) * (1f - t) * (1f - t);
            float white = noise.Value() * 2f - 1f;
            s[i] = env * flutter * (white - last * 0.7f); // a little high-passed: crisp, not hiss
            last = white;
        }
    }

    /// <summary>A tone of <paramref name="hertz"/> from <paramref name="at"/> for <paramref name="seconds"/>, square when <paramref name="square"/>, with a 2 ms attack and release.</summary>
    private static void Tone(float[] s, int rate, float at, float seconds, float hertz, float gain, float decay, bool square)
    {
        int start = (int)(at * rate), n = (int)(seconds * rate), edge = Math.Max(1, (int)(0.002f * rate));
        for (int k = 0; k < n && start + k < s.Length; k++)
        {
            float t = (float)k / rate;
            float wave = MathF.Sin(2f * MathF.PI * hertz * t);
            if (square)
                wave = wave >= 0f ? 0.6f : -0.6f;
            float env = MathF.Min(1f, MathF.Min(k, n - k) / (float)edge) * MathF.Exp(-t * decay);
            s[start + k] += gain * env * wave;
        }
    }

    /// <summary>A struck bell of <paramref name="hertz"/> at <paramref name="at"/>: three inharmonic partials decaying over about <paramref name="ring"/> seconds.</summary>
    private static void Bell(float[] s, int rate, float at, float hertz, float ring)
    {
        int start = (int)(at * rate);
        for (int i = start; i < s.Length; i++)
        {
            float t = (float)(i - start) / rate;
            float env = MathF.Min(1f, t / 0.002f) * MathF.Exp(-t / ring);
            s[i] += env * (MathF.Sin(2f * MathF.PI * hertz * t) + 0.5f * MathF.Sin(2f * MathF.PI * hertz * 2.76f * t) * MathF.Exp(-t * 4f)
                           + 0.25f * MathF.Sin(2f * MathF.PI * hertz * 5.4f * t) * MathF.Exp(-t * 9f));
        }
    }

    /// <summary>Scales the samples so the loudest is <see cref="Level"/>.</summary>
    private static void Normalise(float[] s)
    {
        float peak = 0f;
        foreach (float v in s)
            peak = MathF.Max(peak, MathF.Abs(v));
        if (peak <= 0f)
            return;
        float k = Level / peak;
        for (int i = 0; i < s.Length; i++)
            s[i] *= k;
    }
}
