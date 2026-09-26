using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// The 2D art slots found by name (redesign phase 27, the art hooks): where
/// each Tier-2 file of docs/ART_ASSET_LIST.md lives, how its name is made
/// from the game's ids, and the one lookup rule every slot uses
/// (<see cref="First{T}"/>: the first candidate whose art loads, else the
/// slot's code-drawn fallback). A slot is a path under a Resources folder,
/// without its extension: the files live at
/// <see cref="AssetRoot"/>&lt;slot&gt;.png, so a delivered PNG shows at the next
/// run with no code change and no rebuild; a missing file leaves today's look.
/// The wheel icons (WheelIcons/, Dialog.IconName) and the characters keep
/// their own names. Pure, so it is tested headless.
/// </summary>
public static class ArtSlots
{
    /// <summary>The folder every by-name slot lives under (a Resources folder; the slot is the path below it).</summary>
    public const string AssetRoot = "Assets/Art/UI/Resources/";

    /// <summary>The speech bubble's body: a light 9-slice panel the game tints cream.</summary>
    public const string SpeechBubble = "Office/speech_bubble";

    /// <summary>The speech bubble's tail, under the bubble's bottom centre (none without art).</summary>
    public const string SpeechBubbleTail = "Office/speech_bubble_tail";

    /// <summary>The morning briefing's newsprint sheet ("The Temporal Times").</summary>
    public const string BriefingPaper = "DayFlow/temporal_times_paper";

    /// <summary>The end of shift's ledger sheet.</summary>
    public const string LedgerPaper = "DayFlow/shift_ledger_paper";

    /// <summary>The citation slip after a wrong verdict.</summary>
    public const string CitationSlip = "DayFlow/citation_slip";

    /// <summary>The photo frame on a desk paper's photo cell (the frame's grey stand-in without art).</summary>
    public const string PhotoFrame = "Forms/photo_frame";

    /// <summary>Temporal Customs' seal, printed faintly behind a form's header (the builder's code-drawn ring without art).</summary>
    public const string AgencySeal = "Forms/agency_seal";

    /// <summary>The plain agency paper: a desk paper whose kind has no face of its own, and the PC's pages.</summary>
    public const string AgencyFace = "Forms/paper_agency";

    /// <summary>The landscape slot machine in the slot panel.</summary>
    public const string SlotMachine = "Home/slot_machine";

    /// <summary>The slot machine's lever, at the machine's right edge.</summary>
    public const string SlotLever = "Home/slot_lever";

    /// <summary>The Title's text-free 9-slice button face (Continue, New Run and the ending's New Run; the game prints the labels).</summary>
    public const string TitleButton = "Title/title_button";

    /// <summary>The Title button face under the pointer.</summary>
    public const string TitleButtonHover = "Title/title_button_hover";

    /// <summary>The names of the family portraits' three condition bands, best first.</summary>
    public static readonly IReadOnlyList<string> FamilyBands = new[] { "well", "ill", "grave" };

    /// <summary>A desktop icon's glyph by its app id (DesktopAppIds).</summary>
    public static string DesktopIcon(string appId) => "Desktop/icon_" + Key(appId);

    /// <summary>An upgrade's icon in its Home shop row, by the upgrade's id.</summary>
    public static string UpgradeIcon(string upgradeId) => "Home/upgrade_" + Key(upgradeId);

    /// <summary>A reference book's cover by the category it lists (the asset list's book ids: the Geography book is the capitals', the Politics book the rulers').</summary>
    public static string BookCover(ClueCategory category) => "Investigation/refbook_cover_" + BookId(category);

    /// <summary>The verdict's ink mark on the papers: the tick for an accept, the cross for a deny.</summary>
    public static string VerdictMark(bool accepted) => accepted ? "Forms/stamp_accept" : "Forms/stamp_deny";

