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

    /// <summary>The glow inside an open departure portal's ring (greyscale; the game tints it and draws it as an unlit glow; PortalGlowPlaceholder's until it lands).</summary>
    public const string PortalGlow = "Office/portal_glow";

    /// <summary>The Return Gate's spiral inside its ring (greyscale; tinted amber, an unlit glow; PortalGlowPlaceholder's until it lands).</summary>
    public const string ReturnGateGlow = "Office/portal_return_glow";

    /// <summary>The photo frame on a desk paper's photo cell (the frame's grey stand-in without art).</summary>
    public const string PhotoFrame = "Forms/photo_frame";

    /// <summary>The holographic laminate over the photo on a document drawn on its art (FormArt; the photo frame's art on the others).</summary>
    public const string PhotoHolo = "Forms/photo_holo";

    /// <summary>Temporal Customs' seal, printed faintly behind a form's header (the builder's code-drawn ring without art).</summary>
    public const string AgencySeal = "Forms/agency_seal";

    /// <summary>The desk rulebook's open folder (Saleh's Canva art, run 7: its tabs, its heading and its line numbers painted out, the game prints them; clear outside its outline).</summary>
    public const string RulebookFolder = "Forms/rulebook_folder";

    /// <summary>The folder's clean face: its cover's words painted out (track LANG), printed by the game while a culture's language is read.</summary>
    public const string RulebookFolderClean = "Forms/rulebook_folder_clean";

    /// <summary>One of the rulebook folder's tabs by its art name ("rules", "papers", "guide", "seals" gives Forms/rulebook_tab_seals): cut from the art, its word painted out.</summary>
    public static string RulebookTab(string tab) => "Forms/rulebook_tab_" + Key(tab);

    /// <summary>The plain agency paper: a desk paper whose kind has no face of its own, and the PC's pages.</summary>
    public const string AgencyFace = "Forms/paper_agency";


    /// <summary>A desktop icon's glyph by its app id (DesktopAppIds).</summary>
    public static string DesktopIcon(string appId) => "Desktop/icon_" + Key(appId);

    /// <summary>A Home upgrade's icon in Home's list, by the upgrade's id (the office's upgrades moved to the Orders app: <see cref="OrderIcon"/>).</summary>
    public static string UpgradeIcon(string upgradeId) => "Home/upgrade_" + Key(upgradeId);

    /// <summary>An Orders node's icon (the upgrade tree, Saleh 2026-09-29), by the upgrade's id; without it the node draws its band's glyph.</summary>
    public static string OrderIcon(string upgradeId) => "Orders/upgrade_" + Key(upgradeId);

    /// <summary>An Orders band's glyph by its branch (Orders/branch_desk, _interview, _portals, _contacts); without it the band draws the code-drawn one.</summary>
    public static string OrderBranch(UpgradeBranch branch) => "Orders/branch_" + Key(branch.ToString());

    /// <summary>A reference book's cover by the category it lists (the asset list's book ids: the Geography book is the capitals', the Politics book the rulers').</summary>
    public static string BookCover(ClueCategory category) => "Investigation/refbook_cover_" + BookId(category);

    /// <summary>
    /// The faces a desk paper tries, in order: a passport's page for its
    /// holder's nation (<paramref name="issuer"/>, a nation id: "TC-101" and
    /// "egypt" give Forms/paper_tc101_egypt; the travel documents spec, TD5),
    /// its own kind's face by its form number ("TC-610" gives
    /// Forms/paper_tc610), then the plain agency face. A paper with no form
    /// number tries the agency face only.
    /// </summary>
    /// With <paramref name="clean"/> (a culture's language is read), its clean
    /// face ("TC-230" gives Forms/clean_tc230: every baked word painted out, the
    /// game prints them in the reading language) comes first.
    public static IReadOnlyList<string> PaperFaces(string formNumber, string issuer = null, bool clean = false)
    {
        string key = Key(formNumber), nation = Key(issuer);
        if (key.Length == 0)
            return new[] { AgencyFace };
        var faces = new List<string>();
        if (clean)
            faces.Add("Forms/clean_" + key);
        if (nation.Length > 0)
            faces.Add("Forms/paper_" + key + "_" + nation);
        faces.Add("Forms/paper_" + key);
        faces.Add(AgencyFace);
        return faces;
    }

    /// <summary>A document's blank face by its form number ("TC-230" gives Forms/blank_tc230): its art with every field's label painted out, from which a paper drawn on its art patches the label of a field not introduced yet (FormItemKind.Patch); none for a paper with no form number.</summary>
    public static string PaperBlank(string formNumber)
    {
        string key = Key(formNumber);
        return key.Length == 0 ? null : "Forms/blank_" + key;
    }

    /// <summary>A nation's passport emblem by its emblem's name (EmblemShapes: "WingedSun" gives Forms/emblem_wingedsun; the travel documents spec, TD5): white or one ink on clear, tinted by the cover's colour like the code-drawn stand-in.</summary>
    public static string Emblem(string emblem) => "Forms/emblem_" + Key(emblem);

    /// <summary>
    /// The pictures the pet tries for how it looks (the Home pet spec PS7 and
    /// PS11), in order: its coat's (Home/pet_&lt;kind&gt;_&lt;coat&gt;_&lt;look&gt;,
    /// "Home/pet_cat_ginger_sick"), then the kind's own
    /// (Home/pet_&lt;kind&gt;_&lt;look&gt;, the art request's first four states); a
    /// blank <paramref name="coat"/> tries the kind's only. Without any the
    /// corner draws the code-drawn stand-in (PetStandIn).
    /// </summary>
    public static string[] PetSprite(PetKind kind, string coat, PetLook look)
    {
        string k = Key(kind.ToString()), c = Key(coat), l = Key(look.ToString());
        return c.Length == 0 ? new[] { "Home/pet_" + k + "_" + l } : new[] { "Home/pet_" + k + "_" + c + "_" + l, "Home/pet_" + k + "_" + l };
    }

    /// <summary>The pet corner's backdrop (the corner of the flat where the pet sleeps); without it the corner is a plain plate.</summary>
    public const string PetCorner = "Home/pet_corner";

    /// <summary>The folder of the anime hall's swappable slots' art (HallSlotsSO; the hall slots spec): one folder per slot.</summary>
    public const string HallSlotsFolder = "Hall/Slots/";

    /// <summary>A hall slot variant's art by the slot's id and the variant's file ("13-flag-left-cloth", "egypt" gives Hall/Slots/13-flag-left-cloth/egypt): a painting on the hall's whole source canvas (HallSlotPick names the file).</summary>
    public static string HallSlot(string slotId, string file) => HallSlotsFolder + slotId + "/" + file;

    /// <summary>The largest side a slot's art keeps when imported: 4096 for the hall's slots (their canvas is the painting's 2172 px), 2048 for the rest.</summary>
    public static int MaxSide(string slot) => slot != null && slot.StartsWith(HallSlotsFolder, StringComparison.Ordinal) ? 4096 : 2048;

    /// <summary>A toy's picture in the pet corner, by the toy's upgrade id (its Orders icon is <see cref="OrderIcon"/>).</summary>
    public static string PetToy(string toyId) => "Home/toy_" + Key(toyId);

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
