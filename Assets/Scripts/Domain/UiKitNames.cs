using System;

/// <summary>A kit control's look for one moment (docs/UI_KIT.md): at rest, under the pointer, pressed, or locked (disabled; screentone dots).</summary>
public enum KitState
{
    /// <summary>At rest (Selectable normal).</summary>
    Rest,

    /// <summary>Under the pointer (Selectable highlighted): the halo and the star glint.</summary>
    Hover,

    /// <summary>Pressed (Selectable pressed).</summary>
    Pressed,

    /// <summary>Locked (Selectable disabled): grey with screentone dots.</summary>
    Locked
}

/// <summary>
/// The UI kit's sprite names (Assets/Art/UI/Kit, kit_manifest.json): a
/// stateful piece's sprite is its name, an underscore and the state's word
/// ("plate_ox" at rest is plate_ox_rest), and which faces are dark (their
/// labels print in bone, the others in ink). Pure, so builders and views
/// share one grammar and it is tested headless.
/// </summary>
public static class UiKitNames
{
    /// <summary>The sprite of <paramref name="piece"/> in <paramref name="state"/> ("plate_ox", Hover gives plate_ox_hover).</summary>
    public static string Of(string piece, KitState state) => piece + "_" + Word(state);

    /// <summary>A state's word in the sprite names.</summary>
    public static string Word(KitState state)
    {
        switch (state)
        {
            case KitState.Hover:
                return "hover";
            case KitState.Pressed:
                return "pressed";
            case KitState.Locked:
                return "locked";
            default:
                return "rest";
        }
    }

    /// <summary>The upgrade card (Orders on the PC, the House at Home: one card, sheet 03) for an upgrade in <paramref name="state"/>: owned (brass rim), buyable (ordered ones too), too dear, locked (screentone).</summary>
    public static string UpgradeCard(OrderState state)
    {
        switch (state)
        {
            case OrderState.Owned:
                return "upgradecard_owned";
            case OrderState.TooDear:
                return "upgradecard_dear";
            case OrderState.Locked:
                return "upgradecard_locked";
            default:
                return "upgradecard_buyable";
        }
    }

    /// <summary>The round badge at an upgrade card's top right for <paramref name="state"/>: the tick when owned, the clock while too dear or on its way, the padlock when locked; null for a buyable card (no badge).</summary>
    public static string UpgradeBadge(OrderState state)
    {
        switch (state)
        {
            case OrderState.Owned:
                return "roundbadge_tick";
            case OrderState.TooDear:
            case OrderState.InTransit:
                return "roundbadge_clock";
            case OrderState.Locked:
                return "roundbadge_padlock";
            default:
                return null;
        }
    }

    /// <summary>The verdict ribbon (sheet 05): green for a right call, brass for a wrong one let off with a free warning, red for a wrong one; brass for a notice that is no verdict (<paramref name="correct"/> null).</summary>
    public static string VerdictRibbon(bool? correct, bool freeWarning) =>
        correct == true ? "ribbon_green" : correct == false && !freeWarning ? "ribbon_red" : "ribbon_brass";

    /// <summary>A traveller wheel choice's pictogram tile (sheet 05: the eye for a look, the ID card for a request, a speech balloon for a question or a line, the person for anything else).</summary>
    public static string WheelTile(DialogChoiceKind kind)
    {
        switch (kind)
        {
            case DialogChoiceKind.Look:
                return "tile_eye";
            case DialogChoiceKind.Request:
                return "tile_idcard";
            case DialogChoiceKind.Question:
            case DialogChoiceKind.Dialog:
                return "tile_speech";
            default:
                return "tile_person";
        }
    }

    /// <summary>The guide plate's header pill (sheet 05): red over a tutorial step (<paramref name="tutorial"/>: the one with Skip), green over a moment or a practice.</summary>
    public static string GuidePill(bool tutorial) => tutorial ? "pill_red" : "pill_green";

    /// <summary>The pieces whose face is dark (oxblood, slate, red, green, brass and lavender plates and their kin): a label on them prints in bone.</summary>
    private static readonly string[] DarkPieces =
    {
        "plate_ox", "plate_slate", "plate_red", "plate_green", "plate_brass", "plate_lav", "miniplate_ox", "miniplate_slate",
        "pulltab_", "iconkey", "titlebar_", "panel_dark", "panel_night", "panel_slate", "pill_", "strip_", "ribbon_green", "ribbon_red",
        "chip_active", "segment_on", "row_highlight", "badge_", "wheel_back", "wheelpill_hover", "lcd_glass", "inspect_"
    };

    /// <summary>True when a label on <paramref name="sprite"/> (a piece or a sprite name) prints in bone; false for the light faces (cards, bone plates, panels of paper, fields, rows, tooltips), whose labels print in ink.</summary>
    public static bool DarkFace(string sprite)
    {
        if (string.IsNullOrEmpty(sprite))
            return false;
        foreach (string dark in DarkPieces)
            if (sprite.StartsWith(dark, StringComparison.Ordinal))
                return true;
        return false;
    }
}
