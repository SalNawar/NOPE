using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The PC's look in one asset (D2, Saleh 2026-11-07: "pc no longer going for
/// windows xp and old pc vibes ... I'm going full cyberpunk futristic 80s
/// retro scifi"): which look the Fake-OS wears (<see cref="look"/>), the
/// cyberpunk look's colour tokens (dark glass in near-black blue, phosphor
/// cyan as the primary, magenta as the accent, amber for warnings and money:
/// the IOUPAD's Blade Runner pair, 2026-10-20), how far the leading culture
/// tints the accent, the chrome's geometry, the bezel, the glass overlay's
/// scan rows and bloom, the boot line's colour, and the art: the vector-grid
/// skyline wallpaper and the neon glyphs (by the cel kit sprite each one
/// replaces). DesktopConfigSO.theme names the asset in use
/// (Assets/Data/UI/PcTheme_Cyberpunk.asset); PcTheme_Cel.asset keeps the cel
/// kit's look (its look is Cel, so nothing is redrawn). PcSkin reads it at
/// run time; every knob applies the next time the office loads. The PC stays
/// English and keeps the kit's fonts and type scale whatever the look.
/// </summary>
[CreateAssetMenu(fileName = "PcTheme_", menuName = "TimeDesk/UI/PC Theme", order = 22)]
public sealed class PcThemeSO : ScriptableObject
{
    /// <summary>The look: Cel (the cel kit as built) or Cyberpunk (this asset's tokens and art).</summary>
    public PcLook look = PcLook.Cyberpunk;

    /// <summary>The cel kit the cyberpunk kit is made from (its fonts, inks and sprite names; Assets/Data/UI/UiKit_Default.asset).</summary>
    public UiKitSO baseKit;

    [Header("Tokens")]
    /// <summary>Glass: window bodies and surfaces (near-black blue; opaque: the UI blends in linear light, so a few per cent clear shows the window behind).</summary>
    public Color glass = new Color(0.039f, 0.063f, 0.125f, 1f);

    /// <summary>Raised glass: title bars, buttons, plates, tooltips.</summary>
    public Color glassRaised = new Color(0.071f, 0.110f, 0.200f, 1f);

    /// <summary>The deepest glass: the deck, fields, the sidebar, the tube behind the picture.</summary>
    public Color glassDeep = new Color(0.016f, 0.027f, 0.055f, 1f);

    /// <summary>Phosphor cyan: the primary (edges that matter, glyphs, focus, the tray, the boot line).</summary>
    public Color phosphor = new Color(0.239f, 0.949f, 1f, 1f);

    /// <summary>The accent (magenta): the chosen option, the Start key, badges, buyable orders. The leading culture tints it (<see cref="cultureAccent"/>).</summary>
    public Color accent = new Color(1f, 0.243f, 0.659f, 1f);

    /// <summary>Amber: warnings and money.</summary>
    public Color amber = new Color(1f, 0.710f, 0.278f, 1f);

    /// <summary>The alert red (close keys, errors, Quit).</summary>
    public Color danger = new Color(1f, 0.329f, 0.408f, 1f);

    /// <summary>Text on glass.</summary>
    public Color ink = new Color(0.867f, 0.969f, 1f, 1f);

    /// <summary>Quiet text on glass (captions, hints, placeholders).</summary>
    public Color inkMuted = new Color(0.541f, 0.655f, 0.741f, 1f);

    /// <summary>The quiet frame line round panels and plates.</summary>
    public Color edge = new Color(0.141f, 0.251f, 0.369f, 1f);

    /// <summary>How far the leading culture's colour (its theme's PrimaryAction) replaces the accent, 0 to 1 (0: always magenta).</summary>
    [Range(0f, 1f)] public float cultureAccent = 0.65f;

    [Header("Chrome (sprite pixels at the kit's 2x: 2 px is one design pixel)")]
    /// <summary>A panel's or a plate's corner radius.</summary>
    [Min(0f)] public float cornerRadius = 10f;

    /// <summary>The edge's width (the lit top edge's too).</summary>
    [Min(1f)] public float edgeWidth = 2f;

    /// <summary>The glow round a plate under the pointer, in its pad (at most the kit's pad).</summary>
    [Min(0f)] public float haloWidth = 18f;

    /// <summary>The glow's strength under the pointer (alpha at the plate's edge).</summary>
    [Range(0f, 1f)] public float haloStrength = 0.55f;