    /// <summary>
    /// The faces a desk paper tries, in order: its own kind's face by its form
    /// number ("TC-610" gives Forms/paper_tc610), then the plain agency face.
    /// A paper with no form number tries the agency face only.
    /// </summary>
    public static IReadOnlyList<string> PaperFaces(string formNumber)
    {
        string key = Key(formNumber);
        return key.Length == 0 ? new[] { AgencyFace } : new[] { "Forms/paper_" + key, AgencyFace };
    }

    /// <summary>A family member's portrait for their condition: family_&lt;member&gt;_&lt;band&gt; (FamilyBand, the member's name as a key: "Partner" gives partner).</summary>
    public static string FamilyPortrait(string memberName, int condition, int maxCondition) =>
        "Home/family_" + Key(memberName) + "_" + FamilyBands[FamilyBand(condition, maxCondition)];

    /// <summary>
    /// A condition's band (0 best, 2 worst): the range 0 to
    /// <paramref name="maxCondition"/> in three equal thirds (at 10: 0 to 3,
    /// 4 to 7, 8 to 10). Below 0 is the best band, above the cap the worst.
    /// </summary>
    public static int FamilyBand(int condition, int maxCondition)
    {
        int max = Math.Max(1, maxCondition);
        int clamped = Math.Max(0, Math.Min(max, condition));
        return Math.Min(FamilyBands.Count - 1, clamped * FamilyBands.Count / (max + 1));
    }

    /// <summary>
    /// The lookup rule: the first of <paramref name="candidates"/> that
    /// <paramref name="load"/> finds (non-null), in order; null when none is
    /// found, and the caller keeps its code-drawn fallback. Empty and null
    /// candidates are skipped.
    /// </summary>
    public static T First<T>(IEnumerable<string> candidates, Func<string, T> load) where T : class
    {
        if (candidates == null || load == null)
            return null;
        foreach (string slot in candidates)
        {
            if (string.IsNullOrEmpty(slot))
                continue;
            T found = load(slot);
            if (found != null)
                return found;
        }
        return null;
    }

    /// <summary>The slot an asset path under <see cref="AssetRoot"/> fills ("Assets/Art/UI/Resources/Office/speech_bubble.png" gives Office/speech_bubble); null for any other path.</summary>
    public static string SlotOf(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith(AssetRoot, StringComparison.Ordinal))
            return null;
        string rest = assetPath.Substring(AssetRoot.Length);
        int dot = rest.LastIndexOf('.');
        int slash = rest.LastIndexOf('/');
        return dot > slash ? rest.Substring(0, dot) : rest;
    }

    /// <summary>
    /// The 9-slice border of a slot's art as a share of its shorter side (the
    /// bubble's rounded corners sit inside the outer 32 px of its 128 px
    /// master; the Title face's inside 32 of its 128 px height); 0 for art
    /// that is not sliced.
    /// </summary>
    public static float SliceShare(string slot) =>
        slot == SpeechBubble || slot == TitleButton || slot == TitleButtonHover ? 0.25f : 0f;

    /// <summary>Whether a slot's art is drawn on the desk's 3D papers (a paper face, the photo frame, an ink mark), so it is imported with mipmaps; UI art is not.</summary>
    public static bool OnDeskPaper(string slot) => slot != null && slot.StartsWith("Forms/", StringComparison.Ordinal);

    /// <summary>A name as a file-name key: lower case, letters and digits kept, a run of anything else one underscore, none at the ends ("TC-610" gives tc610; "Citizen Account" gives citizen_account).</summary>
    public static string Key(string name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;
        var sb = new StringBuilder(name.Length);
        bool gap = false;
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                if (gap && sb.Length > 0)
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
                gap = false;
            }
            else if (c != '-')
                gap = true;
        }
        return sb.ToString();
    }

    /// <summary>The asset list's id of the book listing <paramref name="category"/>.</summary>
    private static string BookId(ClueCategory category)
    {
        switch (category)
        {
            case ClueCategory.Geography:
                return "capital";
            case ClueCategory.Politics:
                return "ruler";
            default:
                return category.ToString().ToLowerInvariant();
        }
    }
}
