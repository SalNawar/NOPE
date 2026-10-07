using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>Which mixer group a sound plays through (the bank's UI / Desk / Ambience / Music). Serialized on SoundBankSO: append only.</summary>
public enum SoundBus
{
    /// <summary>Menus, buttons and the PC.</summary>
    Ui,

    /// <summary>The desk, the booth, the shift's bells and the river's stings.</summary>
    Desk,

    /// <summary>The looping beds (the hall, the rain, the booth's room tone).</summary>
    Ambience,

    /// <summary>The music loops.</summary>
    Music
}

/// <summary>The kind of small procedural placeholder a missing P1 one-shot plays (SoundSynth), or none (it stays silent until its file arrives).</summary>
public enum PlaceholderKind
{
    /// <summary>No placeholder: silent until the file arrives.</summary>
    None,

    /// <summary>A short plastic or mouse click.</summary>
    Click,

    /// <summary>A mechanical key tap.</summary>
    Key,

    /// <summary>A paper rustle (pick-up, drop, slide, page, tear, land).</summary>
    Paper,

    /// <summary>A dot-matrix printer burst.</summary>
    Printer,

    /// <summary>A retro two-tone beep (or a low one: a fault, an error).</summary>
    Beep,

    /// <summary>A big decaying bell.</summary>
    Bell,

    /// <summary>A three-note chime.</summary>
    Chime,

    /// <summary>A two-tone alarm ding-ding.</summary>
    Alarm
}

/// <summary>One row of Saleh's sound list (ArtDeliverables/TimeDesk/Audio/SOUND_LIST.md): the cue id (its file name), its section, how many variants it has, whether it loops, its priority and its mixer group.</summary>
public readonly struct SoundListEntry
{
    /// <summary>The cue id, also the file's name without its extension and variant ("paper_drop").</summary>
    public readonly string Id;

    /// <summary>The list's section (1 the desk ... 8 music).</summary>
    public readonly int Section;

    /// <summary>How many variants the list asks for (1 for a single file or a loop).</summary>
    public readonly int Variants;

    /// <summary>True for a loop (the ambience beds, the music, a "loop" row).</summary>
    public readonly bool Loop;

    /// <summary>The priority: 1 the game feels empty without it, 2 a big improvement, 3 polish.</summary>
    public readonly int Priority;

    /// <summary>Its mixer group.</summary>
    public readonly SoundBus Bus;

    /// <summary>A row from its parts.</summary>
    public SoundListEntry(string id, int section, int variants, bool loop, int priority, SoundBus bus)
    {
        Id = id;
        Section = section;
        Variants = variants;
        Loop = loop;
        Priority = priority;
        Bus = bus;
    }
}

/// <summary>
/// Saleh's sound list read as data (Saleh 2026-10-07: drop each file into
/// ArtDeliverables/TimeDesk/Audio under its name; the game picks it up):
/// the table rows of SOUND_LIST.md (a `## N.` heading starts section N; a
/// row's first backticked word is the id; "×3" asks for _v1.._v3; "loop" or
/// the ambience and music sections loop; "P1".."P3"), each cue's mixer group
/// by section, the files that may stand for a cue (its name, or its name
/// with _vN), and which missing P1 one-shots get a small procedural
/// placeholder (clicks, key taps, paper, the printer, beeps, bells, chimes,
/// the alarm; the ambience stays silent rather than cheap). Pure, so the
/// importer and the tests read the list alike.
/// </summary>
public static class SoundList
{
    /// <summary>The sections whose rows are loops whatever their columns say (7 the ambience beds, 8 the music).</summary>
    public const int AmbienceSection = 7, MusicSection = 8;

    private static readonly Regex Heading = new Regex(@"^##\s+(\d+)\.", RegexOptions.CultureInvariant);
    private static readonly Regex Id = new Regex(@"`([a-z0-9_]+)`", RegexOptions.CultureInvariant);
    private static readonly Regex Variant = new Regex(@"^[×x](\d+)$", RegexOptions.CultureInvariant);
    private static readonly Regex Priority = new Regex(@"^P([1-3])$", RegexOptions.CultureInvariant);
    private static readonly Regex VariantFile = new Regex(@"^(.+)_v(\d+)$", RegexOptions.CultureInvariant);

    /// <summary>
    /// The rows of <paramref name="markdown"/> in order; a row it cannot read
    /// (no id, no priority, a repeated id) is skipped with a line in
    /// <paramref name="problems"/> (when given) naming it.
    /// </summary>
    public static List<SoundListEntry> Parse(string markdown, List<string> problems = null)
    {
        var rows = new List<SoundListEntry>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int section = 0;
        foreach (string raw in (markdown ?? string.Empty).Split('\n'))
        {
            string line = raw.Trim();
            Match heading = Heading.Match(line);
            if (heading.Success)
            {
                section = int.Parse(heading.Groups[1].Value, CultureInfo.InvariantCulture);
                continue;
            }
            if (section == 0 || !line.StartsWith("|", StringComparison.Ordinal))
                continue;
            string[] cells = line.Trim('|').Split('|');
            if (cells.Length < 3 || !int.TryParse(cells[0].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out _))
                continue; // the header and its rule
            Match id = Id.Match(cells[1]);
            Match priority = Priority.Match(cells[cells.Length - 1].Trim());
            if (!id.Success || !priority.Success)
            {
                problems?.Add($"section {section}: the row '{line}' has no `id` or no P1-P3");
                continue;
            }
            string name = id.Groups[1].Value;
            if (!seen.Add(name))
            {
                problems?.Add($"section {section}: '{name}' is listed twice");
                continue;
            }
            int variants = 1;
            bool loop = section == AmbienceSection || section == MusicSection;
            if (cells.Length >= 7)
            {
                string var = cells[cells.Length - 2].Trim();
                Match count = Variant.Match(var);
                if (count.Success)
                    variants = Math.Max(1, int.Parse(count.Groups[1].Value, CultureInfo.InvariantCulture));
                else if (var.Equals("loop", StringComparison.OrdinalIgnoreCase))
                    loop = true;
            }
            rows.Add(new SoundListEntry(name, section, loop ? 1 : variants, loop, int.Parse(priority.Groups[1].Value, CultureInfo.InvariantCulture), BusOf(section)));
        }
        return rows;
    }

    /// <summary>The mixer group of section <paramref name="section"/>: the PC and the interface (3, 4) UI; the ambience beds (7) Ambience; the music (8) Music; the rest (the desk, the booth, the shift, the river) Desk.</summary>
    public static SoundBus BusOf(int section)
    {
        switch (section)
        {
            case 3:
            case 4:
                return SoundBus.Ui;
            case AmbienceSection:
                return SoundBus.Ambience;
            case MusicSection:
                return SoundBus.Music;
            default:
                return SoundBus.Desk;
        }
    }

    /// <summary>
    /// Files that stand for a cue under another name (Saleh 2026-10-07: the
    /// GPT stamp, a real two-click press, is the reference for the list's
    /// stamps): stamp_real_cha_ka.wav is stamp_approve, its _deep take
    /// (lower, harder) stamp_deny. A listed name of its own still wins.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> FileAliases = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["stamp_real_cha_ka"] = "stamp_approve",
        ["stamp_real_cha_ka_deep"] = "stamp_deny"
    };

    /// <summary>
    /// The cue and the variant a file name (without its extension) stands
    /// for: "paper_drop_v2" is paper_drop's variant 2, "pc_on" is pc_on's
    /// variant 1, an alias (FileAliases) its cue's variant 1; false when
    /// <paramref name="ids"/> holds none of them.
    /// </summary>
    public static bool TryMatchFile(string fileName, ICollection<string> ids, out string id, out int variant)
    {
        id = null;
        variant = 0;
        if (string.IsNullOrEmpty(fileName) || ids == null)
            return false;
        if (!ids.Contains(fileName) && FileAliases.TryGetValue(fileName, out string alias) && ids.Contains(alias))
        {
            id = alias;
            variant = 1;
            return true;
        }
        Match v = VariantFile.Match(fileName);
        if (v.Success && ids.Contains(v.Groups[1].Value))
        {
            id = v.Groups[1].Value;
            variant = int.Parse(v.Groups[2].Value, CultureInfo.InvariantCulture);
            return variant > 0;
        }
        if (!ids.Contains(fileName))
            return false;
        id = fileName;
        variant = 1;
        return true;
    }

    /// <summary>
    /// The placeholder a missing <paramref name="entry"/> plays: only a P1
    /// one-shot of the kinds a few generated samples can stand for (clicks,
    /// key taps, paper, the printer, beeps, bells, chimes, the alarm); None for
    /// the rest (the stamps, whose reference Saleh is making, the shutters, the
    /// river's stings, every loop), which stay silent until their files arrive.
    /// </summary>
    public static PlaceholderKind PlaceholderFor(SoundListEntry entry)
    {
        if (entry.Loop || entry.Priority != 1)
            return PlaceholderKind.None;
        switch (entry.Id)
        {
            case "ui_press":
            case "ui_release":
            case "ui_toggle":
            case "mouse_click":
            case "window_open":
            case "window_close":
            case "stamp_bar_in":
            case "slot_lever":
            case "slot_stop":
                return PlaceholderKind.Click;
            case "key_tap":
                return PlaceholderKind.Key;
            case "paper_pickup":
            case "paper_drop":
            case "paper_slide":
            case "rulebook_page":
            case "citation_tear":
            case "citation_land":
                return PlaceholderKind.Paper;
            case "citation_print":
                return PlaceholderKind.Printer;
            case "scanner_start":
            case "scanner_done":
            case "scanner_flag":
            case "pc_error":
            case "ui_error":
            case "call_next":
                return PlaceholderKind.Beep;
            case "shift_end_bell":
            case "inspect_match":
            case "slot_win":
                return PlaceholderKind.Bell;
            case "pa_chime":
                return PlaceholderKind.Chime;
            case "last_hour_alarm":
                return PlaceholderKind.Alarm;
            default:
                return PlaceholderKind.None;
        }
    }
}
