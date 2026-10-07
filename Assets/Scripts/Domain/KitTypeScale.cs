using System;

/// <summary>
/// The UI kit's text roles (run 7, Saleh: "the text doesn't match the
/// scale"): every kit label is one of these, and its size comes from its
/// component's height through the type scale (UiKitSO.typeScale), never from
/// a size written in a builder.
/// </summary>
public enum KitText
{
    /// <summary>A primary or secondary plate's label (START SHIFT, ACKNOWLEDGE, SKIP TUTORIAL).</summary>
    PlateLabel,

    /// <summary>A mini plate's label (the PC's buttons, chips, options, the guide's buttons).</summary>
    MiniPlateLabel,

    /// <summary>A screen-edge pull tab's word (PC, STAMPS, CITY, DESK): its share is of the tab's width (the word runs across the tab).</summary>
    PullTabLabel,

    /// <summary>A keycap's key (TAB, SPACE, Q).</summary>
    Keycap,

    /// <summary>A desktop icon's name on its plate.</summary>
    TileCaption,

    /// <summary>An upgrade or choice card's title.</summary>
    CardTitle,

    /// <summary>A card's state or price line.</summary>
    CardSub,

    /// <summary>A heading on a panel (a window's title bar, a band head, the ledger's masthead).</summary>
    PanelHeading,

    /// <summary>A list row's title (Mail, Notes, menus).</summary>
    ListTitle,

    /// <summary>A text field's text.</summary>
    Field,

    /// <summary>A tooltip's or a hint plate's line.</summary>
    Tooltip,

    /// <summary>The verdict ribbon's line.</summary>
    Ribbon,

    /// <summary>A phosphor readout (the tray, the HUD).</summary>
    Readout,

    /// <summary>A pill's or a tag's word (the guide's header, NEW, a status pill, a chip).</summary>
    Pill,

    /// <summary>A reading line on a kit surface (a slip's detail, a bubble, a story, a pet's line): wraps, may shrink to its floor.</summary>
    Body,

    /// <summary>A small reading line (fine print, a caption under a field).</summary>
    BodySmall,

    /// <summary>A large reading line (a deck, an ending's outcomes).</summary>
    BodyLarge,

    /// <summary>A newspaper's or a sheet's masthead (the blackletter).</summary>
    Masthead,

    /// <summary>A key story's headline (condensed capitals).</summary>
    Headline,

    /// <summary>A lit sign's name (the Night Slots machine's marquee): big condensed capitals, one line.</summary>
    Marquee
}

/// <summary>
/// The type scale's rule, pure: a text's size is its share of the component's
/// height, held between a floor and a ceiling; and a label that does not fit
/// its room at that size first widens its component (when it may), else
/// shrinks no lower than the floor (never "shrink-to-tiny").
/// </summary>
public static class KitTypeScale
{
    /// <summary>The size for a component <paramref name="height"/> units tall at <paramref name="share"/> of it, between <paramref name="min"/> and <paramref name="max"/> (whole units); <paramref name="max"/> when the height is unknown (0).</summary>
    public static float Size(float share, float min, float max, float height)
    {
        if (max < min)
            (min, max) = (max, min);
        if (height <= 0f || share <= 0f)
            return max;
        return Math.Max(min, Math.Min(max, (float)Math.Round(share * height)));
    }

    /// <summary>True for a label role (condensed capitals, one line, sized to its component); false for the reading roles (the body lines, list rows, tooltips and fields: mixed case in the body face), which wrap and may shrink to their floor.</summary>
    public static bool IsLabel(KitText kind) =>
        kind != KitText.Body && kind != KitText.BodySmall && kind != KitText.BodyLarge && kind != KitText.ListTitle && kind != KitText.Tooltip && kind != KitText.Field;

    /// <summary>True for a label role set in tracked capitals (the kit's label tracking): every label but the masthead (the paper's name, set tight) and the readouts (the LCD face's digits, already spaced; tracked, the taskbar tray's line ran past its tray).</summary>
    public static bool IsTracked(KitText kind) =>
        IsLabel(kind) && kind != KitText.Masthead && kind != KitText.Readout;

    /// <summary>True for a role that wraps onto more lines: the reading roles, the headline, and an upgrade card's name and state line (an order's name runs to three lines, its state to two); every other label is one line, fitted to its room.</summary>
    public static bool Wraps(KitText kind) =>
        !IsLabel(kind) || kind == KitText.Headline || kind == KitText.CardTitle || kind == KitText.CardSub;

    /// <summary>The scale's defaults, each role's share of its component's height and its floor and ceiling (canvas units; 1080p reference px on the overlays): the sheets' label-to-plate proportions (a plate's label about 0.42 of its height). The body lines' share is 0: their size is their ceiling.</summary>
    public static (KitText kind, float share, float min, float max)[] Defaults => new[]
    {
        (KitText.PlateLabel, 0.42f, 16f, 34f),
        (KitText.MiniPlateLabel, 0.46f, 14f, 26f),
        (KitText.PullTabLabel, 0.24f, 16f, 28f),
        (KitText.Keycap, 0.5f, 12f, 20f),
        (KitText.TileCaption, 0.62f, 14f, 24f),
        (KitText.CardTitle, 0.26f, 14f, 28f),
        (KitText.CardSub, 0.2f, 12f, 20f),
        (KitText.PanelHeading, 0.66f, 24f, 44f), // a heading never under large text (24 px): its contrast is a heading's
        (KitText.ListTitle, 0.46f, 14f, 26f),
        (KitText.Field, 0.5f, 14f, 26f),
        (KitText.Tooltip, 0.42f, 14f, 22f),
        (KitText.Ribbon, 0.42f, 16f, 32f),
        (KitText.Readout, 0.5f, 14f, 26f),
        (KitText.Pill, 0.62f, 12f, 22f),
        (KitText.Body, 0f, 16f, 22f),
        (KitText.BodySmall, 0f, 12f, 18f),
        (KitText.BodyLarge, 0f, 18f, 28f),
        (KitText.Masthead, 0.8f, 40f, 96f),
        (KitText.Headline, 0.3f, 30f, 64f),
        (KitText.Marquee, 0.6f, 32f, 72f),
    };

    /// <summary>
    /// How a one-line label of <paramref name="width"/> units at
    /// <paramref name="size"/> fits <paramref name="room"/> units: the size it
    /// takes (scaled down to fit, never under <paramref name="min"/>) and how
    /// much wider its component must grow to hold it at that size (0 when it fits).
    /// </summary>
    public static (float size, float grow) Fit(float size, float width, float room, float min)
    {
        if (width <= room || width <= 0f)
            return (size, 0f);
        float fitted = Math.Max(min, (float)Math.Floor(size * room / width));
        float atFitted = width * fitted / size;
        return (fitted, Math.Max(0f, atFitted - room));
    }
}
