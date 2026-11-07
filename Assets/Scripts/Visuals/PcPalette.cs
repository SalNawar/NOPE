using System;

/// <summary>The theme token a PC graphic takes in the cyberpunk look (PcPalette.Of; PcThemeSO's colours).</summary>
public enum PcInk
{
    /// <summary>The graphic keeps its colour (evidence, the desk's pieces, art).</summary>
    Keep,

    /// <summary>Near-black blue glass: window bodies, surfaces.</summary>
    Glass,

    /// <summary>Raised glass: title bars, buttons, tooltips.</summary>
    GlassRaised,

    /// <summary>The deepest glass: the deck, fields, the sidebar.</summary>
    GlassDeep,

    /// <summary>Text on glass.</summary>
    Ink,

    /// <summary>Quiet text on glass.</summary>
    InkMuted,

    /// <summary>Phosphor cyan: the primary.</summary>
    Phosphor,

    /// <summary>The accent (magenta, tinted by the leading culture).</summary>
    Accent,

    /// <summary>Amber: warnings and money.</summary>
    Amber,

    /// <summary>The alert red.</summary>
    Danger,

    /// <summary>The quiet frame line.</summary>
    Edge,

    /// <summary>Nothing drawn (a gloss the glass has no use for).</summary>
    Clear
}

/// <summary>
/// The cyberpunk PC's colour rules (D2, the night of 2026-11-07): the token
/// each chrome role takes, for an image's fill and for a text's ink (every
/// surface is dark glass, so every chrome text is light), and the accent the
/// leading culture tints (Saleh 2026-10-20: "you should see effects on it like
/// maybe color changes"). The evidence's roles (ThemeRoles.IsDiegetic) and the
/// desk's own pieces keep their colours. Pure; PcSkin applies it.
/// </summary>
public static class PcPalette
{
    /// <summary>The token a graphic of <paramref name="role"/> takes: a text's ink when <paramref name="text"/>, else an image's fill.</summary>
    public static PcInk Of(ThemeRoleId role, bool text)
    {
        if (ThemeRoles.IsDiegetic(role) || ThemeRoles.IsRetired(role))
            return PcInk.Keep;
        switch (role)
        {
            case ThemeRoleId.Desktop:
            case ThemeRoleId.ClickCatcher:
            case ThemeRoleId.Alert:
            case ThemeRoleId.StickyNote:
            case ThemeRoleId.Newsletter:
            case ThemeRoleId.NewsletterBorder:
            case ThemeRoleId.NewsletterButton:
            case ThemeRoleId.WheelButton:
            case ThemeRoleId.CompareMatch:
            case ThemeRoleId.CompareMismatch:
            case ThemeRoleId.CompareNeutral:
            case ThemeRoleId.SelectionHighlight:
            case ThemeRoleId.MenuEntry when !text:
            case ThemeRoleId.QuitEntry when !text:
                return PcInk.Keep;
        }
        return text ? TextInk(role) : Fill(role);
    }

    /// <summary>
    /// The accent with the leading culture's colour <paramref name="culture"/>
    /// blended in by <paramref name="share"/> (0: the theme's own, 1: the
    /// culture's), the culture's colour first lifted to a neon (saturation at
    /// least 0.65, value at least 0.95) so it reads on the glass as the
    /// magenta does.
    /// </summary>
    public static Rgba CultureAccent(Rgba accent, Rgba culture, float share)
    {
        share = share < 0f ? 0f : share > 1f ? 1f : share;
        Rgba neon = Neon(culture);
        return PcChromeArt.Lerp(accent, neon, share).WithAlpha(accent.A);
    }

    /// <summary><paramref name="c"/> with its saturation raised to at least 0.65 and its value to at least 0.95 (its hue kept; a grey stays grey).</summary>
    public static Rgba Neon(Rgba c)
    {
        float max = Math.Max(c.R, Math.Max(c.G, c.B)), min = Math.Min(c.R, Math.Min(c.G, c.B));
        if (max - min < 1e-4f)
            return new Rgba(0.95f, 0.95f, 0.95f, c.A);
        float s = Math.Max((max - min) / max, 0.65f), v = Math.Max(max, 0.95f);
        // Each channel's place between the smallest and the largest keeps the hue.
        Func<float, float> channel = x => v * (1f - s * (1f - (x - min) / (max - min)));
        return new Rgba(channel(c.R), channel(c.G), channel(c.B), c.A);
    }

    /// <summary>An image's fill by its role.</summary>
    private static PcInk Fill(ThemeRoleId role)
    {
        switch (role)
        {
            case ThemeRoleId.WindowBody:
            case ThemeRoleId.Panel:
            case ThemeRoleId.Surface:
            case ThemeRoleId.SurfaceMuted:
            case ThemeRoleId.SearchResults:
            case ThemeRoleId.TabActive:
                return PcInk.Glass;
            case ThemeRoleId.Taskbar:
            case ThemeRoleId.InputField:
            case ThemeRoleId.Sidebar:
            case ThemeRoleId.TabStrip:
                return PcInk.GlassDeep;
            case ThemeRoleId.TaskbarGloss:
            case ThemeRoleId.TitleGloss:
                return PcInk.Clear;
            case ThemeRoleId.Hairline:
                return PcInk.Edge;
            case ThemeRoleId.HairlineStrong:
            case ThemeRoleId.FocusRing:
            case ThemeRoleId.IconSelection:
            case ThemeRoleId.DesktopIcon:
            case ThemeRoleId.AcceptButton:
            case ThemeRoleId.FindingMatch:
                return PcInk.Phosphor;
            case ThemeRoleId.StartButton:
            case ThemeRoleId.StartMenu:
            case ThemeRoleId.Badge:
            case ThemeRoleId.PrimaryAction:
                return PcInk.Accent;
            case ThemeRoleId.Holding:
                return PcInk.Amber;
            case ThemeRoleId.CloseButton:
            case ThemeRoleId.DenyButton:
            case ThemeRoleId.FindingDiffer:
                return PcInk.Danger;
            case ThemeRoleId.Info:
                return PcInk.InkMuted;
            default:
                return PcInk.GlassRaised;
        }
    }

    /// <summary>A text's ink by its role: light on the glass, quiet for captions, the tones for their meanings.</summary>
    private static PcInk TextInk(ThemeRoleId role)
    {
        switch (role)
        {
            case ThemeRoleId.SurfaceMuted:
            case ThemeRoleId.InputPlaceholder:
            case ThemeRoleId.Info:
                return PcInk.InkMuted;
            case ThemeRoleId.Tray:
            case ThemeRoleId.TabActive:
            case ThemeRoleId.AcceptButton:
            case ThemeRoleId.FindingMatch:
                return PcInk.Phosphor;
            case ThemeRoleId.PrimaryAction:
                return PcInk.Accent;
            case ThemeRoleId.Holding:
                return PcInk.Amber;
            case ThemeRoleId.QuitEntry:
            case ThemeRoleId.DenyButton:
            case ThemeRoleId.CloseButton:
            case ThemeRoleId.FindingDiffer:
                return PcInk.Danger;
            default:
                return PcInk.Ink;
        }
    }
}
