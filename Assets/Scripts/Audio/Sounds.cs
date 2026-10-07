using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The game's sound router: Play(id) plays a cue of the bank (RunConfig's
/// SoundBankSO; a cue without a clip plays nothing) through its mixer group,
/// rotating its variants and nudging each play's pitch (SoundBankSO.pitchJitter,
/// ±4 %), and the PA chime ducks the hall's ambience under it; SetBeds sets
/// how loud each ambience bed plays (HallAmbience: FeelDirector in the office;
/// none elsewhere), fading the loops in and out. Every cue asked for is
/// announced (Fired), clip or not, so a probe can log a day's cues. One
/// persistent player (SoundPlayer) made on first use; play mode only.
/// </summary>
public static class Sounds
{
    private static SoundBankSO _bank;
    private static bool _looked;
    private static SoundPlayer _player;

    /// <summary>Raised for every cue asked for: its id and whether a clip played (the cue probe logs them).</summary>
    public static event Action<string, bool> Fired;

    /// <summary>The bank (RunConfig.soundBank), or null.</summary>
    public static SoundBankSO Bank
    {
        get
        {
            if (_looked)
                return _bank;
            _looked = true;
            var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
            _bank = config != null ? config.soundBank : null;
            if (_bank == null)
                Debug.LogWarning("[Sounds] RunConfig has no SoundBankSO, so the game is silent. Run Tools > Audio > Import Sound List (it creates and assigns SoundBank_Default).");
            return _bank;
        }
    }

    /// <summary>Plays one-shot <paramref name="id"/> (outside play mode, or without a clip, nothing); true when a clip played (a caller with its own stand-in plays that instead).</summary>
    public static bool Play(string id)
    {
        if (!Application.isPlaying)
            return false;
        SoundBankSO bank = Bank;
        SoundBankSO.Cue cue = bank != null ? bank.Find(id) : null;
        bool clip = cue != null && cue.clips != null && cue.clips.Length > 0 && !cue.loop;
        Fired?.Invoke(id, clip);
        if (!clip)
            return false;
        Player.PlayOnce(bank, cue);
        return true;
    }

    /// <summary>Sets the hall's ambience beds to <paramref name="mix"/> (they fade there); null fades every bed out (the office closed).</summary>
    public static void SetBeds(AmbienceMix? mix)
    {
        if (!Application.isPlaying || (mix == null && _player == null))
            return;
        SoundBankSO bank = Bank;
        if (bank != null)
            Player.SetBeds(bank, mix);
    }

    private static SoundPlayer Player
    {
        get
        {
            if (_player != null)
                return _player;
            var host = new GameObject("Sounds") { hideFlags = HideFlags.HideInHierarchy };
            UnityEngine.Object.DontDestroyOnLoad(host);
            _player = host.AddComponent<SoundPlayer>();
            return _player;
        }
    }

    /// <summary>Forgets the bank and the player when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _bank = null;
        _looked = false;
        _player = null;
    }
}

/// <summary>
/// The persistent sound player behind Sounds: a few 2D sources per mixer
/// group for one-shots (round robin, so a pitch nudge never bends a sound
/// still playing), one looping source per ambience bed, the beds' fades and
/// the PA chime's duck. Its own random stream for the pitch nudges (never
/// UnityEngine.Random, so the gameplay's draws stay as they are).
/// </summary>
public sealed class SoundPlayer : MonoBehaviour
{
    /// <summary>One-shot sources per group.</summary>
    private const int Voices = 4;

    private readonly AudioSource[][] _voices = new AudioSource[4][];
    private readonly int[] _nextVoice = new int[4];
    private readonly Dictionary<string, int> _nextVariant = new Dictionary<string, int>();
    private readonly Dictionary<string, AudioSource> _beds = new Dictionary<string, AudioSource>();
    private readonly Dictionary<string, float> _bedTarget = new Dictionary<string, float>();
    private readonly System.Random _pitch = new System.Random(4021);
    private SoundBankSO _bank;
    private float _duck = 1f, _duckUntil;

    /// <summary>Plays <paramref name="cue"/>'s next variant once on its group's next voice.</summary>
    public void PlayOnce(SoundBankSO bank, SoundBankSO.Cue cue)
    {
        _bank = bank;
        int bus = (int)cue.bus;
        _voices[bus] ??= MakeVoices(bank.Group(cue.bus));
        AudioSource voice = _voices[bus][_nextVoice[bus]];
        _nextVoice[bus] = (_nextVoice[bus] + 1) % Voices;
        _nextVariant.TryGetValue(cue.id, out int variant);
        _nextVariant[cue.id] = (variant + 1) % cue.clips.Length;
        AudioClip clip = cue.clips[variant % cue.clips.Length];
        if (clip == null)
            return;
        voice.pitch = 1f + (float)(_pitch.NextDouble() * 2.0 - 1.0) * bank.pitchJitter;
        voice.PlayOneShot(clip, cue.volume * (cue.placeholder ? bank.placeholderVolume : 1f));
        if (cue.id == SoundCues.PaChime)
            _duckUntil = Time.unscaledTime + clip.length + bank.duckHold;
    }

    /// <summary>Aims each hall bed at its level in <paramref name="mix"/> (null: silence); a bed with a clip starts looping, unheard, the first time.</summary>
    public void SetBeds(SoundBankSO bank, AmbienceMix? mix)
    {
        _bank = bank;
        foreach (string id in SoundCues.HallBeds)
        {
            float level = mix?.Of(id) ?? 0f;
            _bedTarget[id] = level;
            if (level <= 0f || _beds.ContainsKey(id))
                continue;
            SoundBankSO.Cue cue = bank.Find(id);
            if (cue == null || cue.clips == null || cue.clips.Length == 0 || cue.clips[0] == null)
                continue;
            AudioSource bed = gameObject.AddComponent<AudioSource>();
            bed.clip = cue.clips[0];
            bed.loop = true;
            bed.playOnAwake = false;
            bed.spatialBlend = 0f;
            bed.volume = 0f;
            bed.outputAudioMixerGroup = bank.Group(SoundBus.Ambience);
            bed.Play();
            _beds.Add(id, bed);
        }
    }

    /// <summary>Fades the beds toward their levels, under the PA chime's duck.</summary>
    private void Update()
    {
        if (_bank == null || _beds.Count == 0)
            return;
        float dt = Time.unscaledDeltaTime;
        float duckTarget = Time.unscaledTime < _duckUntil ? _bank.duckLevel : 1f;
        _duck = Mathf.MoveTowards(_duck, duckTarget, dt / Mathf.Max(0.01f, _bank.duckFade));
        float step = dt / Mathf.Max(0.01f, _bank.bedFade);
        foreach (KeyValuePair<string, AudioSource> pair in _beds)
        {
            _bedTarget.TryGetValue(pair.Key, out float target);
            SoundBankSO.Cue cue = _bank.Find(pair.Key);
            float aim = target * _duck * (cue != null ? cue.volume : 1f);
            pair.Value.volume = Mathf.MoveTowards(pair.Value.volume, aim, step);
        }
    }

    /// <summary>The one-shot voices of a group.</summary>
    private AudioSource[] MakeVoices(UnityEngine.Audio.AudioMixerGroup group)
    {
        var voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0f;
            voices[i].outputAudioMixerGroup = group;
        }
        return voices;
    }
}
