using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Tools > Audio > Import Sound List (Saleh 2026-10-07: "drop files in and
/// run one menu item"): reads Saleh's list (ArtDeliverables/TimeDesk/Audio/
/// SOUND_LIST.md, SoundList), copies every WAV next to it that the list names
/// (&lt;id&gt;.wav or &lt;id&gt;_vN.wav) into Assets/Audio/&lt;group&gt;/ (a changed file
/// is copied again), writes a small generated placeholder for each missing
/// P1 one-shot the synthesiser covers (Assets/Audio/Placeholders/&lt;id&gt;_placeholder.wav:
/// SoundSynth, mixed low; a real file replaces it in the bank the moment it
/// is imported; the ambience stays silent), makes the mixer (Assets/Audio/
/// TimeDesk.mixer: Master with UI, Desk, Ambience and Music) once, and fills
/// the bank (Assets/Data/Config/SoundBank_Default.asset: one cue per row,
/// its clips by variant, its group, loop, priority and placeholder flag; each
/// cue's volume and the bank's other knobs kept) and assigns it to RunConfig.
/// It warns about a cue the code plays (SoundCues) that the list lacks and a
/// WAV the list does not name. Idempotent.
/// </summary>
public static class SoundBankImporter
{
    /// <summary>Where Saleh drops the files and the list.</summary>
    public const string SourceFolder = "ArtDeliverables/TimeDesk/Audio";

    /// <summary>The list's file in the source folder.</summary>
    public const string ListFile = "SOUND_LIST.md";

    /// <summary>Where the imported clips live (one folder per group, and the placeholders').</summary>
    public const string AudioFolder = "Assets/Audio";

    /// <summary>The bank asset.</summary>
    public const string BankPath = "Assets/Data/Config/SoundBank_Default.asset";

    /// <summary>The mixer asset.</summary>
    public const string MixerPath = AudioFolder + "/TimeDesk.mixer";

    /// <summary>The placeholders' sample rate.</summary>
    private const int PlaceholderRate = 22050;

    [MenuItem("Tools/Audio/Import Sound List")]
    private static void ImportMenu() => Debug.Log("[SoundBankImporter] " + Import());

    /// <summary>Runs the import; returns its summary (the counts and every warning).</summary>
    public static string Import()
    {
        string listPath = Path.Combine(SourceFolder, ListFile);
        if (!File.Exists(listPath))
            return $"no list at {listPath}: nothing imported.";
        var problems = new List<string>();
        List<SoundListEntry> rows = SoundList.Parse(File.ReadAllText(listPath, Encoding.UTF8), problems);
        var ids = new HashSet<string>(rows.Select(r => r.Id));
        var byId = rows.ToDictionary(r => r.Id);

        // The dropped files, by cue and variant.
        var found = new Dictionary<string, SortedDictionary<int, string>>();
        foreach (string file in Directory.GetFiles(SourceFolder, "*.wav", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (!SoundList.TryMatchFile(name, ids, out string id, out int variant))
            {
                problems.Add($"'{Path.GetFileName(file)}' is not named by the list (expected <id>.wav or <id>_vN.wav)");
                continue;
            }
            if (!found.TryGetValue(id, out SortedDictionary<int, string> variants))
                found[id] = variants = new SortedDictionary<int, string>();
            variants[variant] = file;
        }

        int copied = 0, placeholders = 0;
        var clipPaths = new Dictionary<string, List<string>>();
        var isPlaceholder = new HashSet<string>();
        foreach (SoundListEntry row in rows)
        {
            var paths = new List<string>();
            if (found.TryGetValue(row.Id, out SortedDictionary<int, string> files))
                foreach (string file in files.Values)
                {
                    string target = $"{AudioFolder}/{row.Bus}/{Path.GetFileName(file)}";
                    if (CopyIfChanged(file, target))
                        copied++;
                    paths.Add(target);
                }
            else
            {
                PlaceholderKind kind = SoundList.PlaceholderFor(row);
                if (kind != PlaceholderKind.None)
                {
                    string target = $"{AudioFolder}/Placeholders/{row.Id}_placeholder.wav";
                    if (!File.Exists(target))
                    {
                        WriteWav(target, SoundSynth.Make(kind, row.Id, PlaceholderRate), PlaceholderRate);
                        placeholders++;
                    }
                    paths.Add(target);
                    isPlaceholder.Add(row.Id);
                }
            }
            clipPaths[row.Id] = paths;
        }
        AssetDatabase.Refresh();

        foreach (KeyValuePair<string, List<string>> pair in clipPaths)
            foreach (string path in pair.Value)
                Settle(path, byId[pair.Key].Loop);

        AudioMixer mixer = EnsureMixer(problems);
        SoundBankSO bank = AssetDatabase.LoadAssetAtPath<SoundBankSO>(BankPath);
        if (bank == null)
        {
            PlaceholderPng.EnsureFolderTree("Assets/Data/Config");
            bank = ScriptableObject.CreateInstance<SoundBankSO>();
            AssetDatabase.CreateAsset(bank, BankPath);
        }
        var volumes = (bank.cues ?? new SoundBankSO.Cue[0]).Where(c => c != null && c.id != null).GroupBy(c => c.id).ToDictionary(g => g.Key, g => g.First().volume);
        int withClips = 0;
        bank.cues = rows.Select(row =>
        {
            AudioClip[] clips = clipPaths[row.Id].Select(AssetDatabase.LoadAssetAtPath<AudioClip>).Where(c => c != null).ToArray();
            if (clips.Length > 0)
                withClips++;
            return new SoundBankSO.Cue
            {
                id = row.Id,
                bus = row.Bus,
                loop = row.Loop,
                priority = row.Priority,
                clips = clips,
                placeholder = isPlaceholder.Contains(row.Id),
                volume = volumes.TryGetValue(row.Id, out float v) ? v : 1f
            };
        }).ToArray();
        if (mixer != null)
        {
            bank.mixer = mixer;
            bank.uiGroup = Group(mixer, "UI");
            bank.deskGroup = Group(mixer, "Desk");
            bank.ambienceGroup = Group(mixer, "Ambience");
            bank.musicGroup = Group(mixer, "Music");
        }
        EditorUtility.SetDirty(bank);

        var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        if (config == null)
            problems.Add("no RunConfig: the bank is not assigned (create Assets/Resources/RunConfig.asset and import again)");
        else if (config.soundBank != bank)
        {
            config.soundBank = bank;
            EditorUtility.SetDirty(config);
        }
        AssetDatabase.SaveAssets();

        foreach (FieldInfo field in typeof(SoundCues).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral && f.FieldType == typeof(string)))
        {
            string id = (string)field.GetRawConstantValue();
            if (!ids.Contains(id))
                problems.Add($"the code plays '{id}' (SoundCues.{field.Name}) but the list has no such row");
        }

        string summary = $"{rows.Count} cues in the list, {found.Count} with files ({copied} copied now), {isPlaceholder.Count} on generated placeholders ({placeholders} written now), {withClips} with a clip, {rows.Count - withClips} silent; mixer {(mixer != null ? MixerPath : "missing")}.";
        foreach (string problem in problems)
            Debug.LogWarning("[SoundBankImporter] " + problem);
        return summary + (problems.Count > 0 ? $" {problems.Count} warning(s): " + string.Join(" | ", problems) : string.Empty);
    }

    /// <summary>Copies <paramref name="source"/> to <paramref name="target"/> unless an identical file is there; true when it copied.</summary>
    private static bool CopyIfChanged(string source, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        if (File.Exists(target) && File.ReadAllBytes(target).AsSpan().SequenceEqual(File.ReadAllBytes(source)))
            return false;
        File.Copy(source, target, true);
        return true;
    }

    /// <summary>A loop streams; a one-shot decompresses on load, forced to mono.</summary>
    private static void Settle(string path, bool loop)
    {
        if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            return;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        AudioClipLoadType load = loop ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        bool mono = !loop;
        if (settings.loadType == load && importer.forceToMono == mono)
            return;
        settings.loadType = load;
        importer.defaultSampleSettings = settings;
        importer.forceToMono = mono;
        importer.SaveAndReimport();
    }

    /// <summary>Writes <paramref name="samples"/> as a 16-bit mono PCM WAV at <paramref name="rate"/>.</summary>
    private static void WriteWav(string path, float[] samples, int rate)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var w = new BinaryWriter(stream);
        int bytes = samples.Length * 2;
        w.Write(Encoding.ASCII.GetBytes("RIFF"));
        w.Write(36 + bytes);
        w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
        w.Write(16);
        w.Write((short)1);
        w.Write((short)1);
        w.Write(rate);
        w.Write(rate * 2);
        w.Write((short)2);
        w.Write((short)16);
        w.Write(Encoding.ASCII.GetBytes("data"));
        w.Write(bytes);
        foreach (float s in samples)
            w.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * 32767f), -32768, 32767));
    }

    /// <summary>
    /// The mixer (Master with UI, Desk, Ambience and Music), made once through
    /// the editor's own mixer controller (Unity has no public API that makes
    /// a mixer or its groups, so its internal AudioMixerController is called
    /// by reflection; when that fails the bank plays without groups and the
    /// warning says how to make the mixer by hand).
    /// </summary>
    private static AudioMixer EnsureMixer(List<string> problems)
    {
        AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (mixer != null && Group(mixer, "Music") != null)
            return mixer;
        try
        {
            Type controller = typeof(AudioImporter).Assembly.GetType("UnityEditor.Audio.AudioMixerController")
                              ?? AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Audio.AudioMixerController")).FirstOrDefault(t => t != null);
            if (controller == null)
                throw new MissingMemberException("UnityEditor.Audio.AudioMixerController");
            Directory.CreateDirectory(AudioFolder);
            object made = mixer != null ? mixer : controller.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { MixerPath });
            object master = controller.GetProperty("masterGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(made);
            MethodInfo create = controller.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First(m => m.Name == "CreateNewGroup" && m.GetParameters().Length == 2);
            MethodInfo add = controller.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).First(m => m.Name == "AddChildToParent" && m.GetParameters().Length == 2);
            foreach (string name in new[] { "UI", "Desk", "Ambience", "Music" })
            {
                if (Group((AudioMixer)made, name) != null)
                    continue;
                object group = create.Invoke(made, new object[] { name, false });
                add.Invoke(made, new[] { group, master });
            }
            EditorUtility.SetDirty((UnityEngine.Object)made);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath);
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        }
        catch (Exception e)
        {
            problems.Add($"could not make the mixer ({e.GetType().Name}: {e.Message}); make {MixerPath} by hand (Master with UI, Desk, Ambience, Music) and import again");
            return mixer;
        }
    }

    /// <summary>The mixer's group called exactly <paramref name="name"/>, or null.</summary>
    private static AudioMixerGroup Group(AudioMixer mixer, string name) =>
        mixer != null ? mixer.FindMatchingGroups(name).FirstOrDefault(g => g.name == name) : null;
}
