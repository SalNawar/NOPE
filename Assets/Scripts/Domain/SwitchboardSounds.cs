using System;

/// <summary>The switchboard's sounds (SwitchboardSounds.Make). Not serialized.</summary>
public enum SwitchSound
{
    /// <summary>A call comes in: a soft mains buzz, heard from the desk (LOOP_v5).</summary>
    Buzz,

    /// <summary>A plug seats in a jack: a click and a short thunk.</summary>
    PlugIn,

    /// <summary>A plug pulled out of a jack: a small pop.</summary>
    PlugOut,

    /// <summary>MASTER passing a ratchet notch on its way down.</summary>
    Ratchet,

    /// <summary>MASTER thrown home: a heavy clunk.</summary>
    Clunk,

    /// <summary>A fuse pops (a wrong period): a crackle and a thump.</summary>
    Fuse,

    /// <summary>A module finished calibrating: its route is live.</summary>
    Opened,

    /// <summary>A group leaves through the portal.</summary>
    Departed,

    /// <summary>A calibration's last seconds tick.</summary>
    Tick
}

/// <summary>
/// The switchboard's own procedural sounds, played when the sound bank has
/// no file for its cue (the sound list's rule: "a file that isn't there yet
/// plays a generated placeholder"): a soft buzz, a plug's click and thunk, a
/// pop, MASTER's clunk, a fuse's crackle; the ratchet, the open beep, the
/// departure chime and the tick are the game's own placeholders
/// (SoundSynth). Mono samples peaking at SoundSynth.Level, the same every
/// time (the noise seeded from the sound's name). Pure.
/// </summary>
public static class SwitchboardSounds
{
    /// <summary>The cue id the sound bank may carry a file for (SOUND_LIST.md style: the file's name).</summary>
    public static string CueId(SwitchSound sound)
    {
        switch (sound)
        {
            case SwitchSound.Buzz: return "switch_buzz";
            case SwitchSound.PlugIn: return "switch_plug_in";
            case SwitchSound.PlugOut: return "switch_plug_out";
            case SwitchSound.Ratchet: return "switch_ratchet";
            case SwitchSound.Clunk: return "switch_master";
            case SwitchSound.Fuse: return "switch_fuse";
            case SwitchSound.Opened: return "switch_open";
            case SwitchSound.Departed: return "switch_send";
            default: return "switch_tick";
        }
    }

    /// <summary>The samples of <paramref name="sound"/> at <paramref name="rate"/> samples a second.</summary>
    public static float[] Make(SwitchSound sound, int rate)
    {
        rate = Math.Max(8000, rate);
        string id = CueId(sound);
        var noise = new SeededRandom(SoundSynth.Hash(id));
        float[] s;
        switch (sound)
        {
            case SwitchSound.Buzz:
                s = Buffer(rate, 0.45f);
                for (int i = 0; i < s.Length; i++)
                {
                    float t = (float)i / rate;
                    float env = MathF.Min(1f, t / 0.02f) * MathF.Min(1f, (0.45f - t) / 0.08f);
                    float wave = 0f;
                    for (int h = 1; h <= 6; h++)
                        wave += MathF.Sin(2f * MathF.PI * 110f * h * t) / h;
                    s[i] = env * wave * (0.75f + 0.25f * MathF.Sin(2f * MathF.PI * 50f * t));
                }
                break;
            case SwitchSound.PlugIn:
                s = Buffer(rate, 0.16f);
                Burst(s, rate, 0f, 0.004f, noise, 1f);
                Ring(s, rate, 0f, 2400f, 90f, 0.35f);
                Ring(s, rate, 0.006f, 150f, 30f, 1f);
                break;
            case SwitchSound.PlugOut:
                s = Buffer(rate, 0.12f);
                Burst(s, rate, 0f, 0.006f, noise, 0.7f);
                for (int i = 0; i < s.Length; i++)
                {
                    float t = (float)i / rate;
                    s[i] += MathF.Exp(-t * 45f) * MathF.Sin(2f * MathF.PI * (320f - 900f * t) * t);
                }
                break;
            case SwitchSound.Clunk:
                s = Buffer(rate, 0.4f);
                Burst(s, rate, 0f, 0.012f, noise, 0.8f);
                Ring(s, rate, 0f, 68f, 9f, 1.2f);
                Ring(s, rate, 0.002f, 910f, 28f, 0.25f);
                Ring(s, rate, 0.004f, 1730f, 50f, 0.12f);
                break;
            case SwitchSound.Fuse:
                s = Buffer(rate, 0.55f);
                for (int i = 0; i < s.Length; i++)
                {
                    float t = (float)i / rate;
                    if (noise.Value() < 0.06f * MathF.Exp(-t * 7f))
                        s[i] += (noise.Value() * 2f - 1f) * 1.4f;
                }
                Burst(s, rate, 0f, 0.03f, noise, 0.9f);
                Ring(s, rate, 0.01f, 88f, 12f, 1f);
                break;
            case SwitchSound.Ratchet:
                return SoundSynth.Make(PlaceholderKind.Click, id, rate);
            case SwitchSound.Opened:
                return SoundSynth.Make(PlaceholderKind.Beep, id, rate);
            case SwitchSound.Departed:
                return SoundSynth.Make(PlaceholderKind.Chime, id, rate);
            default:
                return SoundSynth.Make(PlaceholderKind.Key, id, rate);
        }
        SoundSynth.Normalise(s);
        return s;
    }

    private static float[] Buffer(int rate, float seconds) => new float[Math.Max(1, (int)(rate * seconds))];

    /// <summary>A burst of noise from <paramref name="at"/> for about <paramref name="seconds"/> (exponential decay).</summary>
    private static void Burst(float[] s, int rate, float at, float seconds, SeededRandom noise, float gain)
    {
        int start = (int)(at * rate);
        for (int i = start; i < s.Length; i++)
        {
            float t = (float)(i - start) / rate;
            float env = MathF.Exp(-t / MathF.Max(1e-4f, seconds));
            if (env < 1e-4f)
                break;
            s[i] += gain * env * (noise.Value() * 2f - 1f);
        }
    }

    /// <summary>A struck tone of <paramref name="hertz"/> from <paramref name="at"/>, decaying at <paramref name="decay"/> a second.</summary>
    private static void Ring(float[] s, int rate, float at, float hertz, float decay, float gain)
    {
        int start = (int)(at * rate);
        for (int i = start; i < s.Length; i++)
        {
            float t = (float)(i - start) / rate;
            float env = MathF.Min(1f, t / 0.0015f) * MathF.Exp(-t * decay);
            if (t > 0.01f && env < 1e-4f)
                break;
            s[i] += gain * env * MathF.Sin(2f * MathF.PI * hertz * t);
        }
    }
}