    [Header("The monitor")]
    /// <summary>The bezel's casing at its top.</summary>
    public Color bezel = new Color(0.075f, 0.090f, 0.125f, 1f);

    /// <summary>The bezel's casing at its bottom (the chin's shade).</summary>
    public Color bezelLow = new Color(0.035f, 0.043f, 0.067f, 1f);

    /// <summary>The thin lit line along the bezel's outer edge and the rim at the glass.</summary>
    public Color bezelLit = new Color(0.239f, 0.949f, 1f, 0.85f);

    /// <summary>The dark gasket round the glass.</summary>
    public Color gasket = new Color(0.008f, 0.012f, 0.024f, 1f);

    /// <summary>The bezel's corner radius, in the bezel art's pixels (half the frame's reference size).</summary>
    [Min(0f)] public float bezelRadius = 22f;

    /// <summary>The gasket's width round the glass, in the bezel art's pixels.</summary>
    [Min(0f)] public float gasketWidth = 9f;

    [Header("The glass overlay (on the monitor's glass only)")]
    /// <summary>The scan rows' darkness (0: none).</summary>
    [Range(0f, 1f)] public float scanlineStrength = 0.12f;

    /// <summary>One scan row in every this many screen pixels.</summary>
    [Min(2)] public int scanlinePitch = 3;

    /// <summary>How fast the scan rows roll, in screen pixels a second (the animated part; still under Reduced Motion).</summary>
    [Min(0f)] public float scanlineSpeed = 10f;

    /// <summary>The phosphor bloom rising towards the glass's edges (0: none).</summary>
    [Range(0f, 1f)] public float bloomStrength = 0.05f;

    /// <summary>The vignette's darkness in the glass's corners (0: none).</summary>
    [Range(0f, 1f)] public float vignetteStrength = 0.35f;

    [Header("Art")]
    /// <summary>The desktop wallpaper: the vector-grid skyline (made with PIL from the approved city plate: E:/unity/NOPE-tools/art-gen/pc_cyber/wallpaper.py). It replaces the XP wallpapers in the cyberpunk look.</summary>
    public Sprite wallpaper;

    /// <summary>How far the leading culture's accent tints the wallpaper, 0 to 1 (0: never).</summary>
    [Range(0f, 1f)] public float wallpaperTint = 0.3f;

    /// <summary>The monitor's power switch glyph on the bezel (white, tinted phosphor).</summary>
    public Sprite powerGlyph;

    /// <summary>The neon glyphs and tiles (white, the game tints them), each named as the cel kit sprite it replaces (tile_mail_rest, glyph_cross); a cel sprite with none here keeps its cel art (E:/unity/NOPE-tools/art-gen/pc_cyber/glyphs.py).</summary>
    public List<UiKitSO.KitSprite> glyphs = new List<UiKitSO.KitSprite>();

    /// <summary>True when the PC wears the cyberpunk look (Cel draws the cel kit as built).</summary>
    public bool IsCyberpunk => look == PcLook.Cyberpunk;

    /// <summary>The glyph named <paramref name="spriteName"/>, or null.</summary>
    public Sprite Glyph(string spriteName)
    {
        foreach (UiKitSO.KitSprite g in glyphs)
            if (g != null && g.name == spriteName)
                return g.sprite;
        return null;
    }

    /// <summary>The colour of <paramref name="token"/> (the accent as given: the culture's tint applied by the caller; Keep and Clear give clear).</summary>
    public Color Token(PcInk token, Color accentNow)
    {
        switch (token)
        {
            case PcInk.Glass: return glass;
            case PcInk.GlassRaised: return glassRaised;
            case PcInk.GlassDeep: return glassDeep;
            case PcInk.Ink: return ink;
            case PcInk.InkMuted: return inkMuted;
            case PcInk.Phosphor: return phosphor;
            case PcInk.Accent: return accentNow;
            case PcInk.Amber: return amber;
            case PcInk.Danger: return danger;
            case PcInk.Edge: return edge;
            default: return Color.clear;
        }
    }

    /// <summary>The colour a drawn piece of <paramref name="tone"/> is keyed in.</summary>
    public Color Tone(PcTone tone, Color accentNow)
    {
        switch (tone)
        {
            case PcTone.Primary: return phosphor;
            case PcTone.Accent: return accentNow;
            case PcTone.Amber: return amber;
            case PcTone.Danger: return danger;
            case PcTone.Muted: return inkMuted * new Color(1f, 1f, 1f, 0.6f);
            default: return edge;
        }
    }
}
