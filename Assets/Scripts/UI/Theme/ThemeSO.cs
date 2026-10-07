using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// One culture's look for the PC (piece 6): the resolved colour of every
/// role, the hover rings, the font candidates and style additions, the label
/// language and the wallpaper. Written by Tools &gt; TimeDesk &gt; Generate
/// World from world_source.json (ui.neutral and countries[].culture); never
/// edited by hand and never written at runtime.
/// </summary>
[CreateAssetMenu(fileName = "Theme_", menuName = "TimeDesk/UI/Theme", order = 20)]
public sealed class ThemeSO : ScriptableObject
{
    /// <summary>"neutral" or a NationSO.id.</summary>
    public string cultureId;

    /// <summary>Name shown in the debug inspector.</summary>
    public string displayName;

    /// <summary>The label language (a UiStringTableSO.language).</summary>
    public string language;

    /// <summary>False = keep the project's default TMP font (the neutral theme); true = resolve a runtime font.</summary>
    public bool runtimeFont;

    /// <summary>OS fonts tried in order; the runtime LiberationSans asset is always the last resort.</summary>
    public List<FontCandidate> fonts = new List<FontCandidate>();

    /// <summary>Added to a heading's style.</summary>
    public FontStyles headingAdd;

    /// <summary>Added to a button label's style.</summary>
    public FontStyles buttonAdd;

    /// <summary>Drops synthetic italics (CJK, Arabic).</summary>
    public bool stripItalic;

    /// <summary>The resolved colour of every role this theme has.</summary>
    public List<PaletteEntry> palette = new List<PaletteEntry>();

    /// <summary>The hover outline's inner ring.</summary>
    public Color ringDark;

    /// <summary>The hover outline's outer ring.</summary>
    public Color ringLight;

    /// <summary>The desktop wallpaper (text-free art; a generated placeholder until final art exists).</summary>
    public Sprite wallpaper;

    /// <summary>
    /// The neutral theme's only: the wallpaper of each world outcome that is
    /// not "as the run found it" (world_source.json ui.wallpapers), shown
    /// when that change is the most recent (DesktopWallpaper.Pick).
    /// </summary>
    public List<OutcomeWallpaper> outcomeWallpapers = new List<OutcomeWallpaper>();

    /// <summary>The wallpaper of <paramref name="factor"/>'s outcome <paramref name="outcome"/>, or null.</summary>
    public Sprite OutcomeWallpaper(string factor, string outcome)
    {
        foreach (OutcomeWallpaper w in outcomeWallpapers)
            if (w != null && w.factor == factor && w.outcome == outcome)
                return w.wallpaper;
        return null;
    }

    /// <summary>Palette entries by role (built on first use).</summary>
    [NonSerialized] private Dictionary<ThemeRoleId, PaletteEntry> _byRole;

    /// <summary>The entry of a role, or null when this theme has none.</summary>
    public PaletteEntry Get(ThemeRoleId role)
    {
        if (_byRole == null)
        {
            _byRole = new Dictionary<ThemeRoleId, PaletteEntry>();
            foreach (PaletteEntry e in palette)
                if (e != null && !_byRole.ContainsKey(e.role))
                    _byRole[e.role] = e;
        }
        return _byRole.TryGetValue(role, out PaletteEntry entry) ? entry : null;
    }

    /// <summary>Drops the cached lookup when Generate World rewrites the palette.</summary>
    private void OnValidate() => _byRole = null;

    /// <summary>Drops the cached lookup after a reload.</summary>
    private void OnEnable() => _byRole = null;
}

/// <summary>A world outcome's desktop wallpaper (ThemeSO.outcomeWallpapers).</summary>
[Serializable]
public sealed class OutcomeWallpaper
{
    /// <summary>The factor's id ("government").</summary>
    public string factor;

    /// <summary>The outcome's id ("democracy").</summary>
    public string outcome;

    /// <summary>Its text-free wallpaper.</summary>
    public Sprite wallpaper;
}

/// <summary>One OS font to try: a font file (and face index for .ttc collections) found through Font.GetPathsToOSFonts, else a family and style name.</summary>
[Serializable]
public sealed class FontCandidate
{
    /// <summary>File name ("msyh.ttc"); empty = use the family only.</summary>
    public string file;

    /// <summary>Face index inside a collection file.</summary>
    public int face;

    /// <summary>Family name ("Microsoft YaHei"); empty = the file only.</summary>
    public string family;

    /// <summary>Style name for the family ("Regular").</summary>
    public string style = "Regular";
}

/// <summary>A role's colours in one theme (the class lets the validator re-check a stored theme).</summary>
[Serializable]
public sealed class PaletteEntry
{
    /// <summary>The role.</summary>
    public ThemeRoleId role;

    /// <summary>Whether the role has a fill.</summary>
    public bool hasFill;

    /// <summary>The fill.</summary>
    public Color fill;

    /// <summary>Whether the role has an ink.</summary>
    public bool hasInk;

    /// <summary>The ink.</summary>
    public Color ink;

    /// <summary>How much contrast the ink needs on the fill.</summary>
    public ContrastClass textClass;
}
